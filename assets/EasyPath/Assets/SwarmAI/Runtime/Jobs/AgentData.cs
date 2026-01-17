using Unity.Mathematics;

namespace SwarmAI.Jobs
{
    /// <summary>
    /// Blittable agent data structure for use in Jobs.
    /// Contains all data needed for steering calculations.
    /// </summary>
    public struct AgentData
    {
        /// <summary>Agent's unique identifier.</summary>
        public int AgentId;
        
        /// <summary>Current world position.</summary>
        public float3 Position;
        
        /// <summary>Current velocity vector.</summary>
        public float3 Velocity;
        
        /// <summary>Maximum movement speed.</summary>
        public float MaxSpeed;
        
        /// <summary>Maximum steering force.</summary>
        public float MaxForce;
        
        /// <summary>Neighbor detection radius.</summary>
        public float NeighborRadius;
        
        /// <summary>Agent mass for physics calculations.</summary>
        public float Mass;
        
        /// <summary>Whether this agent slot is active.</summary>
        public bool IsActive;
        
        /// <summary>
        /// Create agent data from position and velocity.
        /// </summary>
        public static AgentData Create(int id, float3 position, float3 velocity, 
            float maxSpeed, float maxForce, float neighborRadius, float mass)
        {
            return new AgentData
            {
                AgentId = id,
                Position = position,
                Velocity = velocity,
                MaxSpeed = maxSpeed,
                MaxForce = maxForce,
                NeighborRadius = neighborRadius,
                Mass = mass,
                IsActive = true
            };
        }
    }
    
    /// <summary>
    /// Behavior weights for steering calculation.
    /// </summary>
    public struct BehaviorWeights
    {
        public float Separation;
        public float Alignment;
        public float Cohesion;
        public float SeparationRadius;
        
        public static BehaviorWeights Default => new BehaviorWeights
        {
            Separation = 1.5f,
            Alignment = 1.0f,
            Cohesion = 1.0f,
            SeparationRadius = 2.5f
        };
    }
    
    /// <summary>
    /// Result of steering calculation for an agent.
    /// </summary>
    public struct SteeringResult
    {
        /// <summary>Calculated steering force.</summary>
        public float3 SteeringForce;
        
        /// <summary>Number of neighbors found.</summary>
        public int NeighborCount;
    }
}
