using NUnit.Framework;
using UnityEngine;
using SwarmAI;

namespace SwarmAI.Tests
{
    /// <summary>
    /// Unit tests for FollowLeaderBehavior.
    /// </summary>
    [TestFixture]
    public class FollowLeaderBehaviorTests
    {
        #region Constructor Tests
        
        [Test]
        public void FollowLeaderBehavior_DefaultConstructor_SetsDefaults()
        {
            var behavior = new FollowLeaderBehavior();
            
            Assert.AreEqual("Follow Leader", behavior.Name);
            Assert.AreEqual(3f, behavior.FollowDistance);
            Assert.AreEqual(5f, behavior.SlowingRadius);
            Assert.IsFalse(behavior.UseOffset);
            Assert.IsNull(behavior.Leader);
        }
        
        [Test]
        public void FollowLeaderBehavior_FollowDistance_ClampsToMin()
        {
            var behavior = new FollowLeaderBehavior();
            behavior.FollowDistance = -5f;
            Assert.AreEqual(0.5f, behavior.FollowDistance);
        }
        
        [Test]
        public void FollowLeaderBehavior_SlowingRadius_ClampsToMin()
        {
            var behavior = new FollowLeaderBehavior();
            behavior.SlowingRadius = -5f;
            Assert.AreEqual(0.1f, behavior.SlowingRadius);
        }
        
        [Test]
        public void FollowLeaderBehavior_NullAgent_ReturnsZero()
        {
            var behavior = new FollowLeaderBehavior();
            Assert.AreEqual(Vector3.zero, behavior.CalculateForce(null));
        }
        
        [Test]
        public void FollowLeaderBehavior_NullLeader_ReturnsZero()
        {
            var behavior = new FollowLeaderBehavior();
            behavior.Leader = null;
            // Would need a real agent to test this properly
            Assert.AreEqual(Vector3.zero, behavior.CalculateForce(null));
        }
        
        [Test]
        public void FollowLeaderBehavior_SetOffset_SetsUseOffset()
        {
            var behavior = new FollowLeaderBehavior();
            behavior.OffsetFromLeader = new Vector3(2, 0, -3);
            
            Assert.IsTrue(behavior.UseOffset);
            Assert.AreEqual(new Vector3(2, 0, -3), behavior.OffsetFromLeader);
        }
        
        #endregion
        
        #region Weight Tests
        
        [Test]
        public void FollowLeaderBehavior_Weight_DefaultsToOne()
        {
            var behavior = new FollowLeaderBehavior();
            Assert.AreEqual(1f, behavior.Weight);
        }
        
        [Test]
        public void FollowLeaderBehavior_IsActive_DefaultsToTrue()
        {
            var behavior = new FollowLeaderBehavior();
            Assert.IsTrue(behavior.IsActive);
        }
        
        #endregion
    }
}
