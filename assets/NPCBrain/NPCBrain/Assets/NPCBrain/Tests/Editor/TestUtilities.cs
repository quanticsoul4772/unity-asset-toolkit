using UnityEngine;
using NPCBrain.BehaviorTree;
using NPCBrain.Criticality;

namespace NPCBrain.Tests.Editor
{
    /// <summary>
    /// Test brain that allows direct initialization without relying on Awake().
    /// Overrides Awake() to prevent automatic initialization, giving tests full control.
    /// </summary>
    public class TestBrain : NPCBrainController
    {
        protected override void Awake()
        {
            // Don't call base.Awake() - tests control initialization via InitializeForTests()
        }
        
        public void InitializeForTests()
        {
            // Protected setters are accessible from derived classes
            Blackboard = new Blackboard();
            Criticality = new CriticalityController();
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
