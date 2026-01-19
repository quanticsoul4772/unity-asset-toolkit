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
        [SerializeField] private float _lootDetectionRange = 25f;
        // Note: Cop detection now uses NPCRegistry<CopNPC> instead of tag-based detection
        
        [Header("Utility Weights")]
        [SerializeField] private float _fleeWeight = 1.0f;
        [SerializeField] private float _carryToEscapeWeight = 0.9f;
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
        
        [Header("Performance")]
        [SerializeField] private int _maxCopRaycastsPerFrame = 2;
        private int _raycastCount;

        
        /// <summary>Current behavior state for UI display.</summary>
        public string CurrentState => _cachedState;
        
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
        
        protected override void Awake()
        {
            base.Awake();
            NPCRegistry<RobberNPC>.Register(this);
            _homePosition = transform.position;
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
            
            // Initialize action timestamps
            Blackboard.SetFloat(BBKeys.LastStealTime, -10f);
            Blackboard.SetFloat(BBKeys.LastFleeTime, -10f);
            Blackboard.SetFloat(BBKeys.LastHideTime, -10f);
            Blackboard.SetFloat(BBKeys.LastSneakTime, -10f);
            Blackboard.SetFloat(BBKeys.LastScoutTime, -10f);
            
            // Find all loot points and cover points in scene
            RefreshKnownPoints();
            
            // Find escape zone (only one in scene)
            _escapeZone = Object.FindAnyObjectByType<EscapeZone>();
        }
        
        protected override void OnDestroy()
        {
            NPCRegistry<RobberNPC>.Unregister(this);
            base.OnDestroy();
        }
        
        private void RefreshKnownPoints()
        {
            // Use FindObjectsOfType only once during initialization
            // This is acceptable as it only happens on Awake
            _knownLootPoints.Clear();
            _knownLootPoints.AddRange(Object.FindObjectsByType<LootPoint>(FindObjectsSortMode.None));
            
            _knownCoverPoints.Clear();
            _knownCoverPoints.AddRange(Object.FindObjectsByType<CoverPoint>(FindObjectsSortMode.None));
        }
        
        private void LateUpdate()
        {
            if (_hasEscaped) return;
            
            UpdateCopDetection();
            UpdateFearLevel();
            TryEscape();
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
        /// Picks up loot from a loot point.
        /// </summary>
        public void PickupLoot(LootPoint loot)
        {
            if (loot == null || loot.IsStolen) return;
            
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
                
                NPCBrainDebug.Log(NPCBrainDebug.Category.General, $"[CopsAndRobbers] {name} stole loot worth ${loot.Value}!", this);
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
                // Must see a cop - critical gate
                new BlackboardConsideration<bool>("SeesCop", BBKeys.CanSeeCop,
                    sees => sees ? 1f : 0f, false),
                // Higher score when cop is close
                new BlackboardConsideration<float>("CopProximity", BBKeys.ClosestCopDistance,
                    dist => Mathf.Clamp01(1f - (dist / _copDetectionRange)), 100f),
                // Higher score when fear is high
                new BlackboardConsideration<float>("FearForFlee", BBKeys.FearLevel,
                    f => 0.5f + f * 0.5f, 0f)
            );
        }
        
        private UtilityAction CreateCarryToEscapeAction()
        {
            var carryBehavior = new Sequence(
                new SetBlackboard(BBKeys.CurrentState, () => { _cachedState = "Escaping"; return "Escaping"; }),
                new MoveTo(
                    () => GetEscapePosition(),
                    _arrivalDistance,
                    _normalSpeed * 1.2f,
                    8f
                )
            );
            carryBehavior.Name = "CarryToEscapeBehavior";
            
            return new UtilityAction(
                "CarryToEscape",
                carryBehavior,
                _carryToEscapeWeight,
                // Must have loot - critical gate
                new BlackboardConsideration<bool>("HasLoot", BBKeys.HasLoot,
                    has => has ? 1f : 0f, false),
                // Higher score when no cop visible
                new BlackboardConsideration<bool>("NoCopForEscape", BBKeys.CanSeeCop,
                    sees => sees ? 0.3f : 1f, false),
                // Higher score when closer to escape
                new DistanceConsideration(
                    "EscapeDistance",
                    brain => GetEscapePosition(),
                    30f,
                    true
                )
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
                    _sneakSpeed
                ),
                new Wait(_stealTime, () => TryStealTargetLoot())
            );
            stealBehavior.Name = "StealBehavior";
            
            return new UtilityAction(
                "StealLoot",
                stealBehavior,
                _stealWeight,
                // Must not have loot already
                new BlackboardConsideration<bool>("NoLootYet", BBKeys.HasLoot,
                    has => has ? 0f : 1f, false),
                // Must not see cop (too risky)
                new BlackboardConsideration<bool>("NoCopForSteal", BBKeys.CanSeeCop,
                    sees => sees ? 0f : 1f, false),
                // Has a target loot nearby
                new ConstantConsideration(0.8f), // Base score if conditions met
                // Lower score when fear is high
                new BlackboardConsideration<float>("LowFearForSteal", BBKeys.FearLevel,
                    f => 1f - f * 0.7f, 0f),
                // Cooldown
                new TimeConsideration("StealCooldown", BBKeys.LastStealTime, 3f)
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
                // Higher score when fear is high
                new BlackboardConsideration<float>("FearForHide", BBKeys.FearLevel,
                    f => f > 0.3f ? 0.5f + f * 0.5f : 0.2f, 0f),
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
                    _sneakSpeed,
                    5f
                )
            );
            sneakBehavior.Name = "SneakBehavior";
            
            return new UtilityAction(
                "Sneak",
                sneakBehavior,
                _sneakWeight,
                // Not seeing cop
                new BlackboardConsideration<bool>("NoCopForSneak", BBKeys.CanSeeCop,
                    sees => sees ? 0f : 1f, false),
                // Moderate fear (cautious)
                new BlackboardConsideration<float>("ModerateFear", BBKeys.FearLevel,
                    f => f > 0.1f && f < 0.6f ? 0.8f : 0.3f, 0f),
                // Cooldown
                new TimeConsideration("SneakCooldown", BBKeys.LastSneakTime, 4f)
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
                    _normalSpeed,
                    6f
                )
            );
            scoutBehavior.Name = "ScoutBehavior";
            
            return new UtilityAction(
                "Scout",
                scoutBehavior,
                _scoutWeight,
                // Baseline behavior
                new ConstantConsideration(0.7f),
                // Lower when fear is high
                new BlackboardConsideration<float>("LowFearForScout", BBKeys.FearLevel,
                    f => 1f - f * 0.8f, 0f),
                // Cooldown
                new TimeConsideration("ScoutCooldown", BBKeys.LastScoutTime, 3f)
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
            
            for (int i = 0; i < _knownLootPoints.Count; i++)
            {
                var loot = _knownLootPoints[i];
                if (loot == null || loot.IsStolen) continue;
                
                // Use sqrMagnitude for distance comparison
                float distSqr = (myPosition - loot.transform.position).sqrMagnitude;
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
                if (distSqr <= stealRadiusSqr)
                {
                    PickupLoot(_targetLoot);
                }
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
            // Randomly explore, biased toward loot areas
            var loot = FindNearestLoot();
            if (loot != null)
            {
                // Move toward loot with some randomness
                Vector3 toLoot = (loot.transform.position - transform.position).normalized;
                Vector3 randomOffset = Random.insideUnitSphere * 3f;
                randomOffset.y = 0f;
                return transform.position + toLoot * 8f + randomOffset;
            }
            
            // Random wander
            Vector2 randomCircle = Random.insideUnitCircle * 10f;
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
