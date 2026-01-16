# SwarmAI Design Document

**Version:** 1.0  
**Last Updated:** January 2026  
**Status:** Planning Phase

## Overview

SwarmAI is a Unity Asset Store package providing multi-agent coordination, swarm intelligence, and emergent behavior systems. Built from real competition experience (MIT Battlecode 2026), it offers production-ready patterns for RTS games, colony simulators, and AI-driven experiences.

## Target Market

- **Primary:** RTS and colony sim developers
- **Secondary:** Game jam participants, AI researchers, indie devs
- **Price Point:** $45
- **Development Time:** 3-4 weeks

## Core Architecture

```
SwarmAI/
├── Runtime/
│   ├── Core/
│   │   ├── SwarmManager.cs         # Central coordinator
│   │   ├── AgentState.cs           # State machine base
│   │   ├── SwarmMessage.cs         # Inter-agent communication
│   │   └── SwarmSettings.cs        # ScriptableObject config
│   ├── Components/
│   │   ├── SwarmAgent.cs           # Agent component
│   │   ├── SwarmSensor.cs          # Perception system
│   │   └── SwarmFormation.cs       # Formation controller
│   └── Behaviors/
│       ├── IBehavior.cs            # Behavior interface
│       ├── IdleBehavior.cs         # Default idle state
│       ├── SeekBehavior.cs         # Move toward target
│       ├── FleeBehavior.cs         # Move away from threat
│       ├── WanderBehavior.cs       # Random movement
│       ├── FlockBehavior.cs        # Boids-style flocking
│       ├── GatherBehavior.cs       # Resource collection
│       └── PatrolBehavior.cs       # Waypoint patrol
├── Editor/
│   ├── Inspectors/
│   │   ├── SwarmAgentEditor.cs     # Custom inspector
│   │   └── SwarmManagerEditor.cs   # Manager inspector
│   └── Windows/
│       └── SwarmDebugWindow.cs     # Debug visualization
└── Demo/
    ├── Scripts/
    │   ├── SwarmDemoController.cs  # Demo input handling
    │   └── ResourceNode.cs         # Demo resource object
    └── Scenes/
        ├── BasicSwarm.unity        # Simple flocking
        ├── ResourceGathering.unity # Colony sim demo
        └── CombatFormations.unity  # RTS formations
```

## Key Components

### 1. SwarmManager (Singleton Coordinator)

```csharp
public class SwarmManager : MonoBehaviour
{
    // Agent registry
    private Dictionary<int, SwarmAgent> _agents;
    
    // Spatial partitioning for neighbor queries
    private SpatialHash<SwarmAgent> _spatialHash;
    
    // Message queue for inter-agent communication
    private Queue<SwarmMessage> _messageQueue;
    
    // Global settings
    public SwarmSettings Settings { get; private set; }
    
    // Core methods
    public void RegisterAgent(SwarmAgent agent);
    public void UnregisterAgent(SwarmAgent agent);
    public List<SwarmAgent> GetNeighbors(Vector3 position, float radius);
    public void BroadcastMessage(SwarmMessage message);
    public void SendMessage(int targetId, SwarmMessage message);
}
```

### 2. SwarmAgent (Per-Agent Component)

```csharp
public class SwarmAgent : MonoBehaviour
{
    // Unique ID for coordination
    public int AgentId { get; private set; }
    
    // State machine
    public AgentState CurrentState { get; private set; }
    
    // Movement
    public float Speed { get; set; }
    public Vector3 Velocity { get; private set; }
    
    // Behaviors (priority-weighted)
    private List<IBehavior> _behaviors;
    
    // Core methods
    public void SetState(AgentState newState);
    public void AddBehavior(IBehavior behavior, float weight);
    public Vector3 CalculateSteeringForce();
}
```

### 3. AgentState (Finite State Machine)

```csharp
public enum AgentStateType
{
    Idle,
    Moving,
    Seeking,
    Fleeing,
    Gathering,
    Attacking,
    Returning,
    Dead
}

public abstract class AgentState
{
    public AgentStateType Type { get; protected set; }
    public SwarmAgent Agent { get; private set; }
    
    public virtual void Enter() { }
    public virtual void Execute() { }
    public virtual void Exit() { }
    public virtual AgentState CheckTransitions() { return this; }
}
```

### 4. Behavior System (Steering Behaviors)

```csharp
public interface IBehavior
{
    string Name { get; }
    float Weight { get; set; }
    bool IsActive { get; set; }
    
    Vector3 CalculateForce(SwarmAgent agent);
}
```

**Implemented Behaviors:**

| Behavior | Description | Use Case |
|----------|-------------|----------|
| Seek | Move toward target position | Navigation |
| Flee | Move away from threat | Survival |
| Arrive | Seek with deceleration | Smooth stopping |
| Wander | Random steering | Idle movement |
| Pursuit | Predict and intercept | Combat |
| Evade | Predict and avoid | Escape |
| Separation | Avoid crowding neighbors | Flocking |
| Alignment | Match neighbor velocity | Flocking |
| Cohesion | Move toward neighbor center | Flocking |
| ObstacleAvoidance | Steer around obstacles | Navigation |

### 5. Spatial Partitioning

```csharp
public class SpatialHash<T>
{
    private float _cellSize;
    private Dictionary<Vector2Int, List<T>> _cells;
    
    public void Insert(T item, Vector3 position);
    public void Remove(T item, Vector3 position);
    public void Update(T item, Vector3 oldPos, Vector3 newPos);
    public List<T> Query(Vector3 center, float radius);
}
```

## Feature Breakdown

### Phase 1: Core Framework (Week 1)
- [x] SwarmManager singleton with agent registry
- [x] SwarmAgent component with ID system
- [x] Basic state machine (Idle, Moving, Seeking, Fleeing)
- [x] Spatial hash for neighbor queries
- [ ] Unit tests for core systems

### Phase 2: Steering Behaviors (Week 2)
- [x] IBehavior interface
- [x] Seek, Flee, Arrive behaviors
- [x] Wander behavior
- [x] Flocking (Separation, Alignment, Cohesion)
- [x] Obstacle avoidance
- [x] Behavior blending/prioritization

### Phase 3: Advanced Features (Week 3) ✅
- [x] Inter-agent messaging system (enhanced SwarmMessage with formation/group/resource types)
- [x] Formation system (line, column, circle, wedge, V, box, custom)
- [x] Resource gathering behavior (GatheringState, ReturningState, ResourceNode)
- [x] Group coordination (SwarmGroup, FollowLeaderBehavior, FollowingState)
- [x] Leader-follower patterns

### Phase 4: Polish & Demo (Week 4) ✅
- [ ] Custom inspectors for all components
- [ ] Debug visualization window
- [x] Demo scenes (3 minimum)
  - [x] FlockingDemo - Separation, Alignment, Cohesion, Wander, Obstacle Avoidance
  - [x] FormationDemo - Line, Column, Circle, Wedge, V, Box formations
  - [x] ResourceGatheringDemo - GatheringState, ReturningState, ResourceNode
- [x] Documentation (Phase 5)
- [ ] Performance optimization
- [ ] Asset Store submission prep

### Phase 5: Documentation (Week 4) ✅
- [x] README.md - Overview, features, installation, quick start guide
- [x] GETTING-STARTED.md - Step-by-step tutorials for all features
- [x] API-REFERENCE.md - Complete class/method documentation
- [x] BEHAVIORS.md - Steering behavior guide with diagrams
- [x] STATES.md - Agent state documentation with transitions
- [x] EXAMPLES.md - Code samples for common use cases

## Integration with EasyPath

SwarmAI uses EasyPath for pathfinding:

```csharp
// SwarmAgent integrates with EasyPath
private EasyPathAgent _pathAgent;

public void MoveTo(Vector3 destination)
{
    if (_pathAgent != null)
    {
        _pathAgent.SetDestination(destination);
    }
    else
    {
        // Fallback: direct movement
        _targetPosition = destination;
    }
}
```

## Performance Considerations

### Spatial Partitioning
- Cell size = 2x agent radius for optimal performance
- Lazy cell cleanup to avoid GC spikes
- Thread-safe for Jobs system compatibility

### Behavior Calculation
- Cache neighbor queries per frame
- Use squared distances to avoid sqrt
- Limit active behaviors per agent
- Consider LOD for distant agents

### Target Performance
- 100+ agents at 60 FPS (standalone)
- 50+ agents at 60 FPS (mobile)
- O(1) neighbor queries via spatial hash

## Demo Scenes

### 1. Basic Swarm (Flocking)
- 50 boid-like agents
- Flocking behaviors enabled
- Click to set flock target
- Obstacle avoidance demo

### 2. Resource Gathering
- 20 worker agents
- 3 resource nodes
- 1 home base
- Demonstrates: gather, return, idle states

### 3. Combat Formations
- 2 teams of 10 agents
- Formation controls (F1-F4)
- Attack/retreat commands
- Demonstrates: formations, combat states

## API Examples

### Creating a Basic Swarm

```csharp
// Manager setup (automatic via singleton)
var manager = SwarmManager.Instance;

// Spawn agents
for (int i = 0; i < 50; i++)
{
    var agent = Instantiate(agentPrefab).GetComponent<SwarmAgent>();
    agent.AddBehavior(new SeparationBehavior(), 1.5f);
    agent.AddBehavior(new AlignmentBehavior(), 1.0f);
    agent.AddBehavior(new CohesionBehavior(), 1.0f);
    agent.AddBehavior(new WanderBehavior(), 0.5f);
}

// Send all agents to a target
manager.BroadcastMessage(new SeekMessage(targetPosition));
```

### Custom Behavior

```csharp
public class AvoidFireBehavior : IBehavior
{
    public string Name => "AvoidFire";
    public float Weight { get; set; } = 2.0f;
    public bool IsActive { get; set; } = true;
    
    public Vector3 CalculateForce(SwarmAgent agent)
    {
        var fires = Physics.OverlapSphere(agent.Position, 10f, fireLayer);
        if (fires.Length == 0) return Vector3.zero;
        
        Vector3 fleeForce = Vector3.zero;
        foreach (var fire in fires)
        {
            Vector3 toAgent = agent.Position - fire.transform.position;
            float distance = toAgent.magnitude;
            fleeForce += toAgent.normalized / (distance * distance);
        }
        
        return fleeForce.normalized * agent.MaxForce;
    }
}
```

### State Machine Usage

```csharp
public class GatheringState : AgentState
{
    private ResourceNode _targetResource;
    
    public GatheringState(ResourceNode resource)
    {
        Type = AgentStateType.Gathering;
        _targetResource = resource;
    }
    
    public override void Enter()
    {
        Agent.MoveTo(_targetResource.Position);
    }
    
    public override void Execute()
    {
        if (Vector3.Distance(Agent.Position, _targetResource.Position) < 1f)
        {
            _targetResource.Harvest(Agent);
        }
    }
    
    public override AgentState CheckTransitions()
    {
        if (Agent.CarryAmount >= Agent.CarryCapacity)
            return new ReturningState();
        if (_targetResource.IsEmpty)
            return new SeekingResourceState();
        return this;
    }
}
```

## Battlecode Patterns Applied

From MIT Battlecode 2026 experience:

| Pattern | Battlecode Use | SwarmAI Implementation |
|---------|----------------|------------------------|
| tryMove() | Efficient movement with fallback | MoveTo() with path retry |
| State enums | Unit behavior management | AgentState FSM |
| ID-based differentiation | Role assignment | AgentId + RoleType |
| Distance-based decisions | Threat assessment | SwarmSensor perception |
| Resource estimation | Economy management | ResourceManager tracking |
| Spawn location selection | Strategic placement | FormationSystem |

## Success Criteria

- [ ] 100+ agents running at 60 FPS
- [ ] All behaviors working independently and combined
- [ ] Clean public API with XML documentation
- [ ] 3+ demo scenes showcasing features
- [ ] Editor tools for debugging
- [ ] Comprehensive documentation
- [ ] Unit tests with 80%+ coverage
- [ ] Asset Store submission approved
