# NPCBrain Cookbook

Common patterns, recipes, and solutions for building NPCs with NPCBrain.

---

## Table of Contents

1. [Patrol Patterns](#patrol-patterns)
2. [Combat Behaviors](#combat-behaviors)
3. [Stealth & Detection](#stealth--detection)
4. [State Management](#state-management)
5. [Performance Tips](#performance-tips)
6. [Debugging Recipes](#debugging-recipes)

---

## Patrol Patterns

### Simple Loop Patrol

```csharp
protected override BTNode CreateBehaviorTree()
{
    return new Sequence(
        new MoveTo(() => GetCurrentWaypoint(), 0.5f, 3f, 30f),
        new Wait(2f),
        new AdvanceWaypoint()
    );
}
```

### Patrol with Random Waits

```csharp
protected override BTNode CreateBehaviorTree()
{
    return new Sequence(
        new MoveTo(() => GetCurrentWaypoint(), 0.5f, 3f),
        new Wait(() => Random.Range(1f, 4f)),  // Random 1-4 seconds
        new AdvanceWaypoint()
    );
}
```

### Patrol with Look-Around

```csharp
protected override BTNode CreateBehaviorTree()
{
    return new Sequence(
        new MoveTo(() => GetCurrentWaypoint(), 0.5f, 3f),
        new Wait(1f),
        new LookAt(() => GetRandomLookDirection(), 2f),
        new Wait(1f),
        new LookAt(() => GetRandomLookDirection(), 2f),
        new Wait(0.5f),
        new AdvanceWaypoint()
    );
}

private Vector3 GetRandomLookDirection()
{
    float angle = Random.Range(0f, 360f);
    return transform.position + Quaternion.Euler(0, angle, 0) * Vector3.forward * 5f;
}
```

---

## Combat Behaviors

### Basic Chase

```csharp
private BTNode CreateChaseBehavior()
{
    return new Sequence(
        new CheckBlackboard("target"),
        new MoveTo(
            () => Blackboard.Get<GameObject>("target")?.transform.position ?? transform.position,
            1.5f,  // Attack range
            _chaseSpeed,
            10f    // Timeout
        )
    );
}
```

### Chase with Distance Check

```csharp
private BTNode CreateChaseBehavior()
{
    return new Sequence(
        new CheckBlackboard("target"),
        // Only chase if within max range
        new CheckDistance(
            brain => brain.transform.position,
            brain => brain.Blackboard.Get<GameObject>("target")?.transform.position ?? brain.transform.position,
            _maxChaseDistance,
            CheckDistance.ComparisonType.LessThan
        ),
        new MoveTo(() => GetTargetPosition(), 1.5f, _chaseSpeed)
    );
}
```

### Attack with Cooldown

```csharp
private BTNode CreateAttackBehavior()
{
    return new Sequence(
        new CheckBlackboard("target"),
        new CheckDistance(/* within attack range */),
        new Cooldown(
            new Sequence(
                new LookAt(() => GetTargetPosition(), 10f),
                new Attack(_attackDamage),
                new Wait(0.5f)  // Attack animation
            ),
            _attackCooldown  // Minimum time between attacks
        )
    );
}
```

### Flee When Low Health

```csharp
protected override BTNode CreateBehaviorTree()
{
    return new Selector(
        // High priority: flee when health low
        new Sequence(
            new CheckBlackboard<float>("health", hp => hp < 30f),
            CreateFleeBehavior()
        ),
        // Normal combat
        CreateCombatBehavior()
    );
}

private BTNode CreateFleeBehavior()
{
    return new MoveTo(
        () => GetFleePosition(),
        1f,
        _runSpeed
    );
}

private Vector3 GetFleePosition()
{
    var target = Blackboard.Get<GameObject>("target");
    if (target == null) return transform.position;
    
    // Run away from target
    Vector3 awayDir = (transform.position - target.transform.position).normalized;
    return transform.position + awayDir * 20f;
}
```

---

## Stealth & Detection

### Guard with Investigation

```csharp
protected override void Awake()
{
    base.Awake();
    
    OnTargetAcquired += (target) => {
        Blackboard.Set("target", target);
        Blackboard.Set("alertLevel", 1f);
    };
    
    OnTargetLost += (target) => {
        Blackboard.SetWithTTL("lastKnownPosition", 
            target.transform.position, 15f);
        Blackboard.Remove("target");
    };
    
    OnSoundHeard += (sound) => {
        if (!Blackboard.Has("target"))
        {
            Blackboard.SetWithTTL("investigatePosition", sound.Position, 30f);
            float alert = Blackboard.Get("alertLevel", 0f);
            Blackboard.Set("alertLevel", Mathf.Min(1f, alert + 0.3f));
        }
    };
}

protected override BTNode CreateBehaviorTree()
{
    return new Selector(
        CreateChaseBehavior(),
        CreateInvestigateBehavior(),
        CreatePatrolBehavior()
    );
}

private BTNode CreateInvestigateBehavior()
{
    return new Sequence(
        new CheckBlackboard("investigatePosition"),
        new MoveTo(
            () => Blackboard.Get<Vector3>("investigatePosition"),
            1f, _walkSpeed, 20f
        ),
        new Wait(3f),  // Look around
        new ClearBlackboardKey("investigatePosition")
    );
}
```

### Alert Level Decay

```csharp
private void LateUpdate()
{
    float alert = Blackboard.Get("alertLevel", 0f);
    if (alert > 0 && !Blackboard.Has("target"))
    {
        alert -= _alertDecayRate * Time.deltaTime;
        Blackboard.Set("alertLevel", Mathf.Max(0, alert));
    }
}
```

### Sound-Based Search Pattern

```csharp
private BTNode CreateSoundSearchBehavior()
{
    return new Sequence(
        new CheckBlackboard("heardSoundPosition"),
        new MoveTo(() => Blackboard.Get<Vector3>("heardSoundPosition"), 2f, _walkSpeed),
        // Search pattern: look in multiple directions
        new Repeater(
            new Sequence(
                new LookAt(() => GetSearchDirection(), 3f),
                new Wait(1.5f)
            ),
            4  // Look 4 directions
        ),
        new ClearBlackboardKey("heardSoundPosition")
    );
}

private int _searchIndex = 0;
private Vector3 GetSearchDirection()
{
    float[] angles = { 0, 90, 180, 270 };
    Vector3 dir = Quaternion.Euler(0, angles[_searchIndex % 4], 0) * transform.forward;
    _searchIndex++;
    return transform.position + dir * 5f;
}
```

---

## State Management

### Explicit State Machine

```csharp
public enum NPCState { Idle, Patrol, Chase, Investigate, Combat }

private NPCState _currentState = NPCState.Idle;

protected override BTNode CreateBehaviorTree()
{
    return new Selector(
        CreateStateGate(NPCState.Combat, CreateCombatBehavior()),
        CreateStateGate(NPCState.Chase, CreateChaseBehavior()),
        CreateStateGate(NPCState.Investigate, CreateInvestigateBehavior()),
        CreateStateGate(NPCState.Patrol, CreatePatrolBehavior()),
        CreateIdleBehavior()
    );
}

private BTNode CreateStateGate(NPCState state, BTNode behavior)
{
    return new Sequence(
        new CheckBlackboard<NPCState>("state", s => s == state),
        behavior
    );
}

public void SetState(NPCState state)
{
    _currentState = state;
    Blackboard.Set("state", state);
    OnStateChanged?.Invoke(state.ToString());
}
```

### State Transitions via Events

```csharp
protected override void Awake()
{
    base.Awake();
    
    Blackboard.Set("state", NPCState.Patrol);
    
    OnTargetAcquired += (t) => SetState(NPCState.Chase);
    OnTargetLost += (t) => SetState(NPCState.Investigate);
    OnSoundHeard += (s) => {
        if (_currentState == NPCState.Patrol)
            SetState(NPCState.Investigate);
    };
}
```

---

## Performance Tips

### Tick Interval for Many NPCs

```csharp
// In Inspector or via code
[SerializeField] private float _tickInterval = 0.1f;  // 10 ticks/second

// For staggered updates (avoid all NPCs ticking same frame)
protected override void Awake()
{
    base.Awake();
    _tickInterval = 0.1f + Random.Range(0f, 0.05f);
}
```

### Cache Component References

```csharp
private Transform _cachedTarget;
private Vector3 _cachedTargetPosition;
private float _lastTargetUpdateTime;

private Vector3 GetTargetPosition()
{
    // Only update every 0.1 seconds
    if (Time.time - _lastTargetUpdateTime > 0.1f)
    {
        var target = Blackboard.Get<GameObject>("target");
        if (target != null)
        {
            _cachedTarget = target.transform;
            _cachedTargetPosition = _cachedTarget.position;
        }
        _lastTargetUpdateTime = Time.time;
    }
    return _cachedTargetPosition;
}
```

### Use NPCRegistry Instead of FindObjectsOfType

```csharp
// Bad (expensive)
var allEnemies = FindObjectsOfType<EnemyNPC>();

// Good (O(1) lookup)
var allEnemies = NPCRegistry<EnemyNPC>.All;
var nearbyEnemies = NPCRegistry<EnemyNPC>.GetInRange(transform.position, 20f);
```

### Limit Perception Raycasts

```csharp
// In SightSensor Inspector
// Max Raycasts Per Tick: 3 (default)
// For many NPCs, reduce to 1-2
```

---

## Debugging Recipes

### Log State Changes

```csharp
protected override void Awake()
{
    base.Awake();
    
    OnStateChanged += (state) => {
        Debug.Log($"[{name}] State: {state}");
    };
    
    OnTargetAcquired += (t) => Debug.Log($"[{name}] Acquired: {t.name}");
    OnTargetLost += (t) => Debug.Log($"[{name}] Lost: {t.name}");
}
```

### Visualize Blackboard

```csharp
void OnGUI()
{
    if (!Application.isPlaying) return;
    
    GUILayout.BeginArea(new Rect(10, 10, 300, 400));
    GUILayout.Label($"=== {name} ===");
    
    foreach (var key in Blackboard.Keys)
    {
        var value = Blackboard.Get<object>(key, null);
        GUILayout.Label($"{key}: {value}");
    }
    
    GUILayout.Label($"--- Criticality ---");
    GUILayout.Label($"Temp: {Criticality.Temperature:F2}");
    GUILayout.Label($"Entropy: {Criticality.Entropy:F2}");
    
    GUILayout.EndArea();
}
```

### Force Specific Behavior

```csharp
// For testing specific behaviors
[ContextMenu("Force Chase Mode")]
void DebugForceChase()
{
    // Create fake target
    var fakeTarget = new GameObject("DebugTarget");
    fakeTarget.transform.position = transform.position + transform.forward * 10f;
    Blackboard.Set("target", fakeTarget);
}

[ContextMenu("Clear All State")]
void DebugClearState()
{
    Blackboard.Clear();
    Criticality.Reset();
}
```

### Pause and Step

```csharp
// Debug controls
void Update()
{
    if (Input.GetKeyDown(KeyCode.P))
    {
        if (IsPaused) Resume();
        else Pause();
    }
    
    if (Input.GetKeyDown(KeyCode.Space) && IsPaused)
    {
        Tick();  // Step one frame
    }
}
```

---

## Quick Reference

### Common Blackboard Keys

| Key | Type | Usage |
|-----|------|-------|
| `target` | `GameObject` | Current chase/attack target |
| `lastKnownPosition` | `Vector3` | Where target was last seen |
| `investigatePosition` | `Vector3` | Sound/alert to investigate |
| `homePosition` | `Vector3` | Starting position |
| `alertLevel` | `float` | 0-1 alert state |
| `health` | `float` | Current health |
| `energy` | `float` | Current energy/stamina |
| `state` | `string/enum` | Current behavior state |

### Typical Utility Weights

| Action | Weight | Notes |
|--------|--------|-------|
| Critical (flee, heal) | 1.0+ | Highest priority |
| Combat (attack, chase) | 0.8-1.0 | High priority |
| Alert (investigate) | 0.5-0.8 | Medium priority |
| Normal (patrol, work) | 0.3-0.6 | Low priority |
| Idle (rest, wander) | 0.1-0.3 | Fallback |

---

[← Back to Index](index.md)
