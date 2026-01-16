using NUnit.Framework;
using UnityEngine;
using SwarmAI;

namespace SwarmAI.Tests
{
    /// <summary>
    /// Unit tests for the SwarmMessage class.
    /// </summary>
    [TestFixture]
    public class SwarmMessageTests
    {
        [Test]
        public void Constructor_SetsTypeAndIds()
        {
            var message = new SwarmMessage(SwarmMessageType.MoveTo, 1, 2);
            
            Assert.AreEqual(SwarmMessageType.MoveTo, message.Type);
            Assert.AreEqual(1, message.SenderId);
            Assert.AreEqual(2, message.TargetId);
        }
        
        [Test]
        public void WithPosition_SetsPosition()
        {
            var position = new Vector3(10, 20, 30);
            var message = new SwarmMessage(SwarmMessageType.MoveTo)
                .WithPosition(position);
            
            Assert.AreEqual(position, message.Position);
        }
        
        [Test]
        public void WithValue_SetsValue()
        {
            var message = new SwarmMessage(SwarmMessageType.Custom)
                .WithValue(42.5f);
            
            Assert.AreEqual(42.5f, message.Value);
        }
        
        [Test]
        public void WithTag_SetsTag()
        {
            var message = new SwarmMessage(SwarmMessageType.Custom)
                .WithTag("test-tag");
            
            Assert.AreEqual("test-tag", message.Tag);
        }
        
        [Test]
        public void WithData_SetsData()
        {
            var data = new GameObject("TestObject");
            var message = new SwarmMessage(SwarmMessageType.Custom)
                .WithData(data);
            
            Assert.AreSame(data, message.Data);
            
            Object.DestroyImmediate(data);
        }
        
        [Test]
        public void IsBroadcast_TrueWhenTargetIdNegative()
        {
            var broadcast = new SwarmMessage(SwarmMessageType.Stop, 0, -1);
            var targeted = new SwarmMessage(SwarmMessageType.Stop, 0, 5);
            
            Assert.IsTrue(broadcast.IsBroadcast);
            Assert.IsFalse(targeted.IsBroadcast);
        }
        
        [Test]
        public void Clone_CopiesAllProperties()
        {
            var original = new SwarmMessage(SwarmMessageType.Seek, 1, 2)
                .WithPosition(new Vector3(10, 20, 30))
                .WithValue(99f)
                .WithTag("my-tag");
            
            var clone = original.Clone();
            
            Assert.AreEqual(original.Type, clone.Type);
            Assert.AreEqual(original.SenderId, clone.SenderId);
            Assert.AreEqual(original.TargetId, clone.TargetId);
            Assert.AreEqual(original.Position, clone.Position);
            Assert.AreEqual(original.Value, clone.Value);
            Assert.AreEqual(original.Tag, clone.Tag);
        }
        
        [Test]
        public void Clone_WithNewSenderAndTarget_OverridesIds()
        {
            var original = new SwarmMessage(SwarmMessageType.Seek, 1, 2);
            
            var clone = original.Clone(newSenderId: 10, newTargetId: 20);
            
            Assert.AreEqual(10, clone.SenderId);
            Assert.AreEqual(20, clone.TargetId);
        }
        
        [Test]
        public void Clone_WithDefaultIds_KeepsOriginalIds()
        {
            var original = new SwarmMessage(SwarmMessageType.Seek, 5, 10);
            
            var clone = original.Clone();
            
            Assert.AreEqual(5, clone.SenderId);
            Assert.AreEqual(10, clone.TargetId);
        }
        
        [Test]
        public void MoveTo_CreatesCorrectMessage()
        {
            var position = new Vector3(100, 0, 50);
            var message = SwarmMessage.MoveTo(position, senderId: 1, targetId: 2);
            
            Assert.AreEqual(SwarmMessageType.MoveTo, message.Type);
            Assert.AreEqual(position, message.Position);
            Assert.AreEqual(1, message.SenderId);
            Assert.AreEqual(2, message.TargetId);
        }
        
        [Test]
        public void Seek_CreatesCorrectMessage()
        {
            var position = new Vector3(50, 0, 50);
            var message = SwarmMessage.Seek(position);
            
            Assert.AreEqual(SwarmMessageType.Seek, message.Type);
            Assert.AreEqual(position, message.Position);
        }
        
        [Test]
        public void Flee_CreatesCorrectMessage()
        {
            var threatPosition = new Vector3(0, 0, 0);
            var message = SwarmMessage.Flee(threatPosition);
            
            Assert.AreEqual(SwarmMessageType.Flee, message.Type);
            Assert.AreEqual(threatPosition, message.Position);
        }
        
        [Test]
        public void Stop_CreatesCorrectMessage()
        {
            var message = SwarmMessage.Stop(senderId: 5);
            
            Assert.AreEqual(SwarmMessageType.Stop, message.Type);
            Assert.AreEqual(5, message.SenderId);
        }
        
        [Test]
        public void ThreatDetected_CreatesCorrectMessage()
        {
            var threatPos = new Vector3(10, 0, 10);
            var message = SwarmMessage.ThreatDetected(threatPos, 0.8f, senderId: 3);
            
            Assert.AreEqual(SwarmMessageType.ThreatDetected, message.Type);
            Assert.AreEqual(threatPos, message.Position);
            Assert.AreEqual(0.8f, message.Value);
            Assert.AreEqual(3, message.SenderId);
            Assert.IsTrue(message.IsBroadcast); // ThreatDetected is broadcast by default
        }
        
        [Test]
        public void FluentChaining_Works()
        {
            var message = new SwarmMessage(SwarmMessageType.Custom, 1, 2)
                .WithPosition(Vector3.one)
                .WithValue(10f)
                .WithTag("chain-test");
            
            Assert.AreEqual(Vector3.one, message.Position);
            Assert.AreEqual(10f, message.Value);
            Assert.AreEqual("chain-test", message.Tag);
        }
    }
}
