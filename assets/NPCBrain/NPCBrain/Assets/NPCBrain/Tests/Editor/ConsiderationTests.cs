using NUnit.Framework;
using UnityEngine;
using NPCBrain.UtilityAI;
using NPCBrain.UtilityAI.Curves;
using NPCBrain.Tests;

namespace NPCBrain.Tests.Editor
{
    /// <summary>
    /// Unit tests for Consideration classes used in Utility AI.
    /// </summary>
    [TestFixture]
    public class ConsiderationTests
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
                Object.DestroyImmediate(_testObject);
            }
        }
        
        #region ConstantConsideration Tests
        
        [Test]
        public void ConstantConsideration_ReturnsConstantValue()
        {
            var consideration = new ConstantConsideration(0.75f);
            
            float score = consideration.Score(_brain);
            
            Assert.AreEqual(0.75f, score, 0.001f);
        }
        
        [Test]
        public void ConstantConsideration_ClampsToValidRange()
        {
            var tooHigh = new ConstantConsideration(1.5f);
            var tooLow = new ConstantConsideration(-0.5f);
            
            Assert.AreEqual(1f, tooHigh.Score(_brain), 0.001f);
            Assert.AreEqual(0f, tooLow.Score(_brain), 0.001f);
        }
        
        #endregion
        
        #region BlackboardConsideration Tests
        
        [Test]
        public void BlackboardConsideration_ReadsFromBlackboard()
        {
            _brain.Blackboard.Set("health", 75f);
            var consideration = new BlackboardConsideration<float>(
                "Health", "health", 
                h => h / 100f, 
                0f);
            
            float score = consideration.Score(_brain);
            
            Assert.AreEqual(0.75f, score, 0.001f);
        }
        
        [Test]
        public void BlackboardConsideration_UsesDefaultWhenKeyMissing()
        {
            var consideration = new BlackboardConsideration<float>(
                "MissingKey", "nonexistent", 
                v => v, 
                0.5f);
            
            float score = consideration.Score(_brain);
            
            Assert.AreEqual(0.5f, score, 0.001f);
        }
        
        [Test]
        public void BlackboardConsideration_BooleanNormalizer()
        {
            _brain.Blackboard.Set("hasTarget", true);
            var consideration = new BlackboardConsideration<bool>(
                "HasTarget", "hasTarget", 
                b => b ? 1f : 0f, 
                false);
            
            float score = consideration.Score(_brain);
            
            Assert.AreEqual(1f, score, 0.001f);
        }
        
        [Test]
        public void BlackboardConsideration_GameObjectNullCheck()
        {
            var consideration = new BlackboardConsideration<GameObject>(
                "Target", "target", 
                go => go != null ? 1f : 0f, 
                null);
            
            float score = consideration.Score(_brain);
            
            Assert.AreEqual(0f, score, 0.001f);
            
            // Now set a target
            var target = new GameObject("Target");
            _brain.Blackboard.Set("target", target);
            
            score = consideration.Score(_brain);
            Assert.AreEqual(1f, score, 0.001f);
            
            Object.DestroyImmediate(target);
        }
        
        [Test]
        public void BlackboardConsideration_ExposesKey()
        {
            var consideration = new BlackboardConsideration<float>(
                "Test", "myKey", v => v, 0f);
            
            Assert.AreEqual("myKey", consideration.Key);
        }
        
        #endregion
        
        #region DistanceConsideration Tests
        
        [Test]
        public void DistanceConsideration_InvertedScore_CloserIsHigher()
        {
            // Target at same position as NPC
            var consideration = new DistanceConsideration(
                "Distance",
                brain => brain.transform.position,
                10f,
                true); // inverted - closer = higher
            
            float score = consideration.Score(_brain);
            
            // At distance 0, inverted score should be 1
            Assert.AreEqual(1f, score, 0.001f);
        }
        
        [Test]
        public void DistanceConsideration_NonInverted_FartherIsHigher()
        {
            // Target at same position
            var consideration = new DistanceConsideration(
                "Distance",
                brain => brain.transform.position,
                10f,
                false); // not inverted - farther = higher
            
            float score = consideration.Score(_brain);
            
            // At distance 0, non-inverted score should be 0
            Assert.AreEqual(0f, score, 0.001f);
        }
        
        [Test]
        public void DistanceConsideration_AtMaxDistance_ScoresCorrectly()
        {
            float maxDistance = 10f;
            Vector3 targetPos = _testObject.transform.position + Vector3.forward * maxDistance;
            
            var inverted = new DistanceConsideration(
                "Distance",
                brain => targetPos,
                maxDistance,
                true);
            
            var nonInverted = new DistanceConsideration(
                "Distance",
                brain => targetPos,
                maxDistance,
                false);
            
            Assert.AreEqual(0f, inverted.Score(_brain), 0.001f);
            Assert.AreEqual(1f, nonInverted.Score(_brain), 0.001f);
        }
        
        [Test]
        public void DistanceConsideration_HalfDistance_ScoresHalf()
        {
            float maxDistance = 10f;
            Vector3 targetPos = _testObject.transform.position + Vector3.forward * (maxDistance / 2f);
            
            var inverted = new DistanceConsideration(
                "Distance",
                brain => targetPos,
                maxDistance,
                true);
            
            Assert.AreEqual(0.5f, inverted.Score(_brain), 0.001f);
        }
        
        [Test]
        public void DistanceConsideration_BeyondMaxDistance_ClampedToZeroOrOne()
        {
            float maxDistance = 10f;
            Vector3 targetPos = _testObject.transform.position + Vector3.forward * 20f; // Beyond max
            
            var inverted = new DistanceConsideration(
                "Distance",
                brain => targetPos,
                maxDistance,
                true);
            
            // Should be clamped to 0 (inverted, beyond max)
            Assert.AreEqual(0f, inverted.Score(_brain), 0.001f);
        }
        
        #endregion
        
        #region TimeConsideration Tests
        
        [Test]
        public void TimeConsideration_NeverExecuted_ReturnsOne()
        {
            // Initialize with a timestamp far in the past
            _brain.Blackboard.Set("lastActionTime", -100f);
            
            var consideration = new TimeConsideration(
                "Cooldown", "lastActionTime", 5f);
            
            float score = consideration.Score(_brain);
            
            // Time.time is 0 in tests, so elapsed = 0 - (-100) = 100
            // 100 / 5 = 20, clamped to 1
            Assert.AreEqual(1f, score, 0.001f);
        }
        
        [Test]
        public void TimeConsideration_JustExecuted_ReturnsZero()
        {
            _brain.Blackboard.Set("lastActionTime", Time.time);
            
            var consideration = new TimeConsideration(
                "Cooldown", "lastActionTime", 5f);
            
            float score = consideration.Score(_brain);
            
            // Elapsed time is ~0, so score is ~0
            Assert.AreEqual(0f, score, 0.05f); // Small tolerance for Time.time precision
        }
        
        [Test]
        public void TimeConsideration_MissingKey_UsesDefault()
        {
            // Don't set any timestamp - should use default of -maxTime
            var consideration = new TimeConsideration(
                "Cooldown", "nonexistentKey", 5f);
            
            float score = consideration.Score(_brain);
            
            // Default is -maxTime, so elapsed = Time.time - (-5) = Time.time + 5
            // At least 5 seconds elapsed, so score >= 1
            Assert.GreaterOrEqual(score, 0.9f);
        }
        
        #endregion
        
        #region Response Curve Tests
        
        [Test]
        public void LinearCurve_PassesThroughUnchanged()
        {
            var curve = new LinearCurve();
            
            Assert.AreEqual(0f, curve.Evaluate(0f), 0.001f);
            Assert.AreEqual(0.5f, curve.Evaluate(0.5f), 0.001f);
            Assert.AreEqual(1f, curve.Evaluate(1f), 0.001f);
        }
        
        [Test]
        public void Consideration_AppliesResponseCurve()
        {
            _brain.Blackboard.Set("value", 0.5f);
            
            // Create a consideration with an exponential curve
            var consideration = new BlackboardConsideration<float>(
                "Test", "value", v => v, 0f, new ExponentialCurve(2f));
            
            float score = consideration.Score(_brain);
            
            // 0.5^2 = 0.25
            Assert.AreEqual(0.25f, score, 0.01f);
        }
        
        #endregion
        
        #region RangeConsideration Tests
        
        [Test]
        public void RangeConsideration_WithinRange_ScalesCorrectly()
        {
            _brain.Blackboard.Set("alertLevel", 0.5f);
            
            var consideration = new RangeConsideration(
                "AlertRange",
                brain => brain.Blackboard.Get("alertLevel", 0f),
                0f, 1f); // min, max
            
            float score = consideration.Score(_brain);
            
            Assert.AreEqual(0.5f, score, 0.001f);
        }
        
        [Test]
        public void RangeConsideration_BelowMin_ReturnsZero()
        {
            _brain.Blackboard.Set("value", -5f);
            
            var consideration = new RangeConsideration(
                "Range",
                brain => brain.Blackboard.Get("value", 0f),
                0f, 10f);
            
            float score = consideration.Score(_brain);
            
            Assert.AreEqual(0f, score, 0.001f);
        }
        
        [Test]
        public void RangeConsideration_AboveMax_ReturnsOne()
        {
            _brain.Blackboard.Set("value", 15f);
            
            var consideration = new RangeConsideration(
                "Range",
                brain => brain.Blackboard.Get("value", 0f),
                0f, 10f);
            
            float score = consideration.Score(_brain);
            
            Assert.AreEqual(1f, score, 0.001f);
        }
        
        #endregion
    }
}
