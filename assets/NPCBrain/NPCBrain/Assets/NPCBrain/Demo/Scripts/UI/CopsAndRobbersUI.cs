using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem.UI;
using NPCBrain.Archetypes;
using NPCBrain.BehaviorTree.Composites;
using NPCBrain.Components;

namespace NPCBrain.Demo.UI
{
    /// <summary>
    /// Modern Canvas-based UI for the Cops and Robbers demo.
    /// Replaces legacy OnGUI with scalable, responsive UI.
    /// </summary>
    public class CopsAndRobbersUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private List<CopNPC> _cops;
        [SerializeField] private List<RobberNPC> _robbers;
        [SerializeField] private List<LootPoint> _lootPoints;
        [SerializeField] private EasyPath.EasyPathGrid _pathfindingGrid;
        
        [Header("UI Settings")]
        [SerializeField] private Vector2 _referenceResolution = new Vector2(1280, 720);
        [SerializeField] private float _panelWidth = 520f;
        
        // Canvas components
        private Canvas _canvas;
        private CanvasScaler _canvasScaler;
        private GameObject _mainPanel;
        private ScrollRect _scrollRect;
        private RectTransform _contentRect;
        
        // Dynamic text elements
        private Text _titleText;
        private Text _timeText;
        private Text _scoreText;
        private Text _winnerText;
        private GameObject _copsContainer;
        private GameObject _robbersContainer;
        private GameObject _lootContainer;
        private Dictionary<CopNPC, Text> _copStatusTexts = new Dictionary<CopNPC, Text>();
        private Dictionary<RobberNPC, Text> _robberStatusTexts = new Dictionary<RobberNPC, Text>();
        private Dictionary<LootPoint, Text> _lootStatusTexts = new Dictionary<LootPoint, Text>();
        
        // Game state references (set by CopsAndRobbersDemoSetup)
        private System.Func<float> _getGameTime;
        private System.Func<int> _getCopScore;
        private System.Func<int> _getRobberScore;
        private System.Func<bool> _getGameEnded;
        private System.Func<string> _getWinner;
        private System.Func<bool> _getTimeLimitEnabled;
        private System.Action _onRestartClicked;
        
        // Styles
        private Color _panelBgColor = new Color(0.1f, 0.1f, 0.12f, 0.92f);
        private Color _sectionBgColor = new Color(0.15f, 0.15f, 0.18f, 0.95f);
        private Color _copColor = new Color(0.3f, 0.5f, 1f);
        private Color _robberColor = new Color(0.4f, 0.4f, 0.4f);
        private Color _lootColor = new Color(1f, 0.85f, 0.3f);
        
        /// <summary>
        /// Initializes the UI with game references.
        /// </summary>
        public void Initialize(
            List<CopNPC> cops,
            List<RobberNPC> robbers,
            List<LootPoint> lootPoints,
            EasyPath.EasyPathGrid grid,
            System.Func<float> getGameTime,
            System.Func<int> getCopScore,
            System.Func<int> getRobberScore,
            System.Func<bool> getGameEnded,
            System.Func<string> getWinner,
            System.Func<bool> getTimeLimitEnabled,
            System.Action onRestartClicked)
        {
            _cops = cops;
            _robbers = robbers;
            _lootPoints = lootPoints;
            _pathfindingGrid = grid;
            _getGameTime = getGameTime;
            _getCopScore = getCopScore;
            _getRobberScore = getRobberScore;
            _getGameEnded = getGameEnded;
            _getWinner = getWinner;
            _getTimeLimitEnabled = getTimeLimitEnabled;
            _onRestartClicked = onRestartClicked;
            
            CreateCanvas();
            CreateMainPanel();
        }
        
        private void CreateCanvas()
        {
            // Create Canvas GameObject
            var canvasGO = new GameObject("HUD_Canvas");
            canvasGO.transform.SetParent(transform);
            
            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;
            
            // Add CanvasScaler for resolution independence
            _canvasScaler = canvasGO.AddComponent<CanvasScaler>();
            _canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _canvasScaler.referenceResolution = _referenceResolution;
            _canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            _canvasScaler.matchWidthOrHeight = 0.5f; // Balance between width and height
            
            // Add GraphicRaycaster for button interaction
            canvasGO.AddComponent<GraphicRaycaster>();
            
            // Ensure EventSystem exists (use new Input System module)
            if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var eventSystemGO = new GameObject("EventSystem");
                eventSystemGO.transform.SetParent(transform);
                eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemGO.AddComponent<InputSystemUIInputModule>(); // New Input System
            }
        }
        
        private void CreateMainPanel()
        {
            // Main panel container - anchored to left side
            _mainPanel = CreatePanel("MainPanel", _canvas.transform);
            var mainRect = _mainPanel.GetComponent<RectTransform>();
            mainRect.anchorMin = new Vector2(0, 0);
            mainRect.anchorMax = new Vector2(0, 1);
            mainRect.pivot = new Vector2(0, 0.5f);
            mainRect.anchoredPosition = new Vector2(10, 0);
            mainRect.sizeDelta = new Vector2(_panelWidth, -20);
            
            // Add main panel background
            var mainBg = _mainPanel.AddComponent<Image>();
            mainBg.color = _panelBgColor;
            
            // Add vertical layout to main panel
            var mainLayout = _mainPanel.AddComponent<VerticalLayoutGroup>();
            mainLayout.padding = new RectOffset(10, 10, 10, 10);
            mainLayout.spacing = 8;
            mainLayout.childAlignment = TextAnchor.UpperCenter;
            mainLayout.childControlWidth = true;
            mainLayout.childControlHeight = false;
            mainLayout.childForceExpandWidth = true;
            mainLayout.childForceExpandHeight = false;
            
            // Title section
            CreateTitleSection(_mainPanel.transform);
            
            // Scroll view for content
            var scrollGO = CreateScrollView(_mainPanel.transform);
            _contentRect = scrollGO.transform.Find("Viewport/Content").GetComponent<RectTransform>();
            
            // Content sections inside scroll view
            CreateCopsSection(_contentRect);
            CreateRobbersSection(_contentRect);
            CreateLootSection(_contentRect);
            CreateInfoSection(_contentRect);
            CreateControlsSection(_contentRect);
        }
        
        private void CreateTitleSection(Transform parent)
        {
            var section = CreateSection("TitleSection", parent, 140);
            
            // Title
            _titleText = CreateText(section.transform, "Title", "🚨 COPS AND ROBBERS 🎭", 28, FontStyle.Bold, TextAnchor.MiddleCenter);
            var titleRect = _titleText.GetComponent<RectTransform>();
            titleRect.sizeDelta = new Vector2(0, 36);
            
            // Subtitle
            var subtitle = CreateText(section.transform, "Subtitle", "NPCBrain Utility AI Demo", 14, FontStyle.Italic, TextAnchor.MiddleCenter);
            subtitle.color = new Color(0.7f, 0.7f, 0.7f);
            var subRect = subtitle.GetComponent<RectTransform>();
            subRect.sizeDelta = new Vector2(0, 20);
            
            // Time and Score
            _timeText = CreateText(section.transform, "Time", "Time: 00:00", 16, FontStyle.Normal, TextAnchor.MiddleCenter);
            var timeRect = _timeText.GetComponent<RectTransform>();
            timeRect.sizeDelta = new Vector2(0, 26);
            
            _scoreText = CreateText(section.transform, "Score", "COPS: $0  |  ROBBERS: $0", 18, FontStyle.Bold, TextAnchor.MiddleCenter);
            var scoreRect = _scoreText.GetComponent<RectTransform>();
            scoreRect.sizeDelta = new Vector2(0, 28);
            
            // Winner text (hidden initially)
            _winnerText = CreateText(section.transform, "Winner", "", 22, FontStyle.Bold, TextAnchor.MiddleCenter);
            _winnerText.color = Color.yellow;
            var winnerRect = _winnerText.GetComponent<RectTransform>();
            winnerRect.sizeDelta = new Vector2(0, 32);
            _winnerText.gameObject.SetActive(false);
        }
        
        private GameObject CreateScrollView(Transform parent)
        {
            var scrollGO = new GameObject("ScrollView");
            scrollGO.transform.SetParent(parent, false);
            
            var scrollRect = scrollGO.AddComponent<RectTransform>();
            var scrollLayout = scrollGO.AddComponent<LayoutElement>();
            scrollLayout.flexibleHeight = 1;
            scrollLayout.minHeight = 100;
            
            _scrollRect = scrollGO.AddComponent<ScrollRect>();
            _scrollRect.horizontal = false;
            _scrollRect.vertical = true;
            _scrollRect.scrollSensitivity = 20f;
            
            // Viewport
            var viewportGO = new GameObject("Viewport");
            viewportGO.transform.SetParent(scrollGO.transform, false);
            var viewportRect = viewportGO.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewportRect.pivot = new Vector2(0, 1);
            
            var viewportMask = viewportGO.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;
            var viewportImg = viewportGO.AddComponent<Image>();
            viewportImg.color = Color.clear;
            
            // Content
            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(viewportGO.transform, false);
            var contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 0);
            
            var contentLayout = contentGO.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(5, 5, 5, 5);
            contentLayout.spacing = 8;
            contentLayout.childAlignment = TextAnchor.UpperCenter;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            
            var contentFitter = contentGO.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            _scrollRect.viewport = viewportRect;
            _scrollRect.content = contentRect;
            
            return scrollGO;
        }
        
        private void CreateCopsSection(Transform parent)
        {
            var section = CreateSection("CopsSection", parent, 0);
            var header = CreateSectionHeader(section.transform, "👮 COPS", _copColor);
            
            _copsContainer = new GameObject("CopsContainer");
            _copsContainer.transform.SetParent(section.transform, false);
            var containerLayout = _copsContainer.AddComponent<VerticalLayoutGroup>();
            containerLayout.spacing = 4;
            containerLayout.childControlHeight = true;
            containerLayout.childControlWidth = true;
            containerLayout.childForceExpandHeight = false;
            
            var containerFitter = _copsContainer.AddComponent<ContentSizeFitter>();
            containerFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            // Create status text for each cop
            foreach (var cop in _cops)
            {
                if (cop == null) continue;
                var statusText = CreateText(_copsContainer.transform, $"Cop_{cop.name}", "", 14, FontStyle.Normal, TextAnchor.UpperLeft);
                statusText.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 75);
                _copStatusTexts[cop] = statusText;
            }
        }
        
        private void CreateRobbersSection(Transform parent)
        {
            var section = CreateSection("RobbersSection", parent, 0);
            var header = CreateSectionHeader(section.transform, "🎭 ROBBERS", _robberColor);
            
            _robbersContainer = new GameObject("RobbersContainer");
            _robbersContainer.transform.SetParent(section.transform, false);
            var containerLayout = _robbersContainer.AddComponent<VerticalLayoutGroup>();
            containerLayout.spacing = 4;
            containerLayout.childControlHeight = true;
            containerLayout.childControlWidth = true;
            containerLayout.childForceExpandHeight = false;
            
            var containerFitter = _robbersContainer.AddComponent<ContentSizeFitter>();
            containerFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            // Create status text for each robber
            foreach (var robber in _robbers)
            {
                if (robber == null) continue;
                var statusText = CreateText(_robbersContainer.transform, $"Robber_{robber.name}", "", 14, FontStyle.Normal, TextAnchor.UpperLeft);
                statusText.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 75);
                _robberStatusTexts[robber] = statusText;
            }
        }
        
        private void CreateLootSection(Transform parent)
        {
            var section = CreateSection("LootSection", parent, 0);
            var header = CreateSectionHeader(section.transform, "💰 LOOT", _lootColor);
            
            _lootContainer = new GameObject("LootContainer");
            _lootContainer.transform.SetParent(section.transform, false);
            var containerLayout = _lootContainer.AddComponent<VerticalLayoutGroup>();
            containerLayout.spacing = 2;
            containerLayout.childControlHeight = true;
            containerLayout.childControlWidth = true;
            containerLayout.childForceExpandHeight = false;
            
            var containerFitter = _lootContainer.AddComponent<ContentSizeFitter>();
            containerFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            // Create status text for each loot
            foreach (var loot in _lootPoints)
            {
                if (loot == null) continue;
                var statusText = CreateText(_lootContainer.transform, $"Loot_{loot.name}", "", 14, FontStyle.Normal, TextAnchor.UpperLeft);
                statusText.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 24);
                _lootStatusTexts[loot] = statusText;
            }
        }
        
        private void CreateInfoSection(Transform parent)
        {
            var section = CreateSection("InfoSection", parent, 0);
            var header = CreateSectionHeader(section.transform, "📊 INFO", Color.white);
            
            var infoText = CreateText(section.transform, "InfoText", 
                "<b>Criticality System:</b>\n" +
                "  T = Temperature (exploration vs exploitation)\n" +
                "  I = Inertia (path following precision)\n\n" +
                "<b>A* Pathfinding:</b> EasyPath Active\n" +
                "<b>AI:</b> Utility-based Decision Making", 
                13, FontStyle.Normal, TextAnchor.UpperLeft);
            infoText.color = new Color(0.8f, 0.8f, 0.8f);
        }
        
        private void CreateControlsSection(Transform parent)
        {
            var section = CreateSection("ControlsSection", parent, 0);
            var header = CreateSectionHeader(section.transform, "🎮 CONTROLS", Color.white);
            
            // Restart button
            var buttonGO = new GameObject("RestartButton");
            buttonGO.transform.SetParent(section.transform, false);
            
            var buttonRect = buttonGO.AddComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(0, 45);
            
            var buttonImg = buttonGO.AddComponent<Image>();
            buttonImg.color = new Color(0.2f, 0.5f, 0.3f);
            
            var button = buttonGO.AddComponent<Button>();
            button.targetGraphic = buttonImg;
            button.onClick.AddListener(() => _onRestartClicked?.Invoke());
            
            var buttonText = CreateText(buttonGO.transform, "Text", "🔄 Restart Game", 16, FontStyle.Bold, TextAnchor.MiddleCenter);
            buttonText.color = Color.white;
            var textRect = buttonText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            
            // Instructions
            var instructions = CreateText(section.transform, "Instructions", 
                "View in Scene window with Gizmos enabled\nAll AI is fully autonomous!", 
                12, FontStyle.Italic, TextAnchor.MiddleCenter);
            instructions.color = new Color(0.7f, 0.7f, 0.7f);
        }
        
        // Helper methods
        private GameObject CreatePanel(string name, Transform parent)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            panel.AddComponent<RectTransform>();
            return panel;
        }
        
        private GameObject CreateSection(string name, Transform parent, float minHeight)
        {
            var section = new GameObject(name);
            section.transform.SetParent(parent, false);
            
            var rect = section.AddComponent<RectTransform>();
            
            var bg = section.AddComponent<Image>();
            bg.color = _sectionBgColor;
            
            var layout = section.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 6, 6);
            layout.spacing = 4;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            
            var fitter = section.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            if (minHeight > 0)
            {
                var layoutElement = section.AddComponent<LayoutElement>();
                layoutElement.minHeight = minHeight;
            }
            
            return section;
        }
        
        private Text CreateSectionHeader(Transform parent, string text, Color color)
        {
            var header = CreateText(parent, "Header", text, 18, FontStyle.Bold, TextAnchor.MiddleLeft);
            header.color = color;
            var rect = header.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 28);
            return header;
        }
        
        private Text CreateText(Transform parent, string name, string text, int fontSize, FontStyle style, TextAnchor anchor)
        {
            var textGO = new GameObject(name);
            textGO.transform.SetParent(parent, false);
            
            var rect = textGO.AddComponent<RectTransform>();
            
            var textComp = textGO.AddComponent<Text>();
            textComp.text = text;
            textComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (textComp.font == null)
                textComp.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            textComp.fontSize = fontSize;
            textComp.fontStyle = style;
            textComp.alignment = anchor;
            textComp.color = Color.white;
            textComp.supportRichText = true;
            textComp.horizontalOverflow = HorizontalWrapMode.Wrap;
            textComp.verticalOverflow = VerticalWrapMode.Overflow;
            
            return textComp;
        }
        
        private void Update()
        {
            if (_getGameTime == null) return;
            
            UpdateTimeAndScore();
            UpdateCopStatuses();
            UpdateRobberStatuses();
            UpdateLootStatuses();
        }
        
        private void UpdateTimeAndScore()
        {
            float gameTime = _getGameTime();
            // Clamp to valid range to prevent TimeSpan overflow
            gameTime = Mathf.Clamp(gameTime, 0f, 359999f); // Max ~100 hours
            string timeStr = System.TimeSpan.FromSeconds(gameTime).ToString(@"mm\:ss");
            
            string timeDisplay = $"Elapsed: {timeStr}";
            
            if (_getTimeLimitEnabled())
            {
                float remaining = HeistTimer.TimeRemaining;
                // Clamp to valid range - handle Infinity, NaN, and negative values
                if (float.IsNaN(remaining) || float.IsInfinity(remaining) || remaining < 0f)
                {
                    remaining = 0f;
                }
                remaining = Mathf.Clamp(remaining, 0f, 359999f);
                
                string remainingStr = System.TimeSpan.FromSeconds(remaining).ToString(@"mm\:ss");
                string urgency = remaining <= 10f ? " <color=red>⚠️ CRITICAL!</color>" : 
                                 remaining <= 30f ? " <color=yellow>⏰ HURRY!</color>" : "";
                timeDisplay += $"  |  Remaining: {remainingStr}{urgency}";
            }
            
            _timeText.text = timeDisplay;
            
            int copScore = _getCopScore();
            int robberScore = _getRobberScore();
            _scoreText.text = $"<color=#6699FF>COPS: ${copScore}</color>  |  <color=#888888>ROBBERS: ${robberScore}</color>";
            
            bool gameEnded = _getGameEnded();
            if (gameEnded)
            {
                _winnerText.gameObject.SetActive(true);
                _winnerText.text = _getWinner();
            }
            else
            {
                _winnerText.gameObject.SetActive(false);
            }
        }
        
        private void UpdateCopStatuses()
        {
            foreach (var kvp in _copStatusTexts)
            {
                var cop = kvp.Key;
                var text = kvp.Value;
                
                if (cop == null) continue;
                
                string actionName = "(selecting)";
                if (cop.BehaviorTree is UtilitySelector selector && selector.CurrentAction != null)
                {
                    actionName = selector.CurrentAction.Name;
                }
                
                string critInfo = cop.Criticality != null ? 
                    $"T:{cop.Criticality.Temperature:F1} I:{cop.Criticality.Inertia:F1}" : "";
                
                string stateColor = GetStateColor(cop.CurrentState);
                
                // Get additional cop details
                string alertInfo = "";
                string visionInfo = cop.CanSeeTarget ? " 👁️" : "";
                
                text.text = $"<b>{cop.name}</b> - {cop.Role}{visionInfo}\n" +
                           $"  Goal: {cop.Goal}\n" +
                           $"  Action: <color={stateColor}>{actionName}</color> {critInfo}\n" +
                           $"  <color=#AADDFF>{cop.CurrentReason}</color>";
            }
        }
        
        private void UpdateRobberStatuses()
        {
            foreach (var kvp in _robberStatusTexts)
            {
                var robber = kvp.Key;
                var text = kvp.Value;
                
                if (robber == null) continue;
                
                if (!robber.gameObject.activeSelf)
                {
                    string status = robber.HasEscaped ? "<color=green>✓ ESCAPED!</color>" : "<color=red>✗ ARRESTED!</color>";
                    string lootResult = robber.HasEscaped ? $" with ${robber.CarriedLootValue}" : "";
                    text.text = $"<b>{robber.name}</b> - {status}{lootResult}\n" +
                               $"  <color=#FFDDAA>{robber.CurrentReason}</color>";
                    continue;
                }
                
                string actionName = "(selecting)";
                if (robber.BehaviorTree is UtilitySelector selector && selector.CurrentAction != null)
                {
                    actionName = selector.CurrentAction.Name;
                }
                
                string critInfo = robber.Criticality != null ? 
                    $"T:{robber.Criticality.Temperature:F1} I:{robber.Criticality.Inertia:F1}" : "";
                
                string lootInfo = robber.IsCarryingLoot ? $" 💰${robber.CarriedLootValue}" : "";
                string fearEmoji = robber.FearLevel > 0.5f ? " 😱" : robber.FearLevel > 0.2f ? " 😰" : "";
                string copVision = robber.CanSeeCop ? " 👁️COP!" : "";
                string stateColor = GetStateColor(robber.CurrentState);
                string urgency = robber.Urgency > 0.7f ? " <color=red>⏰RUSH!</color>" : robber.Urgency > 0.4f ? " <color=yellow>⏰</color>" : "";
                
                text.text = $"<b>{robber.name}</b>{lootInfo}{fearEmoji}{copVision}{urgency}\n" +
                           $"  Goal: {robber.Goal}\n" +
                           $"  Action: <color={stateColor}>{actionName}</color> {critInfo}\n" +
                           $"  Fear: {robber.FearLevel:P0} | <color=#FFDDAA>{robber.CurrentReason}</color>";
            }
        }
        
        private void UpdateLootStatuses()
        {
            foreach (var kvp in _lootStatusTexts)
            {
                var loot = kvp.Key;
                var text = kvp.Value;
                
                if (loot == null) continue;
                
                string status = loot.IsStolen ? "<color=red>STOLEN</color>" : "<color=green>Available</color>";
                text.text = $"{loot.name}: ${loot.Value} - {status}";
            }
        }
        
        private string GetStateColor(string state)
        {
            if (state.Contains("Arrest") || state.Contains("Chase") || state.Contains("Flee"))
                return "#FF6666";
            if (state.Contains("Investigate") || state.Contains("Steal") || state.Contains("Escape"))
                return "#FFFF66";
            if (state.Contains("Hide") || state.Contains("Sneak") || state.Contains("Return"))
                return "#66FFFF";
            return "#66FF66";
        }
        
        /// <summary>
        /// Cleanup when destroyed.
        /// </summary>
        public void Cleanup()
        {
            _copStatusTexts.Clear();
            _robberStatusTexts.Clear();
            _lootStatusTexts.Clear();
        }
    }
}
