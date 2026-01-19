namespace NPCBrain.BehaviorTree
{
    /// <summary>
    /// Status returned by behavior tree nodes during execution.
    /// </summary>
    /// <remarks>
    /// The behavior tree execution engine uses these statuses to control flow:
    /// - Success: Allows parent composites (Sequence/Selector) to proceed to next child
    /// - Failure: Causes Sequence to fail, allows Selector to try next child
    /// - Running: Preserves node state, continues execution next frame (pauses parent composite)
    /// </remarks>
    public enum NodeStatus
    {
        /// <summary>
        /// Node completed successfully. Sequence nodes proceed to next child, Selector nodes succeed immediately.
        /// </summary>
        Success,

        /// <summary>
        /// Node failed to complete its task. Sequence nodes fail immediately, Selector nodes try next child.
        /// </summary>
        Failure,

        /// <summary>
        /// Node is still executing and will continue next frame. Parent composite is paused until completion.
        /// </summary>
        Running
    }
}
