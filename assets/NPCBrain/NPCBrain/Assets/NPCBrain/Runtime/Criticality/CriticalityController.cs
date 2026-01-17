using System;
using System.Collections.Generic;

namespace NPCBrain.Criticality
{
    public class CriticalityController
    {
        private const int HistorySize = 20;
        private const float MinTemperature = 0.5f;
        private const float MaxTemperature = 2.0f;
        private const float TemperatureAdjustRate = 0.1f;
        private const float TargetEntropy = 0.5f;
        
        private readonly Queue<int> _actionHistory;
        private readonly Dictionary<int, int> _actionCounts;
        private float _temperature = 1f;
        private float _inertia = 0.5f;
        private float _entropy;
        
        public float Temperature => _temperature;
        public float Inertia => _inertia;
        public float Entropy => _entropy;
        
        public CriticalityController()
        {
            _actionHistory = new Queue<int>();
            _actionCounts = new Dictionary<int, int>();
        }
        
        public void RecordAction(int actionId)
        {
            if (actionId < 0)
            {
                return;
            }
            
            _actionHistory.Enqueue(actionId);
            
            if (_actionCounts.ContainsKey(actionId))
            {
                _actionCounts[actionId]++;
            }
            else
            {
                _actionCounts[actionId] = 1;
            }
            
            while (_actionHistory.Count > HistorySize)
            {
                int oldAction = _actionHistory.Dequeue();
                _actionCounts[oldAction]--;
                if (_actionCounts[oldAction] <= 0)
                {
                    _actionCounts.Remove(oldAction);
                }
            }
        }
        
        public void Update()
        {
            _entropy = CalculateEntropy();
            
            float normalizedEntropy = _actionCounts.Count > 1 
                ? _entropy / (float)Math.Log(_actionCounts.Count) 
                : 0f;
            
            float entropyDelta = normalizedEntropy - TargetEntropy;
            
            if (entropyDelta < -0.1f)
            {
                _temperature += TemperatureAdjustRate;
            }
            else if (entropyDelta > 0.1f)
            {
                _temperature -= TemperatureAdjustRate;
            }
            
            _temperature = Math.Max(MinTemperature, Math.Min(MaxTemperature, _temperature));
            
            _inertia = 1f - normalizedEntropy;
            _inertia = Math.Max(0f, Math.Min(1f, _inertia));
        }
        
        private float CalculateEntropy()
        {
            if (_actionHistory.Count == 0 || _actionCounts.Count <= 1)
            {
                return 0f;
            }
            
            float total = _actionHistory.Count;
            float entropy = 0f;
            
            foreach (var kvp in _actionCounts)
            {
                if (kvp.Value > 0)
                {
                    float probability = kvp.Value / total;
                    entropy -= probability * (float)Math.Log(probability);
                }
            }
            
            return entropy;
        }
        
        public void Reset()
        {
            _actionHistory.Clear();
            _actionCounts.Clear();
            _temperature = 1f;
            _inertia = 0.5f;
            _entropy = 0f;
        }
        
        public void SetTemperature(float temperature)
        {
            _temperature = Math.Max(MinTemperature, Math.Min(MaxTemperature, temperature));
        }
    }
}
