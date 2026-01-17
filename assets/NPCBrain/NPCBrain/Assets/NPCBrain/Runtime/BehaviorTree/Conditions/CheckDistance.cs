using System;
using UnityEngine;

namespace NPCBrain.BehaviorTree.Conditions
{
    /// <summary>
    /// Condition node that checks if the distance to a target is within a specified range.
    /// </summary>
    /// <remarks>
    /// <para>Returns Success if the target is within the specified distance, Failure otherwise.</para>
    /// <para>Useful for range-based AI decisions like chase range, attack range, or detection range.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Only chase if target is within 20 units
    /// var chaseIfClose = new Sequence(
    ///     new CheckDistance(
    ///         brain => brain.transform.position,
    ///         brain => brain.Blackboard.Get&lt;Vector3&gt;("targetPosition"),
    ///         20f,
    ///         CheckDistance.ComparisonType.LessThanOrEqual
    ///     ),
    ///     new MoveTo(() => GetTargetPosition())
    /// );
    /// </code>
    /// </example>
    public class CheckDistance : BTNode
    {
        /// <summary>
        /// Type of distance comparison to perform.
        /// </summary>
        public enum ComparisonType
        {
            /// <summary>Distance must be less than threshold.</summary>
            LessThan,
            /// <summary>Distance must be less than or equal to threshold.</summary>
            LessThanOrEqual,
            /// <summary>Distance must be greater than threshold.</summary>
            GreaterThan,
            /// <summary>Distance must be greater than or equal to threshold.</summary>
            GreaterThanOrEqual
        }
        
        private readonly Func<NPCBrainController, Vector3> _getSourcePosition;
        private readonly Func<NPCBrainController, Vector3> _getTargetPosition;
        private readonly float _threshold;
        private readonly ComparisonType _comparison;
        
        /// <summary>
        /// Creates a new CheckDistance condition using the NPC's position as source.
        /// </summary>
        /// <param name="getTargetPosition">Function to get the target position.</param>
        /// <param name="threshold">Distance threshold to check against.</param>
        /// <param name="comparison">Type of comparison (default: LessThanOrEqual).</param>
        public CheckDistance(
            Func<NPCBrainController, Vector3> getTargetPosition,
            float threshold,
            ComparisonType comparison = ComparisonType.LessThanOrEqual)
            : this(brain => brain.transform.position, getTargetPosition, threshold, comparison)
        {
        }
        
        /// <summary>
        /// Creates a new CheckDistance condition with custom source and target positions.
        /// </summary>
        /// <param name="getSourcePosition">Function to get the source position.</param>
        /// <param name="getTargetPosition">Function to get the target position.</param>
        /// <param name="threshold">Distance threshold to check against.</param>
        /// <param name="comparison">Type of comparison.</param>
        public CheckDistance(
            Func<NPCBrainController, Vector3> getSourcePosition,
            Func<NPCBrainController, Vector3> getTargetPosition,
            float threshold,
            ComparisonType comparison)
        {
            _getSourcePosition = getSourcePosition ?? throw new ArgumentNullException(nameof(getSourcePosition));
            _getTargetPosition = getTargetPosition ?? throw new ArgumentNullException(nameof(getTargetPosition));
            _threshold = threshold;
            _comparison = comparison;
            Name = $"CheckDistance({comparison}, {threshold})";
        }
        
        /// <inheritdoc/>
        protected override NodeStatus Tick(NPCBrainController brain)
        {
            Vector3 source = _getSourcePosition(brain);
            Vector3 target = _getTargetPosition(brain);
            float distance = Vector3.Distance(source, target);
            
            bool result = _comparison switch
            {
                ComparisonType.LessThan => distance < _threshold,
                ComparisonType.LessThanOrEqual => distance <= _threshold,
                ComparisonType.GreaterThan => distance > _threshold,
                ComparisonType.GreaterThanOrEqual => distance >= _threshold,
                _ => false
            };
            
            return result ? NodeStatus.Success : NodeStatus.Failure;
        }
    }
}
