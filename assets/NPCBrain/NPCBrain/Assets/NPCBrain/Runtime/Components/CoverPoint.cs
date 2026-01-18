using UnityEngine;

namespace NPCBrain.Components
{
    /// <summary>
    /// A point where robbers can hide from cops.
    /// Provides concealment from line-of-sight detection.
    /// </summary>
    public class CoverPoint : MonoBehaviour
    {
        [Header("Cover Settings")]
        [SerializeField] private float _hideRadius = 1.5f;
        [SerializeField] private float _hideDuration = 3f;
        
        private GameObject _occupant;
        private float _occupiedUntil;
        
        /// <summary>Radius within which hiding is effective.</summary>
        public float HideRadius => _hideRadius;
        
        /// <summary>How long hiding lasts.</summary>
        public float HideDuration => _hideDuration;
        
        /// <summary>Whether this cover point is currently occupied.</summary>
        public bool IsOccupied => _occupant != null && Time.time < _occupiedUntil;
        
        /// <summary>Who is currently hiding here.</summary>
        public GameObject Occupant => IsOccupied ? _occupant : null;
        
        /// <summary>Position to hide at.</summary>
        public Vector3 HidePosition => transform.position;
        
        /// <summary>
        /// Checks if a GameObject can hide at this point.
        /// </summary>
        public bool CanHide(GameObject hider)
        {
            if (IsOccupied && _occupant != hider) return false;
            
            float distance = Vector3.Distance(transform.position, hider.transform.position);
            return distance <= _hideRadius * 2f; // Can approach from 2x radius
        }
        
        /// <summary>
        /// Attempts to occupy this cover point.
        /// </summary>
        public bool TryHide(GameObject hider)
        {
            if (!CanHide(hider)) return false;
            
            _occupant = hider;
            _occupiedUntil = Time.time + _hideDuration;
            return true;
        }
        
        /// <summary>
        /// Releases this cover point.
        /// </summary>
        public void Release()
        {
            _occupant = null;
            _occupiedUntil = 0f;
        }
        
        /// <summary>
        /// Creates a cover point at the specified position.
        /// </summary>
        public static CoverPoint Create(Vector3 position, Transform parent = null)
        {
            // Create a visual obstacle that provides cover
            var coverObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            coverObj.name = "CoverPoint";
            coverObj.transform.position = position;
            coverObj.transform.localScale = new Vector3(2f, 2f, 2f);
            coverObj.GetComponent<Renderer>().material.color = new Color(0.3f, 0.3f, 0.35f);
            coverObj.isStatic = true;
            
            if (parent != null)
            {
                coverObj.transform.SetParent(parent);
            }
            
            var coverPoint = coverObj.AddComponent<CoverPoint>();
            return coverPoint;
        }
        
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = IsOccupied ? Color.red : Color.cyan;
            Gizmos.DrawWireSphere(transform.position, _hideRadius);
        }
    }
}
