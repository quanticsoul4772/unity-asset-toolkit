using System;
using System.Collections.Generic;
using UnityEngine;

namespace NPCBrain.Perception
{
    /// <summary>
    /// Selects and prioritizes targets based on configurable scoring criteria.
    /// </summary>
    public class TargetSelector
    {
        /// <summary>
        /// Scoring weights for target selection.
        /// </summary>
        [Serializable]
        public class ScoringWeights
        {
            /// <summary>Weight for distance (closer = higher score).</summary>
            public float Distance = 1f;
            
            /// <summary>Weight for angle (more centered = higher score).</summary>
            public float Angle = 0.5f;
            
            /// <summary>Weight for memory confidence.</summary>
            public float Confidence = 0.3f;
            
            /// <summary>Weight for threat level from blackboard.</summary>
            public float ThreatLevel = 1f;
            
            /// <summary>Bonus for currently visible targets.</summary>
            public float VisibilityBonus = 0.5f;
            
            /// <summary>Bonus for recently heard targets.</summary>
            public float HearingBonus = 0.3f;
            
            /// <summary>Time window for hearing bonus (seconds).</summary>
            public float HearingBonusWindow = 5f;
        }
        
        /// <summary>
        /// Result of target scoring.
        /// </summary>
        public class ScoredTarget
        {
            /// <summary>The target GameObject.</summary>
            public GameObject Target { get; set; }
            
            /// <summary>Total priority score (higher = more priority).</summary>
            public float Score { get; set; }
            
            /// <summary>Distance to the target.</summary>
            public float Distance { get; set; }
            
            /// <summary>Angle to the target from forward direction.</summary>
            public float Angle { get; set; }
            
            /// <summary>True if currently visible.</summary>
            public bool IsVisible { get; set; }
            
            /// <summary>Memory confidence level.</summary>
            public float Confidence { get; set; }
            
            /// <summary>True if recently heard.</summary>
            public bool WasHeard { get; set; }
        }
        
        private readonly List<ScoredTarget> _scoredTargets = new List<ScoredTarget>(16);
        private readonly HashSet<GameObject> _addedTargets = new HashSet<GameObject>();
        private readonly Stack<ScoredTarget> _scoredTargetPool = new Stack<ScoredTarget>(16);
        private readonly ScoringWeights _weights;
        
        // Cached comparator to avoid allocation during sort
        private static readonly System.Comparison<ScoredTarget> _scoreComparer = 
            (a, b) => b.Score.CompareTo(a.Score);
        
        /// <summary>Maximum distance to consider targets.</summary>
        public float MaxDistance { get; set; } = 50f;
        
        /// <summary>Current scoring weights.</summary>
        public ScoringWeights Weights => _weights;
        
        /// <summary>List of scored targets from last evaluation.</summary>
        public IReadOnlyList<ScoredTarget> ScoredTargets => _scoredTargets;
        
        /// <summary>
        /// Creates a new TargetSelector with default weights.
        /// </summary>
        public TargetSelector() : this(new ScoringWeights())
        {
        }
        
        /// <summary>
        /// Creates a new TargetSelector with custom weights.
        /// </summary>
        /// <param name="weights">Scoring weights to use.</param>
        public TargetSelector(ScoringWeights weights)
        {
            _weights = weights;
        }
        
        /// <summary>
        /// Evaluates and scores all known targets.
        /// </summary>
        /// <param name="sightSensor">The sight sensor for visibility info.</param>
        /// <param name="memory">The memory system for remembered targets.</param>
        /// <param name="selectorPosition">Position of the NPC doing the selection.</param>
        /// <param name="selectorForward">Forward direction of the NPC.</param>
        /// <param name="blackboard">Optional blackboard for threat levels.</param>
        /// <returns>List of scored targets, sorted by priority (highest first).</returns>
        public IReadOnlyList<ScoredTarget> Evaluate(
            SightSensor sightSensor,
            Memory memory,
            Vector3 selectorPosition,
            Vector3 selectorForward,
            Blackboard blackboard = null)
        {
            // Return pooled objects from previous evaluation
            for (int i = 0; i < _scoredTargets.Count; i++)
            {
                _scoredTargetPool.Push(_scoredTargets[i]);
            }
            _scoredTargets.Clear();
            _addedTargets.Clear();
            
            // Score visible targets
            if (sightSensor != null)
            {
                var visibleTargets = sightSensor.VisibleTargets;
                for (int i = 0; i < visibleTargets.Count; i++)
                {
                    var target = visibleTargets[i];
                    if (target == null) continue;
                    
                    var scored = ScoreTarget(
                        target,
                        target.transform.position,
                        selectorPosition,
                        selectorForward,
                        true,
                        1f,
                        blackboard,
                        false);
                    
                    _scoredTargets.Add(scored);
                    _addedTargets.Add(target);
                }
            }
            
            // Score remembered but not visible targets
            if (memory != null)
            {
                // Use enumerator directly to avoid allocation
                var enumerator = memory.Memories.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    var mem = enumerator.Current.Value;
                    if (mem.Target == null || mem.IsCurrentlyVisible) continue;
                    
                    // O(1) lookup instead of O(n)
                    if (_addedTargets.Contains(mem.Target)) continue;
                    
                    var scoredMem = ScoreTarget(
                        mem.Target,
                        mem.LastKnownPosition,
                        selectorPosition,
                        selectorForward,
                        false,
                        mem.Confidence,
                        blackboard,
                        mem.WasHeard && mem.TimeSinceLastHeard < _weights.HearingBonusWindow);
                    
                    _scoredTargets.Add(scoredMem);
                    _addedTargets.Add(mem.Target);
                }
                enumerator.Dispose();
            }
            
            // Sort by score (highest first) using cached comparator
            _scoredTargets.Sort(_scoreComparer);
            
            return _scoredTargets;
        }
        
        private ScoredTarget ScoreTarget(
            GameObject target,
            Vector3 targetPosition,
            Vector3 selectorPosition,
            Vector3 selectorForward,
            bool isVisible,
            float confidence,
            Blackboard blackboard,
            bool wasRecentlyHeard)
        {
            float distance = Vector3.Distance(selectorPosition, targetPosition);
            Vector3 dirToTarget = (targetPosition - selectorPosition).normalized;
            float angle = Vector3.Angle(selectorForward, dirToTarget);
            
            // Calculate distance score (inverse - closer is better)
            float distanceScore = 1f - Mathf.Clamp01(distance / MaxDistance);
            
            // Calculate angle score (inverse - more centered is better)
            float angleScore = 1f - Mathf.Clamp01(angle / 180f);
            
            // Get threat level from blackboard if available
            float threatLevel = 1f;
            if (blackboard != null && blackboard.Has("threat_" + target.name))
            {
                // Only allocate string if key exists
                threatLevel = blackboard.Get("threat_" + target.name, 1f);
            }
            
            // Calculate total score
            float score = 0f;
            score += distanceScore * _weights.Distance;
            score += angleScore * _weights.Angle;
            score += confidence * _weights.Confidence;
            score += threatLevel * _weights.ThreatLevel;
            
            if (isVisible)
            {
                score += _weights.VisibilityBonus;
            }
            
            if (wasRecentlyHeard)
            {
                score += _weights.HearingBonus;
            }
            
            // Get from pool or create new
            ScoredTarget result = _scoredTargetPool.Count > 0 
                ? _scoredTargetPool.Pop() 
                : new ScoredTarget();
            
            result.Target = target;
            result.Score = score;
            result.Distance = distance;
            result.Angle = angle;
            result.IsVisible = isVisible;
            result.Confidence = confidence;
            result.WasHeard = wasRecentlyHeard;
            
            return result;
        }
        
        /// <summary>
        /// Gets the highest priority target.
        /// </summary>
        /// <param name="sightSensor">The sight sensor.</param>
        /// <param name="memory">The memory system.</param>
        /// <param name="selectorPosition">Position of the selector.</param>
        /// <param name="selectorForward">Forward direction of the selector.</param>
        /// <param name="blackboard">Optional blackboard.</param>
        /// <returns>The best target, or null if none available.</returns>
        public GameObject SelectBest(
            SightSensor sightSensor,
            Memory memory,
            Vector3 selectorPosition,
            Vector3 selectorForward,
            Blackboard blackboard = null)
        {
            var scored = Evaluate(sightSensor, memory, selectorPosition, selectorForward, blackboard);
            return scored.Count > 0 ? scored[0].Target : null;
        }
        
        /// <summary>
        /// Gets the highest priority visible target only.
        /// </summary>
        /// <param name="sightSensor">The sight sensor.</param>
        /// <param name="selectorPosition">Position of the selector.</param>
        /// <param name="selectorForward">Forward direction of the selector.</param>
        /// <returns>The best visible target, or null if none visible.</returns>
        public GameObject SelectBestVisible(
            SightSensor sightSensor,
            Vector3 selectorPosition,
            Vector3 selectorForward)
        {
            if (sightSensor == null || !sightSensor.HasVisibleTargets)
                return null;
            
            var scored = Evaluate(sightSensor, null, selectorPosition, selectorForward, null);
            
            for (int i = 0; i < scored.Count; i++)
            {
                if (scored[i].IsVisible)
                    return scored[i].Target;
            }
            
            return null;
        }
    }
}
