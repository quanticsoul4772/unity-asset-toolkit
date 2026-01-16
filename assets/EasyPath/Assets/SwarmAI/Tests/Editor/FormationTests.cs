using NUnit.Framework;
using UnityEngine;
using SwarmAI;

namespace SwarmAI.Tests
{
    /// <summary>
    /// Unit tests for the formation system.
    /// </summary>
    [TestFixture]
    public class FormationTests
    {
        #region FormationSlot Tests
        
        [Test]
        public void FormationSlot_Constructor_SetsOffset()
        {
            var slot = new FormationSlot(new Vector3(1, 0, 2), 0);
            Assert.AreEqual(new Vector3(1, 0, 2), slot.LocalOffset);
        }
        
        [Test]
        public void FormationSlot_Constructor_SetsPriority()
        {
            var slot = new FormationSlot(Vector3.zero, 5);
            Assert.AreEqual(5, slot.Priority);
        }
        
        [Test]
        public void FormationSlot_IsOccupied_FalseWhenNoAgent()
        {
            var slot = new FormationSlot(Vector3.zero, 0);
            Assert.IsFalse(slot.IsOccupied);
        }
        
        [Test]
        public void FormationSlot_GetWorldPosition_AppliesRotation()
        {
            var slot = new FormationSlot(new Vector3(1, 0, 0), 0);
            Vector3 center = Vector3.zero;
            Quaternion rotation = Quaternion.Euler(0, 90, 0); // 90 degrees Y
            
            Vector3 worldPos = slot.GetWorldPosition(center, rotation);
            
            // After 90 degree rotation, (1,0,0) becomes approximately (0,0,-1)
            Assert.AreEqual(0f, worldPos.x, 0.01f);
            Assert.AreEqual(0f, worldPos.y, 0.01f);
            Assert.AreEqual(-1f, worldPos.z, 0.01f);
        }
        
        [Test]
        public void FormationSlot_GetWorldPosition_AppliesOffset()
        {
            var slot = new FormationSlot(new Vector3(2, 0, 3), 0);
            Vector3 center = new Vector3(10, 0, 10);
            Quaternion rotation = Quaternion.identity;
            
            Vector3 worldPos = slot.GetWorldPosition(center, rotation);
            
            Assert.AreEqual(12f, worldPos.x);
            Assert.AreEqual(0f, worldPos.y);
            Assert.AreEqual(13f, worldPos.z);
        }
        
        #endregion
        
        #region FormationType Tests
        
        [Test]
        public void FormationType_HasExpectedValues()
        {
            Assert.AreEqual(0, (int)FormationType.None);
            Assert.AreEqual(1, (int)FormationType.Line);
            Assert.AreEqual(2, (int)FormationType.Column);
            Assert.AreEqual(3, (int)FormationType.Circle);
            Assert.AreEqual(4, (int)FormationType.Wedge);
            Assert.AreEqual(5, (int)FormationType.V);
            Assert.AreEqual(6, (int)FormationType.Box);
            Assert.AreEqual(7, (int)FormationType.Custom);
        }
        
        #endregion
    }
}
