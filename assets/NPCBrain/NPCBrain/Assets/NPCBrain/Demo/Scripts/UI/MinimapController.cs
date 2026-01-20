using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NPCBrain.Archetypes;
using NPCBrain.Components;

namespace NPCBrain.Demo.UI
{
    /// <summary>
    /// Creates and manages a minimap showing NPC positions in real-time.
    /// Uses an orthographic camera rendering to a RenderTexture displayed on Canvas.
    /// </summary>
    public class MinimapController : MonoBehaviour
    {
        [Header("Minimap Settings")]
        [SerializeField] private float _mapSize = 60f;
        [SerializeField] private float _cameraHeight = 100f;
        [SerializeField] private int _textureSize = 256;
        [SerializeField] private Vector2 _minimapScreenSize = new Vector2(200, 200);
        [SerializeField] private Vector2 _minimapOffset = new Vector2(15, 15);
        
        [Header("Icon Settings")]
        [SerializeField] private float _iconSize = 12f;
        [SerializeField] private Color _copIconColor = new Color(0.2f, 0.5f, 1f);
        [SerializeField] private Color _robberIconColor = new Color(0.2f, 0.2f, 0.2f);
        [SerializeField] private Color _lootIconColor = new Color(1f, 0.85f, 0.2f);
        [SerializeField] private Color _escapeIconColor = new Color(0.2f, 0.9f, 0.3f);
        [SerializeField] private Color _stolenLootColor = new Color(0.5f, 0.3f, 0.3f);
        
        // References
        private List<CopNPC> _cops;
        private List<RobberNPC> _robbers;
        private List<LootPoint> _lootPoints;
        private EscapeZone _escapeZone;
        
        // Components
        private Camera _minimapCamera;
        private RenderTexture _renderTexture;
        private Canvas _canvas;
        private RawImage _minimapImage;
        private GameObject _iconsContainer;
        
        // Icon tracking
        private Dictionary<CopNPC, Image> _copIcons = new Dictionary<CopNPC, Image>();
        private Dictionary<RobberNPC, Image> _robberIcons = new Dictionary<RobberNPC, Image>();
        private Dictionary<LootPoint, Image> _lootIcons = new Dictionary<LootPoint, Image>();
        private Image _escapeIcon;
        
        // Cached values
        private RectTransform _minimapRect;
        private float _worldToMinimapScale;
        
        /// <summary>
        /// Initializes the minimap with game references.
        /// </summary>
        public void Initialize(
            List<CopNPC> cops,
            List<RobberNPC> robbers,
            List<LootPoint> lootPoints,
            EscapeZone escapeZone,
            float arenaSize)
        {
            _cops = cops;
            _robbers = robbers;
            _lootPoints = lootPoints;
            _escapeZone = escapeZone;
            _mapSize = arenaSize;
            
            CreateMinimapCamera();
            CreateMinimapUI();
            CreateIcons();
            
            // Calculate conversion scale
            _worldToMinimapScale = _minimapScreenSize.x / _mapSize;
        }
        
        private void CreateMinimapCamera()
        {
            // Create camera GameObject
            var cameraGO = new GameObject("MinimapCamera");
            cameraGO.transform.SetParent(transform);
            cameraGO.transform.position = new Vector3(0, _cameraHeight, 0);
            cameraGO.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // Look down
            
            _minimapCamera = cameraGO.AddComponent<Camera>();
            _minimapCamera.orthographic = true;
            _minimapCamera.orthographicSize = _mapSize / 2f;
            _minimapCamera.clearFlags = CameraClearFlags.SolidColor;
            _minimapCamera.backgroundColor = new Color(0.12f, 0.14f, 0.16f);
            _minimapCamera.cullingMask = 0; // Don't render anything - we'll draw icons manually
            _minimapCamera.depth = -10;
            
            // Create RenderTexture
            _renderTexture = new RenderTexture(_textureSize, _textureSize, 16);
            _renderTexture.filterMode = FilterMode.Bilinear;
            _minimapCamera.targetTexture = _renderTexture;
            
            // Disable camera rendering (we only need position reference)
            _minimapCamera.enabled = false;
        }
        
        private void CreateMinimapUI()
        {
            // Create Canvas for minimap
            var canvasGO = new GameObject("MinimapCanvas");
            canvasGO.transform.SetParent(transform);
            
            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 99; // Just below main HUD
            
            var canvasScaler = canvasGO.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = new Vector2(1920, 1080);
            canvasScaler.matchWidthOrHeight = 0.5f;
            
            canvasGO.AddComponent<GraphicRaycaster>();
            
            // Create minimap container (background panel)
            var containerGO = new GameObject("MinimapContainer");
            containerGO.transform.SetParent(_canvas.transform, false);
            
            var containerRect = containerGO.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(1, 1); // Top-right
            containerRect.anchorMax = new Vector2(1, 1);
            containerRect.pivot = new Vector2(1, 1);
            containerRect.anchoredPosition = new Vector2(-_minimapOffset.x, -_minimapOffset.y);
            containerRect.sizeDelta = new Vector2(_minimapScreenSize.x + 8, _minimapScreenSize.y + 30);
            
            var containerBg = containerGO.AddComponent<Image>();
            containerBg.color = new Color(0.08f, 0.08f, 0.1f, 0.95f);
            
            // Title
            var titleGO = new GameObject("Title");
            titleGO.transform.SetParent(containerGO.transform, false);
            
            var titleRect = titleGO.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -2);
            titleRect.sizeDelta = new Vector2(0, 22);
            
            var titleText = titleGO.AddComponent<Text>();
            titleText.text = "🗺️ MINIMAP";
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (titleText.font == null)
                titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            titleText.fontSize = 12;
            titleText.fontStyle = FontStyle.Bold;
            titleText.alignment = TextAnchor.MiddleCenter;
            titleText.color = Color.white;
            
            // Minimap background (the "map" area)
            var mapBgGO = new GameObject("MapBackground");
            mapBgGO.transform.SetParent(containerGO.transform, false);
            
            _minimapRect = mapBgGO.AddComponent<RectTransform>();
            _minimapRect.anchorMin = new Vector2(0.5f, 0);
            _minimapRect.anchorMax = new Vector2(0.5f, 0);
            _minimapRect.pivot = new Vector2(0.5f, 0);
            _minimapRect.anchoredPosition = new Vector2(0, 4);
            _minimapRect.sizeDelta = _minimapScreenSize;
            
            var mapBg = mapBgGO.AddComponent<Image>();
            mapBg.color = new Color(0.15f, 0.17f, 0.2f);
            
            // Icons container (on top of map)
            _iconsContainer = new GameObject("Icons");
            _iconsContainer.transform.SetParent(mapBgGO.transform, false);
            
            var iconsRect = _iconsContainer.AddComponent<RectTransform>();
            iconsRect.anchorMin = Vector2.zero;
            iconsRect.anchorMax = Vector2.one;
            iconsRect.sizeDelta = Vector2.zero;
            iconsRect.anchoredPosition = Vector2.zero;
            
            // Border
            var borderGO = new GameObject("Border");
            borderGO.transform.SetParent(mapBgGO.transform, false);
            
            var borderRect = borderGO.AddComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.sizeDelta = Vector2.zero;
            
            var border = borderGO.AddComponent<Outline>();
            var borderImg = borderGO.AddComponent<Image>();
            borderImg.color = Color.clear;
            border.effectColor = new Color(0.3f, 0.35f, 0.4f);
            border.effectDistance = new Vector2(2, 2);
        }
        
        private void CreateIcons()
        {
            // Create cop icons
            foreach (var cop in _cops)
            {
                if (cop == null) continue;
                var icon = CreateIcon($"CopIcon_{cop.name}", _copIconColor, _iconSize);
                _copIcons[cop] = icon;
            }
            
            // Create robber icons
            foreach (var robber in _robbers)
            {
                if (robber == null) continue;
                var icon = CreateIcon($"RobberIcon_{robber.name}", _robberIconColor, _iconSize * 1.2f);
                _robberIcons[robber] = icon;
            }
            
            // Create loot icons
            foreach (var loot in _lootPoints)
            {
                if (loot == null) continue;
                var icon = CreateIcon($"LootIcon_{loot.name}", _lootIconColor, _iconSize * 0.8f, true);
                _lootIcons[loot] = icon;
            }
            
            // Create escape zone icon
            if (_escapeZone != null)
            {
                _escapeIcon = CreateIcon("EscapeIcon", _escapeIconColor, _iconSize * 2f, false, true);
            }
        }
        
        private Image CreateIcon(string name, Color color, float size, bool isDiamond = false, bool isSquare = false)
        {
            var iconGO = new GameObject(name);
            iconGO.transform.SetParent(_iconsContainer.transform, false);
            
            var rect = iconGO.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(size, size);
            
            var image = iconGO.AddComponent<Image>();
            image.color = color;
            
            // Create simple shapes using rotation
            if (isDiamond)
            {
                rect.rotation = Quaternion.Euler(0, 0, 45);
                rect.sizeDelta = new Vector2(size * 0.7f, size * 0.7f);
            }
            
            return image;
        }
        
        private void Update()
        {
            UpdateIconPositions();
        }
        
        private void UpdateIconPositions()
        {
            // Update cop positions
            foreach (var kvp in _copIcons)
            {
                var cop = kvp.Key;
                var icon = kvp.Value;
                
                if (cop == null || !cop.gameObject.activeSelf)
                {
                    icon.gameObject.SetActive(false);
                    continue;
                }
                
                icon.gameObject.SetActive(true);
                UpdateIconPosition(icon.rectTransform, cop.transform.position);
            }
            
            // Update robber positions
            foreach (var kvp in _robberIcons)
            {
                var robber = kvp.Key;
                var icon = kvp.Value;
                
                if (robber == null || !robber.gameObject.activeSelf)
                {
                    icon.gameObject.SetActive(false);
                    continue;
                }
                
                icon.gameObject.SetActive(true);
                
                // Change color if carrying loot
                icon.color = robber.IsCarryingLoot ? _lootIconColor : _robberIconColor;
                
                UpdateIconPosition(icon.rectTransform, robber.transform.position);
            }
            
            // Update loot positions
            foreach (var kvp in _lootIcons)
            {
                var loot = kvp.Key;
                var icon = kvp.Value;
                
                if (loot == null)
                {
                    icon.gameObject.SetActive(false);
                    continue;
                }
                
                icon.color = loot.IsStolen ? _stolenLootColor : _lootIconColor;
                UpdateIconPosition(icon.rectTransform, loot.transform.position);
            }
            
            // Update escape zone
            if (_escapeIcon != null && _escapeZone != null)
            {
                UpdateIconPosition(_escapeIcon.rectTransform, _escapeZone.transform.position);
            }
        }
        
        private void UpdateIconPosition(RectTransform iconRect, Vector3 worldPos)
        {
            // Convert world position to minimap position
            // World is centered at (0,0), minimap origin is bottom-left
            float halfMap = _mapSize / 2f;
            
            // Normalize to 0-1 range
            float normalizedX = (worldPos.x + halfMap) / _mapSize;
            float normalizedZ = (worldPos.z + halfMap) / _mapSize;
            
            // Clamp to minimap bounds
            normalizedX = Mathf.Clamp01(normalizedX);
            normalizedZ = Mathf.Clamp01(normalizedZ);
            
            // Convert to minimap pixel position
            float mapX = (normalizedX - 0.5f) * _minimapScreenSize.x;
            float mapY = (normalizedZ - 0.5f) * _minimapScreenSize.y;
            
            iconRect.anchoredPosition = new Vector2(mapX, mapY);
        }
        
        /// <summary>
        /// Cleanup resources.
        /// </summary>
        public void Cleanup()
        {
            if (_renderTexture != null)
            {
                _renderTexture.Release();
                Object.Destroy(_renderTexture);
            }
            
            _copIcons.Clear();
            _robberIcons.Clear();
            _lootIcons.Clear();
        }
        
        private void OnDestroy()
        {
            Cleanup();
        }
    }
}
