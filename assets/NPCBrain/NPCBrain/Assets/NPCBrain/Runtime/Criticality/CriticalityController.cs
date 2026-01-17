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
        /// <summary>Default number of recent actions to track for entropy calculation.</summary>
        public const int DefaultHistorySize = 20;
        
        /// <summary>Default minimum temperature (most deterministic).</summary>
        public const float DefaultMinTemperature = 0.5f;
        
        /// <summary>Default maximum temperature (most random).</summary>
        public const float DefaultMaxTemperature = 2.0f;
        
        /// <summary>Default rate at which temperature adjusts per update.</summary>
        public const float DefaultTemperatureAdjustRate = 0.1f;
        
        /// <summary>Default target entropy level (0.5 = balanced).</summary>
        public const float DefaultTargetEntropy = 0.5f;
        
        private readonly int _historySize;
        private readonly float _minTemperature;
        private readonly float _maxTemperature;
        private readonly float _temperatureAdjustRate;
        private readonly float _targetEntropy;
        
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
            : this(DefaultHistorySize, DefaultMinTemperature, DefaultMaxTemperature, 
                   DefaultTemperatureAdjustRate, DefaultTargetEntropy)
        {
        }
        
        /// <summary>
        /// Creates a new CriticalityController with custom settings.
        /// </summary>
        /// <param name="historySize">Number of recent actions to track (default: 20).</param>
        /// <param name="minTemperature">Minimum temperature, most deterministic (default: 0.5).</param>
        /// <param name="maxTemperature">Maximum temperature, most random (default: 2.0).</param>
        /// <param name="temperatureAdjustRate">How fast temperature adjusts per update (default: 0.1).</param>
        /// <param name="targetEntropy">Target entropy level, 0.5 = balanced (default: 0.5).</param>
        public CriticalityController(
            int historySize,
            float minTemperature,
            float maxTemperature,
            float temperatureAdjustRate,
            float targetEntropy)
        {
            _historySize = historySize > 0 ? historySize : DefaultHistorySize;
            _minTemperature = minTemperature > 0 ? minTemperature : DefaultMinTemperature;
            _maxTemperature = maxTemperature > _minTemperature ? maxTemperature : DefaultMaxTemperature;
            _temperatureAdjustRate = temperatureAdjustRate > 0 ? temperatureAdjustRate : DefaultTemperatureAdjustRate;
            _targetEntropy = Math.Max(0f, Math.Min(1f, targetEntropy));
            
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
            
            while (_actionHistory.Count > _historySize)
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
            
            float entropyDelta = normalizedEntropy - _targetEntropy;
            
            if (entropyDelta < -0.1f)
            {
                _temperature += _temperatureAdjustRate;
            }
            else if (entropyDelta > 0.1f)
            {
                _temperature -= _temperatureAdjustRate;
            }
            
            _temperature = Math.Max(_minTemperature, Math.Min(_maxTemperature, _temperature));
            
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
            _temperature = Math.Max(_minTemperature, Math.Min(_maxTemperature, temperature));
        }
        
        /// <summary>
        /// Gets the configured history size.
        /// </summary>
        public int HistorySize => _historySize;
        
        /// <summary>
        /// Gets the configured minimum temperature.
        /// </summary>
        public float MinTemperature => _minTemperature;
        
        /// <summary>
        /// Gets the configured maximum temperature.
        /// </summary>
        public float MaxTemperature => _maxTemperature;
    }
}
