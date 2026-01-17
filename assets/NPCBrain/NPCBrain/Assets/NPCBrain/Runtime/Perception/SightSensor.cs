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
        
        public float ViewDistance => _viewDistance;
        public float ViewAngle => _viewAngle;
        
        private readonly List<GameObject> _visibleTargets = new List<GameObject>();
        
        public List<GameObject> GetVisibleTargets()
        {
            return _visibleTargets;
        }
        
        public void Tick(NPCBrainController brain)
        {
            // Week 2: Implement vision cone detection
        }
    }
}
