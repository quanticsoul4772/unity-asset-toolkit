using System;

namespace NPCBrain.BehaviorTree.Actions
{
    /// <summary>
    /// Action that sets a value in the NPC's blackboard.
    /// </summary>
    public class SetBlackboard : BTNode
    {
        private readonly string _key;
        private readonly Func<object> _valueGetter;
        
        /// <summary>
        /// Creates a SetBlackboard action with a dynamic value.
        /// </summary>
        /// <param name="key">The blackboard key to set.</param>
        /// <param name="valueGetter">Function that returns the value to set.</param>
        public SetBlackboard(string key, Func<object> valueGetter)
        {
            _key = key;
            _valueGetter = valueGetter;
            Name = $"SetBlackboard({key})";
        }
        
        /// <summary>
        /// Creates a SetBlackboard action with a static value.
        /// </summary>
        /// <param name="key">The blackboard key to set.</param>
        /// <param name="value">The value to set.</param>
        public SetBlackboard(string key, object value)
        {
            _key = key;
            _valueGetter = () => value;
            Name = $"SetBlackboard({key})";
        }
        
        protected override NodeStatus Tick(NPCBrainController brain)
        {
            object value = _valueGetter();
            brain.Blackboard.Set(_key, value);
            return NodeStatus.Success;
        }
    }
}
