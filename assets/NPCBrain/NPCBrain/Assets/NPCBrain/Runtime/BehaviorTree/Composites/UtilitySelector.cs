using System;
using System.Collections.Generic;
using NPCBrain.UtilityAI;

namespace NPCBrain.BehaviorTree.Composites
{
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
        
        public int ActionCount => _actions.Count;
        
        public IReadOnlyList<float> GetLastScores()
        {
            _scoresList.Clear();
            for (int i = 0; i < _actions.Count && i < _scores.Length; i++)
            {
                _scoresList.Add(_scores[i]);
            }
            return _scoresList;
        }
        
        public IReadOnlyList<float> GetLastProbabilities()
        {
            _probabilitiesList.Clear();
            for (int i = 0; i < _actions.Count && i < _probabilities.Length; i++)
            {
                _probabilitiesList.Add(_probabilities[i]);
            }
            return _probabilitiesList;
        }
        
        public UtilityAction CurrentAction => _currentAction;
        
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
        
        public bool RemoveAction(UtilityAction action)
        {
            return _actions.Remove(action);
        }
    }
}
