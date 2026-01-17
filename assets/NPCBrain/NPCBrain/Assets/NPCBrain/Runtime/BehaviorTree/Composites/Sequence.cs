namespace NPCBrain.BehaviorTree.Composites
{
    /// <summary>
    /// Executes children in order until one fails or returns running.
    /// Implements AND logic - returns Success only if all children succeed, Failure if any child fails.
    /// </summary>
    /// <remarks>
    /// <para>Use Sequence for multi-step behaviors that must all complete:
    /// "Do A, then B, then C... stop if any step fails."</para>
    /// <para>Common use cases:</para>
    /// <list type="bullet">
    ///   <item><description>Multi-step actions: MoveTo → PickUp → Return</description></item>
    ///   <item><description>Guarded behaviors: CheckCondition → PerformAction</description></item>
    ///   <item><description>Patrol loops: MoveTo → Wait → AdvanceWaypoint</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Create a patrol sequence
    /// var patrol = new Sequence(
    ///     new MoveTo(() => GetCurrentWaypoint(), 0.5f, 3f),
    ///     new Wait(2f),
    ///     new AdvanceWaypoint()
    /// );
    /// </code>
    /// </example>
    public class Sequence : CompositeNode
    {
        /// <summary>
        /// Creates a new Sequence with the specified child nodes.
        /// </summary>
        /// <param name="children">Child nodes to execute in order.</param>
        public Sequence(params BTNode[] children) : base(children)
        {
            Name = "Sequence";
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
                
                if (status == NodeStatus.Failure)
                {
                    return NodeStatus.Failure;
                }
                
                CurrentChildIndex++;
            }
            
            return NodeStatus.Success;
        }
    }
}
