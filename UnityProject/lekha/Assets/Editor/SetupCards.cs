#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class SetupCards : MonoBehaviour
{
    [MenuItem("Lekha/Setup Card Sprites")]
    public static void SetupCardSprites()
    {
        string sourcePath = "Assets/Sprites/Cards";
        string destPath = "Assets/Resources/Cards";

        // Create Resources/Cards folder if it doesn't exist
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Cards"))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "Cards");
        }

        // Create color subfolders
        string[] colors = { "Red", "Yellow", "Blue", "Green" };
        foreach (string color in colors)
        {
            string colorFolder = $"Assets/Resources/Cards/{color}";
            if (!AssetDatabase.IsValidFolder(colorFolder))
            {
                AssetDatabase.CreateFolder("Assets/Resources/Cards", color);
            }
        }

        // Copy all card images
        CopyCardsFromFolder(sourcePath, destPath);

        // Set all images to Sprite type with high quality
        SetTextureImportSettings(destPath);
        SetTextureImportSettings(sourcePath);

        AssetDatabase.Refresh();
        Debug.Log("Card sprites setup complete!");
    }

    [MenuItem("Lekha/Fix Card Quality")]
    public static void FixCardQuality()
    {
        SetTextureImportSettings("Assets/Sprites/Cards");
        SetTextureImportSettings("Assets/Resources/Cards");
        AssetDatabase.Refresh();
        Debug.Log("Card quality fixed!");
    }

    private static void CopyCardsFromFolder(string source, string dest)
    {
        // Copy CardBack
        string cardBackSource = $"{source}/CardBack.jpg";
        string cardBackDest = $"{dest}/CardBack.jpg";
        if (File.Exists(cardBackSource) && !File.Exists(cardBackDest))
        {
            AssetDatabase.CopyAsset(cardBackSource, cardBackDest);
        }

        // Copy color folders
        string[] colors = { "Red", "Yellow", "Blue", "Green" };
        foreach (string color in colors)
        {
            string colorSource = $"{source}/{color}";
            string colorDest = $"{dest}/{color}";

            if (Directory.Exists(colorSource))
            {
                string[] files = Directory.GetFiles(colorSource, "*.jpg");
                foreach (string file in files)
                {
                    string fileName = Path.GetFileName(file);
                    string destFile = $"{colorDest}/{fileName}";
                    string sourceAsset = $"{colorSource}/{fileName}";

                    if (!File.Exists(destFile))
                    {
                        AssetDatabase.CopyAsset(sourceAsset, destFile);
                    }
                }
            }
        }
    }

    private static void SetTextureImportSettings(string folderPath)
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer != null)
            {
                // Basic settings
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 1024;
                importer.spritePixelsPerUnit = 100;

                // Default platform settings
                TextureImporterPlatformSettings defaultSettings = importer.GetDefaultPlatformTextureSettings();
                defaultSettings.maxTextureSize = 1024;
                defaultSettings.textureCompression = TextureImporterCompression.Uncompressed;
                defaultSettings.format = TextureImporterFormat.Automatic;
                importer.SetPlatformTextureSettings(defaultSettings);

                // iOS settings
                TextureImporterPlatformSettings iosSettings = new TextureImporterPlatformSettings();
                iosSettings.name = "iPhone";
                iosSettings.overridden = true;
                iosSettings.maxTextureSize = 1024;
                iosSettings.textureCompression = TextureImporterCompression.Uncompressed;
                iosSettings.format = TextureImporterFormat.Automatic;
                importer.SetPlatformTextureSettings(iosSettings);

                // Android settings
                TextureImporterPlatformSettings androidSettings = new TextureImporterPlatformSettings();
                androidSettings.name = "Android";
                androidSettings.overridden = true;
                androidSettings.maxTextureSize = 1024;
                androidSettings.textureCompression = TextureImporterCompression.Uncompressed;
                androidSettings.format = TextureImporterFormat.Automatic;
                importer.SetPlatformTextureSettings(androidSettings);

                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
            }
        }

        Debug.Log($"Fixed {guids.Length} textures in {folderPath}");
    }
}
#endif
