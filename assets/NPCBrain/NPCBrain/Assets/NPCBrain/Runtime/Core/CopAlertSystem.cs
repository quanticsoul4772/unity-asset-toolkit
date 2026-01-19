using UnityEngine;

namespace NPCBrain
{
    /// <summary>
    /// Static alert system for cops to share robber sightings.
    /// When one cop sees a robber, all cops can access the shared intel.
    /// </summary>
    public static class CopAlertSystem
    {
        private static Vector3 _lastKnownRobberPosition;
        private static float _lastSightingTime = -100f;
        private static GameObject _lastSeenRobber;
        private static bool _hasActiveAlert;
        
        /// <summary>How long shared intel remains valid (seconds).</summary>
        public const float AlertValidDuration = 8f;
        
        /// <summary>Position where robber was last seen by any cop.</summary>
        public static Vector3 LastKnownRobberPosition => _lastKnownRobberPosition;
        
        /// <summary>Time when robber was last seen.</summary>
        public static float LastSightingTime => _lastSightingTime;
        
        /// <summary>The robber that was last seen.</summary>
        public static GameObject LastSeenRobber => _lastSeenRobber;
        
        /// <summary>Whether there is an active alert (recent sighting).</summary>
        public static bool HasActiveAlert => _hasActiveAlert && (Time.time - _lastSightingTime) < AlertValidDuration;
        
        /// <summary>Time since last sighting.</summary>
        public static float TimeSinceLastSighting => Time.time - _lastSightingTime;
        
        /// <summary>
        /// Broadcasts a robber sighting to all cops.
        /// Call this when a cop sees a robber.
        /// </summary>
        /// <param name="robberPosition">Current position of the robber.</param>
        /// <param name="robber">The robber GameObject.</param>
        public static void BroadcastRobberSighting(Vector3 robberPosition, GameObject robber)
        {
            _lastKnownRobberPosition = robberPosition;
            _lastSightingTime = Time.time;
            _lastSeenRobber = robber;
            _hasActiveAlert = true;
            
            NPCBrainDebug.Log(NPCBrainDebug.Category.General, 
                $"[CopAlertSystem] ALERT: Robber spotted at {robberPosition}!", null);
        }
        
        /// <summary>
        /// Clears the current alert (e.g., when robber is arrested).
        /// </summary>
        public static void ClearAlert()
        {
            _hasActiveAlert = false;
            _lastSeenRobber = null;
            
            NPCBrainDebug.Log(NPCBrainDebug.Category.General, 
                "[CopAlertSystem] Alert cleared.", null);
        }
        
        /// <summary>
        /// Checks if a position is near the last known robber position.
        /// </summary>
        /// <param name="position">Position to check.</param>
        /// <param name="radius">Radius to consider "near".</param>
        /// <returns>True if within radius of last known position.</returns>
        public static bool IsNearLastKnownPosition(Vector3 position, float radius = 5f)
        {
            if (!HasActiveAlert) return false;
            
            float distSqr = (position - _lastKnownRobberPosition).sqrMagnitude;
            return distSqr <= radius * radius;
        }
        
        /// <summary>
        /// Gets the direction from a position toward the last known robber position.
        /// </summary>
        public static Vector3 GetDirectionToLastKnown(Vector3 fromPosition)
        {
            if (!HasActiveAlert) return Vector3.zero;
            return (_lastKnownRobberPosition - fromPosition).normalized;
        }
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _lastKnownRobberPosition = Vector3.zero;
            _lastSightingTime = -100f;
            _lastSeenRobber = null;
            _hasActiveAlert = false;
        }
    }
}
