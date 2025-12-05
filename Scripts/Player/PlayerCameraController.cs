using UnityEngine;
using Fusion;
using TheColosseumChallenge.Settings;
using TheColosseumChallenge.Core;

namespace TheColosseumChallenge.Player
{
    /// <summary>
    /// Controls the first-person camera for the local player.
    /// Handles mouse look, camera rotation, and FOV settings.
    /// </summary>
    public class PlayerCameraController : NetworkBehaviour
    {
        #region Inspector Fields
        
        [Header("Camera Settings")]
        [SerializeField] private Transform _cameraHolder;
        [SerializeField] private Camera _playerCamera;
        [SerializeField] private float _defaultFOV = 90f;
        [SerializeField] private float _minPitch = -89f;
        [SerializeField] private float _maxPitch = 89f;
        
        [Header("Sensitivity")]
        [SerializeField] private float _baseSensitivity = 2f;
        [SerializeField] private float _aimSensitivityMultiplier = 0.5f;
        
        [Header("Smoothing")]
        [SerializeField] private bool _useSmoothLook = false;
        [SerializeField] private float _smoothTime = 0.03f;
        
        [Header("Head Bob (Optional)")]
        [SerializeField] private bool _useHeadBob = true;
        [SerializeField] private float _bobFrequency = 1.5f;
        [SerializeField] private float _bobAmplitude = 0.05f;
        
        #endregion

        #region Networked Properties
        
        /// <summary>
        /// Networked pitch for remote player head orientation.
        /// </summary>
        [Networked] private float NetworkedPitch { get; set; }
        
        #endregion

        #region Private Fields
        
        private float _currentPitch;
        private float _currentYaw;
        private float _targetPitch;
        private float _targetYaw;
        private float _pitchVelocity;
        private float _yawVelocity;
        
        private float _bobTimer;
        private Vector3 _originalCameraPosition;
        
        private PlayerInputHandler _inputHandler;
        private PlayerController _playerController;
        
        private bool _isAiming;
        
        #endregion

        #region Properties
        
        /// <summary>
        /// Current camera pitch angle.
        /// </summary>
        public float Pitch => _currentPitch;
        
        /// <summary>
        /// Current camera yaw angle.
        /// </summary>
        public float Yaw => _currentYaw;
        
        /// <summary>
        /// The player camera component.
        /// </summary>
        public Camera Camera => _playerCamera;
        
        #endregion

        #region Unity/Fusion Lifecycle
        
        private void Awake()
        {
            _inputHandler = GetComponent<PlayerInputHandler>();
            _playerController = GetComponent<PlayerController>();
            
            if (_cameraHolder != null)
            {
                _originalCameraPosition = _cameraHolder.localPosition;
            }
        }
        
        public override void Spawned()
        {
            Debug.Log($"[PlayerCameraController] Spawned. HasInputAuthority: {HasInputAuthority}");
            
            if (HasInputAuthority)
            {
                SetupLocalCamera();
                
                // Subscribe to settings changes
                if (SettingsManager.Instance != null)
                {
                    SettingsManager.Instance.OnFOVChanged += OnFOVChanged;
                    OnFOVChanged(SettingsManager.Instance.FOV);
                }
                
                // Initialize rotation
                _currentYaw = transform.eulerAngles.y;
                _currentPitch = 0f;
                
                Debug.Log("[PlayerCameraController] Local player camera setup complete.");
            }
            else
            {
                // Disable camera for remote players
                DisableRemoteCamera();
                Debug.Log("[PlayerCameraController] Remote player camera disabled.");
            }
        }
        
        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (SettingsManager.Instance != null)
            {
                SettingsManager.Instance.OnFOVChanged -= OnFOVChanged;
            }
        }
        
        public override void FixedUpdateNetwork()
        {
            if (HasInputAuthority)
            {
                ProcessLookInput();
                ApplyRotation();
                
                // Sync pitch for remote players
                NetworkedPitch = _currentPitch;
            }
        }
        
        public override void Render()
        {
            if (HasInputAuthority)
            {
                // Apply head bob effect
                if (_useHeadBob)
                {
                    UpdateHeadBob();
                }
            }
            else
            {
                // Update remote player head orientation
                UpdateRemotePlayerHead();
            }
        }
        
        #endregion

        #region Camera Setup
        
        /// <summary>
        /// FIXED: Sets up the camera for the local player with improved scene camera handling.
        /// </summary>
        private void SetupLocalCamera()
        {
            Debug.Log("[PlayerCameraController] Setting up local camera...");
            
            // 1. CRITICAL FIX: Find and disable ALL other cameras in the scene
            Camera[] allCameras = FindObjectsOfType<Camera>(true); // Include inactive
            foreach (Camera cam in allCameras)
            {
                if (cam != _playerCamera)
                {
                    Debug.Log($"[PlayerCameraController] Disabling camera: {cam.gameObject.name}");
                    cam.gameObject.SetActive(false);
                }
            }

            // 2. Create camera if not assigned
            if (_playerCamera == null)
            {
                if (_cameraHolder != null)
                {
                    Debug.Log("[PlayerCameraController] Creating player camera...");
                    GameObject cameraObj = new GameObject("PlayerCamera");
                    cameraObj.transform.SetParent(_cameraHolder);
                    cameraObj.transform.localPosition = Vector3.zero;
                    cameraObj.transform.localRotation = Quaternion.identity;
            
                    _playerCamera = cameraObj.AddComponent<Camera>();
                    
                    // Add AudioListener
                    var existingListener = FindObjectOfType<AudioListener>();
                    if (existingListener != null && existingListener.gameObject != cameraObj)
                    {
                        Destroy(existingListener);
                    }
                    cameraObj.AddComponent<AudioListener>();
                }
                else
                {
                    Debug.LogError("[PlayerCameraController] Camera holder not assigned!");
                }
            }
    
            // 3. Configure Camera
            if (_playerCamera != null)
            {
                _playerCamera.enabled = true;
                _playerCamera.fieldOfView = _defaultFOV;
                _playerCamera.tag = "MainCamera";
                
                Debug.Log($"[PlayerCameraController] Camera configured - FOV: {_defaultFOV}");
            }
    
            // 4. CRITICAL FIX: Lock Cursor immediately and verify
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            Debug.Log($"[PlayerCameraController] Cursor locked - LockState: {Cursor.lockState}, Visible: {Cursor.visible}");
            
            Debug.Log("[PlayerCameraController] Local camera setup complete!");
        }
        
        /// <summary>
        /// Disables camera components for remote players.
        /// </summary>
        private void DisableRemoteCamera()
        {
            if (_playerCamera != null)
            {
                _playerCamera.enabled = false;
            }
            
            var audioListener = GetComponentInChildren<AudioListener>();
            if (audioListener != null)
            {
                audioListener.enabled = false;
            }
        }
        
        #endregion

        #region Look Processing
        
        /// <summary>
        /// Processes look input from the input handler.
        /// </summary>
        private void ProcessLookInput()
        {
            // Don't process look while paused
            if (GameManager.Instance != null && GameManager.Instance.IsPaused)
            {
                return;
            }
            
            if (_inputHandler == null) return;
            
            Vector2 lookInput = _inputHandler.GetLookInput();
            
            // DEBUG: Log if we're not getting input
            if (lookInput.sqrMagnitude == 0 && Time.frameCount % 60 == 0)
            {
                Debug.LogWarning("[PlayerCameraController] No look input detected!");
            }
            
            // Apply sensitivity
            float sensitivity = _baseSensitivity;
            
            if (SettingsManager.Instance != null)
            {
                sensitivity *= SettingsManager.Instance.MouseSensitivity;
            }
            
            if (_isAiming)
            {
                sensitivity *= _aimSensitivityMultiplier;
            }
            
            // Calculate target rotation
            float yawDelta = lookInput.x * sensitivity;
            float pitchDelta = lookInput.y * sensitivity;
            
            // Apply invert Y if enabled
            if (SettingsManager.Instance != null && SettingsManager.Instance.InvertY)
            {
                pitchDelta = -pitchDelta;
            }
            
            _targetYaw += yawDelta;
            _targetPitch -= pitchDelta; // Invert for natural mouse feel
            
            // Clamp pitch
            _targetPitch = Mathf.Clamp(_targetPitch, _minPitch, _maxPitch);
        }
        
        /// <summary>
        /// Applies rotation to player and camera.
        /// </summary>
        private void ApplyRotation()
        {
            if (_useSmoothLook)
            {
                // Smooth rotation
                _currentYaw = Mathf.SmoothDampAngle(_currentYaw, _targetYaw, ref _yawVelocity, _smoothTime);
                _currentPitch = Mathf.SmoothDampAngle(_currentPitch, _targetPitch, ref _pitchVelocity, _smoothTime);
            }
            else
            {
                // Direct rotation
                _currentYaw = _targetYaw;
                _currentPitch = _targetPitch;
            }
            
            // Apply yaw to player body (horizontal rotation)
            transform.rotation = Quaternion.Euler(0f, _currentYaw, 0f);
            
            // Apply pitch to camera holder (vertical rotation)
            if (_cameraHolder != null)
            {
                _cameraHolder.localRotation = Quaternion.Euler(_currentPitch, 0f, 0f);
            }
        }
        
        #endregion

        #region Head Bob
        
        /// <summary>
        /// Updates the head bob effect based on movement.
        /// </summary>
        private void UpdateHeadBob()
        {
            if (_playerController == null || _cameraHolder == null) return;
            
            float speed = _playerController.CurrentSpeed;
            
            if (speed > 0.1f)
            {
                // Increase bob timer based on speed
                _bobTimer += Time.deltaTime * _bobFrequency * (speed / 5f);
                
                // Calculate bob offset
                float bobY = Mathf.Sin(_bobTimer) * _bobAmplitude;
                float bobX = Mathf.Cos(_bobTimer * 0.5f) * _bobAmplitude * 0.5f;
                
                // Apply bob
                Vector3 bobOffset = new Vector3(bobX, bobY, 0f);
                _cameraHolder.localPosition = _originalCameraPosition + bobOffset;
            }
            else
            {
                // Smoothly return to original position
                _cameraHolder.localPosition = Vector3.Lerp(
                    _cameraHolder.localPosition,
                    _originalCameraPosition,
                    Time.deltaTime * 5f
                );
                
                // Slowly reset timer
                _bobTimer = Mathf.Lerp(_bobTimer, 0f, Time.deltaTime * 2f);
            }
        }
        
        #endregion

        #region Remote Player
        
        /// <summary>
        /// Updates the head orientation for remote players.
        /// </summary>
        private void UpdateRemotePlayerHead()
        {
            if (_cameraHolder != null)
            {
                _cameraHolder.localRotation = Quaternion.Euler(NetworkedPitch, 0f, 0f);
            }
        }
        
        #endregion

        #region Settings Callbacks
        
        /// <summary>
        /// Called when FOV setting changes.
        /// </summary>
        /// <param name="fov">New FOV value.</param>
        private void OnFOVChanged(float fov)
        {
            if (_playerCamera != null)
            {
                _playerCamera.fieldOfView = fov;
            }
        }
        
        #endregion

        #region Public Methods
        
        /// <summary>
        /// Sets the aim state for sensitivity adjustment.
        /// </summary>
        /// <param name="isAiming">Whether the player is aiming.</param>
        public void SetAiming(bool isAiming)
        {
            _isAiming = isAiming;
        }
        
        /// <summary>
        /// Sets the camera rotation directly.
        /// </summary>
        /// <param name="pitch">Vertical angle.</param>
        /// <param name="yaw">Horizontal angle.</param>
        public void SetRotation(float pitch, float yaw)
        {
            _currentPitch = _targetPitch = Mathf.Clamp(pitch, _minPitch, _maxPitch);
            _currentYaw = _targetYaw = yaw;
            
            ApplyRotation();
        }
        
        /// <summary>
        /// Adds a camera shake effect.
        /// </summary>
        /// <param name="intensity">Shake intensity.</param>
        /// <param name="duration">Shake duration.</param>
        public void AddCameraShake(float intensity, float duration)
        {
            StartCoroutine(CameraShakeCoroutine(intensity, duration));
        }
        
        private System.Collections.IEnumerator CameraShakeCoroutine(float intensity, float duration)
        {
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                float x = Random.Range(-intensity, intensity);
                float y = Random.Range(-intensity, intensity);
                
                if (_playerCamera != null)
                {
                    _playerCamera.transform.localPosition = new Vector3(x, y, 0);
                }
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            if (_playerCamera != null)
            {
                _playerCamera.transform.localPosition = Vector3.zero;
            }
        }
        
        #endregion
    }
}