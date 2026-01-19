namespace NPCBrain.BehaviorTree.Actions
{
    /// <summary>
    /// Action that removes a key from the NPC's blackboard.
    /// </summary>
    /// <remarks>
    /// Always returns Success after removing the key.
    /// Useful for cleaning up temporary state or resetting conditions.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Clear target after losing sight
    /// new Sequence(
    ///     new CheckBlackboard("target"),  // Check if target exists
    ///     new ClearBlackboardKey("target")  // Clear it
    /// )
    /// </code>
    /// </example>
    public class ClearBlackboardKey : BTNode
    {
        private readonly string _key;
        
        /// <summary>
        /// Creates a ClearBlackboardKey action.
        /// </summary>
        /// <param name="key">The blackboard key to remove.</param>
        public ClearBlackboardKey(string key)
        {
            _key = key;
            Name = $"ClearBlackboard({key})";
        }

        /// <summary>
        /// Removes the specified key from the blackboard.
        /// </summary>
        /// <param name="brain">The NPCBrainController providing the blackboard.</param>
        /// <returns>Always returns Success.</returns>
        protected override NodeStatus Tick(NPCBrainController brain)
        {
            brain.Blackboard.Remove(_key);
            return NodeStatus.Success;
        }
    }
}
