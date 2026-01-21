using System.Collections.Generic;
using UnityEngine;
using NPCBrain.Archetypes;
using NPCBrain.Components;
using NPCBrain.Perception;
using NPCBrain.BehaviorTree.Composites;
using EasyPath;

namespace NPCBrain.Demo
{
    /// <summary>
    /// Sets up a Stealth Infiltration demo scene showcasing NPCBrain's perception systems.
    /// A spy/infiltrator must sneak through a facility, collect intel, and reach extraction.
    /// </summary>
    /// <remarks>
    /// Features demonstrated:
    /// - SightSensor vision cones (visible in debug)
    /// - HearingSensor footstep detection
    /// - Guard alert levels and investigation behavior
    /// - Patrol routes with WaypointPath
    /// - Player sneaking mechanics (crouch = quieter)
    /// - Intel collection
    /// - Extraction zone
    /// </remarks>
    public class StealthDemoSetup : MonoBehaviour
    {
        [Header("Scene Settings")]
        [SerializeField] private bool _autoGenerate = true;
        [SerializeField] private float _arenaSize = 40f;
        
        [Header("NPC Counts")]
        [SerializeField] private int _guardCount = 4;
        [SerializeField] private int _primaryIntelCount = 1;
        [SerializeField] private int _secondaryIntelCount = 3;
        
        [Header("Colors")]
        [SerializeField] private Color _groundColor = new Color(0.15f, 0.15f, 0.18f);
        [SerializeField] private Color _guardColor = new Color(0.7f, 0.2f, 0.2f);
        [SerializeField] private Color _playerColor = new Color(0.2f, 0.7f, 0.3f);
        [SerializeField] private Color _wallColor = new Color(0.3f, 0.3f, 0.35f);
        [SerializeField] private Color _buildingColor = new Color(0.25f, 0.25f, 0.3f);
        
        [Header("Pathfinding")]
        [SerializeField] private float _gridCellSize = 1f;
        [SerializeField] private bool _showPathfindingDebug = false;
        
        [Header("References (auto-populated)")]
        [SerializeField] private GameObject _player;
        [SerializeField] private List<GuardNPC> _guards = new List<GuardNPC>();
        [SerializeField] private List<IntelPoint> _intelPoints = new List<IntelPoint>();
        [SerializeField] private ExtractionZone _extractionZone;
        
        private EasyPathGrid _pathfindingGrid;
        private int _obstacleLayer = 8;
        private bool _layerValidated;
        
        // Game state
        private int _collectedIntelCount;
        private int _totalPoints;
        private bool _hasPrimaryIntel;
        private bool _gameEnded;
        private string _endMessage;
        private float _gameTime;
        private int _timesDetected;
        private bool _isDetected;
        
        // UI
        private bool _showOverlay = true;
        private GUIStyle _boxStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _labelStyle;
        private bool _stylesInitialized;
        
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
                CheckDetection();
                CheckExtraction();
            }
        }
        
        private void CheckDetection()
        {
            bool currentlyDetected = false;
            foreach (var guard in _guards)
            {
                if (guard == null) continue;
                if (guard.Blackboard.TryGet<GameObject>(BBKeys.Target, out var target) && target == _player)
                {
                    currentlyDetected = true;
                    break;
                }
            }
            
            if (currentlyDetected && !_isDetected)
            {
                _timesDetected++;
                Debug.Log($"<color=red>[Stealth] DETECTED! ({_timesDetected} times)</color>");
            }
            _isDetected = currentlyDetected;
        }
        
        private void CheckExtraction()
        {
            if (_player == null || _extractionZone == null) return;
            
            if (_extractionZone.TryExtract(_player, _collectedIntelCount, _hasPrimaryIntel, _totalPoints))
            {
                _gameEnded = true;
                int bonus = CalculateBonus();
                int finalScore = _totalPoints + bonus;
                _endMessage = $"MISSION COMPLETE!\nIntel: {_collectedIntelCount} | Points: {_totalPoints}\nStealth Bonus: +{bonus}\nFINAL SCORE: {finalScore}";
            }
        }
        
        private int CalculateBonus()
        {
            // Bonus for not being detected
            if (_timesDetected == 0) return 500;
            if (_timesDetected == 1) return 200;
            if (_timesDetected <= 3) return 100;
            return 0;
        }
        
        /// <summary>
        /// Generates the complete Stealth Infiltration demo scene.
        /// </summary>
        [ContextMenu("Generate Stealth Demo")]
        public void GenerateScene()
        {
            ClearScene();
            ValidateObstacleLayer();
            CreateGround();
            CreateWalls();
            CreateFacility();
            CreatePathfindingGrid();
            CreateIntelPoints();
            CreateExtractionZone();
            CreateGuards();
            CreatePlayer();
            SetupNPCCollisionIgnoring();
            
            Debug.Log("<color=cyan>[Stealth Demo] Generated! WASD to move, Shift to sprint (louder), Ctrl to crouch (quieter). Collect intel and reach extraction!</color>");
        }
        
        [ContextMenu("Restart Mission")]
        public void RestartMission()
        {
            _collectedIntelCount = 0;
            _totalPoints = 0;
            _hasPrimaryIntel = false;
            _gameEnded = false;
            _endMessage = "";
            _gameTime = 0f;
            _timesDetected = 0;
            _isDetected = false;
            
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
            
            // Unsubscribe from events
            foreach (var intel in _intelPoints)
            {
                if (intel != null) intel.OnCollected -= OnIntelCollected;
            }
            
            _guards.Clear();
            _intelPoints.Clear();
            _player = null;
            _extractionZone = null;
            _pathfindingGrid = null;
        }
        
        private void OnDestroy()
        {
            foreach (var intel in _intelPoints)
            {
                if (intel != null) intel.OnCollected -= OnIntelCollected;
            }
        }
        
        private void ValidateObstacleLayer()
        {
            if (_layerValidated) return;
            _layerValidated = true;
            
            int namedLayer = LayerMask.NameToLayer("Obstacles");
            if (namedLayer != -1)
            {
                _obstacleLayer = namedLayer;
            }
            else
            {
                Debug.LogWarning("[Stealth] Layer 'Obstacles' not found. Add it in Edit → Project Settings → Tags and Layers.");
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
            
            // Add subtle grid pattern
            CreateGridLines();
        }
        
        private void CreateGridLines()
        {
            float halfSize = _arenaSize / 2f;
            Color lineColor = new Color(0.2f, 0.2f, 0.25f);
            
            for (float i = -halfSize + 5f; i < halfSize; i += 5f)
            {
                // Horizontal line
                var hLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
                hLine.name = "GridLine_H";
                hLine.transform.SetParent(transform);
                hLine.transform.position = new Vector3(0f, 0.01f, i);
                hLine.transform.localScale = new Vector3(_arenaSize, 0.02f, 0.05f);
                hLine.GetComponent<Renderer>().material.color = lineColor;
                Object.Destroy(hLine.GetComponent<Collider>());
                
                // Vertical line
                var vLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
                vLine.name = "GridLine_V";
                vLine.transform.SetParent(transform);
                vLine.transform.position = new Vector3(i, 0.01f, 0f);
                vLine.transform.localScale = new Vector3(0.05f, 0.02f, _arenaSize);
                vLine.GetComponent<Renderer>().material.color = lineColor;
                Object.Destroy(vLine.GetComponent<Collider>());
            }
        }
        
        private void CreateWalls()
        {
            float halfSize = _arenaSize / 2f;
            float wallHeight = 4f;
            float wallThickness = 1f;
            
            CreateWall("WallNorth", new Vector3(0f, wallHeight / 2f, halfSize), new Vector3(_arenaSize + wallThickness * 2, wallHeight, wallThickness));
            CreateWall("WallSouth", new Vector3(0f, wallHeight / 2f, -halfSize), new Vector3(_arenaSize + wallThickness * 2, wallHeight, wallThickness));
            CreateWall("WallEast", new Vector3(halfSize, wallHeight / 2f, 0f), new Vector3(wallThickness, wallHeight, _arenaSize + wallThickness * 2));
            CreateWall("WallWest", new Vector3(-halfSize, wallHeight / 2f, 0f), new Vector3(wallThickness, wallHeight, _arenaSize + wallThickness * 2));
        }
        
        private void CreateWall(string name, Vector3 position, Vector3 scale)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(transform);
            wall.transform.position = position;
            wall.transform.localScale = scale;
            wall.GetComponent<Renderer>().material.color = _wallColor;
            wall.layer = _obstacleLayer;
            wall.isStatic = true;
        }
        
        private void CreateFacility()
        {
            float halfSize = _arenaSize / 2f;
            
            // === MAIN BUILDING (North - contains primary intel) ===
            CreateBuilding("MainBuilding", new Vector3(0f, 0f, halfSize * 0.6f), new Vector3(12f, 3f, 8f));
            
            // Entrance pillars
            CreatePillar("MainPillar_L", new Vector3(-3f, 0f, halfSize * 0.6f - 5f));
            CreatePillar("MainPillar_R", new Vector3(3f, 0f, halfSize * 0.6f - 5f));
            
            // === WEST WING (Guard post + secondary intel) ===
            CreateBuilding("WestWing", new Vector3(-halfSize * 0.5f, 0f, 0f), new Vector3(8f, 2.5f, 6f));
            CreateCrate("WestCrate1", new Vector3(-halfSize * 0.5f + 5f, 0f, 2f), new Vector3(1.5f, 1.5f, 1.5f));
            CreateCrate("WestCrate2", new Vector3(-halfSize * 0.5f + 5f, 0f, -2f), new Vector3(1.2f, 2f, 1.2f));
            
            // === EAST WING (Storage + secondary intel) ===
            CreateBuilding("EastWing", new Vector3(halfSize * 0.5f, 0f, 0f), new Vector3(6f, 2.5f, 8f));
            CreateBarrel("EastBarrel1", new Vector3(halfSize * 0.5f - 4f, 0f, 2f));
            CreateBarrel("EastBarrel2", new Vector3(halfSize * 0.5f - 4.5f, 0f, 1f));
            CreateBarrel("EastBarrel3", new Vector3(halfSize * 0.5f - 4f, 0f, 0f));
            
            // === SOUTH AREA (Cover near extraction) ===
            CreateBuilding("SouthShed", new Vector3(-5f, 0f, -halfSize * 0.5f), new Vector3(4f, 2f, 4f));
            CreateBuilding("SouthShed2", new Vector3(8f, 0f, -halfSize * 0.5f), new Vector3(5f, 2f, 3f));
            
            // === CENTER AREA (Courtyard obstacles) ===
            CreateFountain("CentralFountain", new Vector3(0f, 0f, 0f));
            
            // Strategic cover points
            CreateCrate("CoverCrate1", new Vector3(-8f, 0f, 5f), new Vector3(2f, 2f, 2f));
            CreateCrate("CoverCrate2", new Vector3(8f, 0f, 5f), new Vector3(2f, 2f, 2f));
            CreateCrate("CoverCrate3", new Vector3(-8f, 0f, -5f), new Vector3(1.5f, 1.5f, 1.5f));
            CreateCrate("CoverCrate4", new Vector3(10f, 0f, -8f), new Vector3(2f, 1.5f, 2f));
            
            // Security barriers
            CreateBarrier("Barrier1", new Vector3(-3f, 0f, halfSize * 0.3f), 0f);
            CreateBarrier("Barrier2", new Vector3(5f, 0f, -halfSize * 0.2f), 90f);
        }
        
        private void CreateBuilding(string name, Vector3 position, Vector3 size)
        {
            var building = GameObject.CreatePrimitive(PrimitiveType.Cube);
            building.name = name;
            building.transform.SetParent(transform);
            building.transform.position = position + Vector3.up * (size.y / 2f);
            building.transform.localScale = size;
            building.GetComponent<Renderer>().material.color = _buildingColor;
            building.layer = _obstacleLayer;
            building.isStatic = true;
        }
        
        private void CreatePillar(string name, Vector3 position)
        {
            var pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.name = name;
            pillar.transform.SetParent(transform);
            pillar.transform.position = position + Vector3.up * 1.5f;
            pillar.transform.localScale = new Vector3(1f, 1.5f, 1f);
            pillar.GetComponent<Renderer>().material.color = _buildingColor * 0.9f;
            pillar.layer = _obstacleLayer;
            pillar.isStatic = true;
        }
        
        private void CreateCrate(string name, Vector3 position, Vector3 size)
        {
            var crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crate.name = name;
            crate.transform.SetParent(transform);
            crate.transform.position = position + Vector3.up * (size.y / 2f);
            crate.transform.localScale = size;
            crate.GetComponent<Renderer>().material.color = new Color(0.5f, 0.4f, 0.25f);
            crate.layer = _obstacleLayer;
            crate.isStatic = true;
        }
        
        private void CreateBarrel(string name, Vector3 position)
        {
            var barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            barrel.name = name;
            barrel.transform.SetParent(transform);
            barrel.transform.position = position + Vector3.up * 0.6f;
            barrel.transform.localScale = new Vector3(0.8f, 0.6f, 0.8f);
            barrel.GetComponent<Renderer>().material.color = new Color(0.3f, 0.35f, 0.4f);
            barrel.layer = _obstacleLayer;
            barrel.isStatic = true;
        }
        
        private void CreateFountain(string name, Vector3 position)
        {
            var fountain = new GameObject(name);
            fountain.transform.SetParent(transform);
            fountain.transform.position = position;
            
            var baseObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseObj.name = "Base";
            baseObj.transform.SetParent(fountain.transform);
            baseObj.transform.localPosition = new Vector3(0f, 0.3f, 0f);
            baseObj.transform.localScale = new Vector3(4f, 0.3f, 4f);
            baseObj.GetComponent<Renderer>().material.color = new Color(0.4f, 0.4f, 0.45f);
            baseObj.layer = _obstacleLayer;
            baseObj.isStatic = true;
            
            var center = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            center.name = "Center";
            center.transform.SetParent(fountain.transform);
            center.transform.localPosition = new Vector3(0f, 1f, 0f);
            center.transform.localScale = new Vector3(1f, 0.8f, 1f);
            center.GetComponent<Renderer>().material.color = new Color(0.35f, 0.35f, 0.4f);
            center.layer = _obstacleLayer;
            center.isStatic = true;
        }
        
        private void CreateBarrier(string name, Vector3 position, float rotation)
        {
            var barrier = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barrier.name = name;
            barrier.transform.SetParent(transform);
            barrier.transform.position = position + Vector3.up * 0.5f;
            barrier.transform.localScale = new Vector3(2.5f, 1f, 0.3f);
            barrier.transform.rotation = Quaternion.Euler(0f, rotation, 0f);
            barrier.GetComponent<Renderer>().material.color = new Color(1f, 0.6f, 0f);
            barrier.layer = _obstacleLayer;
            barrier.isStatic = true;
        }
        
        private void CreatePathfindingGrid()
        {
            float halfSize = _arenaSize / 2f;
            
            var gridObject = new GameObject("PathfindingGrid");
            gridObject.transform.SetParent(transform);
            gridObject.transform.position = new Vector3(-halfSize, 0f, -halfSize);
            
            _pathfindingGrid = gridObject.AddComponent<EasyPathGrid>();
            
            int gridSize = Mathf.CeilToInt(_arenaSize / _gridCellSize);
            LayerMask obstacleLayerMask = 1 << _obstacleLayer;
            
            _pathfindingGrid.Configure(
                width: gridSize,
                height: gridSize,
                cellSize: _gridCellSize,
                obstacleLayer: obstacleLayerMask,
                obstacleCheckRadius: 0.8f,
                obstacleCheckHeight: 0.5f,
                showDebugGizmos: _showPathfindingDebug
            );
            
            _pathfindingGrid.BuildGrid();
        }
        
        private void CreateIntelPoints()
        {
            float halfSize = _arenaSize / 2f;
            
            // Primary intel (in main building area - heavily guarded)
            var primaryPositions = new Vector3[]
            {
                new Vector3(0f, 0.5f, halfSize * 0.6f + 5f),  // Behind main building
            };
            
            // Secondary intel (spread around facility)
            var secondaryPositions = new Vector3[]
            {
                new Vector3(-halfSize * 0.5f + 5f, 0.5f, 0f),   // Near west wing
                new Vector3(halfSize * 0.5f - 4f, 0.5f, -3f),    // Near east wing
                new Vector3(0f, 0.5f, -halfSize * 0.3f),         // Central area
                new Vector3(-halfSize * 0.3f, 0.5f, halfSize * 0.3f),  // NW area
            };
            
            // Create primary intel
            for (int i = 0; i < Mathf.Min(_primaryIntelCount, primaryPositions.Length); i++)
            {
                var intel = IntelPoint.Create(primaryPositions[i], 500, true, transform);
                intel.name = $"PrimaryIntel_{i}";
                intel.OnCollected += OnIntelCollected;
                _intelPoints.Add(intel);
            }
            
            // Create secondary intel
            for (int i = 0; i < Mathf.Min(_secondaryIntelCount, secondaryPositions.Length); i++)
            {
                var intel = IntelPoint.Create(secondaryPositions[i], 100 + i * 50, false, transform);
                intel.name = $"SecondaryIntel_{i}";
                intel.OnCollected += OnIntelCollected;
                _intelPoints.Add(intel);
            }
        }
        
        private void OnIntelCollected(IntelPoint intel, GameObject collector)
        {
            _collectedIntelCount++;
            _totalPoints += intel.Points;
            if (intel.IsPrimary)
            {
                _hasPrimaryIntel = true;
                Debug.Log("<color=red>[Stealth] PRIMARY INTEL ACQUIRED!</color>");
            }
        }
        
        private void CreateExtractionZone()
        {
            float halfSize = _arenaSize / 2f;
            
            // Extraction zone in the south
            Vector3 extractionPos = new Vector3(0f, 0.1f, -halfSize + 4f);
            _extractionZone = ExtractionZone.Create(extractionPos, 3f, true, 1, transform);
        }
        
        private void CreateGuards()
        {
            float halfSize = _arenaSize / 2f;
            
            // Guard positions and patrol areas
            var guardConfigs = new (Vector3 pos, int patrolIndex)[]
            {
                // Main building guards
                (new Vector3(-5f, 0.1f, halfSize * 0.6f), 0),
                (new Vector3(5f, 0.1f, halfSize * 0.6f), 1),
                // West wing guard
                (new Vector3(-halfSize * 0.5f, 0.1f, 4f), 2),
                // East wing guard
                (new Vector3(halfSize * 0.5f, 0.1f, -4f), 3),
                // Roaming guard
                (new Vector3(0f, 0.1f, -halfSize * 0.3f), 4),
            };
            
            for (int i = 0; i < Mathf.Min(_guardCount, guardConfigs.Length); i++)
            {
                var config = guardConfigs[i];
                var guard = CreateGuard($"Guard_{i}", config.pos, config.patrolIndex);
                _guards.Add(guard);
            }
        }
        
        private GuardNPC CreateGuard(string name, Vector3 position, int patrolIndex)
        {
            var guardObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            guardObj.name = name;
            guardObj.transform.SetParent(transform);
            guardObj.transform.position = position;
            guardObj.GetComponent<Renderer>().material.color = _guardColor;
            
            // Add CharacterController
            var cc = guardObj.AddComponent<CharacterController>();
            cc.height = 2f;
            cc.radius = 0.5f;
            cc.center = new Vector3(0f, 1f, 0f);
            
            // Add sight sensor FIRST
            var sightSensor = guardObj.AddComponent<SightSensor>();
            // Configure sight sensor for stealth gameplay
            var sightType = typeof(SightSensor);
            var fovField = sightType.GetField("_fieldOfView", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var rangeField = sightType.GetField("_sightRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var debugField = sightType.GetField("_showDebugGizmos", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (fovField != null) fovField.SetValue(sightSensor, 90f);  // 90 degree FOV
            if (rangeField != null) rangeField.SetValue(sightSensor, 15f);  // 15m range
            if (debugField != null) debugField.SetValue(sightSensor, true);  // Show vision cones!
            
            // Add hearing sensor
            var hearingSensor = guardObj.AddComponent<HearingSensor>();
            
            // Add guard component
            var guard = guardObj.AddComponent<GuardNPC>();
            
            // Create patrol route
            var waypointPath = CreateGuardPatrol(name + "_Patrol", position, patrolIndex);
            guard.SetWaypointPath(waypointPath);
            
            // Add visual indicator (helmet)
            var helmet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            helmet.name = "Helmet";
            helmet.transform.SetParent(guardObj.transform);
            helmet.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            helmet.transform.localScale = new Vector3(0.5f, 0.3f, 0.5f);
            helmet.GetComponent<Renderer>().material.color = new Color(0.4f, 0.15f, 0.15f);
            Object.Destroy(helmet.GetComponent<Collider>());
            
            return guard;
        }
        
        private WaypointPath CreateGuardPatrol(string name, Vector3 center, int patrolIndex)
        {
            var container = new GameObject(name);
            container.transform.SetParent(transform);
            
            var waypointPath = container.AddComponent<WaypointPath>();
            var waypoints = new List<Transform>();
            
            float halfSize = _arenaSize / 2f;
            Vector3[] patrolPoints;
            
            // Different patrol patterns for each guard
            switch (patrolIndex)
            {
                case 0: // Main building left
                    patrolPoints = new Vector3[]
                    {
                        new Vector3(-8f, 0.1f, halfSize * 0.6f),
                        new Vector3(-8f, 0.1f, halfSize * 0.4f),
                        new Vector3(-3f, 0.1f, halfSize * 0.3f),
                        new Vector3(-3f, 0.1f, halfSize * 0.6f),
                    };
                    break;
                case 1: // Main building right
                    patrolPoints = new Vector3[]
                    {
                        new Vector3(8f, 0.1f, halfSize * 0.6f),
                        new Vector3(8f, 0.1f, halfSize * 0.4f),
                        new Vector3(3f, 0.1f, halfSize * 0.3f),
                        new Vector3(3f, 0.1f, halfSize * 0.6f),
                    };
                    break;
                case 2: // West wing patrol
                    patrolPoints = new Vector3[]
                    {
                        new Vector3(-halfSize * 0.5f - 3f, 0.1f, 5f),
                        new Vector3(-halfSize * 0.5f + 5f, 0.1f, 5f),
                        new Vector3(-halfSize * 0.5f + 5f, 0.1f, -5f),
                        new Vector3(-halfSize * 0.5f - 3f, 0.1f, -5f),
                    };
                    break;
                case 3: // East wing patrol
                    patrolPoints = new Vector3[]
                    {
                        new Vector3(halfSize * 0.5f + 3f, 0.1f, 5f),
                        new Vector3(halfSize * 0.5f - 3f, 0.1f, 5f),
                        new Vector3(halfSize * 0.5f - 3f, 0.1f, -5f),
                        new Vector3(halfSize * 0.5f + 3f, 0.1f, -5f),
                    };
                    break;
                default: // Roaming guard - center area
                    patrolPoints = new Vector3[]
                    {
                        new Vector3(-5f, 0.1f, -5f),
                        new Vector3(5f, 0.1f, -5f),
                        new Vector3(5f, 0.1f, 5f),
                        new Vector3(-5f, 0.1f, 5f),
                    };
                    break;
            }
            
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                var waypointPos = patrolPoints[i];
                
                // Clamp to arena bounds
                float maxPos = halfSize - 2f;
                waypointPos.x = Mathf.Clamp(waypointPos.x, -maxPos, maxPos);
                waypointPos.z = Mathf.Clamp(waypointPos.z, -maxPos, maxPos);
                
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
                marker.GetComponent<Renderer>().material.color = new Color(1f, 0.3f, 0.3f, 0.3f);
                Object.Destroy(marker.GetComponent<Collider>());
            }
            
            waypointPath.SetWaypoints(waypoints);
            return waypointPath;
        }
        
        private void CreatePlayer()
        {
            float halfSize = _arenaSize / 2f;
            
            // Spawn player in south area (near extraction but not in it)
            Vector3 spawnPos = new Vector3(-halfSize * 0.6f, 0.1f, -halfSize + 6f);
            
            _player = PlayerController.CreatePlayer(spawnPos);
            _player.transform.SetParent(transform);
            _player.GetComponent<Renderer>().material.color = _playerColor;
            
            // Add footstep emitter for stealth mechanics
            var footstepEmitter = _player.AddComponent<PlayerFootstepEmitter>();
            
            // Add intel collector component
            var collector = _player.AddComponent<IntelCollector>();
            collector.Initialize(_intelPoints);
        }
        
        private void SetupNPCCollisionIgnoring()
        {
            var allColliders = new List<Collider>();
            
            foreach (var guard in _guards)
            {
                if (guard == null) continue;
                var cc = guard.GetComponent<CharacterController>();
                if (cc != null) allColliders.Add(cc);
            }
            
            if (_player != null)
            {
                var playerCC = _player.GetComponent<CharacterController>();
                if (playerCC != null) allColliders.Add(playerCC);
            }
            
            for (int i = 0; i < allColliders.Count; i++)
            {
                for (int j = i + 1; j < allColliders.Count; j++)
                {
                    Physics.IgnoreCollision(allColliders[i], allColliders[j], true);
                }
            }
        }
        
        #region OnGUI Overlay
        
        private void InitStyles()
        {
            if (_stylesInitialized) return;
            _stylesInitialized = true;
            
            _boxStyle = new GUIStyle(GUI.skin.box);
            _boxStyle.normal.background = MakeTexture(2, 2, new Color(0.05f, 0.08f, 0.1f, 0.9f));
            
            _headerStyle = new GUIStyle(GUI.skin.label);
            _headerStyle.fontSize = 16;
            _headerStyle.fontStyle = FontStyle.Bold;
            _headerStyle.normal.textColor = new Color(0.2f, 0.8f, 0.8f);
            _headerStyle.alignment = TextAnchor.MiddleCenter;
            
            _labelStyle = new GUIStyle(GUI.skin.label);
            _labelStyle.fontSize = 12;
            _labelStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
            _labelStyle.richText = true;
        }
        
        private Texture2D MakeTexture(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;
            Texture2D tex = new Texture2D(width, height);
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }
        
        private void OnGUI()
        {
            if (!_showOverlay) return;
            
            InitStyles();
            
            float panelWidth = 280f;
            float panelX = 10f;
            float panelY = 10f;
            
            // Calculate panel height dynamically
            float panelHeight = _gameEnded ? 320f : 380f;
            
            GUI.Box(new Rect(panelX, panelY, panelWidth, panelHeight), "", _boxStyle);
            
            float y = panelY + 10f;
            float w = panelWidth - 20f;
            float x = panelX + 10f;
            
            // Title
            GUI.Label(new Rect(x, y, w, 24), "🕵️ STEALTH INFILTRATION", _headerStyle);
            y += 28;
            
            // Game time
            string timeStr = System.TimeSpan.FromSeconds(_gameTime).ToString(@"mm\:ss");
            GUI.Label(new Rect(x, y, w, 18), $"Time: {timeStr}", _labelStyle);
            y += 20;
            
            // Detection status
            string detectionStatus = _isDetected ? "<color=red>⚠️ DETECTED!</color>" : "<color=green>✓ Hidden</color>";
            GUI.Label(new Rect(x, y, w, 18), $"Status: {detectionStatus}", _labelStyle);
            y += 20;
            
            GUI.Label(new Rect(x, y, w, 18), $"Times Detected: {_timesDetected}", _labelStyle);
            y += 24;
            
            // Intel status
            GUI.Label(new Rect(x, y, w, 18), "<b>📁 INTEL</b>", _labelStyle);
            y += 20;
            
            string primaryStatus = _hasPrimaryIntel ? "<color=green>✓ ACQUIRED</color>" : "<color=yellow>○ Not collected</color>";
            GUI.Label(new Rect(x, y, w, 18), $"  Primary: {primaryStatus}", _labelStyle);
            y += 18;
            
            int secondaryCollected = _collectedIntelCount - (_hasPrimaryIntel ? 1 : 0);
            GUI.Label(new Rect(x, y, w, 18), $"  Secondary: {secondaryCollected}/{_secondaryIntelCount}", _labelStyle);
            y += 18;
            
            GUI.Label(new Rect(x, y, w, 18), $"  Points: {_totalPoints}", _labelStyle);
            y += 24;
            
            // Guards status
            GUI.Label(new Rect(x, y, w, 18), "<b>👮 GUARDS</b>", _labelStyle);
            y += 20;
            
            foreach (var guard in _guards)
            {
                if (guard == null) continue;
                
                string state = guard.CurrentState;
                string stateColor = "green";
                if (state.Contains("Chase")) stateColor = "red";
                else if (state.Contains("Investigate")) stateColor = "yellow";
                
                bool hasTarget = guard.Blackboard.TryGet<GameObject>(BBKeys.Target, out var target) && target != null;
                string targetIcon = hasTarget ? "👁️" : "";
                
                GUI.Label(new Rect(x, y, w, 16), $"  {guard.name}: <color={stateColor}>{state}</color> {targetIcon}", _labelStyle);
                y += 16;
            }
            y += 8;
            
            // Controls
            GUI.Label(new Rect(x, y, w, 18), "<b>Controls:</b>", _labelStyle);
            y += 18;
            GUI.Label(new Rect(x, y, w, 16), "  WASD - Move", _labelStyle);
            y += 16;
            GUI.Label(new Rect(x, y, w, 16), "  Shift - Sprint (loud!)", _labelStyle);
            y += 16;
            GUI.Label(new Rect(x, y, w, 16), "  E - Collect intel (when near)", _labelStyle);
            y += 16;
            GUI.Label(new Rect(x, y, w, 16), "  F1 - Toggle UI | R - Restart", _labelStyle);
            y += 20;
            
            // Game end message
            if (_gameEnded)
            {
                GUI.Label(new Rect(x, y, w, 80), $"<color=cyan><b>{_endMessage}</b></color>", _labelStyle);
            }
            
            // Handle keyboard input
            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.F1)
                {
                    _showOverlay = !_showOverlay;
                    Event.current.Use();
                }
                else if (Event.current.keyCode == KeyCode.R)
                {
                    RestartMission();
                    Event.current.Use();
                }
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// Component for collecting intel when player is nearby.
    /// </summary>
    public class IntelCollector : MonoBehaviour
    {
        private List<IntelPoint> _intelPoints;
        
        public void Initialize(List<IntelPoint> intelPoints)
        {
            _intelPoints = intelPoints;
        }
        
        private void Update()
        {
            if (_intelPoints == null) return;
            
            // Check for E key press to collect intel
            if (UnityEngine.InputSystem.Keyboard.current != null && 
                UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame)
            {
                TryCollectNearbyIntel();
            }
        }
        
        private void TryCollectNearbyIntel()
        {
            foreach (var intel in _intelPoints)
            {
                if (intel == null || intel.IsCollected) continue;
                
                if (intel.IsInRange(gameObject))
                {
                    intel.TryCollect(gameObject);
                    return;  // Only collect one at a time
                }
            }
            
            Debug.Log("<color=gray>[Stealth] No intel in range to collect.</color>");
        }
    }
}
