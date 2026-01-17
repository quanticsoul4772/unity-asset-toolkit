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
        
        [Tooltip("Default weight for follow leader behavior.")]
        [SerializeField] private float _followLeaderWeight = 1.5f;
        
        [Header("Wander Behavior")]
        [Tooltip("Radius of the wander circle.")]
        [SerializeField] private float _wanderRadius = 4f;
        
        [Tooltip("Distance of the wander circle from the agent.")]
        [SerializeField] private float _wanderDistance = 6f;
        
        [Tooltip("Maximum random displacement per frame for wander.")]
        [SerializeField] private float _wanderJitter = 1f;
        
        [Header("Obstacle Avoidance")]
        [Tooltip("How far ahead to look for obstacles.")]
        [SerializeField] private float _obstacleDetectionDistance = 5f;
        
        [Tooltip("Angle of the side whiskers in degrees.")]
        [SerializeField] private float _obstacleWhiskerAngle = 45f;
        
        [Tooltip("Number of rays to cast for obstacle detection.")]
        [SerializeField] private int _obstacleRayCount = 3;
        
        [Header("Arrive Behavior")]
        [Tooltip("Default slowing radius for arrive behavior.")]
        [SerializeField] private float _arriveSlowingRadius = 5f;
        
        [Tooltip("Default arrival radius for arrive behavior.")]
        [SerializeField] private float _arriveArrivalRadius = 0.5f;
        
        [Header("Formation Settings")]
        [Tooltip("Default spacing between agents in formations.")]
        [SerializeField] private float _formationSpacing = 2f;
        
        [Tooltip("How quickly agents move to their formation positions.")]
        [SerializeField] private float _formationMoveSpeed = 5f;
        
        [Tooltip("Distance at which an agent is considered in position.")]
        [SerializeField] private float _formationArrivalRadius = 0.5f;
        
        [Header("Follow Leader")]
        [Tooltip("Default follow distance for leader-follower behavior.")]
        [SerializeField] private float _defaultFollowDistance = 3f;
        
        [Tooltip("Slowing radius when following a leader.")]
        [SerializeField] private float _followSlowingRadius = 5f;
        
        [Header("Resource Gathering")]
        [Tooltip("Default carry capacity for agents.")]
        [SerializeField] private float _defaultCarryCapacity = 10f;
        
        [Tooltip("Default deposit radius when returning resources.")]
        [SerializeField] private float _depositRadius = 2f;
        
        [Tooltip("Time to wait before searching for new resources after depletion.")]
        [SerializeField] private float _resourceSearchDelay = 0.5f;
        
        [Header("Performance")]
        [Tooltip("Maximum number of agents to process per frame for neighbor updates.")]
        [SerializeField] private int _maxAgentsPerFrame = 50;
        
        [Tooltip("How often to update the spatial hash (in seconds). 0 = every frame.")]
        [SerializeField] private float _spatialHashUpdateInterval = 0f;
        
        [Header("Jobs/Burst (Requires Burst Package)")]
        [Tooltip("Enable parallel job processing for steering calculations. Requires Burst package.")]
        [SerializeField] private bool _useJobsSystem = true;
        
        [Tooltip("Minimum number of agents before using Jobs system. Below this, single-threaded is faster.")]
        [SerializeField] private int _minAgentsForJobs = 50;
        
        [Tooltip("Batch size for parallel jobs. Higher = less overhead, lower = better load balancing.")]
        [SerializeField] private int _jobsBatchSize = 64;
        
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
        public float FollowLeaderWeight => _followLeaderWeight;
        public float WanderRadius => _wanderRadius;
        public float WanderDistance => _wanderDistance;
        public float WanderJitter => _wanderJitter;
        public float ObstacleDetectionDistance => _obstacleDetectionDistance;
        public float ObstacleWhiskerAngle => _obstacleWhiskerAngle;
        public int ObstacleRayCount => _obstacleRayCount;
        public float ArriveSlowingRadius => _arriveSlowingRadius;
        public float ArriveArrivalRadius => _arriveArrivalRadius;
        public float FormationSpacing => _formationSpacing;
        public float FormationMoveSpeed => _formationMoveSpeed;
        public float FormationArrivalRadius => _formationArrivalRadius;
        public float DefaultFollowDistance => _defaultFollowDistance;
        public float FollowSlowingRadius => _followSlowingRadius;
        public float DefaultCarryCapacity => _defaultCarryCapacity;
        public float DepositRadius => _depositRadius;
        public float ResourceSearchDelay => _resourceSearchDelay;
        public int MaxAgentsPerFrame => _maxAgentsPerFrame;
        public float SpatialHashUpdateInterval => _spatialHashUpdateInterval;
        public bool UseJobsSystem => _useJobsSystem;
        public int MinAgentsForJobs => _minAgentsForJobs;
        public int JobsBatchSize => _jobsBatchSize;
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
        /// Shared constant for velocity/magnitude squared threshold checks.
        /// Used to determine if an agent is effectively stationary (velocity.sqrMagnitude less than this).
        /// Value: 0.001f (equivalent to ~0.032 units/second velocity).
        /// </summary>
        public const float DefaultVelocityThresholdSq = 0.001f;
        
        /// <summary>
        /// Threshold for determining if a surface normal is facing backward.
        /// Used in obstacle avoidance to decide when to steer perpendicular.
        /// Value: -0.5f (approximately 120 degrees from forward).
        /// </summary>
        public const float BackwardNormalThreshold = -0.5f;
        
        /// <summary>
        /// Threshold for deciding whether to continue gathering when resource depletes (90% capacity).
        /// If agent has gathered less than this fraction of capacity, look for new resources.
        /// </summary>
        public const float GatheringContinueThreshold = 0.9f;
        
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
