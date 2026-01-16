# Unity Project Structure Guide

Best practices for organizing Unity projects and creating editor tools.

## Table of Contents
- [Folder Structure](#folder-structure)
- [Naming Conventions](#naming-conventions)
- [Assembly Definitions](#assembly-definitions)
- [Editor Scripting](#editor-scripting)
- [ScriptableObjects](#scriptableobjects)
- [Debug Visualization](#debug-visualization)
- [Asset Package Structure](#asset-package-structure)

---

## Folder Structure

### Standard Unity Project

```
Assets/
├── _Project/                    # Your project-specific content
│   ├── Scenes/
│   │   ├── Main.unity
│   │   └── Menu.unity
│   ├── Scripts/
│   │   ├── Core/
│   │   ├── Player/
│   │   ├── AI/
│   │   └── UI/
│   ├── Prefabs/
│   ├── Materials/
│   ├── Textures/
│   ├── Audio/
│   └── Animations/
│
├── Plugins/                     # Third-party plugins
│   └── ThirdPartyAsset/
│
├── Editor/                      # Editor-only scripts (special folder)
│   └── CustomInspectors/
│
└── Resources/                   # Runtime-loaded assets (use sparingly)
    └── Configs/
```

### Asset Store Package Structure

```
Assets/
└── YourAssetName/              # Everything in one folder!
    ├── Documentation/
    │   ├── Manual.pdf
    │   ├── Changelog.txt
    │   └── QuickStart.txt
    │
    ├── Demo/
    │   ├── Scenes/
    │   │   ├── Demo_QuickStart.unity
    │   │   └── Demo_Advanced.unity
    │   ├── Scripts/
    │   │   └── DemoController.cs
    │   └── Prefabs/
    │       └── DemoUI.prefab
    │
    ├── Runtime/                 # Main asset code
    │   ├── Core/
    │   │   ├── Pathfinder.cs
    │   │   └── PathNode.cs
    │   ├── Components/
    │   │   ├── PathfindingAgent.cs
    │   │   └── PathfindingGrid.cs
    │   ├── Data/
    │   │   └── PathfindingSettings.cs
    │   └── YourAsset.Runtime.asmdef
    │
    ├── Editor/
    │   ├── Inspectors/
    │   │   ├── PathfindingAgentEditor.cs
    │   │   └── PathfindingGridEditor.cs
    │   ├── Windows/
    │   │   └── PathfindingDebugWindow.cs
    │   ├── Gizmos/
    │   │   └── PathGizmoDrawer.cs
    │   └── YourAsset.Editor.asmdef
    │
    ├── Prefabs/
    │   └── PathfindingManager.prefab
    │
    └── Gizmos/                  # Gizmo icons (special folder)
        └── PathfindingAgent Icon.png
```

### Why This Structure?

| Folder | Purpose |
|--------|--------|
| `Documentation/` | Required for Asset Store |
| `Demo/` | Self-contained demos (can be deleted by user) |
| `Runtime/` | Core functionality used in builds |
| `Editor/` | Editor-only code (excluded from builds) |
| `Prefabs/` | Pre-configured GameObjects |
| `Gizmos/` | Custom icons for components |

---

## Naming Conventions

### Files and Folders

```
PascalCase for:
├── FolderNames/
├── ScriptNames.cs
├── PrefabNames.prefab
├── SceneNames.unity
└── MaterialNames.mat

camelCase for:
└── Never used for Unity assets

snake_case for:
├── texture_albedo.png
├── texture_normal.png
└── audio_footstep_01.wav
```

### Script Naming

```csharp
// MonoBehaviours: Describe what they do
PathfindingAgent.cs       // Agent that pathfinds
PathfindingGrid.cs        // Grid for pathfinding
PathfindingManager.cs     // Manages pathfinding system

// ScriptableObjects: End with "Settings" or "Data" or "Config"
PathfindingSettings.cs    // Configuration data
AgentData.cs              // Agent stats/info
LevelConfig.cs            // Level configuration

// Editor Scripts: End with "Editor" or "Window" or "Drawer"
PathfindingAgentEditor.cs // Custom inspector for agent
PathfindingWindow.cs      // Editor window
PathPropertyDrawer.cs     // Property drawer

// Interfaces: Start with "I"
IPathfindable.cs
IDamageable.cs

// Abstract classes: Start with "Base" or are just the base name
BaseAgent.cs
Agent.cs  // (abstract, with AgentEnemy, AgentFriendly inheriting)
```

### Prefab Naming

```
Type_Name_Variant.prefab

Examples:
Agent_Enemy_Basic.prefab
Agent_Enemy_Armored.prefab
UI_Panel_Settings.prefab
VFX_Explosion_Small.prefab
Prop_Tree_Oak.prefab
```

---

## Assembly Definitions

### Why Use Assembly Definitions?

1. **Faster compilation** - Only recompile changed assemblies
2. **Clear dependencies** - Explicit references between systems
3. **Platform targeting** - Editor-only or specific platforms
4. **Encapsulation** - Hide internal implementation

### Runtime Assembly Definition

```json
// YourAsset.Runtime.asmdef
{
    "name": "YourAsset.Runtime",
    "rootNamespace": "YourCompany.YourAsset",
    "references": [],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

### Editor Assembly Definition

```json
// YourAsset.Editor.asmdef
{
    "name": "YourAsset.Editor",
    "rootNamespace": "YourCompany.YourAsset.Editor",
    "references": [
        "YourAsset.Runtime"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

---

## Editor Scripting

### Custom Inspector

```csharp
using UnityEngine;
using UnityEditor;

namespace YourCompany.YourAsset.Editor
{
    [CustomEditor(typeof(PathfindingAgent))]
    public class PathfindingAgentEditor : UnityEditor.Editor
    {
        private SerializedProperty _speed;
        private SerializedProperty _stoppingDistance;
        private SerializedProperty _debugMode;

        private void OnEnable()
        {
            // Cache serialized properties
            _speed = serializedObject.FindProperty("_speed");
            _stoppingDistance = serializedObject.FindProperty("_stoppingDistance");
            _debugMode = serializedObject.FindProperty("_debugMode");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // Header
            EditorGUILayout.LabelField("Pathfinding Agent", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Movement settings
            EditorGUILayout.LabelField("Movement", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(_speed, new GUIContent("Speed", "Movement speed in units/sec"));
            EditorGUILayout.PropertyField(_stoppingDistance);
            EditorGUILayout.Space();

            // Debug settings
            EditorGUILayout.LabelField("Debug", EditorStyles.miniBoldLabel);
            EditorGUILayout.PropertyField(_debugMode);

            // Buttons
            EditorGUILayout.Space();
            if (GUILayout.Button("Recalculate Path"))
            {
                ((PathfindingAgent)target).RecalculatePath();
            }

            serializedObject.ApplyModifiedProperties();
        }

        // Draw in Scene view
        private void OnSceneGUI()
        {
            PathfindingAgent agent = (PathfindingAgent)target;
            
            // Draw stopping distance handle
            Handles.color = Color.yellow;
            Handles.DrawWireDisc(
                agent.transform.position, 
                Vector3.up, 
                agent.StoppingDistance
            );
        }
    }
}
```

### Editor Window

```csharp
using UnityEngine;
using UnityEditor;

namespace YourCompany.YourAsset.Editor
{
    public class PathfindingDebugWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private bool _showGrid = true;
        private bool _showPaths = true;
        private PathfindingGrid _selectedGrid;

        [MenuItem("Window/Your Asset/Pathfinding Debug")]
        public static void ShowWindow()
        {
            var window = GetWindow<PathfindingDebugWindow>("Pathfinding Debug");
            window.minSize = new Vector2(300, 400);
        }

        private void OnEnable()
        {
            // Subscribe to scene view updates
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Pathfinding Debug", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Grid selection
            _selectedGrid = (PathfindingGrid)EditorGUILayout.ObjectField(
                "Grid", _selectedGrid, typeof(PathfindingGrid), true
            );

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Visualization", EditorStyles.miniBoldLabel);

            _showGrid = EditorGUILayout.Toggle("Show Grid", _showGrid);
            _showPaths = EditorGUILayout.Toggle("Show Paths", _showPaths);

            EditorGUILayout.Space();

            if (_selectedGrid != null)
            {
                EditorGUILayout.LabelField("Grid Info", EditorStyles.miniBoldLabel);
                EditorGUILayout.LabelField($"Size: {_selectedGrid.Width} x {_selectedGrid.Height}");
                EditorGUILayout.LabelField($"Cell Size: {_selectedGrid.CellSize}");
                EditorGUILayout.LabelField($"Walkable Cells: {_selectedGrid.WalkableCount}");

                EditorGUILayout.Space();

                if (GUILayout.Button("Rebuild Grid"))
                {
                    _selectedGrid.Rebuild();
                    SceneView.RepaintAll();
                }

                if (GUILayout.Button("Clear Cache"))
                {
                    _selectedGrid.ClearCache();
                }
            }

            // Stats section with scroll
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Active Agents", EditorStyles.miniBoldLabel);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            var agents = FindObjectsOfType<PathfindingAgent>();
            foreach (var agent in agents)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(agent.name);
                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    Selection.activeGameObject = agent.gameObject;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            // Repaint on play mode changes
            if (Application.isPlaying)
            {
                Repaint();
            }
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (_selectedGrid == null) return;

            if (_showGrid)
            {
                DrawGrid(_selectedGrid);
            }

            if (_showPaths)
            {
                DrawActivePaths();
            }
        }

        private void DrawGrid(PathfindingGrid grid)
        {
            // Grid visualization logic
            Handles.color = new Color(0, 1, 0, 0.3f);
            // Draw cells...
        }

        private void DrawActivePaths()
        {
            // Path visualization logic
            Handles.color = Color.cyan;
            // Draw paths...
        }
    }
}
```

### Property Drawer

```csharp
using UnityEngine;
using UnityEditor;

namespace YourCompany.YourAsset.Editor
{
    // Custom attribute
    public class MinMaxRangeAttribute : PropertyAttribute
    {
        public float Min;
        public float Max;

        public MinMaxRangeAttribute(float min, float max)
        {
            Min = min;
            Max = max;
        }
    }

    // Drawer for the attribute
    [CustomPropertyDrawer(typeof(MinMaxRangeAttribute))]
    public class MinMaxRangeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var range = (MinMaxRangeAttribute)attribute;

            if (property.propertyType == SerializedPropertyType.Vector2)
            {
                Vector2 value = property.vector2Value;
                
                EditorGUI.BeginChangeCheck();
                EditorGUI.MinMaxSlider(
                    position, 
                    label, 
                    ref value.x, 
                    ref value.y, 
                    range.Min, 
                    range.Max
                );
                
                if (EditorGUI.EndChangeCheck())
                {
                    property.vector2Value = value;
                }
            }
        }
    }
}

// Usage:
public class EnemySpawner : MonoBehaviour
{
    [MinMaxRange(1f, 60f)]
    public Vector2 spawnInterval = new Vector2(5f, 15f);
}
```

---

## ScriptableObjects

### Configuration ScriptableObject

```csharp
using UnityEngine;

namespace YourCompany.YourAsset
{
    [CreateAssetMenu(
        fileName = "PathfindingSettings", 
        menuName = "Your Asset/Pathfinding Settings"
    )]
    public class PathfindingSettings : ScriptableObject
    {
        [Header("Grid Settings")]
        [Tooltip("Size of each cell in world units")]
        [Range(0.1f, 5f)]
        public float cellSize = 1f;

        [Tooltip("Layers that block pathfinding")]
        public LayerMask obstacleLayers;

        [Header("Agent Settings")]
        [Range(1, 100)]
        public int maxPathLength = 50;

        [Range(0.1f, 10f)]
        public float defaultSpeed = 5f;

        [Header("Performance")]
        [Tooltip("Maximum path calculations per frame")]
        [Range(1, 20)]
        public int maxCalculationsPerFrame = 5;

        [Tooltip("Enable path caching")]
        public bool enableCaching = true;

        [Header("Debug")]
        public bool showDebugGizmos = true;
        public Color walkableColor = Color.green;
        public Color blockedColor = Color.red;
        public Color pathColor = Color.cyan;
    }
}
```

### Using ScriptableObject in Components

```csharp
public class PathfindingManager : MonoBehaviour
{
    [SerializeField] private PathfindingSettings _settings;

    // Provide default settings
    private PathfindingSettings Settings => _settings != null 
        ? _settings 
        : CreateDefaultSettings();

    private PathfindingSettings CreateDefaultSettings()
    {
        var settings = ScriptableObject.CreateInstance<PathfindingSettings>();
        // Set defaults...
        return settings;
    }

    private void Start()
    {
        InitializeGrid(Settings.cellSize, Settings.obstacleLayers);
    }
}
```

### Runtime Data Storage

```csharp
// For data that changes at runtime but persists
public class AgentStats : ScriptableObject
{
    public string agentName;
    public int level = 1;
    public float health = 100f;
    public List<string> abilities = new List<string>();

    public void Reset()
    {
        level = 1;
        health = 100f;
        abilities.Clear();
    }
}

// Create instances at runtime
public class AgentFactory
{
    public AgentStats CreateAgentStats(string name)
    {
        var stats = ScriptableObject.CreateInstance<AgentStats>();
        stats.agentName = name;
        return stats;
    }
}
```

---

## Debug Visualization

### Gizmos (Component-Based)

```csharp
using UnityEngine;

namespace YourCompany.YourAsset
{
    public class PathfindingAgent : MonoBehaviour
    {
        [SerializeField] private float _detectionRadius = 5f;
        [SerializeField] private bool _showDebug = true;

        private List<Vector3> _currentPath;

        // Always drawn (when Gizmos enabled in Scene view)
        private void OnDrawGizmos()
        {
            if (!_showDebug) return;

            // Detection radius (faded)
            Gizmos.color = new Color(1, 1, 0, 0.2f);
            Gizmos.DrawSphere(transform.position, _detectionRadius);
        }

        // Only drawn when selected
        private void OnDrawGizmosSelected()
        {
            // Detection radius (solid outline)
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _detectionRadius);

            // Current path
            if (_currentPath != null && _currentPath.Count > 1)
            {
                Gizmos.color = Color.cyan;
                for (int i = 0; i < _currentPath.Count - 1; i++)
                {
                    Gizmos.DrawLine(_currentPath[i], _currentPath[i + 1]);
                    Gizmos.DrawSphere(_currentPath[i], 0.2f);
                }
            }

            // Forward direction
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, transform.forward * 2f);
        }
    }
}
```

### Handles (Editor-Only, Interactive)

```csharp
using UnityEngine;
using UnityEditor;

namespace YourCompany.YourAsset.Editor
{
    [CustomEditor(typeof(PathfindingGrid))]
    public class PathfindingGridEditor : UnityEditor.Editor
    {
        private void OnSceneGUI()
        {
            PathfindingGrid grid = (PathfindingGrid)target;
            
            // Draw grid boundary with resize handles
            Handles.color = Color.white;
            
            Vector3 center = grid.transform.position;
            Vector3 size = new Vector3(grid.Width, 0.1f, grid.Height) * grid.CellSize;
            
            // Size handle
            EditorGUI.BeginChangeCheck();
            Vector3 newSize = Handles.ScaleHandle(
                size,
                center,
                Quaternion.identity,
                HandleUtility.GetHandleSize(center)
            );
            
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(grid, "Resize Grid");
                grid.Width = Mathf.RoundToInt(newSize.x / grid.CellSize);
                grid.Height = Mathf.RoundToInt(newSize.z / grid.CellSize);
            }

            // Draw grid cells
            DrawGridCells(grid);

            // Click to toggle walkability
            HandleCellClicks(grid);
        }

        private void DrawGridCells(PathfindingGrid grid)
        {
            for (int x = 0; x < grid.Width; x++)
            {
                for (int y = 0; y < grid.Height; y++)
                {
                    Vector3 cellPos = grid.GridToWorld(x, y);
                    bool walkable = grid.IsWalkable(x, y);

                    Handles.color = walkable 
                        ? new Color(0, 1, 0, 0.3f) 
                        : new Color(1, 0, 0, 0.3f);

                    Handles.DrawSolidRectangleWithOutline(
                        GetCellRect(cellPos, grid.CellSize),
                        Handles.color,
                        Color.white
                    );
                }
            }
        }

        private void HandleCellClicks(PathfindingGrid grid)
        {
            Event e = Event.current;
            
            if (e.type == EventType.MouseDown && e.button == 0 && e.control)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                Plane groundPlane = new Plane(Vector3.up, grid.transform.position);
                
                if (groundPlane.Raycast(ray, out float distance))
                {
                    Vector3 hitPoint = ray.GetPoint(distance);
                    Vector2Int cell = grid.WorldToGrid(hitPoint);
                    
                    Undo.RecordObject(grid, "Toggle Cell");
                    grid.ToggleWalkable(cell.x, cell.y);
                    
                    e.Use();
                }
            }
        }

        private Vector3[] GetCellRect(Vector3 center, float size)
        {
            float half = size * 0.5f;
            return new Vector3[]
            {
                center + new Vector3(-half, 0, -half),
                center + new Vector3(-half, 0, half),
                center + new Vector3(half, 0, half),
                center + new Vector3(half, 0, -half)
            };
        }
    }
}
```

### Debug GUI (Runtime)

```csharp
public class PathfindingDebugGUI : MonoBehaviour
{
    [SerializeField] private bool _showDebugUI = true;
    [SerializeField] private KeyCode _toggleKey = KeyCode.F1;

    private PathfindingManager _manager;
    private GUIStyle _boxStyle;
    private GUIStyle _labelStyle;

    private void Start()
    {
        _manager = FindObjectOfType<PathfindingManager>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(_toggleKey))
        {
            _showDebugUI = !_showDebugUI;
        }
    }

    private void OnGUI()
    {
        if (!_showDebugUI || _manager == null) return;

        // Initialize styles
        if (_boxStyle == null)
        {
            _boxStyle = new GUIStyle(GUI.skin.box);
            _boxStyle.normal.background = MakeTexture(new Color(0, 0, 0, 0.7f));
        }

        // Debug panel
        GUILayout.BeginArea(new Rect(10, 10, 250, 200));
        GUILayout.BeginVertical(_boxStyle);

        GUILayout.Label("=== Pathfinding Debug ===");
        GUILayout.Label($"Active Agents: {_manager.ActiveAgentCount}");
        GUILayout.Label($"Paths Calculated: {_manager.PathsCalculatedThisFrame}");
        GUILayout.Label($"Cache Hits: {_manager.CacheHitRate:P1}");
        GUILayout.Label($"Avg Path Time: {_manager.AveragePathTime:F2}ms");

        GUILayout.Space(10);
        GUILayout.Label("Press F1 to toggle");

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private Texture2D MakeTexture(Color color)
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        return tex;
    }
}
```

---

## Asset Package Structure

### Complete Package Example: EasyPath

```
Assets/
└── EasyPath/
    │
    ├── Documentation/
    │   ├── EasyPath_Manual.pdf
    │   ├── Changelog.txt
    │   └── QuickStart.txt
    │
    ├── Demo/
    │   ├── Scenes/
    │   │   ├── Demo_BasicSetup.unity
    │   │   ├── Demo_DynamicObstacles.unity
    │   │   └── Demo_Performance.unity
    │   ├── Scripts/
    │   │   ├── DemoController.cs
    │   │   └── ClickToMove.cs
    │   ├── Prefabs/
    │   │   ├── DemoAgent.prefab
    │   │   └── DemoUI.prefab
    │   └── Materials/
    │       └── Demo_Ground.mat
    │
    ├── Runtime/
    │   ├── Core/
    │   │   ├── AStarPathfinder.cs
    │   │   ├── PathNode.cs
    │   │   └── PriorityQueue.cs
    │   ├── Components/
    │   │   ├── EasyPathAgent.cs
    │   │   ├── EasyPathGrid.cs
    │   │   └── EasyPathObstacle.cs
    │   ├── Data/
    │   │   └── EasyPathSettings.cs
    │   ├── Interfaces/
    │   │   └── IPathfindable.cs
    │   └── EasyPath.Runtime.asmdef
    │
    ├── Editor/
    │   ├── Inspectors/
    │   │   ├── EasyPathAgentEditor.cs
    │   │   └── EasyPathGridEditor.cs
    │   ├── Windows/
    │   │   └── EasyPathDebugWindow.cs
    │   ├── MenuItems.cs
    │   └── EasyPath.Editor.asmdef
    │
    ├── Prefabs/
    │   └── EasyPathManager.prefab
    │
    └── Gizmos/
        ├── EasyPathAgent Icon.png
        └── EasyPathGrid Icon.png
```

### Package Export Checklist

```markdown
## Before Exporting Package

### Clean Up
- [ ] Remove any test scenes not intended for release
- [ ] Delete any debug logs (Debug.Log statements)
- [ ] Remove TODO comments
- [ ] Clear console of warnings

### Verify
- [ ] All scripts compile
- [ ] All prefabs have no missing references
- [ ] Demo scenes work with fresh import
- [ ] Documentation is up to date

### Export
1. Select your asset's root folder
2. Right-click → Export Package
3. Uncheck any files that shouldn't be included
4. Name: YourAsset_v1.0.0.unitypackage

### Test
- [ ] Import into fresh Unity project
- [ ] Verify no errors
- [ ] Run all demo scenes
- [ ] Check documentation links work
```
