# NPCBrain

AI toolkit for Unity NPCs combining Behavior Trees, Utility AI, and Perception.

## Quick Start

1. Add `NPCBrainController` to your NPC GameObject
2. Create a subclass and override `CreateBehaviorTree()`
3. Add a `WaypointPath` component for patrol routes

## Example

```csharp
public class GuardNPC : NPCBrainController
{
    protected override BTNode CreateBehaviorTree()
    {
        return new Selector(
            new Sequence(
                new CheckBlackboard("hasTarget"),
                new MoveTo(() => Blackboard.Get<Vector3>("targetPosition"))
            ),
            new Sequence(
                new MoveTo(() => GetCurrentWaypoint()),
                new Wait(2f)
            )
        );
    }
}
```

## Folder Structure

- `Runtime/Core/` - NPCBrainController, Blackboard, WaypointPath
- `Runtime/BehaviorTree/` - BT nodes (Selector, Sequence, MoveTo, Wait, etc.)
- `Runtime/Perception/` - SightSensor, Memory, TargetSelector (Week 2)
- `Runtime/UtilityAI/` - Utility scoring system (Week 3)
- `Runtime/Criticality/` - Adaptive behavior tuning (Week 3)
- `Editor/` - Debug window and gizmos
- `Demo/` - Example scenes and scripts
- `Tests/` - Unit and integration tests

## Development Status

- [x] Week 1: Core Framework (BT, Blackboard, basic nodes)
- [ ] Week 2: Perception system
- [ ] Week 3: Utility AI + Criticality
- [ ] Week 4: Archetypes + Polish
