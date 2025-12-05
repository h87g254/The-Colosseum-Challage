using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Fusion;
using Fusion.Sockets;
using TheColosseumChallenge.Networking;
using TheColosseumChallenge.Core;

namespace TheColosseumChallenge.Player
{
    /// <summary>
    /// ALTERNATIVE VERSION: Uses PlayerInput component directly instead of Input Action References
    /// This version is more reliable and doesn't have the "disappearing references" issue.
    /// 
    /// Setup:
    /// 1. Add PlayerInput component to player prefab
    /// 2. Assign your Input Actions asset to PlayerInput
    /// 3. Set Default Map to "Player"
    /// 4. Set Behavior to "Invoke Unity Events"
    /// 5. That's it! No Input Action Reference fields to assign.
    /// </summary>
    public class PlayerInputHandler : NetworkBehaviour, INetworkRunnerCallbacks
    {
        #region Inspector Fields
        
        [Header("Settings")]
        [SerializeField] private float _lookSensitivityMultiplier = 0.1f;
        
        #endregion

        #region Private Fields
        
        private Vector2 _moveInput;
        private Vector2 _lookInput;
        private bool _jumpPressed;
        private bool _sprintPressed;
        private bool _crouchPressed;
        
        private PlayerInput _playerInput;
        private InputAction _moveAction;
        private InputAction _lookAction;
        private InputAction _jumpAction;
        private InputAction _sprintAction;
        private InputAction _crouchAction;
        private InputAction _pauseAction;
        
        private bool _inputEnabled = true;
        
        #endregion

        #region Unity/Fusion Lifecycle
        
        private void Awake()
        {
            _playerInput = GetComponent<PlayerInput>();
            
            if (_playerInput == null)
            {
                Debug.LogError("[PlayerInputHandler] PlayerInput component not found! Add PlayerInput component and assign your Input Actions asset.");
                return;
            }
            
            // Get actions from PlayerInput component
            var actionMap = _playerInput.actions?.FindActionMap("Player");
            
            if (actionMap == null)
            {
                Debug.LogError("[PlayerInputHandler] 'Player' action map not found! Check your Input Actions asset.");
                return;
            }
            
            // Find all actions
            _moveAction = actionMap.FindAction("Move");
            _lookAction = actionMap.FindAction("Look");
            _jumpAction = actionMap.FindAction("Jump");
            _sprintAction = actionMap.FindAction("Sprint");
            _crouchAction = actionMap.FindAction("Crouch");
            _pauseAction = actionMap.FindAction("Pause");
            
            // Verify actions were found
            if (_moveAction == null) Debug.LogError("[PlayerInputHandler] 'Move' action not found!");
            if (_lookAction == null) Debug.LogError("[PlayerInputHandler] 'Look' action not found!");
            if (_jumpAction == null) Debug.LogError("[PlayerInputHandler] 'Jump' action not found!");
            if (_sprintAction == null) Debug.LogError("[PlayerInputHandler] 'Sprint' action not found!");
            if (_crouchAction == null) Debug.LogError("[PlayerInputHandler] 'Crouch' action not found!");
            if (_pauseAction == null) Debug.LogError("[PlayerInputHandler] 'Pause' action not found!");
            
            Debug.Log("[PlayerInputHandler] Actions loaded from PlayerInput component successfully.");
        }
        
        public override void Spawned()
        {
            if (HasInputAuthority)
            {
                // Register for input callbacks
                Runner.AddCallbacks(this);
                
                // Enable input actions
                EnableInput();
                
                // Subscribe to pause
                SubscribeToPauseInput();
                
                Debug.Log("[PlayerInputHandler] Input handler spawned for local player.");
            }
        }
        
        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (HasInputAuthority)
            {
                runner.RemoveCallbacks(this);
                DisableInput();
                UnsubscribeFromPauseInput();
            }
        }
        
        private void Update()
        {
            if (!HasInputAuthority || !_inputEnabled) return;
            
            // Gather input every frame
            GatherInput();
        }
        
        #endregion

        #region Input Gathering
        
        /// <summary>
        /// Gathers input from the Input System.
        /// </summary>
        private void GatherInput()
        {
            // Check if game is paused
            if (GameManager.Instance != null && GameManager.Instance.IsPaused)
            {
                ResetInput();
                return;
            }
            
            // Movement input
            if (_moveAction != null)
            {
                _moveInput = _moveAction.ReadValue<Vector2>();
            }
            
            // Look input
            if (_lookAction != null)
            {
                _lookInput = _lookAction.ReadValue<Vector2>() * _lookSensitivityMultiplier;
            }
            
            // Button inputs
            if (_jumpAction != null)
            {
                _jumpPressed = _jumpAction.IsPressed();
            }
            
            if (_sprintAction != null)
            {
                _sprintPressed = _sprintAction.IsPressed();
            }
            
            if (_crouchAction != null)
            {
                _crouchPressed = _crouchAction.IsPressed();
            }
        }
        
        /// <summary>
        /// Resets all input values.
        /// </summary>
        private void ResetInput()
        {
            _moveInput = Vector2.zero;
            _lookInput = Vector2.zero;
            _jumpPressed = false;
            _sprintPressed = false;
            _crouchPressed = false;
        }
        
        #endregion

        #region Input Actions Setup
        
        /// <summary>
        /// Enables all input actions.
        /// </summary>
        private void EnableInput()
        {
            _moveAction?.Enable();
            _lookAction?.Enable();
            _jumpAction?.Enable();
            _sprintAction?.Enable();
            _crouchAction?.Enable();
            _pauseAction?.Enable();
            
            _inputEnabled = true;
            
            Debug.Log("[PlayerInputHandler] Input actions enabled.");
        }
        
        /// <summary>
        /// Disables all input actions.
        /// </summary>
        private void DisableInput()
        {
            _moveAction?.Disable();
            _lookAction?.Disable();
            _jumpAction?.Disable();
            _sprintAction?.Disable();
            _crouchAction?.Disable();
            _pauseAction?.Disable();
            
            _inputEnabled = false;
        }
        
        /// <summary>
        /// Subscribes to pause input.
        /// </summary>
        private void SubscribeToPauseInput()
        {
            if (_pauseAction != null)
            {
                _pauseAction.performed += OnPausePerformed;
                Debug.Log("[PlayerInputHandler] Subscribed to pause input.");
            }
        }
        
        /// <summary>
        /// Unsubscribes from pause input.
        /// </summary>
        private void UnsubscribeFromPauseInput()
        {
            if (_pauseAction != null)
            {
                _pauseAction.performed -= OnPausePerformed;
            }
        }
        
        /// <summary>
        /// Handles pause input.
        /// </summary>
        private void OnPausePerformed(InputAction.CallbackContext context)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TogglePause();
            }
        }
        
        #endregion

        #region INetworkRunnerCallbacks Implementation
        
        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
            // Create input data structure
            var inputData = new NetworkInputData
            {
                MoveDirection = _moveInput,
                LookDelta = _lookInput
            };
            
            // Set button states
            inputData.Buttons.Set(NetworkInputData.BUTTON_JUMP, _jumpPressed);
            inputData.Buttons.Set(NetworkInputData.BUTTON_SPRINT, _sprintPressed);
            inputData.Buttons.Set(NetworkInputData.BUTTON_CROUCH, _crouchPressed);
            
            // Set the input
            input.Set(inputData);
        }
        
        // Required interface methods (unused)
        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
        public void OnConnectedToServer(NetworkRunner runner) { }
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        
        #endregion

        #region Public Methods
        
        /// <summary>
        /// Temporarily disables player input (e.g., during cutscenes).
        /// </summary>
        public void SetInputEnabled(bool enabled)
        {
            if (enabled)
            {
                EnableInput();
            }
            else
            {
                DisableInput();
                ResetInput();
            }
        }
        
        /// <summary>
        /// Gets the current look input for camera controller.
        /// </summary>
        public Vector2 GetLookInput()
        {
            return _lookInput;
        }
        
        #endregion
    }
}