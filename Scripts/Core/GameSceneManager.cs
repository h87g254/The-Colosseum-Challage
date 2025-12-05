using System.Collections;
using UnityEngine;
using Fusion;
using TheColosseumChallenge.Networking;
using TheColosseumChallenge.Player;
using TheColosseumChallenge.UI.Panels;

namespace TheColosseumChallenge.Core
{
    /// <summary>
    /// Manages the game scene setup and runtime coordination.
    /// Handles player spawning, wave management, and scene-specific systems.
    /// </summary>
    public class GameSceneManager : MonoBehaviour
    {
        #region Inspector References
        
        [Header("Spawn Points")]
        [SerializeField] private Transform[] _spawnPoints;
        
        [Header("UI References")]
        [SerializeField] private PauseMenuPanel _pauseMenuPanel;
        
        [Header("Scene Settings")]
        [SerializeField] private float _spawnDelay = 0.5f;
        
        #endregion

        #region Private Fields
        
        private bool _isInitialized;
        
        #endregion

        #region Unity Lifecycle
        
        private void Start()
        {
            StartCoroutine(InitializeScene());
        }
        
        #endregion

        #region Initialization
        
        /// <summary>
        /// Initializes the game scene based on game mode.
        /// </summary>
        private IEnumerator InitializeScene()
        {
            Debug.Log("[GameSceneManager] Initializing scene...");
            
            // CRITICAL FIX: Hide all UI panels when entering game scene
            HideAllUIPanels();
            
            // CRITICAL FIX: Lock cursor immediately
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            // Wait for GameManager
            while (GameManager.Instance == null)
            {
                yield return null;
            }
            
            // Short delay for scene to settle
            yield return new WaitForSeconds(_spawnDelay);
            
            // Initialize based on game mode
            if (GameManager.Instance.CurrentGameMode == GameMode.Singleplayer)
            {
                InitializeSingleplayer();
            }
            else if (GameManager.Instance.CurrentGameMode == GameMode.Multiplayer)
            {
                InitializeMultiplayer();
            }
            else
            {
                Debug.LogWarning("[GameSceneManager] Unknown game mode, defaulting to singleplayer.");
                GameManager.Instance.SetGameMode(GameMode.Singleplayer);
                InitializeSingleplayer();
            }
            
            // Initialize UI
            InitializeUI();
            
            _isInitialized = true;
            
            Debug.Log("[GameSceneManager] Scene initialization complete.");
        }
        
        /// <summary>
        /// CRITICAL FIX: Hides all UI panels when entering game scene.
        /// </summary>
        private void HideAllUIPanels()
        {
            if (UI.UIManager.Instance != null)
            {
                var currentPanel = UI.UIManager.Instance.GetCurrentPanel();
                if (currentPanel != null)
                {
                    Debug.Log("[GameSceneManager] Hiding UI panel before game start.");
                    currentPanel.Hide(immediate: true);
                }
            }
        }
        
        /// <summary>
        /// FIXED: Initializes singleplayer mode.
        /// Player spawning is now handled by NetworkManager automatically.
        /// </summary>
        private void InitializeSingleplayer()
        {
            Debug.Log("[GameSceneManager] Initializing singleplayer...");
            
            // CRITICAL FIX: Do NOT manually spawn player here!
            // Player spawning is handled by NetworkManager.OnSceneLoadDone()
            // when the Fusion session is created in GameManager.StartSingleplayerGame()
            
            // Start first wave
            GameManager.Instance.AdvanceWave();
            
            Debug.Log("[GameSceneManager] Singleplayer initialized - player will spawn via NetworkManager.");
        }
        
        /// <summary>
        /// Initializes multiplayer mode.
        /// </summary>
        private void InitializeMultiplayer()
        {
            Debug.Log("[GameSceneManager] Initializing multiplayer...");
            
            // Player spawning is handled by NetworkManager
            // Just ensure we're connected
            if (NetworkManager.Instance == null || !NetworkManager.Instance.IsConnected)
            {
                Debug.LogError("[GameSceneManager] Not connected to network in multiplayer mode!");
                GameManager.Instance.LoadMainMenu();
                return;
            }
            
            // Start first wave (host only in multiplayer)
            if (NetworkManager.Instance.IsSessionOwner)
            {
                GameManager.Instance.AdvanceWave();
            }
            
            Debug.Log("[GameSceneManager] Multiplayer initialized.");
        }
        
        /// <summary>
        /// FIXED: Initializes UI elements and ensures cursor stays locked.
        /// </summary>
        private void InitializeUI()
        {
            // Ensure cursor stays locked for gameplay
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            // Initialize pause menu if assigned
            if (_pauseMenuPanel != null)
            {
                _pauseMenuPanel.Initialize(null); // No UIManager in game scene
            }
            
            Debug.Log("[GameSceneManager] UI initialized and cursor locked.");
        }
        
        #endregion

        #region Spawn Helper Methods
        
        /// <summary>
        /// Gets a spawn position by index.
        /// Used by NetworkManager for player spawning.
        /// </summary>
        /// <param name="index">Spawn point index.</param>
        /// <returns>The spawn position.</returns>
        public Vector3 GetSpawnPosition(int index)
        {
            if (_spawnPoints != null && _spawnPoints.Length > 0)
            {
                int safeIndex = index % _spawnPoints.Length;
                if (_spawnPoints[safeIndex] != null)
                {
                    return _spawnPoints[safeIndex].position;
                }
            }
            
            // Fallback to center of scene
            return Vector3.up;
        }
        
        /// <summary>
        /// Gets a spawn rotation by index.
        /// Used by NetworkManager for player spawning.
        /// </summary>
        /// <param name="index">Spawn point index.</param>
        /// <returns>The spawn rotation.</returns>
        public Quaternion GetSpawnRotation(int index)
        {
            if (_spawnPoints != null && _spawnPoints.Length > 0)
            {
                int safeIndex = index % _spawnPoints.Length;
                if (_spawnPoints[safeIndex] != null)
                {
                    return _spawnPoints[safeIndex].rotation;
                }
            }
            
            return Quaternion.identity;
        }
        
        #endregion
    }
}