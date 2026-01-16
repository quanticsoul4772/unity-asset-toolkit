using System.Collections.Generic;
using UnityEngine;

namespace EasyPath
{
    /// <summary>
    /// Agent component that follows paths calculated by EasyPathGrid.
    /// </summary>
    public class EasyPathAgent : MonoBehaviour, IPathfindable
    {
        [Header("Movement")]
        [SerializeField] private float _speed = 5f;
        [SerializeField] private float _rotationSpeed = 360f;
        [SerializeField] private float _stoppingDistance = 0.1f;
        [SerializeField] private float _waypointTolerance = 0.3f;
        
        [Header("Debug")]
        [SerializeField] private bool _showDebugPath = true;
        [SerializeField] private Color _pathColor = Color.cyan;
        
        private EasyPathGrid _grid;
        private List<Vector3> _currentPath;
        private int _currentWaypointIndex;
        private bool _isMoving;
        
        public float Speed { get => _speed; set => _speed = value; }
        public float StoppingDistance { get => _stoppingDistance; set => _stoppingDistance = value; }
        public bool IsMoving => _isMoving;
        public bool HasPath => _currentPath != null && _currentPath.Count > 0;
        public float RemainingDistance => CalculateRemainingDistance();
        public Vector3 Destination => HasPath ? _currentPath[_currentPath.Count - 1] : transform.position;
        
        public event System.Action OnPathComplete;
        public event System.Action OnPathFailed;
        
        private void Awake()
        {
            _grid = FindFirstObjectByType<EasyPathGrid>();
            if (_grid == null)
            {
                Debug.LogWarning($"[EasyPathAgent] No EasyPathGrid found in scene. Agent {name} will not pathfind.");
            }
        }
        
        private void Update()
        {
            if (_isMoving && HasPath)
            {
                FollowPath();
            }
        }
        
        /// <summary>
        /// Set the destination and start moving.
        /// </summary>
        public bool SetDestination(Vector3 destination)
        {
            if (_grid == null)
            {
                Debug.LogError($"[EasyPathAgent] No grid assigned to agent {name}");
                OnPathFailed?.Invoke();
                return false;
            }
            
            List<Vector3> path = _grid.FindPath(transform.position, destination);
            
            if (path == null || path.Count == 0)
            {
                Debug.LogWarning($"[EasyPathAgent] Could not find path to {destination}");
                OnPathFailed?.Invoke();
                return false;
            }
            
            _currentPath = path;
            _currentWaypointIndex = 0;
            _isMoving = true;
            
            return true;
        }
        
        /// <summary>
        /// Stop moving and clear the current path.
        /// </summary>
        public void Stop()
        {
            _isMoving = false;
            _currentPath = null;
            _currentWaypointIndex = 0;
        }
        
        /// <summary>
        /// Pause movement without clearing the path.
        /// </summary>
        public void Pause()
        {
            _isMoving = false;
        }
        
        /// <summary>
        /// Resume movement on the current path.
        /// </summary>
        public void Resume()
        {
            if (HasPath)
            {
                _isMoving = true;
            }
        }
        
        /// <summary>
        /// Set the grid to use for pathfinding.
        /// </summary>
        public void SetGrid(EasyPathGrid grid)
        {
            _grid = grid;
        }
        
        /// <summary>
        /// Recalculate the path to the current destination.
        /// </summary>
        public void RecalculatePath()
        {
            if (HasPath)
            {
                SetDestination(Destination);
            }
        }
        
        private void FollowPath()
        {
            if (_currentWaypointIndex >= _currentPath.Count)
            {
                OnReachedDestination();
                return;
            }
            
            Vector3 targetWaypoint = _currentPath[_currentWaypointIndex];
            targetWaypoint.y = transform.position.y; // Keep same Y level
            
            Vector3 direction = (targetWaypoint - transform.position);
            float distance = direction.magnitude;
            
            // Check if we've reached the final waypoint
            if (_currentWaypointIndex == _currentPath.Count - 1 && distance <= _stoppingDistance)
            {
                OnReachedDestination();
                return;
            }
            
            // Check if we've reached current waypoint
            if (distance <= _waypointTolerance)
            {
                _currentWaypointIndex++;
                return;
            }
            
            // Move toward waypoint
            direction.Normalize();
            
            // Rotation
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    _rotationSpeed * Time.deltaTime
                );
            }
            
            // Movement
            float moveDistance = _speed * Time.deltaTime;
            if (moveDistance > distance)
            {
                moveDistance = distance;
            }
            
            transform.position += direction * moveDistance;
        }
        
        private void OnReachedDestination()
        {
            _isMoving = false;
            OnPathComplete?.Invoke();
        }
        
        private float CalculateRemainingDistance()
        {
            if (!HasPath || _currentWaypointIndex >= _currentPath.Count)
            {
                return 0f;
            }
            
            float distance = Vector3.Distance(transform.position, _currentPath[_currentWaypointIndex]);
            
            for (int i = _currentWaypointIndex; i < _currentPath.Count - 1; i++)
            {
                distance += Vector3.Distance(_currentPath[i], _currentPath[i + 1]);
            }
            
            return distance;
        }
        
        private void OnDrawGizmos()
        {
            if (!_showDebugPath || !HasPath)
            {
                return;
            }
            
            Gizmos.color = _pathColor;
            
            // Draw path
            for (int i = 0; i < _currentPath.Count - 1; i++)
            {
                Gizmos.DrawLine(_currentPath[i], _currentPath[i + 1]);
            }
            
            // Draw waypoints
            foreach (Vector3 waypoint in _currentPath)
            {
                Gizmos.DrawSphere(waypoint, 0.15f);
            }
            
            // Draw current target
            if (_currentWaypointIndex < _currentPath.Count)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(_currentPath[_currentWaypointIndex], 0.3f);
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            // Draw stopping distance
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _stoppingDistance);
        }
    }
}
