# NPCBrain Performance Profiling Guide

Complete guide to profiling NPCBrain performance and meeting MVP targets.

---

## Performance Targets

| Metric | Target | Priority |
|--------|--------|----------|
| NPCs at 60 FPS | 100+ | High |
| Per-NPC tick cost | < 0.1ms | High |
| Memory per NPC | < 1KB | Medium |
| Perception raycasts | Max 3/frame/NPC | High |

---

## Method 1: Unity Profiler (Recommended)

### Step 1: Open the Profiler

1. **Window → Analysis → Profiler** (Ctrl+7)
2. **Dock the Profiler** window next to Scene/Game view for easy monitoring

### Step 2: Set Up Profiling

**Important Settings**:
- Check **"Record"** button (red dot in top-left)
- Enable **"Deep Profiling"** for detailed call stacks (Profiler → Deep Profiling)
  - ⚠️ Warning: Deep profiling adds overhead, use for detailed analysis only
- Set **"Color Blind Mode"** if needed (Profiler dropdown → Color Blind Mode)

### Step 3: Create Performance Test Scene

Create a stress test scene with many NPCs:

```csharp
// File: assets/NPCBrain/NPCBrain/Assets/NPCBrain/Demo/Scripts/PerformanceTest.cs
using UnityEngine;
using NPCBrain;

public class PerformanceTest : MonoBehaviour
{
    [Header("Test Configuration")]
    [SerializeField] private GameObject _npcPrefab;
    [SerializeField] private int _npcCount = 100;
    [SerializeField] private float _spawnRadius = 50f;
    [SerializeField] private bool _spawnOnStart = true;

    [Header("Performance Monitoring")]
    [SerializeField] private bool _showStats = true;
    [SerializeField] private KeyCode _spawnKey = KeyCode.Space;
    [SerializeField] private KeyCode _clearKey = KeyCode.C;

    private GameObject[] _npcs;
    private float _avgFrameTime;
    private float _peakFrameTime;
    private int _frameCount;

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
        }

        Debug.Log($"Spawned {_npcCount} NPCs for performance testing");
        ResetStats();
    }

    void ClearNPCs()
    {
        if (_npcs != null)
        {
            foreach (var npc in _npcs)
            {
                if (npc != null)
                    Destroy(npc);
            }
        }
        _npcs = null;
    }

    void ResetStats()
    {
        _avgFrameTime = 0;
        _peakFrameTime = 0;
        _frameCount = 0;
    }

    void OnGUI()
    {
        if (!_showStats) return;

        int activeNPCs = _npcs != null ? _npcs.Length : 0;
        float fps = 1000f / _avgFrameTime;

        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Box("Performance Stats", GUILayout.Width(290));

        GUILayout.Label($"Active NPCs: {activeNPCs}");
        GUILayout.Label($"FPS: {fps:F1} ({_avgFrameTime:F2}ms avg)");
        GUILayout.Label($"Peak Frame: {_peakFrameTime:F2}ms");
        GUILayout.Label($"Per-NPC Cost: {(_avgFrameTime / Mathf.Max(1, activeNPCs)):F3}ms");

        GUILayout.Space(10);
        GUILayout.Label("Controls:");
        GUILayout.Label($"[{_spawnKey}] Spawn {_npcCount} NPCs");
        GUILayout.Label($"[{_clearKey}] Clear NPCs");

        GUILayout.EndArea();
    }
}
```

### Step 4: Run Performance Test

1. **Create test scene**:
   - Empty scene with ground plane
   - Add PerformanceTest GameObject with script above
   - Create NPC prefab (PatrolNPC, GuardNPC, or UtilityNPC)
   - Assign prefab to PerformanceTest component

2. **Enter Play Mode**:
   - Press Play
   - NPCs spawn automatically (or press Space)
   - Let run for 30-60 seconds to collect data

3. **Monitor Profiler**:
   - Watch **CPU Usage** module
   - Look for spikes in frame time
   - Check **Scripts** breakdown

### Step 5: Analyze Results

#### CPU Usage Module
Look at the timeline view:
- **Green bar** = Rendering
- **Blue bar** = Scripts
- **Orange bar** = Physics
- **Red bar** = GC.Alloc (garbage collection)

**What to look for**:
- Frame time should stay under 16.67ms (60 FPS)
- Scripts (blue) should be minimal (<5ms for 100 NPCs = 0.05ms per NPC)
- Avoid GC spikes (red bars indicate memory allocations)

#### Hierarchy View
Click on a frame, then expand:
- **PlayerLoop → Update.ScriptRunBehaviourUpdate**
  - Find your NPC scripts here
  - Look for `NPCBrainController.Update()`
  - Check time per call

### Step 6: Deep Dive with Deep Profiling

1. **Enable Deep Profiling**: Profiler → Deep Profiling checkbox
2. **Run test** with fewer NPCs (20-30 due to overhead)
3. **Expand call stack**:
   ```
   Update.ScriptRunBehaviourUpdate
   └─ NPCBrainController.Update()
      ├─ Perception.Tick()
      │  └─ SightSensor.GetVisibleTargets()
      │     └─ Physics.Raycast()  ← Check time here
      ├─ Criticality.Update()
      └─ BehaviorTree.Tick()
         └─ [Your BT nodes]
   ```

4. **Identify bottlenecks**:
   - Which methods take the most time?
   - Are there unexpected allocations?

---

## Method 2: Unity Frame Debugger

For GPU/rendering analysis:

1. **Window → Analysis → Frame Debugger**
2. **Enable** in Play mode
3. **Step through draw calls** to see what's being rendered
4. **Check**:
   - Number of draw calls (fewer is better)
   - Overdraw (transparency, overlapping geometry)
   - Shader complexity

---

## Method 3: Memory Profiler

To validate memory target (<1KB per NPC):

1. **Install Memory Profiler package**:
   - Window → Package Manager
   - Search "Memory Profiler"
   - Install

2. **Window → Analysis → Memory Profiler**

3. **Capture snapshot**:
   - With 0 NPCs
   - With 100 NPCs
   - Compare the difference

4. **Analyze**:
   ```
   Memory difference / 100 NPCs = Memory per NPC
   Target: < 1KB (1024 bytes) per NPC
   ```

5. **Look for**:
   - Managed heap allocations
   - Native memory (GameObject overhead)
   - Arrays, lists, dictionaries

---

## Method 4: Custom Profiling Markers

Add detailed markers to NPCBrain code:

```csharp
using Unity.Profiling;
using UnityEngine;

public class NPCBrainController : MonoBehaviour
{
    // Define profiler markers
    private static readonly ProfilerMarker s_PerceptionMarker = new ProfilerMarker("NPCBrain.Perception");
    private static readonly ProfilerMarker s_CriticalityMarker = new ProfilerMarker("NPCBrain.Criticality");
    private static readonly ProfilerMarker s_BehaviorTreeMarker = new ProfilerMarker("NPCBrain.BehaviorTree");

    void Update()
    {
        // Perception
        s_PerceptionMarker.Begin();
        Perception?.Tick(this);
        s_PerceptionMarker.End();

        // Criticality
        s_CriticalityMarker.Begin();
        Criticality.Update();
        s_CriticalityMarker.End();

        // Behavior Tree
        s_BehaviorTreeMarker.Begin();
        if (_behaviorTree != null)
        {
            _lastStatus = _behaviorTree.Execute(this);
        }
        s_BehaviorTreeMarker.End();
    }
}
```

**Benefits**:
- Appear as named entries in Profiler
- Easy to identify in hierarchy
- No overhead when profiler not active

---

## Method 5: Performance Test Script

Create automated performance test:

```csharp
// File: assets/NPCBrain/NPCBrain/Assets/NPCBrain/Tests/Editor/PerformanceBenchmark.cs
using UnityEngine;
using UnityEditor;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

public class PerformanceBenchmark
{
    [MenuItem("NPCBrain/Run Performance Benchmark")]
    public static void RunBenchmark()
    {
        EditorApplication.isPlaying = true;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            EditorApplication.update += RunTests;
        }
    }

    private static int _frameCount = 0;
    private static bool _testsStarted = false;

    private static void RunTests()
    {
        if (!_testsStarted)
        {
            _testsStarted = true;
            SpawnTestNPCs();
        }

        _frameCount++;

        // Run for 300 frames (5 seconds at 60 FPS)
        if (_frameCount >= 300)
        {
            ReportResults();
            EditorApplication.update -= RunTests;
            EditorApplication.isPlaying = false;
        }
    }

    private static void SpawnTestNPCs()
    {
        // Spawn NPCs programmatically
        Debug.Log("Starting performance benchmark with 100 NPCs...");
        // Implementation: Instantiate NPCs
    }

    private static void ReportResults()
    {
        float avgFPS = _frameCount / 5f; // 5 seconds
        Debug.Log($"Performance Benchmark Complete:");
        Debug.Log($"  Average FPS: {avgFPS:F1}");
        Debug.Log($"  Target: 60 FPS with 100+ NPCs");
        Debug.Log($"  Status: {(avgFPS >= 60 ? "PASS" : "FAIL")}");
    }
}
```

---

## Common Performance Issues & Solutions

### Issue 1: Raycasts Too Expensive

**Symptoms**:
- High time in `Physics.Raycast()`
- Physics (orange) spikes in profiler

**Solutions**:
```csharp
// In SightSensor.cs
[SerializeField] private int maxRaycastsPerTick = 3;  // Limit raycasts
[SerializeField] private int raycastEveryNFrames = 1; // Skip frames

// Spread raycasts across frames
private int _frameCounter = 0;

void Tick()
{
    _frameCounter++;
    if (_frameCounter % raycastEveryNFrames != 0) return;

    // Only raycast every N frames
    UpdateVisibleTargets();
}
```

### Issue 2: GC Allocations

**Symptoms**:
- Red bars in profiler (GC.Alloc)
- Frame stutters

**Solutions**:
```csharp
// ❌ Bad: Creates new list every frame
public List<GameObject> GetVisibleTargets()
{
    return new List<GameObject>(_visibleTargets);
}

// ✅ Good: Reuse cached list
private List<GameObject> _cachedVisibleTargets = new List<GameObject>();

public List<GameObject> GetVisibleTargets()
{
    return _visibleTargets; // Return reference, no allocation
}
```

### Issue 3: Too Many Active NPCs

**Symptoms**:
- Slow even with optimizations
- Linear performance degradation

**Solutions**:
1. **Distance-based updates**:
   ```csharp
   // Only tick NPCs within range of player
   float distToPlayer = Vector3.Distance(transform.position, playerPos);
   if (distToPlayer > 50f)
   {
       _tickInterval = 0.5f; // Slow updates for distant NPCs
   }
   else
   {
       _tickInterval = 0f; // Every frame for close NPCs
   }
   ```

2. **LOD system for AI**:
   ```csharp
   // Different tick rates based on distance
   if (distToPlayer < 10f) tickRate = 0; // Every frame
   else if (distToPlayer < 30f) tickRate = 0.1f; // 10 fps
   else tickRate = 0.5f; // 2 fps
   ```

### Issue 4: Expensive Behavior Trees

**Symptoms**:
- High time in `BehaviorTree.Tick()`
- Complex trees with many nodes

**Solutions**:
- Cache getter functions instead of recalculating
- Use early-exit patterns in Selectors
- Avoid deep nesting (>5 levels)

---

## Profiling Checklist

### Before Profiling
- [ ] Build in **Development Build** mode (Build Settings)
- [ ] Enable **Autoconnect Profiler** (Build Settings)
- [ ] Disable **V-Sync** (Edit → Project Settings → Quality)
- [ ] Set **Target Frame Rate** to unlimited: `Application.targetFrameRate = -1;`

### During Profiling
- [ ] Run for 30+ seconds to get stable average
- [ ] Test with target NPC count (100+)
- [ ] Test in realistic scenes (obstacles, navigation)
- [ ] Test all archetype types (Guard, Patrol, Utility)

### After Profiling
- [ ] Document results in performance report
- [ ] Identify top 3 bottlenecks
- [ ] Optimize and re-test
- [ ] Validate targets met

---

## Performance Report Template

```markdown
# NPCBrain Performance Report

**Date**: [Date]
**Unity Version**: [Version]
**Hardware**: [CPU, GPU, RAM]

## Test Configuration
- NPCs: [Count]
- Archetype: [GuardNPC/PatrolNPC/UtilityNPC]
- Scene: [Description]

## Results

| Metric | Target | Actual | Status |
|--------|--------|--------|--------|
| FPS (100 NPCs) | 60+ | [X] | [PASS/FAIL] |
| Per-NPC tick cost | <0.1ms | [X]ms | [PASS/FAIL] |
| Memory per NPC | <1KB | [X]KB | [PASS/FAIL] |
| GC allocations | 0 | [X]KB/frame | [PASS/FAIL] |

## Bottlenecks Identified
1. [Method name] - [X]ms ([Y]%)
2. [Method name] - [X]ms ([Y]%)
3. [Method name] - [X]ms ([Y]%)

## Optimizations Applied
- [Description of fix]
- [Description of fix]

## Recommendations
- [Next steps]
```

---

## Quick Reference Commands

```csharp
// In code - disable features for testing
#if UNITY_EDITOR
    [MenuItem("NPCBrain/Profiling/Disable Perception")]
    public static void DisablePerception()
    {
        foreach (var npc in FindObjectsOfType<NPCBrainController>())
        {
            var sensor = npc.GetComponent<SightSensor>();
            if (sensor) sensor.enabled = false;
        }
    }

    [MenuItem("NPCBrain/Profiling/Disable Criticality")]
    public static void DisableCriticality()
    {
        // Test impact of criticality system
    }
#endif
```

---

## Recommended Workflow

1. **Baseline Test** (no optimizations)
   - Spawn 100 NPCs
   - Run for 60 seconds
   - Record FPS and frame times

2. **Deep Profile** (20 NPCs with deep profiling)
   - Identify top 3 expensive methods
   - Document call stacks

3. **Optimize** targeted bottlenecks
   - Apply fixes from "Common Issues" above
   - Measure impact of each fix

4. **Re-test** with 100+ NPCs
   - Verify targets met
   - Document final results

5. **Stress Test** (200-500 NPCs)
   - Find breaking point
   - Document scalability limits

---

## Next Steps

After profiling:
1. Document results in `docs/NPCBRAIN-PERFORMANCE-REPORT.md`
2. Update CLAUDE.md with performance characteristics
3. Add performance notes to Asset Store description
4. Create performance demo scene if impressive results
