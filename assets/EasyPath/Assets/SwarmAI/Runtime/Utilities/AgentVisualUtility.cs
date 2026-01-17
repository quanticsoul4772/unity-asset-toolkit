using UnityEngine;

namespace SwarmAI
{
    /// <summary>
    /// Shared utility for creating agent visuals and managing agent colors.
    /// Used by demo scenes and runtime agent spawning.
    /// </summary>
    public static class AgentVisualUtility
    {
        /// <summary>
        /// Standard agent colors for visual variety.
        /// </summary>
        public static readonly Color[] AgentColors = new Color[]
        {
            new Color(0.2f, 0.6f, 1f),   // Blue
            new Color(1f, 0.4f, 0.4f),   // Red
            new Color(0.4f, 1f, 0.4f),   // Green
            new Color(1f, 0.8f, 0.2f),   // Yellow
            new Color(0.8f, 0.4f, 1f),   // Purple
            new Color(1f, 0.6f, 0.4f),   // Orange
            new Color(0.4f, 0.8f, 0.8f), // Cyan
            new Color(0.8f, 0.8f, 0.4f), // Lime
        };

        /// <summary>
        /// Get a color for an agent by index, cycling through the palette.
        /// </summary>
        public static Color GetAgentColor(int index)
        {
            return AgentColors[index % AgentColors.Length];
        }

        /// <summary>
        /// Create a visual representation for an agent (capsule body + direction indicator).
        /// </summary>
        /// <param name="parent">Parent transform for the visual</param>
        /// <param name="color">Color for the agent body</param>
        public static void CreateAgentVisual(Transform parent, Color color)
        {
            // Body (capsule)
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(parent);
            body.transform.localPosition = Vector3.up * 0.5f;
            body.transform.localScale = new Vector3(0.5f, 0.4f, 0.5f);

            var collider = body.GetComponent<Collider>();
            if (collider != null)
            {
                // Use DestroyImmediate in Editor, Destroy at runtime
                if (Application.isPlaying)
                    Object.Destroy(collider);
                else
                    Object.DestroyImmediate(collider);
            }

            var renderer = body.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = CreateMaterial(color);
            }

            // Direction indicator (cone-like using scaled sphere)
            GameObject indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            indicator.name = "DirectionIndicator";
            indicator.transform.SetParent(parent);
            indicator.transform.localPosition = new Vector3(0f, 0.5f, 0.35f);
            indicator.transform.localScale = new Vector3(0.15f, 0.15f, 0.25f);

            var indicatorCollider = indicator.GetComponent<Collider>();
            if (indicatorCollider != null)
            {
                if (Application.isPlaying)
                    Object.Destroy(indicatorCollider);
                else
                    Object.DestroyImmediate(indicatorCollider);
            }

            var indicatorRenderer = indicator.GetComponent<Renderer>();
            if (indicatorRenderer != null)
            {
                indicatorRenderer.material = CreateMaterial(Color.white);
            }
        }

        /// <summary>
        /// Create a material with the specified color.
        /// Automatically selects appropriate shader for current render pipeline.
        /// </summary>
        public static Material CreateMaterial(Color color)
        {
            // Try to find an appropriate shader based on render pipeline
            Shader shader = Shader.Find("Standard");
            
            // If Standard shader not found (URP/HDRP), try alternatives
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Lit");
            }
            if (shader == null)
            {
                shader = Shader.Find("HDRP/Lit");
            }
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }
            
            if (shader == null)
            {
                Debug.LogWarning("[SwarmAI] Could not find suitable shader. Materials may appear incorrect.");
                shader = Shader.Find("Hidden/InternalErrorShader");
            }
            
            Material mat = new Material(shader);
            mat.color = color;
            return mat;
        }
    }
}
