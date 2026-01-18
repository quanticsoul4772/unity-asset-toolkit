using System.Collections.Generic;
using UnityEngine;
using NPCBrain.Archetypes;

namespace NPCBrain.Demo
{
    /// <summary>
    /// Sets up a Utility AI demo scene showcasing the Criticality system.
    /// NPCs make decisions using utility scoring, and Temperature/Inertia
    /// automatically adjust based on action variety.
    /// </summary>
    public class UtilityDemoSetup : MonoBehaviour
    {
        [Header("Scene Settings")]
        [SerializeField] private bool _autoGenerate = true;
        [SerializeField] private int _npcCount = 4;
        [SerializeField] private float _arenaSize = 25f;
        
        [Header("Colors")]
        [SerializeField] private Color _groundColor = new Color(0.2f, 0.3f, 0.25f);
        [SerializeField] private Color _npcColor = new Color(0.3f, 0.5f, 0.8f);
        [SerializeField] private Color _waypointColor = new Color(0.8f, 0.6f, 0.2f, 0.5f);
        [SerializeField] private Color _interestColor = new Color(1f, 0.3f, 0.3f, 0.8f);
        
        [Header("References (auto-populated)")]
        [SerializeField] private List<UtilityNPC> _npcs = new List<UtilityNPC>();
        
        private List<GameObject> _interestPoints = new List<GameObject>();
        private float _lastInterestSpawnTime;
        
        private void Start()
        {
            if (_autoGenerate)
            {
                GenerateScene();
            }
        }
        
        private void Update()
        {
            // Spawn random interest points occasionally
            if (Time.time - _lastInterestSpawnTime > 8f)
            {
                SpawnRandomInterestPoint();
                _lastInterestSpawnTime = Time.time;
            }
            
            // Clean up expired interest points
            CleanupInterestPoints();
            
            // Handle input
            HandleInput();
        }
        
        /// <summary>
        /// Generates the complete Utility AI demo scene.
        /// </summary>
        [ContextMenu("Generate Utility Demo")]
        public void GenerateScene()
        {
            ClearScene();
            CreateGround();
            CreateWalls();
            CreateDecorations();
            CreateNPCs();
            
            Debug.Log("Utility AI Demo generated! Watch NPCs make decisions using Utility AI. Temperature and Inertia will change based on action variety.");
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
            _npcs.Clear();
            _interestPoints.Clear();
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
            float wallHeight = 2f;
            float wallThickness = 0.5f;
            Color wallColor = new Color(0.3f, 0.35f, 0.3f);
            
            CreateWall("WallNorth", new Vector3(0f, wallHeight / 2f, halfSize), new Vector3(_arenaSize, wallHeight, wallThickness), wallColor);
            CreateWall("WallSouth", new Vector3(0f, wallHeight / 2f, -halfSize), new Vector3(_arenaSize, wallHeight, wallThickness), wallColor);
            CreateWall("WallEast", new Vector3(halfSize, wallHeight / 2f, 0f), new Vector3(wallThickness, wallHeight, _arenaSize), wallColor);
            CreateWall("WallWest", new Vector3(-halfSize, wallHeight / 2f, 0f), new Vector3(wallThickness, wallHeight, _arenaSize), wallColor);
        }
        
        private void CreateWall(string name, Vector3 position, Vector3 scale, Color color)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(transform);
            wall.transform.position = position;
            wall.transform.localScale = scale;
            wall.GetComponent<Renderer>().material.color = color;
            wall.isStatic = true;
        }
        
        private void CreateDecorations()
        {
            float halfSize = _arenaSize / 2f - 3f;
            Color decorColor = new Color(0.25f, 0.4f, 0.25f);
            
            // Some decorative pillars
            CreateDecoration("Pillar_1", new Vector3(halfSize * 0.5f, 0.75f, halfSize * 0.5f), new Vector3(1f, 1.5f, 1f), decorColor);
            CreateDecoration("Pillar_2", new Vector3(-halfSize * 0.5f, 0.75f, halfSize * 0.5f), new Vector3(1f, 1.5f, 1f), decorColor);
            CreateDecoration("Pillar_3", new Vector3(halfSize * 0.5f, 0.75f, -halfSize * 0.5f), new Vector3(1f, 1.5f, 1f), decorColor);
            CreateDecoration("Pillar_4", new Vector3(-halfSize * 0.5f, 0.75f, -halfSize * 0.5f), new Vector3(1f, 1.5f, 1f), decorColor);
        }
        
        private void CreateDecoration(string name, Vector3 position, Vector3 scale, Color color)
        {
            var deco = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            deco.name = name;
            deco.transform.SetParent(transform);
            deco.transform.position = position;
            deco.transform.localScale = scale;
            deco.GetComponent<Renderer>().material.color = color;
            deco.isStatic = true;
        }
        
        private void CreateNPCs()
        {
            float halfSize = _arenaSize / 2f - 4f;
            
            for (int i = 0; i < _npcCount; i++)
            {
                float angle = (360f / _npcCount * i) * Mathf.Deg2Rad;
                Vector3 position = new Vector3(
                    Mathf.Cos(angle) * halfSize * 0.6f,
                    0.1f,
                    Mathf.Sin(angle) * halfSize * 0.6f
                );
                
                var npc = CreateUtilityNPC($"UtilityNPC_{i}", position, i);
                _npcs.Add(npc);
            }
        }
        
        private UtilityNPC CreateUtilityNPC(string name, Vector3 position, int index)
        {
            // Create NPC visual
            var npcObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            npcObj.name = name;
            npcObj.transform.SetParent(transform);
            npcObj.transform.position = position;
            
            // Slightly different colors for each NPC
            float hueShift = index * 0.15f;
            Color.RGBToHSV(_npcColor, out float h, out float s, out float v);
            Color npcColor = Color.HSVToRGB((h + hueShift) % 1f, s, v);
            npcObj.GetComponent<Renderer>().material.color = npcColor;
            
            // Add UtilityNPC component
            var npc = npcObj.AddComponent<UtilityNPC>();
            
            // Create patrol waypoints
            var waypointPath = CreatePatrolRoute(name + "_Patrol", position, index);
            npc.SetWaypointPath(waypointPath);
            
            // Add visual indicator on top
            var indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            indicator.name = "Indicator";
            indicator.transform.SetParent(npcObj.transform);
            indicator.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            indicator.transform.localScale = Vector3.one * 0.3f;
            indicator.GetComponent<Renderer>().material.color = Color.white;
            Object.Destroy(indicator.GetComponent<Collider>());
            
            return npc;
        }
        
        private WaypointPath CreatePatrolRoute(string name, Vector3 center, int patrolIndex)
        {
            var container = new GameObject(name);
            container.transform.SetParent(transform);
            
            var waypointPath = container.AddComponent<WaypointPath>();
            var waypoints = new List<Transform>();
            
            float patrolRadius = 5f;
            int waypointCount = 4;
            float angleOffset = patrolIndex * 30f;
            
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
                marker.transform.localScale = Vector3.one * 0.3f;
                marker.GetComponent<Renderer>().material.color = _waypointColor;
                Object.Destroy(marker.GetComponent<Collider>());
            }
            
            waypointPath.SetWaypoints(waypoints);
            return waypointPath;
        }
        
        private void HandleInput()
        {
            // Left click to create interest point at mouse position
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    SpawnInterestPoint(hit.point);
                }
            }
            
            // R to reset all NPC criticality
            if (Input.GetKeyDown(KeyCode.R))
            {
                foreach (var npc in _npcs)
                {
                    if (npc != null)
                    {
                        npc.Criticality?.Reset();
                    }
                }
                Debug.Log("Reset all NPC criticality values");
            }
            
            // Space to spawn random interest point
            if (Input.GetKeyDown(KeyCode.Space))
            {
                SpawnRandomInterestPoint();
            }
        }
        
        private void SpawnRandomInterestPoint()
        {
            float halfSize = _arenaSize / 2f - 2f;
            Vector3 randomPos = new Vector3(
                Random.Range(-halfSize, halfSize),
                0.5f,
                Random.Range(-halfSize, halfSize)
            );
            SpawnInterestPoint(randomPos);
        }
        
        private void SpawnInterestPoint(Vector3 position)
        {
            position.y = 0.5f;
            
            // Create visual
            var interest = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            interest.name = "InterestPoint";
            interest.transform.SetParent(transform);
            interest.transform.position = position;
            interest.transform.localScale = Vector3.one * 0.6f;
            interest.GetComponent<Renderer>().material.color = _interestColor;
            Object.Destroy(interest.GetComponent<Collider>());
            
            // Add a script to track lifetime
            var lifetime = interest.AddComponent<InterestPointLifetime>();
            lifetime.Duration = 12f;
            
            _interestPoints.Add(interest);
            
            // Notify nearby NPCs
            foreach (var npc in _npcs)
            {
                if (npc != null)
                {
                    float distance = Vector3.Distance(npc.transform.position, position);
                    if (distance < 15f)
                    {
                        npc.SetInterestPoint(position);
                    }
                }
            }
        }
        
        private void CleanupInterestPoints()
        {
            _interestPoints.RemoveAll(p => p == null);
        }
        
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 420, 500));
            
            // Title
            GUILayout.BeginVertical("box");
            GUILayout.Label("<size=16><b>NPCBrain Utility AI Demo</b></size>");
            GUILayout.Label("<i>Demonstrating Criticality System</i>");
            GUILayout.EndVertical();
            
            GUILayout.Space(5);
            
            // Controls
            GUILayout.BeginVertical("box");
            GUILayout.Label("<b>Controls:</b>");
            GUILayout.Label("  Left Click - Create interest point");
            GUILayout.Label("  Space - Random interest point");
            GUILayout.Label("  R - Reset all criticality");
            GUILayout.EndVertical();
            
            GUILayout.Space(5);
            
            // Criticality explanation
            GUILayout.BeginVertical("box");
            GUILayout.Label("<b>Criticality System:</b>");
            GUILayout.Label("  Temperature: Controls action randomness");
            GUILayout.Label("    Low (0.5) = Deterministic choices");
            GUILayout.Label("    High (2.0) = Random exploration");
            GUILayout.Label("  Inertia: Tendency to repeat actions");
            GUILayout.Label("    Low = Varied behavior");
            GUILayout.Label("    High = Repetitive behavior");
            GUILayout.EndVertical();
            
            GUILayout.Space(5);
            
            // NPC Status
            GUILayout.BeginVertical("box");
            GUILayout.Label("<b>NPC Status (Utility AI):</b>");
            
            foreach (var npc in _npcs)
            {
                if (npc == null) continue;
                
                var crit = npc.Criticality;
                if (crit == null) continue;
                
                // Temperature color: green at 0.5, yellow at 1.0, red at 2.0
                string tempColor = crit.Temperature < 1f ? "green" : (crit.Temperature < 1.5f ? "yellow" : "red");
                
                // Current action from behavior tree
                string action = "Unknown";
                if (npc.BehaviorTree is NPCBrain.BehaviorTree.Composites.UtilitySelector selector)
                {
                    action = selector.CurrentAction?.Name ?? "Selecting...";
                }
                
                GUILayout.Label($"  {npc.name}:");
                GUILayout.Label($"    Action: <color=cyan>{action}</color>");
                GUILayout.Label($"    Temp: <color={tempColor}>{crit.Temperature:F2}</color> | Inertia: {crit.Inertia:F2} | Energy: {npc.Energy:F1}");
            }
            GUILayout.EndVertical();
            
            GUILayout.Space(5);
            
            // Action variety info
            GUILayout.BeginVertical("box");
            GUILayout.Label("<b>How Criticality Works:</b>");
            GUILayout.Label("  - Each completed action is recorded");
            GUILayout.Label("  - Entropy measures action variety");
            GUILayout.Label("  - Low entropy (repetitive) → Higher temp");
            GUILayout.Label("  - High entropy (varied) → Lower temp");
            GUILayout.Label("  - Creates natural behavior variation!");
            GUILayout.EndVertical();
            
            GUILayout.EndArea();
        }
    }
    
    /// <summary>
    /// Simple component to track interest point lifetime.
    /// </summary>
    public class InterestPointLifetime : MonoBehaviour
    {
        public float Duration = 10f;
        private float _spawnTime;
        
        private void Start()
        {
            _spawnTime = Time.time;
        }
        
        private void Update()
        {
            if (Time.time - _spawnTime > Duration)
            {
                Destroy(gameObject);
            }
            
            // Pulse effect
            float pulse = 1f + Mathf.Sin(Time.time * 3f) * 0.1f;
            transform.localScale = Vector3.one * 0.6f * pulse;
            
            // Fade as lifetime expires
            float remaining = 1f - (Time.time - _spawnTime) / Duration;
            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                Color c = renderer.material.color;
                c.a = remaining;
                renderer.material.color = c;
            }
        }
    }
}
