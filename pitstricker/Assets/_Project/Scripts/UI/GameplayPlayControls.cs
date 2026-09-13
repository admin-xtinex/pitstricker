using UnityEngine;
using UnityEngine.UI;
using PitStriker.Input;
using PitStriker.Gameplay;

namespace PitStriker.UI
{
    [DefaultExecutionOrder(80)]
    public class GameplayPlayControls : MonoBehaviour
    {
        private Button _strikeButton;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            HUDManager hud = Object.FindAnyObjectByType<HUDManager>();
            if (hud == null) return;
            if (hud.GetComponent<GameplayPlayControls>() == null)
                hud.gameObject.AddComponent<GameplayPlayControls>();
        }

        private void Awake()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Transform root = transform;
            Transform existing = root.Find("Btn_Strike");
            if (existing != null)
                _strikeButton = existing.GetComponent<Button>();

            if (_strikeButton == null)
            {
                GameObject btnObj = new GameObject("Btn_Strike");
                btnObj.transform.SetParent(root, false);
                RectTransform rect = btnObj.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(1f, 0f);
                rect.anchorMax = new Vector2(1f, 0f);
                rect.pivot = new Vector2(1f, 0f);
                rect.sizeDelta = new Vector2(132f, 132f);
                rect.anchoredPosition = new Vector2(-28f, 28f);
                Image img = btnObj.AddComponent<Image>();
                img.color = new Color(0.10f, 0.14f, 0.20f, 0.82f);
                _strikeButton = btnObj.AddComponent<Button>();
                GameObject labelObj = new GameObject("Text");
                labelObj.transform.SetParent(btnObj.transform, false);
                RectTransform labelRect = labelObj.AddComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.sizeDelta = Vector2.zero;
                Text label = labelObj.AddComponent<Text>();
                label.font = font;
                label.text = "STRIKE";
                label.fontSize = 20;
                label.fontStyle = FontStyle.Bold;
                label.alignment = TextAnchor.MiddleCenter;
                label.color = Color.white;
                label.raycastTarget = false;
            }

            _strikeButton.onClick.RemoveListener(HandleStrike);
            _strikeButton.onClick.AddListener(HandleStrike);
            if (root.Find("Hint_Swipe") != null) return;

            GameObject hint = new GameObject("Hint_Swipe");
            hint.transform.SetParent(root, false);
            RectTransform hintRect = hint.AddComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0f, 0f);
            hintRect.anchorMax = new Vector2(0f, 0f);
            hintRect.pivot = new Vector2(0f, 0f);
            hintRect.sizeDelta = new Vector2(210f, 74f);
            hintRect.anchoredPosition = new Vector2(20f, 24f);
            Image hintBg = hint.AddComponent<Image>();
            hintBg.color = new Color(0.05f, 0.08f, 0.10f, 0.42f);
            hintBg.raycastTarget = false;
            GameObject hintTextObj = new GameObject("Text");
            hintTextObj.transform.SetParent(hint.transform, false);
            RectTransform hintTextRect = hintTextObj.AddComponent<RectTransform>();
            hintTextRect.anchorMin = Vector2.zero;
            hintTextRect.anchorMax = Vector2.one;
            hintTextRect.offsetMin = new Vector2(10f, 6f);
            hintTextRect.offsetMax = new Vector2(-10f, -6f);
            Text hintText = hintTextObj.AddComponent<Text>();
            hintText.font = font;
            hintText.text = "Swipe or drag\nto aim";
            hintText.fontSize = 15;
            hintText.fontStyle = FontStyle.Bold;
            hintText.alignment = TextAnchor.MiddleLeft;
            hintText.color = new Color(1f, 1f, 1f, 0.85f);
            hintText.raycastTarget = false;
        }

        private void Update()
        {
            if (_strikeButton != null)
                _strikeButton.interactable = TurnManager.CanAim();
        }

        private void HandleStrike()
        {
            if (!TurnManager.CanAim()) return;
            if (TurnManager.Instance != null && TurnManager.Instance.ActivePlayer != null && TurnManager.Instance.ActivePlayer.isAI)
                return;
            if (SwipeLaunchController.Instance != null)
                SwipeLaunchController.Instance.LaunchStrike(0.65f);
        }
    }
}
