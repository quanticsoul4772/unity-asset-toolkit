// NPC archetype interfaces for type-safe NPC handling
namespace NPCBrain.Archetypes
{
    /// <summary>
    /// Common interface for all NPC archetypes.
    /// Defines the contract for NPC behavior state and display.
    /// </summary>
    public interface INPCArchetype
    {
        /// <summary>Current behavior state name for UI display.</summary>
        string CurrentState { get; }
        
        /// <summary>Whether this NPC is still active in the game.</summary>
        bool IsActive { get; }
    }
    
    /// <summary>
    /// Interface for NPCs with alert level (guards, cops).
    /// </summary>
    public interface IAlertableNPC : INPCArchetype
    {
        /// <summary>Current alert level (0-1).</summary>
        float AlertLevel { get; }
        
        /// <summary>Increases the alert level by the specified amount.</summary>
        void IncreaseAlert(float amount);
    }
    
    /// <summary>
    /// Interface for NPCs with energy/stamina (patrol, workers).
    /// </summary>
    public interface IEnergyNPC : INPCArchetype
    {
        /// <summary>Current energy level (0-1).</summary>
        float Energy { get; }
    }
}
