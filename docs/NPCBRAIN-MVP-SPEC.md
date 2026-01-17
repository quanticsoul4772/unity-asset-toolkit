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
| Composites | Selector, Sequence, Parallel, UtilitySelector |
| Decorators | Inverter, Repeater, Cooldown, Succeeder |
| Conditions | CheckBlackboard, CheckDistance, CheckTargetVisible |
| Actions | MoveTo, Wait, SetBlackboard, Log, LookAt |

### 2. Utility AI
Score-based action selection for organic behavior.

**Components:**
- UtilityAction with Considerations
- 3 Response Curves: Linear, Exponential, Step
- Softmax selection with temperature (controlled by Criticality)

**Integration:** `UtilitySelector` is a BT composite node that scores children and picks the best one. This allows mixing structured BT logic with fuzzy Utility decisions.

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
**Integration:** Temperature feeds into UtilitySelector's softmax. Higher temp = more exploration, lower = more exploitation.

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
├── Demo/
│   ├── GuardDemo.unity
│   └── PatrolDemo.unity
└── Tests/
    ├── TestScene.unity
    ├── Editor/
    │   ├── BlackboardTests.cs
    │   ├── BTNodeTests.cs
    │   ├── UtilityAITests.cs
    │   ├── CriticalityTests.cs
    │   └── PerceptionTests.cs
    └── Runtime/
        ├── BehaviorTreeIntegrationTests.cs
        └── PerceptionIntegrationTests.cs
```

---

## Development Phases

### Week 1: Core Framework
- [ ] Project structure + assembly definitions (assets/NPCBrain/)
- [ ] TestScene.unity (validation sandbox - create first!)
- [ ] NPCBrain component (see class skeleton below)
- [ ] Blackboard with events
- [ ] WaypointPath component
- [ ] BTNode base + Selector, Sequence
- [ ] Basic conditions: CheckBlackboard (needed to test Selector fallback)
- [ ] Basic actions: MoveTo, Wait
- [ ] Unit tests: BlackboardTests, BTNodeTests (Selector, Sequence, CheckBlackboard)
- [ ] Validate in TestScene: NPC patrols waypoints, Selector fallback works

### Week 2: Behavior Trees + Perception
- [ ] Remaining BT nodes
- [ ] SightSensor with FOV
- [ ] Memory system
- [ ] TargetSelector
- [ ] Vision cone gizmo
- [ ] Unit tests: BTNodeTests (decorators, conditions, actions), PerceptionTests
- [ ] Integration tests: PerceptionIntegrationTests
- [ ] Validate in TestScene: NPC detects player, chases, loses sight

### Week 3: Utility AI + Criticality
- [ ] UtilityBrain + actions
- [ ] 3 response curves
- [ ] CriticalityController (entropy -> temp/inertia)
- [ ] Integration with Utility AI softmax
- [ ] Unit tests: UtilityAITests, CriticalityTests
- [ ] Integration tests: BehaviorTreeIntegrationTests
- [ ] Validate in TestScene: NPC behavior varies naturally over time

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

1. 5-minute setup for new users
2. BT + Utility AI hybrid works
3. Sight perception detects targets
4. Criticality auto-tunes behavior
5. 2 ready-to-use archetypes
6. Basic debug visualization

---

---

## Implementation Notes

Critical decisions and details to avoid blocking during development.

### 1. BTNode Architecture Decision

**Decision:** Plain C# classes (not ScriptableObjects)

- Trees defined in code, not serialized assets
- Simpler memory model, no Unity overhead
- Visual editor deferred to v2.0

### 2. User Workflow: Archetype Pattern

Users create NPCs by subclassing NPCBrain and overriding `CreateBehaviorTree()`:

```csharp
public class GuardNPC : NPCBrain
{
    protected override BTNode CreateBehaviorTree()
    {
        return new Selector(
            new Sequence(  // Chase if target visible
                new CheckTargetVisible(),
                new MoveTo(() => Blackboard.Get<Vector3>("targetPosition"))
            ),
            new Sequence(  // Patrol otherwise
                new MoveTo(() => GetNextWaypoint()),
                new Wait(2f)
            )
        );
    }
}
```

### 3. Execution Loop (Tick Order)

```
Update() called every frame:
|
|--1. Perception.Tick()        // Update visible targets, memory decay
|      +-- Fires: OnTargetAcquired, OnTargetLost
|
|--2. Criticality.Update()     // Compute entropy, adjust temp/inertia
|
|--3. BehaviorTree.Tick()      // BT executes, UtilitySelector uses Criticality.Temperature
|
+--4. Action execution         // MoveTo updates position, etc.
```

**Note:** There is no "BT vs Utility" mode switch. BT is always the decision structure. UtilitySelector nodes within the BT use Utility AI scoring. Criticality's temperature affects UtilitySelector's softmax.

**Tick Rate:** Every frame by default. Optional: `[SerializeField] float _tickInterval = 0f;`

### 4. Assembly Definitions

| Assembly | Purpose | References |
|----------|---------|------------|
| `NPCBrain.Runtime` | Core systems | Unity.InputSystem (optional) |
| `NPCBrain.Editor` | Debug window, gizmos | NPCBrain.Runtime |
| `NPCBrain.Demo` | Demo scripts | NPCBrain.Runtime |
| `NPCBrain.Tests.Editor` | EditMode unit tests | NPCBrain.Runtime, NUnit |
| `NPCBrain.Tests.Runtime` | PlayMode integration tests | NPCBrain.Runtime, NUnit |

### 5. Namespace Conventions

```csharp
namespace NPCBrain { }                    // Core: NPCBrain, Blackboard
namespace NPCBrain.BehaviorTree { }       // BTNode, Selector, Sequence, etc.
namespace NPCBrain.UtilityAI { }          // UtilityBrain, Consideration, Curves
namespace NPCBrain.Perception { }         // SightSensor, Memory, TargetSelector
namespace NPCBrain.Criticality { }        // CriticalityController, Telemetry
```

### 6. Movement Integration

`MoveTo` action uses this fallback chain:

```csharp
public class MoveTo : BTNode
{
    public override NodeStatus Tick(NPCBrain brain)
    {
        // Priority order:
        // 1. EasyPathAgent (if present)
        // 2. NavMeshAgent (if present)  
        // 3. Direct transform movement (always works)
        
        var agent = brain.GetComponent<EasyPathAgent>();
        if (agent != null) return MoveViaEasyPath(agent);
        
        var nav = brain.GetComponent<NavMeshAgent>();
        if (nav != null) return MoveViaNavMesh(nav);
        
        return MoveDirectly(brain.transform);
    }
}
```

### 7. Events

```csharp
public class NPCBrain : MonoBehaviour
{
    // Perception events
    public event Action<GameObject> OnTargetAcquired;
    public event Action<GameObject> OnTargetLost;
    
    // State events  
    public event Action<string> OnStateChanged;
    
    // Blackboard events
    public event Action<string, object> OnBlackboardChanged;
}
```

### 8. Edge Cases

| Situation | Behavior |
|-----------|----------|
| No targets visible | `SightSensor.GetVisibleTargets()` returns empty list |
| Blackboard key missing | `Get<T>(key, default)` returns default value |
| Destination unreachable | `MoveTo` returns `Failure` after timeout |
| Null target in Blackboard | Conditions check for null, return `Failure` |
| No waypoints assigned | `WaypointPath.GetNext()` returns current position |
| BT node throws exception | Caught, logged, returns `Failure` |

### 9. Performance Targets

| Metric | Target |
|--------|--------|
| NPCs at 60 FPS | 100+ |
| Per-NPC tick cost | < 0.1ms |
| Memory per NPC | < 1KB (excluding Unity overhead) |
| Perception raycasts | Max 3 per NPC per frame |

### 10. Testing Strategy

**Unit Tests (EditMode)** - Test logic without GameObjects:
- Blackboard: Get/Set, missing keys, type safety, events
- BTNode: Selector/Sequence logic, decorator behavior, status returns
- UtilityAI: Score calculation, curve responses, softmax selection
- Criticality: Entropy calculation, temperature/inertia adjustment
- Perception: Memory decay math, target priority scoring

**Integration Tests (PlayMode)** - Test with actual GameObjects:
- BT + NPC: Full behavior tree execution over multiple frames
- Perception: Raycast detection, target tracking, memory updates
- Full loop: Perception -> Decision -> Action pipeline

**Manual Testing (TestScene.unity):**
- Visual validation of NPC behavior
- Gizmo rendering
- Edge cases that are hard to automate

**Run tests:** `scripts/unity-cli.ps1 -Action test`

### 11. Unity Compatibility

- **Minimum:** Unity 2021.3 LTS
- **Target:** Unity 6 (6000.x)
- **Tested:** Unity 6000.3.4f1

### 12. Project Location

**Path:** `assets/NPCBrain/` (separate Unity project, not inside EasyPath)

This keeps NPCBrain independent and allows it to optionally reference EasyPath/SwarmAI without hard dependencies.

### 13. NPCBrain Class Skeleton

```csharp
namespace NPCBrain
{
    public class NPCBrain : MonoBehaviour
    {
        // Subsystems
        public Blackboard Blackboard { get; private set; }
        public SightSensor Perception { get; private set; }
        public CriticalityController Criticality { get; private set; }
        
        // BT
        private BTNode _behaviorTree;
        private NodeStatus _lastStatus;
        
        // Config
        [SerializeField] private float _tickInterval = 0f;
        [SerializeField] private WaypointPath _waypointPath;
        
        // Events
        public event Action<GameObject> OnTargetAcquired;
        public event Action<GameObject> OnTargetLost;
        public event Action<string> OnStateChanged;
        
        protected virtual void Awake()
        {
            Blackboard = new Blackboard();
            Perception = GetComponent<SightSensor>();
            Criticality = new CriticalityController();
            _behaviorTree = CreateBehaviorTree();
        }
        
        protected virtual BTNode CreateBehaviorTree()
        {
            // Override in subclasses (archetypes)
            return null;
        }
        
        private void Update()
        {
            // 1. Perception
            Perception?.Tick(this);
            
            // 2. Criticality
            Criticality.Update();
            
            // 3. BT Decision
            if (_behaviorTree != null)
            {
                _lastStatus = _behaviorTree.Tick(this);
            }
        }
        
        // Helpers for archetypes
        public Vector3 GetNextWaypoint() => _waypointPath?.GetNext() ?? transform.position;
    }
}
```

### 14. Key Interface Clarifications

**MoveTo constructor:**
```csharp
public class MoveTo : BTNode
{
    private readonly Func<Vector3> _targetGetter;
    private readonly float _arrivalDistance = 0.5f;
    private readonly float _moveSpeed = 5f;
    
    public MoveTo(Func<Vector3> targetGetter, float arrivalDistance = 0.5f)
    {
        _targetGetter = targetGetter;
        _arrivalDistance = arrivalDistance;
    }
}
```

**ResponseCurve:** Abstract base class with Linear, Exponential, Step subclasses:
```csharp
public abstract class ResponseCurve
{
    public abstract float Evaluate(float input); // input 0-1, output 0-1
}

public class LinearCurve : ResponseCurve { ... }
public class ExponentialCurve : ResponseCurve { ... }
public class StepCurve : ResponseCurve { ... }
```

**WaypointPath:** MonoBehaviour component with serialized waypoint list:
```csharp
public class WaypointPath : MonoBehaviour
{
    [SerializeField] private List<Transform> _waypoints;
    private int _currentIndex = 0;
    
    public Vector3 GetNext() { ... }
    public Vector3 GetCurrent() => _waypoints[_currentIndex].position;
    public void Advance() => _currentIndex = (_currentIndex + 1) % _waypoints.Count;
}
```

---

## Pre-Coding Checklist

- [x] Architecture decisions documented
- [x] User workflow defined (archetype pattern)
- [x] Tick order specified
- [x] Assembly definitions planned
- [x] Namespaces defined
- [x] Integration pattern for movement
- [x] Events designed
- [x] Edge cases handled
- [x] Performance targets set

**Status: Ready to code**

---

## Next Step

**Start coding Week 1: Core Framework**
