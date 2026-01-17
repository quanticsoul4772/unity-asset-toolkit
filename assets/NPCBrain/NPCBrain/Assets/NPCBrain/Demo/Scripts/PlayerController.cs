using UnityEngine;

namespace NPCBrain.Demo
{
    /// <summary>
    /// Simple WASD player controller for demo scenes.
    /// The player can be detected by GuardNPC's SightSensor.
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
            // Get input
            float horizontal = 0f;
            float vertical = 0f;
            
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) vertical += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) vertical -= 1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) horizontal -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) horizontal += 1f;
            
            // Calculate movement direction (relative to world, top-down style)
            Vector3 moveDirection = new Vector3(horizontal, 0f, vertical).normalized;
            
            // Apply sprint
            float speed = _moveSpeed;
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
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
            playerObj.tag = "Player";
            playerObj.transform.position = position;
            
            // Remove default collider and add CharacterController
            Object.Destroy(playerObj.GetComponent<CapsuleCollider>());
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
