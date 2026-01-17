namespace NPCBrain.BehaviorTree.Composites
{
    public class Selector : BTNode
    {
        private readonly BTNode[] _children;
        private int _currentChild = 0;
        
        public Selector(params BTNode[] children)
        {
            _children = children;
        }
        
        public override NodeStatus Tick(NPCBrainController brain)
        {
            while (_currentChild < _children.Length)
            {
                NodeStatus status = _children[_currentChild].Tick(brain);
                
                if (status == NodeStatus.Running)
                {
                    return NodeStatus.Running;
                }
                
                if (status == NodeStatus.Success)
                {
                    _currentChild = 0;
                    return NodeStatus.Success;
                }
                
                _currentChild++;
            }
            
            _currentChild = 0;
            return NodeStatus.Failure;
        }
        
        public override void OnEnter(NPCBrainController brain)
        {
            _currentChild = 0;
        }
        
        public override void OnExit(NPCBrainController brain)
        {
            _currentChild = 0;
        }
    }
}
