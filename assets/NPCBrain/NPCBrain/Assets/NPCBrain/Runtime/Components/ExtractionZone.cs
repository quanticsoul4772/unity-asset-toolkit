using System;
using UnityEngine;

namespace NPCBrain.Components
{
    /// <summary>
    /// A zone where the player can extract after collecting intel.
    /// Used in stealth gameplay scenarios.
    /// </summary>
    public class ExtractionZone : MonoBehaviour
    {
        [Header("Zone Settings")]
        [SerializeField] private float _zoneRadius = 3f;
        [SerializeField] private bool _requiresPrimaryIntel = true;
        [SerializeField] private int _minimumIntelRequired = 1;
        
        private bool _isActive = true;
        private bool _extractionComplete;
        
        /// <summary>Radius of the extraction zone.</summary>
        public float ZoneRadius => _zoneRadius;
        
        /// <summary>Whether primary intel is required to extract.</summary>
        public bool RequiresPrimaryIntel => _requiresPrimaryIntel;
        
        /// <summary>Minimum intel items required to extract.</summary>
        public int MinimumIntelRequired => _minimumIntelRequired;
        
        /// <summary>Whether extraction has been completed.</summary>
        public bool ExtractionComplete => _extractionComplete;
        
        /// <summary>Whether the zone is currently active.</summary>
        public bool IsActive => _isActive;
        
        /// <summary>Raised when extraction is successful.</summary>
        public event Action<GameObject, int> OnExtraction;
        
        /// <summary>
        /// Checks if a position is within the extraction zone.
        /// </summary>
        public bool IsInZone(Vector3 position)
        {
            float distSqr = (transform.position - position).sqrMagnitude;
            return distSqr <= _zoneRadius * _zoneRadius;
        }
        
        /// <summary>
        /// Attempts to extract from this zone.
        /// </summary>
        /// <param name="extractor">The GameObject attempting to extract.</param>
        /// <param name="intelCount">Number of intel items collected.</param>
        /// <param name="hasPrimaryIntel">Whether primary intel was collected.</param>
        /// <param name="totalPoints">Total points from collected intel.</param>
        /// <returns>True if extraction successful.</returns>
        public bool TryExtract(GameObject extractor, int intelCount, bool hasPrimaryIntel, int totalPoints)
        {
            if (_extractionComplete) return false;
            if (!_isActive) return false;
            if (!IsInZone(extractor.transform.position)) return false;
            
            if (_requiresPrimaryIntel && !hasPrimaryIntel)
            {
                Debug.Log("<color=red>[Stealth] Cannot extract - Primary intel required!</color>");
                return false;
            }
            
            if (intelCount < _minimumIntelRequired)
            {
                Debug.Log($"<color=red>[Stealth] Cannot extract - Need at least {_minimumIntelRequired} intel!</color>");
                return false;
            }
            
            _extractionComplete = true;
            OnExtraction?.Invoke(extractor, totalPoints);
            
            Debug.Log($"<color=green>[Stealth] EXTRACTION SUCCESSFUL! Total points: {totalPoints}</color>");
            
            return true;
        }
        
        /// <summary>
        /// Sets whether the extraction zone is active.
        /// </summary>
        public void SetActive(bool active)
        {
            _isActive = active;
        }
        
        /// <summary>
        /// Resets the extraction zone state.
        /// </summary>
        public void Reset()
        {
            _extractionComplete = false;
            _isActive = true;
        }
        
        /// <summary>
        /// Creates an extraction zone at the specified position.
        /// </summary>
        public static ExtractionZone Create(Vector3 position, float radius, bool requiresPrimary, int minIntel, Transform parent = null)
        {
            var zoneObj = new GameObject("ExtractionZone");
            zoneObj.transform.position = position;
            
            if (parent != null)
            {
                zoneObj.transform.SetParent(parent);
            }
            
            // Visual indicator - flat circle (cyan/teal color for extraction)
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.name = "ZoneVisual";
            visual.transform.SetParent(zoneObj.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = new Vector3(radius * 2f, 0.05f, radius * 2f);
            visual.GetComponent<Renderer>().material.color = new Color(0f, 0.8f, 0.8f, 0.4f);
            UnityEngine.Object.Destroy(visual.GetComponent<Collider>());
            
            // Helicopter/extraction marker
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            marker.name = "Marker";
            marker.transform.SetParent(zoneObj.transform);
            marker.transform.localPosition = new Vector3(0f, 0.1f, 0f);
            marker.transform.localScale = new Vector3(radius * 0.8f, 0.1f, radius * 0.8f);
            marker.GetComponent<Renderer>().material.color = new Color(0f, 0.6f, 0.6f, 0.6f);
            UnityEngine.Object.Destroy(marker.GetComponent<Collider>());
            
            // "H" pattern for helicopter landing
            CreateHPattern(zoneObj.transform, radius);
            
            var extractionZone = zoneObj.AddComponent<ExtractionZone>();
            extractionZone._zoneRadius = radius;
            extractionZone._requiresPrimaryIntel = requiresPrimary;
            extractionZone._minimumIntelRequired = minIntel;
            
            return extractionZone;
        }
        
        private static void CreateHPattern(Transform parent, float radius)
        {
            Color hColor = new Color(1f, 1f, 1f, 0.8f);
            float scale = radius * 0.15f;
            
            // Left vertical
            var left = GameObject.CreatePrimitive(PrimitiveType.Cube);
            left.name = "H_Left";
            left.transform.SetParent(parent);
            left.transform.localPosition = new Vector3(-scale, 0.15f, 0f);
            left.transform.localScale = new Vector3(scale * 0.3f, 0.05f, scale * 2f);
            left.GetComponent<Renderer>().material.color = hColor;
            UnityEngine.Object.Destroy(left.GetComponent<Collider>());
            
            // Right vertical
            var right = GameObject.CreatePrimitive(PrimitiveType.Cube);
            right.name = "H_Right";
            right.transform.SetParent(parent);
            right.transform.localPosition = new Vector3(scale, 0.15f, 0f);
            right.transform.localScale = new Vector3(scale * 0.3f, 0.05f, scale * 2f);
            right.GetComponent<Renderer>().material.color = hColor;
            UnityEngine.Object.Destroy(right.GetComponent<Collider>());
            
            // Horizontal bar
            var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.name = "H_Bar";
            bar.transform.SetParent(parent);
            bar.transform.localPosition = new Vector3(0f, 0.15f, 0f);
            bar.transform.localScale = new Vector3(scale * 2.3f, 0.05f, scale * 0.3f);
            bar.GetComponent<Renderer>().material.color = hColor;
            UnityEngine.Object.Destroy(bar.GetComponent<Collider>());
        }
        
        private void OnDrawGizmos()
        {
            Color zoneColor = _isActive ? new Color(0f, 0.8f, 0.8f, 0.3f) : new Color(0.5f, 0.5f, 0.5f, 0.3f);
            Gizmos.color = zoneColor;
            Gizmos.DrawSphere(transform.position, _zoneRadius);
            Gizmos.color = _isActive ? Color.cyan : Color.gray;
            Gizmos.DrawWireSphere(transform.position, _zoneRadius);
        }
    }
}
