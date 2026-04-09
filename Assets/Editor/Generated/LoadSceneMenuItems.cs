#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class LoadSceneMenuItems
{
    private static void Open(string path)
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene(path);
    }
    [MenuItem("LoadScene/TitleScreen")]
    private static void Open_TitleScreen_0() => Open("Assets/Scenes/TitleScreen.unity");
    [MenuItem("LoadScene/World01")]
    private static void Open_World01_1() => Open("Assets/Scenes/World01.unity");
    [MenuItem("LoadScene/House00")]
    private static void Open_House00_2() => Open("Assets/Scenes/House00.unity");
    [MenuItem("LoadScene/House01")]
    private static void Open_House01_3() => Open("Assets/Scenes/House01.unity");
    [MenuItem("LoadScene/House02")]
    private static void Open_House02_4() => Open("Assets/Scenes/House02.unity");
    [MenuItem("LoadScene/House03")]
    private static void Open_House03_5() => Open("Assets/Scenes/House03.unity");
    [MenuItem("LoadScene/House04")]
    private static void Open_House04_6() => Open("Assets/Scenes/House04.unity");
    [MenuItem("LoadScene/House05")]
    private static void Open_House05_7() => Open("Assets/Scenes/House05.unity");
    [MenuItem("LoadScene/HouseTuto")]
    private static void Open_HouseTuto_8() => Open("Assets/Scenes/HouseTuto.unity");
    [MenuItem("LoadScene/WorldSecretSakura")]
    private static void Open_WorldSecretSakura_9() => Open("Assets/Scenes/WorldSecretSakura.unity");
    [MenuItem("LoadScene/HouseSakura")]
    private static void Open_HouseSakura_10() => Open("Assets/Scenes/HouseSakura.unity");
}
#endif
