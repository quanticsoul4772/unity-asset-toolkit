namespace NPCBrain.BehaviorTree.Composites
{
    public class Sequence : BTNode
    {
        private readonly BTNode[] _children;
        private int _currentChild = 0;
        
        public Sequence(params BTNode[] children)
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
                
                if (status == NodeStatus.Failure)
                {
                    _currentChild = 0;
                    return NodeStatus.Failure;
                }
                
                _currentChild++;
            }
            
            _currentChild = 0;
            return NodeStatus.Success;
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
