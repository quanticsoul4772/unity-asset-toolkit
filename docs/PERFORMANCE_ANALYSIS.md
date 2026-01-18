# NPCBrain Performance Analysis Report

This document identifies all performance improvement opportunities in the NPCBrain codebase, organized by priority and impact.

---

## 🔴 CRITICAL - Hot Path Issues (Fix First)

These issues occur every frame in Update/Tick methods and have the highest performance impact.

### 1. O(n²) Algorithms in Per-Frame Code

#### HearingSensor.cs (Lines 223-240)
**Problem:** Nested foreach loop for sound comparison is O(n²)
```csharp
// Current implementation
foreach (var sound in _heardSounds)
{
    bool isNew = true;
    foreach (var prev in _previousSounds)
    {
        if (prev == sound) { isNew = false; break; }
    }
    // ...
}
```
**Impact:** With 10 sounds heard and 10 previous sounds, this is 100 comparisons per tick.

**Fix:** Use `HashSet<SoundEvent>` for `_previousSounds`:
```csharp
private readonly HashSet<SoundEvent> _previousSoundsSet = new HashSet<SoundEvent>();

// In Tick():
bool isNew = !_previousSoundsSet.Contains(sound);
```

---

#### SightSensor.cs (Lines 193-206)
**Problem:** `List.Contains()` is O(n) - called twice per target
```csharp
if (!_previousTargets.Contains(target))  // O(n)
if (!_visibleTargets.Contains(target))   // O(n)
```
**Impact:** With 10 visible targets, each Contains is 10 comparisons.

**Fix:** Add parallel `HashSet<GameObject>` for O(1) lookups:
```csharp
private readonly HashSet<GameObject> _visibleTargetsSet = new HashSet<GameObject>();
private readonly HashSet<GameObject> _previousTargetsSet = new HashSet<GameObject>();
```

---

#### TargetSelector.cs (Lines 144-153)
**Problem:** Nested loop to check if target already added
```csharp
bool alreadyAdded = false;
foreach (var scored in _scoredTargets)
{
    if (scored.Target == mem.Target)
    {
        alreadyAdded = true;
        break;
    }
}
```
**Fix:** Use `HashSet<GameObject> _addedTargets` for O(1) lookups.

---

### 2. Dictionary Foreach Allocations (Every Tick!)

**Problem:** `foreach (var kvp in dictionary)` allocates an enumerator struct that gets boxed.

**Affected Files:**
- `Memory.cs` (Lines 141, 165, 260, 334)
- `CriticalityController.cs` (Line 177)
- `Blackboard.cs` (Lines 200, 225)

**Fix Options:**
1. Cache dictionary keys in a `List<TKey>` and iterate with `for` loop
2. Use `Dictionary.GetEnumerator()` explicitly with `using` statement
3. For small dictionaries, consider `List<KeyValuePair<K,V>>` instead

```csharp
// Instead of:
foreach (var kvp in _memories) { ... }

// Use:
_cachedKeys.Clear();
_cachedKeys.AddRange(_memories.Keys);
for (int i = 0; i < _cachedKeys.Count; i++)
{
    var key = _cachedKeys[i];
    var value = _memories[key];
    // ...
}
```

---

### 3. Per-Tick List Allocations

#### SightSensor.cs (Lines 79-80) & HearingSensor.cs (Lines 115-116)
**Problem:** Every tick clears and copies list contents
```csharp
_previousTargets.Clear();
_previousTargets.AddRange(_visibleTargets); // May reallocate
_visibleTargets.Clear();
```
**Impact:** Potential memory allocation every frame.

**Fix:** Swap references instead of copying:
```csharp
// Swap lists instead of copying
(_previousTargets, _visibleTargets) = (_visibleTargets, _previousTargets);
_visibleTargets.Clear();
```

---

#### HearingSensor.cs (Line 241)
**Problem:** `List.Sort()` with lambda allocates
```csharp
_heardSounds.Sort((a, b) => b.Priority.CompareTo(a.Priority));
```
**Fix:** Use a cached `IComparer<SoundEvent>` instance:
```csharp
private static readonly Comparison<SoundEvent> _priorityComparer = 
    (a, b) => b.Priority.CompareTo(a.Priority);

// Usage:
_heardSounds.Sort(_priorityComparer);
```

---

### 4. Repeated GetComponent Calls

#### CopNPC.cs (Lines 111 & 201)
**Problem:** `GetComponent<RobberNPC>()` called twice on same target
```csharp
// Line 111
if (target.GetComponent<RobberNPC>() == null) return;
// Line 201
var robber = target.GetComponent<RobberNPC>();
```
**Fix:** Cache result in Blackboard or use single call with null check:
```csharp
var robber = target.GetComponent<RobberNPC>();
if (robber == null) return;
Blackboard.Set("targetRobber", robber);
```

---

## 🟠 HIGH PRIORITY - Significant Impact

### 5. Math.Log() Called Every Frame

#### CriticalityController.cs (Lines 177-185)
**Problem:** `CalculateEntropy()` calls `Math.Log()` for each unique action type every frame.

**Fix:** Only recalculate when action history changes:
```csharp
private bool _entropyDirty = true;
private float _cachedEntropy;

public void RecordAction(int actionId)
{
    // ... existing code ...
    _entropyDirty = true;
}

public void Update()
{
    if (_entropyDirty)
    {
        _entropy = CalculateEntropy();
        _entropyDirty = false;
    }
    // ... rest of update
}
```

---

### 6. Object Pooling Needed

#### TargetSelector.cs (Line 186)
**Problem:** `new ScoredTarget()` created every evaluation cycle
```csharp
return new ScoredTarget { ... };
```
**Fix:** Implement object pooling:
```csharp
private readonly Stack<ScoredTarget> _scoredTargetPool = new Stack<ScoredTarget>(16);

private ScoredTarget GetScoredTarget()
{
    return _scoredTargetPool.Count > 0 
        ? _scoredTargetPool.Pop() 
        : new ScoredTarget();
}

private void ReturnScoredTarget(ScoredTarget st)
{
    _scoredTargetPool.Push(st);
}
```

#### SoundManager.cs (Line 48)
**Problem:** `new SoundEvent()` for every emitted sound
**Fix:** Pool `SoundEvent` objects similarly.

---

### 7. NPCRegistry Array Creation

#### NPCRegistry.cs (Line 31)
**Problem:** `ToArray()` allocates new array even if count hasn't changed
```csharp
public static T[] GetAll()
{
    if (_isDirty || _cachedArray == null)
    {
        _cachedArray = _instances.ToArray(); // Always allocates
        _isDirty = false;
    }
    return _cachedArray;
}
```
**Fix:** Only reallocate when count changes:
```csharp
private static int _lastCount = -1;

public static T[] GetAll()
{
    if (_isDirty || _cachedArray == null || _instances.Count != _lastCount)
    {
        _cachedArray = _instances.ToArray();
        _lastCount = _instances.Count;
        _isDirty = false;
    }
    return _cachedArray;
}
```
**Better Fix:** Return `IReadOnlyList<T>` instead of array to avoid allocation entirely.

---

### 8. Blackboard Value Type Boxing

**Problem:** All `Blackboard.Set<T>()` calls with value types (float, int, bool) box the value:
```csharp
Blackboard.Set("alertLevel", 0f);     // boxes float
Blackboard.Set("canSeeCop", false);   // boxes bool
```
**Impact:** Garbage collection pressure from frequent state updates.

**Fix:** Add type-specific methods:
```csharp
private readonly Dictionary<string, float> _floatData = new Dictionary<string, float>();
private readonly Dictionary<string, int> _intData = new Dictionary<string, int>();
private readonly Dictionary<string, bool> _boolData = new Dictionary<string, bool>();

public void SetFloat(string key, float value) { _floatData[key] = value; }
public float GetFloat(string key, float defaultValue = 0f) { ... }
// Similar for int, bool, Vector3, etc.
```

---

## 🟡 MEDIUM PRIORITY - Noticeable Impact

### 9. SoundManager Redundant Cleanup

#### SoundManager.cs (Lines 77, 96)
**Problem:** `CleanupOldSounds()` called by every `HearingSensor` every tick
**Impact:** With 10 NPCs, cleanup runs 10 times per frame.

**Fix:** Track last cleanup frame:
```csharp
private static int _lastCleanupFrame = -1;

public static void CleanupOldSounds()
{
    if (Time.frameCount == _lastCleanupFrame) return;
    _lastCleanupFrame = Time.frameCount;
    
    // ... actual cleanup code
}
```

---

### 10. Blackboard CleanupExpired() Inefficiency

#### Blackboard.cs (Lines 200-210)
**Problem:** Iterates ALL keys every tick to find expired ones
```csharp
public void CleanupExpired()
{
    foreach (var kvp in _data) // Iterates everything
    {
        if (kvp.Value.HasExpiration && ...)
```
**Fix:** Track expiring keys separately in a priority queue:
```csharp
private readonly SortedList<float, string> _expiringKeys = new SortedList<float, string>();

public void SetWithTTL<T>(string key, T value, float ttlSeconds)
{
    float expirationTime = Time.time + ttlSeconds;
    // ... existing code ...
    _expiringKeys.Add(expirationTime, key);
}

public void CleanupExpired()
{
    float now = Time.time;
    while (_expiringKeys.Count > 0 && _expiringKeys.Keys[0] <= now)
    {
        string key = _expiringKeys.Values[0];
        _expiringKeys.RemoveAt(0);
        _data.Remove(key);
        OnValueExpired?.Invoke(key);
    }
}
```

---

### 11. Distance Calculation Redundancy

#### RobberNPC.cs (Lines 161-175)
**Problem:** Distance calculated, then potentially recalculated for raycast
```csharp
float distance = Vector3.Distance(transform.position, copNPC.transform.position);
// ... later in same method
if (!Physics.Raycast(transform.position + Vector3.up, dirToCop, distance - 0.5f))
```
**Fix:** Cache `transform.position` at start of method:
```csharp
Vector3 myPosition = transform.position;
// Use myPosition throughout method
```

---

### 12. Raycast Rate Limiting

#### RobberNPC.cs (Line 175)
**Problem:** Raycast for every cop every `LateUpdate()` - no limit
```csharp
if (!Physics.Raycast(transform.position + Vector3.up, dirToCop, distance - 0.5f))
```
**Fix:** Implement raycast budget like `SightSensor`:
```csharp
[SerializeField] private int _maxCopRaycastsPerFrame = 2;
private int _raycastCount;

private void LateUpdate()
{
    _raycastCount = 0;
    // ...
    if (_raycastCount < _maxCopRaycastsPerFrame)
    {
        _raycastCount++;
        if (!Physics.Raycast(...)) { ... }
    }
}
```

---

## 🟢 LOW PRIORITY - Easy Wins

### 13. String Key Constants

**Problem:** String literals used repeatedly for Blackboard keys
```csharp
Blackboard.Set("alertLevel", value);
Blackboard.Get("alertLevel", 0f);
```
**Fix:** Use const strings or static readonly:
```csharp
private static class BlackboardKeys
{
    public const string AlertLevel = "alertLevel";
    public const string CanSeeCop = "canSeeCop";
    public const string CurrentState = "currentState";
    // ...
}

Blackboard.Set(BlackboardKeys.AlertLevel, value);
```

---

### 14. List Capacity Pre-allocation

**Problem:** Lists created with default capacity, may resize during use
```csharp
private readonly List<ScoredTarget> _scoredTargets = new List<ScoredTarget>();
```
**Fix:** Pre-size based on expected usage:
```csharp
private readonly List<ScoredTarget> _scoredTargets = new List<ScoredTarget>(16);
private readonly List<SoundEvent> _heardSounds = new List<SoundEvent>(16);
private readonly List<GameObject> _visibleTargets = new List<GameObject>(10);
```

---

### 15. Transform.position Caching

**Problem:** `transform.position` accessed multiple times per method
```csharp
// In UpdateCopDetection():
float distance = Vector3.Distance(transform.position, copNPC.transform.position);
// Later:
Vector3 dirToCop = (copNPC.transform.position - transform.position).normalized;
```
**Fix:** Cache at method start:
```csharp
private void UpdateCopDetection()
{
    Vector3 myPosition = transform.position;
    // Use myPosition throughout
}
```

---

## 📊 Summary & Priority Order

| Priority | Issue | Est. Impact | Effort |
|----------|-------|-------------|--------|
| 🔴 Critical | O(n²) in HearingSensor | High | Medium |
| 🔴 Critical | O(n²) in SightSensor | High | Medium |
| 🔴 Critical | Dictionary foreach allocations | Medium | Medium |
| 🔴 Critical | List swap vs Clear/AddRange | Medium | Low |
| 🟠 High | Pool ScoredTarget/SoundEvent | Medium | Medium |
| 🟠 High | Cache GetComponent results | Medium | Low |
| 🟠 High | Dirty-flag entropy calculation | Low | Low |
| 🟡 Medium | SoundManager cleanup rate-limit | Low | Low |
| 🟡 Medium | Blackboard expiration optimization | Low | Medium |
| 🟡 Medium | Raycast rate limiting in RobberNPC | Low | Low |
| 🟢 Low | String key constants | Minimal | Low |
| 🟢 Low | List capacity pre-allocation | Minimal | Low |
| 🟢 Low | Transform.position caching | Minimal | Low |

---

## 🎯 Recommended Implementation Order

1. **Quick Wins (< 1 hour total):**
   - Add static `Comparison<SoundEvent>` for sort
   - Implement list swapping in sensors
   - Add cleanup rate-limiting to SoundManager
   - Pre-allocate list capacities

2. **Medium Effort (1-2 hours each):**
   - Add HashSets to SightSensor and HearingSensor for O(1) contains
   - Implement dirty-flag caching for CriticalityController
   - Add raycast budgeting to RobberNPC

3. **Larger Refactors (half day each):**
   - Implement object pooling for ScoredTarget and SoundEvent
   - Add type-specific Blackboard methods to avoid boxing
   - Optimize Blackboard TTL tracking with priority queue

---

## 📈 Expected Performance Gains

- **O(n²) fixes:** 10-50% reduction in hot path CPU time with many NPCs/targets
- **Allocation reductions:** Significantly reduced GC pressure, smoother frame times
- **Caching improvements:** 5-15% reduction in per-NPC tick time
- **Overall:** With all fixes, expect 20-40% improvement in scenarios with 20+ NPCs

---

*Generated: January 2026*
*NPCBrain Unity Asset Toolkit*
