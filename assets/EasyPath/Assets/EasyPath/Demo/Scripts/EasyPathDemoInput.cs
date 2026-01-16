using UnityEngine;
using UnityEngine.InputSystem;

namespace EasyPath.Demo
{
    /// <summary>
    /// Centralized input handler for EasyPath demo scenes using Unity Input System.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class EasyPathDemoInput : MonoBehaviour
    {
        private static EasyPathDemoInput _instance;
        
        // Input Actions
        private InputAction _clickAction;
        private InputAction _rightClickAction;
        private InputAction _middleClickAction;
        private InputAction _mousePositionAction;
        private InputAction _rebuildGridAction;
        private InputAction _sendAllRandomAction;
        private InputAction _wanderAction;
        private InputAction _stopAllAction;
        private InputAction _gatherAction;
        private InputAction _scatterAction;
        
        // Input State
        private bool _clickPressed;
        private bool _rightClickPressed;
        private bool _middleClickPressed;
        private bool _rebuildGridPressed;
        private bool _sendAllRandomPressed;
        private bool _wanderPressed;
        private bool _stopAllPressed;
        private bool _gatherPressed;
        private bool _scatterPressed;
        
        // Properties
        public static bool ClickPressed => _instance != null && _instance._clickPressed;
        public static bool RightClickPressed => _instance != null && _instance._rightClickPressed;
        public static bool MiddleClickPressed => _instance != null && _instance._middleClickPressed;
        public static bool RebuildGridPressed => _instance != null && _instance._rebuildGridPressed;
        public static bool SendAllRandomPressed => _instance != null && _instance._sendAllRandomPressed;
        public static bool WanderPressed => _instance != null && _instance._wanderPressed;
        public static bool StopAllPressed => _instance != null && _instance._stopAllPressed;
        public static bool GatherPressed => _instance != null && _instance._gatherPressed;
        public static bool ScatterPressed => _instance != null && _instance._scatterPressed;
        public static Vector2 MousePosition => _instance != null ? _instance._mousePositionAction.ReadValue<Vector2>() : Vector2.zero;
        
        /// <summary>
        /// Initialize the input system. Call this from any demo script's Start/Awake.
        /// </summary>
        public static void Initialize()
        {
            if (_instance != null) return;
            
            GameObject go = new GameObject("EasyPathDemoInput");
            _instance = go.AddComponent<EasyPathDemoInput>();
            DontDestroyOnLoad(go);
        }
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            
            SetupInputActions();
        }
        
        private void SetupInputActions()
        {
            // Mouse buttons
            _clickAction = new InputAction("Click", InputActionType.Button, "<Mouse>/leftButton");
            _rightClickAction = new InputAction("RightClick", InputActionType.Button, "<Mouse>/rightButton");
            _middleClickAction = new InputAction("MiddleClick", InputActionType.Button, "<Mouse>/middleButton");
            _mousePositionAction = new InputAction("MousePosition", InputActionType.Value, "<Mouse>/position");
            
            // Keyboard actions
            _rebuildGridAction = new InputAction("RebuildGrid", InputActionType.Button, "<Keyboard>/r");
            _sendAllRandomAction = new InputAction("SendAllRandom", InputActionType.Button, "<Keyboard>/space");
            _wanderAction = new InputAction("Wander", InputActionType.Button, "<Keyboard>/w");
            _stopAllAction = new InputAction("StopAll", InputActionType.Button, "<Keyboard>/s");
            _gatherAction = new InputAction("Gather", InputActionType.Button, "<Keyboard>/g");
            _scatterAction = new InputAction("Scatter", InputActionType.Button, "<Keyboard>/x");
            
            // Register callbacks
            _clickAction.performed += _ => _clickPressed = true;
            _rightClickAction.performed += _ => _rightClickPressed = true;
            _middleClickAction.performed += _ => _middleClickPressed = true;
            _rebuildGridAction.performed += _ => _rebuildGridPressed = true;
            _sendAllRandomAction.performed += _ => _sendAllRandomPressed = true;
            _wanderAction.performed += _ => _wanderPressed = true;
            _stopAllAction.performed += _ => _stopAllPressed = true;
            _gatherAction.performed += _ => _gatherPressed = true;
            _scatterAction.performed += _ => _scatterPressed = true;
            
            // Enable all actions
            _clickAction.Enable();
            _rightClickAction.Enable();
            _middleClickAction.Enable();
            _mousePositionAction.Enable();
            _rebuildGridAction.Enable();
            _sendAllRandomAction.Enable();
            _wanderAction.Enable();
            _stopAllAction.Enable();
            _gatherAction.Enable();
            _scatterAction.Enable();
        }
        
        private void Update()
        {
            // Clear button press flags at start of frame (before other scripts run due to DefaultExecutionOrder(-100))
            // Callbacks will set flags during this frame, which other scripts can read
            // Then next frame's Update clears them
        }
        
        private void LateUpdate()
        {
            // Clear button press flags at end of frame so they're available for the full frame
            _clickPressed = false;
            _rightClickPressed = false;
            _middleClickPressed = false;
            _rebuildGridPressed = false;
            _sendAllRandomPressed = false;
            _wanderPressed = false;
            _stopAllPressed = false;
            _gatherPressed = false;
            _scatterPressed = false;
        }
        
        private void OnDestroy()
        {
            _clickAction?.Dispose();
            _rightClickAction?.Dispose();
            _middleClickAction?.Dispose();
            _mousePositionAction?.Dispose();
            _rebuildGridAction?.Dispose();
            _sendAllRandomAction?.Dispose();
            _wanderAction?.Dispose();
            _stopAllAction?.Dispose();
            _gatherAction?.Dispose();
            _scatterAction?.Dispose();
            
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
