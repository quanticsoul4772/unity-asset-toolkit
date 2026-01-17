namespace NPCBrain.UtilityAI.Curves
{
    public class StepCurve : ResponseCurve
    {
        private readonly float _threshold;
        private readonly float _lowValue;
        private readonly float _highValue;
        
        public StepCurve(float threshold, float lowValue, float highValue)
        {
            _threshold = threshold;
            _lowValue = lowValue;
            _highValue = highValue;
        }
        
        public StepCurve(float threshold) : this(threshold, 0f, 1f) { }
        
        public StepCurve() : this(0.5f, 0f, 1f) { }
        
        public override float Evaluate(float input)
        {
            return input >= _threshold ? _highValue : _lowValue;
        }
    }
}
