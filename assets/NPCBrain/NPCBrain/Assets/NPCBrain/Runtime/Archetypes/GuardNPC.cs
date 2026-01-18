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
    public class GuardNPC : NPCBrainController
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
        public string CurrentState => Blackboard.Get("currentState", "Patrol");
        
        /// <summary>Current alert level (0-1).</summary>
        public float AlertLevel => Blackboard.Get("alertLevel", 0f);
        
        protected override void Awake()
        {
            base.Awake();
            _homePosition = transform.position;
            Blackboard.Set("homePosition", _homePosition);
            Blackboard.Set("alertLevel", 0f);
            Blackboard.Set("currentState", "Patrol");
            
            // Initialize action timestamps for TimeConsiderations
            Blackboard.Set("lastChaseTime", -10f);
            Blackboard.Set("lastInvestigateTime", -10f);
            Blackboard.Set("lastPatrolTime", -10f);
            Blackboard.Set("lastReturnTime", -10f);
            
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
            Blackboard.Set("target", target);
            Blackboard.Set("lastKnownPosition", target.transform.position);
            IncreaseAlert(0.5f);
        }
        
        private void HandleTargetLost(GameObject target)
        {
            // Keep last known position for investigation
            if (Blackboard.Has("target"))
            {
                var currentTarget = Blackboard.Get<GameObject>("target");
                if (currentTarget == target)
                {
                    Blackboard.Remove("target");
                }
            }
        }
        
        private void IncreaseAlert(float amount)
        {
            float current = Blackboard.Get("alertLevel", 0f);
            Blackboard.Set("alertLevel", Mathf.Clamp01(current + amount));
        }
        
        private void DecayAlert()
        {
            float current = Blackboard.Get("alertLevel", 0f);
            if (current > 0f)
            {
                Blackboard.Set("alertLevel", Mathf.Max(0f, current - _alertDecayRate * Time.deltaTime));
            }
        }
        
        private void LateUpdate()
        {
            // Decay alert over time when not actively engaged
            if (!Blackboard.Has("target"))
            {
                DecayAlert();
            }
            
            // Update last known position if we have a visible target
            if (Blackboard.TryGet<GameObject>("target", out var target) && target != null)
            {
                Blackboard.Set("lastKnownPosition", target.transform.position);
                IncreaseAlert(_alertIncreaseRate * Time.deltaTime);
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
                new SetBlackboard("lastChaseTime", () => Time.time),
                new SetBlackboard("currentState", "Chase"),
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
                new BlackboardConsideration<GameObject>("HasTarget", "target",
                    t => t != null ? 1f : 0f, null),
                // Higher score when target is close
                new DistanceConsideration(
                    "TargetDistance",
                    brain => GetTargetPositionForCheck(brain),
                    _maxChaseDistance,
                    true
                ),
                // Higher score when alert level is high
                new BlackboardConsideration<float>("AlertForChase", "alertLevel",
                    a => 0.5f + a * 0.5f, 0f)
            );
        }
        
        private UtilityAction CreateInvestigateAction()
        {
            var investigateBehavior = new Sequence(
                new SetBlackboard("lastInvestigateTime", () => Time.time),
                new SetBlackboard("currentState", "Investigate"),
                new MoveTo(
                    () => Blackboard.Get<Vector3>("lastKnownPosition"),
                    _arrivalDistance,
                    _investigateSpeed
                ),
                new Wait(_investigateTime),
                new ClearBlackboardKey("lastKnownPosition")
            );
            investigateBehavior.Name = "InvestigateBehavior";
            
            return new UtilityAction(
                "Investigate",
                investigateBehavior,
                _investigateWeight,
                // Must have a last known position
                new BlackboardConsideration<Vector3>("HasLastKnown", "lastKnownPosition",
                    pos => pos != Vector3.zero ? 1f : 0f, Vector3.zero),
                // Must not have a visible target (chase takes priority)
                new BlackboardConsideration<GameObject>("NoVisibleTarget", "target",
                    t => t == null ? 1f : 0f, null),
                // Higher score when alert level is moderate to high
                new BlackboardConsideration<float>("AlertForInvestigate", "alertLevel",
                    a => a > 0.1f ? 0.5f + a * 0.5f : 0.2f, 0f),
                // Distance to last known position - closer = higher priority
                new DistanceConsideration(
                    "LastKnownDistance",
                    brain => brain.Blackboard.Get("lastKnownPosition", brain.transform.position),
                    _maxChaseDistance,
                    true
                ),
                // Cooldown between investigations
                new TimeConsideration("InvestigateCooldown", "lastInvestigateTime", 2f)
            );
        }
        
        private UtilityAction CreateReturnAction()
        {
            var returnBehavior = new Sequence(
                new SetBlackboard("lastReturnTime", () => Time.time),
                new SetBlackboard("currentState", "Return"),
                new MoveTo(
                    () => Blackboard.Get<Vector3>("homePosition"),
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
                new BlackboardConsideration<GameObject>("NoTarget", "target",
                    t => t == null ? 1f : 0f, null),
                // No pending investigation
                new BlackboardConsideration<Vector3>("NoLastKnown", "lastKnownPosition",
                    pos => pos == Vector3.zero ? 1f : 0.3f, Vector3.zero),
                // Higher score when far from home
                new DistanceConsideration(
                    "DistanceFromHome",
                    brain => brain.Blackboard.Get("homePosition", brain.transform.position),
                    15f,
                    false  // farther = higher score
                ),
                // Cooldown
                new TimeConsideration("ReturnCooldown", "lastReturnTime", 5f)
            );
        }
        
        private UtilityAction CreatePatrolAction()
        {
            var patrolBehavior = new Sequence(
                new SetBlackboard("lastPatrolTime", () => Time.time),
                new SetBlackboard("currentState", "Patrol"),
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
                new BlackboardConsideration<float>("LowAlert", "alertLevel",
                    a => 1f - a * 0.5f, 0f),
                // Cooldown between patrol waypoints
                new TimeConsideration("PatrolCooldown", "lastPatrolTime", 2f)
            );
        }
        
        private Vector3 GetTargetPosition()
        {
            if (Blackboard.TryGet<GameObject>("target", out var target) && target != null)
            {
                return target.transform.position;
            }
            return Blackboard.Get<Vector3>("lastKnownPosition", transform.position);
        }
        
        private static Vector3 GetTargetPositionForCheck(NPCBrainController brain)
        {
            if (brain.Blackboard.TryGet<GameObject>("target", out var target) && target != null)
            {
                return target.transform.position;
            }
            return brain.transform.position;
        }
    }
}
