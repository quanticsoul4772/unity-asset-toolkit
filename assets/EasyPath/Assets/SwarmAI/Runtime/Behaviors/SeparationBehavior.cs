using UnityEngine;

namespace SwarmAI
{
    /// <summary>
    /// Flocking behavior that steers away from nearby neighbors to avoid crowding.
    /// Part of the classic "Boids" algorithm.
    /// </summary>
    public class SeparationBehavior : BehaviorBase
    {
        private float _separationRadius;
        private bool _useSquaredFalloff;
        
        /// <inheritdoc/>
        public override string Name => "Separation";
        
        /// <summary>
        /// Radius within which to consider neighbors for separation.
        /// If 0, uses the agent's NeighborRadius.
        /// </summary>
        public float SeparationRadius
        {
            get => _separationRadius;
            set => _separationRadius = Mathf.Max(0f, value);
        }
        
        /// <summary>
        /// If true, separation force falls off with squared distance (more realistic).
        /// If false, uses linear falloff (smoother but less natural).
        /// </summary>
        public bool UseSquaredFalloff
        {
            get => _useSquaredFalloff;
            set => _useSquaredFalloff = value;
        }
        
        /// <summary>
        /// Create a separation behavior with default settings.
        /// </summary>
        public SeparationBehavior()
        {
            _separationRadius = 0f; // Use agent's neighbor radius
            _useSquaredFalloff = true;
        }
        
        /// <summary>
        /// Create a separation behavior with custom radius.
        /// </summary>
        /// <param name="separationRadius">Radius for separation. 0 = use agent's neighbor radius.</param>
        /// <param name="useSquaredFalloff">Use squared distance falloff for force.</param>
        public SeparationBehavior(float separationRadius, bool useSquaredFalloff = true)
        {
            _separationRadius = Mathf.Max(0f, separationRadius);
            _useSquaredFalloff = useSquaredFalloff;
        }
        
        /// <inheritdoc/>
        public override Vector3 CalculateForce(SwarmAgent agent)
        {
            if (agent == null) return Vector3.zero;
            
            var neighbors = agent.GetNeighbors();
            if (neighbors.Count == 0) return Vector3.zero;
            
            float effectiveRadius = _separationRadius > 0f ? _separationRadius : agent.NeighborRadius;
            float radiusSq = effectiveRadius * effectiveRadius;
            
            Vector3 separationForce = Vector3.zero;
            int count = 0;
            
            foreach (var neighbor in neighbors)
            {
                if (neighbor == null || neighbor == agent) continue;
                
                Vector3 toAgent = agent.Position - neighbor.Position;
                float distanceSq = toAgent.sqrMagnitude;
                
                // Skip if outside separation radius
                if (distanceSq > radiusSq || distanceSq < SwarmSettings.DefaultPositionEqualityThresholdSq)
                {
                    continue;
                }
                
                // Calculate repulsion force
                float distance = Mathf.Sqrt(distanceSq);
                Vector3 direction = toAgent / distance; // Normalized
                
                // Apply distance falloff
                float strength;
                if (_useSquaredFalloff)
                {
                    // Inverse square falloff (stronger when closer)
                    strength = 1f / distanceSq;
                }
                else
                {
                    // Linear falloff
                    strength = 1f - (distance / effectiveRadius);
                }
                
                separationForce += direction * strength;
                count++;
            }
            
            if (count == 0) return Vector3.zero;
            
            // Average and scale to max force
            separationForce /= count;
            
            if (separationForce.sqrMagnitude > SwarmSettings.DefaultVelocityThresholdSq)
            {
                separationForce = separationForce.normalized * agent.MaxForce;
            }
            
            return separationForce;
        }
    }
}
