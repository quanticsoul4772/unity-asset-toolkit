using System;
using NPCBrain.UtilityAI.Curves;

namespace NPCBrain.UtilityAI
{
    /// <summary>
    /// A consideration that reads a value from the blackboard and converts it to a 0-1 score.
    /// </summary>
    /// <typeparam name="T">The type of value stored in the blackboard</typeparam>
    public class BlackboardConsideration<T> : Consideration
    {
        private readonly string _key;
        private readonly Func<T, float> _normalizer;
        private readonly T _defaultValue;
        
        /// <summary>
        /// Creates a BlackboardConsideration.
        /// </summary>
        /// <param name="name">Display name for this consideration</param>
        /// <param name="key">Blackboard key to read</param>
        /// <param name="normalizer">Function to convert the value to 0-1 range</param>
        /// <param name="defaultValue">Value to use if key is missing</param>
        public BlackboardConsideration(string name, string key, Func<T, float> normalizer, T defaultValue) 
            : base(name)
        {
            _key = key;
            _normalizer = normalizer;
            _defaultValue = defaultValue;
        }
        
        /// <summary>
        /// Creates a BlackboardConsideration with a response curve.
        /// </summary>
        /// <param name="name">Display name for this consideration</param>
        /// <param name="key">Blackboard key to read</param>
        /// <param name="normalizer">Function to convert the value to 0-1 range</param>
        /// <param name="defaultValue">Value to use if key is missing</param>
        /// <param name="curve">Response curve to apply</param>
        public BlackboardConsideration(string name, string key, Func<T, float> normalizer, T defaultValue, ResponseCurve curve) 
            : base(name, curve)
        {
            _key = key;
            _normalizer = normalizer;
            _defaultValue = defaultValue;
        }
        
        protected override float Evaluate(NPCBrainController brain)
        {
            if (brain?.Blackboard == null)
            {
                return _normalizer(_defaultValue);
            }
            
            T value = brain.Blackboard.Get(_key, _defaultValue);
            return _normalizer(value);
        }
        
        public string Key => _key;
    }
}
