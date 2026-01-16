using NUnit.Framework;
using UnityEngine;
using SwarmAI;

namespace SwarmAI.Tests
{
    /// <summary>
    /// Unit tests for SwarmGroup.
    /// </summary>
    [TestFixture]
    public class SwarmGroupTests
    {
        #region Constructor Tests
        
        [Test]
        public void SwarmGroup_Constructor_AssignsId()
        {
            var group1 = new SwarmGroup();
            var group2 = new SwarmGroup();
            
            Assert.Greater(group1.GroupId, 0);
            Assert.Greater(group2.GroupId, group1.GroupId);
        }
        
        [Test]
        public void SwarmGroup_Constructor_SetsName()
        {
            var group = new SwarmGroup("TestGroup");
            Assert.AreEqual("TestGroup", group.Name);
        }
        
        [Test]
        public void SwarmGroup_Constructor_DefaultsEmptyMembers()
        {
            var group = new SwarmGroup();
            Assert.AreEqual(0, group.MemberCount);
            Assert.IsNotNull(group.Members);
        }
        
        #endregion
        
        #region Property Tests
        
        [Test]
        public void SwarmGroup_HasLeader_FalseWhenNoLeader()
        {
            var group = new SwarmGroup();
            Assert.IsFalse(group.HasLeader);
        }
        
        [Test]
        public void SwarmGroup_CenterOfMass_ReturnsZeroWhenEmpty()
        {
            var group = new SwarmGroup();
            Assert.AreEqual(Vector3.zero, group.CenterOfMass);
        }
        
        [Test]
        public void SwarmGroup_AverageVelocity_ReturnsZeroWhenEmpty()
        {
            var group = new SwarmGroup();
            Assert.AreEqual(Vector3.zero, group.AverageVelocity);
        }
        
        #endregion
        
        #region Name Tests
        
        [Test]
        public void SwarmGroup_Name_CanBeChanged()
        {
            var group = new SwarmGroup("Original");
            group.Name = "NewName";
            Assert.AreEqual("NewName", group.Name);
        }
        
        #endregion
    }
}
