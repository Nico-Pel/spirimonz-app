using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
using System;
using System.IO;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class UITitleScreen : GameBehaviour
{
    public UITitleSaveSlot[] slots;
    public string houseTutoSceneName = "HouseTuto";
    public string newGameIntroVideoFileName = "Introduction-mobile.mp4";
    public float newGameIntroStopTimeSeconds = 96f;
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
    private bool _startingNewGame;

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
            StartNewGameWithIntro();
        }
    }

    private void StartNewGameWithIntro()
    {
        if (_startingNewGame)
            return;

        _startingNewGame = true;
        UIIntroVideoOverlay.Instance.Play(newGameIntroVideoFileName, newGameIntroStopTimeSeconds, LaunchTutorialAfterIntro);
    }

    private void LaunchTutorialAfterIntro()
    {
        _startingNewGame = false;

        if (_gameManager == null)
            _gameManager = GameManager.Instance;

        _gameManager?.LoadScene(houseTutoSceneName);
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

[DisallowMultipleComponent]
public class UIIntroVideoOverlay : MonoBehaviour
{
    private const float TitleAmbientFadeOutDuration = 0.35f;
    private const float SkipButtonVisibleDuration = 4f;
    private const string SubtitleFontResourcePath = "Krungthep SDF";
    private const string SubtitleFontAssetPath = "Assets/TextMesh Pro/Fonts/Krungthep SDF.asset";

    private static UIIntroVideoOverlay _instance;

    private Canvas _canvas;
    private Image _background;
    private RawImage _videoImage;
    private TextMeshProUGUI _subtitleText;
    private Button _skipButton;
    private TextMeshProUGUI _skipButtonText;
    private RenderTexture _renderTexture;
    private VideoPlayer _videoPlayer;
    private AudioSource _audioSource;

    private Action _onFinished;
    private float _stopTimeSeconds;
    private float _skipButtonHideAtTime;
    private bool _isFinishing;
    private bool _hideAfterNextSceneLoad;
    private Coroutine _prepareCoroutine;
    private IntroSubtitleEntry[] _subtitles = Array.Empty<IntroSubtitleEntry>();

    public static UIIntroVideoOverlay Instance => EnsureExists();

    public static UIIntroVideoOverlay EnsureExists()
    {
        if (_instance != null)
            return _instance;

        GameObject root = new GameObject("UIIntroVideoOverlay");
        DontDestroyOnLoad(root);
        _instance = root.AddComponent<UIIntroVideoOverlay>();
        return _instance;
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        BuildUi();
        SetVisible(false);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        if (_isFinishing)
            return;

        UpdateSkipButtonInput();

        if (_videoPlayer == null || !_videoPlayer.isPlaying)
            return;

        UpdateSubtitles();

        if (_videoPlayer.time >= _stopTimeSeconds)
            FinishPlayback();
    }

    public void Play(string fileName, float stopTimeSeconds, Action onFinished = null)
    {
        _onFinished = onFinished;
        _stopTimeSeconds = Mathf.Max(0.1f, stopTimeSeconds);
        _isFinishing = false;
        _hideAfterNextSceneLoad = false;
        _skipButtonHideAtTime = -1f;
        _subtitles = BuildSubtitles(LanguageManager.CurrentLanguage);
        RefreshSkipButtonLabel(LanguageManager.CurrentLanguage);

        EnsureRenderTexture();
        SoundManager.Instance?.StopAmbient(TitleAmbientFadeOutDuration);

        _videoPlayer.Stop();
        _audioSource.Stop();
        _videoPlayer.source = VideoSource.Url;
        SetVisible(true);
        SetSkipButtonVisible(false);

        if (_prepareCoroutine != null)
            StopCoroutine(_prepareCoroutine);

        _prepareCoroutine = StartCoroutine(PrepareAndPlayVideo(fileName));
    }

    private void OnPrepared(VideoPlayer source)
    {
        if (_isFinishing)
            return;

        source.time = 0d;
        source.Play();
        if (_audioSource != null && source.audioOutputMode == VideoAudioOutputMode.AudioSource)
            _audioSource.Play();
    }

    private void OnLoopPointReached(VideoPlayer source)
    {
        FinishPlayback();
    }

    private void FinishPlayback()
    {
        if (_isFinishing)
            return;

        _isFinishing = true;

        if (_videoPlayer != null)
            _videoPlayer.Stop();

        if (_audioSource != null)
            _audioSource.Stop();

        if (_subtitleText != null)
            _subtitleText.text = string.Empty;

        ShowBlackScreenOnly();
        _hideAfterNextSceneLoad = _onFinished != null;

        Action callback = _onFinished;
        _onFinished = null;
        callback?.Invoke();

        if (callback == null)
            SetVisible(false);
    }

    private void BuildUi()
    {
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 10000;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();

        GameObject backgroundGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
        backgroundGo.transform.SetParent(transform, false);
        RectTransform backgroundRect = backgroundGo.GetComponent<RectTransform>();
        Stretch(backgroundRect);
        _background = backgroundGo.GetComponent<Image>();
        _background.color = Color.black;
        _background.raycastTarget = true;

        GameObject videoGo = new GameObject("Video", typeof(RectTransform), typeof(RawImage));
        videoGo.transform.SetParent(transform, false);
        RectTransform videoRect = videoGo.GetComponent<RectTransform>();
        Stretch(videoRect);
        _videoImage = videoGo.GetComponent<RawImage>();
        _videoImage.color = Color.white;
        _videoImage.raycastTarget = false;

        GameObject subtitleGo = new GameObject("Subtitle", typeof(RectTransform));
        subtitleGo.transform.SetParent(transform, false);
        RectTransform subtitleRect = subtitleGo.GetComponent<RectTransform>();
        subtitleRect.anchorMin = new Vector2(0.14f, 0.04f);
        subtitleRect.anchorMax = new Vector2(0.86f, 0.2f);
        subtitleRect.offsetMin = Vector2.zero;
        subtitleRect.offsetMax = Vector2.zero;
        subtitleRect.anchoredPosition = Vector2.zero;
        subtitleRect.localScale = Vector3.one;

        _subtitleText = subtitleGo.AddComponent<TextMeshProUGUI>();
        _subtitleText.text = string.Empty;
        _subtitleText.alignment = TextAlignmentOptions.BottomGeoAligned;
        _subtitleText.fontSize = 46f;
        _subtitleText.enableWordWrapping = true;
        _subtitleText.overflowMode = TextOverflowModes.Overflow;
        _subtitleText.color = Color.white;
        _subtitleText.outlineWidth = 0.2f;
        _subtitleText.outlineColor = new Color(0f, 0f, 0f, 0.95f);
        _subtitleText.raycastTarget = false;
        _subtitleText.font = ResolveSubtitleFont();

        Shadow subtitleShadow = subtitleGo.AddComponent<Shadow>();
        subtitleShadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
        subtitleShadow.effectDistance = new Vector2(3f, -3f);
        subtitleShadow.useGraphicAlpha = true;

        GameObject skipGo = new GameObject("SkipButton", typeof(RectTransform), typeof(Image), typeof(Button));
        skipGo.transform.SetParent(transform, false);
        RectTransform skipRect = skipGo.GetComponent<RectTransform>();
        skipRect.anchorMin = new Vector2(1f, 1f);
        skipRect.anchorMax = new Vector2(1f, 1f);
        skipRect.pivot = new Vector2(1f, 1f);
        skipRect.sizeDelta = new Vector2(220f, 84f);
        skipRect.anchoredPosition = new Vector2(-48f, -42f);

        Image skipImage = skipGo.GetComponent<Image>();
        skipImage.color = new Color(0f, 0f, 0f, 0.7f);
        skipImage.raycastTarget = true;

        _skipButton = skipGo.GetComponent<Button>();
        ColorBlock skipColors = _skipButton.colors;
        skipColors.normalColor = Color.white;
        skipColors.highlightedColor = new Color(1f, 1f, 1f, 0.95f);
        skipColors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        skipColors.selectedColor = Color.white;
        _skipButton.colors = skipColors;
        _skipButton.onClick.AddListener(OnSkipButtonPressed);

        GameObject skipTextGo = new GameObject("Text", typeof(RectTransform));
        skipTextGo.transform.SetParent(skipGo.transform, false);
        RectTransform skipTextRect = skipTextGo.GetComponent<RectTransform>();
        Stretch(skipTextRect);

        _skipButtonText = skipTextGo.AddComponent<TextMeshProUGUI>();
        _skipButtonText.text = GetSkipLabel(LanguageManager.CurrentLanguage);
        _skipButtonText.font = ResolveSubtitleFont();
        _skipButtonText.fontSize = 38f;
        _skipButtonText.alignment = TextAlignmentOptions.Center;
        _skipButtonText.color = Color.white;
        _skipButtonText.outlineWidth = 0.18f;
        _skipButtonText.outlineColor = new Color(0f, 0f, 0f, 0.95f);
        _skipButtonText.raycastTarget = false;

        Shadow skipTextShadow = skipTextGo.AddComponent<Shadow>();
        skipTextShadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
        skipTextShadow.effectDistance = new Vector2(2f, -2f);
        skipTextShadow.useGraphicAlpha = true;

        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.loop = false;
        _audioSource.spatialBlend = 0f;

        _videoPlayer = gameObject.AddComponent<VideoPlayer>();
        _videoPlayer.playOnAwake = false;
        _videoPlayer.isLooping = false;
        _videoPlayer.skipOnDrop = true;
        _videoPlayer.waitForFirstFrame = true;
        _videoPlayer.renderMode = VideoRenderMode.RenderTexture;
#if UNITY_ANDROID && !UNITY_EDITOR
        _videoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
#else
        _videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        _videoPlayer.SetTargetAudioSource(0, _audioSource);
#endif
        _videoPlayer.prepareCompleted += OnPrepared;
        _videoPlayer.loopPointReached += OnLoopPointReached;
        _videoPlayer.errorReceived += OnVideoErrorReceived;
    }

    private void EnsureRenderTexture()
    {
        int width = Mathf.Max(1280, Screen.width);
        int height = Mathf.Max(720, Screen.height);

        if (_renderTexture != null &&
            _renderTexture.width == width &&
            _renderTexture.height == height)
            return;

        if (_renderTexture != null)
            _renderTexture.Release();

        _renderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
        _renderTexture.Create();

        if (_videoPlayer != null)
            _videoPlayer.targetTexture = _renderTexture;

        if (_videoImage != null)
            _videoImage.texture = _renderTexture;
    }

    private System.Collections.IEnumerator PrepareAndPlayVideo(string fileName)
    {
        string resolvedPath = null;

#if UNITY_ANDROID && !UNITY_EDITOR
        yield return StartCoroutine(CopyStreamingAssetToPersistentPath(fileName, path => resolvedPath = path));
#else
        resolvedPath = ResolveVideoPath(fileName);
#endif

        _prepareCoroutine = null;

        if (_isFinishing)
            yield break;

        if (string.IsNullOrEmpty(resolvedPath))
        {
            Action callback = _onFinished;
            _onFinished = null;
            SetVisible(false);
            callback?.Invoke();
            yield break;
        }

        _videoPlayer.url = resolvedPath;
        _videoPlayer.Prepare();
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private System.Collections.IEnumerator CopyStreamingAssetToPersistentPath(string fileName, Action<string> onResolved)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            onResolved?.Invoke(null);
            yield break;
        }

        string persistentDir = Path.Combine(Application.persistentDataPath, "intro-cache");
        string persistentPath = Path.Combine(persistentDir, fileName);

        if (File.Exists(persistentPath))
        {
            onResolved?.Invoke(persistentPath);
            yield break;
        }

        Directory.CreateDirectory(persistentDir);

        string streamingAssetsPath = $"{Application.streamingAssetsPath}/{fileName}";
        using UnityWebRequest request = UnityWebRequest.Get(streamingAssetsPath);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"Intro video download failed: {request.error}");
            onResolved?.Invoke(null);
            yield break;
        }

        try
        {
            File.WriteAllBytes(persistentPath, request.downloadHandler.data);
            onResolved?.Invoke(ToVideoPlayerUrl(persistentPath));
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Intro video cache write failed: {exception.Message}");
            onResolved?.Invoke(null);
        }
    }
#endif

    private static string ResolveVideoPath(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return null;

        string streamingAssetsPath = Path.Combine(Application.streamingAssetsPath, fileName);
        if (File.Exists(streamingAssetsPath))
            return ToVideoPlayerUrl(streamingAssetsPath);

#if UNITY_EDITOR
        string editorAssetPath = Path.Combine(Application.dataPath, "Video", fileName);
        if (File.Exists(editorAssetPath))
            return ToVideoPlayerUrl(editorAssetPath);
#endif

        Debug.LogWarning($"Intro video not found: {fileName}");
        return null;
    }

    private static string ToVideoPlayerUrl(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        if (path.Contains("://"))
            return path;

        return $"file://{path.Replace("\\", "/")}";
    }

    private void OnVideoErrorReceived(VideoPlayer source, string message)
    {
        Debug.LogWarning($"Intro video error: {message}");
        FinishPlayback();
    }

    private static TMP_FontAsset ResolveSubtitleFont()
    {
        TMP_FontAsset font = Resources.Load<TMP_FontAsset>(SubtitleFontResourcePath);
        if (font != null)
            return font;

        TMP_FontAsset[] loadedFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        for (int i = 0; i < loadedFonts.Length; i++)
        {
            TMP_FontAsset candidate = loadedFonts[i];
            if (candidate != null && string.Equals(candidate.name, "Krungthep SDF", StringComparison.OrdinalIgnoreCase))
                return candidate;
        }

#if UNITY_EDITOR
        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(SubtitleFontAssetPath);
        if (font != null)
            return font;
#endif

        return TMP_Settings.defaultFontAsset;
    }

    private void SetVisible(bool visible)
    {
        if (_canvas != null)
            _canvas.enabled = visible;

        if (_background != null)
            _background.enabled = visible;

        if (_videoImage != null)
            _videoImage.enabled = visible;

        if (_subtitleText != null)
            _subtitleText.enabled = visible;

        if (!visible)
            SetSkipButtonVisible(false);
    }

    private void ShowBlackScreenOnly()
    {
        if (_canvas != null)
            _canvas.enabled = true;

        if (_background != null)
            _background.enabled = true;

        if (_videoImage != null)
            _videoImage.enabled = false;

        if (_subtitleText != null)
            _subtitleText.enabled = false;

        SetSkipButtonVisible(false);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!_hideAfterNextSceneLoad)
            return;

        _hideAfterNextSceneLoad = false;
        StartCoroutine(HideOverlayAtEndOfFrame());
    }

    private System.Collections.IEnumerator HideOverlayAtEndOfFrame()
    {
        yield return null;
        SetVisible(false);
    }

    private void UpdateSubtitles()
    {
        if (_subtitleText == null || _videoPlayer == null)
            return;

        double time = _videoPlayer.time;
        for (int i = 0; i < _subtitles.Length; i++)
        {
            IntroSubtitleEntry entry = _subtitles[i];
            if (time >= entry.startTime && time < entry.endTime)
            {
                if (!string.Equals(_subtitleText.text, entry.text, StringComparison.Ordinal))
                    _subtitleText.text = entry.text;
                return;
            }
        }

        if (!string.IsNullOrEmpty(_subtitleText.text))
            _subtitleText.text = string.Empty;
    }

    private static IntroSubtitleEntry[] BuildSubtitles(Language language)
    {
        switch (language)
        {
            case Language.French:
                return CreateSubtitleEntries(
                    "Il y a bien longtemps, un étrange phénomène a\nbouleversé la région. Le soleil a cessé de se lever.",
                    "Avec le temps, les habitants ont appris à vivre\ndans cette nuit sans fin. Mais quelque chose d'autre",
                    "a changé leur vie à jamais. De mystérieuses créatures\nont commencé à apparaître, entrant dans la vie",
                    "de chaque famille et en faisant rapidement partie.\nOn les appelait les Spirimonz. Esprits doux et bienveillants,",
                    "ils étaient naturellement attirés par les humains.\nEt après des années passées côte à côte, les humains",
                    "et les Spirimonz ne pouvaient plus vivre les uns\nsans les autres. Mais tandis que les générations humaines passent,",
                    "les Spirimonz sont éternels, et ils ne peuvent supporter\nl'idée de perdre ceux qu'ils aiment. Quand leurs proches",
                    "disparaissent et qu'ils se retrouvent seuls, les Spirimonz\nse mettent à pleurer. Leur chagrin s'imprègne",
                    "dans les murs des maisons vides qu'ils laissent derrière eux,\net peu à peu ils deviennent autre chose. Des esprits malveillants.",
                    "Ces êtres agités hantent les lieux abandonnés.",
                    "Certains humains ont développé un talent rare et puissant.\nLa capacité d'entrer dans ces maisons hantées",
                    "et de rendre à ces esprits perdus ce qu'ils étaient autrefois,\nen les affrontant avec courage et en découvrant",
                    "la véritable nature du Spirimonz cachée sous\nle monstre. On les appelle les chasseurs.");

            case Language.Spanish:
                return CreateSubtitleEntries(
                    "Hace muchos años, un extraño fenómeno sacudió\nla región. El sol dejó de salir. Con el tiempo,",
                    "la gente aprendió a vivir en esta noche interminable.\nPero algo más cambió sus vidas",
                    "para siempre. Misteriosas criaturas comenzaron a aparecer,\nentrando en la vida de cada familia y",
                    "convirtiéndose rápidamente en parte de ella.\nSe las llamó Spirimonz. Espíritus amables y gentiles,",
                    "se sentían atraídos de forma natural por los humanos.\nY tras años viviendo lado a lado, los humanos",
                    "y los Spirimonz ya no podían vivir unos sin otros.\nPero mientras las generaciones humanas pasan,",
                    "los Spirimonz son eternos y no soportan\nla idea de perder a quienes aman. Cuando sus seres queridos",
                    "desaparecen y se quedan solos, los Spirimonz comienzan\na llorar. Su tristeza se filtra en las",
                    "paredes de los hogares vacíos que dejan atrás, y\npoco a poco se convierten en otra cosa. Espíritus oscuros.",
                    "Estos seres inquietos atormentan los lugares abandonados.",
                    "Algunos humanos han desarrollado una habilidad rara y poderosa.\nLa capacidad de entrar en estas casas embrujadas",
                    "y devolver a estos espíritus perdidos lo que fueron,\nafrontándolos con valentía y descubriendo la",
                    "verdadera naturaleza del Spirimonz oculta bajo\nel monstruo. Son conocidos como cazadores.");

            case Language.Portuguese:
                return CreateSubtitleEntries(
                    "Há muitos anos, um estranho fenômeno abalou\na região. O sol parou de nascer. Com o tempo,",
                    "as pessoas aprenderam a viver nessa noite sem fim.\nMas outra coisa mudou suas vidas",
                    "para sempre. Criaturas misteriosas começaram a aparecer,\nentrando na vida de cada família e",
                    "logo se tornando parte dela.\nElas foram chamadas de Spirimonz. Espíritos gentis e bondosos,",
                    "eram naturalmente atraídos pelos humanos.\nE, após anos vivendo lado a lado, humanos",
                    "e Spirimonz já não conseguiam viver uns sem os outros.\nMas, enquanto as gerações humanas passam,",
                    "os Spirimonz são eternos e não suportam\na ideia de perder aqueles que amam. Quando seus entes queridos",
                    "se vão e eles ficam sozinhos, os Spirimonz\ncomeçam a chorar. Sua tristeza se infiltra nas",
                    "paredes das casas vazias que deixam para trás, e\nlentamente eles se tornam outra coisa. Espíritos sombrios.",
                    "Esses seres inquietos assombram lugares abandonados.",
                    "Alguns humanos desenvolveram uma habilidade rara e poderosa.\nA capacidade de entrar nessas casas assombradas",
                    "e restaurar esses espíritos perdidos ao que eram antes,\nenfrentando-os com coragem e descobrindo a",
                    "verdadeira natureza do Spirimonz escondida sob\no monstro. Eles são conhecidos como caçadores.");

            case Language.Polish:
                return CreateSubtitleEntries(
                    "Wiele lat temu dziwne zjawisko wstrząsnęło\ncałym regionem. Słońce przestało wschodzić.",
                    "Z czasem ludzie nauczyli się żyć w tej niekończącej się nocy.\nAle coś jeszcze zmieniło ich życie",
                    "na zawsze. Tajemnicze stworzenia zaczęły się pojawiać,\nwchodząc do życia każdej rodziny i",
                    "szybko stając się jej częścią.\nNazywano je Spirimonz. Łagodne i życzliwe duchy,",
                    "naturalnie lgnęły do ludzi.\nA po latach życia ramię w ramię ludzie",
                    "i Spirimonzy nie mogli już bez siebie żyć.\nAle podczas gdy ludzkie pokolenia przemijają,",
                    "Spirimonzy są wieczne i nie potrafią znieść\nmyśli o utracie tych, których kochają. Gdy ich bliscy",
                    "odchodzą i zostają same, Spirimonzy\nzaczynają płakać. Ich smutek wsiąka w",
                    "ściany pustych domów, które po sobie zostawiają,\ni powoli stają się czymś innym. Mrocznymi duchami.",
                    "Te niespokojne istoty nawiedzają opuszczone miejsca.",
                    "Niektórzy ludzie rozwinęli rzadką i potężną umiejętność.\nZdolność wchodzenia do tych nawiedzonych domów",
                    "i przywracania zagubionym duchom tego, czym kiedyś były,\nodważnie stawiając im czoła i odkrywając",
                    "prawdziwą naturę Spirimonza ukrytą pod\npotworem. Nazywa się ich łowcami.");

            case Language.Turkish:
                return CreateSubtitleEntries(
                    "Uzun yıllar önce, tuhaf bir olay tüm\nbölgeyi sarstı. Güneş doğmayı bıraktı.",
                    "Zamanla insanlar bu sonsuz gecede yaşamayı öğrendi.\nAma hayatlarını değiştiren başka bir şey daha oldu",
                    "sonsuzca. Gizemli yaratıklar ortaya çıkmaya başladı,\nher ailenin hayatına girip",
                    "hızla onun bir parçası oldular.\nOnlara Spirimonz dendi. Nazik ve iyi kalpli ruhlar olarak,",
                    "insanlara doğal olarak çekiliyorlardı.\nVe yıllar boyunca yan yana yaşadıktan sonra insanlar",
                    "ve Spirimonz artık birbirleri olmadan yaşayamaz hale geldi.\nAma insan nesilleri gelip geçerken,",
                    "Spirimonz sonsuzdur ve sevdiklerini kaybetme\ndüşüncesine bile dayanamazlar. Sevdikleri kişiler",
                    "yok olduğunda ve geride yalnız kaldıklarında, Spirimonz\nağlamaya başlar. Kederleri",
                    "geride bıraktıkları boş evlerin duvarlarına işler ve\nyavaşça başka bir şeye dönüşürler. Karanlık ruhlara.",
                    "Bu huzursuz varlıklar terk edilmiş yerleri musallat eder.",
                    "Bazı insanlar nadir ve güçlü bir yetenek geliştirdi.\nBu lanetli evlere girme yeteneği",
                    "ve bu kayıp ruhları, onlarla cesurca yüzleşip\naltındaki gerçeği ortaya çıkararak eski hallerine döndürme",
                    "canavarın altında saklanan Spirimonz'un\ngerçek doğasını. Onlara avcı denir.");

            case Language.Indonesian:
                return CreateSubtitleEntries(
                    "Bertahun-tahun lalu, sebuah fenomena aneh mengguncang\nwilayah ini. Matahari berhenti terbit.",
                    "Seiring waktu, orang-orang belajar hidup\ndalam malam yang tak berujung ini. Tapi ada hal lain",
                    "yang mengubah hidup mereka selamanya.\nMakhluk-makhluk misterius mulai muncul,",
                    "masuk ke kehidupan setiap keluarga dan dengan cepat\nmenjadi bagian darinya. Mereka disebut Spirimonz.",
                    "Roh yang baik dan lembut,\nmereka secara alami tertarik pada manusia.",
                    "Dan setelah bertahun-tahun hidup berdampingan, manusia\ndan Spirimonz tak lagi bisa hidup",
                    "tanpa satu sama lain. Namun sementara generasi manusia\nterus datang dan pergi, Spirimonz bersifat abadi",
                    "dan mereka tak sanggup menghadapi kehilangan orang-orang\nyang mereka cintai. Saat orang-orang terkasih itu tiada",
                    "dan mereka tertinggal sendirian, Spirimonz\nmulai menangis. Kesedihan mereka meresap ke",
                    "dinding rumah kosong yang mereka tinggalkan, dan\nperlahan mereka menjadi sesuatu yang lain. Roh gelap.",
                    "Makhluk-makhluk gelisah ini menghantui tempat-tempat terlantar.",
                    "Sebagian manusia mengembangkan kemampuan yang langka dan kuat.\nKemampuan untuk memasuki rumah-rumah berhantu ini dan mengembalikan roh-roh",
                    "yang hilang ini ke diri mereka semula,\ndengan berani menghadapi mereka dan mengungkap jati diri Spirimonz yang sebenarnya yang tersembunyi\ndi balik monster itu. Mereka dikenal sebagai pemburu.");

            case Language.Italian:
                return CreateSubtitleEntries(
                    "Molti anni fa, uno strano fenomeno sconvolse\nla regione. Il sole smise di sorgere.",
                    "Con il tempo, le persone impararono a vivere\nin questa notte senza fine. Ma qualcos'altro cambiò le loro vite",
                    "per sempre. Misteriose creature iniziarono ad apparire,\nentrando nella vita di ogni famiglia e",
                    "diventandone rapidamente parte.\nFurono chiamate Spirimonz. Spiriti gentili e benevoli,",
                    "erano naturalmente attratti dagli esseri umani.\nE dopo anni vissuti fianco a fianco, umani",
                    "e Spirimonz non potevano più vivere l'uno senza l'altro.\nMa mentre le generazioni umane passano,",
                    "gli Spirimonz sono eterni e non riescono a sopportare\nl'idea di perdere chi amano. Quando i loro cari",
                    "se ne vanno e loro restano soli, gli Spirimonz\niniziano a piangere. Il loro dolore si insinua nelle",
                    "pareti delle case vuote che si lasciano alle spalle e,\npiano piano, diventano qualcos'altro. Spiriti oscuri.",
                    "Questi esseri inquieti infestano i luoghi abbandonati.",
                    "Alcuni umani hanno sviluppato un'abilità rara e potente.\nLa capacità di entrare in queste case infestate",
                    "e riportare questi spiriti perduti a ciò che erano un tempo,\naffrontandoli con coraggio e scoprendo la",
                    "vera natura dello Spirimonz nascosta sotto\nil mostro. Sono conosciuti come cacciatori.");

            case Language.German:
                return CreateSubtitleEntries(
                    "Vor vielen Jahren erschütterte ein seltsames Phänomen\ndie ganze Region. Die Sonne hörte auf aufzugehen.",
                    "Mit der Zeit lernten die Menschen, in dieser endlosen\nNacht zu leben. Doch noch etwas anderes veränderte ihr Leben",
                    "für immer. Mysteriöse Kreaturen begannen zu erscheinen,\ntraten in das Leben jeder Familie und",
                    "wurden schnell ein Teil davon.\nMan nannte sie Spirimonz. Freundliche und sanfte Geister,",
                    "sie fühlten sich ganz natürlich zu den Menschen hingezogen.\nUnd nach Jahren des Zusammenlebens konnten Menschen",
                    "und Spirimonz nicht mehr ohneeinander leben.\nDoch während die Generationen der Menschen vergehen,",
                    "sind die Spirimonz ewig und können den Gedanken,\ndie Menschen zu verlieren, die sie lieben, nicht ertragen. Wenn ihre Liebsten",
                    "verschwinden und sie allein zurückbleiben, beginnen die Spirimonz\nzu weinen. Ihre Trauer sickert in die",
                    "Wände der leeren Häuser, die sie zurücklassen, und\nlangsam werden sie zu etwas anderem. Dunkle Geister.",
                    "Diese rastlosen Wesen suchen verlassene Orte heim.",
                    "Manche Menschen haben eine seltene und mächtige Fähigkeit entwickelt.\nDie Fähigkeit, diese heimgesuchten Häuser zu betreten",
                    "und diese verlorenen Geister in das zurückzuverwandeln,\nwas sie einst waren, indem sie sich ihnen mutig stellen und die",
                    "wahre Natur des Spirimonz entdecken, die unter\ndem Monster verborgen liegt. Man nennt sie Jäger.");

            default:
                return CreateSubtitleEntries(
                    "Many years ago, a strange phenomenon shook\nthe region. The sun stopped rising. Over time,",
                    "the people learned to live in this endless\nnight. But something else changed their lives",
                    "forever. Mysterious creatures began to appear,\nentering the lives of every family and quickly",
                    "becoming part of them. They were called\nthe Spirimonz. kind and gentle spirits,",
                    "they were naturally drawn to humans. And\nafter years of living side by side, humans",
                    "and Spirimonz could no longer live without one\nanother. But while human generations come and go,",
                    "the Spirimonz are eternal, and they cannot bear\nthe thought of losing the ones they love. When their loved ones",
                    "are gone and they are left behind alone, the Spirimonz\nbegin to weep. Their sorrow seeps into the",
                    "walls of the empty homes they leave behind, and\nslowly they become something else. Dark spirits.",
                    "These restless beings haunt abandoned places.",
                    "Some humans have developed a rare and powerful\nskill. The ability to enter these haunted homes",
                    "and restore these lost spirits to what they once\nwere by bravely facing them and uncovering the",
                    "true nature of the Spirimonz hidden beneath\nthe monster. They are known as hunters.");
        }
    }

    private void RefreshSkipButtonLabel(Language language)
    {
        if (_skipButtonText == null)
            return;

        _skipButtonText.text = GetSkipLabel(language);
    }

    private static string GetSkipLabel(Language language)
    {
        switch (language)
        {
            case Language.French:
                return "Passer";
            case Language.Spanish:
                return "Saltar";
            case Language.Portuguese:
                return "Pular";
            case Language.Polish:
                return "Pomin";
            case Language.Turkish:
                return "Gec";
            case Language.Indonesian:
                return "Lewati";
            case Language.Italian:
                return "Salta";
            case Language.German:
                return "Uberspringen";
            default:
                return "Skip";
        }
    }

    private void UpdateSkipButtonInput()
    {
        if (_videoPlayer == null || !_videoPlayer.isPlaying)
            return;

        if (WasPointerPressedThisFrame())
            ShowSkipButtonTemporarily();

        if (_skipButton != null && _skipButton.gameObject.activeSelf && _skipButtonHideAtTime > 0f && Time.unscaledTime >= _skipButtonHideAtTime)
            SetSkipButtonVisible(false);
    }

    private static bool WasPointerPressedThisFrame()
    {
        if (Input.GetMouseButtonDown(0))
            return true;

        if (Input.touchCount <= 0)
            return false;

        for (int i = 0; i < Input.touchCount; i++)
        {
            if (Input.GetTouch(i).phase == TouchPhase.Began)
                return true;
        }

        return false;
    }

    private void ShowSkipButtonTemporarily()
    {
        SetSkipButtonVisible(true);
        _skipButtonHideAtTime = Time.unscaledTime + SkipButtonVisibleDuration;
    }

    private void SetSkipButtonVisible(bool visible)
    {
        if (_skipButton != null)
            _skipButton.gameObject.SetActive(visible);

        _skipButtonHideAtTime = visible ? _skipButtonHideAtTime : -1f;
    }

    private void OnSkipButtonPressed()
    {
        if (_isFinishing || _videoPlayer == null || !_videoPlayer.isPlaying)
            return;

        FinishPlayback();
    }

    private static IntroSubtitleEntry[] CreateSubtitleEntries(
        string line1,
        string line2,
        string line3,
        string line4,
        string line5,
        string line6,
        string line7,
        string line8,
        string line9,
        string line10,
        string line11,
        string line12,
        string line13)
    {
        return new[]
        {
            new IntroSubtitleEntry(0.560, 8.880, line1),
            new IntroSubtitleEntry(8.880, 14.880, line2),
            new IntroSubtitleEntry(14.880, 21.920, line3),
            new IntroSubtitleEntry(21.920, 28.960, line4),
            new IntroSubtitleEntry(28.960, 35.200, line5),
            new IntroSubtitleEntry(35.200, 42.160, line6),
            new IntroSubtitleEntry(42.160, 50.480, line7),
            new IntroSubtitleEntry(50.480, 58.880, line8),
            new IntroSubtitleEntry(58.880, 69.840, line9),
            new IntroSubtitleEntry(69.840, 73.200, line10),
            new IntroSubtitleEntry(73.840, 80.160, line11),
            new IntroSubtitleEntry(80.160, 87.280, line12),
            new IntroSubtitleEntry(87.280, 98.240, line13)
        };
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private readonly struct IntroSubtitleEntry
    {
        public readonly double startTime;
        public readonly double endTime;
        public readonly string text;

        public IntroSubtitleEntry(double startTime, double endTime, string text)
        {
            this.startTime = startTime;
            this.endTime = endTime;
            this.text = text;
        }
    }
}
