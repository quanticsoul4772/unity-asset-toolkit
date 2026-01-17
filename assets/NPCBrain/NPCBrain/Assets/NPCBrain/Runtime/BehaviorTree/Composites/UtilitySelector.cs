using System;
using System.Collections.Generic;
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
        private readonly List<UtilityAction> _actions;
        private readonly List<float> _scoresList;
        private readonly List<float> _probabilitiesList;
        private UtilityAction _currentAction;
        private int _currentActionIndex = -1;
        private float[] _scores;
        private float[] _probabilities;
        private readonly Random _random;
        
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
            _random = new Random();
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
            _random = new Random(seed);
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
        
        protected override NodeStatus Tick(NPCBrainController brain)
        {
            if (_actions.Count == 0)
            {
                return NodeStatus.Failure;
            }
            
            if (_currentAction == null)
            {
                _currentAction = SelectAction(brain);
                if (_currentAction == null)
                {
                    return NodeStatus.Failure;
                }
            }
            
            NodeStatus status = _currentAction.Action.Execute(brain);
            
            if (status != NodeStatus.Running)
            {
                brain.Criticality?.RecordAction(_currentActionIndex);
                _currentAction = null;
                _currentActionIndex = -1;
            }
            
            return status;
        }
        
        private UtilityAction SelectAction(NPCBrainController brain)
        {
            if (brain == null)
            {
                return null;
            }
            
            float temperature = brain.Criticality?.Temperature ?? 1f;
            
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
                _probabilities[i] = (float)Math.Exp(scaledScore);
                sumExp += _probabilities[i];
            }
            
            if (sumExp <= 0f)
            {
                return null;
            }
            
            for (int i = 0; i < _actions.Count; i++)
            {
                _probabilities[i] /= sumExp;
            }
            
            float randomValue = (float)_random.NextDouble();
            float cumulative = 0f;
            
            for (int i = 0; i < _actions.Count; i++)
            {
                cumulative += _probabilities[i];
                if (randomValue <= cumulative)
                {
                    _currentActionIndex = i;
                    return _actions[i];
                }
            }
            
            _currentActionIndex = _actions.Count - 1;
            return _actions[_actions.Count - 1];
        }
        
        protected override void OnEnter(NPCBrainController brain)
        {
            _currentAction = null;
            _currentActionIndex = -1;
        }
        
        protected override void OnExit(NPCBrainController brain)
        {
            _currentAction = null;
            _currentActionIndex = -1;
        }
        
        public override void Reset()
        {
            base.Reset();
            if (_currentAction != null)
            {
                _currentAction.Action.Reset();
            }
            _currentAction = null;
            _currentActionIndex = -1;
        }
        
        public override void Abort(NPCBrainController brain)
        {
            if (_currentAction != null)
            {
                _currentAction.Action.Abort(brain);
            }
            _currentAction = null;
            _currentActionIndex = -1;
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
            foreach (var action in _actions)
            {
                if (action.Name == name)
                {
                    return action;
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
    }
}
