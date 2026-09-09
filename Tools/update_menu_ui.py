from pathlib import Path
p=Path('pitstricker/Assets/_Project/Scripts/UI/MenuManager.cs')
s=p.read_text(encoding='utf-8')
s=s.replace('public class MenuManager : MonoBehaviour','public partial class MenuManager : MonoBehaviour')
a=s.index('        private void EnsureUIHierarchy()')
b=s.index('        private static GameObject CreatePanel',a)
s=s[:a]+'        private void EnsureUIHierarchy() { BuildModernUI(); }\n\n'+s[b:]
s=s.replace('            Debug.Log($"<color=#00FFAA><b>[MENU]</b> Navigating to screen: {screen}</color>");','            CurrentScreen = screen;\n            if (_screenBackdrop != null) _screenBackdrop.gameObject.SetActive(screen != ScreenType.InGame);\n            if (_confirmation != null) _confirmation.SetActive(false);\n            if (screen == ScreenType.Pause && TurnManager.Instance != null) TurnManager.Instance.SetPaused(true);')
s=s.replace('bool isPlaying = (screen == ScreenType.InGame || screen == ScreenType.Pause);','bool isPlaying = (screen == ScreenType.InGame || screen == ScreenType.Pause);')
s=s.replace('            if (_hudRoot != null) _hudRoot.SetActive(isPlaying);','            if (_hudRoot != null)\n            {\n                _hudRoot.SetActive(isPlaying);\n                var group = _hudRoot.GetComponent<CanvasGroup>();\n                if (!group) group = _hudRoot.AddComponent<CanvasGroup>();\n                group.interactable = screen == ScreenType.InGame;\n                group.blocksRaycasts = screen == ScreenType.InGame;\n            }')
s=s.replace('() => ShowScreen(ScreenType.Rules)','OpenRules')
s=s.replace('() => ShowScreen(ScreenType.Home));\n            }\n\n            // Player Count Tabs','() => ShowScreen(_rulesReturn));\n            }\n\n            // Player Count Tabs')
s=s.replace('_pauseRestartButton.onClick.AddListener(HandleRestartClicked);','_pauseRestartButton.onClick.AddListener(() => Confirm("Restart this match?", "Scores and marble positions will reset. Your players stay the same.", HandleRestartClicked));')
s=s.replace('_pauseHomeButton.onClick.AddListener(HandleHomeClicked);','_pauseHomeButton.onClick.AddListener(() => Confirm("Leave this match?", "This match will end and you will return to the main menu.", HandleHomeClicked));')
s=s.replace('if (slotIndex < 0 || slotIndex >= _isAISlot.Length) return;','if (slotIndex <= 0 || slotIndex >= _isAISlot.Length) return;')
s=s.replace('_playerSlotRoleTexts[i].text = isBot ? "AI BOT" : "REAL PLAYER";','_playerSlotRoleTexts[i].text = i == 0 ? "YOU / HUMAN" : isBot ? "COMPUTER  >" : "HUMAN  >";')
s=s.replace('            ShowScreen(ScreenType.InGame);\n\n            TurnManager tm', '            SaveSetup();\n            ShowScreen(ScreenType.InGame);\n\n            TurnManager tm')
s=s.replace('            UpdatePlayerCountUI();\n            UpdateSlotRowsUI();\n\n            // Default', '            LoadSetup();\n            UpdatePlayerCountUI();\n            UpdateSlotRowsUI();\n\n            // Default')
s=s.replace('            cb.normalColor = bgColor;','            btn.targetGraphic = img;\n            cb.normalColor = Color.white;')
s=s.replace('cb.highlightedColor = Color.Lerp(bgColor, Color.white, 0.2f);','cb.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);')
s=s.replace('cb.pressedColor = Color.Lerp(bgColor, Color.black, 0.2f);','cb.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);\n            cb.selectedColor = Color.white;\n            cb.fadeDuration = 0.08f;')
p.write_text(s,encoding='utf-8')
p=Path('pitstricker/Assets/_Project/Scripts/UI/HUDManager.cs'); s=p.read_text(encoding='utf-8')
a=s.index('        private void EnsureTopBarNavigationButtons()'); b=s.index('        private static GameObject CreateNavButton',a)
s=s[:a]+'''        private void EnsureTopBarNavigationButtons()
        {
            // Navigation belongs to MenuManager's always-visible overlay.
            var legacy = transform.Find("TopBar_Navigation");
            if (legacy) legacy.gameObject.SetActive(false);
            var oldPause = transform.Find("Btn_HUD_Pause");
            if (oldPause) oldPause.gameObject.SetActive(false);
        }

'''+s[b:]
p.write_text(s,encoding='utf-8')
