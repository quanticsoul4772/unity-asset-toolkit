using UnityEngine;

namespace NPCBrain.Perception
{
    /// <summary>
    /// Component that emits sounds that can be detected by HearingSensor components.
    /// Can emit sounds manually, continuously, or on enable.
    /// </summary>
    public class SoundEmitter : MonoBehaviour
    {
        /// <summary>
        /// How the emitter triggers sounds.
        /// </summary>
        public enum EmissionMode
        {
            /// <summary>Call EmitSound() manually.</summary>
            Manual,
            
            /// <summary>Emit at regular intervals while enabled.</summary>
            Continuous,
            
            /// <summary>Emit once when the component is enabled.</summary>
            OnEnable
        }
        
        [Header("Sound Properties")]
        [Tooltip("Category of sound for filtering and priority")]
        [SerializeField] private SoundType _soundType = SoundType.Footstep;
        
        [Tooltip("Base volume of the sound (0-1)")]
        [Range(0f, 1f)]
        [SerializeField] private float _volume = 0.5f;
        
        [Tooltip("Maximum distance at which this sound can be heard")]
        [SerializeField] private float _radius = 20f;
        
        [Tooltip("Optional custom tag for filtering")]
        [SerializeField] private string _customTag;
        
        [Header("Emission Mode")]
        [Tooltip("How the emitter triggers sounds")]
        [SerializeField] private EmissionMode _mode = EmissionMode.Manual;
        
        [Tooltip("Interval between sounds when in Continuous mode")]
        [SerializeField] private float _continuousInterval = 0.5f;
        
        [Header("Debug")]
        [SerializeField] private bool _drawGizmos = true;
        [SerializeField] private Color _gizmoColor = new Color(0.5f, 0.8f, 1f, 0.3f);
        
        private float _lastEmitTime;
        
        /// <summary>The type of sound this emitter produces.</summary>
        public SoundType SoundType => _soundType;
        
        /// <summary>Base volume of emitted sounds.</summary>
        public float Volume => _volume;
        
        /// <summary>Maximum audible distance.</summary>
        public float Radius => _radius;
        
        /// <summary>Custom tag for filtering.</summary>
        public string CustomTag => _customTag;
        
        /// <summary>The most recently emitted sound event.</summary>
        public SoundEvent LastEmittedSound { get; private set; }
        
        private void OnEnable()
        {
            if (_mode == EmissionMode.OnEnable)
            {
                EmitSound();
            }
        }
        
        private void Update()
        {
            if (_mode == EmissionMode.Continuous)
            {
                if (Time.time - _lastEmitTime >= _continuousInterval)
                {
                    EmitSound();
                }
            }
        }
        
        /// <summary>
        /// Emits a sound at this emitter's position with default settings.
        /// </summary>
        /// <returns>The emitted SoundEvent.</returns>
        public SoundEvent EmitSound()
        {
            return EmitSound(_volume);
        }
        
        /// <summary>
        /// Emits a sound with a volume multiplier.
        /// </summary>
        /// <param name="volumeMultiplier">Multiplier for base volume.</param>
        /// <returns>The emitted SoundEvent.</returns>
        public SoundEvent EmitSound(float volumeMultiplier)
        {
            _lastEmitTime = Time.time;
            
            var sound = new SoundEvent(
                transform.position,
                _soundType,
                _volume * volumeMultiplier,
                _radius,
                gameObject,
                _customTag
            );
            
            SoundManager.RegisterSound(sound);
            LastEmittedSound = sound;
            
            return sound;
        }
        
        /// <summary>
        /// Emits a sound with custom parameters, overriding emitter settings.
        /// </summary>
        /// <param name="type">Sound type.</param>
        /// <param name="volume">Volume (0-1).</param>
        /// <param name="radius">Audible radius.</param>
        /// <returns>The emitted SoundEvent.</returns>
        public SoundEvent EmitSound(SoundType type, float volume, float radius)
        {
            _lastEmitTime = Time.time;
            
            var sound = new SoundEvent(
                transform.position,
                type,
                volume,
                radius,
                gameObject,
                _customTag
            );
            
            SoundManager.RegisterSound(sound);
            LastEmittedSound = sound;
            
            return sound;
        }
        
        // Static convenience methods for emitting sounds without a component
        
        /// <summary>
        /// Emits a sound at a position without requiring a SoundEmitter component.
        /// </summary>
        public static SoundEvent EmitAt(Vector3 position, SoundType type, float volume, float radius, GameObject source = null)
        {
            return SoundManager.EmitSound(position, type, volume, radius, source);
        }
        
        /// <summary>Emits a footstep sound at a position.</summary>
        public static SoundEvent EmitFootstepAt(Vector3 position, float volume = 0.3f, GameObject source = null)
        {
            return SoundManager.EmitFootstep(position, volume, source);
        }
        
        /// <summary>Emits a gunshot sound at a position.</summary>
        public static SoundEvent EmitGunshotAt(Vector3 position, float volume = 1f, GameObject source = null)
        {
            return SoundManager.EmitGunshot(position, volume, source);
        }
        
        /// <summary>Emits an explosion sound at a position.</summary>
        public static SoundEvent EmitExplosionAt(Vector3 position, float volume = 1f, GameObject source = null)
        {
            return SoundManager.EmitExplosion(position, volume, source);
        }
        
        private void OnDrawGizmosSelected()
        {
            if (!_drawGizmos) return;
            DrawRadiusGizmo();
        }
        
        private void DrawRadiusGizmo()
        {
            Gizmos.color = _gizmoColor;
            Gizmos.DrawWireSphere(transform.position, _radius);
            
            // Draw filled disc at ground level
            Gizmos.color = new Color(_gizmoColor.r, _gizmoColor.g, _gizmoColor.b, 0.1f);
            
            // Draw concentric circles to show falloff
            int rings = 4;
            for (int i = 1; i <= rings; i++)
            {
                float ringRadius = _radius * i / rings;
                DrawCircle(transform.position, ringRadius, 32);
            }
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
