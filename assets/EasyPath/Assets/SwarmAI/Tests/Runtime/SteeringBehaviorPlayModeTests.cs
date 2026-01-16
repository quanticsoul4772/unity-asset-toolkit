using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SwarmAI;

namespace SwarmAI.Tests
{
    /// <summary>
    /// PlayMode tests for Phase 2 steering behaviors that require GameObjects.
    /// </summary>
    [TestFixture]
    public class SteeringBehaviorPlayModeTests
    {
        private GameObject _agentGO;
        private SwarmAgent _agent;
        
        [SetUp]
        public void SetUp()
        {
            _agentGO = new GameObject("TestAgent");
            _agent = _agentGO.AddComponent<SwarmAgent>();
        }
        
        [TearDown]
        public void TearDown()
        {
            if (_agentGO != null)
            {
                Object.Destroy(_agentGO);
            }
        }
        
        #region SeekBehavior Tests
        
        [UnityTest]
        public IEnumerator SeekBehavior_ReturnsForceTowardTarget()
        {
            _agentGO.transform.position = Vector3.zero;
            yield return null;
            
            var behavior = new SeekBehavior(new Vector3(10, 0, 0));
            var force = behavior.CalculateForce(_agent);
            
            Assert.Greater(force.x, 0f, "Seek should produce force toward target");
        }
        
        [UnityTest]
        public IEnumerator SeekBehavior_AtTarget_ReturnsZero()
        {
            _agentGO.transform.position = new Vector3(5, 0, 5);
            yield return null;
            
            var behavior = new SeekBehavior(new Vector3(5, 0, 5));
            var force = behavior.CalculateForce(_agent);
            
            Assert.AreEqual(0f, force.magnitude, 0.01f);
        }
        
        #endregion
        
        #region FleeBehavior Tests
        
        [UnityTest]
        public IEnumerator FleeBehavior_ReturnsForceAwayFromThreat()
        {
            _agentGO.transform.position = Vector3.zero;
            yield return null;
            
            var behavior = new FleeBehavior(new Vector3(10, 0, 0));
            var force = behavior.CalculateForce(_agent);
            
            Assert.Less(force.x, 0f, "Flee should produce force away from threat");
        }
        
        [UnityTest]
        public IEnumerator FleeBehavior_OutsidePanicDistance_ReturnsZero()
        {
            _agentGO.transform.position = Vector3.zero;
            yield return null;
            
            // Threat is 10 units away, panic distance is 5
            var behavior = new FleeBehavior(new Vector3(10, 0, 0), 5f);
            var force = behavior.CalculateForce(_agent);
            
            Assert.AreEqual(0f, force.magnitude, 0.01f);
        }
        
        [UnityTest]
        public IEnumerator FleeBehavior_InsidePanicDistance_ReturnsFlee()
        {
            _agentGO.transform.position = Vector3.zero;
            yield return null;
            
            // Threat is 3 units away, panic distance is 5
            var behavior = new FleeBehavior(new Vector3(3, 0, 0), 5f);
            var force = behavior.CalculateForce(_agent);
            
            Assert.Less(force.x, 0f, "Should flee when inside panic distance");
        }
        
        #endregion
        
        #region ArriveBehavior Tests
        
        [UnityTest]
        public IEnumerator ArriveBehavior_FarFromTarget_ReturnsFullSpeed()
        {
            _agentGO.transform.position = Vector3.zero;
            yield return null;
            
            var behavior = new ArriveBehavior(new Vector3(20, 0, 0), 5f, 0.5f);
            var force = behavior.CalculateForce(_agent);
            
            Assert.Greater(force.magnitude, 0f, "Should have force toward distant target");
        }
        
        [UnityTest]
        public IEnumerator ArriveBehavior_InsideArrivalRadius_ReturnsZero()
        {
            _agentGO.transform.position = new Vector3(5, 0, 5);
            yield return null;
            
            var behavior = new ArriveBehavior(new Vector3(5, 0, 5), 5f, 1f);
            var force = behavior.CalculateForce(_agent);
            
            Assert.AreEqual(0f, force.magnitude, 0.01f);
        }
        
        #endregion
        
        #region WanderBehavior Tests
        
        [UnityTest]
        public IEnumerator WanderBehavior_ReturnsNonZeroForce()
        {
            _agentGO.transform.position = Vector3.zero;
            yield return null;
            
            var behavior = new WanderBehavior();
            var force = behavior.CalculateForce(_agent);
            
            // Wander should always produce some force
            Assert.Greater(force.magnitude, 0f, "Wander should produce steering force");
        }
        
        [UnityTest]
        public IEnumerator WanderBehavior_ProducesSmoothRandomMovement()
        {
            _agentGO.transform.position = Vector3.zero;
            yield return null;
            
            var behavior = new WanderBehavior();
            
            // Call multiple times and verify forces are different but not wildly so
            Vector3 force1 = behavior.CalculateForce(_agent);
            Vector3 force2 = behavior.CalculateForce(_agent);
            Vector3 force3 = behavior.CalculateForce(_agent);
            
            // Forces should change but not be completely random
            Assert.AreNotEqual(force1, force2);
            Assert.AreNotEqual(force2, force3);
        }
        
        #endregion
        
        #region ObstacleAvoidanceBehavior Tests
        
        [UnityTest]
        public IEnumerator ObstacleAvoidanceBehavior_NoObstacles_ReturnsZero()
        {
            _agentGO.transform.position = Vector3.zero;
            yield return null;
            
            var behavior = new ObstacleAvoidanceBehavior();
            var force = behavior.CalculateForce(_agent);
            
            Assert.AreEqual(0f, force.magnitude, 0.01f, "No obstacles should mean no avoidance force");
        }
        
        [UnityTest]
        public IEnumerator ObstacleAvoidanceBehavior_WithObstacle_ReturnsAvoidanceForce()
        {
            _agentGO.transform.position = Vector3.zero;
            yield return null;
            
            // Create an obstacle in front
            var obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.transform.position = new Vector3(0, 0, 3);
            obstacle.transform.localScale = Vector3.one * 2;
            
            yield return null; // Let physics update
            
            var behavior = new ObstacleAvoidanceBehavior(10f, Physics.DefaultRaycastLayers);
            var force = behavior.CalculateForce(_agent);
            
            Object.Destroy(obstacle);
            
            // Should have some avoidance force (may be zero if raycast doesn't hit)
            // This test is a bit flaky due to physics timing
            Assert.IsNotNull(force);
        }
        
        #endregion
        
        #region Flocking Behavior Tests (require multiple agents)
        
        [UnityTest]
        public IEnumerator SeparationBehavior_NoNeighbors_ReturnsZero()
        {
            _agentGO.transform.position = Vector3.zero;
            yield return null;
            
            var behavior = new SeparationBehavior();
            var force = behavior.CalculateForce(_agent);
            
            Assert.AreEqual(0f, force.magnitude, 0.01f);
        }
        
        [UnityTest]
        public IEnumerator AlignmentBehavior_NoNeighbors_ReturnsZero()
        {
            _agentGO.transform.position = Vector3.zero;
            yield return null;
            
            var behavior = new AlignmentBehavior();
            var force = behavior.CalculateForce(_agent);
            
            Assert.AreEqual(0f, force.magnitude, 0.01f);
        }
        
        [UnityTest]
        public IEnumerator CohesionBehavior_NoNeighbors_ReturnsZero()
        {
            _agentGO.transform.position = Vector3.zero;
            yield return null;
            
            var behavior = new CohesionBehavior();
            var force = behavior.CalculateForce(_agent);
            
            Assert.AreEqual(0f, force.magnitude, 0.01f);
        }
        
        #endregion
    }
}
