using System;
using System.Collections.Generic;
using UnityEngine;

namespace NPCBrain.Perception
{
    /// <summary>
    /// Stores memory of seen targets with decay over time.
    /// Tracks last known position and time since last seen.
    /// </summary>
    public class Memory
    {
        /// <summary>Default memory duration in seconds.</summary>
        public const float DefaultMemoryDuration = 10f;
        
        /// <summary>Default confidence decay rate per second.</summary>
        public const float DefaultDecayRate = 0.1f;
        
        /// <summary>Minimum confidence boost when hearing a target.</summary>
        public const float HearingConfidenceBoost = 0.5f;
        
        /// <summary>Minimum time delta for velocity calculation.</summary>
        public const float MinVelocityTimeDelta = 0.01f;

        /// <summary>
        /// When true, logs warnings when operations fail (e.g., target not in memory).
        /// Also enabled when NPCBrainDebug.LogMemory is true.
        /// </summary>
        public bool LogWarnings { get; set; } = false;
        
        /// <summary>
        /// Information about a remembered target.
        /// </summary>
        public class TargetMemory
        {
            /// <summary>The remembered target GameObject.</summary>
            public GameObject Target { get; set; }
            
            /// <summary>Last known world position of the target.</summary>
            public Vector3 LastKnownPosition { get; set; }
            
            /// <summary>Time when the target was last seen.</summary>
            public float LastSeenTime { get; set; }
            
            /// <summary>Time in seconds since the target was last seen.</summary>
            public float TimeSinceLastSeen => Time.time - LastSeenTime;
            
            /// <summary>True if the target is currently visible.</summary>
            public bool IsCurrentlyVisible { get; set; }
            
            /// <summary>Confidence level (1.0 = just seen, decays to 0).</summary>
            public float Confidence { get; set; }
            
            /// <summary>Direction the target was last moving.</summary>
            public Vector3 LastKnownVelocity { get; set; }
            
            /// <summary>True if the target was detected via hearing.</summary>
            public bool WasHeard { get; set; }
            
            /// <summary>Position where the target was last heard.</summary>
            public Vector3 LastHeardPosition { get; set; }
            
            /// <summary>Time when the target was last heard.</summary>
            public float LastHeardTime { get; set; }
            
            /// <summary>Time in seconds since the target was last heard.</summary>
            public float TimeSinceLastHeard => Time.time - LastHeardTime;
            
            /// <summary>Type of sound that was last heard from this target.</summary>
            public SoundType LastHeardSoundType { get; set; }
        }
        
        private readonly Dictionary<GameObject, TargetMemory> _memories = new Dictionary<GameObject, TargetMemory>();
        private readonly List<GameObject> _toRemove = new List<GameObject>();
        
        /// <summary>How long memories persist after losing sight (seconds).</summary>
        public float MemoryDuration { get; set; } = DefaultMemoryDuration;
        
        /// <summary>Rate at which confidence decays per second.</summary>
        public float DecayRate { get; set; } = DefaultDecayRate;
        
        /// <summary>All current memories.</summary>
        public IReadOnlyDictionary<GameObject, TargetMemory> Memories => _memories;
        
        /// <summary>Number of remembered targets.</summary>
        public int Count => _memories.Count;
        
        /// <summary>
        /// Updates memory for a currently visible target.
        /// </summary>
        /// <param name="target">The visible target.</param>
        /// <param name="position">Current position of the target.</param>
        public void UpdateVisible(GameObject target, Vector3 position)
        {
            if (target == null) return;
            
            if (!_memories.TryGetValue(target, out var memory))
            {
                memory = new TargetMemory { Target = target };
                _memories[target] = memory;
            }
            
            // Calculate velocity from position change
            if (memory.LastSeenTime > 0)
            {
                float dt = Time.time - memory.LastSeenTime;
                if (dt > MinVelocityTimeDelta)
                {
                    memory.LastKnownVelocity = (position - memory.LastKnownPosition) / dt;
                }
            }
            
            memory.LastKnownPosition = position;
            memory.LastSeenTime = Time.time;
            memory.IsCurrentlyVisible = true;
            memory.Confidence = 1f;
        }
        
        /// <summary>
        /// Marks a target as no longer visible (starts decay).
        /// </summary>
        /// <param name="target">The target that was lost.</param>
        public void MarkLost(GameObject target)
        {
            if (target == null) return;
            
            if (_memories.TryGetValue(target, out var memory))
            {
                memory.IsCurrentlyVisible = false;
            }
        }
        
        /// <summary>
        /// Updates all memories, applying decay and removing expired ones.
        /// Call this each tick.
        /// </summary>
        public void Tick()
        {
            _toRemove.Clear();
            
            foreach (var kvp in _memories)
            {
                var memory = kvp.Value;
                
                // Skip if target was destroyed
                if (memory.Target == null)
                {
                    _toRemove.Add(kvp.Key);
                    continue;
                }
                
                // Apply decay to non-visible targets
                if (!memory.IsCurrentlyVisible)
                {
                    memory.Confidence -= DecayRate * Time.deltaTime;
                    
                    // Remove if expired
                    if (memory.TimeSinceLastSeen > MemoryDuration || memory.Confidence <= 0)
                    {
                        _toRemove.Add(kvp.Key);
                    }
                }
            }
            
            foreach (var key in _toRemove)
            {
                _memories.Remove(key);
            }
        }
        
        /// <summary>
        /// Gets memory for a specific target.
        /// </summary>
        /// <param name="target">The target to look up.</param>
        /// <returns>The memory, or null if not remembered.</returns>
        public TargetMemory GetMemory(GameObject target)
        {
            if (target == null) return null;
            _memories.TryGetValue(target, out var memory);
            return memory;
        }
        
        /// <summary>
        /// Checks if a target is remembered.
        /// </summary>
        /// <param name="target">The target to check.</param>
        /// <returns>True if the target is in memory.</returns>
        public bool Remembers(GameObject target)
        {
            return target != null && _memories.ContainsKey(target);
        }
        
        /// <summary>
        /// Gets the predicted current position based on last known position and velocity.
        /// </summary>
        /// <param name="target">The target to predict.</param>
        /// <returns>Predicted position, or Vector3.zero if target not in memory.</returns>
        /// <remarks>Consider using TryGetPredictedPosition for safer access.</remarks>
        public Vector3 GetPredictedPosition(GameObject target)
        {
            if (target == null)
            {
                if (ShouldLogWarning())
                {
                    NPCBrainDebug.LogWarning(NPCBrainDebug.Category.Memory, 
                        "GetPredictedPosition called with null target. Returning Vector3.zero.");
                }
                return Vector3.zero;
            }
            
            if (_memories.TryGetValue(target, out var memory))
            {
                float timeSinceSeen = memory.TimeSinceLastSeen;
                return memory.LastKnownPosition + memory.LastKnownVelocity * timeSinceSeen;
            }
            
            if (ShouldLogWarning())
            {
                NPCBrainDebug.LogWarning(NPCBrainDebug.Category.Memory, 
                    $"GetPredictedPosition called for target '{target.name}' which is not in memory. " +
                    $"Returning Vector3.zero. Use TryGetPredictedPosition or Remembers() to check first.");
            }
            return Vector3.zero;
        }
        
        /// <summary>
        /// Attempts to get the predicted current position based on last known position and velocity.
        /// </summary>
        /// <param name="target">The target to predict.</param>
        /// <param name="position">The predicted position if successful.</param>
        /// <returns>True if the target is in memory and position was calculated.</returns>
        public bool TryGetPredictedPosition(GameObject target, out Vector3 position)
        {
            position = Vector3.zero;
            
            if (target == null)
            {
                return false;
            }
            
            if (_memories.TryGetValue(target, out var memory))
            {
                float timeSinceSeen = memory.TimeSinceLastSeen;
                position = memory.LastKnownPosition + memory.LastKnownVelocity * timeSinceSeen;
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Gets the most recently seen target.
        /// </summary>
        /// <returns>The most recent target, or null if memory is empty.</returns>
        public GameObject GetMostRecentTarget()
        {
            GameObject mostRecent = null;
            float mostRecentTime = float.MinValue;
            
            foreach (var kvp in _memories)
            {
                if (kvp.Value.LastSeenTime > mostRecentTime)
                {
                    mostRecentTime = kvp.Value.LastSeenTime;
                    mostRecent = kvp.Key;
                }
            }
            
            return mostRecent;
        }
        
        /// <summary>
        /// Clears all memories.
        /// </summary>
        public void Clear()
        {
            _memories.Clear();
        }
        
        /// <summary>
        /// Removes a specific target from memory.
        /// </summary>
        /// <param name="target">The target to forget.</param>
        public void Forget(GameObject target)
        {
            if (target != null)
            {
                _memories.Remove(target);
            }
        }
        
        /// <summary>
        /// Updates memory for a target that was heard.
        /// </summary>
        /// <param name="target">The target that made a sound.</param>
        /// <param name="position">Position where the sound originated.</param>
        /// <param name="soundType">Type of sound that was heard.</param>
        public void UpdateHeard(GameObject target, Vector3 position, SoundType soundType)
        {
            if (target == null) return;
            
            if (!_memories.TryGetValue(target, out var memory))
            {
                memory = new TargetMemory { Target = target };
                _memories[target] = memory;
            }
            
            memory.WasHeard = true;
            memory.LastHeardPosition = position;
            memory.LastHeardTime = Time.time;
            memory.LastHeardSoundType = soundType;
            
            // Boost confidence if not currently visible
            if (!memory.IsCurrentlyVisible)
            {
                memory.Confidence = Mathf.Max(memory.Confidence, HearingConfidenceBoost);
                // Use heard position as fallback for last known position
                if (memory.LastSeenTime < memory.LastHeardTime)
                {
                    memory.LastKnownPosition = position;
                }
            }
        }
        
        /// <summary>
        /// Gets the most recently heard target.
        /// </summary>
        /// <returns>The most recently heard target, or null if none.</returns>
        public GameObject GetMostRecentlyHeardTarget()
        {
            GameObject mostRecent = null;
            float mostRecentTime = float.MinValue;
            
            foreach (var kvp in _memories)
            {
                if (kvp.Value.WasHeard && kvp.Value.LastHeardTime > mostRecentTime)
                {
                    mostRecentTime = kvp.Value.LastHeardTime;
                    mostRecent = kvp.Key;
                }
            }
            
            return mostRecent;
        }
        
        private bool ShouldLogWarning()
        {
            return LogWarnings || NPCBrainDebug.IsEnabled(NPCBrainDebug.Category.Memory);
        }
    }
}
