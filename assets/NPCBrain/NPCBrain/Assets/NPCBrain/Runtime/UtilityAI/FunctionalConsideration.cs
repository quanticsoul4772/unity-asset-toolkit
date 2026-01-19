using System;
using NPCBrain.UtilityAI.Curves;

namespace NPCBrain.UtilityAI
{
    /// <summary>
    /// A consideration that uses a delegate function to evaluate the score.
    /// Useful for inline, one-off considerations without creating a new class.
    /// </summary>
    public class FunctionalConsideration : Consideration
    {
        private readonly Func<NPCBrainController, float> _evaluator;
        
        /// <summary>
        /// Creates a functional consideration with the specified evaluator.
        /// </summary>
        /// <param name="name">Display name for debugging.</param>
        /// <param name="evaluator">Function that takes the brain and returns a score (0-1).</param>
        public FunctionalConsideration(string name, Func<NPCBrainController, float> evaluator) : base(name)
        {
            _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
        }
        
        /// <summary>
        /// Creates a functional consideration with a response curve.
        /// </summary>
        /// <param name="name">Display name for debugging.</param>
        /// <param name="evaluator">Function that takes the brain and returns a score (0-1).</param>
        /// <param name="curve">Response curve to apply.</param>
        public FunctionalConsideration(string name, Func<NPCBrainController, float> evaluator, ResponseCurve curve) 
            : base(name, curve)
        {
            _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
        }
        
        protected override float Evaluate(NPCBrainController brain)
        {
            return _evaluator(brain);
        }
    }
}
