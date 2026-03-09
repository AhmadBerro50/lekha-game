using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Lekha.UI
{
    /// <summary>
    /// Loads emoji PNG images from the OpenMoji CDN at runtime and caches them.
    /// Falls back to a coloured circle with a Unicode label if the download fails.
    ///
    /// OpenMoji is open-source under CC BY-SA 4.0
    /// CDN: https://cdn.jsdelivr.net/npm/openmoji@14.0.0/color/72x72/{CODE}.png
    /// </summary>
    public static class EmojiWebLoader
    {
        // ── Emoji name → Unicode codepoint ────────────────────────────────────
        private static readonly Dictionary<string, string> Codes = new Dictionary<string, string>
        {
            { "laugh",        "1F602" },
            { "angry",        "1F624" },
            { "clap",         "1F44F" },
            { "sad",          "1F622" },
            { "cool",         "1F60E" },
            { "fire",         "1F525" },
            { "heart_broken", "1F494" },
            { "party",        "1F389" },
            { "thumbsup",     "1F44D" },
            { "wow",          "1F62E" },
            { "love",         "2764"  },
            { "cry",          "1F62D" },
            { "skull",        "1F480" },
            { "pray",         "1F64F" },
            { "rocket",       "1F680" },
        };

        // ── Unicode display labels (shown if image fails to load) ─────────────
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
        /// Begin downloading an emoji sprite and apply it to the target Image.
        /// If a cached sprite exists it is applied immediately without a web request.
        /// Falls back to showing the Unicode char via a sibling TMP text component.
        /// </summary>
        public static void LoadInto(string emojiName, Image targetImage)
        {
            if (targetImage == null) return;

            // Immediate cache hit
            if (_cache.TryGetValue(emojiName, out Sprite cached))
            {
                ApplyToImage(targetImage, cached, emojiName);
                return;
            }

            // Try local Resources first (Artists may have placed sprites there)
            Sprite local = Resources.Load<Sprite>($"Emojis/{emojiName}");
            if (local != null)
            {
                _cache[emojiName] = local;
                ApplyToImage(targetImage, local, emojiName);
                return;
            }

            // Download from OpenMoji CDN
            if (!Codes.TryGetValue(emojiName, out string code)) return;
            string url = $"https://cdn.jsdelivr.net/npm/openmoji@14.0.0/color/72x72/{code}.png";

            WebResourceLoader.Instance.LoadSprite(url, emojiName, _cache, sprite =>
            {
                if (targetImage != null)
                    ApplyToImage(targetImage, sprite, emojiName);
            });
        }

        private static void ApplyToImage(Image img, Sprite sprite, string emojiName)
        {
            if (img == null) return;
            if (sprite != null)
            {
                img.sprite          = sprite;
                img.color           = Color.white;
                img.preserveAspect  = true;
                img.gameObject.SetActive(true);
            }
            // If sprite is null the parent button shows the Unicode label fallback (set up by GameUI)
        }

        /// <summary>Try to get an already-cached sprite (no download).</summary>
        public static Sprite GetCached(string emojiName)
        {
            _cache.TryGetValue(emojiName, out Sprite s);
            return s;
        }

        /// <summary>Get the emoji Unicode display character for a given name.</summary>
        public static string GetLabel(string emojiName)
        {
            return Labels.TryGetValue(emojiName, out string s) ? s : "?";
        }
    }
}
