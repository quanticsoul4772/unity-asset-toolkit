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
        public const string ClosestCopPosition = "closestCopPosition";
        
        // Timestamp keys for TimeConsideration
        public const string LastPatrolTime = "lastPatrolTime";
        public const string LastRestTime = "lastRestTime";
        public const string LastWanderTime = "lastWanderTime";
        public const string LastChaseTime = "lastChaseTime";
        public const string LastInvestigateTime = "lastInvestigateTime";
        public const string LastReturnTime = "lastReturnTime";
        public const string LastSeekTime = "lastSeekTime";
        public const string LastArrestTime = "lastArrestTime";
        public const string LastStealTime = "lastStealTime";
        public const string LastFleeTime = "lastFleeTime";
        public const string LastHideTime = "lastHideTime";
        public const string LastSneakTime = "lastSneakTime";
        public const string LastScoutTime = "lastScoutTime";
        
        // Sound keys
        public const string LastSoundType = "lastSoundType";
        public const string LastFootstepPosition = "lastFootstepPosition";
        public const string LastFootstepTime = "lastFootstepTime";
        
        // Pursuit persistence keys (for continuing chase after losing sight)
        public const string LastKnownRobberPosition = "lastKnownRobberPosition";
        public const string LastKnownRobberDirection = "lastKnownRobberDirection";
        public const string TimeLostSight = "timeLostSight";
        
        // CopNPC keys
        public const string CanArrest = "canArrest";
        public const string TargetDistance = "targetDistance";
        
        // Shared alert keys (used by CopAlertSystem)
        public const string RespondingToAlert = "respondingToAlert";
        public const string AlertPosition = "alertPosition";
        public const string CrimeInProgress = "crimeInProgress";
        public const string AlarmLocation = "alarmLocation";
        
        // RobberNPC keys
        public const string CanSeeCop = "canSeeCop";
        public const string FearLevel = "fearLevel";
        public const string HasLoot = "hasLoot";
        public const string LootValue = "lootValue";
        public const string ClosestCopDistance = "closestCopDistance";
    }
}
