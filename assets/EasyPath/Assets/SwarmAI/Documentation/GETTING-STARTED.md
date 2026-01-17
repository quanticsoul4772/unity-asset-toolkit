# Getting Started with SwarmAI

This guide walks you through setting up SwarmAI and creating your first swarm.

---

## Table of Contents

1. [Setup](#1-setup)
2. [Your First Swarm](#2-your-first-swarm)
3. [Adding Behaviors](#3-adding-behaviors)
4. [Using States](#4-using-states)
5. [Formations](#5-formations)
6. [Resource Gathering](#6-resource-gathering)
7. [Custom Behaviors](#7-custom-behaviors)
8. [Custom States](#8-custom-states)

---

## 1. Setup

### Creating a SwarmManager

The `SwarmManager` is the central coordinator for all agents. It's implemented as a singleton and will be created automatically when you access `SwarmManager.Instance`.

**Option A: Automatic Creation**
```csharp
// Access the singleton - creates automatically if needed
var manager = SwarmManager.Instance;
```

**Option B: Manual Setup**
1. Create an empty GameObject in your scene
2. Name it "SwarmManager"
3. Add the `SwarmManager` component
4. Optionally assign a `SwarmSettings` asset

### Creating SwarmSettings

1. Right-click in Project window
2. Select **Create → SwarmAI → Swarm Settings**
3. Configure the settings in the Inspector
4. Assign to your SwarmManager

---

## 2. Your First Swarm

### Step 1: Create an Agent Prefab

```csharp
using UnityEngine;
using SwarmAI;

public class AgentSetup : MonoBehaviour
{
    void CreateAgentPrefab()
    {
        // Create the agent GameObject
        GameObject agentObj = new GameObject("SwarmAgent");
        
        // Add the SwarmAgent component
        SwarmAgent agent = agentObj.AddComponent<SwarmAgent>();
        
        // Add a visual (optional)
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visual.transform.SetParent(agentObj.transform);
        visual.transform.localPosition = Vector3.up * 0.5f;
        visual.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        
        // Save as prefab (in editor)
        // PrefabUtility.SaveAsPrefabAsset(agentObj, "Assets/AgentPrefab.prefab");
    }
}
```

### Step 2: Spawn Agents

```csharp
using UnityEngine;
using SwarmAI;

public class SwarmSpawner : MonoBehaviour
{
    [SerializeField] private GameObject agentPrefab;
    [SerializeField] private int agentCount = 20;
    [SerializeField] private float spawnRadius = 5f;
    
    void Start()
    {
        SpawnAgents();
    }
    
    void SpawnAgents()
    {
        for (int i = 0; i < agentCount; i++)
        {
            // Random position within spawn radius
            Vector3 randomPos = Random.insideUnitSphere * spawnRadius;
            randomPos.y = 0; // Keep on ground
            
            // Instantiate agent
            GameObject obj = Instantiate(agentPrefab, randomPos, Quaternion.identity);
            obj.name = $"Agent_{i}";
            
            // Agent automatically registers with SwarmManager
        }
        
        Debug.Log($"Spawned {agentCount} agents");
    }
}
```

### Step 3: Control the Swarm

```csharp
using UnityEngine;
using SwarmAI;

public class SwarmController : MonoBehaviour
{
    void Update()
    {
        // Click to move all agents
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // Move all agents to click position
                SwarmManager.Instance.MoveAllTo(hit.point);
            }
        }
        
        // Press Space to stop all agents
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SwarmManager.Instance.StopAll();
        }
    }
}
```

---

## 3. Adding Behaviors

Behaviors are modular steering forces that influence agent movement.

### Basic Flocking

```csharp
void SetupFlockingAgent(SwarmAgent agent)
{
    // Classic boids flocking
    agent.AddBehavior(new SeparationBehavior(3f), 1.5f);  // Avoid crowding
    agent.AddBehavior(new AlignmentBehavior(5f), 1.0f);   // Match velocity
    agent.AddBehavior(new CohesionBehavior(5f), 1.0f);    // Stay together
    
    // Add some randomness
    agent.AddBehavior(new WanderBehavior(), 0.3f);
    
    // Avoid obstacles
    agent.AddBehavior(new ObstacleAvoidanceBehavior(), 2.0f);
}
```

### Weighted Behaviors

Behaviors have two weight factors:
1. **AddBehavior weight** - Set when adding the behavior
2. **Behavior.Weight property** - Can be modified at runtime

```csharp
// Add behavior with initial weight
var separation = new SeparationBehavior();
agent.AddBehavior(separation, 1.5f);

// Modify weight at runtime
separation.Weight = 2.0f;  // Increase separation force

// Toggle behavior on/off
separation.IsActive = false;  // Disable temporarily
```

### Behavior Targeting

```csharp
// Seek a specific position
var seekBehavior = new SeekBehavior();
seekBehavior.TargetPosition = targetObject.transform.position;
agent.AddBehavior(seekBehavior, 1.0f);

// Or use a Transform (updates automatically)
seekBehavior.TargetTransform = targetObject.transform;

// Flee from a threat
var fleeBehavior = new FleeBehavior();
fleeBehavior.ThreatPosition = enemy.transform.position;
fleeBehavior.PanicDistance = 10f;  // Only flee within this distance
agent.AddBehavior(fleeBehavior, 2.0f);
```

---

## 4. Using States

States control agent behavior through a finite state machine.

### Setting States

```csharp
void ControlAgent(SwarmAgent agent)
{
    // Move to a position
    agent.SetState(new MovingState(targetPosition));
    
    // Seek and pursue a target
    agent.SetState(new SeekingState(targetPosition));
    
    // Flee from a threat
    agent.SetState(new FleeingState(threatPosition));
    
    // Return to idle
    agent.SetState(new IdleState());
    
    // Follow a leader
    SwarmAgent leader = GetLeaderAgent();
    agent.SetState(new FollowingState(leader));
}
```

### Checking State

```csharp
void CheckAgentState(SwarmAgent agent)
{
    // Check state type
    if (agent.CurrentStateType == AgentStateType.Gathering)
    {
        Debug.Log("Agent is gathering resources");
    }
    
    // Get specific state for more info
    if (agent.CurrentState is GatheringState gatherState)
    {
        Debug.Log($"Carrying: {gatherState.CurrentCarry}");
    }
}
```

### State Events

```csharp
void SetupStateListeners(SwarmAgent agent)
{
    agent.OnStateChanged += (oldState, newState) =>
    {
        Debug.Log($"State changed: {oldState?.Type} -> {newState.Type}");
    };
    
    agent.OnTargetReached += () =>
    {
        Debug.Log("Agent reached its target!");
    };
}
```

---

## 5. Formations

### Creating a Formation

```csharp
using UnityEngine;
using SwarmAI;

public class FormationExample : MonoBehaviour
{
    private SwarmAgent _leader;
    private SwarmFormation _formation;
    private SwarmGroup _group;
    
    void Start()
    {
        SetupFormation();
    }
    
    void SetupFormation()
    {
        // Get agents (assume already spawned)
        var agents = FindObjectsByType<SwarmAgent>(FindObjectsSortMode.None);
        if (agents.Length == 0) return;
        
        // First agent is the leader
        _leader = agents[0];
        
        // Add formation component to leader
        _formation = _leader.gameObject.AddComponent<SwarmFormation>();
        _formation.Type = FormationType.Wedge;
        _formation.Spacing = 2f;
        _formation.Leader = _leader;
        
        // Create a group
        _group = new SwarmGroup(_leader, "Squad Alpha");
        _group.SetFormation(_formation);
        
        // Add followers with FormationSlotBehavior
        for (int i = 1; i < agents.Length; i++)
        {
            _group.AddMember(agents[i]);
            
            // Clear existing behaviors and add FormationSlotBehavior
            // This reads from agent.TargetPosition which is set by SwarmFormation
            agents[i].ClearBehaviors();
            agents[i].AddBehavior(new FormationSlotBehavior(
                slowingRadius: 2.5f,
                arrivalRadius: 0.7f,
                dampingFactor: 0.7f
            ), 1.0f);
        }
    }
    
    void Update()
    {
        // Change formations with number keys
        if (Input.GetKeyDown(KeyCode.Alpha1))
            _formation.Type = FormationType.Line;
        if (Input.GetKeyDown(KeyCode.Alpha2))
            _formation.Type = FormationType.Column;
        if (Input.GetKeyDown(KeyCode.Alpha3))
            _formation.Type = FormationType.Circle;
        if (Input.GetKeyDown(KeyCode.Alpha4))
            _formation.Type = FormationType.Wedge;
        if (Input.GetKeyDown(KeyCode.Alpha5))
            _formation.Type = FormationType.V;
        if (Input.GetKeyDown(KeyCode.Alpha6))
            _formation.Type = FormationType.Box;
    }
}
```

### Formation Types

| Type | Description | Best For |
|------|-------------|----------|
| Line | Horizontal row | Defensive lines |
| Column | Vertical column | Marching, corridors |
| Circle | Ring around leader | Defense, surrounding |
| Wedge | Arrow pointing forward | Aggressive push |
| V | V-shape behind leader | Escort, flight |
| Box | Square/rectangular | Balanced protection |
| Custom | User-defined slots | Special formations |

---

## 6. Resource Gathering

### Setting Up Resources

```csharp
using UnityEngine;
using SwarmAI;

public class ResourceSetup : MonoBehaviour
{
    [SerializeField] private Vector3 basePosition;
    
    void Start()
    {
        CreateResourceNode(new Vector3(10, 0, 10));
        CreateResourceNode(new Vector3(-10, 0, 10));
        CreateResourceNode(new Vector3(0, 0, 15));
    }
    
    void CreateResourceNode(Vector3 position)
    {
        // Create visual
        GameObject nodeObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        nodeObj.name = "ResourceNode";
        nodeObj.transform.position = position;
        
        // Add ResourceNode component
        ResourceNode node = nodeObj.AddComponent<ResourceNode>();
        
        // Configure the node
        node.Configure(
            totalAmount: 100f,
            harvestRate: 10f,
            respawns: true,
            respawnTime: 30f
        );
        
        // Subscribe to events
        node.OnDepleted += () => Debug.Log($"{nodeObj.name} depleted!");
        node.OnRespawned += () => Debug.Log($"{nodeObj.name} respawned!");
    }
}
```

### Assigning Gatherers

```csharp
void AssignGatherer(SwarmAgent agent, ResourceNode resource, Vector3 basePosition)
{
    float carryCapacity = 10f;
    
    // Set gathering state - agent will:
    // 1. Move to resource
    // 2. Harvest until full
    // 3. Return to base
    // 4. Repeat
    agent.SetState(new GatheringState(resource, basePosition, carryCapacity));
}

void SendAllToGather(Vector3 basePosition)
{
    var agents = SwarmManager.Instance.GetAllAgents();
    
    foreach (var agent in agents)
    {
        // Find nearest available resource
        var resource = ResourceNode.FindNearestAvailable(agent.Position);
        
        if (resource != null)
        {
            agent.SetState(new GatheringState(resource, basePosition, 10f));
        }
    }
}
```

---

## 7. Custom Behaviors

Create your own steering behaviors by implementing `IBehavior` or extending `BehaviorBase`.

### Simple Custom Behavior

```csharp
using UnityEngine;
using SwarmAI;

public class AvoidFireBehavior : BehaviorBase
{
    public override string Name => "AvoidFire";
    
    public float DetectionRadius = 10f;
    public LayerMask FireLayer;
    
    public override Vector3 CalculateForce(SwarmAgent agent)
    {
        if (agent == null) return Vector3.zero;
        
        // Find nearby fire hazards
        var fires = Physics.OverlapSphere(agent.Position, DetectionRadius, FireLayer);
        if (fires.Length == 0) return Vector3.zero;
        
        // Calculate flee force from all fires
        Vector3 totalForce = Vector3.zero;
        
        foreach (var fire in fires)
        {
            Vector3 toAgent = agent.Position - fire.transform.position;
            float distance = toAgent.magnitude;
            
            // Stronger force when closer (inverse square)
            if (distance > 0.1f)
            {
                totalForce += toAgent.normalized / (distance * distance);
            }
        }
        
        return Truncate(totalForce.normalized * agent.MaxForce, agent.MaxForce);
    }
}
```

### Behavior with Target

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
        
        // Calculate orbit position
        _angle += OrbitSpeed * Time.deltaTime;
        Vector3 orbitPos = Target.position + new Vector3(
            Mathf.Cos(_angle) * OrbitRadius,
            0,
            Mathf.Sin(_angle) * OrbitRadius
        );
        
        // Seek the orbit position
        return Seek(agent, orbitPos);
    }
}
```

---

## 8. Custom States

Create your own states by extending `AgentState`.

### Basic Custom State

```csharp
using UnityEngine;
using SwarmAI;

public class PatrolState : AgentState
{
    private Vector3[] _waypoints;
    private int _currentWaypoint;
    private float _waypointRadius = 1f;
    
    public PatrolState(Vector3[] waypoints)
    {
        Type = AgentStateType.Patrolling;
        _waypoints = waypoints;
        _currentWaypoint = 0;
    }
    
    public override void Enter()
    {
        base.Enter();
        MoveToCurrentWaypoint();
    }
    
    public override void Execute()
    {
        // Check if reached current waypoint
        float distance = Vector3.Distance(Agent.Position, _waypoints[_currentWaypoint]);
        
        if (distance < _waypointRadius)
        {
            // Move to next waypoint
            _currentWaypoint = (_currentWaypoint + 1) % _waypoints.Length;
            MoveToCurrentWaypoint();
        }
    }
    
    private void MoveToCurrentWaypoint()
    {
        Agent.SetTarget(_waypoints[_currentWaypoint]);
    }
    
    public override AgentState CheckTransitions()
    {
        // Example: transition to flee if enemy is near
        var enemies = Physics.OverlapSphere(Agent.Position, 5f, LayerMask.GetMask("Enemy"));
        if (enemies.Length > 0)
        {
            return new FleeingState(enemies[0].transform.position);
        }
        
        return this;
    }
}
```

### State with Message Handling

```csharp
using UnityEngine;
using SwarmAI;

public class GuardState : AgentState
{
    private Vector3 _guardPosition;
    private float _guardRadius = 3f;
    
    public GuardState(Vector3 position)
    {
        Type = AgentStateType.Idle; // Use appropriate type
        _guardPosition = position;
    }
    
    public override void Enter()
    {
        base.Enter();
        Agent.SetTarget(_guardPosition);
    }
    
    public override void Execute()
    {
        // Return to guard position if too far
        float distance = Vector3.Distance(Agent.Position, _guardPosition);
        if (distance > _guardRadius)
        {
            Agent.SetTarget(_guardPosition);
        }
    }
    
    public override bool HandleMessage(SwarmMessage message)
    {
        // Respond to alert messages
        if (message.Type == SwarmMessageType.Alert)
        {
            // Investigate the alert position
            Agent.SetState(new SeekingState(message.Position));
            return true;
        }
        
        return false;
    }
}
```

---

## Next Steps

- Read the [API Reference](API-REFERENCE.md) for complete documentation
- Check [Behaviors](BEHAVIORS.md) for all available behaviors
- See [States](STATES.md) for all agent states
- Browse [Examples](EXAMPLES.md) for more code samples
- Try the demo scenes: SwarmAI → Create Demo Scene

---

*Happy swarming!*
