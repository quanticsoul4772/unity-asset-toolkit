using UnityEngine;
using UnityEngine.InputSystem;

namespace NPCBrain.Demo
{
    /// <summary>
    /// Simple WASD player controller for demo scenes.
    /// The player can be detected by GuardNPC's SightSensor.
    /// Uses the new Input System package.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _sprintMultiplier = 1.5f;
        [SerializeField] private float _rotationSpeed = 720f;
        
        [Header("Visual")]
        [SerializeField] private Color _playerColor = new Color(0.2f, 0.8f, 0.2f);
        
        private CharacterController _controller;
        private Vector3 _velocity;
        
        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            
            // Set player tag for SightSensor detection
            gameObject.tag = "Player";
            
            // Apply color to renderer if present
            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = _playerColor;
            }
        }
        
        private void Update()
        {
            HandleMovement();
        }
        
        private void HandleMovement()
        {
            // Get input using new Input System
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            
            float horizontal = 0f;
            float vertical = 0f;
            
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) vertical += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) vertical -= 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) horizontal -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) horizontal += 1f;
            
            // Calculate movement direction (relative to world, top-down style)
            Vector3 moveDirection = new Vector3(horizontal, 0f, vertical).normalized;
            
            // Apply sprint
            float speed = _moveSpeed;
            if (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed)
            {
                speed *= _sprintMultiplier;
            }
            
            // Move
            if (moveDirection.magnitude > 0.1f)
            {
                // Rotate to face movement direction
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, 
                    targetRotation, 
                    _rotationSpeed * Time.deltaTime
                );
                
                // Move in facing direction
                _controller.Move(moveDirection * speed * Time.deltaTime);
            }
            
            // Apply gravity
            if (!_controller.isGrounded)
            {
                _velocity.y += Physics.gravity.y * Time.deltaTime;
            }
            else
            {
                _velocity.y = -2f; // Small downward force to keep grounded
            }
            
            _controller.Move(_velocity * Time.deltaTime);
        }
        
        /// <summary>
        /// Creates a player GameObject with this controller.
        /// </summary>
        public static GameObject CreatePlayer(Vector3 position)
        {
            var playerObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            playerObj.name = "Player";
            
            // Set Player tag - handle case where tag doesn't exist
            try
            {
                playerObj.tag = "Player";
            }
            catch (UnityException)
            {
                // Player tag not defined in Tag Manager - log warning but continue
                Debug.LogWarning("Player tag not defined in Tag Manager. Guards may not detect the player. " +
                    "Add 'Player' tag in Edit > Project Settings > Tags and Layers.");
            }
            
            playerObj.transform.position = position;
            
            // Keep the CapsuleCollider for SightSensor detection (Physics.OverlapSphere)
            // CharacterController has its own internal capsule for movement collision,
            // so this external collider is used for physics queries only
            // Note: Do NOT set isTrigger=true or OverlapSphere won't detect it!
            
            // Add CharacterController for movement
            var controller = playerObj.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.5f;
            controller.center = new Vector3(0f, 1f, 0f);
            
            // Add player controller
            var player = playerObj.AddComponent<PlayerController>();
            
            // Set color
            playerObj.GetComponent<Renderer>().material.color = new Color(0.2f, 0.8f, 0.2f);
            
            // Add a small indicator on top
            var indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            indicator.name = "PlayerIndicator";
            indicator.transform.SetParent(playerObj.transform);
            indicator.transform.localPosition = new Vector3(0f, 1.3f, 0f);
            indicator.transform.localScale = Vector3.one * 0.3f;
            indicator.GetComponent<Renderer>().material.color = Color.yellow;
            Object.Destroy(indicator.GetComponent<Collider>());
            
            return playerObj;
        }
    }
}
