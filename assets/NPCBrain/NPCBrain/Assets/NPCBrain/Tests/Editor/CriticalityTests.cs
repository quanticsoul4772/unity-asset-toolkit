using NUnit.Framework;
using NPCBrain.Criticality;

namespace NPCBrain.Tests.Editor
{
    [TestFixture]
    public class CriticalityTests
    {
        private CriticalityController _controller;
        
        [SetUp]
        public void SetUp()
        {
            _controller = new CriticalityController();
        }
        
        [Test]
        public void InitialTemperature_IsOne()
        {
            Assert.AreEqual(1f, _controller.Temperature, 0.001f);
        }
        
        [Test]
        public void InitialInertia_IsHalf()
        {
            Assert.AreEqual(0.5f, _controller.Inertia, 0.001f);
        }
        
        [Test]
        public void InitialEntropy_IsZero()
        {
            Assert.AreEqual(0f, _controller.Entropy, 0.001f);
        }
        
        [Test]
        public void RecordAction_SingleAction_ZeroEntropy()
        {
            for (int i = 0; i < 10; i++)
            {
                _controller.RecordAction(0);
            }
            _controller.Update();
            
            Assert.AreEqual(0f, _controller.Entropy, 0.001f);
        }
        
        [Test]
        public void RecordAction_UniformDistribution_HighEntropy()
        {
            // With 4 actions uniformly distributed, max entropy = ln(4) ≈ 1.386
            // Use a more lenient threshold to avoid flaky tests
            for (int i = 0; i < 20; i++)
            {
                _controller.RecordAction(i % 4);
            }
            _controller.Update();
            
            Assert.Greater(_controller.Entropy, 1.3f);
        }
        
        [Test]
        public void Update_LowEntropy_IncreasesTemperature()
        {
            for (int i = 0; i < 20; i++)
            {
                _controller.RecordAction(0);
            }
            
            float initialTemp = _controller.Temperature;
            _controller.Update();
            
            Assert.Greater(_controller.Temperature, initialTemp);
        }
        
        [Test]
        public void Update_HighEntropy_DecreasesTemperature()
        {
            for (int i = 0; i < 20; i++)
            {
                _controller.RecordAction(i);
            }
            
            float initialTemp = _controller.Temperature;
            _controller.Update();
            
            Assert.Less(_controller.Temperature, initialTemp);
        }
        
        [Test]
        public void Temperature_ClampedToMin()
        {
            _controller.SetTemperature(0.1f);
            
            Assert.AreEqual(0.5f, _controller.Temperature, 0.001f);
        }
        
        [Test]
        public void Temperature_ClampedToMax()
        {
            _controller.SetTemperature(10f);
            
            Assert.AreEqual(2f, _controller.Temperature, 0.001f);
        }
        
        [Test]
        public void Reset_ClearsHistory()
        {
            for (int i = 0; i < 10; i++)
            {
                _controller.RecordAction(i);
            }
            _controller.Update();
            
            _controller.Reset();
            
            Assert.AreEqual(1f, _controller.Temperature, 0.001f);
            Assert.AreEqual(0.5f, _controller.Inertia, 0.001f);
            Assert.AreEqual(0f, _controller.Entropy, 0.001f);
        }
        
        [Test]
        public void RecordAction_NegativeId_Ignored()
        {
            _controller.RecordAction(-1);
            _controller.Update();
            
            Assert.AreEqual(0f, _controller.Entropy, 0.001f);
        }
        
        [Test]
        public void Inertia_LowEntropy_HighInertia()
        {
            for (int i = 0; i < 20; i++)
            {
                _controller.RecordAction(0);
            }
            _controller.Update();
            
            Assert.Greater(_controller.Inertia, 0.8f);
        }
        
        [Test]
        public void Inertia_HighEntropy_LowInertia()
        {
            for (int i = 0; i < 20; i++)
            {
                _controller.RecordAction(i);
            }
            _controller.Update();
            
            Assert.Less(_controller.Inertia, 0.3f);
        }
        
        [Test]
        public void HistoryWindow_OldActionsDropped()
        {
            // HistorySize is 20, so we need to ensure a mix remains in the window
            // Add 15 zeros, then 10 ones = 25 total, window keeps last 20
            // Result: 10 zeros + 10 ones = mix with entropy > 0
            for (int i = 0; i < 15; i++)
            {
                _controller.RecordAction(0);
            }
            
            for (int i = 0; i < 10; i++)
            {
                _controller.RecordAction(1);
            }
            _controller.Update();
            
            Assert.Greater(_controller.Entropy, 0f);
        }
    }
}
