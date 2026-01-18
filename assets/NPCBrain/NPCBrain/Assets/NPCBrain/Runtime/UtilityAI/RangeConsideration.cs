using UnityEngine;
using NPCBrain.UtilityAI.Curves;

namespace NPCBrain.UtilityAI
{
    /// <summary>
    /// Consideration that scores highest when a value is within a specific range.
    /// Score is 1.0 inside the range, drops off outside.
    /// </summary>
    public class RangeConsideration : Consideration
    {
        private readonly System.Func<NPCBrainController, float> _valueGetter;
        private readonly float _minValue;
        private readonly float _maxValue;
        private readonly float _falloffDistance;
        
        /// <summary>
        /// Creates a range-based consideration.
        /// </summary>
        /// <param name="name">Display name.</param>
        /// <param name="valueGetter">Function to get current value.</param>
        /// <param name="minValue">Minimum value for full score.</param>
        /// <param name="maxValue">Maximum value for full score.</param>
        /// <param name="falloffDistance">Distance outside range where score drops to 0.</param>
        /// <param name="curve">Response curve (defaults to linear).</param>
        public RangeConsideration(
            string name,
            System.Func<NPCBrainController, float> valueGetter,
            float minValue,
            float maxValue,
            float falloffDistance = 1f,
            ResponseCurve curve = null)
            : base(name, curve)
        {
            _valueGetter = valueGetter;
            _minValue = minValue;
            _maxValue = maxValue;
            _falloffDistance = falloffDistance > 0 ? falloffDistance : 1f;
        }
        
        protected override float Evaluate(NPCBrainController brain)
        {
            float value = _valueGetter(brain);
            
            if (value >= _minValue && value <= _maxValue)
            {
                return 1f;
            }
            
            if (value < _minValue)
            {
                float distance = _minValue - value;
                return Mathf.Clamp01(1f - distance / _falloffDistance);
            }
            else
            {
                float distance = value - _maxValue;
                return Mathf.Clamp01(1f - distance / _falloffDistance);
            }
        }
    }
}
