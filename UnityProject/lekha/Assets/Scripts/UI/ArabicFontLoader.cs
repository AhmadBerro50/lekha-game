using UnityEngine;
using TMPro;

namespace Lekha.UI
{
    /// <summary>
    /// Loads Noto Sans Arabic at runtime and adds it as a TMP fallback font
    /// so Arabic text renders correctly in chat and other TMP components.
    /// Attach to a persistent GameObject or let it auto-create.
    /// </summary>
    public class ArabicFontLoader : MonoBehaviour
    {
        private static bool _loaded = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoLoad()
        {
            if (_loaded) return;
            _loaded = true;

            Font arabicFont = Resources.Load<Font>("Fonts/NotoSansArabic-Regular");
            if (arabicFont == null)
            {
                Debug.LogWarning("[ArabicFontLoader] NotoSansArabic-Regular.ttf not found in Resources/Fonts/");
                return;
            }

            // Create a dynamic TMP font asset from the TTF
            TMP_FontAsset arabicTmpFont = TMP_FontAsset.CreateFontAsset(arabicFont);
            if (arabicTmpFont == null)
            {
                Debug.LogWarning("[ArabicFontLoader] Failed to create TMP_FontAsset from Arabic font");
                return;
            }

            arabicTmpFont.name = "NotoSansArabic-Dynamic";

            // Add as fallback to the default TMP font
            TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;
            if (defaultFont != null)
            {
                if (defaultFont.fallbackFontAssetTable == null)
                    defaultFont.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();

                // Check if already added
                foreach (var f in defaultFont.fallbackFontAssetTable)
                {
                    if (f != null && f.name == "NotoSansArabic-Dynamic")
                        return;
                }

                defaultFont.fallbackFontAssetTable.Add(arabicTmpFont);
                Debug.Log("[ArabicFontLoader] Arabic font added as TMP fallback successfully");
            }
            else
            {
                Debug.LogWarning("[ArabicFontLoader] No default TMP font found to add fallback to");
            }
        }
    }
}
