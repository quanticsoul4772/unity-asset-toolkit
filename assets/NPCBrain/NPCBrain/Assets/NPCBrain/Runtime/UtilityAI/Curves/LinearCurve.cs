namespace NPCBrain.UtilityAI.Curves
{
    public class LinearCurve : ResponseCurve
    {
        private readonly float _slope;
        private readonly float _offset;
        
        public LinearCurve(float slope, float offset)
        {
            _slope = slope;
            _offset = offset;
        }
        
        public LinearCurve() : this(1f, 0f) { }
        
        public override float Evaluate(float input)
        {
            float result = _slope * input + _offset;
            return Clamp01(result);
        }
    }
}
