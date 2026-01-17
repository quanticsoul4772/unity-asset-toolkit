using NPCBrain.UtilityAI.Curves;

namespace NPCBrain.UtilityAI
{
    /// <summary>
    /// Base class for considerations that evaluate game state and return a 0-1 score.
    /// </summary>
    public abstract class Consideration
    {
        public string Name { get; protected set; }
        public ResponseCurve Curve { get; set; }
        
        protected Consideration(string name, ResponseCurve curve)
        {
            Name = name;
            Curve = curve ?? new LinearCurve();
        }
        
        protected Consideration(string name)
        {
            Name = name;
            Curve = new LinearCurve();
        }
        
        public float Score(NPCBrainController brain)
        {
            float rawScore = Evaluate(brain);
            float clampedScore = UnityEngine.Mathf.Clamp01(rawScore);
            return Curve.Evaluate(clampedScore);
        }
        
        protected abstract float Evaluate(NPCBrainController brain);
    }
}
