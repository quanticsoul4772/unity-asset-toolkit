using System;
using System.Collections.Generic;
using UnityEngine;
using NPCBrain.UtilityAI;

namespace NPCBrain.BehaviorTree.Composites
{
    /// <summary>
    /// Selects and executes actions based on utility scores using softmax selection.
    /// Integrates with <see cref="Criticality.CriticalityController"/> for adaptive exploration.
    /// </summary>
    /// <remarks>
    /// <para>This is the core of the Utility AI system. Each action's score is computed
    /// from its considerations, then softmax selection chooses an action based on
    /// the current temperature setting.</para>
    /// <list type="bullet">
    ///   <item><description>Low temperature (0.5): More deterministic, favors highest scores</description></item>
    ///   <item><description>High temperature (2.0): More random, explores varied actions</description></item>
    /// </list>
    /// <para>Actions with score ≤ 0 are excluded from selection entirely.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var patrol = new UtilityAction("Patrol", patrolBehavior, new ConstantConsideration(0.6f));
    /// var idle = new UtilityAction("Idle", new Wait(2f), new ConstantConsideration(0.2f));
    /// var selector = new UtilitySelector(patrol, idle);
    /// </code>
    /// </example>
    public class UtilitySelector : BTNode
    {
        /// <summary>
        /// Fast exp approximation using Schraudolph's algorithm.
        /// Accurate to within ~2% for typical softmax ranges (-10 to 0).
        /// <summary>
        /// Approximates the exponential function e^x using a fast IEEE float bit-level approximation with clamping for extreme inputs.
        /// </summary>
        /// <param name="x">The exponent value.</param>
        /// <returns>`e^x` approximated; returns 0 when x &lt; -20, returns `exp(20)` when x &gt; 20, otherwise an efficient float-precision approximation.</returns>
        private static float FastExp(float x)
        {
            // Clamp to avoid overflow/underflow
            if (x < -20f) return 0f;
            if (x > 20f) return (float)Math.Exp(20f);

            // Schraudolph's approximation: exp(x) ≈ 2^(x/ln2) via IEEE float bit manipulation
            // For softmax with normalized scores, this provides sufficient accuracy
            const float a = 12102203.16156f; // (1 << 23) / ln(2)
            const float b = 1064866805.0f;   // (1 << 23) * (127 - ~0.043677448f * ln(2))
            int i = (int)(a * x + b);
            return BitConverter.Int32BitsToSingle(i);
        }

        private readonly List<UtilityAction> _actions;
        private readonly List<float> _scoresList;
        private readonly List<float> _probabilitiesList;
        private UtilityAction _currentAction;
        private int _currentActionIndex = -1;
        private int _lastSelectedActionIndex = -1; // Tracks previous selection for inertia
        private float[] _scores;
        private float[] _probabilities;
        private readonly System.Random _random;
        
        // Logging throttle: only log periodically or on action change
        private float _lastLogTime;
        private const float LOG_INTERVAL = 2f; // Log every 2 seconds max
        private string _lastLoggedActionName;
        
        /// <summary>
        /// When true, logs warnings when no action can be selected.
        /// Also enabled when NPCBrainDebug.LogUtility is true.
        /// </summary>
        public bool LogWarnings { get; set; } = false;
        
        /// <summary>
        /// Creates a UtilitySelector with the specified actions.
        /// </summary>
        /// <param name="actions">The utility actions to choose from.</param>
        public UtilitySelector(params UtilityAction[] actions)
        {
            _actions = new List<UtilityAction>(actions);
            _scoresList = new List<float>(actions.Length);
            _probabilitiesList = new List<float>(actions.Length);
            _scores = new float[actions.Length];
            _probabilities = new float[actions.Length];
            _random = new System.Random();
            Name = "UtilitySelector";
        }
        
        /// <summary>
        /// Creates a UtilitySelector with a fixed random seed (for testing).
        /// </summary>
        /// <param name="seed">Random seed for deterministic behavior.</param>
        /// <param name="actions">The utility actions to choose from.</param>
        public UtilitySelector(int seed, params UtilityAction[] actions)
        {
            _actions = new List<UtilityAction>(actions);
            _scoresList = new List<float>(actions.Length);
            _probabilitiesList = new List<float>(actions.Length);
            _scores = new float[actions.Length];
            _probabilities = new float[actions.Length];
            _random = new System.Random(seed);
            Name = "UtilitySelector";
        }
        
        /// <summary>
        /// Adds a new action to the selector at runtime.
        /// </summary>
        /// <param name="action">The action to add.</param>
        public void AddAction(UtilityAction action)
        {
            _actions.Add(action);
            EnsureArrayCapacity();
        }
        
        private void EnsureArrayCapacity()
        {
            if (_scores.Length < _actions.Count)
            {
                _scores = new float[_actions.Count];
                _probabilities = new float[_actions.Count];
            }
        }
        
        /// <summary>
        /// Threshold for interrupting current action. If a new action scores this much higher
        /// than the current action, interrupt and switch to the new action.
        /// </summary>
        public float InterruptThreshold { get; set; } = 0.3f;
        
        /// <summary>
        /// How often to check for action interruption (in seconds). Default 0.1s.
        /// Lower values = more responsive but higher CPU cost.
        /// </summary>
        public float InterruptCheckInterval { get; set; } = 0.1f;
        
        private float _lastInterruptCheckTime;
        
        /// <summary>
        /// Selects or maintains a utility-based action for the NPC, executes it, and handles periodic interruption checks and action completion recording.
        /// </summary>
        /// <param name="brain">The NPC brain used to evaluate action scores, provide criticality parameters, and execute/abort actions.</param>
        /// <summary>
        /// Selects and executes a utility action based on current scores, handling interruptions, forced switches, inertia updates, and completion recording.
        /// </summary>
        /// <param name="brain">The NPC brain controller used to evaluate action scores, execute or abort actions, and record action/plan outcomes.</param>
        /// <returns>The current action's <see cref="NodeStatus"/>: `Running` if the action is still executing, otherwise `Success` or `Failure` reflecting the action's outcome.</returns>
        protected override NodeStatus Tick(NPCBrainController brain)
        {
            if (_actions.Count == 0)
            {
                if (ShouldLogWarning())
                {
                    NPCBrainDebug.LogWarning(NPCBrainDebug.Category.Utility, 
                        "No actions configured. Add actions before executing.");
                }
                return NodeStatus.Failure;
            }
            
            // IMPORTANT: Re-evaluate scores periodically to allow interruption
            // This fixes the issue where cops wouldn't switch from Patrol to Chase
            if (_currentAction != null)
            {
                // Throttle interruption checks for performance (default every 0.25s)
                float currentTime = Time.time;
                if (currentTime - _lastInterruptCheckTime >= InterruptCheckInterval)
                {
                    _lastInterruptCheckTime = currentTime;
                    
                    // Check if a significantly better action is available
                    float currentScore = _currentAction.Score(brain);
                    UtilityAction bestAction = null;
                    float bestScore = currentScore;
                    int bestIndex = _currentActionIndex;
                    
                    // Track best positive-scoring action for force-switch (avoids second loop)
                    UtilityAction bestPositiveAction = null;
                    float bestPositiveScore = 0f;
                    int bestPositiveIndex = -1;
                    
                    for (int i = 0; i < _actions.Count; i++)
                    {
                        if (_actions[i] == _currentAction) continue;
                        float score = _actions[i].Score(brain);
                        
                        // Check for interrupt (significantly better action)
                        if (score > bestScore + InterruptThreshold)
                        {
                            bestScore = score;
                            bestAction = _actions[i];
                            bestIndex = i;
                        }
                        
                        // Track best positive for potential force-switch
                        if (score > bestPositiveScore)
                        {
                            bestPositiveScore = score;
                            bestPositiveAction = _actions[i];
                            bestPositiveIndex = i;
                        }
                    }
                    
                    // If a significantly better action exists, interrupt current and switch
                    if (bestAction != null)
                    {
                        if (NPCBrainDebug.IsEnabled(NPCBrainDebug.Category.Utility))
                        {
                            Debug.Log($"<color=yellow>[UtilitySelector]</color> Interrupting {_currentAction.Name} (score {currentScore:F2}) for {bestAction.Name} (score {bestScore:F2})");
                        }
                        _currentAction.Action.Abort(brain);
                        _currentAction = bestAction;
                        _currentActionIndex = bestIndex;
                        _lastSelectedActionIndex = bestIndex; // Update inertia history to track the new action
                    }
                    else if (currentScore <= 0f && bestPositiveAction != null)
                    {
                        // IMPORTANT: If current action scores 0, we MUST switch to something else!
                        // Use the best positive action we already found during the loop
                        if (NPCBrainDebug.IsEnabled(NPCBrainDebug.Category.Utility))
                        {
                            Debug.Log($"<color=yellow>[UtilitySelector]</color> Force-switching from {_currentAction.Name} (score 0) to {bestPositiveAction.Name} (score {bestPositiveScore:F2})");
                        }
                        _currentAction.Action.Abort(brain);
                        _currentAction = bestPositiveAction;
                        _currentActionIndex = bestPositiveIndex;
                        _lastSelectedActionIndex = bestPositiveIndex; // Update inertia history to track the new action
                    }
                }
            }
            else
            {
                _currentAction = SelectAction(brain);
                if (_currentAction == null)
                {
                    // Warning already logged in SelectAction
                    return NodeStatus.Failure;
                }
            }
            
            NodeStatus status = _currentAction.Action.Execute(brain);

            if (status != NodeStatus.Running)
            {
                // Record both the action completion and the plan (for entropy and churn metrics)
                brain.Criticality?.RecordAction(_currentActionIndex);
                brain.Criticality?.RecordPlan(_currentActionIndex);
                _currentAction = null;
                _currentActionIndex = -1;
            }

            return status;
        }
        
        /// <summary>
        /// Selects a UtilityAction using a softmax distribution over actions with positive scores.
        /// </summary>
        /// <remarks>
        /// <para>Selection process:</para>
        /// <list type="number">
        ///   <item><description>Score all actions and filter out non-positive scores</description></item>
        ///   <item><description>Apply softmax with temperature from CriticalityController</description></item>
        ///   <item><description>Apply inertia boost to previous action (encourages commitment)</description></item>
        ///   <item><description>Sample from the resulting probability distribution</description></item>
        /// </list>
        /// <para>Inertia creates "stickiness" - NPCs are more likely to continue their current action,
        /// preventing erratic flip-flopping while still allowing adaptation when scores change significantly.</para>
        /// </remarks>
        /// <param name="brain">The NPCBrainController used to evaluate action scores and obtain criticality parameters.</param>
        /// <summary>
        /// Selects a UtilityAction using a softmax distribution over action scores, applying temperature scaling and an optional inertia-based bias toward the previously chosen action.
        /// </summary>
        /// <param name="brain">The NPC brain used to evaluate action scores and to obtain temperature and inertia settings; if null, no action is selected.</param>
        /// <returns>The chosen UtilityAction, or null if no action could be selected.</returns>
        /// <remarks>
        /// Actions with scores less than or equal to zero are excluded from selection. Temperature controls exploration (higher values flatten probabilities); inertia, if positive, boosts the probability of the previously selected action proportionally to its remaining headroom and renormalizes the distribution. Returns null when the brain is null, when all actions score <= 0, or when numerical conditions prevent a valid probability distribution.
        /// <summary>
        /// Selects a UtilityAction from the configured actions by computing a softmax over positive action scores using the brain's criticality temperature and optionally biasing toward the previously selected action via inertia.
        /// </summary>
        /// <param name="brain">The NPC brain providing context and Criticality parameters (temperature and inertia). If null, selection cannot proceed.</param>
        /// <returns>The chosen UtilityAction, or null if no viable action can be selected (for example, when the brain is null or all actions score ≤ 0).</returns>
        private UtilityAction SelectAction(NPCBrainController brain)
        {
            if (brain == null)
            {
                if (ShouldLogWarning())
                {
                    NPCBrainDebug.LogWarning(NPCBrainDebug.Category.Utility,
                        "Brain is null. Cannot select action.");
                }
                return null;
            }

            float temperature = brain.Criticality?.Temperature ?? 1f;
            float inertia = brain.Criticality?.Inertia ?? 0f;

            float maxScore = float.MinValue;
            for (int i = 0; i < _actions.Count; i++)
            {
                _scores[i] = _actions[i].Score(brain);
                if (_scores[i] > maxScore)
                {
                    maxScore = _scores[i];
                }
            }

            if (maxScore <= 0f)
            {
                if (ShouldLogWarning())
                {
                    NPCBrainDebug.LogWarning(NPCBrainDebug.Category.Utility,
                        $"All {_actions.Count} action(s) scored <= 0. No action selected. " +
                        $"Check that considerations return positive values.");
                }
                return null;
            }

            float sumExp = 0f;
            for (int i = 0; i < _actions.Count; i++)
            {
                // Exclude actions with zero or negative scores from selection
                if (_scores[i] <= 0f)
                {
                    _probabilities[i] = 0f;
                    continue;
                }

                float scaledScore = (_scores[i] - maxScore) / temperature;
                // Performance: Use fast exp approximation for softmax calculation
                _probabilities[i] = FastExp(scaledScore);
                sumExp += _probabilities[i];
            }

            if (sumExp <= 0f)
            {
                if (ShouldLogWarning())
                {
                    NPCBrainDebug.LogWarning(NPCBrainDebug.Category.Utility,
                        "Softmax sum is zero after filtering. This shouldn't happen if maxScore > 0.");
                }
                return null;
            }

            // Normalize to get initial probabilities
            for (int i = 0; i < _actions.Count; i++)
            {
                _probabilities[i] /= sumExp;
            }

            // Apply inertia: boost probability of previous action to encourage commitment
            // This creates "stickiness" - NPCs don't flip-flop between similar-scoring actions
            // Formula: p[prev] += inertia * (1 - p[prev]) — proportional boost based on headroom
            if (inertia > 0f && _lastSelectedActionIndex >= 0 && _lastSelectedActionIndex < _actions.Count)
            {
                // Only apply inertia if the previous action is still viable (positive score)
                if (_scores[_lastSelectedActionIndex] > 0f)
                {
                    float currentProb = _probabilities[_lastSelectedActionIndex];
                    float boost = inertia * (1f - currentProb);
                    _probabilities[_lastSelectedActionIndex] = currentProb + boost;

                    // Renormalize probabilities after inertia boost
                    float newSum = 0f;
                    for (int i = 0; i < _actions.Count; i++)
                    {
                        newSum += _probabilities[i];
                    }
                    if (newSum > 0f)
                    {
                        for (int i = 0; i < _actions.Count; i++)
                        {
                            _probabilities[i] /= newSum;
                        }
                    }

                    // Inertia logging is now combined with the main action log below to reduce spam
                }
            }

            // Debug: Log action scores - but only every LOG_INTERVAL seconds OR when action changes
            // This drastically reduces log spam while still showing important state changes

            float randomValue = (float)_random.NextDouble();
            float cumulative = 0f;

            for (int i = 0; i < _actions.Count; i++)
            {
                cumulative += _probabilities[i];
                if (randomValue <= cumulative)
                {
                    _currentActionIndex = i;
                    _lastSelectedActionIndex = i;
                    LogActionSelection(i, temperature, inertia);
                    return _actions[i];
                }
            }

            _currentActionIndex = _actions.Count - 1;
            _lastSelectedActionIndex = _actions.Count - 1;
            LogActionSelection(_actions.Count - 1, temperature, inertia);
            return _actions[_actions.Count - 1];
        }
        
        /// <summary>
        /// Logs action selection, but only on action change or every LOG_INTERVAL seconds to reduce spam.
        /// </summary>
        private void LogActionSelection(int selectedIndex, float temperature, float inertia)
        {
            if (!NPCBrainDebug.IsEnabled(NPCBrainDebug.Category.Utility))
                return;
                
            string selectedActionName = _actions[selectedIndex].Name;
            float currentTime = Time.time;
            bool actionChanged = selectedActionName != _lastLoggedActionName;
            bool timeElapsed = currentTime - _lastLogTime >= LOG_INTERVAL;
            
            // Only log if action changed OR enough time has passed
            if (!actionChanged && !timeElapsed)
                return;
                
            _lastLogTime = currentTime;
            string previousActionName = _lastLoggedActionName; // Capture before update
            _lastLoggedActionName = selectedActionName;
            
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            
            if (actionChanged && previousActionName != null)
            {
                sb.Append($"<color=green>[UtilitySelector]</color> ACTION CHANGED → ");
            }
            else
            {
                sb.Append($"[UtilitySelector] ");
            }
            
            sb.Append($"T={temperature:F2} I={inertia:F2} | ");
            for (int i = 0; i < _actions.Count; i++)
            {
                if (i == selectedIndex)
                {
                    sb.Append($"<b>{_actions[i].Name}={_scores[i]:F2}({_probabilities[i]:P0})</b> ");
                }
                else
                {
                    sb.Append($"{_actions[i].Name}={_scores[i]:F2}({_probabilities[i]:P0}) ");
                }
            }
            Debug.Log(sb.ToString());
        }
        
        protected override void OnEnter(NPCBrainController brain)
        {
            _currentAction = null;
            _currentActionIndex = -1;
        }
        
        /// <summary>
        /// Clears the selector's active action state when the node is exited.
        /// </summary>
        /// <param name="brain">The NPC brain context for this exit; not used by this node but provided by the caller.</param>
        protected override void OnExit(NPCBrainController brain)
        {
            _currentAction = null;
            _currentActionIndex = -1;
        }
        
        /// <summary>
        /// Resets the selector to its initial state, clearing any currently executing action and the inertia history.
        /// </summary>
        /// <remarks>
        /// If an action is currently running, that action's Reset method is invoked before clearing. After this call there will be no current action and the last-selected action index is cleared.
        /// <summary>
        /// Resets the selector to its initial state and clears inertia/history.
        /// </summary>
        /// <remarks>
        /// If an action is currently executing, that action's Reset method is invoked. The current action reference and indices are cleared; the last selected action index is reset to -1 to remove any inertia bias.
        /// </remarks>
        public override void Reset()
        {
            base.Reset();
            if (_currentAction != null)
            {
                _currentAction.Action.Reset();
            }
            _currentAction = null;
            _currentActionIndex = -1;
            _lastSelectedActionIndex = -1; // Full reset clears inertia history
            _lastLoggedActionName = null; // Reset logging state
            _lastLogTime = 0f;
        }

        /// <summary>
        /// Aborts the currently executing action (if any), clears selection state and inertia history, and forwards the abort to the base composite.
        /// </summary>
        /// <summary>
        /// Aborts the currently running action, clears the selector's current action and selection indices (including inertia history), and delegates the abort to the base node.
        /// </summary>
        /// <param name="brain">The NPC brain controller used to abort the current action and propagate the abort to the base class.</param>
        public override void Abort(NPCBrainController brain)
        {
            if (_currentAction != null)
            {
                _currentAction.Action.Abort(brain);
            }
            _currentAction = null;
            _currentActionIndex = -1;
            _lastSelectedActionIndex = -1; // Full abort clears inertia history
            _lastLoggedActionName = null; // Reset logging state
            _lastLogTime = 0f;
            base.Abort(brain);
        }
        
        /// <summary>Number of actions in this selector.</summary>
        public int ActionCount => _actions.Count;
        
        /// <summary>
        /// Gets the scores from the last selection (for debugging).
        /// </summary>
        /// <returns>List of scores for each action.</returns>
        public IReadOnlyList<float> GetLastScores()
        {
            _scoresList.Clear();
            for (int i = 0; i < _actions.Count && i < _scores.Length; i++)
            {
                _scoresList.Add(_scores[i]);
            }
            return _scoresList;
        }
        
        /// <summary>
        /// Gets the selection probabilities from the last selection (for debugging).
        /// </summary>
        /// <returns>List of probabilities for each action.</returns>
        public IReadOnlyList<float> GetLastProbabilities()
        {
            _probabilitiesList.Clear();
            for (int i = 0; i < _actions.Count && i < _probabilities.Length; i++)
            {
                _probabilitiesList.Add(_probabilities[i]);
            }
            return _probabilitiesList;
        }
        
        /// <summary>The currently executing action, or null if selecting.</summary>
        public UtilityAction CurrentAction => _currentAction;
        
        /// <summary>
        /// Finds an action by name.
        /// </summary>
        /// <param name="name">The name of the action to find.</param>
        /// <returns>The action, or null if not found.</returns>
        public UtilityAction GetAction(string name)
        {
            // Use for loop instead of foreach to avoid enumerator allocation
            for (int i = 0; i < _actions.Count; i++)
            {
                if (_actions[i].Name == name)
                {
                    return _actions[i];
                }
            }
            return null;
        }
        
        /// <summary>
        /// Removes an action by name.
        /// </summary>
        /// <param name="name">The name of the action to remove.</param>
        /// <returns>True if the action was found and removed.</returns>
        public bool RemoveAction(string name)
        {
            for (int i = 0; i < _actions.Count; i++)
            {
                if (_actions[i].Name == name)
                {
                    _actions.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }
        
        /// <summary>
        /// Removes an action by reference.
        /// </summary>
        /// <param name="action">The action to remove.</param>
        /// <returns>True if the action was found and removed.</returns>
        public bool RemoveAction(UtilityAction action)
        {
            return _actions.Remove(action);
        }
        
        private bool ShouldLogWarning()
        {
            return LogWarnings || NPCBrainDebug.IsEnabled(NPCBrainDebug.Category.Utility);
        }
    }
}