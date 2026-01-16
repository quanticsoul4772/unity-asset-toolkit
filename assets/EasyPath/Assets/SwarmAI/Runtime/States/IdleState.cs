using UnityEngine;

namespace SwarmAI
{
    /// <summary>
    /// Idle state - agent is not performing any action.
    /// </summary>
    public class IdleState : AgentState
    {
        private float _idleTime;
        
        public IdleState()
        {
            Type = AgentStateType.Idle;
        }
        
        public override void Enter()
        {
            base.Enter();
            _idleTime = 0f;
            
            // Stop any current movement
            Agent.ClearTarget();
        }
        
        public override void Execute()
        {
            _idleTime += Time.deltaTime;
        }
        
        public override bool HandleMessage(SwarmMessage message)
        {
            switch (message.Type)
            {
                case SwarmMessageType.MoveTo:
                    Agent.SetState(new MovingState(message.Position));
                    return true;
                    
                case SwarmMessageType.Seek:
                    Agent.SetState(new SeekingState(message.Position));
                    return true;
                    
                case SwarmMessageType.Flee:
                    Agent.SetState(new FleeingState(message.Position));
                    return true;
            }
            
            return false;
        }
    }
}
