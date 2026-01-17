using UnityEditor;
using UnityEngine;

namespace SwarmAI.Editor
{
    /// <summary>
    /// Custom editor for SwarmJobSystem with setup helpers.
    /// </summary>
    [CustomEditor(typeof(Jobs.SwarmJobSystem))]
    public class SwarmJobSystemEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var jobSystem = (Jobs.SwarmJobSystem)target;
            
            // Draw default inspector
            DrawDefaultInspector();
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Runtime Stats", EditorStyles.boldLabel);
            
            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField("Active", jobSystem.IsUsingJobs ? "Yes (Jobs)" : "No (Single-threaded)");
                EditorGUILayout.LabelField("Agents Processed", jobSystem.LastProcessedCount.ToString());
                EditorGUILayout.LabelField("Job Time", $"{jobSystem.LastJobTimeMs:F2} ms");
                
                // Force repaint for live stats
                Repaint();
            }
            else
            {
                EditorGUILayout.HelpBox("Runtime stats available in Play mode.", MessageType.Info);
            }
            
            EditorGUILayout.Space();
            
            // Burst package check
            #if !SWARMAI_BURST_ENABLED
            EditorGUILayout.HelpBox(
                "Burst package not detected. Install com.unity.burst via Package Manager for optimal performance.",
                MessageType.Warning);
            #else
            EditorGUILayout.HelpBox(
                "Burst package detected. Jobs will use Burst compilation for optimal performance.",
                MessageType.Info);
            #endif
        }
        
        /// <summary>
        /// Menu item to add SwarmJobSystem to selected SwarmManager.
        /// </summary>
        [MenuItem("CONTEXT/SwarmManager/Add Jobs System")]
        private static void AddJobsSystem(MenuCommand command)
        {
            var swarmManager = command.context as SwarmManager;
            if (swarmManager == null) return;
            
            if (swarmManager.GetComponent<Jobs.SwarmJobSystem>() != null)
            {
                Debug.Log("[SwarmAI] SwarmJobSystem already exists on this GameObject.");
                return;
            }
            
            Undo.AddComponent<Jobs.SwarmJobSystem>(swarmManager.gameObject);
            Debug.Log("[SwarmAI] Added SwarmJobSystem for parallel processing.");
        }
        
        /// <summary>
        /// Menu item to create a SwarmManager with Jobs support.
        /// </summary>
        [MenuItem("GameObject/SwarmAI/Swarm Manager (with Jobs)", false, 10)]
        private static void CreateSwarmManagerWithJobs(MenuCommand menuCommand)
        {
            var go = new GameObject("SwarmManager");
            go.AddComponent<SwarmManager>();
            go.AddComponent<Jobs.SwarmJobSystem>();
            
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            Undo.RegisterCreatedObjectUndo(go, "Create Swarm Manager with Jobs");
            Selection.activeObject = go;
        }
    }
}
