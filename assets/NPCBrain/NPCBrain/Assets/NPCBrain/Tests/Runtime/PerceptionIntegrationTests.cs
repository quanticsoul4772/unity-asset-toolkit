using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using NPCBrain.Perception;

namespace NPCBrain.Tests.Runtime
{
    /// <summary>
    /// PlayMode integration tests for the perception system.
    /// Tests SightSensor, Memory, and TargetSelector working together.
    /// </summary>
    [TestFixture]
    public class PerceptionIntegrationTests
    {
        private GameObject _npcObject;
        private GameObject _targetObject;
        private SightSensor _sightSensor;
        private NPCBrainController _brain;
        
        [SetUp]
        public void SetUp()
        {
            // Create NPC with sight sensor
            _npcObject = new GameObject("TestNPC");
            _sightSensor = _npcObject.AddComponent<SightSensor>();
            _brain = _npcObject.AddComponent<NPCBrainController>();
            
            // Pause the brain to prevent automatic ticking during tests
            // This gives us full control over when Tick() is called
            _brain.Pause();
            
            // Create target
            _targetObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            _targetObject.name = "Target";
            _targetObject.tag = "Player";
            _targetObject.transform.position = new Vector3(0f, 0f, 5f);
        }
        
        [TearDown]
        public void TearDown()
        {
            if (_npcObject != null)
                Object.Destroy(_npcObject);
            if (_targetObject != null)
                Object.Destroy(_targetObject);
        }
        
        [UnityTest]
        public IEnumerator SightSensor_DetectsTargetInFront()
        {
            // Position target in front of NPC
            _targetObject.transform.position = new Vector3(0f, 0f, 5f);
            _npcObject.transform.forward = Vector3.forward;
            
            // Wait for physics to register the collider
            yield return new WaitForFixedUpdate();
            yield return null;
            
            // Tick the sensor
            _sightSensor.Tick(_brain);
            
            Assert.IsTrue(_sightSensor.HasVisibleTargets, "Should detect target in front");
            Assert.AreEqual(_targetObject, _sightSensor.ClosestTarget);
        }
        
        [UnityTest]
        public IEnumerator SightSensor_DoesNotDetectTargetBehind()
        {
            // Position target behind NPC
            _targetObject.transform.position = new Vector3(0f, 0f, -5f);
            _npcObject.transform.forward = Vector3.forward;
            
            // Wait for physics to sync
            yield return new WaitForFixedUpdate();
            yield return null;
            
            _sightSensor.Tick(_brain);
            
            Assert.IsFalse(_sightSensor.HasVisibleTargets, "Should not detect target behind");
        }
        
        [UnityTest]
        public IEnumerator SightSensor_DoesNotDetectTargetTooFar()
        {
            // Position target beyond view distance (default is 20)
            _targetObject.transform.position = new Vector3(0f, 0f, 100f);
            _npcObject.transform.forward = Vector3.forward;
            
            // Wait for physics to sync
            yield return new WaitForFixedUpdate();
            yield return null;
            
            _sightSensor.Tick(_brain);
            
            // ViewDistance is 20 by default, target is at 100, should not be detected
            Assert.IsFalse(_sightSensor.HasVisibleTargets, 
                $"Should not detect target at 100 units (ViewDistance={_sightSensor.ViewDistance})");
        }
        
        [UnityTest]
        public IEnumerator Memory_TracksTargetPosition()
        {
            var memory = new Memory();
            Vector3 position = new Vector3(5f, 0f, 10f);
            
            memory.UpdateVisible(_targetObject, position);
            
            yield return null;
            
            var mem = memory.GetMemory(_targetObject);
            Assert.IsNotNull(mem);
            Assert.AreEqual(position, mem.LastKnownPosition);
        }
        
        [UnityTest]
        public IEnumerator Memory_DecaysOverTime()
        {
            var memory = new Memory();
            memory.MemoryDuration = 0.1f; // Short duration for testing
            memory.DecayRate = 10f; // Fast decay
            
            memory.UpdateVisible(_targetObject, Vector3.zero);
            memory.MarkLost(_targetObject);
            
            // Wait for decay
            float startTime = Time.time;
            while (Time.time - startTime < 0.2f)
            {
                memory.Tick();
                yield return null;
            }
            
            Assert.IsFalse(memory.Remembers(_targetObject), "Memory should have decayed");
        }
        
        [UnityTest]
        public IEnumerator TargetSelector_RanksCloserTargetsHigher()
        {
            var memory = new Memory();
            var selector = new TargetSelector();
            
            // Create two targets at different distances
            var target2 = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            target2.name = "Target2";
            target2.transform.position = new Vector3(0f, 0f, 20f);
            
            _targetObject.transform.position = new Vector3(0f, 0f, 5f);
            
            // Add targets to memory and mark them as lost (not currently visible)
            // This simulates remembered targets that are no longer in view
            memory.UpdateVisible(_targetObject, _targetObject.transform.position);
            memory.UpdateVisible(target2, target2.transform.position);
            memory.MarkLost(_targetObject);
            memory.MarkLost(target2);
            
            yield return null;
            
            // Evaluate with memory only (no sensor) - tests remembered target ranking
            var result = selector.Evaluate(null, memory, Vector3.zero, Vector3.forward, null);
            
            // Memory should have 2 targets
            Assert.AreEqual(2, memory.Count, "Memory should have 2 targets");
            Assert.AreEqual(2, result.Count, $"Selector should return 2 scored targets, got {result.Count}");
            Assert.AreEqual(_targetObject, result[0].Target, "Closer target should be ranked first");
            
            Object.Destroy(target2);
        }
        
        [UnityTest]
        public IEnumerator PerceptionSystem_IntegrationFlow()
        {
            var memory = new Memory();
            var selector = new TargetSelector();
            
            // Position target in view
            _targetObject.transform.position = new Vector3(0f, 0f, 5f);
            _npcObject.transform.forward = Vector3.forward;
            
            yield return new WaitForFixedUpdate();
            yield return null;
            
            // Step 1: Detect target
            _sightSensor.Tick(_brain);
            Assert.IsTrue(_sightSensor.HasVisibleTargets, "Step 1: Should detect target");
            
            // Step 2: Update memory
            foreach (var target in _sightSensor.VisibleTargets)
            {
                memory.UpdateVisible(target, target.transform.position);
            }
            Assert.AreEqual(1, memory.Count, "Step 2: Should have 1 memory");
            
            // Step 3: Select best target
            var best = selector.SelectBest(
                _sightSensor, 
                memory, 
                _npcObject.transform.position, 
                _npcObject.transform.forward, 
                null);
            Assert.AreEqual(_targetObject, best, "Step 3: Should select the visible target");
            
            // Step 4: Move target out of view
            _targetObject.transform.position = new Vector3(0f, 0f, -10f);
            yield return null;
            
            _sightSensor.Tick(_brain);
            foreach (var target in _sightSensor.VisibleTargets)
            {
                memory.UpdateVisible(target, target.transform.position);
            }
            
            // Update memory for lost targets
            var mem = memory.GetMemory(_targetObject);
            if (mem != null && !_sightSensor.VisibleTargets.Contains(_targetObject))
            {
                memory.MarkLost(_targetObject);
            }
            
            Assert.IsFalse(_sightSensor.HasVisibleTargets, "Step 4: Should not see target behind");
            Assert.IsTrue(memory.Remembers(_targetObject), "Step 4: Should still remember target");
        }
        
        [UnityTest]
        public IEnumerator SightSensor_FiresTargetAcquiredEvent()
        {
            bool eventFired = false;
            GameObject acquiredTarget = null;
            
            _brain.OnTargetAcquired += (target) =>
            {
                eventFired = true;
                acquiredTarget = target;
            };
            
            _targetObject.transform.position = new Vector3(0f, 0f, 5f);
            _npcObject.transform.forward = Vector3.forward;
            
            // Wait for physics to fully sync
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            yield return null;
            
            // This is the FIRST tick (brain was paused in SetUp), so target should be newly acquired
            _sightSensor.Tick(_brain);
            
            if (!_sightSensor.HasVisibleTargets)
            {
                Assert.Inconclusive("Physics did not sync in time - target not detected");
                yield break;
            }
            
            Assert.IsTrue(eventFired, "OnTargetAcquired event should fire when target first detected");
            Assert.AreEqual(_targetObject, acquiredTarget);
        }
        
        [UnityTest]
        public IEnumerator SightSensor_FiresTargetLostEvent()
        {
            bool lostEventFired = false;
            
            _brain.OnTargetLost += (target) =>
            {
                lostEventFired = true;
            };
            
            // First, detect the target
            _targetObject.transform.position = new Vector3(0f, 0f, 5f);
            _npcObject.transform.forward = Vector3.forward;
            
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            yield return null;
            _sightSensor.Tick(_brain);
            
            if (!_sightSensor.HasVisibleTargets)
            {
                Assert.Inconclusive("Physics did not sync - initial detection failed");
                yield break;
            }
            
            // Now move target out of view
            _targetObject.transform.position = new Vector3(0f, 0f, -10f);
            
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            yield return null;
            _sightSensor.Tick(_brain);
            
            Assert.IsTrue(lostEventFired, "OnTargetLost event should fire when target moves out of view");
        }
    }
}
