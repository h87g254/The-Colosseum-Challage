using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TheColosseumChallenge.Core;
using TheColosseumChallenge.Data;
using TheColosseumChallenge.UI.Panels;

namespace TheColosseumChallenge.UI
{
    public enum UIPanelType
    {
        None,
        UsernameEntry,
        MainMenu,
        Singleplayer,
        SessionBrowser,
        CreateSession,
        WaitingRoom,
        Settings
    }

    /// <summary>
    /// Central manager for all UI operations.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        #region Singleton Pattern
        
        private static UIManager _instance;
        
        public static UIManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<UIManager>();
                }
                return _instance;
            }
        }
        
        #endregion

        #region Inspector References
        
        [Header("Panel References")]
        [SerializeField] private UsernameEntryPanel _usernameEntryPanel;
        [SerializeField] private MainMenuPanel _mainMenuPanel;
        [SerializeField] private SingleplayerPanel _singleplayerPanel;
        [SerializeField] private SessionBrowserPanel _sessionBrowserPanel;
        [SerializeField] private CreateSessionPanel _createSessionPanel;
        [SerializeField] private WaitingRoomPanel _waitingRoomPanel;
        [SerializeField] private SettingsPanel _settingsPanel;
        
        [Header("Transition Settings")]
        [SerializeField] private float _transitionDuration = 0.3f;
        [SerializeField] private bool _useAnimations = true;
        
        #endregion

        #region Private Fields
        
        private Dictionary<UIPanelType, BasePanel> _panels;
        private Stack<UIPanelType> _navigationHistory;
        private UIPanelType _currentPanel = UIPanelType.None;
        private bool _isTransitioning;
        private bool _isInitialized;
        
        #endregion

        #region Events
        
        public event Action<UIPanelType, UIPanelType> OnPanelTransitionStarted;
        public event Action<UIPanelType> OnPanelTransitionCompleted;
        
        #endregion

        #region Properties
        
        public UIPanelType CurrentPanel => _currentPanel;
        public bool CanNavigateBack => _navigationHistory != null && _navigationHistory.Count > 0;
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            
            _navigationHistory = new Stack<UIPanelType>();
            _panels = new Dictionary<UIPanelType, BasePanel>();
        }

        private void Start()
        {
            StartCoroutine(InitializeDelayed());
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
        
        #endregion

        #region Initialization
        
        private IEnumerator InitializeDelayed()
        {
            yield return null;
            
            InitializePanels();
            DetermineInitialPanel();
        }
        
        private void InitializePanels()
        {
            if (_isInitialized)
            {
                Debug.LogWarning("[UIManager] Already initialized.");
                return;
            }
            
            Debug.Log("[UIManager] Initializing panels...");
            
            RegisterPanel(UIPanelType.UsernameEntry, _usernameEntryPanel);
            RegisterPanel(UIPanelType.MainMenu, _mainMenuPanel);
            RegisterPanel(UIPanelType.Singleplayer, _singleplayerPanel);
            RegisterPanel(UIPanelType.SessionBrowser, _sessionBrowserPanel);
            RegisterPanel(UIPanelType.CreateSession, _createSessionPanel);
            RegisterPanel(UIPanelType.WaitingRoom, _waitingRoomPanel);
            RegisterPanel(UIPanelType.Settings, _settingsPanel);
            
            foreach (var kvp in _panels)
            {
                if (kvp.Value != null)
                {
                    try
                    {
                        kvp.Value.Initialize(this);
                        Debug.Log($"[UIManager] Panel {kvp.Key} initialized.");
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[UIManager] Failed to initialize panel {kvp.Key}: {e.Message}");
                    }
                }
            }
            
            _isInitialized = true;
            Debug.Log($"[UIManager] Initialized {_panels.Count} panels.");
        }
        
        private void RegisterPanel(UIPanelType type, BasePanel panel)
        {
            if (panel != null)
            {
                _panels[type] = panel;
            }
            else
            {
                Debug.LogWarning($"[UIManager] Panel not assigned for type: {type}");
            }
        }
        
        private void DetermineInitialPanel()
        {
            Debug.Log("[UIManager] Determining initial panel...");
            
            bool hasUsername = PlayerDataManager.Instance != null && PlayerDataManager.Instance.HasUsername;
            
            Debug.Log($"[UIManager] HasUsername: {hasUsername}");
            
            if (hasUsername)
            {
                ShowPanel(UIPanelType.MainMenu, addToHistory: false);
            }
            else
            {
                ShowPanel(UIPanelType.UsernameEntry, addToHistory: false);
            }
        }
        
        #endregion

        #region Panel Navigation
        
        public void ShowPanel(UIPanelType panelType, bool addToHistory = true)
        {
            if (_isTransitioning)
            {
                Debug.LogWarning("[UIManager] Panel transition already in progress.");
                return;
            }
            
            if (!_panels.ContainsKey(panelType) || _panels[panelType] == null)
            {
                Debug.LogError($"[UIManager] Panel not found: {panelType}");
                return;
            }
            
            if (_currentPanel == panelType)
            {
                Debug.Log($"[UIManager] Already showing panel: {panelType}");
                return;
            }
            
            StartPanelTransition(panelType, addToHistory);
        }
        
        private void StartPanelTransition(UIPanelType newPanel, bool addToHistory)
        {
            _isTransitioning = true;
            
            UIPanelType oldPanel = _currentPanel;
            
            if (addToHistory && oldPanel != UIPanelType.None)
            {
                _navigationHistory.Push(oldPanel);
            }
            
            OnPanelTransitionStarted?.Invoke(oldPanel, newPanel);
            
            if (oldPanel != UIPanelType.None && _panels.ContainsKey(oldPanel) && _panels[oldPanel] != null)
            {
                _panels[oldPanel].Hide(immediate: !_useAnimations);
            }
            
            _currentPanel = newPanel;
            
            if (_panels[newPanel] != null)
            {
                _panels[newPanel].Show(immediate: !_useAnimations);
            }
            
            _isTransitioning = false;
            
            Debug.Log($"[UIManager] Panel transition: {oldPanel} -> {newPanel}");
            
            OnPanelTransitionCompleted?.Invoke(newPanel);
        }
        
        public void NavigateBack()
        {
            if (!CanNavigateBack)
            {
                Debug.Log("[UIManager] No navigation history available - going to main menu.");
                ShowMainMenu();
                return;
            }
            
            UIPanelType previousPanel = _navigationHistory.Pop();
            ShowPanel(previousPanel, addToHistory: false);
        }
        
        public void ClearNavigationHistory()
        {
            _navigationHistory.Clear();
        }
        
        #endregion

        #region Panel Access
        
        public T GetPanel<T>(UIPanelType panelType) where T : BasePanel
        {
            if (_panels.TryGetValue(panelType, out BasePanel panel))
            {
                return panel as T;
            }
            return null;
        }
        
        public BasePanel GetCurrentPanel()
        {
            if (_currentPanel != UIPanelType.None && _panels.ContainsKey(_currentPanel))
            {
                return _panels[_currentPanel];
            }
            return null;
        }
        
        #endregion

        #region Convenience Methods
        
        public void ShowMainMenu()
        {
            ClearNavigationHistory();
            ShowPanel(UIPanelType.MainMenu, addToHistory: false);
        }
        
        public void ShowSingleplayerPanel()
        {
            ShowPanel(UIPanelType.Singleplayer);
        }
        
        public void ShowMultiplayerPanel()
        {
            ShowPanel(UIPanelType.SessionBrowser);
        }
        
        public void ShowSettingsPanel()
        {
            ShowPanel(UIPanelType.Settings);
        }
        
        public void ShowCreateSessionPanel()
        {
            ShowPanel(UIPanelType.CreateSession);
        }
        
        /// <summary>
        /// FIXED: Shows waiting room and preserves navigation history.
        /// Now when user presses back, they return to session browser.
        /// </summary>
        public void ShowWaitingRoom()
        {
            // DON'T clear history - preserve it so back button works
            ShowPanel(UIPanelType.WaitingRoom, addToHistory: false);
        }
        
        /// <summary>
        /// NEW: Helper method for leaving a session and returning to session browser
        /// </summary>
        public void LeaveSessionAndReturnToBrowser()
        {
            // Clear navigation history to prevent weird back button behavior
            ClearNavigationHistory();
            
            // Return to session browser
            ShowPanel(UIPanelType.SessionBrowser, addToHistory: false);
        }
        
        public void QuitGame()
        {
            Debug.Log("[UIManager] Quit game requested.");
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.QuitGame();
            }
            else
            {
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
                #else
                Application.Quit();
                #endif
            }
        }
        
        #endregion
    }
}