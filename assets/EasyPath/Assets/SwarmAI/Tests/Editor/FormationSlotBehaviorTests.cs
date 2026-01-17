using NUnit.Framework;
using UnityEngine;
using SwarmAI;

namespace SwarmAI.Tests
{
    /// <summary>
    /// Unit tests for the FormationSlotBehavior class.
    /// Tests property validation, constructor behavior, and edge cases.
    /// </summary>
    [TestFixture]
    public class FormationSlotBehaviorTests
    {
        #region Constructor Tests
        
        [Test]
        public void FormationSlotBehavior_DefaultConstructor_SetsDefaultValues()
        {
            var behavior = new FormationSlotBehavior();
            
            Assert.AreEqual(3f, behavior.SlowingRadius);
            Assert.AreEqual(0.8f, behavior.ArrivalRadius);
            Assert.AreEqual(0.5f, behavior.DampingFactor);
        }
        
        [Test]
        public void FormationSlotBehavior_ParameterizedConstructor_SetsValues()
        {
            var behavior = new FormationSlotBehavior(5f, 1f, 0.7f);
            
            Assert.AreEqual(5f, behavior.SlowingRadius);
            Assert.AreEqual(1f, behavior.ArrivalRadius);
            Assert.AreEqual(0.7f, behavior.DampingFactor);
        }
        
        [Test]
        public void FormationSlotBehavior_Name_ReturnsFormationSlot()
        {
            var behavior = new FormationSlotBehavior();
            Assert.AreEqual("Formation Slot", behavior.Name);
        }
        
        #endregion
        
        #region SlowingRadius Tests
        
        [Test]
        public void FormationSlotBehavior_SlowingRadius_ClampsToMinimum()
        {
            var behavior = new FormationSlotBehavior();
            behavior.SlowingRadius = 0f;
            
            Assert.AreEqual(0.8f, behavior.SlowingRadius); // Clamped to arrival radius
        }
        
        [Test]
        public void FormationSlotBehavior_SlowingRadius_ClampsToArrivalRadius()
        {
            var behavior = new FormationSlotBehavior();
            behavior.ArrivalRadius = 2f;
            behavior.SlowingRadius = 1f; // Less than arrival radius
            
            Assert.GreaterOrEqual(behavior.SlowingRadius, behavior.ArrivalRadius);
        }
        
        [Test]
        public void FormationSlotBehavior_SlowingRadius_AcceptsValidValue()
        {
            var behavior = new FormationSlotBehavior();
            behavior.SlowingRadius = 10f;
            
            Assert.AreEqual(10f, behavior.SlowingRadius);
        }
        
        #endregion
        
        #region ArrivalRadius Tests
        
        [Test]
        public void FormationSlotBehavior_ArrivalRadius_ClampsNegativeToZero()
        {
            var behavior = new FormationSlotBehavior();
            behavior.ArrivalRadius = -5f;
            
            Assert.AreEqual(0f, behavior.ArrivalRadius);
        }
        
        [Test]
        public void FormationSlotBehavior_ArrivalRadius_AdjustsSlowingRadius()
        {
            var behavior = new FormationSlotBehavior();
            behavior.SlowingRadius = 2f;
            behavior.ArrivalRadius = 5f; // Greater than slowing radius
            
            Assert.GreaterOrEqual(behavior.SlowingRadius, behavior.ArrivalRadius);
        }
        
        [Test]
        public void FormationSlotBehavior_ArrivalRadius_AcceptsValidValue()
        {
            var behavior = new FormationSlotBehavior();
            behavior.ArrivalRadius = 0.5f;
            
            Assert.AreEqual(0.5f, behavior.ArrivalRadius);
        }
        
        #endregion
        
        #region DampingFactor Tests
        
        [Test]
        public void FormationSlotBehavior_DampingFactor_ClampsToZeroOne()
        {
            var behavior = new FormationSlotBehavior();
            
            behavior.DampingFactor = -1f;
            Assert.AreEqual(0f, behavior.DampingFactor);
            
            behavior.DampingFactor = 2f;
            Assert.AreEqual(1f, behavior.DampingFactor);
        }
        
        [Test]
        public void FormationSlotBehavior_DampingFactor_AcceptsValidValue()
        {
            var behavior = new FormationSlotBehavior();
            behavior.DampingFactor = 0.75f;
            
            Assert.AreEqual(0.75f, behavior.DampingFactor);
        }
        
        #endregion
        
        #region CalculateForce Tests
        
        [Test]
        public void FormationSlotBehavior_NullAgent_ReturnsZero()
        {
            var behavior = new FormationSlotBehavior();
            var force = behavior.CalculateForce(null);
            
            Assert.AreEqual(Vector3.zero, force);
        }
        
        #endregion
        
        #region Constructor Validation Tests
        
        [Test]
        public void FormationSlotBehavior_Constructor_ValidatesRadii()
        {
            // arrivalRadius > slowingRadius should be handled
            var behavior = new FormationSlotBehavior(1f, 5f);
            
            Assert.GreaterOrEqual(behavior.SlowingRadius, behavior.ArrivalRadius);
        }
        
        [Test]
        public void FormationSlotBehavior_Constructor_ClampsNegativeArrival()
        {
            var behavior = new FormationSlotBehavior(3f, -1f, 0.5f);
            
            Assert.AreEqual(0f, behavior.ArrivalRadius);
        }
        
        [Test]
        public void FormationSlotBehavior_Constructor_ClampsDampingFactor()
        {
            var behavior = new FormationSlotBehavior(3f, 0.5f, 5f);
            
            Assert.AreEqual(1f, behavior.DampingFactor);
        }
        
        #endregion
        
        #region Behavior Interface Tests
        
        [Test]
        public void FormationSlotBehavior_DefaultWeight_IsOne()
        {
            var behavior = new FormationSlotBehavior();
            Assert.AreEqual(1f, behavior.Weight);
        }
        
        [Test]
        public void FormationSlotBehavior_DefaultIsActive_IsTrue()
        {
            var behavior = new FormationSlotBehavior();
            Assert.IsTrue(behavior.IsActive);
        }
        
        [Test]
        public void FormationSlotBehavior_Weight_CanBeSet()
        {
            var behavior = new FormationSlotBehavior();
            behavior.Weight = 2.5f;
            
            Assert.AreEqual(2.5f, behavior.Weight);
        }
        
        [Test]
        public void FormationSlotBehavior_IsActive_CanBeSet()
        {
            var behavior = new FormationSlotBehavior();
            behavior.IsActive = false;
            
            Assert.IsFalse(behavior.IsActive);
        }
        
        #endregion
    }
}
