using System;
using System.Collections.Generic;

namespace NPCBrain
{
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
        
        public event Action<string, object> OnValueChanged;
        public event Action<string> OnValueExpired;
        
        public void Set<T>(string key, T value)
        {
            _data[key] = new Entry { Value = value, HasExpiration = false };
            OnValueChanged?.Invoke(key, value);
        }
        
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
        
        public T Get<T>(string key, T defaultValue = default)
        {
            if (TryGet<T>(key, out T value))
            {
                return value;
            }
            return defaultValue;
        }
        
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
        
        public bool Remove(string key)
        {
            return _data.Remove(key);
        }
        
        public void Clear()
        {
            _data.Clear();
        }
        
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
