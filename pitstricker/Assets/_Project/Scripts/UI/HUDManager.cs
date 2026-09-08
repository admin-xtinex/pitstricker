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
            TurnManager.OnStrokeCountChanged += HandleStrokeCountChanged;
            TurnManager.OnTargetPitChanged += HandleTargetPitChanged;
            TurnManager.OnMatchWon += HandleMatchWon;
            TurnManager.OnStatusMessage += HandleStatusMessage;
        }

        private void OnDisable()
        {
            SwipeLaunchController.OnPowerChanged -= HandlePowerChanged;
            TurnManager.OnStrokeCountChanged -= HandleStrokeCountChanged;
            TurnManager.OnTargetPitChanged -= HandleTargetPitChanged;
            TurnManager.OnMatchWon -= HandleMatchWon;
            TurnManager.OnStatusMessage -= HandleStatusMessage;
        }

        private void Start()
        {
            UpdateObjectiveUI();
            HandlePowerChanged(0f);
            HandleStrokeCountChanged(0, 0);
            if (_victoryModal != null) _victoryModal.SetActive(false);
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

        private void HandleMatchWon(int totalStrokes, int coursePar, string rating)
        {
            if (_victoryModal != null)
            {
                _victoryModal.SetActive(true);
            }
            else
            {
                CreateRuntimeVictoryModal(totalStrokes, coursePar, rating);
            }

            if (_victoryStrokesText != null)
            {
                _victoryStrokesText.text = $"TOTAL STROKES: {totalStrokes}";
            }

            if (_victoryParText != null)
            {
                _victoryParText.text = $"COURSE PAR: {coursePar}";
            }

            if (_victoryRatingText != null)
            {
                _victoryRatingText.text = $"RATING: {rating}";
            }
        }

        private void CreateRuntimeVictoryModal(int totalStrokes, int coursePar, string rating)
        {
            GameObject modalObj = new GameObject("Victory_Modal_Runtime");
            modalObj.transform.SetParent(transform, false);
            RectTransform modalRect = modalObj.AddComponent<RectTransform>();
            modalRect.anchorMin = new Vector2(0.5f, 0.5f);
            modalRect.anchorMax = new Vector2(0.5f, 0.5f);
            modalRect.pivot = new Vector2(0.5f, 0.5f);
            modalRect.anchoredPosition = Vector2.zero;
            modalRect.sizeDelta = new Vector2(500f, 320f);

            Image modalBg = modalObj.AddComponent<Image>();
            modalBg.color = new Color(0.04f, 0.06f, 0.1f, 0.96f);

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(modalObj.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0.7f);
            titleRect.anchorMax = new Vector2(1f, 0.95f);
            titleRect.sizeDelta = Vector2.zero;
            Text title = titleObj.AddComponent<Text>();
            title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            title.fontSize = 28;
            title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.MiddleCenter;
            title.color = new Color(1f, 0.85f, 0.2f, 1f);
            title.text = "★ VICTORY! ★";

            // Stats
            GameObject statsObj = new GameObject("Stats");
            statsObj.transform.SetParent(modalObj.transform, false);
            RectTransform statsRect = statsObj.AddComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0f, 0.25f);
            statsRect.anchorMax = new Vector2(1f, 0.7f);
            statsRect.sizeDelta = Vector2.zero;
            Text stats = statsObj.AddComponent<Text>();
            stats.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            stats.fontSize = 20;
            stats.fontStyle = FontStyle.Bold;
            stats.alignment = TextAnchor.MiddleCenter;
            stats.color = Color.white;
            stats.text = $"TOTAL STROKES: {totalStrokes}\nCOURSE PAR: {coursePar}\nRATING: {rating}";

            // Play Again Button
            GameObject btnObj = new GameObject("PlayAgainBtn");
            btnObj.transform.SetParent(modalObj.transform, false);
            RectTransform btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.25f, 0.06f);
            btnRect.anchorMax = new Vector2(0.75f, 0.22f);
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
