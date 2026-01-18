# Criticality System Tutorial

The Criticality system provides adaptive exploration vs. exploitation control, preventing repetitive NPC behavior and encouraging natural variation.

---

## Table of Contents

1. [What is Criticality?](#what-is-criticality)
2. [How It Works](#how-it-works)
3. [Temperature](#temperature)
4. [Entropy](#entropy)
5. [Inertia](#inertia)
6. [Configuration](#configuration)
7. [Integration](#integration)
8. [Debugging](#debugging)

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
┌─────────────────────────────────────────────────────┐
│                                                     │
│   ┌──────────────┐    ┌──────────────┐             │
│   │   Action     │───►│   Record     │             │
│   │   Selected   │    │   History    │             │
│   └──────────────┘    └──────┬───────┘             │
│          ▲                   │                     │
│          │                   ▼                     │
│   ┌──────┴───────┐    ┌──────────────┐             │
│   │   Softmax    │◄───│  Calculate   │             │
│   │   Selection  │    │   Entropy    │             │
│   └──────────────┘    └──────┬───────┘             │
│          ▲                   │                     │
│          │                   ▼                     │
│   ┌──────┴───────┐    ┌──────────────┐             │
│   │  Temperature │◄───│   Adjust     │             │
│   │   Applied    │    │   Temp       │             │
│   └──────────────┘    └──────────────┘             │
│                                                     │
└─────────────────────────────────────────────────────┘
```

### Step-by-Step

1. **Action Selected**: UtilitySelector picks an action
2. **Record**: Action ID added to history buffer
3. **Calculate Entropy**: Shannon entropy of action distribution
4. **Adjust Temperature**: Based on entropy vs. target
5. **Softmax**: Next selection uses new temperature

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

Inertia is the tendency to repeat the current action:

```
Inertia = 1 - NormalizedEntropy
```

| Entropy | Inertia | Meaning |
|---------|---------|--------|
| Low | High | Tends to repeat actions |
| High | Low | Tends to switch actions |

### UtilitySelector Uses Inertia

The current action gets a small bonus:

```csharp
// In UtilitySelector:
if (actionIndex == _lastSelectedAction)
{
    score += Criticality.Inertia * InertiaBonus;
}
```

This creates "commitment" - once an action starts, the NPC is more likely to see it through rather than flip-flopping.

---

## Configuration

### Default Values

```csharp
public const int DefaultHistorySize = 20;
public const float DefaultMinTemperature = 0.5f;
public const float DefaultMaxTemperature = 2.0f;
public const float DefaultTemperatureAdjustRate = 0.1f;
public const float DefaultTargetEntropy = 0.5f;
```

### Custom Configuration

```csharp
public class MyNPC : NPCBrainController
{
    protected override void Awake()
    {
        base.Awake();
        
        // Replace default criticality with custom settings
        Criticality = new CriticalityController(
            historySize: 30,           // More history = smoother
            minTemperature: 0.3f,      // More deterministic minimum
            maxTemperature: 3.0f,      // More random maximum
            temperatureAdjustRate: 0.05f,  // Slower adjustment
            targetEntropy: 0.6f        // Target more variation
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
    Debug.Log($"Temperature: {Criticality.Temperature:F2}");
    Debug.Log($"Entropy: {Criticality.Entropy:F2}");
    Debug.Log($"Inertia: {Criticality.Inertia:F2}");
    Debug.Log($"History: {Criticality.ActionHistoryCount}/{Criticality.HistorySize}");
    Debug.Log($"Unique Actions: {Criticality.UniqueActionCount}");
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
