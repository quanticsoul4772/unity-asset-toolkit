using UnityEngine;

namespace NPCBrain.BehaviorTree.Actions
{
    public class Wait : BTNode
    {
        private readonly float _duration;
        private float _startTime;
        
        public Wait(float duration)
        {
            _duration = duration;
            Name = "Wait";
        }
        
        protected override void OnEnter(NPCBrainController brain)
        {
            _startTime = Time.time;
        }
        
        protected override NodeStatus Tick(NPCBrainController brain)
        {
            if (Time.time - _startTime >= _duration)
            {
                return NodeStatus.Success;
            }
            
            return NodeStatus.Running;
        }
    }
}
