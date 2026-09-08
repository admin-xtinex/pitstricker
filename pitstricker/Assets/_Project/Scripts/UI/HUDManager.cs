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

            if (_strikeButton != null)
            {
                _strikeButton.onClick.AddListener(HandleStrikeClicked);
            }

            if (_powerSlider != null)
            {
                _powerSlider.onValueChanged.AddListener(HandleSliderValueChanged);
            }
        }

        private void OnEnable()
        {
            SwipeLaunchController.OnPowerChanged += HandlePowerChanged;
            PitZone.OnMarbleSunk += HandleMarbleSunk;
        }

        private void OnDisable()
        {
            SwipeLaunchController.OnPowerChanged -= HandlePowerChanged;
            PitZone.OnMarbleSunk -= HandleMarbleSunk;
        }

        private void Start()
        {
            UpdateObjectiveUI();
            HandlePowerChanged(0f);
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

        private void HandleMarbleSunk(PitZone pit, Physics.MarbleController marble)
        {
            if (pit.PitNumber == _currentObjectivePit)
            {
                Debug.Log($"<color=#00FF88><b>[PROGRESSION]</b> Objective Complete! Pit {_currentObjectivePit} conquered!</color>");
                _currentObjectivePit++;

                if (_currentObjectivePit > 3)
                {
                    if (_statusBanner != null) _statusBanner.text = "★ VICTORY! ALL PITS CONQUERED! ★";
                }
                else
                {
                    if (_statusBanner != null) _statusBanner.text = $"TARGET: PIT {_currentObjectivePit}";
                }

                UpdateObjectiveUI();
            }
            else
            {
                Debug.LogWarning($"[PROGRESSION] Sunk into Pit #{pit.PitNumber}, but current target is Pit #{_currentObjectivePit}!");
                if (_statusBanner != null)
                {
                    _statusBanner.text = $"Wrong Pit! Next target is Pit {_currentObjectivePit}";
                }
            }
        }

        private void UpdateObjectiveUI()
        {
            Color activeColor = new Color(0f, 0.85f, 1f, 1f);
            Color completedColor = new Color(0.2f, 0.9f, 0.3f, 1f);
            Color lockedColor = new Color(0.3f, 0.3f, 0.35f, 0.6f);

            if (_bead1Image != null) _bead1Image.color = _currentObjectivePit > 1 ? completedColor : (_currentObjectivePit == 1 ? activeColor : lockedColor);
            if (_bead2Image != null) _bead2Image.color = _currentObjectivePit > 2 ? completedColor : (_currentObjectivePit == 2 ? activeColor : lockedColor);
            if (_bead3Image != null) _bead3Image.color = _currentObjectivePit > 3 ? completedColor : (_currentObjectivePit == 3 ? activeColor : lockedColor);

            if (_statusBanner != null && _currentObjectivePit <= 3)
            {
                _statusBanner.text = $"YOUR TURN  •  TARGET: PIT {_currentObjectivePit}";
            }
        }
    }
}
