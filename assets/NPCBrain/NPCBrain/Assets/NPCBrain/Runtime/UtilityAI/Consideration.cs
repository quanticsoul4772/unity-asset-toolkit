using NPCBrain.UtilityAI.Curves;

namespace NPCBrain.UtilityAI
{
    /// <summary>
    /// Base class for factors that influence a <see cref="UtilityAction"/>'s score.
    /// Evaluates some aspect of game state and returns a 0-1 score.
    /// </summary>
    /// <remarks>
    /// <para>Create custom considerations by deriving from this class and overriding
    /// <see cref="Evaluate"/>. The raw evaluation is passed through the response curve
    /// to shape the final score.</para>
    /// <example>
    /// <code>
    /// public class HealthConsideration : Consideration
    /// {
    ///     public HealthConsideration() : base("Health") { }
    ///     
    ///     protected override float Evaluate(NPCBrainController brain)
    ///     {
    ///         return brain.Blackboard.Get("health", 100) / 100f;
    ///     }
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    public abstract class Consideration
    {
        /// <summary>Display name for debugging.</summary>
        public string Name { get; protected set; }
        
        /// <summary>Response curve that shapes the raw evaluation into the final score.</summary>
        public ResponseCurve Curve { get; set; }
        
        /// <summary>
        /// Creates a consideration with a custom response curve.
        /// </summary>
        /// <param name="name">Display name for debugging.</param>
        /// <param name="curve">Response curve to shape the score (defaults to linear).</param>
        protected Consideration(string name, ResponseCurve curve)
        {
            Name = name;
            Curve = curve ?? new LinearCurve();
        }
        
        /// <summary>
        /// Creates a consideration with the default linear response curve.
        /// </summary>
        /// <param name="name">Display name for debugging.</param>
        protected Consideration(string name)
        {
            Name = name;
            Curve = new LinearCurve();
        }
        
        /// <summary>
        /// Calculates the final score by evaluating game state and applying the response curve.
        /// </summary>
        /// <param name="brain">The NPC brain to evaluate against.</param>
        /// <returns>A score from 0 to 1.</returns>
        public float Score(NPCBrainController brain)
        {
            float rawScore = Evaluate(brain);
            float clampedScore = UnityEngine.Mathf.Clamp01(rawScore);
            return Curve.Evaluate(clampedScore);
        }
        
        /// <summary>
        /// Override this to evaluate game state and return a raw score (0-1).
        /// </summary>
        /// <param name="brain">The NPC brain to evaluate against.</param>
        /// <returns>Raw score before response curve is applied.</returns>
        protected abstract float Evaluate(NPCBrainController brain);
    }
}
