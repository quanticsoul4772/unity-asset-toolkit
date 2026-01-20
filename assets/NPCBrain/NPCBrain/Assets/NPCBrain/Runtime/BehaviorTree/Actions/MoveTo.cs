using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using NPCBrain.Debugging;
using NPCBrain.Settings;

namespace NPCBrain.BehaviorTree.Actions
{
    public class MoveTo : BTNode
    {
        private readonly Func<Vector3> _targetGetter;
        private readonly float _arrivalDistanceSqr;
        private readonly float _moveSpeed;
        private readonly float _timeout;
        
        private float _startTime;
        private NavMeshAgent _cachedNavAgent;
        private CharacterController _cachedCharController;
        private EasyPath.EasyPathGrid _cachedGrid;
        private bool _navAgentCached;
        private bool _charControllerCached;
        private bool _gridCached;
        
        // EasyPath state
        private List<Vector3> _currentPath;
        private int _currentWaypointIndex;
        private Vector3 _lastTargetPosition;
        private float _lastPathCalcTime;
        
        // Stuck detection state
        private Vector3 _lastStuckCheckPosition;
        private float _lastStuckCheckTime;
        private int _stuckCounter;
        private int _recoveryAttempts;
        private float _lastRecoveryTime;
        private Vector3 _recoveryDirection;
        
        // Debug logging
        private float _lastDebugLogTime;
        private Vector3 _lastLoggedTarget;
        
        // Smart logging - only log when path changes significantly
        private int _lastLoggedWaypointCount = -1;
        private bool _hasLoggedInitialPath;
        
        public MoveTo(Func<Vector3> targetGetter, float arrivalDistance, float moveSpeed, float timeout)
        {
            _targetGetter = targetGetter ?? throw new ArgumentNullException(nameof(targetGetter));
            _arrivalDistanceSqr = arrivalDistance * arrivalDistance;
            _moveSpeed = moveSpeed;
            _timeout = timeout;
            Name = "MoveTo";
        }
        
        public MoveTo(Func<Vector3> targetGetter, float arrivalDistance, float moveSpeed)
            : this(targetGetter, arrivalDistance, moveSpeed, 30f)
        {
        }
        
        public MoveTo(Func<Vector3> targetGetter, float arrivalDistance)
            : this(targetGetter, arrivalDistance, 5f, 30f)
        {
        }
        
        public MoveTo(Func<Vector3> targetGetter)
            : this(targetGetter, 0.5f, 5f, 30f)
        {
        }
        
        protected override void OnEnter(NPCBrainController brain)
        {
            _startTime = Time.time;
            _currentPath = null;
            _currentWaypointIndex = 0;
            _lastTargetPosition = Vector3.zero;
            _lastPathCalcTime = 0f;
            _lastStuckCheckPosition = brain.transform.position;
            _lastStuckCheckTime = Time.time;
            _stuckCounter = 0;
            _recoveryAttempts = 0;
            _lastRecoveryTime = 0f;
            _recoveryDirection = Vector3.zero;
            _lastLoggedWaypointCount = -1;
            _hasLoggedInitialPath = false;
            
            if (!_navAgentCached)
            {
                _cachedNavAgent = brain.GetComponent<NavMeshAgent>();
                _navAgentCached = true;
            }
            
            if (!_charControllerCached)
            {
                _cachedCharController = brain.GetComponent<CharacterController>();
                _charControllerCached = true;
            }
            
            if (!_gridCached)
            {
                _cachedGrid = UnityEngine.Object.FindFirstObjectByType<EasyPath.EasyPathGrid>();
                _gridCached = true;
            }
        }
        
        protected override void OnExit(NPCBrainController brain)
        {
            if (_cachedNavAgent != null && _cachedNavAgent.isOnNavMesh)
            {
                _cachedNavAgent.ResetPath();
            }
            
            // Unregister path from visualizer
            NPCPathVisualizer.UnregisterPath(brain.name);
        }
        
        public override void Reset()
        {
            base.Reset();
            _navAgentCached = false;
            _cachedNavAgent = null;
            _charControllerCached = false;
            _cachedCharController = null;
            _gridCached = false;
            _cachedGrid = null;
            _currentPath = null;
            _currentWaypointIndex = 0;
            _stuckCounter = 0;
            _recoveryAttempts = 0;
            _lastRecoveryTime = 0f;
            _recoveryDirection = Vector3.zero;
            _lastLoggedWaypointCount = -1;
            _hasLoggedInitialPath = false;
        }
        
        protected override NodeStatus Tick(NPCBrainController brain)
        {
            Vector3 target = _targetGetter();
            Vector3 currentPos = brain.transform.position;
            float distanceSqr = (currentPos - target).sqrMagnitude;
            
            if (distanceSqr <= _arrivalDistanceSqr)
            {
                Debug.Log($"<color=green>[MoveTo]</color> {brain.name} ARRIVED at target (dist={Mathf.Sqrt(distanceSqr):F2}m, arrivalDist={Mathf.Sqrt(_arrivalDistanceSqr):F2}m)");
                return NodeStatus.Success;
            }
            
            if (Time.time - _startTime > _timeout)
            {
                Debug.LogWarning($"<color=red>[MoveTo]</color> {brain.name} TIMEOUT after {_timeout}s trying to reach {target}");
                return NodeStatus.Failure;
            }
            
            if (_cachedNavAgent != null && _cachedNavAgent.isOnNavMesh)
            {
                return MoveViaNavMesh(_cachedNavAgent, target);
            }
            
            // Use EasyPath A* + CharacterController if grid exists (smart navigation + collision)
            if (_cachedGrid != null && _cachedCharController != null)
            {
                return MoveViaEasyPath(brain, _cachedCharController, brain.transform, target);
            }
            
            // Use CharacterController alone if available (handles collision detection but no pathfinding)
            if (_cachedCharController != null)
            {
                Debug.LogWarning($"<color=orange>[MoveTo]</color> {brain.name} using CharController only (no grid!) - grid={_cachedGrid != null}");
                return MoveViaCharacterController(_cachedCharController, brain.transform, target);
            }
            
            // DIAGNOSTIC: Log when falling through to MoveDirectly (no CharController!)
            Debug.LogWarning($"<color=red>[MoveTo]</color> {brain.name} using MoveDirectly (NO CharController!) - grid={_cachedGrid != null}, charCtrl={_cachedCharController != null}");
            return MoveDirectly(brain.transform, target, brain.name);
        }
        
        private NodeStatus MoveViaNavMesh(NavMeshAgent agent, Vector3 target)
        {
            agent.SetDestination(target);
            
            if (agent.pathPending)
            {
                return NodeStatus.Running;
            }
            
            if (agent.pathStatus == NavMeshPathStatus.PathInvalid)
            {
                return NodeStatus.Failure;
            }
            
            if (agent.remainingDistance * agent.remainingDistance <= _arrivalDistanceSqr && !agent.pathPending)
            {
                return NodeStatus.Success;
            }
            
            return NodeStatus.Running;
        }
        
        /// <summary>
        /// Moves the NPC using EasyPath A* pathfinding combined with CharacterController for collision.
        /// Integrates with Criticality system:
        /// - Temperature affects path recalculation frequency (low temp = frequent recalc = optimal paths)
        /// - Inertia affects waypoint tolerance (high inertia = tight tolerance = precise following)
        /// </summary>
        private NodeStatus MoveViaEasyPath(NPCBrainController brain, CharacterController controller, Transform transform, Vector3 target)
        {
            // Get criticality values for adaptive behavior
            float temperature = brain.Criticality?.Temperature ?? 1f;
            float inertia = brain.Criticality?.Inertia ?? 0.5f;
            
            // Temperature-based path recalculation interval:
            // See PathfindingSettings for parameter documentation
            float recalcInterval = PathfindingSettings.BaseRecalcInterval * temperature;
            
            // Inertia-based waypoint tolerance:
            // See PathfindingSettings for parameter documentation
            float waypointTolerance = PathfindingSettings.BaseWaypointTolerance + 
                (1f - inertia) * PathfindingSettings.InertiaToleranceMultiplier;
            
            // Check if we need to recalculate path
            bool needsNewPath = _currentPath == null || _currentPath.Count == 0;
            bool targetMoved = Vector3.Distance(target, _lastTargetPosition) > PathfindingSettings.TargetMovedThreshold;
            bool timeForRecalc = Time.time - _lastPathCalcTime > recalcInterval;
            
            if (needsNewPath || (targetMoved && timeForRecalc))
            {
                _currentPath = _cachedGrid.FindPath(transform.position, target);
                _currentWaypointIndex = 0;
                _lastTargetPosition = target;
                _lastPathCalcTime = Time.time;
                
                // Smart logging: only log when path changes significantly
                if (NPCBrainDebug.IsEnabled(NPCBrainDebug.Category.General))
                {
                    int newWaypointCount = _currentPath?.Count ?? 0;
                    bool isFirstPath = !_hasLoggedInitialPath;
                    bool waypointCountChangedSignificantly = Mathf.Abs(newWaypointCount - _lastLoggedWaypointCount) >= PathfindingSettings.SignificantWaypointChange;
                    bool pathFailed = _currentPath == null || _currentPath.Count == 0;
                    
                    if (isFirstPath || waypointCountChangedSignificantly || pathFailed)
                    {
                        string pathResult = _currentPath != null ? $"{_currentPath.Count} waypoints" : "FAILED";
                        Debug.Log($"<color=cyan>[MoveTo]</color> {brain.name} path calc: {pathResult} | T={temperature:F2} (recalc={recalcInterval:F2}s) | I={inertia:F2} (tol={waypointTolerance:F2}m)");
                        _lastLoggedWaypointCount = newWaypointCount;
                        _hasLoggedInitialPath = true;
                    }
                }
                
                // Register path with visualizer for debug drawing
                NPCPathVisualizer.RegisterPath(brain.name, _currentPath, _currentWaypointIndex, 
                    transform.position, target);
                
                if (_currentPath == null || _currentPath.Count == 0)
                {
                    // Path failed - target is unreachable
                    // Don't move directly toward a blocked target as that will cause NPC to get stuck against walls
                    if (NPCBrainDebug.IsEnabled(NPCBrainDebug.Category.General))
                    {
                        Debug.Log($"<color=red>[MoveTo]</color> {brain.name} cannot find path to target - waiting for recalc");
                    }
                    return NodeStatus.Running; // Wait and retry on next recalc interval
                }
            }
            
            // Follow the current path
            if (_currentWaypointIndex >= _currentPath.Count)
            {
                // Reached end of path
                _currentPath = null;
                return NodeStatus.Running;
            }
            
            Vector3 currentWaypoint = _currentPath[_currentWaypointIndex];
            currentWaypoint.y = transform.position.y; // Keep on same Y level
            
            Vector3 toWaypoint = currentWaypoint - transform.position;
            float distanceToWaypoint = toWaypoint.magnitude;
            
            // Check if we've reached the current waypoint (using criticality-adjusted tolerance)
            if (distanceToWaypoint <= waypointTolerance)
            {
                _currentWaypointIndex++;
                
                // Update visualizer with new waypoint index
                NPCPathVisualizer.UpdatePathProgress(brain.name, _currentWaypointIndex, transform.position);
                
                // Skip waypoints if low inertia (more aggressive corner cutting)
                if (inertia < PathfindingSettings.CornerCuttingInertiaThreshold && _currentWaypointIndex < _currentPath.Count - 1)
                {
                    // Try to skip to a further waypoint if we have actual line of sight (raycast check)
                    int skipTarget = Mathf.Min(_currentWaypointIndex + PathfindingSettings.CornerCuttingSkipCount, _currentPath.Count - 1);
                    Vector3 skipPos = _currentPath[skipTarget];
                    skipPos.y = transform.position.y;
                    
                    // Raycast to check for actual line of sight - don't skip through walls
                    Vector3 rayOrigin = transform.position + Vector3.up * PathfindingSettings.CornerCuttingRaycastHeight;
                    Vector3 rayTarget = skipPos + Vector3.up * PathfindingSettings.CornerCuttingRaycastHeight;
                    Vector3 rayDirection = rayTarget - rayOrigin;
                    float rayDistance = rayDirection.magnitude;
                    
                    // Get obstacle layer from grid if available, otherwise check all layers
                    int obstacleMask = _cachedGrid != null ? (1 << LayerMask.NameToLayer("Obstacles")) : ~0;
                    if (obstacleMask == (1 << -1)) obstacleMask = ~0; // Fallback if layer doesn't exist
                    
                    if (rayDistance < PathfindingSettings.CornerCuttingMaxDistance && 
                        !Physics.Raycast(rayOrigin, rayDirection.normalized, rayDistance, obstacleMask))
                    {
                        // Clear line of sight - safe to skip waypoints
                        _currentWaypointIndex = skipTarget;
                    }
                }
                
                if (_currentWaypointIndex >= _currentPath.Count)
                {
                    _currentPath = null;
                    return NodeStatus.Running;
                }
                
                currentWaypoint = _currentPath[_currentWaypointIndex];
                currentWaypoint.y = transform.position.y;
                toWaypoint = currentWaypoint - transform.position;
                distanceToWaypoint = toWaypoint.magnitude;
            }
            
            // Move toward current waypoint using CharacterController
            if (distanceToWaypoint > PathfindingSettings.MinMovementForRotation)
            {
                Vector3 direction = toWaypoint / distanceToWaypoint;
                Vector3 movement = direction * _moveSpeed * Time.deltaTime;
                
                // Add gravity
                if (!controller.isGrounded)
                {
                    movement.y = -PathfindingSettings.Gravity * Time.deltaTime;
                }
                
                controller.Move(movement);
                
                if (direction != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(direction);
                }
            }
            
            // Stuck detection and recovery - if we haven't moved much over time, we might be blocked
            if (Time.time - _lastStuckCheckTime > PathfindingSettings.StuckCheckInterval)
            {
                float movedDistance = Vector3.Distance(transform.position, _lastStuckCheckPosition);
                if (movedDistance < PathfindingSettings.StuckDistanceThreshold)
                {
                    _stuckCounter++;
                    if (_stuckCounter >= PathfindingSettings.MaxStuckCount)
                    {
                        // We're stuck - attempt recovery maneuvers
                        _recoveryAttempts++;
                        
                        // Log stuck status
                        Debug.LogWarning($"<color=orange>[MoveTo]</color> {brain.name} STUCK at {transform.position} trying to reach {target}. " +
                            $"Path has {(_currentPath?.Count ?? 0)} waypoints, at index {_currentWaypointIndex}. " +
                            $"Recovery attempt {_recoveryAttempts}. Grounded={controller.isGrounded}, CollisionFlags={controller.collisionFlags}");
                        
                        // Try different recovery strategies based on attempt number
                        PerformStuckRecovery(brain, controller, transform, target);
                        
                        _stuckCounter = 0;
                        
                        // After several failed recovery attempts, force path recalculation with offset target
                        if (_recoveryAttempts >= 5)
                        {
                            Debug.Log($"<color=yellow>[MoveTo]</color> {brain.name} forcing path recalculation after {_recoveryAttempts} recovery attempts");
                            _currentPath = null;
                            _recoveryAttempts = 0;
                        }
                    }
                }
                else
                {
                    _stuckCounter = 0; // Reset if we're making progress
                    _recoveryAttempts = 0; // Reset recovery attempts on successful movement
                    _recoveryDirection = Vector3.zero;
                }
                _lastStuckCheckPosition = transform.position;
                _lastStuckCheckTime = Time.time;
            }
            
            // If we're in recovery mode, apply recovery movement
            if (_recoveryDirection != Vector3.zero && Time.time - _lastRecoveryTime < 0.5f)
            {
                Vector3 recoveryMove = _recoveryDirection * _moveSpeed * 0.5f * Time.deltaTime;
                if (!controller.isGrounded)
                {
                    recoveryMove.y = -PathfindingSettings.Gravity * Time.deltaTime;
                }
                controller.Move(recoveryMove);
            }
            
            // Debug logging every 3 seconds - show where we're going
            if (Time.time - _lastDebugLogTime > 3f || Vector3.Distance(target, _lastLoggedTarget) > 5f)
            {
                _lastDebugLogTime = Time.time;
                _lastLoggedTarget = target;
                float distToTarget = Vector3.Distance(transform.position, target);
                string waypointInfo = _currentPath != null ? $"wp {_currentWaypointIndex}/{_currentPath.Count}" : "no path";
                Debug.Log($"<color=yellow>[MoveTo DEBUG]</color> {brain.name}: pos={transform.position}, target={target}, dist={distToTarget:F1}m, {waypointInfo}, grounded={controller.isGrounded}");
            }
            
            return NodeStatus.Running;
        }
        
        /// <summary>
        /// Performs stuck recovery maneuvers to get the NPC unstuck.
        /// Tries different strategies based on the recovery attempt number.
        /// </summary>
        private void PerformStuckRecovery(NPCBrainController brain, CharacterController controller, Transform transform, Vector3 target)
        {
            Vector3 currentPos = transform.position;
            Vector3 toTarget = (target - currentPos);
            toTarget.y = 0;
            toTarget.Normalize();
            
            // Get the collision flags to understand where we're blocked
            CollisionFlags flags = controller.collisionFlags;
            bool blockedOnSides = (flags & CollisionFlags.Sides) != 0;
            
            // Choose recovery strategy based on attempt number
            int strategy = _recoveryAttempts % 6;
            
            switch (strategy)
            {
                case 0:
                    // Try sliding left (perpendicular to target direction)
                    _recoveryDirection = Vector3.Cross(Vector3.up, toTarget).normalized;
                    Debug.Log($"<color=cyan>[MoveTo]</color> {brain.name} recovery: sliding LEFT");
                    break;
                    
                case 1:
                    // Try sliding right
                    _recoveryDirection = -Vector3.Cross(Vector3.up, toTarget).normalized;
                    Debug.Log($"<color=cyan>[MoveTo]</color> {brain.name} recovery: sliding RIGHT");
                    break;
                    
                case 2:
                    // Try stepping backward
                    _recoveryDirection = -toTarget;
                    Debug.Log($"<color=cyan>[MoveTo]</color> {brain.name} recovery: stepping BACK");
                    break;
                    
                case 3:
                    // Try diagonal left-back
                    _recoveryDirection = (-toTarget + Vector3.Cross(Vector3.up, toTarget)).normalized;
                    Debug.Log($"<color=cyan>[MoveTo]</color> {brain.name} recovery: diagonal LEFT-BACK");
                    break;
                    
                case 4:
                    // Try diagonal right-back
                    _recoveryDirection = (-toTarget - Vector3.Cross(Vector3.up, toTarget)).normalized;
                    Debug.Log($"<color=cyan>[MoveTo]</color> {brain.name} recovery: diagonal RIGHT-BACK");
                    break;
                    
                case 5:
                    // Skip to next waypoint if possible
                    if (_currentPath != null && _currentWaypointIndex < _currentPath.Count - 1)
                    {
                        _currentWaypointIndex++;
                        Debug.Log($"<color=cyan>[MoveTo]</color> {brain.name} recovery: skipping to waypoint {_currentWaypointIndex}/{_currentPath.Count}");
                        _recoveryDirection = Vector3.zero;
                    }
                    else
                    {
                        // Random direction as last resort
                        float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
                        _recoveryDirection = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                        Debug.Log($"<color=cyan>[MoveTo]</color> {brain.name} recovery: random direction");
                    }
                    break;
            }
            
            _lastRecoveryTime = Time.time;
            
            // Apply an immediate recovery movement
            if (_recoveryDirection != Vector3.zero)
            {
                Vector3 immediateMove = _recoveryDirection * 0.3f; // Small immediate push
                if (!controller.isGrounded)
                {
                    immediateMove.y = -0.1f;
                }
                controller.Move(immediateMove);
            }
        }
        
        /// <summary>
        /// Moves the NPC using CharacterController.Move() which respects collisions with walls and obstacles.
        /// </summary>
        private NodeStatus MoveViaCharacterController(CharacterController controller, Transform transform, Vector3 target)
        {
            Vector3 currentPos = transform.position;
            Vector3 toTarget = target - currentPos;
            toTarget.y = 0f; // Keep movement horizontal
            float distance = toTarget.magnitude;

            if (distance < 0.01f)
            {
                return NodeStatus.Running;
            }

            Vector3 direction = toTarget / distance;
            Vector3 movement = direction * _moveSpeed * Time.deltaTime;
            
            // Add gravity to keep grounded
            if (!controller.isGrounded)
            {
                movement.y = -PathfindingSettings.Gravity * Time.deltaTime;
            }

            controller.Move(movement);

            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }

            return NodeStatus.Running;
        }
        
        /// <summary>
        /// Moves the given transform a single step toward the target position and orients it to face the movement direction.
        /// WARNING: This method does NOT respect collisions - NPCs will walk through walls!
        /// Use CharacterController or NavMeshAgent for proper collision handling.
        /// </summary>
        /// <param name="transform">The transform to move and rotate.</param>
        /// <param name="target">The destination position to approach.</param>
        /// <param name="debugName">Optional label used for debugging or tracing.</param>
        /// <returns>`NodeStatus.Running` while the node drives the transform toward the target.</returns>
        private NodeStatus MoveDirectly(Transform transform, Vector3 target, string debugName = "")
        {
            // Performance: Cache position to reduce property access overhead
            Vector3 currentPos = transform.position;
            Vector3 toTarget = target - currentPos;
            float distance = toTarget.magnitude;

            // Avoid division by zero and normalize efficiently
            Vector3 direction = distance > 0.0001f ? toTarget / distance : Vector3.zero;
            Vector3 movement = direction * _moveSpeed * Time.deltaTime;

            transform.position = currentPos + movement;

            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }

            return NodeStatus.Running;
        }
    }
}