using System.Collections.Generic;
using NPCBrain.BehaviorTree;

namespace NPCBrain.UtilityAI
{
    /// <summary>
    /// Represents an action that can be selected by <see cref="Composites.UtilitySelector"/>.
    /// Combines a behavior tree node with considerations that determine its utility score.
    /// </summary>
    /// <remarks>
    /// <para>The final score is calculated by multiplying all consideration scores together,
    /// then applying a compensation factor to prevent actions with more considerations
    /// from being unfairly penalized.</para>
    /// </remarks>
    public class UtilityAction
    {
        /// <summary>Display name for debugging and logging.</summary>
        public string Name { get; set; }
        
        /// <summary>The behavior tree to execute when this action is selected.</summary>
        public BTNode Action { get; set; }
        
        /// <summary>Starting score before considerations are applied.</summary>
        public float BaseScore { get; set; } = 1f;
        
        private readonly List<Consideration> _considerations;
        
        /// <summary>
        /// Creates a utility action with the specified behavior and considerations.
        /// </summary>
        /// <param name="name">Display name for the action.</param>
        /// <param name="action">The behavior tree to execute.</param>
        /// <param name="considerations">Factors that affect this action's score.</param>
        public UtilityAction(string name, BTNode action, params Consideration[] considerations)
        {
            Name = name;
            Action = action;
            _considerations = new List<Consideration>(considerations);
        }
        
        /// <summary>
        /// Creates a utility action with a custom base score.
        /// </summary>
        /// <param name="name">Display name for the action.</param>
        /// <param name="action">The behavior tree to execute.</param>
        /// <param name="baseScore">Starting score before considerations (default: 1.0).</param>
        /// <param name="considerations">Factors that affect this action's score.</param>
        public UtilityAction(string name, BTNode action, float baseScore, params Consideration[] considerations)
        {
            Name = name;
            Action = action;
            BaseScore = baseScore;
            _considerations = new List<Consideration>(considerations);
        }
        
        /// <summary>
        /// Adds a consideration at runtime.
        /// </summary>
        /// <param name="consideration">The consideration to add.</param>
        public void AddConsideration(Consideration consideration)
        {
            _considerations.Add(consideration);
        }
        
        /// <summary>
        /// Removes a consideration by name.
        /// </summary>
        /// <param name="name">The name of the consideration to remove.</param>
        /// <returns>True if found and removed.</returns>
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
        
        /// <summary>
        /// Removes a consideration by reference.
        /// </summary>
        /// <param name="consideration">The consideration to remove.</param>
        /// <returns>True if found and removed.</returns>
        public bool RemoveConsideration(Consideration consideration)
        {
            return _considerations.Remove(consideration);
        }
        
        /// <summary>
        /// Calculates the utility score for this action based on current game state.
        /// </summary>
        /// <param name="brain">The NPC brain to evaluate considerations against.</param>
        /// <returns>A score from 0 to 1, where higher means more desirable.</returns>
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
        
        /// <summary>Number of considerations affecting this action.</summary>
        public int ConsiderationCount => _considerations.Count;
        
        /// <summary>Read-only access to considerations (for debugging).</summary>
        public IReadOnlyList<Consideration> Considerations => _considerations;
    }
}
