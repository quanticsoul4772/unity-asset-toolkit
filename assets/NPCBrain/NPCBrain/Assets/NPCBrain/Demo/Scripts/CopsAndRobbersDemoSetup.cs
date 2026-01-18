using System.Collections.Generic;
using UnityEngine;
using NPCBrain.Archetypes;
using NPCBrain.Components;
using NPCBrain.Perception;
using NPCBrain.BehaviorTree.Composites;

namespace NPCBrain.Demo
{
    /// <summary>
    /// Sets up a unified Cops and Robbers demo showcasing all NPC archetypes.
    /// Robbers try to steal loot and escape while cops patrol and arrest them.
    /// </summary>
    public class CopsAndRobbersDemoSetup : MonoBehaviour
    {
        [Header("Scene Settings")]
        [SerializeField] private bool _autoGenerate = true;
        [SerializeField] private float _arenaSize = 40f;
        
        [Header("NPC Counts")]
        [SerializeField] private int _copCount = 3;
        [SerializeField] private int _robberCount = 2;
        [SerializeField] private int _lootCount = 4;
        
        [Header("Colors")]
        [SerializeField] private Color _groundColor = new Color(0.2f, 0.22f, 0.25f);
        [SerializeField] private Color _copColor = new Color(0.2f, 0.4f, 0.8f);
        [SerializeField] private Color _robberColor = new Color(0.15f, 0.15f, 0.15f);
        [SerializeField] private Color _wallColor = new Color(0.35f, 0.35f, 0.4f);
        [SerializeField] private Color _bankColor = new Color(0.5f, 0.4f, 0.3f);
        [SerializeField] private Color _escapeColor = new Color(0.2f, 0.8f, 0.2f);
        
        [Header("References (auto-populated)")]
        [SerializeField] private List<CopNPC> _cops = new List<CopNPC>();
        [SerializeField] private List<RobberNPC> _robbers = new List<RobberNPC>();
        [SerializeField] private List<LootPoint> _lootPoints = new List<LootPoint>();
        [SerializeField] private EscapeZone _escapeZone;
        
        // Scoring
        private int _copScore;
        private int _robberScore;
        private float _gameTime;
        private bool _gameEnded;
        private string _winner;
        
        private void Start()
        {
            if (_autoGenerate)
            {
                GenerateScene();
            }
        }
        
        private void Update()
        {
            if (!_gameEnded)
            {
                _gameTime += Time.deltaTime;
                CheckGameEnd();
            }
        }
        
        private void CheckGameEnd()
        {
            // Check if all robbers are captured or escaped
            int activeRobbers = 0;
            foreach (var robber in _robbers)
            {
                if (robber != null && robber.gameObject.activeSelf && !robber.HasEscaped)
                {
                    activeRobbers++;
                }
            }
            
            if (activeRobbers == 0)
            {
                _gameEnded = true;
                _winner = _robberScore > _copScore ? "ROBBERS WIN!" : "COPS WIN!";
                Debug.Log($"<color=yellow>[CopsAndRobbers] Game Over! {_winner}</color>");
            }
        }
        
        /// <summary>
        /// Generates the complete Cops and Robbers demo scene.
        /// </summary>
        [ContextMenu("Generate Cops and Robbers Demo")]
        public void GenerateScene()
        {
            ClearScene();
            CreateGround();
            CreateWalls();
            CreateBank();
            CreateStreets();
            CreateEscapeZone();
            CreateLootPoints();
            CreateCoverPoints();
            CreateCops();
            CreateRobbers();
            
            Debug.Log("<color=cyan>Cops and Robbers Demo generated!</color>\n" +
                "Watch the AI battle it out! Robbers steal loot and escape, Cops patrol and arrest.");
        }
        
        /// <summary>
        /// Restarts the game.
        /// </summary>
        [ContextMenu("Restart Game")]
        public void RestartGame()
        {
            _copScore = 0;
            _robberScore = 0;
            _gameTime = 0f;
            _gameEnded = false;
            _winner = "";
            
            GenerateScene();
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
            
            // Unsubscribe from events before clearing
            foreach (var cop in _cops)
            {
                if (cop != null) cop.OnArrest -= OnCopArrest;
            }
            foreach (var loot in _lootPoints)
            {
                if (loot != null) loot.OnStolen -= OnLootStolen;
            }
            if (_escapeZone != null)
            {
                _escapeZone.OnRobberEscaped -= OnRobberEscaped;
            }
            
            _cops.Clear();
            _robbers.Clear();
            _lootPoints.Clear();
            _escapeZone = null;
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from all events to prevent memory leaks
            foreach (var cop in _cops)
            {
                if (cop != null) cop.OnArrest -= OnCopArrest;
            }
            foreach (var loot in _lootPoints)
            {
                if (loot != null) loot.OnStolen -= OnLootStolen;
            }
            if (_escapeZone != null)
            {
                _escapeZone.OnRobberEscaped -= OnRobberEscaped;
            }
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
            
            // Add road markings
            CreateRoadMarkings();
        }
        
        private void CreateRoadMarkings()
        {
            // Horizontal road
            var road1 = GameObject.CreatePrimitive(PrimitiveType.Plane);
            road1.name = "Road_Horizontal";
            road1.transform.SetParent(transform);
            road1.transform.position = new Vector3(0f, 0.01f, 0f);
            road1.transform.localScale = new Vector3(_arenaSize / 10f, 1f, 0.5f);
            road1.GetComponent<Renderer>().material.color = new Color(0.25f, 0.25f, 0.28f);
            Object.Destroy(road1.GetComponent<Collider>());
            
            // Vertical road
            var road2 = GameObject.CreatePrimitive(PrimitiveType.Plane);
            road2.name = "Road_Vertical";
            road2.transform.SetParent(transform);
            road2.transform.position = new Vector3(0f, 0.01f, 0f);
            road2.transform.localScale = new Vector3(0.5f, 1f, _arenaSize / 10f);
            road2.GetComponent<Renderer>().material.color = new Color(0.25f, 0.25f, 0.28f);
            Object.Destroy(road2.GetComponent<Collider>());
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
            wall.GetComponent<Renderer>().material.color = _wallColor;
            wall.isStatic = true;
        }
        
        private void CreateBank()
        {
            float halfSize = _arenaSize / 2f;
            
            // Bank building (northeast quadrant)
            var bankContainer = new GameObject("Bank");
            bankContainer.transform.SetParent(transform);
            bankContainer.transform.position = new Vector3(halfSize * 0.5f, 0f, halfSize * 0.5f);
            
            // Main bank building
            var bankMain = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bankMain.name = "BankMain";
            bankMain.transform.SetParent(bankContainer.transform);
            bankMain.transform.localPosition = Vector3.zero;
            bankMain.transform.localScale = new Vector3(10f, 4f, 8f);
            bankMain.GetComponent<Renderer>().material.color = _bankColor;
            bankMain.transform.position += Vector3.up * 2f;
            bankMain.isStatic = true;
            
            // Bank sign
            var sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sign.name = "BankSign";
            sign.transform.SetParent(bankContainer.transform);
            sign.transform.localPosition = new Vector3(0f, 4.5f, -4.1f);
            sign.transform.localScale = new Vector3(4f, 1f, 0.2f);
            sign.GetComponent<Renderer>().material.color = new Color(0.8f, 0.7f, 0.2f);
            Object.Destroy(sign.GetComponent<Collider>());
            
            // Vault (inside bank, where loot spawns)
            var vault = GameObject.CreatePrimitive(PrimitiveType.Cube);
            vault.name = "Vault";
            vault.transform.SetParent(bankContainer.transform);
            vault.transform.localPosition = new Vector3(0f, 0.5f, 2f);
            vault.transform.localScale = new Vector3(4f, 1f, 3f);
            vault.GetComponent<Renderer>().material.color = new Color(0.3f, 0.3f, 0.35f);
            vault.isStatic = true;
        }
        
        private void CreateStreets()
        {
            float halfSize = _arenaSize / 2f;
            
            // Street obstacles / buildings in other quadrants
            
            // Southwest - Small shops
            CreateBuilding("Shop1", new Vector3(-halfSize * 0.6f, 0f, -halfSize * 0.6f), new Vector3(5f, 3f, 5f));
            CreateBuilding("Shop2", new Vector3(-halfSize * 0.3f, 0f, -halfSize * 0.7f), new Vector3(4f, 2.5f, 4f));
            
            // Northwest - Warehouse
            CreateBuilding("Warehouse", new Vector3(-halfSize * 0.5f, 0f, halfSize * 0.5f), new Vector3(8f, 4f, 6f));
            
            // Southeast - Parking lot pillars
            for (int i = 0; i < 4; i++)
            {
                CreatePillar($"Pillar_{i}", new Vector3(halfSize * 0.4f + i * 3f, 1.5f, -halfSize * 0.5f));
            }
        }
        
        private void CreateBuilding(string name, Vector3 position, Vector3 size)
        {
            var building = GameObject.CreatePrimitive(PrimitiveType.Cube);
            building.name = name;
            building.transform.SetParent(transform);
            building.transform.position = position + Vector3.up * (size.y / 2f);
            building.transform.localScale = size;
            building.GetComponent<Renderer>().material.color = _wallColor * 0.9f;
            building.isStatic = true;
        }
        
        private void CreatePillar(string name, Vector3 position)
        {
            var pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.name = name;
            pillar.transform.SetParent(transform);
            pillar.transform.position = position;
            pillar.transform.localScale = new Vector3(0.5f, 1.5f, 0.5f);
            pillar.GetComponent<Renderer>().material.color = _wallColor;
            pillar.isStatic = true;
        }
        
        private void CreateEscapeZone()
        {
            float halfSize = _arenaSize / 2f;
            
            // Escape zone in the south
            _escapeZone = EscapeZone.Create(
                new Vector3(0f, 0.1f, -halfSize + 5f),
                5f,
                transform
            );
            
            // Subscribe to escape events
            _escapeZone.OnRobberEscaped += OnRobberEscaped;
        }
        
        private void OnRobberEscaped(GameObject robber, int lootValue)
        {
            _robberScore += lootValue;
        }
        
        private void CreateLootPoints()
        {
            float halfSize = _arenaSize / 2f;
            
            // Bank vault loot (high value)
            Vector3 bankCenter = new Vector3(halfSize * 0.5f, 0.5f, halfSize * 0.5f);
            
            // Define loot positions and values
            var lootDefinitions = new (Vector3 position, int value, string name)[]
            {
                (bankCenter + new Vector3(0f, 0f, 0f), 500, "MainVaultLoot"),
                (bankCenter + new Vector3(-2f, 0f, 1f), 200, "SideLoot1"),
                (new Vector3(-halfSize * 0.6f, 0.5f, -halfSize * 0.6f), 100, "ShopLoot1"),
                (new Vector3(-halfSize * 0.3f, 0.5f, -halfSize * 0.7f), 100, "ShopLoot2"),
            };
            
            // Create loot points up to the configured count
            for (int i = 0; i < Mathf.Min(_lootCount, lootDefinitions.Length); i++)
            {
                var def = lootDefinitions[i];
                var loot = LootPoint.Create(def.position, def.value, transform);
                loot.name = def.name;
                _lootPoints.Add(loot);
                loot.OnStolen += OnLootStolen;
            }
        }
        
        private void OnLootStolen(LootPoint loot, GameObject thief)
        {
            // Alarm is automatically emitted by LootPoint
        }
        
        private void CreateCoverPoints()
        {
            float halfSize = _arenaSize / 2f;
            
            // Create cover points around the map
            Vector3[] coverPositions = new Vector3[]
            {
                new Vector3(-halfSize * 0.3f, 0f, halfSize * 0.3f),
                new Vector3(halfSize * 0.3f, 0f, -halfSize * 0.3f),
                new Vector3(-halfSize * 0.7f, 0f, 0f),
                new Vector3(0f, 0f, halfSize * 0.7f),
                new Vector3(halfSize * 0.7f, 0f, halfSize * 0.2f),
            };
            
            for (int i = 0; i < coverPositions.Length; i++)
            {
                var cover = CoverPoint.Create(coverPositions[i], transform);
                cover.name = $"CoverPoint_{i}";
            }
        }
        
        private void CreateCops()
        {
            float halfSize = _arenaSize / 2f;
            
            Vector3[] copPositions = new Vector3[]
            {
                new Vector3(halfSize * 0.3f, 0.1f, halfSize * 0.8f),   // Near bank
                new Vector3(-halfSize * 0.5f, 0.1f, halfSize * 0.3f),  // West patrol
                new Vector3(halfSize * 0.7f, 0.1f, -halfSize * 0.3f),  // East patrol
                new Vector3(-halfSize * 0.3f, 0.1f, -halfSize * 0.5f), // South patrol
            };
            
            for (int i = 0; i < Mathf.Min(_copCount, copPositions.Length); i++)
            {
                var cop = CreateCop($"Cop_{i}", copPositions[i], i);
                _cops.Add(cop);
            }
        }
        
        private CopNPC CreateCop(string name, Vector3 position, int patrolIndex)
        {
            var cop = CopNPC.Create(position, transform);
            cop.name = name;
            cop.GetComponent<Renderer>().material.color = _copColor;
            
            // Create patrol route
            var waypointPath = CreateCopPatrol(name + "_Patrol", position, patrolIndex);
            cop.SetWaypointPath(waypointPath);
            
            // Subscribe to arrest events
            cop.OnArrest += OnCopArrest;
            
            return cop;
        }
        
        private void OnCopArrest(CopNPC cop, RobberNPC robber)
        {
            _copScore += 100 + robber.CarriedLootValue;
        }
        
        private WaypointPath CreateCopPatrol(string name, Vector3 center, int patrolIndex)
        {
            var container = new GameObject(name);
            container.transform.SetParent(transform);
            
            var waypointPath = container.AddComponent<WaypointPath>();
            var waypoints = new List<Transform>();
            
            float patrolRadius = 8f;
            int waypointCount = 4;
            float angleOffset = patrolIndex * 30f;
            
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
                
                // Visual marker (small, blue)
                var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.name = "Marker";
                marker.transform.SetParent(waypoint.transform);
                marker.transform.localPosition = Vector3.zero;
                marker.transform.localScale = Vector3.one * 0.25f;
                marker.GetComponent<Renderer>().material.color = new Color(0.3f, 0.5f, 1f, 0.3f);
                Object.Destroy(marker.GetComponent<Collider>());
            }
            
            waypointPath.SetWaypoints(waypoints);
            return waypointPath;
        }
        
        private void CreateRobbers()
        {
            float halfSize = _arenaSize / 2f;
            
            // Robbers spawn near the escape zone (south)
            Vector3[] robberPositions = new Vector3[]
            {
                new Vector3(-3f, 0.1f, -halfSize + 8f),
                new Vector3(3f, 0.1f, -halfSize + 8f),
                new Vector3(0f, 0.1f, -halfSize + 10f),
            };
            
            for (int i = 0; i < Mathf.Min(_robberCount, robberPositions.Length); i++)
            {
                var robber = CreateRobber($"Robber_{i}", robberPositions[i]);
                _robbers.Add(robber);
            }
        }
        
        private RobberNPC CreateRobber(string name, Vector3 position)
        {
            var robber = RobberNPC.Create(position, transform);
            robber.name = name;
            robber.GetComponent<Renderer>().material.color = _robberColor;
            
            return robber;
        }
        
        private void OnGUI()
        {
            // Main panel
            GUILayout.BeginArea(new Rect(10, 10, 480, 700));
            
            // Title and score
            GUILayout.BeginVertical("box");
            GUILayout.Label("<size=18><b>🚔 COPS AND ROBBERS 🎭</b></size>");
            GUILayout.Label("<i>NPCBrain Unified Demo - All Archetypes</i>");
            GUILayout.Space(5);
            
            string timeStr = System.TimeSpan.FromSeconds(_gameTime).ToString(@"mm\:ss");
            GUILayout.Label($"<b>Time:</b> {timeStr}");
            
            GUILayout.BeginHorizontal();
            GUILayout.Label($"<color=#4488FF><b>COPS: ${_copScore}</b></color>", GUILayout.Width(150));
            GUILayout.Label($"<color=#444444><b>ROBBERS: ${_robberScore}</b></color>");
            GUILayout.EndHorizontal();
            
            if (_gameEnded)
            {
                GUILayout.Label($"<size=16><color=yellow><b>{_winner}</b></color></size>");
            }
            GUILayout.EndVertical();
            
            GUILayout.Space(5);
            
            // Cops section
            GUILayout.BeginVertical("box");
            GUILayout.Label("<b>👮 COPS (Utility AI + Hearing + Sight)</b>");
            foreach (var cop in _cops)
            {
                if (cop == null) continue;
                DrawNPCStatus(cop, _copColor);
            }
            GUILayout.EndVertical();
            
            GUILayout.Space(5);
            
            // Robbers section
            GUILayout.BeginVertical("box");
            GUILayout.Label("<b>🎭 ROBBERS (Utility AI + Evasion)</b>");
            foreach (var robber in _robbers)
            {
                if (robber == null) continue;
                DrawRobberStatus(robber);
            }
            GUILayout.EndVertical();
            
            GUILayout.Space(5);
            
            // Loot status
            GUILayout.BeginVertical("box");
            GUILayout.Label("<b>💰 LOOT STATUS</b>");
            foreach (var loot in _lootPoints)
            {
                if (loot == null) continue;
                string status = loot.IsStolen ? "<color=red>STOLEN</color>" : "<color=green>Available</color>";
                GUILayout.Label($"  {loot.name}: ${loot.Value} - {status}");
            }
            GUILayout.EndVertical();
            
            GUILayout.Space(5);
            
            // Criticality legend
            GUILayout.BeginVertical("box");
            GUILayout.Label("<b>📊 CRITICALITY SYSTEM</b>");
            GUILayout.Label("  <b>T</b> = Temperature (exploration vs exploitation)");
            GUILayout.Label("    <color=green>Low</color> = Deterministic, picks best action");
            GUILayout.Label("    <color=red>High</color> = Random, explores alternatives");
            GUILayout.Label("  <b>I</b> = Inertia (tendency to repeat actions)");
            GUILayout.EndVertical();
            
            GUILayout.Space(5);
            
            // Controls
            GUILayout.BeginVertical("box");
            GUILayout.Label("<b>🎮 CONTROLS</b>");
            if (GUILayout.Button("Restart Game"))
            {
                RestartGame();
            }
            GUILayout.Label("  Drag camera with mouse to observe");
            GUILayout.Label("  All AI is fully autonomous!");
            GUILayout.EndVertical();
            
            GUILayout.EndArea();
        }
        
        private void DrawNPCStatus(CopNPC cop, Color color)
        {
            string state = cop.CurrentState;
            float alert = cop.AlertLevel;
            
            string stateColor = GetStateColor(state);
            
            // Get current action from UtilitySelector
            string actionName = "(selecting)";
            if (cop.BehaviorTree is UtilitySelector selector && selector.CurrentAction != null)
            {
                actionName = selector.CurrentAction.Name;
            }
            
            // Criticality info
            string critInfo = GetCriticalityInfo(cop);
            
            GUILayout.Label($"  <b>{cop.name}</b>: <color={stateColor}>{actionName}</color>{critInfo}");
            GUILayout.Label($"      Alert: {alert:F2} | Arrests: {cop.ArrestCount}");
        }
        
        private void DrawRobberStatus(RobberNPC robber)
        {
            if (!robber.gameObject.activeSelf)
            {
                string status = robber.HasEscaped ? "<color=green>ESCAPED!</color>" : "<color=red>ARRESTED!</color>";
                GUILayout.Label($"  <b>{robber.name}</b>: {status}");
                return;
            }
            
            string state = robber.CurrentState;
            string stateColor = GetStateColor(state);
            
            // Get current action from UtilitySelector
            string actionName = "(selecting)";
            if (robber.BehaviorTree is UtilitySelector selector && selector.CurrentAction != null)
            {
                actionName = selector.CurrentAction.Name;
            }
            
            // Criticality info
            string critInfo = GetCriticalityInfo(robber);
            
            string lootInfo = robber.IsCarryingLoot ? $" 💰${robber.CarriedLootValue}" : "";
            string fearInfo = robber.FearLevel > 0.3f ? " 😰" : "";
            
            GUILayout.Label($"  <b>{robber.name}</b>: <color={stateColor}>{actionName}</color>{critInfo}{lootInfo}{fearInfo}");
            GUILayout.Label($"      Fear: {robber.FearLevel:F2} | CanSeeCop: {robber.CanSeeCop}");
        }
        
        private string GetStateColor(string state)
        {
            if (state.Contains("Arrest") || state.Contains("Chase") || state.Contains("Flee"))
                return "red";
            if (state.Contains("Investigate") || state.Contains("Steal") || state.Contains("Escape"))
                return "yellow";
            if (state.Contains("Hide") || state.Contains("Sneak"))
                return "cyan";
            if (state.Contains("Return"))
                return "cyan";
            return "green";
        }
        
        private string GetCriticalityInfo(NPCBrainController npc)
        {
            if (npc.Criticality == null) return "";
            
            float temp = npc.Criticality.Temperature;
            float inertia = npc.Criticality.Inertia;
            string tempColor = temp < 1f ? "green" : (temp < 1.5f ? "yellow" : "red");
            
            return $" T:<color={tempColor}>{temp:F1}</color> I:{inertia:F1}";
        }
    }
}
