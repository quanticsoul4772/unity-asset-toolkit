using System.Collections.Generic;
using UnityEngine;
using EasyPath;

namespace SwarmAI
{
    /// <summary>
    /// Core component for swarm agents.
    /// Provides state machine, steering behaviors, and movement.
    /// </summary>
    public class SwarmAgent : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float _maxSpeed = 5f;
        [SerializeField] private float _maxForce = 10f;
        [SerializeField] private float _rotationSpeed = 360f;
        [SerializeField] private float _stoppingDistance = 0.5f;
        [SerializeField] private float _mass = 1f;
        
        [Header("Neighbor Detection")]
        [SerializeField] private float _neighborRadius = 5f;
        [SerializeField] private LayerMask _neighborLayers = -1;
        
        [Header("Debug")]
        [SerializeField] private bool _showDebugGizmos = true;
        [SerializeField] private bool _verboseDebug = false;
        [SerializeField] private Color _velocityColor = Color.green;
        [SerializeField] private Color _neighborColor = new Color(1f, 1f, 0f, 0.3f);
        
        // Debug logging throttle
        private float _lastDebugLogTime;
        private const float DebugLogInterval = 1f; // Log every 1 second
        
        // Internal state
        private int _agentId = -1;
        private AgentState _currentState;
        private Vector3 _velocity;
        private Vector3 _steeringForce;
        private Vector3 _targetPosition;
        private bool _hasTarget;
        private List<WeightedBehavior> _behaviors;
        private EasyPathAgent _pathAgent;
        private List<SwarmAgent> _cachedNeighbors;
        private float _lastNeighborQueryTime;
        
        // Behavior wrapper with weight
        private struct WeightedBehavior
        {
            public IBehavior Behavior;
            public float Weight;
        }
        
        #region Properties
        
        /// <summary>
        /// Unique ID for this agent. Assigned by SwarmManager.
        /// </summary>
        public int AgentId => _agentId;
        
        /// <summary>
        /// Current position in world space.
        /// </summary>
        public Vector3 Position => transform.position;
        
        /// <summary>
        /// Current forward direction.
        /// </summary>
        public Vector3 Forward => transform.forward;
        
        /// <summary>
        /// Current velocity vector.
        /// </summary>
        public Vector3 Velocity => _velocity;
        
        /// <summary>
        /// Current speed (velocity magnitude).
        /// </summary>
        public float Speed => _velocity.magnitude;
        
        /// <summary>
        /// Maximum movement speed.
        /// </summary>
        public float MaxSpeed { get => _maxSpeed; set => _maxSpeed = value; }
        
        /// <summary>
        /// Maximum steering force.
        /// </summary>
        public float MaxForce { get => _maxForce; set => _maxForce = value; }
        
        /// <summary>
        /// Mass for physics calculations.
        /// </summary>
        public float Mass { get => _mass; set => _mass = Mathf.Max(0.01f, value); }
        
        /// <summary>
        /// Radius for neighbor detection.
        /// </summary>
        public float NeighborRadius { get => _neighborRadius; set => _neighborRadius = value; }
        
        /// <summary>
        /// Stopping distance for arrival.
        /// </summary>
        public float StoppingDistance { get => _stoppingDistance; set => _stoppingDistance = value; }
        
        /// <summary>
        /// Current state in the FSM.
        /// </summary>
        public AgentState CurrentState => _currentState;
        
        /// <summary>
        /// Type of the current state.
        /// </summary>
        public AgentStateType CurrentStateType => _currentState?.Type ?? AgentStateType.Idle;
        
        /// <summary>
        /// Whether the agent has a movement target.
        /// </summary>
        public bool HasTarget => _hasTarget;
        
        /// <summary>
        /// Current movement target position.
        /// </summary>
        public Vector3 TargetPosition => _targetPosition;
        
        /// <summary>
        /// Whether this agent is registered with the SwarmManager.
        /// </summary>
        public bool IsRegistered => _agentId >= 0;
        
        /// <summary>
        /// EasyPath agent component (optional, for pathfinding).
        /// </summary>
        public EasyPathAgent PathAgent => _pathAgent;
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Fired when the agent's state changes.
        /// </summary>
        public event System.Action<AgentState, AgentState> OnStateChanged;
        
        /// <summary>
        /// Fired when the agent reaches its target.
        /// </summary>
        public event System.Action OnTargetReached;
        
        /// <summary>
        /// Fired when the agent receives a message.
        /// </summary>
        public event System.Action<SwarmMessage> OnMessageReceived;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            _behaviors = new List<WeightedBehavior>();
            _cachedNeighbors = new List<SwarmAgent>();
            _pathAgent = GetComponent<EasyPathAgent>();
            
            // Set initial state
            SetState(new IdleState());
        }
        
        private void OnEnable()
        {
            // Register with SwarmManager
            if (SwarmManager.Instance != null)
            {
                SwarmManager.Instance.RegisterAgent(this);
            }
        }
        
        private void OnDisable()
        {
            // Unregister from SwarmManager
            if (SwarmManager.Instance != null)
            {
                SwarmManager.Instance.UnregisterAgent(this);
            }
        }
        
        private void Update()
        {
            // Execute current state
            _currentState?.Execute();
            
            // Check for state transitions
            AgentState newState = _currentState?.CheckTransitions();
            if (newState != null && newState != _currentState)
            {
                SetState(newState);
            }
            
            // Calculate and apply steering
            CalculateSteering();
            ApplyMovement();
        }
        
        private void FixedUpdate()
        {
            _currentState?.FixedExecute();
        }
        
        #endregion
        
        #region State Machine
        
        /// <summary>
        /// Change to a new state.
        /// </summary>
        public void SetState(AgentState newState)
        {
            if (newState == null) return;
            
            AgentState oldState = _currentState;
            
            // Exit old state
            oldState?.Exit();
            
            // Initialize and enter new state
            newState.Initialize(this);
            _currentState = newState;
            _currentState.Enter();
            
            OnStateChanged?.Invoke(oldState, newState);
        }
        
        #endregion
        
        #region Movement
        
        /// <summary>
        /// Set a target position to move toward.
        /// </summary>
        public void SetTarget(Vector3 position)
        {
            _targetPosition = position;
            _hasTarget = true;
            
            // Use EasyPath if available
            if (_pathAgent != null)
            {
                _pathAgent.SetDestination(position);
            }
        }
        
        /// <summary>
        /// Clear the current movement target.
        /// </summary>
        public void ClearTarget()
        {
            _hasTarget = false;
            
            if (_pathAgent != null)
            {
                _pathAgent.Stop();
            }
        }
        
        /// <summary>
        /// Stop all movement immediately.
        /// </summary>
        public void Stop()
        {
            _velocity = Vector3.zero;
            _steeringForce = Vector3.zero;
            ClearTarget();
        }
        
        /// <summary>
        /// Apply an external force to the agent.
        /// </summary>
        public void ApplyForce(Vector3 force)
        {
            _steeringForce += force;
        }
        
        private void CalculateSteering()
        {
            _steeringForce = Vector3.zero;
            
            bool shouldLog = _verboseDebug && (Time.time - _lastDebugLogTime >= DebugLogInterval);
            
            if (shouldLog)
            {
                Debug.Log($"[SwarmAgent {name}] CalculateSteering: {_behaviors.Count} behaviors registered");
            }
            
            // Calculate behavior forces
            int activeCount = 0;
            foreach (var wb in _behaviors)
            {
                if (wb.Behavior.IsActive)
                {
                    activeCount++;
                    Vector3 force = wb.Behavior.CalculateForce(this);
                    Vector3 weightedForce = force * wb.Weight * wb.Behavior.Weight;
                    _steeringForce += weightedForce;
                    
                    if (shouldLog && force.sqrMagnitude > 0.001f)
                    {
                        Debug.Log($"[SwarmAgent {name}]   {wb.Behavior.Name}: force={force}, weight={wb.Weight}*{wb.Behavior.Weight}, weighted={weightedForce}");
                    }
                }
            }
            
            if (shouldLog)
            {
                Debug.Log($"[SwarmAgent {name}]   Active behaviors: {activeCount}/{_behaviors.Count}, Total steering force: {_steeringForce}");
            }
            
            // Add seek force if we have a target and no pathfinding agent
            if (_hasTarget && _pathAgent == null)
            {
                Vector3 toTarget = _targetPosition - Position;
                float distance = toTarget.magnitude;
                
                if (distance > _stoppingDistance)
                {
                    // Arrive behavior - slow down as we approach
                    float targetSpeed = _maxSpeed;
                    if (distance < _stoppingDistance * 3f)
                    {
                        targetSpeed = _maxSpeed * (distance / (_stoppingDistance * 3f));
                    }
                    
                    Vector3 desired = toTarget.normalized * targetSpeed;
                    _steeringForce += (desired - _velocity);
                }
                else
                {
                    // Reached target
                    _hasTarget = false;
                    OnTargetReached?.Invoke();
                }
            }
            
            // Truncate to max force
            if (_steeringForce.sqrMagnitude > _maxForce * _maxForce)
            {
                _steeringForce = _steeringForce.normalized * _maxForce;
            }
            
            if (shouldLog)
            {
                Debug.Log($"[SwarmAgent {name}]   Final steering force (after truncate): {_steeringForce}");
            }
        }
        
        private void ApplyMovement()
        {
            bool shouldLog = _verboseDebug && (Time.time - _lastDebugLogTime >= DebugLogInterval);
            
            // Skip if using pathfinding agent for movement
            if (_pathAgent != null && _pathAgent.IsMoving)
            {
                _velocity = (_pathAgent.Destination - Position).normalized * _pathAgent.Speed;
                if (shouldLog)
                {
                    Debug.Log($"[SwarmAgent {name}] ApplyMovement: Using EasyPathAgent, velocity={_velocity}");
                }
                return;
            }
            
            // Apply steering force (F = ma, so a = F/m)
            Vector3 acceleration = _steeringForce / _mass;
            _velocity += acceleration * Time.deltaTime;
            
            if (shouldLog)
            {
                Debug.Log($"[SwarmAgent {name}] ApplyMovement: steeringForce={_steeringForce}, mass={_mass}, acceleration={acceleration}, velocity={_velocity}");
            }
            
            // Truncate to max speed
            if (_velocity.sqrMagnitude > _maxSpeed * _maxSpeed)
            {
                _velocity = _velocity.normalized * _maxSpeed;
            }
            
            // Apply movement
            if (_velocity.sqrMagnitude > 0.001f)
            {
                // Move
                Vector3 movement = _velocity * Time.deltaTime;
                transform.position += movement;
                
                if (shouldLog)
                {
                    Debug.Log($"[SwarmAgent {name}] ApplyMovement: MOVING! velocity={_velocity}, movement={movement}, newPos={transform.position}");
                    _lastDebugLogTime = Time.time;
                }
                
                // Rotate toward velocity
                Vector3 flatVelocity = new Vector3(_velocity.x, 0, _velocity.z);
                if (flatVelocity.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(flatVelocity);
                    transform.rotation = Quaternion.RotateTowards(
                        transform.rotation,
                        targetRotation,
                        _rotationSpeed * Time.deltaTime
                    );
                }
            }
            else if (shouldLog)
            {
                Debug.Log($"[SwarmAgent {name}] ApplyMovement: NOT moving, velocity too small: {_velocity.sqrMagnitude}");
                _lastDebugLogTime = Time.time;
            }
        }
        
        #endregion
        
        #region Behaviors
        
        /// <summary>
        /// Add a steering behavior with the specified weight.
        /// </summary>
        public void AddBehavior(IBehavior behavior, float weight = 1f)
        {
            if (behavior == null) return;
            
            _behaviors.Add(new WeightedBehavior
            {
                Behavior = behavior,
                Weight = weight
            });
            
            if (_verboseDebug)
            {
                Debug.Log($"[SwarmAgent {name}] AddBehavior: {behavior.Name}, weight={weight}, total behaviors={_behaviors.Count}");
            }
        }
        
        /// <summary>
        /// Remove a steering behavior.
        /// </summary>
        public void RemoveBehavior(IBehavior behavior)
        {
            if (behavior == null) return;
            
            _behaviors.RemoveAll(wb => wb.Behavior == behavior);
        }
        
        /// <summary>
        /// Remove all behaviors of a specific type.
        /// </summary>
        public void RemoveBehaviorsOfType<T>() where T : IBehavior
        {
            _behaviors.RemoveAll(wb => wb.Behavior is T);
        }
        
        /// <summary>
        /// Clear all behaviors.
        /// </summary>
        public void ClearBehaviors()
        {
            _behaviors.Clear();
        }
        
        #endregion
        
        #region Neighbors
        
        /// <summary>
        /// Get nearby agents using the SwarmManager's spatial hash.
        /// Results are cached for the current frame with accurate distance filtering.
        /// </summary>
        public List<SwarmAgent> GetNeighbors()
        {
            // Use cached result if queried this frame
            if (Time.time == _lastNeighborQueryTime)
            {
                return _cachedNeighbors;
            }
            
            _lastNeighborQueryTime = Time.time;
            
            if (SwarmManager.Instance != null)
            {
                // Use the new accurate distance filtering method
                SwarmManager.Instance.GetNeighborsExcluding(Position, _neighborRadius, this, _cachedNeighbors);
            }
            else
            {
                _cachedNeighbors.Clear();
            }
            
            return _cachedNeighbors;
        }
        
        /// <summary>
        /// Get the nearest neighbor, or null if none found.
        /// </summary>
        public SwarmAgent GetNearestNeighbor()
        {
            var neighbors = GetNeighbors();
            if (neighbors.Count == 0) return null;
            
            SwarmAgent nearest = null;
            float nearestDistSq = float.MaxValue;
            
            foreach (var neighbor in neighbors)
            {
                float distSq = (neighbor.Position - Position).sqrMagnitude;
                if (distSq < nearestDistSq)
                {
                    nearestDistSq = distSq;
                    nearest = neighbor;
                }
            }
            
            return nearest;
        }
        
        #endregion
        
        #region Messages
        
        /// <summary>
        /// Receive a message. Called by SwarmManager.
        /// </summary>
        internal void ReceiveMessage(SwarmMessage message)
        {
            // Let the current state handle it first
            bool handled = _currentState?.HandleMessage(message) ?? false;
            
            // Handle common message types
            if (!handled)
            {
                switch (message.Type)
                {
                    case SwarmMessageType.MoveTo:
                        SetTarget(message.Position);
                        SetState(new MovingState(message.Position));
                        break;
                        
                    case SwarmMessageType.Seek:
                        SetTarget(message.Position);
                        SetState(new SeekingState(message.Position));
                        break;
                        
                    case SwarmMessageType.Stop:
                        Stop();
                        SetState(new IdleState());
                        break;
                        
                    case SwarmMessageType.Flee:
                        SetState(new FleeingState(message.Position));
                        break;
                        
                    case SwarmMessageType.Follow:
                        int leaderId = (int)message.Value;
                        SetState(new FollowingState(leaderId));
                        break;
                        
                    case SwarmMessageType.FormationUpdate:
                        SetTarget(message.Position);
                        break;
                        
                    case SwarmMessageType.GatherResource:
                        if (message.Data is ResourceNode resource && !resource.IsDepleted)
                        {
                            SetState(new GatheringState(resource, message.Position));
                        }
                        break;
                        
                    case SwarmMessageType.ReturnToBase:
                        // Only handle if carrying resources
                        if (_currentState is GatheringState gatherState && gatherState.CurrentCarry > 0)
                        {
                            SetState(new ReturningState(message.Position, gatherState.CurrentCarry));
                        }
                        break;
                }
            }
            
            OnMessageReceived?.Invoke(message);
        }
        
        /// <summary>
        /// Send a message to another agent.
        /// </summary>
        public void SendMessage(int targetId, SwarmMessage message)
        {
            SwarmManager.Instance?.SendMessage(targetId, message.Clone(_agentId, targetId));
        }
        
        /// <summary>
        /// Broadcast a message to all agents.
        /// </summary>
        public void BroadcastMessage(SwarmMessage message)
        {
            SwarmManager.Instance?.BroadcastMessage(message.Clone(_agentId, -1));
        }
        
        #endregion
        
        #region Internal
        
        /// <summary>
        /// Set the agent ID. Called by SwarmManager.
        /// </summary>
        internal void SetAgentId(int id)
        {
            _agentId = id;
        }
        
        #endregion
        
        #region Debug
        
        private void OnDrawGizmos()
        {
            if (!_showDebugGizmos) return;
            
            // Draw velocity
            if (_velocity.sqrMagnitude > 0.01f)
            {
                Gizmos.color = _velocityColor;
                Gizmos.DrawRay(Position, _velocity);
            }
            
            // Draw target
            if (_hasTarget)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(Position, _targetPosition);
                Gizmos.DrawWireSphere(_targetPosition, 0.3f);
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            // Draw neighbor radius
            Gizmos.color = _neighborColor;
            Gizmos.DrawWireSphere(Position, _neighborRadius);
            
            // Draw stopping distance
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(Position, _stoppingDistance);
        }
        
        #endregion
    }
}
