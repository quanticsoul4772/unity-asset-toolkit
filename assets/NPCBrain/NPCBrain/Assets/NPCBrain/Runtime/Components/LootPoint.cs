using System;
using UnityEngine;

namespace NPCBrain.Components
{
    using NPCBrain; // For NPCBrainDebug

    /// <summary>
    /// A point of interest that can be stolen by RobberNPCs.
    /// Emits an alarm sound when stolen to alert nearby cops.
    /// </summary>
    public class LootPoint : MonoBehaviour
    {
        [Header("Loot Settings")]
        [SerializeField] private int _value = 100;
        [SerializeField] private float _stealTime = 2f;
        [SerializeField] private float _stealRadius = 2f;
        
        [Header("Alarm Settings")]
        [SerializeField] private bool _triggersAlarm = true;
        [SerializeField] private float _alarmRadius = 40f;
        
        private bool _isStolen;
        private GameObject _stolenBy;
        
        /// <summary>Value of this loot.</summary>
        public int Value => _value;
        
        /// <summary>Time required to steal this loot.</summary>
        public float StealTime => _stealTime;
        
        /// <summary>Distance within which stealing can occur.</summary>
        public float StealRadius => _stealRadius;
        
        /// <summary>Whether this loot has been stolen.</summary>
        public bool IsStolen => _isStolen;
        
        /// <summary>Who stole this loot (null if not stolen).</summary>
        public GameObject StolenBy => _stolenBy;
        
        /// <summary>Raised when this loot is stolen.</summary>
        public event Action<LootPoint, GameObject> OnStolen;
        
        /// <summary>
        /// Attempts to steal this loot.
        /// </summary>
        /// <param name="thief">The GameObject attempting to steal.</param>
        /// <returns>True if successfully stolen.</returns>
        public bool TrySteal(GameObject thief)
        {
            if (_isStolen) return false;
            
            float distance = Vector3.Distance(transform.position, thief.transform.position);
            if (distance > _stealRadius) return false;
            
            _isStolen = true;
            _stolenBy = thief;
            
            // Trigger alarm
            if (_triggersAlarm)
            {
                Perception.SoundManager.EmitAlarm(transform.position, 1f, gameObject);
                NPCBrainDebug.Log(NPCBrainDebug.Category.Hearing, $"[CopsAndRobbers] ALARM! Loot stolen at {transform.position}", this);
            }
            
            // Raise event
            OnStolen?.Invoke(this, thief);
            
            // Hide the loot visual
            var cachedRenderer = GetComponent<Renderer>();
            if (cachedRenderer != null)
            {
                cachedRenderer.enabled = false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Resets the loot to its initial state.
        /// </summary>
        public void Reset()
        {
            _isStolen = false;
            _stolenBy = null;
            
            var cachedRenderer = GetComponent<Renderer>();
            if (cachedRenderer != null)
            {
                cachedRenderer.enabled = true;
            }
        }
        
        /// <summary>
        /// Creates a loot point at the specified position.
        /// </summary>
        public static LootPoint Create(Vector3 position, int value, Transform parent = null)
        {
            var lootObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lootObj.name = $"Loot_{value}";
            lootObj.transform.position = position;
            lootObj.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            
            if (parent != null)
            {
                lootObj.transform.SetParent(parent);
            }
            
            // Gold color for loot
            var lootRenderer = lootObj.GetComponent<Renderer>();
            lootRenderer.material.color = new Color(1f, 0.84f, 0f);
            
            // Add sparkle indicator
            var sparkle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sparkle.name = "Sparkle";
            sparkle.transform.SetParent(lootObj.transform);
            sparkle.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            sparkle.transform.localScale = Vector3.one * 0.3f;
            var sparkleRenderer = sparkle.GetComponent<Renderer>();
            sparkleRenderer.material.color = Color.yellow;
            UnityEngine.Object.Destroy(sparkle.GetComponent<Collider>());
            
            var lootPoint = lootObj.AddComponent<LootPoint>();
            lootPoint._value = value;
            
            return lootPoint;
        }
        
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = _isStolen ? Color.gray : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _stealRadius);
            
            if (_triggersAlarm)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
                Gizmos.DrawWireSphere(transform.position, _alarmRadius);
            }
        }
    }
}
