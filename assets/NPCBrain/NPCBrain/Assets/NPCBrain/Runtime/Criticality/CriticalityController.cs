using System;
using System.Collections.Generic;

namespace NPCBrain.Criticality
{
    /// <summary>
    /// Manages adaptive exploration vs exploitation through entropy-based temperature control.
    /// Used by <see cref="BehaviorTree.Composites.UtilitySelector"/> for action selection.
    /// </summary>
    /// <remarks>
    /// <para>This system implements "criticality" from statistical mechanics:</para>
    /// <list type="bullet">
    ///   <item><description>Low entropy (repetitive behavior) → Higher temperature → More exploration</description></item>
    ///   <item><description>High entropy (varied behavior) → Lower temperature → More exploitation</description></item>
    /// </list>
    /// <para>This creates naturally varying NPC behavior without manual tuning.</para>
    /// </remarks>
    public class CriticalityController
    {
        /// <summary>Number of recent actions to track for entropy calculation.</summary>
        private const int HistorySize = 20;
        
        /// <summary>Minimum temperature (most deterministic).</summary>
        private const float MinTemperature = 0.5f;
        
        /// <summary>Maximum temperature (most random).</summary>
        private const float MaxTemperature = 2.0f;
        
        /// <summary>How fast temperature adjusts per update.</summary>
        private const float TemperatureAdjustRate = 0.1f;
        
        /// <summary>Target entropy level (0.5 = balanced).</summary>
        private const float TargetEntropy = 0.5f;
        
        private readonly Queue<int> _actionHistory;
        private readonly Dictionary<int, int> _actionCounts;
        private float _temperature = 1f;
        private float _inertia = 0.5f;
        private float _entropy;
        
        /// <summary>
        /// Current temperature for softmax selection.
        /// Lower = more deterministic, Higher = more random.
        /// </summary>
        public float Temperature => _temperature;
        
        /// <summary>
        /// Tendency to stick with current action (inverse of normalized entropy).
        /// </summary>
        public float Inertia => _inertia;
        
        /// <summary>
        /// Shannon entropy of recent action distribution (0 = single action, higher = varied).
        /// </summary>
        public float Entropy => _entropy;
        
        /// <summary>
        /// Creates a new CriticalityController with default settings.
        /// </summary>
        public CriticalityController()
        {
            _actionHistory = new Queue<int>();
            _actionCounts = new Dictionary<int, int>();
        }
        
        /// <summary>
        /// Records that an action was taken. Call this when a UtilityAction completes.
        /// </summary>
        /// <param name="actionId">The index of the action that was taken.</param>
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
        
        /// <summary>
        /// Recalculates entropy and adjusts temperature. Call this each tick.
        /// </summary>
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
        
        /// <summary>
        /// Resets all state to initial values.
        /// </summary>
        public void Reset()
        {
            _actionHistory.Clear();
            _actionCounts.Clear();
            _temperature = 1f;
            _inertia = 0.5f;
            _entropy = 0f;
        }
        
        /// <summary>
        /// Manually sets the temperature (clamped to valid range).
        /// </summary>
        /// <param name="temperature">Desired temperature value.</param>
        public void SetTemperature(float temperature)
        {
            _temperature = Math.Max(MinTemperature, Math.Min(MaxTemperature, temperature));
        }
    }
}
