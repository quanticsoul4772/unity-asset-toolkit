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
        [SerializeField] private float _respondToAlertWeight = 0.95f;
        [SerializeField] private float _alarmInvestigateWeight = 0.9f;
        [SerializeField] private float _soundInvestigateWeight = 0.5f;
        [SerializeField] private float _returnWeight = 0.4f;
        [SerializeField] private float _patrolWeight = 0.3f;
        
        private Vector3 _homePosition;
        private int _arrestCount;
        private RobberNPC _cachedTargetRobber;
        private float _arrestDistanceSqr;
        private string _cachedState = "Patrol";
        
        /// <summary>Current behavior state for UI display.</summary>
        public string CurrentState => _cachedState;
        
        /// <summary>The cop's role in the scenario.</summary>
        public string Role => "Bank Security Guard";
        
        /// <summary>The cop's primary objective.</summary>
        public string Goal => CrimeInProgress ? "Apprehend the robber!" : "Protect the bank money from theft";
        
        /// <summary>Whether a crime is in progress (alarm has been triggered).</summary>
        public bool CrimeInProgress => Blackboard.GetBool(BBKeys.CrimeInProgress, false);
        
        /// <summary>Dynamic explanation of current behavior.</summary>
        public string CurrentReason
        {
            get
            {
                bool crimeActive = CrimeInProgress;
                
                if (Blackboard.TryGet<GameObject>(BBKeys.Target, out var target) && target != null)
                {
                    if (_cachedState == "Arresting!") return "Robber within reach - making arrest!";
                    if (_cachedState == "Chase!") return "Robber spotted - pursuing suspect!";
                }
                if (_cachedState == "Responding!") return "ALARM! Converging on robbery location!";
                if (_cachedState == "Investigate-Alarm") return "ALARM! Robbery in progress - responding!";
                if (_cachedState == "Investigate") return "Suspicious sound heard - checking it out";
                if (_cachedState == "Return") return crimeActive ? "Searching for robber - returning to patrol" : "No threats - returning to patrol area";
                if (_cachedState == "Patrol")
                {
                    if (crimeActive) return "Searching for the robber...";
                    return AlertLevel > 0.3f ? "Guarding the money - staying vigilant" : "Routine patrol - protecting the bank";
                }
                return crimeActive ? "Hunting for the robber..." : "Guarding the money";
            }
        }
        
        /// <summary>Current alert level (0-1).</summary>
        public float AlertLevel => Blackboard.GetFloat(BBKeys.AlertLevel, 0f);
        
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
            _arrestDistanceSqr = _arrestDistance * _arrestDistance;
            
            // Note: Detection uses NPCRegistry<CopNPC> instead of tags to avoid Unity tag setup requirements
            
            Blackboard.SetVector3(BBKeys.HomePosition, _homePosition);
            Blackboard.SetFloat(BBKeys.AlertLevel, 0f);
            Blackboard.Set(BBKeys.CurrentState, "Patrol");
            
            // Initialize action timestamps
            Blackboard.SetFloat(BBKeys.LastChaseTime, -10f);
            Blackboard.SetFloat(BBKeys.LastInvestigateTime, -10f);
            Blackboard.SetFloat(BBKeys.LastPatrolTime, -10f);
            Blackboard.SetFloat(BBKeys.LastReturnTime, -10f);
            Blackboard.SetFloat(BBKeys.LastArrestTime, -10f);
            Blackboard.SetBool(BBKeys.RespondingToAlert, false);
            Blackboard.SetBool(BBKeys.CrimeInProgress, false);
            Blackboard.SetVector3(BBKeys.AlarmLocation, Vector3.zero);
            
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
            if (robber == null)
            {
                // Don't log non-robber targets - too spammy
                return;
            }
            
            Debug.Log($"<color=blue>[{name}]</color> <color=yellow>TARGET ACQUIRED: {target.name}</color> | CrimeInProgress: {Blackboard.GetBool(BBKeys.CrimeInProgress, false)}");
            
            // IMPORTANT: Only chase robbers if a crime is in progress (alarm triggered)
            // Before the alarm, cops just patrol and guard - they don't chase civilians
            if (!Blackboard.GetBool(BBKeys.CrimeInProgress, false))
            {
                // Cop sees robber but no crime yet - just note their presence
                Debug.Log($"<color=blue>[{name}]</color> <color=orange>No crime yet - ignoring robber</color>");
                return;
            }
            
            // Crime in progress - chase the robber!
            Debug.Log($"<color=blue>[{name}]</color> <color=green>CRIME IN PROGRESS - SETTING TARGET TO CHASE!</color>");
            _cachedTargetRobber = robber;
            Blackboard.Set(BBKeys.Target, target);
            Blackboard.SetVector3(BBKeys.InvestigatePosition, target.transform.position);
            IncreaseAlert(0.6f);
            
            // Broadcast to other cops
            CopAlertSystem.BroadcastRobberSighting(target.transform.position, target);
        }
        
        private void HandleTargetLost(GameObject target)
        {
            // Only log if it's a robber we were tracking
            var robber = target?.GetComponent<RobberNPC>();
            
            // Single lookup instead of Has + Get
            if (Blackboard.TryGet<GameObject>(BBKeys.Target, out var currentTarget) && currentTarget == target)
            {
                Debug.Log($"<color=blue>[{name}]</color> <color=red>LOST SIGHT OF ROBBER: {target?.name}</color>");
                Blackboard.Remove(BBKeys.Target);
                _cachedTargetRobber = null;
            }
            // Don't log losing sight of walls/buildings - too spammy
        }
        
        private void HandleSoundHeard(SoundEvent sound)
        {
            if (!Blackboard.Has(BBKeys.Target))
            {
                Blackboard.SetVector3(BBKeys.InvestigatePosition, sound.Position);
                Blackboard.SetInt(BBKeys.LastSoundType, (int)sound.Type);
                
                if (sound.Type >= SoundType.Alarm)
                {
                    // ALARM TRIGGERED - Crime is now in progress!
                    Blackboard.SetBool(BBKeys.CrimeInProgress, true);
                    Blackboard.SetVector3(BBKeys.AlarmLocation, sound.Position);
                    IncreaseAlert(_alarmAlertBoost);
                    
                    // Broadcast alarm to all cops so they ALL converge
                    CopAlertSystem.BroadcastRobberSighting(sound.Position, sound.Source);
                    
                    Debug.Log($"<color=blue>[{name}]</color> <color=red>*** ALARM HEARD! ***</color> Position: {sound.Position} | CrimeInProgress now TRUE");
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
            float current = Blackboard.GetFloat(BBKeys.AlertLevel, 0f);
            Blackboard.SetFloat(BBKeys.AlertLevel, Mathf.Clamp01(current + amount));
        }
        
        private void DecayAlert()
        {
            float current = Blackboard.GetFloat(BBKeys.AlertLevel, 0f);
            if (current > 0f)
            {
                Blackboard.SetFloat(BBKeys.AlertLevel, Mathf.Max(0f, current - _alertDecayRate * Time.deltaTime));
            }
        }
        
        private float _lastDebugLogTime;
        
        private void LateUpdate()
        {
            // Single lookup for target - fixes redundant Blackboard access
            if (Blackboard.TryGet<GameObject>(BBKeys.Target, out var target) && target != null)
            {
                // Has visible target - update position and alert
                Vector3 robberPosition = target.transform.position;
                Blackboard.SetVector3(BBKeys.InvestigatePosition, robberPosition);
                IncreaseAlert(_alertIncreaseRate * Time.deltaTime);
                
                // Log every 2 seconds to avoid spam
                if (Time.time - _lastDebugLogTime > 2f)
                {
                    _lastDebugLogTime = Time.time;
                    float dist = Vector3.Distance(transform.position, robberPosition);
                    Debug.Log($"<color=blue>[{name}]</color> <color=cyan>TRACKING TARGET</color> | State: {_cachedState} | Distance: {dist:F1}m | CanArrest: {Blackboard.GetBool(BBKeys.CanArrest, false)}");
                }
                
                // Broadcast robber sighting to all cops
                CopAlertSystem.BroadcastRobberSighting(robberPosition, target);
                
                // Check for arrest opportunity
                CheckArrestOpportunity(target);
            }
            else
            {
                // No target - decay alert over time
                DecayAlert();
                
                // Check if we should respond to shared alert
                UpdateSharedAlertResponse();
            }
        }
        
        private void UpdateSharedAlertResponse()
        {
            // Check if there's an active shared alert we should respond to
            if (CopAlertSystem.HasActiveAlert)
            {
                Blackboard.SetBool(BBKeys.RespondingToAlert, true);
                Blackboard.SetVector3(BBKeys.AlertPosition, CopAlertSystem.LastKnownRobberPosition);
                
                // IMPORTANT: If there's an active alert, a crime is in progress for ALL cops
                // This ensures cops who didn't directly hear the alarm can still chase the robber
                if (!Blackboard.GetBool(BBKeys.CrimeInProgress, false))
                {
                    Blackboard.SetBool(BBKeys.CrimeInProgress, true);
                    Blackboard.SetVector3(BBKeys.AlarmLocation, CopAlertSystem.LastKnownRobberPosition);
                }
                
                IncreaseAlert(0.3f * Time.deltaTime); // Stay alert while responding
            }
            else
            {
                Blackboard.SetBool(BBKeys.RespondingToAlert, false);
            }
        }
        
        private void CheckArrestOpportunity(GameObject target)
        {
            if (target == null) return;
            
            // Use sqrMagnitude to avoid sqrt
            float distanceSqr = (transform.position - target.transform.position).sqrMagnitude;
            float distance = Mathf.Sqrt(distanceSqr); // Only compute sqrt once for UI/considerations
            Blackboard.SetFloat(BBKeys.TargetDistance, distance);
            Blackboard.SetBool(BBKeys.CanArrest, distanceSqr <= _arrestDistanceSqr);
        }
        
        /// <summary>
        /// Attempts to arrest the current target.
        /// </summary>
        private void TryArrest()
        {
            if (!Blackboard.TryGet<GameObject>(BBKeys.Target, out var target) || target == null)
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
                // Use sqrMagnitude to avoid sqrt
                float distanceSqr = (transform.position - target.transform.position).sqrMagnitude;
                if (distanceSqr <= _arrestDistanceSqr)
                {
                    robber.OnArrested();
                    _arrestCount++;
                    Blackboard.Remove(BBKeys.Target);
                    Blackboard.SetBool(BBKeys.CanArrest, false);
                    _cachedTargetRobber = null;
                    
                    // Clear the shared alert for this specific robber
                    CopAlertSystem.ClearAlertForRobber(target);
                    
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
            var respondToAlertAction = CreateRespondToAlertAction();
            var respondToAlarmAction = CreateRespondToAlarmAction();
            var alarmInvestigateAction = CreateAlarmInvestigateAction();
            var soundInvestigateAction = CreateSoundInvestigateAction();
            var returnAction = CreateReturnAction();
            var patrolAction = CreatePatrolAction();
            
            return new UtilitySelector(
                arrestAction,
                chaseAction,
                respondToAlertAction,
                respondToAlarmAction,
                alarmInvestigateAction,
                soundInvestigateAction,
                returnAction,
                patrolAction
            );
        }
        
        private UtilityAction CreateArrestAction()
        {
            var arrestBehavior = new Sequence(
                new SetBlackboard(BBKeys.LastArrestTime, () => Time.time),
                new SetBlackboard(BBKeys.CurrentState, () => { _cachedState = "Arresting!"; return "Arresting!"; }),
                new Wait(0.5f, () => TryArrest())
            );
            arrestBehavior.Name = "ArrestBehavior";
            
            return new UtilityAction(
                "Arrest",
                arrestBehavior,
                _arrestWeight,
                // Must be able to arrest (very close to target)
                new BlackboardConsideration<bool>("CanArrest", BBKeys.CanArrest,
                    can => can ? 1f : 0f, false),
                // Must have a target
                new BlackboardConsideration<GameObject>("HasTarget", BBKeys.Target,
                    t => t != null ? 1f : 0f, null)
            );
        }
        
        private UtilityAction CreateChaseAction()
        {
            var chaseBehavior = new Sequence(
                new SetBlackboard(BBKeys.LastChaseTime, () => Time.time),
                new SetBlackboard(BBKeys.CurrentState, () => { _cachedState = "Chase!"; return "Chase!"; }),
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
                new BlackboardConsideration<GameObject>("HasTarget", BBKeys.Target,
                    t => t != null ? 1f : 0f, null),
                // Not close enough to arrest
                new BlackboardConsideration<bool>("CantArrestYet", BBKeys.CanArrest,
                    can => can ? 0f : 1f, false),
                // Higher score when target is close
                new DistanceConsideration(
                    "TargetDistance",
                    brain => GetTargetPositionForCheck(brain),
                    _maxChaseDistance,
                    true
                ),
                // Higher score when alert
                new BlackboardConsideration<float>("AlertForChase", BBKeys.AlertLevel,
                    a => 0.5f + a * 0.5f, 0f)
            );
        }
        
        private UtilityAction CreateRespondToAlertAction()
        {
            var respondBehavior = new Sequence(
                new SetBlackboard(BBKeys.CurrentState, () => { _cachedState = "Responding!"; return "Responding!"; }),
                new MoveTo(
                    () => Blackboard.GetVector3(BBKeys.AlertPosition, transform.position),
                    _arrivalDistance,
                    _chaseSpeed, // Move fast when responding to alert
                    4f
                )
            );
            respondBehavior.Name = "RespondToAlertBehavior";
            
            return new UtilityAction(
                "RespondToAlert",
                respondBehavior,
                _respondToAlertWeight,
                // Must be responding to an active alert
                new BlackboardConsideration<bool>("HasAlert", BBKeys.RespondingToAlert,
                    responding => responding ? 1f : 0f, false),
                // Must NOT have direct visual on target (otherwise Chase takes over)
                new BlackboardConsideration<GameObject>("NoDirectVisual", BBKeys.Target,
                    t => t == null ? 1f : 0f, null),
                // Higher score when closer to alert position (converge faster)
                new DistanceConsideration(
                    "AlertDistance",
                    brain => brain.Blackboard.GetVector3(BBKeys.AlertPosition, brain.transform.position),
                    30f,
                    true
                )
            );
        }
        
        private UtilityAction CreateRespondToAlarmAction()
        {
            // This action makes cops converge on the alarm location when a crime occurs
            var respondBehavior = new Sequence(
                new SetBlackboard(BBKeys.CurrentState, () => { _cachedState = "Responding!"; return "Responding!"; }),
                new MoveTo(
                    () => Blackboard.GetVector3(BBKeys.AlarmLocation, transform.position),
                    _arrivalDistance,
                    _chaseSpeed, // Move fast to alarm location
                    6f
                )
            );
            respondBehavior.Name = "RespondToAlarmBehavior";
            
            return new UtilityAction(
                "RespondToAlarm",
                respondBehavior,
                _alarmInvestigateWeight + 0.05f, // Slightly higher than investigate
                // Must have a crime in progress
                new BlackboardConsideration<bool>("CrimeActive", BBKeys.CrimeInProgress,
                    crime => crime ? 1f : 0f, false),
                // Must NOT have direct visual on target (otherwise Chase takes over)
                new BlackboardConsideration<GameObject>("NoDirectVisual", BBKeys.Target,
                    t => t == null ? 1f : 0f, null),
                // Must NOT already be responding to a team alert (avoid duplicate)
                new BlackboardConsideration<bool>("NotAlreadyResponding", BBKeys.RespondingToAlert,
                    responding => responding ? 0.3f : 1f, false),
                // Higher score when closer to alarm (converge faster)
                new DistanceConsideration(
                    "AlarmDistance",
                    brain => brain.Blackboard.GetVector3(BBKeys.AlarmLocation, brain.transform.position),
                    40f,
                    true
                )
            );
        }
        
        private UtilityAction CreateAlarmInvestigateAction()
        {
            var investigateBehavior = new Sequence(
                new SetBlackboard(BBKeys.LastInvestigateTime, () => Time.time),
                new SetBlackboard(BBKeys.CurrentState, () => { _cachedState = "Investigate-Alarm"; return "Investigate-Alarm"; }),
                new MoveTo(
                    () => Blackboard.GetVector3(BBKeys.InvestigatePosition, Vector3.zero),
                    _arrivalDistance,
                    _investigateSpeed
                ),
                new Wait(_investigateTime * 0.5f),
                new ClearBlackboardKey(BBKeys.InvestigatePosition),
                new ClearBlackboardKey(BBKeys.LastSoundType)
            );
            investigateBehavior.Name = "AlarmInvestigateBehavior";
            
            return new UtilityAction(
                "InvestigateAlarm",
                investigateBehavior,
                _alarmInvestigateWeight,
                // Must have heard an alarm
                new HasHeardSoundConsideration("HeardAlarm", SoundType.Alarm),
                // No visible target
                new BlackboardConsideration<GameObject>("NoVisibleTarget", BBKeys.Target,
                    t => t == null ? 1f : 0f, null),
                // Higher score when alert
                new BlackboardConsideration<float>("AlertForAlarm", BBKeys.AlertLevel,
                    a => 0.3f + a * 0.7f, 0f),
                // Distance consideration
                new SoundDistanceConsideration("AlarmDistance", 50f, true),
                // Cooldown
                new TimeConsideration("InvestigateCooldown", BBKeys.LastInvestigateTime, 2f)
            );
        }
        
        private UtilityAction CreateSoundInvestigateAction()
        {
            var investigateBehavior = new Sequence(
                new SetBlackboard(BBKeys.LastInvestigateTime, () => Time.time),
                new SetBlackboard(BBKeys.CurrentState, () => { _cachedState = "Investigate"; return "Investigate"; }),
                new MoveTo(
                    () => Blackboard.GetVector3(BBKeys.InvestigatePosition, Vector3.zero),
                    _arrivalDistance,
                    _investigateSpeed * 0.8f
                ),
                new Wait(_investigateTime),
                new ClearBlackboardKey(BBKeys.InvestigatePosition),
                new ClearBlackboardKey(BBKeys.LastSoundType)
            );
            investigateBehavior.Name = "SoundInvestigateBehavior";
            
            return new UtilityAction(
                "InvestigateSound",
                investigateBehavior,
                _soundInvestigateWeight,
                // Must have heard at least a footstep
                new HasHeardSoundConsideration("HeardSound", SoundType.Footstep),
                // No visible target
                new BlackboardConsideration<GameObject>("NoVisibleTarget", BBKeys.Target,
                    t => t == null ? 1f : 0f, null),
                // Moderate alert needed
                new BlackboardConsideration<float>("AlertForSound", BBKeys.AlertLevel,
                    a => a > 0.1f ? 0.5f + a * 0.5f : 0.3f, 0f),
                // Cooldown
                new TimeConsideration("InvestigateCooldown", BBKeys.LastInvestigateTime, 3f)
            );
        }
        
        private UtilityAction CreateReturnAction()
        {
            var returnBehavior = new Sequence(
                new SetBlackboard(BBKeys.LastReturnTime, () => Time.time),
                new SetBlackboard(BBKeys.CurrentState, () => { _cachedState = "Return"; return "Return"; }),
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
                // No target
                new BlackboardConsideration<GameObject>("NoTarget", BBKeys.Target,
                    t => t == null ? 1f : 0f, null),
                // No pending investigation
                new BlackboardConsideration<Vector3>("NoInvestigation", BBKeys.InvestigatePosition,
                    pos => pos == Vector3.zero ? 1f : 0.3f, Vector3.zero),
                // Higher score when far from home
                new DistanceConsideration(
                    "DistanceFromHome",
                    brain => brain.Blackboard.GetVector3(BBKeys.HomePosition, brain.transform.position),
                    15f,
                    false
                ),
                // Cooldown
                new TimeConsideration("ReturnCooldown", BBKeys.LastReturnTime, 5f)
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
                // Always available as baseline
                new ConstantConsideration(0.8f),
                // Less likely when alert
                new BlackboardConsideration<float>("LowAlert", BBKeys.AlertLevel,
                    a => 1f - a * 0.5f, 0f),
                // Cooldown
                new TimeConsideration("PatrolCooldown", BBKeys.LastPatrolTime, 2f)
            );
        }
        
        private Vector3 GetTargetPosition()
        {
            if (Blackboard.TryGet<GameObject>(BBKeys.Target, out var target) && target != null)
            {
                return target.transform.position;
            }
            return Blackboard.GetVector3(BBKeys.InvestigatePosition, transform.position);
        }
        
        private static Vector3 GetTargetPositionForCheck(NPCBrainController brain)
        {
            if (brain.Blackboard.TryGet<GameObject>(BBKeys.Target, out var target) && target != null)
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
