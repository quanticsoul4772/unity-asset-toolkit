using System;
using UnityEngine;

namespace NPCBrain.BehaviorTree.Actions
{
    /// <summary>
    /// Action that rotates the NPC to face a target position.
    /// Returns Running while rotating, Success when facing the target.
    /// </summary>
    /// <example>
    /// <code>
    /// // Look at target before attacking
    /// var attackSequence = new Sequence(
    ///     new LookAt(brain => brain.Blackboard.Get&lt;Vector3&gt;("targetPosition")),
    ///     new Attack()
    /// );
    /// 
    /// // Quick snap to face direction
    /// var snapLook = new LookAt(brain => GetEnemyPosition(), 0f); // instant
    /// </code>
    /// </example>
    public class LookAt : BTNode
    {
        private readonly Func<NPCBrainController, Vector3> _getTargetPosition;
        private readonly float _rotationSpeed;
        private readonly float _angleTolerance;
        
        /// <summary>
        /// Creates a LookAt action.
        /// </summary>
        /// <param name="getTargetPosition">Function to get the position to look at.</param>
        /// <param name="rotationSpeed">Rotation speed in degrees per second. 0 = instant.</param>
        /// <param name="angleTolerance">Angle tolerance in degrees to consider "facing" (default: 5).</param>
        public LookAt(
            Func<NPCBrainController, Vector3> getTargetPosition, 
            float rotationSpeed = 360f,
            float angleTolerance = 5f)
        {
            _getTargetPosition = getTargetPosition ?? throw new ArgumentNullException(nameof(getTargetPosition));
            _rotationSpeed = rotationSpeed;
            _angleTolerance = angleTolerance;
            Name = "LookAt";
        }
        
        /// <summary>
        /// Creates a LookAt action that looks at a target GameObject.
        /// </summary>
        /// <param name="getTarget">Function to get the target GameObject.</param>
        /// <param name="rotationSpeed">Rotation speed in degrees per second. 0 = instant.</param>
        /// <param name="angleTolerance">Angle tolerance in degrees to consider "facing" (default: 5).</param>
        public LookAt(
            Func<NPCBrainController, GameObject> getTarget, 
            float rotationSpeed = 360f,
            float angleTolerance = 5f)
        {
            if (getTarget == null) throw new ArgumentNullException(nameof(getTarget));
            
            _getTargetPosition = brain =>
            {
                GameObject target = getTarget(brain);
                return target != null ? target.transform.position : brain.transform.position;
            };
            _rotationSpeed = rotationSpeed;
            _angleTolerance = angleTolerance;
            Name = "LookAt";
        }

        /// <summary>
        /// Rotates the NPC toward the target position.
        /// </summary>
        /// <param name="brain">The NPCBrainController to rotate.</param>
        /// <returns>
        /// - Success: When facing the target within angle tolerance
        /// - Running: While rotating toward target
        /// - Failure: If brain is null
        /// </returns>
        /// <remarks>
        /// Rotation is locked to horizontal plane (y-axis rotation only).
        /// Set rotation speed to 0 for instant snap rotation.
        /// </remarks>
        protected override NodeStatus Tick(NPCBrainController brain)
        {
            if (brain == null)
            {
                return NodeStatus.Failure;
            }
            
            Vector3 targetPosition = _getTargetPosition(brain);
            Vector3 direction = targetPosition - brain.transform.position;
            direction.y = 0; // Keep rotation on horizontal plane
            
            if (direction.sqrMagnitude < 0.001f)
            {
                // Target is at our position, nothing to look at
                return NodeStatus.Success;
            }
            
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            float angleToTarget = Quaternion.Angle(brain.transform.rotation, targetRotation);
            
            // Check if we're already facing the target
            if (angleToTarget <= _angleTolerance)
            {
                return NodeStatus.Success;
            }
            
            // Instant rotation
            if (_rotationSpeed <= 0f)
            {
                brain.transform.rotation = targetRotation;
                return NodeStatus.Success;
            }
            
            // Smooth rotation
            float step = _rotationSpeed * Time.deltaTime;
            brain.transform.rotation = Quaternion.RotateTowards(
                brain.transform.rotation, 
                targetRotation, 
                step);
            
            // Check if we've reached the target rotation
            angleToTarget = Quaternion.Angle(brain.transform.rotation, targetRotation);
            if (angleToTarget <= _angleTolerance)
            {
                return NodeStatus.Success;
            }
            
            return NodeStatus.Running;
        }
    }
}
