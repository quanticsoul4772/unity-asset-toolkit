using UnityEngine;

namespace SwarmAI
{
    /// <summary>
    /// Global settings for the SwarmAI system.
    /// Create via Assets > Create > SwarmAI > Swarm Settings.
    /// </summary>
    [CreateAssetMenu(fileName = "SwarmSettings", menuName = "SwarmAI/Swarm Settings")]
    public class SwarmSettings : ScriptableObject
    {
        [Header("Spatial Partitioning")]
        [Tooltip("Cell size for spatial hash. Recommended: 2x the typical neighbor query radius.")]
        [SerializeField] private float _spatialHashCellSize = 10f;
        
        [Header("Agent Defaults")]
        [Tooltip("Default movement speed for agents.")]
        [SerializeField] private float _defaultSpeed = 5f;
        
        [Tooltip("Default rotation speed in degrees per second.")]
        [SerializeField] private float _defaultRotationSpeed = 360f;
        
        [Tooltip("Default maximum steering force.")]
        [SerializeField] private float _defaultMaxForce = 10f;
        
        [Tooltip("Default radius for neighbor detection.")]
        [SerializeField] private float _defaultNeighborRadius = 5f;
        
        [Tooltip("Default stopping distance for arrival.")]
        [SerializeField] private float _defaultStoppingDistance = 0.5f;
        
        [Header("Behavior Weights")]
        [Tooltip("Default weight for separation behavior.")]
        [SerializeField] private float _separationWeight = 1.5f;
        
        [Tooltip("Default weight for alignment behavior.")]
        [SerializeField] private float _alignmentWeight = 1.0f;
        
        [Tooltip("Default weight for cohesion behavior.")]
        [SerializeField] private float _cohesionWeight = 1.0f;
        
        [Header("Performance")]
        [Tooltip("Maximum number of agents to process per frame for neighbor updates.")]
        [SerializeField] private int _maxAgentsPerFrame = 50;
        
        [Tooltip("How often to update the spatial hash (in seconds). 0 = every frame.")]
        [SerializeField] private float _spatialHashUpdateInterval = 0f;
        
        [Header("State Machine")]
        [Tooltip("Minimum movement per second to not be considered stuck.")]
        [SerializeField] private float _stuckThreshold = 0.1f;
        
        [Tooltip("Time in seconds before an agent is considered stuck.")]
        [SerializeField] private float _stuckTimeLimit = 2f;
        
        [Tooltip("Default safe distance when fleeing from threats.")]
        [SerializeField] private float _defaultFleeDistance = 10f;
        
        [Tooltip("Speed multiplier when fleeing (1.0 = normal speed).")]
        [SerializeField] private float _fleeSpeedMultiplier = 1.5f;
        
        [Header("Debug")]
        [Tooltip("Enable debug visualization in Scene view.")]
        [SerializeField] private bool _enableDebugVisualization = true;
        
        [Tooltip("Show neighbor connections in debug view.")]
        [SerializeField] private bool _showNeighborConnections = false;
        
        [Tooltip("Show velocity vectors in debug view.")]
        [SerializeField] private bool _showVelocityVectors = true;
        
        // Public accessors
        public float SpatialHashCellSize => _spatialHashCellSize;
        public float DefaultSpeed => _defaultSpeed;
        public float DefaultRotationSpeed => _defaultRotationSpeed;
        public float DefaultMaxForce => _defaultMaxForce;
        public float DefaultNeighborRadius => _defaultNeighborRadius;
        public float DefaultStoppingDistance => _defaultStoppingDistance;
        public float SeparationWeight => _separationWeight;
        public float AlignmentWeight => _alignmentWeight;
        public float CohesionWeight => _cohesionWeight;
        public int MaxAgentsPerFrame => _maxAgentsPerFrame;
        public float SpatialHashUpdateInterval => _spatialHashUpdateInterval;
        public float StuckThreshold => _stuckThreshold;
        public float StuckTimeLimit => _stuckTimeLimit;
        public float DefaultFleeDistance => _defaultFleeDistance;
        public float FleeSpeedMultiplier => _fleeSpeedMultiplier;
        public bool EnableDebugVisualization => _enableDebugVisualization;
        public bool ShowNeighborConnections => _showNeighborConnections;
        public bool ShowVelocityVectors => _showVelocityVectors;
        
        /// <summary>
        /// Shared constant for position equality checks (squared distance threshold).
        /// Value: 0.0001f (equivalent to 0.01 units distance).
        /// </summary>
        public const float DefaultPositionEqualityThresholdSq = 0.0001f;
        
        /// <summary>
        /// Create default settings at runtime if no asset exists.
        /// </summary>
        public static SwarmSettings CreateDefault()
        {
            var settings = CreateInstance<SwarmSettings>();
            settings.name = "DefaultSwarmSettings";
            return settings;
        }
    }
}
