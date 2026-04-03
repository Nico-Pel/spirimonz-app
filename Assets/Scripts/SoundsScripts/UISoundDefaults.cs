using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class UISoundDefaults
{
#if !UNITY_EDITOR
    public static void AssignIfNull(ref SoundParameters sound)
    {
        if (sound != null)
            sound.isUISound = true;
    }
#endif

    public static void MarkAsUi(SoundParameters sound)
    {
        if (sound != null)
            sound.isUISound = true;
    }

    public static void MarkHierarchyAsUiSounds(GameObject root)
    {
        if (root == null)
            return;

        SoundParameters[] sounds = root.GetComponentsInChildren<SoundParameters>(true);
        foreach (SoundParameters sound in sounds)
        {
            if (sound != null)
                sound.isUISound = true;
        }
    }

#if UNITY_EDITOR
    private const string DefaultUiSoundPath = "Assets/Sounds/UISounds/UIButton_SoundParameters.prefab";

    public static SoundParameters LoadDefault()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DefaultUiSoundPath);
        return prefab != null ? prefab.GetComponent<SoundParameters>() : null;
    }

    public static void AssignIfNull(ref SoundParameters sound)
    {
        if (sound != null)
        {
            sound.isUISound = true;
            return;
        }

        sound = LoadDefault();
        if (sound != null)
            sound.isUISound = true;
    }
#endif
}
