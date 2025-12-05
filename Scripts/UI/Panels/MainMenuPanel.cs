using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TheColosseumChallenge.Core;
using TheColosseumChallenge.Data;
using TheColosseumChallenge.UI.Components;

namespace TheColosseumChallenge.UI.Panels
{
    /// <summary>
    /// Main menu panel displaying navigation options.
    /// </summary>
    public class MainMenuPanel : BasePanel
    {
        #region Inspector References
        
        [Header("Navigation Buttons")]
        [SerializeField] private Button _singleplayerButton;
        [SerializeField] private Button _multiplayerButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _quitButton;
        
        [Header("Player Display")]
        [SerializeField] private TextMeshProUGUI _usernameText;
        [SerializeField] private Button _customizeButton;
        [SerializeField] private RawImage _playerModelDisplay;
        [SerializeField] private PlayerModelPreview _playerModelPreview;
        
        [Header("Additional UI")]
        [SerializeField] private TextMeshProUGUI _versionText;
        [SerializeField] private TextMeshProUGUI _titleText;
        
        #endregion

        #region Unity Lifecycle
        
        protected override void Awake()
        {
            base.Awake();
            
            // Setup button listeners in Awake
            SetupButtons();
        }
        
        #endregion

        #region Initialization
        
        private void SetupButtons()
        {
            if (_singleplayerButton != null)
            {
                _singleplayerButton.onClick.RemoveAllListeners();
                _singleplayerButton.onClick.AddListener(OnSingleplayerClicked);
                Debug.Log("[MainMenuPanel] Singleplayer button set up.");
            }
                
            if (_multiplayerButton != null)
            {
                _multiplayerButton.onClick.RemoveAllListeners();
                _multiplayerButton.onClick.AddListener(OnMultiplayerClicked);
                Debug.Log("[MainMenuPanel] Multiplayer button set up.");
            }
                
            if (_settingsButton != null)
            {
                _settingsButton.onClick.RemoveAllListeners();
                _settingsButton.onClick.AddListener(OnSettingsClicked);
                Debug.Log("[MainMenuPanel] Settings button set up.");
            }
                
            if (_quitButton != null)
            {
                _quitButton.onClick.RemoveAllListeners();
                _quitButton.onClick.AddListener(OnQuitClicked);
                Debug.Log("[MainMenuPanel] Quit button set up.");
            }
                
            if (_customizeButton != null)
            {
                _customizeButton.onClick.RemoveAllListeners();
                _customizeButton.onClick.AddListener(OnCustomizeClicked);
            }
        }
        
        protected override void OnInitialize()
        {
            base.OnInitialize();
            
            SetupVersionText();
            
            // Subscribe to data changes
            if (PlayerDataManager.Instance != null)
            {
                PlayerDataManager.Instance.OnUsernameChanged += OnUsernameChanged;
                PlayerDataManager.Instance.OnCustomizationChanged += OnCustomizationChanged;
            }
            
            Debug.Log("[MainMenuPanel] Initialized.");
        }
        
        private void SetupVersionText()
        {
            if (_versionText != null)
            {
                _versionText.text = $"v{Application.version}";
            }
        }
        
        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            if (PlayerDataManager.Instance != null)
            {
                PlayerDataManager.Instance.OnUsernameChanged -= OnUsernameChanged;
                PlayerDataManager.Instance.OnCustomizationChanged -= OnCustomizationChanged;
            }
        }
        
        #endregion

        #region Panel Lifecycle
        
        protected override void OnBeforeShow()
        {
            base.OnBeforeShow();
            
            UpdateUsernameDisplay();
            UpdatePlayerModelDisplay();
        }
        
        protected override void OnAfterShow()
        {
            base.OnAfterShow();
            
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        
        #endregion

        #region Button Handlers
        
        private void OnSingleplayerClicked()
        {
            Debug.Log("[MainMenuPanel] Singleplayer clicked.");
            UIManager?.ShowSingleplayerPanel();
        }
        
        private void OnMultiplayerClicked()
        {
            Debug.Log("[MainMenuPanel] Multiplayer clicked.");
            UIManager?.ShowMultiplayerPanel();
        }
        
        private void OnSettingsClicked()
        {
            Debug.Log("[MainMenuPanel] Settings clicked.");
            UIManager?.ShowSettingsPanel();
        }
        
        private void OnQuitClicked()
        {
            Debug.Log("[MainMenuPanel] Quit clicked.");
            UIManager?.QuitGame();
        }
        
        private void OnCustomizeClicked()
        {
            Debug.Log("[MainMenuPanel] Customize clicked.");
            
            if (PlayerDataManager.Instance != null)
            {
                PlayerDataManager.Instance.RandomizeCustomization();
            }
        }
        
        #endregion

        #region Data Event Handlers
        
        private void OnUsernameChanged(string newUsername)
        {
            UpdateUsernameDisplay();
        }
        
        private void OnCustomizationChanged(PlayerCustomizationData customization)
        {
            UpdatePlayerModelDisplay();
        }
        
        #endregion

        #region Display Updates
        
        private void UpdateUsernameDisplay()
        {
            if (_usernameText == null) return;
            
            if (PlayerDataManager.Instance != null && PlayerDataManager.Instance.HasUsername)
            {
                _usernameText.text = PlayerDataManager.Instance.Username;
            }
            else
            {
                _usernameText.text = "Player";
            }
        }
        
        private void UpdatePlayerModelDisplay()
        {
            if (_playerModelPreview == null) return;
            
            if (PlayerDataManager.Instance != null)
            {
                _playerModelPreview.UpdateAppearance(PlayerDataManager.Instance.CustomizationData);
            }
        }
        
        public void RefreshDisplay()
        {
            UpdateUsernameDisplay();
            UpdatePlayerModelDisplay();
        }
        
        #endregion
    }
}