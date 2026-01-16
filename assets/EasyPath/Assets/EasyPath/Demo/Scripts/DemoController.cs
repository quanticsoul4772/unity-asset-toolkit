using UnityEngine;
using EasyPath;

namespace EasyPath.Demo
{
    /// <summary>
    /// Demo controller for EasyPath demonstration scenes.
    /// </summary>
    public class DemoController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EasyPathGrid _grid;
        [SerializeField] private EasyPathAgent _agent;
        
        [Header("Demo Settings")]
        [SerializeField] private bool _autoFindReferences = true;
        [SerializeField] private KeyCode _rebuildGridKey = KeyCode.R;
        
        [Header("UI")]
        [SerializeField] private bool _showInstructions = true;
        [SerializeField] private Rect _uiPosition = new Rect(10, 10, 280, 220);
        
        private GUIStyle _boxStyle;
        private GUIStyle _headerStyle;
        
        private void Start()
        {
            if (_autoFindReferences)
            {
                if (_grid == null)
                {
                    _grid = FindFirstObjectByType<EasyPathGrid>();
                }
                if (_agent == null)
                {
                    _agent = FindFirstObjectByType<EasyPathAgent>();
                }
            }
            
            if (_grid == null)
            {
                Debug.LogError("[DemoController] No EasyPathGrid found!");
            }
            
            if (_agent == null)
            {
                Debug.LogError("[DemoController] No EasyPathAgent found!");
            }
        }
        
        private void Update()
        {
            // Rebuild grid on key press
            if (Input.GetKeyDown(_rebuildGridKey) && _grid != null)
            {
                _grid.BuildGrid();
                Debug.Log("[DemoController] Grid rebuilt!");
            }
        }
        
        private void OnGUI()
        {
            if (!_showInstructions)
            {
                return;
            }
            
            InitStyles();
            
            GUILayout.BeginArea(_uiPosition);
            GUILayout.BeginVertical(_boxStyle);
            
            GUILayout.Label("EasyPath Demo", _headerStyle);
            GUILayout.Space(5);
            
            GUILayout.Label("Controls:");
            GUILayout.Label("• Left Click - Set agent destination");
            GUILayout.Label("• Right Click - Spawn obstacle");
            GUILayout.Label("• Middle Click - Remove obstacle");
            GUILayout.Label($"• {_rebuildGridKey} - Rebuild grid");
            
            GUILayout.Space(10);
            
            if (_agent != null)
            {
                GUILayout.Label("Agent Status:");
                GUILayout.Label($"• Moving: {_agent.IsMoving}");
                GUILayout.Label($"• Distance: {_agent.RemainingDistance:F1}m");
            }
            
            if (_grid != null)
            {
                GUILayout.Space(5);
                GUILayout.Label($"Grid: {_grid.Width}x{_grid.Height} ({_grid.WalkableCount} walkable)");
            }
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        
        private void InitStyles()
        {
            if (_boxStyle == null)
            {
                _boxStyle = new GUIStyle(GUI.skin.box);
                _boxStyle.normal.background = MakeTexture(new Color(0, 0, 0, 0.7f));
                _boxStyle.padding = new RectOffset(10, 10, 10, 10);
            }
            
            if (_headerStyle == null)
            {
                _headerStyle = new GUIStyle(GUI.skin.label);
                _headerStyle.fontSize = 16;
                _headerStyle.fontStyle = FontStyle.Bold;
            }
        }
        
        private Texture2D MakeTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
    }
}
