using System;
using UnityEngine;

namespace TheColosseumChallenge.Settings
{
    /// <summary>
    /// Centralized manager for all game settings.
    /// Handles saving, loading, and applying settings.
    /// </summary>
    public class SettingsManager : MonoBehaviour
    {
        #region Singleton Pattern
        
        private static SettingsManager _instance;
        
        public static SettingsManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<SettingsManager>();
                    
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("SettingsManager");
                        _instance = go.AddComponent<SettingsManager>();
                    }
                }
                return _instance;
            }
        }
        
        #endregion

        #region PlayerPrefs Keys
        
        private static class Keys
        {
            // Audio
            public const string MasterVolume = "Settings_MasterVolume";
            public const string MusicVolume = "Settings_MusicVolume";
            public const string SFXVolume = "Settings_SFXVolume";
            public const string MuteAll = "Settings_MuteAll";
            
            // Controls
            public const string MouseSensitivity = "Settings_MouseSensitivity";
            public const string InvertY = "Settings_InvertY";
            public const string ToggleCrouch = "Settings_ToggleCrouch";
            public const string ToggleSprint = "Settings_ToggleSprint";
            
            // Graphics
            public const string QualityLevel = "Settings_QualityLevel";
            public const string AntiAliasing = "Settings_AntiAliasing";
            public const string ShadowQuality = "Settings_ShadowQuality";
            public const string TextureQuality = "Settings_TextureQuality";
            public const string VSync = "Settings_VSync";
            public const string RenderScale = "Settings_RenderScale";
            
            // Display
            public const string ResolutionWidth = "Settings_ResWidth";
            public const string ResolutionHeight = "Settings_ResHeight";
            public const string ScreenMode = "Settings_ScreenMode";
            public const string Brightness = "Settings_Brightness";
            public const string FOV = "Settings_FOV";
            public const string ShowFPS = "Settings_ShowFPS";
        }
        
        #endregion

        #region Default Values
        
        private static class Defaults
        {
            // Audio
            public const float MasterVolume = 1f;
            public const float MusicVolume = 0.8f;
            public const float SFXVolume = 1f;
            public const bool MuteAll = false;
            
            // Controls
            public const float MouseSensitivity = 1f;
            public const bool InvertY = false;
            public const bool ToggleCrouch = false;
            public const bool ToggleSprint = false;
            
            // Graphics
            public const int QualityLevel = 3;
            public const int AntiAliasing = 2;
            public const int ShadowQuality = 2;
            public const int TextureQuality = 0;
            public const bool VSync = true;
            public const float RenderScale = 1f;
            
            // Display
            public const float Brightness = 1f;
            public const float FOV = 90f;
            public const bool ShowFPS = false;
        }
        
        #endregion

        #region Events
        
        public event Action OnSettingsChanged;
        public event Action<float> OnMasterVolumeChanged;
        public event Action<float> OnMusicVolumeChanged;
        public event Action<float> OnSFXVolumeChanged;
        public event Action<float> OnMouseSensitivityChanged;
        public event Action<float> OnFOVChanged;
        
        #endregion

        #region Properties - Audio
        
        public float MasterVolume { get; private set; }
        public float MusicVolume { get; private set; }
        public float SFXVolume { get; private set; }
        public bool MuteAll { get; private set; }
        
        #endregion

        #region Properties - Controls
        
        public float MouseSensitivity { get; private set; }
        public bool InvertY { get; private set; }
        public bool ToggleCrouch { get; private set; }
        public bool ToggleSprint { get; private set; }
        
        #endregion

        #region Properties - Graphics
        
        public int QualityLevel { get; private set; }
        public int AntiAliasing { get; private set; }
        public int ShadowQuality { get; private set; }
        public int TextureQuality { get; private set; }
        public bool VSync { get; private set; }
        public float RenderScale { get; private set; }
        
        #endregion

        #region Properties - Display
        
        public Resolution CurrentResolution { get; private set; }
        public FullScreenMode ScreenMode { get; private set; }
        public float Brightness { get; private set; }
        public float FOV { get; private set; }
        public bool ShowFPS { get; private set; }
        
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
            
            LoadSettings();
            ApplySettings();
            
            Debug.Log("[SettingsManager] Initialized and settings loaded.");
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
        
        #endregion

        #region Audio Settings
        
        public void SetMasterVolume(float value)
        {
            MasterVolume = Mathf.Clamp01(value);
            AudioListener.volume = MuteAll ? 0 : MasterVolume;
            OnMasterVolumeChanged?.Invoke(MasterVolume);
            OnSettingsChanged?.Invoke();
        }
        
        public void SetMusicVolume(float value)
        {
            MusicVolume = Mathf.Clamp01(value);
            OnMusicVolumeChanged?.Invoke(MusicVolume);
            OnSettingsChanged?.Invoke();
        }
        
        public void SetSFXVolume(float value)
        {
            SFXVolume = Mathf.Clamp01(value);
            OnSFXVolumeChanged?.Invoke(SFXVolume);
            OnSettingsChanged?.Invoke();
        }
        
        public void SetMuteAll(bool value)
        {
            MuteAll = value;
            AudioListener.volume = value ? 0 : MasterVolume;
            OnSettingsChanged?.Invoke();
        }
        
        #endregion

        #region Controls Settings
        
        public void SetMouseSensitivity(float value)
        {
            MouseSensitivity = Mathf.Clamp(value, 0.1f, 5f);
            OnMouseSensitivityChanged?.Invoke(MouseSensitivity);
            OnSettingsChanged?.Invoke();
        }
        
        public void SetInvertY(bool value)
        {
            InvertY = value;
            OnSettingsChanged?.Invoke();
        }
        
        public void SetToggleCrouch(bool value)
        {
            ToggleCrouch = value;
            OnSettingsChanged?.Invoke();
        }
        
        public void SetToggleSprint(bool value)
        {
            ToggleSprint = value;
            OnSettingsChanged?.Invoke();
        }
        
        public void ResetControlsToDefault()
        {
            MouseSensitivity = Defaults.MouseSensitivity;
            InvertY = Defaults.InvertY;
            ToggleCrouch = Defaults.ToggleCrouch;
            ToggleSprint = Defaults.ToggleSprint;
            
            OnMouseSensitivityChanged?.Invoke(MouseSensitivity);
            OnSettingsChanged?.Invoke();
        }
        
        #endregion

        #region Graphics Settings
        
        public void SetQualityLevel(int level)
        {
            QualityLevel = Mathf.Clamp(level, 0, QualitySettings.names.Length - 1);
            QualitySettings.SetQualityLevel(QualityLevel);
            OnSettingsChanged?.Invoke();
        }
        
        public void SetAntiAliasing(int value)
        {
            AntiAliasing = value;
            QualitySettings.antiAliasing = value;
            OnSettingsChanged?.Invoke();
        }
        
        public void SetShadowQuality(int value)
        {
            ShadowQuality = value;
            
            switch (value)
            {
                /*case 0:
                    QualitySettings.shadows = ShadowQuality2.Disable;
                    break;
                case 1:
                    QualitySettings.shadows = ShadowQuality2.HardOnly;
                    QualitySettings.shadowResolution = ShadowResolution.Low;
                    break;
                case 2:
                    QualitySettings.shadows = ShadowQuality2.All;
                    QualitySettings.shadowResolution = ShadowResolution.Medium;
                    break;
                case 3:
                    QualitySettings.shadows = ShadowQuality2.All;
                    QualitySettings.shadowResolution = ShadowResolution.High;
                    break;
                case 4:
                    QualitySettings.shadows = ShadowQuality2.All;
                    QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
                    break;*/
            }
            
            OnSettingsChanged?.Invoke();
        }
        
        public void SetTextureQuality(int value)
        {
            TextureQuality = value;
            // 0 = Full res, 1 = Half, 2 = Quarter, 3 = Eighth
            QualitySettings.globalTextureMipmapLimit = 3 - Mathf.Clamp(value, 0, 3);
            OnSettingsChanged?.Invoke();
        }
        
        public void SetVSync(bool value)
        {
            VSync = value;
            QualitySettings.vSyncCount = value ? 1 : 0;
            OnSettingsChanged?.Invoke();
        }
        
        public void SetRenderScale(float value)
        {
            RenderScale = Mathf.Clamp(value, 0.5f, 2f);
            // Apply to render pipeline if using URP/HDRP
            OnSettingsChanged?.Invoke();
        }
        
        #endregion

        #region Display Settings
        
        public void SetResolution(Resolution resolution)
        {
            CurrentResolution = resolution;
            Screen.SetResolution(resolution.width, resolution.height, ScreenMode);
            OnSettingsChanged?.Invoke();
        }
        
        public void SetScreenMode(FullScreenMode mode)
        {
            ScreenMode = mode;
            Screen.fullScreenMode = mode;
            OnSettingsChanged?.Invoke();
        }
        
        public void SetBrightness(float value)
        {
            Brightness = Mathf.Clamp(value, 0f, 2f);
            // Apply brightness through post-processing or shader
            OnSettingsChanged?.Invoke();
        }
        
        public void SetFOV(float value)
        {
            FOV = Mathf.Clamp(value, 60f, 120f);
            OnFOVChanged?.Invoke(FOV);
            OnSettingsChanged?.Invoke();
        }
        
        public void SetShowFPS(bool value)
        {
            ShowFPS = value;
            OnSettingsChanged?.Invoke();
        }
        
        #endregion

        #region Save/Load
        
        public void SaveSettings()
        {
            // Audio
            PlayerPrefs.SetFloat(Keys.MasterVolume, MasterVolume);
            PlayerPrefs.SetFloat(Keys.MusicVolume, MusicVolume);
            PlayerPrefs.SetFloat(Keys.SFXVolume, SFXVolume);
            PlayerPrefs.SetInt(Keys.MuteAll, MuteAll ? 1 : 0);
            
            // Controls
            PlayerPrefs.SetFloat(Keys.MouseSensitivity, MouseSensitivity);
            PlayerPrefs.SetInt(Keys.InvertY, InvertY ? 1 : 0);
            PlayerPrefs.SetInt(Keys.ToggleCrouch, ToggleCrouch ? 1 : 0);
            PlayerPrefs.SetInt(Keys.ToggleSprint, ToggleSprint ? 1 : 0);
            
            // Graphics
            PlayerPrefs.SetInt(Keys.QualityLevel, QualityLevel);
            PlayerPrefs.SetInt(Keys.AntiAliasing, AntiAliasing);
            PlayerPrefs.SetInt(Keys.ShadowQuality, ShadowQuality);
            PlayerPrefs.SetInt(Keys.TextureQuality, TextureQuality);
            PlayerPrefs.SetInt(Keys.VSync, VSync ? 1 : 0);
            PlayerPrefs.SetFloat(Keys.RenderScale, RenderScale);
            
            // Display
            PlayerPrefs.SetInt(Keys.ResolutionWidth, CurrentResolution.width);
            PlayerPrefs.SetInt(Keys.ResolutionHeight, CurrentResolution.height);
            PlayerPrefs.SetInt(Keys.ScreenMode, (int)ScreenMode);
            PlayerPrefs.SetFloat(Keys.Brightness, Brightness);
            PlayerPrefs.SetFloat(Keys.FOV, FOV);
            PlayerPrefs.SetInt(Keys.ShowFPS, ShowFPS ? 1 : 0);
            
            PlayerPrefs.Save();
            
            Debug.Log("[SettingsManager] Settings saved.");
        }
        
        public void LoadSettings()
        {
            // Audio
            MasterVolume = PlayerPrefs.GetFloat(Keys.MasterVolume, Defaults.MasterVolume);
            MusicVolume = PlayerPrefs.GetFloat(Keys.MusicVolume, Defaults.MusicVolume);
            SFXVolume = PlayerPrefs.GetFloat(Keys.SFXVolume, Defaults.SFXVolume);
            MuteAll = PlayerPrefs.GetInt(Keys.MuteAll, Defaults.MuteAll ? 1 : 0) == 1;
            
            // Controls
            MouseSensitivity = PlayerPrefs.GetFloat(Keys.MouseSensitivity, Defaults.MouseSensitivity);
            InvertY = PlayerPrefs.GetInt(Keys.InvertY, Defaults.InvertY ? 1 : 0) == 1;
            ToggleCrouch = PlayerPrefs.GetInt(Keys.ToggleCrouch, Defaults.ToggleCrouch ? 1 : 0) == 1;
            ToggleSprint = PlayerPrefs.GetInt(Keys.ToggleSprint, Defaults.ToggleSprint ? 1 : 0) == 1;
            
            // Graphics
            QualityLevel = PlayerPrefs.GetInt(Keys.QualityLevel, Defaults.QualityLevel);
            AntiAliasing = PlayerPrefs.GetInt(Keys.AntiAliasing, Defaults.AntiAliasing);
            ShadowQuality = PlayerPrefs.GetInt(Keys.ShadowQuality, Defaults.ShadowQuality);
            TextureQuality = PlayerPrefs.GetInt(Keys.TextureQuality, Defaults.TextureQuality);
            VSync = PlayerPrefs.GetInt(Keys.VSync, Defaults.VSync ? 1 : 0) == 1;
            RenderScale = PlayerPrefs.GetFloat(Keys.RenderScale, Defaults.RenderScale);
            
            // Display
            int resW = PlayerPrefs.GetInt(Keys.ResolutionWidth, Screen.currentResolution.width);
            int resH = PlayerPrefs.GetInt(Keys.ResolutionHeight, Screen.currentResolution.height);
            CurrentResolution = new Resolution { width = resW, height = resH };
            ScreenMode = (FullScreenMode)PlayerPrefs.GetInt(Keys.ScreenMode, (int)FullScreenMode.FullScreenWindow);
            Brightness = PlayerPrefs.GetFloat(Keys.Brightness, Defaults.Brightness);
            FOV = PlayerPrefs.GetFloat(Keys.FOV, Defaults.FOV);
            ShowFPS = PlayerPrefs.GetInt(Keys.ShowFPS, Defaults.ShowFPS ? 1 : 0) == 1;
            
            Debug.Log("[SettingsManager] Settings loaded.");
        }
        
        public void ApplySettings()
        {
            // Apply audio
            AudioListener.volume = MuteAll ? 0 : MasterVolume;
            
            // Apply graphics
            QualitySettings.SetQualityLevel(QualityLevel);
            QualitySettings.antiAliasing = AntiAliasing;
            QualitySettings.vSyncCount = VSync ? 1 : 0;
            
            // Apply display
            Screen.SetResolution(CurrentResolution.width, CurrentResolution.height, ScreenMode);
            
            Debug.Log("[SettingsManager] Settings applied.");
        }
        
        public void ResetAllToDefault()
        {
            // Audio
            MasterVolume = Defaults.MasterVolume;
            MusicVolume = Defaults.MusicVolume;
            SFXVolume = Defaults.SFXVolume;
            MuteAll = Defaults.MuteAll;
            
            // Controls
            MouseSensitivity = Defaults.MouseSensitivity;
            InvertY = Defaults.InvertY;
            ToggleCrouch = Defaults.ToggleCrouch;
            ToggleSprint = Defaults.ToggleSprint;
            
            // Graphics
            QualityLevel = Defaults.QualityLevel;
            AntiAliasing = Defaults.AntiAliasing;
            ShadowQuality = Defaults.ShadowQuality;
            TextureQuality = Defaults.TextureQuality;
            VSync = Defaults.VSync;
            RenderScale = Defaults.RenderScale;
            
            // Display
            CurrentResolution = Screen.currentResolution;
            ScreenMode = FullScreenMode.FullScreenWindow;
            Brightness = Defaults.Brightness;
            FOV = Defaults.FOV;
            ShowFPS = Defaults.ShowFPS;
            
            ApplySettings();
            OnSettingsChanged?.Invoke();
            
            Debug.Log("[SettingsManager] All settings reset to defaults.");
        }
        
        #endregion
    }
}