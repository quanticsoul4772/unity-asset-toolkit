using UnityEngine;

namespace SwarmAI
{
    /// <summary>
    /// Fleeing state - agent is moving away from a threat.
    /// </summary>
    public class FleeingState : AgentState
    {
        private Vector3 _threatPosition;
        private Transform _threatTransform;
        private bool _hadThreatTransform;
        private float _safeDistance;
        private float _fleeSpeedMultiplier;
        
        /// <summary>
        /// Create a fleeing state away from a fixed position.
        /// </summary>
        /// <param name="threatPosition">Position to flee from.</param>
        /// <param name="safeDistance">Distance at which agent stops fleeing. Use -1 to use SwarmSettings default.</param>
        public FleeingState(Vector3 threatPosition, float safeDistance = -1f)
        {
            Type = AgentStateType.Fleeing;
            _threatPosition = threatPosition;
            _threatTransform = null;
            _hadThreatTransform = false;
            _safeDistance = safeDistance;
            _fleeSpeedMultiplier = -1f; // Will be set from settings in Enter()
        }
        
        /// <summary>
        /// Create a fleeing state away from a moving threat.
        /// </summary>
        /// <param name="threatTransform">Transform to flee from.</param>
        /// <param name="safeDistance">Distance at which agent stops fleeing. Use -1 to use SwarmSettings default.</param>
        public FleeingState(Transform threatTransform, float safeDistance = -1f)
        {
            Type = AgentStateType.Fleeing;
            _threatTransform = threatTransform;
            _hadThreatTransform = threatTransform != null;
            _threatPosition = threatTransform != null ? threatTransform.position : Vector3.zero;
            _safeDistance = safeDistance;
            _fleeSpeedMultiplier = -1f; // Will be set from settings in Enter()
        }
        
        public override void Enter()
        {
            base.Enter();
            
            // Get defaults from SwarmSettings if not specified
            var settings = SwarmManager.Instance?.Settings;
            if (_safeDistance < 0f)
            {
                _safeDistance = settings != null ? settings.DefaultFleeDistance : 10f;
            }
            if (_fleeSpeedMultiplier < 0f)
            {
                _fleeSpeedMultiplier = settings != null ? settings.FleeSpeedMultiplier : 1.5f;
            }
            
            UpdateFleeTarget();
        }
        
        public override void Execute()
        {
            // Update threat position if following a transform
            if (_threatTransform != null)
            {
                _threatPosition = _threatTransform.position;
            }
            
            UpdateFleeTarget();
        }
        
        private void UpdateFleeTarget()
        {
            // Calculate flee direction (away from threat)
            Vector3 toAgent = Agent.Position - _threatPosition;
            Vector3 fleeDirection;
            
            // Handle case where agent is at exact threat position
            if (toAgent.sqrMagnitude < 0.001f)
            {
                fleeDirection = Vector3.forward;
            }
            else
            {
                fleeDirection = toAgent.normalized;
            }
            
            // Set target position away from threat
            Vector3 fleeTarget = Agent.Position + fleeDirection * _safeDistance;
            Agent.SetTarget(fleeTarget);
            
            // Apply flee force for faster movement
            Agent.ApplyForce(fleeDirection * Agent.MaxForce * _fleeSpeedMultiplier);
        }
        
        public override AgentState CheckTransitions()
        {
            // Check if threat transform was destroyed (Unity's == null check detects destroyed objects)
            if (_hadThreatTransform && _threatTransform == null)
            {
                return new IdleState();
            }
            
            // Check if at safe distance
            float distance = Vector3.Distance(Agent.Position, _threatPosition);
            if (distance >= _safeDistance)
            {
                return new IdleState();
            }
            
            return this;
        }
        
        public override void Exit()
        {
            base.Exit();
            Agent.ClearTarget();
        }
        
        public override bool HandleMessage(SwarmMessage message)
        {
            switch (message.Type)
            {
                case SwarmMessageType.Flee:
                    // Update threat position
                    _threatPosition = message.Position;
                    _threatTransform = null;
                    _hadThreatTransform = false;
                    return true;
                    
                case SwarmMessageType.Stop:
                    Agent.SetState(new IdleState());
                    return true;
            }
            
            return false;
        }
    }
}
