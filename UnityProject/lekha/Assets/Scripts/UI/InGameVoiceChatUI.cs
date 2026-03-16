using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Lekha.Network;

namespace Lekha.UI
{
    /// <summary>
    /// In-game voice chat UI overlay.
    /// Shows circular mic/speaker controls during online gameplay.
    /// Modern pill design with Unicode icons.
    /// </summary>
    public class InGameVoiceChatUI : MonoBehaviour
    {
        public static InGameVoiceChatUI Instance { get; private set; }

        // UI Elements
        private GameObject rootPanel;
        private Button micButton;
        private Button speakerButton;
        private Image micIconImage;
        private TextMeshProUGUI micLabelText;
        private Image speakerIconImage;
        private TextMeshProUGUI speakerLabelText;
        private Image micButtonBg;
        private Image speakerButtonBg;
        private Image micRing;
        private Image speakerRing;

        // Speaking pulse animation
        private float micPulseTime = 0f;
        private bool isMicSpeaking = false;

        // Colors
        private static readonly Color ActiveGreen   = new Color(0.20f, 0.80f, 0.45f, 1f);
        private static readonly Color MutedRed       = new Color(0.90f, 0.25f, 0.25f, 1f);
        private static readonly Color SpeakerBlue    = new Color(0.35f, 0.65f, 1.00f, 1f);
        private static readonly Color SpeakerMuted   = new Color(0.90f, 0.25f, 0.25f, 1f);
        private static readonly Color BgDark         = new Color(0.08f, 0.09f, 0.14f, 0.92f);
        private static readonly Color BgMuted        = new Color(0.22f, 0.06f, 0.06f, 0.92f);

        // ASCII icons (Unicode not supported by default TMP font)
        private const string IconMicOn   = "MIC";
        private const string IconMicOff  = "OFF";
        private const string IconSpkOn   = "SPK";
        private const string IconSpkOff  = "OFF";



        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            CreateUI();
            Debug.Log("[InGameVoiceChatUI] UI built in Awake, ready for Show()");
        }

        private void Start()
        {
            if (VoiceChatManager.Instance != null)
            {
                VoiceChatManager.Instance.OnMicrophoneMuteChanged += OnMicMuteChanged;
                VoiceChatManager.Instance.OnSpeakerMuteChanged    += OnSpeakerMuteChanged;
                VoiceChatManager.Instance.OnPlayerSpeaking        += OnPlayerSpeaking;
            }
        }

        private void Update()
        {
            // Animate mic ring pulse when speaking
            if (isMicSpeaking && micRing != null)
            {
                micPulseTime += Time.deltaTime * 4f;
                float scale = 1f + Mathf.Sin(micPulseTime) * 0.12f;
                micRing.transform.localScale = Vector3.one * scale;
                micRing.color = new Color(ActiveGreen.r, ActiveGreen.g, ActiveGreen.b,
                    0.4f + Mathf.Sin(micPulseTime) * 0.3f);
            }
            else if (micRing != null)
            {
                micRing.transform.localScale = Vector3.one;
                micRing.color = new Color(0.3f, 0.8f, 0.5f, 0.15f);
            }
        }

        private void OnDestroy()
        {
            if (VoiceChatManager.Instance != null)
            {
                VoiceChatManager.Instance.OnMicrophoneMuteChanged -= OnMicMuteChanged;
                VoiceChatManager.Instance.OnSpeakerMuteChanged    -= OnSpeakerMuteChanged;
                VoiceChatManager.Instance.OnPlayerSpeaking        -= OnPlayerSpeaking;
            }
            if (Instance == this) Instance = null;
        }

        // ─── UI Construction ──────────────────────────────────────────────────

        private void CreateUI()
        {
            rootPanel = new GameObject("VoiceChatPanel");
            rootPanel.transform.SetParent(transform, false);

            Canvas canvas = rootPanel.AddComponent<Canvas>();
            canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90;

            CanvasScaler scaler = rootPanel.AddComponent<CanvasScaler>();
            scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight  = 0.5f;

            rootPanel.AddComponent<GraphicRaycaster>();

            Sprite circle = MakeCircleSprite(64);

            // Mic button – bottom left, above hand area
            micButton = CreateButton(rootPanel.transform, "MicButton",
                new Vector2(68f, 190f), 64f, circle, out micButtonBg, out micRing,
                out micIconImage, out micLabelText, "MIC");
            micButton.onClick.AddListener(OnMicClicked);

            // Speaker button – next to mic
            speakerButton = CreateButton(rootPanel.transform, "SpeakerButton",
                new Vector2(152f, 190f), 64f, circle, out speakerButtonBg, out Image _ring,
                out speakerIconImage, out speakerLabelText, "SPK");
            speakerButton.onClick.AddListener(OnSpeakerClicked);

            // Set initial icon sprites
            if (micIconImage != null) micIconImage.sprite = LoadIconSprite("mic");
            if (speakerIconImage != null) speakerIconImage.sprite = LoadIconSprite("speaker");

            UpdateButtonStates();
            rootPanel.SetActive(false);
        }

        private Button CreateButton(Transform parent, string name, Vector2 anchoredPos, float size,
            Sprite circleSprite,
            out Image bg, out Image ring,
            out Image iconImage, out TextMeshProUGUI labelText,
            string labelStr)
        {
            // Container
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchorMin        = new Vector2(0, 0);
            rect.anchorMax        = new Vector2(0, 0);
            rect.pivot            = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta        = new Vector2(size, size);

            // Outer glow ring (animates when speaking)
            GameObject ringObj = new GameObject("Ring");
            ringObj.transform.SetParent(obj.transform, false);
            RectTransform ringRect = ringObj.AddComponent<RectTransform>();
            ringRect.anchorMin = new Vector2(-0.15f, -0.15f);
            ringRect.anchorMax = new Vector2(1.15f, 1.15f);
            ringRect.sizeDelta = Vector2.zero;
            ring = ringObj.AddComponent<Image>();
            ring.sprite         = circleSprite;
            ring.color          = new Color(0.3f, 0.8f, 0.5f, 0.15f);
            ring.raycastTarget  = false;

            // Background circle
            bg = obj.AddComponent<Image>();
            bg.sprite = circleSprite;
            bg.color  = BgDark;

            // Subtle border
            Outline outline = obj.AddComponent<Outline>();
            outline.effectColor    = new Color(0.4f, 0.7f, 1f, 0.35f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            // Button
            Button btn = obj.AddComponent<Button>();
            btn.targetGraphic = bg;

            ColorBlock colors = btn.colors;
            colors.normalColor      = Color.white;
            colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
            colors.pressedColor     = new Color(0.75f, 0.75f, 0.75f, 1f);
            btn.colors              = colors;

            // Main icon (sprite image, centered)
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(obj.transform, false);
            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin        = new Vector2(0.5f, 0.55f);
            iconRect.anchorMax        = new Vector2(0.5f, 0.55f);
            iconRect.pivot            = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta        = new Vector2(size * 0.5f, size * 0.5f);

            iconImage              = iconObj.AddComponent<Image>();
            iconImage.preserveAspect = true;
            iconImage.color        = ActiveGreen;
            iconImage.raycastTarget = false;

            // Label under icon
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(obj.transform, false);
            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin        = new Vector2(0.5f, 0.1f);
            labelRect.anchorMax        = new Vector2(0.5f, 0.1f);
            labelRect.pivot            = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.sizeDelta        = new Vector2(size, size * 0.35f);

            labelText              = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text         = labelStr;
            labelText.fontSize     = 10f;
            labelText.fontStyle    = FontStyles.Bold;
            labelText.alignment    = TextAlignmentOptions.Center;
            labelText.color        = new Color(1f, 1f, 1f, 0.65f);
            labelText.raycastTarget = false;

            return btn;
        }

        // ─── Event Handlers ───────────────────────────────────────────────────

        private void OnMicClicked()
        {
            if (VoiceChatManager.Instance != null)
                VoiceChatManager.Instance.SetMicrophoneMuted(!VoiceChatManager.Instance.IsMicrophoneMuted);
        }

        private void OnSpeakerClicked()
        {
            if (VoiceChatManager.Instance != null)
                VoiceChatManager.Instance.SetSpeakerMuted(!VoiceChatManager.Instance.IsSpeakerMuted);
        }

        private void OnMicMuteChanged(bool muted)     => UpdateButtonStates();
        private void OnSpeakerMuteChanged(bool muted) => UpdateButtonStates();

        private void OnPlayerSpeaking(uint uid, bool speaking)
        {
            // uid 0 means local user (Agora convention)
            if (uid == 0 || uid == 1) // local user in Agora is uid 0 or position-based uid 1 (South)
            {
                isMicSpeaking = speaking && !(VoiceChatManager.Instance?.IsMicrophoneMuted ?? true);
            }
        }

        private void UpdateButtonStates()
        {
            bool micMuted = VoiceChatManager.Instance?.IsMicrophoneMuted ?? false;
            bool spkMuted = VoiceChatManager.Instance?.IsSpeakerMuted    ?? false;

            // --- Mic button ---
            if (micButtonBg != null)
                micButtonBg.color = micMuted ? BgMuted : BgDark;

            if (micIconImage != null)
            {
                micIconImage.sprite = LoadIconSprite(micMuted ? "mic_muted" : "mic");
                micIconImage.color  = micMuted ? MutedRed : ActiveGreen;
            }

            if (micLabelText != null)
            {
                micLabelText.text  = micMuted ? "MUTED" : "MIC";
                micLabelText.color = micMuted
                    ? new Color(MutedRed.r, MutedRed.g, MutedRed.b, 0.75f)
                    : new Color(1f, 1f, 1f, 0.65f);
            }

            // --- Speaker button ---
            if (speakerButtonBg != null)
                speakerButtonBg.color = spkMuted ? BgMuted : BgDark;

            if (speakerIconImage != null)
            {
                speakerIconImage.sprite = LoadIconSprite(spkMuted ? "speaker_muted" : "speaker");
                speakerIconImage.color  = spkMuted ? SpeakerMuted : SpeakerBlue;
            }

            if (speakerLabelText != null)
            {
                speakerLabelText.text  = spkMuted ? "MUTED" : "SPK";
                speakerLabelText.color = spkMuted
                    ? new Color(SpeakerMuted.r, SpeakerMuted.g, SpeakerMuted.b, 0.75f)
                    : new Color(1f, 1f, 1f, 0.65f);
            }
        }

        // ─── Public API ───────────────────────────────────────────────────────

        public void Show()
        {
            if (rootPanel != null) rootPanel.SetActive(true);
            // visible
            UpdateButtonStates();
            Debug.Log("[InGameVoiceChatUI] Show() called");
        }

        public void Hide()
        {
            if (rootPanel != null) rootPanel.SetActive(false);
            // hidden
        }

        // ─── Helpers ─────────────────────────────────────────────────────────

        private static Sprite MakeCircleSprite(int size)
        {
            Texture2D tex    = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float     center = size / 2f;
            float     radius = size / 2f - 1f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist  = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float alpha = dist <= radius ? Mathf.Clamp01((radius - dist) / 1.5f) : 0f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private static Dictionary<string, Sprite> _iconCache = new Dictionary<string, Sprite>();

        private static Sprite LoadIconSprite(string iconName)
        {
            if (_iconCache.TryGetValue(iconName, out Sprite cached))
                return cached;

            Texture2D tex = Resources.Load<Texture2D>($"Icons/{iconName}");
            if (tex != null)
            {
                Sprite s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                _iconCache[iconName] = s;
                return s;
            }
            return null;
        }
    }
}
