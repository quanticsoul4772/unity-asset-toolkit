using UnityEngine;
using NPCBrain.BehaviorTree;
using NPCBrain.Criticality;
using NPCBrain.UtilityAI;

namespace NPCBrain.Tests
{
    /// <summary>
    /// Test brain that allows direct initialization without relying on Awake().
    /// Overrides Awake() to prevent automatic initialization, giving tests full control.
    /// NOTE: This must be outside the Editor folder to be usable as a MonoBehaviour component.
    /// </summary>
    public class TestBrain : NPCBrainController
    {
        protected override void Awake()
        {
            // Don't call base.Awake() - tests control initialization via InitializeForTests()
        }
        
        public void InitializeForTests()
        {
            // Disable warnings about missing sensors - tests typically don't need them
            WarnOnMissingComponents = false;
            
            // Protected setters are accessible from derived classes
            Blackboard = new Blackboard();
            Criticality = new CriticalityController();
        }
    }
    
    /// <summary>
    /// Mock BTNode for testing composite nodes and node lifecycle.
    /// </summary>
    public class MockNode : BTNode
    {
        private readonly NodeStatus _status;
        
        public int TickCount { get; private set; }
        public int OnEnterCount { get; private set; }
        public int OnExitCount { get; private set; }
        
        public MockNode(NodeStatus status)
        {
            _status = status;
            Name = "MockNode";
        }
        
        protected override NodeStatus Tick(NPCBrainController brain)
        {
            TickCount++;
            return _status;
        }
        
        protected override void OnEnter(NPCBrainController brain)
        {
            OnEnterCount++;
        }
        
        /// <summary>
        /// Increments the recorded number of times this node's exit logic has been invoked.
        /// </summary>
        protected override void OnExit(NPCBrainController brain)
        {
            OnExitCount++;
        }
        
        /// <summary>
        /// Resets TickCount, OnEnterCount, and OnExitCount to zero.
        /// <summary>
        /// Resets TickCount, OnEnterCount, and OnExitCount to zero.
        /// </summary>
        public void ResetCounts()
        {
            TickCount = 0;
            OnEnterCount = 0;
            OnExitCount = 0;
        }
    }

    /// <summary>
    /// Test consideration that allows changing the score at runtime.
    /// Useful for testing dynamic behavior changes.
    /// </summary>
    public class DynamicConsideration : Consideration
    {
        private float _score;

        /// <summary>
        /// Creates a DynamicConsideration initialized to the provided score.
        /// </summary>
        /// <param name="initialScore">Initial score value returned until changed via SetScore.</param>
        public DynamicConsideration(float initialScore) : base("DynamicConsideration")
        {
            _score = initialScore;
        }

        /// <summary>
        /// Evaluates and returns the current score.
        /// </summary>
        /// <param name="brain">Ignored for this consideration.</param>
        /// <returns>The current consideration score.</returns>
        protected override float Evaluate(NPCBrainController brain)
        {
            return _score;
        }

        /// <summary>
        /// Sets the consideration's runtime score.
        /// </summary>
        /// <param name="score">The new score value.</param>
        public void SetScore(float score)
        {
            _score = score;
        }
    }
}