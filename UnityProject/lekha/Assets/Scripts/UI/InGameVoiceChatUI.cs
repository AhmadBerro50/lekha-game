using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Lekha.Network;
using Lekha.Core;

namespace Lekha.UI
{
    /// <summary>
    /// In-game voice chat UI overlay
    /// Shows mic/speaker controls and speaking indicators during gameplay
    /// </summary>
    public class InGameVoiceChatUI : MonoBehaviour
    {
        public static InGameVoiceChatUI Instance { get; private set; }

        // UI Elements
        private GameObject rootPanel;
        private Button micButton;
        private Button speakerButton;
        private Button voiceToggleButton;
        private TextMeshProUGUI micButtonText;
        private TextMeshProUGUI speakerButtonText;
        private TextMeshProUGUI voiceToggleText;
        private Image micButtonBg;
        private Image speakerButtonBg;
        private Image voiceToggleBg;

        // Speaking indicators per player position
        private Dictionary<PlayerPosition, Image> speakingIndicators = new Dictionary<PlayerPosition, Image>();

        // Colors
        private static readonly Color GoldColor = new Color(0.85f, 0.65f, 0.13f);
        private static readonly Color MutedColor = new Color(0.5f, 0.3f, 0.3f);
        private static readonly Color ActiveColor = new Color(0.3f, 0.7f, 0.4f);
        private static readonly Color SpeakingColor = new Color(0.4f, 0.9f, 0.5f);
        private static readonly Color DisabledColor = new Color(0.4f, 0.4f, 0.4f);

        private bool isVisible = false;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Voice chat is disabled - don't create UI at all
            if (VoiceChatManager.VOICE_CHAT_DISABLED)
            {
                Debug.Log("[InGameVoiceChatUI] Voice chat is disabled, hiding UI");
                gameObject.SetActive(false);
                return;
            }

            CreateUI();

            // Subscribe to voice chat events
            if (VoiceChatManager.Instance != null)
            {
                VoiceChatManager.Instance.OnMicrophoneMuteChanged += OnMicMuteChanged;
                VoiceChatManager.Instance.OnSpeakerMuteChanged += OnSpeakerMuteChanged;
                VoiceChatManager.Instance.OnPlayerSpeaking += OnPlayerSpeaking;
            }

            // Initially hide if not in online game
            if (NetworkGameSync.Instance == null || !NetworkGameSync.Instance.IsOnlineGame)
            {
                Hide();
            }
        }

        private void OnDestroy()
        {
            if (VoiceChatManager.Instance != null)
            {
                VoiceChatManager.Instance.OnMicrophoneMuteChanged -= OnMicMuteChanged;
                VoiceChatManager.Instance.OnSpeakerMuteChanged -= OnSpeakerMuteChanged;
                VoiceChatManager.Instance.OnPlayerSpeaking -= OnPlayerSpeaking;
            }
        }

        private void Update()
        {
            // Update local speaking indicator
            if (isVisible && VoiceChatManager.Instance != null)
            {
                UpdateLocalSpeakingIndicator();
            }
        }

        private void CreateUI()
        {
            // Root panel
            rootPanel = new GameObject("InGameVoiceChatUI");
            rootPanel.transform.SetParent(transform);

            Canvas canvas = rootPanel.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90; // Below other UI but above game

            CanvasScaler scaler = rootPanel.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            rootPanel.AddComponent<GraphicRaycaster>();

            // Voice controls container (bottom right)
            CreateVoiceControls(rootPanel.transform);

            // Speaking indicators for each player position
            CreateSpeakingIndicators(rootPanel.transform);
        }

        private void CreateVoiceControls(Transform parent)
        {
            // Container
            GameObject container = new GameObject("VoiceControls");
            container.transform.SetParent(parent, false);

            RectTransform containerRect = container.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(1, 0);
            containerRect.anchorMax = new Vector2(1, 0);
            containerRect.pivot = new Vector2(1, 0);
            containerRect.anchoredPosition = new Vector2(-20, 20);
            containerRect.sizeDelta = new Vector2(190, 70); // Wider for 3 buttons

            // Background
            Image bg = container.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.5f);

            // Voice toggle button (on/off for entire voice chat)
            GameObject toggleObj = CreateControlButton(container.transform, "VoiceToggle", "📞", new Vector2(-125, 0));
            voiceToggleButton = toggleObj.GetComponent<Button>();
            voiceToggleBg = toggleObj.GetComponent<Image>();
            voiceToggleText = toggleObj.GetComponentInChildren<TextMeshProUGUI>();
            voiceToggleButton.onClick.AddListener(OnVoiceToggleClicked);

            // Mic button
            GameObject micObj = CreateControlButton(container.transform, "MicButton", "🎤", new Vector2(-75, 0));
            micButton = micObj.GetComponent<Button>();
            micButtonBg = micObj.GetComponent<Image>();
            micButtonText = micObj.GetComponentInChildren<TextMeshProUGUI>();
            micButton.onClick.AddListener(OnMicButtonClicked);

            // Speaker button
            GameObject speakerObj = CreateControlButton(container.transform, "SpeakerButton", "🔊", new Vector2(-25, 0));
            speakerButton = speakerObj.GetComponent<Button>();
            speakerButtonBg = speakerObj.GetComponent<Image>();
            speakerButtonText = speakerObj.GetComponentInChildren<TextMeshProUGUI>();
            speakerButton.onClick.AddListener(OnSpeakerButtonClicked);

            UpdateButtonStates();
        }

        private GameObject CreateControlButton(Transform parent, string name, string icon, Vector2 position)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);

            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 0.5f);
            rect.anchorMax = new Vector2(1, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(50, 50);

            Image bg = btnObj.AddComponent<Image>();
            bg.color = ActiveColor;

            // Create rounded corners
            Texture2D roundTex = CreateRoundedTexture(32, 8);
            bg.sprite = Sprite.Create(roundTex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = bg;

            // Icon text
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(btnObj.transform, false);

            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;

            TextMeshProUGUI iconText = iconObj.AddComponent<TextMeshProUGUI>();
            iconText.text = icon;
            iconText.fontSize = 24;
            iconText.alignment = TextAlignmentOptions.Center;
            iconText.color = Color.white;

            return btnObj;
        }

        private void CreateSpeakingIndicators(Transform parent)
        {
            // Create indicators near each player's position
            // These will glow when the player is speaking

            CreateSpeakingIndicator(parent, PlayerPosition.South, new Vector2(0, 150)); // Bottom center
            CreateSpeakingIndicator(parent, PlayerPosition.West, new Vector2(-450, 500)); // Left
            CreateSpeakingIndicator(parent, PlayerPosition.North, new Vector2(0, 850)); // Top
            CreateSpeakingIndicator(parent, PlayerPosition.East, new Vector2(450, 500)); // Right
        }

        private void CreateSpeakingIndicator(Transform parent, PlayerPosition position, Vector2 anchoredPos)
        {
            GameObject indicator = new GameObject($"SpeakingIndicator_{position}");
            indicator.transform.SetParent(parent, false);

            RectTransform rect = indicator.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0);
            rect.anchorMax = new Vector2(0.5f, 0);
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = new Vector2(30, 30);

            Image img = indicator.AddComponent<Image>();

            // Create glow texture
            Texture2D glowTex = CreateGlowTexture(32);
            img.sprite = Sprite.Create(glowTex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
            img.color = new Color(SpeakingColor.r, SpeakingColor.g, SpeakingColor.b, 0f); // Initially invisible

            speakingIndicators[position] = img;
        }

        private Texture2D CreateRoundedTexture(int size, int radius)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool inside = IsInsideRoundedRect(x, y, size, size, radius);
                    tex.SetPixel(x, y, inside ? Color.white : Color.clear);
                }
            }

            tex.Apply();
            return tex;
        }

        private bool IsInsideRoundedRect(int x, int y, int width, int height, int radius)
        {
            if (x < radius && y < radius)
                return Vector2.Distance(new Vector2(x, y), new Vector2(radius, radius)) <= radius;
            if (x >= width - radius && y < radius)
                return Vector2.Distance(new Vector2(x, y), new Vector2(width - radius - 1, radius)) <= radius;
            if (x < radius && y >= height - radius)
                return Vector2.Distance(new Vector2(x, y), new Vector2(radius, height - radius - 1)) <= radius;
            if (x >= width - radius && y >= height - radius)
                return Vector2.Distance(new Vector2(x, y), new Vector2(width - radius - 1, height - radius - 1)) <= radius;
            return true;
        }

        private Texture2D CreateGlowTexture(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size / 2f, size / 2f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center) / (size / 2f);
                    float alpha = Mathf.Max(0, 1 - dist * dist);
                    tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
            }

            tex.Apply();
            return tex;
        }

        private void OnVoiceToggleClicked()
        {
            if (VoiceChatManager.Instance != null)
            {
                VoiceChatManager.Instance.ToggleVoiceChat();
                UpdateButtonStates();
            }
        }

        private void OnMicButtonClicked()
        {
            if (VoiceChatManager.Instance != null)
            {
                bool currentlyMuted = VoiceChatManager.Instance.IsMicrophoneMuted;
                VoiceChatManager.Instance.SetMicrophoneMuted(!currentlyMuted);
            }
        }

        private void OnSpeakerButtonClicked()
        {
            if (VoiceChatManager.Instance != null)
            {
                bool currentlyMuted = VoiceChatManager.Instance.IsSpeakerMuted;
                VoiceChatManager.Instance.SetSpeakerMuted(!currentlyMuted);
            }
        }

        private void OnMicMuteChanged(bool muted)
        {
            UpdateButtonStates();
        }

        private void OnSpeakerMuteChanged(bool muted)
        {
            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            if (VoiceChatManager.Instance == null) return;

            bool voiceEnabled = VoiceChatManager.Instance.IsVoiceChatEnabled;

            // Update voice toggle button
            if (voiceToggleBg != null)
            {
                voiceToggleBg.color = voiceEnabled ? ActiveColor : DisabledColor;
            }
            if (voiceToggleText != null)
            {
                voiceToggleText.text = voiceEnabled ? "📞" : "❌";
            }

            // Update mic button (disabled if voice chat is off)
            if (micButtonBg != null)
            {
                if (!voiceEnabled)
                    micButtonBg.color = DisabledColor;
                else
                    micButtonBg.color = VoiceChatManager.Instance.IsMicrophoneMuted ? MutedColor : ActiveColor;
            }
            if (micButtonText != null)
            {
                micButtonText.text = VoiceChatManager.Instance.IsMicrophoneMuted ? "🔇" : "🎤";
            }
            if (micButton != null)
            {
                micButton.interactable = voiceEnabled;
            }

            // Update speaker button (disabled if voice chat is off)
            if (speakerButtonBg != null)
            {
                if (!voiceEnabled)
                    speakerButtonBg.color = DisabledColor;
                else
                    speakerButtonBg.color = VoiceChatManager.Instance.IsSpeakerMuted ? MutedColor : ActiveColor;
            }
            if (speakerButtonText != null)
            {
                speakerButtonText.text = VoiceChatManager.Instance.IsSpeakerMuted ? "🔇" : "🔊";
            }
            if (speakerButton != null)
            {
                speakerButton.interactable = voiceEnabled;
            }
        }

        private void OnPlayerSpeaking(string playerId, bool speaking)
        {
            // Find the player's position
            var room = NetworkManager.Instance?.CurrentRoom;
            if (room == null) return;

            var player = room.Players.Find(p => p.PlayerId == playerId);
            if (player == null || player.AssignedPosition == null) return;

            if (player.Position == null) return;
            PlayerPosition position = player.Position.Value;

            if (speakingIndicators.TryGetValue(position, out Image indicator))
            {
                // Animate speaking indicator
                float targetAlpha = speaking ? 0.8f : 0f;
                StartCoroutine(AnimateSpeakingIndicator(indicator, targetAlpha));
            }
        }

        private void UpdateLocalSpeakingIndicator()
        {
            if (NetworkGameSync.Instance == null) return;

            string localPosStr = NetworkGameSync.Instance.LocalPosition;
            if (string.IsNullOrEmpty(localPosStr)) return;

            if (System.Enum.TryParse(localPosStr, out PlayerPosition localPos))
            {
                if (speakingIndicators.TryGetValue(localPos, out Image indicator))
                {
                    float level = VoiceChatManager.Instance?.LocalSpeakingLevel ?? 0f;
                    bool speaking = level > 0.01f && !(VoiceChatManager.Instance?.IsMicrophoneMuted ?? true);

                    // Update alpha based on speaking level
                    Color c = indicator.color;
                    c.a = Mathf.Lerp(c.a, speaking ? Mathf.Clamp01(level * 10f) : 0f, Time.deltaTime * 10f);
                    indicator.color = c;
                }
            }
        }

        private System.Collections.IEnumerator AnimateSpeakingIndicator(Image indicator, float targetAlpha)
        {
            float duration = 0.2f;
            float elapsed = 0f;
            float startAlpha = indicator.color.a;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                Color c = indicator.color;
                c.a = Mathf.Lerp(startAlpha, targetAlpha, t);
                indicator.color = c;

                yield return null;
            }

            Color final = indicator.color;
            final.a = targetAlpha;
            indicator.color = final;
        }

        public void Show()
        {
            // Voice chat is disabled - don't show
            if (VoiceChatManager.VOICE_CHAT_DISABLED)
            {
                return;
            }

            rootPanel?.SetActive(true);
            isVisible = true;

            // Start voice chat if not already
            if (VoiceChatManager.Instance != null)
            {
                VoiceChatManager.Instance.Initialize();
                VoiceChatManager.Instance.StartRecording();
            }

            UpdateButtonStates();
        }

        public void Hide()
        {
            rootPanel?.SetActive(false);
            isVisible = false;
        }
    }
}
