using NUnit.Framework;
using UnityEngine;
using NPCBrain.BehaviorTree;
using NPCBrain.BehaviorTree.Composites;
using NPCBrain.UtilityAI;
using NPCBrain.UtilityAI.Curves;

namespace NPCBrain.Tests.Editor
{
    [TestFixture]
    public class UtilityAITests
    {
        private GameObject _testObject;
        private TestBrain _brain;
        
        [SetUp]
        public void SetUp()
        {
            _testObject = new GameObject("TestNPC");
            _brain = _testObject.AddComponent<TestBrain>();
            if (_brain.Blackboard == null)
            {
                _brain.InitializeForTests();
            }
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
        public void LinearCurve_DefaultSlope_ReturnsInput()
        {
            var curve = new LinearCurve();
            
            Assert.AreEqual(0f, curve.Evaluate(0f), 0.001f);
            Assert.AreEqual(0.5f, curve.Evaluate(0.5f), 0.001f);
            Assert.AreEqual(1f, curve.Evaluate(1f), 0.001f);
        }
        
        [Test]
        public void LinearCurve_WithSlopeAndOffset_TransformsInput()
        {
            var curve = new LinearCurve(2f, 0.1f);
            
            Assert.AreEqual(0.1f, curve.Evaluate(0f), 0.001f);
            Assert.AreEqual(0.6f, curve.Evaluate(0.25f), 0.001f);
        }
        
        [Test]
        public void LinearCurve_ClampsOutput()
        {
            var curve = new LinearCurve(2f, 0.5f);
            
            Assert.AreEqual(1f, curve.Evaluate(1f), 0.001f);
        }
        
        [Test]
        public void ExponentialCurve_SquaredByDefault()
        {
            var curve = new ExponentialCurve();
            
            Assert.AreEqual(0f, curve.Evaluate(0f), 0.001f);
            Assert.AreEqual(0.25f, curve.Evaluate(0.5f), 0.001f);
            Assert.AreEqual(1f, curve.Evaluate(1f), 0.001f);
        }
        
        [Test]
        public void ExponentialCurve_CustomExponent()
        {
            var curve = new ExponentialCurve(3f);
            
            Assert.AreEqual(0.125f, curve.Evaluate(0.5f), 0.001f);
        }
        
        [Test]
        public void StepCurve_BelowThreshold_ReturnsLow()
        {
            var curve = new StepCurve(0.5f);
            
            Assert.AreEqual(0f, curve.Evaluate(0.4f), 0.001f);
        }
        
        [Test]
        public void StepCurve_AtOrAboveThreshold_ReturnsHigh()
        {
            var curve = new StepCurve(0.5f);
            
            Assert.AreEqual(1f, curve.Evaluate(0.5f), 0.001f);
            Assert.AreEqual(1f, curve.Evaluate(0.6f), 0.001f);
        }
        
        [Test]
        public void StepCurve_CustomValues()
        {
            var curve = new StepCurve(0.3f, 0.2f, 0.8f);
            
            Assert.AreEqual(0.2f, curve.Evaluate(0.2f), 0.001f);
            Assert.AreEqual(0.8f, curve.Evaluate(0.3f), 0.001f);
        }
        
        [Test]
        public void ConstantConsideration_ReturnsConstantValue()
        {
            var consideration = new ConstantConsideration(0.7f);
            
            float score = consideration.Score(_brain);
            
            Assert.AreEqual(0.7f, score, 0.001f);
        }
        
        [Test]
        public void ConstantConsideration_WithCurve_AppliesCurve()
        {
            var consideration = new ConstantConsideration("Test", 0.5f, new ExponentialCurve());
            
            float score = consideration.Score(_brain);
            
            Assert.AreEqual(0.25f, score, 0.001f);
        }
        
        [Test]
        public void BlackboardConsideration_ReadsFromBlackboard()
        {
            _brain.Blackboard.Set("health", 80f);
            var consideration = new BlackboardConsideration<float>(
                "Health", "health", h => h / 100f, 0f
            );
            
            float score = consideration.Score(_brain);
            
            Assert.AreEqual(0.8f, score, 0.001f);
        }
        
        [Test]
        public void BlackboardConsideration_MissingKey_UsesDefault()
        {
            var consideration = new BlackboardConsideration<float>(
                "Health", "health", h => h / 100f, 50f
            );
            
            float score = consideration.Score(_brain);
            
            Assert.AreEqual(0.5f, score, 0.001f);
        }
        
        [Test]
        public void UtilityAction_NoConsiderations_ReturnsBaseScore()
        {
            var action = new UtilityAction("Test", new MockNode(NodeStatus.Success));
            action.BaseScore = 0.8f;
            
            float score = action.Score(_brain);
            
            Assert.AreEqual(0.8f, score, 0.001f);
        }
        
        [Test]
        public void UtilityAction_WithConsiderations_MultipliesScores()
        {
            var action = new UtilityAction(
                "Test",
                new MockNode(NodeStatus.Success),
                new ConstantConsideration(0.5f),
                new ConstantConsideration(0.5f)
            );
            
            float score = action.Score(_brain);
            
            Assert.Greater(score, 0.25f);
            Assert.Less(score, 0.5f);
        }
        
        [Test]
        public void UtilityAction_ZeroConsideration_ReturnsZero()
        {
            var action = new UtilityAction(
                "Test",
                new MockNode(NodeStatus.Success),
                new ConstantConsideration(0f)
            );
            
            float score = action.Score(_brain);
            
            Assert.AreEqual(0f, score, 0.001f);
        }
        
        [Test]
        public void UtilitySelector_NoActions_ReturnsFailure()
        {
            var selector = new UtilitySelector();
            
            NodeStatus result = selector.Execute(_brain);
            
            Assert.AreEqual(NodeStatus.Failure, result);
        }
        
        [Test]
        public void UtilitySelector_SingleAction_ExecutesAction()
        {
            var mockNode = new MockNode(NodeStatus.Success);
            var action = new UtilityAction("Test", mockNode, new ConstantConsideration(1f));
            var selector = new UtilitySelector(action);
            
            NodeStatus result = selector.Execute(_brain);
            
            Assert.AreEqual(NodeStatus.Success, result);
            Assert.AreEqual(1, mockNode.TickCount);
        }
        
        [Test]
        public void UtilitySelector_RunningAction_ContinuesSameAction()
        {
            var mockNode = new MockNode(NodeStatus.Running);
            var action = new UtilityAction("Test", mockNode, new ConstantConsideration(1f));
            var selector = new UtilitySelector(action);
            
            selector.Execute(_brain);
            selector.Execute(_brain);
            selector.Execute(_brain);
            
            Assert.AreEqual(3, mockNode.TickCount);
        }
        
        [Test]
        public void UtilitySelector_AllZeroScores_ReturnsFailure()
        {
            var action = new UtilityAction("Test", new MockNode(NodeStatus.Success), new ConstantConsideration(0f));
            var selector = new UtilitySelector(action);
            
            NodeStatus result = selector.Execute(_brain);
            
            Assert.AreEqual(NodeStatus.Failure, result);
        }
        
        [Test]
        public void UtilitySelector_HighTemperature_IncreasesRandomness()
        {
            _brain.Criticality.SetTemperature(2f);
            
            var lowAction = new UtilityAction("Low", new MockNode(NodeStatus.Success), new ConstantConsideration(0.3f));
            var highAction = new UtilityAction("High", new MockNode(NodeStatus.Success), new ConstantConsideration(0.7f));
            var selector = new UtilitySelector(lowAction, highAction);
            
            int lowCount = 0;
            int highCount = 0;
            
            for (int i = 0; i < 100; i++)
            {
                selector.Reset();
                selector.Execute(_brain);
                
                if (selector.CurrentAction == null)
                {
                    selector.Execute(_brain);
                }
                
                if (selector.GetLastProbabilities()[0] > 0.1f)
                {
                    lowCount++;
                }
                if (selector.GetLastProbabilities()[1] > 0.1f)
                {
                    highCount++;
                }
            }
            
            Assert.Greater(lowCount, 0, "Low action should have non-trivial probability with high temperature");
        }
        
        private class MockNode : BTNode
        {
            private readonly NodeStatus _status;
            public int TickCount { get; private set; }
            
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
        }
        
        private class TestBrain : NPCBrainController
        {
            public void InitializeForTests()
            {
                var bbField = typeof(NPCBrainController).GetField("<Blackboard>k__BackingField",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                bbField?.SetValue(this, new Blackboard());
                
                var critField = typeof(NPCBrainController).GetField("<Criticality>k__BackingField",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                critField?.SetValue(this, new Criticality.CriticalityController());
            }
            
            public new Blackboard Blackboard => base.Blackboard;
            public new Criticality.CriticalityController Criticality => base.Criticality;
        }
    }
}
