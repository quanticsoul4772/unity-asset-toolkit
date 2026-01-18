using System;
using System.Collections.Generic;
using UnityEngine;

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
        private readonly List<string> _keysToRemove = new List<string>(8);
        private readonly List<string> _cachedKeys = new List<string>(16);
        
        // Type-specific storage to avoid boxing for common value types
        private readonly Dictionary<string, float> _floatData = new Dictionary<string, float>();
        private readonly Dictionary<string, int> _intData = new Dictionary<string, int>();
        private readonly Dictionary<string, bool> _boolData = new Dictionary<string, bool>();
        private readonly Dictionary<string, Vector3> _vectorData = new Dictionary<string, Vector3>();
        
        // Track which keys have TTL for efficient cleanup
        private readonly List<(float expirationTime, string key)> _expiringKeys = new List<(float, string)>(8);
        private bool _expiringKeysDirty = false;
        
        /// <summary>
        /// When true, logs warnings when type mismatches occur in Get/TryGet.
        /// Also enabled when NPCBrainDebug.LogBlackboard is true.
        /// </summary>
        public bool LogTypeMismatches { get; set; } = false;
        
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
        /// Sets a float value without boxing.
        /// </summary>
        public void SetFloat(string key, float value)
        {
            _floatData[key] = value;
            OnValueChanged?.Invoke(key, value);
        }
        
        /// <summary>
        /// Gets a float value without unboxing.
        /// </summary>
        public float GetFloat(string key, float defaultValue = 0f)
        {
            return _floatData.TryGetValue(key, out float value) ? value : defaultValue;
        }
        
        /// <summary>
        /// Sets an int value without boxing.
        /// </summary>
        public void SetInt(string key, int value)
        {
            _intData[key] = value;
            OnValueChanged?.Invoke(key, value);
        }
        
        /// <summary>
        /// Gets an int value without unboxing.
        /// </summary>
        public int GetInt(string key, int defaultValue = 0)
        {
            return _intData.TryGetValue(key, out int value) ? value : defaultValue;
        }
        
        /// <summary>
        /// Sets a bool value without boxing.
        /// </summary>
        public void SetBool(string key, bool value)
        {
            _boolData[key] = value;
            OnValueChanged?.Invoke(key, value);
        }
        
        /// <summary>
        /// Gets a bool value without unboxing.
        /// </summary>
        public bool GetBool(string key, bool defaultValue = false)
        {
            return _boolData.TryGetValue(key, out bool value) ? value : defaultValue;
        }
        
        /// <summary>
        /// Sets a Vector3 value without boxing.
        /// </summary>
        public void SetVector3(string key, Vector3 value)
        {
            _vectorData[key] = value;
            OnValueChanged?.Invoke(key, value);
        }
        
        /// <summary>
        /// Gets a Vector3 value without unboxing.
        /// </summary>
        public Vector3 GetVector3(string key, Vector3 defaultValue = default)
        {
            return _vectorData.TryGetValue(key, out Vector3 value) ? value : defaultValue;
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
            float expirationTime = UnityEngine.Time.time + ttlSeconds;
            _data[key] = new Entry
            {
                Value = value,
                ExpirationTime = expirationTime,
                HasExpiration = true
            };
            _expiringKeys.Add((expirationTime, key));
            _expiringKeysDirty = true;
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
            
            // Type mismatch - value exists but is wrong type
            if (LogTypeMismatches || NPCBrainDebug.IsEnabled(NPCBrainDebug.Category.Blackboard))
            {
                string actualType = entry.Value?.GetType().Name ?? "null";
                string requestedType = typeof(T).Name;
                NPCBrainDebug.LogWarning(NPCBrainDebug.Category.Blackboard, 
                    $"Type mismatch for key '{key}': stored type is '{actualType}', " +
                    $"but requested type is '{requestedType}'. Returning default value.");
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
        /// Removes all entries from the blackboard (including type-specific stores).
        /// </summary>
        public void Clear()
        {
            _data.Clear();
            _floatData.Clear();
            _intData.Clear();
            _boolData.Clear();
            _vectorData.Clear();
            _expiringKeys.Clear();
        }
        
        /// <summary>
        /// Removes all expired TTL entries. Called automatically each tick.
        /// </summary>
        public void CleanupExpired()
        {
            if (_expiringKeys.Count == 0) return;
            
            float currentTime = UnityEngine.Time.time;
            _keysToRemove.Clear();
            
            // Sort expiring keys by time if dirty
            if (_expiringKeysDirty)
            {
                _expiringKeys.Sort((a, b) => a.expirationTime.CompareTo(b.expirationTime));
                _expiringKeysDirty = false;
            }
            
            // Process only keys that have expired (sorted, so we can stop early)
            int removeCount = 0;
            for (int i = 0; i < _expiringKeys.Count; i++)
            {
                var (expirationTime, key) = _expiringKeys[i];
                if (expirationTime > currentTime) break;
                
                // Verify the key still exists and is still expired
                if (_data.TryGetValue(key, out Entry entry) && 
                    entry.HasExpiration && 
                    currentTime >= entry.ExpirationTime)
                {
                    _keysToRemove.Add(key);
                }
                removeCount++;
            }
            
            // Remove expired entries from tracking list
            if (removeCount > 0)
            {
                _expiringKeys.RemoveRange(0, removeCount);
            }
            
            // Remove expired entries from data
            for (int i = 0; i < _keysToRemove.Count; i++)
            {
                var key = _keysToRemove[i];
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
                CleanupExpired();
                return _data.Keys;
            }
        }
        
        /// <summary>
        /// Removes all entries from the type-specific stores.
        /// </summary>
        public void ClearAll()
        {
            _data.Clear();
            _floatData.Clear();
            _intData.Clear();
            _boolData.Clear();
            _vectorData.Clear();
            _expiringKeys.Clear();
        }
    }
}
