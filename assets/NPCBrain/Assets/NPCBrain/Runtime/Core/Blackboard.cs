using System;
using System.Collections.Generic;

namespace NPCBrain
{
    public class Blackboard
    {
        private readonly Dictionary<string, object> _data = new Dictionary<string, object>();
        
        public event Action<string, object> OnValueChanged;
        
        public void Set<T>(string key, T value)
        {
            _data[key] = value;
            OnValueChanged?.Invoke(key, value);
        }
        
        public T Get<T>(string key, T defaultValue = default)
        {
            if (_data.TryGetValue(key, out object value) && value is T typedValue)
            {
                return typedValue;
            }
            return defaultValue;
        }
        
        public bool Has(string key)
        {
            return _data.ContainsKey(key);
        }
        
        public bool Remove(string key)
        {
            return _data.Remove(key);
        }
        
        public void Clear()
        {
            _data.Clear();
        }
        
        public IEnumerable<string> Keys => _data.Keys;
    }
}
