using UnityEngine;
using NPCBrain;
using NPCBrain.BehaviorTree;
using NPCBrain.BehaviorTree.Composites;
using NPCBrain.BehaviorTree.Actions;

namespace NPCBrain.Demo
{
    /// <summary>
    /// Sets up a basic test scene with ground, waypoints, and a patrol NPC.
    /// Use this to quickly test behavior tree functionality.
    /// </summary>
    /// <remarks>
    /// <para>Attach to an empty GameObject and enable Auto Generate, or use the
    /// context menu "Generate Test Scene" to create the scene manually.</para>
    /// </remarks>
    public class TestSceneSetup : MonoBehaviour
    {
        [Header("Scene Settings")]
        [SerializeField] private bool _autoGenerate = true;
        [SerializeField] private int _waypointCount = 4;
        [SerializeField] private float _waypointRadius = 5f;
        
        [Header("References (auto-populated)")]
        [SerializeField] private GameObject _npcObject;
        [SerializeField] private WaypointPath _waypointPath;
        
        private void Start()
        {
            if (_autoGenerate)
            {
                GenerateTestScene();
            }
        }
        
        /// <summary>
        /// Creates the complete test scene with ground, waypoints, and NPC.
        /// </summary>
        [ContextMenu("Generate Test Scene")]
        public void GenerateTestScene()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(3, 1, 3);
            ground.GetComponent<Renderer>().material.color = new Color(0.3f, 0.3f, 0.3f);
            
            var waypointContainer = new GameObject("Waypoints");
            _waypointPath = waypointContainer.AddComponent<WaypointPath>();
            
            var waypointList = new System.Collections.Generic.List<Transform>();
            
            for (int i = 0; i < _waypointCount; i++)
            {
                float angle = (360f / _waypointCount) * i * Mathf.Deg2Rad;
                Vector3 pos = new Vector3(
                    Mathf.Cos(angle) * _waypointRadius,
                    0.1f,
                    Mathf.Sin(angle) * _waypointRadius
                );
                
                var waypoint = new GameObject($"Waypoint_{i}");
                waypoint.transform.position = pos;
                waypoint.transform.parent = waypointContainer.transform;
                waypointList.Add(waypoint.transform);
                
                var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.name = "Marker";
                marker.transform.parent = waypoint.transform;
                marker.transform.localPosition = Vector3.zero;
                marker.transform.localScale = Vector3.one * 0.3f;
                marker.GetComponent<Renderer>().material.color = Color.cyan;
                Object.Destroy(marker.GetComponent<Collider>());
            }
            
            _waypointPath.SetWaypoints(waypointList);
            
            _npcObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            _npcObject.name = "TestNPC";
            _npcObject.transform.position = new Vector3(0, 1, 0);
            _npcObject.GetComponent<Renderer>().material.color = Color.blue;
            
            var brain = _npcObject.AddComponent<PatrolNPC>();
            brain.SetWaypointPath(_waypointPath);
            
            Debug.Log("Test scene generated! Press Play to see the NPC patrol.");
        }
        
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 350, 250));
            GUILayout.Label("NPCBrain Test Scene", GUI.skin.box);
            
            if (_npcObject != null)
            {
                var brain = _npcObject.GetComponent<NPCBrainController>();
                if (brain != null)
                {
                    GUILayout.Label($"Status: {brain.LastStatus}");
                    GUILayout.Label($"Paused: {brain.IsPaused}");
                    GUILayout.Label($"Position: {_npcObject.transform.position:F1}");
                    
                    if (brain.WaypointPath != null)
                    {
                        GUILayout.Label($"Current Waypoint: {brain.WaypointPath.CurrentIndex}");
                    }
                    
                    if (brain.BehaviorTree != null)
                    {
                        GUILayout.Label($"BT Node: {brain.BehaviorTree.Name ?? "(unnamed)"}");
                        GUILayout.Label($"BT Running: {brain.BehaviorTree.IsRunning}");
                    }
                    
                    GUILayout.Space(5);
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button(brain.IsPaused ? "Resume" : "Pause"))
                    {
                        if (brain.IsPaused)
                            brain.Resume();
                        else
                            brain.Pause();
                    }
                    GUILayout.EndHorizontal();
                    
                    GUILayout.Space(5);
                    GUILayout.Label("Blackboard:");
                    foreach (var key in brain.Blackboard.Keys)
                    {
                        var value = brain.Blackboard.Get<object>(key);
                        GUILayout.Label($"  {key}: {value}");
                    }
                }
            }
            
            GUILayout.EndArea();
        }
    }
    
    /// <summary>
    /// Simple patrol NPC that loops through waypoints.
    /// Used by <see cref="TestSceneSetup"/> for basic testing.
    /// </summary>
    public class PatrolNPC : NPCBrainController
    {
        /// <inheritdoc/>
        protected override BTNode CreateBehaviorTree()
        {
            var tree = new Sequence(
                new MoveTo(() => GetCurrentWaypoint()),
                new Wait(1f),
                new AdvanceWaypoint()
            );
            tree.Name = "PatrolSequence";
            return tree;
        }
    }
}
