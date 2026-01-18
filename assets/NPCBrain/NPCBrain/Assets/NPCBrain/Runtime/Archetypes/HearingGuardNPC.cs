using UnityEngine;
using NPCBrain.BehaviorTree;
using NPCBrain.BehaviorTree.Composites;
using NPCBrain.BehaviorTree.Actions;
using NPCBrain.BehaviorTree.Conditions;
using NPCBrain.BehaviorTree.Decorators;
using NPCBrain.Perception;

namespace NPCBrain.Archetypes
{
    /// <summary>
    /// Guard NPC archetype that responds to both visual and audio stimuli.
    /// Patrols waypoints, investigates sounds, and chases visible targets.
    /// </summary>
    /// <remarks>
    /// <para>Behavior priority (highest to lowest):</para>
    /// <list type="number">
    ///   <item><description>Chase visible target</description></item>
    ///   <item><description>Investigate gunshots (high priority sounds)</description></item>
    ///   <item><description>Investigate footsteps (low priority sounds)</description></item>
    ///   <item><description>Return to patrol route</description></item>
    ///   <item><description>Patrol waypoints</description></item>
    /// </list>
    /// <para>Blackboard keys used:</para>
    /// <list type="bullet">
    ///   <item><description>"target" - Current chase target (GameObject)</description></item>
    ///   <item><description>"investigatePosition" - Position to investigate (Vector3)</description></item>
    ///   <item><description>"lastSoundType" - Type of last heard sound (SoundType)</description></item>
    ///   <item><description>"homePosition" - Starting position to return to (Vector3)</description></item>
    ///   <item><description>"alertLevel" - Current alert state (float, 0-1)</description></item>
    ///   <item><description>"currentState" - Current behavior state name (string)</description></item>
    /// </list>
    /// </remarks>
    public class HearingGuardNPC : NPCBrainController
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
        
        private Vector3 _homePosition;
        
        /// <summary>Current behavior state for UI display.</summary>
        public string CurrentState => Blackboard.Get("currentState", "Patrol");
        
        /// <summary>Current alert level (0-1).</summary>
        public float AlertLevel => Blackboard.Get("alertLevel", 0f);
        
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
            Blackboard.Set("currentState", "Chase");
        }
        
        private void HandleTargetLost(GameObject target)
        {
            if (Blackboard.Has("target"))
            {
                var currentTarget = Blackboard.Get<GameObject>("target");
                if (currentTarget == target)
                {
                    Blackboard.Remove("target");
                    Blackboard.Set("currentState", "Investigate");
                }
            }
        }
        
        private void HandleSoundHeard(SoundEvent sound)
        {
            // Only update investigate position if we don't have a visible target
            if (!Blackboard.Has("target"))
            {
                // Check if this sound is higher priority than current investigation
                bool shouldUpdate = true;
                if (Blackboard.Has("lastSoundType"))
                {
                    var currentType = (SoundType)Blackboard.Get<int>("lastSoundType");
                    // Only update if new sound is higher or equal priority
                    shouldUpdate = sound.Type >= currentType;
                }
                
                if (shouldUpdate)
                {
                    Blackboard.Set("investigatePosition", sound.Position);
                    Blackboard.Set("lastSoundType", (int)sound.Type);
                    
                    // Increase alert based on sound type
                    if (sound.Type >= SoundType.Gunshot)
                    {
                        IncreaseAlert(_gunshotAlertBoost);
                        Blackboard.Set("currentState", "Alert-Gunshot");
                    }
                    else if (sound.Type >= SoundType.Footstep)
                    {
                        IncreaseAlert(_footstepAlertBoost);
                        Blackboard.Set("currentState", "Alert-Footstep");
                    }
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
                Blackboard.Set("investigatePosition", target.transform.position);
                IncreaseAlert(_alertIncreaseRate * Time.deltaTime);
            }
        }
        
        /// <inheritdoc/>
        protected override BTNode CreateBehaviorTree()
        {
            return new Selector(
                // Priority 1: Chase visible target
                CreateChaseSequence(),
                
                // Priority 2: Investigate high-priority sounds (gunshots, explosions)
                CreateUrgentInvestigateSequence(),
                
                // Priority 3: Investigate low-priority sounds (footsteps)
                CreateCasualInvestigateSequence(),
                
                // Priority 4: Return to patrol route if alerted and far from home
                CreateReturnToPostSequence(),
                
                // Priority 5: Normal patrol
                CreatePatrolSequence()
            );
        }
        
        private BTNode CreateChaseSequence()
        {
            var sequence = new Sequence(
                new CheckBlackboard("target"),
                new CheckDistance(
                    brain => brain.transform.position,
                    brain => GetTargetPositionForCheck(brain),
                    _maxChaseDistance,
                    CheckDistance.ComparisonType.LessThanOrEqual
                ),
                new SetBlackboard("currentState", "Chase"),
                new MoveTo(
                    () => GetTargetPosition(),
                    _chaseArrivalDistance,
                    _chaseSpeed,
                    5f
                )
            );
            sequence.Name = "Chase";
            return sequence;
        }
        
        private BTNode CreateUrgentInvestigateSequence()
        {
            var sequence = new Sequence(
                // Has an investigate position
                new CheckBlackboard("investigatePosition"),
                // No current visible target
                new Inverter(new CheckBlackboard("target")),
                // Sound was high priority (gunshot or higher)
                new CheckBlackboard<int>("lastSoundType", type => type >= (int)SoundType.Gunshot),
                // Update state
                new SetBlackboard("currentState", "Investigate-Urgent"),
                // Go to sound location quickly
                new MoveTo(
                    () => Blackboard.Get<Vector3>("investigatePosition"),
                    _arrivalDistance,
                    _urgentInvestigateSpeed
                ),
                // Look around
                new Wait(_investigateTime * 0.5f),
                // Clear investigation
                new ClearBlackboardKey("investigatePosition"),
                new ClearBlackboardKey("lastSoundType")
            );
            sequence.Name = "UrgentInvestigate";
            return sequence;
        }
        
        private BTNode CreateCasualInvestigateSequence()
        {
            var sequence = new Sequence(
                // Has an investigate position
                new CheckBlackboard("investigatePosition"),
                // No current visible target
                new Inverter(new CheckBlackboard("target")),
                // Still somewhat alert
                new CheckBlackboard<float>("alertLevel", level => level > 0.1f),
                // Update state
                new SetBlackboard("currentState", "Investigate"),
                // Go to sound location
                new MoveTo(
                    () => Blackboard.Get<Vector3>("investigatePosition"),
                    _arrivalDistance,
                    _investigateSpeed
                ),
                // Look around
                new Wait(_investigateTime),
                // Clear investigation
                new ClearBlackboardKey("investigatePosition"),
                new ClearBlackboardKey("lastSoundType")
            );
            sequence.Name = "CasualInvestigate";
            return sequence;
        }
        
        private BTNode CreateReturnToPostSequence()
        {
            var sequence = new Sequence(
                // Not currently chasing or investigating
                new Inverter(new CheckBlackboard("target")),
                new Inverter(new CheckBlackboard("investigatePosition")),
                // Far from home position
                new CheckBlackboard<Vector3>("homePosition", 
                    pos => Vector3.Distance(transform.position, pos) > 3f),
                // Update state
                new SetBlackboard("currentState", "Return"),
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
                new SetBlackboard("currentState", "Patrol"),
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
