using NUnit.Framework;
using UnityEngine;
using SwarmAI;

namespace SwarmAI.Tests
{
    /// <summary>
    /// Unit tests for AgentState and concrete state implementations.
    /// </summary>
    [TestFixture]
    public class AgentStateTests
    {
        [Test]
        public void IdleState_HasCorrectType()
        {
            var state = new IdleState();
            
            Assert.AreEqual(AgentStateType.Idle, state.Type);
        }
        
        [Test]
        public void MovingState_HasCorrectType()
        {
            var state = new MovingState(Vector3.zero);
            
            Assert.AreEqual(AgentStateType.Moving, state.Type);
        }
        
        [Test]
        public void SeekingState_HasCorrectType_PositionConstructor()
        {
            var state = new SeekingState(Vector3.one);
            
            Assert.AreEqual(AgentStateType.Seeking, state.Type);
        }
        
        [Test]
        public void SeekingState_HasCorrectType_TransformConstructor()
        {
            var go = new GameObject("Target");
            var state = new SeekingState(go.transform);
            
            Assert.AreEqual(AgentStateType.Seeking, state.Type);
            
            Object.DestroyImmediate(go);
        }
        
        [Test]
        public void FleeingState_HasCorrectType_PositionConstructor()
        {
            var state = new FleeingState(Vector3.zero);
            
            Assert.AreEqual(AgentStateType.Fleeing, state.Type);
        }
        
        [Test]
        public void FleeingState_HasCorrectType_TransformConstructor()
        {
            var go = new GameObject("Threat");
            var state = new FleeingState(go.transform);
            
            Assert.AreEqual(AgentStateType.Fleeing, state.Type);
            
            Object.DestroyImmediate(go);
        }
        
        [Test]
        public void AgentStateType_ContainsAllExpectedValues()
        {
            // Verify all state types we use are defined
            Assert.IsTrue(System.Enum.IsDefined(typeof(AgentStateType), AgentStateType.Idle));
            Assert.IsTrue(System.Enum.IsDefined(typeof(AgentStateType), AgentStateType.Moving));
            Assert.IsTrue(System.Enum.IsDefined(typeof(AgentStateType), AgentStateType.Seeking));
            Assert.IsTrue(System.Enum.IsDefined(typeof(AgentStateType), AgentStateType.Fleeing));
            Assert.IsTrue(System.Enum.IsDefined(typeof(AgentStateType), AgentStateType.Gathering));
            Assert.IsTrue(System.Enum.IsDefined(typeof(AgentStateType), AgentStateType.Attacking));
            Assert.IsTrue(System.Enum.IsDefined(typeof(AgentStateType), AgentStateType.Returning));
            Assert.IsTrue(System.Enum.IsDefined(typeof(AgentStateType), AgentStateType.Following));
            Assert.IsTrue(System.Enum.IsDefined(typeof(AgentStateType), AgentStateType.Patrolling));
            Assert.IsTrue(System.Enum.IsDefined(typeof(AgentStateType), AgentStateType.Dead));
        }
    }
}
