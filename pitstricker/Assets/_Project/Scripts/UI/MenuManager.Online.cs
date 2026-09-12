using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using PitStriker.Networking;
using PitStriker.Gameplay;

namespace PitStriker.UI
{
    public partial class MenuManager
    {
        // =========================================================================
        // PHASE 2 — ONLINE MULTIPLAYER UI & SESSION ORCHESTRATION
        // =========================================================================

        [Header("Phase 2 Online Panels")]
        [SerializeField] private GameObject _playModePanel;
        [SerializeField] private GameObject _onlineMenuPanel;
        [SerializeField] private GameObject _createMatchPanel;
        [SerializeField] private GameObject _joinMatchPanel;
        [SerializeField] private GameObject _onlineLobbyPanel;
        [SerializeField] private GameObject _onlineNoticeModal;
        [SerializeField] private GameObject _onlineResultPanel;
        [SerializeField] private GameObject _quickMatchPanel;

        // Play Mode Screen Controls
        private Button _btnPlayLocal;
        private Button _btnPlayOnline;
        private Button _btnBackFromPlayMode;

        // Online Menu Controls
        private Button _btnQuickMatch;
        private Button _btnCreatePrivateMatch;
        private Button _btnJoinPrivateMatch;
        private Button _btnBackFromOnlineMenu;

        // Create Private Match Controls & Displays
        private Text _txtHostP1Name;
        private Text _txtHostP1Status;
        private Text _txtHostP2Name;
        private Text _txtHostP2Status;
        private Text _txtCreateJoinCode;
        private Text _txtCreateStatusBanner;
        private Button _btnCopyJoinCode;
        private Text _txtCopyFeedback;
        private Button _btnCancelCreateMatch;

        // Join Private Match Controls & Displays
        private InputField _inputJoinCode;
        private Text _txtJoinErrorFeedback;
        private Button _btnSubmitJoin;
        private Button _btnBackFromJoin;

        // Online Lobby (Two-Player Readiness) Controls & Displays
        private Text _txtLobbyTitle;
        private Text _txtLobbyP1Header;
        private Text _txtLobbyP1Details;
        private Text _txtLobbyP2Header;
        private Text _txtLobbyP2Details;
        private Text _txtLobbyCountdown;
        private Button _btnLeaveLobby;

        // Online Notice Modal (Error Handling)
        private Text _txtNoticeTitle;
        private Text _txtNoticeBody;
        private Button _btnCloseNotice;

        // Runtime State
        private Coroutine _waitingPulseCoroutine;
        private Coroutine _lobbyCountdownCoroutine;
        private Coroutine _copyFeedbackCoroutine;
        private string _activeSessionCode = string.Empty;
        private bool _isOnlineTransitioning = false;

        // Phase 5 Result Screen references
        private Text _txtResultHeadline;
        private Text _txtResultSubtitle;
        private Text _txtResultLocalStats;
        private Text _txtResultOpponentStats;
        private Button _btnResultRematch;
        private Button _btnResultMainMenu;
        private Text _txtResultRematchStatus;
        private bool _localWantsRematch = false;

        // Phase 6 Disconnect Overlay references
        private GameObject _disconnectOverlay;
        private Text _txtDisconnectTitle;
        private Text _txtDisconnectCountdown;
        private Button _btnDisconnectLeave;
        private Coroutine _reconnectedToastCoroutine;

        private void InitOnlineSessionListeners()
        {
            if (NetworkSessionManager.Instance != null)
            {
                NetworkSessionManager.Instance.OnJoinCodeReady -= HandleJoinCodeReady;
                NetworkSessionManager.Instance.OnJoinCodeReady += HandleJoinCodeReady;

                NetworkSessionManager.Instance.OnPlayerJoined -= HandleOnlinePlayerJoined;
                NetworkSessionManager.Instance.OnPlayerJoined += HandleOnlinePlayerJoined;

                NetworkSessionManager.Instance.OnPlayerLeft -= HandleOnlinePlayerLeft;
                NetworkSessionManager.Instance.OnPlayerLeft += HandleOnlinePlayerLeft;

                NetworkSessionManager.Instance.OnSessionError -= HandleOnlineSessionError;
                NetworkSessionManager.Instance.OnSessionError += HandleOnlineSessionError;

                NetworkSessionManager.Instance.OnStateChanged -= HandleOnlineStateChanged;
                NetworkSessionManager.Instance.OnStateChanged += HandleOnlineStateChanged;
            }

            // Phase 5: subscribe to network match completion & synchronized start
            NetworkMatchState.OnMatchCompletedNetworkEvent -= HandleNetworkMatchCompleted;
            NetworkMatchState.OnMatchCompletedNetworkEvent += HandleNetworkMatchCompleted;
            NetworkMatchState.OnMatchStartedClientEvent   -= HandleNetworkMatchStarted;
            NetworkMatchState.OnMatchStartedClientEvent   += HandleNetworkMatchStarted;

            // Phase 6: subscribe to grace period events
            DisconnectGracePeriodManager.OnGracePeriodStarted -= HandleGracePeriodStarted;
            DisconnectGracePeriodManager.OnGracePeriodStarted += HandleGracePeriodStarted;
            DisconnectGracePeriodManager.OnGracePeriodTick    -= HandleGracePeriodTick;
            DisconnectGracePeriodManager.OnGracePeriodTick    += HandleGracePeriodTick;
            DisconnectGracePeriodManager.OnGracePeriodExpired -= HandleGracePeriodExpiredUI;
            DisconnectGracePeriodManager.OnGracePeriodExpired += HandleGracePeriodExpiredUI;
            DisconnectGracePeriodManager.OnOpponentReturned   -= HandleOpponentReturnedUI;
            DisconnectGracePeriodManager.OnOpponentReturned   += HandleOpponentReturnedUI;

            // Phase 7: subscribe to Quick Match events
            QuickMatchManager.OnSearching  -= HandleQuickMatchSearching;
            QuickMatchManager.OnSearching  += HandleQuickMatchSearching;
            QuickMatchManager.OnMatchFound -= HandleQuickMatchFound;
            QuickMatchManager.OnMatchFound += HandleQuickMatchFound;
            QuickMatchManager.OnTimeout    -= HandleQuickMatchTimeout;
            QuickMatchManager.OnTimeout    += HandleQuickMatchTimeout;
            QuickMatchManager.OnCancelled  -= HandleQuickMatchCancelled;
            QuickMatchManager.OnCancelled  += HandleQuickMatchCancelled;
            QuickMatchManager.OnError      -= HandleQuickMatchError;
            QuickMatchManager.OnError      += HandleQuickMatchError;

            InitCloudSessionListeners();
        }

        private void EnsureCloudClient()
        {
            if (PitStriker.Networking.Client.CloudNetworkClient.Instance == null)
            {
                var go = new GameObject("CloudNetworkClient");
                go.AddComponent<PitStriker.Networking.Client.CloudNetworkClient>();
            }
            if (PitStriker.Networking.Client.CloudMatchManager.Instance == null)
            {
                var go = new GameObject("CloudMatchManager");
                go.AddComponent<PitStriker.Networking.Client.CloudMatchManager>();
            }
            InitCloudSessionListeners();
        }

        private void InitCloudSessionListeners()
        {
            var client = PitStriker.Networking.Client.CloudNetworkClient.Instance;
            if (client != null)
            {
                client.OnRoomCreated -= HandleCloudRoomCreated;
                client.OnRoomCreated += HandleCloudRoomCreated;

                client.OnRoomJoined -= HandleCloudRoomJoined;
                client.OnRoomJoined += HandleCloudRoomJoined;

                client.OnOpponentJoined -= HandleCloudOpponentJoined;
                client.OnOpponentJoined += HandleCloudOpponentJoined;

                client.OnLobbyCountdownTick -= HandleCloudCountdownTick;
                client.OnLobbyCountdownTick += HandleCloudCountdownTick;

                client.OnMatchStarted -= HandleCloudMatchStarted;
                client.OnMatchStarted += HandleCloudMatchStarted;

                client.OnOpponentDisconnected -= HandleGracePeriodStarted;
                client.OnOpponentDisconnected += HandleGracePeriodStarted;

                client.OnOpponentReconnected -= HandleOpponentReturnedUI;
                client.OnOpponentReconnected += HandleOpponentReturnedUI;

                client.OnMatchAbandoned -= HandleCloudMatchAbandoned;
                client.OnMatchAbandoned += HandleCloudMatchAbandoned;

                client.OnClientError -= HandleCloudError;
                client.OnClientError += HandleCloudError;
            }

            PitStriker.Networking.Client.CloudMatchManager.OnMatchCompletedEvent -= HandleNetworkMatchCompleted;
            PitStriker.Networking.Client.CloudMatchManager.OnMatchCompletedEvent += HandleNetworkMatchCompleted;
            PitStriker.Networking.Client.CloudMatchManager.OnRematchReadyEvent -= HandleCloudRematchReady;
            PitStriker.Networking.Client.CloudMatchManager.OnRematchReadyEvent += HandleCloudRematchReady;
        }

        private void HandleCloudRoomCreated(string code)
        {
            _activeSessionCode = code;
            if (_txtCreateJoinCode != null) _txtCreateJoinCode.text = code;
            if (_txtCreateStatusBanner != null) _txtCreateStatusBanner.text = "Room ready! Waiting for Player 2...";
            StopWaitingPulseAnimation();
        }

        private void HandleCloudRoomJoined(string code)
        {
            _activeSessionCode = code;
            ShowScreen(ScreenType.OnlineLobby);
            UpdateOnlineLobbyUI();
            if (_txtLobbyCountdown != null) _txtLobbyCountdown.text = "Connected to room! Starting soon...";
        }

        private void HandleCloudOpponentJoined(string opponentName)
        {
            if (CurrentScreen == ScreenType.CreateMatch)
            {
                if (_txtHostP2Name != null) _txtHostP2Name.text = $"{opponentName} (CONNECTED)";
                if (_txtHostP2Status != null)
                {
                    _txtHostP2Status.text = "Status: Online • Ready";
                    _txtHostP2Status.color = new Color(.25f, .95f, .55f);
                }
            }
            ShowScreen(ScreenType.OnlineLobby);
            UpdateOnlineLobbyUI();
        }

        private void HandleCloudCountdownTick(int seconds)
        {
            if (_txtLobbyCountdown != null)
            {
                _txtLobbyCountdown.text = $"MATCH STARTING IN {seconds}...";
            }
        }

        private void HandleCloudMatchStarted(string code, string p1, string p2)
        {
            _isOnlineTransitioning = false;
            StopWaitingPulseAnimation();
            ShowScreen(ScreenType.InGame);
        }

        private void HandleCloudMatchAbandoned(int winnerIdx, string reason)
        {
            HideDisconnectOverlay();
            HandleNetworkMatchCompleted(winnerIdx);
        }

        private void HandleCloudError(string error)
        {
            StopWaitingPulseAnimation();
            ShowOnlineNotice("SESSION NOTICE", error);
        }

        private void HandleCloudRematchReady()
        {
            _localWantsRematch = false;
            ShowScreen(ScreenType.InGame);
        }

        private void BuildOnlineUI()
        {
            BuildPlayModeSelectPage();
            BuildOnlineMenuPage();
            BuildCreateMatchPage();
            BuildJoinMatchPage();
            BuildOnlineLobbyPage();
            BuildOnlineResultPage();
            BuildQuickMatchPage();
            BuildDisconnectOverlay();
            BuildOnlineNoticeModal();
            InitOnlineSessionListeners();
        }

        // -------------------------------------------------------------------------
        // TASK 1: Play Mode Selection Screen (PLAY -> LOCAL vs ONLINE)
        // -------------------------------------------------------------------------
        private void BuildPlayModeSelectPage()
        {
            _playModePanel = Page("PlayModeSelect");

            // Header Section
            Label(_playModePanel.transform, "SELECT PLAY MODE", 0, 268, 850, 44, 30, Cream);
            Label(_playModePanel.transform, "Choose between local device play or connecting with an opponent online.", 0, 234, 940, 26, 16, new Color(.72f, .83f, .80f));

            // Card 1: Local Play (Pass & Play / AI Bots)
            var localCard = Box("Card_LocalPlay", _playModePanel.transform, -250, 6, 460, 410, Card);
            Box("LocalAccent", localCard.transform, 0, 201, 460, 6, Gold);
            Label(localCard.transform, "LOCAL PLAY", 0, 165, 420, 26, 14, Gold);
            Label(localCard.transform, "PASS & PLAY / AI BOTS", 0, 132, 420, 36, 24, Cream);
            Label(localCard.transform, "2 TO 4 PLAYERS  •  ONE DEVICE", 0, 100, 420, 22, 13, new Color(.25f, .95f, .55f));

            MultilineLabel(localCard.transform,
                "Experience the traditional village pit game on a single phone or tablet. Play face-to-face with friends or challenge customizable computer AI bots.",
                0, 20, 390, 80, 15, new Color(.75f, .86f, .84f), TextAnchor.UpperLeft);

            // Local feature badges
            var badge1 = Box("Badge1", localCard.transform, 0, -56, 400, 36, Ink);
            Label(badge1.transform, "✔  100% Offline  •  Turn-Based  •  AI Opponents", 0, 0, 390, 22, 13, Cream);

            _btnPlayLocal = ActionButton("Btn_PlayLocal", localCard.transform, "PLAY LOCAL", 0, -140, 380, 62, Gold);
            _btnPlayLocal.onClick.AddListener(HandlePlayLocalSelected);

            // Card 2: Online Multiplayer (1v1 Matchmaking & Private Matches)
            var onlineCard = Box("Card_OnlinePlay", _playModePanel.transform, 250, 6, 460, 410, Card);
            Box("OnlineAccent", onlineCard.transform, 0, 201, 460, 6, Green);
            Label(onlineCard.transform, "ONLINE MULTIPLAYER", 0, 165, 420, 26, 14, Green);
            Label(onlineCard.transform, "1v1 REAL-TIME MATCH", 0, 132, 420, 36, 24, Cream);
            Label(onlineCard.transform, "PRIVATE ROOMS  •  UNITY RELAY", 0, 100, 420, 22, 13, Gold);

            MultilineLabel(onlineCard.transform,
                "Compete against remote players over Wi-Fi or mobile data. Host private rooms with shareable 6-digit Join Codes, or jump directly into quick sessions.",
                0, 20, 390, 80, 15, new Color(.75f, .86f, .84f), TextAnchor.UpperLeft);

            var badge2 = Box("Badge2", onlineCard.transform, 0, -56, 400, 36, Ink);
            Label(badge2.transform, "⚡  Low Latency  •  Room Codes  •  Head-to-Head", 0, 0, 390, 22, 13, Cream);

            _btnPlayOnline = ActionButton("Btn_PlayOnline", onlineCard.transform, "PLAY ONLINE", 0, -140, 380, 62, Green);
            _btnPlayOnline.onClick.AddListener(HandlePlayOnlineSelected);

            // Back button
            _btnBackFromPlayMode = ActionButton("Btn_BackFromPlayMode", _playModePanel.transform, "BACK", 0, -252, 260, 50, Ink);
            _btnBackFromPlayMode.onClick.AddListener(() =>
            {
                if (NetworkSessionManager.Instance != null)
                {
                    NetworkSessionManager.Instance.ShutdownSession();
                }
                ShowScreen(ScreenType.Home);
            });
        }

        // -------------------------------------------------------------------------
        // TASK 2: Online Menu Screen
        // -------------------------------------------------------------------------
        private void BuildOnlineMenuPage()
        {
            _onlineMenuPanel = Page("OnlineMenu");

            Label(_onlineMenuPanel.transform, "ONLINE MULTIPLAYER", 0, 268, 850, 44, 30, Cream);
            Label(_onlineMenuPanel.transform, "Create a private session with a code, join a friend, or find an opponent.", 0, 234, 940, 26, 16, new Color(.72f, .83f, .80f));

            var hubCard = Box("OnlineHubCard", _onlineMenuPanel.transform, 0, 10, 540, 410, Card);
            Box("HubAccent", hubCard.transform, 0, 201, 540, 6, Green);

            Label(hubCard.transform, "CHOOSE MATCH TYPE", 0, 162, 480, 28, 18, Gold);
            Label(hubCard.transform, "Two players  •  Official Pit Striker rules", 0, 134, 480, 22, 13, new Color(.72f, .83f, .80f));

            _btnQuickMatch = ActionButton("Btn_OnlineQuickMatch", hubCard.transform, "QUICK MATCH", 0, 72, 450, 62, Gold);
            _btnQuickMatch.onClick.AddListener(HandleQuickMatchClicked);

            _btnCreatePrivateMatch = ActionButton("Btn_OnlineCreateMatch", hubCard.transform, "CREATE PRIVATE MATCH", 0, -2, 450, 62, Green);
            _btnCreatePrivateMatch.onClick.AddListener(HandleCreatePrivateMatchClicked);

            _btnJoinPrivateMatch = ActionButton("Btn_OnlineJoinMatch", hubCard.transform, "JOIN PRIVATE MATCH", 0, -76, 450, 62, Card);
            // Add subtle border to the Join button
            var joinOutline = _btnJoinPrivateMatch.gameObject.AddComponent<Outline>();
            joinOutline.effectColor = Gold;
            joinOutline.effectDistance = new Vector2(1.5f, -1.5f);
            _btnJoinPrivateMatch.onClick.AddListener(HandleJoinPrivateMatchClicked);

            _btnBackFromOnlineMenu = ActionButton("Btn_OnlineBack", hubCard.transform, "BACK", 0, -150, 450, 52, Ink);
            _btnBackFromOnlineMenu.onClick.AddListener(() =>
            {
                if (NetworkSessionManager.Instance != null)
                {
                    NetworkSessionManager.Instance.ShutdownSession();
                }
                ShowScreen(ScreenType.PlayModeSelect);
            });

            // Status pill at bottom
            var statusPill = Box("OnlineStatusPill", _onlineMenuPanel.transform, 0, -252, 600, 36, Ink);
            Label(statusPill.transform, "UNITY MULTIPLAYER SERVICES  •  READY", 0, 0, 580, 22, 13, new Color(.25f, .95f, .55f));
        }

        // -------------------------------------------------------------------------
        // TASK 3: Create Private Match Screen (Host Flow)
        // -------------------------------------------------------------------------
        private void BuildCreateMatchPage()
        {
            _createMatchPanel = Page("CreateMatch");

            Label(_createMatchPanel.transform, "CREATE PRIVATE MATCH", 0, 268, 850, 44, 30, Cream);
            Label(_createMatchPanel.transform, "Share your Join Code with an opponent to start a 1v1 match.", 0, 234, 940, 26, 16, new Color(.72f, .83f, .80f));

            // Left Card: Player Roster & Connection Status (Task 5)
            var rosterCard = Box("CreateRosterCard", _createMatchPanel.transform, -240, 10, 460, 400, Card);
            Box("RosterAccent", rosterCard.transform, 0, 196, 460, 6, Gold);
            Label(rosterCard.transform, "CONNECTED PLAYERS", 0, 162, 420, 26, 16, Gold);

            // Player 1 (Host - You)
            var p1Box = Box("P1_HostBox", rosterCard.transform, 0, 80, 410, 96, Ink);
            Box("P1_ColorStripe", p1Box.transform, -195, 0, 8, 96, new Color(.15f, .65f, 1f));
            _txtHostP1Name = Label(p1Box.transform, "PLAYER 1 (HOST - YOU)", -20, 22, 320, 28, 17, Cream);
            _txtHostP1Status = Label(p1Box.transform, "Status: Online • Ready", -20, -14, 320, 24, 14, new Color(.25f, .95f, .55f));

            // Player 2 (Opponent)
            var p2Box = Box("P2_OpponentBox", rosterCard.transform, 0, -42, 410, 96, Ink);
            Box("P2_ColorStripe", p2Box.transform, -195, 0, 8, 96, new Color(1f, .3f, .25f));
            _txtHostP2Name = Label(p2Box.transform, "PLAYER 2 (OPPONENT)", -20, 22, 320, 28, 17, Cream);
            _txtHostP2Status = Label(p2Box.transform, "Waiting for opponent to connect...", -20, -14, 320, 24, 14, Gold);

            _txtCreateStatusBanner = Label(rosterCard.transform, "Waiting for opponent...", 0, -140, 410, 32, 15, Cream);

            // Right Card: Join Code & Room Controls
            var codeCard = Box("CreateCodeCard", _createMatchPanel.transform, 240, 10, 460, 400, Card);
            Box("CodeAccent", codeCard.transform, 0, 196, 460, 6, Green);
            Label(codeCard.transform, "ROOM JOIN CODE", 0, 162, 420, 26, 16, Green);
            Label(codeCard.transform, "Have your opponent enter this 6-digit code:", 0, 130, 420, 22, 13, new Color(.72f, .83f, .80f));

            // Giant Join Code Display Box
            var bigCodeBox = Box("BigCodeBox", codeCard.transform, 0, 52, 380, 88, Ink);
            var bigCodeOutline = bigCodeBox.AddComponent<Outline>();
            bigCodeOutline.effectColor = Gold;
            bigCodeOutline.effectDistance = new Vector2(2, -2);
            _txtCreateJoinCode = Label(bigCodeBox.transform, "CREATING...", 0, 0, 370, 70, 44, Gold);

            _btnCopyJoinCode = ActionButton("Btn_CopyJoinCode", codeCard.transform, "COPY JOIN CODE", 0, -26, 380, 52, Green);
            _btnCopyJoinCode.onClick.AddListener(HandleCopyJoinCodeClicked);

            _txtCopyFeedback = Label(codeCard.transform, "", 0, -66, 380, 22, 13, new Color(.25f, .95f, .55f));

            _btnCancelCreateMatch = ActionButton("Btn_CancelCreateMatch", codeCard.transform, "CANCEL", 0, -135, 380, 56, Ink);
            _btnCancelCreateMatch.onClick.AddListener(HandleCancelOnlineMatch);
        }

        // -------------------------------------------------------------------------
        // TASK 4: Join Private Match Screen (Client Flow)
        // -------------------------------------------------------------------------
        private void BuildJoinMatchPage()
        {
            _joinMatchPanel = Page("JoinMatch");

            Label(_joinMatchPanel.transform, "JOIN PRIVATE MATCH", 0, 268, 850, 44, 30, Cream);
            Label(_joinMatchPanel.transform, "Enter the 6-character room code shared by your opponent.", 0, 234, 940, 26, 16, new Color(.72f, .83f, .80f));

            var formCard = Box("JoinFormCard", _joinMatchPanel.transform, 0, 10, 520, 410, Card);
            Box("JoinAccent", formCard.transform, 0, 201, 520, 6, Gold);

            Label(formCard.transform, "ENTER JOIN CODE", 0, 150, 460, 30, 20, Gold);
            Label(formCard.transform, "6-character alphanumeric code (e.g. ABC123)", 0, 118, 460, 22, 13, new Color(.72f, .83f, .80f));

            // Input Field
            _inputJoinCode = CreateCustomInputField("Input_JoinCode", formCard.transform, "XXXXXX", 0, 42, 380, 72);

            // Error / Validation feedback
            _txtJoinErrorFeedback = Label(formCard.transform, "", 0, -18, 460, 26, 14, new Color(1f, .35f, .35f));

            _btnSubmitJoin = ActionButton("Btn_SubmitJoin", formCard.transform, "JOIN MATCH", 0, -78, 380, 60, Green);
            _btnSubmitJoin.onClick.AddListener(HandleSubmitJoinClicked);

            _btnBackFromJoin = ActionButton("Btn_BackFromJoin", formCard.transform, "BACK", 0, -150, 380, 52, Ink);
            _btnBackFromJoin.onClick.AddListener(() =>
            {
                if (NetworkSessionManager.Instance != null)
                {
                    NetworkSessionManager.Instance.ShutdownSession();
                }
                ShowScreen(ScreenType.OnlineMenu);
            });
        }

        // -------------------------------------------------------------------------
        // TASK 6 & 7: Two-Player Readiness & Transition Screen
        // -------------------------------------------------------------------------
        private void BuildOnlineLobbyPage()
        {
            _onlineLobbyPanel = Page("OnlineLobby");

            _txtLobbyTitle = Label(_onlineLobbyPanel.transform, "MATCH READY!", 0, 268, 850, 44, 30, Cream);
            Label(_onlineLobbyPanel.transform, "Both players connected. Initializing village pitch...", 0, 234, 940, 26, 16, new Color(.72f, .83f, .80f));

            var centerCard = Box("LobbyCenterCard", _onlineLobbyPanel.transform, 0, 15, 680, 390, Card);
            Box("LobbyAccent", centerCard.transform, 0, 191, 680, 6, Green);

            // P1 Slot Card
            var p1LobbyBox = Box("P1_LobbyBox", centerCard.transform, -155, 70, 290, 140, Ink);
            Box("P1_Indicator", p1LobbyBox.transform, 0, 66, 290, 6, new Color(.15f, .65f, 1f));
            _txtLobbyP1Header = Label(p1LobbyBox.transform, "PLAYER 1 (HOST)", 0, 34, 260, 26, 16, Cream);
            _txtLobbyP1Details = Label(p1LobbyBox.transform, "ID: 0 • READY", 0, -6, 260, 24, 14, new Color(.25f, .95f, .55f));
            var p1ReadyBadge = Box("P1_Badge", p1LobbyBox.transform, 0, -42, 160, 28, new Color(.12f, .43f, .34f));
            Label(p1ReadyBadge.transform, "CONNECTED", 0, 0, 150, 20, 12, Cream);

            // VS Divider
            Label(centerCard.transform, "VS", 0, 70, 60, 40, 24, Gold);

            // P2 Slot Card
            var p2LobbyBox = Box("P2_LobbyBox", centerCard.transform, 155, 70, 290, 140, Ink);
            Box("P2_Indicator", p2LobbyBox.transform, 0, 66, 290, 6, new Color(1f, .3f, .25f));
            _txtLobbyP2Header = Label(p2LobbyBox.transform, "PLAYER 2 (GUEST)", 0, 34, 260, 26, 16, Cream);
            _txtLobbyP2Details = Label(p2LobbyBox.transform, "CONNECTED • READY", 0, -6, 260, 24, 14, new Color(.25f, .95f, .55f));
            var p2ReadyBadge = Box("P2_Badge", p2LobbyBox.transform, 0, -42, 160, 28, new Color(.12f, .43f, .34f));
            Label(p2ReadyBadge.transform, "CONNECTED", 0, 0, 150, 20, 12, Cream);

            // Countdown banner
            _txtLobbyCountdown = Label(centerCard.transform, "Starting match in 2 seconds...", 0, -42, 600, 36, 19, Gold);

            _btnLeaveLobby = ActionButton("Btn_LeaveLobby", centerCard.transform, "LEAVE MATCH", 0, -125, 300, 52, Ink);
            _btnLeaveLobby.onClick.AddListener(HandleCancelOnlineMatch);
        }

        // -------------------------------------------------------------------------
        // TASK 8: Error Handling Modal
        // -------------------------------------------------------------------------
        private void BuildOnlineNoticeModal()
        {
            _onlineNoticeModal = Box("OnlineNoticeModal", transform, 0, 0, 1280, 720, new Color(0, 0, 0, 0.25f));
            _onlineNoticeModal.SetActive(false);

            var dialogBox = Box("NoticeDialog", _onlineNoticeModal.transform, 0, 0, 540, 290, Card);
            Box("DialogAccent", dialogBox.transform, 0, 141, 540, 6, new Color(1f, .35f, .35f));

            _txtNoticeTitle = Label(dialogBox.transform, "NETWORK NOTICE", 0, 95, 480, 34, 22, Gold);
            _txtNoticeBody = MultilineLabel(dialogBox.transform, "A connection notice occurred.", 0, 15, 460, 90, 16, Cream, TextAnchor.MiddleCenter);

            _btnCloseNotice = ActionButton("Btn_CloseNotice", dialogBox.transform, "OK", 0, -85, 200, 50, Gold);
            _btnCloseNotice.onClick.AddListener(CloseOnlineNotice);
        }

        public void ShowOnlineNotice(string title, string message)
        {
            if (_txtNoticeTitle != null) _txtNoticeTitle.text = title;
            if (_txtNoticeBody != null) _txtNoticeBody.text = message;
            if (_onlineNoticeModal != null)
            {
                _onlineNoticeModal.SetActive(true);
                _onlineNoticeModal.transform.SetAsLastSibling();
            }
        }

        public void CloseOnlineNotice()
        {
            if (_onlineNoticeModal != null) _onlineNoticeModal.SetActive(false);
        }

        // -------------------------------------------------------------------------
        // Custom InputField Construction Helper (uGUI / Input System compatible)
        // -------------------------------------------------------------------------
        private InputField CreateCustomInputField(string name, Transform parent, string placeholderStr, float x, float y, float w, float h)
        {
            GameObject inputObj = Box(name, parent, x, y, w, h, new Color(0.04f, 0.10f, 0.10f, 1f));
            Image bg = inputObj.GetComponent<Image>();
            bg.raycastTarget = true;

            var outline = inputObj.AddComponent<Outline>();
            outline.effectColor = Gold;
            outline.effectDistance = new Vector2(2, -2);

            // Child Text (The typed text)
            GameObject textObj = CreateText("Text", inputObj.transform, "", 28, FontStyle.Bold, Cream, TextAnchor.MiddleCenter);
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = new Vector2(-24, 0);
            textRect.anchoredPosition = Vector2.zero;
            Text textComp = textObj.GetComponent<Text>();
            textComp.supportRichText = false;

            // Child Placeholder
            GameObject phObj = CreateText("Placeholder", inputObj.transform, placeholderStr, 26, FontStyle.Italic, new Color(0.45f, 0.58f, 0.58f, 0.85f), TextAnchor.MiddleCenter);
            RectTransform phRect = phObj.GetComponent<RectTransform>();
            phRect.anchorMin = Vector2.zero;
            phRect.anchorMax = Vector2.one;
            phRect.sizeDelta = new Vector2(-24, 0);
            phRect.anchoredPosition = Vector2.zero;
            Text phComp = phObj.GetComponent<Text>();

            InputField field = inputObj.AddComponent<InputField>();
            field.textComponent = textComp;
            field.placeholder = phComp;
            field.characterLimit = 6;
            field.characterValidation = InputField.CharacterValidation.Alphanumeric;
            field.contentType = InputField.ContentType.Standard;
            field.lineType = InputField.LineType.SingleLine;

            field.onValueChanged.AddListener((val) =>
            {
                string upper = val.ToUpperInvariant();
                if (upper != val) field.text = upper;
                if (_txtJoinErrorFeedback != null) _txtJoinErrorFeedback.text = "";
            });

            return field;
        }

        // =========================================================================
        // FLOW & BUTTON EVENT HANDLERS
        // =========================================================================

        private void HandlePlayLocalSelected()
        {
            Debug.Log("[MENU] Local Play selected -> Ensuring clean offline state & opening Match Setup...");
            if (NetworkSessionManager.Instance != null)
            {
                NetworkSessionManager.Instance.ShutdownSession();
            }
            ShowScreen(ScreenType.ChoosePlayers);
        }

        private void HandlePlayOnlineSelected()
        {
            Debug.Log("[MENU] Online Play selected -> Initializing Cloud Client & Opening Online Menu...");
            EnsureCloudClient();
            PitStriker.Networking.Client.CloudNetworkClient.Instance?.Connect();
            MultiplayerAnalytics.TrackOnlineMenuOpened();
            ShowScreen(ScreenType.OnlineMenu);
        }

        private void HandleQuickMatchClicked()
        {
            Debug.Log("[ONLINE] Quick Match clicked -> Requesting cloud matchmaking...");
            ShowScreen(ScreenType.QuickMatch);
            EnsureCloudClient();

            var client = PitStriker.Networking.Client.CloudNetworkClient.Instance;
            if (client != null)
            {
                if (client.IsConnected)
                {
                    client.RequestQuickMatch();
                }
                else
                {
                    client.Connect();
                    Action onConn = null;
                    onConn = () =>
                    {
                        client.OnConnectedToServer -= onConn;
                        client.RequestQuickMatch();
                    };
                    client.OnConnectedToServer += onConn;
                }
            }
        }

        private void HandleCreatePrivateMatchClicked()
        {
            Debug.Log("[ONLINE] Creating private match session via Cloud Server...");
            ShowScreen(ScreenType.CreateMatch);

            _activeSessionCode = string.Empty;
            if (_txtCreateJoinCode != null) _txtCreateJoinCode.text = "CREATING...";
            if (_txtHostP2Status != null) _txtHostP2Status.text = "Waiting for opponent to connect...";
            if (_txtCreateStatusBanner != null) _txtCreateStatusBanner.text = "Contacting Google Cloud dedicated server...";
            if (_txtCopyFeedback != null) _txtCopyFeedback.text = "";

            StartWaitingPulseAnimation();
            EnsureCloudClient();

            var client = PitStriker.Networking.Client.CloudNetworkClient.Instance;
            if (client != null)
            {
                if (client.IsConnected)
                {
                    client.CreateRoom();
                }
                else
                {
                    client.Connect();
                    Action onConn = null;
                    onConn = () =>
                    {
                        client.OnConnectedToServer -= onConn;
                        client.CreateRoom();
                    };
                    client.OnConnectedToServer += onConn;
                }
            }
        }

        private void HandleCopyJoinCodeClicked()
        {
            if (!string.IsNullOrEmpty(_activeSessionCode))
            {
                GUIUtility.systemCopyBuffer = _activeSessionCode;
                if (_txtCopyFeedback != null) _txtCopyFeedback.text = "✔ COPIED TO CLIPBOARD!";
                if (_copyFeedbackCoroutine != null) StopCoroutine(_copyFeedbackCoroutine);
                _copyFeedbackCoroutine = StartCoroutine(ClearCopyFeedbackRoutine());
            }
        }

        private IEnumerator ClearCopyFeedbackRoutine()
        {
            yield return new WaitForSecondsRealtime(2.5f);
            if (_txtCopyFeedback != null) _txtCopyFeedback.text = "";
        }

        private void HandleJoinPrivateMatchClicked()
        {
            Debug.Log("[ONLINE] Join Private Match clicked -> Opening Join form...");
            ShowScreen(ScreenType.JoinMatch);

            if (_inputJoinCode != null)
            {
                _inputJoinCode.text = "";
                _inputJoinCode.interactable = true;
            }
            if (_txtJoinErrorFeedback != null) _txtJoinErrorFeedback.text = "";
            if (_btnSubmitJoin != null) _btnSubmitJoin.interactable = true;
        }

        private async void HandleSubmitJoinClicked()
        {
            string code = _inputJoinCode != null ? _inputJoinCode.text.Trim().ToUpperInvariant() : "";

            // Validation (Task 4)
            if (string.IsNullOrEmpty(code))
            {
                SetJoinError("Please enter a 6-character join code.");
                return;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            bool isLocalDevCode = (code == "LOCAL");
#else
            bool isLocalDevCode = false;
#endif

            if (!isLocalDevCode && code.Length != 6)
            {
                SetJoinError("Join code must be exactly 6 characters.");
                return;
            }

            if (_btnSubmitJoin != null) _btnSubmitJoin.interactable = false;
            if (_inputJoinCode != null) _inputJoinCode.interactable = false;
            if (_txtJoinErrorFeedback != null)
            {
                _txtJoinErrorFeedback.color = Gold;
                _txtJoinErrorFeedback.text = "Connecting to session...";
            }

            EnsureCloudClient();
            var client = PitStriker.Networking.Client.CloudNetworkClient.Instance;
            if (client != null)
            {
                if (client.IsConnected)
                {
                    client.JoinRoom(code);
                }
                else
                {
                    client.Connect();
                    Action onConn = null;
                    onConn = () =>
                    {
                        client.OnConnectedToServer -= onConn;
                        client.JoinRoom(code);
                    };
                    client.OnConnectedToServer += onConn;
                }
                return;
            }

            if (NetworkSessionManager.Instance != null)
            {
                bool success = false;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (code == "LOCAL")
                {
                    success = NetworkSessionManager.Instance.StartLocalClient();
                }
                else
#endif
                {
                    success = await NetworkSessionManager.Instance.JoinRelaySessionAsync(code);
                }

                if (success)
                {
                    _activeSessionCode = code;
                    MultiplayerAnalytics.TrackPrivateRoomJoined();
                    Debug.Log("<color=#00FF88>[ONLINE] Successfully connected as client!</color>");
                    ShowScreen(ScreenType.OnlineLobby);
                    UpdateOnlineLobbyUI();
                }
                else
                {
                    if (_btnSubmitJoin != null) _btnSubmitJoin.interactable = true;
                    if (_inputJoinCode != null) _inputJoinCode.interactable = true;
                    SetJoinError("Session not found or connection failed. Please verify the code.");
                }
            }
            else
            {
                SetJoinError("NetworkSessionManager not available.");
                if (_btnSubmitJoin != null) _btnSubmitJoin.interactable = true;
            }
        }

        private void SetJoinError(string message)
        {
            if (_txtJoinErrorFeedback != null)
            {
                _txtJoinErrorFeedback.color = new Color(1f, .35f, .35f);
                _txtJoinErrorFeedback.text = message;
            }
        }

        public void HandleCancelOnlineMatch()
        {
            Debug.Log("[ONLINE] Cancelling online match session...");
            StopWaitingPulseAnimation();
            CloseOnlineNotice();
            if (_lobbyCountdownCoroutine != null)
            {
                StopCoroutine(_lobbyCountdownCoroutine);
                _lobbyCountdownCoroutine = null;
            }

            _isOnlineTransitioning = false;
            _localWantsRematch = false;

            if (NetworkSessionManager.Instance != null)
            {
                NetworkSessionManager.Instance.ShutdownSession();
            }

            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.ReturnToMainMenu();
            }

            ShowScreen(ScreenType.OnlineMenu);
        }

        // =========================================================================
        // NETWORK SESSION EVENT CALLBACKS (Task 5, 6, 7, 8)
        // =========================================================================

        private void HandleJoinCodeReady(string code)
        {
            _activeSessionCode = code;
            if (_txtCreateJoinCode != null) _txtCreateJoinCode.text = code;
        }

        private void HandleOnlinePlayerJoined(ulong clientId)
        {
            Debug.Log($"<color=#00FFAA>[ONLINE LOBBY] Player connected: {clientId}</color>");

            // Update host screen display
            if (CurrentScreen == ScreenType.CreateMatch)
            {
                if (_txtHostP2Name != null) _txtHostP2Name.text = "PLAYER 2 (CONNECTED)";
                if (_txtHostP2Status != null)
                {
                    _txtHostP2Status.text = "Status: Online • Ready";
                    _txtHostP2Status.color = new Color(.25f, .95f, .55f);
                }
            }

            bool isHost = Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsHost;
            bool isClientOnly = Unity.Netcode.NetworkManager.Singleton != null && Unity.Netcode.NetworkManager.Singleton.IsClient && !Unity.Netcode.NetworkManager.Singleton.IsServer;

            if (isHost)
            {
                int count = NetworkSessionManager.Instance != null ? NetworkSessionManager.Instance.ConnectedPlayerCount : 0;
                if (count >= 2 && !_isOnlineTransitioning)
                {
                    ShowScreen(ScreenType.OnlineLobby);
                    UpdateOnlineLobbyUI();
                    StartLobbyCountdown();
                }
            }
            else if (isClientOnly)
            {
                ShowScreen(ScreenType.OnlineLobby);
                UpdateOnlineLobbyUI();
                if (_txtLobbyCountdown != null)
                {
                    _txtLobbyCountdown.text = "Connected to Host! Match starting soon...";
                }
            }
        }

        private void HandleOnlinePlayerLeft(ulong clientId)
        {
            Debug.LogWarning($"[ONLINE LOBBY] Player left: {clientId}");

            // Ignore local player disconnect
            if (Unity.Netcode.NetworkManager.Singleton == null || clientId == Unity.Netcode.NetworkManager.Singleton.LocalClientId)
            {
                return;
            }

            // Ignore if session is already offline or disconnecting
            if (NetworkSessionManager.Instance != null &&
                (NetworkSessionManager.Instance.CurrentState == NetworkSessionManager.SessionState.Offline ||
                 NetworkSessionManager.Instance.CurrentState == NetworkSessionManager.SessionState.Disconnecting))
            {
                return;
            }

            bool isActiveOnlineScreen = _isOnlineTransitioning
                || CurrentScreen == ScreenType.InGame
                || CurrentScreen == ScreenType.OnlineLobby
                || CurrentScreen == ScreenType.OnlineResult;

            if (isActiveOnlineScreen)
            {
                if (_lobbyCountdownCoroutine != null)
                {
                    StopCoroutine(_lobbyCountdownCoroutine);
                    _lobbyCountdownCoroutine = null;
                }
                _isOnlineTransitioning = false;

                ShowOnlineNotice("OPPONENT DISCONNECTED", "Your opponent has left the match.");
                HandleCancelOnlineMatch();
            }
        }

        private void HandleOnlineSessionError(string errorMsg)
        {
            Debug.LogError($"[ONLINE ERROR] {errorMsg}");

            // If we are already disconnecting or offline, don't interrupt navigation with an error popup
            if (NetworkSessionManager.Instance != null &&
                (NetworkSessionManager.Instance.CurrentState == NetworkSessionManager.SessionState.Offline ||
                 NetworkSessionManager.Instance.CurrentState == NetworkSessionManager.SessionState.Disconnecting))
            {
                return;
            }

            ShowOnlineNotice("NETWORK ALERT", errorMsg);
            if (CurrentScreen == ScreenType.CreateMatch || CurrentScreen == ScreenType.JoinMatch)
            {
                HandleCancelOnlineMatch();
            }
        }

        private void HandleOnlineStateChanged(NetworkSessionManager.SessionState state)
        {
            Debug.Log($"[ONLINE STATE] Session state -> {state}");
            if (state == NetworkSessionManager.SessionState.Reconnecting)
            {
                if (_txtDisconnectTitle != null) _txtDisconnectTitle.text = "CONNECTION LOST";
                if (_txtDisconnectCountdown != null) _txtDisconnectCountdown.text = "Reconnecting to match...\nPlease wait...";
                ShowDisconnectOverlay();
            }
            else if (state == NetworkSessionManager.SessionState.ConnectedInMatch && _disconnectOverlay != null && _disconnectOverlay.activeSelf)
            {
                HideDisconnectOverlay();
                ShowOnlineNotice("⚡ RECONNECTED", "Connection restored! Resuming match.");
            }
        }

        private void UpdateOnlineLobbyUI()
        {
            bool isHost = NetworkSessionManager.Instance != null && NetworkSessionManager.Instance.IsHost;

            if (_txtLobbyP1Header != null) _txtLobbyP1Header.text = isHost ? "PLAYER 1 (YOU / HOST)" : "PLAYER 1 (HOST)";
            if (_txtLobbyP2Header != null) _txtLobbyP2Header.text = isHost ? "PLAYER 2 (OPPONENT)" : "PLAYER 2 (YOU / GUEST)";

            if (_txtLobbyP1Details != null) _txtLobbyP1Details.text = "ID: 0 • CONNECTED";
            if (_txtLobbyP2Details != null) _txtLobbyP2Details.text = "ID: 1 • CONNECTED";
        }

        private void StartLobbyCountdown()
        {
            if (_lobbyCountdownCoroutine != null) StopCoroutine(_lobbyCountdownCoroutine);
            _lobbyCountdownCoroutine = StartCoroutine(LobbyCountdownRoutine());
        }

        private IEnumerator LobbyCountdownRoutine()
        {
            _isOnlineTransitioning = true;
            float remaining = 2.0f;

            while (remaining > 0f)
            {
                if (_txtLobbyCountdown != null)
                {
                    _txtLobbyCountdown.text = $"Both players ready! Match starting in {Mathf.CeilToInt(remaining)}...";
                }
                yield return new WaitForSecondsRealtime(0.5f);
                remaining -= 0.5f;
            }

            if (_txtLobbyCountdown != null) _txtLobbyCountdown.text = "Launching pitch...";
            yield return new WaitForSecondsRealtime(0.4f);

            TransitionToOnlineGameplay();
        }

        private void TransitionToOnlineGameplay()
        {
            _isOnlineTransitioning = false;
            Debug.Log("<color=#00FF88>[ONLINE] Transitioning from Online Lobby to Gameplay!</color>");

            // Configure match for 2 players (both human)
            ShowScreen(ScreenType.InGame);

            TurnManager tm = TurnManager.Instance != null ? TurnManager.Instance : UnityEngine.Object.FindAnyObjectByType<TurnManager>();
            if (tm != null)
            {
                bool[] isAI = new bool[] { false, false };
                string[] names = new string[] { "Player 1 (Host)", "Player 2 (Guest)" };
                tm.ConfigureAndStartMatch(2, isAI, names);
                string matchId = !string.IsNullOrEmpty(_activeSessionCode) ? _activeSessionCode : "online_match";
                MultiplayerAnalytics.TrackMatchStarted(matchId, "sunset_coastal");
                Debug.Log("<color=#00FF88>[ONLINE MATCH] 2-Player match initialized via TurnManager!</color>");
            }
            else
            {
                Debug.LogError("[ONLINE ERROR] TurnManager not found!");
            }
        }

        private void HandleNetworkMatchStarted(string matchId, string hostName, string guestName)
        {
            Debug.Log($"<color=#00FF88>[ONLINE] Received synchronized match start via RPC: {matchId} ({hostName} vs {guestName})</color>");
            if (_lobbyCountdownCoroutine != null)
            {
                StopCoroutine(_lobbyCountdownCoroutine);
                _lobbyCountdownCoroutine = null;
            }
            _isOnlineTransitioning = false;

            ShowScreen(ScreenType.InGame);

            // On remote clients (guest), initialize TurnManager match configuration
            if (Unity.Netcode.NetworkManager.Singleton != null && !Unity.Netcode.NetworkManager.Singleton.IsServer)
            {
                TurnManager tm = TurnManager.Instance != null ? TurnManager.Instance : UnityEngine.Object.FindAnyObjectByType<TurnManager>();
                if (tm != null)
                {
                    bool[] isAI = new bool[] { false, false };
                    string[] names = new string[] { string.IsNullOrEmpty(hostName) ? "Player 1 (Host)" : hostName, string.IsNullOrEmpty(guestName) ? "Player 2 (Guest)" : guestName };
                    tm.ConfigureAndStartMatch(2, isAI, names);
                    MultiplayerAnalytics.TrackMatchStarted(matchId, "sunset_coastal");
                    Debug.Log("<color=#00FF88>[ONLINE CLIENT] 2-Player match initialized on Guest via TurnManager!</color>");
                }
            }
        }

        // -------------------------------------------------------------------------
        // Pulse animation for "Waiting for opponent..."
        // -------------------------------------------------------------------------
        private void StartWaitingPulseAnimation()
        {
            StopWaitingPulseAnimation();
            _waitingPulseCoroutine = StartCoroutine(WaitingPulseRoutine());
        }

        private void StopWaitingPulseAnimation()
        {
            if (_waitingPulseCoroutine != null)
            {
                StopCoroutine(_waitingPulseCoroutine);
                _waitingPulseCoroutine = null;
            }
        }

        private IEnumerator WaitingPulseRoutine()
        {
            int dotCount = 0;
            while (CurrentScreen == ScreenType.CreateMatch)
            {
                dotCount = (dotCount + 1) % 4;
                string dots = new string('.', dotCount);
                if (_txtCreateStatusBanner != null && !string.IsNullOrEmpty(_activeSessionCode))
                {
                    _txtCreateStatusBanner.text = $"Waiting for opponent to connect{dots}";
                }
                yield return new WaitForSecondsRealtime(0.5f);
            }
        }

        // =========================================================================
        // PHASE 5 — ONLINE RESULT SCREEN (VICTORY / DEFEAT)
        // =========================================================================

        /// <summary>
        /// Builds the online result screen. Called once during BuildOnlineUI().
        /// ShowOnlineResult() populates content at match end.
        /// </summary>
        private void BuildOnlineResultPage()
        {
            _onlineResultPanel = Page("OnlineResult");

            // ---- Header ----
            _txtResultHeadline = Label(_onlineResultPanel.transform, "VICTORY", 0, 268, 850, 60, 46, Gold);
            _txtResultSubtitle = Label(_onlineResultPanel.transform, "You conquered all three pits!", 0, 226, 940, 28, 17, new Color(.72f, .83f, .80f));

            // ---- Stats Card ----
            var statsCard = Box("ResultStatsCard", _onlineResultPanel.transform, 0, 30, 700, 300, Card);
            Box("StatsAccent", statsCard.transform, 0, 146, 700, 6, Gold);
            Label(statsCard.transform, "MATCH SUMMARY", 0, 110, 640, 28, 18, Gold);

            // Left: Local player
            var localBox = Box("ResultLocalBox", statsCard.transform, -170, 8, 290, 156, Ink);
            Box("LocalStripe", localBox.transform, 0, 74, 290, 6, new Color(.15f, .65f, 1f));
            Label(localBox.transform, "YOU", 0, 44, 260, 26, 16, Cream);
            _txtResultLocalStats = Label(localBox.transform, "", 0, 0, 260, 56, 14, Cream);

            // Divider
            Label(statsCard.transform, "VS", 0, 8, 60, 40, 26, Gold);

            // Right: Opponent
            var opponentBox = Box("ResultOpponentBox", statsCard.transform, 170, 8, 290, 156, Ink);
            Box("OpponentStripe", opponentBox.transform, 0, 74, 290, 6, new Color(1f, .3f, .25f));
            Label(opponentBox.transform, "OPPONENT", 0, 44, 260, 26, 16, Cream);
            _txtResultOpponentStats = Label(opponentBox.transform, "", 0, 0, 260, 56, 14, Cream);

            // ---- Action Buttons ----
            _btnResultRematch = ActionButton("Btn_ResultRematch", _onlineResultPanel.transform, "REMATCH", -185, -228, 330, 62, Green);
            _btnResultRematch.onClick.AddListener(HandleResultRematchClicked);

            _btnResultMainMenu = ActionButton("Btn_ResultMainMenu", _onlineResultPanel.transform, "MAIN MENU", 185, -228, 330, 62, Ink);
            _btnResultMainMenu.onClick.AddListener(HandleCancelOnlineMatch);

            _txtResultRematchStatus = Label(_onlineResultPanel.transform, "", 0, -278, 640, 28, 14, new Color(.72f, .83f, .80f));
        }

        /// <summary>
        /// Populates the result screen with data from NetworkMatchState and switches to it.
        /// </summary>
        public void ShowOnlineResult(bool isLocalWinner, bool isAbandoned = false)
        {
            if (_onlineResultPanel == null) return;

            _localWantsRematch = false;
            if (_txtResultRematchStatus != null) _txtResultRematchStatus.text = "";

            // Headline
            if (_txtResultHeadline != null)
            {
                if (isAbandoned)
                {
                    _txtResultHeadline.text  = "MATCH ABANDONED";
                    _txtResultHeadline.color = new Color(1f, .55f, .1f); // orange
                }
                else
                {
                    _txtResultHeadline.text  = isLocalWinner ? "VICTORY" : "DEFEAT";
                    _txtResultHeadline.color = isLocalWinner ? Gold : new Color(1f, .35f, .35f);
                }
            }

            if (_txtResultSubtitle != null)
            {
                _txtResultSubtitle.text = isAbandoned
                    ? "Opponent left before the match could finish."
                    : (isLocalWinner ? "You conquered all three pits!" : "Better luck next time!");
            }

            // Rematch unavailable if match was abandoned
            if (_btnResultRematch != null)
            {
                _btnResultRematch.interactable = !isAbandoned;
            }

            // Stats from NetworkMatchState
            var state = NetworkMatchState.Instance;
            if (state != null)
            {
                int localIdx = state.GetLocalPlayerIndex();
                int oppIdx = 1 - localIdx;

                string localName  = state.GetPlayerName(localIdx);
                int localStrokes  = state.GetPlayerStrokes(localIdx);
                int localPit      = state.GetPlayerCurrentPit(localIdx);

                string oppName    = state.GetPlayerName(oppIdx);
                int oppStrokes    = state.GetPlayerStrokes(oppIdx);
                int oppPit        = state.GetPlayerCurrentPit(oppIdx);

                if (_txtResultLocalStats != null)
                    _txtResultLocalStats.text = $"{localName}\nStrokes: {localStrokes}\nPit Reached: {localPit}";

                if (_txtResultOpponentStats != null)
                    _txtResultOpponentStats.text = $"{oppName}\nStrokes: {oppStrokes}\nPit Reached: {oppPit}";
            }
            else if (PitStriker.Networking.Client.CloudMatchManager.Instance != null &&
                     PitStriker.Networking.Client.CloudMatchManager.Instance.IsOnlineMatchActive)
            {
                var mgr = PitStriker.Networking.Client.CloudMatchManager.Instance;
                int localIdx = PitStriker.Networking.Client.CloudNetworkClient.Instance?.LocalPlayerIndex ?? 0;
                var localData = localIdx == 0 ? mgr.Player0Data : mgr.Player1Data;
                var oppData = localIdx == 0 ? mgr.Player1Data : mgr.Player0Data;

                if (_txtResultLocalStats != null)
                    _txtResultLocalStats.text = $"{localData.Name}\nStrokes: {localData.TotalStrokes}\nPit Reached: {localData.CurrentPit}";

                if (_txtResultOpponentStats != null)
                    _txtResultOpponentStats.text = $"{oppData.Name}\nStrokes: {oppData.TotalStrokes}\nPit Reached: {oppData.CurrentPit}";
            }

            ShowScreen(ScreenType.OnlineResult);
        }


        /// <summary>
        /// Called when NetworkMatchState fires OnMatchCompletedNetworkEvent.
        /// Only shows the result screen when an active online match is running.
        /// </summary>
        private void HandleNetworkMatchCompleted(int winnerIndex)
        {
            bool isCloud = PitStriker.Networking.Client.CloudMatchManager.Instance != null &&
                           PitStriker.Networking.Client.CloudMatchManager.Instance.IsOnlineMatchActive;
            bool isNgo = NetworkSessionManager.Instance != null &&
                         NetworkSessionManager.Instance.ActiveNetworkMode != NetworkSessionManager.NetworkMode.None;

            if (!isCloud && !isNgo) return;

            if (CurrentScreen != ScreenType.InGame && CurrentScreen != ScreenType.OnlineResult) return;

            // Hide any disconnect overlay that may still be visible
            HideDisconnectOverlay();

            var state = NetworkMatchState.Instance;
            bool isAbandoned = (winnerIndex == -1);
            int localIdx = isCloud ? (PitStriker.Networking.Client.CloudNetworkClient.Instance?.LocalPlayerIndex ?? 0)
                                   : (state != null ? state.GetLocalPlayerIndex() : 0);
            bool isLocalWinner = !isAbandoned && (localIdx == winnerIndex);

            Debug.Log($"<color=#FFD700>[MENU ONLINE] Match completed. Winner={winnerIndex}, LocalWinner={isLocalWinner}, Abandoned={isAbandoned}</color>");
            string matchId = !string.IsNullOrEmpty(_activeSessionCode) ? _activeSessionCode : "online_match";
            if (isAbandoned)
            {
                MultiplayerAnalytics.TrackMatchAbandoned(matchId);
            }
            else
            {
                int p1Strokes = isCloud ? PitStriker.Networking.Client.CloudMatchManager.Instance.Player0Data.TotalStrokes : (state != null ? state.GetPlayerStrokes(0) : 0);
                int p2Strokes = isCloud ? PitStriker.Networking.Client.CloudMatchManager.Instance.Player1Data.TotalStrokes : (state != null ? state.GetPlayerStrokes(1) : 0);
                MultiplayerAnalytics.TrackMatchCompleted(matchId, winnerIndex, p1Strokes, p2Strokes);
            }
            ShowOnlineResult(isLocalWinner, isAbandoned);
        }

        /// <summary>
        /// Signals the server that the local player wants a rematch.
        /// </summary>
        private void HandleResultRematchClicked()
        {
            if (_localWantsRematch) return;
            _localWantsRematch = true;

            if (_btnResultRematch != null) _btnResultRematch.interactable = false;
            if (_txtResultRematchStatus != null) _txtResultRematchStatus.text = "Waiting for opponent to agree...";

            if (PitStriker.Networking.Client.CloudMatchManager.Instance != null &&
                PitStriker.Networking.Client.CloudMatchManager.Instance.IsOnlineMatchActive)
            {
                PitStriker.Networking.Client.CloudNetworkClient.Instance?.RequestRematch();
                Debug.Log("<color=#00FFAA>[REMATCH] Sent rematch request to cloud server.</color>");
                return;
            }

            var state = NetworkMatchState.Instance;
            if (state != null && Unity.Netcode.NetworkManager.Singleton != null)
            {
                ulong localId = Unity.Netcode.NetworkManager.Singleton.LocalClientId;
                state.RequestRematchServerRpc(localId);
                Debug.Log($"<color=#00FFAA>[REMATCH] Sent rematch request from client {localId}.</color>");
            }
        }

        // =========================================================================
        // PHASE 6 — DISCONNECT OVERLAY & GRACE PERIOD UI
        // =========================================================================

        /// <summary>
        /// Builds a full-screen overlay that appears over InGame/OnlineResult
        /// without replacing the current screen. Hidden by default.
        /// </summary>
        private void BuildDisconnectOverlay()
        {
            // Non-blocking banner at top of screen so players can always see the 3D board and marbles
            _disconnectOverlay = Box("DisconnectOverlay", transform, 0, 260, 640, 110, Card);
            _disconnectOverlay.SetActive(false);

            Box("DiscoAccent", _disconnectOverlay.transform, 0, 52, 640, 4, new Color(1f, .55f, .1f));

            _txtDisconnectTitle = Label(_disconnectOverlay.transform, "OPPONENT CONNECTION PAUSED", -120, 18, 360, 28, 16, new Color(1f, .55f, .1f));
            _txtDisconnectCountdown = Label(_disconnectOverlay.transform, "Waiting for reconnection... (15s)", -120, -14, 360, 24, 13, Cream);

            _btnDisconnectLeave = ActionButton("Btn_DiscoLeave", _disconnectOverlay.transform, "EXIT", 220, 0, 140, 44, Ink);
            _btnDisconnectLeave.onClick.AddListener(HandleCancelOnlineMatch);
        }

        private void ShowDisconnectOverlay()
        {
            if (_disconnectOverlay != null)
            {
                _disconnectOverlay.SetActive(true);
                _disconnectOverlay.transform.SetAsLastSibling();
            }
        }

        private void HideDisconnectOverlay()
        {
            if (_disconnectOverlay != null) _disconnectOverlay.SetActive(false);
        }

        // Phase 6 event handlers wired in InitOnlineSessionListeners
        private void HandleGracePeriodStarted(float totalSeconds)
        {
            Debug.Log($"[MENU] Disconnect grace period started ({totalSeconds}s).");
            string matchId = !string.IsNullOrEmpty(_activeSessionCode) ? _activeSessionCode : "online_match";
            MultiplayerAnalytics.TrackOpponentDisconnected(matchId);
            if (_txtDisconnectTitle != null)     _txtDisconnectTitle.text     = "OPPONENT DISCONNECTED";
            if (_txtDisconnectCountdown != null) _txtDisconnectCountdown.text = $"Waiting for reconnection...\n({Mathf.CeilToInt(totalSeconds)}s)";
            ShowDisconnectOverlay();
        }

        private void HandleGracePeriodTick(float remaining)
        {
            if (_txtDisconnectCountdown != null)
                _txtDisconnectCountdown.text = $"Waiting for reconnection...\n({Mathf.CeilToInt(remaining)}s)";
        }

        private void HandleGracePeriodExpiredUI()
        {
            Debug.Log("[MENU] Grace period expired — match abandoned.");
            HideDisconnectOverlay();
            // HandleNetworkMatchCompleted(-1) will fire shortly via ServerEndMatch
        }

        private void HandleOpponentReturnedUI()
        {
            Debug.Log("[MENU] Opponent returned — showing reconnected toast.");
            string matchId = !string.IsNullOrEmpty(_activeSessionCode) ? _activeSessionCode : "online_match";
            MultiplayerAnalytics.TrackOpponentReconnected(matchId);
            HideDisconnectOverlay();

            if (Application.isPlaying)
            {
                if (_reconnectedToastCoroutine != null) StopCoroutine(_reconnectedToastCoroutine);
                _reconnectedToastCoroutine = StartCoroutine(ShowReconnectedToastRoutine());
            }
        }

        private IEnumerator ShowReconnectedToastRoutine()
        {
            // Reuse the notice modal as a brief toast
            ShowOnlineNotice("⚡ OPPONENT RECONNECTED", "Your opponent is back. Resuming match!");
            yield return new WaitForSecondsRealtime(2.0f);
            CloseOnlineNotice();
            _reconnectedToastCoroutine = null;
        }

        // =========================================================================
        // PHASE 7 — QUICK MATCH (MATCHMAKING)
        // =========================================================================

        // Runtime references for Quick Match screen
        private Text   _txtQMStatus;       // "SEARCHING FOR OPPONENT..."
        private Text   _txtQMSubtitle;     // spinner dots / "OPPONENT FOUND!"
        private Button _btnQMCancel;
        private Coroutine _qmDotCoroutine;

        /// <summary>
        /// Builds the Quick Match search screen panel.
        /// </summary>
        private void BuildQuickMatchPage()
        {
            _quickMatchPanel = Page("QuickMatch");

            Label(_quickMatchPanel.transform, "QUICK MATCH", 0, 268, 850, 44, 30, Cream);
            Label(_quickMatchPanel.transform, "Finding an opponent in your region...", 0, 234, 940, 26, 16, new Color(.72f, .83f, .80f));

            var searchCard = Box("QMSearchCard", _quickMatchPanel.transform, 0, 20, 540, 360, Card);
            Box("QMAccent", searchCard.transform, 0, 176, 540, 6, Green);

            // Animated status label
            _txtQMStatus = Label(searchCard.transform, "SEARCHING FOR OPPONENT...", 0, 110, 480, 36, 22, Gold);
            _txtQMSubtitle = Label(searchCard.transform, "...", 0, 60, 480, 30, 18, new Color(.72f, .83f, .80f));

            // Version + mode badge
            var badge = Box("QMBadge", searchCard.transform, 0, -10, 440, 36, Ink);
            Label(badge.transform, $"MODE: Pit Striker  •  VERSION: {UnityEngine.Application.version}", 0, 0, 430, 22, 13, Cream);

            _btnQMCancel = ActionButton("Btn_QMCancel", searchCard.transform, "CANCEL", 0, -120, 320, 56, Ink);
            _btnQMCancel.onClick.AddListener(HandleCancelQuickMatch);
        }

        public void HandleCancelQuickMatch()
        {
            CloseOnlineNotice();
            if (QuickMatchManager.Instance != null)
                QuickMatchManager.Instance.CancelSearch();
            if (NetworkSessionManager.Instance != null)
                NetworkSessionManager.Instance.ShutdownSession();
            if (_qmDotCoroutine != null) { StopCoroutine(_qmDotCoroutine); _qmDotCoroutine = null; }
            ShowScreen(ScreenType.OnlineMenu);
        }

        // ---- QuickMatchManager event handlers ----
        private void HandleQuickMatchSearching()
        {
            MultiplayerAnalytics.TrackMatchmakingStarted();
            if (_txtQMStatus != null)   _txtQMStatus.text   = "SEARCHING FOR OPPONENT...";
            if (_txtQMSubtitle != null) _txtQMSubtitle.text = "";
            if (_btnQMCancel != null)   _btnQMCancel.interactable = true;
            if (_qmDotCoroutine != null) StopCoroutine(_qmDotCoroutine);
            _qmDotCoroutine = StartCoroutine(QMDotAnimRoutine());
        }

        private void HandleQuickMatchFound()
        {
            MultiplayerAnalytics.TrackMatchFound();
            if (_qmDotCoroutine != null) { StopCoroutine(_qmDotCoroutine); _qmDotCoroutine = null; }
            if (_txtQMStatus != null)   _txtQMStatus.text   = "OPPONENT FOUND!";
            if (_txtQMSubtitle != null) _txtQMSubtitle.text = "Preparing match...";
            if (_btnQMCancel != null)   _btnQMCancel.interactable = false;
            // A short delay then transition to lobby
            StartCoroutine(QuickMatchFoundTransitionRoutine());
        }

        private void HandleQuickMatchTimeout()
        {
            if (_qmDotCoroutine != null) { StopCoroutine(_qmDotCoroutine); _qmDotCoroutine = null; }
            // Show a timeout notice on the Quick Match screen
            ShowOnlineNotice("NO OPPONENT FOUND",
                "No opponent was found in time. Please try again or go back.");
        }

        private void HandleQuickMatchCancelled()
        {
            if (_qmDotCoroutine != null) { StopCoroutine(_qmDotCoroutine); _qmDotCoroutine = null; }
            ShowScreen(ScreenType.OnlineMenu);
        }

        private void HandleQuickMatchError(string errorMsg)
        {
            if (_qmDotCoroutine != null) { StopCoroutine(_qmDotCoroutine); _qmDotCoroutine = null; }
            ShowOnlineNotice("MATCHMAKING ERROR", errorMsg);
            ShowScreen(ScreenType.OnlineMenu);
        }

        private IEnumerator QMDotAnimRoutine()
        {
            int dots = 0;
            while (true)
            {
                dots = (dots + 1) % 4;
                if (_txtQMSubtitle != null)
                    _txtQMSubtitle.text = new string('.', dots + 1);
                yield return new WaitForSecondsRealtime(0.4f);
            }
        }

        private IEnumerator QuickMatchFoundTransitionRoutine()
        {
            yield return new WaitForSecondsRealtime(1.5f);
            ShowScreen(ScreenType.OnlineLobby);
            UpdateOnlineLobbyUI();
            StartLobbyCountdown();
        }
    }
}
