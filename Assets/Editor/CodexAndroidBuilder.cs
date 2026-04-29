#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public static class CodexAndroidBuilder
{
    public static void BuildDebugApk()
    {
        const string outputDirectory = "Builds/Android";
        const string outputFileName = "Spirimonz-debug.apk";

        Directory.CreateDirectory(outputDirectory);

        EditorBuildSettingsScene[] enabledScenes = EditorBuildSettings.scenes
            .Where(scene => scene != null && scene.enabled && !string.IsNullOrWhiteSpace(scene.path))
            .ToArray();

        if (enabledScenes.Length == 0)
            throw new BuildFailedException("No enabled scenes found in EditorBuildSettings.");

        string[] scenePaths = enabledScenes.Select(scene => scene.path).ToArray();
        string outputPath = Path.Combine(outputDirectory, outputFileName);

        EditorUserBuildSettings.development = true;
        EditorUserBuildSettings.connectProfiler = false;
        EditorUserBuildSettings.buildAppBundle = false;

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenePaths,
            locationPathName = outputPath,
            target = BuildTarget.Android,
            options = BuildOptions.Development
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
            throw new BuildFailedException("Android build failed: " + report.summary.result);

        UnityEngine.Debug.Log("Android build completed: " + outputPath);
    }
}
#endif
