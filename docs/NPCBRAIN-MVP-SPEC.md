# NPCBrain MVP Specification

**Version:** 1.0  
**Status:** Ready to Build  
**Timeline:** 4 weeks

---

## What We're Building

An AI toolkit for Unity NPCs that combines Behavior Trees, Utility AI, and Perception. Ships with ready-to-use archetypes.

**Price:** $60  
**Target:** Intermediate Unity developers

---

## Core Systems (4 total)

### 1. Behavior Trees
Decision structure using nodes that return Running/Success/Failure.

**Nodes:**
| Type | Nodes |
|------|-------|
| Composites | Selector, Sequence, Parallel |
| Decorators | Inverter, Repeater, Cooldown, Succeeder |
| Conditions | CheckBlackboard, CheckDistance, CheckTargetVisible |
| Actions | MoveTo, Wait, SetBlackboard, Log, LookAt |

### 2. Utility AI
Score-based action selection for organic behavior.

**Components:**
- UtilityAction with Considerations
- 3 Response Curves: Linear, Exponential, Step
- Softmax selection with temperature

### 3. Perception (Sight Only)
Vision cone detection with memory.

**Components:**
- SightSensor (FOV, range, raycasts)
- Memory (decay over time, last known position)
- TargetSelector (priority scoring)

### 4. Criticality (Simplified)
Automatic behavior tuning - keeps NPCs balanced between predictable and random.

**Metrics:** Action entropy only  
**Knobs:** Temperature, Inertia  
**Integration:** Feeds into Utility AI softmax

---

## Blackboard

Shared key-value store for NPC knowledge.

```csharp
public class Blackboard
{
    public void Set<T>(string key, T value);
    public T Get<T>(string key, T defaultValue = default);
    public bool Has(string key);
}
```

**Standard Keys:** target, lastKnownPosition, alertLevel, health, homePosition

---

## Archetypes (2 total)

### GuardNPC
- Patrols waypoints
- Investigates when target spotted
- Chases and returns to post

### PatrolNPC
- Follows waypoint path
- Idle animations at waypoints
- Simple behavior tree

---

## Debug Tools

### Debug Window (Basic)
- NPC selector dropdown
- Current state display
- Blackboard viewer
- Pause/Step controls

### Scene Gizmos
- Vision cone (color by alert state)
- Waypoint path lines

---

## Architecture

```
NPCBrain/
├── Runtime/
│   ├── Core/
│   │   ├── NPCBrain.cs
│   │   ├── Blackboard.cs
│   │   └── WaypointPath.cs
│   ├── BehaviorTree/
│   │   ├── BTNode.cs, NodeStatus.cs
│   │   ├── Composites/ (Selector, Sequence, Parallel)
│   │   ├── Decorators/ (Inverter, Repeater, Cooldown, Succeeder)
│   │   ├── Conditions/ (CheckBlackboard, CheckDistance, CheckTargetVisible)
│   │   └── Actions/ (MoveTo, Wait, SetBlackboard, Log, LookAt)
│   ├── UtilityAI/
│   │   ├── UtilityBrain.cs, UtilityAction.cs
│   │   ├── Consideration.cs
│   │   └── Curves/ (Linear, Exponential, Step)
│   ├── Perception/
│   │   ├── SightSensor.cs
│   │   ├── Memory.cs
│   │   └── TargetSelector.cs
│   ├── Criticality/
│   │   ├── CriticalityController.cs (entropy + temp/inertia only)
│   │   └── ActionTelemetry.cs
│   └── Archetypes/
│       ├── GuardNPC.cs
│       └── PatrolNPC.cs
├── Editor/
│   ├── NPCBrainDebugWindow.cs
│   └── Gizmos/
│       └── VisionConeGizmo.cs
└── Demo/
    ├── GuardDemo.unity
    └── PatrolDemo.unity
```

---

## Development Phases

### Week 1: Core Framework
- [ ] NPCBrain component
- [ ] Blackboard with events
- [ ] WaypointPath
- [ ] BTNode base + Selector, Sequence
- [ ] Basic actions: MoveTo, Wait

### Week 2: Behavior Trees + Perception
- [ ] Remaining BT nodes
- [ ] SightSensor with FOV
- [ ] Memory system
- [ ] TargetSelector
- [ ] Vision cone gizmo

### Week 3: Utility AI + Criticality
- [ ] UtilityBrain + actions
- [ ] 3 response curves
- [ ] CriticalityController (entropy → temp/inertia)
- [ ] Integration with Utility AI softmax

### Week 4: Archetypes + Polish
- [ ] GuardNPC archetype
- [ ] PatrolNPC archetype
- [ ] Basic debug window
- [ ] Demo scenes
- [ ] Documentation

---

## Key Interfaces

```csharp
// Behavior Tree Node
public abstract class BTNode
{
    public abstract NodeStatus Tick(NPCBrain brain);
    public virtual void OnEnter(NPCBrain brain) { }
    public virtual void OnExit(NPCBrain brain) { }
}

// Utility Action
public class UtilityAction
{
    public string Name;
    public List<Consideration> Considerations;
    public float Score(NPCBrain brain);
    public abstract void Execute(NPCBrain brain);
}

// Consideration
public abstract class Consideration
{
    public ResponseCurve Curve;
    public abstract float Evaluate(NPCBrain brain); // Returns 0-1
}

// Sight Sensor
public class SightSensor
{
    public float ViewDistance = 20f;
    public float ViewAngle = 120f;
    public List<GameObject> GetVisibleTargets();
}

// Criticality (simplified)
public class CriticalityController
{
    public float Temperature { get; private set; } // 0.5 - 2.0
    public float Inertia { get; private set; }     // 0.0 - 1.0
    public void RecordAction(int actionId);
    public void Update(); // Adjusts temp/inertia based on entropy
}
```

---

## What's NOT in v1.0

Deferred to v2.0 (archived in docs/archive/):
- Emotional State System (PAD model)
- Influence Maps
- Context Steering
- Animation Rigging integration
- Hearing sensor
- Group coordination
- Debug timeline/recording
- Visual BT editor
- GOAP

---

## Success Criteria

1. ✅ 5-minute setup for new users
2. ✅ BT + Utility AI hybrid works
3. ✅ Sight perception detects targets
4. ✅ Criticality auto-tunes behavior
5. ✅ 2 ready-to-use archetypes
6. ✅ Basic debug visualization

---

## Next Step

**Start coding Week 1: Core Framework**
