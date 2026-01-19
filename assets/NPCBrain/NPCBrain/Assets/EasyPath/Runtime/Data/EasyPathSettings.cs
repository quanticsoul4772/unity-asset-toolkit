using UnityEngine;

namespace EasyPath
{
    /// <summary>
    /// Global settings for EasyPath pathfinding system.
    /// </summary>
    [CreateAssetMenu(fileName = "EasyPathSettings", menuName = "EasyPath/Settings")]
    public class EasyPathSettings : ScriptableObject
    {
        [Header("Grid Defaults")]
        [Tooltip("Default width of pathfinding grids")]
        [Range(5, 500)]
        public int defaultWidth = 20;
        
        [Tooltip("Default height of pathfinding grids")]
        [Range(5, 500)]
        public int defaultHeight = 20;
        
        [Tooltip("Default cell size in world units")]
        [Range(0.1f, 10f)]
        public float defaultCellSize = 1f;
        
        [Tooltip("Default layers that block pathfinding")]
        public LayerMask defaultObstacleLayers;
        
        [Header("Agent Defaults")]
        [Tooltip("Default movement speed for agents")]
        [Range(0.1f, 50f)]
        public float defaultSpeed = 5f;
        
        [Tooltip("Default stopping distance for agents")]
        [Range(0.01f, 5f)]
        public float defaultStoppingDistance = 0.1f;
        
        [Header("Performance")]
        [Tooltip("Maximum nodes to process per frame (for async pathfinding)")]
        [Range(10, 1000)]
        public int maxNodesPerFrame = 100;
        
        [Tooltip("Enable path caching for repeated queries")]
        public bool enablePathCaching = true;
        
        [Tooltip("Cache expiration time in seconds")]
        [Range(0.1f, 60f)]
        public float cacheExpirationTime = 5f;
        
        [Header("Debug Visualization")]
        public bool showDebugGizmos = true;
        public Color walkableColor = new Color(0f, 1f, 0f, 0.3f);
        public Color blockedColor = new Color(1f, 0f, 0f, 0.3f);
        public Color pathColor = Color.cyan;
        public Color agentColor = Color.blue;
    }
}
