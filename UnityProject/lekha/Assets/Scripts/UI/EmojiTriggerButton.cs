using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace Lekha.UI
{
    /// <summary>
    /// Dedicated emoji trigger button - simplified to match working pause button pattern
    /// </summary>
    public class EmojiTriggerButton : MonoBehaviour
    {
        private Image bgImage;
        private Button button;

        public static EmojiTriggerButton Create(Transform parent, System.Action onClick)
        {
            Debug.Log("[EmojiTrigger] Creating button (simplified pattern)...");

            GameObject btnObj = new GameObject("EmojiTriggerButton");
            btnObj.transform.SetParent(parent, false);

            // Position DIRECTLY UNDER the Round label
            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1); // Top-left anchor
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(50, 50);
            rect.anchoredPosition = new Vector2(90, -95); // Under Round label

            // Background image - bright and visible
            Image bg = btnObj.AddComponent<Image>();
            if (ModernUITheme.Instance != null && ModernUITheme.Instance.CircleSprite != null)
            {
                bg.sprite = ModernUITheme.Instance.CircleSprite;
            }
            bg.color = new Color(0.2f, 0.25f, 0.45f, 0.98f); // Dark blue background

            // Button component - EXACTLY like pause button
            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = bg;

            ColorBlock colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.selectedColor = Color.white;
            btn.colors = colors;

            // Click handler
            btn.onClick.AddListener(() => {
                Debug.Log("[EmojiTrigger] >>> Button clicked! <<<");
                onClick?.Invoke();
            });

            // Shadow
            Shadow shadow = btnObj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.5f);
            shadow.effectDistance = new Vector2(2, -2);

            // Outline - yellow/gold accent to match the + icon
            Outline outline = btnObj.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.85f, 0.3f, 0.7f);
            outline.effectDistance = new Vector2(2, -2);

            // Icon text (using "☺" which is more widely supported than full emoji)
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(btnObj.transform, false);

            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI iconTmp = iconObj.AddComponent<TextMeshProUGUI>();
            iconTmp.text = "+"; // Plus icon to indicate "add reaction"
            iconTmp.fontSize = 28;
            iconTmp.fontStyle = FontStyles.Bold;
            iconTmp.alignment = TextAlignmentOptions.Center;
            iconTmp.color = new Color(1f, 0.85f, 0.3f, 1f); // Yellow/gold color
            iconTmp.raycastTarget = false;

            // Add component
            EmojiTriggerButton triggerBtn = btnObj.AddComponent<EmojiTriggerButton>();
            triggerBtn.bgImage = bg;
            triggerBtn.button = btn;

            Debug.Log($"[EmojiTrigger] Button created at {rect.anchoredPosition}, size {rect.sizeDelta}");

            return triggerBtn;
        }
    }
}
