using System;
using UnityEngine;

namespace NPCBrain.Components
{
    /// <summary>
    /// A point of interest containing intelligence that can be collected by the player.
    /// Used in stealth gameplay scenarios.
    /// </summary>
    public class IntelPoint : MonoBehaviour
    {
        [Header("Intel Settings")]
        [SerializeField] private int _points = 100;
        [SerializeField] private float _collectRadius = 1.5f;
        [SerializeField] private float _collectTime = 1f;
        [SerializeField] private bool _isPrimary = false;
        
        [Header("Visual")]
        [SerializeField] private Color _primaryColor = new Color(1f, 0.2f, 0.2f);
        [SerializeField] private Color _secondaryColor = new Color(0.2f, 0.6f, 1f);
        
        private bool _isCollected;
        private GameObject _collectedBy;
        private Renderer _renderer;
        private GameObject _indicator;
        
        /// <summary>Point value of this intel.</summary>
        public int Points => _points;
        
        /// <summary>Distance within which collection can occur.</summary>
        public float CollectRadius => _collectRadius;
        
        /// <summary>Time required to collect this intel.</summary>
        public float CollectTime => _collectTime;
        
        /// <summary>Whether this is primary (mission-critical) intel.</summary>
        public bool IsPrimary => _isPrimary;
        
        /// <summary>Whether this intel has been collected.</summary>
        public bool IsCollected => _isCollected;
        
        /// <summary>Who collected this intel (null if not collected).</summary>
        public GameObject CollectedBy => _collectedBy;
        
        /// <summary>Raised when this intel is collected.</summary>
        public event Action<IntelPoint, GameObject> OnCollected;
        
        private void Awake()
        {
            _renderer = GetComponent<Renderer>();
        }
        
        /// <summary>
        /// Checks if a collector is in range to collect this intel.
        /// </summary>
        public bool IsInRange(GameObject collector)
        {
            if (_isCollected) return false;
            float distanceSqr = (transform.position - collector.transform.position).sqrMagnitude;
            return distanceSqr <= _collectRadius * _collectRadius;
        }
        
        /// <summary>
        /// Attempts to collect this intel.
        /// </summary>
        /// <param name="collector">The GameObject attempting to collect.</param>
        /// <returns>True if successfully collected.</returns>
        public bool TryCollect(GameObject collector)
        {
            if (_isCollected) return false;
            if (!IsInRange(collector)) return false;
            
            _isCollected = true;
            _collectedBy = collector;
            
            // Raise event
            OnCollected?.Invoke(this, collector);
            
            // Hide the intel visual
            if (_renderer != null)
            {
                _renderer.enabled = false;
            }
            if (_indicator != null)
            {
                _indicator.SetActive(false);
            }
            
            Debug.Log($"<color=cyan>[Stealth] Intel collected: {name} (+{_points} points)</color>");
            
            return true;
        }
        
        /// <summary>
        /// Resets the intel to its initial state.
        /// </summary>
        public void Reset()
        {
            _isCollected = false;
            _collectedBy = null;
            
            if (_renderer != null)
            {
                _renderer.enabled = true;
            }
            if (_indicator != null)
            {
                _indicator.SetActive(true);
            }
        }
        
        /// <summary>
        /// Creates an intel point at the specified position.
        /// </summary>
        public static IntelPoint Create(Vector3 position, int points, bool isPrimary, Transform parent = null)
        {
            var intelObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            intelObj.name = isPrimary ? $"PrimaryIntel_{points}" : $"Intel_{points}";
            intelObj.transform.position = position;
            intelObj.transform.localScale = new Vector3(0.4f, 0.1f, 0.4f);
            
            if (parent != null)
            {
                intelObj.transform.SetParent(parent);
            }
            
            // Color based on type
            Color color = isPrimary ? new Color(1f, 0.2f, 0.2f) : new Color(0.2f, 0.6f, 1f);
            intelObj.GetComponent<Renderer>().material.color = color;
            
            // Add floating indicator
            var indicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
            indicator.name = "Indicator";
            indicator.transform.SetParent(intelObj.transform);
            indicator.transform.localPosition = new Vector3(0f, 8f, 0f);
            indicator.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            indicator.GetComponent<Renderer>().material.color = color * 1.2f;
            UnityEngine.Object.Destroy(indicator.GetComponent<Collider>());
            
            // Add pulsing effect via rotation
            var rotator = indicator.AddComponent<IntelIndicatorRotator>();
            
            var intelPoint = intelObj.AddComponent<IntelPoint>();
            intelPoint._points = points;
            intelPoint._isPrimary = isPrimary;
            intelPoint._indicator = indicator;
            intelPoint._primaryColor = new Color(1f, 0.2f, 0.2f);
            intelPoint._secondaryColor = new Color(0.2f, 0.6f, 1f);
            
            return intelPoint;
        }
        
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = _isCollected ? Color.gray : (_isPrimary ? Color.red : Color.cyan);
            Gizmos.DrawWireSphere(transform.position, _collectRadius);
        }
    }
    
    /// <summary>
    /// Simple rotator for intel indicators.
    /// </summary>
    public class IntelIndicatorRotator : MonoBehaviour
    {
        private void Update()
        {
            transform.Rotate(0f, 90f * Time.deltaTime, 0f);
            float bob = Mathf.Sin(Time.time * 2f) * 0.1f;
            transform.localPosition = new Vector3(0f, 8f + bob, 0f);
        }
    }
}
