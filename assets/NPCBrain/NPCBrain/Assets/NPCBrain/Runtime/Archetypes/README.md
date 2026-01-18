# NPCBrain Archetypes

Pre-built NPC archetypes that demonstrate common AI patterns. All archetypes use UtilitySelector with the Criticality system for dynamic, adaptive behavior.

## Overview

Each archetype extends `NPCBrainController` and implements `CreateBehaviorTree()` to define its behavior. All use `UtilitySelector` which enables:

- **Utility-based decision making** - Actions are scored and selected probabilistically
- **Criticality system** - Temperature and inertia adapt based on action variety
- **Dynamic behavior** - NPCs become more exploratory when repeating actions

## Archetypes

### CopNPC

Police officer that patrols, investigates sounds/alarms, chases and arrests robbers.

**Behaviors (by priority):**
| Action | Weight | Trigger |
|--------|--------|--------|
| Arrest | 1.1 | Very close to robber |
| Chase | 1.0 | Robber visible |
| InvestigateAlarm | 0.9 | Alarm sound heard |
| InvestigateSound | 0.5 | Footstep heard |
| Return | 0.4 | Far from patrol area |
| Patrol | 0.3 | Baseline |

**Usage:**
```csharp
var cop = CopNPC.Create(position, parent);
cop.SetWaypointPath(patrolPath);
cop.OnArrest += (cop, robber) => Debug.Log("Arrested!");
```

---

### RobberNPC

Thief that steals loot, evades cops, hides, and escapes with stolen goods.

**Behaviors (by priority):**
| Action | Weight | Trigger |
|--------|--------|--------|
| Flee | 1.0 | Cop visible |
| CarryToEscape | 0.9 | Has loot, heading to escape |
| StealLoot | 0.85 | Near loot, no cops |
| Hide | 0.7 | High fear, near cover |
| Sneak | 0.5 | Moderate fear |
| Scout | 0.3 | Baseline exploration |

**Usage:**
```csharp
var robber = RobberNPC.Create(position, parent);
// Robber automatically finds LootPoints and EscapeZones
```

---

### HearingGuardNPC

Guard that responds to both visual and audio stimuli with hearing-aware utility scoring.

**Behaviors:**
| Action | Weight | Trigger |
|--------|--------|--------|
| Chase | 1.0 | Target visible |
| InvestigateGunshot | 0.85 | Gunshot heard |
| InvestigateFootstep | 0.5 | Footstep heard |
| Return | 0.4 | Far from post |
| Patrol | 0.3 | Baseline |

---

### GuardNPC

Sight-based guard with chase, investigate, and patrol behaviors.

**Behaviors:**
| Action | Weight | Trigger |
|--------|--------|--------|
| Chase | 1.0 | Target visible and close |
| Investigate | 0.7 | Has last known position |
| Return | 0.4 | Far from home |
| Patrol | 0.3 | Baseline |

---

### PatrolNPC

Simple patrol NPC with energy-based rest and wander behaviors.

**Behaviors:**
| Action | Weight | Trigger |
|--------|--------|--------|
| Patrol | 0.7 | Has energy |
| Rest | 0.5 | Low energy |
| Wander | 0.4 | Moderate energy |

---

### UtilityNPC

General-purpose utility AI demonstration with interest-seeking behavior.

**Behaviors:**
| Action | Weight | Trigger |
|--------|--------|--------|
| SeekInterest | 0.8 | Has interest point |
| Patrol | 0.7 | Has waypoints |
| Wander | 0.5 | Has energy |
| Rest | 0.3 | Low energy |

---

## Creating Custom Archetypes

```csharp
public class MyCustomNPC : NPCBrainController
{
    protected override BTNode CreateBehaviorTree()
    {
        // Create utility actions with considerations
        var action1 = new UtilityAction(
            "MyAction",
            new Sequence(/* behavior nodes */),
            weight: 1.0f,
            new BlackboardConsideration<bool>("Gate", "key", v => v ? 1f : 0f, false)
        );
        
        // Use UtilitySelector for Criticality integration
        return new UtilitySelector(action1, action2, action3);
    }
}
```

## Criticality System

All archetypes using `UtilitySelector` automatically integrate with the Criticality system:

- **Temperature** - Controls exploration vs exploitation
  - Low (< 1.0): Deterministic, always picks highest-scoring action
  - High (> 1.0): Probabilistic, explores lower-scoring actions
  
- **Entropy** - Measures action variety in recent history
  - Low entropy (repetitive): Temperature increases → more exploration
  - High entropy (varied): Temperature decreases → more exploitation

- **Inertia** - Bonus score for repeating the same action
  - Helps NPCs commit to actions rather than flip-flopping
