using UnityEngine;
using System.Collections.Generic;

namespace SwarmAI.Demo
{
    /// <summary>
    /// Base controller for SwarmAI demonstration scenes.
    /// Provides common UI, agent management, and input handling.
    /// </summary>
    public class SwarmDemoController : MonoBehaviour
    {
        [Header("Demo Settings")]
        [SerializeField] protected string _demoTitle = "SwarmAI Demo";
        [SerializeField] protected bool _showInstructions = true;
        [SerializeField] protected bool _autoSpawnAgents = true;
        [SerializeField] protected int _agentCount = 20;
        
        [Header("Agent Prefab")]
        [SerializeField] protected GameObject _agentPrefab;
        
        [Header("Spawn Settings")]
        [SerializeField] protected Vector3 _spawnCenter = new Vector3(0, 0, 0);
        [SerializeField] protected float _spawnRadius = 5f;
        
        [Header("UI")]
        [SerializeField] protected Rect _uiPosition = new Rect(10, 10, 300, 400);
        
        // Agent tracking
        protected List<SwarmAgent> _agents = new List<SwarmAgent>();
        
        // UI styles
        protected GUIStyle _boxStyle;
        protected GUIStyle _headerStyle;
        protected GUIStyle _labelStyle;
        
        protected virtual void Start()
        {
            // Ensure SwarmManager exists
            var manager = SwarmManager.Instance;
            
            if (_autoSpawnAgents && _agentPrefab != null)
            {
                SpawnAgents(_agentCount);
            }
            else if (_autoSpawnAgents)
            {
                // Find existing agents
                var existingAgents = FindObjectsByType<SwarmAgent>(FindObjectsSortMode.None);
                _agents.AddRange(existingAgents);
            }
        }
        
        private float _cleanupTimer = 0f;
        private const float CleanupInterval = 1f;
        
        protected virtual void Update()
        {
            HandleCommonInput();
            
            // Periodic cleanup instead of every frame
            _cleanupTimer += Time.deltaTime;
            if (_cleanupTimer >= CleanupInterval)
            {
                _cleanupTimer = 0f;
                CleanupStaleAgents();
            }
        }
        
        /// <summary>
        /// Remove null entries from agents list (handles destroyed agents).
        /// </summary>
        protected void CleanupStaleAgents()
        {
            _agents.RemoveAll(a => a == null);
        }
        
        protected virtual void HandleCommonInput()
        {
            // R - Reset agents to spawn positions
            if (Input.GetKeyDown(KeyCode.R))
            {
                ResetAgents();
            }
            
            // Escape - Stop all agents
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                StopAllAgents();
            }
        }
        
        /// <summary>
        /// Spawn agents at random positions within spawn radius.
        /// </summary>
        public virtual void SpawnAgents(int count)
        {
            if (_agentPrefab == null)
            {
                Debug.LogError("[SwarmDemoController] No agent prefab assigned!");
                return;
            }
            
            for (int i = 0; i < count; i++)
            {
                Vector3 randomOffset = Random.insideUnitSphere * _spawnRadius;
                randomOffset.y = 0;
                Vector3 spawnPos = _spawnCenter + randomOffset;
                
                GameObject agentObj = Instantiate(_agentPrefab, spawnPos, Quaternion.identity);
                agentObj.name = $"Agent_{i}";
                
                SwarmAgent agent = agentObj.GetComponent<SwarmAgent>();
                if (agent != null)
                {
                    _agents.Add(agent);
                }
            }
            
            Debug.Log($"[SwarmDemoController] Spawned {count} agents");
        }
        
        /// <summary>
        /// Reset all agents to spawn area.
        /// </summary>
        public virtual void ResetAgents()
        {
            foreach (var agent in _agents)
            {
                if (agent == null) continue;
                
                Vector3 randomOffset = Random.insideUnitSphere * _spawnRadius;
                randomOffset.y = 0;
                agent.transform.position = _spawnCenter + randomOffset;
                agent.SetState(new IdleState());
            }
            
            Debug.Log("[SwarmDemoController] Agents reset");
        }
        
        /// <summary>
        /// Stop all agents.
        /// </summary>
        public virtual void StopAllAgents()
        {
            foreach (var agent in _agents)
            {
                if (agent == null) continue;
                agent.SetState(new IdleState());
            }
            
            Debug.Log("[SwarmDemoController] All agents stopped");
        }
        
        protected virtual void OnGUI()
        {
            if (!_showInstructions) return;
            
            InitStyles();
            
            GUILayout.BeginArea(_uiPosition);
            GUILayout.BeginVertical(_boxStyle);
            
            // Header
            GUILayout.Label(_demoTitle, _headerStyle);
            GUILayout.Space(10);
            
            // Common controls
            GUILayout.Label("Common Controls:", _labelStyle);
            GUILayout.Label("• R - Reset agents");
            GUILayout.Label("• ESC - Stop all agents");
            GUILayout.Space(10);
            
            // Demo-specific controls (override in subclass)
            DrawDemoControls();
            
            GUILayout.Space(10);
            
            // Stats
            DrawStats();
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        
        /// <summary>
        /// Override to draw demo-specific controls.
        /// </summary>
        protected virtual void DrawDemoControls()
        {
            // Override in subclass
        }
        
        /// <summary>
        /// Draw agent stats.
        /// </summary>
        protected virtual void DrawStats()
        {
            GUILayout.Label("Stats:", _labelStyle);
            GUILayout.Label($"• Agents: {_agents.Count}");
            
            int movingCount = 0;
            foreach (var agent in _agents)
            {
                if (agent != null && agent.Velocity.sqrMagnitude > 0.01f)
                    movingCount++;
            }
            GUILayout.Label($"• Moving: {movingCount}");
        }
        
        protected void InitStyles()
        {
            if (_boxStyle == null)
            {
                _boxStyle = new GUIStyle(GUI.skin.box);
                _boxStyle.normal.background = MakeTexture(new Color(0, 0, 0, 0.75f));
                _boxStyle.padding = new RectOffset(15, 15, 15, 15);
            }
            
            if (_headerStyle == null)
            {
                _headerStyle = new GUIStyle(GUI.skin.label);
                _headerStyle.fontSize = 18;
                _headerStyle.fontStyle = FontStyle.Bold;
                _headerStyle.normal.textColor = Color.white;
            }
            
            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label);
                _labelStyle.fontStyle = FontStyle.Bold;
                _labelStyle.normal.textColor = new Color(0.8f, 0.9f, 1f);
            }
        }
        
        protected Texture2D MakeTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
        
        /// <summary>
        /// Get random position on ground plane within bounds.
        /// </summary>
        protected Vector3 GetRandomPosition(float minX, float maxX, float minZ, float maxZ)
        {
            return new Vector3(
                Random.Range(minX, maxX),
                0f,
                Random.Range(minZ, maxZ)
            );
        }
    }
}
