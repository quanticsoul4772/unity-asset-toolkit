namespace NPCBrain.BehaviorTree.Composites
{
    /// <summary>
    /// Policy for determining when a Parallel node succeeds.
    /// </summary>
    public enum ParallelPolicy
    {
        /// <summary>Succeed when all children succeed. Fail if any child fails.</summary>
        RequireAll,
        /// <summary>Succeed when any child succeeds. Fail only if all children fail.</summary>
        RequireOne
    }
    
    /// <summary>
    /// Executes all children concurrently each tick.
    /// Returns based on the specified success/failure policy.
    /// </summary>
    /// <remarks>
    /// <para>Unlike Selector and Sequence which execute children one at a time,
    /// Parallel ticks ALL children every frame. This is useful for:</para>
    /// <list type="bullet">
    ///   <item><description>Running multiple behaviors simultaneously (move while shooting)</description></item>
    ///   <item><description>Monitoring conditions while executing actions</description></item>
    ///   <item><description>Coordinating multiple subsystems</description></item>
    /// </list>
    /// <para>Two policies control success/failure behavior:</para>
    /// <list type="bullet">
    ///   <item><description>RequireAll: AND logic - all children must succeed</description></item>
    ///   <item><description>RequireOne: OR logic - any child can succeed</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Move to target while playing animation (both must complete)
    /// var moveAndAnimate = new Parallel(ParallelPolicy.RequireAll,
    ///     new MoveTo(() => targetPosition),
    ///     new PlayAnimation("walk")
    /// );
    /// 
    /// // Monitor for danger while patrolling (succeed if patrol done OR danger spotted)
    /// var patrolWithAwareness = new Parallel(ParallelPolicy.RequireOne,
    ///     new Sequence(new MoveTo(() => waypoint), new Wait(1f)),
    ///     new CheckTargetVisible()
    /// );
    /// </code>
    /// </example>
    public class Parallel : CompositeNode
    {
        private readonly ParallelPolicy _successPolicy;
        private readonly NodeStatus[] _childStatuses;
        
        /// <summary>
        /// Creates a new Parallel node with the specified policy and children.
        /// </summary>
        /// <param name="successPolicy">Policy for determining success/failure.</param>
        /// <param name="children">Child nodes to execute concurrently.</param>
        public Parallel(ParallelPolicy successPolicy, params BTNode[] children) : base(children)
        {
            Name = "Parallel";
            _successPolicy = successPolicy;
            _childStatuses = new NodeStatus[children.Length];
        }
        
        /// <summary>
        /// Creates a new Parallel node with RequireAll policy.
        /// </summary>
        /// <param name="children">Child nodes to execute concurrently.</param>
        public Parallel(params BTNode[] children) : this(ParallelPolicy.RequireAll, children)
        {
        }
        
        protected override void OnEnter(NPCBrainController brain)
        {
            base.OnEnter(brain);
            for (int i = 0; i < _childStatuses.Length; i++)
            {
                _childStatuses[i] = NodeStatus.Running;
            }
        }
        
        protected override NodeStatus Tick(NPCBrainController brain)
        {
            int successCount = 0;
            int failureCount = 0;
            bool anyRunning = false;
            
            for (int i = 0; i < Children.Length; i++)
            {
                // Only tick children that are still running
                if (_childStatuses[i] == NodeStatus.Running)
                {
                    _childStatuses[i] = Children[i].Execute(brain);
                }
                
                switch (_childStatuses[i])
                {
                    case NodeStatus.Success:
                        successCount++;
                        break;
                    case NodeStatus.Failure:
                        failureCount++;
                        break;
                    case NodeStatus.Running:
                        anyRunning = true;
                        break;
                }
            }
            
            // Evaluate based on policy
            if (_successPolicy == ParallelPolicy.RequireAll)
            {
                // Fail immediately if any child fails
                if (failureCount > 0)
                {
                    return NodeStatus.Failure;
                }
                // Succeed only when all children succeed
                if (successCount == Children.Length)
                {
                    return NodeStatus.Success;
                }
            }
            else // RequireOne
            {
                // Succeed immediately if any child succeeds
                if (successCount > 0)
                {
                    return NodeStatus.Success;
                }
                // Fail only when all children have failed
                if (failureCount == Children.Length)
                {
                    return NodeStatus.Failure;
                }
            }
            
            // Still running if we haven't determined success/failure
            return anyRunning ? NodeStatus.Running : NodeStatus.Failure;
        }
        
        public override void Reset()
        {
            base.Reset();
            for (int i = 0; i < _childStatuses.Length; i++)
            {
                _childStatuses[i] = NodeStatus.Running;
            }
        }
        
        public override void Abort(NPCBrainController brain)
        {
            // Abort all running children
            for (int i = 0; i < Children.Length; i++)
            {
                if (_childStatuses[i] == NodeStatus.Running)
                {
                    Children[i].Abort(brain);
                }
            }
            base.Abort(brain);
        }
    }
}
