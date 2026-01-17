using NUnit.Framework;
using UnityEngine;
using SwarmAI;

namespace SwarmAI.Tests
{
    /// <summary>
    /// Unit tests for SwarmAgent class.
    /// Tests property validation, behavior management, and state transitions.
    /// Note: Tests requiring GameObjects use PlayMode tests in Runtime folder.
    /// </summary>
    [TestFixture]
    public class SwarmAgentTests
    {
        #region Behavior Interface Tests
        
        private class TestBehavior : BehaviorBase
        {
            public override string Name => "TestBehavior";
            public Vector3 ForceToReturn { get; set; } = Vector3.forward;
            public int CalculateCallCount { get; private set; }
            
            public override Vector3 CalculateForce(SwarmAgent agent)
            {
                CalculateCallCount++;
                return ForceToReturn;
            }
        }
        
        [Test]
        public void BehaviorBase_DefaultWeight_IsOne()
        {
            var behavior = new TestBehavior();
            
            Assert.AreEqual(1f, behavior.Weight);
        }
        
        [Test]
        public void BehaviorBase_DefaultIsActive_IsTrue()
        {
            var behavior = new TestBehavior();
            
            Assert.IsTrue(behavior.IsActive);
        }
        
        [Test]
        public void BehaviorBase_Weight_CanBeSet()
        {
            var behavior = new TestBehavior();
            behavior.Weight = 2.5f;
            
            Assert.AreEqual(2.5f, behavior.Weight);
        }
        
        [Test]
        public void BehaviorBase_IsActive_CanBeToggled()
        {
            var behavior = new TestBehavior();
            
            behavior.IsActive = false;
            Assert.IsFalse(behavior.IsActive);
            
            behavior.IsActive = true;
            Assert.IsTrue(behavior.IsActive);
        }
        
        [Test]
        public void BehaviorBase_Name_ReturnsCorrectName()
        {
            var behavior = new TestBehavior();
            
            Assert.AreEqual("TestBehavior", behavior.Name);
        }
        
        #endregion
        
        #region Seek Behavior Tests
        
        [Test]
        public void SeekBehavior_Name_IsSeek()
        {
            var behavior = new SeekBehavior();
            
            Assert.AreEqual("Seek", behavior.Name);
        }
        
        [Test]
        public void SeekBehavior_CalculateForce_WithNullAgent_ReturnsZero()
        {
            var behavior = new SeekBehavior();
            
            var force = behavior.CalculateForce(null);
            
            Assert.AreEqual(Vector3.zero, force);
        }
        
        #endregion
        
        #region Flee Behavior Tests
        
        [Test]
        public void FleeBehavior_Name_IsFlee()
        {
            var behavior = new FleeBehavior();
            
            Assert.AreEqual("Flee", behavior.Name);
        }
        
        [Test]
        public void FleeBehavior_FleeRadius_CanBeSet()
        {
            var behavior = new FleeBehavior();
            behavior.FleeRadius = 15f;
            
            Assert.AreEqual(15f, behavior.FleeRadius);
        }
        
        [Test]
        public void FleeBehavior_FleeRadius_MinimumIsZero()
        {
            var behavior = new FleeBehavior();
            behavior.FleeRadius = -5f;
            
            Assert.AreEqual(0f, behavior.FleeRadius);
        }
        
        [Test]
        public void FleeBehavior_CalculateForce_WithNullAgent_ReturnsZero()
        {
            var behavior = new FleeBehavior();
            
            var force = behavior.CalculateForce(null);
            
            Assert.AreEqual(Vector3.zero, force);
        }
        
        #endregion
        
        #region Arrive Behavior Tests
        
        [Test]
        public void ArriveBehavior_Name_IsArrive()
        {
            var behavior = new ArriveBehavior();
            
            Assert.AreEqual("Arrive", behavior.Name);
        }
        
        [Test]
        public void ArriveBehavior_SlowingRadius_CanBeSet()
        {
            var behavior = new ArriveBehavior();
            behavior.SlowingRadius = 10f;
            
            Assert.AreEqual(10f, behavior.SlowingRadius);
        }
        
        [Test]
        public void ArriveBehavior_ArrivalRadius_CanBeSet()
        {
            var behavior = new ArriveBehavior();
            behavior.ArrivalRadius = 2f;
            
            Assert.AreEqual(2f, behavior.ArrivalRadius);
        }
        
        [Test]
        public void ArriveBehavior_CalculateForce_WithNullAgent_ReturnsZero()
        {
            var behavior = new ArriveBehavior();
            
            var force = behavior.CalculateForce(null);
            
            Assert.AreEqual(Vector3.zero, force);
        }
        
        #endregion
        
        #region Wander Behavior Tests
        
        [Test]
        public void WanderBehavior_Name_IsWander()
        {
            var behavior = new WanderBehavior();
            
            Assert.AreEqual("Wander", behavior.Name);
        }
        
        [Test]
        public void WanderBehavior_WanderRadius_CanBeSet()
        {
            var behavior = new WanderBehavior();
            behavior.WanderRadius = 5f;
            
            Assert.AreEqual(5f, behavior.WanderRadius);
        }
        
        [Test]
        public void WanderBehavior_WanderDistance_CanBeSet()
        {
            var behavior = new WanderBehavior();
            behavior.WanderDistance = 3f;
            
            Assert.AreEqual(3f, behavior.WanderDistance);
        }
        
        [Test]
        public void WanderBehavior_WanderJitter_CanBeSet()
        {
            var behavior = new WanderBehavior();
            behavior.WanderJitter = 0.5f;
            
            Assert.AreEqual(0.5f, behavior.WanderJitter);
        }
        
        [Test]
        public void WanderBehavior_CalculateForce_WithNullAgent_ReturnsZero()
        {
            var behavior = new WanderBehavior();
            
            var force = behavior.CalculateForce(null);
            
            Assert.AreEqual(Vector3.zero, force);
        }
        
        #endregion
        
        #region Separation Behavior Tests
        
        [Test]
        public void SeparationBehavior_Name_IsSeparation()
        {
            var behavior = new SeparationBehavior();
            
            Assert.AreEqual("Separation", behavior.Name);
        }
        
        [Test]
        public void SeparationBehavior_SeparationRadius_CanBeSet()
        {
            var behavior = new SeparationBehavior();
            behavior.SeparationRadius = 8f;
            
            Assert.AreEqual(8f, behavior.SeparationRadius);
        }
        
        [Test]
        public void SeparationBehavior_UseSquaredFalloff_DefaultsToTrue()
        {
            var behavior = new SeparationBehavior();
            
            Assert.IsTrue(behavior.UseSquaredFalloff);
        }
        
        [Test]
        public void SeparationBehavior_Constructor_WithRadius_SetsRadius()
        {
            var behavior = new SeparationBehavior(5f);
            
            Assert.AreEqual(5f, behavior.SeparationRadius);
        }
        
        [Test]
        public void SeparationBehavior_CalculateForce_WithNullAgent_ReturnsZero()
        {
            var behavior = new SeparationBehavior();
            
            var force = behavior.CalculateForce(null);
            
            Assert.AreEqual(Vector3.zero, force);
        }
        
        #endregion
        
        #region Alignment Behavior Tests
        
        [Test]
        public void AlignmentBehavior_Name_IsAlignment()
        {
            var behavior = new AlignmentBehavior();
            
            Assert.AreEqual("Alignment", behavior.Name);
        }
        
        [Test]
        public void AlignmentBehavior_AlignmentRadius_CanBeSet()
        {
            var behavior = new AlignmentBehavior();
            behavior.AlignmentRadius = 12f;
            
            Assert.AreEqual(12f, behavior.AlignmentRadius);
        }
        
        [Test]
        public void AlignmentBehavior_CalculateForce_WithNullAgent_ReturnsZero()
        {
            var behavior = new AlignmentBehavior();
            
            var force = behavior.CalculateForce(null);
            
            Assert.AreEqual(Vector3.zero, force);
        }
        
        #endregion
        
        #region Cohesion Behavior Tests
        
        [Test]
        public void CohesionBehavior_Name_IsCohesion()
        {
            var behavior = new CohesionBehavior();
            
            Assert.AreEqual("Cohesion", behavior.Name);
        }
        
        [Test]
        public void CohesionBehavior_CohesionRadius_CanBeSet()
        {
            var behavior = new CohesionBehavior();
            behavior.CohesionRadius = 20f;
            
            Assert.AreEqual(20f, behavior.CohesionRadius);
        }
        
        [Test]
        public void CohesionBehavior_Constructor_WithRadius_SetsRadius()
        {
            var behavior = new CohesionBehavior(15f);
            
            Assert.AreEqual(15f, behavior.CohesionRadius);
        }
        
        [Test]
        public void CohesionBehavior_CalculateForce_WithNullAgent_ReturnsZero()
        {
            var behavior = new CohesionBehavior();
            
            var force = behavior.CalculateForce(null);
            
            Assert.AreEqual(Vector3.zero, force);
        }
        
        #endregion
        
        #region Obstacle Avoidance Behavior Tests
        
        [Test]
        public void ObstacleAvoidanceBehavior_Name_IsObstacleAvoidance()
        {
            var behavior = new ObstacleAvoidanceBehavior();
            
            Assert.AreEqual("ObstacleAvoidance", behavior.Name);
        }
        
        [Test]
        public void ObstacleAvoidanceBehavior_DetectionDistance_CanBeSet()
        {
            var behavior = new ObstacleAvoidanceBehavior();
            behavior.DetectionDistance = 10f;
            
            Assert.AreEqual(10f, behavior.DetectionDistance);
        }
        
        [Test]
        public void ObstacleAvoidanceBehavior_CalculateForce_WithNullAgent_ReturnsZero()
        {
            var behavior = new ObstacleAvoidanceBehavior();
            
            var force = behavior.CalculateForce(null);
            
            Assert.AreEqual(Vector3.zero, force);
        }
        
        #endregion
        
        #region FormationSlotBehavior Tests
        
        [Test]
        public void FormationSlotBehavior_Name_IsFormationSlot()
        {
            var behavior = new FormationSlotBehavior();
            
            Assert.AreEqual("FormationSlot", behavior.Name);
        }
        
        [Test]
        public void FormationSlotBehavior_ArrivalRadius_CanBeSet()
        {
            var behavior = new FormationSlotBehavior();
            behavior.ArrivalRadius = 1.5f;
            
            Assert.AreEqual(1.5f, behavior.ArrivalRadius);
        }
        
        [Test]
        public void FormationSlotBehavior_DampingFactor_CanBeSet()
        {
            var behavior = new FormationSlotBehavior();
            behavior.DampingFactor = 0.8f;
            
            Assert.AreEqual(0.8f, behavior.DampingFactor);
        }
        
        [Test]
        public void FormationSlotBehavior_CalculateForce_WithNullAgent_ReturnsZero()
        {
            var behavior = new FormationSlotBehavior();
            
            var force = behavior.CalculateForce(null);
            
            Assert.AreEqual(Vector3.zero, force);
        }
        
        #endregion
        
        #region FollowLeaderBehavior Tests
        
        [Test]
        public void FollowLeaderBehavior_Name_IsFollowLeader()
        {
            var behavior = new FollowLeaderBehavior();
            
            Assert.AreEqual("FollowLeader", behavior.Name);
        }
        
        [Test]
        public void FollowLeaderBehavior_FollowDistance_CanBeSet()
        {
            var behavior = new FollowLeaderBehavior();
            behavior.FollowDistance = 5f;
            
            Assert.AreEqual(5f, behavior.FollowDistance);
        }
        
        [Test]
        public void FollowLeaderBehavior_CalculateForce_WithNullAgent_ReturnsZero()
        {
            var behavior = new FollowLeaderBehavior();
            
            var force = behavior.CalculateForce(null);
            
            Assert.AreEqual(Vector3.zero, force);
        }
        
        #endregion
    }
}
