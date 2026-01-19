using UnityEngine;
using UnityEditor;
using EasyPath;
using System.Collections.Generic;

namespace EasyPath.Editor
{
    /// <summary>
    /// Centralized debug window for monitoring and managing EasyPath grids and agents.
    /// </summary>
    /// <remarks>
    /// Access via: Window → EasyPath → Debug Window
    ///
    /// Features:
    /// - Grid selection and statistics display (size, walkable percentage)
    /// - Visualization toggles (grid outline, agent paths, agent markers)
    /// - Active agents list with real-time status (moving, has path, distance)
    /// - Quick creation buttons for grids and agents
    /// - Scene view overlays with agent labels and distance readouts
    /// - Auto-refresh during play mode for live monitoring
    ///
    /// Useful for debugging pathfinding issues and monitoring multiple agents simultaneously.
    /// </remarks>
    public class EasyPathDebugWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private EasyPathGrid _selectedGrid;
        private bool _showGrid = true;
        private bool _showPaths = true;
        private bool _showAgents = true;

        /// <summary>
        /// Opens the EasyPath Debug Window via menu: Window → EasyPath → Debug Window.
        /// </summary>
        /// <returns>The debug window instance.</returns>
        [MenuItem("Window/EasyPath/Debug Window")]
        public static void ShowWindow()
        {
            var window = GetWindow<EasyPathDebugWindow>("EasyPath Debug");
            window.minSize = new Vector2(300, 400);
        }

        /// <summary>
        /// Creates a new EasyPathGrid GameObject via menu: GameObject → EasyPath → Create Grid.
        /// </summary>
        /// <remarks>
        /// Sets the new grid as the active selection and registers undo.
        /// </remarks>
        [MenuItem("GameObject/EasyPath/Create Grid", false, 10)]
        public static void CreateGrid()
        {
            GameObject gridObject = new GameObject("EasyPath Grid");
            gridObject.AddComponent<EasyPathGrid>();
            Selection.activeGameObject = gridObject;
            Undo.RegisterCreatedObjectUndo(gridObject, "Create EasyPath Grid");
        }

        /// <summary>
        /// Creates a new EasyPathAgent GameObject with capsule visual via menu: GameObject → EasyPath → Create Agent.
        /// </summary>
        /// <remarks>
        /// Automatically adds a capsule primitive as a child for visualization.
        /// Sets the new agent as the active selection and registers undo.
        /// </remarks>
        [MenuItem("GameObject/EasyPath/Create Agent", false, 10)]
        public static void CreateAgent()
        {
            GameObject agentObject = new GameObject("EasyPath Agent");
            agentObject.AddComponent<EasyPathAgent>();
            
            // Add a simple visual
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.transform.SetParent(agentObject.transform);
            visual.transform.localPosition = Vector3.up;
            visual.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            
            Selection.activeGameObject = agentObject;
            Undo.RegisterCreatedObjectUndo(agentObject, "Create EasyPath Agent");
        }

        /// <summary>
        /// Subscribes to Scene GUI events for custom visualization.
        /// </summary>
        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        /// <summary>
        /// Unsubscribes from Scene GUI events.
        /// </summary>
        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        /// <summary>
        /// Renders the debug window GUI with grid selection, visualization options, and agent list.
        /// </summary>
        /// <remarks>
        /// Layout:
        /// 1. Grid Selection - Auto-finds first grid if none selected
        /// 2. Visualization - Toggles for grid outline, paths, agent markers
        /// 3. Grid Info - Size, cell count, walkable percentage
        /// 4. Active Agents - Scrollable list showing status and select buttons
        ///
        /// Auto-repaints during play mode for live updates.
        /// </remarks>
        private void OnGUI()
        {
            EditorGUILayout.LabelField("EasyPath Debug", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // Grid Selection
            EditorGUILayout.LabelField("Grid", EditorStyles.miniBoldLabel);
            _selectedGrid = (EasyPathGrid)EditorGUILayout.ObjectField(
                "Selected Grid", _selectedGrid, typeof(EasyPathGrid), true
            );
            
            if (_selectedGrid == null)
            {
                _selectedGrid = Object.FindFirstObjectByType<EasyPathGrid>();
            }
            
            EditorGUILayout.Space();
            
            // Visualization Options
            EditorGUILayout.LabelField("Visualization", EditorStyles.miniBoldLabel);
            _showGrid = EditorGUILayout.Toggle("Show Grid", _showGrid);
            _showPaths = EditorGUILayout.Toggle("Show Paths", _showPaths);
            _showAgents = EditorGUILayout.Toggle("Show Agents", _showAgents);
            
            EditorGUILayout.Space();
            
            // Grid Info
            if (_selectedGrid != null)
            {
                EditorGUILayout.LabelField("Grid Info", EditorStyles.miniBoldLabel);
                EditorGUILayout.LabelField($"Size: {_selectedGrid.Width} x {_selectedGrid.Height}");
                EditorGUILayout.LabelField($"Cell Size: {_selectedGrid.CellSize}");
                EditorGUILayout.LabelField($"Walkable Cells: {_selectedGrid.WalkableCount}");
                EditorGUILayout.LabelField($"Total Cells: {_selectedGrid.Width * _selectedGrid.Height}");
                
                float walkablePercent = (float)_selectedGrid.WalkableCount / (_selectedGrid.Width * _selectedGrid.Height) * 100f;
                EditorGUILayout.LabelField($"Walkable: {walkablePercent:F1}%");
                
                EditorGUILayout.Space();
                
                if (GUILayout.Button("Rebuild Grid"))
                {
                    _selectedGrid.BuildGrid();
                    SceneView.RepaintAll();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No EasyPathGrid found in scene.", MessageType.Info);
                
                if (GUILayout.Button("Create Grid"))
                {
                    CreateGrid();
                }
            }
            
            EditorGUILayout.Space();
            
            // Agents List
            EditorGUILayout.LabelField("Active Agents", EditorStyles.miniBoldLabel);
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            EasyPathAgent[] agents = Object.FindObjectsByType<EasyPathAgent>(FindObjectsSortMode.None);
            
            if (agents.Length == 0)
            {
                EditorGUILayout.HelpBox("No EasyPathAgents in scene.", MessageType.Info);
                
                if (GUILayout.Button("Create Agent"))
                {
                    CreateAgent();
                }
            }
            else
            {
                foreach (var agent in agents)
                {
                    EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                    
                    EditorGUILayout.BeginVertical();
                    EditorGUILayout.LabelField(agent.name, EditorStyles.boldLabel);
                    EditorGUILayout.LabelField($"Moving: {agent.IsMoving} | Has Path: {agent.HasPath}");
                    if (agent.HasPath)
                    {
                        EditorGUILayout.LabelField($"Distance: {agent.RemainingDistance:F1}m");
                    }
                    EditorGUILayout.EndVertical();
                    
                    if (GUILayout.Button("Select", GUILayout.Width(60), GUILayout.Height(40)))
                    {
                        Selection.activeGameObject = agent.gameObject;
                        SceneView.lastActiveSceneView?.FrameSelected();
                    }
                    
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.Space(2);
                }
            }
            
            EditorGUILayout.EndScrollView();
            
            // Repaint during play mode
            if (Application.isPlaying)
            {
                Repaint();
            }
        }

        /// <summary>
        /// Renders Scene view overlays for grid and agent visualization.
        /// </summary>
        /// <param name="sceneView">The active Scene view.</param>
        /// <remarks>
        /// Draws based on visualization toggles:
        /// - Grid: Outline showing grid boundaries
        /// - Agents: Green wire discs at agent positions
        /// - Paths: Cyan labels with agent name and remaining distance
        /// </remarks>
        private void OnSceneGUI(SceneView sceneView)
        {
            if (_selectedGrid == null)
            {
                return;
            }
            
            // Draw grid visualization
            if (_showGrid)
            {
                DrawGridVisualization();
            }
            
            // Draw agent paths
            if (_showPaths || _showAgents)
            {
                DrawAgentVisualization();
            }
        }

        /// <summary>
        /// Draws the grid outline in Scene view using Handles.
        /// </summary>
        /// <remarks>
        /// Draws a rectangle showing the grid boundaries based on:
        /// - Grid origin (transform position)
        /// - Width and height in cells
        /// - Cell size
        ///
        /// Uses semi-transparent gray color for non-intrusive visualization.
        /// </remarks>
        private void DrawGridVisualization()
        {
            if (_selectedGrid == null) return;
            
            Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
            
            Vector3 origin = _selectedGrid.transform.position;
            float cellSize = _selectedGrid.CellSize;
            int width = _selectedGrid.Width;
            int height = _selectedGrid.Height;
            
            // Draw grid outline
            Vector3[] corners = new Vector3[4]
            {
                origin,
                origin + new Vector3(width * cellSize, 0, 0),
                origin + new Vector3(width * cellSize, 0, height * cellSize),
                origin + new Vector3(0, 0, height * cellSize)
            };
            
            Handles.DrawLine(corners[0], corners[1]);
            Handles.DrawLine(corners[1], corners[2]);
            Handles.DrawLine(corners[2], corners[3]);
            Handles.DrawLine(corners[3], corners[0]);
        }

        /// <summary>
        /// Draws agent markers and path labels in Scene view.
        /// </summary>
        /// <remarks>
        /// For each agent:
        /// - Green wire disc at agent position (if _showAgents enabled)
        /// - Cyan label above agent showing name and remaining distance (if _showPaths enabled and agent has path)
        ///
        /// Useful for monitoring multiple agents and their pathfinding status at a glance.
        /// </remarks>
        private void DrawAgentVisualization()
        {
            EasyPathAgent[] agents = Object.FindObjectsByType<EasyPathAgent>(FindObjectsSortMode.None);
            
            foreach (var agent in agents)
            {
                if (_showAgents)
                {
                    // Draw agent marker
                    Handles.color = Color.green;
                    Handles.DrawWireDisc(agent.transform.position, Vector3.up, 0.5f);
                }
                
                if (_showPaths && agent.HasPath)
                {
                    // Path visualization is handled by agent's own Gizmos
                    Handles.color = Color.cyan;
                    Handles.Label(agent.transform.position + Vector3.up * 2f, 
                        $"{agent.name}\nDist: {agent.RemainingDistance:F1}m");
                }
            }
        }
    }
}
