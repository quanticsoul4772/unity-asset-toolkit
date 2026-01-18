using System.Collections.Generic;
using UnityEngine;
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
    public class RobberNPC : NPCBrainController
    {
        [Header("Robber Settings")]
        [SerializeField] private float _normalSpeed = 4f;
        [SerializeField] private float _fleeSpeed = 7f;
        [SerializeField] private float _sneakSpeed = 2f;
        [SerializeField] private float _arrivalDistance = 1f;
        [SerializeField] private float _stealTime = 2f;
        
        [Header("Detection Settings")]
        [SerializeField] private float _copDetectionRange = 15f;
        [SerializeField] private float _lootDetectionRange = 25f;
        [SerializeField] private string _copTag = "Cop";
        
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
        private List<CoverPoint> _knownCoverPoints = new List<CoverPoint>();
        private List<LootPoint> _knownLootPoints = new List<LootPoint>();
        private float _lastCopSightTime;
        private bool _hasEscaped;
        private CopNPC[] _cachedCops;
        private float _lastCopCacheTime;
        
        /// <summary>Current behavior state for UI display.</summary>
        public string CurrentState => Blackboard.Get("currentState", "Scout");
        
        /// <summary>Value of loot being carried.</summary>
        public int CarriedLootValue => _carriedLootValue;
        
        /// <summary>Whether this robber is carrying loot.</summary>
        public bool IsCarryingLoot => _isCarryingLoot;
        
        /// <summary>Whether this robber has escaped.</summary>
        public bool HasEscaped => _hasEscaped;
        
        /// <summary>Time since last saw a cop.</summary>
        public float TimeSinceLastCopSight => Time.time - _lastCopSightTime;
        
        /// <summary>Whether a cop is currently visible.</summary>
        public bool CanSeeCop => Blackboard.Get("canSeeCop", false);
        
        /// <summary>Current fear level (0-1) based on cop proximity.</summary>
        public float FearLevel => Blackboard.Get("fearLevel", 0f);
        
        protected override void Awake()
        {
            base.Awake();
            _homePosition = transform.position;
            
            Blackboard.Set("currentState", "Scout");
            Blackboard.Set("fearLevel", 0f);
            Blackboard.Set("canSeeCop", false);
            Blackboard.Set("hasLoot", false);
            Blackboard.Set("lootValue", 0);
            
            // Initialize action timestamps
            Blackboard.Set("lastStealTime", -10f);
            Blackboard.Set("lastFleeTime", -10f);
            Blackboard.Set("lastHideTime", -10f);
            Blackboard.Set("lastSneakTime", -10f);
            Blackboard.Set("lastScoutTime", -10f);
            
            // Find all loot points and cover points in scene
            RefreshKnownPoints();
            
            // Find escape zone
            _escapeZone = FindObjectOfType<EscapeZone>();
        }
        
        private void RefreshKnownPoints()
        {
            _knownLootPoints.Clear();
            _knownLootPoints.AddRange(FindObjectsOfType<LootPoint>());
            
            _knownCoverPoints.Clear();
            _knownCoverPoints.AddRange(FindObjectsOfType<CoverPoint>());
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
            float closestCopDistance = float.MaxValue;
            Vector3 closestCopPosition = Vector3.zero;
            
            // Cache cop references to avoid expensive FindObjectsOfType every frame
            // Refresh cache every 2 seconds or on first call
            if (_cachedCops == null || Time.time - _lastCopCacheTime > 2f)
            {
                _cachedCops = FindObjectsOfType<CopNPC>();
                _lastCopCacheTime = Time.time;
            }
            
            foreach (var copNPC in _cachedCops)
            {
                if (copNPC == null || !copNPC.gameObject.activeSelf) continue;
                
                float distance = Vector3.Distance(transform.position, copNPC.transform.position);
                if (distance < closestCopDistance)
                {
                    closestCopDistance = distance;
                    closestCopPosition = copNPC.transform.position;
                }
                
                // Check if we can see this cop (simple line-of-sight)
                if (distance <= _copDetectionRange)
                {
                    Vector3 dirToCop = (copNPC.transform.position - transform.position).normalized;
                    if (!Physics.Raycast(transform.position + Vector3.up, dirToCop, distance - 0.5f))
                    {
                        canSeeCop = true;
                        _lastCopSightTime = Time.time;
                    }
                }
            }
            
            Blackboard.Set("canSeeCop", canSeeCop);
            Blackboard.Set("closestCopDistance", closestCopDistance);
            if (closestCopPosition != Vector3.zero)
            {
                Blackboard.Set("closestCopPosition", closestCopPosition);
            }
        }
        
        private void UpdateFearLevel()
        {
            float fearLevel = Blackboard.Get("fearLevel", 0f);
            
            if (Blackboard.Get("canSeeCop", false))
            {
                // Increase fear when we see a cop
                float copDist = Blackboard.Get("closestCopDistance", 100f);
                float proximityFear = Mathf.Clamp01(1f - (copDist / _copDetectionRange));
                fearLevel = Mathf.MoveTowards(fearLevel, 0.5f + proximityFear * 0.5f, Time.deltaTime * 2f);
            }
            else
            {
                // Decay fear when no cop visible
                fearLevel = Mathf.MoveTowards(fearLevel, 0f, Time.deltaTime * 0.3f);
            }
            
            Blackboard.Set("fearLevel", fearLevel);
        }
        
        private void TryEscape()
        {
            if (!_isCarryingLoot || _escapeZone == null) return;
            
            if (_escapeZone.TryEscape(gameObject, _carriedLootValue))
            {
                _hasEscaped = true;
                Blackboard.Set("currentState", "Escaped!");
                
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
            Blackboard.Set("hasLoot", false);
            Blackboard.Set("lootValue", 0);
            Blackboard.Set("currentState", "Arrested!");
            
            Debug.Log($"<color=blue>[CopsAndRobbers] {name} was arrested!</color>");
            
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
                Blackboard.Set("hasLoot", true);
                Blackboard.Set("lootValue", _carriedLootValue);
                
                // Show loot bag visual
                var bag = transform.Find("LootBag");
                if (bag != null) bag.gameObject.SetActive(true);
                
                Debug.Log($"<color=yellow>[CopsAndRobbers] {name} stole loot worth ${loot.Value}!</color>");
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
                new SetBlackboard("lastFleeTime", () => Time.time),
                new SetBlackboard("currentState", "Flee!"),
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
                new BlackboardConsideration<bool>("SeesCop", "canSeeCop",
                    sees => sees ? 1f : 0f, false),
                // Higher score when cop is close
                new BlackboardConsideration<float>("CopProximity", "closestCopDistance",
                    dist => Mathf.Clamp01(1f - (dist / _copDetectionRange)), 100f),
                // Higher score when fear is high
                new BlackboardConsideration<float>("FearForFlee", "fearLevel",
                    f => 0.5f + f * 0.5f, 0f)
            );
        }
        
        private UtilityAction CreateCarryToEscapeAction()
        {
            var carryBehavior = new Sequence(
                new SetBlackboard("currentState", "Escaping"),
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
                new BlackboardConsideration<bool>("HasLoot", "hasLoot",
                    has => has ? 1f : 0f, false),
                // Higher score when no cop visible
                new BlackboardConsideration<bool>("NoCopForEscape", "canSeeCop",
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
                new SetBlackboard("lastStealTime", () => Time.time),
                new SetBlackboard("currentState", "Stealing"),
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
                new BlackboardConsideration<bool>("NoLootYet", "hasLoot",
                    has => has ? 0f : 1f, false),
                // Must not see cop (too risky)
                new BlackboardConsideration<bool>("NoCopForSteal", "canSeeCop",
                    sees => sees ? 0f : 1f, false),
                // Has a target loot nearby
                new ConstantConsideration(0.8f), // Base score if conditions met
                // Lower score when fear is high
                new BlackboardConsideration<float>("LowFearForSteal", "fearLevel",
                    f => 1f - f * 0.7f, 0f),
                // Cooldown
                new TimeConsideration("StealCooldown", "lastStealTime", 3f)
            );
        }
        
        private UtilityAction CreateHideAction()
        {
            var hideBehavior = new Sequence(
                new SetBlackboard("lastHideTime", () => Time.time),
                new SetBlackboard("currentState", "Hiding"),
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
                new BlackboardConsideration<float>("FearForHide", "fearLevel",
                    f => f > 0.3f ? 0.5f + f * 0.5f : 0.2f, 0f),
                // Not seeing cop but was recently
                new BlackboardConsideration<bool>("NoCopNow", "canSeeCop",
                    sees => sees ? 0.3f : 1f, false),
                // Cooldown
                new TimeConsideration("HideCooldown", "lastHideTime", 5f)
            );
        }
        
        private UtilityAction CreateSneakAction()
        {
            var sneakBehavior = new Sequence(
                new SetBlackboard("lastSneakTime", () => Time.time),
                new SetBlackboard("currentState", "Sneaking"),
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
                new BlackboardConsideration<bool>("NoCopForSneak", "canSeeCop",
                    sees => sees ? 0f : 1f, false),
                // Moderate fear (cautious)
                new BlackboardConsideration<float>("ModerateFear", "fearLevel",
                    f => f > 0.1f && f < 0.6f ? 0.8f : 0.3f, 0f),
                // Cooldown
                new TimeConsideration("SneakCooldown", "lastSneakTime", 4f)
            );
        }
        
        private UtilityAction CreateScoutAction()
        {
            var scoutBehavior = new Sequence(
                new SetBlackboard("lastScoutTime", () => Time.time),
                new SetBlackboard("currentState", "Scouting"),
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
                new BlackboardConsideration<float>("LowFearForScout", "fearLevel",
                    f => 1f - f * 0.8f, 0f),
                // Cooldown
                new TimeConsideration("ScoutCooldown", "lastScoutTime", 3f)
            );
        }
        
        private Vector3 GetFleePosition()
        {
            // Flee away from the closest cop
            Vector3 copPos = Blackboard.Get("closestCopPosition", transform.position);
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
            float nearestDist = float.MaxValue;
            
            foreach (var loot in _knownLootPoints)
            {
                if (loot == null || loot.IsStolen) continue;
                
                float dist = Vector3.Distance(transform.position, loot.transform.position);
                if (dist < nearestDist && dist <= _lootDetectionRange)
                {
                    nearestDist = dist;
                    nearest = loot;
                }
            }
            
            return nearest;
        }
        
        private void TryStealTargetLoot()
        {
            if (_targetLoot != null && !_targetLoot.IsStolen)
            {
                float dist = Vector3.Distance(transform.position, _targetLoot.transform.position);
                if (dist <= _targetLoot.StealRadius)
                {
                    PickupLoot(_targetLoot);
                }
            }
        }
        
        private Vector3 GetNearestCoverPosition()
        {
            CoverPoint nearest = null;
            float nearestDist = float.MaxValue;
            
            foreach (var cover in _knownCoverPoints)
            {
                if (cover == null || !cover.CanHide(gameObject)) continue;
                
                float dist = Vector3.Distance(transform.position, cover.transform.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
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
            robberObj.GetComponent<Renderer>().material.color = new Color(0.2f, 0.2f, 0.2f); // Dark color
            
            if (parent != null)
            {
                robberObj.transform.SetParent(parent);
            }
            
            // Add sight sensor for detecting cops
            var sightSensor = robberObj.AddComponent<SightSensor>();
            
            // Set robber to detect cops
            var targetTagField = typeof(SightSensor).GetField("_targetTag", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (targetTagField != null)
            {
                targetTagField.SetValue(sightSensor, "Cop");
            }
            
            // Add robber component
            var robber = robberObj.AddComponent<RobberNPC>();
            
            // Add mask indicator (robber's mask)
            var mask = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mask.name = "Mask";
            mask.transform.SetParent(robberObj.transform);
            mask.transform.localPosition = new Vector3(0f, 0.9f, 0.25f);
            mask.transform.localScale = new Vector3(0.5f, 0.2f, 0.1f);
            mask.GetComponent<Renderer>().material.color = Color.black;
            Object.Destroy(mask.GetComponent<Collider>());
            
            // Add loot bag indicator (shows when carrying)
            var bag = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bag.name = "LootBag";
            bag.transform.SetParent(robberObj.transform);
            bag.transform.localPosition = new Vector3(0.4f, 0.3f, 0f);
            bag.transform.localScale = new Vector3(0.3f, 0.4f, 0.2f);
            bag.GetComponent<Renderer>().material.color = new Color(0.4f, 0.3f, 0.1f);
            bag.SetActive(false); // Hidden until carrying loot
            Object.Destroy(bag.GetComponent<Collider>());
            
            return robber;
        }
    }
}
