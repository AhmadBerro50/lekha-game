using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;
using Lekha.Core;
using Lekha.GameLogic;
using Lekha.AI;
using Lekha.Animation;
using Lekha.Audio;
using Lekha.Effects;
using Lekha.Network;

namespace Lekha.UI
{
    /// <summary>
    /// Main game UI controller - manages all visual elements
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        public static GameUI Instance { get; private set; }

        [Header("UI Containers")]
        private RectTransform playerHandContainer;
        private RectTransform leftHandContainer;
        private RectTransform topHandContainer;
        private RectTransform rightHandContainer;
        private RectTransform trickArea;
        private RectTransform deckPosition;

        [Header("UI Elements")]
        private TextMeshProUGUI instructionText;
        private TextMeshProUGUI scoreText;
        private TextMeshProUGUI roundText;
        private Button startButton;
        private Button passButton;
        private Button pauseButton;
        private Button scoreButton;
        private Image turnIndicator;
        private RectTransform[] playerIndicators = new RectTransform[4];

        [Header("Player Info Panels")]
        private Dictionary<PlayerPosition, PlayerInfoPanel> playerInfoPanels = new Dictionary<PlayerPosition, PlayerInfoPanel>();
        private ScoreSummaryPopup scoreSummaryPopup;

        [Header("Card Settings")]
        private float cardWidth = 180f; // Smaller cards for Jawaker-style
        private float cardHeight = 260f;
        private float cardSpacing = 45f; // Tight overlap like Jawaker

        [Header("State")]
        private List<CardUI> playerHandCards = new List<CardUI>();
        private List<CardUI> trickCards = new List<CardUI>();
        private List<CardUI> selectedForPass = new List<CardUI>();
        private bool isPassPhase = false;
        private bool isAnimating = false; // Block during deal/pass animations
        private bool isProcessingPlay = false; // Prevent double-plays
        private Canvas mainCanvas;
        private Transform canvasTransform;
        private Coroutine pendingAIPlayCoroutine = null;
        private float lastActionTime = 0f; // For watchdog recovery
        private Coroutine watchdogCoroutine = null;

        // NUCLEAR ANIMATION LOCK - absolutely no card play until this time passes
        private float nextAllowedPlayTime = 0f;
        private const float CARD_PLAY_COOLDOWN = 0.4f; // Minimum time between any card plays
        private const float TRICK_COMPLETE_COOLDOWN = 1.0f; // Time to wait after trick completes before next play

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
            CreateUI();
            SubscribeToEvents();
            // Start the watchdog to recover from stuck states
            watchdogCoroutine = StartCoroutine(WatchdogCoroutine());
        }

        /// <summary>
        /// Watchdog coroutine that periodically checks for stuck game state and recovers
        /// </summary>
        private System.Collections.IEnumerator WatchdogCoroutine()
        {
            const float checkInterval = 2f;
            const float stuckThreshold = 5f; // If no action for 5 seconds, try to recover

            while (true)
            {
                yield return new WaitForSeconds(checkInterval);

                // Only check if we're supposed to be playing
                if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.PlayingTricks)
                {
                    lastActionTime = Time.time; // Reset timer when not playing
                    continue;
                }

                // Don't interfere if cooldown is active
                if (!CanPlayNow())
                {
                    Debug.Log($"[Watchdog] Cooldown active ({GetRemainingCooldown():F2}s), skipping check");
                    continue;
                }

                // Check if we're stuck
                float timeSinceLastAction = Time.time - lastActionTime;
                if (timeSinceLastAction > stuckThreshold)
                {
                    Debug.LogWarning($"[Watchdog] Game appears stuck for {timeSinceLastAction:F1}s, attempting recovery");

                    // Reset blocking flags
                    isProcessingPlay = false;
                    nextAllowedPlayTime = 0; // Clear the nuclear lock

                    Player currentPlayer = GameManager.Instance.CurrentPlayer;
                    // In online games, only act if it's local player's turn
                    bool isLocalTurn = GameManager.Instance.IsLocalPlayerTurn();
                    if (isLocalTurn)
                    {
                        Debug.Log("[Watchdog] Highlighting cards for local player");
                        HighlightPlayableCards();
                    }
                    else if (!GameManager.Instance.IsOnlineGame && !currentPlayer.IsHuman)
                    {
                        // Only trigger AI in local games for non-human players
                        Debug.Log($"[Watchdog] Triggering AI play for {currentPlayer.PlayerName}");
                        AIPlayCard();
                    }
                    else
                    {
                        Debug.Log($"[Watchdog] Waiting for remote player {currentPlayer.PlayerName}");
                    }

                    lastActionTime = Time.time;
                }
            }
        }

        /// <summary>
        /// Unsubscribe from all events and destroy the GameCanvas.
        /// GameUI persists (DontDestroyOnLoad) but is reset for next game.
        /// Call Reinitialize() to set up for a new game.
        /// </summary>
        public void Cleanup()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnCardsDealt -= OnCardsDealt;
                GameManager.Instance.OnCardPlayed -= OnCardPlayed;
                GameManager.Instance.OnTrickWon -= OnTrickWon;
                GameManager.Instance.OnRoundEnded -= OnRoundEnded;
                GameManager.Instance.OnGameOver -= OnGameOver;
                GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
                GameManager.Instance.OnPassPhaseComplete -= OnPassPhaseComplete;
                GameManager.Instance.OnTrickStarted -= OnTrickStarted;
            }

            // Destroy the canvas and null out references
            if (mainCanvas != null)
            {
                Destroy(mainCanvas.gameObject);
                mainCanvas = null;
                canvasTransform = null;
            }
            playerInfoPanels.Clear();
        }

        /// <summary>
        /// Reinitialize GameUI for a new game (recreates canvas, re-subscribes events).
        /// Safe to call even if already initialized — cleans up first.
        /// </summary>
        public void Reinitialize()
        {
            Cleanup();
            CreateUI();
            SubscribeToEvents();
            if (watchdogCoroutine != null) StopCoroutine(watchdogCoroutine);
            watchdogCoroutine = StartCoroutine(WatchdogCoroutine());
        }

        private void OnDestroy()
        {
            if (watchdogCoroutine != null)
            {
                StopCoroutine(watchdogCoroutine);
            }
            Cleanup();
        }

        /// <summary>
        /// Call this whenever a game action occurs to reset the watchdog timer
        /// </summary>
        private void ResetWatchdogTimer()
        {
            lastActionTime = Time.time;
        }

        /// <summary>
        /// NUCLEAR LOCK: Check if any play is allowed right now
        /// </summary>
        private bool CanPlayNow()
        {
            return Time.time >= nextAllowedPlayTime && !isProcessingPlay;
        }

        /// <summary>
        /// NUCLEAR LOCK: Set cooldown after a card is played
        /// </summary>
        private void SetPlayCooldown(float duration)
        {
            nextAllowedPlayTime = Time.time + duration;
            Debug.Log($"[AnimationLock] Play locked until {nextAllowedPlayTime:F2} (in {duration:F2}s)");
        }

        /// <summary>
        /// Get remaining cooldown time
        /// </summary>
        private float GetRemainingCooldown()
        {
            return Mathf.Max(0, nextAllowedPlayTime - Time.time);
        }

        private void CreateUI()
        {
            // Create EventSystem (required for UI interaction)
            if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            // Create Canvas
            GameObject canvasObj = new GameObject("GameCanvas");
            mainCanvas = canvasObj.AddComponent<Canvas>();
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();
            canvasTransform = canvasObj.transform;

            // Create background with gradient feel
            CreateBackground(canvasTransform);

            // Create deck position (for dealing animation origin)
            deckPosition = CreateContainer(canvasTransform, "DeckPosition",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 100));

            // Jawaker-style: Position hand containers around the oval table
            // South (human) - bottom edge of table
            playerHandContainer = CreateContainer(canvasTransform, "PlayerHand",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 10));

            // Create trick area (center of table)
            trickArea = CreateContainer(canvasTransform, "TrickArea",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);

            // West - left edge of table
            leftHandContainer = CreateContainer(canvasTransform, "LeftHand",
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(280, 0));

            // North (partner) - top edge of table
            topHandContainer = CreateContainer(canvasTransform, "TopHand",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -120));

            // East - right edge of table
            rightHandContainer = CreateContainer(canvasTransform, "RightHand",
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-280, 0));

            // Create turn indicators for each position
            CreateTurnIndicators(canvasTransform);

            // Create instruction text - centered above play area
            instructionText = CreateInstructionText(canvasTransform);

            // Create score panel at top center
            CreateScorePanel(canvasTransform);

            // Create round text - top left corner
            roundText = CreateRoundText(canvasTransform);

            // Create buttons with better styling
            startButton = CreateStyledButton(canvasTransform, "StartButton",
                new Vector2(0.5f, 0.5f), "Start Game", OnStartClicked);
            startButton.gameObject.SetActive(false); // Hidden - MainMenu handles starting

            passButton = CreateStyledButton(canvasTransform, "PassButton",
                new Vector2(0.5f, 0.25f), "Pass Cards (0/3)", OnPassClicked);
            passButton.gameObject.SetActive(false);

            // Create pause button (top right corner)
            pauseButton = CreatePauseButton(canvasTransform);

            // Create score summary button (next to pause)
            scoreButton = CreateScoreButton(canvasTransform);

            // Create score summary popup
            scoreSummaryPopup = ScoreSummaryPopup.Create(canvasTransform);

            Debug.Log("GameUI created successfully with modern animations");
        }

        /// <summary>
        /// Create player info panels after game starts
        /// </summary>
        private void CreatePlayerInfoPanels()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogWarning("[CreatePlayerInfoPanels] GameManager.Instance is null, cannot create panels");
                return;
            }

            if (GameManager.Instance.Players == null || GameManager.Instance.Players.Length == 0)
            {
                Debug.LogWarning("[CreatePlayerInfoPanels] No players yet, cannot create panels");
                return;
            }

            // Check if any player is null (not initialized)
            if (GameManager.Instance.Players[0] == null)
            {
                Debug.LogWarning("[CreatePlayerInfoPanels] Players not initialized yet, cannot create panels");
                return;
            }

            // Clear existing panels
            foreach (var panel in playerInfoPanels.Values)
            {
                if (panel != null)
                    Destroy(panel.gameObject);
            }
            playerInfoPanels.Clear();

            // Create panel for each player
            foreach (var player in GameManager.Instance.Players)
            {
                if (player != null)
                {
                    var panel = PlayerInfoPanel.Create(canvasTransform, player, player.Position);
                    playerInfoPanels[player.Position] = panel;
                    Debug.Log($"[CreatePlayerInfoPanels] Created panel for {player.PlayerName} at {player.Position}");
                }
            }

            Debug.Log($"[CreatePlayerInfoPanels] Created {playerInfoPanels.Count} player info panels");
        }

        /// <summary>
        /// Refresh all player panel display names (call after network player names are set)
        /// </summary>
        public void RefreshPlayerPanelNames()
        {
            foreach (var kvp in playerInfoPanels)
            {
                kvp.Value?.RefreshDisplayName();
            }
            Debug.Log("[GameUI] Refreshed player panel display names");
        }

        private Button CreateScoreButton(Transform parent)
        {
            GameObject btnObj = new GameObject("ScoreButton");
            btnObj.transform.SetParent(parent, false);

            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.anchoredPosition = new Vector2(-120, -135); // Moved below score panel
            rect.sizeDelta = new Vector2(52, 52);

            Image img = btnObj.AddComponent<Image>();

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;

            // Casino-style button with team color
            if (ModernUITheme.Instance != null && ModernUITheme.Instance.CircleSprite != null)
            {
                img.sprite = ModernUITheme.Instance.CircleSprite;
                img.color = ModernUITheme.TeamNorthSouth;

                ColorBlock colors = btn.colors;
                colors.normalColor = ModernUITheme.TeamNorthSouth;
                colors.highlightedColor = new Color(0.45f, 0.75f, 0.95f, 1f);
                colors.pressedColor = new Color(0.25f, 0.50f, 0.70f, 1f);
                colors.selectedColor = ModernUITheme.TeamNorthSouth;
                btn.colors = colors;

                Shadow shadow = btnObj.AddComponent<Shadow>();
                shadow.effectColor = new Color(0, 0, 0, 0.5f);
                shadow.effectDistance = new Vector2(2, -2);
            }
            else
            {
                img.color = ModernUITheme.TeamNorthSouth;
            }

            btn.onClick.AddListener(() => {
                SoundManager.Instance?.PlayButtonClick();
                if (scoreSummaryPopup != null)
                {
                    scoreSummaryPopup.Toggle();
                }
            });

            // Score icon
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(btnObj.transform, false);

            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI iconTmp = iconObj.AddComponent<TextMeshProUGUI>();
            iconTmp.text = "i";
            iconTmp.fontSize = 26;
            iconTmp.alignment = TextAlignmentOptions.Center;
            iconTmp.color = Color.white;
            iconTmp.fontStyle = FontStyles.Bold | FontStyles.Italic;

            return btn;
        }

        private Button CreatePauseButton(Transform parent)
        {
            GameObject btnObj = new GameObject("PauseButton");
            btnObj.transform.SetParent(parent, false);

            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(1, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.anchoredPosition = new Vector2(-55, -135); // Moved below score panel
            rect.sizeDelta = new Vector2(52, 52);

            Image img = btnObj.AddComponent<Image>();

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;

            // Casino-style gold button
            if (ModernUITheme.Instance != null && ModernUITheme.Instance.CircleSprite != null)
            {
                img.sprite = ModernUITheme.Instance.CircleSprite;
                img.color = ModernUITheme.GoldAccent;

                ColorBlock colors = btn.colors;
                colors.normalColor = ModernUITheme.GoldAccent;
                colors.highlightedColor = ModernUITheme.GoldBright;
                colors.pressedColor = ModernUITheme.GoldDark;
                colors.selectedColor = ModernUITheme.GoldAccent;
                btn.colors = colors;

                Shadow shadow = btnObj.AddComponent<Shadow>();
                shadow.effectColor = new Color(0, 0, 0, 0.5f);
                shadow.effectDistance = new Vector2(2, -2);
            }
            else
            {
                img.color = ModernUITheme.GoldAccent;
            }

            btn.onClick.AddListener(() => {
                SoundManager.Instance?.PlayButtonClick();
                if (PauseMenu.Instance != null)
                {
                    PauseMenu.Instance.TogglePause();
                }
            });

            // Pause icon
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(btnObj.transform, false);

            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI iconTmp = iconObj.AddComponent<TextMeshProUGUI>();
            iconTmp.text = "||";
            iconTmp.fontSize = 22;
            iconTmp.alignment = TextAlignmentOptions.Center;
            iconTmp.color = ModernUITheme.PrimaryDark;
            iconTmp.fontStyle = FontStyles.Bold;

            return btn;
        }

        private void CreateBackground(Transform parent)
        {
            // Jawaker-style: Dark outer background with oval green felt table

            // 1. Dark outer background (covers entire screen)
            GameObject outerBg = new GameObject("OuterBackground");
            outerBg.transform.SetParent(parent, false);
            Image outerImg = outerBg.AddComponent<Image>();
            outerImg.color = new Color(0.08f, 0.12f, 0.08f, 1f); // Dark greenish-black
            RectTransform outerRect = outerBg.GetComponent<RectTransform>();
            outerRect.anchorMin = Vector2.zero;
            outerRect.anchorMax = Vector2.one;
            outerRect.sizeDelta = Vector2.zero;
            outerImg.raycastTarget = false;

            // 2. Create oval table in center
            CreateOvalTable(parent);
        }

        /// <summary>
        /// Create Jawaker-style oval green felt table
        /// </summary>
        private void CreateOvalTable(Transform parent)
        {
            // Table dimensions (oval shape in center)
            float tableWidth = 1400f;
            float tableHeight = 750f;

            // Create table shadow (slightly larger, darker)
            GameObject shadowObj = new GameObject("TableShadow");
            shadowObj.transform.SetParent(parent, false);
            RectTransform shadowRect = shadowObj.AddComponent<RectTransform>();
            shadowRect.anchorMin = new Vector2(0.5f, 0.5f);
            shadowRect.anchorMax = new Vector2(0.5f, 0.5f);
            shadowRect.sizeDelta = new Vector2(tableWidth + 30, tableHeight + 30);
            shadowRect.anchoredPosition = new Vector2(5, -5);

            Image shadowImg = shadowObj.AddComponent<Image>();
            shadowImg.sprite = CreateOvalSprite(256, 160);
            shadowImg.color = new Color(0, 0, 0, 0.5f);
            shadowImg.raycastTarget = false;

            // Create table border (dark green rim)
            GameObject borderObj = new GameObject("TableBorder");
            borderObj.transform.SetParent(parent, false);
            RectTransform borderRect = borderObj.AddComponent<RectTransform>();
            borderRect.anchorMin = new Vector2(0.5f, 0.5f);
            borderRect.anchorMax = new Vector2(0.5f, 0.5f);
            borderRect.sizeDelta = new Vector2(tableWidth + 20, tableHeight + 20);
            borderRect.anchoredPosition = Vector2.zero;

            Image borderImg = borderObj.AddComponent<Image>();
            borderImg.sprite = CreateOvalSprite(256, 160);
            borderImg.color = new Color(0.15f, 0.25f, 0.15f, 1f); // Dark green border
            borderImg.raycastTarget = false;

            // Create main table surface (green felt)
            GameObject tableObj = new GameObject("TableSurface");
            tableObj.transform.SetParent(parent, false);
            RectTransform tableRect = tableObj.AddComponent<RectTransform>();
            tableRect.anchorMin = new Vector2(0.5f, 0.5f);
            tableRect.anchorMax = new Vector2(0.5f, 0.5f);
            tableRect.sizeDelta = new Vector2(tableWidth, tableHeight);
            tableRect.anchoredPosition = Vector2.zero;

            Image tableImg = tableObj.AddComponent<Image>();
            tableImg.sprite = CreateOvalSprite(256, 160);
            tableImg.color = new Color(0.18f, 0.45f, 0.25f, 1f); // Jawaker green felt
            tableImg.raycastTarget = false;

            // Inner lighter highlight (subtle)
            GameObject highlightObj = new GameObject("TableHighlight");
            highlightObj.transform.SetParent(parent, false);
            RectTransform highlightRect = highlightObj.AddComponent<RectTransform>();
            highlightRect.anchorMin = new Vector2(0.5f, 0.5f);
            highlightRect.anchorMax = new Vector2(0.5f, 0.5f);
            highlightRect.sizeDelta = new Vector2(tableWidth - 100, tableHeight - 60);
            highlightRect.anchoredPosition = Vector2.zero;

            Image highlightImg = highlightObj.AddComponent<Image>();
            highlightImg.sprite = CreateOvalSprite(256, 160);
            highlightImg.color = new Color(0.22f, 0.52f, 0.30f, 0.4f); // Lighter center
            highlightImg.raycastTarget = false;
        }

        /// <summary>
        /// Create an oval/ellipse sprite programmatically
        /// </summary>
        private Sprite CreateOvalSprite(int width, int height)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            float cx = width / 2f;
            float cy = height / 2f;
            float rx = width / 2f;
            float ry = height / 2f;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Ellipse equation: (x-cx)^2/rx^2 + (y-cy)^2/ry^2 <= 1
                    float dx = (x - cx) / rx;
                    float dy = (y - cy) / ry;
                    float dist = dx * dx + dy * dy;

                    if (dist <= 1f)
                    {
                        // Inside ellipse - smooth edge with anti-aliasing
                        float alpha = dist > 0.95f ? (1f - dist) / 0.05f : 1f;
                        tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
        }

        private void CreateWoodTableFrame(Transform parent)
        {
            // Create elegant wood frame around the table
            float frameThickness = 18f;
            Color woodMain = ModernUITheme.WoodDark;
            Color woodHighlight = ModernUITheme.WoodLight;
            Color goldTrim = new Color(ModernUITheme.GoldAccent.r, ModernUITheme.GoldAccent.g, ModernUITheme.GoldAccent.b, 0.6f);

            // Outer dark frame
            CreateFrameEdge(parent, "TopFrame", new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0.5f, 1), new Vector2(0, frameThickness), Vector2.zero, woodMain, true);

            CreateFrameEdge(parent, "BottomFrame", new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(0.5f, 0), new Vector2(0, frameThickness), Vector2.zero, woodMain * 0.85f, false);

            CreateFrameEdge(parent, "LeftFrame", new Vector2(0, 0), new Vector2(0, 1),
                new Vector2(0, 0.5f), new Vector2(frameThickness, 0), Vector2.zero, woodMain * 0.9f, false);

            CreateFrameEdge(parent, "RightFrame", new Vector2(1, 0), new Vector2(1, 1),
                new Vector2(1, 0.5f), new Vector2(frameThickness, 0), Vector2.zero, woodMain * 0.95f, false);

            // Inner gold trim line
            float trimOffset = frameThickness - 3f;
            float trimWidth = 2f;

            CreateTrimLine(parent, "TopTrim", new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0.5f, 1), new Vector2(-frameThickness * 2, trimWidth), new Vector2(0, -trimOffset), goldTrim);

            CreateTrimLine(parent, "BottomTrim", new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(0.5f, 0), new Vector2(-frameThickness * 2, trimWidth), new Vector2(0, trimOffset), goldTrim);

            CreateTrimLine(parent, "LeftTrim", new Vector2(0, 0), new Vector2(0, 1),
                new Vector2(0, 0.5f), new Vector2(trimWidth, -frameThickness * 2), new Vector2(trimOffset, 0), goldTrim);

            CreateTrimLine(parent, "RightTrim", new Vector2(1, 0), new Vector2(1, 1),
                new Vector2(1, 0.5f), new Vector2(trimWidth, -frameThickness * 2), new Vector2(-trimOffset, 0), goldTrim);

            // Corner decorations
            CreateCornerAccent(parent, "TopLeftCorner", new Vector2(0, 1), new Vector2(0, 1), frameThickness);
            CreateCornerAccent(parent, "TopRightCorner", new Vector2(1, 1), new Vector2(1, 1), frameThickness);
            CreateCornerAccent(parent, "BottomLeftCorner", new Vector2(0, 0), new Vector2(0, 0), frameThickness);
            CreateCornerAccent(parent, "BottomRightCorner", new Vector2(1, 0), new Vector2(1, 0), frameThickness);
        }

        private void CreateFrameEdge(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 sizeDelta, Vector2 position, Color color, bool addHighlight)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = position;

            Image img = obj.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;

            // Add shadow for depth
            Shadow shadow = obj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.5f);
            shadow.effectDistance = new Vector2(0, -2);

            // Add highlight line on top edge
            if (addHighlight)
            {
                GameObject highlightObj = new GameObject("Highlight");
                highlightObj.transform.SetParent(obj.transform, false);
                RectTransform hRect = highlightObj.AddComponent<RectTransform>();
                hRect.anchorMin = new Vector2(0, 1);
                hRect.anchorMax = new Vector2(1, 1);
                hRect.pivot = new Vector2(0.5f, 1);
                hRect.sizeDelta = new Vector2(0, 2);
                hRect.anchoredPosition = Vector2.zero;

                Image hImg = highlightObj.AddComponent<Image>();
                hImg.color = ModernUITheme.WoodLight * 1.2f;
                hImg.raycastTarget = false;
            }
        }

        private void CreateTrimLine(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 sizeDelta, Vector2 position, Color color)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = position;

            Image img = obj.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
        }

        private void CreateCornerAccent(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, float size)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = anchorMin;
            rect.sizeDelta = new Vector2(size, size);

            Image img = obj.AddComponent<Image>();
            img.color = ModernUITheme.WoodDark;
            img.raycastTarget = false;

            // Gold dot in corner
            GameObject dotObj = new GameObject("GoldDot");
            dotObj.transform.SetParent(obj.transform, false);
            RectTransform dotRect = dotObj.AddComponent<RectTransform>();
            dotRect.anchorMin = new Vector2(0.5f, 0.5f);
            dotRect.anchorMax = new Vector2(0.5f, 0.5f);
            dotRect.sizeDelta = new Vector2(8, 8);

            Image dotImg = dotObj.AddComponent<Image>();
            if (ModernUITheme.Instance != null && ModernUITheme.Instance.CircleSprite != null)
            {
                dotImg.sprite = ModernUITheme.Instance.CircleSprite;
            }
            dotImg.color = ModernUITheme.GoldAccent;
            dotImg.raycastTarget = false;
        }


        private void CreateTurnIndicators(Transform parent)
        {
            // Turn indicators are now handled by PlayerInfoPanel corner panels
            // No separate indicators needed - they caused visual clutter
            // Just initialize the array to avoid null reference issues
            for (int i = 0; i < 4; i++)
            {
                playerIndicators[i] = null;
            }
        }

        private void CreateScorePanel(Transform parent)
        {
            // Score panel showing all 4 individual player scores
            GameObject panelObj = new GameObject("ScorePanel");
            panelObj.transform.SetParent(parent, false);

            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 1f);
            panelRect.anchorMax = new Vector2(0.5f, 1f);
            panelRect.anchoredPosition = new Vector2(0, -70);
            panelRect.sizeDelta = new Vector2(560, 80);

            Image panelImg = panelObj.AddComponent<Image>();
            if (ModernUITheme.Instance != null && ModernUITheme.Instance.GlassPanelDarkSprite != null)
            {
                panelImg.sprite = ModernUITheme.Instance.GlassPanelDarkSprite;
                panelImg.type = Image.Type.Sliced;
            }
            panelImg.color = new Color(0.06f, 0.04f, 0.02f, 0.95f);

            Shadow shadow = panelObj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.6f);
            shadow.effectDistance = new Vector2(0, -5);

            Outline outline = panelObj.AddComponent<Outline>();
            outline.effectColor = new Color(ModernUITheme.GoldAccent.r, ModernUITheme.GoldAccent.g, ModernUITheme.GoldAccent.b, 0.6f);
            outline.effectDistance = new Vector2(2f, -2f);

            // Score text showing all 4 players
            GameObject textObj = new GameObject("ScoreText");
            textObj.transform.SetParent(panelObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            scoreText = textObj.AddComponent<TextMeshProUGUI>();
            scoreText.text = "S:0  E:0  N:0  W:0";
            scoreText.fontSize = 30;
            scoreText.fontStyle = FontStyles.Bold;
            scoreText.alignment = TextAlignmentOptions.Center;
            scoreText.color = ModernUITheme.TextPrimary;
            scoreText.textWrappingMode = TextWrappingModes.NoWrap;
        }

        private TextMeshProUGUI CreateInstructionText(Transform parent)
        {
            // Elegant instruction panel
            GameObject bgObj = new GameObject("Instructions_BG");
            bgObj.transform.SetParent(parent, false);
            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0.5f, 0.78f);
            bgRect.anchorMax = new Vector2(0.5f, 0.78f);
            bgRect.anchoredPosition = Vector2.zero;
            bgRect.sizeDelta = new Vector2(500, 56);

            Image bgImg = bgObj.AddComponent<Image>();
            if (ModernUITheme.Instance != null && ModernUITheme.Instance.GlassPanelDarkSprite != null)
            {
                bgImg.sprite = ModernUITheme.Instance.GlassPanelDarkSprite;
                bgImg.type = Image.Type.Sliced;
            }
            bgImg.color = new Color(0.05f, 0.03f, 0.02f, 0.88f);

            Shadow shadow = bgObj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.4f);
            shadow.effectDistance = new Vector2(0, -3);

            GameObject textObj = new GameObject("Instructions");
            textObj.transform.SetParent(bgObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "";
            tmp.fontSize = 28;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = ModernUITheme.TextPrimary;

            return tmp;
        }

        private TextMeshProUGUI CreateRoundText(Transform parent)
        {
            // Round indicator - top center, small
            GameObject bgObj = new GameObject("Round_BG");
            bgObj.transform.SetParent(parent, false);
            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0.5f, 1f);
            bgRect.anchorMax = new Vector2(0.5f, 1f);
            bgRect.anchoredPosition = new Vector2(0, -28);
            bgRect.sizeDelta = new Vector2(120, 32);

            Image bgImg = bgObj.AddComponent<Image>();
            if (ModernUITheme.Instance != null && ModernUITheme.Instance.GlassPanelDarkSprite != null)
            {
                bgImg.sprite = ModernUITheme.Instance.GlassPanelDarkSprite;
                bgImg.type = Image.Type.Sliced;
            }
            bgImg.color = new Color(0.04f, 0.02f, 0.01f, 0.85f);

            GameObject textObj = new GameObject("Round");
            textObj.transform.SetParent(bgObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "ROUND 1";
            tmp.fontSize = 16;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = ModernUITheme.GoldAccent;

            return tmp;
        }

        private RectTransform CreateContainer(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 position)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(800, 150);
            return rect;
        }


        private Button CreateStyledButton(Transform parent, string name, Vector2 anchor, string text, UnityEngine.Events.UnityAction onClick)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(280, 64);

            Image img = obj.AddComponent<Image>();

            Button btn = obj.AddComponent<Button>();
            btn.targetGraphic = img;

            // Casino-style gold button
            if (ModernUITheme.Instance != null && ModernUITheme.Instance.ButtonSprite != null)
            {
                img.sprite = ModernUITheme.Instance.ButtonSprite;
                img.type = Image.Type.Sliced;

                SpriteState states = new SpriteState();
                states.highlightedSprite = ModernUITheme.Instance.ButtonHoverSprite;
                states.pressedSprite = ModernUITheme.Instance.ButtonPressSprite;
                states.selectedSprite = ModernUITheme.Instance.ButtonSprite;
                btn.spriteState = states;
                btn.transition = Selectable.Transition.SpriteSwap;

                Shadow shadow = obj.AddComponent<Shadow>();
                shadow.effectColor = new Color(0, 0, 0, 0.5f);
                shadow.effectDistance = new Vector2(3, -3);
            }
            else
            {
                img.color = ModernUITheme.GoldAccent;
                ColorBlock colors = btn.colors;
                colors.normalColor = ModernUITheme.GoldAccent;
                colors.highlightedColor = ModernUITheme.GoldBright;
                colors.pressedColor = ModernUITheme.GoldDark;
                colors.selectedColor = ModernUITheme.GoldAccent;
                btn.colors = colors;
            }

            btn.onClick.AddListener(() => {
                SoundManager.Instance?.PlayButtonClick();
                onClick();
            });

            // Button text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(obj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text.ToUpper();
            tmp.fontSize = 22;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = ModernUITheme.PrimaryDark;
            tmp.fontStyle = FontStyles.Bold;

            return btn;
        }

        private void SubscribeToEvents()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnCardsDealt += OnCardsDealt;
                GameManager.Instance.OnCardPlayed += OnCardPlayed;
                GameManager.Instance.OnTrickWon += OnTrickWon;
                GameManager.Instance.OnRoundEnded += OnRoundEnded;
                GameManager.Instance.OnGameOver += OnGameOver;
                GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
                GameManager.Instance.OnPassPhaseComplete += OnPassPhaseComplete;
                GameManager.Instance.OnTrickStarted += OnTrickStarted;
                Debug.Log("[GameUI] All events subscribed successfully");

                // Create player info panels now that GameManager is available
                CreatePlayerInfoPanels();
            }
            else
            {
                Debug.LogError("[GameUI] GameManager.Instance is null! Events not subscribed!");
            }
        }

        private void OnStartClicked()
        {
            startButton.gameObject.SetActive(false);
            GameManager.Instance.StartGame();
        }

        private void OnPassClicked()
        {
            if (selectedForPass.Count == 3)
            {
                Player localPlayer = GameManager.Instance.GetHumanPlayer();

                // Local player's selected cards
                List<Card> localCards = new List<Card>();
                foreach (var cardUI in selectedForPass)
                {
                    localCards.Add(cardUI.Card);
                }

                if (GameManager.Instance.IsOnlineGame)
                {
                    // Online game - send pass cards to network
                    PlayerPosition fromPos = localPlayer.Position;
                    PlayerPosition toPos = Player.GetPlayerToRight(fromPos);

                    // Remove cards from local player's hand
                    localPlayer.RemoveCards(localCards);

                    // Send pass cards over network
                    if (NetworkGameSync.Instance != null)
                    {
                        NetworkGameSync.Instance.SendPassCards(fromPos, toPos, localCards);
                        Debug.Log($"[OnPassClicked] Sent {localCards.Count} cards from {fromPos} to {toPos}");
                    }

                    // Hide pass button and wait for all players to pass
                    // The actual pass completion will be triggered when all players have passed
                    // For now, we'll optimistically continue since the server handles sync
                    selectedForPass.Clear();
                    isPassPhase = false;
                    passButton.gameObject.SetActive(false);

                    // Refresh display (cards removed from hand)
                    DisplayPlayerHandAnimated();
                    instructionText.text = "Waiting for other players to pass...";

                    // In online mode, pass phase completion is handled by network sync
                    // The server will notify when all players have passed
                }
                else
                {
                    // Local game with bots - execute pass phase
                    var cardsToPass = new Dictionary<Player, List<Card>>();
                    cardsToPass[localPlayer] = localCards;

                    // AI players auto-select
                    foreach (var player in GameManager.Instance.Players)
                    {
                        if (!player.IsHuman)
                        {
                            cardsToPass[player] = AIPlayer.ChooseCardsToPass(player);
                        }
                    }

                    GameManager.Instance.ExecutePassPhase(cardsToPass);

                    // Clear selection
                    selectedForPass.Clear();
                    isPassPhase = false;
                    passButton.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Check if a card can be passed based on the "cannot empty a color" rule
        /// </summary>
        private bool CanPassCard(Card card)
        {
            Player localPlayer = GameManager.Instance.GetHumanPlayer();
            List<Card> hand = new List<Card>(localPlayer.Hand);

            // Build list of cards that would be passed (including this one)
            List<Card> wouldPass = new List<Card>();
            foreach (var selected in selectedForPass)
            {
                wouldPass.Add(selected.Card);
            }
            wouldPass.Add(card);

            // Calculate remaining cards of this suit after passing
            Suit suit = card.Suit;
            List<Card> suitCardsInHand = hand.Where(c => c.Suit == suit).ToList();
            List<Card> suitCardsToPass = wouldPass.Where(c => c.Suit == suit).ToList();
            int remainingInSuit = suitCardsInHand.Count - suitCardsToPass.Count;

            // If we'd still have cards of this suit, it's allowed
            if (remainingInSuit > 0)
            {
                return true;
            }

            // We would empty the suit - check if it's allowed
            return CanEmptySuit(suit, suitCardsInHand);
        }

        /// <summary>
        /// Check if it's legal to empty a suit by passing
        /// </summary>
        private bool CanEmptySuit(Suit suit, List<Card> suitCards)
        {
            // For Red (Hearts) and Green (Clubs): Cannot empty
            if (suit == Suit.Hearts || suit == Suit.Clubs)
            {
                return false;
            }

            // For Yellow (Diamonds): Can empty ONLY if all cards are 0, Reverse, +2, Skip, or Ace
            if (suit == Suit.Diamonds)
            {
                return suitCards.All(c =>
                    c.Rank == Rank.Ten ||      // 0
                    c.Rank == Rank.Jack ||     // Reverse
                    c.Rank == Rank.Queen ||    // +2
                    c.Rank == Rank.King ||     // Skip
                    c.Rank == Rank.Ace         // 1
                );
            }

            // For Blue (Spades): Can empty ONLY if all cards are +2, Skip, or Ace
            if (suit == Suit.Spades)
            {
                return suitCards.All(c =>
                    c.Rank == Rank.Queen ||    // +2
                    c.Rank == Rank.King ||     // Skip
                    c.Rank == Rank.Ace         // 1
                );
            }

            return false;
        }

        /// <summary>
        /// Update visual state of cards to show which can be passed
        /// </summary>
        private void UpdatePassableCards()
        {
            foreach (var cardUI in playerHandCards)
            {
                if (selectedForPass.Contains(cardUI))
                {
                    // Already selected - keep selected state
                    continue;
                }

                if (selectedForPass.Count >= 3)
                {
                    // Already have 3 selected - dim all others
                    cardUI.SetPlayable(false);
                }
                else
                {
                    // Show whether this card can be passed
                    bool canPass = CanPassCard(cardUI.Card);
                    cardUI.SetPlayable(canPass);
                }
            }
        }

        private void OnCardsDealt()
        {
            // Ensure player info panels exist
            if (playerInfoPanels.Count == 0)
            {
                CreatePlayerInfoPanels();
            }

            // Clear special card indicators from previous round
            foreach (var panel in playerInfoPanels.Values)
            {
                panel?.ClearSpecialCards();
            }

            // Set animating during initial deal
            isAnimating = true;
            DisplayPlayerHandAnimatedWithCallback(() => {
                isAnimating = false;
                Debug.Log("[OnCardsDealt] Deal animation complete, isAnimating = false");
            });
            DisplayOtherPlayersCardCount();
            UpdateRoundText();
        }

        private void DisplayPlayerHandAnimated()
        {
            DisplayPlayerHandAnimatedWithCallback(null);
        }

        private void DisplayPlayerHandAnimatedWithCallback(System.Action onComplete)
        {
            Debug.Log("[DisplayPlayerHandAnimated] Starting to display hand");

            // Clear existing cards
            ClearPlayerHand();

            Player humanPlayer = GameManager.Instance.GetHumanPlayer();
            if (humanPlayer == null)
            {
                Debug.LogError("[DisplayPlayerHandAnimated] Human player is null!");
                onComplete?.Invoke();
                return;
            }

            Debug.Log($"[DisplayPlayerHandAnimated] Human player has {humanPlayer.Hand.Count} cards");

            // Sort cards by suit then by rank for organized display
            List<Card> sortedHand = new List<Card>(humanPlayer.Hand);
            sortedHand.Sort((a, b) => a.GetSortValue().CompareTo(b.GetSortValue()));

            if (sortedHand.Count == 0)
            {
                Debug.LogWarning("[DisplayPlayerHandAnimated] No cards to display!");
                onComplete?.Invoke();
                return;
            }

            float totalWidth = (sortedHand.Count - 1) * cardSpacing + cardWidth;
            float startX = -totalWidth / 2 + cardWidth / 2;

            int cardsAnimated = 0;
            int totalCards = sortedHand.Count;

            for (int i = 0; i < sortedHand.Count; i++)
            {
                Card card = sortedHand[i];
                CardUI cardUI = CreateCardUI(card, playerHandContainer);

                float xPos = startX + i * cardSpacing;
                Vector2 targetPos = new Vector2(xPos, 0);

                // Animate from deck position with callback on last card
                if (CardAnimator.Instance != null)
                {
                    RectTransform cardRect = cardUI.GetComponent<RectTransform>();

                    // Only the last card triggers the completion callback
                    System.Action cardCallback = null;
                    if (onComplete != null)
                    {
                        cardCallback = () => {
                            cardsAnimated++;
                            if (cardsAnimated >= totalCards)
                            {
                                Debug.Log($"[DisplayPlayerHandAnimated] All {totalCards} cards animated");
                                onComplete.Invoke();
                            }
                        };
                    }

                    CardAnimator.Instance.AnimateDeal(cardRect, new Vector2(0, 400), targetPos, i * 0.05f, cardCallback);
                    SoundManager.Instance?.PlayCardDeal();
                }
                else
                {
                    cardUI.SetOriginalPosition(targetPos);
                    cardsAnimated++;
                }

                cardUI.SetOriginalPosition(targetPos);
                cardUI.OnCardClicked += OnPlayerCardClicked;

                playerHandCards.Add(cardUI);
            }

            // If no animator, call completion immediately
            if (CardAnimator.Instance == null && onComplete != null)
            {
                onComplete.Invoke();
            }

            Debug.Log($"[DisplayPlayerHandAnimated] Created {playerHandCards.Count} card UIs");
        }

        private void DisplayOtherPlayersCardCount()
        {
            // Update player info panels instead of old labels
            UpdatePlayerInfoPanels();

            // Clear old containers (backwards compatibility)
            ClearContainer(leftHandContainer);
            ClearContainer(topHandContainer);
            ClearContainer(rightHandContainer);
        }

        /// <summary>
        /// Update all player info panels with current data
        /// </summary>
        private void UpdatePlayerInfoPanels()
        {
            // Create panels if they don't exist yet
            if (playerInfoPanels.Count == 0 && GameManager.Instance != null)
            {
                CreatePlayerInfoPanels();
            }

            // Update each panel
            foreach (var kvp in playerInfoPanels)
            {
                kvp.Value?.UpdateDisplay();
            }
        }

        private void CreatePlayerLabel(RectTransform container, string text)
        {
            // Background with premium glass effect
            GameObject bgObj = new GameObject("LabelBG");
            bgObj.transform.SetParent(container, false);
            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchoredPosition = Vector2.zero;
            bgRect.sizeDelta = new Vector2(200, 85);

            Image bgImg = bgObj.AddComponent<Image>();
            if (PremiumVisuals.Instance != null && PremiumVisuals.Instance.GlassSprite != null)
            {
                bgImg.sprite = PremiumVisuals.Instance.GlassSprite;
                bgImg.color = new Color(0.08f, 0.05f, 0.12f, 0.85f);
                bgImg.type = Image.Type.Sliced;
            }
            else
            {
                bgImg.color = new Color(0, 0, 0, 0.5f);
            }

            // Add subtle shadow
            Shadow shadow = bgObj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.4f);
            shadow.effectDistance = new Vector2(2, -2);

            // Text with better styling
            GameObject obj = new GameObject("Label");
            obj.transform.SetParent(bgObj.transform, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(190, 75);

            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 20;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(1f, 0.95f, 0.85f); // Warm white
            tmp.fontStyle = FontStyles.Normal;
        }

        private CardUI CreateCardUI(Card card, RectTransform parent)
        {
            GameObject cardObj = new GameObject($"Card_{card.GetUnoName()}");
            cardObj.transform.SetParent(parent, false);

            RectTransform rect = cardObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(cardWidth, cardHeight);

            CardUI cardUI = cardObj.AddComponent<CardUI>();
            cardUI.SetCard(card, true);

            return cardUI;
        }

        private void OnPlayerCardClicked(CardUI cardUI)
        {
            Debug.Log($"[OnPlayerCardClicked] Card clicked: {cardUI.Card.GetUnoName()}, isPassPhase: {isPassPhase}, CanPlay: {CanPlayNow()}, Cooldown: {GetRemainingCooldown():F2}s, GameState: {GameManager.Instance.CurrentState}");

            // Block input during deal animations
            if (isProcessingPlay)
            {
                Debug.Log("[OnPlayerCardClicked] Blocked - processing in progress");
                return;
            }

            if (isPassPhase)
            {
                // Toggle selection for pass phase
                if (selectedForPass.Contains(cardUI))
                {
                    selectedForPass.Remove(cardUI);
                    cardUI.SetSelected(false);
                    // Refresh which cards are passable now
                    UpdatePassableCards();
                }
                else if (selectedForPass.Count < 3)
                {
                    // Validate that this card can be passed
                    if (CanPassCard(cardUI.Card))
                    {
                        selectedForPass.Add(cardUI);
                        cardUI.SetSelected(true);
                        SoundManager.Instance?.PlayCardSelect();
                        // Refresh which cards are passable now
                        UpdatePassableCards();
                    }
                    else
                    {
                        // Show feedback that card can't be passed
                        instructionText.text = "Can't pass - would empty a color!";
                        Debug.Log($"Cannot pass {cardUI.Card.GetUnoName()} - would illegally empty the color");
                    }
                }

                // Update button text
                passButton.GetComponentInChildren<TextMeshProUGUI>().text = $"Pass Cards ({selectedForPass.Count}/3)";
            }
            else if (GameManager.Instance.CurrentState == GameState.PlayingTricks)
            {
                // NUCLEAR LOCK: Check if we can play
                if (!CanPlayNow())
                {
                    Debug.Log($"[OnPlayerCardClicked] BLOCKED by cooldown ({GetRemainingCooldown():F2}s remaining)");
                    return;
                }

                // Try to play the card
                Player localPlayer = GameManager.Instance.GetHumanPlayer();
                bool isLocalTurn = GameManager.Instance.IsLocalPlayerTurn();
                Debug.Log($"[OnPlayerCardClicked] Trying to play card. CurrentPlayer: {GameManager.Instance.CurrentPlayer.PlayerName}, LocalPlayer: {localPlayer.PlayerName}, IsLocalTurn: {isLocalTurn}");

                if (isLocalTurn)
                {
                    // Validate the card is actually playable
                    Suit? ledSuit = GameManager.Instance.LedSuit;
                    List<Card> playableCards = localPlayer.GetPlayableCards(ledSuit);

                    if (!playableCards.Any(c => c.Equals(cardUI.Card)))
                    {
                        Debug.Log($"[OnPlayerCardClicked] Card {cardUI.Card.GetUnoName()} is not playable! Playable cards: {string.Join(", ", playableCards.Select(c => c.GetUnoName()))}");
                        instructionText.text = "You must follow suit or play a forced card!";
                        return;
                    }

                    isProcessingPlay = true;
                    Debug.Log($"[OnPlayerCardClicked] Playing {cardUI.Card.GetUnoName()}");
                    bool success = GameManager.Instance.PlayCard(localPlayer, cardUI.Card);
                    Debug.Log($"[OnPlayerCardClicked] PlayCard result: {success}");

                    if (success)
                    {
                        playerHandCards.Remove(cardUI);
                        Destroy(cardUI.gameObject);
                        RefreshPlayerHandPositions();
                    }

                    isProcessingPlay = false;
                }
                else
                {
                    Debug.Log($"[OnPlayerCardClicked] Not local player's turn!");
                }
            }
        }

        private void RefreshPlayerHandPositions()
        {
            float totalWidth = (playerHandCards.Count - 1) * cardSpacing + cardWidth;
            float startX = -totalWidth / 2 + cardWidth / 2;

            for (int i = 0; i < playerHandCards.Count; i++)
            {
                float xPos = startX + i * cardSpacing;
                Vector2 newPos = new Vector2(xPos, 0);

                if (CardAnimator.Instance != null)
                {
                    RectTransform cardRect = playerHandCards[i].GetComponent<RectTransform>();
                    CardAnimator.Instance.AnimateMove(cardRect, newPos);
                }

                playerHandCards[i].SetOriginalPosition(newPos);
            }
        }

        private void OnCardPlayed(Player player, Card card)
        {
            // Reset watchdog timer - a card was played
            ResetWatchdogTimer();

            // NUCLEAR LOCK: Set cooldown immediately to prevent any other plays
            SetPlayCooldown(CARD_PLAY_COOLDOWN);

            // Cancel any pending AI play immediately
            CancelPendingAIPlay();

            // Play sound
            SoundManager.Instance?.PlayCardPlay();

            // Particle effects disabled - they were causing visual flickering

            // Show card in trick area
            CardUI cardUI = CreateCardUI(card, trickArea);
            trickCards.Add(cardUI);

            // Jawaker-style: overlapping cards in center
            // Cards stack with slight offset based on play order
            int cardIndex = trickCards.Count - 1; // 0-based index for this card
            float baseOffset = 25f; // Offset per card
            Vector2 pos = player.Position switch
            {
                // Stack cards with slight directional offset from player position
                PlayerPosition.South => new Vector2(baseOffset * cardIndex * 0.3f, -30 + cardIndex * 10),
                PlayerPosition.West => new Vector2(-40 + cardIndex * 15, baseOffset * cardIndex * 0.3f),
                PlayerPosition.North => new Vector2(-baseOffset * cardIndex * 0.3f, 30 - cardIndex * 10),
                PlayerPosition.East => new Vector2(40 - cardIndex * 15, -baseOffset * cardIndex * 0.3f),
                _ => Vector2.zero
            };

            cardUI.SetOriginalPosition(pos);

            // Animate card to position with callback when done
            if (CardAnimator.Instance != null)
            {
                RectTransform cardRect = cardUI.GetComponent<RectTransform>();
                Vector2 startPos = GetPlayerStartPosition(player.Position);
                cardRect.anchoredPosition = startPos;
                CardAnimator.Instance.AnimatePlay(cardRect, pos, () => OnCardPlayAnimationComplete(player));
            }
            else
            {
                cardUI.GetComponent<RectTransform>().anchoredPosition = pos;
                OnCardPlayAnimationComplete(player);
            }

            UpdateTurnIndicator();
            UpdateInstructionText();
            DisplayOtherPlayersCardCount();

            Debug.Log($"[OnCardPlayed] State: {GameManager.Instance.CurrentState}, TrickCount: {trickCards.Count}, Current player: {GameManager.Instance.CurrentPlayer.PlayerName}, IsHuman: {GameManager.Instance.CurrentPlayer.IsHuman}");
        }

        /// <summary>
        /// Called when card play animation finishes - schedules next action
        /// </summary>
        private void OnCardPlayAnimationComplete(Player playerWhoJustPlayed)
        {
            Debug.Log($"[OnCardPlayAnimationComplete] Animation done. TrickCards: {trickCards.Count}, Cooldown remaining: {GetRemainingCooldown():F2}s");

            // If this was the 4th card (trick complete), OnTrickWon will handle the next trick
            if (trickCards.Count >= 4)
            {
                Debug.Log("[OnCardPlayAnimationComplete] Trick complete (4 cards), OnTrickWon will handle next action");
                return;
            }

            // Schedule next action after cooldown expires
            float delay = GetRemainingCooldown() + 0.05f; // Add tiny buffer
            if (delay < 0.1f) delay = 0.1f;

            if (GameManager.Instance.CurrentState == GameState.PlayingTricks)
            {
                bool isLocalTurn = GameManager.Instance.IsLocalPlayerTurn();
                if (isLocalTurn)
                {
                    // It's local player's turn - highlight playable cards after cooldown
                    Debug.Log($"[OnCardPlayAnimationComplete] Local player's turn - highlighting in {delay:F2}s");
                    StartCoroutine(DelayedHighlightCards(delay));
                }
                else if (!GameManager.Instance.IsOnlineGame)
                {
                    // It's AI's turn in local game - schedule AI play after cooldown
                    Debug.Log($"[OnCardPlayAnimationComplete] Scheduling AI play for {GameManager.Instance.CurrentPlayer.PlayerName} in {delay:F2}s");
                    ScheduleAIPlay(delay);
                }
                else
                {
                    // Online game - waiting for remote player
                    Debug.Log($"[OnCardPlayAnimationComplete] Waiting for remote player {GameManager.Instance.CurrentPlayer.PlayerName}");
                }
            }
            else
            {
                Debug.Log($"[OnCardPlayAnimationComplete] Not in PlayingTricks state (state={GameManager.Instance.CurrentState}), skipping");
            }
        }

        private System.Collections.IEnumerator DelayedHighlightCards(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (GameManager.Instance.CurrentState == GameState.PlayingTricks && GameManager.Instance.IsLocalPlayerTurn())
            {
                HighlightPlayableCards();
            }
        }

        /// <summary>
        /// Cancel any pending AI play coroutine
        /// </summary>
        private void CancelPendingAIPlay()
        {
            if (pendingAIPlayCoroutine != null)
            {
                StopCoroutine(pendingAIPlayCoroutine);
                pendingAIPlayCoroutine = null;
            }
        }

        /// <summary>
        /// Schedule an AI play after a delay
        /// </summary>
        private void ScheduleAIPlay(float delay)
        {
            CancelPendingAIPlay();
            pendingAIPlayCoroutine = StartCoroutine(DelayedAIPlay(delay));
        }

        private System.Collections.IEnumerator DelayedAIPlay(float delay)
        {
            yield return new WaitForSeconds(delay);
            AIPlayCard();
        }

        private Vector2 GetPlayerStartPosition(PlayerPosition position)
        {
            // Jawaker-style: cards animate from player positions around the table
            return position switch
            {
                PlayerPosition.South => new Vector2(0, -250),
                PlayerPosition.West => new Vector2(-350, 0),
                PlayerPosition.North => new Vector2(0, 250),
                PlayerPosition.East => new Vector2(350, 0),
                _ => Vector2.zero
            };
        }

        private void UpdateTurnIndicator()
        {
            if (this == null) return; // Destroyed check
            // Update player info panels turn state (corner panels handle their own glow effects)
            Player currentPlayer = null;
            if (GameManager.Instance.CurrentState == GameState.PlayingTricks)
            {
                currentPlayer = GameManager.Instance.CurrentPlayer;
            }

            foreach (var kvp in playerInfoPanels)
            {
                bool isActive = currentPlayer != null && kvp.Key == currentPlayer.Position;
                kvp.Value?.SetTurnActive(isActive);
            }
        }

        private void AIPlayCard()
        {
            Debug.Log($"[AIPlayCard] Called. State: {GameManager.Instance.CurrentState}, CanPlay: {CanPlayNow()}, Cooldown: {GetRemainingCooldown():F2}s, IsOnline: {GameManager.Instance.IsOnlineGame}");

            // In online games, AI should NEVER play - remote players play their own cards
            if (GameManager.Instance.IsOnlineGame)
            {
                Debug.Log("[AIPlayCard] Online game - AI disabled, waiting for remote player");
                return;
            }

            // NUCLEAR LOCK: Don't play if cooldown hasn't expired
            if (!CanPlayNow())
            {
                float delay = GetRemainingCooldown() + 0.05f;
                Debug.Log($"[AIPlayCard] BLOCKED by cooldown, rescheduling in {delay:F2}s");
                ScheduleAIPlay(delay);
                return;
            }

            if (GameManager.Instance.CurrentState != GameState.PlayingTricks)
            {
                Debug.Log("[AIPlayCard] Not in PlayingTricks state, aborting");
                return;
            }

            Player currentPlayer = GameManager.Instance.CurrentPlayer;
            Debug.Log($"[AIPlayCard] Current player: {currentPlayer.PlayerName}, IsHuman: {currentPlayer.IsHuman}");

            if (currentPlayer.IsHuman)
            {
                Debug.Log("[AIPlayCard] Current player is human, aborting");
                return;
            }

            // Check if AI has any cards left
            if (currentPlayer.Hand.Count == 0)
            {
                Debug.LogError($"[AIPlayCard] AI {currentPlayer.PlayerName} has no cards in hand!");
                return;
            }

            Suit? ledSuit = GameManager.Instance.LedSuit;
            List<Card> currentTrick = new List<Card>(GameManager.Instance.CurrentTrick);

            Debug.Log($"[AIPlayCard] AI {currentPlayer.PlayerName} choosing card. LedSuit: {ledSuit}, TrickCount: {currentTrick.Count}, HandCount: {currentPlayer.Hand.Count}");
            Debug.Log($"[AIPlayCard] AI hand: {string.Join(", ", currentPlayer.Hand.Select(c => c.GetUnoName()))}");

            Card cardToPlay = AIPlayer.ChooseCardToPlay(currentPlayer, ledSuit, currentTrick);

            if (cardToPlay == null)
            {
                Debug.LogError($"[AIPlayCard] AI {currentPlayer.PlayerName} returned null card! Hand: {string.Join(", ", currentPlayer.Hand.Select(c => c.GetUnoName()))}");
                // Emergency fallback - play first card in hand
                if (currentPlayer.Hand.Count > 0)
                {
                    cardToPlay = currentPlayer.Hand[0];
                    Debug.LogWarning($"[AIPlayCard] Using emergency fallback card: {cardToPlay.GetUnoName()}");
                }
                else
                {
                    return;
                }
            }

            Debug.Log($"[AIPlayCard] AI {currentPlayer.PlayerName} playing {cardToPlay.GetUnoName()}");
            bool success = GameManager.Instance.PlayCard(currentPlayer, cardToPlay);
            Debug.Log($"[AIPlayCard] PlayCard returned: {success}");

            if (!success)
            {
                Debug.LogError($"[AIPlayCard] Failed to play card! Trying again in 0.15s");
                ScheduleAIPlay(0.15f);
            }
        }

        private void OnTrickWon(Player winner, List<Card> cards)
        {
            // Reset watchdog timer - trick was won
            ResetWatchdogTimer();

            // NUCLEAR LOCK: Set long cooldown for trick completion - no plays during collection
            SetPlayCooldown(TRICK_COMPLETE_COOLDOWN);

            // Cancel any pending AI plays
            CancelPendingAIPlay();

            Debug.Log($"[OnTrickWon] {winner.PlayerName} wins. TrickCards count: {trickCards.Count}. Lock set for {TRICK_COMPLETE_COOLDOWN}s");

            // Play sound
            SoundManager.Instance?.PlayTrickWin();

            // Highlight the winner's panel
            HighlightTrickWinner(winner.Position);

            // Update all player scores in real-time
            foreach (var panel in playerInfoPanels.Values)
            {
                panel?.UpdateScore();
            }

            // Show special card indicators if Queen of Spades or 10 of Diamonds was taken
            if (playerInfoPanels.ContainsKey(winner.Position))
            {
                var winnerPanel = playerInfoPanels[winner.Position];
                foreach (var card in cards)
                {
                    if (card.IsQueenOfSpades())
                    {
                        winnerPanel?.ShowQueenOfSpades();
                    }
                    if (card.Suit == Suit.Diamonds && card.Rank == Rank.Ten)
                    {
                        winnerPanel?.ShowTenOfDiamonds();
                    }
                }
            }

            // Animate cards collecting to winner
            if (CardAnimator.Instance != null && trickCards.Count > 0)
            {
                RectTransform[] cardRects = new RectTransform[trickCards.Count];
                for (int i = 0; i < trickCards.Count; i++)
                {
                    if (trickCards[i] != null)
                    {
                        cardRects[i] = trickCards[i].GetComponent<RectTransform>();
                    }
                }

                Vector2 collectPos = GetPlayerStartPosition(winner.Position);
                CardAnimator.Instance.AnimateCollect(cardRects, collectPos, () => {
                    ClearTrickAreaImmediate();
                    ContinueAfterTrick();
                });
            }
            else
            {
                StartCoroutine(DelayedClearTrickArea(0.8f));
            }

            instructionText.text = $"{winner.PlayerName} wins the trick!";
            UpdateScoreText();
        }

        private void HighlightTrickWinner(PlayerPosition position)
        {
            StartCoroutine(AnimateTrickWinnerHighlight(position));
        }

        private System.Collections.IEnumerator AnimateTrickWinnerHighlight(PlayerPosition position)
        {
            if (!playerInfoPanels.TryGetValue(position, out PlayerInfoPanel panel))
                yield break;

            // Create a highlight overlay on the winner's panel
            GameObject highlightObj = new GameObject("TrickWinHighlight");
            highlightObj.transform.SetParent(panel.transform, false);

            RectTransform highlightRect = highlightObj.AddComponent<RectTransform>();
            highlightRect.anchorMin = Vector2.zero;
            highlightRect.anchorMax = Vector2.one;
            highlightRect.sizeDelta = new Vector2(20, 20);

            Image highlightImg = highlightObj.AddComponent<Image>();
            highlightImg.color = new Color(1f, 0.85f, 0.3f, 0f); // Gold, starts transparent
            highlightImg.raycastTarget = false;

            // Animate the highlight
            float duration = 0.6f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Pulse effect - fade in quickly, fade out slowly
                float alpha;
                if (t < 0.2f)
                {
                    alpha = t / 0.2f * 0.6f; // Quick fade in
                }
                else
                {
                    alpha = 0.6f * (1 - (t - 0.2f) / 0.8f); // Slow fade out
                }

                highlightImg.color = new Color(1f, 0.85f, 0.3f, alpha);

                // Scale pulse
                float scale = 1f + Mathf.Sin(t * Mathf.PI * 2) * 0.05f;
                panel.transform.localScale = Vector3.one * scale;

                yield return null;
            }

            panel.transform.localScale = Vector3.one;
            Destroy(highlightObj);
        }

        private void ClearTrickAreaImmediate()
        {
            foreach (var card in trickCards)
            {
                if (card != null)
                    Destroy(card.gameObject);
            }
            trickCards.Clear();
        }

        private void ContinueAfterTrick()
        {
            Debug.Log($"[ContinueAfterTrick] State: {GameManager.Instance.CurrentState}");
            // OnTrickStarted event from GameManager handles the next trick setup
            // This is just called after the animation completes
        }

        private System.Collections.IEnumerator DelayedClearTrickArea(float delay)
        {
            yield return new WaitForSeconds(delay);
            ClearTrickArea();
        }

        private void ClearTrickArea()
        {
            ClearTrickAreaImmediate();
            ContinueAfterTrick();
        }

        private void OnGameStateChanged(GameState state)
        {
            if (this == null) return; // Destroyed check
            UpdateInstructionText();
            UpdateTurnIndicator();

            if (state == GameState.PassingCards)
            {
                isPassPhase = true;
                passButton.gameObject.SetActive(true);
                instructionText.text = "Select 3 cards to pass right";
                // Highlight which cards can be passed (respecting the "cannot empty color" rule)
                UpdatePassableCards();
            }
            else if (state == GameState.PlayingTricks)
            {
                Debug.Log($"[OnGameStateChanged] PlayingTricks - CurrentPlayer: {GameManager.Instance.CurrentPlayer.PlayerName}");
                // Reset watchdog timer when entering playing state
                ResetWatchdogTimer();
                // OnTrickStarted will handle highlighting and AI scheduling
            }
        }

        private void OnPassPhaseComplete()
        {
            Debug.Log("[OnPassPhaseComplete] Pass phase completed, refreshing hand display");
            // Set animating flag BEFORE displaying cards so OnTrickStarted knows to wait
            isAnimating = true;
            DisplayPlayerHandAnimatedWithCallback(() => {
                isAnimating = false;
                Debug.Log("[OnPassPhaseComplete] Hand animation complete, isAnimating = false");
            });
            isPassPhase = false;
            Debug.Log($"[OnPassPhaseComplete] isPassPhase = {isPassPhase}, isAnimating = {isAnimating}");
        }

        /// <summary>
        /// Called when a new trick starts - this is the definitive place to schedule AI play
        /// </summary>
        private void OnTrickStarted(Player leadingPlayer)
        {
            float cooldown = GetRemainingCooldown();
            Debug.Log($"[OnTrickStarted] New trick started, leader: {leadingPlayer.PlayerName}, IsHuman: {leadingPlayer.IsHuman}, Cooldown: {cooldown:F2}s");

            // Reset watchdog timer - new trick started
            ResetWatchdogTimer();

            UpdateTurnIndicator();
            UpdateInstructionText();

            // Cancel any pending AI play to avoid duplicate scheduling
            CancelPendingAIPlay();

            // NUCLEAR LOCK: Wait for cooldown to expire before allowing play
            float delay = cooldown + 0.1f;
            if (delay < 0.15f) delay = 0.15f;

            Debug.Log($"[OnTrickStarted] Scheduling turn start in {delay:F2}s");
            StartCoroutine(StartTurnAfterDelay(leadingPlayer, delay));
        }

        private System.Collections.IEnumerator StartTurnAfterDelay(Player player, float delay)
        {
            yield return new WaitForSeconds(delay);

            // NUCLEAR LOCK: Wait until we can play
            while (!CanPlayNow())
            {
                Debug.Log($"[StartTurnAfterDelay] Waiting for cooldown: {GetRemainingCooldown():F2}s remaining");
                yield return new WaitForSeconds(0.1f);
            }

            // Double-check we're still in the right state
            if (GameManager.Instance.CurrentState != GameState.PlayingTricks)
            {
                Debug.Log($"[StartTurnAfterDelay] State changed to {GameManager.Instance.CurrentState}, aborting");
                yield break;
            }

            // Use the CURRENT player (not the one passed in - it may have changed due to network)
            Player currentPlayer = GameManager.Instance.CurrentPlayer;
            Debug.Log($"[StartTurnAfterDelay] Ready to proceed. Original leader: {player.PlayerName}, Current player: {currentPlayer.PlayerName}");

            bool isLocalTurn = GameManager.Instance.IsLocalPlayerTurn();
            if (isLocalTurn)
            {
                // Local player's turn - highlight playable cards
                Debug.Log("[StartTurnAfterDelay] Local player's turn - highlighting playable cards");
                HighlightPlayableCards();
            }
            else if (!GameManager.Instance.IsOnlineGame)
            {
                // AI leads in local game - play immediately
                Debug.Log($"[StartTurnAfterDelay] AI {currentPlayer.PlayerName}'s turn - playing now");
                AIPlayCard();
            }
            else
            {
                // Online game - waiting for remote player
                Debug.Log($"[StartTurnAfterDelay] Waiting for remote player {currentPlayer.PlayerName}");
            }
        }

        private System.Collections.IEnumerator DelayedTrickStartAction(Player leadingPlayer, float delay)
        {
            yield return new WaitForSeconds(delay);

            // Check again if still animating with a safety timeout
            float waitTime = 0f;
            float maxWaitTime = 5f; // Maximum 5 seconds wait
            while (isAnimating && waitTime < maxWaitTime)
            {
                yield return new WaitForSeconds(0.2f);
                waitTime += 0.2f;
            }

            if (waitTime >= maxWaitTime)
            {
                Debug.LogWarning("[DelayedTrickStartAction] Timeout waiting for animation, forcing continue");
                isAnimating = false;
            }

            Debug.Log($"[DelayedTrickStartAction] Animation wait complete, proceeding with {leadingPlayer.PlayerName}");

            if (leadingPlayer.IsHuman)
            {
                HighlightPlayableCards();
            }
            else
            {
                ScheduleAIPlay(0.15f);
            }
        }

        private System.Collections.IEnumerator DelayedStartNextRound(float delay)
        {
            yield return new WaitForSeconds(delay);
            StartNextRound();
        }

        private void OnRoundEnded(Player[] players)
        {
            SoundManager.Instance?.PlayRoundEnd();
            UpdateScoreText();
            instructionText.text = "Round ended! Starting next round...";
            StartCoroutine(DelayedStartNextRound(2f));
        }

        private void StartNextRound()
        {
            if (GameManager.Instance.CurrentState == GameState.GameOver)
                return;

            if (GameManager.Instance.IsOnlineGame)
            {
                // In online games, only the host starts new rounds
                // Non-host clients wait for card deal from host
                bool isHost = NetworkGameSync.Instance?.IsHost ?? false;
                if (isHost)
                {
                    GameManager.Instance.StartNewRound();
                }
                else
                {
                    // Non-host: wait for cards from host
                    instructionText.text = "Waiting for next round...";
                    GameManager.Instance.SetState_Public(GameState.Dealing);
                }
            }
            else
            {
                GameManager.Instance.StartNewRound();
            }
        }

        private void OnGameOver(Team winningTeam)
        {
            // Determine win/loss relative to local player
            Player localPlayer = GameManager.Instance.GetHumanPlayer();
            bool localPlayerWon = (localPlayer != null && localPlayer.Team != winningTeam) ? false : true;
            // winningTeam is the team that WON (the other team lost)
            // Actually: OnGameOver is invoked with the WINNING team
            localPlayerWon = localPlayer != null && localPlayer.Team == winningTeam;

            if (localPlayerWon)
            {
                SoundManager.Instance?.PlayGameWin();
                instructionText.text = "Game Over! You WIN!";
            }
            else
            {
                SoundManager.Instance?.PlayGameLose();
                instructionText.text = "Game Over! You LOSE!";
            }

            // Get losing player info
            Player loser = GameManager.Instance.GetLosingPlayer();
            string loserName = loser?.PlayerName ?? "";
            int loserScore = loser?.TotalPoints ?? 0;

            // Get individual scores for display
            int yourScore = localPlayer?.TotalPoints ?? 0;
            int roundsPlayed = GameManager.Instance.RoundNumber;

            bool isOnline = GameManager.Instance.IsOnlineGame;

            // Show enhanced game over screen
            ShowGameOverScreen(winningTeam, yourScore, 0, roundsPlayed, loserName, loserScore, isOnline);
        }

        private void ShowGameOverScreen(Team winningTeam, int yourScore, int opponentScore, int roundsPlayed, string loserName = "", int loserScore = 0, bool isOnline = false)
        {
            // Create GameOverScreen if it doesn't exist
            if (GameOverScreen.Instance == null)
            {
                GameObject gameOverObj = new GameObject("GameOverScreen");
                gameOverObj.transform.SetParent(transform);
                GameOverScreen screen = gameOverObj.AddComponent<GameOverScreen>();

                screen.OnPlayAgain = () => {
                    startButton.gameObject.SetActive(false);

                    // Check if Barteyyeh is complete
                    if (BarteyyehManager.Instance != null && BarteyyehManager.Instance.IsBarteyyehComplete)
                    {
                        // Reset Barteyyeh for new series
                        BarteyyehManager.Instance.ResetBarteyyeh();
                    }

                    // Route through GameController for proper flow (online → ready screen, local → new game)
                    GameController.Instance?.StartNextBarteyyehGame();
                };

                screen.OnMainMenu = () => {
                    startButton.gameObject.SetActive(false);
                    GameController.Instance?.ReturnToMainMenu();
                };
            }

            GameOverScreen.Instance.Show(winningTeam, yourScore, opponentScore, roundsPlayed, loserName, loserScore, isOnline);
        }

        private void HighlightPlayableCards()
        {
            Suit? ledSuit = GameManager.Instance.LedSuit;
            Player localPlayer = GameManager.Instance.GetHumanPlayer();
            List<Card> playable = localPlayer.GetPlayableCards(ledSuit);

            Debug.Log($"[HighlightPlayableCards] LedSuit: {ledSuit}, Hand count: {localPlayer.Hand.Count}, Playable count: {playable.Count}, CardUI count: {playerHandCards.Count}");
            Debug.Log($"[HighlightPlayableCards] Playable cards: {string.Join(", ", playable.Select(c => c.GetUnoName()))}");

            int playableCount = 0;
            foreach (var cardUI in playerHandCards)
            {
                if (cardUI == null || cardUI.Card == null)
                {
                    Debug.LogWarning("[HighlightPlayableCards] Null cardUI or Card found!");
                    continue;
                }

                // Use Equals for proper card comparison
                bool canPlay = playable.Any(p => p.Equals(cardUI.Card));
                cardUI.SetPlayable(canPlay);
                if (canPlay)
                {
                    playableCount++;
                    Debug.Log($"[HighlightPlayableCards] Card {cardUI.Card.GetUnoName()} is playable");
                }
            }

            Debug.Log($"[HighlightPlayableCards] Set {playableCount} cards as playable out of {playerHandCards.Count} UI cards");

            // Safety check: if no cards are playable but we have cards, something is wrong
            if (playableCount == 0 && playerHandCards.Count > 0 && localPlayer.Hand.Count > 0)
            {
                Debug.LogError($"[HighlightPlayableCards] BUG: No cards marked as playable! Hand: {string.Join(", ", localPlayer.Hand.Select(c => c.GetUnoName()))}");
                // Emergency: make all cards playable so game doesn't get stuck
                foreach (var cardUI in playerHandCards)
                {
                    cardUI.SetPlayable(true);
                }
            }
        }

        private void UpdateInstructionText()
        {
            if (GameManager.Instance.CurrentState == GameState.PlayingTricks)
            {
                Player current = GameManager.Instance.CurrentPlayer;
                if (GameManager.Instance.IsLocalPlayerTurn())
                {
                    instructionText.text = "Your turn - tap a card to play";
                }
                else
                {
                    instructionText.text = $"{current.PlayerName}'s turn...";
                }
            }
        }

        private void UpdateScoreText()
        {
            // Show all 4 individual player scores
            var players = GameManager.Instance.Players;
            string[] parts = new string[4];
            for (int i = 0; i < 4; i++)
            {
                // Use short name (first word or position initial)
                string shortName = GetShortPlayerName(players[i]);
                int score = players[i].TotalPoints;
                // Highlight in red if approaching 101
                if (score >= 80)
                    parts[i] = $"<color=#FF4444>{shortName}:{score}</color>";
                else
                    parts[i] = $"{shortName}:{score}";
            }
            scoreText.text = string.Join("  ", parts);
            scoreText.richText = true;

            // Update player info panels
            UpdatePlayerInfoPanels();

            // Pulse score on update
            if (CardAnimator.Instance != null)
            {
                CardAnimator.Instance.PunchScale(scoreText.GetComponent<RectTransform>(), 0.15f);
            }
        }

        private string GetShortPlayerName(Player player)
        {
            // For display, use first 3 chars of name or position letter
            string name = player.PlayerName;
            if (string.IsNullOrEmpty(name) || name == "You")
                return "You";
            if (name.Length <= 5)
                return name;
            return name.Substring(0, 4);
        }

        private void UpdateRoundText()
        {
            roundText.text = $"ROUND {GameManager.Instance.RoundNumber}";
        }

        private void ClearPlayerHand()
        {
            foreach (var card in playerHandCards)
            {
                if (card != null)
                    Destroy(card.gameObject);
            }
            playerHandCards.Clear();
        }

        private void ClearContainer(RectTransform container)
        {
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Destroy(container.GetChild(i).gameObject);
            }
        }
    }
}
