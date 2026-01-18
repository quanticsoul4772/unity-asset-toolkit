using System;
using UnityEngine;
using NPCBrain.Perception;

namespace NPCBrain.BehaviorTree.Conditions
{
    /// <summary>
    /// Condition node that checks if a sound was heard by the NPC's HearingSensor.
    /// </summary>
    /// <remarks>
    /// <para>Returns Success if a matching sound was heard, Failure otherwise.</para>
    /// <para>Requires a HearingSensor component on the NPC.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Investigate if any sound heard
    /// var investigateSound = new Sequence(
    ///     new CheckSoundHeard(),
    ///     new MoveTo(brain => brain.Hearing.HighestPrioritySound.Position)
    /// );
    /// 
    /// // React only to gunshots
    /// var reactToGunshot = new Sequence(
    ///     new CheckSoundHeard(SoundType.Gunshot),
    ///     new SetBlackboard("alertLevel", "high")
    /// );
    /// 
    /// // Custom predicate
    /// var reactToLoudSound = new CheckSoundHeard(sound => sound.EffectiveVolume > 0.7f);
    /// </code>
    /// </example>
    public class CheckSoundHeard : BTNode
    {
        private readonly SoundType? _minimumType;
        private readonly Func<SoundEvent, bool> _predicate;
        private readonly Func<NPCBrainController, GameObject> _getSource;
        
        /// <summary>
        /// Creates a CheckSoundHeard condition that succeeds if ANY sound was heard.
        /// </summary>
        public CheckSoundHeard()
        {
            _minimumType = null;
            _predicate = null;
            _getSource = null;
            Name = "CheckSoundHeard(Any)";
        }
        
        /// <summary>
        /// Creates a CheckSoundHeard condition that checks for a minimum sound type.
        /// </summary>
        /// <param name="minimumType">Minimum sound type priority to match.</param>
        public CheckSoundHeard(SoundType minimumType)
        {
            _minimumType = minimumType;
            _predicate = null;
            _getSource = null;
            Name = $"CheckSoundHeard({minimumType}+)";
        }
        
        /// <summary>
        /// Creates a CheckSoundHeard condition with a custom predicate.
        /// </summary>
        /// <param name="predicate">Function that returns true for matching sounds.</param>
        public CheckSoundHeard(Func<SoundEvent, bool> predicate)
        {
            _minimumType = null;
            _predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
            _getSource = null;
            Name = "CheckSoundHeard(Custom)";
        }
        
        /// <summary>
        /// Creates a CheckSoundHeard condition that checks for sounds from a specific source.
        /// </summary>
        /// <param name="getSource">Function to get the source GameObject to check for.</param>
        public CheckSoundHeard(Func<NPCBrainController, GameObject> getSource)
        {
            _minimumType = null;
            _predicate = null;
            _getSource = getSource ?? throw new ArgumentNullException(nameof(getSource));
            Name = "CheckSoundHeard(Source)";
        }
        
        protected override NodeStatus Tick(NPCBrainController brain)
        {
            if (brain == null)
            {
                return NodeStatus.Failure;
            }
            
            HearingSensor sensor = brain.Hearing;
            if (sensor == null)
            {
                sensor = brain.GetComponent<HearingSensor>();
            }
            
            if (sensor == null)
            {
                Debug.LogWarning($"[CheckSoundHeard] No HearingSensor found on {brain.name}");
                return NodeStatus.Failure;
            }
            
            // No sounds heard at all
            if (!sensor.HasHeardSounds)
            {
                return NodeStatus.Failure;
            }
            
            // Check for specific source
            if (_getSource != null)
            {
                GameObject source = _getSource(brain);
                if (source == null)
                {
                    return NodeStatus.Failure;
                }
                
                return sensor.HeardSoundFromSource(source) ? NodeStatus.Success : NodeStatus.Failure;
            }
            
            // Check with custom predicate
            if (_predicate != null)
            {
                foreach (var sound in sensor.HeardSounds)
                {
                    if (_predicate(sound))
                    {
                        return NodeStatus.Success;
                    }
                }
                return NodeStatus.Failure;
            }
            
            // Check for minimum sound type
            if (_minimumType.HasValue)
            {
                foreach (var sound in sensor.HeardSounds)
                {
                    if (sound.Type >= _minimumType.Value)
                    {
                        return NodeStatus.Success;
                    }
                }
                return NodeStatus.Failure;
            }
            
            // Default: any sound heard = success
            return NodeStatus.Success;
        }
    }
}
