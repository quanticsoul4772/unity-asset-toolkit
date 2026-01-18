using System.Collections.Generic;
using UnityEngine;

namespace NPCBrain.Perception
{
    /// <summary>
    /// Vision cone sensor for detecting targets within field of view.
    /// Performs raycasts to check line of sight and maintains a list of visible targets.
    /// </summary>
    /// <remarks>
    /// <para>Attach this component alongside an NPCBrainController to enable sight perception.</para>
    /// <para>The sensor automatically updates each tick and fires events when targets are acquired/lost.</para>
    /// </remarks>
    public class SightSensor : MonoBehaviour
    {
        [Header("Vision Settings")]
        [Tooltip("Maximum distance the NPC can see")]
        [SerializeField] private float _viewDistance = 20f;
        
        [Tooltip("Field of view angle in degrees")]
        [SerializeField] private float _viewAngle = 120f;
        
        [Tooltip("Height offset for raycast origin (eye level)")]
        [SerializeField] private float _eyeHeight = 1.5f;
        
        [Tooltip("Layers that block line of sight")]
        [SerializeField] private LayerMask _obstacleMask = ~0;
        
        [Tooltip("Layers containing potential targets")]
        [SerializeField] private LayerMask _targetMask = ~0;
        
        [Tooltip("Tag to filter targets (empty = all)")]
        [SerializeField] private string _targetTag = "Player";
        
        [Header("Performance")]
        [Tooltip("Maximum targets to track")]
        [SerializeField] private int _maxTargets = 10;
        
        [Tooltip("Maximum raycasts per tick")]
        [SerializeField] private int _maxRaycastsPerTick = 3;
        
        [Header("Debug")]
        [Tooltip("Enable debug logging for this specific sensor (also requires NPCBrainDebug.Enabled)")]
        [SerializeField] private bool _debugLogging = false;
        [Tooltip("Force debug logging even if global debug is disabled")]
        [SerializeField] private bool _forceDebugLogging = false;
        [SerializeField] private bool _drawGizmos = true;
        [SerializeField] private Color _gizmoColorClear = new Color(0.3f, 1f, 0.3f, 0.3f);
        [SerializeField] private Color _gizmoColorAlert = new Color(1f, 0.3f, 0.3f, 0.3f);
        
        private readonly List<GameObject> _visibleTargets = new List<GameObject>();
        private readonly List<GameObject> _previousTargets = new List<GameObject>();
        private readonly Collider[] _overlapResults = new Collider[20];
        private bool _tagValidityChecked;
        private bool _targetTagIsValid;
        
        /// <summary>Maximum view distance in units.</summary>
        public float ViewDistance => _viewDistance;
        
        /// <summary>Field of view angle in degrees.</summary>
        public float ViewAngle => _viewAngle;
        
        /// <summary>List of currently visible target GameObjects.</summary>
        public IReadOnlyList<GameObject> VisibleTargets => _visibleTargets;
        
        /// <summary>True if any targets are currently visible.</summary>
        public bool HasVisibleTargets => _visibleTargets.Count > 0;
        
        /// <summary>The closest visible target, or null if none.</summary>
        public GameObject ClosestTarget { get; private set; }
        
        /// <summary>
        /// Updates the sensor, detecting visible targets.
        /// Called automatically by NPCBrainController each tick.
        /// </summary>
        /// <param name="brain">The brain controller this sensor belongs to.</param>
        public void Tick(NPCBrainController brain)
        {
            // Store previous targets for comparison
            _previousTargets.Clear();
            _previousTargets.AddRange(_visibleTargets);
            _visibleTargets.Clear();
            ClosestTarget = null;
            
            // Find potential targets in range
            Vector3 eyePosition = transform.position + Vector3.up * _eyeHeight;
            int count = Physics.OverlapSphereNonAlloc(eyePosition, _viewDistance, _overlapResults, _targetMask);
            
            if (ShouldLog())
            {
                NPCBrainDebug.Log(NPCBrainDebug.Category.Perception, 
                    $"OverlapSphere at {eyePosition} found {count} colliders within {_viewDistance}m (targetMask: {_targetMask.value})", this);
                for (int j = 0; j < count; j++)
                {
                    var c = _overlapResults[j];
                    if (c != null && c.gameObject != gameObject)
                    {
                        NPCBrainDebug.Log(NPCBrainDebug.Category.Perception, 
                            $"  - Found: {c.name} (tag: {c.tag}, layer: {c.gameObject.layer})", this);
                    }
                }
            }
            
            float closestDistance = float.MaxValue;
            int raycastCount = 0;
            
            for (int i = 0; i < count && _visibleTargets.Count < _maxTargets; i++)
            {
                var collider = _overlapResults[i];
                if (collider == null || collider.gameObject == gameObject) continue;
                
                // Filter by tag if specified
                if (!string.IsNullOrEmpty(_targetTag))
                {
                    // Use cached tag validity to avoid repeated exception handling
                    if (!_tagValidityChecked)
                    {
                        _tagValidityChecked = true;
                        _targetTagIsValid = IsTagValid(_targetTag);
                    }
                    
                    if (!_targetTagIsValid)
                    {
                        continue; // Tag is invalid, skip all targets
                    }
                    
                    if (!collider.CompareTag(_targetTag))
                    {
                        if (ShouldLog())
                        {
                            NPCBrainDebug.Log(NPCBrainDebug.Category.Perception, 
                                $"{collider.name} failed tag filter (has '{collider.tag}', need '{_targetTag}')", this);
                        }
                        continue;
                    }
                }
                
                Vector3 targetPosition = collider.transform.position;
                Vector3 directionToTarget = (targetPosition - eyePosition).normalized;
                float distanceToTarget = Vector3.Distance(eyePosition, targetPosition);
                
                // Check if within view angle
                float angleToTarget = Vector3.Angle(transform.forward, directionToTarget);
                if (angleToTarget > _viewAngle * 0.5f)
                {
                    if (ShouldLog())
                    {
                        NPCBrainDebug.Log(NPCBrainDebug.Category.Perception, 
                            $"{collider.name} outside view angle ({angleToTarget:F1}° > {_viewAngle * 0.5f:F1}°)", this);
                    }
                    continue;
                }
                
                // Check line of sight with raycast (limited per tick)
                if (raycastCount < _maxRaycastsPerTick)
                {
                    raycastCount++;
                    
                    if (Physics.Raycast(eyePosition, directionToTarget, out RaycastHit hit, distanceToTarget, _obstacleMask))
                    {
                        // Something blocks the view - check if it's the target
                        if (hit.collider.gameObject != collider.gameObject)
                        {
                            if (ShouldLog())
                            {
                                NPCBrainDebug.Log(NPCBrainDebug.Category.Perception, 
                                    $"Raycast to {collider.name} blocked by {hit.collider.name}", this);
                            }
                            continue;
                        }
                    }
                    
                    if (ShouldLog())
                    {
                        NPCBrainDebug.Log(NPCBrainDebug.Category.Perception, 
                            $"<color=green>TARGET VISIBLE: {collider.name}</color>", this);
                    }
                    
                    // Target is visible!
                    _visibleTargets.Add(collider.gameObject);
                    
                    if (distanceToTarget < closestDistance)
                    {
                        closestDistance = distanceToTarget;
                        ClosestTarget = collider.gameObject;
                    }
                }
            }
            
            // Fire events for newly acquired/lost targets
            if (brain != null)
            {
                foreach (var target in _visibleTargets)
                {
                    if (!_previousTargets.Contains(target))
                    {
                        brain.RaiseTargetAcquired(target);
                    }
                }
                
                foreach (var target in _previousTargets)
                {
                    if (!_visibleTargets.Contains(target))
                    {
                        brain.RaiseTargetLost(target);
                    }
                }
            }
        }
        
        /// <summary>
        /// Checks if a specific position is within the view cone (ignoring obstacles).
        /// </summary>
        /// <param name="position">World position to check.</param>
        /// <returns>True if the position is within view distance and angle.</returns>
        public bool IsInViewCone(Vector3 position)
        {
            Vector3 eyePosition = transform.position + Vector3.up * _eyeHeight;
            Vector3 direction = (position - eyePosition).normalized;
            float distance = Vector3.Distance(eyePosition, position);
            float angle = Vector3.Angle(transform.forward, direction);
            
            return distance <= _viewDistance && angle <= _viewAngle * 0.5f;
        }
        
        /// <summary>
        /// Checks if there's a clear line of sight to a position.
        /// </summary>
        /// <param name="position">World position to check.</param>
        /// <returns>True if nothing blocks the view.</returns>
        public bool HasLineOfSight(Vector3 position)
        {
            Vector3 eyePosition = transform.position + Vector3.up * _eyeHeight;
            Vector3 direction = (position - eyePosition).normalized;
            float distance = Vector3.Distance(eyePosition, position);
            
            return !Physics.Raycast(eyePosition, direction, distance, _obstacleMask);
        }
        
        private void OnDrawGizmosSelected()
        {
            if (!_drawGizmos) return;
            DrawVisionCone();
        }
        
        private void OnDrawGizmos()
        {
            if (!_drawGizmos || !Application.isPlaying) return;
            if (_visibleTargets.Count > 0)
            {
                DrawVisionCone();
            }
        }
        
        private void DrawVisionCone()
        {
            Vector3 eyePosition = transform.position + Vector3.up * _eyeHeight;
            Vector3 forward = transform.forward;
            
            // Choose color based on alert state
            bool hasTargets = Application.isPlaying && _visibleTargets.Count > 0;
            Gizmos.color = hasTargets ? _gizmoColorAlert : _gizmoColorClear;
            
            float halfAngle = _viewAngle * 0.5f;
            Vector3 leftDir = Quaternion.Euler(0, -halfAngle, 0) * forward;
            Vector3 rightDir = Quaternion.Euler(0, halfAngle, 0) * forward;
            
            // Draw cone edges
            Gizmos.DrawRay(eyePosition, leftDir * _viewDistance);
            Gizmos.DrawRay(eyePosition, rightDir * _viewDistance);
            Gizmos.DrawRay(eyePosition, forward * _viewDistance);
            
            // Draw arc
            int segments = 20;
            float angleStep = _viewAngle / segments;
            Vector3 prevPoint = eyePosition + leftDir * _viewDistance;
            
            for (int i = 1; i <= segments; i++)
            {
                float angle = -halfAngle + angleStep * i;
                Vector3 dir = Quaternion.Euler(0, angle, 0) * forward;
                Vector3 point = eyePosition + dir * _viewDistance;
                Gizmos.DrawLine(prevPoint, point);
                prevPoint = point;
            }
            
            // Draw lines to visible targets
            if (Application.isPlaying)
            {
                Gizmos.color = Color.red;
                foreach (var target in _visibleTargets)
                {
                    if (target != null)
                    {
                        Gizmos.DrawLine(eyePosition, target.transform.position);
                    }
                }
            }
        }
        
        private bool ShouldLog()
        {
            return _forceDebugLogging || (_debugLogging && NPCBrainDebug.IsEnabled(NPCBrainDebug.Category.Perception));
        }
        
        /// <summary>
        /// Checks if a tag exists in the Tag Manager without throwing exceptions repeatedly.
        /// </summary>
        private bool IsTagValid(string tag)
        {
            // Try to validate the tag using a dummy comparison
            try
            {
                // Use the gameObject's CompareTag - if tag doesn't exist, this throws
                gameObject.CompareTag(tag);
                return true;
            }
            catch (UnityException)
            {
                NPCBrainDebug.LogError(NPCBrainDebug.Category.Perception, 
                    $"Tag '{tag}' does not exist in Tag Manager! Add it via Edit > Project Settings > Tags and Layers. " +
                    $"This error will only be logged once.", this);
                return false;
            }
        }
        
        /// <summary>
        /// Clears the tag validity cache. Call this if you change the _targetTag at runtime.
        /// </summary>
        public void ClearTagCache()
        {
            _tagValidityChecked = false;
        }
        
        /// <summary>
        /// Sets the target tag at runtime and resets the tag validity cache.
        /// </summary>
        /// <param name="tag">The new tag to filter targets by. Use empty string for no filtering.</param>
        public void SetTargetTag(string tag)
        {
            _targetTag = tag;
            _tagValidityChecked = false;
            _targetTagIsValid = false;
        }
        
        /// <summary>
        /// Gets the current target tag.
        /// </summary>
        public string TargetTag => _targetTag;
    }
}
