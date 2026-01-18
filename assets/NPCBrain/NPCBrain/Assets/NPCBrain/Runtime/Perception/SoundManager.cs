using System.Collections.Generic;
using UnityEngine;

namespace NPCBrain.Perception
{
    /// <summary>
    /// Static manager that tracks active sounds for HearingSensor components to query.
    /// Sounds are automatically cleaned up after they expire.
    /// </summary>
    public static class SoundManager
    {
        private static readonly List<SoundEvent> _activeSounds = new List<SoundEvent>();
        private static readonly List<SoundEvent> _soundsToRemove = new List<SoundEvent>();
        
        /// <summary>How long sounds remain active before being cleaned up (seconds).</summary>
        public static float MaxSoundAge { get; set; } = 1f;
        
        /// <summary>Number of currently active sounds.</summary>
        public static int ActiveSoundCount => _activeSounds.Count;
        
        /// <summary>
        /// Registers a new sound event that can be detected by HearingSensors.
        /// </summary>
        /// <param name="sound">The sound event to register.</param>
        public static void RegisterSound(SoundEvent sound)
        {
            if (sound == null) return;
            sound.Timestamp = Time.time;
            _activeSounds.Add(sound);
        }
        
        /// <summary>
        /// Creates and registers a sound event.
        /// </summary>
        /// <param name="position">Position of the sound.</param>
        /// <param name="type">Type/category of sound.</param>
        /// <param name="volume">Volume (0-1).</param>
        /// <param name="radius">Maximum audible distance.</param>
        /// <param name="source">Optional source GameObject.</param>
        /// <param name="customTag">Optional custom tag.</param>
        /// <returns>The created SoundEvent.</returns>
        public static SoundEvent EmitSound(Vector3 position, SoundType type, float volume, float radius, GameObject source = null, string customTag = null)
        {
            var sound = new SoundEvent(position, type, volume, radius, source, customTag);
            RegisterSound(sound);
            return sound;
        }
        
        /// <summary>
        /// Gets all sounds within range of a position.
        /// </summary>
        /// <param name="position">Listener position.</param>
        /// <param name="range">Maximum hearing range.</param>
        /// <returns>Enumerable of sounds within range.</returns>
        public static IEnumerable<SoundEvent> GetSoundsInRange(Vector3 position, float range)
        {
            CleanupOldSounds();
            
            foreach (var sound in _activeSounds)
            {
                float distance = Vector3.Distance(position, sound.Position);
                float effectiveRange = Mathf.Min(range, sound.Radius);
                
                if (distance <= effectiveRange)
                {
                    yield return sound;
                }
            }
        }
        
        /// <summary>
        /// Gets all sounds within range of a position as a list.
        /// </summary>
        /// <param name="position">Listener position.</param>
        /// <param name="range">Maximum hearing range.</param>
        /// <param name="results">List to populate with results.</param>
        public static void GetSoundsInRangeNonAlloc(Vector3 position, float range, List<SoundEvent> results)
        {
            results.Clear();
            CleanupOldSounds();
            
            foreach (var sound in _activeSounds)
            {
                float distance = Vector3.Distance(position, sound.Position);
                float effectiveRange = Mathf.Min(range, sound.Radius);
                
                if (distance <= effectiveRange)
                {
                    results.Add(sound);
                }
            }
        }
        
        /// <summary>
        /// Removes sounds that have exceeded MaxSoundAge.
        /// </summary>
        public static void CleanupOldSounds()
        {
            _soundsToRemove.Clear();
            
            foreach (var sound in _activeSounds)
            {
                if (sound.Age > MaxSoundAge)
                {
                    _soundsToRemove.Add(sound);
                }
            }
            
            foreach (var sound in _soundsToRemove)
            {
                _activeSounds.Remove(sound);
            }
        }
        
        /// <summary>
        /// Clears all active sounds. Useful for scene transitions.
        /// </summary>
        public static void ClearAll()
        {
            _activeSounds.Clear();
        }
        
        // Convenience methods for common sound types
        
        /// <summary>Emits a footstep sound.</summary>
        public static SoundEvent EmitFootstep(Vector3 position, float volume = 0.3f, GameObject source = null)
        {
            return EmitSound(position, SoundType.Footstep, volume, 15f, source);
        }
        
        /// <summary>Emits a voice/speech sound.</summary>
        public static SoundEvent EmitVoice(Vector3 position, float volume = 0.5f, GameObject source = null)
        {
            return EmitSound(position, SoundType.Voice, volume, 20f, source);
        }
        
        /// <summary>Emits a gunshot sound.</summary>
        public static SoundEvent EmitGunshot(Vector3 position, float volume = 1f, GameObject source = null)
        {
            return EmitSound(position, SoundType.Gunshot, volume, 50f, source);
        }
        
        /// <summary>Emits an explosion sound.</summary>
        public static SoundEvent EmitExplosion(Vector3 position, float volume = 1f, GameObject source = null)
        {
            return EmitSound(position, SoundType.Explosion, volume, 80f, source);
        }
        
        /// <summary>Emits an impact sound (door, object falling).</summary>
        public static SoundEvent EmitImpact(Vector3 position, float volume = 0.6f, GameObject source = null)
        {
            return EmitSound(position, SoundType.Impact, volume, 25f, source);
        }
        
        /// <summary>Emits an alarm sound.</summary>
        public static SoundEvent EmitAlarm(Vector3 position, float volume = 0.8f, GameObject source = null)
        {
            return EmitSound(position, SoundType.Alarm, volume, 40f, source);
        }
    }
}
