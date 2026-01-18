using System.Collections.Generic;
using UnityEngine;
using NPCBrain.Archetypes;
using NPCBrain.Perception;
using NPCBrain.BehaviorTree.Composites;
using UnityEngine.InputSystem;

namespace NPCBrain.Demo
{
    /// <summary>
    /// Sets up a demo scene showcasing the Hearing system.
    /// Guards react to footsteps from the player and gunshots triggered by clicking.
    /// </summary>
    public class HearingDemoSetup : MonoBehaviour
    {
        [Header("Scene Settings")]
        [SerializeField] private bool _autoGenerate = true;
        [SerializeField] private int _guardCount = 3;
        [SerializeField] private float _arenaSize = 25f;
        
        [Header("Colors")]
        [SerializeField] private Color _groundColor = new Color(0.2f, 0.25f, 0.3f);
        [SerializeField] private Color _guardColor = new Color(0.8f, 0.4f, 0.2f);
        [SerializeField] private Color _obstacleColor = new Color(0.35f, 0.35f, 0.4f);
        [SerializeField] private Color _waypointColor = new Color(0.3f, 0.6f, 1f, 0.5f);
        [SerializeField] private Color _gunshotMarkerColor = new Color(1f, 0.3f, 0.3f, 0.7f);
        
        [Header("References (auto-populated)")]
        [SerializeField] private GameObject _player;
        [SerializeField] private List<HearingGuardNPC> _guards = new List<HearingGuardNPC>();
        
        private List<Vector3> _recentGunshotPositions = new List<Vector3>();
        private List<float> _gunshotTimes = new List<float>();
        
        private void Start()
        {
            if (_autoGenerate)
            {
                GenerateScene();
            }
        }
        
        private void Update()
        {
            HandleGunshotInput();
            CleanupOldGunshots();
        }
        
        private void HandleGunshotInput()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;
            
            // Left click to fire gunshot at mouse position
            if (mouse.leftButton.wasPressedThisFrame)
            {
                Ray ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());
                if (Physics.Raycast(ray, out RaycastHit hit, 100f))
                {
                    Vector3 gunshotPos = hit.point;
                    gunshotPos.y = 1f; // Slightly above ground
                    
                    // Emit gunshot sound
                    SoundManager.EmitGunshot(gunshotPos, 1f, null);
                    
                    // Track for visualization
                    _recentGunshotPositions.Add(gunshotPos);
                    _gunshotTimes.Add(Time.time);
                    
                    Debug.Log($"<color=red>[HearingDemo] GUNSHOT at {gunshotPos}</color>");
                }
            }
        }
        
        private void CleanupOldGunshots()
        {
            // Remove gunshot markers after 3 seconds
            for (int i = _gunshotTimes.Count - 1; i >= 0; i--)
            {
                if (Time.time - _gunshotTimes[i] > 3f)
                {
                    _gunshotTimes.RemoveAt(i);
                    _recentGunshotPositions.RemoveAt(i);
                }
            }
        }
        
        /// <summary>
        /// Generates the complete Hearing demo scene.
        /// </summary>
        [ContextMenu("Generate Hearing Demo")]
        public void GenerateScene()
        {
            ClearScene();
            CreateGround();
            CreateWalls();
            CreateObstacles();
            CreatePlayer();
            CreateGuards();
            
            Debug.Log("Hearing Demo generated! Use WASD to move (emits footsteps). Left-click to fire gunshots. Guards will investigate sounds!");
        }
        
        private void ClearScene()
        {
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
            _recentGunshotPositions.Clear();
            _gunshotTimes.Clear();
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
            float halfSize = _arenaSize / 2f - 3f;
            
            // Create obstacles that can block line of sight but not sound
            CreateObstacle("Pillar_Center", Vector3.zero + Vector3.up, new Vector3(2.5f, 2f, 2.5f));
            
            // Corner pillars
            CreateObstacle("Pillar_NE", new Vector3(halfSize * 0.5f, 1f, halfSize * 0.5f), new Vector3(2f, 2f, 2f));
            CreateObstacle("Pillar_NW", new Vector3(-halfSize * 0.5f, 1f, halfSize * 0.5f), new Vector3(2f, 2f, 2f));
            CreateObstacle("Pillar_SE", new Vector3(halfSize * 0.5f, 1f, -halfSize * 0.5f), new Vector3(2f, 2f, 2f));
            CreateObstacle("Pillar_SW", new Vector3(-halfSize * 0.5f, 1f, -halfSize * 0.5f), new Vector3(2f, 2f, 2f));
            
            // Long barriers
            CreateObstacle("Barrier_N", new Vector3(0f, 0.75f, halfSize * 0.3f), new Vector3(5f, 1.5f, 0.8f));
            CreateObstacle("Barrier_S", new Vector3(0f, 0.75f, -halfSize * 0.3f), new Vector3(5f, 1.5f, 0.8f));
            CreateObstacle("Barrier_E", new Vector3(halfSize * 0.3f, 0.75f, 0f), new Vector3(0.8f, 1.5f, 5f));
            CreateObstacle("Barrier_W", new Vector3(-halfSize * 0.3f, 0.75f, 0f), new Vector3(0.8f, 1.5f, 5f));
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
            
            // Add footstep emitter to player
            var footstepEmitter = _player.AddComponent<PlayerFootstepEmitter>();
        }
        
        private void CreateGuards()
        {
            float halfSize = _arenaSize / 2f - 3f;
            
            // Position guards around the arena
            Vector3[] guardPositions = new Vector3[]
            {
                new Vector3(halfSize, 0.1f, halfSize),
                new Vector3(-halfSize, 0.1f, halfSize),
                new Vector3(0f, 0.1f, halfSize * 0.6f),
                new Vector3(halfSize, 0.1f, -halfSize),
                new Vector3(-halfSize, 0.1f, -halfSize)
            };
            
            for (int i = 0; i < Mathf.Min(_guardCount, guardPositions.Length); i++)
            {
                var guard = CreateGuard($"HearingGuard_{i}", guardPositions[i], i);
                _guards.Add(guard);
            }
        }
        
        private HearingGuardNPC CreateGuard(string name, Vector3 position, int patrolIndex)
        {
            var guardObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            guardObj.name = name;
            guardObj.transform.SetParent(transform);
            guardObj.transform.position = position;
            guardObj.GetComponent<Renderer>().material.color = _guardColor;
            
            // Add sight sensor
            var sightSensor = guardObj.AddComponent<SightSensor>();
            
            // Add hearing sensor
            var hearingSensor = guardObj.AddComponent<HearingSensor>();
            
            // Enable debug logging via reflection
            var debugField = typeof(HearingSensor).GetField("_debugLogging", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (debugField != null)
            {
                debugField.SetValue(hearingSensor, true);
            }
            
            // Add guard component
            var guard = guardObj.AddComponent<HearingGuardNPC>();
            
            // Create patrol waypoints
            var waypointPath = CreatePatrolRoute(name + "_Patrol", position, patrolIndex);
            guard.SetWaypointPath(waypointPath);
            
            // Add ear indicator (shows this guard can hear)
            var earIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            earIndicator.name = "EarIndicator";
            earIndicator.transform.SetParent(guardObj.transform);
            earIndicator.transform.localPosition = new Vector3(0.3f, 1.2f, 0f);
            earIndicator.transform.localScale = Vector3.one * 0.25f;
            earIndicator.GetComponent<Renderer>().material.color = new Color(0.3f, 0.8f, 1f);
            Object.Destroy(earIndicator.GetComponent<Collider>());
            
            // Add second ear
            var earIndicator2 = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            earIndicator2.name = "EarIndicator2";
            earIndicator2.transform.SetParent(guardObj.transform);
            earIndicator2.transform.localPosition = new Vector3(-0.3f, 1.2f, 0f);
            earIndicator2.transform.localScale = Vector3.one * 0.25f;
            earIndicator2.GetComponent<Renderer>().material.color = new Color(0.3f, 0.8f, 1f);
            Object.Destroy(earIndicator2.GetComponent<Collider>());
            
            return guard;
        }
        
        private WaypointPath CreatePatrolRoute(string name, Vector3 center, int patrolIndex)
        {
            var container = new GameObject(name);
            container.transform.SetParent(transform);
            
            var waypointPath = container.AddComponent<WaypointPath>();
            var waypoints = new List<Transform>();
            
            float patrolRadius = 5f;
            int waypointCount = 4;
            float angleOffset = patrolIndex * 45f;
            
            for (int i = 0; i < waypointCount; i++)
            {
                float angle = (360f / waypointCount * i + angleOffset) * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle) * patrolRadius, 0f, Mathf.Sin(angle) * patrolRadius);
                Vector3 waypointPos = center + offset;
                
                float maxPos = _arenaSize / 2f - 2f;
                waypointPos.x = Mathf.Clamp(waypointPos.x, -maxPos, maxPos);
                waypointPos.z = Mathf.Clamp(waypointPos.z, -maxPos, maxPos);
                waypointPos.y = 0.1f;
                
                var waypoint = new GameObject($"Waypoint_{i}");
                waypoint.transform.SetParent(container.transform);
                waypoint.transform.position = waypointPos;
                waypoints.Add(waypoint.transform);
                
                var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.name = "Marker";
                marker.transform.SetParent(waypoint.transform);
                marker.transform.localPosition = Vector3.zero;
                marker.transform.localScale = Vector3.one * 0.3f;
                marker.GetComponent<Renderer>().material.color = _waypointColor;
                Object.Destroy(marker.GetComponent<Collider>());
            }
            
            waypointPath.SetWaypoints(waypoints);
            return waypointPath;
        }
        
        private void OnDrawGizmos()
        {
            // Draw recent gunshot positions
            Gizmos.color = _gunshotMarkerColor;
            foreach (var pos in _recentGunshotPositions)
            {
                Gizmos.DrawWireSphere(pos, 1f);
                Gizmos.DrawWireSphere(pos, 2f);
                // Draw "explosion" lines
                for (int i = 0; i < 8; i++)
                {
                    float angle = i * 45f * Mathf.Deg2Rad;
                    Vector3 dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                    Gizmos.DrawLine(pos, pos + dir * 3f);
                }
            }
        }
        
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 450, 600));
            
            // Title
            GUILayout.BeginVertical("box");
            GUILayout.Label("<size=16><b>NPCBrain Hearing Demo</b></size>");
            GUILayout.Label("<i>Demonstrating HearingSensor + Utility AI + Criticality</i>");
            GUILayout.EndVertical();
            
            GUILayout.Space(5);
            
            // Controls
            GUILayout.BeginVertical("box");
            GUILayout.Label("<b>Controls:</b>");
            GUILayout.Label("  WASD/Arrows - Move player (emits <color=yellow>footsteps</color>)");
            GUILayout.Label("  Shift - Sprint (louder footsteps)");
            GUILayout.Label("  <color=red>Left Click</color> - Fire gunshot at cursor position");
            GUILayout.EndVertical();
            
            GUILayout.Space(5);
            
            // Sound info
            GUILayout.BeginVertical("box");
            GUILayout.Label("<b>Sound Types (by priority):</b>");
            GUILayout.Label("  <color=red>Gunshot</color> - High utility score, guards rush to investigate");
            GUILayout.Label("  <color=yellow>Footstep</color> - Medium utility score, probabilistic response");
            GUILayout.Label($"  Active sounds: {SoundManager.ActiveSoundCount}");
            GUILayout.EndVertical();
            
            GUILayout.Space(5);
            
            // Guard info with Criticality
            GUILayout.BeginVertical("box");
            GUILayout.Label("<b>Guards (Utility AI):</b>");
            foreach (var guard in _guards)
            {
                if (guard == null) continue;
                
                string state = guard.CurrentState;
                float alertLevel = guard.AlertLevel;
                
                string stateColor = "green";
                if (state.Contains("Chase")) stateColor = "red";
                else if (state.Contains("Gunshot")) stateColor = "red";
                else if (state.Contains("Investigate")) stateColor = "yellow";
                else if (state.Contains("Return")) stateColor = "cyan";
                
                // Get current action from UtilitySelector
                string actionName = "(selecting)";
                if (guard.BehaviorTree is UtilitySelector selector && selector.CurrentAction != null)
                {
                    actionName = selector.CurrentAction.Name;
                }
                
                // Get Criticality info
                string critInfo = "";
                if (guard.Criticality != null)
                {
                    float temp = guard.Criticality.Temperature;
                    float inertia = guard.Criticality.Inertia;
                    string tempColor = temp < 1f ? "green" : (temp < 1.5f ? "yellow" : "red");
                    critInfo = $" T:<color={tempColor}>{temp:F1}</color> I:{inertia:F1}";
                }
                
                string soundInfo = "";
                if (guard.Hearing != null && guard.Hearing.HasHeardSounds)
                {
                    var sound = guard.Hearing.HighestPrioritySound;
                    if (sound != null)
                    {
                        soundInfo = $" [Heard: {sound.Type}]";
                    }
                }
                
                GUILayout.Label($"  {guard.name}: <color={stateColor}>{actionName}</color>{critInfo}");
                GUILayout.Label($"      Alert: {alertLevel:F2}{soundInfo}");
            }
            GUILayout.EndVertical();
            
            GUILayout.Space(5);
            
            // Criticality explanation
            GUILayout.BeginVertical("box");
            GUILayout.Label("<b>Criticality System:</b>");
            GUILayout.Label("  <b>T</b> = Temperature (exploration vs exploitation)");
            GUILayout.Label("    <color=green>Low</color> = Deterministic, picks best action");
            GUILayout.Label("    <color=red>High</color> = Random, explores alternatives");
            GUILayout.Label("  <b>I</b> = Inertia (tendency to repeat actions)");
            GUILayout.EndVertical();
            
            GUILayout.Space(5);
            
            // Utility actions
            GUILayout.BeginVertical("box");
            GUILayout.Label("<b>Utility Actions (scored dynamically):</b>");
            GUILayout.Label("  <color=red>Chase</color> - Has visible target + close + alert");
            GUILayout.Label("  <color=red>InvestigateGunshot</color> - Heard gunshot + close + alert");
            GUILayout.Label("  <color=yellow>InvestigateFootstep</color> - Heard footstep + alert");
            GUILayout.Label("  <color=cyan>Return</color> - Far from home + no threats");
            GUILayout.Label("  <color=green>Patrol</color> - Baseline fallback");
            GUILayout.EndVertical();
            
            GUILayout.EndArea();
        }
    }
}
