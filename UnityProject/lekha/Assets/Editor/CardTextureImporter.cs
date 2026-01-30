using UnityEngine;
using UnityEditor;
using System.IO;

namespace Lekha.Editor
{
    /// <summary>
    /// Automatically configures card images to be imported as high-quality sprites.
    /// This runs when Unity imports/reimports textures in the Cards folder.
    /// </summary>
    public class CardTextureImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            // Only process textures in the Cards folders
            if (!assetPath.Contains("Cards/") && !assetPath.Contains("Cards\\"))
                return;

            TextureImporter importer = (TextureImporter)assetImporter;

            // Configure as Sprite
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;

            // High quality settings
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false; // Disable mipmaps for UI sprites
            importer.isReadable = true; // Allow runtime sprite creation if needed

            // Sprite settings
            importer.spritePivot = new Vector2(0.5f, 0.5f);
            importer.spritePixelsPerUnit = 100;

            // Platform-specific settings for mobile
            TextureImporterPlatformSettings androidSettings = new TextureImporterPlatformSettings();
            androidSettings.name = "Android";
            androidSettings.overridden = true;
            androidSettings.maxTextureSize = 2048;
            androidSettings.format = TextureImporterFormat.RGBA32;
            androidSettings.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SetPlatformTextureSettings(androidSettings);

            TextureImporterPlatformSettings iosSettings = new TextureImporterPlatformSettings();
            iosSettings.name = "iPhone";
            iosSettings.overridden = true;
            iosSettings.maxTextureSize = 2048;
            iosSettings.format = TextureImporterFormat.RGBA32;
            iosSettings.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SetPlatformTextureSettings(iosSettings);

            Debug.Log($"CardTextureImporter: Configured {assetPath} as HD sprite");
        }
    }

    /// <summary>
    /// Menu item to manually reimport all card textures with proper settings
    /// </summary>
    public static class CardTextureReimporter
    {
        [MenuItem("Lekha/Reimport All Card Textures as HD Sprites")]
        public static void ReimportAllCards()
        {
            string[] folders = new string[]
            {
                "Assets/Resources/Cards",
                "Assets/Sprites/Cards"
            };

            int count = 0;

            foreach (string folder in folders)
            {
                if (!Directory.Exists(folder))
                    continue;

                string[] files = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories);

                foreach (string file in files)
                {
                    if (file.EndsWith(".jpg") || file.EndsWith(".png") || file.EndsWith(".jpeg"))
                    {
                        string assetPath = file.Replace("\\", "/");

                        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                        if (importer != null)
                        {
                            // Configure as Sprite
                            importer.textureType = TextureImporterType.Sprite;
                            importer.spriteImportMode = SpriteImportMode.Single;
                            importer.maxTextureSize = 2048;
                            importer.textureCompression = TextureImporterCompression.Uncompressed;
                            importer.filterMode = FilterMode.Bilinear;
                            importer.mipmapEnabled = false;
                            importer.isReadable = true;
                            importer.spritePivot = new Vector2(0.5f, 0.5f);
                            importer.spritePixelsPerUnit = 100;

                            // Platform settings
                            TextureImporterPlatformSettings androidSettings = new TextureImporterPlatformSettings();
                            androidSettings.name = "Android";
                            androidSettings.overridden = true;
                            androidSettings.maxTextureSize = 2048;
                            androidSettings.format = TextureImporterFormat.RGBA32;
                            androidSettings.textureCompression = TextureImporterCompression.Uncompressed;
                            importer.SetPlatformTextureSettings(androidSettings);

                            TextureImporterPlatformSettings iosSettings = new TextureImporterPlatformSettings();
                            iosSettings.name = "iPhone";
                            iosSettings.overridden = true;
                            iosSettings.maxTextureSize = 2048;
                            iosSettings.format = TextureImporterFormat.RGBA32;
                            iosSettings.textureCompression = TextureImporterCompression.Uncompressed;
                            importer.SetPlatformTextureSettings(iosSettings);

                            importer.SaveAndReimport();
                            count++;
                        }
                    }
                }
            }

            AssetDatabase.Refresh();
            Debug.Log($"CardTextureReimporter: Reimported {count} card textures as HD sprites");
            EditorUtility.DisplayDialog("Card Textures Reimported",
                $"Successfully reimported {count} card textures as HD sprites.\n\nAll cards are now configured for maximum quality.",
                "OK");
        }
    }
}
