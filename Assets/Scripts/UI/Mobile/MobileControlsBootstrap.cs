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

    private static bool _created;

    public static void EnsureExists()
    {
        if (_created || FindObjectOfType<MobileControlsBootstrap>() != null || FindObjectOfType<MobileControlsRoot>() != null)
            return;

        GameObject go = new GameObject("MobileControlsBootstrap");
        DontDestroyOnLoad(go);
        go.AddComponent<MobileControlsBootstrap>();
        _created = true;
    }

    private void Awake()
    {
        if (FindObjectOfType<MobileControlsRoot>() != null)
        {
            _created = true;
            return;
        }

        CreateEventSystemIfMissing();
        CreateCanvasAndJoysticks();
        _created = true;
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

        CreateMoveJoystick(joysticksRoot, baseSprite, handleSprite);
        CreateLookJoystick(joysticksRoot, baseSprite, handleSprite);

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

    private void CreateMoveJoystick(Transform parent, Sprite baseSpriteToUse, Sprite handleSpriteToUse)
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
        baseImg.raycastTarget = true;

        RectTransform handle = CreateHandle(baseRect, handleSpriteToUse);

        MobileJoystick joystick = baseGO.GetComponent<MobileJoystick>();
        joystick.handle = handle;
        joystick.handleRange = handleRange;
        joystick.deadZone = deadZone;
    }

    private void CreateLookJoystick(Transform parent, Sprite baseSpriteToUse, Sprite handleSpriteToUse)
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
        baseImg.raycastTarget = true;

        RectTransform handle = CreateHandle(baseRect, handleSpriteToUse);

        MobileLookJoystick joystick = baseGO.GetComponent<MobileLookJoystick>();
        joystick.handle = handle;
        joystick.handleRange = handleRange;
        joystick.deadZone = deadZone;
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
            ("TAB", MobileButton.Action.OpenTeamMenu),
            ("E", MobileButton.Action.Grab),
            ("1", MobileButton.Action.Inventory1),
            ("2", MobileButton.Action.Inventory2),
            ("3", MobileButton.Action.Inventory3),
            ("4", MobileButton.Action.Inventory4),
            ("5", MobileButton.Action.Inventory5),
            ("6", MobileButton.Action.Inventory6),
        };

        float totalHeight = (keyButtonSize.y * keys.Length) + (keyButtonSpacing * (keys.Length - 1));
        groupRect.sizeDelta = new Vector2(keyButtonSize.x, totalHeight);

        for (int i = 0; i < keys.Length; i++)
        {
            CreateKeyButton(groupRect, keys[i].label, keys[i].action, 0, i);
        }
    }

    private void CreateKeyButton(RectTransform parent, string label, MobileButton.Action action, int col, int row)
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
