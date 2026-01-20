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
        [SerializeField] private float _scoutWeight = 0.3f;
        
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
                if (_cachedState == "Stealing") return "Coast is clear - grabbing the loot!";
                if (_cachedState == "Hiding")
                {
                    return FearLevel > 0.5f ? "Too dangerous - laying low" : "Staying out of sight";
                }
                if (_cachedState == "Sneaking") return _isCarryingLoot ? "Moving carefully with the goods" : "Approaching target quietly";
                if (_cachedState == "Scouting") return "Looking for opportunities...";
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
            
            Debug.Log($"<color=magenta>[{name}]</color> <color=cyan>START - Found {_knownLootPoints.Count} loot points, {_knownCoverPoints.Count} cover points, HasLootAvailable={_hasLootAvailable}</color>");
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
            
            // Log for debugging
            if (_knownLootPoints.Count > 0)
            {
                Debug.Log($"<color=magenta>[{name}]</color> RefreshKnownPoints: Found {_knownLootPoints.Count} loot points");
            }
        }
        
        private float _lastRobberDebugTime;
        
        private void LateUpdate()
        {
            if (_hasEscaped) return;
            
            UpdateCopDetection();
            UpdateFearLevel();
            UpdateLootAvailability();  // Cache loot availability for utility scoring
            EmitFootstepsIfMoving();  // Emit footstep sounds when moving
            TryEscape();
            
            // Track tick count for debugging
            _tickCount++;
            
            // Debug log every 2 seconds
            if (Time.time - _lastRobberDebugTime > 2f)
            {
                _lastRobberDebugTime = Time.time;
                var nearestLoot = FindNearestLoot();
                string lootInfo = nearestLoot != null ? $"{nearestLoot.name} at {Vector3.Distance(transform.position, nearestLoot.transform.position):F1}m" : "NO LOOT FOUND";
                float copDist = Blackboard.GetFloat(BBKeys.ClosestCopDistance, 999f);
                float timeRemaining = HeistTimer.TimeRemaining;
                string timeInfo = HeistTimer.IsTimeLimitEnabled ? $"Time: {timeRemaining:F0}s | Urgency: {Urgency:F2}" : "No time limit";
                Debug.Log($"<color=magenta>[{name}]</color> State: <color=yellow>{_cachedState}</color> | {timeInfo} | CanSeeCop: {CanSeeCop} | Fear: {FearLevel:F2} | HasLoot: {_isCarryingLoot} | LootAvail: {_hasLootAvailable} | Loot: {lootInfo} | KnownLoot: {_knownLootPoints.Count} | Ticks: {_tickCount}");
            }
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
            
            if (Blackboard.GetBool(BBKeys.CanSeeCop, false))
            {
                // Increase fear when we see a cop
                float copDist = Blackboard.GetFloat(BBKeys.ClosestCopDistance, 100f);
                float proximityFear = Mathf.Clamp01(1f - (copDist / _copDetectionRange));
                fearLevel = Mathf.MoveTowards(fearLevel, 0.5f + proximityFear * 0.5f, Time.deltaTime * 2f);
            }
            else
            {
                // Decay fear when no cop visible
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
                Debug.Log($"<color=magenta>[{name}]</color> <color=red>PickupLoot FAILED - loot null or already stolen</color>");
                return;
            }
            
            Debug.Log($"<color=magenta>[{name}]</color> <color=green>*** STEALING LOOT: {loot.name} worth ${loot.Value}! ***</color>");
            
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
                
                Debug.Log($"<color=magenta>[{name}]</color> <color=green>*** LOOT STOLEN SUCCESSFULLY! Now carrying ${_carriedLootValue} ***</color>");
            }
            else
            {
                Debug.Log($"<color=magenta>[{name}]</color> <color=red>TrySteal returned false!</color>");
            }
        }
        
        /// <inheritdoc/>
        protected override BTNode CreateBehaviorTree()
        {
            var fleeAction = CreateFleeAction();
            var carryToEscapeAction = CreateCarryToEscapeAction();
            var stealAction = CreateStealAction();
            var hideAction = CreateHideAction();
            var sneakAction = CreateSneakAction();
            var scoutAction = CreateScoutAction();
            
            Debug.Log($"<color=magenta>[{name}]</color> <color=green>CreateBehaviorTree - 6 actions created</color>");
            
            return new UtilitySelector(
                fleeAction,
                carryToEscapeAction,
                stealAction,
                hideAction,
                sneakAction,
                scoutAction
            );
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
                // When time is running out, we care less about fear!
                new FunctionalConsideration("FearVsUrgency",
                    _ => {
                        float fear = Blackboard.GetFloat(BBKeys.FearLevel, 0f);
                        float urgency = Urgency;
                        // At high urgency, reduce fear's influence (take more risks)
                        float fearInfluence = 1f - urgency * 0.5f; // At max urgency, fear only 50% effective
                        return 0.5f + fear * 0.5f * fearInfluence;
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
                    2f  // Short timeout to re-evaluate quickly
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
                // This ensures StealLoot wins over Scout when we're close
                new FunctionalConsideration("LootProximityBoost",
                    _ => {
                        if (!_hasLootAvailable) return 0.3f;
                        // Use cached distance to avoid repeated FindNearestLoot calls
                        float dist = _cachedLootDistance;
                        // At 0m: 1.3 (very high priority), at 15m: 0.8, at 40m+: 0.4
                        // This makes StealLoot dominant when close to loot
                        return Mathf.Lerp(1.3f, 0.4f, Mathf.Clamp01(dist / 40f));
                    }),
                // Cop visibility - at high urgency, take more risks!
                new FunctionalConsideration("CopRiskVsUrgency",
                    _ => {
                        bool seesCop = Blackboard.GetBool(BBKeys.CanSeeCop, false);
                        float urgency = Urgency;
                        if (!seesCop) return 1f;
                        // At high urgency, we might try to steal even with cop visible (risky!)
                        return urgency > 0.5f ? 0.5f : 0.1f;  // Small chance even at low urgency
                    }),
                // Fear matters less when urgent - take risks!
                new FunctionalConsideration("FearVsUrgencyForSteal",
                    _ => {
                        float fear = Blackboard.GetFloat(BBKeys.FearLevel, 0f);
                        float urgency = Urgency;
                        // Normal: fear reduces score by 50%. At max urgency: only 10%
                        float fearMultiplier = Mathf.Lerp(0.5f, 0.1f, urgency);
                        return 1f - fear * fearMultiplier;
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
                    _normalSpeed
                ),
                new Wait(3f)
            );
            hideBehavior.Name = "HideBehavior";
            
            return new UtilityAction(
                "Hide",
                hideBehavior,
                _hideWeight,
                // Fear consideration - but hiding is less attractive when urgent!
                // No time to hide when the clock is ticking!
                new FunctionalConsideration("FearVsUrgencyForHide",
                    _ => {
                        float fear = Blackboard.GetFloat(BBKeys.FearLevel, 0f);
                        float urgency = Urgency;
                        float baseFearScore = fear > 0.3f ? 0.5f + fear * 0.5f : 0.2f;
                        // At high urgency, hiding becomes much less attractive
                        // Multiply by inverse urgency: at 0 urgency = 100%, at max urgency = 20%
                        return baseFearScore * (1f - urgency * 0.8f);
                    }),
                // Not seeing cop but was recently
                new BlackboardConsideration<bool>("NoCopNow", BBKeys.CanSeeCop,
                    sees => sees ? 0.3f : 1f, false),
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
            // BUT it should yield to StealLoot when close to loot to avoid oscillation.
            return new UtilityAction(
                "Scout",
                scoutBehavior,
                1.0f,  // HIGH base score - Scout is the fallback!
                // DISTANCE-BASED SCORING: High score when far from loot, low when close
                // This prevents oscillation with StealLoot by making Scout back off
                // when we're close enough for StealLoot to take over
                new FunctionalConsideration("DistanceToLoot",
                    _ => {
                        if (!_hasLootAvailable) return 0.8f;  // No loot? Scout to find some!
                        
                        // Use cached distance to avoid repeated FindNearestLoot calls
                        float dist = _cachedLootDistance;
                        // At 0-5m: 0.2 (very low - let StealLoot handle it)
                        // At 15m: 0.44 (transitioning)
                        // At 30m+: 0.8 (high - need to scout toward loot)
                        return Mathf.Lerp(0.2f, 0.8f, Mathf.Clamp01((dist - 5f) / 25f));
                    })
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
            return _homePosition;
        }
        
        private Vector3 GetTargetLootPosition()
        {
            // Find nearest unstolen loot
            if (_targetLoot == null || _targetLoot.IsStolen)
            {
                _targetLoot = FindNearestLoot();
                if (_targetLoot != null)
                {
                    Debug.Log($"<color=magenta>[{name}]</color> Found new target loot: {_targetLoot.name} at {_targetLoot.transform.position}");
                }
            }
            
            if (_targetLoot != null)
            {
                return _targetLoot.transform.position;
            }
            
            Debug.Log($"<color=magenta>[{name}]</color> <color=red>GetTargetLootPosition: No loot found!</color>");
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
                Debug.Log($"<color=magenta>[{name}]</color> TryStealTargetLoot: Distance to {_targetLoot.name} = {dist:F1}m (need < {_targetLoot.StealRadius}m)");
                if (distSqr <= stealRadiusSqr)
                {
                    PickupLoot(_targetLoot);
                }
                else
                {
                    Debug.Log($"<color=magenta>[{name}]</color> <color=orange>Too far to steal!</color>");
                }
            }
            else
            {
                Debug.Log($"<color=magenta>[{name}]</color> <color=red>TryStealTargetLoot: No target or already stolen</color>");
            }
        }
        
        private Vector3 GetNearestCoverPosition()
        {
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
                return nearest.HidePosition;
            }
            
            // No cover found, just move away from cop
            return GetFleePosition();
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
                // Move DIRECTLY toward loot - scouting IS seeking loot!
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
