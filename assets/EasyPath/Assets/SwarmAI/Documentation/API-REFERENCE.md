# SwarmAI API Reference

Complete API documentation for all SwarmAI classes, interfaces, and enums.

---

## Table of Contents

- [Core Classes](#core-classes)
  - [SwarmManager](#swarmmanager)
  - [SwarmAgent](#swarmagent)
  - [SwarmSettings](#swarmsettings)
  - [SwarmMessage](#swarmmessage)
  - [SpatialHash](#spatialhash)
- [Components](#components)
  - [SwarmFormation](#swarmformation)
  - [ResourceNode](#resourcenode)
- [Groups](#groups)
  - [SwarmGroup](#swarmgroup)
- [Behaviors](#behaviors)
  - [IBehavior](#ibehavior)
  - [BehaviorBase](#behaviorbase)
- [States](#states)
  - [AgentState](#agentstate)
  - [AgentStateType](#agentstatetype)
- [Enums](#enums)
  - [SwarmMessageType](#swarmmessagetype)
  - [FormationType](#formationtype)

---

## Core Classes

### SwarmManager

`namespace SwarmAI`

Central coordinator for all swarm agents. Provides agent registry, spatial partitioning, and messaging.

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Instance` | `SwarmManager` | Singleton instance (creates if needed) |
| `HasInstance` | `bool` | Check if instance exists without creating |
| `Settings` | `SwarmSettings` | Global settings for the swarm system |
| `AgentCount` | `int` | Number of registered agents |
| `Agents` | `IReadOnlyDictionary<int, SwarmAgent>` | All registered agents |

#### Events

| Event | Signature | Description |
|-------|-----------|-------------|
| `OnAgentRegistered` | `Action<SwarmAgent>` | Fired when an agent is registered |
| `OnAgentUnregistered` | `Action<SwarmAgent>` | Fired when an agent is unregistered |

#### Methods

##### Agent Registry

```csharp
// Register an agent (called automatically by SwarmAgent.OnEnable)
void RegisterAgent(SwarmAgent agent)

// Unregister an agent (called automatically by SwarmAgent.OnDisable)
void UnregisterAgent(SwarmAgent agent)

// Get an agent by ID
SwarmAgent GetAgent(int id)

// Get all agents as a list
List<SwarmAgent> GetAllAgents()

// Get all agents into a pre-allocated list (reduces GC)
void GetAllAgents(List<SwarmAgent> results)
```

##### Spatial Queries

```csharp
// Get all agents within a radius
List<SwarmAgent> GetNeighbors(Vector3 position, float radius)

// Get neighbors with accurate distance filtering (no allocations)
void GetNeighbors(Vector3 position, float radius, List<SwarmAgent> results)

// Get neighbors excluding a specific agent
List<SwarmAgent> GetNeighborsExcluding(Vector3 position, float radius, SwarmAgent exclude)

// Get neighbors excluding agent (no allocations)
void GetNeighborsExcluding(Vector3 position, float radius, SwarmAgent exclude, List<SwarmAgent> results)

// Get the nearest agent to a position
SwarmAgent GetNearestAgent(Vector3 position, float maxRadius = float.MaxValue)
```

##### Messaging

```csharp
// Send a message to a specific agent
void SendMessage(int targetId, SwarmMessage message)

// Broadcast a message to all agents
void BroadcastMessage(SwarmMessage message)

// Broadcast to all agents within a radius
void BroadcastToArea(Vector3 position, float radius, SwarmMessage message)
```

##### Commands

```csharp
// Command all agents to move to a position
void MoveAllTo(Vector3 position)

// Command all agents to stop
void StopAll()

// Command all agents to seek a position
void SeekAll(Vector3 position)
```

---

### SwarmAgent

`namespace SwarmAI`

Core component for swarm agents. Provides state machine, steering behaviors, and movement.

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `AgentId` | `int` | Unique ID assigned by SwarmManager (-1 if not registered) |
| `Position` | `Vector3` | Current world position |
| `Forward` | `Vector3` | Current forward direction |
| `Velocity` | `Vector3` | Current velocity vector |
| `Speed` | `float` | Current speed (velocity magnitude) |
| `MaxSpeed` | `float` | Maximum movement speed |
| `MaxForce` | `float` | Maximum steering force |
| `Mass` | `float` | Mass for physics calculations |
| `NeighborRadius` | `float` | Radius for neighbor detection |
| `StoppingDistance` | `float` | Distance for arrival stopping |
| `CurrentState` | `AgentState` | Current FSM state |
| `CurrentStateType` | `AgentStateType` | Type of current state |
| `HasTarget` | `bool` | Whether agent has a movement target |
| `TargetPosition` | `Vector3` | Current target position |
| `IsRegistered` | `bool` | Whether registered with SwarmManager |
| `PathAgent` | `EasyPathAgent` | Optional pathfinding component |

#### Events

| Event | Signature | Description |
|-------|-----------|-------------|
| `OnStateChanged` | `Action<AgentState, AgentState>` | Fired on state change (old, new) |
| `OnTargetReached` | `Action` | Fired when target is reached |
| `OnMessageReceived` | `Action<SwarmMessage>` | Fired when message received |

#### Methods

##### State Machine

```csharp
// Change to a new state
void SetState(AgentState newState)
```

##### Movement

```csharp
// Set a target position to move toward
void SetTarget(Vector3 position)

// Clear the current movement target
void ClearTarget()

// Stop all movement immediately
void Stop()

// Apply an external force
void ApplyForce(Vector3 force)
```

##### Behaviors

```csharp
// Add a steering behavior with weight
void AddBehavior(IBehavior behavior, float weight = 1f)

// Remove a specific behavior
void RemoveBehavior(IBehavior behavior)

// Remove all behaviors of a type
void RemoveBehaviorsOfType<T>() where T : IBehavior

// Clear all behaviors
void ClearBehaviors()
```

##### Neighbors

```csharp
// Get nearby agents (cached per frame)
List<SwarmAgent> GetNeighbors()

// Get the nearest neighbor
SwarmAgent GetNearestNeighbor()
```

##### Messaging

```csharp
// Send a message to another agent
void SendMessage(int targetId, SwarmMessage message)

// Broadcast a message to all agents
void BroadcastMessage(SwarmMessage message)
```

---

### SwarmSettings

`namespace SwarmAI`

ScriptableObject containing global settings for the swarm system.

#### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `SpatialHashCellSize` | `float` | 5.0 | Cell size for spatial partitioning |
| `SpatialHashUpdateInterval` | `float` | 0.0 | Update interval (0 = every frame) |
| `MaxAgentsPerFrame` | `int` | 50 | Max messages processed per frame |
| `NeighborRadius` | `float` | 5.0 | Default neighbor detection radius |
| `WanderRadius` | `float` | 2.0 | Wander circle radius |
| `WanderDistance` | `float` | 4.0 | Wander circle distance |
| `WanderJitter` | `float` | 80.0 | Wander randomness (degrees/sec) |
| `ObstacleAvoidanceDistance` | `float` | 5.0 | Raycast distance for obstacles |
| `ObstacleAvoidanceRayCount` | `int` | 3 | Number of avoidance rays |
| `FormationArrivalRadius` | `float` | 0.5 | Radius to consider "in position" |
| `EnableDebugVisualization` | `bool` | false | Enable debug gizmos |

#### Constants

| Constant | Type | Value | Description |
|----------|------|-------|-------------|
| `DefaultPositionEqualityThresholdSq` | `float` | 0.0001 | Squared threshold for position equality |
| `DefaultVelocityThresholdSq` | `float` | 0.001 | Squared threshold for velocity checks |
| `BackwardNormalThreshold` | `float` | -0.5 | Dot product threshold for backward |

#### Methods

```csharp
// Create default settings instance
static SwarmSettings CreateDefault()
```

---

### SwarmMessage

`namespace SwarmAI`

Message for inter-agent communication.

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Type` | `SwarmMessageType` | Type of message |
| `SenderId` | `int` | ID of sender (-1 for system) |
| `TargetId` | `int` | ID of target (-1 for broadcast) |
| `Position` | `Vector3` | Position data |
| `Value` | `float` | Numeric value |
| `Data` | `object` | Optional object data |
| `Timestamp` | `float` | Time when message was created |

#### Static Factory Methods

```csharp
// Movement messages
static SwarmMessage MoveTo(Vector3 position, int senderId = -1)
static SwarmMessage Stop(int senderId = -1)
static SwarmMessage Seek(Vector3 position, int senderId = -1)
static SwarmMessage Flee(Vector3 position, int senderId = -1)

// Group messages
static SwarmMessage Follow(int leaderId, int senderId = -1)
static SwarmMessage FormationUpdate(Vector3 position, int slotIndex, int senderId = -1)
static SwarmMessage GroupCommand(string command, int senderId = -1)

// Resource messages
static SwarmMessage ResourceFound(Vector3 position, int senderId = -1)
static SwarmMessage ResourceDepleted(Vector3 position, int senderId = -1)
static SwarmMessage GatherResource(ResourceNode resource, Vector3 basePosition, int senderId = -1)
static SwarmMessage ReturnToBase(Vector3 basePosition, int senderId = -1)

// Alert messages
static SwarmMessage ThreatDetected(Vector3 position, float threatLevel, int senderId = -1)
static SwarmMessage Alert(Vector3 position, int senderId = -1)

// Custom message
static SwarmMessage Custom(SwarmMessageType type, Vector3 position, float value, object data, int senderId = -1)
```

#### Methods

```csharp
// Create a copy with new sender/target IDs
SwarmMessage Clone(int newSenderId, int newTargetId)
```

---

### SpatialHash

`namespace SwarmAI`

Spatial partitioning data structure for efficient neighbor queries.

#### Constructor

```csharp
SpatialHash<T>(float cellSize) where T : class
```

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `CellSize` | `float` | Size of each cell |
| `Count` | `int` | Total items in the hash |

#### Methods

```csharp
// Insert an item at a position
void Insert(T item, Vector3 position)

// Remove an item
void Remove(T item)

// Update an item's position
void UpdatePosition(T item, Vector3 newPosition)

// Query items within a radius
List<T> Query(Vector3 center, float radius)

// Query with accurate distance filtering (no allocations)
void Query(Vector3 center, float radius, List<T> results, Func<T, Vector3> getPosition)

// Query excluding an item
List<T> QueryExcluding(Vector3 center, float radius, T exclude)

// Query excluding with accurate filtering (no allocations)
void QueryExcluding(Vector3 center, float radius, T exclude, List<T> results, Func<T, Vector3> getPosition)

// Clear all items
void Clear()

// Get the cell an item is in
bool TryGetCell(T item, out Vector2Int cell)
```

---

## Components

### SwarmFormation

`namespace SwarmAI`

Component for managing agent formations.

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Type` | `FormationType` | Current formation type |
| `Spacing` | `float` | Distance between agents |
| `Leader` | `SwarmAgent` | Formation leader |
| `AgentCount` | `int` | Number of agents in formation |
| `Slots` | `IReadOnlyList<FormationSlot>` | Current slot positions |

#### Methods

```csharp
// Move the entire formation to a position
void MoveTo(Vector3 position)

// Assign an agent to a specific slot
void AssignAgentToSlot(SwarmAgent agent, int slotIndex)

// Remove an agent from the formation
void RemoveAgent(SwarmAgent agent)

// Get the world position of a slot
Vector3 GetSlotWorldPosition(int slotIndex)

// Regenerate formation slots (call after changing Type or Spacing)
void RegenerateSlots()
```

---

### ResourceNode

`namespace SwarmAI`

Component for harvestable resource nodes.

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `ResourceType` | `string` | Type identifier |
| `CurrentAmount` | `float` | Remaining resource amount |
| `TotalAmount` | `float` | Maximum capacity |
| `HarvestRate` | `float` | Amount harvested per second |
| `HarvestRadius` | `float` | Radius for harvesting |
| `MaxHarvesters` | `int` | Max concurrent harvesters |
| `CurrentHarvesters` | `int` | Current harvester count |
| `IsDepleted` | `bool` | Whether empty |
| `HasCapacity` | `bool` | Has available slots and resources |
| `AmountPercent` | `float` | Remaining percentage (0-1) |
| `Position` | `Vector3` | World position |

#### Events

| Event | Signature | Description |
|-------|-----------|-------------|
| `OnHarvestStarted` | `Action<SwarmAgent>` | Agent started harvesting |
| `OnHarvestStopped` | `Action<SwarmAgent>` | Agent stopped harvesting |
| `OnResourceHarvested` | `Action<float>` | Resources harvested (amount) |
| `OnDepleted` | `Action` | Node is empty |
| `OnRespawned` | `Action` | Node respawned |

#### Methods

```csharp
// Configure the node programmatically
void Configure(float totalAmount, float harvestRate, bool respawns = false, float respawnTime = 30f)

// Start harvesting (returns true if successful)
bool TryStartHarvesting(SwarmAgent agent)

// Stop harvesting
void StopHarvesting(SwarmAgent agent)

// Check if agent is harvesting
bool IsHarvesting(SwarmAgent agent)

// Harvest resources (returns amount harvested)
float Harvest(SwarmAgent agent, float deltaTime)

// Check if agent is in range
bool IsInRange(SwarmAgent agent)

// Refill the node
void Refill(float amount)

// Respawn to full
void Respawn()
```

#### Static Methods

```csharp
// Find nearest non-depleted node
static ResourceNode FindNearest(Vector3 position, string resourceType = null, float maxDistance = float.MaxValue)

// Find nearest node with available capacity
static ResourceNode FindNearestAvailable(Vector3 position, string resourceType = null, float maxDistance = float.MaxValue)

// Get all nodes (read-only)
static IReadOnlyList<ResourceNode> AllNodes { get; }
```

---

## Groups

### SwarmGroup

`namespace SwarmAI`

Class for coordinating groups of agents.

#### Constructor

```csharp
SwarmGroup(SwarmAgent leader, string groupName = "Group")
```

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `GroupName` | `string` | Name of the group |
| `Leader` | `SwarmAgent` | Group leader |
| `Members` | `IReadOnlyList<SwarmAgent>` | Group members (excluding leader) |
| `MemberCount` | `int` | Total member count |
| `Formation` | `SwarmFormation` | Associated formation |

#### Methods

```csharp
// Add a member to the group
void AddMember(SwarmAgent agent)

// Remove a member from the group
void RemoveMember(SwarmAgent agent)

// Set the group's formation
void SetFormation(SwarmFormation formation)

// Move the entire group to a position
void MoveTo(Vector3 position)

// Stop all group members
void Stop()

// Command all members to seek a position
void Seek(Vector3 position)

// Command all members to flee from a position
void Flee(Vector3 position)

// Command all members to follow the leader
void FollowLeader()

// Broadcast a message to all members
void BroadcastToMembers(SwarmMessage message)

// Cleanup (call when destroying group)
void Cleanup()
```

---

## Behaviors

### IBehavior

`namespace SwarmAI`

Interface for steering behaviors.

```csharp
public interface IBehavior
{
    string Name { get; }
    float Weight { get; set; }
    bool IsActive { get; set; }
    Vector3 CalculateForce(SwarmAgent agent);
}
```

---

### BehaviorBase

`namespace SwarmAI`

Abstract base class for steering behaviors.

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Behavior name (abstract) |
| `Weight` | `float` | Weight multiplier (default: 1.0) |
| `IsActive` | `bool` | Whether active (default: true) |

#### Protected Methods

```csharp
// Calculate seek force toward a target
protected Vector3 Seek(SwarmAgent agent, Vector3 targetPosition)

// Calculate flee force away from a position
protected Vector3 Flee(SwarmAgent agent, Vector3 threatPosition)

// Truncate vector to maximum length
protected Vector3 Truncate(Vector3 vector, float maxLength)
```

---

## States

### AgentState

`namespace SwarmAI`

Abstract base class for FSM states.

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Type` | `AgentStateType` | State type |
| `Agent` | `SwarmAgent` | Owner agent |
| `EnterTime` | `float` | Time when entered |
| `Duration` | `float` | Time in this state |

#### Virtual Methods

```csharp
// Called when entering this state
public virtual void Enter()

// Called every frame
public virtual void Execute()

// Called every fixed update
public virtual void FixedExecute()

// Called when exiting this state
public virtual void Exit()

// Check for state transitions
public virtual AgentState CheckTransitions()

// Handle a message (return true if handled)
public virtual bool HandleMessage(SwarmMessage message)
```

---

### AgentStateType

`namespace SwarmAI`

```csharp
public enum AgentStateType
{
    Idle,       // Agent is idle
    Moving,     // Moving to destination
    Seeking,    // Seeking a target
    Fleeing,    // Fleeing from threat
    Gathering,  // Gathering resources
    Attacking,  // Attacking a target
    Returning,  // Returning to base
    Following,  // Following leader/formation
    Patrolling, // Patrolling waypoints
    Dead        // Dead/inactive
}
```

---

## Enums

### SwarmMessageType

`namespace SwarmAI`

```csharp
public enum SwarmMessageType
{
    None,
    MoveTo,
    Stop,
    Seek,
    Flee,
    Follow,
    FormationUpdate,
    GroupCommand,
    ResourceFound,
    ResourceDepleted,
    GatherResource,
    ReturnToBase,
    ThreatDetected,
    Alert,
    Custom
}
```

---

### FormationType

`namespace SwarmAI`

```csharp
public enum FormationType
{
    Line,    // Horizontal line
    Column,  // Vertical column
    Circle,  // Circle around leader
    Wedge,   // Arrow/wedge shape
    V,       // V-shape behind leader
    Box,     // Square/rectangular
    Custom   // User-defined slots
}
```

---

### FormationSlot

`namespace SwarmAI`

Struct representing a position in a formation.

```csharp
public struct FormationSlot
{
    public int Index;           // Slot index
    public Vector3 LocalOffset; // Offset from leader
    public SwarmAgent Agent;    // Assigned agent (can be null)
    public bool IsOccupied;     // Whether slot has an agent
}
```

---

*For behavior-specific documentation, see [BEHAVIORS.md](BEHAVIORS.md)*  
*For state-specific documentation, see [STATES.md](STATES.md)*
