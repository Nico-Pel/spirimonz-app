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
}
