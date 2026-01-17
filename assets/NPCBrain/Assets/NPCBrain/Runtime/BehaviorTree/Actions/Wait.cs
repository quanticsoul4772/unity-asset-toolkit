using UnityEngine;

namespace NPCBrain.BehaviorTree.Actions
{
    public class Wait : BTNode
    {
        private readonly float _duration;
        private float _startTime;
        private bool _isWaiting;
        
        public Wait(float duration)
        {
            _duration = duration;
        }
        
        public override void OnEnter(NPCBrainController brain)
        {
            _startTime = Time.time;
            _isWaiting = true;
        }
        
        public override void OnExit(NPCBrainController brain)
        {
            _isWaiting = false;
        }
        
        public override NodeStatus Tick(NPCBrainController brain)
        {
            if (!_isWaiting)
            {
                OnEnter(brain);
            }
            
            if (Time.time - _startTime >= _duration)
            {
                _isWaiting = false;
                return NodeStatus.Success;
            }
            
            return NodeStatus.Running;
        }
    }
}
