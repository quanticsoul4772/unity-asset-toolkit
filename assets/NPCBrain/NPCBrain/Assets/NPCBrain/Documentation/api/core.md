# Core API Reference

Complete API documentation for NPCBrain core classes.

---

## NPCBrainController

The main MonoBehaviour that drives NPC AI behavior.

### Namespace
```csharp
namespace NPCBrain
```

### Inheritance
```
MonoBehaviour → NPCBrainController
```

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Blackboard` | `Blackboard` | Key-value data store for behavior tree nodes |
| `Perception` | `SightSensor` | Attached sight sensor (null if not present) |
| `Hearing` | `HearingSensor` | Attached hearing sensor (null if not present) |
| `Criticality` | `CriticalityController` | Exploration/exploitation controller |
| `WaypointPath` | `WaypointPath` | Assigned waypoint path for patrol |
| `BehaviorTree` | `BTNode` | Root node of the behavior tree |
| `LastStatus` | `NodeStatus` | Result of last behavior tree tick |
| `IsPaused` | `bool` | Whether the brain is paused |

### Events

| Event | Signature | Description |
|-------|-----------|-------------|
| `OnTargetAcquired` | `Action<GameObject>` | Fired when perception detects a new target |
| `OnTargetLost` | `Action<GameObject>` | Fired when a target leaves perception |
| `OnStateChanged` | `Action<string>` | Fired when NPC state changes |
| `OnBrainPaused` | `Action` | Fired when `Pause()` is called |
| `OnBrainResumed` | `Action` | Fired when `Resume()` is called |
| `OnSoundHeard` | `Action<SoundEvent>` | Fired when hearing sensor detects a sound |

### Methods

#### CreateBehaviorTree
```csharp
protected virtual BTNode CreateBehaviorTree()
```
Override to define the NPC's behavior tree. Returns the root node.

#### Tick
```csharp
public void Tick()
```
Manually triggers one tick of the brain. Called automatically by `Update()`.

#### Pause / Resume
```csharp
public void Pause()
public void Resume()
```
Pauses or resumes behavior tree execution.

#### SetBehaviorTree
```csharp
public void SetBehaviorTree(BTNode tree)
```
Replaces the current behavior tree at runtime.

#### GetCurrentWaypoint
```csharp
public Vector3 GetCurrentWaypoint()
```
Returns the current waypoint position without advancing.

#### AdvanceAndGetWaypoint
```csharp
public Vector3 AdvanceAndGetWaypoint()
```
Advances to the next waypoint and returns its position.

#### SetWaypointPath
```csharp
public void SetWaypointPath(WaypointPath path)
```
Assigns a waypoint path for patrol behaviors.

### Example
```csharp
public class MyNPC : NPCBrainController
{
    protected override void Awake()
    {
        base.Awake();
        OnTargetAcquired += (t) => Blackboard.Set("target", t);
    }
    
    protected override BTNode CreateBehaviorTree()
    {
        return new Sequence(
            new MoveTo(() => GetCurrentWaypoint(), 0.5f, 3f),
            new Wait(2f),
            new AdvanceWaypoint()
        );
    }
}
```

---

## Blackboard

Key-value data store for sharing information between behavior tree nodes.

### Namespace
```csharp
namespace NPCBrain
```

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Keys` | `IEnumerable<string>` | All non-expired keys |
| `LogTypeMismatches` | `bool` | Enable warnings for type mismatches |

### Events

| Event | Signature | Description |
|-------|-----------|-------------|
| `OnValueChanged` | `Action<string, object>` | Fired when any value is set |
| `OnValueExpired` | `Action<string>` | Fired when a TTL value expires |

### Methods

#### Set<T>
```csharp
public void Set<T>(string key, T value)
```
Stores a value that persists until explicitly removed.

#### SetWithTTL<T>
```csharp
public void SetWithTTL<T>(string key, T value, float ttlSeconds)
```
Stores a value that expires after the specified time.

#### Get<T>
```csharp
public T Get<T>(string key, T defaultValue = default)
```
Retrieves a value, returning `defaultValue` if not found or wrong type.

#### TryGet<T>
```csharp
public bool TryGet<T>(string key, out T value)
```
Attempts to retrieve a value. Returns false if not found or wrong type.

#### Has
```csharp
public bool Has(string key)
```
Checks if a key exists and hasn't expired.

#### Remove
```csharp
public bool Remove(string key)
```
Removes a key. Returns true if found and removed.

#### Clear
```csharp
public void Clear()
```
Removes all entries.

#### CleanupExpired
```csharp
public void CleanupExpired()
```
Removes expired TTL entries. Called automatically each tick.

### Example
```csharp
// Store values
Blackboard.Set("health", 100);
Blackboard.Set("target", enemyGameObject);

// Store with expiration
Blackboard.SetWithTTL("lastKnownPosition", position, 10f);

// Retrieve
int health = Blackboard.Get("health", 0);
if (Blackboard.TryGet<GameObject>("target", out var target))
{
    // Use target
}

// Check and remove
if (Blackboard.Has("danger"))
{
    Blackboard.Remove("danger");
}
```

---

## WaypointPath

Manages a sequence of waypoints for patrol behaviors.

### Namespace
```csharp
namespace NPCBrain
```

### Inspector Properties

| Property | Type | Description |
|----------|------|-------------|
| `Loop` | `bool` | Whether to loop back to start |
| `Reverse On End` | `bool` | Reverse direction at ends (ping-pong) |
| `Auto Find Children` | `bool` | Automatically use child transforms as waypoints |

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `CurrentIndex` | `int` | Index of current waypoint |
| `Count` | `int` | Total number of waypoints |
| `Waypoints` | `IReadOnlyList<Transform>` | All waypoint transforms |

### Methods

#### GetCurrent
```csharp
public Vector3 GetCurrent()
```
Returns the current waypoint position.

#### AdvanceAndGetWaypoint
```csharp
public Vector3 AdvanceAndGetWaypoint()
```
Advances to next waypoint and returns its position.

#### SetWaypoints
```csharp
public void SetWaypoints(Transform[] waypoints)
```
Replaces the waypoint list.

#### Reset
```csharp
public void Reset()
```
Resets to the first waypoint.

### Example
```csharp
// Setup in code
var path = gameObject.AddComponent<WaypointPath>();
path.SetWaypoints(new Transform[] { wp1, wp2, wp3, wp4 });

// Use in behavior tree
new MoveTo(() => brain.WaypointPath.GetCurrent(), 0.5f, 3f)
```

---

## NPCBrainDebug

Centralized debug logging system with per-category control.

### Namespace
```csharp
namespace NPCBrain
```

### Static Properties

| Property | Type | Description |
|----------|------|-------------|
| `Enabled` | `bool` | Master enable for all debug logging |
| `AlwaysLogWarnings` | `bool` | Log warnings even when disabled |
| `AlwaysLogErrors` | `bool` | Log errors even when disabled |

### Categories

```csharp
public enum Category
{
    General,
    BehaviorTree,
    Perception,
    Blackboard,
    Utility,
    Criticality
}
```

### Static Methods

#### Log
```csharp
public static void Log(Category category, string message, Object context = null)
```
Logs a message if the category is enabled.

#### LogWarning
```csharp
public static void LogWarning(Category category, string message, Object context = null)
```
Logs a warning (respects `AlwaysLogWarnings`).

#### LogError
```csharp
public static void LogError(Category category, string message, Object context = null)
```
Logs an error (respects `AlwaysLogErrors`).

#### IsEnabled
```csharp
public static bool IsEnabled(Category category)
```
Checks if a category is enabled for logging.

#### SetCategoryEnabled
```csharp
public static void SetCategoryEnabled(Category category, bool enabled)
```
Enables or disables a logging category.

### Example
```csharp
// Enable debug logging
NPCBrainDebug.Enabled = true;
NPCBrainDebug.SetCategoryEnabled(NPCBrainDebug.Category.Perception, true);

// Log messages
NPCBrainDebug.Log(NPCBrainDebug.Category.Perception, 
    $"Target detected: {target.name}", this);
```

---

## NPCRegistry<T>

Static registry for tracking active NPC instances. Replaces expensive `FindObjectsOfType` calls.

### Namespace
```csharp
namespace NPCBrain
```

### Static Properties

| Property | Type | Description |
|----------|------|-------------|
| `All` | `IReadOnlyList<T>` | All registered instances |
| `Count` | `int` | Number of registered instances |

### Static Methods

#### Register
```csharp
public static void Register(T instance)
```
Adds an instance to the registry. Call in `Awake()`.

#### Unregister
```csharp
public static void Unregister(T instance)
```
Removes an instance. Call in `OnDestroy()`.

#### GetNearest
```csharp
public static T GetNearest(Vector3 position)
```
Returns the nearest registered instance to a position.

#### GetInRange
```csharp
public static List<T> GetInRange(Vector3 position, float range)
```
Returns all instances within range.

### Example
```csharp
public class CopNPC : NPCBrainController
{
    protected override void Awake()
    {
        base.Awake();
        NPCRegistry<CopNPC>.Register(this);
    }
    
    protected override void OnDestroy()
    {
        NPCRegistry<CopNPC>.Unregister(this);
        base.OnDestroy();
    }
}

// Usage elsewhere
var nearestCop = NPCRegistry<CopNPC>.GetNearest(transform.position);
var copsInRange = NPCRegistry<CopNPC>.GetInRange(transform.position, 20f);
```

---

[← Back to Index](../index.md) | [Behavior Tree API →](behavior-tree.md)
