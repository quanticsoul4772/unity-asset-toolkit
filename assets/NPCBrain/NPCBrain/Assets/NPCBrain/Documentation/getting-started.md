# Getting Started with NPCBrain

This guide will walk you through setting up NPCBrain and creating your first intelligent NPC.

## Table of Contents

1. [Installation](#installation)
2. [Project Setup](#project-setup)
3. [Your First NPC](#your-first-npc)
4. [Using Built-in Archetypes](#using-built-in-archetypes)
5. [Adding Perception](#adding-perception)
6. [Next Steps](#next-steps)

## Installation

### From Unity Asset Store

1. Open the Asset Store window (Window → Asset Store)
2. Search for "NPCBrain"
3. Click Download, then Import
4. Import all files when prompted

### Package Contents

```
Assets/
└── NPCBrain/
    ├── Runtime/           # Core scripts
    │   ├── Core/          # NPCBrainController, Blackboard
    │   ├── BehaviorTree/  # BT nodes and composites
    │   ├── UtilityAI/     # Utility scoring system
    │   ├── Perception/    # Sensors and memory
    │   ├── Criticality/   # Adaptive temperature
    │   ├── Archetypes/    # Ready-to-use NPCs
    │   └── Components/    # Game components
    ├── Editor/            # Debug tools and inspectors
    ├── Demo/              # Example scenes and scripts
    ├── Tests/             # Unit and integration tests
    └── Documentation/     # This documentation
```

## Project Setup

### Required Settings

#### 1. Create the "Player" Tag

NPCBrain's perception system uses Unity tags for target detection:

1. Go to **Edit → Project Settings → Tags and Layers**
2. Under "Tags", click the **+** button
3. Add a tag named `Player`
4. Apply this tag to any GameObjects you want NPCs to detect

You can use any tag name, but "Player" is the default for all built-in archetypes.

#### 2. NavMesh Setup (Optional but Recommended)

For NPCs that navigate around obstacles:

1. Open **Window → AI → Navigation**
2. Select your walkable surfaces
3. Mark them as "Navigation Static"
4. Click **Bake**
5. Add `NavMeshAgent` component to your NPCs

MoveTo automatically uses NavMeshAgent when present. Without it, NPCs move in straight lines.

## Your First NPC

### Step 1: Create the Script

Create a new C# script called `MyFirstNPC.cs`:

```csharp
using UnityEngine;
using NPCBrain;
using NPCBrain.BehaviorTree;
using NPCBrain.BehaviorTree.Composites;
using NPCBrain.BehaviorTree.Actions;

public class MyFirstNPC : NPCBrainController
{
    [Header("Movement")]
    [SerializeField] private float _moveSpeed = 3f;
    [SerializeField] private float _waitTime = 2f;
    
    protected override BTNode CreateBehaviorTree()
    {
        // Create a simple patrol behavior:
        // 1. Move to current waypoint
        // 2. Wait for a bit
        // 3. Advance to next waypoint
        // 4. Repeat forever
        
        return new Sequence(
            new MoveTo(
                () => GetCurrentWaypoint(),  // Target position
                0.5f,                         // Arrival distance
                _moveSpeed,                   // Movement speed
                30f                           // Timeout (seconds)
            ),
            new Wait(_waitTime),
            new AdvanceWaypoint()
        );
    }
}
```

### Step 2: Setup in Unity

1. **Create the NPC GameObject**
   - Right-click in Hierarchy → 3D Object → Capsule
   - Name it "PatrolNPC"
   - Position it above your ground

2. **Add Components**
   - Add your `MyFirstNPC` component
   - Add `WaypointPath` component

3. **Create Waypoints**
   - Create 4 empty GameObjects as children of the NPC
   - Name them "Waypoint1", "Waypoint2", etc.
   - Position them around your scene
   - The `WaypointPath` will auto-detect child waypoints

4. **Press Play!**
   - Your NPC should patrol between the waypoints

### Visual Structure

```
PatrolNPC (Capsule)
├── MyFirstNPC (Script)
├── WaypointPath (Script)
├── Waypoint1 (Empty GameObject)
├── Waypoint2 (Empty GameObject)
├── Waypoint3 (Empty GameObject)
└── Waypoint4 (Empty GameObject)
```

## Using Built-in Archetypes

Included NPC types that require no coding:

### PatrolNPC - Simple Patrol

Use for: Background NPCs, civilian patrols, ambient movement

```csharp
// Just add the PatrolNPC component - that's it!
// Inspector settings:
// - Patrol Speed: 3
// - Waypoint Wait Time: 2
// - Arrival Distance: 0.5
```

**Setup:**
1. Add `PatrolNPC` component
2. Add `WaypointPath` component
3. Create waypoint children
4. Play!

### GuardNPC - Sight-Based Guard

Use for: Guards, enemies, lookouts

Behaviors:
- Patrols waypoints when idle
- Chases visible targets
- Investigates last known position
- Returns to patrol when target lost

```csharp
// Inspector settings:
// - Chase Speed: 6
// - Patrol Speed: 3
// - Max Chase Distance: 20
// - Investigate Time: 3
```

**Setup:**
1. Add `SightSensor` component **first**
2. Add `GuardNPC` component
3. Add `WaypointPath` component
4. Ensure targets have the "Player" tag

### HearingGuardNPC - Sound-Responsive Guard

Use for: Stealth games, alert systems

Additional Behaviors:
- Investigates gunshots (high priority)
- Investigates footsteps (lower priority)
- Uses both sight AND hearing

**Setup:**
1. Add `SightSensor` component
2. Add `HearingSensor` component
3. Add `HearingGuardNPC` component
4. Add `WaypointPath` component

## Adding Perception

### SightSensor - Vision Detection

The SightSensor creates a vision cone that detects tagged targets:

```csharp
// SightSensor properties (Inspector):
// - View Distance: 20 (how far NPC can see)
// - View Angle: 120 (field of view degrees)
// - Eye Height: 1.5 (raycast origin offset)
// - Target Tag: "Player" (what to look for)
// - Obstacle Mask: Everything (what blocks view)
// - Target Mask: Default (layers with targets)
```

**Using SightSensor in Code:**

```csharp
public class AlertNPC : NPCBrainController
{
    protected override void Awake()
    {
        base.Awake();
        
        // Subscribe to perception events
        OnTargetAcquired += HandleTargetSpotted;
        OnTargetLost += HandleTargetLost;
    }
    
    private void HandleTargetSpotted(GameObject target)
    {
        Debug.Log($"I see {target.name}!");
        Blackboard.Set("target", target);
    }
    
    private void HandleTargetLost(GameObject target)
    {
        Debug.Log($"Lost sight of {target.name}");
        // Store last known position
        Blackboard.SetWithTTL("lastKnownPos", target.transform.position, 10f);
        Blackboard.Remove("target");
    }
}
```

### HearingSensor - Sound Detection

Detects sounds emitted via `SoundEmitter` or `SoundManager`:

```csharp
// HearingSensor properties (Inspector):
// - Hearing Range: 30 (max detection distance)
// - Hearing Threshold: 0.1 (minimum volume)
// - Sound Mask: Everything (which sounds to hear)
```

**Emitting Sounds:**

```csharp
// Emit a sound that NPCs can hear
SoundManager.EmitSound(
    transform.position,  // Where
    SoundType.Gunshot,   // What type
    1.0f,                // Volume (affects range)
    gameObject           // Who made it
);
```

## Next Steps

### Learn More
- [Behavior Trees Tutorial](tutorials/behavior-trees.md) - BT nodes
- [Utility AI Tutorial](tutorials/utility-ai.md) - Score-based decisions
- [Perception Tutorial](tutorials/perception.md) - Sensor usage
- [Criticality Tutorial](tutorials/criticality.md) - Adaptive behavior

### Try the Demos
- Window > NPCBrain > Create Guard Demo
- Window > NPCBrain > Create Patrol Demo

### Debug Your NPCs
- Window > NPCBrain > Debug Window - Inspect NPC state
- Enable gizmos to see vision cones in Scene view

### Common Issues

| Problem | Solution |
|---------|----------|
| NPC doesn't move | Check for NavMeshAgent or ensure ground is flat |
| NPC doesn't see targets | Verify "Player" tag exists and is applied |
| Errors about missing tag | Create tag in Project Settings → Tags and Layers |
| NPC gets stuck | Add NavMesh and NavMeshAgent, or reduce obstacles |
| Behavior tree not running | Ensure `CreateBehaviorTree()` returns a non-null node |

## Example: Complete Guard NPC

Here's a complete example combining everything:

```csharp
using UnityEngine;
using NPCBrain;
using NPCBrain.BehaviorTree;
using NPCBrain.BehaviorTree.Composites;
using NPCBrain.BehaviorTree.Actions;
using NPCBrain.BehaviorTree.Conditions;
using NPCBrain.BehaviorTree.Decorators;

public class MyGuard : NPCBrainController
{
    [SerializeField] private float _chaseSpeed = 6f;
    [SerializeField] private float _patrolSpeed = 3f;
    [SerializeField] private float _maxChaseDistance = 25f;
    
    protected override void Awake()
    {
        base.Awake();
        Blackboard.Set("homePosition", transform.position);
        
        OnTargetAcquired += target => {
            Blackboard.Set("target", target);
            Debug.Log($"Spotted: {target.name}");
        };
        
        OnTargetLost += target => {
            Blackboard.SetWithTTL("lastKnownPosition", 
                target.transform.position, 15f);
            Blackboard.Remove("target");
        };
    }
    
    protected override BTNode CreateBehaviorTree()
    {
        return new Selector(
            // Priority 1: Chase visible target
            CreateChaseBehavior(),
            // Priority 2: Investigate last known position
            CreateInvestigateBehavior(),
            // Priority 3: Return if far from home
            CreateReturnBehavior(),
            // Priority 4: Normal patrol
            CreatePatrolBehavior()
        );
    }
    
    private BTNode CreateChaseBehavior()
    {
        return new Sequence(
            new CheckBlackboard("target"),
            new CheckDistance(
                brain => brain.transform.position,
                brain => brain.Blackboard.Get<GameObject>("target")?.transform.position ?? brain.transform.position,
                _maxChaseDistance,
                CheckDistance.ComparisonType.LessThan
            ),
            new MoveTo(
                () => Blackboard.Get<GameObject>("target")?.transform.position ?? transform.position,
                1.5f,
                _chaseSpeed
            )
        );
    }
    
    private BTNode CreateInvestigateBehavior()
    {
        return new Sequence(
            new CheckBlackboard("lastKnownPosition"),
            new MoveTo(
                () => Blackboard.Get<Vector3>("lastKnownPosition"),
                1f,
                _patrolSpeed
            ),
            new Wait(3f),
            new ClearBlackboardKey("lastKnownPosition")
        );
    }
    
    private BTNode CreateReturnBehavior()
    {
        return new Sequence(
            new CheckDistance(
                brain => brain.transform.position,
                brain => brain.Blackboard.Get<Vector3>("homePosition"),
                15f,
                CheckDistance.ComparisonType.GreaterThan
            ),
            new MoveTo(
                () => Blackboard.Get<Vector3>("homePosition"),
                2f,
                _patrolSpeed
            )
        );
    }
    
    private BTNode CreatePatrolBehavior()
    {
        return new Sequence(
            new MoveTo(() => GetCurrentWaypoint(), 0.5f, _patrolSpeed, 30f),
            new Wait(2f),
            new AdvanceWaypoint()
        );
    }
}
```

[Back to Index](index.md) | [Behavior Trees](tutorials/behavior-trees.md)
