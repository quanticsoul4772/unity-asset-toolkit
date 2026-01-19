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
        
        protected override void OnExit(NPCBrainController brain)
        {
            OnExitCount++;
        }
        
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
        /// Creates a DynamicConsideration initialized with the specified score.
        /// </summary>
        /// <param name="initialScore">Initial value returned by Score(NPCBrainController) until changed via SetScore.</param>
        public DynamicConsideration(float initialScore)
        {
            _score = initialScore;
        }

        /// <summary>
        /// Provides the consideration's current score.
        /// </summary>
        /// <param name="brain">Ignored for this consideration; included to match the evaluation signature.</param>
        /// <returns>The current consideration score.</returns>
        public override float Score(NPCBrainController brain)
        {
            return _score;
        }

        /// <summary>
        /// Sets the consideration's runtime score used when evaluating the node.
        /// </summary>
        /// <param name="score">The new score value that Score(NPCBrainController) will return.</param>
        public void SetScore(float score)
        {
            _score = score;
        }
    }
}