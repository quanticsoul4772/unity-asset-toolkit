using System;
using UnityEngine;
using NPCBrain.BehaviorTree;
using NPCBrain.Perception;
using NPCBrain.Criticality;

namespace NPCBrain
{
    /// <summary>
    /// Core component for NPC AI. Manages behavior tree, perception, and criticality.
    /// Subclass this and override CreateBehaviorTree() to create custom NPC archetypes.
    /// </summary>
    public class NPCBrain : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] 
        [Tooltip("Tick interval in seconds. 0 = every frame.")]
        private float _tickInterval = 0f;
        
        [SerializeField]
        [Tooltip("Waypoint path for patrol behaviors.")]
        private WaypointPath _waypointPath;
        
        [Header("Movement")]
        [SerializeField]
        private float _moveSpeed = 5f;
        
        [SerializeField]
        private float _rotationSpeed = 360f;
        
        [SerializeField]
        private float _arrivalDistance = 0.5f;
        
        // Subsystems
        public Blackboard Blackboard { get; private set; }
        public SightSensor Perception { get; private set; }
        public CriticalityController Criticality { get; private set; }
        
        // BT state
        private BTNode _behaviorTree;
        private NodeStatus _lastStatus;
        private float _lastTickTime;
        
        // Movement state (used by MoveTo action)
        private Vector3? _moveTarget;
        private bool _isMoving;
        
        // Events
        public event Action<GameObject> OnTargetAcquired;
        public event Action<GameObject> OnTargetLost;
        public event Action<string> OnStateChanged;
        
        // Properties
        public float MoveSpeed => _moveSpeed;
        public float RotationSpeed => _rotationSpeed;
        public float ArrivalDistance => _arrivalDistance;
        public WaypointPath WaypointPath => _waypointPath;
        public NodeStatus LastStatus => _lastStatus;
        public bool IsMoving => _isMoving;
        
        protected virtual void Awake()
        {
            Blackboard = new Blackboard();
            Perception = GetComponent<SightSensor>();
            Criticality = new CriticalityController();
            _behaviorTree = CreateBehaviorTree();
            
            // Store home position
            Blackboard.Set("homePosition", transform.position);
        }
        
        /// <summary>
        /// Override in subclasses to define the NPC's behavior tree.
        /// </summary>
        protected virtual BTNode CreateBehaviorTree()
        {
            return null;
        }
        
        private void Update()
        {
            // Check tick interval
            if (_tickInterval > 0)
            {
                if (Time.time - _lastTickTime < _tickInterval)
                {
                    // Still process movement between ticks
                    ProcessMovement();
                    return;
                }
                _lastTickTime = Time.time;
            }
            
            // 1. Perception
            Perception?.Tick(this);
            
            // 2. Criticality
            Criticality.Update();
            
            // 3. BT Decision
            if (_behaviorTree != null)
            {
                _lastStatus = _behaviorTree.Tick(this);
            }
            
            // 4. Movement (if target set by MoveTo action)
            ProcessMovement();
        }
        
        private void ProcessMovement()
        {
            if (!_moveTarget.HasValue)
            {
                _isMoving = false;
                return;
            }
            
            Vector3 target = _moveTarget.Value;
            Vector3 toTarget = target - transform.position;
            toTarget.y = 0; // Keep on ground plane
            
            float distance = toTarget.magnitude;
            
            if (distance <= _arrivalDistance)
            {
                _isMoving = false;
                return;
            }
            
            _isMoving = true;
            
            // Rotate toward target
            if (toTarget != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(toTarget);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, 
                    targetRotation, 
                    _rotationSpeed * Time.deltaTime
                );
            }
            
            // Move toward target
            Vector3 movement = toTarget.normalized * _moveSpeed * Time.deltaTime;
            if (movement.magnitude > distance)
            {
                movement = toTarget;
            }
            transform.position += movement;
        }
        
        /// <summary>
        /// Set movement target. Called by MoveTo action.
        /// </summary>
        public void SetMoveTarget(Vector3 target)
        {
            _moveTarget = target;
        }
        
        /// <summary>
        /// Clear movement target. Called when MoveTo completes.
        /// </summary>
        public void ClearMoveTarget()
        {
            _moveTarget = null;
            _isMoving = false;
        }
        
        /// <summary>
        /// Check if arrived at current move target.
        /// </summary>
        public bool HasArrivedAtTarget()
        {
            if (!_moveTarget.HasValue)
            {
                return true;
            }
            
            Vector3 toTarget = _moveTarget.Value - transform.position;
            toTarget.y = 0;
            return toTarget.magnitude <= _arrivalDistance;
        }
        
        /// <summary>
        /// Get the next waypoint from the waypoint path.
        /// </summary>
        public Vector3 GetNextWaypoint()
        {
            if (_waypointPath != null)
            {
                return _waypointPath.GetNext();
            }
            return transform.position;
        }
        
        /// <summary>
        /// Get the current waypoint from the waypoint path.
        /// </summary>
        public Vector3 GetCurrentWaypoint()
        {
            if (_waypointPath != null)
            {
                return _waypointPath.GetCurrent();
            }
            return transform.position;
        }
        
        /// <summary>
        /// Fire target acquired event (called by perception system).
        /// </summary>
        public void NotifyTargetAcquired(GameObject target)
        {
            OnTargetAcquired?.Invoke(target);
        }
        
        /// <summary>
        /// Fire target lost event (called by perception system).
        /// </summary>
        public void NotifyTargetLost(GameObject target)
        {
            OnTargetLost?.Invoke(target);
        }
        
        /// <summary>
        /// Fire state changed event.
        /// </summary>
        public void NotifyStateChanged(string newState)
        {
            Blackboard.Set("currentState", newState);
            OnStateChanged?.Invoke(newState);
        }
    }
}
