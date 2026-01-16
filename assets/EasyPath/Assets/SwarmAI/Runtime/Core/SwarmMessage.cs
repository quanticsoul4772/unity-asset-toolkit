using UnityEngine;

namespace SwarmAI
{
    /// <summary>
    /// Types of messages that can be sent between agents.
    /// </summary>
    public enum SwarmMessageType
    {
        /// <summary>Generic message.</summary>
        Generic,
        /// <summary>Command to move to a position.</summary>
        MoveTo,
        /// <summary>Command to seek a target.</summary>
        Seek,
        /// <summary>Command to flee from a threat.</summary>
        Flee,
        /// <summary>Command to stop current action.</summary>
        Stop,
        /// <summary>Command to follow a leader.</summary>
        Follow,
        /// <summary>Alert about a threat.</summary>
        ThreatDetected,
        /// <summary>Alert about a resource.</summary>
        ResourceFound,
        /// <summary>Request for help.</summary>
        RequestHelp,
        /// <summary>Custom message type.</summary>
        Custom
    }
    
    /// <summary>
    /// Message for communication between swarm agents.
    /// </summary>
    public class SwarmMessage
    {
        /// <summary>
        /// Type of this message.
        /// </summary>
        public SwarmMessageType Type { get; private set; }
        
        /// <summary>
        /// ID of the sender agent. -1 if from SwarmManager.
        /// </summary>
        public int SenderId { get; private set; }
        
        /// <summary>
        /// ID of the target agent. -1 for broadcast.
        /// </summary>
        public int TargetId { get; private set; }
        
        /// <summary>
        /// Position data (for MoveTo, Seek, Flee, etc.).
        /// </summary>
        public Vector3 Position { get; private set; }
        
        /// <summary>
        /// Optional object reference.
        /// </summary>
        public Object Data { get; private set; }
        
        /// <summary>
        /// Optional float value.
        /// </summary>
        public float Value { get; private set; }
        
        /// <summary>
        /// Optional custom tag for filtering.
        /// </summary>
        public string Tag { get; private set; }
        
        /// <summary>
        /// Time when this message was created.
        /// </summary>
        public float Timestamp { get; private set; }
        
        /// <summary>
        /// Create a new swarm message.
        /// </summary>
        public SwarmMessage(SwarmMessageType type, int senderId = -1, int targetId = -1)
        {
            Type = type;
            SenderId = senderId;
            TargetId = targetId;
            Position = Vector3.zero;
            Data = null;
            Value = 0f;
            Tag = null;
            Timestamp = Time.time;
        }
        
        /// <summary>
        /// Set the position data for this message.
        /// </summary>
        public SwarmMessage WithPosition(Vector3 position)
        {
            Position = position;
            return this;
        }
        
        /// <summary>
        /// Set the data object for this message.
        /// </summary>
        public SwarmMessage WithData(Object data)
        {
            Data = data;
            return this;
        }
        
        /// <summary>
        /// Set the float value for this message.
        /// </summary>
        public SwarmMessage WithValue(float value)
        {
            Value = value;
            return this;
        }
        
        /// <summary>
        /// Set the tag for this message.
        /// </summary>
        public SwarmMessage WithTag(string tag)
        {
            Tag = tag;
            return this;
        }
        
        /// <summary>
        /// Check if this message is a broadcast (no specific target).
        /// </summary>
        public bool IsBroadcast => TargetId < 0;
        
        /// <summary>
        /// Create a copy of this message with optional sender/target override.
        /// </summary>
        /// <param name="newSenderId">New sender ID, or null to keep original.</param>
        /// <param name="newTargetId">New target ID, or null to keep original.</param>
        public SwarmMessage Clone(int? newSenderId = null, int? newTargetId = null)
        {
            return new SwarmMessage(Type, 
                newSenderId ?? SenderId, 
                newTargetId ?? TargetId)
                .WithPosition(Position)
                .WithData(Data)
                .WithValue(Value)
                .WithTag(Tag);
        }
        
        /// <summary>
        /// Create a MoveTo message.
        /// </summary>
        public static SwarmMessage MoveTo(Vector3 position, int senderId = -1, int targetId = -1)
        {
            return new SwarmMessage(SwarmMessageType.MoveTo, senderId, targetId)
                .WithPosition(position);
        }
        
        /// <summary>
        /// Create a Seek message.
        /// </summary>
        public static SwarmMessage Seek(Vector3 position, int senderId = -1, int targetId = -1)
        {
            return new SwarmMessage(SwarmMessageType.Seek, senderId, targetId)
                .WithPosition(position);
        }
        
        /// <summary>
        /// Create a Flee message.
        /// </summary>
        public static SwarmMessage Flee(Vector3 threatPosition, int senderId = -1, int targetId = -1)
        {
            return new SwarmMessage(SwarmMessageType.Flee, senderId, targetId)
                .WithPosition(threatPosition);
        }
        
        /// <summary>
        /// Create a Stop message.
        /// </summary>
        public static SwarmMessage Stop(int senderId = -1, int targetId = -1)
        {
            return new SwarmMessage(SwarmMessageType.Stop, senderId, targetId);
        }
        
        /// <summary>
        /// Create a ThreatDetected message.
        /// </summary>
        public static SwarmMessage ThreatDetected(Vector3 threatPosition, float threatLevel, int senderId = -1)
        {
            return new SwarmMessage(SwarmMessageType.ThreatDetected, senderId, -1)
                .WithPosition(threatPosition)
                .WithValue(threatLevel);
        }
    }
}
