using UnityEngine;

namespace SwarmAI
{
    /// <summary>
    /// State for returning to base with gathered resources.
    /// Agent moves to base position, deposits resources, then can go gather more.
    /// </summary>
    public class ReturningState : AgentState
    {
        private Vector3 _basePosition;
        private Transform _baseTransform;
        private float _carryAmount;
        private float _depositRadius;
        private bool _hasDeposited;
        private string _resourceType;
        
        // Stuck detection (consistent with GatheringState)
        private float _stuckTimer;
        private Vector3 _lastPosition;
        
        /// <summary>
        /// Position of the base to return to.
        /// </summary>
        public Vector3 BasePosition => _baseTransform != null ? _baseTransform.position : _basePosition;
        
        /// <summary>
        /// Amount of resources being carried.
        /// </summary>
        public float CarryAmount => _carryAmount;
        
        /// <summary>
        /// Whether resources have been deposited.
        /// </summary>
        public bool HasDeposited => _hasDeposited;
        
        /// <summary>
        /// Fired when resources are deposited at base.
        /// </summary>
        public event System.Action<float> OnResourcesDeposited;
        
        /// <summary>
        /// Create a returning state to a position.
        /// </summary>
        /// <param name="basePosition">Position to return to.</param>
        /// <param name="carryAmount">Amount of resources being carried.</param>
        /// <param name="depositRadius">Distance from base to trigger deposit.</param>
        public ReturningState(Vector3 basePosition, float carryAmount, float depositRadius = 2f)
        {
            Type = AgentStateType.Returning;
            _basePosition = basePosition;
            _carryAmount = Mathf.Max(0f, carryAmount);
            _depositRadius = Mathf.Max(0.5f, depositRadius);
            _hasDeposited = false;
        }
        
        /// <summary>
        /// Create a returning state to a transform (tracks position).
        /// </summary>
        /// <param name="baseTransform">Transform to return to.</param>
        /// <param name="carryAmount">Amount of resources being carried.</param>
        /// <param name="depositRadius">Distance from base to trigger deposit.</param>
        public ReturningState(Transform baseTransform, float carryAmount, float depositRadius = 2f)
        {
            Type = AgentStateType.Returning;
            _baseTransform = baseTransform;
            _basePosition = baseTransform != null ? baseTransform.position : Vector3.zero;
            _carryAmount = Mathf.Max(0f, carryAmount);
            _depositRadius = Mathf.Max(0.5f, depositRadius);
            _hasDeposited = false;
        }
        
        /// <summary>
        /// Create a returning state with a resource type.
        /// </summary>
        public ReturningState(Vector3 basePosition, float carryAmount, string resourceType, float depositRadius = 2f)
            : this(basePosition, carryAmount, depositRadius)
        {
            _resourceType = resourceType;
        }
        
        public override void Enter()
        {
            base.Enter();
            Agent.SetTarget(BasePosition);
            _lastPosition = Agent.Position;
            _stuckTimer = 0f;
        }
        
        public override void Execute()
        {
            // Update target if following a transform
            if (_baseTransform != null)
            {
                _basePosition = _baseTransform.position;
                Agent.SetTarget(_basePosition);
            }
            
            // Check if at base
            float distanceToBase = Vector3.Distance(Agent.Position, BasePosition);
            
            if (distanceToBase <= _depositRadius && !_hasDeposited)
            {
                // Deposit resources
                DepositResources();
            }
            else if (!_hasDeposited)
            {
                // Check if stuck while moving to base
                CheckStuck();
            }
        }
        
        public override AgentState CheckTransitions()
        {
            // Check if base transform was destroyed (Unity object may be "fake null")
            if (_baseTransform != null && _baseTransform.gameObject == null)
            {
                return new IdleState();
            }
            
            // Check if deposited
            if (_hasDeposited)
            {
                // Find new resource to gather
                var resource = ResourceNode.FindNearestAvailable(Agent.Position);
                if (resource != null)
                {
                    return new GatheringState(resource, BasePosition);
                }
                
                return new IdleState();
            }
            
            // Check if stuck for too long
            float stuckTimeLimit = SwarmManager.HasInstance && SwarmManager.Instance.Settings != null
                ? SwarmManager.Instance.Settings.StuckTimeLimit
                : 2f;
            if (_stuckTimer > stuckTimeLimit)
            {
                // Can't reach base, go idle
                return new IdleState();
            }
            
            return this;
        }
        
        public override bool HandleMessage(SwarmMessage message)
        {
            switch (message.Type)
            {
                case SwarmMessageType.Stop:
                    Agent.SetState(new IdleState());
                    return true;
                    
                case SwarmMessageType.ReturnToBase:
                    // Update base position
                    _basePosition = message.Position;
                    _baseTransform = null;
                    Agent.SetTarget(_basePosition);
                    return true;
                    
                case SwarmMessageType.GatherResource:
                    // Abort return and gather new resource
                    if (message.Data is ResourceNode resource && !resource.IsDepleted)
                    {
                        Agent.SetState(new GatheringState(resource, BasePosition));
                        return true;
                    }
                    break;
            }
            
            return false;
        }
        
        private void DepositResources()
        {
            _hasDeposited = true;
            
            // Fire event
            OnResourcesDeposited?.Invoke(_carryAmount);
            
            // Clear carry amount
            float deposited = _carryAmount;
            _carryAmount = 0f;
            
            // Only log if debug visualization is enabled
            if (SwarmManager.HasInstance && SwarmManager.Instance.Settings != null && 
                SwarmManager.Instance.Settings.EnableDebugVisualization)
            {
                Debug.Log($"[ReturningState] Agent {Agent.AgentId} deposited {deposited} resources");
            }
        }
        
        private void CheckStuck()
        {
            // Use squared distance for performance
            float movedSq = (Agent.Position - _lastPosition).sqrMagnitude;
            float stuckThreshold = SwarmManager.HasInstance && SwarmManager.Instance.Settings != null
                ? SwarmManager.Instance.Settings.StuckThreshold
                : 0.1f;
            float thresholdSq = stuckThreshold * Time.deltaTime;
            thresholdSq *= thresholdSq;
            
            if (movedSq < thresholdSq)
            {
                _stuckTimer += Time.deltaTime;
            }
            else
            {
                _stuckTimer = 0f;
            }
            
            _lastPosition = Agent.Position;
        }
    }
}
