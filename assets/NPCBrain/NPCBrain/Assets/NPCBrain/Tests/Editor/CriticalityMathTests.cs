using System;
using NUnit.Framework;
using NPCBrain.Criticality;

namespace NPCBrain.Tests.Editor
{
    /// <summary>
    /// Deterministic unit tests for CriticalityController entropy and temperature calculations.
    /// These tests verify the mathematical correctness without relying on random behavior.
    /// </summary>
    [TestFixture]
    public class CriticalityMathTests
    {
        [Test]
        public void Entropy_SingleActionRepeated_IsZero()
        {
            var controller = new CriticalityController();
            
            // Record same action 20 times
            for (int i = 0; i < 20; i++)
            {
                controller.RecordAction(0);
            }
            controller.Update();
            
            Assert.AreEqual(0f, controller.Entropy, 0.001f, 
                "Repeating single action should yield zero entropy");
        }
        
        [Test]
        public void Entropy_TwoActionsEqual_IsMaxForTwo()
        {
            var controller = new CriticalityController();
            
            // Record two actions equally (10 each)
            for (int i = 0; i < 10; i++)
            {
                controller.RecordAction(0);
                controller.RecordAction(1);
            }
            controller.Update();
            
            // Max entropy for 2 equally likely outcomes = ln(2) ≈ 0.693
            float maxEntropy = (float)Math.Log(2);
            Assert.AreEqual(maxEntropy, controller.Entropy, 0.001f, 
                "Two equally distributed actions should yield max entropy for 2 outcomes");
        }
        
        [Test]
        public void Entropy_MoreActions_HigherEntropy()
        {
            var controller2 = new CriticalityController();
            var controller4 = new CriticalityController();
            
            // 2 actions equally distributed
            for (int i = 0; i < 20; i++)
            {
                controller2.RecordAction(i % 2);
            }
            controller2.Update();
            
            // 4 actions equally distributed
            for (int i = 0; i < 20; i++)
            {
                controller4.RecordAction(i % 4);
            }
            controller4.Update();
            
            Assert.Greater(controller4.Entropy, controller2.Entropy, 
                "More equally distributed actions should have higher entropy");
        }
        
        [Test]
        public void Temperature_LowEntropy_Increases()
        {
            var controller = new CriticalityController();
            float initialTemp = controller.Temperature;
            
            // Record same action repeatedly (low entropy)
            for (int i = 0; i < 25; i++)
            {
                controller.RecordAction(0);
            }
            controller.Update();
            
            Assert.Greater(controller.Temperature, initialTemp, 
                "Low entropy should increase temperature to encourage exploration");
        }
        
        [Test]
        public void Temperature_HighEntropy_Decreases()
        {
            var controller = new CriticalityController();
            
            // First, raise temperature with low entropy
            for (int i = 0; i < 25; i++)
            {
                controller.RecordAction(0);
            }
            controller.Update();
            float elevatedTemp = controller.Temperature;
            
            // Reset and create high entropy
            controller.Reset();
            for (int i = 0; i < 20; i++)
            {
                controller.RecordAction(i); // All different actions
            }
            controller.Update();
            
            Assert.Less(controller.Temperature, elevatedTemp, 
                "High entropy should decrease temperature");
        }
        
        [Test]
        public void Temperature_ClampedToMin()
        {
            var controller = new CriticalityController();
            
            controller.SetTemperature(0.1f);
            
            Assert.AreEqual(CriticalityController.DefaultMinTemperature, controller.Temperature, 0.001f, 
                "Temperature should be clamped to minimum");
        }
        
        [Test]
        public void Temperature_ClampedToMax()
        {
            var controller = new CriticalityController();
            
            controller.SetTemperature(10f);
            
            Assert.AreEqual(CriticalityController.DefaultMaxTemperature, controller.Temperature, 0.001f, 
                "Temperature should be clamped to maximum");
        }
        
        [Test]
        public void Inertia_LowEntropy_HighInertia()
        {
            var controller = new CriticalityController();
            
            // Same action = low entropy = high inertia (tendency to stick)
            for (int i = 0; i < 25; i++)
            {
                controller.RecordAction(0);
            }
            controller.Update();
            
            Assert.Greater(controller.Inertia, 0.8f, 
                "Low entropy should produce high inertia");
        }
        
        [Test]
        public void Inertia_HighEntropy_LowInertia()
        {
            var controller = new CriticalityController();
            
            // Many different actions = high entropy = low inertia
            for (int i = 0; i < 20; i++)
            {
                controller.RecordAction(i);
            }
            controller.Update();
            
            Assert.Less(controller.Inertia, 0.5f, 
                "High entropy should produce low inertia");
        }
        
        [Test]
        public void HistorySize_OldActionsDropped()
        {
            var controller = new CriticalityController();
            
            // Record action 0 fifteen times
            for (int i = 0; i < 15; i++)
            {
                controller.RecordAction(0);
            }
            
            // Then record action 1 ten times (total 25, history is 20)
            for (int i = 0; i < 10; i++)
            {
                controller.RecordAction(1);
            }
            controller.Update();
            
            // History should now be: 5 zeros + 10 ones = 15:10 = mix
            // Should have non-zero entropy
            Assert.Greater(controller.Entropy, 0f, 
                "After overflow, history should contain a mix of actions with non-zero entropy");
        }
        
        [Test]
        public void CustomSettings_Respected()
        {
            var controller = new CriticalityController(
                historySize: 10,
                minTemperature: 0.3f,
                maxTemperature: 3.0f,
                temperatureAdjustRate: 0.2f,
                targetEntropy: 0.6f
            );
            
            Assert.AreEqual(10, controller.HistorySize);
            Assert.AreEqual(0.3f, controller.MinTemperature, 0.001f);
            Assert.AreEqual(3.0f, controller.MaxTemperature, 0.001f);
            
            // Test that custom min/max are respected
            controller.SetTemperature(0.1f);
            Assert.AreEqual(0.3f, controller.Temperature, 0.001f, "Custom min should be respected");
            
            controller.SetTemperature(5f);
            Assert.AreEqual(3.0f, controller.Temperature, 0.001f, "Custom max should be respected");
        }
        
        [Test]
        public void Reset_ClearsAllState()
        {
            var controller = new CriticalityController();
            
            // Build up some state
            for (int i = 0; i < 20; i++)
            {
                controller.RecordAction(0);
            }
            controller.Update();
            controller.SetTemperature(1.5f);
            
            // Reset
            controller.Reset();
            
            Assert.AreEqual(1f, controller.Temperature, 0.001f, "Temperature should reset to 1");
            Assert.AreEqual(0.5f, controller.Inertia, 0.001f, "Inertia should reset to 0.5");
            Assert.AreEqual(0f, controller.Entropy, 0.001f, "Entropy should reset to 0");
        }
    }
}
