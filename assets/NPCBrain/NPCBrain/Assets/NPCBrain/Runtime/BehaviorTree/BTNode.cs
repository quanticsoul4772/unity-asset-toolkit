namespace NPCBrain.BehaviorTree
{
    /// <summary>
    /// Base class for all behavior tree nodes.
    /// Provides the core execution lifecycle: Enter -> Tick -> Exit.
    /// </summary>
    /// <remarks>
    /// <para>Derive from this class to create custom behavior tree nodes.</para>
    /// <para>The execution flow is:</para>
    /// <list type="number">
    ///   <item><description><see cref="OnEnter"/> is called when the node starts executing</description></item>
    ///   <item><description><see cref="Tick"/> is called each frame while Running</description></item>
    ///   <item><description><see cref="OnExit"/> is called when the node completes (Success/Failure)</description></item>
    /// </list>
    /// </remarks>
    public abstract class BTNode
    {
        private bool _isRunning;
        private NodeStatus _lastStatus = NodeStatus.Failure;
        
        /// <summary>
        /// Display name for this node (used in debugging and logging).
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// The status returned by the last <see cref="Execute"/> call.
        /// </summary>
        public NodeStatus LastStatus => _lastStatus;
        
        /// <summary>
        /// True if the node is currently in the Running state.
        /// </summary>
        public bool IsRunning => _isRunning;
        
        /// <summary>
        /// Executes the behavior tree node for one tick.
        /// </summary>
        /// <param name="brain">The NPC brain controller executing this node.</param>
        /// <returns>The current status of this node (Success, Failure, or Running).</returns>
        public NodeStatus Execute(NPCBrainController brain)
        {
            if (!_isRunning)
            {
                _isRunning = true;
                OnEnter(brain);
            }
            
            _lastStatus = Tick(brain);
            
            if (_lastStatus != NodeStatus.Running)
            {
                _isRunning = false;
                OnExit(brain);
            }
            
            return _lastStatus;
        }
        
        /// <summary>
        /// Performs the node's logic for one tick. Override this in derived classes.
        /// </summary>
        /// <param name="brain">The NPC brain controller executing this node.</param>
        /// <returns>Success, Failure, or Running.</returns>
        protected abstract NodeStatus Tick(NPCBrainController brain);
        
        /// <summary>
        /// Called when the node begins execution. Override to initialize state.
        /// </summary>
        /// <param name="brain">The NPC brain controller executing this node.</param>
        protected virtual void OnEnter(NPCBrainController brain) { }
        
        /// <summary>
        /// Called when the node completes (Success or Failure). Override to cleanup.
        /// </summary>
        /// <param name="brain">The NPC brain controller executing this node.</param>
        protected virtual void OnExit(NPCBrainController brain) { }
        
        /// <summary>
        /// Resets the node to its initial state for re-execution.
        /// </summary>
        public virtual void Reset()
        {
            if (_isRunning)
            {
                _isRunning = false;
            }
            _lastStatus = NodeStatus.Failure;
        }
        
        /// <summary>
        /// Immediately stops execution and calls <see cref="OnExit"/>.
        /// </summary>
        /// <param name="brain">The NPC brain controller executing this node.</param>
        public virtual void Abort(NPCBrainController brain)
        {
            if (_isRunning)
            {
                _isRunning = false;
                OnExit(brain);
            }
        }
    }
}
