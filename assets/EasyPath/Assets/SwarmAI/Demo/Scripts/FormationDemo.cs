using UnityEngine;
using System.Collections.Generic;

namespace SwarmAI.Demo
{
    /// <summary>
    /// Demo showcasing formation system: Line, Circle, Wedge, V, Box formations.
    /// Demonstrates leader-follower patterns and formation movement.
    /// Uses the new Unity Input System.
    /// </summary>
    public class FormationDemo : SwarmDemoController
    {
        [Header("Formation Settings")]
        [SerializeField] private FormationType _currentFormation = FormationType.Line;
        [SerializeField] private float _formationSpacing = 2f;
        
        [Header("Leader")]
        [SerializeField] private SwarmAgent _leader;
        [SerializeField] private float _leaderSpeed = 5f;
        [SerializeField] private Color _leaderColor = Color.yellow;
        
        [Header("Movement")]
        [SerializeField] private bool _wasdControl = true;
        
        [Header("Debug")]
        [SerializeField] private bool _verboseDebug = true;
        
        // Formation component
        private SwarmFormation _formation;
        private SwarmGroup _group;
        
        // Leader's arrive behavior for click-to-move
        private ArriveBehavior _leaderArriveBehavior;
        
        // Track if formation has been set up (to prevent duplicate behavior adding)
        private bool _formationInitialized = false;
        
        // Movement state
        private Vector3 _moveTarget;
        private bool _hasTarget = false;
        
        // Debug throttling
        private float _lastDebugLogTime;
        private const float DebugLogInterval = 1f;
        
        protected override void Start()
        {
            _demoTitle = "SwarmAI - Formation Demo";
            
            base.Start();
            
            if (_verboseDebug)
            {
                Debug.Log($"[FormationDemo] Start() called. Agents found: {_agents.Count}");
            }
            
            SetupFormation();
        }
        
        protected override void Update()
        {
            base.Update();
            
            HandleFormationInput();
            
            if (_wasdControl)
            {
                HandleLeaderMovement();
            }
        }
        
        private void SetupFormation()
        {
            if (_agents.Count == 0)
            {
                Debug.LogWarning("[FormationDemo] SetupFormation() called but no agents found! Make sure agents exist in the scene.");
                return;
            }
            
            // Prevent duplicate setup
            if (_formationInitialized)
            {
                if (_verboseDebug)
                {
                    Debug.Log("[FormationDemo] Formation already initialized, skipping setup");
                }
                return;
            }
            
            if (_verboseDebug)
            {
                Debug.Log($"[FormationDemo] Setting up formation with {_agents.Count} agents");
            }
            
            // Designate first agent as leader
            _leader = _agents[0];
            SetLeaderVisual(_leader);
            
            // Clear any existing behaviors on leader and add arrive behavior for click-to-move
            _leader.ClearBehaviors();
            _leaderArriveBehavior = new ArriveBehavior();
            _leaderArriveBehavior.SlowingRadius = 3f;
            _leaderArriveBehavior.ArrivalRadius = 0.5f;
            _leaderArriveBehavior.IsActive = false; // Start inactive, activated on click
            _leader.AddBehavior(_leaderArriveBehavior, 1.0f);
            
            if (_verboseDebug)
            {
                Debug.Log($"[FormationDemo] Leader assigned: {_leader.name} with ArriveBehavior");
            }
            
            // Create formation on leader
            _formation = _leader.gameObject.AddComponent<SwarmFormation>();
            _formation.Type = _currentFormation;
            _formation.Spacing = _formationSpacing;
            _formation.Leader = _leader;
            
            // Create group
            _group = new SwarmGroup(_leader, "DemoSquad");
            _group.SetFormation(_formation);
            
            // Add followers to group (skip leader)
            int followerCount = 0;
            for (int i = 1; i < _agents.Count; i++)
            {
                var agent = _agents[i];
                if (agent == null) continue;
                
                _group.AddMember(agent);
                
                // Clear any existing behaviors to avoid duplication
                agent.ClearBehaviors();
                
                // Add FormationSlotBehavior - this reads from agent.Target which is
                // set by SwarmFormation.UpdateSlotPositions() every frame
                // Using larger arrival radius and damping for stable formations
                var slotBehavior = new FormationSlotBehavior(
                    slowingRadius: 2f,
                    arrivalRadius: 0.5f,
                    dampingFactor: 0.6f
                );
                agent.AddBehavior(slotBehavior, 1.0f);
                
                // Light separation only to prevent exact overlap during transitions
                // (weight is low so it doesn't overpower formation slots)
                agent.AddBehavior(new SeparationBehavior(_formationSpacing * 0.3f), 0.3f);
                
                followerCount++;
            }
            
            _formationInitialized = true;
            Debug.Log($"[FormationDemo] Created {_currentFormation} formation with 1 leader + {followerCount} followers using FormationSlotBehavior");
        }
        
        private void SetLeaderVisual(SwarmAgent leader)
        {
            if (leader == null) return;
            
            // Try to change leader's visual color
            var renderers = leader.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                if (renderer.material != null)
                {
                    renderer.material.color = _leaderColor;
                }
            }
        }
        
        private void HandleFormationInput()
        {
            // Formation type selection
            if (SwarmDemoInput.Number1Pressed)
            {
                SetFormationType(FormationType.Line);
            }
            if (SwarmDemoInput.Number2Pressed)
            {
                SetFormationType(FormationType.Column);
            }
            if (SwarmDemoInput.Number3Pressed)
            {
                SetFormationType(FormationType.Circle);
            }
            if (SwarmDemoInput.Number4Pressed)
            {
                SetFormationType(FormationType.Wedge);
            }
            if (SwarmDemoInput.Number5Pressed)
            {
                SetFormationType(FormationType.V);
            }
            if (SwarmDemoInput.Number6Pressed)
            {
                SetFormationType(FormationType.Box);
            }
            
            // Spacing adjustments
            if (SwarmDemoInput.PlusPressed)
            {
                AdjustSpacing(0.5f);
            }
            if (SwarmDemoInput.MinusPressed)
            {
                AdjustSpacing(-0.5f);
            }
            
            // Click to move formation
            if (SwarmDemoInput.ClickPressed && Camera.main != null)
            {
                Ray ray = Camera.main.ScreenPointToRay(SwarmDemoInput.MousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 100f))
                {
                    MoveFormationTo(hit.point);
                }
            }
            
            // F - Follow leader toggle
            if (SwarmDemoInput.ActionFPressed)
            {
                _group?.FollowLeader();
                Debug.Log("[FormationDemo] Followers following leader");
            }
            
            // X - Stop formation
            if (SwarmDemoInput.ActionXPressed)
            {
                _group?.Stop();
                Debug.Log("[FormationDemo] Formation stopped");
            }
        }
        
        private void HandleLeaderMovement()
        {
            if (_leader == null)
            {
                bool shouldLog = _verboseDebug && (Time.time - _lastDebugLogTime >= DebugLogInterval);
                if (shouldLog)
                {
                    Debug.LogWarning("[FormationDemo] HandleLeaderMovement: No leader assigned!");
                    _lastDebugLogTime = Time.time;
                }
                return;
            }
            
            // Get movement from WASD via Input System
            Vector2 moveInput = SwarmDemoInput.Movement;
            Vector3 moveDir = new Vector3(moveInput.x, 0f, moveInput.y);
            
            if (moveDir.sqrMagnitude > 0.01f)
            {
                moveDir.Normalize();
                Vector3 newPos = _leader.Position + moveDir * _leaderSpeed * Time.deltaTime;
                _leader.transform.position = newPos;
                
                // Face movement direction
                _leader.transform.rotation = Quaternion.LookRotation(moveDir);
                
                // Disable arrive behavior during manual control
                if (_leaderArriveBehavior != null)
                {
                    _leaderArriveBehavior.IsActive = false;
                }
                _hasTarget = false;
                
                bool shouldLog = _verboseDebug && (Time.time - _lastDebugLogTime >= DebugLogInterval);
                if (shouldLog)
                {
                    Debug.Log($"[FormationDemo] Leader moving: input={moveInput}, pos={newPos}");
                    _lastDebugLogTime = Time.time;
                }
            }
        }
        
        private void SetFormationType(FormationType type)
        {
            _currentFormation = type;
            
            if (_formation != null)
            {
                _formation.Type = type;
                Debug.Log($"[FormationDemo] Formation changed to {type}");
            }
            else
            {
                Debug.LogWarning($"[FormationDemo] Cannot change formation - formation component is null!");
            }
        }
        
        private void AdjustSpacing(float delta)
        {
            _formationSpacing = Mathf.Max(1f, _formationSpacing + delta);
            
            if (_formation != null)
            {
                _formation.Spacing = _formationSpacing;
            }
            
            Debug.Log($"[FormationDemo] Spacing adjusted to {_formationSpacing:F1}");
        }
        
        private void MoveFormationTo(Vector3 position)
        {
            _moveTarget = position;
            _hasTarget = true;
            
            if (_leader != null && _leaderArriveBehavior != null)
            {
                // Update arrive behavior target and activate it
                _leaderArriveBehavior.TargetPosition = position;
                _leaderArriveBehavior.IsActive = true;
                
                // Also update the formation if it exists
                if (_formation != null)
                {
                    _formation.MoveTo(position);
                }
                
                Debug.Log($"[FormationDemo] Moving formation to ({position.x:F1}, {position.z:F1})");
            }
            else if (_leader != null)
            {
                _leader.SetTarget(position);
                _leader.SetState(new SeekingState(position));
                Debug.Log($"[FormationDemo] Moving leader to ({position.x:F1}, {position.z:F1}) - no arrive behavior");
            }
            else
            {
                Debug.LogWarning("[FormationDemo] Cannot move - no leader!");
            }
        }
        
        protected override void DrawDemoControls()
        {
            GUILayout.Label("Formation Controls:", _labelStyle);
            GUILayout.Label("• WASD - Move leader");
            GUILayout.Label("• Left Click - Move formation to point");
            GUILayout.Space(5);
            GUILayout.Label($"• 1 - Line {(_currentFormation == FormationType.Line ? "[ACTIVE]" : "")}");
            GUILayout.Label($"• 2 - Column {(_currentFormation == FormationType.Column ? "[ACTIVE]" : "")}");
            GUILayout.Label($"• 3 - Circle {(_currentFormation == FormationType.Circle ? "[ACTIVE]" : "")}");
            GUILayout.Label($"• 4 - Wedge {(_currentFormation == FormationType.Wedge ? "[ACTIVE]" : "")}");
            GUILayout.Label($"• 5 - V {(_currentFormation == FormationType.V ? "[ACTIVE]" : "")}");
            GUILayout.Label($"• 6 - Box {(_currentFormation == FormationType.Box ? "[ACTIVE]" : "")}");
            GUILayout.Space(5);
            GUILayout.Label($"• +/- Adjust spacing ({_formationSpacing:F1})");
            GUILayout.Label("• F - Follow leader");
            GUILayout.Label("• X - Stop all");
        }
        
        protected override void DrawStats()
        {
            base.DrawStats();
            
            GUILayout.Label($"• Formation: {_currentFormation}");
            GUILayout.Label($"• Spacing: {_formationSpacing:F1}");
            
            if (_formation != null)
            {
                GUILayout.Label($"• In Formation: {_formation.AgentCount}");
            }
            
            if (_hasTarget)
            {
                GUILayout.Label($"• Target: ({_moveTarget.x:F1}, {_moveTarget.z:F1})");
            }
        }
        
        /// <summary>
        /// Override to clean up formation references when agents are destroyed.
        /// </summary>
        protected override void CleanupStaleAgents()
        {
            base.CleanupStaleAgents();
            
            // Check if leader was destroyed
            if (_leader == null && _formationInitialized)
            {
                Debug.LogWarning("[FormationDemo] Leader was destroyed! Formation needs to be re-established.");
                
                // Clean up formation component (it was on the leader)
                _formation = null;
                _group = null;
                _leaderArriveBehavior = null;
                _formationInitialized = false;
                
                // Try to re-establish formation with remaining agents
                if (_agents.Count > 0)
                {
                    SetupFormation();
                }
            }
        }
        
        private void OnDrawGizmos()
        {
            // Draw target
            if (_hasTarget)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(_moveTarget, 1f);
                Gizmos.DrawLine(_moveTarget, _moveTarget + Vector3.up * 3f);
            }
            
            // Draw leader indicator
            if (_leader != null)
            {
                Gizmos.color = _leaderColor;
                Gizmos.DrawWireSphere(_leader.Position + Vector3.up * 2f, 0.5f);
            }
        }
    }
}
