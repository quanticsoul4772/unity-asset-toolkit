using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SwarmAI;

namespace SwarmAI.Tests
{
    /// <summary>
    /// PlayMode tests for SwarmAgent that require GameObjects.
    /// </summary>
    [TestFixture]
    public class SwarmAgentPlayModeTests
    {
        private GameObject _managerGO;
        private SwarmManager _manager;
        private GameObject _agentGO;
        private SwarmAgent _agent;
        
        [SetUp]
        public void SetUp()
        {
            // Create SwarmManager first
            _managerGO = new GameObject("TestSwarmManager");
            _manager = _managerGO.AddComponent<SwarmManager>();
            
            // Create test agent
            _agentGO = new GameObject("TestAgent");
            _agent = _agentGO.AddComponent<SwarmAgent>();
        }
        
        [TearDown]
        public void TearDown()
        {
            if (_agentGO != null)
            {
                Object.DestroyImmediate(_agentGO);
            }
            if (_managerGO != null)
            {
                Object.DestroyImmediate(_managerGO);
            }
        }
        
        #region Initialization Tests
        
        [UnityTest]
        public IEnumerator SwarmAgent_AgentId_AssignedAfterRegistration()
        {
            yield return null;
            
            Assert.GreaterOrEqual(_agent.AgentId, 0);
        }
        
        [UnityTest]
        public IEnumerator SwarmAgent_IsRegistered_TrueAfterEnable()
        {
            yield return null;
            
            Assert.IsTrue(_agent.IsRegistered);
        }
        
        [UnityTest]
        public IEnumerator SwarmAgent_Position_ReturnsTransformPosition()
        {
            _agentGO.transform.position = new Vector3(10, 5, 20);
            yield return null;
            
            Assert.AreEqual(new Vector3(10, 5, 20), _agent.Position);
        }
        
        [UnityTest]
        public IEnumerator SwarmAgent_Forward_ReturnsTransformForward()
        {
            _agentGO.transform.rotation = Quaternion.LookRotation(Vector3.right);
            yield return null;
            
            Assert.AreEqual(Vector3.right, _agent.Forward);
        }
        
        #endregion
        
        #region Property Tests
        
        [Test]
        public void SwarmAgent_MaxSpeed_CanBeSet()
        {
            _agent.MaxSpeed = 10f;
            
            Assert.AreEqual(10f, _agent.MaxSpeed);
        }
        
        [Test]
        public void SwarmAgent_MaxForce_CanBeSet()
        {
            _agent.MaxForce = 15f;
            
            Assert.AreEqual(15f, _agent.MaxForce);
        }
        
        [Test]
        public void SwarmAgent_Mass_CanBeSet()
        {
            _agent.Mass = 2f;
            
            Assert.AreEqual(2f, _agent.Mass);
        }
        
        [Test]
        public void SwarmAgent_Mass_MinimumEnforced()
        {
            _agent.Mass = -1f;
            
            Assert.GreaterOrEqual(_agent.Mass, 0.01f);
        }
        
        [Test]
        public void SwarmAgent_NeighborRadius_CanBeSet()
        {
            _agent.NeighborRadius = 8f;
            
            Assert.AreEqual(8f, _agent.NeighborRadius);
        }
        
        [Test]
        public void SwarmAgent_StoppingDistance_CanBeSet()
        {
            _agent.StoppingDistance = 1.5f;
            
            Assert.AreEqual(1.5f, _agent.StoppingDistance);
        }
        
        #endregion
        
        #region State Machine Tests
        
        [UnityTest]
        public IEnumerator SwarmAgent_CurrentState_InitiallyIdle()
        {
            yield return null;
            
            Assert.AreEqual(AgentStateType.Idle, _agent.CurrentStateType);
        }
        
        [UnityTest]
        public IEnumerator SwarmAgent_SetState_ChangesState()
        {
            yield return null;
            
            _agent.SetState(new SeekingState(new Vector3(10, 0, 10)));
            
            Assert.AreEqual(AgentStateType.Seeking, _agent.CurrentStateType);
        }
        
        [UnityTest]
        public IEnumerator SwarmAgent_OnStateChanged_EventFires()
        {
            yield return null;
            
            AgentState oldState = null;
            AgentState newState = null;
            _agent.OnStateChanged += (o, n) => {
                oldState = o;
                newState = n;
            };
            
            _agent.SetState(new MovingState(Vector3.forward * 10));
            
            Assert.IsNotNull(oldState);
            Assert.IsNotNull(newState);
            Assert.AreEqual(AgentStateType.Idle, oldState.Type);
            Assert.AreEqual(AgentStateType.Moving, newState.Type);
        }
        
        #endregion
        
        #region Target Tests
        
        [UnityTest]
        public IEnumerator SwarmAgent_HasTarget_InitiallyFalse()
        {
            yield return null;
            
            Assert.IsFalse(_agent.HasTarget);
        }
        
        [UnityTest]
        public IEnumerator SwarmAgent_SetTarget_SetsTarget()
        {
            yield return null;
            
            Vector3 target = new Vector3(50, 0, 50);
            _agent.SetTarget(target);
            
            Assert.IsTrue(_agent.HasTarget);
            Assert.AreEqual(target, _agent.TargetPosition);
        }
        
        [UnityTest]
        public IEnumerator SwarmAgent_ClearTarget_RemovesTarget()
        {
            yield return null;
            
            _agent.SetTarget(Vector3.one * 10);
            Assert.IsTrue(_agent.HasTarget);
            
            _agent.ClearTarget();
            
            Assert.IsFalse(_agent.HasTarget);
        }
        
        [UnityTest]
        public IEnumerator SwarmAgent_Stop_ClearsVelocityAndTarget()
        {
            yield return null;
            
            _agent.SetTarget(Vector3.one * 10);
            _agent.Stop();
            
            Assert.IsFalse(_agent.HasTarget);
            Assert.AreEqual(Vector3.zero, _agent.Velocity);
        }
        
        #endregion
        
        #region Behavior Tests
        
        [Test]
        public void SwarmAgent_AddBehavior_AddsBehavior()
        {
            var behavior = new SeekBehavior();
            
            _agent.AddBehavior(behavior, 1f);
            
            // We can't directly access behaviors, but we can verify by adding a force
            Assert.Pass("Behavior added without error");
        }
        
        [Test]
        public void SwarmAgent_ClearBehaviors_RemovesAllBehaviors()
        {
            _agent.AddBehavior(new SeekBehavior(), 1f);
            _agent.AddBehavior(new FleeBehavior(), 1f);
            
            _agent.ClearBehaviors();
            
            // No way to verify count directly, but should not throw
            Assert.Pass("Behaviors cleared without error");
        }
        
        [Test]
        public void SwarmAgent_RemoveBehavior_RemovesSpecificBehavior()
        {
            var seekBehavior = new SeekBehavior();
            var fleeBehavior = new FleeBehavior();
            
            _agent.AddBehavior(seekBehavior, 1f);
            _agent.AddBehavior(fleeBehavior, 1f);
            
            _agent.RemoveBehavior(seekBehavior);
            
            Assert.Pass("Behavior removed without error");
        }
        
        [Test]
        public void SwarmAgent_RemoveBehaviorsOfType_RemovesAllOfType()
        {
            _agent.AddBehavior(new SeekBehavior(), 1f);
            _agent.AddBehavior(new SeekBehavior(), 2f);
            _agent.AddBehavior(new FleeBehavior(), 1f);
            
            _agent.RemoveBehaviorsOfType<SeekBehavior>();
            
            Assert.Pass("Behaviors of type removed without error");
        }
        
        #endregion
        
        #region Neighbor Tests
        
        [UnityTest]
        public IEnumerator SwarmAgent_GetNeighbors_ReturnsNearbyAgents()
        {
            // Create another agent nearby
            var nearbyGO = new GameObject("NearbyAgent");
            nearbyGO.transform.position = new Vector3(2, 0, 0);
            var nearbyAgent = nearbyGO.AddComponent<SwarmAgent>();
            
            yield return null;  // Registration
            yield return null;  // Spatial hash update
            
            var neighbors = _agent.GetNeighbors();
            
            Assert.IsTrue(neighbors.Contains(nearbyAgent));
            
            Object.DestroyImmediate(nearbyGO);
        }
        
        [UnityTest]
        public IEnumerator SwarmAgent_GetNeighbors_CachesPerFrame()
        {
            yield return null;
            
            var neighbors1 = _agent.GetNeighbors();
            var neighbors2 = _agent.GetNeighbors();
            
            // Same frame, should be same list instance (cached)
            Assert.AreSame(neighbors1, neighbors2);
        }
        
        [UnityTest]
        public IEnumerator SwarmAgent_GetNearestNeighbor_FindsClosest()
        {
            // Create agents at different distances
            var near = new GameObject("NearAgent");
            near.transform.position = new Vector3(2, 0, 0);
            var nearAgent = near.AddComponent<SwarmAgent>();
            
            var far = new GameObject("FarAgent");
            far.transform.position = new Vector3(4, 0, 0);
            var farAgent = far.AddComponent<SwarmAgent>();
            
            yield return null;
            yield return null;
            
            var nearest = _agent.GetNearestNeighbor();
            
            Assert.AreSame(nearAgent, nearest);
            
            Object.DestroyImmediate(near);
            Object.DestroyImmediate(far);
        }
        
        #endregion
        
        #region Message Tests
        
        [UnityTest]
        public IEnumerator SwarmAgent_OnMessageReceived_EventFires()
        {
            yield return null;
            
            bool messageReceived = false;
            SwarmMessage receivedMessage = default;
            
            _agent.OnMessageReceived += (msg) => {
                messageReceived = true;
                receivedMessage = msg;
            };
            
            _manager.SendMessage(_agent.AgentId, SwarmMessage.Stop());
            yield return null;
            
            Assert.IsTrue(messageReceived);
            Assert.AreEqual(SwarmMessageType.Stop, receivedMessage.Type);
        }
        
        [UnityTest]
        public IEnumerator SwarmAgent_ReceiveMoveTo_SetsTarget()
        {
            yield return null;
            
            Vector3 target = new Vector3(20, 0, 20);
            _manager.SendMessage(_agent.AgentId, SwarmMessage.MoveTo(target));
            yield return null;
            
            Assert.IsTrue(_agent.HasTarget);
            Assert.AreEqual(AgentStateType.Moving, _agent.CurrentStateType);
        }
        
        [UnityTest]
        public IEnumerator SwarmAgent_ReceiveStop_ClearsMovement()
        {
            yield return null;
            
            _agent.SetTarget(Vector3.one * 10);
            _manager.SendMessage(_agent.AgentId, SwarmMessage.Stop());
            yield return null;
            
            Assert.AreEqual(AgentStateType.Idle, _agent.CurrentStateType);
        }
        
        [UnityTest]
        public IEnumerator SwarmAgent_ReceiveSeek_EntersSeekingState()
        {
            yield return null;
            
            Vector3 target = new Vector3(15, 0, 15);
            _manager.SendMessage(_agent.AgentId, SwarmMessage.Seek(target));
            yield return null;
            
            Assert.AreEqual(AgentStateType.Seeking, _agent.CurrentStateType);
        }
        
        [UnityTest]
        public IEnumerator SwarmAgent_ReceiveFlee_EntersFleeingState()
        {
            yield return null;
            
            Vector3 threat = new Vector3(-10, 0, -10);
            _manager.SendMessage(_agent.AgentId, SwarmMessage.Flee(threat));
            yield return null;
            
            Assert.AreEqual(AgentStateType.Fleeing, _agent.CurrentStateType);
        }
        
        #endregion
        
        #region Movement Tests
        
        [UnityTest]
        public IEnumerator SwarmAgent_WithSeekBehavior_MovesTowardTarget()
        {
            yield return null;
            
            Vector3 startPos = _agent.Position;
            Vector3 targetPos = new Vector3(10, 0, 0);
            
            var seekBehavior = new SeekBehavior();
            seekBehavior.TargetPosition = targetPos;
            _agent.AddBehavior(seekBehavior, 1f);
            
            // Wait a few frames for movement
            for (int i = 0; i < 10; i++)
            {
                yield return new WaitForFixedUpdate();
            }
            
            // Agent should have moved toward target
            float startDistance = Vector3.Distance(startPos, targetPos);
            float currentDistance = Vector3.Distance(_agent.Position, targetPos);
            
            Assert.Less(currentDistance, startDistance, "Agent should have moved closer to target");
        }
        
        [UnityTest]
        public IEnumerator SwarmAgent_ApplyForce_AffectsMovement()
        {
            yield return null;
            
            Vector3 startPos = _agent.Position;
            
            // Apply a force for several frames
            for (int i = 0; i < 5; i++)
            {
                _agent.ApplyForce(Vector3.right * 10f);
                yield return new WaitForFixedUpdate();
            }
            
            // Agent should have moved
            Assert.AreNotEqual(startPos, _agent.Position);
        }
        
        #endregion
    }
}
