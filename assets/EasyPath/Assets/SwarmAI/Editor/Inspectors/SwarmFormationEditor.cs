using UnityEngine;
using UnityEditor;
using SwarmAI;

namespace SwarmAI.Editor
{
    [CustomEditor(typeof(SwarmFormation))]
    public class SwarmFormationEditor : UnityEditor.Editor
    {
        private SerializedProperty _formationType;
        private SerializedProperty _spacing;
        private SerializedProperty _maxSlots;
        private SerializedProperty _autoAssignSlots;
        private SerializedProperty _formationSpeed;
        private SerializedProperty _matchLeaderRotation;
        private SerializedProperty _customOffsets;
        private SerializedProperty _showDebugGizmos;
        private SerializedProperty _slotColor;
        private SerializedProperty _occupiedSlotColor;
        
        private bool _showFormationSettings = true;
        private bool _showMovementSettings = true;
        private bool _showCustomOffsets = false;
        private bool _showDebug = true;
        private bool _showRuntimeInfo = true;
        
        private void OnEnable()
        {
            _formationType = serializedObject.FindProperty("_formationType");
            _spacing = serializedObject.FindProperty("_spacing");
            _maxSlots = serializedObject.FindProperty("_maxSlots");
            _autoAssignSlots = serializedObject.FindProperty("_autoAssignSlots");
            _formationSpeed = serializedObject.FindProperty("_formationSpeed");
            _matchLeaderRotation = serializedObject.FindProperty("_matchLeaderRotation");
            _customOffsets = serializedObject.FindProperty("_customOffsets");
            _showDebugGizmos = serializedObject.FindProperty("_showDebugGizmos");
            _slotColor = serializedObject.FindProperty("_slotColor");
            _occupiedSlotColor = serializedObject.FindProperty("_occupiedSlotColor");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            // Validate properties exist before using
            if (_formationType == null || _spacing == null)
            {
                DrawDefaultInspector();
                return;
            }
            
            SwarmFormation formation = (SwarmFormation)target;
            
            // Header
            EditorGUILayout.LabelField("SwarmAI Formation", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Formation ID: {formation.FormationId}", EditorStyles.miniLabel);
            EditorGUILayout.Space();
            
            // Formation Settings
            _showFormationSettings = EditorGUILayout.Foldout(_showFormationSettings, "Formation Settings", true);
            if (_showFormationSettings)
            {
                EditorGUI.indentLevel++;
                
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(_formationType, new GUIContent("Formation Type"));
                EditorGUILayout.PropertyField(_spacing, new GUIContent("Spacing", "Distance between agents"));
                EditorGUILayout.PropertyField(_maxSlots, new GUIContent("Max Slots"));
                EditorGUILayout.PropertyField(_autoAssignSlots, new GUIContent("Auto Assign", "Automatically assign agents to slots"));
                
                if (EditorGUI.EndChangeCheck() && Application.isPlaying)
                {
                    formation.RegenerateSlots();
                }
                
                // Formation preview buttons
                EditorGUILayout.Space();
                EditorGUILayout.BeginHorizontal();
                
                string[] formationNames = System.Enum.GetNames(typeof(FormationType));
                for (int i = 0; i < Mathf.Min(formationNames.Length, 4); i++)
                {
                    FormationType type = (FormationType)i;
                    if (GUILayout.Button(formationNames[i], GUILayout.Height(20)))
                    {
                        _formationType.enumValueIndex = i;
                        if (Application.isPlaying)
                        {
                            formation.RegenerateSlots();
                        }
                    }
                }
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                
                for (int i = 4; i < formationNames.Length - 1; i++) // Skip Custom for now
                {
                    FormationType type = (FormationType)i;
                    if (GUILayout.Button(formationNames[i], GUILayout.Height(20)))
                    {
                        _formationType.enumValueIndex = i;
                        if (Application.isPlaying)
                        {
                            formation.RegenerateSlots();
                        }
                    }
                }
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // Movement Settings
            _showMovementSettings = EditorGUILayout.Foldout(_showMovementSettings, "Movement", true);
            if (_showMovementSettings)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_formationSpeed, new GUIContent("Formation Speed"));
                EditorGUILayout.PropertyField(_matchLeaderRotation, new GUIContent("Match Leader Rotation"));
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // Custom Offsets (only show for Custom type)
            if ((FormationType)_formationType.enumValueIndex == FormationType.Custom)
            {
                _showCustomOffsets = EditorGUILayout.Foldout(_showCustomOffsets, "Custom Offsets", true);
                if (_showCustomOffsets)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(_customOffsets, true);
                    
                    if (GUILayout.Button("Apply Custom Offsets") && Application.isPlaying)
                    {
                        formation.RegenerateSlots();
                    }
                    EditorGUI.indentLevel--;
                }
                
                EditorGUILayout.Space();
            }
            
            // Debug
            _showDebug = EditorGUILayout.Foldout(_showDebug, "Debug Visualization", true);
            if (_showDebug)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_showDebugGizmos, new GUIContent("Show Gizmos"));
                if (_showDebugGizmos.boolValue)
                {
                    EditorGUILayout.PropertyField(_slotColor, new GUIContent("Empty Slot Color"));
                    EditorGUILayout.PropertyField(_occupiedSlotColor, new GUIContent("Occupied Slot Color"));
                }
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // Runtime Info
            _showRuntimeInfo = EditorGUILayout.Foldout(_showRuntimeInfo, "Runtime Info", true);
            if (_showRuntimeInfo)
            {
                EditorGUI.indentLevel++;
                EditorGUI.BeginDisabledGroup(true);
                
                EditorGUILayout.ObjectField("Leader", formation.Leader, typeof(SwarmAgent), true);
                EditorGUILayout.IntField("Agent Count", formation.AgentCount);
                EditorGUILayout.IntField("Total Slots", formation.Slots?.Count ?? 0);
                EditorGUILayout.Vector3Field("Position", formation.Position);
                
                EditorGUI.EndDisabledGroup();
                
                // Slot list
                if (Application.isPlaying && formation.Slots != null && formation.Slots.Count > 0)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Slots:", EditorStyles.miniBoldLabel);
                    
                    for (int i = 0; i < Mathf.Min(formation.Slots.Count, 10); i++)
                    {
                        var slot = formation.Slots[i];
                        string agentName = slot.IsOccupied ? slot.AssignedAgent.name : "(empty)";
                        EditorGUILayout.LabelField($"  [{i}] {agentName}");
                    }
                    
                    if (formation.Slots.Count > 10)
                    {
                        EditorGUILayout.LabelField($"  ... and {formation.Slots.Count - 10} more");
                    }
                }
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // Buttons
            if (Application.isPlaying)
            {
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("Regenerate", GUILayout.Height(25)))
                {
                    formation.RegenerateSlots();
                }
                
                if (GUILayout.Button("Clear", GUILayout.Height(25)))
                {
                    formation.ClearFormation();
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
            SwarmFormation formation = (SwarmFormation)target;
            
            if (formation.Slots == null) return;
            
            Vector3 center = formation.Position;
            Quaternion rotation = formation.Rotation;
            
            // Draw formation center handle
            Handles.color = Color.yellow;
            Handles.DrawWireDisc(center, Vector3.up, 0.5f);
            
            // Draw direction
            Handles.color = Color.blue;
            Handles.DrawLine(center, center + rotation * Vector3.forward * 2f);
            
            // Draw slot labels
            Handles.color = Color.white;
            for (int i = 0; i < formation.Slots.Count; i++)
            {
                Vector3 slotPos = formation.GetSlotWorldPosition(i);
                Handles.Label(slotPos + Vector3.up * 0.7f, $"[{i}]");
            }
        }
    }
}
