using System.Collections.Generic;
using UnityEngine;

namespace SwarmAI
{
    /// <summary>
    /// Represents a group of SwarmAgents that can be coordinated together.
    /// Groups support leader-follower patterns and group-wide commands.
    /// </summary>
    public class SwarmGroup
    {
        private static int _nextGroupId = 1;
        
        private int _groupId;
        private string _groupName;
        private SwarmAgent _leader;
        private List<SwarmAgent> _members;
        private SwarmFormation _formation;
        
        #region Properties
        
        /// <summary>
        /// Unique ID for this group.
        /// </summary>
        public int GroupId => _groupId;
        
        /// <summary>
        /// Optional name for this group.
        /// </summary>
        public string Name
        {
            get => _groupName;
            set => _groupName = value;
        }
        
        /// <summary>
        /// The leader of this group, or null if no leader.
        /// </summary>
        public SwarmAgent Leader
        {
            get => _leader;
            set => SetLeader(value);
        }
        
        /// <summary>
        /// All members of the group (read-only).
        /// </summary>
        public IReadOnlyList<SwarmAgent> Members => _members;
        
        /// <summary>
        /// Number of members in the group.
        /// </summary>
        public int MemberCount => _members.Count;
        
        /// <summary>
        /// Whether the group has a leader.
        /// </summary>
        public bool HasLeader => _leader != null;
        
        /// <summary>
        /// The formation associated with this group, if any.
        /// </summary>
        public SwarmFormation Formation
        {
            get => _formation;
            set => _formation = value;
        }
        
        /// <summary>
        /// Calculate the center of mass of all group members.
        /// </summary>
        public Vector3 CenterOfMass
        {
            get
            {
                if (_members.Count == 0) return Vector3.zero;
                
                Vector3 sum = Vector3.zero;
                int validCount = 0;
                
                foreach (var member in _members)
                {
                    if (member != null)
                    {
                        sum += member.Position;
                        validCount++;
                    }
                }
                
                return validCount > 0 ? sum / validCount : Vector3.zero;
            }
        }
        
        /// <summary>
        /// Calculate the average velocity of all group members.
        /// </summary>
        public Vector3 AverageVelocity
        {
            get
            {
                if (_members.Count == 0) return Vector3.zero;
                
                Vector3 sum = Vector3.zero;
                int validCount = 0;
                
                foreach (var member in _members)
                {
                    if (member != null)
                    {
                        sum += member.Velocity;
                        validCount++;
                    }
                }
                
                return validCount > 0 ? sum / validCount : Vector3.zero;
            }
        }
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Fired when a member joins the group.
        /// </summary>
        public event System.Action<SwarmAgent> OnMemberJoined;
        
        /// <summary>
        /// Fired when a member leaves the group.
        /// </summary>
        public event System.Action<SwarmAgent> OnMemberLeft;
        
        /// <summary>
        /// Fired when the leader changes.
        /// </summary>
        public event System.Action<SwarmAgent, SwarmAgent> OnLeaderChanged;
        
        #endregion
        
        #region Constructors
        
        /// <summary>
        /// Create a new empty group.
        /// </summary>
        public SwarmGroup(string name = null)
        {
            _groupId = _nextGroupId++;
            _groupName = name ?? $"Group_{_groupId}";
            _members = new List<SwarmAgent>();
        }
        
        /// <summary>
        /// Create a group with an initial leader.
        /// </summary>
        public SwarmGroup(SwarmAgent leader, string name = null) : this(name)
        {
            if (leader != null)
            {
                AddMember(leader);
                _leader = leader;
            }
        }
        
        #endregion
        
        #region Member Management
        
        /// <summary>
        /// Add an agent to the group.
        /// </summary>
        public bool AddMember(SwarmAgent agent)
        {
            if (agent == null || _members.Contains(agent))
                return false;
                
            _members.Add(agent);
            
            // Send join message
            SwarmManager.Instance?.SendMessage(agent.AgentId,
                SwarmMessage.JoinGroup(_groupId, -1, agent.AgentId));
            
            // Add to formation if exists
            _formation?.AddAgent(agent);
            
            OnMemberJoined?.Invoke(agent);
            return true;
        }
        
        /// <summary>
        /// Remove an agent from the group.
        /// </summary>
        public bool RemoveMember(SwarmAgent agent)
        {
            if (agent == null || !_members.Contains(agent))
                return false;
                
            _members.Remove(agent);
            
            // If this was the leader, clear leader
            if (_leader == agent)
            {
                SetLeader(null);
            }
            
            // Remove from formation if exists
            _formation?.RemoveAgent(agent);
            
            // Send leave message
            SwarmManager.Instance?.SendMessage(agent.AgentId,
                SwarmMessage.LeaveGroup(-1, agent.AgentId));
            
            OnMemberLeft?.Invoke(agent);
            return true;
        }
        
        /// <summary>
        /// Check if an agent is a member of this group.
        /// </summary>
        public bool Contains(SwarmAgent agent)
        {
            return agent != null && _members.Contains(agent);
        }
        
        /// <summary>
        /// Remove all members from the group.
        /// </summary>
        public void ClearMembers()
        {
            var membersCopy = new List<SwarmAgent>(_members);
            foreach (var member in membersCopy)
            {
                RemoveMember(member);
            }
        }
        
        /// <summary>
        /// Clean up any destroyed agents.
        /// </summary>
        public void CleanupDestroyedAgents()
        {
            _members.RemoveAll(m => m == null || m.gameObject == null);
            
            // Check if leader was destroyed (Unity "fake null" pattern)
            if (_leader != null && _leader.gameObject == null)
            {
                _leader = null;
            }
        }
        
        private void SetLeader(SwarmAgent newLeader)
        {
            if (newLeader == _leader) return;
            
            var oldLeader = _leader;
            _leader = newLeader;
            
            // Ensure new leader is a member
            if (newLeader != null && !_members.Contains(newLeader))
            {
                AddMember(newLeader);
            }
            
            // Update formation leader
            if (_formation != null)
            {
                _formation.Leader = newLeader;
            }
            
            OnLeaderChanged?.Invoke(oldLeader, newLeader);
        }
        
        /// <summary>
        /// Elect a new leader from the group (closest to center).
        /// </summary>
        public SwarmAgent ElectLeader()
        {
            CleanupDestroyedAgents();
            
            if (_members.Count == 0) return null;
            
            Vector3 center = CenterOfMass;
            SwarmAgent closest = null;
            float closestDist = float.MaxValue;
            
            foreach (var member in _members)
            {
                if (member == null) continue;
                
                float dist = Vector3.Distance(member.Position, center);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = member;
                }
            }
            
            SetLeader(closest);
            return closest;
        }
        
        #endregion
        
        #region Group Commands
        
        /// <summary>
        /// Command all members to move to a position.
        /// </summary>
        public void MoveTo(Vector3 position)
        {
            if (_formation != null)
            {
                _formation.MoveTo(position);
            }
            else
            {
                BroadcastToMembers(SwarmMessage.MoveTo(position));
            }
        }
        
        /// <summary>
        /// Command all members to stop.
        /// </summary>
        public void Stop()
        {
            BroadcastToMembers(SwarmMessage.Stop());
        }
        
        /// <summary>
        /// Command all members to seek a position.
        /// </summary>
        public void Seek(Vector3 position)
        {
            BroadcastToMembers(SwarmMessage.Seek(position));
        }
        
        /// <summary>
        /// Command all members to flee from a position.
        /// </summary>
        public void Flee(Vector3 threatPosition)
        {
            BroadcastToMembers(SwarmMessage.Flee(threatPosition));
        }
        
        /// <summary>
        /// Command all followers to follow the leader.
        /// </summary>
        public void FollowLeader()
        {
            if (_leader == null) return;
            
            foreach (var member in _members)
            {
                if (member != null && member != _leader)
                {
                    SwarmManager.Instance?.SendMessage(member.AgentId,
                        SwarmMessage.Follow(_leader.AgentId, -1, member.AgentId));
                }
            }
        }
        
        /// <summary>
        /// Broadcast a message to all members.
        /// </summary>
        public void BroadcastToMembers(SwarmMessage message)
        {
            // Iterate over a copy to avoid issues if handlers modify the list
            var membersCopy = new List<SwarmAgent>(_members);
            foreach (var member in membersCopy)
            {
                if (member != null && member.gameObject != null)
                {
                    SwarmManager.Instance?.SendMessage(member.AgentId, message.Clone(null, member.AgentId));
                }
            }
        }
        
        #endregion
        
        #region Formation
        
        /// <summary>
        /// Set a formation for this group.
        /// </summary>
        public void SetFormation(SwarmFormation formation)
        {
            // Remove members from old formation
            if (_formation != null)
            {
                foreach (var member in _members)
                {
                    _formation.RemoveAgent(member);
                }
            }
            
            _formation = formation;
            
            // Add members to new formation
            if (_formation != null)
            {
                _formation.Leader = _leader;
                foreach (var member in _members)
                {
                    if (member != _leader) // Leader doesn't need a slot
                    {
                        _formation.AddAgent(member);
                    }
                }
            }
        }
        
        /// <summary>
        /// Change the formation type (requires attached formation component).
        /// </summary>
        public void SetFormationType(FormationType type)
        {
            if (_formation != null)
            {
                _formation.Type = type;
            }
        }
        
        #endregion
    }
}
