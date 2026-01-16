using UnityEngine;
using UnityEditor;
using EasyPath;
using System.Collections.Generic;

namespace EasyPath.Editor
{
    public class EasyPathDebugWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private EasyPathGrid _selectedGrid;
        private bool _showGrid = true;
        private bool _showPaths = true;
        private bool _showAgents = true;
        
        [MenuItem("Window/EasyPath/Debug Window")]
        public static void ShowWindow()
        {
            var window = GetWindow<EasyPathDebugWindow>("EasyPath Debug");
            window.minSize = new Vector2(300, 400);
        }
        
        [MenuItem("GameObject/EasyPath/Create Grid", false, 10)]
        public static void CreateGrid()
        {
            GameObject gridObject = new GameObject("EasyPath Grid");
            gridObject.AddComponent<EasyPathGrid>();
            Selection.activeGameObject = gridObject;
            Undo.RegisterCreatedObjectUndo(gridObject, "Create EasyPath Grid");
        }
        
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
        
        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }
        
        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }
        
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
