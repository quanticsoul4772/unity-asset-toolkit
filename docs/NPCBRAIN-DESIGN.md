# NPCBrain Design Document

**Version:** 1.0  
**Status:** Planning  
**Last Updated:** January 2026

## Overview

NPCBrain is an all-in-one AI toolkit that combines decision-making systems (Behavior Trees, Utility AI), perception (sight, hearing, memory), and seamless integration with EasyPath and SwarmAI.

**Target:** Intermediate Unity developers building games with intelligent NPCs  
**Price:** $60  
**Timeline:** 4-6 weeks

## Market Analysis

### Competitors
| Asset | Price | Strengths | Weaknesses |
|-------|-------|-----------|------------|
| Behavior Designer | $80 | Visual editor, many tasks | Complex, steep learning curve |
| NodeCanvas | $75 | Behavior Trees + FSM | Overkill for simple games |
| RAIN AI | Free (discontinued) | Was comprehensive | No longer maintained |
| Emerald AI | $50 | Combat-focused | Limited decision systems |

### Our Differentiator
- **Integrated ecosystem** - Works seamlessly with EasyPath (pathfinding) and SwarmAI (steering)
- **Multiple AI paradigms** - Behavior Trees AND Utility AI (choose what fits)
- **Pre-built archetypes** - Ready-to-use NPC types (Guard, Patrol, Civilian, Enemy)
- **Simple API** - Get started in 5 minutes, not 5 hours
- **Full source code** - Learn and customize everything

---

## Architecture

```
NPCBrain/
├── Runtime/
│   ├── Core/
│   │   ├── NPCBrain.cs              # Main component (attach to NPC)
│   │   ├── Blackboard.cs            # Shared knowledge storage
│   │   └── NPCBrainSettings.cs      # ScriptableObject settings
│   │
│   ├── BehaviorTree/
│   │   ├── BehaviorTree.cs          # Tree executor
│   │   ├── BTNode.cs                # Base node class
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
│   │   │   └── Cooldown.cs          # Rate limit execution
│   │   └── Actions/
│   │       ├── MoveTo.cs            # Navigate to position
│   │       ├── Wait.cs              # Wait for duration
│   │       ├── PlayAnimation.cs     # Trigger animation
│   │       ├── SetBlackboard.cs     # Store value
│   │       ├── Log.cs               # Debug logging
│   │       └── SendMessage.cs       # Inter-NPC communication
│   │
│   ├── UtilityAI/
│   │   ├── UtilityBrain.cs          # Action selector
│   │   ├── Action.cs                # Base action class
│   │   ├── Consideration.cs         # Scoring factor
│   │   ├── ResponseCurve.cs         # Score mapping curves
│   │   └── Actions/
│   │       ├── IdleAction.cs
│   │       ├── PatrolAction.cs
│   │       ├── ChaseAction.cs
│   │       ├── AttackAction.cs
│   │       ├── FleeAction.cs
│   │       └── InvestigateAction.cs
│   │
│   ├── Perception/
│   │   ├── PerceptionSystem.cs      # Main perception manager
│   │   ├── SightSensor.cs           # Vision cone detection
│   │   ├── HearingSensor.cs         # Sound detection
│   │   ├── ProximitySensor.cs       # Range-based detection
│   │   └── Memory.cs                # Remember past detections
│   │
│   ├── Archetypes/                  # Pre-built NPC behaviors
│   │   ├── GuardNPC.cs              # Stand guard, investigate, alert
│   │   ├── PatrolNPC.cs             # Follow waypoints
│   │   ├── CivilianNPC.cs           # Wander, flee from danger
│   │   └── EnemyNPC.cs              # Chase, attack, search
│   │
│   └── Integration/
│       ├── EasyPathIntegration.cs   # Pathfinding bridge
│       └── SwarmAIIntegration.cs    # Steering bridge
│
├── Editor/
│   ├── NPCBrainEditor.cs            # Custom inspector
│   ├── BehaviorTreeWindow.cs        # Visual BT editor (stretch goal)
│   ├── BlackboardEditor.cs          # Blackboard visualization
│   └── PerceptionDebugger.cs        # Gizmo visualization
│
├── Demo/
│   ├── Scripts/
│   │   ├── DemoSetup.cs
│   │   └── PlayerController.cs      # Simple player for demos
│   └── Scenes/
│       ├── GuardDemo.unity          # Stealth game scenario
│       ├── PatrolDemo.unity         # Patrol and investigate
│       ├── CombatDemo.unity         # Combat AI showcase
│       └── TownDemo.unity           # Multiple NPC types
│
└── Documentation/
    ├── README.md
    ├── GETTING-STARTED.md
    ├── BEHAVIOR-TREES.md
    ├── UTILITY-AI.md
    ├── PERCEPTION.md
    └── API-REFERENCE.md
```

---

## Core Systems

### 1. Blackboard (Knowledge System)

Shared data storage for NPC decision-making.

```csharp
public class Blackboard
{
    // Store any type of data by key
    public void Set<T>(string key, T value);
    public T Get<T>(string key, T defaultValue = default);
    public bool Has(string key);
    public void Remove(string key);
    
    // Common keys (constants)
    public static class Keys
    {
        public const string Target = "target";
        public const string LastKnownPosition = "lastKnownPosition";
        public const string AlertLevel = "alertLevel";
        public const string HomePosition = "homePosition";
        public const string PatrolPoints = "patrolPoints";
    }
}
```

### 2. Behavior Tree

Node-based decision tree with Running/Success/Failure states.

```csharp
public enum NodeStatus { Running, Success, Failure }

public abstract class BTNode
{
    public abstract NodeStatus Tick(NPCBrain brain);
    public virtual void OnEnter(NPCBrain brain) { }
    public virtual void OnExit(NPCBrain brain) { }
}

// Example: Guard Behavior Tree
var guardTree = new Selector(
    new Sequence(                           // If enemy visible
        new CheckEnemyVisible(),
        new Selector(
            new Sequence(                   // Attack if in range
                new CheckInAttackRange(),
                new Attack()
            ),
            new ChaseTarget()               // Otherwise chase
        )
    ),
    new Sequence(                           // If heard something
        new CheckHeardSound(),
        new InvestigateSound()
    ),
    new Patrol()                            // Default: patrol
);
```

### 3. Utility AI

Score-based action selection for more organic behavior.

```csharp
public class UtilityAction
{
    public string Name;
    public List<Consideration> Considerations;
    
    public float Score(NPCBrain brain)
    {
        float score = 1f;
        foreach (var c in Considerations)
            score *= c.Evaluate(brain);
        return score;
    }
}

public class Consideration
{
    public string InputKey;           // Blackboard key
    public ResponseCurve Curve;       // How input maps to score
    
    public float Evaluate(NPCBrain brain)
    {
        float input = brain.Blackboard.Get<float>(InputKey);
        return Curve.Evaluate(input);
    }
}

// Example considerations for "Attack" action:
// - Distance to target (closer = higher score)
// - My health (lower health = lower score)
// - Target health (lower = higher score, finish them!)
// - Ammo count (no ammo = 0 score)
```

### 4. Perception System

Modular sensors with memory.

```csharp
public class SightSensor
{
    public float ViewDistance = 20f;
    public float ViewAngle = 120f;
    public LayerMask TargetLayers;
    public LayerMask ObstacleLayers;
    
    public List<GameObject> GetVisibleTargets();
    public bool CanSee(GameObject target);
}

public class HearingSensor
{
    public float HearingRange = 15f;
    
    public void OnSoundEmitted(Vector3 position, float loudness, GameObject source);
    public List<SoundEvent> GetRecentSounds();
}

public class Memory
{
    public float MemoryDuration = 10f;
    
    public void Remember(GameObject target, Vector3 position);
    public MemoryRecord GetMemory(GameObject target);
    public List<MemoryRecord> GetAllMemories();
    public void Forget(GameObject target);
}
```

---

## Development Phases

### Phase 1: Core Framework (Week 1)
- [ ] NPCBrain component
- [ ] Blackboard system
- [ ] NPCBrainSettings ScriptableObject
- [ ] Assembly definitions
- [ ] Basic unit tests

### Phase 2: Behavior Trees (Week 2)
- [ ] BTNode base class with Running/Success/Failure
- [ ] Composite nodes: Selector, Sequence, Parallel
- [ ] Decorator nodes: Inverter, Repeater, Cooldown
- [ ] Action nodes: MoveTo, Wait, Log
- [ ] Condition nodes: CheckBlackboard, CheckDistance
- [ ] Unit tests for all nodes

### Phase 3: Perception System (Week 3)
- [ ] SightSensor with cone detection
- [ ] HearingSensor with sound events
- [ ] ProximitySensor for range checks
- [ ] Memory system with decay
- [ ] Gizmo visualization
- [ ] Integration with Blackboard

### Phase 4: Utility AI (Week 4)
- [ ] UtilityBrain action selector
- [ ] Consideration system
- [ ] Response curves (Linear, Exponential, Logistic, etc.)
- [ ] Pre-built actions (Idle, Patrol, Chase, Attack, Flee)
- [ ] Hybrid BT + Utility support

### Phase 5: Integration & Archetypes (Week 5)
- [ ] EasyPath integration (MoveTo uses pathfinding)
- [ ] SwarmAI integration (steering behaviors)
- [ ] GuardNPC archetype
- [ ] PatrolNPC archetype
- [ ] CivilianNPC archetype
- [ ] EnemyNPC archetype

### Phase 6: Demo & Polish (Week 6)
- [ ] Guard Demo scene (stealth game)
- [ ] Patrol Demo scene
- [ ] Combat Demo scene
- [ ] Town Demo (multiple NPC types)
- [ ] Documentation
- [ ] Custom editors
- [ ] Performance optimization

---

## Integration Points

### With EasyPath
```csharp
// MoveTo action uses EasyPath for navigation
public class MoveTo : BTAction
{
    public override NodeStatus Tick(NPCBrain brain)
    {
        var target = brain.Blackboard.Get<Vector3>(targetKey);
        var agent = brain.GetComponent<EasyPathAgent>();
        
        if (!agent.HasPath)
            agent.SetDestination(target);
            
        return agent.HasReachedDestination ? NodeStatus.Success : NodeStatus.Running;
    }
}
```

### With SwarmAI
```csharp
// FleeAction uses SwarmAI steering
public class FleeAction : UtilityAction
{
    public override void Execute(NPCBrain brain)
    {
        var swarmAgent = brain.GetComponent<SwarmAgent>();
        var threat = brain.Blackboard.Get<Transform>("threat");
        
        swarmAgent.AddBehavior(new FleeBehavior { Target = threat });
    }
}
```

---

## Demo Scenes

### 1. Guard Demo
**Scenario:** Stealth game with guards and player  
**Features demonstrated:**
- Vision cone detection
- Investigation behavior
- Alert states
- Return to patrol

### 2. Patrol Demo
**Scenario:** Guards patrolling waypoints  
**Features demonstrated:**
- Waypoint following
- Hearing detection
- Memory (last known position)
- Search patterns

### 3. Combat Demo
**Scenario:** Enemy NPCs vs player  
**Features demonstrated:**
- Utility AI for combat decisions
- Cover-seeking behavior
- Flanking
- Retreat when low health

### 4. Town Demo
**Scenario:** Living town with multiple NPC types  
**Features demonstrated:**
- Civilians wandering
- Guards patrolling
- Shopkeepers at stations
- Reactions to player actions

---

## API Examples

### Quick Start
```csharp
// Minimal setup - just add component
var npc = gameObject.AddComponent<NPCBrain>();
npc.SetArchetype(NPCArchetype.Guard);
// Done! NPC now guards, investigates sounds, chases intruders
```

### Custom Behavior Tree
```csharp
var brain = GetComponent<NPCBrain>();

brain.BehaviorTree = new BehaviorTree(
    new Selector(
        // Priority 1: Flee if health low
        new Sequence(
            new CheckHealth(threshold: 0.2f, comparison: Comparison.LessThan),
            new Flee()
        ),
        // Priority 2: Attack if enemy visible
        new Sequence(
            new CheckEnemyVisible(),
            new Attack()
        ),
        // Priority 3: Patrol
        new Patrol()
    )
);
```

### Custom Utility AI
```csharp
var brain = GetComponent<NPCBrain>();

brain.UtilityBrain.AddAction(new UtilityAction {
    Name = "Heal",
    Action = new UseHealthPotion(),
    Considerations = new List<Consideration> {
        new Consideration {
            InputKey = "healthPercent",
            Curve = ResponseCurve.InverseLinear  // Lower health = higher score
        },
        new Consideration {
            InputKey = "hasPotions",
            Curve = ResponseCurve.Boolean  // Must have potions
        }
    }
});
```

---

## Success Criteria

1. **5-minute setup** - New users can have a working NPC in 5 minutes
2. **Flexible** - Supports both Behavior Trees and Utility AI
3. **Integrated** - Works seamlessly with EasyPath and SwarmAI
4. **Visual debugging** - Easy to see what NPCs are thinking
5. **Performance** - Handle 100+ NPCs at 60 FPS
6. **Well-documented** - Clear examples for every feature

---

## Next Steps

1. Create folder structure and assembly definitions
2. Implement Blackboard system
3. Implement basic Behavior Tree nodes
4. Create first demo (Guard Demo)
