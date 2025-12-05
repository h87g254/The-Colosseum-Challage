using UnityEngine;
using TheColosseumChallenge.Data;
using TheColosseumChallenge.Settings;
using TheColosseumChallenge.Networking;

namespace TheColosseumChallenge.Core
{
    /// <summary>
    /// Bootstrapper component that ensures all singleton managers are initialized
    /// in the correct order when the game starts.
    /// </summary>
    public class Bootstrapper : MonoBehaviour
    {
        #region Inspector Fields
        
        [Header("Manager Prefabs (Optional)")]
        [SerializeField] private GameObject _gameManagerPrefab;
        [SerializeField] private GameObject _playerDataManagerPrefab;
        [SerializeField] private GameObject _settingsManagerPrefab;
        [SerializeField] private GameObject _networkManagerPrefab;
        
        [Header("Settings")]
        [SerializeField] private bool _initializeOnAwake = true;
        
        #endregion

        #region Private Fields
        
        private static bool _isInitialized = false;
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            if (_initializeOnAwake)
            {
                Initialize();
            }
        }
        
        #endregion

        #region Initialization
        
        /// <summary>
        /// Initializes all game managers in the correct order.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
            {
                Debug.Log("[Bootstrapper] Already initialized, skipping.");
                return;
            }
            
            Debug.Log("[Bootstrapper] Initializing game systems...");
            
            // Initialize managers in order of dependency
            InitializeSettingsManager();
            InitializePlayerDataManager();
            InitializeGameManager();
            InitializeNetworkManager();
            
            _isInitialized = true;
            
            Debug.Log("[Bootstrapper] All systems initialized successfully.");
        }
        
        /// <summary>
        /// Initializes the SettingsManager.
        /// </summary>
        private void InitializeSettingsManager()
        {
            if (SettingsManager.Instance == null)
            {
                if (_settingsManagerPrefab != null)
                {
                    Instantiate(_settingsManagerPrefab);
                }
                else
                {
                    // Force creation via Instance property
                    var _ = SettingsManager.Instance;
                }
            }
            
            Debug.Log("[Bootstrapper] SettingsManager initialized.");
        }
        
        /// <summary>
        /// Initializes the PlayerDataManager.
        /// </summary>
        private void InitializePlayerDataManager()
        {
            if (PlayerDataManager.Instance == null)
            {
                if (_playerDataManagerPrefab != null)
                {
                    Instantiate(_playerDataManagerPrefab);
                }
                else
                {
                    var _ = PlayerDataManager.Instance;
                }
            }
            
            Debug.Log("[Bootstrapper] PlayerDataManager initialized.");
        }
        
        /// <summary>
        /// Initializes the GameManager.
        /// </summary>
        private void InitializeGameManager()
        {
            if (GameManager.Instance == null)
            {
                if (_gameManagerPrefab != null)
                {
                    Instantiate(_gameManagerPrefab);
                }
                else
                {
                    var _ = GameManager.Instance;
                }
            }
            
            Debug.Log("[Bootstrapper] GameManager initialized.");
        }
        
        /// <summary>
        /// Initializes the NetworkManager.
        /// </summary>
        private void InitializeNetworkManager()
        {
            if (NetworkManager.Instance == null)
            {
                if (_networkManagerPrefab != null)
                {
                    Instantiate(_networkManagerPrefab);
                }
                else
                {
                    var _ = NetworkManager.Instance;
                }
            }
            
            Debug.Log("[Bootstrapper] NetworkManager initialized.");
        }
        
        #endregion

        #region Static Methods
        
        /// <summary>
        /// Checks if the bootstrapper has completed initialization.
        /// </summary>
        public static bool IsInitialized => _isInitialized;
        
        /// <summary>
        /// Resets initialization state (for testing).
        /// </summary>
        public static void ResetInitialization()
        {
            _isInitialized = false;
        }
        
        #endregion
    }
}