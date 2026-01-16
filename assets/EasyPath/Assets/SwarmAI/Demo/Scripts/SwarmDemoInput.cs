using UnityEngine;
using UnityEngine.InputSystem;

namespace SwarmAI.Demo
{
    /// <summary>
    /// Centralized input handler for SwarmAI demo scenes using the new Input System.
    /// Provides a singleton-style access pattern with automatic lifecycle management.
    /// </summary>
    [DefaultExecutionOrder(-100)] // Run before other scripts
    public class SwarmDemoInput : MonoBehaviour
    {
        private static SwarmDemoInput _instance;
        private static SwarmDemoInputActions _actions;
        private static bool _isInitialized = false;
        
        // Cached input values
        private static Vector2 _panInput;
        private static Vector2 _scrollInput;
        private static bool _rotateButton;
        private static Vector2 _rotateDelta;
        private static Vector2 _mousePosition;
        private static Vector2 _movement;
        
        // Button press tracking (for WasPressedThisFrame emulation)
        private static bool _clickPressed;
        private static bool _resetPressed;
        private static bool _cancelPressed;
        private static bool _number1Pressed;
        private static bool _number2Pressed;
        private static bool _number3Pressed;
        private static bool _number4Pressed;
        private static bool _number5Pressed;
        private static bool _number6Pressed;
        private static bool _spacePressed;
        private static bool _actionGPressed;
        private static bool _actionHPressed;
        private static bool _actionNPressed;
        private static bool _actionFPressed;
        private static bool _actionXPressed;
        private static bool _plusPressed;
        private static bool _minusPressed;

        
        /// <summary>
        /// Ensures the input system is initialized. Call from any demo script's Awake or Start.
        /// </summary>
        public static void Initialize()
        {
            if (_isInitialized) return;
            
            // Create or find instance
            if (_instance == null)
            {
                var go = new GameObject("[SwarmDemoInput]");
                _instance = go.AddComponent<SwarmDemoInput>();
                DontDestroyOnLoad(go);
            }
            
            // Create actions
            _actions = new SwarmDemoInputActions();
            _actions.Enable();
            
            // Subscribe to button events for press detection
            _actions.Demo.Click.performed += _ => _clickPressed = true;
            _actions.Demo.Reset.performed += _ => _resetPressed = true;
            _actions.Demo.Cancel.performed += _ => _cancelPressed = true;
            _actions.Demo.Number1.performed += _ => _number1Pressed = true;
            _actions.Demo.Number2.performed += _ => _number2Pressed = true;
            _actions.Demo.Number3.performed += _ => _number3Pressed = true;
            _actions.Demo.Number4.performed += _ => _number4Pressed = true;
            _actions.Demo.Number5.performed += _ => _number5Pressed = true;
            _actions.Demo.Number6.performed += _ => _number6Pressed = true;
            _actions.Demo.Space.performed += _ => _spacePressed = true;
            _actions.Demo.ActionG.performed += _ => _actionGPressed = true;
            _actions.Demo.ActionH.performed += _ => _actionHPressed = true;
            _actions.Demo.ActionN.performed += _ => _actionNPressed = true;
            _actions.Demo.ActionF.performed += _ => _actionFPressed = true;
            _actions.Demo.ActionX.performed += _ => _actionXPressed = true;
            _actions.Demo.Plus.performed += _ => _plusPressed = true;
            _actions.Demo.Minus.performed += _ => _minusPressed = true;
            
            _isInitialized = true;
        }
        
        private void Update()
        {
            // Update continuous values
            if (_actions != null)
            {
                _panInput = _actions.Camera.Pan.ReadValue<Vector2>();
                _scrollInput = _actions.Camera.Scroll.ReadValue<Vector2>();
                _rotateButton = _actions.Camera.RotateButton.IsPressed();
                _rotateDelta = _actions.Camera.RotateDelta.ReadValue<Vector2>();
                _mousePosition = _actions.Demo.MousePosition.ReadValue<Vector2>();
                _movement = _actions.Demo.Movement.ReadValue<Vector2>();
            }
        }
        
        private void LateUpdate()
        {
            // Clear button press flags at END of frame so they're available for the full frame
            // Callbacks set these flags during the frame, and other scripts can read them
            // Then LateUpdate clears them for the next frame
            _clickPressed = false;
            _resetPressed = false;
            _cancelPressed = false;
            _number1Pressed = false;
            _number2Pressed = false;
            _number3Pressed = false;
            _number4Pressed = false;
            _number5Pressed = false;
            _number6Pressed = false;
            _spacePressed = false;
            _actionGPressed = false;
            _actionHPressed = false;
            _actionNPressed = false;
            _actionFPressed = false;
            _actionXPressed = false;
            _plusPressed = false;
            _minusPressed = false;
        }
        
        private void OnDestroy()
        {
            if (_actions != null)
            {
                _actions.Disable();
                _actions.Dispose();
                _actions = null;
            }
            _isInitialized = false;
            _instance = null;
        }
        
        #region Camera Input
        
        /// <summary>Arrow keys pan direction as Vector2 (x: left/right, y: up/down mapped to z).</summary>
        public static Vector2 PanInput
        {
            get
            {
                EnsureInitialized();
                return _panInput;
            }
        }
        
        /// <summary>Mouse scroll wheel input. Y component is vertical scroll.</summary>
        public static Vector2 ScrollInput
        {
            get
            {
                EnsureInitialized();
                return _scrollInput;
            }
        }
        
        /// <summary>True while right mouse button is held.</summary>
        public static bool RotateButton
        {
            get
            {
                EnsureInitialized();
                return _rotateButton;
            }
        }
        
        /// <summary>Mouse delta for rotation.</summary>
        public static Vector2 RotateDelta
        {
            get
            {
                EnsureInitialized();
                return _rotateDelta;
            }
        }
        
        #endregion
        
        #region Demo Input
        
        /// <summary>Current mouse position in screen coordinates.</summary>
        public static Vector2 MousePosition
        {
            get
            {
                EnsureInitialized();
                return _mousePosition;
            }
        }
        
        /// <summary>WASD movement input as Vector2 (x: A/D, y: W/S).</summary>
        public static Vector2 Movement
        {
            get
            {
                EnsureInitialized();
                return _movement;
            }
        }
        
        /// <summary>True the frame left mouse button was pressed.</summary>
        public static bool ClickPressed
        {
            get
            {
                EnsureInitialized();
                return _clickPressed;
            }
        }
        
        /// <summary>True the frame R key was pressed.</summary>
        public static bool ResetPressed
        {
            get
            {
                EnsureInitialized();
                return _resetPressed;
            }
        }
        
        /// <summary>True the frame Escape key was pressed.</summary>
        public static bool CancelPressed
        {
            get
            {
                EnsureInitialized();
                return _cancelPressed;
            }
        }
        
        /// <summary>True the frame 1 key was pressed.</summary>
        public static bool Number1Pressed
        {
            get
            {
                EnsureInitialized();
                return _number1Pressed;
            }
        }
        
        /// <summary>True the frame 2 key was pressed.</summary>
        public static bool Number2Pressed
        {
            get
            {
                EnsureInitialized();
                return _number2Pressed;
            }
        }
        
        /// <summary>True the frame 3 key was pressed.</summary>
        public static bool Number3Pressed
        {
            get
            {
                EnsureInitialized();
                return _number3Pressed;
            }
        }
        
        /// <summary>True the frame 4 key was pressed.</summary>
        public static bool Number4Pressed
        {
            get
            {
                EnsureInitialized();
                return _number4Pressed;
            }
        }
        
        /// <summary>True the frame 5 key was pressed.</summary>
        public static bool Number5Pressed
        {
            get
            {
                EnsureInitialized();
                return _number5Pressed;
            }
        }
        
        /// <summary>True the frame 6 key was pressed.</summary>
        public static bool Number6Pressed
        {
            get
            {
                EnsureInitialized();
                return _number6Pressed;
            }
        }
        
        /// <summary>True the frame Space key was pressed.</summary>
        public static bool SpacePressed
        {
            get
            {
                EnsureInitialized();
                return _spacePressed;
            }
        }
        
        /// <summary>True the frame G key was pressed.</summary>
        public static bool ActionGPressed
        {
            get
            {
                EnsureInitialized();
                return _actionGPressed;
            }
        }
        
        /// <summary>True the frame H key was pressed.</summary>
        public static bool ActionHPressed
        {
            get
            {
                EnsureInitialized();
                return _actionHPressed;
            }
        }
        
        /// <summary>True the frame N key was pressed.</summary>
        public static bool ActionNPressed
        {
            get
            {
                EnsureInitialized();
                return _actionNPressed;
            }
        }
        
        /// <summary>True the frame F key was pressed.</summary>
        public static bool ActionFPressed
        {
            get
            {
                EnsureInitialized();
                return _actionFPressed;
            }
        }
        
        /// <summary>True the frame X key was pressed.</summary>
        public static bool ActionXPressed
        {
            get
            {
                EnsureInitialized();
                return _actionXPressed;
            }
        }
        
        /// <summary>True the frame +/= key was pressed.</summary>
        public static bool PlusPressed
        {
            get
            {
                EnsureInitialized();
                return _plusPressed;
            }
        }
        
        /// <summary>True the frame - key was pressed.</summary>
        public static bool MinusPressed
        {
            get
            {
                EnsureInitialized();
                return _minusPressed;
            }
        }
        
        #endregion
        
        private static void EnsureInitialized()
        {
            if (!_isInitialized)
            {
                Initialize();
            }
        }
    }
}
