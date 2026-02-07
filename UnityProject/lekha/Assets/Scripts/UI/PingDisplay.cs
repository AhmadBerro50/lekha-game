using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Lekha.Network;
using Lekha.GameLogic;

namespace Lekha.UI
{
    /// <summary>
    /// Self-managing ping display below top-right buttons.
    /// Uses CanvasGroup for visibility so Update() always runs
    /// and can auto-detect online state without external calls.
    /// </summary>
    public class PingDisplay : MonoBehaviour
    {
        public static PingDisplay Instance { get; private set; }

        private CanvasGroup canvasGroup;
        private TextMeshProUGUI pingValueText;
        private Image signalIcon;
        private Outline outline;
        private float updateTimer = 0f;
        private float glowPulseTime = 0f;
        private bool isShowing = false;

        // Colors for connection quality
        private static readonly Color GreenGlow = new Color(0.2f, 1f, 0.4f, 1f);
        private static readonly Color YellowGlow = new Color(1f, 0.9f, 0.2f, 1f);
        private static readonly Color RedGlow = new Color(1f, 0.3f, 0.2f, 1f);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public static PingDisplay Create(Transform canvasParent)
        {
            GameObject obj = new GameObject("PingDisplay");
            obj.transform.SetParent(canvasParent, false);

            PingDisplay display = obj.AddComponent<PingDisplay>();
            display.BuildUI();

            return display;
        }

        private void BuildUI()
        {
            RectTransform rootRect = GetComponent<RectTransform>();
            if (rootRect == null)
                rootRect = gameObject.AddComponent<RectTransform>();

            // Position below the pause/score buttons (they are at y=-50, size 50)
            rootRect.anchorMin = new Vector2(1, 1);
            rootRect.anchorMax = new Vector2(1, 1);
            rootRect.pivot = new Vector2(1, 1);
            rootRect.anchoredPosition = new Vector2(-60, -100);
            rootRect.sizeDelta = new Vector2(105, 32);

            // CanvasGroup for smooth show/hide without disabling GameObject
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            // Background
            Image bg = gameObject.AddComponent<Image>();
            if (ModernUITheme.Instance != null && ModernUITheme.Instance.GlassPanelDarkSprite != null)
            {
                bg.sprite = ModernUITheme.Instance.GlassPanelDarkSprite;
                bg.type = Image.Type.Sliced;
            }
            bg.color = new Color(0.08f, 0.1f, 0.15f, 0.85f);
            bg.raycastTarget = false;

            // Outline glow
            outline = gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(GreenGlow.r, GreenGlow.g, GreenGlow.b, 0.4f);
            outline.effectDistance = new Vector2(1, -1);

            // Signal bars icon
            GameObject iconObj = new GameObject("SignalIcon");
            iconObj.transform.SetParent(transform, false);

            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0, 0);
            iconRect.anchorMax = new Vector2(0, 1);
            iconRect.pivot = new Vector2(0, 0.5f);
            iconRect.anchoredPosition = new Vector2(8, 0);
            iconRect.sizeDelta = new Vector2(18, 0);

            signalIcon = iconObj.AddComponent<Image>();
            signalIcon.color = GreenGlow;
            signalIcon.raycastTarget = false;
            signalIcon.sprite = CreateSignalSprite();
            signalIcon.preserveAspect = true;

            // Ping value text (e.g. "42 ms")
            GameObject valueObj = new GameObject("PingValue");
            valueObj.transform.SetParent(transform, false);

            RectTransform valueRect = valueObj.AddComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(0, 0);
            valueRect.anchorMax = new Vector2(1, 1);
            valueRect.offsetMin = new Vector2(32, 2);
            valueRect.offsetMax = new Vector2(-6, -2);

            pingValueText = valueObj.AddComponent<TextMeshProUGUI>();
            pingValueText.text = "-- ms";
            pingValueText.fontSize = 14;
            pingValueText.fontStyle = FontStyles.Bold;
            pingValueText.color = GreenGlow;
            pingValueText.alignment = TextAlignmentOptions.Left;
            pingValueText.raycastTarget = false;
        }

        private Sprite CreateSignalSprite()
        {
            int size = 24;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, Color.clear);

            int barWidth = 4;
            int gap = 2;
            int[] heights = { 6, 10, 15, 20 };

            for (int bar = 0; bar < 4; bar++)
            {
                int startX = bar * (barWidth + gap);
                for (int y = 0; y < heights[bar]; y++)
                    for (int x = startX; x < startX + barWidth && x < size; x++)
                        if (y < size)
                            tex.SetPixel(x, y, Color.white);
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private void Update()
        {
            // Self-manage visibility: show when in online game, hide otherwise
            bool shouldShow = IsOnlineGame();

            if (shouldShow && !isShowing)
            {
                isShowing = true;
                canvasGroup.alpha = 1f;
            }
            else if (!shouldShow && isShowing)
            {
                isShowing = false;
                canvasGroup.alpha = 0f;
            }

            if (!isShowing) return;

            // Update ping every 1 second
            updateTimer += Time.deltaTime;
            if (updateTimer >= 1f)
            {
                updateTimer = 0f;
                UpdatePing();
            }

            // Pulse glow effect
            glowPulseTime += Time.deltaTime;
            float pulse = 0.3f + Mathf.Sin(glowPulseTime * 2f) * 0.15f;
            if (outline != null)
            {
                Color c = GetQualityColor();
                outline.effectColor = new Color(c.r, c.g, c.b, pulse);
            }
        }

        private bool IsOnlineGame()
        {
            // Check multiple sources - any one being true means we're online
            if (NetworkGameSync.Instance != null && NetworkGameSync.Instance.IsOnlineGame)
                return true;
            if (GameManager.Instance != null && GameManager.Instance.LocalPlayerPosition.HasValue)
                return true;
            return false;
        }

        private void UpdatePing()
        {
            if (NetworkManager.Instance == null) return;

            int ping = NetworkManager.Instance.PingMs;

            if (ping <= 0)
            {
                pingValueText.text = "-- ms";
                return;
            }

            pingValueText.text = $"{ping} ms";

            Color color = GetQualityColor();
            pingValueText.color = color;
            if (signalIcon != null) signalIcon.color = color;
        }

        private Color GetQualityColor()
        {
            if (NetworkManager.Instance == null) return GreenGlow;
            int ping = NetworkManager.Instance.PingMs;

            if (ping <= 0) return GreenGlow;
            if (ping < 80) return GreenGlow;
            if (ping < 150) return YellowGlow;
            return RedGlow;
        }

        // Keep public methods for manual control if needed, but self-management handles it
        public void Show()
        {
            isShowing = true;
            if (canvasGroup != null) canvasGroup.alpha = 1f;
            UpdatePing();
        }

        public void Hide()
        {
            isShowing = false;
            if (canvasGroup != null) canvasGroup.alpha = 0f;
        }

        public void AutoDetect()
        {
            // No-op: self-managed in Update()
        }
    }
}
