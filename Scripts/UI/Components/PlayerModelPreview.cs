using UnityEngine;
using TheColosseumChallenge.Data;

namespace TheColosseumChallenge.UI.Components
{
    /// <summary>
    /// Controls the 3D player model preview displayed in the main menu.
    /// Handles appearance updates, rotation, and pose animations.
    /// </summary>
    public class PlayerModelPreview : MonoBehaviour
    {
        #region Inspector References
        
        [Header("Model References")]
        [SerializeField] private Transform _modelRoot;
        [SerializeField] private Renderer _bodyRenderer;
        [SerializeField] private Transform _hatAttachPoint;
        [SerializeField] private Transform _glassesAttachPoint;
        [SerializeField] private Transform _accessoryAttachPoint;
        
        [Header("Cosmetic Prefabs")]
        [SerializeField] private GameObject[] _hatPrefabs;
        [SerializeField] private GameObject[] _glassesPrefabs;
        [SerializeField] private GameObject[] _accessoryPrefabs;
        
        [Header("Rotation Settings")]
        [SerializeField] private bool _autoRotate = true;
        [SerializeField] private float _rotationSpeed = 20f;
        [SerializeField] private bool _allowDrag = true;
        [SerializeField] private float _dragSensitivity = 0.5f;
        
        [Header("Animation")]
        [SerializeField] private Animator _animator;
        [SerializeField] private string[] _idleAnimations;
        [SerializeField] private float _animationChangeInterval = 5f;
        
        #endregion

        #region Private Fields
        
        private GameObject _currentHat;
        private GameObject _currentGlasses;
        private GameObject _currentAccessory;
        
        private float _currentRotation;
        private float _targetRotation;
        private bool _isDragging;
        private Vector3 _lastMousePosition;
        
        private float _animationTimer;
        private int _currentAnimationIndex;
        
        private static readonly int MainColorProperty = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorProperty = Shader.PropertyToID("_EmissionColor");
        
        #endregion

        #region Unity Lifecycle
        
        private void Start()
        {
            _currentRotation = _modelRoot != null ? _modelRoot.eulerAngles.y : 0f;
            _targetRotation = _currentRotation;
            _animationTimer = _animationChangeInterval;
        }
        
        private void Update()
        {
            HandleRotation();
            HandleAnimation();
        }
        
        #endregion

        #region Public Methods
        
        /// <summary>
        /// Updates the player model appearance based on customization data.
        /// </summary>
        /// <param name="customization">The customization data to apply.</param>
        public void UpdateAppearance(PlayerCustomizationData customization)
        {
            if (customization == null) return;
            
            // Apply color
            ApplyColor(customization.playerColor);
            
            // Apply cosmetics
            ApplyHat(customization.hatIndex);
            ApplyGlasses(customization.glassesIndex);
            ApplyAccessory(customization.accessoryIndex);
            
            // Trigger pose change for visual feedback
            TriggerRandomPose();
        }
        
        /// <summary>
        /// Sets a specific pose on the model.
        /// </summary>
        /// <param name="poseIndex">Index of the pose animation.</param>
        public void SetPose(int poseIndex)
        {
            if (_animator == null || _idleAnimations == null || _idleAnimations.Length == 0)
                return;
            
            int index = Mathf.Clamp(poseIndex, 0, _idleAnimations.Length - 1);
            _animator.CrossFade(_idleAnimations[index], 0.3f);
            _currentAnimationIndex = index;
        }
        
        /// <summary>
        /// Triggers a random pose change.
        /// </summary>
        public void TriggerRandomPose()
        {
            if (_idleAnimations != null && _idleAnimations.Length > 0)
            {
                int newIndex;
                do
                {
                    newIndex = Random.Range(0, _idleAnimations.Length);
                } while (newIndex == _currentAnimationIndex && _idleAnimations.Length > 1);
                
                SetPose(newIndex);
            }
        }
        
        /// <summary>
        /// Resets the model rotation to default.
        /// </summary>
        public void ResetRotation()
        {
            _targetRotation = 0f;
        }
        
        #endregion

        #region Appearance Application
        
        /// <summary>
        /// Applies a color to the player model.
        /// </summary>
        /// <param name="color">The color to apply.</param>
        private void ApplyColor(Color color)
        {
            if (_bodyRenderer == null) return;
            
            // Create material instance if needed
            Material material = _bodyRenderer.material;
            
            // Apply color to main color property
            if (material.HasProperty(MainColorProperty))
            {
                material.SetColor(MainColorProperty, color);
            }
            else if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }
            
            // Apply slight emission for visual pop
            if (material.HasProperty(EmissionColorProperty))
            {
                material.SetColor(EmissionColorProperty, color * 0.1f);
            }
        }
        
        /// <summary>
        /// Applies a hat to the player model.
        /// </summary>
        /// <param name="hatIndex">Index of the hat (-1 for none).</param>
        private void ApplyHat(int hatIndex)
        {
            // Remove current hat
            if (_currentHat != null)
            {
                Destroy(_currentHat);
                _currentHat = null;
            }
            
            // Apply new hat
            if (hatIndex >= 0 && hatIndex < _hatPrefabs?.Length && _hatAttachPoint != null)
            {
                _currentHat = Instantiate(_hatPrefabs[hatIndex], _hatAttachPoint);
                _currentHat.transform.localPosition = Vector3.zero;
                _currentHat.transform.localRotation = Quaternion.identity;
            }
        }
        
        /// <summary>
        /// Applies glasses to the player model.
        /// </summary>
        /// <param name="glassesIndex">Index of the glasses (-1 for none).</param>
        private void ApplyGlasses(int glassesIndex)
        {
            // Remove current glasses
            if (_currentGlasses != null)
            {
                Destroy(_currentGlasses);
                _currentGlasses = null;
            }
            
            // Apply new glasses
            if (glassesIndex >= 0 && glassesIndex < _glassesPrefabs?.Length && _glassesAttachPoint != null)
            {
                _currentGlasses = Instantiate(_glassesPrefabs[glassesIndex], _glassesAttachPoint);
                _currentGlasses.transform.localPosition = Vector3.zero;
                _currentGlasses.transform.localRotation = Quaternion.identity;
            }
        }
        
        /// <summary>
        /// Applies an accessory to the player model.
        /// </summary>
        /// <param name="accessoryIndex">Index of the accessory (-1 for none).</param>
        private void ApplyAccessory(int accessoryIndex)
        {
            // Remove current accessory
            if (_currentAccessory != null)
            {
                Destroy(_currentAccessory);
                _currentAccessory = null;
            }
            
            // Apply new accessory
            if (accessoryIndex >= 0 && accessoryIndex < _accessoryPrefabs?.Length && _accessoryAttachPoint != null)
            {
                _currentAccessory = Instantiate(_accessoryPrefabs[accessoryIndex], _accessoryAttachPoint);
                _currentAccessory.transform.localPosition = Vector3.zero;
                _currentAccessory.transform.localRotation = Quaternion.identity;
            }
        }
        
        #endregion

        #region Rotation Handling
        
        /// <summary>
        /// Handles model rotation (auto-rotate and drag).
        /// </summary>
        private void HandleRotation()
        {
            if (_modelRoot == null) return;
            
            // Handle drag input
            if (_allowDrag)
            {
                HandleDragInput();
            }
            
            // Auto-rotate when not dragging
            if (_autoRotate && !_isDragging)
            {
                _targetRotation += _rotationSpeed * Time.deltaTime;
            }
            
            // Smooth rotation
            _currentRotation = Mathf.LerpAngle(_currentRotation, _targetRotation, Time.deltaTime * 10f);
            _modelRoot.localRotation = Quaternion.Euler(0f, _currentRotation, 0f);
        }
        
        /// <summary>
        /// Handles mouse drag input for rotation.
        /// </summary>
        private void HandleDragInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                // Check if we're over this object (simplified check)
                _isDragging = true;
                _lastMousePosition = Input.mousePosition;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                _isDragging = false;
            }
            
            if (_isDragging)
            {
                Vector3 delta = Input.mousePosition - _lastMousePosition;
                _targetRotation -= delta.x * _dragSensitivity;
                _lastMousePosition = Input.mousePosition;
            }
        }
        
        #endregion

        #region Animation Handling
        
        /// <summary>
        /// Handles automatic animation changes.
        /// </summary>
        private void HandleAnimation()
        {
            if (_animator == null || _idleAnimations == null || _idleAnimations.Length <= 1)
                return;
            
            _animationTimer -= Time.deltaTime;
            
            if (_animationTimer <= 0)
            {
                TriggerRandomPose();
                _animationTimer = _animationChangeInterval + Random.Range(-1f, 1f);
            }
        }
        
        #endregion
    }
}