using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace NPCBrain.Tests.Runtime
{
    /// <summary>
    /// Minimal PlayMode tests - no NPCBrain references.
    /// This tests that the basic test infrastructure works.
    /// </summary>
    [TestFixture]
    public class PlayModeTests
    {
        private GameObject _testObject;
        
        [SetUp]
        public void SetUp()
        {
            _testObject = new GameObject("TestObject");
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
        public IEnumerator BasicTest_GameObjectExists()
        {
            Assert.IsNotNull(_testObject);
            yield return null;
        }
        
        [UnityTest]
        public IEnumerator BasicTest_WaitForSeconds()
        {
            float startTime = Time.time;
            yield return new WaitForSeconds(0.1f);
            float elapsed = Time.time - startTime;
            
            Assert.GreaterOrEqual(elapsed, 0.09f);
        }
    }
}
