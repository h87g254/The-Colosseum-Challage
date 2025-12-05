using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TheColosseumChallenge.Data;
using TheColosseumChallenge.Networking;
using TheColosseumChallenge.UI.Components;

namespace TheColosseumChallenge.UI.Panels
{
    public class WaitingRoomPanel : BasePanel
    {
        #region Inspector References
        
        [Header("Session Info")]
        [SerializeField] private TextMeshProUGUI _sessionNameText;
        [SerializeField] private TextMeshProUGUI _playerCountText;
        [SerializeField] private TextMeshProUGUI _wavesText;
        [SerializeField] private TextMeshProUGUI _ownerText;
        [SerializeField] private TextMeshProUGUI _statusText;
        
        [Header("Player List")]
        [SerializeField] private ScrollRect _playerListScrollView;
        [SerializeField] private Transform _playerListContent;
        [SerializeField] private GameObject _playerListItemPrefab;
        
        [Header("Buttons")]
        [SerializeField] private Button _startGameButton;
        [SerializeField] private Button _leaveButton;
        [SerializeField] private TextMeshProUGUI _startButtonText;
        
        [Header("Settings")]
        [SerializeField] private int _minPlayersToStart = 1;
        
        #endregion

        #region Private Fields
        
        private List<PlayerListItem> _playerItems = new List<PlayerListItem>();
        private SessionData _currentSession;
        private bool _isOwner;
        
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
            if (_startGameButton != null)
                _startGameButton.onClick.AddListener(OnStartGameClicked);
                
            if (_leaveButton != null)
                _leaveButton.onClick.AddListener(OnLeaveClicked);
            
            Debug.Log("[WaitingRoomPanel] Listeners set up.");
        }
        
        protected override void OnInitialize()
        {
            base.OnInitialize();
            
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.PlayerJoinedSession += OnPlayerJoined;
                NetworkManager.Instance.PlayerLeftSession += OnPlayerLeft;
                NetworkManager.Instance.SessionUpdated += OnSessionUpdated;
                NetworkManager.Instance.GameStarting += OnGameStarting;
            }
            
            Debug.Log("[WaitingRoomPanel] Initialized.");
        }
        
        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.PlayerJoinedSession -= OnPlayerJoined;
                NetworkManager.Instance.PlayerLeftSession -= OnPlayerLeft;
                NetworkManager.Instance.SessionUpdated -= OnSessionUpdated;
                NetworkManager.Instance.GameStarting -= OnGameStarting;
            }
        }
        
        #endregion

        #region Panel Lifecycle
        
        protected override void OnBeforeShow()
        {
            base.OnBeforeShow();
            
            if (NetworkManager.Instance != null)
            {
                _currentSession = NetworkManager.Instance.CurrentSession;
                _isOwner = NetworkManager.Instance.IsSessionOwner;
            }
            
            UpdateSessionInfo();
            UpdatePlayerList();
            UpdateStartButton();
        }
        
        protected override void OnAfterShow()
        {
            base.OnAfterShow();
            UpdateStatus("Waiting for players...");
        }
        
        #endregion

        #region Session Management
        
        public void SetSessionData(SessionData session, bool isOwner)
        {
            _currentSession = session;
            _isOwner = isOwner;
            
            if (IsVisible)
            {
                UpdateSessionInfo();
                UpdatePlayerList();
                UpdateStartButton();
            }
        }
        
        #endregion

        #region Network Event Handlers
        
        private void OnPlayerJoined(SessionPlayerData player)
        {
            Debug.Log($"[WaitingRoomPanel] Player joined: {player.Username}");
            
            UpdatePlayerList();
            UpdatePlayerCount();
            UpdateStartButton();
            UpdateStatus($"{player.Username} joined the session.");
        }
        
        private void OnPlayerLeft(SessionPlayerData player)
        {
            Debug.Log($"[WaitingRoomPanel] Player left: {player.Username}");
            
            UpdatePlayerList();
            UpdatePlayerCount();
            UpdateStartButton();
            UpdateStatus($"{player.Username} left the session.");
        }
        
        private void OnSessionUpdated(SessionData session)
        {
            _currentSession = session;
            UpdateSessionInfo();
        }
        
        private void OnGameStarting()
        {
            UpdateStatus("Game starting...");
            
            if (_startGameButton != null)
                _startGameButton.interactable = false;
                
            if (_leaveButton != null)
                _leaveButton.interactable = false;
        }
        
        #endregion

        #region Button Handlers
        
        private void OnStartGameClicked()
        {
            if (!_isOwner)
            {
                Debug.LogWarning("[WaitingRoomPanel] Only owner can start the game.");
                return;
            }
            
            Debug.Log("[WaitingRoomPanel] Starting game...");
            UpdateStatus("Starting game...");
            
            if (_startGameButton != null)
                _startGameButton.interactable = false;
            
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.StartGame();
            }
        }
        
        /// <summary>
        /// FIXED: Properly leaves session and navigates back
        /// </summary>
        private void OnLeaveClicked()
        {
            Debug.Log("[WaitingRoomPanel] Leaving session...");
            
            // Leave the network session
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.LeaveSession();
            }
            
            // Navigate back to session browser
            if (UIManager != null)
            {
                UIManager.LeaveSessionAndReturnToBrowser();
            }
        }
        
        #endregion

        #region UI Updates
        
        private void UpdateSessionInfo()
        {
            if (_currentSession == null) return;
            
            if (_sessionNameText != null)
                _sessionNameText.text = _currentSession.SessionName;
                
            if (_wavesText != null)
                _wavesText.text = $"Waves: {_currentSession.WaveCountDisplay}";
                
            if (_ownerText != null)
                _ownerText.text = $"Host: {_currentSession.OwnerName}";
                
            UpdatePlayerCount();
        }
        
        private void UpdatePlayerCount()
        {
            if (_playerCountText == null || _currentSession == null) return;
            
            _playerCountText.text = $"Players: {_currentSession.PlayerCountDisplay}";
        }
        
        private void UpdatePlayerList()
        {
            ClearPlayerList();
            
            if (_playerListItemPrefab == null || _playerListContent == null) return;
            if (NetworkManager.Instance == null) return;
            
            var players = NetworkManager.Instance.GetConnectedPlayers();
            
            foreach (var player in players)
            {
                GameObject itemObj = Instantiate(_playerListItemPrefab, _playerListContent);
                PlayerListItem item = itemObj.GetComponent<PlayerListItem>();
                
                if (item != null)
                {
                    item.SetPlayerData(player);
                    _playerItems.Add(item);
                }
            }
        }
        
        private void ClearPlayerList()
        {
            foreach (var item in _playerItems)
            {
                if (item != null)
                {
                    Destroy(item.gameObject);
                }
            }
            _playerItems.Clear();
        }
        
        private void UpdateStartButton()
        {
            if (_startGameButton == null) return;
            
            _startGameButton.gameObject.SetActive(_isOwner);
            
            if (_isOwner)
            {
                int playerCount = NetworkManager.Instance?.GetConnectedPlayers().Count ?? 0;
                bool canStart = playerCount >= _minPlayersToStart;
                
                _startGameButton.interactable = canStart;
                
                if (_startButtonText != null)
                {
                    _startButtonText.text = canStart ? "Start Game" : 
                        $"Need {_minPlayersToStart - playerCount} more player(s)";
                }
            }
        }
        
        private void UpdateStatus(string status)
        {
            if (_statusText != null)
            {
                _statusText.text = status;
            }
        }
        
        #endregion
    }
}