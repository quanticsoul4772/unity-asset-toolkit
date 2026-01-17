using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using NPCBrain.BehaviorTree;
using NPCBrain.BehaviorTree.Actions;
using NPCBrain.BehaviorTree.Decorators;

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
        public IEnumerator Cooldown_BlocksExecutionDuringCooldown()
        {
            var child = new Wait(0.01f);
            var cooldown = new Cooldown(child, 0.1f);
            
            // First execution should work
            cooldown.Execute(_brain);
            yield return new WaitForSeconds(0.02f);
            cooldown.Execute(_brain); // Complete the child
            
            Assert.IsTrue(cooldown.IsOnCooldown());
            
            // During cooldown, should return Failure
            NodeStatus status = cooldown.Execute(_brain);
            Assert.AreEqual(NodeStatus.Failure, status);
            
            yield return null;
        }
        
        [UnityTest]
        public IEnumerator Cooldown_AllowsExecutionAfterCooldownExpires()
        {
            var child = new Wait(0.01f);
            var cooldown = new Cooldown(child, 0.1f);
            
            // First execution
            cooldown.Execute(_brain);
            yield return new WaitForSeconds(0.02f);
            cooldown.Execute(_brain);
            
            Assert.IsTrue(cooldown.IsOnCooldown());
            
            // Wait for cooldown to expire
            yield return new WaitForSeconds(0.15f);
            
            Assert.IsFalse(cooldown.IsOnCooldown());
            
            // Should be able to execute again
            NodeStatus status = cooldown.Execute(_brain);
            Assert.AreNotEqual(NodeStatus.Failure, status);
        }
    }
}
