using UnityEngine;

namespace SwarmAI.Demo
{
    /// <summary>
    /// Simple camera controller for SwarmAI demo scenes.
    /// Supports panning, zooming, and orbiting.
    /// Uses the new Unity Input System.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float _panSpeed = 20f;
        [SerializeField] private float _zoomSpeed = 10f;
        [SerializeField] private float _rotationSpeed = 100f;
        [SerializeField] private float _scrollNormalization = 120f;
        
        [Header("Limits")]
        [SerializeField] private float _minHeight = 5f;
        [SerializeField] private float _maxHeight = 50f;
        [SerializeField] private float _minPitch = 20f;
        [SerializeField] private float _maxPitch = 80f;
        
        [Header("Focus")]
        [SerializeField] private Transform _focusTarget;
        [SerializeField] private bool _autoFocusOnFlock = false;
        
        private float _currentYaw;
        private float _currentPitch;
        
        private void Start()
        {
            // Initialize input system
            SwarmDemoInput.Initialize();
            
            // Initialize from current rotation
            Vector3 euler = transform.eulerAngles;
            _currentYaw = euler.y;
            _currentPitch = euler.x;
        }
        
        private void Update()
        {
            HandlePanning();
            HandleZoom();
            HandleRotation();
            
            if (_autoFocusOnFlock)
            {
                FocusOnFlockCenter();
            }
        }
        
        private void HandlePanning()
        {
            // Get pan input from arrow keys (Vector2: x = left/right, y = up/down)
            Vector2 panInput = SwarmDemoInput.PanInput;
            Vector3 panDirection = new Vector3(panInput.x, 0f, panInput.y);
            
            if (panDirection.sqrMagnitude > 0.01f)
            {
                // Pan relative to camera's horizontal facing
                Vector3 forward = transform.forward;
                forward.y = 0;
                forward.Normalize();
                
                Vector3 right = transform.right;
                right.y = 0;
                right.Normalize();
                
                Vector3 worldPan = (forward * panDirection.z + right * panDirection.x) * _panSpeed * Time.deltaTime;
                transform.position += worldPan;
            }
        }
        
        private void HandleZoom()
        {
            // Get scroll input (y component is vertical scroll)
            // Note: 120 is the typical Windows scroll delta, but this may vary by platform
            float scroll = SwarmDemoInput.ScrollInput.y / _scrollNormalization;
            
            if (Mathf.Abs(scroll) > 0.01f)
            {
                Vector3 newPos = transform.position + transform.forward * scroll * _zoomSpeed;
                newPos.y = Mathf.Clamp(newPos.y, _minHeight, _maxHeight);
                transform.position = newPos;
            }
        }
        
        private void HandleRotation()
        {
            // Right mouse button + drag to rotate
            if (SwarmDemoInput.RotateButton)
            {
                Vector2 delta = SwarmDemoInput.RotateDelta;
                float mouseX = delta.x * 0.1f; // Scale down the delta
                float mouseY = delta.y * 0.1f;
                
                _currentYaw += mouseX * _rotationSpeed * Time.deltaTime;
                _currentPitch -= mouseY * _rotationSpeed * Time.deltaTime;
                _currentPitch = Mathf.Clamp(_currentPitch, _minPitch, _maxPitch);
                
                transform.rotation = Quaternion.Euler(_currentPitch, _currentYaw, 0f);
            }
        }
        
        private void FocusOnFlockCenter()
        {
            var agents = FindObjectsByType<SwarmAgent>(FindObjectsSortMode.None);
            if (agents.Length == 0) return;
            
            Vector3 center = Vector3.zero;
            foreach (var agent in agents)
            {
                center += agent.Position;
            }
            center /= agents.Length;
            
            // Smoothly move toward center while maintaining height
            Vector3 targetPos = center - transform.forward * 20f;
            targetPos.y = transform.position.y;
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 2f);
        }
        
        /// <summary>
        /// Focus camera on a specific position.
        /// </summary>
        public void FocusOn(Vector3 position)
        {
            Vector3 targetPos = position - transform.forward * 20f;
            targetPos.y = Mathf.Clamp(targetPos.y, _minHeight, _maxHeight);
            transform.position = targetPos;
        }
    }
}
