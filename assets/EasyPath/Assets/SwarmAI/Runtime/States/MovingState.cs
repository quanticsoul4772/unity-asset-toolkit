using UnityEngine;

namespace SwarmAI
{
    /// <summary>
    /// Moving state - agent is moving toward a destination.
    /// </summary>
    public class MovingState : AgentState
    {
        private Vector3 _destination;
        private float _stuckTime;
        private Vector3 _lastPosition;
        private float _stuckThreshold;
        private float _stuckTimeLimit;
        
        public MovingState(Vector3 destination)
        {
            Type = AgentStateType.Moving;
            _destination = destination;
        }
        
        public override void Enter()
        {
            base.Enter();
            _stuckTime = 0f;
            _lastPosition = Agent.Position;
            
            // Get stuck detection parameters from SwarmSettings
            var settings = SwarmManager.Instance?.Settings;
            _stuckThreshold = settings != null ? settings.StuckThreshold : 0.1f;
            _stuckTimeLimit = settings != null ? settings.StuckTimeLimit : 2f;
            
            // Set target
            Agent.SetTarget(_destination);
        }
        
        public override void Execute()
        {
            // Check if we've reached the destination
            float distance = Vector3.Distance(Agent.Position, _destination);
            if (distance <= Agent.StoppingDistance)
            {
                // Reached destination
                return;
            }
            
            // Check if stuck
            float movedDistance = Vector3.Distance(Agent.Position, _lastPosition);
            if (movedDistance < _stuckThreshold * Time.deltaTime)
            {
                _stuckTime += Time.deltaTime;
            }
            else
            {
                _stuckTime = 0f;
            }
            
            _lastPosition = Agent.Position;
        }
        
        public override AgentState CheckTransitions()
        {
            // Reached destination?
            float distance = Vector3.Distance(Agent.Position, _destination);
            if (distance <= Agent.StoppingDistance)
            {
                return new IdleState();
            }
            
            // Stuck for too long?
            if (_stuckTime >= _stuckTimeLimit)
            {
                // Only log warning if debug visualization is enabled
                if (SwarmManager.HasInstance && SwarmManager.Instance.Settings != null &&
                    SwarmManager.Instance.Settings.EnableDebugVisualization)
                {
                    Debug.LogWarning($"[MovingState] Agent {Agent.AgentId} stuck, returning to idle.");
                }
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
                case SwarmMessageType.MoveTo:
                    // Update destination
                    _destination = message.Position;
                    Agent.SetTarget(_destination);
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
