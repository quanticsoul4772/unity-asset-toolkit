using System.Collections.Generic;
using UnityEngine;
using NPCBrain.Archetypes;
using NPCBrain.Components;
using NPCBrain.Perception;
using NPCBrain.BehaviorTree.Composites;
using NPCBrain.Debugging;
using EasyPath;

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
        [SerializeField] private float _arenaSize = 60f;
        
        [Header("NPC Counts")]
        [SerializeField] private int _copCount = 4;
        [SerializeField] private int _robberCount = 1;
        [SerializeField] private int _lootCount = 6;
        
        [Header("Colors")]
        [SerializeField] private Color _groundColor = new Color(0.2f, 0.22f, 0.25f);
        [SerializeField] private Color _copColor = new Color(0.2f, 0.4f, 0.8f);
        [SerializeField] private Color _robberColor = new Color(0.15f, 0.15f, 0.15f);
        [SerializeField] private Color _wallColor = new Color(0.35f, 0.35f, 0.4f);
        [SerializeField] private Color _bankColor = new Color(0.5f, 0.4f, 0.3f);
        [SerializeField] private Color _escapeColor = new Color(0.2f, 0.8f, 0.2f);
        
        [Header("Pathfinding")]
        [SerializeField] private float _gridCellSize = 1f;
        [SerializeField] private bool _showPathfindingDebug = false;
        [SerializeField] private bool _showNPCPaths = true;
        
        [Header("References (auto-populated)")]
        [SerializeField] private List<CopNPC> _cops = new List<CopNPC>();
        [SerializeField] private List<RobberNPC> _robbers = new List<RobberNPC>();
        [SerializeField] private List<LootPoint> _lootPoints = new List<LootPoint>();
        [SerializeField] private EscapeZone _escapeZone;
        private EasyPathGrid _pathfindingGrid;
        private NPCPathVisualizer _pathVisualizer;
        
        // Layer for obstacles (used by pathfinding)
        // NOTE: Layer 8 should be named "Obstacles" in Unity (Edit → Project Settings → Tags and Layers)
        // If the layer doesn't exist, obstacles will be placed on Default layer and pathfinding may not work correctly
        private int _obstacleLayer = 8;
        private bool _layerValidated;
        
        // Scoring
        private int _copScore;
        private int _robberScore;
        private float _gameTime;
        private bool _gameEnded;
        private string _winner;
        
        private void Start()
        {
            Debug.Log("<color=lime>[CopsAndRobbersDemoSetup] START - Scripts are loaded and running!</color>");
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
            // Use for loop to avoid enumerator allocation
            int activeRobbers = 0;
            for (int i = 0; i < _robbers.Count; i++)
            {
                var robber = _robbers[i];
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
            ValidateObstacleLayer();  // Check layer exists before creating obstacles
            CreateGround();
            CreateWalls();
            CreateBank();
            CreateStreets();
            CreateEscapeZone();
            CreateLootPoints();
            CreateCoverPoints();
            CreatePathfindingGrid();  // Create grid AFTER all obstacles
            CreateCops();
            CreateRobbers();
            CreatePathVisualizer();  // Create visualizer for path debug
            
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
            _pathfindingGrid = null;
            _pathVisualizer = null;
            NPCPathVisualizer.ClearAllPaths();
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
            float wallHeight = 6f;  // Taller walls to prevent escape
            float wallThickness = 1.5f;  // Thicker walls for better collision
            
            CreateWall("WallNorth", new Vector3(0f, wallHeight / 2f, halfSize), new Vector3(_arenaSize + wallThickness * 2, wallHeight, wallThickness));
            CreateWall("WallSouth", new Vector3(0f, wallHeight / 2f, -halfSize), new Vector3(_arenaSize + wallThickness * 2, wallHeight, wallThickness));
            CreateWall("WallEast", new Vector3(halfSize, wallHeight / 2f, 0f), new Vector3(wallThickness, wallHeight, _arenaSize + wallThickness * 2));
            CreateWall("WallWest", new Vector3(-halfSize, wallHeight / 2f, 0f), new Vector3(wallThickness, wallHeight, _arenaSize + wallThickness * 2));
            
            // Add corner pillars for extra containment
            CreateCornerPillar("CornerNE", new Vector3(halfSize, 0f, halfSize));
            CreateCornerPillar("CornerNW", new Vector3(-halfSize, 0f, halfSize));
            CreateCornerPillar("CornerSE", new Vector3(halfSize, 0f, -halfSize));
            CreateCornerPillar("CornerSW", new Vector3(-halfSize, 0f, -halfSize));
        }
        
        /// <summary>
        /// Validates that the "Obstacles" layer exists and logs warnings if not.
        /// </summary>
        private void ValidateObstacleLayer()
        {
            if (_layerValidated) return;
            _layerValidated = true;
            
            // Try to find the "Obstacles" layer by name
            int namedLayer = LayerMask.NameToLayer("Obstacles");
            if (namedLayer != -1)
            {
                _obstacleLayer = namedLayer;
                Debug.Log($"<color=green>[CopsAndRobbers]</color> Using 'Obstacles' layer (index {_obstacleLayer}) for pathfinding");
            }
            else
            {
                // Layer doesn't exist - warn the user
                Debug.LogWarning("<color=yellow>[CopsAndRobbers]</color> Layer 'Obstacles' not found! " +
                    "Pathfinding may not work correctly.\n" +
                    "To fix: Go to Edit → Project Settings → Tags and Layers, and add 'Obstacles' as Layer 8.\n" +
                    $"Falling back to layer index {_obstacleLayer}.");
            }
        }
        
        private void CreateCornerPillar(string name, Vector3 position)
        {
            var pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.name = name;
            pillar.transform.SetParent(transform);
            pillar.transform.position = position + Vector3.up * 3f;
            pillar.transform.localScale = new Vector3(3f, 3f, 3f);
            pillar.GetComponent<Renderer>().material.color = _wallColor * 0.8f;
            pillar.layer = _obstacleLayer;  // Set layer for pathfinding
            pillar.isStatic = true;
        }
        
        private void CreateWall(string name, Vector3 position, Vector3 scale)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(transform);
            wall.transform.position = position;
            wall.transform.localScale = scale;
            wall.GetComponent<Renderer>().material.color = _wallColor;
            wall.layer = _obstacleLayer;  // Set layer for pathfinding
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
            bankMain.layer = _obstacleLayer;  // Set layer for pathfinding
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
            vault.layer = _obstacleLayer;  // Set layer for pathfinding
            vault.isStatic = true;
        }
        
        private void CreateStreets()
        {
            float halfSize = _arenaSize / 2f;
            
            // ============= SOUTHWEST QUADRANT - Shopping District =============
            CreateBuilding("Shop1", new Vector3(-halfSize * 0.6f, 0f, -halfSize * 0.5f), new Vector3(6f, 3.5f, 6f));
            CreateBuilding("Shop2", new Vector3(-halfSize * 0.35f, 0f, -halfSize * 0.65f), new Vector3(5f, 3f, 5f));
            CreateBuilding("Shop3", new Vector3(-halfSize * 0.75f, 0f, -halfSize * 0.35f), new Vector3(4f, 2.5f, 4f));
            
            // Alley crates
            CreateCrate("Crate_SW1", new Vector3(-halfSize * 0.5f, 0f, -halfSize * 0.4f), new Vector3(1.5f, 1.5f, 1.5f));
            CreateCrate("Crate_SW2", new Vector3(-halfSize * 0.48f, 0f, -halfSize * 0.38f), new Vector3(1f, 2f, 1f));
            
            // ============= NORTHWEST QUADRANT - Industrial/Warehouse =============
            CreateBuilding("Warehouse", new Vector3(-halfSize * 0.5f, 0f, halfSize * 0.5f), new Vector3(10f, 5f, 8f));
            CreateBuilding("WarehouseSmall", new Vector3(-halfSize * 0.75f, 0f, halfSize * 0.65f), new Vector3(5f, 3f, 5f));
            CreateBuilding("LoadingDock", new Vector3(-halfSize * 0.3f, 0f, halfSize * 0.7f), new Vector3(6f, 2f, 4f));
            
            // Industrial crates and barrels
            CreateCrate("Crate_NW1", new Vector3(-halfSize * 0.65f, 0f, halfSize * 0.35f), new Vector3(2f, 2f, 2f));
            CreateCrate("Crate_NW2", new Vector3(-halfSize * 0.62f, 0f, halfSize * 0.32f), new Vector3(1.5f, 1f, 1.5f));
            CreateCrate("Crate_NW3", new Vector3(-halfSize * 0.35f, 0f, halfSize * 0.4f), new Vector3(2f, 1.5f, 2f));
            CreateBarrel("Barrel_NW1", new Vector3(-halfSize * 0.4f, 0f, halfSize * 0.55f));
            CreateBarrel("Barrel_NW2", new Vector3(-halfSize * 0.38f, 0f, halfSize * 0.52f));
            
            // ============= SOUTHEAST QUADRANT - Parking/Plaza =============
            // Parking lot pillars
            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    CreatePillar($"Pillar_{i}_{j}", new Vector3(halfSize * 0.3f + i * 4f, 1.5f, -halfSize * 0.4f - j * 5f));
                }
            }
            
            // Parked "cars" (simple boxes)
            CreateCar("Car1", new Vector3(halfSize * 0.4f, 0f, -halfSize * 0.55f));
            CreateCar("Car2", new Vector3(halfSize * 0.55f, 0f, -halfSize * 0.55f));
            CreateCar("Car3", new Vector3(halfSize * 0.7f, 0f, -halfSize * 0.55f));
            
            // Plaza benches/obstacles
            CreateBench("Bench1", new Vector3(halfSize * 0.25f, 0f, -halfSize * 0.25f));
            CreateBench("Bench2", new Vector3(halfSize * 0.35f, 0f, -halfSize * 0.2f));
            
            // ============= CENTER AREA - Street obstacles =============
            // Central plaza/fountain
            CreateFountain("CentralFountain", new Vector3(0f, 0f, 0f));
            
            // Street barriers
            CreateBarrier("Barrier1", new Vector3(-8f, 0f, 5f));
            CreateBarrier("Barrier2", new Vector3(8f, 0f, -5f));
            CreateBarrier("Barrier3", new Vector3(-5f, 0f, -8f));
            CreateBarrier("Barrier4", new Vector3(5f, 0f, 8f));
            
            // Dumpsters for cover
            CreateDumpster("Dumpster1", new Vector3(-12f, 0f, 0f));
            CreateDumpster("Dumpster2", new Vector3(12f, 0f, 3f));
            CreateDumpster("Dumpster3", new Vector3(0f, 0f, -12f));
            
            // ============= NORTHEAST - Near bank additional cover =============
            CreateCrate("Crate_NE1", new Vector3(halfSize * 0.3f, 0f, halfSize * 0.35f), new Vector3(2f, 2f, 2f));
            CreateCrate("Crate_NE2", new Vector3(halfSize * 0.75f, 0f, halfSize * 0.3f), new Vector3(1.5f, 1.5f, 1.5f));
            
            // Small guard booth near bank
            CreateBuilding("GuardBooth", new Vector3(halfSize * 0.25f, 0f, halfSize * 0.65f), new Vector3(3f, 2.5f, 3f));
        }
        
        private void CreateCrate(string name, Vector3 position, Vector3 size)
        {
            var crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crate.name = name;
            crate.transform.SetParent(transform);
            crate.transform.position = position + Vector3.up * (size.y / 2f);
            crate.transform.localScale = size;
            crate.GetComponent<Renderer>().material.color = new Color(0.55f, 0.4f, 0.25f); // Brown wooden color
            crate.layer = _obstacleLayer;  // Set layer for pathfinding
            crate.isStatic = true;
        }
        
        private void CreateBarrel(string name, Vector3 position)
        {
            var barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            barrel.name = name;
            barrel.transform.SetParent(transform);
            barrel.transform.position = position + Vector3.up * 0.75f;
            barrel.transform.localScale = new Vector3(1f, 0.75f, 1f);
            barrel.GetComponent<Renderer>().material.color = new Color(0.3f, 0.35f, 0.4f); // Metal gray
            barrel.layer = _obstacleLayer;  // Set layer for pathfinding
            barrel.isStatic = true;
        }
        
        private void CreateCar(string name, Vector3 position)
        {
            var car = new GameObject(name);
            car.transform.SetParent(transform);
            car.transform.position = position;
            
            // Car body - larger collider for better NPC collision
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(car.transform);
            body.transform.localPosition = new Vector3(0f, 0.75f, 0f);
            body.transform.localScale = new Vector3(2.2f, 1.5f, 4.2f);  // Slightly larger for collision
            body.GetComponent<Renderer>().material.color = new Color(0.6f, 0.1f, 0.1f); // Red car
            body.layer = _obstacleLayer;  // Set layer for pathfinding
            body.isStatic = true;
            
            // Car roof
            var roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "Roof";
            roof.transform.SetParent(car.transform);
            roof.transform.localPosition = new Vector3(0f, 1.5f, -0.3f);
            roof.transform.localScale = new Vector3(1.8f, 0.6f, 2f);
            roof.GetComponent<Renderer>().material.color = new Color(0.5f, 0.08f, 0.08f);
            Object.Destroy(roof.GetComponent<Collider>());
        }
        
        private void CreateBench(string name, Vector3 position)
        {
            var bench = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bench.name = name;
            bench.transform.SetParent(transform);
            bench.transform.position = position + Vector3.up * 0.4f;
            bench.transform.localScale = new Vector3(3f, 0.8f, 1f);
            bench.GetComponent<Renderer>().material.color = new Color(0.4f, 0.3f, 0.2f); // Wood color
            bench.layer = _obstacleLayer;  // Set layer for pathfinding
            bench.isStatic = true;
        }
        
        private void CreateFountain(string name, Vector3 position)
        {
            var fountain = new GameObject(name);
            fountain.transform.SetParent(transform);
            fountain.transform.position = position;
            
            // Fountain base (circular) - make thicker so NPCs don't get stuck
            var baseObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseObj.name = "Base";
            baseObj.transform.SetParent(fountain.transform);
            baseObj.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            baseObj.transform.localScale = new Vector3(6f, 0.5f, 6f);  // Thicker base
            baseObj.GetComponent<Renderer>().material.color = new Color(0.5f, 0.5f, 0.55f); // Stone gray
            baseObj.layer = _obstacleLayer;  // Set layer for pathfinding
            baseObj.isStatic = true;
            
            // Fountain center pillar
            var center = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            center.name = "Center";
            center.transform.SetParent(fountain.transform);
            center.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            center.transform.localScale = new Vector3(1.5f, 1.2f, 1.5f);
            center.GetComponent<Renderer>().material.color = new Color(0.45f, 0.45f, 0.5f);
            center.layer = _obstacleLayer;  // Set layer for pathfinding
            center.isStatic = true;
        }
        
        private void CreateBarrier(string name, Vector3 position)
        {
            var barrier = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barrier.name = name;
            barrier.transform.SetParent(transform);
            barrier.transform.position = position + Vector3.up * 0.5f;
            barrier.transform.localScale = new Vector3(2f, 1f, 0.3f);
            barrier.GetComponent<Renderer>().material.color = new Color(1f, 0.5f, 0f); // Orange construction barrier
            barrier.layer = _obstacleLayer;  // Set layer for pathfinding
            barrier.isStatic = true;
        }
        
        private void CreateDumpster(string name, Vector3 position)
        {
            var dumpster = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dumpster.name = name;
            dumpster.transform.SetParent(transform);
            dumpster.transform.position = position + Vector3.up * 0.9f;
            dumpster.transform.localScale = new Vector3(2.5f, 1.8f, 1.5f);
            dumpster.GetComponent<Renderer>().material.color = new Color(0.15f, 0.35f, 0.15f); // Dark green
            dumpster.layer = _obstacleLayer;  // Set layer for pathfinding
            dumpster.isStatic = true;
        }
        
        private void CreateBuilding(string name, Vector3 position, Vector3 size)
        {
            var building = GameObject.CreatePrimitive(PrimitiveType.Cube);
            building.name = name;
            building.transform.SetParent(transform);
            building.transform.position = position + Vector3.up * (size.y / 2f);
            building.transform.localScale = size;
            building.GetComponent<Renderer>().material.color = _wallColor * 0.9f;
            building.layer = _obstacleLayer;  // Set layer for pathfinding
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
            pillar.layer = _obstacleLayer;  // Set layer for pathfinding
            pillar.isStatic = true;
        }
        
        private void CreateEscapeZone()
        {
            float halfSize = _arenaSize / 2f;
            
            // Escape zone in the south
            Vector3 escapePosition = new Vector3(0f, 0.1f, -halfSize + 5f);
            _escapeZone = EscapeZone.Create(
                escapePosition,
                5f,
                transform
            );
            
            // COORDINATED PURSUIT: Tell CopAlertSystem where the escape zone is
            // This enables the "closest cop chases, others intercept" strategy
            CopAlertSystem.EscapeZonePosition = escapePosition;
            
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
            
            // Bank area - loot placed OUTSIDE buildings but nearby (accessible!)
            // Bank building is at (halfSize * 0.5f, 0, halfSize * 0.5f) with size (10, 4, 8)
            // So place loot in front of bank (south side) where NPCs can reach it
            Vector3 bankFront = new Vector3(halfSize * 0.5f, 0.5f, halfSize * 0.5f - 6f);
            
            // Define loot positions and values - ALL positions are now OUTSIDE buildings
            var lootDefinitions = new (Vector3 position, int value, string name)[]
            {
                // Bank loot - in front of the bank building
                (bankFront, 500, "MainVaultLoot"),
                (bankFront + new Vector3(-4f, 0f, 0f), 250, "BankSideLoot"),
                
                // Shop area (southwest) - in the street near shops, not inside them
                (new Vector3(-halfSize * 0.45f, 0.5f, -halfSize * 0.4f), 150, "ShopLoot1"),
                (new Vector3(-halfSize * 0.25f, 0.5f, -halfSize * 0.55f), 100, "ShopLoot2"),
                
                // Warehouse area (northwest) - outside warehouse, near loading dock
                (new Vector3(-halfSize * 0.35f, 0.5f, halfSize * 0.35f), 200, "WarehouseLoot"),
                
                // Parking area (southeast) - between cars, accessible
                (new Vector3(halfSize * 0.45f, 0.5f, -halfSize * 0.4f), 100, "ParkingLoot"),
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
        
        /// <summary>
        /// Creates the NPCPathVisualizer for debug path visualization.
        /// </summary>
        private void CreatePathVisualizer()
        {
            var visualizerObj = new GameObject("NPCPathVisualizer");
            visualizerObj.transform.SetParent(transform);
            _pathVisualizer = visualizerObj.AddComponent<NPCPathVisualizer>();
            _pathVisualizer.ShowPaths = _showNPCPaths;
        }
        
        /// <summary>
        /// Creates an EasyPathGrid for A* pathfinding after all obstacles have been placed.
        /// </summary>
        private void CreatePathfindingGrid()
        {
            float halfSize = _arenaSize / 2f;
            
            // Create grid GameObject
            var gridObject = new GameObject("PathfindingGrid");
            gridObject.transform.SetParent(transform);
            
            // Position grid at corner of arena (grid extends in +X and +Z)
            gridObject.transform.position = new Vector3(-halfSize, 0f, -halfSize);
            
            // Add EasyPathGrid component - Configure() must be called BEFORE AddComponent
            // triggers Awake(), so we add the component then configure it
            _pathfindingGrid = gridObject.AddComponent<EasyPathGrid>();
            
            // Calculate grid dimensions
            int gridSize = Mathf.CeilToInt(_arenaSize / _gridCellSize);
            
            // Create obstacle layer mask (bitmask: 1 << layerNumber)
            LayerMask obstacleLayerMask = 1 << _obstacleLayer;
            
            // Use the public Configure() API instead of fragile reflection
            // This is cleaner and won't break if field names change
            _pathfindingGrid.Configure(
                width: gridSize,
                height: gridSize,
                cellSize: _gridCellSize,
                obstacleLayer: obstacleLayerMask,
                obstacleCheckRadius: _gridCellSize * 0.4f,
                obstacleCheckHeight: 0.5f,
                showDebugGizmos: _showPathfindingDebug
            );
            
            // Build the grid now that all obstacles exist and settings are configured
            _pathfindingGrid.BuildGrid();
            
            Debug.Log($"<color=cyan>[Pathfinding]</color> Grid created: {gridSize}x{gridSize} cells, " +
                $"{_pathfindingGrid.WalkableCount} walkable ({(_pathfindingGrid.WalkableCount * 100f / (gridSize * gridSize)):F1}%)");
        }
        
        private void CreateCoverPoints()
        {
            float halfSize = _arenaSize / 2f;
            
            // Create many cover points around the map near obstacles
            Vector3[] coverPositions = new Vector3[]
            {
                // Near buildings
                new Vector3(-halfSize * 0.55f, 0f, -halfSize * 0.4f),   // Near Shop1
                new Vector3(-halfSize * 0.45f, 0f, halfSize * 0.4f),    // Near Warehouse
                new Vector3(-halfSize * 0.7f, 0f, halfSize * 0.55f),    // Behind WarehouseSmall
                
                // Near center
                new Vector3(-6f, 0f, 3f),    // Near fountain west
                new Vector3(6f, 0f, -3f),    // Near fountain east
                new Vector3(0f, 0f, 8f),     // North of fountain
                new Vector3(0f, 0f, -8f),    // South of fountain
                
                // Near cars/parking
                new Vector3(halfSize * 0.5f, 0f, -halfSize * 0.45f),
                new Vector3(halfSize * 0.65f, 0f, -halfSize * 0.5f),
                
                // Near bank area
                new Vector3(halfSize * 0.35f, 0f, halfSize * 0.55f),
                new Vector3(halfSize * 0.7f, 0f, halfSize * 0.35f),
                
                // Near dumpsters
                new Vector3(-12f, 0f, 2f),
                new Vector3(12f, 0f, 5f),
                new Vector3(2f, 0f, -12f),
                
                // Strategic corners
                new Vector3(-halfSize * 0.8f, 0f, -halfSize * 0.8f),
                new Vector3(halfSize * 0.8f, 0f, -halfSize * 0.8f),
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
            
            // Spread cops around the larger arena
            Vector3[] copPositions = new Vector3[]
            {
                new Vector3(halfSize * 0.35f, 0.1f, halfSize * 0.7f),   // Near bank
                new Vector3(-halfSize * 0.4f, 0.1f, halfSize * 0.4f),   // Northwest patrol
                new Vector3(halfSize * 0.6f, 0.1f, -halfSize * 0.4f),   // Southeast patrol
                new Vector3(-halfSize * 0.4f, 0.1f, -halfSize * 0.4f),  // Southwest patrol
                new Vector3(0f, 0.1f, 0f),                               // Center patrol
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
            
            float patrolRadius = 12f;  // Larger patrol radius for bigger arena
            int waypointCount = 5;     // More waypoints for better coverage
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
            
            // Robbers spawn scattered around the map, away from escape zone (south)
            // They need to navigate through obstacles to reach loot and escape
            Vector3[] robberPositions = new Vector3[]
            {
                new Vector3(-halfSize * 0.3f, 0.1f, halfSize * 0.2f),   // Northwest area
                new Vector3(halfSize * 0.2f, 0.1f, halfSize * 0.3f),    // Near bank
                new Vector3(0f, 0.1f, 5f),                               // Center north
                new Vector3(-halfSize * 0.5f, 0.1f, -halfSize * 0.2f),  // West side
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
            GUILayout.Label("    <color=green>Low</color> = Deterministic, optimal paths");
            GUILayout.Label("    <color=red>High</color> = Random, commits to paths");
            GUILayout.Label("  <b>I</b> = Inertia (tendency to repeat actions)");
            GUILayout.Label("    <color=green>High</color> = Precise path following");
            GUILayout.Label("    <color=yellow>Low</color> = Cuts corners, shortcuts");
            GUILayout.EndVertical();
            
            // Pathfinding info
            if (_pathfindingGrid != null)
            {
                GUILayout.BeginVertical("box");
                GUILayout.Label("<b>🗺️ A* PATHFINDING</b>");
                GUILayout.Label($"  Grid: {_pathfindingGrid.Width}x{_pathfindingGrid.Height} cells");
                GUILayout.Label($"  Walkable: {_pathfindingGrid.WalkableCount} cells");
                GUILayout.Label($"  Active paths: {NPCPathVisualizer.PathCount}");
                GUILayout.Label("  Integrated with Criticality!");
                GUILayout.EndVertical();
            }
            
            GUILayout.Space(5);
            
            // Controls
            GUILayout.BeginVertical("box");
            GUILayout.Label("<b>🎮 CONTROLS</b>");
            if (GUILayout.Button("Restart Game"))
            {
                RestartGame();
            }
            
            // Debug visualization toggles
            GUILayout.Space(5);
            GUILayout.Label("<b>Debug Visualization (Scene View)</b>");
            
            bool newShowPaths = GUILayout.Toggle(_showNPCPaths, " Show NPC Paths");
            if (newShowPaths != _showNPCPaths)
            {
                _showNPCPaths = newShowPaths;
                if (_pathVisualizer != null)
                {
                    _pathVisualizer.ShowPaths = _showNPCPaths;
                }
            }
            
            GUILayout.Label("  <size=10><i>Grid debug: set in EasyPathGrid inspector</i></size>");
            
            GUILayout.Space(5);
            GUILayout.Label("  <size=10><i>View in Scene window with Gizmos enabled</i></size>");
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
            
            // Role and Goal display
            GUILayout.Label($"  <b>{cop.name}</b> - <i>{cop.Role}</i>");
            GUILayout.Label($"      <size=10><color=#888888>Goal: {cop.Goal}</color></size>");
            GUILayout.Label($"      Action: <color={stateColor}>{actionName}</color>{critInfo}");
            GUILayout.Label($"      <color=#AADDFF>→ {cop.CurrentReason}</color>");
            // Show vision info for debugging
            string visionInfo = cop.Perception != null && cop.Perception.HasVisibleTargets 
                ? $"<color=red>SEES TARGET!</color>" 
                : $"No visual ({cop.Perception?.VisibleTargets.Count ?? 0} targets)";
            string crimeStatus = cop.CrimeInProgress ? "<color=red>CRIME!</color>" : "No crime";
            GUILayout.Label($"      Alert: {alert:F2} | Arrests: {cop.ArrestCount} | {crimeStatus}");
            GUILayout.Label($"      Vision: {visionInfo}");
        }
        
        private void DrawRobberStatus(RobberNPC robber)
        {
            if (!robber.gameObject.activeSelf)
            {
                string status = robber.HasEscaped ? "<color=green>ESCAPED!</color>" : "<color=red>ARRESTED!</color>";
                GUILayout.Label($"  <b>{robber.name}</b> - <i>{robber.Role}</i>");
                GUILayout.Label($"      {status}");
                GUILayout.Label($"      <color=#AADDFF>→ {robber.CurrentReason}</color>");
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
            
            // Role and Goal display
            GUILayout.Label($"  <b>{robber.name}</b> - <i>{robber.Role}</i>{lootInfo}{fearInfo}");
            GUILayout.Label($"      <size=10><color=#888888>Goal: {robber.Goal}</color></size>");
            GUILayout.Label($"      Action: <color={stateColor}>{actionName}</color>{critInfo}");
            GUILayout.Label($"      <color=#FFDDAA>→ {robber.CurrentReason}</color>");
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
