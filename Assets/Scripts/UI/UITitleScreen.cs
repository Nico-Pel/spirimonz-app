using UnityEngine;
using UnityEngine.UI;

public class UITitleScreen : GameBehaviour
{
    public UITitleSaveSlot[] slots;
    public string houseTutoSceneName = "HouseTuto";
    public float mobileTitleScalePortrait = 0.86f;
    public float mobileTitleScaleLandscape = 0.56f;
    public float mobileTitleTopPortrait = 72f;
    public float mobileTitleTopLandscape = -18f;
    public float mobileSavesCenterYPortrait = 500f;
    public float mobileSavesCenterYLandscape = 500f;
    public float mobileMinimumGap = 64f;
    public float mobileExtraTopPadding = 24f;
    public float mobileBottomPadding = 48f;

    private GameManager _gameManager;
    private RectTransform _titleRect;
    private RectTransform _savesRect;
    private Vector3 _titleScale;
    private Vector2 _titleAnchoredPosition;
    private Vector2 _savesAnchoredPosition;
    private bool _layoutCached;
    private Rect _lastSafeArea;
    private Vector2Int _lastScreenSize;
    private UISettingsMenu _settingsMenu;

    private void Awake()
    {
        _gameManager = GameManager.Instance;
        CacheLayoutReferences();

        if (slots != null)
        {
            foreach (UITitleSaveSlot slot in slots)
            {
                if (slot != null)
                    slot.Initialize(this);
            }
        }
    }

    private void Start()
    {
        RefreshSlots();
        ApplyResponsiveLayout();
        EnsureSettingsMenu();

#if UNITY_EDITOR
        if (LoadScenePolicyPreview.ConsumePendingPoliciesRequest())
        {
            UILegalOverlay.Instance.Show(requireAcceptance: true, LegalDocumentType.PrivacyPolicy);
            return;
        }
#endif

        UILegalOverlay.Instance.ShowFirstLaunchGateIfNeeded();
    }

    public void RefreshSlots()
    {
        if (slots == null)
            return;

        if (_gameManager == null)
            _gameManager = GameManager.Instance;

        foreach (UITitleSaveSlot slot in slots)
        {
            if (slot != null)
                slot.Refresh(_gameManager);
        }

        ApplyResponsiveLayout();
    }

    public void OnSlotSelected(UITitleSaveSlot slot)
    {
        if (slot == null)
            return;

        if (_gameManager == null)
            _gameManager = GameManager.Instance;

        if (_gameManager == null)
            return;

        if (slot.hasSave)
        {
            _gameManager.UseSaveSlot(slot.slotIndex, createIfMissing: true, temporary: false);
            _gameManager.LoadWorldFromCurrentSave();
        }
        else
        {
            _gameManager.UseSaveSlot(slot.slotIndex, createIfMissing: true, temporary: false);
            _gameManager.SetNextHouseSceneMode(GameManager.HouseSceneMode.Tutorial);
            _gameManager.LoadScene(houseTutoSceneName);
        }
    }

    private void CacheLayoutReferences()
    {
        if (_layoutCached)
            return;

        Transform root = transform.parent != null ? transform.parent : transform;
        Transform titleTransform = root.Find("iTitle");
        if (titleTransform != null)
            _titleRect = titleTransform as RectTransform;

        Transform savesTransform = root.Find("Saves");
        if (savesTransform != null)
            _savesRect = savesTransform as RectTransform;

        if (_titleRect != null)
        {
            _titleScale = _titleRect.localScale;
            _titleAnchoredPosition = _titleRect.anchoredPosition;
        }
        if (_savesRect != null)
            _savesAnchoredPosition = _savesRect.anchoredPosition;

        _layoutCached = true;
    }

    private void OnRectTransformDimensionsChange()
    {
        ApplyResponsiveLayout();
    }

    private void Update()
    {
        if (_lastSafeArea != Screen.safeArea ||
            _lastScreenSize.x != Screen.width ||
            _lastScreenSize.y != Screen.height)
        {
            ApplyResponsiveLayout();
        }

        HandleSettingsToggle();
    }

    private void HandleSettingsToggle()
    {
        bool mobileMode = Application.isMobilePlatform || (_gameManager != null && _gameManager.mobileControlsEnabled);
        bool toggleDown = (!mobileMode && Input.GetKeyDown(KeyCode.Escape)) || MobileInput.ExitMenusDown;
        if (!toggleDown)
            return;

        ToggleSettingsMenu();
    }

    public void ToggleSettingsMenu()
    {
        UISettingsMenu settingsMenu = EnsureSettingsMenu();
        if (settingsMenu == null)
            return;

        settingsMenu.Toggle();
    }

    private UISettingsMenu EnsureSettingsMenu()
    {
        if (_settingsMenu != null)
            return _settingsMenu;

        Transform root = transform.parent != null ? transform.parent : transform;
        _settingsMenu = root.GetComponentInChildren<UISettingsMenu>(true);
        if (_settingsMenu != null)
            return _settingsMenu;

        GameObject go = new GameObject("UISettingsMenu", typeof(RectTransform));
        go.transform.SetParent(root, false);
        _settingsMenu = go.AddComponent<UISettingsMenu>();
        return _settingsMenu;
    }

    private void ApplyResponsiveLayout()
    {
        CacheLayoutReferences();

        if (_titleRect == null || _savesRect == null)
            return;

        bool useMobileLayout = Application.isMobilePlatform || (_gameManager != null && _gameManager.mobileControlsEnabled);
        _titleRect.localScale = _titleScale;
        _titleRect.anchoredPosition = _titleAnchoredPosition;
        _savesRect.anchoredPosition = _savesAnchoredPosition;

        _lastSafeArea = Screen.safeArea;
        _lastScreenSize = new Vector2Int(Screen.width, Screen.height);

        if (!useMobileLayout)
            return;

        float aspectRatio = Screen.height > 0 ? Screen.width / (float)Screen.height : 1f;
        float landscapeFactor = Mathf.InverseLerp(0.85f, 1.6f, aspectRatio);
        float targetScale = Mathf.Lerp(mobileTitleScalePortrait, mobileTitleScaleLandscape, landscapeFactor);
        float targetTitleTop = Mathf.Lerp(mobileTitleTopPortrait, mobileTitleTopLandscape, landscapeFactor);
        float targetSavesY = Mathf.Lerp(mobileSavesCenterYPortrait, mobileSavesCenterYLandscape, landscapeFactor);

        _titleRect.localScale = _titleScale * targetScale;
        _titleRect.anchoredPosition = new Vector2(_titleAnchoredPosition.x, targetTitleTop);
        _savesRect.anchoredPosition = new Vector2(_savesAnchoredPosition.x, targetSavesY);

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_titleRect);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_savesRect);

        Canvas canvas = _savesRect.GetComponentInParent<Canvas>();
        float canvasScale = canvas != null ? Mathf.Max(0.0001f, canvas.scaleFactor) : 1f;

        float titleBottom = GetWorldBottom(_titleRect) - (mobileExtraTopPadding * canvasScale);
        float savesTop = GetWorldTop(_savesRect);
        float requiredGap = mobileMinimumGap * canvasScale;
        float overlap = (titleBottom + requiredGap) - savesTop;
        if (overlap > 0f)
        {
            Vector2 anchoredPosition = _savesRect.anchoredPosition;
            anchoredPosition.y -= overlap / canvasScale;
            _savesRect.anchoredPosition = anchoredPosition;
            LayoutRebuilder.ForceRebuildLayoutImmediate(_savesRect);
        }

        float safeBottom = Screen.safeArea.yMin + (mobileBottomPadding * canvasScale);
        float savesBottom = GetWorldBottom(_savesRect);
        if (savesBottom < safeBottom)
        {
            Vector2 anchoredPosition = _savesRect.anchoredPosition;
            anchoredPosition.y += (safeBottom - savesBottom) / canvasScale;
            _savesRect.anchoredPosition = anchoredPosition;
        }
    }

    private static float GetWorldTop(RectTransform rect)
    {
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        return corners[1].y;
    }

    private static float GetWorldBottom(RectTransform rect)
    {
        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        return corners[0].y;
    }
}
