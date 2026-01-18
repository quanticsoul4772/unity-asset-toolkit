# NPCBrain - AI Toolkit for Unity

**Version 1.0** | Unity 2022.3 LTS+ | Full Source Code Included

NPCBrain is a modular AI toolkit that combines **Behavior Trees**, **Utility AI**, and **Perception Systems** to create intelligent, believable NPCs for your Unity games.

---

## Table of Contents

1. [Features](#features)
2. [Quick Start](#quick-start)
3. [Core Concepts](#core-concepts)
4. [Built-in Nodes](#built-in-nodes)
5. [Archetypes](#archetypes)
6. [Perception System](#perception-system)
7. [Utility AI](#utility-ai)
8. [Criticality System](#criticality-system)
9. [Debug Tools](#debug-tools)
10. [API Reference](#api-reference)
11. [Best Practices](#best-practices)
12. [Demo Scenes](#demo-scenes)
13. [Support](#support)

---

## Features

- **Behavior Trees** - Composable, hierarchical decision making
- **Utility AI** - Score-based action selection with softmax
- **Blackboard System** - Key-value data sharing with TTL support
- **Perception** - Vision cone with line-of-sight checking
- **Criticality Controller** - Adaptive exploration vs exploitation
- **Ready-to-Use Archetypes** - GuardNPC and PatrolNPC
- **Debug Window** - Real-time NPC state inspection
- **Scene Gizmos** - Vision cones and waypoint visualization
- **Full Source Code** - Extend and customize everything
- **100+ Unit Tests** - Production-quality code

---

## Quick Start

### 1. Create Your First NPC

```csharp
using NPCBrain;
using NPCBrain.BehaviorTree;
using NPCBrain.BehaviorTree.Composites;
using NPCBrain.BehaviorTree.Actions;

public class SimplePatroller : NPCBrainController
{
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

### 2. Setup in Unity

1. Create a GameObject with a Capsule mesh
2. Add your NPC component (e.g., `SimplePatroller`)
3. Add a `NavMeshAgent` component (optional, for pathfinding)
4. Create waypoints as empty GameObjects
5. Add a `WaypointPath` component and assign waypoints
6. Press Play

### 3. Use Built-in Archetypes

Use the included archetypes for quick setup:

```csharp
// Just add PatrolNPC component - no code needed!
// Or extend it:
public class MyPatroller : PatrolNPC { }

// GuardNPC includes chase, investigate, and patrol behaviors
public class MyGuard : GuardNPC { }
```

---

## Core Concepts

### NPCBrainController

The main component that drives NPC behavior. Attach this (or a subclass) to any NPC.

```csharp
public class NPCBrainController : MonoBehaviour
{
    // Core systems
    public Blackboard Blackboard { get; }      // Shared data storage
    public SightSensor Perception { get; }     // Vision sensor (if attached)
    public CriticalityController Criticality { get; }  // Exploration control
    
    // Events
    public event Action<GameObject> OnTargetAcquired;
    public event Action<GameObject> OnTargetLost;
    public event Action<string> OnStateChanged;
    
    // Override to define behavior
    protected virtual BTNode CreateBehaviorTree() { return null; }
}
```

### Behavior Trees

Behavior trees are built from composable nodes that execute in a hierarchical structure.

**Node Types:**
- **Composites** - Control flow (Selector, Sequence)
- **Decorators** - Modify child behavior (Inverter, Repeater, Cooldown)
- **Actions** - Do things (MoveTo, Wait)
- **Conditions** - Check state (CheckBlackboard)

**Node Status:**
- `Success` - Node completed successfully
- `Failure` - Node failed
- `Running` - Node still executing

```csharp
// Example: Priority-based behavior
return new Selector(      // Try children in order until one succeeds
    new Sequence(         // All must succeed
        new CheckBlackboard("target"),
        new MoveTo(() => GetTargetPosition())
    ),
    new Sequence(         // Fallback: patrol
        new MoveTo(() => GetCurrentWaypoint()),
        new Wait(2f),
        new AdvanceWaypoint()
    )
);
```

### Blackboard

A key-value store for sharing data between nodes and systems.

```csharp
// Store values
Blackboard.Set("health", 100);
Blackboard.Set("target", enemyGameObject);

// Store with expiration (5 seconds)
Blackboard.SetWithTTL("lastKnownPosition", position, 5f);

// Retrieve values
int health = Blackboard.Get("health", 0);  // 0 is default if not found

// Check existence
if (Blackboard.Has("target"))
{
    var target = Blackboard.Get<GameObject>("target");
}

// Safe retrieval
if (Blackboard.TryGet<Vector3>("position", out var pos))
{
    // Use pos
}

// Remove
Blackboard.Remove("target");
```

---

## Built-in Nodes

### Actions

| Node | Description | Parameters |
|------|-------------|------------|
| `MoveTo` | Moves to a target position | `targetGetter`, `arrivalDistance`, `speed`, `timeout` |
| `Wait` | Waits for a duration | `duration` |
| `AdvanceWaypoint` | Advances to next waypoint | - |
| `ClearBlackboardKey` | Removes a blackboard key | `key` |

```csharp
// MoveTo - move to position
new MoveTo(
    () => targetPosition,  // Target getter function
    0.5f,                  // Arrival distance
    5f,                    // Movement speed
    30f                    // Timeout in seconds
)

// Wait - pause execution
new Wait(2.5f)  // Wait 2.5 seconds

// AdvanceWaypoint - move to next waypoint in path
new AdvanceWaypoint()

// ClearBlackboardKey - remove a value
new ClearBlackboardKey("target")
```

### Composites

| Node | Description | Behavior |
|------|-------------|----------|
| `Selector` | OR logic | Succeeds if ANY child succeeds |
| `Sequence` | AND logic | Succeeds if ALL children succeed |
| `UtilitySelector` | Score-based | Selects based on utility scores |

```csharp
// Selector - tries children in order until one succeeds
new Selector(
    highPriorityBehavior,
    mediumPriorityBehavior,
    lowPriorityBehavior
)

// Sequence - all children must succeed
new Sequence(
    checkCondition,
    doAction,
    cleanup
)
```

### Conditions

| Node | Description | Parameters |
|------|-------------|------------|
| `CheckBlackboard` | Checks if key exists | `key` |
| `CheckBlackboard<T>` | Checks key with predicate | `key`, `predicate` |
| `CheckDistance` | Compares distances | `fromGetter`, `toGetter`, `distance`, `comparison` |

```csharp
// Check if key exists
new CheckBlackboard("target")

// Check with condition
new CheckBlackboard<float>("health", hp => hp > 50)

// Check distance
new CheckDistance(
    brain => brain.transform.position,
    brain => targetPosition,
    10f,
    CheckDistance.ComparisonType.LessThanOrEqual
)
```

### Decorators

| Node | Description | Parameters |
|------|-------------|------------|
| `Inverter` | Inverts result | `child` |
| `Repeater` | Repeats child | `child`, `repeatCount` |
| `Cooldown` | Rate limits execution | `child`, `cooldownTime` |

```csharp
// Inverter - flip Success/Failure
new Inverter(new CheckBlackboard("isDead"))  // True if NOT dead

// Repeater - repeat N times
new Repeater(new Wait(1f), 3)  // Wait 1 second, 3 times

// Cooldown - minimum time between executions
new Cooldown(attackAction, 2f)  // Can only attack every 2 seconds
```

---

## Archetypes

NPCBrain includes ready-to-use NPC archetypes.

### PatrolNPC

Simple waypoint-following NPC.

**Inspector Settings:**
- `Patrol Speed` - Movement speed (default: 3)
- `Arrival Distance` - How close to waypoint before "arrived" (default: 0.5)
- `Waypoint Wait Time` - Pause at each waypoint (default: 2s)
- `Wait Time Variation` - Random variation in wait time (default: 0.5s)
- `Speed Variation` - Random variation in speed (default: 0)

**Setup:**
1. Add `PatrolNPC` component to GameObject
2. Add `WaypointPath` component
3. Create child GameObjects as waypoints OR assign external transforms
4. Play!

### GuardNPC

Advanced guard with chase, investigate, and patrol behaviors.

**Behavior Priority:**
1. **Chase** - Pursues visible targets
2. **Investigate** - Goes to last known position when target lost
3. **Return** - Returns to patrol area if far from home
4. **Patrol** - Normal waypoint patrol when idle

**Inspector Settings:**
- `Chase Speed` - Speed when chasing (default: 6)
- `Patrol Speed` - Speed when patrolling (default: 3)
- `Investigate Speed` - Speed when investigating (default: 4)
- `Max Chase Distance` - Give up chase beyond this (default: 20)
- `Investigate Time` - Time spent looking around (default: 3s)
- `Alert Decay Rate` - How fast alert level drops (default: 0.1)

**Blackboard Keys Used:**
- `target` - Current chase target (GameObject)
- `lastKnownPosition` - Where target was last seen (Vector3)
- `homePosition` - Starting position (Vector3)
- `alertLevel` - Current alert (float, 0-1)

**Setup:**
1. Add `SightSensor` component first
2. Add `GuardNPC` component
3. Add `WaypointPath` with patrol route
4. Make sure targets have the "Player" tag
5. Play

---

## Perception System

### SightSensor

Vision cone detection with line-of-sight checking.

**Inspector Settings:**
- `View Distance` - How far the NPC can see (default: 20)
- `View Angle` - Field of view in degrees (default: 120°)
- `Eye Height` - Height offset for raycasts (default: 1.5)
- `Target Tag` - Tag to filter targets (default: "Player")
- `Obstacle Mask` - Layers that block sight
- `Target Mask` - Layers containing targets

**Properties:**
```csharp
// Check if any targets visible
if (Perception.HasVisibleTargets)
{
    // Get closest visible target
    GameObject closest = Perception.ClosestTarget;
    
    // Get all visible targets
    foreach (var target in Perception.VisibleTargets)
    {
        // ...
    }
}

// Manual checks
bool inCone = Perception.IsInViewCone(position);
bool hasLOS = Perception.HasLineOfSight(position);
```

**Events (via NPCBrainController):**
```csharp
OnTargetAcquired += (target) => {
    Debug.Log($"Spotted: {target.name}");
};

OnTargetLost += (target) => {
    Debug.Log($"Lost sight of: {target.name}");
};
```

**Important:** Add `SightSensor` **before** `NPCBrainController` components so it's detected in Awake().

---

## Utility AI

Utility AI selects actions based on scored "utility" values rather than priority order.

### UtilityAction

Wraps a behavior with scoring considerations.

```csharp
var patrolAction = new UtilityAction(
    "Patrol",
    new Sequence(new MoveTo(...), new Wait(2f)),
    new ConstantConsideration(0.6f)  // Base score of 0.6
);

var idleAction = new UtilityAction(
    "Idle",
    new Wait(2f),
    new ConstantConsideration(0.2f)  // Lower priority
);
```

### UtilitySelector

Selects actions using softmax probability.

```csharp
return new UtilitySelector(
    patrolAction,
    idleAction,
    attackAction
);
```

**How Selection Works:**
1. Each action's score is calculated from its considerations
2. Scores are converted to probabilities using softmax
3. Temperature controls randomness:
   - **Low temp (0.5)**: Almost always picks highest score
   - **High temp (2.0)**: More random selection
4. Actions with score ≤ 0 are excluded

### Considerations

Considerations calculate utility scores:

```csharp
// Constant value
new ConstantConsideration(0.5f)

// Custom consideration
public class HealthConsideration : IConsideration
{
    public float Score(NPCBrainController brain)
    {
        float health = brain.Blackboard.Get("health", 100f);
        return health / 100f;  // 0-1 based on health
    }
}

// Response curves modify scores
new LinearCurve(consideration, 0f, 1f)      // Linear mapping
new ExponentialCurve(consideration, 2f)     // Exponential falloff
new StepCurve(consideration, 0.5f)          // Binary threshold
```

---

## Criticality System

The `CriticalityController` automatically adjusts exploration vs exploitation based on action history.

**How It Works:**
- Tracks recent action selections
- Calculates Shannon entropy of action distribution
- Adjusts temperature based on entropy:
  - **Low entropy** (repetitive) → Increase temperature → More exploration
  - **High entropy** (varied) → Decrease temperature → More exploitation

**Properties:**
```csharp
float temp = Criticality.Temperature;   // Current softmax temperature
float entropy = Criticality.Entropy;    // Action distribution entropy
float inertia = Criticality.Inertia;    // Tendency to repeat (1 - normalized entropy)
```

**Configuration:**
```csharp
// Custom configuration in your NPC
protected override void Awake()
{
    base.Awake();
    Criticality = new CriticalityController(
        historySize: 20,           // Actions to track
        minTemperature: 0.5f,      // Most deterministic
        maxTemperature: 2.0f,      // Most random
        temperatureAdjustRate: 0.1f,
        targetEntropy: 0.5f        // Balanced exploration
    );
}
```

---

## Debug Tools

### NPCBrain Debug Window

Access via **Window → NPCBrain → Debug Window**

**Features:**
- NPC selector dropdown
- Current behavior tree state
- Blackboard key viewer with values
- Criticality stats (Temperature, Entropy, Inertia)
- Pause / Step / Resume controls

### Scene Gizmos

**Vision Cones:**
- Green = Clear (no targets)
- Red = Alert (target visible)
- Lines drawn to visible targets

**Waypoint Paths:**
- Lines between waypoints
- Current waypoint highlighted

Gizmos are drawn when the NPC is selected or during Play mode.

### Pause & Step

```csharp
// Pause NPC brain
myNPC.Pause();

// Step one tick manually
myNPC.Tick();

// Resume normal execution
myNPC.Resume();
```

---

## API Reference

### Namespaces

| Namespace | Contents |
|-----------|----------|
| `NPCBrain` | Core classes (NPCBrainController, Blackboard, WaypointPath) |
| `NPCBrain.BehaviorTree` | BTNode, NodeStatus |
| `NPCBrain.BehaviorTree.Actions` | MoveTo, Wait, AdvanceWaypoint, ClearBlackboardKey |
| `NPCBrain.BehaviorTree.Composites` | Selector, Sequence, UtilitySelector |
| `NPCBrain.BehaviorTree.Conditions` | CheckBlackboard, CheckDistance |
| `NPCBrain.BehaviorTree.Decorators` | Inverter, Repeater, Cooldown |
| `NPCBrain.Perception` | SightSensor |
| `NPCBrain.UtilityAI` | UtilityAction, IConsideration, curves |
| `NPCBrain.Criticality` | CriticalityController |
| `NPCBrain.Archetypes` | GuardNPC, PatrolNPC |

### Key Classes

#### NPCBrainController

```csharp
public class NPCBrainController : MonoBehaviour
{
    // Properties
    Blackboard Blackboard { get; }
    SightSensor Perception { get; }
    CriticalityController Criticality { get; }
    WaypointPath WaypointPath { get; }
    BTNode BehaviorTree { get; }
    NodeStatus LastStatus { get; }
    bool IsPaused { get; }
    
    // Events
    event Action<GameObject> OnTargetAcquired;
    event Action<GameObject> OnTargetLost;
    event Action<string> OnStateChanged;
    event Action OnBrainPaused;
    event Action OnBrainResumed;
    
    // Methods
    void Tick();                              // Manual tick
    void Pause();                             // Pause execution
    void Resume();                            // Resume execution
    void SetBehaviorTree(BTNode tree);        // Replace behavior tree
    void SetWaypointPath(WaypointPath path);  // Set waypoint path
    Vector3 GetCurrentWaypoint();             // Get current waypoint position
    Vector3 AdvanceAndGetWaypoint();          // Advance and get next waypoint
}
```

#### BTNode

```csharp
public abstract class BTNode
{
    // Properties
    string Name { get; set; }
    NodeStatus LastStatus { get; }
    bool IsRunning { get; }
    
    // Methods
    NodeStatus Execute(NPCBrainController brain);  // Run one tick
    void Reset();                                   // Reset state
    void Abort(NPCBrainController brain);          // Force stop
    
    // Override in subclasses
    protected abstract NodeStatus Tick(NPCBrainController brain);
    protected virtual void OnEnter(NPCBrainController brain) { }
    protected virtual void OnExit(NPCBrainController brain) { }
}
```

#### Blackboard

```csharp
public class Blackboard
{
    // Events
    event Action<string, object> OnValueChanged;
    event Action<string> OnValueExpired;
    
    // Methods
    void Set<T>(string key, T value);
    void SetWithTTL<T>(string key, T value, float ttlSeconds);
    T Get<T>(string key, T defaultValue = default);
    bool TryGet<T>(string key, out T value);
    bool Has(string key);
    bool Remove(string key);
    void Clear();
    IEnumerable<string> Keys { get; }
}
```

---

## Best Practices

### 1. Component Order Matters

Add perception components **before** NPCBrainController:

```csharp
// Correct order:
gameObject.AddComponent<SightSensor>();     // First!
gameObject.AddComponent<GuardNPC>();        // Second
```

### 2. Use Tags for Detection

Create the "Player" tag in **Edit → Project Settings → Tags and Layers** before using SightSensor.

### 3. Keep Trees Simple

Prefer many small, focused behavior trees over one giant tree.

```csharp
// Good: Separate behaviors
BTNode chaseBehavior = CreateChaseTree();
BTNode patrolBehavior = CreatePatrolTree();

return new Selector(chaseBehavior, patrolBehavior);

// Bad: Everything inline
return new Selector(
    new Sequence(/* 20 nodes */),
    new Sequence(/* 15 nodes */),
    // ...
);
```

### 4. Use Blackboard for Communication

Don't pass data through method calls—use the Blackboard.

```csharp
// Good
Blackboard.Set("lastKnownPosition", position);

// Later, in another node:
var pos = brain.Blackboard.Get<Vector3>("lastKnownPosition");
```

### 5. Set Timeouts on MoveTo

```csharp
// Prevent stuck NPCs
new MoveTo(() => target, 0.5f, 5f, 10f)  // 10 second timeout
```

### 6. Use NavMeshAgent When Possible

MoveTo automatically uses NavMeshAgent if present for proper pathfinding.

### 7. Name Your Nodes

```csharp
var chase = new Sequence(...);
chase.Name = "ChaseSequence";  // Shows in Debug Window
```

---

## Demo Scenes

Create demo scenes via the Unity menu:

- **NPCBrain → Create Guard Demo Scene**
- **NPCBrain → Create Patrol Demo Scene**
- **NPCBrain → Create All Demo Scenes**

### Guard Demo

Demonstrates GuardNPC with player interaction:
- Use **WASD** to move the player
- **Shift** to sprint
- Guards will chase when they see you!

### Patrol Demo

Demonstrates PatrolNPC with multiple patrol patterns:
- Square, Diamond, Circle, and Line patterns
- Color-coded NPCs and waypoints
- Random timing variation

---

## Support

**Documentation:** See the `Demo/README.md` for demo-specific documentation.

**Issues:** If you encounter bugs or have feature requests, please contact support.

**Updates:** Check the Asset Store for the latest version.

---

## Changelog

### Version 1.0
- Initial release
- Behavior Tree system with Selector, Sequence, Decorators
- Utility AI with softmax selection
- Criticality Controller for adaptive exploration
- SightSensor perception
- GuardNPC and PatrolNPC archetypes
- Debug Window and Scene Gizmos
- 100+ unit tests

---

© 2025 - All Rights Reserved
