using UnityEngine;

namespace NPCBrain.Perception
{
    /// <summary>
    /// Represents a sound event that can be detected by HearingSensors.
    /// </summary>
    public class SoundEvent
    {
        /// <summary>World position where the sound originated.</summary>
        public Vector3 Position { get; set; }
        
        /// <summary>Base volume of the sound (0-1).</summary>
        public float Volume { get; set; }
        
        /// <summary>Maximum distance at which this sound can be heard.</summary>
        public float Radius { get; set; }
        
        /// <summary>Category of the sound for filtering and priority.</summary>
        public SoundType Type { get; set; }
        
        /// <summary>Optional custom tag for filtering specific sounds.</summary>
        public string CustomTag { get; set; }
        
        /// <summary>The GameObject that emitted this sound (may be null).</summary>
        public GameObject Source { get; set; }
        
        /// <summary>Time.time when this sound was emitted.</summary>
        public float Timestamp { get; set; }
        
        /// <summary>Calculated effective volume after distance attenuation.</summary>
        public float EffectiveVolume { get; set; }
        
        /// <summary>Calculated priority score for this sound.</summary>
        public float Priority { get; set; }
        
        /// <summary>Time in seconds since this sound was emitted.</summary>
        public float Age => Time.time - Timestamp;
        
        /// <summary>
        /// Creates a new SoundEvent with the specified parameters.
        /// </summary>
        public SoundEvent(Vector3 position, SoundType type, float volume, float radius, GameObject source = null, string customTag = null)
        {
            Position = position;
            Type = type;
            Volume = Mathf.Clamp01(volume);
            Radius = Mathf.Max(0f, radius);
            Source = source;
            CustomTag = customTag;
            Timestamp = Time.time;
            EffectiveVolume = volume;
            Priority = 0f;
        }
        
        /// <summary>
        /// Calculates the effective volume at a given listener position.
        /// </summary>
        /// <param name="listenerPosition">Position of the listener.</param>
        /// <returns>Attenuated volume (0-1).</returns>
        public float GetVolumeAtPosition(Vector3 listenerPosition)
        {
            // Early-out using sqrMagnitude to avoid sqrt when out of range
            float radiusSqr = Radius * Radius;
            float distSqr = (Position - listenerPosition).sqrMagnitude;
            if (distSqr >= radiusSqr) return 0f;
            
            // Only calculate actual distance when in range
            float distance = Mathf.Sqrt(distSqr);
            
            // Inverse linear falloff
            float attenuation = 1f - (distance / Radius);
            return Volume * attenuation;
        }
        
        /// <summary>
        /// Calculates the priority of this sound for a listener.
        /// </summary>
        /// <param name="listenerPosition">Position of the listener.</param>
        /// <param name="hearingRange">Maximum hearing range of the listener.</param>
        /// <returns>Priority score (higher = more important).</returns>
        public float CalculatePriority(Vector3 listenerPosition, float hearingRange)
        {
            float effectiveRadius = Mathf.Min(Radius, hearingRange);
            
            // Use sqrMagnitude for early-out check
            float distSqr = (Position - listenerPosition).sqrMagnitude;
            float effectiveRadiusSqr = effectiveRadius * effectiveRadius;
            
            // If completely out of range, priority is effectively 0
            float distanceFactor;
            if (distSqr >= effectiveRadiusSqr)
            {
                distanceFactor = 0f;
            }
            else
            {
                // Only calculate sqrt when in range
                float distance = Mathf.Sqrt(distSqr);
                distanceFactor = 1f - (distance / effectiveRadius);
            }
            
            // Type factor (normalized 0-1 based on enum value)
            float typeFactor = (int)Type / (float)SoundType.Explosion;
            
            // Recency factor (sounds older than 2 seconds get deprioritized)
            float recencyFactor = Mathf.Clamp01(1f - Age / 2f);
            
            // Weighted combination
            Priority = (Volume * distanceFactor * 0.4f) +
                       (typeFactor * 0.35f) +
                       (recencyFactor * 0.15f) +
                       (distanceFactor * 0.1f);
            
            EffectiveVolume = GetVolumeAtPosition(listenerPosition);
            
            return Priority;
        }
    }
}
