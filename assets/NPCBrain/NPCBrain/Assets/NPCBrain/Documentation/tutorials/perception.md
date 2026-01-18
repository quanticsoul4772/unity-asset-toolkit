# Perception System Tutorial

The perception system allows NPCs to detect and remember targets through sight, hearing, and memory. This tutorial covers all perception components and how to use them effectively.

---

## Table of Contents

1. [Overview](#overview)
2. [SightSensor](#sightsensor)
3. [HearingSensor](#hearingsensor)
4. [Memory System](#memory-system)
5. [Sound System](#sound-system)
6. [Events](#events)
7. [Best Practices](#best-practices)

---

## Overview

### Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    NPCBrainController                    │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐     │
│  │ SightSensor │  │HearingSensor│  │   Memory    │     │
│  │  (Vision)   │  │  (Audio)    │  │ (History)   │     │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘     │
│         │                │                │             │
│         └────────────────┼────────────────┘             │
│                          ▼                              │
│                   [Perception Data]                     │
│                   - Visible targets                     │
│                   - Heard sounds                        │
│                   - Remembered positions                │
└─────────────────────────────────────────────────────────┘
```

### Sensor Interfaces

```csharp
// Base sensor interface
public interface ISensor
{
    bool IsActive { get; }
    void Tick(NPCBrainController brain);
}

// Target detection (SightSensor)
public interface ITargetSensor : ISensor
{
    IReadOnlyList<GameObject> DetectedTargets { get; }
    GameObject PrimaryTarget { get; }
    bool HasTargets { get; }
}

// Sound detection (HearingSensor)
public interface ISoundSensor : ISensor
{
    IReadOnlyList<SoundEvent> HeardSounds { get; }
    bool HasHeardSounds { get; }
}
```

---

## SightSensor

### Overview

The SightSensor creates a vision cone that detects tagged targets using raycasts for line-of-sight verification.

```
         ╱‾‾‾‾‾‾‾‾‾╲
        ╱            ╲
       ╱   Vision     ╲
      ╱     Cone       ╲
     ╱                  ╲
    ◄───── NPC ─────────►
     ╲   View Angle    ╱
      ╲    120°       ╱
       ╲            ╱
        ╲__________╱
              │
         View Distance
```

### Inspector Settings

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| View Distance | float | 20 | Max detection range in units |
| View Angle | float | 120 | Field of view in degrees |
| Eye Height | float | 1.5 | Raycast origin offset |
| Target Tag | string | "Player" | Tag to filter targets |
| Obstacle Mask | LayerMask | Everything | Layers that block vision |
| Target Mask | LayerMask | Everything | Layers containing targets |
| Max Targets | int | 10 | Maximum tracked targets |
| Max Raycasts/Tick | int | 3 | Performance limiter |

### Setup

1. Add `SightSensor` component to NPC **before** `NPCBrainController`
2. Configure vision settings in Inspector
3. Ensure target GameObjects have the correct tag

```csharp
// Component order matters!
gameObject.AddComponent<SightSensor>();      // First!
gameObject.AddComponent<MyNPCController>();  // Second
```

### Usage

```csharp
public class AlertGuard : NPCBrainController
{
    protected override void Awake()
    {
        base.Awake();
        
        // Perception is auto-detected SightSensor
        if (Perception != null)
        {
            Debug.Log($"Vision: {Perception.ViewDistance}m, {Perception.ViewAngle}°");
        }
    }
    
    private void Update()
    {
        // Check for visible targets
        if (Perception != null && Perception.HasVisibleTargets)
        {
            // Get closest target
            GameObject closest = Perception.ClosestTarget;
            Debug.Log($"I see: {closest.name}");
            
            // Get all visible targets
            foreach (var target in Perception.VisibleTargets)
            {
                float dist = Vector3.Distance(transform.position, target.transform.position);
                Debug.Log($"  - {target.name} at {dist:F1}m");
            }
        }
    }
}
```

### Manual Checks

```csharp
// Check if a position is in the view cone (ignoring obstacles)
bool inCone = Perception.IsInViewCone(suspiciousPosition);

// Check line of sight to a position
bool canSee = Perception.HasLineOfSight(targetPosition);

// Change target tag at runtime
Perception.SetTargetTag("Enemy");
```

### Tag Handling

The SightSensor handles missing tags gracefully:

```csharp
// If "Player" tag doesn't exist:
// - Error logged once (not spammed)
// - Sensor continues working but finds no targets
// - Use SetTargetTag() to change at runtime
```

---

## HearingSensor

### Overview

The HearingSensor detects sounds emitted through the `SoundManager` or `SoundEmitter` components.

```
                    ┌─────────────┐
                    │  Sound      │
      ╭─────────────│  Source     │
     ╱              └─────────────┘
    ╱  Sound Wave
   ╱
  ◄────── NPC (HearingSensor)
   ╲
    ╲  Hearing Range: 30m
     ╲
      ╰─────────────╮
                    │ (blocked by
                    │  obstacles?)
```

### Inspector Settings

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| Hearing Range | float | 30 | Max detection distance |
| Hearing Threshold | float | 0.1 | Min volume to detect |
| Sound Memory Duration | float | 5 | How long to remember sounds |

### Setup

1. Add `HearingSensor` to NPC (before controller)
2. Configure range and threshold
3. Emit sounds using `SoundManager` or `SoundEmitter`

### Usage

```csharp
public class ListeningGuard : NPCBrainController
{
    protected override void Awake()
    {
        base.Awake();
        
        // Subscribe to sound events
        OnSoundHeard += HandleSound;
    }
    
    private void HandleSound(SoundEvent sound)
    {
        Debug.Log($"Heard {sound.Type} at {sound.Position}");
        
        switch (sound.Type)
        {
            case SoundType.Gunshot:
                Blackboard.Set("investigatePosition", sound.Position);
                Blackboard.Set("alertLevel", 1f);
                break;
                
            case SoundType.Footstep:
                if (!Blackboard.Has("investigatePosition"))
                {
                    Blackboard.Set("investigatePosition", sound.Position);
                }
                break;
        }
    }
    
    protected override void OnDestroy()
    {
        OnSoundHeard -= HandleSound;
        base.OnDestroy();
    }
}
```

### Recent Sound Queries

```csharp
// Get all sounds heard in memory window
var recentSounds = Hearing.GetRecentSounds();
foreach (var sound in recentSounds)
{
    Debug.Log($"{sound.Type} from {sound.Source?.name} at {sound.EmitTime}");
}

// Check for specific sound type
if (Hearing.HasRecentSound(SoundType.Gunshot))
{
    // React to gunshot
}
```

---

## Memory System

### Overview

The Memory system remembers target positions with confidence decay over time.

### Memory Entry

```csharp
public class MemoryEntry
{
    public GameObject Target;     // The remembered object
    public Vector3 LastPosition;  // Where it was last seen
    public float LastSeenTime;    // When it was last seen
    public float Confidence;      // 0-1, decays over time
    public bool IsVisible;        // Currently visible?
}
```

### Usage with SightSensor

```csharp
// Memory is integrated with perception
if (Perception != null)
{
    // Targets are automatically added to memory when seen
    // Memory confidence decays when target is lost
    
    var memory = GetComponent<Memory>();
    if (memory != null)
    {
        // Check if we remember a specific target
        if (memory.TryGetMemory(target, out MemoryEntry entry))
        {
            Debug.Log($"Last saw {target.name} at {entry.LastPosition}");
            Debug.Log($"Confidence: {entry.Confidence:P0}");
        }
        
        // Get all remembered targets
        foreach (var mem in memory.GetAllMemories())
        {
            if (mem.Confidence > 0.5f)
            {
                // Still fairly sure about this one
            }
        }
    }
}
```

### Confidence Decay

```
Confidence
1.0 │████████████████
    │              ╲
0.5 │               ╲████████
    │                       ╲
  0 │────────────────────────╲───
    0   5   10   15   20   25   Time (seconds)
        │                   │
    Target Lost        Memory Forgotten
```

---

## Sound System

### SoundType Enum

```csharp
public enum SoundType
{
    Generic,    // Default sound
    Footstep,   // Walking/running
    Gunshot,    // Weapon fire
    Explosion,  // Large blast
    Voice,      // Speech/yelling
    Alert,      // Alarm systems
    Impact      // Physical collision
}
```

### SoundEvent

```csharp
public class SoundEvent
{
    public Vector3 Position;    // Where the sound originated
    public SoundType Type;      // What kind of sound
    public float Volume;        // 0-1, affects effective range
    public float EmitTime;      // When it was emitted
    public GameObject Source;   // What made the sound (can be null)
}
```

### SoundManager (Static)

Emit sounds from anywhere:

```csharp
// Simple emission
SoundManager.EmitSound(
    transform.position,  // Position
    SoundType.Gunshot,   // Type
    1.0f                 // Volume (1.0 = full range)
);

// With source reference
SoundManager.EmitSound(
    transform.position,
    SoundType.Footstep,
    0.3f,                // Quieter = shorter range
    gameObject           // Who made it
);
```

### SoundEmitter (Component)

Attach to objects that make sounds:

```csharp
// SoundEmitter attached to player
public class PlayerController : MonoBehaviour
{
    private SoundEmitter _soundEmitter;
    
    void Awake()
    {
        _soundEmitter = GetComponent<SoundEmitter>();
    }
    
    void Update()
    {
        if (IsWalking())
        {
            // Emit footstep sounds
            _soundEmitter.EmitSound(SoundType.Footstep, 0.3f);
        }
        
        if (Input.GetButtonDown("Fire"))
        {
            // Emit gunshot
            _soundEmitter.EmitSound(SoundType.Gunshot, 1.0f);
        }
    }
}
```

### Sound Range Calculation

```
Effective Range = Hearing Range × Volume

Example:
- Hearing Range: 30m
- Gunshot Volume: 1.0  → 30m range
- Footstep Volume: 0.3 → 9m range
```

---

## Events

### NPCBrainController Events

```csharp
public class MyNPC : NPCBrainController
{
    protected override void Awake()
    {
        base.Awake();
        
        // Target detection events
        OnTargetAcquired += (target) => {
            Debug.Log($"New target: {target.name}");
            Blackboard.Set("target", target);
        };
        
        OnTargetLost += (target) => {
            Debug.Log($"Lost target: {target.name}");
            Blackboard.SetWithTTL("lastKnownPosition", 
                target.transform.position, 15f);
            Blackboard.Remove("target");
        };
        
        // Sound events
        OnSoundHeard += (sound) => {
            Debug.Log($"Heard: {sound.Type}");
        };
    }
    
    protected override void OnDestroy()
    {
        // Events are automatically cleaned up in base.OnDestroy()
        base.OnDestroy();
    }
}
```

### Event Flow

```
[SightSensor.Tick()]
        │
        ├── Target enters vision cone
        │         │
        │         ▼
        │   OnTargetAcquired(target)
        │
        ├── Target leaves vision cone
        │         │
        │         ▼
        │   OnTargetLost(target)
        │
[HearingSensor.Tick()]
        │
        ├── Sound detected
        │         │
        │         ▼
        │   OnSoundHeard(soundEvent)
```

---

## Best Practices

### 1. Component Order

Add sensors BEFORE the controller:

```csharp
// Correct:
gameObject.AddComponent<SightSensor>();
gameObject.AddComponent<HearingSensor>();
gameObject.AddComponent<MyNPC>();

// Wrong (sensors won't be detected):
gameObject.AddComponent<MyNPC>();
gameObject.AddComponent<SightSensor>();
```

### 2. Create Tags Before Running

```csharp
// Tags must exist in Project Settings!
// Edit → Project Settings → Tags and Layers
// Add: Player, Enemy, etc.
```

### 3. Use Appropriate Ranges

```csharp
// Realistic ranges for stealth game:
SightSensor:
  - View Distance: 15-25m (human-like)
  - View Angle: 90-140° (peripheral vision)
  
HearingSensor:
  - Hearing Range: 20-40m
  - Gunshot volume: 1.0 (very loud)
  - Footstep volume: 0.2-0.4 (quiet)
```

### 4. Performance Tuning

```csharp
// Limit raycasts per tick
SightSensor:
  - Max Raycasts/Tick: 3 (default)
  - Max Targets: 5-10
  
// For many NPCs, stagger tick intervals
NPCBrainController:
  - Tick Interval: 0.1-0.2s (instead of every frame)
```

### 5. Layer Masks

```csharp
// Be specific with layer masks
SightSensor:
  - Obstacle Mask: Default, Terrain, Buildings
  - Target Mask: Player, Enemies
  
// This prevents NPCs from trying to see through everything
```

### 6. Debugging

```csharp
// Enable gizmos in Scene view
SightSensor:
  - Draw Gizmos: true
  - Gizmo Color Clear: green
  - Gizmo Color Alert: red
  
// Enable per-sensor logging
SightSensor:
  - Debug Logging: true (requires NPCBrainDebug.Enabled)
```

---

## Complete Example: Stealth Guard

```csharp
using UnityEngine;
using NPCBrain;
using NPCBrain.BehaviorTree;
using NPCBrain.BehaviorTree.Composites;
using NPCBrain.BehaviorTree.Actions;
using NPCBrain.Perception;

public class StealthGuard : NPCBrainController
{
    [SerializeField] private float _alertDecayRate = 0.1f;
    
    private float _alertLevel;
    
    protected override void Awake()
    {
        base.Awake();
        
        // Setup perception events
        OnTargetAcquired += HandleTargetSpotted;
        OnTargetLost += HandleTargetLost;
        OnSoundHeard += HandleSoundHeard;
        
        Blackboard.Set("alertLevel", 0f);
    }
    
    private void HandleTargetSpotted(GameObject target)
    {
        _alertLevel = 1f;
        Blackboard.Set("target", target);
        Blackboard.Set("alertLevel", _alertLevel);
        Debug.Log($"ALERT! Spotted {target.name}!");
    }
    
    private void HandleTargetLost(GameObject target)
    {
        Blackboard.SetWithTTL("lastKnownPosition", 
            target.transform.position, 20f);
        Blackboard.Remove("target");
        Debug.Log($"Lost sight of {target.name}");
    }
    
    private void HandleSoundHeard(SoundEvent sound)
    {
        float alertIncrease = sound.Type switch
        {
            SoundType.Gunshot => 0.8f,
            SoundType.Footstep => 0.2f,
            SoundType.Voice => 0.4f,
            _ => 0.1f
        };
        
        _alertLevel = Mathf.Clamp01(_alertLevel + alertIncrease);
        Blackboard.Set("alertLevel", _alertLevel);
        
        if (!Blackboard.Has("target"))
        {
            Blackboard.SetWithTTL("investigatePosition", sound.Position, 30f);
        }
    }
    
    private void LateUpdate()
    {
        // Decay alert over time
        if (!Blackboard.Has("target") && _alertLevel > 0)
        {
            _alertLevel = Mathf.Max(0, _alertLevel - _alertDecayRate * Time.deltaTime);
            Blackboard.Set("alertLevel", _alertLevel);
        }
    }
    
    protected override BTNode CreateBehaviorTree()
    {
        return new Selector(
            CreateChaseBehavior(),
            CreateInvestigateBehavior(),
            CreatePatrolBehavior()
        );
    }
    
    // ... behavior implementations
}
```

---

[← Utility AI](utility-ai.md) | [Criticality →](criticality.md)
