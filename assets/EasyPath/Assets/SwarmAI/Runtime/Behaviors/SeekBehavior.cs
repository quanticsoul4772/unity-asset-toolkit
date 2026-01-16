using UnityEngine;

namespace SwarmAI
{
    /// <summary>
    /// Steering behavior that moves the agent toward a target position.
    /// </summary>
    public class SeekBehavior : BehaviorBase
    {
        private Vector3 _targetPosition;
        private Transform _targetTransform;
        
        /// <inheritdoc/>
        public override string Name => "Seek";
        
        /// <summary>
        /// The target position to seek.
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
        /// Create a seek behavior with no initial target.
        /// </summary>
        public SeekBehavior()
        {
            _targetPosition = Vector3.zero;
            _targetTransform = null;
        }
        
        /// <summary>
        /// Create a seek behavior targeting a position.
        /// </summary>
        public SeekBehavior(Vector3 targetPosition)
        {
            _targetPosition = targetPosition;
            _targetTransform = null;
        }
        
        /// <summary>
        /// Create a seek behavior targeting a transform.
        /// </summary>
        public SeekBehavior(Transform targetTransform)
        {
            _targetTransform = targetTransform;
            _targetPosition = targetTransform != null ? targetTransform.position : Vector3.zero;
        }
        
        /// <inheritdoc/>
        public override Vector3 CalculateForce(SwarmAgent agent)
        {
            if (agent == null) return Vector3.zero;
            
            return Seek(agent, TargetPosition);
        }
    }
}
