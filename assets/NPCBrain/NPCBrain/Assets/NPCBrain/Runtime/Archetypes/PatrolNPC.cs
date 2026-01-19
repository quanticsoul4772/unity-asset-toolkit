using System;
using UnityEngine;
using NPCBrain.BehaviorTree;
using NPCBrain.BehaviorTree.Composites;
using NPCBrain.BehaviorTree.Actions;
using NPCBrain.UtilityAI;

namespace NPCBrain.Archetypes
{
    /// <summary>
    /// Patrol NPC archetype that uses Utility AI for varied, natural movement.
    /// </summary>
    /// <remarks>
    /// <para>Utility-scored behaviors:</para>
    /// <list type="bullet">
    ///   <item><description>Patrol - Follow waypoints (main behavior)</description></item>
    ///   <item><description>Rest - Stop and recover energy when tired</description></item>
    ///   <item><description>Wander - Random exploration near patrol route</description></item>
    /// </list>
    /// <para>The Criticality system adjusts temperature based on action variety:</para>
    /// <list type="bullet">
    ///   <item><description>Repetitive behavior → Higher temperature → More exploration</description></item>
    ///   <item><description>Varied behavior → Lower temperature → More exploitation</description></item>
    /// </list>
    /// </remarks>
    public class PatrolNPC : NPCBrainController, IEnergyNPC
    {
        [Header("Patrol Settings")]
        [Tooltip("Movement speed while patrolling")]
        [SerializeField] private float _patrolSpeed = 3f;
        
        [Tooltip("How close to a waypoint before considered 'arrived'")]
        [SerializeField] private float _arrivalDistance = 0.5f;
        
        [Tooltip("Time to wait at each waypoint")]
        [SerializeField] private float _waypointWaitTime = 2f;
        
        [Header("Wander Settings")]
        [Tooltip("How far from current position to wander")]
        [SerializeField] private float _wanderRadius = 5f;
        
        [Tooltip("Speed while wandering")]
        [SerializeField] private float _wanderSpeed = 2f;
        
        [Header("Rest Settings")]
        [Tooltip("How long to rest when tired")]
        [SerializeField] private float _restDuration = 2f;
        
        [Header("Utility Weights")]
        [SerializeField] private float _patrolWeight = 0.7f;
        [SerializeField] private float _restWeight = 0.5f;
        [SerializeField] private float _wanderWeight = 0.4f;
        
        private float _energy = 1f;
        private float _lastBlackboardEnergy = 1f;
        private Vector3 _currentWanderTarget;
        private Vector3 _homePosition;
        private float _arrivalDistanceSqr2x;
        private string _cachedState = "Patrol";
        
        private const float EnergyDecayRate = 0.03f;
        private const float EnergyRecoveryRate = 0.3f;
        
        /// <summary>Raised when the energy level changes.</summary>
        public event Action<float> OnEnergyChanged;
        
        /// <summary>Current behavior state for UI display.</summary>
        public string CurrentState => _cachedState;
        
        /// <summary>Current energy level (0-1).</summary>
        public float Energy => _energy;
        
        /// <summary>Whether this NPC is still active.</summary>
        public bool IsActive => gameObject.activeSelf;
        
        /// <summary>Gets the current patrol speed.</summary>
        public float PatrolSpeed => _patrolSpeed;
        
        /// <summary>Gets the wait time at waypoints.</summary>
        public float WaitTime => _waypointWaitTime;
        
        protected override void Awake()
        {
            base.Awake();
            _homePosition = transform.position;
            _currentWanderTarget = GetRandomWanderPoint();
            _arrivalDistanceSqr2x = (_arrivalDistance * 2f) * (_arrivalDistance * 2f);
            
            Blackboard.Set(BBKeys.CurrentState, "Patrol");
            Blackboard.SetFloat(BBKeys.Energy, _energy);
            Blackboard.SetVector3(BBKeys.HomePosition, _homePosition);
            
            // Initialize action timestamps for TimeConsiderations
            Blackboard.SetFloat(BBKeys.LastPatrolTime, -10f);
            Blackboard.SetFloat(BBKeys.LastRestTime, -10f);
            Blackboard.SetFloat(BBKeys.LastWanderTime, -10f);
        }
        
        protected override void OnDestroy()
        {
            OnEnergyChanged = null;
            base.OnDestroy();
        }
        
        private void LateUpdate()
        {
            float previousEnergy = _energy;
            
            // Update energy based on current state (use cached state to avoid Blackboard lookup)
            if (_cachedState == "Rest")
            {
                // Recover energy while resting
                _energy = Mathf.Min(1f, _energy + EnergyRecoveryRate * Time.deltaTime);
            }
            else if (LastStatus == NodeStatus.Running)
            {
                // Deplete energy while active
                _energy = Mathf.Max(0f, _energy - EnergyDecayRate * Time.deltaTime);
            }
            
            // Only update Blackboard when energy changed significantly
            if (Mathf.Abs(_energy - _lastBlackboardEnergy) > 0.001f)
            {
                Blackboard.SetFloat(BBKeys.Energy, _energy);
                _lastBlackboardEnergy = _energy;
                OnEnergyChanged?.Invoke(_energy);
            }
        }
        
        /// <inheritdoc/>
        protected override BTNode CreateBehaviorTree()
        {
            var patrolAction = CreatePatrolAction();
            var restAction = CreateRestAction();
            var wanderAction = CreateWanderAction();
            
            // Use UtilitySelector - this activates the Criticality system!
            return new UtilitySelector(
                patrolAction,
                restAction,
                wanderAction
            );
        }
        
        private UtilityAction CreatePatrolAction()
        {
            var patrolBehavior = new Sequence(
                new SetBlackboard(BBKeys.LastPatrolTime, () => Time.time),
                new SetBlackboard(BBKeys.CurrentState, () => { _cachedState = "Patrol"; return "Patrol"; }),
                new MoveTo(
                    () => GetCurrentWaypoint(),
                    _arrivalDistance,
                    _patrolSpeed
                ),
                new Wait(_waypointWaitTime),
                new AdvanceWaypoint()
            );
            patrolBehavior.Name = "PatrolBehavior";
            
            return new UtilityAction(
                "Patrol",
                patrolBehavior,
                _patrolWeight,
                // More likely when energy is good (0.5-1.0 range)
                new BlackboardConsideration<float>("EnergyForPatrol", BBKeys.Energy,
                    e => 0.3f + Mathf.Clamp01(e) * 0.7f, 1f),
                // Cooldown between patrol waypoints
                new TimeConsideration("PatrolCooldown", BBKeys.LastPatrolTime, 3f)
            );
        }
        
        private UtilityAction CreateRestAction()
        {
            var restBehavior = new Sequence(
                new SetBlackboard(BBKeys.LastRestTime, () => Time.time),
                new SetBlackboard(BBKeys.CurrentState, () => { _cachedState = "Rest"; return "Rest"; }),
                new Wait(_restDuration)
            );
            restBehavior.Name = "RestBehavior";
            
            return new UtilityAction(
                "Rest",
                restBehavior,
                _restWeight,
                // More likely when energy is low (inverted curve)
                // Energy 0-0.5 maps to high score, 0.5-1.0 maps to low score
                new BlackboardConsideration<float>("TiredCheck", BBKeys.Energy,
                    e => Mathf.Pow(1f - Mathf.Clamp01(e / 0.6f), 2f), 1f),
                // Cooldown between rests
                new TimeConsideration("RestCooldown", BBKeys.LastRestTime, 8f)
            );
        }
        
        private UtilityAction CreateWanderAction()
        {
            var wanderBehavior = new Sequence(
                new SetBlackboard(BBKeys.LastWanderTime, () => Time.time),
                new SetBlackboard(BBKeys.CurrentState, () => { _cachedState = "Wander"; return "Wander"; }),
                new MoveTo(
                    () => GetOrRefreshWanderTarget(),
                    _arrivalDistance,
                    _wanderSpeed,
                    8f
                )
            );
            wanderBehavior.Name = "WanderBehavior";
            
            return new UtilityAction(
                "Wander",
                wanderBehavior,
                _wanderWeight,
                // More likely when energy is moderate to high
                new BlackboardConsideration<float>("EnergyForWander", BBKeys.Energy,
                    e => Mathf.Clamp01((e - 0.3f) / 0.7f), 1f),
                // Cooldown between wanders
                new TimeConsideration("WanderCooldown", BBKeys.LastWanderTime, 5f)
            );
        }
        
        private Vector3 GetRandomWanderPoint()
        {
            // Get a random point near current waypoint or home position
            Vector3 center = WaypointPath != null ? GetCurrentWaypoint() : _homePosition;
            Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * _wanderRadius;
            return center + new Vector3(randomCircle.x, 0f, randomCircle.y);
        }
        
        private Vector3 GetOrRefreshWanderTarget()
        {
            // Refresh if close to current target (use sqrMagnitude to avoid sqrt)
            if ((transform.position - _currentWanderTarget).sqrMagnitude < _arrivalDistanceSqr2x)
            {
                _currentWanderTarget = GetRandomWanderPoint();
            }
            return _currentWanderTarget;
        }
    }
}
