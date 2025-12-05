using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TheColosseumChallenge.Data;

namespace TheColosseumChallenge.UI.Components
{
    /// <summary>
    /// UI component for displaying a single player in the waiting room.
    /// </summary>
    public class PlayerListItem : MonoBehaviour
    {
        #region Inspector References
        
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI _usernameText;
        [SerializeField] private Image _playerColorIndicator;
        [SerializeField] private GameObject _ownerBadge;
        [SerializeField] private GameObject _readyIndicator;
        [SerializeField] private TextMeshProUGUI _statusText;
        
        [Header("Visual Settings")]
        [SerializeField] private Color _readyColor = Color.green;
        [SerializeField] private Color _notReadyColor = Color.gray;
        
        #endregion

        #region Private Fields
        
        private SessionPlayerData _playerData;
        
        #endregion

        #region Public Methods
        
        /// <summary>
        /// Sets the player data to display.
        /// </summary>
        /// <param name="player">The player data.</param>
        public void SetPlayerData(SessionPlayerData player)
        {
            _playerData = player;
            UpdateDisplay();
        }
        
        /// <summary>
        /// Updates the ready state display.
        /// </summary>
        /// <param name="isReady">Whether the player is ready.</param>
        public void SetReady(bool isReady)
        {
            if (_playerData != null)
            {
                _playerData.IsReady = isReady;
                UpdateDisplay();
            }
        }
        
        #endregion

        #region Display Updates
        
        /// <summary>
        /// Updates the UI to reflect current player data.
        /// </summary>
        private void UpdateDisplay()
        {
            if (_playerData == null) return;
            
            // Username
            if (_usernameText != null)
            {
                _usernameText.text = _playerData.Username;
            }
            
            // Player color
            if (_playerColorIndicator != null)
            {
                _playerColorIndicator.color = _playerData.PlayerColor;
            }
            
            // Owner badge
            if (_ownerBadge != null)
            {
                _ownerBadge.SetActive(_playerData.IsOwner);
            }
            
            // Ready indicator
            if (_readyIndicator != null)
            {
                var indicatorImage = _readyIndicator.GetComponent<Image>();
                if (indicatorImage != null)
                {
                    indicatorImage.color = _playerData.IsReady ? _readyColor : _notReadyColor;
                }
            }
            
            // Status text
            if (_statusText != null)
            {
                if (_playerData.IsOwner)
                {
                    _statusText.text = "Host";
                }
                else if (_playerData.IsReady)
                {
                    _statusText.text = "Ready";
                }
                else
                {
                    _statusText.text = "Not Ready";
                }
            }
        }
        
        #endregion
    }
}