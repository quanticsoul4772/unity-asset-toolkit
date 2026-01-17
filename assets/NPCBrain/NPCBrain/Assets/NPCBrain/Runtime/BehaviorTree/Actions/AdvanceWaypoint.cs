namespace NPCBrain.BehaviorTree.Actions
{
    /// <summary>
    /// Advances to the next waypoint in the NPC's waypoint path.
    /// Always returns Success after advancing.
    /// </summary>
    /// <remarks>
    /// Use this action in a Sequence after MoveTo to create patrol behavior:
    /// <code>
    /// new Sequence(
    ///     new MoveTo(() => brain.GetCurrentWaypoint()),
    ///     new Wait(1f),
    ///     new AdvanceWaypoint()
    /// )
    /// </code>
    /// </remarks>
    public class AdvanceWaypoint : BTNode
    {
        /// <summary>
        /// Creates a new AdvanceWaypoint action.
        /// </summary>
        public AdvanceWaypoint()
        {
            Name = "AdvanceWaypoint";
        }
        
        /// <summary>
        /// Advances to the next waypoint and returns Success.
        /// </summary>
        protected override NodeStatus Tick(NPCBrainController brain)
        {
            if (brain.WaypointPath != null)
            {
                brain.WaypointPath.Advance();
            }
            return NodeStatus.Success;
        }
    }
}
