using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Language
{
    English,
    French,
    Spanish,
    Portuguese,
    Polish,
    Turkish,
    Indonesian,
    Italian,
    German
}

public static class LanguageManager
{
    public static event System.Action<Language> OnLanguageChanged;

    private static Language _currentLanguage = Language.English;

    public static Language CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
            if (_currentLanguage == value)
                return;

            _currentLanguage = value;
            LocalizationManager.Load(_currentLanguage);
            OnLanguageChanged?.Invoke(_currentLanguage);
        }
    }

    public static Language GetBestAvailableLanguageForSystem(SystemLanguage systemLanguage)
    {
        switch (systemLanguage)
        {
            case SystemLanguage.French:
                return Language.French;
            case SystemLanguage.Spanish:
                return Language.Spanish;
            case SystemLanguage.Portuguese:
                return Language.Portuguese;
            case SystemLanguage.Italian:
                return Language.Italian;
            case SystemLanguage.German:
                return Language.German;
            case SystemLanguage.Polish:
                return Language.Polish;
            case SystemLanguage.Turkish:
                return Language.Turkish;
            case SystemLanguage.Indonesian:
                return Language.Indonesian;
            default:
                return Language.English;
        }
    }
}
