#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
public static class LoadSceneMenuItems
{
    private static void Open(string path)
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene(path);
    }
    private static void OpenTitleScreenWithPolicies(string path)
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        LoadScenePolicyPreview.RequestPoliciesOnNextTitleScreen();
        EditorSceneManager.OpenScene(path);
    }
    [MenuItem("LoadScene/TitleScreen", priority = 0)]
    private static void Open_TitleScreen_0() => Open("Assets/Scenes/TitleScreen.unity");
    [MenuItem("LoadScene/TitleScreen[Policies]", priority = 1)]
    private static void Open_TitleScreenPolicies_0() => OpenTitleScreenWithPolicies("Assets/Scenes/TitleScreen.unity");
    [MenuItem("LoadScene/World01", priority = 11)]
    private static void Open_World01_1() => Open("Assets/Scenes/World01.unity");
    [MenuItem("LoadScene/WSObakemachi", priority = 21)]
    private static void Open_WSObakemachi_2() => Open("Assets/Scenes/WSObakemachi.unity");
    [MenuItem("LoadScene/WSWinter", priority = 31)]
    private static void Open_WSWinter_3() => Open("Assets/Scenes/WSWinter.unity");
    [MenuItem("LoadScene/House00", priority = 41)]
    private static void Open_House00_4() => Open("Assets/Scenes/House00.unity");
    [MenuItem("LoadScene/House01", priority = 51)]
    private static void Open_House01_5() => Open("Assets/Scenes/House01.unity");
    [MenuItem("LoadScene/House02", priority = 61)]
    private static void Open_House02_6() => Open("Assets/Scenes/House02.unity");
    [MenuItem("LoadScene/House03", priority = 71)]
    private static void Open_House03_7() => Open("Assets/Scenes/House03.unity");
    [MenuItem("LoadScene/House04", priority = 81)]
    private static void Open_House04_8() => Open("Assets/Scenes/House04.unity");
    [MenuItem("LoadScene/House05", priority = 91)]
    private static void Open_House05_9() => Open("Assets/Scenes/House05.unity");
    [MenuItem("LoadScene/HouseSakura", priority = 101)]
    private static void Open_HouseSakura_10() => Open("Assets/Scenes/HouseSakura.unity");
    [MenuItem("LoadScene/HouseTuto", priority = 111)]
    private static void Open_HouseTuto_11() => Open("Assets/Scenes/HouseTuto.unity");
    [MenuItem("LoadScene/HouseWinter", priority = 121)]
    private static void Open_HouseWinter_12() => Open("Assets/Scenes/HouseWinter.unity");
}
#endif
