using UnityEngine;
using UnityEditor;
using EasyPath;

namespace EasyPath.Editor
{
    [CustomEditor(typeof(EasyPathAgent))]
    public class EasyPathAgentEditor : UnityEditor.Editor
    {
        private SerializedProperty _speed;
        private SerializedProperty _rotationSpeed;
        private SerializedProperty _stoppingDistance;
        private SerializedProperty _waypointTolerance;
        private SerializedProperty _showDebugPath;
        private SerializedProperty _pathColor;
        
        private void OnEnable()
        {
            _speed = serializedObject.FindProperty("_speed");
            _rotationSpeed = serializedObject.FindProperty("_rotationSpeed");
            _stoppingDistance = serializedObject.FindProperty("_stoppingDistance");
            _waypointTolerance = serializedObject.FindProperty("_waypointTolerance");
            _showDebugPath = serializedObject.FindProperty("_showDebugPath");
            _pathColor = serializedObject.FindProperty("_pathColor");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EasyPathAgent agent = (EasyPathAgent)target;
            
            // Header
            EditorGUILayout.LabelField("EasyPath Agent", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // Movement Settings
            EditorGUILayout.LabelField("Movement", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(_speed, new GUIContent("Speed", "Movement speed in units/second"));
            EditorGUILayout.PropertyField(_rotationSpeed, new GUIContent("Rotation Speed", "Rotation speed in degrees/second"));
            EditorGUILayout.PropertyField(_stoppingDistance, new GUIContent("Stopping Distance", "Stop this far from destination"));
            EditorGUILayout.PropertyField(_waypointTolerance, new GUIContent("Waypoint Tolerance", "Distance to consider a waypoint reached"));
            EditorGUILayout.Space();
            
            // Debug
            EditorGUILayout.LabelField("Debug", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(_showDebugPath, new GUIContent("Show Path", "Display the current path in Scene view"));
            
            if (_showDebugPath.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_pathColor, new GUIContent("Path Color"));
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // Runtime Info
            EditorGUILayout.LabelField("Runtime Info", EditorStyles.miniBoldLabel);
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.Toggle("Is Moving", agent.IsMoving);
            EditorGUILayout.Toggle("Has Path", agent.HasPath);
            EditorGUILayout.FloatField("Remaining Distance", agent.RemainingDistance);
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.Space();
            
            // Buttons (only in play mode)
            if (Application.isPlaying)
            {
                EditorGUILayout.BeginHorizontal();
                
                if (agent.IsMoving)
                {
                    if (GUILayout.Button("Pause", GUILayout.Height(25)))
                    {
                        agent.Pause();
                    }
                }
                else if (agent.HasPath)
                {
                    if (GUILayout.Button("Resume", GUILayout.Height(25)))
                    {
                        agent.Resume();
                    }
                }
                
                if (GUILayout.Button("Stop", GUILayout.Height(25)))
                {
                    agent.Stop();
                }
                
                if (GUILayout.Button("Recalculate", GUILayout.Height(25)))
                {
                    agent.RecalculatePath();
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
            EasyPathAgent agent = (EasyPathAgent)target;
            
            // Draw stopping distance
            Handles.color = Color.yellow;
            Handles.DrawWireDisc(agent.transform.position, Vector3.up, agent.StoppingDistance);
            
            // Draw destination marker
            if (agent.HasPath)
            {
                Handles.color = Color.green;
                Handles.DrawWireDisc(agent.Destination, Vector3.up, 0.5f);
                Handles.DrawLine(agent.transform.position, agent.Destination);
            }
        }
    }
}
