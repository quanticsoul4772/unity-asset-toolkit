using System;

namespace NPCBrain.BehaviorTree.Actions
{
    /// <summary>
    /// Action that sets a value in the NPC's blackboard.
    /// </summary>
    /// <remarks>
    /// Always returns Success after setting the value.
    /// Supports both static values and dynamic value getters.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Static value
    /// new SetBlackboard("alert_level", 5)
    ///
    /// // Dynamic value
    /// new SetBlackboard("player_distance", () => Vector3.Distance(brain.transform.position, player.position))
    /// </code>
    /// </example>
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

        /// <summary>
        /// Evaluates the value getter and sets the blackboard key.
        /// </summary>
        /// <param name="brain">The NPCBrainController providing the blackboard.</param>
        /// <returns>Always returns Success.</returns>
        protected override NodeStatus Tick(NPCBrainController brain)
        {
            object value = _valueGetter();
            brain.Blackboard.Set(_key, value);
            return NodeStatus.Success;
        }
    }
}
