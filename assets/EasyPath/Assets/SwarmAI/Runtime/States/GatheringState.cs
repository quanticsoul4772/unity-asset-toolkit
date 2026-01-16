using UnityEngine;

namespace SwarmAI
{
    /// <summary>
    /// State for gathering resources from a ResourceNode.
    /// Agent moves to the resource, harvests it, then transitions based on carry capacity.
    /// </summary>
    public class GatheringState : AgentState
    {
        private ResourceNode _targetResource;
        private Vector3 _basePosition;
        private float _carryCapacity;
        private float _currentCarry;
        private bool _isHarvesting;
        private float _stuckTimer;
        private Vector3 _lastPosition;
        
        /// <summary>
        /// The resource node being gathered from.
        /// </summary>
        public ResourceNode TargetResource => _targetResource;
        
        /// <summary>
        /// Position to return to after gathering.
        /// </summary>
        public Vector3 BasePosition => _basePosition;
        
        /// <summary>
        /// Maximum amount the agent can carry.
        /// </summary>
        public float CarryCapacity => _carryCapacity;
        
        /// <summary>
        /// Current amount being carried.
        /// </summary>
        public float CurrentCarry => _currentCarry;
        
        /// <summary>
        /// Whether currently harvesting (in range of resource).
        /// </summary>
        public bool IsHarvesting => _isHarvesting;
        
        /// <summary>
        /// Create a gathering state targeting a resource node.
        /// </summary>
        /// <param name="targetResource">The resource to gather from.</param>
        /// <param name="basePosition">Position to return to when full.</param>
        /// <param name="carryCapacity">Maximum carry capacity.</param>
        public GatheringState(ResourceNode targetResource, Vector3 basePosition, float carryCapacity = 10f)
        {
            Type = AgentStateType.Gathering;
            _targetResource = targetResource;
            _basePosition = basePosition;
            _carryCapacity = Mathf.Max(1f, carryCapacity);
            _currentCarry = 0f;
            _isHarvesting = false;
        }
        
        /// <summary>
        /// Create a gathering state with an initial carry amount.
        /// </summary>
        public GatheringState(ResourceNode targetResource, Vector3 basePosition, float carryCapacity, float initialCarry)
            : this(targetResource, basePosition, carryCapacity)
        {
            _currentCarry = Mathf.Clamp(initialCarry, 0f, carryCapacity);
        }
        
        public override void Enter()
        {
            base.Enter();
            _lastPosition = Agent.Position;
            _stuckTimer = 0f;
            
            // Start moving toward resource
            if (_targetResource != null)
            {
                Agent.SetTarget(_targetResource.Position);
            }
        }
        
        public override void Execute()
        {
            if (_targetResource == null || _targetResource.IsDepleted)
            {
                // Resource is gone, will transition
                return;
            }
            
            float distanceToResource = Vector3.Distance(Agent.Position, _targetResource.Position);
            
            if (distanceToResource <= _targetResource.HarvestRadius)
            {
                // In range - harvest
                if (!_isHarvesting)
                {
                    _isHarvesting = _targetResource.TryStartHarvesting(Agent);
                }
                
                if (_isHarvesting)
                {
                    float harvested = _targetResource.Harvest(Agent, Time.deltaTime);
                    _currentCarry += harvested;
                }
            }
            else
            {
                // Not in range - move toward resource
                if (_isHarvesting)
                {
                    _targetResource.StopHarvesting(Agent);
                    _isHarvesting = false;
                }
                
                Agent.SetTarget(_targetResource.Position);
                
                // Check if stuck
                CheckStuck();
            }
        }
        
        public override void Exit()
        {
            base.Exit();
            
            // Stop harvesting if we were
            if (_isHarvesting && _targetResource != null)
            {
                _targetResource.StopHarvesting(Agent);
                _isHarvesting = false;
            }
        }
        
        public override AgentState CheckTransitions()
        {
            // Check if target resource is gone or depleted
            if (_targetResource == null || _targetResource.IsDepleted)
            {
                if (_currentCarry > 0)
                {
                    // Have some resources, return to base
                    return new ReturningState(_basePosition, _currentCarry);
                }
                else
                {
                    // Find another resource or go idle
                    var newResource = ResourceNode.FindNearestAvailable(Agent.Position);
                    if (newResource != null)
                    {
                        return new GatheringState(newResource, _basePosition, _carryCapacity);
                    }
                    return new IdleState();
                }
            }
            
            // Check if full
            if (_currentCarry >= _carryCapacity)
            {
                return new ReturningState(_basePosition, _currentCarry);
            }
            
            // Check if stuck for too long
            float stuckTimeLimit = SwarmManager.HasInstance && SwarmManager.Instance.Settings != null
                ? SwarmManager.Instance.Settings.StuckTimeLimit
                : 2f;
            if (_stuckTimer > stuckTimeLimit)
            {
                // Try to find alternative path or resource
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
                    
                case SwarmMessageType.ResourceDepleted:
                    // Check if it's our resource
                    if (_targetResource != null && 
                        Vector3.Distance(message.Position, _targetResource.Position) < 0.1f)
                    {
                        // Find new resource if we haven't gathered much yet (below 90% capacity threshold)
                        var newResource = ResourceNode.FindNearestAvailable(Agent.Position);
                        if (newResource != null && _currentCarry < _carryCapacity * SwarmSettings.GatheringContinueThreshold)
                        {
                            Agent.SetState(new GatheringState(newResource, _basePosition, _carryCapacity, _currentCarry));
                        }
                        else if (_currentCarry > 0)
                        {
                            Agent.SetState(new ReturningState(_basePosition, _currentCarry));
                        }
                        return true;
                    }
                    break;
                    
                case SwarmMessageType.ReturnToBase:
                    if (_currentCarry > 0)
                    {
                        Agent.SetState(new ReturningState(message.Position, _currentCarry));
                        return true;
                    }
                    break;
            }
            
            return false;
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
