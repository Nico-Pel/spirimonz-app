using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

[Serializable]
public class LocalizationFile
{
    public string language;
    public LocalizationSection[] sections;
}

[Serializable]
public class LocalizationSection
{
    public string header;
    public string id;
    public LocalizationEntry[] entries;
}

[Serializable]
public class LocalizationEntry
{
    public string key;
    public string value;
    public string note;
}

public static class LocalizationManager
{
    public static bool LogMissingKeys = true;
    public static string HighlightColorHex = "F9AB2D";

    private static readonly Dictionary<string, string> _english = new Dictionary<string, string>();
    private static readonly Dictionary<string, string> _current = new Dictionary<string, string>();
    private static bool _initialized;
    private static Language _loadedLanguage = Language.English;

    public static void Load(Language language)
    {
        _english.Clear();
        _current.Clear();

        LoadLanguageFile(Language.English, _english);
        if (language == Language.English)
        {
            CopyDictionary(_english, _current);
        }
        else
        {
            LoadLanguageFile(language, _current);
        }

        _initialized = true;
        _loadedLanguage = language;
    }

    public static string Get(string key, string fallback = null)
    {
        if (string.IsNullOrWhiteSpace(key))
            return string.Empty;

        EnsureInitialized();

        if (_current.TryGetValue(key, out string value) && !string.IsNullOrWhiteSpace(value))
            return ApplyMarkup(value);

        if (_english.TryGetValue(key, out value) && !string.IsNullOrWhiteSpace(value))
        {
            if (LogMissingKeys && _loadedLanguage != Language.English)
                Debug.LogWarning($"[Localization] Missing key '{key}' in '{_loadedLanguage}', using English fallback.");
            return ApplyMarkup(value);
        }

        if (LogMissingKeys)
            Debug.LogWarning($"[Localization] Missing key '{key}' (no fallback).");

        return fallback ?? $"[{key}]";
    }

    public static string Format(string key, params object[] args)
    {
        string value = Get(key);
        if (args == null || args.Length == 0)
            return value;
        return string.Format(value, args);
    }

    public static string ApplyMarkup(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        if (value.IndexOf('*') < 0)
            return value;

        string colorTag = $"<color=#{HighlightColorHex}>";
        return Regex.Replace(value, "\\*(.+?)\\*", match => $"{colorTag}{match.Groups[1].Value}</color>");
    }

    private static void EnsureInitialized()
    {
        if (_initialized)
            return;

        Load(LanguageManager.CurrentLanguage);
    }

    private static void LoadLanguageFile(Language language, Dictionary<string, string> target)
    {
        string fileName = GetLanguageFileName(language);
        TextAsset jsonAsset = Resources.Load<TextAsset>($"Localization/{fileName}");
        if (jsonAsset == null)
        {
            if (LogMissingKeys)
                Debug.LogWarning($"[Localization] Missing json file for {language} (Resources/Localization/{fileName}.json).");
            return;
        }

        LocalizationFile file = JsonUtility.FromJson<LocalizationFile>(jsonAsset.text);
        if (file == null || file.sections == null)
            return;

        foreach (LocalizationSection section in file.sections)
        {
            if (section == null || section.entries == null)
                continue;

            foreach (LocalizationEntry entry in section.entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.key))
                    continue;

                target[entry.key] = DecodeEscapes(entry.value ?? string.Empty);
            }
        }
    }

    private static string GetLanguageFileName(Language language)
    {
        switch (language)
        {
            case Language.French:
                return "French";
            case Language.Spanish:
                return "Spanish";
            case Language.Portuguese:
                return "Portuguese";
            default:
                return "English";
        }
    }

    private static void CopyDictionary(Dictionary<string, string> source, Dictionary<string, string> target)
    {
        foreach (var pair in source)
            target[pair.Key] = pair.Value;
    }

    public static string GetGhostTypeName(GhostTypeData.GhostType type)
    {
        string key = $"ghost_type.{type.ToString().ToLowerInvariant()}";
        return Get(key, type.ToString());
    }

    public static string GetEvidenceTypeName(GhostInvestigator.EvidenceType type)
    {
        string key = $"evidence_type.{type.ToString().ToLowerInvariant()}";
        return Get(key, type.ToString());
    }

    private static string DecodeEscapes(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        // Handle literal escape sequences that can slip into JSON (ex: \\xE9)
        value = value.Replace("\\r\\n", "\n");
        value = value.Replace("\\n", "\n");
        value = value.Replace("\\r", "\n");
        value = value.Replace("\\t", "\t");

        value = Regex.Replace(value, "\\\\x([0-9A-Fa-f]{2})", match =>
        {
            if (int.TryParse(match.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int code))
                return ((char)code).ToString();
            return match.Value;
        });

        value = Regex.Replace(value, "\\\\u([0-9A-Fa-f]{4})", match =>
        {
            if (int.TryParse(match.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int code))
                return ((char)code).ToString();
            return match.Value;
        });

        return value;
    }
}
