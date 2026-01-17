namespace NPCBrain.BehaviorTree
{
    public abstract class BTNode
    {
        public abstract NodeStatus Tick(NPCBrainController brain);
        
        public virtual void OnEnter(NPCBrainController brain) { }
        
        public virtual void OnExit(NPCBrainController brain) { }
    }
}
