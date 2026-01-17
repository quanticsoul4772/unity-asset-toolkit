using UnityEngine;
using UnityEditor;
using NPCBrain.Perception;

namespace NPCBrain.Editor.Gizmos
{
    /// <summary>
    /// Custom editor that draws vision cone gizmos for SightSensor components.
    /// </summary>
    [CustomEditor(typeof(SightSensor))]
    public class SightSensorEditor : UnityEditor.Editor
    {
        private void OnSceneGUI()
        {
            var sensor = (SightSensor)target;
            if (sensor == null) return;
            
            DrawVisionCone(sensor);
        }
        
        private void DrawVisionCone(SightSensor sensor)
        {
            Transform transform = sensor.transform;
            Vector3 position = transform.position + Vector3.up * 0.5f; // Eye height offset
            Vector3 forward = transform.forward;
            
            float viewDistance = sensor.ViewDistance;
            float viewAngle = sensor.ViewAngle;
            
            // Determine color based on whether targets are detected
            var visibleTargets = sensor.VisibleTargets;
            Color coneColor = visibleTargets != null && visibleTargets.Count > 0 
                ? new Color(1f, 0.3f, 0.3f, 0.3f)  // Red when detecting
                : new Color(0.3f, 1f, 0.3f, 0.3f); // Green when clear
            
            Color wireColor = visibleTargets != null && visibleTargets.Count > 0
                ? new Color(1f, 0.3f, 0.3f, 0.8f)
                : new Color(0.3f, 1f, 0.3f, 0.8f);
            
            // Draw the vision cone
            Handles.color = coneColor;
            
            // Calculate cone edges
            float halfAngle = viewAngle * 0.5f;
            Vector3 leftDir = Quaternion.Euler(0, -halfAngle, 0) * forward;
            Vector3 rightDir = Quaternion.Euler(0, halfAngle, 0) * forward;
            
            Vector3 leftPoint = position + leftDir * viewDistance;
            Vector3 rightPoint = position + rightDir * viewDistance;
            
            // Draw filled arc
            Handles.DrawSolidArc(position, Vector3.up, leftDir, viewAngle, viewDistance);
            
            // Draw wire outline
            Handles.color = wireColor;
            Handles.DrawWireArc(position, Vector3.up, leftDir, viewAngle, viewDistance);
            
            // Draw edge lines
            Handles.DrawLine(position, leftPoint);
            Handles.DrawLine(position, rightPoint);
            
            // Draw forward direction
            Handles.color = Color.blue;
            Handles.DrawLine(position, position + forward * viewDistance * 0.5f);
            
            // Draw range indicator
            Handles.color = new Color(1f, 1f, 1f, 0.3f);
            Handles.DrawWireDisc(position, Vector3.up, viewDistance);
            
            // Draw label
            Handles.Label(position + Vector3.up * 0.3f, 
                $"FOV: {viewAngle}° | Range: {viewDistance}m",
                new GUIStyle { normal = { textColor = Color.white }, fontSize = 10 });
            
            // Draw lines to visible targets
            if (visibleTargets != null)
            {
                Handles.color = Color.red;
                foreach (var target in visibleTargets)
                {
                    if (target != null)
                    {
                        Handles.DrawDottedLine(position, target.transform.position, 4f);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Static class for drawing vision cone gizmos without a custom editor.
    /// Can be called from OnDrawGizmos in any MonoBehaviour.
    /// </summary>
    public static class VisionConeGizmo
    {
        /// <summary>
        /// Draws a vision cone gizmo for the given sensor.
        /// </summary>
        public static void Draw(SightSensor sensor)
        {
            if (sensor == null) return;
            
            Transform transform = sensor.transform;
            Vector3 position = transform.position + Vector3.up * 0.5f;
            Vector3 forward = transform.forward;
            
            float viewDistance = sensor.ViewDistance;
            float viewAngle = sensor.ViewAngle;
            float halfAngle = viewAngle * 0.5f;
            
            // Determine color
            var visibleTargets = sensor.VisibleTargets;
            bool hasTargets = visibleTargets != null && visibleTargets.Count > 0;
            Gizmos.color = hasTargets 
                ? new Color(1f, 0.3f, 0.3f, 0.5f) 
                : new Color(0.3f, 1f, 0.3f, 0.5f);
            
            // Draw cone edges
            Vector3 leftDir = Quaternion.Euler(0, -halfAngle, 0) * forward;
            Vector3 rightDir = Quaternion.Euler(0, halfAngle, 0) * forward;
            
            Gizmos.DrawRay(position, leftDir * viewDistance);
            Gizmos.DrawRay(position, rightDir * viewDistance);
            Gizmos.DrawRay(position, forward * viewDistance);
            
            // Draw arc segments
            int segments = 20;
            float angleStep = viewAngle / segments;
            Vector3 prevPoint = position + leftDir * viewDistance;
            
            for (int i = 1; i <= segments; i++)
            {
                float angle = -halfAngle + angleStep * i;
                Vector3 dir = Quaternion.Euler(0, angle, 0) * forward;
                Vector3 point = position + dir * viewDistance;
                Gizmos.DrawLine(prevPoint, point);
                prevPoint = point;
            }
            
            // Draw range circle
            Gizmos.color = new Color(1f, 1f, 1f, 0.2f);
            DrawWireCircle(position, viewDistance, 32);
        }
        
        private static void DrawWireCircle(Vector3 center, float radius, int segments)
        {
            float angleStep = 360f / segments;
            Vector3 prevPoint = center + Vector3.forward * radius;
            
            for (int i = 1; i <= segments; i++)
            {
                float angle = angleStep * i;
                Vector3 dir = Quaternion.Euler(0, angle, 0) * Vector3.forward;
                Vector3 point = center + dir * radius;
                Gizmos.DrawLine(prevPoint, point);
                prevPoint = point;
            }
        }
    }
}
