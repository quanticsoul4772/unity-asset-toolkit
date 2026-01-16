using UnityEngine;
using UnityEditor;
using SwarmAI;

namespace SwarmAI.Editor
{
    [CustomEditor(typeof(SwarmSettings))]
    public class SwarmSettingsEditor : UnityEditor.Editor
    {
        // Foldout states
        private bool _showSpatial = true;
        private bool _showAgentDefaults = true;
        private bool _showBehaviorWeights = true;
        private bool _showWander = true;
        private bool _showObstacle = true;
        private bool _showArrive = true;
        private bool _showFormation = true;
        private bool _showFollowLeader = true;
        private bool _showResourceGathering = true;
        private bool _showPerformance = true;
        private bool _showStateMachine = true;
        private bool _showDebug = true;
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            // Header
            EditorGUILayout.LabelField("SwarmAI Settings", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Global configuration for the SwarmAI system. Assign to a SwarmManager.", MessageType.Info);
            EditorGUILayout.Space();
            
            // Spatial Partitioning
            _showSpatial = DrawSection("Spatial Partitioning", _showSpatial, () =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_spatialHashCellSize"));
            });
            
            // Agent Defaults
            _showAgentDefaults = DrawSection("Agent Defaults", _showAgentDefaults, () =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_defaultSpeed"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_defaultRotationSpeed"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_defaultMaxForce"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_defaultNeighborRadius"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_defaultStoppingDistance"));
            });
            
            // Behavior Weights
            _showBehaviorWeights = DrawSection("Behavior Weights", _showBehaviorWeights, () =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_separationWeight"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_alignmentWeight"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_cohesionWeight"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_followLeaderWeight"));
            });
            
            // Wander Behavior
            _showWander = DrawSection("Wander Behavior", _showWander, () =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_wanderRadius"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_wanderDistance"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_wanderJitter"));
            });
            
            // Obstacle Avoidance
            _showObstacle = DrawSection("Obstacle Avoidance", _showObstacle, () =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_obstacleDetectionDistance"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_obstacleWhiskerAngle"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_obstacleRayCount"));
            });
            
            // Arrive Behavior
            _showArrive = DrawSection("Arrive Behavior", _showArrive, () =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_arriveSlowingRadius"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_arriveArrivalRadius"));
            });
            
            // Formation Settings
            _showFormation = DrawSection("Formation Settings", _showFormation, () =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_formationSpacing"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_formationMoveSpeed"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_formationArrivalRadius"));
            });
            
            // Follow Leader
            _showFollowLeader = DrawSection("Follow Leader", _showFollowLeader, () =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_defaultFollowDistance"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_followSlowingRadius"));
            });
            
            // Resource Gathering
            _showResourceGathering = DrawSection("Resource Gathering", _showResourceGathering, () =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_defaultCarryCapacity"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_depositRadius"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_resourceSearchDelay"));
            });
            
            // Performance
            _showPerformance = DrawSection("Performance", _showPerformance, () =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_maxAgentsPerFrame"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_spatialHashUpdateInterval"));
            });
            
            // State Machine
            _showStateMachine = DrawSection("State Machine", _showStateMachine, () =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_stuckThreshold"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_stuckTimeLimit"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_defaultFleeDistance"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_fleeSpeedMultiplier"));
            });
            
            // Debug
            _showDebug = DrawSection("Debug", _showDebug, () =>
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_enableDebugVisualization"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_showNeighborConnections"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_showVelocityVectors"));
            });
            
            serializedObject.ApplyModifiedProperties();
        }
        
        private bool DrawSection(string title, bool foldout, System.Action drawContent)
        {
            foldout = EditorGUILayout.Foldout(foldout, title, true, EditorStyles.foldoutHeader);
            if (foldout)
            {
                EditorGUI.indentLevel++;
                drawContent();
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space();
            return foldout;
        }
    }
}
