using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UISettingsMenu : GameBehaviour
{
    private const float MinVolumeMultiplier = 0f;
    private const float MaxVolumeMultiplier = 1.5f;
    private const float MinSensitivityMultiplier = 0.5f;
    private const float MaxSensitivityMultiplier = 1.5f;
    private const int AllTextSize = 54;
    private const float BindingButtonMinWidth = 220f;
    private const float BindingButtonHeight = 90f;

    public CanvasGroup canvasGroup;
    public RectTransform panelRoot;
    public Slider ambientSlider;
    public Slider sfxSlider;
    public Slider tpsSensitivitySlider;
    public Slider fpsSensitivitySlider;
    public Text ambientValue;
    public Text sfxValue;
    public Text tpsValue;
    public Text fpsValue;
    public Dropdown languageDropdown;
    public GameObject keybindingsRoot;
    public GameObject keybindingsHeader;
    public GameObject keybindingsContainer;
    public ScrollRect scrollRect;
    public ScrollRect keybindingsScrollRect;

    private bool _built;
    private bool _bindingsBuilt;
    private Font _font;
    private InputManager _input;
    private SoundManager _sound;
    private UIManager _uiManager;

    private bool _isOpen;
    private bool _waitingForKey;
    private BindingEntry _waitingEntry;
    private bool _waitingSecondary;
    private int _captureStartFrame = -1;

    private readonly Color _bindingNormalColor = new Color(1f, 1f, 1f, 0.15f);
    private readonly Color _bindingSelectedColor = new Color(0.3f, 0.8f, 1f, 0.35f);
    private readonly Color _accentColor = new Color(0.3f, 0.8f, 1f, 1f);

    private readonly List<BindingEntry> _bindings = new List<BindingEntry>();

    public bool IsOpen => _isOpen;
    public bool IsCapturingKey => _waitingForKey;

    private class BindingEntry
    {
        public string label;
        public Func<KeyCode> getPrimary;
        public Action<KeyCode> setPrimary;
        public Func<KeyCode> getSecondary;
        public Action<KeyCode> setSecondary;
        public Text primaryText;
        public Text secondaryText;
        public Image primaryImage;
        public Image secondaryImage;
    }

    public static UISettingsMenu EnsureExists(UIGame uiGame)
    {
        if (uiGame == null)
            return null;

        UISettingsMenu existing = uiGame.GetComponentInChildren<UISettingsMenu>(true);
        if (existing != null)
            return existing;

        GameObject go = new GameObject("UISettingsMenu", typeof(RectTransform));
        go.transform.SetParent(uiGame.transform, false);
        UISettingsMenu menu = go.AddComponent<UISettingsMenu>();
        menu.BuildUI();
        return menu;
    }

    private void Awake()
    {
        if (panelRoot == null)
            BuildUI();

        _uiManager = GetComponentInParent<UIManager>();
        SetVisible(false);
    }

    private void Update()
    {
        if (!_isOpen)
            return;

        bool showBindings = !Application.isMobilePlatform && !MobileInput.Enabled;
        if (keybindingsContainer != null)
            keybindingsContainer.SetActive(showBindings);
        if (keybindingsRoot != null)
            keybindingsRoot.SetActive(showBindings);
        if (keybindingsHeader != null)
            keybindingsHeader.SetActive(showBindings);

        if (_waitingForKey)
            CaptureKey();
    }

    public void Toggle()
    {
        SetVisible(!_isOpen);
    }

    public void SetVisible(bool visible)
    {
        _isOpen = visible;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }

        if (_uiManager != null)
        {
            if (visible) _uiManager.AddShowCursor();
            else _uiManager.RemoveShowCursor();
        }

        if (visible)
            Refresh();
        else
            CancelCapture();

        if (visible && Player.Instance != null)
            Player.Instance.LockControls(true);

        if (!visible && _uiManager != null && !_uiManager.IsCursorActive && Player.Instance != null)
            Player.Instance.LockControls(false);
    }

    private void Refresh()
    {
        _input = InputManager.Instance;
        _sound = SoundManager.Instance;

        if (_sound != null)
        {
            SetSliderValue(ambientSlider, MultiplierToVolumeSlider(_sound.ambientVolumeMultiplier));
            SetSliderValue(sfxSlider, MultiplierToVolumeSlider(_sound.sfxVolumeMultiplier));
        }

        if (_input != null)
        {
            SetSliderValue(tpsSensitivitySlider, MultiplierToSensitivitySlider(_input.tpsLookSensitivityMultiplier));
            SetSliderValue(fpsSensitivitySlider, MultiplierToSensitivitySlider(_input.fpsLookSensitivityMultiplier));
        }

        UpdateValueTexts();
        RefreshBindingTexts();
        RefreshLanguageDropdown();

        if (_input != null && keybindingsRoot != null)
        {
            int definitionCount = _input.GetBindingDefinitions().Count;
            if (!_bindingsBuilt || _bindings.Count != definitionCount)
            {
                BuildBindingsUI(keybindingsRoot.transform, _font);
                _bindingsBuilt = _bindings.Count > 0;
            }
        }
    }

    private void UpdateValueTexts()
    {
        if (ambientValue != null && _sound != null)
            ambientValue.text = $"{_sound.ambientVolumeMultiplier:0.00}x";
        if (sfxValue != null && _sound != null)
            sfxValue.text = $"{_sound.sfxVolumeMultiplier:0.00}x";
        if (tpsValue != null && _input != null)
            tpsValue.text = $"{_input.tpsLookSensitivityMultiplier:0.00}x";
        if (fpsValue != null && _input != null)
            fpsValue.text = $"{_input.fpsLookSensitivityMultiplier:0.00}x";
    }

    private void RefreshBindingTexts()
    {
        if (_input == null)
            return;

        foreach (BindingEntry entry in _bindings)
        {
            if (entry.primaryText != null)
                entry.primaryText.text = entry.getPrimary?.Invoke().ToString() ?? "-";

            if (entry.secondaryText != null)
            {
                KeyCode secondary = entry.getSecondary != null ? entry.getSecondary() : KeyCode.None;
                entry.secondaryText.text = secondary == KeyCode.None ? "-" : secondary.ToString();
            }
        }
    }

    private void CaptureKey()
    {
        if (Time.frameCount == _captureStartFrame)
            return;

        if (!Input.anyKeyDown)
            return;

        if (!TryGetPressedKey(out KeyCode key))
            return;

        if (_waitingEntry != null)
        {
            if (key == KeyCode.Backspace || key == KeyCode.Delete)
                key = KeyCode.None;

            if (_waitingSecondary)
                _waitingEntry.setSecondary?.Invoke(key);
            else
                _waitingEntry.setPrimary?.Invoke(key);
        }

        CancelCapture();
        RefreshBindingTexts();
        GameManager.Instance?.SaveInputBindings();
    }

    private void CancelCapture()
    {
        SetEntrySelected(_waitingEntry, _waitingSecondary, false);

        if (_waitingEntry != null)
        {
            if (_waitingEntry.primaryText != null && !_waitingSecondary)
                _waitingEntry.primaryText.text = _waitingEntry.getPrimary?.Invoke().ToString() ?? "-";
            if (_waitingEntry.secondaryText != null && _waitingSecondary)
            {
                KeyCode secondary = _waitingEntry.getSecondary != null ? _waitingEntry.getSecondary() : KeyCode.None;
                _waitingEntry.secondaryText.text = secondary == KeyCode.None ? "-" : secondary.ToString();
            }
        }

        _waitingForKey = false;
        _waitingEntry = null;
        _waitingSecondary = false;
        _captureStartFrame = -1;
    }

    private bool TryGetPressedKey(out KeyCode key)
    {
        foreach (KeyCode code in Enum.GetValues(typeof(KeyCode)))
        {
            if (Input.GetKeyDown(code))
            {
                key = code;
                return true;
            }
        }
        key = KeyCode.None;
        return false;
    }

    private void BuildUI()
    {
        if (_built)
            return;
        _built = true;

        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _input = InputManager.Instance;

        GameObject root = gameObject;
        panelRoot = root.GetComponent<RectTransform>();
        if (panelRoot == null)
            panelRoot = root.AddComponent<RectTransform>();
        panelRoot.anchorMin = new Vector2(0.12f, 0.08f);
        panelRoot.anchorMax = new Vector2(0.88f, 0.92f);
        panelRoot.pivot = new Vector2(0.5f, 0.5f);
        panelRoot.offsetMin = Vector2.zero;
        panelRoot.offsetMax = Vector2.zero;

        Image bg = root.GetComponent<Image>();
        if (bg == null)
            bg = root.AddComponent<Image>();
        bg.color = new Color(0.06f, 0.09f, 0.14f, 0.96f);

        canvasGroup = root.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = root.AddComponent<CanvasGroup>();

        VerticalLayoutGroup layout = root.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
            layout = root.AddComponent<VerticalLayoutGroup>();
        if (layout == null)
            return;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.spacing = 20f;
        layout.padding = new RectOffset(32, 32, 28, 28);

        CreateHeader(root.transform, "Settings", _font, AllTextSize);

        GameObject accentBar = new GameObject("AccentBar", typeof(RectTransform), typeof(Image));
        accentBar.transform.SetParent(root.transform, false);
        Image accentImage = accentBar.GetComponent<Image>();
        accentImage.color = new Color(_accentColor.r, _accentColor.g, _accentColor.b, 0.85f);
        LayoutElement accentLayout = accentBar.AddComponent<LayoutElement>();
        accentLayout.preferredHeight = 6f;
        accentLayout.minHeight = 6f;

        GameObject scrollGO = new GameObject("ScrollView", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollGO.transform.SetParent(root.transform, false);
        LayoutElement scrollLayout = scrollGO.AddComponent<LayoutElement>();
        scrollLayout.flexibleHeight = 1f;
        scrollLayout.minHeight = 0f;
        Image scrollBg = scrollGO.GetComponent<Image>();
        scrollBg.color = new Color(1f, 1f, 1f, 0.04f);

        scrollRect = scrollGO.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 24f;

        GameObject viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewportGO.transform.SetParent(scrollGO.transform, false);
        Image viewportImage = viewportGO.GetComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.02f);
        Mask mask = viewportGO.GetComponent<Mask>();
        mask.showMaskGraphic = false;

        RectTransform viewportRect = viewportGO.GetComponent<RectTransform>();
        viewportRect.anchorMin = new Vector2(0f, 0f);
        viewportRect.anchorMax = new Vector2(1f, 1f);
        viewportRect.offsetMin = new Vector2(10f, 10f);
        viewportRect.offsetMax = new Vector2(-32f, -10f);

        GameObject contentGO = new GameObject("Content", typeof(RectTransform));
        contentGO.transform.SetParent(viewportGO.transform, false);
        RectTransform contentRect = contentGO.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = Vector2.zero;

        VerticalLayoutGroup contentLayout = contentGO.AddComponent<VerticalLayoutGroup>();
        contentLayout.childAlignment = TextAnchor.UpperCenter;
        contentLayout.childControlHeight = true;
        contentLayout.childControlWidth = true;
        contentLayout.childForceExpandHeight = false;
        contentLayout.childForceExpandWidth = true;
        contentLayout.spacing = 16f;
        contentLayout.padding = new RectOffset(12, 12, 14, 18);

        ContentSizeFitter contentFitter = contentGO.AddComponent<ContentSizeFitter>();
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        GameObject scrollbarGO = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        scrollbarGO.transform.SetParent(scrollGO.transform, false);
        RectTransform sbRect = scrollbarGO.GetComponent<RectTransform>();
        sbRect.anchorMin = new Vector2(1f, 0f);
        sbRect.anchorMax = new Vector2(1f, 1f);
        sbRect.pivot = new Vector2(1f, 1f);
        sbRect.sizeDelta = new Vector2(20f, 0f);
        sbRect.anchoredPosition = new Vector2(-6f, 0f);
        Image sbBg = scrollbarGO.GetComponent<Image>();
        sbBg.color = new Color(1f, 1f, 1f, 0.32f);
        Scrollbar scrollbar = scrollbarGO.GetComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;

        GameObject handleGO = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleGO.transform.SetParent(scrollbarGO.transform, false);
        Image handleImg = handleGO.GetComponent<Image>();
        handleImg.color = new Color(0.3f, 0.8f, 1f, 0.95f);
        RectTransform handleRect = handleGO.GetComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0f, 0f);
        handleRect.anchorMax = new Vector2(1f, 1f);
        handleRect.offsetMin = Vector2.zero;
        handleRect.offsetMax = Vector2.zero;
        scrollbar.handleRect = handleRect;
        scrollbar.targetGraphic = handleImg;

        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;
        scrollRect.verticalScrollbar = scrollbar;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;

        CreateHeader(contentGO.transform, "Audio", _font, AllTextSize);
        ambientSlider = CreateSliderRow(contentGO.transform, "Ambient Volume", _font, out ambientValue);
        sfxSlider = CreateSliderRow(contentGO.transform, "SFX Volume", _font, out sfxValue);

        CreateHeader(contentGO.transform, "Camera", _font, AllTextSize);
        tpsSensitivitySlider = CreateSliderRow(contentGO.transform, "TPS Camera Sensitivity", _font, out tpsValue);
        fpsSensitivitySlider = CreateSliderRow(contentGO.transform, "FPS Camera Sensitivity", _font, out fpsValue);

        CreateHeader(contentGO.transform, "Language", _font, AllTextSize);
        languageDropdown = CreateLanguageDropdownRow(contentGO.transform, "Language", _font);

        keybindingsHeader = CreateHeader(contentGO.transform, "Key Bindings", _font, AllTextSize);
        GameObject keyRoot = new GameObject("KeybindingsRoot", typeof(RectTransform));
        keyRoot.transform.SetParent(contentGO.transform, false);
        keybindingsContainer = keyRoot;

        VerticalLayoutGroup keyLayout = keyRoot.AddComponent<VerticalLayoutGroup>();
        keyLayout.childControlHeight = true;
        keyLayout.childControlWidth = true;
        keyLayout.childForceExpandHeight = false;
        keyLayout.childForceExpandWidth = true;
        keyLayout.spacing = 22f;
        keyLayout.padding = new RectOffset(10, 10, 14, 18);

        ContentSizeFitter keyFitter = keyRoot.AddComponent<ContentSizeFitter>();
        keyFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        keyFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        keybindingsRoot = keyRoot;
        keybindingsScrollRect = null;
        BuildBindingsUI(keybindingsRoot.transform, _font);
        _bindingsBuilt = _bindings.Count > 0;

        HookSliders();
        HookLanguageDropdown();
    }

    private GameObject CreateHeader(Transform parent, string title, Font font, int size = 24)
    {
        GameObject header = new GameObject($"Header_{title}", typeof(RectTransform));
        header.transform.SetParent(parent, false);
        Text text = header.AddComponent<Text>();
        text.text = title;
        text.font = font;
        text.fontSize = size;
        text.color = _accentColor;
        text.alignment = TextAnchor.MiddleCenter;
        LayoutElement layout = header.AddComponent<LayoutElement>();
        layout.preferredHeight = size + 14f;
        layout.flexibleHeight = 0f;
        return header;
    }

    private Slider CreateSliderRow(Transform parent, string label, Font font, out Text valueText)
    {
        GameObject row = new GameObject($"Row_{label}", typeof(RectTransform));
        row.transform.SetParent(parent, false);
        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.spacing = 16f;

        float rowHeight = Mathf.Max(AllTextSize + 20f, 80f);
        LayoutElement rowLayout = row.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = rowHeight;
        rowLayout.flexibleHeight = 0f;

        GameObject labelGO = new GameObject("Label", typeof(RectTransform));
        labelGO.transform.SetParent(row.transform, false);
        Text labelText = labelGO.AddComponent<Text>();
        labelText.text = label;
        labelText.font = font;
        labelText.fontSize = AllTextSize;
        labelText.color = Color.white;
        labelText.alignment = TextAnchor.MiddleLeft;
        labelText.horizontalOverflow = HorizontalWrapMode.Wrap;
        labelText.verticalOverflow = VerticalWrapMode.Overflow;

        LayoutElement labelLayout = labelGO.AddComponent<LayoutElement>();
        labelLayout.minWidth = 320f;
        labelLayout.preferredWidth = 420f;
        labelLayout.flexibleWidth = 1f;

        GameObject sliderGO = new GameObject("Slider", typeof(RectTransform));
        sliderGO.transform.SetParent(row.transform, false);
        Slider slider = sliderGO.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.5f;
        slider.transition = Selectable.Transition.ColorTint;

        RectTransform sliderRect = sliderGO.GetComponent<RectTransform>();
        sliderRect.sizeDelta = new Vector2(0f, 60f);
        LayoutElement sliderLayout = sliderGO.AddComponent<LayoutElement>();
        sliderLayout.minWidth = 280f;
        sliderLayout.preferredWidth = 360f;
        sliderLayout.flexibleWidth = 1f;
        sliderLayout.minHeight = 60f;
        sliderLayout.preferredHeight = 60f;

        GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(sliderGO.transform, false);
        Image bgImage = background.GetComponent<Image>();
        bgImage.color = new Color(1f, 1f, 1f, 0.32f);
        RectTransform bgRect = background.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderGO.transform, false);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.42f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.58f);
        fillAreaRect.offsetMin = new Vector2(14f, 0f);
        fillAreaRect.offsetMax = new Vector2(-14f, 0f);

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        Image fillImage = fill.GetComponent<Image>();
        fillImage.color = new Color(0.3f, 0.8f, 1f, 0.95f);
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        slider.fillRect = fillRect;

        GameObject handleSlideArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleSlideArea.transform.SetParent(sliderGO.transform, false);
        RectTransform handleAreaRect = handleSlideArea.GetComponent<RectTransform>();
        handleAreaRect.anchorMin = new Vector2(0f, 0f);
        handleAreaRect.anchorMax = new Vector2(1f, 1f);
        handleAreaRect.offsetMin = new Vector2(10f, 0f);
        handleAreaRect.offsetMax = new Vector2(-10f, 0f);

        GameObject handleGO = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handleGO.transform.SetParent(handleSlideArea.transform, false);
        Image handleImage = handleGO.GetComponent<Image>();
        handleImage.color = new Color(1f, 1f, 1f, 0.95f);
        RectTransform handleRect = handleGO.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(36f, 36f);
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;
        slider.direction = Slider.Direction.LeftToRight;

        GameObject valueGO = new GameObject("Value", typeof(RectTransform));
        valueGO.transform.SetParent(row.transform, false);
        valueText = valueGO.AddComponent<Text>();
        valueText.text = "1.00x";
        valueText.font = font;
        valueText.fontSize = AllTextSize;
        valueText.color = Color.white;
        valueText.alignment = TextAnchor.MiddleCenter;
        LayoutElement valueLayout = valueGO.AddComponent<LayoutElement>();
        valueLayout.preferredWidth = 200f;
        valueLayout.minWidth = 160f;

        return slider;
    }

    private Dropdown CreateLanguageDropdownRow(Transform parent, string label, Font font)
    {
        GameObject row = new GameObject($"Row_{label}", typeof(RectTransform));
        row.transform.SetParent(parent, false);
        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.spacing = 16f;

        float rowHeight = Mathf.Max(AllTextSize + 20f, 80f);
        LayoutElement rowLayout = row.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = rowHeight;
        rowLayout.flexibleHeight = 0f;

        GameObject labelGO = new GameObject("Label", typeof(RectTransform));
        labelGO.transform.SetParent(row.transform, false);
        Text labelText = labelGO.AddComponent<Text>();
        labelText.text = label;
        labelText.font = font;
        labelText.fontSize = AllTextSize;
        labelText.color = Color.white;
        labelText.alignment = TextAnchor.MiddleLeft;
        labelText.horizontalOverflow = HorizontalWrapMode.Wrap;
        labelText.verticalOverflow = VerticalWrapMode.Overflow;
        LayoutElement labelLayout = labelGO.AddComponent<LayoutElement>();
        labelLayout.minWidth = 320f;
        labelLayout.preferredWidth = 420f;
        labelLayout.flexibleWidth = 1f;

        Dropdown dropdown = CreateDropdown(row.transform, font);
        return dropdown;
    }

    private Dropdown CreateDropdown(Transform parent, Font font)
    {
        GameObject dropdownGO = new GameObject("Dropdown", typeof(RectTransform), typeof(Image), typeof(Dropdown));
        dropdownGO.transform.SetParent(parent, false);
        Image bg = dropdownGO.GetComponent<Image>();
        bg.color = new Color(1f, 1f, 1f, 0.15f);

        RectTransform dropdownRect = dropdownGO.GetComponent<RectTransform>();
        dropdownRect.sizeDelta = new Vector2(0f, BindingButtonHeight);
        LayoutElement dropdownLayout = dropdownGO.AddComponent<LayoutElement>();
        dropdownLayout.minWidth = BindingButtonMinWidth * 2f;
        dropdownLayout.preferredWidth = 0f;
        dropdownLayout.flexibleWidth = 1f;
        dropdownLayout.minHeight = BindingButtonHeight;
        dropdownLayout.preferredHeight = BindingButtonHeight;

        Dropdown dropdown = dropdownGO.GetComponent<Dropdown>();
        dropdown.targetGraphic = bg;

        GameObject labelGO = new GameObject("Label", typeof(RectTransform));
        labelGO.transform.SetParent(dropdownGO.transform, false);
        Text label = labelGO.AddComponent<Text>();
        label.font = font;
        label.fontSize = AllTextSize;
        label.color = Color.white;
        label.alignment = TextAnchor.MiddleLeft;
        RectTransform labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.offsetMin = new Vector2(18f, 0f);
        labelRect.offsetMax = new Vector2(-60f, 0f);
        dropdown.captionText = label;

        GameObject arrowGO = new GameObject("Arrow", typeof(RectTransform), typeof(Image));
        arrowGO.transform.SetParent(dropdownGO.transform, false);
        Image arrowImg = arrowGO.GetComponent<Image>();
        arrowImg.color = _accentColor;
        arrowImg.sprite = CreateTriangleSprite();
        RectTransform arrowRect = arrowGO.GetComponent<RectTransform>();
        arrowRect.anchorMin = new Vector2(1f, 0.5f);
        arrowRect.anchorMax = new Vector2(1f, 0.5f);
        arrowRect.sizeDelta = new Vector2(24f, 18f);
        arrowRect.anchoredPosition = new Vector2(-24f, 0f);

        GameObject templateGO = new GameObject("Template", typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        templateGO.transform.SetParent(dropdownGO.transform, false);
        templateGO.SetActive(false);
        Image templateBg = templateGO.GetComponent<Image>();
        templateBg.color = new Color(0.07f, 0.1f, 0.16f, 0.98f);

        RectTransform templateRect = templateGO.GetComponent<RectTransform>();
        templateRect.anchorMin = new Vector2(0f, 0f);
        templateRect.anchorMax = new Vector2(1f, 0f);
        templateRect.pivot = new Vector2(0.5f, 1f);
        templateRect.anchoredPosition = new Vector2(0f, -8f);
        templateRect.sizeDelta = new Vector2(0f, 220f);

        ScrollRect templateScroll = templateGO.GetComponent<ScrollRect>();
        templateScroll.horizontal = false;
        templateScroll.vertical = true;
        templateScroll.movementType = ScrollRect.MovementType.Clamped;
        templateScroll.scrollSensitivity = 24f;

        GameObject viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewportGO.transform.SetParent(templateGO.transform, false);
        Image viewportImg = viewportGO.GetComponent<Image>();
        viewportImg.color = new Color(1f, 1f, 1f, 0.02f);
        Mask viewportMask = viewportGO.GetComponent<Mask>();
        viewportMask.showMaskGraphic = false;
        RectTransform viewportRect = viewportGO.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(6f, 6f);
        viewportRect.offsetMax = new Vector2(-6f, -6f);

        GameObject contentGO = new GameObject("Content", typeof(RectTransform));
        contentGO.transform.SetParent(viewportGO.transform, false);
        RectTransform contentRect = contentGO.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = Vector2.zero;

        VerticalLayoutGroup contentLayout = contentGO.AddComponent<VerticalLayoutGroup>();
        contentLayout.childControlHeight = true;
        contentLayout.childControlWidth = true;
        contentLayout.childForceExpandHeight = false;
        contentLayout.childForceExpandWidth = true;
        contentLayout.spacing = 6f;
        contentLayout.padding = new RectOffset(4, 4, 4, 4);

        ContentSizeFitter contentFitter = contentGO.AddComponent<ContentSizeFitter>();
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        templateScroll.viewport = viewportRect;
        templateScroll.content = contentRect;

        GameObject itemGO = new GameObject("Item", typeof(RectTransform), typeof(Toggle));
        itemGO.transform.SetParent(contentGO.transform, false);
        Toggle itemToggle = itemGO.GetComponent<Toggle>();
        itemToggle.isOn = true;
        RectTransform itemRect = itemGO.GetComponent<RectTransform>();
        itemRect.sizeDelta = new Vector2(0f, BindingButtonHeight);
        LayoutElement itemLayout = itemGO.AddComponent<LayoutElement>();
        itemLayout.minHeight = BindingButtonHeight;
        itemLayout.preferredHeight = BindingButtonHeight;
        itemLayout.flexibleWidth = 1f;

        GameObject itemBgGO = new GameObject("Item Background", typeof(RectTransform), typeof(Image));
        itemBgGO.transform.SetParent(itemGO.transform, false);
        Image itemBg = itemBgGO.GetComponent<Image>();
        itemBg.color = new Color(1f, 1f, 1f, 0.08f);
        RectTransform itemBgRect = itemBgGO.GetComponent<RectTransform>();
        itemBgRect.anchorMin = Vector2.zero;
        itemBgRect.anchorMax = Vector2.one;
        itemBgRect.offsetMin = Vector2.zero;
        itemBgRect.offsetMax = Vector2.zero;

        GameObject itemCheckGO = new GameObject("Item Checkmark", typeof(RectTransform), typeof(Image));
        itemCheckGO.transform.SetParent(itemGO.transform, false);
        Image itemCheck = itemCheckGO.GetComponent<Image>();
        itemCheck.color = _accentColor;
        RectTransform itemCheckRect = itemCheckGO.GetComponent<RectTransform>();
        itemCheckRect.anchorMin = new Vector2(0f, 0.5f);
        itemCheckRect.anchorMax = new Vector2(0f, 0.5f);
        itemCheckRect.sizeDelta = new Vector2(18f, 18f);
        itemCheckRect.anchoredPosition = new Vector2(16f, 0f);

        GameObject itemLabelGO = new GameObject("Item Label", typeof(RectTransform));
        itemLabelGO.transform.SetParent(itemGO.transform, false);
        Text itemLabel = itemLabelGO.AddComponent<Text>();
        itemLabel.font = font;
        itemLabel.fontSize = AllTextSize;
        itemLabel.color = Color.white;
        itemLabel.alignment = TextAnchor.MiddleLeft;
        RectTransform itemLabelRect = itemLabelGO.GetComponent<RectTransform>();
        itemLabelRect.anchorMin = new Vector2(0f, 0f);
        itemLabelRect.anchorMax = new Vector2(1f, 1f);
        itemLabelRect.offsetMin = new Vector2(44f, 0f);
        itemLabelRect.offsetMax = new Vector2(-10f, 0f);

        itemToggle.targetGraphic = itemBg;
        itemToggle.graphic = itemCheck;

        dropdown.template = templateRect;
        dropdown.itemText = itemLabel;
        dropdown.captionText = label;

        return dropdown;
    }

    private Sprite CreateTriangleSprite()
    {
        Texture2D tex = new Texture2D(32, 32, TextureFormat.ARGB32, false);
        tex.SetPixels32(new Color32[32 * 32]);
        for (int y = 0; y < 18; y++)
        {
            int start = 16 - y;
            int end = 16 + y;
            for (int x = start; x <= end; x++)
                tex.SetPixel(x, y + 7, Color.white);
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 32f);
    }

    private void BuildBindingsUI(Transform parent, Font font)
    {
        _bindings.Clear();

        if (_input == null)
            _input = InputManager.Instance;
        if (_input == null)
            return;

        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);

        List<InputManager.BindingDefinition> definitions = _input.GetBindingDefinitions();
        foreach (var def in definitions)
        {
            AddBinding(parent, font, def.label, def.getPrimary, def.setPrimary, def.getSecondary, def.setSecondary);
        }
    }

    private void AddBinding(Transform parent, Font font, string label,
        Func<KeyCode> getPrimary, Action<KeyCode> setPrimary,
        Func<KeyCode> getSecondary, Action<KeyCode> setSecondary)
    {
        GameObject row = new GameObject($"Bind_{label}", typeof(RectTransform));
        row.transform.SetParent(parent, false);
        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;
        layout.spacing = 14f;
        LayoutElement rowLayout = row.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = Mathf.Max(BindingButtonHeight + 12f, AllTextSize + 20f);
        rowLayout.flexibleHeight = 0f;

        GameObject labelGO = new GameObject("Label", typeof(RectTransform));
        labelGO.transform.SetParent(row.transform, false);
        Text labelText = labelGO.AddComponent<Text>();
        labelText.text = label;
        labelText.font = font;
        labelText.fontSize = AllTextSize;
        labelText.color = Color.white;
        labelText.alignment = TextAnchor.MiddleLeft;
        labelText.horizontalOverflow = HorizontalWrapMode.Wrap;
        labelText.verticalOverflow = VerticalWrapMode.Overflow;
        LayoutElement labelLayout = labelGO.AddComponent<LayoutElement>();
        labelLayout.minWidth = 300f;
        labelLayout.preferredWidth = 320f;
        labelLayout.flexibleWidth = 0f;

        Button primaryButton = CreateKeyButton(row.transform, font, out Text primaryText, out Image primaryImage);
        Button secondaryButton = CreateKeyButton(row.transform, font, out Text secondaryText, out Image secondaryImage);

        BindingEntry entry = new BindingEntry
        {
            label = label,
            getPrimary = getPrimary,
            setPrimary = setPrimary,
            getSecondary = getSecondary,
            setSecondary = setSecondary,
            primaryText = primaryText,
            secondaryText = secondaryText,
            primaryImage = primaryImage,
            secondaryImage = secondaryImage
        };

        primaryButton.onClick.AddListener(() => StartCapture(entry, false));
        secondaryButton.onClick.AddListener(() => StartCapture(entry, true));

        _bindings.Add(entry);
    }

    private Button CreateKeyButton(Transform parent, Font font, out Text label, out Image image)
    {
        GameObject buttonGO = new GameObject("KeyButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGO.transform.SetParent(parent, false);
        image = buttonGO.GetComponent<Image>();
        image.color = _bindingNormalColor;

        RectTransform rect = buttonGO.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0f, BindingButtonHeight);
        LayoutElement layout = buttonGO.AddComponent<LayoutElement>();
        layout.minWidth = BindingButtonMinWidth;
        layout.preferredWidth = 0f;
        layout.flexibleWidth = 1f;
        layout.minHeight = BindingButtonHeight;
        layout.preferredHeight = BindingButtonHeight;

        GameObject textGO = new GameObject("Label", typeof(RectTransform));
        textGO.transform.SetParent(buttonGO.transform, false);
        label = textGO.AddComponent<Text>();
        label.font = font;
        label.fontSize = AllTextSize;
        label.color = Color.white;
        label.alignment = TextAnchor.MiddleCenter;
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return buttonGO.GetComponent<Button>();
    }

    private void StartCapture(BindingEntry entry, bool secondary)
    {
        if (_waitingEntry != null)
            SetEntrySelected(_waitingEntry, _waitingSecondary, false);

        _waitingForKey = true;
        _waitingEntry = entry;
        _waitingSecondary = secondary;
        _captureStartFrame = Time.frameCount;

        SetEntrySelected(entry, secondary, true);

        if (secondary && entry.secondaryText != null)
            entry.secondaryText.text = "...";
        else if (!secondary && entry.primaryText != null)
            entry.primaryText.text = "...";
    }

    private void SetEntrySelected(BindingEntry entry, bool secondary, bool selected)
    {
        if (entry == null)
            return;

        if (secondary)
        {
            if (entry.secondaryImage != null)
                entry.secondaryImage.color = selected ? _bindingSelectedColor : _bindingNormalColor;
            if (entry.primaryImage != null && selected)
                entry.primaryImage.color = _bindingNormalColor;
        }
        else
        {
            if (entry.primaryImage != null)
                entry.primaryImage.color = selected ? _bindingSelectedColor : _bindingNormalColor;
            if (entry.secondaryImage != null && selected)
                entry.secondaryImage.color = _bindingNormalColor;
        }
    }

    private void HookSliders()
    {
        if (ambientSlider != null)
            ambientSlider.onValueChanged.AddListener(value =>
            {
                if (_sound == null) _sound = SoundManager.Instance;
                if (_sound != null)
                    _sound.SetAmbientVolumeMultiplier(SliderToVolumeMultiplier(value));
                UpdateValueTexts();
            });

        if (sfxSlider != null)
            sfxSlider.onValueChanged.AddListener(value =>
            {
                if (_sound == null) _sound = SoundManager.Instance;
                if (_sound != null)
                    _sound.SetSfxVolumeMultiplier(SliderToVolumeMultiplier(value));
                UpdateValueTexts();
            });

        if (tpsSensitivitySlider != null)
            tpsSensitivitySlider.onValueChanged.AddListener(value =>
            {
                if (_input == null) _input = InputManager.Instance;
                if (_input != null)
                    _input.tpsLookSensitivityMultiplier = SliderToSensitivityMultiplier(value);
                UpdateValueTexts();
            });

        if (fpsSensitivitySlider != null)
            fpsSensitivitySlider.onValueChanged.AddListener(value =>
            {
                if (_input == null) _input = InputManager.Instance;
                if (_input != null)
                    _input.fpsLookSensitivityMultiplier = SliderToSensitivityMultiplier(value);
                UpdateValueTexts();
            });
    }

    private void HookLanguageDropdown()
    {
        if (languageDropdown == null)
            return;

        languageDropdown.onValueChanged.RemoveAllListeners();
        languageDropdown.onValueChanged.AddListener(index =>
        {
            Language[] languages = (Language[])Enum.GetValues(typeof(Language));
            if (index >= 0 && index < languages.Length)
                LanguageManager.CurrentLanguage = languages[index];
        });

        RefreshLanguageDropdown();
    }

    private void RefreshLanguageDropdown()
    {
        if (languageDropdown == null)
            return;

        Language[] languages = (Language[])Enum.GetValues(typeof(Language));
        List<string> options = new List<string>();
        foreach (Language language in languages)
            options.Add(language.ToString());

        languageDropdown.ClearOptions();
        languageDropdown.AddOptions(options);

        int currentIndex = Array.IndexOf(languages, LanguageManager.CurrentLanguage);
        if (currentIndex < 0) currentIndex = 0;
        languageDropdown.SetValueWithoutNotify(currentIndex);
        languageDropdown.RefreshShownValue();
    }

    private float SliderToVolumeMultiplier(float value)
    {
        return Mathf.Lerp(MinVolumeMultiplier, MaxVolumeMultiplier, value);
    }

    private float MultiplierToVolumeSlider(float multiplier)
    {
        return Mathf.InverseLerp(MinVolumeMultiplier, MaxVolumeMultiplier, multiplier);
    }

    private float SliderToSensitivityMultiplier(float value)
    {
        return Mathf.Lerp(MinSensitivityMultiplier, MaxSensitivityMultiplier, value);
    }

    private float MultiplierToSensitivitySlider(float multiplier)
    {
        return Mathf.InverseLerp(MinSensitivityMultiplier, MaxSensitivityMultiplier, multiplier);
    }

    private void SetSliderValue(Slider slider, float value)
    {
        if (slider == null) return;
        slider.SetValueWithoutNotify(Mathf.Clamp01(value));
    }
}
