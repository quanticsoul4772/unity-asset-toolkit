using UnityEngine;
using System.Collections.Generic;

namespace SwarmAI.Demo
{
    /// <summary>
    /// Formation types for the combat demo.
    /// </summary>
    public enum CombatFormationType
    {
        Line,
        Column,
        Circle,
        Wedge,
        V,
        Box
    }
    /// <summary>
    /// Demo controller for combat formations featuring two opposing teams.
    /// Demonstrates formations, team coordination, and combat behaviors.
    /// </summary>
    public class CombatFormationsDemo : SwarmDemoController
    {
        [Header("Team Settings")]
        [SerializeField] private int _agentsPerTeam = 5;
        [SerializeField] private float _teamSpacing = 15f;
        [SerializeField] private Color _team1Color = new Color(0.2f, 0.4f, 1f); // Blue
        [SerializeField] private Color _team2Color = new Color(1f, 0.3f, 0.2f); // Red
        
        [Header("Formation Settings")]
        [SerializeField] private float _formationSpacing = 1.5f;
        [SerializeField] private CombatFormationType _currentFormation = CombatFormationType.Line;
        
        [Header("Combat Settings")]
        [SerializeField] private float _attackRange = 3f;
        [SerializeField] private float _retreatDistance = 8f;
        
        [Header("Debug")]
        [SerializeField] private bool _verboseDebug = true;
        
        // Team management
        private List<SwarmAgent> _team1 = new List<SwarmAgent>();
        private List<SwarmAgent> _team2 = new List<SwarmAgent>();
        private SwarmFormation _team1Formation;
        private SwarmFormation _team2Formation;
        private SwarmGroup _team1Group;
        private SwarmGroup _team2Group;
        
        // State
        private int _selectedTeam = 1; // 1 or 2
        private Vector3 _boundsCenter = Vector3.zero;
        
        protected override void Start()
        {
            base.Start();
            
            if (_verboseDebug)
            {
                Debug.Log($"[CombatFormationsDemo] Start() called. Agents found: {_agents.Count}");
            }
            
            // Calculate bounds center from camera or default
            _boundsCenter = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            _boundsCenter.y = 0;
            
            SplitIntoTeams();
            SetupTeamFormations();
            SetupTeamBehaviors();
        }
        
        protected override void Update()
        {
            base.Update();
            HandleCombatInput();
            UpdateFormations();
        }
        
        private void SplitIntoTeams()
        {
            _team1.Clear();
            _team2.Clear();
            
            // Split agents into two teams
            for (int i = 0; i < _agents.Count; i++)
            {
                if (i < _agents.Count / 2)
                {
                    _team1.Add(_agents[i]);
                }
                else
                {
                    _team2.Add(_agents[i]);
                }
            }
            
            // Color the teams
            foreach (var agent in _team1)
            {
                ColorAgent(agent, _team1Color);
            }
            foreach (var agent in _team2)
            {
                ColorAgent(agent, _team2Color);
            }
            
            if (_verboseDebug)
            {
                Debug.Log($"[CombatFormationsDemo] Team 1: {_team1.Count} agents, Team 2: {_team2.Count} agents");
            }
        }
        
        private void ColorAgent(SwarmAgent agent, Color color)
        {
            var renderers = agent.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                if (renderer.material != null)
                {
                    renderer.material.color = color;
                }
            }
        }
        
        private void SetupTeamFormations()
        {
            // Create formation for team 1
            if (_team1.Count > 0)
            {
                var team1Leader = _team1[0];
                _team1Formation = team1Leader.gameObject.AddComponent<SwarmFormation>();
                _team1Formation.Spacing = _formationSpacing;
                
                // Position team 1 on the left
                Vector3 team1Center = _boundsCenter - Vector3.right * _teamSpacing / 2f;
                team1Leader.transform.position = team1Center;
                
                // Add followers
                for (int i = 1; i < _team1.Count; i++)
                {
                    _team1Formation.AddAgent(_team1[i]);
                }
                
                // Create group
                _team1Group = new SwarmGroup(team1Leader, "Team1");
                foreach (var agent in _team1)
                {
                    if (agent != team1Leader)
                    {
                        _team1Group.AddMember(agent);
                    }
                }
            }
            
            // Create formation for team 2
            if (_team2.Count > 0)
            {
                var team2Leader = _team2[0];
                _team2Formation = team2Leader.gameObject.AddComponent<SwarmFormation>();
                _team2Formation.Spacing = _formationSpacing;
                
                // Position team 2 on the right
                Vector3 team2Center = _boundsCenter + Vector3.right * _teamSpacing / 2f;
                team2Leader.transform.position = team2Center;
                
                // Add followers (and rotate to face team 1)
                team2Leader.transform.rotation = Quaternion.Euler(0, 180, 0);
                for (int i = 1; i < _team2.Count; i++)
                {
                    _team2Formation.AddAgent(_team2[i]);
                }
                
                // Create group
                _team2Group = new SwarmGroup(team2Leader, "Team2");
                foreach (var agent in _team2)
                {
                    if (agent != team2Leader)
                    {
                        _team2Group.AddMember(agent);
                    }
                }
            }
            
            if (_verboseDebug)
            {
                Debug.Log($"[CombatFormationsDemo] Formations created: {_currentFormation}");
            }
        }
        
        private void SetupTeamBehaviors()
        {
            // Set up behaviors for all agents
            foreach (var agent in _agents)
            {
                agent.ClearBehaviors();
                
                // Add formation slot behavior for followers
                var formationBehavior = new FormationSlotBehavior(3f, 0.7f, 0.5f);
                agent.AddBehavior(formationBehavior, 1.5f);
                
                // Add separation to avoid crowding
                var separationBehavior = new SeparationBehavior(1.5f);
                agent.AddBehavior(separationBehavior, 0.5f);
            }
        }
        
        private void HandleCombatInput()
        {
            // Tab - Switch selected team
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                _selectedTeam = _selectedTeam == 1 ? 2 : 1;
                Debug.Log($"[CombatFormationsDemo] Selected Team {_selectedTeam}");
            }
            
            // Formation keys 1-6
            if (SwarmDemoInput.Number1Pressed) SetFormation(CombatFormationType.Line);
            if (SwarmDemoInput.Number2Pressed) SetFormation(CombatFormationType.Column);
            if (SwarmDemoInput.Number3Pressed) SetFormation(CombatFormationType.Circle);
            if (SwarmDemoInput.Number4Pressed) SetFormation(CombatFormationType.Wedge);
            if (SwarmDemoInput.Number5Pressed) SetFormation(CombatFormationType.V);
            if (SwarmDemoInput.Number6Pressed) SetFormation(CombatFormationType.Box);
            
            // A - Attack (move toward enemy)
            if (Input.GetKeyDown(KeyCode.A))
            {
                Attack();
            }
            
            // R - Retreat
            if (Input.GetKeyDown(KeyCode.R))
            {
                Retreat();
            }
            
            // H - Hold position
            if (Input.GetKeyDown(KeyCode.H))
            {
                HoldPosition();
            }
            
            // Click to move selected team
            if (SwarmDemoInput.ClickPressed)
            {
                HandleClick();
            }
        }
        
        private void HandleClick()
        {
            Ray ray = Camera.main.ScreenPointToRay(SwarmDemoInput.MousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
            {
                MoveSelectedTeam(hit.point);
            }
        }
        
        private void MoveSelectedTeam(Vector3 position)
        {
            SwarmFormation formation = _selectedTeam == 1 ? _team1Formation : _team2Formation;
            List<SwarmAgent> team = _selectedTeam == 1 ? _team1 : _team2;
            
            if (formation != null && team.Count > 0)
            {
                // Move the leader
                var leader = team[0];
                leader.SetTarget(position);
                leader.SetState(new SeekingState(position));
                
                if (_verboseDebug)
                {
                    Debug.Log($"[CombatFormationsDemo] Moving Team {_selectedTeam} to {position}");
                }
            }
        }
        
        private void SetFormation(CombatFormationType type)
        {
            _currentFormation = type;
            Debug.Log($"[CombatFormationsDemo] Team {_selectedTeam} formation: {type}");
        }
        
        private void Attack()
        {
            _isAttacking = true;
            
            // Get enemy team center
            Vector3 enemyCenter = GetTeamCenter(_selectedTeam == 1 ? _team2 : _team1);
            MoveSelectedTeam(enemyCenter);
            
            Debug.Log($"[CombatFormationsDemo] Team {_selectedTeam} attacking!");
        }
        
        private void Retreat()
        {
            _isAttacking = false;
            
            // Retreat to starting position
            Vector3 retreatPos = _selectedTeam == 1 
                ? _boundsCenter - Vector3.right * _teamSpacing / 2f
                : _boundsCenter + Vector3.right * _teamSpacing / 2f;
            
            MoveSelectedTeam(retreatPos);
            
            Debug.Log($"[CombatFormationsDemo] Team {_selectedTeam} retreating!");
        }
        
        private void HoldPosition()
        {
            List<SwarmAgent> team = _selectedTeam == 1 ? _team1 : _team2;
            foreach (var agent in team)
            {
                agent.SetState(new IdleState());
            }
            
            Debug.Log($"[CombatFormationsDemo] Team {_selectedTeam} holding position");
        }
        
        private Vector3 GetTeamCenter(List<SwarmAgent> team)
        {
            if (team.Count == 0) return Vector3.zero;
            
            Vector3 sum = Vector3.zero;
            foreach (var agent in team)
            {
                sum += agent.Position;
            }
            return sum / team.Count;
        }
        
        private void UpdateFormations()
        {
            // Formation slot positions are updated automatically by SwarmFormation
        }
        
        protected override void OnGUI()
        {
            base.OnGUI();
            
            // Draw combat controls
            GUILayout.BeginArea(new Rect(10, 300, 280, 220));
            GUILayout.BeginVertical(GUI.skin.box);
            
            GUILayout.Label("Combat Formations Demo", GetHeaderStyle());
            GUILayout.Space(5);
            
            // Team selection
            GUI.color = _selectedTeam == 1 ? _team1Color : _team2Color;
            GUILayout.Label($"Selected: Team {_selectedTeam}");
            GUI.color = Color.white;
            
            GUILayout.Space(5);
            GUILayout.Label("Controls:");
            GUILayout.Label("• Tab - Switch team");
            GUILayout.Label("• 1-6 - Change formation");
            GUILayout.Label("• Click - Move team");
            GUILayout.Label("• A - Attack enemy");
            GUILayout.Label("• R - Retreat");
            GUILayout.Label("• H - Hold position");
            
            GUILayout.Space(5);
            GUILayout.Label($"Formation: {_currentFormation}");
            GUILayout.Label($"Team 1: {_team1.Count} | Team 2: {_team2.Count}");
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        
        private new GUIStyle _headerStyle;
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
