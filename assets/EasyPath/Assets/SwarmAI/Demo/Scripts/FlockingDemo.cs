using UnityEngine;
using System.Collections.Generic;

namespace SwarmAI.Demo
{
    /// <summary>
    /// Demo showcasing flocking behaviors: Separation, Alignment, Cohesion, Wander, and Obstacle Avoidance.
    /// Agents flock together like birds or fish with smooth emergent behavior.
    /// Uses the new Unity Input System.
    /// </summary>
    public class FlockingDemo : SwarmDemoController
    {
        [Header("Flocking Settings")]
        [SerializeField] private float _separationWeight = 1.5f;
        [SerializeField] private float _alignmentWeight = 1.0f;
        [SerializeField] private float _cohesionWeight = 1.0f;
        [SerializeField] private float _wanderWeight = 0.3f;
        [SerializeField] private float _obstacleAvoidanceWeight = 2.0f;
        [SerializeField] private float _seekWeight = 1.5f;
        [SerializeField] private float _neighborRadius = 5f;
        
        [Header("Flock Target")]
        [SerializeField] private bool _useClickTarget = true;
        
        [Header("Bounds")]
        [SerializeField] private Vector3 _boundsCenter = new Vector3(0, 0, 0);
        [SerializeField] private Vector3 _boundsSize = new Vector3(30, 10, 30);
        
        [Header("Debug")]
        [SerializeField] private bool _verboseDebug = true;
        
        // Behaviors for each agent
        private Dictionary<SwarmAgent, List<IBehavior>> _agentBehaviors = new Dictionary<SwarmAgent, List<IBehavior>>();
        
        // Seek behaviors (one per agent) for click-to-move
        private Dictionary<SwarmAgent, SeekBehavior> _agentSeekBehaviors = new Dictionary<SwarmAgent, SeekBehavior>();
        
        // Current target position
        private Vector3 _targetPosition;
        private bool _hasTarget = false;
        
        protected override void Start()
        {
            _demoTitle = "SwarmAI - Flocking Demo";
            _targetPosition = _boundsCenter;
            
            base.Start();
            
            if (_verboseDebug)
            {
                Debug.Log($"[FlockingDemo] Start() called. Found {_agents.Count} agents in _agents list");
                foreach (var agent in _agents)
                {
                    Debug.Log($"[FlockingDemo]   - Agent: {(agent != null ? agent.name : "NULL")}");
                }
            }
            
            // Setup behaviors for all agents
            SetupAgentBehaviors();
            
            if (_verboseDebug)
            {
                Debug.Log($"[FlockingDemo] After SetupAgentBehaviors: {_agentBehaviors.Count} agents have behaviors, {_agentSeekBehaviors.Count} have seek behaviors");
            }
        }
        
        protected override void Update()
        {
            base.Update();
            
            // Ensure any new agents have behaviors set up
            EnsureAgentBehaviors();
            
            HandleFlockingInput();
            UpdateBoundaryAvoidance();
        }
        
        /// <summary>
        /// Override to also clean up behavior dictionaries when agents are destroyed.
        /// </summary>
        protected override void CleanupStaleAgents()
        {
            // First, clean up our dictionaries of any destroyed agents
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
                _agentSeekBehaviors.Remove(staleAgent);
            }
            
            // Then call base cleanup
            base.CleanupStaleAgents();
        }
        
        /// <summary>
        /// Ensure all agents in the list have behaviors set up.
        /// Handles dynamically spawned agents that weren't present at Start().
        /// </summary>
        private void EnsureAgentBehaviors()
        {
            foreach (var agent in _agents)
            {
                if (agent == null) continue;
                
                // If this agent doesn't have behaviors yet, set them up
                if (!_agentBehaviors.ContainsKey(agent))
                {
                    SetupAgentBehavior(agent);
                }
            }
        }
        
        private void HandleFlockingInput()
        {
            // D - Toggle verbose debug on all agents
            if (Input.GetKeyDown(KeyCode.D))
            {
                ToggleVerboseDebug();
            }
            
            // Left click - Set flock target
            if (_useClickTarget && SwarmDemoInput.ClickPressed && Camera.main != null)
            {
                Ray ray = Camera.main.ScreenPointToRay(SwarmDemoInput.MousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 100f))
                {
                    SetFlockTarget(hit.point);
                }
            }
            
            // 1 - Toggle Separation
            if (SwarmDemoInput.Number1Pressed)
            {
                ToggleBehavior<SeparationBehavior>();
            }
            
            // 2 - Toggle Alignment
            if (SwarmDemoInput.Number2Pressed)
            {
                ToggleBehavior<AlignmentBehavior>();
            }
            
            // 3 - Toggle Cohesion
            if (SwarmDemoInput.Number3Pressed)
            {
                ToggleBehavior<CohesionBehavior>();
            }
            
            // 4 - Toggle Wander
            if (SwarmDemoInput.Number4Pressed)
            {
                ToggleBehavior<WanderBehavior>();
            }
            
            // 5 - Toggle Obstacle Avoidance
            if (SwarmDemoInput.Number5Pressed)
            {
                ToggleBehavior<ObstacleAvoidanceBehavior>();
            }
            
            // 6 - Toggle Seek (target following)
            if (SwarmDemoInput.Number6Pressed)
            {
                ToggleBehavior<SeekBehavior>();
            }
            
            // Space - Scatter (disable cohesion, increase separation)
            if (SwarmDemoInput.SpacePressed)
            {
                ScatterFlock();
            }
            
            // G - Gather (enable cohesion, set target to center)
            if (SwarmDemoInput.ActionGPressed)
            {
                GatherFlock();
            }
        }
        
        private void SetupAgentBehaviors()
        {
            foreach (var agent in _agents)
            {
                if (agent == null) continue;
                SetupAgentBehavior(agent);
            }
        }
        
        /// <summary>
        /// Set up behaviors for a single agent. Used both at Start() and for dynamically spawned agents.
        /// </summary>
        private void SetupAgentBehavior(SwarmAgent agent)
        {
            if (agent == null || _agentBehaviors.ContainsKey(agent)) return;
            
            if (_verboseDebug)
            {
                Debug.Log($"[FlockingDemo] Setting up behaviors for agent: {agent.name}");
            }
            
            var behaviors = new List<IBehavior>();
            
            // Create and add behaviors
            var separation = new SeparationBehavior(_neighborRadius);
            var alignment = new AlignmentBehavior(_neighborRadius);
            var cohesion = new CohesionBehavior(_neighborRadius);
            var wander = new WanderBehavior();
            var obstacleAvoidance = new ObstacleAvoidanceBehavior();
            
            // Create seek behavior for click-to-move (starts inactive until target is set)
            var seek = new SeekBehavior();
            seek.IsActive = false;
            
            // If we have an active target, set it up immediately
            if (_hasTarget)
            {
                seek.TargetPosition = _targetPosition;
                seek.IsActive = true;
            }
            
            agent.AddBehavior(separation, _separationWeight);
            agent.AddBehavior(alignment, _alignmentWeight);
            agent.AddBehavior(cohesion, _cohesionWeight);
            agent.AddBehavior(wander, _wanderWeight);
            agent.AddBehavior(obstacleAvoidance, _obstacleAvoidanceWeight);
            agent.AddBehavior(seek, _seekWeight);
            
            behaviors.Add(separation);
            behaviors.Add(alignment);
            behaviors.Add(cohesion);
            behaviors.Add(wander);
            behaviors.Add(obstacleAvoidance);
            behaviors.Add(seek);
            
            _agentBehaviors[agent] = behaviors;
            _agentSeekBehaviors[agent] = seek;
            
            if (_verboseDebug)
            {
                Debug.Log($"[FlockingDemo] Agent {agent.name}: Added {behaviors.Count} behaviors. Seek active: {seek.IsActive}, target: {seek.TargetPosition}");
            }
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
            
            Debug.Log($"[FlockingDemo] Toggled {typeof(T).Name}");
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
        
        private void SetFlockTarget(Vector3 position)
        {
            _targetPosition = position;
            _hasTarget = true;
            
            // Update seek behavior target for all agents
            foreach (var agent in _agents)
            {
                if (agent == null) continue;
                
                // Update the seek behavior's target and activate it
                if (_agentSeekBehaviors.TryGetValue(agent, out var seekBehavior))
                {
                    seekBehavior.TargetPosition = position;
                    seekBehavior.IsActive = true;
                }
            }
            
            Debug.Log($"[FlockingDemo] Flock target set to {position}. Updated {_agentSeekBehaviors.Count} seek behaviors.");
            
            if (_verboseDebug)
            {
                foreach (var kvp in _agentSeekBehaviors)
                {
                    if (kvp.Key != null)
                    {
                        Debug.Log($"[FlockingDemo]   - {kvp.Key.name}: SeekBehavior.IsActive={kvp.Value.IsActive}, TargetPosition={kvp.Value.TargetPosition}");
                    }
                }
            }
        }
        
        private void ScatterFlock()
        {
            _hasTarget = false;
            
            foreach (var agent in _agents)
            {
                if (agent == null || !_agentBehaviors.ContainsKey(agent)) continue;
                
                foreach (var behavior in _agentBehaviors[agent])
                {
                    if (behavior is CohesionBehavior)
                        behavior.IsActive = false;
                    if (behavior is SeparationBehavior)
                        behavior.Weight = _separationWeight * 3f;
                    if (behavior is WanderBehavior)
                        behavior.IsActive = true;
                }
                
                // Disable seek when scattering so agents don't move toward target
                if (_agentSeekBehaviors.TryGetValue(agent, out var seekBehavior))
                {
                    seekBehavior.IsActive = false;
                }
            }
            
            Debug.Log("[FlockingDemo] Flock scattered!");
        }
        
        private void GatherFlock()
        {
            foreach (var agent in _agents)
            {
                if (agent == null || !_agentBehaviors.ContainsKey(agent)) continue;
                
                foreach (var behavior in _agentBehaviors[agent])
                {
                    if (behavior is CohesionBehavior)
                        behavior.IsActive = true;
                    if (behavior is SeparationBehavior)
                        behavior.Weight = _separationWeight;
                }
                
                // Update seek behavior to target center
                if (_agentSeekBehaviors.TryGetValue(agent, out var seekBehavior))
                {
                    seekBehavior.TargetPosition = _boundsCenter;
                    seekBehavior.IsActive = true;
                }
            }
            
            _targetPosition = _boundsCenter;
            _hasTarget = true;
            
            Debug.Log("[FlockingDemo] Flock gathering at center");
        }
        
        private void ToggleVerboseDebug()
        {
            _verboseDebug = !_verboseDebug;
            
            // Also enable/disable on all agents via reflection or serialized field
            foreach (var agent in _agents)
            {
                if (agent == null) continue;
                
                // Use reflection to set _verboseDebug on agents
                var field = typeof(SwarmAgent).GetField("_verboseDebug", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(agent, _verboseDebug);
                }
            }
            
            Debug.Log($"[FlockingDemo] Verbose debug {(_verboseDebug ? "ENABLED" : "DISABLED")} for FlockingDemo and {_agents.Count} agents");
        }
        
        private void UpdateBoundaryAvoidance()
        {
            // Keep agents within bounds by applying steering force when near edges
            foreach (var agent in _agents)
            {
                if (agent == null) continue;
                
                Vector3 pos = agent.Position;
                Vector3 avoidForce = Vector3.zero;
                
                Vector3 min = _boundsCenter - _boundsSize * 0.5f;
                Vector3 max = _boundsCenter + _boundsSize * 0.5f;
                float margin = 3f;
                
                // Check each boundary
                if (pos.x < min.x + margin) avoidForce.x = 1f;
                if (pos.x > max.x - margin) avoidForce.x = -1f;
                if (pos.z < min.z + margin) avoidForce.z = 1f;
                if (pos.z > max.z - margin) avoidForce.z = -1f;
                
                if (avoidForce.sqrMagnitude > 0)
                {
                    // Apply boundary steering through velocity adjustment
                    // The agent's behaviors will handle this through target setting
                    Vector3 safeTarget = agent.Position + avoidForce.normalized * 5f;
                    safeTarget.x = Mathf.Clamp(safeTarget.x, min.x + margin, max.x - margin);
                    safeTarget.z = Mathf.Clamp(safeTarget.z, min.z + margin, max.z - margin);
                }
            }
        }
        
        protected override void DrawDemoControls()
        {
            GUILayout.Label("Flocking Controls:", _labelStyle);
            GUILayout.Label("• Left Click - Set flock target");
            GUILayout.Label($"• 1 - Separation ({(IsBehaviorActive<SeparationBehavior>() ? "ON" : "OFF")})");
            GUILayout.Label($"• 2 - Alignment ({(IsBehaviorActive<AlignmentBehavior>() ? "ON" : "OFF")})");
            GUILayout.Label($"• 3 - Cohesion ({(IsBehaviorActive<CohesionBehavior>() ? "ON" : "OFF")})");
            GUILayout.Label($"• 4 - Wander ({(IsBehaviorActive<WanderBehavior>() ? "ON" : "OFF")})");
            GUILayout.Label($"• 5 - Obstacle Avoid ({(IsBehaviorActive<ObstacleAvoidanceBehavior>() ? "ON" : "OFF")})");
            GUILayout.Label($"• 6 - Seek Target ({(IsBehaviorActive<SeekBehavior>() ? "ON" : "OFF")})");
            GUILayout.Label("• Space - Scatter flock");
            GUILayout.Label("• G - Gather at center");
            GUILayout.Label($"• D - Toggle Debug ({(_verboseDebug ? "ON" : "OFF")})");
        }
        
        protected override void DrawStats()
        {
            base.DrawStats();
            
            if (_hasTarget)
            {
                GUILayout.Label($"• Target: ({_targetPosition.x:F1}, {_targetPosition.z:F1})");
            }
            
            // Calculate flock center
            if (_agents.Count > 0)
            {
                Vector3 center = Vector3.zero;
                int validCount = 0;
                foreach (var agent in _agents)
                {
                    if (agent != null)
                    {
                        center += agent.Position;
                        validCount++;
                    }
                }
                if (validCount > 0)
                {
                    center /= validCount;
                    GUILayout.Label($"• Flock Center: ({center.x:F1}, {center.z:F1})");
                }
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
                Gizmos.DrawWireSphere(_targetPosition, 1f);
            }
        }
    }
}
