namespace NPCBrain.BehaviorTree.Composites
{
    public abstract class CompositeNode : BTNode
    {
        protected readonly BTNode[] Children;
        protected int CurrentChildIndex;
        
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
        
        public int ChildCount => Children.Length;
    }
}
