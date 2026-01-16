using UnityEngine;
using UnityEditor;
using SwarmAI;
using System.Collections.Generic;

namespace SwarmAI.Editor
{
    [CustomEditor(typeof(SwarmAgent))]
    public class SwarmAgentEditor : UnityEditor.Editor
    {
        // Movement
        private SerializedProperty _maxSpeed;
        private SerializedProperty _maxForce;
        private SerializedProperty _rotationSpeed;
        private SerializedProperty _stoppingDistance;
        private SerializedProperty _mass;
        
        // Neighbor Detection
        private SerializedProperty _neighborRadius;
        private SerializedProperty _neighborLayers;
        
        // Debug
        private SerializedProperty _showDebugGizmos;
        private SerializedProperty _velocityColor;
        private SerializedProperty _neighborColor;
        
        // Foldouts
        private bool _showMovement = true;
        private bool _showNeighbor = true;
        private bool _showDebug = true;
        private bool _showRuntimeInfo = true;
        private bool _showBehaviors = true;
        
        private void OnEnable()
        {
            _maxSpeed = serializedObject.FindProperty("_maxSpeed");
            _maxForce = serializedObject.FindProperty("_maxForce");
            _rotationSpeed = serializedObject.FindProperty("_rotationSpeed");
            _stoppingDistance = serializedObject.FindProperty("_stoppingDistance");
            _mass = serializedObject.FindProperty("_mass");
            
            _neighborRadius = serializedObject.FindProperty("_neighborRadius");
            _neighborLayers = serializedObject.FindProperty("_neighborLayers");
            
            _showDebugGizmos = serializedObject.FindProperty("_showDebugGizmos");
            _velocityColor = serializedObject.FindProperty("_velocityColor");
            _neighborColor = serializedObject.FindProperty("_neighborColor");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            SwarmAgent agent = (SwarmAgent)target;
            
            // Header
            EditorGUILayout.LabelField("SwarmAI Agent", EditorStyles.boldLabel);
            
            if (agent.IsRegistered)
            {
                EditorGUILayout.LabelField($"Agent ID: {agent.AgentId}", EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.HelpBox("Agent not registered with SwarmManager", MessageType.Info);
            }
            
            EditorGUILayout.Space();
            
            // Movement Settings
            _showMovement = EditorGUILayout.Foldout(_showMovement, "Movement", true);
            if (_showMovement)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_maxSpeed, new GUIContent("Max Speed", "Maximum movement speed"));
                EditorGUILayout.PropertyField(_maxForce, new GUIContent("Max Force", "Maximum steering force"));
                EditorGUILayout.PropertyField(_rotationSpeed, new GUIContent("Rotation Speed", "Degrees per second"));
                EditorGUILayout.PropertyField(_stoppingDistance, new GUIContent("Stopping Distance"));
                EditorGUILayout.PropertyField(_mass, new GUIContent("Mass", "Mass for physics calculations"));
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // Neighbor Detection
            _showNeighbor = EditorGUILayout.Foldout(_showNeighbor, "Neighbor Detection", true);
            if (_showNeighbor)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_neighborRadius, new GUIContent("Neighbor Radius"));
                EditorGUILayout.PropertyField(_neighborLayers, new GUIContent("Neighbor Layers"));
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
                    EditorGUILayout.PropertyField(_velocityColor, new GUIContent("Velocity Color"));
                    EditorGUILayout.PropertyField(_neighborColor, new GUIContent("Neighbor Color"));
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
                
                EditorGUILayout.EnumPopup("Current State", agent.CurrentStateType);
                EditorGUILayout.Vector3Field("Velocity", agent.Velocity);
                EditorGUILayout.FloatField("Speed", agent.Speed);
                EditorGUILayout.Toggle("Has Target", agent.HasTarget);
                
                if (agent.HasTarget)
                {
                    EditorGUILayout.Vector3Field("Target Position", agent.TargetPosition);
                    float distance = Vector3.Distance(agent.Position, agent.TargetPosition);
                    EditorGUILayout.FloatField("Distance to Target", distance);
                }
                
                if (Application.isPlaying)
                {
                    var neighbors = agent.GetNeighbors();
                    EditorGUILayout.IntField("Neighbor Count", neighbors.Count);
                }
                
                EditorGUI.EndDisabledGroup();
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // Behaviors (in play mode)
            if (Application.isPlaying)
            {
                _showBehaviors = EditorGUILayout.Foldout(_showBehaviors, "Active Behaviors", true);
                if (_showBehaviors)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.HelpBox("Behaviors are added via code at runtime.", MessageType.Info);
                    EditorGUI.indentLevel--;
                }
                
                EditorGUILayout.Space();
                
                // Control Buttons
                EditorGUILayout.LabelField("Controls", EditorStyles.miniBoldLabel);
                
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("Stop", GUILayout.Height(25)))
                {
                    agent.Stop();
                    agent.SetState(new IdleState());
                }
                
                if (GUILayout.Button("Clear Target", GUILayout.Height(25)))
                {
                    agent.ClearTarget();
                }
                
                if (GUILayout.Button("Clear Behaviors", GUILayout.Height(25)))
                {
                    agent.ClearBehaviors();
                }
                
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button("Select in Scene", GUILayout.Height(25)))
                {
                    Selection.activeGameObject = agent.gameObject;
                    SceneView.lastActiveSceneView?.FrameSelected();
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            serializedObject.ApplyModifiedProperties();
            
            // Repaint during play mode for live updates
            if (Application.isPlaying)
            {
                Repaint();
            }
        }
        
        private void OnSceneGUI()
        {
            SwarmAgent agent = (SwarmAgent)target;
            
            // Draw stopping distance
            Handles.color = Color.red;
            Handles.DrawWireDisc(agent.transform.position, Vector3.up, agent.StoppingDistance);
            
            // Draw neighbor radius
            Handles.color = new Color(1f, 1f, 0f, 0.3f);
            Handles.DrawWireDisc(agent.transform.position, Vector3.up, agent.NeighborRadius);
            
            // Draw target
            if (agent.HasTarget)
            {
                Handles.color = Color.green;
                Handles.DrawWireDisc(agent.TargetPosition, Vector3.up, 0.5f);
                Handles.DrawDottedLine(agent.transform.position, agent.TargetPosition, 2f);
                
                // Label with distance
                float distance = Vector3.Distance(agent.Position, agent.TargetPosition);
                Handles.Label(agent.TargetPosition + Vector3.up, $"Target ({distance:F1}m)");
            }
            
            // Draw velocity
            if (agent.Velocity.sqrMagnitude > 0.01f)
            {
                Handles.color = Color.cyan;
                Handles.DrawLine(agent.Position, agent.Position + agent.Velocity);
                Handles.ArrowHandleCap(0, agent.Position + agent.Velocity * 0.8f, 
                    Quaternion.LookRotation(agent.Velocity), 0.3f, EventType.Repaint);
            }
        }
    }
}
