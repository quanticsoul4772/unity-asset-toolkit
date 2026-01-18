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
        }
        
        private readonly Dictionary<GameObject, TargetMemory> _memories = new Dictionary<GameObject, TargetMemory>();
        private readonly List<GameObject> _toRemove = new List<GameObject>();
        
        /// <summary>How long memories persist after losing sight (seconds).</summary>
        public float MemoryDuration { get; set; } = 10f;
        
        /// <summary>Rate at which confidence decays per second.</summary>
        public float DecayRate { get; set; } = 0.1f;
        
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
                if (dt > 0.01f)
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
        /// <returns>Predicted position, or last known position if no velocity data.</returns>
        public Vector3 GetPredictedPosition(GameObject target)
        {
            if (target == null) return Vector3.zero;
            
            if (_memories.TryGetValue(target, out var memory))
            {
                float timeSinceSeen = memory.TimeSinceLastSeen;
                return memory.LastKnownPosition + memory.LastKnownVelocity * timeSinceSeen;
            }
            
            return Vector3.zero;
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
    }
}
