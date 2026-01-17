namespace NPCBrain.BehaviorTree.Decorators
{
    /// <summary>
    /// Base class for decorator nodes that wrap a single child node.
    /// </summary>
    public abstract class DecoratorNode : BTNode
    {
        protected BTNode Child { get; private set; }
        
        protected DecoratorNode(BTNode child)
        {
            Child = child;
        }
        
        public override void Reset()
        {
            base.Reset();
            Child?.Reset();
        }
        
        public override void Abort(NPCBrainController brain)
        {
            Child?.Abort(brain);
            base.Abort(brain);
        }
        
        public void SetChild(BTNode child)
        {
            Child = child;
        }
    }
}
