namespace NPCBrain.Settings
{
    /// <summary>
    /// Centralized configuration for pathfinding behavior and Criticality integration.
    /// Adjust these values to tune how NPCs navigate and respond to their internal state.
    /// </summary>
    /// <remarks>
    /// <para><b>How Criticality Affects Pathfinding:</b></para>
    /// <list type="bullet">
    ///   <item><description><b>Temperature</b> affects path recalculation frequency. Low temperature NPCs
    ///   constantly seek optimal paths; high temperature NPCs commit to suboptimal routes.</description></item>
    ///   <item><description><b>Inertia</b> affects path precision. High inertia NPCs follow paths precisely;
    ///   low inertia NPCs cut corners and take shortcuts.</description></item>
    /// </list>
    /// 
    /// <para><b>Example Configurations:</b></para>
    /// <para>For <b>precise tactical movement</b> (soldiers, guards):</para>
    /// <code>
    /// BaseRecalcInterval = 0.3f;      // Frequent updates
    /// BaseWaypointTolerance = 0.3f;   // Tight tolerance
    /// CornerCuttingInertiaThreshold = 0.1f;  // Rarely cut corners
    /// </code>
    /// 
    /// <para>For <b>natural organic movement</b> (civilians, animals):</para>
    /// <code>
    /// BaseRecalcInterval = 0.8f;      // Less frequent updates
    /// BaseWaypointTolerance = 0.8f;   // Loose tolerance
    /// CornerCuttingInertiaThreshold = 0.5f;  // Often cut corners
    /// </code>
    /// </remarks>
    public static class PathfindingSettings
    {
        #region Path Recalculation Settings
        
        /// <summary>
        /// Base interval (seconds) between path recalculations.
        /// Actual interval = BaseRecalcInterval * Temperature.
        /// </summary>
        /// <remarks>
        /// <para>Lower values = more responsive to target movement but higher CPU cost.</para>
        /// <para>With Temperature range [0.5, 2.0], actual intervals range from:</para>
        /// <list type="bullet">
        ///   <item><description>Low T (0.5): 0.25s recalc → always seeking optimal path</description></item>
        ///   <item><description>High T (2.0): 1.0s recalc → commits to suboptimal paths</description></item>
        /// </list>
        /// <para>Recommended range: 0.3 - 1.0</para>
        /// </remarks>
        public const float BaseRecalcInterval = 0.5f;
        
        /// <summary>
        /// Minimum distance the target must move before triggering a path recalculation.
        /// Prevents constant recalculation for stationary or slow-moving targets.
        /// </summary>
        /// <remarks>
        /// <para>Set higher for performance, lower for responsiveness.</para>
        /// <para>Recommended range: 1.0 - 5.0</para>
        /// </remarks>
        public const float TargetMovedThreshold = 2f;
        
        #endregion
        
        #region Waypoint Tolerance Settings
        
        /// <summary>
        /// Base tolerance (meters) for considering a waypoint "reached".
        /// Actual tolerance = BaseWaypointTolerance + (1 - Inertia) * InertiaToleranceMultiplier.
        /// </summary>
        /// <remarks>
        /// <para>Lower values = more precise path following but risk getting stuck.</para>
        /// <para>With Inertia range [0, 1] and default multiplier:</para>
        /// <list type="bullet">
        ///   <item><description>High Inertia (1.0): 0.5m tolerance → precise path following</description></item>
        ///   <item><description>Low Inertia (0.0): 2.0m tolerance → cuts corners, more direct</description></item>
        /// </list>
        /// <para>Recommended range: 0.3 - 1.0</para>
        /// </remarks>
        public const float BaseWaypointTolerance = 0.5f;
        
        /// <summary>
        /// How much inertia affects waypoint tolerance.
        /// tolerance = BaseWaypointTolerance + (1 - Inertia) * InertiaToleranceMultiplier.
        /// </summary>
        /// <remarks>
        /// <para>Higher values = bigger difference between high/low inertia NPCs.</para>
        /// <para>At default 1.5, tolerance ranges from 0.5m to 2.0m based on inertia.</para>
        /// <para>Recommended range: 0.5 - 2.5</para>
        /// </remarks>
        public const float InertiaToleranceMultiplier = 1.5f;
        
        #endregion
        
        #region Corner Cutting Settings
        
        /// <summary>
        /// Inertia threshold below which NPCs will attempt to skip waypoints (cut corners).
        /// Only NPCs with Inertia below this value will try to shortcut paths.
        /// </summary>
        /// <remarks>
        /// <para>Lower values = only very erratic NPCs cut corners.</para>
        /// <para>Higher values = more NPCs cut corners more often.</para>
        /// <para>Set to 0 to disable corner cutting entirely.</para>
        /// <para>Recommended range: 0.2 - 0.5</para>
        /// </remarks>
        public const float CornerCuttingInertiaThreshold = 0.3f;
        
        /// <summary>
        /// Number of waypoints to attempt skipping when corner cutting.
        /// NPC will try to skip to waypoint at (currentIndex + CornerCuttingSkipCount).
        /// </summary>
        /// <remarks>
        /// <para>Higher values = more aggressive shortcuts but higher risk of hitting walls.</para>
        /// <para>The skip is only performed if line-of-sight check passes.</para>
        /// <para>Recommended range: 1 - 4</para>
        /// </remarks>
        public const int CornerCuttingSkipCount = 2;
        
        /// <summary>
        /// Maximum raycast distance for corner cutting line-of-sight check.
        /// Prevents NPCs from trying to shortcut across large distances.
        /// </summary>
        /// <remarks>
        /// <para>Set lower in dense obstacle environments.</para>
        /// <para>Recommended range: 3.0 - 10.0</para>
        /// </remarks>
        public const float CornerCuttingMaxDistance = 5f;
        
        /// <summary>
        /// Height offset for corner cutting raycast origin.
        /// Raises the ray above ground level to avoid false positives from floor colliders.
        /// </summary>
        public const float CornerCuttingRaycastHeight = 0.5f;
        
        #endregion
        
        #region Stuck Detection Settings
        
        /// <summary>
        /// Interval (seconds) between stuck detection checks.
        /// </summary>
        /// <remarks>
        /// <para>Lower = faster detection but more overhead.</para>
        /// <para>Recommended range: 0.5 - 2.0</para>
        /// </remarks>
        public const float StuckCheckInterval = 1.0f;
        
        /// <summary>
        /// Distance threshold (meters) below which NPC is considered "not moving".
        /// If NPC moves less than this distance between checks, it may be stuck.
        /// </summary>
        /// <remarks>
        /// <para>Should be smaller than BaseWaypointTolerance.</para>
        /// <para>Recommended range: 0.1 - 0.5</para>
        /// </remarks>
        public const float StuckDistanceThreshold = 0.3f;
        
        /// <summary>
        /// Number of consecutive "not moving" checks before forcing path recalculation.
        /// Higher values = more forgiving, fewer false positives but slower recovery.
        /// </summary>
        /// <remarks>
        /// <para>Total time before unstuck = StuckCheckInterval * MaxStuckCount.</para>
        /// <para>At defaults: 1.0s * 3 = 3 seconds before path recalc.</para>
        /// <para>Recommended range: 2 - 5</para>
        /// </remarks>
        public const int MaxStuckCount = 3;
        
        /// <summary>
        /// Distance (meters) to push NPC horizontally when performing stuck recovery.
        /// This is the immediate push applied in the recovery direction.
        /// </summary>
        /// <remarks>
        /// <para>Should be large enough to escape collision but small enough to not overshoot.</para>
        /// <para>Recommended range: 0.5 - 1.5</para>
        /// </remarks>
        public const float StuckRecoveryPushDistance = 1.0f;
        
        /// <summary>
        /// Number of recovery attempts before forcing a complete path recalculation.
        /// Lower values = faster adaptation to blocked paths but more CPU cost.
        /// </summary>
        /// <remarks>
        /// <para>After this many failed recovery attempts, the path is discarded and recalculated.</para>
        /// <para>Recommended range: 2 - 4</para>
        /// </remarks>
        public const int RecoveryAttemptsBeforePathRecalc = 2;
        
        /// <summary>
        /// Fixed downward push (meters) applied during stuck recovery when NPC is not grounded.
        /// Uses a fixed value rather than frame-rate dependent gravity for consistency.
        /// </summary>
        /// <remarks>
        /// <para>Should be small to keep NPC close to ground without teleporting through floors.</para>
        /// <para>Recommended range: 0.05 - 0.2</para>
        /// </remarks>
        public const float StuckRecoveryDownwardPush = 0.1f;
        
        #endregion
        
        #region Path Following Settings
        
        /// <summary>
        /// Gravity applied when NPC is not grounded (m/s²).
        /// Standard Earth gravity is 9.81.
        /// </summary>
        public const float Gravity = 9.81f;
        
        /// <summary>
        /// Minimum movement distance before applying rotation.
        /// Prevents jittery rotation when nearly stationary.
        /// </summary>
        public const float MinMovementForRotation = 0.01f;
        
        #endregion
        
        #region Logging Settings
        
        /// <summary>
        /// Minimum change in waypoint count before logging a path recalculation.
        /// Reduces log spam by only logging significant path changes.
        /// </summary>
        /// <remarks>
        /// <para>Set to 1 for verbose logging, higher for less spam.</para>
        /// <para>Set to int.MaxValue to effectively disable path logging.</para>
        /// </remarks>
        public const int SignificantWaypointChange = 3;
        
        #endregion
    }
}
