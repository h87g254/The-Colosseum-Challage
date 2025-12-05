using UnityEngine;
using Fusion;
using TheColosseumChallenge.Networking;

namespace TheColosseumChallenge.Player
{
    /// <summary>
    /// Networked player movement controller.
    /// Handles all physics-based player movement with network synchronization.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : NetworkBehaviour
    {
        #region Inspector Fields
        
        [Header("Movement Settings")]
        [SerializeField] private float _walkSpeed = 5f;
        [SerializeField] private float _sprintSpeed = 8f;
        [SerializeField] private float _crouchSpeed = 2.5f;
        [SerializeField] private float _acceleration = 10f;
        [SerializeField] private float _deceleration = 10f;
        
        [Header("Jump Settings")]
        [SerializeField] private float _jumpForce = 7f;
        [SerializeField] private float _gravity = -20f;
        [SerializeField] private float _groundCheckDistance = 0.2f;
        [SerializeField] private LayerMask _groundMask;
        
        [Header("Crouch Settings")]
        [SerializeField] private float _standingHeight = 2f;
        [SerializeField] private float _crouchingHeight = 1f;
        [SerializeField] private float _crouchTransitionSpeed = 10f;
        
        [Header("References")]
        [SerializeField] private Transform _cameraHolder;
        [SerializeField] private Transform _playerModel;
        
        #endregion

        #region Networked Properties
        
        /// <summary>
        /// Networked velocity for smooth interpolation.
        /// </summary>
        [Networked] private Vector3 Velocity { get; set; }
        
        /// <summary>
        /// Networked crouch state.
        /// </summary>
        [Networked] private NetworkBool IsCrouching { get; set; }
        
        /// <summary>
        /// Networked grounded state.
        /// </summary>
        [Networked] private NetworkBool IsGrounded { get; set; }
        
        #endregion

        #region Private Fields
        
        private CharacterController _characterController;
        private float _currentHeight;
        private Vector3 _currentVelocity;
        private float _verticalVelocity;
        
        #endregion

        #region Properties
        
        /// <summary>
        /// Current movement speed based on state.
        /// </summary>
        public float CurrentSpeed => IsCrouching ? _crouchSpeed : 
            (_currentVelocity.magnitude > _walkSpeed + 0.1f ? _sprintSpeed : _walkSpeed);
        
        #endregion

        #region Unity/Fusion Lifecycle
        
        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            _currentHeight = _standingHeight;
        }
        
        public override void Spawned()
        {
            Debug.Log($"[PlayerController] Player spawned. HasInputAuthority: {HasInputAuthority}");
            
            if (HasInputAuthority)
            {
                // Setup local player camera
                SetupLocalPlayer();
            }
            else
            {
                // Disable input for remote players
                DisableLocalComponents();
            }
        }
        
        public override void FixedUpdateNetwork()
        {
            // Only process input if we have input authority
            if (GetInput(out NetworkInputData input))
            {
                ProcessMovement(input);
                ProcessJump(input);
                ProcessCrouch(input);
            }
            
            ApplyGravity();
            UpdateCrouchHeight();
            MoveCharacter();
        }
        
        public override void Render()
        {
            // Smooth visual updates between network ticks
            InterpolateVisuals();
        }
        
        #endregion

        #region Setup Methods
        
        /// <summary>
        /// Sets up components for the local player.
        /// </summary>
        private void SetupLocalPlayer()
        {
            // Camera will be setup by PlayerCameraController
            
            // Lock cursor for gameplay
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        
        /// <summary>
        /// Disables local-only components for remote players.
        /// </summary>
        private void DisableLocalComponents()
        {
            // Disable audio listener if present
            var audioListener = GetComponentInChildren<AudioListener>();
            if (audioListener != null)
            {
                audioListener.enabled = false;
            }
        }
        
        #endregion

        #region Movement Processing
        
        /// <summary>
        /// Processes movement input and calculates velocity.
        /// </summary>
        /// <param name="input">The network input data.</param>
        private void ProcessMovement(NetworkInputData input)
        {
            // Get move direction relative to camera
            Vector3 moveInput = new Vector3(input.MoveDirection.x, 0f, input.MoveDirection.y);
            
            // Transform to world space based on player rotation
            Vector3 moveDirection = transform.TransformDirection(moveInput);
            moveDirection.y = 0f;
            moveDirection.Normalize();
            
            // Calculate target speed
            float targetSpeed = 0f;
            
            if (moveDirection.magnitude > 0.1f)
            {
                if (IsCrouching)
                {
                    targetSpeed = _crouchSpeed;
                }
                else if (input.IsSprintPressed)
                {
                    targetSpeed = _sprintSpeed;
                }
                else
                {
                    targetSpeed = _walkSpeed;
                }
            }
            
            // Calculate target velocity
            Vector3 targetVelocity = moveDirection * targetSpeed;
            
            // Smooth acceleration/deceleration
            float smoothRate = targetSpeed > _currentVelocity.magnitude ? _acceleration : _deceleration;
            _currentVelocity = Vector3.Lerp(
                _currentVelocity,
                targetVelocity,
                smoothRate * Runner.DeltaTime
            );
            
            // Update networked velocity (horizontal only)
            Velocity = new Vector3(_currentVelocity.x, Velocity.y, _currentVelocity.z);
        }
        
        /// <summary>
        /// Processes jump input.
        /// </summary>
        /// <param name="input">The network input data.</param>
        private void ProcessJump(NetworkInputData input)
        {
            // Check if grounded
            CheckGrounded();
            
            // Handle jump
            if (input.IsJumpPressed && IsGrounded && !IsCrouching)
            {
                _verticalVelocity = _jumpForce;
                IsGrounded = false;
                
                Debug.Log("[PlayerController] Jump!");
            }
        }
        
        /// <summary>
        /// Processes crouch input.
        /// </summary>
        /// <param name="input">The network input data.</param>
        private void ProcessCrouch(NetworkInputData input)
        {
            bool wantsToCrouch = input.IsCrouchPressed;
            
            // Check if we can stand up
            if (IsCrouching && !wantsToCrouch)
            {
                // Check for overhead obstacles
                if (CanStandUp())
                {
                    IsCrouching = false;
                }
            }
            else if (!IsCrouching && wantsToCrouch && IsGrounded)
            {
                IsCrouching = true;
            }
        }
        
        #endregion

        #region Physics Methods
        
        /// <summary>
        /// Applies gravity to vertical velocity.
        /// </summary>
        private void ApplyGravity()
        {
            if (IsGrounded && _verticalVelocity < 0)
            {
                _verticalVelocity = -2f; // Small downward force to stay grounded
            }
            else
            {
                _verticalVelocity += _gravity * Runner.DeltaTime;
            }
        }
        
        /// <summary>
        /// Checks if the player is on the ground.
        /// </summary>
        private void CheckGrounded()
        {
            Vector3 spherePosition = transform.position + Vector3.down * (_characterController.height / 2f - _characterController.radius);
            IsGrounded = Physics.CheckSphere(spherePosition, _characterController.radius + _groundCheckDistance, _groundMask);
        }
        
        /// <summary>
        /// Checks if there's room to stand up.
        /// </summary>
        /// <returns>True if the player can stand up.</returns>
        private bool CanStandUp()
        {
            float checkDistance = _standingHeight - _crouchingHeight;
            Vector3 checkStart = transform.position + Vector3.up * _crouchingHeight;
            
            return !Physics.Raycast(checkStart, Vector3.up, checkDistance);
        }
        
        /// <summary>
        /// Moves the character controller.
        /// </summary>
        private void MoveCharacter()
        {
            Vector3 motion = new Vector3(_currentVelocity.x, _verticalVelocity, _currentVelocity.z);
            _characterController.Move(motion * Runner.DeltaTime);
        }
        
        /// <summary>
        /// Updates the character height for crouching.
        /// </summary>
        private void UpdateCrouchHeight()
        {
            float targetHeight = IsCrouching ? _crouchingHeight : _standingHeight;
            
            if (Mathf.Abs(_currentHeight - targetHeight) > 0.01f)
            {
                _currentHeight = Mathf.Lerp(_currentHeight, targetHeight, _crouchTransitionSpeed * Runner.DeltaTime);
                _characterController.height = _currentHeight;
                
                // Adjust center
                _characterController.center = new Vector3(0f, _currentHeight / 2f, 0f);
                
                // Adjust camera holder position
                if (_cameraHolder != null)
                {
                    float cameraY = _currentHeight - 0.2f;
                    _cameraHolder.localPosition = new Vector3(0f, cameraY, 0f);
                }
            }
        }
        
        #endregion

        #region Visual Interpolation
        
        /// <summary>
        /// Interpolates visual elements between network ticks.
        /// </summary>
        private void InterpolateVisuals()
        {
            // Can be used for smoothing animations, model tilting, etc.
        }
        
        #endregion

        #region Public Methods
        
        /// <summary>
        /// Teleports the player to a position.
        /// </summary>
        /// <param name="position">Target position.</param>
        /// <param name="rotation">Target rotation.</param>
        public void Teleport(Vector3 position, Quaternion rotation)
        {
            _characterController.enabled = false;
            transform.position = position;
            transform.rotation = rotation;
            _characterController.enabled = true;
            
            _currentVelocity = Vector3.zero;
            _verticalVelocity = 0f;
        }
        
        #endregion

        #region Debug
        
        private void OnDrawGizmosSelected()
        {
            // Draw ground check sphere
            if (_characterController != null)
            {
                Gizmos.color = IsGrounded ? Color.green : Color.red;
                Vector3 spherePosition = transform.position + Vector3.down * (_characterController.height / 2f - _characterController.radius);
                Gizmos.DrawWireSphere(spherePosition, _characterController.radius + _groundCheckDistance);
            }
        }
        
        #endregion
    }
}