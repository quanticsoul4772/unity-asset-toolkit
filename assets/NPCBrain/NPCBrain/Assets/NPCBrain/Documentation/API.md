# NPCBrain API Reference

Complete API documentation for all public classes and methods.

## Table of Contents

- [Core](#core)
  - [NPCBrainController](#npcbraincontroller)
  - [Blackboard](#blackboard)
  - [WaypointPath](#waypointpath)
- [Behavior Tree](#behavior-tree)
  - [BTNode](#btnode)
  - [Composites](#composites)
  - [Decorators](#decorators)
  - [Actions](#actions)
  - [Conditions](#conditions)
- [Utility AI](#utility-ai)
  - [UtilitySelector](#utilityselector)
  - [UtilityAction](#utilityaction)
  - [Considerations](#considerations)
  - [Curves](#curves)
- [Perception](#perception)
  - [SightSensor](#sightsensor)
  - [HearingSensor](#hearingsensor)
  - [SoundEmitter](#soundemitter)
  - [SoundManager](#soundmanager)
  - [Memory](#memory)
  - [TargetSelector](#targetselector)
- [Criticality](#criticality)
  - [CriticalityController](#criticalitycontroller)

---

## Core

### NPCBrainController

Main component for NPC AI control.

**Namespace**: `NPCBrain`

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Blackboard` | `Blackboard` | Shared data storage for behavior tree nodes |
| `Perception` | `SightSensor` | Sight sensor component (may be null) |
| `Hearing` | `HearingSensor` | Hearing sensor component (may be null) |
| `Criticality` | `CriticalityController` | Controls exploration vs exploitation |
| `WaypointPath` | `WaypointPath` | Waypoint path for patrol behaviors |
| `BehaviorTree` | `BTNode` | Root node of the behavior tree |
| `LastStatus` | `NodeStatus` | Result of last behavior tree tick |
| `IsPaused` | `bool` | True if brain is paused |

#### Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `Tick()` | `void` | Manually triggers one tick |
| `Pause()` | `void` | Pauses behavior tree execution |
| `Resume()` | `void` | Resumes after pause |
| `SetBehaviorTree(BTNode)` | `void` | Replaces current behavior tree |
| `AdvanceAndGetWaypoint()` | `Vector3` | Advances to next waypoint, returns position |
| `GetCurrentWaypoint()` | `Vector3` | Returns current waypoint position |
| `SetWaypointPath(WaypointPath)` | `void` | Sets the waypoint path |

#### Events

| Event | Signature | Description |
|-------|-----------|-------------|
| `OnTargetAcquired` | `Action<GameObject>` | Raised when new target detected |
| `OnTargetLost` | `Action<GameObject>` | Raised when target lost |
| `OnStateChanged` | `Action<string>` | Raised on state change |
| `OnBrainPaused` | `Action` | Raised when paused |
| `OnBrainResumed` | `Action` | Raised when resumed |
| `OnSoundHeard` | `Action<SoundEvent>` | Raised when a sound is heard |

#### Virtual Methods

```csharp
protected virtual BTNode CreateBehaviorTree()
```
Override to define the NPC's behavior tree.

---

### Blackboard

Key-value data store with TTL support.

**Namespace**: `NPCBrain`

#### Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `Set<T>(string key, T value)` | `void` | Sets a persistent value |
| `SetWithTTL<T>(string key, T value, float ttl)` | `void` | Sets a value that expires after ttl seconds |
| `Get<T>(string key, T defaultValue)` | `T` | Gets value or default |
| `TryGet<T>(string key, out T value)` | `bool` | Tries to get value |
| `Has(string key)` | `bool` | Checks if key exists |
| `Remove(string key)` | `bool` | Removes a key |
| `Clear()` | `void` | Removes all entries |
| `CleanupExpired()` | `void` | Removes expired TTL entries |

#### Events

| Event | Signature | Description |
|-------|-----------|-------------|
| `OnValueChanged` | `Action<string, object>` | Raised when value set |
| `OnValueExpired` | `Action<string>` | Raised when TTL expires |

---

### WaypointPath

Manages a sequence of waypoints for patrol behaviors.

**Namespace**: `NPCBrain`

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Waypoints` | `Transform[]` | Array of waypoint transforms |
| `CurrentIndex` | `int` | Current waypoint index |
| `Loop` | `bool` | Whether to loop back to start |

#### Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `GetCurrent()` | `Vector3` | Returns current waypoint position |
| `AdvanceAndGetWaypoint()` | `Vector3` | Advances and returns new position |
| `Reset()` | `void` | Resets to first waypoint |

---

## Behavior Tree

### BTNode

Base class for all behavior tree nodes.

**Namespace**: `NPCBrain.BehaviorTree`

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Node name for debugging |
| `Status` | `NodeStatus` | Current execution status |

#### Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `Execute(NPCBrainController)` | `NodeStatus` | Executes the node |
| `Reset()` | `void` | Resets node state |
| `Abort(NPCBrainController)` | `void` | Aborts running execution |

### NodeStatus

```csharp
public enum NodeStatus
{
    Success,   // Node completed successfully
    Failure,   // Node failed
    Running    // Node still executing
}
```

---

### Composites

**Namespace**: `NPCBrain.BehaviorTree.Composites`

#### Selector

Tries children in order until one succeeds.

```csharp
new Selector(child1, child2, child3)
```

| Child Result | Selector Result |
|--------------|----------------|
| Success | Stop, return Success |
| Failure | Try next child |
| Running | Return Running |
| All Failure | Return Failure |

#### Sequence

Runs children in order until one fails.

```csharp
new Sequence(child1, child2, child3)
```

| Child Result | Sequence Result |
|--------------|----------------|
| Success | Run next child |
| Failure | Stop, return Failure |
| Running | Return Running |
| All Success | Return Success |

#### Parallel

Runs all children simultaneously each tick.

```csharp
new Parallel(child1, child2, child3)
new Parallel(Parallel.Policy.RequireOne, child1, child2, child3)
```

**Policies:**
- `RequireAll` (default): Returns Success when ALL children succeed, Failure if ANY fail
- `RequireOne`: Returns Success when ANY child succeeds, Failure if ALL fail

Running children are aborted when the Parallel completes.

#### UtilitySelector

Selects action based on utility scores.

```csharp
new UtilitySelector(action1, action2, action3)
```

Uses Criticality temperature for selection randomness.

---

### Decorators

**Namespace**: `NPCBrain.BehaviorTree.Decorators`

#### Inverter

Inverts child result (Success ↔ Failure).

```csharp
new Inverter(child)
```

#### Repeater

Repeats child N times or infinitely.

```csharp
new Repeater(child, times: 3)    // Repeat 3 times
new Repeater(child, times: -1)   // Repeat forever
```

#### Cooldown

Prevents child execution for a duration after success.

```csharp
new Cooldown(child, cooldownSeconds: 5f)
```

#### Succeeder

Always returns Success regardless of child result. Running is passed through.

```csharp
new Succeeder(child)
```

| Child Result | Succeeder Result |
|--------------|------------------|
| Success | Success |
| Failure | Success |
| Running | Running |

---

### Actions

**Namespace**: `NPCBrain.BehaviorTree.Actions`

#### MoveTo

Moves NPC toward a target position.

```csharp
new MoveTo(
    Func<Vector3> getTarget,     // Function returning target position
    float stoppingDistance,       // Distance to stop at
    float speed,                  // Movement speed (default: 5)
    float rotationSpeed           // Rotation speed (default: 360)
)
```

#### Wait

Waits for a duration.

```csharp
new Wait(float seconds)
```

#### SetBlackboard

Sets a blackboard value.

```csharp
new SetBlackboard(string key, Func<object> getValue)
new SetBlackboard(string key, object value)
```

#### AdvanceWaypoint

Advances to the next waypoint in the NPC's WaypointPath and returns Success.

```csharp
new AdvanceWaypoint()
```

**Returns:** Always `Success` (advances waypoint index as side effect)

**Note:** Requires a `WaypointPath` component assigned to the NPCBrainController.

#### ClearBlackboardKey

Removes a key from the blackboard and returns Success.

```csharp
new ClearBlackboardKey(string key)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `key` | `string` | The blackboard key to remove |

**Returns:** Always `Success` (removes key if it exists, no-op if it doesn't)

#### Log

Logs a debug message and returns Success. Useful for debugging behavior tree execution.

```csharp
// Static message
new Log("Starting patrol")

// Dynamic message
new Log(brain => $"{brain.name} reached waypoint")

// With log level
new Log("Warning message", Log.LogLevel.Warning)
new Log("Error message", Log.LogLevel.Error)
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `message` | `string` | Static message to log |
| `getMessage` | `Func<NPCBrainController, string>` | Function returning dynamic message |
| `level` | `Log.LogLevel` | Log level: Info, Warning, Error (default: Info) |

#### LookAt

Rotates the NPC to face a target position. Returns Running while rotating, Success when facing target.

```csharp
// Look at a position
new LookAt(
    brain => brain.Blackboard.Get<Vector3>("targetPosition"),
    rotationSpeed: 360f,    // degrees per second (0 = instant)
    angleTolerance: 5f      // degrees
)

// Look at a GameObject
new LookAt(
    brain => brain.Blackboard.Get<GameObject>("target"),
    rotationSpeed: 180f
)

// Instant rotation
new LookAt(brain => GetEnemyPosition(), rotationSpeed: 0f)
```

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `getTargetPosition` | `Func<NPCBrainController, Vector3>` | required | Target position to face |
| `getTarget` | `Func<NPCBrainController, GameObject>` | required | Target object to face |
| `rotationSpeed` | `float` | 360 | Degrees per second (0 = instant) |
| `angleTolerance` | `float` | 5 | Angle tolerance to consider "facing" |

---

### Conditions

**Namespace**: `NPCBrain.BehaviorTree.Conditions`

#### CheckBlackboard

Checks a blackboard value.

```csharp
new CheckBlackboard(string key, object expectedValue)
new CheckBlackboard<T>(string key, Func<T, bool> predicate)
```

#### CheckDistance

Checks distance to a position.

```csharp
new CheckDistance(
    Func<Vector3> getTarget,
    float threshold,
    bool lessThan  // true = < threshold, false = > threshold
)
```

#### CheckTargetVisible

Checks if a target is visible to the NPC's SightSensor.

```csharp
// Check if ANY target is visible
new CheckTargetVisible()

// Check if a SPECIFIC target is visible
new CheckTargetVisible(brain => brain.Blackboard.Get<GameObject>("player"))
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `getTarget` | `Func<NPCBrainController, GameObject>` | Optional. If provided, checks for specific target. If omitted, checks for any visible target. |

**Returns:**
- `Success` if target(s) visible
- `Failure` if no targets visible or no SightSensor attached

#### CheckSoundHeard

Checks if a sound was heard by the NPC's HearingSensor.

```csharp
// Check if ANY sound was heard
new CheckSoundHeard()

// Check for minimum sound type priority
new CheckSoundHeard(SoundType.Gunshot)  // Gunshot or higher

// Custom predicate
new CheckSoundHeard(sound => sound.EffectiveVolume > 0.7f)

// Check for specific source
new CheckSoundHeard(brain => brain.Blackboard.Get<GameObject>("player"))
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `minimumType` | `SoundType` | Minimum sound type to match |
| `predicate` | `Func<SoundEvent, bool>` | Custom filter function |
| `getSource` | `Func<NPCBrainController, GameObject>` | Check for specific source |

**Returns:**
- `Success` if matching sound(s) heard
- `Failure` if no matching sounds or no HearingSensor attached

---

## Utility AI

### UtilitySelector

Composite node that selects actions based on utility scores.

**Namespace**: `NPCBrain.BehaviorTree.Composites`

```csharp
new UtilitySelector(params UtilityAction[] actions)
```

Uses softmax with Criticality temperature to select actions.

---

### UtilityAction

An action with a utility score.

**Namespace**: `NPCBrain.UtilityAI`

```csharp
new UtilityAction(
    string name,                          // Action name
    BTNode behavior,                      // Behavior to execute
    float baseScore,                      // Base utility score
    params Consideration[] considerations // Score modifiers
)
```

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Action name |
| `Id` | `int` | Unique action ID |
| `BaseScore` | `float` | Base utility score |

#### Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `CalculateScore(NPCBrainController)` | `float` | Calculates total utility score |

---

### Considerations

**Namespace**: `NPCBrain.UtilityAI`

#### Consideration (Base Class)

```csharp
public abstract class Consideration
{
    public ResponseCurve Curve { get; set; }
    public abstract float Evaluate(NPCBrainController brain);
}
```

#### ConstantConsideration

Returns a constant value.

```csharp
new ConstantConsideration(float value)
```

#### BlackboardConsideration<T>

Reads value from blackboard.

```csharp
new BlackboardConsideration<float>(
    string name,                // Display name
    string key,                 // Blackboard key
    Func<T, float> normalizer,  // Converts T to 0-1 range
    T defaultValue              // Value if key missing
)

// With response curve:
new BlackboardConsideration<float>(
    string name,
    string key,
    Func<T, float> normalizer,
    T defaultValue,
    ResponseCurve curve
)
```

#### DistanceConsideration

Scores based on distance to target.

```csharp
new DistanceConsideration(
    Func<Vector3> getTarget,
    float maxDistance,
    bool invertScore  // true = closer is higher
)
```

#### TimeConsideration

Scores based on time since an event.

```csharp
new TimeConsideration(
    Func<float> getTimestamp,
    float maxTime
)
```

#### RangeConsideration

Scores based on whether target is in range.

```csharp
new RangeConsideration(
    Func<float> getDistance,
    float minRange,
    float maxRange
)
```

---

### Curves

**Namespace**: `NPCBrain.UtilityAI.Curves`

#### LinearCurve

```csharp
new LinearCurve()           // f(x) = x
new LinearCurve(slope, intercept)  // f(x) = slope * x + intercept
```

#### ExponentialCurve

```csharp
new ExponentialCurve(exponent)  // f(x) = x^exponent
```

#### StepCurve

```csharp
new StepCurve(threshold)  // f(x) = x >= threshold ? 1 : 0
```

---

## Perception

### SightSensor

Vision cone sensor for detecting targets.

**Namespace**: `NPCBrain.Perception`

#### Inspector Properties

| Property | Default | Description |
|----------|---------|-------------|
| `View Distance` | 20 | Maximum sight range |
| `View Angle` | 120 | FOV in degrees |
| `Eye Height` | 1.5 | Raycast origin height |
| `Obstacle Mask` | Everything | Layers that block sight |
| `Target Mask` | Everything | Layers containing targets |
| `Target Tag` | "Player" | Tag to filter targets |
| `Max Targets` | 10 | Maximum tracked targets |
| `Max Raycasts Per Tick` | 3 | Performance limit |
| `Draw Gizmos` | true | Show vision cone in editor |

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `ViewDistance` | `float` | Maximum view distance |
| `ViewAngle` | `float` | Field of view angle |
| `VisibleTargets` | `IReadOnlyList<GameObject>` | Currently visible targets |
| `HasVisibleTargets` | `bool` | True if any targets visible |
| `ClosestTarget` | `GameObject` | Nearest visible target |

#### Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `Tick(NPCBrainController)` | `void` | Updates sensor detection |
| `IsInViewCone(Vector3)` | `bool` | Checks if position in FOV |
| `HasLineOfSight(Vector3)` | `bool` | Checks for clear line of sight |

---

### HearingSensor

Sound detection sensor for hearing sounds emitted by SoundEmitter or SoundManager.

**Namespace**: `NPCBrain.Perception`

#### Inspector Properties

| Property | Default | Description |
|----------|---------|-------------|
| `Hearing Range` | 30 | Maximum hearing distance |
| `Hearing Threshold` | 0.1 | Minimum volume to hear (0-1) |
| `Ear Height` | 1.5 | Height offset for hearing position |
| `Minimum Priority` | Ambient | Minimum sound type to detect |
| `Ignore Tags` | [] | Custom tags to ignore |
| `Ignore Own Sounds` | true | Ignore sounds from self |
| `Check Occlusion` | false | Raycast to check for obstacles |
| `Occlusion Mask` | Everything | Layers that block sound |
| `Occlusion Damping` | 0.3 | Volume multiplier through walls |
| `Sound Memory Duration` | 3 | How long to remember sounds |
| `Max Remembered Sounds` | 10 | Maximum sounds to track |

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `HearingRange` | `float` | Maximum hearing distance |
| `HearingThreshold` | `float` | Minimum volume threshold |
| `HeardSounds` | `IReadOnlyList<SoundEvent>` | Sounds heard this tick |
| `HasHeardSounds` | `bool` | True if any sounds heard |
| `HighestPrioritySound` | `SoundEvent` | Highest priority sound |
| `MostRecentSound` | `SoundEvent` | Most recent sound heard |

#### Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `Tick(NPCBrainController)` | `void` | Updates sensor detection |
| `HeardSoundType(SoundType)` | `bool` | Checks if type was heard |
| `HeardSoundWithTag(string)` | `bool` | Checks if tag was heard |
| `HeardSoundFromSource(GameObject)` | `bool` | Checks if source was heard |
| `GetDirectionToHighestPrioritySound()` | `Vector3` | Direction to loudest sound |

---

### SoundEmitter

Component that emits sounds detectable by HearingSensor.

**Namespace**: `NPCBrain.Perception`

#### Inspector Properties

| Property | Default | Description |
|----------|---------|-------------|
| `Sound Type` | Footstep | Category of sound |
| `Volume` | 0.5 | Base volume (0-1) |
| `Radius` | 20 | Maximum audible distance |
| `Custom Tag` | "" | Optional tag for filtering |
| `Mode` | Manual | Emission mode (Manual/Continuous/OnEnable) |
| `Continuous Interval` | 0.5 | Interval for continuous mode |

#### Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `EmitSound()` | `SoundEvent` | Emit with default settings |
| `EmitSound(float)` | `SoundEvent` | Emit with volume multiplier |
| `EmitSound(SoundType, float, float)` | `SoundEvent` | Emit with custom settings |

#### Static Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `EmitAt(Vector3, SoundType, float, float, GameObject)` | `SoundEvent` | Emit at position |
| `EmitFootstepAt(Vector3, float, GameObject)` | `SoundEvent` | Emit footstep |
| `EmitGunshotAt(Vector3, float, GameObject)` | `SoundEvent` | Emit gunshot |
| `EmitExplosionAt(Vector3, float, GameObject)` | `SoundEvent` | Emit explosion |

---

### SoundManager

Static manager for registering and querying sounds.

**Namespace**: `NPCBrain.Perception`

#### Static Properties

| Property | Type | Description |
|----------|------|-------------|
| `MaxSoundAge` | `float` | How long sounds remain active (default: 1s) |
| `ActiveSoundCount` | `int` | Number of active sounds |

#### Static Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `RegisterSound(SoundEvent)` | `void` | Register a sound event |
| `EmitSound(Vector3, SoundType, float, float, GameObject, string)` | `SoundEvent` | Create and register sound |
| `GetSoundsInRange(Vector3, float)` | `IEnumerable<SoundEvent>` | Get sounds in range |
| `GetSoundsInRangeNonAlloc(Vector3, float, List<SoundEvent>)` | `void` | Get sounds (no allocation) |
| `CleanupOldSounds()` | `void` | Remove expired sounds |
| `ClearAll()` | `void` | Clear all sounds |
| `EmitFootstep(Vector3, float, GameObject)` | `SoundEvent` | Convenience method |
| `EmitVoice(Vector3, float, GameObject)` | `SoundEvent` | Convenience method |
| `EmitGunshot(Vector3, float, GameObject)` | `SoundEvent` | Convenience method |
| `EmitExplosion(Vector3, float, GameObject)` | `SoundEvent` | Convenience method |
| `EmitImpact(Vector3, float, GameObject)` | `SoundEvent` | Convenience method |
| `EmitAlarm(Vector3, float, GameObject)` | `SoundEvent` | Convenience method |

---

### SoundEvent

Data class representing a sound event.

**Namespace**: `NPCBrain.Perception`

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Position` | `Vector3` | Sound origin position |
| `Volume` | `float` | Base volume (0-1) |
| `Radius` | `float` | Maximum audible distance |
| `Type` | `SoundType` | Sound category |
| `CustomTag` | `string` | Optional custom identifier |
| `Source` | `GameObject` | Source object (may be null) |
| `Timestamp` | `float` | Time when emitted |
| `EffectiveVolume` | `float` | Volume after attenuation |
| `Priority` | `float` | Calculated priority score |
| `Age` | `float` | Seconds since emitted |

#### Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `GetVolumeAtPosition(Vector3)` | `float` | Get attenuated volume |
| `CalculatePriority(Vector3, float)` | `float` | Calculate priority score |

---

### SoundType

Enum for sound categories, ordered by priority.

**Namespace**: `NPCBrain.Perception`

| Value | Priority | Description |
|-------|----------|-------------|
| `Ambient` | 0 | Background noise |
| `Footstep` | 1 | Movement sounds |
| `Voice` | 2 | Speech, grunts |
| `Impact` | 3 | Doors, objects |
| `Alarm` | 4 | Alert sounds |
| `Gunshot` | 5 | Weapons fire |
| `Explosion` | 6 | Explosions (highest) |

---

### Memory

Stores memory of targets with decay.

**Namespace**: `NPCBrain.Perception`

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `MemoryDuration` | `float` | Seconds before memory expires (default: 10) |
| `DecayRate` | `float` | Confidence decay per second (default: 0.1) |
| `Memories` | `IReadOnlyDictionary` | All current memories |
| `Count` | `int` | Number of remembered targets |

#### Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `UpdateVisible(GameObject, Vector3)` | `void` | Updates memory for visible target |
| `MarkLost(GameObject)` | `void` | Marks target as no longer visible |
| `Tick()` | `void` | Updates decay and removes expired |
| `GetMemory(GameObject)` | `TargetMemory` | Gets memory for target |
| `Remembers(GameObject)` | `bool` | Checks if target is remembered |
| `GetPredictedPosition(GameObject)` | `Vector3` | Predicts current position |
| `GetMostRecentTarget()` | `GameObject` | Gets most recently seen target |
| `Clear()` | `void` | Clears all memories |
| `Forget(GameObject)` | `void` | Removes specific target |

#### TargetMemory

| Property | Type | Description |
|----------|------|-------------|
| `Target` | `GameObject` | The remembered target |
| `LastKnownPosition` | `Vector3` | Last seen position |
| `LastSeenTime` | `float` | Time when last seen |
| `TimeSinceLastSeen` | `float` | Seconds since last seen |
| `IsCurrentlyVisible` | `bool` | True if currently visible |
| `Confidence` | `float` | Memory confidence (1.0 to 0.0) |
| `LastKnownVelocity` | `Vector3` | Last known movement direction |

---

### TargetSelector

Prioritizes targets based on scoring.

**Namespace**: `NPCBrain.Perception`

#### Constructor

```csharp
new TargetSelector()                    // Default weights
new TargetSelector(ScoringWeights)      // Custom weights
```

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `MaxDistance` | `float` | Maximum distance to consider (default: 50) |
| `Weights` | `ScoringWeights` | Current scoring weights |
| `ScoredTargets` | `IReadOnlyList<ScoredTarget>` | Results from last evaluation |

#### Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `Evaluate(...)` | `IReadOnlyList<ScoredTarget>` | Scores all known targets |
| `SelectBest(...)` | `GameObject` | Returns highest priority target |
| `SelectBestVisible(...)` | `GameObject` | Returns highest priority visible target |

#### ScoringWeights

| Property | Default | Description |
|----------|---------|-------------|
| `Distance` | 1.0 | Weight for distance (closer = higher) |
| `Angle` | 0.5 | Weight for angle (centered = higher) |
| `Confidence` | 0.3 | Weight for memory confidence |
| `ThreatLevel` | 1.0 | Weight for threat from blackboard |
| `VisibilityBonus` | 0.5 | Bonus for visible targets |
| `HearingBonus` | 0.3 | Bonus for recently heard targets |
| `HearingBonusWindow` | 5.0 | Time window for hearing bonus (seconds) |

---

## Criticality

### CriticalityController

Controls exploration vs exploitation through entropy-based adaptation.

**Namespace**: `NPCBrain.Criticality`

#### Properties

| Property | Type | Range | Description |
|----------|------|-------|-------------|
| `Temperature` | `float` | 0.5 - 2.0 | Selection randomness |
| `Inertia` | `float` | 0.0 - 1.0 | Action persistence |
| `Entropy` | `float` | 0.0 - ~2.0 | Current behavior variety |

#### Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `RecordAction(int actionId)` | `void` | Records an action execution |
| `Update()` | `void` | Updates entropy, temperature, inertia |
| `Reset()` | `void` | Resets to default values |
| `SetTemperature(float)` | `void` | Manually sets temperature (clamped) |

#### Behavior

| Condition | Entropy | Temperature | Inertia |
|-----------|---------|-------------|---------|
| Repetitive actions | Low | Increases | High |
| Varied actions | High | Decreases | Low |

---

## Editor Tools

### Debug Window

Access via: **Menu > NPCBrain > Open Debug Window**

Features:
- Real-time NPC monitoring
- Behavior tree visualization
- Blackboard contents
- Criticality values
- Perception status

### Demo Scene Generator

Access via: **Menu > NPCBrain > Create [Demo] Scene**

Creates pre-configured demo scenes for testing.
