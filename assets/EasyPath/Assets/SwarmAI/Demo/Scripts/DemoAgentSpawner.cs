using UnityEngine;

namespace SwarmAI.Demo
{
    /// <summary>
    /// Helper component to spawn SwarmAgents at runtime.
    /// Useful for demos where agents need to be created dynamically.
    /// </summary>
    public class DemoAgentSpawner : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private int _spawnCount = 10;
        [SerializeField] private float _spawnRadius = 5f;
        [SerializeField] private bool _spawnOnStart = true;
        
        [Header("Visual Settings")]
        [SerializeField] private Color[] _agentColors = new Color[]
        {
            new Color(0.2f, 0.6f, 1f),
            new Color(1f, 0.4f, 0.4f),
            new Color(0.4f, 1f, 0.4f),
            new Color(1f, 0.8f, 0.2f),
            new Color(0.8f, 0.4f, 1f),
        };
        
        private void Start()
        {
            if (_spawnOnStart)
            {
                SpawnAgents(_spawnCount);
            }
        }
        
        /// <summary>
        /// Spawn agents around this object's position.
        /// </summary>
        public void SpawnAgents(int count)
        {
            for (int i = 0; i < count; i++)
            {
                SpawnAgent(i);
            }
            
            Debug.Log($"[DemoAgentSpawner] Spawned {count} agents");
        }
        
        /// <summary>
        /// Spawn a single agent.
        /// </summary>
        public SwarmAgent SpawnAgent(int index)
        {
            // Create agent object
            GameObject agentObj = new GameObject($"Agent_{index}");
            
            // Random position within spawn radius
            Vector3 randomOffset = Random.insideUnitSphere * _spawnRadius;
            randomOffset.y = 0;
            agentObj.transform.position = transform.position + randomOffset;
            
            // Add SwarmAgent component
            SwarmAgent agent = agentObj.AddComponent<SwarmAgent>();
            
            // Create visual
            Color color = _agentColors[index % _agentColors.Length];
            CreateAgentVisual(agentObj.transform, color);
            
            return agent;
        }
        
        private void CreateAgentVisual(Transform parent, Color color)
        {
            // Body
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(parent);
            body.transform.localPosition = Vector3.up * 0.5f;
            body.transform.localScale = new Vector3(0.5f, 0.4f, 0.5f);
            
            // Remove collider from visual
            var collider = body.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            
            // Set material
            var renderer = body.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = new Material(Shader.Find("Standard"));
                renderer.material.color = color;
            }
            
            // Direction indicator
            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            indicator.name = "DirectionIndicator";
            indicator.transform.SetParent(parent);
            indicator.transform.localPosition = new Vector3(0f, 0.5f, 0.35f);
            indicator.transform.localScale = new Vector3(0.15f, 0.15f, 0.25f);
            
            var indicatorCollider = indicator.GetComponent<Collider>();
            if (indicatorCollider != null) Destroy(indicatorCollider);
            
            var indicatorRenderer = indicator.GetComponent<Renderer>();
            if (indicatorRenderer != null)
            {
                indicatorRenderer.material = new Material(Shader.Find("Standard"));
                indicatorRenderer.material.color = Color.white;
            }
        }
        
        private void OnDrawGizmosSelected()
        {
            // Draw spawn radius
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawWireSphere(transform.position, _spawnRadius);
        }
    }
}
