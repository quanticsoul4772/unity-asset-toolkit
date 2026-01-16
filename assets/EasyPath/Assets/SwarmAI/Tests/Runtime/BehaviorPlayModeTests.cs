using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SwarmAI;

namespace SwarmAI.Tests
{
    /// <summary>
    /// PlayMode tests for steering behavior helpers that require GameObjects.
    /// </summary>
    [TestFixture]
    public class BehaviorPlayModeTests
    {
        // Test implementation of BehaviorBase to expose protected methods
        // Note: This class is duplicated in BehaviorBaseTests.cs for test isolation.
        // Both versions expose Seek/Flee for testing but run in different contexts (EditMode vs PlayMode).
        private class TestBehavior : BehaviorBase
        {
            public override string Name => "Test Behavior";
            
            public override Vector3 CalculateForce(SwarmAgent agent)
            {
                return Vector3.zero;
            }
            
            public Vector3 TestSeek(SwarmAgent agent, Vector3 target)
            {
                return Seek(agent, target);
            }
            
            public Vector3 TestFlee(SwarmAgent agent, Vector3 threat)
            {
                return Flee(agent, threat);
            }
        }
        
        private TestBehavior _behavior;
        private GameObject _agentGO;
        private SwarmAgent _agent;
        
        [SetUp]
        public void SetUp()
        {
            _behavior = new TestBehavior();
            _agentGO = new GameObject("TestAgent");
            _agent = _agentGO.AddComponent<SwarmAgent>();
        }
        
        [TearDown]
        public void TearDown()
        {
            _behavior = null;
            if (_agentGO != null)
            {
                Object.Destroy(_agentGO);
            }
        }
        
        [UnityTest]
        public IEnumerator Seek_ReturnsForceTowardTarget()
        {
            _agentGO.transform.position = Vector3.zero;
            yield return null; // Wait one frame for Awake/Start
            
            var target = new Vector3(10, 0, 0);
            var force = _behavior.TestSeek(_agent, target);
            
            // Force should point toward target (positive X)
            Assert.Greater(force.x, 0f, "Seek force should point toward target");
        }
        
        [UnityTest]
        public IEnumerator Flee_ReturnsForceAwayFromThreat()
        {
            _agentGO.transform.position = Vector3.zero;
            yield return null; // Wait one frame for Awake/Start
            
            var threat = new Vector3(10, 0, 0);
            var force = _behavior.TestFlee(_agent, threat);
            
            // Force should point away from threat (negative X)
            Assert.Less(force.x, 0f, "Flee force should point away from threat");
        }
        
        [UnityTest]
        public IEnumerator Seek_AtTarget_ReturnsZeroForce()
        {
            _agentGO.transform.position = new Vector3(5, 0, 5);
            yield return null; // Wait one frame for Awake/Start
            
            var target = new Vector3(5, 0, 5);
            var force = _behavior.TestSeek(_agent, target);
            
            // Force should be zero or near-zero
            Assert.AreEqual(0f, force.magnitude, 0.01f, "Seek at target should return zero force");
        }
        
        [Test]
        public void Seek_WithNullAgent_ReturnsZero()
        {
            var force = _behavior.TestSeek(null, Vector3.forward);
            
            Assert.AreEqual(Vector3.zero, force);
        }
        
        [Test]
        public void Flee_WithNullAgent_ReturnsZero()
        {
            var force = _behavior.TestFlee(null, Vector3.forward);
            
            Assert.AreEqual(Vector3.zero, force);
        }
    }
}
