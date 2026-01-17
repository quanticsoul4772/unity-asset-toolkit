namespace NPCBrain.BehaviorTree
{
    public abstract class BTNode
    {
        private bool _isRunning;
        private NodeStatus _lastStatus = NodeStatus.Failure;
        
        public string Name { get; set; }
        public NodeStatus LastStatus => _lastStatus;
        public bool IsRunning => _isRunning;
        
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
        
        protected abstract NodeStatus Tick(NPCBrainController brain);
        
        protected virtual void OnEnter(NPCBrainController brain) { }
        
        protected virtual void OnExit(NPCBrainController brain) { }
        
        public virtual void Reset()
        {
            if (_isRunning)
            {
                _isRunning = false;
            }
            _lastStatus = NodeStatus.Failure;
        }
        
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
