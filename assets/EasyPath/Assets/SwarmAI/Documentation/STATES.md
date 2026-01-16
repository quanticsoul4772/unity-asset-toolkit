# SwarmAI States Guide

Complete documentation for all agent states in SwarmAI.

---

## Table of Contents

- [Overview](#overview)
- [Built-in States](#built-in-states)
  - [IdleState](#idlestate)
  - [MovingState](#movingstate)
  - [SeekingState](#seekingstate)
  - [FleeingState](#fleeingstate)
  - [GatheringState](#gatheringstate)
  - [ReturningState](#returningstate)
  - [FollowingState](#followingstate)
- [Creating Custom States](#creating-custom-states)
- [State Transitions](#state-transitions)
- [Message Handling](#message-handling)

---

## Overview

### What are Agent States?

States are part of a Finite State Machine (FSM) that controls agent behavior. Each agent has exactly one active state at any time.

### State Lifecycle

```
┌─────────────────────────────────────────────────────┐
│                                                     │
│   Enter()  →  Execute() (every frame)  →  Exit()   │
│                    ↓                                │
│            CheckTransitions()                       │
│                    ↓                                │
│           New state if needed                       │
│                                                     │
└─────────────────────────────────────────────────────┘
```

### Setting States

```csharp
// Set a new state
agent.SetState(new SeekingState(targetPosition));

// Check current state
if (agent.CurrentStateType == AgentStateType.Gathering)
{
    // Agent is gathering resources
}

// Get specific state data
if (agent.CurrentState is GatheringState gatherState)
{
    float carrying = gatherState.CurrentCarry;
}
```

### State Events

```csharp
// Listen for state changes
agent.OnStateChanged += (oldState, newState) =>
{
    Debug.Log($"State: {oldState?.Type} → {newState.Type}");
};
```

---

## Built-in States

### IdleState

Default resting state. Agent does nothing.

#### Constructor

```csharp
IdleState()
```

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Type` | `AgentStateType` | `AgentStateType.Idle` |

#### Usage

```csharp
// Return to idle
agent.SetState(new IdleState());

// Or use Stop() which sets idle state
agent.Stop();
```

#### Behavior

- Agent stops moving
- No automatic transitions
- Responds to messages (MoveTo, Seek, etc.)

---

### MovingState

Moves directly to a target position.

#### Constructor

```csharp
MovingState(Vector3 targetPosition)
```

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Type` | `AgentStateType` | `AgentStateType.Moving` |
| `TargetPosition` | `Vector3` | Destination |

#### Usage

```csharp
// Move to a position
agent.SetState(new MovingState(destination));
```

#### Behavior

- Sets agent target to destination
- Transitions to `IdleState` when arrived
- Uses agent's built-in arrival behavior

#### Transitions

| Condition | New State |
|-----------|----------|
| Reached target | IdleState |

---

### SeekingState

Actively pursues a target position or transform.

#### Constructors

```csharp
SeekingState(Vector3 targetPosition)
SeekingState(Transform targetTransform)
```

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Type` | `AgentStateType` | `AgentStateType.Seeking` |
| `TargetPosition` | `Vector3` | Current target position |
| `TargetTransform` | `Transform` | Target transform (if tracking) |

#### Usage

```csharp
// Seek a fixed position
agent.SetState(new SeekingState(position));

// Seek a moving target (updates each frame)
agent.SetState(new SeekingState(enemy.transform));
```

#### Behavior

- Continuously updates target if using Transform
- Sets agent target each frame
- More aggressive than MovingState

#### Transitions

| Condition | New State |
|-----------|----------|
| Reached target (position) | IdleState |
| Target transform destroyed | IdleState |

---

### FleeingState

Escapes from a threat position.

#### Constructors

```csharp
FleeingState(Vector3 threatPosition)
FleeingState(Transform threatTransform)
```

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Type` | `AgentStateType` | `AgentStateType.Fleeing` |
| `ThreatPosition` | `Vector3` | Current threat position |
| `SafeDistance` | `float` | Distance to flee (default: 15) |

#### Usage

```csharp
// Flee from a position
agent.SetState(new FleeingState(dangerPosition));

// Flee from a moving threat
agent.SetState(new FleeingState(predator.transform));

// Custom safe distance
var fleeState = new FleeingState(threatPosition);
fleeState.SafeDistance = 20f;
agent.SetState(fleeState);
```

#### Behavior

- Calculates direction away from threat
- Continuously moves in opposite direction
- Uses velocity-based steering

#### Transitions

| Condition | New State |
|-----------|----------|
| Distance > SafeDistance | IdleState |
| Threat destroyed | IdleState |

---

### GatheringState

Gathers resources from a ResourceNode.

#### Constructor

```csharp
GatheringState(ResourceNode resource, Vector3 basePosition, float carryCapacity = 10f)
```

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Type` | `AgentStateType` | `AgentStateType.Gathering` |
| `TargetResource` | `ResourceNode` | Resource being gathered |
| `BasePosition` | `Vector3` | Where to return resources |
| `CarryCapacity` | `float` | Max carry amount |
| `CurrentCarry` | `float` | Current carry amount |

#### Usage

```csharp
// Start gathering from a resource
var resource = ResourceNode.FindNearestAvailable(agent.Position);
if (resource != null)
{
    agent.SetState(new GatheringState(resource, homeBase.position, 15f));
}
```

#### Behavior

1. Move to resource node
2. Harvest until full or resource depleted
3. Automatically transition to ReturningState
4. If resource depletes, find another or go idle

#### Transitions

| Condition | New State |
|-----------|----------|
| Carry full | ReturningState |
| Resource depleted (not full) | Seek new resource or IdleState |
| Stuck (no progress) | IdleState |

---

### ReturningState

Returns carried resources to base.

#### Constructor

```csharp
ReturningState(Vector3 basePosition, float carryAmount = 0f)
```

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Type` | `AgentStateType` | `AgentStateType.Returning` |
| `BasePosition` | `Vector3` | Destination |
| `CarryAmount` | `float` | Resources being carried |

#### Usage

```csharp
// Return to base with resources
agent.SetState(new ReturningState(homeBase.position, 10f));
```

#### Behavior

1. Move to base position
2. "Deposit" resources on arrival
3. Optionally return to gathering

#### Transitions

| Condition | New State |
|-----------|----------|
| Reached base | IdleState (or GatheringState if configured) |
| Stuck (no progress) | IdleState |

---

### FollowingState

Follows a leader agent or formation position.

#### Constructors

```csharp
FollowingState(SwarmAgent leader)
FollowingState(int leaderId)
```

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Type` | `AgentStateType` | `AgentStateType.Following` |
| `Leader` | `SwarmAgent` | Leader being followed |
| `FollowDistance` | `float` | Distance to maintain |

#### Usage

```csharp
// Follow a specific agent
agent.SetState(new FollowingState(leaderAgent));

// Follow by leader ID
agent.SetState(new FollowingState(leaderId));
```

#### Behavior

- Calculates position behind leader
- Uses arrive behavior for smooth following
- Updates target as leader moves

#### Transitions

| Condition | New State |
|-----------|----------|
| Leader destroyed | IdleState |
| Leader not found | IdleState |

---

## Creating Custom States

### Basic Custom State

```csharp
using UnityEngine;
using SwarmAI;

public class PatrolState : AgentState
{
    private Vector3[] _waypoints;
    private int _currentIndex;
    private float _waypointRadius = 1f;
    
    public PatrolState(Vector3[] waypoints)
    {
        Type = AgentStateType.Patrolling;
        _waypoints = waypoints;
        _currentIndex = 0;
    }
    
    public override void Enter()
    {
        base.Enter();
        GoToCurrentWaypoint();
    }
    
    public override void Execute()
    {
        // Check if reached current waypoint
        float dist = Vector3.Distance(Agent.Position, _waypoints[_currentIndex]);
        
        if (dist < _waypointRadius)
        {
            // Move to next waypoint (loop)
            _currentIndex = (_currentIndex + 1) % _waypoints.Length;
            GoToCurrentWaypoint();
        }
    }
    
    private void GoToCurrentWaypoint()
    {
        Agent.SetTarget(_waypoints[_currentIndex]);
    }
    
    public override AgentState CheckTransitions()
    {
        // Check for threats
        var threats = Physics.OverlapSphere(Agent.Position, 10f, LayerMask.GetMask("Enemy"));
        if (threats.Length > 0)
        {
            return new FleeingState(threats[0].transform.position);
        }
        
        return this; // Stay in patrol state
    }
}
```

### State with Timers

```csharp
using UnityEngine;
using SwarmAI;

public class WaitState : AgentState
{
    private float _waitDuration;
    private AgentState _nextState;
    
    public WaitState(float duration, AgentState nextState)
    {
        Type = AgentStateType.Idle;
        _waitDuration = duration;
        _nextState = nextState;
    }
    
    public override void Enter()
    {
        base.Enter();
        Agent.Stop(); // Stop moving
    }
    
    public override AgentState CheckTransitions()
    {
        // Duration property from base class
        if (Duration >= _waitDuration)
        {
            return _nextState;
        }
        return this;
    }
}

// Usage:
agent.SetState(new WaitState(3f, new SeekingState(target)));
```

### State with Message Handling

```csharp
using UnityEngine;
using SwarmAI;

public class GuardState : AgentState
{
    private Vector3 _guardPosition;
    private float _alertRadius = 5f;
    
    public GuardState(Vector3 position)
    {
        Type = AgentStateType.Idle;
        _guardPosition = position;
    }
    
    public override void Enter()
    {
        base.Enter();
        Agent.SetTarget(_guardPosition);
    }
    
    public override void Execute()
    {
        // Return to guard position if drifted
        if (Vector3.Distance(Agent.Position, _guardPosition) > _alertRadius)
        {
            Agent.SetTarget(_guardPosition);
        }
    }
    
    public override bool HandleMessage(SwarmMessage message)
    {
        switch (message.Type)
        {
            case SwarmMessageType.Alert:
                // Investigate alert
                Agent.SetState(new SeekingState(message.Position));
                return true;
                
            case SwarmMessageType.ThreatDetected:
                // Flee from threat
                Agent.SetState(new FleeingState(message.Position));
                return true;
        }
        
        return false; // Message not handled
    }
}
```

---

## State Transitions

### Automatic Transitions

Implement `CheckTransitions()` to automatically change states:

```csharp
public override AgentState CheckTransitions()
{
    // Priority-based checks
    
    // 1. Emergency - always flee from nearby danger
    if (DetectNearbyDanger())
    {
        return new FleeingState(dangerPosition);
    }
    
    // 2. Task completion
    if (TaskComplete)
    {
        return new IdleState();
    }
    
    // 3. Resource needs
    if (NeedsResources)
    {
        return new GatheringState(FindResource(), basePosition);
    }
    
    // Stay in current state
    return this;
}
```

### Manual Transitions

Change states directly when needed:

```csharp
// From external controller
agent.SetState(new SeekingState(target));

// From within a state
public override void Execute()
{
    if (SomeCondition)
    {
        Agent.SetState(new OtherState());
    }
}
```

### Transition Flow Example

```
┌──────────┐     Click      ┌───────────┐
│  Idle    │ ────────────→  │  Seeking  │
└──────────┘                └───────────┘
     ↑                            │
     │   Arrived                  │ Threat!
     │                            ↓
     │                      ┌───────────┐
     └───────────────────── │  Fleeing  │
           Safe distance    └───────────┘
```

---

## Message Handling

### How Messages Work

1. Message sent to agent via SwarmManager
2. Current state's `HandleMessage()` called first
3. If state returns `false`, agent handles common messages
4. `OnMessageReceived` event fired

### State Message Handler

```csharp
public override bool HandleMessage(SwarmMessage message)
{
    // Return true if you handled the message
    // Return false to let the default handler process it
    
    if (message.Type == SwarmMessageType.Custom)
    {
        // Handle custom message
        ProcessCustomMessage(message);
        return true; // We handled it
    }
    
    return false; // Let default handler try
}
```

### Default Message Handling

The agent handles these messages if state doesn't:

| Message Type | Action |
|--------------|--------|
| `MoveTo` | SetTarget + MovingState |
| `Seek` | SetTarget + SeekingState |
| `Stop` | Stop + IdleState |
| `Flee` | FleeingState |
| `Follow` | FollowingState |
| `FormationUpdate` | SetTarget |
| `GatherResource` | GatheringState |
| `ReturnToBase` | ReturningState |

---

## State Reference Table

| State | Type | Auto Transitions | Key Properties |
|-------|------|------------------|----------------|
| IdleState | Idle | None | - |
| MovingState | Moving | → Idle (arrived) | TargetPosition |
| SeekingState | Seeking | → Idle (arrived/lost) | TargetPosition, TargetTransform |
| FleeingState | Fleeing | → Idle (safe) | ThreatPosition, SafeDistance |
| GatheringState | Gathering | → Returning (full) | TargetResource, CurrentCarry |
| ReturningState | Returning | → Idle (deposited) | BasePosition, CarryAmount |
| FollowingState | Following | → Idle (leader lost) | Leader, FollowDistance |

---

*For complete API details, see [API-REFERENCE.md](API-REFERENCE.md)*
