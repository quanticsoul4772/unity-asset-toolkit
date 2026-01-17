using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using NPCBrain.BehaviorTree;
using NPCBrain.BehaviorTree.Actions;
using NPCBrain.BehaviorTree.Decorators;
using NPCBrain.Tests;

namespace NPCBrain.Tests.Runtime
{
    [TestFixture]
    public class PlayModeTests
    {
        private GameObject _testObject;
        private TestBrain _brain;
        
        [SetUp]
        public void SetUp()
        {
            _testObject = new GameObject("TestNPC");
            _brain = _testObject.AddComponent<TestBrain>();
            _brain.InitializeForTests();
        }
        
        [TearDown]
        public void TearDown()
        {
            if (_testObject != null)
            {
                UnityEngine.Object.Destroy(_testObject);
            }
        }
        
        #region Basic Tests
        
        [UnityTest]
        public IEnumerator BasicTest_GameObjectExists()
        {
            Assert.IsNotNull(_testObject);
            yield return null;
        }
        
        #endregion
        
        #region Wait Tests
        
        [UnityTest]
        public IEnumerator Wait_ReturnsRunning_WhileWaiting()
        {
            var wait = new Wait(0.1f);
            
            NodeStatus result = wait.Execute(_brain);
            
            Assert.AreEqual(NodeStatus.Running, result);
            yield return null;
        }
        
        [UnityTest]
        public IEnumerator Wait_ReturnsSuccess_AfterDuration()
        {
            var wait = new Wait(0.1f);
            
            wait.Execute(_brain);
            yield return new WaitForSeconds(0.15f);
            NodeStatus result = wait.Execute(_brain);
            
            Assert.AreEqual(NodeStatus.Success, result);
        }
        
        #endregion
        
        #region Cooldown Tests
        
        [UnityTest]
        public IEnumerator Cooldown_AfterCooldownExpires_ExecutesChild()
        {
            var child = new Wait(0.01f);
            var cooldown = new Cooldown(child, 0.1f);
            
            // First execution starts cooldown
            cooldown.Execute(_brain);
            yield return new WaitForSeconds(0.02f);
            cooldown.Execute(_brain); // Complete the child
            
            // During cooldown - should fail
            NodeStatus duringCooldown = cooldown.Execute(_brain);
            Assert.AreEqual(NodeStatus.Failure, duringCooldown);
            
            // Wait for cooldown to expire
            yield return new WaitForSeconds(0.15f);
            
            // After cooldown - should execute child again
            cooldown.Reset();
            NodeStatus afterCooldown = cooldown.Execute(_brain);
            Assert.AreEqual(NodeStatus.Running, afterCooldown);
        }
        
        #endregion
        
        #region MoveTo Tests
        
        [UnityTest]
        public IEnumerator MoveTo_MovesTowardsTarget()
        {
            Vector3 startPos = Vector3.zero;
            Vector3 targetPos = new Vector3(10f, 0f, 0f);
            _testObject.transform.position = startPos;
            
            Func<Vector3> getTarget = () => targetPos;
            var moveTo = new MoveTo(getTarget, 0.5f, 10f);
            
            // Execute a few ticks to let movement happen
            for (int i = 0; i < 5; i++)
            {
                moveTo.Execute(_brain);
                yield return null;
            }
            
            // Should have moved towards target
            float distanceFromStart = Vector3.Distance(_testObject.transform.position, startPos);
            Assert.Greater(distanceFromStart, 0f, "NPC should have moved from start position");
        }
        
        [UnityTest]
        public IEnumerator MoveTo_ReturnsSuccess_WhenReachesTarget()
        {
            Vector3 targetPos = new Vector3(0.1f, 0f, 0f);
            _testObject.transform.position = Vector3.zero;
            
            Func<Vector3> getTarget = () => targetPos;
            var moveTo = new MoveTo(getTarget, 0.5f, 10f);
            
            NodeStatus result = NodeStatus.Running;
            int maxIterations = 100;
            int iterations = 0;
            
            while (result == NodeStatus.Running && iterations < maxIterations)
            {
                result = moveTo.Execute(_brain);
                iterations++;
                yield return null;
            }
            
            Assert.AreEqual(NodeStatus.Success, result, "MoveTo should return Success when reaching target");
        }
        
        [UnityTest]
        public IEnumerator MoveTo_ReturnsFailure_OnTimeout()
        {
            // Set target very far away with short timeout
            Vector3 targetPos = new Vector3(10000f, 0f, 0f);
            _testObject.transform.position = Vector3.zero;
            
            Func<Vector3> getTarget = () => targetPos;
            var moveTo = new MoveTo(getTarget, 0.5f, 1f, 0.1f); // Very short timeout
            
            moveTo.Execute(_brain);
            yield return new WaitForSeconds(0.15f);
            
            NodeStatus result = moveTo.Execute(_brain);
            
            Assert.AreEqual(NodeStatus.Failure, result, "MoveTo should return Failure on timeout");
        }
        
        #endregion
    }
}
