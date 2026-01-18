using NUnit.Framework;
using UnityEngine;
using NPCBrain.Components;

namespace NPCBrain.Tests.Editor
{
    /// <summary>
    /// Unit tests for game components (LootPoint, EscapeZone, CoverPoint).
    /// </summary>
    [TestFixture]
    public class ComponentTests
    {
        private GameObject _testObject;
        
        [TearDown]
        public void TearDown()
        {
            if (_testObject != null)
            {
                Object.DestroyImmediate(_testObject);
            }
        }
        
        #region LootPoint Tests
        
        [Test]
        public void LootPoint_InitialState_NotStolen()
        {
            var loot = CreateLootPoint(100);
            
            Assert.IsFalse(loot.IsStolen);
            Assert.IsNull(loot.StolenBy);
            Assert.AreEqual(100, loot.Value);
        }
        
        [Test]
        public void LootPoint_TrySteal_WithinRadius_Succeeds()
        {
            var loot = CreateLootPoint(100);
            var thief = new GameObject("Thief");
            thief.transform.position = loot.transform.position + Vector3.forward * 1f; // Within default radius
            
            bool result = loot.TrySteal(thief);
            
            Assert.IsTrue(result);
            Assert.IsTrue(loot.IsStolen);
            Assert.AreEqual(thief, loot.StolenBy);
            
            Object.DestroyImmediate(thief);
        }
        
        [Test]
        public void LootPoint_TrySteal_OutsideRadius_Fails()
        {
            var loot = CreateLootPoint(100);
            var thief = new GameObject("Thief");
            thief.transform.position = loot.transform.position + Vector3.forward * 100f; // Far away
            
            bool result = loot.TrySteal(thief);
            
            Assert.IsFalse(result);
            Assert.IsFalse(loot.IsStolen);
            
            Object.DestroyImmediate(thief);
        }
        
        [Test]
        public void LootPoint_TrySteal_AlreadyStolen_Fails()
        {
            var loot = CreateLootPoint(100);
            var thief1 = new GameObject("Thief1");
            var thief2 = new GameObject("Thief2");
            thief1.transform.position = loot.transform.position;
            thief2.transform.position = loot.transform.position;
            
            loot.TrySteal(thief1);
            bool secondAttempt = loot.TrySteal(thief2);
            
            Assert.IsFalse(secondAttempt);
            Assert.AreEqual(thief1, loot.StolenBy);
            
            Object.DestroyImmediate(thief1);
            Object.DestroyImmediate(thief2);
        }
        
        [Test]
        public void LootPoint_OnStolen_EventFired()
        {
            var loot = CreateLootPoint(100);
            var thief = new GameObject("Thief");
            thief.transform.position = loot.transform.position;
            
            LootPoint eventLoot = null;
            GameObject eventThief = null;
            loot.OnStolen += (l, t) => { eventLoot = l; eventThief = t; };
            
            loot.TrySteal(thief);
            
            Assert.AreEqual(loot, eventLoot);
            Assert.AreEqual(thief, eventThief);
            
            Object.DestroyImmediate(thief);
        }
        
        [Test]
        public void LootPoint_Reset_RestoresState()
        {
            var loot = CreateLootPoint(100);
            var thief = new GameObject("Thief");
            thief.transform.position = loot.transform.position;
            
            loot.TrySteal(thief);
            loot.Reset();
            
            Assert.IsFalse(loot.IsStolen);
            Assert.IsNull(loot.StolenBy);
            
            Object.DestroyImmediate(thief);
        }
        
        private LootPoint CreateLootPoint(int value)
        {
            _testObject = new GameObject("LootPoint");
            var loot = _testObject.AddComponent<LootPoint>();
            
            // Set value via reflection since it's serialized
            var valueField = typeof(LootPoint).GetField("_value", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            valueField?.SetValue(loot, value);
            
            return loot;
        }
        
        #endregion
        
        #region EscapeZone Tests
        
        [Test]
        public void EscapeZone_InitialState_Empty()
        {
            var zone = CreateEscapeZone(5f);
            
            Assert.AreEqual(0, zone.EscapedCount);
            Assert.AreEqual(5f, zone.ZoneRadius);
        }
        
        [Test]
        public void EscapeZone_TryEscape_WithLoot_Succeeds()
        {
            var zone = CreateEscapeZone(5f);
            var robber = new GameObject("Robber");
            robber.transform.position = zone.transform.position;
            
            bool result = zone.TryEscape(robber, 100);
            
            Assert.IsTrue(result);
            Assert.AreEqual(1, zone.EscapedCount);
            
            Object.DestroyImmediate(robber);
        }
        
        [Test]
        public void EscapeZone_TryEscape_WithoutLoot_Fails()
        {
            var zone = CreateEscapeZone(5f, requiresLoot: true);
            var robber = new GameObject("Robber");
            robber.transform.position = zone.transform.position;
            
            bool result = zone.TryEscape(robber, 0);
            
            Assert.IsFalse(result);
            Assert.AreEqual(0, zone.EscapedCount);
            
            Object.DestroyImmediate(robber);
        }
        
        [Test]
        public void EscapeZone_TryEscape_OutsideRadius_Fails()
        {
            var zone = CreateEscapeZone(5f);
            var robber = new GameObject("Robber");
            robber.transform.position = zone.transform.position + Vector3.forward * 100f;
            
            bool result = zone.TryEscape(robber, 100);
            
            Assert.IsFalse(result);
            
            Object.DestroyImmediate(robber);
        }
        
        [Test]
        public void EscapeZone_TryEscape_AlreadyEscaped_Fails()
        {
            var zone = CreateEscapeZone(5f);
            var robber = new GameObject("Robber");
            robber.transform.position = zone.transform.position;
            
            zone.TryEscape(robber, 100);
            bool secondAttempt = zone.TryEscape(robber, 100);
            
            Assert.IsFalse(secondAttempt);
            Assert.AreEqual(1, zone.EscapedCount);
            
            Object.DestroyImmediate(robber);
        }
        
        [Test]
        public void EscapeZone_OnRobberEscaped_EventFired()
        {
            var zone = CreateEscapeZone(5f);
            var robber = new GameObject("Robber");
            robber.transform.position = zone.transform.position;
            
            GameObject eventRobber = null;
            int eventValue = 0;
            zone.OnRobberEscaped += (r, v) => { eventRobber = r; eventValue = v; };
            
            zone.TryEscape(robber, 250);
            
            Assert.AreEqual(robber, eventRobber);
            Assert.AreEqual(250, eventValue);
            
            Object.DestroyImmediate(robber);
        }
        
        [Test]
        public void EscapeZone_IsInZone_CorrectlyDetects()
        {
            var zone = CreateEscapeZone(5f);
            
            Assert.IsTrue(zone.IsInZone(zone.transform.position));
            Assert.IsTrue(zone.IsInZone(zone.transform.position + Vector3.forward * 4f));
            Assert.IsFalse(zone.IsInZone(zone.transform.position + Vector3.forward * 10f));
        }
        
        [Test]
        public void EscapeZone_Reset_ClearsEscaped()
        {
            var zone = CreateEscapeZone(5f);
            var robber = new GameObject("Robber");
            robber.transform.position = zone.transform.position;
            
            zone.TryEscape(robber, 100);
            zone.Reset();
            
            Assert.AreEqual(0, zone.EscapedCount);
            
            Object.DestroyImmediate(robber);
        }
        
        private EscapeZone CreateEscapeZone(float radius, bool requiresLoot = true)
        {
            _testObject = new GameObject("EscapeZone");
            var zone = _testObject.AddComponent<EscapeZone>();
            
            var radiusField = typeof(EscapeZone).GetField("_zoneRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            radiusField?.SetValue(zone, radius);
            
            var requiresField = typeof(EscapeZone).GetField("_requiresLoot", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            requiresField?.SetValue(zone, requiresLoot);
            
            return zone;
        }
        
        #endregion
        
        #region CoverPoint Tests
        
        [Test]
        public void CoverPoint_InitialState_NotOccupied()
        {
            var cover = CreateCoverPoint(1.5f);
            
            Assert.IsFalse(cover.IsOccupied);
            Assert.IsNull(cover.Occupant);
        }
        
        [Test]
        public void CoverPoint_TryHide_Succeeds()
        {
            var cover = CreateCoverPoint(1.5f);
            var hider = new GameObject("Hider");
            hider.transform.position = cover.transform.position;
            
            bool result = cover.TryHide(hider);
            
            Assert.IsTrue(result);
            Assert.IsTrue(cover.IsOccupied);
            Assert.AreEqual(hider, cover.Occupant);
            
            Object.DestroyImmediate(hider);
        }
        
        [Test]
        public void CoverPoint_TryHide_AlreadyOccupied_FailsForOthers()
        {
            var cover = CreateCoverPoint(1.5f);
            var hider1 = new GameObject("Hider1");
            var hider2 = new GameObject("Hider2");
            hider1.transform.position = cover.transform.position;
            hider2.transform.position = cover.transform.position;
            
            cover.TryHide(hider1);
            bool secondResult = cover.TryHide(hider2);
            
            Assert.IsFalse(secondResult);
            Assert.AreEqual(hider1, cover.Occupant);
            
            Object.DestroyImmediate(hider1);
            Object.DestroyImmediate(hider2);
        }
        
        [Test]
        public void CoverPoint_TryHide_SameHider_SucceedsAgain()
        {
            var cover = CreateCoverPoint(1.5f);
            var hider = new GameObject("Hider");
            hider.transform.position = cover.transform.position;
            
            cover.TryHide(hider);
            bool secondResult = cover.TryHide(hider);
            
            Assert.IsTrue(secondResult);
            
            Object.DestroyImmediate(hider);
        }
        
        [Test]
        public void CoverPoint_CanHide_ChecksDistance()
        {
            var cover = CreateCoverPoint(1.5f);
            var nearHider = new GameObject("Near");
            var farHider = new GameObject("Far");
            nearHider.transform.position = cover.transform.position + Vector3.forward * 2f;
            farHider.transform.position = cover.transform.position + Vector3.forward * 10f;
            
            Assert.IsTrue(cover.CanHide(nearHider)); // Within 2x radius
            Assert.IsFalse(cover.CanHide(farHider)); // Beyond 2x radius
            
            Object.DestroyImmediate(nearHider);
            Object.DestroyImmediate(farHider);
        }
        
        [Test]
        public void CoverPoint_Release_ClearsOccupant()
        {
            var cover = CreateCoverPoint(1.5f);
            var hider = new GameObject("Hider");
            hider.transform.position = cover.transform.position;
            
            cover.TryHide(hider);
            cover.Release();
            
            Assert.IsFalse(cover.IsOccupied);
            Assert.IsNull(cover.Occupant);
            
            Object.DestroyImmediate(hider);
        }
        
        [Test]
        public void CoverPoint_HidePosition_ReturnsTransformPosition()
        {
            var cover = CreateCoverPoint(1.5f);
            cover.transform.position = new Vector3(5f, 0f, 10f);
            
            Assert.AreEqual(new Vector3(5f, 0f, 10f), cover.HidePosition);
        }
        
        private CoverPoint CreateCoverPoint(float radius)
        {
            _testObject = new GameObject("CoverPoint");
            var cover = _testObject.AddComponent<CoverPoint>();
            
            var radiusField = typeof(CoverPoint).GetField("_hideRadius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            radiusField?.SetValue(cover, radius);
            
            return cover;
        }
        
        #endregion
    }
}
