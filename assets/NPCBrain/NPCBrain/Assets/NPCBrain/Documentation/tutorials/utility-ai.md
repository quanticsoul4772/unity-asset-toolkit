# Utility AI Tutorial

Utility AI provides score-based decision making, allowing NPCs to weigh multiple factors when choosing actions. This creates more dynamic and believable behavior than simple priority-based systems.

---

## Table of Contents

1. [What is Utility AI?](#what-is-utility-ai)
2. [UtilitySelector](#utilityselector)
3. [UtilityAction](#utilityaction)
4. [Considerations](#considerations)
5. [Response Curves](#response-curves)
6. [Score Calculation](#score-calculation)
7. [Examples](#examples)
8. [Best Practices](#best-practices)

---

## What is Utility AI?

Utility AI assigns numeric scores to possible actions and selects based on those scores. Unlike behavior trees that use fixed priorities, Utility AI can:

- **Consider multiple factors** simultaneously
- **Adapt to changing situations** naturally
- **Create varied behavior** through probabilistic selection
- **Balance competing needs** (hunger vs. safety vs. curiosity)

### Comparison

```
Behavior Tree (Priority):        Utility AI (Scored):
┌────────────────────────┐       ┌────────────────────────┐
│ 1. If target → Chase   │       │ Chase:    0.85  ████▌  │
│ 2. If sound → Invest   │   vs  │ Patrol:   0.30  █▌     │
│ 3. Else → Patrol       │       │ Rest:     0.65  ███▎   │
└────────────────────────┘       │ Invest:   0.45  ██▎    │
                                 └────────────────────────┘
                                 Selected: Chase (highest)
```

---

## UtilitySelector

`UtilitySelector` is a composite node that selects children based on utility scores.

### Basic Usage

```csharp
using NPCBrain.BehaviorTree.Composites;
using NPCBrain.UtilityAI;

protected override BTNode CreateBehaviorTree()
{
    return new UtilitySelector(
        new UtilityAction("Attack", attackBehavior, 1.0f),
        new UtilityAction("Flee", fleeBehavior, 0.8f),
        new UtilityAction("Patrol", patrolBehavior, 0.3f)
    );
}
```

### Selection Process

1. **Calculate scores** for all actions
2. **Filter** actions with score ≤ 0
3. **Apply softmax** to convert scores to probabilities
4. **Select** action probabilistically (influenced by temperature)
5. **Execute** selected action until completion

### Temperature Control

The Criticality system adjusts selection temperature:

- **Low temperature (< 1.0)**: Nearly always picks highest score
- **Temperature = 1.0**: Balanced probabilistic selection
- **High temperature (> 1.0)**: More random, explores lower-scoring options

```csharp
// Manual temperature override
brain.Criticality.SetTemperature(0.5f);  // More deterministic
brain.Criticality.SetTemperature(2.0f);  // More random
```

---

## UtilityAction

`UtilityAction` wraps a behavior with scoring information.

### Constructor

```csharp
public UtilityAction(
    string name,                    // Display name
    BTNode behavior,                // The behavior to execute
    float baseWeight,               // Base importance (0-1+)
    params Consideration[] considerations  // Scoring factors
)
```

### Examples

```csharp
// Simple action with constant score
var patrolAction = new UtilityAction(
    "Patrol",
    patrolBehavior,
    0.5f  // Base weight of 0.5
);

// Action with considerations
var attackAction = new UtilityAction(
    "Attack",
    attackBehavior,
    1.0f,  // High base weight
    new DistanceConsideration("TargetRange", GetTarget, 10f, true),
    new BlackboardConsideration<float>("HasAmmo", "ammo", a => a > 0 ? 1f : 0f, 0f)
);
```

### Score Calculation

Final score = `baseWeight × consideration1 × consideration2 × ... × make-up`

The "make-up value" compensates for having many considerations:
```
makeUp = (1 + (1 - rawScore)) × (1 / numConsiderations)
finalScore = rawScore + makeUp × (1 - rawScore)
```

This prevents actions with many considerations from being unfairly penalized.

---

## Considerations

Considerations calculate scores based on game state.

### ConstantConsideration

Returns a fixed value (useful for base probabilities).

```csharp
new ConstantConsideration(0.5f)  // Always returns 0.5
```

### BlackboardConsideration<T>

Scores based on a blackboard value.

```csharp
// Linear health consideration (health 0-100 → score 0-1)
new BlackboardConsideration<float>(
    "HealthCheck",      // Name
    "health",           // Blackboard key
    hp => hp / 100f,    // Scoring function
    100f                // Default value if key missing
)

// Boolean check
new BlackboardConsideration<bool>(
    "HasTarget",
    "targetVisible",
    visible => visible ? 1f : 0f,
    false
)

// Inverse (low health = high flee score)
new BlackboardConsideration<float>(
    "LowHealth",
    "health",
    hp => 1f - (hp / 100f),  // Inverted
    100f
)
```

### DistanceConsideration

Scores based on distance to a target.

```csharp
new DistanceConsideration(
    "TargetDistance",                           // Name
    brain => GetTargetPosition(brain),          // Target position getter
    20f,                                        // Max distance for scoring
    true                                        // Invert (closer = higher score)
)
```

**Scoring:**
- `invert = true`: score = 1 - (distance / maxDistance)
- `invert = false`: score = distance / maxDistance

### TimeConsideration

Scores based on time since an event (cooldown-style).

```csharp
new TimeConsideration(
    "PatrolCooldown",    // Name
    "lastPatrolTime",    // Blackboard key storing last event time
    10f                  // Time for score to reach 1.0
)
```

**Scoring:** `min(1.0, (currentTime - lastEventTime) / duration)`

### RangeConsideration

Scores based on whether a value is within a range.

```csharp
new RangeConsideration(
    "IdealDistance",     // Name
    brain => GetDistanceToTarget(brain),
    5f,                  // Minimum ideal value
    15f,                 // Maximum ideal value
    1f                   // Score when within range
)
```

### SoundConsideration

Scores based on heard sounds.

```csharp
new SoundConsideration(
    "HeardGunshot",           // Name
    SoundType.Gunshot,        // Sound type to check
    1f                        // Score when sound heard recently
)
```

### Custom Consideration

```csharp
public class ThreatConsideration : Consideration
{
    public ThreatConsideration() : base("ThreatLevel") { }
    
    public override float Evaluate(NPCBrainController brain)
    {
        int enemyCount = CountNearbyEnemies(brain);
        float healthRatio = brain.Blackboard.Get("health", 100f) / 100f;
        
        // More threats + lower health = higher score
        float threatScore = enemyCount * 0.2f * (1f - healthRatio);
        return Mathf.Clamp01(threatScore);
    }
}
```

---

## Response Curves

Response curves transform consideration scores for fine-tuned behavior.

### Linear Curve

Linear interpolation between min and max.

```csharp
new LinearCurve(
    consideration,
    0.2f,  // Output at input 0
    0.8f   // Output at input 1
)
```

```
Output
1.0 │          ╱
0.8 │        ╱
    │      ╱
0.2 │    ╱
  0 └──╱─────────
    0          1  Input
```

### Exponential Curve

Exponential falloff for threshold-like behavior.

```csharp
new ExponentialCurve(
    consideration,
    2f  // Exponent (higher = sharper curve)
)
```

```
Output
1.0 │                ╱
    │              ╱
    │           ╱
    │       ╱
  0 └──────────────
    0          1  Input
```

### Step Curve

Binary threshold.

```csharp
new StepCurve(
    consideration,
    0.5f  // Threshold
)
```

```
Output
1.0 │         ┌────
    │         │
    │         │
  0 │─────────┘
    0        0.5  1  Input
```

### Logistic (S-Curve)

Smooth S-shaped transition.

```csharp
new LogisticCurve(
    consideration,
    0.5f,  // Midpoint
    10f    // Steepness
)
```

```
Output
1.0 │           ╭───
    │         ╱
0.5 │       •
    │     ╱
  0 │───╯
    0   0.5    1  Input
```

---

## Score Calculation

### Step-by-Step Example

```csharp
var fleeAction = new UtilityAction(
    "Flee",
    fleeBehavior,
    0.8f,  // Base weight
    new BlackboardConsideration<float>("LowHealth", "health", hp => 1f - hp/100f, 100f),
    new DistanceConsideration("EnemyClose", GetEnemy, 10f, true)
);
```

**Given state:** Health = 30, Enemy distance = 3m

1. **LowHealth consideration:** `1 - 30/100 = 0.7`
2. **EnemyClose consideration:** `1 - 3/10 = 0.7`
3. **Raw score:** `0.8 × 0.7 × 0.7 = 0.392`
4. **Make-up compensation:** Applied to prevent multi-consideration penalty
5. **Final score:** ~0.52 (compensated)

### Softmax Selection

Given final scores, softmax converts to probabilities:

```
P(action_i) = exp(score_i / T) / Σ exp(score_j / T)
```

Where T = temperature from Criticality system.

**Example with T = 1.0:**
- Attack: 0.85 → P = 0.48
- Flee: 0.65 → P = 0.32
- Patrol: 0.30 → P = 0.20

---

## Examples

### Combat NPC

```csharp
protected override BTNode CreateBehaviorTree()
{
    return new UtilitySelector(
        // High priority: Flee when health low and enemies close
        new UtilityAction(
            "Flee",
            CreateFleeBehavior(),
            1.0f,
            new BlackboardConsideration<float>("LowHealth", "health", 
                hp => Mathf.Pow(1f - hp/100f, 2f), 100f),  // Exponential urgency
            new DistanceConsideration("EnemyNear", GetEnemy, 15f, true)
        ),
        
        // Attack when have target and ammo
        new UtilityAction(
            "Attack",
            CreateAttackBehavior(),
            0.9f,
            new BlackboardConsideration<GameObject>("HasTarget", "target",
                t => t != null ? 1f : 0f, null),
            new BlackboardConsideration<int>("HasAmmo", "ammo",
                a => a > 0 ? 1f : 0f, 0),
            new DistanceConsideration("InRange", GetTarget, 20f, true)
        ),
        
        // Reload when out of ammo and safe
        new UtilityAction(
            "Reload",
            CreateReloadBehavior(),
            0.6f,
            new BlackboardConsideration<int>("NeedsAmmo", "ammo",
                a => a == 0 ? 1f : 0f, 30),
            new DistanceConsideration("EnemyFar", GetEnemy, 20f, false)  // Not inverted
        ),
        
        // Patrol as fallback
        new UtilityAction(
            "Patrol",
            CreatePatrolBehavior(),
            0.3f
        )
    );
}
```

### Needs-Based NPC (Sims-style)

```csharp
protected override BTNode CreateBehaviorTree()
{
    return new UtilitySelector(
        // Sleep when tired
        new UtilityAction(
            "Sleep",
            CreateSleepBehavior(),
            0.8f,
            new BlackboardConsideration<float>("Tired", "energy",
                e => Mathf.Pow(1f - e, 2f), 1f)  // Exponential urgency
        ),
        
        // Eat when hungry
        new UtilityAction(
            "Eat",
            CreateEatBehavior(),
            0.7f,
            new BlackboardConsideration<float>("Hungry", "hunger",
                h => h, 0f),  // Linear hunger
            new DistanceConsideration("NearFood", GetFood, 30f, true)
        ),
        
        // Socialize when lonely
        new UtilityAction(
            "Socialize",
            CreateSocializeBehavior(),
            0.5f,
            new BlackboardConsideration<float>("Lonely", "social",
                s => 1f - s, 1f),
            new TimeConsideration("SocialCooldown", "lastSocial", 60f)
        ),
        
        // Work by default
        new UtilityAction(
            "Work",
            CreateWorkBehavior(),
            0.4f,
            new BlackboardConsideration<float>("HasEnergy", "energy",
                e => e > 0.3f ? 1f : 0f, 1f)
        ),
        
        // Wander when nothing else to do
        new UtilityAction(
            "Wander",
            CreateWanderBehavior(),
            0.2f
        )
    );
}
```

---

## Best Practices

### 1. Start Simple

```csharp
// Start with base weights only
new UtilityAction("Attack", attackBehavior, 1.0f)
new UtilityAction("Patrol", patrolBehavior, 0.3f)

// Then add considerations one at a time
```

### 2. Use Meaningful Weights

```csharp
// Good: Weights reflect priority
"CriticalAction": 1.0f   // Most important
"ImportantAction": 0.7f
"NormalAction": 0.4f
"FallbackAction": 0.2f   // Least important

// Bad: All same weight
"Action1": 0.5f
"Action2": 0.5f
"Action3": 0.5f
```

### 3. Gate with Zero Scores

```csharp
// Action only possible when target exists
new BlackboardConsideration<GameObject>("HasTarget", "target",
    t => t != null ? 1f : 0f,  // Returns 0 if no target → action excluded
    null
)
```

### 4. Combine with Regular BT Nodes

```csharp
// UtilitySelector for high-level decisions
return new UtilitySelector(
    new UtilityAction("Combat", 
        // Regular Sequence for combat details
        new Sequence(
            new CheckBlackboard("target"),
            new MoveTo(() => targetPos),
            new Attack()
        ),
        0.9f,
        combatConsiderations
    ),
    new UtilityAction("Patrol", patrolSequence, 0.3f)
);
```

### 5. Debug Scores

Use the Debug Window to see:
- Current scores for each action
- Which action was selected
- Temperature and entropy values

---

[← Behavior Trees](behavior-trees.md) | [Perception →](perception.md)
