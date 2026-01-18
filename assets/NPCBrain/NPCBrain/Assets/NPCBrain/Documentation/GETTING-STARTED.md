# Getting Started with NPCBrain

This guide will walk you through creating your first AI-controlled NPC using NPCBrain.

## Installation

1. Import the NPCBrain package into your Unity project
2. The package will be available under `Assets/NPCBrain`

## Creating Your First NPC

### Step 1: Create the NPC GameObject

1. Create a new 3D Object (e.g., Capsule) in your scene
2. Position it where you want your NPC to start

### Step 2: Add the NPC Component

You can use a built-in archetype or create your own:

**Using a Built-in Archetype:**

1. Add Component > NPCBrain > Archetypes > PatrolNPC (or GuardNPC)
2. The NPC is ready to use!

**Creating a Custom NPC:**

1. Create a new C# script:

```csharp
using UnityEngine;
using NPCBrain;
using NPCBrain.BehaviorTree;
using NPCBrain.BehaviorTree.Composites;
using NPCBrain.BehaviorTree.Actions;

public class MyNPC : NPCBrainController
{
    [SerializeField] private float _moveSpeed = 5f;
    
    protected override BTNode CreateBehaviorTree()
    {
        // Define your behavior tree here
        return new Sequence(
            new MoveTo(() => GetCurrentWaypoint(), 0.5f, _moveSpeed),
            new Wait(1f),
            new AdvanceWaypoint()
        );
    }
}
```

2. Add your script to the NPC GameObject

### Step 3: Set Up Waypoints

1. Create empty GameObjects for each waypoint
2. Position them where you want the NPC to patrol
3. Create a parent GameObject with a `WaypointPath` component
4. Add the waypoint transforms to the path
5. Assign the `WaypointPath` to your NPC's Waypoint Path field

### Step 4: Add Perception (Optional)

To let your NPC detect the player:

1. Add a `SightSensor` component to your NPC
2. Configure the view distance and angle
3. Set the Target Tag to "Player"
4. Ensure your player has the "Player" tag

### Step 5: Press Play!

Your NPC should now:
- Follow the waypoint path
- Detect the player (if SightSensor is attached)
- React based on your behavior tree

## Understanding Behavior Trees

Behavior Trees are built from nodes. Here's how they work:

### Composites: Control Flow

```csharp
// Sequence: Do things in order
// Fails if any child fails
new Sequence(
    new MoveTo(() => position, 0.5f, 5f),  // Step 1
    new Wait(2f),                            // Step 2
    new Log("Done!")                         // Step 3
)

// Selector: Try options until one works
// Succeeds if any child succeeds
new Selector(
    new ChasePlayer(),      // Try this first
    new SearchForPlayer(),  // If chase fails, search
    new Patrol()            // If search fails, patrol
)
```

### Decorators: Modify Behavior

```csharp
// Repeat an action 3 times
new Repeater(new Patrol(), 3)

// Invert the result
new Inverter(new CheckDanger())  // "is safe" = NOT "is danger"

// Rate limit
new Cooldown(new Attack(), 2f)  // Can only attack every 2 seconds
```

### Conditions: Check State

```csharp
// Check if target is visible
new CheckTargetVisible()

// Check blackboard value
new CheckBlackboard<float>("health", h => h > 0.5f)

// Check distance
new CheckDistance(() => targetPosition, 5f, Comparison.Less)
```

### Actions: Do Things

```csharp
// Move to position
new MoveTo(() => targetPosition, 0.5f, 5f, 10f)

// Wait
new Wait(2f)

// Set data
new SetBlackboard("lastPatrolTime", () => Time.time)
```

## Using Utility AI

For more organic behavior, use `UtilitySelector`:

```csharp
protected override BTNode CreateBehaviorTree()
{
    return new UtilitySelector(
        // Wander when bored
        new UtilityAction(
            "Wander",
            WanderBehavior(),
            0.3f,  // base score
            new TimeConsideration("WanderCooldown", "lastWander", 5f)
        ),
        
        // Rest when tired
        new UtilityAction(
            "Rest",
            RestBehavior(),
            0.2f,
            new BlackboardConsideration<float>("Energy", "energy", 
                e => 1f - e, 1f)  // Lower energy = higher score
        ),
        
        // Chase when enemy spotted
        new UtilityAction(
            "Chase",
            ChaseBehavior(),
            0.5f,
            new ConstantConsideration(1f),
            new BlackboardConsideration<GameObject>("HasTarget", "target",
                t => t != null ? 1f : 0f, null)
        )
    );
}
```

## Using Memory and Target Selection

For advanced perception:

```csharp
public class SmartNPC : NPCBrainController
{
    private Memory _memory;
    private TargetSelector _targetSelector;
    
    protected override void Awake()
    {
        base.Awake();
        _memory = new Memory();
        _targetSelector = new TargetSelector();
    }
    
    private void LateUpdate()
    {
        // Update memory with visible targets
        if (Perception != null)
        {
            foreach (var target in Perception.VisibleTargets)
            {
                _memory.UpdateVisible(target, target.transform.position);
            }
        }
        
        // Apply memory decay
        _memory.Tick();
        
        // Select best target
        var best = _targetSelector.SelectBest(
            Perception, _memory, 
            transform.position, transform.forward,
            Blackboard
        );
        
        if (best != null)
        {
            Blackboard.Set("target", best);
        }
    }
}
```

## Debugging

### Debug Window

1. Open `Window > NPCBrain > Debug Window`
2. Select an NPC from the dropdown
3. View real-time behavior tree state
4. Inspect blackboard values
5. Monitor criticality metrics

### Scene Gizmos

- Select an NPC to see its vision cone
- Green cone = no targets
- Red cone = target visible
- Yellow markers = remembered positions

## Next Steps

- Check out the demo scenes for examples
- Read the API Reference for detailed documentation
- Experiment with different behavior tree structures
- Try the Utility AI for more organic behavior
