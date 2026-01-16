using System.Collections.Generic;
using UnityEngine;

namespace SwarmAI
{
    /// <summary>
    /// Component that manages a formation of SwarmAgents.
    /// Attach to a leader agent or a separate formation controller object.
    /// </summary>
    public class SwarmFormation : MonoBehaviour
    {
        [Header("Formation Settings")]
        [SerializeField] private FormationType _formationType = FormationType.Line;
        [SerializeField] private float _spacing = 2f;
        [SerializeField] private int _maxSlots = 10;
        
        [Header("Movement")]
        [SerializeField] private bool _matchLeaderRotation = true;
        
        [Header("Custom Formation")]
        [SerializeField] private List<Vector3> _customOffsets = new List<Vector3>();
        
        [Header("Debug")]
        [SerializeField] private bool _showDebugGizmos = true;
        [SerializeField] private Color _slotColor = Color.cyan;
        [SerializeField] private Color _occupiedSlotColor = Color.green;
        
        // Internal state
        private int _formationId;
        private static int _nextFormationId = 1;
        private List<FormationSlot> _slots;
        private SwarmAgent _leader;
        private Vector3 _targetPosition;
        private Quaternion _targetRotation;
        
        #region Properties
        
        /// <summary>
        /// Unique ID for this formation.
        /// </summary>
        public int FormationId => _formationId;
        
        /// <summary>
        /// Current formation type.
        /// </summary>
        public FormationType Type
        {
            get => _formationType;
            set
            {
                if (_formationType != value)
                {
                    _formationType = value;
                    RegenerateSlots();
                }
            }
        }
        
        /// <summary>
        /// Spacing between formation slots.
        /// </summary>
        public float Spacing
        {
            get => _spacing;
            set
            {
                _spacing = Mathf.Max(0.5f, value);
                RegenerateSlots();
            }
        }
        
        /// <summary>
        /// Maximum number of slots in the formation.
        /// </summary>
        public int MaxSlots => _maxSlots;
        
        /// <summary>
        /// Number of agents currently in the formation.
        /// </summary>
        public int AgentCount
        {
            get
            {
                int count = 0;
                if (_slots != null)
                {
                    foreach (var slot in _slots)
                    {
                        if (slot.IsOccupied) count++;
                    }
                }
                return count;
            }
        }
        
        /// <summary>
        /// The leader of this formation, if any.
        /// </summary>
        public SwarmAgent Leader
        {
            get => _leader;
            set => _leader = value;
        }
        
        /// <summary>
        /// Current center position of the formation.
        /// </summary>
        public Vector3 Position => _leader != null ? _leader.Position : transform.position;
        
        /// <summary>
        /// Current rotation of the formation.
        /// </summary>
        public Quaternion Rotation => _matchLeaderRotation && _leader != null 
            ? _leader.transform.rotation 
            : transform.rotation;
        
        /// <summary>
        /// All slots in the formation (read-only).
        /// </summary>
        public IReadOnlyList<FormationSlot> Slots => _slots;
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Fired when an agent joins the formation.
        /// </summary>
        public event System.Action<SwarmAgent, int> OnAgentJoined;
        
        /// <summary>
        /// Fired when an agent leaves the formation.
        /// </summary>
        public event System.Action<SwarmAgent> OnAgentLeft;
        
        /// <summary>
        /// Fired when the formation type changes.
        /// </summary>
        public event System.Action<FormationType> OnFormationChanged;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            _formationId = _nextFormationId++;
            _slots = new List<FormationSlot>();
            
            // Try to get leader from this object
            _leader = GetComponent<SwarmAgent>();
            
            RegenerateSlots();
        }
        
        private void Update()
        {
            UpdateSlotPositions();
        }
        
        #endregion
        
        #region Slot Management
        
        /// <summary>
        /// Add an agent to the formation.
        /// </summary>
        /// <returns>The slot index, or -1 if formation is full.</returns>
        public int AddAgent(SwarmAgent agent)
        {
            if (agent == null) return -1;
            
            // Check if already in formation
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].AssignedAgent == agent)
                {
                    return i;
                }
            }
            
            // Find empty slot
            for (int i = 0; i < _slots.Count; i++)
            {
                if (!_slots[i].IsOccupied)
                {
                    AssignAgentToSlot(agent, i);
                    return i;
                }
            }
            
            return -1; // Formation full
        }
        
        /// <summary>
        /// Add an agent to a specific slot.
        /// </summary>
        public bool AddAgentToSlot(SwarmAgent agent, int slotIndex)
        {
            if (agent == null || slotIndex < 0 || slotIndex >= _slots.Count)
                return false;
                
            if (_slots[slotIndex].IsOccupied)
                return false;
                
            AssignAgentToSlot(agent, slotIndex);
            return true;
        }
        
        /// <summary>
        /// Remove an agent from the formation.
        /// </summary>
        public bool RemoveAgent(SwarmAgent agent)
        {
            if (agent == null) return false;
            
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].AssignedAgent == agent)
                {
                    var slot = _slots[i];
                    slot.AssignedAgent = null;
                    _slots[i] = slot;
                    
                    OnAgentLeft?.Invoke(agent);
                    return true;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Remove all agents from the formation.
        /// </summary>
        public void ClearFormation()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].IsOccupied)
                {
                    var agent = _slots[i].AssignedAgent;
                    var slot = _slots[i];
                    slot.AssignedAgent = null;
                    _slots[i] = slot;
                    
                    OnAgentLeft?.Invoke(agent);
                }
            }
        }
        
        /// <summary>
        /// Get the world position of a slot.
        /// </summary>
        public Vector3 GetSlotWorldPosition(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _slots.Count)
                return Position;
                
            return _slots[slotIndex].GetWorldPosition(Position, Rotation);
        }
        
        /// <summary>
        /// Get the slot index for an agent, or -1 if not in formation.
        /// </summary>
        public int GetAgentSlotIndex(SwarmAgent agent)
        {
            if (agent == null) return -1;
            
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].AssignedAgent == agent)
                    return i;
            }
            
            return -1;
        }
        
        private void AssignAgentToSlot(SwarmAgent agent, int slotIndex)
        {
            var slot = _slots[slotIndex];
            slot.AssignedAgent = agent;
            _slots[slotIndex] = slot;
            
            // Send formation update message to agent (senderId = formationId for attribution)
            Vector3 slotPosition = GetSlotWorldPosition(slotIndex);
            SwarmManager.Instance?.SendMessage(agent.AgentId, 
                SwarmMessage.FormationUpdate(slotPosition, _formationId, agent.AgentId));
            
            OnAgentJoined?.Invoke(agent, slotIndex);
        }
        
        private void UpdateSlotPositions()
        {
            // Send position updates to all assigned agents
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].IsOccupied)
                {
                    var agent = _slots[i].AssignedAgent;
                    if (agent != null && agent.gameObject != null)
                    {
                        Vector3 slotPosition = GetSlotWorldPosition(i);
                        agent.SetTarget(slotPosition);
                    }
                    else
                    {
                        // Agent was destroyed, clear slot
                        var slot = _slots[i];
                        slot.AssignedAgent = null;
                        _slots[i] = slot;
                    }
                }
            }
        }
        
        #endregion
        
        #region Formation Generation
        
        /// <summary>
        /// Regenerate formation slots based on current settings.
        /// </summary>
        public void RegenerateSlots()
        {
            // Preserve assigned agents
            var preservedAgents = new List<SwarmAgent>();
            if (_slots != null)
            {
                foreach (var slot in _slots)
                {
                    if (slot.IsOccupied)
                        preservedAgents.Add(slot.AssignedAgent);
                }
            }
            
            _slots = GenerateSlots(_formationType, _maxSlots, _spacing);
            
            // Reassign preserved agents
            foreach (var agent in preservedAgents)
            {
                AddAgent(agent);
            }
            
            OnFormationChanged?.Invoke(_formationType);
        }
        
        /// <summary>
        /// Generate slots for a formation type.
        /// </summary>
        private List<FormationSlot> GenerateSlots(FormationType type, int count, float spacing)
        {
            var slots = new List<FormationSlot>();
            
            switch (type)
            {
                case FormationType.Line:
                    slots = GenerateLineFormation(count, spacing);
                    break;
                case FormationType.Column:
                    slots = GenerateColumnFormation(count, spacing);
                    break;
                case FormationType.Circle:
                    slots = GenerateCircleFormation(count, spacing);
                    break;
                case FormationType.Wedge:
                    slots = GenerateWedgeFormation(count, spacing);
                    break;
                case FormationType.V:
                    slots = GenerateVFormation(count, spacing);
                    break;
                case FormationType.Box:
                    slots = GenerateBoxFormation(count, spacing);
                    break;
                case FormationType.Custom:
                    slots = GenerateCustomFormation(_customOffsets);
                    break;
                default:
                    // None - no slots
                    break;
            }
            
            return slots;
        }
        
        private List<FormationSlot> GenerateLineFormation(int count, float spacing)
        {
            var slots = new List<FormationSlot>();
            float halfWidth = (count - 1) * spacing * 0.5f;
            
            for (int i = 0; i < count; i++)
            {
                float x = i * spacing - halfWidth;
                slots.Add(new FormationSlot(new Vector3(x, 0, 0), i));
            }
            
            return slots;
        }
        
        private List<FormationSlot> GenerateColumnFormation(int count, float spacing)
        {
            var slots = new List<FormationSlot>();
            
            for (int i = 0; i < count; i++)
            {
                float z = -i * spacing; // Behind the leader
                slots.Add(new FormationSlot(new Vector3(0, 0, z), i));
            }
            
            return slots;
        }
        
        private List<FormationSlot> GenerateCircleFormation(int count, float spacing)
        {
            var slots = new List<FormationSlot>();
            
            // Guard against divide-by-zero
            if (count <= 0) return slots;
            
            // Calculate radius from circumference: C = 2*PI*r, r = (count * spacing) / (2 * PI)
            float radius = count * spacing / (2f * Mathf.PI);
            radius = Mathf.Max(radius, spacing);
            
            for (int i = 0; i < count; i++)
            {
                float angle = (i / (float)count) * 2f * Mathf.PI;
                float x = Mathf.Sin(angle) * radius;
                float z = Mathf.Cos(angle) * radius;
                slots.Add(new FormationSlot(new Vector3(x, 0, z), i));
            }
            
            return slots;
        }
        
        private List<FormationSlot> GenerateWedgeFormation(int count, float spacing)
        {
            var slots = new List<FormationSlot>();
            int row = 0;
            int index = 0;
            
            while (index < count)
            {
                int unitsInRow = row + 1;
                // 0.866f = cos(30°) = sqrt(3)/2 - creates 60° wedge angle
                float rowOffset = -row * spacing * 0.866f; // Behind leader
                float halfWidth = row * spacing * 0.5f;
                
                for (int i = 0; i < unitsInRow && index < count; i++)
                {
                    float x = i * spacing - halfWidth;
                    slots.Add(new FormationSlot(new Vector3(x, 0, rowOffset), index));
                    index++;
                }
                row++;
            }
            
            return slots;
        }
        
        private List<FormationSlot> GenerateVFormation(int count, float spacing)
        {
            var slots = new List<FormationSlot>();
            
            // Guard against empty formation
            if (count <= 0) return slots;
            
            // Leader at front
            slots.Add(new FormationSlot(Vector3.zero, 0));
            
            // Alternate left and right wings
            for (int i = 1; i < count; i++)
            {
                int wing = (i + 1) / 2;
                bool isLeft = (i % 2) == 1;
                
                float x = wing * spacing * (isLeft ? -1 : 1);
                // 0.7f creates approximately 55° angle from forward - classic V formation angle
                float z = -wing * spacing * 0.7f; // Behind
                
                slots.Add(new FormationSlot(new Vector3(x, 0, z), i));
            }
            
            return slots;
        }
        
        private List<FormationSlot> GenerateBoxFormation(int count, float spacing)
        {
            var slots = new List<FormationSlot>();
            int cols = Mathf.CeilToInt(Mathf.Sqrt(count));
            int rows = Mathf.CeilToInt((float)count / cols);
            
            float halfWidth = (cols - 1) * spacing * 0.5f;
            float halfDepth = (rows - 1) * spacing * 0.5f;
            
            int index = 0;
            for (int row = 0; row < rows && index < count; row++)
            {
                for (int col = 0; col < cols && index < count; col++)
                {
                    float x = col * spacing - halfWidth;
                    float z = -row * spacing + halfDepth;
                    slots.Add(new FormationSlot(new Vector3(x, 0, z), index));
                    index++;
                }
            }
            
            return slots;
        }
        
        private List<FormationSlot> GenerateCustomFormation(List<Vector3> offsets)
        {
            var slots = new List<FormationSlot>();
            
            for (int i = 0; i < offsets.Count; i++)
            {
                slots.Add(new FormationSlot(offsets[i], i));
            }
            
            return slots;
        }
        
        /// <summary>
        /// Set custom formation offsets.
        /// </summary>
        public void SetCustomOffsets(List<Vector3> offsets)
        {
            _customOffsets = new List<Vector3>(offsets);
            if (_formationType == FormationType.Custom)
            {
                RegenerateSlots();
            }
        }
        
        #endregion
        
        #region Commands
        
        /// <summary>
        /// Move the entire formation to a position.
        /// </summary>
        public void MoveTo(Vector3 position)
        {
            _targetPosition = position;
            
            if (_leader != null)
            {
                _leader.SetTarget(position);
            }
            else
            {
                transform.position = position;
            }
        }
        
        /// <summary>
        /// Set the formation's rotation.
        /// </summary>
        public void SetRotation(Quaternion rotation)
        {
            _targetRotation = rotation;
            
            if (!_matchLeaderRotation)
            {
                transform.rotation = rotation;
            }
        }
        
        /// <summary>
        /// Face a direction.
        /// </summary>
        public void FaceDirection(Vector3 direction)
        {
            if (direction.sqrMagnitude > 0.001f)
            {
                SetRotation(Quaternion.LookRotation(direction));
            }
        }
        
        #endregion
        
        #region Debug
        
        private void OnDrawGizmos()
        {
            if (!_showDebugGizmos || _slots == null) return;
            
            Vector3 center = Application.isPlaying ? Position : transform.position;
            Quaternion rotation = Application.isPlaying ? Rotation : transform.rotation;
            
            foreach (var slot in _slots)
            {
                Vector3 worldPos = slot.GetWorldPosition(center, rotation);
                Gizmos.color = slot.IsOccupied ? _occupiedSlotColor : _slotColor;
                Gizmos.DrawWireSphere(worldPos, 0.5f);
                
                if (slot.IsOccupied && slot.AssignedAgent != null)
                {
                    Gizmos.DrawLine(worldPos, slot.AssignedAgent.Position);
                }
            }
            
            // Draw formation center
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(center, 0.3f);
            Gizmos.DrawRay(center, rotation * Vector3.forward * 2f);
        }
        
        #endregion
    }
}
