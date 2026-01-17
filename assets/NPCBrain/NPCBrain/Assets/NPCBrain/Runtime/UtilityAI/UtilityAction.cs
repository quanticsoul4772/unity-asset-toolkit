using System.Collections.Generic;
using NPCBrain.BehaviorTree;

namespace NPCBrain.UtilityAI
{
    public class UtilityAction
    {
        public string Name { get; set; }
        public BTNode Action { get; private set; }
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
            
            float modificationFactor = 1f - (1f / _considerations.Count);
            float makeUpValue = (1f - score) * modificationFactor;
            score += makeUpValue * score;
            
            return score;
        }
        
        public int ConsiderationCount => _considerations.Count;
        
        public IReadOnlyList<Consideration> Considerations => _considerations;
    }
}
