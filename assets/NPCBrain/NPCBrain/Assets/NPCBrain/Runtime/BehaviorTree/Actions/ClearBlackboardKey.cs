namespace NPCBrain.BehaviorTree.Actions
{
    /// <summary>
    /// Action node that removes a key from the NPC's blackboard.
    /// Always returns Success after clearing the key.
    /// </summary>
    /// <remarks>
    /// <para>Use this to clear temporary blackboard data after it's no longer needed,
    /// such as investigation targets or expired alerts.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Clear the last known position after investigating
    /// var investigate = new Sequence(
    ///     new MoveTo(() => blackboard.Get&lt;Vector3&gt;("lastKnownPosition")),
    ///     new Wait(3f),
    ///     new ClearBlackboardKey("lastKnownPosition")
    /// );
    /// </code>
    /// </example>
    public class ClearBlackboardKey : BTNode
    {
        private readonly string _key;
        
        /// <summary>
        /// Creates a new ClearBlackboardKey action.
        /// </summary>
        /// <param name="key">The blackboard key to clear.</param>
        public ClearBlackboardKey(string key)
        {
            _key = key;
            Name = $"Clear({key})";
        }
        
        /// <inheritdoc/>
        protected override NodeStatus Tick(NPCBrainController brain)
        {
            brain.Blackboard.Remove(_key);
            return NodeStatus.Success;
        }
    }
}
