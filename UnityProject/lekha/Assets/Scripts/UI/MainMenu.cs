using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Lekha.Audio;

namespace Lekha.UI
{
    /// <summary>
    /// Modern 2026 main menu with glassmorphism design
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        public static MainMenu Instance { get; private set; }

        private Canvas menuCanvas;
        private GameObject menuPanel;
        private GameObject settingsPanel;
        private CanvasGroup canvasGroup;

        // Settings
        private Slider volumeSlider;
        private Toggle soundToggle;

        // Animation
        private float animationTime = 0f;
        private bool isAnimating = false;
        private bool isShowing = false;

        public System.Action OnPlayClicked;

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
            CreateMainMenu();
        }

        private void Update()
        {
            // Handle fade animation
            if (isAnimating)
            {
                animationTime += Time.unscaledDeltaTime * 4f;
                float t = Mathf.SmoothStep(0, 1, Mathf.Min(1f, animationTime));

                if (isShowing)
                {
                    canvasGroup.alpha = t;
                }
                else
                {
                    canvasGroup.alpha = 1f - t;
                    if (animationTime >= 1f)
                    {
                        menuCanvas.gameObject.SetActive(false);
                    }
                }

                if (animationTime >= 1f)
                {
                    isAnimating = false;
                }
            }
        }

        private void CreateMainMenu()
        {
            // Create Canvas
            GameObject canvasObj = new GameObject("MenuCanvas");
            canvasObj.transform.SetParent(transform);
            menuCanvas = canvasObj.AddComponent<Canvas>();
            menuCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            menuCanvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            canvasGroup = canvasObj.AddComponent<CanvasGroup>();

            // Create main menu panel
            CreateMenuPanel(canvasObj.transform);

            // Create settings panel (hidden initially)
            CreateSettingsPanel(canvasObj.transform);
            settingsPanel.SetActive(false);
        }

        private void CreateMenuPanel(Transform parent)
        {
            menuPanel = new GameObject("MenuPanel");
            menuPanel.transform.SetParent(parent, false);

            RectTransform panelRect = menuPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;

            // Modern gradient background
            Image bg = menuPanel.AddComponent<Image>();
            if (ModernUITheme.Instance != null && ModernUITheme.Instance.TableSprite != null)
            {
                bg.sprite = ModernUITheme.Instance.TableSprite;
                bg.color = Color.white;
            }
            else
            {
                bg.color = ModernUITheme.PrimaryDark;
            }

            // Add decorative elements
            CreateBackgroundDecoration(menuPanel.transform);

            // Title
            CreateTitle(menuPanel.transform);

            // Center panel with buttons
            CreateCenterPanel(menuPanel.transform);

            // Version text
            CreateVersionText(menuPanel.transform);
        }

        private void CreateBackgroundDecoration(Transform parent)
        {
            // Center glow effect
            GameObject glowObj = new GameObject("CenterGlow");
            glowObj.transform.SetParent(parent, false);

            RectTransform glowRect = glowObj.AddComponent<RectTransform>();
            glowRect.anchorMin = new Vector2(0.5f, 0.5f);
            glowRect.anchorMax = new Vector2(0.5f, 0.5f);
            glowRect.sizeDelta = new Vector2(1200, 900);

            Image glowImg = glowObj.AddComponent<Image>();
            if (ModernUITheme.Instance != null && ModernUITheme.Instance.SoftGlowSprite != null)
            {
                glowImg.sprite = ModernUITheme.Instance.SoftGlowSprite;
            }
            glowImg.color = new Color(0.3f, 0.6f, 0.4f, 0.15f);
            glowImg.raycastTarget = false;
        }

        private void CreateTitle(Transform parent)
        {
            // Title container
            GameObject titleContainer = new GameObject("TitleContainer");
            titleContainer.transform.SetParent(parent, false);

            RectTransform containerRect = titleContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 1);
            containerRect.anchorMax = new Vector2(0.5f, 1);
            containerRect.anchoredPosition = new Vector2(0, -120);
            containerRect.sizeDelta = new Vector2(600, 180);

            // Main title with glow
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(titleContainer.transform, false);

            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = new Vector2(0, 20);
            titleRect.sizeDelta = new Vector2(600, 120);

            TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "LEKHA";
            titleTmp.fontSize = 96;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = ModernUITheme.AccentGold;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.characterSpacing = 12;

            // Subtitle
            GameObject subObj = new GameObject("Subtitle");
            subObj.transform.SetParent(titleContainer.transform, false);

            RectTransform subRect = subObj.AddComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0.5f, 0.5f);
            subRect.anchorMax = new Vector2(0.5f, 0.5f);
            subRect.anchoredPosition = new Vector2(0, -50);
            subRect.sizeDelta = new Vector2(400, 40);

            TextMeshProUGUI subTmp = subObj.AddComponent<TextMeshProUGUI>();
            subTmp.text = "Fo222 Game";
            subTmp.fontSize = 22;
            subTmp.alignment = TextAlignmentOptions.Center;
            subTmp.color = ModernUITheme.TextSecondary;
            subTmp.fontStyle = FontStyles.Italic;
            subTmp.characterSpacing = 2;
        }

        private void CreateCenterPanel(Transform parent)
        {
            // Glass panel for buttons
            GameObject centerPanel = new GameObject("CenterPanel");
            centerPanel.transform.SetParent(parent, false);

            RectTransform centerRect = centerPanel.AddComponent<RectTransform>();
            centerRect.anchorMin = new Vector2(0.5f, 0.5f);
            centerRect.anchorMax = new Vector2(0.5f, 0.5f);
            centerRect.anchoredPosition = new Vector2(0, -30);
            centerRect.sizeDelta = new Vector2(340, 320);

            Image centerImg = centerPanel.AddComponent<Image>();
            if (ModernUITheme.Instance != null && ModernUITheme.Instance.GlassPanelSprite != null)
            {
                centerImg.sprite = ModernUITheme.Instance.GlassPanelSprite;
                centerImg.type = Image.Type.Sliced;
            }
            centerImg.color = new Color(1f, 1f, 1f, 0.05f);

            // Buttons
            CreateModernButton(centerPanel.transform, "Play", new Vector2(0, 80), OnPlayButtonClicked, true);
            CreateModernButton(centerPanel.transform, "Settings", new Vector2(0, 0), OnSettingsButtonClicked, false);
            CreateModernButton(centerPanel.transform, "How to Play", new Vector2(0, -80), OnHowToPlayClicked, false);
        }

        private void CreateModernButton(Transform parent, string text, Vector2 position, UnityEngine.Events.UnityAction onClick, bool isPrimary)
        {
            GameObject btnObj = new GameObject($"Button_{text}");
            btnObj.transform.SetParent(parent, false);

            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(280, 56);

            Image img = btnObj.AddComponent<Image>();

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;

            if (isPrimary)
            {
                // Primary button with gold styling
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
                }
                else
                {
                    img.color = ModernUITheme.AccentGold;
                    ColorBlock colors = btn.colors;
                    colors.normalColor = ModernUITheme.AccentGold;
                    colors.highlightedColor = new Color(1f, 0.85f, 0.4f, 1f);
                    colors.pressedColor = ModernUITheme.AccentGoldDark;
                    btn.colors = colors;
                }

                Shadow shadow = btnObj.AddComponent<Shadow>();
                shadow.effectColor = new Color(0, 0, 0, 0.4f);
                shadow.effectDistance = new Vector2(0, -4);
            }
            else
            {
                // Secondary button with glass styling
                if (ModernUITheme.Instance != null && ModernUITheme.Instance.GlassPanelDarkSprite != null)
                {
                    img.sprite = ModernUITheme.Instance.GlassPanelDarkSprite;
                    img.type = Image.Type.Sliced;
                    img.color = new Color(1f, 1f, 1f, 0.15f);

                    ColorBlock colors = btn.colors;
                    colors.normalColor = new Color(1f, 1f, 1f, 0.15f);
                    colors.highlightedColor = new Color(1f, 1f, 1f, 0.25f);
                    colors.pressedColor = new Color(1f, 1f, 1f, 0.1f);
                    btn.colors = colors;
                }
                else
                {
                    img.color = new Color(1f, 1f, 1f, 0.15f);
                }
            }

            btn.onClick.AddListener(() => {
                SoundManager.Instance?.PlayButtonClick();
                onClick?.Invoke();
            });

            // Button text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 24;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = isPrimary ? ModernUITheme.PrimaryDark : ModernUITheme.TextPrimary;
            tmp.fontStyle = FontStyles.Bold;
        }

        private void CreateVersionText(Transform parent)
        {
            GameObject versionObj = new GameObject("Version");
            versionObj.transform.SetParent(parent, false);

            RectTransform rect = versionObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0, 25);
            rect.sizeDelta = new Vector2(300, 30);

            TextMeshProUGUI tmp = versionObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "Version 1.0.0";
            tmp.fontSize = 14;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(ModernUITheme.TextSecondary.r, ModernUITheme.TextSecondary.g, ModernUITheme.TextSecondary.b, 0.5f);
        }

        private void CreateSettingsPanel(Transform parent)
        {
            settingsPanel = new GameObject("SettingsPanel");
            settingsPanel.transform.SetParent(parent, false);

            RectTransform panelRect = settingsPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;

            // Background
            Image bg = settingsPanel.AddComponent<Image>();
            if (ModernUITheme.Instance != null && ModernUITheme.Instance.TableSprite != null)
            {
                bg.sprite = ModernUITheme.Instance.TableSprite;
                bg.color = Color.white;
            }
            else
            {
                bg.color = ModernUITheme.PrimaryDark;
            }

            // Settings center panel
            GameObject centerPanel = new GameObject("SettingsCenterPanel");
            centerPanel.transform.SetParent(settingsPanel.transform, false);

            RectTransform centerRect = centerPanel.AddComponent<RectTransform>();
            centerRect.anchorMin = new Vector2(0.5f, 0.5f);
            centerRect.anchorMax = new Vector2(0.5f, 0.5f);
            centerRect.anchoredPosition = Vector2.zero;
            centerRect.sizeDelta = new Vector2(400, 420);

            Image centerImg = centerPanel.AddComponent<Image>();
            if (ModernUITheme.Instance != null && ModernUITheme.Instance.GlassPanelDarkSprite != null)
            {
                centerImg.sprite = ModernUITheme.Instance.GlassPanelDarkSprite;
                centerImg.type = Image.Type.Sliced;
            }
            centerImg.color = new Color(0.08f, 0.09f, 0.14f, 0.95f);

            Shadow shadow = centerPanel.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.5f);
            shadow.effectDistance = new Vector2(0, -8);

            // Title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(centerPanel.transform, false);

            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1);
            titleRect.anchorMax = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -50);
            titleRect.sizeDelta = new Vector2(300, 60);

            TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "SETTINGS";
            titleTmp.fontSize = 32;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = ModernUITheme.AccentGold;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.characterSpacing = 4;

            // Sound toggle
            CreateSoundToggle(centerPanel.transform);

            // Volume slider
            CreateVolumeSlider(centerPanel.transform);

            // Back button
            CreateModernButton(centerPanel.transform, "Back", new Vector2(0, -140), OnBackFromSettings, true);
        }

        private void CreateSoundToggle(Transform parent)
        {
            GameObject toggleObj = new GameObject("SoundToggle");
            toggleObj.transform.SetParent(parent, false);

            RectTransform rect = toggleObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0, 60);
            rect.sizeDelta = new Vector2(300, 50);

            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(toggleObj.transform, false);

            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0.5f);
            labelRect.anchorMax = new Vector2(0, 0.5f);
            labelRect.anchoredPosition = new Vector2(20, 0);
            labelRect.sizeDelta = new Vector2(150, 40);

            TextMeshProUGUI labelTmp = labelObj.AddComponent<TextMeshProUGUI>();
            labelTmp.text = "Sound";
            labelTmp.fontSize = 22;
            labelTmp.alignment = TextAlignmentOptions.MidlineLeft;
            labelTmp.color = ModernUITheme.TextPrimary;

            // Toggle background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(toggleObj.transform, false);

            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(1, 0.5f);
            bgRect.anchorMax = new Vector2(1, 0.5f);
            bgRect.anchoredPosition = new Vector2(-50, 0);
            bgRect.sizeDelta = new Vector2(60, 30);

            Image bgImg = bgObj.AddComponent<Image>();
            if (ModernUITheme.Instance != null && ModernUITheme.Instance.PillSprite != null)
            {
                bgImg.sprite = ModernUITheme.Instance.PillSprite;
                bgImg.type = Image.Type.Sliced;
            }
            bgImg.color = new Color(0.3f, 0.3f, 0.4f, 0.8f);

            // Checkmark
            GameObject checkObj = new GameObject("Checkmark");
            checkObj.transform.SetParent(bgObj.transform, false);

            RectTransform checkRect = checkObj.AddComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0.5f, 0.5f);
            checkRect.anchorMax = new Vector2(0.5f, 0.5f);
            checkRect.anchoredPosition = Vector2.zero;
            checkRect.sizeDelta = new Vector2(20, 20);

            Image checkImg = checkObj.AddComponent<Image>();
            if (ModernUITheme.Instance != null && ModernUITheme.Instance.CircleSprite != null)
            {
                checkImg.sprite = ModernUITheme.Instance.CircleSprite;
            }
            checkImg.color = ModernUITheme.AccentGold;

            // Toggle component
            soundToggle = toggleObj.AddComponent<Toggle>();
            soundToggle.targetGraphic = bgImg;
            soundToggle.graphic = checkImg;
            soundToggle.isOn = true;
            soundToggle.onValueChanged.AddListener(OnSoundToggled);
        }

        private void CreateVolumeSlider(Transform parent)
        {
            GameObject sliderObj = new GameObject("VolumeSlider");
            sliderObj.transform.SetParent(parent, false);

            RectTransform rect = sliderObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0, -10);
            rect.sizeDelta = new Vector2(300, 50);

            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(sliderObj.transform, false);

            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0.5f);
            labelRect.anchorMax = new Vector2(0, 0.5f);
            labelRect.anchoredPosition = new Vector2(20, 0);
            labelRect.sizeDelta = new Vector2(100, 40);

            TextMeshProUGUI labelTmp = labelObj.AddComponent<TextMeshProUGUI>();
            labelTmp.text = "Volume";
            labelTmp.fontSize = 22;
            labelTmp.alignment = TextAlignmentOptions.MidlineLeft;
            labelTmp.color = ModernUITheme.TextPrimary;

            // Slider background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(sliderObj.transform, false);

            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0.35f, 0.5f);
            bgRect.anchorMax = new Vector2(1, 0.5f);
            bgRect.anchoredPosition = new Vector2(-20, 0);
            bgRect.sizeDelta = new Vector2(0, 8);

            Image bgImg = bgObj.AddComponent<Image>();
            if (ModernUITheme.Instance != null && ModernUITheme.Instance.PillSprite != null)
            {
                bgImg.sprite = ModernUITheme.Instance.PillSprite;
                bgImg.type = Image.Type.Sliced;
            }
            bgImg.color = new Color(0.2f, 0.22f, 0.28f, 1f);

            // Fill area
            GameObject fillArea = new GameObject("FillArea");
            fillArea.transform.SetParent(bgObj.transform, false);

            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.sizeDelta = Vector2.zero;

            // Fill
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(fillArea.transform, false);

            RectTransform fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0.7f, 1);
            fillRect.sizeDelta = Vector2.zero;

            Image fillImg = fillObj.AddComponent<Image>();
            if (ModernUITheme.Instance != null && ModernUITheme.Instance.PillSprite != null)
            {
                fillImg.sprite = ModernUITheme.Instance.PillSprite;
                fillImg.type = Image.Type.Sliced;
            }
            fillImg.color = ModernUITheme.AccentGold;

            // Handle area
            GameObject handleArea = new GameObject("HandleArea");
            handleArea.transform.SetParent(bgObj.transform, false);

            RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.sizeDelta = Vector2.zero;

            // Handle
            GameObject handleObj = new GameObject("Handle");
            handleObj.transform.SetParent(handleArea.transform, false);

            RectTransform handleRect = handleObj.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20, 20);

            Image handleImg = handleObj.AddComponent<Image>();
            if (ModernUITheme.Instance != null && ModernUITheme.Instance.CircleSprite != null)
            {
                handleImg.sprite = ModernUITheme.Instance.CircleSprite;
            }
            handleImg.color = Color.white;

            // Slider component
            volumeSlider = bgObj.AddComponent<Slider>();
            volumeSlider.fillRect = fillRect;
            volumeSlider.handleRect = handleRect;
            volumeSlider.minValue = 0;
            volumeSlider.maxValue = 1;
            volumeSlider.value = 0.7f;
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

        private void OnPlayButtonClicked()
        {
            Hide();
            OnPlayClicked?.Invoke();
        }

        private void OnSettingsButtonClicked()
        {
            menuPanel.SetActive(false);
            settingsPanel.SetActive(true);
        }

        private void OnHowToPlayClicked()
        {
            ShowHowToPlay();
        }

        private void OnBackFromSettings()
        {
            settingsPanel.SetActive(false);
            menuPanel.SetActive(true);
        }

        private void OnSoundToggled(bool isOn)
        {
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.SetMasterVolume(isOn ? volumeSlider.value : 0);
            }
        }

        private void OnVolumeChanged(float value)
        {
            if (SoundManager.Instance != null && soundToggle.isOn)
            {
                SoundManager.Instance.SetMasterVolume(value);
            }
        }

        private void ShowHowToPlay()
        {
            // Modern how to play overlay
            GameObject overlay = new GameObject("HowToPlayOverlay");
            overlay.transform.SetParent(menuCanvas.transform, false);

            RectTransform overlayRect = overlay.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.sizeDelta = Vector2.zero;

            Image overlayBg = overlay.AddComponent<Image>();
            overlayBg.color = new Color(0.02f, 0.03f, 0.06f, 0.9f);

            // Content panel
            GameObject contentPanel = new GameObject("ContentPanel");
            contentPanel.transform.SetParent(overlay.transform, false);

            RectTransform panelRect = contentPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.1f, 0.08f);
            panelRect.anchorMax = new Vector2(0.9f, 0.92f);
            panelRect.sizeDelta = Vector2.zero;

            Image panelImg = contentPanel.AddComponent<Image>();
            if (ModernUITheme.Instance != null && ModernUITheme.Instance.GlassPanelDarkSprite != null)
            {
                panelImg.sprite = ModernUITheme.Instance.GlassPanelDarkSprite;
                panelImg.type = Image.Type.Sliced;
            }
            panelImg.color = new Color(0.08f, 0.09f, 0.14f, 0.95f);

            // Content text
            GameObject content = new GameObject("Content");
            content.transform.SetParent(contentPanel.transform, false);

            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.05f, 0.1f);
            contentRect.anchorMax = new Vector2(0.95f, 0.9f);
            contentRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI contentTmp = content.AddComponent<TextMeshProUGUI>();
            contentTmp.text = $@"<size=42><color=#{ColorUtility.ToHtmlStringRGB(ModernUITheme.AccentGold)}>How to Play Lekha</color></size>

<size=24>Lekha is a 4-player trick-taking card game played in teams.

<color=#{ColorUtility.ToHtmlStringRGB(ModernUITheme.AccentCyan)}><b>Teams:</b></color>
- You (South) and Partner (North) vs West and East

<color=#{ColorUtility.ToHtmlStringRGB(ModernUITheme.AccentCyan)}><b>Cards:</b></color>
- Uses Uno-styled cards mapped to traditional suits
- Red = Hearts, Yellow = Diamonds, Blue = Spades, Green = Clubs

<color=#{ColorUtility.ToHtmlStringRGB(ModernUITheme.AccentCyan)}><b>Gameplay:</b></color>
1. Each player receives 13 cards
2. Pass 3 cards to the player on your right
3. Play tricks - must follow suit if possible
4. Highest card of the led suit wins the trick

<color=#{ColorUtility.ToHtmlStringRGB(ModernUITheme.AccentCyan)}><b>Scoring:</b></color>
- Hearts: 1 point each
- Queen of Spades (Blue +2): 13 points
- First team to 101 points <color=#F55>LOSES!</color>

<color=#{ColorUtility.ToHtmlStringRGB(ModernUITheme.TextSecondary)}><i>Tap anywhere to close</i></color></size>";
            contentTmp.alignment = TextAlignmentOptions.Center;
            contentTmp.color = ModernUITheme.TextPrimary;

            // Close on tap
            Button closeBtn = overlay.AddComponent<Button>();
            closeBtn.onClick.AddListener(() => {
                SoundManager.Instance?.PlayButtonClick();
                Destroy(overlay);
            });
        }

        public void Show()
        {
            menuCanvas.gameObject.SetActive(true);
            menuPanel.SetActive(true);
            settingsPanel.SetActive(false);

            // Fade in animation
            isShowing = true;
            isAnimating = true;
            animationTime = 0f;
            canvasGroup.alpha = 0;
        }

        public void Hide()
        {
            // Fade out animation
            isShowing = false;
            isAnimating = true;
            animationTime = 0f;
        }
    }
}
