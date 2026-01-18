using UnityEngine;
using NPCBrain.BehaviorTree;
using NPCBrain.BehaviorTree.Composites;
using NPCBrain.BehaviorTree.Actions;
using NPCBrain.UtilityAI;
using NPCBrain.UtilityAI.Curves;

namespace NPCBrain.Archetypes
{
    /// <summary>
    /// NPC archetype that uses Utility AI for all decisions.
    /// Demonstrates the Criticality system with temperature/inertia adaptation.
    /// </summary>
    /// <remarks>
    /// <para>This NPC has multiple utility-scored actions:</para>
    /// <list type="bullet">
    ///   <item><description>Wander - Random exploration when bored</description></item>
    ///   <item><description>Rest - Stop and wait when tired</description></item>
    ///   <item><description>SeekInterest - Move toward interesting points</description></item>
    ///   <item><description>Patrol - Follow waypoints</description></item>
    /// </list>
    /// <para>The Criticality system adjusts temperature based on action variety:</para>
    /// <list type="bullet">
    ///   <item><description>Repetitive behavior → Higher temperature → More exploration</description></item>
    ///   <item><description>Varied behavior → Lower temperature → More exploitation</description></item>
    /// </list>
    /// </remarks>
    public class UtilityNPC : NPCBrainController
    {
        [Header("Utility NPC Settings")]
        [SerializeField] private float _moveSpeed = 3f;
        [SerializeField] private float _wanderRadius = 8f;
        [SerializeField] private float _restDuration = 2f;
        [SerializeField] private float _arrivalDistance = 0.5f;
        
        [Header("Behavior Weights")]
        [SerializeField] private float _wanderWeight = 0.5f;
        [SerializeField] private float _restWeight = 0.3f;
        [SerializeField] private float _patrolWeight = 0.7f;
        [SerializeField] private float _seekInterestWeight = 0.8f;
        
        private Vector3 _homePosition;
        private Vector3 _currentWanderTarget;
        private float _energy = 1f;
        private const float EnergyDecayRate = 0.05f;
        private const float EnergyRecoveryRate = 0.2f;
        
        protected override void Awake()
        {
            base.Awake();
            _homePosition = transform.position;
            _currentWanderTarget = GetRandomWanderPoint();
            
            Blackboard.Set("homePosition", _homePosition);
            Blackboard.Set("energy", _energy);
            Blackboard.Set("lastWanderTime", -10f);
            Blackboard.Set("lastRestTime", -10f);
            Blackboard.Set("lastPatrolTime", -10f);
            Blackboard.Set("lastSeekTime", -10f);
        }
        
        private void LateUpdate()
        {
            // Update energy based on movement
            if (LastStatus == NodeStatus.Running)
            {
                _energy = Mathf.Max(0f, _energy - EnergyDecayRate * Time.deltaTime);
            }
            Blackboard.Set("energy", _energy);
        }
        
        /// <inheritdoc/>
        protected override BTNode CreateBehaviorTree()
        {
            // Create utility actions with various considerations
            var wanderAction = CreateWanderAction();
            var restAction = CreateRestAction();
            var patrolAction = CreatePatrolAction();
            var seekInterestAction = CreateSeekInterestAction();
            
            // Use UtilitySelector - this triggers the Criticality system!
            return new UtilitySelector(
                wanderAction,
                restAction,
                patrolAction,
                seekInterestAction
            );
        }
        
        private UtilityAction CreateWanderAction()
        {
            var moveBehavior = new Sequence(
                new SetBlackboard("lastWanderTime", () => Time.time),
                new MoveTo(
                    () => GetOrRefreshWanderTarget(),
                    _arrivalDistance,
                    _moveSpeed,
                    10f
                )
            );
            moveBehavior.Name = "WanderBehavior";
            
            return new UtilityAction(
                "Wander",
                moveBehavior,
                _wanderWeight,
                // More likely when we haven't wandered recently
                new TimeConsideration("WanderCooldown", "lastWanderTime", 5f),
                // More likely when we have energy
                new BlackboardConsideration<float>("EnergyCheck", "energy", 0.7f, 1f)
            );
        }
        
        private UtilityAction CreateRestAction()
        {
            var restBehavior = new Sequence(
                new SetBlackboard("lastRestTime", () => Time.time),
                new Wait(_restDuration, () => RecoverEnergy())
            );
            restBehavior.Name = "RestBehavior";
            
            return new UtilityAction(
                "Rest",
                restBehavior,
                _restWeight,
                // More likely when energy is low (inverted - low energy = high score)
                new BlackboardConsideration<float>("TiredCheck", "energy", 0f, 0.5f, new ExponentialCurve(2f, true)),
                // Cooldown
                new TimeConsideration("RestCooldown", "lastRestTime", 8f)
            );
        }
        
        private UtilityAction CreatePatrolAction()
        {
            var patrolBehavior = new Sequence(
                new SetBlackboard("lastPatrolTime", () => Time.time),
                new MoveTo(
                    () => GetCurrentWaypoint(),
                    _arrivalDistance,
                    _moveSpeed
                ),
                new Wait(1f),
                new AdvanceWaypoint()
            );
            patrolBehavior.Name = "PatrolBehavior";
            
            return new UtilityAction(
                "Patrol",
                patrolBehavior,
                _patrolWeight,
                // More likely when we have waypoints and haven't patrolled recently
                new ConstantConsideration(0.8f),
                new TimeConsideration("PatrolCooldown", "lastPatrolTime", 3f),
                // Moderate energy needed
                new BlackboardConsideration<float>("EnergyForPatrol", "energy", 0.3f, 1f)
            );
        }
        
        private UtilityAction CreateSeekInterestAction()
        {
            var seekBehavior = new Sequence(
                new SetBlackboard("lastSeekTime", () => Time.time),
                new MoveTo(
                    () => GetInterestPoint(),
                    _arrivalDistance,
                    _moveSpeed * 1.2f,
                    8f
                ),
                new Wait(1.5f)
            );
            seekBehavior.Name = "SeekInterestBehavior";
            
            return new UtilityAction(
                "SeekInterest",
                seekBehavior,
                _seekInterestWeight,
                // Has an interest point
                new BlackboardConsideration("HasInterest", "interestPoint"),
                // Distance consideration - closer interest points are more attractive
                new DistanceConsideration(
                    "InterestDistance",
                    brain => brain.Blackboard.Get("interestPoint", brain.transform.position),
                    _wanderRadius * 2f,
                    true
                ),
                // Cooldown
                new TimeConsideration("SeekCooldown", "lastSeekTime", 4f)
            );
        }
        
        private Vector3 GetRandomWanderPoint()
        {
            Vector2 randomCircle = Random.insideUnitCircle * _wanderRadius;
            Vector3 wanderPoint = _homePosition + new Vector3(randomCircle.x, 0f, randomCircle.y);
            return wanderPoint;
        }
        
        private Vector3 GetOrRefreshWanderTarget()
        {
            // Refresh if we're close to current target
            if (Vector3.Distance(transform.position, _currentWanderTarget) < _arrivalDistance * 2f)
            {
                _currentWanderTarget = GetRandomWanderPoint();
            }
            return _currentWanderTarget;
        }
        
        private Vector3 GetInterestPoint()
        {
            return Blackboard.Get("interestPoint", _homePosition);
        }
        
        private void RecoverEnergy()
        {
            _energy = Mathf.Min(1f, _energy + EnergyRecoveryRate * _restDuration);
            Blackboard.Set("energy", _energy);
        }
        
        /// <summary>
        /// Sets a point of interest for the NPC to investigate.
        /// </summary>
        public void SetInterestPoint(Vector3 point)
        {
            Blackboard.Set("interestPoint", point);
        }
        
        /// <summary>
        /// Clears the current interest point.
        /// </summary>
        public void ClearInterestPoint()
        {
            Blackboard.Remove("interestPoint");
        }
        
        /// <summary>Current energy level (0-1).</summary>
        public float Energy => _energy;
        
        /// <summary>Home position for wander calculations.</summary>
        public Vector3 HomePosition => _homePosition;
    }
}
