using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Lekha.Network;

namespace Lekha.UI
{
    /// <summary>
    /// In-game voice chat UI overlay.
    /// Shows circular mic/speaker controls during online gameplay.
    /// </summary>
    public class InGameVoiceChatUI : MonoBehaviour
    {
        public static InGameVoiceChatUI Instance { get; private set; }

        // UI Elements
        private GameObject rootPanel;
        private Button micButton;
        private Button speakerButton;
        private TextMeshProUGUI micIconText;
        private TextMeshProUGUI speakerIconText;
        private Image micButtonBg;
        private Image speakerButtonBg;
        private Outline micOutline;
        private Outline speakerOutline;

        // Colors
        private static readonly Color MicActiveColor = new Color(0.2f, 0.75f, 0.4f, 1f);
        private static readonly Color MicMutedColor = new Color(0.85f, 0.25f, 0.25f, 1f);
        private static readonly Color SpeakerActiveColor = new Color(0.3f, 0.55f, 0.9f, 1f);
        private static readonly Color SpeakerMutedColor = new Color(0.85f, 0.25f, 0.25f, 1f);
        private static readonly Color ButtonBgDark = new Color(0.1f, 0.12f, 0.18f, 0.9f);

        private bool isVisible = false;
        private Sprite circleSprite;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Create UI in Awake so Show() works immediately after AddComponent
            circleSprite = CreateCircleSprite(64);
            CreateUI();
            Debug.Log("[InGameVoiceChatUI] UI built in Awake, ready for Show()");
        }

        private void Start()
        {
            if (VoiceChatManager.Instance != null)
            {
                VoiceChatManager.Instance.OnMicrophoneMuteChanged += OnMicMuteChanged;
                VoiceChatManager.Instance.OnSpeakerMuteChanged += OnSpeakerMuteChanged;
            }
        }

        private void OnDestroy()
        {
            if (VoiceChatManager.Instance != null)
            {
                VoiceChatManager.Instance.OnMicrophoneMuteChanged -= OnMicMuteChanged;
                VoiceChatManager.Instance.OnSpeakerMuteChanged -= OnSpeakerMuteChanged;
            }

            if (Instance == this)
                Instance = null;
        }

        private void CreateUI()
        {
            rootPanel = new GameObject("VoiceChatPanel");
            rootPanel.transform.SetParent(transform, false);

            Canvas canvas = rootPanel.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90;

            CanvasScaler scaler = rootPanel.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            rootPanel.AddComponent<GraphicRaycaster>();

            // Mic button - bottom left, above the card hand
            GameObject micObj = CreateCircularButton(rootPanel.transform, "MicButton", new Vector2(80, 180), 70f);
            micButton = micObj.GetComponent<Button>();
            micButtonBg = micObj.GetComponent<Image>();
            micIconText = micObj.GetComponentInChildren<TextMeshProUGUI>();
            micOutline = micObj.GetComponent<Outline>();
            micButton.onClick.AddListener(OnMicButtonClicked);

            // Speaker button - next to mic
            GameObject speakerObj = CreateCircularButton(rootPanel.transform, "SpeakerButton", new Vector2(170, 180), 70f);
            speakerButton = speakerObj.GetComponent<Button>();
            speakerButtonBg = speakerObj.GetComponent<Image>();
            speakerIconText = speakerObj.GetComponentInChildren<TextMeshProUGUI>();
            speakerOutline = speakerObj.GetComponent<Outline>();
            speakerButton.onClick.AddListener(OnSpeakerButtonClicked);

            UpdateButtonStates();

            // Hidden by default
            rootPanel.SetActive(false);
        }

        private GameObject CreateCircularButton(Transform parent, string name, Vector2 position, float size)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(0, 0);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(size, size);

            Image bg = btnObj.AddComponent<Image>();
            bg.sprite = circleSprite;
            bg.type = Image.Type.Simple;
            bg.color = ButtonBgDark;

            Outline outline = btnObj.AddComponent<Outline>();
            outline.effectColor = MicActiveColor;
            outline.effectDistance = new Vector2(2, -2);

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = bg;

            ColorBlock colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1, 1, 1, 0.9f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            btn.colors = colors;

            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(btnObj.transform, false);

            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;

            TextMeshProUGUI text = iconObj.AddComponent<TextMeshProUGUI>();
            text.text = "MIC";
            text.fontSize = 20;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;

            return btnObj;
        }

        private Sprite CreateCircleSprite(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = size / 2f;
            float radius = size / 2f - 1f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    if (dist <= radius)
                    {
                        float alpha = Mathf.Clamp01((radius - dist) / 1.5f);
                        tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private void OnMicButtonClicked()
        {
            if (VoiceChatManager.Instance != null)
                VoiceChatManager.Instance.SetMicrophoneMuted(!VoiceChatManager.Instance.IsMicrophoneMuted);
        }

        private void OnSpeakerButtonClicked()
        {
            if (VoiceChatManager.Instance != null)
                VoiceChatManager.Instance.SetSpeakerMuted(!VoiceChatManager.Instance.IsSpeakerMuted);
        }

        private void OnMicMuteChanged(bool muted) => UpdateButtonStates();
        private void OnSpeakerMuteChanged(bool muted) => UpdateButtonStates();

        private void UpdateButtonStates()
        {
            bool micMuted = VoiceChatManager.Instance?.IsMicrophoneMuted ?? false;
            bool spkMuted = VoiceChatManager.Instance?.IsSpeakerMuted ?? false;

            Color micColor = micMuted ? MicMutedColor : MicActiveColor;
            if (micButtonBg != null)
                micButtonBg.color = micMuted ? new Color(0.25f, 0.08f, 0.08f, 0.9f) : ButtonBgDark;
            if (micOutline != null)
                micOutline.effectColor = new Color(micColor.r, micColor.g, micColor.b, 0.6f);
            if (micIconText != null)
            {
                micIconText.text = micMuted ? "X" : "MIC";
                micIconText.color = micColor;
            }

            Color spkColor = spkMuted ? SpeakerMutedColor : SpeakerActiveColor;
            if (speakerButtonBg != null)
                speakerButtonBg.color = spkMuted ? new Color(0.25f, 0.08f, 0.08f, 0.9f) : ButtonBgDark;
            if (speakerOutline != null)
                speakerOutline.effectColor = new Color(spkColor.r, spkColor.g, spkColor.b, 0.6f);
            if (speakerIconText != null)
            {
                speakerIconText.text = spkMuted ? "X" : "SPK";
                speakerIconText.color = spkColor;
            }
        }

        public void Show()
        {
            if (rootPanel != null)
                rootPanel.SetActive(true);
            isVisible = true;
            UpdateButtonStates();
            Debug.Log("[InGameVoiceChatUI] Show() called - buttons visible");
        }

        public void Hide()
        {
            if (rootPanel != null)
                rootPanel.SetActive(false);
            isVisible = false;
        }
    }
}
