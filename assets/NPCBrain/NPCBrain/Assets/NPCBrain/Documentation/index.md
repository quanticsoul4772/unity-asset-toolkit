# NPCBrain Documentation

**Version 1.0** | Unity 2022.3 LTS+ | Full Source Code Included

NPCBrain is an AI toolkit that combines Behavior Trees, Utility AI, and Perception Systems for NPC development in Unity.

## Documentation Overview

### Getting Started
- [Getting Started Guide](getting-started.md) - Installation, setup, and your first NPC
- [Quick Reference](quick-reference.md) - Cheat sheet for common operations

### Tutorials
- [Behavior Trees](tutorials/behavior-trees.md) - Composites, decorators, and actions
- [Utility AI](tutorials/utility-ai.md) - Score-based decision making
- [Perception System](tutorials/perception.md) - Vision, hearing, and memory
- [Criticality System](tutorials/criticality.md) - Adaptive exploration vs exploitation
- [Creating Custom NPCs](tutorials/custom-npcs.md) - Building NPC archetypes

### API Reference
- [Core API](api/core.md) - NPCBrainController, Blackboard, WaypointPath
- [Behavior Tree API](api/behavior-tree.md) - Nodes, composites, decorators, actions
- [Utility AI API](api/utility-ai.md) - Actions, considerations, response curves
- [Perception API](api/perception.md) - Sensors, memory, sound system
- [Archetypes API](api/archetypes.md) - Built-in NPC types

### Examples
- [Cookbook](cookbook.md) - Common patterns and solutions
- [Demo Scenes](../Demo/README.md) - Demonstrations

## Architecture Overview

```
NPCBrainController
+-- Blackboard (Key-Value store)
+-- Perception (Sensors)
+-- Criticality (Temperature/Entropy)
+-- Behavior Tree
    +-- Composites
    +-- Decorators
    +-- Actions
    +-- Conditions
```

### Core Components

| Component | Purpose |
|-----------|--------|
| **NPCBrainController** | Main MonoBehaviour that drives NPC AI |
| **Blackboard** | Key-value data store for sharing information |
| **Behavior Tree** | Hierarchical decision-making structure |
| **Utility AI** | Score-based action selection |
| **Perception** | Sight and hearing sensors |
| **Criticality** | Adaptive exploration/exploitation control |

## Quick Start

### 1. Create a Simple NPC

```csharp
using NPCBrain;
using NPCBrain.BehaviorTree;
using NPCBrain.BehaviorTree.Composites;
using NPCBrain.BehaviorTree.Actions;

public class SimplePatroller : NPCBrainController
{
    [SerializeField] private float _speed = 3f;
    
    protected override BTNode CreateBehaviorTree()
    {
        return new Sequence(
            new MoveTo(() => GetCurrentWaypoint(), 0.5f, _speed),
            new Wait(2f),
            new AdvanceWaypoint()
        );
    }
}
```

### 2. Add to Scene

1. Create a Capsule GameObject
2. Add your `SimplePatroller` component
3. Add a `WaypointPath` component
4. Create waypoint child GameObjects
5. Press Play!

### 3. Use Built-in Archetypes

Available archetypes (add component, no code needed):
- PatrolNPC: Waypoint patrol
- GuardNPC: Chase, investigate, patrol
- HearingGuardNPC: Responds to sounds
- CopNPC: Arrests, chases, investigates
- RobberNPC: Steals, evades, escapes
- UtilityNPC: General utility AI demo

## Namespace Reference

| Namespace | Contents |
|-----------|----------|
| `NPCBrain` | Core classes |
| `NPCBrain.BehaviorTree` | BTNode, NodeStatus |
| `NPCBrain.BehaviorTree.Actions` | MoveTo, Wait, etc. |
| `NPCBrain.BehaviorTree.Composites` | Selector, Sequence, UtilitySelector |
| `NPCBrain.BehaviorTree.Conditions` | CheckBlackboard, CheckDistance |
| `NPCBrain.BehaviorTree.Decorators` | Inverter, Repeater, Cooldown |
| `NPCBrain.Perception` | SightSensor, HearingSensor, Memory |
| `NPCBrain.UtilityAI` | UtilityAction, Consideration, Curves |
| `NPCBrain.Criticality` | CriticalityController |
| `NPCBrain.Archetypes` | Built-in NPC types |
| `NPCBrain.Components` | LootPoint, EscapeZone, CoverPoint |

## Features

### Behavior Trees
- Composable, hierarchical decision making
- Selector (OR), Sequence (AND), Parallel execution
- Decorators: Inverter, Repeater, Cooldown, Succeeder
- Actions: MoveTo, Wait, LookAt, SetBlackboard

### Utility AI
- Score-based action selection with softmax
- Multiple consideration types
- Response curves for tuning
- Make-up value compensation

### Perception
- Vision cone with line-of-sight raycasting
- 3D sound detection with distance falloff
- Target memory with confidence decay
- Tag-based and layer-based filtering

### Criticality System
- Entropy-based behavior analysis
- Automatic temperature adjustment
- Prevents repetitive behavior patterns

### Debug Tools
- Real-time NPC inspection window
- Scene gizmos for vision cones
- Pause/Step/Resume controls
- Per-category logging system

## Menu Locations

- Demo Scenes: Window > NPCBrain > Create Demo Scenes
- Debug Window: Window > NPCBrain > Debug Window
