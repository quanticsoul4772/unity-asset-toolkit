using UnityEngine;
using NPCBrain.BehaviorTree;
using NPCBrain.BehaviorTree.Composites;
using NPCBrain.BehaviorTree.Actions;

namespace NPCBrain.Archetypes
{
    /// <summary>
    /// Simple patrol NPC archetype that follows a waypoint path.
    /// </summary>
    /// <remarks>
    /// <para>This is the simplest NPC archetype - it just patrols between waypoints
    /// with configurable speed and wait times.</para>
    /// <para>Use this as a starting point for more complex NPCs, or for background
    /// characters that just need to walk a patrol route.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Setup in Unity:
    /// // 1. Add PatrolNPC component to a GameObject
    /// // 2. Create empty GameObjects as waypoints
    /// // 3. Add WaypointPath component and assign waypoints
    /// // 4. Assign WaypointPath to PatrolNPC
    /// // 5. Press Play!
    /// </code>
    /// </example>
    public class PatrolNPC : NPCBrainController
    {
        [Header("Patrol Settings")]
        [Tooltip("Movement speed while patrolling")]
        [SerializeField] private float _patrolSpeed = 3f;
        
        [Tooltip("How close to a waypoint before considered 'arrived'")]
        [SerializeField] private float _arrivalDistance = 0.5f;
        
        [Tooltip("Time to wait at each waypoint")]
        [SerializeField] private float _waypointWaitTime = 2f;
        

        [Header("Optional Features")]
        [Tooltip("Random variation added to wait time (0 = none)")]
        [SerializeField] private float _waitTimeVariation = 0.5f;
        
        [Tooltip("Random variation added to speed (0 = none)")]
        [SerializeField] private float _speedVariation = 0f;
        
        private float _currentWaitTime;
        private float _currentSpeed;
        
        protected override void Awake()
        {
            base.Awake();
            RandomizeParameters();
        }
        
        private void RandomizeParameters()
        {
            _currentWaitTime = _waypointWaitTime + Random.Range(-_waitTimeVariation, _waitTimeVariation);
            _currentWaitTime = Mathf.Max(0.1f, _currentWaitTime);
            
            _currentSpeed = _patrolSpeed + Random.Range(-_speedVariation, _speedVariation);
            _currentSpeed = Mathf.Max(0.5f, _currentSpeed);
        }
        
        /// <inheritdoc/>
        protected override BTNode CreateBehaviorTree()
        {
            /*
             * Simple Patrol Behavior Tree:
             * 
             * Sequence (repeats via behavior tree tick)
             * ├── MoveTo(current waypoint)
             * ├── Wait(random wait time)
             * └── AdvanceWaypoint
             */
            
            var tree = new Sequence(
                new MoveTo(
                    () => GetCurrentWaypoint(),
                    _arrivalDistance,
                    _currentSpeed
                ),
                new Wait(_currentWaitTime),
                new AdvanceWaypoint(),
                new RandomizeWait(this)
            );
            tree.Name = "PatrolSequence";
            return tree;
        }
        
        /// <summary>
        /// Gets the current patrol speed.
        /// </summary>
        public float PatrolSpeed => _currentSpeed;
        
        /// <summary>
        /// Gets the current wait time at waypoints.
        /// </summary>
        public float WaitTime => _currentWaitTime;
        
        /// <summary>
        /// Custom action to randomize wait time after each waypoint.
        /// </summary>
        private class RandomizeWait : BTNode
        {
            private readonly PatrolNPC _patrol;
            
            public RandomizeWait(PatrolNPC patrol)
            {
                _patrol = patrol;
                Name = "RandomizeWait";
            }
            
            protected override NodeStatus Tick(NPCBrainController brain)
            {
                _patrol.RandomizeParameters();
                return NodeStatus.Success;
            }
        }
    }
}
