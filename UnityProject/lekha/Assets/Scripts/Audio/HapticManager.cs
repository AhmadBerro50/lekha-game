using UnityEngine;

namespace Lekha.Audio
{
    /// <summary>
    /// Manages haptic/vibration feedback on mobile devices.
    /// Provides light, medium, and heavy vibration patterns.
    /// </summary>
    public class HapticManager : MonoBehaviour
    {
        public static HapticManager Instance { get; private set; }

        private bool hapticsEnabled = true;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Load preference
            hapticsEnabled = PlayerPrefs.GetInt("HapticsEnabled", 1) == 1;
        }

        public void SetEnabled(bool enabled)
        {
            hapticsEnabled = enabled;
            PlayerPrefs.SetInt("HapticsEnabled", enabled ? 1 : 0);
        }

        public bool IsEnabled => hapticsEnabled;

        /// <summary>
        /// Light tap - card hover, card select
        /// </summary>
        public void LightTap()
        {
            if (!hapticsEnabled) return;
            Vibrate(10);
        }

        /// <summary>
        /// Medium tap - card play, button click
        /// </summary>
        public void MediumTap()
        {
            if (!hapticsEnabled) return;
            Vibrate(25);
        }

        /// <summary>
        /// Heavy tap - trick win, special card, emoji received
        /// </summary>
        public void HeavyTap()
        {
            if (!hapticsEnabled) return;
            Vibrate(50);
        }

        /// <summary>
        /// Success feedback - game win, round end
        /// </summary>
        public void SuccessTap()
        {
            if (!hapticsEnabled) return;
            Vibrate(40);
        }

        /// <summary>
        /// Error/warning feedback - game lose
        /// </summary>
        public void ErrorTap()
        {
            if (!hapticsEnabled) return;
            Vibrate(60);
        }

        private void Vibrate(long milliseconds)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
                {
                    if (vibrator != null)
                    {
                        vibrator.Call("vibrate", milliseconds);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[HapticManager] Android vibration failed: {e.Message}");
            }
#elif UNITY_IOS && !UNITY_EDITOR
            // iOS uses Handheld.Vibrate() for basic vibration
            // For more nuanced haptics, we'd need a native plugin
            Handheld.Vibrate();
#endif
        }
    }
}
