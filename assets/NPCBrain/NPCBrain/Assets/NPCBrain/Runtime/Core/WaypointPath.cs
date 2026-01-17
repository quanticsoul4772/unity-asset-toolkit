using System.Collections.Generic;
using UnityEngine;

namespace NPCBrain
{
    public class WaypointPath : MonoBehaviour
    {
        [SerializeField] private List<Transform> _waypoints = new List<Transform>();
        private int _currentIndex = 0;
        
        public int WaypointCount => _waypoints.Count;
        public int CurrentIndex => _currentIndex;
        
        public Vector3 GetCurrent()
        {
            if (_waypoints.Count == 0)
            {
                return transform.position;
            }
            
            Transform waypoint = _waypoints[_currentIndex];
            if (waypoint == null)
            {
                return FindNextValidWaypoint();
            }
            
            return waypoint.position;
        }
        
        public Vector3 AdvanceAndGetWaypoint()
        {
            Advance();
            return GetCurrent();
        }
        
        public void Advance()
        {
            if (_waypoints.Count == 0)
            {
                return;
            }
            _currentIndex = (_currentIndex + 1) % _waypoints.Count;
        }
        
        public void ResetToStart()
        {
            _currentIndex = 0;
        }
        
        public void SetWaypoints(List<Transform> waypoints)
        {
            _waypoints = waypoints ?? new List<Transform>();
            _currentIndex = 0;
        }
        
        public bool TryGetWaypoint(int index, out Vector3 position)
        {
            position = Vector3.zero;
            
            if (index < 0 || index >= _waypoints.Count)
            {
                return false;
            }
            
            Transform waypoint = _waypoints[index];
            if (waypoint == null)
            {
                return false;
            }
            
            position = waypoint.position;
            return true;
        }
        
        private Vector3 FindNextValidWaypoint()
        {
            if (_waypoints.Count == 0)
            {
                return transform.position;
            }
            
            int startIndex = _currentIndex;
            do
            {
                _currentIndex = (_currentIndex + 1) % _waypoints.Count;
                if (_waypoints[_currentIndex] != null)
                {
                    return _waypoints[_currentIndex].position;
                }
            }
            while (_currentIndex != startIndex);
            
            return transform.position;
        }
        
        private void OnDrawGizmos()
        {
            if (_waypoints == null || _waypoints.Count < 2)
            {
                return;
            }
            
            Gizmos.color = Color.cyan;
            for (int i = 0; i < _waypoints.Count; i++)
            {
                if (_waypoints[i] == null) continue;
                
                int nextIndex = (i + 1) % _waypoints.Count;
                if (_waypoints[nextIndex] == null) continue;
                
                Gizmos.DrawLine(_waypoints[i].position, _waypoints[nextIndex].position);
                Gizmos.DrawWireSphere(_waypoints[i].position, 0.3f);
            }
        }
    }
}
