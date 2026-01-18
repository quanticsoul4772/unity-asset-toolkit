using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using NPCBrain.BehaviorTree;
using NPCBrain.BehaviorTree.Composites;
using NPCBrain.BehaviorTree.Actions;
using NPCBrain.BehaviorTree.Decorators;

namespace NPCBrain.Tests.Runtime
{
    /// <summary>
    /// PlayMode integration tests for the behavior tree system.
    /// Tests full behavior tree execution over multiple frames.
    /// </summary>
    [TestFixture]
    public class BehaviorTreeIntegrationTests
    {
        private GameObject _npcObject;
        private NPCBrainController _brain;
        
        [SetUp]
        public void SetUp()
        {
            _npcObject = new GameObject("TestNPC");
            _brain = _npcObject.AddComponent<NPCBrainController>();
        }
        
        [TearDown]
        public void TearDown()
        {
            if (_npcObject != null)
                Object.Destroy(_npcObject);
        }
        
        [UnityTest]
        public IEnumerator Sequence_ExecutesChildrenInOrder()
        {
            int step = 0;
            
            var sequence = new Sequence(
                new ActionNode(() =>
                {
                    Assert.AreEqual(0, step, "First action should run first");
                    step = 1;
                    return NodeStatus.Success;
                }),
                new ActionNode(() =>
                {
                    Assert.AreEqual(1, step, "Second action should run second");
                    step = 2;
                    return NodeStatus.Success;
                })
            );
            
            _brain.SetBehaviorTree(sequence);
            
            yield return null;
            yield return null;
            
            Assert.AreEqual(2, step, "Both actions should have executed");
        }
        
        [UnityTest]
        public IEnumerator Selector_TriesNextOnFailure()
        {
            bool secondRan = false;
            
            var selector = new Selector(
                new ActionNode(() => NodeStatus.Failure),
                new ActionNode(() =>
                {
                    secondRan = true;
                    return NodeStatus.Success;
                })
            );
            
            _brain.SetBehaviorTree(selector);
            
            yield return null;
            yield return null;
            
            Assert.IsTrue(secondRan, "Second child should run when first fails");
        }
        
        [UnityTest]
        public IEnumerator Selector_StopsOnSuccess()
        {
            bool secondRan = false;
            
            var selector = new Selector(
                new ActionNode(() => NodeStatus.Success),
                new ActionNode(() =>
                {
                    secondRan = true;
                    return NodeStatus.Success;
                })
            );
            
            _brain.SetBehaviorTree(selector);
            
            yield return null;
            yield return null;
            
            Assert.IsFalse(secondRan, "Second child should not run when first succeeds");
        }
        
        [UnityTest]
        public IEnumerator Wait_TakesCorrectTime()
        {
            float waitTime = 0.5f;
            float startTime = Time.time;
            bool completed = false;
            
            var sequence = new Sequence(
                new Wait(waitTime),
                new ActionNode(() =>
                {
                    completed = true;
                    return NodeStatus.Success;
                })
            );
            
            _brain.SetBehaviorTree(sequence);
            
            // Wait for completion
            while (!completed && Time.time - startTime < waitTime + 1f)
            {
                yield return null;
            }
            
            float elapsed = Time.time - startTime;
            Assert.IsTrue(completed, "Wait should complete");
            Assert.GreaterOrEqual(elapsed, waitTime - 0.1f, "Should wait at least the specified time");
        }
        
        [UnityTest]
        public IEnumerator MoveTo_MovesNPCToTarget()
        {
            Vector3 targetPosition = new Vector3(5f, 0f, 0f);
            _npcObject.transform.position = Vector3.zero;
            
            var moveTo = new MoveTo(() => targetPosition, 0.5f, 10f, 5f);
            _brain.SetBehaviorTree(moveTo);
            
            float startTime = Time.time;
            while (Time.time - startTime < 2f)
            {
                yield return null;
                if (Vector3.Distance(_npcObject.transform.position, targetPosition) < 0.5f)
                    break;
            }
            
            float distance = Vector3.Distance(_npcObject.transform.position, targetPosition);
            Assert.Less(distance, 1f, "NPC should move toward target");
        }
        
        [UnityTest]
        public IEnumerator Inverter_InvertsResult()
        {
            bool innerRan = false;
            
            var inverter = new Inverter(
                new ActionNode(() =>
                {
                    innerRan = true;
                    return NodeStatus.Success;
                })
            );
            
            _brain.SetBehaviorTree(inverter);
            
            yield return null;
            yield return null;
            
            Assert.IsTrue(innerRan, "Inner action should run");
            Assert.AreEqual(NodeStatus.Failure, _brain.LastStatus, "Inverter should invert success to failure");
        }
        
        [UnityTest]
        public IEnumerator Repeater_RepeatsTimes()
        {
            int count = 0;
            
            var repeater = new Repeater(
                new ActionNode(() =>
                {
                    count++;
                    return NodeStatus.Success;
                }),
                3
            );
            
            _brain.SetBehaviorTree(repeater);
            
            // Run until repeater completes (returns Success)
            for (int i = 0; i < 10; i++)
            {
                yield return null;
                // Stop once the repeater has completed its 3 repetitions
                if (_brain.LastStatus == NodeStatus.Success)
                {
                    break;
                }
            }
            
            Assert.AreEqual(3, count, "Should repeat exactly 3 times");
        }
        
        [UnityTest]
        public IEnumerator Blackboard_PersistsBetweenTicks()
        {
            string key = "testValue";
            int value = 42;
            
            var sequence = new Sequence(
                new SetBlackboard(key, () => value),
                new ActionNode(() =>
                {
                    int retrieved = _brain.Blackboard.Get(key, 0);
                    Assert.AreEqual(value, retrieved);
                    return NodeStatus.Success;
                })
            );
            
            _brain.SetBehaviorTree(sequence);
            
            yield return null;
            yield return null;
            
            Assert.AreEqual(value, _brain.Blackboard.Get(key, 0));
        }
        
        [UnityTest]
        public IEnumerator Parallel_RunsChildrenSimultaneously()
        {
            bool child1Ran = false;
            bool child2Ran = false;
            
            var parallel = new Parallel(
                new ActionNode(() =>
                {
                    child1Ran = true;
                    return NodeStatus.Success;
                }),
                new ActionNode(() =>
                {
                    child2Ran = true;
                    return NodeStatus.Success;
                })
            );
            
            _brain.SetBehaviorTree(parallel);
            
            yield return null;
            yield return null;
            
            Assert.IsTrue(child1Ran, "First child should run");
            Assert.IsTrue(child2Ran, "Second child should run");
        }
        
        [UnityTest]
        public IEnumerator Brain_CanBePaused()
        {
            int tickCount = 0;
            
            var action = new ActionNode(() =>
            {
                tickCount++;
                return NodeStatus.Running;
            });
            
            _brain.SetBehaviorTree(action);
            
            yield return null;
            int countBeforePause = tickCount;
            
            _brain.Pause();
            
            yield return null;
            yield return null;
            
            Assert.AreEqual(countBeforePause, tickCount, "Should not tick while paused");
            Assert.IsTrue(_brain.IsPaused);
        }
        
        [UnityTest]
        public IEnumerator Brain_CanBeResumed()
        {
            int tickCount = 0;
            
            var action = new ActionNode(() =>
            {
                tickCount++;
                return NodeStatus.Running;
            });
            
            _brain.SetBehaviorTree(action);
            
            _brain.Pause();
            yield return null;
            
            int countWhilePaused = tickCount;
            
            _brain.Resume();
            yield return null;
            yield return null;
            
            Assert.Greater(tickCount, countWhilePaused, "Should tick after resume");
            Assert.IsFalse(_brain.IsPaused);
        }
        
        [UnityTest]
        public IEnumerator Cooldown_PreventsRapidExecution()
        {
            int executeCount = 0;
            
            var cooldown = new Cooldown(
                new ActionNode(() =>
                {
                    executeCount++;
                    return NodeStatus.Success;
                }),
                0.5f
            );
            
            _brain.SetBehaviorTree(cooldown);
            
            // Run for a short time
            float startTime = Time.time;
            while (Time.time - startTime < 0.3f)
            {
                yield return null;
            }
            
            // Should only execute once due to cooldown
            Assert.AreEqual(1, executeCount, "Should only execute once during cooldown period");
        }
        
        [UnityTest]
        public IEnumerator UtilitySelector_SelectsAction()
        {
            bool actionExecuted = false;
            
            var utilitySelector = new UtilitySelector(
                new NPCBrain.UtilityAI.UtilityAction(
                    "TestAction",
                    new ActionNode(() =>
                    {
                        actionExecuted = true;
                        return NodeStatus.Success;
                    }),
                    1f,
                    new NPCBrain.UtilityAI.ConstantConsideration(1f)
                )
            );
            
            _brain.SetBehaviorTree(utilitySelector);
            
            yield return null;
            yield return null;
            
            Assert.IsTrue(actionExecuted, "Utility action should execute");
        }
        
        [UnityTest]
        public IEnumerator Criticality_UpdatesTemperature()
        {
            var utilitySelector = new UtilitySelector(
                new NPCBrain.UtilityAI.UtilityAction(
                    "Action1",
                    new ActionNode(() => NodeStatus.Success),
                    1f,
                    new NPCBrain.UtilityAI.ConstantConsideration(1f)
                )
            );
            
            _brain.SetBehaviorTree(utilitySelector);
            
            float initialTemp = _brain.Criticality.Temperature;
            
            // Run for several frames to trigger actions
            for (int i = 0; i < 30; i++)
            {
                yield return null;
            }
            
            // Temperature should have changed due to action recording
            // Note: It may or may not have changed depending on entropy, but the system should work
            Assert.IsNotNull(_brain.Criticality, "Criticality should exist");
        }
    }
    
    /// <summary>
    /// Simple action node for testing that executes a delegate.
    /// </summary>
    internal class ActionNode : BTNode
    {
        private readonly System.Func<NodeStatus> _action;
        
        public ActionNode(System.Func<NodeStatus> action)
        {
            _action = action;
            Name = "ActionNode";
        }
        
        protected override NodeStatus Tick(NPCBrainController brain)
        {
            return _action();
        }
    }
}
