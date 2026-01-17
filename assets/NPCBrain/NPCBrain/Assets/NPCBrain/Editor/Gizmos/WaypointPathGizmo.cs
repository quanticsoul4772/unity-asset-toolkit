using UnityEngine;
using UnityEditor;

namespace NPCBrain.Editor.Gizmos
{
    /// <summary>
    /// Custom editor that draws waypoint path gizmos.
    /// </summary>
    [CustomEditor(typeof(WaypointPath))]
    public class WaypointPathEditor : UnityEditor.Editor
    {
        private void OnSceneGUI()
        {
            var path = (WaypointPath)target;
            if (path == null) return;
            
            DrawWaypointPath(path);
        }
        
        private void DrawWaypointPath(WaypointPath path)
        {
            var waypoints = path.Waypoints;
            if (waypoints == null || waypoints.Count == 0) return;
            
            Handles.color = new Color(0.2f, 0.8f, 1f, 0.8f);
            
            // Draw lines between waypoints
            for (int i = 0; i < waypoints.Count; i++)
            {
                var current = waypoints[i];
                var next = waypoints[(i + 1) % waypoints.Count];
                
                if (current == null || next == null) continue;
                
                // Draw line
                Handles.DrawLine(current.position, next.position);
                
                // Draw direction arrow
                Vector3 direction = (next.position - current.position).normalized;
                Vector3 midPoint = Vector3.Lerp(current.position, next.position, 0.5f);
                float arrowSize = 0.5f;
                Handles.ConeHandleCap(0, midPoint, Quaternion.LookRotation(direction), arrowSize, EventType.Repaint);
            }
            
            // Draw waypoint markers
            for (int i = 0; i < waypoints.Count; i++)
            {
                var waypoint = waypoints[i];
                if (waypoint == null) continue;
                
                // Highlight current waypoint
                bool isCurrent = Application.isPlaying && i == path.CurrentIndex;
                Handles.color = isCurrent 
                    ? new Color(1f, 0.8f, 0.2f, 1f)  // Yellow for current
                    : new Color(0.2f, 0.8f, 1f, 0.8f); // Cyan for others
                
                float size = isCurrent ? 0.4f : 0.3f;
                Handles.SphereHandleCap(0, waypoint.position, Quaternion.identity, size, EventType.Repaint);
                
                // Draw index label
                Handles.Label(waypoint.position + Vector3.up * 0.5f, 
                    $"[{i}]", 
                    new GUIStyle { normal = { textColor = Color.white }, fontSize = 12, fontStyle = FontStyle.Bold });
            }
        }
    }
}
