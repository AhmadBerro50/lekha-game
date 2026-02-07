using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Lekha.Audio;
using Lekha.GameLogic;

namespace Lekha.UI
{
    /// <summary>
    /// Modern 2026 pause menu with glassmorphism design
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        public static PauseMenu Instance { get; private set; }

        private Canvas pauseCanvas;
        private GameObject pausePanel;
        private GameObject centerPanel;
        private CanvasGroup canvasGroup;
        private bool isPaused = false;
        private float animationTime = 0f;
        private bool isAnimating = false;

        public bool IsPaused => isPaused;

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
            CreatePauseMenu();
            Hide();
        }

        private void Update()
        {
            // Check for escape key to toggle pause
            if (UnityEngine.InputSystem.Keyboard.current != null &&
                UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                TogglePause();
            }

            // Handle animation
            if (isAnimating)
            {
                animationTime += Time.unscaledDeltaTime * 5f;
                float t = Mathf.SmoothStep(0, 1, Mathf.Min(1f, animationTime));

                if (isPaused)
                {
                    canvasGroup.alpha = t;
                    if (centerPanel != null)
                    {
                        centerPanel.transform.localScale = Vector3.Lerp(Vector3.one * 0.8f, Vector3.one, t);
                    }
                }
                else
                {
                    canvasGroup.alpha = 1f - t;
                    if (centerPanel != null)
                    {
                        centerPanel.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.9f, t);
                    }
                }

                if (animationTime >= 1f)
                {
                    isAnimating = false;
                    if (!isPaused)
                    {
                        pauseCanvas.gameObject.SetActive(false);
                    }
                }
            }
        }

        private void CreatePauseMenu()
        {
            // Create Canvas
            GameObject canvasObj = new GameObject("PauseCanvas");
            canvasObj.transform.SetParent(transform);
            pauseCanvas = canvasObj.AddComponent<Canvas>();
            pauseCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            pauseCanvas.sortingOrder = 200;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            canvasGroup = canvasObj.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0;

            // Dark overlay background
            pausePanel = new GameObject("PausePanel");
            pausePanel.transform.SetParent(canvasObj.transform, false);

            RectTransform panelRect = pausePanel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;

            Image bg = pausePanel.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.05f, 0.10f, 0.88f); // Modern dark overlay

            // Click overlay to resume (optional - can be removed if not desired)
            Button overlayBtn = pausePanel.AddComponent<Button>();
            overlayBtn.targetGraphic = bg;
            overlayBtn.transition = Selectable.Transition.None;
            // overlayBtn.onClick.AddListener(Resume); // Uncomment to allow click-to-close

            // Center panel with modern glass effect
            centerPanel = new GameObject("CenterPanel");
            centerPanel.transform.SetParent(pausePanel.transform, false);

            RectTransform centerRect = centerPanel.AddComponent<RectTransform>();
            centerRect.anchorMin = new Vector2(0.5f, 0.5f);
            centerRect.anchorMax = new Vector2(0.5f, 0.5f);
            centerRect.anchoredPosition = Vector2.zero;
            centerRect.sizeDelta = new Vector2(380, 380);

            Image centerImg = centerPanel.AddComponent<Image>();
            if (ModernUITheme.Instance != null && ModernUITheme.Instance.GlassPanelDarkSprite != null)
            {
                centerImg.sprite = ModernUITheme.Instance.GlassPanelDarkSprite;
                centerImg.color = new Color(0.12f, 0.14f, 0.22f, 0.95f); // Modern glass
                centerImg.type = Image.Type.Sliced;
            }
            else if (PremiumVisuals.Instance != null && PremiumVisuals.Instance.PanelSprite != null)
            {
                centerImg.sprite = PremiumVisuals.Instance.PanelSprite;
                centerImg.color = new Color(0.12f, 0.14f, 0.22f, 0.95f);
                centerImg.type = Image.Type.Sliced;
            }
            else
            {
                centerImg.color = new Color(0.12f, 0.14f, 0.22f, 0.95f);
            }

            Shadow shadow = centerPanel.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.4f);
            shadow.effectDistance = new Vector2(0, -6);

            // Add cyan glow outline
            Outline outline = centerPanel.AddComponent<Outline>();
            outline.effectColor = new Color(0.40f, 0.75f, 1f, 0.15f);
            outline.effectDistance = new Vector2(2, -2);

            // Cyan accent line at top
            GameObject accentLine = new GameObject("AccentLine");
            accentLine.transform.SetParent(centerPanel.transform, false);
            RectTransform accentRect = accentLine.AddComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0.15f, 1);
            accentRect.anchorMax = new Vector2(0.85f, 1);
            accentRect.sizeDelta = new Vector2(0, 3);
            accentRect.anchoredPosition = new Vector2(0, -1);
            Image accentImg = accentLine.AddComponent<Image>();
            accentImg.color = new Color(0.40f, 0.75f, 1f, 1f); // Cyan accent

            // Title with icon
            CreateTitleSection(centerPanel.transform);

            // Buttons
            CreateModernButton(centerPanel.transform, "Resume", new Vector2(0, -20), OnResumeClicked);
            CreateModernButton(centerPanel.transform, "Main Menu", new Vector2(0, -100), OnMainMenuClicked);

            // Footer text
            GameObject footerObj = new GameObject("Footer");
            footerObj.transform.SetParent(centerPanel.transform, false);
            RectTransform footerRect = footerObj.AddComponent<RectTransform>();
            footerRect.anchorMin = new Vector2(0.5f, 0);
            footerRect.anchorMax = new Vector2(0.5f, 0);
            footerRect.anchoredPosition = new Vector2(0, 30);
            footerRect.sizeDelta = new Vector2(300, 30);

            TextMeshProUGUI footerText = footerObj.AddComponent<TextMeshProUGUI>();
            footerText.text = "Press ESC to resume";
            footerText.fontSize = 14;
            footerText.alignment = TextAlignmentOptions.Center;
            footerText.color = ModernUITheme.TextSecondary;
            footerText.fontStyle = FontStyles.Italic;
        }

        private void CreateTitleSection(Transform parent)
        {
            // Title container
            GameObject titleContainer = new GameObject("TitleContainer");
            titleContainer.transform.SetParent(parent, false);

            RectTransform containerRect = titleContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 1);
            containerRect.anchorMax = new Vector2(0.5f, 1);
            containerRect.anchoredPosition = new Vector2(0, -65);
            containerRect.sizeDelta = new Vector2(300, 70);

            // Pause icon circle
            GameObject iconObj = new GameObject("PauseIcon");
            iconObj.transform.SetParent(titleContainer.transform, false);

            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = new Vector2(0, 10);
            iconRect.sizeDelta = new Vector2(50, 50);

            Image iconBg = iconObj.AddComponent<Image>();
            if (ModernUITheme.Instance != null && ModernUITheme.Instance.CircleSprite != null)
            {
                iconBg.sprite = ModernUITheme.Instance.CircleSprite;
            }
            iconBg.color = new Color(0.85f, 0.45f, 0.95f, 1f); // Magenta accent

            // Pause symbol
            TextMeshProUGUI iconText = ModernUITheme.CreateText(iconObj.transform, "⏸", 26, ModernUITheme.PrimaryDark);
            iconText.fontStyle = FontStyles.Bold;
            RectTransform iconTextRect = iconText.GetComponent<RectTransform>();
            iconTextRect.anchorMin = Vector2.zero;
            iconTextRect.anchorMax = Vector2.one;
            iconTextRect.sizeDelta = Vector2.zero;

            // Title text below icon
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(titleContainer.transform, false);

            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = new Vector2(0, -35);
            titleRect.sizeDelta = new Vector2(200, 40);

            TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "PAUSED";
            titleTmp.fontSize = 24;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = ModernUITheme.TextSecondary;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.characterSpacing = 6;
        }

        private void CreateModernButton(Transform parent, string text, Vector2 position, UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnObj = new GameObject($"Button_{text}");
            btnObj.transform.SetParent(parent, false);

            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(260, 56);

            Image img = btnObj.AddComponent<Image>();

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;

            // Use modern button styling
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

                Shadow shadow = btnObj.AddComponent<Shadow>();
                shadow.effectColor = new Color(0, 0, 0, 0.35f);
                shadow.effectDistance = new Vector2(0, -3);
            }
            else if (PremiumVisuals.Instance != null && PremiumVisuals.Instance.ButtonNormalSprite != null)
            {
                PremiumVisuals.Instance.StyleButton(btn);

                Shadow shadow = btnObj.AddComponent<Shadow>();
                shadow.effectColor = new Color(0, 0, 0, 0.5f);
                shadow.effectDistance = new Vector2(3, -3);
            }
            else
            {
                Color accentCyan = new Color(0.40f, 0.75f, 1f, 1f);
                img.color = accentCyan;
                ColorBlock colors = btn.colors;
                colors.normalColor = accentCyan;
                colors.highlightedColor = new Color(0.55f, 0.85f, 1f, 1f);
                colors.pressedColor = new Color(0.30f, 0.60f, 0.85f, 1f);
                btn.colors = colors;
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
            tmp.fontSize = 22;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = ModernUITheme.PrimaryDark;
            tmp.fontStyle = FontStyles.Bold;
        }

        public void TogglePause()
        {
            if (isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }

        public void Pause()
        {
            isPaused = true;
            pauseCanvas.gameObject.SetActive(true);
            Time.timeScale = 0f;

            // Start fade in animation
            isAnimating = true;
            animationTime = 0f;
            canvasGroup.alpha = 0;
            if (centerPanel != null)
            {
                centerPanel.transform.localScale = Vector3.one * 0.8f;
            }
        }

        public void Resume()
        {
            isPaused = false;
            Time.timeScale = 1f;

            // Start fade out animation
            isAnimating = true;
            animationTime = 0f;
        }

        public void Hide()
        {
            isPaused = false;
            pauseCanvas.gameObject.SetActive(false);
            Time.timeScale = 1f;
        }

        private void OnResumeClicked()
        {
            Resume();
        }

        private void OnMainMenuClicked()
        {
            Resume();
            if (GameController.Instance != null)
            {
                GameController.Instance.ReturnToMainMenu();
            }
        }
    }
}
