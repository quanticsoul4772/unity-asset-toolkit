using UnityEngine;
using System.Collections.Generic;

namespace SwarmAI.Demo
{
    /// <summary>
    /// Demo showcasing flocking behaviors: Separation, Alignment, Cohesion, Wander, and Obstacle Avoidance.
    /// Agents flock together like birds or fish with smooth emergent behavior.
    /// </summary>
    public class FlockingDemo : SwarmDemoController
    {
        [Header("Flocking Settings")]
        [SerializeField] private float _separationWeight = 1.5f;
        [SerializeField] private float _alignmentWeight = 1.0f;
        [SerializeField] private float _cohesionWeight = 1.0f;
        [SerializeField] private float _wanderWeight = 0.3f;
        [SerializeField] private float _obstacleAvoidanceWeight = 2.0f;
        [SerializeField] private float _neighborRadius = 5f;
        
        [Header("Flock Target")]
        [SerializeField] private bool _useClickTarget = true;
        
        [Header("Bounds")]
        [SerializeField] private Vector3 _boundsCenter = new Vector3(0, 0, 0);
        [SerializeField] private Vector3 _boundsSize = new Vector3(30, 10, 30);
        [SerializeField] private float _boundaryAvoidanceWeight = 1.5f;
        
        // Behaviors for each agent
        private Dictionary<SwarmAgent, List<IBehavior>> _agentBehaviors = new Dictionary<SwarmAgent, List<IBehavior>>();
        
        // Current target position
        private Vector3 _targetPosition;
        private bool _hasTarget = false;
        
        protected override void Start()
        {
            _demoTitle = "SwarmAI - Flocking Demo";
            _targetPosition = _boundsCenter;
            
            base.Start();
            
            // Setup behaviors for all agents
            SetupAgentBehaviors();
        }
        
        protected override void Update()
        {
            base.Update();
            
            HandleFlockingInput();
            UpdateBoundaryAvoidance();
        }
        
        private void HandleFlockingInput()
        {
            // Left click - Set flock target
            if (_useClickTarget && Input.GetMouseButtonDown(0) && Camera.main != null)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 100f))
                {
                    SetFlockTarget(hit.point);
                }
            }
            
            // 1 - Toggle Separation
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                ToggleBehavior<SeparationBehavior>();
            }
            
            // 2 - Toggle Alignment
            if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                ToggleBehavior<AlignmentBehavior>();
            }
            
            // 3 - Toggle Cohesion
            if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                ToggleBehavior<CohesionBehavior>();
            }
            
            // 4 - Toggle Wander
            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                ToggleBehavior<WanderBehavior>();
            }
            
            // 5 - Toggle Obstacle Avoidance
            if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                ToggleBehavior<ObstacleAvoidanceBehavior>();
            }
            
            // Space - Scatter (disable cohesion, increase separation)
            if (Input.GetKeyDown(KeyCode.Space))
            {
                ScatterFlock();
            }
            
            // G - Gather (enable cohesion, set target to center)
            if (Input.GetKeyDown(KeyCode.G))
            {
                GatherFlock();
            }
        }
        
        private void SetupAgentBehaviors()
        {
            foreach (var agent in _agents)
            {
                if (agent == null) continue;
                
                var behaviors = new List<IBehavior>();
                
                // Create and add behaviors
                var separation = new SeparationBehavior(_neighborRadius);
                var alignment = new AlignmentBehavior(_neighborRadius);
                var cohesion = new CohesionBehavior(_neighborRadius);
                var wander = new WanderBehavior();
                var obstacleAvoidance = new ObstacleAvoidanceBehavior();
                
                agent.AddBehavior(separation, _separationWeight);
                agent.AddBehavior(alignment, _alignmentWeight);
                agent.AddBehavior(cohesion, _cohesionWeight);
                agent.AddBehavior(wander, _wanderWeight);
                agent.AddBehavior(obstacleAvoidance, _obstacleAvoidanceWeight);
                
                behaviors.Add(separation);
                behaviors.Add(alignment);
                behaviors.Add(cohesion);
                behaviors.Add(wander);
                behaviors.Add(obstacleAvoidance);
                
                _agentBehaviors[agent] = behaviors;
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
            
            // Add seek behavior toward target for all agents
            foreach (var agent in _agents)
            {
                if (agent == null) continue;
                agent.SetTarget(position);
            }
            
            Debug.Log($"[FlockingDemo] Flock target set to {position}");
        }
        
        private void ScatterFlock()
        {
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
                
                agent.SetTarget(_boundsCenter);
            }
            
            Debug.Log("[FlockingDemo] Flock gathering at center");
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
            GUILayout.Label("• Space - Scatter flock");
            GUILayout.Label("• G - Gather at center");
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
