using UnityEngine;
using NPCBrain.BehaviorTree;
using NPCBrain.BehaviorTree.Composites;
using NPCBrain.BehaviorTree.Actions;
using NPCBrain.BehaviorTree.Conditions;

namespace NPCBrain.Demo
{
    public class TestNPC : NPCBrainController
    {
        [Header("Test Settings")]
        [SerializeField] private float _waypointWaitTime = 2f;
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _arrivalDistance = 0.5f;
        
        protected override BTNode CreateBehaviorTree()
        {
            return new Selector(
                new Sequence(
                    new CheckBlackboard("hasTarget"),
                    new MoveTo(
                        () => Blackboard.Get<Vector3>("targetPosition"),
                        _arrivalDistance,
                        _moveSpeed
                    )
                ),
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
    
    public class AdvanceWaypoint : BTNode
    {
        public AdvanceWaypoint()
        {
            Name = "AdvanceWaypoint";
        }
        
        protected override NodeStatus Tick(NPCBrainController brain)
        {
            if (brain.WaypointPath != null)
            {
                brain.WaypointPath.Advance();
            }
            return NodeStatus.Success;
        }
    }
}