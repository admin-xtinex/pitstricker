using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using PitStriker.Gameplay;
using PitStriker.Settings;
using PitStriker.Networking;

namespace PitStriker.UI
{
    public partial class MenuManager : MonoBehaviour
    {
        public static MenuManager Instance { get; private set; }

        [SerializeField] private GameObject _homePanel;
        [SerializeField] private GameObject _choosePlayersPanel;
        [SerializeField] private GameObject _rulesModal;
        [SerializeField] private GameObject _pauseModal;
        [SerializeField] private GameObject _mapsPanel;
        [SerializeField] private GameObject _settingsPanel;
        [SerializeField] private GameObject _hudRoot;

        [SerializeField] private Button _homePlayButton;
        [SerializeField] private Button _homeMapsButton;
        [SerializeField] private Button _homeRulesButton;
        [SerializeField] private Button _homeSettingsButton;
        [SerializeField] private Button _homeExitButton;

        [SerializeField] private Button _closeMapsButton;
        [SerializeField] private Button _btnSelectMapCoastal;
        [SerializeField] private Button _btnSelectMapTemple;
        [SerializeField] private Button _btnSelectMapQuarry;
        [SerializeField] private Button _btnSelectMapVillage;

        [SerializeField] private Button _closeSettingsButton;
        [SerializeField] private Button _tabAudioBtn;
        [SerializeField] private Button _tabGameplayBtn;
        [SerializeField] private Button _tabDisplayBtn;
        [SerializeField] private Button _tabAccessibilityBtn;
        [SerializeField] private Button _tabOtherBtn;
        [SerializeField] private GameObject _audioSettingsPanel;
        [SerializeField] private GameObject _gameplaySettingsPanel;
        [SerializeField] private GameObject _displaySettingsPanel;
        [SerializeField] private GameObject _accessibilitySettingsPanel;
        [SerializeField] private GameObject _otherSettingsPanel;
        [SerializeField] private Slider _masterVolumeSlider;
        [SerializeField] private Text _masterVolumeText;
        [SerializeField] private Slider _musicVolumeSlider;
        [SerializeField] private Text _musicVolumeText;
        [SerializeField] private Slider _sfxVolumeSlider;
        [SerializeField] private Text _sfxVolumeText;
        [SerializeField] private Button _resetAudioBtn;
        [SerializeField] private Button _hapticsToggleButton;
        [SerializeField] private Text _hapticsToggleText;
        [SerializeField] private Button _btnQualityLow;
        [SerializeField] private Button _btnQualityMed;
        [SerializeField] private Button _btnQualityHigh;
        [SerializeField] private Text _displayInfoText;
        [SerializeField] private Button _btnPrivacyPolicy;
        [SerializeField] private Button _btnTermsOfService;
        [SerializeField] private Button _btnCredits;
        [SerializeField] private GameObject _infoModal;
        [SerializeField] private Text _infoTitle;
        [SerializeField] private Text _infoBody;
        [SerializeField] private Button _closeInfoBtn;

        [SerializeField] private Text _matchCourseNameText;
        [SerializeField] private Text _matchCourseSpecsText;
        [SerializeField] private Button _btnChangeCourse;

        [SerializeField] private Button _btn2Players;
        [SerializeField] private Button _btn3Players;
        [SerializeField] private Button _btn4Players;
        [SerializeField] private Image _img2Players;
        [SerializeField] private Image _img3Players;
        [SerializeField] private Image _img4Players;

        [SerializeField] private GameObject[] _playerSlotRows;
        [SerializeField] private Text[] _playerSlotNameTexts;
        [SerializeField] private Text[] _playerSlotRoleTexts;
        [SerializeField] private Button[] _playerSlotToggleButtons;

        [SerializeField] private Button _startMatchButton;
        [SerializeField] private Button _backToHomeButton;
        [SerializeField] private Button _closeRulesButton;
        [SerializeField] private Button _hudPauseButton;
        [SerializeField] private Button _pauseResumeButton;
        [SerializeField] private Button _pauseRestartButton;
        [SerializeField] private Button _pauseSettingsButton;
        [SerializeField] private Button _pauseRulesButton;
        [SerializeField] private Button _pauseHomeButton;

        private int _selectedPlayerCount = 2;
        private bool[] _isAISlot = new bool[] { false, true, true, true };
        private SettingsCategory _activeSettingsCategory = SettingsCategory.Audio;
        private readonly Color _colorActiveTab = new Color(0.78f, 0.46f, 0.13f, 1f);
        private readonly Color _colorInactiveTab = new Color(0.10f, 0.21f, 0.21f, 1f);
        private static bool _pendingChoosePlayersAfterLoad;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) { Destroy(this); return; }

            if (PitStriker.Maps.MapManager.Instance == null && FindAnyObjectByType<PitStriker.Maps.MapManager>() == null)
                new GameObject("MapManager").AddComponent<PitStriker.Maps.MapManager>();
            if (SettingsManager.Instance == null && FindAnyObjectByType<SettingsManager>() == null)
                new GameObject("SettingsManager").AddComponent<SettingsManager>();

            EnsureUIHierarchy();
            BindButtons();
        }

        private void OnEnable() { BindButtons(); }

        private void Start()
        {
            LoadSetup();
            UpdatePlayerCountUI();
            UpdateSlotRowsUI();
            if (_pendingChoosePlayersAfterLoad)
            {
                _pendingChoosePlayersAfterLoad = false;
                UpdateMatchCourseBanner();
                ShowScreen(ScreenType.ChoosePlayers);
                return;
            }
            ShowScreen(ScreenType.Home);
            StartCoroutine(PlaySplashScreenRoutine());
        }

        public enum ScreenType
        {
            Home, ChoosePlayers, InGame, Rules, Pause, Maps, Settings,
            PlayModeSelect, OnlineMenu, CreateMatch, JoinMatch, OnlineLobby, OnlineResult, QuickMatch
        }

        public void ShowScreen(ScreenType screen)
        {
            CurrentScreen = screen;
            if (_screenBackdrop != null) _screenBackdrop.gameObject.SetActive(screen != ScreenType.InGame);
            if (_confirmation != null) _confirmation.SetActive(false);
            if (screen == ScreenType.Pause && TurnManager.Instance != null) TurnManager.Instance.SetPaused(true);
            if (_homePanel != null) _homePanel.SetActive(screen == ScreenType.Home);
            if (_choosePlayersPanel != null) _choosePlayersPanel.SetActive(screen == ScreenType.ChoosePlayers);
            if (_rulesModal != null) _rulesModal.SetActive(screen == ScreenType.Rules);
            if (_pauseModal != null) _pauseModal.SetActive(screen == ScreenType.Pause);
            if (_mapsPanel != null) _mapsPanel.SetActive(screen == ScreenType.Maps);
            if (_settingsPanel != null) _settingsPanel.SetActive(screen == ScreenType.Settings);
            if (_playModePanel != null) _playModePanel.SetActive(screen == ScreenType.PlayModeSelect);
            if (_onlineMenuPanel != null) _onlineMenuPanel.SetActive(screen == ScreenType.OnlineMenu);
            if (_createMatchPanel != null) _createMatchPanel.SetActive(screen == ScreenType.CreateMatch);
            if (_joinMatchPanel != null) _joinMatchPanel.SetActive(screen == ScreenType.JoinMatch);
            if (_onlineLobbyPanel != null) _onlineLobbyPanel.SetActive(screen == ScreenType.OnlineLobby);
            if (_onlineResultPanel != null) _onlineResultPanel.SetActive(screen == ScreenType.OnlineResult);
            if (_quickMatchPanel != null) _quickMatchPanel.SetActive(screen == ScreenType.QuickMatch);
            if (screen == ScreenType.Settings) UpdateSettingsUI();
            else if (screen == ScreenType.ChoosePlayers) UpdateMatchCourseBanner();

            bool isPlaying = (screen == ScreenType.InGame || screen == ScreenType.Pause);
            if (_hudRoot != null)
            {
                _hudRoot.SetActive(isPlaying);
                var group = _hudRoot.GetComponent<CanvasGroup>();
                if (!group) group = _hudRoot.AddComponent<CanvasGroup>();
                group.interactable = screen == ScreenType.InGame;
                group.blocksRaycasts = screen == ScreenType.InGame;
            }
            if (_hudPauseButton != null) _hudPauseButton.gameObject.SetActive(screen == ScreenType.InGame);

            if (PitStriker.Audio.AudioManager.Instance != null && _splashHasPlayed)
            {
                if (screen == ScreenType.Home || screen == ScreenType.ChoosePlayers || screen == ScreenType.Maps ||
                    screen == ScreenType.PlayModeSelect || screen == ScreenType.OnlineMenu || screen == ScreenType.CreateMatch ||
                    screen == ScreenType.JoinMatch || screen == ScreenType.OnlineLobby || screen == ScreenType.OnlineResult ||
                    screen == ScreenType.QuickMatch ||
                    (screen == ScreenType.Settings && _settingsReturn != ScreenType.Pause) ||
                    (screen == ScreenType.Rules && _rulesReturn != ScreenType.Pause))
                    PitStriker.Audio.AudioManager.Instance.PlayStartMusic();
                else if (screen == ScreenType.InGame)
                {
                    if (TurnManager.Instance != null && TurnManager.Instance.CurrentState == TurnManager.GameState.TossPhase)
                        PitStriker.Audio.AudioManager.Instance.PlayTossMusic();
                    else
                        PitStriker.Audio.AudioManager.Instance.PlayGameplayMusic();
                }
            }
        }

        private void BindButtons()
        {
            if (_homePlayButton != null) { _homePlayButton.onClick.RemoveAllListeners(); _homePlayButton.onClick.AddListener(HandleHomePlayClicked); }
            if (_homeMapsButton != null) { _homeMapsButton.onClick.RemoveAllListeners(); _homeMapsButton.onClick.AddListener(OpenMaps); }
            if (_homeRulesButton != null) { _homeRulesButton.onClick.RemoveAllListeners(); _homeRulesButton.onClick.AddListener(OpenRules); }
            if (_homeSettingsButton != null) { _homeSettingsButton.onClick.RemoveAllListeners(); _homeSettingsButton.onClick.AddListener(OpenSettings); }
            if (_homeExitButton != null) { _homeExitButton.onClick.RemoveAllListeners(); _homeExitButton.onClick.AddListener(HandleExitClicked); }
            if (_closeMapsButton != null) { _closeMapsButton.onClick.RemoveAllListeners(); _closeMapsButton.onClick.AddListener(() => ShowScreen(ScreenType.Home)); }
            if (_btnSelectMapCoastal != null) { _btnSelectMapCoastal.onClick.RemoveAllListeners(); _btnSelectMapCoastal.onClick.AddListener(() => SelectAndConfigureMap("beach")); }
            if (_btnSelectMapVillage != null) { _btnSelectMapVillage.onClick.RemoveAllListeners(); _btnSelectMapVillage.onClick.AddListener(() => SelectAndConfigureMap("village")); }
            if (_closeSettingsButton != null) { _closeSettingsButton.onClick.RemoveAllListeners(); _closeSettingsButton.onClick.AddListener(() => ShowScreen(_settingsReturn)); }
            if (_tabAudioBtn != null) { _tabAudioBtn.onClick.RemoveAllListeners(); _tabAudioBtn.onClick.AddListener(() => SwitchSettingsCategory(SettingsCategory.Audio)); }
            if (_tabGameplayBtn != null) { _tabGameplayBtn.onClick.RemoveAllListeners(); _tabGameplayBtn.onClick.AddListener(() => SwitchSettingsCategory(SettingsCategory.Gameplay)); }
            if (_tabDisplayBtn != null) { _tabDisplayBtn.onClick.RemoveAllListeners(); _tabDisplayBtn.onClick.AddListener(() => SwitchSettingsCategory(SettingsCategory.Display)); }
            if (_tabAccessibilityBtn != null) { _tabAccessibilityBtn.onClick.RemoveAllListeners(); _tabAccessibilityBtn.onClick.AddListener(() => SwitchSettingsCategory(SettingsCategory.Accessibility)); }
            if (_tabOtherBtn != null) { _tabOtherBtn.onClick.RemoveAllListeners(); _tabOtherBtn.onClick.AddListener(() => SwitchSettingsCategory(SettingsCategory.Other)); }
            if (_masterVolumeSlider != null) { _masterVolumeSlider.onValueChanged.RemoveAllListeners(); _masterVolumeSlider.onValueChanged.AddListener(HandleMasterVolumeChanged); }
            if (_musicVolumeSlider != null) { _musicVolumeSlider.onValueChanged.RemoveAllListeners(); _musicVolumeSlider.onValueChanged.AddListener(HandleMusicVolumeChanged); }
            if (_sfxVolumeSlider != null) { _sfxVolumeSlider.onValueChanged.RemoveAllListeners(); _sfxVolumeSlider.onValueChanged.AddListener(HandleSfxVolumeChanged); }
            if (_resetAudioBtn != null) { _resetAudioBtn.onClick.RemoveAllListeners(); _resetAudioBtn.onClick.AddListener(HandleResetAudioClicked); }
            if (_hapticsToggleButton != null) { _hapticsToggleButton.onClick.RemoveAllListeners(); _hapticsToggleButton.onClick.AddListener(ToggleHaptics); }
            if (_btnQualityLow != null) { _btnQualityLow.onClick.RemoveAllListeners(); _btnQualityLow.onClick.AddListener(() => HandleQualityClicked(GraphicsQualityLevel.Performance)); }
            if (_btnQualityMed != null) { _btnQualityMed.onClick.RemoveAllListeners(); _btnQualityMed.onClick.AddListener(() => HandleQualityClicked(GraphicsQualityLevel.Balanced)); }
            if (_btnQualityHigh != null) { _btnQualityHigh.onClick.RemoveAllListeners(); _btnQualityHigh.onClick.AddListener(() => HandleQualityClicked(GraphicsQualityLevel.HighFidelity)); }
            if (_closeInfoBtn != null) { _closeInfoBtn.onClick.RemoveAllListeners(); _closeInfoBtn.onClick.AddListener(CloseInfoModal); }
            if (_closeRulesButton != null) { _closeRulesButton.onClick.RemoveAllListeners(); _closeRulesButton.onClick.AddListener(() => ShowScreen(_rulesReturn)); }
            if (_btn2Players != null) { _btn2Players.onClick.RemoveAllListeners(); _btn2Players.onClick.AddListener(() => SetPlayerCount(2)); }
            if (_btn3Players != null) { _btn3Players.onClick.RemoveAllListeners(); _btn3Players.onClick.AddListener(() => SetPlayerCount(3)); }
            if (_btn4Players != null) { _btn4Players.onClick.RemoveAllListeners(); _btn4Players.onClick.AddListener(() => SetPlayerCount(4)); }
            for (int i = 0; i < 4; i++)
            {
                int index = i;
                if (_playerSlotToggleButtons != null && index < _playerSlotToggleButtons.Length && _playerSlotToggleButtons[index] != null)
                {
                    _playerSlotToggleButtons[index].onClick.RemoveAllListeners();
                    _playerSlotToggleButtons[index].onClick.AddListener(() => ToggleSlotRole(index));
                }
            }
            if (_btnChangeCourse != null) { _btnChangeCourse.onClick.RemoveAllListeners(); _btnChangeCourse.onClick.AddListener(OpenMaps); }
            if (_startMatchButton != null) { _startMatchButton.onClick.RemoveAllListeners(); _startMatchButton.onClick.AddListener(HandleStartMatchClicked); }
            if (_backToHomeButton != null) { _backToHomeButton.onClick.RemoveAllListeners(); _backToHomeButton.onClick.AddListener(() => ShowScreen(ScreenType.PlayModeSelect)); }
            if (_hudPauseButton != null) { _hudPauseButton.onClick.RemoveAllListeners(); _hudPauseButton.onClick.AddListener(HandlePauseClicked); }
            if (_pauseResumeButton != null) { _pauseResumeButton.onClick.RemoveAllListeners(); _pauseResumeButton.onClick.AddListener(HandleResumeClicked); }
            if (_pauseRestartButton != null) { _pauseRestartButton.onClick.RemoveAllListeners(); _pauseRestartButton.onClick.AddListener(() => Confirm("Restart this match?", "Scores and marble positions will reset. Your players stay the same.", HandleRestartClicked)); }
            if (_pauseSettingsButton != null) { _pauseSettingsButton.onClick.RemoveAllListeners(); _pauseSettingsButton.onClick.AddListener(OpenSettings); }
            if (_pauseRulesButton != null) { _pauseRulesButton.onClick.RemoveAllListeners(); _pauseRulesButton.onClick.AddListener(OpenRules); }
            if (_pauseHomeButton != null) { _pauseHomeButton.onClick.RemoveAllListeners(); _pauseHomeButton.onClick.AddListener(() => Confirm("Leave this match?", "This match will end and you will return to the main menu.", HandleHomeClicked)); }
        }

        public void OpenMaps() { ShowScreen(ScreenType.Maps); }

        public void SelectAndConfigureMap(string mapId)
        {
            PitStriker.Maps.MapDefinition map = null;
            if (PitStriker.Maps.MapManager.Instance != null)
            {
                PitStriker.Maps.MapManager.Instance.SelectMap(mapId);
                map = PitStriker.Maps.MapManager.Instance.SelectedMap;
            }
            if (map != null && !string.IsNullOrEmpty(map.sceneName) && map.sceneName != SceneManager.GetActiveScene().name)
            {
                _pendingChoosePlayersAfterLoad = true;
                SceneManager.LoadScene(map.sceneName);
                return;
            }
            UpdateMatchCourseBanner();
            ShowScreen(ScreenType.ChoosePlayers);
        }

        public void UpdateMatchCourseBanner()
        {
            var map = PitStriker.Maps.MapManager.Instance != null ? PitStriker.Maps.MapManager.Instance.SelectedMap : null;
            if (map != null)
            {
                if (_matchCourseNameText != null) _matchCourseNameText.text = map.mapNumber.ToUpper() + " • " + map.displayName.ToUpper();
                if (_matchCourseSpecsText != null) _matchCourseSpecsText.text = map.specifications;
            }
        }

        public void OpenSettings()
        {
            _settingsReturn = CurrentScreen == ScreenType.Pause ? ScreenType.Pause : ScreenType.Home;
            SwitchSettingsCategory(SettingsCategory.Audio);
            ShowScreen(ScreenType.Settings);
        }

        public void SwitchSettingsCategory(SettingsCategory category)
        {
            _activeSettingsCategory = category;
            if (_audioSettingsPanel != null) _audioSettingsPanel.SetActive(category == SettingsCategory.Audio);
            if (_gameplaySettingsPanel != null) _gameplaySettingsPanel.SetActive(category == SettingsCategory.Gameplay);
            if (_displaySettingsPanel != null) _displaySettingsPanel.SetActive(category == SettingsCategory.Display);
            if (_accessibilitySettingsPanel != null) _accessibilitySettingsPanel.SetActive(category == SettingsCategory.Accessibility);
            if (_otherSettingsPanel != null) _otherSettingsPanel.SetActive(category == SettingsCategory.Other);
            UpdateTabButtonVisuals();
            UpdateSettingsUI();
        }

        private void UpdateTabButtonVisuals()
        {
            SetTabColor(_tabAudioBtn, _activeSettingsCategory == SettingsCategory.Audio);
            SetTabColor(_tabGameplayBtn, _activeSettingsCategory == SettingsCategory.Gameplay);
            SetTabColor(_tabDisplayBtn, _activeSettingsCategory == SettingsCategory.Display);
            SetTabColor(_tabAccessibilityBtn, _activeSettingsCategory == SettingsCategory.Accessibility);
            SetTabColor(_tabOtherBtn, _activeSettingsCategory == SettingsCategory.Other);
        }

        private void SetTabColor(Button btn, bool active)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img != null) img.color = active ? _colorActiveTab : _colorInactiveTab;
        }

        private void UpdateSettingsUI()
        {
            var sm = SettingsManager.Instance;
            if (sm != null)
            {
                if (_masterVolumeSlider != null) _masterVolumeSlider.SetValueWithoutNotify(sm.MasterVolume);
                if (_musicVolumeSlider != null) _musicVolumeSlider.SetValueWithoutNotify(sm.MusicVolume);
                if (_sfxVolumeSlider != null) _sfxVolumeSlider.SetValueWithoutNotify(sm.SfxVolume);
                if (_masterVolumeText != null) _masterVolumeText.text = Mathf.RoundToInt(sm.MasterVolume * 100f) + "%";
                if (_musicVolumeText != null) _musicVolumeText.text = Mathf.RoundToInt(sm.MusicVolume * 100f) + "%";
                if (_sfxVolumeText != null) _sfxVolumeText.text = Mathf.RoundToInt(sm.SfxVolume * 100f) + "%";
                if (_hapticsToggleText != null) _hapticsToggleText.text = sm.HapticsEnabled ? "VIBRATION: ENABLED" : "VIBRATION: MUTED";
                UpdateQualityButtonVisuals(sm.GraphicsQuality);
            }
        }

        private void UpdateQualityButtonVisuals(GraphicsQualityLevel level)
        {
            SetQualityBtnColor(_btnQualityLow, level == GraphicsQualityLevel.Performance);
            SetQualityBtnColor(_btnQualityMed, level == GraphicsQualityLevel.Balanced);
            SetQualityBtnColor(_btnQualityHigh, level == GraphicsQualityLevel.HighFidelity);
        }

        private void SetQualityBtnColor(Button btn, bool active)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img != null) img.color = active ? new Color(0.12f, 0.58f, 0.45f, 1f) : new Color(0.14f, 0.18f, 0.24f, 0.85f);
        }

        private void HandleMasterVolumeChanged(float value)
        {
            if (SettingsManager.Instance != null) SettingsManager.Instance.MasterVolume = value;
            if (_masterVolumeText != null) _masterVolumeText.text = Mathf.RoundToInt(value * 100f) + "%";
        }
        private void HandleMusicVolumeChanged(float value)
        {
            if (SettingsManager.Instance != null) SettingsManager.Instance.MusicVolume = value;
            if (_musicVolumeText != null) _musicVolumeText.text = Mathf.RoundToInt(value * 100f) + "%";
        }
        private void HandleSfxVolumeChanged(float value)
        {
            if (SettingsManager.Instance != null) SettingsManager.Instance.SfxVolume = value;
            if (_sfxVolumeText != null) _sfxVolumeText.text = Mathf.RoundToInt(value * 100f) + "%";
        }
        private void HandleResetAudioClicked()
        {
            if (SettingsManager.Instance != null) SettingsManager.Instance.ResetAudioDefaults();
            UpdateSettingsUI();
        }
        private void ToggleHaptics()
        {
            if (SettingsManager.Instance != null)
            {
                SettingsManager.Instance.HapticsEnabled = !SettingsManager.Instance.HapticsEnabled;
                if (SettingsManager.Instance.HapticsEnabled) SettingsManager.Instance.TriggerHapticFeedback();
            }
            UpdateSettingsUI();
        }
        private void HandleQualityClicked(GraphicsQualityLevel level)
        {
            if (SettingsManager.Instance != null) SettingsManager.Instance.GraphicsQuality = level;
            UpdateSettingsUI();
        }
        public void OpenInfoModal(string title, string content)
        {
            if (_infoTitle != null) _infoTitle.text = title;
            if (_infoBody != null) _infoBody.text = content;
            if (_infoModal != null) { _infoModal.SetActive(true); _infoModal.transform.SetAsLastSibling(); }
        }
        public void CloseInfoModal()
        {
            if (_infoModal != null) _infoModal.SetActive(false);
        }
        public void SetPlayerCount(int count)
        {
            _selectedPlayerCount = Mathf.Clamp(count, 2, 4);
            UpdatePlayerCountUI();
            UpdateSlotRowsUI();
        }
        private void ToggleSlotRole(int slotIndex)
        {
            if (slotIndex <= 0 || slotIndex >= _isAISlot.Length) return;
            _isAISlot[slotIndex] = !_isAISlot[slotIndex];
            UpdateSlotRowsUI();
        }
        private void UpdatePlayerCountUI()
        {
            if (_img2Players != null) _img2Players.color = _selectedPlayerCount == 2 ? _colorActiveTab : _colorInactiveTab;
            if (_img3Players != null) _img3Players.color = _selectedPlayerCount == 3 ? _colorActiveTab : _colorInactiveTab;
            if (_img4Players != null) _img4Players.color = _selectedPlayerCount == 4 ? _colorActiveTab : _colorInactiveTab;
        }
        private void UpdateSlotRowsUI()
        {
            for (int i = 0; i < 4; i++)
            {
                bool slotVisible = i < _selectedPlayerCount;
                if (_playerSlotRows != null && i < _playerSlotRows.Length && _playerSlotRows[i] != null)
                    _playerSlotRows[i].SetActive(slotVisible);
                if (!slotVisible) continue;
                bool isBot = _isAISlot[i];
                if (_playerSlotNameTexts != null && i < _playerSlotNameTexts.Length && _playerSlotNameTexts[i] != null)
                    _playerSlotNameTexts[i].text = isBot ? ("Bot " + (i + 1)) : ("Player " + (i + 1));
                if (_playerSlotRoleTexts != null && i < _playerSlotRoleTexts.Length && _playerSlotRoleTexts[i] != null)
                {
                    _playerSlotRoleTexts[i].text = i == 0 ? "YOU / HUMAN" : isBot ? "COMPUTER  >" : "HUMAN  >";
                    _playerSlotRoleTexts[i].color = isBot ? new Color(1f, 0.45f, 0.25f, 1f) : new Color(0.25f, 0.95f, 0.55f, 1f);
                }
            }
        }
        private void HandleHomePlayClicked() { ShowScreen(ScreenType.PlayModeSelect); }
        private void HandleStartMatchClicked()
        {
            bool[] configuredIsAI = new bool[_selectedPlayerCount];
            string[] names = new string[_selectedPlayerCount];
            for (int i = 0; i < _selectedPlayerCount; i++)
            {
                configuredIsAI[i] = _isAISlot[i];
                names[i] = configuredIsAI[i] ? ("Bot " + (i + 1)) : ("Player " + (i + 1));
            }
            SaveSetup();
            ShowScreen(ScreenType.InGame);
            TurnManager tm = TurnManager.Instance != null ? TurnManager.Instance : UnityEngine.Object.FindAnyObjectByType<TurnManager>();
            if (tm != null) tm.ConfigureAndStartMatch(_selectedPlayerCount, configuredIsAI, names);
        }
        private void HandlePauseClicked()
        {
            bool isOnline = (PitStriker.Networking.Client.CloudMatchManager.Instance != null &&
                             PitStriker.Networking.Client.CloudMatchManager.Instance.IsOnlineMatchActive) ||
                            (PitStriker.Networking.NetworkSessionManager.Instance != null && PitStriker.Networking.NetworkSessionManager.Instance.IsConnected);
            if (isOnline) { ShowOnlineNotice("LIVE MATCH", "You cannot pause a live online match."); return; }
            TurnManager tm = TurnManager.Instance != null ? TurnManager.Instance : UnityEngine.Object.FindAnyObjectByType<TurnManager>();
            if (tm != null) tm.SetPaused(true);
            ShowScreen(ScreenType.Pause);
        }
        private void HandleResumeClicked()
        {
            TurnManager tm = TurnManager.Instance != null ? TurnManager.Instance : UnityEngine.Object.FindAnyObjectByType<TurnManager>();
            if (tm != null) tm.SetPaused(false);
            ShowScreen(ScreenType.InGame);
        }
        private void HandleRestartClicked()
        {
            TurnManager tm = TurnManager.Instance != null ? TurnManager.Instance : UnityEngine.Object.FindAnyObjectByType<TurnManager>();
            if (tm != null) tm.RestartMatch();
            ShowScreen(ScreenType.InGame);
        }
        public void HandleHomeClicked()
        {
            TurnManager tm = TurnManager.Instance != null ? TurnManager.Instance : UnityEngine.Object.FindAnyObjectByType<TurnManager>();
            if (tm != null) tm.ReturnToMainMenu();
            ShowScreen(ScreenType.Home);
        }
        private void HandleExitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        private void EnsureUIHierarchy() { BuildModernUI(); }

        private static GameObject CreatePanel(string name, Transform parent, Color color)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            Image img = obj.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return obj;
        }
        private static GameObject CreateImage(string name, Transform parent, Color color)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>();
            Image img = obj.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return obj;
        }
        private static GameObject CreateText(string name, Transform parent, string text, int fontSize, FontStyle style, Color color, TextAnchor align)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>();
            Text t = obj.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.text = text; t.fontSize = fontSize; t.fontStyle = style; t.color = color; t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return obj;
        }
        private static GameObject CreateButton(string name, Transform parent, string label, Color bgColor, int fontSize)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            btnObj.AddComponent<RectTransform>();
            Image img = btnObj.AddComponent<Image>();
            img.color = bgColor;
            img.raycastTarget = true;
            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;
            GameObject textObj = CreateText("Text", btnObj.transform, label, fontSize, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one; textRect.sizeDelta = Vector2.zero;
            return btnObj;
        }
        private static void SetRect(GameObject obj, Vector2 anchorMin, Vector2 anchorMax, Vector2 size)
        {
            RectTransform rect = obj.GetComponent<RectTransform>();
            if (rect == null) rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin; rect.anchorMax = anchorMax; rect.sizeDelta = size; rect.anchoredPosition = Vector2.zero;
        }
    }
}
