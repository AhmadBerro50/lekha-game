using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Lekha.Core;
using Lekha.Network;

namespace Lekha.UI
{
    /// <summary>
    /// Modern Lobby UI for online multiplayer
    /// Clean, professional design with card-based layout
    /// </summary>
    public class LobbyUI : MonoBehaviour
    {
        private enum LobbyState { MainLobby, InRoom, Connecting }
        private LobbyState currentState = LobbyState.Connecting;

        // Root panels
        private GameObject rootPanel;
        private GameObject mainLobbyPanel;
        private GameObject roomPanel;
        private GameObject connectingPanel;

        // Main lobby elements
        private Transform roomListContent;
        private List<GameObject> roomListItems = new List<GameObject>();
        private TMP_InputField newRoomNameInput;

        // Spectator elements
        private Transform liveGamesContent;
        private List<GameObject> liveGameItems = new List<GameObject>();

        // Refresh state
        private float refreshTimer = 0f;
        private const float AUTO_REFRESH_INTERVAL = 5f;
        private bool isRefreshing = false;
        private TextMeshProUGUI refreshButtonText;
        private GameObject loadingIndicator;

        // Online count display (lobby screen)
        private TextMeshProUGUI onlineCountText;

        // Room panel elements
        private TextMeshProUGUI roomNameText;
        private TextMeshProUGUI roomCodeText;
        private TextMeshProUGUI playerCountText;
        private Transform onlinePlayersBar;
        private Transform playerListContent;
        private List<GameObject> playerListItems = new List<GameObject>();
        private Button readyButton;
        private Button startButton;
        private TextMeshProUGUI readyButtonText;

        // Modern teal/ocean color palette
        private static readonly Color BgDark = new Color(0.04f, 0.08f, 0.14f);
        private static readonly Color CardBg = new Color(0.06f, 0.12f, 0.20f, 0.92f);
        private static readonly Color CardBgHover = new Color(0.08f, 0.16f, 0.26f, 0.95f);
        private static readonly Color AccentCyan = new Color(0.30f, 0.80f, 0.90f, 1f);
        private static readonly Color AccentMagenta = new Color(0.30f, 0.80f, 0.90f, 1f); // Use cyan instead of magenta
        private static readonly Color AccentGreen = new Color(0.30f, 0.90f, 0.60f, 1f);
        private static readonly Color AccentRed = new Color(0.95f, 0.35f, 0.45f, 1f);
        private static readonly Color TextPrimary = new Color(1f, 1f, 1f, 1f);
        private static readonly Color TextSecondary = new Color(0.70f, 0.80f, 0.85f, 1f);
        private static readonly Color TextMuted = new Color(0.45f, 0.55f, 0.62f, 1f);
        private static readonly Color InputBg = new Color(0.04f, 0.08f, 0.14f, 0.95f);
        private static readonly Color BorderColor = new Color(1f, 1f, 1f, 0.12f);
        private static readonly Color GlassBorder = new Color(1f, 1f, 1f, 0.12f);

        // Generated textures
        private Sprite roundedSprite;
        private Sprite circleSprite;
        private Sprite gradientSprite;

        public event System.Action OnBackClicked;
        public event System.Action OnGameStarting;

        private void Start()
        {
            GenerateSprites();
            CreateUI();
            SubscribeToNetworkEvents();

            if (NetworkManager.Instance != null)
            {
                // Set initial online count from any previously received value
                if (NetworkManager.Instance.OnlinePlayerCount > 0 && onlineCountText != null)
                {
                    onlineCountText.text = $"{NetworkManager.Instance.OnlinePlayerCount} Online";
                }
                NetworkManager.Instance.Connect();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromNetworkEvents();
        }

        private void Update()
        {
            // Auto-refresh room list when in main lobby
            if (currentState == LobbyState.MainLobby)
            {
                refreshTimer += Time.deltaTime;

                if (isRefreshing)
                {
                    // Timeout loading after 5 seconds
                    if (refreshTimer >= 5f)
                    {
                        Debug.Log("[LobbyUI] Refresh timeout - resetting loading state");
                        isRefreshing = false;
                        refreshTimer = 0f;
                        if (refreshButtonText != null) refreshButtonText.text = "Refresh";
                        if (loadingIndicator != null) loadingIndicator.SetActive(false);
                    }
                }
                else if (refreshTimer >= AUTO_REFRESH_INTERVAL)
                {
                    refreshTimer = 0f;
                    OnRefreshClicked();
                }
            }
        }

        private void GenerateSprites()
        {
            // Generate rounded rectangle sprite
            roundedSprite = CreateRoundedSprite(64, 64, 16);
            circleSprite = CreateCircleSprite(64);
            gradientSprite = CreateVerticalGradientSprite(32);
        }

        private Sprite CreateVerticalGradientSprite(int height)
        {
            // Load custom background image from Resources
            Texture2D bgTex = Resources.Load<Texture2D>("backgrounds/background");
            if (bgTex != null)
            {
                return Sprite.Create(bgTex, new Rect(0, 0, bgTex.width, bgTex.height), new Vector2(0.5f, 0.5f));
            }

            // Fallback: solid dark color
            Debug.LogWarning("[LobbyUI] Failed to load backgrounds/background, using fallback color");
            Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, new Color(0.04f, 0.08f, 0.14f, 1f));
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateRoundedSprite(int width, int height, int radius)
        {
            Texture2D tex = new Texture2D(width, height);
            Color[] pixels = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float alpha = 1f;

                    // Check corners
                    if (x < radius && y < radius)
                        alpha = GetCornerAlpha(x, y, radius, radius, radius);
                    else if (x >= width - radius && y < radius)
                        alpha = GetCornerAlpha(x, y, width - radius - 1, radius, radius);
                    else if (x < radius && y >= height - radius)
                        alpha = GetCornerAlpha(x, y, radius, height - radius - 1, radius);
                    else if (x >= width - radius && y >= height - radius)
                        alpha = GetCornerAlpha(x, y, width - radius - 1, height - radius - 1, radius);

                    pixels[y * width + x] = new Color(1, 1, 1, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }

        private Sprite CreateCircleSprite(int size)
        {
            Texture2D tex = new Texture2D(size, size);
            Color[] pixels = new Color[size * size];
            float center = size / 2f;
            float radius = size / 2f - 1;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float alpha = Mathf.Clamp01(radius - dist + 1);
                    pixels[y * size + x] = new Color(1, 1, 1, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private float GetCornerAlpha(int x, int y, int cx, int cy, int radius)
        {
            float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
            return Mathf.Clamp01(radius - dist + 1);
        }

        private void SubscribeToNetworkEvents()
        {
            if (NetworkManager.Instance == null) return;

            NetworkManager.Instance.OnConnectionStateChanged += OnConnectionStateChanged;
            NetworkManager.Instance.OnRoomListReceived += OnRoomListReceived;
            NetworkManager.Instance.OnLiveGamesReceived += OnLiveGamesReceived;
            NetworkManager.Instance.OnRoomJoined += OnRoomJoined;
            NetworkManager.Instance.OnRoomUpdated += OnRoomUpdated;
            NetworkManager.Instance.OnPlayerJoined += OnPlayerJoined;
            NetworkManager.Instance.OnPlayerLeft += OnPlayerLeft;
            NetworkManager.Instance.OnPositionSelected += OnPositionSelectedHandler;
            NetworkManager.Instance.OnGameStarted += OnGameStarted;
            NetworkManager.Instance.OnError += OnNetworkError;
            NetworkManager.Instance.OnOnlineCountChanged += OnOnlineCountChanged;
        }

        private void UnsubscribeFromNetworkEvents()
        {
            if (NetworkManager.Instance == null) return;

            NetworkManager.Instance.OnConnectionStateChanged -= OnConnectionStateChanged;
            NetworkManager.Instance.OnRoomListReceived -= OnRoomListReceived;
            NetworkManager.Instance.OnLiveGamesReceived -= OnLiveGamesReceived;
            NetworkManager.Instance.OnRoomJoined -= OnRoomJoined;
            NetworkManager.Instance.OnRoomUpdated -= OnRoomUpdated;
            NetworkManager.Instance.OnPlayerJoined -= OnPlayerJoined;
            NetworkManager.Instance.OnPlayerLeft -= OnPlayerLeft;
            NetworkManager.Instance.OnPositionSelected -= OnPositionSelectedHandler;
            NetworkManager.Instance.OnGameStarted -= OnGameStarted;
            NetworkManager.Instance.OnError -= OnNetworkError;
            NetworkManager.Instance.OnOnlineCountChanged -= OnOnlineCountChanged;
        }

        private void OnOnlineCountChanged(int count)
        {
            Debug.Log($"[LobbyUI] OnOnlineCountChanged: {count}");
            if (onlineCountText != null)
            {
                onlineCountText.text = $"{count} Online";
            }
        }

        private void OnPositionSelectedHandler(string playerId, string newPosition, string oldPosition)
        {
            Debug.Log($"[LobbyUI] Position selected: {playerId} moved from {oldPosition} to {newPosition}");
            // Refresh the grid when any position changes
            if (NetworkManager.Instance?.CurrentRoom != null)
            {
                UpdatePlayerGrid(NetworkManager.Instance.CurrentRoom);
            }
        }

        private void CreateUI()
        {
            // Root canvas
            rootPanel = new GameObject("LobbyUI");
            rootPanel.transform.SetParent(transform);

            Canvas canvas = rootPanel.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = rootPanel.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            rootPanel.AddComponent<GraphicRaycaster>();

            // Background with gradient
            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(rootPanel.transform, false);
            RectTransform bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            Image bgImage = bg.AddComponent<Image>();
            bgImage.sprite = gradientSprite;
            bgImage.color = Color.white;

            Debug.Log("[LobbyUI] Creating panels...");
            CreateConnectingPanel(bg.transform);
            CreateMainLobbyPanel(bg.transform);
            CreateRoomPanel(bg.transform);
            Debug.Log($"[LobbyUI] All panels created. roomListContent: {roomListContent != null}");

            SetState(LobbyState.Connecting);
        }

        private void CreateConnectingPanel(Transform parent)
        {
            connectingPanel = CreateFullPanel(parent, "ConnectingPanel");

            // Center container
            GameObject container = CreateCard(connectingPanel.transform, "Container", Vector2.zero, new Vector2(400, 200));

            // Loading spinner placeholder
            GameObject spinner = new GameObject("Spinner");
            spinner.transform.SetParent(container.transform, false);
            RectTransform spinnerRect = spinner.AddComponent<RectTransform>();
            spinnerRect.anchoredPosition = new Vector2(0, 30);
            spinnerRect.sizeDelta = new Vector2(60, 60);
            Image spinnerImg = spinner.AddComponent<Image>();
            spinnerImg.sprite = circleSprite;
            spinnerImg.color = AccentCyan;

            // Text
            CreateLabel(container.transform, "Text", "Connecting...", 28, TextPrimary, new Vector2(0, -40));
        }

        private void CreateMainLobbyPanel(Transform parent)
        {
            mainLobbyPanel = CreateFullPanel(parent, "MainLobbyPanel");

            // Scrollable content
            GameObject scrollView = CreateScrollView(mainLobbyPanel.transform, "MainScroll", true);
            Transform content = scrollView.GetComponent<ScrollRect>().content;

            // Add padding at top
            CreateSpacer(content, 40);

            // Header section
            CreateMainHeader(content);

            // Quick Actions - Join/Create
            CreateQuickActionsSection(content);

            // Available Rooms and Live Games side by side
            CreateRoomsAndLiveGamesSection(content);

            // Bottom spacer
            CreateSpacer(content, 100);

            // Fixed back button at bottom
            CreateFixedBackButton(mainLobbyPanel.transform);
        }

        private void CreateMainHeader(Transform parent)
        {
            GameObject header = new GameObject("Header");
            header.transform.SetParent(parent, false);
            RectTransform headerRect = header.AddComponent<RectTransform>();
            headerRect.sizeDelta = new Vector2(0, 160);
            LayoutElement headerLayout = header.AddComponent<LayoutElement>();
            headerLayout.preferredHeight = 160;
            headerLayout.flexibleWidth = 1;

            // Title
            CreateLabel(header.transform, "Title", "ONLINE LOBBY", 48, AccentCyan, new Vector2(0, 30), FontStyles.Bold);

            // Online players count badge (top right)
            GameObject onlineBadge = new GameObject("OnlineBadge");
            onlineBadge.transform.SetParent(header.transform, false);
            RectTransform badgeRect = onlineBadge.AddComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(1, 1);
            badgeRect.anchorMax = new Vector2(1, 1);
            badgeRect.pivot = new Vector2(1, 1);
            badgeRect.anchoredPosition = new Vector2(-20, -15);
            badgeRect.sizeDelta = new Vector2(160, 36);

            Image badgeBg = onlineBadge.AddComponent<Image>();
            badgeBg.sprite = roundedSprite;
            badgeBg.type = Image.Type.Sliced;
            badgeBg.color = new Color(0.15f, 0.35f, 0.20f, 0.9f);

            // Green dot
            GameObject dot = new GameObject("Dot");
            dot.transform.SetParent(onlineBadge.transform, false);
            RectTransform dotRect = dot.AddComponent<RectTransform>();
            dotRect.anchorMin = new Vector2(0, 0.5f);
            dotRect.anchorMax = new Vector2(0, 0.5f);
            dotRect.anchoredPosition = new Vector2(16, 0);
            dotRect.sizeDelta = new Vector2(10, 10);
            Image dotImg = dot.AddComponent<Image>();
            dotImg.sprite = circleSprite;
            dotImg.color = AccentGreen;

            onlineCountText = CreateLabel(onlineBadge.transform, "Count", "1 Online", 18, AccentGreen, new Vector2(10, 0), FontStyles.Bold);
            onlineCountText.alignment = TextAlignmentOptions.Center;

            // Player info bar
            GameObject infoBar = CreateCard(header.transform, "InfoBar", new Vector2(0, -50), new Vector2(980, 60));

            string playerName = PlayerProfileManager.Instance?.CurrentProfile?.DisplayName ?? "Player";

            // Avatar circle
            GameObject avatar = new GameObject("Avatar");
            avatar.transform.SetParent(infoBar.transform, false);
            RectTransform avatarRect = avatar.AddComponent<RectTransform>();
            avatarRect.anchorMin = new Vector2(0, 0.5f);
            avatarRect.anchorMax = new Vector2(0, 0.5f);
            avatarRect.anchoredPosition = new Vector2(40, 0);
            avatarRect.sizeDelta = new Vector2(40, 40);
            Image avatarImg = avatar.AddComponent<Image>();
            avatarImg.sprite = circleSprite;
            avatarImg.color = AccentCyan;

            CreateLabel(avatar.transform, "Initial", playerName.Length > 0 ? playerName[0].ToString().ToUpper() : "?",
                20, BgDark, Vector2.zero, FontStyles.Bold);

            // Player name
            TextMeshProUGUI nameText = CreateLabel(infoBar.transform, "Name", playerName, 24, TextPrimary,
                new Vector2(80, 0), FontStyles.Normal, TextAlignmentOptions.Left);
            nameText.GetComponent<RectTransform>().anchorMin = new Vector2(0, 0.5f);
            nameText.GetComponent<RectTransform>().anchorMax = new Vector2(0, 0.5f);

            // Status indicator
            GameObject status = new GameObject("Status");
            status.transform.SetParent(infoBar.transform, false);
            RectTransform statusRect = status.AddComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(1, 0.5f);
            statusRect.anchorMax = new Vector2(1, 0.5f);
            statusRect.anchoredPosition = new Vector2(-40, 0);
            statusRect.sizeDelta = new Vector2(12, 12);
            Image statusImg = status.AddComponent<Image>();
            statusImg.sprite = circleSprite;
            statusImg.color = AccentGreen;
        }

        private void CreateQuickActionsSection(Transform parent)
        {
            GameObject section = CreateSection(parent, "QuickActions", 180);

            // Create Room card (centered, full width)
            GameObject createCard = CreateCard(section.transform, "CreateCard", new Vector2(0, -20), new Vector2(500, 140));

            CreateLabel(createCard.transform, "Title", "Create Room", 28, TextPrimary, new Vector2(0, 35), FontStyles.Bold);

            newRoomNameInput = CreateModernInput(createCard.transform, "NameInput", "Room Name (optional)", new Vector2(-80, -20), new Vector2(300, 56));

            CreateModernButton(createCard.transform, "CreateBtn", "CREATE", AccentGreen, new Vector2(160, -20), new Vector2(140, 50), OnCreateRoomClicked);
        }

        private void CreateRoomsAndLiveGamesSection(Transform parent)
        {
            Debug.Log("[LobbyUI] CreateRoomsAndLiveGamesSection called");

            // Container for both sections side by side
            GameObject container = new GameObject("RoomsAndLiveGames");
            container.transform.SetParent(parent, false);
            RectTransform containerRect = container.AddComponent<RectTransform>();
            containerRect.sizeDelta = new Vector2(0, 420);
            LayoutElement containerLayout = container.AddComponent<LayoutElement>();
            containerLayout.preferredHeight = 420;
            containerLayout.flexibleWidth = 1;

            HorizontalLayoutGroup hLayout = container.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 20;
            hLayout.padding = new RectOffset(50, 50, 10, 10);
            hLayout.childForceExpandWidth = true;
            hLayout.childForceExpandHeight = true;
            hLayout.childControlWidth = true;
            hLayout.childControlHeight = true;

            // === LEFT: Available Rooms ===
            GameObject roomsPanel = new GameObject("RoomsPanel");
            roomsPanel.transform.SetParent(container.transform, false);
            RectTransform roomsRect = roomsPanel.AddComponent<RectTransform>();
            LayoutElement roomsLayout = roomsPanel.AddComponent<LayoutElement>();
            roomsLayout.flexibleWidth = 1;
            roomsLayout.flexibleHeight = 1;

            Image roomsBg = roomsPanel.AddComponent<Image>();
            roomsBg.sprite = roundedSprite;
            roomsBg.color = CardBg;
            roomsBg.type = Image.Type.Sliced;

            // Rooms header
            GameObject roomsHeader = new GameObject("Header");
            roomsHeader.transform.SetParent(roomsPanel.transform, false);
            RectTransform roomsHeaderRect = roomsHeader.AddComponent<RectTransform>();
            roomsHeaderRect.anchorMin = new Vector2(0, 1);
            roomsHeaderRect.anchorMax = new Vector2(1, 1);
            roomsHeaderRect.pivot = new Vector2(0.5f, 1);
            roomsHeaderRect.anchoredPosition = new Vector2(0, -10);
            roomsHeaderRect.sizeDelta = new Vector2(-30, 50);

            // Title label - anchored to left
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(roomsHeader.transform, false);
            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.5f);
            titleRect.anchorMax = new Vector2(0, 0.5f);
            titleRect.pivot = new Vector2(0, 0.5f);
            titleRect.anchoredPosition = new Vector2(15, 0);
            titleRect.sizeDelta = new Vector2(200, 40);
            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "Available Rooms";
            titleText.fontSize = 24;
            titleText.color = TextPrimary;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Left;

            // Refresh button - anchored to right
            Button refreshBtn = CreateModernButton(roomsHeader.transform, "RefreshBtn", "Refresh", AccentCyan, new Vector2(-15, 0), new Vector2(120, 36), OnRefreshClicked);
            RectTransform refreshRect = refreshBtn.GetComponent<RectTransform>();
            refreshRect.anchorMin = new Vector2(1, 0.5f);
            refreshRect.anchorMax = new Vector2(1, 0.5f);
            refreshRect.pivot = new Vector2(1, 0.5f);
            refreshButtonText = refreshBtn.GetComponentInChildren<TextMeshProUGUI>();

            // Loading indicator
            loadingIndicator = new GameObject("LoadingIndicator");
            loadingIndicator.transform.SetParent(roomsHeader.transform, false);
            RectTransform loadingRect = loadingIndicator.AddComponent<RectTransform>();
            loadingRect.anchorMin = new Vector2(1, 0.5f);
            loadingRect.anchorMax = new Vector2(1, 0.5f);
            loadingRect.anchoredPosition = new Vector2(-120, 0);
            loadingRect.sizeDelta = new Vector2(30, 30);
            TextMeshProUGUI loadingText = loadingIndicator.AddComponent<TextMeshProUGUI>();
            loadingText.text = "...";
            loadingText.fontSize = 20;
            loadingText.color = AccentCyan;
            loadingText.alignment = TextAlignmentOptions.Center;
            loadingIndicator.SetActive(false);

            // Rooms scroll view
            GameObject roomsScrollView = new GameObject("ScrollView");
            roomsScrollView.transform.SetParent(roomsPanel.transform, false);
            RectTransform roomsScrollRect = roomsScrollView.AddComponent<RectTransform>();
            roomsScrollRect.anchorMin = new Vector2(0, 0);
            roomsScrollRect.anchorMax = new Vector2(1, 1);
            roomsScrollRect.offsetMin = new Vector2(10, 10);
            roomsScrollRect.offsetMax = new Vector2(-10, -60);

            ScrollRect roomsScroll = roomsScrollView.AddComponent<ScrollRect>();
            roomsScroll.horizontal = false;
            roomsScroll.vertical = true;
            roomsScroll.scrollSensitivity = 30;

            GameObject roomsViewport = new GameObject("Viewport");
            roomsViewport.transform.SetParent(roomsScrollView.transform, false);
            RectTransform roomsViewportRect = roomsViewport.AddComponent<RectTransform>();
            roomsViewportRect.anchorMin = Vector2.zero;
            roomsViewportRect.anchorMax = Vector2.one;
            roomsViewportRect.offsetMin = Vector2.zero;
            roomsViewportRect.offsetMax = Vector2.zero;
            roomsViewport.AddComponent<RectMask2D>();
            roomsScroll.viewport = roomsViewportRect;

            GameObject roomsContent = new GameObject("Content");
            roomsContent.transform.SetParent(roomsViewport.transform, false);
            RectTransform roomsContentRect = roomsContent.AddComponent<RectTransform>();
            roomListContent = roomsContent.transform;  // Assign AFTER adding RectTransform
            Debug.Log($"[LobbyUI] roomListContent assigned: {roomListContent != null}");
            roomsContentRect.anchorMin = new Vector2(0, 1);
            roomsContentRect.anchorMax = new Vector2(1, 1);
            roomsContentRect.pivot = new Vector2(0.5f, 1);
            roomsContentRect.anchoredPosition = Vector2.zero;
            roomsContentRect.sizeDelta = new Vector2(0, 0);

            VerticalLayoutGroup roomsVLayout = roomsContent.AddComponent<VerticalLayoutGroup>();
            roomsVLayout.spacing = 8;
            roomsVLayout.padding = new RectOffset(5, 5, 5, 5);
            roomsVLayout.childForceExpandWidth = true;
            roomsVLayout.childForceExpandHeight = false;
            roomsVLayout.childControlWidth = true;
            roomsVLayout.childControlHeight = false;

            ContentSizeFitter roomsFitter = roomsContent.AddComponent<ContentSizeFitter>();
            roomsFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            roomsScroll.content = roomsContentRect;

            // === RIGHT: Live Games ===
            GameObject livePanel = new GameObject("LiveGamesPanel");
            livePanel.transform.SetParent(container.transform, false);
            RectTransform liveRect = livePanel.AddComponent<RectTransform>();
            LayoutElement liveLayout = livePanel.AddComponent<LayoutElement>();
            liveLayout.flexibleWidth = 1;
            liveLayout.flexibleHeight = 1;

            Image liveBg = livePanel.AddComponent<Image>();
            liveBg.sprite = roundedSprite;
            liveBg.color = CardBg;
            liveBg.type = Image.Type.Sliced;

            // Live games header
            GameObject liveHeader = new GameObject("Header");
            liveHeader.transform.SetParent(livePanel.transform, false);
            RectTransform liveHeaderRect = liveHeader.AddComponent<RectTransform>();
            liveHeaderRect.anchorMin = new Vector2(0, 1);
            liveHeaderRect.anchorMax = new Vector2(1, 1);
            liveHeaderRect.pivot = new Vector2(0.5f, 1);
            liveHeaderRect.anchoredPosition = new Vector2(0, -10);
            liveHeaderRect.sizeDelta = new Vector2(-30, 50);

            // Title - anchored to left
            GameObject liveTitleObj = new GameObject("Title");
            liveTitleObj.transform.SetParent(liveHeader.transform, false);
            RectTransform liveTitleRect = liveTitleObj.AddComponent<RectTransform>();
            liveTitleRect.anchorMin = new Vector2(0, 0.5f);
            liveTitleRect.anchorMax = new Vector2(1, 0.5f);
            liveTitleRect.pivot = new Vector2(0, 0.5f);
            liveTitleRect.anchoredPosition = new Vector2(15, 5);
            liveTitleRect.sizeDelta = new Vector2(0, 30);
            TextMeshProUGUI liveTitleText = liveTitleObj.AddComponent<TextMeshProUGUI>();
            liveTitleText.text = "Live Games";
            liveTitleText.fontSize = 24;
            liveTitleText.color = TextPrimary;
            liveTitleText.fontStyle = FontStyles.Bold;
            liveTitleText.alignment = TextAlignmentOptions.Left;

            // Subtitle
            GameObject liveSubObj = new GameObject("Subtitle");
            liveSubObj.transform.SetParent(liveHeader.transform, false);
            RectTransform liveSubRect = liveSubObj.AddComponent<RectTransform>();
            liveSubRect.anchorMin = new Vector2(0, 0.5f);
            liveSubRect.anchorMax = new Vector2(1, 0.5f);
            liveSubRect.pivot = new Vector2(0, 0.5f);
            liveSubRect.anchoredPosition = new Vector2(15, -15);
            liveSubRect.sizeDelta = new Vector2(0, 20);
            TextMeshProUGUI liveSubText = liveSubObj.AddComponent<TextMeshProUGUI>();
            liveSubText.text = "Watch ongoing matches";
            liveSubText.fontSize = 14;
            liveSubText.color = TextSecondary;
            liveSubText.alignment = TextAlignmentOptions.Left;

            // Live games scroll view (vertical now for side-by-side layout)
            GameObject liveScrollView = new GameObject("ScrollView");
            liveScrollView.transform.SetParent(livePanel.transform, false);
            RectTransform liveScrollRect = liveScrollView.AddComponent<RectTransform>();
            liveScrollRect.anchorMin = new Vector2(0, 0);
            liveScrollRect.anchorMax = new Vector2(1, 1);
            liveScrollRect.offsetMin = new Vector2(10, 10);
            liveScrollRect.offsetMax = new Vector2(-10, -60);

            ScrollRect liveScroll = liveScrollView.AddComponent<ScrollRect>();
            liveScroll.horizontal = false;
            liveScroll.vertical = true;
            liveScroll.scrollSensitivity = 30;

            GameObject liveViewport = new GameObject("Viewport");
            liveViewport.transform.SetParent(liveScrollView.transform, false);
            RectTransform liveViewportRect = liveViewport.AddComponent<RectTransform>();
            liveViewportRect.anchorMin = Vector2.zero;
            liveViewportRect.anchorMax = Vector2.one;
            liveViewportRect.offsetMin = Vector2.zero;
            liveViewportRect.offsetMax = Vector2.zero;
            liveViewport.AddComponent<RectMask2D>();
            liveScroll.viewport = liveViewportRect;

            GameObject liveContent = new GameObject("Content");
            liveContent.transform.SetParent(liveViewport.transform, false);
            RectTransform liveContentRect = liveContent.AddComponent<RectTransform>();
            liveGamesContent = liveContent.transform;  // Assign AFTER adding RectTransform
            liveContentRect.anchorMin = new Vector2(0, 1);
            liveContentRect.anchorMax = new Vector2(1, 1);
            liveContentRect.pivot = new Vector2(0.5f, 1);
            liveContentRect.anchoredPosition = Vector2.zero;
            liveContentRect.sizeDelta = new Vector2(0, 0);

            VerticalLayoutGroup liveVLayout = liveContent.AddComponent<VerticalLayoutGroup>();
            liveVLayout.spacing = 8;
            liveVLayout.padding = new RectOffset(5, 5, 5, 5);
            liveVLayout.childForceExpandWidth = true;
            liveVLayout.childForceExpandHeight = false;
            liveVLayout.childControlWidth = true;
            liveVLayout.childControlHeight = false;

            ContentSizeFitter liveFitter = liveContent.AddComponent<ContentSizeFitter>();
            liveFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            liveScroll.content = liveContentRect;

            // Empty state for live games
            GameObject emptyMsg = new GameObject("EmptyMessage");
            emptyMsg.transform.SetParent(livePanel.transform, false);
            RectTransform emptyRect = emptyMsg.AddComponent<RectTransform>();
            emptyRect.anchorMin = new Vector2(0, 0);
            emptyRect.anchorMax = new Vector2(1, 1);
            emptyRect.offsetMin = new Vector2(10, 10);
            emptyRect.offsetMax = new Vector2(-10, -60);
            TextMeshProUGUI emptyText = emptyMsg.AddComponent<TextMeshProUGUI>();
            emptyText.text = "No live games\nright now";
            emptyText.fontSize = 18;
            emptyText.color = TextMuted;
            emptyText.alignment = TextAlignmentOptions.Center;
            emptyText.verticalAlignment = VerticalAlignmentOptions.Middle;
        }

        private void CreateFixedBackButton(Transform parent)
        {
            GameObject backBar = new GameObject("BackBar");
            backBar.transform.SetParent(parent, false);
            RectTransform barRect = backBar.AddComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0, 0);
            barRect.anchorMax = new Vector2(1, 0);
            barRect.anchoredPosition = new Vector2(0, 50);
            barRect.sizeDelta = new Vector2(0, 80);

            Image barBg = backBar.AddComponent<Image>();
            barBg.color = new Color(BgDark.r, BgDark.g, BgDark.b, 0.95f);

            CreateModernButton(backBar.transform, "BackBtn", "← Back to Menu", CardBgHover, Vector2.zero, new Vector2(300, 50), OnBackButtonClicked);
        }

        private void CreateRoomPanel(Transform parent)
        {
            roomPanel = CreateFullPanel(parent, "RoomPanel");

            // Background
            Image panelBg = roomPanel.AddComponent<Image>();
            panelBg.color = BgDark;

            // Main container with padding
            GameObject container = new GameObject("Container");
            container.transform.SetParent(roomPanel.transform, false);
            RectTransform containerRect = container.AddComponent<RectTransform>();
            containerRect.anchorMin = Vector2.zero;
            containerRect.anchorMax = Vector2.one;
            containerRect.offsetMin = new Vector2(40, 100);
            containerRect.offsetMax = new Vector2(-40, -40);

            // Header section
            CreateRoomHeader(container.transform);

            // Players grid (2x2 layout)
            CreatePlayersGrid(container.transform);

            // Bottom actions bar
            CreateActionsBar(container.transform);
        }

        private void CreateRoomHeader(Transform parent)
        {
            GameObject header = new GameObject("Header");
            header.transform.SetParent(parent, false);
            RectTransform rect = header.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0, 160);

            // Header card background
            Image bg = header.AddComponent<Image>();
            bg.sprite = roundedSprite;
            bg.type = Image.Type.Sliced;
            bg.color = CardBg;

            // Room name (large, centered)
            roomNameText = CreateLabel(header.transform, "RoomName", "Room Name", 28, AccentCyan, new Vector2(0, 35), FontStyles.Bold);

            // Room code below name
            roomCodeText = CreateLabel(header.transform, "RoomCode", "Public Room", 16, TextSecondary, new Vector2(0, 10));

            // Online players bar (bottom of header)
            GameObject playersBar = new GameObject("OnlinePlayersBar");
            playersBar.transform.SetParent(header.transform, false);
            RectTransform barRect = playersBar.AddComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0, 0);
            barRect.anchorMax = new Vector2(1, 0);
            barRect.pivot = new Vector2(0.5f, 0);
            barRect.anchoredPosition = new Vector2(0, 8);
            barRect.sizeDelta = new Vector2(-30, 50);

            // Bar background
            Image barBg = playersBar.AddComponent<Image>();
            barBg.sprite = roundedSprite;
            barBg.type = Image.Type.Sliced;
            barBg.color = new Color(0.08f, 0.10f, 0.16f, 0.8f);

            // Player count text (left side)
            playerCountText = CreateLabel(playersBar.transform, "PlayerCount", "0/4 Players", 16, TextSecondary, Vector2.zero, FontStyles.Bold);
            RectTransform countRect = playerCountText.GetComponent<RectTransform>();
            countRect.anchorMin = new Vector2(0, 0);
            countRect.anchorMax = new Vector2(0, 1);
            countRect.pivot = new Vector2(0, 0.5f);
            countRect.anchoredPosition = new Vector2(15, 0);
            countRect.sizeDelta = new Vector2(120, 0);
            playerCountText.alignment = TextAlignmentOptions.Left;

            // Online player avatars container (right side)
            GameObject avatarsRow = new GameObject("AvatarsRow");
            avatarsRow.transform.SetParent(playersBar.transform, false);
            RectTransform avatarsRect = avatarsRow.AddComponent<RectTransform>();
            avatarsRect.anchorMin = new Vector2(0, 0);
            avatarsRect.anchorMax = new Vector2(1, 1);
            avatarsRect.offsetMin = new Vector2(130, 5);
            avatarsRect.offsetMax = new Vector2(-10, -5);

            HorizontalLayoutGroup hlg = avatarsRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.padding = new RectOffset(5, 5, 2, 2);

            onlinePlayersBar = avatarsRow.transform;

            // Leave button (top right corner)
            GameObject leaveBtn = new GameObject("LeaveBtn");
            leaveBtn.transform.SetParent(header.transform, false);
            RectTransform leaveBtnRect = leaveBtn.AddComponent<RectTransform>();
            leaveBtnRect.anchorMin = new Vector2(1, 1);
            leaveBtnRect.anchorMax = new Vector2(1, 1);
            leaveBtnRect.pivot = new Vector2(1, 1);
            leaveBtnRect.anchoredPosition = new Vector2(-15, -12);
            leaveBtnRect.sizeDelta = new Vector2(90, 36);

            Image leaveBg = leaveBtn.AddComponent<Image>();
            leaveBg.sprite = roundedSprite;
            leaveBg.type = Image.Type.Sliced;
            leaveBg.color = AccentRed;

            Button leaveButton = leaveBtn.AddComponent<Button>();
            leaveButton.onClick.AddListener(OnLeaveRoomClicked);

            GameObject leaveText = new GameObject("Text");
            leaveText.transform.SetParent(leaveBtn.transform, false);
            RectTransform leaveTextRect = leaveText.AddComponent<RectTransform>();
            leaveTextRect.anchorMin = Vector2.zero;
            leaveTextRect.anchorMax = Vector2.one;
            leaveTextRect.offsetMin = Vector2.zero;
            leaveTextRect.offsetMax = Vector2.zero;
            TextMeshProUGUI leaveTextTmp = leaveText.AddComponent<TextMeshProUGUI>();
            leaveTextTmp.text = "Leave";
            leaveTextTmp.fontSize = 15;
            leaveTextTmp.color = TextPrimary;
            leaveTextTmp.alignment = TextAlignmentOptions.Center;
            leaveTextTmp.fontStyle = FontStyles.Bold;
        }

        private void CreatePlayersGrid(Transform parent)
        {
            GameObject gridSection = new GameObject("PlayersGrid");
            gridSection.transform.SetParent(parent, false);
            RectTransform gridRect = gridSection.AddComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0, 0);
            gridRect.anchorMax = new Vector2(1, 1);
            gridRect.offsetMin = new Vector2(20, 180);  // Space at bottom for actions bar
            gridRect.offsetMax = new Vector2(-20, -170);  // Space at top for header

            playerListContent = gridSection.transform;

            // Modern gradient-style team colors
            Color teamNSColor = new Color(0.18f, 0.45f, 0.85f, 1f);  // Vibrant blue
            Color teamEWColor = new Color(0.85f, 0.45f, 0.18f, 1f);  // Vibrant orange

            // Larger team cards for better visibility
            float teamCardWidth = 580;
            float teamCardHeight = 380;
            float gapX = 30;

            // Team North-South (left) - "Your Team" style
            CreateTeamCard(gridSection.transform, "TeamNS", "TEAM BLUE", teamNSColor,
                new Vector2(-teamCardWidth / 2 - gapX / 2, 0),
                new Vector2(teamCardWidth, teamCardHeight),
                new string[] { "North", "South" },
                new int[] { 0, 3 });

            // Team East-West (right)
            CreateTeamCard(gridSection.transform, "TeamEW", "TEAM ORANGE", teamEWColor,
                new Vector2(teamCardWidth / 2 + gapX / 2, 0),
                new Vector2(teamCardWidth, teamCardHeight),
                new string[] { "East", "West" },
                new int[] { 1, 2 });
        }

        private void CreateTeamCard(Transform parent, string name, string teamLabel, Color teamColor,
            Vector2 position, Vector2 size, string[] positions, int[] slotIndices)
        {
            GameObject teamCard = new GameObject(name);
            teamCard.transform.SetParent(parent, false);
            RectTransform teamRect = teamCard.AddComponent<RectTransform>();
            teamRect.anchoredPosition = position;
            teamRect.sizeDelta = size;

            // Modern team background with subtle gradient effect
            Image teamBg = teamCard.AddComponent<Image>();
            teamBg.sprite = roundedSprite;
            teamBg.type = Image.Type.Sliced;
            // Darker background with team color accent
            teamBg.color = new Color(0.10f, 0.11f, 0.14f, 0.98f);

            // Top accent bar for team color
            GameObject accentBar = new GameObject("AccentBar");
            accentBar.transform.SetParent(teamCard.transform, false);
            RectTransform accentRect = accentBar.AddComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0, 1);
            accentRect.anchorMax = new Vector2(1, 1);
            accentRect.pivot = new Vector2(0.5f, 1);
            accentRect.anchoredPosition = Vector2.zero;
            accentRect.sizeDelta = new Vector2(0, 4);
            Image accentImg = accentBar.AddComponent<Image>();
            accentImg.color = teamColor;

            // Team label with icon-like design
            GameObject labelContainer = new GameObject("LabelContainer");
            labelContainer.transform.SetParent(teamCard.transform, false);
            RectTransform labelContRect = labelContainer.AddComponent<RectTransform>();
            labelContRect.anchorMin = new Vector2(0.5f, 1);
            labelContRect.anchorMax = new Vector2(0.5f, 1);
            labelContRect.pivot = new Vector2(0.5f, 1);
            labelContRect.anchoredPosition = new Vector2(0, -14);
            labelContRect.sizeDelta = new Vector2(240, 38);

            // Team label background pill
            Image labelBg = labelContainer.AddComponent<Image>();
            labelBg.sprite = roundedSprite;
            labelBg.type = Image.Type.Sliced;
            labelBg.color = new Color(teamColor.r, teamColor.g, teamColor.b, 0.25f);

            GameObject labelObj = new GameObject("TeamLabel");
            labelObj.transform.SetParent(labelContainer.transform, false);
            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = teamLabel;
            labelText.fontSize = 20;
            labelText.color = new Color(teamColor.r * 0.6f + 0.4f, teamColor.g * 0.6f + 0.4f, teamColor.b * 0.6f + 0.4f, 1f);
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.fontStyle = FontStyles.Bold;

            // Two player slots - larger for better visibility
            float slotHeight = 140;
            float slotGap = 16;
            float topPadding = 60;

            float startY = size.y / 2 - topPadding - slotHeight / 2;

            for (int i = 0; i < 2; i++)
            {
                float yPos = startY - (slotHeight + slotGap) * i;
                CreateTeamPlayerSlot(teamCard.transform, slotIndices[i], positions[i],
                    new Vector2(0, yPos), new Vector2(size.x - 20, slotHeight), teamColor);
            }
        }

        private void CreateTeamPlayerSlot(Transform parent, int slotIndex, string positionName,
            Vector2 position, Vector2 size, Color teamColor)
        {
            GameObject slot = new GameObject($"Slot_{slotIndex}");
            slot.transform.SetParent(parent, false);
            RectTransform rect = slot.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            // Sleek slot background
            Image bg = slot.AddComponent<Image>();
            bg.sprite = roundedSprite;
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0.08f, 0.09f, 0.11f, 0.9f);

            // Make slot clickable for position selection
            Button slotButton = slot.AddComponent<Button>();
            slotButton.targetGraphic = bg;  // Explicitly set target graphic
            string pos = positionName;
            slotButton.onClick.AddListener(() => OnPositionSlotClicked(pos));
            Debug.Log($"[LobbyUI] Created slot button for position: {pos}");

            // Hover color for button
            ColorBlock colors = slotButton.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f);
            colors.pressedColor = new Color(0.9f, 0.9f, 0.9f);
            colors.selectedColor = Color.white;
            slotButton.colors = colors;

            // Left side: Position indicator with team color
            GameObject posIndicator = new GameObject("PosIndicator");
            posIndicator.transform.SetParent(slot.transform, false);
            RectTransform posIndRect = posIndicator.AddComponent<RectTransform>();
            posIndRect.anchorMin = new Vector2(0, 0);
            posIndRect.anchorMax = new Vector2(0, 1);
            posIndRect.pivot = new Vector2(0, 0.5f);
            posIndRect.anchoredPosition = new Vector2(0, 0);
            posIndRect.sizeDelta = new Vector2(4, 0);
            posIndRect.offsetMin = new Vector2(0, 8);
            posIndRect.offsetMax = new Vector2(4, -8);
            Image posIndImg = posIndicator.AddComponent<Image>();
            posIndImg.color = new Color(teamColor.r, teamColor.g, teamColor.b, 0.6f);
            posIndImg.raycastTarget = false;  // Don't block button clicks

            // Position badge (top-left, pill style)
            GameObject posBadge = new GameObject("PosBadge");
            posBadge.transform.SetParent(slot.transform, false);
            RectTransform posBadgeRect = posBadge.AddComponent<RectTransform>();
            posBadgeRect.anchorMin = new Vector2(0, 1);
            posBadgeRect.anchorMax = new Vector2(0, 1);
            posBadgeRect.pivot = new Vector2(0, 1);
            posBadgeRect.anchoredPosition = new Vector2(18, -10);
            posBadgeRect.sizeDelta = new Vector2(90, 28);
            Image posBadgeBg = posBadge.AddComponent<Image>();
            posBadgeBg.sprite = roundedSprite;
            posBadgeBg.type = Image.Type.Sliced;
            posBadgeBg.color = new Color(teamColor.r, teamColor.g, teamColor.b, 0.2f);
            posBadgeBg.raycastTarget = false;  // Don't block button clicks

            GameObject posLabel = new GameObject("PosLabel");
            posLabel.transform.SetParent(posBadge.transform, false);
            RectTransform posLabelRect = posLabel.AddComponent<RectTransform>();
            posLabelRect.anchorMin = Vector2.zero;
            posLabelRect.anchorMax = Vector2.one;
            posLabelRect.offsetMin = Vector2.zero;
            posLabelRect.offsetMax = Vector2.zero;
            TextMeshProUGUI posLabelText = posLabel.AddComponent<TextMeshProUGUI>();
            posLabelText.text = positionName.ToUpper();
            posLabelText.fontSize = 14;
            posLabelText.color = new Color(teamColor.r * 0.5f + 0.5f, teamColor.g * 0.5f + 0.5f, teamColor.b * 0.5f + 0.5f, 1f);
            posLabelText.fontStyle = FontStyles.Bold;
            posLabelText.alignment = TextAlignmentOptions.Center;
            posLabelText.raycastTarget = false;  // Don't block button clicks

            // Avatar circle (modern ring style)
            GameObject avatarContainer = new GameObject("Avatar");
            avatarContainer.transform.SetParent(slot.transform, false);
            RectTransform avatarRect = avatarContainer.AddComponent<RectTransform>();
            avatarRect.anchorMin = new Vector2(0, 0.5f);
            avatarRect.anchorMax = new Vector2(0, 0.5f);
            avatarRect.anchoredPosition = new Vector2(60, -4);
            avatarRect.sizeDelta = new Vector2(70, 70);

            // Avatar ring (border)
            Image avatarRing = avatarContainer.AddComponent<Image>();
            avatarRing.sprite = circleSprite;
            avatarRing.color = new Color(teamColor.r, teamColor.g, teamColor.b, 0.4f);
            avatarRing.raycastTarget = false;  // Don't block button clicks

            // Avatar inner
            GameObject avatarInner = new GameObject("AvatarInner");
            avatarInner.transform.SetParent(avatarContainer.transform, false);
            RectTransform avatarInnerRect = avatarInner.AddComponent<RectTransform>();
            avatarInnerRect.anchorMin = Vector2.zero;
            avatarInnerRect.anchorMax = Vector2.one;
            avatarInnerRect.offsetMin = new Vector2(4, 4);
            avatarInnerRect.offsetMax = new Vector2(-4, -4);
            Image avatarInnerBg = avatarInner.AddComponent<Image>();
            avatarInnerBg.sprite = circleSprite;
            avatarInnerBg.color = new Color(0.06f, 0.07f, 0.09f, 1f);
            avatarInnerBg.raycastTarget = false;  // Don't block button clicks

            // Avatar initial/icon
            GameObject initial = new GameObject("Initial");
            initial.transform.SetParent(avatarInner.transform, false);
            RectTransform initialRect = initial.AddComponent<RectTransform>();
            initialRect.anchorMin = Vector2.zero;
            initialRect.anchorMax = Vector2.one;
            initialRect.offsetMin = Vector2.zero;
            initialRect.offsetMax = Vector2.zero;
            TextMeshProUGUI initialText = initial.AddComponent<TextMeshProUGUI>();
            initialText.text = "+";
            initialText.fontSize = 28;
            initialText.color = new Color(teamColor.r, teamColor.g, teamColor.b, 0.8f);
            initialText.alignment = TextAlignmentOptions.Center;
            initialText.raycastTarget = false;  // Don't block button clicks

            // Player name (center area)
            GameObject nameObj = new GameObject("PlayerName");
            nameObj.transform.SetParent(slot.transform, false);
            RectTransform nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0.5f);
            nameRect.anchorMax = new Vector2(1, 0.5f);
            nameRect.anchoredPosition = new Vector2(60, 0);
            nameRect.sizeDelta = new Vector2(-160, 32);
            TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
            nameText.text = "Click to join";
            nameText.fontSize = 18;
            nameText.color = TextMuted;
            nameText.fontStyle = FontStyles.Italic;
            nameText.alignment = TextAlignmentOptions.Left;
            nameText.raycastTarget = false;  // Don't block button clicks

            // Status badge (right side)
            GameObject badge = new GameObject("StatusBadge");
            badge.transform.SetParent(slot.transform, false);
            RectTransform badgeRect = badge.AddComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(1, 0.5f);
            badgeRect.anchorMax = new Vector2(1, 0.5f);
            badgeRect.pivot = new Vector2(1, 0.5f);
            badgeRect.anchoredPosition = new Vector2(-16, 0);
            badgeRect.sizeDelta = new Vector2(90, 32);
            Image badgeBg = badge.AddComponent<Image>();
            badgeBg.sprite = roundedSprite;
            badgeBg.type = Image.Type.Sliced;
            badgeBg.color = new Color(0.15f, 0.16f, 0.19f, 1f);
            badgeBg.raycastTarget = false;  // Don't block button clicks

            GameObject badgeTextObj = new GameObject("Text");
            badgeTextObj.transform.SetParent(badge.transform, false);
            RectTransform badgeTextRect = badgeTextObj.AddComponent<RectTransform>();
            badgeTextRect.anchorMin = Vector2.zero;
            badgeTextRect.anchorMax = Vector2.one;
            badgeTextRect.offsetMin = Vector2.zero;
            badgeTextRect.offsetMax = Vector2.zero;
            TextMeshProUGUI badgeTmp = badgeTextObj.AddComponent<TextMeshProUGUI>();
            badgeTmp.text = "OPEN";
            badgeTmp.fontSize = 13;
            badgeTmp.raycastTarget = false;  // Don't block button clicks
            badgeTmp.color = TextMuted;
            badgeTmp.alignment = TextAlignmentOptions.Center;
            badgeTmp.fontStyle = FontStyles.Bold;
        }

        private void OnPositionSlotClicked(string position)
        {
            Debug.Log($"[LobbyUI] >>> Position slot clicked: {position}");

            // Convert string to PlayerPosition
            if (System.Enum.TryParse<Lekha.Core.PlayerPosition>(position, out var playerPosition))
            {
                Debug.Log($"[LobbyUI] Parsed position: {playerPosition}, sending to NetworkManager...");

                if (NetworkManager.Instance == null)
                {
                    Debug.LogError("[LobbyUI] NetworkManager.Instance is null!");
                    return;
                }

                if (NetworkManager.Instance.CurrentRoom == null)
                {
                    Debug.LogError("[LobbyUI] CurrentRoom is null - not in a room!");
                    return;
                }

                NetworkManager.Instance.SelectPosition(playerPosition);
                Debug.Log($"[LobbyUI] SelectPosition called for {playerPosition}");
            }
            else
            {
                Debug.LogError($"[LobbyUI] Failed to parse position: {position}");
            }
        }

        private void CreateActionsBar(Transform parent)
        {
            GameObject actionsBar = new GameObject("ActionsBar");
            actionsBar.transform.SetParent(parent, false);
            RectTransform rect = actionsBar.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0);
            rect.anchorMax = new Vector2(1, 0);
            rect.pivot = new Vector2(0.5f, 0);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0, 170);

            // Actions card background
            Image bg = actionsBar.AddComponent<Image>();
            bg.sprite = roundedSprite;
            bg.type = Image.Type.Sliced;
            bg.color = CardBg;

            // Ready button (large, prominent) - top row at y=100
            readyButton = CreateModernButton(actionsBar.transform, "ReadyBtn", "Ready", AccentGreen, new Vector2(-170, 100), new Vector2(280, 60), OnReadyClicked);
            readyButtonText = readyButton.GetComponentInChildren<TextMeshProUGUI>();
            readyButtonText.fontSize = 24;

            // Start button (host only) - top row at y=100
            startButton = CreateModernButton(actionsBar.transform, "StartBtn", "Start Game", AccentMagenta, new Vector2(170, 100), new Vector2(280, 60), OnStartClicked);
            startButton.GetComponentInChildren<TextMeshProUGUI>().fontSize = 24;

            // Voice chat controls handled by InGameVoiceChatUI during gameplay
        }

        // Helper methods
        private GameObject CreateFullPanel(Transform parent, string name)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return panel;
        }

        private GameObject CreateSection(Transform parent, string name, float height)
        {
            GameObject section = new GameObject(name);
            section.transform.SetParent(parent, false);
            RectTransform rect = section.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, height);
            LayoutElement layout = section.AddComponent<LayoutElement>();
            layout.preferredHeight = height;
            layout.flexibleWidth = 1;
            return section;
        }

        private void CreateSpacer(Transform parent, float height)
        {
            GameObject spacer = new GameObject("Spacer");
            spacer.transform.SetParent(parent, false);
            RectTransform rect = spacer.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, height);
            LayoutElement layout = spacer.AddComponent<LayoutElement>();
            layout.preferredHeight = height;
            layout.flexibleWidth = 1;
        }

        private GameObject CreateCard(Transform parent, string name, Vector2 position, Vector2 size)
        {
            GameObject card = new GameObject(name);
            card.transform.SetParent(parent, false);
            RectTransform rect = card.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image img = card.AddComponent<Image>();
            img.sprite = roundedSprite;
            img.type = Image.Type.Sliced;
            img.color = CardBg;

            return card;
        }

        private GameObject CreateScrollView(Transform parent, string name, bool vertical)
        {
            GameObject scrollView = new GameObject(name);
            scrollView.transform.SetParent(parent, false);
            RectTransform scrollRect = scrollView.AddComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = Vector2.zero;
            scrollRect.offsetMax = Vector2.zero;

            ScrollRect scroll = scrollView.AddComponent<ScrollRect>();
            scroll.horizontal = !vertical;
            scroll.vertical = vertical;
            scroll.scrollSensitivity = 30;

            // Viewport
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollView.transform, false);
            RectTransform viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewport.AddComponent<RectMask2D>();
            scroll.viewport = viewportRect;

            // Content
            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.AddComponent<RectTransform>();

            if (vertical)
            {
                contentRect.anchorMin = new Vector2(0, 1);
                contentRect.anchorMax = new Vector2(1, 1);
                contentRect.pivot = new Vector2(0.5f, 1);

                VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 0;
                layout.padding = new RectOffset(50, 50, 0, 0);
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;
                layout.childControlWidth = true;
                layout.childControlHeight = false;

                ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
                fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }

            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;

            scroll.content = contentRect;

            return scrollView;
        }

        private TextMeshProUGUI CreateLabel(Transform parent, string name, string text, int fontSize, Color color,
            Vector2 position, FontStyles style = FontStyles.Normal, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(800, fontSize + 20);

            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.fontStyle = style;

            return tmp;
        }

        private Button CreateModernButton(Transform parent, string name, string text, Color bgColor, Vector2 position, Vector2 size, UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image img = btnObj.AddComponent<Image>();
            img.sprite = roundedSprite;
            img.type = Image.Type.Sliced;
            img.color = bgColor;

            Button btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(onClick);

            ColorBlock colors = btn.colors;
            colors.normalColor = bgColor;
            colors.highlightedColor = new Color(bgColor.r + 0.1f, bgColor.g + 0.1f, bgColor.b + 0.1f);
            colors.pressedColor = new Color(bgColor.r - 0.1f, bgColor.g - 0.1f, bgColor.b - 0.1f);
            colors.selectedColor = bgColor;
            btn.colors = colors;

            // Button text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 22;
            tmp.color = TextPrimary;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;

            return btn;
        }

        private TMP_InputField CreateModernInput(Transform parent, string name, string placeholder, Vector2 position, Vector2 size)
        {
            GameObject inputObj = new GameObject(name);
            inputObj.transform.SetParent(parent, false);
            RectTransform rect = inputObj.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image img = inputObj.AddComponent<Image>();
            img.sprite = roundedSprite;
            img.type = Image.Type.Sliced;
            img.color = InputBg;

            TMP_InputField input = inputObj.AddComponent<TMP_InputField>();

            // Text area
            GameObject textArea = new GameObject("TextArea");
            textArea.transform.SetParent(inputObj.transform, false);
            RectTransform textAreaRect = textArea.AddComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = new Vector2(20, 8);
            textAreaRect.offsetMax = new Vector2(-20, -8);
            textArea.AddComponent<RectMask2D>();

            // Placeholder
            GameObject phObj = new GameObject("Placeholder");
            phObj.transform.SetParent(textArea.transform, false);
            RectTransform phRect = phObj.AddComponent<RectTransform>();
            phRect.anchorMin = Vector2.zero;
            phRect.anchorMax = Vector2.one;
            phRect.offsetMin = Vector2.zero;
            phRect.offsetMax = Vector2.zero;
            TextMeshProUGUI phText = phObj.AddComponent<TextMeshProUGUI>();
            phText.text = placeholder;
            phText.fontSize = 20;
            phText.color = TextMuted;
            phText.alignment = TextAlignmentOptions.Left;

            // Input text
            GameObject inputTextObj = new GameObject("Text");
            inputTextObj.transform.SetParent(textArea.transform, false);
            RectTransform inputTextRect = inputTextObj.AddComponent<RectTransform>();
            inputTextRect.anchorMin = Vector2.zero;
            inputTextRect.anchorMax = Vector2.one;
            inputTextRect.offsetMin = Vector2.zero;
            inputTextRect.offsetMax = Vector2.zero;
            TextMeshProUGUI inputText = inputTextObj.AddComponent<TextMeshProUGUI>();
            inputText.fontSize = 22;
            inputText.color = TextPrimary;
            inputText.alignment = TextAlignmentOptions.Left;

            input.textViewport = textAreaRect;
            input.textComponent = inputText;
            input.placeholder = phText;
            input.characterLimit = 20;

            return input;
        }

        private Toggle CreateModernToggle(Transform parent, string name, string label, Vector2 position)
        {
            GameObject toggleObj = new GameObject(name);
            toggleObj.transform.SetParent(parent, false);
            RectTransform rect = toggleObj.AddComponent<RectTransform>();
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(150, 30);

            Toggle toggle = toggleObj.AddComponent<Toggle>();

            // Background
            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(toggleObj.transform, false);
            RectTransform bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 0.5f);
            bgRect.anchorMax = new Vector2(0, 0.5f);
            bgRect.anchoredPosition = new Vector2(15, 0);
            bgRect.sizeDelta = new Vector2(24, 24);
            Image bgImg = bg.AddComponent<Image>();
            bgImg.sprite = roundedSprite;
            bgImg.type = Image.Type.Sliced;
            bgImg.color = InputBg;
            toggle.targetGraphic = bgImg;

            // Checkmark
            GameObject check = new GameObject("Checkmark");
            check.transform.SetParent(bg.transform, false);
            RectTransform checkRect = check.AddComponent<RectTransform>();
            checkRect.anchorMin = Vector2.zero;
            checkRect.anchorMax = Vector2.one;
            checkRect.offsetMin = new Vector2(4, 4);
            checkRect.offsetMax = new Vector2(-4, -4);
            Image checkImg = check.AddComponent<Image>();
            checkImg.sprite = roundedSprite;
            checkImg.type = Image.Type.Sliced;
            checkImg.color = AccentGreen;
            toggle.graphic = checkImg;

            // Label
            CreateLabel(toggleObj.transform, "Label", label, 18, TextSecondary, new Vector2(55, 0), FontStyles.Normal, TextAlignmentOptions.Left);

            // Default to OFF (public rooms)
            toggle.isOn = false;

            return toggle;
        }

        private void SetState(LobbyState state)
        {
            currentState = state;
            connectingPanel?.SetActive(state == LobbyState.Connecting);
            mainLobbyPanel?.SetActive(state == LobbyState.MainLobby);
            roomPanel?.SetActive(state == LobbyState.InRoom);
        }

        // Network event handlers
        private void OnConnectionStateChanged(ConnectionState state)
        {
            if (this == null) return;

            Debug.Log($"[LobbyUI] Connection state: {state}");

            switch (state)
            {
                case ConnectionState.Connected:
                    SetState(LobbyState.MainLobby);
                    OnRefreshClicked();
                    break;
                case ConnectionState.InLobby:
                    SetState(LobbyState.InRoom);
                    break;
                case ConnectionState.Disconnected:
                    SetState(LobbyState.Connecting);
                    Invoke(nameof(TryReconnect), 2f);
                    break;
            }
        }

        private void TryReconnect()
        {
            if (NetworkManager.Instance != null && NetworkManager.Instance.State == ConnectionState.Disconnected)
            {
                NetworkManager.Instance.Connect();
            }
        }

        private void OnRoomListReceived(List<GameRoom> rooms)
        {
            Debug.Log($"[LobbyUI] OnRoomListReceived called with {rooms?.Count ?? 0} rooms");
            Debug.Log($"[LobbyUI] roomListContent is {(roomListContent == null ? "NULL" : "valid")}");

            // Always reset loading state first
            isRefreshing = false;
            refreshTimer = 0f;
            if (refreshButtonText != null) refreshButtonText.text = "Refresh";
            if (loadingIndicator != null) loadingIndicator.SetActive(false);

            // Safety check - don't process if we've been destroyed
            if (this == null || roomListContent == null)
            {
                Debug.LogWarning("[LobbyUI] Early return - roomListContent is null!");
                return;
            }

            Debug.Log($"[LobbyUI] Creating UI for {rooms.Count} rooms");

            foreach (var item in roomListItems)
            {
                if (item != null) Destroy(item);
            }
            roomListItems.Clear();

            if (rooms.Count == 0)
            {
                GameObject emptyMsg = new GameObject("EmptyMessage");
                emptyMsg.transform.SetParent(roomListContent, false);
                RectTransform rect = emptyMsg.AddComponent<RectTransform>();
                rect.sizeDelta = new Vector2(0, 280);  // Taller to fill available space
                LayoutElement le = emptyMsg.AddComponent<LayoutElement>();
                le.preferredHeight = 280;
                le.flexibleWidth = 1;

                TextMeshProUGUI text = emptyMsg.AddComponent<TextMeshProUGUI>();
                text.text = "No rooms available\nCreate one to get started!";
                text.fontSize = 20;
                text.color = TextMuted;
                text.alignment = TextAlignmentOptions.Center;
                text.verticalAlignment = VerticalAlignmentOptions.Middle;

                roomListItems.Add(emptyMsg);
            }
            else
            {
                foreach (var room in rooms)
                {
                    CreateRoomListItem(room);
                }
            }
        }

        private void CreateRoomListItem(GameRoom room)
        {
            GameObject item = new GameObject("Room_" + room.RoomId);
            item.transform.SetParent(roomListContent, false);
            RectTransform rect = item.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 85);
            LayoutElement le = item.AddComponent<LayoutElement>();
            le.preferredHeight = 85;
            le.flexibleWidth = 1;

            Image bg = item.AddComponent<Image>();
            bg.sprite = roundedSprite;
            bg.type = Image.Type.Sliced;
            bg.color = CardBgHover;

            Button btn = item.AddComponent<Button>();
            string roomId = room.RoomId;
            btn.onClick.AddListener(() => OnRoomItemClicked(roomId));

            ColorBlock colors = btn.colors;
            colors.highlightedColor = new Color(CardBgHover.r + 0.05f, CardBgHover.g + 0.05f, CardBgHover.b + 0.05f);
            btn.colors = colors;

            // Room name - top left
            GameObject nameObj = new GameObject("Name");
            nameObj.transform.SetParent(item.transform, false);
            RectTransform nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0.5f);
            nameRect.anchorMax = new Vector2(1, 1);
            nameRect.offsetMin = new Vector2(15, 2);
            nameRect.offsetMax = new Vector2(-100, -5);
            TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
            nameText.text = room.RoomName;
            nameText.fontSize = 20;
            nameText.color = TextPrimary;
            nameText.fontStyle = FontStyles.Bold;
            nameText.alignment = TextAlignmentOptions.Left;
            nameText.verticalAlignment = VerticalAlignmentOptions.Middle;
            nameText.overflowMode = TextOverflowModes.Ellipsis;

            // Player names row - bottom left
            string playerNames = "";
            foreach (var p in room.Players)
            {
                if (playerNames.Length > 0) playerNames += ", ";
                playerNames += p.DisplayName ?? "Player";
            }
            if (string.IsNullOrEmpty(playerNames)) playerNames = "No players yet";

            GameObject playersObj = new GameObject("Players");
            playersObj.transform.SetParent(item.transform, false);
            RectTransform playersRect = playersObj.AddComponent<RectTransform>();
            playersRect.anchorMin = new Vector2(0, 0);
            playersRect.anchorMax = new Vector2(1, 0.5f);
            playersRect.offsetMin = new Vector2(15, 5);
            playersRect.offsetMax = new Vector2(-100, -2);
            TextMeshProUGUI playersTmp = playersObj.AddComponent<TextMeshProUGUI>();
            playersTmp.text = playerNames;
            playersTmp.fontSize = 14;
            playersTmp.color = TextSecondary;
            playersTmp.alignment = TextAlignmentOptions.Left;
            playersTmp.verticalAlignment = VerticalAlignmentOptions.Middle;
            playersTmp.overflowMode = TextOverflowModes.Ellipsis;

            // Player count badge - right side
            GameObject badge = new GameObject("Badge");
            badge.transform.SetParent(item.transform, false);
            RectTransform badgeRect = badge.AddComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(1, 0.5f);
            badgeRect.anchorMax = new Vector2(1, 0.5f);
            badgeRect.anchoredPosition = new Vector2(-50, 0);
            badgeRect.sizeDelta = new Vector2(70, 36);
            Image badgeImg = badge.AddComponent<Image>();
            badgeImg.sprite = roundedSprite;
            badgeImg.type = Image.Type.Sliced;
            badgeImg.color = room.Players.Count >= 4 ? AccentRed : AccentGreen;

            CreateLabel(badge.transform, "Count", $"{room.Players.Count}/4", 16, TextPrimary, Vector2.zero, FontStyles.Bold);

            roomListItems.Add(item);
        }

        private void OnLiveGamesReceived(List<LiveGameInfo> games)
        {
            // Safety check - don't process if we've been destroyed
            if (this == null || liveGamesContent == null) return;

            foreach (var item in liveGameItems)
            {
                if (item != null) Destroy(item);
            }
            liveGameItems.Clear();

            // Toggle empty message - EmptyMessage is a child of livePanel (3 levels up from Content)
            if (liveGamesContent?.parent?.parent?.parent != null)
            {
                Transform emptyMsg = liveGamesContent.parent.parent.parent.Find("EmptyMessage");
                if (emptyMsg != null) emptyMsg.gameObject.SetActive(games.Count == 0);
            }

            foreach (var game in games)
            {
                CreateLiveGameItem(game);
            }
        }

        private void CreateLiveGameItem(LiveGameInfo game)
        {
            GameObject item = new GameObject("Live_" + game.RoomId);
            item.transform.SetParent(liveGamesContent, false);
            RectTransform rect = item.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 60);
            LayoutElement le = item.AddComponent<LayoutElement>();
            le.preferredHeight = 60;
            le.flexibleWidth = 1;

            Image bg = item.AddComponent<Image>();
            bg.sprite = roundedSprite;
            bg.type = Image.Type.Sliced;
            bg.color = CardBgHover;

            Button btn = item.AddComponent<Button>();
            string roomId = game.RoomId;
            btn.onClick.AddListener(() => OnSpectateClicked(roomId));

            // Live indicator (pulsing red dot)
            GameObject liveIndicator = new GameObject("Live");
            liveIndicator.transform.SetParent(item.transform, false);
            RectTransform liveRect = liveIndicator.AddComponent<RectTransform>();
            liveRect.anchorMin = new Vector2(0, 0.5f);
            liveRect.anchorMax = new Vector2(0, 0.5f);
            liveRect.anchoredPosition = new Vector2(20, 0);
            liveRect.sizeDelta = new Vector2(10, 10);
            Image liveImg = liveIndicator.AddComponent<Image>();
            liveImg.sprite = circleSprite;
            liveImg.color = AccentRed;

            // Room name
            TextMeshProUGUI nameLabel = CreateLabel(item.transform, "Name", game.RoomName, 16, TextPrimary, new Vector2(40, 0), FontStyles.Bold, TextAlignmentOptions.Left);
            RectTransform nameRect = nameLabel.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0.5f);
            nameRect.anchorMax = new Vector2(0, 0.5f);

            // Watch text on right
            TextMeshProUGUI watchLabel = CreateLabel(item.transform, "Watch", "Watch →", 14, AccentMagenta, new Vector2(-15, 0), FontStyles.Normal, TextAlignmentOptions.Right);
            RectTransform watchRect = watchLabel.GetComponent<RectTransform>();
            watchRect.anchorMin = new Vector2(1, 0.5f);
            watchRect.anchorMax = new Vector2(1, 0.5f);

            liveGameItems.Add(item);
        }

        private void OnSpectateClicked(string roomId)
        {
            NetworkManager.Instance?.SpectateRoom(roomId);
        }

        private void OnRoomJoined(GameRoom room)
        {
            if (this == null) return;

            if (roomNameText != null) roomNameText.text = room.RoomName;
            if (roomCodeText != null)
            {
                roomCodeText.text = "Public Room";
                roomCodeText.color = TextSecondary;
            }

            UpdatePlayerGrid(room);
            UpdateOnlinePlayersBar(room);
            UpdateRoomButtons(room);
            SetState(LobbyState.InRoom);
        }

        private void OnRoomUpdated(GameRoom room)
        {
            if (this == null) return;
            if (currentState != LobbyState.InRoom) return;

            Debug.Log($"[LobbyUI] Room updated, players: {room.Players.Count}");
            UpdatePlayerGrid(room);
            UpdateOnlinePlayersBar(room);
            UpdateRoomButtons(room);
        }

        private void UpdatePlayerGrid(GameRoom room)
        {
            if (playerListContent == null) return;

            // Position mapping: slot index -> PlayerPosition name
            // 0 = North, 1 = East, 2 = West, 3 = South
            string[] positionNames = { "North", "East", "West", "South" };

            for (int i = 0; i < 4; i++)
            {
                // Find slot - check both team cards
                Transform slot = playerListContent.Find($"TeamNS/Slot_{i}");
                if (slot == null)
                    slot = playerListContent.Find($"TeamEW/Slot_{i}");
                if (slot == null) continue;

                // Find player for this position (by assigned position)
                NetworkPlayer player = null;
                foreach (var p in room.Players)
                {
                    if (p.AssignedPosition == positionNames[i])
                    {
                        player = p;
                        break;
                    }
                }

                UpdateSlot(slot, player, i);
            }
        }

        private void UpdateSlot(Transform slot, NetworkPlayer player, int slotIndex)
        {
            Transform avatarTransform = slot.Find("Avatar");
            Transform avatarInner = avatarTransform?.Find("AvatarInner");
            Transform initialTransform = avatarInner?.Find("Initial");
            Transform nameTransform = slot.Find("PlayerName");
            Transform badgeTransform = slot.Find("StatusBadge");
            Transform posIndicator = slot.Find("PosIndicator");

            bool hasPlayer = player != null;
            bool isLocalPlayer = hasPlayer && player.PlayerId == NetworkManager.Instance?.LocalPlayerId;
            string localPlayerPosition = NetworkManager.Instance?.LocalPlayer?.AssignedPosition;

            // Update slot button interactability
            // Clickable when: empty OR when local player wants to move (any empty slot)
            Button slotButton = slot.GetComponent<Button>();
            if (slotButton != null)
            {
                // Allow clicking on empty slots to move there
                slotButton.interactable = !hasPlayer || isLocalPlayer;
            }

            // Update position indicator color based on occupancy
            if (posIndicator != null)
            {
                Image posIndImg = posIndicator.GetComponent<Image>();
                if (posIndImg != null)
                {
                    if (hasPlayer)
                    {
                        posIndImg.color = player.IsReady ? AccentGreen : AccentCyan;
                    }
                    // Empty slots keep their team color (set during creation)
                }
            }

            // Update avatar ring
            if (avatarTransform != null)
            {
                Image avatarRing = avatarTransform.GetComponent<Image>();
                if (avatarRing != null)
                {
                    if (hasPlayer)
                    {
                        avatarRing.color = player.IsReady ? AccentGreen : AccentCyan;
                    }
                    // Empty slots keep their team color (set during creation)
                }
            }

            // Update avatar inner background
            if (avatarInner != null)
            {
                Image avatarInnerBg = avatarInner.GetComponent<Image>();
                if (avatarInnerBg != null)
                {
                    if (hasPlayer)
                    {
                        avatarInnerBg.color = player.IsReady ? AccentGreen : AccentCyan;
                    }
                    else
                    {
                        avatarInnerBg.color = new Color(0.06f, 0.07f, 0.09f, 1f);
                    }
                }
            }

            // Update initial
            if (initialTransform != null)
            {
                TextMeshProUGUI initialText = initialTransform.GetComponent<TextMeshProUGUI>();
                if (initialText != null)
                {
                    if (hasPlayer && !string.IsNullOrEmpty(player.DisplayName))
                    {
                        initialText.text = player.DisplayName[0].ToString().ToUpper();
                        initialText.color = BgDark;
                        initialText.fontSize = 26;
                    }
                    else
                    {
                        initialText.text = "+";
                        initialText.fontSize = 28;
                        // Keep team color from creation
                    }
                }
            }

            // Update name
            if (nameTransform != null)
            {
                TextMeshProUGUI nameText = nameTransform.GetComponent<TextMeshProUGUI>();
                if (nameText != null)
                {
                    if (hasPlayer)
                    {
                        string displayName = player.DisplayName;
                        if (isLocalPlayer) displayName += " (You)";
                        if (player.IsHost) displayName = "[HOST] " + displayName;
                        nameText.text = displayName;
                        nameText.color = isLocalPlayer ? AccentCyan : TextPrimary;
                        nameText.fontStyle = isLocalPlayer ? FontStyles.Bold : FontStyles.Normal;
                    }
                    else
                    {
                        nameText.text = "Click to join";
                        nameText.color = TextMuted;
                        nameText.fontStyle = FontStyles.Italic;
                    }
                }
            }

            // Update badge
            if (badgeTransform != null)
            {
                Image badgeBg = badgeTransform.GetComponent<Image>();
                Transform badgeTextTransform = badgeTransform.Find("Text");
                TextMeshProUGUI badgeText = badgeTextTransform?.GetComponent<TextMeshProUGUI>();

                if (hasPlayer)
                {
                    if (player.IsReady)
                    {
                        if (badgeBg != null) badgeBg.color = AccentGreen;
                        if (badgeText != null)
                        {
                            badgeText.text = "READY";
                            badgeText.color = BgDark;
                        }
                    }
                    else
                    {
                        if (badgeBg != null) badgeBg.color = new Color(0.8f, 0.3f, 0.3f, 0.9f);
                        if (badgeText != null)
                        {
                            badgeText.text = "WAITING";
                            badgeText.color = TextPrimary;
                        }
                    }
                }
                else
                {
                    if (badgeBg != null) badgeBg.color = new Color(0.15f, 0.16f, 0.19f, 1f);
                    if (badgeText != null)
                    {
                        badgeText.text = "OPEN";
                        badgeText.color = TextMuted;
                    }
                }
            }

            // Update slot background
            Image slotBg = slot.GetComponent<Image>();
            if (slotBg != null)
            {
                if (hasPlayer)
                {
                    if (isLocalPlayer)
                    {
                        slotBg.color = new Color(0.12f, 0.13f, 0.16f, 1f);  // Slightly highlighted for local player
                    }
                    else
                    {
                        slotBg.color = new Color(0.10f, 0.11f, 0.14f, 1f);
                    }
                }
                else
                {
                    slotBg.color = new Color(0.08f, 0.09f, 0.11f, 0.9f);
                }
            }
        }

        private void UpdateOnlinePlayersBar(GameRoom room)
        {
            // Update player count text
            if (playerCountText != null)
            {
                int count = room.Players.Count;
                playerCountText.text = $"{count}/4 Online";
                playerCountText.color = count >= 4 ? AccentGreen : AccentCyan;
            }

            // Rebuild avatars row
            if (onlinePlayersBar != null)
            {
                // Clear existing avatars
                for (int i = onlinePlayersBar.childCount - 1; i >= 0; i--)
                {
                    Destroy(onlinePlayersBar.GetChild(i).gameObject);
                }

                // Create avatar chip for each connected player
                foreach (var player in room.Players)
                {
                    CreatePlayerChip(onlinePlayersBar, player);
                }
            }
        }

        private void CreatePlayerChip(Transform parent, NetworkPlayer player)
        {
            bool isLocal = player.PlayerId == NetworkManager.Instance?.LocalPlayerId;

            GameObject chip = new GameObject("Chip_" + player.DisplayName);
            chip.transform.SetParent(parent, false);

            RectTransform chipRect = chip.AddComponent<RectTransform>();
            chipRect.sizeDelta = new Vector2(130, 36);

            // Chip background
            Image chipBg = chip.AddComponent<Image>();
            chipBg.sprite = roundedSprite;
            chipBg.type = Image.Type.Sliced;
            chipBg.color = isLocal
                ? new Color(AccentCyan.r, AccentCyan.g, AccentCyan.b, 0.2f)
                : new Color(0.15f, 0.17f, 0.22f, 0.9f);

            LayoutElement le = chip.AddComponent<LayoutElement>();
            le.preferredWidth = 130;
            le.preferredHeight = 36;

            // Avatar circle (left side of chip)
            GameObject avatar = new GameObject("Avatar");
            avatar.transform.SetParent(chip.transform, false);
            RectTransform avatarRect = avatar.AddComponent<RectTransform>();
            avatarRect.anchorMin = new Vector2(0, 0.5f);
            avatarRect.anchorMax = new Vector2(0, 0.5f);
            avatarRect.anchoredPosition = new Vector2(18, 0);
            avatarRect.sizeDelta = new Vector2(26, 26);

            Image avatarBg = avatar.AddComponent<Image>();
            avatarBg.sprite = circleSprite;
            Color chipColor = player.IsReady ? AccentGreen : AccentCyan;
            avatarBg.color = chipColor;

            // Avatar initial
            GameObject initialObj = new GameObject("Initial");
            initialObj.transform.SetParent(avatar.transform, false);
            RectTransform initRect = initialObj.AddComponent<RectTransform>();
            initRect.anchorMin = Vector2.zero;
            initRect.anchorMax = Vector2.one;
            initRect.offsetMin = Vector2.zero;
            initRect.offsetMax = Vector2.zero;
            TextMeshProUGUI initText = initialObj.AddComponent<TextMeshProUGUI>();
            string initial = !string.IsNullOrEmpty(player.DisplayName) ? player.DisplayName[0].ToString().ToUpper() : "?";
            initText.text = initial;
            initText.fontSize = 14;
            initText.color = BgDark;
            initText.alignment = TextAlignmentOptions.Center;
            initText.fontStyle = FontStyles.Bold;

            // Player name (right of avatar)
            GameObject nameObj = new GameObject("Name");
            nameObj.transform.SetParent(chip.transform, false);
            RectTransform nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0);
            nameRect.anchorMax = new Vector2(1, 1);
            nameRect.offsetMin = new Vector2(35, 0);
            nameRect.offsetMax = new Vector2(-5, 0);
            TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
            string displayName = player.DisplayName ?? "Player";
            if (isLocal) displayName += " (You)";
            nameText.text = displayName;
            nameText.fontSize = 13;
            nameText.color = isLocal ? AccentCyan : TextPrimary;
            nameText.alignment = TextAlignmentOptions.Left;
            nameText.verticalAlignment = VerticalAlignmentOptions.Middle;
            nameText.fontStyle = isLocal ? FontStyles.Bold : FontStyles.Normal;
            nameText.overflowMode = TextOverflowModes.Ellipsis;

            // Ready indicator dot (right edge)
            if (player.IsReady)
            {
                GameObject dot = new GameObject("ReadyDot");
                dot.transform.SetParent(chip.transform, false);
                RectTransform dotRect = dot.AddComponent<RectTransform>();
                dotRect.anchorMin = new Vector2(1, 0.5f);
                dotRect.anchorMax = new Vector2(1, 0.5f);
                dotRect.anchoredPosition = new Vector2(-8, 0);
                dotRect.sizeDelta = new Vector2(8, 8);
                Image dotImg = dot.AddComponent<Image>();
                dotImg.sprite = circleSprite;
                dotImg.color = AccentGreen;
            }
        }

        private void UpdateRoomButtons(GameRoom room)
        {
            bool isHost = NetworkManager.Instance?.LocalPlayer?.IsHost ?? false;
            bool isReady = NetworkManager.Instance?.LocalPlayer?.IsReady ?? false;
            bool canStart = room.CanStart && isHost;

            if (readyButtonText != null)
            {
                readyButtonText.text = isReady ? "Not Ready" : "Ready";
            }

            if (readyButton != null)
            {
                var img = readyButton.GetComponent<Image>();
                Color readyColor = isReady ? AccentRed : AccentGreen;
                if (img != null) img.color = readyColor;

                ColorBlock colors = readyButton.colors;
                colors.normalColor = readyColor;
                colors.highlightedColor = new Color(readyColor.r + 0.1f, readyColor.g + 0.1f, readyColor.b + 0.1f);
                colors.pressedColor = new Color(readyColor.r - 0.1f, readyColor.g - 0.1f, readyColor.b - 0.1f);
                readyButton.colors = colors;
            }

            if (startButton != null)
            {
                startButton.gameObject.SetActive(isHost);
                startButton.interactable = canStart;

                var img = startButton.GetComponent<Image>();
                Color startColor = canStart ? AccentCyan : TextMuted;
                if (img != null) img.color = startColor;

                ColorBlock colors = startButton.colors;
                colors.normalColor = startColor;
                colors.disabledColor = TextMuted;
                startButton.colors = colors;
            }

        }


        private void OnPlayerJoined(NetworkPlayer player)
        {
            if (this == null) return;
            if (NetworkManager.Instance?.CurrentRoom != null)
                UpdatePlayerGrid(NetworkManager.Instance.CurrentRoom);
        }

        private void OnPlayerLeft(NetworkPlayer player)
        {
            if (this == null) return;
            if (NetworkManager.Instance?.CurrentRoom != null)
                UpdatePlayerGrid(NetworkManager.Instance.CurrentRoom);
        }

        private void OnGameStarted(string roomId)
        {
            if (this == null) return;
            OnGameStarting?.Invoke();
            Hide();
        }

        private void OnNetworkError(string error)
        {
            if (this == null) return;
            Debug.LogError($"[LobbyUI] Error: {error}");
        }

        // Button handlers
        private void OnRefreshClicked()
        {
            if (isRefreshing) return;

            isRefreshing = true;
            refreshTimer = 0f;

            // Show loading state
            if (refreshButtonText != null) refreshButtonText.text = "...";
            if (loadingIndicator != null) loadingIndicator.SetActive(true);

            Debug.Log("[LobbyUI] Requesting room list...");
            NetworkManager.Instance?.RequestRoomList();
            NetworkManager.Instance?.RequestLiveGames();
        }

        private void OnCreateRoomClicked()
        {
            string roomName = newRoomNameInput?.text?.Trim();
            if (string.IsNullOrEmpty(roomName))
            {
                roomName = GenerateDefaultRoomName();
            }

            NetworkManager.Instance?.CreateRoom(roomName, false); // Always public
        }

        private string GenerateDefaultRoomName()
        {
            string playerName = PlayerProfileManager.Instance?.CurrentProfile?.DisplayName ?? "Player";

            // Array of room name formats for variety
            string[] formats = {
                "{0}'s Room",
                "{0}'s Game",
                "{0}'s Table",
                "Play with {0}",
                "{0}'s Lekha"
            };

            // Pick a random format
            string format = formats[UnityEngine.Random.Range(0, formats.Length)];
            return string.Format(format, playerName);
        }

        private void OnRoomItemClicked(string roomId)
        {
            NetworkManager.Instance?.JoinRoom(roomId);
        }

        private void OnReadyClicked()
        {
            bool currentReady = NetworkManager.Instance?.LocalPlayer?.IsReady ?? false;
            Debug.Log($"[LobbyUI] Ready clicked. Current: {currentReady}, Sending: {!currentReady}");
            Debug.Log($"[LobbyUI] CurrentRoom: {NetworkManager.Instance?.CurrentRoom?.RoomName ?? "null"}");
            NetworkManager.Instance?.SetReady(!currentReady);
        }

        private void OnStartClicked()
        {
            NetworkManager.Instance?.StartGame();
        }

        private void OnLeaveRoomClicked()
        {
            NetworkManager.Instance?.LeaveRoom();
            SetState(LobbyState.MainLobby);
            OnRefreshClicked();
        }


        private void OnBackButtonClicked()
        {
            NetworkManager.Instance?.Disconnect();
            OnBackClicked?.Invoke();
            Hide();
        }

        public void Show()
        {
            rootPanel?.SetActive(true);
            if (NetworkManager.Instance != null && NetworkManager.Instance.State == ConnectionState.Disconnected)
            {
                SetState(LobbyState.Connecting);
                NetworkManager.Instance.Connect();
            }
        }

        public void Hide()
        {
            rootPanel?.SetActive(false);
        }
    }
}
