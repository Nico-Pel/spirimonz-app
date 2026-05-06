using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public sealed class AndroidBuildSettingsEnforcer : IPreprocessBuildWithReport
{
    public int callbackOrder => int.MinValue;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.Android)
            return;

        // Google Play requires a 64-bit Android binary. On Unity 2022, this
        // means forcing IL2CPP and both ARMv7 + ARM64 before every Android build.
        PlayerSettings.SetScriptingBackend(
            NamedBuildTarget.Android,
            ScriptingImplementation.IL2CPP
        );

        PlayerSettings.Android.targetArchitectures =
            AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
    }
}
