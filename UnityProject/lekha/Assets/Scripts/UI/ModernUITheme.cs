using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Lekha.UI
{
    /// <summary>
    /// 2026 Modern UI Theme - Glassmorphism with vibrant gradients
    /// Frosted glass panels, neon accents, smooth gradients, modern aesthetics
    /// </summary>
    public class ModernUITheme : MonoBehaviour
    {
        public static ModernUITheme Instance { get; private set; }

        // === 2026 MODERN COLOR PALETTE ===

        // Primary Background Colors - Deep rich tones
        public static readonly Color BackgroundDark = new Color(0.06f, 0.08f, 0.14f, 1f);        // Deep navy
        public static readonly Color BackgroundMid = new Color(0.10f, 0.12f, 0.20f, 1f);         // Rich purple-navy
        public static readonly Color BackgroundLight = new Color(0.15f, 0.17f, 0.26f, 1f);       // Lighter purple-navy

        // Gradient Colors - Vibrant modern feel
        public static readonly Color GradientStart = new Color(0.18f, 0.12f, 0.35f, 1f);         // Deep purple
        public static readonly Color GradientMid = new Color(0.12f, 0.20f, 0.40f, 1f);           // Blue-purple
        public static readonly Color GradientEnd = new Color(0.08f, 0.25f, 0.35f, 1f);           // Teal-blue

        // Accent Colors - Vibrant neon-inspired
        public static readonly Color AccentPrimary = new Color(0.40f, 0.75f, 1f, 1f);            // Bright cyan
        public static readonly Color AccentSecondary = new Color(0.85f, 0.45f, 0.95f, 1f);       // Vibrant magenta
        public static readonly Color AccentTertiary = new Color(0.45f, 0.95f, 0.75f, 1f);        // Mint green
        public static readonly Color AccentGold = new Color(1f, 0.82f, 0.40f, 1f);               // Warm gold
        public static readonly Color AccentOrange = new Color(1f, 0.55f, 0.30f, 1f);             // Vibrant orange

        // Glass Panel Colors
        public static readonly Color GlassLight = new Color(1f, 1f, 1f, 0.08f);                  // Light frosted
        public static readonly Color GlassMedium = new Color(1f, 1f, 1f, 0.12f);                 // Medium frosted
        public static readonly Color GlassDark = new Color(0.1f, 0.1f, 0.15f, 0.75f);            // Dark glass
        public static readonly Color GlassBorder = new Color(1f, 1f, 1f, 0.18f);                 // Glass edge highlight
        public static readonly Color GlassGlow = new Color(0.5f, 0.7f, 1f, 0.15f);               // Subtle glow

        // Text Colors
        public static readonly Color TextPrimary = new Color(1f, 1f, 1f, 1f);                    // Pure white
        public static readonly Color TextSecondary = new Color(0.75f, 0.80f, 0.90f, 1f);         // Soft blue-white
        public static readonly Color TextMuted = new Color(0.55f, 0.60f, 0.70f, 1f);             // Muted gray-blue
        public static readonly Color TextAccent = AccentPrimary;                                  // Accent color for highlights

        // Button Colors
        public static readonly Color ButtonPrimary = new Color(0.35f, 0.55f, 0.95f, 1f);         // Bright blue
        public static readonly Color ButtonPrimaryHover = new Color(0.45f, 0.65f, 1f, 1f);       // Lighter blue
        public static readonly Color ButtonSecondary = new Color(0.25f, 0.28f, 0.38f, 0.9f);     // Muted gray
        public static readonly Color ButtonSuccess = new Color(0.25f, 0.85f, 0.55f, 1f);         // Green
        public static readonly Color ButtonDanger = new Color(0.95f, 0.35f, 0.40f, 1f);          // Red
        public static readonly Color ButtonWarning = new Color(1f, 0.70f, 0.25f, 1f);            // Orange

        // Team Colors - Modern vibrant
        public static readonly Color TeamNorthSouth = new Color(0.30f, 0.70f, 1f, 1f);           // Bright blue
        public static readonly Color TeamEastWest = new Color(1f, 0.50f, 0.35f, 1f);             // Coral orange

        // Status Colors
        public static readonly Color Success = new Color(0.25f, 0.90f, 0.55f, 1f);
        public static readonly Color Warning = new Color(1f, 0.75f, 0.25f, 1f);
        public static readonly Color Danger = new Color(0.95f, 0.35f, 0.40f, 1f);
        public static readonly Color Info = AccentPrimary;

        // Legacy compatibility (redirect to new colors)
        public static readonly Color FeltGreen = BackgroundDark;
        public static readonly Color FeltGreenLight = BackgroundMid;
        public static readonly Color FeltGreenDark = new Color(0.04f, 0.05f, 0.10f, 1f);
        public static readonly Color WoodDark = BackgroundDark;
        public static readonly Color WoodMid = BackgroundMid;
        public static readonly Color WoodLight = BackgroundLight;
        public static readonly Color GoldAccent = AccentGold;
        public static readonly Color GoldBright = new Color(1f, 0.88f, 0.50f, 1f);
        public static readonly Color GoldDark = new Color(0.85f, 0.65f, 0.25f, 1f);
        public static readonly Color PrimaryDark = BackgroundDark;
        public static readonly Color PrimaryMid = BackgroundMid;
        public static readonly Color AccentGoldDark = GoldDark;
        public static readonly Color AccentCyan = AccentPrimary;
        public static readonly Color AccentPurple = AccentSecondary;
        public static readonly Color GlassWhite = GlassLight;

        // Generated Sprites
        public Sprite TableSprite { get; private set; }
        public Sprite GlassPanelSprite { get; private set; }
        public Sprite GlassPanelDarkSprite { get; private set; }
        public Sprite GlassPanelLightSprite { get; private set; }
        public Sprite GlassPanelAccentSprite { get; private set; }
        public Sprite ButtonSprite { get; private set; }
        public Sprite ButtonHoverSprite { get; private set; }
        public Sprite ButtonPressSprite { get; private set; }
        public Sprite ButtonSecondarySprite { get; private set; }
        public Sprite ButtonGlassSprite { get; private set; }
        public Sprite CircleSprite { get; private set; }
        public Sprite CircleOutlineSprite { get; private set; }
        public Sprite CircleGlowSprite { get; private set; }
        public Sprite PillSprite { get; private set; }
        public Sprite SoftGlowSprite { get; private set; }
        public Sprite CardSlotSprite { get; private set; }
        public Sprite GradientHorizontalSprite { get; private set; }
        public Sprite GradientVerticalSprite { get; private set; }
        public Sprite GradientRadialSprite { get; private set; }
        public Sprite CornerPanelSprite { get; private set; }
        public Sprite WoodFrameSprite { get; private set; }
        public Sprite NeonBorderSprite { get; private set; }
        public Sprite ModernCardSprite { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            GenerateAllAssets();
            Debug.Log("[ModernUITheme] 2026 Modern UI assets generated");
        }

        private void GenerateAllAssets()
        {
            GenerateTableTexture();
            GenerateGlassPanels();
            GenerateModernButtons();
            GenerateShapes();
            GenerateGradients();
            GenerateCornerPanel();
            GenerateNeonBorder();
            GenerateModernCard();
        }

        private void GenerateTableTexture()
        {
            int size = 1024;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            float centerX = size / 2f;
            float centerY = size / 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Distance from center for radial gradient
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                    float maxDist = size * 0.8f;
                    float radialT = Mathf.Clamp01(dist / maxDist);

                    // Modern gradient background - deep to rich
                    Color baseColor;

                    // Multi-tone gradient for depth
                    if (radialT < 0.4f)
                    {
                        float t = radialT / 0.4f;
                        baseColor = Color.Lerp(GradientMid, GradientStart, t * t);
                    }
                    else if (radialT < 0.7f)
                    {
                        float t = (radialT - 0.4f) / 0.3f;
                        baseColor = Color.Lerp(GradientStart, BackgroundMid, t);
                    }
                    else
                    {
                        float t = (radialT - 0.7f) / 0.3f;
                        baseColor = Color.Lerp(BackgroundMid, BackgroundDark, t);
                    }

                    // Add subtle noise for texture
                    float noise = Mathf.PerlinNoise(x * 0.008f, y * 0.008f) * 0.03f - 0.015f;
                    baseColor.r = Mathf.Clamp01(baseColor.r + noise);
                    baseColor.g = Mathf.Clamp01(baseColor.g + noise);
                    baseColor.b = Mathf.Clamp01(baseColor.b + noise * 1.5f);

                    // Subtle center glow effect
                    if (radialT < 0.5f)
                    {
                        float glowT = 1f - (radialT / 0.5f);
                        Color glow = AccentPrimary * 0.08f;
                        baseColor = Color.Lerp(baseColor, baseColor + glow, glowT * glowT);
                    }

                    // Soft vignette at edges
                    if (radialT > 0.6f)
                    {
                        float vignette = (radialT - 0.6f) / 0.4f;
                        baseColor *= (1f - vignette * 0.4f);
                    }

                    tex.SetPixel(x, y, baseColor);
                }
            }

            tex.Apply();
            TableSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100);
        }

        private void GenerateGlassPanels()
        {
            // Main glass panel - frosted glass effect
            GlassPanelSprite = CreateGlassPanel(256, 256, 20,
                new Color(0.15f, 0.18f, 0.25f, 0.85f),
                GlassBorder,
                1.5f);

            // Dark glass panel - deeper tint
            GlassPanelDarkSprite = CreateGlassPanel(256, 256, 20,
                new Color(0.08f, 0.10f, 0.15f, 0.92f),
                new Color(1f, 1f, 1f, 0.12f),
                1f);

            // Light glass panel - subtle frosted
            GlassPanelLightSprite = CreateGlassPanel(256, 256, 20,
                new Color(1f, 1f, 1f, 0.10f),
                new Color(1f, 1f, 1f, 0.25f),
                2f);

            // Accent glass panel - with color tint
            GlassPanelAccentSprite = CreateGlassPanel(256, 256, 20,
                new Color(AccentPrimary.r * 0.2f, AccentPrimary.g * 0.2f, AccentPrimary.b * 0.3f, 0.85f),
                new Color(AccentPrimary.r, AccentPrimary.g, AccentPrimary.b, 0.4f),
                2f);

            // Card slot
            CardSlotSprite = CreateGlassPanel(128, 180, 12,
                new Color(0f, 0f, 0f, 0.35f),
                new Color(1f, 1f, 1f, 0.08f),
                1f);
        }

        private void GenerateModernButtons()
        {
            int width = 200;
            int height = 56;
            int radius = 16;

            // Primary button - vibrant gradient with glow
            ButtonSprite = CreateModernButtonSprite(width, height, radius,
                ButtonPrimary, new Color(0.25f, 0.45f, 0.85f, 1f),
                new Color(AccentPrimary.r, AccentPrimary.g, AccentPrimary.b, 0.4f));

            // Hover state - brighter with more glow
            ButtonHoverSprite = CreateModernButtonSprite(width, height, radius,
                ButtonPrimaryHover, ButtonPrimary,
                new Color(AccentPrimary.r, AccentPrimary.g, AccentPrimary.b, 0.6f));

            // Pressed state - deeper color
            ButtonPressSprite = CreateModernButtonSprite(width, height, radius,
                new Color(0.25f, 0.40f, 0.75f, 1f), new Color(0.20f, 0.35f, 0.65f, 1f),
                new Color(AccentPrimary.r, AccentPrimary.g, AccentPrimary.b, 0.3f));

            // Secondary button - glass style
            ButtonSecondarySprite = CreateModernButtonSprite(width, height, radius,
                new Color(0.25f, 0.28f, 0.35f, 0.85f), new Color(0.18f, 0.20f, 0.28f, 0.85f),
                GlassBorder);

            // Glass button - transparent
            ButtonGlassSprite = CreateGlassPanel(width, height, radius,
                new Color(1f, 1f, 1f, 0.08f),
                GlassBorder,
                1.5f);
        }

        private void GenerateShapes()
        {
            // Circle - smooth with subtle gradient
            CircleSprite = CreateCircle(128, Color.white, true);

            // Circle outline
            CircleOutlineSprite = CreateCircleOutline(128, 3, Color.white);

            // Circle with glow effect
            CircleGlowSprite = CreateCircleGlow(128, AccentPrimary);

            // Pill shape
            PillSprite = CreateGlassPanel(128, 48, 24, Color.white, Color.clear, 0);

            // Soft glow for effects
            SoftGlowSprite = CreateSoftGlow(256);
        }

        private void GenerateGradients()
        {
            // Horizontal gradient
            Texture2D hGrad = new Texture2D(256, 1, TextureFormat.RGBA32, false);
            for (int x = 0; x < 256; x++)
            {
                float t = x / 255f;
                hGrad.SetPixel(x, 0, new Color(1, 1, 1, t));
            }
            hGrad.Apply();
            GradientHorizontalSprite = Sprite.Create(hGrad, new Rect(0, 0, 256, 1), new Vector2(0.5f, 0.5f));

            // Vertical gradient
            Texture2D vGrad = new Texture2D(1, 256, TextureFormat.RGBA32, false);
            for (int y = 0; y < 256; y++)
            {
                float t = y / 255f;
                vGrad.SetPixel(0, y, new Color(1, 1, 1, t));
            }
            vGrad.Apply();
            GradientVerticalSprite = Sprite.Create(vGrad, new Rect(0, 0, 1, 256), new Vector2(0.5f, 0.5f));

            // Radial gradient for glow effects
            int radialSize = 256;
            Texture2D radGrad = new Texture2D(radialSize, radialSize, TextureFormat.RGBA32, false);
            float radCenter = radialSize / 2f;
            for (int y = 0; y < radialSize; y++)
            {
                for (int x = 0; x < radialSize; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(radCenter, radCenter));
                    float t = Mathf.Clamp01(1f - dist / radCenter);
                    radGrad.SetPixel(x, y, new Color(1, 1, 1, t * t));
                }
            }
            radGrad.Apply();
            GradientRadialSprite = Sprite.Create(radGrad, new Rect(0, 0, radialSize, radialSize), new Vector2(0.5f, 0.5f));
        }

        private void GenerateCornerPanel()
        {
            int width = 256;
            int height = 128;
            int radius = 20;

            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color panelColor = new Color(0.12f, 0.14f, 0.22f, 0.92f);
            Color borderColor = GlassBorder;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dist = DistanceToRoundedRect(x, y, width, height, radius);

                    if (dist <= 0)
                    {
                        float gradY = (float)y / height;
                        Color col = panelColor;

                        // Subtle vertical gradient for depth
                        col = Color.Lerp(col * 0.85f, col * 1.1f, gradY);

                        // Glass border effect
                        if (dist > -2.5f)
                        {
                            float borderT = (dist + 2.5f) / 2.5f;
                            col = Color.Lerp(col, borderColor, borderT * 0.5f);
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

            Vector4 border = new Vector4(radius + 4, radius + 4, radius + 4, radius + 4);
            CornerPanelSprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, border);
        }

        private void GenerateNeonBorder()
        {
            int size = 128;
            int borderWidth = 4;
            int radius = 16;

            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = DistanceToRoundedRect(x, y, size, size, radius);

                    // Only draw the border region with glow
                    if (dist >= -borderWidth * 2 && dist <= borderWidth * 2)
                    {
                        float glowT;
                        if (dist < 0)
                        {
                            // Inner glow
                            glowT = 1f - Mathf.Abs(dist) / (borderWidth * 2);
                        }
                        else
                        {
                            // Outer glow
                            glowT = 1f - dist / (borderWidth * 2);
                        }

                        glowT = Mathf.Pow(glowT, 1.5f);
                        tex.SetPixel(x, y, new Color(1, 1, 1, glowT));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            tex.Apply();
            Vector4 border = new Vector4(radius + borderWidth, radius + borderWidth, radius + borderWidth, radius + borderWidth);
            NeonBorderSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, border);
        }

        private void GenerateModernCard()
        {
            int width = 160;
            int height = 220;
            int radius = 12;

            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dist = DistanceToRoundedRect(x, y, width, height, radius);

                    if (dist <= 0)
                    {
                        float gradY = (float)y / height;
                        // Card gradient from bottom to top
                        Color baseColor = Color.Lerp(
                            new Color(0.95f, 0.95f, 0.97f, 1f),
                            new Color(1f, 1f, 1f, 1f),
                            gradY
                        );

                        // Subtle border darkening
                        if (dist > -2f)
                        {
                            float borderT = (dist + 2f) / 2f;
                            baseColor = Color.Lerp(baseColor, baseColor * 0.9f, borderT);
                        }

                        tex.SetPixel(x, y, baseColor);
                    }
                    else if (dist < 1.5f)
                    {
                        float alpha = 1f - dist / 1.5f;
                        tex.SetPixel(x, y, new Color(0.8f, 0.8f, 0.85f, alpha));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            tex.Apply();
            Vector4 border = new Vector4(radius + 2, radius + 2, radius + 2, radius + 2);
            ModernCardSprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, border);
        }

        // === SPRITE GENERATION HELPERS ===

        private Sprite CreateGlassPanel(int width, int height, int radius, Color fillColor, Color borderColor, float borderWidth)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dist = DistanceToRoundedRect(x, y, width, height, radius);

                    if (dist <= 0)
                    {
                        Color col = fillColor;

                        // Add subtle vertical gradient for glass depth
                        float gradY = (float)y / height;
                        col = Color.Lerp(col * 0.9f, col * 1.05f, gradY);

                        // Inner highlight at top edge
                        if (gradY > 0.85f && dist < -2f)
                        {
                            float highlightT = (gradY - 0.85f) / 0.15f;
                            col = Color.Lerp(col, col + new Color(1f, 1f, 1f, 0.05f), highlightT);
                        }

                        // Border glow effect
                        if (borderWidth > 0 && dist > -borderWidth)
                        {
                            float t = (dist + borderWidth) / borderWidth;
                            col = Color.Lerp(col, borderColor, t * 0.7f);
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

            Vector4 border = new Vector4(radius + 2, radius + 2, radius + 2, radius + 2);
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, border);
        }

        private Sprite CreateModernButtonSprite(int width, int height, int radius, Color topColor, Color bottomColor, Color glowColor)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dist = DistanceToRoundedRect(x, y, width, height, radius);

                    if (dist <= 0)
                    {
                        float gradT = (float)y / height;
                        Color baseColor = Color.Lerp(bottomColor, topColor, gradT);

                        // Add gloss highlight at top
                        if (gradT > 0.7f)
                        {
                            float glossT = (gradT - 0.7f) / 0.3f;
                            baseColor = Color.Lerp(baseColor, baseColor + new Color(0.15f, 0.15f, 0.2f, 0f), glossT * glossT);
                        }

                        // Subtle shadow at bottom
                        if (gradT < 0.15f)
                        {
                            float shadowT = 1f - gradT / 0.15f;
                            baseColor *= (1f - shadowT * 0.15f);
                        }

                        // Glow at edge
                        if (dist > -3f)
                        {
                            float edgeT = (dist + 3f) / 3f;
                            baseColor = Color.Lerp(baseColor, glowColor, edgeT * 0.3f);
                        }

                        tex.SetPixel(x, y, baseColor);
                    }
                    else if (dist < 3f)
                    {
                        // Outer glow
                        float glowT = 1f - dist / 3f;
                        Color c = glowColor;
                        c.a *= glowT * glowT * 0.5f;
                        tex.SetPixel(x, y, c);
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            tex.Apply();

            Vector4 border = new Vector4(radius + 2, radius + 2, radius + 2, radius + 2);
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, border);
        }

        private Sprite CreateCircle(int size, Color color, bool gradient)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            float center = size / 2f;
            float radius = center - 2;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));

                    if (dist <= radius)
                    {
                        Color c = color;
                        if (gradient)
                        {
                            float t = dist / radius;
                            c = Color.Lerp(color, color * 0.85f, t * 0.25f);
                        }

                        if (dist > radius - 1.5f)
                        {
                            c.a *= (radius - dist) / 1.5f;
                        }

                        tex.SetPixel(x, y, c);
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100);
        }

        private Sprite CreateCircleOutline(int size, int thickness, Color color)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            float center = size / 2f;
            float outerRadius = center - 2;
            float innerRadius = outerRadius - thickness;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));

                    if (dist <= outerRadius && dist >= innerRadius)
                    {
                        Color c = color;

                        if (dist > outerRadius - 1.5f)
                            c.a *= (outerRadius - dist) / 1.5f;
                        else if (dist < innerRadius + 1.5f)
                            c.a *= (dist - innerRadius) / 1.5f;

                        tex.SetPixel(x, y, c);
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100);
        }

        private Sprite CreateCircleGlow(int size, Color color)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            float center = size / 2f;
            float coreRadius = center * 0.4f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));

                    if (dist <= center)
                    {
                        float t = dist / center;

                        if (dist <= coreRadius)
                        {
                            // Bright core
                            tex.SetPixel(x, y, color);
                        }
                        else
                        {
                            // Fading glow
                            float glowT = (dist - coreRadius) / (center - coreRadius);
                            float alpha = Mathf.Pow(1f - glowT, 2f);
                            Color c = color;
                            c.a = alpha;
                            tex.SetPixel(x, y, c);
                        }
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100);
        }

        private Sprite CreateSoftGlow(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            float center = size / 2f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float t = dist / center;

                    if (t <= 1f)
                    {
                        float alpha = Mathf.Pow(1f - t, 2.5f);
                        tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100);
        }

        private float DistanceToRoundedRect(int x, int y, int width, int height, int radius)
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

        // === HELPER METHODS FOR UI CREATION ===

        public static Image CreateGlassPanel(Transform parent, string name, Vector2 size)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.sizeDelta = size;

            Image img = obj.AddComponent<Image>();
            if (Instance != null && Instance.GlassPanelSprite != null)
            {
                img.sprite = Instance.GlassPanelSprite;
                img.type = Image.Type.Sliced;
            }
            img.color = Color.white;

            return img;
        }

        public static Image CreateDarkGlassPanel(Transform parent, string name, Vector2 size)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);

            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.sizeDelta = size;

            Image img = obj.AddComponent<Image>();
            if (Instance != null && Instance.GlassPanelDarkSprite != null)
            {
                img.sprite = Instance.GlassPanelDarkSprite;
                img.type = Image.Type.Sliced;
            }
            img.color = Color.white;

            return img;
        }

        public static Button CreateModernButton(Transform parent, string text, Vector2 size)
        {
            GameObject obj = new GameObject("Button_" + text);
            obj.transform.SetParent(parent, false);

            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.sizeDelta = size;

            Image img = obj.AddComponent<Image>();
            if (Instance != null)
            {
                img.sprite = Instance.ButtonSprite;
                img.type = Image.Type.Sliced;
            }

            Button btn = obj.AddComponent<Button>();

            if (Instance != null)
            {
                SpriteState states = new SpriteState();
                states.highlightedSprite = Instance.ButtonHoverSprite;
                states.pressedSprite = Instance.ButtonPressSprite;
                states.selectedSprite = Instance.ButtonSprite;
                btn.spriteState = states;
                btn.transition = Selectable.Transition.SpriteSwap;
            }

            // Add text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(obj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 20;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = TextPrimary;
            tmp.alignment = TextAlignmentOptions.Center;

            return btn;
        }

        public static Button CreateSecondaryButton(Transform parent, string text, Vector2 size)
        {
            GameObject obj = new GameObject("Button_" + text);
            obj.transform.SetParent(parent, false);

            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.sizeDelta = size;

            Image img = obj.AddComponent<Image>();
            if (Instance != null)
            {
                img.sprite = Instance.ButtonSecondarySprite ?? Instance.GlassPanelSprite;
                img.type = Image.Type.Sliced;
            }

            Button btn = obj.AddComponent<Button>();

            // Add text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(obj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 18;
            tmp.color = TextSecondary;
            tmp.alignment = TextAlignmentOptions.Center;

            return btn;
        }

        public static Button CreateGlassButton(Transform parent, string text, Vector2 size)
        {
            GameObject obj = new GameObject("Button_" + text);
            obj.transform.SetParent(parent, false);

            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.sizeDelta = size;

            Image img = obj.AddComponent<Image>();
            if (Instance != null)
            {
                img.sprite = Instance.ButtonGlassSprite ?? Instance.GlassPanelLightSprite;
                img.type = Image.Type.Sliced;
            }

            Button btn = obj.AddComponent<Button>();

            // Color tint for hover/press
            ColorBlock colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.1f, 1.1f, 1.15f, 1f);
            colors.pressedColor = new Color(0.9f, 0.9f, 0.95f, 1f);
            colors.selectedColor = Color.white;
            btn.colors = colors;

            // Add text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(obj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 18;
            tmp.color = TextPrimary;
            tmp.alignment = TextAlignmentOptions.Center;

            return btn;
        }

        public static TextMeshProUGUI CreateText(Transform parent, string text, int fontSize, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            GameObject obj = new GameObject("Text");
            obj.transform.SetParent(parent, false);

            RectTransform rect = obj.AddComponent<RectTransform>();

            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = alignment;

            return tmp;
        }

        public static TextMeshProUGUI CreateHeadingText(Transform parent, string text, int fontSize = 32)
        {
            var tmp = CreateText(parent, text, fontSize, TextPrimary, TextAlignmentOptions.Center);
            tmp.fontStyle = FontStyles.Bold;
            return tmp;
        }

        public static TextMeshProUGUI CreateSubheadingText(Transform parent, string text, int fontSize = 18)
        {
            return CreateText(parent, text, fontSize, TextSecondary, TextAlignmentOptions.Center);
        }

        /// <summary>
        /// Adds a subtle pulsing glow animation to a UI element
        /// </summary>
        public static void AddGlowPulse(Image image, Color glowColor, float minAlpha = 0.3f, float maxAlpha = 0.8f, float speed = 1.5f)
        {
            var pulse = image.gameObject.AddComponent<GlowPulseEffect>();
            pulse.Initialize(glowColor, minAlpha, maxAlpha, speed);
        }
    }

    /// <summary>
    /// Simple glow pulse animation component
    /// </summary>
    public class GlowPulseEffect : MonoBehaviour
    {
        private Image targetImage;
        private Color baseColor;
        private float minAlpha;
        private float maxAlpha;
        private float speed;
        private float time;

        public void Initialize(Color color, float min, float max, float pulseSpeed)
        {
            targetImage = GetComponent<Image>();
            baseColor = color;
            minAlpha = min;
            maxAlpha = max;
            speed = pulseSpeed;
        }

        private void Update()
        {
            if (targetImage == null) return;

            time += Time.deltaTime * speed;
            float alpha = Mathf.Lerp(minAlpha, maxAlpha, (Mathf.Sin(time * Mathf.PI) + 1f) / 2f);

            Color c = baseColor;
            c.a = alpha;
            targetImage.color = c;
        }
    }
}
