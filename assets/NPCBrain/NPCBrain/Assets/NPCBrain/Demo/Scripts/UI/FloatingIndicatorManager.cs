using System.Collections.Generic;
using UnityEngine;
using NPCBrain.Archetypes;

namespace NPCBrain.Demo.UI
{
    /// <summary>
    /// Manages floating status indicators for all NPCs in the scene.
    /// Creates and tracks indicators, handling cleanup on game reset.
    /// </summary>
    public class FloatingIndicatorManager : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool _showIndicators = true;
        
        private List<FloatingStatusIndicator> _indicators = new List<FloatingStatusIndicator>();
        private GameObject _indicatorContainer;
        
        /// <summary>
        /// Whether floating indicators are visible.
        /// </summary>
        public bool ShowIndicators
        {
            get => _showIndicators;
            set
            {
                _showIndicators = value;
                if (_indicatorContainer != null)
                {
                    _indicatorContainer.SetActive(_showIndicators);
                }
            }
        }
        
        /// <summary>
        /// Initializes indicators for all NPCs.
        /// </summary>
        public void Initialize(List<CopNPC> cops, List<RobberNPC> robbers)
        {
            // Create container
            _indicatorContainer = new GameObject("FloatingIndicators");
            _indicatorContainer.transform.SetParent(transform);
            _indicatorContainer.SetActive(_showIndicators);
            
            // Create indicators for cops
            foreach (var cop in cops)
            {
                if (cop == null) continue;
                var indicator = FloatingStatusIndicator.Create(cop, _indicatorContainer.transform);
                _indicators.Add(indicator);
            }
            
            // Create indicators for robbers
            foreach (var robber in robbers)
            {
                if (robber == null) continue;
                var indicator = FloatingStatusIndicator.Create(robber, _indicatorContainer.transform);
                _indicators.Add(indicator);
            }
        }
        
        /// <summary>
        /// Cleanup all indicators.
        /// </summary>
        public void Cleanup()
        {
            foreach (var indicator in _indicators)
            {
                if (indicator != null)
                {
                    Object.Destroy(indicator.gameObject);
                }
            }
            _indicators.Clear();
            
            if (_indicatorContainer != null)
            {
                Object.Destroy(_indicatorContainer);
                _indicatorContainer = null;
            }
        }
        
        private void OnDestroy()
        {
            Cleanup();
        }
    }
}
