# NPCBrain Criticality System Design

**Version:** 1.0  
**Status:** Planning  
**Last Updated:** January 2026

## Overview

The Criticality System is a **core component** of NPCBrain that keeps NPC behavior at the "edge of chaos" - a dynamic balance between predictable (boring) and random (erratic) behavior. This creates NPCs that are **stable yet responsive, predictable yet surprising**.

**Key Insight:** Criticality doesn't add new behaviors - it **tunes existing parameters** that Utility AI already needs (temperature, inertia), creating emergent adaptive behavior with minimal overhead.

---

## The Problem Criticality Solves

| Without Criticality | With Criticality |
|---------------------|------------------|
| NPCs always do the "optimal" thing → predictable, exploitable | NPCs occasionally try alternatives → harder to cheese |
| NPCs randomly switch actions → erratic, unrealistic | Behavior adapts to situation → more believable |
| Static difficulty → gets stale | Dynamic challenge → stays engaging |
| No emergent group behavior | Groups self-organize based on situation |

---

## Core Concept: Order Parameters

**Criticality = feedback-controlled balance between exploitation and exploration**

We measure how "ordered" or "chaotic" NPC behavior is using **order parameters**:

### Per-NPC Metrics (Normalized to 0-1)

| Metric | Measures | Too Low (Ordered) | Too High (Chaotic) |
|--------|----------|-------------------|-------------------|
| **Action Entropy** | How "peaked" action selection is | Always same action | Random actions |
| **Plan Churn** | How often plan changes | Stuck on one plan | Constantly switching |
| **Surprise** | Prediction error (expected vs actual outcomes) | Too predictable | Outcomes make no sense |
| **State Volatility** | How often high-level states flip | Stuck in one state | Rapid state oscillation |

### Group Metrics

| Metric | Measures | Too Low | Too High |
|--------|----------|---------|----------|
| **Intent Diversity** | Disagreement among NPCs | Herd behavior | No coordination |
| **Message Burstiness** | Spikes in coordination signals | Silent | Flooding |
| **Task Reassignment Rate** | Work "avalanches" | Rigid roles | Chaotic reassignment |

---

## Architecture

```
NPCBrain/
└── Runtime/
    └── Criticality/                    # NEW MODULE
        ├── Core/
        │   ├── CriticalityController.cs    # Main controller
        │   ├── CriticalitySettings.cs      # ScriptableObject config
        │   └── CriticalityMetrics.cs       # Metric calculations
        ├── Telemetry/
        │   ├── ActionTelemetry.cs          # Track action history
        │   ├── PlanTelemetry.cs            # Track plan changes
        │   └── StateTelemetry.cs           # Track state transitions
        ├── Group/
        │   ├── GroupField.cs               # Shared coordination field
        │   └── IntentBroadcast.cs          # Low-bandwidth intent sharing
        └── Debug/
            └── CriticalityDebugger.cs      # Visualization
```

---

## Data Structures

### Telemetry (Per NPC)

```csharp
public class CriticalityTelemetry
{
    // Configuration
    public int WindowSize = 64;  // Number of ticks to track
    
    // Rolling buffers (ring buffers for O(1) operations)
    private RingBuffer<int> _actionHistory;      // Selected action IDs
    private RingBuffer<int> _planHistory;        // Current plan IDs
    private RingBuffer<float> _predictionErrors; // |predicted - actual|
    private RingBuffer<int> _stateHistory;       // High-level state IDs
    
    // Cached metrics (recomputed periodically, not every tick)
    public float ActionEntropy { get; private set; }
    public float PlanChurn { get; private set; }
    public float Surprise { get; private set; }
    public float StateVolatility { get; private set; }
    
    // Update
    public void RecordAction(int actionId);
    public void RecordPlan(int planId);
    public void RecordPredictionError(float predicted, float actual);
    public void RecordStateTransition(int newStateId);
    public void RecomputeMetrics();  // Called every N ticks
}
```

### Control State (Per NPC)

```csharp
public class CriticalityControlState
{
    // Knobs (what the controller adjusts)
    [Range(0.1f, 2.0f)]
    public float Temperature = 1.0f;        // Softmax exploration
    
    [Range(0f, 1f)]
    public float Inertia = 0.3f;            // Plan stickiness
    
    [Range(3, 12)]
    public int AttentionWidth = 6;          // Options considered
    
    [Range(0f, 1f)]
    public float Coupling = 0.5f;           // Group alignment strength
    
    [Range(0f, 1f)]
    public float BroadcastThreshold = 0.5f; // When to signal group
}
```

### Criticality Settings (ScriptableObject)

```csharp
[CreateAssetMenu(fileName = "CriticalitySettings", menuName = "NPCBrain/Criticality Settings")]
public class CriticalitySettings : ScriptableObject
{
    [Header("Target Bands (Critical Range)")]
    [MinMaxSlider(0, 1)]
    public Vector2 EntropyBand = new Vector2(0.35f, 0.55f);
    
    [MinMaxSlider(0, 1)]
    public Vector2 ChurnBand = new Vector2(0.15f, 0.30f);
    
    [MinMaxSlider(0, 1)]
    public Vector2 SurpriseBand = new Vector2(0.20f, 0.40f);
    
    [MinMaxSlider(0, 1)]
    public Vector2 VolatilityBand = new Vector2(0.10f, 0.25f);
    
    [Header("Metric Weights")]
    [Range(0, 2)] public float EntropyWeight = 1.0f;
    [Range(0, 2)] public float ChurnWeight = 0.8f;
    [Range(0, 2)] public float SurpriseWeight = 1.0f;
    [Range(0, 2)] public float VolatilityWeight = 0.6f;
    
    [Header("Controller Settings")]
    public float UpdateRate = 0.1f;         // Seconds between updates
    public float AdjustmentSpeed = 0.1f;    // How fast knobs change
    
    [Header("Knob Ranges")]
    public Vector2 TemperatureRange = new Vector2(0.1f, 2.0f);
    public Vector2 InertiaRange = new Vector2(0f, 1f);
    public Vector2Int AttentionRange = new Vector2Int(3, 12);
}
```

---

## Metric Calculations

### Action Entropy

Measures how "peaked" the action selection distribution is.

```csharp
public float ComputeActionEntropy(RingBuffer<int> actions, int totalActionCount)
{
    // Count occurrences of each action
    var counts = new Dictionary<int, int>();
    foreach (int action in actions)
    {
        counts[action] = counts.GetValueOrDefault(action, 0) + 1;
    }
    
    // Compute entropy: H = -Σ p(a) * log(p(a))
    float entropy = 0f;
    int total = actions.Count;
    foreach (var count in counts.Values)
    {
        float p = (float)count / total;
        if (p > 0)
            entropy -= p * Mathf.Log(p);
    }
    
    // Normalize to [0, 1] using max possible entropy
    float maxEntropy = Mathf.Log(totalActionCount);
    return maxEntropy > 0 ? entropy / maxEntropy : 0f;
}
```

### Plan Churn

Measures how often the NPC changes plans.

```csharp
public float ComputePlanChurn(RingBuffer<int> plans)
{
    if (plans.Count < 2) return 0f;
    
    int changes = 0;
    int lastPlan = plans[0];
    
    for (int i = 1; i < plans.Count; i++)
    {
        if (plans[i] != lastPlan)
        {
            changes++;
            lastPlan = plans[i];
        }
    }
    
    // Normalize: max changes = N-1
    return (float)changes / (plans.Count - 1);
}
```

### Surprise

Measures prediction error (expected vs actual outcomes).

```csharp
public float ComputeSurprise(RingBuffer<float> predictionErrors, float scale = 1f)
{
    if (predictionErrors.Count == 0) return 0f;
    
    float avgError = 0f;
    foreach (float error in predictionErrors)
    {
        avgError += Mathf.Abs(error);
    }
    avgError /= predictionErrors.Count;
    
    // Normalize and clamp
    return Mathf.Clamp01(avgError / scale);
}
```

### State Volatility

Measures how often high-level states flip.

```csharp
public float ComputeStateVolatility(RingBuffer<int> states)
{
    if (states.Count < 2) return 0f;
    
    int transitions = 0;
    int lastState = states[0];
    
    for (int i = 1; i < states.Count; i++)
    {
        if (states[i] != lastState)
        {
            transitions++;
            lastState = states[i];
        }
    }
    
    return (float)transitions / (states.Count - 1);
}
```

---

## Controller Logic

### Chaos Index Calculation

```csharp
public float ComputeChaosIndex(CriticalityMetrics metrics, CriticalitySettings settings)
{
    // Weighted sum of normalized metrics
    float chaos = 
        settings.EntropyWeight * metrics.ActionEntropy +
        settings.ChurnWeight * metrics.PlanChurn +
        settings.SurpriseWeight * metrics.Surprise +
        settings.VolatilityWeight * metrics.StateVolatility;
    
    // Normalize by total weight
    float totalWeight = settings.EntropyWeight + settings.ChurnWeight + 
                        settings.SurpriseWeight + settings.VolatilityWeight;
    
    return chaos / totalWeight;
}
```

### Knob Adjustment

```csharp
public void UpdateController(CriticalityControlState state, float chaosIndex, 
                             CriticalitySettings settings)
{
    float targetLow = settings.ChaosBand.x;   // e.g., 0.40
    float targetHigh = settings.ChaosBand.y;  // e.g., 0.55
    float speed = settings.AdjustmentSpeed;
    
    if (chaosIndex < targetLow)
    {
        // TOO ORDERED - Need more exploration
        state.Temperature += speed;                    // More randomness
        state.Inertia -= speed * 0.5f;                // Easier to switch
        state.AttentionWidth = Mathf.Min(state.AttentionWidth + 1, 
                                          settings.AttentionRange.y);
        state.Coupling -= speed * 0.3f;               // Less herd behavior
    }
    else if (chaosIndex > targetHigh)
    {
        // TOO CHAOTIC - Need more stability
        state.Temperature -= speed;                    // Less randomness
        state.Inertia += speed * 0.5f;                // Stick to plans
        state.AttentionWidth = Mathf.Max(state.AttentionWidth - 1,
                                          settings.AttentionRange.x);
        state.Coupling += speed * 0.3f;               // Align with group
    }
    // else: IN BAND - No adjustment needed
    
    // Clamp all values
    state.Temperature = Mathf.Clamp(state.Temperature, 
                                     settings.TemperatureRange.x,
                                     settings.TemperatureRange.y);
    state.Inertia = Mathf.Clamp01(state.Inertia);
    state.Coupling = Mathf.Clamp01(state.Coupling);
}
```

---

## Integration with Utility AI

The criticality system **feeds directly into Utility AI** action selection:

```csharp
public class UtilityBrain
{
    private CriticalityControlState _criticalityState;
    
    public UtilityAction SelectAction(List<UtilityAction> candidates, NPCBrain brain)
    {
        // 1. Limit candidates by attention width
        int width = _criticalityState?.AttentionWidth ?? candidates.Count;
        var topCandidates = GetTopScoring(candidates, width, brain);
        
        // 2. Score remaining candidates
        var scores = new float[topCandidates.Count];
        for (int i = 0; i < topCandidates.Count; i++)
        {
            scores[i] = topCandidates[i].Score(brain);
        }
        
        // 3. Apply softmax with temperature
        float temperature = _criticalityState?.Temperature ?? 1f;
        var probabilities = Softmax(scores, temperature);
        
        // 4. Apply inertia (prefer current action)
        float inertia = _criticalityState?.Inertia ?? 0f;
        if (_currentAction != null && topCandidates.Contains(_currentAction))
        {
            int currentIdx = topCandidates.IndexOf(_currentAction);
            probabilities[currentIdx] += inertia * (1f - probabilities[currentIdx]);
            NormalizeProbabilities(probabilities);
        }
        
        // 5. Sample from distribution
        return SampleFromDistribution(topCandidates, probabilities);
    }
    
    private float[] Softmax(float[] scores, float temperature)
    {
        float[] result = new float[scores.Length];
        float sum = 0f;
        
        for (int i = 0; i < scores.Length; i++)
        {
            result[i] = Mathf.Exp(scores[i] / temperature);
            sum += result[i];
        }
        
        for (int i = 0; i < result.Length; i++)
            result[i] /= sum;
            
        return result;
    }
}
```

---

## Integration with Behavior Trees

Criticality can influence BT execution:

```csharp
public class CriticalityAwareSelector : BTComposite
{
    public override NodeStatus Tick(NPCBrain brain)
    {
        var criticality = brain.CriticalityController;
        
        // If too ordered, occasionally try non-optimal branches
        if (criticality != null && criticality.IsTooOrdered)
        {
            float explorationChance = criticality.State.Temperature * 0.1f;
            if (Random.value < explorationChance)
            {
                // Try a random child instead of priority order
                return TickRandomChild(brain);
            }
        }
        
        // Normal priority-based selection
        return base.Tick(brain);
    }
}
```

---

## Group Coordination

### Shared Intent Field

NPCs can broadcast low-bandwidth intent signals:

```csharp
public class GroupField
{
    // Shared maps (updated by NPCs, read by all)
    public Dictionary<Vector3Int, float> ThreatMap;
    public Dictionary<Vector3Int, float> InterestMap;
    public Dictionary<Vector3Int, float> ResourceNeed;
    
    // Intent broadcasts (limited tokens)
    public struct IntentBroadcast
    {
        public int NPCId;
        public IntentType Type;  // Attacking, Flanking, Searching, Retreating
        public Vector3 Target;
        public float Confidence;
    }
    
    public List<IntentBroadcast> ActiveIntents;
}
```

### Coupling Control

```csharp
public Vector3 ApplyGroupCoupling(Vector3 individualDecision, NPCBrain brain)
{
    float coupling = brain.CriticalityState.Coupling;
    
    // Get group consensus direction
    Vector3 groupDirection = GetGroupConsensusDirection(brain);
    
    // Blend individual with group based on coupling
    return Vector3.Lerp(individualDecision, groupDirection, coupling);
}
```

**Criticality controls coupling:**
- **Ordered regime (low chaos):** Lower coupling → encourage diversity, scouts explore
- **Chaotic regime (high chaos):** Higher coupling → converge, rally, form lines

---

## Debug Visualization

### Criticality Debugger

```csharp
public class CriticalityDebugger : MonoBehaviour
{
    [Header("Display")]
    public bool ShowMetrics = true;
    public bool ShowControlState = true;
    public bool ShowChaosIndex = true;
    
    private void OnGUI()
    {
        if (!ShowMetrics) return;
        
        var brain = GetComponent<NPCBrain>();
        var metrics = brain.CriticalityMetrics;
        var state = brain.CriticalityState;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 400));
        
        // Chaos Index with color coding
        float chaos = brain.CriticalityController.ChaosIndex;
        GUI.color = GetChaosColor(chaos);
        GUILayout.Label($"Chaos Index: {chaos:F2}");
        GUI.color = Color.white;
        
        // Metrics
        DrawMetricBar("Entropy", metrics.ActionEntropy, settings.EntropyBand);
        DrawMetricBar("Churn", metrics.PlanChurn, settings.ChurnBand);
        DrawMetricBar("Surprise", metrics.Surprise, settings.SurpriseBand);
        DrawMetricBar("Volatility", metrics.StateVolatility, settings.VolatilityBand);
        
        // Control knobs
        GUILayout.Label($"Temperature: {state.Temperature:F2}");
        GUILayout.Label($"Inertia: {state.Inertia:F2}");
        GUILayout.Label($"Attention: {state.AttentionWidth}");
        GUILayout.Label($"Coupling: {state.Coupling:F2}");
        
        GUILayout.EndArea();
    }
    
    private Color GetChaosColor(float chaos)
    {
        if (chaos < settings.ChaosBand.x) return Color.blue;   // Too ordered
        if (chaos > settings.ChaosBand.y) return Color.red;    // Too chaotic
        return Color.green;  // In critical band
    }
}
```

---

## Implementation Phases

### Phase 1: Core (Week 4, alongside Utility AI)

| Component | Scope | Effort |
|-----------|-------|--------|
| CriticalityTelemetry | Ring buffers, all 4 metrics | 1.5 days |
| CriticalityController | Temperature + inertia + attention control | 1 day |
| UtilityBrain integration | Softmax with temperature, inertia | 0.5 day |
| Debug visualization | Full metrics display | 0.5 day |
| **Total Phase 1** | | **3.5 days** |

### Phase 2: Group Coordination (Week 5)

| Component | Scope | Effort |
|-----------|-------|--------|
| Group field | Shared coordination signals | 1.5 days |
| Coupling control | Group alignment tuning | 1 day |
| Intent broadcast | Low-bandwidth NPC communication | 1 day |
| **Total Phase 2** | | **3.5 days** |

---

## Performance Considerations

| Operation | Cost | Frequency |
|-----------|------|-----------|
| Record action/plan/state | O(1) | Every tick |
| Recompute metrics | O(N) where N=window size | Every 5-10 ticks |
| Controller update | O(1) | Every 5-10 ticks |
| Group field read/write | O(1) | Every tick |

**Estimated overhead:** <0.1ms per NPC per frame with default settings.

---

## User-Facing Configuration

### Inspector

```
[NPCBrain Component]
├── Core Settings
├── Behavior Tree
├── Utility AI
├── Perception
└── ▼ Adaptive Behavior
    ├── Criticality Settings: [Default Settings ▼]
    ├── [Show Debug Visualization]
    └── ── Live Metrics ──
        ├── Chaos Index: ████████░░ 0.47 (OK)
        ├── Temperature: 1.2
        ├── Inertia: 0.35
        └── Attention: 6
```

### Friendly Naming

| Technical Term | User-Facing Name |
|----------------|------------------|
| Criticality | Adaptive Behavior |
| Chaos Index | Behavior Balance |
| Temperature | Exploration |
| Inertia | Commitment |
| Coupling | Group Alignment |
| Entropy | Action Variety |
| Churn | Decision Stability |

---

## Marketing Angle

**NPCBrain is the ONLY Unity AI asset with criticality-based adaptive behavior.**

> "NPCs that feel alive - neither robotic nor random. Our unique Adaptive Behavior system keeps AI at the perfect balance point, creating enemies that are challenging yet fair, companions that are helpful yet surprising."

**Competitor comparison:**

| Feature | NPCBrain | Behavior Designer | NodeCanvas | Emerald AI |
|---------|----------|-------------------|------------|------------|
| Adaptive temperature | ✅ | ❌ | ❌ | ❌ |
| Action entropy tracking | ✅ | ❌ | ❌ | ❌ |
| Group criticality | ✅ | ❌ | ❌ | ❌ |
| Exploration/exploitation balance | ✅ | ❌ | ❌ | ❌ |

---

## Success Criteria

1. ✅ **Core Feature** - Integrated into every NPCBrain
2. ✅ **Lightweight** - <0.1ms overhead per NPC
3. ✅ **Visible** - Clear debug visualization
4. ✅ **Tunable** - ScriptableObject settings for target bands and weights
5. ✅ **Integrated** - Seamlessly works with Utility AI and Behavior Trees
6. ✅ **Documented** - Guide with examples

---

## References

- Self-Organized Criticality (Bak, Tang, Wiesenfeld 1987)
- Edge of Chaos in Neural Networks (Langton 1990)
- Utility AI with Temperature (Mark, Lewis - GDC talks)
- Softmax Exploration in RL (Sutton & Barto)
