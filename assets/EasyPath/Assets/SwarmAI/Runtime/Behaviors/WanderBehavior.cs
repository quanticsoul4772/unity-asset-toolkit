using UnityEngine;

namespace SwarmAI
{
    /// <summary>
    /// Steering behavior that produces smooth random movement.
    /// Projects a circle in front of the agent and picks a random point on it.
    /// </summary>
    public class WanderBehavior : BehaviorBase
    {
        private float _wanderRadius;
        private float _wanderDistance;
        private float _wanderJitter;
        private Vector3 _wanderTarget;
        
        /// <inheritdoc/>
        public override string Name => "Wander";
        
        /// <summary>
        /// Radius of the wander circle.
        /// </summary>
        public float WanderRadius
        {
            get => _wanderRadius;
            set => _wanderRadius = Mathf.Max(0.1f, value);
        }
        
        /// <summary>
        /// Distance of the wander circle from the agent.
        /// </summary>
        public float WanderDistance
        {
            get => _wanderDistance;
            set => _wanderDistance = Mathf.Max(0.1f, value);
        }
        
        /// <summary>
        /// Maximum random displacement per frame.
        /// Higher values = more erratic movement.
        /// </summary>
        public float WanderJitter
        {
            get => _wanderJitter;
            set => _wanderJitter = Mathf.Max(0f, value);
        }
        
        /// <summary>
        /// Create a wander behavior with default settings.
        /// </summary>
        public WanderBehavior()
        {
            _wanderRadius = 4f;
            _wanderDistance = 6f;
            _wanderJitter = 1f;
            _wanderTarget = Vector3.forward * _wanderRadius;
        }
        
        /// <summary>
        /// Create a wander behavior with custom settings.
        /// </summary>
        /// <param name="wanderRadius">Radius of the wander circle.</param>
        /// <param name="wanderDistance">Distance of the wander circle from the agent.</param>
        /// <param name="wanderJitter">Maximum random displacement per frame.</param>
        public WanderBehavior(float wanderRadius, float wanderDistance, float wanderJitter)
        {
            _wanderRadius = Mathf.Max(0.1f, wanderRadius);
            _wanderDistance = Mathf.Max(0.1f, wanderDistance);
            _wanderJitter = Mathf.Max(0f, wanderJitter);
            _wanderTarget = Vector3.forward * _wanderRadius;
        }
        
        /// <inheritdoc/>
        public override Vector3 CalculateForce(SwarmAgent agent)
        {
            if (agent == null) return Vector3.zero;
            
            // Add random jitter to the wander target
            _wanderTarget += new Vector3(
                RandomBinomial() * _wanderJitter,
                0f,
                RandomBinomial() * _wanderJitter
            );
            
            // Re-project onto the circle (avoid zero vector normalization)
            if (_wanderTarget.sqrMagnitude > SwarmSettings.DefaultPositionEqualityThresholdSq)
            {
                _wanderTarget = _wanderTarget.normalized * _wanderRadius;
            }
            else
            {
                _wanderTarget = Vector3.forward * _wanderRadius;
            }
            
            // Calculate the world position of the wander target
            Vector3 localTarget = _wanderTarget + Vector3.forward * _wanderDistance;
            
            // Transform to world space based on agent's forward direction
            Vector3 worldTarget;
            if (agent.Velocity.sqrMagnitude > 0.001f)
            {
                // Use velocity direction
                Quaternion rotation = Quaternion.LookRotation(agent.Velocity.normalized);
                worldTarget = agent.Position + rotation * localTarget;
            }
            else
            {
                // Use agent's forward direction
                worldTarget = agent.Position + agent.Forward * _wanderDistance + _wanderTarget;
            }
            
            // Seek the wander target
            return Seek(agent, worldTarget);
        }
        
        /// <summary>
        /// Returns a random value between -1 and 1 with normal distribution.
        /// </summary>
        private float RandomBinomial()
        {
            return Random.value - Random.value;
        }
    }
}
