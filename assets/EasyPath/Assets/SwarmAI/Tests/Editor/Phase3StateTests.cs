using NUnit.Framework;
using UnityEngine;
using SwarmAI;

namespace SwarmAI.Tests
{
    /// <summary>
    /// Unit tests for Phase 3 states.
    /// </summary>
    [TestFixture]
    public class Phase3StateTests
    {
        #region GatheringState Tests
        
        [Test]
        public void GatheringState_Constructor_SetsType()
        {
            var state = new GatheringState(null, Vector3.zero);
            Assert.AreEqual(AgentStateType.Gathering, state.Type);
        }
        
        [Test]
        public void GatheringState_Constructor_SetsBasePosition()
        {
            var basePos = new Vector3(10, 0, 10);
            var state = new GatheringState(null, basePos);
            Assert.AreEqual(basePos, state.BasePosition);
        }
        
        [Test]
        public void GatheringState_Constructor_DefaultCarryCapacity()
        {
            var state = new GatheringState(null, Vector3.zero);
            Assert.AreEqual(10f, state.CarryCapacity);
        }
        
        [Test]
        public void GatheringState_Constructor_CustomCarryCapacity()
        {
            var state = new GatheringState(null, Vector3.zero, 50f);
            Assert.AreEqual(50f, state.CarryCapacity);
        }
        
        [Test]
        public void GatheringState_CurrentCarry_StartsAtZero()
        {
            var state = new GatheringState(null, Vector3.zero);
            Assert.AreEqual(0f, state.CurrentCarry);
        }
        
        [Test]
        public void GatheringState_IsHarvesting_StartsFalse()
        {
            var state = new GatheringState(null, Vector3.zero);
            Assert.IsFalse(state.IsHarvesting);
        }
        
        [Test]
        public void GatheringState_WithInitialCarry_SetsAmount()
        {
            var state = new GatheringState(null, Vector3.zero, 20f, 5f);
            Assert.AreEqual(5f, state.CurrentCarry);
        }
        
        [Test]
        public void GatheringState_WithInitialCarry_ClampsToCapacity()
        {
            var state = new GatheringState(null, Vector3.zero, 10f, 50f);
            Assert.AreEqual(10f, state.CurrentCarry); // Clamped to capacity
        }
        
        #endregion
        
        #region ReturningState Tests
        
        [Test]
        public void ReturningState_Constructor_SetsType()
        {
            var state = new ReturningState(Vector3.zero, 5f);
            Assert.AreEqual(AgentStateType.Returning, state.Type);
        }
        
        [Test]
        public void ReturningState_Constructor_SetsBasePosition()
        {
            var basePos = new Vector3(10, 0, 10);
            var state = new ReturningState(basePos, 5f);
            Assert.AreEqual(basePos, state.BasePosition);
        }
        
        [Test]
        public void ReturningState_Constructor_SetsCarryAmount()
        {
            var state = new ReturningState(Vector3.zero, 15f);
            Assert.AreEqual(15f, state.CarryAmount);
        }
        
        [Test]
        public void ReturningState_CarryAmount_ClampsToZero()
        {
            var state = new ReturningState(Vector3.zero, -5f);
            Assert.AreEqual(0f, state.CarryAmount);
        }
        
        [Test]
        public void ReturningState_HasDeposited_StartsFalse()
        {
            var state = new ReturningState(Vector3.zero, 5f);
            Assert.IsFalse(state.HasDeposited);
        }
        
        #endregion
        
        #region FollowingState Tests
        
        [Test]
        public void FollowingState_Constructor_SetsType()
        {
            var state = new FollowingState((SwarmAgent)null, 3f);
            Assert.AreEqual(AgentStateType.Following, state.Type);
        }
        
        [Test]
        public void FollowingState_Constructor_SetsFollowDistance()
        {
            var state = new FollowingState((SwarmAgent)null, 5f);
            Assert.AreEqual(5f, state.FollowDistance);
        }
        
        [Test]
        public void FollowingState_FollowDistance_ClampsToMin()
        {
            var state = new FollowingState((SwarmAgent)null, 0.5f);
            Assert.AreEqual(1f, state.FollowDistance); // Clamped to 1
        }
        
        [Test]
        public void FollowingState_ById_AcceptsLeaderId()
        {
            var state = new FollowingState(42, 3f);
            Assert.AreEqual(AgentStateType.Following, state.Type);
            // Leader will be null until Enter() is called with a SwarmManager
            Assert.IsNull(state.Leader);
        }
        
        #endregion
    }
}
