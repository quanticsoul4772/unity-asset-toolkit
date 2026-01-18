using UnityEngine;
using NPCBrain;

namespace NPCBrain.Demo
{
    /// <summary>
    /// Performance testing utility for NPCBrain.
    /// Spawns multiple NPCs and monitors performance metrics.
    /// </summary>
    public class PerformanceTest : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private GameObject _npcPrefab;
        [SerializeField] private int _npcCount = 100;
        [SerializeField] private float _spawnRadius = 50f;
        [SerializeField] private bool _spawnOnStart = true;
        #pragma warning disable CS0414 // Field is assigned but never used
        [SerializeField] private bool _randomizeArchetypes = false; // Reserved for future use
        #pragma warning restore CS0414

        [Header("Performance Monitoring")]
        [SerializeField] private bool _showStats = true;
        [SerializeField] private KeyCode _spawnKey = KeyCode.Space;
        [SerializeField] private KeyCode _clearKey = KeyCode.C;
        [SerializeField] private KeyCode _increaseKey = KeyCode.Plus;
        [SerializeField] private KeyCode _decreaseKey = KeyCode.Minus;

        [Header("Advanced Options")]
        #pragma warning disable CS0414 // Field is assigned but never used
        [SerializeField] private bool _createNavMesh = false; // Reserved for future use
        #pragma warning restore CS0414
        [SerializeField] private bool _addWaypoints = true;
        [SerializeField] private int _waypointsPerNPC = 4;

        private GameObject[] _npcs;
        private float _avgFrameTime;
        private float _peakFrameTime;
        private int _frameCount;
        private float _startTime;

        void Start()
        {
            if (_spawnOnStart)
            {
                SpawnNPCs();
            }
        }

        void Update()
        {
            // Manual spawn control
            if (Input.GetKeyDown(_spawnKey))
            {
                SpawnNPCs();
            }

            if (Input.GetKeyDown(_clearKey))
            {
                ClearNPCs();
            }

            if (Input.GetKeyDown(_increaseKey))
            {
                _npcCount += 10;
                SpawnNPCs();
            }

            if (Input.GetKeyDown(_decreaseKey))
            {
                _npcCount = Mathf.Max(10, _npcCount - 10);
                SpawnNPCs();
            }

            // Track performance
            float frameTime = Time.deltaTime * 1000f; // Convert to ms
            _avgFrameTime = (_avgFrameTime * _frameCount + frameTime) / (_frameCount + 1);
            _peakFrameTime = Mathf.Max(_peakFrameTime, frameTime);
            _frameCount++;
        }

        void SpawnNPCs()
        {
            ClearNPCs();

            _npcs = new GameObject[_npcCount];

            for (int i = 0; i < _npcCount; i++)
            {
                Vector3 randomPos = new Vector3(
                    Random.Range(-_spawnRadius, _spawnRadius),
                    0.5f,
                    Random.Range(-_spawnRadius, _spawnRadius)
                );

                _npcs[i] = Instantiate(_npcPrefab, randomPos, Quaternion.identity);
                _npcs[i].name = $"NPC_{i:000}";

                // Optionally add waypoints
                if (_addWaypoints)
                {
                    SetupWaypoints(_npcs[i], randomPos);
                }
            }

            Debug.Log($"Spawned {_npcCount} NPCs for performance testing");
            ResetStats();
        }

        void SetupWaypoints(GameObject npc, Vector3 center)
        {
            var brain = npc.GetComponent<NPCBrainController>();
            if (brain == null) return;

            // Create waypoint path
            GameObject waypointRoot = new GameObject($"{npc.name}_Waypoints");
            WaypointPath path = waypointRoot.AddComponent<WaypointPath>();

            for (int i = 0; i < _waypointsPerNPC; i++)
            {
                GameObject waypoint = new GameObject($"Waypoint_{i}");
                waypoint.transform.parent = waypointRoot.transform;

                // Create circular pattern around spawn point
                float angle = (360f / _waypointsPerNPC) * i * Mathf.Deg2Rad;
                float radius = Random.Range(5f, 15f);
                waypoint.transform.position = center + new Vector3(
                    Mathf.Cos(angle) * radius,
                    0,
                    Mathf.Sin(angle) * radius
                );

                path.AddWaypoint(waypoint.transform);
            }

            // Assign to brain
            // Note: This assumes your brain has a public waypointPath field or method
            // Adjust based on your actual implementation
        }

        void ClearNPCs()
        {
            if (_npcs != null)
            {
                foreach (var npc in _npcs)
                {
                    if (npc != null)
                    {
                        // Clean up waypoints
                        var waypointRoot = GameObject.Find($"{npc.name}_Waypoints");
                        if (waypointRoot != null)
                            Destroy(waypointRoot);

                        Destroy(npc);
                    }
                }
            }
            _npcs = null;
        }

        void ResetStats()
        {
            _avgFrameTime = 0;
            _peakFrameTime = 0;
            _frameCount = 0;
            _startTime = Time.time;
        }

        void OnGUI()
        {
            if (!_showStats) return;

            int activeNPCs = _npcs != null ? _npcs.Length : 0;
            float fps = _avgFrameTime > 0 ? 1000f / _avgFrameTime : 0;
            float elapsedTime = Time.time - _startTime;
            float perNPCCost = activeNPCs > 0 ? _avgFrameTime / activeNPCs : 0;

            // Stats box
            GUILayout.BeginArea(new Rect(10, 10, 350, 280));

            GUI.Box(new Rect(0, 0, 350, 280), "");
            GUILayout.Label("<b>NPCBrain Performance Test</b>", new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, richText = true });

            GUILayout.Space(10);

            // Performance metrics
            GUILayout.Label($"<b>Performance Metrics</b>", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, richText = true });
            GUILayout.Label($"Active NPCs: {activeNPCs}");

            // Color-code FPS
            string fpsColor = fps >= 60 ? "green" : fps >= 30 ? "yellow" : "red";
            GUILayout.Label($"FPS: <color={fpsColor}><b>{fps:F1}</b></color> ({_avgFrameTime:F2}ms avg)",
                new GUIStyle(GUI.skin.label) { richText = true });

            GUILayout.Label($"Peak Frame: {_peakFrameTime:F2}ms");

            // Color-code per-NPC cost
            string costColor = perNPCCost < 0.1f ? "green" : perNPCCost < 0.2f ? "yellow" : "red";
            GUILayout.Label($"Per-NPC Cost: <color={costColor}><b>{perNPCCost:F3}ms</b></color> (target: <0.1ms)",
                new GUIStyle(GUI.skin.label) { richText = true });

            GUILayout.Label($"Elapsed Time: {elapsedTime:F1}s");

            GUILayout.Space(10);

            // Status indicators
            GUILayout.Label($"<b>Status</b>", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, richText = true });

            bool fpsPass = fps >= 60 && activeNPCs >= 100;
            bool costPass = perNPCCost < 0.1f;

            string fpsStatus = fpsPass ? "<color=green>✓ PASS</color>" : "<color=red>✗ FAIL</color>";
            string costStatus = costPass ? "<color=green>✓ PASS</color>" : "<color=red>✗ FAIL</color>";

            GUILayout.Label($"100+ NPCs @ 60 FPS: {fpsStatus}", new GUIStyle(GUI.skin.label) { richText = true });
            GUILayout.Label($"Per-NPC < 0.1ms: {costStatus}", new GUIStyle(GUI.skin.label) { richText = true });

            GUILayout.Space(10);

            // Controls
            GUILayout.Label($"<b>Controls</b>", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, richText = true });
            GUILayout.Label($"[{_spawnKey}] Spawn {_npcCount} NPCs");
            GUILayout.Label($"[{_clearKey}] Clear NPCs");
            GUILayout.Label($"[{_increaseKey}] +10 NPCs");
            GUILayout.Label($"[{_decreaseKey}] -10 NPCs");

            GUILayout.EndArea();
        }

        void OnDestroy()
        {
            ClearNPCs();
        }
    }
}
