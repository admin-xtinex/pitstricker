using UnityEngine;
using UnityEngine.UI;
using PitStriker.Input;
using PitStriker.Gameplay;

namespace PitStriker.UI
{
    /// <summary>
    /// Phase 3 HUD Manager:
    /// Displays real-time Power Meter, Objective Stage Beads (1 -> 2 -> 3),
    /// and Strike controls matching the Concept Art layout.
    /// </summary>
    public class HUDManager : MonoBehaviour
    {
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

        // Current Stage Progression (1 -> 2 -> 3)
        private int _currentObjectivePit = 1;

        private void Awake()
        {
            // Runtime Safety Check: Replace legacy StandaloneInputModule if present to prevent Unity 6 New Input System exceptions
            var es = Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
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
                TurnManager existingTm = Object.FindFirstObjectByType<TurnManager>();
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

            if (_victoryModal != null)
            {
                _victoryModal.SetActive(false);
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
        }

        private void OnDisable()
        {
            SwipeLaunchController.OnPowerChanged -= HandlePowerChanged;
            TurnManager.OnActivePlayerChanged -= HandleActivePlayerChanged;
            TurnManager.OnStrokeCountChanged -= HandleStrokeCountChanged;
            TurnManager.OnTargetPitChanged -= HandleTargetPitChanged;
            TurnManager.OnMatchVictory -= HandleMatchVictory;
            TurnManager.OnStatusMessage -= HandleStatusMessage;
        }

        private void Start()
        {
            UpdateObjectiveUI();
            HandlePowerChanged(0f);
            HandleStrokeCountChanged(0, 8);
            if (_victoryModal != null) _victoryModal.SetActive(false);
        }

        private void HandleActivePlayerChanged(TurnManager.PlayerData activePlayer)
        {
            if (activePlayer == null) return;

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
            if (_victoryModal != null)
            {
                _victoryModal.SetActive(true);
            }
            else
            {
                CreateRuntimeVictoryModal(winner, leaderboard);
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
            modalRect.sizeDelta = new Vector2(520f, 360f);

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

            // Play Again Button
            GameObject btnObj = new GameObject("PlayAgainBtn");
            btnObj.transform.SetParent(modalObj.transform, false);
            RectTransform btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.25f, 0.05f);
            btnRect.anchorMax = new Vector2(0.75f, 0.2f);
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
            btnText.fontSize = 18;
            btnText.fontStyle = FontStyle.Bold;
            btnText.alignment = TextAnchor.MiddleCenter;
            btnText.color = Color.white;
            btnText.text = "PLAY AGAIN";

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
