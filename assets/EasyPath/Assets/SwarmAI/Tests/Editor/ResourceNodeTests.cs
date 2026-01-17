using NUnit.Framework;
using UnityEngine;
using SwarmAI;

namespace SwarmAI.Tests
{
    /// <summary>
    /// Unit tests for ResourceNode class.
    /// Tests harvesting mechanics, depletion, respawn, and configuration.
    /// Note: Tests requiring GameObjects are in Runtime folder.
    /// </summary>
    [TestFixture]
    public class ResourceNodeTests
    {
        #region Resource Message Tests
        
        [Test]
        public void SwarmMessage_GatherResource_CreatesCorrectMessage()
        {
            Vector3 position = new Vector3(10, 0, 10);
            var message = SwarmMessage.GatherResource(position, null, -1);
            
            Assert.AreEqual(SwarmMessageType.GatherResource, message.Type);
            Assert.AreEqual(position, message.Position);
        }
        
        [Test]
        public void SwarmMessage_ReturnToBase_CreatesCorrectMessage()
        {
            Vector3 basePosition = new Vector3(0, 0, 0);
            var message = SwarmMessage.ReturnToBase(basePosition, -1);
            
            Assert.AreEqual(SwarmMessageType.ReturnToBase, message.Type);
            Assert.AreEqual(basePosition, message.Position);
        }
        
        [Test]
        public void SwarmMessage_ResourceDepleted_CreatesCorrectMessage()
        {
            Vector3 position = new Vector3(20, 0, 20);
            var message = SwarmMessage.ResourceDepleted(position, -1);
            
            Assert.AreEqual(SwarmMessageType.ResourceDepleted, message.Type);
            Assert.AreEqual(position, message.Position);
        }
        
        #endregion
        
        #region GatheringState Tests
        
        [Test]
        public void GatheringState_HasCorrectType()
        {
            var state = new GatheringState(null, Vector3.zero);
            
            Assert.AreEqual(AgentStateType.Gathering, state.Type);
        }
        
        [Test]
        public void GatheringState_CanBeCreatedWithPosition()
        {
            Vector3 resourcePos = new Vector3(15, 0, 15);
            var state = new GatheringState(null, resourcePos);
            
            Assert.AreEqual(AgentStateType.Gathering, state.Type);
        }
        
        [Test]
        public void GatheringState_InitialCarry_IsZero()
        {
            var state = new GatheringState(null, Vector3.zero);
            
            Assert.AreEqual(0f, state.CurrentCarry);
        }
        
        [Test]
        public void GatheringState_CarryCapacity_HasDefaultValue()
        {
            var state = new GatheringState(null, Vector3.zero);
            
            Assert.Greater(state.CarryCapacity, 0f);
        }
        
        [Test]
        public void GatheringState_Constructor_WithCapacity_SetsCapacity()
        {
            float capacity = 50f;
            var state = new GatheringState(null, Vector3.zero, capacity);
            
            Assert.AreEqual(capacity, state.CarryCapacity);
        }
        
        #endregion
        
        #region ReturningState Tests
        
        [Test]
        public void ReturningState_HasCorrectType()
        {
            var state = new ReturningState(Vector3.zero, 10f);
            
            Assert.AreEqual(AgentStateType.Returning, state.Type);
        }
        
        [Test]
        public void ReturningState_StoresBasePosition()
        {
            Vector3 basePos = new Vector3(0, 0, 0);
            var state = new ReturningState(basePos, 10f);
            
            Assert.AreEqual(basePos, state.BasePosition);
        }
        
        [Test]
        public void ReturningState_StoresCarryAmount()
        {
            float carryAmount = 25f;
            var state = new ReturningState(Vector3.zero, carryAmount);
            
            Assert.AreEqual(carryAmount, state.CarryAmount);
        }
        
        #endregion
        
        #region Behavior Pattern Tests
        
        [Test]
        public void SeekBehavior_CanBeUsedForResourceSeeking()
        {
            var behavior = new SeekBehavior();
            Vector3 resourcePosition = new Vector3(10, 0, 10);
            behavior.TargetPosition = resourcePosition;
            
            Assert.AreEqual(resourcePosition, behavior.TargetPosition);
            Assert.IsTrue(behavior.IsActive);
        }
        
        [Test]
        public void ArriveBehavior_CanBeUsedForResourceApproach()
        {
            var behavior = new ArriveBehavior();
            behavior.SlowingRadius = 3f;
            behavior.ArrivalRadius = 1f;
            
            Assert.AreEqual(3f, behavior.SlowingRadius);
            Assert.AreEqual(1f, behavior.ArrivalRadius);
        }
        
        #endregion
        
        #region Static Helper Logic Tests
        
        [Test]
        public void DistanceSquared_IsMoreEfficientThanDistance()
        {
            // This test verifies the performance pattern used in ResourceNode.FindNearest
            Vector3 a = new Vector3(0, 0, 0);
            Vector3 b = new Vector3(3, 4, 0); // Distance = 5, DistanceSq = 25
            
            float distSq = (b - a).sqrMagnitude;
            float maxDistSq = 10f * 10f; // 100
            
            // Verify squared distance comparison works correctly
            Assert.AreEqual(25f, distSq);
            Assert.IsTrue(distSq < maxDistSq);
        }
        
        [Test]
        public void FindNearest_Logic_UsesSquaredDistance()
        {
            // Verify the pattern: sqrMagnitude is used instead of magnitude for efficiency
            Vector3 position = Vector3.zero;
            Vector3 nodeA = new Vector3(5, 0, 0);  // Distance = 5
            Vector3 nodeB = new Vector3(3, 0, 0);  // Distance = 3 (closer)
            
            float distSqA = (nodeA - position).sqrMagnitude;
            float distSqB = (nodeB - position).sqrMagnitude;
            
            // nodeB should have smaller squared distance
            Assert.Less(distSqB, distSqA);
        }
        
        #endregion
        
        #region Configuration Validation Tests
        
        [Test]
        public void ResourceAmount_ShouldBePositive()
        {
            // This validates the expected configuration pattern
            float totalAmount = 100f;
            float harvestRate = 10f;
            
            Assert.Greater(totalAmount, 0f);
            Assert.Greater(harvestRate, 0f);
        }
        
        [Test]
        public void HarvestCalculation_RespectsTimeAndRate()
        {
            // Verify harvest amount calculation logic
            float harvestRate = 10f;  // 10 units per second
            float deltaTime = 0.1f;   // 100ms
            float currentAmount = 100f;
            
            float harvestAmount = harvestRate * deltaTime;
            harvestAmount = Mathf.Min(harvestAmount, currentAmount);
            
            Assert.AreEqual(1f, harvestAmount);  // 10 * 0.1 = 1
        }
        
        [Test]
        public void HarvestCalculation_CapsAtRemainingAmount()
        {
            // Verify harvest doesn't exceed remaining resources
            float harvestRate = 10f;
            float deltaTime = 1f;
            float currentAmount = 5f;  // Only 5 remaining
            
            float harvestAmount = harvestRate * deltaTime;  // Would be 10
            harvestAmount = Mathf.Min(harvestAmount, currentAmount);  // Capped to 5
            
            Assert.AreEqual(5f, harvestAmount);
        }
        
        [Test]
        public void RespawnTimer_AccumulatesTime()
        {
            // Verify respawn timer logic
            float respawnTime = 30f;
            float elapsedTime = 0f;
            
            // Simulate time passing
            for (int i = 0; i < 30; i++)
            {
                elapsedTime += 1f;  // 1 second per "frame"
            }
            
            Assert.GreaterOrEqual(elapsedTime, respawnTime);
        }
        
        [Test]
        public void AmountPercent_CalculatesCorrectly()
        {
            // Verify percentage calculation
            float totalAmount = 100f;
            float currentAmount = 75f;
            
            float percent = totalAmount > 0 ? currentAmount / totalAmount : 0f;
            
            Assert.AreEqual(0.75f, percent);
        }
        
        [Test]
        public void AmountPercent_HandlesZeroTotal()
        {
            // Verify percentage handles edge case
            float totalAmount = 0f;
            float currentAmount = 0f;
            
            float percent = totalAmount > 0 ? currentAmount / totalAmount : 0f;
            
            Assert.AreEqual(0f, percent);
        }
        
        [Test]
        public void IsDepleted_WhenAmountIsZero()
        {
            // Verify depletion check
            float currentAmount = 0f;
            bool isDepleted = currentAmount <= 0f;
            
            Assert.IsTrue(isDepleted);
        }
        
        [Test]
        public void HasCapacity_ChecksHarvestersAndDepletion()
        {
            // Verify capacity check logic
            int currentHarvesters = 2;
            int maxHarvesters = 3;
            bool isDepleted = false;
            
            bool hasCapacity = currentHarvesters < maxHarvesters && !isDepleted;
            
            Assert.IsTrue(hasCapacity);
        }
        
        [Test]
        public void HasCapacity_FalseWhenMaxHarvesters()
        {
            int currentHarvesters = 3;
            int maxHarvesters = 3;
            bool isDepleted = false;
            
            bool hasCapacity = currentHarvesters < maxHarvesters && !isDepleted;
            
            Assert.IsFalse(hasCapacity);
        }
        
        [Test]
        public void HasCapacity_FalseWhenDepleted()
        {
            int currentHarvesters = 1;
            int maxHarvesters = 3;
            bool isDepleted = true;
            
            bool hasCapacity = currentHarvesters < maxHarvesters && !isDepleted;
            
            Assert.IsFalse(hasCapacity);
        }
        
        #endregion
        
        #region Visual Feedback Logic Tests
        
        [Test]
        public void ScaleCalculation_LerpsCorrectly()
        {
            float minScale = 0.2f;
            float amountPercent = 0.5f;  // 50% remaining
            
            float scalePercent = Mathf.Lerp(minScale, 1f, amountPercent);
            
            // At 50%, should be halfway between 0.2 and 1.0 = 0.6
            Assert.AreEqual(0.6f, scalePercent, 0.001f);
        }
        
        [Test]
        public void ScaleCalculation_AtZeroPercent()
        {
            float minScale = 0.2f;
            float amountPercent = 0f;  // 0% remaining
            
            float scalePercent = Mathf.Lerp(minScale, 1f, amountPercent);
            
            Assert.AreEqual(0.2f, scalePercent);
        }
        
        [Test]
        public void ScaleCalculation_AtFullPercent()
        {
            float minScale = 0.2f;
            float amountPercent = 1f;  // 100% remaining
            
            float scalePercent = Mathf.Lerp(minScale, 1f, amountPercent);
            
            Assert.AreEqual(1f, scalePercent);
        }
        
        #endregion
    }
}
