using UnityEngine;
using System.Collections.Generic;
using SwarmAI.Jobs;

namespace SwarmAI.Demo
{
    /// <summary>
    /// Demo showcasing Jobs/Burst parallel processing with large swarms (500+ agents).
    /// Demonstrates the performance benefits of the Jobs system for steering calculations.
    /// </summary>
    public class MassSwarmDemo : SwarmDemoController
    {
        [Header("Mass Swarm Settings")]
        [SerializeField] private int _initialAgentCount = 500;
        [SerializeField] private int _spawnBatchSize = 50;
        [SerializeField] private float _largeSpawnRadius = 25f;
        
        [Header("Bounds")]
        [SerializeField] private Vector3 _boundsCenter = new Vector3(0, 0, 0);
        [SerializeField] private Vector3 _boundsSize = new Vector3(60, 10, 60);
        
        [Header("Flocking Weights")]
        [SerializeField] private float _separationWeight = 1.5f;
        [SerializeField] private float _alignmentWeight = 1.0f;
        [SerializeField] private float _cohesionWeight = 1.0f;
        [SerializeField] private float _wanderWeight = 0.2f;
        
        // Jobs system reference
        private SwarmJobSystem _jobSystem;
        private JobsBenchmark _benchmark;
        
        // Behaviors for non-Jobs mode
        private Dictionary<SwarmAgent, List<IBehavior>> _agentBehaviors = new Dictionary<SwarmAgent, List<IBehavior>>();
        
        // Current target for seeking
        private Vector3 _targetPosition;
        private bool _hasTarget = false;
        
        // Performance tracking
        private float _lastFps;
        private float _fpsUpdateTimer;
        private const float FpsUpdateInterval = 0.5f;
        
        protected override void Start()
        {
            _demoTitle = "SwarmAI - Mass Swarm Demo (Jobs/Burst)";
            _agentCount = _initialAgentCount;
            _spawnRadius = _largeSpawnRadius;
            _targetPosition = _boundsCenter;
            
            base.Start();
            
            // Find Jobs system components
            _jobSystem = FindFirstObjectByType<SwarmJobSystem>();
            _benchmark = FindFirstObjectByType<JobsBenchmark>();
            
            // Setup behaviors for all existing agents
            SetupAllAgentBehaviors();
            
            Debug.Log($"[MassSwarmDemo] Started with {_agents.Count} agents. Jobs system: {(_jobSystem != null ? "Found" : "Not Found")}");
        }
        
        protected override void Update()
        {
            base.Update();
            
            // Update FPS counter
            _fpsUpdateTimer += Time.deltaTime;
            if (_fpsUpdateTimer >= FpsUpdateInterval)
            {
                _lastFps = 1f / Time.deltaTime;
                _fpsUpdateTimer = 0f;
            }
            
            // Ensure new agents have behaviors
            EnsureAgentBehaviors();
            
            HandleMassSwarmInput();
            UpdateBoundaryAvoidance();
        }
        
        protected override void CleanupStaleAgents()
        {
            // Clean up behavior dictionaries
            var staleAgents = new List<SwarmAgent>();
            foreach (var agent in _agentBehaviors.Keys)
            {
                if (agent == null)
                {
                    staleAgents.Add(agent);
                }
            }
            
            foreach (var staleAgent in staleAgents)
            {
                _agentBehaviors.Remove(staleAgent);
            }
            
            base.CleanupStaleAgents();
        }
        
        private void HandleMassSwarmInput()
        {
            // J - Toggle Jobs system
            if (SwarmDemoInput.ActionJPressed && _jobSystem != null)
            {
                _jobSystem.UseJobs = !_jobSystem.UseJobs;
                Debug.Log($"[MassSwarmDemo] Jobs system: {(_jobSystem.UseJobs ? "ON" : "OFF")}");
            }
            
            // + or = - Add agents
            if (SwarmDemoInput.PlusPressed)
            {
                SpawnMoreAgents(_spawnBatchSize);
            }
            
            // - - Remove agents
            if (SwarmDemoInput.MinusPressed)
            {
                RemoveAgents(_spawnBatchSize);
            }
            
            // Left click - Set target
            if (SwarmDemoInput.ClickPressed && Camera.main != null)
            {
                Ray ray = Camera.main.ScreenPointToRay(SwarmDemoInput.MousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 100f))
                {
                    SetSwarmTarget(hit.point);
                }
            }
            
            // Space - Scatter
            if (SwarmDemoInput.SpacePressed)
            {
                ScatterSwarm();
            }
            
            // G - Gather at center
            if (SwarmDemoInput.ActionGPressed)
            {
                GatherSwarm();
            }
            
            // Number keys for behavior toggles (when Jobs is off)
            if (_jobSystem == null || !_jobSystem.IsUsingJobs)
            {
                if (SwarmDemoInput.Number1Pressed) ToggleBehavior<SeparationBehavior>();
                if (SwarmDemoInput.Number2Pressed) ToggleBehavior<AlignmentBehavior>();
                if (SwarmDemoInput.Number3Pressed) ToggleBehavior<CohesionBehavior>();
                if (SwarmDemoInput.Number4Pressed) ToggleBehavior<WanderBehavior>();
            }
        }
        
        private void SpawnMoreAgents(int count)
        {
            int startIndex = _agents.Count;
            
            for (int i = 0; i < count; i++)
            {
                Vector3 randomOffset = Random.insideUnitSphere * _largeSpawnRadius;
                randomOffset.y = 0;
                Vector3 spawnPos = _spawnCenter + randomOffset;
                
                GameObject agentObj = CreateAgentWithVisual(spawnPos, startIndex + i);
                SwarmAgent agent = agentObj.GetComponent<SwarmAgent>();
                if (agent != null)
                {
                    _agents.Add(agent);
                    SetupAgentBehavior(agent);
                }
            }
            
            Debug.Log($"[MassSwarmDemo] Spawned {count} agents. Total: {_agents.Count}");
        }
        
        private void RemoveAgents(int count)
        {
            int removeCount = Mathf.Min(count, _agents.Count - 10); // Keep at least 10 agents
            
            for (int i = 0; i < removeCount; i++)
            {
                if (_agents.Count <= 10) break;
                
                int lastIndex = _agents.Count - 1;
                SwarmAgent agent = _agents[lastIndex];
                _agents.RemoveAt(lastIndex);
                
                if (agent != null)
                {
                    _agentBehaviors.Remove(agent);
                    Destroy(agent.gameObject);
                }
            }
            
            Debug.Log($"[MassSwarmDemo] Removed {removeCount} agents. Total: {_agents.Count}");
        }
        
        private GameObject CreateAgentWithVisual(Vector3 position, int index)
        {
            GameObject agentObj = new GameObject($"Agent_{index}");
            agentObj.transform.position = position;
            
            // Add SwarmAgent component
            SwarmAgent agent = agentObj.AddComponent<SwarmAgent>();
            
            // Create visual
            Color color = GetAgentColor(index);
            CreateAgentVisual(agentObj.transform, color);
            
            return agentObj;
        }
        
        private Color GetAgentColor(int index)
        {
            return AgentVisualUtility.GetAgentColor(index);
        }
        
        private void CreateAgentVisual(Transform parent, Color color)
        {
            AgentVisualUtility.CreateAgentVisual(parent, color);
        }
        
        private void SetupAllAgentBehaviors()
        {
            foreach (var agent in _agents)
            {
                if (agent != null)
                {
                    SetupAgentBehavior(agent);
                }
            }
        }
        
        private void EnsureAgentBehaviors()
        {
            foreach (var agent in _agents)
            {
                if (agent != null && !_agentBehaviors.ContainsKey(agent))
                {
                    SetupAgentBehavior(agent);
                }
            }
        }
        
        private void SetupAgentBehavior(SwarmAgent agent)
        {
            if (agent == null || _agentBehaviors.ContainsKey(agent)) return;
            
            var behaviors = new List<IBehavior>();
            
            var separation = new SeparationBehavior(agent.NeighborRadius);
            var alignment = new AlignmentBehavior(agent.NeighborRadius);
            var cohesion = new CohesionBehavior(agent.NeighborRadius);
            var wander = new WanderBehavior();
            
            agent.AddBehavior(separation, _separationWeight);
            agent.AddBehavior(alignment, _alignmentWeight);
            agent.AddBehavior(cohesion, _cohesionWeight);
            agent.AddBehavior(wander, _wanderWeight);
            
            behaviors.Add(separation);
            behaviors.Add(alignment);
            behaviors.Add(cohesion);
            behaviors.Add(wander);
            
            _agentBehaviors[agent] = behaviors;
        }
        
        private void ToggleBehavior<T>() where T : IBehavior
        {
            foreach (var agent in _agents)
            {
                if (agent == null || !_agentBehaviors.ContainsKey(agent)) continue;
                
                foreach (var behavior in _agentBehaviors[agent])
                {
                    if (behavior is T)
                    {
                        behavior.IsActive = !behavior.IsActive;
                    }
                }
            }
            
            Debug.Log($"[MassSwarmDemo] Toggled {typeof(T).Name}");
        }
        
        private bool IsBehaviorActive<T>() where T : IBehavior
        {
            if (_agents.Count == 0 || _agents[0] == null) return false;
            if (!_agentBehaviors.ContainsKey(_agents[0])) return false;
            
            foreach (var behavior in _agentBehaviors[_agents[0]])
            {
                if (behavior is T)
                {
                    return behavior.IsActive;
                }
            }
            return false;
        }
        
        private void SetSwarmTarget(Vector3 position)
        {
            _targetPosition = position;
            _hasTarget = true;
            
            // Move all agents toward target
            foreach (var agent in _agents)
            {
                if (agent != null)
                {
                    agent.SetTarget(position);
                }
            }
            
            Debug.Log($"[MassSwarmDemo] Target set to {position}");
        }
        
        private void ScatterSwarm()
        {
            _hasTarget = false;
            
            foreach (var agent in _agents)
            {
                if (agent == null) continue;
                
                // Clear target and increase separation
                agent.ClearTarget();
                
                if (_agentBehaviors.ContainsKey(agent))
                {
                    foreach (var behavior in _agentBehaviors[agent])
                    {
                        if (behavior is CohesionBehavior)
                            behavior.IsActive = false;
                        if (behavior is SeparationBehavior)
                            behavior.Weight = _separationWeight * 3f;
                        if (behavior is WanderBehavior)
                            behavior.IsActive = true;
                    }
                }
            }
            
            Debug.Log("[MassSwarmDemo] Swarm scattered!");
        }
        
        private void GatherSwarm()
        {
            _targetPosition = _boundsCenter;
            _hasTarget = true;
            
            foreach (var agent in _agents)
            {
                if (agent == null) continue;
                
                agent.SetTarget(_boundsCenter);
                
                if (_agentBehaviors.ContainsKey(agent))
                {
                    foreach (var behavior in _agentBehaviors[agent])
                    {
                        if (behavior is CohesionBehavior)
                            behavior.IsActive = true;
                        if (behavior is SeparationBehavior)
                            behavior.Weight = _separationWeight;
                    }
                }
            }
            
            Debug.Log("[MassSwarmDemo] Swarm gathering at center");
        }
        
        private void UpdateBoundaryAvoidance()
        {
            Vector3 min = _boundsCenter - _boundsSize * 0.5f;
            Vector3 max = _boundsCenter + _boundsSize * 0.5f;
            float margin = 5f;
            
            foreach (var agent in _agents)
            {
                if (agent == null) continue;
                
                Vector3 pos = agent.Position;
                Vector3 avoidForce = Vector3.zero;
                
                if (pos.x < min.x + margin) avoidForce.x = 1f;
                if (pos.x > max.x - margin) avoidForce.x = -1f;
                if (pos.z < min.z + margin) avoidForce.z = 1f;
                if (pos.z > max.z - margin) avoidForce.z = -1f;
                
                if (avoidForce.sqrMagnitude > 0)
                {
                    agent.ApplyForce(avoidForce.normalized * agent.MaxForce * 0.5f);
                }
            }
        }
        
        protected override void DrawDemoControls()
        {
            bool jobsEnabled = _jobSystem != null && _jobSystem.UseJobs;
            bool jobsActive = _jobSystem != null && _jobSystem.IsUsingJobs;
            
            GUILayout.Label("Jobs/Burst Controls:", _labelStyle);
            GUILayout.Label($"• J - Toggle Jobs ({(jobsEnabled ? "<color=green>ON</color>" : "<color=red>OFF</color>")})", 
                new GUIStyle(GUI.skin.label) { richText = true });
            GUILayout.Label($"  Jobs Active: {(jobsActive ? "<color=green>Yes</color>" : "<color=yellow>No (< min agents)</color>")}",
                new GUIStyle(GUI.skin.label) { richText = true });
            
            GUILayout.Space(5);
            GUILayout.Label("Swarm Controls:", _labelStyle);
            GUILayout.Label($"• +/= - Add {_spawnBatchSize} agents");
            GUILayout.Label($"• - - Remove {_spawnBatchSize} agents");
            GUILayout.Label("• Left Click - Set target");
            GUILayout.Label("• Space - Scatter");
            GUILayout.Label("• G - Gather at center");
            
            if (!jobsActive)
            {
                GUILayout.Space(5);
                GUILayout.Label("Behavior Toggles (non-Jobs):", _labelStyle);
                GUILayout.Label($"• 1 - Separation ({(IsBehaviorActive<SeparationBehavior>() ? "ON" : "OFF")})");
                GUILayout.Label($"• 2 - Alignment ({(IsBehaviorActive<AlignmentBehavior>() ? "ON" : "OFF")})");
                GUILayout.Label($"• 3 - Cohesion ({(IsBehaviorActive<CohesionBehavior>() ? "ON" : "OFF")})");
                GUILayout.Label($"• 4 - Wander ({(IsBehaviorActive<WanderBehavior>() ? "ON" : "OFF")})");
            }
        }
        
        protected override void DrawStats()
        {
            GUILayout.Label("Performance:", _labelStyle);
            GUILayout.Label($"• FPS: {_lastFps:F0}");
            GUILayout.Label($"• Agents: {_agents.Count}");
            
            if (_jobSystem != null && _jobSystem.IsUsingJobs)
            {
                GUILayout.Label($"• Jobs Time: {_jobSystem.LastJobTimeMs:F2}ms");
            }
            
            int movingCount = 0;
            foreach (var agent in _agents)
            {
                if (agent != null && agent.Velocity.sqrMagnitude > 0.01f)
                    movingCount++;
            }
            GUILayout.Label($"• Moving: {movingCount}");
            
            if (_hasTarget)
            {
                GUILayout.Label($"• Target: ({_targetPosition.x:F0}, {_targetPosition.z:F0})");
            }
        }
        
        private void OnDrawGizmos()
        {
            // Draw bounds
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawWireCube(_boundsCenter, _boundsSize);
            
            // Draw target
            if (_hasTarget)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(_targetPosition, 2f);
            }
        }
    }
}
