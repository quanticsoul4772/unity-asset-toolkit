using System.Collections.Generic;
using UnityEngine;
using NPCBrain;
using NPCBrain.BehaviorTree;
using NPCBrain.BehaviorTree.Composites;
using NPCBrain.BehaviorTree.Actions;
using NPCBrain.Perception;
using NPCBrain.UtilityAI;

namespace NPCBrain.Archetypes
{
    /// <summary>
    /// Cop NPC archetype that patrols, investigates sounds, chases robbers, and arrests them.
    /// Uses Utility AI with hearing and sight-aware scoring.
    /// </summary>
    /// <remarks>
    /// <para>Utility-scored behaviors:</para>
    /// <list type="bullet">
    ///   <item><description>Arrest - Very close to robber, capture them</description></item>
    ///   <item><description>Chase - Robber visible and in range</description></item>
    ///   <item><description>InvestigateAlarm - Alarm sound heard</description></item>
    ///   <item><description>InvestigateSound - Footstep/other sound heard</description></item>
    ///   <item><description>Return - Far from patrol area</description></item>
    ///   <item><description>Patrol - Standard patrol behavior</description></item>
    /// </list>
    /// </remarks>
    public class CopNPC : NPCBrainController, IAlertableNPC
    {
        /// <summary>All active CopNPC instances.</summary>
        public static IReadOnlyList<CopNPC> AllInstances => NPCRegistry<CopNPC>.Instances;

        [Header("Cop Settings")]
        [SerializeField] private float _chaseSpeed = 6.5f;
        [SerializeField] private float _patrolSpeed = 3f;
        [SerializeField] private float _investigateSpeed = 5f;
        [SerializeField] private float _arrivalDistance = 0.5f;
        [SerializeField] private float _arrestDistance = 2f;
        [SerializeField] private float _waypointWaitTime = 2f;
        [SerializeField] private float _investigateTime = 3f;
        [SerializeField] private float _maxChaseDistance = 25f;
        
        [Header("Alert Settings")]
        [SerializeField] private float _alertDecayRate = 0.1f;
        [SerializeField] private float _alertIncreaseRate = 0.5f;
        [SerializeField] private float _alarmAlertBoost = 0.9f;
        [SerializeField] private float _footstepAlertBoost = 0.2f;
        
        [Header("Utility Weights")]
        [SerializeField] private float _arrestWeight = 1.1f;
        [SerializeField] private float _chaseWeight = 1.0f;
        [SerializeField] private float _alarmInvestigateWeight = 0.9f;
        [SerializeField] private float _soundInvestigateWeight = 0.5f;
        [SerializeField] private float _returnWeight = 0.4f;
        [SerializeField] private float _patrolWeight = 0.3f;
        
        private Vector3 _homePosition;
        private int _arrestCount;
        private RobberNPC _cachedTargetRobber;
        
        /// <summary>Current behavior state for UI display.</summary>
        public string CurrentState => Blackboard.Get("currentState", "Patrol");
        
        /// <summary>Current alert level (0-1).</summary>
        public float AlertLevel => Blackboard.Get("alertLevel", 0f);
        
        /// <summary>Number of arrests made.</summary>
        public int ArrestCount => _arrestCount;
        
        /// <summary>Whether this NPC is still active.</summary>
        public bool IsActive => gameObject.activeSelf;
        
        /// <summary>Event raised when this cop arrests a robber.</summary>
        public event System.Action<CopNPC, RobberNPC> OnArrest;
        
        protected override void Awake()
        {
            base.Awake();
            NPCRegistry<CopNPC>.Register(this);
            _homePosition = transform.position;
            
            // Note: Detection uses NPCRegistry<CopNPC> instead of tags to avoid Unity tag setup requirements
            
            Blackboard.Set("homePosition", _homePosition);
            Blackboard.SetFloat(BBKeys.AlertLevel, 0f);
            Blackboard.Set("currentState", "Patrol");
            
            // Initialize action timestamps
            Blackboard.Set("lastChaseTime", -10f);
            Blackboard.Set("lastInvestigateTime", -10f);
            Blackboard.Set("lastPatrolTime", -10f);
            Blackboard.Set("lastReturnTime", -10f);
            Blackboard.Set("lastArrestTime", -10f);
            
            // Subscribe to perception events
            OnTargetAcquired += HandleTargetAcquired;
            OnTargetLost += HandleTargetLost;
            OnSoundHeard += HandleSoundHeard;
        }
        
        protected override void OnDestroy()
        {
            NPCRegistry<CopNPC>.Unregister(this);
            OnTargetAcquired -= HandleTargetAcquired;
            OnTargetLost -= HandleTargetLost;
            OnSoundHeard -= HandleSoundHeard;
            base.OnDestroy();
        }
        
        private void HandleTargetAcquired(GameObject target)
        {
            // Only react to RobberNPC targets (filter since we can't use tags)
            var robber = target.GetComponent<RobberNPC>();
            if (robber == null) return;
            
            // Cache the component reference
            _cachedTargetRobber = robber;
            Blackboard.Set("target", target);
            Blackboard.Set("investigatePosition", target.transform.position);
            IncreaseAlert(0.6f);
        }
        
        private void HandleTargetLost(GameObject target)
        {
            if (Blackboard.Has("target"))
            {
                var currentTarget = Blackboard.Get<GameObject>("target");
                if (currentTarget == target)
                {
                    Blackboard.Remove("target");
                    _cachedTargetRobber = null;
                }
            }
        }
        
        private void HandleSoundHeard(SoundEvent sound)
        {
            if (!Blackboard.Has("target"))
            {
                Blackboard.Set("investigatePosition", sound.Position);
                Blackboard.Set("lastSoundType", (int)sound.Type);
                
                if (sound.Type >= SoundType.Alarm)
                {
                    IncreaseAlert(_alarmAlertBoost);
                }
                else if (sound.Type >= SoundType.Footstep)
                {
                    IncreaseAlert(_footstepAlertBoost);
                }
            }
        }
        
        /// <summary>Increases the alert level by the specified amount.</summary>
        public void IncreaseAlert(float amount)
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
            if (!Blackboard.Has("target"))
            {
                DecayAlert();
            }
            
            // Update investigation position if we have a visible target
            if (Blackboard.TryGet<GameObject>("target", out var target) && target != null)
            {
                Blackboard.Set("investigatePosition", target.transform.position);
                IncreaseAlert(_alertIncreaseRate * Time.deltaTime);
                
                // Check for arrest opportunity
                CheckArrestOpportunity(target);
            }
        }
        
        private void CheckArrestOpportunity(GameObject target)
        {
            if (target == null) return;
            
            float distance = Vector3.Distance(transform.position, target.transform.position);
            Blackboard.Set("targetDistance", distance);
            Blackboard.Set("canArrest", distance <= _arrestDistance);
        }
        
        /// <summary>
        /// Attempts to arrest the current target.
        /// </summary>
        private void TryArrest()
        {
            if (!Blackboard.TryGet<GameObject>("target", out var target) || target == null)
            {
                return;
            }
            
            // Use cached reference instead of GetComponent
            var robber = _cachedTargetRobber;
            if (robber == null)
            {
                // Fallback if cache is stale
                robber = target.GetComponent<RobberNPC>();
                _cachedTargetRobber = robber;
            }
            
            if (robber != null && !robber.HasEscaped)
            {
                float distance = Vector3.Distance(transform.position, target.transform.position);
                if (distance <= _arrestDistance)
                {
                    robber.OnArrested();
                    _arrestCount++;
                    Blackboard.Remove("target");
                    Blackboard.Set("canArrest", false);
                    _cachedTargetRobber = null;
                    
                    OnArrest?.Invoke(this, robber);
                    
                    NPCBrainDebug.Log(NPCBrainDebug.Category.General, $"[CopsAndRobbers] {name} arrested {robber.name}!", this);
                }
            }
        }
        
        /// <inheritdoc/>
        protected override BTNode CreateBehaviorTree()
        {
            var arrestAction = CreateArrestAction();
            var chaseAction = CreateChaseAction();
            var alarmInvestigateAction = CreateAlarmInvestigateAction();
            var soundInvestigateAction = CreateSoundInvestigateAction();
            var returnAction = CreateReturnAction();
            var patrolAction = CreatePatrolAction();
            
            return new UtilitySelector(
                arrestAction,
                chaseAction,
                alarmInvestigateAction,
                soundInvestigateAction,
                returnAction,
                patrolAction
            );
        }
        
        private UtilityAction CreateArrestAction()
        {
            var arrestBehavior = new Sequence(
                new SetBlackboard("lastArrestTime", () => Time.time),
                new SetBlackboard("currentState", "Arresting!"),
                new Wait(0.5f, () => TryArrest())
            );
            arrestBehavior.Name = "ArrestBehavior";
            
            return new UtilityAction(
                "Arrest",
                arrestBehavior,
                _arrestWeight,
                // Must be able to arrest (very close to target)
                new BlackboardConsideration<bool>("CanArrest", "canArrest",
                    can => can ? 1f : 0f, false),
                // Must have a target
                new BlackboardConsideration<GameObject>("HasTarget", "target",
                    t => t != null ? 1f : 0f, null)
            );
        }
        
        private UtilityAction CreateChaseAction()
        {
            var chaseBehavior = new Sequence(
                new SetBlackboard("lastChaseTime", () => Time.time),
                new SetBlackboard("currentState", "Chase!"),
                new MoveTo(
                    () => GetTargetPosition(),
                    _arrestDistance * 0.8f, // Get very close for arrest
                    _chaseSpeed,
                    5f
                )
            );
            chaseBehavior.Name = "ChaseBehavior";
            
            return new UtilityAction(
                "Chase",
                chaseBehavior,
                _chaseWeight,
                // Must have a visible target
                new BlackboardConsideration<GameObject>("HasTarget", "target",
                    t => t != null ? 1f : 0f, null),
                // Not close enough to arrest
                new BlackboardConsideration<bool>("CantArrestYet", "canArrest",
                    can => can ? 0f : 1f, false),
                // Higher score when target is close
                new DistanceConsideration(
                    "TargetDistance",
                    brain => GetTargetPositionForCheck(brain),
                    _maxChaseDistance,
                    true
                ),
                // Higher score when alert
                new BlackboardConsideration<float>("AlertForChase", "alertLevel",
                    a => 0.5f + a * 0.5f, 0f)
            );
        }
        
        private UtilityAction CreateAlarmInvestigateAction()
        {
            var investigateBehavior = new Sequence(
                new SetBlackboard("lastInvestigateTime", () => Time.time),
                new SetBlackboard("currentState", "Investigate-Alarm"),
                new MoveTo(
                    () => Blackboard.Get<Vector3>("investigatePosition"),
                    _arrivalDistance,
                    _investigateSpeed
                ),
                new Wait(_investigateTime * 0.5f),
                new ClearBlackboardKey("investigatePosition"),
                new ClearBlackboardKey("lastSoundType")
            );
            investigateBehavior.Name = "AlarmInvestigateBehavior";
            
            return new UtilityAction(
                "InvestigateAlarm",
                investigateBehavior,
                _alarmInvestigateWeight,
                // Must have heard an alarm
                new HasHeardSoundConsideration("HeardAlarm", SoundType.Alarm),
                // No visible target
                new BlackboardConsideration<GameObject>("NoVisibleTarget", "target",
                    t => t == null ? 1f : 0f, null),
                // Higher score when alert
                new BlackboardConsideration<float>("AlertForAlarm", "alertLevel",
                    a => 0.3f + a * 0.7f, 0f),
                // Distance consideration
                new SoundDistanceConsideration("AlarmDistance", 50f, true),
                // Cooldown
                new TimeConsideration("InvestigateCooldown", "lastInvestigateTime", 2f)
            );
        }
        
        private UtilityAction CreateSoundInvestigateAction()
        {
            var investigateBehavior = new Sequence(
                new SetBlackboard("lastInvestigateTime", () => Time.time),
                new SetBlackboard("currentState", "Investigate"),
                new MoveTo(
                    () => Blackboard.Get<Vector3>("investigatePosition"),
                    _arrivalDistance,
                    _investigateSpeed * 0.8f
                ),
                new Wait(_investigateTime),
                new ClearBlackboardKey("investigatePosition"),
                new ClearBlackboardKey("lastSoundType")
            );
            investigateBehavior.Name = "SoundInvestigateBehavior";
            
            return new UtilityAction(
                "InvestigateSound",
                investigateBehavior,
                _soundInvestigateWeight,
                // Must have heard at least a footstep
                new HasHeardSoundConsideration("HeardSound", SoundType.Footstep),
                // No visible target
                new BlackboardConsideration<GameObject>("NoVisibleTarget", "target",
                    t => t == null ? 1f : 0f, null),
                // Moderate alert needed
                new BlackboardConsideration<float>("AlertForSound", "alertLevel",
                    a => a > 0.1f ? 0.5f + a * 0.5f : 0.3f, 0f),
                // Cooldown
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
                // No target
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
                    false
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
                // Always available as baseline
                new ConstantConsideration(0.8f),
                // Less likely when alert
                new BlackboardConsideration<float>("LowAlert", "alertLevel",
                    a => 1f - a * 0.5f, 0f),
                // Cooldown
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
        
        /// <summary>
        /// Creates a CopNPC at the specified position.
        /// </summary>
        public static CopNPC Create(Vector3 position, Transform parent = null)
        {
            var copObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            copObj.name = "Cop";
            copObj.transform.position = position;
            var copRenderer = copObj.GetComponent<Renderer>();
            copRenderer.material.color = new Color(0.2f, 0.4f, 0.8f); // Blue
            
            // Note: Detection uses NPCRegistry and component checks instead of tags
            
            if (parent != null)
            {
                copObj.transform.SetParent(parent);
            }
            
            // Add sight sensor - clear target tag to detect all (tags require manual Unity setup)
            var sightSensor = copObj.AddComponent<SightSensor>();
            sightSensor.SetTargetTag(""); // Detect all targets, not just specific tags
            
            // Add hearing sensor
            var hearingSensor = copObj.AddComponent<HearingSensor>();
            
            // Add cop component
            var cop = copObj.AddComponent<CopNPC>();
            
            // Add police hat indicator
            var hat = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hat.name = "PoliceHat";
            hat.transform.SetParent(copObj.transform);
            hat.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            hat.transform.localScale = new Vector3(0.6f, 0.15f, 0.6f);
            var hatRenderer = hat.GetComponent<Renderer>();
            hatRenderer.material.color = new Color(0.1f, 0.2f, 0.5f);
            Object.Destroy(hat.GetComponent<Collider>());
            
            // Add badge
            var badge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            badge.name = "Badge";
            badge.transform.SetParent(copObj.transform);
            badge.transform.localPosition = new Vector3(0.25f, 0.8f, 0.25f);
            badge.transform.localScale = new Vector3(0.15f, 0.15f, 0.05f);
            var badgeRenderer = badge.GetComponent<Renderer>();
            badgeRenderer.material.color = Color.yellow;
            Object.Destroy(badge.GetComponent<Collider>());
            
            return cop;
        }
    }
}
