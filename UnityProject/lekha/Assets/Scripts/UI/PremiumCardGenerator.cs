using System.Collections.Generic;
using UnityEngine;
using Lekha.Core;

namespace Lekha.UI
{
    /// <summary>
    /// Generates premium quality card sprites with gradients, shadows, and crisp graphics
    /// </summary>
    public class PremiumCardGenerator : MonoBehaviour
    {
        public static PremiumCardGenerator Instance { get; private set; }

        private Dictionary<string, Sprite> generatedSprites = new Dictionary<string, Sprite>();
        private Sprite cardBackSprite;

        // High resolution card dimensions
        private const int CARD_WIDTH = 512;
        private const int CARD_HEIGHT = 768;
        private const int CORNER_RADIUS = 40;

        // Premium color palettes (matching Uno colors but richer)
        private readonly Color RED_PRIMARY = new Color(0.89f, 0.15f, 0.21f);
        private readonly Color RED_SECONDARY = new Color(0.7f, 0.08f, 0.13f);
        private readonly Color RED_ACCENT = new Color(1f, 0.3f, 0.35f);

        private readonly Color YELLOW_PRIMARY = new Color(0.98f, 0.82f, 0.1f);
        private readonly Color YELLOW_SECONDARY = new Color(0.85f, 0.65f, 0.05f);
        private readonly Color YELLOW_ACCENT = new Color(1f, 0.92f, 0.4f);

        private readonly Color BLUE_PRIMARY = new Color(0.1f, 0.46f, 0.82f);
        private readonly Color BLUE_SECONDARY = new Color(0.05f, 0.3f, 0.6f);
        private readonly Color BLUE_ACCENT = new Color(0.3f, 0.6f, 0.95f);

        private readonly Color GREEN_PRIMARY = new Color(0.18f, 0.72f, 0.33f);
        private readonly Color GREEN_SECONDARY = new Color(0.1f, 0.5f, 0.2f);
        private readonly Color GREEN_ACCENT = new Color(0.4f, 0.85f, 0.5f);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            GenerateAllCards();
        }

        private void GenerateAllCards()
        {
            GenerateCardsForSuit(Suit.Hearts, RED_PRIMARY, RED_SECONDARY, RED_ACCENT);
            GenerateCardsForSuit(Suit.Diamonds, YELLOW_PRIMARY, YELLOW_SECONDARY, YELLOW_ACCENT);
            GenerateCardsForSuit(Suit.Spades, BLUE_PRIMARY, BLUE_SECONDARY, BLUE_ACCENT);
            GenerateCardsForSuit(Suit.Clubs, GREEN_PRIMARY, GREEN_SECONDARY, GREEN_ACCENT);

            cardBackSprite = GeneratePremiumCardBack();

            Debug.Log($"PremiumCardGenerator: Generated {generatedSprites.Count} high-quality card sprites");
        }

        private void GenerateCardsForSuit(Suit suit, Color primary, Color secondary, Color accent)
        {
            foreach (Rank rank in System.Enum.GetValues(typeof(Rank)))
            {
                string key = $"{suit}_{rank}";
                Sprite sprite = GeneratePremiumCard(primary, secondary, accent, rank);
                generatedSprites[key] = sprite;
            }
        }

        private Sprite GeneratePremiumCard(Color primary, Color secondary, Color accent, Rank rank)
        {
            Texture2D texture = new Texture2D(CARD_WIDTH, CARD_HEIGHT, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            // Clear to transparent
            Color[] pixels = new Color[CARD_WIDTH * CARD_HEIGHT];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.clear;
            texture.SetPixels(pixels);

            // Draw card layers
            DrawCardShadow(texture);
            DrawCardBase(texture);
            DrawColoredCenter(texture, primary, secondary);
            DrawCardBorder(texture, primary);
            DrawInnerGlow(texture, accent);
            DrawRankSymbol(texture, rank, Color.white);
            DrawCornerSymbols(texture, rank, primary);

            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, CARD_WIDTH, CARD_HEIGHT), new Vector2(0.5f, 0.5f), 100);
        }

        private void DrawCardShadow(Texture2D tex)
        {
            // Soft shadow offset
            int shadowOffset = 8;
            float shadowAlpha = 0.3f;

            for (int y = 0; y < CARD_HEIGHT; y++)
            {
                for (int x = 0; x < CARD_WIDTH; x++)
                {
                    int shadowX = x - shadowOffset;
                    int shadowY = y + shadowOffset;

                    if (IsInsideRoundedRect(shadowX, shadowY, CARD_WIDTH - shadowOffset * 2, CARD_HEIGHT - shadowOffset * 2, CORNER_RADIUS))
                    {
                        // Distance-based shadow falloff
                        float dist = GetDistanceToEdge(shadowX, shadowY, CARD_WIDTH - shadowOffset * 2, CARD_HEIGHT - shadowOffset * 2, CORNER_RADIUS);
                        float alpha = Mathf.Clamp01(shadowAlpha * (1 - dist / 20f));
                        Color current = tex.GetPixel(x, y);
                        tex.SetPixel(x, y, Color.Lerp(current, new Color(0, 0, 0, alpha), alpha));
                    }
                }
            }
        }

        private void DrawCardBase(Texture2D tex)
        {
            // White card base with subtle gradient
            for (int y = 0; y < CARD_HEIGHT; y++)
            {
                for (int x = 0; x < CARD_WIDTH; x++)
                {
                    if (IsInsideRoundedRect(x, y, CARD_WIDTH, CARD_HEIGHT, CORNER_RADIUS))
                    {
                        // Subtle vertical gradient for depth
                        float gradientT = (float)y / CARD_HEIGHT;
                        Color baseColor = Color.Lerp(new Color(0.98f, 0.98f, 0.98f), new Color(0.92f, 0.92f, 0.92f), gradientT * 0.3f);
                        tex.SetPixel(x, y, baseColor);
                    }
                }
            }
        }

        private void DrawColoredCenter(Texture2D tex, Color primary, Color secondary)
        {
            // Large colored oval in center with gradient
            int centerX = CARD_WIDTH / 2;
            int centerY = CARD_HEIGHT / 2;
            int radiusX = 180;
            int radiusY = 260;

            for (int y = centerY - radiusY; y <= centerY + radiusY; y++)
            {
                for (int x = centerX - radiusX; x <= centerX + radiusX; x++)
                {
                    if (x < 0 || x >= CARD_WIDTH || y < 0 || y >= CARD_HEIGHT) continue;

                    float dx = (float)(x - centerX) / radiusX;
                    float dy = (float)(y - centerY) / radiusY;
                    float dist = dx * dx + dy * dy;

                    if (dist <= 1)
                    {
                        // Radial gradient from center
                        float gradientT = Mathf.Sqrt(dist);
                        Color color = Color.Lerp(primary, secondary, gradientT * 0.7f);

                        // Add subtle highlight at top
                        float highlight = Mathf.Max(0, 1 - ((float)(y - (centerY - radiusY)) / (radiusY * 0.5f)));
                        color = Color.Lerp(color, Color.white, highlight * 0.15f);

                        // Smooth edge anti-aliasing
                        if (dist > 0.95f)
                        {
                            float edgeAlpha = 1 - (dist - 0.95f) / 0.05f;
                            Color bgColor = tex.GetPixel(x, y);
                            color = Color.Lerp(bgColor, color, edgeAlpha);
                        }

                        tex.SetPixel(x, y, color);
                    }
                }
            }
        }

        private void DrawCardBorder(Texture2D tex, Color borderColor)
        {
            int borderWidth = 6;

            for (int y = 0; y < CARD_HEIGHT; y++)
            {
                for (int x = 0; x < CARD_WIDTH; x++)
                {
                    bool insideOuter = IsInsideRoundedRect(x, y, CARD_WIDTH, CARD_HEIGHT, CORNER_RADIUS);
                    bool insideInner = IsInsideRoundedRect(x - borderWidth, y - borderWidth,
                        CARD_WIDTH - borderWidth * 2, CARD_HEIGHT - borderWidth * 2, CORNER_RADIUS - borderWidth);

                    if (insideOuter && !insideInner)
                    {
                        // Gradient border for 3D effect
                        float gradientT = (float)y / CARD_HEIGHT;
                        Color color = Color.Lerp(borderColor * 1.2f, borderColor * 0.8f, gradientT);
                        tex.SetPixel(x, y, color);
                    }
                }
            }
        }

        private void DrawInnerGlow(Texture2D tex, Color glowColor)
        {
            // Inner glow around the colored center
            int centerX = CARD_WIDTH / 2;
            int centerY = CARD_HEIGHT / 2;
            int radiusX = 185;
            int radiusY = 265;
            int glowWidth = 15;

            for (int y = centerY - radiusY - glowWidth; y <= centerY + radiusY + glowWidth; y++)
            {
                for (int x = centerX - radiusX - glowWidth; x <= centerX + radiusX + glowWidth; x++)
                {
                    if (x < 0 || x >= CARD_WIDTH || y < 0 || y >= CARD_HEIGHT) continue;

                    float dx = (float)(x - centerX) / radiusX;
                    float dy = (float)(y - centerY) / radiusY;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist > 0.95f && dist < 1.1f)
                    {
                        float glowAlpha = 1 - Mathf.Abs(dist - 1.02f) / 0.12f;
                        glowAlpha = Mathf.Clamp01(glowAlpha) * 0.4f;

                        Color current = tex.GetPixel(x, y);
                        Color glowWithAlpha = new Color(glowColor.r, glowColor.g, glowColor.b, glowAlpha);
                        tex.SetPixel(x, y, Color.Lerp(current, glowColor, glowAlpha));
                    }
                }
            }
        }

        private void DrawRankSymbol(Texture2D tex, Rank rank, Color color)
        {
            string symbol = GetRankSymbol(rank);
            int centerX = CARD_WIDTH / 2;
            int centerY = CARD_HEIGHT / 2;

            // Large center symbol with shadow
            DrawTextWithShadow(tex, symbol, centerX, centerY, color, 120, true);
        }

        private void DrawCornerSymbols(Texture2D tex, Rank rank, Color color)
        {
            string symbol = GetRankSymbol(rank);

            // Top-left corner
            DrawTextWithShadow(tex, symbol, 55, CARD_HEIGHT - 80, color, 48, false);

            // Bottom-right corner (rotated 180 would need special handling, just mirror position)
            DrawTextWithShadow(tex, symbol, CARD_WIDTH - 55, 80, color, 48, false);
        }

        private void DrawTextWithShadow(Texture2D tex, string text, int centerX, int centerY, Color color, int fontSize, bool bold)
        {
            // Draw shadow first
            DrawBitmapText(tex, text, centerX + 3, centerY - 3, new Color(0, 0, 0, 0.4f), fontSize, bold);
            // Draw main text
            DrawBitmapText(tex, text, centerX, centerY, color, fontSize, bold);
        }

        private void DrawBitmapText(Texture2D tex, string text, int centerX, int centerY, Color color, int fontSize, bool bold)
        {
            int charWidth = fontSize * 6 / 10;
            int charHeight = fontSize;

            int totalWidth = text.Length * charWidth;
            int startX = centerX - totalWidth / 2;
            int startY = centerY - charHeight / 2;

            for (int i = 0; i < text.Length; i++)
            {
                DrawPremiumCharacter(tex, text[i], startX + i * charWidth, startY, charWidth, charHeight, color, bold);
            }
        }

        private void DrawPremiumCharacter(Texture2D tex, char c, int x, int y, int width, int height, Color color, bool bold)
        {
            bool[,] pattern = GetCharacterPattern(c);
            if (pattern == null) return;

            int patternHeight = pattern.GetLength(0);
            int patternWidth = pattern.GetLength(1);

            float scaleX = (float)width / patternWidth;
            float scaleY = (float)height / patternHeight;

            int boldOffset = bold ? 2 : 1;

            for (int py = 0; py < height; py++)
            {
                for (int px = 0; px < width; px++)
                {
                    int patX = Mathf.Clamp((int)(px / scaleX), 0, patternWidth - 1);
                    int patY = Mathf.Clamp((int)(py / scaleY), 0, patternHeight - 1);

                    bool shouldDraw = pattern[patternHeight - 1 - patY, patX];

                    // For bold, also check adjacent pixels
                    if (bold && !shouldDraw)
                    {
                        for (int ox = -1; ox <= 1 && !shouldDraw; ox++)
                        {
                            for (int oy = -1; oy <= 1 && !shouldDraw; oy++)
                            {
                                int checkX = Mathf.Clamp(patX + ox, 0, patternWidth - 1);
                                int checkY = Mathf.Clamp(patternHeight - 1 - patY + oy, 0, patternHeight - 1);
                                if (pattern[checkY, checkX]) shouldDraw = true;
                            }
                        }
                    }

                    if (shouldDraw)
                    {
                        int texX = x + px;
                        int texY = y + py;
                        if (texX >= 0 && texX < tex.width && texY >= 0 && texY < tex.height)
                        {
                            Color current = tex.GetPixel(texX, texY);
                            tex.SetPixel(texX, texY, Color.Lerp(current, color, color.a));
                        }
                    }
                }
            }
        }

        private Sprite GeneratePremiumCardBack()
        {
            Texture2D texture = new Texture2D(CARD_WIDTH, CARD_HEIGHT, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;

            // Rich dark gradient background
            for (int y = 0; y < CARD_HEIGHT; y++)
            {
                for (int x = 0; x < CARD_WIDTH; x++)
                {
                    if (IsInsideRoundedRect(x, y, CARD_WIDTH, CARD_HEIGHT, CORNER_RADIUS))
                    {
                        float gradientT = (float)y / CARD_HEIGHT;
                        Color bgColor = Color.Lerp(
                            new Color(0.15f, 0.08f, 0.2f),
                            new Color(0.08f, 0.04f, 0.12f),
                            gradientT
                        );
                        texture.SetPixel(x, y, bgColor);
                    }
                    else
                    {
                        texture.SetPixel(x, y, Color.clear);
                    }
                }
            }

            // Elegant pattern - diagonal lines
            for (int y = 0; y < CARD_HEIGHT; y++)
            {
                for (int x = 0; x < CARD_WIDTH; x++)
                {
                    if (IsInsideRoundedRect(x, y, CARD_WIDTH, CARD_HEIGHT, CORNER_RADIUS))
                    {
                        // Diamond pattern
                        int patternSize = 30;
                        int px = (x + y) % patternSize;
                        int py = (x - y + CARD_HEIGHT) % patternSize;

                        if ((px < 2 || py < 2) && x > 30 && x < CARD_WIDTH - 30 && y > 30 && y < CARD_HEIGHT - 30)
                        {
                            Color current = texture.GetPixel(x, y);
                            texture.SetPixel(x, y, Color.Lerp(current, new Color(0.3f, 0.15f, 0.4f), 0.3f));
                        }
                    }
                }
            }

            // Golden border
            int borderWidth = 8;
            Color goldPrimary = new Color(0.85f, 0.65f, 0.2f);
            Color goldSecondary = new Color(0.6f, 0.45f, 0.1f);

            for (int y = 0; y < CARD_HEIGHT; y++)
            {
                for (int x = 0; x < CARD_WIDTH; x++)
                {
                    bool insideOuter = IsInsideRoundedRect(x, y, CARD_WIDTH, CARD_HEIGHT, CORNER_RADIUS);
                    bool insideInner = IsInsideRoundedRect(x - borderWidth, y - borderWidth,
                        CARD_WIDTH - borderWidth * 2, CARD_HEIGHT - borderWidth * 2, CORNER_RADIUS - borderWidth);

                    if (insideOuter && !insideInner)
                    {
                        float gradientT = (float)y / CARD_HEIGHT;
                        Color borderColor = Color.Lerp(goldPrimary, goldSecondary, gradientT);
                        texture.SetPixel(x, y, borderColor);
                    }
                }
            }

            // Inner decorative border
            int innerBorderOffset = 25;
            int innerBorderWidth = 3;

            for (int y = 0; y < CARD_HEIGHT; y++)
            {
                for (int x = 0; x < CARD_WIDTH; x++)
                {
                    bool insideOuter = IsInsideRoundedRect(x - innerBorderOffset, y - innerBorderOffset,
                        CARD_WIDTH - innerBorderOffset * 2, CARD_HEIGHT - innerBorderOffset * 2, CORNER_RADIUS - innerBorderOffset);
                    bool insideInner = IsInsideRoundedRect(x - innerBorderOffset - innerBorderWidth, y - innerBorderOffset - innerBorderWidth,
                        CARD_WIDTH - (innerBorderOffset + innerBorderWidth) * 2, CARD_HEIGHT - (innerBorderOffset + innerBorderWidth) * 2,
                        CORNER_RADIUS - innerBorderOffset - innerBorderWidth);

                    if (insideOuter && !insideInner)
                    {
                        texture.SetPixel(x, y, new Color(0.7f, 0.5f, 0.15f, 0.6f));
                    }
                }
            }

            // Center emblem - "L" for Lekha
            DrawCenterEmblem(texture);

            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, CARD_WIDTH, CARD_HEIGHT), new Vector2(0.5f, 0.5f), 100);
        }

        private void DrawCenterEmblem(Texture2D tex)
        {
            int centerX = CARD_WIDTH / 2;
            int centerY = CARD_HEIGHT / 2;
            int emblemRadius = 80;

            // Circular emblem background
            for (int y = centerY - emblemRadius; y <= centerY + emblemRadius; y++)
            {
                for (int x = centerX - emblemRadius; x <= centerX + emblemRadius; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                    if (dist <= emblemRadius)
                    {
                        float t = dist / emblemRadius;
                        Color emblemColor = Color.Lerp(
                            new Color(0.85f, 0.65f, 0.2f),
                            new Color(0.6f, 0.4f, 0.1f),
                            t
                        );

                        // Anti-aliased edge
                        if (dist > emblemRadius - 2)
                        {
                            float edgeAlpha = (emblemRadius - dist) / 2f;
                            Color current = tex.GetPixel(x, y);
                            emblemColor = Color.Lerp(current, emblemColor, edgeAlpha);
                        }

                        tex.SetPixel(x, y, emblemColor);
                    }
                }
            }

            // Draw "L" letter
            DrawBitmapText(tex, "L", centerX, centerY, new Color(0.15f, 0.08f, 0.2f), 80, true);
        }

        private string GetRankSymbol(Rank rank)
        {
            return rank switch
            {
                Rank.Ace => "1",
                Rank.Two => "2",
                Rank.Three => "3",
                Rank.Four => "4",
                Rank.Five => "5",
                Rank.Six => "6",
                Rank.Seven => "7",
                Rank.Eight => "8",
                Rank.Nine => "9",
                Rank.Ten => "10",
                Rank.Jack => "R",
                Rank.Queen => "+2",
                Rank.King => "S",
                _ => "?"
            };
        }

        private bool IsInsideRoundedRect(int x, int y, int width, int height, int radius)
        {
            if (x < 0 || y < 0 || x >= width || y >= height) return false;

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

        private float GetDistanceToEdge(int x, int y, int width, int height, int radius)
        {
            float minDist = float.MaxValue;

            // Distance to each edge
            minDist = Mathf.Min(minDist, x);
            minDist = Mathf.Min(minDist, y);
            minDist = Mathf.Min(minDist, width - x);
            minDist = Mathf.Min(minDist, height - y);

            return minDist;
        }

        private bool[,] GetCharacterPattern(char c)
        {
            return c switch
            {
                '0' => new bool[,] {
                    {false,true,true,true,false},
                    {true,false,false,false,true},
                    {true,false,false,true,true},
                    {true,false,true,false,true},
                    {true,true,false,false,true},
                    {true,false,false,false,true},
                    {false,true,true,true,false}
                },
                '1' => new bool[,] {
                    {false,false,true,false,false},
                    {false,true,true,false,false},
                    {true,false,true,false,false},
                    {false,false,true,false,false},
                    {false,false,true,false,false},
                    {false,false,true,false,false},
                    {true,true,true,true,true}
                },
                '2' => new bool[,] {
                    {false,true,true,true,false},
                    {true,false,false,false,true},
                    {false,false,false,false,true},
                    {false,false,true,true,false},
                    {false,true,false,false,false},
                    {true,false,false,false,false},
                    {true,true,true,true,true}
                },
                '3' => new bool[,] {
                    {false,true,true,true,false},
                    {true,false,false,false,true},
                    {false,false,false,false,true},
                    {false,false,true,true,false},
                    {false,false,false,false,true},
                    {true,false,false,false,true},
                    {false,true,true,true,false}
                },
                '4' => new bool[,] {
                    {false,false,false,true,false},
                    {false,false,true,true,false},
                    {false,true,false,true,false},
                    {true,false,false,true,false},
                    {true,true,true,true,true},
                    {false,false,false,true,false},
                    {false,false,false,true,false}
                },
                '5' => new bool[,] {
                    {true,true,true,true,true},
                    {true,false,false,false,false},
                    {true,true,true,true,false},
                    {false,false,false,false,true},
                    {false,false,false,false,true},
                    {true,false,false,false,true},
                    {false,true,true,true,false}
                },
                '6' => new bool[,] {
                    {false,true,true,true,false},
                    {true,false,false,false,false},
                    {true,false,false,false,false},
                    {true,true,true,true,false},
                    {true,false,false,false,true},
                    {true,false,false,false,true},
                    {false,true,true,true,false}
                },
                '7' => new bool[,] {
                    {true,true,true,true,true},
                    {false,false,false,false,true},
                    {false,false,false,true,false},
                    {false,false,true,false,false},
                    {false,true,false,false,false},
                    {false,true,false,false,false},
                    {false,true,false,false,false}
                },
                '8' => new bool[,] {
                    {false,true,true,true,false},
                    {true,false,false,false,true},
                    {true,false,false,false,true},
                    {false,true,true,true,false},
                    {true,false,false,false,true},
                    {true,false,false,false,true},
                    {false,true,true,true,false}
                },
                '9' => new bool[,] {
                    {false,true,true,true,false},
                    {true,false,false,false,true},
                    {true,false,false,false,true},
                    {false,true,true,true,true},
                    {false,false,false,false,true},
                    {false,false,false,false,true},
                    {false,true,true,true,false}
                },
                '+' => new bool[,] {
                    {false,false,false,false,false},
                    {false,false,true,false,false},
                    {false,false,true,false,false},
                    {true,true,true,true,true},
                    {false,false,true,false,false},
                    {false,false,true,false,false},
                    {false,false,false,false,false}
                },
                'R' => new bool[,] {
                    {true,true,true,true,false},
                    {true,false,false,false,true},
                    {true,false,false,false,true},
                    {true,true,true,true,false},
                    {true,false,true,false,false},
                    {true,false,false,true,false},
                    {true,false,false,false,true}
                },
                'S' => new bool[,] {
                    {false,true,true,true,false},
                    {true,false,false,false,true},
                    {true,false,false,false,false},
                    {false,true,true,true,false},
                    {false,false,false,false,true},
                    {true,false,false,false,true},
                    {false,true,true,true,false}
                },
                'L' => new bool[,] {
                    {true,false,false,false,false},
                    {true,false,false,false,false},
                    {true,false,false,false,false},
                    {true,false,false,false,false},
                    {true,false,false,false,false},
                    {true,false,false,false,false},
                    {true,true,true,true,true}
                },
                _ => null
            };
        }

        public Sprite GetCardSprite(Card card)
        {
            return GetCardSprite(card.Suit, card.Rank);
        }

        public Sprite GetCardSprite(Suit suit, Rank rank)
        {
            string key = $"{suit}_{rank}";
            if (generatedSprites.TryGetValue(key, out Sprite sprite))
            {
                return sprite;
            }
            return cardBackSprite;
        }

        public Sprite GetCardBack()
        {
            return cardBackSprite;
        }
    }
}
