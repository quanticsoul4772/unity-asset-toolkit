# AI & Pathfinding Patterns Reference

Comprehensive guide to game AI patterns for Unity development.

## Table of Contents
- [Finite State Machines](#finite-state-machines)
- [Behavior Trees](#behavior-trees)
- [A* Pathfinding](#a-pathfinding)
- [Unity NavMesh](#unity-navmesh)
- [Steering Behaviors](#steering-behaviors)
- [Multi-Agent Coordination](#multi-agent-coordination)
- [Pattern Selection Guide](#pattern-selection-guide)

---

## Finite State Machines

FSMs are the simplest and most widely used AI pattern. Perfect for straightforward behaviors.

### When to Use
- ✅ Simple AI with clear states (Idle → Chase → Attack)
- ✅ UI flow management
- ✅ Animation state control
- ❌ Complex behaviors with many states (use Behavior Trees instead)
- ❌ Behaviors requiring memory or planning

### Basic Implementation
```csharp
public enum EnemyState
{
    Idle,
    Patrol,
    Chase,
    Attack,
    Flee,
    Dead
}

public class EnemyFSM : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float _detectionRange = 10f;
    [SerializeField] private float _attackRange = 2f;
    [SerializeField] private float _fleeHealthThreshold = 20f;

    private EnemyState _currentState = EnemyState.Idle;
    private Transform _player;
    private float _health = 100f;

    private void Update()
    {
        // Evaluate transitions
        EnemyState newState = EvaluateTransitions();
        if (newState != _currentState)
        {
            ExitState(_currentState);
            _currentState = newState;
            EnterState(_currentState);
        }

        // Execute current state
        ExecuteState(_currentState);
    }

    private EnemyState EvaluateTransitions()
    {
        if (_health <= 0)
            return EnemyState.Dead;

        if (_health < _fleeHealthThreshold)
            return EnemyState.Flee;

        float distanceToPlayer = Vector3.Distance(transform.position, _player.position);

        switch (_currentState)
        {
            case EnemyState.Idle:
            case EnemyState.Patrol:
                if (distanceToPlayer < _detectionRange)
                    return EnemyState.Chase;
                break;

            case EnemyState.Chase:
                if (distanceToPlayer < _attackRange)
                    return EnemyState.Attack;
                if (distanceToPlayer > _detectionRange * 1.5f)
                    return EnemyState.Patrol;
                break;

            case EnemyState.Attack:
                if (distanceToPlayer > _attackRange * 1.2f)
                    return EnemyState.Chase;
                break;
        }

        return _currentState;
    }

    private void EnterState(EnemyState state)
    {
        switch (state)
        {
            case EnemyState.Idle:
                // Stop movement
                break;
            case EnemyState.Patrol:
                // Pick patrol point
                break;
            case EnemyState.Chase:
                // Alert animation
                break;
            case EnemyState.Attack:
                // Start attack animation
                break;
            case EnemyState.Flee:
                // Panic animation
                break;
            case EnemyState.Dead:
                // Death animation, disable collider
                break;
        }
    }

    private void ExecuteState(EnemyState state)
    {
        switch (state)
        {
            case EnemyState.Patrol:
                // Move to patrol point
                break;
            case EnemyState.Chase:
                // Move toward player
                break;
            case EnemyState.Attack:
                // Deal damage if in range
                break;
            case EnemyState.Flee:
                // Move away from player
                break;
        }
    }

    private void ExitState(EnemyState state)
    {
        // Cleanup for previous state
    }
}
```

### Interface-Based FSM (More Flexible)
```csharp
public interface IState
{
    void Enter();
    void Execute();
    void Exit();
}

public class StateMachine
{
    private IState _currentState;
    private Dictionary<System.Type, IState> _states = new Dictionary<System.Type, IState>();

    public void AddState(IState state)
    {
        _states[state.GetType()] = state;
    }

    public void ChangeState<T>() where T : IState
    {
        _currentState?.Exit();
        _currentState = _states[typeof(T)];
        _currentState.Enter();
    }

    public void Update()
    {
        _currentState?.Execute();
    }
}

// Example state
public class ChaseState : IState
{
    private readonly EnemyController _enemy;
    private readonly Transform _target;

    public ChaseState(EnemyController enemy, Transform target)
    {
        _enemy = enemy;
        _target = target;
    }

    public void Enter()
    {
        _enemy.PlayAnimation("Alert");
    }

    public void Execute()
    {
        _enemy.MoveToward(_target.position);
    }

    public void Exit()
    {
        _enemy.StopAnimation("Alert");
    }
}
```

---

## Behavior Trees

More powerful than FSMs for complex AI. Used in AAA games like Halo.

### Core Concepts
```
Behavior Tree Structure:

         [Root]
            │
       [Selector]     ← Try children until one succeeds
        /      \
  [Sequence]  [Sequence]   ← Run children in order until one fails
    /    \       /    \
 [Task] [Task] [Task] [Task]  ← Actual actions
```

### Node Types
| Node Type | Description | Returns |
|-----------|-------------|--------|
| **Selector** | Try children L→R until SUCCESS | First SUCCESS or FAILURE |
| **Sequence** | Run children L→R until FAILURE | First FAILURE or SUCCESS |
| **Decorator** | Modifies child behavior | Depends on decorator |
| **Task/Leaf** | Actual action or check | SUCCESS, FAILURE, or RUNNING |

### Implementation
```csharp
public enum NodeStatus
{
    Success,
    Failure,
    Running
}

public abstract class BTNode
{
    public abstract NodeStatus Evaluate();
}

// Composite: Sequence - all children must succeed
public class Sequence : BTNode
{
    private readonly List<BTNode> _children = new List<BTNode>();

    public Sequence(params BTNode[] children)
    {
        _children.AddRange(children);
    }

    public override NodeStatus Evaluate()
    {
        foreach (var child in _children)
        {
            NodeStatus status = child.Evaluate();
            if (status != NodeStatus.Success)
                return status; // Return FAILURE or RUNNING
        }
        return NodeStatus.Success;
    }
}

// Composite: Selector - first child to succeed wins
public class Selector : BTNode
{
    private readonly List<BTNode> _children = new List<BTNode>();

    public Selector(params BTNode[] children)
    {
        _children.AddRange(children);
    }

    public override NodeStatus Evaluate()
    {
        foreach (var child in _children)
        {
            NodeStatus status = child.Evaluate();
            if (status != NodeStatus.Failure)
                return status; // Return SUCCESS or RUNNING
        }
        return NodeStatus.Failure;
    }
}

// Decorator: Inverter
public class Inverter : BTNode
{
    private readonly BTNode _child;

    public Inverter(BTNode child)
    {
        _child = child;
    }

    public override NodeStatus Evaluate()
    {
        NodeStatus status = _child.Evaluate();
        return status switch
        {
            NodeStatus.Success => NodeStatus.Failure,
            NodeStatus.Failure => NodeStatus.Success,
            _ => status
        };
    }
}

// Example Task Nodes
public class CheckPlayerInRange : BTNode
{
    private readonly Transform _agent;
    private readonly Transform _player;
    private readonly float _range;

    public CheckPlayerInRange(Transform agent, Transform player, float range)
    {
        _agent = agent;
        _player = player;
        _range = range;
    }

    public override NodeStatus Evaluate()
    {
        float distance = Vector3.Distance(_agent.position, _player.position);
        return distance < _range ? NodeStatus.Success : NodeStatus.Failure;
    }
}

public class MoveToTarget : BTNode
{
    private readonly NavMeshAgent _agent;
    private readonly Transform _target;
    private readonly float _stoppingDistance;

    public MoveToTarget(NavMeshAgent agent, Transform target, float stoppingDistance)
    {
        _agent = agent;
        _target = target;
        _stoppingDistance = stoppingDistance;
    }

    public override NodeStatus Evaluate()
    {
        _agent.SetDestination(_target.position);

        if (_agent.pathPending)
            return NodeStatus.Running;

        if (_agent.remainingDistance <= _stoppingDistance)
            return NodeStatus.Success;

        return NodeStatus.Running;
    }
}

public class Attack : BTNode
{
    private readonly EnemyController _enemy;

    public Attack(EnemyController enemy)
    {
        _enemy = enemy;
    }

    public override NodeStatus Evaluate()
    {
        _enemy.PerformAttack();
        return NodeStatus.Success;
    }
}
```

### Building a Behavior Tree
```csharp
public class EnemyBT : MonoBehaviour
{
    private BTNode _root;
    private NavMeshAgent _agent;
    private Transform _player;

    private void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        _player = FindObjectOfType<Player>().transform;
        BuildTree();
    }

    private void BuildTree()
    {
        // Tree structure:
        // Selector
        //   ├── Sequence (Attack if close)
        //   │     ├── CheckPlayerInRange(2)
        //   │     └── Attack
        //   ├── Sequence (Chase if detected)
        //   │     ├── CheckPlayerInRange(10)
        //   │     └── MoveToTarget
        //   └── Patrol

        _root = new Selector(
            new Sequence(
                new CheckPlayerInRange(transform, _player, 2f),
                new Attack(GetComponent<EnemyController>())
            ),
            new Sequence(
                new CheckPlayerInRange(transform, _player, 10f),
                new MoveToTarget(_agent, _player, 1.5f)
            ),
            new PatrolTask(_agent, GetPatrolPoints())
        );
    }

    private void Update()
    {
        _root.Evaluate();
    }
}
```

---

## A* Pathfinding

The gold standard algorithm for pathfinding. Finds optimal paths using heuristics.

### How A* Works
```
f(n) = g(n) + h(n)

f(n) = Total estimated cost
g(n) = Actual cost from start to current node
h(n) = Heuristic estimate from current to goal
```

### Grid-Based Implementation
```csharp
public class AStarPathfinder
{
    private readonly int _width;
    private readonly int _height;
    private readonly bool[,] _walkable;

    // Movement costs
    private const int STRAIGHT_COST = 10;
    private const int DIAGONAL_COST = 14;

    public AStarPathfinder(int width, int height, bool[,] walkable)
    {
        _width = width;
        _height = height;
        _walkable = walkable;
    }

    public List<Vector2Int> FindPath(Vector2Int start, Vector2Int end)
    {
        // Open set: nodes to evaluate (priority queue)
        var openSet = new PriorityQueue<PathNode>();
        
        // Closed set: already evaluated nodes
        var closedSet = new HashSet<Vector2Int>();
        
        // All nodes
        var nodes = new Dictionary<Vector2Int, PathNode>();

        // Initialize start node
        var startNode = new PathNode(start, 0, GetHeuristic(start, end), null);
        nodes[start] = startNode;
        openSet.Enqueue(startNode);

        while (openSet.Count > 0)
        {
            PathNode current = openSet.Dequeue();

            // Found the goal!
            if (current.Position == end)
                return ReconstructPath(current);

            closedSet.Add(current.Position);

            // Check all neighbors
            foreach (var neighbor in GetNeighbors(current.Position))
            {
                if (closedSet.Contains(neighbor))
                    continue;

                if (!IsWalkable(neighbor))
                    continue;

                int moveCost = IsDiagonal(current.Position, neighbor) 
                    ? DIAGONAL_COST 
                    : STRAIGHT_COST;
                    
                int tentativeG = current.GCost + moveCost;

                if (!nodes.TryGetValue(neighbor, out PathNode neighborNode))
                {
                    neighborNode = new PathNode(
                        neighbor, 
                        tentativeG, 
                        GetHeuristic(neighbor, end),
                        current
                    );
                    nodes[neighbor] = neighborNode;
                    openSet.Enqueue(neighborNode);
                }
                else if (tentativeG < neighborNode.GCost)
                {
                    // Found a better path to this node
                    neighborNode.GCost = tentativeG;
                    neighborNode.Parent = current;
                    openSet.UpdatePriority(neighborNode);
                }
            }
        }

        // No path found
        return null;
    }

    private int GetHeuristic(Vector2Int a, Vector2Int b)
    {
        // Manhattan distance (good for 4-directional movement)
        // return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

        // Diagonal distance (good for 8-directional movement)
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        return STRAIGHT_COST * (dx + dy) + (DIAGONAL_COST - 2 * STRAIGHT_COST) * Mathf.Min(dx, dy);
    }

    private IEnumerable<Vector2Int> GetNeighbors(Vector2Int pos)
    {
        // 8-directional neighbors
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue;
                
                Vector2Int neighbor = new Vector2Int(pos.x + x, pos.y + y);
                
                if (neighbor.x >= 0 && neighbor.x < _width &&
                    neighbor.y >= 0 && neighbor.y < _height)
                {
                    yield return neighbor;
                }
            }
        }
    }

    private bool IsWalkable(Vector2Int pos)
    {
        return _walkable[pos.x, pos.y];
    }

    private bool IsDiagonal(Vector2Int a, Vector2Int b)
    {
        return a.x != b.x && a.y != b.y;
    }

    private List<Vector2Int> ReconstructPath(PathNode endNode)
    {
        var path = new List<Vector2Int>();
        PathNode current = endNode;

        while (current != null)
        {
            path.Add(current.Position);
            current = current.Parent;
        }

        path.Reverse();
        return path;
    }
}

public class PathNode : System.IComparable<PathNode>
{
    public Vector2Int Position;
    public int GCost; // Cost from start
    public int HCost; // Heuristic to goal
    public int FCost => GCost + HCost;
    public PathNode Parent;

    public PathNode(Vector2Int pos, int gCost, int hCost, PathNode parent)
    {
        Position = pos;
        GCost = gCost;
        HCost = hCost;
        Parent = parent;
    }

    public int CompareTo(PathNode other)
    {
        int compare = FCost.CompareTo(other.FCost);
        if (compare == 0)
            compare = HCost.CompareTo(other.HCost);
        return compare;
    }
}
```

### Priority Queue for A*
```csharp
public class PriorityQueue<T> where T : System.IComparable<T>
{
    private List<T> _heap = new List<T>();

    public int Count => _heap.Count;

    public void Enqueue(T item)
    {
        _heap.Add(item);
        HeapifyUp(_heap.Count - 1);
    }

    public T Dequeue()
    {
        T item = _heap[0];
        int lastIndex = _heap.Count - 1;
        _heap[0] = _heap[lastIndex];
        _heap.RemoveAt(lastIndex);
        
        if (_heap.Count > 0)
            HeapifyDown(0);
            
        return item;
    }

    public void UpdatePriority(T item)
    {
        int index = _heap.IndexOf(item);
        if (index >= 0)
        {
            HeapifyUp(index);
            HeapifyDown(index);
        }
    }

    private void HeapifyUp(int index)
    {
        while (index > 0)
        {
            int parent = (index - 1) / 2;
            if (_heap[index].CompareTo(_heap[parent]) >= 0)
                break;
            Swap(index, parent);
            index = parent;
        }
    }

    private void HeapifyDown(int index)
    {
        while (true)
        {
            int smallest = index;
            int left = 2 * index + 1;
            int right = 2 * index + 2;

            if (left < _heap.Count && _heap[left].CompareTo(_heap[smallest]) < 0)
                smallest = left;
            if (right < _heap.Count && _heap[right].CompareTo(_heap[smallest]) < 0)
                smallest = right;

            if (smallest == index)
                break;

            Swap(index, smallest);
            index = smallest;
        }
    }

    private void Swap(int a, int b)
    {
        T temp = _heap[a];
        _heap[a] = _heap[b];
        _heap[b] = temp;
    }
}
```

---

## Unity NavMesh

Unity's built-in pathfinding system. Use this when possible!

### Basic NavMeshAgent Usage
```csharp
using UnityEngine;
using UnityEngine.AI;

public class NavMeshEnemy : MonoBehaviour
{
    [SerializeField] private float _stoppingDistance = 1.5f;
    
    private NavMeshAgent _agent;
    private Transform _target;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
    }

    public void SetTarget(Transform target)
    {
        _target = target;
    }

    private void Update()
    {
        if (_target == null) return;

        _agent.SetDestination(_target.position);

        // Check if arrived
        if (!_agent.pathPending && _agent.remainingDistance <= _stoppingDistance)
        {
            OnArrived();
        }
    }

    private void OnArrived()
    {
        // Attack or other behavior
    }

    // Useful properties
    public bool IsMoving => _agent.velocity.magnitude > 0.1f;
    public bool HasPath => _agent.hasPath;
    public float RemainingDistance => _agent.remainingDistance;
}
```

### NavMesh Configuration Tips
```csharp
// Agent settings
_agent.speed = 5f;              // Movement speed
_agent.angularSpeed = 120f;     // Rotation speed
_agent.acceleration = 8f;       // How fast to reach max speed
_agent.stoppingDistance = 0.5f; // Stop this far from destination
_agent.autoBraking = true;      // Slow down when approaching destination

// Path settings
_agent.autoRepath = true;       // Recalculate when path becomes invalid
_agent.areaMask = NavMesh.AllAreas; // Which areas can be traversed

// Manual control
_agent.isStopped = true;        // Pause movement
_agent.ResetPath();             // Clear current path
_agent.Warp(position);          // Teleport (validates position on NavMesh)
```

### Dynamic Obstacles
```csharp
// NavMeshObstacle for moving obstacles
public class DynamicDoor : MonoBehaviour
{
    private NavMeshObstacle _obstacle;

    private void Awake()
    {
        _obstacle = GetComponent<NavMeshObstacle>();
        _obstacle.carving = true;  // Cut hole in NavMesh
    }

    public void Open()
    {
        _obstacle.enabled = false; // Allow passage
    }

    public void Close()
    {
        _obstacle.enabled = true;  // Block path
    }
}
```

### Runtime NavMesh Building
```csharp
using UnityEngine.AI;

public class RuntimeNavMesh : MonoBehaviour
{
    private NavMeshSurface _surface;

    private void Awake()
    {
        _surface = GetComponent<NavMeshSurface>();
    }

    public void RebuildNavMesh()
    {
        _surface.BuildNavMesh(); // Synchronous
    }

    public async void RebuildNavMeshAsync()
    {
        var operation = _surface.UpdateNavMesh(_surface.navMeshData);
        while (!operation.isDone)
        {
            await System.Threading.Tasks.Task.Yield();
        }
    }
}
```

---

## Steering Behaviors

Smooth, natural-looking movement. Great combined with pathfinding.

### Core Steering Behaviors
```csharp
public class SteeringBehaviors : MonoBehaviour
{
    [SerializeField] private float _maxSpeed = 5f;
    [SerializeField] private float _maxForce = 10f;

    private Vector3 _velocity;

    private void Update()
    {
        // Combine steering forces
        Vector3 steering = Vector3.zero;
        
        // Add behaviors with weights
        steering += Seek(_target.position) * 1.0f;
        steering += AvoidObstacles() * 2.0f;
        steering += Separation(_neighbors) * 1.5f;
        
        // Limit steering force
        steering = Vector3.ClampMagnitude(steering, _maxForce);
        
        // Apply steering
        _velocity += steering * Time.deltaTime;
        _velocity = Vector3.ClampMagnitude(_velocity, _maxSpeed);
        
        // Move
        transform.position += _velocity * Time.deltaTime;
        
        // Face movement direction
        if (_velocity.magnitude > 0.1f)
        {
            transform.forward = _velocity.normalized;
        }
    }

    // SEEK: Move toward target
    private Vector3 Seek(Vector3 target)
    {
        Vector3 desired = (target - transform.position).normalized * _maxSpeed;
        return desired - _velocity;
    }

    // FLEE: Move away from target
    private Vector3 Flee(Vector3 target)
    {
        Vector3 desired = (transform.position - target).normalized * _maxSpeed;
        return desired - _velocity;
    }

    // ARRIVE: Slow down when approaching target
    private Vector3 Arrive(Vector3 target, float slowingRadius = 3f)
    {
        Vector3 toTarget = target - transform.position;
        float distance = toTarget.magnitude;

        if (distance < 0.1f)
            return -_velocity; // Stop

        float speed = distance < slowingRadius
            ? _maxSpeed * (distance / slowingRadius)
            : _maxSpeed;

        Vector3 desired = toTarget.normalized * speed;
        return desired - _velocity;
    }

    // PURSUE: Predict target's future position
    private Vector3 Pursue(Transform target, Vector3 targetVelocity)
    {
        float prediction = Vector3.Distance(transform.position, target.position) / _maxSpeed;
        Vector3 futurePos = target.position + targetVelocity * prediction;
        return Seek(futurePos);
    }

    // EVADE: Flee from predicted position
    private Vector3 Evade(Transform target, Vector3 targetVelocity)
    {
        float prediction = Vector3.Distance(transform.position, target.position) / _maxSpeed;
        Vector3 futurePos = target.position + targetVelocity * prediction;
        return Flee(futurePos);
    }

    // WANDER: Random movement
    private float _wanderAngle = 0f;
    private Vector3 Wander(float circleDistance = 2f, float circleRadius = 1f, float angleChange = 0.5f)
    {
        Vector3 circleCenter = _velocity.normalized * circleDistance;
        
        _wanderAngle += Random.Range(-angleChange, angleChange);
        
        Vector3 displacement = new Vector3(
            Mathf.Cos(_wanderAngle) * circleRadius,
            0,
            Mathf.Sin(_wanderAngle) * circleRadius
        );

        return circleCenter + displacement;
    }

    // OBSTACLE AVOIDANCE
    private Vector3 AvoidObstacles(float lookAhead = 3f, float avoidForce = 5f)
    {
        Vector3 steering = Vector3.zero;
        
        if (Physics.Raycast(transform.position, _velocity.normalized, out RaycastHit hit, lookAhead))
        {
            // Steer away from obstacle
            Vector3 avoidDirection = Vector3.Reflect(_velocity.normalized, hit.normal);
            steering = avoidDirection * avoidForce;
        }

        return steering;
    }

    // SEPARATION: Avoid crowding neighbors
    private Vector3 Separation(List<Transform> neighbors, float desiredSeparation = 2f)
    {
        Vector3 steering = Vector3.zero;
        int count = 0;

        foreach (var neighbor in neighbors)
        {
            float distance = Vector3.Distance(transform.position, neighbor.position);
            
            if (distance > 0 && distance < desiredSeparation)
            {
                Vector3 diff = (transform.position - neighbor.position).normalized;
                diff /= distance; // Weight by distance
                steering += diff;
                count++;
            }
        }

        if (count > 0)
            steering /= count;

        return steering;
    }

    // ALIGNMENT: Match neighbors' heading
    private Vector3 Alignment(List<SteeringBehaviors> neighbors)
    {
        Vector3 avgVelocity = Vector3.zero;
        int count = 0;

        foreach (var neighbor in neighbors)
        {
            avgVelocity += neighbor._velocity;
            count++;
        }

        if (count > 0)
        {
            avgVelocity /= count;
            return avgVelocity - _velocity;
        }

        return Vector3.zero;
    }

    // COHESION: Move toward center of neighbors
    private Vector3 Cohesion(List<Transform> neighbors)
    {
        Vector3 center = Vector3.zero;
        int count = 0;

        foreach (var neighbor in neighbors)
        {
            center += neighbor.position;
            count++;
        }

        if (count > 0)
        {
            center /= count;
            return Seek(center);
        }

        return Vector3.zero;
    }
}
```

### Path Following
```csharp
public class PathFollower : MonoBehaviour
{
    [SerializeField] private float _pathRadius = 1f;
    [SerializeField] private float _lookAhead = 2f;

    private List<Vector3> _path;
    private int _currentWaypoint = 0;

    public void SetPath(List<Vector3> path)
    {
        _path = path;
        _currentWaypoint = 0;
    }

    public Vector3 FollowPath()
    {
        if (_path == null || _path.Count == 0)
            return Vector3.zero;

        // Get target waypoint
        Vector3 target = _path[_currentWaypoint];
        
        // Check if we've reached current waypoint
        if (Vector3.Distance(transform.position, target) < _pathRadius)
        {
            _currentWaypoint++;
            if (_currentWaypoint >= _path.Count)
            {
                // Path complete
                return Vector3.zero;
            }
            target = _path[_currentWaypoint];
        }

        // Seek the waypoint
        return Seek(target);
    }
}
```

---

## Multi-Agent Coordination

Patterns for coordinating multiple AI agents together.

### Flocking (Boids)
```csharp
public class FlockingAgent : MonoBehaviour
{
    [Header("Flocking Weights")]
    [SerializeField] private float _separationWeight = 1.5f;
    [SerializeField] private float _alignmentWeight = 1.0f;
    [SerializeField] private float _cohesionWeight = 1.0f;
    
    [Header("Settings")]
    [SerializeField] private float _neighborRadius = 5f;
    [SerializeField] private float _maxSpeed = 5f;

    private Vector3 _velocity;
    private List<FlockingAgent> _allAgents;

    public void Initialize(List<FlockingAgent> allAgents)
    {
        _allAgents = allAgents;
        _velocity = Random.insideUnitSphere * _maxSpeed;
        _velocity.y = 0;
    }

    private void Update()
    {
        var neighbors = GetNeighbors();
        
        Vector3 separation = CalculateSeparation(neighbors) * _separationWeight;
        Vector3 alignment = CalculateAlignment(neighbors) * _alignmentWeight;
        Vector3 cohesion = CalculateCohesion(neighbors) * _cohesionWeight;

        _velocity += separation + alignment + cohesion;
        _velocity = Vector3.ClampMagnitude(_velocity, _maxSpeed);
        _velocity.y = 0;

        transform.position += _velocity * Time.deltaTime;
        
        if (_velocity.magnitude > 0.1f)
            transform.forward = _velocity.normalized;
    }

    private List<FlockingAgent> GetNeighbors()
    {
        var neighbors = new List<FlockingAgent>();
        
        foreach (var agent in _allAgents)
        {
            if (agent == this) continue;
            
            float distance = Vector3.Distance(transform.position, agent.transform.position);
            if (distance < _neighborRadius)
                neighbors.Add(agent);
        }
        
        return neighbors;
    }

    // ... Separation, Alignment, Cohesion methods from SteeringBehaviors
}
```

### Formation Movement
```csharp
public class FormationManager : MonoBehaviour
{
    [SerializeField] private List<Transform> _units;
    [SerializeField] private FormationType _formation = FormationType.Line;
    [SerializeField] private float _spacing = 2f;

    public enum FormationType { Line, Column, Wedge, Circle }

    public void MoveFormation(Vector3 destination)
    {
        Vector3[] offsets = GetFormationOffsets(_units.Count);
        
        for (int i = 0; i < _units.Count; i++)
        {
            Vector3 targetPos = destination + transform.rotation * offsets[i];
            // Command unit to move to targetPos
            _units[i].GetComponent<NavMeshAgent>().SetDestination(targetPos);
        }
    }

    private Vector3[] GetFormationOffsets(int count)
    {
        Vector3[] offsets = new Vector3[count];

        switch (_formation)
        {
            case FormationType.Line:
                for (int i = 0; i < count; i++)
                {
                    offsets[i] = new Vector3((i - count / 2f) * _spacing, 0, 0);
                }
                break;

            case FormationType.Column:
                for (int i = 0; i < count; i++)
                {
                    offsets[i] = new Vector3(0, 0, -i * _spacing);
                }
                break;

            case FormationType.Wedge:
                int row = 0;
                int col = 0;
                for (int i = 0; i < count; i++)
                {
                    offsets[i] = new Vector3(col * _spacing, 0, -row * _spacing);
                    col++;
                    if (col > row)
                    {
                        row++;
                        col = -row;
                    }
                }
                break;

            case FormationType.Circle:
                float angleStep = 360f / count;
                for (int i = 0; i < count; i++)
                {
                    float angle = i * angleStep * Mathf.Deg2Rad;
                    offsets[i] = new Vector3(
                        Mathf.Sin(angle) * _spacing * 2,
                        0,
                        Mathf.Cos(angle) * _spacing * 2
                    );
                }
                break;
        }

        return offsets;
    }
}
```

### Task Assignment (from Battlecode patterns)
```csharp
public class TaskManager : MonoBehaviour
{
    private List<AIAgent> _agents = new List<AIAgent>();
    private Queue<Task> _taskQueue = new Queue<Task>();

    public void AssignTasks()
    {
        // Sort agents by ID for consistent behavior
        var availableAgents = _agents
            .Where(a => !a.HasTask)
            .OrderBy(a => a.ID)
            .ToList();

        foreach (var agent in availableAgents)
        {
            if (_taskQueue.Count == 0) break;
            
            Task task = _taskQueue.Dequeue();
            agent.AssignTask(task);
        }
    }

    // ID-based role assignment (Battlecode pattern)
    public void AssignRolesByID()
    {
        foreach (var agent in _agents)
        {
            // Use ID to spread out behaviors
            switch (agent.ID % 5)
            {
                case 0:
                case 1:
                    agent.SetRole(AgentRole.Gatherer);
                    break;
                case 2:
                case 3:
                    agent.SetRole(AgentRole.Scout);
                    break;
                case 4:
                    agent.SetRole(AgentRole.Guard);
                    break;
            }
        }
    }
}
```

---

## Pattern Selection Guide

| Scenario | Recommended Pattern |
|----------|--------------------|
| Simple enemy AI | Finite State Machine |
| Complex NPC behaviors | Behavior Tree |
| Grid-based movement | Custom A* |
| 3D environment navigation | Unity NavMesh |
| Natural-looking movement | Steering Behaviors |
| Crowd of units | Flocking + Steering |
| Squad tactics | Formation + FSM |
| RTS game | NavMesh + Behavior Trees + Formations |

### Combining Patterns

```csharp
// Example: NavMesh for pathfinding + Steering for smooth movement
public class HybridAgent : MonoBehaviour
{
    private NavMeshAgent _navAgent;
    private SteeringBehaviors _steering;
    
    private void Update()
    {
        // Use NavMesh for high-level path
        if (_navAgent.hasPath)
        {
            // Get next corner as steering target
            Vector3 target = _navAgent.path.corners.Length > 1
                ? _navAgent.path.corners[1]
                : _navAgent.destination;
            
            // Use steering for smooth movement to corner
            Vector3 steering = _steering.Arrive(target);
            steering += _steering.AvoidObstacles();
            steering += _steering.Separation(GetNearbyAgents());
            
            // Apply steering instead of letting NavMesh control directly
            _navAgent.velocity = Vector3.ClampMagnitude(
                _navAgent.velocity + steering * Time.deltaTime,
                _navAgent.speed
            );
        }
    }
}
```

---

## Performance Checklist

- [ ] Use `sqrMagnitude` instead of `Distance` for comparisons
- [ ] Stagger AI updates across frames (don't update all agents every frame)
- [ ] Pool pathfinding requests
- [ ] Use spatial partitioning for neighbor queries
- [ ] Cache NavMesh queries when possible
- [ ] Limit behavior tree depth (flatten when possible)
- [ ] Profile with Unity Profiler to find bottlenecks
