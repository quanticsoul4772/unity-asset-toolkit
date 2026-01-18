# Getting Started with NPCBrain

This guide walks you through creating your first AI-powered NPC using NPCBrain.

## Table of Contents

1. [Basic Setup](#basic-setup)
2. [Creating a Patrol NPC](#creating-a-patrol-npc)
3. [Adding Perception](#adding-perception)
4. [Using Utility AI](#using-utility-ai)
5. [Understanding Criticality](#understanding-criticality)
6. [Common Patterns](#common-patterns)

---

## Basic Setup

### Step 1: Create Your NPC Script

Create a new C# script that inherits from `NPCBrainController`:

```csharp
using NPCBrain;
using NPCBrain.BehaviorTree;

public class SimpleNPC : NPCBrainController
{
    protected override BTNode CreateBehaviorTree()
    {
        // Return your behavior tree here
        return null;
    }
}
```

### Step 2: Attach to GameObject

1. Create a new GameObject (e.g., Capsule)
2. Add your `SimpleNPC` component
3. Press Play - the NPC will do nothing until you define a behavior tree

---

## Creating a Patrol NPC

### Step 1: Set Up Waypoints

1. Create empty GameObjects for waypoints
2. Create another GameObject and add the `WaypointPath` component
3. Add your waypoint transforms to the `Waypoints` array

### Step 2: Create the Patrol Behavior

```csharp
using NPCBrain;
using NPCBrain.BehaviorTree;
using NPCBrain.BehaviorTree.Composites;
using NPCBrain.BehaviorTree.Actions;
using UnityEngine;

public class PatrolNPC : NPCBrainController
{
    protected override BTNode CreateBehaviorTree()
    {
        return new Sequence(
            // Move to current waypoint
            new MoveTo(
                () => GetCurrentWaypoint(),
                stoppingDistance: 0.5f,
                speed: 3f
            ),
            // Wait at waypoint
            new Wait(2f),
            // Advance to next waypoint
            new AdvanceWaypoint()
        );
    }
}
```

### Step 3: Link the Waypoint Path

1. In the Inspector, drag your WaypointPath to the PatrolNPC's `Waypoint Path` field
2. Press Play - the NPC will patrol between waypoints

---

## Adding Perception

### Step 1: Add SightSensor

1. Add `SightSensor` component to your NPC
2. Configure vision settings:
   - **View Distance**: How far the NPC can see (default: 20)
   - **View Angle**: Field of view in degrees (default: 120)
   - **Target Tag**: Tag to look for (default: "Player")

### Step 2: React to Targets

```csharp
using NPCBrain;
using NPCBrain.BehaviorTree;
using NPCBrain.BehaviorTree.Composites;
using NPCBrain.BehaviorTree.Actions;
using NPCBrain.BehaviorTree.Conditions;
using UnityEngine;

public class GuardNPC : NPCBrainController
{
    protected override BTNode CreateBehaviorTree()
    {
        return new Selector(
            // Priority 1: Chase visible target
            new Sequence(
                new CheckTargetVisible(), // Check if any target visible
                new LookAt(brain => brain.Perception.ClosestTarget), // Face the target
                new MoveTo(
                    () => Perception.ClosestTarget?.transform.position ?? transform.position,
                    stoppingDistance: 1.5f,
                    speed: 5f
                )
            ),
            // Priority 2: Patrol
            new Sequence(
                new MoveTo(() => GetCurrentWaypoint(), 0.5f, 3f),
                new Wait(1f),
                new AdvanceWaypoint()
            )
        );
    }
}
```

### Step 3: Use Memory for Lost Targets

```csharp
using NPCBrain.Perception; // Add this import for Memory

private Memory _memory = new Memory();

protected override void Awake()
{
    base.Awake();
    
    OnTargetAcquired += (target) => 
        _memory.UpdateVisible(target, target.transform.position);
    
    OnTargetLost += (target) => 
        _memory.MarkLost(target);
}

protected override BTNode CreateBehaviorTree()
{
    return new Selector(
        // Chase visible target
        new Sequence(
            new CheckTargetVisible(),
            new MoveTo(() => Perception.ClosestTarget.transform.position, 1.5f, 5f)
        ),
        // Go to last known position
        new Sequence(
            new CheckBlackboard("hasLostTarget", true),
            new MoveTo(() => _memory.GetMostRecentTarget()?.transform.position ?? transform.position, 1f, 4f)
        ),
        // Default patrol
        new Sequence(
            new MoveTo(() => GetCurrentWaypoint(), 0.5f, 3f),
            new Wait(1f),
            new AdvanceWaypoint()
        )
    );
}
```

---

## Using Utility AI

Utility AI selects actions based on scores rather than fixed priorities.

### Basic Utility Setup

```csharp
using NPCBrain;
using NPCBrain.BehaviorTree;
using NPCBrain.BehaviorTree.Composites;
using NPCBrain.BehaviorTree.Actions;
using NPCBrain.UtilityAI;
using NPCBrain.UtilityAI.Curves;

public class UtilityNPC : NPCBrainController
{
    protected override BTNode CreateBehaviorTree()
    {
        return new UtilitySelector(
            // Wander action - always available
            new UtilityAction(
                "Wander",
                new MoveTo(() => GetRandomPosition(), 0.5f, 2f),
                baseScore: 0.3f,
                new ConstantConsideration(1f)
            ),
            // Rest action - more desirable when tired
            new UtilityAction(
                "Rest",
                new Wait(3f),
                baseScore: 0.5f,
                new BlackboardConsideration<float>(
                    "EnergyCheck",  // name
                    "energy",       // blackboard key
                    t => 1f - t,    // normalizer: low energy = high score
                    1f              // default value
                )
            ),
            // Patrol - moderate priority
            new UtilityAction(
                "Patrol",
                new Sequence(
                    new MoveTo(() => GetCurrentWaypoint(), 0.5f, 3f),
                    new AdvanceWaypoint()
                ),
                baseScore: 0.6f,
                new ConstantConsideration(1f)
            )
        );
    }
    
    private Vector3 GetRandomPosition()
    {
        return transform.position + Random.insideUnitSphere * 5f;
    }
}
```

### Response Curves

| Curve | Description | Use Case |
|-------|-------------|----------|
| `LinearCurve` | Linear interpolation | General purpose |
| `ExponentialCurve` | Exponential growth | Urgency, danger |
| `StepCurve` | Binary threshold | On/off conditions |

---

## Understanding Criticality

The Criticality system prevents repetitive behavior by tracking action entropy.

### How It Works

1. **Entropy**: Measures action variety (0 = repetitive, high = varied)
2. **Temperature**: Controls randomness (high = more random selection)
3. **Inertia**: Tendency to stick with current action

### Behavior

- **Repetitive actions** → Low entropy → Temperature INCREASES → More random exploration
- **Varied actions** → High entropy → Temperature DECREASES → More deterministic choices

### Accessing Criticality

```csharp
void Update()
{
    // Read current values
    float temp = Criticality.Temperature;     // Range: 0.5 to 2.0
    float inertia = Criticality.Inertia;      // Range: 0.0 to 1.0
    float entropy = Criticality.Entropy;      // Range: 0.0 to ~2.0
    
    // Reset to defaults
    Criticality.Reset();
}
```

The `UtilitySelector` automatically uses Criticality when selecting actions.

---

## Common Patterns

### Pattern 1: State Machine Style

```csharp
return new Selector(
    // State: Combat
    new Sequence(
        new CheckBlackboard("state", "combat"),
        CreateCombatBehavior()
    ),
    // State: Patrol
    new Sequence(
        new CheckBlackboard("state", "patrol"),
        CreatePatrolBehavior()
    ),
    // Default: Idle
    new Wait(1f)
);
```

### Pattern 2: Priority-Based

```csharp
return new Selector(
    // Highest priority: Flee if health low
    new Sequence(
        new CheckBlackboard<float>("health", h => h < 20),
        new Log("Health low, fleeing!", Log.LogLevel.Warning),
        CreateFleeBehavior()
    ),
    // Medium priority: Attack if enemy nearby
    new Sequence(
        new CheckTargetVisible(),
        new LookAt(brain => brain.Perception.ClosestTarget),
        CreateAttackBehavior()
    ),
    // Lowest priority: Patrol
    CreatePatrolBehavior()
);
```

### Pattern 3: Cooldown-Based Actions

```csharp
return new Selector(
    // Special attack with cooldown
    new Cooldown(
        new Sequence(
            new CheckTargetVisible(),
            new Log("Executing special attack!"),
            CreateSpecialAttack()
        ),
        cooldownSeconds: 10f
    ),
    // Normal attack
    new Sequence(
        new CheckTargetVisible(),
        new LookAt(brain => brain.Perception.ClosestTarget, rotationSpeed: 720f),
        CreateNormalAttack()
    )
);
```

### Pattern 4: Parallel Actions

```csharp
return new Parallel(
    // Always: Update perception
    CreatePerceptionUpdate(),
    // Always: Check for threats
    CreateThreatMonitor(),
    // Main behavior
    CreateMainBehavior()
);
```

### Pattern 5: Optional Behaviors with Succeeder

```csharp
return new Sequence(
    // Optional: Try to look at target (don't fail if no target)
    new Succeeder(
        new Sequence(
            new CheckTargetVisible(),
            new LookAt(brain => brain.Perception.ClosestTarget)
        )
    ),
    // Always: Continue with main behavior
    new MoveTo(() => GetDestination(), 0.5f, 3f)
);
```

### Pattern 6: Debug Logging

```csharp
return new Sequence(
    new Log("Starting behavior tree tick"),
    new Selector(
        new Sequence(
            new CheckTargetVisible(),
            new Log(brain => $"Target spotted: {brain.Perception.ClosestTarget.name}"),
            CreateChaseBehavior()
        ),
        new Sequence(
            new Log("No targets, patrolling"),
            CreatePatrolBehavior()
        )
    )
);
```

---

## Next Steps

1. **Explore the Demos**: Menu > NPCBrain > Open [Demo Name]
2. **Read the API Reference**: [API.md](API.md)
3. **Experiment**: Combine different nodes to create unique behaviors

## Requirements

- Unity 6000.0 or later
- **Input System package** (required for demo scenes)

## Tips

- Start simple, add complexity gradually
- Use the Debug Window (Menu > NPCBrain > Open Debug Window) to monitor NPCs
- Enable `Debug Logging` on SightSensor to troubleshoot perception issues
- Use Blackboard for sharing data between nodes
- Test with the UtilityDemo to understand Criticality
