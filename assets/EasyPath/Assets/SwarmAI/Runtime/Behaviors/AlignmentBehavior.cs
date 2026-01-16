using UnityEngine;

namespace SwarmAI
{
    /// <summary>
    /// Flocking behavior that steers to match the average velocity of nearby neighbors.
    /// Part of the classic "Boids" algorithm.
    /// </summary>
    public class AlignmentBehavior : BehaviorBase
    {
        private float _alignmentRadius;
        
        /// <inheritdoc/>
        public override string Name => "Alignment";
        
        /// <summary>
        /// Radius within which to consider neighbors for alignment.
        /// If 0, uses the agent's NeighborRadius.
        /// </summary>
        public float AlignmentRadius
        {
            get => _alignmentRadius;
            set => _alignmentRadius = Mathf.Max(0f, value);
        }
        
        /// <summary>
        /// Create an alignment behavior with default settings.
        /// </summary>
        public AlignmentBehavior()
        {
            _alignmentRadius = 0f; // Use agent's neighbor radius
        }
        
        /// <summary>
        /// Create an alignment behavior with custom radius.
        /// </summary>
        /// <param name="alignmentRadius">Radius for alignment. 0 = use agent's neighbor radius.</param>
        public AlignmentBehavior(float alignmentRadius)
        {
            _alignmentRadius = Mathf.Max(0f, alignmentRadius);
        }
        
        /// <inheritdoc/>
        public override Vector3 CalculateForce(SwarmAgent agent)
        {
            if (agent == null) return Vector3.zero;
            
            var neighbors = agent.GetNeighbors();
            if (neighbors.Count == 0) return Vector3.zero;
            
            float effectiveRadius = _alignmentRadius > 0f ? _alignmentRadius : agent.NeighborRadius;
            float radiusSq = effectiveRadius * effectiveRadius;
            
            Vector3 averageVelocity = Vector3.zero;
            int count = 0;
            
            foreach (var neighbor in neighbors)
            {
                if (neighbor == null || neighbor == agent) continue;
                
                // Check if within alignment radius
                float distanceSq = (neighbor.Position - agent.Position).sqrMagnitude;
                if (distanceSq > radiusSq) continue;
                
                // Only consider moving neighbors
                if (neighbor.Velocity.sqrMagnitude > SwarmSettings.DefaultVelocityThresholdSq)
                {
                    averageVelocity += neighbor.Velocity;
                    count++;
                }
            }
            
            if (count == 0) return Vector3.zero;
            
            // Calculate average velocity
            averageVelocity /= count;
            
            // Return steering force to match average velocity
            if (averageVelocity.sqrMagnitude > SwarmSettings.DefaultVelocityThresholdSq)
            {
                // Scale to desired speed
                Vector3 desiredVelocity = averageVelocity.normalized * agent.MaxSpeed;
                return desiredVelocity - agent.Velocity;
            }
            
            return Vector3.zero;
        }
    }
}
