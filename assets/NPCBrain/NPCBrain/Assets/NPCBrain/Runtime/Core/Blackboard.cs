using System;
using System.Collections.Generic;

namespace NPCBrain
{
    /// <summary>
    /// A key-value data store for sharing information between behavior tree nodes.
    /// Supports typed values, TTL expiration, and change notifications.
    /// </summary>
    /// <remarks>
    /// <para>The blackboard is the primary way for nodes to communicate:</para>
    /// <list type="bullet">
    ///   <item><description>Perception sensors write detected targets</description></item>
    ///   <item><description>Condition nodes read values to make decisions</description></item>
    ///   <item><description>Action nodes read/write state as needed</description></item>
    /// </list>
    /// <example>
    /// <code>
    /// // Set a value
    /// brain.Blackboard.Set("health", 100);
    /// 
    /// // Set a value that expires after 5 seconds
    /// brain.Blackboard.SetWithTTL("lastKnownPosition", targetPos, 5f);
    /// 
    /// // Get a value with default fallback
    /// int health = brain.Blackboard.Get("health", 0);
    /// </code>
    /// </example>
    /// </remarks>
    public class Blackboard
    {
        private struct Entry
        {
            public object Value;
            public float ExpirationTime;
            public bool HasExpiration;
        }
        
        private readonly Dictionary<string, Entry> _data = new Dictionary<string, Entry>();
        private readonly List<string> _keysToRemove = new List<string>();
        
        /// <summary>Raised when any value is set or updated.</summary>
        public event Action<string, object> OnValueChanged;
        
        /// <summary>Raised when a TTL value expires.</summary>
        public event Action<string> OnValueExpired;
        
        /// <summary>
        /// Clears all event subscriptions. Called during cleanup.
        /// </summary>
        public void ClearEvents()
        {
            OnValueChanged = null;
            OnValueExpired = null;
        }
        
        /// <summary>
        /// Sets a value that persists until explicitly removed.
        /// </summary>
        /// <typeparam name="T">The type of value to store.</typeparam>
        /// <param name="key">The key to store the value under.</param>
        /// <param name="value">The value to store.</param>
        public void Set<T>(string key, T value)
        {
            _data[key] = new Entry { Value = value, HasExpiration = false };
            OnValueChanged?.Invoke(key, value);
        }
        
        /// <summary>
        /// Sets a value that automatically expires after the specified time.
        /// </summary>
        /// <typeparam name="T">The type of value to store.</typeparam>
        /// <param name="key">The key to store the value under.</param>
        /// <param name="value">The value to store.</param>
        /// <param name="ttlSeconds">Time-to-live in seconds before the value expires.</param>
        public void SetWithTTL<T>(string key, T value, float ttlSeconds)
        {
            _data[key] = new Entry
            {
                Value = value,
                ExpirationTime = UnityEngine.Time.time + ttlSeconds,
                HasExpiration = true
            };
            OnValueChanged?.Invoke(key, value);
        }
        
        /// <summary>
        /// Gets a value by key, returning a default if not found or wrong type.
        /// </summary>
        /// <typeparam name="T">The expected type of the value.</typeparam>
        /// <param name="key">The key to look up.</param>
        /// <param name="defaultValue">Value to return if key not found or type mismatch.</param>
        /// <returns>The stored value, or defaultValue.</returns>
        public T Get<T>(string key, T defaultValue = default)
        {
            if (TryGet<T>(key, out T value))
            {
                return value;
            }
            return defaultValue;
        }
        
        /// <summary>
        /// Attempts to get a value by key.
        /// </summary>
        /// <typeparam name="T">The expected type of the value.</typeparam>
        /// <param name="key">The key to look up.</param>
        /// <param name="value">The value if found and type matches.</param>
        /// <returns>True if the key exists, hasn't expired, and type matches.</returns>
        public bool TryGet<T>(string key, out T value)
        {
            value = default;
            
            if (!_data.TryGetValue(key, out Entry entry))
            {
                return false;
            }
            
            if (entry.HasExpiration && UnityEngine.Time.time >= entry.ExpirationTime)
            {
                _data.Remove(key);
                OnValueExpired?.Invoke(key);
                return false;
            }
            
            if (entry.Value is T typedValue)
            {
                value = typedValue;
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Checks if a key exists and hasn't expired.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if the key exists and hasn't expired.</returns>
        public bool Has(string key)
        {
            if (!_data.TryGetValue(key, out Entry entry))
            {
                return false;
            }
            
            if (entry.HasExpiration && UnityEngine.Time.time >= entry.ExpirationTime)
            {
                _data.Remove(key);
                OnValueExpired?.Invoke(key);
                return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Removes a key from the blackboard.
        /// </summary>
        /// <param name="key">The key to remove.</param>
        /// <returns>True if the key was found and removed.</returns>
        public bool Remove(string key)
        {
            return _data.Remove(key);
        }
        
        /// <summary>
        /// Removes all entries from the blackboard.
        /// </summary>
        public void Clear()
        {
            _data.Clear();
        }
        
        /// <summary>
        /// Removes all expired TTL entries. Called automatically each tick.
        /// </summary>
        public void CleanupExpired()
        {
            _keysToRemove.Clear();
            float currentTime = UnityEngine.Time.time;
            
            foreach (var kvp in _data)
            {
                if (kvp.Value.HasExpiration && currentTime >= kvp.Value.ExpirationTime)
                {
                    _keysToRemove.Add(kvp.Key);
                }
            }
            
            foreach (var key in _keysToRemove)
            {
                _data.Remove(key);
                OnValueExpired?.Invoke(key);
            }
        }
        
        /// <summary>
        /// Gets all non-expired keys in the blackboard.
        /// </summary>
        public IEnumerable<string> Keys
        {
            get
            {
                _keysToRemove.Clear();
                float currentTime = UnityEngine.Time.time;
                
                foreach (var kvp in _data)
                {
                    if (kvp.Value.HasExpiration && currentTime >= kvp.Value.ExpirationTime)
                    {
                        _keysToRemove.Add(kvp.Key);
                    }
                }
                
                foreach (var key in _keysToRemove)
                {
                    _data.Remove(key);
                    OnValueExpired?.Invoke(key);
                }
                
                return _data.Keys;
            }
        }
    }
}
