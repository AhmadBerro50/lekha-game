using System.Collections.Generic;
using UnityEngine;
using Lekha.Core;

namespace Lekha.UI
{
    /// <summary>
    /// Generates card sprites programmatically for sharp, clear cards at any resolution
    /// </summary>
    public class CardGenerator : MonoBehaviour
    {
        public static CardGenerator Instance { get; private set; }

        private Dictionary<string, Sprite> generatedSprites = new Dictionary<string, Sprite>();
        private Sprite cardBackSprite;

        // Card dimensions (will be crisp at any size)
        private const int CARD_WIDTH = 200;
        private const int CARD_HEIGHT = 300;
        private const int CORNER_RADIUS = 20;
        private const int BORDER_WIDTH = 8;

        // Colors
        private readonly Color RED_COLOR = new Color(0.9f, 0.15f, 0.15f);
        private readonly Color YELLOW_COLOR = new Color(1f, 0.85f, 0.1f);
        private readonly Color BLUE_COLOR = new Color(0.1f, 0.4f, 0.9f);
        private readonly Color GREEN_COLOR = new Color(0.1f, 0.7f, 0.2f);
        private readonly Color WHITE_COLOR = Color.white;
        private readonly Color BLACK_COLOR = new Color(0.1f, 0.1f, 0.1f);

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
            // Generate all 52 cards
            GenerateCardsForSuit(Suit.Hearts, RED_COLOR);
            GenerateCardsForSuit(Suit.Diamonds, YELLOW_COLOR);
            GenerateCardsForSuit(Suit.Spades, BLUE_COLOR);
            GenerateCardsForSuit(Suit.Clubs, GREEN_COLOR);

            // Generate card back
            cardBackSprite = GenerateCardBack();

            Debug.Log($"CardGenerator: Generated {generatedSprites.Count} card sprites");
        }

        private void GenerateCardsForSuit(Suit suit, Color color)
        {
            foreach (Rank rank in System.Enum.GetValues(typeof(Rank)))
            {
                string key = $"{suit}_{rank}";
                Sprite sprite = GenerateCard(color, rank);
                generatedSprites[key] = sprite;
            }
        }

        private Sprite GenerateCard(Color cardColor, Rank rank)
        {
            Texture2D texture = new Texture2D(CARD_WIDTH, CARD_HEIGHT, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;

            // Fill with transparent
            Color[] pixels = new Color[CARD_WIDTH * CARD_HEIGHT];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.clear;
            texture.SetPixels(pixels);

            // Draw white background with rounded corners
            DrawRoundedRect(texture, 0, 0, CARD_WIDTH, CARD_HEIGHT, CORNER_RADIUS, WHITE_COLOR);

            // Draw colored border
            DrawRoundedRectBorder(texture, BORDER_WIDTH, BORDER_WIDTH,
                CARD_WIDTH - BORDER_WIDTH * 2, CARD_HEIGHT - BORDER_WIDTH * 2,
                CORNER_RADIUS - BORDER_WIDTH, cardColor, 6);

            // Draw center oval/ellipse
            DrawFilledEllipse(texture, CARD_WIDTH / 2, CARD_HEIGHT / 2, 70, 100, cardColor);

            // Draw the rank symbol
            string symbol = GetRankSymbol(rank);
            DrawText(texture, symbol, CARD_WIDTH / 2, CARD_HEIGHT / 2, WHITE_COLOR, true);

            // Draw corner symbols (top-left and bottom-right)
            DrawText(texture, symbol, 25, CARD_HEIGHT - 40, cardColor, false);
            DrawText(texture, symbol, CARD_WIDTH - 25, 40, cardColor, false);

            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, CARD_WIDTH, CARD_HEIGHT), new Vector2(0.5f, 0.5f), 100);
        }

        private Sprite GenerateCardBack()
        {
            Texture2D texture = new Texture2D(CARD_WIDTH, CARD_HEIGHT, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;

            // Fill with transparent
            Color[] pixels = new Color[CARD_WIDTH * CARD_HEIGHT];
            texture.SetPixels(pixels);

            // Draw dark background
            DrawRoundedRect(texture, 0, 0, CARD_WIDTH, CARD_HEIGHT, CORNER_RADIUS, BLACK_COLOR);

            // Draw pattern
            Color patternColor = new Color(0.2f, 0.2f, 0.2f);
            for (int y = 20; y < CARD_HEIGHT - 20; y += 20)
            {
                for (int x = 20; x < CARD_WIDTH - 20; x += 20)
                {
                    DrawFilledEllipse(texture, x, y, 6, 6, patternColor);
                }
            }

            // Draw border
            DrawRoundedRectBorder(texture, 10, 10, CARD_WIDTH - 20, CARD_HEIGHT - 20,
                CORNER_RADIUS - 5, new Color(0.8f, 0.1f, 0.1f), 4);

            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, CARD_WIDTH, CARD_HEIGHT), new Vector2(0.5f, 0.5f), 100);
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
                Rank.Ten => "0",
                Rank.Jack => "R",      // Reverse
                Rank.Queen => "+2",    // Draw 2
                Rank.King => "S",      // Skip
                _ => "?"
            };
        }

        private void DrawRoundedRect(Texture2D tex, int x, int y, int width, int height, int radius, Color color)
        {
            for (int py = y; py < y + height; py++)
            {
                for (int px = x; px < x + width; px++)
                {
                    if (IsInsideRoundedRect(px - x, py - y, width, height, radius))
                    {
                        if (px >= 0 && px < tex.width && py >= 0 && py < tex.height)
                            tex.SetPixel(px, py, color);
                    }
                }
            }
        }

        private void DrawRoundedRectBorder(Texture2D tex, int x, int y, int width, int height, int radius, Color color, int thickness)
        {
            for (int py = y; py < y + height; py++)
            {
                for (int px = x; px < x + width; px++)
                {
                    bool inside = IsInsideRoundedRect(px - x, py - y, width, height, radius);
                    bool insideInner = IsInsideRoundedRect(px - x - thickness, py - y - thickness,
                        width - thickness * 2, height - thickness * 2, Mathf.Max(0, radius - thickness));

                    if (inside && !insideInner)
                    {
                        if (px >= 0 && px < tex.width && py >= 0 && py < tex.height)
                            tex.SetPixel(px, py, color);
                    }
                }
            }
        }

        private bool IsInsideRoundedRect(int x, int y, int width, int height, int radius)
        {
            // Check corners
            if (x < radius && y < radius)
                return (x - radius) * (x - radius) + (y - radius) * (y - radius) <= radius * radius;
            if (x >= width - radius && y < radius)
                return (x - (width - radius)) * (x - (width - radius)) + (y - radius) * (y - radius) <= radius * radius;
            if (x < radius && y >= height - radius)
                return (x - radius) * (x - radius) + (y - (height - radius)) * (y - (height - radius)) <= radius * radius;
            if (x >= width - radius && y >= height - radius)
                return (x - (width - radius)) * (x - (width - radius)) + (y - (height - radius)) * (y - (height - radius)) <= radius * radius;

            // Inside rectangle
            return x >= 0 && x < width && y >= 0 && y < height;
        }

        private void DrawFilledEllipse(Texture2D tex, int centerX, int centerY, int radiusX, int radiusY, Color color)
        {
            for (int y = -radiusY; y <= radiusY; y++)
            {
                for (int x = -radiusX; x <= radiusX; x++)
                {
                    float dx = (float)x / radiusX;
                    float dy = (float)y / radiusY;
                    if (dx * dx + dy * dy <= 1)
                    {
                        int px = centerX + x;
                        int py = centerY + y;
                        if (px >= 0 && px < tex.width && py >= 0 && py < tex.height)
                            tex.SetPixel(px, py, color);
                    }
                }
            }
        }

        private void DrawText(Texture2D tex, string text, int centerX, int centerY, Color color, bool large)
        {
            // Simple bitmap font for digits and symbols
            int charWidth = large ? 30 : 15;
            int charHeight = large ? 50 : 25;

            int totalWidth = text.Length * charWidth;
            int startX = centerX - totalWidth / 2;

            for (int i = 0; i < text.Length; i++)
            {
                DrawCharacter(tex, text[i], startX + i * charWidth, centerY - charHeight / 2, charWidth, charHeight, color);
            }
        }

        private void DrawCharacter(Texture2D tex, char c, int x, int y, int width, int height, Color color)
        {
            // Simple pixel patterns for each character
            bool[,] pattern = GetCharacterPattern(c);
            if (pattern == null) return;

            int patternHeight = pattern.GetLength(0);
            int patternWidth = pattern.GetLength(1);

            float scaleX = (float)width / patternWidth;
            float scaleY = (float)height / patternHeight;

            for (int py = 0; py < height; py++)
            {
                for (int px = 0; px < width; px++)
                {
                    int patX = Mathf.Clamp((int)(px / scaleX), 0, patternWidth - 1);
                    int patY = Mathf.Clamp((int)(py / scaleY), 0, patternHeight - 1);

                    if (pattern[patternHeight - 1 - patY, patX])
                    {
                        int texX = x + px;
                        int texY = y + py;
                        if (texX >= 0 && texX < tex.width && texY >= 0 && texY < tex.height)
                            tex.SetPixel(texX, texY, color);
                    }
                }
            }
        }

        private bool[,] GetCharacterPattern(char c)
        {
            // 5x7 pixel font patterns
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
                    {false,false,true,false,false},
                    {false,false,true,false,false},
                    {false,false,true,false,false},
                    {false,false,true,false,false},
                    {false,true,true,true,false}
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
                _ => null
            };
        }

        /// <summary>
        /// Get the generated sprite for a card
        /// </summary>
        public Sprite GetCardSprite(Card card)
        {
            return GetCardSprite(card.Suit, card.Rank);
        }

        /// <summary>
        /// Get the generated sprite for a suit and rank
        /// </summary>
        public Sprite GetCardSprite(Suit suit, Rank rank)
        {
            string key = $"{suit}_{rank}";
            if (generatedSprites.TryGetValue(key, out Sprite sprite))
            {
                return sprite;
            }
            return cardBackSprite;
        }

        /// <summary>
        /// Get the card back sprite
        /// </summary>
        public Sprite GetCardBack()
        {
            return cardBackSprite;
        }
    }
}
