using System;

namespace NPCBrain.BehaviorTree.Conditions
{
    public class CheckBlackboard : BTNode
    {
        private readonly string _key;
        private readonly Func<object, bool> _predicate;
        
        public CheckBlackboard(string key)
        {
            _key = key;
            _predicate = value => value != null;
        }
        
        public CheckBlackboard(string key, Func<object, bool> predicate)
        {
            _key = key;
            _predicate = predicate;
        }
        
        public override NodeStatus Tick(NPCBrainController brain)
        {
            if (!brain.Blackboard.Has(_key))
            {
                return NodeStatus.Failure;
            }
            
            object value = brain.Blackboard.Get<object>(_key);
            return _predicate(value) ? NodeStatus.Success : NodeStatus.Failure;
        }
    }
    
    public class CheckBlackboard<T> : BTNode
    {
        private readonly string _key;
        private readonly Func<T, bool> _predicate;
        
        public CheckBlackboard(string key, Func<T, bool> predicate)
        {
            _key = key;
            _predicate = predicate;
        }
        
        public override NodeStatus Tick(NPCBrainController brain)
        {
            if (!brain.Blackboard.Has(_key))
            {
                return NodeStatus.Failure;
            }
            
            object rawValue = brain.Blackboard.Get<object>(_key);
            if (!(rawValue is T))
            {
                return NodeStatus.Failure;
            }
            
            T value = (T)rawValue;
            return _predicate(value) ? NodeStatus.Success : NodeStatus.Failure;
        }
    }
}
