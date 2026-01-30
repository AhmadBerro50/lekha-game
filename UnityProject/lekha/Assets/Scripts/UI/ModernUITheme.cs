using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Lekha.UI
{
    /// <summary>
    /// Casino-style UI Theme - Realistic felt table with elegant wood frame
    /// Deep green felt, rich wood accents, and premium card game aesthetics
    /// </summary>
    public class ModernUITheme : MonoBehaviour
    {
        public static ModernUITheme Instance { get; private set; }

        // Color Palette - Casino/Poker Table Theme
        public static readonly Color FeltGreen = new Color(0.05f, 0.28f, 0.15f, 1f);          // Deep casino green
        public static readonly Color FeltGreenLight = new Color(0.08f, 0.35f, 0.18f, 1f);     // Lighter felt center
        public static readonly Color FeltGreenDark = new Color(0.02f, 0.15f, 0.08f, 1f);      // Dark felt edges
        public static readonly Color WoodDark = new Color(0.25f, 0.15f, 0.08f, 1f);           // Dark mahogany
        public static readonly Color WoodMid = new Color(0.35f, 0.22f, 0.12f, 1f);            // Mid wood
        public static readonly Color WoodLight = new Color(0.45f, 0.30f, 0.18f, 1f);          // Light wood highlight
        public static readonly Color GoldAccent = new Color(0.85f, 0.70f, 0.35f, 1f);         // Gold trim
        public static readonly Color GoldBright = new Color(0.95f, 0.82f, 0.45f, 1f);         // Bright gold
        public static readonly Color GoldDark = new Color(0.65f, 0.50f, 0.20f, 1f);           // Dark gold

        // UI Colors
        public static readonly Color PrimaryDark = new Color(0.12f, 0.08f, 0.05f, 1f);        // Near black brown
        public static readonly Color PrimaryMid = new Color(0.18f, 0.12f, 0.08f, 1f);         // Dark brown
        public static readonly Color AccentGold = GoldAccent;
        public static readonly Color AccentGoldDark = GoldDark;
        public static readonly Color AccentCyan = new Color(0.4f, 0.85f, 0.9f, 1f);           // Cyan for highlights
        public static readonly Color AccentPurple = new Color(0.6f, 0.4f, 0.9f, 1f);          // Purple accent
        public static readonly Color TextPrimary = new Color(0.95f, 0.92f, 0.85f, 1f);        // Warm cream white
        public static readonly Color TextSecondary = new Color(0.75f, 0.70f, 0.60f, 1f);      // Muted tan
        public static readonly Color GlassWhite = new Color(1f, 1f, 1f, 0.08f);
        public static readonly Color GlassBorder = new Color(1f, 1f, 1f, 0.15f);
        public static readonly Color Success = new Color(0.3f, 0.85f, 0.5f, 1f);
        public static readonly Color Warning = new Color(0.95f, 0.65f, 0.2f, 1f);
        public static readonly Color Danger = new Color(0.95f, 0.35f, 0.35f, 1f);

        // Team Colors
        public static readonly Color TeamNorthSouth = new Color(0.35f, 0.65f, 0.9f, 1f);      // Blue team
        public static readonly Color TeamEastWest = new Color(0.9f, 0.55f, 0.35f, 1f);        // Orange team

        // Generated Sprites
        public Sprite TableSprite { get; private set; }
        public Sprite GlassPanelSprite { get; private set; }
        public Sprite GlassPanelDarkSprite { get; private set; }
        public Sprite ButtonSprite { get; private set; }
        public Sprite ButtonHoverSprite { get; private set; }
        public Sprite ButtonPressSprite { get; private set; }
        public Sprite CircleSprite { get; private set; }
        public Sprite CircleOutlineSprite { get; private set; }
        public Sprite PillSprite { get; private set; }
        public Sprite SoftGlowSprite { get; private set; }
        public Sprite CardSlotSprite { get; private set; }
        public Sprite GradientHorizontalSprite { get; private set; }
        public Sprite GradientVerticalSprite { get; private set; }
        public Sprite CornerPanelSprite { get; private set; }
        public Sprite WoodFrameSprite { get; private set; }

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
            Debug.Log("[ModernUITheme] Casino-style UI assets generated");
        }

        private void GenerateAllAssets()
        {
            GenerateTableTexture();
            GenerateGlassPanels();
            GenerateButtons();
            GenerateShapes();
            GenerateGradients();
            GenerateCornerPanel();
        }

        private void GenerateTableTexture()
        {
            int size = 1024;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            float centerX = size / 2f;
            float centerY = size / 2f;

            // Use a fixed seed for deterministic noise pattern
            System.Random rand = new System.Random(12345);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Distance from center for radial gradient
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                    float maxDist = size * 0.7f;
                    float radialT = Mathf.Clamp01(dist / maxDist);

                    // Smooth gradient from center to edges - realistic felt look
                    Color baseColor;
                    if (radialT < 0.3f)
                    {
                        // Center area - lighter green
                        float t = radialT / 0.3f;
                        baseColor = Color.Lerp(FeltGreenLight, FeltGreen, t * t);
                    }
                    else if (radialT < 0.8f)
                    {
                        // Main play area
                        float t = (radialT - 0.3f) / 0.5f;
                        baseColor = Color.Lerp(FeltGreen, FeltGreenDark, t * t);
                    }
                    else
                    {
                        // Edge area - darker
                        float t = (radialT - 0.8f) / 0.2f;
                        baseColor = Color.Lerp(FeltGreenDark, FeltGreenDark * 0.7f, t);
                    }

                    // Add felt texture - fine fiber pattern
                    float fiberNoiseX = Mathf.PerlinNoise(x * 0.15f, y * 0.02f);
                    float fiberNoiseY = Mathf.PerlinNoise(x * 0.02f, y * 0.15f);
                    float fiberNoise = (fiberNoiseX * 0.6f + fiberNoiseY * 0.4f) * 0.04f - 0.02f;

                    // Very subtle random variation for natural look
                    float microNoise = ((float)rand.NextDouble() - 0.5f) * 0.008f;

                    baseColor.r = Mathf.Clamp01(baseColor.r + fiberNoise + microNoise);
                    baseColor.g = Mathf.Clamp01(baseColor.g + fiberNoise * 1.5f + microNoise);
                    baseColor.b = Mathf.Clamp01(baseColor.b + fiberNoise * 0.8f + microNoise);

                    // Subtle center spotlight effect
                    float spotlight = 1f - radialT * 0.15f;
                    baseColor *= spotlight;

                    // Soft vignette at edges
                    if (radialT > 0.7f)
                    {
                        float vignette = (radialT - 0.7f) / 0.3f;
                        baseColor *= (1f - vignette * 0.3f);
                    }

                    tex.SetPixel(x, y, baseColor);
                }
            }

            tex.Apply();
            TableSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100);
        }

        private void GenerateCornerPanel()
        {
            int width = 256;
            int height = 128;
            int radius = 16;

            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color panelColor = new Color(0.08f, 0.05f, 0.03f, 0.92f);
            Color borderColor = GoldAccent * 0.7f;
            borderColor.a = 0.8f;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float dist = DistanceToRoundedRect(x, y, width, height, radius);

                    if (dist <= 0)
                    {
                        // Inside panel
                        float gradY = (float)y / height;
                        Color col = panelColor;

                        // Subtle vertical gradient for depth
                        col *= (0.9f + gradY * 0.2f);

                        // Gold border effect
                        if (dist > -3f)
                        {
                            float borderT = (dist + 3f) / 3f;
                            col = Color.Lerp(col, borderColor, borderT * 0.6f);
                        }

                        tex.SetPixel(x, y, col);
                    }
                    else if (dist < 2f)
                    {
                        // Anti-aliased edge
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

            // Create 9-slice sprite with proper borders
            Vector4 border = new Vector4(radius + 4, radius + 4, radius + 4, radius + 4);
            CornerPanelSprite = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, border);
        }

        private void GenerateGlassPanels()
        {
            // Glass panel - dark elegant
            GlassPanelSprite = CreateRoundedRect(256, 256, 24,
                new Color(0.1f, 0.08f, 0.05f, 0.85f),
                new Color(GoldAccent.r, GoldAccent.g, GoldAccent.b, 0.3f),
                2f);

            // Darker glass panel
            GlassPanelDarkSprite = CreateRoundedRect(256, 256, 24,
                new Color(0.06f, 0.04f, 0.02f, 0.92f),
                new Color(GoldAccent.r, GoldAccent.g, GoldAccent.b, 0.25f),
                1.5f);

            // Card slot
            CardSlotSprite = CreateRoundedRect(128, 180, 12,
                new Color(0f, 0f, 0f, 0.25f),
                new Color(1f, 1f, 1f, 0.08f),
                1f);
        }

        private void GenerateButtons()
        {
            int width = 200;
            int height = 56;
            int radius = 12;

            // Normal - elegant gold gradient
            ButtonSprite = CreateGradientPill(width, height, radius,
                GoldBright, GoldDark, 0.2f);

            // Hover - brighter
            ButtonHoverSprite = CreateGradientPill(width, height, radius,
                new Color(1f, 0.88f, 0.55f, 1f), GoldAccent, 0.25f);

            // Pressed - darker
            ButtonPressSprite = CreateGradientPill(width, height, radius,
                GoldDark, new Color(0.45f, 0.32f, 0.12f, 1f), 0.1f);
        }

        private void GenerateShapes()
        {
            // Circle - smooth with subtle gradient
            CircleSprite = CreateCircle(128, Color.white, true);

            // Circle outline
            CircleOutlineSprite = CreateCircleOutline(128, 4, Color.white);

            // Pill shape
            PillSprite = CreateRoundedRect(128, 48, 24, Color.white, Color.clear, 0);

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
        }

        private Sprite CreateRoundedRect(int width, int height, int radius, Color fillColor, Color borderColor, float borderWidth)
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
                        if (borderWidth > 0 && dist > -borderWidth)
                        {
                            float t = (dist + borderWidth) / borderWidth;
                            tex.SetPixel(x, y, Color.Lerp(fillColor, borderColor, t));
                        }
                        else
                        {
                            tex.SetPixel(x, y, fillColor);
                        }
                    }
                    else if (dist < 1.5f)
                    {
                        float alpha = 1f - dist / 1.5f;
                        Color c = borderWidth > 0 ? borderColor : fillColor;
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

        private Sprite CreateGradientPill(int width, int height, int radius, Color topColor, Color bottomColor, float glossStrength)
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
                        if (gradT > 0.65f)
                        {
                            float glossT = (gradT - 0.65f) / 0.35f;
                            baseColor = Color.Lerp(baseColor, Color.white, glossT * glossStrength);
                        }

                        // Subtle inner shadow at bottom
                        if (gradT < 0.12f)
                        {
                            baseColor *= (0.75f + gradT * 2f);
                        }

                        tex.SetPixel(x, y, baseColor);
                    }
                    else if (dist < 1.5f)
                    {
                        float alpha = 1f - dist / 1.5f;
                        Color c = Color.Lerp(bottomColor, topColor, 0.5f);
                        c.a = alpha;
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
                            c = Color.Lerp(color, color * 0.8f, t * 0.3f);
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

        // Helper methods for UI creation
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
            tmp.fontSize = 22;
            tmp.fontStyle = FontStyles.Bold;
            tmp.color = PrimaryDark;
            tmp.alignment = TextAlignmentOptions.Center;

            // Add shadow
            Shadow shadow = obj.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.4f);
            shadow.effectDistance = new Vector2(0, -3);

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
    }
}
