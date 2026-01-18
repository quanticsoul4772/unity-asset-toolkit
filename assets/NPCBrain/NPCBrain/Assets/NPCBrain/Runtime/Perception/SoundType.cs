namespace NPCBrain.Perception
{
    /// <summary>
    /// Categories of sounds, ordered by priority (higher value = higher priority).
    /// </summary>
    public enum SoundType
    {
        /// <summary>Background noise, usually ignored.</summary>
        Ambient = 0,
        
        /// <summary>Movement sounds like footsteps.</summary>
        Footstep = 1,
        
        /// <summary>Speech, grunts, breathing.</summary>
        Voice = 2,
        
        /// <summary>Doors, objects falling, impacts.</summary>
        Impact = 3,
        
        /// <summary>Alert sounds, alarms.</summary>
        Alarm = 4,
        
        /// <summary>Weapons fire.</summary>
        Gunshot = 5,
        
        /// <summary>Explosions - highest priority.</summary>
        Explosion = 6
    }
}
