# Criticality System Tutorial

The Criticality system provides adaptive exploration vs. exploitation control, preventing repetitive NPC behavior and encouraging natural variation. It keeps NPCs at the "edge of chaos" - behaving consistently enough to be believable, yet varied enough to be interesting.

---

## Table of Contents

1. [What is Criticality?](#what-is-criticality)
2. [How It Works](#how-it-works)
3. [The Three Metrics](#the-three-metrics)
4. [Chaos Index](#chaos-index)
5. [Temperature](#temperature)
6. [Inertia](#inertia)
7. [Configuration](#configuration)
8. [Integration](#integration)
9. [Debugging](#debugging)

---

## What is Criticality?

Criticality is borrowed from statistical mechanics. Systems at "critical points" exhibit interesting emergent behavior—neither frozen nor chaotic. NPCBrain's Criticality system maintains NPCs near this critical point.

### The Problem It Solves

Without criticality:
```
Time:  t1  t2  t3  t4  t5  t6  t7  t8  t9  t10
Action: P   P   P   P   P   P   P   P   P   P
        └───────────────────────────────────┘
              Boring, repetitive patrol
```

With criticality:
```
Time:  t1  t2  t3  t4  t5  t6  t7  t8  t9  t10
Action: P   P   W   P   R   P   W   P   P   I
        └───────────────────────────────────┘
         Varied, interesting behavior
         (P=Patrol, W=Wander, R=Rest, I=Investigate)
```

---

## How It Works

### Feedback Loop

```
┌───────────────────────────────────────────────────────────────┐
│                                                               │
│   ┌──────────────┐    ┌─────────────────────────────────┐    │
│   │   Action     │───►│       Record History            │    │
│   │   Selected   │    │  - Action ID (for entropy)      │    │
│   └──────────────┘    │  - Plan ID (for churn)          │    │
│          ▲            │  - State ID (for volatility)    │    │
│          │            └───────────────┬─────────────────┘    │
│          │                            │                      │
│          │                            ▼                      │
│   ┌──────┴───────┐    ┌─────────────────────────────────┐    │
│   │   Softmax    │    │      Calculate Metrics          │    │
│   │   Selection  │    │  - Entropy (action variety)     │    │
│   │  + Inertia   │    │  - Churn (plan stability)       │    │
│   │    Boost     │    │  - Volatility (state changes)   │    │
│   └──────────────┘    └───────────────┬─────────────────┘    │
│          ▲                            │                      │
│          │                            ▼                      │
│   ┌──────┴───────┐    ┌─────────────────────────────────┐    │
│   │  Temperature │◄───│     Compute Chaos Index         │    │
│   │  + Inertia   │    │  Weighted sum → Adjust T & I    │    │
│   └──────────────┘    └─────────────────────────────────┘    │
│                                                               │
└───────────────────────────────────────────────────────────────┘
```

### Step-by-Step

1. **Action Selected**: UtilitySelector picks an action using softmax + inertia
2. **Record History**: Action, plan, and state IDs added to history buffers
3. **Calculate Metrics**: Entropy, plan churn, and state volatility computed
4. **Compute Chaos Index**: Weighted combination of all metrics (0-1)
5. **Adjust Controls**: Temperature and inertia updated based on chaos vs. target
6. **Next Selection**: Uses new temperature for randomness, inertia for commitment

---

## The Three Metrics

Criticality tracks three complementary metrics to understand NPC behavior patterns:

### 1. Action Entropy

**What it measures**: Variety in action selection (Shannon entropy)

```
History: [P, P, P, P, P, P, P, P, P, P]  → Entropy: 0.0 (repetitive)
History: [P, W, R, P, W, R, P, W, R, P]  → Entropy: 1.09 (varied)
```

- **Zero**: Single action repeated (boring, exploitable)
- **High**: Many different actions (interesting, unpredictable)

### 2. Plan Churn

**What it measures**: How often the NPC switches behaviors/plans

```
Plans: [Patrol, Patrol, Patrol, Patrol, Patrol]  → Churn: 0.0 (stable)
Plans: [Patrol, Chase, Patrol, Chase, Patrol]    → Churn: 1.0 (flip-flopping)
```

- **Zero**: Never changes plan (committed but inflexible)
- **High**: Constantly switching (erratic, unreliable)

### 3. State Volatility

**What it measures**: Rate of high-level state transitions

```
States: [Idle, Idle, Idle, Idle, Idle]    → Volatility: 0.0 (calm)
States: [Idle, Alert, Combat, Idle, Alert] → Volatility: 1.0 (chaotic)
```

- **Zero**: Stable state (predictable)
- **High**: Rapid state changes (unstable)

---

## Chaos Index

### Weighted Combination

The **Chaos Index** combines all three metrics:

```csharp
ChaosIndex = (
    EntropyWeight * NormalizedEntropy +
    ChurnWeight * PlanChurn +
    VolatilityWeight * StateVolatility
) / TotalWeight;
```

Default weights (per the design spec):
- **Entropy**: 1.0 (most important - action variety)
- **Churn**: 0.8 (important - plan stability)
- **Volatility**: 0.6 (less critical - state changes)

### Regime Detection

```csharp
// Three regimes based on chaos vs. target
bool isTooOrdered = ChaosIndex < (TargetEntropy - 0.1f);   // Boring!
bool isTooChaotic = ChaosIndex > (TargetEntropy + 0.1f);   // Erratic!
bool isInCriticalBand = !isTooOrdered && !isTooChaotic;    // Just right!
```

| Regime | Chaos Index | System Response |
|--------|-------------|-----------------|
| Too Ordered | < 0.4 | ↑ Temperature, ↓ Inertia (explore more) |
| Critical Band | 0.4 - 0.6 | No change (optimal) |
| Too Chaotic | > 0.6 | ↓ Temperature, ↑ Inertia (stabilize) |

---

## Temperature

### What It Controls

Temperature affects the softmax probability distribution:

```
P(action_i) = exp(score_i / T) / Σ exp(score_j / T)
```

### Visual Effect

**Low Temperature (T = 0.5)** - Deterministic
```
Scores:  Attack=0.8  Patrol=0.3  Rest=0.2
         ████████░░  ███░░░░░░░  ██░░░░░░░░

Probs:   Attack=0.92 Patrol=0.06 Rest=0.02
         █████████▌  ▌           ░
         
         Almost always picks Attack
```

**Normal Temperature (T = 1.0)** - Balanced
```
Scores:  Attack=0.8  Patrol=0.3  Rest=0.2
         ████████░░  ███░░░░░░░  ██░░░░░░░░

Probs:   Attack=0.58 Patrol=0.25 Rest=0.17
         █████▊      ██▌         █▊
         
         Usually picks Attack, sometimes others
```

**High Temperature (T = 2.0)** - Exploratory
```
Scores:  Attack=0.8  Patrol=0.3  Rest=0.2
         ████████░░  ███░░░░░░░  ██░░░░░░░░

Probs:   Attack=0.42 Patrol=0.31 Rest=0.27
         ████▏       ███         ██▊
         
         Often explores lower-scoring options
```

### Temperature Range

| Value | Behavior |
|-------|----------|
| 0.5 (min) | Almost deterministic, always picks highest |
| 1.0 | Normal probabilistic selection |
| 2.0 (max) | Very exploratory, nearly random |

---

## Entropy

### Shannon Entropy

```
H = -Σ p_i × log(p_i)
```

Entropy measures the "randomness" of recent action distribution.

### Examples

**Zero Entropy** - All same action
```
History: [P, P, P, P, P, P, P, P, P, P]
Counts:  Patrol=10, others=0
Entropy: 0.0 (perfectly repetitive)
```

**Low Entropy** - Dominated by one action
```
History: [P, P, P, P, P, P, P, P, W, R]
Counts:  Patrol=8, Wander=1, Rest=1
Entropy: 0.72 (mostly patrol)
```

**High Entropy** - Evenly distributed
```
History: [P, W, R, P, W, R, P, W, R, P]
Counts:  Patrol=4, Wander=3, Rest=3
Entropy: 1.09 (varied behavior)
```

### Normalized Entropy

```
Normalized = H / log(numUniqueActions)
```

Ranges from 0 (one action) to 1 (perfectly uniform).

---

## Inertia

### What It Is

Inertia is the tendency to stick with the current action, computed as the inverse of chaos:

```
Inertia = 1 - ChaosIndex
```

| Chaos | Inertia | Meaning |
|-------|---------|---------|
| Low (ordered) | High | NPC commits strongly to current action |
| High (chaotic) | Low | NPC readily switches to alternatives |

### How UtilitySelector Applies Inertia

After softmax probabilities are computed, the previous action gets an inertia boost:

```csharp
// In UtilitySelector.SelectAction():
if (inertia > 0f && _lastSelectedActionIndex >= 0)
{
    // Only boost if previous action is still viable (positive score)
    if (_scores[_lastSelectedActionIndex] > 0f)
    {
        float currentProb = _probabilities[_lastSelectedActionIndex];
        // Proportional boost based on "headroom"
        float boost = inertia * (1f - currentProb);
        _probabilities[_lastSelectedActionIndex] = currentProb + boost;
        // Renormalize...
    }
}
```

### The Effect

**High Inertia (repetitive history)**:
```
Before inertia: Patrol=40%, Wander=35%, Rest=25%
After inertia:  Patrol=70%, Wander=18%, Rest=12%  (Patrol was previous)
```

**Low Inertia (varied history)**:
```
Before inertia: Patrol=40%, Wander=35%, Rest=25%
After inertia:  Patrol=52%, Wander=28%, Rest=20%  (smaller boost)
```

This creates **commitment**: NPCs that have been doing varied things (high chaos) are more willing to switch, while NPCs stuck in patterns (low chaos) paradoxically get pushed to explore by the temperature increase.

### Why This Works

The feedback creates homeostasis:
1. **Stuck in loop** → Low chaos → High inertia → BUT temperature also rises → More random selection breaks the loop
2. **Flip-flopping** → High chaos → Low inertia → BUT temperature drops → More deterministic selection stabilizes

---

## Configuration

### Default Values

```csharp
// Core settings
public const int DefaultHistorySize = 20;
public const float DefaultMinTemperature = 0.5f;
public const float DefaultMaxTemperature = 2.0f;
public const float DefaultTemperatureAdjustRate = 0.1f;
public const float DefaultTargetEntropy = 0.5f;

// Metric weights for chaos index
public const float DefaultEntropyWeight = 1.0f;
public const float DefaultChurnWeight = 0.8f;
public const float DefaultVolatilityWeight = 0.6f;
```

### Custom Configuration

```csharp
public class MyNPC : NPCBrainController
{
    protected override void Awake()
    {
        base.Awake();

        // Basic configuration (backward compatible)
        Criticality = new CriticalityController(
            historySize: 30,               // More history = smoother
            minTemperature: 0.3f,          // More deterministic minimum
            maxTemperature: 3.0f,          // More random maximum
            temperatureAdjustRate: 0.05f,  // Slower adjustment
            targetEntropy: 0.6f            // Target more variation
        );

        // Full configuration with metric weights
        Criticality = new CriticalityController(
            historySize: 30,
            minTemperature: 0.3f,
            maxTemperature: 3.0f,
            temperatureAdjustRate: 0.05f,
            targetEntropy: 0.6f,
            entropyWeight: 1.0f,       // Action variety (most important)
            churnWeight: 0.5f,         // Plan stability (reduced)
            volatilityWeight: 0.3f     // State changes (less critical)
        );
    }
}
```

### Parameter Guidelines

| Parameter | Low Value | High Value |
|-----------|-----------|------------|
| History Size | 10: Quick adaptation | 50: Stable, slow changes |
| Min Temperature | 0.3: Very deterministic | 0.8: Still somewhat random |
| Max Temperature | 1.5: Limited exploration | 3.0: Very random |
| Adjust Rate | 0.01: Very gradual | 0.2: Rapid response |
| Target Entropy | 0.3: Prefer consistency | 0.7: Prefer variety |

### Presets

```csharp
// Consistent NPC (guards, workers)
Criticality = new CriticalityController(
    historySize: 30,
    minTemperature: 0.5f,
    maxTemperature: 1.5f,
    temperatureAdjustRate: 0.05f,
    targetEntropy: 0.3f
);

// Chaotic NPC (goblins, children)
Criticality = new CriticalityController(
    historySize: 10,
    minTemperature: 0.8f,
    maxTemperature: 2.5f,
    temperatureAdjustRate: 0.15f,
    targetEntropy: 0.7f
);

// Balanced NPC (default)
Criticality = new CriticalityController();  // Uses all defaults
```

---

## Integration

### Automatic with UtilitySelector

Criticality works automatically when using `UtilitySelector`:

```csharp
protected override BTNode CreateBehaviorTree()
{
    // UtilitySelector automatically:
    // 1. Records selected actions to Criticality
    // 2. Uses Criticality.Temperature for softmax
    // 3. Uses Criticality.Inertia for action bonus
    
    return new UtilitySelector(
        new UtilityAction("Attack", attackBehavior, 1.0f),
        new UtilityAction("Patrol", patrolBehavior, 0.5f),
        new UtilityAction("Rest", restBehavior, 0.3f)
    );
}
```

### Manual Integration

If building custom selection:

```csharp
public void SelectAction()
{
    // Calculate scores
    float[] scores = CalculateScores();
    
    // Apply softmax with criticality temperature
    float[] probs = Softmax(scores, Criticality.Temperature);
    
    // Select probabilistically
    int selected = SampleFromDistribution(probs);
    
    // Record selection for entropy tracking
    Criticality.RecordAction(selected);
    
    // Execute
    ExecuteAction(selected);
}
```

### Reading Values

```csharp
void DisplayDebugInfo()
{
    // Primary controls
    Debug.Log($"Temperature: {Criticality.Temperature:F2}");
    Debug.Log($"Inertia: {Criticality.Inertia:F2}");

    // Chaos index and regime
    Debug.Log($"Chaos Index: {Criticality.ChaosIndex:F2}");
    Debug.Log($"Regime: {(Criticality.IsTooOrdered ? "TOO ORDERED" : Criticality.IsTooChaotic ? "TOO CHAOTIC" : "CRITICAL BAND")}");

    // Individual metrics
    Debug.Log($"Entropy: {Criticality.Entropy:F2}");
    Debug.Log($"Plan Churn: {Criticality.PlanChurn:F2}");
    Debug.Log($"State Volatility: {Criticality.StateVolatility:F2}");

    // History stats
    Debug.Log($"Action History: {Criticality.ActionHistoryCount}/{Criticality.HistorySize}");
    Debug.Log($"Plan History: {Criticality.PlanHistoryCount}/{Criticality.HistorySize}");
    Debug.Log($"State History: {Criticality.StateHistoryCount}/{Criticality.HistorySize}");
    Debug.Log($"Unique Actions: {Criticality.UniqueActionCount}");
}
```

### Recording Custom Events

You can record additional events to influence criticality:

```csharp
// When switching behaviors
void OnBehaviorChanged(int newBehaviorId)
{
    Criticality.RecordPlan(newBehaviorId);
}

// When state changes
void OnStateChanged(int newStateId)
{
    Criticality.RecordStateTransition(newStateId);
}
```

---

## Debugging

### Debug Window

The NPCBrain Debug Window shows criticality stats:

```
┌─ Criticality ─────────────────────┐
│ Temperature:  1.23  [███████░░░]  │
│ Entropy:      0.67  [██████▌░░░]  │
│ Inertia:      0.33  [███░░░░░░░]  │
│ History:      20/20               │
│ Unique:       4 actions           │
└───────────────────────────────────┘
```

### Visual Indicators

```
Temperature Bar:
[░░░░░░░░░░] 0.5  - Very deterministic
[█████░░░░░] 1.0  - Normal
[██████████] 2.0  - Very exploratory

Entropy Bar:
[░░░░░░░░░░] 0.0  - Repetitive (all same action)
[█████░░░░░] 0.5  - Balanced mix
[██████████] 1.0  - Maximum variety
```

### Understanding Behavior

**If NPC seems too predictable:**
- Check if Temperature is stuck at minimum
- Increase `targetEntropy`
- Increase `maxTemperature`

**If NPC seems too random:**
- Check if Temperature is stuck at maximum
- Decrease `targetEntropy`
- Decrease `maxTemperature`

**If behavior changes too fast:**
- Increase `historySize`
- Decrease `temperatureAdjustRate`

**If behavior doesn't adapt:**
- Decrease `historySize`
- Increase `temperatureAdjustRate`

---

## Example: Adapting Guard

```csharp
using UnityEngine;
using NPCBrain;
using NPCBrain.BehaviorTree;
using NPCBrain.BehaviorTree.Composites;
using NPCBrain.UtilityAI;
using NPCBrain.Criticality;

public class AdaptiveGuard : NPCBrainController
{
    [Header("Criticality Tuning")]
    [SerializeField] private int _historySize = 20;
    [SerializeField] private float _minTemp = 0.5f;
    [SerializeField] private float _maxTemp = 2.0f;
    [SerializeField] private float _adjustRate = 0.1f;
    [SerializeField] private float _targetEntropy = 0.5f;
    
    protected override void Awake()
    {
        base.Awake();
        
        // Custom criticality from inspector values
        Criticality = new CriticalityController(
            _historySize,
            _minTemp,
            _maxTemp,
            _adjustRate,
            _targetEntropy
        );
    }
    
    protected override BTNode CreateBehaviorTree()
    {
        return new UtilitySelector(
            new UtilityAction("Chase", CreateChaseBehavior(), 1.0f,
                new BlackboardConsideration<GameObject>("HasTarget", "target",
                    t => t != null ? 1f : 0f, null)),
                    
            new UtilityAction("Investigate", CreateInvestigateBehavior(), 0.7f,
                new BlackboardConsideration<Vector3>("HasLKP", "lastKnownPosition",
                    p => p != Vector3.zero ? 1f : 0f, Vector3.zero)),
                    
            new UtilityAction("Patrol", CreatePatrolBehavior(), 0.5f,
                new TimeConsideration("PatrolCooldown", "lastPatrolTime", 10f)),
                
            new UtilityAction("Rest", CreateRestBehavior(), 0.3f,
                new BlackboardConsideration<float>("Tired", "energy",
                    e => 1f - e, 1f))
        );
    }
    
    // Debug display
    void OnGUI()
    {
        if (!Application.isPlaying) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 200, 100));
        GUILayout.Label($"Temp: {Criticality.Temperature:F2}");
        GUILayout.Label($"Entropy: {Criticality.Entropy:F2}");
        GUILayout.Label($"Inertia: {Criticality.Inertia:F2}");
        GUILayout.EndArea();
    }
}
```

---

## Advanced: Manual Temperature Control

Sometimes you want to override automatic adjustment:

```csharp
// Force high exploration during search
public void EnterSearchMode()
{
    Criticality.SetTemperature(Criticality.MaxTemperature);
}

// Force deterministic behavior in combat
public void EnterCombatMode()
{
    Criticality.SetTemperature(Criticality.MinTemperature);
}

// Reset to let auto-adjustment take over
public void ExitSpecialMode()
{
    Criticality.SetTemperature(1.0f);
}
```

---

[← Perception](perception.md) | [Creating Custom NPCs →](custom-npcs.md)
