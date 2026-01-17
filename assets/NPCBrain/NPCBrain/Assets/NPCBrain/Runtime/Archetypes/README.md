# NPCBrain Archetypes

Ready-to-use NPC controller classes that demonstrate common AI patterns.

## Available Archetypes

### PatrolNPC

Simple patrol behavior - follows waypoints in a loop.

**Setup:**
1. Add `PatrolNPC` component to a GameObject
2. Create waypoint GameObjects (empty objects work fine)
3. Add `WaypointPath` component to a parent object
4. Assign waypoints to the WaypointPath
5. Assign the WaypointPath to the PatrolNPC inspector field
6. Press Play!

**Settings:**
- `Patrol Speed` - Movement speed
- `Arrival Distance` - How close to waypoint before considered arrived
- `Waypoint Wait Time` - Pause duration at each waypoint
- `Wait Time Variation` - Random +/- added to wait time

### GuardNPC

Advanced guard behavior with states:
1. **Patrol** - Follow waypoints (default)
2. **Chase** - Pursue visible targets
3. **Investigate** - Check last known target position
4. **Return** - Go back to patrol route

**Setup:**
1. Add `GuardNPC` component to a GameObject
2. Add `SightSensor` component for target detection
3. Set up waypoints (same as PatrolNPC)
4. Assign a target tag in SightSensor (default: "Player")
5. Press Play!

**Settings:**
- `Chase Speed` - Speed when pursuing targets
- `Patrol Speed` - Speed during normal patrol
- `Investigate Speed` - Speed when checking positions
- `Chase Arrival Distance` - Stop distance from target
- `Investigate Time` - How long to search an area
- `Max Chase Distance` - Give up chase beyond this
- `Alert Decay Rate` - How fast alert level drops

**Blackboard Keys:**
- `target` - Current chase target (GameObject)
- `lastKnownPosition` - Where target was last seen (Vector3)
- `homePosition` - Starting position (Vector3)
- `alertLevel` - Current alert 0-1 (float)

## Creating Custom Archetypes

Inherit from `NPCBrainController` and override `CreateBehaviorTree()`:

```csharp
using NPCBrain;
using NPCBrain.BehaviorTree;
using NPCBrain.BehaviorTree.Composites;
using NPCBrain.BehaviorTree.Actions;

public class MyCustomNPC : NPCBrainController
{
    protected override BTNode CreateBehaviorTree()
    {
        return new Selector(
            // Priority behaviors go here
            new Sequence(
                // Conditions and actions
            ),
            // Fallback behavior
            new Wait(1f)
        );
    }
}
```

## Tips

- Use `Selector` for priority-based decisions (first success wins)
- Use `Sequence` for step-by-step tasks (all must succeed)
- Use `UtilitySelector` for score-based decisions
- Store shared data in `Blackboard`
- Use `CheckBlackboard` conditions to gate behaviors
- Add `SightSensor` for target detection
- Subscribe to `OnTargetAcquired`/`OnTargetLost` events
