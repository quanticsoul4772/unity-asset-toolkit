# NPCBrain: Opportunities, Gaps, and Cross-Domain Innovations

**Version:** 1.0  
**Status:** Research Synthesis  
**Last Updated:** January 2026

## Purpose

This document synthesizes research across multiple domains to identify:
1. What we might be missing
2. Unity features we could leverage
3. Cross-domain concepts worth adopting
4. Gaps in the current design
5. Top opportunities for differentiation

---

## Executive Summary: Top 10 Opportunities

| Priority | Opportunity | Effort | Impact | Recommendation |
|----------|-------------|--------|--------|----------------|
| 🔥 1 | **Emotional State System** | Medium | High | Add to v1.0 |
| 🔥 2 | **Influence Maps** | Low | High | Add to v1.0 |
| 🔥 3 | **Context Steering** | Medium | High | Add to v1.0 |
| 🔥 4 | **Animation Rigging Integration** | Low | Medium | Add to v1.0 |
| 5 | Stigmergy (Pheromone Fields) | Medium | Medium | Enhance Group Coordination |
| 6 | Drama Manager / AI Director | High | High | v2.0 |
| 7 | Relationship/Reputation System | Medium | Medium | v2.0 |
| 8 | Unity Sentis ML Integration | High | Medium | v2.0 |
| 9 | Subsumption Architecture Layer | Low | Medium | Consider for v1.0 |
| 10 | Dialogue/Bark System | Medium | Medium | v2.0 |

---

## Part 1: Unity Features to Leverage

### 1.1 Unity Animation Rigging (RECOMMENDED - v1.0)

**What it is:** Procedural animation system for runtime bone manipulation.

**Why it matters for NPCBrain:**
- NPCs can look at targets naturally (head/eye tracking)
- Procedural gestures during idle states
- Aim IK for combat without animation clips
- Blending procedural + authored animation

**Integration with NPCBrain:**
```csharp
public class AnimatorBridge
{
    // Existing animation control
    public void SetTrigger(string name);
    public void SetFloat(string name, float value);
    
    // NEW: Procedural animation hooks
    public void SetLookAtTarget(Transform target, float weight);
    public void SetAimTarget(Vector3 position);
    public void SetProceduralPose(string poseName, float weight);
}
```

**Effort:** 2-3 days | **Impact:** Makes NPCs feel more alive without more animation clips

---

### 1.2 Unity Sentis (ML Inference) - v2.0

**What it is:** Run ONNX neural network models in Unity.

**Potential uses:**
- Train behavior models from player demonstrations (imitation learning)
- Neural network-based target prioritization
- Learned response curves instead of hand-tuned
- Player behavior prediction

**Why defer to v2.0:**
- Requires ML training pipeline
- Most users don't have ML expertise
- Adds significant complexity

**But prepare for it:**
- Keep perception data in tensor-friendly formats
- Design Consideration interface to accept external scorers
- Document extension points for ML integration

---

### 1.3 Addressables for NPC Prefabs - Consider

**What it is:** Async asset loading system.

**Benefit:** Load NPC prefabs on demand, reduce memory for large scenes.

**Decision:** Not critical for v1.0, but mention in documentation for users with many NPC types.

---

### 1.4 DOTS/ECS - Not Now

**What it is:** Data-oriented tech stack for massive parallelism.

**Why not for v1.0:**
- Steep learning curve for users
- Most games don't need 1000+ NPCs
- Our Jobs/Burst-ready design is sufficient
- Can refactor later if demand exists

**Keep in mind:** Current design should be DOTS-migration-friendly (data separate from logic).

---

## Part 2: Cross-Domain Concepts Worth Adopting

### 2.1 Emotional State System (RECOMMENDED - v1.0)

**Source:** Affective computing, psychology (PAD model, OCC model)

**What it is:** NPCs have emotional states that influence behavior.

**The PAD Model (Pleasure-Arousal-Dominance):**
```
Pleasure:   [-1, 1]  Happy ←→ Sad
Arousal:    [-1, 1]  Calm ←→ Excited  
Dominance:  [-1, 1]  Submissive ←→ Dominant
```

**Why it matters:**
- Same NPC behaves differently based on emotional state
- Emotions decay over time (return to baseline)
- Events trigger emotional responses
- Creates more believable characters

**Implementation:**
```csharp
public class EmotionalState
{
    // PAD values
    public float Pleasure { get; private set; }    // -1 to 1
    public float Arousal { get; private set; }     // -1 to 1
    public float Dominance { get; private set; }   // -1 to 1
    
    // Derived emotions (from PAD combinations)
    public Emotion CurrentEmotion => DeriveEmotion();
    
    // Baseline personality (NPC returns to this)
    public Vector3 Baseline = new Vector3(0.2f, 0f, 0.3f);
    
    // Decay rate (emotions fade over time)
    public float DecayRate = 0.1f;
    
    public void ApplyEvent(EmotionalEvent evt)
    {
        Pleasure += evt.PleasureImpact;
        Arousal += evt.ArousalImpact;
        Dominance += evt.DominanceImpact;
        ClampValues();
    }
    
    public void Update(float deltaTime)
    {
        // Decay toward baseline
        Pleasure = Mathf.MoveTowards(Pleasure, Baseline.x, DecayRate * deltaTime);
        Arousal = Mathf.MoveTowards(Arousal, Baseline.y, DecayRate * deltaTime);
        Dominance = Mathf.MoveTowards(Dominance, Baseline.z, DecayRate * deltaTime);
    }
}

public enum Emotion
{
    Neutral,
    Happy,      // +P, +A, +D
    Angry,      // -P, +A, +D
    Afraid,     // -P, +A, -D
    Sad,        // -P, -A, -D
    Confident,  // +P, -A, +D
    Bored,      // -P, -A, +D
    Surprised,  // varies, high A spike
}
```

**Integration with Utility AI:**
```csharp
public class EmotionConsideration : Consideration
{
    public Emotion RequiredEmotion;
    public float MinIntensity = 0.3f;
    
    public override float Evaluate(NPCBrain brain)
    {
        if (brain.Emotions.CurrentEmotion != RequiredEmotion)
            return 0f;
        return brain.Emotions.GetIntensity();
    }
}

// Example: Flee action scores higher when Afraid
var fleeAction = new UtilityAction("Flee")
{
    Considerations = new List<Consideration>
    {
        new EmotionConsideration { RequiredEmotion = Emotion.Afraid },
        new HealthConsideration { Threshold = 0.3f },
        new HasEscapeRouteConsideration()
    }
};
```

**Effort:** 3-4 days | **Impact:** Huge differentiation, very few assets do this well

---

### 2.2 Influence Maps (RECOMMENDED - v1.0)

**Source:** RTS games, military AI research

**What it is:** Grid-based heat maps showing tactical information.

**Maps to maintain:**
| Map | Updates | Used For |
|-----|---------|----------|
| **Threat Map** | When enemies move/attack | Pathfinding avoidance, fear response |
| **Territory Map** | Periodically | Defining "home" vs "enemy" areas |
| **Interest Map** | When goals change | Search patterns, patrol routes |
| **Cover Map** | Static | Finding safe positions |

**Implementation:**
```csharp
public class InfluenceMapSystem
{
    public InfluenceMap ThreatMap { get; private set; }
    public InfluenceMap TerritoryMap { get; private set; }
    public InfluenceMap InterestMap { get; private set; }
    
    private float _cellSize = 2f;
    
    public void UpdateThreatMap()
    {
        ThreatMap.Clear();
        
        foreach (var threat in GetAllThreats())
        {
            // Add influence with distance falloff
            ThreatMap.AddInfluence(
                threat.Position, 
                threat.ThreatLevel,
                threat.InfluenceRadius,
                InfluenceFalloff.Linear
            );
        }
        
        // Blur for smoother gradients
        ThreatMap.Blur();
    }
    
    public float GetThreatAt(Vector3 position)
    {
        return ThreatMap.Sample(position);
    }
    
    public Vector3 GetSafestDirection(Vector3 from, float searchRadius)
    {
        // Sample threat in 8 directions, return direction with lowest threat
        return ThreatMap.GetLowestInfluenceDirection(from, searchRadius);
    }
}

public class InfluenceMap
{
    private float[,] _values;
    private Vector3 _origin;
    private float _cellSize;
    
    public void AddInfluence(Vector3 center, float strength, float radius, InfluenceFalloff falloff);
    public float Sample(Vector3 worldPosition);
    public Vector3 GetLowestInfluenceDirection(Vector3 from, float searchRadius);
    public void Blur(int iterations = 1);
    public void Decay(float rate);
}
```

**Uses in NPCBrain:**
- **Flee behavior:** Move toward lowest threat
- **Patrol:** Bias toward high-interest areas
- **Search:** Mark searched areas, prioritize unsearched
- **Combat positioning:** Find cover with sight to target, low threat

**Effort:** 3-4 days | **Impact:** Enables smart tactical behavior

---

### 2.3 Context Steering (RECOMMENDED - v1.0)

**Source:** GDC talks (Andrew Fray), robotics

**What it is:** Instead of picking ONE direction, sample ALL directions and weight by desirability.

**Why it's better than traditional steering:**
- Smoothly blends multiple goals (seek target + avoid threats + stay in cover)
- No "stuck" situations
- Works with any number of considerations

**Implementation:**
```csharp
public class ContextSteering
{
    private int _numDirections = 16;  // Sample 16 directions around NPC
    private float[] _interest;         // How much we WANT to go this direction
    private float[] _danger;           // How much we DON'T want to go this direction
    
    public Vector3 GetBestDirection(NPCBrain brain)
    {
        // Reset
        Array.Clear(_interest, 0, _numDirections);
        Array.Clear(_danger, 0, _numDirections);
        
        // Each behavior adds to interest/danger
        AddSeekInterest(brain.CurrentTarget.Position);
        AddDangerFromThreats(brain.Perception.VisibleThreats);
        AddDangerFromInfluenceMap(brain.InfluenceMaps.ThreatMap);
        AddInterestFromCover(brain.NearestCoverPositions);
        
        // Combine: interest minus danger
        float[] combined = new float[_numDirections];
        for (int i = 0; i < _numDirections; i++)
        {
            combined[i] = _interest[i] - _danger[i];
        }
        
        // Pick best direction (or blend top 3 for smoother movement)
        return GetWeightedAverageDirection(combined);
    }
    
    private void AddSeekInterest(Vector3 target)
    {
        Vector3 toTarget = (target - transform.position).normalized;
        for (int i = 0; i < _numDirections; i++)
        {
            Vector3 dir = GetDirectionForSlot(i);
            float dot = Vector3.Dot(toTarget, dir);
            _interest[i] += Mathf.Max(0, dot);
        }
    }
    
    private void AddDangerFromThreats(List<Threat> threats)
    {
        foreach (var threat in threats)
        {
            Vector3 toThreat = (threat.Position - transform.position).normalized;
            for (int i = 0; i < _numDirections; i++)
            {
                Vector3 dir = GetDirectionForSlot(i);
                float dot = Vector3.Dot(toThreat, dir);
                if (dot > 0)
                {
                    _danger[i] += dot * threat.DangerLevel;
                }
            }
        }
    }
}
```

**Integration with existing movement:**
- Replace simple "move toward target" with context steering
- Works alongside EasyPath (steer within navigable area)
- SwarmAI separation/alignment can feed into interest/danger arrays

**Effort:** 2-3 days | **Impact:** Much smarter movement, handles complex situations

---

### 2.4 Stigmergy / Pheromone Fields

**Source:** Ant colony behavior, swarm robotics

**What it is:** NPCs leave "marks" in the environment that influence other NPCs.

**Already partially covered:** The Criticality system's Group Field is essentially stigmergy.

**Enhancements to consider:**
```csharp
public class StigmergyField
{
    // Different "pheromone" types
    public InfluenceMap DangerTrail;    // "I was attacked here"
    public InfluenceMap SearchedArea;   // "I already looked here"
    public InfluenceMap PathTrail;      // "This is a good path" (from successful navigation)
    public InfluenceMap ResourceMarker; // "Found something valuable here"
    
    // NPCs deposit while moving
    public void Deposit(Vector3 position, PheromoneType type, float amount);
    
    // Pheromones evaporate over time
    public void Update(float deltaTime)
    {
        DangerTrail.Decay(0.1f * deltaTime);
        SearchedArea.Decay(0.05f * deltaTime);
        // etc.
    }
}
```

**Use cases:**
- **Coordinated search:** NPCs avoid areas already searched by others
- **Danger avoidance:** If one NPC was attacked, others avoid that area
- **Patrol optimization:** NPCs spread out naturally

**Decision:** Merge into Group Coordination system in Phase 2.

---

### 2.5 Subsumption Architecture

**Source:** Robotics (Rodney Brooks, 1986)

**What it is:** Layered behaviors where lower layers handle survival, higher layers handle goals.

```
Layer 3: Goals (patrol route, complete mission)
   ↑ can be suppressed by ↓
Layer 2: Tactics (take cover, flank target)
   ↑ can be suppressed by ↓
Layer 1: Reactions (avoid collision, flee danger)
   ↑ can be suppressed by ↓
Layer 0: Survival (don't fall off cliff, don't walk into fire)
```

**Why it matters:**
- Guarantees safety behaviors always work
- Higher-level AI can't make NPC walk off a cliff
- Natural priority system

**Implementation:**
```csharp
public class SubsumptionController
{
    private List<IBehaviorLayer> _layers; // Ordered by priority (0 = highest)
    
    public void Tick(NPCBrain brain)
    {
        Vector3 desiredMovement = Vector3.zero;
        UtilityAction desiredAction = null;
        
        // Each layer can override or suppress previous
        foreach (var layer in _layers)
        {
            var output = layer.Evaluate(brain);
            
            if (output.SuppressMovement)
                desiredMovement = output.Movement;
            else
                desiredMovement += output.Movement;
                
            if (output.SuppressAction)
                desiredAction = output.Action;
        }
        
        // Execute
        brain.Move(desiredMovement);
        if (desiredAction != null)
            desiredAction.Execute(brain);
    }
}
```

**Decision:** Consider integrating. Could be a wrapper around existing BT/Utility systems.

---

## Part 3: Advanced AI Techniques

### 3.1 Hierarchical Task Networks (HTN)

**What it is:** Plan-based AI that decomposes high-level goals into subtasks.

**Comparison:**

| Approach | Strengths | Weaknesses |
|----------|-----------|------------|
| **Behavior Tree** | Easy to understand, predictable | Manual design required |
| **Utility AI** | Emergent, handles many considerations | Can feel random |
| **GOAP** | Flexible planning | Expensive, hard to debug |
| **HTN** | Structured planning, efficient | Complex to author |

**Decision:** Defer to v2.0. BT + Utility covers 90% of use cases.

---

### 3.2 Monte Carlo Tree Search (MCTS)

**What it is:** Look-ahead planning by simulating possible futures.

**Use case:** Tactical combat decisions (should I attack now or wait for backup?)

**Why defer:** 
- Expensive (many simulations)
- Overkill for most games
- Criticality already adds strategic variety

**Note for future:** Could be a premium "Tactical AI" expansion.

---

### 3.3 Imitation Learning

**What it is:** Train NPC behavior from player demonstrations.

**Potential workflow:**
1. Player plays as NPC (record inputs + observations)
2. Train neural network to map observations → actions
3. Deploy with Unity Sentis

**Why defer:** Requires ML pipeline, training data, expertise.

**Prepare for it:** Keep observation data (perception, state) in formats suitable for ML training.

---

## Part 4: Gaps in Current Design

### Gap 1: No Emotional Modeling ⚠️ HIGH PRIORITY

**Current state:** NPCs have no internal emotional state.

**Problem:** All NPCs feel robotic, no personality variation.

**Solution:** Add Emotional State System (see 2.1).

---

### Gap 2: No Tactical Positioning System ⚠️ HIGH PRIORITY

**Current state:** Movement is simple (move toward target, flee from danger).

**Problem:** No understanding of cover, flanking, crossfire.

**Solutions:**
- Influence Maps (see 2.2)
- Context Steering (see 2.3)
- Cover Point Analysis

```csharp
public class TacticalPositioning
{
    public Vector3 FindBestCombatPosition(NPCBrain brain, Transform target)
    {
        var candidates = FindCoverPoints(brain.Position, 10f);
        
        float bestScore = float.MinValue;
        Vector3 bestPosition = brain.Position;
        
        foreach (var cover in candidates)
        {
            float score = 0f;
            
            // Can see target from here?
            if (HasLineOfSight(cover, target.position))
                score += 10f;
            
            // Protected from target?
            if (IsProtectedFrom(cover, target.position))
                score += 8f;
            
            // Low threat from other enemies
            score -= brain.InfluenceMaps.ThreatMap.Sample(cover) * 5f;
            
            // Not too far to move
            float distance = Vector3.Distance(brain.Position, cover);
            score -= distance * 0.5f;
            
            if (score > bestScore)
            {
                bestScore = score;
                bestPosition = cover;
            }
        }
        
        return bestPosition;
    }
}
```

---

### Gap 3: No Procedural Animation Hooks ⚠️ MEDIUM PRIORITY

**Current state:** AnimatorBridge just sets parameters/triggers.

**Problem:** NPCs can't dynamically look at targets, gesture, etc.

**Solution:** Add Animation Rigging integration (see 1.1).

---

### Gap 4: No NPC Dialogue/Barks

**Current state:** No system for NPC vocalizations.

**Problem:** Silent NPCs feel dead.

**Solution (v2.0):**
```csharp
public class BarkSystem
{
    // Context-aware bark selection
    public void TryBark(NPCBrain brain, BarkContext context)
    {
        var validBarks = _barkDatabase.GetBarksFor(
            brain.Archetype,
            brain.CurrentState,
            context
        );
        
        // Don't spam - check cooldown
        if (!brain.CanBark) return;
        
        // Pick weighted random
        var bark = SelectBark(validBarks);
        
        // Play audio + optional subtitle
        brain.AudioSource.PlayOneShot(bark.AudioClip);
        if (bark.Subtitle != null)
            ShowSubtitle(bark.Subtitle);
        
        // Cooldown
        brain.SetBarkCooldown(bark.Cooldown);
    }
}

public enum BarkContext
{
    SpottedEnemy,
    LostTarget,
    TakingDamage,
    AllyDown,
    Searching,
    Idle,
    Victory,
    Retreating
}
```

---

### Gap 5: No Relationship/Reputation System

**Current state:** NPCs don't remember past interactions.

**Problem:** Can't create persistent worlds where actions have consequences.

**Solution (v2.0):**
```csharp
public class RelationshipSystem
{
    // Per-NPC relationships
    private Dictionary<int, float> _relationships; // npcId → trust (-1 to 1)
    
    // Faction relationships
    private Dictionary<(Faction, Faction), float> _factionRelations;
    
    public float GetRelationship(NPCBrain other)
    {
        // Personal relationship overrides faction
        if (_relationships.TryGetValue(other.Id, out float personal))
            return personal;
        
        // Fall back to faction
        return GetFactionRelationship(this.Faction, other.Faction);
    }
    
    public void ModifyRelationship(NPCBrain other, float delta, string reason)
    {
        float current = GetRelationship(other);
        _relationships[other.Id] = Mathf.Clamp(current + delta, -1f, 1f);
        
        // Event for other systems
        OnRelationshipChanged?.Invoke(this, other, delta, reason);
    }
}
```

---

### Gap 6: No AI Director / Drama Manager

**Current state:** NPCs act independently, no system coordinates overall pacing.

**Problem:** Game can get too easy or too hard, no dramatic tension.

**Solution (v2.0):**
```csharp
public class AIDirector
{
    // Track player stress (combat intensity, recent damage, etc.)
    public float PlayerStress { get; private set; }
    
    // Target stress curve over time
    public AnimationCurve DesiredStressCurve;
    
    public void Update()
    {
        float desiredStress = DesiredStressCurve.Evaluate(Time.time);
        float currentStress = MeasurePlayerStress();
        
        if (currentStress < desiredStress * 0.8f)
        {
            // Too easy - increase pressure
            IncreaseAggression();
            SpawnReinforcements();
        }
        else if (currentStress > desiredStress * 1.2f)
        {
            // Too hard - reduce pressure
            DecreaseAggression();
            CreateEscapeOpportunity();
        }
    }
    
    private void IncreaseAggression()
    {
        // Adjust NPC criticality settings toward more aggressive
        foreach (var npc in ActiveNPCs)
        {
            npc.CriticalitySettings.ChaosBandOffset += 0.05f;
        }
    }
}
```

---

## Part 5: Psychological Principles

### 5.1 Theory of Mind

**What it is:** Understanding that others have different knowledge/beliefs.

**Application:**
- NPC doesn't know player is behind wall (even though game knows)
- NPC acts on what THEY saw, not omniscient knowledge
- Can be fooled by distractions

**Already covered:** Perception + Memory system handles this.

**Enhancement:** Add explicit "belief" system:
```csharp
public class Beliefs
{
    // What I BELIEVE about targets (may be wrong)
    public Dictionary<int, TargetBelief> TargetBeliefs;
    
    public class TargetBelief
    {
        public Vector3 BelievedPosition;  // May be outdated
        public float BelievedHealth;      // May be wrong
        public bool BelievedAlive;        // May not know they're dead
        public float Confidence;          // How sure am I?
    }
}
```

### 5.2 Attribution Theory

**What it is:** How we explain others' behavior (intentional vs accidental).

**Application:** 
- NPC hears noise → Was that intentional (enemy) or accident (rat)?
- Low threat = assume accident, investigate cautiously
- High threat = assume enemy, respond aggressively

**Integration with Perception:**
```csharp
public class SoundEvent
{
    public Vector3 Position;
    public float Loudness;
    public SoundType Type;
    
    // NEW: Attribution
    public float HostilityScore;  // 0 = probably harmless, 1 = definitely threat
    
    public static float EstimateHostility(SoundType type, float loudness, NPCBrain hearer)
    {
        float base = type switch
        {
            SoundType.Gunshot => 1.0f,
            SoundType.Explosion => 1.0f,
            SoundType.Footstep => 0.3f,
            SoundType.Door => 0.2f,
            SoundType.Ambient => 0.0f,
            _ => 0.5f
        };
        
        // Louder = more likely intentional
        base += loudness * 0.2f;
        
        // Anxious NPCs assume hostility
        base += hearer.Emotions.Arousal * 0.2f;
        
        return Mathf.Clamp01(base);
    }
}
```

---

## Part 6: Updated Architecture

Based on these findings, here's the enhanced NPCBrain architecture:

```
NPCBrain/
├── Runtime/
│   ├── Core/
│   │   ├── NPCBrain.cs
│   │   ├── Blackboard.cs
│   │   ├── NPCBrainSettings.cs
│   │   ├── WaypointPath.cs
│   │   └── NPCEvents.cs
│   │
│   ├── BehaviorTree/          # Existing
│   ├── UtilityAI/             # Existing
│   ├── Perception/            # Existing
│   ├── Criticality/           # Existing
│   │
│   ├── Emotions/              # NEW MODULE
│   │   ├── EmotionalState.cs
│   │   ├── EmotionalEvent.cs
│   │   ├── Emotion.cs
│   │   └── PersonalityProfile.cs  # Baseline PAD values
│   │
│   ├── Tactical/              # NEW MODULE
│   │   ├── InfluenceMapSystem.cs
│   │   ├── InfluenceMap.cs
│   │   ├── ContextSteering.cs
│   │   ├── CoverAnalyzer.cs
│   │   └── TacticalPositioning.cs
│   │
│   ├── Animation/             # ENHANCED
│   │   ├── AnimatorBridge.cs
│   │   ├── ProceduralAnimation.cs  # NEW: Animation Rigging hooks
│   │   └── LookAtController.cs     # NEW: Head/eye tracking
│   │
│   ├── Archetypes/            # Existing
│   └── Integration/           # Existing
│
└── Editor/
    └── (existing structure)
```

---

## Part 7: Revised Development Timeline

### Phase 1: Core Framework (Week 1) - No Change

### Phase 2: Behavior Trees (Week 2) - No Change

### Phase 3: Perception System (Week 3) - No Change

### Phase 4: Utility AI + Criticality + Emotions (Week 4) - ENHANCED
- [ ] Utility AI (existing)
- [ ] Criticality (existing)
- [ ] **NEW:** Emotional State System (2-3 days)
- [ ] **NEW:** Emotion Considerations for Utility AI (1 day)

### Phase 5: Tactical Systems + Integration (Week 5) - ENHANCED
- [ ] EasyPath/SwarmAI integration (existing)
- [ ] Archetypes (existing)
- [ ] **NEW:** Influence Maps (2 days)
- [ ] **NEW:** Context Steering (2 days)
- [ ] **NEW:** Tactical Positioning (1 day)

### Phase 6: Animation + Polish (Week 6) - ENHANCED
- [ ] Debug window (existing)
- [ ] Documentation (existing)
- [ ] **NEW:** Animation Rigging integration (1-2 days)
- [ ] **NEW:** Procedural look-at (1 day)

---

## Part 8: Success Criteria (Updated)

| # | Criteria | Status |
|---|----------|--------|
| 1 | 5-minute setup | ✅ Planned |
| 2 | BT + Utility AI hybrid | ✅ Planned |
| 3 | EasyPath + SwarmAI integration | ✅ Planned |
| 4 | Runtime debug visualization | ✅ Planned |
| 5 | 100+ NPCs @ 60 FPS | ✅ Planned |
| 6 | 4 ready-to-use archetypes | ✅ Planned |
| 7 | Criticality (adaptive behavior) | ✅ Planned |
| 8 | **Emotional State System** | 🆕 NEW |
| 9 | **Influence Maps** | 🆕 NEW |
| 10 | **Context Steering** | 🆕 NEW |
| 11 | **Procedural Animation Hooks** | 🆕 NEW |

---

## Part 9: Competitor Gap Analysis (Updated)

| Feature | NPCBrain | Behavior Designer | NodeCanvas | Emerald AI |
|---------|----------|-------------------|------------|------------|
| Behavior Trees | ✅ | ✅ | ✅ | ✅ |
| Utility AI | ✅ | ❌ | ✅ | ❌ |
| Perception System | ✅ | ❌ (addon) | ❌ | ✅ |
| Memory System | ✅ | ❌ | ❌ | ✅ |
| Criticality | ✅ **UNIQUE** | ❌ | ❌ | ❌ |
| Emotional State | ✅ **UNIQUE** | ❌ | ❌ | ❌ |
| Influence Maps | ✅ | ❌ | ❌ | ❌ |
| Context Steering | ✅ | ❌ | ❌ | ❌ |
| Pathfinding Integration | ✅ (EasyPath) | ❌ | ❌ | ✅ (NavMesh) |
| Swarm Integration | ✅ (SwarmAI) | ❌ | ❌ | ❌ |
| Animation Integration | ✅ (Rigging) | ❌ | ❌ | ✅ (basic) |
| Visual Editor | ❌ (v2.0) | ✅ | ✅ | ❌ |
| DOTS Support | ❌ (future) | ✅ | ❌ | ❌ |

**Summary:** NPCBrain will have 4 unique features no competitor offers:
1. Criticality (adaptive behavior)
2. Emotional State System
3. Integrated Influence Maps
4. Context Steering

Plus unique integration with EasyPath + SwarmAI.

---

## Part 10: v2.0 Roadmap

Features to defer but plan for:

| Feature | Complexity | Notes |
|---------|------------|-------|
| Visual Behavior Tree Editor | High | Node-based UI, save as ScriptableObject |
| GOAP Planner | Medium | Alternative to BT for planning NPCs |
| Relationship System | Medium | Persistent NPC memory across sessions |
| Dialogue/Bark System | Medium | Context-aware NPC vocalizations |
| AI Director | High | Pacing and dramatic tension management |
| Unity Sentis Integration | High | ML-based behavior training |
| DOTS Migration | Very High | For 1000+ NPC scenarios |

---

## Conclusion

By adding **Emotional State**, **Influence Maps**, **Context Steering**, and **Animation Rigging** to the v1.0 scope, NPCBrain becomes the most comprehensive NPC AI solution on the Unity Asset Store.

The additional effort is approximately **7-10 days** but the differentiation is massive:
- No competitor has emotional modeling
- No competitor has influence maps
- No competitor has context steering
- No competitor integrates with pathfinding AND steering AND decisions

NPCBrain won't just be another AI asset - it will be the **complete NPC brain** that the name promises.

---

## References

- PAD Emotional Model (Mehrabian & Russell, 1974)
- Utility AI (Dave Mark, GDC talks)
- Influence Maps (various RTS games, military AI)
- Context Steering (Andrew Fray, GDC 2015)
- Subsumption Architecture (Rodney Brooks, 1986)
- Stigmergy (Grassé, ant colony behavior)
- Unity Animation Rigging documentation
- Unity Sentis documentation
