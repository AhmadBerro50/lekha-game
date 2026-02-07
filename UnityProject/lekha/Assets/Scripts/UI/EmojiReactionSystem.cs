using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Lekha.Core;

namespace Lekha.UI
{
    /// <summary>
    /// Modern emoji reaction system for in-game player expression
    /// </summary>
    public class EmojiReactionSystem : MonoBehaviour
    {
        public static EmojiReactionSystem Instance { get; private set; }

        // Reaction data - sprite name and accent color for each (9 reactions for 3x3 grid)
        // Sprites loaded from Resources/Emojis/{name}
        private static readonly (string name, Color color)[] Reactions = new (string, Color)[]
        {
            ("laugh", new Color(1f, 0.85f, 0.2f)),       // Yellow - 😂
            ("angry", new Color(0.95f, 0.3f, 0.3f)),     // Red - 😤
            ("clap", new Color(0.3f, 0.9f, 0.4f)),       // Green - 👏
            ("sad", new Color(0.4f, 0.6f, 0.95f)),       // Blue - 😢
            ("cool", new Color(0.2f, 0.8f, 0.9f)),       // Cyan - 😎
            ("fire", new Color(1f, 0.5f, 0.2f)),         // Orange - 🔥
            ("heart_broken", new Color(0.9f, 0.4f, 0.6f)), // Pink - 💔
            ("party", new Color(0.7f, 0.4f, 0.95f)),     // Purple - 🎉
            ("thumbsup", new Color(0.3f, 0.7f, 1f))      // Blue - 👍
        };

        // Cached sprites
        private static Dictionary<string, Sprite> cachedSprites = new Dictionary<string, Sprite>();

        // UI References
        private GameObject selectionPanelObj;
        private CanvasGroup panelCanvasGroup;
        private bool isPanelOpen = false;
        private Coroutine autoCloseCoroutine;

        // Settings
        private const float AUTO_CLOSE_DELAY = 5f;

        // Track active emoji per player (only one at a time)
        private Dictionary<PlayerPosition, bool> playerHasActiveEmoji = new Dictionary<PlayerPosition, bool>();

        private void Awake()
        {
            Debug.Log("[EmojiSystem] Awake called!");

            if (Instance != null && Instance != this)
            {
                Debug.Log("[EmojiSystem] Another instance exists, destroying this one");
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Debug.Log("[EmojiSystem] Instance set to this");

            // Initialize tracking
            playerHasActiveEmoji[PlayerPosition.South] = false;
            playerHasActiveEmoji[PlayerPosition.North] = false;
            playerHasActiveEmoji[PlayerPosition.East] = false;
            playerHasActiveEmoji[PlayerPosition.West] = false;
        }

        private void OnDestroy()
        {
            Debug.Log($"[EmojiSystem] OnDestroy called! Stack trace:\n{System.Environment.StackTrace}");

            // Destroy UI elements
            if (selectionPanelObj != null)
            {
                Destroy(selectionPanelObj);
            }

            // Clear singleton reference when destroyed
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Initialize(Transform canvasParent)
        {
            Debug.Log("[EmojiSystem] Initialize called");
            CreateSelectionPanel(canvasParent);
            Debug.Log("[EmojiSystem] Initialize complete - panel created");
        }

        /// <summary>
        /// Initialize only the panel (button created separately in GameUI)
        /// </summary>
        public void InitializePanelOnly(Transform canvasParent)
        {
            Debug.Log("[EmojiSystem] InitializePanelOnly called");
            CreateSelectionPanel(canvasParent);
            Debug.Log("[EmojiSystem] Panel created successfully");
        }

        public void TogglePanel()
        {
            Debug.Log($"[EmojiSystem] TogglePanel called! isPanelOpen={isPanelOpen}");
            if (isPanelOpen)
                ClosePanel();
            else
                OpenPanel();
        }

        private void CreateSelectionPanel(Transform parent)
        {
            selectionPanelObj = new GameObject("EmojiSelectionPanel");
            selectionPanelObj.transform.SetParent(parent, false);

            // Move to end of hierarchy to render on top
            selectionPanelObj.transform.SetAsLastSibling();

            RectTransform panelRect = selectionPanelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(200, 220);
            panelRect.anchoredPosition = Vector2.zero;

            // NO nested Canvas - just use parent canvas
            // CanvasGroup for alpha control
            panelCanvasGroup = selectionPanelObj.AddComponent<CanvasGroup>();
            panelCanvasGroup.alpha = 1; // Start visible for testing!
            panelCanvasGroup.blocksRaycasts = true;
            panelCanvasGroup.interactable = true;

            // BRIGHT RED background for visibility testing
            Image panelBg = selectionPanelObj.AddComponent<Image>();
            panelBg.color = new Color(0.9f, 0.1f, 0.1f, 1f); // BRIGHT RED for testing
            panelBg.raycastTarget = true;

            // Grid layout
            GameObject layoutObj = new GameObject("EmojiLayout");
            layoutObj.transform.SetParent(selectionPanelObj.transform, false);
            RectTransform layoutRect = layoutObj.AddComponent<RectTransform>();
            layoutRect.anchorMin = Vector2.zero;
            layoutRect.anchorMax = Vector2.one;
            layoutRect.sizeDelta = Vector2.zero;
            layoutRect.anchoredPosition = Vector2.zero;

            GridLayoutGroup layout = layoutObj.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(50, 50);
            layout.spacing = new Vector2(6, 6);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 3;
            layout.padding = new RectOffset(8, 8, 8, 8);

            foreach (var reaction in Reactions)
            {
                CreateEmojiButton(layoutObj.transform, reaction.name, reaction.color);
            }

            Debug.Log($"[EmojiSystem] Panel created at {panelRect.anchoredPosition}, parent={parent.name}, size={panelRect.sizeDelta}");

            // DON'T hide on creation - leave visible to test if it renders at all
            // selectionPanelObj.SetActive(false);
        }

        private void CreateEmojiButton(Transform parent, string emojiName, Color accentColor)
        {
            GameObject btnObj = new GameObject($"Reaction_{emojiName}");
            btnObj.transform.SetParent(parent, false);

            RectTransform btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.sizeDelta = new Vector2(50, 50);

            // Background with accent color
            Image bg = btnObj.AddComponent<Image>();
            bg.color = new Color(accentColor.r * 0.3f, accentColor.g * 0.3f, accentColor.b * 0.3f, 0.95f);
            bg.raycastTarget = true;
            if (ModernUITheme.Instance != null && ModernUITheme.Instance.CircleSprite != null)
            {
                bg.sprite = ModernUITheme.Instance.CircleSprite;
            }

            // Colored outline/glow
            Outline outline = btnObj.AddComponent<Outline>();
            outline.effectColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.9f);
            outline.effectDistance = new Vector2(2, -2);

            // Emoji sprite image
            GameObject spriteObj = new GameObject("EmojiSprite");
            spriteObj.transform.SetParent(btnObj.transform, false);
            RectTransform spriteRect = spriteObj.AddComponent<RectTransform>();
            spriteRect.anchorMin = new Vector2(0.1f, 0.1f);
            spriteRect.anchorMax = new Vector2(0.9f, 0.9f);
            spriteRect.sizeDelta = Vector2.zero;
            spriteRect.anchoredPosition = Vector2.zero;

            Image emojiImage = spriteObj.AddComponent<Image>();
            emojiImage.sprite = GetEmojiSprite(emojiName);
            emojiImage.preserveAspect = true;
            emojiImage.raycastTarget = false;

            // Button
            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = bg;

            ColorBlock colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            btn.colors = colors;

            string capturedName = emojiName;
            btn.onClick.AddListener(() => OnEmojiSelected(capturedName));
        }

        /// <summary>
        /// Load emoji sprite from Resources/Emojis/{name}
        /// </summary>
        public static Sprite GetEmojiSprite(string emojiName)
        {
            if (cachedSprites.TryGetValue(emojiName, out Sprite cached))
                return cached;

            Sprite sprite = Resources.Load<Sprite>($"Emojis/{emojiName}");
            if (sprite != null)
            {
                cachedSprites[emojiName] = sprite;
            }
            else
            {
                Debug.LogWarning($"[EmojiSystem] Sprite not found: Resources/Emojis/{emojiName}");
            }
            return sprite;
        }

        private void OnEmojiSelected(string emoji)
        {
            Debug.Log($"[EmojiSystem] Emoji selected: {emoji}");

            // Check if player already has an active emoji
            if (playerHasActiveEmoji[PlayerPosition.South])
            {
                Debug.Log("[EmojiSystem] Player already has an active emoji, ignoring");
                return;
            }

            // Send emoji for the human player (South)
            ShowEmojiForPlayer(emoji, PlayerPosition.South);
            ClosePanel();
        }

        /// <summary>
        /// Get the accent color for an emoji reaction
        /// </summary>
        public static Color GetReactionColor(string emojiName)
        {
            foreach (var reaction in Reactions)
            {
                if (reaction.name == emojiName)
                    return reaction.color;
            }
            return new Color(1f, 0.85f, 0.2f); // Default yellow
        }

        /// <summary>
        /// Get a font that supports emoji rendering on the current platform
        /// </summary>
        public static Font GetEmojiFont(int size)
        {
            // Try platform-specific emoji fonts
            string[] fontNames;
#if UNITY_IOS
            fontNames = new[] { "Apple Color Emoji", "AppleColorEmoji", ".AppleColorEmojiUI" };
#elif UNITY_ANDROID
            fontNames = new[] { "NotoColorEmoji", "Noto Color Emoji", "SamsungColorEmoji" };
#else
            fontNames = new[] { "Segoe UI Emoji", "Apple Color Emoji", "Arial" };
#endif
            foreach (var fontName in fontNames)
            {
                Font font = Font.CreateDynamicFontFromOSFont(fontName, size);
                if (font != null)
                    return font;
            }

            // Last resort - use built-in font (emojis may not render)
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        public void ShowEmojiForPlayer(string emoji, PlayerPosition position)
        {
            // Check if this player already has an active emoji
            if (playerHasActiveEmoji.ContainsKey(position) && playerHasActiveEmoji[position])
            {
                return;
            }

            playerHasActiveEmoji[position] = true;

            // Show the emoji above the player's panel
            if (GameUI.Instance != null)
            {
                GameUI.Instance.ShowEmojiForPlayer(emoji, position);
            }

            // Clear the flag after the emoji expires (2.5 seconds)
            StartCoroutine(ClearEmojiFlag(position, 2.85f));
        }

        private IEnumerator ClearEmojiFlag(PlayerPosition position, float delay)
        {
            yield return new WaitForSeconds(delay);
            playerHasActiveEmoji[position] = false;
        }

        public void OpenPanel()
        {
            Debug.Log($"[EmojiSystem] OpenPanel called. selectionPanelObj={selectionPanelObj != null}, isPanelOpen={isPanelOpen}");

            // Guard against destroyed object
            if (this == null || selectionPanelObj == null)
            {
                Debug.LogError("[EmojiSystem] OpenPanel failed - selectionPanelObj is null!");
                return;
            }
            if (isPanelOpen) return;
            isPanelOpen = true;

            Debug.Log("[EmojiSystem] Opening panel NOW");

            selectionPanelObj.SetActive(true);

            // Immediately set alpha to 1 for testing - if this works, animation is the issue
            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = 1;
                panelCanvasGroup.blocksRaycasts = true;
                panelCanvasGroup.interactable = true;
                Debug.Log($"[EmojiSystem] Panel alpha set to 1, blocksRaycasts=true");
            }
            else
            {
                Debug.LogError("[EmojiSystem] panelCanvasGroup is null!");
            }

            // Don't use StopAllCoroutines - it kills emoji flag reset coroutines!
            if (autoCloseCoroutine != null)
            {
                StopCoroutine(autoCloseCoroutine);
                autoCloseCoroutine = null;
            }

            // Auto-close timer
            autoCloseCoroutine = StartCoroutine(AutoCloseAfterDelay());
        }

        public void ClosePanel()
        {
            // Guard against destroyed object
            if (this == null) return;
            if (!isPanelOpen) return;
            isPanelOpen = false;

            Debug.Log("[EmojiSystem] Closing panel");

            if (autoCloseCoroutine != null)
            {
                StopCoroutine(autoCloseCoroutine);
                autoCloseCoroutine = null;
            }

            // Immediately close - no animation for now
            if (panelCanvasGroup != null)
            {
                panelCanvasGroup.alpha = 0;
                panelCanvasGroup.blocksRaycasts = false;
                panelCanvasGroup.interactable = false;
            }
            if (selectionPanelObj != null)
            {
                selectionPanelObj.SetActive(false);
            }
        }

        private IEnumerator AnimatePanel(bool opening)
        {
            float duration = 0.2f;
            float elapsed = 0;

            panelCanvasGroup.blocksRaycasts = true;
            panelCanvasGroup.interactable = true;

            RectTransform panelRect = selectionPanelObj.GetComponent<RectTransform>();
            Vector3 startScale = Vector3.one * 0.8f;
            Vector3 endScale = Vector3.one;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float eased = 1 - Mathf.Pow(1 - t, 3); // ease out

                panelCanvasGroup.alpha = eased;
                panelRect.localScale = Vector3.Lerp(startScale, endScale, eased);

                yield return null;
            }

            panelCanvasGroup.alpha = 1;
            panelRect.localScale = Vector3.one;
        }

        private IEnumerator AnimatePanelClose()
        {
            float duration = 0.15f;
            float elapsed = 0;

            panelCanvasGroup.blocksRaycasts = false;
            panelCanvasGroup.interactable = false;

            RectTransform panelRect = selectionPanelObj.GetComponent<RectTransform>();
            Vector3 startScale = Vector3.one;
            Vector3 endScale = Vector3.one * 0.8f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                panelCanvasGroup.alpha = 1 - t;
                panelRect.localScale = Vector3.Lerp(startScale, endScale, t);

                yield return null;
            }

            panelCanvasGroup.alpha = 0;
            selectionPanelObj.SetActive(false);
        }

        private IEnumerator AutoCloseAfterDelay()
        {
            yield return new WaitForSeconds(AUTO_CLOSE_DELAY);
            ClosePanel();
        }
    }
}
