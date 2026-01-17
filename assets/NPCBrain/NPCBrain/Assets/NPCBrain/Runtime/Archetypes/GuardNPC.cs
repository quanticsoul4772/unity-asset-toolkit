using UnityEngine;
using NPCBrain.BehaviorTree;
using NPCBrain.BehaviorTree.Composites;
using NPCBrain.BehaviorTree.Actions;
using NPCBrain.BehaviorTree.Conditions;
using NPCBrain.BehaviorTree.Decorators;

namespace NPCBrain.Archetypes
{
    /// <summary>
    /// Guard NPC archetype that patrols waypoints, investigates suspicious activity,
    /// and chases visible targets before returning to patrol.
    /// </summary>
    /// <remarks>
    /// <para>Behavior priority (highest to lowest):</para>
    /// <list type="number">
    ///   <item><description>Chase visible target</description></item>
    ///   <item><description>Investigate last known position</description></item>
    ///   <item><description>Return to patrol route</description></item>
    ///   <item><description>Patrol waypoints</description></item>
    /// </list>
    /// <para>Blackboard keys used:</para>
    /// <list type="bullet">
    ///   <item><description>"target" - Current chase target (GameObject)</description></item>
    ///   <item><description>"lastKnownPosition" - Where target was last seen (Vector3)</description></item>
    ///   <item><description>"homePosition" - Starting position to return to (Vector3)</description></item>
    ///   <item><description>"alertLevel" - Current alert state (float, 0-1)</description></item>
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
        
        private Vector3 _homePosition;
        
        protected override void Awake()
        {
            base.Awake();
            _homePosition = transform.position;
            Blackboard.Set("homePosition", _homePosition);
            Blackboard.Set("alertLevel", 0f);
            
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
            /*
             * Guard Behavior Tree Structure:
             * 
             * Selector (priority fallback)
             * ├── Sequence: CHASE (if target visible and in range)
             * │   ├── CheckBlackboard("target")
             * │   ├── CheckTargetInRange (custom)
             * │   └── MoveTo(target position, chase speed)
             * │
             * ├── Sequence: INVESTIGATE (if last known position exists)
             * │   ├── CheckBlackboard("lastKnownPosition")
             * │   ├── Inverter(CheckBlackboard("target"))  // Only if no current target
             * │   ├── MoveTo(lastKnownPosition)
             * │   ├── Wait(investigate time)
             * │   └── ClearLastKnownPosition (custom)
             * │
             * ├── Sequence: RETURN TO POST (if far from home and alert)
             * │   ├── CheckAlertLevel (custom)
             * │   ├── CheckFarFromHome (custom)
             * │   └── MoveTo(homePosition)
             * │
             * └── Sequence: PATROL (default)
             *     ├── MoveTo(current waypoint)
             *     ├── Wait(waypointWaitTime)
             *     └── AdvanceWaypoint
             */
            
            return new Selector(
                // Priority 1: Chase visible target
                CreateChaseSequence(),
                
                // Priority 2: Investigate last known position
                CreateInvestigateSequence(),
                
                // Priority 3: Return to patrol route if alerted and far from home
                CreateReturnToPostSequence(),
                
                // Priority 4: Normal patrol
                CreatePatrolSequence()
            );
        }
        
        private BTNode CreateChaseSequence()
        {
            var sequence = new Sequence(
                new CheckBlackboard("target"),
                new CheckBlackboard<float>("alertLevel", level => level > 0.2f),
                // Check target is within chase range
                new CheckDistance(
                    brain => brain.transform.position,
                    brain => GetTargetPositionForCheck(brain),
                    _maxChaseDistance,
                    CheckDistance.ComparisonType.LessThanOrEqual
                ),
                new MoveTo(
                    () => GetTargetPosition(),
                    _chaseArrivalDistance,
                    _chaseSpeed,
                    5f // Short timeout - re-evaluate often
                )
            );
            sequence.Name = "Chase";
            return sequence;
        }
        
        private BTNode CreateInvestigateSequence()
        {
            var sequence = new Sequence(
                // Has a last known position to investigate
                new CheckBlackboard("lastKnownPosition"),
                // But no current visible target
                new Inverter(new CheckBlackboard("target")),
                // Still somewhat alert
                new CheckBlackboard<float>("alertLevel", level => level > 0.1f),
                // Go to last known position
                new MoveTo(
                    () => Blackboard.Get<Vector3>("lastKnownPosition"),
                    _arrivalDistance,
                    _investigateSpeed
                ),
                // Look around
                new Wait(_investigateTime),
                // Clear investigation target
                new ClearBlackboardKey("lastKnownPosition")
            );
            sequence.Name = "Investigate";
            return sequence;
        }
        
        private BTNode CreateReturnToPostSequence()
        {
            var sequence = new Sequence(
                // Not currently chasing or investigating
                new Inverter(new CheckBlackboard("target")),
                new Inverter(new CheckBlackboard("lastKnownPosition")),
                // Far from home position
                new CheckBlackboard<Vector3>("homePosition", 
                    pos => Vector3.Distance(transform.position, pos) > 3f),
                // Return home
                new MoveTo(
                    () => Blackboard.Get<Vector3>("homePosition"),
                    _arrivalDistance,
                    _patrolSpeed
                )
            );
            sequence.Name = "ReturnToPost";
            return sequence;
        }
        
        private BTNode CreatePatrolSequence()
        {
            var sequence = new Sequence(
                new MoveTo(
                    () => GetCurrentWaypoint(),
                    _arrivalDistance,
                    _patrolSpeed
                ),
                new Wait(_waypointWaitTime),
                new AdvanceWaypoint()
            );
            sequence.Name = "Patrol";
            return sequence;
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
            return brain.transform.position; // Same position = 0 distance = will fail range check
        }
    }
}
