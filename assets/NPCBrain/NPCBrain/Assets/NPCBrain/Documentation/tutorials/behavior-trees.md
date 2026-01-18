# Behavior Trees Tutorial

Behavior Trees (BTs) are the core decision-making system in NPCBrain. This tutorial explains how they work and how to build complex NPC behaviors.

---

## Table of Contents

1. [What is a Behavior Tree?](#what-is-a-behavior-tree)
2. [Node Status](#node-status)
3. [Composite Nodes](#composite-nodes)
4. [Decorator Nodes](#decorator-nodes)
5. [Action Nodes](#action-nodes)
6. [Condition Nodes](#condition-nodes)
7. [Building Complex Behaviors](#building-complex-behaviors)
8. [Best Practices](#best-practices)

---

## What is a Behavior Tree?

A Behavior Tree is a hierarchical structure of nodes that controls NPC decision-making. Each tick, the tree is traversed from the root, and nodes return one of three statuses: **Success**, **Failure**, or **Running**.

### Visual Representation

```
                    [Root: Selector]
                    /       |       \
           [Sequence]   [Sequence]   [Sequence]
           /    |         /    \        |
      [Check] [Chase]  [Check] [Inv] [Patrol]
      Target          LastPos
```

### Execution Flow

1. Tree starts at the root node each tick
2. Nodes execute their logic and return a status
3. Parent nodes use child results to determine their own status
4. "Running" nodes maintain state between ticks

---

## Node Status

Every node returns one of three statuses:

| Status | Meaning | Parent Behavior |
|--------|---------|----------------|
| `Success` | Node completed successfully | Continues to next step |
| `Failure` | Node failed to complete | Depends on parent type |
| `Running` | Node still executing | Re-evaluates next tick |

```csharp
public enum NodeStatus
{
    Success,
    Failure,
    Running
}
```

---

## Composite Nodes

Composites have multiple children and control flow based on child results.

### Selector (OR Logic)

Tries children in order until one **succeeds**.

```
[Selector]
├── Child 1: Failure  ← Tries this first
├── Child 2: Success  ← Stops here, returns Success
└── Child 3: (skipped)
```

**Use case:** Priority-based behavior selection

```csharp
// Try high-priority behaviors first
return new Selector(
    CreateChaseBehavior(),      // If target visible
    CreateInvestigateBehavior(), // Else if heard something
    CreatePatrolBehavior()       // Fallback
);
```

**Return values:**
- Returns `Success` if ANY child succeeds
- Returns `Failure` if ALL children fail
- Returns `Running` if current child is running

### Sequence (AND Logic)

Runs children in order; all must **succeed**.

```
[Sequence]
├── Child 1: Success  ← Continues
├── Child 2: Success  ← Continues  
├── Child 3: Failure  ← Stops here, returns Failure
└── Child 4: (skipped)
```

**Use case:** Multi-step actions that must all complete

```csharp
// All steps must succeed
return new Sequence(
    new CheckBlackboard("target"),  // Must have target
    new MoveTo(() => targetPos),     // Must reach it
    new Wait(1f),                    // Must wait
    new Attack()                     // Must attack
);
```

**Return values:**
- Returns `Success` if ALL children succeed
- Returns `Failure` if ANY child fails
- Returns `Running` if current child is running

### Parallel

Runs all children simultaneously.

```
[Parallel(RequireAll=true)]
├── Child 1: Running  ← All running
├── Child 2: Running  ← simultaneously
└── Child 3: Running
```

**Use case:** Simultaneous behaviors (move AND animate)

```csharp
// Do multiple things at once
return new Parallel(
    new MoveTo(() => destination),
    new PlayAnimation("walk"),
    new LookAt(() => destination)
);
```

**Policies:**
- `RequireAll = true`: Success when ALL complete
- `RequireAll = false`: Success when ANY completes

### UtilitySelector (Score-Based)

Selects children based on utility scores. See [Utility AI Tutorial](utility-ai.md).

```csharp
return new UtilitySelector(
    new UtilityAction("Attack", attackBehavior, 1.0f, damageConsideration),
    new UtilityAction("Flee", fleeBehavior, 0.8f, healthConsideration),
    new UtilityAction("Patrol", patrolBehavior, 0.3f)
);
```

---

## Decorator Nodes

Decorators wrap a single child and modify its behavior.

### Inverter

Flips Success ↔ Failure.

```csharp
// Succeeds if target is NOT visible
new Inverter(
    new CheckBlackboard("target")
)
```

| Child Result | Inverter Result |
|--------------|----------------|
| Success | Failure |
| Failure | Success |
| Running | Running |

### Repeater

Repeats child execution a specified number of times.

```csharp
// Patrol 5 waypoints before stopping
new Repeater(
    new Sequence(
        new MoveTo(() => GetWaypoint()),
        new Wait(1f),
        new AdvanceWaypoint()
    ),
    5  // Repeat 5 times
)

// Repeat forever (pass -1 or use loop)
new Repeater(patrolBehavior, -1)
```

### Cooldown

Prevents execution until cooldown expires.

```csharp
// Can only attack every 2 seconds
new Cooldown(
    new Attack(),
    2f  // Cooldown duration
)
```

**Behavior:**
- First execution: Runs normally
- Subsequent executions within cooldown: Returns `Failure`
- After cooldown expires: Runs normally again

### Succeeder

Always returns Success, regardless of child result.

```csharp
// Try to reload, but don't fail the sequence if we can't
new Sequence(
    new Attack(),
    new Succeeder(new Reload()),  // Optional reload
    new Attack()
)
```

---

## Action Nodes

Actions perform actual work and are the leaf nodes of the tree.

### MoveTo

Moves the NPC to a target position.

```csharp
new MoveTo(
    () => targetPosition,  // Target getter (Func<Vector3>)
    0.5f,                  // Arrival distance
    5f,                    // Movement speed
    30f                    // Timeout in seconds (optional)
)
```

**Features:**
- Automatically uses NavMeshAgent if present
- Falls back to direct movement otherwise
- Returns `Success` when within arrival distance
- Returns `Failure` on timeout or if stuck

### Wait

Pauses execution for a duration.

```csharp
// Simple wait
new Wait(2.5f)

// Wait with callback when complete
new Wait(2f, () => {
    Debug.Log("Done waiting!");
})
```

### AdvanceWaypoint

Moves to the next waypoint in the NPC's WaypointPath.

```csharp
new AdvanceWaypoint()
```

**Note:** Requires a `WaypointPath` component on the NPC or assigned via `SetWaypointPath()`.

### LookAt

Rotates the NPC to face a target.

```csharp
new LookAt(
    () => target.transform.position,  // Look target
    5f,                                // Rotation speed
    5f                                 // Angle threshold for success
)
```

### SetBlackboard

Sets a value in the blackboard.

```csharp
// Set a constant value
new SetBlackboard("state", "investigating")

// Set a computed value
new SetBlackboard("lastPosition", () => transform.position)
```

### ClearBlackboardKey

Removes a key from the blackboard.

```csharp
new ClearBlackboardKey("target")
```

### Log

Logs a message (useful for debugging).

```csharp
new Log("Starting patrol behavior")
```

---

## Condition Nodes

Conditions check state and return Success or Failure.

### CheckBlackboard

Checks if a key exists in the blackboard.

```csharp
// Check if key exists
new CheckBlackboard("target")

// Check with predicate
new CheckBlackboard<float>("health", hp => hp > 50f)

// Check boolean value
new CheckBlackboard<bool>("isAlerted", v => v == true)
```

### CheckDistance

Compares distances between positions.

```csharp
new CheckDistance(
    brain => brain.transform.position,           // From
    brain => brain.Blackboard.Get<Vector3>("target"),  // To
    10f,                                         // Distance threshold
    CheckDistance.ComparisonType.LessThan        // Comparison type
)
```

**Comparison Types:**
- `LessThan`
- `LessThanOrEqual`
- `GreaterThan`
- `GreaterThanOrEqual`
- `Equal`

---

## Building Complex Behaviors

### Pattern: Priority Selector

```csharp
protected override BTNode CreateBehaviorTree()
{
    return new Selector(
        // Highest priority first
        CriticalBehavior(),     // e.g., flee when health low
        CombatBehavior(),       // e.g., attack if target visible
        AlertBehavior(),        // e.g., investigate sounds
        IdleBehavior()          // Fallback: patrol or idle
    );
}
```

### Pattern: Gated Sequence

```csharp
private BTNode CombatBehavior()
{
    return new Sequence(
        // Gate: only if target exists and is close
        new CheckBlackboard("target"),
        new CheckDistance(/* ... */, CheckDistance.ComparisonType.LessThan),
        
        // Actions: performed only if gates pass
        new MoveTo(() => GetTargetPosition(), 2f, _chaseSpeed),
        new Attack()
    );
}
```

### Pattern: Interruptible Behavior

```csharp
// Use Selector to allow interruption
return new Selector(
    // High priority interrupt
    new Sequence(
        new CheckBlackboard("danger"),
        new Flee()
    ),
    // Normal behavior (interrupted if danger appears)
    new Sequence(
        new MoveTo(() => destination),
        new DoWork()
    )
);
```

### Pattern: State Machine Style

```csharp
private BTNode CreateStateMachine()
{
    return new Selector(
        // Each "state" is a gated sequence
        new Sequence(
            new CheckBlackboard<string>("state", s => s == "chase"),
            ChaseState()
        ),
        new Sequence(
            new CheckBlackboard<string>("state", s => s == "investigate"),
            InvestigateState()
        ),
        new Sequence(
            new CheckBlackboard<string>("state", s => s == "patrol"),
            PatrolState()
        ),
        // Default state
        IdleState()
    );
}
```

---

## Best Practices

### 1. Name Your Nodes

```csharp
var chase = new Sequence(
    new CheckBlackboard("target"),
    new MoveTo(() => targetPos)
);
chase.Name = "ChaseBehavior";  // Shows in debug window
```

### 2. Keep Trees Shallow

Deep nesting makes trees hard to debug. Extract sub-behaviors:

```csharp
// Good: Flat, readable
return new Selector(
    CreateChaseBehavior(),
    CreateInvestigateBehavior(),
    CreatePatrolBehavior()
);

// Bad: Deeply nested inline
return new Selector(
    new Sequence(
        new Sequence(
            new Sequence(/* ... */)
        )
    )
);
```

### 3. Use Timeouts

```csharp
// Prevent stuck NPCs
new MoveTo(() => target, 0.5f, 5f, 30f)  // 30 second timeout
```

### 4. Clean Up Blackboard State

```csharp
// Clear temporary values when done
new Sequence(
    new MoveTo(() => lastKnownPos),
    new Wait(3f),
    new ClearBlackboardKey("lastKnownPosition")  // Clean up!
)
```

### 5. Use Cooldowns for Expensive Operations

```csharp
// Don't spam expensive checks
new Cooldown(
    new PerformExpensiveCheck(),
    1f  // Only check once per second
)
```

### 6. Consider Node Order in Selectors

```csharp
// Fast-failing checks first
return new Selector(
    new Sequence(
        new CheckBlackboard("target"),  // Fast check first
        new CheckLineOfSight(),          // Then expensive raycast
        new Chase()
    ),
    PatrolBehavior()
);
```

---

## Creating Custom Nodes

### Custom Action Node

```csharp
using NPCBrain;
using NPCBrain.BehaviorTree;

public class Attack : BTNode
{
    private float _attackDuration;
    private float _startTime;
    
    public Attack(float duration = 1f)
    {
        Name = "Attack";
        _attackDuration = duration;
    }
    
    protected override void OnEnter(NPCBrainController brain)
    {
        _startTime = Time.time;
        // Start attack animation, etc.
    }
    
    protected override NodeStatus Tick(NPCBrainController brain)
    {
        if (Time.time - _startTime >= _attackDuration)
        {
            return NodeStatus.Success;
        }
        return NodeStatus.Running;
    }
    
    protected override void OnExit(NPCBrainController brain)
    {
        // Cleanup
    }
}
```

### Custom Condition Node

```csharp
public class CheckHealth : BTNode
{
    private readonly float _threshold;
    private readonly bool _checkAbove;
    
    public CheckHealth(float threshold, bool checkAbove = true)
    {
        Name = "CheckHealth";
        _threshold = threshold;
        _checkAbove = checkAbove;
    }
    
    protected override NodeStatus Tick(NPCBrainController brain)
    {
        float health = brain.Blackboard.Get<float>("health", 100f);
        
        if (_checkAbove)
        {
            return health > _threshold ? NodeStatus.Success : NodeStatus.Failure;
        }
        return health < _threshold ? NodeStatus.Success : NodeStatus.Failure;
    }
}
```

---

[← Getting Started](../getting-started.md) | [Utility AI →](utility-ai.md)
