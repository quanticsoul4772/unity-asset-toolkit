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
            return _waypoints[_currentIndex].position;
        }
        
        public Vector3 GetNext()
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
        
        public void Reset()
        {
            _currentIndex = 0;
        }
        
        public void SetWaypoints(List<Transform> waypoints)
        {
            _waypoints = waypoints;
            _currentIndex = 0;
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
