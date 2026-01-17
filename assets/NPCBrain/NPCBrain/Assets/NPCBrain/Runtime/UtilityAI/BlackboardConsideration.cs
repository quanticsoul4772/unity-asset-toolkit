using System;
using NPCBrain.UtilityAI.Curves;

namespace NPCBrain.UtilityAI
{
    public class BlackboardConsideration<T> : Consideration
    {
        private readonly string _key;
        private readonly Func<T, float> _normalizer;
        private readonly T _defaultValue;
        
        public BlackboardConsideration(string name, string key, Func<T, float> normalizer, T defaultValue, ResponseCurve curve)
            : base(name, curve)
        {
            _key = key;
            _normalizer = normalizer;
            _defaultValue = defaultValue;
        }
        
        public BlackboardConsideration(string name, string key, Func<T, float> normalizer, T defaultValue)
            : this(name, key, normalizer, defaultValue, new LinearCurve()) { }
        
        protected override float Evaluate(NPCBrainController brain)
        {
            T value = brain.Blackboard.Get(_key, _defaultValue);
            return _normalizer(value);
        }
    }
}
