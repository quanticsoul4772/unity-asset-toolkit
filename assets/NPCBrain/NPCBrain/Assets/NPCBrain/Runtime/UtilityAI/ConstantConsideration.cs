using NPCBrain.UtilityAI.Curves;

namespace NPCBrain.UtilityAI
{
    /// <summary>
    /// A consideration that always returns a constant score.
    /// Useful for setting a baseline utility or for testing.
    /// </summary>
    public class ConstantConsideration : Consideration
    {
        private readonly float _value;
        
        /// <summary>
        /// Creates a constant consideration with the specified value.
        /// </summary>
        /// <param name="value">The constant score to return (0-1).</param>
        public ConstantConsideration(float value) : base("Constant")
        {
            _value = UnityEngine.Mathf.Clamp01(value);
        }
        
        /// <summary>
        /// Creates a constant consideration with a custom name.
        /// </summary>
        /// <param name="name">Display name.</param>
        /// <param name="value">The constant score to return (0-1).</param>
        public ConstantConsideration(string name, float value) : base(name)
        {
            _value = UnityEngine.Mathf.Clamp01(value);
        }
        
        /// <summary>
        /// Creates a constant consideration with a response curve.
        /// </summary>
        /// <param name="name">Display name.</param>
        /// <param name="value">The constant score to return (0-1).</param>
        /// <param name="curve">Response curve to apply.</param>
        public ConstantConsideration(string name, float value, ResponseCurve curve) : base(name, curve)
        {
            _value = UnityEngine.Mathf.Clamp01(value);
        }
        
        protected override float Evaluate(NPCBrainController brain)
        {
            return _value;
        }
    }
}
