using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using NPCBrain.BehaviorTree;
using NPCBrain.BehaviorTree.Actions;
using NPCBrain.BehaviorTree.Composites;
using NPCBrain.BehaviorTree.Conditions;
using NPCBrain.BehaviorTree.Decorators;
using NPCBrain.UtilityAI;
using NPCBrain.Tests;

namespace NPCBrain.Tests.Runtime
{
    [TestFixture]
    public class BehaviorTreeIntegrationTests
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
                UnityEngine.Object.Destroy(_testObject);
            }
        }
        
        #region Selector Integration Tests
        
        [UnityTest]
        public IEnumerator Selector_FallsBackToSecondChild_WhenFirstFails()
        {
            // Setup: First child checks for missing blackboard key, second always succeeds
            var selector = new Selector(
                new CheckBlackboard("missingKey"),
                new Wait(0.01f)
            );
            
            // First tick - CheckBlackboard fails, Wait starts
            NodeStatus result = selector.Execute(_brain);
            Assert.AreEqual(NodeStatus.Running, result);
            
            yield return new WaitForSeconds(0.02f);
            
            // Second tick - Wait completes
            result = selector.Execute(_brain);
            Assert.AreEqual(NodeStatus.Success, result);
        }
        
        [UnityTest]
        public IEnumerator Selector_SucceedsImmediately_WhenFirstChildSucceeds()
        {
            _brain.Blackboard.Set("existingKey", true);
            
            var selector = new Selector(
                new CheckBlackboard("existingKey"),
                new Wait(10f) // Should never reach this
            );
            
            NodeStatus result = selector.Execute(_brain);
            
            Assert.AreEqual(NodeStatus.Success, result);
            yield return null;
        }
        
        #endregion
        
        #region Sequence Integration Tests
        
        [UnityTest]
        public IEnumerator Sequence_ExecutesChildrenInOrder()
        {
            bool firstExecuted = false;
            bool secondExecuted = false;
            
            _brain.Blackboard.Set("step", 0);
            
            var sequence = new Sequence(
                new CheckBlackboard<int>("step", s => { firstExecuted = true; return s == 0; }),
                new CheckBlackboard<int>("step", s => { secondExecuted = true; return true; })
            );
            
            sequence.Execute(_brain);
            
            Assert.IsTrue(firstExecuted, "First child should execute");
            Assert.IsTrue(secondExecuted, "Second child should execute after first succeeds");
            yield return null;
        }
        
        [UnityTest]
        public IEnumerator Sequence_StopsOnFailure()
        {
            bool thirdExecuted = false;
            
            var sequence = new Sequence(
                new CheckBlackboard("exists"), // Will fail - key doesn't exist
                new CheckBlackboard<int>("other", _ => { thirdExecuted = true; return true; })
            );
            
            NodeStatus result = sequence.Execute(_brain);
            
            Assert.AreEqual(NodeStatus.Failure, result);
            Assert.IsFalse(thirdExecuted, "Third child should not execute after failure");
            yield return null;
        }
        
        #endregion
        
        #region UtilitySelector Integration Tests
        
        [UnityTest]
        public IEnumerator UtilitySelector_SelectsHighestScoringAction()
        {
            var lowAction = new UtilityAction("Low", new Wait(0.01f), new ConstantConsideration(0.2f));
            var highAction = new UtilityAction("High", new Wait(0.01f), new ConstantConsideration(0.9f));
            
            var selector = new UtilitySelector(42, lowAction, highAction); // Seeded for determinism
            
            // With low temperature (default 1.0), high scoring action should be heavily favored
            _brain.Criticality.SetTemperature(0.5f); // Low temp = more deterministic
            
            int highCount = 0;
            int iterations = 20;
            
            for (int i = 0; i < iterations; i++)
            {
                selector.Reset();
                selector.Execute(_brain);
                
                if (selector.CurrentAction?.Name == "High")
                {
                    highCount++;
                }
                yield return null;
            }
            
            // With low temperature and big score difference, high should be selected most of the time
            Assert.Greater(highCount, iterations / 2, "High scoring action should be selected more often");
        }
        
        [UnityTest]
        public IEnumerator UtilitySelector_HighTemperature_IncreasesVariation()
        {
            var action1 = new UtilityAction("A", new Wait(0.01f), new ConstantConsideration(0.5f));
            var action2 = new UtilityAction("B", new Wait(0.01f), new ConstantConsideration(0.5f));
            
            var selector = new UtilitySelector(123, action1, action2);
            
            // High temperature should give more variation
            _brain.Criticality.SetTemperature(2.0f);
            
            int action1Count = 0;
            int action2Count = 0;
            int iterations = 30;
            
            for (int i = 0; i < iterations; i++)
            {
                selector.Reset();
                selector.Execute(_brain);
                
                if (selector.CurrentAction?.Name == "A")
                    action1Count++;
                else if (selector.CurrentAction?.Name == "B")
                    action2Count++;
                    
                yield return null;
            }
            
            // With equal scores and high temp, both should be selected sometimes
            Assert.Greater(action1Count, 0, "Action A should be selected at least once");
            Assert.Greater(action2Count, 0, "Action B should be selected at least once");
        }
        
        [UnityTest]
        public IEnumerator UtilitySelector_RecordsCriticality_AfterActionCompletes()
        {
            var quickAction = new UtilityAction("Quick", new Wait(0.01f), new ConstantConsideration(1f));
            var selector = new UtilitySelector(quickAction);
            
            // Execute until action completes
            selector.Execute(_brain);
            yield return new WaitForSeconds(0.02f);
            selector.Execute(_brain);
            
            // Criticality should have recorded the action
            _brain.Criticality.Update();
            
            // After recording same action repeatedly, entropy should be low
            for (int i = 0; i < 10; i++)
            {
                selector.Reset();
                selector.Execute(_brain);
                yield return new WaitForSeconds(0.02f);
                selector.Execute(_brain);
            }
            
            _brain.Criticality.Update();
            Assert.AreEqual(0f, _brain.Criticality.Entropy, 0.001f, "Entropy should be 0 with single repeated action");
        }
        
        #endregion
        
        #region Criticality Integration Tests
        
        [UnityTest]
        public IEnumerator Criticality_AffectsUtilitySelection_OverTime()
        {
            // Create actions with different scores
            var explore = new UtilityAction("Explore", new Wait(0.01f), new ConstantConsideration(0.4f));
            var exploit = new UtilityAction("Exploit", new Wait(0.01f), new ConstantConsideration(0.6f));
            
            var selector = new UtilitySelector(999, explore, exploit);
            
            // Record many of the same action to trigger temperature increase
            for (int i = 0; i < 20; i++)
            {
                _brain.Criticality.RecordAction(0); // Always action 0
            }
            _brain.Criticality.Update();
            
            // Low entropy should increase temperature
            float tempAfterLowEntropy = _brain.Criticality.Temperature;
            Assert.Greater(tempAfterLowEntropy, 1f, "Temperature should increase with low entropy");
            
            yield return null;
        }
        
        [UnityTest]
        public IEnumerator Criticality_HighEntropy_DecreasesTemperature()
        {
            // Record varied actions
            for (int i = 0; i < 20; i++)
            {
                _brain.Criticality.RecordAction(i); // Different action each time
            }
            _brain.Criticality.Update();
            
            // High entropy should decrease temperature
            float temp = _brain.Criticality.Temperature;
            Assert.Less(temp, 1f, "Temperature should decrease with high entropy");
            
            yield return null;
        }
        
        #endregion
        
        #region Complex Tree Integration Tests
        
        [UnityTest]
        public IEnumerator ComplexTree_SelectorWithSequences_ExecutesCorrectly()
        {
            _brain.Blackboard.Set("health", 100);
            _brain.Blackboard.Set("hasTarget", false);
            
            // Complex tree: If has target -> chase, else -> patrol
            var tree = new Selector(
                new Sequence(
                    new CheckBlackboard("hasTarget"),
                    new Wait(0.01f) // Chase action
                ),
                new Wait(0.01f) // Patrol action (fallback)
            );
            
            // No target - should fall back to patrol
            NodeStatus result = tree.Execute(_brain);
            Assert.AreEqual(NodeStatus.Running, result);
            
            yield return new WaitForSeconds(0.02f);
            result = tree.Execute(_brain);
            Assert.AreEqual(NodeStatus.Success, result);
        }
        
        [UnityTest]
        public IEnumerator ComplexTree_DecoratorsWithComposites_WorkTogether()
        {
            var counter = 0;
            
            // Repeat a sequence 3 times
            var tree = new Repeater(
                new Sequence(
                    new CheckBlackboard<int>("counter", _ => { counter++; return true; }),
                    new CheckBlackboard<int>("counter", _ => true) // Always succeeds
                ),
                3
            );
            
            // Execute until complete
            NodeStatus result = NodeStatus.Running;
            int maxIterations = 10;
            int iterations = 0;
            
            while (result == NodeStatus.Running && iterations < maxIterations)
            {
                _brain.Blackboard.Set("counter", counter);
                result = tree.Execute(_brain);
                iterations++;
                yield return null;
            }
            
            Assert.AreEqual(NodeStatus.Success, result);
            Assert.AreEqual(3, counter, "Sequence should have executed 3 times");
        }
        
        #endregion
        
        #region Blackboard Integration Tests
        
        [UnityTest]
        public IEnumerator Blackboard_TTL_ExpiresCorrectly()
        {
            _brain.Blackboard.SetWithTTL("temporary", "value", 0.1f);
            
            Assert.IsTrue(_brain.Blackboard.Has("temporary"));
            
            yield return new WaitForSeconds(0.15f);
            _brain.Blackboard.CleanupExpired();
            
            Assert.IsFalse(_brain.Blackboard.Has("temporary"), "Key should expire after TTL");
        }
        
        [UnityTest]
        public IEnumerator Blackboard_UsedByConditionNodes_InRealTime()
        {
            var tree = new Selector(
                new Sequence(
                    new CheckBlackboard<int>("alertLevel", level => level > 5),
                    new Wait(0.01f) // High alert action
                ),
                new Wait(0.01f) // Normal action
            );
            
            // Low alert - should take normal path
            _brain.Blackboard.Set("alertLevel", 2);
            tree.Execute(_brain);
            yield return new WaitForSeconds(0.02f);
            tree.Reset();
            
            // High alert - should take alert path
            _brain.Blackboard.Set("alertLevel", 10);
            NodeStatus result = tree.Execute(_brain);
            Assert.AreEqual(NodeStatus.Running, result, "Should be in high alert sequence");
            
            yield return null;
        }
        
        #endregion
    }
}
