using NUnit.Framework;
using UnityEngine;
using NPCBrain.BehaviorTree;
using NPCBrain.BehaviorTree.Decorators;

namespace NPCBrain.Tests.Editor
{
    [TestFixture]
    public class DecoratorTests
    {
        private GameObject _testObject;
        private TestBrain _brain;
        
        [SetUp]
        public void SetUp()
        {
            _testObject = new GameObject("TestNPC");
            _brain = _testObject.AddComponent<TestBrain>();
            if (_brain.Blackboard == null)
            {
                _brain.InitializeForTests();
            }
        }
        
        [TearDown]
        public void TearDown()
        {
            if (_testObject != null)
            {
                Object.DestroyImmediate(_testObject);
            }
        }
        
        [Test]
        public void Inverter_Success_ReturnsFailure()
        {
            var child = new MockNode(NodeStatus.Success);
            var inverter = new Inverter(child);
            
            NodeStatus result = inverter.Execute(_brain);
            
            Assert.AreEqual(NodeStatus.Failure, result);
        }
        
        [Test]
        public void Inverter_Failure_ReturnsSuccess()
        {
            var child = new MockNode(NodeStatus.Failure);
            var inverter = new Inverter(child);
            
            NodeStatus result = inverter.Execute(_brain);
            
            Assert.AreEqual(NodeStatus.Success, result);
        }
        
        [Test]
        public void Inverter_Running_ReturnsRunning()
        {
            var child = new MockNode(NodeStatus.Running);
            var inverter = new Inverter(child);
            
            NodeStatus result = inverter.Execute(_brain);
            
            Assert.AreEqual(NodeStatus.Running, result);
        }
        
        [Test]
        public void Inverter_NullChild_ReturnsFailure()
        {
            var inverter = new Inverter(null);
            
            NodeStatus result = inverter.Execute(_brain);
            
            Assert.AreEqual(NodeStatus.Failure, result);
        }
        
        [Test]
        public void Repeater_RepeatsSpecifiedTimes()
        {
            var child = new MockNode(NodeStatus.Success);
            var repeater = new Repeater(child, 3);
            
            Assert.AreEqual(NodeStatus.Running, repeater.Execute(_brain));
            Assert.AreEqual(1, repeater.CurrentCount);
            
            Assert.AreEqual(NodeStatus.Running, repeater.Execute(_brain));
            Assert.AreEqual(2, repeater.CurrentCount);
            
            Assert.AreEqual(NodeStatus.Success, repeater.Execute(_brain));
            Assert.AreEqual(3, repeater.CurrentCount);
        }
        
        [Test]
        public void Repeater_Running_DoesNotIncrement()
        {
            var child = new MockNode(NodeStatus.Running);
            var repeater = new Repeater(child, 3);
            
            repeater.Execute(_brain);
            repeater.Execute(_brain);
            
            Assert.AreEqual(0, repeater.CurrentCount);
        }
        
        [Test]
        public void Repeater_Infinite_AlwaysReturnsRunning()
        {
            var child = new MockNode(NodeStatus.Success);
            var repeater = new Repeater(child, -1);
            
            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(NodeStatus.Running, repeater.Execute(_brain));
            }
            
            Assert.AreEqual(100, repeater.CurrentCount);
        }
        
        [Test]
        public void Repeater_Reset_ClearsCount()
        {
            var child = new MockNode(NodeStatus.Success);
            var repeater = new Repeater(child, 5);
            
            repeater.Execute(_brain);
            repeater.Execute(_brain);
            Assert.AreEqual(2, repeater.CurrentCount);
            
            repeater.Reset();
            
            Assert.AreEqual(0, repeater.CurrentCount);
        }
        
        [Test]
        public void Cooldown_BeforeCooldown_ExecutesChild()
        {
            var child = new MockNode(NodeStatus.Success);
            var cooldown = new Cooldown(child, 1f);
            
            NodeStatus result = cooldown.Execute(_brain);
            
            Assert.AreEqual(NodeStatus.Success, result);
            Assert.AreEqual(1, child.TickCount);
        }
        
        [Test]
        public void Cooldown_DuringCooldown_ReturnsFailure()
        {
            var child = new MockNode(NodeStatus.Success);
            var cooldown = new Cooldown(child, 10f);
            
            cooldown.Execute(_brain);
            
            NodeStatus result = cooldown.Execute(_brain);
            
            Assert.AreEqual(NodeStatus.Failure, result);
            Assert.AreEqual(1, child.TickCount);
        }
        
        [Test]
        public void Cooldown_ChildFailure_DoesNotStartCooldown()
        {
            var child = new MockNode(NodeStatus.Failure);
            var cooldown = new Cooldown(child, 10f);
            
            cooldown.Execute(_brain);
            
            Assert.IsFalse(cooldown.IsOnCooldown());
        }
        
        [Test]
        public void Cooldown_ResetCooldown_AllowsImmediateExecution()
        {
            var child = new MockNode(NodeStatus.Success);
            var cooldown = new Cooldown(child, 10f);
            
            cooldown.Execute(_brain);
            Assert.IsTrue(cooldown.IsOnCooldown());
            
            cooldown.ResetCooldown();
            
            Assert.IsFalse(cooldown.IsOnCooldown());
        }
        
        [Test]
        public void Cooldown_RemainingCooldown_ReturnsCorrectValue()
        {
            var child = new MockNode(NodeStatus.Success);
            var cooldown = new Cooldown(child, 5f);
            
            cooldown.Execute(_brain);
            
            float remaining = cooldown.RemainingCooldown();
            Assert.Greater(remaining, 4.9f);
            Assert.LessOrEqual(remaining, 5f);
        }
        
        [Test]
        public void DecoratorNode_Abort_AbortsChild()
        {
            var child = new MockNode(NodeStatus.Running);
            var inverter = new Inverter(child);
            
            inverter.Execute(_brain);
            inverter.Abort(_brain);
            
            Assert.AreEqual(1, child.OnExitCount);
        }
        
        [Test]
        public void DecoratorNode_Reset_ResetsChild()
        {
            var child = new MockNode(NodeStatus.Running);
            var inverter = new Inverter(child);
            
            inverter.Execute(_brain);
            Assert.IsTrue(child.IsRunning);
            
            inverter.Reset();
            
            Assert.IsFalse(child.IsRunning);
        }
    }
}
