using System.Collections.Generic;
using UnityEngine;

namespace NPCBrain.Debug
{
    /// <summary>
    /// Visualizes NPC pathfinding data in the Scene view using Gizmos.
    /// Add this component to a GameObject in the scene to enable path visualization.
    /// NPCs register their paths via static methods, which are drawn each frame.
    /// </summary>
    public class NPCPathVisualizer : MonoBehaviour
    {
        [Header("Path Visualization")]
        [SerializeField] private bool _showPaths = true;
        [SerializeField] private float _pathLineWidth = 2f;
        [SerializeField] private float _waypointSize = 0.3f;
        [SerializeField] private float _currentWaypointSize = 0.5f;
        
        [Header("Colors")]
        [SerializeField] private Color _pathColor = new Color(0f, 1f, 1f, 0.8f);  // Cyan
        [SerializeField] private Color _completedPathColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);  // Gray
        [SerializeField] private Color _currentWaypointColor = new Color(1f, 1f, 0f, 1f);  // Yellow
        [SerializeField] private Color _targetColor = new Color(1f, 0.5f, 0f, 1f);  // Orange
        
        // Static instance for global access
        private static NPCPathVisualizer _instance;
        public static NPCPathVisualizer Instance => _instance;
        
        // Path data storage
        private static readonly Dictionary<string, NPCPathData> _npcPaths = new Dictionary<string, NPCPathData>();
        
        /// <summary>
        /// Data structure to hold an NPC's current path information.
        /// </summary>
        public class NPCPathData
        {
            public List<Vector3> Path;
            public int CurrentWaypointIndex;
            public Vector3 NpcPosition;
            public Vector3 TargetPosition;
            public Color PathColor;
            public float LastUpdateTime;
        }
        
        private void Awake()
        {
            _instance = this;
        }
        
        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
                _npcPaths.Clear();
            }
        }
        
        /// <summary>
        /// Enable or disable path visualization at runtime.
        /// </summary>
        public bool ShowPaths
        {
            get => _showPaths;
            set => _showPaths = value;
        }
        
        /// <summary>
        /// Register or update an NPC's path for visualization.
        /// Called by MoveTo when a path is calculated.
        /// </summary>
        public static void RegisterPath(string npcName, List<Vector3> path, int currentWaypointIndex, 
            Vector3 npcPosition, Vector3 targetPosition, Color? pathColor = null)
        {
            if (string.IsNullOrEmpty(npcName)) return;
            
            var data = new NPCPathData
            {
                Path = path != null ? new List<Vector3>(path) : null,
                CurrentWaypointIndex = currentWaypointIndex,
                NpcPosition = npcPosition,
                TargetPosition = targetPosition,
                PathColor = pathColor ?? (_instance != null ? _instance._pathColor : Color.cyan),
                LastUpdateTime = Time.time
            };
            
            _npcPaths[npcName] = data;
        }
        
        /// <summary>
        /// Update just the waypoint index and NPC position (called frequently during path following).
        /// </summary>
        public static void UpdatePathProgress(string npcName, int currentWaypointIndex, Vector3 npcPosition)
        {
            if (_npcPaths.TryGetValue(npcName, out var data))
            {
                data.CurrentWaypointIndex = currentWaypointIndex;
                data.NpcPosition = npcPosition;
                data.LastUpdateTime = Time.time;
            }
        }
        
        /// <summary>
        /// Remove an NPC's path from visualization.
        /// </summary>
        public static void UnregisterPath(string npcName)
        {
            if (!string.IsNullOrEmpty(npcName))
            {
                _npcPaths.Remove(npcName);
            }
        }
        
        /// <summary>
        /// Clear all registered paths.
        /// </summary>
        public static void ClearAllPaths()
        {
            _npcPaths.Clear();
        }
        
        /// <summary>
        /// Get the count of currently registered paths.
        /// </summary>
        public static int PathCount => _npcPaths.Count;
        
        private void OnDrawGizmos()
        {
            if (!_showPaths) return;
            
            // Clean up stale paths (not updated in 2 seconds)
            var staleNpcs = new List<string>();
            foreach (var kvp in _npcPaths)
            {
                if (Time.time - kvp.Value.LastUpdateTime > 2f)
                {
                    staleNpcs.Add(kvp.Key);
                }
            }
            foreach (var npc in staleNpcs)
            {
                _npcPaths.Remove(npc);
            }
            
            // Draw all registered paths
            foreach (var kvp in _npcPaths)
            {
                DrawNPCPath(kvp.Key, kvp.Value);
            }
        }
        
        private void DrawNPCPath(string npcName, NPCPathData data)
        {
            if (data.Path == null || data.Path.Count == 0) return;
            
            // Draw line from NPC to first uncompleted waypoint
            if (data.CurrentWaypointIndex < data.Path.Count)
            {
                Gizmos.color = data.PathColor;
                Vector3 npcPos = data.NpcPosition + Vector3.up * 0.2f;  // Slightly above ground
                Vector3 nextWaypoint = data.Path[data.CurrentWaypointIndex] + Vector3.up * 0.2f;
                Gizmos.DrawLine(npcPos, nextWaypoint);
            }
            
            // Draw completed path segments (grayed out)
            Gizmos.color = _completedPathColor;
            for (int i = 0; i < data.CurrentWaypointIndex && i < data.Path.Count - 1; i++)
            {
                Vector3 from = data.Path[i] + Vector3.up * 0.2f;
                Vector3 to = data.Path[i + 1] + Vector3.up * 0.2f;
                Gizmos.DrawLine(from, to);
            }
            
            // Draw remaining path segments
            Gizmos.color = data.PathColor;
            for (int i = data.CurrentWaypointIndex; i < data.Path.Count - 1; i++)
            {
                Vector3 from = data.Path[i] + Vector3.up * 0.2f;
                Vector3 to = data.Path[i + 1] + Vector3.up * 0.2f;
                Gizmos.DrawLine(from, to);
            }
            
            // Draw waypoint markers
            for (int i = 0; i < data.Path.Count; i++)
            {
                Vector3 waypoint = data.Path[i] + Vector3.up * 0.2f;
                
                if (i < data.CurrentWaypointIndex)
                {
                    // Completed waypoint - small gray
                    Gizmos.color = _completedPathColor;
                    Gizmos.DrawWireSphere(waypoint, _waypointSize * 0.5f);
                }
                else if (i == data.CurrentWaypointIndex)
                {
                    // Current target waypoint - large yellow
                    Gizmos.color = _currentWaypointColor;
                    Gizmos.DrawSphere(waypoint, _currentWaypointSize);
                    Gizmos.DrawWireSphere(waypoint, _currentWaypointSize * 1.2f);
                }
                else
                {
                    // Future waypoint - normal
                    Gizmos.color = data.PathColor;
                    Gizmos.DrawWireSphere(waypoint, _waypointSize);
                }
            }
            
            // Draw final target position marker
            Gizmos.color = _targetColor;
            Vector3 targetPos = data.TargetPosition + Vector3.up * 0.5f;
            Gizmos.DrawWireCube(targetPos, Vector3.one * 0.6f);
            
            // Draw a vertical line at target for visibility
            Gizmos.DrawLine(data.TargetPosition, targetPos + Vector3.up * 0.5f);
            
            // Draw NPC name label position (for reference)
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(data.NpcPosition + Vector3.up * 2f, npcName);
            #endif
        }
    }
}
