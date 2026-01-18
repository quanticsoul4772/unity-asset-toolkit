using System;
using UnityEngine;
using NPCBrain.Perception;

namespace NPCBrain.BehaviorTree.Conditions
{
    /// <summary>
    /// Condition node that checks if a target is visible to the NPC's SightSensor.
    /// </summary>
    /// <remarks>
    /// <para>Returns Success if the target is visible, Failure otherwise.</para>
    /// <para>Requires a SightSensor component on the NPC.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Chase only if target is visible
    /// var chaseIfSeen = new Sequence(
    ///     new CheckTargetVisible(),
    ///     new MoveTo(() => GetTargetPosition())
    /// );
    /// 
    /// // Check for specific target
    /// var checkPlayer = new CheckTargetVisible(brain => brain.Blackboard.Get&lt;GameObject&gt;("player"));
    /// </code>
    /// </example>
    public class CheckTargetVisible : BTNode
    {
        private readonly Func<NPCBrainController, GameObject> _getTarget;
        
        /// <summary>
        /// Creates a CheckTargetVisible condition that succeeds if ANY target is visible.
        /// </summary>
        public CheckTargetVisible()
        {
            _getTarget = null;
            Name = "CheckTargetVisible(Any)";
        }
        
        /// <summary>
        /// Creates a CheckTargetVisible condition that checks for a specific target.
        /// </summary>
        /// <param name="getTarget">Function to get the specific target to check for.</param>
        public CheckTargetVisible(Func<NPCBrainController, GameObject> getTarget)
        {
            _getTarget = getTarget ?? throw new ArgumentNullException(nameof(getTarget));
            Name = "CheckTargetVisible(Specific)";
        }
        
        protected override NodeStatus Tick(NPCBrainController brain)
        {
            if (brain == null)
            {
                return NodeStatus.Failure;
            }
            
            SightSensor sensor = brain.Perception;
            if (sensor == null)
            {
                sensor = brain.GetComponent<SightSensor>();
            }
            
            if (sensor == null)
            {
                NPCBrainDebug.LogWarning(NPCBrainDebug.Category.BehaviorTree, 
                    $"CheckTargetVisible: No SightSensor found on {brain.name}");
                return NodeStatus.Failure;
            }
            
            // Check for any visible target
            if (_getTarget == null)
            {
                return sensor.HasVisibleTargets ? NodeStatus.Success : NodeStatus.Failure;
            }
            
            // Check for specific target
            GameObject target = _getTarget(brain);
            if (target == null)
            {
                return NodeStatus.Failure;
            }
            
            // Use for loop instead of foreach to avoid enumerator allocation
            var visibleTargets = sensor.VisibleTargets;
            for (int i = 0; i < visibleTargets.Count; i++)
            {
                if (visibleTargets[i] == target)
                {
                    return NodeStatus.Success;
                }
            }
            
            return NodeStatus.Failure;
        }
    }
}
