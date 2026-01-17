using NPCBrain.UtilityAI.Curves;

namespace NPCBrain.UtilityAI
{
    public abstract class Consideration
    {
        public string Name { get; set; }
        public ResponseCurve Curve { get; set; }
        
        protected Consideration(string name, ResponseCurve curve)
        {
            Name = name;
            Curve = curve ?? new LinearCurve();
        }
        
        protected Consideration(string name) : this(name, new LinearCurve()) { }
        
        public float Score(NPCBrainController brain)
        {
            float rawValue = Evaluate(brain);
            float clampedValue = rawValue < 0f ? 0f : (rawValue > 1f ? 1f : rawValue);
            return Curve.Evaluate(clampedValue);
        }
        
        protected abstract float Evaluate(NPCBrainController brain);
    }
}
