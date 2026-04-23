using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UILegalOverlay : MonoBehaviour
{
    private static UILegalOverlay _instance;

    private Canvas _canvas;
    private GameObject _backdrop;
    private RectTransform _panelRect;
    private Text _titleText;
    private Text _introText;
    private Text _documentTitleText;
    private Text _documentBodyText;
    private Button _privacyButton;
    private Button _termsButton;
    private Button _acceptButton;
    private Button _closeButton;
    private Button _quitButton;
    private ScrollRect _scrollRect;

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

    private void QuitGame()
    {
        Application.Quit();
    }

    private void HandleLanguageChanged(Language language)
    {
        if (_canvas != null && _canvas.enabled)
            RefreshTexts();
    }

    private void RefreshTexts()
    {
        if (_titleText != null)
            _titleText.text = LegalDocuments.GetWindowTitle(_requireAcceptance);

        if (_introText != null)
            _introText.text = LegalDocuments.GetIntroText(_requireAcceptance);

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

        if (_quitButton != null)
        {
            _quitButton.gameObject.SetActive(_requireAcceptance);
            Text label = _quitButton.GetComponentInChildren<Text>();
            if (label != null)
                label.text = LegalDocuments.GetQuitButtonLabel();
        }

        if (_privacyButton != null)
        {
            Text label = _privacyButton.GetComponentInChildren<Text>();
            if (label != null)
                label.text = LegalDocuments.GetPrivacyButtonLabel();
        }

        if (_termsButton != null)
        {
            Text label = _termsButton.GetComponentInChildren<Text>();
            if (label != null)
                label.text = LegalDocuments.GetTermsButtonLabel();
        }

        if (_scrollRect != null)
            _scrollRect.verticalNormalizedPosition = 1f;
    }

    private void BuildUi()
    {
        Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            DontDestroyOnLoad(eventSystem);
        }

        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 5000;
        gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        gameObject.AddComponent<GraphicRaycaster>();

        _backdrop = CreateUiObject("Backdrop", transform);
        Image backdropImage = _backdrop.AddComponent<Image>();
        backdropImage.color = new Color(0f, 0f, 0f, 0.82f);
        RectTransform backdropRect = _backdrop.GetComponent<RectTransform>();
        backdropRect.anchorMin = Vector2.zero;
        backdropRect.anchorMax = Vector2.one;
        backdropRect.offsetMin = Vector2.zero;
        backdropRect.offsetMax = Vector2.zero;

        GameObject panel = CreateUiObject("Panel", _backdrop.transform);
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.08f, 0.1f, 0.15f, 0.98f);
        _panelRect = panel.GetComponent<RectTransform>();
        _panelRect.anchorMin = new Vector2(0.1f, 0.08f);
        _panelRect.anchorMax = new Vector2(0.9f, 0.92f);
        _panelRect.offsetMin = Vector2.zero;
        _panelRect.offsetMax = Vector2.zero;

        VerticalLayoutGroup panelLayout = panel.AddComponent<VerticalLayoutGroup>();
        panelLayout.padding = new RectOffset(28, 28, 28, 28);
        panelLayout.spacing = 18f;
        panelLayout.childControlHeight = true;
        panelLayout.childControlWidth = true;
        panelLayout.childForceExpandHeight = false;
        panelLayout.childForceExpandWidth = true;

        _titleText = CreateLabel("Title", panel.transform, font, 34, FontStyle.Bold, TextAnchor.MiddleCenter);
        AddPreferredHeight(_titleText.gameObject, 46f);

        _introText = CreateLabel("Intro", panel.transform, font, 20, FontStyle.Normal, TextAnchor.MiddleLeft);
        _introText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _introText.verticalOverflow = VerticalWrapMode.Overflow;
        AddPreferredHeight(_introText.gameObject, 78f);

        GameObject tabsRow = CreateUiObject("TabsRow", panel.transform);
        HorizontalLayoutGroup tabsLayout = tabsRow.AddComponent<HorizontalLayoutGroup>();
        tabsLayout.spacing = 12f;
        tabsLayout.childControlWidth = true;
        tabsLayout.childControlHeight = true;
        tabsLayout.childForceExpandWidth = true;
        tabsLayout.childForceExpandHeight = false;
        AddPreferredHeight(tabsRow, 72f);

        _privacyButton = CreateActionButton("PrivacyButton", tabsRow.transform, font, () => SetCurrentDocument(LegalDocumentType.PrivacyPolicy));
        _termsButton = CreateActionButton("TermsButton", tabsRow.transform, font, () => SetCurrentDocument(LegalDocumentType.TermsOfUse));

        _documentTitleText = CreateLabel("DocumentTitle", panel.transform, font, 24, FontStyle.Bold, TextAnchor.MiddleLeft);
        AddPreferredHeight(_documentTitleText.gameObject, 34f);

        GameObject scrollView = CreateUiObject("ScrollView", panel.transform);
        Image scrollBg = scrollView.AddComponent<Image>();
        scrollBg.color = new Color(1f, 1f, 1f, 0.06f);
        _scrollRect = scrollView.AddComponent<ScrollRect>();
        _scrollRect.horizontal = false;
        _scrollRect.vertical = true;
        _scrollRect.movementType = ScrollRect.MovementType.Clamped;
        AddFlexibleHeight(scrollView, 1f, 520f);

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
        viewportRect.offsetMin = new Vector2(12f, 12f);
        viewportRect.offsetMax = new Vector2(-12f, -12f);

        GameObject content = CreateUiObject("Content", viewport.transform);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;

        ContentSizeFitter contentFitter = content.AddComponent<ContentSizeFitter>();
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        _documentBodyText = CreateLabel("DocumentBody", content.transform, font, 18, FontStyle.Normal, TextAnchor.UpperLeft);
        _documentBodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _documentBodyText.verticalOverflow = VerticalWrapMode.Overflow;
        ContentSizeFitter bodyFitter = _documentBodyText.gameObject.AddComponent<ContentSizeFitter>();
        bodyFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        bodyFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        RectTransform bodyRect = _documentBodyText.GetComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0f, 1f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.pivot = new Vector2(0.5f, 1f);
        bodyRect.offsetMin = new Vector2(0f, 0f);
        bodyRect.offsetMax = new Vector2(0f, 0f);

        _scrollRect.viewport = viewportRect;
        _scrollRect.content = contentRect;

        GameObject bottomRow = CreateUiObject("BottomRow", panel.transform);
        HorizontalLayoutGroup bottomLayout = bottomRow.AddComponent<HorizontalLayoutGroup>();
        bottomLayout.spacing = 12f;
        bottomLayout.childControlWidth = true;
        bottomLayout.childControlHeight = true;
        bottomLayout.childForceExpandWidth = true;
        bottomLayout.childForceExpandHeight = false;
        AddPreferredHeight(bottomRow, 86f);

        _quitButton = CreateActionButton("QuitButton", bottomRow.transform, font, QuitGame);
        _closeButton = CreateActionButton("CloseButton", bottomRow.transform, font, Hide);
        _acceptButton = CreateActionButton("AcceptButton", bottomRow.transform, font, AcceptAndContinue);
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
        image.color = new Color(1f, 1f, 1f, 0.16f);

        LayoutElement layout = buttonGo.AddComponent<LayoutElement>();
        layout.minHeight = 72f;
        layout.preferredHeight = 72f;
        layout.flexibleWidth = 1f;

        GameObject textGo = CreateUiObject("Label", buttonGo.transform);
        Text label = textGo.AddComponent<Text>();
        label.font = font;
        label.fontSize = 22;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        RectTransform textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Button button = buttonGo.GetComponent<Button>();
        button.onClick.AddListener(onClick);
        return button;
    }

    private static void AddPreferredHeight(GameObject target, float preferredHeight)
    {
        LayoutElement layout = target.AddComponent<LayoutElement>();
        layout.preferredHeight = preferredHeight;
        layout.flexibleHeight = 0f;
    }

    private static void AddFlexibleHeight(GameObject target, float flexibleHeight, float minHeight)
    {
        LayoutElement layout = target.AddComponent<LayoutElement>();
        layout.flexibleHeight = flexibleHeight;
        layout.minHeight = minHeight;
        layout.preferredHeight = minHeight;
    }
}
