using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SwarmAI;

namespace SwarmAI.Tests
{
    /// <summary>
    /// Integration tests for flocking behaviors with actual multi-agent setups.
    /// These tests create a SwarmManager and multiple SwarmAgents to verify
    /// that Separation, Alignment, and Cohesion behaviors work correctly
    /// when neighbors are present.
    /// </summary>
    [TestFixture]
    public class FlockingIntegrationTests
    {
        private GameObject _managerGO;
        private SwarmManager _manager;
        private List<GameObject> _agentGOs;
        private List<SwarmAgent> _agents;
        
        [SetUp]
        public void SetUp()
        {
            // Create SwarmManager
            _managerGO = new GameObject("TestSwarmManager");
            _manager = _managerGO.AddComponent<SwarmManager>();
            
            _agentGOs = new List<GameObject>();
            _agents = new List<SwarmAgent>();
        }
        
        [TearDown]
        public void TearDown()
        {
            // Clean up agents first (they unregister from manager in OnDisable)
            foreach (var go in _agentGOs)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }
            _agentGOs.Clear();
            _agents.Clear();
            
            // Then clean up manager
            if (_managerGO != null)
            {
                Object.DestroyImmediate(_managerGO);
            }
        }
        
        /// <summary>
        /// Helper to create a SwarmAgent at a specific position.
        /// Default neighborRadius is 10f to ensure agents can detect each other in tests.
        /// </summary>
        private SwarmAgent CreateAgent(Vector3 position, float neighborRadius = 10f)
        {
            var go = new GameObject($"Agent_{_agents.Count}");
            go.transform.position = position;
            
            var agent = go.AddComponent<SwarmAgent>();
            agent.NeighborRadius = neighborRadius;
            agent.MaxSpeed = 5f;
            agent.MaxForce = 10f;
            
            _agentGOs.Add(go);
            _agents.Add(agent);
            
            return agent;
        }
        
        /// <summary>
        /// Helper to simulate agent velocity by setting the private _velocity field.
        /// Uses reflection since velocity is read-only in the public API.
        /// </summary>
        private void SetAgentVelocity(SwarmAgent agent, Vector3 velocity)
        {
            var velocityField = typeof(SwarmAgent).GetField("_velocity", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (velocityField == null)
            {
                throw new System.InvalidOperationException("Could not find _velocity field on SwarmAgent - field may have been renamed");
            }
            velocityField.SetValue(agent, velocity);
        }
        
        #region SeparationBehavior Tests
        
        [UnityTest]
        public IEnumerator SeparationBehavior_WithCloseNeighbor_ReturnsPushAwayForce()
        {
            // Create main agent at origin
            var mainAgent = CreateAgent(Vector3.zero);
            
            // Create neighbor agent very close (2 units away on X axis)
            CreateAgent(new Vector3(2f, 0f, 0f));
            
            // Wait for SwarmManager to register agents and update spatial hash
            yield return null; // Let OnEnable run
            yield return new WaitForEndOfFrame(); // Let LateUpdate run
            yield return null; // Extra frame for spatial hash update
            
            // Verify manager initialized and agents are registered
            Assert.IsNotNull(_manager.Settings, "SwarmManager should have settings initialized");
            Assert.AreEqual(2, _manager.AgentCount, "Both agents should be registered");
            
            // Create separation behavior
            var separation = new SeparationBehavior();
            
            // Calculate force for main agent
            var force = separation.CalculateForce(mainAgent);
            
            // Main agent should be pushed AWAY from neighbor (negative X direction)
            Assert.Less(force.x, 0f, "Separation force should push agent away from neighbor (negative X)");
            Assert.Greater(force.magnitude, 0f, "Separation force should have magnitude > 0");
        }
        
        [UnityTest]
        public IEnumerator SeparationBehavior_WithMultipleNeighbors_ReturnsCombinedForce()
        {
            // Create main agent at origin
            var mainAgent = CreateAgent(Vector3.zero);
            
            // Create neighbors surrounding the main agent
            CreateAgent(new Vector3(2f, 0f, 0f));   // Right
            CreateAgent(new Vector3(-2f, 0f, 0f));  // Left
            CreateAgent(new Vector3(0f, 0f, 2f));   // Front
            
            yield return null;
            yield return new WaitForEndOfFrame();
            yield return null;
            
            Assert.AreEqual(4, _manager.AgentCount, "All agents should be registered");
            
            var separation = new SeparationBehavior();
            var force = separation.CalculateForce(mainAgent);
            
            // With neighbors on left, right, and front, the combined force should push back (negative Z)
            // Left and right should cancel out somewhat
            Assert.Less(force.z, 0f, "Combined separation force should push agent backward (away from front neighbor)");
            Assert.Greater(force.magnitude, 0f, "Separation force should have magnitude > 0");
        }
        
        [UnityTest]
        public IEnumerator SeparationBehavior_NeighborOutsideRadius_ReturnsZero()
        {
            // Create main agent at origin with small neighbor radius
            var mainAgent = CreateAgent(Vector3.zero, 3f);
            
            // Create neighbor agent far away (5 units, outside 3 unit radius)
            CreateAgent(new Vector3(5f, 0f, 0f), 3f);
            
            yield return null;
            yield return new WaitForEndOfFrame();
            yield return null;
            
            var separation = new SeparationBehavior();
            var force = separation.CalculateForce(mainAgent);
            
            // Neighbor is outside radius, so no separation force
            Assert.AreEqual(0f, force.magnitude, 0.01f, "No separation force when neighbor is outside radius");
        }
        
        [UnityTest]
        public IEnumerator SeparationBehavior_SquaredVsLinearFalloff_ProducesDifferentForces()
        {
            // Create main agent at origin
            var mainAgent = CreateAgent(Vector3.zero);
            
            // Create neighbor at moderate distance
            CreateAgent(new Vector3(3f, 0f, 0f));
            
            yield return null;
            yield return new WaitForEndOfFrame();
            yield return null;
            
            // Test with squared falloff (default)
            var separationSquared = new SeparationBehavior(10f, true);
            var forceSquared = separationSquared.CalculateForce(mainAgent);
            
            // Test with linear falloff
            var separationLinear = new SeparationBehavior(10f, false);
            var forceLinear = separationLinear.CalculateForce(mainAgent);
            
            // Both should produce forces in the same direction (negative X)
            Assert.Less(forceSquared.x, 0f, "Squared falloff should push away");
            Assert.Less(forceLinear.x, 0f, "Linear falloff should push away");
            
            // Forces should be different magnitudes due to different falloff calculations
            Assert.AreNotEqual(forceSquared.magnitude, forceLinear.magnitude, 0.1f, 
                "Squared and linear falloff should produce different force magnitudes");
        }
        
        #endregion
        
        #region AlignmentBehavior Tests
        
        [UnityTest]
        public IEnumerator AlignmentBehavior_WithMovingNeighbor_SteersToMatchVelocity()
        {
            // Create main agent at origin, stationary
            var mainAgent = CreateAgent(Vector3.zero);
            
            // Create moving neighbor
            var neighborAgent = CreateAgent(new Vector3(2f, 0f, 0f));
            
            yield return null;
            yield return new WaitForEndOfFrame();
            yield return null;
            
            // Set neighbor velocity to move forward (positive Z)
            SetAgentVelocity(neighborAgent, new Vector3(0f, 0f, 5f));
            
            var alignment = new AlignmentBehavior();
            var force = alignment.CalculateForce(mainAgent);
            
            // Main agent should steer to match neighbor's velocity (positive Z)
            Assert.Greater(force.z, 0f, "Alignment force should steer toward neighbor's velocity direction");
        }
        
        [UnityTest]
        public IEnumerator AlignmentBehavior_WithMultipleMovingNeighbors_SteersToAverageVelocity()
        {
            // Create main agent at origin
            var mainAgent = CreateAgent(Vector3.zero);
            
            // Create neighbors moving in different directions
            var neighbor1 = CreateAgent(new Vector3(2f, 0f, 0f));
            var neighbor2 = CreateAgent(new Vector3(-2f, 0f, 0f));
            
            yield return null;
            yield return new WaitForEndOfFrame();
            yield return null;
            
            // Neighbor 1 moving forward (Z+), neighbor 2 moving forward (Z+)
            // Both moving in same direction
            SetAgentVelocity(neighbor1, new Vector3(0f, 0f, 5f));
            SetAgentVelocity(neighbor2, new Vector3(0f, 0f, 5f));
            
            var alignment = new AlignmentBehavior();
            var force = alignment.CalculateForce(mainAgent);
            
            // Main agent should steer forward to match average velocity
            Assert.Greater(force.z, 0f, "Alignment force should match average neighbor velocity (forward)");
        }
        
        [UnityTest]
        public IEnumerator AlignmentBehavior_WithStationaryNeighbors_ReturnsZero()
        {
            // Create main agent at origin
            var mainAgent = CreateAgent(Vector3.zero);
            
            // Create stationary neighbors (velocity = 0)
            CreateAgent(new Vector3(2f, 0f, 0f));
            CreateAgent(new Vector3(-2f, 0f, 0f));
            
            yield return null;
            yield return new WaitForEndOfFrame();
            yield return null;
            
            var alignment = new AlignmentBehavior();
            var force = alignment.CalculateForce(mainAgent);
            
            // Stationary neighbors should not produce alignment force
            Assert.AreEqual(0f, force.magnitude, 0.01f, "No alignment force with stationary neighbors");
        }
        
        [UnityTest]
        public IEnumerator AlignmentBehavior_CustomRadius_OnlyConsidersNearbyNeighbors()
        {
            // Create main agent at origin with large neighbor radius
            var mainAgent = CreateAgent(Vector3.zero, 10f);
            
            // Create close neighbor moving right
            var closeNeighbor = CreateAgent(new Vector3(2f, 0f, 0f), 10f);
            
            // Create far neighbor moving left
            var farNeighbor = CreateAgent(new Vector3(8f, 0f, 0f), 10f);
            
            yield return null;
            yield return new WaitForEndOfFrame();
            yield return null;
            
            SetAgentVelocity(closeNeighbor, new Vector3(5f, 0f, 0f));  // Moving right
            SetAgentVelocity(farNeighbor, new Vector3(-5f, 0f, 0f));   // Moving left
            
            // Use custom small radius that only includes close neighbor
            var alignment = new AlignmentBehavior(3f);
            var force = alignment.CalculateForce(mainAgent);
            
            // Should only align with close neighbor (moving right)
            Assert.Greater(force.x, 0f, "Should align with close neighbor moving right");
        }
        
        #endregion
        
        #region CohesionBehavior Tests
        
        [UnityTest]
        public IEnumerator CohesionBehavior_WithNeighbors_SeeksTowardCenterOfMass()
        {
            // Create main agent at origin
            var mainAgent = CreateAgent(Vector3.zero);
            
            // Create neighbors to the right
            CreateAgent(new Vector3(5f, 0f, 0f));
            CreateAgent(new Vector3(7f, 0f, 0f));
            
            yield return null;
            yield return new WaitForEndOfFrame();
            yield return null;
            
            var cohesion = new CohesionBehavior();
            var force = cohesion.CalculateForce(mainAgent);
            
            // Center of mass of neighbors is at (6, 0, 0), so agent should seek right
            Assert.Greater(force.x, 0f, "Cohesion force should seek toward center of mass (right)");
        }
        
        [UnityTest]
        public IEnumerator CohesionBehavior_WithSurroundingNeighbors_SeeksTowardCenter()
        {
            // Create main agent offset from neighbor center
            var mainAgent = CreateAgent(new Vector3(-3f, 0f, 0f));
            
            // Create neighbors forming a cluster at origin
            CreateAgent(new Vector3(0f, 0f, 1f));
            CreateAgent(new Vector3(0f, 0f, -1f));
            CreateAgent(new Vector3(1f, 0f, 0f));
            CreateAgent(new Vector3(-1f, 0f, 0f));
            
            yield return null;
            yield return new WaitForEndOfFrame();
            yield return null;
            
            var cohesion = new CohesionBehavior();
            var force = cohesion.CalculateForce(mainAgent);
            
            // Center of mass is approximately at origin, so agent should seek right (positive X)
            Assert.Greater(force.x, 0f, "Cohesion force should seek toward center of mass");
        }
        
        [UnityTest]
        public IEnumerator CohesionBehavior_AtCenterOfMass_ReturnsMinimalForce()
        {
            // Create main agent at origin
            var mainAgent = CreateAgent(Vector3.zero);
            
            // Create neighbors symmetrically around origin
            CreateAgent(new Vector3(3f, 0f, 0f));
            CreateAgent(new Vector3(-3f, 0f, 0f));
            CreateAgent(new Vector3(0f, 0f, 3f));
            CreateAgent(new Vector3(0f, 0f, -3f));
            
            yield return null;
            yield return new WaitForEndOfFrame();
            yield return null;
            
            var cohesion = new CohesionBehavior();
            var force = cohesion.CalculateForce(mainAgent);
            
            // Main agent is already at center of mass, so force should be minimal
            Assert.Less(force.magnitude, 1f, "Cohesion force should be minimal when at center of mass");
        }
        
        [UnityTest]
        public IEnumerator CohesionBehavior_CustomRadius_OnlyConsidersNearbyNeighbors()
        {
            // Create main agent at origin with large neighbor radius
            var mainAgent = CreateAgent(Vector3.zero, 20f);
            
            // Create close neighbors to the left
            CreateAgent(new Vector3(-2f, 0f, 0f), 20f);
            CreateAgent(new Vector3(-3f, 0f, 0f), 20f);
            
            // Create far neighbors to the right
            CreateAgent(new Vector3(10f, 0f, 0f), 20f);
            CreateAgent(new Vector3(12f, 0f, 0f), 20f);
            
            yield return null;
            yield return new WaitForEndOfFrame();
            yield return null;
            
            // Use custom small radius that only includes close neighbors
            var cohesion = new CohesionBehavior(5f);
            var force = cohesion.CalculateForce(mainAgent);
            
            // Should only consider close neighbors (to the left)
            Assert.Less(force.x, 0f, "Should seek toward close neighbors on the left");
        }
        
        #endregion
        
        #region Combined Flocking Tests
        
        [UnityTest]
        public IEnumerator AllFlockingBehaviors_WorkTogetherInSwarm()
        {
            // Create a small swarm
            var agent1 = CreateAgent(new Vector3(0f, 0f, 0f));
            var agent2 = CreateAgent(new Vector3(2f, 0f, 0f));
            var agent3 = CreateAgent(new Vector3(1f, 0f, 2f));
            
            yield return null;
            yield return new WaitForEndOfFrame();
            yield return null;
            
            // Give agents some velocity
            SetAgentVelocity(agent1, new Vector3(1f, 0f, 1f));
            SetAgentVelocity(agent2, new Vector3(1f, 0f, 1f));
            SetAgentVelocity(agent3, new Vector3(1f, 0f, 1f));
            
            // Test that all behaviors produce valid forces
            var separation = new SeparationBehavior();
            var alignment = new AlignmentBehavior();
            var cohesion = new CohesionBehavior();
            
            foreach (var agent in _agents)
            {
                var sepForce = separation.CalculateForce(agent);
                var alignForce = alignment.CalculateForce(agent);
                var cohForce = cohesion.CalculateForce(agent);
                
                // Each behavior should produce a valid force vector (not NaN or Infinity)
                Assert.IsFalse(float.IsNaN(sepForce.magnitude), $"Separation force should not be NaN for {agent.name}");
                Assert.IsFalse(float.IsInfinity(sepForce.magnitude), $"Separation force should not be Infinity for {agent.name}");
                
                Assert.IsFalse(float.IsNaN(alignForce.magnitude), $"Alignment force should not be NaN for {agent.name}");
                Assert.IsFalse(float.IsInfinity(alignForce.magnitude), $"Alignment force should not be Infinity for {agent.name}");
                
                Assert.IsFalse(float.IsNaN(cohForce.magnitude), $"Cohesion force should not be NaN for {agent.name}");
                Assert.IsFalse(float.IsInfinity(cohForce.magnitude), $"Cohesion force should not be Infinity for {agent.name}");
            }
        }
        
        [UnityTest]
        public IEnumerator FlockingBehaviors_WithWeights_CombineCorrectly()
        {
            // Create two agents close together
            var mainAgent = CreateAgent(Vector3.zero);
            var neighbor = CreateAgent(new Vector3(2f, 0f, 0f));
            
            yield return null;
            yield return new WaitForEndOfFrame();
            yield return null;
            
            SetAgentVelocity(neighbor, new Vector3(0f, 0f, 5f));
            
            // Create behaviors with different weights
            var separation = new SeparationBehavior { Weight = 2.0f };
            var alignment = new AlignmentBehavior { Weight = 1.0f };
            var cohesion = new CohesionBehavior { Weight = 0.5f };
            
            // Verify weights are applied
            Assert.AreEqual(2.0f, separation.Weight);
            Assert.AreEqual(1.0f, alignment.Weight);
            Assert.AreEqual(0.5f, cohesion.Weight);
            
            // Calculate weighted forces (simulating what SwarmAgent does)
            var sepForce = separation.CalculateForce(mainAgent) * separation.Weight;
            var alignForce = alignment.CalculateForce(mainAgent) * alignment.Weight;
            var cohForce = cohesion.CalculateForce(mainAgent) * cohesion.Weight;
            
            // Combined force
            var combinedForce = sepForce + alignForce + cohForce;
            
            // The combined force should be valid
            Assert.IsFalse(float.IsNaN(combinedForce.magnitude), "Combined force should not be NaN");
            Assert.Greater(combinedForce.magnitude, 0f, "Combined force should have magnitude");
        }
        
        #endregion
    }
}
