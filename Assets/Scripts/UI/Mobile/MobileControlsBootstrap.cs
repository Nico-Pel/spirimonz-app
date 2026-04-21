using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MobileControlsBootstrap : MonoBehaviour
{
    [Header("Layout")]
    public Vector2 leftJoystickAnchoredPos = new Vector2(240f, 220f);
    public Vector2 rightJoystickAnchoredPos = new Vector2(-240f, 220f);
    public float joystickSize = 320f;
    public float handleSize = 190f;
    public float handleRange = 140f;
    public float deadZone = 0.1f;

    [Header("Visuals")]
    public Color baseColor = new Color(1f, 1f, 1f, 0.12f);
    public Color handleColor = new Color(1f, 1f, 1f, 0.35f);
    public Sprite baseSprite;
    public Sprite handleSprite;

    [Header("Key Buttons")]
    public bool createKeyButtons = true;
    public Font keyButtonFont;
    public Vector2 keyButtonsAnchoredPos = new Vector2(20f, -20f);
    public Vector2 keyButtonSize = new Vector2(110f, 72f);
    public float keyButtonSpacing = 10f;
    public int keyButtonFontSize = 22;
    public Color keyButtonColor = new Color(0f, 0f, 0f, 0.55f);
    public Color keyButtonTextColor = new Color(1f, 1f, 1f, 0.9f);
    public Vector2 keyButtonsSafePaddingMin = Vector2.zero;
    public Vector2 keyButtonsSafePaddingMax = Vector2.zero;

    [Header("Action Buttons")]
    public bool createActionButtons = true;
    public float actionButtonSize = 120f;
    public float actionButtonRadius = 200f;
    public float actionButtonCenterYOffset = 40f;
    public float actionButtonAngleLeft = 120f;  // F
    public float actionButtonAngleCenter = 90f; // E
    public float actionButtonAngleRight = 60f;  // G
    public Vector2 actionButtonsOffset = new Vector2(-120f, 80f);
    public string secondaryButtonLabel = "B";
    public int actionButtonFontSize = 30;
    public Color actionButtonColor = new Color(0f, 0f, 0f, 0.55f);
    public Color actionButtonTextColor = new Color(1f, 1f, 1f, 0.95f);

    [Header("Torch Button")]
    public bool createTorchButton = true;
    public float torchButtonSize = 80f;
    public Vector2 torchButtonOffset = new Vector2(-170f, -120f);
    public string torchButtonLabel = "T";

    private static bool _created;

    public static void EnsureExists()
    {
        if (_created ||
            FindObjectOfType<MobileControlsBootstrap>() != null ||
            FindObjectOfType<MobileControlsView>() != null ||
            FindObjectOfType<MobileControlsRoot>() != null)
            return;

        GameObject go = new GameObject("MobileControlsBootstrap");
        DontDestroyOnLoad(go);
        go.AddComponent<MobileControlsBootstrap>();
        _created = true;
    }

    private void Awake()
    {
        if (FindObjectOfType<MobileControlsView>() != null || FindObjectOfType<MobileControlsRoot>() != null)
        {
            _created = true;
            return;
        }

        CreateEventSystemIfMissing();
        if (!TryInstantiatePrefab())
            CreateCanvasAndJoysticks();
        _created = true;
    }

    private bool TryInstantiatePrefab()
    {
        GameObject prefab = Resources.Load<GameObject>(MobileControlsView.ResourcePath);
        if (prefab == null)
            return false;

        GameObject instance = Instantiate(prefab);
        instance.name = prefab.name;
        DontDestroyOnLoad(instance);

        MobileControlsView view = instance.GetComponent<MobileControlsView>();
        if (view != null)
            view.InitializeAfterInstantiation();

        return true;
    }

    private void CreateEventSystemIfMissing()
    {
        if (FindObjectOfType<EventSystem>() != null)
            return;

        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
        DontDestroyOnLoad(es);
    }

    private void CreateCanvasAndJoysticks()
    {
        GameObject canvasGO = new GameObject("MobileControlsCanvas");
        DontDestroyOnLoad(canvasGO);

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        if (baseSprite == null)
            baseSprite = CreateCircleSprite(128, 0.95f);
        if (handleSprite == null)
            handleSprite = CreateCircleSprite(128, 0.95f);

        RectTransform joysticksRoot = CreateRoot(canvasGO.transform, "MobileJoysticksRoot");
        RectTransform keysRoot = CreateRoot(canvasGO.transform, "MobileKeyButtonsRoot");

        CanvasGroup joystickGroup = joysticksRoot.gameObject.AddComponent<CanvasGroup>();
        MobileControlsRoot joystickVisibility = joysticksRoot.gameObject.AddComponent<MobileControlsRoot>();
        joystickVisibility.hideWhenTablet = true;
        joystickVisibility.hideWhenDialogue = true;
        joystickVisibility.hideWhenEndGame = true;
        joystickVisibility.alwaysVisibleWhenMobile = false;

        CanvasGroup keysGroup = keysRoot.gameObject.AddComponent<CanvasGroup>();
        MobileControlsRoot keysVisibility = keysRoot.gameObject.AddComponent<MobileControlsRoot>();
        keysVisibility.hideWhenTablet = false;
        keysVisibility.hideWhenDialogue = false;
        keysVisibility.hideWhenEndGame = false;
        keysVisibility.alwaysVisibleWhenMobile = true;

        SafeAreaFitter keysSafeArea = keysRoot.gameObject.AddComponent<SafeAreaFitter>();
        keysSafeArea.extraPaddingMin = keyButtonsSafePaddingMin;
        keysSafeArea.extraPaddingMax = keyButtonsSafePaddingMax;

        MobileJoystick moveJoystick = CreateMoveJoystick(joysticksRoot, baseSprite, handleSprite);
        MobileLookJoystick lookJoystick = CreateLookJoystick(joysticksRoot, baseSprite, handleSprite);

        CreateJoystickRouter(canvasGO, joysticksRoot, moveJoystick, lookJoystick);

        if (createActionButtons)
            CreateActionButtons(joysticksRoot, moveJoystick, lookJoystick);

        if (createKeyButtons)
            CreateKeyButtons(keysRoot);
    }

    private RectTransform CreateRoot(Transform parent, string name)
    {
        GameObject root = new GameObject(name, typeof(RectTransform));
        root.transform.SetParent(parent, false);

        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return rect;
    }

    private MobileJoystick CreateMoveJoystick(Transform parent, Sprite baseSpriteToUse, Sprite handleSpriteToUse)
    {
        GameObject baseGO = new GameObject("MoveJoystick", typeof(RectTransform), typeof(Image), typeof(MobileJoystick));
        baseGO.transform.SetParent(parent, false);

        RectTransform baseRect = baseGO.GetComponent<RectTransform>();
        baseRect.anchorMin = Vector2.zero;
        baseRect.anchorMax = Vector2.zero;
        baseRect.pivot = new Vector2(0.5f, 0.5f);
        baseRect.sizeDelta = new Vector2(joystickSize, joystickSize);
        baseRect.anchoredPosition = leftJoystickAnchoredPos;

        Image baseImg = baseGO.GetComponent<Image>();
        baseImg.sprite = baseSpriteToUse;
        baseImg.color = baseColor;
        baseImg.raycastTarget = false;

        RectTransform handle = CreateHandle(baseRect, handleSpriteToUse);

        MobileJoystick joystick = baseGO.GetComponent<MobileJoystick>();
        joystick.handle = handle;
        joystick.handleRange = handleRange;
        joystick.deadZone = deadZone;
        joystick.CacheStartPositions();

        return joystick;
    }

    private MobileLookJoystick CreateLookJoystick(Transform parent, Sprite baseSpriteToUse, Sprite handleSpriteToUse)
    {
        GameObject baseGO = new GameObject("LookJoystick", typeof(RectTransform), typeof(Image), typeof(MobileLookJoystick));
        baseGO.transform.SetParent(parent, false);

        RectTransform baseRect = baseGO.GetComponent<RectTransform>();
        baseRect.anchorMin = new Vector2(1f, 0f);
        baseRect.anchorMax = new Vector2(1f, 0f);
        baseRect.pivot = new Vector2(0.5f, 0.5f);
        baseRect.sizeDelta = new Vector2(joystickSize, joystickSize);
        baseRect.anchoredPosition = rightJoystickAnchoredPos;

        Image baseImg = baseGO.GetComponent<Image>();
        baseImg.sprite = baseSpriteToUse;
        baseImg.color = baseColor;
        baseImg.raycastTarget = false;

        RectTransform handle = CreateHandle(baseRect, handleSpriteToUse);

        MobileLookJoystick joystick = baseGO.GetComponent<MobileLookJoystick>();
        joystick.handle = handle;
        joystick.handleRange = handleRange;
        joystick.deadZone = deadZone;
        joystick.CacheStartPositions();

        return joystick;
    }

    private RectTransform CreateHandle(RectTransform parent, Sprite sprite)
    {
        GameObject handleGO = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleGO.transform.SetParent(parent, false);

        RectTransform handleRect = handleGO.GetComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0.5f, 0.5f);
        handleRect.anchorMax = new Vector2(0.5f, 0.5f);
        handleRect.pivot = new Vector2(0.5f, 0.5f);
        handleRect.sizeDelta = new Vector2(handleSize, handleSize);
        handleRect.anchoredPosition = Vector2.zero;

        Image handleImg = handleGO.GetComponent<Image>();
        handleImg.sprite = sprite;
        handleImg.color = handleColor;
        handleImg.raycastTarget = false;

        return handleRect;
    }

    private void CreateKeyButtons(Transform parent)
    {
        GameObject group = new GameObject("MobileKeyButtons", typeof(RectTransform));
        group.transform.SetParent(parent, false);

        RectTransform groupRect = group.GetComponent<RectTransform>();
        groupRect.anchorMin = new Vector2(0f, 1f);
        groupRect.anchorMax = new Vector2(0f, 1f);
        groupRect.pivot = new Vector2(0f, 1f);
        groupRect.anchoredPosition = keyButtonsAnchoredPos;

        (string label, MobileButton.Action action)[] keys =
        {
            ("ESC", MobileButton.Action.ExitMenus),
            ("J", MobileButton.Action.OpenJournal),
            ("Y", MobileButton.Action.KeyY),
            ("Prev", MobileButton.Action.Previous),
            ("Nxt", MobileButton.Action.Next),
        };

        float totalHeight = (keyButtonSize.y * keys.Length) + (keyButtonSpacing * (keys.Length - 1));
        groupRect.sizeDelta = new Vector2(keyButtonSize.x, totalHeight);

        GameObject yButton = null;
        GameObject prevButton = null;
        GameObject nextButton = null;

        for (int i = 0; i < keys.Length; i++)
        {
            GameObject btn = CreateKeyButton(groupRect, keys[i].label, keys[i].action, 0, i);
            if (keys[i].label == "Y") yButton = btn;
            else if (keys[i].label == "Prev") prevButton = btn;
            else if (keys[i].label == "Nxt") nextButton = btn;
        }

        MobileKeyButtonsVisibility visibility = group.AddComponent<MobileKeyButtonsVisibility>();
        visibility.yButton = yButton;
        visibility.prevButton = prevButton;
        visibility.nextButton = nextButton;
    }

    private GameObject CreateKeyButton(RectTransform parent, string label, MobileButton.Action action, int col, int row)
    {
        GameObject btnGO = new GameObject($"Key_{label}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(MobileButton));
        btnGO.transform.SetParent(parent, false);

        RectTransform rect = btnGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.sizeDelta = keyButtonSize;
        rect.anchoredPosition = new Vector2(
            col * (keyButtonSize.x + keyButtonSpacing),
            -row * (keyButtonSize.y + keyButtonSpacing)
        );

        Image img = btnGO.GetComponent<Image>();
        img.color = keyButtonColor;
        img.raycastTarget = true;

        MobileButton mobileButton = btnGO.GetComponent<MobileButton>();
        mobileButton.action = action;

        GameObject textGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
        textGO.transform.SetParent(btnGO.transform, false);

        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text text = textGO.GetComponent<Text>();
        text.text = label;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = keyButtonTextColor;
        text.fontSize = keyButtonFontSize;
        if (keyButtonFont == null)
            keyButtonFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.font = keyButtonFont;

        return btnGO;
    }

    private void CreateJoystickRouter(GameObject canvasGO, RectTransform joystickRoot, MobileJoystick moveJoystick, MobileLookJoystick lookJoystick)
    {
        MobileJoystickInputRouter router = canvasGO.AddComponent<MobileJoystickInputRouter>();
        router.joystickRoot = joystickRoot;
        router.moveJoystick = moveJoystick;
        router.lookJoystick = lookJoystick;
        router.freezeFloatingWhenDoorGrabbed = true;
        router.enablePrimaryTouch = true;
    }

    private void CreateActionButtons(RectTransform parent, MobileJoystick moveJoystick, MobileLookJoystick lookJoystick)
    {
        GameObject group = new GameObject("MobileActionButtons", typeof(RectTransform));
        group.transform.SetParent(parent, false);

        RectTransform groupRect = group.GetComponent<RectTransform>();
        groupRect.anchorMin = new Vector2(1f, 0f);
        groupRect.anchorMax = new Vector2(1f, 0f);
        groupRect.pivot = new Vector2(0.5f, 0.5f);
        groupRect.anchoredPosition = Vector2.zero;

        Vector2 arcCenter = rightJoystickAnchoredPos + new Vector2(0f, actionButtonCenterYOffset) + actionButtonsOffset;
        Vector2 leftPos = arcCenter + AngleToOffset(actionButtonAngleLeft, actionButtonRadius);
        Vector2 rightPos = arcCenter + AngleToOffset(actionButtonAngleRight, actionButtonRadius);

        GameObject grabButton = CreateActionButton(groupRect, "Action_A", "A", MobileButton.Action.Grab, leftPos, actionButtonSize);
        GameObject secondaryButton = CreateActionButton(groupRect, "Action_B", secondaryButtonLabel, MobileButton.Action.Secondary,
            rightPos, actionButtonSize);

        GameObject torchButton = null;
        if (createTorchButton)
        {
            Vector2 torchPos = rightJoystickAnchoredPos + torchButtonOffset;
            torchButton = CreateActionButton(groupRect, "Action_Torch", torchButtonLabel, MobileButton.Action.ToggleLight, torchPos, torchButtonSize);
        }

        MobileActionButtons actionButtons = group.AddComponent<MobileActionButtons>();
        actionButtons.primaryButton = grabButton;
        actionButtons.secondaryButton = secondaryButton;
        actionButtons.torchButton = torchButton;
    }

    private GameObject CreateActionButton(RectTransform parent, string name, string label, MobileButton.Action action, Vector2 anchoredPos, float size)
    {
        GameObject btnGO = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(MobileButton));
        btnGO.transform.SetParent(parent, false);

        RectTransform rect = btnGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(size, size);
        rect.anchoredPosition = anchoredPos;

        Image img = btnGO.GetComponent<Image>();
        img.sprite = baseSprite != null ? baseSprite : CreateCircleSprite(128, 0.95f);
        img.color = actionButtonColor;
        img.raycastTarget = true;

        MobileButton mobileButton = btnGO.GetComponent<MobileButton>();
        mobileButton.action = action;

        GameObject textGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
        textGO.transform.SetParent(btnGO.transform, false);

        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text text = textGO.GetComponent<Text>();
        text.text = label;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = actionButtonTextColor;
        text.fontSize = actionButtonFontSize;
        if (keyButtonFont == null)
            keyButtonFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.font = keyButtonFont;

        return btnGO;
    }

    private Vector2 AngleToOffset(float angleDeg, float radius)
    {
        float rad = angleDeg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;
    }

    private Sprite CreateCircleSprite(int size, float radius01)
    {
        int texSize = Mathf.Max(32, size);
        float radius = Mathf.Clamp01(radius01) * (texSize * 0.5f);
        float center = (texSize - 1) * 0.5f;

        Texture2D tex = new Texture2D(texSize, texSize, TextureFormat.ARGB32, false);
        tex.name = "MobileJoystickCircle";
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color clear = new Color(1f, 1f, 1f, 0f);
        Color white = Color.white;

        for (int y = 0; y < texSize; y++)
        {
            for (int x = 0; x < texSize; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt((dx * dx) + (dy * dy));
                tex.SetPixel(x, y, dist <= radius ? white : clear);
            }
        }

        tex.Apply();
        Rect rect = new Rect(0, 0, texSize, texSize);
        return Sprite.Create(tex, rect, new Vector2(0.5f, 0.5f), 100f);
    }
}
