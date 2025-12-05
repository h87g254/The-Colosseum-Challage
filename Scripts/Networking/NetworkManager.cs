using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;
using Fusion.Sockets;
using TheColosseumChallenge.Core;
using TheColosseumChallenge.Data;
using GameMode = Fusion.GameMode;

namespace TheColosseumChallenge.Networking
{
    /// <summary>
    /// Session state tracking
    /// </summary>
    public enum SessionState
    {
        None,
        CreatingSession,
        InWaitingRoom,
        InGame,
        JoiningSession,
        Leaving
    }

    public class NetworkManager : MonoBehaviour, INetworkRunnerCallbacks
    {
        #region Singleton
        
        private static NetworkManager _instance;
        public static NetworkManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindFirstObjectByType<NetworkManager>();
                return _instance;
            }
        }
        
        #endregion

        #region Inspector Fields
        
        [Header("Network Configuration")]
        [SerializeField] private NetworkObject _playerPrefab;
        [SerializeField] private string _gameSceneName = "GameScene";
        [SerializeField] private string _menuSceneName = "MainMenu";
        [SerializeField] private string _spawnPointTag = "SpawnPoint";
        
        #endregion

        #region Private Fields
        
        private NetworkRunner _gameRunner;
        private NetworkRunner _lobbyRunner;
        private SessionData _currentSession;
        private List<SessionInfo> _cachedSessionList = new List<SessionInfo>();
        private SessionState _sessionState = SessionState.None;
        private bool _isSessionOwner;
        private string _currentSessionId;
        
        #endregion

        #region Events
        
        public event Action SessionCreated;
        public event Action<string> SessionCreationFailed;
        public event Action<List<SessionData>> SessionListUpdated;
        public event Action<string> JoinSessionFailed;
        public event Action<SessionPlayerData> PlayerJoinedSession;
        public event Action<SessionPlayerData> PlayerLeftSession;
        public event Action<SessionData> SessionUpdated;
        public event Action GameStarting;
        
        #endregion

        #region Properties
        
        public bool IsConnected => _gameRunner != null && _gameRunner.IsRunning;
        public bool IsSessionOwner => _isSessionOwner;
        public SessionData CurrentSession => _currentSession;
        public SessionState CurrentSessionState => _sessionState;
        public bool IsInSession => _sessionState == SessionState.InWaitingRoom || _sessionState == SessionState.InGame;
        
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
            DontDestroyOnLoad(gameObject);
            
            if (_playerPrefab == null)
            {
                Debug.LogError("[NetworkManager] Player prefab not assigned!");
            }
            
            Debug.Log("[NetworkManager] Initialized.");
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        private void OnApplicationQuit()
        {
            if (_gameRunner != null)
                _gameRunner.Shutdown();
            if (_lobbyRunner != null)
                _lobbyRunner.Shutdown();
        }
        
        #endregion

        #region Session Browser / Lobby

        /// <summary>
        /// Refreshes the list of available sessions
        /// </summary>
        public async void RefreshSessionList()
        {
            Debug.Log("[NetworkManager] Refreshing session list...");
            
            try
            {
                // Create lobby runner if it doesn't exist
                if (_lobbyRunner == null || !_lobbyRunner.IsRunning)
                {
                    await CreateLobbyRunner();
                }
                
                // Wait for session list callback to populate
                await Task.Delay(1000);
                
                // Convert cached session list
                var sessions = ConvertSessionList(_cachedSessionList);
                
                SessionListUpdated?.Invoke(sessions);
                
                Debug.Log($"[NetworkManager] Found {sessions.Count} sessions.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkManager] Session list refresh failed: {e.Message}");
                SessionListUpdated?.Invoke(new List<SessionData>());
            }
        }

        /// <summary>
        /// Converts Fusion SessionInfo list to our SessionData format
        /// </summary>
        private List<SessionData> ConvertSessionList(List<SessionInfo> sessionInfoList)
        {
            var sessions = new List<SessionData>();
            
            if (sessionInfoList != null)
            {
                foreach (var sessionInfo in sessionInfoList)
                {
                    // Only show public and open sessions
                    if (sessionInfo.IsOpen && sessionInfo.IsVisible)
                    {
                        try
                        {
                            sessions.Add(new SessionData(sessionInfo));
                        }
                        catch (Exception e)
                        {
                            Debug.LogWarning($"[NetworkManager] Failed to convert session: {e.Message}");
                        }
                    }
                }
            }
            
            return sessions;
        }

        /// <summary>
        /// Creates a lobby runner for browsing sessions
        /// </summary>
        private async Task CreateLobbyRunner()
        {
            // Shutdown existing lobby runner
            if (_lobbyRunner != null)
            {
                await _lobbyRunner.Shutdown();
                _lobbyRunner = null;
            }
            
            // Create new lobby runner
            var lobbyRunnerGO = new GameObject("LobbyRunner");
            DontDestroyOnLoad(lobbyRunnerGO);
            
            _lobbyRunner = lobbyRunnerGO.AddComponent<NetworkRunner>();
            _lobbyRunner.AddCallbacks(this);
            
            // Start lobby session
            var result = await _lobbyRunner.JoinSessionLobby(SessionLobby.ClientServer);
            
            if (!result.Ok)
            {
                Debug.LogError($"[NetworkManager] Failed to join lobby: {result.ShutdownReason}");
                if (_lobbyRunner != null)
                {
                    Destroy(_lobbyRunner.gameObject);
                    _lobbyRunner = null;
                }
            }
            else
            {
                Debug.Log("[NetworkManager] Lobby runner created successfully.");
            }
        }
        
        #endregion

        #region Create Session

        /// <summary>
        /// Creates a new multiplayer session
        /// </summary>
        public async void CreateSession(SessionConfiguration config)
        {
            if (!config.IsValid())
            {
                Debug.LogError("[NetworkManager] Invalid session configuration!");
                SessionCreationFailed?.Invoke("Invalid configuration");
                return;
            }
            
            if (_sessionState != SessionState.None)
            {
                Debug.LogWarning("[NetworkManager] Already in a session!");
                SessionCreationFailed?.Invoke("Already in a session");
                return;
            }
            
            _sessionState = SessionState.CreatingSession;
            
            Debug.Log($"[NetworkManager] Creating session: {config.SessionName}");
            
            try
            {
                // Shutdown lobby runner (we'll create a game runner)
                await ShutdownLobbyRunner();
                
                // Create game runner
                await CreateGameRunner();
                
                if (_gameRunner == null)
                {
                    throw new Exception("Failed to create game runner");
                }
                
                // Prepare session properties
                var sessionProps = SessionData.ToProperties(new SessionData
                {
                    SessionName = config.SessionName,
                    MaxPlayerCount = config.MaxPlayers,
                    WaveCount = config.WaveCount,
                    Status = SessionStatus.Waiting,
                    OwnerName = PlayerDataManager.Instance?.Username ?? "Host",
                    Region = string.Empty
                });
                
                // Get current scene for session creation
                Scene currentScene = SceneManager.GetActiveScene();
                
                // Start as host
                var args = new StartGameArgs
                {
                    GameMode = GameMode.Host,
                    SessionName = GenerateSessionId(),
                    Scene = SceneRef.FromIndex(currentScene.buildIndex),
                    SceneManager = _gameRunner.GetComponent<NetworkSceneManagerDefault>(),
                    SessionProperties = sessionProps,
                    PlayerCount = config.MaxPlayers,
                    IsOpen = config.IsPublic,
                    IsVisible = config.IsPublic
                };
                
                var result = await _gameRunner.StartGame(args);
                
                if (result.Ok)
                {
                    _currentSessionId = args.SessionName;
                    _currentSession = new SessionData
                    {
                        SessionId = args.SessionName,
                        SessionName = config.SessionName,
                        MaxPlayerCount = config.MaxPlayers,
                        WaveCount = config.WaveCount,
                        CurrentPlayerCount = 1,
                        Status = SessionStatus.Waiting,
                        OwnerName = PlayerDataManager.Instance?.Username ?? "Host"
                    };
                    
                    _isSessionOwner = true;
                    _sessionState = SessionState.InWaitingRoom;
                    
                    GameManager.Instance?.SetGameMode(Core.GameMode.Multiplayer);
                    GameManager.Instance?.ConfigureWaves(config.WaveCount);
                    
                    Debug.Log("[NetworkManager] Session created successfully.");
                    SessionCreated?.Invoke();
                }
                else
                {
                    _sessionState = SessionState.None;
                    Debug.LogError($"[NetworkManager] Failed to create session: {result.ShutdownReason}");
                    SessionCreationFailed?.Invoke(result.ShutdownReason.ToString());
                }
            }
            catch (Exception e)
            {
                _sessionState = SessionState.None;
                Debug.LogError($"[NetworkManager] Session creation error: {e.Message}");
                SessionCreationFailed?.Invoke(e.Message);
            }
        }

        /// <summary>
        /// Generates a unique session ID
        /// </summary>
        private string GenerateSessionId()
        {
            return $"Session_{Guid.NewGuid().ToString().Substring(0, 8)}";
        }
        
        #endregion

        #region Join Session

        /// <summary>
        /// Joins an existing session
        /// </summary>
        public async void JoinSession(string sessionId)
        {
            if (_sessionState != SessionState.None)
            {
                Debug.LogWarning("[NetworkManager] Already in a session!");
                JoinSessionFailed?.Invoke("Already in a session");
                return;
            }
            
            _sessionState = SessionState.JoiningSession;
            
            Debug.Log($"[NetworkManager] Joining session: {sessionId}");
            
            try
            {
                // Shutdown lobby runner
                await ShutdownLobbyRunner();
                
                // Create game runner
                await CreateGameRunner();
                
                if (_gameRunner == null)
                {
                    throw new Exception("Failed to create game runner");
                }
                
                // Get current scene
                Scene currentScene = SceneManager.GetActiveScene();
                
                // Join as client
                var args = new StartGameArgs
                {
                    GameMode = GameMode.Client,
                    SessionName = sessionId,
                    Scene = SceneRef.FromIndex(currentScene.buildIndex),
                    SceneManager = _gameRunner.GetComponent<NetworkSceneManagerDefault>()
                };
                
                var result = await _gameRunner.StartGame(args);
                
                if (result.Ok)
                {
                    _currentSessionId = sessionId;
                    _isSessionOwner = false;
                    _sessionState = SessionState.InWaitingRoom;
                    
                    Debug.Log("[NetworkManager] Joined session successfully.");
                    
                    // Session data will be updated via network
                }
                else
                {
                    _sessionState = SessionState.None;
                    Debug.LogError($"[NetworkManager] Failed to join session: {result.ShutdownReason}");
                    JoinSessionFailed?.Invoke(result.ShutdownReason.ToString());
                }
            }
            catch (Exception e)
            {
                _sessionState = SessionState.None;
                Debug.LogError($"[NetworkManager] Join session error: {e.Message}");
                JoinSessionFailed?.Invoke(e.Message);
            }
        }
        
        #endregion

        #region Leave Session

        /// <summary>
        /// Leaves the current session and returns to menu
        /// </summary>
        public async void LeaveSession()
        {
            if (_sessionState == SessionState.None || _sessionState == SessionState.Leaving)
            {
                Debug.LogWarning("[NetworkManager] Not in a session.");
                return;
            }
            
            Debug.Log("[NetworkManager] Leaving session...");
            
            _sessionState = SessionState.Leaving;
            
            try
            {
                // Shutdown game runner
                if (_gameRunner != null)
                {
                    await _gameRunner.Shutdown();
                    _gameRunner = null;
                }
                
                // Clear session data
                _currentSession = null;
                _currentSessionId = null;
                _isSessionOwner = false;
                _sessionState = SessionState.None;
                
                // Load menu scene if needed
                if (SceneManager.GetActiveScene().name != _menuSceneName)
                {
                    SceneManager.LoadScene(_menuSceneName);
                }
                
                Debug.Log("[NetworkManager] Left session successfully.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkManager] Error leaving session: {e.Message}");
                _sessionState = SessionState.None;
            }
        }
        
        #endregion

        #region Start Game

        /// <summary>
        /// Starts the game (host only)
        /// </summary>
        public void StartGame()
        {
            if (!_isSessionOwner)
            {
                Debug.LogWarning("[NetworkManager] Only the host can start the game!");
                return;
            }
            
            if (_gameRunner == null || !_gameRunner.IsRunning)
            {
                Debug.LogError("[NetworkManager] Game runner not active!");
                return;
            }
            
            Debug.Log("[NetworkManager] Starting game...");
            
            _sessionState = SessionState.InGame;
            
            GameStarting?.Invoke();
            
            // Update session status
            if (_currentSession != null)
            {
                _currentSession.Status = SessionStatus.InProgress;
            }
            
            // Load game scene
            int sceneIndex = SceneUtility.GetBuildIndexByScenePath(_gameSceneName);
            if (sceneIndex >= 0)
            {
                // FIXED: Use proper Fusion 2 scene loading
                _gameRunner.LoadScene(SceneRef.FromIndex(sceneIndex));
                
                GameManager.Instance?.SetGameMode(Core.GameMode.Multiplayer);
                GameManager.Instance?.ConfigureWaves(_currentSession?.WaveCount ?? 10);
            }
            else
            {
                Debug.LogError($"[NetworkManager] Game scene '{_gameSceneName}' not found in build settings!");
            }
        }
        
        #endregion

        #region Runner Management

        /// <summary>
        /// Creates the game runner
        /// </summary>
        private async Task CreateGameRunner()
        {
            // Shutdown existing game runner
            if (_gameRunner != null)
            {
                await _gameRunner.Shutdown();
                _gameRunner = null;
            }
            
            // Create new game runner
            var gameRunnerGO = new GameObject("GameRunner");
            DontDestroyOnLoad(gameRunnerGO);
            
            _gameRunner = gameRunnerGO.AddComponent<NetworkRunner>();
            _gameRunner.ProvideInput = true;
            
            // Add scene manager
            var sceneManager = gameRunnerGO.AddComponent<NetworkSceneManagerDefault>();
            
            // Add callbacks
            _gameRunner.AddCallbacks(this);
            
            Debug.Log("[NetworkManager] Game runner created.");
        }

        /// <summary>
        /// Shuts down the lobby runner
        /// </summary>
        private async Task ShutdownLobbyRunner()
        {
            if (_lobbyRunner != null && _lobbyRunner.IsRunning)
            {
                await _lobbyRunner.Shutdown();
                
                if (_lobbyRunner != null)
                {
                    Destroy(_lobbyRunner.gameObject);
                    _lobbyRunner = null;
                }
                
                Debug.Log("[NetworkManager] Lobby runner shutdown.");
            }
        }
        
        #endregion

        #region Player Management

        /// <summary>
        /// Gets list of connected players
        /// </summary>
        public List<SessionPlayerData> GetConnectedPlayers()
        {
            var players = new List<SessionPlayerData>();
            
            if (_gameRunner == null || !_gameRunner.IsRunning)
                return players;
            
            foreach (var player in _gameRunner.ActivePlayers)
            {
                players.Add(new SessionPlayerData
                {
                    PlayerRef = player,
                    Username = $"Player_{player.PlayerId}",
                    IsOwner = player == _gameRunner.LocalPlayer && _isSessionOwner,
                    IsReady = true,
                    PlayerColor = UnityEngine.Color.white
                });
            }
            
            return players;
        }

        /// <summary>
        /// Spawns a player for the given PlayerRef
        /// </summary>
        private void SpawnPlayer(NetworkRunner runner, PlayerRef player)
        {
            if (_playerPrefab == null)
            {
                Debug.LogError("[NetworkManager] Player prefab not assigned!");
                return;
            }
            
            // Find spawn point
            Vector3 spawnPosition = GetSpawnPosition(player.PlayerId);
            Quaternion spawnRotation = Quaternion.identity;
            
            // Spawn player object
            NetworkObject playerObject = runner.Spawn(
                _playerPrefab,
                spawnPosition,
                spawnRotation,
                player
            );
            
            Debug.Log($"[NetworkManager] Spawned player for {player} at {spawnPosition}");
        }

        /// <summary>
        /// Gets a spawn position for a player
        /// </summary>
        private Vector3 GetSpawnPosition(int playerIndex)
        {
            var spawnPoints = GameObject.FindGameObjectsWithTag(_spawnPointTag);
            
            if (spawnPoints.Length > 0)
            {
                int index = playerIndex % spawnPoints.Length;
                return spawnPoints[index].transform.position;
            }
            
            // Fallback
            return Vector3.up;
        }
        
        #endregion

        #region INetworkRunnerCallbacks

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            Debug.Log($"[NetworkManager] Player joined: {player}");
            
            var playerData = new SessionPlayerData
            {
                PlayerRef = player,
                Username = $"Player_{player.PlayerId}",
                IsOwner = player == runner.LocalPlayer && _isSessionOwner,
                IsReady = false
            };
            
            if (_currentSession != null)
            {
                _currentSession.CurrentPlayerCount++;
            }
            
            PlayerJoinedSession?.Invoke(playerData);
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            Debug.Log($"[NetworkManager] Player left: {player}");
            
            var playerData = new SessionPlayerData
            {
                PlayerRef = player,
                Username = $"Player_{player.PlayerId}"
            };
            
            if (_currentSession != null)
            {
                _currentSession.CurrentPlayerCount--;
            }
            
            PlayerLeftSession?.Invoke(playerData);
        }

        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
            // Input is handled by PlayerInputHandler
        }

        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
        {
        }

        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            Debug.Log($"[NetworkManager] Runner shutdown: {shutdownReason}");
            
            if (runner == _gameRunner)
            {
                _gameRunner = null;
            }
            else if (runner == _lobbyRunner)
            {
                _lobbyRunner = null;
            }
        }

        public void OnConnectedToServer(NetworkRunner runner)
        {
            Debug.Log("[NetworkManager] Connected to server.");
        }

        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            Debug.LogWarning($"[NetworkManager] Disconnected: {reason}");
        }

        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
        {
        }

        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {
            Debug.LogError($"[NetworkManager] Connect failed: {reason}");
        }

        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
        {
        }

        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {
            Debug.Log($"[NetworkManager] Session list updated: {sessionList.Count} sessions.");
            
            // FIXED: Cache the session list from the callback
            if (runner == _lobbyRunner)
            {
                _cachedSessionList = sessionList;
                
                // Convert and notify
                var sessions = ConvertSessionList(sessionList);
                SessionListUpdated?.Invoke(sessions);
            }
        }

        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
        {
        }

        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
        {
        }

        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
        {
        }

        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
        {
        }

        public void OnSceneLoadDone(NetworkRunner runner)
        {
            Debug.Log("[NetworkManager] Scene load complete.");
            
            // Spawn players when game scene loads
            if (runner == _gameRunner && runner.GameMode != GameMode.Client)
            {
                foreach (var player in runner.ActivePlayers)
                {
                    SpawnPlayer(runner, player);
                }
            }
        }

        public void OnSceneLoadStart(NetworkRunner runner)
        {
            Debug.Log("[NetworkManager] Scene load started.");
        }

        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
        }

        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
        }

        #endregion
    }
}