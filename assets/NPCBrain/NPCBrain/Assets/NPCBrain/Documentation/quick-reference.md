# NPCBrain Quick Reference

A cheat sheet for common NPCBrain operations.

---

## Namespaces

```csharp
using NPCBrain;
using NPCBrain.BehaviorTree;
using NPCBrain.BehaviorTree.Actions;
using NPCBrain.BehaviorTree.Composites;
using NPCBrain.BehaviorTree.Conditions;
using NPCBrain.BehaviorTree.Decorators;
using NPCBrain.UtilityAI;
using NPCBrain.Perception;
using NPCBrain.Criticality;
using NPCBrain.Archetypes;
```

---

## Basic NPC Template

```csharp
public class MyNPC : NPCBrainController
{
    [SerializeField] private float _speed = 3f;
    
    protected override void Awake()
    {
        base.Awake();
        OnTargetAcquired += (t) => Blackboard.Set("target", t);
        OnTargetLost += (t) => Blackboard.Remove("target");
    }
    
    protected override BTNode CreateBehaviorTree()
    {
        return new Selector(
            CreateChaseBehavior(),
            CreatePatrolBehavior()
        );
    }
}
```

---

## Behavior Tree Nodes

### Composites

| Node | Behavior | Returns Success | Returns Failure |
|------|----------|-----------------|-----------------|
| `Selector` | Try until success | Any child succeeds | All children fail |
| `Sequence` | All must succeed | All children succeed | Any child fails |
| `Parallel` | Run all at once | Based on policy | Based on policy |
| `UtilitySelector` | Score-based | Selected completes | No valid actions |

### Decorators

| Node | Effect |
|------|--------|
| `Inverter(child)` | Flip Success↔Failure |
| `Repeater(child, n)` | Repeat n times (-1 = forever) |
| `Cooldown(child, time)` | Rate limit execution |
| `Succeeder(child)` | Always return Success |

### Actions

| Node | Parameters |
|------|------------|
| `MoveTo(target, arrival, speed, timeout)` | Func<Vector3>, float, float, float |
| `Wait(duration)` | float seconds |
| `AdvanceWaypoint()` | - |
| `LookAt(target, speed, threshold)` | Func<Vector3>, float, float |
| `SetBlackboard(key, value)` | string, Func<T> |
| `ClearBlackboardKey(key)` | string |
| `Log(message)` | string |

### Conditions

| Node | Parameters |
|------|------------|
| `CheckBlackboard(key)` | string |
| `CheckBlackboard<T>(key, predicate)` | string, Func<T, bool> |
| `CheckDistance(from, to, dist, comparison)` | Func, Func, float, ComparisonType |

---

## Blackboard

```csharp
// Set
Blackboard.Set("key", value);
Blackboard.SetWithTTL("key", value, 10f);  // Expires in 10s

// Get
T val = Blackboard.Get<T>("key", defaultValue);
if (Blackboard.TryGet<T>("key", out var val)) { }

// Check/Remove
if (Blackboard.Has("key")) { }
Blackboard.Remove("key");
Blackboard.Clear();
```

---

## Utility AI

```csharp
return new UtilitySelector(
    new UtilityAction("Name", behavior, weight, ...considerations),
    new UtilityAction("Name2", behavior2, weight2)
);
```

### Considerations

| Type | Constructor |
|------|-------------|
| `ConstantConsideration` | `(float value)` |
| `BlackboardConsideration<T>` | `(name, key, Func<T,float>, default)` |
| `DistanceConsideration` | `(name, Func<Vector3>, maxDist, invert)` |
| `TimeConsideration` | `(name, key, duration)` |
| `RangeConsideration` | `(name, Func<float>, min, max, score)` |
| `SoundConsideration` | `(name, SoundType, score)` |

---

## Perception

### SightSensor

```csharp
// Properties
Perception.HasVisibleTargets  // bool
Perception.ClosestTarget      // GameObject
Perception.VisibleTargets     // IReadOnlyList<GameObject>

// Methods
Perception.IsInViewCone(position)    // bool
Perception.HasLineOfSight(position)  // bool
Perception.SetTargetTag("Enemy")     // Change target tag
```

### HearingSensor

```csharp
// Properties
Hearing.GetRecentSounds()           // List<SoundEvent>
Hearing.HasRecentSound(SoundType)   // bool

// Emit sounds
SoundManager.EmitSound(position, SoundType.Gunshot, 1f, source);
```

---

## Events

```csharp
OnTargetAcquired += (GameObject target) => { };
OnTargetLost += (GameObject target) => { };
OnSoundHeard += (SoundEvent sound) => { };
OnStateChanged += (string state) => { };
OnBrainPaused += () => { };
OnBrainResumed += () => { };
```

---

## Criticality

```csharp
// Read values
Criticality.Temperature  // float (0.5-2.0)
Criticality.Entropy      // float (0-1)
Criticality.Inertia      // float (0-1)

// Manual control
Criticality.SetTemperature(1.5f);
Criticality.Reset();

// Custom config
Criticality = new CriticalityController(
    historySize: 20,
    minTemperature: 0.5f,
    maxTemperature: 2.0f,
    temperatureAdjustRate: 0.1f,
    targetEntropy: 0.5f
);
```

---

## Built-in Archetypes

| Archetype | Use Case |
|-----------|----------|
| `PatrolNPC` | Simple waypoint patrol |
| `GuardNPC` | Sight-based chase & investigate |
| `HearingGuardNPC` | Responds to sounds |
| `UtilityNPC` | General utility AI demo |
| `CopNPC` | Chase, arrest, investigate |
| `RobberNPC` | Steal, evade, escape |

---

## NPCRegistry

```csharp
// Register in Awake
NPCRegistry<MyNPC>.Register(this);

// Unregister in OnDestroy
NPCRegistry<MyNPC>.Unregister(this);

// Query
var all = NPCRegistry<MyNPC>.All;
var nearest = NPCRegistry<MyNPC>.GetNearest(position);
var inRange = NPCRegistry<MyNPC>.GetInRange(position, 20f);
```

---

## Debug

```csharp
// Enable logging
NPCBrainDebug.Enabled = true;
NPCBrainDebug.SetCategoryEnabled(NPCBrainDebug.Category.Perception, true);

// Log
NPCBrainDebug.Log(NPCBrainDebug.Category.BehaviorTree, "message", this);

// Pause/Resume
brain.Pause();
brain.Resume();
brain.Tick();  // Manual step
```

---

## Component Setup Order

```
1. SightSensor (optional)
2. HearingSensor (optional)
3. YourNPCController (extends NPCBrainController)
4. WaypointPath (optional)
5. NavMeshAgent (optional, for pathfinding)
```

---

[← Back to Index](index.md)
