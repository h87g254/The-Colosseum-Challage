using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TheColosseumChallenge.Data;

namespace TheColosseumChallenge.UI.Panels
{
    /// <summary>
    /// Panel for initial username entry when the player first starts the game.
    /// Validates username input and saves it for future sessions.
    /// </summary>
    public class UsernameEntryPanel : BasePanel
    {
        #region Inspector References
        
        [Header("UI Elements")]
        [SerializeField] private TMP_InputField _usernameInput;
        [SerializeField] private Button _continueButton;
        [SerializeField] private TextMeshProUGUI _errorText;
        [SerializeField] private TextMeshProUGUI _titleText;
        
        [Header("Validation Settings")]
        [SerializeField] private int _minUsernameLength = 2;
        [SerializeField] private int _maxUsernameLength = 20;
        
        #endregion

        #region Private Fields
        
        private bool _isValidating = false;
        
        #endregion

        #region Initialization
        
        protected override void OnInitialize()
        {
            base.OnInitialize();
            
            // Set up input field
            if (_usernameInput != null)
            {
                _usernameInput.characterLimit = _maxUsernameLength;
                _usernameInput.onValueChanged.AddListener(OnUsernameInputChanged);
                _usernameInput.onSubmit.AddListener(OnUsernameSubmit);
            }
            
            // Set up continue button
            if (_continueButton != null)
            {
                _continueButton.onClick.AddListener(OnContinueClicked);
            }
            
            // Hide error text initially
            SetError(string.Empty);
            
            Debug.Log("[UsernameEntryPanel] Initialized.");
        }
        
        #endregion

        #region Panel Lifecycle
        
        protected override void OnBeforeShow()
        {
            base.OnBeforeShow();
            
            // Clear any previous input
            if (_usernameInput != null)
            {
                _usernameInput.text = string.Empty;
            }
            
            SetError(string.Empty);
            UpdateContinueButton();
        }
        
        protected override void OnAfterShow()
        {
            base.OnAfterShow();
            
            // Focus the input field
            if (_usernameInput != null)
            {
                _usernameInput.Select();
                _usernameInput.ActivateInputField();
            }
        }
        
        #endregion

        #region Input Handling
        
        /// <summary>
        /// Called when the username input field value changes.
        /// </summary>
        /// <param name="value">The new input value.</param>
        private void OnUsernameInputChanged(string value)
        {
            // Clear error when user starts typing
            SetError(string.Empty);
            
            // Update button state
            UpdateContinueButton();
        }
        
        /// <summary>
        /// Called when the user submits the input field (Enter key).
        /// </summary>
        /// <param name="value">The submitted value.</param>
        private void OnUsernameSubmit(string value)
        {
            TrySubmitUsername();
        }
        
        /// <summary>
        /// Called when the continue button is clicked.
        /// </summary>
        private void OnContinueClicked()
        {
            TrySubmitUsername();
        }
        
        #endregion

        #region Username Validation
        
        /// <summary>
        /// Attempts to validate and save the username.
        /// </summary>
        private void TrySubmitUsername()
        {
            if (_isValidating) return;
            
            string username = _usernameInput != null ? _usernameInput.text.Trim() : string.Empty;
            
            // Validate username
            if (!ValidateUsername(username, out string errorMessage))
            {
                SetError(errorMessage);
                return;
            }
            
            _isValidating = true;
            
            // Save username
            if (PlayerDataManager.Instance != null)
            {
                if (PlayerDataManager.Instance.SetUsername(username))
                {
                    // Success - proceed to main menu
                    Debug.Log($"[UsernameEntryPanel] Username saved: {username}");
                    ProceedToMainMenu();
                }
                else
                {
                    SetError("Failed to save username. Please try again.");
                }
            }
            else
            {
                Debug.LogError("[UsernameEntryPanel] PlayerDataManager not found!");
                SetError("An error occurred. Please restart the game.");
            }
            
            _isValidating = false;
        }
        
        /// <summary>
        /// Validates the username against defined rules.
        /// </summary>
        /// <param name="username">The username to validate.</param>
        /// <param name="errorMessage">Output error message if validation fails.</param>
        /// <returns>True if valid, false otherwise.</returns>
        private bool ValidateUsername(string username, out string errorMessage)
        {
            errorMessage = string.Empty;
            
            // Check empty
            if (string.IsNullOrWhiteSpace(username))
            {
                errorMessage = "Please enter a username.";
                return false;
            }
            
            // Check minimum length
            if (username.Length < _minUsernameLength)
            {
                errorMessage = $"Username must be at least {_minUsernameLength} characters.";
                return false;
            }
            
            // Check maximum length
            if (username.Length > _maxUsernameLength)
            {
                errorMessage = $"Username cannot exceed {_maxUsernameLength} characters.";
                return false;
            }
            
            // Check for invalid characters (optional - allow alphanumeric and some special chars)
            foreach (char c in username)
            {
                if (!char.IsLetterOrDigit(c) && c != '_' && c != '-' && c != ' ')
                {
                    errorMessage = "Username can only contain letters, numbers, spaces, underscores, and hyphens.";
                    return false;
                }
            }
            
            return true;
        }
        
        #endregion

        #region UI Updates
        
        /// <summary>
        /// Updates the continue button interactable state.
        /// </summary>
        private void UpdateContinueButton()
        {
            if (_continueButton == null) return;
            
            string username = _usernameInput != null ? _usernameInput.text.Trim() : string.Empty;
            _continueButton.interactable = username.Length >= _minUsernameLength;
        }
        
        /// <summary>
        /// Sets the error message text.
        /// </summary>
        /// <param name="message">The error message to display. Empty string to hide.</param>
        private void SetError(string message)
        {
            if (_errorText == null) return;
            
            _errorText.text = message;
            _errorText.gameObject.SetActive(!string.IsNullOrEmpty(message));
        }
        
        #endregion

        #region Navigation
        
        /// <summary>
        /// Proceeds to the main menu after successful username entry.
        /// </summary>
        private void ProceedToMainMenu()
        {
            if (UIManager != null)
            {
                UIManager.ShowMainMenu();
            }
            else
            {
                Debug.LogError("[UsernameEntryPanel] UIManager not found!");
            }
        }
        
        #endregion
    }
}