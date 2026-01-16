using UnityEngine;

namespace SwarmAI
{
    /// <summary>
    /// Seeking state - agent is actively seeking a target position.
    /// Similar to MovingState but continuously updates target.
    /// </summary>
    public class SeekingState : AgentState
    {
        private Vector3 _targetPosition;
        private Transform _targetTransform;
        private bool _hadTargetTransform;
        
        /// <summary>
        /// Create a seeking state toward a fixed position.
        /// </summary>
        public SeekingState(Vector3 targetPosition)
        {
            Type = AgentStateType.Seeking;
            _targetPosition = targetPosition;
            _targetTransform = null;
        }
        
        /// <summary>
        /// Create a seeking state toward a moving transform.
        /// </summary>
        public SeekingState(Transform targetTransform)
        {
            Type = AgentStateType.Seeking;
            _targetTransform = targetTransform;
            _hadTargetTransform = targetTransform != null;
            _targetPosition = targetTransform != null ? targetTransform.position : Vector3.zero;
        }
        
        public override void Enter()
        {
            base.Enter();
            UpdateTarget();
        }
        
        public override void Execute()
        {
            // Update target if following a transform
            if (_targetTransform != null)
            {
                _targetPosition = _targetTransform.position;
            }
            
            UpdateTarget();
        }
        
        private void UpdateTarget()
        {
            Agent.SetTarget(_targetPosition);
        }
        
        public override AgentState CheckTransitions()
        {
            // Check if target transform was destroyed (Unity's == null check detects destroyed objects)
            if (_hadTargetTransform && _targetTransform == null)
            {
                return new IdleState();
            }
            
            // Check if arrived at target - use Agent.StoppingDistance for consistency
            float distance = Vector3.Distance(Agent.Position, _targetPosition);
            if (distance <= Agent.StoppingDistance)
            {
                return new IdleState();
            }
            
            return this;
        }
        
        public override void Exit()
        {
            base.Exit();
        }
        
        public override bool HandleMessage(SwarmMessage message)
        {
            switch (message.Type)
            {
                case SwarmMessageType.Seek:
                    // Update target
                    _targetPosition = message.Position;
                    _targetTransform = null;
                    _hadTargetTransform = false;
                    return true;
                    
                case SwarmMessageType.Stop:
                    Agent.SetState(new IdleState());
                    return true;
                    
                case SwarmMessageType.Flee:
                    Agent.SetState(new FleeingState(message.Position));
                    return true;
            }
            
            return false;
        }
    }
}
