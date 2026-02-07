using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Lekha.Core;
using Lekha.GameLogic;
using Lekha.Network;

namespace Lekha.UI
{
    /// <summary>
    /// Corner-positioned player info panel - anchored to screen corners
    /// Modern 2026 glassmorphism design with vibrant accents
    /// Supports custom avatars from player profiles
    /// </summary>
    public class PlayerInfoPanel : MonoBehaviour
    {
        private Player player;
        private PlayerPosition position;       // Server-assigned position (for data/network)
        private PlayerPosition visualPosition; // Screen position (for UI layout)
        private RectTransform panelRect;

        // UI Elements
        private Image backgroundImage;
        private Image avatarBg;
        private Image avatarImage; // For custom avatar display
        private Image avatarRing;
        private TextMeshProUGUI avatarText;
        private TextMeshProUGUI nameText;
        private TextMeshProUGUI roundPointsLabel;
        private TextMeshProUGUI roundPointsText;
        private TextMeshProUGUI totalPointsLabel;
        private TextMeshProUGUI totalPointsText;
        private Image turnGlow;
        private Image teamBar;
        private CanvasGroup canvasGroup;

        // Special card indicators (Queen of Spades, 10 of Diamonds)
        private GameObject specialCardsContainer;
        private Image queenOfSpadesIcon;
        private Image tenOfDiamondsIcon;

        // Disconnect overlay + BOT badge
        private GameObject disconnectOverlay;
        private TextMeshProUGUI disconnectText;
        private GameObject botBadge;
        private bool isDisconnected = false;
        private bool isBotReplaced = false;

        // State
        private bool isTurnActive = false;

        // Modern 2026 Theme Colors
        private static readonly Color PanelBg = new Color(0.10f, 0.12f, 0.18f, 0.92f);
        private static readonly Color AccentCyan = new Color(0.40f, 0.75f, 1f, 1f);
        private static readonly Color AccentBright = new Color(0.50f, 0.85f, 1f, 1f);
        private static readonly Color TextWhite = new Color(1f, 1f, 1f, 1f);
        private static readonly Color TextCyan = new Color(0.40f, 0.85f, 1f, 1f);
        private static readonly Color GlassBorder = new Color(1f, 1f, 1f, 0.18f);

        // Legacy aliases
        private static readonly Color GoldTrim = AccentCyan;
        private static readonly Color GoldBright = AccentBright;
        private static readonly Color TextGold = TextCyan;

        public static PlayerInfoPanel Create(Transform parent, Player player, PlayerPosition serverPosition, PlayerPosition visualPos)
        {
            GameObject obj = new GameObject($"PlayerPanel_{serverPosition}_at_{visualPos}");
            obj.transform.SetParent(parent, false);

            RectTransform rect = obj.AddComponent<RectTransform>();

            PlayerInfoPanel panel = obj.AddComponent<PlayerInfoPanel>();
            panel.player = player;
            panel.position = serverPosition;
            panel.visualPosition = visualPos;
            panel.panelRect = rect;
            panel.BuildUI();
            panel.PositionInCorner();

            return panel;
        }

        private void OnDestroy()
        {
            // Clean up the special cards container since it's parented to canvas, not this panel
            if (specialCardsContainer != null)
            {
                Destroy(specialCardsContainer);
            }
        }

        private void BuildUI()
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

            // Panel size - LARGE with prominent avatar and both scores
            Vector2 panelSize = new Vector2(210, 115);
            panelRect.sizeDelta = panelSize;

            // Turn glow (behind everything)
            CreateTurnGlow();

            // Main background panel
            CreateBackground();

            // Team color indicator
            CreateTeamIndicator();

            // Avatar section
            CreateAvatar();

            // Name and score section
            CreateInfoSection();

            // Special cards indicator (Queen of Spades, 10 of Diamonds)
            CreateSpecialCardsIndicator();

            // Disconnect overlay + BOT badge (hidden by default)
            CreateDisconnectOverlay();
            CreateBotBadge();

            UpdateDisplay();
        }

        private void CreateTurnGlow()
        {
            GameObject glowObj = new GameObject("TurnGlow");
            glowObj.transform.SetParent(transform, false);

            RectTransform glowRect = glowObj.AddComponent<RectTransform>();
            glowRect.anchorMin = new Vector2(0.5f, 0.5f);
            glowRect.anchorMax = new Vector2(0.5f, 0.5f);
            glowRect.sizeDelta = panelRect.sizeDelta + new Vector2(40, 40);

            turnGlow = glowObj.AddComponent<Image>();
            if (ModernUITheme.Instance != null && ModernUITheme.Instance.SoftGlowSprite != null)
            {
                turnGlow.sprite = ModernUITheme.Instance.SoftGlowSprite;
            }
            turnGlow.color = new Color(AccentCyan.r, AccentCyan.g, AccentCyan.b, 0.7f);
            turnGlow.raycastTarget = false;
            turnGlow.enabled = false;
        }

        private void CreateBackground()
        {
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(transform, false);

            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            backgroundImage = bgObj.AddComponent<Image>();
            // Use modern glass panel if available
            if (ModernUITheme.Instance != null && ModernUITheme.Instance.GlassPanelDarkSprite != null)
            {
                backgroundImage.sprite = ModernUITheme.Instance.GlassPanelDarkSprite;
                backgroundImage.type = Image.Type.Sliced;
                backgroundImage.color = Color.white;
            }
            else if (ModernUITheme.Instance != null && ModernUITheme.Instance.CornerPanelSprite != null)
            {
                backgroundImage.sprite = ModernUITheme.Instance.CornerPanelSprite;
                backgroundImage.type = Image.Type.Sliced;
                backgroundImage.color = PanelBg;
            }
            else
            {
                backgroundImage.color = PanelBg;
            }
        }

        private void CreateTeamIndicator()
        {
            GameObject teamObj = new GameObject("TeamBar");
            teamObj.transform.SetParent(transform, false);

            RectTransform teamRect = teamObj.AddComponent<RectTransform>();
            teamRect.anchorMin = new Vector2(0, 0);
            teamRect.anchorMax = new Vector2(0, 1);
            teamRect.pivot = new Vector2(0, 0.5f);
            teamRect.sizeDelta = new Vector2(5, -16);
            teamRect.anchoredPosition = new Vector2(8, 0);

            teamBar = teamObj.AddComponent<Image>();
            Color teamColor = player.Team == Team.NorthSouth ? ModernUITheme.TeamNorthSouth : ModernUITheme.TeamEastWest;
            teamBar.color = teamColor;

            if (ModernUITheme.Instance != null && ModernUITheme.Instance.PillSprite != null)
            {
                teamBar.sprite = ModernUITheme.Instance.PillSprite;
                teamBar.type = Image.Type.Sliced;
            }
        }

        private void CreateAvatar()
        {
            float avatarSize = 80f; // LARGE avatar
            float leftPadding = 10f;

            // Avatar container
            GameObject avatarObj = new GameObject("Avatar");
            avatarObj.transform.SetParent(transform, false);

            RectTransform avatarRect = avatarObj.AddComponent<RectTransform>();
            avatarRect.anchorMin = new Vector2(0, 0.5f);
            avatarRect.anchorMax = new Vector2(0, 0.5f);
            avatarRect.pivot = new Vector2(0, 0.5f);
            avatarRect.sizeDelta = new Vector2(avatarSize, avatarSize);
            avatarRect.anchoredPosition = new Vector2(leftPadding, 0);

            // Avatar ring (gold border)
            GameObject ringObj = new GameObject("Ring");
            ringObj.transform.SetParent(avatarObj.transform, false);

            RectTransform ringRect = ringObj.AddComponent<RectTransform>();
            ringRect.anchorMin = Vector2.zero;
            ringRect.anchorMax = Vector2.one;
            ringRect.sizeDelta = new Vector2(4, 4);
            ringRect.anchoredPosition = Vector2.zero;

            avatarRing = ringObj.AddComponent<Image>();
            if (ModernUITheme.Instance != null && ModernUITheme.Instance.CircleSprite != null)
            {
                avatarRing.sprite = ModernUITheme.Instance.CircleSprite;
            }
            avatarRing.color = GoldTrim;

            // Avatar background (for placeholder color)
            GameObject bgObj = new GameObject("AvatarBg");
            bgObj.transform.SetParent(avatarObj.transform, false);

            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = new Vector2(-6, -6);
            bgRect.anchoredPosition = Vector2.zero;

            avatarBg = bgObj.AddComponent<Image>();
            if (ModernUITheme.Instance != null && ModernUITheme.Instance.CircleSprite != null)
            {
                avatarBg.sprite = ModernUITheme.Instance.CircleSprite;
            }
            avatarBg.color = GetAvatarColor();

            // Custom avatar image (overlays the background when available)
            GameObject customAvatarObj = new GameObject("CustomAvatar");
            customAvatarObj.transform.SetParent(avatarObj.transform, false);

            RectTransform customRect = customAvatarObj.AddComponent<RectTransform>();
            customRect.anchorMin = Vector2.zero;
            customRect.anchorMax = Vector2.one;
            customRect.sizeDelta = new Vector2(-6, -6);
            customRect.anchoredPosition = Vector2.zero;

            avatarImage = customAvatarObj.AddComponent<Image>();
            avatarImage.sprite = CreateCircleSprite(64);
            avatarImage.color = Color.white;
            avatarImage.preserveAspect = true;
            avatarImage.gameObject.SetActive(false); // Hidden until custom avatar is set

            // Mask for circular avatar
            Mask mask = customAvatarObj.AddComponent<Mask>();
            mask.showMaskGraphic = true;

            // Avatar initial letter
            GameObject initialObj = new GameObject("Initial");
            initialObj.transform.SetParent(avatarObj.transform, false);

            RectTransform initRect = initialObj.AddComponent<RectTransform>();
            initRect.anchorMin = Vector2.zero;
            initRect.anchorMax = Vector2.one;
            initRect.sizeDelta = Vector2.zero;

            avatarText = initialObj.AddComponent<TextMeshProUGUI>();
            avatarText.text = GetInitial();
            avatarText.fontSize = 28;
            avatarText.fontStyle = FontStyles.Bold;
            avatarText.color = Color.white;
            avatarText.alignment = TextAlignmentOptions.Center;

            // Try to load custom avatar for human player
            LoadCustomAvatar();
        }

        private Sprite CreateCircleSprite(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size / 2f - 1;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    tex.SetPixel(x, y, dist <= radius ? Color.white : Color.clear);
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private void LoadCustomAvatar()
        {
            // Only load custom avatar for human player (South position)
            if (!player.IsHuman || position != PlayerPosition.South)
                return;

            var profile = PlayerProfileManager.Instance?.CurrentProfile;
            if (profile == null)
                return;

            Sprite avatarSprite = profile.GetAvatarSprite();
            if (avatarSprite != null)
            {
                SetCustomAvatar(avatarSprite, profile.DisplayName);
            }
            else
            {
                // Use profile name for initial
                SetPlaceholderAvatar(profile.DisplayName);
            }
        }

        /// <summary>
        /// Set a custom avatar image for this player
        /// </summary>
        public void SetCustomAvatar(Sprite sprite, string displayName = null)
        {
            if (avatarImage == null)
                return;

            avatarImage.sprite = sprite;
            avatarImage.gameObject.SetActive(true);
            avatarText.gameObject.SetActive(false);
            avatarBg.gameObject.SetActive(false);

            // Update name if provided
            if (!string.IsNullOrEmpty(displayName) && nameText != null && player.IsHuman)
            {
                nameText.text = displayName.ToUpper();
            }
        }

        /// <summary>
        /// Set placeholder avatar with initial letter
        /// </summary>
        public void SetPlaceholderAvatar(string displayName)
        {
            if (avatarImage != null)
                avatarImage.gameObject.SetActive(false);

            if (avatarText != null)
            {
                avatarText.gameObject.SetActive(true);
                if (!string.IsNullOrEmpty(displayName))
                {
                    avatarText.text = displayName[0].ToString().ToUpper();
                }
            }

            if (avatarBg != null)
            {
                avatarBg.gameObject.SetActive(true);
                // Generate color based on name
                if (!string.IsNullOrEmpty(displayName))
                {
                    avatarBg.color = GetColorFromName(displayName);
                }
            }

            // Update name display
            if (!string.IsNullOrEmpty(displayName) && nameText != null && player.IsHuman)
            {
                nameText.text = displayName.ToUpper();
            }
        }

        private Color GetColorFromName(string name)
        {
            int hash = name.GetHashCode();
            float hue = Mathf.Abs(hash % 360) / 360f;
            return Color.HSVToRGB(hue, 0.5f, 0.7f);
        }

        /// <summary>
        /// Refresh avatar from profile (call when profile changes)
        /// </summary>
        public void RefreshFromProfile()
        {
            if (!player.IsHuman)
                return;

            var profile = PlayerProfileManager.Instance?.CurrentProfile;
            if (profile == null)
                return;

            Sprite avatarSprite = profile.GetAvatarSprite();
            if (avatarSprite != null)
            {
                SetCustomAvatar(avatarSprite, profile.DisplayName);
            }
            else
            {
                SetPlaceholderAvatar(profile.DisplayName);
            }
        }

        private void CreateInfoSection()
        {
            float leftOffset = 95f; // After large avatar
            float rightPadding = 8f;

            // Name text
            GameObject nameObj = new GameObject("Name");
            nameObj.transform.SetParent(transform, false);

            RectTransform nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0.5f);
            nameRect.anchorMax = new Vector2(1, 0.5f);
            nameRect.pivot = new Vector2(0, 0.5f);
            nameRect.anchoredPosition = new Vector2(leftOffset, 28);
            nameRect.sizeDelta = new Vector2(-leftOffset - rightPadding, 26);

            nameText = nameObj.AddComponent<TextMeshProUGUI>();
            // Use GetDisplayName() for all players - it handles online/offline modes
            bool isOnlineGame = NetworkGameSync.Instance != null && NetworkGameSync.Instance.IsOnlineGame;
            nameText.text = GetDisplayName().ToUpper();
            nameText.fontSize = player.IsHuman ? 18 : 16;
            nameText.fontStyle = FontStyles.Bold;
            // Use gold color for human player in offline mode, but white for everyone in online mode
            nameText.color = (player.IsHuman && !isOnlineGame) ? TextGold : TextWhite;
            nameText.alignment = TextAlignmentOptions.Left;
            nameText.textWrappingMode = TextWrappingModes.NoWrap;
            nameText.overflowMode = TextOverflowModes.Truncate;

            // Round points row
            // Round label
            GameObject roundLabelObj = new GameObject("RoundLabel");
            roundLabelObj.transform.SetParent(transform, false);

            RectTransform roundLabelRect = roundLabelObj.AddComponent<RectTransform>();
            roundLabelRect.anchorMin = new Vector2(0, 0.5f);
            roundLabelRect.anchorMax = new Vector2(0, 0.5f);
            roundLabelRect.pivot = new Vector2(0, 0.5f);
            roundLabelRect.anchoredPosition = new Vector2(leftOffset, 2);
            roundLabelRect.sizeDelta = new Vector2(50, 20);

            roundPointsLabel = roundLabelObj.AddComponent<TextMeshProUGUI>();
            roundPointsLabel.text = "ROUND";
            roundPointsLabel.fontSize = 12;
            roundPointsLabel.fontStyle = FontStyles.Bold;
            roundPointsLabel.color = new Color(TextWhite.r, TextWhite.g, TextWhite.b, 0.75f);
            roundPointsLabel.alignment = TextAlignmentOptions.Left;

            // Round points value
            GameObject roundObj = new GameObject("RoundPoints");
            roundObj.transform.SetParent(transform, false);

            RectTransform roundRect = roundObj.AddComponent<RectTransform>();
            roundRect.anchorMin = new Vector2(0, 0.5f);
            roundRect.anchorMax = new Vector2(1, 0.5f);
            roundRect.pivot = new Vector2(0, 0.5f);
            roundRect.anchoredPosition = new Vector2(leftOffset + 50, 2);
            roundRect.sizeDelta = new Vector2(-leftOffset - rightPadding - 50, 22);

            roundPointsText = roundObj.AddComponent<TextMeshProUGUI>();
            roundPointsText.text = "0";
            roundPointsText.fontSize = 26;
            roundPointsText.fontStyle = FontStyles.Bold;
            roundPointsText.color = TextGold;
            roundPointsText.alignment = TextAlignmentOptions.Left;

            // Total points row
            // Total label
            GameObject totalLabelObj = new GameObject("TotalLabel");
            totalLabelObj.transform.SetParent(transform, false);

            RectTransform totalLabelRect = totalLabelObj.AddComponent<RectTransform>();
            totalLabelRect.anchorMin = new Vector2(0, 0.5f);
            totalLabelRect.anchorMax = new Vector2(0, 0.5f);
            totalLabelRect.pivot = new Vector2(0, 0.5f);
            totalLabelRect.anchoredPosition = new Vector2(leftOffset, -22);
            totalLabelRect.sizeDelta = new Vector2(50, 20);

            totalPointsLabel = totalLabelObj.AddComponent<TextMeshProUGUI>();
            totalPointsLabel.text = "TOTAL";
            totalPointsLabel.fontSize = 12;
            totalPointsLabel.fontStyle = FontStyles.Bold;
            totalPointsLabel.color = new Color(TextWhite.r, TextWhite.g, TextWhite.b, 0.75f);
            totalPointsLabel.alignment = TextAlignmentOptions.Left;

            // Total points value
            GameObject totalObj = new GameObject("TotalPoints");
            totalObj.transform.SetParent(transform, false);

            RectTransform totalRect = totalObj.AddComponent<RectTransform>();
            totalRect.anchorMin = new Vector2(0, 0.5f);
            totalRect.anchorMax = new Vector2(1, 0.5f);
            totalRect.pivot = new Vector2(0, 0.5f);
            totalRect.anchoredPosition = new Vector2(leftOffset + 50, -22);
            totalRect.sizeDelta = new Vector2(-leftOffset - rightPadding - 50, 22);

            totalPointsText = totalObj.AddComponent<TextMeshProUGUI>();
            totalPointsText.text = "0";
            totalPointsText.fontSize = 26;
            totalPointsText.fontStyle = FontStyles.Bold;
            totalPointsText.color = TextWhite;
            totalPointsText.alignment = TextAlignmentOptions.Left;
        }

        /// <summary>
        /// Create indicators for special cards taken (Queen of Spades, 10 of Diamonds)
        /// Position based on player location:
        /// - East/West: UNDER the panel
        /// - North/South: NEXT TO the panel
        /// </summary>
        private void CreateSpecialCardsIndicator()
        {
            // Container for special card icons
            specialCardsContainer = new GameObject("SpecialCards");
            specialCardsContainer.transform.SetParent(transform.parent, false); // Parent to canvas, not panel

            RectTransform containerRect = specialCardsContainer.AddComponent<RectTransform>();

            // Position based on visual position (where panel is on screen)
            switch (visualPosition)
            {
                case PlayerPosition.South: // Bottom - put to the RIGHT of panel
                    containerRect.anchorMin = new Vector2(0, 0);
                    containerRect.anchorMax = new Vector2(0, 0);
                    containerRect.pivot = new Vector2(0, 0.5f);
                    containerRect.anchoredPosition = new Vector2(panelRect.anchoredPosition.x + 85, panelRect.anchoredPosition.y + 40);
                    break;

                case PlayerPosition.North: // Top - put to the RIGHT of panel
                    containerRect.anchorMin = new Vector2(0.5f, 1);
                    containerRect.anchorMax = new Vector2(0.5f, 1);
                    containerRect.pivot = new Vector2(0, 0.5f);
                    containerRect.anchoredPosition = new Vector2(85, panelRect.anchoredPosition.y - 40);
                    break;

                case PlayerPosition.East: // Right side - put UNDER the panel
                    containerRect.anchorMin = new Vector2(1, 0.5f);
                    containerRect.anchorMax = new Vector2(1, 0.5f);
                    containerRect.pivot = new Vector2(0.5f, 1);
                    containerRect.anchoredPosition = new Vector2(panelRect.anchoredPosition.x, panelRect.anchoredPosition.y - 60);
                    break;

                case PlayerPosition.West: // Left side - put UNDER the panel
                    containerRect.anchorMin = new Vector2(0, 0.5f);
                    containerRect.anchorMax = new Vector2(0, 0.5f);
                    containerRect.pivot = new Vector2(0.5f, 1);
                    containerRect.anchoredPosition = new Vector2(panelRect.anchoredPosition.x, panelRect.anchoredPosition.y - 60);
                    break;
            }

            containerRect.sizeDelta = new Vector2(120, 50);

            // Add canvas to ensure it renders on top
            Canvas containerCanvas = specialCardsContainer.AddComponent<Canvas>();
            containerCanvas.overrideSorting = true;
            containerCanvas.sortingOrder = 150;
            specialCardsContainer.AddComponent<GraphicRaycaster>();

            // Horizontal layout for icons
            HorizontalLayoutGroup layout = specialCardsContainer.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(5, 5, 5, 5);

            // Queen of Spades indicator (Blue +2) - BIGGER AND CLEARER
            queenOfSpadesIcon = CreateSpecialCardIcon(specialCardsContainer.transform, "QueenOfSpades",
                new Color(0.3f, 0.5f, 0.95f, 1f), "+13", new Color(0.6f, 0.1f, 0.8f, 1f)); // Blue with purple accent
            queenOfSpadesIcon.gameObject.SetActive(false);

            // 10 of Diamonds indicator (Yellow 0) - BIGGER AND CLEARER
            tenOfDiamondsIcon = CreateSpecialCardIcon(specialCardsContainer.transform, "TenOfDiamonds",
                new Color(1f, 0.85f, 0.2f, 1f), "×0", new Color(1f, 0.5f, 0.1f, 1f)); // Yellow with orange accent
            tenOfDiamondsIcon.gameObject.SetActive(false);
        }

        private Image CreateSpecialCardIcon(Transform parent, string name, Color bgColor, string label, Color accentColor)
        {
            GameObject iconObj = new GameObject(name);
            iconObj.transform.SetParent(parent, false);

            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(48, 48); // BIGGER!

            // Background with glass effect
            Image bg = iconObj.AddComponent<Image>();
            bg.color = new Color(bgColor.r * 0.8f, bgColor.g * 0.8f, bgColor.b * 0.8f, 0.95f);
            if (ModernUITheme.Instance != null && ModernUITheme.Instance.GlassPanelDarkSprite != null)
            {
                bg.sprite = ModernUITheme.Instance.GlassPanelDarkSprite;
                bg.type = Image.Type.Sliced;
            }

            // Add strong outline effect
            Outline outline = iconObj.AddComponent<Outline>();
            outline.effectColor = accentColor;
            outline.effectDistance = new Vector2(2, -2);

            // Add shadow for depth
            Shadow shadow = iconObj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.6f);
            shadow.effectDistance = new Vector2(2, -2);

            // Label text - BIGGER
            GameObject textObj = new GameObject("Label");
            textObj.transform.SetParent(iconObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 22; // BIGGER font
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.outlineWidth = 0.15f;
            tmp.outlineColor = new Color(0, 0, 0, 0.7f);

            // Add layout element for proper sizing
            LayoutElement layoutElem = iconObj.AddComponent<LayoutElement>();
            layoutElem.preferredWidth = 48;
            layoutElem.preferredHeight = 48;
            layoutElem.minWidth = 48;
            layoutElem.minHeight = 48;

            return bg;
        }

        /// <summary>
        /// Show that this player took the Queen of Spades
        /// </summary>
        public void ShowQueenOfSpades()
        {
            if (queenOfSpadesIcon != null)
            {
                queenOfSpadesIcon.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Show that this player took the 10 of Diamonds
        /// </summary>
        public void ShowTenOfDiamonds()
        {
            if (tenOfDiamondsIcon != null)
            {
                tenOfDiamondsIcon.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Clear special card indicators (call at start of new round)
        /// </summary>
        public void ClearSpecialCards()
        {
            if (queenOfSpadesIcon != null)
            {
                queenOfSpadesIcon.gameObject.SetActive(false);
            }
            if (tenOfDiamondsIcon != null)
            {
                tenOfDiamondsIcon.gameObject.SetActive(false);
            }
        }

        private void CreateDisconnectOverlay()
        {
            disconnectOverlay = new GameObject("DisconnectOverlay");
            disconnectOverlay.transform.SetParent(transform, false);

            RectTransform overlayRect = disconnectOverlay.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.sizeDelta = Vector2.zero;

            Image overlayBg = disconnectOverlay.AddComponent<Image>();
            overlayBg.color = new Color(0.05f, 0.05f, 0.08f, 0.75f);
            overlayBg.raycastTarget = false;
            if (ModernUITheme.Instance != null && ModernUITheme.Instance.GlassPanelDarkSprite != null)
            {
                overlayBg.sprite = ModernUITheme.Instance.GlassPanelDarkSprite;
                overlayBg.type = Image.Type.Sliced;
            }

            // "DISCONNECTED" text
            GameObject textObj = new GameObject("DisconnectLabel");
            textObj.transform.SetParent(disconnectOverlay.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            disconnectText = textObj.AddComponent<TextMeshProUGUI>();
            disconnectText.text = "DISCONNECTED";
            disconnectText.fontSize = 16;
            disconnectText.fontStyle = FontStyles.Bold;
            disconnectText.alignment = TextAlignmentOptions.Center;
            disconnectText.color = new Color(1f, 0.65f, 0.2f, 1f); // Orange
            disconnectText.raycastTarget = false;

            disconnectOverlay.SetActive(false);
        }

        private void CreateBotBadge()
        {
            botBadge = new GameObject("BotBadge");
            botBadge.transform.SetParent(transform, false);

            RectTransform badgeRect = botBadge.AddComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(1, 1);
            badgeRect.anchorMax = new Vector2(1, 1);
            badgeRect.pivot = new Vector2(1, 1);
            badgeRect.anchoredPosition = new Vector2(-5, -5);
            badgeRect.sizeDelta = new Vector2(48, 22);

            Image badgeBg = botBadge.AddComponent<Image>();
            badgeBg.color = new Color(0.3f, 0.5f, 0.9f, 0.95f);
            badgeBg.raycastTarget = false;
            if (ModernUITheme.Instance != null && ModernUITheme.Instance.PillSprite != null)
            {
                badgeBg.sprite = ModernUITheme.Instance.PillSprite;
                badgeBg.type = Image.Type.Sliced;
            }

            GameObject labelObj = new GameObject("BotLabel");
            labelObj.transform.SetParent(botBadge.transform, false);

            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = "BOT";
            labelText.fontSize = 14;
            labelText.fontStyle = FontStyles.Bold;
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.color = Color.white;
            labelText.raycastTarget = false;

            botBadge.SetActive(false);
        }

        public void SetDisconnected(bool disconnected)
        {
            isDisconnected = disconnected;

            if (disconnectOverlay != null)
                disconnectOverlay.SetActive(disconnected);

            // Gray out avatar ring when disconnected
            if (avatarRing != null)
                avatarRing.color = disconnected ? new Color(0.4f, 0.4f, 0.4f, 0.6f) : GoldTrim;
        }

        public void SetBotReplaced(bool replaced)
        {
            isBotReplaced = replaced;

            if (botBadge != null)
                botBadge.SetActive(replaced);

            // Show blue ring for bot
            if (replaced && avatarRing != null)
                avatarRing.color = new Color(0.3f, 0.5f, 0.9f, 1f);
        }

        private void PositionInCorner()
        {
            // CLEAN LAYOUT: Position panels above each player's card area
            // Uses visualPosition so local player always appears at bottom

            switch (visualPosition)
            {
                case PlayerPosition.South: // Human player - directly above hand cards
                    panelRect.anchorMin = new Vector2(0.5f, 0);
                    panelRect.anchorMax = new Vector2(0.5f, 0);
                    panelRect.pivot = new Vector2(0.5f, 0);
                    panelRect.anchoredPosition = new Vector2(0, 185); // Above hand cards with ~10px padding
                    break;

                case PlayerPosition.West: // Left side - above card backs (moved right to avoid camera notch)
                    panelRect.anchorMin = new Vector2(0, 0.5f);
                    panelRect.anchorMax = new Vector2(0, 0.5f);
                    panelRect.pivot = new Vector2(0, 0.5f);
                    panelRect.anchoredPosition = new Vector2(60, 80); // More right to avoid camera/speaker
                    break;

                case PlayerPosition.North: // Partner - top center, above card backs
                    panelRect.anchorMin = new Vector2(0.5f, 1);
                    panelRect.anchorMax = new Vector2(0.5f, 1);
                    panelRect.pivot = new Vector2(0.5f, 1);
                    panelRect.anchoredPosition = new Vector2(0, -20); // Near top
                    break;

                case PlayerPosition.East: // Right side - above card backs
                    panelRect.anchorMin = new Vector2(1, 0.5f);
                    panelRect.anchorMax = new Vector2(1, 0.5f);
                    panelRect.pivot = new Vector2(1, 0.5f);
                    panelRect.anchoredPosition = new Vector2(-20, 80); // Right edge, slightly up
                    break;
            }
        }

        private Color GetAvatarColor()
        {
            // Modern vibrant colors based on visual position (local player always blue)
            return visualPosition switch
            {
                PlayerPosition.South => new Color(0.30f, 0.55f, 0.90f, 1f),  // Vibrant blue (local player)
                PlayerPosition.North => new Color(0.35f, 0.65f, 0.95f, 1f),  // Lighter blue (partner)
                PlayerPosition.East => new Color(0.95f, 0.50f, 0.40f, 1f),   // Coral orange (opponent)
                PlayerPosition.West => new Color(0.90f, 0.45f, 0.35f, 1f),   // Coral variant (opponent)
                _ => Color.gray
            };
        }

        private string GetInitial()
        {
            // Use visual position: South is always local player ("Y"), North is partner ("P")
            return visualPosition switch
            {
                PlayerPosition.South => "Y",
                PlayerPosition.North => "P",
                PlayerPosition.East => "E",
                PlayerPosition.West => "W",
                _ => "?"
            };
        }

        private string GetDisplayName()
        {
            // For online games, use actual network player names
            bool isOnlineGame = NetworkGameSync.Instance != null && NetworkGameSync.Instance.IsOnlineGame;

            if (isOnlineGame)
            {
                // In online games, use actual player names for everyone (including human)
                // Check if player.PlayerName is set and not default
                string name = player.PlayerName;
                if (!string.IsNullOrEmpty(name) && name != "South" && name != "North" &&
                    name != "East" && name != "West" && name != "Player")
                {
                    return name;
                }

                // Fallback: try to get from NetworkManager's room data
                var room = NetworkManager.Instance?.CurrentRoom;
                if (room?.Players != null)
                {
                    foreach (var netPlayer in room.Players)
                    {
                        if (netPlayer.Position == position)
                        {
                            if (!string.IsNullOrEmpty(netPlayer.DisplayName))
                            {
                                return netPlayer.DisplayName;
                            }
                        }
                    }
                }

                // Last resort
                return position.ToString();
            }

            // Offline mode: show "You" for human, position names for AI
            if (player.IsHuman) return "You";
            return position switch
            {
                PlayerPosition.North => "Partner",
                PlayerPosition.East => "East",
                PlayerPosition.West => "West",
                _ => player.PlayerName
            };
        }

        /// <summary>
        /// Refresh the displayed name (call when player name changes)
        /// </summary>
        public void RefreshDisplayName()
        {
            if (nameText != null)
            {
                nameText.text = GetDisplayName().ToUpper();
            }
        }

        public void SetTurnActive(bool active)
        {
            if (turnGlow == null) return;
            isTurnActive = active;
            turnGlow.enabled = active;

            if (active)
            {
                // Scale up when active - static scale, no animation
                transform.localScale = Vector3.one * 1.06f;
                // Brighten the panel slightly when active
                if (ModernUITheme.Instance != null && ModernUITheme.Instance.GlassPanelDarkSprite != null)
                {
                    backgroundImage.color = new Color(1.15f, 1.15f, 1.2f, 1f);
                }
                else
                {
                    backgroundImage.color = new Color(PanelBg.r * 1.3f, PanelBg.g * 1.3f, PanelBg.b * 1.5f, PanelBg.a);
                }
                // Set static glow color - cyan for modern look
                turnGlow.color = new Color(AccentCyan.r, AccentCyan.g, AccentCyan.b, 0.65f);
            }
            else
            {
                transform.localScale = Vector3.one;
                if (ModernUITheme.Instance != null && ModernUITheme.Instance.GlassPanelDarkSprite != null)
                {
                    backgroundImage.color = Color.white;
                }
                else
                {
                    backgroundImage.color = PanelBg;
                }
            }
        }

        public void UpdateScore()
        {
            if (player == null) return;

            // Update round points
            if (roundPointsText != null)
            {
                roundPointsText.text = player.RoundPoints.ToString();

                // Color based on round points danger
                if (player.RoundPoints >= 20)
                    roundPointsText.color = ModernUITheme.Danger;
                else if (player.RoundPoints >= 10)
                    roundPointsText.color = ModernUITheme.Warning;
                else
                    roundPointsText.color = TextGold;
            }

            // Update total points
            if (totalPointsText != null)
            {
                totalPointsText.text = player.TotalPoints.ToString();

                // Color total based on danger threshold (101 is losing score)
                if (player.TotalPoints >= 90)
                    totalPointsText.color = ModernUITheme.Danger;
                else if (player.TotalPoints >= 70)
                    totalPointsText.color = ModernUITheme.Warning;
                else
                    totalPointsText.color = TextWhite;
            }
        }

        public void UpdateDisplay()
        {
            UpdateScore();
        }

        #region Emoji Display

        private GameObject currentEmojiDisplay;
        private Coroutine emojiCoroutine;

        /// <summary>
        /// Show an emoji reaction above this player's panel
        /// </summary>
        public void ShowEmoji(string emoji)
        {
            // Cancel any existing emoji animation
            if (emojiCoroutine != null)
            {
                StopCoroutine(emojiCoroutine);
                if (currentEmojiDisplay != null)
                {
                    Destroy(currentEmojiDisplay);
                }
            }

            emojiCoroutine = StartCoroutine(AnimateEmoji(emoji));
        }

        private System.Collections.IEnumerator AnimateEmoji(string emoji)
        {
            // Create emoji display object
            currentEmojiDisplay = new GameObject("EmojiDisplay");
            currentEmojiDisplay.transform.SetParent(transform.parent, false);

            RectTransform emojiRect = currentEmojiDisplay.AddComponent<RectTransform>();

            // Position above the player panel
            Vector2 panelPos = panelRect.anchoredPosition;
            float yOffset = 70f;

            // Adjust offset based on player position
            switch (position)
            {
                case PlayerPosition.South:
                    emojiRect.anchorMin = new Vector2(0.5f, 0);
                    emojiRect.anchorMax = new Vector2(0.5f, 0);
                    emojiRect.anchoredPosition = new Vector2(panelPos.x, panelPos.y + panelRect.sizeDelta.y / 2 + yOffset);
                    break;
                case PlayerPosition.North:
                    emojiRect.anchorMin = new Vector2(0.5f, 1);
                    emojiRect.anchorMax = new Vector2(0.5f, 1);
                    emojiRect.anchoredPosition = new Vector2(panelPos.x, panelPos.y - panelRect.sizeDelta.y / 2 - yOffset);
                    break;
                case PlayerPosition.West:
                    emojiRect.anchorMin = new Vector2(0, 0.5f);
                    emojiRect.anchorMax = new Vector2(0, 0.5f);
                    emojiRect.anchoredPosition = new Vector2(panelPos.x + panelRect.sizeDelta.x / 2 + 20, panelPos.y + yOffset);
                    break;
                case PlayerPosition.East:
                    emojiRect.anchorMin = new Vector2(1, 0.5f);
                    emojiRect.anchorMax = new Vector2(1, 0.5f);
                    emojiRect.anchoredPosition = new Vector2(panelPos.x - panelRect.sizeDelta.x / 2 - 20, panelPos.y + yOffset);
                    break;
            }

            emojiRect.sizeDelta = new Vector2(70, 70);

            // Get the reaction color
            Color reactionColor = EmojiReactionSystem.GetReactionColor(emoji);

            // Background circle - darker version of accent color
            Image bg = currentEmojiDisplay.AddComponent<Image>();
            bg.sprite = CreateEmojiCircleSprite(64);
            bg.color = new Color(reactionColor.r * 0.2f, reactionColor.g * 0.2f, reactionColor.b * 0.2f, 0.95f);

            // Colored outline
            Outline outline = currentEmojiDisplay.AddComponent<Outline>();
            outline.effectColor = new Color(reactionColor.r, reactionColor.g, reactionColor.b, 0.9f);
            outline.effectDistance = new Vector2(3, -3);

            // Add shadow
            Shadow shadow = currentEmojiDisplay.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.6f);
            shadow.effectDistance = new Vector2(2, -2);

            // Emoji sprite image
            GameObject spriteObj = new GameObject("EmojiSprite");
            spriteObj.transform.SetParent(currentEmojiDisplay.transform, false);

            RectTransform spriteRect = spriteObj.AddComponent<RectTransform>();
            spriteRect.anchorMin = new Vector2(0.1f, 0.1f);
            spriteRect.anchorMax = new Vector2(0.9f, 0.9f);
            spriteRect.sizeDelta = Vector2.zero;
            spriteRect.anchoredPosition = Vector2.zero;

            Image emojiImage = spriteObj.AddComponent<Image>();
            emojiImage.sprite = EmojiReactionSystem.GetEmojiSprite(emoji);
            emojiImage.preserveAspect = true;
            emojiImage.raycastTarget = false;

            // Canvas group for fade
            CanvasGroup canvasGroup = currentEmojiDisplay.AddComponent<CanvasGroup>();

            // Phase 1: Scale in with bounce (0 -> 1.2 -> 1.0)
            float bounceTime = 0.35f;
            float elapsed = 0;
            emojiRect.localScale = Vector3.zero;
            canvasGroup.alpha = 1f;

            while (elapsed < bounceTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / bounceTime;

                // Overshoot bounce curve
                float scale;
                if (t < 0.6f)
                {
                    // Scale up to 1.25
                    scale = Mathf.Lerp(0f, 1.25f, t / 0.6f);
                }
                else
                {
                    // Bounce back to 1.0
                    scale = Mathf.Lerp(1.25f, 1f, (t - 0.6f) / 0.4f);
                }

                emojiRect.localScale = Vector3.one * scale;
                yield return null;
            }
            emojiRect.localScale = Vector3.one;

            // Phase 2: Hold (2 seconds)
            yield return new WaitForSeconds(2f);

            // Phase 3: Fade out and float up (0.5 seconds)
            float fadeTime = 0.5f;
            elapsed = 0;
            Vector2 startPos = emojiRect.anchoredPosition;

            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeTime;

                canvasGroup.alpha = 1f - t;
                emojiRect.anchoredPosition = startPos + new Vector2(0, t * 30f);
                emojiRect.localScale = Vector3.one * (1f - t * 0.3f);

                yield return null;
            }

            // Cleanup
            Destroy(currentEmojiDisplay);
            currentEmojiDisplay = null;
            emojiCoroutine = null;
        }

        private Sprite CreateEmojiCircleSprite(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size / 2f - 1;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = Mathf.Clamp01(radius - dist + 1);
                    tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        #endregion
    }
}
