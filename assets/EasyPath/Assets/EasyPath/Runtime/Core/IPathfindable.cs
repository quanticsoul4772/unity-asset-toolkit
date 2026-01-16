using UnityEngine;

namespace EasyPath
{
    /// <summary>
    /// Interface for objects that can follow paths.
    /// </summary>
    public interface IPathfindable
    {
        /// <summary>
        /// Set the destination and start moving.
        /// </summary>
        /// <param name="destination">Target position in world space.</param>
        /// <returns>True if a valid path was found.</returns>
        bool SetDestination(Vector3 destination);
        
        /// <summary>
        /// Stop moving and clear the current path.
        /// </summary>
        void Stop();
        
        /// <summary>
        /// Whether the agent is currently moving.
        /// </summary>
        bool IsMoving { get; }
        
        /// <summary>
        /// Whether the agent has a valid path.
        /// </summary>
        bool HasPath { get; }
        
        /// <summary>
        /// Remaining distance to destination.
        /// </summary>
        float RemainingDistance { get; }
        
        /// <summary>
        /// Current destination position.
        /// </summary>
        Vector3 Destination { get; }
    }
}
