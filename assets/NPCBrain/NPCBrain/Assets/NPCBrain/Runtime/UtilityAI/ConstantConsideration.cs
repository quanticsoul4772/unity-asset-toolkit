using NPCBrain.UtilityAI.Curves;

namespace NPCBrain.UtilityAI
{
    /// <summary>
    /// A consideration that always returns a constant value.
    /// Useful for base scores or testing.
    /// </summary>
    public class ConstantConsideration : Consideration
    {
        private readonly float _value;
        
        public ConstantConsideration(float value) : base("Constant")
        {
            _value = value;
        }
        
        public ConstantConsideration(string name, float value) : base(name)
        {
            _value = value;
        }
        
        public ConstantConsideration(string name, float value, ResponseCurve curve) : base(name, curve)
        {
            _value = value;
        }
        
        protected override float Evaluate(NPCBrainController brain)
        {
            return _value;
        }
    }
}
