using NUnit.Framework;
using UnityEngine;
using SwarmAI;

namespace SwarmAI.Tests
{
    /// <summary>
    /// Unit tests for Phase 3 SwarmMessage types.
    /// </summary>
    [TestFixture]
    public class SwarmMessagePhase3Tests
    {
        #region Follow Message Tests
        
        [Test]
        public void SwarmMessage_Follow_SetsType()
        {
            var msg = SwarmMessage.Follow(5);
            Assert.AreEqual(SwarmMessageType.Follow, msg.Type);
        }
        
        [Test]
        public void SwarmMessage_Follow_SetsLeaderId()
        {
            var msg = SwarmMessage.Follow(42);
            Assert.AreEqual(42f, msg.Value);
        }
        
        #endregion
        
        #region Formation Message Tests
        
        [Test]
        public void SwarmMessage_JoinFormation_SetsType()
        {
            var msg = SwarmMessage.JoinFormation(1, 0);
            Assert.AreEqual(SwarmMessageType.JoinFormation, msg.Type);
        }
        
        [Test]
        public void SwarmMessage_JoinFormation_SetsFormationId()
        {
            var msg = SwarmMessage.JoinFormation(5, 2);
            Assert.AreEqual(5f, msg.Value);
        }
        
        [Test]
        public void SwarmMessage_JoinFormation_SetsSlotIndex()
        {
            var msg = SwarmMessage.JoinFormation(1, 3);
            Assert.AreEqual("3", msg.Tag);
        }
        
        [Test]
        public void SwarmMessage_LeaveFormation_SetsType()
        {
            var msg = SwarmMessage.LeaveFormation();
            Assert.AreEqual(SwarmMessageType.LeaveFormation, msg.Type);
        }
        
        [Test]
        public void SwarmMessage_FormationUpdate_SetsPosition()
        {
            var pos = new Vector3(5, 0, 10);
            var msg = SwarmMessage.FormationUpdate(pos);
            
            Assert.AreEqual(SwarmMessageType.FormationUpdate, msg.Type);
            Assert.AreEqual(pos, msg.Position);
        }
        
        #endregion
        
        #region Group Message Tests
        
        [Test]
        public void SwarmMessage_JoinGroup_SetsType()
        {
            var msg = SwarmMessage.JoinGroup(1);
            Assert.AreEqual(SwarmMessageType.JoinGroup, msg.Type);
        }
        
        [Test]
        public void SwarmMessage_JoinGroup_SetsGroupId()
        {
            var msg = SwarmMessage.JoinGroup(7);
            Assert.AreEqual(7f, msg.Value);
        }
        
        [Test]
        public void SwarmMessage_LeaveGroup_SetsType()
        {
            var msg = SwarmMessage.LeaveGroup();
            Assert.AreEqual(SwarmMessageType.LeaveGroup, msg.Type);
        }
        
        #endregion
        
        #region Resource Message Tests
        
        [Test]
        public void SwarmMessage_GatherResource_SetsType()
        {
            var msg = SwarmMessage.GatherResource(Vector3.zero, null);
            Assert.AreEqual(SwarmMessageType.GatherResource, msg.Type);
        }
        
        [Test]
        public void SwarmMessage_GatherResource_SetsPosition()
        {
            var pos = new Vector3(10, 0, 20);
            var msg = SwarmMessage.GatherResource(pos, null);
            Assert.AreEqual(pos, msg.Position);
        }
        
        [Test]
        public void SwarmMessage_ReturnToBase_SetsType()
        {
            var msg = SwarmMessage.ReturnToBase(Vector3.zero);
            Assert.AreEqual(SwarmMessageType.ReturnToBase, msg.Type);
        }
        
        [Test]
        public void SwarmMessage_ReturnToBase_SetsPosition()
        {
            var pos = new Vector3(0, 0, 0);
            var msg = SwarmMessage.ReturnToBase(pos);
            Assert.AreEqual(pos, msg.Position);
        }
        
        [Test]
        public void SwarmMessage_ResourceFound_SetsType()
        {
            var msg = SwarmMessage.ResourceFound(Vector3.zero, 100f);
            Assert.AreEqual(SwarmMessageType.ResourceFound, msg.Type);
        }
        
        [Test]
        public void SwarmMessage_ResourceFound_SetsAmount()
        {
            var msg = SwarmMessage.ResourceFound(Vector3.zero, 50f);
            Assert.AreEqual(50f, msg.Value);
        }
        
        [Test]
        public void SwarmMessage_ResourceDepleted_SetsType()
        {
            var msg = SwarmMessage.ResourceDepleted(Vector3.zero);
            Assert.AreEqual(SwarmMessageType.ResourceDepleted, msg.Type);
        }
        
        [Test]
        public void SwarmMessage_ResourceDepleted_SetsPosition()
        {
            var pos = new Vector3(5, 0, 5);
            var msg = SwarmMessage.ResourceDepleted(pos);
            Assert.AreEqual(pos, msg.Position);
        }
        
        #endregion
    }
}
