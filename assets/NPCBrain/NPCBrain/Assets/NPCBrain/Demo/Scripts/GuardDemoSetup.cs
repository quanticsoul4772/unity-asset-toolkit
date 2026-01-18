using System.Collections.Generic;
using UnityEngine;
using NPCBrain.Archetypes;
using NPCBrain.Perception;

namespace NPCBrain.Demo
{
    /// <summary>
    /// Sets up a polished Guard demo scene with player, guards, obstacles, and patrol routes.
    /// Demonstrates GuardNPC's chase, investigate, and patrol behaviors.
    /// </summary>
    public class GuardDemoSetup : MonoBehaviour
    {
        [Header("Scene Settings")]
        [SerializeField] private bool _autoGenerate = true;
        [SerializeField] private int _guardCount = 2;
        [SerializeField] private float _arenaSize = 20f;
        
        [Header("Colors")]
        [SerializeField] private Color _groundColor = new Color(0.25f, 0.25f, 0.3f);
        [SerializeField] private Color _guardColor = new Color(0.8f, 0.3f, 0.3f);
        [SerializeField] private Color _obstacleColor = new Color(0.4f, 0.4f, 0.45f);
        [SerializeField] private Color _waypointColor = new Color(0.3f, 0.6f, 1f, 0.5f);
        
        [Header("References (auto-populated)")]
        [SerializeField] private GameObject _player;
        [SerializeField] private List<GuardNPC> _guards = new List<GuardNPC>();
        
        private void Start()
        {
            if (_autoGenerate)
            {
                GenerateScene();
            }
        }
        
        /// <summary>
        /// Generates the complete Guard demo scene.
        /// </summary>
        [ContextMenu("Generate Guard Demo")]
        public void GenerateScene()
        {
            ClearScene();
            CreateGround();
            CreateWalls();
            CreateObstacles();
            CreatePlayer();
            CreateGuards();
            
            Debug.Log("Guard Demo generated! Use WASD to move the player. Guards will chase when they see you!");
        }
        
        private void ClearScene()
        {
            // Clear any existing generated objects
            var toDestroy = new List<GameObject>();
            foreach (Transform child in transform)
            {
                toDestroy.Add(child.gameObject);
            }
            foreach (var obj in toDestroy)
            {
                if (Application.isPlaying)
                    Destroy(obj);
                else
                    DestroyImmediate(obj);
            }
            _guards.Clear();
            _player = null;
        }
        
        private void CreateGround()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(transform);
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(_arenaSize / 10f, 1f, _arenaSize / 10f);
            ground.GetComponent<Renderer>().material.color = _groundColor;
            ground.isStatic = true;
        }
        
        private void CreateWalls()
        {
            float halfSize = _arenaSize / 2f;
            float wallHeight = 3f;
            float wallThickness = 0.5f;
            
            // Create 4 walls around the arena
            CreateWall("WallNorth", new Vector3(0f, wallHeight / 2f, halfSize), new Vector3(_arenaSize, wallHeight, wallThickness));
            CreateWall("WallSouth", new Vector3(0f, wallHeight / 2f, -halfSize), new Vector3(_arenaSize, wallHeight, wallThickness));
            CreateWall("WallEast", new Vector3(halfSize, wallHeight / 2f, 0f), new Vector3(wallThickness, wallHeight, _arenaSize));
            CreateWall("WallWest", new Vector3(-halfSize, wallHeight / 2f, 0f), new Vector3(wallThickness, wallHeight, _arenaSize));
        }
        
        private void CreateWall(string name, Vector3 position, Vector3 scale)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(transform);
            wall.transform.position = position;
            wall.transform.localScale = scale;
            wall.GetComponent<Renderer>().material.color = _obstacleColor;
            wall.isStatic = true;
        }
        
        private void CreateObstacles()
        {
            // Create some obstacles for cover and visual interest
            float halfSize = _arenaSize / 2f - 3f;
            
            // Central pillar
            CreateObstacle("CentralPillar", Vector3.zero + Vector3.up, new Vector3(2f, 2f, 2f));
            
            // Corner pillars
            CreateObstacle("Pillar_NE", new Vector3(halfSize * 0.6f, 1f, halfSize * 0.6f), new Vector3(1.5f, 2f, 1.5f));
            CreateObstacle("Pillar_NW", new Vector3(-halfSize * 0.6f, 1f, halfSize * 0.6f), new Vector3(1.5f, 2f, 1.5f));
            CreateObstacle("Pillar_SE", new Vector3(halfSize * 0.6f, 1f, -halfSize * 0.6f), new Vector3(1.5f, 2f, 1.5f));
            CreateObstacle("Pillar_SW", new Vector3(-halfSize * 0.6f, 1f, -halfSize * 0.6f), new Vector3(1.5f, 2f, 1.5f));
            
            // Side barriers for cover
            CreateObstacle("Barrier_N", new Vector3(0f, 0.75f, halfSize * 0.4f), new Vector3(4f, 1.5f, 0.8f));
            CreateObstacle("Barrier_S", new Vector3(0f, 0.75f, -halfSize * 0.4f), new Vector3(4f, 1.5f, 0.8f));
        }
        
        private void CreateObstacle(string name, Vector3 position, Vector3 scale)
        {
            var obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.name = name;
            obstacle.transform.SetParent(transform);
            obstacle.transform.position = position;
            obstacle.transform.localScale = scale;
            obstacle.GetComponent<Renderer>().material.color = _obstacleColor * 0.8f;
            obstacle.isStatic = true;
        }
        
        private void CreatePlayer()
        {
            _player = PlayerController.CreatePlayer(new Vector3(0f, 0.1f, -_arenaSize / 2f + 3f));
            _player.transform.SetParent(transform);
        }
        
        private void CreateGuards()
        {
            float halfSize = _arenaSize / 2f - 2f;
            
            // Create guards at different positions with their own patrol routes
            Vector3[] guardPositions = new Vector3[]
            {
                new Vector3(halfSize, 0.1f, halfSize),
                new Vector3(-halfSize, 0.1f, -halfSize),
                new Vector3(halfSize, 0.1f, -halfSize),
                new Vector3(-halfSize, 0.1f, halfSize)
            };
            
            for (int i = 0; i < Mathf.Min(_guardCount, guardPositions.Length); i++)
            {
                var guard = CreateGuard($"Guard_{i}", guardPositions[i], i);
                _guards.Add(guard);
            }
        }
        
        private GuardNPC CreateGuard(string name, Vector3 position, int patrolIndex)
        {
            // Create guard visual
            var guardObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            guardObj.name = name;
            guardObj.transform.SetParent(transform);
            guardObj.transform.position = position;
            guardObj.GetComponent<Renderer>().material.color = _guardColor;
            
            // Add guard component
            var guard = guardObj.AddComponent<GuardNPC>();
            
            // Add sight sensor with debug logging enabled
            var sightSensor = guardObj.AddComponent<SightSensor>();
            
            // Enable debug logging via reflection to help diagnose issues
            var debugField = typeof(SightSensor).GetField("_debugLogging", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (debugField != null)
            {
                debugField.SetValue(sightSensor, true);
            }
            
            // Create patrol waypoints
            var waypointPath = CreatePatrolRoute(name + "_Patrol", position, patrolIndex);
            guard.SetWaypointPath(waypointPath);
            
            // Add hat/indicator for visual distinction
            var hat = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hat.name = "Hat";
            hat.transform.SetParent(guardObj.transform);
            hat.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            hat.transform.localScale = new Vector3(0.6f, 0.15f, 0.6f);
            hat.GetComponent<Renderer>().material.color = Color.black;
            Object.Destroy(hat.GetComponent<Collider>());
            
            return guard;
        }
        
        private WaypointPath CreatePatrolRoute(string name, Vector3 center, int patrolIndex)
        {
            var container = new GameObject(name);
            container.transform.SetParent(transform);
            
            var waypointPath = container.AddComponent<WaypointPath>();
            var waypoints = new List<Transform>();
            
            // Create different patrol patterns based on index
            float patrolRadius = 4f;
            int waypointCount = 4;
            float angleOffset = patrolIndex * 45f;
            
            for (int i = 0; i < waypointCount; i++)
            {
                float angle = (360f / waypointCount * i + angleOffset) * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle) * patrolRadius, 0f, Mathf.Sin(angle) * patrolRadius);
                Vector3 waypointPos = center + offset;
                
                // Clamp to arena bounds
                float maxPos = _arenaSize / 2f - 1.5f;
                waypointPos.x = Mathf.Clamp(waypointPos.x, -maxPos, maxPos);
                waypointPos.z = Mathf.Clamp(waypointPos.z, -maxPos, maxPos);
                waypointPos.y = 0.1f;
                
                var waypoint = new GameObject($"Waypoint_{i}");
                waypoint.transform.SetParent(container.transform);
                waypoint.transform.position = waypointPos;
                waypoints.Add(waypoint.transform);
                
                // Visual marker
                var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.name = "Marker";
                marker.transform.SetParent(waypoint.transform);
                marker.transform.localPosition = Vector3.zero;
                marker.transform.localScale = Vector3.one * 0.4f;
                marker.GetComponent<Renderer>().material.color = _waypointColor;
                Object.Destroy(marker.GetComponent<Collider>());
            }
            
            waypointPath.SetWaypoints(waypoints);
            return waypointPath;
        }
        
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 380, 400));
            
            // Title
            GUILayout.BeginVertical("box");
            GUILayout.Label("<size=16><b>NPCBrain Guard Demo</b></size>");
            GUILayout.Label("<i>Demonstrating GuardNPC archetype</i>");
            GUILayout.EndVertical();
            
            GUILayout.Space(5);
            
            // Controls
            GUILayout.BeginVertical("box");
            GUILayout.Label("<b>Controls:</b>");
            GUILayout.Label("  WASD/Arrows - Move player");
            GUILayout.Label("  Shift - Sprint");
            GUILayout.EndVertical();
            
            GUILayout.Space(5);
            
            // Guard info
            GUILayout.BeginVertical("box");
            GUILayout.Label("<b>Guards:</b>");
            foreach (var guard in _guards)
            {
                if (guard == null) continue;
                
                string state = "Patrol";
                float alertLevel = guard.Blackboard.Get("alertLevel", 0f);
                bool hasTarget = guard.Blackboard.Has("target");
                bool hasLastKnown = guard.Blackboard.Has("lastKnownPosition");
                
                if (hasTarget) state = "<color=red>CHASE</color>";
                else if (hasLastKnown) state = "<color=yellow>INVESTIGATE</color>";
                else if (alertLevel > 0.1f) state = "<color=orange>RETURNING</color>";
                else state = "<color=green>Patrol</color>";
                
                GUILayout.Label($"  {guard.name}: {state} (Alert: {alertLevel:F1})");
            }
            GUILayout.EndVertical();
            
            GUILayout.Space(5);
            
            // Behavior explanation
            GUILayout.BeginVertical("box");
            GUILayout.Label("<b>Guard Behavior Priority:</b>");
            GUILayout.Label("  1. Chase visible target");
            GUILayout.Label("  2. Investigate last known position");
            GUILayout.Label("  3. Return to post");
            GUILayout.Label("  4. Patrol waypoints");
            GUILayout.EndVertical();
            
            GUILayout.EndArea();
        }
    }
}
