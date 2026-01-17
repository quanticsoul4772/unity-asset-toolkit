using System.Collections.Generic;
using UnityEngine;

namespace NPCBrain
{
    /// <summary>
    /// Component that defines a path of waypoints for NPCs to follow.
    /// </summary>
    /// <remarks>
    /// <para>Create waypoints as child GameObjects or assign external transforms.</para>
    /// <para>Use with PatrolNPC or GuardNPC archetypes, or in custom behavior trees
    /// with the MoveTo and AdvanceWaypoint actions.</para>
    /// </remarks>
    public class WaypointPath : MonoBehaviour
    {
        [Tooltip("List of waypoint transforms. Leave empty to auto-populate from children.")]
        [SerializeField] private List<Transform> _waypoints = new List<Transform>();
        
        [Tooltip("Should the path loop back to the start?")]
        [SerializeField] private bool _loop = true;
        
        [Tooltip("Should waypoints be collected from child objects on Awake?")]
        [SerializeField] private bool _autoPopulateFromChildren = true;
        
        private int _currentIndex = 0;
        
        /// <summary>Current waypoint index.</summary>
        public int CurrentIndex => _currentIndex;
        
        /// <summary>Total number of waypoints.</summary>
        public int Count => _waypoints.Count;
        
        /// <summary>Whether the path loops.</summary>
        public bool IsLooping => _loop;
        
        /// <summary>Read-only access to waypoints (for gizmos and debugging).</summary>
        public IReadOnlyList<Transform> Waypoints => _waypoints;
        
        private void Awake()
        {
            if (_autoPopulateFromChildren && _waypoints.Count == 0)
            {
                PopulateFromChildren();
            }
        }
        
        /// <summary>
        /// Populates the waypoint list from child transforms.
        /// </summary>
        public void PopulateFromChildren()
        {
            _waypoints.Clear();
            foreach (Transform child in transform)
            {
                _waypoints.Add(child);
            }
        }
        
        /// <summary>
        /// Sets the waypoint list.
        /// </summary>
        /// <param name="waypoints">List of waypoint transforms.</param>
        public void SetWaypoints(List<Transform> waypoints)
        {
            _waypoints = waypoints ?? new List<Transform>();
            _currentIndex = 0;
        }
        
        /// <summary>
        /// Adds a waypoint to the path.
        /// </summary>
        /// <param name="waypoint">Transform to add.</param>
        public void AddWaypoint(Transform waypoint)
        {
            if (waypoint != null)
            {
                _waypoints.Add(waypoint);
            }
        }
        
        /// <summary>
        /// Removes a waypoint from the path.
        /// </summary>
        /// <param name="waypoint">Transform to remove.</param>
        /// <returns>True if the waypoint was found and removed.</returns>
        public bool RemoveWaypoint(Transform waypoint)
        {
            int index = _waypoints.IndexOf(waypoint);
            if (index >= 0)
            {
                _waypoints.RemoveAt(index);
                if (_currentIndex >= _waypoints.Count)
                {
                    _currentIndex = _waypoints.Count > 0 ? 0 : 0;
                }
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Gets the position of the current waypoint.
        /// </summary>
        /// <returns>Current waypoint position, or this transform's position if no waypoints.</returns>
        public Vector3 GetCurrent()
        {
            if (_waypoints.Count == 0 || _waypoints[_currentIndex] == null)
            {
                return transform.position;
            }
            return _waypoints[_currentIndex].position;
        }
        
        /// <summary>
        /// Gets a waypoint by index.
        /// </summary>
        /// <param name="index">Waypoint index.</param>
        /// <returns>Waypoint position, or this transform's position if invalid index.</returns>
        public Vector3 GetWaypoint(int index)
        {
            if (_waypoints.Count == 0 || index < 0 || index >= _waypoints.Count)
            {
                return transform.position;
            }
            return _waypoints[index]?.position ?? transform.position;
        }
        
        /// <summary>
        /// Advances to the next waypoint.
        /// </summary>
        public void Advance()
        {
            if (_waypoints.Count == 0) return;
            
            _currentIndex++;
            if (_currentIndex >= _waypoints.Count)
            {
                _currentIndex = _loop ? 0 : _waypoints.Count - 1;
            }
        }
        
        /// <summary>
        /// Advances to the next waypoint and returns its position.
        /// </summary>
        /// <returns>Position of the new current waypoint.</returns>
        public Vector3 AdvanceAndGetWaypoint()
        {
            Advance();
            return GetCurrent();
        }
        
        /// <summary>
        /// Moves to the previous waypoint.
        /// </summary>
        public void Retreat()
        {
            if (_waypoints.Count == 0) return;
            
            _currentIndex--;
            if (_currentIndex < 0)
            {
                _currentIndex = _loop ? _waypoints.Count - 1 : 0;
            }
        }
        
        /// <summary>
        /// Resets to the first waypoint.
        /// </summary>
        public void Reset()
        {
            _currentIndex = 0;
        }
        
        /// <summary>
        /// Sets the current waypoint index.
        /// </summary>
        /// <param name="index">Index to set.</param>
        public void SetCurrentIndex(int index)
        {
            if (_waypoints.Count == 0) return;
            _currentIndex = Mathf.Clamp(index, 0, _waypoints.Count - 1);
        }
        
        /// <summary>
        /// Finds the nearest waypoint to a position.
        /// </summary>
        /// <param name="position">Position to check from.</param>
        /// <returns>Index of the nearest waypoint, or -1 if no waypoints.</returns>
        public int GetNearestWaypointIndex(Vector3 position)
        {
            if (_waypoints.Count == 0) return -1;
            
            int nearest = 0;
            float nearestDistance = float.MaxValue;
            
            for (int i = 0; i < _waypoints.Count; i++)
            {
                if (_waypoints[i] == null) continue;
                
                float distance = Vector3.Distance(position, _waypoints[i].position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = i;
                }
            }
            
            return nearest;
        }
        
        /// <summary>
        /// Gets the total path length.
        /// </summary>
        /// <returns>Sum of distances between all waypoints.</returns>
        public float GetTotalPathLength()
        {
            if (_waypoints.Count < 2) return 0f;
            
            float length = 0f;
            for (int i = 0; i < _waypoints.Count - 1; i++)
            {
                if (_waypoints[i] != null && _waypoints[i + 1] != null)
                {
                    length += Vector3.Distance(_waypoints[i].position, _waypoints[i + 1].position);
                }
            }
            
            if (_loop && _waypoints[0] != null && _waypoints[_waypoints.Count - 1] != null)
            {
                length += Vector3.Distance(_waypoints[_waypoints.Count - 1].position, _waypoints[0].position);
            }
            
            return length;
        }
    }
}
