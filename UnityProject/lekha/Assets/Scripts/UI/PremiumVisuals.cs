using UnityEngine;
using UnityEngine.UI;

namespace Lekha.UI
{
    /// <summary>
    /// Premium visual effects and textures generator
    /// </summary>
    public class PremiumVisuals : MonoBehaviour
    {
        public static PremiumVisuals Instance { get; private set; }

        // Cached textures
        private Texture2D tableTexture;
        private Texture2D buttonNormalTexture;
        private Texture2D buttonHoverTexture;
        private Texture2D buttonPressTexture;
        private Texture2D panelTexture;
        private Texture2D glassTexture;

        // Sprites
        public Sprite TableSprite { get; private set; }
        public Sprite ButtonNormalSprite { get; private set; }
        public Sprite ButtonHoverSprite { get; private set; }
        public Sprite ButtonPressSprite { get; private set; }
        public Sprite PanelSprite { get; private set; }
        public Sprite GlassSprite { get; private set; }
        public Sprite CircleSprite { get; private set; }
        public Sprite GlowSprite { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            GenerateAllVisuals();
        }

        private void GenerateAllVisuals()
        {
            GenerateTableTexture();
            GenerateButtonTextures();
            GeneratePanelTexture();
            GenerateGlassTexture();
            GenerateCircleSprite();
            GenerateGlowSprite();

            Debug.Log("PremiumVisuals: All premium textures generated");
        }

        private void GenerateTableTexture()
        {
            // Simplified static table texture - no noise/animations
            // ModernUITheme is now the primary theme system
            int width = 64;
            int height = 64;
            tableTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tableTexture.filterMode = FilterMode.Bilinear;
            tableTexture.wrapMode = TextureWrapMode.Clamp;

            // Simple solid felt color - no perlin noise
            Color feltColor = new Color(0.05f, 0.28f, 0.12f, 1f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    tableTexture.SetPixel(x, y, feltColor);
                }
            }

            tableTexture.Apply();
            TableSprite = Sprite.Create(tableTexture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100);
        }

        private void GenerateButtonTextures()
        {
            int width = 256;
            int height = 80;
            int cornerRadius = 20;

            // Normal state - golden gradient
            buttonNormalTexture = CreateButtonTexture(width, height, cornerRadius,
                new Color(0.75f, 0.55f, 0.15f),
                new Color(0.55f, 0.4f, 0.1f),
                new Color(0.9f, 0.7f, 0.25f));

            // Hover state - brighter
            buttonHoverTexture = CreateButtonTexture(width, height, cornerRadius,
                new Color(0.85f, 0.65f, 0.2f),
                new Color(0.65f, 0.5f, 0.15f),
                new Color(1f, 0.8f, 0.35f));

            // Press state - darker
            buttonPressTexture = CreateButtonTexture(width, height, cornerRadius,
                new Color(0.55f, 0.4f, 0.1f),
                new Color(0.4f, 0.28f, 0.05f),
                new Color(0.7f, 0.5f, 0.15f));

            ButtonNormalSprite = Sprite.Create(buttonNormalTexture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100);
            ButtonHoverSprite = Sprite.Create(buttonHoverTexture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100);
            ButtonPressSprite = Sprite.Create(buttonPressTexture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100);
        }

        private Texture2D CreateButtonTexture(int width, int height, int cornerRadius, Color topColor, Color bottomColor, Color highlightColor)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (IsInsideRoundedRect(x, y, width, height, cornerRadius))
                    {
                        float gradientT = 1 - (float)y / height;

                        // Main gradient
                        Color color = Color.Lerp(bottomColor, topColor, gradientT);

                        // Top highlight
                        if (y > height * 0.7f)
                        {
                            float highlightT = (y - height * 0.7f) / (height * 0.3f);
                            color = Color.Lerp(color, highlightColor, highlightT * 0.4f);
                        }

                        // Bottom shadow
                        if (y < height * 0.15f)
                        {
                            float shadowT = 1 - y / (height * 0.15f);
                            color = Color.Lerp(color, Color.black, shadowT * 0.3f);
                        }

                        // Edge darkening for 3D effect
                        float edgeDist = GetDistanceToRoundedEdge(x, y, width, height, cornerRadius);
                        if (edgeDist < 3)
                        {
                            color = Color.Lerp(color * 0.7f, color, edgeDist / 3f);
                        }

                        tex.SetPixel(x, y, color);
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

        private void GeneratePanelTexture()
        {
            int width = 256;
            int height = 256;
            int cornerRadius = 25;

            panelTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (IsInsideRoundedRect(x, y, width, height, cornerRadius))
                    {
                        // Dark semi-transparent panel with gradient
                        float gradientT = (float)y / height;
                        Color panelColor = Color.Lerp(
                            new Color(0.08f, 0.05f, 0.12f, 0.92f),
                            new Color(0.04f, 0.02f, 0.08f, 0.95f),
                            gradientT
                        );

                        // Subtle inner glow at top
                        if (y > height * 0.85f)
                        {
                            float glowT = (y - height * 0.85f) / (height * 0.15f);
                            panelColor = Color.Lerp(panelColor, new Color(0.3f, 0.2f, 0.4f, 0.9f), glowT * 0.2f);
                        }

                        // Border
                        float edgeDist = GetDistanceToRoundedEdge(x, y, width, height, cornerRadius);
                        if (edgeDist < 2)
                        {
                            panelColor = Color.Lerp(new Color(0.6f, 0.45f, 0.2f, 0.8f), panelColor, edgeDist / 2f);
                        }

                        panelTexture.SetPixel(x, y, panelColor);
                    }
                    else
                    {
                        panelTexture.SetPixel(x, y, Color.clear);
                    }
                }
            }

            panelTexture.Apply();
            PanelSprite = Sprite.Create(panelTexture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100);
        }

        private void GenerateGlassTexture()
        {
            int width = 256;
            int height = 256;
            int cornerRadius = 20;

            glassTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            glassTexture.filterMode = FilterMode.Bilinear;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (IsInsideRoundedRect(x, y, width, height, cornerRadius))
                    {
                        // Simple glass effect - no perlin noise
                        float gradientT = (float)y / height;

                        // Base glass color with transparency
                        Color glassColor = new Color(1f, 1f, 1f, 0.1f + gradientT * 0.05f);

                        // Top reflection
                        if (y > height * 0.8f)
                        {
                            float reflectT = (y - height * 0.8f) / (height * 0.2f);
                            glassColor.a += reflectT * 0.15f;
                        }

                        // Subtle border
                        float edgeDist = GetDistanceToRoundedEdge(x, y, width, height, cornerRadius);
                        if (edgeDist < 1.5f)
                        {
                            glassColor = Color.Lerp(new Color(1f, 1f, 1f, 0.4f), glassColor, edgeDist / 1.5f);
                        }

                        glassTexture.SetPixel(x, y, glassColor);
                    }
                    else
                    {
                        glassTexture.SetPixel(x, y, Color.clear);
                    }
                }
            }

            glassTexture.Apply();
            GlassSprite = Sprite.Create(glassTexture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100);
        }

        private void GenerateCircleSprite()
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

            int center = size / 2;
            int radius = center - 4;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));

                    if (dist <= radius)
                    {
                        // Smooth anti-aliased circle with gradient
                        float t = dist / radius;
                        float alpha = 1f;

                        // Soft edge
                        if (dist > radius - 2)
                        {
                            alpha = (radius - dist) / 2f;
                        }

                        // Radial gradient for 3D look
                        Color circleColor = Color.Lerp(Color.white, new Color(0.8f, 0.8f, 0.8f), t * 0.5f);
                        circleColor.a = alpha;

                        tex.SetPixel(x, y, circleColor);
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            tex.Apply();
            CircleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100);
        }

        private void GenerateGlowSprite()
        {
            int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

            int center = size / 2;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float maxDist = center;

                    if (dist <= maxDist)
                    {
                        // Soft glow falloff
                        float t = dist / maxDist;
                        float alpha = Mathf.Pow(1 - t, 2) * 0.8f;

                        tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }

            tex.Apply();
            GlowSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100);
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

        private float GetDistanceToRoundedEdge(int x, int y, int width, int height, int radius)
        {
            float minDist = float.MaxValue;

            // Check distance to straight edges
            if (x >= radius && x < width - radius)
            {
                minDist = Mathf.Min(minDist, y);
                minDist = Mathf.Min(minDist, height - 1 - y);
            }
            if (y >= radius && y < height - radius)
            {
                minDist = Mathf.Min(minDist, x);
                minDist = Mathf.Min(minDist, width - 1 - x);
            }

            // Check distance to corner arcs
            Vector2[] cornerCenters = {
                new Vector2(radius, radius),
                new Vector2(width - radius - 1, radius),
                new Vector2(radius, height - radius - 1),
                new Vector2(width - radius - 1, height - radius - 1)
            };

            foreach (var center in cornerCenters)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (dist >= radius - 1)
                {
                    minDist = Mathf.Min(minDist, Mathf.Abs(dist - radius));
                }
            }

            return minDist;
        }

        /// <summary>
        /// Creates a premium styled button with all visual states
        /// </summary>
        public void StyleButton(Button button)
        {
            Image img = button.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = ButtonNormalSprite;
                img.type = Image.Type.Sliced;
            }

            // Set up color transitions
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            colors.selectedColor = Color.white;
            button.colors = colors;

            // Add sprite swap if SpriteState is available
            SpriteState spriteState = new SpriteState();
            spriteState.highlightedSprite = ButtonHoverSprite;
            spriteState.pressedSprite = ButtonPressSprite;
            spriteState.selectedSprite = ButtonNormalSprite;
            button.spriteState = spriteState;
            button.transition = Selectable.Transition.SpriteSwap;
        }
    }
}
