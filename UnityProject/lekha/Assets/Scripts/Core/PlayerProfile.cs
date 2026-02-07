using UnityEngine;
using System;

namespace Lekha.Core
{
    /// <summary>
    /// Represents a player's profile with name, avatar, and statistics
    /// </summary>
    [Serializable]
    public class PlayerProfile
    {
        public string PlayerId;
        public string DisplayName;
        public string AvatarPath; // Path to saved avatar image, empty if using placeholder
        public int GamesPlayed;
        public int GamesWon;
        public int TotalPointsScored;
        public long CreatedAt;
        public long LastPlayedAt;

        // Transient - not serialized
        [NonSerialized]
        private Texture2D cachedAvatar;
        [NonSerialized]
        private Sprite cachedAvatarSprite;

        public PlayerProfile()
        {
            PlayerId = Guid.NewGuid().ToString();
            DisplayName = "Player";
            AvatarPath = "";
            GamesPlayed = 0;
            GamesWon = 0;
            TotalPointsScored = 0;
            CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            LastPlayedAt = CreatedAt;
        }

        public PlayerProfile(string name) : this()
        {
            DisplayName = name;
        }

        /// <summary>
        /// Get win rate as percentage (0-100)
        /// </summary>
        public float WinRate => GamesPlayed > 0 ? (float)GamesWon / GamesPlayed * 100f : 0f;

        /// <summary>
        /// Get average points per game
        /// </summary>
        public float AveragePoints => GamesPlayed > 0 ? (float)TotalPointsScored / GamesPlayed : 0f;

        /// <summary>
        /// Check if this profile has a custom avatar
        /// </summary>
        public bool HasCustomAvatar => !string.IsNullOrEmpty(AvatarPath);

        /// <summary>
        /// Get the first letter of display name for placeholder avatar
        /// </summary>
        public string Initial => string.IsNullOrEmpty(DisplayName) ? "?" : DisplayName[0].ToString().ToUpper();

        /// <summary>
        /// Load and cache the avatar texture
        /// </summary>
        public Texture2D GetAvatarTexture()
        {
            if (cachedAvatar != null)
                return cachedAvatar;

            if (!HasCustomAvatar)
                return null;

            try
            {
                if (System.IO.File.Exists(AvatarPath))
                {
                    byte[] imageData = System.IO.File.ReadAllBytes(AvatarPath);

                    // Validate we have actual image data
                    if (imageData == null || imageData.Length < 100)
                    {
                        Debug.LogWarning($"Avatar file is too small or empty: {AvatarPath}");
                        ClearAvatar();
                        return null;
                    }

                    cachedAvatar = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    bool loadSuccess = cachedAvatar.LoadImage(imageData);

                    // Validate the texture loaded correctly (should be larger than 2x2)
                    if (!loadSuccess || cachedAvatar.width <= 2 || cachedAvatar.height <= 2)
                    {
                        Debug.LogWarning($"Failed to decode avatar image: {AvatarPath}");
                        UnityEngine.Object.Destroy(cachedAvatar);
                        cachedAvatar = null;
                        ClearAvatar();
                        return null;
                    }

                    return cachedAvatar;
                }
                else
                {
                    // File doesn't exist - clear the path
                    Debug.LogWarning($"Avatar file not found: {AvatarPath}");
                    AvatarPath = "";
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to load avatar from {AvatarPath}: {e.Message}");
                AvatarPath = "";
            }

            return null;
        }

        /// <summary>
        /// Get avatar as sprite (creates from texture)
        /// </summary>
        public Sprite GetAvatarSprite()
        {
            if (cachedAvatarSprite != null)
                return cachedAvatarSprite;

            Texture2D tex = GetAvatarTexture();
            if (tex != null)
            {
                cachedAvatarSprite = Sprite.Create(
                    tex,
                    new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f)
                );
                return cachedAvatarSprite;
            }

            return null;
        }

        /// <summary>
        /// Set avatar from texture and save to disk
        /// </summary>
        public void SetAvatar(Texture2D texture)
        {
            if (texture == null)
            {
                ClearAvatar();
                return;
            }

            try
            {
                string avatarsDir = System.IO.Path.Combine(Application.persistentDataPath, "Avatars");
                if (!System.IO.Directory.Exists(avatarsDir))
                    System.IO.Directory.CreateDirectory(avatarsDir);

                string fileName = $"avatar_{PlayerId}.png";
                string filePath = System.IO.Path.Combine(avatarsDir, fileName);

                byte[] pngData = texture.EncodeToPNG();
                System.IO.File.WriteAllBytes(filePath, pngData);

                AvatarPath = filePath;
                cachedAvatar = texture;
                cachedAvatarSprite = null; // Will be recreated on next request

                Debug.Log($"Avatar saved to {filePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save avatar: {e.Message}");
            }
        }

        /// <summary>
        /// Clear the custom avatar
        /// </summary>
        public void ClearAvatar()
        {
            if (HasCustomAvatar && System.IO.File.Exists(AvatarPath))
            {
                try
                {
                    System.IO.File.Delete(AvatarPath);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to delete avatar file: {e.Message}");
                }
            }

            AvatarPath = "";
            cachedAvatar = null;
            cachedAvatarSprite = null;
        }

        /// <summary>
        /// Record a game result
        /// </summary>
        public void RecordGame(bool won, int pointsScored)
        {
            GamesPlayed++;
            if (won) GamesWon++;
            TotalPointsScored += pointsScored;
            LastPlayedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        /// <summary>
        /// Clear cached textures to free memory
        /// </summary>
        public void ClearCache()
        {
            if (cachedAvatar != null)
            {
                UnityEngine.Object.Destroy(cachedAvatar);
                cachedAvatar = null;
            }
            if (cachedAvatarSprite != null)
            {
                UnityEngine.Object.Destroy(cachedAvatarSprite);
                cachedAvatarSprite = null;
            }
        }
    }
}
