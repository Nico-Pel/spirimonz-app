using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Dialogue/Dialogue")]
public class Dialogue : ScriptableObject
{
    public string npcName;

    public List<DialogueLine> lines;
    
    [Space]
    public LetterSoundProfile letterSoundProfile = new LetterSoundProfile();
    public CharacterVoiceType voiceType = CharacterVoiceType.Custom;
    
    private static readonly Dictionary<CharacterVoiceType, LetterSoundProfile> DefaultProfiles = new Dictionary<CharacterVoiceType, LetterSoundProfile>()
    {
        { CharacterVoiceType.Girl, new LetterSoundProfile { baseBeep = null, minPitch = 1.0f, maxPitch = 1.2f } },
        { CharacterVoiceType.Lady, new LetterSoundProfile { baseBeep = null, minPitch = 1.0f, maxPitch = 1.1f } },
        { CharacterVoiceType.OldLady, new LetterSoundProfile { baseBeep = null, minPitch = 0.9f, maxPitch = 1.05f } },
        { CharacterVoiceType.Boy, new LetterSoundProfile { baseBeep = null, minPitch = 0.95f, maxPitch = 1.15f } },
        { CharacterVoiceType.Man, new LetterSoundProfile { baseBeep = null, minPitch = 0.9f, maxPitch = 1.1f } },
        { CharacterVoiceType.OldMan, new LetterSoundProfile { baseBeep = null, minPitch = 0.85f, maxPitch = 1.0f } },
        { CharacterVoiceType.Custom, new LetterSoundProfile() }
    };
    
    private LetterSoundProfile _lastProfile; 

    public string GetLocalizedNpcName()
    {
        string fallback = npcName;
        return LocalizationManager.Get(LocalizationKeys.DialogueNpcName(this), fallback);
    }

    public string GetLocalizedLine(int index)
    {
        if (lines == null || index < 0 || index >= lines.Count)
            return string.Empty;

        DialogueLine line = lines[index];
        string fallback = LanguageManager.CurrentLanguage == Language.French ? line.french : line.english;
        return LocalizationManager.Get(LocalizationKeys.DialogueLine(this, index), fallback);
    }
    
    private void OnValidate()
    {
        // 1️⃣ Si l'enum est différent de Custom, appliquer les paramètres prédéfinis
        if (voiceType != CharacterVoiceType.Custom)
        {
            LetterSoundProfile preset = DefaultProfiles[voiceType];
            if (letterSoundProfile.baseBeep != preset.baseBeep ||
                letterSoundProfile.minPitch != preset.minPitch ||
                letterSoundProfile.maxPitch != preset.maxPitch)
            {
                letterSoundProfile.baseBeep = preset.baseBeep;
                letterSoundProfile.minPitch = preset.minPitch;
                letterSoundProfile.maxPitch = preset.maxPitch;
            }
            _lastProfile = new LetterSoundProfile(letterSoundProfile);
        }
        else
        {
            // 2️⃣ Si on est en Custom, on ne touche pas aux paramètres
            _lastProfile = new LetterSoundProfile(letterSoundProfile);
        }

        // 3️⃣ Détecter modification manuelle et passer l'enum sur Custom
        if (voiceType != CharacterVoiceType.Custom && _lastProfile != null)
        {
            if (letterSoundProfile.baseBeep != _lastProfile.baseBeep ||
                letterSoundProfile.minPitch != _lastProfile.minPitch ||
                letterSoundProfile.maxPitch != _lastProfile.maxPitch)
            {
                voiceType = CharacterVoiceType.Custom;
            }
        }
    }
}

[System.Serializable]
public class DialogueLine
{
    [TextArea(3,6)]
    public string english;
    [TextArea(3,6)]
    public string french;
    


    public string GetText()
    {
        switch (LanguageManager.CurrentLanguage)
        {
            case Language.French:
                return french;

            default:
                return english;
        }
    }
}

[System.Serializable]
public class LetterSoundProfile
{
    public AudioClip baseBeep;
    public float minPitch = 0.9f;
    public float maxPitch = 1.1f;

    // Constructeur de copie pour comparer
    public LetterSoundProfile() { }
    public LetterSoundProfile(LetterSoundProfile other)
    {
        baseBeep = other.baseBeep;
        minPitch = other.minPitch;
        maxPitch = other.maxPitch;
    }
}

public enum CharacterVoiceType
{
    Girl,
    Lady,
    OldLady,
    Boy,
    Man,
    OldMan,
    Custom
}
