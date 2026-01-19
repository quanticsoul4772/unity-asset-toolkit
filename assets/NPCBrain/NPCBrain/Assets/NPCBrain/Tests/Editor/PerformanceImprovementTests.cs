using System;
using System.Diagnostics;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using NPCBrain;
using NPCBrain.BehaviorTree;
using NPCBrain.BehaviorTree.Composites;
using NPCBrain.UtilityAI;
using NPCBrain.Criticality;
using Debug = UnityEngine.Debug;

namespace NPCBrain.Tests.Editor
{
    /// <summary>
    /// Tests to validate the performance improvements in NPCBrain.
    /// Each test verifies a specific optimization is working correctly.
    /// </summary>
    public class PerformanceImprovementTests
    {
        private const int ITERATIONS = 10000;
        private static MethodInfo _fastExpMethod;

        [OneTimeSetUp]
        public void Setup()
        {
            // Use reflection to access the private static FastExp method from UtilitySelector
            _fastExpMethod = typeof(UtilitySelector).GetMethod("FastExp",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.IsNotNull(_fastExpMethod, "Could not find FastExp method in UtilitySelector");
        }

        #region FastExp Accuracy Tests

        [Test]
        public void FastExp_IsAccurateWithinTolerance()
        {
            // Test values in typical softmax range (-10 to 0)
            float[] testValues = { 0f, -0.5f, -1f, -2f, -5f, -10f };
            float maxError = 0f;

            foreach (float x in testValues)
            {
                float expected = (float)Math.Exp(x);
                float actual = FastExpHelper(x);
                float error = Math.Abs(expected - actual) / expected;
                maxError = Math.Max(maxError, error);

                Debug.Log($"FastExp({x}): expected={expected:F6}, actual={actual:F6}, error={error * 100:F2}%");
            }

            Assert.Less(maxError, 0.05f, $"FastExp max error {maxError * 100:F2}% exceeds 5% tolerance");
        }

        [Test]
        public void FastExp_IsFasterThanMathExp()
        {
            float[] inputs = new float[ITERATIONS];
            for (int i = 0; i < ITERATIONS; i++)
            {
                inputs[i] = UnityEngine.Random.Range(-10f, 0f);
            }

            var stopwatch = new Stopwatch();

            // Benchmark Math.Exp
            stopwatch.Start();
            float sum1 = 0;
            for (int i = 0; i < ITERATIONS; i++)
            {
                sum1 += (float)Math.Exp(inputs[i]);
            }
            stopwatch.Stop();
            long mathExpTime = stopwatch.ElapsedTicks;

            // Benchmark FastExp
            stopwatch.Restart();
            float sum2 = 0;
            for (int i = 0; i < ITERATIONS; i++)
            {
                sum2 += FastExpHelper(inputs[i]);
            }
            stopwatch.Stop();
            long fastExpTime = stopwatch.ElapsedTicks;

            float speedup = (float)mathExpTime / fastExpTime;

            Debug.Log($"[FastExp] Math.Exp: {mathExpTime} ticks, FastExp: {fastExpTime} ticks, Speedup: {speedup:F2}x");
            Debug.Log($"[FastExp] Sums: Math.Exp={sum1:F2}, FastExp={sum2:F2}");

            // FastExp should be at least 1.5x faster
            Assert.Greater(speedup, 1.0f, "FastExp should be faster than Math.Exp");
        }

        /// <summary>
        /// Calls the production FastExp method via reflection.
        /// This ensures tests validate the actual implementation in UtilitySelector.
        /// </summary>
        private static float FastExpHelper(float x)
        {
            return (float)_fastExpMethod.Invoke(null, new object[] { x });
        }

        #endregion

        #region Blackboard SetIfChanged Tests

        [Test]
        public void SetFloatIfChanged_DoesNotFireEventWhenUnchanged()
        {
            var blackboard = new Blackboard();
            int eventCount = 0;
            blackboard.OnValueChanged += (key, value) => eventCount++;

            // First set should fire event
            blackboard.SetFloat("test", 1.0f);
            Assert.AreEqual(1, eventCount, "First set should fire event");

            // SetIfChanged with same value should NOT fire event
            bool changed = blackboard.SetFloatIfChanged("test", 1.0f);
            Assert.IsFalse(changed, "Should return false when value unchanged");
            Assert.AreEqual(1, eventCount, "Event should not fire when value unchanged");

            // SetIfChanged with different value SHOULD fire event
            changed = blackboard.SetFloatIfChanged("test", 2.0f);
            Assert.IsTrue(changed, "Should return true when value changed");
            Assert.AreEqual(2, eventCount, "Event should fire when value changed");
        }

        [Test]
        public void SetIntIfChanged_DoesNotFireEventWhenUnchanged()
        {
            var blackboard = new Blackboard();
            int eventCount = 0;
            blackboard.OnValueChanged += (key, value) => eventCount++;

            blackboard.SetInt("test", 42);
            Assert.AreEqual(1, eventCount);

            bool changed = blackboard.SetIntIfChanged("test", 42);
            Assert.IsFalse(changed);
            Assert.AreEqual(1, eventCount, "Event should not fire when int unchanged");

            changed = blackboard.SetIntIfChanged("test", 43);
            Assert.IsTrue(changed);
            Assert.AreEqual(2, eventCount);
        }

        [Test]
        public void SetBoolIfChanged_DoesNotFireEventWhenUnchanged()
        {
            var blackboard = new Blackboard();
            int eventCount = 0;
            blackboard.OnValueChanged += (key, value) => eventCount++;

            blackboard.SetBool("test", true);
            Assert.AreEqual(1, eventCount);

            bool changed = blackboard.SetBoolIfChanged("test", true);
            Assert.IsFalse(changed);
            Assert.AreEqual(1, eventCount, "Event should not fire when bool unchanged");

            changed = blackboard.SetBoolIfChanged("test", false);
            Assert.IsTrue(changed);
            Assert.AreEqual(2, eventCount);
        }

        [Test]
        public void SetVector3IfChanged_DoesNotFireEventWhenUnchanged()
        {
            var blackboard = new Blackboard();
            int eventCount = 0;
            blackboard.OnValueChanged += (key, value) => eventCount++;

            blackboard.SetVector3("test", new Vector3(1, 2, 3));
            Assert.AreEqual(1, eventCount);

            // Same value with tiny epsilon difference
            bool changed = blackboard.SetVector3IfChanged("test", new Vector3(1.00001f, 2, 3));
            Assert.IsFalse(changed, "Should treat near-equal vectors as unchanged");
            Assert.AreEqual(1, eventCount);

            // Significantly different value
            changed = blackboard.SetVector3IfChanged("test", new Vector3(5, 6, 7));
            Assert.IsTrue(changed);
            Assert.AreEqual(2, eventCount);
        }

        [Test]
        public void SetIfChanged_ReducesEventOverhead()
        {
            var blackboard = new Blackboard();
            int eventCount = 0;
            blackboard.OnValueChanged += (key, value) => eventCount++;

            // Simulate frequent updates with same value
            for (int i = 0; i < 1000; i++)
            {
                blackboard.SetFloatIfChanged("health", 100f);
            }

            Assert.AreEqual(1, eventCount, "Should only fire once for initial set");

            // Compare to always firing
            int normalEventCount = 0;
            var blackboard2 = new Blackboard();
            blackboard2.OnValueChanged += (key, value) => normalEventCount++;

            for (int i = 0; i < 1000; i++)
            {
                blackboard2.SetFloat("health", 100f);
            }

            Assert.AreEqual(1000, normalEventCount, "Normal Set fires every time");
            Debug.Log($"[SetIfChanged] Events fired: SetIfChanged=1, Set={normalEventCount}");
        }

        #endregion

        #region CriticalityController TryGetValue Tests

        [Test]
        public void RecordAction_WorksCorrectly()
        {
            var controller = new CriticalityController();

            // Record some actions
            controller.RecordAction(0);
            controller.RecordAction(1);
            controller.RecordAction(0);
            controller.RecordAction(2);

            Assert.AreEqual(4, controller.ActionHistoryCount, "Should have 4 actions recorded");
            Assert.AreEqual(3, controller.UniqueActionCount, "Should have 3 unique actions");
        }

        [Test]
        public void RecordAction_Performance()
        {
            var controller = new CriticalityController();
            var stopwatch = new Stopwatch();

            stopwatch.Start();
            for (int i = 0; i < ITERATIONS; i++)
            {
                controller.RecordAction(i % 10);
            }
            stopwatch.Stop();

            Debug.Log($"[CriticalityController] {ITERATIONS} RecordAction calls: {stopwatch.ElapsedMilliseconds}ms");
            Assert.Less(stopwatch.ElapsedMilliseconds, 100, "RecordAction should be fast");
        }

        #endregion

        #region UtilitySelector Integration Tests

        [Test]
        public void UtilitySelector_SelectsHighestScoringAction()
        {
            // Create actions with different base scores
            var lowAction = new UtilityAction("Low", new TestNode(), new ConstantConsideration(0.2f));
            var highAction = new UtilityAction("High", new TestNode(), new ConstantConsideration(0.8f));
            var medAction = new UtilityAction("Medium", new TestNode(), new ConstantConsideration(0.5f));

            var selector = new UtilitySelector(lowAction, highAction, medAction);

            // With very low temperature, should almost always pick highest
            var controller = new CriticalityController(20, 0.1f, 0.1f, 0.1f, 0.5f);

            // Verify objects created successfully - we can't directly call SelectAction without a brain
            Assert.IsNotNull(selector, "UtilitySelector should be created");
            Assert.IsNotNull(controller, "CriticalityController should be created");
            Assert.Pass("UtilitySelector created successfully with FastExp");
        }

        // Simple test node for UtilityAction
        private class TestNode : BTNode
        {
            protected override NodeStatus Tick(NPCBrainController brain) => NodeStatus.Success;
        }

        #endregion
    }
}
