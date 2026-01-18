# NPCBrain - Unity AI Toolkit

A comprehensive AI toolkit for Unity combining **Behavior Trees**, **Utility AI**, **Perception**, and a unique **Criticality System** for adaptive NPC behavior.

## Features

### Behavior Trees
- **Composites**: Selector, Sequence, Parallel, UtilitySelector
- **Decorators**: Inverter, Repeater, Cooldown, Succeeder
- **Actions**: MoveTo, Wait, SetBlackboard, AdvanceWaypoint, ClearBlackboardKey, Log, LookAt
- **Conditions**: CheckBlackboard, CheckDistance, CheckTargetVisible

### Utility AI
- Score-based action selection
- Multiple response curves (Linear, Exponential, Step)
- Considerations with normalization
- Blackboard-driven considerations

### Perception System
- **SightSensor**: FOV-based vision with raycasting
- **Memory**: Target tracking with decay over time
- **TargetSelector**: Priority-based target scoring

### Criticality System
- Entropy-based behavior adaptation
- Temperature controls exploration vs exploitation
- Inertia affects action persistence
- Automatic variety enforcement

### Development Tools
- Debug window with real-time NPC monitoring
- Vision cone gizmos
- Demo scenes for each feature

## Quick Start

```csharp
using NPCBrain;
using NPCBrain.BehaviorTree;
using NPCBrain.BehaviorTree.Composites;
using NPCBrain.BehaviorTree.Actions;

public class MyNPC : NPCBrainController
{
    protected override BTNode CreateBehaviorTree()
    {
        return new Sequence(
            new MoveTo(() => GetCurrentWaypoint(), 0.5f),
            new Wait(1f),
            new AdvanceWaypoint()
        );
    }
}
```

## Installation

1. Import the NPCBrain package into your Unity project
2. Add `NPCBrainController` (or a subclass) to your NPC GameObject
3. Optionally add `SightSensor` for perception
4. Optionally add `WaypointPath` for patrol behaviors

## Demo Scenes

Access demo scenes via the Unity menu:
- **NPCBrain > Open Guard Demo** - Guards patrol and chase detected players
- **NPCBrain > Open Patrol Demo** - NPCs follow waypoint paths
- **NPCBrain > Open Utility Demo** - Utility AI with Criticality system

## Architecture

```
NPCBrainController
├── Blackboard (shared data store)
├── BehaviorTree (decision making)
├── Perception (SightSensor)
│   ├── Memory (target tracking)
│   └── TargetSelector (priority scoring)
└── Criticality (adaptive behavior)
    ├── Temperature (exploration vs exploitation)
    ├── Inertia (action persistence)
    └── Entropy (behavior variety)
```

## Requirements

- Unity 6000.0 or later
- Input System package (for demos)

## Documentation

- [Getting Started Guide](GETTING_STARTED.md)
- [API Reference](API.md)

## Support

For issues and feature requests, please contact the developer through the Unity Asset Store.

## License

This asset is licensed for use according to the Unity Asset Store EULA.
