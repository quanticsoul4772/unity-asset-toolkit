# NPCBrain Design Document

**Version:** 1.1  
**Status:** Planning  
**Last Updated:** January 2026

## Overview

NPCBrain is an all-in-one AI toolkit that combines decision-making systems (Behavior Trees, Utility AI), perception (sight, hearing, memory), and seamless integration with EasyPath and SwarmAI.

**Target:** Intermediate Unity developers building games with intelligent NPCs  
**Price:** $60  
**Timeline:** 4-6 weeks

---

## Research Summary

### Behavior Tree Best Practices (2025-2026)

| Pattern | Description | Implementation |
|---------|-------------|----------------|
| **ScriptableObject Trees** | Save nodes as assets for reusability | BTNodeAsset base class |
| **Blackboard System** | Shared data storage between nodes | Generic dictionary with type safety |
| **Event-Driven Ticks** | Avoid full tree traversal every frame | Tick only when state changes |
| **Conditional Aborts** | Interrupt running nodes when conditions change | AbortType enum (None, Self, LowerPriority, Both) |
| **Status Returns** | Running/Success/Failure for flow control | NodeStatus enum |
| **Node Pooling** | Reduce GC allocations | Object pool pattern from SwarmAI |

### Utility AI Best Practices

| Concept | Description | Example |
|---------|-------------|---------|
| **Response Curves** | Map inputs to 0-1 scores | Linear, Exponential, Logistic, Step |
| **Considerations** | Individual scoring factors | Health, Distance, Ammo, Threat |
| **Action Scoring** | Multiply considerations for final score | Attack = 0.8 * health * 1.0 * inRange |
| **Normalization** | All inputs scaled to 0-1 | Distance: 1 - (dist/maxDist) |
| **Cheap Checks First** | Evaluate fast considerations first | HasAmmo before CalculateDistance |

### Perception System Best Practices

| Technique | Purpose | Performance |
|-----------|---------|-------------|
| **Layered Checks** | Distance → Cone → Raycast | Fastest first |
| **Fuzzy Detection** | Percentage-based, not binary | More realistic |
| **Memory Decay** | Forget targets over time | 10-30 second default |
| **Sound Spheres** | Radius-based hearing | Use SpatialHash |
| **Vision Cones** | Field of view detection | Dot product + raycast |

---

## Market Analysis

### Competitors
| Asset | Price | Strengths | Weaknesses |
|-------|-------|-----------|------------|
| Behavior Designer | $80 | Visual editor, many tasks, DOTS support | Complex, steep learning curve |
| NodeCanvas | $75 | BT + FSM + Dialogue, full source | Overkill for simple games |
| Emerald AI | $50 | Combat-focused, animation viewer | Limited decision systems |
| RAIN AI | Free | Was comprehensive | Discontinued |

### What Customers Want (Priority Order)

| Must-Have | High Priority | Nice-to-Have |
|-----------|---------------|--------------|
| Blackboard system | Perception (sight/hearing) | Visual BT editor |
| Pre-built BT nodes | Utility AI hybrid | Animation viewer |
| Runtime visualizer | Integration w/ pathfinding | Multiplayer support |
| Full source code | Zero allocations | Cover system |
| Good documentation | Event system | GOAP (v2.0) |

### Our Differentiators
1. **Only AI asset integrating pathfinding + steering + decisions** (EasyPath + SwarmAI + NPCBrain)
2. **Two AI paradigms** - Behavior Trees for structure, Utility AI for organic behavior
3. **4 ready-to-use archetypes** - Guard, Patrol, Civilian, Enemy
4. **Code-first with runtime visualization** - Easy to learn, debug, customize
5. **Performance-focused** - Same architecture patterns as SwarmAI (Jobs/Burst ready)

---

## Reusable Patterns from SwarmAI

| SwarmAI Pattern | NPCBrain Usage |
|-----------------|----------------|
| `SpatialHash<T>` | PerceptionSystem target detection |
| `AgentState` lifecycle | BTNode OnEnter/OnExit/Execute |
| `SwarmMessage` | Inter-NPC communication events |
| `IBehavior` interface | BTNode design (Name, IsActive, Weight) |
| `BehaviorBase` helpers | Seek/Flee as BTAction utilities |
| Object pooling | BTNode allocation optimization |

---

## Architecture

```
NPCBrain/
├── Runtime/
│   ├── Core/
│   │   ├── NPCBrain.cs              # Main component (attach to NPC)
│   │   ├── Blackboard.cs            # Shared knowledge storage
│   │   ├── NPCBrainSettings.cs      # ScriptableObject settings
│   │   ├── WaypointPath.cs          # Patrol waypoints (NEW)
│   │   └── NPCEvents.cs             # Event definitions (NEW)
│   │
│   ├── BehaviorTree/
│   │   ├── Core/
│   │   │   ├── BehaviorTree.cs      # Tree executor
│   │   │   ├── BTNode.cs            # Base node class
│   │   │   ├── NodeStatus.cs        # Running/Success/Failure
│   │   │   └── AbortType.cs         # Conditional abort types (NEW)
│   │   ├── Composites/
│   │   │   ├── Selector.cs          # Run until one succeeds
│   │   │   ├── Sequence.cs          # Run until one fails
│   │   │   ├── Parallel.cs          # Run all simultaneously
│   │   │   └── RandomSelector.cs    # Pick random child
│   │   ├── Decorators/
│   │   │   ├── Inverter.cs          # Flip success/failure
│   │   │   ├── Repeater.cs          # Repeat N times
│   │   │   ├── Succeeder.cs         # Always succeed
│   │   │   ├── UntilFail.cs         # Repeat until failure
│   │   │   ├── Cooldown.cs          # Rate limit execution
│   │   │   └── ConditionalAbort.cs  # Abort on condition change (NEW)
│   │   ├── Conditions/              # NEW folder
│   │   │   ├── CheckBlackboard.cs   # Check blackboard value
│   │   │   ├── CheckDistance.cs     # Distance comparison
│   │   │   ├── CheckHealth.cs       # Health threshold
│   │   │   ├── CheckTargetVisible.cs# Perception check
│   │   │   └── CheckHeardSound.cs   # Audio perception
│   │   └── Actions/
│   │       ├── MoveTo.cs            # Navigate to position
│   │       ├── MoveToWaypoint.cs    # Follow waypoint path (NEW)
│   │       ├── Wait.cs              # Wait for duration
│   │       ├── PlayAnimation.cs     # Trigger animation
│   │       ├── SetBlackboard.cs     # Store value
│   │       ├── Log.cs               # Debug logging
│   │       ├── SendMessage.cs       # Inter-NPC communication
│   │       ├── LookAt.cs            # Face target (NEW)
│   │       └── Investigate.cs       # Go to last known position (NEW)
│   │
│   ├── UtilityAI/
│   │   ├── Core/
│   │   │   ├── UtilityBrain.cs      # Action selector
│   │   │   ├── UtilityAction.cs     # Base action class
│   │   │   ├── Consideration.cs     # Scoring factor
│   │   │   └── ResponseCurve.cs     # Score mapping curves
│   │   ├── Curves/                  # NEW folder
│   │   │   ├── LinearCurve.cs
│   │   │   ├── ExponentialCurve.cs
│   │   │   ├── LogisticCurve.cs
│   │   │   ├── StepCurve.cs
│   │   │   └── BellCurve.cs
│   │   └── Actions/
│   │       ├── IdleAction.cs
│   │       ├── PatrolAction.cs
│   │       ├── ChaseAction.cs
│   │       ├── AttackAction.cs
│   │       ├── FleeAction.cs
│   │       ├── InvestigateAction.cs
│   │       ├── HealAction.cs        # NEW
│   │       └── TakeCoverAction.cs   # NEW
│   │
│   ├── Perception/
│   │   ├── PerceptionSystem.cs      # Main perception manager
│   │   ├── SightSensor.cs           # Vision cone detection
│   │   ├── HearingSensor.cs         # Sound detection
│   │   ├── ProximitySensor.cs       # Range-based detection
│   │   ├── Memory.cs                # Remember past detections
│   │   ├── TargetSelector.cs        # Priority-based targeting (NEW)
│   │   └── SoundEmitter.cs          # Emit sounds for AI to hear (NEW)
│   │
│   ├── Archetypes/                  # Pre-built NPC behaviors
│   │   ├── GuardNPC.cs              # Stand guard, investigate, alert
│   │   ├── PatrolNPC.cs             # Follow waypoints
│   │   ├── CivilianNPC.cs           # Wander, flee from danger
│   │   └── EnemyNPC.cs              # Chase, attack, search
│   │
│   └── Integration/
│       ├── EasyPathBridge.cs        # Pathfinding integration
│       ├── SwarmAIBridge.cs         # Steering behaviors integration
│       └── AnimatorBridge.cs        # Animation control (NEW)
│
├── Editor/
│   ├── Inspectors/
│   │   ├── NPCBrainEditor.cs        # Custom inspector
│   │   ├── BlackboardEditor.cs      # Blackboard visualization
│   │   └── WaypointPathEditor.cs    # Waypoint editing (NEW)
│   ├── Windows/
│   │   ├── NPCBrainDebugWindow.cs   # Runtime visualizer (NEW - CRITICAL)
│   │   └── BehaviorTreeWindow.cs    # Visual BT editor (v2.0 stretch goal)
│   └── Gizmos/
│       ├── PerceptionGizmos.cs      # Vision cones, hearing spheres
│       └── WaypointGizmos.cs        # Waypoint visualization
│
├── Demo/
│   ├── Scripts/
│   │   ├── DemoSetup.cs
│   │   ├── PlayerController.cs      # Simple player for demos
│   │   └── DemoUI.cs                # On-screen instructions
│   ├── Prefabs/
│   │   ├── GuardNPC.prefab
│   │   ├── PatrolNPC.prefab
│   │   ├── CivilianNPC.prefab
│   │   └── EnemyNPC.prefab
│   └── Scenes/
│       ├── PatrolDemo.unity         # Week 2 milestone
│       ├── GuardDemo.unity          # Week 3 milestone
│       ├── CombatDemo.unity         # Week 4 milestone
│       └── TownDemo.unity           # Week 5 milestone
│
├── Tests/
│   ├── Editor/
│   │   ├── BlackboardTests.cs
│   │   ├── BehaviorTreeTests.cs
│   │   ├── UtilityAITests.cs
│   │   └── PerceptionTests.cs
│   └── Runtime/
│       ├── NPCBrainPlayModeTests.cs
│       └── IntegrationTests.cs
│
└── Documentation/
    ├── README.md
    ├── GETTING-STARTED.md
    ├── BEHAVIOR-TREES.md
    ├── UTILITY-AI.md
    ├── PERCEPTION.md
    ├── ARCHETYPES.md               # NEW
    └── API-REFERENCE.md
```

---

## Core Systems

### 1. Blackboard (Knowledge System)

Shared data storage for NPC decision-making, inspired by industry standard implementations.

```csharp
public class Blackboard
{
    private Dictionary<string, object> _data = new();
    
    // Core operations
    public void Set<T>(string key, T value);
    public T Get<T>(string key, T defaultValue = default);
    public bool TryGet<T>(string key, out T value);
    public bool Has(string key);
    public void Remove(string key);
    public void Clear();
    
    // Events for reactive updates
    public event Action<string, object> OnValueChanged;
    
    // Common keys (constants)
    public static class Keys
    {
        public const string Target = "target";
        public const string LastKnownPosition = "lastKnownPosition";
        public const string AlertLevel = "alertLevel";
        public const string HomePosition = "homePosition";
        public const string PatrolWaypoints = "patrolWaypoints";
        public const string Health = "health";
        public const string Ammo = "ammo";
        public const string ThreatLevel = "threatLevel";
    }
}
```

### 2. Behavior Tree

Node-based decision tree with Running/Success/Failure states and conditional aborts.

```csharp
public enum NodeStatus { Running, Success, Failure }

public enum AbortType 
{ 
    None,           // Never abort
    Self,           // Abort this subtree when condition changes
    LowerPriority,  // Abort lower priority siblings
    Both            // Abort both self and lower priority
}

public abstract class BTNode
{
    public string Name { get; protected set; }
    public BTNode Parent { get; internal set; }
    public AbortType AbortType { get; set; } = AbortType.None;
    
    public abstract NodeStatus Tick(NPCBrain brain);
    public virtual void OnEnter(NPCBrain brain) { }
    public virtual void OnExit(NPCBrain brain) { }
    public virtual void OnAbort(NPCBrain brain) { }  // NEW: Handle interruption
}

// Example: Guard Behavior Tree with Conditional Aborts
var guardTree = new Selector(
    // High priority: React to visible enemy (aborts patrol if enemy appears)
    new ConditionalAbort(AbortType.LowerPriority, new CheckTargetVisible(),
        new Sequence(
            new CheckTargetVisible(),
            new Selector(
                new Sequence(new CheckInAttackRange(), new Attack()),
                new ChaseTarget()
            )
        )
    ),
    // Medium priority: Investigate sounds
    new Sequence(
        new CheckHeardSound(),
        new Investigate()
    ),
    // Low priority: Default patrol
    new Patrol()
);
```

### 3. Utility AI

Score-based action selection for organic, emergent behavior.

```csharp
public class UtilityAction
{
    public string Name;
    public float BasePriority = 1f;           // Multiplier for this action type
    public List<Consideration> Considerations;
    public float CooldownTime = 0f;           // Prevent spamming
    
    public float Score(NPCBrain brain)
    {
        if (IsOnCooldown) return 0f;
        
        float score = BasePriority;
        foreach (var c in Considerations)
        {
            float considerationScore = c.Evaluate(brain);
            if (considerationScore <= 0f) return 0f;  // Instant disqualification
            score *= considerationScore;
        }
        
        // Compensation factor for multiple considerations
        // Prevents actions with many considerations from being unfairly penalized
        float modFactor = 1f - (1f / Considerations.Count);
        float makeUpValue = (1f - score) * modFactor;
        return score + (makeUpValue * score);
    }
}

public abstract class ResponseCurve
{
    public abstract float Evaluate(float input);  // Input: 0-1, Output: 0-1
}

// Built-in curves
public class LinearCurve : ResponseCurve { /* y = mx + b */ }
public class ExponentialCurve : ResponseCurve { /* y = x^exponent */ }
public class LogisticCurve : ResponseCurve { /* S-curve for thresholds */ }
public class StepCurve : ResponseCurve { /* Binary threshold */ }
public class BellCurve : ResponseCurve { /* Peak at center value */ }
```

### 4. Perception System

Modular sensors with memory and priority-based target selection.

```csharp
public class PerceptionSystem : MonoBehaviour
{
    public SightSensor Sight;
    public HearingSensor Hearing;
    public Memory Memory;
    public TargetSelector TargetSelector;
    
    // Aggregate all sensory input
    public List<PerceivedTarget> GetAllPerceivedTargets();
    public PerceivedTarget GetHighestPriorityTarget();
}

public class SightSensor
{
    public float ViewDistance = 20f;
    public float ViewAngle = 120f;
    public float PeripheralAngle = 180f;      // Wider but less accurate
    public LayerMask TargetLayers;
    public LayerMask ObstacleLayers;
    public int RaycastsPerTarget = 3;         // Head, torso, feet
    
    // Returns visibility percentage (0-1) based on how exposed target is
    public float GetVisibility(GameObject target);
    public List<PerceivedTarget> GetVisibleTargets();
}

public class HearingSensor
{
    public float HearingRange = 15f;
    public float SuspicionThreshold = 0.3f;   // Min loudness to trigger investigation
    
    public void OnSoundEmitted(SoundEvent sound);
    public List<SoundEvent> GetRecentSounds(float withinSeconds = 5f);
}

public class Memory
{
    public float MemoryDuration = 10f;        // How long before forgetting
    public int MaxMemories = 10;              // Limit tracked targets
    
    public void Remember(GameObject target, Vector3 position, float confidence);
    public MemoryRecord GetMemory(GameObject target);
    public Vector3? GetLastKnownPosition(GameObject target);
    public void Forget(GameObject target);
}

public class TargetSelector
{
    // Scoring factors for target prioritization
    public float DistanceWeight = 1f;
    public float ThreatWeight = 2f;
    public float VisibilityWeight = 1.5f;
    public float HealthWeight = 0.5f;         // Prefer wounded targets
    
    public PerceivedTarget SelectBestTarget(List<PerceivedTarget> targets);
}
```

### 5. Event System (NEW)

Decoupled communication between NPC systems.

```csharp
public static class NPCEvents
{
    // Perception events
    public static event Action<NPCBrain, GameObject> OnTargetDetected;
    public static event Action<NPCBrain, GameObject> OnTargetLost;
    public static event Action<NPCBrain, SoundEvent> OnSoundHeard;
    
    // Combat events
    public static event Action<NPCBrain, float> OnDamageTaken;
    public static event Action<NPCBrain, GameObject> OnAttackStarted;
    public static event Action<NPCBrain> OnDeath;
    
    // State events
    public static event Action<NPCBrain, string> OnStateChanged;
    public static event Action<NPCBrain, string, object> OnBlackboardChanged;
}
```

---

## Development Phases (Detailed)

### Phase 1: Core Framework (Week 1)
**Milestone:** Simple patrol NPC works with basic movement

- [ ] Create folder structure and assembly definitions
- [ ] NPCBrain component with tick throttling
- [ ] Blackboard system with events
- [ ] NPCBrainSettings ScriptableObject
- [ ] WaypointPath component for patrols
- [ ] Basic unit tests for Blackboard
- [ ] Integration hooks (optional EasyPath/SwarmAI detection)

**Deliverable:** NPC that can store/retrieve data and follow waypoints

### Phase 2: Behavior Trees (Week 2)
**Milestone:** Patrol Demo complete with BT-driven behavior

- [ ] BTNode base class with lifecycle (Enter/Exit/Tick)
- [ ] NodeStatus enum and tree traversal
- [ ] Composite nodes: Selector, Sequence, Parallel, RandomSelector
- [ ] Decorator nodes: Inverter, Repeater, Cooldown, Succeeder, UntilFail
- [ ] Action nodes: MoveTo, MoveToWaypoint, Wait, Log, SetBlackboard
- [ ] Condition nodes: CheckBlackboard, CheckDistance
- [ ] Conditional aborts (Self, LowerPriority, Both)
- [ ] NPCBrainDebugWindow for runtime visualization
- [ ] Unit tests for all node types
- [ ] **Patrol Demo scene**

**Deliverable:** NPCs patrol waypoints, stop, look around, continue

### Phase 3: Perception System (Week 3)
**Milestone:** Guard Demo with vision cones and investigation

- [ ] PerceptionSystem component
- [ ] SightSensor with cone detection and raycasts
- [ ] HearingSensor with sound events
- [ ] SoundEmitter component (for player footsteps, gunshots)
- [ ] Memory system with decay
- [ ] TargetSelector with priority scoring
- [ ] Perception Gizmos (vision cones, hearing spheres)
- [ ] BT Conditions: CheckTargetVisible, CheckHeardSound
- [ ] BT Actions: Investigate, LookAt
- [ ] Unit tests for perception
- [ ] **Guard Demo scene**

**Deliverable:** Guards detect player in vision cone, investigate sounds, lose/remember targets

### Phase 4: Utility AI (Week 4)
**Milestone:** Combat Demo with dynamic decision-making

- [ ] UtilityBrain action selector
- [ ] Consideration system with response curves
- [ ] Response curves: Linear, Exponential, Logistic, Step, Bell
- [ ] Pre-built actions: Idle, Patrol, Chase, Attack, Flee, Heal, TakeCover
- [ ] Hybrid mode: BT structure with Utility AI action selection
- [ ] Action cooldowns and history
- [ ] Utility debugging in NPCBrainDebugWindow
- [ ] Unit tests for utility scoring
- [ ] **Combat Demo scene**

**Deliverable:** Enemies make tactical decisions (attack, retreat, heal) based on context

### Phase 5: Integration & Archetypes (Week 5)
**Milestone:** All 4 NPC archetypes working

- [ ] EasyPathBridge (optional pathfinding)
- [ ] SwarmAIBridge (optional steering behaviors)
- [ ] AnimatorBridge (animation parameters/triggers)
- [ ] GuardNPC archetype (patrol, investigate, chase, return)
- [ ] PatrolNPC archetype (waypoints, idle animations)
- [ ] CivilianNPC archetype (wander, flee, cower)
- [ ] EnemyNPC archetype (hunt, attack, retreat, search)
- [ ] Archetype prefabs
- [ ] **Town Demo scene** (multiple NPC types)

**Deliverable:** Drop-in NPC prefabs that work out of the box

### Phase 6: Polish & Documentation (Week 6)
**Milestone:** Asset Store ready

- [ ] Performance optimization (tick throttling, pooling)
- [ ] Custom inspectors for all components
- [ ] Complete NPCBrainDebugWindow
- [ ] Documentation: README, Getting Started, API Reference
- [ ] Documentation: Behavior Trees guide with examples
- [ ] Documentation: Utility AI guide with examples
- [ ] Documentation: Perception guide
- [ ] Documentation: Archetypes customization guide
- [ ] Final testing pass
- [ ] Asset Store screenshots and promotional materials

---

## Performance Considerations

| Technique | Purpose | Implementation |
|-----------|---------|----------------|
| **Tick Throttling** | Don't update every frame | Configurable tick rate (default: 10 ticks/sec) |
| **Staggered Updates** | Spread load across frames | Use agent ID % frameCount |
| **Spatial Partitioning** | Fast neighbor queries | Reuse SwarmAI's SpatialHash |
| **Object Pooling** | Reduce GC | Pool BT nodes, perception results |
| **Cheap Checks First** | Fail fast | Distance before raycast in perception |
| **Jobs/Burst Ready** | Future optimization | Data-oriented perception queries |

---

## Risk Analysis

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Users expect visual editor | High | Medium | Market as "code-first" + runtime visualizer, visual editor is v2.0 |
| Performance with 100+ NPCs | Medium | High | Tick throttling, staggered updates, benchmark tests |
| Integration complexity | Medium | Medium | Make EasyPath/SwarmAI optional, NPCBrain works standalone |
| Scope creep | High | High | Strict phase milestones, cut features for v2.0 |
| Documentation takes too long | Medium | Medium | Write docs as we build, not at the end |

---

## Future Expansion (v2.0+)

| Feature | Description | Price |
|---------|-------------|-------|
| **Visual BT Editor** | Node-based tree building in Unity | +$20 |
| **GOAP Planner** | Goal-oriented action planning | +$20 |
| **NavMesh Integration** | Unity NavMesh support | Included |
| **Multiplayer AI** | Network-synced decisions | +$15 |
| **Machine Learning** | Train behaviors from examples | +$25 |

---

## Success Criteria

1. ✅ **5-minute setup** - New users can have a working NPC in 5 minutes
2. ✅ **Flexible** - Supports both Behavior Trees and Utility AI
3. ✅ **Integrated** - Works seamlessly with EasyPath and SwarmAI
4. ✅ **Visual debugging** - Runtime visualizer shows NPC decision-making
5. ✅ **Performance** - Handle 100+ NPCs at 60 FPS
6. ✅ **Well-documented** - Clear examples for every feature
7. ✅ **4 Archetypes** - Guard, Patrol, Civilian, Enemy ready to use

---

## Next Steps

1. ☐ Create folder structure and assembly definitions
2. ☐ Implement Blackboard system with events
3. ☐ Implement BTNode base class and core composites
4. ☐ Create WaypointPath component
5. ☐ Build first demo (Patrol Demo)
