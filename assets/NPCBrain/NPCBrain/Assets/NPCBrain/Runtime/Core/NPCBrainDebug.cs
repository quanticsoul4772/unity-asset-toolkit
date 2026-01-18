using UnityEngine;

namespace NPCBrain
{
    /// <summary>
    /// Centralized debug logging system for NPCBrain components.
    /// Enable/disable logging globally or per-category.
    /// </summary>
    /// <remarks>
    /// <para>Use this to control debug output across all NPCBrain components.</para>
    /// <example>
    /// <code>
    /// // Enable all debug logging
    /// NPCBrainDebug.Enabled = true;
    /// 
    /// // Enable only perception logging
    /// NPCBrainDebug.Enabled = true;
    /// NPCBrainDebug.LogPerception = true;
    /// NPCBrainDebug.LogBehaviorTree = false;
    /// 
    /// // Log from your own code
    /// NPCBrainDebug.Log(NPCBrainDebug.Category.Perception, "Custom message");
    /// </code>
    /// </example>
    /// </remarks>
    public static class NPCBrainDebug
    {
        /// <summary>
        /// Debug log categories for filtering output.
        /// </summary>
        public enum Category
        {
            /// <summary>General NPCBrain messages</summary>
            General,
            /// <summary>Behavior tree execution</summary>
            BehaviorTree,
            /// <summary>Utility AI and action selection</summary>
            Utility,
            /// <summary>Sight sensor and vision detection</summary>
            Perception,
            /// <summary>Hearing sensor and sound detection</summary>
            Hearing,
            /// <summary>Target memory and tracking</summary>
            Memory,
            /// <summary>Blackboard key-value store</summary>
            Blackboard,
            /// <summary>Waypoint paths and navigation</summary>
            Waypoints,
            /// <summary>Criticality system</summary>
            Criticality
        }
        
        /// <summary>
        /// Master switch to enable/disable all NPCBrain debug logging.
        /// Must be true for any logging to occur.
        /// </summary>
        public static bool Enabled { get; set; } = false;
        
        /// <summary>Enable general NPCBrain messages.</summary>
        public static bool LogGeneral { get; set; } = true;
        
        /// <summary>Enable behavior tree execution logging.</summary>
        public static bool LogBehaviorTree { get; set; } = true;
        
        /// <summary>Enable utility AI logging.</summary>
        public static bool LogUtility { get; set; } = true;
        
        /// <summary>Enable sight perception logging.</summary>
        public static bool LogPerception { get; set; } = true;
        
        /// <summary>Enable hearing sensor logging.</summary>
        public static bool LogHearing { get; set; } = true;
        
        /// <summary>Enable memory system logging.</summary>
        public static bool LogMemory { get; set; } = true;
        
        /// <summary>Enable blackboard logging.</summary>
        public static bool LogBlackboard { get; set; } = true;
        
        /// <summary>Enable waypoint system logging.</summary>
        public static bool LogWaypoints { get; set; } = true;
        
        /// <summary>Enable criticality system logging.</summary>
        public static bool LogCriticality { get; set; } = true;
        
        /// <summary>
        /// Always log warnings regardless of Enabled flag.
        /// Useful for catching configuration issues.
        /// </summary>
        public static bool AlwaysLogWarnings { get; set; } = true;
        
        /// <summary>
        /// Always log errors regardless of Enabled flag.
        /// </summary>
        public static bool AlwaysLogErrors { get; set; } = true;
        
        /// <summary>
        /// Include timestamps in log messages.
        /// </summary>
        public static bool IncludeTimestamp { get; set; } = false;
        
        /// <summary>
        /// Use colored output for different log levels.
        /// </summary>
        public static bool UseColors { get; set; } = true;
        
        /// <summary>
        /// Checks if logging is enabled for a specific category.
        /// </summary>
        /// <param name="category">The category to check.</param>
        /// <returns>True if logging is enabled for this category.</returns>
        public static bool IsEnabled(Category category)
        {
            if (!Enabled) return false;
            
            return category switch
            {
                Category.General => LogGeneral,
                Category.BehaviorTree => LogBehaviorTree,
                Category.Utility => LogUtility,
                Category.Perception => LogPerception,
                Category.Hearing => LogHearing,
                Category.Memory => LogMemory,
                Category.Blackboard => LogBlackboard,
                Category.Waypoints => LogWaypoints,
                Category.Criticality => LogCriticality,
                _ => true
            };
        }
        
        /// <summary>
        /// Logs an info message if the category is enabled.
        /// </summary>
        /// <param name="category">The log category.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="context">Optional Unity Object for context.</param>
        public static void Log(Category category, string message, Object context = null)
        {
            if (!IsEnabled(category)) return;
            
            string formatted = FormatMessage(category, message);
            if (UseColors)
            {
                formatted = $"<color=#88CC88>{formatted}</color>";
            }
            
            if (context != null)
                Debug.Log(formatted, context);
            else
                Debug.Log(formatted);
        }
        
        /// <summary>
        /// Logs a warning message. Respects AlwaysLogWarnings setting.
        /// </summary>
        /// <param name="category">The log category.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="context">Optional Unity Object for context.</param>
        public static void LogWarning(Category category, string message, Object context = null)
        {
            if (!AlwaysLogWarnings && !IsEnabled(category)) return;
            
            string formatted = FormatMessage(category, message);
            if (UseColors)
            {
                formatted = $"<color=#CCCC44>{formatted}</color>";
            }
            
            if (context != null)
                Debug.LogWarning(formatted, context);
            else
                Debug.LogWarning(formatted);
        }
        
        /// <summary>
        /// Logs an error message. Respects AlwaysLogErrors setting.
        /// </summary>
        /// <param name="category">The log category.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="context">Optional Unity Object for context.</param>
        public static void LogError(Category category, string message, Object context = null)
        {
            if (!AlwaysLogErrors && !IsEnabled(category)) return;
            
            string formatted = FormatMessage(category, message);
            if (UseColors)
            {
                formatted = $"<color=#CC4444>{formatted}</color>";
            }
            
            if (context != null)
                Debug.LogError(formatted, context);
            else
                Debug.LogError(formatted);
        }
        
        /// <summary>
        /// Enables all logging categories.
        /// </summary>
        public static void EnableAll()
        {
            Enabled = true;
            LogGeneral = true;
            LogBehaviorTree = true;
            LogUtility = true;
            LogPerception = true;
            LogHearing = true;
            LogMemory = true;
            LogBlackboard = true;
            LogWaypoints = true;
            LogCriticality = true;
        }
        
        /// <summary>
        /// Disables all logging (sets Enabled to false).
        /// </summary>
        public static void DisableAll()
        {
            Enabled = false;
        }
        
        /// <summary>
        /// Enables only specific categories, disabling all others.
        /// </summary>
        /// <param name="categories">Categories to enable.</param>
        public static void EnableOnly(params Category[] categories)
        {
            Enabled = true;
            LogGeneral = false;
            LogBehaviorTree = false;
            LogUtility = false;
            LogPerception = false;
            LogHearing = false;
            LogMemory = false;
            LogBlackboard = false;
            LogWaypoints = false;
            LogCriticality = false;
            
            foreach (var cat in categories)
            {
                SetCategoryEnabled(cat, true);
            }
        }
        
        /// <summary>
        /// Sets whether a specific category is enabled.
        /// </summary>
        /// <param name="category">The category to configure.</param>
        /// <param name="enabled">Whether to enable the category.</param>
        public static void SetCategoryEnabled(Category category, bool enabled)
        {
            switch (category)
            {
                case Category.General: LogGeneral = enabled; break;
                case Category.BehaviorTree: LogBehaviorTree = enabled; break;
                case Category.Utility: LogUtility = enabled; break;
                case Category.Perception: LogPerception = enabled; break;
                case Category.Hearing: LogHearing = enabled; break;
                case Category.Memory: LogMemory = enabled; break;
                case Category.Blackboard: LogBlackboard = enabled; break;
                case Category.Waypoints: LogWaypoints = enabled; break;
                case Category.Criticality: LogCriticality = enabled; break;
            }
        }
        
        private static string FormatMessage(Category category, string message)
        {
            string prefix = $"[NPCBrain.{category}]";
            
            if (IncludeTimestamp)
            {
                return $"{prefix} [{Time.time:F2}] {message}";
            }
            
            return $"{prefix} {message}";
        }
    }
}
