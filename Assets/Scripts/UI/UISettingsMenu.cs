using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UISettingsMenu : GameBehaviour
{
    private const float MinVolumeMultiplier = 0f;
    private const float MaxVolumeMultiplier = 1.5f;
    private const float MinSensitivityMultiplier = 0.2f;
    private const float MaxSensitivityMultiplier = 3f;
    private const float SensitivityBaseMultiplier = 1f;
    private const float SensitivityBaseSlider = 0.75f;
    private const float DefaultVolumeMultiplier = 1f;
    private const float DefaultSensitivityMultiplier = 1f;
    private const int DefaultFpsSetting = 0;
    private const Language DefaultLanguage = Language.English;
    private const int TitleTextSize = 54;
    private const int SectionHeaderTextSize = 38;
    private const int BodyTextSize = 34;
    private const int ValueTextSize = 34;
    private const int ButtonTextSize = 36;
    private const int BindingTextSize = 30;
    private const float BindingButtonMinWidth = 170f;
    private const float BindingButtonHeight = 66f;
    private const float SettingsRowHeight = 66f;
    private const float LanguageDropdownMinWidth = 460f;
    private const float LanguageDropdownTemplateHeight = 340f;
    private const float LanguageDropdownItemHeight = 70f;

    public CanvasGroup canvasGroup;
    public RectTransform panelRoot;
    public Slider ambientSlider;
    public Slider sfxSlider;
    public Slider uiSlider;
    public Slider tpsSensitivitySlider;
    public Slider fpsSensitivitySlider;
    public Slider fpsVerticalSensitivitySlider;
    public Text ambientValue;
    public Text sfxValue;
    public Text uiValue;
    public Text tpsValue;
    public Text fpsValue;
    public Text fpsVerticalValue;
    public Dropdown fpsLimitDropdown;
    public Dropdown languageDropdown;
    public GameObject keybindingsRoot;
    public GameObject keybindingsHeader;
    public GameObject keybindingsContainer;
    public ScrollRect scrollRect;
    public ScrollRect keybindingsScrollRect;

    [Header("Sounds")]
    public SoundParameters openSound;
    public SoundParameters closeSound;
    public SoundParameters captureKeySound;
    public SoundParameters resetSound;

    [Header("Delete Save")]
    public Button deleteSaveButton;
    public GameObject deleteConfirmPanel;
    public Text deleteConfirmText;
    public Button deleteConfirmYes;
    public Button deleteConfirmNo;
    [TextArea] public string deleteButtonEnglish = "Delete Save";
    [TextArea] public string deleteButtonFrench = "Supprimer la sauvegarde";
    [TextArea] public string deleteConfirmEnglish = "Are you sure you want to delete this save?";
    [TextArea] public string deleteConfirmFrench = "Voulez-vous vraiment supprimer cette sauvegarde ?";

    [Header("Title Screen")]
    public Button returnToTitleButton;
    [TextArea] public string returnToTitleEnglish = "Back to Title Screen";
    [TextArea] public string returnToTitleFrench = "Retour à l'écran titre";

    private Button _resetButton;
    private Button _privacyPolicyButton;
    private Button _termsOfUseButton;
    private bool _built;
    private bool _bindingsBuilt;
    private Font _font;
    private InputManager _input;
    private SoundManager _sound;
    private UIManager _uiManager;
    private Text _deleteSaveLabel;
    private Text _returnToTitleLabel;
    private Text _legalHeaderLabel;
    private Text _privacyPolicyLabel;
    private Text _termsOfUseLabel;
    private bool _localizationSubscribed;

    private bool _isOpen;
    private bool _waitingForKey;
    private BindingEntry _waitingEntry;
    private bool _waitingSecondary;
    private int _captureStartFrame = -1;

    private readonly Color _bindingNormalColor = new Color(1f, 1f, 1f, 0.15f);
    private readonly Color _bindingSelectedColor = new Color(0.3f, 0.8f, 1f, 0.35f);
    private readonly Color _accentColor = new Color(0.3f, 0.8f, 1f, 1f);

    private readonly List<BindingEntry> _bindings = new List<BindingEntry>();
    private static readonly int[] FpsLimitOptions = { 0, 30, 60, 90, 120, -1 };
    private static readonly string[] FpsLimitLabelKeys =
    {
        "ui.settings.fps_limit.auto_vsync",
        "ui.settings.fps_limit.30",
        "ui.settings.fps_limit.60",
        "ui.settings.fps_limit.90",
        "ui.settings.fps_limit.120",
        "ui.settings.fps_limit.unlimited"
    };

    private const string SettingsHeaderKey = "ui.settings.header";
    private const string AudioHeaderKey = "ui.settings.audio";
    private const string AmbientVolumeKey = "ui.settings.ambient_volume";
    private const string SfxVolumeKey = "ui.settings.sfx_volume";
    private const string UiVolumeKey = "ui.settings.ui_volume";
    private const string CameraHeaderKey = "ui.settings.camera";
    private const string TpsSensitivityKey = "ui.settings.tps_sensitivity";
    private const string FpsHorizontalSensitivityKey = "ui.settings.fps_horizontal_sensitivity";
    private const string FpsVerticalSensitivityKey = "ui.settings.fps_vertical_sensitivity";
    private const string FpsLimitKey = "ui.settings.fps_limit";
    private const string LanguageHeaderKey = "ui.settings.language";
    private const string KeyBindingsHeaderKey = "ui.settings.key_bindings";
    private const string ResetSettingsKey = "ui.settings.reset";
    private const string ReturnToTitleKey = "ui.settings.return_to_title";
    private const string DeleteSaveKey = "ui.settings.delete_save";
    private const string DeleteConfirmKey = "ui.settings.delete_confirm";

    private struct LocalizedTextBinding
    {
        public Text text;
        public string key;
    }

    private readonly List<LocalizedTextBinding> _localizedTextBindings = new List<LocalizedTextBinding>();

    public bool IsOpen => _isOpen;
    public bool IsCapturingKey => _waitingForKey;

    private class BindingEntry
    {
        public string label;
        public string labelKey;
        public Text labelText;
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

#if UNITY_EDITOR
        UISoundDefaults.AssignIfNull(ref openSound);
        UISoundDefaults.AssignIfNull(ref closeSound);
        UISoundDefaults.AssignIfNull(ref captureKeySound);
        UISoundDefaults.AssignIfNull(ref resetSound);
#endif

        if (deleteSaveButton != null)
        {
            deleteSaveButton.onClick.RemoveAllListeners();
            deleteSaveButton.onClick.AddListener(RequestDeleteSave);
        }
        if (deleteConfirmYes != null)
        {
            deleteConfirmYes.onClick.RemoveAllListeners();
            deleteConfirmYes.onClick.AddListener(ConfirmDeleteSave);
        }
        if (deleteConfirmNo != null)
        {
            deleteConfirmNo.onClick.RemoveAllListeners();
            deleteConfirmNo.onClick.AddListener(CancelDeleteSave);
        }

        if (deleteConfirmPanel != null)
            deleteConfirmPanel.SetActive(false);
    }

    private void Update()
    {
        if (!_isOpen)
            return;

        bool showBindings = !ShouldHideKeyBindingsForMobile();
        if (keybindingsContainer != null)
            keybindingsContainer.SetActive(showBindings);
        if (keybindingsRoot != null)
            keybindingsRoot.SetActive(showBindings);
        if (keybindingsHeader != null)
            keybindingsHeader.SetActive(showBindings);

        if (_waitingForKey)
            CaptureKey();
    }

    private bool ShouldHideKeyBindingsForMobile()
    {
        return Application.isMobilePlatform
               || MobileInput.Enabled
               || (GameManager.Instance != null && GameManager.Instance.mobileControlsEnabled);
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

        if (!visible && deleteConfirmPanel != null)
            deleteConfirmPanel.SetActive(false);

        if (visible)
        {
            if (openSound != null)
                openSound.PlaySound();

            SubscribeLocalization();
        }
        else
        {
            if (closeSound != null)
                closeSound.PlaySound();

            UnsubscribeLocalization();
        }

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
            SetSliderValue(uiSlider, MultiplierToVolumeSlider(_sound.uiVolumeMultiplier));
        }

        if (_input != null)
        {
            SetSliderValue(tpsSensitivitySlider, MultiplierToSensitivitySlider(_input.tpsLookSensitivityMultiplier));
            SetSliderValue(fpsSensitivitySlider, MultiplierToSensitivitySlider(_input.fpsLookSensitivityMultiplier));
            SetSliderValue(fpsVerticalSensitivitySlider, MultiplierToSensitivitySlider(_input.fpsLookVerticalSensitivityMultiplier));
        }

        UpdateValueTexts();
        RefreshFpsDropdown();
        RefreshBindingTexts();
        RefreshLanguageDropdown();
        RefreshLocalizedTexts();

        UpdateDeleteConfirmText();

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

    private void UpdateDeleteConfirmText()
    {
        if (deleteConfirmText != null)
        {
            string fallback = LanguageManager.CurrentLanguage == Language.French && !string.IsNullOrWhiteSpace(deleteConfirmFrench)
                ? deleteConfirmFrench
                : deleteConfirmEnglish;
            deleteConfirmText.text = LocalizationManager.Get(DeleteConfirmKey, fallback);
        }

        if (_deleteSaveLabel != null)
        {
            string fallback = LanguageManager.CurrentLanguage == Language.French && !string.IsNullOrWhiteSpace(deleteButtonFrench)
                ? deleteButtonFrench
                : deleteButtonEnglish;
            _deleteSaveLabel.text = LocalizationManager.Get(DeleteSaveKey, fallback);
        }

        if (_returnToTitleLabel != null)
        {
            string fallback = LanguageManager.CurrentLanguage == Language.French && !string.IsNullOrWhiteSpace(returnToTitleFrench)
                ? returnToTitleFrench
                : returnToTitleEnglish;
            _returnToTitleLabel.text = LocalizationManager.Get(ReturnToTitleKey, fallback);
        }

        if (_legalHeaderLabel != null)
            _legalHeaderLabel.text = LegalDocuments.GetSectionHeaderLabel();

        if (_privacyPolicyLabel != null)
            _privacyPolicyLabel.text = LegalDocuments.GetPrivacyButtonLabel();

        if (_termsOfUseLabel != null)
            _termsOfUseLabel.text = LegalDocuments.GetTermsButtonLabel();
    }

    private void SubscribeLocalization()
    {
        if (_localizationSubscribed)
            return;
        LanguageManager.OnLanguageChanged += HandleLanguageChanged;
        _localizationSubscribed = true;
    }

    private void UnsubscribeLocalization()
    {
        if (!_localizationSubscribed)
            return;
        LanguageManager.OnLanguageChanged -= HandleLanguageChanged;
        _localizationSubscribed = false;
    }

    private void HandleLanguageChanged(Language lang)
    {
        RefreshLocalizedTexts();
        UpdateDeleteConfirmText();
        RefreshLanguageDropdown();
        RefreshFpsDropdown();
    }

    private void RegisterLocalizedText(Text text, string key)
    {
        if (text == null || string.IsNullOrWhiteSpace(key))
            return;

        _localizedTextBindings.Add(new LocalizedTextBinding { text = text, key = key });
        text.text = LocalizationManager.Get(key, text.text);
    }

    private void RefreshLocalizedTexts()
    {
        foreach (LocalizedTextBinding binding in _localizedTextBindings)
        {
            if (binding.text == null || string.IsNullOrWhiteSpace(binding.key))
                continue;
            binding.text.text = LocalizationManager.Get(binding.key, binding.text.text);
        }

        foreach (BindingEntry entry in _bindings)
        {
            if (entry.labelText == null || string.IsNullOrWhiteSpace(entry.labelKey))
                continue;
            entry.labelText.text = LocalizationManager.Get(entry.labelKey, entry.label);
        }
    }

    private void RequestDeleteSave()
    {
        if (deleteConfirmPanel != null)
            deleteConfirmPanel.SetActive(true);
    }

    private void CancelDeleteSave()
    {
        if (deleteConfirmPanel != null)
            deleteConfirmPanel.SetActive(false);
    }

    private void ConfirmDeleteSave()
    {
        if (deleteConfirmPanel != null)
            deleteConfirmPanel.SetActive(false);

        SaveManager.DeleteSave(SaveManager.CurrentSlot);

        GameManager gm = GameManager.Instance;
        if (gm != null && !string.IsNullOrEmpty(gm.titleScreenSceneName))
        {
            gm.LoadScene(gm.titleScreenSceneName);
        }
    }

    private void ReturnToTitleScreen()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || string.IsNullOrEmpty(gm.titleScreenSceneName))
            return;

        if (closeSound != null)
            closeSound.PlaySound();

        gm.LoadScene(gm.titleScreenSceneName);
    }

    private GameObject CreateDeleteConfirmPanel(Transform parent, Font font)
    {
        GameObject overlay = new GameObject("DeleteConfirmPanel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(LayoutElement));
        overlay.transform.SetParent(parent, false);
        RectTransform overlayRect = overlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image overlayImage = overlay.GetComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0.65f);

        LayoutElement overlayLayout = overlay.GetComponent<LayoutElement>();
        overlayLayout.ignoreLayout = true;

        CanvasGroup overlayGroup = overlay.GetComponent<CanvasGroup>();
        overlayGroup.blocksRaycasts = true;
        overlayGroup.interactable = true;

        GameObject panel = new GameObject("ConfirmBox", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(overlay.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.2f, 0.3f);
        panelRect.anchorMax = new Vector2(0.8f, 0.7f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0.08f, 0.1f, 0.16f, 0.98f);

        VerticalLayoutGroup panelLayout = panel.AddComponent<VerticalLayoutGroup>();
        panelLayout.childAlignment = TextAnchor.MiddleCenter;
        panelLayout.childControlHeight = true;
        panelLayout.childControlWidth = true;
        panelLayout.childForceExpandHeight = false;
        panelLayout.childForceExpandWidth = true;
        panelLayout.spacing = 20f;
        panelLayout.padding = new RectOffset(24, 24, 24, 24);

        GameObject textGO = new GameObject("ConfirmText", typeof(RectTransform));
        textGO.transform.SetParent(panel.transform, false);
        Text text = textGO.AddComponent<Text>();
        text.font = font;
        text.fontSize = BodyTextSize;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        deleteConfirmText = text;

        GameObject buttonsRow = new GameObject("Buttons", typeof(RectTransform));
        buttonsRow.transform.SetParent(panel.transform, false);
        HorizontalLayoutGroup buttonsLayout = buttonsRow.AddComponent<HorizontalLayoutGroup>();
        buttonsLayout.childAlignment = TextAnchor.MiddleCenter;
        buttonsLayout.childControlHeight = true;
        buttonsLayout.childControlWidth = true;
        buttonsLayout.childForceExpandHeight = false;
        buttonsLayout.childForceExpandWidth = true;
        buttonsLayout.spacing = 18f;

        deleteConfirmYes = CreateConfirmButton(buttonsRow.transform, "Yes", font, new Color(0.2f, 0.7f, 0.3f, 0.35f), "ui.common.yes");
        deleteConfirmNo = CreateConfirmButton(buttonsRow.transform, "No", font, new Color(0.8f, 0.2f, 0.2f, 0.35f), "ui.common.no");

        return overlay;
    }

    private Button CreateConfirmButton(Transform parent, string labelText, Font font, Color bgColor, string localizationKey = null)
    {
        GameObject buttonGO = new GameObject($"Button_{labelText}", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGO.transform.SetParent(parent, false);
        Image image = buttonGO.GetComponent<Image>();
        image.color = bgColor;

        LayoutElement layout = buttonGO.AddComponent<LayoutElement>();
        layout.minWidth = 220f;
        layout.preferredWidth = 260f;
        layout.minHeight = BindingButtonHeight;
        layout.preferredHeight = BindingButtonHeight;
        layout.flexibleWidth = 1f;

        GameObject textGO = new GameObject("Label", typeof(RectTransform));
        textGO.transform.SetParent(buttonGO.transform, false);
        Text text = textGO.AddComponent<Text>();
        text.text = localizationKey != null ? LocalizationManager.Get(localizationKey, labelText) : labelText;
        text.font = font;
        text.fontSize = ButtonTextSize;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        if (!string.IsNullOrWhiteSpace(localizationKey))
            RegisterLocalizedText(text, localizationKey);

        return buttonGO.GetComponent<Button>();
    }

    private void UpdateValueTexts()
    {
        if (ambientValue != null && _sound != null)
            ambientValue.text = $"{_sound.ambientVolumeMultiplier:0.00}x";
        if (sfxValue != null && _sound != null)
            sfxValue.text = $"{_sound.sfxVolumeMultiplier:0.00}x";
        if (uiValue != null && _sound != null)
            uiValue.text = $"{_sound.uiVolumeMultiplier:0.00}x";
        if (tpsValue != null && _input != null)
            tpsValue.text = $"{_input.tpsLookSensitivityMultiplier:0.00}x";
        if (fpsValue != null && _input != null)
            fpsValue.text = $"{_input.fpsLookSensitivityMultiplier:0.00}x";
        if (fpsVerticalValue != null && _input != null)
            fpsVerticalValue.text = $"{_input.fpsLookVerticalSensitivityMultiplier:0.00}x";
    }

    private void RefreshBindingTexts()
    {
        if (_input == null)
            return;

        foreach (BindingEntry entry in _bindings)
        {
            if (entry.primaryText != null)
                entry.primaryText.text = entry.getPrimary != null ? _input.GetKeyDisplayName(entry.getPrimary()) : "-";

            if (entry.secondaryText != null)
            {
                KeyCode secondary = entry.getSecondary != null ? entry.getSecondary() : KeyCode.None;
                entry.secondaryText.text = secondary == KeyCode.None ? "-" : _input.GetKeyDisplayName(secondary);
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
        panelRoot.anchorMin = new Vector2(0.13f, 0.08f);
        panelRoot.anchorMax = new Vector2(0.87f, 0.92f);
        panelRoot.pivot = new Vector2(0.5f, 0.5f);
        panelRoot.offsetMin = Vector2.zero;
        panelRoot.offsetMax = Vector2.zero;

        Image bg = root.GetComponent<Image>();
        if (bg == null)
            bg = root.AddComponent<Image>();
        bg.color = new Color(0.035f, 0.055f, 0.085f, 0.985f);

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
        layout.spacing = 14f;
        layout.padding = new RectOffset(30, 30, 24, 24);

        CreateHeader(root.transform, "Settings", _font, TitleTextSize, SettingsHeaderKey);

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
        scrollBg.color = new Color(1f, 1f, 1f, 0.025f);

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
        viewportRect.offsetMin = new Vector2(18f, 12f);
        viewportRect.offsetMax = new Vector2(-34f, -12f);

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
        contentLayout.spacing = 10f;
        contentLayout.padding = new RectOffset(10, 10, 10, 16);

        ContentSizeFitter contentFitter = contentGO.AddComponent<ContentSizeFitter>();
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        GameObject scrollbarGO = new GameObject("Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
        scrollbarGO.transform.SetParent(scrollGO.transform, false);
        RectTransform sbRect = scrollbarGO.GetComponent<RectTransform>();
        sbRect.anchorMin = new Vector2(1f, 0f);
        sbRect.anchorMax = new Vector2(1f, 1f);
        sbRect.pivot = new Vector2(1f, 1f);
        sbRect.sizeDelta = new Vector2(12f, 0f);
        sbRect.anchoredPosition = new Vector2(-6f, 0f);
        Image sbBg = scrollbarGO.GetComponent<Image>();
        sbBg.color = new Color(1f, 1f, 1f, 0.18f);
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

        returnToTitleButton = CreateActionButton(contentGO.transform, returnToTitleEnglish, _font, ReturnToTitleKey);
        if (returnToTitleButton != null)
        {
            returnToTitleButton.onClick.RemoveAllListeners();
            Image titleImage = returnToTitleButton.GetComponent<Image>();
            if (titleImage != null)
                titleImage.color = new Color(1f, 1f, 1f, 0.22f);
            _returnToTitleLabel = returnToTitleButton.GetComponentInChildren<Text>();
            returnToTitleButton.onClick.AddListener(ReturnToTitleScreen);
        }

        CreateHeader(contentGO.transform, "Audio", _font, SectionHeaderTextSize, AudioHeaderKey);
        ambientSlider = CreateSliderRow(contentGO.transform, "Ambient Volume", _font, out ambientValue, AmbientVolumeKey);
        sfxSlider = CreateSliderRow(contentGO.transform, "SFX Volume", _font, out sfxValue, SfxVolumeKey);
        uiSlider = CreateSliderRow(contentGO.transform, "UI Volume", _font, out uiValue, UiVolumeKey);

        CreateHeader(contentGO.transform, "Camera", _font, SectionHeaderTextSize, CameraHeaderKey);
        tpsSensitivitySlider = CreateSliderRow(contentGO.transform, "TPS Camera Sensitivity", _font, out tpsValue, TpsSensitivityKey);
        fpsSensitivitySlider = CreateSliderRow(contentGO.transform, "FPS Horizontal Camera Sensitivity", _font, out fpsValue, FpsHorizontalSensitivityKey);
        fpsVerticalSensitivitySlider = CreateSliderRow(contentGO.transform, "FPS Vertical Camera Sensitivity", _font, out fpsVerticalValue, FpsVerticalSensitivityKey);
        fpsLimitDropdown = CreateDropdownRow(contentGO.transform, "FPS Limit", _font, FpsLimitKey);

        CreateHeader(contentGO.transform, "Language", _font, SectionHeaderTextSize, LanguageHeaderKey);
        languageDropdown = CreateLanguageDropdownRow(contentGO.transform, "Language", _font, LanguageHeaderKey);

        GameObject legalHeader = CreateHeader(contentGO.transform, LegalDocuments.GetSectionHeaderLabel(), _font, SectionHeaderTextSize);
        if (legalHeader != null)
            _legalHeaderLabel = legalHeader.GetComponent<Text>();
        _privacyPolicyButton = CreateActionButton(contentGO.transform, LegalDocuments.GetPrivacyButtonLabel(), _font);
        if (_privacyPolicyButton != null)
        {
            _privacyPolicyLabel = _privacyPolicyButton.GetComponentInChildren<Text>();
            _privacyPolicyButton.onClick.AddListener(() => UILegalOverlay.Instance.ShowDocument(LegalDocumentType.PrivacyPolicy));
        }

        _termsOfUseButton = CreateActionButton(contentGO.transform, LegalDocuments.GetTermsButtonLabel(), _font);
        if (_termsOfUseButton != null)
        {
            _termsOfUseLabel = _termsOfUseButton.GetComponentInChildren<Text>();
            _termsOfUseButton.onClick.AddListener(() => UILegalOverlay.Instance.ShowDocument(LegalDocumentType.TermsOfUse));
        }

        keybindingsHeader = CreateHeader(contentGO.transform, "Key Bindings", _font, SectionHeaderTextSize, KeyBindingsHeaderKey);
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

        _resetButton = CreateActionButton(contentGO.transform, "Reset Settings", _font, ResetSettingsKey);
        _resetButton.onClick.AddListener(() =>
        {
            if (resetSound != null)
                resetSound.PlaySound();
            ResetSettingsToDefault();
        });

        deleteSaveButton = CreateActionButton(contentGO.transform, deleteButtonEnglish, _font, DeleteSaveKey);
        if (deleteSaveButton != null)
        {
            Image deleteImage = deleteSaveButton.GetComponent<Image>();
            if (deleteImage != null)
                deleteImage.color = new Color(0.8f, 0.2f, 0.2f, 0.28f);
            _deleteSaveLabel = deleteSaveButton.GetComponentInChildren<Text>();
        }

        deleteConfirmPanel = CreateDeleteConfirmPanel(root.transform, _font);
        if (deleteConfirmPanel != null)
        {
            deleteConfirmPanel.SetActive(false);
            deleteConfirmPanel.transform.SetAsLastSibling();
        }

        HookSliders();
        HookFpsDropdown();
        HookLanguageDropdown();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        UISoundDefaults.AssignIfNull(ref openSound);
        UISoundDefaults.AssignIfNull(ref closeSound);
        UISoundDefaults.AssignIfNull(ref captureKeySound);
        UISoundDefaults.AssignIfNull(ref resetSound);
    }
#endif

    private GameObject CreateHeader(Transform parent, string title, Font font, int size = 24, string localizationKey = null)
    {
        GameObject header = new GameObject($"Header_{title}", typeof(RectTransform));
        header.transform.SetParent(parent, false);
        Text text = header.AddComponent<Text>();
        text.text = localizationKey != null ? LocalizationManager.Get(localizationKey, title) : title;
        text.font = font;
        text.fontSize = size;
        text.color = _accentColor;
        text.alignment = TextAnchor.MiddleCenter;
        LayoutElement layout = header.AddComponent<LayoutElement>();
        layout.preferredHeight = size + 10f;
        layout.flexibleHeight = 0f;

        if (!string.IsNullOrWhiteSpace(localizationKey))
            RegisterLocalizedText(text, localizationKey);

        return header;
    }

    private Slider CreateSliderRow(Transform parent, string label, Font font, out Text valueText, string localizationKey = null)
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

        float rowHeight = SettingsRowHeight;
        LayoutElement rowLayout = row.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = rowHeight;
        rowLayout.flexibleHeight = 0f;

        GameObject labelGO = new GameObject("Label", typeof(RectTransform));
        labelGO.transform.SetParent(row.transform, false);
        Text labelText = labelGO.AddComponent<Text>();
        labelText.text = localizationKey != null ? LocalizationManager.Get(localizationKey, label) : label;
        labelText.font = font;
        labelText.fontSize = BodyTextSize;
        labelText.color = new Color(0.9f, 0.94f, 1f, 0.96f);
        labelText.alignment = TextAnchor.MiddleLeft;
        labelText.horizontalOverflow = HorizontalWrapMode.Wrap;
        labelText.verticalOverflow = VerticalWrapMode.Overflow;

        LayoutElement labelLayout = labelGO.AddComponent<LayoutElement>();
        labelLayout.minWidth = 260f;
        labelLayout.preferredWidth = 360f;
        labelLayout.flexibleWidth = 1f;

        if (!string.IsNullOrWhiteSpace(localizationKey))
            RegisterLocalizedText(labelText, localizationKey);

        GameObject sliderGO = new GameObject("Slider", typeof(RectTransform));
        sliderGO.transform.SetParent(row.transform, false);
        Slider slider = sliderGO.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.5f;
        slider.transition = Selectable.Transition.ColorTint;

        RectTransform sliderRect = sliderGO.GetComponent<RectTransform>();
        sliderRect.sizeDelta = new Vector2(0f, 46f);
        LayoutElement sliderLayout = sliderGO.AddComponent<LayoutElement>();
        sliderLayout.minWidth = 280f;
        sliderLayout.preferredWidth = 360f;
        sliderLayout.flexibleWidth = 1f;
        sliderLayout.minHeight = 46f;
        sliderLayout.preferredHeight = 46f;

        GameObject background = new GameObject("Background", typeof(RectTransform), typeof(Image));
        background.transform.SetParent(sliderGO.transform, false);
        Image bgImage = background.GetComponent<Image>();
        bgImage.color = new Color(1f, 1f, 1f, 0.18f);
        RectTransform bgRect = background.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0f, 0.42f);
        bgRect.anchorMax = new Vector2(1f, 0.58f);
        bgRect.offsetMin = new Vector2(12f, 0f);
        bgRect.offsetMax = new Vector2(-12f, 0f);

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
        handleRect.sizeDelta = new Vector2(24f, 42f);
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;
        slider.direction = Slider.Direction.LeftToRight;

        GameObject valueGO = new GameObject("Value", typeof(RectTransform));
        valueGO.transform.SetParent(row.transform, false);
        valueText = valueGO.AddComponent<Text>();
        valueText.text = "1.00x";
        valueText.font = font;
        valueText.fontSize = ValueTextSize;
        valueText.color = new Color(0.92f, 0.96f, 1f, 0.98f);
        valueText.alignment = TextAnchor.MiddleCenter;
        LayoutElement valueLayout = valueGO.AddComponent<LayoutElement>();
        valueLayout.preferredWidth = 140f;
        valueLayout.minWidth = 120f;

        return slider;
    }

    private Dropdown CreateDropdownRow(Transform parent, string label, Font font, string localizationKey = null)
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

        float rowHeight = SettingsRowHeight;
        LayoutElement rowLayout = row.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = rowHeight;
        rowLayout.flexibleHeight = 0f;

        GameObject labelGO = new GameObject("Label", typeof(RectTransform));
        labelGO.transform.SetParent(row.transform, false);
        Text labelText = labelGO.AddComponent<Text>();
        labelText.text = localizationKey != null ? LocalizationManager.Get(localizationKey, label) : label;
        labelText.font = font;
        labelText.fontSize = BodyTextSize;
        labelText.color = new Color(0.9f, 0.94f, 1f, 0.96f);
        labelText.alignment = TextAnchor.MiddleLeft;
        labelText.horizontalOverflow = HorizontalWrapMode.Wrap;
        labelText.verticalOverflow = VerticalWrapMode.Overflow;
        LayoutElement labelLayout = labelGO.AddComponent<LayoutElement>();
        labelLayout.minWidth = 260f;
        labelLayout.preferredWidth = 360f;
        labelLayout.flexibleWidth = 1f;

        if (!string.IsNullOrWhiteSpace(localizationKey))
            RegisterLocalizedText(labelText, localizationKey);

        Dropdown dropdown = CreateDropdown(row.transform, font);
        return dropdown;
    }

    private Dropdown CreateLanguageDropdownRow(Transform parent, string label, Font font, string localizationKey = null)
    {
        Dropdown dropdown = CreateDropdownRow(parent, label, font, localizationKey);
        if (dropdown == null)
            return null;

        LayoutElement layout = dropdown.GetComponent<LayoutElement>();
        if (layout != null)
            layout.minWidth = LanguageDropdownMinWidth;

        if (dropdown.template != null)
            dropdown.template.sizeDelta = new Vector2(dropdown.template.sizeDelta.x, LanguageDropdownTemplateHeight);

        if (dropdown.captionText != null)
            dropdown.captionText.fontSize = BodyTextSize;

        if (dropdown.itemText != null)
            dropdown.itemText.fontSize = BodyTextSize;

        RectTransform itemRect = dropdown.itemText != null ? dropdown.itemText.transform.parent as RectTransform : null;
        if (itemRect != null)
            itemRect.sizeDelta = new Vector2(itemRect.sizeDelta.x, LanguageDropdownItemHeight);

        LayoutElement itemLayout = itemRect != null ? itemRect.GetComponent<LayoutElement>() : null;
        if (itemLayout != null)
        {
            itemLayout.minHeight = LanguageDropdownItemHeight;
            itemLayout.preferredHeight = LanguageDropdownItemHeight;
        }

        return dropdown;
    }

    private Dropdown CreateDropdown(Transform parent, Font font)
    {
        GameObject dropdownGO = new GameObject("Dropdown", typeof(RectTransform), typeof(Image), typeof(Dropdown));
        dropdownGO.transform.SetParent(parent, false);
        Image bg = dropdownGO.GetComponent<Image>();
        bg.color = new Color(1f, 1f, 1f, 0.12f);

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
        label.fontSize = BodyTextSize;
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
        itemRect.sizeDelta = new Vector2(0f, LanguageDropdownItemHeight);
        LayoutElement itemLayout = itemGO.AddComponent<LayoutElement>();
        itemLayout.minHeight = LanguageDropdownItemHeight;
        itemLayout.preferredHeight = LanguageDropdownItemHeight;
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
        itemLabel.fontSize = BodyTextSize;
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
            AddBinding(parent, font, def.label, def.labelKey, def.getPrimary, def.setPrimary, def.getSecondary, def.setSecondary);
        }
    }

    private void AddBinding(Transform parent, Font font, string label, string labelKey,
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
        rowLayout.preferredHeight = BindingButtonHeight + 8f;
        rowLayout.flexibleHeight = 0f;

        GameObject labelGO = new GameObject("Label", typeof(RectTransform));
        labelGO.transform.SetParent(row.transform, false);
        Text labelText = labelGO.AddComponent<Text>();
        labelText.text = string.IsNullOrWhiteSpace(labelKey)
            ? label
            : LocalizationManager.Get(labelKey, label);
        labelText.font = font;
        labelText.fontSize = BindingTextSize;
        labelText.color = new Color(0.9f, 0.94f, 1f, 0.96f);
        labelText.alignment = TextAnchor.MiddleLeft;
        labelText.horizontalOverflow = HorizontalWrapMode.Wrap;
        labelText.verticalOverflow = VerticalWrapMode.Overflow;
        LayoutElement labelLayout = labelGO.AddComponent<LayoutElement>();
        labelLayout.minWidth = 260f;
        labelLayout.preferredWidth = 300f;
        labelLayout.flexibleWidth = 0f;

        Button primaryButton = CreateKeyButton(row.transform, font, out Text primaryText, out Image primaryImage);
        Button secondaryButton = CreateKeyButton(row.transform, font, out Text secondaryText, out Image secondaryImage);

        BindingEntry entry = new BindingEntry
        {
            label = label,
            labelKey = labelKey,
            labelText = labelText,
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
        label.fontSize = BindingTextSize;
        label.color = Color.white;
        label.alignment = TextAnchor.MiddleCenter;
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return buttonGO.GetComponent<Button>();
    }

    private Button CreateActionButton(Transform parent, string labelText, Font font, string localizationKey = null)
    {
        GameObject buttonGO = new GameObject($"Button_{labelText}", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGO.transform.SetParent(parent, false);

        Image image = buttonGO.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.18f);

        LayoutElement layout = buttonGO.AddComponent<LayoutElement>();
        layout.minHeight = BindingButtonHeight;
        layout.preferredHeight = BindingButtonHeight;
        layout.flexibleWidth = 1f;

        GameObject textGO = new GameObject("Label", typeof(RectTransform));
        textGO.transform.SetParent(buttonGO.transform, false);
        Text text = textGO.AddComponent<Text>();
        text.text = localizationKey != null ? LocalizationManager.Get(localizationKey, labelText) : labelText;
        text.font = font;
        text.fontSize = ButtonTextSize;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        if (!string.IsNullOrWhiteSpace(localizationKey))
            RegisterLocalizedText(text, localizationKey);

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

        if (captureKeySound != null)
            captureKeySound.PlaySound();
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
                float multiplier = SliderToVolumeMultiplier(value);
                if (_sound != null)
                    _sound.SetAmbientVolumeMultiplier(multiplier);
                SaveSettingFloat(SaveKeys.AMBIENT_VOLUME_MULTIPLIER, multiplier);
                UpdateValueTexts();
            });

        if (sfxSlider != null)
            sfxSlider.onValueChanged.AddListener(value =>
            {
                if (_sound == null) _sound = SoundManager.Instance;
                float multiplier = SliderToVolumeMultiplier(value);
                if (_sound != null)
                    _sound.SetSfxVolumeMultiplier(multiplier);
                SaveSettingFloat(SaveKeys.SFX_VOLUME_MULTIPLIER, multiplier);
                UpdateValueTexts();
            });

        if (uiSlider != null)
            uiSlider.onValueChanged.AddListener(value =>
            {
                if (_sound == null) _sound = SoundManager.Instance;
                float multiplier = SliderToVolumeMultiplier(value);
                if (_sound != null)
                    _sound.SetUiVolumeMultiplier(multiplier);
                SaveSettingFloat(SaveKeys.UI_VOLUME_MULTIPLIER, multiplier);
                UpdateValueTexts();
            });

        if (tpsSensitivitySlider != null)
            tpsSensitivitySlider.onValueChanged.AddListener(value =>
            {
                if (_input == null) _input = InputManager.Instance;
                float multiplier = SliderToSensitivityMultiplier(value);
                if (_input != null)
                    _input.tpsLookSensitivityMultiplier = multiplier;
                SaveSettingFloat(SaveKeys.TPS_SENSITIVITY_MULTIPLIER, multiplier);
                UpdateValueTexts();
            });

        if (fpsSensitivitySlider != null)
            fpsSensitivitySlider.onValueChanged.AddListener(value =>
            {
                if (_input == null) _input = InputManager.Instance;
                float multiplier = SliderToSensitivityMultiplier(value);
                if (_input != null)
                    _input.fpsLookSensitivityMultiplier = multiplier;
                SaveSettingFloat(SaveKeys.FPS_SENSITIVITY_MULTIPLIER, multiplier);
                UpdateValueTexts();
            });

        if (fpsVerticalSensitivitySlider != null)
            fpsVerticalSensitivitySlider.onValueChanged.AddListener(value =>
            {
                if (_input == null) _input = InputManager.Instance;
                float multiplier = SliderToSensitivityMultiplier(value);
                if (_input != null)
                    _input.fpsLookVerticalSensitivityMultiplier = multiplier;
                SaveSettingFloat(SaveKeys.FPS_VERTICAL_SENSITIVITY_MULTIPLIER, multiplier);
                UpdateValueTexts();
            });
    }

    private void HookFpsDropdown()
    {
        if (fpsLimitDropdown == null)
            return;

        fpsLimitDropdown.onValueChanged.RemoveAllListeners();
        fpsLimitDropdown.onValueChanged.AddListener(index =>
        {
            int fpsSetting = GetFpsSettingByIndex(index);
            if (GameManager.Instance != null)
                GameManager.Instance.ApplyFrameRateSetting(fpsSetting, save: true);
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
            SaveSettingInt(SaveKeys.LANGUAGE, index);
        });

        RefreshLanguageDropdown();
    }

    private void SaveSettingFloat(string key, float value)
    {
        GameManager gm = GameManager.Instance;
        if (gm == null)
            return;
        gm.SetFloat(key, value);
    }

    private void SaveSettingInt(string key, int value)
    {
        GameManager gm = GameManager.Instance;
        if (gm == null)
            return;
        gm.SetInt(key, value);
    }

    private void ResetSettingsToDefault()
    {
        if (_sound == null) _sound = SoundManager.Instance;
        if (_input == null) _input = InputManager.Instance;

        float ambient = DefaultVolumeMultiplier;
        float sfx = DefaultVolumeMultiplier;
        float ui = DefaultVolumeMultiplier;
        float tps = DefaultSensitivityMultiplier;
        float fps = DefaultSensitivityMultiplier;
        float fpsVertical = DefaultSensitivityMultiplier;

        if (_sound != null)
        {
            _sound.SetAmbientVolumeMultiplier(ambient);
            _sound.SetSfxVolumeMultiplier(sfx);
            _sound.SetUiVolumeMultiplier(ui);
        }

        if (_input != null)
        {
            _input.tpsLookSensitivityMultiplier = tps;
            fps = _input.GetDefaultFpsLookHorizontalSensitivityMultiplier();
            _input.fpsLookSensitivityMultiplier = fps;
            fpsVertical = _input.GetDefaultFpsLookVerticalSensitivityMultiplier();
            _input.fpsLookVerticalSensitivityMultiplier = fpsVertical;
        }

        LanguageManager.CurrentLanguage = DefaultLanguage;

        if (GameManager.Instance != null)
            GameManager.Instance.ApplyFrameRateSetting(DefaultFpsSetting, save: true);

        SaveSettingFloat(SaveKeys.AMBIENT_VOLUME_MULTIPLIER, ambient);
        SaveSettingFloat(SaveKeys.SFX_VOLUME_MULTIPLIER, sfx);
        SaveSettingFloat(SaveKeys.UI_VOLUME_MULTIPLIER, ui);
        SaveSettingFloat(SaveKeys.TPS_SENSITIVITY_MULTIPLIER, tps);
        SaveSettingFloat(SaveKeys.FPS_SENSITIVITY_MULTIPLIER, fps);
        SaveSettingFloat(SaveKeys.FPS_VERTICAL_SENSITIVITY_MULTIPLIER, fpsVertical);
        SaveSettingInt(SaveKeys.LANGUAGE, (int)DefaultLanguage);

        SetSliderValue(ambientSlider, MultiplierToVolumeSlider(ambient));
        SetSliderValue(sfxSlider, MultiplierToVolumeSlider(sfx));
        SetSliderValue(uiSlider, MultiplierToVolumeSlider(ui));
        SetSliderValue(tpsSensitivitySlider, MultiplierToSensitivitySlider(tps));
        SetSliderValue(fpsSensitivitySlider, MultiplierToSensitivitySlider(fps));
        SetSliderValue(fpsVerticalSensitivitySlider, MultiplierToSensitivitySlider(fpsVertical));
        UpdateValueTexts();
        RefreshFpsDropdown();
        RefreshLanguageDropdown();
    }

    private void RefreshLanguageDropdown()
    {
        if (languageDropdown == null)
            return;

        Language[] languages = (Language[])Enum.GetValues(typeof(Language));
        List<string> options = new List<string>();
        foreach (Language language in languages)
            options.Add(GetLanguageDisplayName(language));

        languageDropdown.ClearOptions();
        languageDropdown.AddOptions(options);

        int currentIndex = Array.IndexOf(languages, LanguageManager.CurrentLanguage);
        if (currentIndex < 0) currentIndex = 0;
        languageDropdown.SetValueWithoutNotify(currentIndex);
        languageDropdown.RefreshShownValue();
    }

    private void RefreshFpsDropdown()
    {
        if (fpsLimitDropdown == null)
            return;

        fpsLimitDropdown.ClearOptions();
        List<string> labels = new List<string>();
        for (int i = 0; i < FpsLimitLabelKeys.Length; i++)
            labels.Add(LocalizationManager.Get(FpsLimitLabelKeys[i]));
        fpsLimitDropdown.AddOptions(labels);

        int setting = ResolveCurrentFpsSetting();
        int index = GetFpsIndex(setting);
        fpsLimitDropdown.SetValueWithoutNotify(index);
        fpsLimitDropdown.RefreshShownValue();
    }

    private string GetLanguageDisplayName(Language language)
    {
        string key = $"language.{language.ToString().ToLowerInvariant()}";
        return LocalizationManager.Get(key, language.ToString());
    }

    private int ResolveCurrentFpsSetting()
    {
        int saved = GameManager.Instance != null
            ? GameManager.Instance.GetInt(SaveKeys.TARGET_FPS, int.MinValue)
            : int.MinValue;
        if (saved != int.MinValue)
            return saved;

        if (QualitySettings.vSyncCount > 0)
            return 0;

        int target = Application.targetFrameRate;
        if (target <= 0)
            return -1;

        return target;
    }

    private int GetFpsIndex(int fpsSetting)
    {
        for (int i = 0; i < FpsLimitOptions.Length; i++)
        {
            if (FpsLimitOptions[i] == fpsSetting)
                return i;
        }

        if (fpsSetting <= 0)
            return Array.IndexOf(FpsLimitOptions, -1);

        int closestIndex = 0;
        int closestDistance = int.MaxValue;
        for (int i = 0; i < FpsLimitOptions.Length; i++)
        {
            int option = FpsLimitOptions[i];
            if (option <= 0)
                continue;
            int distance = Mathf.Abs(option - fpsSetting);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }
        return closestIndex;
    }

    private int GetFpsSettingByIndex(int index)
    {
        if (index < 0 || index >= FpsLimitOptions.Length)
            return 0;
        return FpsLimitOptions[index];
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
        float t = Mathf.Clamp01(value);
        if (t <= SensitivityBaseSlider)
        {
            float local = t / Mathf.Max(0.0001f, SensitivityBaseSlider);
            return Mathf.Lerp(MinSensitivityMultiplier, SensitivityBaseMultiplier, local);
        }

        float upper = (t - SensitivityBaseSlider) / Mathf.Max(0.0001f, 1f - SensitivityBaseSlider);
        return Mathf.Lerp(SensitivityBaseMultiplier, MaxSensitivityMultiplier, upper);
    }

    private float MultiplierToSensitivitySlider(float multiplier)
    {
        float m = Mathf.Clamp(multiplier, MinSensitivityMultiplier, MaxSensitivityMultiplier);
        if (m <= SensitivityBaseMultiplier)
        {
            float local = Mathf.InverseLerp(MinSensitivityMultiplier, SensitivityBaseMultiplier, m);
            return local * SensitivityBaseSlider;
        }

        float upper = Mathf.InverseLerp(SensitivityBaseMultiplier, MaxSensitivityMultiplier, m);
        return SensitivityBaseSlider + upper * (1f - SensitivityBaseSlider);
    }

    private void SetSliderValue(Slider slider, float value)
    {
        if (slider == null) return;
        slider.SetValueWithoutNotify(Mathf.Clamp01(value));
    }
}
