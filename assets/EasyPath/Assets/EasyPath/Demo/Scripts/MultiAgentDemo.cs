using UnityEngine;
using EasyPath;
using System.Collections.Generic;

namespace EasyPath.Demo
{
    /// <summary>
    /// Demo controller for multiple pathfinding agents.
    /// Demonstrates various multi-agent behaviors.
    /// </summary>
    public class MultiAgentDemo : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EasyPathGrid _grid;
        [SerializeField] private List<EasyPathAgent> _agents = new List<EasyPathAgent>();
        
        [Header("Demo Settings")]
        [SerializeField] private bool _autoFindAgents = true;
        [SerializeField] private float _minWanderInterval = 2f;
        [SerializeField] private float _maxWanderInterval = 5f;
        
        [Header("Controls")]
        [SerializeField] private KeyCode _sendAllToRandomKey = KeyCode.Space;
        [SerializeField] private KeyCode _startWanderKey = KeyCode.W;
        [SerializeField] private KeyCode _stopAllKey = KeyCode.S;
        [SerializeField] private KeyCode _gatherKey = KeyCode.G;
        [SerializeField] private KeyCode _scatterKey = KeyCode.X;
        
        [Header("State")]
        [SerializeField] private bool _isWandering = false;
        
        private float[] _nextWanderTimes;
        private Vector3 _gridCenter;
        private float _gridRadius;
        
        private void Start()
        {
            if (_autoFindAgents)
            {
                FindAllAgents();
            }
            
            if (_grid == null)
            {
                _grid = FindFirstObjectByType<EasyPathGrid>();
            }
            
            if (_grid != null)
            {
                _gridCenter = new Vector3(
                    _grid.Width * _grid.CellSize / 2f,
                    0f,
                    _grid.Height * _grid.CellSize / 2f
                );
                _gridRadius = Mathf.Min(_grid.Width, _grid.Height) * _grid.CellSize / 2f - 2f;
            }
            
            InitializeWanderTimers();
        }
        
        private void Update()
        {
            HandleInput();
            
            if (_isWandering)
            {
                UpdateWandering();
            }
        }
        
        private void HandleInput()
        {
            // Send all to random positions
            if (Input.GetKeyDown(_sendAllToRandomKey))
            {
                SendAllToRandomPositions();
            }
            
            // Toggle wandering
            if (Input.GetKeyDown(_startWanderKey))
            {
                ToggleWandering();
            }
            
            // Stop all agents
            if (Input.GetKeyDown(_stopAllKey))
            {
                StopAllAgents();
            }
            
            // Gather at center
            if (Input.GetKeyDown(_gatherKey))
            {
                GatherAtCenter();
            }
            
            // Scatter to corners
            if (Input.GetKeyDown(_scatterKey))
            {
                ScatterToCorners();
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
            Debug.Log($"[MultiAgentDemo] Found {_agents.Count} agents");
        }
        
        /// <summary>
        /// Send all agents to random walkable positions.
        /// </summary>
        public void SendAllToRandomPositions()
        {
            _isWandering = false;
            
            foreach (var agent in _agents)
            {
                Vector3 randomPos = GetRandomWalkablePosition();
                agent.SetDestination(randomPos);
            }
            
            Debug.Log("[MultiAgentDemo] Sent all agents to random positions");
        }
        
        /// <summary>
        /// Toggle autonomous wandering behavior.
        /// </summary>
        public void ToggleWandering()
        {
            _isWandering = !_isWandering;
            
            if (_isWandering)
            {
                InitializeWanderTimers();
                Debug.Log("[MultiAgentDemo] Wandering started");
            }
            else
            {
                Debug.Log("[MultiAgentDemo] Wandering stopped");
            }
        }
        
        /// <summary>
        /// Stop all agents.
        /// </summary>
        public void StopAllAgents()
        {
            _isWandering = false;
            
            foreach (var agent in _agents)
            {
                agent.Stop();
            }
            
            Debug.Log("[MultiAgentDemo] Stopped all agents");
        }
        
        /// <summary>
        /// Send all agents to gather at the center of the grid.
        /// </summary>
        public void GatherAtCenter()
        {
            _isWandering = false;
            
            for (int i = 0; i < _agents.Count; i++)
            {
                // Offset slightly so they don't all go to exact same spot
                float angle = (float)i / _agents.Count * Mathf.PI * 2f;
                float radius = 1.5f;
                Vector3 offset = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                
                _agents[i].SetDestination(_gridCenter + offset);
            }
            
            Debug.Log("[MultiAgentDemo] Gathering at center");
        }
        
        /// <summary>
        /// Send agents to scatter to the corners of the grid.
        /// </summary>
        public void ScatterToCorners()
        {
            _isWandering = false;
            
            Vector3[] corners = new Vector3[]
            {
                new Vector3(2f, 0f, 2f),
                new Vector3(_grid.Width * _grid.CellSize - 2f, 0f, 2f),
                new Vector3(2f, 0f, _grid.Height * _grid.CellSize - 2f),
                new Vector3(_grid.Width * _grid.CellSize - 2f, 0f, _grid.Height * _grid.CellSize - 2f)
            };
            
            for (int i = 0; i < _agents.Count; i++)
            {
                Vector3 corner = corners[i % corners.Length];
                _agents[i].SetDestination(corner);
            }
            
            Debug.Log("[MultiAgentDemo] Scattering to corners");
        }
        
        private void InitializeWanderTimers()
        {
            _nextWanderTimes = new float[_agents.Count];
            
            for (int i = 0; i < _agents.Count; i++)
            {
                // Stagger initial wander times
                _nextWanderTimes[i] = Time.time + Random.Range(0f, _maxWanderInterval);
            }
        }
        
        private void UpdateWandering()
        {
            for (int i = 0; i < _agents.Count; i++)
            {
                if (Time.time >= _nextWanderTimes[i])
                {
                    // Check if agent is idle or near destination
                    if (!_agents[i].IsMoving || _agents[i].RemainingDistance < 1f)
                    {
                        Vector3 randomPos = GetRandomWalkablePosition();
                        _agents[i].SetDestination(randomPos);
                        _nextWanderTimes[i] = Time.time + Random.Range(_minWanderInterval, _maxWanderInterval);
                    }
                }
            }
        }
        
        private Vector3 GetRandomWalkablePosition()
        {
            if (_grid == null)
            {
                return Vector3.zero;
            }
            
            // Try to find a walkable position
            for (int attempt = 0; attempt < 30; attempt++)
            {
                int x = Random.Range(1, _grid.Width - 1);
                int y = Random.Range(1, _grid.Height - 1);
                
                if (_grid.IsWalkable(x, y))
                {
                    return _grid.GridToWorld(x, y);
                }
            }
            
            // Fallback to center
            return _gridCenter;
        }
        
        private void OnGUI()
        {
            // Draw additional controls for multi-agent demo
            GUILayout.BeginArea(new Rect(10, 220, 280, 180));
            GUILayout.BeginVertical(GUI.skin.box);
            
            GUILayout.Label("Multi-Agent Controls", GetHeaderStyle());
            GUILayout.Space(5);
            
            GUILayout.Label($"• {_sendAllToRandomKey} - Send all to random");
            GUILayout.Label($"• {_startWanderKey} - Toggle wander ({(_isWandering ? "ON" : "OFF")})");
            GUILayout.Label($"• {_stopAllKey} - Stop all agents");
            GUILayout.Label($"• {_gatherKey} - Gather at center");
            GUILayout.Label($"• {_scatterKey} - Scatter to corners");
            
            GUILayout.Space(5);
            GUILayout.Label($"Agents: {_agents.Count}");
            
            int movingCount = 0;
            foreach (var agent in _agents)
            {
                if (agent.IsMoving) movingCount++;
            }
            GUILayout.Label($"Moving: {movingCount}");
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        
        private GUIStyle _headerStyle;
        private GUIStyle GetHeaderStyle()
        {
            if (_headerStyle == null)
            {
                _headerStyle = new GUIStyle(GUI.skin.label);
                _headerStyle.fontSize = 14;
                _headerStyle.fontStyle = FontStyle.Bold;
            }
            return _headerStyle;
        }
    }
}
