using UnityEngine;
using NPCBrain;
using NPCBrain.BehaviorTree;
using NPCBrain.BehaviorTree.Composites;
using NPCBrain.BehaviorTree.Actions;

namespace NPCBrain.Demo
{
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
        
        [ContextMenu("Generate Test Scene")]
        public void GenerateTestScene()
        {
            // Create ground plane
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(3, 1, 3);
            ground.GetComponent<Renderer>().material.color = new Color(0.3f, 0.3f, 0.3f);
            
            // Create waypoint container
            var waypointContainer = new GameObject("Waypoints");
            _waypointPath = waypointContainer.AddComponent<WaypointPath>();
            
            // Create waypoints in a circle
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
                
                // Visual marker
                var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.name = "Marker";
                marker.transform.parent = waypoint.transform;
                marker.transform.localPosition = Vector3.zero;
                marker.transform.localScale = Vector3.one * 0.3f;
                marker.GetComponent<Renderer>().material.color = Color.cyan;
                Object.Destroy(marker.GetComponent<Collider>());
            }
            
            _waypointPath.SetWaypoints(waypointList);
            
            // Create NPC
            _npcObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            _npcObject.name = "TestNPC";
            _npcObject.transform.position = new Vector3(0, 1, 0);
            _npcObject.GetComponent<Renderer>().material.color = Color.blue;
            
            // Add PatrolNPC component
            var brain = _npcObject.AddComponent<PatrolNPC>();
            brain.SetWaypointPath(_waypointPath);
            
            Debug.Log("Test scene generated! Press Play to see the NPC patrol.");
        }
        
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label("NPCBrain Test Scene", GUI.skin.box);
            
            if (_npcObject != null)
            {
                var brain = _npcObject.GetComponent<NPCBrainController>();
                if (brain != null)
                {
                    GUILayout.Label($"Status: {brain.LastStatus}");
                    GUILayout.Label($"Position: {_npcObject.transform.position:F1}");
                    
                    if (brain.WaypointPath != null)
                    {
                        GUILayout.Label($"Current Waypoint: {brain.WaypointPath.CurrentIndex}");
                    }
                    
                    GUILayout.Space(10);
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
    
    public class PatrolNPC : NPCBrainController
    {
        protected override BTNode CreateBehaviorTree()
        {
            return new Sequence(
                new MoveTo(() => GetCurrentWaypoint()),
                new Wait(1f),
                new AdvanceWaypoint()
            );
        }
    }
}
