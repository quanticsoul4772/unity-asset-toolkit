using UnityEngine;
using NPCBrain.BehaviorTree;
using NPCBrain.BehaviorTree.Composites;
using NPCBrain.BehaviorTree.Actions;
using NPCBrain.BehaviorTree.Decorators;
using NPCBrain.UtilityAI;

namespace NPCBrain.Archetypes
{
    /// <summary>
    /// Guard NPC archetype that uses Utility AI with sight-aware scoring.
    /// Responds to visual stimuli with dynamic, probabilistic behavior.
    /// </summary>
    /// <remarks>
    /// <para>Utility-scored behaviors:</para>
    /// <list type="bullet">
    ///   <item><description>Chase - High score when target visible and close</description></item>
    ///   <item><description>Investigate - Medium score for last known positions</description></item>
    ///   <item><description>Return - Score when far from home, no threats</description></item>
    ///   <item><description>Patrol - Baseline fallback behavior</description></item>
    /// </list>
    /// <para>The Criticality system adjusts temperature based on action variety:</para>
    /// <list type="bullet">
    ///   <item><description>Repetitive behavior → Higher temperature → More exploration</description></item>
    ///   <item><description>Varied behavior → Lower temperature → More exploitation</description></item>
    /// </list>
    /// </remarks>
    public class GuardNPC : NPCBrainController, IAlertableNPC
    {
        [Header("Guard Settings")]
        [SerializeField] private float _chaseSpeed = 6f;
        [SerializeField] private float _patrolSpeed = 3f;
        [SerializeField] private float _investigateSpeed = 4f;
        [SerializeField] private float _arrivalDistance = 0.5f;
        [SerializeField] private float _chaseArrivalDistance = 1.5f;
        [SerializeField] private float _waypointWaitTime = 2f;
        [SerializeField] private float _investigateTime = 3f;
        [SerializeField] private float _maxChaseDistance = 20f;
        
        [Header("Alert Settings")]
        [SerializeField] private float _alertDecayRate = 0.1f;
        [SerializeField] private float _alertIncreaseRate = 0.5f;
        
        [Header("Utility Weights")]
        [SerializeField] private float _chaseWeight = 1.0f;
        [SerializeField] private float _investigateWeight = 0.7f;
        [SerializeField] private float _returnWeight = 0.4f;
        [SerializeField] private float _patrolWeight = 0.3f;
        
        private Vector3 _homePosition;
        
        /// <summary>Current behavior state for UI display.</summary>
        public string CurrentState => Blackboard.Get(BBKeys.CurrentState, "Patrol");
        
        /// <summary>Current alert level (0-1).</summary>
        public float AlertLevel => Blackboard.GetFloat(BBKeys.AlertLevel, 0f);
        
        /// <summary>Whether this NPC is still active.</summary>
        public bool IsActive => gameObject.activeSelf;
        
        /// <summary>Increases the alert level by the specified amount.</summary>
        public void IncreaseAlert(float amount)
        {
            float current = Blackboard.GetFloat(BBKeys.AlertLevel, 0f);
            Blackboard.SetFloat(BBKeys.AlertLevel, Mathf.Clamp01(current + amount));
        }
        
        protected override void Awake()
        {
            base.Awake();
            _homePosition = transform.position;
            Blackboard.SetVector3(BBKeys.HomePosition, _homePosition);
            Blackboard.SetFloat(BBKeys.AlertLevel, 0f);
            Blackboard.Set(BBKeys.CurrentState, "Patrol");
            
            // Initialize action timestamps for TimeConsiderations
            Blackboard.SetFloat(BBKeys.LastChaseTime, -10f);
            Blackboard.SetFloat(BBKeys.LastInvestigateTime, -10f);
            Blackboard.SetFloat(BBKeys.LastPatrolTime, -10f);
            Blackboard.SetFloat(BBKeys.LastReturnTime, -10f);
            
            // Subscribe to perception events
            OnTargetAcquired += HandleTargetAcquired;
            OnTargetLost += HandleTargetLost;
        }
        
        protected override void OnDestroy()
        {
            OnTargetAcquired -= HandleTargetAcquired;
            OnTargetLost -= HandleTargetLost;
            base.OnDestroy();
        }
        
        private void HandleTargetAcquired(GameObject target)
        {
            Blackboard.Set(BBKeys.Target, target);
            Blackboard.SetVector3(BBKeys.LastKnownPosition, target.transform.position);
            IncreaseAlert(0.5f);
        }
        
        private void HandleTargetLost(GameObject target)
        {
            // Keep last known position for investigation
            if (Blackboard.TryGet<GameObject>(BBKeys.Target, out var currentTarget) && currentTarget == target)
            {
                Blackboard.Remove(BBKeys.Target);
            }
        }
        
        private void DecayAlert()
        {
            float current = Blackboard.GetFloat(BBKeys.AlertLevel, 0f);
            if (current > 0f)
            {
                Blackboard.SetFloat(BBKeys.AlertLevel, Mathf.Max(0f, current - _alertDecayRate * Time.deltaTime));
            }
        }
        
        private void LateUpdate()
        {
            // Single lookup for target - fixes redundant Blackboard access
            if (Blackboard.TryGet<GameObject>(BBKeys.Target, out var target) && target != null)
            {
                // Has visible target - update position and alert
                Blackboard.SetVector3(BBKeys.LastKnownPosition, target.transform.position);
                IncreaseAlert(_alertIncreaseRate * Time.deltaTime);
            }
            else
            {
                // No target - decay alert over time
                DecayAlert();
            }
        }
        
        /// <inheritdoc/>
        protected override BTNode CreateBehaviorTree()
        {
            // Create utility actions with sight-aware considerations
            var chaseAction = CreateChaseAction();
            var investigateAction = CreateInvestigateAction();
            var returnAction = CreateReturnAction();
            var patrolAction = CreatePatrolAction();
            
            // Use UtilitySelector - this activates the Criticality system!
            return new UtilitySelector(
                chaseAction,
                investigateAction,
                returnAction,
                patrolAction
            );
        }
        
        private UtilityAction CreateChaseAction()
        {
            var chaseBehavior = new Sequence(
                new SetBlackboard(BBKeys.LastChaseTime, () => Time.time),
                new SetBlackboard(BBKeys.CurrentState, "Chase"),
                new MoveTo(
                    () => GetTargetPosition(),
                    _chaseArrivalDistance,
                    _chaseSpeed,
                    5f
                )
            );
            chaseBehavior.Name = "ChaseBehavior";
            
            return new UtilityAction(
                "Chase",
                chaseBehavior,
                _chaseWeight,
                // Must have a visible target - this is the key gate
                new BlackboardConsideration<GameObject>("HasTarget", BBKeys.Target,
                    t => t != null ? 1f : 0f, null),
                // Higher score when target is close
                new DistanceConsideration(
                    "TargetDistance",
                    brain => GetTargetPositionForCheck(brain),
                    _maxChaseDistance,
                    true
                ),
                // Higher score when alert level is high
                new BlackboardConsideration<float>("AlertForChase", BBKeys.AlertLevel,
                    a => 0.5f + a * 0.5f, 0f)
            );
        }
        
        private UtilityAction CreateInvestigateAction()
        {
            var investigateBehavior = new Sequence(
                new SetBlackboard(BBKeys.LastInvestigateTime, () => Time.time),
                new SetBlackboard(BBKeys.CurrentState, "Investigate"),
                new MoveTo(
                    () => Blackboard.GetVector3(BBKeys.LastKnownPosition, Vector3.zero),
                    _arrivalDistance,
                    _investigateSpeed
                ),
                new Wait(_investigateTime),
                new ClearBlackboardKey(BBKeys.LastKnownPosition)
            );
            investigateBehavior.Name = "InvestigateBehavior";
            
            return new UtilityAction(
                "Investigate",
                investigateBehavior,
                _investigateWeight,
                // Must have a last known position
                new BlackboardConsideration<Vector3>("HasLastKnown", BBKeys.LastKnownPosition,
                    pos => pos != Vector3.zero ? 1f : 0f, Vector3.zero),
                // Must not have a visible target (chase takes priority)
                new BlackboardConsideration<GameObject>("NoVisibleTarget", BBKeys.Target,
                    t => t == null ? 1f : 0f, null),
                // Higher score when alert level is moderate to high
                new BlackboardConsideration<float>("AlertForInvestigate", BBKeys.AlertLevel,
                    a => a > 0.1f ? 0.5f + a * 0.5f : 0.2f, 0f),
                // Distance to last known position - closer = higher priority
                new DistanceConsideration(
                    "LastKnownDistance",
                    brain => brain.Blackboard.GetVector3(BBKeys.LastKnownPosition, brain.transform.position),
                    _maxChaseDistance,
                    true
                ),
                // Cooldown between investigations
                new TimeConsideration("InvestigateCooldown", BBKeys.LastInvestigateTime, 2f)
            );
        }
        
        private UtilityAction CreateReturnAction()
        {
            var returnBehavior = new Sequence(
                new SetBlackboard(BBKeys.LastReturnTime, () => Time.time),
                new SetBlackboard(BBKeys.CurrentState, "Return"),
                new MoveTo(
                    () => Blackboard.GetVector3(BBKeys.HomePosition, transform.position),
                    _arrivalDistance,
                    _patrolSpeed
                )
            );
            returnBehavior.Name = "ReturnBehavior";
            
            return new UtilityAction(
                "Return",
                returnBehavior,
                _returnWeight,
                // Must not have target or investigation
                new BlackboardConsideration<GameObject>("NoTarget", BBKeys.Target,
                    t => t == null ? 1f : 0f, null),
                // No pending investigation
                new BlackboardConsideration<Vector3>("NoLastKnown", BBKeys.LastKnownPosition,
                    pos => pos == Vector3.zero ? 1f : 0.3f, Vector3.zero),
                // Higher score when far from home
                new DistanceConsideration(
                    "DistanceFromHome",
                    brain => brain.Blackboard.GetVector3(BBKeys.HomePosition, brain.transform.position),
                    15f,
                    false  // farther = higher score
                ),
                // Cooldown
                new TimeConsideration("ReturnCooldown", BBKeys.LastReturnTime, 5f)
            );
        }
        
        private UtilityAction CreatePatrolAction()
        {
            var patrolBehavior = new Sequence(
                new SetBlackboard(BBKeys.LastPatrolTime, () => Time.time),
                new SetBlackboard(BBKeys.CurrentState, "Patrol"),
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
                // Always available as baseline (constant score)
                new ConstantConsideration(0.8f),
                // Less likely when alert is high
                new BlackboardConsideration<float>("LowAlert", BBKeys.AlertLevel,
                    a => 1f - a * 0.5f, 0f),
                // Cooldown between patrol waypoints
                new TimeConsideration("PatrolCooldown", BBKeys.LastPatrolTime, 2f)
            );
        }
        
        private Vector3 GetTargetPosition()
        {
            if (Blackboard.TryGet<GameObject>(BBKeys.Target, out var target) && target != null)
            {
                return target.transform.position;
            }
            return Blackboard.GetVector3(BBKeys.LastKnownPosition, transform.position);
        }
        
        private static Vector3 GetTargetPositionForCheck(NPCBrainController brain)
        {
            if (brain.Blackboard.TryGet<GameObject>(BBKeys.Target, out var target) && target != null)
            {
                return target.transform.position;
            }
            return brain.transform.position;
        }
    }
}
