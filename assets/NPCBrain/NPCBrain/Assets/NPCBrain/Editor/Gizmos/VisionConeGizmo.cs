using UnityEngine;
using UnityEditor;
using NPCBrain.Perception;

namespace NPCBrain.Editor.Gizmos
{
    /// <summary>
    /// Custom gizmo drawer for SightSensor vision cones.
    /// Draws vision cones in the scene view with alert state coloring.
    /// </summary>
    public static class VisionConeGizmo
    {
        private static readonly Color ClearColor = new Color(0.3f, 1f, 0.3f, 0.3f);
        private static readonly Color AlertColor = new Color(1f, 0.3f, 0.3f, 0.3f);
        private static readonly Color MemoryColor = new Color(1f, 1f, 0.3f, 0.3f);
        
        /// <summary>
        /// Draws a vision cone gizmo for a SightSensor.
        /// </summary>
        /// <param name="sensor">The sight sensor to draw.</param>
        /// <param name="memory">Optional memory system to visualize remembered positions.</param>
        public static void Draw(SightSensor sensor, Memory memory = null)
        {
            if (sensor == null) return;
            
            Transform transform = sensor.transform;
            Vector3 eyePosition = transform.position + Vector3.up * 1.5f; // Default eye height
            Vector3 forward = transform.forward;
            float viewDistance = sensor.ViewDistance;
            float viewAngle = sensor.ViewAngle;
            
            // Determine color based on state
            bool hasTargets = Application.isPlaying && sensor.HasVisibleTargets;
            Color coneColor = hasTargets ? AlertColor : ClearColor;
            
            // Draw the vision cone
            DrawCone(eyePosition, forward, viewDistance, viewAngle, coneColor);
            
            // Draw lines to visible targets
            if (Application.isPlaying)
            {
                UnityEngine.Gizmos.color = Color.red;
                foreach (var target in sensor.VisibleTargets)
                {
                    if (target != null)
                    {
                        UnityEngine.Gizmos.DrawLine(eyePosition, target.transform.position);
                        DrawTargetMarker(target.transform.position, 0.5f);
                    }
                }
                
                // Draw remembered positions
                if (memory != null)
                {
                    UnityEngine.Gizmos.color = MemoryColor;
                    foreach (var kvp in memory.Memories)
                    {
                        var mem = kvp.Value;
                        if (mem.Target != null && !mem.IsCurrentlyVisible)
                        {
                            UnityEngine.Gizmos.DrawLine(eyePosition, mem.LastKnownPosition);
                            DrawMemoryMarker(mem.LastKnownPosition, mem.Confidence);
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Draws a vision cone with the specified parameters.
        /// </summary>
        public static void DrawCone(Vector3 origin, Vector3 forward, float distance, float angle, Color color)
        {
            UnityEngine.Gizmos.color = color;
            
            float halfAngle = angle * 0.5f;
            Vector3 leftDir = Quaternion.Euler(0, -halfAngle, 0) * forward;
            Vector3 rightDir = Quaternion.Euler(0, halfAngle, 0) * forward;
            
            // Draw main rays
            UnityEngine.Gizmos.DrawRay(origin, forward * distance);
            UnityEngine.Gizmos.DrawRay(origin, leftDir * distance);
            UnityEngine.Gizmos.DrawRay(origin, rightDir * distance);
            
            // Draw arc at the end
            int segments = 20;
            float angleStep = angle / segments;
            Vector3 prevPoint = origin + leftDir * distance;
            
            for (int i = 1; i <= segments; i++)
            {
                float a = -halfAngle + angleStep * i;
                Vector3 dir = Quaternion.Euler(0, a, 0) * forward;
                Vector3 point = origin + dir * distance;
                UnityEngine.Gizmos.DrawLine(prevPoint, point);
                prevPoint = point;
            }
            
            // Draw filled mesh for better visibility
            DrawFilledCone(origin, forward, distance, angle, new Color(color.r, color.g, color.b, color.a * 0.3f));
        }
        
        private static void DrawFilledCone(Vector3 origin, Vector3 forward, float distance, float angle, Color color)
        {
            Handles.color = color;
            
            float halfAngle = angle * 0.5f;
            Vector3 leftDir = Quaternion.Euler(0, -halfAngle, 0) * forward;
            
            // Draw filled arc using Handles
            Handles.DrawSolidArc(origin, Vector3.up, leftDir, angle, distance);
        }
        
        private static void DrawTargetMarker(Vector3 position, float size)
        {
            UnityEngine.Gizmos.DrawWireSphere(position, size);
            UnityEngine.Gizmos.DrawLine(position - Vector3.right * size, position + Vector3.right * size);
            UnityEngine.Gizmos.DrawLine(position - Vector3.forward * size, position + Vector3.forward * size);
        }
        
        private static void DrawMemoryMarker(Vector3 position, float confidence)
        {
            float size = 0.3f + confidence * 0.3f;
            UnityEngine.Gizmos.DrawWireCube(position, Vector3.one * size);
            
            // Draw question mark above
            Vector3 above = position + Vector3.up * (size + 0.2f);
            Handles.Label(above, "?");
        }
    }
    
    /// <summary>
    /// Custom editor for SightSensor that draws gizmos.
    /// </summary>
    [CustomEditor(typeof(SightSensor))]
    public class SightSensorEditor : UnityEditor.Editor
    {
        private void OnSceneGUI()
        {
            SightSensor sensor = (SightSensor)target;
            if (sensor == null) return;
            
            // Draw handles for adjusting view distance and angle
            Handles.color = new Color(0.3f, 1f, 0.3f, 0.8f);
            
            Vector3 eyePos = sensor.transform.position + Vector3.up * 1.5f;
            Vector3 forward = sensor.transform.forward;
            float viewDist = sensor.ViewDistance;
            float viewAngle = sensor.ViewAngle;
            
            // Draw arc handle for view angle
            float halfAngle = viewAngle * 0.5f;
            Vector3 leftDir = Quaternion.Euler(0, -halfAngle, 0) * forward;
            Vector3 rightDir = Quaternion.Euler(0, halfAngle, 0) * forward;
            
            Handles.DrawWireArc(eyePos, Vector3.up, leftDir, viewAngle, viewDist);
            
            // Label with current values
            Handles.Label(eyePos + forward * viewDist, 
                $"View: {viewAngle}° / {viewDist}m");
        }
    }
}
