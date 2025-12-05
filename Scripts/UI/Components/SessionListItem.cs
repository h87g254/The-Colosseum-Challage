using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TheColosseumChallenge.Data;

namespace TheColosseumChallenge.UI.Components
{
    /// <summary>
    /// UI component for a single session entry in the session browser list.
    /// </summary>
    public class SessionListItem : MonoBehaviour
    {
        #region Inspector References
        
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI _sessionNameText;
        [SerializeField] private TextMeshProUGUI _playerCountText;
        [SerializeField] private TextMeshProUGUI _wavesText;
        [SerializeField] private TextMeshProUGUI _statusText;
        [SerializeField] private Button _joinButton;
        [SerializeField] private TextMeshProUGUI _joinButtonText;
        
        [Header("Visual Feedback")]
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private Color _waitingColor = new Color(0.2f, 0.6f, 0.2f, 0.3f);
        [SerializeField] private Color _inProgressColor = new Color(0.6f, 0.6f, 0.2f, 0.3f);
        [SerializeField] private Color _fullColor = new Color(0.6f, 0.2f, 0.2f, 0.3f);
        
        #endregion

        #region Events
        
        /// <summary>
        /// Fired when the join button is clicked.
        /// </summary>
        public event Action<SessionData> OnJoinClicked;
        
        #endregion

        #region Private Fields
        
        private SessionData _sessionData;
        
        #endregion

        #region Initialization
        
        private void Awake()
        {
            if (_joinButton != null)
            {
                _joinButton.onClick.AddListener(HandleJoinClick);
            }
        }
        
        private void OnDestroy()
        {
            if (_joinButton != null)
            {
                _joinButton.onClick.RemoveListener(HandleJoinClick);
            }
        }
        
        #endregion

        #region Public Methods
        
        /// <summary>
        /// Sets the session data to display.
        /// </summary>
        /// <param name="session">The session data.</param>
        public void SetSessionData(SessionData session)
        {
            _sessionData = session;
            UpdateDisplay();
        }
        
        #endregion

        #region Display Updates
        
        /// <summary>
        /// Updates the UI to reflect current session data.
        /// </summary>
        private void UpdateDisplay()
        {
            if (_sessionData == null) return;
            
            // Session name
            if (_sessionNameText != null)
            {
                _sessionNameText.text = _sessionData.SessionName;
            }
            
            // Player count
            if (_playerCountText != null)
            {
                _playerCountText.text = _sessionData.PlayerCountDisplay;
            }
            
            // Waves
            if (_wavesText != null)
            {
                _wavesText.text = _sessionData.WaveCountDisplay;
            }
            
            // Status
            if (_statusText != null)
            {
                _statusText.text = _sessionData.StatusDisplay;
            }
            
            // Join button
            if (_joinButton != null)
            {
                _joinButton.interactable = _sessionData.CanJoin;
            }
            
            if (_joinButtonText != null)
            {
                if (_sessionData.IsFull)
                {
                    _joinButtonText.text = "Full";
                }
                else if (_sessionData.Status == SessionStatus.InProgress)
                {
                    _joinButtonText.text = "In Progress";
                }
                else
                {
                    _joinButtonText.text = "Join";
                }
            }
            
            // Background color
            UpdateBackgroundColor();
        }
        
        /// <summary>
        /// Updates the background color based on session state.
        /// </summary>
        private void UpdateBackgroundColor()
        {
            if (_backgroundImage == null || _sessionData == null) return;
            
            if (_sessionData.IsFull)
            {
                _backgroundImage.color = _fullColor;
            }
            else if (_sessionData.Status == SessionStatus.InProgress)
            {
                _backgroundImage.color = _inProgressColor;
            }
            else
            {
                _backgroundImage.color = _waitingColor;
            }
        }
        
        #endregion

        #region Event Handlers
        
        /// <summary>
        /// Handles join button click.
        /// </summary>
        private void HandleJoinClick()
        {
            if (_sessionData != null && _sessionData.CanJoin)
            {
                OnJoinClicked?.Invoke(_sessionData);
            }
        }
        
        #endregion
    }
}