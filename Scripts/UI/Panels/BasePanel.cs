using System;
using System.Collections;
using UnityEngine;

namespace TheColosseumChallenge.UI.Panels
{
    /// <summary>
    /// Abstract base class for all UI panels.
    /// Provides common show/hide functionality and animation support.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class BasePanel : MonoBehaviour
    {
        #region Inspector Fields
        
        [Header("Panel Settings")]
        [SerializeField] protected bool _startHidden = true;
        [SerializeField] protected float _fadeInDuration = 0.25f;
        [SerializeField] protected float _fadeOutDuration = 0.2f;
        
        [Header("Animation Settings")]
        [SerializeField] protected bool _useScaleAnimation = false;
        [SerializeField] protected Vector3 _hiddenScale = new Vector3(0.9f, 0.9f, 1f);
        [SerializeField] protected AnimationCurve _showCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] protected AnimationCurve _hideCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        #endregion

        #region Protected Fields
        
        /// <summary>
        /// Reference to the UIManager.
        /// </summary>
        protected UIManager UIManager { get; private set; }
        
        /// <summary>
        /// CanvasGroup component for fade animations.
        /// </summary>
        private CanvasGroup _canvasGroup;
        protected CanvasGroup CanvasGroup
        {
            get
            {
                if (_canvasGroup == null)
                {
                    _canvasGroup = GetComponent<CanvasGroup>();
                    if (_canvasGroup == null)
                    {
                        _canvasGroup = gameObject.AddComponent<CanvasGroup>();
                    }
                }
                return _canvasGroup;
            }
        }
        
        /// <summary>
        /// RectTransform component for positioning.
        /// </summary>
        private RectTransform _rectTransform;
        protected RectTransform RectTransform
        {
            get
            {
                if (_rectTransform == null)
                {
                    _rectTransform = GetComponent<RectTransform>();
                }
                return _rectTransform;
            }
        }
        
        /// <summary>
        /// Whether the panel is currently visible.
        /// </summary>
        protected bool IsVisible { get; private set; }
        
        /// <summary>
        /// Whether an animation is currently in progress.
        /// </summary>
        protected bool IsAnimating { get; private set; }
        
        /// <summary>
        /// Current animation coroutine.
        /// </summary>
        private Coroutine _animationCoroutine;
        
        /// <summary>
        /// Whether the panel has been initialized.
        /// </summary>
        private bool _isInitialized;
        
        #endregion

        #region Events
        
        /// <summary>
        /// Fired when the panel starts showing.
        /// </summary>
        public event Action OnShowStarted;
        
        /// <summary>
        /// Fired when the panel finishes showing.
        /// </summary>
        public event Action OnShowCompleted;
        
        /// <summary>
        /// Fired when the panel starts hiding.
        /// </summary>
        public event Action OnHideStarted;
        
        /// <summary>
        /// Fired when the panel finishes hiding.
        /// </summary>
        public event Action OnHideCompleted;
        
        #endregion

        #region Unity Lifecycle
        
        protected virtual void Awake()
        {
            // Force component caching
            _ = CanvasGroup;
            _ = RectTransform;
        }
        
        protected virtual void OnDestroy()
        {
            // Clean up coroutine
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
            }
        }
        
        #endregion

        #region Initialization
        
        /// <summary>
        /// Initializes the panel with a reference to the UIManager.
        /// Called by UIManager during setup.
        /// </summary>
        /// <param name="uiManager">The UIManager instance.</param>
        public virtual void Initialize(UIManager uiManager)
        {
            if (_isInitialized)
            {
                Debug.LogWarning($"[{GetType().Name}] Already initialized.");
                return;
            }
            
            UIManager = uiManager;
            
            // Ensure components are available (in case Awake hasn't run)
            _ = CanvasGroup;
            _ = RectTransform;
            
            // Apply initial state
            if (_startHidden)
            {
                SetVisibleImmediate(false);
            }
            else
            {
                SetVisibleImmediate(true);
            }
            
            _isInitialized = true;
            
            OnInitialize();
            
            Debug.Log($"[{GetType().Name}] Initialized.");
        }
        
        /// <summary>
        /// Override this to perform panel-specific initialization.
        /// </summary>
        protected virtual void OnInitialize() { }
        
        #endregion

        #region Show/Hide Methods
        
        /// <summary>
        /// Shows the panel with optional animation.
        /// </summary>
        /// <param name="immediate">If true, skip animation.</param>
        public virtual void Show(bool immediate = false)
        {
            if (IsVisible && !IsAnimating) return;
            
            // Cancel any ongoing animation
            StopAnimation();
            
            gameObject.SetActive(true);
            
            OnShowStarted?.Invoke();
            OnBeforeShow();
            
            if (immediate || _fadeInDuration <= 0)
            {
                SetVisibleImmediate(true);
                OnAfterShow();
                OnShowCompleted?.Invoke();
            }
            else
            {
                _animationCoroutine = StartCoroutine(AnimateShow());
            }
        }
        
        /// <summary>
        /// Hides the panel with optional animation.
        /// </summary>
        /// <param name="immediate">If true, skip animation.</param>
        public virtual void Hide(bool immediate = false)
        {
            if (!IsVisible && !IsAnimating) return;
            
            // Cancel any ongoing animation
            StopAnimation();
            
            OnHideStarted?.Invoke();
            OnBeforeHide();
            
            if (immediate || _fadeOutDuration <= 0)
            {
                SetVisibleImmediate(false);
                OnAfterHide();
                OnHideCompleted?.Invoke();
            }
            else
            {
                _animationCoroutine = StartCoroutine(AnimateHide());
            }
        }
        
        /// <summary>
        /// Toggles the panel visibility.
        /// </summary>
        public void Toggle()
        {
            if (IsVisible)
                Hide();
            else
                Show();
        }
        
        #endregion

        #region Animation Methods
        
        /// <summary>
        /// Coroutine for animated show transition.
        /// </summary>
        private IEnumerator AnimateShow()
        {
            IsAnimating = true;
            float elapsed = 0f;
            
            // Starting values
            float startAlpha = CanvasGroup.alpha;
            Vector3 startScale = _useScaleAnimation ? transform.localScale : Vector3.one;
            
            while (elapsed < _fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = _showCurve.Evaluate(elapsed / _fadeInDuration);
                
                // Animate alpha
                CanvasGroup.alpha = Mathf.Lerp(startAlpha, 1f, t);
                
                // Animate scale if enabled
                if (_useScaleAnimation)
                {
                    transform.localScale = Vector3.Lerp(startScale, Vector3.one, t);
                }
                
                yield return null;
            }
            
            // Ensure final values
            SetVisibleImmediate(true);
            
            IsAnimating = false;
            _animationCoroutine = null;
            
            OnAfterShow();
            OnShowCompleted?.Invoke();
        }
        
        /// <summary>
        /// Coroutine for animated hide transition.
        /// </summary>
        private IEnumerator AnimateHide()
        {
            IsAnimating = true;
            float elapsed = 0f;
            
            // Starting values
            float startAlpha = CanvasGroup.alpha;
            Vector3 startScale = transform.localScale;
            
            while (elapsed < _fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = _hideCurve.Evaluate(elapsed / _fadeOutDuration);
                
                // Animate alpha
                CanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
                
                // Animate scale if enabled
                if (_useScaleAnimation)
                {
                    transform.localScale = Vector3.Lerp(startScale, _hiddenScale, t);
                }
                
                yield return null;
            }
            
            // Ensure final values
            SetVisibleImmediate(false);
            
            IsAnimating = false;
            _animationCoroutine = null;
            
            OnAfterHide();
            OnHideCompleted?.Invoke();
        }
        
        /// <summary>
        /// Stops any running animation.
        /// </summary>
        private void StopAnimation()
        {
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
                _animationCoroutine = null;
                IsAnimating = false;
            }
        }
        
        /// <summary>
        /// Immediately sets the panel visibility without animation.
        /// </summary>
        /// <param name="visible">Target visibility state.</param>
        protected void SetVisibleImmediate(bool visible)
        {
            IsVisible = visible;
            
            // Ensure CanvasGroup is available
            var cg = CanvasGroup;
            if (cg != null)
            {
                cg.alpha = visible ? 1f : 0f;
                cg.interactable = visible;
                cg.blocksRaycasts = visible;
            }
            
            if (_useScaleAnimation)
            {
                transform.localScale = visible ? Vector3.one : _hiddenScale;
            }
            
            if (!visible)
            {
                gameObject.SetActive(false);
            }
        }
        
        #endregion

        #region Override Hooks
        
        /// <summary>
        /// Called before the panel starts showing.
        /// Override to set up panel state.
        /// </summary>
        protected virtual void OnBeforeShow() { }
        
        /// <summary>
        /// Called after the panel has finished showing.
        /// Override to start panel-specific logic.
        /// </summary>
        protected virtual void OnAfterShow() { }
        
        /// <summary>
        /// Called before the panel starts hiding.
        /// Override to save state or clean up.
        /// </summary>
        protected virtual void OnBeforeHide() { }
        
        /// <summary>
        /// Called after the panel has finished hiding.
        /// Override for additional cleanup.
        /// </summary>
        protected virtual void OnAfterHide() { }
        
        #endregion

        #region Utility Methods
        
        /// <summary>
        /// Navigates back using the UIManager.
        /// </summary>
        protected void NavigateBack()
        {
            if (UIManager != null && UIManager.CanNavigateBack)
            {
                UIManager.NavigateBack();
            }
            else
            {
                Debug.LogWarning($"[{GetType().Name}] Cannot navigate back - UIManager is null or no history.");
            }
        }
        
        #endregion
    }
}