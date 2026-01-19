# Performance Improvement Opportunities

This document identifies all performance optimization opportunities in the Unity Asset Toolkit codebase. The analysis covers both EasyPath (A* pathfinding) and NPCBrain (AI toolkit) systems.

**Performance Targets**: 100+ NPCs @ 60 FPS, <0.1ms per NPC tick cost

---

## Executive Summary

| Priority | Issue Count | Estimated Impact |
|----------|-------------|------------------|
| HIGH     | 6           | Major FPS gains for large NPC counts |
| MEDIUM   | 5           | Moderate improvements, reduced GC pressure |
| LOW      | 3           | Minor optimizations, already well-optimized |

**Already Optimized** (no changes needed):
- Blackboard type-specific storage (avoids boxing for common types)
- SightSensor list swapping pattern (avoids allocation)
- CriticalityController entropy dirty flag (lazy recalculation)
- UtilitySelector pre-allocated arrays
- SightSensor/HearingSensor raycast limiting per tick

---

## HIGH PRIORITY Issues

### 1. EasyPathAgent.RemainingDistance Property (Recalculates Every Access)

**File**: `assets/EasyPath/Assets/EasyPath/Runtime/Components/EasyPathAgent.cs:30,187-202`

**Issue**: The `RemainingDistance` property recalculates the entire path distance on every access. This O(n) operation iterates through all remaining waypoints with Vector3.Distance calls.

```csharp
// Current (called every frame in gameplay code)
public float RemainingDistance => CalculateRemainingDistance();

private float CalculateRemainingDistance()
{
    // O(n) Vector3.Distance calls every time property is accessed
    for (int i = _currentWaypointIndex; i < _currentPath.Count - 1; i++)
    {
        distance += Vector3.Distance(_currentPath[i], _currentPath[i + 1]);
    }
}
```

**Recommendation**: Cache the remaining distance and only recalculate when path changes or waypoint advances.

```csharp
private float _cachedRemainingDistance;
private bool _remainingDistanceDirty = true;
private int _lastWaypointIndex = -1;

public float RemainingDistance
{
    get
    {
        if (_remainingDistanceDirty || _currentWaypointIndex != _lastWaypointIndex)
        {
            _cachedRemainingDistance = CalculateRemainingDistance();
            _lastWaypointIndex = _currentWaypointIndex;
            _remainingDistanceDirty = false;
        }
        return _cachedRemainingDistance;
    }
}
```

**Impact**: Eliminates O(n) calculations per frame when RemainingDistance is polled frequently.

---

### 2. EasyPathAgent.FollowPath() - Multiple Transform Property Accesses

**File**: `assets/EasyPath/Assets/EasyPath/Runtime/Components/EasyPathAgent.cs:129-179`

**Issue**: Multiple `transform.position` accesses in the hot path. Each property access involves native Unity interop.

```csharp
private void FollowPath()
{
    // Line 138: First access
    targetWaypoint.y = transform.position.y;

    // Line 140: Second access
    Vector3 direction = (targetWaypoint - transform.position);

    // Line 178: Third access (write)
    transform.position += direction * moveDistance;
}
```

**Recommendation**: Cache `transform.position` at the start of FollowPath.

```csharp
private void FollowPath()
{
    Vector3 currentPos = transform.position;  // Cache once

    Vector3 targetWaypoint = _currentPath[_currentWaypointIndex];
    targetWaypoint.y = currentPos.y;

    Vector3 direction = (targetWaypoint - currentPos);
    // ...
    transform.position = currentPos + direction * moveDistance;
}
```

**Impact**: Reduces 3+ property accesses to 2 per frame per agent.

---

### 3. SightSensor.Tick() - Redundant Vector3 Calculations

**File**: `assets/NPCBrain/NPCBrain/Assets/NPCBrain/Runtime/Perception/SightSensor.cs:150-156`

**Issue**: The direction vector is normalized (line 151), then `Vector3.Distance` is called separately (line 152), but distance could be obtained during normalization. Also, `_viewAngle * 0.5f` is calculated for every target.

```csharp
for (int i = 0; i < count; i++)
{
    Vector3 directionToTarget = (targetPosition - eyePosition).normalized;  // Calculates magnitude internally
    float distanceToTarget = Vector3.Distance(eyePosition, targetPosition);  // Recalculates magnitude!

    if (angleToTarget > _viewAngle * 0.5f)  // Multiplied for each target
```

**Recommendation**: Pre-compute half angle and combine distance/direction calculation.

```csharp
// Pre-compute outside loop
float halfViewAngle = _viewAngle * 0.5f;

for (int i = 0; i < count; i++)
{
    Vector3 toTarget = targetPosition - eyePosition;
    float distanceToTarget = toTarget.magnitude;  // Calculate once
    Vector3 directionToTarget = distanceToTarget > 0.0001f
        ? toTarget / distanceToTarget
        : Vector3.zero;

    if (angleToTarget > halfViewAngle)
```

**Impact**: Eliminates redundant magnitude calculation per target (~40% reduction in vector math per target).

---

### 4. EasyPathGrid.GetNeighbors() - Allocation via yield return

**File**: `assets/EasyPath/Assets/EasyPath/Runtime/Components/EasyPathGrid.cs:174-207`

**Issue**: Using `yield return` creates an iterator allocation for every pathfinding query. This method is called many times per A* search.

```csharp
public IEnumerable<PathNode> GetNeighbors(PathNode node)
{
    for (int x = -1; x <= 1; x++)
    {
        for (int y = -1; y <= 1; y++)
        {
            // ...
            yield return neighbor;  // Allocates iterator
        }
    }
}
```

**Recommendation**: Use a cached list or accept a pre-allocated list to fill.

```csharp
private readonly List<PathNode> _neighborBuffer = new List<PathNode>(8);

public void GetNeighbors(PathNode node, List<PathNode> results)
{
    results.Clear();
    for (int x = -1; x <= 1; x++)
    {
        for (int y = -1; y <= 1; y++)
        {
            // ...
            results.Add(neighbor);
        }
    }
}
```

**Impact**: Eliminates GC allocation per A* node expansion (significant for large paths).

---

### 5. AStarPathfinder.ReconstructPath() - List Reversal Allocation

**File**: `assets/EasyPath/Assets/EasyPath/Runtime/Core/AStarPathfinder.cs:138-151`

**Issue**: Creates a new List and then reverses it, which is an O(n) operation on top of O(n) construction.

```csharp
private List<Vector3> ReconstructPath(PathNode endNode)
{
    List<Vector3> path = new List<Vector3>();  // Allocation
    while (current != null)
    {
        path.Add(current.WorldPosition);  // Build backwards
        current = current.Parent;
    }
    path.Reverse();  // O(n) extra operation
    return path;
}
```

**Recommendation**: Build path in correct order using a stack, or estimate capacity.

```csharp
private readonly Stack<Vector3> _pathStack = new Stack<Vector3>(64);

private List<Vector3> ReconstructPath(PathNode endNode)
{
    _pathStack.Clear();
    PathNode current = endNode;
    while (current != null)
    {
        _pathStack.Push(current.WorldPosition);
        current = current.Parent;
    }

    var path = new List<Vector3>(_pathStack.Count);
    while (_pathStack.Count > 0)
    {
        path.Add(_pathStack.Pop());
    }
    return path;
}
```

**Impact**: Eliminates Reverse() operation, reduces GC by reusing stack.

---

### 6. EasyPathGrid.ResetNodes() - O(width * height) Every Pathfind

**File**: `assets/EasyPath/Assets/EasyPath/Runtime/Components/EasyPathGrid.cs:127-136`

**Issue**: Resets ALL nodes before every pathfind operation, even nodes that weren't touched.

```csharp
public void ResetNodes()
{
    for (int x = 0; x < _width; x++)
    {
        for (int y = 0; y < _height; y++)
        {
            _nodes[x, y].Reset();  // O(width * height)
        }
    }
}
```

**Recommendation**: Track touched nodes and only reset those, or use a version number pattern.

```csharp
// Version-based reset (no O(n) cleanup needed)
private int _currentPathVersion = 0;

// In PathNode:
public int LastUsedVersion;
public bool NeedsReset => LastUsedVersion != _grid.CurrentPathVersion;

// In FindPath:
public List<Vector3> FindPath(...)
{
    _currentPathVersion++;  // Invalidates all nodes instantly
    // ...
}
```

**Impact**: For large grids (100x100 = 10,000 nodes), this eliminates 10,000 Reset() calls per pathfind.

---

## MEDIUM PRIORITY Issues

### 7. MoveTo.MoveDirectly() - Transform Access Pattern

**File**: `assets/NPCBrain/NPCBrain/Assets/NPCBrain/Runtime/BehaviorTree/Actions/MoveTo.cs:114-127`

**Issue**: Three separate transform property accesses in hot path.

```csharp
private NodeStatus MoveDirectly(Transform transform, Vector3 target, string debugName = "")
{
    Vector3 direction = (target - transform.position).normalized;  // Access 1
    transform.position += movement;  // Access 2 (read) + Access 3 (write)
    transform.rotation = Quaternion.LookRotation(direction);  // Access 4
}
```

**Recommendation**: Cache transform position locally.

**Impact**: Minor but compounds with many NPCs using direct movement.

---

### 8. UtilitySelector.SelectAction() - Math.Exp Per Action

**File**: `assets/NPCBrain/NPCBrain/Assets/NPCBrain/Runtime/BehaviorTree/Composites/UtilitySelector.cs:176`

**Issue**: `Math.Exp()` is called for every action every selection. This is a relatively expensive transcendental function.

```csharp
for (int i = 0; i < _actions.Count; i++)
{
    float scaledScore = (_scores[i] - maxScore) / temperature;
    _probabilities[i] = (float)Math.Exp(scaledScore);  // Expensive
}
```

**Recommendation**: Consider caching selection results when state hasn't changed significantly, or use a fast exp approximation for non-critical NPCs.

```csharp
// Fast exp approximation (within 2% accuracy for typical ranges)
private static float FastExp(float x)
{
    // Schraudolph's approximation
    const float a = (1 << 23) / 0.6931472f;
    const float b = (1 << 23) * (127 - 0.043677448f);
    int i = (int)(a * x + b);
    return BitConverter.Int32BitsToSingle(i);
}
```

**Impact**: Moderate savings when many actions scored frequently.

---

### 9. Blackboard.OnValueChanged Event - Fires on Every Set

**File**: `assets/NPCBrain/NPCBrain/Assets/NPCBrain/Runtime/Core/Blackboard.cs:84,93,110,127,144,173`

**Issue**: Event invocation occurs on every Set operation, even if value unchanged.

```csharp
public void Set<T>(string key, T value)
{
    _data[key] = new Entry { Value = value, HasExpiration = false };
    OnValueChanged?.Invoke(key, value);  // Always fires
}
```

**Recommendation**: Add a `SetIfChanged` method or check before invoking.

```csharp
public bool SetIfChanged<T>(string key, T value)
{
    if (TryGet<T>(key, out T existing) && EqualityComparer<T>.Default.Equals(existing, value))
    {
        return false;  // No change, no event
    }
    Set(key, value);
    return true;
}
```

**Impact**: Reduces event invocation overhead for frequently-updated values.

---

### 10. CriticalityController.RecordAction() - Dictionary Operations

**File**: `assets/NPCBrain/NPCBrain/Assets/NPCBrain/Runtime/Criticality/CriticalityController.cs:121-128`

**Issue**: Uses ContainsKey followed by indexer access (two lookups).

```csharp
if (_actionCounts.ContainsKey(actionId))  // Lookup 1
{
    _actionCounts[actionId]++;  // Lookup 2
}
```

**Recommendation**: Use TryGetValue pattern.

```csharp
if (_actionCounts.TryGetValue(actionId, out int count))
{
    _actionCounts[actionId] = count + 1;  // Still 2 ops but cleaner
}
else
{
    _actionCounts[actionId] = 1;
}

// Or use CollectionsMarshal.GetValueRefOrAddDefault in .NET 6+
```

**Impact**: Minor optimization, but good practice.

---

### 11. EasyPathGrid.GridToWorld() - Repeated transform.position Access

**File**: `assets/EasyPath/Assets/EasyPath/Runtime/Components/EasyPathGrid.cs:212-220`

**Issue**: Accesses `transform.position` on every call, even during grid building (O(width * height) calls).

```csharp
public Vector3 GridToWorld(int x, int y)
{
    Vector3 origin = transform.position;  // Called width*height times during BuildGrid
    return new Vector3(
        origin.x + x * _cellSize + _cellSize * 0.5f,
        origin.y,
        origin.z + y * _cellSize + _cellSize * 0.5f
    );
}
```

**Recommendation**: Cache origin in BuildGrid and pass it to an internal version.

```csharp
public void BuildGrid()
{
    Vector3 origin = transform.position;  // Cache once
    for (int x = 0; x < _width; x++)
    {
        for (int y = 0; y < _height; y++)
        {
            Vector3 worldPos = GridToWorldInternal(x, y, origin);
            // ...
        }
    }
}

private Vector3 GridToWorldInternal(int x, int y, Vector3 origin)
{
    return new Vector3(
        origin.x + x * _cellSize + _cellSize * 0.5f,
        origin.y,
        origin.z + y * _cellSize + _cellSize * 0.5f
    );
}
```

**Impact**: Eliminates width*height native interop calls during grid initialization.

---

## LOW PRIORITY Issues (Already Well-Optimized)

### 12. SightSensor - Already Optimized List Swapping

**File**: `assets/NPCBrain/NPCBrain/Assets/NPCBrain/Runtime/Perception/SightSensor.cs:81-94`

**Status**: ALREADY OPTIMIZED. Uses list swapping pattern to avoid allocation.

```csharp
// Good pattern - swap lists instead of copying
var temp = _previousTargets;
_previousTargets = _visibleTargets;
_visibleTargets = temp;
_visibleTargets.Clear();
```

---

### 13. Blackboard Type-Specific Storage

**File**: `assets/NPCBrain/NPCBrain/Assets/NPCBrain/Runtime/Core/Blackboard.cs:44-48`

**Status**: ALREADY OPTIMIZED. Has dedicated dictionaries for common types to avoid boxing.

```csharp
// Good pattern - avoids boxing for common types
private readonly Dictionary<string, float> _floatData;
private readonly Dictionary<string, int> _intData;
private readonly Dictionary<string, bool> _boolData;
private readonly Dictionary<string, Vector3> _vectorData;
```

---

### 14. CriticalityController Entropy Caching

**File**: `assets/NPCBrain/NPCBrain/Assets/NPCBrain/Runtime/Criticality/CriticalityController.cs:147-151`

**Status**: ALREADY OPTIMIZED. Uses dirty flag to avoid recalculating entropy every tick.

```csharp
// Good pattern - lazy recalculation
if (_entropyDirty)
{
    _entropy = CalculateEntropy();
    _entropyDirty = false;
}
```

---

## Scalability Improvements (Future Consideration)

### 15. Spatial Hashing for Large NPC Counts

**Applicable to**: SightSensor, NPCRegistry.FindNearest()

For 100+ NPCs, consider implementing spatial hashing:

```csharp
public class SpatialHash<T> where T : Component
{
    private readonly Dictionary<int, List<T>> _cells;
    private readonly float _cellSize;

    public void UpdatePosition(T obj, Vector3 position)
    {
        int hash = GetCellHash(position);
        // Move obj to new cell if changed
    }

    public void GetNearby(Vector3 position, float radius, List<T> results)
    {
        // Only check cells within radius
    }
}
```

### 16. NPC LOD System

For distant NPCs, reduce tick frequency:

```csharp
public class NPCLODManager
{
    public float TickIntervalNear = 0f;      // Every frame
    public float TickIntervalMedium = 0.1f;  // 10 Hz
    public float TickIntervalFar = 0.5f;     // 2 Hz
    public float TickIntervalVeryFar = 1f;   // 1 Hz

    public float GetTickInterval(float distanceToPlayer)
    {
        if (distanceToPlayer < 20f) return TickIntervalNear;
        if (distanceToPlayer < 50f) return TickIntervalMedium;
        if (distanceToPlayer < 100f) return TickIntervalFar;
        return TickIntervalVeryFar;
    }
}
```

---

## Implementation Priority

### Phase 1: Critical Path Optimizations
1. Fix EasyPathGrid.ResetNodes() version pattern (HIGH - affects all pathfinding)
2. Fix GetNeighbors() yield allocation (HIGH - GC pressure)
3. Cache RemainingDistance in EasyPathAgent (HIGH - frequent polling)

### Phase 2: Per-Tick Optimizations
4. SightSensor Vector3 calculation deduplication (HIGH)
5. EasyPathAgent.FollowPath transform caching (HIGH)
6. MoveTo transform access consolidation (MEDIUM)

### Phase 3: Future Scalability
7. Spatial hashing for 100+ NPCs
8. NPC LOD system for tick throttling
9. Fast exp approximation for UtilitySelector

---

## Profiling Recommendations

Before implementing changes, profile with Unity Profiler to verify bottlenecks:

1. **CPU Usage Timeline**: Identify per-frame spikes
2. **Scripts Section**: Find expensive MonoBehaviour methods
3. **GC Allocations**: Track memory pressure (should be near-zero in hot paths)
4. **Deep Profile Mode**: Get exact line-level CPU cost (use sparingly, significant overhead)

See `docs/NPCBRAIN-PROFILING-GUIDE.md` for detailed profiling instructions.
