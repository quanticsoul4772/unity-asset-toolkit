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
        [SerializeField] private bool _logExceptions = true;
        
        public Blackboard Blackboard { get; private set; }
        public SightSensor Perception { get; private set; }
        public CriticalityController Criticality { get; private set; }
        public WaypointPath WaypointPath => _waypointPath;
        public BTNode BehaviorTree => _behaviorTree;
        
        private BTNode _behaviorTree;
        private NodeStatus _lastStatus;
        private float _lastTickTime;
        private bool _isPaused;
        
        public NodeStatus LastStatus => _lastStatus;
        public bool IsPaused => _isPaused;
        
        public event Action<GameObject> OnTargetAcquired;
        public event Action<GameObject> OnTargetLost;
        public event Action<string> OnStateChanged;
        public event Action OnBrainPaused;
        public event Action OnBrainResumed;
        
        protected virtual void Awake()
        {
            Blackboard = new Blackboard();
            Perception = GetComponent<SightSensor>();
            Criticality = new CriticalityController();
            _behaviorTree = CreateBehaviorTree();
        }
        
        protected virtual void OnDestroy()
        {
            OnTargetAcquired = null;
            OnTargetLost = null;
            OnStateChanged = null;
            OnBrainPaused = null;
            OnBrainResumed = null;
            
            if (Blackboard != null)
            {
                Blackboard.OnValueChanged = null;
                Blackboard.OnValueExpired = null;
            }
        }
        
        protected virtual BTNode CreateBehaviorTree()
        {
            return null;
        }
        
        private void Update()
        {
            if (_isPaused)
            {
                return;
            }
            
            if (_tickInterval > 0f && Time.time - _lastTickTime < _tickInterval)
            {
                return;
            }
            _lastTickTime = Time.time;
            
            Tick();
        }
        
        public void Tick()
        {
            if (_isPaused)
            {
                return;
            }
            
            Blackboard?.CleanupExpired();
            Perception?.Tick(this);
            Criticality?.Update();
            
            if (_behaviorTree != null)
            {
                try
                {
                    _lastStatus = _behaviorTree.Execute(this);
                }
                catch (Exception ex)
                {
                    _lastStatus = NodeStatus.Failure;
                    if (_logExceptions)
                    {
                        Debug.LogException(ex, this);
                    }
                }
            }
        }
        
        public void Pause()
        {
            if (_isPaused) return;
            
            _isPaused = true;
            _behaviorTree?.Abort(this);
            OnBrainPaused?.Invoke();
        }
        
        public void Resume()
        {
            if (!_isPaused) return;
            
            _isPaused = false;
            _behaviorTree?.Reset();
            OnBrainResumed?.Invoke();
        }
        
        public void SetBehaviorTree(BTNode tree)
        {
            _behaviorTree?.Abort(this);
            _behaviorTree = tree;
        }
        
        public Vector3 AdvanceAndGetWaypoint()
        {
            return _waypointPath != null ? _waypointPath.AdvanceAndGetWaypoint() : transform.position;
        }
        
        public Vector3 GetCurrentWaypoint()
        {
            return _waypointPath != null ? _waypointPath.GetCurrent() : transform.position;
        }
        
        public void SetWaypointPath(WaypointPath path)
        {
            _waypointPath = path;
        }
        
        internal void RaiseTargetAcquired(GameObject target)
        {
            OnTargetAcquired?.Invoke(target);
        }
        
        internal void RaiseTargetLost(GameObject target)
        {
            OnTargetLost?.Invoke(target);
        }
        
        internal void RaiseStateChanged(string state)
        {
            OnStateChanged?.Invoke(state);
        }
    }
}