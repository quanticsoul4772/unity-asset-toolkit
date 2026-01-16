using UnityEngine;
using UnityEditor;
using SwarmAI;

namespace SwarmAI.Editor
{
    [CustomEditor(typeof(ResourceNode))]
    public class ResourceNodeEditor : UnityEditor.Editor
    {
        private SerializedProperty _resourceType;
        private SerializedProperty _totalAmount;
        private SerializedProperty _harvestRate;
        private SerializedProperty _harvestRadius;
        private SerializedProperty _maxHarvesters;
        private SerializedProperty _respawns;
        private SerializedProperty _respawnTime;
        private SerializedProperty _scaleWithAmount;
        private SerializedProperty _minScale;
        private SerializedProperty _showDebugGizmos;
        private SerializedProperty _availableColor;
        private SerializedProperty _depletedColor;
        
        private bool _showResourceSettings = true;
        private bool _showVisualSettings = true;
        private bool _showDebug = true;
        private bool _showRuntimeInfo = true;
        
        private void OnEnable()
        {
            _resourceType = serializedObject.FindProperty("_resourceType");
            _totalAmount = serializedObject.FindProperty("_totalAmount");
            _harvestRate = serializedObject.FindProperty("_harvestRate");
            _harvestRadius = serializedObject.FindProperty("_harvestRadius");
            _maxHarvesters = serializedObject.FindProperty("_maxHarvesters");
            _respawns = serializedObject.FindProperty("_respawns");
            _respawnTime = serializedObject.FindProperty("_respawnTime");
            _scaleWithAmount = serializedObject.FindProperty("_scaleWithAmount");
            _minScale = serializedObject.FindProperty("_minScale");
            _showDebugGizmos = serializedObject.FindProperty("_showDebugGizmos");
            _availableColor = serializedObject.FindProperty("_availableColor");
            _depletedColor = serializedObject.FindProperty("_depletedColor");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            ResourceNode node = (ResourceNode)target;
            
            // Header
            EditorGUILayout.LabelField("SwarmAI Resource Node", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // Resource Settings
            _showResourceSettings = EditorGUILayout.Foldout(_showResourceSettings, "Resource Settings", true);
            if (_showResourceSettings)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_resourceType, new GUIContent("Resource Type"));
                EditorGUILayout.PropertyField(_totalAmount, new GUIContent("Total Amount"));
                EditorGUILayout.PropertyField(_harvestRate, new GUIContent("Harvest Rate", "Units per second"));
                EditorGUILayout.PropertyField(_harvestRadius, new GUIContent("Harvest Radius"));
                EditorGUILayout.PropertyField(_maxHarvesters, new GUIContent("Max Harvesters"));
                
                EditorGUILayout.Space();
                EditorGUILayout.PropertyField(_respawns, new GUIContent("Respawns"));
                if (_respawns.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(_respawnTime, new GUIContent("Respawn Time", "Seconds until respawn"));
                    EditorGUI.indentLevel--;
                }
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // Visual Settings
            _showVisualSettings = EditorGUILayout.Foldout(_showVisualSettings, "Visual Feedback", true);
            if (_showVisualSettings)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_scaleWithAmount, new GUIContent("Scale With Amount"));
                if (_scaleWithAmount.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(_minScale, new GUIContent("Min Scale", "Scale when depleted"));
                    EditorGUI.indentLevel--;
                }
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // Debug
            _showDebug = EditorGUILayout.Foldout(_showDebug, "Debug Visualization", true);
            if (_showDebug)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_showDebugGizmos, new GUIContent("Show Gizmos"));
                if (_showDebugGizmos.boolValue)
                {
                    EditorGUILayout.PropertyField(_availableColor, new GUIContent("Available Color"));
                    EditorGUILayout.PropertyField(_depletedColor, new GUIContent("Depleted Color"));
                }
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // Runtime Info
            _showRuntimeInfo = EditorGUILayout.Foldout(_showRuntimeInfo, "Runtime Info", true);
            if (_showRuntimeInfo)
            {
                EditorGUI.indentLevel++;
                
                // Amount progress bar
                float percent = node.AmountPercent;
                Rect progressRect = EditorGUILayout.GetControlRect(false, 20);
                EditorGUI.ProgressBar(progressRect, percent, 
                    $"{node.CurrentAmount:F0} / {node.TotalAmount:F0} ({percent * 100:F0}%)");
                
                EditorGUILayout.Space();
                
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.Toggle("Depleted", node.IsDepleted);
                EditorGUILayout.Toggle("Has Capacity", node.HasCapacity);
                EditorGUILayout.IntField("Current Harvesters", node.CurrentHarvesters);
                EditorGUI.EndDisabledGroup();
                
                // Status indicator
                EditorGUILayout.Space();
                Color statusColor = node.IsDepleted ? Color.red : 
                                   (node.HasCapacity ? Color.green : Color.yellow);
                string status = node.IsDepleted ? "DEPLETED" : 
                               (node.HasCapacity ? "AVAILABLE" : "FULL (max harvesters)");
                
                GUIStyle statusStyle = new GUIStyle(EditorStyles.boldLabel);
                statusStyle.normal.textColor = statusColor;
                EditorGUILayout.LabelField("Status:", status, statusStyle);
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // Buttons
            if (Application.isPlaying)
            {
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("Refill", GUILayout.Height(25)))
                {
                    node.Respawn();
                }
                
                if (GUILayout.Button("Deplete", GUILayout.Height(25)))
                {
                    node.Deplete();
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            serializedObject.ApplyModifiedProperties();
            
            if (Application.isPlaying)
            {
                Repaint();
            }
        }
        
        private void OnSceneGUI()
        {
            ResourceNode node = (ResourceNode)target;
            
            // Draw harvest radius
            Handles.color = node.IsDepleted ? Color.gray : Color.green;
            Handles.DrawWireDisc(node.Position, Vector3.up, node.HarvestRadius);
            
            // Draw fill indicator
            if (!node.IsDepleted)
            {
                Handles.color = new Color(0, 1, 0, 0.2f);
                float fillRadius = node.HarvestRadius * node.AmountPercent;
                Handles.DrawSolidDisc(node.Position, Vector3.up, fillRadius);
            }
            
            // Label
            string label = $"{node.ResourceType}\n{node.CurrentAmount:F0}/{node.TotalAmount:F0}";
            Handles.Label(node.Position + Vector3.up * 2f, label);
        }
        
        [MenuItem("GameObject/SwarmAI/Create Resource Node", false, 10)]
        public static void CreateResourceNode()
        {
            GameObject nodeObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            nodeObj.name = "ResourceNode";
            nodeObj.transform.localScale = new Vector3(2f, 0.5f, 2f);
            
            // Set material color
            Renderer renderer = nodeObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(0.8f, 0.6f, 0.2f); // Golden/resource color
                renderer.material = mat;
            }
            
            nodeObj.AddComponent<ResourceNode>();
            
            Selection.activeGameObject = nodeObj;
            Undo.RegisterCreatedObjectUndo(nodeObj, "Create Resource Node");
        }
    }
}
