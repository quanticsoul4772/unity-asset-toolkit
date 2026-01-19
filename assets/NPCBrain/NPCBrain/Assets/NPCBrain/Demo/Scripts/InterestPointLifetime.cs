using System.Collections.Generic;
using UnityEngine;
using NPCBrain.Archetypes;

namespace NPCBrain.Demo
{
    /// <summary>
    /// Tracks interest point lifetime and notifies NPCs when it expires.
    /// </summary>
    public class InterestPointLifetime : MonoBehaviour
    {
        public float Duration = 10f;
        public Vector3 Position;
        
        private float _spawnTime;
        private List<UtilityNPC> _registeredNPCs = new List<UtilityNPC>();
        
        // Cached squared threshold for position comparison (0.1f squared)
        private const float PositionMatchThresholdSqr = 0.01f;
        
        private void Start()
        {
            _spawnTime = Time.time;
        }
        
        /// <summary>
        /// Registers an NPC to be notified when this interest point expires.
        /// </summary>
        public void RegisterNPC(UtilityNPC npc)
        {
            if (npc != null && !_registeredNPCs.Contains(npc))
            {
                _registeredNPCs.Add(npc);
            }
        }
        
        private void Update()
        {
            if (Time.time - _spawnTime > Duration)
            {
                // Clear interest point from all registered NPCs before destroying
                // Use for loop and sqrMagnitude for better performance
                for (int i = 0; i < _registeredNPCs.Count; i++)
                {
                    var npc = _registeredNPCs[i];
                    if (npc != null)
                    {
                        // Only clear if the NPC's interest point matches this one
                        Vector3 npcInterest = npc.Blackboard.Get("interestPoint", Vector3.zero);
                        if ((npcInterest - Position).sqrMagnitude < PositionMatchThresholdSqr)
                        {
                            npc.ClearInterestPoint();
                        }
                    }
                }
                
                Destroy(gameObject);
                return;
            }
            
            // Pulse effect
            float pulse = 1f + Mathf.Sin(Time.time * 3f) * 0.1f;
            transform.localScale = Vector3.one * 0.6f * pulse;
            
            // Fade as lifetime expires
            float remaining = 1f - (Time.time - _spawnTime) / Duration;
            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                Color c = renderer.material.color;
                c.a = remaining;
                renderer.material.color = c;
            }
        }
    }
}
