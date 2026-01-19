using UnityEngine;

namespace NPCBrain
{
    /// <summary>
    /// Static timer for heist scenarios. Tracks time remaining and provides urgency calculations.
    /// Set by demo/game setup scripts, accessed by NPCs for time-aware behavior.
    /// </summary>
    public static class HeistTimer
    {
        private static float _heistTimeLimit;
        private static float _heistStartTime;
        private static bool _timeLimitEnabled;
        private static bool _heistActive;
        
        /// <summary>Total time allowed for the heist in seconds.</summary>
        public static float TimeLimit => _heistTimeLimit;
        
        /// <summary>Time remaining in the heist in seconds.</summary>
        public static float TimeRemaining => _timeLimitEnabled && _heistActive 
            ? Mathf.Max(0f, _heistTimeLimit - (Time.time - _heistStartTime)) 
            : float.MaxValue;
        
        /// <summary>Time remaining normalized (1.0 = full time, 0.0 = no time left).</summary>
        public static float TimeRemainingNormalized => _timeLimitEnabled && _heistActive && _heistTimeLimit > 0f
            ? Mathf.Clamp01(TimeRemaining / _heistTimeLimit)
            : 1f;
        
        /// <summary>Whether the time limit is enabled.</summary>
        public static bool IsTimeLimitEnabled => _timeLimitEnabled;
        
        /// <summary>Whether the heist is currently active.</summary>
        public static bool IsHeistActive => _heistActive;
        
        /// <summary>Whether time has expired.</summary>
        public static bool HasTimeExpired => _timeLimitEnabled && _heistActive && TimeRemaining <= 0f;
        
        /// <summary>
        /// Starts the heist timer with the specified time limit.
        /// </summary>
        /// <param name="timeLimit">Time limit in seconds.</param>
        /// <param name="enableTimeLimit">Whether to enforce the time limit.</param>
        public static void StartHeist(float timeLimit, bool enableTimeLimit = true)
        {
            _heistTimeLimit = timeLimit;
            _heistStartTime = Time.time;
            _timeLimitEnabled = enableTimeLimit;
            _heistActive = true;
        }
        
        /// <summary>
        /// Ends the heist (called when game ends).
        /// </summary>
        public static void EndHeist()
        {
            _heistActive = false;
        }
        
        /// <summary>
        /// Resets the timer state (called on domain reload).
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _heistTimeLimit = 120f;
            _heistStartTime = 0f;
            _timeLimitEnabled = false;
            _heistActive = false;
        }
    }
}
