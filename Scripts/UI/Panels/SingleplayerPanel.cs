using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TheColosseumChallenge.Core;

namespace TheColosseumChallenge.UI.Panels
{
    /// <summary>
    /// Popup panel for setting up a singleplayer game session.
    /// </summary>
    public class SingleplayerPanel : BasePanel
    {
        #region Inspector References
        
        [Header("UI Elements")]
        [SerializeField] private TMP_InputField _wavesInput;
        [SerializeField] private Toggle _infiniteWavesToggle;
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _backButton;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _wavesLabel;
        
        [Header("Background")]
        [SerializeField] private Image _backgroundOverlay;
        [SerializeField] private Button _backgroundCloseButton;
        
        [Header("Settings")]
        [SerializeField] private int _defaultWaveCount = 10;
        [SerializeField] private int _minWaveCount = 1;
        [SerializeField] private int _maxWaveCount = 999;
        
        #endregion

        #region Private Fields
        
        private int _selectedWaveCount;
        private bool _isInfinite;
        
        #endregion

        #region Unity Lifecycle
        
        protected override void Awake()
        {
            base.Awake();
            
            SetupListeners();
            
            _selectedWaveCount = _defaultWaveCount;
            _isInfinite = false;
        }
        
        #endregion

        #region Initialization
        
        private void SetupListeners()
        {
            if (_wavesInput != null)
            {
                _wavesInput.contentType = TMP_InputField.ContentType.IntegerNumber;
                _wavesInput.characterLimit = 3;
                _wavesInput.onValueChanged.AddListener(OnWavesInputChanged);
                _wavesInput.onEndEdit.AddListener(OnWavesInputEndEdit);
            }
            
            if (_infiniteWavesToggle != null)
            {
                _infiniteWavesToggle.onValueChanged.AddListener(OnInfiniteToggleChanged);
            }
            
            if (_startButton != null)
            {
                _startButton.onClick.RemoveAllListeners();
                _startButton.onClick.AddListener(OnStartClicked);
            }
                
            if (_backButton != null)
            {
                _backButton.onClick.RemoveAllListeners();
                _backButton.onClick.AddListener(OnBackClicked);
            }
                
            if (_backgroundCloseButton != null)
            {
                _backgroundCloseButton.onClick.RemoveAllListeners();
                _backgroundCloseButton.onClick.AddListener(OnBackClicked);
            }
            
            Debug.Log("[SingleplayerPanel] Listeners set up.");
        }
        
        protected override void OnInitialize()
        {
            base.OnInitialize();
            Debug.Log("[SingleplayerPanel] Initialized.");
        }
        
        #endregion

        #region Panel Lifecycle
        
        protected override void OnBeforeShow()
        {
            base.OnBeforeShow();
            
            _selectedWaveCount = _defaultWaveCount;
            _isInfinite = false;
            
            UpdateUI();
        }
        
        protected override void OnAfterShow()
        {
            base.OnAfterShow();
            
            if (_wavesInput != null && !_isInfinite)
            {
                _wavesInput.Select();
                _wavesInput.ActivateInputField();
            }
        }
        
        #endregion

        #region Input Handlers
        
        private void OnWavesInputChanged(string value)
        {
            if (int.TryParse(value, out int waves))
            {
                _selectedWaveCount = waves;
            }
        }
        
        private void OnWavesInputEndEdit(string value)
        {
            if (int.TryParse(value, out int waves))
            {
                _selectedWaveCount = Mathf.Clamp(waves, _minWaveCount, _maxWaveCount);
            }
            else
            {
                _selectedWaveCount = _defaultWaveCount;
            }
            
            if (_wavesInput != null)
            {
                _wavesInput.text = _selectedWaveCount.ToString();
            }
        }
        
        private void OnInfiniteToggleChanged(bool isOn)
        {
            _isInfinite = isOn;
            UpdateUI();
        }
        
        #endregion

        #region Button Handlers
        
        private void OnStartClicked()
        {
            int waveCount = _isInfinite ? -1 : _selectedWaveCount;
            
            Debug.Log($"[SingleplayerPanel] Starting singleplayer with {(_isInfinite ? "infinite" : waveCount.ToString())} waves.");
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.StartSingleplayerGame(waveCount);
            }
            else
            {
                Debug.LogError("[SingleplayerPanel] GameManager not found!");
            }
        }
        
        private void OnBackClicked()
        {
            Debug.Log("[SingleplayerPanel] Back clicked.");
            NavigateBack();
        }
        
        #endregion

        #region UI Updates
        
        private void UpdateUI()
        {
            if (_wavesInput != null)
            {
                _wavesInput.interactable = !_isInfinite;
                _wavesInput.text = _isInfinite ? "∞" : _selectedWaveCount.ToString();
            }
            
            if (_infiniteWavesToggle != null)
            {
                _infiniteWavesToggle.SetIsOnWithoutNotify(_isInfinite);
            }
            
            if (_wavesLabel != null)
            {
                _wavesLabel.color = _isInfinite ? new Color(1, 1, 1, 0.5f) : Color.white;
            }
        }
        
        public void SetWaveCount(int count)
        {
            _selectedWaveCount = Mathf.Clamp(count, _minWaveCount, _maxWaveCount);
            _isInfinite = false;
            UpdateUI();
        }
        
        public void SetInfiniteWaves(bool infinite)
        {
            _isInfinite = infinite;
            UpdateUI();
        }
        
        #endregion
    }
}