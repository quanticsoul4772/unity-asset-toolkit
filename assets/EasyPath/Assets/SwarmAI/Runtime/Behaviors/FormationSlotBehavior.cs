using UnityEngine;

namespace SwarmAI
{
    /// <summary>
    /// Steering behavior for formation followers that moves toward the agent's target position
    /// (set by SwarmFormation) with arrival damping for stable formations.
    /// </summary>
    public class FormationSlotBehavior : BehaviorBase
    {
        private float _slowingRadius;
        private float _arrivalRadius;
        private float _dampingFactor;
        
        /// <inheritdoc/>
        public override string Name => "Formation Slot";
        
        /// <summary>
        /// Distance at which the agent starts slowing down.
        /// </summary>
        public float SlowingRadius
        {
            get => _slowingRadius;
            set => _slowingRadius = Mathf.Max(_arrivalRadius, Mathf.Max(0.1f, value));
        }
        
        /// <summary>
        /// Distance at which the agent is considered "arrived" and stops moving.
        /// </summary>
        public float ArrivalRadius
        {
            get => _arrivalRadius;
            set
            {
                _arrivalRadius = Mathf.Max(0f, value);
                if (_slowingRadius < _arrivalRadius)
                {
                    _slowingRadius = _arrivalRadius;
                }
            }
        }
        
        /// <summary>
        /// Damping factor applied when close to target (0-1). Higher = more damping.
        /// </summary>
        public float DampingFactor
        {
            get => _dampingFactor;
            set => _dampingFactor = Mathf.Clamp01(value);
        }
        
        /// <summary>
        /// Create a formation slot behavior with default settings.
        /// </summary>
        public FormationSlotBehavior()
        {
            _slowingRadius = 3f;
            _arrivalRadius = 0.8f; // Larger arrival radius for stability
            _dampingFactor = 0.5f;
        }
        
        /// <summary>
        /// Create a formation slot behavior with custom settings.
        /// </summary>
        /// <param name="slowingRadius">Distance at which to start slowing. Default: 3.</param>
        /// <param name="arrivalRadius">Distance at which agent stops. Default: 0.8.</param>
        /// <param name="dampingFactor">Velocity damping near target (0-1). Default: 0.5.</param>
        public FormationSlotBehavior(float slowingRadius, float arrivalRadius, float dampingFactor = 0.5f)
        {
            _arrivalRadius = Mathf.Max(0f, arrivalRadius);
            _slowingRadius = Mathf.Max(_arrivalRadius, Mathf.Max(0.1f, slowingRadius));
            _dampingFactor = Mathf.Clamp01(dampingFactor);
        }
        
        /// <inheritdoc/>
        public override Vector3 CalculateForce(SwarmAgent agent)
        {
            if (agent == null) return Vector3.zero;
            
            // Read target from agent (set by SwarmFormation.UpdateSlotPositions)
            Vector3 targetPosition = agent.TargetPosition;
            
            Vector3 toTarget = targetPosition - agent.Position;
            float distance = toTarget.magnitude;
            
            // Already arrived - apply strong braking to stop completely
            if (distance < _arrivalRadius)
            {
                // Full brake - return opposite of current velocity to stop quickly
                return -agent.Velocity;
            }
            
            // Avoid division by zero
            if (distance < 0.001f)
            {
                return -agent.Velocity * _dampingFactor;
            }
            
            // Calculate desired speed based on distance
            float targetSpeed;
            if (distance < _slowingRadius)
            {
                // Smoothly decelerate as we approach
                float t = distance / _slowingRadius;
                targetSpeed = agent.MaxSpeed * t * t; // Quadratic falloff for smoother stop
            }
            else
            {
                targetSpeed = agent.MaxSpeed;
            }
            
            // Calculate desired velocity
            Vector3 desiredVelocity = (toTarget / distance) * targetSpeed;
            
            // Return steering force with damping when close
            Vector3 steering = desiredVelocity - agent.Velocity;
            
            // Apply extra damping when within slowing radius to reduce oscillation
            if (distance < _slowingRadius)
            {
                steering -= agent.Velocity * _dampingFactor * (1f - distance / _slowingRadius);
            }
            
            return steering;
        }
    }
}
