namespace NPCBrain.BehaviorTree.Composites
{
    /// <summary>
    /// Executes children in order until one succeeds or returns running.
    /// Implements OR logic - returns Success if any child succeeds, Failure if all children fail.
    /// </summary>
    /// <remarks>
    /// <para>Use Selector for priority-based behavior where you want to try alternatives:
    /// "Try A, if that fails try B, if that fails try C..."</para>
    /// <para>Common use cases:</para>
    /// <list type="bullet">
    ///   <item><description>Priority systems: Chase → Investigate → Patrol</description></item>
    ///   <item><description>Fallback behaviors: Attack → Flee → Idle</description></item>
    ///   <item><description>Conditional branching: If-Then-Else patterns</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Create a priority-based AI selector
    /// var ai = new Selector(
    ///     new Sequence(                     // Priority 1: Attack if enemy nearby
    ///         new CheckBlackboard("hasEnemy"),
    ///         new MoveTo(() => GetEnemyPosition())
    ///     ),
    ///     new Sequence(                     // Priority 2: Patrol (fallback)
    ///         new MoveTo(() => GetWaypoint()),
    ///         new Wait(2f)
    ///     )
    /// );
    /// </code>
    /// </example>
    public class Selector : CompositeNode
    {
        /// <summary>
        /// Creates a new Selector with the specified child nodes.
        /// </summary>
        /// <param name="children">Child nodes to execute in priority order.</param>
        public Selector(params BTNode[] children) : base(children)
        {
            Name = "Selector";
        }
        
        protected override NodeStatus Tick(NPCBrainController brain)
        {
            while (CurrentChildIndex < Children.Length)
            {
                NodeStatus status = Children[CurrentChildIndex].Execute(brain);
                
                if (status == NodeStatus.Running)
                {
                    return NodeStatus.Running;
                }
                
                if (status == NodeStatus.Success)
                {
                    return NodeStatus.Success;
                }
                
                CurrentChildIndex++;
            }
            
            return NodeStatus.Failure;
        }
    }
}
