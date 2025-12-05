using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TheColosseumChallenge.Settings;

namespace TheColosseumChallenge.UI.Panels
{
    /// <summary>
    /// Defines the different settings tabs.
    /// </summary>
    public enum SettingsTab
    {
        Audio,
        Controls,
        Graphics,
        Display
    }

    /// <summary>
    /// Main settings panel with tabbed navigation for different settings categories.
    /// </summary>
    public class SettingsPanel : BasePanel
    {
        #region Inspector References
        
        [Header("Tab Buttons")]
        [SerializeField] private Button _audioTabButton;
        [SerializeField] private Button _controlsTabButton;
        [SerializeField] private Button _graphicsTabButton;
        [SerializeField] private Button _displayTabButton;
        
        [Header("Tab Panels")]
        [SerializeField] private GameObject _audioPanel;
        [SerializeField] private GameObject _controlsPanel;
        [SerializeField] private GameObject _graphicsPanel;
        [SerializeField] private GameObject _displayPanel;
        
        [Header("Audio Settings")]
        [SerializeField] private Slider _masterVolumeSlider;
        [SerializeField] private Slider _musicVolumeSlider;
        [SerializeField] private Slider _sfxVolumeSlider;
        [SerializeField] private TextMeshProUGUI _masterVolumeValue;
        [SerializeField] private TextMeshProUGUI _musicVolumeValue;
        [SerializeField] private TextMeshProUGUI _sfxVolumeValue;
        [SerializeField] private Toggle _muteAllToggle;
        
        [Header("Controls Settings")]
        [SerializeField] private Slider _mouseSensitivitySlider;
        [SerializeField] private TextMeshProUGUI _mouseSensitivityValue;
        [SerializeField] private Toggle _invertYAxisToggle;
        [SerializeField] private Toggle _toggleCrouchToggle;
        [SerializeField] private Toggle _toggleSprintToggle;
        [SerializeField] private Button _resetControlsButton;
        
        [Header("Graphics Settings")]
        [SerializeField] private TMP_Dropdown _qualityDropdown;
        [SerializeField] private TMP_Dropdown _antiAliasingDropdown;
        [SerializeField] private TMP_Dropdown _shadowQualityDropdown;
        [SerializeField] private TMP_Dropdown _textureQualityDropdown;
        [SerializeField] private Toggle _vsyncToggle;
        [SerializeField] private Slider _renderScaleSlider;
        [SerializeField] private TextMeshProUGUI _renderScaleValue;
        
        [Header("Display Settings")]
        [SerializeField] private TMP_Dropdown _resolutionDropdown;
        [SerializeField] private TMP_Dropdown _screenModeDropdown;
        [SerializeField] private Slider _brightnessSlider;
        [SerializeField] private TextMeshProUGUI _brightnessValue;
        [SerializeField] private Slider _fovSlider;
        [SerializeField] private TextMeshProUGUI _fovValue;
        [SerializeField] private Toggle _showFpsToggle;
        
        [Header("Bottom Buttons")]
        [SerializeField] private Button _applyButton;
        [SerializeField] private Button _resetAllButton;
        [SerializeField] private Button _backButton;
        
        [Header("Tab Visual Settings")]
        [SerializeField] private Color _activeTabColor = Color.white;
        [SerializeField] private Color _inactiveTabColor = new Color(0.7f, 0.7f, 0.7f);
        
        #endregion

        #region Private Fields
        
        private SettingsTab _currentTab = SettingsTab.Audio;
        private Resolution[] _resolutions;
        private bool _hasUnsavedChanges;
        
        #endregion

        #region Initialization
        
        protected override void OnInitialize()
        {
            base.OnInitialize();
            
            SetupTabButtons();
            SetupAudioSettings();
            SetupControlsSettings();
            SetupGraphicsSettings();
            SetupDisplaySettings();
            SetupBottomButtons();
            
            // Initialize resolutions
            _resolutions = Screen.resolutions;
            PopulateResolutionDropdown();
            
            Debug.Log("[SettingsPanel] Initialized.");
        }
        
        /// <summary>
        /// Sets up tab button listeners.
        /// </summary>
        private void SetupTabButtons()
        {
            if (_audioTabButton != null)
                _audioTabButton.onClick.AddListener(() => SwitchTab(SettingsTab.Audio));
            if (_controlsTabButton != null)
                _controlsTabButton.onClick.AddListener(() => SwitchTab(SettingsTab.Controls));
            if (_graphicsTabButton != null)
                _graphicsTabButton.onClick.AddListener(() => SwitchTab(SettingsTab.Graphics));
            if (_displayTabButton != null)
                _displayTabButton.onClick.AddListener(() => SwitchTab(SettingsTab.Display));
        }
        
        /// <summary>
        /// Sets up audio settings controls.
        /// </summary>
        private void SetupAudioSettings()
        {
            if (_masterVolumeSlider != null)
            {
                _masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            }
            if (_musicVolumeSlider != null)
            {
                _musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            }
            if (_sfxVolumeSlider != null)
            {
                _sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            }
            if (_muteAllToggle != null)
            {
                _muteAllToggle.onValueChanged.AddListener(OnMuteAllChanged);
            }
        }
        
        /// <summary>
        /// Sets up controls settings controls.
        /// </summary>
        private void SetupControlsSettings()
        {
            if (_mouseSensitivitySlider != null)
            {
                _mouseSensitivitySlider.minValue = 0.1f;
                _mouseSensitivitySlider.maxValue = 5f;
                _mouseSensitivitySlider.onValueChanged.AddListener(OnMouseSensitivityChanged);
            }
            if (_invertYAxisToggle != null)
            {
                _invertYAxisToggle.onValueChanged.AddListener(OnInvertYChanged);
            }
            if (_toggleCrouchToggle != null)
            {
                _toggleCrouchToggle.onValueChanged.AddListener(OnToggleCrouchChanged);
            }
            if (_toggleSprintToggle != null)
            {
                _toggleSprintToggle.onValueChanged.AddListener(OnToggleSprintChanged);
            }
            if (_resetControlsButton != null)
            {
                _resetControlsButton.onClick.AddListener(OnResetControlsClicked);
            }
        }
        
        /// <summary>
        /// Sets up graphics settings controls.
        /// </summary>
        private void SetupGraphicsSettings()
        {
            if (_qualityDropdown != null)
            {
                _qualityDropdown.ClearOptions();
                _qualityDropdown.AddOptions(new System.Collections.Generic.List<string>(QualitySettings.names));
                _qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
            }
            if (_antiAliasingDropdown != null)
            {
                _antiAliasingDropdown.ClearOptions();
                _antiAliasingDropdown.AddOptions(new System.Collections.Generic.List<string> 
                    { "Disabled", "2x", "4x", "8x" });
                _antiAliasingDropdown.onValueChanged.AddListener(OnAntiAliasingChanged);
            }
            if (_shadowQualityDropdown != null)
            {
                _shadowQualityDropdown.ClearOptions();
                _shadowQualityDropdown.AddOptions(new System.Collections.Generic.List<string> 
                    { "Disabled", "Low", "Medium", "High", "Very High" });
                _shadowQualityDropdown.onValueChanged.AddListener(OnShadowQualityChanged);
            }
            if (_textureQualityDropdown != null)
            {
                _textureQualityDropdown.ClearOptions();
                _textureQualityDropdown.AddOptions(new System.Collections.Generic.List<string> 
                    { "Low", "Medium", "High", "Ultra" });
                _textureQualityDropdown.onValueChanged.AddListener(OnTextureQualityChanged);
            }
            if (_vsyncToggle != null)
            {
                _vsyncToggle.onValueChanged.AddListener(OnVSyncChanged);
            }
            if (_renderScaleSlider != null)
            {
                _renderScaleSlider.minValue = 0.5f;
                _renderScaleSlider.maxValue = 2f;
                _renderScaleSlider.onValueChanged.AddListener(OnRenderScaleChanged);
            }
        }
        
        /// <summary>
        /// Sets up display settings controls.
        /// </summary>
        private void SetupDisplaySettings()
        {
            if (_screenModeDropdown != null)
            {
                _screenModeDropdown.ClearOptions();
                _screenModeDropdown.AddOptions(new System.Collections.Generic.List<string> 
                    { "Fullscreen", "Borderless Window", "Windowed" });
                _screenModeDropdown.onValueChanged.AddListener(OnScreenModeChanged);
            }
            if (_brightnessSlider != null)
            {
                _brightnessSlider.minValue = 0f;
                _brightnessSlider.maxValue = 2f;
                _brightnessSlider.onValueChanged.AddListener(OnBrightnessChanged);
            }
            if (_fovSlider != null)
            {
                _fovSlider.minValue = 60f;
                _fovSlider.maxValue = 120f;
                _fovSlider.onValueChanged.AddListener(OnFOVChanged);
            }
            if (_showFpsToggle != null)
            {
                _showFpsToggle.onValueChanged.AddListener(OnShowFPSChanged);
            }
        }
        
        /// <summary>
        /// Sets up bottom button listeners.
        /// </summary>
        private void SetupBottomButtons()
        {
            if (_applyButton != null)
                _applyButton.onClick.AddListener(OnApplyClicked);
            if (_resetAllButton != null)
                _resetAllButton.onClick.AddListener(OnResetAllClicked);
            if (_backButton != null)
                _backButton.onClick.AddListener(OnBackClicked);
        }
        
        /// <summary>
        /// Populates the resolution dropdown with available resolutions.
        /// </summary>
        private void PopulateResolutionDropdown()
        {
            if (_resolutionDropdown == null) return;
            
            _resolutionDropdown.ClearOptions();
            
            var options = new System.Collections.Generic.List<string>();
            int currentResIndex = 0;
            
            for (int i = 0; i < _resolutions.Length; i++)
            {
                string option = $"{_resolutions[i].width} x {_resolutions[i].height} @ {_resolutions[i].refreshRateRatio}Hz";
                options.Add(option);
                
                if (_resolutions[i].width == Screen.currentResolution.width &&
                    _resolutions[i].height == Screen.currentResolution.height)
                {
                    currentResIndex = i;
                }
            }
            
            _resolutionDropdown.AddOptions(options);
            _resolutionDropdown.value = currentResIndex;
            _resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        }
        
        #endregion

        #region Panel Lifecycle
        
        protected override void OnBeforeShow()
        {
            base.OnBeforeShow();
            
            LoadCurrentSettings();
            SwitchTab(SettingsTab.Audio);
            _hasUnsavedChanges = false;
        }
        
        #endregion

        #region Tab Navigation
        
        /// <summary>
        /// Switches to the specified settings tab.
        /// </summary>
        /// <param name="tab">The tab to switch to.</param>
        public void SwitchTab(SettingsTab tab)
        {
            _currentTab = tab;
            
            // Hide all panels
            if (_audioPanel != null) _audioPanel.SetActive(false);
            if (_controlsPanel != null) _controlsPanel.SetActive(false);
            if (_graphicsPanel != null) _graphicsPanel.SetActive(false);
            if (_displayPanel != null) _displayPanel.SetActive(false);
            
            // Show selected panel
            switch (tab)
            {
                case SettingsTab.Audio:
                    if (_audioPanel != null) _audioPanel.SetActive(true);
                    break;
                case SettingsTab.Controls:
                    if (_controlsPanel != null) _controlsPanel.SetActive(true);
                    break;
                case SettingsTab.Graphics:
                    if (_graphicsPanel != null) _graphicsPanel.SetActive(true);
                    break;
                case SettingsTab.Display:
                    if (_displayPanel != null) _displayPanel.SetActive(true);
                    break;
            }
            
            UpdateTabVisuals();
        }
        
        /// <summary>
        /// Updates the visual state of tab buttons.
        /// </summary>
        private void UpdateTabVisuals()
        {
            UpdateTabButtonColor(_audioTabButton, _currentTab == SettingsTab.Audio);
            UpdateTabButtonColor(_controlsTabButton, _currentTab == SettingsTab.Controls);
            UpdateTabButtonColor(_graphicsTabButton, _currentTab == SettingsTab.Graphics);
            UpdateTabButtonColor(_displayTabButton, _currentTab == SettingsTab.Display);
        }
        
        /// <summary>
        /// Updates a tab button's color based on active state.
        /// </summary>
        private void UpdateTabButtonColor(Button button, bool isActive)
        {
            if (button == null) return;
            
            var colors = button.colors;
            colors.normalColor = isActive ? _activeTabColor : _inactiveTabColor;
            button.colors = colors;
        }
        
        #endregion

        #region Settings Value Handlers
        
        // Audio handlers
        private void OnMasterVolumeChanged(float value)
        {
            _hasUnsavedChanges = true;
            if (_masterVolumeValue != null) _masterVolumeValue.text = $"{Mathf.RoundToInt(value * 100)}%";
            SettingsManager.Instance?.SetMasterVolume(value);
        }
        
        private void OnMusicVolumeChanged(float value)
        {
            _hasUnsavedChanges = true;
            if (_musicVolumeValue != null) _musicVolumeValue.text = $"{Mathf.RoundToInt(value * 100)}%";
            SettingsManager.Instance?.SetMusicVolume(value);
        }
        
        private void OnSFXVolumeChanged(float value)
        {
            _hasUnsavedChanges = true;
            if (_sfxVolumeValue != null) _sfxVolumeValue.text = $"{Mathf.RoundToInt(value * 100)}%";
            SettingsManager.Instance?.SetSFXVolume(value);
        }
        
        private void OnMuteAllChanged(bool value)
        {
            _hasUnsavedChanges = true;
            SettingsManager.Instance?.SetMuteAll(value);
        }
        
        // Controls handlers
        private void OnMouseSensitivityChanged(float value)
        {
            _hasUnsavedChanges = true;
            if (_mouseSensitivityValue != null) _mouseSensitivityValue.text = value.ToString("F1");
            SettingsManager.Instance?.SetMouseSensitivity(value);
        }
        
        private void OnInvertYChanged(bool value)
        {
            _hasUnsavedChanges = true;
            SettingsManager.Instance?.SetInvertY(value);
        }
        
        private void OnToggleCrouchChanged(bool value)
        {
            _hasUnsavedChanges = true;
            SettingsManager.Instance?.SetToggleCrouch(value);
        }
        
        private void OnToggleSprintChanged(bool value)
        {
            _hasUnsavedChanges = true;
            SettingsManager.Instance?.SetToggleSprint(value);
        }
        
        // Graphics handlers
        private void OnQualityChanged(int value)
        {
            _hasUnsavedChanges = true;
            SettingsManager.Instance?.SetQualityLevel(value);
        }
        
        private void OnAntiAliasingChanged(int value)
        {
            _hasUnsavedChanges = true;
            int[] aaValues = { 0, 2, 4, 8 };
            SettingsManager.Instance?.SetAntiAliasing(aaValues[value]);
        }
        
        private void OnShadowQualityChanged(int value)
        {
            _hasUnsavedChanges = true;
            SettingsManager.Instance?.SetShadowQuality(value);
        }
        
        private void OnTextureQualityChanged(int value)
        {
            _hasUnsavedChanges = true;
            SettingsManager.Instance?.SetTextureQuality(value);
        }
        
        private void OnVSyncChanged(bool value)
        {
            _hasUnsavedChanges = true;
            SettingsManager.Instance?.SetVSync(value);
        }
        
        private void OnRenderScaleChanged(float value)
        {
            _hasUnsavedChanges = true;
            if (_renderScaleValue != null) _renderScaleValue.text = $"{Mathf.RoundToInt(value * 100)}%";
            SettingsManager.Instance?.SetRenderScale(value);
        }
        
        // Display handlers
        private void OnResolutionChanged(int value)
        {
            _hasUnsavedChanges = true;
            if (value >= 0 && value < _resolutions.Length)
            {
                SettingsManager.Instance?.SetResolution(_resolutions[value]);
            }
        }
        
        private void OnScreenModeChanged(int value)
        {
            _hasUnsavedChanges = true;
            FullScreenMode mode = value switch
            {
                0 => FullScreenMode.ExclusiveFullScreen,
                1 => FullScreenMode.FullScreenWindow,
                2 => FullScreenMode.Windowed,
                _ => FullScreenMode.FullScreenWindow
            };
            SettingsManager.Instance?.SetScreenMode(mode);
        }
        
        private void OnBrightnessChanged(float value)
        {
            _hasUnsavedChanges = true;
            if (_brightnessValue != null) _brightnessValue.text = value.ToString("F1");
            SettingsManager.Instance?.SetBrightness(value);
        }
        
        private void OnFOVChanged(float value)
        {
            _hasUnsavedChanges = true;
            if (_fovValue != null) _fovValue.text = $"{Mathf.RoundToInt(value)}°";
            SettingsManager.Instance?.SetFOV(value);
        }
        
        private void OnShowFPSChanged(bool value)
        {
            _hasUnsavedChanges = true;
            SettingsManager.Instance?.SetShowFPS(value);
        }
        
        #endregion

        #region Button Handlers
        
        private void OnResetControlsClicked()
        {
            SettingsManager.Instance?.ResetControlsToDefault();
            LoadCurrentSettings();
        }
        
        private void OnApplyClicked()
        {
            SettingsManager.Instance?.SaveSettings();
            _hasUnsavedChanges = false;
            Debug.Log("[SettingsPanel] Settings applied.");
        }
        
        private void OnResetAllClicked()
        {
            SettingsManager.Instance?.ResetAllToDefault();
            LoadCurrentSettings();
            _hasUnsavedChanges = true;
        }
        
        private void OnBackClicked()
        {
            if (_hasUnsavedChanges)
            {
                // Could show confirmation dialog here
                SettingsManager.Instance?.SaveSettings();
            }
            
            NavigateBack();
        }
        
        #endregion

        #region Settings Loading
        
        /// <summary>
        /// Loads current settings into UI controls.
        /// </summary>
        private void LoadCurrentSettings()
        {
            if (SettingsManager.Instance == null) return;
            
            var settings = SettingsManager.Instance;
            
            // Audio
            if (_masterVolumeSlider != null) 
            {
                _masterVolumeSlider.SetValueWithoutNotify(settings.MasterVolume);
                OnMasterVolumeChanged(settings.MasterVolume);
            }
            if (_musicVolumeSlider != null) 
            {
                _musicVolumeSlider.SetValueWithoutNotify(settings.MusicVolume);
                OnMusicVolumeChanged(settings.MusicVolume);
            }
            if (_sfxVolumeSlider != null) 
            {
                _sfxVolumeSlider.SetValueWithoutNotify(settings.SFXVolume);
                OnSFXVolumeChanged(settings.SFXVolume);
            }
            if (_muteAllToggle != null) _muteAllToggle.SetIsOnWithoutNotify(settings.MuteAll);
            
            // Controls
            if (_mouseSensitivitySlider != null) 
            {
                _mouseSensitivitySlider.SetValueWithoutNotify(settings.MouseSensitivity);
                OnMouseSensitivityChanged(settings.MouseSensitivity);
            }
            if (_invertYAxisToggle != null) _invertYAxisToggle.SetIsOnWithoutNotify(settings.InvertY);
            if (_toggleCrouchToggle != null) _toggleCrouchToggle.SetIsOnWithoutNotify(settings.ToggleCrouch);
            if (_toggleSprintToggle != null) _toggleSprintToggle.SetIsOnWithoutNotify(settings.ToggleSprint);
            
            // Graphics
            if (_qualityDropdown != null) _qualityDropdown.SetValueWithoutNotify(settings.QualityLevel);
            if (_vsyncToggle != null) _vsyncToggle.SetIsOnWithoutNotify(settings.VSync);
            if (_renderScaleSlider != null) 
            {
                _renderScaleSlider.SetValueWithoutNotify(settings.RenderScale);
                OnRenderScaleChanged(settings.RenderScale);
            }
            
            // Display
            if (_brightnessSlider != null) 
            {
                _brightnessSlider.SetValueWithoutNotify(settings.Brightness);
                OnBrightnessChanged(settings.Brightness);
            }
            if (_fovSlider != null) 
            {
                _fovSlider.SetValueWithoutNotify(settings.FOV);
                OnFOVChanged(settings.FOV);
            }
            if (_showFpsToggle != null) _showFpsToggle.SetIsOnWithoutNotify(settings.ShowFPS);
        }
        
        #endregion
    }
}