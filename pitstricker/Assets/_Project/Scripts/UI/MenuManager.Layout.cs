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
        private ScreenType _settingsReturn = ScreenType.Home;
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
            _screenBackdrop.raycastTarget = false;
            _safeFrame = new GameObject("SafeMenuFrame", typeof(RectTransform)).GetComponent<RectTransform>();
            _safeFrame.SetParent(transform, false);
            _safeFrame.sizeDelta = new Vector2(1080, 640);

            _homePanel = Page("Home");
            // Left Hero Branding Column
            Label(_homePanel.transform, "SUNSET MARBLES", -260, 218, 460, 32, 18, Gold);
            Label(_homePanel.transform, "PIT STRIKER", -260, 144, 480, 84, 54, Cream);
            Label(_homePanel.transform, "A little aim. A clever strike.", -260, 72, 480, 38, 22, Cream);
            Label(_homePanel.transform, "Play against the computer or pass the phone\nto friends. Two to four players, one coastal lane.", -260, 8, 480, 70, 19, new Color(.72f,.83f,.80f));

            // Current Course Pill Card
            var coursePill = Box("CoursePill", _homePanel.transform, -260, -82, 460, 72, Card);
            Label(coursePill.transform, "CURRENT COURSE", 0, 16, 430, 22, 13, Gold);
            Label(coursePill.transform, "Sunset Coastal  •  3 Pits  •  Par 6", 0, -10, 430, 28, 19, Cream);
            Label(_homePanel.transform, "LOCAL PLAY  •  2–4 PLAYERS", -260, -230, 460, 32, 16, new Color(.72f,.83f,.80f));

            // Right Main Menu Action Hub (Hierarchy: Play -> Maps -> How To Play -> Settings -> Exit)
            var homeCard = Box("PlayCard", _homePanel.transform, 270, 0, 430, 530, Card);
            Label(homeCard.transform, "MAIN MENU", 0, 220, 370, 36, 20, Gold);
            _homePlayButton = ActionButton("Btn_Play", homeCard.transform, "PLAY", 0, 142, 356, 76, Gold);
            _homeMapsButton = ActionButton("Btn_Maps", homeCard.transform, "MAPS", 0, 62, 356, 60, Green);
            _homeRulesButton = ActionButton("Btn_Rules", homeCard.transform, "HOW TO PLAY", 0, -12, 356, 60, Card);
            _homeSettingsButton = ActionButton("Btn_Settings", homeCard.transform, "SETTINGS", 0, -86, 356, 60, Card);
            _homeExitButton = ActionButton("Btn_Exit", homeCard.transform, "EXIT", 0, -164, 356, 52, Ink);

            _choosePlayersPanel = Page("MatchSetup");
            Label(_choosePlayersPanel.transform, "MATCH SETUP", 0, 272, 850, 44, 30, Cream);
            Label(_choosePlayersPanel.transform, "Configure course, participants, and controller assignments.", 0, 238, 940, 26, 17, new Color(.72f,.83f,.80f));

            // Course Banner with Change Course CTA
            var courseBanner = Box("CourseBanner", _choosePlayersPanel.transform, 0, 192, 760, 52, Card);
            Box("CourseAccent", courseBanner.transform, -376, 0, 8, 52, Gold);
            _matchCourseNameText = Label(courseBanner.transform, "MAP 01 • SUNSET COASTAL", -90, 8, 480, 24, 18, Gold);
            _matchCourseSpecsText = Label(courseBanner.transform, "3 Pits • Par 6 • Sand Verge", -90, -12, 480, 20, 14, Cream);
            _btnChangeCourse = ActionButton("Btn_ChangeCourse", courseBanner.transform, "CHANGE COURSE >", 260, 0, 200, 40, Green);

            // Player Count Selectors
            _btn2Players = ActionButton("Btn_2P", _choosePlayersPanel.transform, "2 players", -252, 130, 230, 48, Card);
            _btn3Players = ActionButton("Btn_3P", _choosePlayersPanel.transform, "3 players", 0, 130, 230, 48, Card);
            _btn4Players = ActionButton("Btn_4P", _choosePlayersPanel.transform, "4 players", 252, 130, 230, 48, Card);
            _img2Players = _btn2Players.image; _img3Players = _btn3Players.image; _img4Players = _btn4Players.image;
            _playerSlotRows = new GameObject[4]; _playerSlotNameTexts = new Text[4];
            _playerSlotRoleTexts = new Text[4]; _playerSlotToggleButtons = new Button[4];
            Color[] colors = { new Color(.15f,.65f,1), new Color(1,.3f,.25f), new Color(.2f,.8f,.4f), new Color(1,.75f,.2f) };
            for (int i = 0; i < 4; i++)
            {
                var row = Box("Slot_P" + (i+1), _choosePlayersPanel.transform, 0, 66-i*52, 760, 46, Card);
                Box("MarbleColor", row.transform, -350, 0, 10, 28, colors[i]);
                _playerSlotNameTexts[i] = Label(row.transform, "Player " + (i+1), -185, 0, 260, 36, 20, Cream);
                var button = ActionButton("ToggleRoleBtn", row.transform, "COMPUTER  >", 210, 0, 280, 38, Green);
                button.interactable = i != 0;
                _playerSlotRows[i] = row;
                _playerSlotToggleButtons[i] = button;
                _playerSlotRoleTexts[i] = button.GetComponentInChildren<Text>();
            }

            // Future-Proof Match Options Container
            var futureCard = Box("FutureOptionsCard", _choosePlayersPanel.transform, 0, -152, 760, 36, Ink);
            Label(futureCard.transform, "STANDARD RULES: 3 Consecutive Pits • Unlimited Turns • Deterministic Physics", 0, 0, 740, 24, 13, new Color(.72f,.83f,.80f));

            _backToHomeButton = ActionButton("Btn_BackToHome", _choosePlayersPanel.transform, "Back", -252, -238, 230, 58, Card);
            _startMatchButton = ActionButton("Btn_StartMatch", _choosePlayersPanel.transform, "START MATCH", 130, -238, 476, 58, Gold);

            _mapsPanel = Page("Maps");
            Label(_mapsPanel.transform, "COURSE SELECTION", 0, 270, 850, 46, 32, Cream);
            Label(_mapsPanel.transform, "Select an available course to play or preview upcoming tracks.", 0, 234, 940, 26, 16, new Color(.72f,.83f,.80f));

            // Map 1: Sunset Coastal (Playable Launch Map)
            var card1 = Box("MapCard_Coastal", _mapsPanel.transform, -366, 10, 232, 345, Card);
            Box("Accent1", card1.transform, 0, 170, 232, 4, new Color(.35f,.75f,.95f,1));
            Label(card1.transform, "MAP 01", 0, 140, 210, 22, 13, new Color(.35f,.75f,.95f,1));
            Label(card1.transform, "SUNSET COASTAL", 0, 114, 210, 28, 19, Cream);
            Label(card1.transform, "[ AVAILABLE ]", 0, 86, 210, 22, 13, Green);
            Label(card1.transform, "3 Pits • Par 6\nSand Verge", 0, 50, 210, 34, 14, Cream);
            MultilineLabel(card1.transform, "Sunset boardwalk fairway with sandy verges, a coastal boundary rail, and a lighthouse island backdrop.", 0, -16, 204, 76, 13, new Color(.75f,.86f,.84f), TextAnchor.MiddleCenter);
            _btnSelectMapCoastal = ActionButton("Btn_SelectMap_sunset_coastal", card1.transform, "PLAY COURSE", 0, -125, 200, 48, Gold);

            // Map 2: Temple Courtyard (Coming Soon)
            var card2 = Box("MapCard_Temple", _mapsPanel.transform, -122, 10, 232, 345, new Color(.08f,.17f,.17f,1));
            Box("Accent2", card2.transform, 0, 170, 232, 4, new Color(.85f,.45f,.85f,1));
            Label(card2.transform, "MAP 02", 0, 140, 210, 22, 13, new Color(.85f,.45f,.85f,1));
            Label(card2.transform, "TEMPLE COURTYARD", 0, 114, 210, 28, 18, Cream);
            Label(card2.transform, "[ COMING SOON ]", 0, 86, 210, 22, 13, new Color(.85f,.65f,.25f,1));
            Label(card2.transform, "3 Pits • Par 6\nFlagstone", 0, 50, 210, 34, 14, new Color(.65f,.75f,.72f));
            MultilineLabel(card2.transform, "Ancient flagstone courtyard with carved pillars, bank shot walls, and sharp angles.", 0, -16, 204, 76, 13, new Color(.55f,.68f,.65f), TextAnchor.MiddleCenter);
            _btnSelectMapTemple = ActionButton("Btn_SelectMap_temple_courtyard", card2.transform, "COMING SOON", 0, -125, 200, 48, Ink);
            _btnSelectMapTemple.interactable = false;

            // Map 3: Mountain Quarry (Coming Soon)
            var card3 = Box("MapCard_Quarry", _mapsPanel.transform, 122, 10, 232, 345, new Color(.08f,.17f,.17f,1));
            Box("Accent3", card3.transform, 0, 170, 232, 4, new Color(.95f,.55f,.35f,1));
            Label(card3.transform, "MAP 03", 0, 140, 210, 22, 13, new Color(.95f,.55f,.35f,1));
            Label(card3.transform, "MOUNTAIN QUARRY", 0, 114, 210, 28, 18, Cream);
            Label(card3.transform, "[ COMING SOON ]", 0, 86, 210, 22, 13, new Color(.85f,.65f,.25f,1));
            Label(card3.transform, "4 Pits • Par 8\nQuarry Stone", 0, 50, 210, 34, 14, new Color(.65f,.75f,.72f));
            MultilineLabel(card3.transform, "Highland quarry course with rugged elevation drops, shale, and rocky obstacles.", 0, -16, 204, 76, 13, new Color(.55f,.68f,.65f), TextAnchor.MiddleCenter);
            _btnSelectMapQuarry = ActionButton("Btn_SelectMap_mountain_quarry", card3.transform, "COMING SOON", 0, -125, 200, 48, Ink);
            _btnSelectMapQuarry.interactable = false;

            // Map 4: Village (Coming Soon — rebuild planned)
            var card4 = Box("MapCard_Village", _mapsPanel.transform, 366, 10, 232, 345, new Color(.08f,.17f,.17f,1));
            Box("Accent4", card4.transform, 0, 170, 232, 4, new Color(.95f,.77f,.25f,1));
            Label(card4.transform, "MAP 04", 0, 140, 210, 22, 13, new Color(.95f,.77f,.25f,1));
            Label(card4.transform, "VILLAGE", 0, 114, 210, 28, 20, Cream);
            Label(card4.transform, "[ COMING SOON ]", 0, 86, 210, 22, 13, new Color(.85f,.65f,.25f,1));
            Label(card4.transform, "3 Pits • Par 6\nEarth Track", 0, 50, 210, 34, 14, new Color(.65f,.75f,.72f));
            MultilineLabel(card4.transform, "A rebuilt village fairway with earth embankments and stone boundary walls.", 0, -16, 204, 76, 13, new Color(.55f,.68f,.65f), TextAnchor.MiddleCenter);
            _btnSelectMapVillage = ActionButton("Btn_SelectMap_village", card4.transform, "COMING SOON", 0, -125, 200, 48, Ink);
            _btnSelectMapVillage.interactable = false;

            _closeMapsButton = ActionButton("Btn_CloseMaps", _mapsPanel.transform, "Back to Menu", 0, -242, 320, 56, Gold);

            _settingsPanel = Page("Settings");
            Label(_settingsPanel.transform, "SETTINGS", 0, 266, 850, 48, 34, Cream);
            Label(_settingsPanel.transform, "Audio balances, engine performance, and preferences", 0, 230, 940, 28, 17, new Color(.72f,.83f,.80f));

            // Settings Category Tab Bar
            _tabAudioBtn = ActionButton("Btn_Tab_Audio", _settingsPanel.transform, "AUDIO", -340, 186, 156, 42, Gold);
            _tabGameplayBtn = ActionButton("Btn_Tab_Gameplay", _settingsPanel.transform, "GAMEPLAY", -170, 186, 156, 42, Card);
            _tabDisplayBtn = ActionButton("Btn_Tab_Display", _settingsPanel.transform, "DISPLAY", 0, 186, 156, 42, Card);
            _tabAccessibilityBtn = ActionButton("Btn_Tab_Accessibility", _settingsPanel.transform, "ACCESS", 170, 186, 156, 42, Card);
            _tabOtherBtn = ActionButton("Btn_Tab_Other", _settingsPanel.transform, "MORE", 340, 186, 156, 42, Card);

            var settingsCard = Box("SettingsContainer", _settingsPanel.transform, 0, -20, 860, 334, Card);

            // 1. Audio Subpanel
            _audioSettingsPanel = Box("AudioSettingsPanel", settingsCard.transform, 0, 0, 860, 334, new Color(0,0,0,0));

            Label(_audioSettingsPanel.transform, "MASTER VOLUME", -180, 114, 320, 26, 18, Cream);
            _masterVolumeText = Label(_audioSettingsPanel.transform, "100%", 260, 114, 100, 26, 18, Gold);
            _masterVolumeSlider = CreateSlider("Slider_Master", _audioSettingsPanel.transform, 0, 84, 660, 26);

            Label(_audioSettingsPanel.transform, "MUSIC VOLUME", -180, 46, 320, 26, 18, Cream);
            _musicVolumeText = Label(_audioSettingsPanel.transform, "100%", 260, 46, 100, 26, 18, Gold);
            _musicVolumeSlider = CreateSlider("Slider_Music", _audioSettingsPanel.transform, 0, 16, 660, 26);

            Label(_audioSettingsPanel.transform, "SOUND EFFECTS (SFX)", -180, -22, 320, 26, 18, Cream);
            _sfxVolumeText = Label(_audioSettingsPanel.transform, "100%", 260, -22, 100, 26, 18, Gold);
            _sfxVolumeSlider = CreateSlider("Slider_SFX", _audioSettingsPanel.transform, 0, -52, 660, 26);

            _resetAudioBtn = ActionButton("Btn_ResetAudio", _audioSettingsPanel.transform, "RESET AUDIO DEFAULTS", 0, -114, 320, 42, Ink);

            // 2. Gameplay Subpanel
            _gameplaySettingsPanel = Box("GameplaySettingsPanel", settingsCard.transform, 0, 0, 860, 334, new Color(0,0,0,0));
            var vibCard = Box("VibCard", _gameplaySettingsPanel.transform, 0, 60, 760, 94, Ink);
            MultilineLabel(vibCard.transform, "HAPTIC FEEDBACK / VIBRATION", -120, 20, 480, 26, 18, Cream, TextAnchor.MiddleLeft);
            MultilineLabel(vibCard.transform, "Provides tactile vibration on marble strikes, bank collisions, and pocketing.", -120, -15, 480, 40, 14, new Color(.62f,.75f,.72f), TextAnchor.MiddleLeft);
            _hapticsToggleButton = ActionButton("Btn_Haptics", vibCard.transform, "VIBRATION: ENABLED", 260, 0, 185, 52, Green);
            _hapticsToggleText = _hapticsToggleButton.GetComponentInChildren<Text>();

            var aimCard = Box("AimCard", _gameplaySettingsPanel.transform, 0, -55, 760, 94, Ink);
            MultilineLabel(aimCard.transform, "AIM TRAJECTORY ASSIST", -120, 20, 480, 26, 18, Cream, TextAnchor.MiddleLeft);
            MultilineLabel(aimCard.transform, "Active trajectory guide (50% range). Deterministic physics prediction for all players.", -120, -15, 480, 40, 14, new Color(.62f,.75f,.72f), TextAnchor.MiddleLeft);
            var aimPill = Box("AimPill", aimCard.transform, 260, 0, 185, 52, Card);
            Label(aimPill.transform, "ACTIVE (50%)", 0, 0, 175, 30, 17, Gold);
            _gameplaySettingsPanel.SetActive(false);

            // 3. Display Subpanel
            _displaySettingsPanel = Box("DisplaySettingsPanel", settingsCard.transform, 0, 0, 860, 334, new Color(0,0,0,0));
            Label(_displaySettingsPanel.transform, "GRAPHICS FIDELITY & PERFORMANCE PRESET", 0, 118, 640, 26, 18, Cream);

            _btnQualityLow = ActionButton("Btn_QualityLow", _displaySettingsPanel.transform, "PERFORMANCE\n30 FPS", -245, 52, 225, 68, Card);
            _btnQualityMed = ActionButton("Btn_QualityMed", _displaySettingsPanel.transform, "BALANCED\n60 FPS", 0, 52, 225, 68, Green);
            _btnQualityHigh = ActionButton("Btn_QualityHigh", _displaySettingsPanel.transform, "HIGH FIDELITY\n60 FPS MAX", 245, 52, 225, 68, Card);

            var statsBox = Box("StatsBox", _displaySettingsPanel.transform, 0, -42, 760, 74, Ink);
            _displayInfoText = MultilineLabel(statsBox.transform, "TARGET: 60 FPS • Balanced Mode\nDEVICE: Unity Universal Render Pipeline", 0, 0, 720, 64, 14, Cream, TextAnchor.MiddleCenter);

            Label(_displaySettingsPanel.transform, "Changes take effect instantly. Preserves responsive physics and smooth mobile performance.", 0, -114, 760, 26, 14, new Color(.62f,.75f,.72f));
            _displaySettingsPanel.SetActive(false);

            // 4. Accessibility Subpanel (Future-Proof Roadmap)
            _accessibilitySettingsPanel = Box("AccessSettingsPanel", settingsCard.transform, 0, 0, 860, 334, new Color(0,0,0,0));
            var accessBox = Box("AccessBox", _accessibilitySettingsPanel.transform, 0, 0, 760, 270, Ink);
            Label(accessBox.transform, "ACCESSIBILITY (UPCOMING ROADMAP)", 0, 95, 700, 30, 20, Gold);
            string accessRoadmap =
                "• High-Contrast Marble Outlines: Visual highlights for enhanced marble legibility.\n" +
                "• Scalable HUD Elements: Adjustable power gauge and score meter sizes.\n" +
                "• Colorblind Assistance: Alternative high-contrast palettes for Player 1 - 4.\n" +
                "• Vibration Cues: Distinct haptic pulses for pit entry versus foul events.\n\n" +
                "This architecture is ready for future accessibility updates without menu restructuring.";
            MultilineLabel(accessBox.transform, accessRoadmap, 0, -20, 700, 180, 15, Cream, TextAnchor.UpperLeft);
            _accessibilitySettingsPanel.SetActive(false);

            // 5. Other Subpanel
            _otherSettingsPanel = Box("OtherSettingsPanel", settingsCard.transform, 0, 0, 860, 334, new Color(0,0,0,0));
            var langBox = Box("LangBox", _otherSettingsPanel.transform, 0, 102, 760, 48, Ink);
            Label(langBox.transform, "LANGUAGE: ENGLISH (UNITED STATES)", 0, 0, 600, 30, 16, Cream);

            _btnPrivacyPolicy = ActionButton("Btn_Privacy", _otherSettingsPanel.transform, "PRIVACY POLICY", -250, 32, 230, 56, Card);
            _btnTermsOfService = ActionButton("Btn_Terms", _otherSettingsPanel.transform, "TERMS OF SERVICE", 0, 32, 230, 56, Card);
            _btnCredits = ActionButton("Btn_Credits", _otherSettingsPanel.transform, "CREDITS", 250, 32, 230, 56, Card);

            var studioBox = Box("StudioBox", _otherSettingsPanel.transform, 0, -64, 760, 68, Ink);
            Label(studioBox.transform, "Pit Striker v1.0.0  •  Studio xtinex  •  Offline Single & Local Pass-and-Play", 0, 12, 710, 24, 15, Gold);
            Label(studioBox.transform, "Zero telemetry • 100% offline • Physics predictions run strictly locally on device", 0, -14, 710, 24, 13, new Color(.62f,.75f,.72f));
            _otherSettingsPanel.SetActive(false);

            _closeSettingsButton = ActionButton("Btn_CloseSettings", _settingsPanel.transform, "Back", 0, -252, 320, 64, Gold);

            _rulesModal = Page("HowToPlay");
            Label(_rulesModal.transform, "HOW TO PLAY", 0, 270, 850, 48, 34, Cream);
            Label(_rulesModal.transform, "Master the five fundamental mechanics of village marble striking.", 0, 234, 940, 26, 17, new Color(.72f,.83f,.80f));

            // Row 1 (3 steps)
            var ruleCard1 = Box("RuleCard_01", _rulesModal.transform, -336, 118, 320, 164, Card);
            Label(ruleCard1.transform, "01  POSITION", 0, 54, 290, 28, 19, Gold);
            MultilineLabel(ruleCard1.transform, "Place your marble anywhere behind the baseline, or play from your current lie on the track.", 0, -12, 290, 95, 14, Cream, TextAnchor.MiddleCenter);

            var ruleCard2 = Box("RuleCard_02", _rulesModal.transform, 0, 118, 320, 164, Card);
            Label(ruleCard2.transform, "02  AIM VECTOR", 0, 54, 290, 28, 19, Gold);
            MultilineLabel(ruleCard2.transform, "Drag back to align your target vector directly towards the active pit or opponent marble.", 0, -12, 290, 95, 14, Cream, TextAnchor.MiddleCenter);

            var ruleCard3 = Box("RuleCard_03", _rulesModal.transform, 336, 118, 320, 164, Card);
            Label(ruleCard3.transform, "03  PULL & POWER", 0, 54, 290, 28, 19, Gold);
            MultilineLabel(ruleCard3.transform, "Hold and pull farther back to meter strike velocity accurately on the power gauge.", 0, -12, 290, 95, 14, Cream, TextAnchor.MiddleCenter);

            // Row 2 (2 steps)
            var ruleCard4 = Box("RuleCard_04", _rulesModal.transform, -252, -56, 488, 146, Card);
            Label(ruleCard4.transform, "04  RELEASE & STRIKE", 0, 46, 450, 28, 19, Gold);
            MultilineLabel(ruleCard4.transform, "Release finger to strike. Bank off stone boundary walls to navigate past terrain obstacles.", 0, -14, 450, 70, 14, Cream, TextAnchor.MiddleCenter);

            var ruleCard5 = Box("RuleCard_05", _rulesModal.transform, 252, -56, 488, 146, Card);
            Label(ruleCard5.transform, "05  SCORE & WIN", 0, 46, 450, 28, 19, Gold);
            MultilineLabel(ruleCard5.transform, "Sink Pit 1, Pit 2, then Pit 3 in order. Sinking a target pit or hitting an opponent earns a bonus shot!", 0, -14, 450, 70, 14, Cream, TextAnchor.MiddleCenter);

            _closeRulesButton = ActionButton("Btn_CloseRules", _rulesModal.transform, "Back", 0, -242, 320, 56, Gold);

            _pauseModal = Page("Pause");
            Label(_pauseModal.transform, "MATCH PAUSED", 0, 238, 800, 56, 38, Cream);
            Label(_pauseModal.transform, "Take your time. Your turn will be waiting.", 0, 186, 850, 32, 19, new Color(.72f,.83f,.80f));
            _pauseResumeButton = ActionButton("Btn_Resume", _pauseModal.transform, "RESUME MATCH", 0, 100, 450, 64, Gold);
            _pauseRestartButton = ActionButton("Btn_Restart", _pauseModal.transform, "Restart match", 0, 26, 450, 56, Green);
            _pauseSettingsButton = ActionButton("Btn_PauseSettings", _pauseModal.transform, "Settings", 0, -42, 450, 56, Card);
            _pauseRulesButton = ActionButton("Btn_PauseRules", _pauseModal.transform, "How to play", 0, -110, 450, 56, Card);
            _pauseHomeButton = ActionButton("Btn_HomeMenu", _pauseModal.transform, "Main menu", 0, -178, 450, 56, Ink);
            _hudPauseButton = ActionButton("Btn_HUD_Pause", _safeFrame, "II  PAUSE", 474, 276, 124, 46, Ink);

            _confirmation = Page("ConfirmAction");
            var confirmImg = _confirmation.GetComponent<Image>();
            confirmImg.color = new Color(.025f,.07f,.07f,1);
            confirmImg.raycastTarget = true;
            _confirmTitle = Label(_confirmation.transform, "Leave this match?", 0, 116, 950, 70, 36, Cream);
            _confirmBody = Label(_confirmation.transform, "", 0, 16, 850, 96, 24, Cream);
            var btnCancel = ActionButton("Btn_Cancel", _confirmation.transform, "Cancel", -140, -110, 240, 64, Card);
            btnCancel.onClick.AddListener(CancelConfirmation);
            var btnConfirm = ActionButton("Btn_Confirm", _confirmation.transform, "Confirm", 140, -110, 240, 64, Gold);
            btnConfirm.onClick.AddListener(() => { var a = _confirmedAction; CancelConfirmation(); a?.Invoke(); });

            // Legal & Information Overlay Modal
            _infoModal = Box("InfoModal", _safeFrame, 0, 0, 1080, 640, new Color(.02f,.06f,.06f,.95f));
            _infoModal.GetComponent<Image>().raycastTarget = true;
            var infoCard = Box("InfoCard", _infoModal.transform, 0, 20, 840, 440, Ink);
            _infoTitle = Label(infoCard.transform, "Information", 0, 175, 780, 40, 26, Gold);
            var infoBodyBox = Box("InfoBodyBox", infoCard.transform, 0, 8, 780, 270, Card);
            _infoBody = MultilineLabel(infoBodyBox.transform, "", 0, 0, 730, 245, 16, Cream, TextAnchor.UpperLeft);
            _closeInfoBtn = ActionButton("Btn_CloseInfo", infoCard.transform, "Back to Settings", 0, -165, 280, 52, Gold);
            _infoModal.SetActive(false);
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

            BuildOnlineUI();
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
        private static Text MultilineLabel(Transform parent, string text, float x, float y, float w, float h, int size, Color color, TextAnchor align = TextAnchor.UpperLeft)
        {
            var obj = CreateText("MultilineLabel", parent, text, size, FontStyle.Normal, color, align);
            Position(obj, x, y, w, h);
            var t = obj.GetComponent<Text>();
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
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
        public void FitSafeArea()
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
        private static Slider CreateSlider(string name, Transform parent, float x, float y, float w, float h)
        {
            GameObject sliderObj = new GameObject(name, typeof(RectTransform));
            sliderObj.transform.SetParent(parent, false);
            Position(sliderObj, x, y, w, h);

            // Background track
            GameObject bgObj = CreateImage("Background", sliderObj.transform, new Color(0.04f, 0.10f, 0.10f, 1f));
            RectTransform bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 0.32f);
            bgRect.anchorMax = new Vector2(1, 0.68f);
            bgRect.sizeDelta = Vector2.zero;
            bgRect.anchoredPosition = Vector2.zero;

            // Fill Area
            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0, 0.32f);
            fillAreaRect.anchorMax = new Vector2(1, 0.68f);
            fillAreaRect.sizeDelta = new Vector2(-16, 0);
            fillAreaRect.anchoredPosition = Vector2.zero;

            GameObject fill = CreateImage("Fill", fillArea.transform, Gold);
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.sizeDelta = Vector2.zero;

            // Handle Slide Area
            GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(sliderObj.transform, false);
            RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.sizeDelta = new Vector2(-24, 0);
            handleAreaRect.anchoredPosition = Vector2.zero;

            GameObject handle = CreateImage("Handle", handleArea.transform, Cream);
            handle.GetComponent<Image>().raycastTarget = true;
            RectTransform handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(28, 28);
            var shadow = handle.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.35f);
            shadow.effectDistance = new Vector2(0, -2);

            Slider slider = sliderObj.AddComponent<Slider>();
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;

            var nav = slider.navigation;
            nav.mode = Navigation.Mode.None;
            slider.navigation = nav;

            return slider;
        }

        public void NavigateBack()
        {
            if (_infoModal && _infoModal.activeSelf) { CloseInfoModal(); return; }
            if (_confirmation && _confirmation.activeSelf) { CancelConfirmation(); return; }
            switch (CurrentScreen)
            {
                case ScreenType.InGame: HandlePauseClicked(); break;
                case ScreenType.Pause: HandleResumeClicked(); break;
                case ScreenType.Rules: ShowScreen(_rulesReturn); break;
                case ScreenType.Settings: ShowScreen(_settingsReturn); break;
                case ScreenType.Maps: ShowScreen(ScreenType.Home); break;
                case ScreenType.ChoosePlayers: ShowScreen(ScreenType.PlayModeSelect); break;
                case ScreenType.PlayModeSelect: ShowScreen(ScreenType.Home); break;
                case ScreenType.OnlineMenu: ShowScreen(ScreenType.PlayModeSelect); break;
                case ScreenType.CreateMatch: HandleCancelOnlineMatch(); break;
                case ScreenType.JoinMatch: ShowScreen(ScreenType.OnlineMenu); break;
                case ScreenType.OnlineLobby: HandleCancelOnlineMatch(); break;
                case ScreenType.OnlineResult: HandleCancelOnlineMatch(); break;
                case ScreenType.QuickMatch: HandleCancelQuickMatch(); break;
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
