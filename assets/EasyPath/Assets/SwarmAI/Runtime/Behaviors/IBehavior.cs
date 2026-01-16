using UnityEngine;

namespace SwarmAI
{
    /// <summary>
    /// Interface for steering behaviors that can be added to SwarmAgents.
    /// Implement this interface to create custom behaviors.
    /// </summary>
    public interface IBehavior
    {
        /// <summary>
        /// Display name of the behavior.
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// Weight multiplier for this behavior's force.
        /// Higher weight = more influence on agent movement.
        /// </summary>
        float Weight { get; set; }
        
        /// <summary>
        /// Whether this behavior is currently active.
        /// Inactive behaviors return zero force.
        /// </summary>
        bool IsActive { get; set; }
        
        /// <summary>
        /// Calculate the steering force for this behavior.
        /// </summary>
        /// <param name="agent">The agent to calculate force for.</param>
        /// <returns>The steering force vector.</returns>
        Vector3 CalculateForce(SwarmAgent agent);
    }
    
    /// <summary>
    /// Base class for steering behaviors with common functionality.
    /// Derive from this class for easier behavior implementation.
    /// </summary>
    public abstract class BehaviorBase : IBehavior
    {
        /// <inheritdoc/>
        public abstract string Name { get; }
        
        /// <inheritdoc/>
        public float Weight { get; set; } = 1f;
        
        /// <inheritdoc/>
        public bool IsActive { get; set; } = true;
        
        /// <inheritdoc/>
        public abstract Vector3 CalculateForce(SwarmAgent agent);
        
        /// <summary>
        /// Helper to calculate seek steering toward a target.
        /// Returns zero if agent is null or already at target.
        /// </summary>
        protected Vector3 Seek(SwarmAgent agent, Vector3 targetPosition)
        {
            if (agent == null) return Vector3.zero;
            
            Vector3 toTarget = targetPosition - agent.Position;
            if (toTarget.sqrMagnitude < SwarmSettings.DefaultPositionEqualityThresholdSq) return Vector3.zero;
            
            Vector3 desired = toTarget.normalized * agent.MaxSpeed;
            return desired - agent.Velocity;
        }
        
        /// <summary>
        /// Helper to calculate flee steering away from a position.
        /// Returns zero if agent is null or at the same position as threat.
        /// </summary>
        protected Vector3 Flee(SwarmAgent agent, Vector3 threatPosition)
        {
            if (agent == null) return Vector3.zero;
            
            Vector3 fromThreat = agent.Position - threatPosition;
            if (fromThreat.sqrMagnitude < SwarmSettings.DefaultPositionEqualityThresholdSq) return Vector3.zero;
            
            Vector3 desired = fromThreat.normalized * agent.MaxSpeed;
            return desired - agent.Velocity;
        }
        
        /// <summary>
        /// Helper to truncate a vector to a maximum length.
        /// </summary>
        protected Vector3 Truncate(Vector3 vector, float maxLength)
        {
            if (vector.sqrMagnitude > maxLength * maxLength)
            {
                return vector.normalized * maxLength;
            }
            return vector;
        }
    }
}
