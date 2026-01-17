namespace NPCBrain.BehaviorTree.Decorators
{
    /// <summary>
    /// Inverts the result of its child node.
    /// Success becomes Failure, Failure becomes Success, Running stays Running.
    /// </summary>
    public class Inverter : DecoratorNode
    {
        public Inverter(BTNode child) : base(child)
        {
            Name = "Inverter";
        }
        
        protected override NodeStatus Tick(NPCBrainController brain)
        {
            if (Child == null)
            {
                return NodeStatus.Failure;
            }
            
            NodeStatus status = Child.Execute(brain);
            
            switch (status)
            {
                case NodeStatus.Success:
                    return NodeStatus.Failure;
                case NodeStatus.Failure:
                    return NodeStatus.Success;
                default:
                    return NodeStatus.Running;
            }
        }
    }
}
