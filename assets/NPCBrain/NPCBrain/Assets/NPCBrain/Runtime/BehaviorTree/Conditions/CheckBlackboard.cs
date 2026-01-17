using System;

namespace NPCBrain.BehaviorTree.Conditions
{
    /// <summary>
    /// Checks if a blackboard key exists and optionally validates it with a predicate.
    /// </summary>
    public class CheckBlackboard<T> : BTNode
    {
        private readonly string _key;
        private readonly Func<T, bool> _predicate;
        
        public CheckBlackboard(string key) : this(key, null)
        {
        }
        
        public CheckBlackboard(string key, Func<T, bool> predicate)
        {
            _key = key ?? throw new ArgumentNullException(nameof(key));
            _predicate = predicate;
            Name = $"CheckBlackboard({key})";
        }
        
        protected override NodeStatus Tick(NPCBrainController brain)
        {
            if (!brain.Blackboard.TryGet<T>(_key, out T value))
            {
                return NodeStatus.Failure;
            }
            
            if (_predicate == null)
            {
                return NodeStatus.Success;
            }
            
            return _predicate(value) ? NodeStatus.Success : NodeStatus.Failure;
        }
    }
    
    /// <summary>
    /// Non-generic version for simple key existence checks.
    /// </summary>
    public class CheckBlackboard : CheckBlackboard<object>
    {
        public CheckBlackboard(string key) : base(key)
        {
        }
        
        public CheckBlackboard(string key, Func<object, bool> predicate) : base(key, predicate)
        {
        }
    }
}
