using UnityEngine;

namespace NPCBrain.Perception
{
    /// <summary>
    /// Common interface for all perception sensors (sight, hearing, etc.).
    /// Enables generic handling of different sensor types.
    /// </summary>
    public interface ISensor
    {
        /// <summary>Whether the sensor is currently enabled.</summary>
        bool IsEnabled { get; set; }
        
        /// <summary>Range of the sensor in world units.</summary>
        float Range { get; }
        
        /// <summary>Whether the sensor has detected any targets.</summary>
        bool HasDetection { get; }
        
        /// <summary>Updates the sensor state. Called each tick.</summary>
        void UpdateSensor();
        
        /// <summary>Clears all current detections.</summary>
        void ClearDetections();
    }
    
    /// <summary>
    /// Interface for sensors that detect specific GameObjects (sight, proximity).
    /// </summary>
    public interface ITargetSensor : ISensor
    {
        /// <summary>Gets the currently detected targets.</summary>
        GameObject[] GetDetectedTargets();
        
        /// <summary>Gets the primary (most important) detected target.</summary>
        GameObject GetPrimaryTarget();
        
        /// <summary>Checks if a specific target is currently detected.</summary>
        bool IsTargetDetected(GameObject target);
    }
    
    /// <summary>
    /// Interface for sensors that detect sounds.
    /// </summary>
    public interface ISoundSensor : ISensor
    {
        /// <summary>Gets the sounds heard this tick.</summary>
        SoundEvent[] GetHeardSounds();
        
        /// <summary>Gets the most significant sound heard.</summary>
        SoundEvent GetPrimarySound();
        
        /// <summary>Minimum volume threshold for detection.</summary>
        float HearingThreshold { get; }
    }
}
