using NUnit.Framework;
using NPCBrain.Criticality;

namespace NPCBrain.Tests.Editor
{
    /// <summary>
    /// Tests for the CriticalityController entropy-based temperature system.
    /// </summary>
    [TestFixture]
    public class CriticalityTests
    {
        #region Initialization Tests
        
        [Test]
        public void CriticalityController_DefaultValues_AreCorrect()
        {
            var controller = new CriticalityController();
            
            Assert.AreEqual(1f, controller.Temperature, 0.001f);
            Assert.AreEqual(0.5f, controller.Inertia, 0.001f);
            Assert.AreEqual(0f, controller.Entropy, 0.001f);
            Assert.AreEqual(CriticalityController.DefaultHistorySize, controller.HistorySize);
            Assert.AreEqual(CriticalityController.DefaultMinTemperature, controller.MinTemperature);
            Assert.AreEqual(CriticalityController.DefaultMaxTemperature, controller.MaxTemperature);
            Assert.AreEqual(CriticalityController.DefaultTargetEntropy, controller.TargetEntropy);
        }
        
        [Test]
        public void CriticalityController_CustomValues_AreStored()
        {
            var controller = new CriticalityController(
                historySize: 10,
                minTemperature: 0.3f,
                maxTemperature: 3.0f,
                temperatureAdjustRate: 0.2f,
                targetEntropy: 0.7f);
            
            Assert.AreEqual(10, controller.HistorySize);
            Assert.AreEqual(0.3f, controller.MinTemperature, 0.001f);
            Assert.AreEqual(3.0f, controller.MaxTemperature, 0.001f);
            Assert.AreEqual(0.7f, controller.TargetEntropy, 0.001f);
            Assert.AreEqual(0.2f, controller.TemperatureAdjustRate, 0.001f);
        }
        
        #endregion
        
        #region Action Recording Tests
        
        [Test]
        public void RecordAction_TracksActionHistory()
        {
            var controller = new CriticalityController();
            
            controller.RecordAction(0);
            controller.RecordAction(1);
            controller.RecordAction(2);
            
            Assert.AreEqual(3, controller.ActionHistoryCount);
            Assert.AreEqual(3, controller.UniqueActionCount);
        }
        
        [Test]
        public void RecordAction_NegativeId_IsIgnored()
        {
            var controller = new CriticalityController();
            
            controller.RecordAction(-1);
            controller.RecordAction(-5);
            
            Assert.AreEqual(0, controller.ActionHistoryCount);
        }
        
        [Test]
        public void RecordAction_ExceedsHistorySize_RemovesOldest()
        {
            var controller = new CriticalityController(
                historySize: 5,
                minTemperature: 0.5f,
                maxTemperature: 2.0f,
                temperatureAdjustRate: 0.1f,
                targetEntropy: 0.5f);
            
            // Record more actions than history size
            for (int i = 0; i < 10; i++)
            {
                controller.RecordAction(i % 3); // Cycle through 0, 1, 2
            }
            
            Assert.AreEqual(5, controller.ActionHistoryCount);
        }
        
        #endregion
        
        #region Entropy Calculation Tests
        
        [Test]
        public void Entropy_SingleAction_IsZero()
        {
            var controller = new CriticalityController();
            
            // Only one type of action
            controller.RecordAction(0);
            controller.RecordAction(0);
            controller.RecordAction(0);
            controller.Update();
            
            Assert.AreEqual(0f, controller.Entropy, 0.001f);
        }
        
        [Test]
        public void Entropy_TwoEqualActions_IsPositive()
        {
            var controller = new CriticalityController();
            
            // Equal distribution of two actions
            controller.RecordAction(0);
            controller.RecordAction(1);
            controller.RecordAction(0);
            controller.RecordAction(1);
            controller.Update();
            
            // Maximum entropy for 2 actions is ln(2) ≈ 0.693
            Assert.Greater(controller.Entropy, 0.5f);
        }
        
        [Test]
        public void Entropy_MultipleEqualActions_IncreasesWithVariety()
        {
            var controller2 = new CriticalityController();
            var controller3 = new CriticalityController();
            
            // Two actions
            controller2.RecordAction(0);
            controller2.RecordAction(1);
            controller2.Update();
            
            // Three actions
            controller3.RecordAction(0);
            controller3.RecordAction(1);
            controller3.RecordAction(2);
            controller3.Update();
            
            // More variety = higher entropy
            Assert.Greater(controller3.Entropy, controller2.Entropy);
        }
        
        #endregion
        
        #region Temperature Adjustment Tests
        
        [Test]
        public void Temperature_RepetitiveBehavior_Increases()
        {
            var controller = new CriticalityController(
                historySize: 10,
                minTemperature: 0.5f,
                maxTemperature: 2.0f,
                temperatureAdjustRate: 0.1f,
                targetEntropy: 0.5f);
            
            float initialTemp = controller.Temperature;
            
            // Record only one action repeatedly (low entropy)
            for (int i = 0; i < 10; i++)
            {
                controller.RecordAction(0);
                controller.Update();
            }
            
            // Temperature should increase due to low entropy
            Assert.Greater(controller.Temperature, initialTemp);
        }
        
        [Test]
        public void Temperature_VariedBehavior_Decreases()
        {
            var controller = new CriticalityController(
                historySize: 20,
                minTemperature: 0.5f,
                maxTemperature: 2.0f,
                temperatureAdjustRate: 0.1f,
                targetEntropy: 0.3f); // Low target entropy
            
            // Start with high temperature
            controller.SetTemperature(1.8f);
            float initialTemp = controller.Temperature;
            
            // Record varied actions (high entropy)
            for (int i = 0; i < 20; i++)
            {
                controller.RecordAction(i % 5); // Cycle through 5 actions
                controller.Update();
            }
            
            // Temperature should decrease due to high entropy
            Assert.Less(controller.Temperature, initialTemp);
        }
        
        [Test]
        public void Temperature_IsClampedToMinMax()
        {
            var controller = new CriticalityController(
                historySize: 10,
                minTemperature: 0.5f,
                maxTemperature: 2.0f,
                temperatureAdjustRate: 0.5f, // High adjust rate
                targetEntropy: 0.5f);
            
            // Try to push temperature very low
            controller.SetTemperature(0.1f);
            Assert.AreEqual(0.5f, controller.Temperature, 0.001f);
            
            // Try to push temperature very high
            controller.SetTemperature(5.0f);
            Assert.AreEqual(2.0f, controller.Temperature, 0.001f);
        }
        
        #endregion
        
        #region Inertia Tests
        
        [Test]
        public void Inertia_LowEntropy_IsHigh()
        {
            var controller = new CriticalityController();
            
            // Only one action (zero entropy)
            for (int i = 0; i < 5; i++)
            {
                controller.RecordAction(0);
            }
            controller.Update();
            
            // Inertia = 1 - normalizedEntropy, so should be 1 when entropy is 0
            Assert.AreEqual(1f, controller.Inertia, 0.001f);
        }
        
        [Test]
        public void Inertia_HighEntropy_IsLow()
        {
            var controller = new CriticalityController();
            
            // Many varied actions (high entropy)
            for (int i = 0; i < 10; i++)
            {
                controller.RecordAction(i);
            }
            controller.Update();
            
            // High entropy = low inertia
            Assert.Less(controller.Inertia, 0.3f);
        }
        
        #endregion
        
        #region Reset Tests
        
        [Test]
        public void Reset_ClearsAllState()
        {
            var controller = new CriticalityController();
            
            // Record some actions and update
            for (int i = 0; i < 5; i++)
            {
                controller.RecordAction(i);
            }
            controller.Update();
            controller.SetTemperature(1.5f);
            
            // Verify state changed
            Assert.Greater(controller.ActionHistoryCount, 0);
            Assert.Greater(controller.Entropy, 0f);
            Assert.AreEqual(1.5f, controller.Temperature, 0.001f);
            
            // Reset
            controller.Reset();
            
            // Verify state cleared
            Assert.AreEqual(0, controller.ActionHistoryCount);
            Assert.AreEqual(0, controller.UniqueActionCount);
            Assert.AreEqual(0f, controller.Entropy, 0.001f);
            Assert.AreEqual(1f, controller.Temperature, 0.001f);
            Assert.AreEqual(0.5f, controller.Inertia, 0.001f);
        }
        
        #endregion
        
        #region Full Cycle Integration Test
        
        [Test]
        public void FullCycle_AdaptsToBehaviorPatterns()
        {
            var controller = new CriticalityController(
                historySize: 10,
                minTemperature: 0.5f,
                maxTemperature: 2.0f,
                temperatureAdjustRate: 0.15f,
                targetEntropy: 0.5f);
            
            // Phase 1: Repetitive behavior (should increase temperature)
            float phase1StartTemp = controller.Temperature;
            for (int i = 0; i < 10; i++)
            {
                controller.RecordAction(0); // Same action
                controller.Update();
            }
            float phase1EndTemp = controller.Temperature;
            
            Assert.Greater(phase1EndTemp, phase1StartTemp, 
                "Temperature should increase during repetitive behavior");
            
            // Phase 2: Varied behavior (should decrease temperature)
            float phase2StartTemp = controller.Temperature;
            for (int i = 0; i < 15; i++)
            {
                controller.RecordAction(i % 4); // Varied actions
                controller.Update();
            }
            float phase2EndTemp = controller.Temperature;
            
            Assert.Less(phase2EndTemp, phase2StartTemp, 
                "Temperature should decrease during varied behavior");
            
            // Phase 3: Return to repetitive (should increase again)
            float phase3StartTemp = controller.Temperature;
            for (int i = 0; i < 10; i++)
            {
                controller.RecordAction(2); // Same action
                controller.Update();
            }
            float phase3EndTemp = controller.Temperature;
            
            Assert.Greater(phase3EndTemp, phase3StartTemp, 
                "Temperature should increase again when returning to repetitive behavior");
        }
        
        [Test]
        public void FullCycle_InertiaCorrelatesWithEntropy()
        {
            var controller = new CriticalityController();
            
            // Start with varied behavior
            for (int i = 0; i < 10; i++)
            {
                controller.RecordAction(i % 5);
            }
            controller.Update();
            
            float variedInertia = controller.Inertia;
            float variedEntropy = controller.Entropy;
            
            // Reset and do repetitive behavior
            controller.Reset();
            for (int i = 0; i < 10; i++)
            {
                controller.RecordAction(0);
            }
            controller.Update();
            
            float repetitiveInertia = controller.Inertia;
            float repetitiveEntropy = controller.Entropy;
            
            // Verify correlation: high entropy = low inertia, low entropy = high inertia
            Assert.Less(repetitiveEntropy, 0.1f, "Repetitive behavior should have near-zero entropy");
            Assert.Greater(repetitiveInertia, variedInertia, 
                "Inertia should be higher during repetitive behavior");
            Assert.Less(repetitiveEntropy, variedEntropy,
                "Entropy should be lower during repetitive behavior");
        }
        
        #endregion
    }
}
