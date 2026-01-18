using UnityEngine;
using NPCBrain.Perception;
using NPCBrain.UtilityAI.Curves;

namespace NPCBrain.UtilityAI
{
    /// <summary>
    /// Consideration that scores based on the highest priority sound heard.
    /// Returns 0 if no sounds heard, scales up based on sound type priority.
    /// </summary>
    public class SoundConsideration : Consideration
    {
        private readonly SoundType _minType;
        private readonly SoundType _maxType;
        
        /// <summary>
        /// Creates a sound consideration that scores based on heard sound priority.
        /// </summary>
        /// <param name="name">Display name.</param>
        /// <param name="minType">Minimum sound type to consider (inclusive).</param>
        /// <param name="maxType">Maximum sound type for normalization.</param>
        /// <param name="curve">Response curve (defaults to linear).</param>
        public SoundConsideration(
            string name,
            SoundType minType = SoundType.Footstep,
            SoundType maxType = SoundType.Explosion,
            ResponseCurve curve = null)
            : base(name, curve)
        {
            _minType = minType;
            _maxType = maxType;
        }
        
        protected override float Evaluate(NPCBrainController brain)
        {
            var hearing = brain.Hearing;
            if (hearing == null || !hearing.HasHeardSounds)
            {
                return 0f;
            }
            
            var sound = hearing.HighestPrioritySound;
            if (sound == null || sound.Type < _minType)
            {
                return 0f;
            }
            
            // Normalize the sound type to 0-1 range
            float typeRange = (float)_maxType - (float)_minType;
            if (typeRange <= 0) return 1f;
            
            float normalizedType = ((float)sound.Type - (float)_minType) / typeRange;
            return Mathf.Clamp01(normalizedType);
        }
    }
    
    /// <summary>
    /// Consideration that returns 1 if a sound of at least the specified type was heard, 0 otherwise.
    /// </summary>
    public class HasHeardSoundConsideration : Consideration
    {
        private readonly SoundType _minType;
        
        /// <summary>
        /// Creates a consideration that checks if a sound of minimum type was heard.
        /// </summary>
        /// <param name="name">Display name.</param>
        /// <param name="minType">Minimum sound type to trigger (defaults to any sound).</param>
        public HasHeardSoundConsideration(string name, SoundType minType = SoundType.Ambient)
            : base(name)
        {
            _minType = minType;
        }
        
        protected override float Evaluate(NPCBrainController brain)
        {
            // Check blackboard for lastSoundType (set by HandleSoundHeard)
            if (!brain.Blackboard.Has("lastSoundType"))
            {
                return 0f;
            }
            
            var soundType = (SoundType)brain.Blackboard.Get<int>("lastSoundType");
            return soundType >= _minType ? 1f : 0f;
        }
    }
    
    /// <summary>
    /// Consideration that scores based on distance to the last heard sound.
    /// </summary>
    public class SoundDistanceConsideration : Consideration
    {
        private readonly float _maxDistance;
        private readonly float _maxDistanceSqr;
        private readonly bool _invertScore;
        
        /// <summary>
        /// Creates a sound distance consideration.
        /// </summary>
        /// <param name="name">Display name.</param>
        /// <param name="maxDistance">Distance at which score is 0 (if inverted) or 1 (if not).</param>
        /// <param name="invertScore">If true, closer sounds = higher score.</param>
        /// <param name="curve">Response curve.</param>
        public SoundDistanceConsideration(
            string name,
            float maxDistance = 30f,
            bool invertScore = true,
            ResponseCurve curve = null)
            : base(name, curve)
        {
            _maxDistance = maxDistance > 0 ? maxDistance : 30f;
            _maxDistanceSqr = _maxDistance * _maxDistance;
            _invertScore = invertScore;
        }
        
        protected override float Evaluate(NPCBrainController brain)
        {
            if (!brain.Blackboard.Has("investigatePosition"))
            {
                return 0f;
            }
            
            Vector3 soundPos = brain.Blackboard.Get<Vector3>("investigatePosition");
            // Use sqrMagnitude to avoid sqrt operation
            float distanceSqr = (soundPos - brain.transform.position).sqrMagnitude;
            float normalizedSqr = distanceSqr / _maxDistanceSqr;
            // sqrt only the normalized value (cheaper than full distance)
            float normalized = Mathf.Clamp01(Mathf.Sqrt(normalizedSqr));
            
            return _invertScore ? 1f - normalized : normalized;
        }
    }
}
