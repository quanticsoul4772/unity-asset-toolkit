namespace NPCBrain.BehaviorTree.Decorators
{
    /// <summary>
    /// Repeats execution of its child node a specified number of times.
    /// Returns Running until all repetitions complete, then returns the last status.
    /// If repeatCount is -1, repeats forever (always returns Running).
    /// 
    /// Note: The child node is Reset() after each completion to allow re-execution.
    /// This is intentional behavior for repeating actions.
    /// </summary>
    public class Repeater : DecoratorNode
    {
        private readonly int _repeatCount;
        private int _currentCount;
        private NodeStatus _lastChildStatus;
        
        /// <summary>
        /// Creates a Repeater decorator.
        /// </summary>
        /// <param name="child">Child node to repeat</param>
        /// <param name="repeatCount">Number of times to repeat. Use -1 for infinite.</param>
        public Repeater(BTNode child, int repeatCount) : base(child)
        {
            _repeatCount = repeatCount;
            Name = repeatCount == -1 ? "Repeater(∞)" : $"Repeater({repeatCount})";
        }
        
        protected override void OnEnter(NPCBrainController brain)
        {
            _currentCount = 0;
            _lastChildStatus = NodeStatus.Running;
        }
        
        protected override NodeStatus Tick(NPCBrainController brain)
        {
            if (Child == null)
            {
                return NodeStatus.Failure;
            }
            
            _lastChildStatus = Child.Execute(brain);
            
            if (_lastChildStatus == NodeStatus.Running)
            {
                return NodeStatus.Running;
            }
            
            _currentCount++;
            // Reset child to allow re-execution on next tick
            Child.Reset();
            
            if (_repeatCount == -1)
            {
                return NodeStatus.Running;
            }
            
            if (_currentCount >= _repeatCount)
            {
                return _lastChildStatus;
            }
            
            return NodeStatus.Running;
        }
        
        public override void Reset()
        {
            base.Reset();
            _currentCount = 0;
            _lastChildStatus = NodeStatus.Running;
        }
        
        public int CurrentCount => _currentCount;
        public int RepeatCount => _repeatCount;
    }
}
