using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using NPCBrain.Perception;
using NPCBrain.Tests;

namespace NPCBrain.Tests.Runtime
{
    [TestFixture]
    public class PerceptionIntegrationTests
    {
        private GameObject _npcObject;
        private TestBrain _brain;
        private SightSensor _sightSensor;
        private GameObject _targetObject;
        
        [SetUp]
        public void SetUp()
        {
            // Create NPC with sight sensor
            _npcObject = new GameObject("TestNPC");
            _brain = _npcObject.AddComponent<TestBrain>();
            _brain.InitializeForTests();
            _sightSensor = _npcObject.AddComponent<SightSensor>();
            
            // Create target with collider (required for Physics.OverlapSphere)
            _targetObject = new GameObject("Target");
            var collider = _targetObject.AddComponent<SphereCollider>();
            collider.radius = 0.5f;
            _targetObject.layer = 0; // Default layer
        }
        
        [TearDown]
        public void TearDown()
        {
            if (_npcObject != null)
            {
                Object.Destroy(_npcObject);
            }
            if (_targetObject != null)
            {
                Object.Destroy(_targetObject);
            }
        }
        
        #region Basic Detection Tests
        
        [UnityTest]
        public IEnumerator SightSensor_DetectsTarget_InFront()
        {
            // Position target in front of NPC, within range
            _npcObject.transform.position = Vector3.zero;
            _npcObject.transform.forward = Vector3.forward;
            _targetObject.transform.position = new Vector3(0f, 0f, 5f);
            
            // Wait for physics to update
            yield return new WaitForFixedUpdate();
            yield return null;
            
            _sightSensor.Tick(_brain);
            
            var visibleTargets = _sightSensor.VisibleTargets;
            Assert.IsTrue(visibleTargets.Contains(_targetObject), 
                "Target directly in front should be visible");
        }
        
        [UnityTest]
        public IEnumerator SightSensor_DoesNotDetect_TargetBehind()
        {
            // Position target behind NPC
            _npcObject.transform.position = Vector3.zero;
            _npcObject.transform.forward = Vector3.forward;
            _targetObject.transform.position = new Vector3(0f, 0f, -5f);
            
            yield return new WaitForFixedUpdate();
            yield return null;
            
            _sightSensor.Tick(_brain);
            
            var visibleTargets = _sightSensor.VisibleTargets;
            Assert.IsFalse(visibleTargets.Contains(_targetObject), 
                "Target behind NPC should not be visible");
        }
        
        [UnityTest]
        public IEnumerator SightSensor_DoesNotDetect_TargetOutOfRange()
        {
            // Position target far away (default range is 20)
            _npcObject.transform.position = Vector3.zero;
            _npcObject.transform.forward = Vector3.forward;
            _targetObject.transform.position = new Vector3(0f, 0f, 50f);
            
            yield return new WaitForFixedUpdate();
            yield return null;
            
            _sightSensor.Tick(_brain);
            
            var visibleTargets = _sightSensor.VisibleTargets;
            Assert.IsFalse(visibleTargets.Contains(_targetObject), 
                "Target out of range should not be visible");
        }
        
        [UnityTest]
        public IEnumerator SightSensor_DoesNotDetect_TargetOutsideFOV()
        {
            // Position target to the side (outside 120 degree FOV)
            _npcObject.transform.position = Vector3.zero;
            _npcObject.transform.forward = Vector3.forward;
            _targetObject.transform.position = new Vector3(10f, 0f, 0f); // 90 degrees to right
            
            yield return new WaitForFixedUpdate();
            yield return null;
            
            _sightSensor.Tick(_brain);
            
            var visibleTargets = _sightSensor.VisibleTargets;
            Assert.IsFalse(visibleTargets.Contains(_targetObject), 
                "Target outside FOV should not be visible");
        }
        
        #endregion
        
        #region Event Tests
        
        [UnityTest]
        public IEnumerator SightSensor_RaisesTargetAcquired_WhenTargetEntersView()
        {
            GameObject acquiredTarget = null;
            _brain.OnTargetAcquired += (target) => acquiredTarget = target;
            
            // Start with target out of view
            _npcObject.transform.position = Vector3.zero;
            _npcObject.transform.forward = Vector3.forward;
            _targetObject.transform.position = new Vector3(0f, 0f, -10f); // Behind
            
            yield return new WaitForFixedUpdate();
            _sightSensor.Tick(_brain);
            Assert.IsNull(acquiredTarget, "Should not have acquired target yet");
            
            // Move target into view
            _targetObject.transform.position = new Vector3(0f, 0f, 5f);
            yield return new WaitForFixedUpdate();
            yield return null;
            
            _sightSensor.Tick(_brain);
            
            Assert.AreEqual(_targetObject, acquiredTarget, "Should have raised TargetAcquired event");
        }
        
        [UnityTest]
        public IEnumerator SightSensor_RaisesTargetLost_WhenTargetLeavesView()
        {
            GameObject lostTarget = null;
            _brain.OnTargetLost += (target) => lostTarget = target;
            
            // Start with target in view
            _npcObject.transform.position = Vector3.zero;
            _npcObject.transform.forward = Vector3.forward;
            _targetObject.transform.position = new Vector3(0f, 0f, 5f);
            
            yield return new WaitForFixedUpdate();
            yield return null;
            _sightSensor.Tick(_brain);
            
            // Move target out of view
            _targetObject.transform.position = new Vector3(0f, 0f, -10f);
            yield return new WaitForFixedUpdate();
            yield return null;
            
            _sightSensor.Tick(_brain);
            
            Assert.AreEqual(_targetObject, lostTarget, "Should have raised TargetLost event");
        }
        
        #endregion
        
        #region Helper Method Tests
        
        [UnityTest]
        public IEnumerator SightSensor_GetClosestTarget_ReturnsNearest()
        {
            // Create second target
            var farTarget = new GameObject("FarTarget");
            var farCollider = farTarget.AddComponent<SphereCollider>();
            farCollider.radius = 0.5f;
            
            // Position targets
            _npcObject.transform.position = Vector3.zero;
            _npcObject.transform.forward = Vector3.forward;
            _targetObject.transform.position = new Vector3(0f, 0f, 3f); // Close
            farTarget.transform.position = new Vector3(0f, 0f, 10f); // Far
            
            yield return new WaitForFixedUpdate();
            yield return null;
            
            _sightSensor.Tick(_brain);
            
            var closest = _sightSensor.ClosestTarget;
            Assert.AreEqual(_targetObject, closest, "Should return the closest target");
            
            Object.Destroy(farTarget);
        }
        
        [UnityTest]
        public IEnumerator SightSensor_CanSee_ReturnsCorrectly()
        {
            _npcObject.transform.position = Vector3.zero;
            _npcObject.transform.forward = Vector3.forward;
            _targetObject.transform.position = new Vector3(0f, 0f, 5f);
            
            yield return new WaitForFixedUpdate();
            yield return null;
            
            _sightSensor.Tick(_brain);
            
            Assert.IsTrue(_sightSensor.VisibleTargets.Contains(_targetObject), "Should see visible target");
            
            // Create invisible target
            var invisibleTarget = new GameObject("Invisible");
            Assert.IsFalse(_sightSensor.VisibleTargets.Contains(invisibleTarget), "Should not see non-visible target");
            
            Object.Destroy(invisibleTarget);
        }
        
        #endregion
        
        #region Edge Cases
        
        [UnityTest]
        public IEnumerator SightSensor_DoesNotDetect_Self()
        {
            // Add collider to NPC itself
            var npcCollider = _npcObject.AddComponent<SphereCollider>();
            npcCollider.radius = 0.5f;
            
            _npcObject.transform.position = Vector3.zero;
            _npcObject.transform.forward = Vector3.forward;
            
            yield return new WaitForFixedUpdate();
            yield return null;
            
            _sightSensor.Tick(_brain);
            
            var visibleTargets = _sightSensor.VisibleTargets;
            Assert.IsFalse(visibleTargets.Contains(_npcObject), "NPC should not detect itself");
        }
        
        [UnityTest]
        public IEnumerator SightSensor_ReturnsEmptyList_WhenNoTargets()
        {
            // Remove the target
            Object.Destroy(_targetObject);
            _targetObject = null;
            
            yield return new WaitForFixedUpdate();
            yield return null;
            
            _sightSensor.Tick(_brain);
            
            var visibleTargets = _sightSensor.VisibleTargets;
            Assert.AreEqual(0, visibleTargets.Count, "Should return empty list when no targets");
        }
        
        #endregion
    }
}
