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
                DrawPropertyIfExists("_spatialHashCellSize");
            });
            
            // Agent Defaults
            _showAgentDefaults = DrawSection("Agent Defaults", _showAgentDefaults, () =>
            {
                DrawPropertyIfExists("_defaultSpeed");
                DrawPropertyIfExists("_defaultRotationSpeed");
                DrawPropertyIfExists("_defaultMaxForce");
                DrawPropertyIfExists("_defaultNeighborRadius");
                DrawPropertyIfExists("_defaultStoppingDistance");
            });
            
            // Behavior Weights
            _showBehaviorWeights = DrawSection("Behavior Weights", _showBehaviorWeights, () =>
            {
                DrawPropertyIfExists("_separationWeight");
                DrawPropertyIfExists("_alignmentWeight");
                DrawPropertyIfExists("_cohesionWeight");
                DrawPropertyIfExists("_followLeaderWeight");
            });
            
            // Wander Behavior
            _showWander = DrawSection("Wander Behavior", _showWander, () =>
            {
                DrawPropertyIfExists("_wanderRadius");
                DrawPropertyIfExists("_wanderDistance");
                DrawPropertyIfExists("_wanderJitter");
            });
            
            // Obstacle Avoidance
            _showObstacle = DrawSection("Obstacle Avoidance", _showObstacle, () =>
            {
                DrawPropertyIfExists("_obstacleDetectionDistance");
                DrawPropertyIfExists("_obstacleWhiskerAngle");
                DrawPropertyIfExists("_obstacleRayCount");
            });
            
            // Arrive Behavior
            _showArrive = DrawSection("Arrive Behavior", _showArrive, () =>
            {
                DrawPropertyIfExists("_arriveSlowingRadius");
                DrawPropertyIfExists("_arriveArrivalRadius");
            });
            
            // Formation Settings
            _showFormation = DrawSection("Formation Settings", _showFormation, () =>
            {
                DrawPropertyIfExists("_formationSpacing");
                DrawPropertyIfExists("_formationMoveSpeed");
                DrawPropertyIfExists("_formationArrivalRadius");
            });
            
            // Follow Leader
            _showFollowLeader = DrawSection("Follow Leader", _showFollowLeader, () =>
            {
                DrawPropertyIfExists("_defaultFollowDistance");
                DrawPropertyIfExists("_followSlowingRadius");
            });
            
            // Resource Gathering
            _showResourceGathering = DrawSection("Resource Gathering", _showResourceGathering, () =>
            {
                DrawPropertyIfExists("_defaultCarryCapacity");
                DrawPropertyIfExists("_depositRadius");
                DrawPropertyIfExists("_resourceSearchDelay");
            });
            
            // Performance
            _showPerformance = DrawSection("Performance", _showPerformance, () =>
            {
                DrawPropertyIfExists("_maxAgentsPerFrame");
                DrawPropertyIfExists("_spatialHashUpdateInterval");
            });
            
            // State Machine
            _showStateMachine = DrawSection("State Machine", _showStateMachine, () =>
            {
                DrawPropertyIfExists("_stuckThreshold");
                DrawPropertyIfExists("_stuckTimeLimit");
                DrawPropertyIfExists("_defaultFleeDistance");
                DrawPropertyIfExists("_fleeSpeedMultiplier");
            });
            
            // Debug
            _showDebug = DrawSection("Debug", _showDebug, () =>
            {
                DrawPropertyIfExists("_enableDebugVisualization");
                DrawPropertyIfExists("_showNeighborConnections");
                DrawPropertyIfExists("_showVelocityVectors");
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
        
        private void DrawPropertyIfExists(string propertyName)
        {
            var prop = serializedObject.FindProperty(propertyName);
            if (prop != null)
            {
                EditorGUILayout.PropertyField(prop);
            }
        }
    }
}
