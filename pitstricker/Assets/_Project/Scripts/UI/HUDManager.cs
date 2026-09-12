using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PitStriker.Input;
using PitStriker.Gameplay;
using PitStriker.Networking;

namespace PitStriker.UI
{
    /// <summary>
    /// Phase 3 HUD Manager:
    /// Displays real-time Power Meter, Objective Stage Beads (1 -> 2 -> 3),
    /// and Strike controls matching the Concept Art layout.
    /// </summary>
    public class HUDManager : MonoBehaviour
    {
#pragma warning disable CS0649
        [Header("Power Gauge Elements")]
        [SerializeField] private Slider _powerSlider;
        [SerializeField] private Image _powerFillImage;
        [SerializeField] private Text _powerLabel;

        [Header("Strike Action Button")]
        [SerializeField] private Button _strikeButton;

        [Header("Objective Stage Beads (1 -> 2 -> 3)")]
        [SerializeField] private Image _bead1Image;
        [SerializeField] private Image _bead2Image;
        [SerializeField] private Image _bead3Image;
        [SerializeField] private Text _statusBanner;

        [Header("Score & Stroke Tracking")]
        [SerializeField] private Text _strokeCounterText;
        [SerializeField] private Text _parText;

        [Header("Player Roster Badges (Concept Art layout)")]
        [SerializeField] private Image[] _playerBadgeImages; // P1..P4 badge backgrounds
        [SerializeField] private Text[] _playerBadgeTexts;   // P1..P4 labels
        [SerializeField] private Image[] _playerBadgeGlows;   // Active turn ring highlights

        [Header("Victory Modal Panel")]
        [SerializeField] private GameObject _victoryModal;
        [SerializeField] private Text _victoryStrokesText;
        [SerializeField] private Text _victoryParText;
        [SerializeField] private Text _victoryRatingText;
        [SerializeField] private Button _playAgainButton;
        [SerializeField] private Button _homeMenuButton;

        [Header("In-Game Top-Bar Navigation Controls")]
        [SerializeField] private Button _topHomeButton;
        [SerializeField] private Button _topRestartButton;
        [SerializeField] private Button _topPauseButton;

        // Current Stage Progression (1 -> 2 -> 3)
        private int _currentObjectivePit = 1;

        private void Awake()
        {
            // Ensure HUD Canvas sorting order is above GameScreens (sortingOrder 50)
            Canvas canvas = GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.sortingOrder = 60;
            }
            if (GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }

            // Runtime Safety Check: Replace legacy StandaloneInputModule if present to prevent Unity 6 New Input System exceptions
            var es = Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (es != null)
            {
                var standalone = es.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                if (standalone != null)
                {
                    Destroy(standalone);
                    if (es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
                    {
                        es.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                    }
                }
            }

            // Ensure TurnManager is active in the scene even if arena was not regenerated
            if (TurnManager.Instance == null)
            {
                TurnManager existingTm = Object.FindAnyObjectByType<TurnManager>();
                if (existingTm == null)
                {
                    GameObject tmObj = new GameObject("TurnManager");
                    tmObj.AddComponent<TurnManager>();
                }
            }

            if (_strikeButton != null)
            {
                _strikeButton.onClick.AddListener(HandleStrikeClicked);
            }

            if (_powerSlider != null)
            {
                _powerSlider.onValueChanged.AddListener(HandleSliderValueChanged);
            }

            if (_playAgainButton != null)
            {
                _playAgainButton.onClick.AddListener(HandlePlayAgainClicked);
            }

            if (_homeMenuButton != null)
            {
                _homeMenuButton.onClick.AddListener(HandleHomeMenuClicked);
            }

            EnsureTopBarNavigationButtons();

            if (_victoryModal != null)
            {
                _victoryModal.SetActive(false);
            }
        }

        private void EnsureTopBarNavigationButtons()
        {
            // Navigation belongs to MenuManager's always-visible overlay.
            var legacy = transform.Find("TopBar_Navigation");
            if (legacy) legacy.gameObject.SetActive(false);
            var oldPause = transform.Find("Btn_HUD_Pause");
            if (oldPause) oldPause.gameObject.SetActive(false);
        }

        private static GameObject CreateNavButton(string name, Transform parent, string label, Color bgColor)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            btnObj.AddComponent<RectTransform>();

            Image img = btnObj.AddComponent<Image>();
            img.color = bgColor;

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = bgColor;
            cb.highlightedColor = Color.Lerp(bgColor, Color.white, 0.25f);
            cb.pressedColor = Color.Lerp(bgColor, Color.black, 0.25f);
            btn.colors = cb;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;

            Text t = textObj.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.text = label;
            t.fontSize = 15;
            t.fontStyle = FontStyle.Bold;
            t.color = Color.white;
            t.alignment = TextAnchor.MiddleCenter;
            t.raycastTarget = false;

            return btnObj;
        }

        private void HandleTopPauseClicked()
        {
            bool isOnline = (PitStriker.Networking.Client.CloudMatchManager.Instance != null &&
                             PitStriker.Networking.Client.CloudMatchManager.Instance.IsOnlineMatchActive) ||
                            (NetworkSessionManager.Instance != null && NetworkSessionManager.Instance.IsConnected);

            if (isOnline)
            {
                if (MenuManager.Instance != null)
                {
                    MenuManager.Instance.ShowOnlineNotice("LIVE MATCH", "You cannot pause a live online match. To forfeit and return to menu, use the options menu.");
                }
                return;
            }

            if (MenuManager.Instance != null)
            {
                MenuManager.Instance.ShowScreen(MenuManager.ScreenType.Pause);
                if (TurnManager.Instance != null)
                {
                    TurnManager.Instance.SetPaused(true);
                }
            }
            else if (TurnManager.Instance != null)
            {
                TurnManager.Instance.SetPaused(true);
            }
        }

        private void OnEnable()
        {
            SwipeLaunchController.OnPowerChanged += HandlePowerChanged;
            TurnManager.OnActivePlayerChanged += HandleActivePlayerChanged;
            TurnManager.OnStrokeCountChanged += HandleStrokeCountChanged;
            TurnManager.OnTargetPitChanged += HandleTargetPitChanged;
            TurnManager.OnMatchVictory += HandleMatchVictory;
            TurnManager.OnStatusMessage += HandleStatusMessage;
            TurnManager.OnStateChanged += HandleStateChanged;

            NetworkMatchState.OnTimerTickEvent += HandleNetworkTimerTick;
            NetworkMatchState.OnActivePlayerChangedEvent += HandleNetworkActivePlayerChanged;

            PitStriker.Networking.Client.CloudMatchManager.OnTimerTickEvent += HandleCloudTimerTick;
            PitStriker.Networking.Client.CloudMatchManager.OnActivePlayerChangedEvent += HandleCloudActivePlayerChanged;
        }

        private void OnDisable()
        {
            SwipeLaunchController.OnPowerChanged -= HandlePowerChanged;
            TurnManager.OnActivePlayerChanged -= HandleActivePlayerChanged;
            TurnManager.OnStrokeCountChanged -= HandleStrokeCountChanged;
            TurnManager.OnTargetPitChanged -= HandleTargetPitChanged;
            TurnManager.OnMatchVictory -= HandleMatchVictory;
            TurnManager.OnStatusMessage -= HandleStatusMessage;
            TurnManager.OnStateChanged -= HandleStateChanged;

            NetworkMatchState.OnTimerTickEvent -= HandleNetworkTimerTick;
            NetworkMatchState.OnActivePlayerChangedEvent -= HandleNetworkActivePlayerChanged;

            PitStriker.Networking.Client.CloudMatchManager.OnTimerTickEvent -= HandleCloudTimerTick;
            PitStriker.Networking.Client.CloudMatchManager.OnActivePlayerChangedEvent -= HandleCloudActivePlayerChanged;
        }

        private void HandleNetworkTimerTick(float secondsRemaining)
        {
            if (NetworkSessionManager.Instance == null || NetworkSessionManager.Instance.ActiveNetworkMode == NetworkSessionManager.NetworkMode.None) return;
            if (NetworkMatchState.Instance == null || NetworkMatchState.Instance.IsMatchCompleted.Value) return;

            int secs = Mathf.Max(0, Mathf.CeilToInt(secondsRemaining));
            bool isMyTurn = NetworkMatchState.Instance.IsMyTurn();

            if (_parText != null && TurnManager.Instance != null)
            {
                _parText.text = $"TIME: {secs}s  |  COURSE PAR: {TurnManager.Instance.CoursePar}";
            }

            if (_statusBanner != null && NetworkMatchState.Instance.CurrentPhase.Value == NetworkMatchPhase.ReadyToAim)
            {
                if (isMyTurn)
                {
                    _statusBanner.text = $"★ YOUR TURN ({secs}s) • AIM FOR PIT {TurnManager.Instance?.CurrentTargetPit} ★";
                }
                else
                {
                    _statusBanner.text = $"OPPONENT'S TURN ({secs}s) • WATCHING PLAY";
                }
            }

            if (_strikeButton != null && TurnManager.Instance != null && TurnManager.Instance.ActivePlayer != null)
            {
                _strikeButton.interactable = isMyTurn && !TurnManager.Instance.ActivePlayer.isAI;
            }
        }

        private void HandleNetworkActivePlayerChanged(int activePlayerIndex)
        {
            if (NetworkSessionManager.Instance == null || NetworkSessionManager.Instance.ActiveNetworkMode == NetworkSessionManager.NetworkMode.None) return;
            if (NetworkMatchState.Instance == null) return;

            bool isMyTurn = NetworkMatchState.Instance.IsMyTurn();
            if (_strikeButton != null)
            {
                _strikeButton.interactable = isMyTurn;
            }

            if (_statusBanner != null)
            {
                _statusBanner.text = isMyTurn ? "★ YOUR TURN! AIM & STRIKE ★" : "OPPONENT'S TURN • WATCHING PLAY";
            }
        }

        private void HandleCloudTimerTick(float secondsRemaining)
        {
            if (PitStriker.Networking.Client.CloudMatchManager.Instance == null || !PitStriker.Networking.Client.CloudMatchManager.Instance.IsOnlineMatchActive) return;

            int secs = Mathf.Max(0, Mathf.CeilToInt(secondsRemaining));
            bool isMyTurn = PitStriker.Networking.Client.CloudMatchManager.Instance.IsMyTurn();

            if (_parText != null && TurnManager.Instance != null)
            {
                _parText.text = $"TIME: {secs}s  |  COURSE PAR: {TurnManager.Instance.CoursePar}";
            }

            if (_statusBanner != null && PitStriker.Networking.Client.CloudMatchManager.Instance.CurrentPhase == PitStriker.Networking.Shared.CloudMatchPhase.ReadyToAim)
            {
                _statusBanner.text = isMyTurn ? $"★ YOUR TURN ({secs}s) • AIM & STRIKE ★" : $"OPPONENT'S TURN ({secs}s) • WATCHING PLAY";
            }

            if (_strikeButton != null)
            {
                _strikeButton.interactable = isMyTurn;
            }
        }

        private void HandleCloudActivePlayerChanged(int activePlayerIndex)
        {
            if (PitStriker.Networking.Client.CloudMatchManager.Instance == null || !PitStriker.Networking.Client.CloudMatchManager.Instance.IsOnlineMatchActive) return;

            bool isMyTurn = PitStriker.Networking.Client.CloudMatchManager.Instance.IsMyTurn();
            if (_strikeButton != null)
            {
                _strikeButton.interactable = isMyTurn;
            }

            if (_statusBanner != null)
            {
                _statusBanner.text = isMyTurn ? "★ YOUR TURN! AIM & STRIKE ★" : "OPPONENT'S TURN • WATCHING PLAY";
            }
        }

        private void Start()
        {
            UpdateObjectiveUI();
            HandlePowerChanged(0f);
            HandleStrokeCountChanged(0, 8);
            if (_victoryModal != null) _victoryModal.SetActive(false);
        }

        private void HandleStateChanged(TurnManager.GameState newState)
        {
            if (newState == TurnManager.GameState.TossPhase)
            {
                if (_strikeButton != null)
                {
                    Text btnText = _strikeButton.GetComponentInChildren<Text>();
                    if (btnText != null) btnText.text = "TOSS";
                }
                if (_statusBanner != null)
                {
                    _statusBanner.text = "TOSS PHASE: SWIPE FORWARD TO THROW TO PIT 3!";
                }
                if (_parText != null)
                {
                    _parText.text = "CLOSEST TO PIT 3 PLAYS FIRST";
                }
            }
            else if (newState == TurnManager.GameState.ReadyToAim)
            {
                if (_strikeButton != null)
                {
                    Text btnText = _strikeButton.GetComponentInChildren<Text>();
                    if (btnText != null) btnText.text = "STRIKE";
                }
            }
        }

        private void HandleActivePlayerChanged(TurnManager.PlayerData activePlayer)
        {
            if (activePlayer == null) return;

            if (_playerBadgeImages != null && TurnManager.Instance != null)
                for (int i = 0; i < _playerBadgeImages.Length; i++)
                    if (_playerBadgeImages[i] != null)
                        _playerBadgeImages[i].transform.parent.gameObject.SetActive(i < TurnManager.Instance.PlayerCount);

            // Highlight active player badge; dim others
            if (_playerBadgeGlows != null)
            {
                for (int i = 0; i < _playerBadgeGlows.Length; i++)
                {
                    if (_playerBadgeGlows[i] != null)
                    {
                        bool isActive = (i + 1) == activePlayer.id;
                        _playerBadgeGlows[i].enabled = isActive;
                    }
                }
            }

            if (_playerBadgeImages != null)
            {
                for (int i = 0; i < _playerBadgeImages.Length; i++)
                {
                    if (_playerBadgeImages[i] != null)
                    {
                        bool isActive = (i + 1) == activePlayer.id;
                        Color c = _playerBadgeImages[i].color;
                        c.a = isActive ? 1.0f : 0.45f;
                        _playerBadgeImages[i].color = c;
                    }
                }
            }

            if (_strokeCounterText != null)
            {
                _strokeCounterText.text = $"{activePlayer.name.ToUpper()}: {activePlayer.totalStrokes} STROKES";
            }

            // Disable Strike button during autonomous AI turns or opponent turns
            if (_strikeButton != null)
            {
                bool allowed = !activePlayer.isAI;
                if (NetworkMatchState.Instance != null && NetworkSessionManager.Instance != null && NetworkSessionManager.Instance.ActiveNetworkMode != NetworkSessionManager.NetworkMode.None)
                {
                    allowed = allowed && NetworkMatchState.Instance.IsMyTurn();
                }
                _strikeButton.interactable = allowed;
            }
        }

        private void HandleSliderValueChanged(float val)
        {
            if (_powerFillImage != null)
            {
                _powerFillImage.color = Color.Lerp(new Color(0f, 0.85f, 1f, 1f), new Color(1f, 0.4f, 0f, 1f), val);
            }

            if (_powerLabel != null)
            {
                _powerLabel.text = val > 0.01f ? $"Power: {Mathf.RoundToInt(val * 100f)}%" : "Power: 0%";
            }
        }

        private void HandleStrikeClicked()
        {
            if (TurnManager.Instance != null && TurnManager.Instance.ActivePlayer != null && TurnManager.Instance.ActivePlayer.isAI)
            {
                return; // Guard against clicking strike during AI turn
            }

            if (NetworkSessionManager.Instance != null && NetworkSessionManager.Instance.ActiveNetworkMode != NetworkSessionManager.NetworkMode.None)
            {
                if (NetworkMatchState.Instance != null && !NetworkMatchState.Instance.IsMyTurn())
                {
                    return; // Guard against clicking strike during opponent turn
                }
            }

            if (SwipeLaunchController.Instance != null)
            {
                float p = _powerSlider != null && _powerSlider.value > 0.05f ? _powerSlider.value : 0.65f;
                SwipeLaunchController.Instance.LaunchStrike(p);
            }
        }

        private void HandlePlayAgainClicked()
        {
            if (_victoryModal != null)
            {
                _victoryModal.SetActive(false);
            }

            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.RestartMatch();
            }
        }

        private void HandleHomeMenuClicked()
        {
            if (_victoryModal != null)
            {
                _victoryModal.SetActive(false);
            }

            if (MenuManager.Instance != null)
            {
                MenuManager.Instance.HandleHomeClicked();
            }
            else if (TurnManager.Instance != null)
            {
                TurnManager.Instance.ReturnToMainMenu();
            }
        }

        private void HandlePowerChanged(float power01)
        {
            if (_powerSlider != null)
            {
                _powerSlider.SetValueWithoutNotify(power01);
            }

            if (_powerFillImage != null)
            {
                // Shift color from bright cyan to intense fiery orange at full power
                _powerFillImage.color = Color.Lerp(new Color(0f, 0.85f, 1f, 1f), new Color(1f, 0.4f, 0f, 1f), power01);
            }

            if (_powerLabel != null)
            {
                _powerLabel.text = power01 > 0.01f ? $"Power: {Mathf.RoundToInt(power01 * 100f)}%" : "Power: 0%";
            }
        }

        private void HandleStrokeCountChanged(int pitStrokes, int totalStrokes)
        {
            if (_strokeCounterText != null)
            {
                _strokeCounterText.text = $"STROKES: {totalStrokes}";
            }

            if (_parText != null && TurnManager.Instance != null)
            {
                _parText.text = $"COURSE PAR: {TurnManager.Instance.CoursePar}  (PIT {TurnManager.Instance.CurrentTargetPit}: PAR {TurnManager.Instance.CurrentPitPar})";
            }
        }

        private void HandleTargetPitChanged(int newTargetPit)
        {
            _currentObjectivePit = newTargetPit;
            UpdateObjectiveUI();

            if (_parText != null && TurnManager.Instance != null)
            {
                _parText.text = $"COURSE PAR: {TurnManager.Instance.CoursePar}  (PIT {TurnManager.Instance.CurrentTargetPit}: PAR {TurnManager.Instance.CurrentPitPar})";
            }
        }

        private void HandleStatusMessage(string message)
        {
            if (_statusBanner != null)
            {
                _statusBanner.text = message;
            }
        }

        private void HandleMatchVictory(TurnManager.PlayerData winner, List<TurnManager.PlayerData> leaderboard)
        {
            Time.timeScale = 1f;

            if (_victoryModal != null)
            {
                _victoryModal.SetActive(true);
                _victoryModal.transform.SetAsLastSibling();

                // Ensure top-level sorting order so modal is never blocked
                Canvas modalCanvas = _victoryModal.GetComponent<Canvas>();
                if (modalCanvas == null) modalCanvas = _victoryModal.AddComponent<Canvas>();
                modalCanvas.overrideSorting = true;
                modalCanvas.sortingOrder = 100;
                if (_victoryModal.GetComponent<GraphicRaycaster>() == null)
                {
                    _victoryModal.AddComponent<GraphicRaycaster>();
                }

                // Ensure Play Again button is configured and positioned on left side
                if (_playAgainButton != null)
                {
                    RectTransform paRect = _playAgainButton.GetComponent<RectTransform>();
                    if (paRect != null)
                    {
                        paRect.anchorMin = new Vector2(0.06f, 0.04f);
                        paRect.anchorMax = new Vector2(0.48f, 0.16f);
                        paRect.sizeDelta = Vector2.zero;
                        paRect.anchoredPosition = Vector2.zero;
                    }

                    _playAgainButton.onClick.RemoveAllListeners();
                    _playAgainButton.onClick.AddListener(HandlePlayAgainClicked);
                }

                // Ensure Main Menu button exists on the right side
                Transform existingHomeBtn = _victoryModal.transform.Find("Btn_VictoryHome");
                if (existingHomeBtn == null)
                {
                    GameObject homeBtnObj = new GameObject("Btn_VictoryHome");
                    homeBtnObj.transform.SetParent(_victoryModal.transform, false);
                    RectTransform homeRect = homeBtnObj.AddComponent<RectTransform>();
                    homeRect.anchorMin = new Vector2(0.52f, 0.04f);
                    homeRect.anchorMax = new Vector2(0.94f, 0.16f);
                    homeRect.sizeDelta = Vector2.zero;
                    homeRect.anchoredPosition = Vector2.zero;

                    Image homeImg = homeBtnObj.AddComponent<Image>();
                    homeImg.color = new Color(0.15f, 0.45f, 0.85f, 1f);

                    Button homeBtn = homeBtnObj.AddComponent<Button>();
                    homeBtn.onClick.AddListener(HandleHomeMenuClicked);
                    _homeMenuButton = homeBtn;

                    GameObject textObj = new GameObject("Text");
                    textObj.transform.SetParent(homeBtnObj.transform, false);
                    RectTransform textRect = textObj.AddComponent<RectTransform>();
                    textRect.anchorMin = Vector2.zero;
                    textRect.anchorMax = Vector2.one;
                    textRect.sizeDelta = Vector2.zero;
                    textRect.anchoredPosition = Vector2.zero;

                    Text homeText = textObj.AddComponent<Text>();
                    homeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    homeText.fontSize = 18;
                    homeText.fontStyle = FontStyle.Bold;
                    homeText.alignment = TextAnchor.MiddleCenter;
                    homeText.color = Color.white;
                    homeText.text = "MAIN MENU";
                    homeText.raycastTarget = false;
                }
                else
                {
                    Button hb = existingHomeBtn.GetComponent<Button>();
                    if (hb != null)
                    {
                        hb.onClick.RemoveAllListeners();
                        hb.onClick.AddListener(HandleHomeMenuClicked);
                        _homeMenuButton = hb;
                    }
                }
            }
            else
            {
                CreateRuntimeVictoryModal(winner, leaderboard);
                if (_victoryModal != null) _victoryModal.transform.SetAsLastSibling();
            }

            if (_victoryStrokesText != null && winner != null)
            {
                _victoryStrokesText.text = $"★ {winner.name.ToUpper()} WINS! ★\nTOTAL STROKES: {winner.totalStrokes}";
            }

            if (_victoryParText != null)
            {
                _victoryParText.text = $"COURSE PAR: 8";
            }

            if (_victoryRatingText != null && leaderboard != null)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                for (int i = 0; i < leaderboard.Count; i++)
                {
                    string medal = i == 0 ? "🥇" : (i == 1 ? "🥈" : (i == 2 ? "🥉" : "  "));
                    sb.AppendLine($"{medal} {leaderboard[i].name}: {leaderboard[i].totalStrokes} strokes");
                }
                _victoryRatingText.text = sb.ToString();
            }
        }

        private void CreateRuntimeVictoryModal(TurnManager.PlayerData winner, List<TurnManager.PlayerData> leaderboard)
        {
            GameObject modalObj = new GameObject("Victory_Modal_Runtime");
            modalObj.transform.SetParent(transform, false);
            RectTransform modalRect = modalObj.AddComponent<RectTransform>();
            modalRect.anchorMin = new Vector2(0.5f, 0.5f);
            modalRect.anchorMax = new Vector2(0.5f, 0.5f);
            modalRect.pivot = new Vector2(0.5f, 0.5f);
            modalRect.anchoredPosition = Vector2.zero;
            modalRect.sizeDelta = new Vector2(540f, 380f);

            Canvas modalCanvas = modalObj.AddComponent<Canvas>();
            modalCanvas.overrideSorting = true;
            modalCanvas.sortingOrder = 100;
            modalObj.AddComponent<GraphicRaycaster>();

            Image modalBg = modalObj.AddComponent<Image>();
            modalBg.color = new Color(0.04f, 0.06f, 0.1f, 0.96f);

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(modalObj.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.75f);
            titleRect.anchorMax = new Vector2(1f, 0.98f);
            titleRect.sizeDelta = Vector2.zero;
            Text title = titleObj.AddComponent<Text>();
            title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            title.fontSize = 26;
            title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.MiddleCenter;
            title.color = new Color(1f, 0.85f, 0.2f, 1f);
            title.text = winner != null ? $"★ {winner.name.ToUpper()} WINS! ★" : "★ MATCH VICTORY! ★";

            // Leaderboard Text
            GameObject statsObj = new GameObject("Leaderboard");
            statsObj.transform.SetParent(modalObj.transform, false);
            RectTransform statsRect = statsObj.AddComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0.08f, 0.24f);
            statsRect.anchorMax = new Vector2(0.92f, 0.74f);
            statsRect.sizeDelta = Vector2.zero;
            Text stats = statsObj.AddComponent<Text>();
            stats.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            stats.fontSize = 18;
            stats.fontStyle = FontStyle.Bold;
            stats.alignment = TextAnchor.MiddleCenter;
            stats.color = Color.white;

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            if (leaderboard != null)
            {
                for (int i = 0; i < leaderboard.Count; i++)
                {
                    string medal = i == 0 ? "🥇" : (i == 1 ? "🥈" : (i == 2 ? "🥉" : "   "));
                    sb.AppendLine($"{medal} {leaderboard[i].name} — {leaderboard[i].totalStrokes} Strokes (Pit {leaderboard[i].currentPit})");
                }
            }
            stats.text = sb.ToString();

            // Play Again Button (Left)
            GameObject btnObj = new GameObject("PlayAgainBtn");
            btnObj.transform.SetParent(modalObj.transform, false);
            RectTransform btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.08f, 0.05f);
            btnRect.anchorMax = new Vector2(0.48f, 0.20f);
            btnRect.sizeDelta = Vector2.zero;
            Image btnImg = btnObj.AddComponent<Image>();
            btnImg.color = new Color(0f, 0.8f, 0.4f, 1f);
            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                Destroy(modalObj);
                if (TurnManager.Instance != null) TurnManager.Instance.RestartMatch();
            });

            GameObject btnTextObj = new GameObject("Text");
            btnTextObj.transform.SetParent(btnObj.transform, false);
            RectTransform btnTextRect = btnTextObj.AddComponent<RectTransform>();
            btnTextRect.anchorMin = Vector2.zero;
            btnTextRect.anchorMax = Vector2.one;
            btnTextRect.sizeDelta = Vector2.zero;
            Text btnText = btnTextObj.AddComponent<Text>();
            btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            btnText.fontSize = 17;
            btnText.fontStyle = FontStyle.Bold;
            btnText.alignment = TextAnchor.MiddleCenter;
            btnText.color = Color.white;
            btnText.text = "PLAY AGAIN";

            // Main Menu Button (Right)
            GameObject homeBtnObj = new GameObject("HomeBtn");
            homeBtnObj.transform.SetParent(modalObj.transform, false);
            RectTransform homeBtnRect = homeBtnObj.AddComponent<RectTransform>();
            homeBtnRect.anchorMin = new Vector2(0.52f, 0.05f);
            homeBtnRect.anchorMax = new Vector2(0.92f, 0.20f);
            homeBtnRect.sizeDelta = Vector2.zero;
            Image homeBtnImg = homeBtnObj.AddComponent<Image>();
            homeBtnImg.color = new Color(0.15f, 0.45f, 0.85f, 1f);
            Button homeBtn = homeBtnObj.AddComponent<Button>();
            homeBtn.onClick.AddListener(() =>
            {
                Destroy(modalObj);
                if (MenuManager.Instance != null) MenuManager.Instance.HandleHomeClicked();
                else if (TurnManager.Instance != null) TurnManager.Instance.ReturnToMainMenu();
            });

            GameObject homeTextObj = new GameObject("Text");
            homeTextObj.transform.SetParent(homeBtnObj.transform, false);
            RectTransform homeTextRect = homeTextObj.AddComponent<RectTransform>();
            homeTextRect.anchorMin = Vector2.zero;
            homeTextRect.anchorMax = Vector2.one;
            homeTextRect.sizeDelta = Vector2.zero;
            Text homeText = homeTextObj.AddComponent<Text>();
            homeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            homeText.fontSize = 17;
            homeText.fontStyle = FontStyle.Bold;
            homeText.alignment = TextAnchor.MiddleCenter;
            homeText.color = Color.white;
            homeText.text = "MAIN MENU";

            _victoryModal = modalObj;
        }

        private void UpdateObjectiveUI()
        {
            Color activeColor = new Color(0f, 0.85f, 1f, 1f);
            Color completedColor = new Color(0.2f, 0.9f, 0.3f, 1f);
            Color lockedColor = new Color(0.3f, 0.3f, 0.35f, 0.6f);

            if (_bead1Image != null) _bead1Image.color = _currentObjectivePit > 1 ? completedColor : (_currentObjectivePit == 1 ? activeColor : lockedColor);
            if (_bead2Image != null) _bead2Image.color = _currentObjectivePit > 2 ? completedColor : (_currentObjectivePit == 2 ? activeColor : lockedColor);
            if (_bead3Image != null) _bead3Image.color = _currentObjectivePit > 3 ? completedColor : (_currentObjectivePit == 3 ? activeColor : lockedColor);
        }
    }
}
