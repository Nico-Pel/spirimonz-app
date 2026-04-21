#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class MobileControlsPrefabBuilder
{
    private const string PrefabAssetPath = "Assets/Resources/UI/MobileControlsCanvas.prefab";

    static MobileControlsPrefabBuilder()
    {
        EditorApplication.delayCall += EnsurePrefabExists;
    }

    [MenuItem("Tools/Mobile/Create Missing Mobile Controls Prefab")]
    public static void EnsurePrefabExists()
    {
        if (Application.isPlaying || EditorApplication.isCompiling)
            return;

        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabAssetPath) != null)
            return;

        CreateOrRebuildPrefab();
    }

    [MenuItem("Tools/Mobile/Rebuild Mobile Controls Prefab")]
    public static void RebuildPrefabMenu()
    {
        CreateOrRebuildPrefab();
    }

    [MenuItem("Tools/Mobile/Repair Mobile Controls Prefab References")]
    public static void RepairPrefabReferencesMenu()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabAssetPath) == null)
        {
            CreateOrRebuildPrefab();
            return;
        }

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PrefabAssetPath);
        try
        {
            MobileControlsView view = prefabRoot.GetComponent<MobileControlsView>();
            if (view == null)
                view = prefabRoot.AddComponent<MobileControlsView>();

            RepairReferences(prefabRoot, view);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabAssetPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static void CreateOrRebuildPrefab()
    {
        EnsureFolders();

        GameObject root = BuildPrefabRoot();
        try
        {
            PrefabUtility.SaveAsPrefabAsset(root, PrefabAssetPath);
            AssetDatabase.SaveAssets();
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static GameObject BuildPrefabRoot()
    {
        GameObject root = new GameObject(
            "MobileControlsCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(MobileJoystickInputRouter),
            typeof(MobileControlsView));

        RectTransform rootRect = root.GetComponent<RectTransform>();
        Stretch(rootRect);

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform safeAreaRoot = CreateRect("SafeAreaRoot", rootRect);
        Stretch(safeAreaRoot);

        RectTransform joysticksRoot = CreateRect(
            "MobileJoysticksRoot",
            rootRect,
            typeof(CanvasGroup),
            typeof(MobileControlsRoot));
        Stretch(joysticksRoot);
        ConfigureVisibility(joysticksRoot.GetComponent<MobileControlsRoot>(), true, true, true, false);

        RectTransform keyButtonsRoot = CreateRect(
            "MobileKeyButtonsRoot",
            safeAreaRoot,
            typeof(CanvasGroup),
            typeof(MobileControlsRoot),
            typeof(SafeAreaFitter));
        Stretch(keyButtonsRoot);
        ConfigureVisibility(keyButtonsRoot.GetComponent<MobileControlsRoot>(), false, false, false, true);

        MobileJoystick moveJoystick = CreateMoveJoystick(
            "MoveJoystick",
            joysticksRoot,
            new Vector2(0f, 0f),
            new Vector2(0f, 0f),
            new Vector2(0.5f, 0.5f),
            new Vector2(240f, 220f));

        MobileLookJoystick lookJoystick = CreateLookJoystick(
            "LookJoystick",
            joysticksRoot,
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0.5f, 0.5f),
            new Vector2(-240f, 220f));

        MobileActionButtons actionButtons = CreateActionButtons(joysticksRoot, lookJoystick.transform as RectTransform);
        MobileKeyButtonsVisibility keyButtonsVisibility = CreateKeyButtons(keyButtonsRoot);

        MobileJoystickInputRouter router = root.GetComponent<MobileJoystickInputRouter>();
        router.joystickRoot = joysticksRoot;
        router.moveJoystick = moveJoystick;
        router.lookJoystick = lookJoystick;
        router.freezeFloatingWhenDoorGrabbed = true;
        router.enablePrimaryTouch = true;
        router.enableMouseSimulation = true;

        MobileControlsView view = root.GetComponent<MobileControlsView>();
        RepairReferences(root, view);
        view.safeAreaRoot = safeAreaRoot;
        view.joystickRoot = joysticksRoot;
        view.keyButtonsRoot = keyButtonsRoot;
        view.actionButtons = actionButtons;
        view.keyButtonsVisibility = keyButtonsVisibility;

        return root;
    }

    private static void RepairReferences(GameObject root, MobileControlsView view)
    {
        view.safeAreaRoot = root.transform.Find("SafeAreaRoot") as RectTransform;
        view.joystickRoot = root.transform.Find("MobileJoysticksRoot") as RectTransform;
        view.keyButtonsRoot = root.transform.Find("SafeAreaRoot/MobileKeyButtonsRoot") as RectTransform;

        if (view.joystickRoot != null)
            view.joystickVisibilityRoot = view.joystickRoot.GetComponent<MobileControlsRoot>();
        if (view.keyButtonsRoot != null)
        {
            view.keyButtonsVisibilityRoot = view.keyButtonsRoot.GetComponent<MobileControlsRoot>();
            view.keyButtonsSafeArea = view.keyButtonsRoot.GetComponent<SafeAreaFitter>();
        }

        view.moveJoystick = root.GetComponentInChildren<MobileJoystick>(true);
        view.lookJoystick = root.GetComponentInChildren<MobileLookJoystick>(true);
        view.inputRouter = root.GetComponent<MobileJoystickInputRouter>();
        view.actionButtons = root.GetComponentInChildren<MobileActionButtons>(true);
        view.keyButtonsVisibility = root.GetComponentInChildren<MobileKeyButtonsVisibility>(true);

        if (view.moveJoystick != null)
        {
            view.moveJoystickBaseImage = view.moveJoystick.GetComponent<Image>();
            if (view.moveJoystick.handle != null)
                view.moveJoystickHandleImage = view.moveJoystick.handle.GetComponent<Image>();
        }

        if (view.lookJoystick != null)
        {
            view.lookJoystickBaseImage = view.lookJoystick.GetComponent<Image>();
            if (view.lookJoystick.handle != null)
                view.lookJoystickHandleImage = view.lookJoystick.handle.GetComponent<Image>();
        }

        if (view.actionButtons != null)
        {
            if (view.actionButtons.primaryButton != null)
                view.grabButtonImage = view.actionButtons.primaryButton.GetComponent<Image>();
            if (view.actionButtons.secondaryButton != null)
                view.secondaryButtonImage = view.actionButtons.secondaryButton.GetComponent<Image>();
            if (view.actionButtons.torchButton != null)
                view.torchButtonImage = view.actionButtons.torchButton.GetComponent<Image>();
        }

        view.EnsureReferences();
        EditorUtility.SetDirty(root);
        EditorUtility.SetDirty(view);
    }

    private static MobileJoystick CreateMoveJoystick(
        string name,
        RectTransform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition)
    {
        RectTransform baseRect = CreateRect(
            name,
            parent,
            typeof(Image),
            typeof(MobileJoystick));
        baseRect.anchorMin = anchorMin;
        baseRect.anchorMax = anchorMax;
        baseRect.pivot = pivot;
        baseRect.sizeDelta = new Vector2(320f, 320f);
        baseRect.anchoredPosition = anchoredPosition;

        Image baseImage = baseRect.GetComponent<Image>();
        baseImage.color = new Color(1f, 1f, 1f, 0.12f);
        baseImage.sprite = GetBuiltinSprite("UI/Skin/Background.psd");
        baseImage.type = Image.Type.Sliced;
        baseImage.raycastTarget = false;

        RectTransform handleRect = CreateRect("Handle", baseRect, typeof(Image));
        handleRect.anchorMin = new Vector2(0.5f, 0.5f);
        handleRect.anchorMax = new Vector2(0.5f, 0.5f);
        handleRect.pivot = new Vector2(0.5f, 0.5f);
        handleRect.sizeDelta = new Vector2(190f, 190f);
        handleRect.anchoredPosition = Vector2.zero;

        Image handleImage = handleRect.GetComponent<Image>();
        handleImage.color = new Color(1f, 1f, 1f, 0.35f);
        handleImage.sprite = GetBuiltinSprite("UI/Skin/Knob.psd");
        handleImage.type = Image.Type.Sliced;
        handleImage.raycastTarget = false;

        MobileJoystick moveJoystick = baseRect.GetComponent<MobileJoystick>();
        moveJoystick.handle = handleRect;
        moveJoystick.handleRange = 140f;
        moveJoystick.deadZone = 0.1f;
        moveJoystick.floating = true;
        moveJoystick.returnToOriginOnRelease = true;
        return moveJoystick;
    }

    private static MobileLookJoystick CreateLookJoystick(
        string name,
        RectTransform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition)
    {
        RectTransform baseRect = CreateRect(
            name,
            parent,
            typeof(Image),
            typeof(MobileLookJoystick));
        baseRect.anchorMin = anchorMin;
        baseRect.anchorMax = anchorMax;
        baseRect.pivot = pivot;
        baseRect.sizeDelta = new Vector2(320f, 320f);
        baseRect.anchoredPosition = anchoredPosition;

        Image baseImage = baseRect.GetComponent<Image>();
        baseImage.color = new Color(1f, 1f, 1f, 0.12f);
        baseImage.sprite = GetBuiltinSprite("UI/Skin/Background.psd");
        baseImage.type = Image.Type.Sliced;
        baseImage.raycastTarget = false;

        RectTransform handleRect = CreateRect("Handle", baseRect, typeof(Image));
        handleRect.anchorMin = new Vector2(0.5f, 0.5f);
        handleRect.anchorMax = new Vector2(0.5f, 0.5f);
        handleRect.pivot = new Vector2(0.5f, 0.5f);
        handleRect.sizeDelta = new Vector2(190f, 190f);
        handleRect.anchoredPosition = Vector2.zero;

        Image handleImage = handleRect.GetComponent<Image>();
        handleImage.color = new Color(1f, 1f, 1f, 0.35f);
        handleImage.sprite = GetBuiltinSprite("UI/Skin/Knob.psd");
        handleImage.type = Image.Type.Sliced;
        handleImage.raycastTarget = false;

        MobileLookJoystick lookJoystick = baseRect.GetComponent<MobileLookJoystick>();
        lookJoystick.handle = handleRect;
        lookJoystick.handleRange = 140f;
        lookJoystick.deadZone = 0.1f;
        lookJoystick.floating = true;
        lookJoystick.returnToOriginOnRelease = true;
        return lookJoystick;
    }

    private static MobileActionButtons CreateActionButtons(RectTransform parent, RectTransform lookJoystickRect)
    {
        RectTransform groupRect = CreateRect("MobileActionButtons", parent, typeof(MobileActionButtons));
        groupRect.anchorMin = new Vector2(1f, 0f);
        groupRect.anchorMax = new Vector2(1f, 0f);
        groupRect.pivot = new Vector2(0.5f, 0.5f);
        groupRect.anchoredPosition = Vector2.zero;

        Vector2 rightJoystickAnchoredPos = lookJoystickRect != null ? lookJoystickRect.anchoredPosition : new Vector2(-240f, 220f);
        Vector2 arcCenter = rightJoystickAnchoredPos + new Vector2(-120f, 120f);
        Vector2 leftPos = arcCenter + AngleToOffset(120f, 200f);
        Vector2 rightPos = arcCenter + AngleToOffset(60f, 200f);
        Vector2 torchPos = rightJoystickAnchoredPos + new Vector2(-170f, -120f);

        GameObject grabButton = CreateActionButton(groupRect, "Action_A", "A", MobileButton.Action.Grab, leftPos, 120f);
        GameObject secondaryButton = CreateActionButton(groupRect, "Action_B", "B", MobileButton.Action.Secondary, rightPos, 120f);
        GameObject torchButton = CreateActionButton(groupRect, "Action_Torch", "T", MobileButton.Action.ToggleLight, torchPos, 80f);

        MobileActionButtons actionButtons = groupRect.GetComponent<MobileActionButtons>();
        actionButtons.primaryButton = grabButton;
        actionButtons.secondaryButton = secondaryButton;
        actionButtons.torchButton = torchButton;
        return actionButtons;
    }

    private static MobileKeyButtonsVisibility CreateKeyButtons(RectTransform parent)
    {
        RectTransform groupRect = CreateRect("MobileKeyButtons", parent, typeof(MobileKeyButtonsVisibility));
        groupRect.anchorMin = new Vector2(0f, 1f);
        groupRect.anchorMax = new Vector2(0f, 1f);
        groupRect.pivot = new Vector2(0f, 1f);
        groupRect.anchoredPosition = new Vector2(20f, -20f);
        groupRect.sizeDelta = new Vector2(110f, 400f);

        (string name, string label, MobileButton.Action action)[] keys =
        {
            ("Key_ESC", "ESC", MobileButton.Action.ExitMenus),
            ("Key_J", "J", MobileButton.Action.OpenJournal),
            ("Key_Y", "Y", MobileButton.Action.KeyY),
            ("Key_Prev", "Prev", MobileButton.Action.Previous),
            ("Key_Nxt", "Nxt", MobileButton.Action.Next),
        };

        GameObject yButton = null;
        GameObject prevButton = null;
        GameObject nextButton = null;

        for (int i = 0; i < keys.Length; i++)
        {
            GameObject button = CreateKeyButton(groupRect, keys[i].name, keys[i].label, keys[i].action, i);
            if (keys[i].action == MobileButton.Action.KeyY)
                yButton = button;
            else if (keys[i].action == MobileButton.Action.Previous)
                prevButton = button;
            else if (keys[i].action == MobileButton.Action.Next)
                nextButton = button;
        }

        MobileKeyButtonsVisibility visibility = groupRect.GetComponent<MobileKeyButtonsVisibility>();
        visibility.yButton = yButton;
        visibility.prevButton = prevButton;
        visibility.nextButton = nextButton;
        return visibility;
    }

    private static GameObject CreateActionButton(
        RectTransform parent,
        string name,
        string label,
        MobileButton.Action action,
        Vector2 anchoredPosition,
        float size)
    {
        RectTransform rect = CreateRect(name, parent, typeof(Image), typeof(Button), typeof(MobileButton));
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(size, size);
        rect.anchoredPosition = anchoredPosition;

        Image image = rect.GetComponent<Image>();
        image.sprite = GetBuiltinSprite("UI/Skin/UISprite.psd");
        image.type = Image.Type.Sliced;
        image.color = new Color(0f, 0f, 0f, 0.55f);
        image.raycastTarget = true;

        rect.GetComponent<MobileButton>().action = action;

        Text text = CreateText(rect, "Label", label, 30);
        text.alignment = TextAnchor.MiddleCenter;
        return rect.gameObject;
    }

    private static GameObject CreateKeyButton(
        RectTransform parent,
        string name,
        string label,
        MobileButton.Action action,
        int row)
    {
        RectTransform rect = CreateRect(name, parent, typeof(Image), typeof(Button), typeof(MobileButton));
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(110f, 72f);
        rect.anchoredPosition = new Vector2(0f, -(row * 82f));

        Image image = rect.GetComponent<Image>();
        image.sprite = GetBuiltinSprite("UI/Skin/UISprite.psd");
        image.type = Image.Type.Sliced;
        image.color = new Color(0f, 0f, 0f, 0.55f);
        image.raycastTarget = true;

        rect.GetComponent<MobileButton>().action = action;

        Text text = CreateText(rect, "Label", label, 22);
        text.alignment = TextAnchor.MiddleCenter;
        return rect.gameObject;
    }

    private static Text CreateText(RectTransform parent, string name, string value, int fontSize)
    {
        RectTransform textRect = CreateRect(name, parent, typeof(Text));
        Stretch(textRect);

        Text text = textRect.GetComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.color = new Color(1f, 1f, 1f, 0.95f);
        text.raycastTarget = false;
        return text;
    }

    private static RectTransform CreateRect(string name, Transform parent, params System.Type[] extraComponents)
    {
        System.Type[] componentTypes = new System.Type[extraComponents.Length + 1];
        componentTypes[0] = typeof(RectTransform);
        for (int i = 0; i < extraComponents.Length; i++)
            componentTypes[i + 1] = extraComponents[i];

        GameObject go = new GameObject(name, componentTypes);
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private static void ConfigureVisibility(
        MobileControlsRoot root,
        bool hideWhenTablet,
        bool hideWhenDialogue,
        bool hideWhenEndGame,
        bool alwaysVisibleWhenMobile)
    {
        root.hideWhenTablet = hideWhenTablet;
        root.hideWhenDialogue = hideWhenDialogue;
        root.hideWhenEndGame = hideWhenEndGame;
        root.alwaysVisibleWhenMobile = alwaysVisibleWhenMobile;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/UI"))
            AssetDatabase.CreateFolder("Assets/Resources", "UI");
    }

    private static Vector2 AngleToOffset(float angleDeg, float radius)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;
    }

    private static Sprite GetBuiltinSprite(string path)
    {
        return AssetDatabase.GetBuiltinExtraResource<Sprite>(path);
    }
}
#endif
