# NPCBrain API Reference

## Core Classes

### NPCBrainController

Main component for NPC AI control.

```csharp
public class NPCBrainController : MonoBehaviour
```

**Properties:**
- `Blackboard Blackboard` - Shared data storage
- `SightSensor Perception` - Vision sensor (may be null)
- `CriticalityController Criticality` - Behavior tuning system
- `WaypointPath WaypointPath` - Patrol waypoints
- `BTNode BehaviorTree` - Root behavior tree node
- `NodeStatus LastStatus` - Result of last tick
- `bool IsPaused` - Whether brain is paused

**Methods:**
- `void Tick()` - Manually trigger one tick
- `void Pause()` - Pause behavior execution
- `void Resume()` - Resume after pause
- `void SetBehaviorTree(BTNode tree)` - Replace behavior tree
- `Vector3 GetCurrentWaypoint()` - Get current waypoint position
- `Vector3 AdvanceAndGetWaypoint()` - Advance to next waypoint

**Events:**
- `event Action<GameObject> OnTargetAcquired` - Target detected
- `event Action<GameObject> OnTargetLost` - Target lost
- `event Action<string> OnStateChanged` - State changed
- `event Action OnBrainPaused` - Brain paused
- `event Action OnBrainResumed` - Brain resumed

---

### Blackboard

Key-value store for NPC knowledge.

```csharp
public class Blackboard
```

**Methods:**
- `void Set<T>(string key, T value)` - Store a value
- `T Get<T>(string key, T defaultValue = default)` - Retrieve a value
- `bool Has(string key)` - Check if key exists
- `void Remove(string key)` - Remove a key
- `void Clear()` - Remove all keys

**Events:**
- `event Action<string, object> OnChanged` - Value changed

---

## Behavior Tree

### BTNode

Base class for all behavior tree nodes.

```csharp
public abstract class BTNode
```

**Properties:**
- `string Name` - Node name for debugging
- `NodeStatus Status` - Current execution status

**Methods (override in subclasses):**
- `protected abstract NodeStatus Tick(NPCBrainController brain)`
- `protected virtual void OnEnter(NPCBrainController brain)`
- `protected virtual void OnExit(NPCBrainController brain)`
- `public virtual void Reset()`
- `public virtual void Abort(NPCBrainController brain)`

---

### Composites

#### Selector
```csharp
new Selector(child1, child2, ...)
```
Tries children until one succeeds. Returns Failure if all fail.

#### Sequence
```csharp
new Sequence(child1, child2, ...)
```
Runs children in order. Returns Failure if any fails.

#### Parallel
```csharp
new Parallel(child1, child2, ...)
```
Runs all children simultaneously.

#### UtilitySelector
```csharp
new UtilitySelector(action1, action2, ...)
```
Chooses action based on utility scores using softmax selection.

---

### Decorators

#### Inverter
```csharp
new Inverter(child)
```
Flips Success ↔ Failure.

#### Repeater
```csharp
new Repeater(child, times)  // Repeat N times
new Repeater(child, -1)     // Repeat forever
```

#### Cooldown
```csharp
new Cooldown(child, duration)
```
Prevents re-execution for `duration` seconds.

#### Succeeder
```csharp
new Succeeder(child)
```
Always returns Success.

---

### Conditions

#### CheckBlackboard
```csharp
new CheckBlackboard<T>(key, predicate)
```
Checks if blackboard value satisfies predicate.

#### CheckDistance
```csharp
new CheckDistance(targetGetter, threshold, comparison)
```
Checks distance to a target.

#### CheckTargetVisible
```csharp
new CheckTargetVisible()
```
Checks if any target is visible to SightSensor.

---

### Actions

#### MoveTo
```csharp
new MoveTo(targetGetter, arrivalDistance, speed, timeout)
```
Moves NPC to target position.

#### Wait
```csharp
new Wait(duration)
new Wait(duration, onComplete)
```
Waits for specified duration.

#### SetBlackboard
```csharp
new SetBlackboard(key, valueGetter)
```
Sets a blackboard value.

#### Log
```csharp
new Log(message)
```
Logs a debug message.

#### LookAt
```csharp
new LookAt(targetGetter, rotationSpeed)
```
Rotates to face target.

#### AdvanceWaypoint
```csharp
new AdvanceWaypoint()
```
Advances to next waypoint in path.

---

## Utility AI

### UtilityAction

```csharp
new UtilityAction(name, behavior, baseScore, considerations...)
```

**Properties:**
- `string Name` - Action name
- `BTNode Action` - Behavior to execute
- `float BaseScore` - Base utility score
- `List<Consideration> Considerations` - Scoring factors

**Methods:**
- `float Score(NPCBrainController brain)` - Calculate total score

---

### Considerations

#### ConstantConsideration
```csharp
new ConstantConsideration(value)
```

#### BlackboardConsideration<T>
```csharp
new BlackboardConsideration<T>(name, key, normalizer, defaultValue)
```

#### DistanceConsideration
```csharp
new DistanceConsideration(name, targetGetter, maxDistance, invert)
```

#### TimeConsideration
```csharp
new TimeConsideration(name, blackboardKey, cooldown)
```

---

### Response Curves

#### LinearCurve
```csharp
new LinearCurve(slope, offset)
```

#### ExponentialCurve
```csharp
new ExponentialCurve(exponent)
```

#### StepCurve
```csharp
new StepCurve(threshold, below, above)
```

---

## Perception

### SightSensor

Vision cone detection component.

```csharp
public class SightSensor : MonoBehaviour
```

**Properties:**
- `float ViewDistance` - Max view distance
- `float ViewAngle` - Field of view angle
- `IReadOnlyList<GameObject> VisibleTargets` - Currently visible targets
- `bool HasVisibleTargets` - True if any targets visible
- `GameObject ClosestTarget` - Nearest visible target

**Methods:**
- `void Tick(NPCBrainController brain)` - Update detection
- `bool IsInViewCone(Vector3 position)` - Check if position is in cone
- `bool HasLineOfSight(Vector3 position)` - Check line of sight

---

### Memory

Remember targets after losing sight.

```csharp
public class Memory
```

**Properties:**
- `float MemoryDuration` - How long to remember (seconds)
- `float DecayRate` - Confidence decay per second
- `int Count` - Number of remembered targets

**Methods:**
- `void UpdateVisible(GameObject target, Vector3 position)` - Update seen target
- `void MarkLost(GameObject target)` - Mark target as lost
- `void Tick()` - Apply decay
- `TargetMemory GetMemory(GameObject target)` - Get memory for target
- `bool Remembers(GameObject target)` - Check if target is remembered
- `Vector3 GetPredictedPosition(GameObject target)` - Predict current position
- `GameObject GetMostRecentTarget()` - Get most recently seen
- `void Forget(GameObject target)` - Remove from memory
- `void Clear()` - Clear all memories

---

### TargetSelector

Prioritize targets by score.

```csharp
public class TargetSelector
```

**Properties:**
- `float MaxDistance` - Max distance to consider
- `ScoringWeights Weights` - Scoring weights
- `IReadOnlyList<ScoredTarget> ScoredTargets` - Last evaluation results

**Methods:**
- `IReadOnlyList<ScoredTarget> Evaluate(...)` - Score all known targets
- `GameObject SelectBest(...)` - Get highest priority target
- `GameObject SelectBestVisible(...)` - Get highest priority visible target

---

## Criticality

### CriticalityController

Automatic behavior tuning.

```csharp
public class CriticalityController
```

**Properties:**
- `float Temperature` - Current temperature (0.5-2.0)
- `float Inertia` - Action inertia (0-1)
- `float Entropy` - Action entropy

**Methods:**
- `void RecordAction(int actionId)` - Record completed action
- `void Update()` - Recalculate entropy and adjust temp
- `void Reset()` - Reset to initial state
- `void SetTemperature(float temperature)` - Manually set temperature

---

## Enums

### NodeStatus

```csharp
public enum NodeStatus
{
    Running,   // Still executing
    Success,   // Completed successfully
    Failure    // Failed
}
```
