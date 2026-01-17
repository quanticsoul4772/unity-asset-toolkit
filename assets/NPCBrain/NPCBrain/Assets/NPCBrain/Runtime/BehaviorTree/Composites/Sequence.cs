namespace NPCBrain.BehaviorTree.Composites
{
    /// <summary>
    /// Executes children in order until one fails or returns running.
    /// Returns Success if all children succeed, Failure if any child fails.
    /// </summary>
    public class Sequence : CompositeNode
    {
        public Sequence(params BTNode[] children) : base(children)
        {
            Name = "Sequence";
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
                
                if (status == NodeStatus.Failure)
                {
                    return NodeStatus.Failure;
                }
                
                CurrentChildIndex++;
            }
            
            return NodeStatus.Success;
        }
    }
}
