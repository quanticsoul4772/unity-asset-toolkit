using System;
using UnityEngine;
using NPCBrain.BehaviorTree;
using NPCBrain.Perception;
using NPCBrain.Criticality;

namespace NPCBrain
{
    /// <summary>
    /// Main component for NPC AI control. Manages behavior tree execution,
    /// blackboard state, perception, and criticality systems.
    /// </summary>
    /// <remarks>
    /// <para>Attach this component (or a derived class) to any NPC GameObject.</para>
    /// <para>Override <see cref="CreateBehaviorTree"/> to define the NPC's behavior.</para>
    /// <example>
    /// <code>
    /// public class PatrolNPC : NPCBrainController
    /// {
    ///     protected override BTNode CreateBehaviorTree()
    ///     {
    ///         return new Sequence(
    ///             new MoveTo(() => GetCurrentWaypoint()),
    ///             new Wait(1f),
    ///             new AdvanceWaypoint()
    ///         );
    ///     }
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    public class NPCBrainController : MonoBehaviour
    {
        [SerializeField] private float _tickInterval = 0f;
        [SerializeField] private WaypointPath _waypointPath;
        [SerializeField] private bool _logExceptions = true;
        
        /// <summary>
        /// Shared data storage for the behavior tree. Store and retrieve values by key.
        /// </summary>
        public Blackboard Blackboard { get; protected set; }
        
        /// <summary>
        /// Sight sensor for detecting targets. May be null if not attached.
        /// </summary>
        public SightSensor Perception { get; private set; }
        
        /// <summary>
        /// Controls exploration vs exploitation through temperature-based selection.
        /// </summary>
        public CriticalityController Criticality { get; protected set; }
        
        /// <summary>
        /// The waypoint path for patrol behaviors.
        /// </summary>
        public WaypointPath WaypointPath => _waypointPath;
        
        /// <summary>
        /// The root node of the behavior tree.
        /// </summary>
        public BTNode BehaviorTree => _behaviorTree;
        
        private BTNode _behaviorTree;
        private NodeStatus _lastStatus;
        private float _lastTickTime;
        private bool _isPaused;
        
        /// <summary>
        /// The result of the last behavior tree tick.
        /// </summary>
        public NodeStatus LastStatus => _lastStatus;
        
        /// <summary>
        /// True if the brain is paused and not ticking.
        /// </summary>
        public bool IsPaused => _isPaused;
        
        /// <summary>Raised when the perception system detects a new target.</summary>
        public event Action<GameObject> OnTargetAcquired;
        
        /// <summary>Raised when a previously detected target is no longer visible.</summary>
        public event Action<GameObject> OnTargetLost;
        
        /// <summary>Raised when the NPC's state changes.</summary>
        public event Action<string> OnStateChanged;
        
        /// <summary>Raised when the brain is paused via <see cref="Pause"/>.</summary>
        public event Action OnBrainPaused;
        
        /// <summary>Raised when the brain is resumed via <see cref="Resume"/>.</summary>
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
            
            Blackboard?.ClearEvents();
        }
        
        /// <summary>
        /// Override this method to create the NPC's behavior tree.
        /// </summary>
        /// <returns>The root node of the behavior tree, or null for no behavior.</returns>
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
        
        /// <summary>
        /// Manually triggers one tick of the brain. Called automatically by Update().
        /// </summary>
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
        
        /// <summary>
        /// Pauses the brain, stopping all behavior tree execution.
        /// </summary>
        public void Pause()
        {
            if (_isPaused) return;
            
            _isPaused = true;
            _behaviorTree?.Abort(this);
            OnBrainPaused?.Invoke();
        }
        
        /// <summary>
        /// Resumes the brain after being paused.
        /// </summary>
        public void Resume()
        {
            if (!_isPaused) return;
            
            _isPaused = false;
            _behaviorTree?.Reset();
            OnBrainResumed?.Invoke();
        }
        
        /// <summary>
        /// Replaces the current behavior tree with a new one.
        /// </summary>
        /// <param name="tree">The new behavior tree root node.</param>
        public void SetBehaviorTree(BTNode tree)
        {
            _behaviorTree?.Abort(this);
            _behaviorTree = tree;
        }
        
        /// <summary>
        /// Advances to the next waypoint and returns its position.
        /// </summary>
        /// <returns>The position of the new current waypoint, or this object's position if no path.</returns>
        public Vector3 AdvanceAndGetWaypoint()
        {
            return _waypointPath != null ? _waypointPath.AdvanceAndGetWaypoint() : transform.position;
        }
        
        /// <summary>
        /// Gets the current waypoint position without advancing.
        /// </summary>
        /// <returns>The current waypoint position, or this object's position if no path.</returns>
        public Vector3 GetCurrentWaypoint()
        {
            return _waypointPath != null ? _waypointPath.GetCurrent() : transform.position;
        }
        
        /// <summary>
        /// Sets the waypoint path for patrol behaviors.
        /// </summary>
        /// <param name="path">The waypoint path to follow.</param>
        public void SetWaypointPath(WaypointPath path)
        {
            _waypointPath = path;
        }
        
        internal void RaiseTargetAcquired(GameObject target)
        {
            Debug.Log($"[NPCBrain] {name} RaiseTargetAcquired: {target?.name} (subscribers: {OnTargetAcquired?.GetInvocationList()?.Length ?? 0})");
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