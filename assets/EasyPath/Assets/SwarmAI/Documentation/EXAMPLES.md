# SwarmAI Code Examples

Practical code samples for common use cases.

---

## Table of Contents

- [Basic Swarm Setup](#basic-swarm-setup)
- [Flocking Behavior](#flocking-behavior)
- [Formations](#formations)
- [Resource Gathering](#resource-gathering)
- [Combat AI](#combat-ai)
- [Custom Behaviors](#custom-behaviors)
- [Event Handling](#event-handling)

---

## Basic Swarm Setup

### Minimal Setup

```csharp
using UnityEngine;
using SwarmAI;

public class MinimalSwarm : MonoBehaviour
{
    [SerializeField] private GameObject agentPrefab;
    [SerializeField] private int agentCount = 10;
    
    void Start()
    {
        for (int i = 0; i < agentCount; i++)
        {
            Vector3 pos = Random.insideUnitSphere * 5f;
            pos.y = 0;
            Instantiate(agentPrefab, pos, Quaternion.identity);
        }
    }
    
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                SwarmManager.Instance.MoveAllTo(hit.point);
            }
        }
    }
}
```

### Agent with Click-to-Move

```csharp
using UnityEngine;
using SwarmAI;

public class ClickToMoveAgent : MonoBehaviour
{
    private SwarmAgent _agent;
    
    void Start()
    {
        _agent = GetComponent<SwarmAgent>();
    }
    
    void Update()
    {
        if (Input.GetMouseButtonDown(1)) // Right click
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                _agent.SetTarget(hit.point);
                _agent.SetState(new SeekingState(hit.point));
            }
        }
    }
}
```

---

## Flocking Behavior

### Classic Boids

```csharp
using UnityEngine;
using SwarmAI;

public class BoidsController : MonoBehaviour
{
    [SerializeField] private GameObject agentPrefab;
    [SerializeField] private int flockSize = 30;
    
    [Header("Behavior Weights")]
    [SerializeField] private float separationWeight = 1.5f;
    [SerializeField] private float alignmentWeight = 1.0f;
    [SerializeField] private float cohesionWeight = 1.0f;
    [SerializeField] private float wanderWeight = 0.3f;
    
    void Start()
    {
        SpawnFlock();
    }
    
    void SpawnFlock()
    {
        for (int i = 0; i < flockSize; i++)
        {
            Vector3 pos = Random.insideUnitSphere * 10f;
            pos.y = 0;
            
            GameObject obj = Instantiate(agentPrefab, pos, Quaternion.identity);
            SwarmAgent agent = obj.GetComponent<SwarmAgent>();
            
            // Add flocking behaviors
            agent.AddBehavior(new SeparationBehavior(3f), separationWeight);
            agent.AddBehavior(new AlignmentBehavior(5f), alignmentWeight);
            agent.AddBehavior(new CohesionBehavior(5f), cohesionWeight);
            agent.AddBehavior(new WanderBehavior(), wanderWeight);
            agent.AddBehavior(new ObstacleAvoidanceBehavior(), 2.0f);
        }
    }
}
```

### Flock with Leader

```csharp
using UnityEngine;
using SwarmAI;

public class LeaderFlock : MonoBehaviour
{
    [SerializeField] private GameObject leaderPrefab;
    [SerializeField] private GameObject followerPrefab;
    [SerializeField] private int followerCount = 10;
    
    private SwarmAgent _leader;
    
    void Start()
    {
        // Spawn leader
        GameObject leaderObj = Instantiate(leaderPrefab, Vector3.zero, Quaternion.identity);
        _leader = leaderObj.GetComponent<SwarmAgent>();
        _leader.AddBehavior(new WanderBehavior(), 1.0f);
        
        // Spawn followers
        for (int i = 0; i < followerCount; i++)
        {
            Vector3 pos = Random.insideUnitSphere * 5f;
            pos.y = 0;
            
            GameObject obj = Instantiate(followerPrefab, pos, Quaternion.identity);
            SwarmAgent agent = obj.GetComponent<SwarmAgent>();
            
            // Follow the leader
            var followBehavior = new FollowLeaderBehavior();
            followBehavior.Leader = _leader;
            followBehavior.FollowDistance = 3f;
            agent.AddBehavior(followBehavior, 1.0f);
            
            // Also add flocking for smooth movement
            agent.AddBehavior(new SeparationBehavior(2f), 1.5f);
            agent.AddBehavior(new AlignmentBehavior(4f), 0.5f);
        }
    }
}
```

---

## Formations

### Basic Formation Setup

```csharp
using UnityEngine;
using SwarmAI;

public class FormationController : MonoBehaviour
{
    private SwarmAgent _leader;
    private SwarmFormation _formation;
    private SwarmGroup _group;
    
    void Start()
    {
        var agents = FindObjectsByType<SwarmAgent>(FindObjectsSortMode.None);
        if (agents.Length == 0) return;
        
        // First agent is leader
        _leader = agents[0];
        
        // Create formation
        _formation = _leader.gameObject.AddComponent<SwarmFormation>();
        _formation.Type = FormationType.Wedge;
        _formation.Spacing = 2f;
        _formation.Leader = _leader;
        
        // Create group
        _group = new SwarmGroup(_leader, "Squad");
        _group.SetFormation(_formation);
        
        // Add followers
        for (int i = 1; i < agents.Length; i++)
        {
            _group.AddMember(agents[i]);
            agents[i].SetState(new FollowingState(_leader));
        }
    }
    
    void Update()
    {
        // Change formation with number keys
        if (Input.GetKeyDown(KeyCode.Alpha1)) _formation.Type = FormationType.Line;
        if (Input.GetKeyDown(KeyCode.Alpha2)) _formation.Type = FormationType.Column;
        if (Input.GetKeyDown(KeyCode.Alpha3)) _formation.Type = FormationType.Circle;
        if (Input.GetKeyDown(KeyCode.Alpha4)) _formation.Type = FormationType.Wedge;
        if (Input.GetKeyDown(KeyCode.Alpha5)) _formation.Type = FormationType.V;
        if (Input.GetKeyDown(KeyCode.Alpha6)) _formation.Type = FormationType.Box;
        
        // Click to move formation
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                _formation.MoveTo(hit.point);
            }
        }
    }
}
```

---

## Resource Gathering

### RTS-Style Workers

```csharp
using UnityEngine;
using SwarmAI;
using System.Collections.Generic;

public class ResourceManager : MonoBehaviour
{
    [SerializeField] private GameObject workerPrefab;
    [SerializeField] private int workerCount = 5;
    [SerializeField] private Transform homeBase;
    
    private List<SwarmAgent> _workers = new List<SwarmAgent>();
    
    void Start()
    {
        // Create resource nodes
        CreateResourceNode(new Vector3(15, 0, 10), "Gold");
        CreateResourceNode(new Vector3(-15, 0, 10), "Gold");
        CreateResourceNode(new Vector3(0, 0, 20), "Wood");
        
        // Spawn workers
        SpawnWorkers();
    }
    
    void CreateResourceNode(Vector3 position, string type)
    {
        GameObject nodeObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        nodeObj.name = $"{type}Node";
        nodeObj.transform.position = position;
        
        ResourceNode node = nodeObj.AddComponent<ResourceNode>();
        node.Configure(
            totalAmount: 200f,
            harvestRate: 15f,
            respawns: true,
            respawnTime: 60f
        );
    }
    
    void SpawnWorkers()
    {
        for (int i = 0; i < workerCount; i++)
        {
            Vector3 pos = homeBase.position + Random.insideUnitSphere * 2f;
            pos.y = 0;
            
            GameObject obj = Instantiate(workerPrefab, pos, Quaternion.identity);
            SwarmAgent worker = obj.GetComponent<SwarmAgent>();
            _workers.Add(worker);
            
            // Start gathering
            AssignToNearestResource(worker);
        }
    }
    
    void AssignToNearestResource(SwarmAgent worker)
    {
        ResourceNode resource = ResourceNode.FindNearestAvailable(worker.Position);
        if (resource != null)
        {
            worker.SetState(new GatheringState(resource, homeBase.position, 20f));
        }
    }
    
    void Update()
    {
        // G key sends all idle workers to gather
        if (Input.GetKeyDown(KeyCode.G))
        {
            foreach (var worker in _workers)
            {
                if (worker.CurrentStateType == AgentStateType.Idle)
                {
                    AssignToNearestResource(worker);
                }
            }
        }
    }
}
```

---

## Combat AI

### Simple Combat State

```csharp
using UnityEngine;
using SwarmAI;

public class CombatState : AgentState
{
    private Transform _target;
    private float _attackRange = 2f;
    private float _attackCooldown = 1f;
    private float _lastAttackTime;
    
    public CombatState(Transform target)
    {
        Type = AgentStateType.Attacking;
        _target = target;
    }
    
    public override void Enter()
    {
        base.Enter();
        Agent.SetTarget(_target.position);
    }
    
    public override void Execute()
    {
        if (_target == null) return;
        
        // Update target position
        Agent.SetTarget(_target.position);
        
        // Check attack range
        float distance = Vector3.Distance(Agent.Position, _target.position);
        if (distance <= _attackRange && Time.time - _lastAttackTime >= _attackCooldown)
        {
            Attack();
        }
    }
    
    private void Attack()
    {
        _lastAttackTime = Time.time;
        Debug.Log($"Agent {Agent.AgentId} attacks!");
        // Deal damage, play animation, etc.
    }
    
    public override AgentState CheckTransitions()
    {
        // Target destroyed
        if (_target == null)
            return new IdleState();
        
        // Target too far - chase
        float distance = Vector3.Distance(Agent.Position, _target.position);
        if (distance > 15f)
            return new SeekingState(_target);
        
        return this;
    }
}
```

### Squad Combat Controller

```csharp
using UnityEngine;
using SwarmAI;
using System.Collections.Generic;

public class SquadCombat : MonoBehaviour
{
    private SwarmGroup _squad;
    private List<SwarmAgent> _enemies = new List<SwarmAgent>();
    
    void Update()
    {
        // Attack nearest enemy
        if (Input.GetKeyDown(KeyCode.A))
        {
            AttackNearestEnemy();
        }
        
        // Retreat
        if (Input.GetKeyDown(KeyCode.R))
        {
            Retreat();
        }
    }
    
    void AttackNearestEnemy()
    {
        if (_enemies.Count == 0) return;
        
        // Find nearest enemy to squad leader
        SwarmAgent nearestEnemy = null;
        float nearestDist = float.MaxValue;
        
        foreach (var enemy in _enemies)
        {
            float dist = Vector3.Distance(_squad.Leader.Position, enemy.Position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearestEnemy = enemy;
            }
        }
        
        if (nearestEnemy != null)
        {
            // Command squad to attack
            _squad.Seek(nearestEnemy.Position);
        }
    }
    
    void Retreat()
    {
        Vector3 retreatPos = transform.position; // Base position
        _squad.Flee(_enemies[0].Position);
    }
}
```

---

## Custom Behaviors

### Patrol Behavior

```csharp
using UnityEngine;
using SwarmAI;

public class PatrolBehavior : BehaviorBase
{
    public override string Name => "Patrol";
    
    public Vector3[] Waypoints;
    public float WaypointRadius = 1f;
    
    private int _currentIndex;
    
    public override Vector3 CalculateForce(SwarmAgent agent)
    {
        if (agent == null || Waypoints == null || Waypoints.Length == 0)
            return Vector3.zero;
        
        Vector3 target = Waypoints[_currentIndex];
        float distance = Vector3.Distance(agent.Position, target);
        
        // Move to next waypoint if close enough
        if (distance < WaypointRadius)
        {
            _currentIndex = (_currentIndex + 1) % Waypoints.Length;
            target = Waypoints[_currentIndex];
        }
        
        return Seek(agent, target);
    }
}

// Usage:
var patrol = new PatrolBehavior();
patrol.Waypoints = new Vector3[] {
    new Vector3(10, 0, 0),
    new Vector3(10, 0, 10),
    new Vector3(-10, 0, 10),
    new Vector3(-10, 0, 0)
};
agent.AddBehavior(patrol, 1.0f);
```

### Avoid Area Behavior

```csharp
using UnityEngine;
using SwarmAI;

public class AvoidAreaBehavior : BehaviorBase
{
    public override string Name => "AvoidArea";
    
    public Vector3 AreaCenter;
    public float AreaRadius = 10f;
    public float AvoidStrength = 2f;
    
    public override Vector3 CalculateForce(SwarmAgent agent)
    {
        if (agent == null) return Vector3.zero;
        
        Vector3 toAgent = agent.Position - AreaCenter;
        float distance = toAgent.magnitude;
        
        // Only avoid if inside the area
        if (distance >= AreaRadius) return Vector3.zero;
        
        // Stronger force when closer to center
        float strength = 1f - (distance / AreaRadius);
        return toAgent.normalized * strength * AvoidStrength * agent.MaxForce;
    }
}
```

---

## Event Handling

### State Change Listener

```csharp
using UnityEngine;
using SwarmAI;

public class AgentEventHandler : MonoBehaviour
{
    private SwarmAgent _agent;
    
    void Start()
    {
        _agent = GetComponent<SwarmAgent>();
        
        // State changes
        _agent.OnStateChanged += HandleStateChange;
        
        // Target reached
        _agent.OnTargetReached += HandleTargetReached;
        
        // Messages
        _agent.OnMessageReceived += HandleMessage;
    }
    
    void OnDestroy()
    {
        if (_agent != null)
        {
            _agent.OnStateChanged -= HandleStateChange;
            _agent.OnTargetReached -= HandleTargetReached;
            _agent.OnMessageReceived -= HandleMessage;
        }
    }
    
    void HandleStateChange(AgentState oldState, AgentState newState)
    {
        Debug.Log($"State: {oldState?.Type} → {newState.Type}");
        
        // Play animations based on state
        switch (newState.Type)
        {
            case AgentStateType.Idle:
                // Play idle animation
                break;
            case AgentStateType.Moving:
            case AgentStateType.Seeking:
                // Play walk animation
                break;
            case AgentStateType.Fleeing:
                // Play run animation
                break;
        }
    }
    
    void HandleTargetReached()
    {
        Debug.Log("Target reached!");
    }
    
    void HandleMessage(SwarmMessage message)
    {
        Debug.Log($"Received: {message.Type} from {message.SenderId}");
    }
}
```

### SwarmManager Events

```csharp
using UnityEngine;
using SwarmAI;

public class SwarmEventHandler : MonoBehaviour
{
    void Start()
    {
        SwarmManager.Instance.OnAgentRegistered += OnAgentJoined;
        SwarmManager.Instance.OnAgentUnregistered += OnAgentLeft;
    }
    
    void OnDestroy()
    {
        if (SwarmManager.HasInstance)
        {
            SwarmManager.Instance.OnAgentRegistered -= OnAgentJoined;
            SwarmManager.Instance.OnAgentUnregistered -= OnAgentLeft;
        }
    }
    
    void OnAgentJoined(SwarmAgent agent)
    {
        Debug.Log($"Agent {agent.AgentId} joined the swarm");
    }
    
    void OnAgentLeft(SwarmAgent agent)
    {
        Debug.Log($"Agent {agent.AgentId} left the swarm");
    }
}
```

---

*For more details, see the other documentation files.*
