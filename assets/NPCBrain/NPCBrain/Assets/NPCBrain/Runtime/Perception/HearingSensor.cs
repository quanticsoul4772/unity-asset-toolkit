using System;
using System.Collections.Generic;
using UnityEngine;

namespace NPCBrain.Perception
{
    /// <summary>
    /// Hearing sensor that detects sounds emitted by SoundEmitter components or SoundManager.
    /// Integrates with NPCBrainController to fire events and update memory.
    /// </summary>
    public class HearingSensor : MonoBehaviour
    {
        [Header("Hearing Settings")]
        [Tooltip("Maximum distance at which sounds can be heard")]
        [SerializeField] private float _hearingRange = 30f;
        
        [Tooltip("Minimum effective volume to register a sound (0-1)")]
        [Range(0f, 1f)]
        [SerializeField] private float _hearingThreshold = 0.1f;
        
        [Tooltip("Height offset for hearing position (ear level)")]
        [SerializeField] private float _earHeight = 1.5f;
        
        [Header("Filtering")]
        [Tooltip("Minimum sound type priority to detect")]
        [SerializeField] private SoundType _minimumPriority = SoundType.Ambient;
        
        [Tooltip("Custom tags to ignore (leave empty to hear all)")]
        [SerializeField] private string[] _ignoreTags;
        
        [Tooltip("Ignore sounds emitted by this GameObject")]
        [SerializeField] private bool _ignoreOwnSounds = true;
        
        [Header("Occlusion")]
        [Tooltip("Check for obstacles blocking sound")]
        [SerializeField] private bool _checkOcclusion = false;
        
        [Tooltip("Layers that block sound")]
        [SerializeField] private LayerMask _occlusionMask = ~0;
        
        [Tooltip("Volume multiplier when sound passes through obstacles")]
        [Range(0f, 1f)]
        [SerializeField] private float _occlusionDamping = 0.3f;
        
        [Header("Memory")]
        [Tooltip("How long to remember heard sounds")]
        [SerializeField] private float _soundMemoryDuration = 3f;
        
        [Tooltip("Maximum number of sounds to remember")]
        [SerializeField] private int _maxRememberedSounds = 10;
        
        [Header("Performance")]
        [Tooltip("Maximum occlusion raycasts per tick")]
        [SerializeField] private int _maxRaycastsPerTick = 5;
        
        [Header("Debug")]
        [Tooltip("Enable debug logging for this specific sensor (also requires NPCBrainDebug.Enabled)")]
        [SerializeField] private bool _debugLogging = false;
        [Tooltip("Force debug logging even if global debug is disabled")]
        [SerializeField] private bool _forceDebugLogging = false;
        [SerializeField] private bool _drawGizmos = true;
        [SerializeField] private Color _gizmoColor = new Color(0.5f, 0.8f, 1f, 0.2f);
        [SerializeField] private Color _gizmoColorHeard = new Color(1f, 0.5f, 0.2f, 0.3f);
        
        private readonly List<SoundEvent> _heardSounds = new List<SoundEvent>();
        private readonly List<SoundEvent> _previousSounds = new List<SoundEvent>();
        private readonly List<SoundEvent> _soundsInRange = new List<SoundEvent>();
        private readonly HashSet<string> _ignoreTagSet = new HashSet<string>();
        
        /// <summary>Maximum hearing range in units.</summary>
        public float HearingRange => _hearingRange;
        
        /// <summary>Minimum volume threshold to hear sounds.</summary>
        public float HearingThreshold => _hearingThreshold;
        
        /// <summary>List of sounds heard in the last tick.</summary>
        public IReadOnlyList<SoundEvent> HeardSounds => _heardSounds;
        
        /// <summary>True if any sounds were heard in the last tick.</summary>
        public bool HasHeardSounds => _heardSounds.Count > 0;
        
        /// <summary>The highest priority sound heard, or null if none.</summary>
        public SoundEvent HighestPrioritySound { get; private set; }
        
        /// <summary>The most recent sound heard, or null if none.</summary>
        public SoundEvent MostRecentSound => _heardSounds.Count > 0 ? _heardSounds[0] : null;
        
        /// <summary>Number of sounds currently heard.</summary>
        public int HeardSoundCount => _heardSounds.Count;
        
        /// <summary>How long sounds are remembered for filtering purposes.</summary>
        public float SoundMemoryDuration => _soundMemoryDuration;
        
        private void Awake()
        {
            // Build ignore tag set for fast lookup
            _ignoreTagSet.Clear();
            if (_ignoreTags != null)
            {
                foreach (var tag in _ignoreTags)
                {
                    if (!string.IsNullOrEmpty(tag))
                    {
                        _ignoreTagSet.Add(tag);
                    }
                }
            }
        }
        
        /// <summary>
        /// Updates the sensor, detecting sounds in range.
        /// Called automatically by NPCBrainController each tick.
        /// </summary>
        /// <param name="brain">The brain controller this sensor belongs to.</param>
        public void Tick(NPCBrainController brain)
        {
            // Store previous sounds for comparison
            _previousSounds.Clear();
            _previousSounds.AddRange(_heardSounds);
            _heardSounds.Clear();
            HighestPrioritySound = null;
            
            Vector3 earPosition = transform.position + Vector3.up * _earHeight;
            
            // Get all sounds in range
            SoundManager.GetSoundsInRangeNonAlloc(earPosition, _hearingRange, _soundsInRange);
            
            if (ShouldLog() && _soundsInRange.Count > 0)
            {
                NPCBrainDebug.Log(NPCBrainDebug.Category.Hearing, 
                    $"Found {_soundsInRange.Count} sounds in range", this);
            }
            
            int raycastCount = 0;
            float highestPriority = float.MinValue;
            
            foreach (var sound in _soundsInRange)
            {
                // Skip sounds below minimum priority type
                if (sound.Type < _minimumPriority)
                {
                    if (ShouldLog())
                    {
                        NPCBrainDebug.Log(NPCBrainDebug.Category.Hearing, 
                            $"Skipping {sound.Type} - below minimum priority {_minimumPriority}", this);
                    }
                    continue;
                }
                
                // Skip own sounds
                if (_ignoreOwnSounds && sound.Source == gameObject)
                {
                    continue;
                }
                
                // Skip ignored tags
                if (!string.IsNullOrEmpty(sound.CustomTag) && _ignoreTagSet.Contains(sound.CustomTag))
                {
                    continue;
                }
                
                // Calculate effective volume
                float effectiveVolume = sound.GetVolumeAtPosition(earPosition);
                
                // Apply occlusion if enabled
                if (_checkOcclusion && raycastCount < _maxRaycastsPerTick)
                {
                    raycastCount++;
                    Vector3 direction = (sound.Position - earPosition).normalized;
                    float distance = Vector3.Distance(earPosition, sound.Position);
                    
                    if (Physics.Raycast(earPosition, direction, distance, _occlusionMask))
                    {
                        effectiveVolume *= _occlusionDamping;
                        if (ShouldLog())
                        {
                            NPCBrainDebug.Log(NPCBrainDebug.Category.Hearing, 
                                $"Sound occluded, volume reduced to {effectiveVolume:F2}", this);
                        }
                    }
                }
                
                // Skip if below threshold
                if (effectiveVolume < _hearingThreshold)
                {
                    if (ShouldLog())
                    {
                        NPCBrainDebug.Log(NPCBrainDebug.Category.Hearing, 
                            $"Sound too quiet ({effectiveVolume:F2} < {_hearingThreshold})", this);
                    }
                    continue;
                }
                
                // Calculate priority
                sound.EffectiveVolume = effectiveVolume;
                sound.CalculatePriority(earPosition, _hearingRange);
                
                // Add to heard sounds (respect max limit)
                if (_heardSounds.Count < _maxRememberedSounds)
                {
                    _heardSounds.Add(sound);
                    
                    if (sound.Priority > highestPriority)
                    {
                        highestPriority = sound.Priority;
                        HighestPrioritySound = sound;
                    }
                    
                    if (ShouldLog())
                    {
                        NPCBrainDebug.Log(NPCBrainDebug.Category.Hearing, 
                            $"<color=cyan>SOUND HEARD: {sound.Type} at {sound.Position}, volume={effectiveVolume:F2}, priority={sound.Priority:F2}</color>", this);
                    }
                }
            }
            
            // Sort by priority (highest first)
            _heardSounds.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            
            // Fire events for newly heard sounds
            if (brain != null)
            {
                foreach (var sound in _heardSounds)
                {
                    bool isNew = true;
                    foreach (var prev in _previousSounds)
                    {
                        if (prev == sound)
                        {
                            isNew = false;
                            break;
                        }
                    }
                    
                    if (isNew)
                    {
                        brain.RaiseSoundHeard(sound);
                    }
                }
            }
        }
        
        /// <summary>
        /// Checks if a specific sound type was heard in the last tick.
        /// </summary>
        /// <param name="type">The sound type to check for.</param>
        /// <returns>True if a sound of that type was heard.</returns>
        public bool HeardSoundType(SoundType type)
        {
            foreach (var sound in _heardSounds)
            {
                if (sound.Type == type)
                {
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// Checks if a sound with a specific tag was heard.
        /// </summary>
        /// <param name="tag">The custom tag to check for.</param>
        /// <returns>True if a sound with that tag was heard.</returns>
        public bool HeardSoundWithTag(string tag)
        {
            foreach (var sound in _heardSounds)
            {
                if (sound.CustomTag == tag)
                {
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// Checks if a sound from a specific source was heard.
        /// </summary>
        /// <param name="source">The source GameObject to check for.</param>
        /// <returns>True if a sound from that source was heard.</returns>
        public bool HeardSoundFromSource(GameObject source)
        {
            if (source == null) return false;
            
            foreach (var sound in _heardSounds)
            {
                if (sound.Source == source)
                {
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// Gets the direction to the highest priority sound.
        /// </summary>
        /// <returns>Normalized direction, or Vector3.zero if no sounds heard.</returns>
        public Vector3 GetDirectionToHighestPrioritySound()
        {
            if (HighestPrioritySound == null) return Vector3.zero;
            
            Vector3 earPosition = transform.position + Vector3.up * _earHeight;
            return (HighestPrioritySound.Position - earPosition).normalized;
        }
        
        /// <summary>
        /// Gets sounds of a specific type or higher priority.
        /// </summary>
        /// <param name="minimumType">Minimum sound type to include.</param>
        /// <param name="results">List to populate with results.</param>
        public void GetSoundsOfType(SoundType minimumType, List<SoundEvent> results)
        {
            results.Clear();
            foreach (var sound in _heardSounds)
            {
                if (sound.Type >= minimumType)
                {
                    results.Add(sound);
                }
            }
        }
        
        /// <summary>
        /// Gets sounds heard within the configured memory duration.
        /// </summary>
        /// <param name="results">List to populate with results.</param>
        public void GetRecentSounds(List<SoundEvent> results)
        {
            results.Clear();
            float cutoffTime = Time.time - _soundMemoryDuration;
            foreach (var sound in _heardSounds)
            {
                if (sound.Timestamp >= cutoffTime)
                {
                    results.Add(sound);
                }
            }
        }
        
        /// <summary>
        /// Checks if any sound was heard within the memory duration window.
        /// </summary>
        /// <returns>True if a sound was heard recently.</returns>
        public bool HasRecentSound()
        {
            if (_heardSounds.Count == 0) return false;
            
            float cutoffTime = Time.time - _soundMemoryDuration;
            foreach (var sound in _heardSounds)
            {
                if (sound.Timestamp >= cutoffTime)
                {
                    return true;
                }
            }
            return false;
        }
        
        private void OnDrawGizmosSelected()
        {
            if (!_drawGizmos) return;
            DrawHearingRange();
        }
        
        private void OnDrawGizmos()
        {
            if (!_drawGizmos || !Application.isPlaying) return;
            if (_heardSounds.Count > 0)
            {
                DrawHearingRange();
            }
        }
        
        private void DrawHearingRange()
        {
            Vector3 earPosition = transform.position + Vector3.up * _earHeight;
            bool hasHeard = Application.isPlaying && _heardSounds.Count > 0;
            
            // Draw hearing radius
            Gizmos.color = hasHeard ? _gizmoColorHeard : _gizmoColor;
            
            // Draw horizontal circle at ear height
            DrawCircle(earPosition, _hearingRange, 32);
            
            // Draw vertical reference lines
            Gizmos.DrawLine(earPosition + Vector3.forward * _hearingRange, earPosition + Vector3.back * _hearingRange);
            Gizmos.DrawLine(earPosition + Vector3.left * _hearingRange, earPosition + Vector3.right * _hearingRange);
            
            // Draw lines to heard sounds
            if (Application.isPlaying)
            {
                Gizmos.color = Color.yellow;
                foreach (var sound in _heardSounds)
                {
                    Gizmos.DrawLine(earPosition, sound.Position);
                    Gizmos.DrawWireSphere(sound.Position, 0.5f);
                }
                
                // Highlight highest priority sound
                if (HighestPrioritySound != null)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireSphere(HighestPrioritySound.Position, 0.8f);
                }
            }
        }
        
        private bool ShouldLog()
        {
            return _forceDebugLogging || (_debugLogging && NPCBrainDebug.IsEnabled(NPCBrainDebug.Category.Hearing));
        }
        
        private void DrawCircle(Vector3 center, float radius, int segments)
        {
            float angleStep = 360f / segments;
            Vector3 prevPoint = center + Vector3.forward * radius;
            
            for (int i = 1; i <= segments; i++)
            {
                float angle = angleStep * i;
                Vector3 point = center + Quaternion.Euler(0, angle, 0) * Vector3.forward * radius;
                Gizmos.DrawLine(prevPoint, point);
                prevPoint = point;
            }
        }
    }
}
