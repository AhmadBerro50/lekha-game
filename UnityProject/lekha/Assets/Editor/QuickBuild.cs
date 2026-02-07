using UnityEditor;
using UnityEngine;

/// <summary>
/// Quick build helper for testing multiplayer locally.
/// Build a macOS app, then run multiple copies to simulate 4 players.
/// </summary>
public class QuickBuild
{
    [MenuItem("Lekha/Build macOS (for multiplayer testing)")]
    public static void BuildMacOS()
    {
        string path = EditorUtility.SaveFolderPanel("Choose Build Location", "", "LekhaBuild");
        if (string.IsNullOrEmpty(path)) return;

        string appPath = path + "/Lekha.app";

        // Set portrait resolution for phone-like testing (these stay permanently)
        PlayerSettings.defaultScreenWidth = 540;
        PlayerSettings.defaultScreenHeight = 960;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = true;

        // Ensure microphone usage description is set (required even if voice chat is disabled)
        if (string.IsNullOrEmpty(PlayerSettings.iOS.microphoneUsageDescription))
            PlayerSettings.iOS.microphoneUsageDescription = "Voice chat with other players";

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/SampleScene.unity" },
            locationPathName = appPath,
            target = BuildTarget.StandaloneOSX,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(options);

        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"[QuickBuild] macOS build succeeded! Location: {appPath}");
            Debug.Log("[QuickBuild] Run multiple copies of Lekha.app to test multiplayer.");
            Debug.Log("[QuickBuild] Window: 540x960 portrait, resizable, windowed.");
            EditorUtility.RevealInFinder(appPath);
        }
        else
        {
            Debug.LogError($"[QuickBuild] Build failed: {report.summary.totalErrors} errors");
        }
    }

    /// <summary>
    /// Apply portrait windowed settings without building.
    /// Run this before using Unity's standard Build & Run.
    /// </summary>
    [MenuItem("Lekha/Apply Portrait Window Settings")]
    public static void ApplyPortraitSettings()
    {
        PlayerSettings.defaultScreenWidth = 540;
        PlayerSettings.defaultScreenHeight = 960;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = true;

        if (string.IsNullOrEmpty(PlayerSettings.iOS.microphoneUsageDescription))
            PlayerSettings.iOS.microphoneUsageDescription = "Voice chat with other players";

        Debug.Log("[QuickBuild] Applied: 540x960, windowed, resizable. You can now use Build & Run.");
    }

    /// <summary>
    /// One-time fix: set microphone usage descriptions for all platforms.
    /// Run this once if builds fail due to missing microphone description.
    /// </summary>
    [MenuItem("Lekha/Fix Microphone Permissions")]
    public static void FixMicrophonePermissions()
    {
        PlayerSettings.iOS.microphoneUsageDescription = "Voice chat with other players";
        Debug.Log("[QuickBuild] Microphone usage description set for iOS/macOS. Try building again.");
    }
}
