using System.Collections.Generic;
using UnityEngine;

namespace SwarmAI
{
    /// <summary>
    /// Central manager for all swarm agents.
    /// Provides agent registry, spatial partitioning, and messaging.
    /// </summary>
    public class SwarmManager : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private SwarmSettings _settings;
        
        [Header("Debug")]
        [SerializeField] private bool _showDebugInfo = true;
        [SerializeField] private bool _showSpatialHash = false;
        [SerializeField] private Color _spatialHashColor = new Color(0.5f, 0.5f, 1f, 0.2f);
        
        // Singleton instance
        private static SwarmManager _instance;
        private static bool _applicationQuitting;
        
        // Agent registry
        private Dictionary<int, SwarmAgent> _agents;
        private List<SwarmAgent> _agentList; // Cached list for iteration without allocation
        private bool _agentListDirty = true;
        private int _nextAgentId;
        
        // Spatial partitioning
        private SpatialHash<SwarmAgent> _spatialHash;
        private float _lastSpatialHashUpdate;
        
        // Message queue
        private Queue<QueuedMessage> _messageQueue;
        
        // Performance: Frame-based neighbor query cache
        private Dictionary<int, List<SwarmAgent>> _neighborCache;
        private int _lastCacheFrame = -1;
        
        // Message wrapper for queued messages
        private struct QueuedMessage
        {
            public SwarmMessage Message;
            public int TargetId; // -1 for broadcast
        }
        
        #region Singleton
        
        /// <summary>
        /// Singleton instance of the SwarmManager.
        /// Creates a new instance if one doesn't exist (unless application is quitting).
        /// </summary>
        public static SwarmManager Instance
        {
            get
            {
                if (_applicationQuitting)
                {
                    return null;
                }
                
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<SwarmManager>();
                    
                    if (_instance == null && !_applicationQuitting)
                    {
                        // Create new instance
                        var go = new GameObject("SwarmManager");
                        _instance = go.AddComponent<SwarmManager>();
                        // Debug log handled by _showDebugInfo flag in Awake
                    }
                }
                return _instance;
            }
        }
        
        /// <summary>
        /// Check if an instance exists without creating one.
        /// Use this in OnDisable/OnDestroy to avoid creating new instances during scene teardown.
        /// </summary>
        public static bool HasInstance => _instance != null && !_applicationQuitting;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Global settings for the swarm system.
        /// </summary>
        public SwarmSettings Settings => _settings;
        
        /// <summary>
        /// Number of registered agents.
        /// </summary>
        public int AgentCount => _agents?.Count ?? 0;
        
        /// <summary>
        /// All registered agents (read-only).
        /// </summary>
        public IReadOnlyDictionary<int, SwarmAgent> Agents => _agents;
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Fired when an agent is registered.
        /// </summary>
        public event System.Action<SwarmAgent> OnAgentRegistered;
        
        /// <summary>
        /// Fired when an agent is unregistered.
        /// </summary>
        public event System.Action<SwarmAgent> OnAgentUnregistered;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            // Singleton setup
            if (_instance != null && _instance != this)
            {
                if (_showDebugInfo)
                {
                    Debug.LogWarning("[SwarmManager] Multiple instances detected. Destroying duplicate.");
                }
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            
            // Initialize
            _agents = new Dictionary<int, SwarmAgent>();
            _agentList = new List<SwarmAgent>();
            _messageQueue = new Queue<QueuedMessage>();
            _nextAgentId = 0;
            
            // Create or load settings
            if (_settings == null)
            {
                _settings = SwarmSettings.CreateDefault();
            }
            
            // Initialize spatial hash
            _spatialHash = new SpatialHash<SwarmAgent>(_settings.SpatialHashCellSize);
            
            // Initialize neighbor cache
            _neighborCache = new Dictionary<int, List<SwarmAgent>>();
            
            if (_showDebugInfo)
            {
                Debug.Log($"[SwarmManager] Initialized with cell size {_settings.SpatialHashCellSize}");
            }
        }
        
        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
        
        private void OnApplicationQuit()
        {
            _applicationQuitting = true;
        }
        
        private void Update()
        {
            // Update spatial hash
            UpdateSpatialHash();
            
            // Process message queue
            ProcessMessages();
        }
        
        private void LateUpdate()
        {
            // Rebuild agent list if dirty (avoids dictionary enumerator allocation)
            if (_agentListDirty)
            {
                _agentList.Clear();
                foreach (var kvp in _agents)
                {
                    _agentList.Add(kvp.Value);
                }
                _agentListDirty = false;
            }
            
            // Update agent positions in spatial hash (only for dirty agents)
            int count = _agentList.Count;
            for (int i = 0; i < count; i++)
            {
                SwarmAgent agent = _agentList[i];
                if (agent != null && agent.gameObject.activeInHierarchy && agent.IsPositionDirty)
                {
                    _spatialHash.UpdatePosition(agent, agent.Position);
                    agent.ClearPositionDirty();
                }
            }
        }
        
        #endregion
        
        #region Agent Registry
        
        /// <summary>
        /// Register an agent with the swarm system.
        /// Called automatically by SwarmAgent.OnEnable.
        /// </summary>
        public void RegisterAgent(SwarmAgent agent)
        {
            if (agent == null) return;
            
            // Check if already registered
            if (agent.AgentId >= 0 && _agents.ContainsKey(agent.AgentId))
            {
                return;
            }
            
            // Assign ID
            int id = _nextAgentId++;
            agent.SetAgentId(id);
            
            // Add to registry
            _agents[id] = agent;
            _agentListDirty = true;
            
            // Add to spatial hash
            _spatialHash.Insert(agent, agent.Position);
            
            OnAgentRegistered?.Invoke(agent);
            
            if (_showDebugInfo)
            {
                Debug.Log($"[SwarmManager] Registered agent {id} ({agent.name}). Total: {_agents.Count}");
            }
        }
        
        /// <summary>
        /// Unregister an agent from the swarm system.
        /// Called automatically by SwarmAgent.OnDisable.
        /// </summary>
        public void UnregisterAgent(SwarmAgent agent)
        {
            if (agent == null || agent.AgentId < 0) return;
            
            int id = agent.AgentId;
            
            if (_agents.Remove(id))
            {
                _agentListDirty = true;
                _spatialHash.Remove(agent);
                OnAgentUnregistered?.Invoke(agent);
                
                if (_showDebugInfo)
                {
                    Debug.Log($"[SwarmManager] Unregistered agent {id}. Total: {_agents.Count}");
                }
            }
        }
        
        /// <summary>
        /// Get an agent by ID.
        /// </summary>
        public SwarmAgent GetAgent(int id)
        {
            _agents.TryGetValue(id, out SwarmAgent agent);
            return agent;
        }
        
        /// <summary>
        /// Get all agents as a list.
        /// </summary>
        public List<SwarmAgent> GetAllAgents()
        {
            return new List<SwarmAgent>(_agents.Values);
        }
        
        /// <summary>
        /// Get all agents into a pre-allocated list to reduce GC.
        /// </summary>
        public void GetAllAgents(List<SwarmAgent> results)
        {
            results.Clear();
            results.AddRange(_agents.Values);
        }
        
        #endregion
        
        #region Spatial Queries
        
        /// <summary>
        /// Get all agents within a radius of a position.
        /// Results are cached per-frame for identical queries.
        /// </summary>
        public List<SwarmAgent> GetNeighbors(Vector3 position, float radius)
        {
            if (_spatialHash == null) return new List<SwarmAgent>();
            
            // Clear cache on new frame - return lists to pool to reduce GC
            if (Time.frameCount != _lastCacheFrame)
            {
                // Return all cached lists to pool before clearing
                foreach (var kvp in _neighborCache)
                {
                    _spatialHash.ReturnListToPool(kvp.Value);
                }
                _neighborCache.Clear();
                _lastCacheFrame = Time.frameCount;
            }
            
            // Create cache key from position and radius (discretized)
            int cacheKey = GetCacheKey(position, radius);
            
            if (_neighborCache.TryGetValue(cacheKey, out List<SwarmAgent> cached))
            {
                return cached;
            }
            
            var result = _spatialHash.Query(position, radius);
            _neighborCache[cacheKey] = result;
            return result;
        }
        
        /// <summary>
        /// Generate a cache key from position and radius.
        /// Uses finer granularity (0.1 units) to avoid cache collisions for nearby positions.
        /// </summary>
        private int GetCacheKey(Vector3 position, float radius)
        {
            // Discretize position with 0.1 unit precision to avoid cache collisions
            int px = Mathf.RoundToInt(position.x * 10);
            int py = Mathf.RoundToInt(position.y * 10);
            int pz = Mathf.RoundToInt(position.z * 10);
            int r = Mathf.RoundToInt(radius * 10); // 0.1 precision
            
            // Combine into hash
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + px;
                hash = hash * 31 + py;
                hash = hash * 31 + pz;
                hash = hash * 31 + r;
                return hash;
            }
        }
        
        /// <summary>
        /// Get all agents within a radius, with accurate distance filtering.
        /// </summary>
        public void GetNeighbors(Vector3 position, float radius, List<SwarmAgent> results)
        {
            if (_spatialHash == null)
            {
                results.Clear();
                return;
            }
            _spatialHash.Query(position, radius, results, agent => agent.Position);
        }
        
        /// <summary>
        /// Get all agents within a radius, excluding a specific agent.
        /// </summary>
        public List<SwarmAgent> GetNeighborsExcluding(Vector3 position, float radius, SwarmAgent exclude)
        {
            if (_spatialHash == null) return new List<SwarmAgent>();
            return _spatialHash.QueryExcluding(position, radius, exclude);
        }
        
        /// <summary>
        /// Get all agents within a radius, excluding a specific agent, with accurate distance filtering.
        /// </summary>
        public void GetNeighborsExcluding(Vector3 position, float radius, SwarmAgent exclude, List<SwarmAgent> results)
        {
            if (_spatialHash == null)
            {
                results.Clear();
                return;
            }
            _spatialHash.QueryExcluding(position, radius, exclude, results, agent => agent.Position);
        }
        
        /// <summary>
        /// Get the nearest agent to a position.
        /// </summary>
        public SwarmAgent GetNearestAgent(Vector3 position, float maxRadius = float.MaxValue)
        {
            if (_spatialHash == null) return null;
            
            var neighbors = _spatialHash.Query(position, maxRadius);
            
            SwarmAgent nearest = null;
            float nearestDistSq = maxRadius * maxRadius;
            
            // Use indexed for loop to avoid enumerator allocation
            int count = neighbors.Count;
            for (int i = 0; i < count; i++)
            {
                var agent = neighbors[i];
                if (agent == null) continue;
                
                float distSq = (agent.Position - position).sqrMagnitude;
                if (distSq < nearestDistSq)
                {
                    nearestDistSq = distSq;
                    nearest = agent;
                }
            }
            
            // Return pooled list to reduce GC allocations
            _spatialHash.ReturnListToPool(neighbors);
            
            return nearest;
        }
        
        private void UpdateSpatialHash()
        {
            // Check if we should update based on interval
            if (_settings.SpatialHashUpdateInterval > 0)
            {
                if (Time.time - _lastSpatialHashUpdate < _settings.SpatialHashUpdateInterval)
                {
                    return;
                }
                _lastSpatialHashUpdate = Time.time;
            }
            
            // Update is done in LateUpdate for all agents
        }
        
        #endregion
        
        #region Messaging
        
        /// <summary>
        /// Send a message to a specific agent.
        /// </summary>
        public void SendMessage(int targetId, SwarmMessage message)
        {
            _messageQueue.Enqueue(new QueuedMessage
            {
                Message = message,
                TargetId = targetId
            });
        }
        
        /// <summary>
        /// Broadcast a message to all agents.
        /// </summary>
        public void BroadcastMessage(SwarmMessage message)
        {
            _messageQueue.Enqueue(new QueuedMessage
            {
                Message = message,
                TargetId = -1
            });
        }
        
        /// <summary>
        /// Send a message to all agents within a radius.
        /// </summary>
        public void BroadcastToArea(Vector3 position, float radius, SwarmMessage message)
        {
            var agents = GetNeighbors(position, radius);
            int count = agents.Count;
            for (int i = 0; i < count; i++)
            {
                SendMessage(agents[i].AgentId, message);
            }
        }
        
        private void ProcessMessages()
        {
            int processed = 0;
            int maxPerFrame = _settings != null ? _settings.MaxAgentsPerFrame : 50;
            
            while (_messageQueue.Count > 0 && processed < maxPerFrame)
            {
                var qm = _messageQueue.Dequeue();
                
                if (qm.TargetId < 0)
                {
                    // Broadcast - use cached list to avoid enumerator allocation
                    int count = _agentList.Count;
                    for (int i = 0; i < count; i++)
                    {
                        SwarmAgent agent = _agentList[i];
                        if (agent != null && agent.gameObject != null)
                        {
                            agent.ReceiveMessage(qm.Message);
                        }
                    }
                }
                else
                {
                    // Targeted message
                    if (_agents.TryGetValue(qm.TargetId, out SwarmAgent agent) && agent != null && agent.gameObject != null)
                    {
                        agent.ReceiveMessage(qm.Message);
                    }
                }
                
                processed++;
            }
        }
        
        #endregion
        
        #region Commands
        
        /// <summary>
        /// Command all agents to move to a position.
        /// </summary>
        public void MoveAllTo(Vector3 position)
        {
            BroadcastMessage(SwarmMessage.MoveTo(position));
        }
        
        /// <summary>
        /// Command all agents to stop.
        /// </summary>
        public void StopAll()
        {
            BroadcastMessage(SwarmMessage.Stop());
        }
        
        /// <summary>
        /// Command all agents to seek a position.
        /// </summary>
        public void SeekAll(Vector3 position)
        {
            BroadcastMessage(SwarmMessage.Seek(position));
        }
        
        #endregion
        
        #region Debug
        
        private void OnDrawGizmos()
        {
            if (!_showSpatialHash || _spatialHash == null) return;
            
            // Draw spatial hash cells that contain agents
            Gizmos.color = _spatialHashColor;
            float cellSize = _spatialHash.CellSize;
            
            HashSet<Vector2Int> drawnCells = new HashSet<Vector2Int>();
            
            if (_agentList != null)
            {
                int count = _agentList.Count;
                for (int i = 0; i < count; i++)
                {
                    SwarmAgent agent = _agentList[i];
                    if (agent == null) continue;
                    
                    if (_spatialHash.TryGetCell(agent, out Vector2Int cell))
                    {
                        if (!drawnCells.Contains(cell))
                        {
                            drawnCells.Add(cell);
                            
                            Vector3 cellCenter = new Vector3(
                                cell.x * cellSize + cellSize * 0.5f,
                                0,
                                cell.y * cellSize + cellSize * 0.5f
                            );
                            
                            Gizmos.DrawWireCube(cellCenter, new Vector3(cellSize, 0.1f, cellSize));
                        }
                    }
                }
            }
        }
        
        private void OnGUI()
        {
            if (!_showDebugInfo) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 200, 100));
            GUILayout.Label($"SwarmAI - Agents: {AgentCount}");
            GUILayout.Label($"Messages queued: {_messageQueue?.Count ?? 0}");
            GUILayout.EndArea();
        }
        
        #endregion
    }
}
