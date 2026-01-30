using System.Collections.Generic;
using UnityEngine;
using Lekha.Core;

namespace Lekha.UI
{
    /// <summary>
    /// Manages loading and retrieving card sprites.
    /// Automatically loads sprites from Resources/Cards folder.
    /// </summary>
    public class CardSpriteManager : MonoBehaviour
    {
        public static CardSpriteManager Instance { get; private set; }

        private Dictionary<string, Sprite> cardSprites = new Dictionary<string, Sprite>();
        private Sprite cardBackSprite;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadAllSprites();
        }

        private void LoadAllSprites()
        {
            Debug.Log("CardSpriteManager: Starting to load sprites...");

            // Load sprites from each color folder
            string[] colorFolders = { "Red", "Yellow", "Blue", "Green" };

            foreach (string color in colorFolders)
            {
                LoadSpritesFromFolder($"Cards/{color}");
            }

            // Load card back from root Cards folder
            LoadSpritesFromFolder("Cards");

            // Try to find card back with various possible names
            string[] cardBackNames = { "CardBack", "CardBack_0", "Card_Back", "card_back", "back" };
            foreach (string backName in cardBackNames)
            {
                if (cardSprites.TryGetValue(backName, out Sprite back))
                {
                    cardBackSprite = back;
                    Debug.Log($"CardSpriteManager: Found card back as '{backName}'");
                    break;
                }
            }

            // If still no card back, create a default one
            if (cardBackSprite == null)
            {
                Debug.Log("CardSpriteManager: No card back found, creating default");
                cardBackSprite = CreateDefaultCardBack();
            }

            Debug.Log($"CardSpriteManager: Loaded {cardSprites.Count} sprites total");

            // Log all loaded sprite names for debugging
            foreach (var kvp in cardSprites)
            {
                Debug.Log($"  - Sprite loaded: '{kvp.Key}'");
            }
        }

        private void LoadSpritesFromFolder(string folder)
        {
            // Load as Sprites (this works when textures are imported as Sprite type)
            Sprite[] sprites = Resources.LoadAll<Sprite>(folder);
            Debug.Log($"CardSpriteManager: Found {sprites.Length} sprites in '{folder}'");

            foreach (var sprite in sprites)
            {
                // Store with original name
                cardSprites[sprite.name] = sprite;

                // Also store without _0 suffix if it has one (Unity sometimes adds this)
                if (sprite.name.EndsWith("_0"))
                {
                    string baseName = sprite.name.Substring(0, sprite.name.Length - 2);
                    if (!cardSprites.ContainsKey(baseName))
                    {
                        cardSprites[baseName] = sprite;
                    }
                }
            }

            // If no sprites found, try loading as Texture2D
            if (sprites.Length == 0)
            {
                Texture2D[] textures = Resources.LoadAll<Texture2D>(folder);
                Debug.Log($"CardSpriteManager: Found {textures.Length} textures in '{folder}' (fallback)");

                foreach (var tex in textures)
                {
                    if (!cardSprites.ContainsKey(tex.name) && tex.isReadable)
                    {
                        Sprite sprite = Sprite.Create(
                            tex,
                            new Rect(0, 0, tex.width, tex.height),
                            new Vector2(0.5f, 0.5f),
                            100f
                        );
                        cardSprites[tex.name] = sprite;
                        Debug.Log($"CardSpriteManager: Created sprite from texture '{tex.name}'");
                    }
                    else if (!tex.isReadable)
                    {
                        Debug.LogWarning($"CardSpriteManager: Texture '{tex.name}' is not readable, cannot create sprite");
                    }
                }
            }
        }

        private Sprite CreateDefaultCardBack()
        {
            int width = 256;
            int height = 384;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

            // Create a nice card back pattern
            Color darkBlue = new Color(0.1f, 0.15f, 0.3f);
            Color lightBlue = new Color(0.2f, 0.3f, 0.5f);
            Color gold = new Color(0.8f, 0.65f, 0.2f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Border
                    if (x < 8 || x >= width - 8 || y < 8 || y >= height - 8)
                    {
                        tex.SetPixel(x, y, gold);
                    }
                    // Diamond pattern
                    else
                    {
                        int patternX = (x - 8) % 32;
                        int patternY = (y - 8) % 32;
                        bool isDiamond = Mathf.Abs(patternX - 16) + Mathf.Abs(patternY - 16) < 12;
                        tex.SetPixel(x, y, isDiamond ? lightBlue : darkBlue);
                    }
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>
        /// Get the sprite for a specific card
        /// </summary>
        public Sprite GetCardSprite(Card card)
        {
            return GetCardSprite(card.Suit, card.Rank);
        }

        /// <summary>
        /// Get the sprite for a specific suit and rank
        /// </summary>
        public Sprite GetCardSprite(Suit suit, Rank rank)
        {
            string colorName = suit switch
            {
                Suit.Hearts => "Red",
                Suit.Diamonds => "Yellow",
                Suit.Spades => "Blue",
                Suit.Clubs => "Green",
                _ => "Red"
            };

            string spriteName = rank switch
            {
                Rank.Ace => $"{colorName}_1",
                Rank.Two => $"{colorName}_2",
                Rank.Three => $"{colorName}_3",
                Rank.Four => $"{colorName}_4",
                Rank.Five => $"{colorName}_5",
                Rank.Six => $"{colorName}_6",
                Rank.Seven => $"{colorName}_7",
                Rank.Eight => $"{colorName}_8",
                Rank.Nine => $"{colorName}_9",
                Rank.Ten => $"{colorName}_0",
                Rank.Jack => $"{colorName}_Reverse",
                Rank.Queen => $"{colorName}_Draw_2",
                Rank.King => $"{colorName}_Skip",
                _ => $"{colorName}_1"
            };

            // Try exact match first
            if (cardSprites.TryGetValue(spriteName, out Sprite sprite))
            {
                return sprite;
            }

            // Try with _0 suffix (Unity sometimes adds this to sprite names)
            if (cardSprites.TryGetValue(spriteName + "_0", out sprite))
            {
                return sprite;
            }

            // Try lowercase
            if (cardSprites.TryGetValue(spriteName.ToLower(), out sprite))
            {
                return sprite;
            }

            // Try uppercase color
            string upperColorName = colorName.ToUpper();
            string upperSpriteName = spriteName.Replace(colorName, upperColorName);
            if (cardSprites.TryGetValue(upperSpriteName, out sprite))
            {
                return sprite;
            }

            Debug.LogWarning($"CardSpriteManager: Sprite not found: '{spriteName}' for {suit} {rank}. Available sprites: {cardSprites.Count}");
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
