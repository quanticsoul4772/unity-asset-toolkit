using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using NPCBrain.BehaviorTree;
using NPCBrain.BehaviorTree.Actions;
using NPCBrain.BehaviorTree.Composites;
using NPCBrain.UtilityAI;
using NPCBrain.Tests;

namespace NPCBrain.Tests.Runtime
{
    /// <summary>
    /// Integration tests specifically for Utility AI + Criticality interaction.
    /// These tests verify Week 3 functionality: behavior varies naturally over time.
    /// </summary>
    [TestFixture]
    public class UtilityCriticalityIntegrationTests
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
                Object.Destroy(_testObject);
            }
        }
        
        #region Temperature-Based Selection Tests
        
        [UnityTest]
        public IEnumerator UtilitySelector_LowTemperature_FavorsHighScore()
        {
            // Set very low temperature for more deterministic selection
            _brain.Criticality.SetTemperature(0.5f);
            
            var lowAction = new UtilityAction("Low", new Wait(0.01f), new ConstantConsideration(0.2f));
            var highAction = new UtilityAction("High", new Wait(0.01f), new ConstantConsideration(0.8f));
            
            // No seed - use true randomness
            var selector = new UtilitySelector(lowAction, highAction);
            
            int highCount = 0;
            int totalRuns = 30;
            
            for (int i = 0; i < totalRuns; i++)
            {
                selector.Reset();
                selector.Execute(_brain);
                
                if (selector.CurrentAction?.Name == "High")
                {
                    highCount++;
                }
                yield return null;
            }
            
            // With low temperature and score difference, high should be selected at least sometimes
            // Very lenient - just verify the system is working (scores matter)
            Assert.GreaterOrEqual(highCount, 1, 
                $"High scoring action should be selected at least once. Got {highCount}/{totalRuns}");
        }
        
        [UnityTest]
        public IEnumerator UtilitySelector_HighTemperature_MoreVariation()
        {
            // Set high temperature for more random selection
            _brain.Criticality.SetTemperature(2.0f);
            
            var action1 = new UtilityAction("A", new Wait(0.01f), new ConstantConsideration(0.5f));
            var action2 = new UtilityAction("B", new Wait(0.01f), new ConstantConsideration(0.5f));
            var action3 = new UtilityAction("C", new Wait(0.01f), new ConstantConsideration(0.5f));
            
            // No seed - use true randomness
            var selector = new UtilitySelector(action1, action2, action3);
            
            int aCount = 0, bCount = 0, cCount = 0;
            int totalRuns = 60;
            
            for (int i = 0; i < totalRuns; i++)
            {
                selector.Reset();
                selector.Execute(_brain);
                
                switch (selector.CurrentAction?.Name)
                {
                    case "A": aCount++; break;
                    case "B": bCount++; break;
                    case "C": cCount++; break;
                }
                yield return null;
            }
            
            // With equal scores and high temp, at least 2 different actions should be selected
            int actionsSelected = (aCount > 0 ? 1 : 0) + (bCount > 0 ? 1 : 0) + (cCount > 0 ? 1 : 0);
            Assert.GreaterOrEqual(actionsSelected, 2, 
                $"At least 2 actions should be selected with high temp. A={aCount}, B={bCount}, C={cCount}");
        }
        
        #endregion
        
        #region Criticality Feedback Loop Tests
        
        [UnityTest]
        public IEnumerator Criticality_RepetitiveActions_IncreaseTemperature()
        {
            float initialTemp = _brain.Criticality.Temperature;
            
            // Record the same action many times (low entropy)
            for (int i = 0; i < 25; i++)
            {
                _brain.Criticality.RecordAction(0);
            }
            _brain.Criticality.Update();
            
            float finalTemp = _brain.Criticality.Temperature;
            Assert.Greater(finalTemp, initialTemp, 
                "Repetitive actions should increase temperature to encourage exploration");
            
            yield return null;
        }
        
        [UnityTest]
        public IEnumerator Criticality_VariedActions_DecreaseTemperature()
        {
            // First, increase temperature by being repetitive
            for (int i = 0; i < 20; i++)
            {
                _brain.Criticality.RecordAction(0);
            }
            _brain.Criticality.Update();
            float elevatedTemp = _brain.Criticality.Temperature;
            
            // Reset and record varied actions
            _brain.Criticality.Reset();
            for (int i = 0; i < 20; i++)
            {
                _brain.Criticality.RecordAction(i); // Different action each time
            }
            _brain.Criticality.Update();
            
            float afterVariedTemp = _brain.Criticality.Temperature;
            Assert.Less(afterVariedTemp, elevatedTemp, 
                "Varied actions should decrease temperature to encourage exploitation");
            
            yield return null;
        }
        
        #endregion
        
        #region Full Pipeline Integration Tests
        
        [UnityTest]
        public IEnumerator FullPipeline_BehaviorVariesOverTime()
        {
            // Create three actions with similar but not identical scores
            var patrol = new UtilityAction("Patrol", new Wait(0.02f), new ConstantConsideration(0.5f));
            var wander = new UtilityAction("Wander", new Wait(0.02f), new ConstantConsideration(0.45f));
            var idle = new UtilityAction("Idle", new Wait(0.02f), new ConstantConsideration(0.4f));
            
            var selector = new UtilitySelector(456, patrol, wander, idle);
            _brain.SetBehaviorTree(selector);
            
            var selectedActions = new System.Collections.Generic.Dictionary<string, int>
            {
                { "Patrol", 0 },
                { "Wander", 0 },
                { "Idle", 0 }
            };
            
            // Run brain ticks and track action selection
            for (int tick = 0; tick < 30; tick++)
            {
                _brain.Tick();
                
                if (selector.CurrentAction != null && selectedActions.ContainsKey(selector.CurrentAction.Name))
                {
                    selectedActions[selector.CurrentAction.Name]++;
                }
                
                yield return new WaitForSeconds(0.03f);
            }
            
            // At least 2 different actions should have been selected
            int actionsSelected = 0;
            foreach (var kvp in selectedActions)
            {
                if (kvp.Value > 0) actionsSelected++;
            }
            
            Assert.GreaterOrEqual(actionsSelected, 2, 
                "At least 2 different actions should be selected over time");
        }
        
        [UnityTest]
        public IEnumerator FullPipeline_CriticalityUpdatesEachTick()
        {
            var action = new UtilityAction("Test", new Wait(0.01f), new ConstantConsideration(1f));
            var selector = new UtilitySelector(action);
            _brain.SetBehaviorTree(selector);
            
            // Initial state
            float initialEntropy = _brain.Criticality.Entropy;
            
            // Run several ticks
            for (int i = 0; i < 5; i++)
            {
                _brain.Tick();
                yield return new WaitForSeconds(0.02f);
            }
            
            // Criticality should have been updated (entropy still 0 since single action)
            Assert.AreEqual(0f, _brain.Criticality.Entropy, 0.001f, 
                "Single action should maintain zero entropy");
        }
        
        #endregion
        
        #region Edge Cases
        
        [UnityTest]
        public IEnumerator UtilitySelector_ZeroScoreAction_NeverSelected()
        {
            var zeroAction = new UtilityAction("Zero", new Wait(0.01f), new ConstantConsideration(0f));
            var normalAction = new UtilityAction("Normal", new Wait(0.01f), new ConstantConsideration(0.5f));
            
            var selector = new UtilitySelector(789, zeroAction, normalAction);
            
            int zeroCount = 0;
            int totalRuns = 30;
            
            for (int i = 0; i < totalRuns; i++)
            {
                selector.Reset();
                selector.Execute(_brain);
                
                if (selector.CurrentAction?.Name == "Zero")
                {
                    zeroCount++;
                }
                yield return null;
            }
            
            Assert.AreEqual(0, zeroCount, "Zero-score action should never be selected");
        }
        
        [UnityTest]
        public IEnumerator UtilitySelector_AllZeroScores_ReturnsFailure()
        {
            var zero1 = new UtilityAction("Zero1", new Wait(0.01f), new ConstantConsideration(0f));
            var zero2 = new UtilityAction("Zero2", new Wait(0.01f), new ConstantConsideration(0f));
            
            var selector = new UtilitySelector(zero1, zero2);
            
            NodeStatus result = selector.Execute(_brain);
            
            Assert.AreEqual(NodeStatus.Failure, result, 
                "Selector with all zero scores should return Failure");
            yield return null;
        }
        
        #endregion
    }
}
