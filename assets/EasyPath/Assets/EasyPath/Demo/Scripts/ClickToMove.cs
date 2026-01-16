using UnityEngine;
using EasyPath;

namespace EasyPath.Demo
{
    /// <summary>
    /// Simple click-to-move demonstration.
    /// Click on the ground to move the agent to that position.
    /// </summary>
    public class ClickToMove : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private EasyPathAgent _agent;
        [SerializeField] private LayerMask _groundLayer;
        [SerializeField] private Camera _camera;
        
        [Header("Visual Feedback")]
        [SerializeField] private bool _showClickMarker = true;
        [SerializeField] private float _markerDuration = 1f;
        [SerializeField] private Color _markerColor = Color.green;
        
        private Vector3 _lastClickPosition;
        private float _markerTimer;
        
        private void Start()
        {
            EasyPathDemoInput.Initialize();
            
            if (_agent == null)
            {
                _agent = FindFirstObjectByType<EasyPathAgent>();
            }
            
            if (_camera == null)
            {
                _camera = Camera.main;
            }
            
            if (_agent == null)
            {
                Debug.LogError("[ClickToMove] No EasyPathAgent found!");
                enabled = false;
            }
        }
        
        private void Update()
        {
            HandleInput();
            UpdateMarker();
        }
        
        private void HandleInput()
        {
            if (EasyPathDemoInput.ClickPressed)
            {
                Ray ray = _camera.ScreenPointToRay(EasyPathDemoInput.MousePosition);
                
                if (Physics.Raycast(ray, out RaycastHit hit, 1000f, _groundLayer))
                {
                    MoveAgentTo(hit.point);
                }
            }
        }
        
        private void MoveAgentTo(Vector3 destination)
        {
            if (_agent.SetDestination(destination))
            {
                _lastClickPosition = destination;
                _markerTimer = _markerDuration;
                Debug.Log($"[ClickToMove] Moving to {destination}");
            }
            else
            {
                Debug.LogWarning($"[ClickToMove] Could not find path to {destination}");
            }
        }
        
        private void UpdateMarker()
        {
            if (_markerTimer > 0)
            {
                _markerTimer -= Time.deltaTime;
            }
        }
        
        private void OnDrawGizmos()
        {
            if (!_showClickMarker || _markerTimer <= 0)
            {
                return;
            }
            
            float alpha = _markerTimer / _markerDuration;
            Gizmos.color = new Color(_markerColor.r, _markerColor.g, _markerColor.b, alpha);
            
            // Draw destination marker
            Gizmos.DrawSphere(_lastClickPosition, 0.3f);
            Gizmos.DrawWireSphere(_lastClickPosition, 0.5f);
            
            // Draw vertical line
            Gizmos.DrawLine(
                _lastClickPosition,
                _lastClickPosition + Vector3.up * 2f
            );
        }
    }
}
