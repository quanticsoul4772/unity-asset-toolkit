using UnityEngine;

namespace NPCBrain.BehaviorTree.Decorators
{
    /// <summary>
    /// Prevents execution of its child for a cooldown period after successful completion.
    /// Returns Failure during cooldown, otherwise returns child's status.
    /// </summary>
    public class Cooldown : DecoratorNode
    {
        private readonly float _cooldownTime;
        private float _lastSuccessTime = float.MinValue;
        
        /// <summary>
        /// Creates a Cooldown decorator.
        /// </summary>
        /// <param name="child">Child node to gate</param>
        /// <param name="cooldownTime">Time in seconds before child can execute again after success</param>
        public Cooldown(BTNode child, float cooldownTime) : base(child)
        {
            _cooldownTime = cooldownTime;
            Name = $"Cooldown({cooldownTime}s)";
        }
        
        protected override NodeStatus Tick(NPCBrainController brain)
        {
            if (Child == null)
            {
                return NodeStatus.Failure;
            }
            
            if (IsOnCooldown())
            {
                return NodeStatus.Failure;
            }
            
            NodeStatus status = Child.Execute(brain);
            
            if (status == NodeStatus.Success)
            {
                _lastSuccessTime = Time.time;
            }
            
            return status;
        }
        
        public bool IsOnCooldown()
        {
            return Time.time - _lastSuccessTime < _cooldownTime;
        }
        
        public float RemainingCooldown()
        {
            float remaining = _cooldownTime - (Time.time - _lastSuccessTime);
            return remaining > 0f ? remaining : 0f;
        }
        
        public void ResetCooldown()
        {
            _lastSuccessTime = float.MinValue;
        }
        
        public override void Reset()
        {
            base.Reset();
        }
        
        public float CooldownTime => _cooldownTime;
    }
}
