using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Lekha.Network;

namespace Lekha.UI
{
    /// <summary>
    /// Room chat UI - allows players to send text messages in the room.
    /// Persists across lobby/game transitions via DontDestroyOnLoad.
    /// </summary>
    public class RoomChatUI : MonoBehaviour
    {
        public static RoomChatUI Instance { get; private set; }

        // Chat state
        private List<ChatEntry> messages = new List<ChatEntry>();
        private const int MAX_MESSAGES = 100;
        private int unreadCount = 0;
        private bool isPanelOpen = false;

        // UI references
        private Canvas canvas;
        private GameObject chatButton;
        private GameObject unreadBadge;
        private TextMeshProUGUI unreadBadgeText;
        private GameObject chatPanel;
        private RectTransform chatPanelRect;
        private ScrollRect scrollRect;
        private RectTransform contentRect;
        private TMP_InputField inputField;
        private Button sendButton;

        // Backdrop for tap-outside-to-close
        private GameObject backdrop;

        // Animation
        private bool isAnimating = false;
        private float animationProgress = 0f;
        private bool animatingOpen = false;
        private const float ANIMATION_SPEED = 6f;
        private const float PANEL_WIDTH = 700f;
        private const float PANEL_HEIGHT = 500f;

        // Colors
        private static readonly Color PanelBg = new Color(0.04f, 0.06f, 0.12f, 0.94f);
        private static readonly Color HeaderBg = new Color(0.06f, 0.09f, 0.16f, 1f);
        private static readonly Color InputBg = new Color(0.06f, 0.09f, 0.16f, 0.95f);
        private static readonly Color SendBtnColor = new Color(0.35f, 0.55f, 0.95f, 1f);
        private static readonly Color TeamBlue = new Color(0.30f, 0.70f, 1f, 1f);
        private static readonly Color TeamOrange = new Color(1f, 0.50f, 0.35f, 1f);
        private static readonly Color TextWhite = new Color(1f, 1f, 1f, 1f);
        private static readonly Color TextMuted = new Color(0.50f, 0.58f, 0.68f, 1f);
        private static readonly Color MyMessageBg = new Color(0.15f, 0.25f, 0.45f, 0.6f);
        private static readonly Color OtherMessageBg = new Color(0.08f, 0.10f, 0.18f, 0.6f);
        private static readonly Color BadgeColor = new Color(0.95f, 0.35f, 0.40f, 1f);

        private struct ChatEntry
        {
            public string PlayerName;
            public string Text;
            public string Position;
            public long Timestamp;
            public bool IsLocal;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            CreateUI();
            SubscribeToNetwork();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            UnsubscribeFromNetwork();
        }

        private void Update()
        {
            // Animate panel slide
            if (isAnimating)
            {
                animationProgress += Time.unscaledDeltaTime * ANIMATION_SPEED;
                if (animationProgress >= 1f)
                {
                    animationProgress = 1f;
                    isAnimating = false;
                }

                float t = EaseOutCubic(animationProgress);
                CanvasGroup panelCG = chatPanel.GetComponent<CanvasGroup>();
                if (animatingOpen)
                {
                    if (panelCG != null) panelCG.alpha = t;
                    chatPanel.transform.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, t);
                }
                else
                {
                    if (panelCG != null) panelCG.alpha = 1f - t;
                    chatPanel.transform.localScale = Vector3.one * Mathf.Lerp(1f, 0.92f, t);
                    if (!isAnimating)
                    {
                        chatPanel.SetActive(false);
                        if (backdrop != null) backdrop.SetActive(false);
                    }
                }
            }

            // Submit on Enter key
            if (isPanelOpen && inputField != null && inputField.isFocused)
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    OnSendClicked();
                }
            }
        }

        private float EaseOutCubic(float t)
        {
            return 1f - Mathf.Pow(1f - t, 3f);
        }

        private void SubscribeToNetwork()
        {
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnChatMessageReceived += OnChatMessageReceived;
            }
        }

        private void UnsubscribeFromNetwork()
        {
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnChatMessageReceived -= OnChatMessageReceived;
            }
        }

        private void OnChatMessageReceived(string playerName, string text, string position)
        {
            bool isLocal = false;
            if (NetworkManager.Instance != null && NetworkManager.Instance.LocalPlayer != null)
            {
                isLocal = playerName == NetworkManager.Instance.LocalPlayer.DisplayName;
            }

            var entry = new ChatEntry
            {
                PlayerName = playerName,
                Text = text,
                Position = position,
                Timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                IsLocal = isLocal
            };

            messages.Add(entry);
            if (messages.Count > MAX_MESSAGES)
            {
                messages.RemoveAt(0);
                // Remove oldest UI element
                if (contentRect != null && contentRect.childCount > 0)
                {
                    Destroy(contentRect.GetChild(0).gameObject);
                }
            }

            // Add to UI
            AddMessageToUI(entry);

            // Handle unread badge
            if (!isPanelOpen)
            {
                unreadCount++;
                UpdateUnreadBadge();
            }
            else
            {
                // Auto-scroll to bottom
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 0f;
            }
        }

        private void OnSendClicked()
        {
            if (inputField == null) return;
            string text = inputField.text.Trim();
            if (string.IsNullOrEmpty(text)) return;

            NetworkManager.Instance?.SendChatMessage(text);
            inputField.text = "";
            inputField.ActivateInputField();
        }

        private void TogglePanel()
        {
            if (isAnimating) return;

            if (isPanelOpen)
            {
                ClosePanel();
            }
            else
            {
                OpenPanel();
            }
        }

        private void OpenPanel()
        {
            isPanelOpen = true;
            if (backdrop != null) backdrop.SetActive(true);
            chatPanel.SetActive(true);
            CanvasGroup cg = chatPanel.GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = 0f;
            chatPanel.transform.localScale = Vector3.one * 0.92f;
            isAnimating = true;
            animationProgress = 0f;
            animatingOpen = true;

            // Clear unread
            unreadCount = 0;
            UpdateUnreadBadge();

            // Scroll to bottom
            Canvas.ForceUpdateCanvases();
            if (scrollRect != null)
                scrollRect.verticalNormalizedPosition = 0f;
        }

        private void ClosePanel()
        {
            isPanelOpen = false;
            isAnimating = true;
            animationProgress = 0f;
            animatingOpen = false;
        }

        public void Show()
        {
            if (chatButton != null) chatButton.SetActive(true);
            // Re-subscribe in case NetworkManager was recreated
            UnsubscribeFromNetwork();
            SubscribeToNetwork();
        }

        public void ClearAndHide()
        {
            messages.Clear();
            unreadCount = 0;
            UpdateUnreadBadge();

            // Clear UI messages
            if (contentRect != null)
            {
                for (int i = contentRect.childCount - 1; i >= 0; i--)
                {
                    Destroy(contentRect.GetChild(i).gameObject);
                }
            }

            if (isPanelOpen)
            {
                isPanelOpen = false;
                chatPanel.SetActive(false);
                if (backdrop != null) backdrop.SetActive(false);
            }

            if (chatButton != null) chatButton.SetActive(false);
        }

        public void Hide()
        {
            if (isPanelOpen)
            {
                isPanelOpen = false;
                chatPanel.SetActive(false);
                if (backdrop != null) backdrop.SetActive(false);
            }
            if (chatButton != null) chatButton.SetActive(false);
        }

        // ===== UI CREATION =====

        private void CreateUI()
        {
            // Create canvas on this object
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90; // Above game, below popup dialogs

            CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            gameObject.AddComponent<GraphicRaycaster>();

            CreateChatButton();
            CreateChatPanel();

            // Start hidden
            chatButton.SetActive(false);
            chatPanel.SetActive(false);
        }

        private Sprite GetRoundedSprite()
        {
            if (ModernUITheme.Instance != null && ModernUITheme.Instance.GlassPanelSprite != null)
                return ModernUITheme.Instance.GlassPanelSprite;
            return null;
        }

        private Sprite GetCircleSprite()
        {
            if (ModernUITheme.Instance != null && ModernUITheme.Instance.CircleSprite != null)
                return ModernUITheme.Instance.CircleSprite;
            return null;
        }

        private Sprite GetPillSprite()
        {
            if (ModernUITheme.Instance != null && ModernUITheme.Instance.PillSprite != null)
                return ModernUITheme.Instance.PillSprite;
            return null;
        }

        private void CreateChatButton()
        {
            chatButton = new GameObject("ChatButton");
            chatButton.transform.SetParent(transform, false);

            RectTransform btnRect = chatButton.AddComponent<RectTransform>();
            // Bottom center
            btnRect.anchorMin = new Vector2(0.5f, 0);
            btnRect.anchorMax = new Vector2(0.5f, 0);
            btnRect.pivot = new Vector2(0.5f, 0);
            btnRect.anchoredPosition = new Vector2(0, 16);
            btnRect.sizeDelta = new Vector2(110, 40);

            Image btnBg = chatButton.AddComponent<Image>();
            btnBg.sprite = GetPillSprite();
            btnBg.color = new Color(0.12f, 0.16f, 0.30f, 0.92f);

            Button btn = chatButton.AddComponent<Button>();
            btn.targetGraphic = btnBg;
            btn.onClick.AddListener(TogglePanel);

            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
            cb.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            cb.selectedColor = Color.white;
            btn.colors = cb;

            Shadow shadow = chatButton.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.4f);
            shadow.effectDistance = new Vector2(0, -2);

            Outline outline = chatButton.AddComponent<Outline>();
            outline.effectColor = new Color(0.35f, 0.55f, 0.95f, 0.3f);
            outline.effectDistance = new Vector2(1, -1);

            // "CHAT" label with envelope icon
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(chatButton.transform, false);
            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI labelTmp = labelObj.AddComponent<TextMeshProUGUI>();
            labelTmp.text = "\u2709  CHAT";
            labelTmp.fontSize = 16;
            labelTmp.fontStyle = FontStyles.Bold;
            labelTmp.alignment = TextAlignmentOptions.Center;
            labelTmp.color = new Color(0.55f, 0.80f, 1f, 1f);
            labelTmp.raycastTarget = false;

            // Unread badge
            unreadBadge = new GameObject("UnreadBadge");
            unreadBadge.transform.SetParent(chatButton.transform, false);

            RectTransform badgeRect = unreadBadge.AddComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(1, 1);
            badgeRect.anchorMax = new Vector2(1, 1);
            badgeRect.pivot = new Vector2(0.5f, 0.5f);
            badgeRect.anchoredPosition = new Vector2(-4, -4);
            badgeRect.sizeDelta = new Vector2(24, 24);

            Image badgeBg = unreadBadge.AddComponent<Image>();
            badgeBg.sprite = GetCircleSprite();
            badgeBg.color = BadgeColor;

            GameObject badgeTextObj = new GameObject("BadgeText");
            badgeTextObj.transform.SetParent(unreadBadge.transform, false);
            RectTransform badgeTextRect = badgeTextObj.AddComponent<RectTransform>();
            badgeTextRect.anchorMin = Vector2.zero;
            badgeTextRect.anchorMax = Vector2.one;
            badgeTextRect.sizeDelta = Vector2.zero;

            unreadBadgeText = badgeTextObj.AddComponent<TextMeshProUGUI>();
            unreadBadgeText.text = "0";
            unreadBadgeText.fontSize = 13;
            unreadBadgeText.fontStyle = FontStyles.Bold;
            unreadBadgeText.alignment = TextAlignmentOptions.Center;
            unreadBadgeText.color = Color.white;
            unreadBadgeText.raycastTarget = false;

            unreadBadge.SetActive(false);
        }

        private void CreateChatPanel()
        {
            // Backdrop: semi-transparent overlay, tap to close
            backdrop = new GameObject("ChatBackdrop");
            backdrop.transform.SetParent(transform, false);
            RectTransform bdRect = backdrop.AddComponent<RectTransform>();
            bdRect.anchorMin = Vector2.zero;
            bdRect.anchorMax = Vector2.one;
            bdRect.sizeDelta = Vector2.zero;

            Image bdImg = backdrop.AddComponent<Image>();
            bdImg.color = new Color(0f, 0f, 0f, 0.45f);

            Button bdBtn = backdrop.AddComponent<Button>();
            bdBtn.targetGraphic = bdImg;
            bdBtn.onClick.AddListener(ClosePanel);
            ColorBlock bdCb = bdBtn.colors;
            bdCb.normalColor = Color.white;
            bdCb.highlightedColor = Color.white;
            bdCb.pressedColor = Color.white;
            bdCb.selectedColor = Color.white;
            bdBtn.colors = bdCb;

            backdrop.SetActive(false);

            // Chat panel: centered on screen
            chatPanel = new GameObject("ChatPanel");
            chatPanel.transform.SetParent(transform, false);

            chatPanelRect = chatPanel.AddComponent<RectTransform>();
            chatPanelRect.anchorMin = new Vector2(0.5f, 0.5f);
            chatPanelRect.anchorMax = new Vector2(0.5f, 0.5f);
            chatPanelRect.pivot = new Vector2(0.5f, 0.5f);
            chatPanelRect.anchoredPosition = Vector2.zero;
            chatPanelRect.sizeDelta = new Vector2(PANEL_WIDTH, PANEL_HEIGHT);

            // CanvasGroup for fade animation
            CanvasGroup panelCG = chatPanel.AddComponent<CanvasGroup>();
            panelCG.alpha = 0f;

            Image panelBg = chatPanel.AddComponent<Image>();
            panelBg.sprite = GetRoundedSprite();
            panelBg.color = PanelBg;

            // Outline border
            Outline panelOutline = chatPanel.AddComponent<Outline>();
            panelOutline.effectColor = new Color(1f, 1f, 1f, 0.10f);
            panelOutline.effectDistance = new Vector2(1, -1);

            Shadow panelShadow = chatPanel.AddComponent<Shadow>();
            panelShadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
            panelShadow.effectDistance = new Vector2(0, -4);

            // Layout: Header, Messages, Input
            VerticalLayoutGroup vlg = chatPanel.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(0, 0, 0, 0);
            vlg.spacing = 0;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            CreateHeader();
            CreateMessageArea();
            CreateInputArea();
        }

        private void CreateHeader()
        {
            GameObject header = new GameObject("Header");
            header.transform.SetParent(chatPanel.transform, false);

            RectTransform headerRect = header.AddComponent<RectTransform>();
            headerRect.sizeDelta = new Vector2(0, 44);

            Image headerBg = header.AddComponent<Image>();
            headerBg.color = HeaderBg;

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(header.transform, false);

            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.offsetMin = new Vector2(14, 0);
            titleRect.offsetMax = new Vector2(-44, 0);

            TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "CHAT";
            titleTmp.fontSize = 16;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.MidlineLeft;
            titleTmp.color = new Color(0.40f, 0.75f, 1f, 1f);
            titleTmp.characterSpacing = 3f;
            titleTmp.raycastTarget = false;

            // Close button
            GameObject closeObj = new GameObject("CloseButton");
            closeObj.transform.SetParent(header.transform, false);

            RectTransform closeRect = closeObj.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(1, 0.5f);
            closeRect.anchorMax = new Vector2(1, 0.5f);
            closeRect.pivot = new Vector2(1, 0.5f);
            closeRect.anchoredPosition = new Vector2(-8, 0);
            closeRect.sizeDelta = new Vector2(40, 40);

            Image closeBg = closeObj.AddComponent<Image>();
            closeBg.sprite = GetCircleSprite();
            closeBg.color = new Color(0.95f, 0.30f, 0.35f, 0.85f);

            Button closeBtn = closeObj.AddComponent<Button>();
            closeBtn.targetGraphic = closeBg;
            closeBtn.onClick.AddListener(ClosePanel);

            ColorBlock closeCb = closeBtn.colors;
            closeCb.normalColor = Color.white;
            closeCb.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
            closeCb.pressedColor = new Color(0.80f, 0.80f, 0.80f, 1f);
            closeBtn.colors = closeCb;

            GameObject closeIcon = new GameObject("X");
            closeIcon.transform.SetParent(closeObj.transform, false);
            RectTransform closeIconRect = closeIcon.AddComponent<RectTransform>();
            closeIconRect.anchorMin = Vector2.zero;
            closeIconRect.anchorMax = Vector2.one;
            closeIconRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI closeTmp = closeIcon.AddComponent<TextMeshProUGUI>();
            closeTmp.text = "\u2715";
            closeTmp.fontSize = 20;
            closeTmp.fontStyle = FontStyles.Bold;
            closeTmp.alignment = TextAlignmentOptions.Center;
            closeTmp.color = Color.white;
            closeTmp.raycastTarget = false;
        }

        private void CreateMessageArea()
        {
            GameObject scrollObj = new GameObject("ScrollArea");
            scrollObj.transform.SetParent(chatPanel.transform, false);

            RectTransform scrollObjRect = scrollObj.AddComponent<RectTransform>();
            scrollObjRect.sizeDelta = new Vector2(0, PANEL_HEIGHT - 44 - 52); // header(44) + input(52)

            Image scrollBg = scrollObj.AddComponent<Image>();
            scrollBg.color = new Color(0, 0, 0, 0.15f);

            scrollRect = scrollObj.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.scrollSensitivity = 20f;

            // Mask
            Mask mask = scrollObj.AddComponent<Mask>();
            mask.showMaskGraphic = true;

            // Viewport
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollObj.transform, false);

            RectTransform vpRect = viewport.AddComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.offsetMin = Vector2.zero;
            vpRect.offsetMax = Vector2.zero;

            // Content
            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);

            contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0, 0);

            VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(10, 10, 8, 8);
            contentLayout.spacing = 6;
            contentLayout.childAlignment = TextAnchor.LowerCenter;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = contentRect;
            scrollRect.viewport = vpRect;
        }

        private void CreateInputArea()
        {
            GameObject inputArea = new GameObject("InputArea");
            inputArea.transform.SetParent(chatPanel.transform, false);

            RectTransform inputAreaRect = inputArea.AddComponent<RectTransform>();
            inputAreaRect.sizeDelta = new Vector2(0, 52);

            Image inputAreaBg = inputArea.AddComponent<Image>();
            inputAreaBg.color = HeaderBg;

            // Horizontal layout for input + send button
            HorizontalLayoutGroup hlg = inputArea.AddComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(8, 8, 8, 8);
            hlg.spacing = 6;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;

            // Input field
            GameObject inputObj = new GameObject("InputField");
            inputObj.transform.SetParent(inputArea.transform, false);

            Image inputBg = inputObj.AddComponent<Image>();
            inputBg.sprite = GetPillSprite();
            inputBg.color = InputBg;

            LayoutElement inputLE = inputObj.AddComponent<LayoutElement>();
            inputLE.flexibleWidth = 1;
            inputLE.preferredHeight = 36;

            inputField = inputObj.AddComponent<TMP_InputField>();
            inputField.characterLimit = 200;

            // Text area
            RectTransform inputRectT = inputObj.GetComponent<RectTransform>();

            GameObject textArea = new GameObject("TextArea");
            textArea.transform.SetParent(inputObj.transform, false);
            RectTransform textAreaRect = textArea.AddComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = new Vector2(12, 2);
            textAreaRect.offsetMax = new Vector2(-12, -2);

            RectMask2D textMask = textArea.AddComponent<RectMask2D>();

            // Input text
            GameObject inputTextObj = new GameObject("Text");
            inputTextObj.transform.SetParent(textArea.transform, false);
            RectTransform inputTextRect = inputTextObj.AddComponent<RectTransform>();
            inputTextRect.anchorMin = Vector2.zero;
            inputTextRect.anchorMax = Vector2.one;
            inputTextRect.offsetMin = Vector2.zero;
            inputTextRect.offsetMax = Vector2.zero;

            TextMeshProUGUI inputTmp = inputTextObj.AddComponent<TextMeshProUGUI>();
            inputTmp.fontSize = 16;
            inputTmp.color = TextWhite;
            inputTmp.alignment = TextAlignmentOptions.MidlineLeft;
            inputTmp.isRightToLeftText = false; // Will auto-handle mixed text

            inputField.textComponent = inputTmp;
            inputField.textViewport = textAreaRect;

            // Placeholder
            GameObject placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(textArea.transform, false);
            RectTransform phRect = placeholderObj.AddComponent<RectTransform>();
            phRect.anchorMin = Vector2.zero;
            phRect.anchorMax = Vector2.one;
            phRect.offsetMin = Vector2.zero;
            phRect.offsetMax = Vector2.zero;

            TextMeshProUGUI placeholderTmp = placeholderObj.AddComponent<TextMeshProUGUI>();
            placeholderTmp.text = "Type a message...";
            placeholderTmp.fontSize = 14;
            placeholderTmp.fontStyle = FontStyles.Italic;
            placeholderTmp.color = TextMuted;
            placeholderTmp.alignment = TextAlignmentOptions.MidlineLeft;

            inputField.placeholder = placeholderTmp;

            // Send button
            GameObject sendObj = new GameObject("SendButton");
            sendObj.transform.SetParent(inputArea.transform, false);

            Image sendBg = sendObj.AddComponent<Image>();
            sendBg.sprite = GetPillSprite();
            sendBg.color = SendBtnColor;

            LayoutElement sendLE = sendObj.AddComponent<LayoutElement>();
            sendLE.preferredWidth = 60;
            sendLE.preferredHeight = 36;

            sendButton = sendObj.AddComponent<Button>();
            sendButton.targetGraphic = sendBg;
            sendButton.onClick.AddListener(OnSendClicked);

            ColorBlock sendCb = sendButton.colors;
            sendCb.normalColor = Color.white;
            sendCb.highlightedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
            sendCb.pressedColor = new Color(0.80f, 0.80f, 0.80f, 1f);
            sendCb.selectedColor = Color.white;
            sendButton.colors = sendCb;

            GameObject sendTextObj = new GameObject("Text");
            sendTextObj.transform.SetParent(sendObj.transform, false);
            RectTransform sendTextRect = sendTextObj.AddComponent<RectTransform>();
            sendTextRect.anchorMin = Vector2.zero;
            sendTextRect.anchorMax = Vector2.one;
            sendTextRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI sendTmp = sendTextObj.AddComponent<TextMeshProUGUI>();
            sendTmp.text = "Send";
            sendTmp.fontSize = 14;
            sendTmp.fontStyle = FontStyles.Bold;
            sendTmp.alignment = TextAlignmentOptions.Center;
            sendTmp.color = Color.white;
            sendTmp.raycastTarget = false;
        }

        // ===== MESSAGE UI =====

        private void AddMessageToUI(ChatEntry entry)
        {
            if (contentRect == null) return;

            GameObject msgObj = new GameObject("Message");
            msgObj.transform.SetParent(contentRect, false);

            // Outer container with horizontal layout for alignment
            HorizontalLayoutGroup msgHlg = msgObj.AddComponent<HorizontalLayoutGroup>();
            msgHlg.padding = new RectOffset(0, 0, 0, 0);
            msgHlg.spacing = 0;
            msgHlg.childControlWidth = false;
            msgHlg.childControlHeight = true;
            msgHlg.childForceExpandWidth = false;
            msgHlg.childForceExpandHeight = false;
            msgHlg.childAlignment = entry.IsLocal ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;

            ContentSizeFitter msgCsf = msgObj.AddComponent<ContentSizeFitter>();
            msgCsf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            msgCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Add spacer on appropriate side for alignment
            if (entry.IsLocal)
            {
                GameObject spacer = new GameObject("Spacer");
                spacer.transform.SetParent(msgObj.transform, false);
                LayoutElement spacerLE = spacer.AddComponent<LayoutElement>();
                spacerLE.flexibleWidth = 1;
                spacerLE.minWidth = 40;
            }

            // Message bubble
            GameObject bubble = new GameObject("Bubble");
            bubble.transform.SetParent(msgObj.transform, false);

            Image bubbleBg = bubble.AddComponent<Image>();
            bubbleBg.sprite = GetPillSprite();
            bubbleBg.color = entry.IsLocal ? MyMessageBg : OtherMessageBg;

            LayoutElement bubbleLE = bubble.AddComponent<LayoutElement>();
            bubbleLE.preferredWidth = PANEL_WIDTH * 0.72f;
            bubbleLE.flexibleWidth = 0;

            VerticalLayoutGroup bubbleVlg = bubble.AddComponent<VerticalLayoutGroup>();
            bubbleVlg.padding = new RectOffset(10, 10, 6, 6);
            bubbleVlg.spacing = 2;
            bubbleVlg.childControlWidth = true;
            bubbleVlg.childControlHeight = true;
            bubbleVlg.childForceExpandWidth = true;
            bubbleVlg.childForceExpandHeight = false;

            ContentSizeFitter bubbleCsf = bubble.AddComponent<ContentSizeFitter>();
            bubbleCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Player name (skip for local messages)
            if (!entry.IsLocal)
            {
                GameObject nameObj = new GameObject("Name");
                nameObj.transform.SetParent(bubble.transform, false);

                TextMeshProUGUI nameTmp = nameObj.AddComponent<TextMeshProUGUI>();
                nameTmp.text = entry.PlayerName;
                nameTmp.fontSize = 12;
                nameTmp.fontStyle = FontStyles.Bold;
                nameTmp.color = GetPositionColor(entry.Position);
                nameTmp.alignment = TextAlignmentOptions.TopLeft;
                nameTmp.raycastTarget = false;
            }

            // Message text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(bubble.transform, false);

            TextMeshProUGUI textTmp = textObj.AddComponent<TextMeshProUGUI>();
            textTmp.text = entry.Text;
            textTmp.fontSize = 16;
            textTmp.color = TextWhite;
            textTmp.enableWordWrapping = true;
            textTmp.raycastTarget = false;

            // Arabic / RTL support: detect if text contains Arabic chars
            bool hasArabic = false;
            foreach (char c in entry.Text)
            {
                if (c >= 0x0600 && c <= 0x06FF) { hasArabic = true; break; }
            }
            if (hasArabic)
            {
                textTmp.isRightToLeftText = true;
                textTmp.alignment = TextAlignmentOptions.TopRight;
            }
            else
            {
                textTmp.alignment = entry.IsLocal ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft;
            }

            // Timestamp
            GameObject tsObj = new GameObject("Timestamp");
            tsObj.transform.SetParent(bubble.transform, false);

            TextMeshProUGUI tsTmp = tsObj.AddComponent<TextMeshProUGUI>();
            var dt = System.DateTimeOffset.FromUnixTimeMilliseconds(entry.Timestamp).LocalDateTime;
            tsTmp.text = dt.ToString("HH:mm");
            tsTmp.fontSize = 10;
            tsTmp.color = TextMuted;
            tsTmp.alignment = entry.IsLocal ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft;
            tsTmp.raycastTarget = false;

            // Add spacer on the other side for non-local
            if (!entry.IsLocal)
            {
                GameObject spacer = new GameObject("Spacer");
                spacer.transform.SetParent(msgObj.transform, false);
                LayoutElement spacerLE = spacer.AddComponent<LayoutElement>();
                spacerLE.flexibleWidth = 1;
                spacerLE.minWidth = 40;
            }

            // Auto-scroll if panel is open
            if (isPanelOpen)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 0f;
            }
        }

        private Color GetPositionColor(string position)
        {
            if (position == "North" || position == "South")
                return TeamBlue;
            if (position == "East" || position == "West")
                return TeamOrange;
            return TextWhite;
        }

        private void UpdateUnreadBadge()
        {
            if (unreadBadge == null) return;

            if (unreadCount > 0)
            {
                unreadBadge.SetActive(true);
                if (unreadBadgeText != null)
                {
                    unreadBadgeText.text = unreadCount > 99 ? "99+" : unreadCount.ToString();
                }
            }
            else
            {
                unreadBadge.SetActive(false);
            }
        }
    }
}
