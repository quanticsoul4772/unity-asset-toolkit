namespace NPCBrain.BehaviorTree.Decorators
{
    /// <summary>
    /// Decorator that always returns Success regardless of the child's result.
    /// Running is passed through. Useful for making optional behaviors.
    /// </summary>
    public class Succeeder : DecoratorNode
    {
        public Succeeder(BTNode child) : base(child)
        {
            Name = "Succeeder";
        }
        
        protected override NodeStatus Tick(NPCBrainController brain)
        {
            if (Child == null)
            {
                return NodeStatus.Success;
            }
            
            NodeStatus status = Child.Execute(brain);
            
            // Pass through Running, convert everything else to Success
            if (status == NodeStatus.Running)
            {
                return NodeStatus.Running;
            }
            
            return NodeStatus.Success;
        }
    }
}
