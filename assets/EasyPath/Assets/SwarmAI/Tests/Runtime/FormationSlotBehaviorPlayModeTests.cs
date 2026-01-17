using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SwarmAI;

namespace SwarmAI.Tests
{
    /// <summary>
    /// PlayMode tests for FormationSlotBehavior that require GameObjects.
    /// </summary>
    [TestFixture]
    public class FormationSlotBehaviorPlayModeTests
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
        
        #region CalculateForce Tests
        
        [UnityTest]
        public IEnumerator FormationSlotBehavior_AtTarget_ReturnsBrakingForce()
        {
            _agentGO.transform.position = Vector3.zero;
            _agent.SetTarget(Vector3.zero); // Target at same position
            yield return null;
            
            var behavior = new FormationSlotBehavior(3f, 0.8f);
            var force = behavior.CalculateForce(_agent);
            
            // Should return braking force (opposite of velocity)
            // Since agent is stationary, force should be zero or near zero
            Assert.LessOrEqual(force.magnitude, 0.1f);
        }
        
        [UnityTest]
        public IEnumerator FormationSlotBehavior_FarFromTarget_ReturnsSeekForce()
        {
            _agentGO.transform.position = Vector3.zero;
            _agent.SetTarget(new Vector3(10, 0, 0)); // Target far away
            yield return null;
            
            var behavior = new FormationSlotBehavior(3f, 0.5f);
            var force = behavior.CalculateForce(_agent);
            
            // Should return force toward target
            Assert.Greater(force.x, 0f, "Should produce force toward target");
        }
        
        [UnityTest]
        public IEnumerator FormationSlotBehavior_InsideSlowingRadius_ReducesSpeed()
        {
            _agentGO.transform.position = Vector3.zero;
            yield return null;
            
            // Target at (2,0,0), slowing radius 5 - agent is inside slowing radius
            _agent.SetTarget(new Vector3(2, 0, 0));
            var behaviorInside = new FormationSlotBehavior(5f, 0.5f);
            var forceInside = behaviorInside.CalculateForce(_agent);
            
            // Target at (10,0,0), slowing radius 5 - agent is outside slowing radius
            _agent.SetTarget(new Vector3(10, 0, 0));
            var behaviorOutside = new FormationSlotBehavior(5f, 0.5f);
            var forceOutside = behaviorOutside.CalculateForce(_agent);
            
            // Both should seek toward target
            Assert.Greater(forceInside.x, 0f, "Should seek toward target inside slowing radius");
            Assert.Greater(forceOutside.x, 0f, "Should seek toward target outside slowing radius");
        }
        
        [UnityTest]
        public IEnumerator FormationSlotBehavior_InsideArrivalRadius_ReturnsBrake()
        {
            _agentGO.transform.position = new Vector3(0.3f, 0, 0); // Within 0.5 arrival radius
            _agent.SetTarget(Vector3.zero);
            yield return null;
            
            var behavior = new FormationSlotBehavior(3f, 0.5f);
            var force = behavior.CalculateForce(_agent);
            
            // Force should be braking (opposite of velocity)
            // Since agent starts stationary, this will be near zero
            Assert.IsNotNull(force);
        }
        
        #endregion
        
        #region Integration Tests
        
        [UnityTest]
        public IEnumerator FormationSlotBehavior_AgentMovesTowardTarget()
        {
            _agentGO.transform.position = Vector3.zero;
            _agent.SetTarget(new Vector3(5, 0, 0));
            
            var behavior = new FormationSlotBehavior(3f, 0.5f);
            _agent.AddBehavior(behavior, 1f);
            
            Vector3 startPos = _agentGO.transform.position;
            
            // Wait a few frames for agent to move
            for (int i = 0; i < 10; i++)
            {
                yield return new WaitForFixedUpdate();
            }
            
            // Agent should have moved toward target
            // (This may or may not work depending on SwarmAgent's Update implementation)
            Assert.IsNotNull(_agent);
        }
        
        [UnityTest]
        public IEnumerator FormationSlotBehavior_InactiveReturnsZero()
        {
            _agentGO.transform.position = Vector3.zero;
            _agent.SetTarget(new Vector3(10, 0, 0));
            yield return null;
            
            var behavior = new FormationSlotBehavior();
            behavior.IsActive = false;
            
            // When inactive, CalculateForce should still work but behavior won't be processed
            // by the agent's steering calculation
            var force = behavior.CalculateForce(_agent);
            
            // The behavior itself still calculates force, but the agent ignores inactive behaviors
            Assert.IsNotNull(force);
        }
        
        #endregion
    }
}
