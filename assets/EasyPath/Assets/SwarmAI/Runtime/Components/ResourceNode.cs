using System.Collections.Generic;
using UnityEngine;

namespace SwarmAI
{
    /// <summary>
    /// Component for harvestable resource nodes.
    /// Agents can gather resources from this node until it's depleted.
    /// </summary>
    public class ResourceNode : MonoBehaviour
    {
        [Header("Resource Settings")]
        [SerializeField] private string _resourceType = "Generic";
        [SerializeField] private float _totalAmount = 100f;
        [SerializeField] private float _harvestRate = 10f;
        [SerializeField] private float _harvestRadius = 2f;
        [SerializeField] private int _maxHarvesters = 3;
        [SerializeField] private bool _respawns = false;
        [SerializeField] private float _respawnTime = 30f;
        
        [Header("Visual Feedback")]
        [SerializeField] private bool _scaleWithAmount = true;
        [SerializeField] private float _minScale = 0.2f;
        
        [Header("Debug")]
        [SerializeField] private bool _showDebugGizmos = true;
        [SerializeField] private Color _availableColor = Color.green;
        [SerializeField] private Color _depletedColor = Color.gray;
        
        // Internal state
        private float _currentAmount;
        private float _initialAmount;
        private HashSet<SwarmAgent> _activeHarvesters;
        private float _respawnTimer;
        private Vector3 _initialScale;
        
        // Static registry for efficient lookups
        private static readonly List<ResourceNode> _allNodes = new List<ResourceNode>();
        
        #region Properties
        
        /// <summary>
        /// Type of resource (for filtering/matching).
        /// </summary>
        public string ResourceType => _resourceType;
        
        /// <summary>
        /// Current amount of resource remaining.
        /// </summary>
        public float CurrentAmount => _currentAmount;
        
        /// <summary>
        /// Total capacity of this resource node.
        /// </summary>
        public float TotalAmount => _totalAmount;
        
        /// <summary>
        /// How much resource is harvested per second per agent.
        /// </summary>
        public float HarvestRate => _harvestRate;
        
        /// <summary>
        /// Radius within which agents can harvest.
        /// </summary>
        public float HarvestRadius => _harvestRadius;
        
        /// <summary>
        /// Maximum number of agents that can harvest simultaneously.
        /// </summary>
        public int MaxHarvesters => _maxHarvesters;
        
        /// <summary>
        /// Number of agents currently harvesting.
        /// </summary>
        public int CurrentHarvesters => _activeHarvesters?.Count ?? 0;
        
        /// <summary>
        /// Whether this node is depleted (no resources left).
        /// </summary>
        public bool IsDepleted => _currentAmount <= 0f;
        
        /// <summary>
        /// Whether this node has available harvest slots.
        /// </summary>
        public bool HasCapacity => CurrentHarvesters < _maxHarvesters && !IsDepleted;
        
        /// <summary>
        /// Percentage of resources remaining (0-1).
        /// </summary>
        public float AmountPercent => _totalAmount > 0 ? _currentAmount / _totalAmount : 0f;
        
        /// <summary>
        /// World position of this resource node.
        /// </summary>
        public Vector3 Position => transform.position;
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Fired when an agent starts harvesting.
        /// </summary>
        public event System.Action<SwarmAgent> OnHarvestStarted;
        
        /// <summary>
        /// Fired when an agent stops harvesting.
        /// </summary>
        public event System.Action<SwarmAgent> OnHarvestStopped;
        
        /// <summary>
        /// Fired when resources are harvested (amount harvested).
        /// </summary>
        public event System.Action<float> OnResourceHarvested;
        
        /// <summary>
        /// Fired when the node is depleted.
        /// </summary>
        public event System.Action OnDepleted;
        
        /// <summary>
        /// Fired when the node respawns.
        /// </summary>
        public event System.Action OnRespawned;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            _currentAmount = _totalAmount;
            _initialAmount = _totalAmount;
            _initialScale = transform.localScale;
            _activeHarvesters = new HashSet<SwarmAgent>();
        }
        
        private void OnEnable()
        {
            if (!_allNodes.Contains(this))
            {
                _allNodes.Add(this);
            }
        }
        
        private void OnDisable()
        {
            _allNodes.Remove(this);
        }
        
        private void Update()
        {
            // Handle respawn
            if (_respawns && IsDepleted)
            {
                _respawnTimer += Time.deltaTime;
                if (_respawnTimer >= _respawnTime)
                {
                    Respawn();
                }
            }
            
            // Update visual scale
            if (_scaleWithAmount && !IsDepleted)
            {
                float scalePercent = Mathf.Lerp(_minScale, 1f, AmountPercent);
                transform.localScale = _initialScale * scalePercent;
            }
        }
        
        #endregion
        
        #region Harvesting
        
        /// <summary>
        /// Attempt to start harvesting this resource.
        /// </summary>
        /// <returns>True if harvesting can begin.</returns>
        public bool TryStartHarvesting(SwarmAgent agent)
        {
            if (agent == null || !HasCapacity) return false;
            
            // Check distance
            float distance = Vector3.Distance(agent.Position, Position);
            if (distance > _harvestRadius) return false;
            
            _activeHarvesters.Add(agent);
            OnHarvestStarted?.Invoke(agent);
            return true;
        }
        
        /// <summary>
        /// Stop harvesting this resource.
        /// </summary>
        public void StopHarvesting(SwarmAgent agent)
        {
            if (agent == null) return;
            
            if (_activeHarvesters.Remove(agent))
            {
                OnHarvestStopped?.Invoke(agent);
            }
        }
        
        /// <summary>
        /// Check if a specific agent is currently harvesting this node.
        /// </summary>
        public bool IsHarvesting(SwarmAgent agent)
        {
            return agent != null && _activeHarvesters.Contains(agent);
        }
        
        /// <summary>
        /// Harvest resources from this node.
        /// </summary>
        /// <param name="agent">The harvesting agent.</param>
        /// <param name="deltaTime">Time since last harvest call.</param>
        /// <returns>Amount harvested.</returns>
        public float Harvest(SwarmAgent agent, float deltaTime)
        {
            if (agent == null || IsDepleted) return 0f;
            
            // Check distance
            float distance = Vector3.Distance(agent.Position, Position);
            if (distance > _harvestRadius) return 0f;
            
            // Calculate harvest amount
            float harvestAmount = _harvestRate * deltaTime;
            harvestAmount = Mathf.Min(harvestAmount, _currentAmount);
            
            _currentAmount -= harvestAmount;
            
            OnResourceHarvested?.Invoke(harvestAmount);
            
            // Check if depleted
            if (_currentAmount <= 0f)
            {
                _currentAmount = 0f;
                OnDepleted?.Invoke();
                
                // Broadcast depletion message
                SwarmManager.Instance?.BroadcastMessage(
                    SwarmMessage.ResourceDepleted(Position, -1));
                
                if (_respawns)
                {
                    _respawnTimer = 0f;
                }
            }
            
            return harvestAmount;
        }
        
        /// <summary>
        /// Check if an agent is within harvest range.
        /// </summary>
        public bool IsInRange(SwarmAgent agent)
        {
            if (agent == null) return false;
            return Vector3.Distance(agent.Position, Position) <= _harvestRadius;
        }
        
        /// <summary>
        /// Refill the resource node.
        /// </summary>
        public void Refill(float amount)
        {
            _currentAmount = Mathf.Min(_currentAmount + amount, _totalAmount);
        }
        
        /// <summary>
        /// Deplete the resource node immediately (for testing/editor use).
        /// </summary>
        public void Deplete()
        {
            _currentAmount = 0f;
            
            // Update visual scale if enabled
            if (_scaleWithAmount)
            {
                transform.localScale = _initialScale * _minScale;
            }
            
            OnDepleted?.Invoke();
            
            if (_respawns)
            {
                _respawnTimer = 0f;
            }
        }
        
        /// <summary>
        /// Respawn the resource node to full.
        /// </summary>
        public void Respawn()
        {
            _currentAmount = _totalAmount;
            _respawnTimer = 0f;
            _activeHarvesters.Clear();
            
            if (_scaleWithAmount)
            {
                transform.localScale = _initialScale;
            }
            
            OnRespawned?.Invoke();
        }
        
        /// <summary>
        /// Configure the resource node settings programmatically.
        /// Call this after AddComponent but before the first Update.
        /// </summary>
        /// <param name="totalAmount">Total resource amount.</param>
        /// <param name="harvestRate">Harvest rate per second.</param>
        /// <param name="respawns">Whether the node respawns after depletion.</param>
        /// <param name="respawnTime">Time to respawn in seconds.</param>
        public void Configure(float totalAmount, float harvestRate, bool respawns = false, float respawnTime = 30f)
        {
            _totalAmount = totalAmount;
            _currentAmount = totalAmount;
            _initialAmount = totalAmount;
            _harvestRate = harvestRate;
            _respawns = respawns;
            _respawnTime = respawnTime;
        }
        
        #endregion
        
        #region Static Helpers
        
        /// <summary>
        /// Clean up any stale/destroyed entries from the registry.
        /// Called automatically before lookups to handle edge cases where OnDisable wasn't called.
        /// </summary>
        private static void CleanupStaleEntries()
        {
            _allNodes.RemoveAll(n => n == null);
        }
        
        /// <summary>
        /// Find the nearest resource node matching a predicate.
        /// </summary>
        private static ResourceNode FindNearestWhere(Vector3 position, System.Func<ResourceNode, bool> predicate, string resourceType, float maxDistance)
        {
            // Clean up any stale entries first (handles destroyed-while-disabled edge case)
            CleanupStaleEntries();
            
            ResourceNode nearest = null;
            float nearestDistSq = maxDistance * maxDistance;
            
            foreach (var node in _allNodes)
            {
                if (node == null || node.gameObject == null) continue;
                if (!predicate(node)) continue;
                if (resourceType != null && node.ResourceType != resourceType) continue;
                
                float distSq = (node.Position - position).sqrMagnitude;
                if (distSq < nearestDistSq)
                {
                    nearestDistSq = distSq;
                    nearest = node;
                }
            }
            
            return nearest;
        }
        
        /// <summary>
        /// Find the nearest resource node of a given type.
        /// </summary>
        public static ResourceNode FindNearest(Vector3 position, string resourceType = null, float maxDistance = float.MaxValue)
        {
            return FindNearestWhere(position, node => !node.IsDepleted, resourceType, maxDistance);
        }
        
        /// <summary>
        /// Find the nearest resource node with available capacity.
        /// </summary>
        public static ResourceNode FindNearestAvailable(Vector3 position, string resourceType = null, float maxDistance = float.MaxValue)
        {
            return FindNearestWhere(position, node => node.HasCapacity, resourceType, maxDistance);
        }
        
        /// <summary>
        /// Get all registered resource nodes (read-only).
        /// </summary>
        public static IReadOnlyList<ResourceNode> AllNodes => _allNodes;
        
        #endregion
        
        #region Debug
        
        private void OnDrawGizmos()
        {
            if (!_showDebugGizmos) return;
            
            Gizmos.color = IsDepleted ? _depletedColor : _availableColor;
            Gizmos.DrawWireSphere(transform.position, _harvestRadius);
        }
        
        private void OnDrawGizmosSelected()
        {
            // Show harvest radius
            Gizmos.color = new Color(0, 1, 0, 0.2f);
            Gizmos.DrawSphere(transform.position, _harvestRadius);
        }
        
        #endregion
    }
}
