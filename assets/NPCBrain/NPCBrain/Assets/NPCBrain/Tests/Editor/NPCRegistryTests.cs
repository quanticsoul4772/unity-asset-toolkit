using NUnit.Framework;
using UnityEngine;
using NPCBrain.Tests;

namespace NPCBrain.Tests.Editor
{
    /// <summary>
    /// Unit tests for NPCRegistry static class.
    /// </summary>
    [TestFixture]
    public class NPCRegistryTests
    {
        private GameObject _testObject1;
        private GameObject _testObject2;
        private GameObject _testObject3;
        
        [SetUp]
        public void SetUp()
        {
            // Clear the registry before each test
            NPCRegistry<TestBrain>.Clear();
        }
        
        [TearDown]
        public void TearDown()
        {
            NPCRegistry<TestBrain>.Clear();
            
            if (_testObject1 != null) Object.DestroyImmediate(_testObject1);
            if (_testObject2 != null) Object.DestroyImmediate(_testObject2);
            if (_testObject3 != null) Object.DestroyImmediate(_testObject3);
        }
        
        [Test]
        public void Register_AddsInstance()
        {
            var brain = CreateTestBrain(out _testObject1);
            
            NPCRegistry<TestBrain>.Register(brain);
            
            Assert.AreEqual(1, NPCRegistry<TestBrain>.Count);
            Assert.Contains(brain, NPCRegistry<TestBrain>.GetAll());
        }
        
        [Test]
        public void Register_DuplicateInstance_NotAdded()
        {
            var brain = CreateTestBrain(out _testObject1);
            
            NPCRegistry<TestBrain>.Register(brain);
            NPCRegistry<TestBrain>.Register(brain);
            
            Assert.AreEqual(1, NPCRegistry<TestBrain>.Count);
        }
        
        [Test]
        public void Register_NullInstance_Ignored()
        {
            NPCRegistry<TestBrain>.Register(null);
            
            Assert.AreEqual(0, NPCRegistry<TestBrain>.Count);
        }
        
        [Test]
        public void Unregister_RemovesInstance()
        {
            var brain = CreateTestBrain(out _testObject1);
            NPCRegistry<TestBrain>.Register(brain);
            
            NPCRegistry<TestBrain>.Unregister(brain);
            
            Assert.AreEqual(0, NPCRegistry<TestBrain>.Count);
        }
        
        [Test]
        public void Unregister_NonExistentInstance_NoError()
        {
            var brain = CreateTestBrain(out _testObject1);
            
            // Should not throw
            NPCRegistry<TestBrain>.Unregister(brain);
            
            Assert.AreEqual(0, NPCRegistry<TestBrain>.Count);
        }
        
        [Test]
        public void Clear_RemovesAllInstances()
        {
            var brain1 = CreateTestBrain(out _testObject1);
            var brain2 = CreateTestBrain(out _testObject2);
            NPCRegistry<TestBrain>.Register(brain1);
            NPCRegistry<TestBrain>.Register(brain2);
            
            NPCRegistry<TestBrain>.Clear();
            
            Assert.AreEqual(0, NPCRegistry<TestBrain>.Count);
        }
        
        [Test]
        public void GetAll_ReturnsCachedArray()
        {
            var brain = CreateTestBrain(out _testObject1);
            NPCRegistry<TestBrain>.Register(brain);
            
            var array1 = NPCRegistry<TestBrain>.GetAll();
            var array2 = NPCRegistry<TestBrain>.GetAll();
            
            // Should return same array instance (cached)
            Assert.AreSame(array1, array2);
        }
        
        [Test]
        public void GetAll_InvalidatesCache_AfterRegister()
        {
            var brain1 = CreateTestBrain(out _testObject1);
            NPCRegistry<TestBrain>.Register(brain1);
            var array1 = NPCRegistry<TestBrain>.GetAll();
            
            var brain2 = CreateTestBrain(out _testObject2);
            NPCRegistry<TestBrain>.Register(brain2);
            var array2 = NPCRegistry<TestBrain>.GetAll();
            
            // Should return different array (cache invalidated)
            Assert.AreNotSame(array1, array2);
            Assert.AreEqual(2, array2.Length);
        }
        
        [Test]
        public void FindNearest_ReturnsClosestInstance()
        {
            var brain1 = CreateTestBrain(out _testObject1);
            var brain2 = CreateTestBrain(out _testObject2);
            _testObject1.transform.position = new Vector3(10f, 0f, 0f);
            _testObject2.transform.position = new Vector3(5f, 0f, 0f);
            NPCRegistry<TestBrain>.Register(brain1);
            NPCRegistry<TestBrain>.Register(brain2);
            
            var nearest = NPCRegistry<TestBrain>.FindNearest(Vector3.zero);
            
            Assert.AreEqual(brain2, nearest);
        }
        
        [Test]
        public void FindNearest_RespectsMaxDistance()
        {
            var brain = CreateTestBrain(out _testObject1);
            _testObject1.transform.position = new Vector3(100f, 0f, 0f);
            NPCRegistry<TestBrain>.Register(brain);
            
            var nearest = NPCRegistry<TestBrain>.FindNearest(Vector3.zero, maxDistance: 10f);
            
            Assert.IsNull(nearest);
        }
        
        [Test]
        public void FindNearest_IgnoresInactiveObjects()
        {
            var brain1 = CreateTestBrain(out _testObject1);
            var brain2 = CreateTestBrain(out _testObject2);
            _testObject1.transform.position = new Vector3(5f, 0f, 0f);
            _testObject2.transform.position = new Vector3(10f, 0f, 0f);
            _testObject1.SetActive(false);
            NPCRegistry<TestBrain>.Register(brain1);
            NPCRegistry<TestBrain>.Register(brain2);
            
            var nearest = NPCRegistry<TestBrain>.FindNearest(Vector3.zero);
            
            Assert.AreEqual(brain2, nearest);
        }
        
        [Test]
        public void GetInRadius_ReturnsInstancesWithinRadius()
        {
            var brain1 = CreateTestBrain(out _testObject1);
            var brain2 = CreateTestBrain(out _testObject2);
            var brain3 = CreateTestBrain(out _testObject3);
            _testObject1.transform.position = new Vector3(5f, 0f, 0f);
            _testObject2.transform.position = new Vector3(8f, 0f, 0f);
            _testObject3.transform.position = new Vector3(20f, 0f, 0f);
            NPCRegistry<TestBrain>.Register(brain1);
            NPCRegistry<TestBrain>.Register(brain2);
            NPCRegistry<TestBrain>.Register(brain3);
            
            var results = new System.Collections.Generic.List<TestBrain>();
            NPCRegistry<TestBrain>.GetInRadius(Vector3.zero, 10f, results);
            
            Assert.AreEqual(2, results.Count);
            Assert.Contains(brain1, results);
            Assert.Contains(brain2, results);
        }
        
        private TestBrain CreateTestBrain(out GameObject obj)
        {
            obj = new GameObject("TestBrain");
            var brain = obj.AddComponent<TestBrain>();
            brain.InitializeForTests();
            return brain;
        }
    }
}
