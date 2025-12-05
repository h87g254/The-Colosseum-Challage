using System;
using TheColosseumChallenge.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TheColosseumChallenge.Core
{
    /// <summary>
    /// Defines the possible states of the game.
    /// </summary>
    public enum GameState
    {
        MainMenu,
        Loading,
        WaitingRoom,
        InGame,
        Paused,
        GameOver
    }

    /// <summary>
    /// Defines the game mode types.
    /// </summary>
    public enum GameMode
    {
        None,
        Singleplayer,
        Multiplayer
    }

    /// <summary>
    /// Central singleton manager that coordinates all game systems.
    /// Persists across scene loads and manages game state transitions.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        #region Singleton Pattern
        
        private static GameManager _instance;
        
        /// <summary>
        /// Global access point for the GameManager instance.
        /// </summary>
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<GameManager>();
                    
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("GameManager");
                        _instance = go.AddComponent<GameManager>();
                    }
                }
                return _instance;
            }
        }
        
        #endregion

        #region Events
        
        /// <summary>
        /// Fired when the game state changes. Provides old and new state.
        /// </summary>
        public event Action<GameState, GameState> OnGameStateChanged;
        
        /// <summary>
        /// Fired when the game mode changes.
        /// </summary>
        public event Action<GameMode> OnGameModeChanged;
        
        /// <summary>
        /// Fired when the game is paused or unpaused.
        /// </summary>
        public event Action<bool> OnPauseStateChanged;
        
        #endregion

        #region Properties
        
        /// <summary>
        /// Current state of the game.
        /// </summary>
        public GameState CurrentState { get; private set; } = GameState.MainMenu;
        
        /// <summary>
        /// Current game mode (singleplayer/multiplayer).
        /// </summary>
        public GameMode CurrentGameMode { get; private set; } = GameMode.None;
        
        /// <summary>
        /// Whether the game is currently paused.
        /// </summary>
        public bool IsPaused { get; private set; }
        
        /// <summary>
        /// Number of waves configured for current session.
        /// -1 indicates infinite waves.
        /// </summary>
        public int ConfiguredWaves { get; private set; } = 10;
        
        /// <summary>
        /// Current wave number in the game session.
        /// </summary>
        public int CurrentWave { get; private set; } = 0;
        
        /// <summary>
        /// Whether the current session has infinite waves.
        /// </summary>
        public bool IsInfiniteWaves => ConfiguredWaves == -1;
        
        #endregion

        #region Scene Names
        
        /// <summary>
        /// Constants for scene names to prevent typos and enable easy changes.
        /// </summary>
        public static class Scenes
        {
            public const string MainMenu = "MainMenu";
            public const string Game = "GameScene";
        }
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            // Implement singleton pattern with persistence
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            InitializeManager();
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
        
        /// <summary>
        /// Performs initial setup of the GameManager.
        /// </summary>
        private void InitializeManager()
        {
            // Subscribe to scene load events
            SceneManager.sceneLoaded += OnSceneLoaded;
            
            Debug.Log("[GameManager] Initialized successfully.");
        }
        
        /// <summary>
        /// Handles scene load events to update game state accordingly.
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log($"[GameManager] Scene loaded: {scene.name}");
            
            if (scene.name == Scenes.MainMenu)
            {
                SetGameState(GameState.MainMenu);
                SetPaused(false);
            }
            else if (scene.name == Scenes.Game)
            {
                SetGameState(GameState.InGame);
            }
        }
        
        #endregion

        #region State Management
        
        /// <summary>
        /// Changes the current game state and fires appropriate events.
        /// </summary>
        /// <param name="newState">The state to transition to.</param>
        public void SetGameState(GameState newState)
        {
            if (CurrentState == newState) return;
            
            GameState oldState = CurrentState;
            CurrentState = newState;
            
            Debug.Log($"[GameManager] State changed: {oldState} -> {newState}");
            
            OnGameStateChanged?.Invoke(oldState, newState);
        }
        
        /// <summary>
        /// Sets the current game mode.
        /// </summary>
        /// <param name="mode">The game mode to set.</param>
        public void SetGameMode(GameMode mode)
        {
            if (CurrentGameMode == mode) return;
            
            CurrentGameMode = mode;
            Debug.Log($"[GameManager] Game mode set to: {mode}");
            
            OnGameModeChanged?.Invoke(mode);
        }
        
        /// <summary>
        /// Configures the wave settings for the current session.
        /// </summary>
        /// <param name="waves">Number of waves. Use -1 for infinite.</param>
        public void ConfigureWaves(int waves)
        {
            ConfiguredWaves = waves;
            CurrentWave = 0;
            
            Debug.Log($"[GameManager] Waves configured: {(waves == -1 ? "Infinite" : waves.ToString())}");
        }
        
        #endregion

        #region Pause System
        
        /// <summary>
        /// Sets the pause state of the game.
        /// </summary>
        /// <param name="paused">True to pause, false to unpause.</param>
        public void SetPaused(bool paused)
        {
            if (IsPaused == paused) return;
            
            IsPaused = paused;
            
            // Only affect time scale in singleplayer
            if (CurrentGameMode == GameMode.Singleplayer)
            {
                Time.timeScale = paused ? 0f : 1f;
            }
            
            // Update cursor state
            Cursor.visible = paused;
            Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
            
            Debug.Log($"[GameManager] Game {(paused ? "paused" : "resumed")}");
            
            OnPauseStateChanged?.Invoke(paused);
        }
        
        /// <summary>
        /// Toggles the pause state.
        /// </summary>
        public void TogglePause()
        {
            SetPaused(!IsPaused);
        }
        
        #endregion

        #region Scene Transitions
        
        /// <summary>
        /// Loads the main menu scene.
        /// </summary>
        public void LoadMainMenu()
        {
            SetGameState(GameState.Loading);
            Time.timeScale = 1f; // Ensure time is normal when returning to menu
            SceneManager.LoadScene(Scenes.MainMenu);
        }
        
        /// <summary>
        /// Starts a singleplayer game session.
        /// </summary>
        /// <param name="waves">Number of waves. Use -1 for infinite.</param>
        public void StartSingleplayerGame(int waves)
        {
            SetGameMode(GameMode.Singleplayer);
            ConfigureWaves(waves);
    
            // We must use the NetworkManager even for Singleplayer
            // so that NetworkBehaviours (Movement/Camera) actually run.
            if (Networking.NetworkManager.Instance != null)
            {
                var config = new SessionConfiguration()
                {
                    SessionName = "SinglePlayer_Local",
                    MaxPlayers = 1, // Restrict to 1 player
                    WaveCount = waves,
                    IsPublic = false // Not visible in server browser
                };

                // This starts a Host session, which behaves exactly like Singleplayer
                Networking.NetworkManager.Instance.CreateSession(config);
            }
            else
            {
                Debug.LogError("[GameManager] NetworkManager missing! Cannot start Singleplayer.");
            }
        }
        
        /// <summary>
        /// Starts a multiplayer game session.
        /// Called when the session owner starts the game.
        /// </summary>
        public void StartMultiplayerGame()
        {
            SetGameMode(GameMode.Multiplayer);
            SetGameState(GameState.Loading);
            
            // Scene loading is handled by NetworkManager for multiplayer
        }
        
        #endregion

        #region Wave Management
        
        /// <summary>
        /// Advances to the next wave.
        /// </summary>
        /// <returns>True if there are more waves, false if game is complete.</returns>
        public bool AdvanceWave()
        {
            CurrentWave++;
            
            if (!IsInfiniteWaves && CurrentWave > ConfiguredWaves)
            {
                SetGameState(GameState.GameOver);
                return false;
            }
            
            Debug.Log($"[GameManager] Wave advanced to: {CurrentWave}");
            return true;
        }
        
        #endregion

        #region Application Lifecycle
        
        /// <summary>
        /// Quits the application gracefully.
        /// </summary>
        public void QuitGame()
        {
            Debug.Log("[GameManager] Quitting game...");
            
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
        
        #endregion
    }
}