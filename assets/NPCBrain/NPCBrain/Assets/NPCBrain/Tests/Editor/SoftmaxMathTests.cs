using System;
using NUnit.Framework;
using UnityEngine;
using NPCBrain.BehaviorTree;
using NPCBrain.BehaviorTree.Composites;
using NPCBrain.UtilityAI;
using NPCBrain.Tests;

namespace NPCBrain.Tests.Editor
{
    /// <summary>
    /// Deterministic unit tests for softmax probability calculations.
    /// These tests verify the mathematical correctness of UtilitySelector's
    /// probability distribution without relying on random sampling.
    /// </summary>
    [TestFixture]
    public class SoftmaxMathTests
    {
        private GameObject _testObject;
        private TestBrain _brain;
        
        [SetUp]
        public void SetUp()
        {
            _testObject = new GameObject("TestNPC");
            _brain = _testObject.AddComponent<TestBrain>();
            _brain.InitializeForTests();
        }
        
        [TearDown]
        public void TearDown()
        {
            if (_testObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_testObject);
            }
        }
        
        [Test]
        public void Softmax_EqualScores_EqualProbabilities()
        {
            var action1 = new UtilityAction("A", new MockNode(NodeStatus.Success), new ConstantConsideration(0.5f));
            var action2 = new UtilityAction("B", new MockNode(NodeStatus.Success), new ConstantConsideration(0.5f));
            var selector = new UtilitySelector(42, action1, action2);
            
            _brain.Criticality.SetTemperature(1f);
            
            // Execute to calculate probabilities
            selector.Execute(_brain);
            var probs = selector.GetLastProbabilities();
            
            Assert.AreEqual(2, probs.Count);
            // With equal scores, probabilities should be equal (50/50)
            Assert.AreEqual(probs[0], probs[1], 0.001f, "Equal scores should yield equal probabilities");
            Assert.AreEqual(0.5f, probs[0], 0.001f, "Each probability should be 0.5");
        }
        
        [Test]
        public void Softmax_HigherScore_HigherProbability()
        {
            var lowAction = new UtilityAction("Low", new MockNode(NodeStatus.Success), new ConstantConsideration(0.3f));
            var highAction = new UtilityAction("High", new MockNode(NodeStatus.Success), new ConstantConsideration(0.7f));
            var selector = new UtilitySelector(42, lowAction, highAction);
            
            _brain.Criticality.SetTemperature(1f);
            
            selector.Execute(_brain);
            var probs = selector.GetLastProbabilities();
            
            Assert.AreEqual(2, probs.Count);
            Assert.Greater(probs[1], probs[0], "Higher score should have higher probability");
        }
        
        [Test]
        public void Softmax_LowTemperature_MoreDeterministic()
        {
            var lowAction = new UtilityAction("Low", new MockNode(NodeStatus.Success), new ConstantConsideration(0.3f));
            var highAction = new UtilityAction("High", new MockNode(NodeStatus.Success), new ConstantConsideration(0.7f));
            var selector = new UtilitySelector(42, lowAction, highAction);
            
            // Low temperature = more deterministic
            _brain.Criticality.SetTemperature(0.5f);
            selector.Execute(_brain);
            var lowTempProbs = selector.GetLastProbabilities();
            float lowTempDiff = lowTempProbs[1] - lowTempProbs[0];
            
            // Reset and test with high temperature
            selector.Reset();
            _brain.Criticality.SetTemperature(2.0f);
            selector.Execute(_brain);
            var highTempProbs = selector.GetLastProbabilities();
            float highTempDiff = highTempProbs[1] - highTempProbs[0];
            
            // Low temperature should create bigger probability difference
            Assert.Greater(lowTempDiff, highTempDiff, 
                "Low temperature should make probability distribution more skewed toward high scores");
        }
        
        [Test]
        public void Softmax_HighTemperature_MoreUniform()
        {
            var action1 = new UtilityAction("A", new MockNode(NodeStatus.Success), new ConstantConsideration(0.3f));
            var action2 = new UtilityAction("B", new MockNode(NodeStatus.Success), new ConstantConsideration(0.5f));
            var action3 = new UtilityAction("C", new MockNode(NodeStatus.Success), new ConstantConsideration(0.7f));
            var selector = new UtilitySelector(42, action1, action2, action3);
            
            // Very high temperature
            _brain.Criticality.SetTemperature(2.0f);
            selector.Execute(_brain);
            var probs = selector.GetLastProbabilities();
            
            // All probabilities should be reasonably close to uniform (1/3 ≈ 0.33)
            foreach (var prob in probs)
            {
                Assert.Greater(prob, 0.15f, "With high temp, even low-scoring actions should have decent probability");
                Assert.Less(prob, 0.5f, "With high temp, no single action should dominate");
            }
        }
        
        [Test]
        public void Softmax_ProbabilitiesSumToOne()
        {
            var action1 = new UtilityAction("A", new MockNode(NodeStatus.Success), new ConstantConsideration(0.2f));
            var action2 = new UtilityAction("B", new MockNode(NodeStatus.Success), new ConstantConsideration(0.5f));
            var action3 = new UtilityAction("C", new MockNode(NodeStatus.Success), new ConstantConsideration(0.8f));
            var selector = new UtilitySelector(42, action1, action2, action3);
            
            _brain.Criticality.SetTemperature(1f);
            selector.Execute(_brain);
            var probs = selector.GetLastProbabilities();
            
            float sum = 0f;
            foreach (var prob in probs)
            {
                sum += prob;
            }
            
            Assert.AreEqual(1f, sum, 0.001f, "Probabilities should sum to 1");
        }
        
        [Test]
        public void Softmax_ZeroScore_ZeroProbability()
        {
            var zeroAction = new UtilityAction("Zero", new MockNode(NodeStatus.Success), new ConstantConsideration(0f));
            var normalAction = new UtilityAction("Normal", new MockNode(NodeStatus.Success), new ConstantConsideration(0.5f));
            var selector = new UtilitySelector(42, zeroAction, normalAction);
            
            _brain.Criticality.SetTemperature(1f);
            selector.Execute(_brain);
            var probs = selector.GetLastProbabilities();
            
            // Zero-score action should have zero probability (enforced by UtilitySelector)
            Assert.AreEqual(0f, probs[0], 0.001f, "Zero-score action should have zero probability");
            Assert.AreEqual(1f, probs[1], 0.001f, "Only valid action should have 100% probability");
        }
        
        [Test]
        public void Softmax_SingleAction_FullProbability()
        {
            var action = new UtilityAction("Only", new MockNode(NodeStatus.Success), new ConstantConsideration(0.5f));
            var selector = new UtilitySelector(42, action);
            
            _brain.Criticality.SetTemperature(1f);
            selector.Execute(_brain);
            var probs = selector.GetLastProbabilities();
            
            Assert.AreEqual(1, probs.Count);
            Assert.AreEqual(1f, probs[0], 0.001f, "Single action should have 100% probability");
        }
        
        [Test]
        public void Softmax_ExtremeScoreDifference_AlmostDeterministic()
        {
            var lowAction = new UtilityAction("Low", new MockNode(NodeStatus.Success), new ConstantConsideration(0.01f));
            var highAction = new UtilityAction("High", new MockNode(NodeStatus.Success), new ConstantConsideration(0.99f));
            var selector = new UtilitySelector(42, lowAction, highAction);
            
            // Very low temperature
            _brain.Criticality.SetTemperature(0.5f);
            selector.Execute(_brain);
            var probs = selector.GetLastProbabilities();
            
            // High action should have significantly higher probability than low action
            // With softmax at temp 0.5, the high action gets ~87% probability
            Assert.Greater(probs[1], 0.80f, "With extreme score difference and low temp, high action should dominate");
            Assert.Greater(probs[1], probs[0] * 5f, "High action probability should be much greater than low action");
        }
    }
}
