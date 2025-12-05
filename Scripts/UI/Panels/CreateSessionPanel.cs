using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TheColosseumChallenge.Data;
using TheColosseumChallenge.Networking;

namespace TheColosseumChallenge.UI.Panels
{
    public class CreateSessionPanel : BasePanel
    {
        #region Inspector References
        
        [Header("UI Elements")]
        [SerializeField] private TMP_InputField _sessionNameInput;
        [SerializeField] private Slider _maxPlayersSlider;
        [SerializeField] private TextMeshProUGUI _maxPlayersValueText;
        [SerializeField] private TMP_InputField _wavesInput;
        [SerializeField] private Toggle _infiniteWavesToggle;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _errorText;
        
        [Header("Buttons")]
        [SerializeField] private Button _createButton;
        [SerializeField] private Button _backButton;
        
        [Header("Settings")]
        [SerializeField] private int _minPlayers = 2;
        [SerializeField] private int _maxPlayers = 8;
        [SerializeField] private int _defaultPlayers = 4;
        [SerializeField] private int _defaultWaves = 10;
        [SerializeField] private int _minWaves = 1;
        [SerializeField] private int _maxWaves = 999;
        
        #endregion

        #region Private Fields
        
        private SessionConfiguration _configuration;
        private bool _isCreating;
        
        #endregion

        #region Unity Lifecycle
        
        protected override void Awake()
        {
            base.Awake();
            
            _configuration = new SessionConfiguration();
            SetupListeners();
        }
        
        #endregion

        #region Initialization
        
        private void SetupListeners()
        {
            if (_sessionNameInput != null)
            {
                _sessionNameInput.characterLimit = 30;
                _sessionNameInput.onValueChanged.AddListener(OnSessionNameChanged);
            }
            
            if (_wavesInput != null)
            {
                _wavesInput.contentType = TMP_InputField.ContentType.IntegerNumber;
                _wavesInput.characterLimit = 3;
                _wavesInput.onValueChanged.AddListener(OnWavesChanged);
                _wavesInput.onEndEdit.AddListener(OnWavesEndEdit);
            }
            
            if (_infiniteWavesToggle != null)
            {
                _infiniteWavesToggle.onValueChanged.AddListener(OnInfiniteWavesChanged);
            }
            
            if (_maxPlayersSlider != null)
            {
                _maxPlayersSlider.minValue = _minPlayers;
                _maxPlayersSlider.maxValue = _maxPlayers;
                _maxPlayersSlider.wholeNumbers = true;
                _maxPlayersSlider.onValueChanged.AddListener(OnMaxPlayersChanged);
            }
            
            if (_createButton != null)
                _createButton.onClick.AddListener(OnCreateClicked);
                
            if (_backButton != null)
                _backButton.onClick.AddListener(OnBackClicked);
            
            Debug.Log("[CreateSessionPanel] Listeners set up.");
        }
        
        protected override void OnInitialize()
        {
            base.OnInitialize();
            
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.SessionCreated += OnSessionCreated;
                NetworkManager.Instance.SessionCreationFailed += OnSessionCreationFailed;
            }
            
            Debug.Log("[CreateSessionPanel] Initialized.");
        }
        
        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.SessionCreated -= OnSessionCreated;
                NetworkManager.Instance.SessionCreationFailed -= OnSessionCreationFailed;
            }
        }
        
        #endregion

        #region Panel Lifecycle
        
        protected override void OnBeforeShow()
        {
            base.OnBeforeShow();
            
            ResetToDefaults();
            ClearError();
        }
        
        protected override void OnAfterShow()
        {
            base.OnAfterShow();
            
            if (_sessionNameInput != null)
            {
                _sessionNameInput.Select();
                _sessionNameInput.ActivateInputField();
            }
        }
        
        #endregion

        #region Input Handlers
        
        private void OnSessionNameChanged(string value)
        {
            _configuration.SessionName = value.Trim();
            ClearError();
            UpdateCreateButton();
        }
        
        private void OnMaxPlayersChanged(float value)
        {
            _configuration.MaxPlayers = Mathf.RoundToInt(value);
            UpdateMaxPlayersDisplay();
        }
        
        private void OnWavesChanged(string value)
        {
            if (int.TryParse(value, out int waves))
            {
                _configuration.WaveCount = waves;
            }
        }
        
        private void OnWavesEndEdit(string value)
        {
            if (int.TryParse(value, out int waves))
            {
                _configuration.WaveCount = Mathf.Clamp(waves, _minWaves, _maxWaves);
            }
            else
            {
                _configuration.WaveCount = _defaultWaves;
            }
            
            UpdateWavesDisplay();
        }
        
        private void OnInfiniteWavesChanged(bool isOn)
        {
            _configuration.WaveCount = isOn ? -1 : _defaultWaves;
            UpdateWavesDisplay();
        }
        
        #endregion

        #region Button Handlers
        
        private void OnCreateClicked()
        {
            if (_isCreating) return;
            
            if (!ValidateConfiguration()) return;
            
            _isCreating = true;
            SetCreateButtonState(false);
            
            Debug.Log($"[CreateSessionPanel] Creating session: {_configuration.SessionName}");
            
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.CreateSession(_configuration);
            }
            else
            {
                Debug.LogError("[CreateSessionPanel] NetworkManager not found!");
                ShowError("Network not available");
                _isCreating = false;
                SetCreateButtonState(true);
            }
        }
        
        private void OnBackClicked()
        {
            Debug.Log("[CreateSessionPanel] Back clicked.");
            NavigateBack();
        }
        
        #endregion

        #region Validation
        
        private bool ValidateConfiguration()
        {
            if (string.IsNullOrWhiteSpace(_configuration.SessionName))
            {
                ShowError("Please enter a session name.");
                return false;
            }
            
            if (_configuration.SessionName.Length < 3)
            {
                ShowError("Session name must be at least 3 characters.");
                return false;
            }
            
            if (_configuration.MaxPlayers < _minPlayers || _configuration.MaxPlayers > _maxPlayers)
            {
                ShowError($"Player count must be between {_minPlayers} and {_maxPlayers}.");
                return false;
            }
            
            if (_configuration.WaveCount != -1 && 
                (_configuration.WaveCount < _minWaves || _configuration.WaveCount > _maxWaves))
            {
                ShowError($"Wave count must be between {_minWaves} and {_maxWaves}.");
                return false;
            }
            
            return true;
        }
        
        #endregion

        #region Network Event Handlers
        
        private void OnSessionCreated()
        {
            _isCreating = false;
            SetCreateButtonState(true);
            
            Debug.Log("[CreateSessionPanel] Session created successfully.");
            
            UIManager?.ShowWaitingRoom();
        }
        
        private void OnSessionCreationFailed(string reason)
        {
            _isCreating = false;
            SetCreateButtonState(true);
            
            ShowError($"Failed to create session: {reason}");
            Debug.LogWarning($"[CreateSessionPanel] Session creation failed: {reason}");
        }
        
        #endregion

        #region UI Updates
        
        private void ResetToDefaults()
        {
            _configuration = new SessionConfiguration
            {
                SessionName = $"{PlayerDataManager.Instance?.Username ?? "Player"}'s Session",
                MaxPlayers = _defaultPlayers,
                WaveCount = _defaultWaves,
                IsPublic = true
            };
            
            if (_sessionNameInput != null)
                _sessionNameInput.text = _configuration.SessionName;
                
            if (_maxPlayersSlider != null)
                _maxPlayersSlider.value = _configuration.MaxPlayers;
                
            UpdateMaxPlayersDisplay();
            UpdateWavesDisplay();
            UpdateCreateButton();
            
            _isCreating = false;
        }
        
        private void UpdateMaxPlayersDisplay()
        {
            if (_maxPlayersValueText != null)
            {
                _maxPlayersValueText.text = _configuration.MaxPlayers.ToString();
            }
        }
        
        private void UpdateWavesDisplay()
        {
            bool isInfinite = _configuration.WaveCount == -1;
            
            if (_wavesInput != null)
            {
                _wavesInput.interactable = !isInfinite;
                _wavesInput.text = isInfinite ? "∞" : _configuration.WaveCount.ToString();
            }
            
            if (_infiniteWavesToggle != null)
            {
                _infiniteWavesToggle.SetIsOnWithoutNotify(isInfinite);
            }
        }
        
        private void UpdateCreateButton()
        {
            if (_createButton != null)
            {
                _createButton.interactable = !string.IsNullOrWhiteSpace(_configuration.SessionName) && 
                                             _configuration.SessionName.Length >= 3;
            }
        }
        
        private void SetCreateButtonState(bool interactable)
        {
            if (_createButton != null)
            {
                _createButton.interactable = interactable;
            }
        }
        
        private void ShowError(string message)
        {
            if (_errorText != null)
            {
                _errorText.text = message;
                _errorText.gameObject.SetActive(true);
            }
        }
        
        private void ClearError()
        {
            if (_errorText != null)
            {
                _errorText.text = string.Empty;
                _errorText.gameObject.SetActive(false);
            }
        }
        
        #endregion
    }
}