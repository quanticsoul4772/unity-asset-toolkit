using UnityEngine;

namespace SwarmAI
{
    /// <summary>
    /// Flocking behavior that steers toward the center of mass of nearby neighbors.
    /// Part of the classic "Boids" algorithm.
    /// </summary>
    public class CohesionBehavior : BehaviorBase
    {
        private float _cohesionRadius;
        
        /// <inheritdoc/>
        public override string Name => "Cohesion";
        
        /// <summary>
        /// Radius within which to consider neighbors for cohesion.
        /// If 0, uses the agent's NeighborRadius.
        /// </summary>
        public float CohesionRadius
        {
            get => _cohesionRadius;
            set => _cohesionRadius = Mathf.Max(0f, value);
        }
        
        /// <summary>
        /// Create a cohesion behavior with default settings.
        /// </summary>
        public CohesionBehavior()
        {
            _cohesionRadius = 0f; // Use agent's neighbor radius
        }
        
        /// <summary>
        /// Create a cohesion behavior with custom radius.
        /// </summary>
        /// <param name="cohesionRadius">Radius for cohesion. 0 = use agent's neighbor radius.</param>
        public CohesionBehavior(float cohesionRadius)
        {
            _cohesionRadius = Mathf.Max(0f, cohesionRadius);
        }
        
        /// <inheritdoc/>
        public override Vector3 CalculateForce(SwarmAgent agent)
        {
            if (agent == null) return Vector3.zero;
            
            var neighbors = agent.GetNeighbors();
            if (neighbors.Count == 0) return Vector3.zero;
            
            float effectiveRadius = _cohesionRadius > 0f ? _cohesionRadius : agent.NeighborRadius;
            float radiusSq = effectiveRadius * effectiveRadius;
            
            Vector3 centerOfMass = Vector3.zero;
            int count = 0;
            
            foreach (var neighbor in neighbors)
            {
                if (neighbor == null || neighbor == agent) continue;
                
                // Check if within cohesion radius (using squared distance for performance)
                Vector3 toNeighbor = neighbor.Position - agent.Position;
                float distanceSq = toNeighbor.sqrMagnitude;
                if (distanceSq > radiusSq) continue;
                
                centerOfMass += neighbor.Position;
                count++;
            }
            
            if (count == 0) return Vector3.zero;
            
            // Calculate center of mass
            centerOfMass /= count;
            
            // Seek toward center of mass
            return Seek(agent, centerOfMass);
        }
    }
}
