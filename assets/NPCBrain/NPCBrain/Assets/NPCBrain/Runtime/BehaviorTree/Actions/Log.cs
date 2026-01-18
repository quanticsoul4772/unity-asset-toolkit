using System;
using UnityEngine;

namespace NPCBrain.BehaviorTree.Actions
{
    /// <summary>
    /// Action that logs a debug message and immediately returns Success.
    /// Useful for debugging behavior tree execution flow.
    /// </summary>
    /// <example>
    /// <code>
    /// var tree = new Sequence(
    ///     new Log("Starting patrol"),
    ///     new MoveTo(() => GetNextWaypoint()),
    ///     new Log(brain => $"{brain.name} reached waypoint")
    /// );
    /// </code>
    /// </example>
    public class Log : BTNode
    {
        /// <summary>
        /// Log level for the message.
        /// </summary>
        public enum LogLevel
        {
            Info,
            Warning,
            Error
        }
        
        private readonly Func<NPCBrainController, string> _getMessage;
        private readonly LogLevel _level;
        
        /// <summary>
        /// Creates a Log action with a static message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="level">Log level (default: Info).</param>
        public Log(string message, LogLevel level = LogLevel.Info)
        {
            _getMessage = _ => message;
            _level = level;
            Name = $"Log({TruncateMessage(message)})";
        }
        
        /// <summary>
        /// Creates a Log action with a dynamic message.
        /// </summary>
        /// <param name="getMessage">Function to generate the message.</param>
        /// <param name="level">Log level (default: Info).</param>
        public Log(Func<NPCBrainController, string> getMessage, LogLevel level = LogLevel.Info)
        {
            _getMessage = getMessage ?? throw new ArgumentNullException(nameof(getMessage));
            _level = level;
            Name = "Log(Dynamic)";
        }
        
        protected override NodeStatus Tick(NPCBrainController brain)
        {
            string message = _getMessage(brain);
            string formattedMessage = brain != null 
                ? $"[{brain.name}] {message}" 
                : message;
            
            switch (_level)
            {
                case LogLevel.Warning:
                    Debug.LogWarning(formattedMessage);
                    break;
                case LogLevel.Error:
                    Debug.LogError(formattedMessage);
                    break;
                default:
                    Debug.Log(formattedMessage);
                    break;
            }
            
            return NodeStatus.Success;
        }
        
        private static string TruncateMessage(string message)
        {
            const int maxLength = 20;
            if (string.IsNullOrEmpty(message)) return "";
            if (message.Length <= maxLength) return message;
            return message.Substring(0, maxLength - 3) + "...";
        }
    }
}
