using System.Collections.Generic;
using NPCBrain.BehaviorTree;

namespace NPCBrain.UtilityAI
{
    public class UtilityAction
    {
        public string Name { get; set; }
        public BTNode Action { get; set; }
        public float BaseScore { get; set; } = 1f;
        
        private readonly List<Consideration> _considerations;
        
        public UtilityAction(string name, BTNode action, params Consideration[] considerations)
        {
            Name = name;
            Action = action;
            _considerations = new List<Consideration>(considerations);
        }
        
        public UtilityAction(string name, BTNode action, float baseScore, params Consideration[] considerations)
        {
            Name = name;
            Action = action;
            BaseScore = baseScore;
            _considerations = new List<Consideration>(considerations);
        }
        
        public void AddConsideration(Consideration consideration)
        {
            _considerations.Add(consideration);
        }
        
        public bool RemoveConsideration(string name)
        {
            for (int i = 0; i < _considerations.Count; i++)
            {
                if (_considerations[i].Name == name)
                {
                    _considerations.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }
        
        public bool RemoveConsideration(Consideration consideration)
        {
            return _considerations.Remove(consideration);
        }
        
        public float Score(NPCBrainController brain)
        {
            if (_considerations.Count == 0)
            {
                return BaseScore;
            }
            
            float score = BaseScore;
            
            foreach (var consideration in _considerations)
            {
                float considerationScore = consideration.Score(brain);
                score *= considerationScore;
                
                if (score <= 0f)
                {
                    return 0f;
                }
            }
            
            // Compensation factor (Dave Mark's "make-up value" from GDC Utility AI talks):
            // Multiplicative scoring unfairly penalizes actions with more considerations.
            // E.g., 3 considerations at 0.8 each = 0.512, but 2 at 0.8 = 0.64.
            // This formula adds back a portion of the "lost" score proportional to
            // the number of considerations, making scores more comparable across actions.
            // Formula: finalScore = score + (1 - score) * (1 - 1/n) * score
            float modificationFactor = 1f - (1f / _considerations.Count);
            float makeUpValue = (1f - score) * modificationFactor;
            score += makeUpValue * score;
            
            return score;
        }
        
        public int ConsiderationCount => _considerations.Count;
        
        public IReadOnlyList<Consideration> Considerations => _considerations;
    }
}
