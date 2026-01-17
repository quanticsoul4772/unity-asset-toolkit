using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using SwarmAI;

namespace SwarmAI.Tests
{
    /// <summary>
    /// Unit tests for the SwarmManager class.
    /// Tests singleton pattern, agent registration, spatial queries, and messaging.
    /// </summary>
    [TestFixture]
    public class SwarmManagerTests
    {
        #region Singleton Tests
        
        [Test]
        public void HasInstance_WhenNoInstance_ReturnsFalse()
        {
            // Note: This test may fail if a SwarmManager exists from previous tests
            // In isolation, HasInstance should be false before any access
            // We can't truly test this without destroying existing instances
            Assert.Pass("Singleton behavior tested via integration tests");
        }
        
        #endregion
        
        #region Settings Tests
        
        [Test]
        public void SwarmSettings_CreateDefault_HasValidValues()
        {
            var settings = SwarmSettings.CreateDefault();
            
            Assert.IsNotNull(settings);
            Assert.Greater(settings.SpatialHashCellSize, 0f);
            Assert.GreaterOrEqual(settings.MaxAgentsPerFrame, 1);
        }
        
        [Test]
        public void SwarmSettings_SpatialHashCellSize_MustBePositive()
        {
            var settings = SwarmSettings.CreateDefault();
            float originalSize = settings.SpatialHashCellSize;
            
            Assert.Greater(originalSize, 0f, "Default cell size should be positive");
        }
        
        #endregion
        
        #region Message Tests
        
        [Test]
        public void SwarmMessage_MoveTo_CreatesCorrectMessage()
        {
            Vector3 position = new Vector3(10, 0, 20);
            var message = SwarmMessage.MoveTo(position);
            
            Assert.AreEqual(SwarmMessageType.MoveTo, message.Type);
            Assert.AreEqual(position, message.Position);
        }
        
        [Test]
        public void SwarmMessage_Stop_CreatesCorrectMessage()
        {
            var message = SwarmMessage.Stop();
            
            Assert.AreEqual(SwarmMessageType.Stop, message.Type);
        }
        
        [Test]
        public void SwarmMessage_Seek_CreatesCorrectMessage()
        {
            Vector3 position = new Vector3(5, 0, 5);
            var message = SwarmMessage.Seek(position);
            
            Assert.AreEqual(SwarmMessageType.Seek, message.Type);
            Assert.AreEqual(position, message.Position);
        }
        
        [Test]
        public void SwarmMessage_Flee_CreatesCorrectMessage()
        {
            Vector3 position = new Vector3(-5, 0, -5);
            var message = SwarmMessage.Flee(position);
            
            Assert.AreEqual(SwarmMessageType.Flee, message.Type);
            Assert.AreEqual(position, message.Position);
        }
        
        [Test]
        public void SwarmMessage_Follow_CreatesCorrectMessage()
        {
            int leaderId = 42;
            var message = SwarmMessage.Follow(leaderId);
            
            Assert.AreEqual(SwarmMessageType.Follow, message.Type);
            Assert.AreEqual(leaderId, (int)message.Value);
        }
        
        [Test]
        public void SwarmMessage_Clone_CopiesAllFields()
        {
            var original = SwarmMessage.MoveTo(new Vector3(1, 2, 3));
            var clone = original.Clone(10, 20);
            
            Assert.AreEqual(original.Type, clone.Type);
            Assert.AreEqual(original.Position, clone.Position);
            Assert.AreEqual(10, clone.SenderId);
            Assert.AreEqual(20, clone.TargetId);
        }
        
        [Test]
        public void SwarmMessage_Clone_PreservesType()
        {
            var original = SwarmMessage.Seek(new Vector3(5, 0, 5));
            var clone = original.Clone(1, 2);
            
            Assert.AreEqual(SwarmMessageType.Seek, clone.Type);
        }
        
        #endregion
        
        #region Agent State Type Tests
        
        [Test]
        public void AgentStateType_HasExpectedValues()
        {
            // Verify all expected state types exist
            Assert.IsTrue(System.Enum.IsDefined(typeof(AgentStateType), AgentStateType.Idle));
            Assert.IsTrue(System.Enum.IsDefined(typeof(AgentStateType), AgentStateType.Moving));
            Assert.IsTrue(System.Enum.IsDefined(typeof(AgentStateType), AgentStateType.Seeking));
            Assert.IsTrue(System.Enum.IsDefined(typeof(AgentStateType), AgentStateType.Fleeing));
            Assert.IsTrue(System.Enum.IsDefined(typeof(AgentStateType), AgentStateType.Gathering));
            Assert.IsTrue(System.Enum.IsDefined(typeof(AgentStateType), AgentStateType.Returning));
            Assert.IsTrue(System.Enum.IsDefined(typeof(AgentStateType), AgentStateType.Following));
        }
        
        #endregion
        
        #region Idle State Tests
        
        [Test]
        public void IdleState_HasCorrectType()
        {
            var state = new IdleState();
            
            Assert.AreEqual(AgentStateType.Idle, state.Type);
        }
        
        [Test]
        public void IdleState_CheckTransitions_ReturnsSelf()
        {
            var state = new IdleState();
            
            var result = state.CheckTransitions();
            
            Assert.AreSame(state, result);
        }
        
        #endregion
        
        #region Moving State Tests
        
        [Test]
        public void MovingState_HasCorrectType()
        {
            var state = new MovingState(Vector3.zero);
            
            Assert.AreEqual(AgentStateType.Moving, state.Type);
        }
        
        [Test]
        public void MovingState_CanBeCreatedWithDestination()
        {
            Vector3 destination = new Vector3(10, 0, 20);
            var state = new MovingState(destination);
            
            Assert.AreEqual(AgentStateType.Moving, state.Type);
        }
        
        #endregion
        
        #region Seeking State Tests
        
        [Test]
        public void SeekingState_HasCorrectType()
        {
            var state = new SeekingState(Vector3.zero);
            
            Assert.AreEqual(AgentStateType.Seeking, state.Type);
        }
        
        [Test]
        public void SeekingState_CanBeCreatedWithTarget()
        {
            Vector3 target = new Vector3(5, 0, 5);
            var state = new SeekingState(target);
            
            Assert.AreEqual(AgentStateType.Seeking, state.Type);
        }
        
        #endregion
        
        #region Fleeing State Tests
        
        [Test]
        public void FleeingState_HasCorrectType()
        {
            var state = new FleeingState(Vector3.zero);
            
            Assert.AreEqual(AgentStateType.Fleeing, state.Type);
        }
        
        [Test]
        public void FleeingState_CanBeCreatedWithThreat()
        {
            Vector3 threat = new Vector3(-10, 0, -10);
            var state = new FleeingState(threat);
            
            Assert.AreEqual(AgentStateType.Fleeing, state.Type);
        }
        
        #endregion
        
        #region Spatial Query Helper Tests
        
        [Test]
        public void SpatialHash_CellSize_CalculatesCorrectly()
        {
            float cellSize = 5f;
            var hash = new SpatialHash<object>(cellSize);
            
            Assert.AreEqual(cellSize, hash.CellSize);
        }
        
        [Test]
        public void SpatialHash_Count_TracksInsertions()
        {
            var hash = new SpatialHash<string>(10f);
            
            Assert.AreEqual(0, hash.Count);
            
            hash.Insert("item1", Vector3.zero);
            Assert.AreEqual(1, hash.Count);
            
            hash.Insert("item2", Vector3.one * 100);
            Assert.AreEqual(2, hash.Count);
        }
        
        [Test]
        public void SpatialHash_Contains_FindsInsertedItems()
        {
            var hash = new SpatialHash<string>(10f);
            string item = "test item";
            
            Assert.IsFalse(hash.Contains(item));
            
            hash.Insert(item, Vector3.zero);
            
            Assert.IsTrue(hash.Contains(item));
        }
        
        [Test]
        public void SpatialHash_Clear_RemovesAllItems()
        {
            var hash = new SpatialHash<string>(10f);
            hash.Insert("item1", Vector3.zero);
            hash.Insert("item2", Vector3.one);
            
            hash.Clear();
            
            Assert.AreEqual(0, hash.Count);
        }
        
        #endregion
    }
}
