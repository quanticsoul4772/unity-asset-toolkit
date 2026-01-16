using UnityEngine;

namespace SwarmAI
{
    /// <summary>
    /// Steering behavior that follows a leader agent while maintaining distance.
    /// Useful for group coordination and escort behavior.
    /// </summary>
    public class FollowLeaderBehavior : BehaviorBase
    {
        private SwarmAgent _leader;
        private float _followDistance;
        private float _slowingRadius;
        private Vector3 _offsetFromLeader;
        private bool _useOffset;
        
        /// <inheritdoc/>
        public override string Name => "Follow Leader";
        
        /// <summary>
        /// The leader agent to follow.
        /// </summary>
        public SwarmAgent Leader
        {
            get => _leader;
            set => _leader = value;
        }
        
        /// <summary>
        /// Desired distance behind the leader.
        /// </summary>
        public float FollowDistance
        {
            get => _followDistance;
            set => _followDistance = Mathf.Max(0.5f, value);
        }
        
        /// <summary>
        /// Distance at which the follower starts slowing down.
        /// </summary>
        public float SlowingRadius
        {
            get => _slowingRadius;
            set => _slowingRadius = Mathf.Max(0.1f, value);
        }
        
        /// <summary>
        /// Optional fixed offset from leader instead of behind.
        /// </summary>
        public Vector3 OffsetFromLeader
        {
            get => _offsetFromLeader;
            set
            {
                _offsetFromLeader = value;
                _useOffset = true;
            }
        }
        
        /// <summary>
        /// Whether to use a fixed offset or follow behind.
        /// </summary>
        public bool UseOffset
        {
            get => _useOffset;
            set => _useOffset = value;
        }
        
        /// <summary>
        /// Create a follow leader behavior with default settings.
        /// </summary>
        public FollowLeaderBehavior()
        {
            _followDistance = 3f;
            _slowingRadius = 5f;
            _offsetFromLeader = Vector3.zero;
            _useOffset = false;
        }
        
        /// <summary>
        /// Create a follow leader behavior targeting a specific leader.
        /// </summary>
        /// <param name="leader">The agent to follow.</param>
        /// <param name="followDistance">Distance behind the leader.</param>
        /// <param name="slowingRadius">Distance at which to start slowing.</param>
        public FollowLeaderBehavior(SwarmAgent leader, float followDistance = 3f, float slowingRadius = 5f)
        {
            _leader = leader;
            _followDistance = Mathf.Max(0.5f, followDistance);
            _slowingRadius = Mathf.Max(0.1f, slowingRadius);
            _offsetFromLeader = Vector3.zero;
            _useOffset = false;
        }
        
        /// <summary>
        /// Create a follow leader behavior with a fixed offset.
        /// </summary>
        /// <param name="leader">The agent to follow.</param>
        /// <param name="offset">Fixed offset from leader in leader's local space.</param>
        /// <param name="slowingRadius">Distance at which to start slowing.</param>
        public FollowLeaderBehavior(SwarmAgent leader, Vector3 offset, float slowingRadius = 5f)
        {
            _leader = leader;
            _followDistance = offset.magnitude;
            _slowingRadius = Mathf.Max(0.1f, slowingRadius);
            _offsetFromLeader = offset;
            _useOffset = true;
        }
        
        /// <inheritdoc/>
        public override Vector3 CalculateForce(SwarmAgent agent)
        {
            if (agent == null || _leader == null) return Vector3.zero;
            
            // Don't follow yourself
            if (agent == _leader) return Vector3.zero;
            
            // Calculate target position
            Vector3 targetPosition = CalculateTargetPosition();
            
            // Calculate distance to target using sqrMagnitude for efficiency
            Vector3 toTarget = targetPosition - agent.Position;
            float sqrDistance = toTarget.sqrMagnitude;
            
            // Already at target
            if (sqrDistance < SwarmSettings.DefaultPositionEqualityThresholdSq)
            {
                return Vector3.zero;
            }
            
            float distance = Mathf.Sqrt(sqrDistance);
            
            // Calculate desired speed based on distance (arrive behavior)
            float targetSpeed;
            if (distance < _slowingRadius)
            {
                targetSpeed = agent.MaxSpeed * (distance / _slowingRadius);
            }
            else
            {
                targetSpeed = agent.MaxSpeed;
            }
            
            // Calculate desired velocity
            Vector3 desiredVelocity = toTarget / distance * targetSpeed; // Normalize manually since we have distance
            
            // Steering = desired - current
            return desiredVelocity - agent.Velocity;
        }
        
        /// <summary>
        /// Calculate the position to move toward.
        /// </summary>
        private Vector3 CalculateTargetPosition()
        {
            if (_useOffset)
            {
                // Use fixed offset in leader's local space
                return _leader.Position + _leader.transform.rotation * _offsetFromLeader;
            }
            else
            {
                // Follow behind the leader based on their velocity/facing
                Vector3 leaderDirection;
                if (_leader.Velocity.sqrMagnitude > SwarmSettings.DefaultVelocityThresholdSq)
                {
                    leaderDirection = _leader.Velocity.normalized;
                }
                else
                {
                    leaderDirection = _leader.Forward;
                }
                
                // Position behind leader
                return _leader.Position - leaderDirection * _followDistance;
            }
        }
        
        /// <summary>
        /// Set the leader by agent ID (looks up from SwarmManager).
        /// </summary>
        public void SetLeaderById(int leaderId)
        {
            if (SwarmManager.HasInstance)
            {
                _leader = SwarmManager.Instance.GetAgent(leaderId);
            }
        }
    }
}
