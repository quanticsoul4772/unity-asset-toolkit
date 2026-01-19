using System;
using UnityEngine;

namespace NPCBrain.BehaviorTree.Actions
{
    /// <summary>
    /// Action that waits for a specified duration before completing.
    /// </summary>
    /// <remarks>
    /// Returns Running until the duration elapses, then returns Success.
    /// Commonly used for delays between actions (e.g., pause at waypoints, cooldowns).
    /// </remarks>
    /// <example>
    /// <code>
    /// var tree = new Sequence(
    ///     new MoveTo(() => waypoint),
    ///     new Wait(2f),  // Wait 2 seconds
    ///     new AdvanceWaypoint()
    /// );
    /// </code>
    /// </example>
    public class Wait : BTNode
    {
        private readonly float _duration;
        private readonly Action _onComplete;
        private float _startTime;
        private bool _completed;
        
        /// <summary>
        /// Creates a Wait action.
        /// </summary>
        /// <param name="duration">Time to wait in seconds.</param>
        /// <param name="onComplete">Optional callback when wait completes.</param>
        public Wait(float duration, Action onComplete = null)
        {
            _duration = duration;
            _onComplete = onComplete;
            Name = $"Wait({duration:F1}s)";
        }

        /// <summary>
        /// Records the start time when wait begins.
        /// </summary>
        protected override void OnEnter(NPCBrainController brain)
        {
            _startTime = Time.time;
            _completed = false;
        }

        /// <summary>
        /// Checks elapsed time and returns Success when duration is reached.
        /// </summary>
        /// <param name="brain">The NPCBrainController (unused).</param>
        /// <returns>Running until duration elapses, then Success.</returns>
        protected override NodeStatus Tick(NPCBrainController brain)
        {
            float elapsed = Time.time - _startTime;
            
            if (elapsed >= _duration)
            {
                if (!_completed)
                {
                    _completed = true;
                    _onComplete?.Invoke();
                }
                return NodeStatus.Success;
            }
            
            return NodeStatus.Running;
        }

        /// <summary>
        /// Resets completion flag on exit (for interrupted waits).
        /// </summary>
        protected override void OnExit(NPCBrainController brain)
        {
            _completed = false;
        }
        
        public override void Reset()
        {
            base.Reset();
            _completed = false;
        }
    }
}
