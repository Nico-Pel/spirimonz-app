using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Language
{
    English,
    French
}

public static class LanguageManager
{
    public static Language CurrentLanguage = Language.English;
}