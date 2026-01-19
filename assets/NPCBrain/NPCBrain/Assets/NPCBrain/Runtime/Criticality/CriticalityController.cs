using System;
using System.Collections.Generic;

namespace NPCBrain.Criticality
{
    /// <summary>
    /// Manages adaptive exploration vs exploitation through multi-metric criticality control.
    /// Used by <see cref="BehaviorTree.Composites.UtilitySelector"/> for action selection.
    /// </summary>
    /// <remarks>
    /// <para>This system implements "criticality" - keeping NPC behavior at the edge of chaos:</para>
    /// <list type="bullet">
    ///   <item><description>Too ordered (repetitive) → Increase temperature/decrease inertia → More exploration</description></item>
    ///   <item><description>Too chaotic (erratic) → Decrease temperature/increase inertia → More exploitation</description></item>
    /// </list>
    /// <para>The system tracks multiple metrics to compute a composite chaos index:</para>
    /// <list type="bullet">
    ///   <item><description><b>Action Entropy</b>: Variety in action selection (Shannon entropy)</description></item>
    ///   <item><description><b>Plan Churn</b>: How often behavior/plan changes</description></item>
    ///   <item><description><b>State Volatility</b>: How often high-level state transitions occur</description></item>
    /// </list>
    /// <para>Inertia (commitment) is computed as inverse of normalized chaos, encouraging
    /// NPCs to stick with actions when behavior is varied, and explore when stuck.</para>
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

        /// <summary>Chaos index delta threshold below which temperature increases.</summary>
        public const float ChaosLowThreshold = -0.1f;

        /// <summary>Chaos index delta threshold above which temperature decreases.</summary>
        public const float ChaosHighThreshold = 0.1f;

        /// <summary>Default inertia value (0.5 = balanced).</summary>
        public const float DefaultInertia = 0.5f;

        /// <summary>Weight for action entropy in chaos index calculation.</summary>
        public const float DefaultEntropyWeight = 1.0f;

        /// <summary>Weight for plan churn in chaos index calculation.</summary>
        public const float DefaultChurnWeight = 0.8f;

        /// <summary>Weight for state volatility in chaos index calculation.</summary>
        public const float DefaultVolatilityWeight = 0.6f;

        // Legacy constants for backward compatibility
        /// <summary>Entropy delta threshold below which temperature increases (legacy, use ChaosLowThreshold).</summary>
        public const float EntropyLowThreshold = -0.1f;

        /// <summary>Entropy delta threshold above which temperature decreases (legacy, use ChaosHighThreshold).</summary>
        public const float EntropyHighThreshold = 0.1f;

        private readonly int _historySize;
        private readonly float _minTemperature;
        private readonly float _maxTemperature;
        private readonly float _temperatureAdjustRate;
        private readonly float _targetEntropy;

        // Metric weights (configurable)
        private readonly float _entropyWeight;
        private readonly float _churnWeight;
        private readonly float _volatilityWeight;

        // Action tracking (for entropy)
        private readonly Queue<int> _actionHistory;
        private readonly Dictionary<int, int> _actionCounts;

        // Plan tracking (for churn)
        private readonly Queue<int> _planHistory;
        private int _lastPlanId = -1;

        // State tracking (for volatility)
        private readonly Queue<int> _stateHistory;
        private int _lastStateId = -1;

        private float _temperature = 1f;
        private float _inertia = DefaultInertia;
        private float _entropy;
        private float _planChurn;
        private float _stateVolatility;
        private float _chaosIndex;
        private bool _metricsDirty = true;
        
        /// <summary>
        /// Current temperature for softmax selection.
        /// Lower = more deterministic, Higher = more random.
        /// </summary>
        public float Temperature => _temperature;

        /// <summary>
        /// Tendency to stick with current action (inverse of normalized chaos index).
        /// High inertia = NPC commits to actions. Low inertia = NPC explores alternatives.
        /// </summary>
        public float Inertia => _inertia;

        /// <summary>
        /// Shannon entropy of recent action distribution (0 = single action, higher = varied).
        /// </summary>
        public float Entropy => _entropy;

        /// <summary>
        /// Rate of plan/behavior changes (0 = never changes, 1 = changes every tick).
        /// </summary>
        public float PlanChurn => _planChurn;

        /// <summary>
        /// Rate of high-level state transitions (0 = stable, 1 = highly volatile).
        /// </summary>
        public float StateVolatility => _stateVolatility;

        /// <summary>
        /// Composite chaos index combining all metrics (0 = ordered, 1 = chaotic).
        /// This is the primary measure used for temperature/inertia adjustment.
        /// </summary>
        public float ChaosIndex => _chaosIndex;
        
        /// <summary>
        /// Creates a new CriticalityController with default settings.
        /// <summary>
        /// Initializes a CriticalityController with default history size, temperature bounds, adjustment rate, target chaos, and metric weights.
        /// </summary>
        public CriticalityController()
            : this(DefaultHistorySize, DefaultMinTemperature, DefaultMaxTemperature,
                   DefaultTemperatureAdjustRate, DefaultTargetEntropy,
                   DefaultEntropyWeight, DefaultChurnWeight, DefaultVolatilityWeight)
        {
        }

        /// <summary>
        /// Creates a new CriticalityController with custom settings (backward compatible).
        /// </summary>
        /// <param name="historySize">Number of recent events to track (default: 20).</param>
        /// <param name="minTemperature">Minimum temperature, most deterministic (default: 0.5).</param>
        /// <param name="maxTemperature">Maximum temperature, most random (default: 2.0).</param>
        /// <param name="temperatureAdjustRate">How fast temperature adjusts per update (default: 0.1).</param>
        /// <summary>
        /// Initializes a CriticalityController with core configuration and default metric weights.
        /// </summary>
        /// <param name="historySize">Number of recent events to track for metrics (clamped to at least 1).</param>
        /// <param name="minTemperature">Minimum allowed temperature (lower exploration bound).</param>
        /// <param name="maxTemperature">Maximum allowed temperature (upper exploration bound).</param>
        /// <param name="temperatureAdjustRate">Amount to change temperature per update when chaos deviates from the target.</param>
        /// <param name="targetEntropy">Target chaos level used to compare against the composite chaos index; 0.5 represents a balanced regime.</param>
        public CriticalityController(
            int historySize,
            float minTemperature,
            float maxTemperature,
            float temperatureAdjustRate,
            float targetEntropy)
            : this(historySize, minTemperature, maxTemperature, temperatureAdjustRate, targetEntropy,
                   DefaultEntropyWeight, DefaultChurnWeight, DefaultVolatilityWeight)
        {
        }

        /// <summary>
        /// Creates a new CriticalityController with full custom settings including metric weights.
        /// </summary>
        /// <param name="historySize">Number of recent events to track (default: 20).</param>
        /// <param name="minTemperature">Minimum temperature, most deterministic (default: 0.5).</param>
        /// <param name="maxTemperature">Maximum temperature, most random (default: 2.0).</param>
        /// <param name="temperatureAdjustRate">How fast temperature adjusts per update (default: 0.1).</param>
        /// <param name="targetEntropy">Target chaos level, 0.5 = balanced (default: 0.5).</param>
        /// <param name="entropyWeight">Weight for action entropy in chaos index (default: 1.0).</param>
        /// <param name="churnWeight">Weight for plan churn in chaos index (default: 0.8).</param>
        /// <summary>
        /// Initializes a CriticalityController with explicit configuration for history length, temperature bounds and dynamics, target chaos, and metric weights.
        /// </summary>
        /// <param name="historySize">Maximum number of recent events to track; if less than or equal to 0, DefaultHistorySize is used.</param>
        /// <param name="minTemperature">Minimum allowed temperature; if less than or equal to 0, DefaultMinTemperature is used.</param>
        /// <param name="maxTemperature">Maximum allowed temperature; if not greater than <paramref name="minTemperature"/>, DefaultMaxTemperature is used.</param>
        /// <param name="temperatureAdjustRate">Per-update temperature adjustment step; if less than or equal to 0, DefaultTemperatureAdjustRate is used.</param>
        /// <param name="targetEntropy">Target chaos level (treated as a normalized entropy/chaos target) and clamped to the range [0,1].</param>
        /// <param name="entropyWeight">Non-negative weight applied to action entropy when computing the composite chaos index.</param>
        /// <param name="churnWeight">Non-negative weight applied to plan churn when computing the composite chaos index.</param>
        /// <param name="volatilityWeight">Non-negative weight applied to state volatility when computing the composite chaos index (default: 0.6).</param>
        public CriticalityController(
            int historySize,
            float minTemperature,
            float maxTemperature,
            float temperatureAdjustRate,
            float targetEntropy,
            float entropyWeight,
            float churnWeight,
            float volatilityWeight)
        {
            _historySize = historySize > 0 ? historySize : DefaultHistorySize;
            _minTemperature = minTemperature > 0 ? minTemperature : DefaultMinTemperature;
            _maxTemperature = maxTemperature > _minTemperature ? maxTemperature : DefaultMaxTemperature;
            _temperatureAdjustRate = temperatureAdjustRate > 0 ? temperatureAdjustRate : DefaultTemperatureAdjustRate;
            _targetEntropy = Math.Max(0f, Math.Min(1f, targetEntropy));

            _entropyWeight = Math.Max(0f, entropyWeight);
            _churnWeight = Math.Max(0f, churnWeight);
            _volatilityWeight = Math.Max(0f, volatilityWeight);

            _actionHistory = new Queue<int>();
            _actionCounts = new Dictionary<int, int>();
            _planHistory = new Queue<int>();
            _stateHistory = new Queue<int>();
        }
        
        /// <summary>
        /// Records that an action was taken. Call this when a UtilityAction completes.
        /// </summary>
        /// <param name="actionId">Index of the action taken; negative values are ignored.</param>
        /// <remarks>
        /// Enqueues the action into the bounded history, updates occurrence counts, and marks
        /// cached metrics as needing recalculation. If the history exceeds the configured
        /// capacity, the oldest action is removed and its count decremented.
        /// <summary>
        /// Records an action occurrence, updating the rolling action history and frequency counts.
        /// </summary>
        /// <param name="actionId">Identifier of the action to record; negative values are ignored.</param>
        /// <remarks>
        /// Marks internal metrics as needing recalculation, increments the occurrence count for the action,
        /// and trims the stored history to the configured HistorySize by removing oldest entries and
        /// decrementing/removing their counts as needed.
        /// </remarks>
        public void RecordAction(int actionId)
        {
            if (actionId < 0)
            {
                return;
            }

            _actionHistory.Enqueue(actionId);
            _metricsDirty = true;

            // Performance: Use TryGetValue to avoid double dictionary lookup
            if (_actionCounts.TryGetValue(actionId, out int count))
            {
                _actionCounts[actionId] = count + 1;
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
        /// Records a plan/behavior change. Call this when the NPC switches to a different behavior.
        /// </summary>
        /// <param name="planId">Identifier for the current plan/behavior.</param>
        /// <remarks>
        /// <para>Plan churn measures how often the NPC changes its high-level approach.
        /// High churn indicates erratic planning; low churn indicates stable, committed behavior.</para>
        /// <para>Examples of when to call this:</para>
        /// <list type="bullet">
        ///   <item><description>When switching from "Patrol" to "Chase" behavior</description></item>
        ///   <item><description>When selecting a new UtilityAction (can use action index)</description></item>
        ///   <item><description>When a Selector node picks a different branch</description></item>
        /// </list>
        /// <summary>
        /// Records a plan identifier into the recent plan history and updates internal state for metric recalculation.
        /// </summary>
        /// <param name="planId">The plan identifier to record; negative values are ignored.</param>
        public void RecordPlan(int planId)
        {
            if (planId < 0)
            {
                return;
            }

            _planHistory.Enqueue(planId);
            _metricsDirty = true;

            // Track if this is a change from the previous plan
            _lastPlanId = planId;

            while (_planHistory.Count > _historySize)
            {
                _planHistory.Dequeue();
            }
        }

        /// <summary>
        /// Records a high-level state transition. Call this when the NPC's overall state changes.
        /// </summary>
        /// <param name="stateId">Identifier for the current state.</param>
        /// <remarks>
        /// <para>State volatility measures how rapidly the NPC transitions between states.
        /// High volatility indicates instability; low volatility indicates consistency.</para>
        /// <para>Examples of when to call this:</para>
        /// <list type="bullet">
        ///   <item><description>FSM state changes (Idle → Alert → Combat)</description></item>
        ///   <item><description>Behavior tree branch switches</description></item>
        ///   <item><description>Goal changes in GOAP systems</description></item>
        /// </list>
        /// <summary>
        /// Records a state transition ID into the controller's state history.
        /// </summary>
        /// <param name="stateId">The state identifier to record; negative values are ignored.</param>
        /// <remarks>Marks metrics as dirty, updates the last recorded state ID, and trims the stored history to the configured history size.</remarks>
        public void RecordStateTransition(int stateId)
        {
            if (stateId < 0)
            {
                return;
            }

            _stateHistory.Enqueue(stateId);
            _metricsDirty = true;

            _lastStateId = stateId;

            while (_stateHistory.Count > _historySize)
            {
                _stateHistory.Dequeue();
            }
        }
        
        /// <summary>
        /// Recalculates all metrics and adjusts temperature/inertia. Call this each tick.
        /// </summary>
        /// <remarks>
        /// <para>The update process:</para>
        /// <list type="number">
        ///   <item><description>Calculate individual metrics (entropy, churn, volatility)</description></item>
        ///   <item><description>Compute weighted chaos index from all metrics</description></item>
        ///   <item><description>Adjust temperature based on chaos vs target</description></item>
        ///   <item><description>Compute inertia as inverse of chaos (more chaos = less commitment)</description></item>
        /// </list>
        /// <summary>
        /// Recomputes internal criticality metrics and adapts temperature and inertia based on the composite chaos index.
        /// </summary>
        /// <remarks>
        /// If recent history changed, recomputes action entropy, plan churn, and state volatility; then normalizes and combines them into a weighted chaos index. The method clamps the chaos index to [0,1], adjusts the temperature toward the configured target entropy using configured thresholds and adjust rate, clamps temperature to configured bounds, and updates inertia to be 1 minus the chaos index.
        /// </remarks>
        public void Update()
        {
            // Only recalculate metrics when history changes
            if (_metricsDirty)
            {
                _entropy = CalculateEntropy();
                _planChurn = CalculatePlanChurn();
                _stateVolatility = CalculateStateVolatility();
                _metricsDirty = false;
            }

            // Normalize entropy to 0-1 range
            float normalizedEntropy = _actionCounts.Count > 1
                ? _entropy / (float)Math.Log(_actionCounts.Count)
                : 0f;

            // Compute weighted chaos index from all metrics
            float totalWeight = _entropyWeight + _churnWeight + _volatilityWeight;
            if (totalWeight > 0f)
            {
                _chaosIndex = (
                    _entropyWeight * normalizedEntropy +
                    _churnWeight * _planChurn +
                    _volatilityWeight * _stateVolatility
                ) / totalWeight;
            }
            else
            {
                _chaosIndex = normalizedEntropy; // Fallback to entropy only
            }

            // Clamp chaos index to valid range
            _chaosIndex = Math.Max(0f, Math.Min(1f, _chaosIndex));

            // Adjust temperature based on chaos vs target
            float chaosDelta = _chaosIndex - _targetEntropy;

            if (chaosDelta < ChaosLowThreshold)
            {
                // Too ordered - increase temperature to encourage exploration
                _temperature += _temperatureAdjustRate;
            }
            else if (chaosDelta > ChaosHighThreshold)
            {
                // Too chaotic - decrease temperature to encourage exploitation
                _temperature -= _temperatureAdjustRate;
            }

            _temperature = Math.Max(_minTemperature, Math.Min(_maxTemperature, _temperature));

            // Inertia is inverse of chaos - high chaos means low commitment, low chaos means high commitment
            // This creates the adaptive feedback: repetitive behavior → low inertia → more likely to try alternatives
            _inertia = 1f - _chaosIndex;
            _inertia = Math.Max(0f, Math.Min(1f, _inertia));
        }
        
        /// <summary>
        /// Computes the Shannon entropy of the recorded action distribution in the action history using natural logarithm.
        /// </summary>
        /// <returns>The entropy of the action distribution in natural units (nats); `0` if there are no actions or only one unique action recorded.</returns>
        private float CalculateEntropy()
        {
            if (_actionHistory.Count == 0 || _actionCounts.Count <= 1)
            {
                return 0f;
            }

            float total = _actionHistory.Count;
            float entropy = 0f;

            // Use struct enumerator directly to avoid allocation
            var enumerator = _actionCounts.GetEnumerator();
            while (enumerator.MoveNext())
            {
                int count = enumerator.Current.Value;
                if (count > 0)
                {
                    float probability = count / total;
                    entropy -= probability * (float)Math.Log(probability);
                }
            }
            enumerator.Dispose();

            return entropy;
        }

        /// <summary>
        /// Calculates plan churn as the rate of plan changes in recent history.
        /// </summary>
        /// <summary>
        /// Calculates the fraction of consecutive plan entries that differ, indicating how frequently plans change.
        /// </summary>
        /// <returns>Fraction between 0 (no consecutive changes) and 1 (every consecutive entry is a change).</returns>
        private float CalculatePlanChurn()
        {
            if (_planHistory.Count < 2)
            {
                return 0f;
            }

            int changes = 0;
            int lastPlan = -1;
            bool first = true;

            foreach (int plan in _planHistory)
            {
                if (first)
                {
                    lastPlan = plan;
                    first = false;
                    continue;
                }

                if (plan != lastPlan)
                {
                    changes++;
                    lastPlan = plan;
                }
            }

            // Normalize: max changes = N-1
            return (float)changes / (_planHistory.Count - 1);
        }

        /// <summary>
        /// Calculates state volatility as the rate of state transitions in recent history.
        /// </summary>
        /// <summary>
        /// Computes the proportion of transitions in the recorded state history that change value.
        /// </summary>
        /// <returns>Value between 0 and 1 representing the fraction of consecutive state entries that differ; returns 0 if fewer than two states are recorded.</returns>
        private float CalculateStateVolatility()
        {
            if (_stateHistory.Count < 2)
            {
                return 0f;
            }

            int transitions = 0;
            int lastState = -1;
            bool first = true;

            foreach (int state in _stateHistory)
            {
                if (first)
                {
                    lastState = state;
                    first = false;
                    continue;
                }

                if (state != lastState)
                {
                    transitions++;
                    lastState = state;
                }
            }

            // Normalize: max transitions = N-1
            return (float)transitions / (_stateHistory.Count - 1);
        }

        /// <summary>
        /// Resets all state to initial values.
        /// <summary>
        /// Clears all recorded histories and resets internal metrics, temperature, inertia, and last IDs to their initial defaults.
        /// </summary>
        /// <remarks>
        /// After calling this method, all action, plan, and state histories are empty, metric values (entropy, plan churn, state volatility, chaos index) are reset to zero, temperature and inertia are restored to their defaults, and internal metrics are marked dirty for recalculation.
        /// </remarks>
        public void Reset()
        {
            _actionHistory.Clear();
            _actionCounts.Clear();
            _planHistory.Clear();
            _stateHistory.Clear();

            _lastPlanId = -1;
            _lastStateId = -1;

            _temperature = 1f;
            _inertia = DefaultInertia;
            _entropy = 0f;
            _planChurn = 0f;
            _stateVolatility = 0f;
            _chaosIndex = 0f;
            _metricsDirty = true;
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
        
        /// <summary>
        /// Gets the configured target entropy level (0-1).
        /// </summary>
        public float TargetEntropy => _targetEntropy;
        
        /// <summary>
        /// Gets the configured temperature adjustment rate.
        /// </summary>
        public float TemperatureAdjustRate => _temperatureAdjustRate;
        
        /// <summary>
        /// Gets the number of unique actions in the current history.
        /// </summary>
        public int UniqueActionCount => _actionCounts.Count;
        
        /// <summary>
        /// Gets the total number of recorded actions in history.
        /// </summary>
        public int ActionHistoryCount => _actionHistory.Count;

        /// <summary>
        /// Gets the total number of recorded plans in history.
        /// </summary>
        public int PlanHistoryCount => _planHistory.Count;

        /// <summary>
        /// Gets the total number of recorded state transitions in history.
        /// </summary>
        public int StateHistoryCount => _stateHistory.Count;

        /// <summary>
        /// Gets the weight for action entropy in chaos index calculation.
        /// </summary>
        public float EntropyWeight => _entropyWeight;

        /// <summary>
        /// Gets the weight for plan churn in chaos index calculation.
        /// </summary>
        public float ChurnWeight => _churnWeight;

        /// <summary>
        /// Gets the weight for state volatility in chaos index calculation.
        /// </summary>
        public float VolatilityWeight => _volatilityWeight;

        /// <summary>
        /// Returns true if the system is in the "too ordered" regime (needs more exploration).
        /// </summary>
        public bool IsTooOrdered => (_chaosIndex - _targetEntropy) < ChaosLowThreshold;

        /// <summary>
        /// Returns true if the system is in the "too chaotic" regime (needs more stability).
        /// </summary>
        public bool IsTooChaotic => (_chaosIndex - _targetEntropy) > ChaosHighThreshold;

        /// <summary>
        /// Returns true if the system is in the critical band (balanced exploration/exploitation).
        /// </summary>
        public bool IsInCriticalBand => !IsTooOrdered && !IsTooChaotic;
    }
}