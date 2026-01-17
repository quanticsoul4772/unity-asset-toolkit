using UnityEditor;
using UnityEngine;

namespace NPCBrain.Editor
{
    /// <summary>
    /// Debug window for inspecting NPC behavior at runtime.
    /// TODO: Implement in Week 4
    /// </summary>
    public class NPCBrainDebugWindow : EditorWindow
    {
        [MenuItem("Window/NPCBrain/Debug Window")]
        public static void ShowWindow()
        {
            GetWindow<NPCBrainDebugWindow>("NPCBrain Debug");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("NPCBrain Debug Window", EditorStyles.boldLabel);
            GUILayout.Space(10);
            GUILayout.Label("Coming in Week 4!", EditorStyles.centeredGreyMiniLabel);
            GUILayout.Space(20);
            
            GUILayout.Label("Planned Features:", EditorStyles.boldLabel);
            GUILayout.Label("- NPC selector dropdown");
            GUILayout.Label("- Current state display");
            GUILayout.Label("- Blackboard viewer");
            GUILayout.Label("- Pause/Step controls");
        }
    }
}
