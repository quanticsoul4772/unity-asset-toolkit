using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using NPCBrain.BehaviorTree;
using NPCBrain.BehaviorTree.Composites;

namespace NPCBrain.Editor
{
    /// <summary>
    /// Debug window for inspecting NPC behavior at runtime.
    /// Provides NPC selection, state display, blackboard viewer, and pause/step controls.
    /// </summary>
    public class NPCBrainDebugWindow : EditorWindow
    {
        private NPCBrainController _selectedNPC;
        private Vector2 _blackboardScrollPos;
        private Vector2 _mainScrollPos;
        private bool _showBlackboard = true;
        private bool _showCriticality = true;
        private bool _showBehaviorTree = true;
        private bool _autoRefresh = true;
        private double _lastRefreshTime;
        private const double RefreshInterval = 0.1; // 100ms
        
        private static GUIStyle _headerStyle;
        private static GUIStyle _boxStyle;
        private static GUIStyle _statusRunningStyle;
        private static GUIStyle _statusSuccessStyle;
        private static GUIStyle _statusFailureStyle;
        
        [MenuItem("Window/NPCBrain/Debug Window")]
        public static void ShowWindow()
        {
            var window = GetWindow<NPCBrainDebugWindow>("NPCBrain Debug");
            window.minSize = new Vector2(350, 400);
        }
        
        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }
        
        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        }
        
        private void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                _selectedNPC = null;
            }
            Repaint();
        }
        
        private void Update()
        {
            if (_autoRefresh && EditorApplication.isPlaying && _selectedNPC != null)
            {
                if (EditorApplication.timeSinceStartup - _lastRefreshTime > RefreshInterval)
                {
                    _lastRefreshTime = EditorApplication.timeSinceStartup;
                    Repaint();
                }
            }
        }
        
        private void InitStyles()
        {
            if (_headerStyle == null)
            {
                _headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 14,
                    margin = new RectOffset(0, 0, 10, 5)
                };
            }
            
            if (_boxStyle == null)
            {
                _boxStyle = new GUIStyle("box")
                {
                    padding = new RectOffset(10, 10, 10, 10),
                    margin = new RectOffset(0, 0, 5, 5)
                };
            }
            
            if (_statusRunningStyle == null)
            {
                _statusRunningStyle = new GUIStyle(EditorStyles.label)
                {
                    normal = { textColor = new Color(1f, 0.8f, 0f) },
                    fontStyle = FontStyle.Bold
                };
            }
            
            if (_statusSuccessStyle == null)
            {
                _statusSuccessStyle = new GUIStyle(EditorStyles.label)
                {
                    normal = { textColor = new Color(0.2f, 0.8f, 0.2f) },
                    fontStyle = FontStyle.Bold
                };
            }
            
            if (_statusFailureStyle == null)
            {
                _statusFailureStyle = new GUIStyle(EditorStyles.label)
                {
                    normal = { textColor = new Color(0.9f, 0.3f, 0.3f) },
                    fontStyle = FontStyle.Bold
                };
            }
        }
        
        private void OnGUI()
        {
            InitStyles();
            
            _mainScrollPos = EditorGUILayout.BeginScrollView(_mainScrollPos);
            
            DrawHeader();
            EditorGUILayout.Space(5);
            
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to debug NPCs.", MessageType.Info);
                EditorGUILayout.EndScrollView();
                return;
            }
            
            DrawNPCSelector();
            EditorGUILayout.Space(10);
            
            if (_selectedNPC == null)
            {
                EditorGUILayout.HelpBox("Select an NPC to inspect.", MessageType.Info);
                EditorGUILayout.EndScrollView();
                return;
            }
            
            DrawControls();
            EditorGUILayout.Space(10);
            
            DrawStateDisplay();
            EditorGUILayout.Space(5);
            
            DrawCriticalitySection();
            EditorGUILayout.Space(5);
            
            DrawBehaviorTreeSection();
            EditorGUILayout.Space(5);
            
            DrawBlackboardSection();
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("NPCBrain Debug", _headerStyle);
            GUILayout.FlexibleSpace();
            _autoRefresh = GUILayout.Toggle(_autoRefresh, "Auto Refresh", GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();
            
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
        }
        
        private void DrawNPCSelector()
        {
            EditorGUILayout.LabelField("NPC Selection", EditorStyles.boldLabel);
            
            var npcs = FindObjectsByType<NPCBrainController>(FindObjectsSortMode.None);
            
            if (npcs.Length == 0)
            {
                EditorGUILayout.HelpBox("No NPCBrainController found in scene.", MessageType.Warning);
                return;
            }
            
            var npcNames = new string[npcs.Length + 1];
            npcNames[0] = "(Select NPC)";
            int selectedIndex = 0;
            
            for (int i = 0; i < npcs.Length; i++)
            {
                npcNames[i + 1] = npcs[i].gameObject.name;
                if (npcs[i] == _selectedNPC)
                {
                    selectedIndex = i + 1;
                }
            }
            
            EditorGUILayout.BeginHorizontal();
            int newIndex = EditorGUILayout.Popup("Active NPC", selectedIndex, npcNames);
            if (newIndex != selectedIndex)
            {
                _selectedNPC = newIndex == 0 ? null : npcs[newIndex - 1];
            }
            
            if (GUILayout.Button("Ping", GUILayout.Width(50)) && _selectedNPC != null)
            {
                EditorGUIUtility.PingObject(_selectedNPC.gameObject);
                Selection.activeGameObject = _selectedNPC.gameObject;
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.LabelField($"NPCs in Scene: {npcs.Length}", EditorStyles.miniLabel);
        }
        
        private void DrawControls()
        {
            EditorGUILayout.LabelField("Controls", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            GUI.enabled = !_selectedNPC.IsPaused;
            if (GUILayout.Button("⏸ Pause", GUILayout.Height(25)))
            {
                _selectedNPC.Pause();
            }
            
            GUI.enabled = _selectedNPC.IsPaused;
            if (GUILayout.Button("▶ Resume", GUILayout.Height(25)))
            {
                _selectedNPC.Resume();
            }
            
            if (GUILayout.Button("⏭ Step", GUILayout.Height(25)))
            {
                // Single step: unpause, tick once, pause again
                bool wasPaused = _selectedNPC.IsPaused;
                if (wasPaused)
                {
                    // Temporarily enable for one tick
                    var pausedField = typeof(NPCBrainController).GetField("_isPaused", 
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (pausedField != null)
                    {
                        pausedField.SetValue(_selectedNPC, false);
                        _selectedNPC.Tick();
                        pausedField.SetValue(_selectedNPC, true);
                    }
                }
            }
            
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawStateDisplay()
        {
            EditorGUILayout.BeginVertical(_boxStyle);
            EditorGUILayout.LabelField("State", EditorStyles.boldLabel);
            
            // Paused state
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Paused:", GUILayout.Width(100));
            EditorGUILayout.LabelField(_selectedNPC.IsPaused ? "Yes" : "No");
            EditorGUILayout.EndHorizontal();
            
            // Last status with color
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Last Status:", GUILayout.Width(100));
            GUIStyle statusStyle = _selectedNPC.LastStatus switch
            {
                NodeStatus.Running => _statusRunningStyle,
                NodeStatus.Success => _statusSuccessStyle,
                NodeStatus.Failure => _statusFailureStyle,
                _ => EditorStyles.label
            };
            EditorGUILayout.LabelField(_selectedNPC.LastStatus.ToString(), statusStyle);
            EditorGUILayout.EndHorizontal();
            
            // Position
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Position:", GUILayout.Width(100));
            EditorGUILayout.LabelField(_selectedNPC.transform.position.ToString("F1"));
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawCriticalitySection()
        {
            _showCriticality = EditorGUILayout.Foldout(_showCriticality, "Criticality", true);
            if (!_showCriticality) return;
            
            EditorGUILayout.BeginVertical(_boxStyle);
            
            var criticality = _selectedNPC.Criticality;
            if (criticality == null)
            {
                EditorGUILayout.LabelField("No CriticalityController");
            }
            else
            {
                // Temperature bar
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Temperature:", GUILayout.Width(100));
                float tempNormalized = (criticality.Temperature - 0.5f) / 1.5f; // 0.5-2.0 range
                var tempRect = EditorGUILayout.GetControlRect(GUILayout.Height(18));
                EditorGUI.ProgressBar(tempRect, tempNormalized, $"{criticality.Temperature:F2}");
                EditorGUILayout.EndHorizontal();
                
                // Entropy bar
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Entropy:", GUILayout.Width(100));
                var entropyRect = EditorGUILayout.GetControlRect(GUILayout.Height(18));
                EditorGUI.ProgressBar(entropyRect, Mathf.Clamp01(criticality.Entropy), $"{criticality.Entropy:F2}");
                EditorGUILayout.EndHorizontal();
                
                // Inertia bar
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Inertia:", GUILayout.Width(100));
                var inertiaRect = EditorGUILayout.GetControlRect(GUILayout.Height(18));
                EditorGUI.ProgressBar(inertiaRect, criticality.Inertia, $"{criticality.Inertia:F2}");
                EditorGUILayout.EndHorizontal();
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawBehaviorTreeSection()
        {
            _showBehaviorTree = EditorGUILayout.Foldout(_showBehaviorTree, "Behavior Tree", true);
            if (!_showBehaviorTree) return;
            
            EditorGUILayout.BeginVertical(_boxStyle);
            
            var bt = _selectedNPC.BehaviorTree;
            if (bt == null)
            {
                EditorGUILayout.LabelField("No Behavior Tree");
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Root Node:", GUILayout.Width(100));
                EditorGUILayout.LabelField(bt.Name ?? bt.GetType().Name);
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Is Running:", GUILayout.Width(100));
                EditorGUILayout.LabelField(bt.IsRunning ? "Yes" : "No");
                EditorGUILayout.EndHorizontal();
                
                // Show current action if UtilitySelector
                if (bt is UtilitySelector utilitySelector)
                {
                    EditorGUILayout.Space(5);
                    EditorGUILayout.LabelField("Utility Selector", EditorStyles.boldLabel);
                    
                    var currentAction = utilitySelector.CurrentAction;
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Current Action:", GUILayout.Width(100));
                    EditorGUILayout.LabelField(currentAction?.Name ?? "(selecting...)");
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Action Count:", GUILayout.Width(100));
                    EditorGUILayout.LabelField(utilitySelector.ActionCount.ToString());
                    EditorGUILayout.EndHorizontal();
                    
                    // Show scores if available
                    var scores = utilitySelector.GetLastScores();
                    var probs = utilitySelector.GetLastProbabilities();
                    if (scores.Count > 0)
                    {
                        EditorGUILayout.Space(3);
                        EditorGUILayout.LabelField("Action Scores:", EditorStyles.miniLabel);
                        for (int i = 0; i < scores.Count; i++)
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField($"  [{i}]", GUILayout.Width(30));
                            var scoreRect = EditorGUILayout.GetControlRect(GUILayout.Height(16));
                            string label = $"Score: {scores[i]:F2}";
                            if (i < probs.Count)
                            {
                                label += $" | Prob: {probs[i]:P0}";
                            }
                            EditorGUI.ProgressBar(scoreRect, Mathf.Clamp01(scores[i]), label);
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawBlackboardSection()
        {
            _showBlackboard = EditorGUILayout.Foldout(_showBlackboard, "Blackboard", true);
            if (!_showBlackboard) return;
            
            EditorGUILayout.BeginVertical(_boxStyle);
            
            var blackboard = _selectedNPC.Blackboard;
            if (blackboard == null)
            {
                EditorGUILayout.LabelField("No Blackboard");
            }
            else
            {
                var keys = new List<string>(blackboard.Keys);
                
                if (keys.Count == 0)
                {
                    EditorGUILayout.LabelField("(empty)", EditorStyles.centeredGreyMiniLabel);
                }
                else
                {
                    _blackboardScrollPos = EditorGUILayout.BeginScrollView(_blackboardScrollPos, 
                        GUILayout.MaxHeight(150));
                    
                    foreach (var key in keys)
                    {
                        object value = blackboard.Get<object>(key);
                        string valueStr = FormatValue(value);
                        
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(key, GUILayout.Width(120));
                        EditorGUILayout.LabelField(valueStr, EditorStyles.wordWrappedLabel);
                        EditorGUILayout.EndHorizontal();
                    }
                    
                    EditorGUILayout.EndScrollView();
                }
                
                EditorGUILayout.LabelField($"Keys: {keys.Count}", EditorStyles.miniLabel);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private string FormatValue(object value)
        {
            if (value == null) return "null";
            
            if (value is Vector3 v3)
                return v3.ToString("F1");
            if (value is Vector2 v2)
                return v2.ToString("F1");
            if (value is float f)
                return f.ToString("F2");
            if (value is GameObject go)
                return go != null ? go.name : "(destroyed)";
            if (value is Component comp)
                return comp != null ? $"{comp.GetType().Name} on {comp.gameObject.name}" : "(destroyed)";
            
            return value.ToString();
        }
    }
}
