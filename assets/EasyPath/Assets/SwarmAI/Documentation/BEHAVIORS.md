# SwarmAI Behaviors Guide

Complete documentation for all steering behaviors in SwarmAI.

---

## Table of Contents

- [Overview](#overview)
- [Basic Behaviors](#basic-behaviors)
  - [SeekBehavior](#seekbehavior)
  - [FleeBehavior](#fleebehavior)
  - [ArriveBehavior](#arrivebehavior)
  - [WanderBehavior](#wanderbehavior)
- [Flocking Behaviors](#flocking-behaviors)
  - [SeparationBehavior](#separationbehavior)
  - [AlignmentBehavior](#alignmentbehavior)
  - [CohesionBehavior](#cohesionbehavior)
- [Navigation Behaviors](#navigation-behaviors)
  - [ObstacleAvoidanceBehavior](#obstacleavoidancebehavior)
  - [FollowLeaderBehavior](#followleaderbehavior)
- [Formation Behaviors](#formation-behaviors)
  - [FormationSlotBehavior](#formationslotbehavior)
- [Creating Custom Behaviors](#creating-custom-behaviors)

---

## Overview

### What are Steering Behaviors?

Steering behaviors are modular forces that influence how agents move. Each behavior calculates a `Vector3` force that is combined with other behaviors to determine the agent's final movement.

### How Behaviors Work

1. Each behavior calculates a steering force
2. Forces are multiplied by their weights
3. All forces are summed together
4. The result is clamped to `MaxForce`
5. Force is applied as acceleration (`F = ma`)

### Adding Behaviors

```csharp
// Add a behavior with a weight
agent.AddBehavior(new SeekBehavior(), 1.0f);

// Behaviors can have their own weight property too
var behavior = new SeparationBehavior();
behavior.Weight = 1.5f;  // This multiplies with the AddBehavior weight
agent.AddBehavior(behavior, 1.0f);
```

### Managing Behaviors

```csharp
// Enable/disable a behavior
behavior.IsActive = false;

// Change weight at runtime
behavior.Weight = 2.0f;

// Remove a specific behavior
agent.RemoveBehavior(behavior);

// Remove all behaviors of a type
agent.RemoveBehaviorsOfType<WanderBehavior>();

// Clear all behaviors
agent.ClearBehaviors();
```

---

## Basic Behaviors

### SeekBehavior

Steers toward a target position.

#### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `TargetPosition` | `Vector3` | zero | Target position to seek |
| `TargetTransform` | `Transform` | null | Target transform (updates automatically) |

#### Usage

```csharp
// Seek a fixed position
var seek = new SeekBehavior();
seek.TargetPosition = new Vector3(10, 0, 10);
agent.AddBehavior(seek, 1.0f);

// Seek a moving target
seek.TargetTransform = enemy.transform;
```

#### How It Works

1. Calculate direction to target
2. Desired velocity = direction × MaxSpeed
3. Steering = desired velocity - current velocity

```
     Target
        ●
       ↗
      /
     /  Steering Force
    ●────→
  Agent   Current Velocity
```

---

### FleeBehavior

Steers away from a threat position.

#### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ThreatPosition` | `Vector3` | zero | Position to flee from |
| `ThreatTransform` | `Transform` | null | Threat transform |
| `PanicDistance` | `float` | 10.0 | Only flee within this distance |

#### Usage

```csharp
// Flee from a position
var flee = new FleeBehavior();
flee.ThreatPosition = enemyPosition;
flee.PanicDistance = 15f;  // Only flee if within 15 units
agent.AddBehavior(flee, 2.0f);  // Higher weight for urgency

// Flee from a moving threat
flee.ThreatTransform = predator.transform;
```

#### How It Works

1. If distance > PanicDistance, return zero force
2. Calculate direction away from threat
3. Desired velocity = direction × MaxSpeed
4. Steering = desired velocity - current velocity

---

### ArriveBehavior

Steers toward a target with smooth deceleration.

#### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `TargetPosition` | `Vector3` | zero | Target position |
| `TargetTransform` | `Transform` | null | Target transform |
| `SlowingRadius` | `float` | 5.0 | Start slowing at this distance |
| `ArrivalRadius` | `float` | 0.5 | Stop at this distance |
| `Falloff` | `FalloffType` | Linear | Deceleration curve |

#### FalloffType

| Type | Description |
|------|-------------|
| `Linear` | Constant deceleration |
| `Quadratic` | Slower start, faster end |
| `SquareRoot` | Faster start, slower end |

#### Usage

```csharp
// Arrive with smooth stopping
var arrive = new ArriveBehavior();
arrive.TargetPosition = destination;
arrive.SlowingRadius = 8f;
arrive.ArrivalRadius = 1f;
arrive.Falloff = ArriveBehavior.FalloffType.Quadratic;
agent.AddBehavior(arrive, 1.0f);
```

#### How It Works

```
                    SlowingRadius
           ←──────────────────────────→
           
  ●════════════════════════════════════●
Start        Gradual Slowdown        Target
           Speed ─────────────→ 0
```

---

### WanderBehavior

Produces smooth, random movement for idle states.

#### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `WanderRadius` | `float` | from Settings | Circle radius |
| `WanderDistance` | `float` | from Settings | Circle distance ahead |
| `WanderJitter` | `float` | from Settings | Randomness (deg/sec) |

#### Usage

```csharp
// Basic wander
var wander = new WanderBehavior();
agent.AddBehavior(wander, 0.5f);  // Low weight for subtle effect

// Custom wander parameters
var customWander = new WanderBehavior();
customWander.WanderRadius = 3f;
customWander.WanderDistance = 6f;
customWander.WanderJitter = 100f;  // More erratic
agent.AddBehavior(customWander, 0.3f);
```

#### How It Works

The wander behavior uses a "wander circle" technique:

1. Project a circle in front of the agent
2. Pick a random point on the circle
3. Seek that point
4. Jitter the point each frame for smooth variation

```
              Wander Circle
                  ○
                 /|\
                / | \
               /  |  \
    Agent ●───────●───────→ Target on circle
              Distance
```

---

## Flocking Behaviors

These three behaviors combine to create emergent flocking (boids) behavior.

### SeparationBehavior

Steers away from nearby neighbors to avoid crowding.

#### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `NeighborRadius` | `float` | 3.0 | Detection radius |
| `Falloff` | `FalloffType` | Linear | Force falloff with distance |

#### FalloffType

| Type | Description |
|------|-------------|
| `Linear` | Force decreases linearly |
| `Squared` | Force decreases with square of distance |

#### Usage

```csharp
// Standard separation
var separation = new SeparationBehavior(3f);
agent.AddBehavior(separation, 1.5f);

// Stronger close-range separation
var strongSep = new SeparationBehavior(2f);
strongSep.Falloff = SeparationBehavior.FalloffType.Squared;
agent.AddBehavior(strongSep, 2.0f);
```

#### How It Works

```
    Neighbor ●
              \
               \ Push away
                \
    Agent ●←─────
                /
               /
              /
    Neighbor ●
```

The force is the sum of vectors pointing away from each neighbor, weighted by distance.

---

### AlignmentBehavior

Steers to match the average velocity of nearby neighbors.

#### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `NeighborRadius` | `float` | 5.0 | Detection radius |

#### Usage

```csharp
// Standard alignment
var alignment = new AlignmentBehavior(5f);
agent.AddBehavior(alignment, 1.0f);
```

#### How It Works

```
    ● → →     Neighbors moving right
    ● → →
    
    ● ↗       Agent steers to match
```

1. Calculate average velocity of all neighbors
2. Steer toward that average velocity

---

### CohesionBehavior

Steers toward the center of mass of nearby neighbors.

#### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `NeighborRadius` | `float` | 5.0 | Detection radius |

#### Usage

```csharp
// Standard cohesion
var cohesion = new CohesionBehavior(5f);
agent.AddBehavior(cohesion, 1.0f);
```

#### How It Works

```
    ●     ●
      \   /
       \ /
        ★ Center of mass
       ↗
      /
    ●  Agent seeks center
```

---

### Combining Flocking Behaviors

```csharp
void SetupFlockingAgent(SwarmAgent agent)
{
    // Separation - highest weight to avoid collisions
    agent.AddBehavior(new SeparationBehavior(3f), 1.5f);
    
    // Alignment - medium weight for smooth movement
    agent.AddBehavior(new AlignmentBehavior(5f), 1.0f);
    
    // Cohesion - medium weight to stay together
    agent.AddBehavior(new CohesionBehavior(5f), 1.0f);
    
    // Wander - low weight for variety
    agent.AddBehavior(new WanderBehavior(), 0.3f);
}
```

---

## Navigation Behaviors

### ObstacleAvoidanceBehavior

Steers around obstacles using raycasts.

#### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `RayDistance` | `float` | from Settings | Raycast distance |
| `RayCount` | `int` | from Settings | Number of rays |
| `ObstacleLayer` | `LayerMask` | Default | Layers to avoid |

#### Usage

```csharp
// Basic obstacle avoidance
var avoid = new ObstacleAvoidanceBehavior();
agent.AddBehavior(avoid, 2.0f);  // High weight for safety

// Custom configuration
var customAvoid = new ObstacleAvoidanceBehavior();
customAvoid.RayDistance = 8f;
customAvoid.RayCount = 5;
customAvoid.ObstacleLayer = LayerMask.GetMask("Obstacles", "Walls");
agent.AddBehavior(customAvoid, 2.5f);
```

#### How It Works

```
                  ╱ Ray 1
                 ╱
    Agent ●─────────→ Forward ray
                 \
                  \ Ray 2
```

1. Cast multiple rays ahead of the agent
2. If a ray hits an obstacle, calculate avoidance force
3. Force steers perpendicular to the hit normal

---

### FollowLeaderBehavior

Steers to follow a leader agent with optional offset.

#### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Leader` | `SwarmAgent` | null | Agent to follow |
| `FollowDistance` | `float` | from Settings | Distance behind leader |
| `Offset` | `Vector3` | zero | Offset from follow position |
| `SlowingRadius` | `float` | 3.0 | Arrive slowing radius |

#### Usage

```csharp
// Basic follow
var follow = new FollowLeaderBehavior();
follow.Leader = leaderAgent;
follow.FollowDistance = 3f;
agent.AddBehavior(follow, 1.0f);

// Follow with offset (for formations)
var offsetFollow = new FollowLeaderBehavior();
offsetFollow.Leader = leaderAgent;
offsetFollow.Offset = new Vector3(2f, 0, 0);  // Stay to the right
agent.AddBehavior(offsetFollow, 1.0f);
```

#### How It Works

```
    Leader ●────────→
           ↖
            \
             \ Follow distance
              \
               ●  Follower
```

---

## Formation Behaviors

### FormationSlotBehavior

Steers toward the agent's target position (set by SwarmFormation) with arrival damping for stable formations.

#### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `SlowingRadius` | `float` | 3.0 | Distance at which agent starts slowing |
| `ArrivalRadius` | `float` | 0.8 | Distance at which agent stops (dead zone) |
| `DampingFactor` | `float` | 0.5 | Velocity damping when close to target (0-1) |

#### Usage

```csharp
// Standard formation slot behavior
var slotBehavior = new FormationSlotBehavior();
agent.AddBehavior(slotBehavior, 1.0f);

// Custom settings for tighter formations
var tightSlot = new FormationSlotBehavior(
    slowingRadius: 2.5f,
    arrivalRadius: 0.7f,
    dampingFactor: 0.7f
);
agent.AddBehavior(tightSlot, 1.0f);
```

#### How It Works

1. Reads target position from `agent.TargetPosition` (set by SwarmFormation)
2. Uses quadratic speed falloff for smooth deceleration
3. Applies full braking when within arrival radius
4. Includes velocity damping to prevent oscillation

```
                SlowingRadius
        ←───────────────────────→
        
  Agent ●═══════════════════════════●  Slot
           Speed decreases    │ Stop zone
           quadratically      │ (ArrivalRadius)
```

#### Why Use This Instead of FollowLeaderBehavior?

`FormationSlotBehavior` is specifically designed to work with `SwarmFormation`:

| Aspect | FollowLeaderBehavior | FormationSlotBehavior |
|--------|---------------------|----------------------|
| Target source | Calculates own position behind leader | Uses `agent.TargetPosition` from formation |
| Formation support | No (conflicts with formation slots) | Yes (designed for formations) |
| Stability | Can oscillate | Has damping for stability |
| Use case | Simple leader-following | Precise formation positions |

**Note:** When using `SwarmFormation`, always use `FormationSlotBehavior` for followers, not `FollowLeaderBehavior`. The formation system calls `agent.SetTarget()` to set each agent's slot position, and `FormationSlotBehavior` is designed to read from this target.

---

## Creating Custom Behaviors

### Option 1: Extend BehaviorBase

```csharp
using UnityEngine;
using SwarmAI;

public class CircleTargetBehavior : BehaviorBase
{
    public override string Name => "CircleTarget";
    
    public Transform Target;
    public float OrbitRadius = 5f;
    public float OrbitSpeed = 2f;
    
    private float _angle;
    
    public override Vector3 CalculateForce(SwarmAgent agent)
    {
        if (agent == null || Target == null) return Vector3.zero;
        
        // Update orbit angle
        _angle += OrbitSpeed * Time.deltaTime;
        
        // Calculate orbit position
        Vector3 orbitPos = Target.position + new Vector3(
            Mathf.Cos(_angle) * OrbitRadius,
            0,
            Mathf.Sin(_angle) * OrbitRadius
        );
        
        // Use the built-in Seek helper
        return Seek(agent, orbitPos);
    }
}
```

### Option 2: Implement IBehavior

```csharp
using UnityEngine;
using SwarmAI;

public class AttractToPointsBehavior : IBehavior
{
    public string Name => "AttractToPoints";
    public float Weight { get; set; } = 1f;
    public bool IsActive { get; set; } = true;
    
    public Vector3[] AttractorPoints;
    public float AttractRadius = 10f;
    
    public Vector3 CalculateForce(SwarmAgent agent)
    {
        if (agent == null || AttractorPoints == null) 
            return Vector3.zero;
        
        Vector3 totalForce = Vector3.zero;
        
        foreach (var point in AttractorPoints)
        {
            Vector3 toPoint = point - agent.Position;
            float distance = toPoint.magnitude;
            
            if (distance < AttractRadius && distance > 0.1f)
            {
                // Stronger attraction when closer
                float strength = 1f - (distance / AttractRadius);
                totalForce += toPoint.normalized * strength;
            }
        }
        
        return totalForce.normalized * agent.MaxForce;
    }
}
```

### Best Practices

1. **Always null-check the agent**
2. **Return Vector3.zero for edge cases**
3. **Use sqrMagnitude when possible** for performance
4. **Clamp output to agent.MaxForce**
5. **Use the Seek/Flee/Truncate helpers** from BehaviorBase

---

## Behavior Reference Table

| Behavior | Purpose | Typical Weight | Key Properties |
|----------|---------|----------------|----------------|
| SeekBehavior | Move toward target | 1.0 | TargetPosition, TargetTransform |
| FleeBehavior | Move away from threat | 1.5-2.0 | ThreatPosition, PanicDistance |
| ArriveBehavior | Seek with deceleration | 1.0 | SlowingRadius, ArrivalRadius |
| WanderBehavior | Random movement | 0.3-0.5 | WanderRadius, WanderJitter |
| SeparationBehavior | Avoid crowding | 1.5 | NeighborRadius, Falloff |
| AlignmentBehavior | Match neighbor velocity | 1.0 | NeighborRadius |
| CohesionBehavior | Stay with group | 1.0 | NeighborRadius |
| ObstacleAvoidanceBehavior | Avoid obstacles | 2.0+ | RayDistance, ObstacleLayer |
| FollowLeaderBehavior | Follow a leader | 1.0 | Leader, FollowDistance, Offset |
| FormationSlotBehavior | Hold formation position | 1.0 | SlowingRadius, ArrivalRadius, DampingFactor |

---

*For complete API details, see [API-REFERENCE.md](API-REFERENCE.md)*
