using UnityEngine;

namespace SwarmAI
{
    /// <summary>
    /// Steering behavior that steers around obstacles using raycasts.
    /// Casts multiple rays in front of the agent to detect obstacles.
    /// </summary>
    public class ObstacleAvoidanceBehavior : BehaviorBase
    {
        private float _detectionDistance;
        private float _whiskerAngle;
        private LayerMask _obstacleLayers;
        private int _rayCount;
        
        /// <inheritdoc/>
        public override string Name => "Obstacle Avoidance";
        
        /// <summary>
        /// How far ahead to look for obstacles.
        /// </summary>
        public float DetectionDistance
        {
            get => _detectionDistance;
            set => _detectionDistance = Mathf.Max(0.1f, value);
        }
        
        /// <summary>
        /// Angle of the side whiskers in degrees.
        /// </summary>
        public float WhiskerAngle
        {
            get => _whiskerAngle;
            set => _whiskerAngle = Mathf.Clamp(value, 0f, 90f);
        }
        
        /// <summary>
        /// Layer mask for obstacle detection.
        /// </summary>
        public LayerMask ObstacleLayers
        {
            get => _obstacleLayers;
            set => _obstacleLayers = value;
        }
        
        /// <summary>
        /// Number of rays to cast (3, 5, or 7 recommended).
        /// </summary>
        public int RayCount
        {
            get => _rayCount;
            set => _rayCount = Mathf.Clamp(value, 1, 9);
        }
        
        /// <summary>
        /// Create an obstacle avoidance behavior with default settings.
        /// </summary>
        public ObstacleAvoidanceBehavior()
        {
            _detectionDistance = 5f;
            _whiskerAngle = 45f;
            _obstacleLayers = Physics.DefaultRaycastLayers;
            _rayCount = 3;
        }
        
        /// <summary>
        /// Create an obstacle avoidance behavior with custom settings.
        /// </summary>
        /// <param name="detectionDistance">How far ahead to look for obstacles.</param>
        /// <param name="obstacleLayers">Layer mask for obstacle detection.</param>
        /// <param name="whiskerAngle">Angle of the side whiskers in degrees.</param>
        /// <param name="rayCount">Number of rays to cast.</param>
        public ObstacleAvoidanceBehavior(float detectionDistance, LayerMask obstacleLayers, float whiskerAngle = 45f, int rayCount = 3)
        {
            _detectionDistance = Mathf.Max(0.1f, detectionDistance);
            _obstacleLayers = obstacleLayers;
            _whiskerAngle = Mathf.Clamp(whiskerAngle, 0f, 90f);
            _rayCount = Mathf.Clamp(rayCount, 1, 9);
        }
        
        /// <inheritdoc/>
        public override Vector3 CalculateForce(SwarmAgent agent)
        {
            if (agent == null) return Vector3.zero;
            
            // Determine forward direction
            Vector3 forward = agent.Velocity.sqrMagnitude > SwarmSettings.DefaultVelocityThresholdSq 
                ? agent.Velocity.normalized 
                : agent.Forward;
            
            // Scale detection distance by speed
            float dynamicDetection = _detectionDistance * (1f + agent.Speed / agent.MaxSpeed);
            
            Vector3 avoidanceForce = Vector3.zero;
            float closestDistance = float.MaxValue;
            RaycastHit closestHit = default;
            
            // Cast rays in a fan pattern
            for (int i = 0; i < _rayCount; i++)
            {
                // Calculate ray direction
                float angle;
                if (_rayCount == 1)
                {
                    angle = 0f;
                }
                else
                {
                    // Distribute rays from -whiskerAngle to +whiskerAngle
                    float t = (float)i / (_rayCount - 1);
                    angle = Mathf.Lerp(-_whiskerAngle, _whiskerAngle, t);
                }
                
                Vector3 rayDir = Quaternion.Euler(0, angle, 0) * forward;
                
                // Cast the ray
                if (Physics.Raycast(agent.Position, rayDir, out RaycastHit hit, dynamicDetection, _obstacleLayers))
                {
                    if (hit.distance < closestDistance)
                    {
                        closestDistance = hit.distance;
                        closestHit = hit;
                    }
                }
            }
            
            // If we hit something, calculate avoidance force
            if (closestDistance < float.MaxValue)
            {
                // Calculate how much to steer (more urgent when closer)
                float urgency = 1f - (closestDistance / dynamicDetection);
                
                // Steer away from the obstacle using the surface normal
                Vector3 avoidDir = closestHit.normal;
                avoidDir.y = 0; // Keep on horizontal plane
                
                // If normal is pointing mostly backward, steer perpendicular instead
                if (Vector3.Dot(avoidDir, forward) < SwarmSettings.BackwardNormalThreshold)
                {
                    // Choose perpendicular direction based on which side is clearer
                    Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
                    float leftClear = CheckClearance(agent.Position, -right, dynamicDetection);
                    float rightClear = CheckClearance(agent.Position, right, dynamicDetection);
                    avoidDir = rightClear > leftClear ? right : -right;
                }
                
                if (avoidDir.sqrMagnitude > SwarmSettings.DefaultVelocityThresholdSq)
                {
                    avoidanceForce = avoidDir.normalized * agent.MaxForce * urgency;
                }
            }
            
            return avoidanceForce;
        }
        
        /// <summary>
        /// Check how much clearance exists in a direction.
        /// </summary>
        private float CheckClearance(Vector3 origin, Vector3 direction, float maxDistance)
        {
            if (Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance, _obstacleLayers))
            {
                return hit.distance;
            }
            return maxDistance;
        }
    }
}
