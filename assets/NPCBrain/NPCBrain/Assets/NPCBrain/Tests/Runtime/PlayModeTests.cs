using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using NPCBrain.BehaviorTree;
using NPCBrain.BehaviorTree.Actions;

namespace NPCBrain.Tests.Runtime
{
    /// <summary>
    /// PlayMode tests for time-dependent behavior tree nodes.
    /// These tests run across multiple frames and can test real time-based behavior.
    /// </summary>
    [TestFixture]
    public class PlayModeTests
    {
        private GameObject _testObject;
        private NPCBrainController _brain;
        
        [SetUp]
        public void SetUp()
        {
            _testObject = new GameObject("TestNPC");
            _brain = _testObject.AddComponent<NPCBrainController>();
        }
        
        [TearDown]
        public void TearDown()
        {
            if (_testObject != null)
            {
                Object.Destroy(_testObject);
            }
        }
        
        [UnityTest]
        public IEnumerator Wait_CompletesAfterDuration()
        {
            var wait = new Wait(0.1f);
            
            NodeStatus status = wait.Execute(_brain);
            Assert.AreEqual(NodeStatus.Running, status);
            
            yield return new WaitForSeconds(0.15f);
            
            status = wait.Execute(_brain);
            Assert.AreEqual(NodeStatus.Success, status);
        }
        
        [UnityTest]
        public IEnumerator Wait_Reset_RestartsTimer()
        {
            var wait = new Wait(0.1f);
            
            wait.Execute(_brain);
            yield return new WaitForSeconds(0.05f);
            
            wait.Reset();
            NodeStatus status = wait.Execute(_brain);
            
            Assert.AreEqual(NodeStatus.Running, status);
            
            yield return new WaitForSeconds(0.15f);
            
            status = wait.Execute(_brain);
            Assert.AreEqual(NodeStatus.Success, status);
        }
        
        [UnityTest]
        public IEnumerator MoveTo_MovesTowardsTarget()
        {
            _testObject.transform.position = Vector3.zero;
            Vector3 targetPos = new Vector3(10f, 0f, 0f);
            var moveTo = new MoveTo(() => targetPos, 0.5f, 10f);
            
            moveTo.Execute(_brain);
            yield return new WaitForSeconds(0.1f);
            moveTo.Execute(_brain);
            
            Assert.Greater(_testObject.transform.position.x, 0f, "Object should have moved towards target");
        }
        
        [UnityTest]
        public IEnumerator MoveTo_ReturnsSuccess_WhenReachesTarget()
        {
            _testObject.transform.position = new Vector3(9.8f, 0f, 0f);
            Vector3 targetPos = new Vector3(10f, 0f, 0f);
            var moveTo = new MoveTo(() => targetPos, 0.5f, 10f);
            
            NodeStatus status = NodeStatus.Running;
            for (int i = 0; i < 10 && status == NodeStatus.Running; i++)
            {
                status = moveTo.Execute(_brain);
                yield return null;
            }
            
            Assert.AreEqual(NodeStatus.Success, status);
        }
        
        [UnityTest]
        public IEnumerator Cooldown_AfterCooldownExpires_AllowsExecution()
        {
            var child = new Wait(0.01f);
            var cooldown = new BehaviorTree.Decorators.Cooldown(child, 0.1f);
            
            cooldown.Execute(_brain);
            yield return new WaitForSeconds(0.02f);
            cooldown.Execute(_brain);
            
            Assert.IsTrue(cooldown.IsOnCooldown());
            
            yield return new WaitForSeconds(0.15f);
            
            Assert.IsFalse(cooldown.IsOnCooldown());
            
            NodeStatus status = cooldown.Execute(_brain);
            Assert.AreNotEqual(NodeStatus.Failure, status);
        }
        
        [UnityTest]
        public IEnumerator NPCBrainController_Tick_ExecutesBehaviorTree()
        {
            var testBrain = _testObject.AddComponent<TestPlayModeBrain>();
            var wait = new Wait(0.05f);
            testBrain.SetBehaviorTree(wait);
            
            yield return null;
            
            Assert.AreEqual(NodeStatus.Running, testBrain.LastStatus);
            
            yield return new WaitForSeconds(0.1f);
            
            Assert.AreEqual(NodeStatus.Success, testBrain.LastStatus);
        }
        
        private class TestPlayModeBrain : NPCBrainController
        {
        }
    }
}
