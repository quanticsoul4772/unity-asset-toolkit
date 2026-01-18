namespace NPCBrain.BehaviorTree.Actions
{
    /// <summary>
    /// Action that removes a key from the NPC's blackboard.
    /// </summary>
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
        
        protected override NodeStatus Tick(NPCBrainController brain)
        {
            brain.Blackboard.Remove(_key);
            return NodeStatus.Success;
        }
    }
}
