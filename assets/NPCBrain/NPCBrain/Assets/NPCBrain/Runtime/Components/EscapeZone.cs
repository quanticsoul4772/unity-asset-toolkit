using System;
using System.Collections.Generic;
using UnityEngine;

namespace NPCBrain.Components
{
    /// <summary>
    /// A zone where robbers can escape with stolen loot.
    /// Triggers victory when a robber with loot enters.
    /// </summary>
    public class EscapeZone : MonoBehaviour
    {
        [Header("Zone Settings")]
        [SerializeField] private float _zoneRadius = 5f;
        [SerializeField] private bool _requiresLoot = true;
        
        private HashSet<GameObject> _escapedRobbers = new HashSet<GameObject>();
        
        /// <summary>Radius of the escape zone.</summary>
        public float ZoneRadius => _zoneRadius;
        
        /// <summary>Whether robbers need loot to escape.</summary>
        public bool RequiresLoot => _requiresLoot;
        
        /// <summary>Number of robbers that have escaped.</summary>
        public int EscapedCount => _escapedRobbers.Count;
        
        /// <summary>Raised when a robber escapes.</summary>
        public event Action<GameObject, int> OnRobberEscaped;
        
        /// <summary>
        /// Checks if a robber can escape from this zone.
        /// </summary>
        /// <param name="robber">The robber attempting to escape.</param>
        /// <param name="lootValue">The value of loot the robber is carrying.</param>
        /// <returns>True if the robber successfully escaped.</returns>
        public bool TryEscape(GameObject robber, int lootValue)
        {
            if (_escapedRobbers.Contains(robber)) return false;
            
            float distance = Vector3.Distance(transform.position, robber.transform.position);
            if (distance > _zoneRadius) return false;
            
            if (_requiresLoot && lootValue <= 0) return false;
            
            _escapedRobbers.Add(robber);
            OnRobberEscaped?.Invoke(robber, lootValue);
            
            NPCBrainDebug.Log(NPCBrainDebug.Category.General, $"[CopsAndRobbers] Robber escaped with ${lootValue}!");
            
            return true;
        }
        
        /// <summary>
        /// Checks if a position is within the escape zone.
        /// </summary>
        public bool IsInZone(Vector3 position)
        {
            return Vector3.Distance(transform.position, position) <= _zoneRadius;
        }
        
        /// <summary>
        /// Resets the escape zone state.
        /// </summary>
        public void Reset()
        {
            _escapedRobbers.Clear();
        }
        
        /// <summary>
        /// Creates an escape zone at the specified position.
        /// </summary>
        public static EscapeZone Create(Vector3 position, float radius, Transform parent = null)
        {
            var zoneObj = new GameObject("EscapeZone");
            zoneObj.transform.position = position;
            
            if (parent != null)
            {
                zoneObj.transform.SetParent(parent);
            }
            
            // Visual indicator - flat circle
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.name = "ZoneVisual";
            visual.transform.SetParent(zoneObj.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = new Vector3(radius * 2f, 0.05f, radius * 2f);
            var visualRenderer = visual.GetComponent<Renderer>();
            visualRenderer.material.color = new Color(0f, 1f, 0f, 0.3f);
            UnityEngine.Object.Destroy(visual.GetComponent<Collider>());
            
            // Arrow indicator pointing up
            var arrow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arrow.name = "Arrow";
            arrow.transform.SetParent(zoneObj.transform);
            arrow.transform.localPosition = new Vector3(0f, 2f, 0f);
            arrow.transform.localScale = new Vector3(0.5f, 3f, 0.5f);
            var arrowRenderer = arrow.GetComponent<Renderer>();
            arrowRenderer.material.color = Color.green;
            UnityEngine.Object.Destroy(arrow.GetComponent<Collider>());
            
            var escapeZone = zoneObj.AddComponent<EscapeZone>();
            escapeZone._zoneRadius = radius;
            
            return escapeZone;
        }
        
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Gizmos.DrawSphere(transform.position, _zoneRadius);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, _zoneRadius);
        }
    }
}
