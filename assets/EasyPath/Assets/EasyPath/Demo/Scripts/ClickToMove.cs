using UnityEngine;
using EasyPath;
using System.Collections.Generic;

namespace EasyPath.Demo
{
    /// <summary>
    /// Simple click-to-move demonstration.
    /// Click on the ground to move the agent to that position.
    /// </summary>
    public class ClickToMove : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private List<EasyPathAgent> _agents = new List<EasyPathAgent>();
        [SerializeField] private bool _autoFindAgents = true;
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
            
            if (_autoFindAgents || _agents.Count == 0)
            {
                FindAllAgents();
            }
            
            if (_camera == null)
            {
                _camera = Camera.main;
            }
            
            if (_agents.Count == 0)
            {
                Debug.LogError("[ClickToMove] No EasyPathAgents found!");
                enabled = false;
            }
        }
        
        /// <summary>
        /// Find all EasyPathAgents in the scene.
        /// </summary>
        public void FindAllAgents()
        {
            _agents.Clear();
            EasyPathAgent[] foundAgents = FindObjectsByType<EasyPathAgent>(FindObjectsSortMode.None);
            _agents.AddRange(foundAgents);
            Debug.Log($"[ClickToMove] Found {_agents.Count} agents");
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
            int successCount = 0;
            
            foreach (var agent in _agents)
            {
                if (agent != null && agent.SetDestination(destination))
                {
                    successCount++;
                }
            }
            
            if (successCount > 0)
            {
                _lastClickPosition = destination;
                _markerTimer = _markerDuration;
                Debug.Log($"[ClickToMove] Moving {successCount} agent(s) to {destination}");
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
