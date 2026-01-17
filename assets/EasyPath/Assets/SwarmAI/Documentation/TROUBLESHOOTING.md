# SwarmAI Troubleshooting Guide

Common issues and solutions when working with SwarmAI.

---

## Table of Contents

- [Demo Scene Issues](#demo-scene-issues)
- [Formation Problems](#formation-problems)
- [Movement Issues](#movement-issues)
- [Unity Warnings](#unity-warnings)
- [Performance Issues](#performance-issues)

---

## Demo Scene Issues

### "Agents don't move when I click"

**Symptoms:** Clicking on the ground in demo scenes does nothing.

**Possible Causes:**

1. **Missing SeekBehavior:** The FlockingDemo requires a SeekBehavior to move agents toward clicked targets.
   ```csharp
   // SeekBehavior must be added for click-to-move
   agent.AddBehavior(new SeekBehavior(), 1.5f);
   ```

2. **Agents not registered:** Check console for "Agents found: X" message. If 0, agents aren't being detected.
   - Ensure agents have `SwarmAgent` component
   - Check that `SwarmManager` exists in scene

3. **Raycast not hitting ground:** Ensure ground has a collider.

### "No console output in demo scenes"

**Solution:** Enable verbose debug logging:
1. Select the demo controller in hierarchy
2. Check "Verbose Debug" in Inspector
3. Play scene and check console

### "Input Manager deprecation warning"

**Message:** "This project uses Input Manager, which is marked for deprecation."

**Solution:** Change Unity's input handling setting:
1. Go to **Edit → Project Settings → Player**
2. Find **Other Settings → Configuration → Active Input Handling**
3. Change from "Both" to **"Input System Package (New)"**

SwarmAI demos use the new Input System - this warning appears when the legacy Input Manager is still enabled alongside it.

---

## Formation Problems

### Agents Oscillating Around Formation Positions

**Symptoms:** Agents move to formation positions but never settle, constantly jittering.

**Cause:** Using `FollowLeaderBehavior` or `ArriveBehavior` which don't have strong enough damping.

**Solution:** Use `FormationSlotBehavior` which is specifically designed for stable formations:

```csharp
// Replace FollowLeaderBehavior with FormationSlotBehavior
agent.ClearBehaviors();
agent.AddBehavior(new FormationSlotBehavior(3f, 0.7f, 0.5f), 1.5f);
```

### Formation Not Updating When Leader Moves

**Symptoms:** Leader moves but followers don't follow.

**Cause:** `SwarmFormation.UpdateSlotPositions()` not being called in Update.

**Solution:** Ensure your demo controller calls `_formation.UpdateSlotPositions()` every frame.

### "Agents won't hold formation positions"

**Symptoms:** Agents move to formation slots but keep oscillating, never settling.

**Cause:** Using `FollowLeaderBehavior` instead of `FormationSlotBehavior`.

**Why it happens:**
- `SwarmFormation.UpdateSlotPositions()` calls `agent.SetTarget(slotPosition)` every frame
- `FollowLeaderBehavior` calculates its own target (behind leader), ignoring `agent.TargetPosition`
- These two systems conflict, causing oscillation

**Solution:** Use `FormationSlotBehavior` for formation followers:

```csharp
// WRONG - conflicts with SwarmFormation
agent.AddBehavior(new FollowLeaderBehavior { Leader = leader }, 1.0f);
agent.AddBehavior(new SeparationBehavior(), 1.5f);  // Makes it worse!

// CORRECT - works with SwarmFormation
agent.ClearBehaviors();
agent.AddBehavior(new FormationSlotBehavior(
    slowingRadius: 2.5f,
    arrivalRadius: 0.7f,
    dampingFactor: 0.7f
), 1.0f);
// No separation needed - formation slots define spacing
```

### "SeparationBehavior breaks formations"

**Cause:** SeparationBehavior pushes agents away from neighbors, counteracting formation cohesion.

**Solution:** Remove SeparationBehavior from formation followers, or use very low weight (0.1-0.2).

### "Formation doesn't move when clicking"

**Cause:** Leader doesn't have a behavior to process click targets.

**Solution:** Add `ArriveBehavior` to the formation leader:

```csharp
// Setup leader with ArriveBehavior for click-to-move
_leaderArriveBehavior = new ArriveBehavior();
_leaderArriveBehavior.SlowingRadius = 3f;
_leaderArriveBehavior.ArrivalRadius = 0.5f;
_leaderArriveBehavior.IsActive = false;  // Activated on click
leader.AddBehavior(_leaderArriveBehavior, 1.0f);

// On click:
_leaderArriveBehavior.TargetPosition = clickPosition;
_leaderArriveBehavior.IsActive = true;
```

---

## Movement Issues

### "Agents don't respond to SetTarget()"

**Cause:** `SetTarget()` sets internal data but doesn't change agent state or add behaviors.

**Solution:** Either:
1. Add a behavior that uses `agent.TargetPosition` (like `FormationSlotBehavior`)
2. Also call `agent.SetState(new SeekingState(position))`

```csharp
// Option 1: Use FormationSlotBehavior (for formations)
agent.AddBehavior(new FormationSlotBehavior(), 1.0f);
agent.SetTarget(position);  // FormationSlotBehavior reads this

// Option 2: Use state for one-time movement
agent.SetTarget(position);
agent.SetState(new SeekingState(position));
```

### "Agents overshoot targets"

**Cause:** Using SeekBehavior instead of ArriveBehavior.

**Solution:** Use `ArriveBehavior` for destinations:
```csharp
var arrive = new ArriveBehavior(targetPosition);
arrive.SlowingRadius = 5f;   // Start slowing at 5 units
arrive.ArrivalRadius = 0.5f; // Stop at 0.5 units
agent.AddBehavior(arrive, 1.0f);
```

### "Agents keep moving forever"

**Cause:** WanderBehavior is active with no other constraints.

**Solution:** Lower WanderBehavior weight or disable when stationary:
```csharp
wanderBehavior.IsActive = false;  // Disable when at destination
```

---

## Unity Warnings

### "Some objects were not cleaned up when closing the scene"

**Message:** "(Did you spawn new GameObjects from OnDestroy?) The following scene GameObjects were found: SwarmManager"

**Cause:** During scene teardown, accessing `SwarmManager.Instance` creates a new SwarmManager because the old one was already destroyed.

**Solution:** Use `SwarmManager.HasInstance` instead of checking `Instance != null`:

```csharp
// WRONG - creates new instance during teardown
if (SwarmManager.Instance != null)
{
    SwarmManager.Instance.UnregisterAgent(this);
}

// CORRECT - just checks if instance exists
if (SwarmManager.HasInstance)
{
    SwarmManager.Instance.UnregisterAgent(this);
}
```

This is already fixed in SwarmAI v1.1.0+.

### "NullReferenceException in behavior"

**Cause:** Agent was destroyed but behavior is still being calculated.

**Solution:** Always null-check agent in `CalculateForce()`:
```csharp
public override Vector3 CalculateForce(SwarmAgent agent)
{
    if (agent == null) return Vector3.zero;
    // ... rest of calculation
}
```

---

## Performance Issues

### "Frame rate drops with many agents"

**Possible solutions:**

1. **Reduce neighbor radius:**
   ```csharp
   agent.NeighborRadius = 3f;  // Smaller = fewer checks
   ```

2. **Limit active behaviors:**
   ```csharp
   // Disable behaviors for distant agents
   float distToCamera = Vector3.Distance(agent.Position, Camera.main.transform.position);
   foreach (var behavior in agent.Behaviors)
   {
       behavior.IsActive = distToCamera < 50f;
   }
   ```

3. **Adjust spatial hash cell size:**
   ```csharp
   // In SwarmSettings, set CellSize to ~2x agent radius
   settings.SpatialHashCellSize = 4f;
   ```

4. **Use fewer rays for obstacle avoidance:**
   ```csharp
   var avoidance = new ObstacleAvoidanceBehavior();
   avoidance.RayCount = 3;  // Default is 5
   ```

### "Garbage collection spikes"

**Cause:** Allocating new lists every frame for neighbor queries.

**Solution:** Use the non-allocating versions of methods:

```csharp
// Allocating (creates garbage)
var neighbors = SwarmManager.Instance.GetNeighbors(position, radius);

// Non-allocating (reuse list)
private List<SwarmAgent> _neighborCache = new List<SwarmAgent>();

void Update()
{
    _neighborCache.Clear();
    SwarmManager.Instance.GetNeighbors(position, radius, _neighborCache);
}
```

---

## Debug Tips

### Enable Verbose Debug Logging

Demo controllers have a `_verboseDebug` option:
1. Select demo controller in hierarchy
2. Check "Verbose Debug" in Inspector
3. Watch console for detailed output

### Check Agent State

```csharp
Debug.Log($"Agent {agent.AgentId}: State={agent.CurrentStateType}, " +
          $"Target={agent.TargetPosition}, Velocity={agent.Velocity.magnitude:F2}");
```

### Check Behavior Forces

```csharp
foreach (var behavior in agent.Behaviors)
{
    Vector3 force = behavior.CalculateForce(agent);
    Debug.Log($"{behavior.Name}: Active={behavior.IsActive}, Force={force.magnitude:F2}");
}
```

### Visualize Formation Slots

Enable gizmos on `SwarmFormation` component:
1. Select formation object
2. Check "Show Debug Gizmos"
3. Turn on Gizmos in Scene view

---

*For more help, see the other documentation files or contact support.*
