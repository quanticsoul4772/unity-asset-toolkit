using System;
using UnityEngine;
using NPCBrain.BehaviorTree;
using NPCBrain.Perception;
using NPCBrain.Criticality;

namespace NPCBrain
{
    public class NPCBrainController : MonoBehaviour
    {
        [SerializeField] private float _tickInterval = 0f;
        [SerializeField] private WaypointPath _waypointPath;
        
        public Blackboard Blackboard { get; private set; }
        public SightSensor Perception { get; private set; }
        public CriticalityController Criticality { get; private set; }
        public WaypointPath WaypointPath => _waypointPath;
        
        private BTNode _behaviorTree;
        private NodeStatus _lastStatus;
        private float _lastTickTime;
        
        public NodeStatus LastStatus => _lastStatus;
        
        public event Action<GameObject> OnTargetAcquired;
        public event Action<GameObject> OnTargetLost;
        public event Action<string> OnStateChanged;
        
        protected virtual void Awake()
        {
            Blackboard = new Blackboard();
            Perception = GetComponent<SightSensor>();
            Criticality = new CriticalityController();
            _behaviorTree = CreateBehaviorTree();
        }
        
        protected virtual BTNode CreateBehaviorTree()
        {
            return null;
        }
        
        private void Update()
        {
            if (_tickInterval > 0f && Time.time - _lastTickTime < _tickInterval)
            {
                return;
            }
            _lastTickTime = Time.time;
            
            Tick();
        }
        
        public void Tick()
        {
            Perception?.Tick(this);
            Criticality.Update();
            
            if (_behaviorTree != null)
            {
                _lastStatus = _behaviorTree.Tick(this);
            }
        }
        
        public void SetBehaviorTree(BTNode tree)
        {
            _behaviorTree = tree;
        }
        
        public Vector3 GetNextWaypoint()
        {
            return _waypointPath != null ? _waypointPath.GetNext() : transform.position;
        }
        
        public Vector3 GetCurrentWaypoint()
        {
            return _waypointPath != null ? _waypointPath.GetCurrent() : transform.position;
        }
        
        public void RaiseTargetAcquired(GameObject target)
        {
            OnTargetAcquired?.Invoke(target);
        }
        
        public void RaiseTargetLost(GameObject target)
        {
            OnTargetLost?.Invoke(target);
        }
        
        public void RaiseStateChanged(string state)
        {
            OnStateChanged?.Invoke(state);
        }
    }
}