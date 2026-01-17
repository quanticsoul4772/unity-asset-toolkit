using NUnit.Framework;
using UnityEngine;
using NPCBrain.BehaviorTree;
using NPCBrain.BehaviorTree.Composites;
using NPCBrain.BehaviorTree.Conditions;

namespace NPCBrain.Tests.Editor
{
    // Note: These EditMode tests use AddComponent which calls Awake().
    // This works because NPCBrainController.Awake() initializes the Blackboard.
    // For tests that need multi-frame behavior, use PlayMode tests in Tests/Runtime.
    [TestFixture]
    public class BTNodeTests
    {
        private GameObject _testObject;
        private TestBrain _brain;
        
        [SetUp]
        public void SetUp()
        {
            _testObject = new GameObject("TestNPC");
            _brain = _testObject.AddComponent<TestBrain>();
        }
        
        [TearDown]
        public void TearDown()
        {
            if (_testObject != null)
            {
                Object.DestroyImmediate(_testObject);
            }
        }
        
        [Test]
        public void Selector_FirstChildSucceeds_ReturnsSuccess()
        {
            var selector = new Selector(
                new MockNode(NodeStatus.Success),
                new MockNode(NodeStatus.Success)
            );
            
            NodeStatus result = selector.Execute(_brain);
            
            Assert.AreEqual(NodeStatus.Success, result);
        }
        
        [Test]
        public void Selector_FirstChildFails_TriesSecond()
        {
            var secondNode = new MockNode(NodeStatus.Success);
            var selector = new Selector(
                new MockNode(NodeStatus.Failure),
                secondNode
            );
            
            NodeStatus result = selector.Execute(_brain);
            
            Assert.AreEqual(NodeStatus.Success, result);
            Assert.AreEqual(1, secondNode.TickCount);
        }
        
        [Test]
        public void Selector_AllChildrenFail_ReturnsFailure()
        {
            var selector = new Selector(
                new MockNode(NodeStatus.Failure),
                new MockNode(NodeStatus.Failure)
            );
            
            NodeStatus result = selector.Execute(_brain);
            
            Assert.AreEqual(NodeStatus.Failure, result);
        }
        
        [Test]
        public void Selector_ChildRunning_ReturnsRunning()
        {
            var selector = new Selector(
                new MockNode(NodeStatus.Running)
            );
            
            NodeStatus result = selector.Execute(_brain);
            
            Assert.AreEqual(NodeStatus.Running, result);
        }
        
        [Test]
        public void Sequence_AllChildrenSucceed_ReturnsSuccess()
        {
            var sequence = new Sequence(
                new MockNode(NodeStatus.Success),
                new MockNode(NodeStatus.Success)
            );
            
            NodeStatus result = sequence.Execute(_brain);
            
            Assert.AreEqual(NodeStatus.Success, result);
        }
        
        [Test]
        public void Sequence_FirstChildFails_ReturnsFailure()
        {
            var secondNode = new MockNode(NodeStatus.Success);
            var sequence = new Sequence(
                new MockNode(NodeStatus.Failure),
                secondNode
            );
            
            NodeStatus result = sequence.Execute(_brain);
            
            Assert.AreEqual(NodeStatus.Failure, result);
            Assert.AreEqual(0, secondNode.TickCount);
        }
        
        [Test]
        public void Sequence_ChildRunning_ReturnsRunning()
        {
            var sequence = new Sequence(
                new MockNode(NodeStatus.Running)
            );
            
            NodeStatus result = sequence.Execute(_brain);
            
            Assert.AreEqual(NodeStatus.Running, result);
        }
        
        [Test]
        public void CheckBlackboard_KeyExists_ReturnsSuccess()
        {
            _brain.Blackboard.Set("target", "player");
            var check = new CheckBlackboard("target");
            
            NodeStatus result = check.Execute(_brain);
            
            Assert.AreEqual(NodeStatus.Success, result);
        }
        
        [Test]
        public void CheckBlackboard_KeyMissing_ReturnsFailure()
        {
            var check = new CheckBlackboard("target");
            
            NodeStatus result = check.Execute(_brain);
            
            Assert.AreEqual(NodeStatus.Failure, result);
        }
        
        [Test]
        public void CheckBlackboard_PredicatePasses_ReturnsSuccess()
        {
            _brain.Blackboard.Set("health", 100);
            var check = new CheckBlackboard<int>("health", h => h > 50);
            
            NodeStatus result = check.Execute(_brain);
            
            Assert.AreEqual(NodeStatus.Success, result);
        }
        
        [Test]
        public void CheckBlackboard_PredicateFails_ReturnsFailure()
        {
            _brain.Blackboard.Set("health", 25);
            var check = new CheckBlackboard<int>("health", h => h > 50);
            
            NodeStatus result = check.Execute(_brain);
            
            Assert.AreEqual(NodeStatus.Failure, result);
        }
        
        [Test]
        public void BTNode_Execute_CallsOnEnterOnFirstTick()
        {
            var node = new MockNode(NodeStatus.Running);
            
            node.Execute(_brain);
            
            Assert.AreEqual(1, node.OnEnterCount);
        }
        
        [Test]
        public void BTNode_Execute_DoesNotCallOnEnterOnSubsequentTicks()
        {
            var node = new MockNode(NodeStatus.Running);
            
            node.Execute(_brain);
            node.Execute(_brain);
            node.Execute(_brain);
            
            Assert.AreEqual(1, node.OnEnterCount);
        }
        
        [Test]
        public void BTNode_Execute_CallsOnExitWhenComplete()
        {
            var node = new MockNode(NodeStatus.Success);
            
            node.Execute(_brain);
            
            Assert.AreEqual(1, node.OnExitCount);
        }
        
        [Test]
        public void BTNode_Reset_ClearsRunningState()
        {
            var node = new MockNode(NodeStatus.Running);
            node.Execute(_brain);
            Assert.IsTrue(node.IsRunning);
            
            node.Reset();
            
            Assert.IsFalse(node.IsRunning);
        }
        
        [Test]
        public void BTNode_Abort_CallsOnExitWhenRunning()
        {
            var node = new MockNode(NodeStatus.Running);
            node.Execute(_brain);
            
            node.Abort(_brain);
            
            Assert.AreEqual(1, node.OnExitCount);
            Assert.IsFalse(node.IsRunning);
        }
        
        [Test]
        public void BTNode_LastStatus_TracksStatus()
        {
            var node = new MockNode(NodeStatus.Success);
            
            node.Execute(_brain);
            
            Assert.AreEqual(NodeStatus.Success, node.LastStatus);
        }
        
        [Test]
        public void CompositeNode_Reset_ResetsAllChildren()
        {
            var child1 = new MockNode(NodeStatus.Running);
            var child2 = new MockNode(NodeStatus.Running);
            var selector = new Selector(child1, child2);
            
            child1.Execute(_brain);
            child2.Execute(_brain);
            selector.Reset();
            
            Assert.IsFalse(child1.IsRunning);
            Assert.IsFalse(child2.IsRunning);
        }
        
        private class MockNode : BTNode
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
        }
        
        private class TestBrain : NPCBrainController
        {
        }
    }
}
