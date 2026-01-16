using UnityEngine;
using UnityEditor;
using SwarmAI;
using System.Collections.Generic;

namespace SwarmAI.Editor
{
    public class SwarmDebugWindow : EditorWindow
    {
        private Vector2 _agentScrollPos;
        private SwarmAgent _selectedAgent;
        
        // Visualization options
        private bool _showAgentLabels = true;
        private bool _showVelocities = true;
        private bool _showNeighborRadius = false;
        private bool _showConnections = false;
        private bool _showTargets = true;
        
        // Filter
        private AgentStateType? _stateFilter = null;
        private string _searchFilter = "";
        
        // Tab selection
        private int _selectedTab = 0;
        private readonly string[] _tabNames = { "Agents", "Visualization", "Commands", "Stats" };
        
        // Cached texture to avoid memory leak
        private Texture2D _selectedRowTexture;
        
        [MenuItem("Window/SwarmAI/Debug Window")]
        public static void ShowWindow()
        {
            var window = GetWindow<SwarmDebugWindow>("SwarmAI Debug");
            window.minSize = new Vector2(350, 450);
        }
        
        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }
        
        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }
        
        private void OnDestroy()
        {
            if (_selectedRowTexture != null)
            {
                DestroyImmediate(_selectedRowTexture);
                _selectedRowTexture = null;
            }
        }
        
        private void OnGUI()
        {
            // Header
            EditorGUILayout.LabelField("SwarmAI Debug", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // Check for SwarmManager
            if (!SwarmManager.HasInstance)
            {
                EditorGUILayout.HelpBox("No SwarmManager in scene. Create one to use SwarmAI.", MessageType.Warning);
                
                if (GUILayout.Button("Create SwarmManager", GUILayout.Height(30)))
                {
                    SwarmManagerEditor.CreateManager();
                }
                return;
            }
            
            SwarmManager manager = SwarmManager.Instance;
            
            // Quick stats bar
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField($"Agents: {manager.AgentCount}", GUILayout.Width(80));
            EditorGUILayout.LabelField($"| Play: {(Application.isPlaying ? "Yes" : "No")}", GUILayout.Width(70));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // Tabs
            _selectedTab = GUILayout.Toolbar(_selectedTab, _tabNames);
            EditorGUILayout.Space();
            
            switch (_selectedTab)
            {
                case 0:
                    DrawAgentsTab(manager);
                    break;
                case 1:
                    DrawVisualizationTab();
                    break;
                case 2:
                    DrawCommandsTab(manager);
                    break;
                case 3:
                    DrawStatsTab(manager);
                    break;
            }
            
            // Repaint during play mode
            if (Application.isPlaying)
            {
                Repaint();
            }
        }
        
        private void DrawAgentsTab(SwarmManager manager)
        {
            // Search and filter
            EditorGUILayout.BeginHorizontal();
            _searchFilter = EditorGUILayout.TextField("Search", _searchFilter, GUILayout.Width(200));
            
            EditorGUILayout.LabelField("State:", GUILayout.Width(40));
            string[] stateOptions = new string[] { "All", "Idle", "Moving", "Seeking", "Fleeing", "Gathering", "Returning", "Following" };
            int stateIndex = _stateFilter.HasValue ? (int)_stateFilter.Value + 1 : 0;
            int newStateIndex = EditorGUILayout.Popup(stateIndex, stateOptions, GUILayout.Width(80));
            _stateFilter = newStateIndex == 0 ? null : (AgentStateType?)(newStateIndex - 1);
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // Agent list
            _agentScrollPos = EditorGUILayout.BeginScrollView(_agentScrollPos);
            
            if (manager.AgentCount == 0)
            {
                EditorGUILayout.HelpBox("No agents registered.", MessageType.Info);
                
                if (GUILayout.Button("Create Agent"))
                {
                    SwarmManagerEditor.CreateAgent();
                }
            }
            else
            {
                // Create snapshot to avoid iteration issues during play mode
                var agentSnapshot = new List<KeyValuePair<int, SwarmAgent>>(manager.Agents);
                foreach (var kvp in agentSnapshot)
                {
                    SwarmAgent agent = kvp.Value;
                    if (agent == null) continue;
                    
                    // Apply filters
                    if (!string.IsNullOrEmpty(_searchFilter) && 
                        !agent.name.ToLower().Contains(_searchFilter.ToLower()))
                        continue;
                    
                    if (_stateFilter.HasValue && agent.CurrentStateType != _stateFilter.Value)
                        continue;
                    
                    // Draw agent row
                    bool isSelected = _selectedAgent == agent;
                    
                    GUIStyle rowStyle = isSelected ? 
                        new GUIStyle(EditorStyles.helpBox) { normal = { background = GetSelectedRowTexture() } } :
                        EditorStyles.helpBox;
                    
                    EditorGUILayout.BeginHorizontal(rowStyle);
                    
                    // Agent info
                    EditorGUILayout.BeginVertical(GUILayout.Width(180));
                    EditorGUILayout.LabelField($"[{agent.AgentId}] {agent.name}", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"State: {agent.CurrentStateType}", EditorStyles.miniLabel);
                    if (agent.HasTarget)
                    {
                        float dist = Vector3.Distance(agent.Position, agent.TargetPosition);
                        EditorGUILayout.LabelField($"Target: {dist:F1}m away", EditorStyles.miniLabel);
                    }
                    EditorGUILayout.EndVertical();
                    
                    // Buttons
                    EditorGUILayout.BeginVertical(GUILayout.Width(60));
                    
                    if (GUILayout.Button("Select"))
                    {
                        _selectedAgent = agent;
                        Selection.activeGameObject = agent.gameObject;
                        SceneView.lastActiveSceneView?.FrameSelected();
                    }
                    
                    if (Application.isPlaying && GUILayout.Button("Stop"))
                    {
                        agent.Stop();
                        agent.SetState(new IdleState());
                    }
                    
                    EditorGUILayout.EndVertical();
                    
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.Space(2);
                }
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawVisualizationTab()
        {
            EditorGUILayout.LabelField("Scene Visualization", EditorStyles.miniBoldLabel);
            EditorGUILayout.Space();
            
            _showAgentLabels = EditorGUILayout.Toggle("Show Agent Labels", _showAgentLabels);
            _showVelocities = EditorGUILayout.Toggle("Show Velocities", _showVelocities);
            _showNeighborRadius = EditorGUILayout.Toggle("Show Neighbor Radius", _showNeighborRadius);
            _showConnections = EditorGUILayout.Toggle("Show Neighbor Connections", _showConnections);
            _showTargets = EditorGUILayout.Toggle("Show Targets", _showTargets);
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Repaint Scene View"))
            {
                SceneView.RepaintAll();
            }
        }
        
        private void DrawCommandsTab(SwarmManager manager)
        {
            EditorGUILayout.LabelField("Global Commands", EditorStyles.miniBoldLabel);
            EditorGUILayout.Space();
            
            EditorGUI.BeginDisabledGroup(!Application.isPlaying);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Stop All", GUILayout.Height(35)))
            {
                manager.StopAll();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.miniBoldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Seek Origin", GUILayout.Height(25)))
            {
                manager.SeekAll(Vector3.zero);
            }
            if (GUILayout.Button("Move to Origin", GUILayout.Height(25)))
            {
                manager.MoveAllTo(Vector3.zero);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Click in Scene view to set target for all agents.", EditorStyles.miniLabel);
            
            EditorGUI.EndDisabledGroup();
            
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Commands only work in Play mode.", MessageType.Info);
            }
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Create", EditorStyles.miniBoldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Create Agent", GUILayout.Height(25)))
            {
                SwarmManagerEditor.CreateAgent();
            }
            if (GUILayout.Button("Create Resource", GUILayout.Height(25)))
            {
                ResourceNodeEditor.CreateResourceNode();
            }
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawStatsTab(SwarmManager manager)
        {
            EditorGUILayout.LabelField("Statistics", EditorStyles.miniBoldLabel);
            EditorGUILayout.Space();
            
            EditorGUI.BeginDisabledGroup(true);
            
            EditorGUILayout.IntField("Total Agents", manager.AgentCount);
            
            if (manager.Settings != null)
            {
                EditorGUILayout.FloatField("Cell Size", manager.Settings.SpatialHashCellSize);
                EditorGUILayout.IntField("Max Messages/Frame", manager.Settings.MaxAgentsPerFrame);
            }
            
            // Count agents by state
            if (Application.isPlaying && manager.AgentCount > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Agents by State", EditorStyles.miniBoldLabel);
                
                Dictionary<AgentStateType, int> stateCounts = new Dictionary<AgentStateType, int>();
                
                // Create snapshot to avoid iteration issues during play mode
                var agentSnapshot = new List<KeyValuePair<int, SwarmAgent>>(manager.Agents);
                foreach (var kvp in agentSnapshot)
                {
                    SwarmAgent agent = kvp.Value;
                    if (agent == null) continue;
                    
                    AgentStateType state = agent.CurrentStateType;
                    if (!stateCounts.ContainsKey(state))
                        stateCounts[state] = 0;
                    stateCounts[state]++;
                }
                
                foreach (var kvp in stateCounts)
                {
                    EditorGUILayout.IntField(kvp.Key.ToString(), kvp.Value);
                }
            }
            
            // Resource nodes
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Resources", EditorStyles.miniBoldLabel);
            
            int nodeCount = ResourceNode.AllNodes?.Count ?? 0;
            EditorGUILayout.IntField("Resource Nodes", nodeCount);
            
            int depletedCount = 0;
            if (ResourceNode.AllNodes != null)
            {
                foreach (var node in ResourceNode.AllNodes)
                {
                    // Use implicit bool conversion to handle Unity's fake null for destroyed objects
                    if (node && node.IsDepleted)
                        depletedCount++;
                }
            }
            EditorGUILayout.IntField("Depleted", depletedCount);
            
            EditorGUI.EndDisabledGroup();
        }
        
        private void OnSceneGUI(SceneView sceneView)
        {
            if (!SwarmManager.HasInstance) return;
            
            SwarmManager manager = SwarmManager.Instance;
            
            // Create snapshot to avoid iteration issues during play mode
            var agentSnapshot = new List<KeyValuePair<int, SwarmAgent>>(manager.Agents);
            foreach (var kvp in agentSnapshot)
            {
                SwarmAgent agent = kvp.Value;
                if (agent == null) continue;
                
                Vector3 pos = agent.Position;
                
                // Agent labels
                if (_showAgentLabels)
                {
                    string label = $"[{agent.AgentId}] {agent.CurrentStateType}";
                    Handles.Label(pos + Vector3.up * 1.5f, label);
                }
                
                // Velocities
                if (_showVelocities && agent.Velocity.sqrMagnitude > 0.01f)
                {
                    Handles.color = Color.cyan;
                    Handles.DrawLine(pos, pos + agent.Velocity);
                }
                
                // Neighbor radius
                if (_showNeighborRadius)
                {
                    Handles.color = new Color(1f, 1f, 0f, 0.2f);
                    Handles.DrawWireDisc(pos, Vector3.up, agent.NeighborRadius);
                }
                
                // Targets
                if (_showTargets && agent.HasTarget)
                {
                    Handles.color = Color.green;
                    Handles.DrawDottedLine(pos, agent.TargetPosition, 3f);
                    Handles.DrawWireDisc(agent.TargetPosition, Vector3.up, 0.3f);
                }
                
                // Neighbor connections
                if (_showConnections && Application.isPlaying)
                {
                    Handles.color = new Color(0.5f, 0.5f, 1f, 0.3f);
                    var neighbors = agent.GetNeighbors();
                    foreach (var neighbor in neighbors)
                    {
                        if (neighbor != null)
                        {
                            Handles.DrawLine(pos, neighbor.Position);
                        }
                    }
                }
                
                // Highlight selected agent
                if (_selectedAgent == agent)
                {
                    Handles.color = Color.yellow;
                    Handles.DrawWireDisc(pos, Vector3.up, 1f);
                    Handles.DrawWireDisc(pos, Vector3.up, 1.1f);
                }
            }
        }
        
        private Texture2D GetSelectedRowTexture()
        {
            if (_selectedRowTexture == null)
            {
                _selectedRowTexture = new Texture2D(1, 1);
                _selectedRowTexture.SetPixel(0, 0, new Color(0.3f, 0.5f, 0.8f, 0.5f));
                _selectedRowTexture.Apply();
            }
            return _selectedRowTexture;
        }
    }
}
