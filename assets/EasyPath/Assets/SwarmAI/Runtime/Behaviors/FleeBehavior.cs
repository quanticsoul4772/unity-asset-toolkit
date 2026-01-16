using UnityEngine;

namespace SwarmAI
{
    /// <summary>
    /// Steering behavior that moves the agent away from a threat position.
    /// </summary>
    public class FleeBehavior : BehaviorBase
    {
        private Vector3 _threatPosition;
        private Transform _threatTransform;
        private float _panicDistance;
        
        /// <inheritdoc/>
        public override string Name => "Flee";
        
        /// <summary>
        /// The position to flee from.
        /// </summary>
        public Vector3 ThreatPosition
        {
            get => _threatTransform != null ? _threatTransform.position : _threatPosition;
            set
            {
                _threatPosition = value;
                _threatTransform = null;
            }
        }
        
        /// <summary>
        /// Optional transform to flee from (updates position automatically).
        /// </summary>
        public Transform ThreatTransform
        {
            get => _threatTransform;
            set => _threatTransform = value;
        }
        
        /// <summary>
        /// Distance at which the agent starts fleeing. 0 = always flee.
        /// </summary>
        public float PanicDistance
        {
            get => _panicDistance;
            set => _panicDistance = Mathf.Max(0f, value);
        }
        
        /// <summary>
        /// Create a flee behavior with no initial threat.
        /// </summary>
        public FleeBehavior()
        {
            _threatPosition = Vector3.zero;
            _threatTransform = null;
            _panicDistance = 0f;
        }
        
        /// <summary>
        /// Create a flee behavior from a position.
        /// </summary>
        /// <param name="threatPosition">Position to flee from.</param>
        /// <param name="panicDistance">Distance at which to start fleeing. 0 = always flee.</param>
        public FleeBehavior(Vector3 threatPosition, float panicDistance = 0f)
        {
            _threatPosition = threatPosition;
            _threatTransform = null;
            _panicDistance = Mathf.Max(0f, panicDistance);
        }
        
        /// <summary>
        /// Create a flee behavior from a transform.
        /// </summary>
        /// <param name="threatTransform">Transform to flee from.</param>
        /// <param name="panicDistance">Distance at which to start fleeing. 0 = always flee.</param>
        public FleeBehavior(Transform threatTransform, float panicDistance = 0f)
        {
            _threatTransform = threatTransform;
            _threatPosition = threatTransform != null ? threatTransform.position : Vector3.zero;
            _panicDistance = Mathf.Max(0f, panicDistance);
        }
        
        /// <inheritdoc/>
        public override Vector3 CalculateForce(SwarmAgent agent)
        {
            if (agent == null) return Vector3.zero;
            
            Vector3 threat = ThreatPosition;
            
            // Check if within panic distance
            if (_panicDistance > 0f)
            {
                float distanceSq = (agent.Position - threat).sqrMagnitude;
                if (distanceSq > _panicDistance * _panicDistance)
                {
                    return Vector3.zero;
                }
            }
            
            return Flee(agent, threat);
        }
    }
}
