using UnityEngine;
using NPCBrain.BehaviorTree;
using NPCBrain.Criticality;

namespace NPCBrain.Tests.Editor
{
    /// <summary>
    /// Test brain that allows direct initialization without relying on Awake().
    /// Uses protected setters instead of reflection for cleaner test setup.
    /// </summary>
    public class TestBrain : NPCBrainController
    {
        public void InitializeForTests()
        {
            Blackboard = new Blackboard();
            Criticality = new CriticalityController();
        }
        
        public new Blackboard Blackboard
        {
            get => base.Blackboard;
            set => base.Blackboard = value;
        }
        
        public new CriticalityController Criticality
        {
            get => base.Criticality;
            set => base.Criticality = value;
        }
    }
    
    /// <summary>
    /// Mock BTNode for testing composite nodes and node lifecycle.
    /// </summary>
    public class MockNode : BTNode
    {
        private readonly NodeStatus _status;
        
        public int TickCount { get; private set; }
        public int OnEnterCount { get; private set; }
        public int OnExitCount { get; private set; }
        
        public MockNode(NodeStatus status)
        {
            _status = status;
            Name = "MockNode";
        }
        
        protected override NodeStatus Tick(NPCBrainController brain)
        {
            TickCount++;
            return _status;
        }
        
        protected override void OnEnter(NPCBrainController brain)
        {
            OnEnterCount++;
        }
        
        protected override void OnExit(NPCBrainController brain)
        {
            OnExitCount++;
        }
        
        public void ResetCounts()
        {
            TickCount = 0;
            OnEnterCount = 0;
            OnExitCount = 0;
        }
    }
}
