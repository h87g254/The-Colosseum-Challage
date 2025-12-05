using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TheColosseumChallenge.Core;
using TheColosseumChallenge.Networking;

namespace TheColosseumChallenge.UI.Panels
{
    public class PauseMenuPanel : BasePanel
    {
        #region Inspector References
        
        [Header("Buttons")]
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _quitSessionButton;
        
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _sessionInfoText;
        
        [Header("Settings Panel")]
        [SerializeField] private GameObject _settingsSubPanel;
        [SerializeField] private Button _settingsBackButton;
        
        [Header("Confirmation Dialog")]
        [SerializeField] private GameObject _confirmationDialog;
        [SerializeField] private TextMeshProUGUI _confirmationText;
        [SerializeField] private Button _confirmYesButton;
        [SerializeField] private Button _confirmNoButton;
        
        #endregion

        #region Private Fields
        
        private bool _showingSettings;
        private bool _showingConfirmation;
        
        #endregion

        #region Unity Lifecycle
        
        protected override void Awake()
        {
            base.Awake();
            
            SetupListeners();
        }
        
        #endregion

        #region Initialization
        
        private void SetupListeners()
        {
            if (_resumeButton != null)
                _resumeButton.onClick.AddListener(OnResumeClicked);
                
            if (_settingsButton != null)
                _settingsButton.onClick.AddListener(OnSettingsClicked);
                
            if (_quitSessionButton != null)
                _quitSessionButton.onClick.AddListener(OnQuitSessionClicked);
                
            if (_settingsBackButton != null)
                _settingsBackButton.onClick.AddListener(OnSettingsBackClicked);
                
            if (_confirmYesButton != null)
                _confirmYesButton.onClick.AddListener(OnConfirmYes);
                
            if (_confirmNoButton != null)
                _confirmNoButton.onClick.AddListener(OnConfirmNo);
            
            Debug.Log("[PauseMenuPanel] Listeners set up.");
        }
        
        protected override void OnInitialize()
        {
            base.OnInitialize();
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPauseStateChanged += OnPauseStateChanged;
            }
            
            Debug.Log("[PauseMenuPanel] Initialized.");
        }
        
        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPauseStateChanged -= OnPauseStateChanged;
            }
        }
        
        #endregion

        #region Panel Lifecycle
        
        protected override void OnBeforeShow()
        {
            base.OnBeforeShow();
            
            _showingSettings = false;
            _showingConfirmation = false;
            
            UpdateSubPanels();
            UpdateSessionInfo();
        }
        
        protected override void OnAfterShow()
        {
            base.OnAfterShow();
            
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        
        protected override void OnAfterHide()
        {
            base.OnAfterHide();
            
            if (GameManager.Instance != null && !GameManager.Instance.IsPaused)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
        
        #endregion

        #region Pause State Handler
        
        private void OnPauseStateChanged(bool isPaused)
        {
            if (isPaused)
            {
                Show();
            }
            else
            {
                Hide();
            }
        }
        
        #endregion

        #region Button Handlers
        
        private void OnResumeClicked()
        {
            Debug.Log("[PauseMenuPanel] Resume clicked.");
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetPaused(false);
            }
        }
        
        private void OnSettingsClicked()
        {
            Debug.Log("[PauseMenuPanel] Settings clicked.");
            
            _showingSettings = true;
            UpdateSubPanels();
        }
        
        private void OnSettingsBackClicked()
        {
            _showingSettings = false;
            UpdateSubPanels();
        }
        
        private void OnQuitSessionClicked()
        {
            Debug.Log("[PauseMenuPanel] Quit session clicked.");
            
            ShowConfirmation("Are you sure you want to leave the session?");
        }
        
        private void OnConfirmYes()
        {
            _showingConfirmation = false;
            UpdateSubPanels();
            
            QuitSession();
        }
        
        private void OnConfirmNo()
        {
            _showingConfirmation = false;
            UpdateSubPanels();
        }
        
        #endregion

        #region Session Management
        
        private void QuitSession()
        {
            Debug.Log("[PauseMenuPanel] Quitting session...");
            
            if (GameManager.Instance?.CurrentGameMode == GameMode.Multiplayer)
            {
                if (NetworkManager.Instance != null)
                {
                    NetworkManager.Instance.LeaveSession();
                }
            }
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetPaused(false);
                GameManager.Instance.LoadMainMenu();
            }
        }
        
        #endregion

        #region UI Updates
        
        private void UpdateSubPanels()
        {
            if (_resumeButton != null)
                _resumeButton.gameObject.SetActive(!_showingSettings && !_showingConfirmation);
            if (_settingsButton != null)
                _settingsButton.gameObject.SetActive(!_showingSettings && !_showingConfirmation);
            if (_quitSessionButton != null)
                _quitSessionButton.gameObject.SetActive(!_showingSettings && !_showingConfirmation);
            
            if (_settingsSubPanel != null)
                _settingsSubPanel.SetActive(_showingSettings);
            
            if (_confirmationDialog != null)
                _confirmationDialog.SetActive(_showingConfirmation);
        }
        
        private void UpdateSessionInfo()
        {
            if (_sessionInfoText == null) return;
            
            if (GameManager.Instance == null)
            {
                _sessionInfoText.text = "";
                return;
            }
            
            string modeText = GameManager.Instance.CurrentGameMode == GameMode.Singleplayer 
                ? "Singleplayer" : "Multiplayer";
                
            string waveText = GameManager.Instance.IsInfiniteWaves 
                ? "∞" : $"{GameManager.Instance.CurrentWave}/{GameManager.Instance.ConfiguredWaves}";
            
            _sessionInfoText.text = $"{modeText} - Wave {waveText}";
        }
        
        private void ShowConfirmation(string message)
        {
            _showingConfirmation = true;
            
            if (_confirmationText != null)
            {
                _confirmationText.text = message;
            }
            
            UpdateSubPanels();
        }
        
        #endregion

        #region Input Handling
        
        private void Update()
        {
            if (!IsVisible) return;
            
            // Handle escape key
            if (UnityEngine.InputSystem.Keyboard.current != null && 
                UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (_showingConfirmation)
                {
                    OnConfirmNo();
                }
                else if (_showingSettings)
                {
                    OnSettingsBackClicked();
                }
                else
                {
                    OnResumeClicked();
                }
            }
        }
        
        #endregion
    }
}