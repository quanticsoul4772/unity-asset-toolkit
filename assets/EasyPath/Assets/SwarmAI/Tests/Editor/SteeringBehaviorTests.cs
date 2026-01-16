using NUnit.Framework;
using UnityEngine;
using SwarmAI;

namespace SwarmAI.Tests
{
    /// <summary>
    /// Unit tests for Phase 2 steering behaviors.
    /// Tests behavior properties and edge cases that don't require GameObjects.
    /// </summary>
    [TestFixture]
    public class SteeringBehaviorTests
    {
        #region SeekBehavior Tests
        
        [Test]
        public void SeekBehavior_Name_ReturnsSeek()
        {
            var behavior = new SeekBehavior();
            Assert.AreEqual("Seek", behavior.Name);
        }
        
        [Test]
        public void SeekBehavior_DefaultTarget_IsZero()
        {
            var behavior = new SeekBehavior();
            Assert.AreEqual(Vector3.zero, behavior.TargetPosition);
        }
        
        [Test]
        public void SeekBehavior_SetTargetPosition_UpdatesPosition()
        {
            var behavior = new SeekBehavior();
            behavior.TargetPosition = new Vector3(10, 0, 10);
            Assert.AreEqual(new Vector3(10, 0, 10), behavior.TargetPosition);
        }
        
        [Test]
        public void SeekBehavior_ConstructorWithPosition_SetsTarget()
        {
            var behavior = new SeekBehavior(new Vector3(5, 0, 5));
            Assert.AreEqual(new Vector3(5, 0, 5), behavior.TargetPosition);
        }
        
        [Test]
        public void SeekBehavior_NullAgent_ReturnsZero()
        {
            var behavior = new SeekBehavior(Vector3.forward * 10);
            Assert.AreEqual(Vector3.zero, behavior.CalculateForce(null));
        }
        
        #endregion
        
        #region FleeBehavior Tests
        
        [Test]
        public void FleeBehavior_Name_ReturnsFlee()
        {
            var behavior = new FleeBehavior();
            Assert.AreEqual("Flee", behavior.Name);
        }
        
        [Test]
        public void FleeBehavior_PanicDistance_ClampsToZero()
        {
            var behavior = new FleeBehavior();
            behavior.PanicDistance = -5f;
            Assert.AreEqual(0f, behavior.PanicDistance);
        }
        
        [Test]
        public void FleeBehavior_ConstructorWithPanicDistance_SetsPanicDistance()
        {
            var behavior = new FleeBehavior(Vector3.zero, 10f);
            Assert.AreEqual(10f, behavior.PanicDistance);
        }
        
        [Test]
        public void FleeBehavior_NullAgent_ReturnsZero()
        {
            var behavior = new FleeBehavior(Vector3.zero);
            Assert.AreEqual(Vector3.zero, behavior.CalculateForce(null));
        }
        
        #endregion
        
        #region ArriveBehavior Tests
        
        [Test]
        public void ArriveBehavior_Name_ReturnsArrive()
        {
            var behavior = new ArriveBehavior();
            Assert.AreEqual("Arrive", behavior.Name);
        }
        
        [Test]
        public void ArriveBehavior_DefaultSlowingRadius_IsFive()
        {
            var behavior = new ArriveBehavior();
            Assert.AreEqual(5f, behavior.SlowingRadius);
        }
        
        [Test]
        public void ArriveBehavior_SlowingRadius_ClampsToMin()
        {
            var behavior = new ArriveBehavior();
            behavior.SlowingRadius = -1f;
            Assert.AreEqual(0.1f, behavior.SlowingRadius);
        }
        
        [Test]
        public void ArriveBehavior_ArrivalRadius_ClampsToZero()
        {
            var behavior = new ArriveBehavior();
            behavior.ArrivalRadius = -1f;
            Assert.AreEqual(0f, behavior.ArrivalRadius);
        }
        
        [Test]
        public void ArriveBehavior_NullAgent_ReturnsZero()
        {
            var behavior = new ArriveBehavior(Vector3.forward * 10);
            Assert.AreEqual(Vector3.zero, behavior.CalculateForce(null));
        }
        
        #endregion
        
        #region WanderBehavior Tests
        
        [Test]
        public void WanderBehavior_Name_ReturnsWander()
        {
            var behavior = new WanderBehavior();
            Assert.AreEqual("Wander", behavior.Name);
        }
        
        [Test]
        public void WanderBehavior_DefaultRadius_IsFour()
        {
            var behavior = new WanderBehavior();
            Assert.AreEqual(4f, behavior.WanderRadius);
        }
        
        [Test]
        public void WanderBehavior_DefaultDistance_IsSix()
        {
            var behavior = new WanderBehavior();
            Assert.AreEqual(6f, behavior.WanderDistance);
        }
        
        [Test]
        public void WanderBehavior_WanderRadius_ClampsToMin()
        {
            var behavior = new WanderBehavior();
            behavior.WanderRadius = 0f;
            Assert.AreEqual(0.1f, behavior.WanderRadius);
        }
        
        [Test]
        public void WanderBehavior_WanderJitter_ClampsToZero()
        {
            var behavior = new WanderBehavior();
            behavior.WanderJitter = -1f;
            Assert.AreEqual(0f, behavior.WanderJitter);
        }
        
        [Test]
        public void WanderBehavior_NullAgent_ReturnsZero()
        {
            var behavior = new WanderBehavior();
            Assert.AreEqual(Vector3.zero, behavior.CalculateForce(null));
        }
        
        #endregion
        
        #region ObstacleAvoidanceBehavior Tests
        
        [Test]
        public void ObstacleAvoidanceBehavior_Name_ReturnsObstacleAvoidance()
        {
            var behavior = new ObstacleAvoidanceBehavior();
            Assert.AreEqual("Obstacle Avoidance", behavior.Name);
        }
        
        [Test]
        public void ObstacleAvoidanceBehavior_DefaultDetectionDistance_IsFive()
        {
            var behavior = new ObstacleAvoidanceBehavior();
            Assert.AreEqual(5f, behavior.DetectionDistance);
        }
        
        [Test]
        public void ObstacleAvoidanceBehavior_WhiskerAngle_ClampsTo90()
        {
            var behavior = new ObstacleAvoidanceBehavior();
            behavior.WhiskerAngle = 100f;
            Assert.AreEqual(90f, behavior.WhiskerAngle);
        }
        
        [Test]
        public void ObstacleAvoidanceBehavior_RayCount_ClampsTo1To9()
        {
            var behavior = new ObstacleAvoidanceBehavior();
            behavior.RayCount = 0;
            Assert.AreEqual(1, behavior.RayCount);
            behavior.RayCount = 15;
            Assert.AreEqual(9, behavior.RayCount);
        }
        
        [Test]
        public void ObstacleAvoidanceBehavior_NullAgent_ReturnsZero()
        {
            var behavior = new ObstacleAvoidanceBehavior();
            Assert.AreEqual(Vector3.zero, behavior.CalculateForce(null));
        }
        
        #endregion
        
        #region SeparationBehavior Tests
        
        [Test]
        public void SeparationBehavior_Name_ReturnsSeparation()
        {
            var behavior = new SeparationBehavior();
            Assert.AreEqual("Separation", behavior.Name);
        }
        
        [Test]
        public void SeparationBehavior_DefaultRadius_IsZero()
        {
            var behavior = new SeparationBehavior();
            Assert.AreEqual(0f, behavior.SeparationRadius);
        }
        
        [Test]
        public void SeparationBehavior_DefaultUsesSquaredFalloff()
        {
            var behavior = new SeparationBehavior();
            Assert.IsTrue(behavior.UseSquaredFalloff);
        }
        
        [Test]
        public void SeparationBehavior_NullAgent_ReturnsZero()
        {
            var behavior = new SeparationBehavior();
            Assert.AreEqual(Vector3.zero, behavior.CalculateForce(null));
        }
        
        #endregion
        
        #region AlignmentBehavior Tests
        
        [Test]
        public void AlignmentBehavior_Name_ReturnsAlignment()
        {
            var behavior = new AlignmentBehavior();
            Assert.AreEqual("Alignment", behavior.Name);
        }
        
        [Test]
        public void AlignmentBehavior_DefaultRadius_IsZero()
        {
            var behavior = new AlignmentBehavior();
            Assert.AreEqual(0f, behavior.AlignmentRadius);
        }
        
        [Test]
        public void AlignmentBehavior_NullAgent_ReturnsZero()
        {
            var behavior = new AlignmentBehavior();
            Assert.AreEqual(Vector3.zero, behavior.CalculateForce(null));
        }
        
        #endregion
        
        #region CohesionBehavior Tests
        
        [Test]
        public void CohesionBehavior_Name_ReturnsCohesion()
        {
            var behavior = new CohesionBehavior();
            Assert.AreEqual("Cohesion", behavior.Name);
        }
        
        [Test]
        public void CohesionBehavior_DefaultRadius_IsZero()
        {
            var behavior = new CohesionBehavior();
            Assert.AreEqual(0f, behavior.CohesionRadius);
        }
        
        [Test]
        public void CohesionBehavior_NullAgent_ReturnsZero()
        {
            var behavior = new CohesionBehavior();
            Assert.AreEqual(Vector3.zero, behavior.CalculateForce(null));
        }
        
        #endregion
        
        #region Behavior Weight and Active Tests
        
        [Test]
        public void AllBehaviors_DefaultWeight_IsOne()
        {
            Assert.AreEqual(1f, new SeekBehavior().Weight);
            Assert.AreEqual(1f, new FleeBehavior().Weight);
            Assert.AreEqual(1f, new ArriveBehavior().Weight);
            Assert.AreEqual(1f, new WanderBehavior().Weight);
            Assert.AreEqual(1f, new ObstacleAvoidanceBehavior().Weight);
            Assert.AreEqual(1f, new SeparationBehavior().Weight);
            Assert.AreEqual(1f, new AlignmentBehavior().Weight);
            Assert.AreEqual(1f, new CohesionBehavior().Weight);
        }
        
        [Test]
        public void AllBehaviors_DefaultIsActive_IsTrue()
        {
            Assert.IsTrue(new SeekBehavior().IsActive);
            Assert.IsTrue(new FleeBehavior().IsActive);
            Assert.IsTrue(new ArriveBehavior().IsActive);
            Assert.IsTrue(new WanderBehavior().IsActive);
            Assert.IsTrue(new ObstacleAvoidanceBehavior().IsActive);
            Assert.IsTrue(new SeparationBehavior().IsActive);
            Assert.IsTrue(new AlignmentBehavior().IsActive);
            Assert.IsTrue(new CohesionBehavior().IsActive);
        }
        
        #endregion
    }
}
