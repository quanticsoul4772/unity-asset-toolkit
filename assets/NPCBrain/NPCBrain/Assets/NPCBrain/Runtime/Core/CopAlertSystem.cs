using UnityEngine;

namespace NPCBrain
{
    /// <summary>
    /// Static alert system for cops to share robber sightings.
    /// When one cop sees a robber, all cops can access the shared intel.
    /// Supports coordinated pursuit where closest cop chases while others intercept at escape zone.
    /// </summary>
    public static class CopAlertSystem
    {
        private static Vector3 _lastKnownRobberPosition;
        private static Vector3 _lastKnownRobberDirection;
        private static float _lastSightingTime = -100f;
        private static float _timeLostSight = -100f;
        private static GameObject _lastSeenRobber;
        private static bool _hasActiveAlert;
        private static bool _hasActivePursuit;
        private static float _lastNoDirectionLogTime = -100f;  // Throttle for "no direction" debug log
        
        // Coordinated pursuit: escape zone interception
        private static Vector3 _escapeZonePosition = Vector3.zero;
        
        /// <summary>How long shared intel remains valid (seconds).</summary>
        public const float AlertValidDuration = 8f;
        
        /// <summary>How long coordinated pursuit remains active (seconds).</summary>
        public const float PursuitValidDuration = 5f;
        
        /// <summary>Position where robber was last seen by any cop.</summary>
        public static Vector3 LastKnownRobberPosition => _lastKnownRobberPosition;
        
        /// <summary>Position of the escape zone. Non-closest cops will intercept here during pursuit.</summary>
        public static Vector3 EscapeZonePosition
        {
            get => _escapeZonePosition;
            set => _escapeZonePosition = value;
        }
        
        /// <summary>Whether the escape zone position has been set.</summary>
        public static bool HasEscapeZone => _escapeZonePosition != Vector3.zero;
        
        /// <summary>Direction the robber was moving when last seen.</summary>
        public static Vector3 LastKnownRobberDirection => _lastKnownRobberDirection;
        
        /// <summary>Time when robber was last seen.</summary>
        public static float LastSightingTime => _lastSightingTime;
        
        /// <summary>Time when sight was lost and pursuit began.</summary>
        public static float TimeLostSight => _timeLostSight;
        
        /// <summary>The robber that was last seen.</summary>
        public static GameObject LastSeenRobber => _lastSeenRobber;
        
        /// <summary>Whether there is an active alert (recent sighting).</summary>
        public static bool HasActiveAlert => _hasActiveAlert && (Time.time - _lastSightingTime) < AlertValidDuration;
        
        /// <summary>Whether there is an active coordinated pursuit (recently lost sight).</summary>
        public static bool HasActivePursuit => _hasActivePursuit && (Time.time - _timeLostSight) < PursuitValidDuration;
        
        /// <summary>Time since last sighting.</summary>
        public static float TimeSinceLastSighting => Time.time - _lastSightingTime;
        
        /// <summary>Time since sight was lost.</summary>
        public static float TimeSinceLostSight => Time.time - _timeLostSight;
        
        /// <summary>
        /// Broadcasts a robber sighting to all cops.
        /// Call this when a cop sees a robber.
        /// </summary>
        /// <param name="robberPosition">Current position of the robber.</param>
        /// <param name="robber">The robber GameObject.</param>
        public static void BroadcastRobberSighting(Vector3 robberPosition, GameObject robber)
        {
            // Only log when this is a new alert (not every frame)
            bool isNewAlert = !_hasActiveAlert || (Time.time - _lastSightingTime) > 1f;
            
            _lastKnownRobberPosition = robberPosition;
            _lastSightingTime = Time.time;
            _lastSeenRobber = robber;
            _hasActiveAlert = true;
            // NOTE: Do NOT cancel pursuit here! When one cop has visual, other cops without visual
            // should continue pursuing using the coordinated pursuit system. The cop WITH visual
            // will use the Chase action (which has higher priority due to HasTarget consideration).
            // Pursuit will naturally expire after PursuitValidDuration or when robber is arrested.
            
            if (isNewAlert)
            {
                NPCBrainDebug.Log(NPCBrainDebug.Category.General, 
                    $"[CopAlertSystem] ALERT: Robber spotted at {robberPosition}!", null);
            }
        }
        
        /// <summary>
        /// Updates the robber's movement direction (call while tracking).
        /// </summary>
        /// <param name="direction">Normalized direction the robber is moving.</param>
        public static void UpdateRobberDirection(Vector3 direction)
        {
            if (direction.sqrMagnitude > 0.01f)
            {
                _lastKnownRobberDirection = direction.normalized;
            }
        }
        
        /// <summary>
        /// Broadcasts that a cop has lost sight of the robber.
        /// This starts a coordinated pursuit where all cops pursue in the predicted direction.
        /// </summary>
        /// <param name="lastPosition">Last known position of the robber.</param>
        /// <param name="lastDirection">Direction the robber was moving.</param>
        public static void BroadcastLostSight(Vector3 lastPosition, Vector3 lastDirection)
        {
            // Only start pursuit if we don't already have an active one
            // or if this provides newer information
            if (!_hasActivePursuit || (Time.time - _timeLostSight) > 0.5f)
            {
                _lastKnownRobberPosition = lastPosition;
                _timeLostSight = Time.time;
                _hasActivePursuit = true;
                
                // Only update direction if the new one is valid
                // This preserves any previously tracked direction if the new one is zero
                bool hasValidDirection = lastDirection.sqrMagnitude > 0.01f;
                if (hasValidDirection)
                {
                    _lastKnownRobberDirection = lastDirection;
                }
                else if (_lastKnownRobberDirection.sqrMagnitude < 0.01f)
                {
                    // No previous direction either - this is expected when pursuit starts from alarm
                    // (alarm provides position but not direction). Cops will converge on position.
                    // This is normal for alarm-triggered pursuits, so just log info (not warning).
                    Debug.Log("<color=cyan>[CopAlertSystem]</color> No direction for pursuit - converging on position only");
                }
                
                string directionStatus = _lastKnownRobberDirection.sqrMagnitude > 0.01f 
                    ? $"direction {_lastKnownRobberDirection}" 
                    : "<color=red>NO DIRECTION!</color>";
                
                Debug.Log($"<color=cyan>[CopAlertSystem]</color> <color=magenta>COORDINATED PURSUIT STARTED!</color> Position: {lastPosition} | {directionStatus} | Duration: {PursuitValidDuration}s | HasActivePursuit: {_hasActivePursuit}");
            }
        }
        
        /// <summary>
        /// Gets the predicted position of the robber based on shared intel.
        /// </summary>
        /// <param name="predictionMultiplier">How aggressively to predict ahead (1.0 = normal, 1.5 = aggressive).</param>
        /// <returns>Predicted position of the robber.</returns>
        public static Vector3 GetPredictedRobberPosition(float predictionMultiplier = 1.5f)
        {
            // Always return something useful, even if pursuit has expired
            if (_lastKnownRobberPosition == Vector3.zero)
            {
                return Vector3.zero;  // No data at all
            }
            
            if (!HasActivePursuit)
            {
                return _lastKnownRobberPosition;
            }
            
            float timeSinceLost = Time.time - _timeLostSight;
            
            // If no direction, just return last known position (cops will converge there)
            if (_lastKnownRobberDirection.sqrMagnitude < 0.01f)
            {
                // Throttle this log to avoid spam (every 2 seconds max)
                if (Time.time - _lastNoDirectionLogTime > 2f)
                {
                    _lastNoDirectionLogTime = Time.time;
                    if (NPCBrainDebug.IsEnabled(NPCBrainDebug.Category.General))
                    {
                        Debug.Log($"<color=cyan>[CopAlertSystem]</color> <color=yellow>No direction for prediction - returning last known position</color>");
                    }
                }
                return _lastKnownRobberPosition;
            }
            
            // Estimate robber speed (flee speed is 7)
            float estimatedRobberSpeed = 7f;
            Vector3 predictedOffset = _lastKnownRobberDirection * estimatedRobberSpeed * timeSinceLost * predictionMultiplier;
            
            return _lastKnownRobberPosition + predictedOffset;
        }
        
        /// <summary>
        /// Clears the current alert (e.g., when robber is arrested).
        /// </summary>
        public static void ClearAlert()
        {
            _hasActiveAlert = false;
            _hasActivePursuit = false;
            _lastSeenRobber = null;
            
            NPCBrainDebug.Log(NPCBrainDebug.Category.General, 
                "[CopAlertSystem] Alert cleared.", null);
        }
        
        /// <summary>
        /// Clears the alert only if it matches the specified robber.
        /// Use this when arresting to avoid clearing alerts for other robbers.
        /// </summary>
        /// <param name="robber">The robber to clear the alert for.</param>
        public static void ClearAlertForRobber(GameObject robber)
        {
            if (_lastSeenRobber == robber)
            {
                ClearAlert();
            }
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
            _lastKnownRobberDirection = Vector3.zero;
            _lastSightingTime = -100f;
            _timeLostSight = -100f;
            _lastSeenRobber = null;
            _hasActiveAlert = false;
            _hasActivePursuit = false;
            _lastNoDirectionLogTime = -100f;
            _escapeZonePosition = Vector3.zero;
        }
    }
}
