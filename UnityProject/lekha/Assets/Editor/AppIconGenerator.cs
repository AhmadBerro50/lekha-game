using UnityEngine;
using UnityEditor;

/// <summary>
/// Sets the app icon for iOS and Android from Assets/AppIcon_1024.png.
/// Menu: Tools > Set App Icon
/// </summary>
public class AppIconGenerator : EditorWindow
{
    [MenuItem("Tools/Generate App Icon")]
    public static void Generate()
    {
        // Load the icon texture
        Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/AppIcon_1024.png");
        if (icon == null)
        {
            EditorUtility.DisplayDialog("Error", "AppIcon_1024.png not found in Assets/", "OK");
            return;
        }

        // Ensure texture is readable and set as Sprite
        string path = AssetDatabase.GetAssetPath(icon);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            bool changed = false;
            if (!importer.isReadable) { importer.isReadable = true; changed = true; }
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }
            if (importer.maxTextureSize < 2048)
            {
                importer.maxTextureSize = 2048;
                changed = true;
            }
            // Disable compression for icon quality
            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                changed = true;
            }
            if (changed)
            {
                importer.SaveAndReimport();
                // Reload after reimport
                icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/AppIcon_1024.png");
            }
        }

        int set = 0;

        // ── Set Default Icon ──────────────────────────────────────────────
        PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new Texture2D[] { icon });
        set++;

        // ── Set Android Icons ─────────────────────────────────────────────
        var androidPlatform = BuildTargetGroup.Android;
        var androidKinds = PlayerSettings.GetSupportedIconKindsForPlatform(androidPlatform);
        foreach (var kind in androidKinds)
        {
            var sizes = PlayerSettings.GetIconSizesForPlatform(androidPlatform, kind);
            Texture2D[] icons = new Texture2D[sizes.Length];
            for (int i = 0; i < icons.Length; i++)
                icons[i] = icon;
            PlayerSettings.SetPlatformIcons(androidPlatform, kind, icons);
            set += icons.Length;
        }

        // ── Set iOS Icons ─────────────────────────────────────────────────
        var iosPlatform = BuildTargetGroup.iOS;
        var iosKinds = PlayerSettings.GetSupportedIconKindsForPlatform(iosPlatform);
        foreach (var kind in iosKinds)
        {
            var sizes = PlayerSettings.GetIconSizesForPlatform(iosPlatform, kind);
            Texture2D[] icons = new Texture2D[sizes.Length];
            for (int i = 0; i < icons.Length; i++)
                icons[i] = icon;
            PlayerSettings.SetPlatformIcons(iosPlatform, kind, icons);
            set += icons.Length;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[AppIcon] Set {set} icon slots for Android + iOS from AppIcon_1024.png");
        EditorUtility.DisplayDialog("App Icon", $"Icon set for Android + iOS ({set} slots)\nfrom AppIcon_1024.png", "OK");
    }
}
