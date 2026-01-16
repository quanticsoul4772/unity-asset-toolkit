using UnityEngine;
using UnityEditor;
using SwarmAI;

namespace SwarmAI.Editor
{
    [CustomEditor(typeof(SwarmManager))]
    public class SwarmManagerEditor : UnityEditor.Editor
    {
        private SerializedProperty _settings;
        private SerializedProperty _showDebugInfo;
        private SerializedProperty _showSpatialHash;
        private SerializedProperty _spatialHashColor;
        
        private bool _showAgentList = false;
        private Vector2 _agentScrollPos;
        
        private void OnEnable()
        {
            _settings = serializedObject.FindProperty("_settings");
            _showDebugInfo = serializedObject.FindProperty("_showDebugInfo");
            _showSpatialHash = serializedObject.FindProperty("_showSpatialHash");
            _spatialHashColor = serializedObject.FindProperty("_spatialHashColor");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            SwarmManager manager = (SwarmManager)target;
            
            // Header
            EditorGUILayout.LabelField("SwarmAI Manager", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // Settings
            EditorGUILayout.LabelField("Settings", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(_settings, new GUIContent("Swarm Settings"));
            
            if (_settings.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("No SwarmSettings assigned. Default settings will be used.", MessageType.Info);
                
                if (GUILayout.Button("Create Settings Asset"))
                {
                    CreateSettingsAsset(manager);
                }
            }
            
            EditorGUILayout.Space();
            
            // Debug Options
            EditorGUILayout.LabelField("Debug", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(_showDebugInfo, new GUIContent("Show Debug Info", "Display runtime stats in game view"));
            EditorGUILayout.PropertyField(_showSpatialHash, new GUIContent("Show Spatial Hash", "Visualize spatial partitioning cells"));
            
            if (_showSpatialHash.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_spatialHashColor, new GUIContent("Cell Color"));
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // Statistics
            EditorGUILayout.LabelField("Statistics", EditorStyles.miniBoldLabel);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.IntField("Registered Agents", manager.AgentCount);
            
            if (manager.Settings != null)
            {
                EditorGUILayout.FloatField("Cell Size", manager.Settings.SpatialHashCellSize);
                EditorGUILayout.IntField("Max Messages/Frame", manager.Settings.MaxAgentsPerFrame);
            }
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.Space();
            
            // Agent List (Play Mode)
            if (Application.isPlaying && manager.AgentCount > 0)
            {
                _showAgentList = EditorGUILayout.Foldout(_showAgentList, $"Agents ({manager.AgentCount})", true);
                
                if (_showAgentList)
                {
                    _agentScrollPos = EditorGUILayout.BeginScrollView(_agentScrollPos, GUILayout.MaxHeight(200));
                    
                    // Create snapshot to avoid iteration issues during play mode
                    var agentSnapshot = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<int, SwarmAgent>>(manager.Agents);
                    foreach (var kvp in agentSnapshot)
                    {
                        SwarmAgent agent = kvp.Value;
                        if (agent == null) continue;
                        
                        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                        
                        EditorGUILayout.LabelField($"[{agent.AgentId}] {agent.name}", GUILayout.Width(150));
                        EditorGUILayout.LabelField(agent.CurrentStateType.ToString(), GUILayout.Width(80));
                        
                        if (GUILayout.Button("Select", GUILayout.Width(50)))
                        {
                            Selection.activeGameObject = agent.gameObject;
                            SceneView.lastActiveSceneView?.FrameSelected();
                        }
                        
                        EditorGUILayout.EndHorizontal();
                    }
                    
                    EditorGUILayout.EndScrollView();
                }
            }
            
            EditorGUILayout.Space();
            
            // Control Buttons
            EditorGUILayout.LabelField("Commands", EditorStyles.miniBoldLabel);
            
            EditorGUI.BeginDisabledGroup(!Application.isPlaying);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Stop All", GUILayout.Height(30)))
            {
                manager.StopAll();
            }
            
            if (GUILayout.Button("Open Debug Window", GUILayout.Height(30)))
            {
                SwarmDebugWindow.ShowWindow();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.Space();
            
            // Create Agent Button
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Create Agent", GUILayout.Height(25)))
            {
                CreateAgent();
            }
            
            EditorGUILayout.EndHorizontal();
            
            serializedObject.ApplyModifiedProperties();
            
            // Repaint during play mode
            if (Application.isPlaying)
            {
                Repaint();
            }
        }
        
        private void CreateSettingsAsset(SwarmManager manager)
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Save Swarm Settings",
                "SwarmSettings",
                "asset",
                "Save SwarmSettings asset");
            
            if (!string.IsNullOrEmpty(path))
            {
                SwarmSettings settings = ScriptableObject.CreateInstance<SwarmSettings>();
                AssetDatabase.CreateAsset(settings, path);
                AssetDatabase.SaveAssets();
                
                _settings.objectReferenceValue = settings;
                serializedObject.ApplyModifiedProperties();
                
                EditorGUIUtility.PingObject(settings);
            }
        }
        
        [MenuItem("GameObject/SwarmAI/Create Manager", false, 10)]
        public static void CreateManager()
        {
            if (Object.FindFirstObjectByType<SwarmManager>() != null)
            {
                EditorUtility.DisplayDialog("SwarmManager Exists", 
                    "A SwarmManager already exists in the scene.", "OK");
                return;
            }
            
            GameObject managerObj = new GameObject("SwarmManager");
            managerObj.AddComponent<SwarmManager>();
            Selection.activeGameObject = managerObj;
            Undo.RegisterCreatedObjectUndo(managerObj, "Create SwarmManager");
        }
        
        [MenuItem("GameObject/SwarmAI/Create Agent", false, 10)]
        public static void CreateAgent()
        {
            GameObject agentObj = new GameObject("SwarmAgent");
            agentObj.AddComponent<SwarmAgent>();
            
            // Add a simple visual
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.SetParent(agentObj.transform);
            visual.transform.localPosition = Vector3.up * 0.5f;
            visual.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            
            // Remove collider from visual
            Object.DestroyImmediate(visual.GetComponent<Collider>());
            
            Selection.activeGameObject = agentObj;
            Undo.RegisterCreatedObjectUndo(agentObj, "Create SwarmAgent");
        }
    }
}
