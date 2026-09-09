using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using PitStriker.Gameplay;

namespace PitStriker.UI
{
    public partial class MenuManager
    {
        public ScreenType CurrentScreen { get; private set; }
        private ScreenType _rulesReturn = ScreenType.Home;
        private Image _screenBackdrop;
        private GameObject _confirmation;
        private Text _confirmTitle, _confirmBody;
        private Action _confirmedAction;
        private RectTransform _safeFrame;
        private GameObject _splashPanel;
        private CanvasGroup _splashPanelCanvasGroup;
        private CanvasGroup _splashLogoCanvasGroup;
        private RectTransform _splashLogoRect;
        private bool _skipSplash = false;
        private static bool _splashHasPlayed = false;
        private static readonly Color Ink = new Color(.055f, .13f, .13f, .97f);
        private static readonly Color Card = new Color(.10f, .21f, .21f, 1);
        private static readonly Color Gold = new Color(.78f, .46f, .13f, 1);
        private static readonly Color Green = new Color(.12f, .43f, .34f, 1);
        private static readonly Color Cream = new Color(.98f, .94f, .82f, 1);

        private void BuildModernUI()
        {
            var canvas = GetComponent<Canvas>();
            if (!canvas) canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 300; // Above HUD and its victory sub-canvas.
            var scaler = GetComponent<CanvasScaler>();
            if (!scaler) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 1;
            if (!GetComponent<GraphicRaycaster>()) gameObject.AddComponent<GraphicRaycaster>();
            var events = FindAnyObjectByType<EventSystem>();
            if (!events) events = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
            var legacyInput = events.GetComponent<StandaloneInputModule>();
            if (legacyInput) { legacyInput.enabled = false; Destroy(legacyInput); }
            if (!events.GetComponent<InputSystemUIInputModule>()) events.gameObject.AddComponent<InputSystemUIInputModule>();

            // Replace serialized legacy screens as well as incomplete runtime menus.
            foreach (Transform child in transform) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            var hud = FindAnyObjectByType<HUDManager>(FindObjectsInactive.Include);
            _hudRoot = hud ? hud.gameObject : null;
            if (_hudRoot)
                foreach (var t in _hudRoot.GetComponentsInChildren<Transform>(true))
                    if (t.name == "TopBar_Navigation" || t.name == "Btn_HUD_Pause") t.gameObject.SetActive(false);

            _screenBackdrop = CreatePanel("MenuBackdrop", transform, new Color(.02f,.07f,.07f,.80f)).GetComponent<Image>();
            _safeFrame = new GameObject("SafeMenuFrame", typeof(RectTransform)).GetComponent<RectTransform>();
            _safeFrame.SetParent(transform, false);
            _safeFrame.sizeDelta = new Vector2(1080, 640);

            _homePanel = Page("Home");
            Label(_homePanel.transform, "VILLAGE MARBLES", -240, 216, 430, 32, 18, Gold);
            Label(_homePanel.transform, "PIT STRIKER", -240, 145, 460, 84, 52, Cream);
            Label(_homePanel.transform, "A little aim. A clever strike.", -240, 65, 460, 42, 24, Cream);
            Label(_homePanel.transform, "Play against the computer or pass the phone\nto friends. Two to four players, one village lane.", -240, -5, 460, 80, 20, new Color(.72f,.83f,.80f));
            Label(_homePanel.transform, "LOCAL PLAY  /  2–4 PLAYERS", -240, -226, 460, 34, 17, Cream);
            var homeCard = Box("PlayCard", _homePanel.transform, 278, 0, 420, 492, Card);
            Label(homeCard.transform, "Ready to play?", 0, 172, 370, 52, 30, Cream);
            _homePlayButton = ActionButton("Btn_Play", homeCard.transform, "PLAY", 0, 72, 336, 72, Gold);
            _homeRulesButton = ActionButton("Btn_Rules", homeCard.transform, "How to play", 0, -24, 336, 64, Green);
            _homeExitButton = ActionButton("Btn_Exit", homeCard.transform, "Exit game", 0, -116, 336, 60, Ink);

            _choosePlayersPanel = Page("MatchSetup");
            Label(_choosePlayersPanel.transform, "Set up your match", 0, 264, 850, 56, 34, Cream);
            Label(_choosePlayersPanel.transform, "Choose the number of marbles, then who controls each one.", 0, 213, 940, 36, 20, Cream);
            _btn2Players = ActionButton("Btn_2P", _choosePlayersPanel.transform, "2 players", -252, 145, 230, 60, Card);
            _btn3Players = ActionButton("Btn_3P", _choosePlayersPanel.transform, "3 players", 0, 145, 230, 60, Card);
            _btn4Players = ActionButton("Btn_4P", _choosePlayersPanel.transform, "4 players", 252, 145, 230, 60, Card);
            _img2Players = _btn2Players.image; _img3Players = _btn3Players.image; _img4Players = _btn4Players.image;
            _playerSlotRows = new GameObject[4]; _playerSlotNameTexts = new Text[4];
            _playerSlotRoleTexts = new Text[4]; _playerSlotToggleButtons = new Button[4];
            Color[] colors = { new Color(.15f,.65f,1), new Color(1,.3f,.25f), new Color(.2f,.8f,.4f), new Color(1,.75f,.2f) };
            for (int i = 0; i < 4; i++)
            {
                var row = Box("Slot_P" + (i+1), _choosePlayersPanel.transform, 0, 64-i*70, 736, 62, Card);
                Box("MarbleColor", row.transform, -330, 0, 12, 34, colors[i]);
                _playerSlotNameTexts[i] = Label(row.transform, "Player " + (i+1), -175, 0, 260, 44, 23, Cream);
                var button = ActionButton("ToggleRoleBtn", row.transform, "COMPUTER  >", 196, 0, 304, 54, Green);
                button.interactable = i != 0;
                _playerSlotRows[i] = row;
                _playerSlotToggleButtons[i] = button;
                _playerSlotRoleTexts[i] = button.GetComponentInChildren<Text>();
            }
            _backToHomeButton = ActionButton("Btn_BackToHome", _choosePlayersPanel.transform, "Back", -252, -252, 230, 64, Card);
            _startMatchButton = ActionButton("Btn_StartMatch", _choosePlayersPanel.transform, "START MATCH", 130, -252, 476, 64, Gold);

            _rulesModal = Page("HowToPlay");
            Label(_rulesModal.transform, "How to play", 0, 255, 850, 60, 36, Cream);
            string[] headings = { "01   Aim & strike", "02   Win the toss", "03   Reach the pits", "04   Earn another shot" };
            string[] bodies = {
                "Swipe to aim and launch your marble.\nUse the power control for a measured strike.",
                "Throw towards Pit 3. The closest marble\ngets the first turn in the match.",
                "Sink your marble in order: Pit 1, then 2,\nthen 3. First to finish wins.",
                "Sink your target pit or hit another marble\nfor a bonus shot. Up to three shots per turn." };
            for (int i=0; i<4; i++)
            {
                float x = i%2 == 0 ? -252 : 252, y = i<2 ? 112 : -68;
                var card = Box("Rule"+i, _rulesModal.transform, x, y, 474, 156, Card);
                Label(card.transform, headings[i], 0, 40, 430, 42, 25, Cream);
                Label(card.transform, bodies[i], 0, -24, 430, 70, 20, Cream);
            }
            _closeRulesButton = ActionButton("Btn_CloseRules", _rulesModal.transform, "Back", 0, -252, 320, 64, Gold);

            _pauseModal = Page("Pause");
            Label(_pauseModal.transform, "Match paused", 0, 232, 800, 64, 40, Cream);
            Label(_pauseModal.transform, "Take your time. Your turn will be here.", 0, 175, 850, 40, 22, Cream);
            _pauseResumeButton = ActionButton("Btn_Resume", _pauseModal.transform, "RESUME MATCH", 0, 86, 450, 72, Gold);
            _pauseRestartButton = ActionButton("Btn_Restart", _pauseModal.transform, "Restart match", 0, -2, 450, 64, Green);
            var rules = ActionButton("Btn_PauseRules", _pauseModal.transform, "How to play", 0, -82, 450, 64, Card);
            rules.onClick.AddListener(OpenRules);
            _pauseHomeButton = ActionButton("Btn_HomeMenu", _pauseModal.transform, "Main menu", 0, -162, 450, 64, Card);
            _hudPauseButton = ActionButton("Btn_HUD_Pause", _safeFrame, "II  PAUSE", 434, 274, 180, 64, Ink);

            _confirmation = Page("ConfirmAction");
            _confirmation.GetComponent<Image>().color = new Color(.025f,.07f,.07f,1);
            _confirmTitle = Label(_confirmation.transform, "Leave this match?", 0, 116, 950, 70, 36, Cream);
            _confirmBody = Label(_confirmation.transform, "", 0, 16, 850, 96, 24, Cream);
            _confirmation.SetActive(false);

            // Top-level XTINEX Splash Screen overlay
            _splashPanel = CreatePanel("XTINEX_SplashScreen", transform, Color.black);
            var splashRect = _splashPanel.GetComponent<RectTransform>();
            splashRect.anchorMin = Vector2.zero;
            splashRect.anchorMax = Vector2.one;
            splashRect.sizeDelta = Vector2.zero;
            splashRect.anchoredPosition = Vector2.zero;

            _splashPanelCanvasGroup = _splashPanel.AddComponent<CanvasGroup>();
            _splashPanelCanvasGroup.alpha = 1f; // Solid pitch black covering scene on boot

            var logoObj = new GameObject("XTINEX_Logo");
            logoObj.transform.SetParent(_splashPanel.transform, false);
            _splashLogoRect = logoObj.AddComponent<RectTransform>();
            _splashLogoRect.anchorMin = Vector2.zero;
            _splashLogoRect.anchorMax = Vector2.one;
            _splashLogoRect.sizeDelta = Vector2.zero;
            _splashLogoRect.anchoredPosition = Vector2.zero;

            _splashLogoCanvasGroup = logoObj.AddComponent<CanvasGroup>();
            _splashLogoCanvasGroup.alpha = 0f; // Logo starts hidden on pitch black background

            var logoImg = logoObj.AddComponent<Image>();
            var sprite = Resources.Load<Sprite>("UI/XTINEX_Splash_Screen_16x9");
            if (sprite == null)
            {
                var tex = Resources.Load<Texture2D>("UI/XTINEX_Splash_Screen_16x9");
                if (tex != null)
                {
                    sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                }
            }
            if (sprite != null)
            {
                logoImg.sprite = sprite;
            }
            logoImg.preserveAspect = true;
            logoImg.raycastTarget = false;

            var skipBtn = _splashPanel.AddComponent<Button>();
            var skipNav = skipBtn.navigation;
            skipNav.mode = Navigation.Mode.None;
            skipBtn.navigation = skipNav;
            skipBtn.onClick.AddListener(() => { _skipSplash = true; });

            _splashPanel.transform.SetAsLastSibling();

            FitSafeArea();
        }

        private GameObject Page(string name)
        {
            return Box(name, _safeFrame, 0, 0, 1080, 640, Ink);
        }
        private static GameObject Box(string name, Transform parent, float x, float y, float w, float h, Color color)
        {
            var obj = CreatePanel(name, parent, color);
            Position(obj, x,y,w,h);
            return obj;
        }
        private static void Position(GameObject obj, float x, float y, float w, float h)
        {
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f,.5f);
            rect.sizeDelta = new Vector2(w,h); rect.anchoredPosition = new Vector2(x,y);
        }
        private static Text Label(Transform parent, string text, float x, float y, float w, float h, int size, Color color)
        {
            var obj = CreateText("Label", parent, text, size, FontStyle.Normal, color, TextAnchor.MiddleCenter);
            Position(obj,x,y,w,h);
            var t = obj.GetComponent<Text>(); t.verticalOverflow = VerticalWrapMode.Truncate;
            return t;
        }
        private static Button ActionButton(string name, Transform parent, string label, float x, float y, float w, float h, Color color)
        {
            var obj = CreateButton(name,parent,label,color,22);
            Position(obj,x,y,w,h);
            var shadow = obj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0,0,0,.22f); shadow.effectDistance = new Vector2(0,-3);
            var button = obj.GetComponent<Button>();
            var navigation = button.navigation; navigation.mode = Navigation.Mode.None; button.navigation = navigation;
            return button;
        }
        private void FitSafeArea()
        {
            if (!_safeFrame || Screen.width == 0 || Screen.height == 0) return;
            var canvas = GetComponent<Canvas>();
            var safe = Screen.safeArea;
            float scale = Mathf.Max(.01f,canvas.scaleFactor);
            _safeFrame.anchoredPosition = (safe.center - new Vector2(Screen.width,Screen.height)*.5f)/scale;
            _safeFrame.localScale = Vector3.one * Mathf.Min((safe.width/scale-32)/1080f,(safe.height/scale-24)/640f);
        }
        private void Update()
        {
            FitSafeArea();
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) NavigateBack();
        }
        public void NavigateBack()
        {
            if (_confirmation && _confirmation.activeSelf) { CancelConfirmation(); return; }
            switch (CurrentScreen)
            {
                case ScreenType.InGame: HandlePauseClicked(); break;
                case ScreenType.Pause: HandleResumeClicked(); break;
                case ScreenType.Rules: ShowScreen(_rulesReturn); break;
                case ScreenType.ChoosePlayers: ShowScreen(ScreenType.Home); break;
                case ScreenType.Home: Confirm("Exit game?", "You can start a new match whenever you return.", HandleExitClicked); break;
            }
        }
        private void OpenRules()
        {
            _rulesReturn = CurrentScreen == ScreenType.Pause ? ScreenType.Pause : ScreenType.Home;
            ShowScreen(ScreenType.Rules);
        }
        private void Confirm(string title, string body, Action action)
        {
            _confirmedAction = action; _confirmTitle.text = title; _confirmBody.text = body;
            _confirmation.SetActive(true); _confirmation.transform.SetAsLastSibling();
        }
        private void CancelConfirmation() { _confirmation.SetActive(false); _confirmedAction = null; }
        private void OnApplicationPause(bool paused) { if (paused && CurrentScreen == ScreenType.InGame) HandlePauseClicked(); }
        private void OnApplicationFocus(bool focused) { if (!focused && CurrentScreen == ScreenType.InGame) HandlePauseClicked(); }
        private void OnDestroy() { if (Instance == this) { Instance = null; Time.timeScale = 1; } }
        private void SaveSetup()
        {
            if (Application.isBatchMode) return; // Automated checks must not replace the user's setup.
            PlayerPrefs.SetInt("UI.PlayerCount",_selectedPlayerCount);
            for(int i=1;i<4;i++) PlayerPrefs.SetInt("UI.Bot"+i,_isAISlot[i]?1:0);
            PlayerPrefs.Save();
        }
        private void LoadSetup()
        {
            _selectedPlayerCount = Mathf.Clamp(PlayerPrefs.GetInt("UI.PlayerCount",2),2,4);
            _isAISlot[0] = false;
            for(int i=1;i<4;i++) _isAISlot[i] = PlayerPrefs.GetInt("UI.Bot"+i,1)==1;
        }

        public IEnumerator PlaySplashScreenRoutine()
        {
            if (_splashPanel == null) yield break;

            // In automated test runs or if already displayed, bypass animation
            if (Application.isBatchMode || _splashHasPlayed || System.IO.File.Exists("Library/MenuFlowChecks.running"))
            {
                _splashPanel.SetActive(false);
                if (PitStriker.Audio.AudioManager.Instance != null)
                {
                    PitStriker.Audio.AudioManager.Instance.PlayStartMusic();
                }
                yield break;
            }

            _splashHasPlayed = true;
            _splashPanel.SetActive(true);
            _splashPanel.transform.SetAsLastSibling();

            // Background remains 100% solid pitch black covering all 3D scene & UI elements
            if (_splashPanelCanvasGroup != null) _splashPanelCanvasGroup.alpha = 1f;
            // Logo begins hidden
            if (_splashLogoCanvasGroup != null) _splashLogoCanvasGroup.alpha = 0f;
            if (_splashLogoRect != null) _splashLogoRect.localScale = new Vector3(0.92f, 0.92f, 1f);

            _skipSplash = false;

            // Phase 1: Smooth Fade In of XTINEX Logo onto black backdrop & subtle scale-in (0.85s)
            float elapsed = 0f;
            float fadeInDuration = 0.85f;
            while (elapsed < fadeInDuration && !_skipSplash)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeInDuration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                if (_splashLogoCanvasGroup != null) _splashLogoCanvasGroup.alpha = smoothT;
                if (_splashLogoRect != null) _splashLogoRect.localScale = Vector3.one * Mathf.Lerp(0.92f, 1.00f, smoothT);
                yield return null;
            }

            if (_splashLogoCanvasGroup != null) _splashLogoCanvasGroup.alpha = 1f;

            // Phase 2: Ambient Cinematic Hold with gentle volumetric expansion (1.5s)
            elapsed = 0f;
            float holdDuration = 1.5f;
            while (elapsed < holdDuration && !_skipSplash)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / holdDuration);
                if (_splashLogoRect != null) _splashLogoRect.localScale = Vector3.one * Mathf.Lerp(1.00f, 1.04f, t);
                yield return null;
            }

            // Phase 3: Fade Out the entire black splash overlay to reveal the Home Menu and Game for the first time (0.75s, or 0.25s on skip)
            if (PitStriker.Audio.AudioManager.Instance != null)
            {
                PitStriker.Audio.AudioManager.Instance.PlayStartMusic(fade: true);
            }

            elapsed = 0f;
            float fadeOutDuration = _skipSplash ? 0.25f : 0.75f;
            float startAlpha = _splashPanelCanvasGroup != null ? _splashPanelCanvasGroup.alpha : 1f;
            Vector3 startScale = _splashLogoRect != null ? _splashLogoRect.localScale : Vector3.one;
            Vector3 endScale = startScale * 1.03f;

            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeOutDuration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                if (_splashPanelCanvasGroup != null) _splashPanelCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, smoothT);
                if (_splashLogoRect != null) _splashLogoRect.localScale = Vector3.Lerp(startScale, endScale, smoothT);
                yield return null;
            }

            _splashPanel.SetActive(false);
        }
    }
}
