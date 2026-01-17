using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace SwarmAI.Jobs
{
    /// <summary>
    /// Manages the Jobs/Burst parallel processing system for SwarmAI.
    /// Attach to the same GameObject as SwarmManager for automatic integration.
    /// </summary>
    [RequireComponent(typeof(SwarmManager))]
    public class SwarmJobSystem : MonoBehaviour
    {
        [Header("Jobs Settings")]
        [Tooltip("Enable parallel job processing for steering calculations.")]
        [SerializeField] private bool _useJobs = true;
        
        [Tooltip("Minimum agent count before using Jobs (below this, single-threaded is faster).")]
        [SerializeField] private int _minAgentsForJobs = 50;
        
        [Tooltip("Batch size for parallel jobs. Higher = less overhead, lower = better load balancing.")]
        [SerializeField] private int _batchSize = 64;
        
        [Header("Behavior Weights")]
        [SerializeField] private float _separationWeight = 1.5f;
        [SerializeField] private float _alignmentWeight = 1.0f;
        [SerializeField] private float _cohesionWeight = 1.0f;
        [SerializeField] private float _separationRadius = 2.5f;
        
        [Header("Debug")]
        [SerializeField] private bool _showDebugInfo = true;
        
        // Native collections
        private NativeArray<AgentData> _agentDataArray;
        private NativeArray<SteeringResult> _steeringResults;
        private BurstSpatialHash _spatialHash;
        
        // Job handles for dependency management
        private JobHandle _currentJobHandle;
        private bool _jobsScheduled;
        
        // References
        private SwarmManager _swarmManager;
        private Dictionary<int, int> _agentIdToIndex;
        private List<SwarmAgent> _activeAgents;
        
        // Stats
        private int _lastProcessedCount;
        private float _lastJobTime;
        
        #region Properties
        
        /// <summary>Whether Jobs processing is enabled.</summary>
        public bool UseJobs
        {
            get => _useJobs;
            set => _useJobs = value;
        }
        
        /// <summary>Whether Jobs are currently being used (enabled and enough agents).</summary>
        public bool IsUsingJobs => _useJobs && _activeAgents != null && _activeAgents.Count >= _minAgentsForJobs;
        
        /// <summary>Number of agents processed in last update.</summary>
        public int LastProcessedCount => _lastProcessedCount;
        
        /// <summary>Time taken for last job batch in milliseconds.</summary>
        public float LastJobTimeMs => _lastJobTime * 1000f;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            _swarmManager = GetComponent<SwarmManager>();
            _agentIdToIndex = new Dictionary<int, int>();
            _activeAgents = new List<SwarmAgent>();
        }
        
        private void OnEnable()
        {
            // Subscribe to agent events
            if (_swarmManager != null)
            {
                _swarmManager.OnAgentRegistered += OnAgentRegistered;
                _swarmManager.OnAgentUnregistered += OnAgentUnregistered;
            }
        }
        
        private void OnDisable()
        {
            // Complete any pending jobs
            CompleteJobs();
            
            // Unsubscribe
            if (_swarmManager != null)
            {
                _swarmManager.OnAgentRegistered -= OnAgentRegistered;
                _swarmManager.OnAgentUnregistered -= OnAgentUnregistered;
            }
            
            // Dispose native collections
            DisposeNativeCollections();
        }
        
        private void Update()
        {
            if (!_useJobs || _swarmManager == null) return;
            
            // Refresh active agent list
            RefreshActiveAgents();
            
            // Only use jobs if we have enough agents
            if (_activeAgents.Count < _minAgentsForJobs) return;
            
            float startTime = Time.realtimeSinceStartup;
            
            // Ensure native arrays are properly sized
            EnsureCapacity(_activeAgents.Count);
            
            // Copy data from agents to native arrays
            CopyAgentDataToNative();
            
            // Schedule jobs
            ScheduleJobs();
            
            _lastJobTime = Time.realtimeSinceStartup - startTime;
        }
        
        private void LateUpdate()
        {
            if (!_jobsScheduled) return;
            
            float startTime = Time.realtimeSinceStartup;
            
            // Complete jobs and apply results
            CompleteJobs();
            ApplyResultsToAgents();
            
            _lastJobTime += Time.realtimeSinceStartup - startTime;
            _jobsScheduled = false;
        }
        
        #endregion
        
        #region Job Management
        
        private void RefreshActiveAgents()
        {
            _activeAgents.Clear();
            _agentIdToIndex.Clear();
            
            if (_swarmManager.Agents == null) return;
            
            int index = 0;
            foreach (var kvp in _swarmManager.Agents)
            {
                SwarmAgent agent = kvp.Value;
                if (agent != null && agent.gameObject.activeInHierarchy)
                {
                    _activeAgents.Add(agent);
                    _agentIdToIndex[agent.AgentId] = index;
                    index++;
                }
            }
            
            _lastProcessedCount = _activeAgents.Count;
        }
        
        private void EnsureCapacity(int count)
        {
            // Dispose and recreate if size changed significantly
            bool needsResize = !_agentDataArray.IsCreated || 
                              _agentDataArray.Length < count ||
                              _agentDataArray.Length > count * 2;
            
            if (needsResize)
            {
                DisposeNativeCollections();
                
                int capacity = Mathf.Max(count, 64);
                _agentDataArray = new NativeArray<AgentData>(capacity, Allocator.Persistent);
                _steeringResults = new NativeArray<SteeringResult>(capacity, Allocator.Persistent);
                
                float cellSize = _swarmManager.Settings != null ? 
                    _swarmManager.Settings.SpatialHashCellSize : 10f;
                _spatialHash = new BurstSpatialHash(cellSize, capacity, Allocator.Persistent);
            }
        }
        
        private void CopyAgentDataToNative()
        {
            int count = _activeAgents.Count;
            
            for (int i = 0; i < count; i++)
            {
                SwarmAgent agent = _activeAgents[i];
                Vector3 pos = agent.Position;
                Vector3 vel = agent.Velocity;
                
                _agentDataArray[i] = AgentData.Create(
                    agent.AgentId,
                    new float3(pos.x, pos.y, pos.z),
                    new float3(vel.x, vel.y, vel.z),
                    agent.MaxSpeed,
                    agent.MaxForce,
                    agent.NeighborRadius,
                    agent.Mass
                );
            }
            
            // Mark remaining slots as inactive
            for (int i = count; i < _agentDataArray.Length; i++)
            {
                var data = _agentDataArray[i];
                data.IsActive = false;
                _agentDataArray[i] = data;
            }
        }
        
        private void ScheduleJobs()
        {
            int count = _activeAgents.Count;
            if (count == 0) return;
            
            // Clear and rebuild spatial hash
            _spatialHash.Clear();
            
            // Build spatial hash job
            var buildHashJob = new BuildSpatialHashJob
            {
                Agents = _agentDataArray,
                CellToAgents = _spatialHash.CellToAgents.AsParallelWriter(),
                CellSize = _spatialHash.CellSize
            };
            
            JobHandle buildHandle = buildHashJob.Schedule(count, _batchSize);
            
            // Flocking steering job (depends on spatial hash being built)
            var flockingJob = new FlockingSteeringJob
            {
                Agents = _agentDataArray,
                SpatialHash = _spatialHash.CellToAgents,
                Weights = new BehaviorWeights
                {
                    Separation = _separationWeight,
                    Alignment = _alignmentWeight,
                    Cohesion = _cohesionWeight,
                    SeparationRadius = _separationRadius
                },
                CellSize = _spatialHash.CellSize,
                Results = _steeringResults
            };
            
            _currentJobHandle = flockingJob.Schedule(count, _batchSize, buildHandle);
            _jobsScheduled = true;
            
            // Schedule job to run
            JobHandle.ScheduleBatchedJobs();
        }
        
        private void CompleteJobs()
        {
            if (!_jobsScheduled) return;
            
            _currentJobHandle.Complete();
        }
        
        private void ApplyResultsToAgents()
        {
            int count = _activeAgents.Count;
            
            for (int i = 0; i < count; i++)
            {
                SwarmAgent agent = _activeAgents[i];
                if (agent == null) continue;
                
                SteeringResult result = _steeringResults[i];
                
                // Apply the job-calculated steering force to the agent
                Vector3 force = new Vector3(
                    result.SteeringForce.x,
                    result.SteeringForce.y,
                    result.SteeringForce.z
                );
                
                // Apply force through the agent's public API
                agent.ApplyForce(force);
            }
        }
        
        private void DisposeNativeCollections()
        {
            // Complete any pending jobs first
            if (_jobsScheduled)
            {
                _currentJobHandle.Complete();
                _jobsScheduled = false;
            }
            
            if (_agentDataArray.IsCreated)
            {
                _agentDataArray.Dispose();
            }
            
            if (_steeringResults.IsCreated)
            {
                _steeringResults.Dispose();
            }
            
            if (_spatialHash.IsCreated)
            {
                _spatialHash.Dispose();
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnAgentRegistered(SwarmAgent agent)
        {
            // Agent list will be refreshed in next Update
        }
        
        private void OnAgentUnregistered(SwarmAgent agent)
        {
            // Agent list will be refreshed in next Update
        }
        
        #endregion
        
        #region Debug
        
        private void OnGUI()
        {
            if (!_showDebugInfo || !_useJobs) return;
            
            GUILayout.BeginArea(new Rect(10, 120, 250, 100));
            GUILayout.Label($"[Jobs System]");
            GUILayout.Label($"Active: {IsUsingJobs}");
            GUILayout.Label($"Agents: {_lastProcessedCount}");
            GUILayout.Label($"Job Time: {_lastJobTime * 1000f:F2}ms");
            GUILayout.EndArea();
        }
        
        #endregion
    }
}
