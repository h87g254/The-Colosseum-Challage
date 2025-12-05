using System;
using UnityEngine;

namespace TheColosseumChallenge.Data
{
    /// <summary>
    /// Data structure representing player customization state.
    /// </summary>
    [Serializable]
    public class PlayerCustomizationData
    {
        public Color playerColor = Color.white;
        public int hatIndex = -1;        // -1 means no hat
        public int glassesIndex = -1;    // -1 means no glasses
        public int accessoryIndex = -1;  // -1 means no accessory
    }

    /// <summary>
    /// Manages persistent player data storage and retrieval.
    /// Uses PlayerPrefs for cross-session persistence.
    /// </summary>
    public class PlayerDataManager : MonoBehaviour
    {
        #region Singleton Pattern
        
        private static PlayerDataManager _instance;
        
        /// <summary>
        /// Global access point for the PlayerDataManager.
        /// </summary>
        public static PlayerDataManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<PlayerDataManager>();
                    
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("PlayerDataManager");
                        _instance = go.AddComponent<PlayerDataManager>();
                    }
                }
                return _instance;
            }
        }
        
        #endregion

        #region PlayerPrefs Keys
        
        /// <summary>
        /// Constants for PlayerPrefs keys to prevent typos.
        /// </summary>
        private static class Keys
        {
            public const string Username = "Player_Username";
            public const string HasUsername = "Player_HasUsername";
            public const string PlayerColorR = "Player_Color_R";
            public const string PlayerColorG = "Player_Color_G";
            public const string PlayerColorB = "Player_Color_B";
            public const string HatIndex = "Player_Hat_Index";
            public const string GlassesIndex = "Player_Glasses_Index";
            public const string AccessoryIndex = "Player_Accessory_Index";
        }
        
        #endregion

        #region Events
        
        /// <summary>
        /// Fired when the username is changed.
        /// </summary>
        public event Action<string> OnUsernameChanged;
        
        /// <summary>
        /// Fired when customization data is changed.
        /// </summary>
        public event Action<PlayerCustomizationData> OnCustomizationChanged;
        
        #endregion

        #region Properties
        
        /// <summary>
        /// The current player's username.
        /// </summary>
        public string Username { get; private set; } = string.Empty;
        
        /// <summary>
        /// Whether a username has been set previously.
        /// </summary>
        public bool HasUsername => !string.IsNullOrEmpty(Username);
        
        /// <summary>
        /// The current customization data.
        /// </summary>
        public PlayerCustomizationData CustomizationData { get; private set; }
        
        #endregion

        #region Customization Options
        
        /// <summary>
        /// Available player colors for randomization.
        /// </summary>
        private readonly Color[] _availableColors = new Color[]
        {
            new Color(0.9f, 0.2f, 0.2f),    // Red
            new Color(0.2f, 0.6f, 0.9f),    // Blue
            new Color(0.2f, 0.8f, 0.3f),    // Green
            new Color(0.9f, 0.8f, 0.2f),    // Yellow
            new Color(0.7f, 0.3f, 0.9f),    // Purple
            new Color(0.9f, 0.5f, 0.2f),    // Orange
            new Color(0.2f, 0.9f, 0.8f),    // Cyan
            new Color(0.9f, 0.4f, 0.7f),    // Pink
            new Color(0.4f, 0.4f, 0.4f),    // Gray
            new Color(1f, 1f, 1f),          // White
        };
        
        /// <summary>
        /// Number of available hat options.
        /// </summary>
        public int AvailableHats => 5;
        
        /// <summary>
        /// Number of available glasses options.
        /// </summary>
        public int AvailableGlasses => 4;
        
        /// <summary>
        /// Number of available accessory options.
        /// </summary>
        public int AvailableAccessories => 6;
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            // Implement singleton pattern
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            LoadPlayerData();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
        
        #endregion

        #region Data Loading
        
        /// <summary>
        /// Loads all player data from PlayerPrefs.
        /// </summary>
        private void LoadPlayerData()
        {
            LoadUsername();
            LoadCustomizationData();
            
            Debug.Log($"[PlayerDataManager] Loaded player data. Username: {Username}");
        }
        
        /// <summary>
        /// Loads the username from PlayerPrefs.
        /// </summary>
        private void LoadUsername()
        {
            if (PlayerPrefs.GetInt(Keys.HasUsername, 0) == 1)
            {
                Username = PlayerPrefs.GetString(Keys.Username, string.Empty);
            }
            else
            {
                Username = string.Empty;
            }
        }
        
        /// <summary>
        /// Loads customization data from PlayerPrefs.
        /// </summary>
        private void LoadCustomizationData()
        {
            CustomizationData = new PlayerCustomizationData
            {
                playerColor = new Color(
                    PlayerPrefs.GetFloat(Keys.PlayerColorR, 1f),
                    PlayerPrefs.GetFloat(Keys.PlayerColorG, 1f),
                    PlayerPrefs.GetFloat(Keys.PlayerColorB, 1f)
                ),
                hatIndex = PlayerPrefs.GetInt(Keys.HatIndex, -1),
                glassesIndex = PlayerPrefs.GetInt(Keys.GlassesIndex, -1),
                accessoryIndex = PlayerPrefs.GetInt(Keys.AccessoryIndex, -1)
            };
        }
        
        #endregion

        #region Username Management
        
        /// <summary>
        /// Sets and saves the player's username.
        /// </summary>
        /// <param name="username">The username to save.</param>
        /// <returns>True if the username was valid and saved.</returns>
        public bool SetUsername(string username)
        {
            // Validate username
            if (string.IsNullOrWhiteSpace(username))
            {
                Debug.LogWarning("[PlayerDataManager] Attempted to set empty username.");
                return false;
            }
            
            // Trim and limit length
            username = username.Trim();
            if (username.Length > 20)
            {
                username = username.Substring(0, 20);
            }
            
            if (username.Length < 2)
            {
                Debug.LogWarning("[PlayerDataManager] Username too short.");
                return false;
            }
            
            // Save to PlayerPrefs
            Username = username;
            PlayerPrefs.SetString(Keys.Username, username);
            PlayerPrefs.SetInt(Keys.HasUsername, 1);
            PlayerPrefs.Save();
            
            Debug.Log($"[PlayerDataManager] Username saved: {username}");
            
            OnUsernameChanged?.Invoke(username);
            return true;
        }
        
        /// <summary>
        /// Clears the saved username.
        /// </summary>
        public void ClearUsername()
        {
            Username = string.Empty;
            PlayerPrefs.DeleteKey(Keys.Username);
            PlayerPrefs.SetInt(Keys.HasUsername, 0);
            PlayerPrefs.Save();
            
            Debug.Log("[PlayerDataManager] Username cleared.");
            
            OnUsernameChanged?.Invoke(string.Empty);
        }
        
        #endregion

        #region Customization Management
        
        /// <summary>
        /// Randomizes all customization options.
        /// </summary>
        public void RandomizeCustomization()
        {
            // Randomize color
            CustomizationData.playerColor = _availableColors[UnityEngine.Random.Range(0, _availableColors.Length)];
            
            // Randomize cosmetics (include -1 as "none" option)
            CustomizationData.hatIndex = UnityEngine.Random.Range(-1, AvailableHats);
            CustomizationData.glassesIndex = UnityEngine.Random.Range(-1, AvailableGlasses);
            CustomizationData.accessoryIndex = UnityEngine.Random.Range(-1, AvailableAccessories);
            
            SaveCustomizationData();
            
            Debug.Log("[PlayerDataManager] Customization randomized.");
            
            OnCustomizationChanged?.Invoke(CustomizationData);
        }
        
        /// <summary>
        /// Sets a specific customization option.
        /// </summary>
        /// <param name="color">The player color.</param>
        /// <param name="hat">Hat index (-1 for none).</param>
        /// <param name="glasses">Glasses index (-1 for none).</param>
        /// <param name="accessory">Accessory index (-1 for none).</param>
        public void SetCustomization(Color color, int hat = -1, int glasses = -1, int accessory = -1)
        {
            CustomizationData.playerColor = color;
            CustomizationData.hatIndex = Mathf.Clamp(hat, -1, AvailableHats - 1);
            CustomizationData.glassesIndex = Mathf.Clamp(glasses, -1, AvailableGlasses - 1);
            CustomizationData.accessoryIndex = Mathf.Clamp(accessory, -1, AvailableAccessories - 1);
            
            SaveCustomizationData();
            
            OnCustomizationChanged?.Invoke(CustomizationData);
        }
        
        /// <summary>
        /// Saves customization data to PlayerPrefs.
        /// </summary>
        private void SaveCustomizationData()
        {
            PlayerPrefs.SetFloat(Keys.PlayerColorR, CustomizationData.playerColor.r);
            PlayerPrefs.SetFloat(Keys.PlayerColorG, CustomizationData.playerColor.g);
            PlayerPrefs.SetFloat(Keys.PlayerColorB, CustomizationData.playerColor.b);
            PlayerPrefs.SetInt(Keys.HatIndex, CustomizationData.hatIndex);
            PlayerPrefs.SetInt(Keys.GlassesIndex, CustomizationData.glassesIndex);
            PlayerPrefs.SetInt(Keys.AccessoryIndex, CustomizationData.accessoryIndex);
            PlayerPrefs.Save();
        }
        
        #endregion

        #region Utility
        
        /// <summary>
        /// Clears all player data.
        /// </summary>
        public void ClearAllData()
        {
            PlayerPrefs.DeleteKey(Keys.Username);
            PlayerPrefs.DeleteKey(Keys.HasUsername);
            PlayerPrefs.DeleteKey(Keys.PlayerColorR);
            PlayerPrefs.DeleteKey(Keys.PlayerColorG);
            PlayerPrefs.DeleteKey(Keys.PlayerColorB);
            PlayerPrefs.DeleteKey(Keys.HatIndex);
            PlayerPrefs.DeleteKey(Keys.GlassesIndex);
            PlayerPrefs.DeleteKey(Keys.AccessoryIndex);
            PlayerPrefs.Save();
            
            Username = string.Empty;
            CustomizationData = new PlayerCustomizationData();
            
            Debug.Log("[PlayerDataManager] All player data cleared.");
        }
        
        #endregion
    }
}