using NUnit.Framework;
using UnityEngine;
using NPCBrain.BehaviorTree;
using NPCBrain.BehaviorTree.Composites;
using NPCBrain.BehaviorTree.Conditions;

namespace NPCBrain.Tests.Editor
{
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
            
            NodeStatus result = selector.Tick(_brain);
            
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
            
            NodeStatus result = selector.Tick(_brain);
            
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
            
            NodeStatus result = selector.Tick(_brain);
            
            Assert.AreEqual(NodeStatus.Failure, result);
        }
        
        [Test]
        public void Selector_ChildRunning_ReturnsRunning()
        {
            var selector = new Selector(
                new MockNode(NodeStatus.Running)
            );
            
            NodeStatus result = selector.Tick(_brain);
            
            Assert.AreEqual(NodeStatus.Running, result);
        }
        
        [Test]
        public void Sequence_AllChildrenSucceed_ReturnsSuccess()
        {
            var sequence = new Sequence(
                new MockNode(NodeStatus.Success),
                new MockNode(NodeStatus.Success)
            );
            
            NodeStatus result = sequence.Tick(_brain);
            
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
            
            NodeStatus result = sequence.Tick(_brain);
            
            Assert.AreEqual(NodeStatus.Failure, result);
            Assert.AreEqual(0, secondNode.TickCount);
        }
        
        [Test]
        public void Sequence_ChildRunning_ReturnsRunning()
        {
            var sequence = new Sequence(
                new MockNode(NodeStatus.Running)
            );
            
            NodeStatus result = sequence.Tick(_brain);
            
            Assert.AreEqual(NodeStatus.Running, result);
        }
        
        [Test]
        public void CheckBlackboard_KeyExists_ReturnsSuccess()
        {
            _brain.Blackboard.Set("target", "player");
            var check = new CheckBlackboard("target");
            
            NodeStatus result = check.Tick(_brain);
            
            Assert.AreEqual(NodeStatus.Success, result);
        }
        
        [Test]
        public void CheckBlackboard_KeyMissing_ReturnsFailure()
        {
            var check = new CheckBlackboard("target");
            
            NodeStatus result = check.Tick(_brain);
            
            Assert.AreEqual(NodeStatus.Failure, result);
        }
        
        [Test]
        public void CheckBlackboard_PredicatePasses_ReturnsSuccess()
        {
            _brain.Blackboard.Set("health", 100);
            var check = new CheckBlackboard<int>("health", h => h > 50);
            
            NodeStatus result = check.Tick(_brain);
            
            Assert.AreEqual(NodeStatus.Success, result);
        }
        
        [Test]
        public void CheckBlackboard_PredicateFails_ReturnsFailure()
        {
            _brain.Blackboard.Set("health", 25);
            var check = new CheckBlackboard<int>("health", h => h > 50);
            
            NodeStatus result = check.Tick(_brain);
            
            Assert.AreEqual(NodeStatus.Failure, result);
        }
        
        private class MockNode : BTNode
        {
            private readonly NodeStatus _status;
            public int TickCount { get; private set; }
            
            public MockNode(NodeStatus status)
            {
                _status = status;
            }
            
            public override NodeStatus Tick(NPCBrainController brain)
            {
                TickCount++;
                return _status;
            }
        }
        
        private class TestBrain : NPCBrainController
        {
        }
    }
}
