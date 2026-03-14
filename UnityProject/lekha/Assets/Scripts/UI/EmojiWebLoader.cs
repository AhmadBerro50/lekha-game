using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Lekha.UI
{
    /// <summary>
    /// Loads emoji sprites from Resources/Emojis/ (Microsoft Fluent 3D PNGs).
    /// All assets are bundled locally — no network downloads required.
    /// </summary>
    public static class EmojiWebLoader
    {
        // Unicode display labels (fallback while sprite loads or if missing)
        public static readonly Dictionary<string, string> Labels = new Dictionary<string, string>
        {
            { "laugh",        "\U0001F602" },
            { "angry",        "\U0001F624" },
            { "clap",         "\U0001F44F" },
            { "sad",          "\U0001F622" },
            { "cool",         "\U0001F60E" },
            { "fire",         "\U0001F525" },
            { "heart_broken", "\U0001F494" },
            { "party",        "\U0001F389" },
            { "thumbsup",     "\U0001F44D" },
            { "wow",          "\U0001F62E" },
            { "love",         "\u2764"     },
            { "cry",          "\U0001F62D" },
            { "skull",        "\U0001F480" },
            { "pray",         "\U0001F64F" },
            { "rocket",       "\U0001F680" },
        };

        private static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();

        /// <summary>
        /// Load an emoji sprite from local Resources and apply it to the target Image.
        /// </summary>
        public static void LoadInto(string emojiName, Image targetImage)
        {
            if (targetImage == null) return;

            Sprite sprite = GetSprite(emojiName);
            if (sprite != null)
            {
                targetImage.sprite = sprite;
                targetImage.color = Color.white;
                targetImage.preserveAspect = true;
                targetImage.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Get an emoji sprite by name (cached after first load).
        /// Handles both Sprite and Texture2D import modes.
        /// </summary>
        public static Sprite GetSprite(string emojiName)
        {
            if (_cache.TryGetValue(emojiName, out Sprite cached))
                return cached;

            string path = $"Emojis/{emojiName}";
            Sprite sprite = Resources.Load<Sprite>(path);
            if (sprite == null)
            {
                Texture2D tex = Resources.Load<Texture2D>(path);
                if (tex != null)
                    sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                        new Vector2(0.5f, 0.5f), 100f);
            }

            if (sprite != null)
                _cache[emojiName] = sprite;

            return sprite;
        }

        /// <summary>Get the emoji Unicode display character for a given name.</summary>
        public static string GetLabel(string emojiName)
        {
            return Labels.TryGetValue(emojiName, out string s) ? s : "?";
        }
    }
}
