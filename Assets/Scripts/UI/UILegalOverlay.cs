using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UILegalOverlay : MonoBehaviour
{
    private static UILegalOverlay _instance;

    private Canvas _canvas;
    private CanvasScaler _canvasScaler;
    private GameObject _backdrop;
    private RectTransform _panelRect;
    private VerticalLayoutGroup _panelLayout;
    private Text _titleText;
    private Text _introText;
    private Text _eyebrowText;
    private Text _documentTitleText;
    private Text _documentBodyText;
    private Button _privacyButton;
    private Button _termsButton;
    private Button _acceptButton;
    private Button _closeButton;
    private ScrollRect _scrollRect;
    private HorizontalLayoutGroup _tabsLayout;
    private HorizontalLayoutGroup _bottomLayout;
    private LayoutElement _titleLayout;
    private LayoutElement _introLayout;
    private LayoutElement _eyebrowLayout;
    private LayoutElement _documentTitleLayout;
    private LayoutElement _tabsRowLayout;
    private LayoutElement _scrollLayout;
    private LayoutElement _bottomRowLayout;
    private int _lastScreenWidth;
    private int _lastScreenHeight;

    private LegalDocumentType _currentDocumentType = LegalDocumentType.PrivacyPolicy;
    private bool _requireAcceptance;

    public static UILegalOverlay Instance
    {
        get
        {
            EnsureExists();
            return _instance;
        }
    }

    public static void EnsureExists()
    {
        if (_instance != null)
            return;

        GameObject root = new GameObject("UILegalOverlay");
        DontDestroyOnLoad(root);
        _instance = root.AddComponent<UILegalOverlay>();
        _instance.BuildUi();
        _instance.HideImmediate();
    }

    private void OnEnable()
    {
        LanguageManager.OnLanguageChanged += HandleLanguageChanged;
    }

    private void OnDisable()
    {
        LanguageManager.OnLanguageChanged -= HandleLanguageChanged;
    }

    public void ShowFirstLaunchGateIfNeeded()
    {
        if (LegalDocuments.HasAcceptedLatestDocuments())
            return;

        Show(requireAcceptance: true, LegalDocumentType.PrivacyPolicy);
    }

    public void ShowDocument(LegalDocumentType documentType)
    {
        Show(requireAcceptance: false, documentType);
    }

    public void Show(bool requireAcceptance, LegalDocumentType documentType)
    {
        _requireAcceptance = requireAcceptance;
        _currentDocumentType = documentType;

        ApplyResponsiveLayout(force: true);
        RefreshTexts();

        if (_canvas != null)
        {
            _canvas.enabled = true;
            GraphicRaycaster raycaster = _canvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
                raycaster.enabled = true;
        }

        if (_backdrop != null)
            _backdrop.SetActive(true);
    }

    public void Hide()
    {
        if (_requireAcceptance && !LegalDocuments.HasAcceptedLatestDocuments())
            return;

        HideImmediate();
    }

    private void HideImmediate()
    {
        _requireAcceptance = false;

        if (_canvas != null)
        {
            _canvas.enabled = false;
            GraphicRaycaster raycaster = _canvas.GetComponent<GraphicRaycaster>();
            if (raycaster != null)
                raycaster.enabled = false;
        }

        if (_backdrop != null)
            _backdrop.SetActive(false);
    }

    private void AcceptAndContinue()
    {
        LegalDocuments.AcceptLatestDocuments();
        HideImmediate();
    }

    private void HandleLanguageChanged(Language language)
    {
        if (_canvas != null && _canvas.enabled)
            RefreshTexts();
    }

    private void Update()
    {
        if (_lastScreenWidth != Screen.width || _lastScreenHeight != Screen.height)
            ApplyResponsiveLayout(force: true);
    }

    private void RefreshTexts()
    {
        if (_titleText != null)
            _titleText.text = LegalDocuments.GetWindowTitle(_requireAcceptance);

        if (_introText != null)
            _introText.text = LegalDocuments.GetIntroText(_requireAcceptance);

        if (_eyebrowText != null)
            _eyebrowText.text = _requireAcceptance ? "REVIEW REQUIRED" : "LEGAL DOCUMENTS";

        if (_documentTitleText != null)
            _documentTitleText.text = LegalDocuments.GetDocumentTitle(_currentDocumentType);

        if (_documentBodyText != null)
            _documentBodyText.text = LegalDocuments.GetDocumentBody(_currentDocumentType);

        if (_acceptButton != null)
        {
            _acceptButton.gameObject.SetActive(_requireAcceptance);
            Text label = _acceptButton.GetComponentInChildren<Text>();
            if (label != null)
                label.text = LegalDocuments.GetAcceptButtonLabel();
        }

        if (_closeButton != null)
        {
            _closeButton.gameObject.SetActive(!_requireAcceptance);
            Text label = _closeButton.GetComponentInChildren<Text>();
            if (label != null)
                label.text = LegalDocuments.GetCloseButtonLabel();
        }

        if (_privacyButton != null)
        {
            Text label = _privacyButton.GetComponentInChildren<Text>();
            if (label != null)
                label.text = LegalDocuments.GetPrivacyButtonLabel();

            Image image = _privacyButton.GetComponent<Image>();
            if (image != null)
                image.color = _currentDocumentType == LegalDocumentType.PrivacyPolicy
                    ? new Color(0.18f, 0.58f, 0.91f, 0.95f)
                    : new Color(1f, 1f, 1f, 0.08f);
        }

        if (_termsButton != null)
        {
            Text label = _termsButton.GetComponentInChildren<Text>();
            if (label != null)
                label.text = LegalDocuments.GetTermsButtonLabel();

            Image image = _termsButton.GetComponent<Image>();
            if (image != null)
                image.color = _currentDocumentType == LegalDocumentType.TermsOfUse
                    ? new Color(0.18f, 0.58f, 0.91f, 0.95f)
                    : new Color(1f, 1f, 1f, 0.08f);
        }

        if (_scrollRect != null)
            _scrollRect.verticalNormalizedPosition = 1f;
    }

    private void BuildUi()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(eventSystem);
        }

        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 5000;
        _canvasScaler = gameObject.AddComponent<CanvasScaler>();
        _canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        _canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        _canvasScaler.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();

        _backdrop = CreateUiObject("Backdrop", transform);
        Image backdropImage = _backdrop.AddComponent<Image>();
        backdropImage.color = new Color(0.02f, 0.04f, 0.08f, 0.9f);
        RectTransform backdropRect = _backdrop.GetComponent<RectTransform>();
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.offsetMin = Vector2.zero;
        backdropRect.offsetMax = Vector2.zero;

        GameObject panel = CreateUiObject("Panel", _backdrop.transform);
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.07f, 0.09f, 0.13f, 0.97f);
        _panelRect = panel.GetComponent<RectTransform>();
        _panelRect.anchorMin = new Vector2(0.1f, 0.08f);
        _panelRect.anchorMax = new Vector2(0.9f, 0.92f);
        _panelRect.offsetMin = Vector2.zero;
        _panelRect.offsetMax = Vector2.zero;

        _panelLayout = panel.AddComponent<VerticalLayoutGroup>();
        _panelLayout.padding = new RectOffset(28, 28, 28, 28);
        _panelLayout.spacing = 18f;
        _panelLayout.childControlHeight = true;
        _panelLayout.childControlWidth = true;
        _panelLayout.childForceExpandHeight = false;
        _panelLayout.childForceExpandWidth = true;

        _eyebrowText = CreateLabel("Eyebrow", panel.transform, font, 16, FontStyle.Bold, TextAnchor.MiddleCenter);
        _eyebrowText.color = new Color(0.47f, 0.79f, 0.98f, 1f);
        _eyebrowLayout = AddPreferredHeight(_eyebrowText.gameObject, 22f);

        _titleText = CreateLabel("Title", panel.transform, font, 34, FontStyle.Bold, TextAnchor.MiddleCenter);
        _titleLayout = AddPreferredHeight(_titleText.gameObject, 46f);

        _introText = CreateLabel("Intro", panel.transform, font, 20, FontStyle.Normal, TextAnchor.MiddleLeft);
        _introText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _introText.verticalOverflow = VerticalWrapMode.Overflow;
        _introText.color = new Color(0.85f, 0.89f, 0.95f, 0.95f);
        _introLayout = AddPreferredHeight(_introText.gameObject, 78f);

        GameObject tabsRow = CreateUiObject("TabsRow", panel.transform);
        _tabsLayout = tabsRow.AddComponent<HorizontalLayoutGroup>();
        _tabsLayout.spacing = 12f;
        _tabsLayout.childControlWidth = true;
        _tabsLayout.childControlHeight = true;
        _tabsLayout.childForceExpandWidth = true;
        _tabsLayout.childForceExpandHeight = false;
        _tabsRowLayout = AddPreferredHeight(tabsRow, 72f);

        _privacyButton = CreateActionButton("PrivacyButton", tabsRow.transform, font, () => SetCurrentDocument(LegalDocumentType.PrivacyPolicy));
        _termsButton = CreateActionButton("TermsButton", tabsRow.transform, font, () => SetCurrentDocument(LegalDocumentType.TermsOfUse));

        _documentTitleText = CreateLabel("DocumentTitle", panel.transform, font, 24, FontStyle.Bold, TextAnchor.MiddleLeft);
        _documentTitleText.color = new Color(0.97f, 0.98f, 1f, 1f);
        _documentTitleLayout = AddPreferredHeight(_documentTitleText.gameObject, 34f);

        GameObject scrollView = CreateUiObject("ScrollView", panel.transform);
        Image scrollBg = scrollView.AddComponent<Image>();
        scrollBg.color = new Color(1f, 1f, 1f, 0.045f);
        _scrollRect = scrollView.AddComponent<ScrollRect>();
        _scrollRect.horizontal = false;
        _scrollRect.vertical = true;
        _scrollRect.movementType = ScrollRect.MovementType.Clamped;
        _scrollLayout = AddFlexibleHeight(scrollView, 1f, 520f);

        RectTransform scrollRectTransform = scrollView.GetComponent<RectTransform>();
        scrollRectTransform.sizeDelta = new Vector2(0f, 520f);

        GameObject viewport = CreateUiObject("Viewport", scrollView.transform);
        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
        Mask viewportMask = viewport.AddComponent<Mask>();
        viewportMask.showMaskGraphic = false;
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = new Vector2(18f, 18f);
        viewportRect.offsetMax = new Vector2(-18f, -18f);

        GameObject content = CreateUiObject("Content", viewport.transform);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(6, 6, 0, 0);
        contentLayout.spacing = 0f;
        contentLayout.childAlignment = TextAnchor.UpperLeft;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        ContentSizeFitter contentFitter = content.AddComponent<ContentSizeFitter>();
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        _documentBodyText = CreateLabel("DocumentBody", content.transform, font, 18, FontStyle.Normal, TextAnchor.UpperLeft);
        _documentBodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _documentBodyText.verticalOverflow = VerticalWrapMode.Overflow;
        _documentBodyText.supportRichText = false;
        _documentBodyText.color = new Color(0.9f, 0.93f, 0.98f, 0.96f);
        ContentSizeFitter bodyFitter = _documentBodyText.gameObject.AddComponent<ContentSizeFitter>();
        bodyFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        bodyFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        RectTransform bodyRect = _documentBodyText.GetComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0f, 1f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.pivot = new Vector2(0.5f, 1f);
        bodyRect.offsetMin = Vector2.zero;
        bodyRect.offsetMax = Vector2.zero;
        LayoutElement bodyLayout = _documentBodyText.gameObject.AddComponent<LayoutElement>();
        bodyLayout.flexibleWidth = 1f;

        _scrollRect.viewport = viewportRect;
        _scrollRect.content = contentRect;

        GameObject bottomRow = CreateUiObject("BottomRow", panel.transform);
        _bottomLayout = bottomRow.AddComponent<HorizontalLayoutGroup>();
        _bottomLayout.spacing = 12f;
        _bottomLayout.childControlWidth = true;
        _bottomLayout.childControlHeight = true;
        _bottomLayout.childForceExpandWidth = false;
        _bottomLayout.childForceExpandHeight = false;
        _bottomLayout.childAlignment = TextAnchor.MiddleRight;
        _bottomRowLayout = AddPreferredHeight(bottomRow, 86f);

        _closeButton = CreateActionButton("CloseButton", bottomRow.transform, font, Hide);
        _acceptButton = CreateActionButton("AcceptButton", bottomRow.transform, font, AcceptAndContinue);

        ApplyResponsiveLayout(force: true);
    }

    private void SetCurrentDocument(LegalDocumentType documentType)
    {
        _currentDocumentType = documentType;
        RefreshTexts();
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static Text CreateLabel(string name, Transform parent, Font font, int fontSize, FontStyle style, TextAnchor anchor)
    {
        GameObject go = CreateUiObject(name, parent);
        Text text = go.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = anchor;
        text.color = Color.white;
        return text;
    }

    private static Button CreateActionButton(string name, Transform parent, Font font, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonGo.transform.SetParent(parent, false);

        Image image = buttonGo.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.08f);

        LayoutElement layout = buttonGo.AddComponent<LayoutElement>();
        layout.minHeight = 72f;
        layout.preferredHeight = 72f;
        layout.preferredWidth = 280f;
        layout.flexibleWidth = 0f;

        GameObject textGo = CreateUiObject("Label", buttonGo.transform);
        Text label = textGo.AddComponent<Text>();
        label.font = font;
        label.fontSize = 22;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        RectTransform textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(14f, 8f);
        textRect.offsetMax = new Vector2(-14f, -8f);

        Button button = buttonGo.GetComponent<Button>();
        button.onClick.AddListener(onClick);
        return button;
    }

    private void ApplyResponsiveLayout(bool force = false)
    {
        if (!force && _lastScreenWidth == Screen.width && _lastScreenHeight == Screen.height)
            return;

        _lastScreenWidth = Screen.width;
        _lastScreenHeight = Screen.height;

        bool landscape = Screen.width > Screen.height;
        float shortSide = Mathf.Min(Screen.width, Screen.height);
        bool compact = shortSide <= 720f;

        if (_canvasScaler != null)
        {
            _canvasScaler.referenceResolution = landscape ? new Vector2(1920f, 1080f) : new Vector2(1080f, 1920f);
            _canvasScaler.matchWidthOrHeight = landscape ? 1f : 0f;
        }

        if (_panelRect != null)
        {
            _panelRect.anchorMin = landscape ? new Vector2(0.06f, 0.06f) : new Vector2(0.04f, 0.04f);
            _panelRect.anchorMax = landscape ? new Vector2(0.94f, 0.94f) : new Vector2(0.96f, 0.96f);
        }

        if (_panelLayout != null)
        {
            int horizontalPadding = landscape ? 34 : 24;
            int verticalPadding = landscape ? 24 : 24;
            _panelLayout.padding = new RectOffset(horizontalPadding, horizontalPadding, verticalPadding, verticalPadding);
            _panelLayout.spacing = landscape ? 14f : 16f;
        }

        if (_eyebrowText != null)
            _eyebrowText.fontSize = compact ? 12 : 14;

        if (_eyebrowLayout != null)
            _eyebrowLayout.preferredHeight = compact ? 18f : 22f;

        if (_titleText != null)
            _titleText.fontSize = compact ? 26 : (landscape ? 30 : 36);

        if (_titleLayout != null)
            _titleLayout.preferredHeight = compact ? 34f : (landscape ? 42f : 48f);

        if (_introText != null)
            _introText.fontSize = compact ? 14 : (landscape ? 16 : 18);

        if (_introLayout != null)
            _introLayout.preferredHeight = compact ? 54f : (landscape ? 50f : 64f);

        if (_tabsLayout != null)
            _tabsLayout.spacing = landscape ? 10f : 12f;

        if (_tabsRowLayout != null)
            _tabsRowLayout.preferredHeight = compact ? 52f : (landscape ? 54f : 64f);

        if (_documentTitleText != null)
            _documentTitleText.fontSize = compact ? 19 : (landscape ? 22 : 24);

        if (_documentTitleLayout != null)
            _documentTitleLayout.preferredHeight = compact ? 28f : 34f;

        if (_documentBodyText != null)
            _documentBodyText.fontSize = compact ? 14 : (landscape ? 16 : 17);

        if (_scrollLayout != null)
        {
            float scrollHeight = compact ? 250f : (landscape ? 320f : 520f);
            _scrollLayout.minHeight = scrollHeight;
            _scrollLayout.preferredHeight = scrollHeight;
        }

        if (_bottomLayout != null)
            _bottomLayout.spacing = landscape ? 10f : 12f;

        if (_bottomRowLayout != null)
            _bottomRowLayout.preferredHeight = compact ? 62f : (landscape ? 64f : 78f);

        ApplyButtonStyle(_privacyButton, landscape, compact, false);
        ApplyButtonStyle(_termsButton, landscape, compact, false);
        ApplyButtonStyle(_closeButton, landscape, compact, false);
        ApplyButtonStyle(_acceptButton, landscape, compact, true);
    }

    private static void ApplyButtonStyle(Button button, bool landscape, bool compact, bool accent)
    {
        if (button == null)
            return;

        LayoutElement layout = button.GetComponent<LayoutElement>();
        if (layout != null)
        {
            float height = compact ? 58f : (landscape ? 56f : 72f);
            layout.minHeight = height;
            layout.preferredHeight = height;
            layout.preferredWidth = accent ? (landscape ? 360f : 0f) : (landscape ? 220f : 0f);
            layout.flexibleWidth = landscape ? 0f : 1f;
        }

        Image image = button.GetComponent<Image>();
        if (image != null)
            image.color = accent
                ? new Color(0.17f, 0.65f, 0.39f, 0.98f)
                : new Color(1f, 1f, 1f, 0.08f);

        Text label = button.GetComponentInChildren<Text>();
        if (label != null)
            label.fontSize = compact ? 16 : (landscape ? 18 : 20);
    }

    private static LayoutElement AddPreferredHeight(GameObject target, float preferredHeight)
    {
        LayoutElement layout = target.AddComponent<LayoutElement>();
        layout.preferredHeight = preferredHeight;
        layout.flexibleHeight = 0f;
        return layout;
    }

    private static LayoutElement AddFlexibleHeight(GameObject target, float flexibleHeight, float minHeight)
    {
        LayoutElement layout = target.AddComponent<LayoutElement>();
        layout.flexibleHeight = flexibleHeight;
        layout.minHeight = minHeight;
        layout.preferredHeight = minHeight;
        return layout;
    }
}
