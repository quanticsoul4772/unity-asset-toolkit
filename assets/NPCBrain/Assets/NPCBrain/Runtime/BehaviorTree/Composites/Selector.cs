namespace NPCBrain.BehaviorTree.Composites
{
    /// <summary>
    /// Executes children in order until one succeeds or returns running.
    /// Returns Success if any child succeeds, Failure if all children fail.
    /// </summary>
    public class Selector : CompositeNode
    {
        public Selector(params BTNode[] children) : base(children)
        {
            Name = "Selector";
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
                
                if (status == NodeStatus.Success)
                {
                    return NodeStatus.Success;
                }
                
                CurrentChildIndex++;
            }
            
            return NodeStatus.Failure;
        }
    }
}
