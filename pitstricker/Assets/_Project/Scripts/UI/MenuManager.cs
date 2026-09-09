using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PitStriker.Gameplay;

namespace PitStriker.UI
{
    /// <summary>
    /// Coordinates all primary game navigation screens:
    /// 1. Home Screen (Play, How to Play, Exit)
    /// 2. Match Setup (Choose 2/3/4 Players, toggle Real Player vs AI Bot per slot)
    /// 3. How to Play Rules Modal
    /// 4. In-Game Pause Menu
    /// 
    /// Features automatic programmatic UI construction so screens exist and function
    /// seamlessly across all scenes and standalone APK builds without manual setup.
    /// </summary>
    public partial class MenuManager : MonoBehaviour
    {
        public static MenuManager Instance { get; private set; }

        [Header("Main Screen Panels")]
        [SerializeField] private GameObject _homePanel;
        [SerializeField] private GameObject _choosePlayersPanel;
        [SerializeField] private GameObject _rulesModal;
        [SerializeField] private GameObject _pauseModal;
        [SerializeField] private GameObject _hudRoot;

        [Header("Home Screen Controls")]
        [SerializeField] private Button _homePlayButton;
        [SerializeField] private Button _homeRulesButton;
        [SerializeField] private Button _homeExitButton;

        [Header("Player Count Selectors")]
        [SerializeField] private Button _btn2Players;
        [SerializeField] private Button _btn3Players;
        [SerializeField] private Button _btn4Players;
        [SerializeField] private Image _img2Players;
        [SerializeField] private Image _img3Players;
        [SerializeField] private Image _img4Players;

        [Header("Player Slot Configuration Cards")]
        [SerializeField] private GameObject[] _playerSlotRows; // Rows for P1, P2, P3, P4
        [SerializeField] private Text[] _playerSlotNameTexts;
        [SerializeField] private Text[] _playerSlotRoleTexts;  // "REAL PLAYER" or "AI BOT"
        [SerializeField] private Button[] _playerSlotToggleButtons; // Toggles AI vs Real

        [Header("Match Setup Actions")]
        [SerializeField] private Button _startMatchButton;
        [SerializeField] private Button _backToHomeButton;

        [Header("Rules Modal Controls")]
        [SerializeField] private Button _closeRulesButton;

        [Header("Pause Modal Controls")]
        [SerializeField] private Button _hudPauseButton;
        [SerializeField] private Button _pauseResumeButton;
        [SerializeField] private Button _pauseRestartButton;
        [SerializeField] private Button _pauseHomeButton;

        // Configuration State
        private int _selectedPlayerCount = 2;
        private bool[] _isAISlot = new bool[] { false, true, true, true }; // P1 Human, P2-P4 default AI

        private readonly Color _colorActiveTab = new Color(0.78f, 0.46f, 0.13f, 1f);
        private readonly Color _colorInactiveTab = new Color(0.10f, 0.21f, 0.21f, 1f);

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(this);
                return;
            }

            EnsureUIHierarchy();
            BindButtons();
        }

        private void OnEnable()
        {
            BindButtons();
        }

        private void Start()
        {
            LoadSetup();
            UpdatePlayerCountUI();
            UpdateSlotRowsUI();

            // Default startup: Show Home Screen, hide match & pause panels
            ShowScreen(ScreenType.Home);

            StartCoroutine(PlaySplashScreenRoutine());
        }

        public enum ScreenType
        {
            Home,
            ChoosePlayers,
            InGame,
            Rules,
            Pause
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

            bool isPlaying = (screen == ScreenType.InGame || screen == ScreenType.Pause);
            if (_hudRoot != null)
            {
                _hudRoot.SetActive(isPlaying);
                var group = _hudRoot.GetComponent<CanvasGroup>();
                if (!group) group = _hudRoot.AddComponent<CanvasGroup>();
                group.interactable = screen == ScreenType.InGame;
                group.blocksRaycasts = screen == ScreenType.InGame;
            }

            if (_hudPauseButton != null)
            {
                _hudPauseButton.gameObject.SetActive(screen == ScreenType.InGame);
            }

            // Music orchestration: Start / Menu music on main menus, Toss / Gameplay music during match
            if (PitStriker.Audio.AudioManager.Instance != null && _splashHasPlayed)
            {
                if (screen == ScreenType.Home || screen == ScreenType.ChoosePlayers || (screen == ScreenType.Rules && _rulesReturn != ScreenType.Pause))
                {
                    PitStriker.Audio.AudioManager.Instance.PlayStartMusic();
                }
                else if (screen == ScreenType.InGame)
                {
                    if (TurnManager.Instance != null && TurnManager.Instance.CurrentState == TurnManager.GameState.TossPhase)
                    {
                        PitStriker.Audio.AudioManager.Instance.PlayTossMusic();
                    }
                    else
                    {
                        PitStriker.Audio.AudioManager.Instance.PlayGameplayMusic();
                    }
                }
            }
        }

        private void BindButtons()
        {
            // Home Screen
            if (_homePlayButton != null)
            {
                _homePlayButton.onClick.RemoveAllListeners();
                _homePlayButton.onClick.AddListener(HandleHomePlayClicked);
            }

            if (_homeRulesButton != null)
            {
                _homeRulesButton.onClick.RemoveAllListeners();
                _homeRulesButton.onClick.AddListener(OpenRules);
            }

            if (_homeExitButton != null)
            {
                _homeExitButton.onClick.RemoveAllListeners();
                _homeExitButton.onClick.AddListener(HandleExitClicked);
            }

            // Rules Screen
            if (_closeRulesButton != null)
            {
                _closeRulesButton.onClick.RemoveAllListeners();
                _closeRulesButton.onClick.AddListener(() => ShowScreen(_rulesReturn));
            }

            // Player Count Tabs
            if (_btn2Players != null)
            {
                _btn2Players.onClick.RemoveAllListeners();
                _btn2Players.onClick.AddListener(() => SetPlayerCount(2));
            }
            if (_btn3Players != null)
            {
                _btn3Players.onClick.RemoveAllListeners();
                _btn3Players.onClick.AddListener(() => SetPlayerCount(3));
            }
            if (_btn4Players != null)
            {
                _btn4Players.onClick.RemoveAllListeners();
                _btn4Players.onClick.AddListener(() => SetPlayerCount(4));
            }

            // Player Slot Toggles (Slot 0 is P1 - human, Slot 1..3 are P2..P4)
            for (int i = 0; i < 4; i++)
            {
                int index = i;
                if (_playerSlotToggleButtons != null && index < _playerSlotToggleButtons.Length && _playerSlotToggleButtons[index] != null)
                {
                    _playerSlotToggleButtons[index].onClick.RemoveAllListeners();
                    _playerSlotToggleButtons[index].onClick.AddListener(() => ToggleSlotRole(index));
                }
            }

            // Match Setup Actions
            if (_startMatchButton != null)
            {
                _startMatchButton.onClick.RemoveAllListeners();
                _startMatchButton.onClick.AddListener(HandleStartMatchClicked);
            }

            if (_backToHomeButton != null)
            {
                _backToHomeButton.onClick.RemoveAllListeners();
                _backToHomeButton.onClick.AddListener(() => ShowScreen(ScreenType.Home));
            }

            // Pause Modal
            if (_hudPauseButton != null)
            {
                _hudPauseButton.onClick.RemoveAllListeners();
                _hudPauseButton.onClick.AddListener(HandlePauseClicked);
            }

            if (_pauseResumeButton != null)
            {
                _pauseResumeButton.onClick.RemoveAllListeners();
                _pauseResumeButton.onClick.AddListener(HandleResumeClicked);
            }

            if (_pauseRestartButton != null)
            {
                _pauseRestartButton.onClick.RemoveAllListeners();
                _pauseRestartButton.onClick.AddListener(() => Confirm("Restart this match?", "Scores and marble positions will reset. Your players stay the same.", HandleRestartClicked));
            }

            if (_pauseHomeButton != null)
            {
                _pauseHomeButton.onClick.RemoveAllListeners();
                _pauseHomeButton.onClick.AddListener(() => Confirm("Leave this match?", "This match will end and you will return to the main menu.", HandleHomeClicked));
            }
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
                {
                    _playerSlotRows[i].SetActive(slotVisible);
                }

                if (slotVisible)
                {
                    bool isBot = _isAISlot[i];

                    if (_playerSlotNameTexts != null && i < _playerSlotNameTexts.Length && _playerSlotNameTexts[i] != null)
                    {
                        _playerSlotNameTexts[i].text = isBot ? $"Bot {i + 1}" : $"Player {i + 1}";
                    }

                    if (_playerSlotRoleTexts != null && i < _playerSlotRoleTexts.Length && _playerSlotRoleTexts[i] != null)
                    {
                        _playerSlotRoleTexts[i].text = i == 0 ? "YOU / HUMAN" : isBot ? "COMPUTER  >" : "HUMAN  >";
                        _playerSlotRoleTexts[i].color = isBot ? new Color(1f, 0.45f, 0.25f, 1f) : new Color(0.25f, 0.95f, 0.55f, 1f);
                    }
                }
            }
        }

        private void HandleHomePlayClicked()
        {
            Debug.Log("<color=#00FFAA><b>[MENU]</b> PLAY MATCH button clicked -> Navigating to Choose Players...</color>");
            ShowScreen(ScreenType.ChoosePlayers);
        }

        private void HandleStartMatchClicked()
        {
            Debug.Log($"<color=#00FFAA><b>[MENU]</b> START MATCH button clicked! Configuring match for {_selectedPlayerCount} players...</color>");
            bool[] configuredIsAI = new bool[_selectedPlayerCount];
            string[] names = new string[_selectedPlayerCount];

            for (int i = 0; i < _selectedPlayerCount; i++)
            {
                configuredIsAI[i] = _isAISlot[i];
                names[i] = configuredIsAI[i] ? $"Bot {i + 1}" : $"Player {i + 1}";
            }

            SaveSetup();
            ShowScreen(ScreenType.InGame);

            TurnManager tm = TurnManager.Instance != null ? TurnManager.Instance : UnityEngine.Object.FindAnyObjectByType<TurnManager>();
            if (tm != null)
            {
                tm.ConfigureAndStartMatch(_selectedPlayerCount, configuredIsAI, names);
                Debug.Log("<color=#00FF88><b>[MENU]</b> Match successfully launched via TurnManager!</color>");
            }
            else
            {
                Debug.LogError("<color=#FF0044><b>[MENU ERROR]</b> TurnManager could not be found anywhere in the scene!</color>");
            }
        }

        private void HandlePauseClicked()
        {
            TurnManager tm = TurnManager.Instance != null ? TurnManager.Instance : UnityEngine.Object.FindAnyObjectByType<TurnManager>();
            if (tm != null)
            {
                tm.SetPaused(true);
            }
            ShowScreen(ScreenType.Pause);
        }

        private void HandleResumeClicked()
        {
            TurnManager tm = TurnManager.Instance != null ? TurnManager.Instance : UnityEngine.Object.FindAnyObjectByType<TurnManager>();
            if (tm != null)
            {
                tm.SetPaused(false);
            }
            ShowScreen(ScreenType.InGame);
        }

        private void HandleRestartClicked()
        {
            TurnManager tm = TurnManager.Instance != null ? TurnManager.Instance : UnityEngine.Object.FindAnyObjectByType<TurnManager>();
            if (tm != null)
            {
                tm.RestartMatch();
            }
            ShowScreen(ScreenType.InGame);
        }

        public void HandleHomeClicked()
        {
            TurnManager tm = TurnManager.Instance != null ? TurnManager.Instance : UnityEngine.Object.FindAnyObjectByType<TurnManager>();
            if (tm != null)
            {
                tm.ReturnToMainMenu();
            }
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

        /// <summary>
        /// Ensures all screens exist in the scene hierarchy; builds them programmatically if missing.
        /// </summary>
        private void EnsureUIHierarchy() { BuildModernUI(); }

        private static GameObject CreatePanel(string name, Transform parent, Color color)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;

            Image img = obj.AddComponent<Image>();
            img.color = color;
            return obj;
        }

        private static GameObject CreateImage(string name, Transform parent, Color color)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>();
            Image img = obj.AddComponent<Image>();
            img.color = color;
            return obj;
        }

        private static GameObject CreateText(string name, Transform parent, string text, int fontSize, FontStyle style, Color color, TextAnchor align)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>();

            Text t = obj.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.text = text;
            t.fontSize = fontSize;
            t.fontStyle = style;
            t.color = color;
            t.alignment = align;
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

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            btn.targetGraphic = img;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            cb.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            cb.selectedColor = Color.white;
            cb.fadeDuration = 0.08f;
            btn.colors = cb;

            GameObject textObj = CreateText("Text", btnObj.transform, label, fontSize, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;

            return btnObj;
        }

        private static void SetRect(GameObject obj, Vector2 anchorMin, Vector2 anchorMax, Vector2 size)
        {
            RectTransform rect = obj.GetComponent<RectTransform>();
            if (rect == null) rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
        }
    }
}
