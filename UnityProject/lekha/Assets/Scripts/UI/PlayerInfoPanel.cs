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
    /// Casino-style design with wood/gold accents
    /// Supports custom avatars from player profiles
    /// </summary>
    public class PlayerInfoPanel : MonoBehaviour
    {
        private Player player;
        private PlayerPosition position;
        private RectTransform panelRect;

        // UI Elements
        private Image backgroundImage;
        private Image avatarBg;
        private Image avatarImage; // For custom avatar display
        private Image avatarRing;
        private TextMeshProUGUI avatarText;
        private TextMeshProUGUI nameText;
        private TextMeshProUGUI scoreText;
        private TextMeshProUGUI scoreLabel;
        private Image turnGlow;
        private Image teamBar;
        private CanvasGroup canvasGroup;

        // Special card indicators (Queen of Spades, 10 of Diamonds)
        private GameObject specialCardsContainer;
        private Image queenOfSpadesIcon;
        private Image tenOfDiamondsIcon;

        // State
        private bool isTurnActive = false;
        private bool hasQueenOfSpades = false;
        private bool hasTenOfDiamonds = false;

        // Casino Theme Colors
        private static readonly Color PanelBg = new Color(0.06f, 0.04f, 0.02f, 0.94f);
        private static readonly Color GoldTrim = new Color(0.85f, 0.70f, 0.35f, 1f);
        private static readonly Color GoldBright = new Color(0.95f, 0.80f, 0.40f, 1f);
        private static readonly Color TextWhite = new Color(0.95f, 0.92f, 0.85f, 1f);
        private static readonly Color TextGold = new Color(0.95f, 0.82f, 0.45f, 1f);

        public static PlayerInfoPanel Create(Transform parent, Player player, PlayerPosition position)
        {
            GameObject obj = new GameObject($"PlayerPanel_{position}");
            obj.transform.SetParent(parent, false);

            RectTransform rect = obj.AddComponent<RectTransform>();

            PlayerInfoPanel panel = obj.AddComponent<PlayerInfoPanel>();
            panel.player = player;
            panel.position = position;
            panel.panelRect = rect;
            panel.BuildUI();
            panel.PositionInCorner();

            return panel;
        }

        private void BuildUI()
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

            // Panel size - LARGE with prominent avatar
            Vector2 panelSize = new Vector2(200, 100);
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
            turnGlow.color = new Color(GoldBright.r, GoldBright.g, GoldBright.b, 0.8f);
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
            if (ModernUITheme.Instance != null && ModernUITheme.Instance.CornerPanelSprite != null)
            {
                backgroundImage.sprite = ModernUITheme.Instance.CornerPanelSprite;
                backgroundImage.type = Image.Type.Sliced;
            }
            backgroundImage.color = PanelBg;

            // Add shadow for depth
            Shadow shadow = bgObj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.6f);
            shadow.effectDistance = new Vector2(3, -3);

            // Gold border highlight
            GameObject borderObj = new GameObject("GoldBorder");
            borderObj.transform.SetParent(bgObj.transform, false);

            RectTransform borderRect = borderObj.AddComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.sizeDelta = Vector2.zero;

            Outline outline = borderObj.AddComponent<Outline>();
            outline.effectColor = new Color(GoldTrim.r, GoldTrim.g, GoldTrim.b, 0.5f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
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
            nameRect.anchoredPosition = new Vector2(leftOffset, 18);
            nameRect.sizeDelta = new Vector2(-leftOffset - rightPadding, 30);

            nameText = nameObj.AddComponent<TextMeshProUGUI>();
            // Use GetDisplayName() for all players - it handles online/offline modes
            bool isOnlineGame = NetworkGameSync.Instance != null && NetworkGameSync.Instance.IsOnlineGame;
            nameText.text = GetDisplayName().ToUpper();
            nameText.fontSize = player.IsHuman ? 20 : 18;
            nameText.fontStyle = FontStyles.Bold;
            // Use gold color for human player in offline mode, but white for everyone in online mode
            nameText.color = (player.IsHuman && !isOnlineGame) ? TextGold : TextWhite;
            nameText.alignment = TextAlignmentOptions.Left;
            nameText.textWrappingMode = TextWrappingModes.NoWrap;
            nameText.overflowMode = TextOverflowModes.Truncate;

            // Score label
            GameObject labelObj = new GameObject("ScoreLabel");
            labelObj.transform.SetParent(transform, false);

            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0.5f);
            labelRect.anchorMax = new Vector2(0, 0.5f);
            labelRect.pivot = new Vector2(0, 0.5f);
            labelRect.anchoredPosition = new Vector2(leftOffset, -18);
            labelRect.sizeDelta = new Vector2(50, 24);

            scoreLabel = labelObj.AddComponent<TextMeshProUGUI>();
            scoreLabel.text = "SCORE";
            scoreLabel.fontSize = 11;
            scoreLabel.color = new Color(TextWhite.r, TextWhite.g, TextWhite.b, 0.6f);
            scoreLabel.alignment = TextAlignmentOptions.Left;

            // Score value
            GameObject scoreObj = new GameObject("Score");
            scoreObj.transform.SetParent(transform, false);

            RectTransform scoreRect = scoreObj.AddComponent<RectTransform>();
            scoreRect.anchorMin = new Vector2(0, 0.5f);
            scoreRect.anchorMax = new Vector2(1, 0.5f);
            scoreRect.pivot = new Vector2(0, 0.5f);
            scoreRect.anchoredPosition = new Vector2(leftOffset + 52, -18);
            scoreRect.sizeDelta = new Vector2(-leftOffset - rightPadding - 52, 28);

            scoreText = scoreObj.AddComponent<TextMeshProUGUI>();
            scoreText.text = "0";
            scoreText.fontSize = 36;
            scoreText.fontStyle = FontStyles.Bold;
            scoreText.color = TextGold;
            scoreText.alignment = TextAlignmentOptions.Left;
        }

        /// <summary>
        /// Create indicators for special cards taken (Queen of Spades, 10 of Diamonds)
        /// </summary>
        private void CreateSpecialCardsIndicator()
        {
            // Container for special card icons - positioned below the score
            specialCardsContainer = new GameObject("SpecialCards");
            specialCardsContainer.transform.SetParent(transform, false);

            RectTransform containerRect = specialCardsContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0, 0);
            containerRect.anchorMax = new Vector2(1, 0);
            containerRect.pivot = new Vector2(0.5f, 0);
            containerRect.anchoredPosition = new Vector2(0, 5);
            containerRect.sizeDelta = new Vector2(0, 30);

            // Horizontal layout for icons
            HorizontalLayoutGroup layout = specialCardsContainer.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(10, 10, 0, 0);

            // Queen of Spades indicator (Blue +2)
            queenOfSpadesIcon = CreateSpecialCardIcon(specialCardsContainer.transform, "QueenOfSpades",
                new Color(0.2f, 0.4f, 0.9f, 1f), "+2"); // Blue
            queenOfSpadesIcon.gameObject.SetActive(false);

            // 10 of Diamonds indicator (Yellow 0)
            tenOfDiamondsIcon = CreateSpecialCardIcon(specialCardsContainer.transform, "TenOfDiamonds",
                new Color(0.95f, 0.85f, 0.2f, 1f), "0"); // Yellow
            tenOfDiamondsIcon.gameObject.SetActive(false);
        }

        private Image CreateSpecialCardIcon(Transform parent, string name, Color bgColor, string label)
        {
            GameObject iconObj = new GameObject(name);
            iconObj.transform.SetParent(parent, false);

            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(28, 28);

            // Background circle/rounded rect
            Image bg = iconObj.AddComponent<Image>();
            bg.color = bgColor;

            // Add outline effect
            Outline outline = iconObj.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 1f, 1f, 0.8f);
            outline.effectDistance = new Vector2(1, -1);

            // Label text
            GameObject textObj = new GameObject("Label");
            textObj.transform.SetParent(iconObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 14;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;

            // Add layout element for proper sizing
            LayoutElement layoutElem = iconObj.AddComponent<LayoutElement>();
            layoutElem.preferredWidth = 28;
            layoutElem.preferredHeight = 28;

            return bg;
        }

        /// <summary>
        /// Show that this player took the Queen of Spades
        /// </summary>
        public void ShowQueenOfSpades()
        {
            hasQueenOfSpades = true;
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
            hasTenOfDiamonds = true;
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
            hasQueenOfSpades = false;
            hasTenOfDiamonds = false;
            if (queenOfSpadesIcon != null)
            {
                queenOfSpadesIcon.gameObject.SetActive(false);
            }
            if (tenOfDiamondsIcon != null)
            {
                tenOfDiamondsIcon.gameObject.SetActive(false);
            }
        }

        private void PositionInCorner()
        {
            // CLEAN LAYOUT: Position panels above each player's card area
            // Ensures NO OVERLAP with cards

            switch (position)
            {
                case PlayerPosition.South: // Human player - ABOVE cards, bottom center
                    panelRect.anchorMin = new Vector2(0.5f, 0);
                    panelRect.anchorMax = new Vector2(0.5f, 0);
                    panelRect.pivot = new Vector2(0.5f, 0);
                    panelRect.anchoredPosition = new Vector2(0, 280); // High enough to be above cards
                    break;

                case PlayerPosition.West: // Left side - above card backs
                    panelRect.anchorMin = new Vector2(0, 0.5f);
                    panelRect.anchorMax = new Vector2(0, 0.5f);
                    panelRect.pivot = new Vector2(0, 0.5f);
                    panelRect.anchoredPosition = new Vector2(20, 80); // Left edge, slightly up
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
            return position switch
            {
                PlayerPosition.South => new Color(0.25f, 0.65f, 0.85f, 1f),  // Blue (human)
                PlayerPosition.North => new Color(0.30f, 0.60f, 0.80f, 1f),  // Lighter blue (partner)
                PlayerPosition.East => new Color(0.85f, 0.50f, 0.30f, 1f),   // Orange
                PlayerPosition.West => new Color(0.80f, 0.45f, 0.28f, 1f),   // Orange variant
                _ => Color.gray
            };
        }

        private string GetInitial()
        {
            if (player.IsHuman) return "Y";
            return position switch
            {
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
                transform.localScale = Vector3.one * 1.08f;
                backgroundImage.color = new Color(PanelBg.r * 1.3f, PanelBg.g * 1.3f, PanelBg.b * 1.3f, PanelBg.a);
                // Set static glow color - no animation to avoid flickering
                turnGlow.color = new Color(GoldBright.r, GoldBright.g, GoldBright.b, 0.7f);
            }
            else
            {
                transform.localScale = Vector3.one;
                backgroundImage.color = PanelBg;
            }
        }

        public void UpdateScore()
        {
            if (scoreText != null && player != null)
            {
                // Show only current round points on corners (real-time during round)
                scoreText.text = player.RoundPoints.ToString();

                // Color based on round points danger
                if (player.RoundPoints >= 20)
                    scoreText.color = ModernUITheme.Danger;
                else if (player.RoundPoints >= 10)
                    scoreText.color = ModernUITheme.Warning;
                else
                    scoreText.color = TextGold;
            }
        }

        public void UpdateDisplay()
        {
            UpdateScore();
        }

        // Update loop removed to fix flickering - turn glow is now static when active
        // The SetTurnActive method handles the visual state change
    }
}
