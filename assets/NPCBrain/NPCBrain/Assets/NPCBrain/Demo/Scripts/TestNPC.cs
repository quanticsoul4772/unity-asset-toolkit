using UnityEngine;
using NPCBrain.BehaviorTree;
using NPCBrain.BehaviorTree.Composites;
using NPCBrain.BehaviorTree.Actions;
using NPCBrain.BehaviorTree.Conditions;

namespace NPCBrain.Demo
{
    /// <summary>
    /// Demo NPC that patrols waypoints or chases targets.
    /// Demonstrates basic behavior tree usage with Selector and Sequence nodes.
    /// </summary>
    /// <remarks>
    /// <para>Behavior priority:</para>
    /// <list type="number">
    ///   <item><description>If "hasTarget" is set in blackboard, move to "targetPosition"</description></item>
    ///   <item><description>Otherwise, patrol through waypoints</description></item>
    /// </list>
    /// </remarks>
    public class TestNPC : NPCBrainController
    {
        [Header("Test Settings")]
        [SerializeField] private float _waypointWaitTime = 2f;
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _arrivalDistance = 0.5f;
        
        /// <inheritdoc/>
        protected override BTNode CreateBehaviorTree()
        {
            return new Selector(
                // Priority 1: Chase target if we have one
                new Sequence(
                    new CheckBlackboard("hasTarget"),
                    new MoveTo(
                        () => Blackboard.Get<Vector3>("targetPosition"),
                        _arrivalDistance,
                        _moveSpeed
                    )
                ),
                // Priority 2: Patrol waypoints
                new Sequence(
                    new MoveTo(
                        () => GetCurrentWaypoint(),
                        _arrivalDistance,
                        _moveSpeed
                    ),
                    new Wait(_waypointWaitTime),
                    new AdvanceWaypoint()
                )
            );
        }
    }
}