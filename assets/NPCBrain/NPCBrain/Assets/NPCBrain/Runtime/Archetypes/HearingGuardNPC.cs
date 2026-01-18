using UnityEngine;
using NPCBrain.BehaviorTree;
using NPCBrain.BehaviorTree.Composites;
using NPCBrain.BehaviorTree.Actions;
using NPCBrain.BehaviorTree.Decorators;
using NPCBrain.Perception;
using NPCBrain.UtilityAI;

namespace NPCBrain.Archetypes
{
    /// <summary>
    /// Guard NPC archetype that uses Utility AI with hearing-aware scoring.
    /// Responds to both visual and audio stimuli with dynamic, probabilistic behavior.
    /// </summary>
    /// <remarks>
    /// <para>Utility-scored behaviors:</para>
    /// <list type="bullet">
    ///   <item><description>Chase - High score when target visible and close</description></item>
    ///   <item><description>InvestigateGunshot - High score for loud sounds</description></item>
    ///   <item><description>InvestigateFootstep - Medium score for quiet sounds</description></item>
    ///   <item><description>ReturnToPost - Score when far from home, no threats</description></item>
    ///   <item><description>Patrol - Baseline fallback behavior</description></item>
    /// </list>
    /// <para>The Criticality system adjusts temperature based on action variety:</para>
    /// <list type="bullet">
    ///   <item><description>Repetitive behavior → Higher temperature → More exploration</description></item>
    ///   <item><description>Varied behavior → Lower temperature → More exploitation</description></item>
    /// </list>
    /// </remarks>
    public class HearingGuardNPC : NPCBrainController, IAlertableNPC
    {
        [Header("Guard Settings")]
        [SerializeField] private float _chaseSpeed = 6f;
        [SerializeField] private float _patrolSpeed = 3f;
        [SerializeField] private float _investigateSpeed = 4f;
        [SerializeField] private float _urgentInvestigateSpeed = 5f;
        [SerializeField] private float _arrivalDistance = 0.5f;
        [SerializeField] private float _chaseArrivalDistance = 1.5f;
        [SerializeField] private float _waypointWaitTime = 2f;
        [SerializeField] private float _investigateTime = 3f;
        [SerializeField] private float _maxChaseDistance = 20f;
        
        [Header("Alert Settings")]
        [SerializeField] private float _alertDecayRate = 0.1f;
        [SerializeField] private float _alertIncreaseRate = 0.5f;
        [SerializeField] private float _gunshotAlertBoost = 0.8f;
        [SerializeField] private float _footstepAlertBoost = 0.2f;
        
        [Header("Utility Weights")]
        [SerializeField] private float _chaseWeight = 1.0f;
        [SerializeField] private float _gunshotInvestigateWeight = 0.85f;
        [SerializeField] private float _footstepInvestigateWeight = 0.5f;
        [SerializeField] private float _returnWeight = 0.4f;
        [SerializeField] private float _patrolWeight = 0.3f;
        
        private Vector3 _homePosition;
        
        /// <summary>Current behavior state for UI display.</summary>
        public string CurrentState => Blackboard.Get("currentState", "Patrol");
        
        /// <summary>Current alert level (0-1).</summary>
        public float AlertLevel => Blackboard.Get("alertLevel", 0f);
        
        /// <summary>Whether this NPC is still active.</summary>
        public bool IsActive => gameObject.activeSelf;
        
        /// <summary>Increases the alert level by the specified amount.</summary>
        public void IncreaseAlert(float amount)
        {
            float current = Blackboard.Get("alertLevel", 0f);
            Blackboard.Set("alertLevel", Mathf.Clamp01(current + amount));
        }
        
        /// <summary>Last sound type heard.</summary>
        public SoundType? LastSoundType => Blackboard.Has("lastSoundType") 
            ? (SoundType?)Blackboard.Get<int>("lastSoundType") 
            : null;
        
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
            OnSoundHeard += HandleSoundHeard;
        }
        
        protected override void OnDestroy()
        {
            OnTargetAcquired -= HandleTargetAcquired;
            OnTargetLost -= HandleTargetLost;
            OnSoundHeard -= HandleSoundHeard;
            base.OnDestroy();
        }
        
        private void HandleTargetAcquired(GameObject target)
        {
            Blackboard.Set("target", target);
            Blackboard.Set("investigatePosition", target.transform.position);
            IncreaseAlert(0.5f);
        }
        
        private void HandleTargetLost(GameObject target)
        {
            if (Blackboard.Has("target"))
            {
                var currentTarget = Blackboard.Get<GameObject>("target");
                if (currentTarget == target)
                {
                    Blackboard.Remove("target");
                }
            }
        }
        
        private void HandleSoundHeard(SoundEvent sound)
        {
            // Only update investigate position if we don't have a visible target
            if (!Blackboard.Has("target"))
            {
                bool shouldUpdate = true;
                if (Blackboard.Has("lastSoundType"))
                {
                    var currentType = (SoundType)Blackboard.Get<int>("lastSoundType");
                    shouldUpdate = sound.Type >= currentType;
                }
                
                if (shouldUpdate)
                {
                    Blackboard.Set("investigatePosition", sound.Position);
                    Blackboard.Set("lastSoundType", (int)sound.Type);
                    
                    if (sound.Type >= SoundType.Gunshot)
                    {
                        IncreaseAlert(_gunshotAlertBoost);
                    }
                    else if (sound.Type >= SoundType.Footstep)
                    {
                        IncreaseAlert(_footstepAlertBoost);
                    }
                }
            }
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
                Blackboard.Set("investigatePosition", target.transform.position);
                IncreaseAlert(_alertIncreaseRate * Time.deltaTime);
            }
        }
        
        /// <inheritdoc/>
        protected override BTNode CreateBehaviorTree()
        {
            // Create utility actions with hearing-aware considerations
            var chaseAction = CreateChaseAction();
            var gunshotAction = CreateGunshotInvestigateAction();
            var footstepAction = CreateFootstepInvestigateAction();
            var returnAction = CreateReturnAction();
            var patrolAction = CreatePatrolAction();
            
            // Use UtilitySelector - this activates the Criticality system!
            return new UtilitySelector(
                chaseAction,
                gunshotAction,
                footstepAction,
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
        
        private UtilityAction CreateGunshotInvestigateAction()
        {
            var investigateBehavior = new Sequence(
                new SetBlackboard("lastInvestigateTime", () => Time.time),
                new SetBlackboard("currentState", "Investigate-Gunshot"),
                new MoveTo(
                    () => Blackboard.Get<Vector3>("investigatePosition"),
                    _arrivalDistance,
                    _urgentInvestigateSpeed
                ),
                new Wait(_investigateTime * 0.5f),
                new ClearBlackboardKey("investigatePosition"),
                new ClearBlackboardKey("lastSoundType")
            );
            investigateBehavior.Name = "GunshotInvestigateBehavior";
            
            return new UtilityAction(
                "InvestigateGunshot",
                investigateBehavior,
                _gunshotInvestigateWeight,
                // Must have heard a gunshot or higher
                new HasHeardSoundConsideration("HeardGunshot", SoundType.Gunshot),
                // Must not have a visible target (chase takes priority)
                new BlackboardConsideration<GameObject>("NoVisibleTarget", "target",
                    t => t == null ? 1f : 0f, null),
                // Higher score when sound is close
                new SoundDistanceConsideration("GunshotDistance", 40f, true),
                // Higher score when alert level is high
                new BlackboardConsideration<float>("AlertForGunshot", "alertLevel",
                    a => 0.3f + a * 0.7f, 0f),
                // Cooldown between investigations
                new TimeConsideration("InvestigateCooldown", "lastInvestigateTime", 2f)
            );
        }
        
        private UtilityAction CreateFootstepInvestigateAction()
        {
            var investigateBehavior = new Sequence(
                new SetBlackboard("lastInvestigateTime", () => Time.time),
                new SetBlackboard("currentState", "Investigate-Footstep"),
                new MoveTo(
                    () => Blackboard.Get<Vector3>("investigatePosition"),
                    _arrivalDistance,
                    _investigateSpeed
                ),
                new Wait(_investigateTime),
                new ClearBlackboardKey("investigatePosition"),
                new ClearBlackboardKey("lastSoundType")
            );
            investigateBehavior.Name = "FootstepInvestigateBehavior";
            
            return new UtilityAction(
                "InvestigateFootstep",
                investigateBehavior,
                _footstepInvestigateWeight,
                // Must have heard at least a footstep
                new HasHeardSoundConsideration("HeardFootstep", SoundType.Footstep),
                // Must not have a visible target
                new BlackboardConsideration<GameObject>("NoVisibleTarget", "target",
                    t => t == null ? 1f : 0f, null),
                // Higher score when sound is close
                new SoundDistanceConsideration("FootstepDistance", 25f, true),
                // Moderate alert needed
                new BlackboardConsideration<float>("AlertForFootstep", "alertLevel",
                    a => a > 0.1f ? 0.5f + a * 0.5f : 0.3f, 0f),
                // Cooldown between investigations
                new TimeConsideration("InvestigateCooldown", "lastInvestigateTime", 3f)
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
                new BlackboardConsideration<Vector3>("NoInvestigation", "investigatePosition",
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
            return Blackboard.Get<Vector3>("investigatePosition", transform.position);
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
