using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Lekha.Audio;
using Lekha.Core;
using System.Collections;
using System.Collections.Generic;

namespace Lekha.UI
{
    /// <summary>
    /// Modern 2026 main menu with animated card reveal intro
    /// Glassmorphism aesthetic with vibrant gradients and modern colors
    /// </summary>
    public class PremiumMainMenu : MonoBehaviour
    {
        public static PremiumMainMenu Instance { get; private set; }

        private Canvas menuCanvas;
        private GameObject splashPanel;
        private GameObject menuPanel;
        private GameObject settingsPanel;
        private CanvasGroup menuCanvasGroup;
        private CanvasGroup splashCanvasGroup;

        // Animated elements
        private RectTransform titleTransform;
        private RectTransform[] buttonTransforms;
        private List<RectTransform> introCards = new List<RectTransform>();
        private List<GameObject> backgroundParticles = new List<GameObject>();

        // Settings
        private Slider volumeSlider;
        private Toggle soundToggle;

        // Profile button elements
        private Image profileAvatarBg;
        private TextMeshProUGUI profileAvatarInitial;
        private TextMeshProUGUI profileNameText;

        // State
        private bool splashComplete = false;

        public System.Action OnPlayClicked;

        // Modern 2026 Color Palette
        private static readonly Color DeepNavy = new Color(0.04f, 0.06f, 0.12f, 1f);         // Deep rich background
        private static readonly Color RichPurple = new Color(0.10f, 0.08f, 0.22f, 1f);       // Subtle purple mid
        private static readonly Color AccentCyan = new Color(0.40f, 0.75f, 1f, 1f);          // Bright cyan accent
        private static readonly Color AccentMagenta = new Color(0.85f, 0.45f, 0.95f, 1f);    // Vibrant magenta
        private static readonly Color TextWhite = new Color(1f, 1f, 1f, 1f);                  // Pure white
        private static readonly Color TextMuted = new Color(0.75f, 0.80f, 0.90f, 1f);        // Soft blue-white
        private static readonly Color GlassPanel = new Color(0.15f, 0.18f, 0.25f, 0.85f);    // Glass panel color
        private static readonly Color GlassBorder = new Color(1f, 1f, 1f, 0.18f);            // Glass border

        // Legacy compatibility aliases
        private static readonly Color DeepGreen = DeepNavy;
        private static readonly Color RichBurgundy = RichPurple;
        private static readonly Color LuxuryGold = AccentCyan;
        private static readonly Color BrightGold = new Color(0.50f, 0.85f, 1f, 1f);          // Lighter cyan
        private static readonly Color CreamWhite = TextWhite;
        private static readonly Color DarkWood = DeepNavy;
        private static readonly Color VelvetBlack = new Color(0.03f, 0.04f, 0.08f, 1f);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Keep running when window loses focus (needed for multiplayer + mobile)
            Application.runInBackground = true;

#if UNITY_STANDALONE && !UNITY_EDITOR
            // Force windowed mode on desktop for multi-instance testing
            Screen.fullScreen = false;
            Screen.SetResolution(540, 960, false);
#endif
        }

        private void Start()
        {
            CreateLuxuryMenu();
            StartCoroutine(PlayCardRevealIntro());

            // Subscribe to profile changes
            if (PlayerProfileManager.Instance != null)
            {
                PlayerProfileManager.Instance.OnProfileChanged += OnProfileChanged;
            }
        }

        private void CheckPlayerNameAfterSplash()
        {
            var profile = PlayerProfileManager.Instance?.CurrentProfile;
            if (profile != null)
            {
                string name = profile.DisplayName?.Trim();
                if (string.IsNullOrEmpty(name) || name == "Player")
                {
                    Debug.Log("[PremiumMainMenu] Player name is default, showing profile setup");
                    ShowProfileSetupRequired();
                }
            }
        }

        private void ShowProfileSetupRequired()
        {
            // Create and show profile setup screen as required (first time)
            if (ProfileSetupScreen.Instance == null)
            {
                GameObject profileScreenObj = new GameObject("ProfileSetupScreen");
                profileScreenObj.transform.SetParent(transform);
                ProfileSetupScreen screen = profileScreenObj.AddComponent<ProfileSetupScreen>();
                screen.OnProfileSaved = () => {
                    UpdateProfileButtonDisplay();
                };
            }

            ProfileSetupScreen.Instance?.Show(true);  // true = first time/required
        }

        private void OnDestroy()
        {
            // Unsubscribe from profile changes
            if (PlayerProfileManager.Instance != null)
            {
                PlayerProfileManager.Instance.OnProfileChanged -= OnProfileChanged;
            }

            // Clean up particles
            foreach (var particle in backgroundParticles)
            {
                if (particle != null) Destroy(particle);
            }
            backgroundParticles.Clear();
        }

        private void OnProfileChanged(PlayerProfile profile)
        {
            UpdateProfileButtonDisplay();
        }

        private void CreateLuxuryMenu()
        {
            // Create Canvas
            GameObject canvasObj = new GameObject("LuxuryMenuCanvas");
            canvasObj.transform.SetParent(transform);
            menuCanvas = canvasObj.AddComponent<Canvas>();
            menuCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            menuCanvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            menuCanvasGroup = canvasObj.AddComponent<CanvasGroup>();

            // Create luxurious background
            CreateLuxuryBackground(canvasObj.transform);

            // Create splash screen with card reveal
            CreateSplashScreen(canvasObj.transform);

            // Create main menu panel (hidden initially)
            CreateMainMenuPanel(canvasObj.transform);
            menuPanel.SetActive(false);

            // Create settings panel
            CreateSettingsPanel(canvasObj.transform);
            settingsPanel.SetActive(false);
        }

        private void CreateLuxuryBackground(Transform parent)
        {
            // ── Layer 1: Smooth vertical gradient (dark navy top → deep black bottom) ──
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(parent, false);

            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            Image bgImg = bgObj.AddComponent<Image>();

            int texW = 4, texH = 256;
            Texture2D gradientTex = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
            gradientTex.filterMode = FilterMode.Bilinear;
            gradientTex.wrapMode = TextureWrapMode.Clamp;

            Color topColor    = new Color(0.06f, 0.22f, 0.38f, 1f); // Deep teal top
            Color midColor    = new Color(0.04f, 0.12f, 0.24f, 1f); // Dark ocean mid
            Color bottomColor = new Color(0.02f, 0.05f, 0.12f, 1f); // Midnight bottom

            for (int y = 0; y < texH; y++)
            {
                float t = (float)y / (texH - 1); // 0 = bottom, 1 = top
                Color c;
                if (t < 0.5f)
                    c = Color.Lerp(bottomColor, midColor, t / 0.5f);
                else
                    c = Color.Lerp(midColor, topColor, (t - 0.5f) / 0.5f);

                for (int x = 0; x < texW; x++)
                    gradientTex.SetPixel(x, y, c);
            }
            gradientTex.Apply();
            bgImg.sprite = Sprite.Create(gradientTex, new Rect(0, 0, texW, texH), new Vector2(0.5f, 0.5f));
            bgImg.raycastTarget = false;

            // ── Layer 2: Subtle centre spotlight ──────────────────────────────
            GameObject spotObj = new GameObject("CentreSpot");
            spotObj.transform.SetParent(parent, false);
            RectTransform spotRect = spotObj.AddComponent<RectTransform>();
            spotRect.anchorMin = Vector2.zero;
            spotRect.anchorMax = Vector2.one;
            spotRect.sizeDelta = Vector2.zero;

            Image spotImg = spotObj.AddComponent<Image>();
            int spotSize = 256;
            Texture2D spotTex = new Texture2D(spotSize, spotSize, TextureFormat.RGBA32, false);
            spotTex.filterMode = FilterMode.Bilinear;
            Vector2 sc = new Vector2(spotSize / 2f, spotSize / 2f);
            for (int y = 0; y < spotSize; y++)
                for (int x = 0; x < spotSize; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), sc) / (spotSize / 2f);
                    float a = Mathf.Pow(Mathf.Max(0, 1f - d), 2.5f);
                    spotTex.SetPixel(x, y, new Color(1, 1, 1, a));
                }
            spotTex.Apply();
            spotImg.sprite = Sprite.Create(spotTex, new Rect(0, 0, spotSize, spotSize), new Vector2(0.5f, 0.5f));
            spotImg.color = new Color(0.10f, 0.35f, 0.50f, 0.25f); // Teal centre glow
            spotImg.raycastTarget = false;

            // ── Layer 3: Soft vignette (darkens edges naturally) ──────────────
            CreateVignette(parent);

            // ── Layer 4: Floating particles (fewer, subtler) ─────────────────
            CreateAnimatedBackground(parent);
        }

        private void CreateVignette(Transform parent)
        {
            GameObject vignetteObj = new GameObject("Vignette");
            vignetteObj.transform.SetParent(parent, false);

            RectTransform rect = vignetteObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            Image img = vignetteObj.AddComponent<Image>();

            int size = 256;
            Texture2D vignetteTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            vignetteTex.filterMode = FilterMode.Bilinear;
            Vector2 center = new Vector2(size / 2f, size / 2f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center) / (size / 2f);
                    // Gentle vignette — mostly transparent centre, dark at edges
                    float alpha = Mathf.Pow(Mathf.Clamp01(dist), 2.0f) * 0.55f;
                    vignetteTex.SetPixel(x, y, new Color(0, 0, 0, alpha));
                }
            }
            vignetteTex.Apply();

            img.sprite = Sprite.Create(vignetteTex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            img.raycastTarget = false;
        }

        private void CreateAnimatedBackground(Transform parent)
        {
            GameObject particleContainer = new GameObject("ParticleContainer");
            particleContainer.transform.SetParent(parent, false);

            RectTransform containerRect = particleContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = Vector2.zero;
            containerRect.anchorMax = Vector2.one;
            containerRect.sizeDelta = Vector2.zero;

            // Subtle floating dust particles
            for (int i = 0; i < 15; i++)
            {
                StartCoroutine(CreateFloatingParticle(particleContainer.transform, i));
            }
        }

        private IEnumerator CreateFloatingParticle(Transform parent, int index)
        {
            yield return new WaitForSeconds(index * 0.2f);

            GameObject particle = new GameObject($"GlowParticle_{index}");
            particle.transform.SetParent(parent, false);
            backgroundParticles.Add(particle);

            RectTransform rect = particle.AddComponent<RectTransform>();
            float size = Random.Range(2f, 6f);
            rect.sizeDelta = new Vector2(size, size);

            Image img = particle.AddComponent<Image>();

            // Create soft glow particle
            Texture2D particleTex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(16, 16);
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center) / 16f;
                    float alpha = Mathf.Pow(Mathf.Max(0, 1 - dist), 2.5f);
                    particleTex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
            }
            particleTex.Apply();
            particleTex.filterMode = FilterMode.Bilinear;

            img.sprite = Sprite.Create(particleTex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
            img.raycastTarget = false;

            // Soft white / pale cyan particles (no harsh magenta)
            Color particleColor = Color.Lerp(new Color(0.6f, 0.75f, 1f), TextWhite, Random.Range(0.3f, 0.7f));

            // Animate particle floating upward
            while (particle != null)
            {
                float startX = Random.Range(-900f, 900f);
                float startY = Random.Range(-600f, -400f);
                rect.anchoredPosition = new Vector2(startX, startY);

                float duration = Random.Range(12f, 20f);
                float elapsed = 0;
                float wobbleSpeed = Random.Range(0.5f, 1.5f);
                float wobbleAmount = Random.Range(20f, 60f);
                float maxAlpha = Random.Range(0.1f, 0.3f);

                while (elapsed < duration && particle != null)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / duration;

                    float y = Mathf.Lerp(startY, 600f, t);
                    float x = startX + Mathf.Sin(elapsed * wobbleSpeed) * wobbleAmount;
                    rect.anchoredPosition = new Vector2(x, y);

                    // Fade in and out with gentle pulse
                    float baseAlpha = Mathf.Sin(t * Mathf.PI);
                    float pulse = 1f + Mathf.Sin(elapsed * 3f) * 0.2f;
                    float alpha = baseAlpha * maxAlpha * pulse;
                    img.color = new Color(particleColor.r, particleColor.g, particleColor.b, alpha);

                    yield return null;
                }
            }
        }

        private void CreateCornerOrnaments(Transform parent)
        {
            // Removed — clean gradient background looks better without corner blobs
        }

        private void CreateCornerGlow(Transform parent, Vector2 anchor, Vector2 position, float size, Color glowColor)
        {
            GameObject glowObj = new GameObject("CornerGlow");
            glowObj.transform.SetParent(parent, false);

            RectTransform rect = glowObj.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(size, size);

            Image img = glowObj.AddComponent<Image>();

            // Create radial glow texture
            Texture2D glowTex = CreateCornerGlowTexture(64);
            img.sprite = Sprite.Create(glowTex, new Rect(0, 0, 64, 64), anchor);
            img.color = new Color(glowColor.r, glowColor.g, glowColor.b, 0.15f);
            img.raycastTarget = false;
        }

        private Texture2D CreateCornerGlowTexture(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Distance from corner (0,0)
                    float dist = Vector2.Distance(new Vector2(x, y), Vector2.zero) / size;
                    float alpha = Mathf.Pow(Mathf.Max(0, 1 - dist), 2f);
                    tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
            }

            tex.Apply();
            return tex;
        }

        private void CreateSplashScreen(Transform parent)
        {
            splashPanel = new GameObject("SplashPanel");
            splashPanel.transform.SetParent(parent, false);

            RectTransform panelRect = splashPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;

            splashCanvasGroup = splashPanel.AddComponent<CanvasGroup>();

            // Create intro cards that will animate
            CreateIntroCards(splashPanel.transform);

            // Hidden logo that reveals after cards
            CreateHiddenLogo(splashPanel.transform);
        }

        private void CreateIntroCards(Transform parent)
        {
            // Container for cards
            GameObject cardContainer = new GameObject("CardContainer");
            cardContainer.transform.SetParent(parent, false);

            RectTransform containerRect = cardContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 0.5f);
            containerRect.anchorMax = new Vector2(0.5f, 0.5f);
            containerRect.anchoredPosition = Vector2.zero;
            containerRect.sizeDelta = new Vector2(800, 500);

            // Use actual card images from Resources
            // Cards are stored as: Resources/Cards/{Color}/{Color}_{Number}.jpg
            // Include Queen of Spades (Blue Draw 2) and 10 of Diamonds (Yellow 0)
            string[] cardPaths = {
                "Cards/Red/Red_7",
                "Cards/Blue/Blue_Draw_2",    // Queen of Spades (+2 Blue)
                "Cards/Green/Green_9",
                "Cards/Yellow/Yellow_0",     // 10 of Diamonds
                "Cards/Red/Red_5"
            };

            for (int i = 0; i < cardPaths.Length; i++)
            {
                GameObject cardObj = new GameObject($"IntroCard_{i}");
                cardObj.transform.SetParent(cardContainer.transform, false);

                RectTransform cardRect = cardObj.AddComponent<RectTransform>();
                cardRect.anchoredPosition = Vector2.zero;
                cardRect.sizeDelta = new Vector2(160, 230);
                cardRect.localScale = Vector3.one;

                Image cardImg = cardObj.AddComponent<Image>();

                // Load actual card sprite from Resources
                string cardPath = cardPaths[i];
                Sprite cardSprite = Resources.Load<Sprite>(cardPath);

                if (cardSprite != null)
                {
                    cardImg.sprite = cardSprite;
                }
                else
                {
                    // Fallback: try loading as Texture2D and create sprite
                    Texture2D cardTex = Resources.Load<Texture2D>(cardPath);
                    if (cardTex != null)
                    {
                        cardImg.sprite = Sprite.Create(cardTex, new Rect(0, 0, cardTex.width, cardTex.height), new Vector2(0.5f, 0.5f));
                    }
                    else
                    {
                        // Last resort: use placeholder
                        Debug.LogWarning($"Could not load card: {cardPath}");
                        // Extract color from path for fallback
                        string colorName = cardPath.Contains("Red") ? "Red" :
                                           cardPath.Contains("Blue") ? "Blue" :
                                           cardPath.Contains("Green") ? "Green" : "Yellow";
                        cardImg.color = GetColorForCardColor(colorName);
                    }
                }

                // Add shadow
                Shadow shadow = cardObj.AddComponent<Shadow>();
                shadow.effectColor = new Color(0, 0, 0, 0.5f);
                shadow.effectDistance = new Vector2(4, -4);

                introCards.Add(cardRect);
            }
        }

        private Color GetColorForCardColor(string colorName)
        {
            return colorName switch
            {
                "Red" => new Color(0.9f, 0.2f, 0.2f),
                "Blue" => new Color(0.2f, 0.4f, 0.9f),
                "Green" => new Color(0.2f, 0.7f, 0.3f),
                "Yellow" => new Color(0.95f, 0.8f, 0.2f),
                _ => Color.white
            };
        }

        private void CreateHiddenLogo(Transform parent)
        {
            GameObject logoContainer = new GameObject("LogoContainer");
            logoContainer.transform.SetParent(parent, false);

            RectTransform logoRect = logoContainer.AddComponent<RectTransform>();
            logoRect.anchorMin = new Vector2(0.5f, 0.5f);
            logoRect.anchorMax = new Vector2(0.5f, 0.5f);
            logoRect.anchoredPosition = Vector2.zero;
            logoRect.sizeDelta = new Vector2(600, 250);

            CanvasGroup logoCg = logoContainer.AddComponent<CanvasGroup>();
            logoCg.alpha = 0;

            // Modern glow behind logo (cyan/magenta blend)
            GameObject glowObj = new GameObject("LogoGlow");
            glowObj.transform.SetParent(logoContainer.transform, false);

            RectTransform glowRect = glowObj.AddComponent<RectTransform>();
            glowRect.anchorMin = new Vector2(0.5f, 0.5f);
            glowRect.anchorMax = new Vector2(0.5f, 0.5f);
            glowRect.anchoredPosition = Vector2.zero;
            glowRect.sizeDelta = new Vector2(550, 320);

            Image glowImg = glowObj.AddComponent<Image>();
            Texture2D glowTex = CreateGlowTexture(128);
            glowImg.sprite = Sprite.Create(glowTex, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));
            glowImg.color = new Color(AccentCyan.r, AccentCyan.g, AccentCyan.b, 0.35f);
            glowImg.raycastTarget = false;

            // Secondary glow (magenta tint)
            GameObject glow2Obj = new GameObject("LogoGlow2");
            glow2Obj.transform.SetParent(logoContainer.transform, false);

            RectTransform glow2Rect = glow2Obj.AddComponent<RectTransform>();
            glow2Rect.anchorMin = new Vector2(0.5f, 0.5f);
            glow2Rect.anchorMax = new Vector2(0.5f, 0.5f);
            glow2Rect.anchoredPosition = new Vector2(30, -20);
            glow2Rect.sizeDelta = new Vector2(400, 250);

            Image glow2Img = glow2Obj.AddComponent<Image>();
            glow2Img.sprite = Sprite.Create(glowTex, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));
            glow2Img.color = new Color(AccentMagenta.r, AccentMagenta.g, AccentMagenta.b, 0.25f);
            glow2Img.raycastTarget = false;

            // Main title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(logoContainer.transform, false);

            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = new Vector2(0, 20);
            titleRect.sizeDelta = new Vector2(500, 150);

            TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "LEKHA";
            titleTmp.fontSize = 140;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.fontStyle = FontStyles.Bold;

            // Modern gradient (cyan to white)
            titleTmp.enableVertexGradient = true;
            titleTmp.colorGradient = new VertexGradient(
                TextWhite,
                TextWhite,
                AccentCyan,
                AccentCyan
            );

            // Modern decorative line (gradient)
            GameObject lineObj = new GameObject("DecoLine");
            lineObj.transform.SetParent(logoContainer.transform, false);

            RectTransform lineRect = lineObj.AddComponent<RectTransform>();
            lineRect.anchorMin = new Vector2(0.5f, 0.5f);
            lineRect.anchorMax = new Vector2(0.5f, 0.5f);
            lineRect.anchoredPosition = new Vector2(0, -55);
            lineRect.sizeDelta = new Vector2(300, 3);

            Image lineImg = lineObj.AddComponent<Image>();
            // Create horizontal gradient for line
            Texture2D lineTex = new Texture2D(128, 1, TextureFormat.RGBA32, false);
            for (int x = 0; x < 128; x++)
            {
                float t = (float)x / 127f;
                float alpha = 1f - Mathf.Abs(t - 0.5f) * 2f;
                alpha = Mathf.Pow(alpha, 0.7f);
                Color c = Color.Lerp(AccentCyan, AccentMagenta, t);
                c.a = alpha;
                lineTex.SetPixel(x, 0, c);
            }
            lineTex.Apply();
            lineImg.sprite = Sprite.Create(lineTex, new Rect(0, 0, 128, 1), new Vector2(0.5f, 0.5f));
            lineImg.raycastTarget = false;

            // Subtitle
            GameObject subObj = new GameObject("Subtitle");
            subObj.transform.SetParent(logoContainer.transform, false);

            RectTransform subRect = subObj.AddComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0.5f, 0.5f);
            subRect.anchorMax = new Vector2(0.5f, 0.5f);
            subRect.anchoredPosition = new Vector2(0, -90);
            subRect.sizeDelta = new Vector2(400, 40);

            TextMeshProUGUI subTmp = subObj.AddComponent<TextMeshProUGUI>();
            subTmp.text = "Classic Card Game";
            subTmp.fontSize = 26;
            subTmp.alignment = TextAlignmentOptions.Center;
            subTmp.color = TextMuted;
            subTmp.fontStyle = FontStyles.Normal;
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
                    float alpha = Mathf.Pow(Mathf.Max(0, 1 - dist), 2);
                    tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
            }

            tex.Apply();
            return tex;
        }

        private IEnumerator PlayCardRevealIntro()
        {
            yield return new WaitForSeconds(0.2f);

            // Hide all cards initially
            foreach (var card in introCards)
                card.localScale = Vector3.zero;

            // ── Phase 1: Cards cascade in from top, one by one ──
            int count = introCards.Count;
            float[] fanX = { -240f, -120f, 0f, 120f, 240f };
            float[] fanAngle = { -15f, -7f, 0f, 7f, 15f };

            for (int i = 0; i < count; i++)
            {
                StartCoroutine(AnimateCardCascadeIn(introCards[i], fanX[i], fanAngle[i], i * 0.12f));
            }

            yield return new WaitForSeconds(0.12f * count + 0.6f);

            // ── Phase 2: Hold the fan for a beat ──
            yield return new WaitForSeconds(0.8f);

            // ── Phase 3: Cards smoothly slide off screen ──
            for (int i = 0; i < count; i++)
            {
                float dir = (i < count / 2) ? -1f : (i > count / 2) ? 1f : 0f;
                float flyX = dir * 1400f;
                float flyY = (i == count / 2) ? 800f : -200f + i * 100f;
                StartCoroutine(AnimateCardSlideOff(introCards[i], flyX, flyY, i * 0.06f));
            }

            yield return new WaitForSeconds(0.4f);

            // ── Reveal logo ──
            Transform logoContainer = splashPanel.transform.Find("LogoContainer");
            if (logoContainer != null)
            {
                CanvasGroup logoCg = logoContainer.GetComponent<CanvasGroup>();
                RectTransform logoRect = logoContainer.GetComponent<RectTransform>();
                logoRect.localScale = Vector3.one * 0.85f;

                float duration = 0.7f;
                float elapsed = 0;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / duration;
                    float easeT = 1 - Mathf.Pow(1 - t, 3);
                    logoCg.alpha = easeT;
                    logoRect.localScale = Vector3.Lerp(Vector3.one * 0.85f, Vector3.one, easeT);
                    yield return null;
                }
            }

            yield return new WaitForSeconds(1.2f);

            // ── Transition to menu ──
            splashComplete = true;
            float fadeDuration = 0.5f;
            float fadeElapsed = 0;
            while (fadeElapsed < fadeDuration)
            {
                fadeElapsed += Time.deltaTime;
                splashCanvasGroup.alpha = 1 - (fadeElapsed / fadeDuration);
                yield return null;
            }

            splashPanel.SetActive(false);
            menuPanel.SetActive(true);
            StartCoroutine(AnimateMenuEntrance());
            CheckPlayerNameAfterSplash();
        }

        /// <summary>
        /// Card cascades in from above, lands at its fan position with a smooth spring feel
        /// </summary>
        private IEnumerator AnimateCardCascadeIn(RectTransform card, float targetX, float targetAngle, float delay)
        {
            yield return new WaitForSeconds(delay);

            float startY = 700f;
            float duration = 0.55f;
            float elapsed = 0;

            card.anchoredPosition = new Vector2(targetX * 0.3f, startY);
            card.localRotation = Quaternion.Euler(0, 0, Random.Range(-30f, 30f));
            card.localScale = Vector3.one * 0.8f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Spring-out ease
                float easeT = 1 - Mathf.Pow(1 - t, 3);
                // Slight overshoot bounce
                float bounce = 1f + Mathf.Sin(t * Mathf.PI) * 0.08f;

                card.anchoredPosition = new Vector2(
                    Mathf.Lerp(targetX * 0.3f, targetX, easeT),
                    Mathf.Lerp(startY, 0f, easeT));
                card.localRotation = Quaternion.Euler(0, 0, Mathf.LerpAngle(card.localEulerAngles.z > 180 ? card.localEulerAngles.z - 360 : card.localEulerAngles.z, targetAngle, easeT));
                card.localScale = Vector3.one * Mathf.Lerp(0.8f, 1f, easeT) * bounce;
                yield return null;
            }

            card.anchoredPosition = new Vector2(targetX, 0);
            card.localRotation = Quaternion.Euler(0, 0, targetAngle);
            card.localScale = Vector3.one;
        }

        /// <summary>
        /// Card smoothly slides off screen with gentle rotation and fade
        /// </summary>
        private IEnumerator AnimateCardSlideOff(RectTransform card, float targetX, float targetY, float delay)
        {
            yield return new WaitForSeconds(delay);

            Vector2 startPos = card.anchoredPosition;
            float startAngle = card.localEulerAngles.z;
            if (startAngle > 180f) startAngle -= 360f;
            float duration = 0.5f;
            float elapsed = 0;

            Image cardImg = card.GetComponent<Image>();

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float easeT = t * t * (3f - 2f * t); // Smoothstep

                card.anchoredPosition = Vector2.Lerp(startPos, new Vector2(targetX, targetY), easeT);
                card.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(startAngle, startAngle + 25f * Mathf.Sign(targetX), easeT));

                if (cardImg != null)
                    cardImg.color = new Color(1, 1, 1, 1f - easeT);

                yield return null;
            }

            card.gameObject.SetActive(false);
        }

        private void CreateMainMenuPanel(Transform parent)
        {
            menuPanel = new GameObject("MenuPanel");
            menuPanel.transform.SetParent(parent, false);

            RectTransform panelRect = menuPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;

            // Title section
            CreateMenuTitle(menuPanel.transform);

            // Profile button in top-right corner
            CreateProfileButton(menuPanel.transform);

            // Luxurious buttons - adjusted positions to make room for profile and online play
            buttonTransforms = new RectTransform[4];
            buttonTransforms[0] = CreateLuxuryButton(menuPanel.transform, "PLAY", new Vector2(0, 50), OnPlayButtonClicked, true);
            buttonTransforms[1] = CreateLuxuryButton(menuPanel.transform, "PLAY ONLINE", new Vector2(0, -40), OnPlayOnlineClicked, false);
            buttonTransforms[2] = CreateLuxuryButton(menuPanel.transform, "SETTINGS", new Vector2(0, -120), OnSettingsButtonClicked, false);
            buttonTransforms[3] = CreateLuxuryButton(menuPanel.transform, "HOW TO PLAY", new Vector2(0, -200), OnHowToPlayClicked, false);

            // Footer
            CreateFooter(menuPanel.transform);
        }

        private void CreateProfileButton(Transform parent)
        {
            // Profile button container - top right corner with modern glass style
            GameObject profileContainer = new GameObject("ProfileButton");
            profileContainer.transform.SetParent(parent, false);

            RectTransform containerRect = profileContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(1, 1);
            containerRect.anchorMax = new Vector2(1, 1);
            containerRect.pivot = new Vector2(1, 1);
            containerRect.anchoredPosition = new Vector2(-30, -30);
            containerRect.sizeDelta = new Vector2(180, 56);

            // Glass background
            Image bgImg = profileContainer.AddComponent<Image>();
            Texture2D glassTex = CreateGlassButtonTexture(100, 56);
            bgImg.sprite = Sprite.Create(glassTex, new Rect(0, 0, 100, 56), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(20, 20, 20, 20));
            bgImg.type = Image.Type.Sliced;
            bgImg.color = Color.white;

            Button btn = profileContainer.AddComponent<Button>();
            btn.targetGraphic = bgImg;

            ColorBlock colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.1f, 1.1f, 1.15f, 1f);
            colors.pressedColor = new Color(0.9f, 0.9f, 0.95f, 1f);
            btn.colors = colors;

            btn.onClick.AddListener(() => {
                SoundManager.Instance?.PlayButtonClick();
                OnProfileClicked();
            });

            // Avatar circle with glow ring
            float avatarSize = 40f;
            GameObject avatarObj = new GameObject("Avatar");
            avatarObj.transform.SetParent(profileContainer.transform, false);

            RectTransform avatarRect = avatarObj.AddComponent<RectTransform>();
            avatarRect.anchorMin = new Vector2(0, 0.5f);
            avatarRect.anchorMax = new Vector2(0, 0.5f);
            avatarRect.pivot = new Vector2(0, 0.5f);
            avatarRect.anchoredPosition = new Vector2(10, 0);
            avatarRect.sizeDelta = new Vector2(avatarSize, avatarSize);

            // Avatar ring with gradient
            Image ringImg = avatarObj.AddComponent<Image>();
            ringImg.sprite = CreateCircleSprite(64);
            ringImg.color = AccentCyan;

            // Avatar background
            GameObject avatarBgObj = new GameObject("AvatarBg");
            avatarBgObj.transform.SetParent(avatarObj.transform, false);

            RectTransform avatarBgRect = avatarBgObj.AddComponent<RectTransform>();
            avatarBgRect.anchorMin = new Vector2(0.5f, 0.5f);
            avatarBgRect.anchorMax = new Vector2(0.5f, 0.5f);
            avatarBgRect.sizeDelta = new Vector2(avatarSize - 4, avatarSize - 4);

            profileAvatarBg = avatarBgObj.AddComponent<Image>();
            profileAvatarBg.sprite = CreateCircleSprite(64);
            profileAvatarBg.color = new Color(0.25f, 0.45f, 0.75f, 1f);

            // Avatar initial
            GameObject initialObj = new GameObject("Initial");
            initialObj.transform.SetParent(avatarBgObj.transform, false);

            RectTransform initRect = initialObj.AddComponent<RectTransform>();
            initRect.anchorMin = Vector2.zero;
            initRect.anchorMax = Vector2.one;
            initRect.sizeDelta = Vector2.zero;

            profileAvatarInitial = initialObj.AddComponent<TextMeshProUGUI>();
            profileAvatarInitial.text = "P";
            profileAvatarInitial.fontSize = 20;
            profileAvatarInitial.fontStyle = FontStyles.Bold;
            profileAvatarInitial.color = TextWhite;
            profileAvatarInitial.alignment = TextAlignmentOptions.Center;

            // Profile name
            GameObject nameObj = new GameObject("ProfileName");
            nameObj.transform.SetParent(profileContainer.transform, false);

            RectTransform nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0.5f);
            nameRect.anchorMax = new Vector2(1, 0.5f);
            nameRect.pivot = new Vector2(0, 0.5f);
            nameRect.anchoredPosition = new Vector2(56, 0);
            nameRect.sizeDelta = new Vector2(-66, 40);

            profileNameText = nameObj.AddComponent<TextMeshProUGUI>();
            profileNameText.text = "PLAYER";
            profileNameText.fontSize = 16;
            profileNameText.fontStyle = FontStyles.Bold;
            profileNameText.color = TextWhite;
            profileNameText.alignment = TextAlignmentOptions.Left;
            profileNameText.textWrappingMode = TextWrappingModes.NoWrap;
            profileNameText.overflowMode = TextOverflowModes.Truncate;

            // Update with current profile data
            UpdateProfileButtonDisplay();
        }

        private Texture2D CreateGlassButtonTexture(int width, int height)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            int radius = 16;

            Color fillColor = new Color(0.12f, 0.15f, 0.22f, 0.85f);
            Color borderColor = new Color(1f, 1f, 1f, 0.2f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dist = DistanceToRoundedRectFloat(x, y, width, height, radius);

                    if (dist <= 0)
                    {
                        float gradT = (float)y / height;
                        Color col = Color.Lerp(fillColor * 0.9f, fillColor * 1.05f, gradT);

                        // Border highlight
                        if (dist > -2f)
                        {
                            float borderT = (dist + 2f) / 2f;
                            col = Color.Lerp(col, borderColor, borderT * 0.5f);
                        }

                        tex.SetPixel(x, y, col);
                    }
                    else if (dist < 1.5f)
                    {
                        float alpha = 1f - dist / 1.5f;
                        Color c = borderColor;
                        c.a *= alpha;
                        tex.SetPixel(x, y, c);
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            tex.Apply();
            return tex;
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

        private void UpdateProfileButtonDisplay()
        {
            var profile = PlayerProfileManager.Instance?.CurrentProfile;
            if (profile == null)
                return;

            // Update name
            if (profileNameText != null)
            {
                profileNameText.text = (profile.DisplayName ?? "PLAYER").ToUpper();
            }

            // Update avatar
            Sprite avatarSprite = profile.GetAvatarSprite();
            if (avatarSprite != null && profileAvatarBg != null)
            {
                profileAvatarBg.sprite = avatarSprite;
                profileAvatarBg.color = Color.white;
                if (profileAvatarInitial != null)
                    profileAvatarInitial.gameObject.SetActive(false);
            }
            else if (profileAvatarBg != null)
            {
                profileAvatarBg.sprite = CreateCircleSprite(64);
                profileAvatarBg.color = GetColorFromName(profile.DisplayName);
                if (profileAvatarInitial != null)
                {
                    profileAvatarInitial.gameObject.SetActive(true);
                    profileAvatarInitial.text = profile.Initial;
                }
            }
        }

        private Color GetColorFromName(string name)
        {
            int hash = string.IsNullOrEmpty(name) ? 0 : name.GetHashCode();
            float hue = Mathf.Abs(hash % 360) / 360f;
            return Color.HSVToRGB(hue, 0.5f, 0.7f);
        }

        private void OnProfileClicked()
        {
            // Show profile setup screen
            if (ProfileSetupScreen.Instance == null)
            {
                GameObject profileScreenObj = new GameObject("ProfileSetupScreen");
                profileScreenObj.transform.SetParent(transform);
                ProfileSetupScreen screen = profileScreenObj.AddComponent<ProfileSetupScreen>();
                screen.OnProfileSaved = () => {
                    UpdateProfileButtonDisplay();
                };
            }

            ProfileSetupScreen.Instance?.Show(false);
        }

        private void CreateMenuTitle(Transform parent)
        {
            GameObject titleContainer = new GameObject("TitleContainer");
            titleContainer.transform.SetParent(parent, false);

            titleTransform = titleContainer.AddComponent<RectTransform>();
            titleTransform.anchorMin = new Vector2(0.5f, 0.5f);
            titleTransform.anchorMax = new Vector2(0.5f, 0.5f);
            titleTransform.anchoredPosition = new Vector2(0, 260);
            titleTransform.sizeDelta = new Vector2(600, 180);

            // Modern glow (cyan)
            GameObject glowObj = new GameObject("TitleGlow");
            glowObj.transform.SetParent(titleContainer.transform, false);

            RectTransform glowRect = glowObj.AddComponent<RectTransform>();
            glowRect.anchorMin = new Vector2(0.5f, 0.5f);
            glowRect.anchorMax = new Vector2(0.5f, 0.5f);
            glowRect.anchoredPosition = Vector2.zero;
            glowRect.sizeDelta = new Vector2(500, 220);

            Image glowImg = glowObj.AddComponent<Image>();
            Texture2D glowTex = CreateGlowTexture(128);
            glowImg.sprite = Sprite.Create(glowTex, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));
            glowImg.color = new Color(AccentCyan.r, AccentCyan.g, AccentCyan.b, 0.20f);
            glowImg.raycastTarget = false;

            // Secondary glow (magenta, offset)
            GameObject glow2Obj = new GameObject("TitleGlow2");
            glow2Obj.transform.SetParent(titleContainer.transform, false);

            RectTransform glow2Rect = glow2Obj.AddComponent<RectTransform>();
            glow2Rect.anchorMin = new Vector2(0.5f, 0.5f);
            glow2Rect.anchorMax = new Vector2(0.5f, 0.5f);
            glow2Rect.anchoredPosition = new Vector2(40, -10);
            glow2Rect.sizeDelta = new Vector2(350, 180);

            Image glow2Img = glow2Obj.AddComponent<Image>();
            glow2Img.sprite = Sprite.Create(glowTex, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));
            glow2Img.color = new Color(AccentMagenta.r, AccentMagenta.g, AccentMagenta.b, 0.15f);
            glow2Img.raycastTarget = false;

            // Main title
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(titleContainer.transform, false);

            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = new Vector2(0, 15);
            titleRect.sizeDelta = new Vector2(500, 120);

            TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "LEKHA";
            titleTmp.fontSize = 110;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.fontStyle = FontStyles.Bold;

            // Modern gradient (white to cyan)
            titleTmp.enableVertexGradient = true;
            titleTmp.colorGradient = new VertexGradient(
                TextWhite,
                TextWhite,
                AccentCyan,
                AccentCyan
            );

            // Modern gradient line
            GameObject lineObj = new GameObject("DecoLine");
            lineObj.transform.SetParent(titleContainer.transform, false);

            RectTransform lineRect = lineObj.AddComponent<RectTransform>();
            lineRect.anchorMin = new Vector2(0.5f, 0.5f);
            lineRect.anchorMax = new Vector2(0.5f, 0.5f);
            lineRect.anchoredPosition = new Vector2(0, -45);
            lineRect.sizeDelta = new Vector2(250, 3);

            Image lineImg = lineObj.AddComponent<Image>();
            // Create horizontal gradient for line
            Texture2D lineTex = new Texture2D(128, 1, TextureFormat.RGBA32, false);
            for (int x = 0; x < 128; x++)
            {
                float t = (float)x / 127f;
                float alpha = 1f - Mathf.Abs(t - 0.5f) * 2f;
                alpha = Mathf.Pow(alpha, 0.7f);
                Color c = Color.Lerp(AccentCyan, AccentMagenta, t);
                c.a = alpha;
                lineTex.SetPixel(x, 0, c);
            }
            lineTex.Apply();
            lineImg.sprite = Sprite.Create(lineTex, new Rect(0, 0, 128, 1), new Vector2(0.5f, 0.5f));
            lineImg.raycastTarget = false;

            // Subtitle
            GameObject subObj = new GameObject("Subtitle");
            subObj.transform.SetParent(titleContainer.transform, false);

            RectTransform subRect = subObj.AddComponent<RectTransform>();
            subRect.anchorMin = new Vector2(0.5f, 0.5f);
            subRect.anchorMax = new Vector2(0.5f, 0.5f);
            subRect.anchoredPosition = new Vector2(0, -75);
            subRect.sizeDelta = new Vector2(400, 35);

            TextMeshProUGUI subTmp = subObj.AddComponent<TextMeshProUGUI>();
            subTmp.text = "Classic Card Game";
            subTmp.fontSize = 22;
            subTmp.alignment = TextAlignmentOptions.Center;
            subTmp.color = TextMuted;
            subTmp.fontStyle = FontStyles.Normal;
        }

        private RectTransform CreateLuxuryButton(Transform parent, string text, Vector2 position, UnityEngine.Events.UnityAction onClick, bool isPrimary)
        {
            GameObject btnObj = new GameObject($"Button_{text}");
            btnObj.transform.SetParent(parent, false);

            RectTransform rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = isPrimary ? new Vector2(300, 65) : new Vector2(260, 52);

            Image img = btnObj.AddComponent<Image>();

            // Create modern glassmorphism button background
            Texture2D btnTex = CreateModernButtonTexture(128, 48, isPrimary);
            img.sprite = Sprite.Create(btnTex, new Rect(0, 0, 128, 48), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(20, 20, 20, 20));
            img.type = Image.Type.Sliced;
            img.color = Color.white;

            Button btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;

            ColorBlock colors = btn.colors;
            if (isPrimary)
            {
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1.1f, 1.1f, 1.15f, 1f);
                colors.pressedColor = new Color(0.9f, 0.9f, 0.95f, 1f);
                colors.selectedColor = Color.white;
            }
            else
            {
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1.15f, 1.15f, 1.2f, 1f);
                colors.pressedColor = new Color(0.85f, 0.85f, 0.9f, 1f);
                colors.selectedColor = Color.white;
            }
            btn.colors = colors;

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
            tmp.fontSize = isPrimary ? 26 : 20;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = isPrimary ? TextWhite : TextMuted;
            tmp.fontStyle = FontStyles.Bold;

            return rect;
        }

        private Texture2D CreateButtonTexture(int width, int height, bool filled)
        {
            return CreateModernButtonTexture(width, height, filled);
        }

        private Texture2D CreateModernButtonTexture(int width, int height, bool isPrimary)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            int radius = 16;

            // Colors for primary vs secondary
            Color fillStart, fillEnd, borderColor, glowColor;
            if (isPrimary)
            {
                // Vibrant gradient for primary button
                fillStart = new Color(0.25f, 0.45f, 0.85f, 1f);  // Blue
                fillEnd = new Color(0.35f, 0.55f, 0.95f, 1f);    // Lighter blue
                borderColor = new Color(0.5f, 0.75f, 1f, 0.6f);  // Cyan border
                glowColor = new Color(0.4f, 0.7f, 1f, 0.4f);     // Cyan glow
            }
            else
            {
                // Glass style for secondary button
                fillStart = new Color(0.15f, 0.18f, 0.25f, 0.8f);
                fillEnd = new Color(0.20f, 0.23f, 0.32f, 0.8f);
                borderColor = new Color(1f, 1f, 1f, 0.2f);
                glowColor = new Color(1f, 1f, 1f, 0.1f);
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dist = DistanceToRoundedRectFloat(x, y, width, height, radius);

                    if (dist <= 0)
                    {
                        // Inside button
                        float gradT = (float)y / height;
                        Color baseColor = Color.Lerp(fillStart, fillEnd, gradT);

                        // Gloss highlight at top
                        if (gradT > 0.7f)
                        {
                            float glossT = (gradT - 0.7f) / 0.3f;
                            baseColor = Color.Lerp(baseColor, baseColor + new Color(0.1f, 0.1f, 0.15f, 0f), glossT * glossT);
                        }

                        // Subtle shadow at bottom
                        if (gradT < 0.15f)
                        {
                            float shadowT = 1f - gradT / 0.15f;
                            baseColor *= (1f - shadowT * 0.12f);
                        }

                        // Border glow
                        if (dist > -2.5f)
                        {
                            float borderT = (dist + 2.5f) / 2.5f;
                            baseColor = Color.Lerp(baseColor, borderColor, borderT * 0.5f);
                        }

                        tex.SetPixel(x, y, baseColor);
                    }
                    else if (dist < 3f)
                    {
                        // Outer glow
                        float glowT = 1f - dist / 3f;
                        Color c = glowColor;
                        c.a *= glowT * glowT;
                        tex.SetPixel(x, y, c);
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            tex.Apply();
            return tex;
        }

        private float DistanceToRoundedRectFloat(int x, int y, int width, int height, int radius)
        {
            float px = Mathf.Clamp(x, radius, width - radius - 1);
            float py = Mathf.Clamp(y, radius, height - radius - 1);

            bool inCorner = (x < radius || x >= width - radius) && (y < radius || y >= height - radius);

            if (inCorner)
            {
                float cx = x < radius ? radius : width - radius - 1;
                float cy = y < radius ? radius : height - radius - 1;
                return Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy)) - radius;
            }
            else
            {
                float dx = x < radius ? radius - x : (x >= width - radius ? x - (width - radius - 1) : 0);
                float dy = y < radius ? radius - y : (y >= height - radius ? y - (height - radius - 1) : 0);
                return Mathf.Max(dx, dy) - radius;
            }
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

        private void CreateFooter(Transform parent)
        {
            GameObject footerObj = new GameObject("Footer");
            footerObj.transform.SetParent(parent, false);

            RectTransform rect = footerObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0, 40);
            rect.sizeDelta = new Vector2(400, 30);

            TextMeshProUGUI tmp = footerObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "Version 1.0";
            tmp.fontSize = 16;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(CreamWhite.r, CreamWhite.g, CreamWhite.b, 0.4f);
        }

        private void CreateSettingsPanel(Transform parent)
        {
            settingsPanel = new GameObject("SettingsPanel");
            settingsPanel.transform.SetParent(parent, false);

            RectTransform panelRect = settingsPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;

            Image overlay = settingsPanel.AddComponent<Image>();
            overlay.color = new Color(0.02f, 0.03f, 0.06f, 0.90f);

            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(settingsPanel.transform, false);

            RectTransform contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(500, 420);

            // Modern glass panel background
            Image contentBg = contentObj.AddComponent<Image>();
            Texture2D glassTex = CreateSettingsPanelTexture(256, 256);
            contentBg.sprite = Sprite.Create(glassTex, new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(24, 24, 24, 24));
            contentBg.type = Image.Type.Sliced;
            contentBg.color = Color.white;

            CreateSettingsTitle(contentObj.transform);

            // Sound Settings Section
            CreateSettingsSection(contentObj.transform, "SOUND", new Vector2(0, 60));

            // Master Volume Slider
            volumeSlider = CreateSettingsSlider(contentObj.transform, "Master Volume", new Vector2(0, 10), 1f, (value) => {
                SoundManager.Instance?.SetMasterVolume(value);
                PlayerPrefs.SetFloat("MasterVolume", value);
            });

            // SFX Volume Slider
            Slider sfxSlider = CreateSettingsSlider(contentObj.transform, "SFX Volume", new Vector2(0, -55), 0.7f, (value) => {
                SoundManager.Instance?.SetSFXVolume(value);
                PlayerPrefs.SetFloat("SFXVolume", value);
            });

            // Sound Toggle
            soundToggle = CreateSettingsToggle(contentObj.transform, "Sound Enabled", new Vector2(0, -115), true, (isOn) => {
                SoundManager.Instance?.SetMasterVolume(isOn ? PlayerPrefs.GetFloat("MasterVolume", 1f) : 0f);
                PlayerPrefs.SetInt("SoundEnabled", isOn ? 1 : 0);
            });

            // Back button
            CreateLuxuryButton(contentObj.transform, "BACK", new Vector2(0, -170), OnBackFromSettings, false);

            // Load saved settings
            LoadSettings();
        }

        private Texture2D CreateSettingsPanelTexture(int width, int height)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            int radius = 20;

            Color fillColor = new Color(0.10f, 0.12f, 0.18f, 0.95f);
            Color borderColor = new Color(1f, 1f, 1f, 0.15f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dist = DistanceToRoundedRectFloat(x, y, width, height, radius);

                    if (dist <= 0)
                    {
                        float gradT = (float)y / height;
                        Color col = Color.Lerp(fillColor * 0.85f, fillColor * 1.1f, gradT);

                        // Border highlight
                        if (dist > -2f)
                        {
                            float borderT = (dist + 2f) / 2f;
                            col = Color.Lerp(col, borderColor, borderT * 0.6f);
                        }

                        tex.SetPixel(x, y, col);
                    }
                    else if (dist < 2f)
                    {
                        float alpha = 1f - dist / 2f;
                        Color c = borderColor;
                        c.a *= alpha;
                        tex.SetPixel(x, y, c);
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            tex.Apply();
            return tex;
        }

        private void CreateSettingsSection(Transform parent, string title, Vector2 position)
        {
            GameObject sectionObj = new GameObject($"Section_{title}");
            sectionObj.transform.SetParent(parent, false);

            RectTransform rect = sectionObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(400, 30);

            TextMeshProUGUI tmp = sectionObj.AddComponent<TextMeshProUGUI>();
            tmp.text = title;
            tmp.fontSize = 20;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.color = AccentCyan;
            tmp.fontStyle = FontStyles.Bold;
        }

        private Slider CreateSettingsSlider(Transform parent, string label, Vector2 position, float defaultValue, System.Action<float> onValueChanged)
        {
            // Container
            GameObject containerObj = new GameObject($"Slider_{label}");
            containerObj.transform.SetParent(parent, false);

            RectTransform containerRect = containerObj.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 0.5f);
            containerRect.anchorMax = new Vector2(0.5f, 0.5f);
            containerRect.anchoredPosition = position;
            containerRect.sizeDelta = new Vector2(400, 40);

            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(containerObj.transform, false);

            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0.5f);
            labelRect.anchorMax = new Vector2(0, 0.5f);
            labelRect.pivot = new Vector2(0, 0.5f);
            labelRect.anchoredPosition = new Vector2(0, 0);
            labelRect.sizeDelta = new Vector2(150, 30);

            TextMeshProUGUI labelTmp = labelObj.AddComponent<TextMeshProUGUI>();
            labelTmp.text = label;
            labelTmp.fontSize = 18;
            labelTmp.alignment = TextAlignmentOptions.Left;
            labelTmp.color = CreamWhite;

            // Slider background
            GameObject sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(containerObj.transform, false);

            RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0, 0.5f);
            sliderRect.anchorMax = new Vector2(0, 0.5f);
            sliderRect.pivot = new Vector2(0, 0.5f);
            sliderRect.anchoredPosition = new Vector2(160, 0);
            sliderRect.sizeDelta = new Vector2(200, 20);

            Slider slider = sliderObj.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = defaultValue;

            // Background track
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(sliderObj.transform, false);

            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            Image bgImg = bgObj.AddComponent<Image>();
            bgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

            // Fill area
            GameObject fillAreaObj = new GameObject("FillArea");
            fillAreaObj.transform.SetParent(sliderObj.transform, false);

            RectTransform fillAreaRect = fillAreaObj.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1, 0.75f);
            fillAreaRect.offsetMin = new Vector2(5, 0);
            fillAreaRect.offsetMax = new Vector2(-5, 0);

            // Fill
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(fillAreaObj.transform, false);

            RectTransform fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;

            Image fillImg = fillObj.AddComponent<Image>();
            fillImg.color = AccentCyan;

            slider.fillRect = fillRect;

            // Handle slide area
            GameObject handleAreaObj = new GameObject("HandleSlideArea");
            handleAreaObj.transform.SetParent(sliderObj.transform, false);

            RectTransform handleAreaRect = handleAreaObj.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(10, 0);
            handleAreaRect.offsetMax = new Vector2(-10, 0);

            // Handle
            GameObject handleObj = new GameObject("Handle");
            handleObj.transform.SetParent(handleAreaObj.transform, false);

            RectTransform handleRect = handleObj.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20, 20);

            Image handleImg = handleObj.AddComponent<Image>();
            handleImg.color = BrightGold;

            // Make handle circular
            Texture2D circleTex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            Vector2 center = new Vector2(16, 16);
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    circleTex.SetPixel(x, y, dist <= 14 ? Color.white : Color.clear);
                }
            }
            circleTex.Apply();
            handleImg.sprite = Sprite.Create(circleTex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));

            slider.handleRect = handleRect;
            slider.targetGraphic = handleImg;

            // Value text
            GameObject valueObj = new GameObject("Value");
            valueObj.transform.SetParent(containerObj.transform, false);

            RectTransform valueRect = valueObj.AddComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(1, 0.5f);
            valueRect.anchorMax = new Vector2(1, 0.5f);
            valueRect.pivot = new Vector2(1, 0.5f);
            valueRect.anchoredPosition = new Vector2(0, 0);
            valueRect.sizeDelta = new Vector2(40, 30);

            TextMeshProUGUI valueTmp = valueObj.AddComponent<TextMeshProUGUI>();
            valueTmp.text = Mathf.RoundToInt(defaultValue * 100) + "%";
            valueTmp.fontSize = 16;
            valueTmp.alignment = TextAlignmentOptions.Right;
            valueTmp.color = CreamWhite;

            // Update value text when slider changes
            slider.onValueChanged.AddListener((value) => {
                valueTmp.text = Mathf.RoundToInt(value * 100) + "%";
                onValueChanged?.Invoke(value);
            });

            return slider;
        }

        private Toggle CreateSettingsToggle(Transform parent, string label, Vector2 position, bool defaultValue, System.Action<bool> onValueChanged)
        {
            // Container
            GameObject containerObj = new GameObject($"Toggle_{label}");
            containerObj.transform.SetParent(parent, false);

            RectTransform containerRect = containerObj.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 0.5f);
            containerRect.anchorMax = new Vector2(0.5f, 0.5f);
            containerRect.anchoredPosition = position;
            containerRect.sizeDelta = new Vector2(400, 40);

            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(containerObj.transform, false);

            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0.5f);
            labelRect.anchorMax = new Vector2(0, 0.5f);
            labelRect.pivot = new Vector2(0, 0.5f);
            labelRect.anchoredPosition = new Vector2(0, 0);
            labelRect.sizeDelta = new Vector2(200, 30);

            TextMeshProUGUI labelTmp = labelObj.AddComponent<TextMeshProUGUI>();
            labelTmp.text = label;
            labelTmp.fontSize = 18;
            labelTmp.alignment = TextAlignmentOptions.Left;
            labelTmp.color = CreamWhite;

            // Toggle background
            GameObject toggleObj = new GameObject("Toggle");
            toggleObj.transform.SetParent(containerObj.transform, false);

            RectTransform toggleRect = toggleObj.AddComponent<RectTransform>();
            toggleRect.anchorMin = new Vector2(1, 0.5f);
            toggleRect.anchorMax = new Vector2(1, 0.5f);
            toggleRect.pivot = new Vector2(1, 0.5f);
            toggleRect.anchoredPosition = new Vector2(0, 0);
            toggleRect.sizeDelta = new Vector2(60, 30);

            Image toggleBg = toggleObj.AddComponent<Image>();
            toggleBg.color = defaultValue ? AccentCyan : new Color(0.2f, 0.22f, 0.28f, 0.8f);

            // Create rounded rect for toggle background
            Texture2D toggleTex = new Texture2D(60, 30, TextureFormat.RGBA32, false);
            for (int y = 0; y < 30; y++)
            {
                for (int x = 0; x < 60; x++)
                {
                    bool inside = IsInsideRoundedRect(x, y, 60, 30, 15);
                    toggleTex.SetPixel(x, y, inside ? Color.white : Color.clear);
                }
            }
            toggleTex.Apply();
            toggleBg.sprite = Sprite.Create(toggleTex, new Rect(0, 0, 60, 30), new Vector2(0.5f, 0.5f));

            Toggle toggle = toggleObj.AddComponent<Toggle>();
            toggle.targetGraphic = toggleBg;
            toggle.isOn = defaultValue;

            // Checkmark (the knob)
            GameObject knobObj = new GameObject("Knob");
            knobObj.transform.SetParent(toggleObj.transform, false);

            RectTransform knobRect = knobObj.AddComponent<RectTransform>();
            knobRect.anchorMin = new Vector2(0.5f, 0.5f);
            knobRect.anchorMax = new Vector2(0.5f, 0.5f);
            knobRect.anchoredPosition = defaultValue ? new Vector2(12, 0) : new Vector2(-12, 0);
            knobRect.sizeDelta = new Vector2(22, 22);

            Image knobImg = knobObj.AddComponent<Image>();
            knobImg.color = Color.white;

            // Make knob circular
            Texture2D knobTex = new Texture2D(22, 22, TextureFormat.RGBA32, false);
            Vector2 knobCenter = new Vector2(11, 11);
            for (int y = 0; y < 22; y++)
            {
                for (int x = 0; x < 22; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), knobCenter);
                    knobTex.SetPixel(x, y, dist <= 10 ? Color.white : Color.clear);
                }
            }
            knobTex.Apply();
            knobImg.sprite = Sprite.Create(knobTex, new Rect(0, 0, 22, 22), new Vector2(0.5f, 0.5f));

            toggle.graphic = knobImg;

            // Animate toggle on change
            toggle.onValueChanged.AddListener((isOn) => {
                toggleBg.color = isOn ? AccentCyan : new Color(0.2f, 0.22f, 0.28f, 0.8f);
                knobRect.anchoredPosition = isOn ? new Vector2(12, 0) : new Vector2(-12, 0);
                SoundManager.Instance?.PlayButtonClick();
                onValueChanged?.Invoke(isOn);
            });

            return toggle;
        }

        private void LoadSettings()
        {
            // Load saved settings from PlayerPrefs
            float masterVol = PlayerPrefs.GetFloat("MasterVolume", 1f);
            float sfxVol = PlayerPrefs.GetFloat("SFXVolume", 0.7f);
            bool soundEnabled = PlayerPrefs.GetInt("SoundEnabled", 1) == 1;

            if (volumeSlider != null)
                volumeSlider.value = masterVol;

            if (soundToggle != null)
                soundToggle.isOn = soundEnabled;

            // Apply to SoundManager
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.SetMasterVolume(soundEnabled ? masterVol : 0f);
                SoundManager.Instance.SetSFXVolume(sfxVol);
            }
        }

        private void CreateSettingsTitle(Transform parent)
        {
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(parent, false);

            RectTransform rect = titleObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0, -50);
            rect.sizeDelta = new Vector2(300, 50);

            TextMeshProUGUI tmp = titleObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "SETTINGS";
            tmp.fontSize = 36;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = TextWhite;
            tmp.fontStyle = FontStyles.Bold;
        }

        private IEnumerator AnimateMenuEntrance()
        {
            yield return new WaitForSeconds(0.1f);

            // Animate title
            if (titleTransform != null)
            {
                CanvasGroup titleCg = titleTransform.gameObject.AddComponent<CanvasGroup>();
                titleCg.alpha = 0;
                Vector2 targetPos = titleTransform.anchoredPosition;
                titleTransform.anchoredPosition = targetPos + new Vector2(0, 40);

                float duration = 0.5f;
                float elapsed = 0;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / duration;
                    float easeT = 1 - Mathf.Pow(1 - t, 3);

                    titleCg.alpha = easeT;
                    titleTransform.anchoredPosition = Vector2.Lerp(targetPos + new Vector2(0, 40), targetPos, easeT);
                    yield return null;
                }
            }

            // Animate buttons
            if (buttonTransforms != null)
            {
                for (int i = 0; i < buttonTransforms.Length; i++)
                {
                    if (buttonTransforms[i] != null)
                    {
                        StartCoroutine(AnimateButtonEntrance(buttonTransforms[i], i * 0.1f));
                    }
                }
            }
        }

        private IEnumerator AnimateButtonEntrance(RectTransform button, float delay)
        {
            yield return new WaitForSeconds(delay);

            CanvasGroup btnCg = button.gameObject.AddComponent<CanvasGroup>();
            btnCg.alpha = 0;

            Vector2 targetPos = button.anchoredPosition;
            button.anchoredPosition = targetPos + new Vector2(0, -25);

            float duration = 0.4f;
            float elapsed = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float easeT = 1 - Mathf.Pow(1 - t, 3);

                btnCg.alpha = easeT;
                button.anchoredPosition = Vector2.Lerp(targetPos + new Vector2(0, -25), targetPos, easeT);
                yield return null;
            }
        }

        private void OnPlayButtonClicked()
        {
            Hide();
            OnPlayClicked?.Invoke();
        }

        private void OnPlayOnlineClicked()
        {
            // Hide menu and show lobby
            Hide();
            ShowOnlineLobby();
        }

        private void ShowOnlineLobby()
        {
            // Create lobby UI if it doesn't exist
            LobbyUI existingLobby = FindFirstObjectByType<LobbyUI>();
            if (existingLobby == null)
            {
                GameObject lobbyObj = new GameObject("LobbyUI");
                lobbyObj.transform.SetParent(transform);
                LobbyUI lobby = lobbyObj.AddComponent<LobbyUI>();
                lobby.OnBackClicked += () => {
                    Show();
                };
                lobby.OnGameStarting += () => {
                    // Game is starting from online lobby
                    OnPlayClicked?.Invoke();
                };
            }
            else
            {
                existingLobby.Show();
            }
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

        private void ShowHowToPlay()
        {
            GameObject overlay = new GameObject("HowToPlayOverlay");
            overlay.transform.SetParent(menuCanvas.transform, false);

            RectTransform overlayRect = overlay.AddComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.sizeDelta = Vector2.zero;

            Image overlayBg = overlay.AddComponent<Image>();
            overlayBg.color = new Color(0.02f, 0.03f, 0.06f, 0.92f);

            GameObject contentContainer = new GameObject("ContentContainer");
            contentContainer.transform.SetParent(overlay.transform, false);

            RectTransform containerRect = contentContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 0.5f);
            containerRect.anchorMax = new Vector2(0.5f, 0.5f);
            containerRect.anchoredPosition = Vector2.zero;
            containerRect.sizeDelta = new Vector2(820, 650);

            // Modern glass panel background
            Image containerBg = contentContainer.AddComponent<Image>();
            Texture2D glassTex = CreateSettingsPanelTexture(256, 256);
            containerBg.sprite = Sprite.Create(glassTex, new Rect(0, 0, 256, 256), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(24, 24, 24, 24));
            containerBg.type = Image.Type.Sliced;
            containerBg.color = Color.white;

            // Title - anchored to top with proper offset
            GameObject titleObj = new GameObject("Title");
            titleObj.transform.SetParent(contentContainer.transform, false);

            RectTransform titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1f);
            titleRect.anchorMax = new Vector2(1, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0, -20);
            titleRect.sizeDelta = new Vector2(0, 60);

            TextMeshProUGUI titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "HOW TO PLAY";
            titleTmp.fontSize = 38;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.color = TextWhite;

            // Content - positioned below title with proper spacing
            GameObject content = new GameObject("Content");
            content.transform.SetParent(contentContainer.transform, false);

            RectTransform contentRect = content.AddComponent<RectTransform>();
            // Anchor to fill space between title and hint
            contentRect.anchorMin = new Vector2(0, 0);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.offsetMin = new Vector2(30, 60); // Left, bottom padding (space for hint)
            contentRect.offsetMax = new Vector2(-30, -90); // Right, top padding (space for title)

            TextMeshProUGUI contentTmp = content.AddComponent<TextMeshProUGUI>();
            contentTmp.text = @"<size=22><b>Lekha is a 4-player trick-taking card game played in teams.</b>

<color=#66BFFF><b>TEAMS</b></color>
You (South) and Partner (North) vs West and East

<color=#66BFFF><b>CARDS</b></color>
Uses Uno-styled cards mapped to traditional suits
<color=#FF6666>Red = Hearts</color>  |  <color=#FFCC44>Yellow = Diamonds</color>  |  <color=#6699FF>Blue = Spades</color>  |  <color=#66CC66>Green = Clubs</color>

<color=#66BFFF><b>GAMEPLAY</b></color>
1. Each player receives 13 cards
2. Pass 3 cards to the player on your right
3. Play tricks - must follow suit if possible
4. Highest card of the led suit wins the trick

<color=#66BFFF><b>SCORING</b></color>
Hearts: 1 point each  |  Queen of Spades (Blue +2): 13 points
First team to 101 points <color=#FF6666>LOSES!</color></size>";
            contentTmp.alignment = TextAlignmentOptions.TopLeft;
            contentTmp.color = TextMuted;

            // Close hint - anchored to bottom
            GameObject hintObj = new GameObject("Hint");
            hintObj.transform.SetParent(contentContainer.transform, false);

            RectTransform hintRect = hintObj.AddComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0, 0f);
            hintRect.anchorMax = new Vector2(1, 0f);
            hintRect.pivot = new Vector2(0.5f, 0f);
            hintRect.anchoredPosition = new Vector2(0, 20);
            hintRect.sizeDelta = new Vector2(0, 30);

            TextMeshProUGUI hintTmp = hintObj.AddComponent<TextMeshProUGUI>();
            hintTmp.text = "Tap anywhere to close";
            hintTmp.fontSize = 16;
            hintTmp.alignment = TextAlignmentOptions.Center;
            hintTmp.color = new Color(0.5f, 0.55f, 0.65f, 0.7f);
            hintTmp.fontStyle = FontStyles.Italic;

            Button closeBtn = overlay.AddComponent<Button>();
            closeBtn.onClick.AddListener(() => {
                SoundManager.Instance?.PlayButtonClick();
                Destroy(overlay);
            });
        }

        public void Show()
        {
            menuCanvas.gameObject.SetActive(true);

            if (splashComplete)
            {
                splashPanel.SetActive(false);
                menuPanel.SetActive(true);
                settingsPanel.SetActive(false);
                StartCoroutine(AnimateMenuEntrance());
            }
            else
            {
                splashPanel.SetActive(true);
                menuPanel.SetActive(false);
                settingsPanel.SetActive(false);
                StartCoroutine(PlayCardRevealIntro());
            }
        }

        public void Hide()
        {
            menuCanvas.gameObject.SetActive(false);
        }
    }
}
