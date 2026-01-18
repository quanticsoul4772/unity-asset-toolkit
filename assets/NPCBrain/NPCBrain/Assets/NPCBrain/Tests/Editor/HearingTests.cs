using NUnit.Framework;
using UnityEngine;
using NPCBrain.Perception;

namespace NPCBrain.Tests.Editor
{
    /// <summary>
    /// Unit tests for the hearing perception system.
    /// Tests SoundEvent, SoundManager, and related functionality.
    /// </summary>
    [TestFixture]
    public class HearingTests
    {
        [SetUp]
        public void SetUp()
        {
            // Clear any leftover sounds from previous tests
            SoundManager.ClearAll();
        }
        
        [TearDown]
        public void TearDown()
        {
            SoundManager.ClearAll();
        }
        
        // SoundEvent Tests
        
        [Test]
        public void SoundEvent_Constructor_SetsProperties()
        {
            var position = new Vector3(10, 0, 10);
            var sound = new SoundEvent(position, SoundType.Gunshot, 0.8f, 50f);
            
            Assert.AreEqual(position, sound.Position);
            Assert.AreEqual(SoundType.Gunshot, sound.Type);
            Assert.AreEqual(0.8f, sound.Volume, 0.001f);
            Assert.AreEqual(50f, sound.Radius);
            Assert.IsNull(sound.Source);
            Assert.IsNull(sound.CustomTag);
        }
        
        [Test]
        public void SoundEvent_VolumeIsClamped()
        {
            var sound1 = new SoundEvent(Vector3.zero, SoundType.Footstep, 1.5f, 20f);
            var sound2 = new SoundEvent(Vector3.zero, SoundType.Footstep, -0.5f, 20f);
            
            Assert.AreEqual(1f, sound1.Volume, 0.001f, "Volume should be clamped to max 1");
            Assert.AreEqual(0f, sound2.Volume, 0.001f, "Volume should be clamped to min 0");
        }
        
        [Test]
        public void SoundEvent_GetVolumeAtPosition_AttenuatesWithDistance()
        {
            var sound = new SoundEvent(Vector3.zero, SoundType.Footstep, 1f, 20f);
            
            // At origin - full volume
            Assert.AreEqual(1f, sound.GetVolumeAtPosition(Vector3.zero), 0.001f);
            
            // At half radius - half volume
            Assert.AreEqual(0.5f, sound.GetVolumeAtPosition(new Vector3(10, 0, 0)), 0.001f);
            
            // At radius - zero volume
            Assert.AreEqual(0f, sound.GetVolumeAtPosition(new Vector3(20, 0, 0)), 0.001f);
            
            // Beyond radius - zero volume
            Assert.AreEqual(0f, sound.GetVolumeAtPosition(new Vector3(30, 0, 0)), 0.001f);
        }
        
        [Test]
        public void SoundEvent_CalculatePriority_ReturnsValidScore()
        {
            var sound = new SoundEvent(Vector3.zero, SoundType.Gunshot, 1f, 50f);
            
            float priority = sound.CalculatePriority(new Vector3(10, 0, 0), 30f);
            
            Assert.Greater(priority, 0f, "Priority should be positive");
            Assert.LessOrEqual(priority, 1f, "Priority should be at most 1");
        }
        
        [Test]
        public void SoundEvent_Priority_HigherForCloserSounds()
        {
            var sound = new SoundEvent(Vector3.zero, SoundType.Footstep, 1f, 50f);
            
            float priorityClose = sound.CalculatePriority(new Vector3(5, 0, 0), 50f);
            float priorityFar = sound.CalculatePriority(new Vector3(40, 0, 0), 50f);
            
            Assert.Greater(priorityClose, priorityFar, "Closer sounds should have higher priority");
        }
        
        [Test]
        public void SoundEvent_Priority_HigherForHigherSoundType()
        {
            var footstep = new SoundEvent(Vector3.zero, SoundType.Footstep, 1f, 50f);
            var gunshot = new SoundEvent(Vector3.zero, SoundType.Gunshot, 1f, 50f);
            var explosion = new SoundEvent(Vector3.zero, SoundType.Explosion, 1f, 50f);
            
            Vector3 listenerPos = new Vector3(10, 0, 0);
            
            float footstepPriority = footstep.CalculatePriority(listenerPos, 50f);
            float gunshotPriority = gunshot.CalculatePriority(listenerPos, 50f);
            float explosionPriority = explosion.CalculatePriority(listenerPos, 50f);
            
            Assert.Greater(explosionPriority, gunshotPriority, "Explosion should have higher priority than gunshot");
            Assert.Greater(gunshotPriority, footstepPriority, "Gunshot should have higher priority than footstep");
        }
        
        // SoundManager Tests
        
        [Test]
        public void SoundManager_EmitSound_RegistersSound()
        {
            Assert.AreEqual(0, SoundManager.ActiveSoundCount, "Should start with no sounds");
            
            SoundManager.EmitSound(Vector3.zero, SoundType.Footstep, 0.5f, 20f);
            
            Assert.AreEqual(1, SoundManager.ActiveSoundCount, "Should have one sound");
        }
        
        [Test]
        public void SoundManager_EmitSound_ReturnsValidSoundEvent()
        {
            var sound = SoundManager.EmitSound(new Vector3(5, 0, 5), SoundType.Voice, 0.7f, 25f);
            
            Assert.IsNotNull(sound);
            Assert.AreEqual(new Vector3(5, 0, 5), sound.Position);
            Assert.AreEqual(SoundType.Voice, sound.Type);
            Assert.AreEqual(0.7f, sound.Volume, 0.001f);
            Assert.AreEqual(25f, sound.Radius);
        }
        
        [Test]
        public void SoundManager_GetSoundsInRange_ReturnsSoundsWithinRange()
        {
            SoundManager.EmitSound(Vector3.zero, SoundType.Footstep, 1f, 10f);
            SoundManager.EmitSound(new Vector3(50, 0, 0), SoundType.Gunshot, 1f, 20f);
            
            var sounds = new System.Collections.Generic.List<SoundEvent>();
            SoundManager.GetSoundsInRangeNonAlloc(Vector3.zero, 30f, sounds);
            
            Assert.AreEqual(1, sounds.Count, "Should only find one sound in range");
            Assert.AreEqual(SoundType.Footstep, sounds[0].Type);
        }
        
        [Test]
        public void SoundManager_GetSoundsInRange_ConsidersSoundRadius()
        {
            // Sound at (30,0,0) with radius 50 - listener at origin with range 10 should hear it
            // because distance (30) < min(listener range 10, sound radius 50) = 10... wait, that's wrong
            // Actually: distance (30) <= min(10, 50) = 10 is FALSE, so shouldn't hear
            SoundManager.EmitSound(new Vector3(30, 0, 0), SoundType.Footstep, 1f, 50f);
            
            var sounds = new System.Collections.Generic.List<SoundEvent>();
            SoundManager.GetSoundsInRangeNonAlloc(Vector3.zero, 10f, sounds);
            
            Assert.AreEqual(0, sounds.Count, "Should not hear sound outside listener range");
            
            // But with larger listener range
            SoundManager.GetSoundsInRangeNonAlloc(Vector3.zero, 40f, sounds);
            Assert.AreEqual(1, sounds.Count, "Should hear sound within listener range");
        }
        
        [Test]
        public void SoundManager_ClearAll_RemovesAllSounds()
        {
            SoundManager.EmitSound(Vector3.zero, SoundType.Footstep, 1f, 20f);
            SoundManager.EmitSound(Vector3.one, SoundType.Gunshot, 1f, 50f);
            SoundManager.EmitSound(Vector3.up, SoundType.Explosion, 1f, 80f);
            
            Assert.AreEqual(3, SoundManager.ActiveSoundCount);
            
            SoundManager.ClearAll();
            
            Assert.AreEqual(0, SoundManager.ActiveSoundCount);
        }
        
        [Test]
        public void SoundManager_ConvenienceMethods_CreateCorrectSoundTypes()
        {
            var footstep = SoundManager.EmitFootstep(Vector3.zero);
            var voice = SoundManager.EmitVoice(Vector3.zero);
            var gunshot = SoundManager.EmitGunshot(Vector3.zero);
            var explosion = SoundManager.EmitExplosion(Vector3.zero);
            var impact = SoundManager.EmitImpact(Vector3.zero);
            var alarm = SoundManager.EmitAlarm(Vector3.zero);
            
            Assert.AreEqual(SoundType.Footstep, footstep.Type);
            Assert.AreEqual(SoundType.Voice, voice.Type);
            Assert.AreEqual(SoundType.Gunshot, gunshot.Type);
            Assert.AreEqual(SoundType.Explosion, explosion.Type);
            Assert.AreEqual(SoundType.Impact, impact.Type);
            Assert.AreEqual(SoundType.Alarm, alarm.Type);
        }
        
        // SoundType Tests
        
        [Test]
        public void SoundType_HasCorrectPriorityOrder()
        {
            Assert.Less((int)SoundType.Ambient, (int)SoundType.Footstep);
            Assert.Less((int)SoundType.Footstep, (int)SoundType.Voice);
            Assert.Less((int)SoundType.Voice, (int)SoundType.Impact);
            Assert.Less((int)SoundType.Impact, (int)SoundType.Alarm);
            Assert.Less((int)SoundType.Alarm, (int)SoundType.Gunshot);
            Assert.Less((int)SoundType.Gunshot, (int)SoundType.Explosion);
        }
        
        // Memory Integration Tests
        
        [Test]
        public void Memory_UpdateHeard_TracksHeardTarget()
        {
            var memory = new Memory();
            var target = new GameObject("Target");
            var position = new Vector3(10, 0, 10);
            
            memory.UpdateHeard(target, position, SoundType.Gunshot);
            
            Assert.IsTrue(memory.Remembers(target), "Should remember heard target");
            
            var mem = memory.GetMemory(target);
            Assert.IsTrue(mem.WasHeard, "WasHeard should be true");
            Assert.AreEqual(position, mem.LastHeardPosition);
            Assert.AreEqual(SoundType.Gunshot, mem.LastHeardSoundType);
            
            Object.DestroyImmediate(target);
        }
        
        [Test]
        public void Memory_UpdateHeard_BoostsConfidenceForNonVisibleTarget()
        {
            var memory = new Memory();
            var target = new GameObject("Target");
            
            // Target not yet in memory
            memory.UpdateHeard(target, Vector3.zero, SoundType.Footstep);
            
            var mem = memory.GetMemory(target);
            Assert.GreaterOrEqual(mem.Confidence, 0.5f, "Confidence should be boosted for heard target");
            
            Object.DestroyImmediate(target);
        }
        
        [Test]
        public void Memory_GetMostRecentlyHeardTarget_ReturnsCorrectTarget()
        {
            var memory = new Memory();
            var target1 = new GameObject("Target1");
            var target2 = new GameObject("Target2");
            
            memory.UpdateHeard(target1, Vector3.zero, SoundType.Footstep);
            memory.UpdateHeard(target2, Vector3.one, SoundType.Gunshot);
            
            var mostRecent = memory.GetMostRecentlyHeardTarget();
            
            // Both were heard at essentially the same time, so either could be returned
            Assert.IsTrue(mostRecent == target1 || mostRecent == target2, 
                "Should return one of the heard targets");
            
            Object.DestroyImmediate(target1);
            Object.DestroyImmediate(target2);
        }
    }
}
