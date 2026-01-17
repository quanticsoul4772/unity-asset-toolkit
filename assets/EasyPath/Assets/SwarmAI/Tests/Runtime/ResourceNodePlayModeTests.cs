using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SwarmAI;

namespace SwarmAI.Tests
{
    /// <summary>
    /// PlayMode tests for ResourceNode that require GameObjects.
    /// </summary>
    [TestFixture]
    public class ResourceNodePlayModeTests
    {
        private GameObject _resourceGO;
        private ResourceNode _resource;
        
        [SetUp]
        public void SetUp()
        {
            // Create a resource node
            _resourceGO = new GameObject("TestResource");
            _resource = _resourceGO.AddComponent<ResourceNode>();
            _resource.Configure(100f, 10f, false, 30f);
        }
        
        [TearDown]
        public void TearDown()
        {
            if (_resourceGO != null)
            {
                Object.DestroyImmediate(_resourceGO);
            }
        }
        
        [Test]
        public void ResourceNode_Configure_SetsCorrectValues()
        {
            Assert.AreEqual(100f, _resource.TotalAmount);
            Assert.AreEqual(100f, _resource.CurrentAmount);
            Assert.AreEqual(10f, _resource.HarvestRate);
        }
        
        [Test]
        public void ResourceNode_IsDepleted_InitiallyFalse()
        {
            Assert.IsFalse(_resource.IsDepleted);
        }
        
        [Test]
        public void ResourceNode_Deplete_SetsIsDepletedTrue()
        {
            _resource.Deplete();
            
            Assert.IsTrue(_resource.IsDepleted);
            Assert.AreEqual(0f, _resource.CurrentAmount);
        }
        
        [Test]
        public void ResourceNode_Respawn_RestoresAmount()
        {
            _resource.Deplete();
            Assert.IsTrue(_resource.IsDepleted);
            
            _resource.Respawn();
            
            Assert.IsFalse(_resource.IsDepleted);
            Assert.AreEqual(100f, _resource.CurrentAmount);
        }
        
        [Test]
        public void ResourceNode_Refill_AddsAmount()
        {
            // Deplete half
            _resource.Deplete();
            _resource.Refill(50f);
            
            Assert.AreEqual(50f, _resource.CurrentAmount);
        }
        
        [Test]
        public void ResourceNode_Refill_CapsAtTotal()
        {
            _resource.Refill(200f);  // Try to overfill
            
            Assert.AreEqual(100f, _resource.CurrentAmount);
        }
        
        [Test]
        public void ResourceNode_AmountPercent_CalculatesCorrectly()
        {
            Assert.AreEqual(1f, _resource.AmountPercent);  // Full
            
            // Manually deplete and refill to 50%
            _resource.Deplete();
            _resource.Refill(50f);
            
            Assert.AreEqual(0.5f, _resource.AmountPercent, 0.001f);
        }
        
        [Test]
        public void ResourceNode_Position_ReturnsTransformPosition()
        {
            _resourceGO.transform.position = new Vector3(10, 5, 20);
            
            Assert.AreEqual(new Vector3(10, 5, 20), _resource.Position);
        }
        
        [Test]
        public void ResourceNode_HasCapacity_TrueWhenNotDepleted()
        {
            Assert.IsTrue(_resource.HasCapacity);
        }
        
        [Test]
        public void ResourceNode_HasCapacity_FalseWhenDepleted()
        {
            _resource.Deplete();
            
            Assert.IsFalse(_resource.HasCapacity);
        }
        
        [Test]
        public void ResourceNode_CurrentHarvesters_InitiallyZero()
        {
            Assert.AreEqual(0, _resource.CurrentHarvesters);
        }
        
        [UnityTest]
        public IEnumerator ResourceNode_OnDepleted_EventFires()
        {
            bool eventFired = false;
            _resource.OnDepleted += () => eventFired = true;
            
            _resource.Deplete();
            yield return null;
            
            Assert.IsTrue(eventFired);
        }
        
        [UnityTest]
        public IEnumerator ResourceNode_OnRespawned_EventFires()
        {
            bool eventFired = false;
            _resource.OnRespawned += () => eventFired = true;
            
            _resource.Deplete();
            _resource.Respawn();
            yield return null;
            
            Assert.IsTrue(eventFired);
        }
        
        [UnityTest]
        public IEnumerator ResourceNode_AllNodes_RegistersOnEnable()
        {
            // The resource should be registered in AllNodes
            yield return null;
            
            Assert.IsTrue(ResourceNode.AllNodes.Contains(_resource));
        }
        
        [UnityTest]
        public IEnumerator ResourceNode_FindNearest_FindsClosestNode()
        {
            // Create a second resource node farther away
            var farResourceGO = new GameObject("FarResource");
            farResourceGO.transform.position = new Vector3(100, 0, 100);
            var farResource = farResourceGO.AddComponent<ResourceNode>();
            farResource.Configure(100f, 10f, false, 30f);
            
            // Position our test resource closer
            _resourceGO.transform.position = new Vector3(5, 0, 5);
            
            yield return null;
            
            // Find nearest from origin
            var nearest = ResourceNode.FindNearest(Vector3.zero);
            
            Assert.AreSame(_resource, nearest);
            
            Object.DestroyImmediate(farResourceGO);
        }
        
        [UnityTest]
        public IEnumerator ResourceNode_FindNearestAvailable_SkipsDepleted()
        {
            // Create a second resource node
            var secondResourceGO = new GameObject("SecondResource");
            secondResourceGO.transform.position = new Vector3(20, 0, 20);
            var secondResource = secondResourceGO.AddComponent<ResourceNode>();
            secondResource.Configure(100f, 10f, false, 30f);
            
            // Position our test resource closer but deplete it
            _resourceGO.transform.position = new Vector3(5, 0, 5);
            _resource.Deplete();
            
            yield return null;
            
            // Find nearest available from origin - should skip depleted one
            var nearest = ResourceNode.FindNearestAvailable(Vector3.zero);
            
            Assert.AreSame(secondResource, nearest);
            
            Object.DestroyImmediate(secondResourceGO);
        }
    }
}
