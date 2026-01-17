using System.Collections.Generic;
using UnityEngine;
using NPCBrain.Archetypes;

namespace NPCBrain.Demo
{
    /// <summary>
    /// Sets up a polished Patrol demo scene with multiple PatrolNPCs on different routes.
    /// Demonstrates simple waypoint-following behavior with parameter variation.
    /// </summary>
    public class PatrolDemoSetup : MonoBehaviour
    {
        [Header("Scene Settings")]
        [SerializeField] private bool _autoGenerate = true;
        [SerializeField] private int _patrollerCount = 4;
        [SerializeField] private float _arenaSize = 25f;
        
        [Header("Colors")]
        [SerializeField] private Color _groundColor = new Color(0.2f, 0.3f, 0.2f);
        [SerializeField] private Color[] _patrollerColors = new Color[]
        {
            new Color(0.2f, 0.5f, 0.9f),
            new Color(0.9f, 0.5f, 0.2f),
            new Color(0.5f, 0.2f, 0.9f),
            new Color(0.9f, 0.9f, 0.2f)
        };
        
        [Header("References (auto-populated)")]
        [SerializeField] private List<PatrolNPC> _patrollers = new List<PatrolNPC>();
        
        private void Start()
        {
            if (_autoGenerate)
            {
                GenerateScene();
            }
        }
        
        /// <summary>
        /// Generates the complete Patrol demo scene.
        /// </summary>
        [ContextMenu("Generate Patrol Demo")]
        public void GenerateScene()
        {
            ClearScene();
            CreateGround();
            CreateDecorations();
            CreatePatrollers();
            
            Debug.Log("Patrol Demo generated! Watch the NPCs patrol their routes with varied timing.");
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
            _patrollers.Clear();
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
            
            // Add grid pattern for visual reference
            CreateGridLines();
        }
        
        private void CreateGridLines()
        {
            var gridContainer = new GameObject("GridLines");
            gridContainer.transform.SetParent(transform);
            
            float halfSize = _arenaSize / 2f;
            float spacing = 5f;
            Color gridColor = _groundColor * 0.7f;
            
            for (float x = -halfSize; x <= halfSize; x += spacing)
            {
                var lineX = GameObject.CreatePrimitive(PrimitiveType.Cube);
                lineX.name = "GridLineX";
                lineX.transform.SetParent(gridContainer.transform);
                lineX.transform.position = new Vector3(x, 0.01f, 0f);
                lineX.transform.localScale = new Vector3(0.05f, 0.01f, _arenaSize);
                lineX.GetComponent<Renderer>().material.color = gridColor;
                Object.Destroy(lineX.GetComponent<Collider>());
            }
            
            for (float z = -halfSize; z <= halfSize; z += spacing)
            {
                var lineZ = GameObject.CreatePrimitive(PrimitiveType.Cube);
                lineZ.name = "GridLineZ";
                lineZ.transform.SetParent(gridContainer.transform);
                lineZ.transform.position = new Vector3(0f, 0.01f, z);
                lineZ.transform.localScale = new Vector3(_arenaSize, 0.01f, 0.05f);
                lineZ.GetComponent<Renderer>().material.color = gridColor;
                Object.Destroy(lineZ.GetComponent<Collider>());
            }
        }
        
        private void CreateDecorations()
        {
            var decorContainer = new GameObject("Decorations");
            decorContainer.transform.SetParent(transform);
            
            // Create some decorative elements
            float halfSize = _arenaSize / 2f - 2f;
            
            // Corner posts
            CreateDecorPost(decorContainer.transform, new Vector3(halfSize, 0f, halfSize));
            CreateDecorPost(decorContainer.transform, new Vector3(-halfSize, 0f, halfSize));
            CreateDecorPost(decorContainer.transform, new Vector3(halfSize, 0f, -halfSize));
            CreateDecorPost(decorContainer.transform, new Vector3(-halfSize, 0f, -halfSize));
            
            // Center feature
            CreateCenterFeature(decorContainer.transform);
        }
        
        private void CreateDecorPost(Transform parent, Vector3 position)
        {
            var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.name = "Post";
            post.transform.SetParent(parent);
            post.transform.position = position + Vector3.up * 1f;
            post.transform.localScale = new Vector3(0.3f, 1f, 0.3f);
            post.GetComponent<Renderer>().material.color = new Color(0.4f, 0.35f, 0.3f);
            post.isStatic = true;
            
            // Light on top
            var light = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            light.name = "Light";
            light.transform.SetParent(post.transform);
            light.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            light.transform.localScale = new Vector3(1.5f, 0.5f, 1.5f);
            light.GetComponent<Renderer>().material.color = new Color(1f, 0.95f, 0.8f);
            Object.Destroy(light.GetComponent<Collider>());
        }
        
        private void CreateCenterFeature(Transform parent)
        {
            var center = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            center.name = "CenterFeature";
            center.transform.SetParent(parent);
            center.transform.position = new Vector3(0f, 0.25f, 0f);
            center.transform.localScale = new Vector3(3f, 0.25f, 3f);
            center.GetComponent<Renderer>().material.color = new Color(0.35f, 0.35f, 0.4f);
            center.isStatic = true;
            
            var fountain = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fountain.name = "Fountain";
            fountain.transform.SetParent(center.transform);
            fountain.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            fountain.transform.localScale = new Vector3(0.5f, 2f, 0.5f);
            fountain.GetComponent<Renderer>().material.color = new Color(0.5f, 0.7f, 0.9f);
            Object.Destroy(fountain.GetComponent<Collider>());
        }
        
        private void CreatePatrollers()
        {
            int count = Mathf.Min(_patrollerCount, _patrollerColors.Length);
            
            // Different patrol patterns
            PatrolPattern[] patterns = new PatrolPattern[]
            {
                PatrolPattern.Square,
                PatrolPattern.Diamond,
                PatrolPattern.Circle,
                PatrolPattern.Line
            };
            
            for (int i = 0; i < count; i++)
            {
                var patroller = CreatePatroller($"Patroller_{i}", i, patterns[i % patterns.Length]);
                _patrollers.Add(patroller);
            }
        }
        
        private enum PatrolPattern { Square, Diamond, Circle, Line }
        
        private PatrolNPC CreatePatroller(string name, int index, PatrolPattern pattern)
        {
            Color color = _patrollerColors[index % _patrollerColors.Length];
            
            // Create patroller visual
            var patrollerObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            patrollerObj.name = name;
            patrollerObj.transform.SetParent(transform);
            patrollerObj.GetComponent<Renderer>().material.color = color;
            
            // Add patroller component
            var patroller = patrollerObj.AddComponent<PatrolNPC>();
            
            // Create patrol waypoints
            var waypointPath = CreatePatrolRoute(name + "_Route", index, pattern, color);
            patroller.SetWaypointPath(waypointPath);
            
            // Position at first waypoint
            if (waypointPath.WaypointCount > 0)
            {
                patrollerObj.transform.position = waypointPath.GetCurrent() + Vector3.up * 0.1f;
            }
            
            // Add visual indicator showing patrol number
            var indicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
            indicator.name = "NumberIndicator";
            indicator.transform.SetParent(patrollerObj.transform);
            indicator.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            indicator.transform.localScale = new Vector3(0.3f, 0.3f, 0.1f);
            indicator.GetComponent<Renderer>().material.color = Color.white;
            Object.Destroy(indicator.GetComponent<Collider>());
            
            return patroller;
        }
        
        private WaypointPath CreatePatrolRoute(string name, int index, PatrolPattern pattern, Color color)
        {
            var container = new GameObject(name);
            container.transform.SetParent(transform);
            
            var waypointPath = container.AddComponent<WaypointPath>();
            var waypoints = new List<Transform>();
            
            float halfSize = _arenaSize / 2f - 3f;
            Vector3[] positions;
            
            // Offset each patrol route to different quadrants
            Vector3 offset = GetQuadrantOffset(index, halfSize * 0.5f);
            float routeSize = halfSize * 0.4f;
            
            switch (pattern)
            {
                case PatrolPattern.Square:
                    positions = new Vector3[]
                    {
                        offset + new Vector3(-routeSize, 0f, -routeSize),
                        offset + new Vector3(-routeSize, 0f, routeSize),
                        offset + new Vector3(routeSize, 0f, routeSize),
                        offset + new Vector3(routeSize, 0f, -routeSize)
                    };
                    break;
                    
                case PatrolPattern.Diamond:
                    positions = new Vector3[]
                    {
                        offset + new Vector3(0f, 0f, routeSize),
                        offset + new Vector3(routeSize, 0f, 0f),
                        offset + new Vector3(0f, 0f, -routeSize),
                        offset + new Vector3(-routeSize, 0f, 0f)
                    };
                    break;
                    
                case PatrolPattern.Circle:
                    int circlePoints = 6;
                    positions = new Vector3[circlePoints];
                    for (int i = 0; i < circlePoints; i++)
                    {
                        float angle = (360f / circlePoints * i) * Mathf.Deg2Rad;
                        positions[i] = offset + new Vector3(
                            Mathf.Cos(angle) * routeSize,
                            0f,
                            Mathf.Sin(angle) * routeSize
                        );
                    }
                    break;
                    
                case PatrolPattern.Line:
                default:
                    positions = new Vector3[]
                    {
                        offset + new Vector3(-routeSize, 0f, 0f),
                        offset + new Vector3(routeSize, 0f, 0f)
                    };
                    break;
            }
            
            for (int i = 0; i < positions.Length; i++)
            {
                Vector3 pos = positions[i];
                pos.y = 0.1f;
                
                // Clamp to bounds
                float maxPos = _arenaSize / 2f - 1f;
                pos.x = Mathf.Clamp(pos.x, -maxPos, maxPos);
                pos.z = Mathf.Clamp(pos.z, -maxPos, maxPos);
                
                var waypoint = new GameObject($"Waypoint_{i}");
                waypoint.transform.SetParent(container.transform);
                waypoint.transform.position = pos;
                waypoints.Add(waypoint.transform);
                
                // Visual marker with matching color
                var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.name = "Marker";
                marker.transform.SetParent(waypoint.transform);
                marker.transform.localPosition = Vector3.zero;
                marker.transform.localScale = Vector3.one * 0.35f;
                marker.GetComponent<Renderer>().material.color = color * 0.6f;
                Object.Destroy(marker.GetComponent<Collider>());
            }
            
            waypointPath.SetWaypoints(waypoints);
            return waypointPath;
        }
        
        private Vector3 GetQuadrantOffset(int index, float distance)
        {
            switch (index % 4)
            {
                case 0: return new Vector3(distance, 0f, distance);
                case 1: return new Vector3(-distance, 0f, distance);
                case 2: return new Vector3(-distance, 0f, -distance);
                case 3: return new Vector3(distance, 0f, -distance);
                default: return Vector3.zero;
            }
        }
        
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 350, 450));
            
            // Title
            GUILayout.BeginVertical("box");
            GUILayout.Label("<size=16><b>NPCBrain Patrol Demo</b></size>");
            GUILayout.Label("<i>Demonstrating PatrolNPC archetype</i>");
            GUILayout.EndVertical();
            
            GUILayout.Space(5);
            
            // Explanation
            GUILayout.BeginVertical("box");
            GUILayout.Label("<b>Patrol Behavior:</b>");
            GUILayout.Label("  1. Move to current waypoint");
            GUILayout.Label("  2. Wait (with random variation)");
            GUILayout.Label("  3. Advance to next waypoint");
            GUILayout.Label("  4. Repeat endlessly");
            GUILayout.EndVertical();
            
            GUILayout.Space(5);
            
            // Patroller info
            GUILayout.BeginVertical("box");
            GUILayout.Label("<b>Patrollers:</b>");
            for (int i = 0; i < _patrollers.Count; i++)
            {
                var patroller = _patrollers[i];
                if (patroller == null) continue;
                
                Color color = _patrollerColors[i % _patrollerColors.Length];
                string colorHex = ColorUtility.ToHtmlStringRGB(color);
                
                string status = patroller.IsPaused ? "Paused" : "Active";
                int waypointIndex = patroller.WaypointPath?.CurrentIndex ?? 0;
                
                GUILayout.Label($"  <color=#{colorHex}>●</color> {patroller.name}: {status} (WP: {waypointIndex})");
                GUILayout.Label($"      Speed: {patroller.PatrolSpeed:F1} | Wait: {patroller.WaitTime:F1}s");
            }
            GUILayout.EndVertical();
            
            GUILayout.Space(5);
            
            // Notes
            GUILayout.BeginVertical("box");
            GUILayout.Label("<b>Features:</b>");
            GUILayout.Label("  • Each patroller has unique route pattern");
            GUILayout.Label("  • Random wait time variation at waypoints");
            GUILayout.Label("  • Color-coded routes for easy tracking");
            GUILayout.Label("  • Waypoint gizmos visible in Scene view");
            GUILayout.EndVertical();
            
            GUILayout.EndArea();
        }
    }
}
