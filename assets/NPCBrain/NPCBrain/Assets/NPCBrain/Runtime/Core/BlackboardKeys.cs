namespace NPCBrain
{
    /// <summary>
    /// Static constants for common Blackboard keys.
    /// Using constants avoids repeated string allocations and hash calculations.
    /// </summary>
    public static class BBKeys
    {
        // Common state keys
        public const string CurrentState = "currentState";
        public const string Energy = "energy";
        public const string AlertLevel = "alertLevel";
        public const string Target = "target";
        public const string HomePosition = "homePosition";
        
        // Position keys
        public const string LastKnownPosition = "lastKnownPosition";
        public const string InvestigatePosition = "investigatePosition";
        public const string InterestPoint = "interestPoint";
        
        // Timestamp keys for TimeConsideration
        public const string LastPatrolTime = "lastPatrolTime";
        public const string LastRestTime = "lastRestTime";
        public const string LastWanderTime = "lastWanderTime";
        public const string LastChaseTime = "lastChaseTime";
        public const string LastInvestigateTime = "lastInvestigateTime";
        public const string LastReturnTime = "lastReturnTime";
        public const string LastSeekTime = "lastSeekTime";
        
        // Sound keys
        public const string LastSoundType = "lastSoundType";
    }
}
