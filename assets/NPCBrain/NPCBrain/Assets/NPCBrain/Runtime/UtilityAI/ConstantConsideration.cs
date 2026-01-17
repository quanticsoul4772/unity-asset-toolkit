using NPCBrain.UtilityAI.Curves;

namespace NPCBrain.UtilityAI
{
    public class ConstantConsideration : Consideration
    {
        private readonly float _value;
        
        public ConstantConsideration(string name, float value, ResponseCurve curve)
            : base(name, curve)
        {
            _value = value;
        }
        
        public ConstantConsideration(string name, float value)
            : this(name, value, new LinearCurve()) { }
        
        public ConstantConsideration(float value)
            : this("Constant", value, new LinearCurve()) { }
        
        protected override float Evaluate(NPCBrainController brain)
        {
            return _value;
        }
    }
}
