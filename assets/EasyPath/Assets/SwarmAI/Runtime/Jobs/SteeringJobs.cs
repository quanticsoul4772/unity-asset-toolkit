using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace SwarmAI.Jobs
{
    /// <summary>
    /// Burst-compiled job for calculating flocking steering forces in parallel.
    /// Computes Separation, Alignment, and Cohesion for each agent.
    /// </summary>
    [BurstCompile]
    public struct FlockingSteeringJob : IJobParallelFor
    {
        /// <summary>All agent data (read-only).</summary>
        [ReadOnly] public NativeArray<AgentData> Agents;
        
        /// <summary>Spatial hash for neighbor queries.</summary>
        [ReadOnly] public NativeParallelMultiHashMap<int, int> SpatialHash;
        
        /// <summary>Behavior weights.</summary>
        [ReadOnly] public BehaviorWeights Weights;
        
        /// <summary>Cell size for spatial queries.</summary>
        public float CellSize;
        
        /// <summary>Output steering forces.</summary>
        [WriteOnly] public NativeArray<SteeringResult> Results;
        
        public void Execute(int index)
        {
            AgentData agent = Agents[index];
            
            if (!agent.IsActive)
            {
                Results[index] = new SteeringResult { SteeringForce = float3.zero, NeighborCount = 0 };
                return;
            }
            
            // Initialize accumulators
            float3 separationForce = float3.zero;
            float3 alignmentForce = float3.zero;
            float3 cohesionForce = float3.zero;
            float3 centerOfMass = float3.zero;
            float3 avgVelocity = float3.zero;
            
            int separationCount = 0;
            int alignmentCount = 0;
            int cohesionCount = 0;
            
            float neighborRadiusSq = agent.NeighborRadius * agent.NeighborRadius;
            float separationRadiusSq = Weights.SeparationRadius * Weights.SeparationRadius;
            
            // Calculate cell range to check
            int cellRadius = (int)math.ceil(agent.NeighborRadius / CellSize);
            int2 centerCell = new int2(
                (int)math.floor(agent.Position.x / CellSize),
                (int)math.floor(agent.Position.z / CellSize)
            );
            
            // Query neighbors from spatial hash
            for (int dx = -cellRadius; dx <= cellRadius; dx++)
            {
                for (int dz = -cellRadius; dz <= cellRadius; dz++)
                {
                    int2 cell = centerCell + new int2(dx, dz);
                    int hash;
                    unchecked
                    {
                        hash = cell.x * 73856093 ^ cell.y * 19349663;
                    }
                    
                    // Iterate through all agents in this cell
                    if (SpatialHash.TryGetFirstValue(hash, out int neighborIndex, out var iterator))
                    {
                        do
                        {
                            // Skip self
                            if (neighborIndex == index) continue;
                            
                            AgentData neighbor = Agents[neighborIndex];
                            if (!neighbor.IsActive) continue;
                            
                            float3 toNeighbor = neighbor.Position - agent.Position;
                            float distSq = math.lengthsq(toNeighbor);
                            
                            // Skip if outside neighbor radius
                            if (distSq > neighborRadiusSq || distSq < 0.0001f) continue;
                            
                            // Separation (within separation radius)
                            if (distSq < separationRadiusSq && distSq > 0.0001f)
                            {
                                float dist = math.sqrt(distSq);
                                float3 awayDir = -toNeighbor / dist;
                                // Inverse distance falloff
                                separationForce += awayDir / distSq;
                                separationCount++;
                            }
                            
                            // Alignment - match velocity of neighbors
                            if (math.lengthsq(neighbor.Velocity) > 0.001f)
                            {
                                avgVelocity += neighbor.Velocity;
                                alignmentCount++;
                            }
                            
                            // Cohesion - move toward center of mass
                            centerOfMass += neighbor.Position;
                            cohesionCount++;
                            
                        } while (SpatialHash.TryGetNextValue(out neighborIndex, ref iterator));
                    }
                }
            }
            
            // Finalize separation force
            if (separationCount > 0)
            {
                separationForce /= separationCount;
                if (math.lengthsq(separationForce) > 0.001f)
                {
                    separationForce = math.normalize(separationForce) * agent.MaxForce;
                }
            }
            
            // Finalize alignment force (steer toward average velocity)
            if (alignmentCount > 0)
            {
                avgVelocity /= alignmentCount;
                if (math.lengthsq(avgVelocity) > 0.001f)
                {
                    float3 desiredVelocity = math.normalize(avgVelocity) * agent.MaxSpeed;
                    alignmentForce = desiredVelocity - agent.Velocity;
                    if (math.lengthsq(alignmentForce) > agent.MaxForce * agent.MaxForce)
                    {
                        alignmentForce = math.normalize(alignmentForce) * agent.MaxForce;
                    }
                }
            }
            
            // Finalize cohesion force (seek toward center of mass)
            if (cohesionCount > 0)
            {
                centerOfMass /= cohesionCount;
                float3 toCenter = centerOfMass - agent.Position;
                if (math.lengthsq(toCenter) > 0.001f)
                {
                    float3 desiredVelocity = math.normalize(toCenter) * agent.MaxSpeed;
                    cohesionForce = desiredVelocity - agent.Velocity;
                    if (math.lengthsq(cohesionForce) > agent.MaxForce * agent.MaxForce)
                    {
                        cohesionForce = math.normalize(cohesionForce) * agent.MaxForce;
                    }
                }
            }
            
            // Combine forces with weights
            float3 totalForce = 
                separationForce * Weights.Separation +
                alignmentForce * Weights.Alignment +
                cohesionForce * Weights.Cohesion;
            
            // Clamp to max force
            if (math.lengthsq(totalForce) > agent.MaxForce * agent.MaxForce)
            {
                totalForce = math.normalize(totalForce) * agent.MaxForce;
            }
            
            Results[index] = new SteeringResult
            {
                SteeringForce = totalForce,
                NeighborCount = cohesionCount
            };
        }
    }
    
    /// <summary>
    /// Job to apply steering forces and update velocities.
    /// </summary>
    [BurstCompile]
    public struct ApplySteeringJob : IJobParallelFor
    {
        /// <summary>Steering results from flocking job.</summary>
        [ReadOnly] public NativeArray<SteeringResult> SteeringResults;
        
        /// <summary>Delta time for integration.</summary>
        public float DeltaTime;
        
        /// <summary>Agent data to update.</summary>
        public NativeArray<AgentData> Agents;
        
        public void Execute(int index)
        {
            AgentData agent = Agents[index];
            if (!agent.IsActive) return;
            
            SteeringResult result = SteeringResults[index];
            
            // Apply steering force (F = ma, a = F/m)
            float3 acceleration = result.SteeringForce / agent.Mass;
            agent.Velocity += acceleration * DeltaTime;
            
            // Clamp to max speed
            float speedSq = math.lengthsq(agent.Velocity);
            if (speedSq > agent.MaxSpeed * agent.MaxSpeed)
            {
                agent.Velocity = math.normalize(agent.Velocity) * agent.MaxSpeed;
            }
            
            // Update position
            if (speedSq > 0.001f)
            {
                agent.Position += agent.Velocity * DeltaTime;
            }
            
            Agents[index] = agent;
        }
    }
}
