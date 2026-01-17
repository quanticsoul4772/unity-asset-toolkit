namespace NPCBrain.BehaviorTree.Composites
{
    /// <summary>
    /// Base class for behavior tree nodes that have multiple children.
    /// </summary>
    /// <remarks>
    /// <para>Composite nodes control the flow of execution through their children.
    /// The two most common composites are:</para>
    /// <list type="bullet">
    ///   <item><description><see cref="Selector"/> - OR logic: succeeds if any child succeeds</description></item>
    ///   <item><description><see cref="Sequence"/> - AND logic: succeeds only if all children succeed</description></item>
    /// </list>
    /// <para>Composites track which child is currently executing via <see cref="CurrentChildIndex"/>
    /// and automatically reset this index when entering/exiting the node.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Create a custom composite that executes children in random order
    /// public class RandomSelector : CompositeNode
    /// {
    ///     public RandomSelector(params BTNode[] children) : base(children) { }
    ///     
    ///     protected override NodeStatus Tick(NPCBrainController brain)
    ///     {
    ///         // Custom selection logic here
    ///     }
    /// }
    /// </code>
    /// </example>
    public abstract class CompositeNode : BTNode
    {
        /// <summary>Array of child nodes to execute.</summary>
        protected readonly BTNode[] Children;
        
        /// <summary>Index of the currently executing child.</summary>
        protected int CurrentChildIndex;
        
        /// <summary>
        /// Creates a new composite node with the specified children.
        /// </summary>
        /// <param name="children">Child nodes to execute.</param>
        protected CompositeNode(params BTNode[] children)
        {
            Children = children ?? System.Array.Empty<BTNode>();
        }
        
        protected override void OnEnter(NPCBrainController brain)
        {
            CurrentChildIndex = 0;
        }
        
        protected override void OnExit(NPCBrainController brain)
        {
            CurrentChildIndex = 0;
        }
        
        public override void Reset()
        {
            base.Reset();
            CurrentChildIndex = 0;
            foreach (var child in Children)
            {
                child.Reset();
            }
        }
        
        public override void Abort(NPCBrainController brain)
        {
            if (CurrentChildIndex < Children.Length)
            {
                Children[CurrentChildIndex].Abort(brain);
            }
            base.Abort(brain);
        }
        
        /// <summary>
        /// Gets the number of child nodes.
        /// </summary>
        public int ChildCount => Children.Length;
        
        /// <summary>
        /// Gets a child node by index.
        /// </summary>
        /// <param name="index">Index of the child to retrieve.</param>
        /// <returns>The child node at the specified index.</returns>
        public BTNode GetChild(int index) => Children[index];
    }
}
