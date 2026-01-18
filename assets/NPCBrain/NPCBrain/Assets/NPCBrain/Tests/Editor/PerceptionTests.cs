using NUnit.Framework;
using UnityEngine;
using NPCBrain.Perception;

namespace NPCBrain.Tests.Editor
{
    /// <summary>
    /// Unit tests for the perception system (Memory and TargetSelector).
    /// </summary>
    [TestFixture]
    public class PerceptionTests
    {
        #region Memory Tests
        
        [Test]
        public void Memory_StartsEmpty()
        {
            var memory = new Memory();
            Assert.AreEqual(0, memory.Count);
        }
        
        [Test]
        public void Memory_UpdateVisible_AddsTarget()
        {
            var memory = new Memory();
            var target = new GameObject("Target");
            
            memory.UpdateVisible(target, Vector3.one);
            
            Assert.AreEqual(1, memory.Count);
            Assert.IsTrue(memory.Remembers(target));
            
            Object.DestroyImmediate(target);
        }
        
        [Test]
        public void Memory_UpdateVisible_SetsCorrectPosition()
        {
            var memory = new Memory();
            var target = new GameObject("Target");
            Vector3 position = new Vector3(5f, 0f, 10f);
            
            memory.UpdateVisible(target, position);
            
            var mem = memory.GetMemory(target);
            Assert.AreEqual(position, mem.LastKnownPosition);
            
            Object.DestroyImmediate(target);
        }
        
        [Test]
        public void Memory_UpdateVisible_SetsConfidenceToOne()
        {
            var memory = new Memory();
            var target = new GameObject("Target");
            
            memory.UpdateVisible(target, Vector3.zero);
            
            var mem = memory.GetMemory(target);
            Assert.AreEqual(1f, mem.Confidence);
            
            Object.DestroyImmediate(target);
        }
        
        [Test]
        public void Memory_UpdateVisible_SetsIsVisible()
        {
            var memory = new Memory();
            var target = new GameObject("Target");
            
            memory.UpdateVisible(target, Vector3.zero);
            
            var mem = memory.GetMemory(target);
            Assert.IsTrue(mem.IsCurrentlyVisible);
            
            Object.DestroyImmediate(target);
        }
        
        [Test]
        public void Memory_MarkLost_SetsNotVisible()
        {
            var memory = new Memory();
            var target = new GameObject("Target");
            
            memory.UpdateVisible(target, Vector3.zero);
            memory.MarkLost(target);
            
            var mem = memory.GetMemory(target);
            Assert.IsFalse(mem.IsCurrentlyVisible);
            
            Object.DestroyImmediate(target);
        }
        
        [Test]
        public void Memory_Forget_RemovesTarget()
        {
            var memory = new Memory();
            var target = new GameObject("Target");
            
            memory.UpdateVisible(target, Vector3.zero);
            memory.Forget(target);
            
            Assert.AreEqual(0, memory.Count);
            Assert.IsFalse(memory.Remembers(target));
            
            Object.DestroyImmediate(target);
        }
        
        [Test]
        public void Memory_Clear_RemovesAllTargets()
        {
            var memory = new Memory();
            var target1 = new GameObject("Target1");
            var target2 = new GameObject("Target2");
            
            memory.UpdateVisible(target1, Vector3.zero);
            memory.UpdateVisible(target2, Vector3.one);
            memory.Clear();
            
            Assert.AreEqual(0, memory.Count);
            
            Object.DestroyImmediate(target1);
            Object.DestroyImmediate(target2);
        }
        
        [Test]
        public void Memory_GetMostRecentTarget_ReturnsLatest()
        {
            var memory = new Memory();
            var target1 = new GameObject("Target1");
            var target2 = new GameObject("Target2");
            
            memory.UpdateVisible(target1, Vector3.zero);
            memory.UpdateVisible(target2, Vector3.one);
            
            var mostRecent = memory.GetMostRecentTarget();
            Assert.AreEqual(target2, mostRecent);
            
            Object.DestroyImmediate(target1);
            Object.DestroyImmediate(target2);
        }
        
        [Test]
        public void Memory_GetMemory_ReturnsNullForUnknownTarget()
        {
            var memory = new Memory();
            var target = new GameObject("Target");
            
            var mem = memory.GetMemory(target);
            
            Assert.IsNull(mem);
            
            Object.DestroyImmediate(target);
        }
        
        [Test]
        public void Memory_Remembers_ReturnsFalseForNull()
        {
            var memory = new Memory();
            Assert.IsFalse(memory.Remembers(null));
        }
        
        [Test]
        public void Memory_GetPredictedPosition_ReturnsLastKnownWhenNoVelocity()
        {
            var memory = new Memory();
            var target = new GameObject("Target");
            Vector3 position = new Vector3(5f, 0f, 10f);
            
            memory.UpdateVisible(target, position);
            
            var predicted = memory.GetPredictedPosition(target);
            Assert.AreEqual(position, predicted);
            
            Object.DestroyImmediate(target);
        }
        
        #endregion
        
        #region TargetSelector Tests
        
        [Test]
        public void TargetSelector_DefaultWeights_NotNull()
        {
            var selector = new TargetSelector();
            Assert.IsNotNull(selector.Weights);
        }
        
        [Test]
        public void TargetSelector_CustomWeights_Applied()
        {
            var weights = new TargetSelector.ScoringWeights
            {
                Distance = 2f,
                Angle = 1f
            };
            var selector = new TargetSelector(weights);
            
            Assert.AreEqual(2f, selector.Weights.Distance);
            Assert.AreEqual(1f, selector.Weights.Angle);
        }
        
        [Test]
        public void TargetSelector_Evaluate_ReturnsEmptyWhenNoSensor()
        {
            var selector = new TargetSelector();
            var result = selector.Evaluate(null, null, Vector3.zero, Vector3.forward, null);
            
            Assert.AreEqual(0, result.Count);
        }
        
        [Test]
        public void TargetSelector_SelectBest_ReturnsNullWhenNoTargets()
        {
            var selector = new TargetSelector();
            var result = selector.SelectBest(null, null, Vector3.zero, Vector3.forward, null);
            
            Assert.IsNull(result);
        }
        
        [Test]
        public void TargetSelector_ScoredTargets_IsReadOnlyList()
        {
            var selector = new TargetSelector();
            selector.Evaluate(null, null, Vector3.zero, Vector3.forward, null);
            
            Assert.IsNotNull(selector.ScoredTargets);
        }
        
        [Test]
        public void TargetSelector_MaxDistance_DefaultValue()
        {
            var selector = new TargetSelector();
            Assert.AreEqual(50f, selector.MaxDistance);
        }
        
        [Test]
        public void TargetSelector_MaxDistance_CanBeSet()
        {
            var selector = new TargetSelector();
            selector.MaxDistance = 100f;
            Assert.AreEqual(100f, selector.MaxDistance);
        }
        
        #endregion
        
        #region ScoringWeights Tests
        
        [Test]
        public void ScoringWeights_DefaultValues()
        {
            var weights = new TargetSelector.ScoringWeights();
            
            Assert.AreEqual(1f, weights.Distance);
            Assert.AreEqual(0.5f, weights.Angle);
            Assert.AreEqual(0.3f, weights.Confidence);
            Assert.AreEqual(1f, weights.ThreatLevel);
            Assert.AreEqual(0.5f, weights.VisibilityBonus);
        }
        
        #endregion
        
        #region ScoredTarget Tests
        
        [Test]
        public void ScoredTarget_PropertiesCanBeSet()
        {
            var target = new GameObject("Target");
            var scored = new TargetSelector.ScoredTarget
            {
                Target = target,
                Score = 5f,
                Distance = 10f,
                Angle = 45f,
                IsVisible = true,
                Confidence = 0.8f
            };
            
            Assert.AreEqual(target, scored.Target);
            Assert.AreEqual(5f, scored.Score);
            Assert.AreEqual(10f, scored.Distance);
            Assert.AreEqual(45f, scored.Angle);
            Assert.IsTrue(scored.IsVisible);
            Assert.AreEqual(0.8f, scored.Confidence);
            
            Object.DestroyImmediate(target);
        }
        
        #endregion
        
        #region TargetMemory Tests
        
        [Test]
        public void TargetMemory_TimeSinceLastSeen_IsPositive()
        {
            var memory = new Memory.TargetMemory
            {
                LastSeenTime = Time.time - 1f
            };
            
            Assert.GreaterOrEqual(memory.TimeSinceLastSeen, 1f);
        }
        
        #endregion
    }
}
