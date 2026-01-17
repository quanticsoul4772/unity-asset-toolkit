using UnityEngine;

namespace SwarmAI.Jobs
{
    /// <summary>
    /// Benchmark utility for measuring Jobs system performance.
    /// Add to scene with SwarmManager to display performance comparison.
    /// </summary>
    public class JobsBenchmark : MonoBehaviour
    {
        [Header("Benchmark Settings")]
        [SerializeField] private bool _showBenchmark = true;
        [SerializeField] private int _sampleCount = 60;
        
        private SwarmManager _swarmManager;
        private SwarmJobSystem _jobSystem;
        
        private float[] _frameTimes;
        private int _frameIndex;
        private float _avgFrameTime;
        private float _minFrameTime;
        private float _maxFrameTime;
        
        private void Awake()
        {
            _swarmManager = GetComponent<SwarmManager>();
            _jobSystem = GetComponent<SwarmJobSystem>();
            _frameTimes = new float[_sampleCount];
        }
        
        private void Update()
        {
            if (!_showBenchmark) return;
            
            // Record frame time
            _frameTimes[_frameIndex] = Time.deltaTime * 1000f;
            _frameIndex = (_frameIndex + 1) % _sampleCount;
            
            // Calculate stats
            float sum = 0f;
            _minFrameTime = float.MaxValue;
            _maxFrameTime = 0f;
            
            for (int i = 0; i < _sampleCount; i++)
            {
                float t = _frameTimes[i];
                sum += t;
                if (t > 0 && t < _minFrameTime) _minFrameTime = t;
                if (t > _maxFrameTime) _maxFrameTime = t;
            }
            
            _avgFrameTime = sum / _sampleCount;
        }
        
        private void OnGUI()
        {
            if (!_showBenchmark) return;
            
            int agentCount = _swarmManager != null ? _swarmManager.AgentCount : 0;
            bool jobsActive = _jobSystem != null && _jobSystem.IsUsingJobs;
            float jobTime = _jobSystem != null ? _jobSystem.LastJobTimeMs : 0f;
            
            GUILayout.BeginArea(new Rect(Screen.width - 260, 10, 250, 200));
            
            GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.normal.background = MakeTexture(2, 2, new Color(0f, 0f, 0f, 0.7f));
            
            GUILayout.BeginVertical(boxStyle);
            
            GUILayout.Label("<b>SwarmAI Benchmark</b>", new GUIStyle(GUI.skin.label) { richText = true });
            GUILayout.Space(5);
            
            GUILayout.Label($"Agents: {agentCount}");
            GUILayout.Label($"Jobs Active: {(jobsActive ? "<color=green>Yes</color>" : "<color=yellow>No</color>")}", 
                new GUIStyle(GUI.skin.label) { richText = true });
            
            GUILayout.Space(5);
            GUILayout.Label("Frame Time:");
            GUILayout.Label($"  Avg: {_avgFrameTime:F2} ms ({1000f / _avgFrameTime:F0} FPS)");
            GUILayout.Label($"  Min: {_minFrameTime:F2} ms");
            GUILayout.Label($"  Max: {_maxFrameTime:F2} ms");
            
            if (jobsActive)
            {
                GUILayout.Space(5);
                GUILayout.Label($"Jobs Time: {jobTime:F2} ms");
                float jobPercent = _avgFrameTime > 0 ? (jobTime / _avgFrameTime) * 100f : 0f;
                GUILayout.Label($"Jobs % of Frame: {jobPercent:F1}%");
            }
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        
        private Texture2D MakeTexture(int width, int height, Color color)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
            {
                pix[i] = color;
            }
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
    }
}
