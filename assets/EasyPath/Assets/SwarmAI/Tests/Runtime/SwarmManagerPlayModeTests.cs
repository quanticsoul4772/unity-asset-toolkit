using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SwarmAI;

namespace SwarmAI.Tests
{
    /// <summary>
    /// PlayMode tests for SwarmManager that require GameObjects.
    /// </summary>
    [TestFixture]
    public class SwarmManagerPlayModeTests
    {
        private GameObject _managerGO;
        private SwarmManager _manager;
        private List<GameObject> _agentGOs;
        
        [SetUp]
        public void SetUp()
        {
            // Create SwarmManager
            _managerGO = new GameObject("TestSwarmManager");
            _manager = _managerGO.AddComponent<SwarmManager>();
            _agentGOs = new List<GameObject>();
        }
        
        [TearDown]
        public void TearDown()
        {
            foreach (var go in _agentGOs)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }
            _agentGOs.Clear();
            
            if (_managerGO != null)
            {
                Object.DestroyImmediate(_managerGO);
            }
        }
        
        private SwarmAgent CreateAgent(Vector3 position)
        {
            var go = new GameObject($"TestAgent_{_agentGOs.Count}");
            go.transform.position = position;
            var agent = go.AddComponent<SwarmAgent>();
            _agentGOs.Add(go);
            return agent;
        }
        
        [UnityTest]
        public IEnumerator SwarmManager_HasInstance_TrueAfterCreation()
        {
            yield return null;
            
            Assert.IsTrue(SwarmManager.HasInstance);
        }
        
        [UnityTest]
        public IEnumerator SwarmManager_Instance_ReturnsSameInstance()
        {
            yield return null;
            
            var instance1 = SwarmManager.Instance;
            var instance2 = SwarmManager.Instance;
            
            Assert.AreSame(instance1, instance2);
        }
        
        [UnityTest]
        public IEnumerator SwarmManager_Settings_NotNull()
        {
            yield return null;
            
            Assert.IsNotNull(_manager.Settings);
        }
        
        [UnityTest]
        public IEnumerator SwarmManager_AgentCount_InitiallyZero()
        {
            yield return null;
            
            Assert.AreEqual(0, _manager.AgentCount);
        }
        
        [UnityTest]
        public IEnumerator SwarmManager_RegisterAgent_IncreasesCount()
        {
            var agent = CreateAgent(Vector3.zero);
            yield return null;
            
            Assert.AreEqual(1, _manager.AgentCount);
        }
        
        [UnityTest]
        public IEnumerator SwarmManager_RegisterMultipleAgents_CountsCorrectly()
        {
            CreateAgent(new Vector3(0, 0, 0));
            CreateAgent(new Vector3(10, 0, 0));
            CreateAgent(new Vector3(20, 0, 0));
            yield return null;
            
            Assert.AreEqual(3, _manager.AgentCount);
        }
        
        [UnityTest]
        public IEnumerator SwarmManager_UnregisterAgent_DecreasesCount()
        {
            var agent = CreateAgent(Vector3.zero);
            yield return null;
            
            Assert.AreEqual(1, _manager.AgentCount);
            
            Object.DestroyImmediate(agent.gameObject);
            _agentGOs.Remove(agent.gameObject);
            yield return null;
            
            Assert.AreEqual(0, _manager.AgentCount);
        }
        
        [UnityTest]
        public IEnumerator SwarmManager_GetAgent_ReturnsRegisteredAgent()
        {
            var agent = CreateAgent(Vector3.zero);
            yield return null;
            
            var retrieved = _manager.GetAgent(agent.AgentId);
            
            Assert.AreSame(agent, retrieved);
        }
        
        [UnityTest]
        public IEnumerator SwarmManager_GetAgent_InvalidId_ReturnsNull()
        {
            yield return null;
            
            var retrieved = _manager.GetAgent(999);
            
            Assert.IsNull(retrieved);
        }
        
        [UnityTest]
        public IEnumerator SwarmManager_GetAllAgents_ReturnsAllAgents()
        {
            var agent1 = CreateAgent(new Vector3(0, 0, 0));
            var agent2 = CreateAgent(new Vector3(10, 0, 0));
            yield return null;
            
            var allAgents = _manager.GetAllAgents();
            
            Assert.AreEqual(2, allAgents.Count);
            Assert.IsTrue(allAgents.Contains(agent1));
            Assert.IsTrue(allAgents.Contains(agent2));
        }
        
        [UnityTest]
        public IEnumerator SwarmManager_GetAllAgents_PreallocatedList_PopulatesCorrectly()
        {
            var agent1 = CreateAgent(new Vector3(0, 0, 0));
            var agent2 = CreateAgent(new Vector3(10, 0, 0));
            yield return null;
            
            var results = new List<SwarmAgent>();
            _manager.GetAllAgents(results);
            
            Assert.AreEqual(2, results.Count);
            Assert.IsTrue(results.Contains(agent1));
            Assert.IsTrue(results.Contains(agent2));
        }
        
        [UnityTest]
        public IEnumerator SwarmManager_GetNeighbors_FindsAgentsInRadius()
        {
            var agent1 = CreateAgent(new Vector3(0, 0, 0));
            var agent2 = CreateAgent(new Vector3(5, 0, 0));  // Within 10 units
            var agent3 = CreateAgent(new Vector3(100, 0, 100));  // Far away
            
            yield return null;  // Wait for registration
            yield return null;  // Wait for spatial hash update
            
            var neighbors = _manager.GetNeighbors(Vector3.zero, 10f);
            
            Assert.IsTrue(neighbors.Contains(agent1));
            Assert.IsTrue(neighbors.Contains(agent2));
            Assert.IsFalse(neighbors.Contains(agent3));
        }
        
        [UnityTest]
        public IEnumerator SwarmManager_GetNeighborsExcluding_ExcludesAgent()
        {
            var agent1 = CreateAgent(new Vector3(0, 0, 0));
            var agent2 = CreateAgent(new Vector3(5, 0, 0));
            
            yield return null;
            yield return null;
            
            var results = new List<SwarmAgent>();
            _manager.GetNeighborsExcluding(Vector3.zero, 10f, agent1, results);
            
            Assert.IsFalse(results.Contains(agent1));
            Assert.IsTrue(results.Contains(agent2));
        }
        
        [UnityTest]
        public IEnumerator SwarmManager_GetNearestAgent_FindsClosest()
        {
            var agent1 = CreateAgent(new Vector3(10, 0, 0));
            var agent2 = CreateAgent(new Vector3(5, 0, 0));  // Closer
            var agent3 = CreateAgent(new Vector3(20, 0, 0));
            
            yield return null;
            yield return null;
            
            var nearest = _manager.GetNearestAgent(Vector3.zero, 30f);
            
            Assert.AreSame(agent2, nearest);
        }
        
        [UnityTest]
        public IEnumerator SwarmManager_OnAgentRegistered_EventFires()
        {
            bool eventFired = false;
            SwarmAgent registeredAgent = null;
            _manager.OnAgentRegistered += (agent) => {
                eventFired = true;
                registeredAgent = agent;
            };
            
            var agent = CreateAgent(Vector3.zero);
            yield return null;
            
            Assert.IsTrue(eventFired);
            Assert.AreSame(agent, registeredAgent);
        }
        
        [UnityTest]
        public IEnumerator SwarmManager_OnAgentUnregistered_EventFires()
        {
            var agent = CreateAgent(Vector3.zero);
            yield return null;
            
            bool eventFired = false;
            _manager.OnAgentUnregistered += (a) => eventFired = true;
            
            Object.DestroyImmediate(agent.gameObject);
            _agentGOs.Remove(agent.gameObject);
            yield return null;
            
            Assert.IsTrue(eventFired);
        }
        
        [UnityTest]
        public IEnumerator SwarmManager_BroadcastMessage_ReachesAllAgents()
        {
            var agent1 = CreateAgent(new Vector3(0, 0, 0));
            var agent2 = CreateAgent(new Vector3(10, 0, 0));
            yield return null;
            
            int messagesReceived = 0;
            agent1.OnMessageReceived += (msg) => messagesReceived++;
            agent2.OnMessageReceived += (msg) => messagesReceived++;
            
            _manager.BroadcastMessage(SwarmMessage.Stop());
            yield return null;  // Wait for message processing
            
            Assert.AreEqual(2, messagesReceived);
        }
        
        [UnityTest]
        public IEnumerator SwarmManager_SendMessage_ReachesTargetOnly()
        {
            var agent1 = CreateAgent(new Vector3(0, 0, 0));
            var agent2 = CreateAgent(new Vector3(10, 0, 0));
            yield return null;
            
            int agent1Messages = 0;
            int agent2Messages = 0;
            agent1.OnMessageReceived += (msg) => agent1Messages++;
            agent2.OnMessageReceived += (msg) => agent2Messages++;
            
            _manager.SendMessage(agent1.AgentId, SwarmMessage.Stop());
            yield return null;
            
            Assert.AreEqual(1, agent1Messages);
            Assert.AreEqual(0, agent2Messages);
        }
        
        [UnityTest]
        public IEnumerator SwarmManager_MoveAllTo_SendsMoveMessage()
        {
            var agent = CreateAgent(Vector3.zero);
            yield return null;
            
            Vector3 targetPosition = new Vector3(50, 0, 50);
            bool receivedMoveMessage = false;
            
            agent.OnMessageReceived += (msg) => {
                if (msg.Type == SwarmMessageType.MoveTo && msg.Position == targetPosition)
                {
                    receivedMoveMessage = true;
                }
            };
            
            _manager.MoveAllTo(targetPosition);
            yield return null;
            
            Assert.IsTrue(receivedMoveMessage);
        }
        
        [UnityTest]
        public IEnumerator SwarmManager_StopAll_SendsStopMessage()
        {
            var agent = CreateAgent(Vector3.zero);
            yield return null;
            
            bool receivedStopMessage = false;
            agent.OnMessageReceived += (msg) => {
                if (msg.Type == SwarmMessageType.Stop)
                {
                    receivedStopMessage = true;
                }
            };
            
            _manager.StopAll();
            yield return null;
            
            Assert.IsTrue(receivedStopMessage);
        }
        
        [UnityTest]
        public IEnumerator SwarmManager_SeekAll_SendsSeekMessage()
        {
            var agent = CreateAgent(Vector3.zero);
            yield return null;
            
            Vector3 targetPosition = new Vector3(25, 0, 25);
            bool receivedSeekMessage = false;
            
            agent.OnMessageReceived += (msg) => {
                if (msg.Type == SwarmMessageType.Seek && msg.Position == targetPosition)
                {
                    receivedSeekMessage = true;
                }
            };
            
            _manager.SeekAll(targetPosition);
            yield return null;
            
            Assert.IsTrue(receivedSeekMessage);
        }
    }
}
