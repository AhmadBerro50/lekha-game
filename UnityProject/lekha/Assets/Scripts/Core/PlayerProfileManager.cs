using UnityEngine;
using System;
using System.IO;

namespace Lekha.Core
{
    /// <summary>
    /// Manages player profiles - saving, loading, and current session
    /// </summary>
    public class PlayerProfileManager : MonoBehaviour
    {
        public static PlayerProfileManager Instance { get; private set; }

        private const string PROFILE_KEY = "LocalPlayerProfile";
        private const string PROFILE_FILE = "player_profile.json";

        private PlayerProfile currentProfile;
        public PlayerProfile CurrentProfile => currentProfile;

        public event Action<PlayerProfile> OnProfileChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadProfile();
        }

        /// <summary>
        /// Load the player's profile from storage
        /// </summary>
        public void LoadProfile()
        {
            currentProfile = null;

            // Try to load from file first (more reliable than PlayerPrefs for large data)
            string filePath = GetProfileFilePath();
            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    currentProfile = JsonUtility.FromJson<PlayerProfile>(json);
                    Debug.Log($"Loaded profile: {currentProfile.DisplayName}");
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to load profile from file: {e.Message}");
                }
            }

            // Fallback to PlayerPrefs
            if (currentProfile == null && PlayerPrefs.HasKey(PROFILE_KEY))
            {
                try
                {
                    string json = PlayerPrefs.GetString(PROFILE_KEY);
                    currentProfile = JsonUtility.FromJson<PlayerProfile>(json);
                    Debug.Log($"Loaded profile from PlayerPrefs: {currentProfile.DisplayName}");

                    // Migrate to file storage
                    SaveProfile();
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to load profile from PlayerPrefs: {e.Message}");
                }
            }

            // Create default profile if none exists
            if (currentProfile == null)
            {
                currentProfile = new PlayerProfile("Player");
                Debug.Log("Created new default profile");
                SaveProfile();
            }

            OnProfileChanged?.Invoke(currentProfile);
        }

        /// <summary>
        /// Save the current profile to storage
        /// </summary>
        public void SaveProfile()
        {
            if (currentProfile == null)
                return;

            try
            {
                string json = JsonUtility.ToJson(currentProfile, true);

                // Save to file
                string filePath = GetProfileFilePath();
                string directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllText(filePath, json);

                // Also save to PlayerPrefs as backup
                PlayerPrefs.SetString(PROFILE_KEY, json);
                PlayerPrefs.Save();

                Debug.Log($"Profile saved: {currentProfile.DisplayName}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save profile: {e.Message}");
            }
        }

        /// <summary>
        /// Update the player's display name
        /// </summary>
        public void SetDisplayName(string name)
        {
            if (currentProfile == null)
                return;

            name = name?.Trim();
            if (string.IsNullOrEmpty(name))
                name = "Player";

            // Limit name length
            if (name.Length > 20)
                name = name.Substring(0, 20);

            currentProfile.DisplayName = name;
            SaveProfile();
            OnProfileChanged?.Invoke(currentProfile);
        }

        /// <summary>
        /// Set the player's avatar from a texture
        /// </summary>
        public void SetAvatar(Texture2D texture)
        {
            if (currentProfile == null)
                return;

            currentProfile.SetAvatar(texture);
            SaveProfile();
            OnProfileChanged?.Invoke(currentProfile);
        }

        /// <summary>
        /// Clear the player's custom avatar
        /// </summary>
        public void ClearAvatar()
        {
            if (currentProfile == null)
                return;

            currentProfile.ClearAvatar();
            SaveProfile();
            OnProfileChanged?.Invoke(currentProfile);
        }

        /// <summary>
        /// Record a completed game
        /// </summary>
        public void RecordGameResult(bool won, int pointsScored)
        {
            if (currentProfile == null)
                return;

            currentProfile.RecordGame(won, pointsScored);
            SaveProfile();
        }

        /// <summary>
        /// Reset all statistics (keeps name and avatar)
        /// </summary>
        public void ResetStatistics()
        {
            if (currentProfile == null)
                return;

            currentProfile.GamesPlayed = 0;
            currentProfile.GamesWon = 0;
            currentProfile.TotalPointsScored = 0;
            SaveProfile();
            OnProfileChanged?.Invoke(currentProfile);
        }

        /// <summary>
        /// Delete the profile and create a fresh one
        /// </summary>
        public void DeleteProfile()
        {
            if (currentProfile != null)
            {
                currentProfile.ClearAvatar();
                currentProfile.ClearCache();
            }

            // Delete saved file
            string filePath = GetProfileFilePath();
            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to delete profile file: {e.Message}");
                }
            }

            // Clear PlayerPrefs
            PlayerPrefs.DeleteKey(PROFILE_KEY);
            PlayerPrefs.Save();

            // Create fresh profile
            currentProfile = new PlayerProfile("Player");
            SaveProfile();
            OnProfileChanged?.Invoke(currentProfile);
        }

        /// <summary>
        /// Check if profile setup is needed (first time user)
        /// </summary>
        public bool NeedsProfileSetup()
        {
            return currentProfile != null &&
                   currentProfile.DisplayName == "Player" &&
                   currentProfile.GamesPlayed == 0;
        }

        private string GetProfileFilePath()
        {
            return Path.Combine(Application.persistentDataPath, "Profiles", PROFILE_FILE);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SaveProfile();
            }
        }

        private void OnApplicationQuit()
        {
            SaveProfile();
            currentProfile?.ClearCache();
        }
    }
}
