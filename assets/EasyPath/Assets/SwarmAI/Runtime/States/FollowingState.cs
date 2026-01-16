using UnityEngine;

namespace SwarmAI
{
    /// <summary>
    /// State for following a leader agent.
    /// Agent maintains formation position relative to leader.
    /// </summary>
    public class FollowingState : AgentState
    {
        private SwarmAgent _leader;
        private int _leaderId;
        private float _followDistance;
        private Vector3 _offset;
        private bool _useOffset;
        private FollowLeaderBehavior _followBehavior;
        
        /// <summary>
        /// The leader being followed.
        /// </summary>
        public SwarmAgent Leader => _leader;
        
        /// <summary>
        /// Distance to maintain behind leader.
        /// </summary>
        public float FollowDistance => _followDistance;
        
        /// <summary>
        /// Create a following state for a specific leader.
        /// </summary>
        /// <param name="leader">The agent to follow.</param>
        /// <param name="followDistance">Distance to maintain.</param>
        public FollowingState(SwarmAgent leader, float followDistance = 3f)
        {
            Type = AgentStateType.Following;
            _leader = leader;
            _leaderId = leader?.AgentId ?? -1;
            _followDistance = Mathf.Max(1f, followDistance);
            _useOffset = false;
        }
        
        /// <summary>
        /// Create a following state with a specific offset.
        /// </summary>
        /// <param name="leader">The agent to follow.</param>
        /// <param name="offset">Offset from leader in leader's local space.</param>
        public FollowingState(SwarmAgent leader, Vector3 offset)
        {
            Type = AgentStateType.Following;
            _leader = leader;
            _leaderId = leader?.AgentId ?? -1;
            _offset = offset;
            _followDistance = offset.magnitude;
            _useOffset = true;
        }
        
        /// <summary>
        /// Create a following state by leader ID.
        /// </summary>
        /// <param name="leaderId">ID of the agent to follow.</param>
        /// <param name="followDistance">Distance to maintain.</param>
        public FollowingState(int leaderId, float followDistance = 3f)
        {
            Type = AgentStateType.Following;
            _leaderId = leaderId;
            _followDistance = Mathf.Max(1f, followDistance);
            _useOffset = false;
        }
        
        public override void Enter()
        {
            base.Enter();
            
            // Look up leader if we only have ID
            if (_leader == null && _leaderId >= 0 && SwarmManager.HasInstance)
            {
                _leader = SwarmManager.Instance.GetAgent(_leaderId);
            }
            
            // Create and add follow behavior
            if (_leader != null)
            {
                if (_useOffset)
                {
                    _followBehavior = new FollowLeaderBehavior(_leader, _offset);
                }
                else
                {
                    _followBehavior = new FollowLeaderBehavior(_leader, _followDistance);
                }
                
                Agent.AddBehavior(_followBehavior, 1.5f);
            }
        }
        
        public override void Execute()
        {
            // Check if leader still exists
            if (_leader == null || _leader.gameObject == null)
            {
                // Try to find leader again
                if (_leaderId >= 0 && SwarmManager.HasInstance)
                {
                    _leader = SwarmManager.Instance.GetAgent(_leaderId);
                    if (_followBehavior != null)
                    {
                        _followBehavior.Leader = _leader;
                    }
                }
            }
        }
        
        public override void Exit()
        {
            base.Exit();
            
            // Remove follow behavior
            if (_followBehavior != null)
            {
                Agent.RemoveBehavior(_followBehavior);
                _followBehavior = null;
            }
        }
        
        public override AgentState CheckTransitions()
        {
            // Leader is gone
            if (_leader == null || _leader.gameObject == null)
            {
                // Try to find by ID
                if (_leaderId >= 0 && SwarmManager.HasInstance)
                {
                    _leader = SwarmManager.Instance.GetAgent(_leaderId);
                }
                
                if (_leader == null)
                {
                    return new IdleState();
                }
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
                    
                case SwarmMessageType.Follow:
                    // Change leader
                    int newLeaderId = (int)message.Value;
                    if (newLeaderId != _leaderId)
                    {
                        Agent.SetState(new FollowingState(newLeaderId, _followDistance));
                    }
                    return true;
                    
                case SwarmMessageType.FormationUpdate:
                    // Update offset position
                    _offset = message.Position - (_leader?.Position ?? Agent.Position);
                    _useOffset = true;
                    if (_followBehavior != null)
                    {
                        _followBehavior.OffsetFromLeader = _offset;
                        _followBehavior.UseOffset = true;
                    }
                    return true;
                    
                case SwarmMessageType.LeaveFormation:
                    Agent.SetState(new IdleState());
                    return true;
            }
            
            return false;
        }
    }
}
