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
        
        [Header("Alert Settings")]
        [SerializeField] private float _alertDecayRate = 0.1f;
        [SerializeField] private float _alertIncreaseRate = 0.5f;
        [SerializeField] private float _alarmAlertBoost = 0.9f;
        [SerializeField] private float _footstepAlertBoost = 0.5f;  // Increased from 0.2 - cops now react more to footsteps
        
        [Header("Pursuit Persistence")]
        [SerializeField] private float _pursuitPredictionMultiplier = 1.5f;  // How far ahead to predict robber position (passed to CopAlertSystem)
        
        [Header("Utility Weights")]
        [SerializeField] private float _arrestWeight = 2.0f;  // Highest priority - arrest when close
        [SerializeField] private float _chaseWeight = 1.8f;   // Very high - always chase when target visible
        [SerializeField] private float _pursueLastKnownWeight = 1.9f;  // Very high - continue pursuit after losing sight
        [SerializeField] private float _trackFootstepsWeight = 1.0f;  // High - follow footsteps aggressively
        [SerializeField] private float _respondToAlertWeight = 0.95f;
        [SerializeField] private float _alarmInvestigateWeight = 0.9f;
        [SerializeField] private float _soundInvestigateWeight = 0.5f;
        [SerializeField] private float _searchWeight = 0.85f;  // Active searching when crime in progress but no specific lead
        [SerializeField] private float _returnWeight = 0.4f;
        [SerializeField] private float _patrolWeight = 0.3f;
        
        private Vector3 _homePosition;
        private int _arrestCount;
        private RobberNPC _cachedTargetRobber;
        private float _arrestDistanceSqr;
        private string _cachedState = "Patrol";
        private float _lastDebugLogTime;
        private float _lastChaseLogTime;
        private float _lastFootstepLogTime;
        private float _lastTrackLogTime;
        private float _lastPursueLogTime;
        private float _lastActionLogTime;
        private Vector3 _lastTrackedRobberPosition;  // For calculating robber velocity/direction
        private Vector3 _cachedSearchPosition;  // Cached search position to avoid jittery movement
        private float _lastSearchPositionTime;  // When the search position was last calculated
        
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
            
            // Initialize pursuit persistence
            Blackboard.SetFloat(BBKeys.TimeLostSight, -10f);
            Blackboard.SetVector3(BBKeys.LastKnownRobberPosition, Vector3.zero);
            Blackboard.SetVector3(BBKeys.LastKnownRobberDirection, Vector3.zero);
            
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
            _lastTrackedRobberPosition = target.transform.position;  // Reset for accurate direction tracking
            Blackboard.Set(BBKeys.Target, target);
            Blackboard.SetVector3(BBKeys.InvestigatePosition, target.transform.position);
            IncreaseAlert(0.6f);
            
            // Broadcast to other cops
            CopAlertSystem.BroadcastRobberSighting(target.transform.position, target);
        }
        
        private void HandleTargetLost(GameObject target)
        {
            // Single lookup instead of Has + Get
            if (Blackboard.TryGet<GameObject>(BBKeys.Target, out var currentTarget) && currentTarget == target)
            {
                // PURSUIT PERSISTENCE: Save last known position and direction before clearing target
                Vector3 lastPosition = target.transform.position;
                Vector3 lastDirection = Blackboard.GetVector3(BBKeys.LastKnownRobberDirection, Vector3.zero);
                
                // FALLBACK: If stored direction is zero (robber wasn't moving much), calculate alternatives
                if (lastDirection.sqrMagnitude < 0.01f)
                {
                    // Try position delta if we have previous position
                    if (_lastTrackedRobberPosition != Vector3.zero)
                    {
                        Vector3 positionDelta = lastPosition - _lastTrackedRobberPosition;
                        if (positionDelta.sqrMagnitude > 0.1f)
                        {
                            lastDirection = positionDelta.normalized;
                            Debug.Log($"<color=blue>[{name}]</color> <color=yellow>Using fallback direction from position delta: {lastDirection}</color>");
                        }
                    }
                    
                    // ULTIMATE FALLBACK: If still no direction, use direction from cop to robber
                    // This ensures we ALWAYS have a valid direction for pursuit!
                    if (lastDirection.sqrMagnitude < 0.01f)
                    {
                        lastDirection = (lastPosition - transform.position).normalized;
                        Debug.Log($"<color=blue>[{name}]</color> <color=yellow>Using ultimate fallback - direction from cop to robber: {lastDirection}</color>");
                    }
                }
                
                // Store pursuit persistence data locally (kept for debugging/UI display even though
                // coordinated pursuit uses CopAlertSystem shared data)
                Blackboard.SetVector3(BBKeys.LastKnownRobberPosition, lastPosition);
                Blackboard.SetVector3(BBKeys.LastKnownRobberDirection, lastDirection);
                Blackboard.SetFloat(BBKeys.TimeLostSight, Time.time);
                
                // COORDINATED PURSUIT: Broadcast lost sight to ALL cops so they all pursue together!
                CopAlertSystem.BroadcastLostSight(lastPosition, lastDirection);
                
                Debug.Log($"<color=blue>[{name}]</color> <color=red>LOST SIGHT OF ROBBER: {target?.name}</color> | Last pos: {lastPosition} | Direction: {lastDirection} | ALL COPS pursuing for {CopAlertSystem.PursuitValidDuration}s");
                
                Blackboard.Remove(BBKeys.Target);
                _cachedTargetRobber = null;
                _lastTrackedRobberPosition = Vector3.zero;  // Reset for next chase
            }
            // Don't log losing sight of walls/buildings - too spammy
        }
        
        private void HandleSoundHeard(SoundEvent sound)
        {
            // Always track footstep position for TrackFootsteps action, even if we have a visual target
            if (sound.Type == SoundType.Footstep)
            {
                Blackboard.SetVector3(BBKeys.LastFootstepPosition, sound.Position);
                Blackboard.SetFloat(BBKeys.LastFootstepTime, Time.time);
                
                // Only log if crime is in progress and we don't have visual (avoid spam)
                if (Blackboard.GetBool(BBKeys.CrimeInProgress, false) && !Blackboard.Has(BBKeys.Target))
                {
                    if (Time.time - _lastFootstepLogTime > 2f)
                    {
                        _lastFootstepLogTime = Time.time;
                        float dist = Vector3.Distance(transform.position, sound.Position);
                        Debug.Log($"<color=blue>[{name}]</color> <color=yellow>FOOTSTEPS HEARD!</color> Distance: {dist:F1}m | Position: {sound.Position}");
                    }
                }
            }
            
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
        
        private void LateUpdate()
        {
            // Single lookup for target - fixes redundant Blackboard access
            if (Blackboard.TryGet<GameObject>(BBKeys.Target, out var target) && target != null)
            {
                // Has visible target - update position and alert
                Vector3 robberPosition = target.transform.position;
                Blackboard.SetVector3(BBKeys.InvestigatePosition, robberPosition);
                IncreaseAlert(_alertIncreaseRate * Time.deltaTime);
                
                // Track robber direction for pursuit persistence
                // Calculate velocity based on position change
                if (_lastTrackedRobberPosition != Vector3.zero)
                {
                    Vector3 movement = robberPosition - _lastTrackedRobberPosition;
                    if (movement.sqrMagnitude > 0.01f)  // Only update if significant movement
                    {
                        Vector3 direction = movement.normalized;
                        Blackboard.SetVector3(BBKeys.LastKnownRobberDirection, direction);
                        
                        // Share direction with all cops via CopAlertSystem
                        CopAlertSystem.UpdateRobberDirection(direction);
                    }
                }
                _lastTrackedRobberPosition = robberPosition;
                
                // Also continuously update last known position while tracking
                Blackboard.SetVector3(BBKeys.LastKnownRobberPosition, robberPosition);
                
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
                
                // VERBOSE LOGGING: Show what this cop is doing when no visual target
                if (Time.time - _lastActionLogTime > 3f)
                {
                    _lastActionLogTime = Time.time;
                    LogCurrentActionStatus();
                }
            }
        }
        
        /// <summary>
        /// Logs detailed status about what this cop is doing and why.
        /// </summary>
        private void LogCurrentActionStatus()
        {
            bool crimeActive = Blackboard.GetBool(BBKeys.CrimeInProgress, false);
            bool hasActivePursuit = CopAlertSystem.HasActivePursuit;
            bool hasActiveAlert = CopAlertSystem.HasActiveAlert;
            Vector3 pursuitDirection = CopAlertSystem.LastKnownRobberDirection;
            bool hasValidDirection = pursuitDirection.sqrMagnitude > 0.01f;
            
            string pursuitStatus = "N/A";
            if (hasActivePursuit)
            {
                float timeSinceLostSight = CopAlertSystem.TimeSinceLostSight;
                if (hasValidDirection)
                    pursuitStatus = $"ACTIVE ({timeSinceLostSight:F1}s ago, dir: {pursuitDirection})";
                else
                    pursuitStatus = $"BLOCKED (no direction!)";
            }
            else if (crimeActive && CopAlertSystem.TimeLostSight > 0f)
            {
                // Only show expired if there was a previous pursuit
                pursuitStatus = "EXPIRED";
            }
            
            // Log warning if we're in Patrol/Return but crime is active - this shouldn't happen!
            if (crimeActive && (_cachedState == "Patrol" || _cachedState == "Return"))
            {
                Debug.LogWarning($"<color=red>[{name}] BUG: {_cachedState} action active during crime! Crime={crimeActive}</color>");
            }
            
            Debug.Log($"<color=blue>[{name}]</color> ACTION: <color=white>{_cachedState}</color> | Crime: {crimeActive} | Alert: {hasActiveAlert} | Pursuit: {pursuitStatus} | AlertLevel: {AlertLevel:F2}");
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
            var pursueLastKnownAction = CreatePursueLastKnownAction();  // Continue pursuit after losing sight
            var trackFootstepsAction = CreateTrackFootstepsAction();  // Aggressively follow footsteps
            var respondToAlertAction = CreateRespondToAlertAction();
            var respondToAlarmAction = CreateRespondToAlarmAction();
            var alarmInvestigateAction = CreateAlarmInvestigateAction();
            var soundInvestigateAction = CreateSoundInvestigateAction();
            var searchAction = CreateSearchAction();  // Active searching when crime in progress
            var returnAction = CreateReturnAction();
            var patrolAction = CreatePatrolAction();
            
            var selector = new UtilitySelector(
                arrestAction,
                chaseAction,
                pursueLastKnownAction,  // High priority - continue pursuit after losing sight
                trackFootstepsAction,   // Follow footsteps when no visual
                respondToAlertAction,
                respondToAlarmAction,
                alarmInvestigateAction,
                soundInvestigateAction,
                searchAction,           // Active searching when crime in progress but no specific lead
                returnAction,
                patrolAction
            );
            
            // Configure for faster reaction to high-priority events like target acquisition
            selector.InterruptCheckInterval = 0.05f;  // Check every 50ms for cops
            selector.InterruptThreshold = 0.2f;       // Lower threshold to allow easier interruption
            
            return selector;
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
                new SetBlackboard(BBKeys.CurrentState, () => { 
                    _cachedState = "Chase!";
                    // Throttle chase log to avoid spam (only log every 2 seconds)
                    if (Time.time - _lastChaseLogTime > 2f)
                    {
                        _lastChaseLogTime = Time.time;
                        Debug.Log($"<color=blue>[{name}]</color> <color=lime>*** CHASING! ***</color>");
                    }
                    return "Chase!"; 
                }),
                new MoveTo(
                    () => GetTargetPosition(),
                    _arrestDistance * 0.8f, // Get very close for arrest
                    _chaseSpeed,
                    1.5f // Short timeout to re-evaluate, but long enough to avoid jitter
                )
            );
            chaseBehavior.Name = "ChaseBehavior";
            
            return new UtilityAction(
                "Chase",
                chaseBehavior,
                _chaseWeight,
                // Must have a visible target - this is the ONLY hard requirement
                // Score 1.0 if target exists, 0.0 if not
                new BlackboardConsideration<GameObject>("HasTarget", BBKeys.Target,
                    t => t != null ? 1f : 0f, null),
                // Not close enough to arrest (if close, Arrest action takes over)
                new BlackboardConsideration<bool>("CantArrestYet", BBKeys.CanArrest,
                    can => can ? 0f : 1f, false)
                // REMOVED: Distance and Alert considerations that were reducing the score
                // Chase should ALWAYS win when there's a target to chase
            );
        }
        
        private UtilityAction CreatePursueLastKnownAction()
        {
            // This action continues pursuit in the last known direction after losing sight
            // ALL cops pursue together using shared intel from CopAlertSystem
            var pursueBehavior = new Sequence(
                new SetBlackboard(BBKeys.CurrentState, () => { 
                    _cachedState = "Pursuing!";
                    if (Time.time - _lastPursueLogTime > 2f)
                    {
                        _lastPursueLogTime = Time.time;
                        Vector3 predictedPos = GetPredictedRobberPosition();
                        Debug.Log($"<color=blue>[{name}]</color> <color=magenta>*** COORDINATED PURSUIT! ***</color> Time since lost: {CopAlertSystem.TimeSinceLostSight:F1}s | Predicted pos: {predictedPos} | Direction: {CopAlertSystem.LastKnownRobberDirection}");
                    }
                    return "Pursuing!"; 
                }),
                new MoveTo(
                    () => GetPredictedRobberPosition(),
                    _arrivalDistance,
                    _chaseSpeed,  // Move at chase speed - this is active pursuit!
                    1.0f  // Short timeout to re-evaluate frequently
                )
            );
            pursueBehavior.Name = "PursueLastKnownBehavior";
            
            return new UtilityAction(
                "PursueLastKnown",
                pursueBehavior,
                _pursueLastKnownWeight,
                // Must have crime in progress
                new FunctionalConsideration("CrimeActive",
                    brain => brain.Blackboard.GetBool(BBKeys.CrimeInProgress, false) ? 1f : 0f),
                // Must NOT have direct visual on target (otherwise Chase takes over)
                new FunctionalConsideration("NoDirectVisual",
                    brain => {
                        bool hasTarget = brain.Blackboard.TryGet<GameObject>(BBKeys.Target, out var t) && t != null;
                        return hasTarget ? 0f : 1f;
                    }),
                // Must have active coordinated pursuit from CopAlertSystem (ANY cop lost sight recently)
                new FunctionalConsideration("HasActivePursuit",
                    _ => {
                        if (!CopAlertSystem.HasActivePursuit)
                        {
                            // Debug only occasionally to avoid spam
                            return 0f;
                        }
                        // High score throughout pursuit - slight decay but stays above 0.5
                        float timeSinceLost = CopAlertSystem.TimeSinceLostSight;
                        float decay = 1f - (timeSinceLost / CopAlertSystem.PursuitValidDuration) * 0.5f;
                        return Mathf.Clamp01(decay);  // Decays from 1.0 to 0.5 over 5 seconds
                    }),
                // Direction consideration - with ultimate fallbacks, always returns positive score during pursuit
                // Note: HasActivePursuit is already checked by the previous consideration, so no need to check again here
                new FunctionalConsideration("HasPursuitData",
                    _ => {
                        // Check if we have good direction data
                        if (CopAlertSystem.LastKnownRobberDirection.sqrMagnitude > 0.01f) return 1f;
                        
                        // Fallback: pursue to last known position even without direction
                        if (CopAlertSystem.LastKnownRobberPosition != Vector3.zero) return 0.9f;
                        
                        // Ultimate fallback: still pursue with lower confidence
                        // Use NPCBrainDebug for consistency with rest of codebase
                        NPCBrainDebug.Log(NPCBrainDebug.Category.General, 
                            $"[{name}] PursueLastKnown: No direction or position! This shouldn't happen.", null);
                        return 0.5f;  // Still positive so pursuit can happen
                    })
            );
        }
        
        /// <summary>
        /// Gets the predicted position of the robber based on SHARED intel from CopAlertSystem.
        /// This allows ALL cops to pursue in the same direction as a coordinated team.
        /// </summary>
        private Vector3 GetPredictedRobberPosition()
        {
            // Use shared intel from CopAlertSystem for coordinated team pursuit
            return CopAlertSystem.GetPredictedRobberPosition(_pursuitPredictionMultiplier);
        }
        
        private UtilityAction CreateTrackFootstepsAction()
        {
            // This action allows cops to aggressively follow footstep sounds even without visual contact
            var trackBehavior = new Sequence(
                new SetBlackboard(BBKeys.CurrentState, () => { 
                    _cachedState = "Tracking!";
                    if (Time.time - _lastTrackLogTime > 2f)
                    {
                        _lastTrackLogTime = Time.time;
                        Vector3 footstepPos = Blackboard.GetVector3(BBKeys.LastFootstepPosition, transform.position);
                        float dist = Vector3.Distance(transform.position, footstepPos);
                        Debug.Log($"<color=blue>[{name}]</color> <color=orange>*** TRACKING FOOTSTEPS! ***</color> Distance: {dist:F1}m");
                    }
                    return "Tracking!"; 
                }),
                new MoveTo(
                    () => Blackboard.GetVector3(BBKeys.LastFootstepPosition, transform.position),
                    _arrivalDistance,
                    _chaseSpeed,  // Move fast - this is active pursuit!
                    1.0f  // Short timeout to react quickly to new footsteps
                )
            );
            trackBehavior.Name = "TrackFootstepsBehavior";
            
            return new UtilityAction(
                "TrackFootsteps",
                trackBehavior,
                _trackFootstepsWeight,
                // Must have crime in progress - don't chase footsteps before the alarm
                new BlackboardConsideration<bool>("CrimeActive", BBKeys.CrimeInProgress,
                    crime => crime ? 1f : 0f, false),
                // Must NOT have direct visual on target (otherwise Chase takes over)
                new BlackboardConsideration<GameObject>("NoDirectVisual", BBKeys.Target,
                    t => t == null ? 1f : 0f, null),
                // Must have heard recent footsteps (within 3 seconds)
                new FunctionalConsideration("RecentFootsteps",
                    brain => {
                        float lastTime = brain.Blackboard.GetFloat(BBKeys.LastFootstepTime, -10f);
                        float timeSince = Time.time - lastTime;
                        // Score 1.0 if heard in last second, decays to 0 over 3 seconds
                        return timeSince < 3f ? Mathf.Clamp01(1f - (timeSince / 3f)) : 0f;
                    }),
                // Higher score when closer to footstep position
                new DistanceConsideration(
                    "FootstepDistance",
                    brain => brain.Blackboard.GetVector3(BBKeys.LastFootstepPosition, brain.transform.position),
                    25f,
                    true  // Invert - closer = higher score
                )
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
                    _patrolSpeed,
                    5f  // Add timeout so return can be interrupted
                )
            );
            returnBehavior.Name = "ReturnBehavior";
            
            return new UtilityAction(
                "Return",
                returnBehavior,
                _returnWeight,
                // IMPORTANT: Don't return to home base when crime is in progress!
                // Cops should be actively searching, not casually going home
                new FunctionalConsideration("NoCrimeInProgress",
                    brain => {
                        bool crime = brain.Blackboard.GetBool(BBKeys.CrimeInProgress, false);
                        return crime ? 0f : 1f;
                    }),
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
        
        private UtilityAction CreateSearchAction()
        {
            // This action makes cops actively search when crime is in progress but there's no specific lead
            // (no visual target, no pursuit, no footsteps to track)
            var searchBehavior = new Sequence(
                new SetBlackboard(BBKeys.CurrentState, () => { _cachedState = "Searching!"; return "Searching!"; }),
                new MoveTo(
                    () => GetSearchPosition(),
                    _arrivalDistance,
                    _investigateSpeed,  // Move at investigation speed - actively searching
                    3f  // Re-evaluate every 3 seconds to pick new search targets
                )
            );
            searchBehavior.Name = "SearchBehavior";
            
            return new UtilityAction(
                "Search",
                searchBehavior,
                _searchWeight,
                // MUST have crime in progress - this is the active search behavior
                new FunctionalConsideration("CrimeActive",
                    brain => brain.Blackboard.GetBool(BBKeys.CrimeInProgress, false) ? 1f : 0f),
                // Must NOT have direct visual on target (otherwise Chase takes over)
                new FunctionalConsideration("NoDirectVisual",
                    brain => (brain.Blackboard.TryGet<GameObject>(BBKeys.Target, out var t) && t != null) ? 0f : 1f),
                // Much less priority when there's an active pursuit - cops should be pursuing, not searching!
                new FunctionalConsideration("NoActivePursuit",
                    _ => CopAlertSystem.HasActivePursuit ? 0.05f : 1f),
                // Less priority when there are recent footsteps to track
                new FunctionalConsideration("NoRecentFootsteps",
                    brain => {
                        float lastTime = brain.Blackboard.GetFloat(BBKeys.LastFootstepTime, -10f);
                        float timeSince = Time.time - lastTime;
                        return timeSince > 3f ? 1f : 0.3f;  // Only high score if no recent footsteps
                    })
            );
        }
        
        /// <summary>
        /// Gets a position to search - moves toward the last known alarm location with cop-specific offsets.
        /// Uses cached position to avoid jittery movement, regenerating every 5 seconds or when arrived.
        /// </summary>
        private Vector3 GetSearchPosition()
        {
            // Check if we need to regenerate the search position
            // Regenerate if: never set, or 5+ seconds old, or we're close to current position
            bool needNewPosition = _cachedSearchPosition == Vector3.zero ||
                                   Time.time - _lastSearchPositionTime > 5f ||
                                   Vector3.Distance(transform.position, _cachedSearchPosition) < _arrivalDistance * 2f;
            
            if (needNewPosition)
            {
                _cachedSearchPosition = CalculateSearchPosition();
                _lastSearchPositionTime = Time.time;
            }
            
            return _cachedSearchPosition;
        }
        
        /// <summary>
        /// Calculates a new search position based on cop index for better coverage.
        /// </summary>
        private Vector3 CalculateSearchPosition()
        {
            Vector3 alarmLocation = Blackboard.GetVector3(BBKeys.AlarmLocation, Vector3.zero);
            if (alarmLocation == Vector3.zero)
            {
                // Fallback: move to a random waypoint
                return GetCurrentWaypoint();
            }
            
            // Use cop index to spread out - each cop searches a different quadrant
            var instances = NPCRegistry<CopNPC>.Instances;
            int copIndex = 0;
            for (int i = 0; i < instances.Count; i++)
            {
                if (instances[i] == this)
                {
                    copIndex = i;
                    break;
                }
            }
            int copCount = Mathf.Max(1, NPCRegistry<CopNPC>.Instances.Count);
            
            // Calculate angle based on cop index (spread evenly around the alarm location)
            float baseAngle = (copIndex * 360f / copCount) * Mathf.Deg2Rad;
            // Add some randomness to the angle (±30 degrees)
            float angleOffset = UnityEngine.Random.Range(-0.5f, 0.5f);
            float angle = baseAngle + angleOffset;
            
            // Distance from alarm location (8-15m radius)
            float distance = UnityEngine.Random.Range(8f, 15f);
            
            Vector3 offset = new Vector3(
                Mathf.Cos(angle) * distance,
                0f,
                Mathf.Sin(angle) * distance
            );
            
            return alarmLocation + offset;
        }
        
        private UtilityAction CreatePatrolAction()
        {
            var patrolBehavior = new Sequence(
                new SetBlackboard(BBKeys.LastPatrolTime, () => Time.time),
                new SetBlackboard(BBKeys.CurrentState, () => { _cachedState = "Patrol"; return "Patrol"; }),
                new MoveTo(
                    () => GetCurrentWaypoint(),
                    _arrivalDistance,
                    _patrolSpeed,
                    5f  // Add timeout so patrol can be interrupted
                ),
                new Wait(_waypointWaitTime),
                new AdvanceWaypoint()
            );
            patrolBehavior.Name = "PatrolBehavior";
            
            return new UtilityAction(
                "Patrol",
                patrolBehavior,
                _patrolWeight,
                // IMPORTANT: Don't casually patrol when crime is in progress!
                // Cops should be actively searching, not casually patrolling
                new FunctionalConsideration("NoCrimeInProgress",
                    brain => {
                        bool crime = brain.Blackboard.GetBool(BBKeys.CrimeInProgress, false);
                        return crime ? 0f : 1f;
                    }),
                // Always available as baseline (when no crime)
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
