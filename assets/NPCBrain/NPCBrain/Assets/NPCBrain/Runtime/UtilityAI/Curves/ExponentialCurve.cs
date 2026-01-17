using System;

namespace NPCBrain.UtilityAI.Curves
{
    public class ExponentialCurve : ResponseCurve
    {
        private readonly float _exponent;
        
        public ExponentialCurve(float exponent)
        {
            _exponent = exponent;
        }
        
        public ExponentialCurve() : this(2f) { }
        
        public override float Evaluate(float input)
        {
            float clampedInput = Clamp01(input);
            float result = (float)Math.Pow(clampedInput, _exponent);
            return Clamp01(result);
        }
    }
}
