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
        private Button emojiButton;
        private Image turnIndicator;
        private RectTransform[] playerIndicators = new RectTransform[4];

        [Header("Player Info Panels")]
        private Dictionary<PlayerPosition, PlayerInfoPanel> playerInfoPanels = new Dictionary<PlayerPosition, PlayerInfoPanel>();
        private ScoreSummaryPopup scoreSummaryPopup;
        private EmojiReactionSystem emojiSystem;

        // Emoji panel - managed directly by GameUI to avoid destruction issues
        private GameObject emojiPanelObj;
        private CanvasGroup emojiPanelCanvasGroup;
        private bool isEmojiPanelOpen = false;

        [Header("Card Settings")]
        private float cardWidth = 260f; // Much bigger cards for better visibility
        private float cardHeight = 370f;
        private float cardSpacing = 110f; // Wide spread to use full screen width

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
            const float checkInterval  = 2f;
            const float stuckThreshold = 5f;  // Basic recovery threshold
            const float deepStuck      = 15f; // Deep-stuck: force re-highlight / resync

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

                float timeSinceLastAction = Time.time - lastActionTime;
                if (timeSinceLastAction <= stuckThreshold) continue;

                Debug.LogWarning($"[Watchdog] Game stuck for {timeSinceLastAction:F1}s — recovering");

                // Reset blocking flags
                isProcessingPlay   = false;
                nextAllowedPlayTime = 0f;

                Player currentPlayer = GameManager.Instance.CurrentPlayer;
                bool   isLocalTurn   = GameManager.Instance.IsLocalPlayerTurn();

                if (isLocalTurn)
                {
                    Debug.Log("[Watchdog] Highlighting cards for local player");
                    HighlightPlayableCards();
                }
                else if (!GameManager.Instance.IsOnlineGame && !currentPlayer.IsHuman)
                {
                    Debug.Log($"[Watchdog] Triggering AI for {currentPlayer.PlayerName}");
                    AIPlayCard();
                }
                else if (GameManager.Instance.ShouldHostPlayForPosition(currentPlayer.Position))
                {
                    Debug.Log($"[Watchdog] HOST AI for disconnected {currentPlayer.PlayerName}");
                    AIPlayCard();
                }
                else if (GameManager.Instance.IsOnlineGame && timeSinceLastAction > deepStuck)
                {
                    // Deep-stuck in online game waiting for a remote player.
                    // Re-fire OnTrickStarted-equivalent to re-schedule the remote wait.
                    Debug.LogWarning($"[Watchdog] Deep stuck {timeSinceLastAction:F1}s — re-triggering turn setup for {currentPlayer.PlayerName}");
                    // Force re-highlight for local player or re-trigger AI if bot
                    if (NetworkGameSync.Instance?.IsHost == true &&
                        GameManager.Instance.ShouldHostPlayForPosition(currentPlayer.Position))
                    {
                        AIPlayCard();
                    }
                    else if (isLocalTurn)
                    {
                        HighlightPlayableCards();
                    }
                    else
                    {
                        // Nothing we can do; update instruction text so player knows we're waiting
                        if (instructionText != null)
                            instructionText.text = $"Waiting for {currentPlayer.PlayerName}...";
                    }
                }
                else
                {
                    Debug.Log($"[Watchdog] Online — waiting for remote player {currentPlayer.PlayerName}");
                }

                lastActionTime = Time.time;
            }
        }

        /// <summary>
        /// Unsubscribe from all events and destroy the GameCanvas.
        /// GameUI persists (DontDestroyOnLoad) but is reset for next game.
        /// Call Reinitialize() to set up for a new game.
        /// </summary>
        public void Cleanup()
        {
            Debug.Log($"[GameUI] Cleanup called! Stack trace:\n{System.Environment.StackTrace}");

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

            // Unsubscribe from disconnect events
            if (NetworkGameSync.Instance != null)
            {
                NetworkGameSync.Instance.OnPlayerDisconnectedUI -= OnPlayerDisconnectedUI;
                NetworkGameSync.Instance.OnPlayerReconnectedUI -= OnPlayerReconnectedUI;
                NetworkGameSync.Instance.OnBotReplacedUI -= OnBotReplacedUI;
            }

            // Unsubscribe from emoji events
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnEmojiReceived -= OnRemoteEmojiReceived;
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
            ResetGameplayState();
            CreateUI();
            SubscribeToEvents();
            if (watchdogCoroutine != null) StopCoroutine(watchdogCoroutine);
            watchdogCoroutine = StartCoroutine(WatchdogCoroutine());
        }

        /// <summary>
        /// Reset all gameplay state flags to prevent stale state between rounds/games.
        /// </summary>
        private void ResetGameplayState()
        {
            isPassPhase = false;
            isAnimating = false;
            isProcessingPlay = false;
            selectedForPass.Clear();
            lastActionTime = Time.time;
            nextAllowedPlayTime = 0f;
            StopTransientCoroutines();
        }

        private Coroutine emojiAutoCloseCoroutine = null;
        private Coroutine delayedClearTrickCoroutine = null;
        private Coroutine trickWinnerHighlightCoroutine = null;
        private Coroutine delayedHighlightCoroutine = null;
        private Coroutine delayedStartNextRoundCoroutine = null;

        /// <summary>
        /// Stop all transient coroutines that could interfere with round transitions.
        /// </summary>
        private void StopTransientCoroutines()
        {
            if (emojiAutoCloseCoroutine != null) { StopCoroutine(emojiAutoCloseCoroutine); emojiAutoCloseCoroutine = null; }
            if (delayedClearTrickCoroutine != null) { StopCoroutine(delayedClearTrickCoroutine); delayedClearTrickCoroutine = null; }
            if (trickWinnerHighlightCoroutine != null) { StopCoroutine(trickWinnerHighlightCoroutine); trickWinnerHighlightCoroutine = null; }
            if (delayedHighlightCoroutine != null) { StopCoroutine(delayedHighlightCoroutine); delayedHighlightCoroutine = null; }
            if (delayedStartNextRoundCoroutine != null) { StopCoroutine(delayedStartNextRoundCoroutine); delayedStartNextRoundCoroutine = null; }
            if (pendingAIPlayCoroutine != null) { StopCoroutine(pendingAIPlayCoroutine); pendingAIPlayCoroutine = null; }
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
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 50));

            // CLEAN LAYOUT: Position hand containers to avoid overlaps
            // South (human) - lower third of screen, cards at very bottom
            playerHandContainer = CreateContainer(canvasTransform, "PlayerHand",
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 30));

            // Create trick area (center of table)
            trickArea = CreateContainer(canvasTransform, "TrickArea",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);

            // West - left side, card backs area
            leftHandContainer = CreateContainer(canvasTransform, "LeftHand",
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(180, -40));

            // North (partner) - top area, card backs
            topHandContainer = CreateContainer(canvasTransform, "TopHand",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -180));

            // East - right side, card backs area
            rightHandContainer = CreateContainer(canvasTransform, "RightHand",
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-180, -40));

            // Create turn indicators for each position
            CreateTurnIndicators(canvasTransform);

            // Create instruction text - CENTER OF TABLE
            instructionText = CreateInstructionText(canvasTransform);

            // Create round text - TOP LEFT corner
            roundText = CreateRoundText(canvasTransform);

            // Score tracker (hidden - scores shown in player panels and popup)
            CreateScoreTracker(canvasTransform);

            // Create buttons - START hidden, PASS centered on table
            startButton = CreateStyledButton(canvasTransform, "StartButton",
                new Vector2(0.5f, 0.5f), "Start Game", OnStartClicked);
            startButton.gameObject.SetActive(false); // Hidden - MainMenu handles starting

            // Pass button - CENTER OF TABLE
            passButton = CreateStyledButton(canvasTransform, "PassButton",
                new Vector2(0.5f, 0.5f), "Pass Cards (0/3)", OnPassClicked);
            passButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 40); // Slightly above center
            passButton.gameObject.SetActive(false);

            // Create pause button (top right corner)
            pauseButton = CreatePauseButton(canvasTransform);

            // Create score summary button (next to pause) - opens score popup
            scoreButton = CreateScoreButton(canvasTransform);

            // Create emoji button (under round label) - opens emoji panel
            emojiButton = CreateEmojiButton(canvasTransform);

            // Create score summary popup
            scoreSummaryPopup = ScoreSummaryPopup.Create(canvasTransform);

            // Create emoji panel directly in GameUI (no separate component - avoids destruction issues)
            CreateEmojiPanel(canvasTransform);

            // Create disconnect notification banner (centered, for online games)
            DisconnectNotification.Create(canvasTransform);

            // Create ping display (top right, always visible in online games)
            PingDisplay.Create(canvasTransform);

            // Create special card effect system
            Debug.Log("[GameUI] Creating SpecialCardEffect...");
            GameObject specialEffectObj = new GameObject("SpecialCardEffect");
            specialEffectObj.transform.SetParent(canvasTransform, false);
            SpecialCardEffect specialEffect = specialEffectObj.AddComponent<SpecialCardEffect>();
            specialEffect.Initialize(mainCanvas);
            Debug.Log($"[GameUI] SpecialCardEffect created. Instance={SpecialCardEffect.Instance != null}");

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

            // Create panel for each player with visual position mapping
            foreach (var player in GameManager.Instance.Players)
            {
                if (player != null)
                {
                    var visualPos = GameManager.Instance.GetVisualPosition(player.Position);
                    var panel = PlayerInfoPanel.Create(canvasTransform, player, player.Position, visualPos);
                    playerInfoPanels[player.Position] = panel;
                    Debug.Log($"[CreatePlayerInfoPanels] Created panel for {player.PlayerName} at {player.Position} (visual: {visualPos})");
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
            rect.anchoredPosition = new Vector2(-130, -50); // Top right, next to pause (moved in from edge)
            rect.sizeDelta = new Vector2(50, 50);

            Image img = btnObj.AddComponent<Image>();

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;

            // Modern glass button with cyan accent
            Color accentCyan = new Color(0.40f, 0.75f, 1f, 1f);
            if (ModernUITheme.Instance != null && ModernUITheme.Instance.CircleSprite != null)
            {
                img.sprite = ModernUITheme.Instance.CircleSprite;
                img.color = new Color(0.16f, 0.14f, 0.28f, 0.95f);

                ColorBlock colors = btn.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
                colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
                colors.selectedColor = Color.white;
                btn.colors = colors;

                Shadow shadow = btnObj.AddComponent<Shadow>();
                shadow.effectColor = new Color(0.40f, 0.75f, 1f, 0.3f);
                shadow.effectDistance = new Vector2(0, -2);

                Outline outline = btnObj.AddComponent<Outline>();
                outline.effectColor = new Color(0.40f, 0.75f, 1f, 0.5f);
                outline.effectDistance = new Vector2(1, -1);
            }
            else
            {
                img.color = accentCyan;
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
            iconTmp.color = new Color(0.40f, 0.75f, 1f, 1f);
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
            rect.anchoredPosition = new Vector2(-70, -50); // Top right corner (moved in from edge)
            rect.sizeDelta = new Vector2(50, 50);

            Image img = btnObj.AddComponent<Image>();

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;

            // Modern glass button with magenta accent
            Color accentMagenta = new Color(0.85f, 0.45f, 0.95f, 1f);
            if (ModernUITheme.Instance != null && ModernUITheme.Instance.CircleSprite != null)
            {
                img.sprite = ModernUITheme.Instance.CircleSprite;
                img.color = new Color(0.16f, 0.14f, 0.28f, 0.95f);

                ColorBlock colors = btn.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
                colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
                colors.selectedColor = Color.white;
                btn.colors = colors;

                Shadow shadow = btnObj.AddComponent<Shadow>();
                shadow.effectColor = new Color(0.85f, 0.45f, 0.95f, 0.3f);
                shadow.effectDistance = new Vector2(0, -2);

                Outline outline = btnObj.AddComponent<Outline>();
                outline.effectColor = new Color(0.85f, 0.45f, 0.95f, 0.5f);
                outline.effectDistance = new Vector2(1, -1);
            }
            else
            {
                img.color = accentMagenta;
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
            iconTmp.color = accentMagenta;
            iconTmp.fontStyle = FontStyles.Bold;

            return btn;
        }

        private Button CreateEmojiButton(Transform parent)
        {
            // EXACT SAME PATTERN AS PAUSE BUTTON - THIS WILL WORK
            GameObject btnObj = new GameObject("EmojiButton");
            btnObj.transform.SetParent(parent, false);

            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1); // TOP LEFT (under round label)
            rect.anchorMax = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(150, -95); // Under Round label, pushed right to avoid camera
            rect.sizeDelta = new Vector2(50, 50);

            Image img = btnObj.AddComponent<Image>();

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;

            // Yellow/gold accent for emoji button
            Color accentGold = new Color(1f, 0.85f, 0.3f, 1f);
            if (ModernUITheme.Instance != null && ModernUITheme.Instance.CircleSprite != null)
            {
                img.sprite = ModernUITheme.Instance.CircleSprite;
                img.color = new Color(0.2f, 0.25f, 0.45f, 0.98f); // Dark blue background

                ColorBlock colors = btn.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
                colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
                colors.selectedColor = Color.white;
                btn.colors = colors;

                Shadow shadow = btnObj.AddComponent<Shadow>();
                shadow.effectColor = new Color(1f, 0.85f, 0.3f, 0.3f);
                shadow.effectDistance = new Vector2(0, -2);

                Outline outline = btnObj.AddComponent<Outline>();
                outline.effectColor = new Color(1f, 0.85f, 0.3f, 0.7f);
                outline.effectDistance = new Vector2(2, -2);
            }
            else
            {
                img.color = accentGold;
            }

            btn.onClick.AddListener(() => {
                Debug.Log("[GameUI] Emoji btn clicked - toggling panel directly");
                SoundManager.Instance?.PlayButtonClick();
                ToggleEmojiPanel();
            });

            // Plus icon
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(btnObj.transform, false);

            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI iconTmp = iconObj.AddComponent<TextMeshProUGUI>();
            iconTmp.text = "+";
            iconTmp.fontSize = 28;
            iconTmp.alignment = TextAlignmentOptions.Center;
            iconTmp.color = accentGold;
            iconTmp.fontStyle = FontStyles.Bold;
            iconTmp.raycastTarget = false;

            Debug.Log($"[GameUI] Emoji button created at {rect.anchoredPosition}");

            return btn;
        }

        /// <summary>
        /// Create the emoji panel directly in GameUI (avoids separate component destruction issues)
        /// </summary>
        private void CreateEmojiPanel(Transform parent)
        {
            emojiPanelObj = new GameObject("EmojiPanel");
            emojiPanelObj.transform.SetParent(parent, false);
            emojiPanelObj.transform.SetAsLastSibling(); // Render on top

            RectTransform panelRect = emojiPanelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(230, 400);
            panelRect.anchoredPosition = Vector2.zero;

            emojiPanelCanvasGroup = emojiPanelObj.AddComponent<CanvasGroup>();
            emojiPanelCanvasGroup.alpha = 0;
            emojiPanelCanvasGroup.blocksRaycasts = false;
            emojiPanelCanvasGroup.interactable = false;

            // Dark glassmorphism background
            Image panelBg = emojiPanelObj.AddComponent<Image>();
            panelBg.color = new Color(0.08f, 0.10f, 0.15f, 0.95f);
            if (ModernUITheme.Instance != null && ModernUITheme.Instance.GlassPanelDarkSprite != null)
            {
                panelBg.sprite = ModernUITheme.Instance.GlassPanelDarkSprite;
                panelBg.type = Image.Type.Sliced;
            }

            // Grid layout for emoji buttons
            GameObject layoutObj = new GameObject("EmojiLayout");
            layoutObj.transform.SetParent(emojiPanelObj.transform, false);
            RectTransform layoutRect = layoutObj.AddComponent<RectTransform>();
            layoutRect.anchorMin = Vector2.zero;
            layoutRect.anchorMax = Vector2.one;
            layoutRect.sizeDelta = Vector2.zero;
            layoutRect.anchoredPosition = Vector2.zero;

            GridLayoutGroup layout = layoutObj.AddComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(60, 60);
            layout.spacing = new Vector2(8, 8);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = 3;
            layout.padding = new RectOffset(10, 10, 10, 10);

            // Create emoji buttons
            var reactions = new (string name, Color color)[]
            {
                ("laugh", new Color(1f, 0.85f, 0.2f)),
                ("angry", new Color(0.95f, 0.3f, 0.3f)),
                ("clap", new Color(0.3f, 0.9f, 0.4f)),
                ("sad", new Color(0.4f, 0.6f, 0.95f)),
                ("cool", new Color(0.2f, 0.8f, 0.9f)),
                ("fire", new Color(1f, 0.5f, 0.2f)),
                ("heart_broken", new Color(0.9f, 0.4f, 0.6f)),
                ("party", new Color(0.7f, 0.4f, 0.95f)),
                ("thumbsup", new Color(0.3f, 0.7f, 1f)),
                ("wow", new Color(1f, 0.85f, 0.2f)),
                ("love", new Color(0.95f, 0.3f, 0.5f)),
                ("cry", new Color(0.4f, 0.6f, 0.95f)),
                ("skull", new Color(0.7f, 0.7f, 0.75f)),
                ("pray", new Color(0.95f, 0.75f, 0.5f)),
                ("rocket", new Color(0.4f, 0.5f, 1f))
            };

            foreach (var reaction in reactions)
            {
                CreateEmojiReactionButton(layoutObj.transform, reaction.name, reaction.color);
            }

            emojiPanelObj.SetActive(false);
            Debug.Log("[GameUI] Emoji panel created directly in GameUI");
        }

        private void CreateEmojiReactionButton(Transform parent, string emojiName, Color accentColor)
        {
            GameObject btnObj = new GameObject($"Reaction_{emojiName}");
            btnObj.transform.SetParent(parent, false);

            RectTransform btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.sizeDelta = new Vector2(60, 60);

            // Background with accent color
            Image bg = btnObj.AddComponent<Image>();
            bg.color = new Color(accentColor.r * 0.3f, accentColor.g * 0.3f, accentColor.b * 0.3f, 0.95f);
            bg.raycastTarget = true;
            if (ModernUITheme.Instance != null && ModernUITheme.Instance.CircleSprite != null)
            {
                bg.sprite = ModernUITheme.Instance.CircleSprite;
            }

            // Colored outline
            Outline outline = btnObj.AddComponent<Outline>();
            outline.effectColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0.9f);
            outline.effectDistance = new Vector2(2, -2);

            // Emoji sprite
            GameObject spriteObj = new GameObject("EmojiSprite");
            spriteObj.transform.SetParent(btnObj.transform, false);
            RectTransform spriteRect = spriteObj.AddComponent<RectTransform>();
            spriteRect.anchorMin = new Vector2(0.1f, 0.1f);
            spriteRect.anchorMax = new Vector2(0.9f, 0.9f);
            spriteRect.sizeDelta = Vector2.zero;
            spriteRect.anchoredPosition = Vector2.zero;

            Image emojiImage = spriteObj.AddComponent<Image>();
            emojiImage.sprite = EmojiReactionSystem.GetEmojiSprite(emojiName);
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

        private void OnEmojiSelected(string emoji)
        {
            Debug.Log($"[GameUI] Emoji selected: {emoji}");
            SoundManager.Instance?.PlayButtonClick();

            // Show emoji for the local player
            Player localPlayer = GameManager.Instance?.GetHumanPlayer();
            PlayerPosition localPos = localPlayer?.Position ?? PlayerPosition.South;
            Debug.Log($"[GameUI] Local player pos: {localPos}, IsOnline: {GameManager.Instance?.IsOnlineGame}");
            if (playerInfoPanels.TryGetValue(localPos, out PlayerInfoPanel panel))
            {
                panel.ShowEmoji(emoji);
            }

            // Send emoji to other players in online game
            if (GameManager.Instance != null && GameManager.Instance.IsOnlineGame &&
                NetworkManager.Instance != null)
            {
                Debug.Log($"[GameUI] Sending emoji '{emoji}' from position {localPos} to server");
                NetworkManager.Instance.SendEmojiReaction(emoji, localPos.ToString());
            }

            CloseEmojiPanel();
        }

        private void ToggleEmojiPanel()
        {
            if (isEmojiPanelOpen)
                CloseEmojiPanel();
            else
                OpenEmojiPanel();
        }

        private void OpenEmojiPanel()
        {
            if (emojiPanelObj == null)
            {
                Debug.LogError("[GameUI] emojiPanelObj is null!");
                return;
            }
            if (isEmojiPanelOpen) return;

            isEmojiPanelOpen = true;
            emojiPanelObj.SetActive(true);
            emojiPanelObj.transform.SetAsLastSibling(); // Ensure on top

            if (emojiPanelCanvasGroup != null)
            {
                emojiPanelCanvasGroup.alpha = 1;
                emojiPanelCanvasGroup.blocksRaycasts = true;
                emojiPanelCanvasGroup.interactable = true;
            }

            Debug.Log("[GameUI] Emoji panel opened");

            // Auto-close after 5 seconds
            if (emojiAutoCloseCoroutine != null) StopCoroutine(emojiAutoCloseCoroutine);
            emojiAutoCloseCoroutine = StartCoroutine(AutoCloseEmojiPanel());
        }

        private void CloseEmojiPanel()
        {
            if (!isEmojiPanelOpen) return;

            isEmojiPanelOpen = false;

            if (emojiPanelCanvasGroup != null)
            {
                emojiPanelCanvasGroup.alpha = 0;
                emojiPanelCanvasGroup.blocksRaycasts = false;
                emojiPanelCanvasGroup.interactable = false;
            }

            if (emojiPanelObj != null)
            {
                emojiPanelObj.SetActive(false);
            }

            Debug.Log("[GameUI] Emoji panel closed");
        }

        private System.Collections.IEnumerator AutoCloseEmojiPanel()
        {
            yield return new WaitForSeconds(5f);
            CloseEmojiPanel();
        }

        private void CreateBackground(Transform parent)
        {
            // Elegant gradient background - deep purple to dark teal

            // Base layer - modern deep navy to purple gradient
            GameObject baseBg = new GameObject("BaseBackground");
            baseBg.transform.SetParent(parent, false);
            Image baseImg = baseBg.AddComponent<Image>();
            baseImg.sprite = CreateVerticalGradientSprite(64, new Color(0.06f, 0.08f, 0.14f, 1f), new Color(0.12f, 0.08f, 0.22f, 1f));
            RectTransform baseRect = baseBg.GetComponent<RectTransform>();
            baseRect.anchorMin = Vector2.zero;
            baseRect.anchorMax = Vector2.one;
            baseRect.sizeDelta = Vector2.zero;
            baseImg.raycastTarget = false;

            // Corner vignette overlay for depth
            GameObject vignetteObj = new GameObject("Vignette");
            vignetteObj.transform.SetParent(parent, false);
            Image vignetteImg = vignetteObj.AddComponent<Image>();
            vignetteImg.sprite = CreateVignetteSprite(128);
            vignetteImg.color = new Color(0, 0, 0, 0.4f);
            RectTransform vignetteRect = vignetteObj.GetComponent<RectTransform>();
            vignetteRect.anchorMin = Vector2.zero;
            vignetteRect.anchorMax = Vector2.one;
            vignetteRect.sizeDelta = Vector2.zero;
            vignetteImg.raycastTarget = false;

            // Create modern card table
            CreateModernTable(parent);
        }

        /// <summary>
        /// Create vertical gradient sprite (top to bottom)
        /// </summary>
        private Sprite CreateVerticalGradientSprite(int height, Color topColor, Color bottomColor)
        {
            Texture2D tex = new Texture2D(1, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            for (int y = 0; y < height; y++)
            {
                float t = (float)y / (height - 1);
                Color c = Color.Lerp(bottomColor, topColor, t);
                tex.SetPixel(0, y, c);
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, height), new Vector2(0.5f, 0.5f));
        }

        /// <summary>
        /// Create vignette sprite (dark corners, lighter center)
        /// </summary>
        private Sprite CreateVignetteSprite(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Vector2 center = new Vector2(size / 2f, size / 2f);
            float maxDist = center.magnitude;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center) / maxDist;
                    float alpha = Mathf.Pow(dist, 1.5f); // Gentle falloff
                    tex.SetPixel(x, y, new Color(0, 0, 0, alpha));
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        /// <summary>
        /// Create a modern, inviting card table with elegant design
        /// </summary>
        private void CreateModernTable(Transform parent)
        {
            // Larger table dimensions for better visibility
            float tableWidth = 1600f;
            float tableHeight = 900f;

            // Wood frame/border - warm mahogany
            GameObject frameObj = new GameObject("TableFrame");
            frameObj.transform.SetParent(parent, false);
            RectTransform frameRect = frameObj.AddComponent<RectTransform>();
            frameRect.anchorMin = new Vector2(0.5f, 0.5f);
            frameRect.anchorMax = new Vector2(0.5f, 0.5f);
            frameRect.sizeDelta = new Vector2(tableWidth + 40, tableHeight + 40);
            frameRect.anchoredPosition = Vector2.zero;

            Image frameImg = frameObj.AddComponent<Image>();
            frameImg.sprite = CreateRoundedRectSprite(128, 80, 20);
            frameImg.type = Image.Type.Sliced;
            frameImg.color = new Color(0.12f, 0.10f, 0.22f, 0.95f); // Modern dark purple frame
            frameImg.raycastTarget = false;

            // Subtle shadow
            Shadow frameShadow = frameObj.AddComponent<Shadow>();
            frameShadow.effectColor = new Color(0, 0, 0, 0.4f);
            frameShadow.effectDistance = new Vector2(0, -4);

            // Cyan glow trim accent
            GameObject trimObj = new GameObject("CyanTrim");
            trimObj.transform.SetParent(parent, false);
            RectTransform trimRect = trimObj.AddComponent<RectTransform>();
            trimRect.anchorMin = new Vector2(0.5f, 0.5f);
            trimRect.anchorMax = new Vector2(0.5f, 0.5f);
            trimRect.sizeDelta = new Vector2(tableWidth + 16, tableHeight + 16);
            trimRect.anchoredPosition = Vector2.zero;

            Image trimImg = trimObj.AddComponent<Image>();
            trimImg.sprite = CreateRoundedRectSprite(128, 80, 18);
            trimImg.type = Image.Type.Sliced;
            trimImg.color = new Color(0.40f, 0.75f, 1f, 0.20f); // Subtle cyan accent glow
            trimImg.raycastTarget = false;

            // Main table surface - modern dark glass
            GameObject tableObj = new GameObject("TableSurface");
            tableObj.transform.SetParent(parent, false);
            RectTransform tableRect = tableObj.AddComponent<RectTransform>();
            tableRect.anchorMin = new Vector2(0.5f, 0.5f);
            tableRect.anchorMax = new Vector2(0.5f, 0.5f);
            tableRect.sizeDelta = new Vector2(tableWidth, tableHeight);
            tableRect.anchoredPosition = Vector2.zero;

            Image tableImg = tableObj.AddComponent<Image>();
            tableImg.sprite = CreateRoundedRectSprite(128, 80, 16);
            tableImg.type = Image.Type.Sliced;
            tableImg.color = new Color(0.08f, 0.10f, 0.16f, 0.92f); // Modern dark glass surface
            tableImg.raycastTarget = false;

            // Center gradient highlight - cyan/magenta glow
            GameObject highlightObj = new GameObject("TableHighlight");
            highlightObj.transform.SetParent(parent, false);
            RectTransform highlightRect = highlightObj.AddComponent<RectTransform>();
            highlightRect.anchorMin = new Vector2(0.5f, 0.5f);
            highlightRect.anchorMax = new Vector2(0.5f, 0.5f);
            highlightRect.sizeDelta = new Vector2(tableWidth - 200, tableHeight - 150);
            highlightRect.anchoredPosition = new Vector2(0, 20); // Slightly offset up

            Image highlightImg = highlightObj.AddComponent<Image>();
            highlightImg.sprite = CreateSoftGlowSprite(128);
            highlightImg.color = new Color(0.30f, 0.50f, 0.85f, 0.12f); // Subtle cyan/blue glow
            highlightImg.raycastTarget = false;
        }

        /// <summary>
        /// Create a rounded rectangle sprite
        /// </summary>
        private Sprite CreateRoundedRectSprite(int width, int height, int cornerRadius)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool inside = IsInsideRoundedRect(x, y, width, height, cornerRadius);
                    tex.SetPixel(x, y, inside ? Color.white : Color.clear);
                }
            }

            tex.Apply();

            // Create sliced sprite with proper border for 9-slicing
            int border = cornerRadius + 2;
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100, 0,
                SpriteMeshType.FullRect, new Vector4(border, border, border, border));
        }

        private bool IsInsideRoundedRect(int x, int y, int width, int height, int radius)
        {
            // Check corners
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

        /// <summary>
        /// Create a soft radial glow sprite
        /// </summary>
        private Sprite CreateSoftGlowSprite(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Vector2 center = new Vector2(size / 2f, size / 2f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center) / (size / 2f);
                    float alpha = Mathf.Max(0, 1 - dist * dist); // Quadratic falloff
                    tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        // Keep legacy oval method for compatibility but not used
        private void CreateOvalTable(Transform parent)
        {
            // Redirected to modern table design
            CreateModernTable(parent);
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
            // Create elegant modern frame around the table
            float frameThickness = 18f;
            Color frameMain = new Color(0.12f, 0.10f, 0.22f, 0.95f);
            Color frameHighlight = new Color(0.18f, 0.16f, 0.30f, 1f);
            Color accentTrim = new Color(0.40f, 0.75f, 1f, 0.4f); // Cyan accent

            // Outer dark frame
            CreateFrameEdge(parent, "TopFrame", new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0.5f, 1), new Vector2(0, frameThickness), Vector2.zero, frameMain, true);

            CreateFrameEdge(parent, "BottomFrame", new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(0.5f, 0), new Vector2(0, frameThickness), Vector2.zero, frameMain * 0.85f, false);

            CreateFrameEdge(parent, "LeftFrame", new Vector2(0, 0), new Vector2(0, 1),
                new Vector2(0, 0.5f), new Vector2(frameThickness, 0), Vector2.zero, frameMain * 0.9f, false);

            CreateFrameEdge(parent, "RightFrame", new Vector2(1, 0), new Vector2(1, 1),
                new Vector2(1, 0.5f), new Vector2(frameThickness, 0), Vector2.zero, frameMain * 0.95f, false);

            // Inner cyan trim line
            float trimOffset = frameThickness - 3f;
            float trimWidth = 2f;

            CreateTrimLine(parent, "TopTrim", new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0.5f, 1), new Vector2(-frameThickness * 2, trimWidth), new Vector2(0, -trimOffset), accentTrim);

            CreateTrimLine(parent, "BottomTrim", new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(0.5f, 0), new Vector2(-frameThickness * 2, trimWidth), new Vector2(0, trimOffset), accentTrim);

            CreateTrimLine(parent, "LeftTrim", new Vector2(0, 0), new Vector2(0, 1),
                new Vector2(0, 0.5f), new Vector2(trimWidth, -frameThickness * 2), new Vector2(trimOffset, 0), accentTrim);

            CreateTrimLine(parent, "RightTrim", new Vector2(1, 0), new Vector2(1, 1),
                new Vector2(1, 0.5f), new Vector2(trimWidth, -frameThickness * 2), new Vector2(-trimOffset, 0), accentTrim);

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
                hImg.color = new Color(0.40f, 0.75f, 1f, 0.3f); // Cyan highlight
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
            img.color = new Color(0.12f, 0.10f, 0.22f, 0.95f); // Modern dark purple
            img.raycastTarget = false;

            // Cyan dot in corner
            GameObject dotObj = new GameObject("CyanDot");
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
            dotImg.color = new Color(0.40f, 0.75f, 1f, 0.8f); // Cyan accent
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

        private void CreateScoreTracker(Transform parent)
        {
            // Hidden score tracker - scores are shown in popup and player panels
            GameObject textObj = new GameObject("ScoreTracker");
            textObj.transform.SetParent(parent, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.zero;
            textRect.sizeDelta = Vector2.zero;

            scoreText = textObj.AddComponent<TextMeshProUGUI>();
            scoreText.text = "";
            scoreText.fontSize = 1;
            scoreText.gameObject.SetActive(false); // Hidden - just for tracking
        }

        private TextMeshProUGUI CreateInstructionText(Transform parent)
        {
            // Instruction panel - TOP LEFT, next to Round indicator
            GameObject bgObj = new GameObject("Instructions_BG");
            bgObj.transform.SetParent(parent, false);
            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 1f);
            bgRect.anchorMax = new Vector2(0f, 1f);
            bgRect.anchoredPosition = new Vector2(300, -50); // Next to Round indicator
            bgRect.sizeDelta = new Vector2(350, 36);

            Image bgImg = bgObj.AddComponent<Image>();
            if (ModernUITheme.Instance != null && ModernUITheme.Instance.GlassPanelDarkSprite != null)
            {
                bgImg.sprite = ModernUITheme.Instance.GlassPanelDarkSprite;
                bgImg.type = Image.Type.Sliced;
            }
            bgImg.color = new Color(0.10f, 0.12f, 0.20f, 0.85f); // Modern dark glass

            GameObject textObj = new GameObject("Instructions");
            textObj.transform.SetParent(bgObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "";
            tmp.fontSize = 18;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = ModernUITheme.TextPrimary;

            return tmp;
        }

        private TextMeshProUGUI CreateRoundText(Transform parent)
        {
            // Round indicator - TOP LEFT CORNER (moved in from edge)
            GameObject bgObj = new GameObject("Round_BG");
            bgObj.transform.SetParent(parent, false);
            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 1f);
            bgRect.anchorMax = new Vector2(0f, 1f);
            bgRect.anchoredPosition = new Vector2(90, -50); // More inward to stay on screen
            bgRect.sizeDelta = new Vector2(120, 36);

            Image bgImg = bgObj.AddComponent<Image>();
            if (ModernUITheme.Instance != null && ModernUITheme.Instance.GlassPanelDarkSprite != null)
            {
                bgImg.sprite = ModernUITheme.Instance.GlassPanelDarkSprite;
                bgImg.type = Image.Type.Sliced;
            }
            bgImg.color = new Color(0.10f, 0.12f, 0.20f, 0.90f); // Modern dark glass
            bgImg.raycastTarget = false; // Prevent blocking emoji button clicks

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
            tmp.color = new Color(0.40f, 0.75f, 1f, 1f); // Modern cyan accent

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

                // Show ping display if online
                if (PingDisplay.Instance != null)
                    PingDisplay.Instance.AutoDetect();

                // Subscribe to disconnect UI events for online games
                if (NetworkGameSync.Instance != null)
                {
                    NetworkGameSync.Instance.OnPlayerDisconnectedUI += OnPlayerDisconnectedUI;
                    NetworkGameSync.Instance.OnPlayerReconnectedUI += OnPlayerReconnectedUI;
                    NetworkGameSync.Instance.OnBotReplacedUI += OnBotReplacedUI;
                }

                // Subscribe to emoji reactions from other players
                if (NetworkManager.Instance != null)
                {
                    NetworkManager.Instance.OnEmojiReceived += OnRemoteEmojiReceived;
                }
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

                    // Notify GameManager that local player has submitted their pass
                    GameManager.Instance.NotifyLocalPassSubmitted();

                    // Hide pass button and wait for all players to pass
                    selectedForPass.Clear();
                    isPassPhase = false;
                    passButton.gameObject.SetActive(false);

                    // Refresh display (cards removed from hand)
                    DisplayPlayerHandAnimated();
                    instructionText.text = "Waiting for other players to pass...";
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
            // Reset gameplay state for new round
            isPassPhase = false;
            isProcessingPlay = false;
            selectedForPass.Clear();
            nextAllowedPlayTime = 0f;
            StopTransientCoroutines();
            ClearTrickArea();

            // Recreate player info panels to ensure correct visual positions
            // (online game config may have been set after initial panel creation)
            CreatePlayerInfoPanels();

            // Show ping display for online games
            if (PingDisplay.Instance != null)
            {
                bool isOnline = (GameManager.Instance != null && GameManager.Instance.LocalPlayerPosition.HasValue)
                    || (NetworkGameSync.Instance != null && NetworkGameSync.Instance.IsOnlineGame);
                if (isOnline)
                    PingDisplay.Instance.Show();
            }

            // Clear history when starting a new game (round 1)
            if (GameManager.Instance?.RoundNumber == 1 && scoreSummaryPopup != null)
            {
                scoreSummaryPopup.ClearHistory();
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
            // Update player info panels
            UpdatePlayerInfoPanels();

            // Display small card backs for opponents
            DisplayOpponentCardBacks();
        }

        /// <summary>
        /// Display small card backs for opponent players (shows card count visually)
        /// </summary>
        private void DisplayOpponentCardBacks()
        {
            // Clear old card backs
            ClearContainer(leftHandContainer);
            ClearContainer(topHandContainer);
            ClearContainer(rightHandContainer);

            if (GameManager.Instance == null || GameManager.Instance.Players == null) return;

            // Card back size (smaller than player cards)
            float backWidth = 45f;
            float backHeight = 65f;
            float overlap = 12f; // Heavy overlap for compact look

            // Get local player to skip (works for both online and offline)
            Player localPlayer = GameManager.Instance.GetHumanPlayer();

            foreach (var player in GameManager.Instance.Players)
            {
                // Skip the local player (their cards are shown face-up at bottom)
                if (player == localPlayer) continue;

                // Use visual position to choose which container
                PlayerPosition visualPos = GameManager.Instance.GetVisualPosition(player.Position);

                RectTransform container = visualPos switch
                {
                    PlayerPosition.West => leftHandContainer,
                    PlayerPosition.North => topHandContainer,
                    PlayerPosition.East => rightHandContainer,
                    _ => null
                };

                if (container == null) continue;

                int cardCount = player.Hand.Count;
                float totalWidth = (cardCount - 1) * overlap + backWidth;
                float startX = -totalWidth / 2 + backWidth / 2;

                // Create card backs (limit display to avoid clutter)
                int displayCount = Mathf.Min(cardCount, 13);
                for (int i = 0; i < displayCount; i++)
                {
                    CreateCardBack(container, backWidth, backHeight, startX + i * overlap, visualPos);
                }
            }
        }

        /// <summary>
        /// Create a single card back image
        /// </summary>
        private void CreateCardBack(RectTransform parent, float width, float height, float xPos, PlayerPosition position)
        {
            GameObject backObj = new GameObject("CardBack");
            backObj.transform.SetParent(parent, false);

            RectTransform rect = backObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);

            // Position based on player position (horizontal or vertical layout)
            if (position == PlayerPosition.North)
            {
                rect.anchoredPosition = new Vector2(xPos, 0);
            }
            else if (position == PlayerPosition.West)
            {
                // Rotate for left side - vertical stack
                rect.anchoredPosition = new Vector2(0, xPos);
                rect.localRotation = Quaternion.Euler(0, 0, 90);
            }
            else if (position == PlayerPosition.East)
            {
                // Rotate for right side - vertical stack
                rect.anchoredPosition = new Vector2(0, -xPos);
                rect.localRotation = Quaternion.Euler(0, 0, -90);
            }

            Image img = backObj.AddComponent<Image>();

            // Try to load card back sprite, fallback to color
            Sprite cardBackSprite = Resources.Load<Sprite>("Cards/CardBack");
            if (cardBackSprite != null)
            {
                img.sprite = cardBackSprite;
                img.color = Color.white;
            }
            else
            {
                // Fallback: dark card back with pattern
                img.color = new Color(0.15f, 0.25f, 0.45f, 1f); // Dark blue
            }

            img.raycastTarget = false;

            // Add subtle shadow
            Shadow shadow = backObj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.4f);
            shadow.effectDistance = new Vector2(1, -1);
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
                        HapticManager.Instance?.LightTap();
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

            // Play sound and haptic
            SoundManager.Instance?.PlayCardPlay();
            HapticManager.Instance?.MediumTap();

            // Check for special cards and play dramatic effect
            Debug.Log($"[OnCardPlayed] Checking special cards: {card.Suit} {card.Rank}, IsQueenOfSpades={card.IsQueenOfSpades()}, SpecialCardEffect.Instance={SpecialCardEffect.Instance != null}");

            // Ensure SpecialCardEffect exists (create if missing)
            if (SpecialCardEffect.Instance == null && mainCanvas != null)
            {
                Debug.Log("[OnCardPlayed] SpecialCardEffect.Instance is null! Creating it now...");
                GameObject specialEffectObj = new GameObject("SpecialCardEffect");
                specialEffectObj.transform.SetParent(canvasTransform, false);
                SpecialCardEffect specialEffect = specialEffectObj.AddComponent<SpecialCardEffect>();
                specialEffect.Initialize(mainCanvas);
                Debug.Log($"[OnCardPlayed] SpecialCardEffect created. Instance now: {SpecialCardEffect.Instance != null}");
            }

            if (card.IsQueenOfSpades())
            {
                // Queen of Spades (+2 points) - intense effect
                Debug.Log("[OnCardPlayed] >>> QUEEN OF SPADES DETECTED! Playing effect...");
                SpecialCardEffect.Instance?.PlayQueenOfSpadesEffect();
            }
            else if (card.Suit == Suit.Diamonds && card.Rank == Rank.Ten)
            {
                // 10 of Diamonds (0 points) - special effect
                Debug.Log("[OnCardPlayed] >>> 10 OF DIAMONDS DETECTED! Playing effect...");
                SpecialCardEffect.Instance?.PlayTenOfDiamondsEffect();
            }

            // Particle effects disabled - they were causing visual flickering

            // Show card in trick area
            CardUI cardUI = CreateCardUI(card, trickArea);
            trickCards.Add(cardUI);

            // Spread cards around center - each player's card in distinct position
            // Use visual position so cards come from the correct screen direction
            PlayerPosition visualPos = GameManager.Instance.GetVisualPosition(player.Position);
            float spreadDistance = 120f; // Distance from center for each card
            Vector2 pos = visualPos switch
            {
                // Each card gets its own position based on visual seat
                PlayerPosition.South => new Vector2(0, -spreadDistance),      // Below center
                PlayerPosition.West => new Vector2(-spreadDistance - 30, 0),  // Left of center
                PlayerPosition.North => new Vector2(0, spreadDistance),       // Above center
                PlayerPosition.East => new Vector2(spreadDistance + 30, 0),   // Right of center
                _ => Vector2.zero
            };

            cardUI.SetOriginalPosition(pos);

            // Animate card to position with callback when done
            if (CardAnimator.Instance != null)
            {
                RectTransform cardRect = cardUI.GetComponent<RectTransform>();
                Vector2 startPos = GetPlayerStartPosition(visualPos);
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
                    if (delayedHighlightCoroutine != null) StopCoroutine(delayedHighlightCoroutine);
                    delayedHighlightCoroutine = StartCoroutine(DelayedHighlightCards(delay));
                }
                else if (!GameManager.Instance.IsOnlineGame)
                {
                    // It's AI's turn in local game - schedule AI play after cooldown
                    Debug.Log($"[OnCardPlayAnimationComplete] Scheduling AI play for {GameManager.Instance.CurrentPlayer.PlayerName} in {delay:F2}s");
                    ScheduleAIPlay(delay);
                }
                else if (GameManager.Instance.ShouldHostPlayForPosition(GameManager.Instance.CurrentPlayer.Position))
                {
                    // Online game - host plays AI for disconnected/bot player
                    Debug.Log($"[OnCardPlayAnimationComplete] HOST scheduling AI for disconnected {GameManager.Instance.CurrentPlayer.PlayerName} in {delay:F2}s");
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
        /// Trigger AI play for a disconnected/bot player. Called by NetworkGameSync.
        /// </summary>
        public void TriggerDisconnectedPlayerAI()
        {
            if (GameManager.Instance.CurrentState == GameState.PlayingTricks)
            {
                float delay = GetRemainingCooldown() + 0.3f;
                if (delay < 0.5f) delay = 0.5f;
                ScheduleAIPlay(delay);
            }
        }

        /// <summary>
        /// Cancel any pending AI play coroutine
        /// </summary>
        public void CancelPendingAIPlay()
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

            // In online games, AI only plays for disconnected/bot-replaced players (host only)
            if (GameManager.Instance.IsOnlineGame)
            {
                PlayerPosition currentPos = GameManager.Instance.CurrentPlayer.Position;
                if (!GameManager.Instance.ShouldHostPlayForPosition(currentPos))
                {
                    Debug.Log($"[AIPlayCard] Online game - waiting for remote player at {currentPos}");
                    return;
                }
                Debug.Log($"[AIPlayCard] Online game - HOST playing AI for disconnected/bot at {currentPos}");
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
                // In online games, host can play for disconnected players still marked human
                if (GameManager.Instance.IsOnlineGame && GameManager.Instance.ShouldHostPlayForPosition(currentPlayer.Position))
                {
                    Debug.Log($"[AIPlayCard] Playing AI for disconnected human player {currentPlayer.PlayerName}");
                }
                else
                {
                    Debug.Log("[AIPlayCard] Current player is human, aborting");
                    return;
                }
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

            // Play sound and haptic
            SoundManager.Instance?.PlayTrickWin();
            HapticManager.Instance?.HeavyTap();

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

                Vector2 collectPos = GetPlayerStartPosition(GameManager.Instance.GetVisualPosition(winner.Position));
                CardAnimator.Instance.AnimateCollect(cardRects, collectPos, () => {
                    ClearTrickAreaImmediate();
                    ContinueAfterTrick();
                });
            }
            else
            {
                if (delayedClearTrickCoroutine != null) StopCoroutine(delayedClearTrickCoroutine);
                delayedClearTrickCoroutine = StartCoroutine(DelayedClearTrickArea(0.8f));
            }

            instructionText.text = $"{winner.PlayerName} wins the trick!";
            UpdateScoreText();
        }

        private void HighlightTrickWinner(PlayerPosition position)
        {
            if (trickWinnerHighlightCoroutine != null) StopCoroutine(trickWinnerHighlightCoroutine);
            trickWinnerHighlightCoroutine = StartCoroutine(AnimateTrickWinnerHighlight(position));
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

                // HOST: auto-pass for disconnected/bot players
                if (NetworkGameSync.Instance != null)
                    NetworkGameSync.Instance.AutoPassForDisconnectedPlayers();
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
            ResetWatchdogTimer();
            // Set animating flag BEFORE displaying cards so OnTrickStarted knows to wait
            isAnimating = true;
            DisplayPlayerHandAnimatedWithCallback(() => {
                isAnimating = false;
                Debug.Log("[OnPassPhaseComplete] Hand animation complete, isAnimating = false");
            });
            isPassPhase = false;
            selectedForPass.Clear();
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
            else if (GameManager.Instance.ShouldHostPlayForPosition(currentPlayer.Position))
            {
                // Online game - host plays AI for disconnected/bot player
                Debug.Log($"[StartTurnAfterDelay] HOST playing AI for disconnected {currentPlayer.PlayerName}");
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
            HapticManager.Instance?.MediumTap();
            UpdateScoreText();
            instructionText.text = "Round ended! Starting next round...";

            // Record round scores to history
            if (scoreSummaryPopup != null && GameManager.Instance != null)
            {
                int roundNum = GameManager.Instance.RoundNumber;
                int southScore = 0, eastScore = 0, northScore = 0, westScore = 0;
                foreach (var p in players)
                {
                    switch (p.Position)
                    {
                        case PlayerPosition.South: southScore = p.RoundPoints; break;
                        case PlayerPosition.East: eastScore = p.RoundPoints; break;
                        case PlayerPosition.North: northScore = p.RoundPoints; break;
                        case PlayerPosition.West: westScore = p.RoundPoints; break;
                    }
                }
                scoreSummaryPopup.RecordRoundHistory(roundNum, southScore, eastScore, northScore, westScore);
            }

            if (delayedStartNextRoundCoroutine != null) StopCoroutine(delayedStartNextRoundCoroutine);
            delayedStartNextRoundCoroutine = StartCoroutine(DelayedStartNextRound(2f));
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
                    // GUARD: If we already received CardDealt and progressed past RoundEnd,
                    // don't overwrite the state (race condition with host's CardDealt broadcast)
                    var currentState = GameManager.Instance.CurrentState;
                    if (currentState != GameState.RoundEnd)
                    {
                        Debug.Log($"[StartNextRound] Non-host: state already {currentState}, skipping state reset to Dealing");
                        return;
                    }
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
                HapticManager.Instance?.SuccessTap();
                instructionText.text = "Game Over! You WIN!";
            }
            else
            {
                SoundManager.Instance?.PlayGameLose();
                HapticManager.Instance?.ErrorTap();
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
            // Scores are now shown in player info panels and score popup
            // Update player info panels with current scores
            UpdatePlayerInfoPanels();
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
                {
                    card.OnCardClicked -= OnPlayerCardClicked;
                    Destroy(card.gameObject);
                }
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

        /// <summary>
        /// Show an emoji reaction above the specified player's panel
        /// </summary>
        public void ShowEmojiForPlayer(string emoji, PlayerPosition position)
        {
            if (playerInfoPanels.TryGetValue(position, out PlayerInfoPanel panel))
            {
                panel.ShowEmoji(emoji);
                HapticManager.Instance?.LightTap();
            }
        }

        /// <summary>
        /// Called when a remote player sends an emoji reaction
        /// </summary>
        private void OnRemoteEmojiReceived(string emoji, string positionStr)
        {
            if (System.Enum.TryParse<PlayerPosition>(positionStr, out PlayerPosition serverPos))
            {
                // playerInfoPanels is keyed by server position
                Debug.Log($"[GameUI] Remote emoji received: {emoji} from {serverPos}");
                ShowEmojiForPlayer(emoji, serverPos);
            }
        }

        // --- Disconnect UI event handlers ---

        private void OnPlayerDisconnectedUI(PlayerPosition pos, string playerName, float timeoutSeconds)
        {
            // Update player info panel
            if (playerInfoPanels.TryGetValue(pos, out PlayerInfoPanel panel))
            {
                panel.SetDisconnected(true);
            }

            // Show notification banner
            DisconnectNotification.Instance?.ShowDisconnected(pos, playerName, timeoutSeconds);
        }

        private void OnPlayerReconnectedUI(PlayerPosition pos, string playerName)
        {
            // Update player info panel
            if (playerInfoPanels.TryGetValue(pos, out PlayerInfoPanel panel))
            {
                panel.SetDisconnected(false);
            }

            // Show reconnected notification
            DisconnectNotification.Instance?.ShowReconnected(pos, playerName);
        }

        private void OnBotReplacedUI(PlayerPosition pos, string playerName)
        {
            // Update player info panel
            if (playerInfoPanels.TryGetValue(pos, out PlayerInfoPanel panel))
            {
                panel.SetDisconnected(false);
                panel.SetBotReplaced(true);
            }

            // Show bot replaced notification
            DisconnectNotification.Instance?.ShowBotReplaced(pos, playerName);
        }
    }
}
