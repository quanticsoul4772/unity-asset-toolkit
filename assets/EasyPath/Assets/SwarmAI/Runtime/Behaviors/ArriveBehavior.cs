using UnityEngine;

namespace SwarmAI
{
    /// <summary>
    /// Steering behavior that moves toward a target and decelerates smoothly on approach.
    /// Similar to Seek but slows down as it approaches the target.
    /// </summary>
    public class ArriveBehavior : BehaviorBase
    {
        private Vector3 _targetPosition;
        private Transform _targetTransform;
        private float _slowingRadius;
        private float _arrivalRadius;
        
        /// <inheritdoc/>
        public override string Name => "Arrive";
        
        /// <summary>
        /// The target position to arrive at.
        /// </summary>
        public Vector3 TargetPosition
        {
            get => _targetTransform != null ? _targetTransform.position : _targetPosition;
            set
            {
                _targetPosition = value;
                _targetTransform = null;
            }
        }
        
        /// <summary>
        /// Optional transform to follow (updates position automatically).
        /// </summary>
        public Transform TargetTransform
        {
            get => _targetTransform;
            set => _targetTransform = value;
        }
        
        /// <summary>
        /// Distance at which the agent starts slowing down.
        /// Must be greater than or equal to <see cref="ArrivalRadius"/>.
        /// </summary>
        public float SlowingRadius
        {
            get => _slowingRadius;
            set => _slowingRadius = Mathf.Max(_arrivalRadius, Mathf.Max(0.1f, value));
        }
        
        /// <summary>
        /// Distance at which the agent is considered "arrived" (returns zero force).
        /// Will automatically adjust <see cref="SlowingRadius"/> if needed.
        /// </summary>
        public float ArrivalRadius
        {
            get => _arrivalRadius;
            set
            {
                _arrivalRadius = Mathf.Max(0f, value);
                // Ensure slowing radius is always >= arrival radius
                if (_slowingRadius < _arrivalRadius)
                {
                    _slowingRadius = _arrivalRadius;
                }
            }
        }
        
        /// <summary>
        /// Create an arrive behavior with default settings.
        /// </summary>
        public ArriveBehavior()
        {
            _targetPosition = Vector3.zero;
            _targetTransform = null;
            _slowingRadius = 5f;
            _arrivalRadius = 0.5f;
        }
        
        /// <summary>
        /// Create an arrive behavior targeting a position.
        /// </summary>
        /// <param name="targetPosition">Position to arrive at.</param>
        /// <param name="slowingRadius">Distance at which to start slowing. Default: 5.</param>
        /// <param name="arrivalRadius">Distance at which agent is considered arrived. Default: 0.5.</param>
        public ArriveBehavior(Vector3 targetPosition, float slowingRadius = 5f, float arrivalRadius = 0.5f)
        {
            _targetPosition = targetPosition;
            _targetTransform = null;
            _arrivalRadius = Mathf.Max(0f, arrivalRadius);
            _slowingRadius = Mathf.Max(Mathf.Max(0.1f, _arrivalRadius), slowingRadius);
        }
        
        /// <summary>
        /// Create an arrive behavior targeting a transform.
        /// </summary>
        /// <param name="targetTransform">Transform to arrive at.</param>
        /// <param name="slowingRadius">Distance at which to start slowing. Default: 5.</param>
        /// <param name="arrivalRadius">Distance at which agent is considered arrived. Default: 0.5.</param>
        public ArriveBehavior(Transform targetTransform, float slowingRadius = 5f, float arrivalRadius = 0.5f)
        {
            _targetTransform = targetTransform;
            _targetPosition = targetTransform != null ? targetTransform.position : Vector3.zero;
            _arrivalRadius = Mathf.Max(0f, arrivalRadius);
            _slowingRadius = Mathf.Max(Mathf.Max(0.1f, _arrivalRadius), slowingRadius);
        }
        
        /// <inheritdoc/>
        public override Vector3 CalculateForce(SwarmAgent agent)
        {
            if (agent == null) return Vector3.zero;
            
            Vector3 toTarget = TargetPosition - agent.Position;
            float distance = toTarget.magnitude;
            
            // Already arrived
            if (distance < _arrivalRadius)
            {
                return Vector3.zero;
            }
            
            // Calculate desired speed based on distance
            float targetSpeed;
            if (distance < _slowingRadius)
            {
                // Decelerate as we approach
                targetSpeed = agent.MaxSpeed * (distance / _slowingRadius);
            }
            else
            {
                // Full speed
                targetSpeed = agent.MaxSpeed;
            }
            
            // Calculate desired velocity
            Vector3 desiredVelocity = toTarget.normalized * targetSpeed;
            
            // Return steering force
            return desiredVelocity - agent.Velocity;
        }
    }
}
