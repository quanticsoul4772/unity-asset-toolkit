using UnityEngine;
using NPCBrain.UtilityAI.Curves;

namespace NPCBrain.UtilityAI
{
    /// <summary>
    /// Consideration that scores based on distance to a target position.
    /// </summary>
    public class DistanceConsideration : Consideration
    {
        private readonly System.Func<NPCBrainController, Vector3> _targetGetter;
        private readonly float _maxDistance;
        private readonly float _maxDistanceSqr;
        private readonly bool _invertScore;
        
        /// <summary>
        /// Creates a distance consideration.
        /// </summary>
        /// <param name="name">Display name.</param>
        /// <param name="targetGetter">Function to get target position.</param>
        /// <param name="maxDistance">Distance at which score is 1 (or 0 if inverted).</param>
        /// <param name="invertScore">If true, closer = higher score. If false, farther = higher score.</param>
        /// <param name="curve">Response curve (defaults to linear).</param>
        public DistanceConsideration(
            string name,
            System.Func<NPCBrainController, Vector3> targetGetter,
            float maxDistance,
            bool invertScore = true,
            ResponseCurve curve = null)
            : base(name, curve)
        {
            _targetGetter = targetGetter;
            _maxDistance = maxDistance > 0 ? maxDistance : 10f;
            _maxDistanceSqr = _maxDistance * _maxDistance;
            _invertScore = invertScore;
        }
        
        protected override float Evaluate(NPCBrainController brain)
        {
            Vector3 target = _targetGetter(brain);
            float distanceSqr = (target - brain.transform.position).sqrMagnitude;
            // Early out if beyond max distance to avoid sqrt
            if (distanceSqr >= _maxDistanceSqr)
            {
                return _invertScore ? 0f : 1f;
            }
            float normalized = Mathf.Sqrt(distanceSqr) / _maxDistance;
            
            return _invertScore ? 1f - normalized : normalized;
        }
    }
}
