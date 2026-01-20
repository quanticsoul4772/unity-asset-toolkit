using UnityEngine;
using UnityEngine.UI;
using NPCBrain.Archetypes;
using NPCBrain.BehaviorTree.Composites;

namespace NPCBrain.Demo.UI
{
    /// <summary>
    /// World-space floating UI indicator that appears above NPCs.
    /// Shows current state and action with billboard effect (always faces camera).
    /// </summary>
    public class FloatingStatusIndicator : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float _heightOffset = 2.5f;
        [SerializeField] private float _baseScale = 0.008f;
        [SerializeField] private float _minScale = 0.004f;
        [SerializeField] private float _maxScale = 0.012f;
        [SerializeField] private float _fadeStartDistance = 30f;
        [SerializeField] private float _fadeEndDistance = 50f;
        
        // Components
        private Canvas _canvas;
        private CanvasGroup _canvasGroup;
        private Text _nameText;
        private Text _stateText;
        private Text _actionText;
        private Image _backgroundImage;
        private Image _stateIcon;
        
        // Target reference
        private Transform _target;
        private NPCBrainController _npcBrain;
        private CopNPC _copNPC;
        private RobberNPC _robberNPC;
        private Camera _mainCamera;
        
        // Colors
        private Color _copColor = new Color(0.2f, 0.5f, 1f);
        private Color _robberColor = new Color(0.3f, 0.3f, 0.3f);
        private Color _alertColor = new Color(1f, 0.3f, 0.3f);
        private Color _normalColor = new Color(0.3f, 0.8f, 0.3f);
        private Color _cautionColor = new Color(1f, 0.8f, 0.2f);
        
        /// <summary>
        /// Creates a floating indicator for the specified NPC.
        /// </summary>
        public static FloatingStatusIndicator Create(NPCBrainController npc, Transform parent)
        {
            var indicatorGO = new GameObject($"FloatingIndicator_{npc.name}");
            indicatorGO.transform.SetParent(parent);
            
            var indicator = indicatorGO.AddComponent<FloatingStatusIndicator>();
            indicator.Initialize(npc);
            
            return indicator;
        }
        
        private void Initialize(NPCBrainController npc)
        {
            _target = npc.transform;
            _npcBrain = npc;
            _copNPC = npc as CopNPC;
            _robberNPC = npc as RobberNPC;
            _mainCamera = Camera.main;
            
            CreateCanvas();
            CreateUI();
        }
        
        private void CreateCanvas()
        {
            // World-space canvas
            _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;
            _canvas.sortingOrder = 50;
            
            // Set initial scale
            var rect = gameObject.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 80);
            transform.localScale = Vector3.one * _baseScale;
            
            // Canvas group for fading
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }
        
        private void CreateUI()
        {
            var rect = GetComponent<RectTransform>();
            
            // Background panel
            var bgGO = new GameObject("Background");
            bgGO.transform.SetParent(transform, false);
            
            var bgRect = bgGO.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            
            _backgroundImage = bgGO.AddComponent<Image>();
            _backgroundImage.color = new Color(0.1f, 0.1f, 0.12f, 0.85f);
            
            // Content layout
            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(bgGO.transform, false);
            
            var contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(8, 5);
            contentRect.offsetMax = new Vector2(-8, -5);
            
            var contentLayout = contentGO.AddComponent<VerticalLayoutGroup>();
            contentLayout.childAlignment = TextAnchor.MiddleCenter;
            contentLayout.spacing = 2;
            contentLayout.childControlHeight = true;
            contentLayout.childControlWidth = true;
            contentLayout.childForceExpandHeight = false;
            contentLayout.childForceExpandWidth = true;
            
            // Name text
            _nameText = CreateText(contentGO.transform, "Name", _npcBrain.name, 16, FontStyle.Bold);
            var nameRect = _nameText.GetComponent<RectTransform>();
            var nameLayout = _nameText.gameObject.AddComponent<LayoutElement>();
            nameLayout.preferredHeight = 22;
            
            // Set name color based on NPC type
            _nameText.color = _copNPC != null ? _copColor : _robberColor;
            
            // State text (e.g., "Patrolling", "Fleeing")
            _stateText = CreateText(contentGO.transform, "State", "State", 12, FontStyle.Normal);
            var stateLayout = _stateText.gameObject.AddComponent<LayoutElement>();
            stateLayout.preferredHeight = 16;
            
            // Action text (current behavior tree action)
            _actionText = CreateText(contentGO.transform, "Action", "Action", 10, FontStyle.Italic);
            _actionText.color = new Color(0.7f, 0.7f, 0.7f);
            var actionLayout = _actionText.gameObject.AddComponent<LayoutElement>();
            actionLayout.preferredHeight = 14;
            
            // State indicator bar at bottom
            var barGO = new GameObject("StateBar");
            barGO.transform.SetParent(bgGO.transform, false);
            
            var barRect = barGO.AddComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0, 0);
            barRect.anchorMax = new Vector2(1, 0);
            barRect.pivot = new Vector2(0.5f, 0);
            barRect.sizeDelta = new Vector2(0, 4);
            barRect.anchoredPosition = Vector2.zero;
            
            _stateIcon = barGO.AddComponent<Image>();
            _stateIcon.color = _normalColor;
        }
        
        private Text CreateText(Transform parent, string name, string text, int fontSize, FontStyle style)
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
            textComp.alignment = TextAnchor.MiddleCenter;
            textComp.color = Color.white;
            textComp.horizontalOverflow = HorizontalWrapMode.Overflow;
            textComp.verticalOverflow = VerticalWrapMode.Overflow;
            
            return textComp;
        }
        
        private void LateUpdate()
        {
            if (_target == null || _mainCamera == null)
            {
                gameObject.SetActive(false);
                return;
            }
            
            // Check if target is active
            if (!_target.gameObject.activeSelf)
            {
                gameObject.SetActive(false);
                return;
            }
            
            gameObject.SetActive(true);
            
            // Position above target
            transform.position = _target.position + Vector3.up * _heightOffset;
            
            // Billboard effect - face camera
            Vector3 dirToCamera = _mainCamera.transform.position - transform.position;
            dirToCamera.y = 0; // Keep upright
            if (dirToCamera.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(-dirToCamera);
            }
            
            // Scale based on distance
            float distance = Vector3.Distance(_mainCamera.transform.position, transform.position);
            float scaleFactor = Mathf.Clamp(_baseScale * (distance / 20f), _minScale, _maxScale);
            transform.localScale = Vector3.one * scaleFactor;
            
            // Fade based on distance
            float alpha = 1f;
            if (distance > _fadeStartDistance)
            {
                alpha = 1f - Mathf.Clamp01((distance - _fadeStartDistance) / (_fadeEndDistance - _fadeStartDistance));
            }
            _canvasGroup.alpha = alpha;
            
            // Update content
            UpdateContent();
        }
        
        private void UpdateContent()
        {
            if (_copNPC != null)
            {
                UpdateCopContent();
            }
            else if (_robberNPC != null)
            {
                UpdateRobberContent();
            }
        }
        
        private void UpdateCopContent()
        {
            // Get current action name
            string actionName = "(selecting)";
            if (_copNPC.BehaviorTree is UtilitySelector selector && selector.CurrentAction != null)
            {
                actionName = selector.CurrentAction.Name;
            }
            
            // Update state text based on current action
            string state = _copNPC.CurrentState;
            _stateText.text = state;
            
            // Color based on state
            Color stateColor;
            if (state.Contains("Chase") || state.Contains("Arrest"))
            {
                stateColor = _alertColor;
                _stateIcon.color = _alertColor;
            }
            else if (state.Contains("Investigate") || state.Contains("Search"))
            {
                stateColor = _cautionColor;
                _stateIcon.color = _cautionColor;
            }
            else
            {
                stateColor = _normalColor;
                _stateIcon.color = _normalColor;
            }
            _stateText.color = stateColor;
            
            // Action info
            string alertInfo = _copNPC.AlertLevel > 0.5f ? $" ⚠️{_copNPC.AlertLevel:P0}" : "";
            _actionText.text = $"{actionName}{alertInfo}";
        }
        
        private void UpdateRobberContent()
        {
            // Get current action name
            string actionName = "(selecting)";
            if (_robberNPC.BehaviorTree is UtilitySelector selector && selector.CurrentAction != null)
            {
                actionName = selector.CurrentAction.Name;
            }
            
            // Update state text
            string state = _robberNPC.CurrentState;
            
            // Add loot indicator
            if (_robberNPC.IsCarryingLoot)
            {
                state += $" 💰${_robberNPC.CarriedLootValue}";
            }
            
            _stateText.text = state;
            
            // Color based on state
            Color stateColor;
            if (state.Contains("Flee") || _robberNPC.FearLevel > 0.5f)
            {
                stateColor = _alertColor;
                _stateIcon.color = _alertColor;
            }
            else if (state.Contains("Escape") || state.Contains("Steal"))
            {
                stateColor = _cautionColor;
                _stateIcon.color = _cautionColor;
            }
            else if (state.Contains("Hide") || state.Contains("Sneak"))
            {
                stateColor = new Color(0.5f, 0.8f, 1f);
                _stateIcon.color = stateColor;
            }
            else
            {
                stateColor = _normalColor;
                _stateIcon.color = _normalColor;
            }
            _stateText.color = stateColor;
            
            // Action info with fear
            string fearInfo = _robberNPC.FearLevel > 0.3f ? $" 😰{_robberNPC.FearLevel:P0}" : "";
            _actionText.text = $"{actionName}{fearInfo}";
        }
    }
}
