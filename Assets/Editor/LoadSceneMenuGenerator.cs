#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class LoadSceneMenuGenerator
{
    private const string OutputPath = "Assets/Editor/Generated/LoadSceneMenuItems.cs";

    static LoadSceneMenuGenerator()
    {
        EditorApplication.delayCall += GenerateIfNeeded;
    }

    [MenuItem("LoadScene/Refresh Menu", priority = 2000)]
    private static void RefreshMenu()
    {
        Generate(force: true);
    }

    private static void GenerateIfNeeded()
    {
        Generate(force: false);
    }

    private static void Generate(bool force)
    {
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        if (scenes == null || scenes.Length == 0)
            return;

        var sceneInfos = scenes
            .Where(s => s != null && !string.IsNullOrWhiteSpace(s.path))
            .Select(s =>
            {
                string name = Path.GetFileNameWithoutExtension(s.path);
                string parent = Path.GetFileName(Path.GetDirectoryName(s.path));
                if (string.IsNullOrWhiteSpace(parent))
                    parent = "Scenes";
                return new { s.path, s.enabled, name, parent };
            })
            .ToList();

        var nameCounts = sceneInfos
            .GroupBy(s => s.name)
            .ToDictionary(g => g.Key, g => g.Count());

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("#if UNITY_EDITOR");
        sb.AppendLine("using UnityEditor;");
        sb.AppendLine("using UnityEditor.SceneManagement;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("public static class LoadSceneMenuItems");
        sb.AppendLine("{");
        sb.AppendLine("    private static void Open(string path)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())");
        sb.AppendLine("            EditorSceneManager.OpenScene(path);");
        sb.AppendLine("    }");

        int index = 0;
        foreach (var info in sceneInfos)
        {
            string label = nameCounts[info.name] > 1 ? $"{info.parent}/{info.name}" : info.name;
            string menuPath = info.enabled ? $"LoadScene/{label}" : $"LoadScene/Disabled/{label}";
            string methodName = $"Open_{SanitizeIdentifier(label)}_{index}";
            sb.AppendLine($"    [MenuItem(\"{menuPath}\")]");
            sb.AppendLine($"    private static void {methodName}() => Open(\"{info.path}\");");
            index++;
        }

        sb.AppendLine("}");
        sb.AppendLine("#endif");

        string newContent = sb.ToString();
        if (!force && File.Exists(OutputPath))
        {
            string existing = File.ReadAllText(OutputPath);
            if (existing == newContent)
                return;
        }

        string folder = Path.GetDirectoryName(OutputPath);
        if (!string.IsNullOrEmpty(folder))
            Directory.CreateDirectory(folder);

        File.WriteAllText(OutputPath, newContent, Encoding.UTF8);
        AssetDatabase.ImportAsset(OutputPath);
    }

    private static string SanitizeIdentifier(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "_Scene";

        StringBuilder sb = new StringBuilder();
        foreach (char c in value)
        {
            sb.Append(char.IsLetterOrDigit(c) ? c : '_');
        }

        if (char.IsDigit(sb[0]))
            sb.Insert(0, '_');

        return sb.ToString();
    }
}
#endif
