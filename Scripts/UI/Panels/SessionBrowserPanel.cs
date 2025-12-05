using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TheColosseumChallenge.Data;
using TheColosseumChallenge.Networking;
using TheColosseumChallenge.UI.Components;

namespace TheColosseumChallenge.UI.Panels
{
    public class SessionBrowserPanel : BasePanel
    {
        #region Inspector References
        
        [Header("UI Elements")]
        [SerializeField] private ScrollRect _sessionScrollView;
        [SerializeField] private Transform _sessionListContent;
        [SerializeField] private GameObject _sessionItemPrefab;
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _statusText;
        
        [Header("Buttons")]
        [SerializeField] private Button _createSessionButton;
        [SerializeField] private Button _refreshButton;
        [SerializeField] private Button _backButton;
        
        [Header("Empty State")]
        [SerializeField] private GameObject _emptyStateContainer;
        [SerializeField] private TextMeshProUGUI _emptyStateText;
        
        [Header("Loading State")]
        [SerializeField] private GameObject _loadingIndicator;
        
        [Header("Settings")]
        [SerializeField] private float _autoRefreshInterval = 5f;
        [SerializeField] private bool _autoRefreshEnabled = true;
        
        #endregion

        #region Private Fields
        
        private List<SessionListItem> _sessionItems = new List<SessionListItem>();
        private bool _isRefreshing;
        private float _refreshTimer;
        
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
            if (_createSessionButton != null)
                _createSessionButton.onClick.AddListener(OnCreateSessionClicked);
                
            if (_refreshButton != null)
                _refreshButton.onClick.AddListener(OnRefreshClicked);
                
            if (_backButton != null)
                _backButton.onClick.AddListener(OnBackClicked);
            
            Debug.Log("[SessionBrowserPanel] Listeners set up.");
        }
        
        protected override void OnInitialize()
        {
            base.OnInitialize();
            
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.SessionListUpdated += OnSessionListUpdated;
                NetworkManager.Instance.JoinSessionFailed += OnJoinFailed;
            }
            
            Debug.Log("[SessionBrowserPanel] Initialized.");
        }
        
        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.SessionListUpdated -= OnSessionListUpdated;
                NetworkManager.Instance.JoinSessionFailed -= OnJoinFailed;
            }
        }
        
        #endregion

        #region Panel Lifecycle
        
        protected override void OnBeforeShow()
        {
            base.OnBeforeShow();
            
            ClearSessionList();
            SetLoadingState(true);
            SetEmptyState(false);
        }
        
        protected override void OnAfterShow()
        {
            base.OnAfterShow();
            
            RefreshSessions();
            _refreshTimer = _autoRefreshInterval;
        }
        
        protected override void OnBeforeHide()
        {
            base.OnBeforeHide();
            _isRefreshing = false;
        }
        
        #endregion

        #region Update Loop
        
        private void Update()
        {
            if (!IsVisible || !_autoRefreshEnabled) return;
            
            _refreshTimer -= Time.deltaTime;
            if (_refreshTimer <= 0)
            {
                _refreshTimer = _autoRefreshInterval;
                RefreshSessions();
            }
        }
        
        #endregion

        #region Session Management
        
        public void RefreshSessions()
        {
            if (_isRefreshing) return;
            
            _isRefreshing = true;
            SetLoadingState(true);
            UpdateStatus("Refreshing...");
            
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.RefreshSessionList();
            }
            else
            {
                Debug.LogWarning("[SessionBrowserPanel] NetworkManager not found.");
                _isRefreshing = false;
                SetLoadingState(false);
                SetEmptyState(true, "Network not available");
            }
        }
        
        private void OnSessionListUpdated(List<SessionData> sessions)
        {
            _isRefreshing = false;
            SetLoadingState(false);
            
            var filteredSessions = sessions.FindAll(s => 
                s.Status == SessionStatus.Waiting || 
                s.Status == SessionStatus.InProgress);
            
            UpdateSessionList(filteredSessions);
            
            if (filteredSessions.Count == 0)
            {
                SetEmptyState(true, "No sessions available");
                UpdateStatus("No sessions found");
            }
            else
            {
                SetEmptyState(false);
                UpdateStatus($"{filteredSessions.Count} session(s) found");
            }
            
            Debug.Log($"[SessionBrowserPanel] Session list updated: {filteredSessions.Count} sessions.");
        }
        
        private void UpdateSessionList(List<SessionData> sessions)
        {
            ClearSessionList();
            
            if (_sessionItemPrefab == null || _sessionListContent == null)
            {
                Debug.LogError("[SessionBrowserPanel] Session item prefab or content not set.");
                return;
            }
            
            foreach (var session in sessions)
            {
                GameObject itemObj = Instantiate(_sessionItemPrefab, _sessionListContent);
                SessionListItem item = itemObj.GetComponent<SessionListItem>();
                
                if (item != null)
                {
                    item.SetSessionData(session);
                    item.OnJoinClicked += OnSessionJoinClicked;
                    _sessionItems.Add(item);
                }
            }
        }
        
        private void ClearSessionList()
        {
            foreach (var item in _sessionItems)
            {
                if (item != null)
                {
                    item.OnJoinClicked -= OnSessionJoinClicked;
                    Destroy(item.gameObject);
                }
            }
            _sessionItems.Clear();
        }
        
        #endregion

        #region Event Handlers
        
        private void OnSessionJoinClicked(SessionData session)
        {
            if (session == null || !session.CanJoin)
            {
                UpdateStatus("Cannot join this session");
                return;
            }
            
            Debug.Log($"[SessionBrowserPanel] Joining session: {session.SessionName}");
            
            UpdateStatus("Joining session...");
            SetLoadingState(true);
            
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.JoinSession(session.SessionId);
            }
        }
        
        private void OnJoinFailed(string reason)
        {
            SetLoadingState(false);
            UpdateStatus($"Join failed: {reason}");
            Debug.LogWarning($"[SessionBrowserPanel] Join failed: {reason}");
        }
        
        #endregion

        #region Button Handlers
        
        private void OnCreateSessionClicked()
        {
            Debug.Log("[SessionBrowserPanel] Create session clicked.");
            UIManager?.ShowCreateSessionPanel();
        }
        
        private void OnRefreshClicked()
        {
            RefreshSessions();
        }
        
        private void OnBackClicked()
        {
            Debug.Log("[SessionBrowserPanel] Back clicked.");
            NavigateBack();
        }
        
        #endregion

        #region UI State Management
        
        private void SetLoadingState(bool isLoading)
        {
            if (_loadingIndicator != null)
                _loadingIndicator.SetActive(isLoading);
                
            if (_refreshButton != null)
                _refreshButton.interactable = !isLoading;
        }
        
        private void SetEmptyState(bool isEmpty, string message = "")
        {
            if (_emptyStateContainer != null)
                _emptyStateContainer.SetActive(isEmpty);
                
            if (_emptyStateText != null && !string.IsNullOrEmpty(message))
                _emptyStateText.text = message;
        }
        
        private void UpdateStatus(string status)
        {
            if (_statusText != null)
                _statusText.text = status;
        }
        
        #endregion
    }
}