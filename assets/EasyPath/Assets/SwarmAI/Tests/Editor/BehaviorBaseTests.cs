using NUnit.Framework;
using UnityEngine;
using SwarmAI;

namespace SwarmAI.Tests
{
    /// <summary>
    /// Unit tests for IBehavior interface and BehaviorBase class.
    /// </summary>
    [TestFixture]
    public class BehaviorBaseTests
    {
        // Test implementation of BehaviorBase
        private class TestBehavior : BehaviorBase
        {
            public override string Name => "Test Behavior";
            
            public Vector3 ForceToReturn { get; set; } = Vector3.forward;
            
            public override Vector3 CalculateForce(SwarmAgent agent)
            {
                return ForceToReturn;
            }
            
            // Expose protected methods for testing
            public Vector3 TestSeek(SwarmAgent agent, Vector3 target)
            {
                return Seek(agent, target);
            }
            
            public Vector3 TestFlee(SwarmAgent agent, Vector3 threat)
            {
                return Flee(agent, threat);
            }
            
            public Vector3 TestTruncate(Vector3 vector, float maxLength)
            {
                return Truncate(vector, maxLength);
            }
        }
        
        private TestBehavior _behavior;
        
        [SetUp]
        public void SetUp()
        {
            _behavior = new TestBehavior();
        }
        
        [Test]
        public void Name_ReturnsCorrectName()
        {
            Assert.AreEqual("Test Behavior", _behavior.Name);
        }
        
        [Test]
        public void Weight_DefaultsToOne()
        {
            Assert.AreEqual(1f, _behavior.Weight);
        }
        
        [Test]
        public void Weight_CanBeSet()
        {
            _behavior.Weight = 2.5f;
            
            Assert.AreEqual(2.5f, _behavior.Weight);
        }
        
        [Test]
        public void IsActive_DefaultsToTrue()
        {
            Assert.IsTrue(_behavior.IsActive);
        }
        
        [Test]
        public void IsActive_CanBeSet()
        {
            _behavior.IsActive = false;
            
            Assert.IsFalse(_behavior.IsActive);
        }
        
        [Test]
        public void CalculateForce_ReturnsExpectedForce()
        {
            _behavior.ForceToReturn = new Vector3(1, 2, 3);
            
            var force = _behavior.CalculateForce(null);
            
            Assert.AreEqual(new Vector3(1, 2, 3), force);
        }
        
        [Test]
        public void Truncate_VectorBelowMax_ReturnsOriginal()
        {
            var vector = new Vector3(1, 0, 0);
            
            var result = _behavior.TestTruncate(vector, 5f);
            
            Assert.AreEqual(vector, result);
        }
        
        [Test]
        public void Truncate_VectorAboveMax_ReturnsTruncated()
        {
            var vector = new Vector3(10, 0, 0);
            
            var result = _behavior.TestTruncate(vector, 5f);
            
            Assert.AreEqual(5f, result.magnitude, 0.001f);
            Assert.AreEqual(Vector3.right, result.normalized);
        }
        
        [Test]
        public void Truncate_VectorExactlyMax_ReturnsOriginal()
        {
            var vector = new Vector3(5, 0, 0);
            
            var result = _behavior.TestTruncate(vector, 5f);
            
            Assert.AreEqual(vector, result);
        }
        
        // Note: Seek/Flee tests that require GameObjects are in PlayMode tests
        // See: Tests/Runtime/BehaviorPlayModeTests.cs
    }
}
