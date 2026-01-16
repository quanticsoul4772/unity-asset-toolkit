using UnityEngine;

namespace SwarmAI
{
    /// <summary>
    /// Types of formations that agents can arrange into.
    /// </summary>
    public enum FormationType
    {
        /// <summary>No formation - agents move freely.</summary>
        None,
        /// <summary>Agents line up in a row.</summary>
        Line,
        /// <summary>Agents form a column (single file).</summary>
        Column,
        /// <summary>Agents form a circle around the leader.</summary>
        Circle,
        /// <summary>Agents form a wedge/arrow shape.</summary>
        Wedge,
        /// <summary>Agents form a V shape.</summary>
        V,
        /// <summary>Agents form a square/box.</summary>
        Box,
        /// <summary>Custom formation with user-defined positions.</summary>
        Custom
    }
    
    /// <summary>
    /// Represents a single slot in a formation.
    /// </summary>
    [System.Serializable]
    public struct FormationSlot
    {
        /// <summary>
        /// Local offset from the formation center/leader.
        /// </summary>
        public Vector3 LocalOffset;
        
        /// <summary>
        /// The agent assigned to this slot, or null if empty.
        /// </summary>
        [System.NonSerialized]
        public SwarmAgent AssignedAgent;
        
        /// <summary>
        /// Whether this slot is currently occupied.
        /// </summary>
        public bool IsOccupied => AssignedAgent != null;
        
        /// <summary>
        /// Priority for slot assignment (lower = higher priority).
        /// </summary>
        public int Priority;
        
        /// <summary>
        /// Create a formation slot with the given offset.
        /// </summary>
        public FormationSlot(Vector3 localOffset, int priority = 0)
        {
            LocalOffset = localOffset;
            AssignedAgent = null;
            Priority = priority;
        }
        
        /// <summary>
        /// Calculate the world position of this slot given the formation's transform.
        /// </summary>
        public Vector3 GetWorldPosition(Vector3 formationCenter, Quaternion formationRotation)
        {
            return formationCenter + formationRotation * LocalOffset;
        }
    }
}
