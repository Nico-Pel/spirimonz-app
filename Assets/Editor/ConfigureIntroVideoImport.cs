using UnityEditor;
using UnityEngine;

public static class ConfigureIntroVideoImport
{
    private const string IntroVideoPath = "Assets/Resources/Introduction-mobile.mp4";
    private const string AndroidPlatformName = "Android";

    [MenuItem("Tools/Video/Configure Intro Video Import For Android")]
    public static void ApplyAndroidSettings()
    {
        try
        {
            VideoClipImporter importer = AssetImporter.GetAtPath(IntroVideoPath) as VideoClipImporter;
            if (importer == null)
            {
                Debug.LogError($"Intro video importer not found at {IntroVideoPath}");
                return;
            }

            VideoImporterTargetSettings settings = importer.GetTargetSettings(AndroidPlatformName);
            if (settings == null)
                settings = importer.defaultTargetSettings ?? new VideoImporterTargetSettings();

            settings.enableTranscoding = true;
            settings.codec = VideoCodec.H264;
            settings.resizeMode = VideoResizeMode.HalfRes;
            settings.aspectRatio = VideoEncodeAspectRatio.NoScaling;
            settings.bitrateMode = VideoBitrateMode.Medium;
            settings.spatialQuality = VideoSpatialQuality.HighSpatialQuality;

            importer.importAudio = true;
            importer.keepAlpha = false;
            importer.deinterlaceMode = VideoDeinterlaceMode.Off;
            importer.SetTargetSettings(AndroidPlatformName, settings);
            importer.SaveAndReimport();

            VideoClipImporter refreshedImporter = AssetImporter.GetAtPath(IntroVideoPath) as VideoClipImporter;
            VideoImporterTargetSettings refreshedSettings = refreshedImporter != null
                ? refreshedImporter.GetTargetSettings(AndroidPlatformName)
                : settings;

            Debug.Log(
                $"Configured intro video import for Android. " +
                $"Transcoding={refreshedSettings != null && refreshedSettings.enableTranscoding}, " +
                $"Codec={(refreshedSettings != null ? refreshedSettings.codec.ToString() : "Unknown")}, " +
                $"Resize={(refreshedSettings != null ? refreshedSettings.resizeMode.ToString() : "Unknown")}, " +
                $"Bitrate={(refreshedSettings != null ? refreshedSettings.bitrateMode.ToString() : "Unknown")}, " +
                $"SpatialQuality={(refreshedSettings != null ? refreshedSettings.spatialQuality.ToString() : "Unknown")}");
        }
        catch (System.Exception exception)
        {
            Debug.LogError($"ConfigureIntroVideoImport failed: {exception}");
        }
    }
}
