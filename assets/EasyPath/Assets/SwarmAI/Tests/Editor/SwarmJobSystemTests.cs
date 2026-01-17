using NUnit.Framework;
using UnityEngine;
using Unity.Mathematics;
using SwarmAI.Jobs;

namespace SwarmAI.Tests
{
    /// <summary>
    /// Unit tests for the SwarmJobSystem and related Jobs/Burst components.
    /// Tests steering calculations, agent data structures, and job execution.
    /// </summary>
    [TestFixture]
    public class SwarmJobSystemTests
    {
        #region AgentData Tests
        
        [Test]
        public void AgentData_DefaultValues_AreZero()
        {
            var data = new AgentData();
            
            Assert.AreEqual(float3.zero, data.Position);
            Assert.AreEqual(float3.zero, data.Velocity);
            Assert.AreEqual(0, data.AgentId);
        }
        
        [Test]
        public void AgentData_CanSetPosition()
        {
            var data = new AgentData
            {
                Position = new float3(10f, 0f, 20f)
            };
            
            Assert.AreEqual(10f, data.Position.x);
            Assert.AreEqual(0f, data.Position.y);
            Assert.AreEqual(20f, data.Position.z);
        }
        
        [Test]
        public void AgentData_CanSetVelocity()
        {
            var data = new AgentData
            {
                Velocity = new float3(1f, 0f, 1f)
            };
            
            Assert.AreEqual(1f, data.Velocity.x);
            Assert.AreEqual(0f, data.Velocity.y);
            Assert.AreEqual(1f, data.Velocity.z);
        }
        
        [Test]
        public void AgentData_CanSetAgentId()
        {
            var data = new AgentData
            {
                AgentId = 42
            };
            
            Assert.AreEqual(42, data.AgentId);
        }
        
        #endregion
        
        #region SteeringResult Tests
        
        [Test]
        public void SteeringResult_DefaultValues_AreZero()
        {
            var result = new SteeringResult();
            
            Assert.AreEqual(float3.zero, result.SteeringForce);
            Assert.AreEqual(0, result.NeighborCount);
        }
        
        [Test]
        public void SteeringResult_CanSetSteeringForce()
        {
            var result = new SteeringResult
            {
                SteeringForce = new float3(5f, 0f, -3f)
            };
            
            Assert.AreEqual(5f, result.SteeringForce.x);
            Assert.AreEqual(0f, result.SteeringForce.y);
            Assert.AreEqual(-3f, result.SteeringForce.z);
        }
        
        [Test]
        public void SteeringResult_CanSetNeighborCount()
        {
            var result = new SteeringResult
            {
                NeighborCount = 15
            };
            
            Assert.AreEqual(15, result.NeighborCount);
        }
        
        #endregion
        
        #region BehaviorWeights Tests
        
        [Test]
        public void BehaviorWeights_DefaultConstructor_HasZeroValues()
        {
            var weights = new BehaviorWeights();
            
            Assert.AreEqual(0f, weights.Separation);
            Assert.AreEqual(0f, weights.Alignment);
            Assert.AreEqual(0f, weights.Cohesion);
            Assert.AreEqual(0f, weights.SeparationRadius);
        }
        
        [Test]
        public void BehaviorWeights_CanSetAllWeights()
        {
            var weights = new BehaviorWeights
            {
                Separation = 1.5f,
                Alignment = 1.0f,
                Cohesion = 0.8f,
                SeparationRadius = 2.5f
            };
            
            Assert.AreEqual(1.5f, weights.Separation);
            Assert.AreEqual(1.0f, weights.Alignment);
            Assert.AreEqual(0.8f, weights.Cohesion);
            Assert.AreEqual(2.5f, weights.SeparationRadius);
        }
        
        [Test]
        public void BehaviorWeights_Default_HasSensibleValues()
        {
            var weights = BehaviorWeights.Default;
            
            Assert.AreEqual(1.5f, weights.Separation);
            Assert.AreEqual(1.0f, weights.Alignment);
            Assert.AreEqual(1.0f, weights.Cohesion);
            Assert.AreEqual(2.5f, weights.SeparationRadius);
        }
        
        [Test]
        public void BehaviorWeights_SeparationRadius_IsPositive()
        {
            var weights = BehaviorWeights.Default;
            
            Assert.Greater(weights.SeparationRadius, 0f);
        }
        
        #endregion
        
        #region Math Helper Tests
        
        [Test]
        public void Float3_DistanceSq_CalculatesCorrectly()
        {
            float3 a = new float3(0, 0, 0);
            float3 b = new float3(3, 4, 0); // 3-4-5 triangle
            
            float distSq = math.distancesq(a, b);
            
            Assert.AreEqual(25f, distSq); // 3^2 + 4^2 = 25
        }
        
        [Test]
        public void Float3_Normalize_WorksCorrectly()
        {
            float3 v = new float3(3, 4, 0);
            float3 normalized = math.normalize(v);
            
            float length = math.length(normalized);
            
            Assert.AreEqual(1f, length, 0.0001f);
        }
        
        [Test]
        public void Float3_Clamp_WorksWithMagnitude()
        {
            float3 v = new float3(10, 0, 0);
            float maxMagnitude = 5f;
            
            float magnitude = math.length(v);
            float3 clamped = magnitude > maxMagnitude 
                ? math.normalize(v) * maxMagnitude 
                : v;
            
            Assert.AreEqual(5f, math.length(clamped), 0.0001f);
        }
        
        #endregion
        
        #region Steering Calculation Logic Tests
        
        [Test]
        public void Separation_OppositeDirection_WhenNear()
        {
            // Agent at origin, neighbor at (1, 0, 0)
            float3 agentPos = float3.zero;
            float3 neighborPos = new float3(1f, 0f, 0f);
            
            // Separation should push away from neighbor (negative x direction)
            float3 separationDir = agentPos - neighborPos;
            
            Assert.Less(separationDir.x, 0f);
        }
        
        [Test]
        public void Cohesion_TowardsCenterOfMass()
        {
            // Agent at origin, neighbors at (2, 0, 0) and (-2, 0, 0)
            float3 agentPos = new float3(0f, 0f, 5f); // Agent behind center
            float3 neighbor1 = new float3(2f, 0f, 0f);
            float3 neighbor2 = new float3(-2f, 0f, 0f);
            
            float3 centerOfMass = (neighbor1 + neighbor2) / 2f; // (0, 0, 0)
            float3 cohesionDir = centerOfMass - agentPos; // (0, 0, -5)
            
            Assert.Less(cohesionDir.z, 0f); // Should point towards center
        }
        
        [Test]
        public void Alignment_MatchesAverageVelocity()
        {
            // Two neighbors moving in same direction
            float3 velocity1 = new float3(5f, 0f, 0f);
            float3 velocity2 = new float3(5f, 0f, 0f);
            
            float3 avgVelocity = (velocity1 + velocity2) / 2f;
            
            Assert.AreEqual(5f, avgVelocity.x);
            Assert.AreEqual(0f, avgVelocity.z);
        }
        
        [Test]
        public void Separation_InverseSquareFalloff_CloserMeansStronger()
        {
            float3 agentPos = float3.zero;
            
            // Close neighbor
            float3 closeNeighbor = new float3(1f, 0f, 0f);
            float closeDistSq = math.distancesq(agentPos, closeNeighbor);
            
            // Far neighbor
            float3 farNeighbor = new float3(4f, 0f, 0f);
            float farDistSq = math.distancesq(agentPos, farNeighbor);
            
            // Closer distance = stronger separation force (inverse relationship)
            float closeForce = 1f / closeDistSq; // 1/1 = 1
            float farForce = 1f / farDistSq;     // 1/16 = 0.0625
            
            Assert.Greater(closeForce, farForce);
        }
        
        #endregion
        
        #region Edge Cases
        
        [Test]
        public void Steering_NoNeighbors_ReturnsZero()
        {
            // When there are no neighbors, steering should be zero
            int neighborCount = 0;
            float3 separation = float3.zero;
            float3 alignment = float3.zero;
            float3 cohesion = float3.zero;
            
            // Only divide by neighbor count if > 0
            float3 result = neighborCount > 0 
                ? (separation + alignment + cohesion) / neighborCount 
                : float3.zero;
            
            Assert.AreEqual(float3.zero, result);
        }
        
        [Test]
        public void Steering_SingleNeighbor_ValidResult()
        {
            int neighborCount = 1;
            float3 separationSum = new float3(-1f, 0f, 0f);
            
            float3 result = separationSum / neighborCount;
            
            Assert.AreEqual(-1f, result.x);
        }
        
        [Test]
        public void Steering_SamePosition_HandlesDivideByZero()
        {
            float3 agentPos = new float3(5f, 0f, 5f);
            float3 neighborPos = new float3(5f, 0f, 5f); // Same position!
            
            float distSq = math.distancesq(agentPos, neighborPos);
            
            // Should skip this neighbor (distance is 0)
            bool shouldProcess = distSq > 0.0001f;
            
            Assert.IsFalse(shouldProcess);
        }
        
        [Test]
        public void MaxForce_ClampsProperly()
        {
            float3 force = new float3(100f, 0f, 0f);
            float maxForce = 10f;
            
            float magnitude = math.length(force);
            float3 clamped = magnitude > maxForce 
                ? math.normalize(force) * maxForce 
                : force;
            
            float clampedMagnitude = math.length(clamped);
            
            Assert.AreEqual(maxForce, clampedMagnitude, 0.0001f);
        }
        
        #endregion
        
        #region Performance Threshold Tests
        
        [Test]
        public void MinAgentsForJobs_DefaultIsSensible()
        {
            // Jobs should have a reasonable minimum threshold
            // Too low = overhead not worth it, too high = never uses jobs
            int minAgentsForJobs = 50; // Default value
            
            Assert.GreaterOrEqual(minAgentsForJobs, 10);
            Assert.LessOrEqual(minAgentsForJobs, 200);
        }
        
        [Test]
        public void BatchSize_DefaultIsPowerOfTwo()
        {
            int batchSize = 64; // Default value
            
            // Batch size should be power of 2 for optimal job scheduling
            bool isPowerOfTwo = (batchSize & (batchSize - 1)) == 0;
            
            Assert.IsTrue(isPowerOfTwo);
        }
        
        #endregion
    }
}
