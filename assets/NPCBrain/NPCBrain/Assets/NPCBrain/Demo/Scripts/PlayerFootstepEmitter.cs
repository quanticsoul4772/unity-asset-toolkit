using UnityEngine;
using NPCBrain.Perception;
using UnityEngine.InputSystem;

namespace NPCBrain.Demo
{
    /// <summary>
    /// Emits footstep sounds when the player moves.
    /// Attach to the player GameObject alongside PlayerController.
    /// </summary>
    public class PlayerFootstepEmitter : MonoBehaviour
    {
        [Header("Footstep Settings")]
        [SerializeField] private float _footstepInterval = 0.4f;
        [SerializeField] private float _sprintFootstepInterval = 0.25f;
        [SerializeField] private float _walkVolume = 0.3f;
        [SerializeField] private float _sprintVolume = 0.6f;
        [SerializeField] private float _footstepRadius = 15f;
        [SerializeField] private float _sprintRadius = 25f;
        
        [Header("Debug")]
        [SerializeField] private bool _showDebug = true;
        
        private float _lastFootstepTime;
        private Vector3 _lastPosition;
        private bool _isMoving;
        private bool _isSprinting;
        
        /// <summary>The most recent footstep sound emitted.</summary>
        public SoundEvent LastFootstep { get; private set; }
        
        /// <summary>True if the player is currently moving.</summary>
        public bool IsMoving => _isMoving;
        
        /// <summary>True if the player is sprinting.</summary>
        public bool IsSprinting => _isSprinting;
        
        private void Start()
        {
            _lastPosition = transform.position;
        }
        
        private void Update()
        {
            CheckMovement();
            
            if (_isMoving)
            {
                EmitFootstepIfNeeded();
            }
        }
        
        private void CheckMovement()
        {
            Vector3 currentPos = transform.position;
            float movement = Vector3.Distance(currentPos, _lastPosition);
            _isMoving = movement > 0.01f;
            
            // Check sprint
            var keyboard = Keyboard.current;
            _isSprinting = keyboard != null && 
                (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
            
            _lastPosition = currentPos;
        }
        
        private void EmitFootstepIfNeeded()
        {
            float interval = _isSprinting ? _sprintFootstepInterval : _footstepInterval;
            
            if (Time.time - _lastFootstepTime >= interval)
            {
                EmitFootstep();
            }
        }
        
        /// <summary>
        /// Manually emit a footstep sound.
        /// </summary>
        public void EmitFootstep()
        {
            float volume = _isSprinting ? _sprintVolume : _walkVolume;
            float radius = _isSprinting ? _sprintRadius : _footstepRadius;
            
            LastFootstep = SoundManager.EmitSound(
                transform.position,
                SoundType.Footstep,
                volume,
                radius,
                gameObject
            );
            
            _lastFootstepTime = Time.time;
            
            if (_showDebug)
            {
                string sprintText = _isSprinting ? " (SPRINT)" : "";
                Debug.Log($"<color=yellow>[Footstep]{sprintText} at {transform.position}, volume={volume:F2}, radius={radius}</color>");
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            // Draw footstep radius
            Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, _footstepRadius);
            
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, _sprintRadius);
        }
    }
}
