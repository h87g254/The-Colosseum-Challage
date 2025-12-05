using System;
using System.Collections.Generic;
using Fusion;

namespace TheColosseumChallenge.Data
{
    /// <summary>
    /// Represents the current status of a multiplayer session.
    /// </summary>
    public enum SessionStatus
    {
        /// <summary>
        /// Session is waiting for players to join before starting.
        /// </summary>
        Waiting,
        
        /// <summary>
        /// Session is currently in progress (game has started).
        /// </summary>
        InProgress,
        
        /// <summary>
        /// Session has ended.
        /// </summary>
        Ended
    }

    /// <summary>
    /// Contains all information about a multiplayer game session.
    /// Used for displaying session information in the browser and waiting room.
    /// </summary>
    [Serializable]
    public class SessionData
    {
        #region Session Properties
        
        /// <summary>
        /// Unique identifier for the session (Photon session name).
        /// </summary>
        public string SessionId { get; set; }
        
        /// <summary>
        /// Display name for the session (set by owner).
        /// </summary>
        public string SessionName { get; set; }
        
        /// <summary>
        /// Current number of players in the session.
        /// </summary>
        public int CurrentPlayerCount { get; set; }
        
        /// <summary>
        /// Maximum number of players allowed in the session.
        /// </summary>
        public int MaxPlayerCount { get; set; }
        
        /// <summary>
        /// Number of waves configured for this session.
        /// -1 indicates infinite waves.
        /// </summary>
        public int WaveCount { get; set; }
        
        /// <summary>
        /// Current status of the session.
        /// </summary>
        public SessionStatus Status { get; set; }
        
        /// <summary>
        /// Username of the session owner/host.
        /// </summary>
        public string OwnerName { get; set; }
        
        /// <summary>
        /// Region where the session is hosted.
        /// </summary>
        public string Region { get; set; }
        
        #endregion

        #region Computed Properties
        
        /// <summary>
        /// Whether the session is full.
        /// </summary>
        public bool IsFull => CurrentPlayerCount >= MaxPlayerCount;
        
        /// <summary>
        /// Whether players can join this session.
        /// </summary>
        public bool CanJoin => Status == SessionStatus.Waiting && !IsFull;
        
        /// <summary>
        /// Formatted string showing player count.
        /// </summary>
        public string PlayerCountDisplay => $"{CurrentPlayerCount}/{MaxPlayerCount}";
        
        /// <summary>
        /// Formatted string showing wave count.
        /// </summary>
        public string WaveCountDisplay => WaveCount == -1 ? "∞" : WaveCount.ToString();
        
        /// <summary>
        /// Status as a readable string.
        /// </summary>
        public string StatusDisplay => Status switch
        {
            SessionStatus.Waiting => "Waiting",
            SessionStatus.InProgress => "In Progress",
            SessionStatus.Ended => "Ended",
            _ => "Unknown"
        };
        
        #endregion

        #region Constructors
        
        /// <summary>
        /// Creates a new empty session data instance.
        /// </summary>
        public SessionData()
        {
            SessionId = string.Empty;
            SessionName = "New Session";
            CurrentPlayerCount = 0;
            MaxPlayerCount = 4;
            WaveCount = 10;
            Status = SessionStatus.Waiting;
            OwnerName = string.Empty;
            Region = string.Empty;
        }
        
        /// <summary>
        /// Creates session data from Photon SessionInfo.
        /// </summary>
        /// <param name="sessionInfo">Photon session info object.</param>
        public SessionData(SessionInfo sessionInfo)
        {
            SessionId = sessionInfo.Name;
            CurrentPlayerCount = sessionInfo.PlayerCount;
            MaxPlayerCount = sessionInfo.MaxPlayers;
            
            // Parse custom properties from session
            if (sessionInfo.Properties != null)
            {
                SessionName = sessionInfo.Properties.TryGetValue("SessionName", out var name) 
                    ? name.ToString() 
                    : sessionInfo.Name;
                    
                if (sessionInfo.Properties.TryGetValue("WaveCount", out var waves))
                    WaveCount = int.Parse(waves.ToString());
                    
                if (sessionInfo.Properties.TryGetValue("Status", out var status))
                    Status = (SessionStatus)int.Parse(status.ToString());
                    
                if (sessionInfo.Properties.TryGetValue("OwnerName", out var owner))
                    OwnerName = owner.ToString();
                    
                if (sessionInfo.Properties.TryGetValue("Region", out var region))
                    Region = region.ToString();
            }
            else
            {
                SessionName = sessionInfo.Name;
            }
        }
        
        #endregion

        #region Static Helper Methods
        
        /// <summary>
        /// Creates session properties dictionary for Photon.
        /// Fusion 2 uses implicit conversion for SessionProperty values.
        /// </summary>
        /// <param name="data">Session data to convert.</param>
        /// <returns>Dictionary of session properties.</returns>
        public static Dictionary<string, SessionProperty> ToProperties(SessionData data)
        {
            return new Dictionary<string, SessionProperty>
            {
                ["SessionName"] = data.SessionName ?? string.Empty,
                ["WaveCount"] = data.WaveCount,
                ["Status"] = (int)data.Status,
                ["OwnerName"] = data.OwnerName ?? string.Empty,
                ["Region"] = data.Region ?? string.Empty
            };
        }
        
        #endregion
    }

    /// <summary>
    /// Represents a player in a multiplayer session.
    /// </summary>
    [Serializable]
    public class SessionPlayerData
    {
        /// <summary>
        /// Unique player reference from Photon.
        /// </summary>
        public PlayerRef PlayerRef { get; set; }
        
        /// <summary>
        /// Display name of the player.
        /// </summary>
        public string Username { get; set; }
        
        /// <summary>
        /// Whether this player is the session owner.
        /// </summary>
        public bool IsOwner { get; set; }
        
        /// <summary>
        /// Whether the player is ready to start.
        /// </summary>
        public bool IsReady { get; set; }
        
        /// <summary>
        /// Player's customization color.
        /// </summary>
        public UnityEngine.Color PlayerColor { get; set; }
        
        /// <summary>
        /// Creates a new session player data instance.
        /// </summary>
        public SessionPlayerData()
        {
            Username = "Player";
            IsOwner = false;
            IsReady = false;
            PlayerColor = UnityEngine.Color.white;
        }
    }

    /// <summary>
    /// Configuration for creating a new session.
    /// </summary>
    [Serializable]
    public class SessionConfiguration
    {
        /// <summary>
        /// Display name for the session.
        /// </summary>
        public string SessionName { get; set; } = "My Session";
        
        /// <summary>
        /// Maximum number of players (2-8).
        /// </summary>
        public int MaxPlayers { get; set; } = 4;
        
        /// <summary>
        /// Number of waves (-1 for infinite).
        /// </summary>
        public int WaveCount { get; set; } = 10;
        
        /// <summary>
        /// Whether the session is public (visible in browser).
        /// </summary>
        public bool IsPublic { get; set; } = true;
        
        /// <summary>
        /// Validates the configuration.
        /// </summary>
        /// <returns>True if valid, false otherwise.</returns>
        public bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(SessionName)) return false;
            if (SessionName.Length < 3 || SessionName.Length > 30) return false;
            if (MaxPlayers < 2 || MaxPlayers > 8) return false;
            if (WaveCount < -1 || WaveCount == 0) return false;
            
            return true;
        }
    }
}