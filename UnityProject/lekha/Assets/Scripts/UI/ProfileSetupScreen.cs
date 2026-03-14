using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Lekha.Core;
using Lekha.Audio;
using System.Collections;

namespace Lekha.UI
{
    /// <summary>
    /// Profile setup/edit screen with luxurious casino styling
    /// </summary>
    public class ProfileSetupScreen : MonoBehaviour
    {
        public static ProfileSetupScreen Instance { get; private set; }

        private Canvas screenCanvas;
        private GameObject screenPanel;
        private TMP_InputField nameInput;
        private Image avatarImage;
        private TextMeshProUGUI avatarInitial;
        private TextMeshProUGUI statsText;

        private Texture2D pendingAvatar;
        private bool isNewProfile = false;

        public System.Action OnProfileSaved;
        public System.Action OnScreenClosed;

        // Modern 2026 Glassmorphism Colors
        private static readonly Color DeepNavy = new Color(0.06f, 0.08f, 0.14f, 1f);
        private static readonly Color GlassPanel = new Color(0.12f, 0.14f, 0.22f, 0.95f);
        private static readonly Color AccentCyan = new Color(0.40f, 0.75f, 1f, 1f);
        private static readonly Color AccentMagenta = new Color(0.85f, 0.45f, 0.95f, 1f);
        private static readonly Color TextWhite = new Color(1f, 1f, 1f, 1f);
        private static readonly Color DarkBg = new Color(0.08f, 0.10f, 0.16f, 1f);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Show the profile setup screen
        /// </summary>
        public void Show(bool isFirstTime = false)
        {
            isNewProfile = isFirstTime;

            if (screenCanvas == null)
            {
                CreateUI();
            }

            screenCanvas.gameObject.SetActive(true);
            LoadCurrentProfile();
        }

        /// <summary>
        /// Hide the profile setup screen
        /// </summary>
        public void Hide()
        {
            if (screenCanvas != null)
            {
                screenCanvas.gameObject.SetActive(false);
            }
            OnScreenClosed?.Invoke();
        }

        private void CreateUI()
        {
            // Create Canvas
            GameObject canvasObj = new GameObject("ProfileSetupCanvas");
            canvasObj.transform.SetParent(transform);
            screenCanvas = canvasObj.AddComponent<Canvas>();
            screenCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            screenCanvas.sortingOrder = 150;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            // Dark overlay
            GameObject overlayObj = new GameObject("Overlay");
            overlayObj.transform.SetParent(canvasObj.transform, false);

            RectTransform overlayRect = overlayObj.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.sizeDelta = Vector2.zero;

            Image overlayImg = overlayObj.AddComponent<Image>();
            overlayImg.color = new Color(0.04f, 0.05f, 0.10f, 0.88f);

            // Main panel
            screenPanel = new GameObject("ProfilePanel");
            screenPanel.transform.SetParent(canvasObj.transform, false);

            RectTransform panelRect = screenPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(600, 700);

            Image panelBg = screenPanel.AddComponent<Image>();
            panelBg.color = GlassPanel;

            // Cyan glow border
            Outline border = screenPanel.AddComponent<Outline>();
            border.effectColor = new Color(AccentCyan.r, AccentCyan.g, AccentCyan.b, 0.25f);
            border.effectDistance = new Vector2(2, -2);

            // Shadow
            Shadow shadow = screenPanel.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.5f);
            shadow.effectDistance = new Vector2(0, -6);

            CreateTitle(screenPanel.transform);
            CreateAvatarSection(screenPanel.transform);
            CreateNameInput(screenPanel.transform);
            CreateStatsSection(screenPanel.transform);
            CreateButtons(screenPanel.transform);
        }

        private void CreateTitle(Transform parent)
        {
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(parent, false);

            RectTransform rect = titleObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0, -50);
            rect.sizeDelta = new Vector2(500, 60);

            TextMeshProUGUI tmp = titleObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "YOUR PROFILE";
            tmp.fontSize = 42;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = AccentCyan;
        }

        private void CreateAvatarSection(Transform parent)
        {
            float avatarSize = 150f;

            // Avatar container
            GameObject avatarContainer = new GameObject("AvatarContainer");
            avatarContainer.transform.SetParent(parent, false);

            RectTransform containerRect = avatarContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 1f);
            containerRect.anchorMax = new Vector2(0.5f, 1f);
            containerRect.anchoredPosition = new Vector2(0, -170);
            containerRect.sizeDelta = new Vector2(avatarSize + 20, avatarSize + 20);

            // Gold ring around avatar
            Image ringImg = avatarContainer.AddComponent<Image>();
            ringImg.sprite = CreateCircleSprite(128);
            ringImg.color = AccentCyan;

            // Avatar background (for placeholder)
            GameObject avatarBgObj = new GameObject("AvatarBg");
            avatarBgObj.transform.SetParent(avatarContainer.transform, false);

            RectTransform bgRect = avatarBgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0.5f, 0.5f);
            bgRect.anchorMax = new Vector2(0.5f, 0.5f);
            bgRect.sizeDelta = new Vector2(avatarSize, avatarSize);

            avatarImage = avatarBgObj.AddComponent<Image>();
            avatarImage.sprite = CreateCircleSprite(128);
            avatarImage.color = new Color(0.25f, 0.55f, 0.75f, 1f); // Default blue

            // Avatar initial text
            GameObject initialObj = new GameObject("Initial");
            initialObj.transform.SetParent(avatarBgObj.transform, false);

            RectTransform initRect = initialObj.AddComponent<RectTransform>();
            initRect.anchorMin = Vector2.zero;
            initRect.anchorMax = Vector2.one;
            initRect.sizeDelta = Vector2.zero;

            avatarInitial = initialObj.AddComponent<TextMeshProUGUI>();
            avatarInitial.text = "P";
            avatarInitial.fontSize = 72;
            avatarInitial.fontStyle = FontStyles.Bold;
            avatarInitial.color = Color.white;
            avatarInitial.alignment = TextAlignmentOptions.Center;

            // Change avatar button
            GameObject changeBtnObj = new GameObject("ChangeAvatarBtn");
            changeBtnObj.transform.SetParent(avatarContainer.transform, false);

            RectTransform changeBtnRect = changeBtnObj.AddComponent<RectTransform>();
            changeBtnRect.anchorMin = new Vector2(1f, 0f);
            changeBtnRect.anchorMax = new Vector2(1f, 0f);
            changeBtnRect.pivot = new Vector2(1f, 0f);
            changeBtnRect.anchoredPosition = new Vector2(10, -10);
            changeBtnRect.sizeDelta = new Vector2(50, 50);

            Image changeBtnImg = changeBtnObj.AddComponent<Image>();
            changeBtnImg.sprite = CreateCircleSprite(64);
            changeBtnImg.color = AccentCyan;

            Button changeBtn = changeBtnObj.AddComponent<Button>();
            changeBtn.targetGraphic = changeBtnImg;
            changeBtn.onClick.AddListener(OnChangeAvatarClicked);

            // Camera icon
            GameObject iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(changeBtnObj.transform, false);

            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.sizeDelta = new Vector2(-16, -16);

            TextMeshProUGUI iconTmp = iconObj.AddComponent<TextMeshProUGUI>();
            iconTmp.text = "+";
            iconTmp.fontSize = 32;
            iconTmp.fontStyle = FontStyles.Bold;
            iconTmp.color = DarkBg;
            iconTmp.alignment = TextAlignmentOptions.Center;

            // Tap to change hint
            GameObject hintObj = new GameObject("Hint");
            hintObj.transform.SetParent(parent, false);

            RectTransform hintRect = hintObj.AddComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0.5f, 1f);
            hintRect.anchorMax = new Vector2(0.5f, 1f);
            hintRect.anchoredPosition = new Vector2(0, -260);
            hintRect.sizeDelta = new Vector2(300, 25);

            TextMeshProUGUI hintTmp = hintObj.AddComponent<TextMeshProUGUI>();
            hintTmp.text = "Tap + to change photo";
            hintTmp.fontSize = 16;
            hintTmp.color = new Color(TextWhite.r, TextWhite.g, TextWhite.b, 0.6f);
            hintTmp.alignment = TextAlignmentOptions.Center;
            hintTmp.fontStyle = FontStyles.Italic;
        }

        private void CreateNameInput(Transform parent)
        {
            // Label
            GameObject labelObj = new GameObject("NameLabel");
            labelObj.transform.SetParent(parent, false);

            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.5f, 1f);
            labelRect.anchorMax = new Vector2(0.5f, 1f);
            labelRect.anchoredPosition = new Vector2(0, -310);
            labelRect.sizeDelta = new Vector2(400, 30);

            TextMeshProUGUI labelTmp = labelObj.AddComponent<TextMeshProUGUI>();
            labelTmp.text = "DISPLAY NAME";
            labelTmp.fontSize = 18;
            labelTmp.color = AccentCyan;
            labelTmp.alignment = TextAlignmentOptions.Center;
            labelTmp.fontStyle = FontStyles.Bold;

            // Input field container
            GameObject inputContainer = new GameObject("InputContainer");
            inputContainer.transform.SetParent(parent, false);

            RectTransform containerRect = inputContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 1f);
            containerRect.anchorMax = new Vector2(0.5f, 1f);
            containerRect.anchoredPosition = new Vector2(0, -365);
            containerRect.sizeDelta = new Vector2(400, 60);

            Image containerImg = inputContainer.AddComponent<Image>();
            containerImg.color = new Color(0.08f, 0.10f, 0.16f, 0.95f);

            Outline inputBorder = inputContainer.AddComponent<Outline>();
            inputBorder.effectColor = new Color(AccentCyan.r, AccentCyan.g, AccentCyan.b, 0.5f);
            inputBorder.effectDistance = new Vector2(1.5f, -1.5f);

            // Text area
            GameObject textAreaObj = new GameObject("TextArea");
            textAreaObj.transform.SetParent(inputContainer.transform, false);

            RectTransform textAreaRect = textAreaObj.AddComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = new Vector2(15, 5);
            textAreaRect.offsetMax = new Vector2(-15, -5);

            // Input text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(textAreaObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI textTmp = textObj.AddComponent<TextMeshProUGUI>();
            textTmp.fontSize = 28;
            textTmp.color = TextWhite;
            textTmp.alignment = TextAlignmentOptions.Left;

            // Placeholder
            GameObject placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(textAreaObj.transform, false);

            RectTransform placeholderRect = placeholderObj.AddComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI placeholderTmp = placeholderObj.AddComponent<TextMeshProUGUI>();
            placeholderTmp.text = "Enter your name...";
            placeholderTmp.fontSize = 28;
            placeholderTmp.color = new Color(TextWhite.r, TextWhite.g, TextWhite.b, 0.4f);
            placeholderTmp.alignment = TextAlignmentOptions.Left;
            placeholderTmp.fontStyle = FontStyles.Italic;

            // Create input field
            nameInput = inputContainer.AddComponent<TMP_InputField>();
            nameInput.textViewport = textAreaRect;
            nameInput.textComponent = textTmp;
            nameInput.placeholder = placeholderTmp;
            nameInput.characterLimit = 20;
            nameInput.contentType = TMP_InputField.ContentType.Standard;
            nameInput.onValueChanged.AddListener(OnNameChanged);
        }

        private void CreateStatsSection(Transform parent)
        {
            // Stats container
            GameObject statsContainer = new GameObject("StatsContainer");
            statsContainer.transform.SetParent(parent, false);

            RectTransform containerRect = statsContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 1f);
            containerRect.anchorMax = new Vector2(0.5f, 1f);
            containerRect.anchoredPosition = new Vector2(0, -480);
            containerRect.sizeDelta = new Vector2(450, 120);

            Image containerBg = statsContainer.AddComponent<Image>();
            containerBg.color = new Color(0, 0, 0, 0.3f);

            // Stats title
            GameObject titleObj = new GameObject("StatsTitle");
            titleObj.transform.SetParent(statsContainer.transform, false);

            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0, -20);
            titleRect.sizeDelta = new Vector2(400, 30);

            TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "STATISTICS";
            titleTmp.fontSize = 18;
            titleTmp.color = AccentCyan;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.fontStyle = FontStyles.Bold;

            // Stats text
            GameObject statsObj = new GameObject("Stats");
            statsObj.transform.SetParent(statsContainer.transform, false);

            RectTransform statsRect = statsObj.AddComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0.5f, 0.5f);
            statsRect.anchorMax = new Vector2(0.5f, 0.5f);
            statsRect.anchoredPosition = new Vector2(0, -10);
            statsRect.sizeDelta = new Vector2(400, 70);

            statsText = statsObj.AddComponent<TextMeshProUGUI>();
            statsText.text = "Games: 0  |  Wins: 0  |  Win Rate: 0%";
            statsText.fontSize = 20;
            statsText.color = TextWhite;
            statsText.alignment = TextAlignmentOptions.Center;
        }

        private void CreateButtons(Transform parent)
        {
            // Save button
            CreateButton(parent, "SAVE", new Vector2(0, -610), true, OnSaveClicked);

            // Cancel button (only show if not first time)
            if (!isNewProfile)
            {
                CreateButton(parent, "CANCEL", new Vector2(0, -670), false, OnCancelClicked);
            }
        }

        private void CreateButton(Transform parent, string text, Vector2 position, bool isPrimary, UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnObj = new GameObject($"Button_{text}");
            btnObj.transform.SetParent(parent, false);

            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = isPrimary ? new Vector2(280, 60) : new Vector2(200, 45);

            Image img = btnObj.AddComponent<Image>();
            img.sprite = CreateRoundedRectSprite(128, 48, 12);
            img.type = Image.Type.Sliced;
            img.color = isPrimary ? AccentCyan : new Color(AccentCyan.r, AccentCyan.g, AccentCyan.b, 0.2f);

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;

            ColorBlock colors = btn.colors;
            if (isPrimary)
            {
                colors.normalColor = AccentCyan;
                colors.highlightedColor = new Color(0.55f, 0.85f, 1f, 1f);
                colors.pressedColor = new Color(AccentCyan.r * 0.8f, AccentCyan.g * 0.8f, AccentCyan.b * 0.8f);
            }
            else
            {
                colors.normalColor = new Color(1, 1, 1, 0.2f);
                colors.highlightedColor = new Color(1, 1, 1, 0.3f);
                colors.pressedColor = new Color(1, 1, 1, 0.15f);
            }
            btn.colors = colors;

            btn.onClick.AddListener(() => {
                SoundManager.Instance?.PlayButtonClick();
                onClick?.Invoke();
            });

            if (!isPrimary)
            {
                Outline outline = btnObj.AddComponent<Outline>();
                outline.effectColor = new Color(AccentCyan.r, AccentCyan.g, AccentCyan.b, 0.5f);
                outline.effectDistance = new Vector2(1.5f, -1.5f);
            }

            // Button text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = isPrimary ? 28 : 20;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = isPrimary ? DarkBg : TextWhite;
            tmp.fontStyle = FontStyles.Bold;
        }

        private void LoadCurrentProfile()
        {
            var profile = PlayerProfileManager.Instance?.CurrentProfile;
            if (profile == null)
                return;

            // Set name
            if (nameInput != null)
            {
                nameInput.text = profile.DisplayName;
            }

            // Set avatar
            UpdateAvatarDisplay(profile);

            // Set stats
            UpdateStatsDisplay(profile);
        }

        private void UpdateAvatarDisplay(PlayerProfile profile)
        {
            if (avatarImage == null || avatarInitial == null)
                return;

            Sprite avatarSprite = profile.GetAvatarSprite();
            if (avatarSprite != null)
            {
                avatarImage.sprite = avatarSprite;
                avatarImage.color = Color.white;
                avatarInitial.gameObject.SetActive(false);
            }
            else
            {
                avatarImage.sprite = CreateCircleSprite(128);
                avatarImage.color = GetAvatarColor(profile.DisplayName);
                avatarInitial.text = profile.Initial;
                avatarInitial.gameObject.SetActive(true);
            }
        }

        private void UpdateStatsDisplay(PlayerProfile profile)
        {
            if (statsText == null)
                return;

            statsText.text = $"Games: {profile.GamesPlayed}  |  Wins: {profile.GamesWon}  |  Win Rate: {profile.WinRate:F0}%";
        }

        private Color GetAvatarColor(string name)
        {
            // Generate consistent color based on name
            int hash = string.IsNullOrEmpty(name) ? 0 : name.GetHashCode();
            float hue = Mathf.Abs(hash % 360) / 360f;
            return Color.HSVToRGB(hue, 0.5f, 0.7f);
        }

        private void OnNameChanged(string newName)
        {
            // Update avatar initial in real-time
            if (avatarInitial != null && avatarInitial.gameObject.activeSelf)
            {
                avatarInitial.text = string.IsNullOrEmpty(newName) ? "?" : newName[0].ToString().ToUpper();
                avatarImage.color = GetAvatarColor(newName);
            }
        }

        private void OnChangeAvatarClicked()
        {
            // On mobile, this would open camera/gallery picker
            // For now, we'll show a simple color picker or use NativeGallery plugin
            ShowAvatarOptions();
        }

        private void ShowAvatarOptions()
        {
            // Create options popup
            GameObject popupObj = new GameObject("AvatarOptionsPopup");
            popupObj.transform.SetParent(screenCanvas.transform, false);

            RectTransform popupRect = popupObj.AddComponent<RectTransform>();
            popupRect.anchorMin = Vector2.zero;
            popupRect.anchorMax = Vector2.one;
            popupRect.sizeDelta = Vector2.zero;

            // Background
            Image popupBg = popupObj.AddComponent<Image>();
            popupBg.color = new Color(0, 0, 0, 0.8f);

            Button closeBgBtn = popupObj.AddComponent<Button>();
            closeBgBtn.onClick.AddListener(() => Destroy(popupObj));

            // Options panel - taller to fit avatar presets
            GameObject optionsPanel = new GameObject("Options");
            optionsPanel.transform.SetParent(popupObj.transform, false);

            RectTransform optionsRect = optionsPanel.AddComponent<RectTransform>();
            optionsRect.anchorMin = new Vector2(0.5f, 0.5f);
            optionsRect.anchorMax = new Vector2(0.5f, 0.5f);
            optionsRect.sizeDelta = new Vector2(420, 360);

            Image optionsBg = optionsPanel.AddComponent<Image>();
            optionsBg.color = DeepNavy;

            Outline optionsBorder = optionsPanel.AddComponent<Outline>();
            optionsBorder.effectColor = AccentCyan;
            optionsBorder.effectDistance = new Vector2(2, -2);

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(optionsPanel.transform, false);

            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1f);
            titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0, -30);
            titleRect.sizeDelta = new Vector2(350, 40);

            TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "CHOOSE AVATAR";
            titleTmp.fontSize = 24;
            titleTmp.color = AccentCyan;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.fontStyle = FontStyles.Bold;

            // Superhero avatars grid
            CreatePresetAvatarsSection(optionsPanel.transform, popupObj);
        }

        // Bundled avatar names (must match files in Resources/Avatars/)
        private static readonly string[] BundledAvatarNames = new string[]
        {
            "blackpanther", "doctorstrange", "goku", "ironman", "itachi", "naruto"
        };

        private void CreatePresetAvatarsSection(Transform parent, GameObject popupToDestroy)
        {
            // Section label
            GameObject labelObj = new GameObject("PresetsLabel");
            labelObj.transform.SetParent(parent, false);

            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.5f, 1f);
            labelRect.anchorMax = new Vector2(0.5f, 1f);
            labelRect.anchoredPosition = new Vector2(0, -70);
            labelRect.sizeDelta = new Vector2(350, 25);

            TextMeshProUGUI labelTmp = labelObj.AddComponent<TextMeshProUGUI>();
            labelTmp.text = "CHOOSE AVATAR";
            labelTmp.fontSize = 16;
            labelTmp.color = new Color(TextWhite.r, TextWhite.g, TextWhite.b, 0.7f);
            labelTmp.alignment = TextAlignmentOptions.Center;

            // Avatar grid container
            GameObject gridObj = new GameObject("AvatarGrid");
            gridObj.transform.SetParent(parent, false);

            RectTransform gridRect = gridObj.AddComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0.5f, 1f);
            gridRect.anchorMax = new Vector2(0.5f, 1f);
            gridRect.anchoredPosition = new Vector2(0, -185);
            gridRect.sizeDelta = new Vector2(380, 160);

            GridLayoutGroup gridLayout = gridObj.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(90, 90);
            gridLayout.spacing = new Vector2(12, 12);
            gridLayout.childAlignment = TextAnchor.MiddleCenter;
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 3;
            gridLayout.padding = new RectOffset(5, 5, 5, 5);

            // Create buttons for each bundled avatar
            foreach (string avatarName in BundledAvatarNames)
            {
                CreateBundledAvatarButton(gridObj.transform, popupToDestroy, avatarName);
            }
        }

        private void CreateBundledAvatarButton(Transform parent, GameObject popupToDestroy, string avatarName)
        {
            GameObject btnObj = new GameObject($"Avatar_{avatarName}");
            btnObj.transform.SetParent(parent, false);

            // Load the avatar texture from Resources
            Texture2D tex = Resources.Load<Texture2D>($"Avatars/{avatarName}");
            Image btnImg = btnObj.AddComponent<Image>();
            if (tex != null)
            {
                btnImg.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f), 100f);
            }
            btnImg.preserveAspect = true;

            // Circular mask
            Mask mask = btnObj.AddComponent<Mask>();
            mask.showMaskGraphic = true;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = btnImg;

            ColorBlock colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            btn.colors = colors;

            string captured = avatarName;
            btn.onClick.AddListener(() => {
                SoundManager.Instance?.PlayButtonClick();
                ApplyBundledAvatar(captured);
                if (popupToDestroy != null) Destroy(popupToDestroy);
            });
        }

        private void ApplyBundledAvatar(string avatarName)
        {
            Texture2D src = Resources.Load<Texture2D>($"Avatars/{avatarName}");
            if (src == null) return;

            // Copy to a readable texture (Resources textures are not readable by default)
            RenderTexture rt = RenderTexture.GetTemporary(src.width, src.height, 0);
            Graphics.Blit(src, rt);
            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, src.width, src.height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            RenderTexture.ReleaseTemporary(rt);

            // Save to profile
            PlayerProfileManager.Instance?.SetAvatar(tex);

            // Update display
            var profile = PlayerProfileManager.Instance?.CurrentProfile;
            if (profile != null)
            {
                UpdateAvatarDisplay(profile);
            }

            // Save the selected avatar name for deterministic use
            PlayerPrefs.SetString("SelectedAvatar", avatarName);
            PlayerPrefs.Save();
        }

        private void CreatePopupButton(Transform parent, string text, Vector2 position, UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnObj = new GameObject($"Btn_{text}");
            btnObj.transform.SetParent(parent, false);

            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(280, 45);

            Image img = btnObj.AddComponent<Image>();
            img.color = new Color(AccentCyan.r, AccentCyan.g, AccentCyan.b, 0.2f);

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => {
                SoundManager.Instance?.PlayButtonClick();
                onClick?.Invoke();
            });

            Outline outline = btnObj.AddComponent<Outline>();
            outline.effectColor = new Color(AccentCyan.r, AccentCyan.g, AccentCyan.b, 0.5f);
            outline.effectDistance = new Vector2(1, -1);

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 20;
            tmp.color = TextWhite;
            tmp.alignment = TextAlignmentOptions.Center;
        }

        private void TakePhoto()
        {
            // Camera not available — apply a random bundled avatar
            Debug.Log("Camera not available - using bundled avatar");
            int idx = UnityEngine.Random.Range(0, BundledAvatarNames.Length);
            ApplyBundledAvatar(BundledAvatarNames[idx]);
        }

        private void ChooseFromGallery()
        {
#if UNITY_ANDROID || UNITY_IOS
            // Try NativeGallery if available
            try
            {
                // Check if NativeGallery is available using reflection
                System.Type nativeGalleryType = System.Type.GetType("NativeGallery, Assembly-CSharp");
                if (nativeGalleryType != null)
                {
                    // NativeGallery is available - use it
                    var getImageMethod = nativeGalleryType.GetMethod("GetImageFromGallery",
                        new System.Type[] { typeof(System.Action<string>), typeof(string), typeof(string) });

                    if (getImageMethod != null)
                    {
                        System.Action<string> callback = (path) => {
                            if (!string.IsNullOrEmpty(path))
                            {
                                LoadImageFromPath(path);
                            }
                        };
                        getImageMethod.Invoke(null, new object[] { callback, "Select Avatar", "image/*" });
                        return;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"NativeGallery not available: {e.Message}");
            }
#endif
            // Fallback - show preset selection instead of random avatar
            Debug.Log("Gallery not available on this platform - showing preset avatars");
            ShowPresetAvatarFallback();
        }

        private void ShowPresetAvatarFallback()
        {
            // Apply the first bundled avatar as default
            ApplyBundledAvatar(BundledAvatarNames[0]);
        }

        private void LoadImageFromPath(string path)
        {
            StartCoroutine(LoadImageCoroutine(path));
        }

        private IEnumerator LoadImageCoroutine(string path)
        {
            yield return null;

            try
            {
                byte[] imageData = System.IO.File.ReadAllBytes(path);
                Texture2D tex = new Texture2D(2, 2);
                tex.LoadImage(imageData);

                // Crop to square and resize
                Texture2D croppedTex = CropToSquare(tex);
                Texture2D resizedTex = ResizeTexture(croppedTex, 256, 256);

                // Clean up
                Destroy(tex);
                if (croppedTex != resizedTex) Destroy(croppedTex);

                // Apply to profile
                PlayerProfileManager.Instance?.SetAvatar(resizedTex);

                // Update display
                var profile = PlayerProfileManager.Instance?.CurrentProfile;
                if (profile != null)
                {
                    UpdateAvatarDisplay(profile);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load image: {e.Message}");
            }
        }

        private void CreateTestAvatar()
        {
            // Create a simple colored texture for testing
            int size = 256;
            Texture2D tex = new Texture2D(size, size);
            Color baseColor = new Color(Random.value * 0.5f + 0.3f, Random.value * 0.5f + 0.3f, Random.value * 0.5f + 0.3f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Simple gradient
                    float t = (float)y / size;
                    Color c = Color.Lerp(baseColor * 0.8f, baseColor * 1.2f, t);
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();

            PlayerProfileManager.Instance?.SetAvatar(tex);

            var profile = PlayerProfileManager.Instance?.CurrentProfile;
            if (profile != null)
            {
                UpdateAvatarDisplay(profile);
            }
        }

        private void RemoveAvatar()
        {
            PlayerProfileManager.Instance?.ClearAvatar();

            var profile = PlayerProfileManager.Instance?.CurrentProfile;
            if (profile != null)
            {
                UpdateAvatarDisplay(profile);
            }
        }

        private Texture2D CropToSquare(Texture2D source)
        {
            int size = Mathf.Min(source.width, source.height);
            int xOffset = (source.width - size) / 2;
            int yOffset = (source.height - size) / 2;

            Texture2D result = new Texture2D(size, size);
            result.SetPixels(source.GetPixels(xOffset, yOffset, size, size));
            result.Apply();
            return result;
        }

        private Texture2D ResizeTexture(Texture2D source, int newWidth, int newHeight)
        {
            RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight);
            RenderTexture.active = rt;
            Graphics.Blit(source, rt);

            Texture2D result = new Texture2D(newWidth, newHeight);
            result.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
            result.Apply();

            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);
            return result;
        }

        private void OnSaveClicked()
        {
            string newName = nameInput?.text?.Trim();

            // Validate name - don't allow empty or default "Player"
            if (string.IsNullOrEmpty(newName) || newName.ToLower() == "player")
            {
                ShowNameError("Please enter a unique name");
                return;
            }

            // Name must be at least 2 characters
            if (newName.Length < 2)
            {
                ShowNameError("Name must be at least 2 characters");
                return;
            }

            PlayerProfileManager.Instance?.SetDisplayName(newName);
            OnProfileSaved?.Invoke();
            Hide();
        }

        private void ShowNameError(string message)
        {
            // Find or create error label
            Transform existingError = screenPanel?.transform.Find("NameError");
            if (existingError != null)
            {
                existingError.GetComponent<TextMeshProUGUI>().text = message;
                return;
            }

            GameObject errorObj = new GameObject("NameError");
            errorObj.transform.SetParent(screenPanel.transform, false);

            RectTransform rect = errorObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0, -400);
            rect.sizeDelta = new Vector2(400, 30);

            TextMeshProUGUI tmp = errorObj.AddComponent<TextMeshProUGUI>();
            tmp.text = message;
            tmp.fontSize = 18;
            tmp.color = new Color(1f, 0.4f, 0.4f);  // Red
            tmp.alignment = TextAlignmentOptions.Center;

            // Clear error after 3 seconds
            StartCoroutine(ClearErrorAfterDelay(errorObj, 3f));
        }

        private IEnumerator ClearErrorAfterDelay(GameObject errorObj, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (errorObj != null)
            {
                Destroy(errorObj);
            }
        }

        private void OnCancelClicked()
        {
            Hide();
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
                    if (dist <= radius)
                        tex.SetPixel(x, y, Color.white);
                    else
                        tex.SetPixel(x, y, Color.clear);
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }

        private Sprite CreateRoundedRectSprite(int width, int height, int radius)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool inside = IsInsideRoundedRect(x, y, width, height, radius);
                    tex.SetPixel(x, y, inside ? Color.white : Color.clear);
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }

        private bool IsInsideRoundedRect(int x, int y, int width, int height, int radius)
        {
            if (x < 0 || y < 0 || x >= width || y >= height) return false;

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
    }
}
