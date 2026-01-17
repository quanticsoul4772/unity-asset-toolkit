using System.Collections.Generic;
using UnityEngine;

namespace NPCBrain.Perception
{
    public class SightSensor : MonoBehaviour
    {
        [SerializeField] private float _viewDistance = 20f;
        [SerializeField] private float _viewAngle = 120f;
        [SerializeField] private LayerMask _targetLayers = -1;
        [SerializeField] private LayerMask _obstacleLayers = -1;
        [SerializeField] private int _maxTargets = 10;
        [SerializeField] private float _eyeHeight = 1.5f;
        
        public float ViewDistance => _viewDistance;
        public float ViewAngle => _viewAngle;
        
        private readonly List<GameObject> _visibleTargets = new List<GameObject>();
        private readonly List<GameObject> _previousTargets = new List<GameObject>();
        private Collider[] _overlapResults;
        
        private void Awake()
        {
            _overlapResults = new Collider[_maxTargets];
        }
        
        public IReadOnlyList<GameObject> GetVisibleTargets()
        {
            return _visibleTargets;
        }
        
        public void Tick(NPCBrainController brain)
        {
            if (_overlapResults == null)
            {
                _overlapResults = new Collider[_maxTargets];
            }
            
            _previousTargets.Clear();
            _previousTargets.AddRange(_visibleTargets);
            _visibleTargets.Clear();
            
            int count = Physics.OverlapSphereNonAlloc(
                transform.position,
                _viewDistance,
                _overlapResults,
                _targetLayers
            );
            
            for (int i = 0; i < count; i++)
            {
                Collider col = _overlapResults[i];
                if (col == null || col.gameObject == gameObject)
                {
                    continue;
                }
                
                Vector3 directionToTarget = col.transform.position - transform.position;
                float distanceToTarget = directionToTarget.magnitude;
                
                if (distanceToTarget > _viewDistance)
                {
                    continue;
                }
                
                directionToTarget.Normalize();
                float angle = Vector3.Angle(transform.forward, directionToTarget);
                
                if (angle > _viewAngle * 0.5f)
                {
                    continue;
                }
                
                if (!IsLineOfSightClear(transform.position, col.transform.position, distanceToTarget))
                {
                    continue;
                }
                
                _visibleTargets.Add(col.gameObject);
            }
            
            if (brain != null)
            {
                NotifyTargetChanges(brain);
            }
        }
        
        private bool IsLineOfSightClear(Vector3 origin, Vector3 target, float distance)
        {
            Vector3 direction = (target - origin).normalized;
            Vector3 eyePosition = origin + Vector3.up * _eyeHeight;
            
            if (Physics.Raycast(eyePosition, direction, distance, _obstacleLayers))
            {
                return false;
            }
            
            return true;
        }
        
        private void NotifyTargetChanges(NPCBrainController brain)
        {
            foreach (var target in _visibleTargets)
            {
                if (!_previousTargets.Contains(target))
                {
                    brain.RaiseTargetAcquired(target);
                }
            }
            
            foreach (var target in _previousTargets)
            {
                if (!_visibleTargets.Contains(target))
                {
                    brain.RaiseTargetLost(target);
                }
            }
        }
        
        public GameObject GetClosestTarget()
        {
            if (_visibleTargets.Count == 0)
            {
                return null;
            }
            
            GameObject closest = null;
            float closestDistance = float.MaxValue;
            
            foreach (var target in _visibleTargets)
            {
                float distance = Vector3.Distance(transform.position, target.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = target;
                }
            }
            
            return closest;
        }
        
        public bool CanSee(GameObject target)
        {
            return _visibleTargets.Contains(target);
        }
    }
}
