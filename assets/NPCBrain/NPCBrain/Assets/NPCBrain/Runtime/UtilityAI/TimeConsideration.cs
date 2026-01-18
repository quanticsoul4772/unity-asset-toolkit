using UnityEngine;
using NPCBrain.UtilityAI.Curves;

namespace NPCBrain.UtilityAI
{
    /// <summary>
    /// Consideration that scores based on time elapsed since a blackboard timestamp.
    /// Useful for cooldown-like behaviors.
    /// </summary>
    public class TimeConsideration : Consideration
    {
        private readonly string _timestampKey;
        private readonly float _maxTime;
        
        /// <summary>
        /// Creates a time-based consideration.
        /// </summary>
        /// <param name="name">Display name.</param>
        /// <param name="timestampKey">Blackboard key storing the last action time.</param>
        /// <param name="maxTime">Time at which score reaches 1.0.</param>
        /// <param name="curve">Response curve (defaults to linear).</param>
        public TimeConsideration(
            string name,
            string timestampKey,
            float maxTime,
            ResponseCurve curve = null)
            : base(name, curve)
        {
            _timestampKey = timestampKey;
            _maxTime = maxTime > 0 ? maxTime : 5f;
        }
        
        protected override float Evaluate(NPCBrainController brain)
        {
            float lastTime = brain.Blackboard.Get(_timestampKey, -_maxTime);
            float elapsed = Time.time - lastTime;
            return Mathf.Clamp01(elapsed / _maxTime);
        }
    }
}
