namespace NPCBrain.UtilityAI.Curves
{
    public abstract class ResponseCurve
    {
        public abstract float Evaluate(float input);
        
        protected static float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }
    }
}
