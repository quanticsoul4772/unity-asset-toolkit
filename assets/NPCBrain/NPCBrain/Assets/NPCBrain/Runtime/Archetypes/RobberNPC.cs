using System.Collections.Generic;
using UnityEngine;
using NPCBrain;
using NPCBrain.BehaviorTree;
using NPCBrain.BehaviorTree.Composites;
using NPCBrain.BehaviorTree.Actions;
using NPCBrain.Components;
using NPCBrain.Perception;
using NPCBrain.UtilityAI;

namespace NPCBrain.Archetypes
{
    /// <summary>
    /// Robber NPC archetype that uses Utility AI for heist behaviors.
    /// Steals loot, evades cops, and escapes with the goods.
    /// </summary>
    /// <remarks>
    /// <para>Utility-scored behaviors:</para>
    /// <list type="bullet">
    ///   <item><description>StealLoot - Near loot, no cops visible, high priority</description></item>
    ///   <item><description>Flee - Cop visible or chasing, emergency escape</description></item>
    ///   <item><description>CarryToEscape - Has loot, head to escape zone</description></item>
    ///   <item><description>Hide - Being pursued, find cover</description></item>
    ///   <item><description>Sneak - Cop nearby but not visible, move carefully</description></item>
    ///   <item><description>Scout - Look for loot opportunities</description></item>
    /// </list>
    /// </remarks>
    public class RobberNPC : NPCBrainController, INPCArchetype
    {
        /// <summary>All active RobberNPC instances.</summary>
        public static IReadOnlyList<RobberNPC> AllInstances => NPCRegistry<RobberNPC>.Instances;

        [Header("Robber Settings")]
        [SerializeField] private float _normalSpeed = 4f;
        [SerializeField] private float _fleeSpeed = 7f;
        [SerializeField] private float _sneakSpeed = 2f;
        [SerializeField] private float _arrivalDistance = 1f;
        [SerializeField] private float _stealTime = 2f;
        
        [Header("Detection Settings")]
        [SerializeField] private float _copDetectionRange = 15f;
        [SerializeField] private float _lootDetectionRange = 100f;  // Large range - robber should find loot across the map
        // Note: Cop detection now uses NPCRegistry<CopNPC> instead of tag-based detection
        
        [Header("Utility Weights")]
        [SerializeField] private float _fleeWeight = 1.0f;
        [SerializeField] private float _carryToEscapeWeight = 1.5f;  // High priority - escape with loot!
        [SerializeField] private float _stealWeight = 0.85f;
        [SerializeField] private float _hideWeight = 0.7f;
        [SerializeField] private float _sneakWeight = 0.5f;
        [SerializeField] private float _scoutWeight = 0.35f;  // Slightly higher to beat Hide's typical score
        
        private LootPoint _targetLoot;
        private EscapeZone _escapeZone;
        private CoverPoint _targetCover;
        private int _carriedLootValue;
        private bool _isCarryingLoot;
        private Vector3 _homePosition;
        private List<CoverPoint> _knownCoverPoints = new List<CoverPoint>(16);
        private List<LootPoint> _knownLootPoints = new List<LootPoint>(16);
        private float _lastCopSightTime;
        private bool _hasEscaped;
        private string _cachedState = "Scout";
        private float _lootDetectionRangeSqr;
        private float _copDetectionRangeSqr;
        private bool _hasLootAvailable;  // Cached for performance
        private float _cachedLootDistance = 999f;  // Cached distance to nearest loot
        private int _tickCount;  // Debug counter
        private Vector3 _cachedCoverPosition;  // Cached to prevent moving target
        private float _coverPositionCacheTime;  // When cover was last cached
        
        [Header("Sound Settings")]
        [SerializeField] private float _footstepInterval = 0.4f;
        [SerializeField] private float _footstepVolume = 0.6f;
        [SerializeField] private float _sneakFootstepVolume = 0.2f;
        private float _lastFootstepTime;
        private Vector3 _lastPosition;
        
        [Header("Performance")]
        [SerializeField] private int _maxCopRaycastsPerFrame = 2;
        private int _raycastCount;

        
        /// <summary>Current behavior state for UI display.</summary>
        public string CurrentState => _cachedState;
        
        /// <summary>The robber's role in the scenario.</summary>
        public string Role => "Bank Robber";
        
        /// <summary>The robber's primary objective.</summary>
        public string Goal => "Steal money and escape without getting caught";
        
        /// <summary>Dynamic explanation of current behavior.</summary>
        public string CurrentReason
        {
            get
            {
                if (_cachedState == "Flee!") return "Cop spotted - need to escape!";
                if (_cachedState == "Escaping") return _isCarryingLoot ? $"Got ${_carriedLootValue} - heading to escape zone!" : "Making my getaway!";
                if (_cachedState == "Stealing") return "Going for the loot - no fear!";
                if (_cachedState == "Hiding")
                {
                    return FearLevel > 0.5f ? "Too dangerous - laying low" : "Staying out of sight";
                }
                if (_cachedState == "Sneaking") return _isCarryingLoot ? "Moving carefully with the goods" : "Approaching target quietly";
                if (_cachedState == "Scouting") return _isCarryingLoot ? "Looking for the exit..." : "Boldly seeking the loot!";
                if (_cachedState == "Arrested!") return "Busted!";
                if (_cachedState == "Escaped!") return "Got away with the loot!";
                if (_cachedState == "Time's Up!") return "Ran out of time!";
                
                // Add urgency context to default reason
                float urgency = Urgency;
                if (urgency > 0.7f) return "No time left - MOVE!";
                if (urgency > 0.4f) return "Running low on time...";
                return "Planning the heist...";
            }
        }
        
        /// <summary>Value of loot being carried.</summary>
        public int CarriedLootValue => _carriedLootValue;
        
        /// <summary>Whether this robber is carrying loot.</summary>
        public bool IsCarryingLoot => _isCarryingLoot;
        
        /// <summary>Whether this robber has escaped.</summary>
        public bool HasEscaped => _hasEscaped;
        
        /// <summary>Whether this NPC is still active.</summary>
        public bool IsActive => !_hasEscaped && gameObject.activeSelf;
        
        /// <summary>Time since last saw a cop.</summary>
        public float TimeSinceLastCopSight => Time.time - _lastCopSightTime;
        
        /// <summary>Whether a cop is currently visible.</summary>
        public bool CanSeeCop => Blackboard.GetBool(BBKeys.CanSeeCop, false);
        
        /// <summary>Current fear level (0-1) based on cop proximity.</summary>
        public float FearLevel => Blackboard.GetFloat(BBKeys.FearLevel, 0f);
        
        /// <summary>
        /// Urgency level (0-1) based on time remaining. Higher = more urgent.
        /// At 0.0 = plenty of time, at 1.0 = no time left!
        /// </summary>
        public float Urgency => CalculateUrgency();
        
        /// <summary>
        /// Calculates urgency based on time remaining in the heist.
        /// Uses exponential curve so urgency increases dramatically as time runs out.
        /// </summary>
        private float CalculateUrgency()
        {
            // Access time from HeistTimer static class
            float timeNormalized = HeistTimer.TimeRemainingNormalized;
            
            // Defensive: ensure timeNormalized is valid
            if (float.IsNaN(timeNormalized) || float.IsInfinity(timeNormalized))
            {
                timeNormalized = 1f;  // Assume full time if invalid
            }
            timeNormalized = Mathf.Clamp01(timeNormalized);
            
            // Invert so 0 time = 1 urgency, full time = 0 urgency
            float rawUrgency = 1f - timeNormalized;
            
            // Apply curve: low urgency until 50% time used, then rises quickly
            return Mathf.Clamp01(rawUrgency * rawUrgency);
        }
        
        protected override void Awake()
        {
            base.Awake();
            NPCRegistry<RobberNPC>.Register(this);
            _homePosition = transform.position;
            _lastPosition = transform.position;  // Initialize to avoid false footstep on first frame
            _lootDetectionRangeSqr = _lootDetectionRange * _lootDetectionRange;
            _copDetectionRangeSqr = _copDetectionRange * _copDetectionRange;
            
            // Explicitly initialize state (important if Unity's domain reload is disabled)
            _hasEscaped = false;
            _isCarryingLoot = false;
            _carriedLootValue = 0;
            _targetLoot = null;
            _targetCover = null;
            
            Blackboard.Set(BBKeys.CurrentState, "Scout");
            Blackboard.SetFloat(BBKeys.FearLevel, 0f);
            Blackboard.SetBool(BBKeys.CanSeeCop, false);
            Blackboard.SetBool(BBKeys.HasLoot, false);
            Blackboard.SetInt(BBKeys.LootValue, 0);
            
            // Initialize action timestamps to allow immediate action
            Blackboard.SetFloat(BBKeys.LastStealTime, -100f);
            Blackboard.SetFloat(BBKeys.LastFleeTime, -100f);
            Blackboard.SetFloat(BBKeys.LastHideTime, -100f);
            Blackboard.SetFloat(BBKeys.LastSneakTime, -100f);
            Blackboard.SetFloat(BBKeys.LastScoutTime, -100f);
            
            // Initial point discovery - will be refreshed in Start() after scene is fully set up
            RefreshKnownPoints();
            
            // Find escape zone (only one in scene)
            _escapeZone = Object.FindAnyObjectByType<EscapeZone>();
        }
        
        private void Start()
        {
            // Refresh points after scene is fully set up (Awake order is not guaranteed)
            RefreshKnownPoints();
            
            // Also schedule another refresh in case scene setup is still in progress
            Invoke(nameof(RefreshKnownPoints), 0.5f);
            
            // Initialize loot availability BEFORE first tick
            UpdateLootAvailability();
            
            // Verify behavior tree is set up
            string btStatus = BehaviorTree != null ? "OK" : "NULL!";
            

        }
        
        protected override void OnDestroy()
        {
            CancelInvoke();  // Cancel any pending Invoke calls
            NPCRegistry<RobberNPC>.Unregister(this);
            base.OnDestroy();
        }
        
        private void RefreshKnownPoints()
        {
            // Find all loot points and cover points in scene
            _knownLootPoints.Clear();
            _knownLootPoints.AddRange(Object.FindObjectsByType<LootPoint>(FindObjectsSortMode.None));
            
            _knownCoverPoints.Clear();
            _knownCoverPoints.AddRange(Object.FindObjectsByType<CoverPoint>(FindObjectsSortMode.None));
            
            // Find escape zone if not already found
            if (_escapeZone == null)
            {
                _escapeZone = Object.FindAnyObjectByType<EscapeZone>();
            }
            

        }
        
        // Cache action references for debug logging
        private UtilityAction _fleeAction;
        private UtilityAction _carryToEscapeAction;
        private UtilityAction _stealAction;
        private UtilityAction _hideAction;
        private UtilityAction _sneakAction;
        private UtilityAction _scoutAction;
        
        /// <summary>
        /// Override Update to handle escaped state check before ticking.
        /// </summary>
        protected override void Update()
        {
            if (_hasEscaped) return;
            
            base.Update();
        }
        
        private void LateUpdate()
        {
            if (_hasEscaped) return;
            
            UpdateCopDetection();
            UpdateFearLevel();
            UpdateLootAvailability();  // Cache loot availability for utility scoring
            EmitFootstepsIfMoving();  // Emit footstep sounds when moving
            TryEscape();
            

        }
        
        /// <summary>
        /// Debug helper to log all utility action scores.
        /// </summary>
        private string GetUtilityScoresDebug()
        {
            if (_fleeAction == null || _scoutAction == null)
            {
                return "Actions not cached!";
            }
            
            // No try/catch here - if Score() throws, we WANT to see the full error
            // so we can diagnose the root cause rather than masking bugs
            float flee = _fleeAction.Score(this);
            float carry = _carryToEscapeAction.Score(this);
            float steal = _stealAction.Score(this);
            float hide = _hideAction.Score(this);
            float sneak = _sneakAction.Score(this);
            float scout = _scoutAction.Score(this);
            
            // Find the winner
            float maxScore = Mathf.Max(flee, carry, steal, hide, sneak, scout);
            string winner = "?";
            if (maxScore == flee) winner = "Flee";
            else if (maxScore == carry) winner = "CarryToEscape";
            else if (maxScore == steal) winner = "StealLoot";
            else if (maxScore == hide) winner = "Hide";
            else if (maxScore == sneak) winner = "Sneak";
            else if (maxScore == scout) winner = "Scout";
            
            return $"Flee={flee:F2} Carry={carry:F2} Steal={steal:F2} Hide={hide:F2} Sneak={sneak:F2} Scout={scout:F2} | <b>Winner: {winner} ({maxScore:F2})</b>";
        }
        
        private void UpdateCopDetection()
        {
            bool canSeeCop = false;
            float closestCopDistanceSqr = float.MaxValue;
            float closestCopDistance = float.MaxValue;
            Vector3 closestCopPosition = Vector3.zero;
            
            // Cache transform.position to avoid repeated property access
            Vector3 myPosition = transform.position;
            Vector3 myEyePosition = myPosition + Vector3.up;
            
            // Use registry instead of expensive FindObjectsOfType
            var cops = NPCRegistry<CopNPC>.GetAll();
            
            // Reset raycast budget per frame
            _raycastCount = 0;
            
            for (int i = 0; i < cops.Length; i++)
            {
                var copNPC = cops[i];
                if (copNPC == null || !copNPC.gameObject.activeSelf) continue;
                
                Vector3 copPosition = copNPC.transform.position;
                // Use sqrMagnitude for distance comparison
                float distanceSqr = (myPosition - copPosition).sqrMagnitude;
                if (distanceSqr < closestCopDistanceSqr)
                {
                    closestCopDistanceSqr = distanceSqr;
                    closestCopPosition = copPosition;
                }
                
                // Check if we can see this cop (simple line-of-sight) with raycast budget
                if (distanceSqr <= _copDetectionRangeSqr && _raycastCount < _maxCopRaycastsPerFrame)
                {
                    _raycastCount++;
                    float distance = Mathf.Sqrt(distanceSqr);
                    Vector3 dirToCop = (copPosition - myPosition).normalized;
                    if (!Physics.Raycast(myEyePosition, dirToCop, distance - 0.5f))
                    {
                        canSeeCop = true;
                        _lastCopSightTime = Time.time;
                    }
                }
            }
            
            // Only compute sqrt when needed for storage
            closestCopDistance = closestCopDistanceSqr < float.MaxValue ? Mathf.Sqrt(closestCopDistanceSqr) : float.MaxValue;
            
            Blackboard.SetBool(BBKeys.CanSeeCop, canSeeCop);
            Blackboard.SetFloat(BBKeys.ClosestCopDistance, closestCopDistance);
            if (closestCopPosition != Vector3.zero)
            {
                Blackboard.SetVector3(BBKeys.ClosestCopPosition, closestCopPosition);
            }
        }
        
        private void UpdateLootAvailability()
        {
            // Cache loot availability AND distance to avoid repeated FindNearestLoot() calls during utility scoring
            var nearestLoot = FindNearestLoot();
            _hasLootAvailable = nearestLoot != null;
            _cachedLootDistance = nearestLoot != null 
                ? Vector3.Distance(transform.position, nearestLoot.transform.position) 
                : 999f;
        }
        
        private void EmitFootstepsIfMoving()
        {
            Vector3 currentPosition = transform.position;
            float distanceMoved = Vector3.Distance(currentPosition, _lastPosition);
            
            // Only emit footsteps if actually moving
            if (distanceMoved > 0.1f)
            {
                // Emit footsteps at regular intervals based on speed
                float timeSinceLastFootstep = Time.time - _lastFootstepTime;
                
                // Faster movement = more frequent footsteps
                float currentSpeed = distanceMoved / Time.deltaTime;
                float adjustedInterval = _footstepInterval * (4f / Mathf.Max(currentSpeed, 1f));
                adjustedInterval = Mathf.Clamp(adjustedInterval, 0.2f, 0.8f);
                
                if (timeSinceLastFootstep >= adjustedInterval)
                {
                    // Volume depends on movement state - sneaking is quieter
                    float volume = _cachedState == "Sneaking" ? _sneakFootstepVolume : _footstepVolume;
                    
                    // Emit footstep sound that cops can hear
                    // Use EmitSound directly to pass the source GameObject
                    // Use 15f radius for consistency with standard EmitFootstep helper
                    SoundManager.EmitSound(currentPosition, SoundType.Footstep, volume, 15f, gameObject);
                    _lastFootstepTime = Time.time;
                }
            }
            
            _lastPosition = currentPosition;
        }
        
        private void UpdateFearLevel()
        {
            float fearLevel = Blackboard.GetFloat(BBKeys.FearLevel, 0f);
            
            // KEY DESIGN: Robber is BOLD when approaching loot (no fear)!
            // Fear only kicks in AFTER stealing - now they have something to lose.
            if (!_isCarryingLoot)
            {
                // No loot yet = stay bold and confident!
                // Rapidly decay any existing fear to 0
                fearLevel = Mathf.MoveTowards(fearLevel, 0f, Time.deltaTime * 3f);
            }
            else if (Blackboard.GetBool(BBKeys.CanSeeCop, false))
            {
                // Carrying loot AND see a cop = FEAR!
                // Now we have something to lose - get nervous!
                float copDist = Blackboard.GetFloat(BBKeys.ClosestCopDistance, 100f);
                float proximityFear = Mathf.Clamp01(1f - (copDist / _copDetectionRange));
                fearLevel = Mathf.MoveTowards(fearLevel, 0.5f + proximityFear * 0.5f, Time.deltaTime * 2f);
            }
            else
            {
                // Carrying loot but no cop visible - slowly calm down
                fearLevel = Mathf.MoveTowards(fearLevel, 0f, Time.deltaTime * 0.3f);
            }
            
            Blackboard.SetFloat(BBKeys.FearLevel, fearLevel);
        }
        
        private void TryEscape()
        {
            if (!_isCarryingLoot || _escapeZone == null) return;
            
            if (_escapeZone.TryEscape(gameObject, _carriedLootValue))
            {
                _hasEscaped = true;
                _cachedState = "Escaped!";
                Blackboard.Set(BBKeys.CurrentState, "Escaped!");
                
                // Disable the robber
                gameObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// Called when this robber is arrested by a cop.
        /// </summary>
        public void OnArrested()
        {
            _carriedLootValue = 0;
            _isCarryingLoot = false;
            Blackboard.SetBool(BBKeys.HasLoot, false);
            Blackboard.SetInt(BBKeys.LootValue, 0);
            _cachedState = "Arrested!";
            Blackboard.Set(BBKeys.CurrentState, "Arrested!");
            
            NPCBrainDebug.Log(NPCBrainDebug.Category.General, $"[CopsAndRobbers] {name} was arrested!", this);
            
            // Disable the robber
            gameObject.SetActive(false);
        }
        
        /// <summary>
        /// Called when the heist time expires. Robber loses!
        /// </summary>
        public void OnTimeExpired()
        {
            _carriedLootValue = 0;
            _isCarryingLoot = false;
            Blackboard.SetBool(BBKeys.HasLoot, false);
            Blackboard.SetInt(BBKeys.LootValue, 0);
            _cachedState = "Time's Up!";
            Blackboard.Set(BBKeys.CurrentState, "Time's Up!");
            
            NPCBrainDebug.Log(NPCBrainDebug.Category.General, $"[CopsAndRobbers] {name} ran out of time!", this);
            
            // Disable the robber
            gameObject.SetActive(false);
        }
        
        /// <summary>
        /// Picks up loot from a loot point.
        /// </summary>
        public void PickupLoot(LootPoint loot)
        {
            if (loot == null || loot.IsStolen)
            {
                return;
            }
            
            if (loot.TrySteal(gameObject))
            {
                _targetLoot = null;
                _carriedLootValue += loot.Value;
                _isCarryingLoot = true;
                Blackboard.SetBool(BBKeys.HasLoot, true);
                Blackboard.SetInt(BBKeys.LootValue, _carriedLootValue);
                
                // Show loot bag visual
                var bag = transform.Find("LootBag");
                if (bag != null) bag.gameObject.SetActive(true);
                
                Debug.Log($"<color=green>[{name}] STOLE {loot.name} (${loot.Value}) - Now carrying ${_carriedLootValue}</color>");
            }
        }
        
        /// <inheritdoc/>
        protected override BTNode CreateBehaviorTree()
        {
            // Create and CACHE actions so we can debug their scores later
            _fleeAction = CreateFleeAction();
            _carryToEscapeAction = CreateCarryToEscapeAction();
            _stealAction = CreateStealAction();
            _hideAction = CreateHideAction();
            _sneakAction = CreateSneakAction();
            _scoutAction = CreateScoutAction();
            
            // Actions created for utility AI
            
            var selector = new UtilitySelector(
                _fleeAction,
                _carryToEscapeAction,
                _stealAction,
                _hideAction,
                _sneakAction,
                _scoutAction
            );
            
            // Enable warning logging so we can see when no action is selected
            selector.LogWarnings = true;
            
            return selector;
        }
        
        private UtilityAction CreateFleeAction()
        {
            var fleeBehavior = new Sequence(
                new SetBlackboard(BBKeys.LastFleeTime, () => Time.time),
                new SetBlackboard(BBKeys.CurrentState, () => { _cachedState = "Flee!"; return "Flee!"; }),
                new MoveTo(
                    () => GetFleePosition(),
                    _arrivalDistance,
                    _fleeSpeed,
                    3f
                )
            );
            fleeBehavior.Name = "FleeBehavior";
            
            return new UtilityAction(
                "Flee",
                fleeBehavior,
                _fleeWeight,
                // Must see a cop - BUT when urgency is high, lower this threshold
                // At high urgency, even fleeing feels less necessary - need to take risks!
                new BlackboardConsideration<bool>("SeesCop", BBKeys.CanSeeCop,
                    sees => sees ? 1f : 0f, false),
                // Higher score when cop is close
                new BlackboardConsideration<float>("CopProximity", BBKeys.ClosestCopDistance,
                    dist => Mathf.Clamp01(1f - (dist / _copDetectionRange)), 100f),
                // Higher score when fear is high, but urgency reduces fear response
                // IMPORTANT: Even with 0 fear (before stealing), flee should still work when cop is CLOSE!
                new FunctionalConsideration("FearVsUrgency",
                    _ => {
                        float fear = Blackboard.GetFloat(BBKeys.FearLevel, 0f);
                        float urgency = Urgency;
                        float copDist = Blackboard.GetFloat(BBKeys.ClosestCopDistance, 100f);
                        // Base of 0.7 ensures Flee can beat Scout (0.35) even with 0 fear when cop is close
                        // Fear adds up to 0.3 more
                        float fearInfluence = 1f - urgency * 0.5f;
                        return 0.7f + fear * 0.3f * fearInfluence;
                    })
            );
        }
        
        private UtilityAction CreateCarryToEscapeAction()
        {
            var carryBehavior = new Sequence(
                new SetBlackboard(BBKeys.CurrentState, () => { 
                    float urgency = Urgency;
                    _cachedState = urgency > 0.5f ? "Escaping (RUSH!)" : "Escaping"; 
                    return _cachedState; 
                }),
                new MoveTo(
                    () => GetEscapePosition(),
                    _arrivalDistance,
                    _fleeSpeed,  // Use flee speed - urgency!
                    15f  // Long enough to reach escape zone
                )
            );
            carryBehavior.Name = "CarryToEscapeBehavior";
            
            return new UtilityAction(
                "CarryToEscape",
                carryBehavior,
                _carryToEscapeWeight,
                // Must have loot - critical gate (returns 1.0 when has loot)
                new BlackboardConsideration<bool>("HasLoot", BBKeys.HasLoot,
                    has => has ? 1f : 0f, false),
                // Cop visibility matters less when time is running out!
                // At high urgency, ignore cops and just RUN for the exit
                new FunctionalConsideration("CopVisibilityVsUrgency",
                    _ => {
                        bool seesCop = Blackboard.GetBool(BBKeys.CanSeeCop, false);
                        float urgency = Urgency;
                        // Base: 0.8 if sees cop, 1.0 if not
                        // At high urgency: always 1.0 (ignore cop, just escape!)
                        float basePriority = seesCop ? 0.8f : 1f;
                        return Mathf.Lerp(basePriority, 1f, urgency);
                    }),
                // URGENCY BOOST: Dramatically increase escape priority as time runs out!
                new FunctionalConsideration("UrgencyBoost",
                    _ => {
                        float urgency = Urgency;
                        // At 0 urgency: 1.0 (no boost)
                        // At max urgency: 1.5 (50% boost to escape priority!)
                        return 1f + urgency * 0.5f;
                    })
            );
        }
        
        private UtilityAction CreateStealAction()
        {
            var stealBehavior = new Sequence(
                new SetBlackboard(BBKeys.LastStealTime, () => Time.time),
                new SetBlackboard(BBKeys.CurrentState, () => { _cachedState = "Stealing"; return "Stealing"; }),
                new MoveTo(
                    () => GetTargetLootPosition(),
                    1.5f,
                    _normalSpeed + 1f,  // Slightly faster when stealing
                    10f  // Longer timeout - don't interrupt while approaching loot!
                ),
                new Wait(_stealTime * 0.5f, () => TryStealTargetLoot())  // Faster steal
            );
            stealBehavior.Name = "StealBehavior";
            
            return new UtilityAction(
                "StealLoot",
                stealBehavior,
                _stealWeight,
                // Must not have loot already
                new FunctionalConsideration("NoLootYet",
                    _ => Blackboard.GetBool(BBKeys.HasLoot, false) ? 0f : 1f),
                // Must have loot available to steal - use cached value (updated in LateUpdate)
                new FunctionalConsideration("LootAvailable", 
                    _ => _hasLootAvailable ? 1f : 0f),
                // PROXIMITY BOOST: Higher score when closer to loot!
                new FunctionalConsideration("LootProximityBoost",
                    _ => {
                        if (!_hasLootAvailable) return 0.3f;
                        float dist = _cachedLootDistance;
                        // At 0m: 1.3 (very high priority), at 40m+: 0.5
                        return Mathf.Lerp(1.3f, 0.5f, Mathf.Clamp01(dist / 40f));
                    }),
                // Cop visibility - LESS PUNISHING now!
                // INTENDED BEHAVIOR: Robber WILL try to steal even with cops visible because
                // they are BOLD before stealing (fear=0). This is by design - the robber takes
                // risks to get the loot, then becomes nervous after stealing.
                // StealLoot (0.5-0.6) will beat Scout (0.35) when loot is nearby, even with cops visible.
                new FunctionalConsideration("CopRiskVsUrgency",
                    _ => {
                        bool seesCop = Blackboard.GetBool(BBKeys.CanSeeCop, false);
                        if (!seesCop) return 1f;
                        // Can see cop - but still try if cop is far away or urgency is high
                        float copDist = Blackboard.GetFloat(BBKeys.ClosestCopDistance, 100f);
                        float urgency = Urgency;
                        // Base: 0.3 when seeing cop (was 0.1 - too punishing!)
                        // Boost if cop is far (>10m) or urgency is high
                        float distBonus = Mathf.Clamp01((copDist - 5f) / 15f) * 0.4f;  // 0 at 5m, 0.4 at 20m
                        float urgencyBonus = urgency * 0.3f;  // Up to 0.3 at max urgency
                        return 0.3f + distBonus + urgencyBonus;
                    }),
                // Fear impact - but robber is BOLD before stealing (fear=0), so this mainly affects
                // edge cases where robber somehow has fear without loot
                new FunctionalConsideration("FearVsUrgencyForSteal",
                    _ => {
                        float fear = Blackboard.GetFloat(BBKeys.FearLevel, 0f);
                        // Fear is 0 before stealing, so this usually returns 1.0
                        // If somehow fearful, still allow stealing at reduced score
                        return Mathf.Max(0.5f, 1f - fear * 0.3f);
                    })
            );
        }
        
        private UtilityAction CreateHideAction()
        {
            var hideBehavior = new Sequence(
                new SetBlackboard(BBKeys.LastHideTime, () => Time.time),
                new SetBlackboard(BBKeys.CurrentState, () => { _cachedState = "Hiding"; return "Hiding"; }),
                new MoveTo(
                    () => GetNearestCoverPosition(),
                    _arrivalDistance,
                    _normalSpeed,
                    5f  // 5 second timeout - don't get stuck trying to reach cover
                ),
                new Wait(1f)  // Short pause, then re-evaluate
            );
            hideBehavior.Name = "HideBehavior";
            
            return new UtilityAction(
                "Hide",
                hideBehavior,
                _hideWeight,
                // CRITICAL: If we have loot, we should be ESCAPING, not hiding!
                // This prevents the robber from hiding forever when they should run to escape zone
                new FunctionalConsideration("ShouldEscapeInstead",
                    _ => {
                        bool hasLoot = Blackboard.GetBool(BBKeys.HasLoot, false);
                        if (!hasLoot) return 1.0f;  // No loot - hiding is fine
                        // Has loot - only hide if cop is VERY close (emergency)
                        bool seesCop = Blackboard.GetBool(BBKeys.CanSeeCop, false);
                        float copDist = Blackboard.GetFloat(BBKeys.ClosestCopDistance, 100f);
                        if (seesCop && copDist < 5f) return 0.8f;  // Emergency hide - cop is right there!
                        return 0.0f;  // Has loot and no immediate danger - GO ESCAPE!
                    }),
                // Fear consideration - but hiding is less attractive when urgent!
                // No time to hide when the clock is ticking!
                // IMPORTANT: Hide should only win when NOT seeing a cop (just lost sight)
                new FunctionalConsideration("FearVsUrgencyForHide",
                    _ => {
                        float fear = Blackboard.GetFloat(BBKeys.FearLevel, 0f);
                        float urgency = Urgency;
                        // Only hide if fear is meaningful
                        if (fear < 0.2f) return 0.1f;  // No reason to hide if not scared
                        float baseFearScore = 0.4f + fear * 0.4f;  // 0.4-0.8 based on fear
                        // At high urgency, hiding becomes much less attractive
                        return baseFearScore * (1f - urgency * 0.8f);
                    }),
                // CRITICAL: Hide is for when you JUST LOST sight of a cop, not when you can see one!
                // When you can SEE a cop, you should FLEE, not hide.
                // When you CAN'T see a cop but were recently scared, THEN hide.
                new FunctionalConsideration("RecentlySawCopButNotNow",
                    _ => {
                        bool seesCop = Blackboard.GetBool(BBKeys.CanSeeCop, false);
                        if (seesCop) return 0.1f;  // Very low - you can see cop, should flee instead!
                        // Don't see cop now - check if we recently saw one
                        float timeSinceCop = TimeSinceLastCopSight;
                        if (timeSinceCop < 5f) return 1.0f;  // Recently saw cop, good time to hide
                        return 0.3f;  // Long time since cop, no real need to hide
                    }),
                // Cooldown
                new TimeConsideration("HideCooldown", BBKeys.LastHideTime, 5f)
            );
        }
        
        private UtilityAction CreateSneakAction()
        {
            var sneakBehavior = new Sequence(
                new SetBlackboard(BBKeys.LastSneakTime, () => Time.time),
                new SetBlackboard(BBKeys.CurrentState, () => { _cachedState = "Sneaking"; return "Sneaking"; }),
                new MoveTo(
                    () => GetSneakPosition(),
                    _arrivalDistance,
                    _sneakSpeed,  // Keep sneak speed - urgency makes sneaking less likely to be selected
                    5f
                )
            );
            sneakBehavior.Name = "SneakBehavior";
            
            return new UtilityAction(
                "Sneak",
                sneakBehavior,
                _sneakWeight,
                // Not seeing cop - but at high urgency, we're more willing to risk being seen
                new FunctionalConsideration("CopVisibilityForSneak",
                    _ => {
                        bool seesCop = Blackboard.GetBool(BBKeys.CanSeeCop, false);
                        float urgency = Urgency;
                        // Normal: 0 if sees cop. High urgency: 0.3 even if sees cop
                        if (!seesCop) return 1f;
                        return urgency > 0.6f ? 0.3f : 0f;
                    }),
                // Moderate fear (cautious) - but less important when urgent
                new FunctionalConsideration("FearForSneak",
                    _ => {
                        float fear = Blackboard.GetFloat(BBKeys.FearLevel, 0f);
                        float urgency = Urgency;
                        float baseFearScore = fear > 0.1f && fear < 0.6f ? 0.8f : 0.3f;
                        // At high urgency, sneaking is less attractive (no time for caution!)
                        return baseFearScore * (1f - urgency * 0.6f);
                    }),
                // Cooldown - reduced at high urgency
                new FunctionalConsideration("SneakCooldown",
                    _ => {
                        float lastSneak = Blackboard.GetFloat(BBKeys.LastSneakTime, -10f);
                        float cooldown = Mathf.Lerp(4f, 2f, Urgency); // 4s normal, 2s at max urgency
                        float elapsed = Time.time - lastSneak;
                        return elapsed >= cooldown ? 1f : elapsed / cooldown;
                    })
            );
        }
        
        private UtilityAction CreateScoutAction()
        {
            var scoutBehavior = new Sequence(
                new SetBlackboard(BBKeys.LastScoutTime, () => Time.time),
                new SetBlackboard(BBKeys.CurrentState, () => { _cachedState = "Scouting"; return "Scouting"; }),
                new MoveTo(
                    () => GetScoutPosition(),
                    _arrivalDistance,
                    _normalSpeed + 1f,  // Faster scouting - get to loot quickly!
                    8f  // Longer timeout - let Scout complete its movement
                )
            );
            scoutBehavior.Name = "ScoutBehavior";
            
            // Scout is the FALLBACK action - it should ALWAYS have a positive score!
            // IMPORTANT: We use a GUARANTEED 1.0 consideration to ensure Scout works.
            // The base score of 0.35 ensures it can beat Hide (which scores ~0.17-0.3 typically)
            // but still loses to StealLoot when close to loot.
            //
            // WORKAROUND: We add a dummy "AlwaysReady" consideration that returns 1.0 because
            // UtilityAction.Score() may behave unexpectedly with zero considerations in some
            // edge cases (e.g., compensation factor calculation divides by consideration count).
            // Adding a single 1.0 consideration ensures consistent scoring behavior.
            return new UtilityAction(
                "Scout",
                scoutBehavior,
                _scoutWeight,  // Uses serialized weight (default 0.3, but set to 0.35 to beat Hide)
                // Single consideration that ALWAYS returns 1.0 to ensure score is calculated
                new FunctionalConsideration("AlwaysReady", _ => 1.0f)
            );
        }
        
        private Vector3 GetFleePosition()
        {
            // Flee away from the closest cop
            Vector3 copPos = Blackboard.GetVector3(BBKeys.ClosestCopPosition, transform.position);
            Vector3 fleeDir = (transform.position - copPos).normalized;
            
            // Try to flee toward escape zone if carrying loot
            if (_isCarryingLoot && _escapeZone != null)
            {
                Vector3 toEscape = (_escapeZone.transform.position - transform.position).normalized;
                fleeDir = (fleeDir + toEscape).normalized;
            }
            
            return transform.position + fleeDir * 10f;
        }
        
        private Vector3 GetEscapePosition()
        {
            if (_escapeZone != null)
            {
                return _escapeZone.transform.position;
            }
            Debug.LogWarning($"<color=red>[{name}]</color> GetEscapePosition: No escape zone found!");
            return _homePosition;
        }
        
        private Vector3 GetTargetLootPosition()
        {
            // Find nearest unstolen loot
            if (_targetLoot == null || _targetLoot.IsStolen)
            {
                _targetLoot = FindNearestLoot();
    
            }
            
            if (_targetLoot != null)
            {
                return _targetLoot.transform.position;
            }
            return transform.position;
        }
        
        private LootPoint FindNearestLoot()
        {
            LootPoint nearest = null;
            float nearestDistSqr = float.MaxValue;
            
            // Cache transform.position
            Vector3 myPosition = transform.position;
            
            // If no known loot points, try to refresh
            if (_knownLootPoints.Count == 0)
            {
                RefreshKnownPoints();
            }
            
            for (int i = 0; i < _knownLootPoints.Count; i++)
            {
                var loot = _knownLootPoints[i];
                if (loot == null || loot.IsStolen) continue;
                
                // Use sqrMagnitude for distance comparison
                float distSqr = (myPosition - loot.transform.position).sqrMagnitude;
                // Use configured detection range (already set to 100m = 10000 sqr)
                if (distSqr < nearestDistSqr && distSqr <= _lootDetectionRangeSqr)
                {
                    nearestDistSqr = distSqr;
                    nearest = loot;
                }
            }
            
            return nearest;
        }
        
        private void TryStealTargetLoot()
        {
            if (_targetLoot != null && !_targetLoot.IsStolen)
            {
                // Use sqrMagnitude for distance check
                float distSqr = (transform.position - _targetLoot.transform.position).sqrMagnitude;
                float stealRadiusSqr = _targetLoot.StealRadius * _targetLoot.StealRadius;
                float dist = Mathf.Sqrt(distSqr);
                if (distSqr <= stealRadiusSqr)
                {
                    PickupLoot(_targetLoot);
                }
            }

        }
        
        private Vector3 GetNearestCoverPosition()
        {
            // Cache the cover position for 2 seconds to prevent moving target issues
            // This stops the MoveTo from never arriving because target keeps changing
            if (Time.time - _coverPositionCacheTime < 2f && _cachedCoverPosition != Vector3.zero)
            {
                return _cachedCoverPosition;
            }
            
            CoverPoint nearest = null;
            float nearestDistSqr = float.MaxValue;
            
            // Cache transform.position
            Vector3 myPosition = transform.position;
            
            for (int i = 0; i < _knownCoverPoints.Count; i++)
            {
                var cover = _knownCoverPoints[i];
                if (cover == null || !cover.CanHide(gameObject)) continue;
                
                // Use sqrMagnitude for distance comparison
                float distSqr = (myPosition - cover.transform.position).sqrMagnitude;
                if (distSqr < nearestDistSqr)
                {
                    nearestDistSqr = distSqr;
                    nearest = cover;
                }
            }
            
            _targetCover = nearest;
            
            if (nearest != null)
            {
                _cachedCoverPosition = nearest.HidePosition;
                _coverPositionCacheTime = Time.time;
                return _cachedCoverPosition;
            }
            
            // No cover found - pick a fixed fallback position (don't use GetFleePosition which changes every frame!)
            // Move to a position 10m away from current position, away from the cop
            Vector3 copPos = Blackboard.GetVector3(BBKeys.ClosestCopPosition, myPosition + Vector3.forward * 10f);
            Vector3 awayFromCop = (myPosition - copPos).normalized;
            _cachedCoverPosition = myPosition + awayFromCop * 10f;
            _coverPositionCacheTime = Time.time;
            return _cachedCoverPosition;
        }
        
        private Vector3 GetSneakPosition()
        {
            // Sneak toward loot if we don't have any
            if (!_isCarryingLoot)
            {
                var loot = FindNearestLoot();
                if (loot != null)
                {
                    // Move partway toward loot, cautiously
                    Vector3 toLoot = (loot.transform.position - transform.position).normalized;
                    return transform.position + toLoot * 5f;
                }
            }
            else
            {
                // Sneak toward escape
                return GetEscapePosition();
            }
            
            return transform.position + Random.insideUnitSphere * 5f;
        }
        
        private Vector3 GetScoutPosition()
        {
            // Scouting should DIRECTLY move toward loot if available
            var loot = FindNearestLoot();
            if (loot != null)
            {
                return loot.transform.position;
            }
            
            // No loot found - wander toward center of map
            Vector2 randomCircle = Random.insideUnitCircle * 15f;
            return _homePosition + new Vector3(randomCircle.x, 0f, randomCircle.y);
        }
        
        /// <summary>
        /// Creates a RobberNPC at the specified position.
        /// </summary>
        public static RobberNPC Create(Vector3 position, Transform parent = null)
        {
            var robberObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            robberObj.name = "Robber";
            robberObj.transform.position = position;
            var robberRenderer = robberObj.GetComponent<Renderer>();
            robberRenderer.material.color = new Color(0.2f, 0.2f, 0.2f); // Dark color
            
            if (parent != null)
            {
                robberObj.transform.SetParent(parent);
            }
            
            // Add sight sensor - clear target tag to avoid tag errors (tags require manual Unity setup)
            // Robber uses NPCRegistry<CopNPC> for cop detection instead of sight sensor tags
            var sightSensor = robberObj.AddComponent<SightSensor>();
            sightSensor.SetTargetTag(""); // Clear tag filter to avoid "tag not defined" errors
            
            // Add hearing sensor (required by NPCBrainController)
            robberObj.AddComponent<HearingSensor>();
            
            // Add CharacterController for collision-based movement (respects walls/obstacles)
            var charController = robberObj.AddComponent<CharacterController>();
            charController.height = 2f;
            charController.radius = 0.5f;
            charController.center = new Vector3(0f, 1f, 0f);
            charController.slopeLimit = 45f;
            charController.stepOffset = 0.3f;
            
            // Add robber component
            var robber = robberObj.AddComponent<RobberNPC>();
            
            // Add mask indicator (robber's mask)
            var mask = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mask.name = "Mask";
            mask.transform.SetParent(robberObj.transform);
            mask.transform.localPosition = new Vector3(0f, 0.9f, 0.25f);
            mask.transform.localScale = new Vector3(0.5f, 0.2f, 0.1f);
            var maskRenderer = mask.GetComponent<Renderer>();
            maskRenderer.material.color = Color.black;
            Object.Destroy(mask.GetComponent<Collider>());
            
            // Add loot bag indicator (shows when carrying)
            var bag = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bag.name = "LootBag";
            bag.transform.SetParent(robberObj.transform);
            bag.transform.localPosition = new Vector3(0.4f, 0.3f, 0f);
            bag.transform.localScale = new Vector3(0.3f, 0.4f, 0.2f);
            var bagRenderer = bag.GetComponent<Renderer>();
            bagRenderer.material.color = new Color(0.4f, 0.3f, 0.1f);
            bag.SetActive(false); // Hidden until carrying loot
            Object.Destroy(bag.GetComponent<Collider>());
            
            return robber;
        }
    }
}
