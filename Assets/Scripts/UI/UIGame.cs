using System;
using UnityEngine;
using TMPro;
using UnityEngine.Serialization;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class UIGame : UIManager
{
    public static UIGame Instance { get; private set; }
    public static event Action<UIEndGame.EndTypes> OnEndGameOpened;

    public float openSceneFadeDuration = 3f;

    [Space] [Header("Money elements")]
    public TextMeshProUGUI tGold;
    public GameObject uiMoney;
    public bool forceShowMoney = true;
    
    [Space]
    
    [Header("Cursor elements")]
    public GameObject pointerBase;
    public Image pointerON;
    public TextMeshProUGUI tGrab;
    
    [Space]
    
    [Header("Window elements")]
    public UITablet tablet;
    public UIDialogue uiDialogue;
    public UISettingsMenu settingsMenu;
    public UnityEvent onJournalOpened;

    [Space]

    [Header("House Loading")]
    public float houseLoadingFadeOutDuration = 0.35f;
    public float houseLoadingBlinkDuration = 0.45f;
    public Vector2 houseLoadingIconSize = new Vector2(84f, 84f);
    public Vector2 houseLoadingIconMargin = new Vector2(52f, 52f);
    public float mobileGrabTextScale = 3f;

    private Sprite _baseBigPointer;
    private GameManager _gameManager;
    private RectTransform _moneyRect;
    private Vector2 _moneyBasePos;
    private bool _moneyBaseCached;
    private Rect _lastMoneySafeArea;
    private Vector2Int _lastMoneyScreenSize;
    private Canvas _canvas;
    private bool _lastMoneyVisibility;
    private bool _moneyVisibilityCached;
    private Image _houseLoadingIcon;
    private Tween _houseLoadingBlinkTween;
    private bool _houseLoadingScreenActive;
    private RectTransform _grabRect;
    private Vector2 _grabBaseSize;
    private float _grabBaseFontSize;
    private bool _grabLayoutCached;
    private bool _captureUiHidden;

    public bool IsBlockingHouseLoadingScreenActive => _houseLoadingScreenActive;
    public bool IsCaptureUiHidden => _captureUiHidden;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject);
            return;
        }

        Instance = this;
        CloseAllWindows();
        _canvas = GetComponent<Canvas>();
        UISoundDefaults.MarkHierarchyAsUiSounds(gameObject);

        if(pointerON != null)
            _baseBigPointer = pointerON.sprite;

        CacheGrabLayout();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        UISoundDefaults.MarkHierarchyAsUiSounds(gameObject);
    }
#endif
    
    protected override void Start()
    {
        base.Start();
        
        Ghost currentGhost = House.Instance?.currentGhost;
        if (currentGhost != null)
        {
            currentGhost.onGhostStartToHunt.AddListener(CloseAllWindows);
        }
        
        this.Invoke(0.1f, () =>
        {
            EnableBigPointer(false);
            EnableGrabText(false);
        });

        _player = Player.Instance;
        _gameManager = GameManager.Instance;
        _gameManager.onMoneyUpdated.AddListener(UpdateGold);
        ApplyTutorialUIVisibility();
        RefreshMoneyVisibility();
        ApplyMoneySafeArea();
        UpdateGold();
        ApplyGrabTextLayout();

        if (settingsMenu == null)
            settingsMenu = UISettingsMenu.EnsureExists(this);
        UISoundDefaults.MarkHierarchyAsUiSounds(gameObject);
        
        EnableOverlay(true, 0);

        if (_gameManager != null &&
            _gameManager.TryConsumePendingHouseLoadingScreen(SceneManager.GetActiveScene().name, out float loadingDuration))
        {
            PlayHouseLoadingScreen(loadingDuration);
        }
        else
        {
            EnableOverlay(false, openSceneFadeDuration);
        }
        
        this.Invoke(0.1f, () =>
        {
            InitControlTexts(_player.inputManager);
        });
    }

    private void OnEnable()
    {
        if(_gameManager != null)
        {
            ApplyTutorialUIVisibility();
            RefreshMoneyVisibility();
            ApplyMoneySafeArea();
            UpdateGold();
        }

        ApplyGrabTextLayout();
    }

    public void UpdateGold()
    {
        if (tGold == null)
            return;
        tGold.text = "$" + _gameManager.GetInt(SaveKeys.GOLD);
    }

    private GameObject GetMoneyRoot()
    {
        if (uiMoney != null)
            return uiMoney;

        if (tGold != null && tGold.transform.parent != null)
            return tGold.transform.parent.gameObject;

        return tGold != null ? tGold.gameObject : null;
    }

    private bool ShouldShowMoney()
    {
        if (_captureUiHidden)
            return false;

        if (TutorialManager.Instance != null &&
            (TutorialManager.Instance.IsControlsTutorial || TutorialManager.Instance.IsTraining))
            return false;

        bool isWorld = _gameManager != null && _gameManager.IsWorld();
        if (isWorld)
            return true;

        return false;
    }

    private void RefreshMoneyVisibility()
    {
        GameObject moneyRoot = GetMoneyRoot();
        if (moneyRoot == null)
            return;

        bool shouldShow = ShouldShowMoney();
        if (_moneyVisibilityCached && _lastMoneyVisibility == shouldShow && moneyRoot.activeSelf == shouldShow)
            return;

        moneyRoot.SetActive(shouldShow);

        if (tGold != null && tGold.gameObject != moneyRoot)
            tGold.gameObject.SetActive(shouldShow);

        _lastMoneyVisibility = shouldShow;
        _moneyVisibilityCached = true;
    }

    private void CacheMoneyRect()
    {
        if (_moneyBaseCached || tGold == null)
            return;

        _moneyRect = tGold.transform.parent as RectTransform;
        if (_moneyRect == null)
            return;

        _moneyBasePos = _moneyRect.anchoredPosition;
        _moneyBaseCached = true;
    }

    private void ApplyMoneySafeArea()
    {
        if (!MobileInput.Enabled)
            return;

        CacheMoneyRect();
        if (_moneyRect == null)
            return;

        if (Screen.width == 0 || Screen.height == 0)
            return;

        Rect safe = Screen.safeArea;
        if (_lastMoneySafeArea == safe && _lastMoneyScreenSize.x == Screen.width && _lastMoneyScreenSize.y == Screen.height)
            return;

        float rightInset = Screen.width - (safe.x + safe.width);
        float topInset = Screen.height - (safe.y + safe.height);
        float scale = _canvas != null ? _canvas.scaleFactor : 1f;

        _moneyRect.anchoredPosition = new Vector2(
            _moneyBasePos.x - (rightInset / scale),
            _moneyBasePos.y - (topInset / scale)
        );

        _lastMoneySafeArea = safe;
        _lastMoneyScreenSize = new Vector2Int(Screen.width, Screen.height);
    }

    private void Update()
    {
        RefreshMoneyVisibility();
        ApplyMoneySafeArea();
        HandleUI();
    }
    
    private void HandleUI()
    {
        bool endGameOpen = tablet != null && tablet.endGame != null && tablet.endGame.gameObject.activeSelf;

        bool journalDown = (!MobileInput.Enabled && _player.inputManager.GetOpenJournalDown()) || MobileInput.OpenJournalDown;
        bool allowJournal = TutorialInputGate.IsAllowed(TutorialInputGate.AllowJournal);
        if (!endGameOpen && allowJournal && journalDown && _player.IsDead() == false)
        {
            if (IsJournalOpen())
                tablet.TurnOffTablet();
            else
                OpenJournal();
        }
        
        bool allowTeamMenu = TutorialInputGate.IsAllowed(TutorialInputGate.AllowTeamMenu);
        if (!endGameOpen && allowTeamMenu && ((!MobileInput.Enabled && _player.inputManager.GetOpenTeamMenuDown()) || MobileInput.OpenTeamMenuDown) && _player.IsDead() == false)
        {
            OpenTeamPanel();
        }

        if ((!MobileInput.Enabled && _player.inputManager.GetExitMenusDown()) || MobileInput.ExitMenusDown)
        {
            bool tabletActive = tablet != null && tablet.gameObject.activeSelf;
            if (tabletActive)
            {
                ExitLastMenu();
                return;
            }

            if (settingsMenu != null && settingsMenu.IsCapturingKey)
                return;

            if (settingsMenu != null)
            {
                if (MobileInput.Enabled)
                    GameManager.Instance?.RegisterDebugMoneySettingsPress();
                settingsMenu.Toggle();
                return;
            }

            ExitLastMenu();
        }
    }

    public void InitControlTexts(InputManager controller)
    {
        tGrab.text = LocalizationManager.Format("ui.grab_item", controller.GetGrabDisplay());
        ApplyGrabTextLayout();
    }

    public void EnablePointer(bool enable)
    {
        if (Player.Instance != null && Player.Instance.IsDead()) enable = false;
        if (_captureUiHidden)
            enable = false;
        
        pointerBase.SetActive(enable);
    }

    public void EnableBigPointer(bool enable)
    {
        if (pointerON == null) return;
        
        pointerON.gameObject.SetActive(enable);
    }

    public void SetBigPointerSprite(Sprite sprite, float size = 1)
    {
        if(sprite == null) sprite = _baseBigPointer;
        
        pointerON.sprite = sprite;
        pointerON.transform.localScale = Vector3.one * size;
    }

    public void EnableGrabText(bool enable)
    {
        ApplyGrabTextLayout();
        if (_captureUiHidden)
            enable = false;
        tGrab.gameObject.SetActive(enable);
    }

    private void OpenTeamPanel()
    {
        if (tablet == null)
            return;

        bool isTeamOpen = tablet.gameObject.activeSelf &&
                          tablet.tabWindows != null &&
                          tablet.tabWindows.Length > 0 &&
                          tablet.tabWindows[0].activeSelf;

        if (isTeamOpen)
        {
            tablet.TurnOffTablet();
            return;
        }

        tablet.gameObject.SetActive(true);
        tablet.OpenTab(0, true);
    }
    
    private void OpenJournal()
    {
        if (tablet == null)
            return;

        if (settingsMenu != null && settingsMenu.IsOpen)
            settingsMenu.SetVisible(false);

        tablet.gameObject.SetActive(true);
        tablet.OpenTab(1, true);
        UIJournal journal = tablet.GetComponentInChildren<UIJournal>(true);
        if (journal != null)
            journal.RefreshModeVisibility();
        onJournalOpened?.Invoke();
    }

    private void ApplyTutorialUIVisibility()
    {
        bool hideMoney = TutorialManager.Instance != null &&
                         (TutorialManager.Instance.IsControlsTutorial || TutorialManager.Instance.IsTraining);

        if (tGold != null)
        {
            _moneyVisibilityCached = false;
        }

        if (tablet != null && tablet.tabsObject != null)
        {
            bool hideTabs = TutorialManager.Instance != null && TutorialManager.Instance.IsControlsTutorial;
            tablet.tabsObject.SetActive(!hideTabs);
        }
    }

    private bool IsJournalOpen()
    {
        if (tablet == null || !tablet.gameObject.activeSelf)
            return false;

        if (tablet.CurrentTabIndex == 1)
            return true;

        if (tablet.tabWindows == null || tablet.tabWindows.Length <= 1)
            return false;

        return tablet.tabWindows[1] != null && tablet.tabWindows[1].activeSelf;
    }

    public void OpenPrivateTabletWindow(int windowID)
    {
        tablet.gameObject.SetActive(true);
        tablet.OpenPrivateWindow(windowID);
    }

    private void CloseTablet()
    {
        tablet.gameObject.SetActive(false);
    }

    public override void ExitLastMenu()
    {
        base.ExitLastMenu();
        tablet.TurnOffTablet();
        if (settingsMenu != null && settingsMenu.IsOpen)
            settingsMenu.SetVisible(false);
    }

    public override void CloseAllWindows()
    {
        base.CloseAllWindows();
        tablet.TurnOffTablet();
        if (settingsMenu != null && settingsMenu.IsOpen)
            settingsMenu.SetVisible(false);
    }

    public void SetCaptureUiHidden(bool hidden)
    {
        if (_captureUiHidden == hidden)
            return;

        _captureUiHidden = hidden;

        if (hidden)
        {
            CloseAllWindows();
            EnablePointer(false);
            EnableBigPointer(false);
            EnableGrabText(false);
            if (uiDialogue != null)
                uiDialogue.gameObject.SetActive(false);
        }

        RefreshMoneyVisibility();
    }

    public void OpenEndGame(UIEndGame.EndTypes endType, House house)
    {
        SetCaptureUiHidden(false);
        EnableOverlay(false, 0.5f);
        tablet.gameObject.SetActive(true);
        tablet.OpenEndGame(endType, house);
        OnEndGameOpened?.Invoke(endType);
    }

    private void PlayHouseLoadingScreen(float duration)
    {
        _houseLoadingScreenActive = true;
        _player?.LockControls(true);
        ForceHouseLoadingOverlayFront();
        EnableOverlay(true, 0f);

        Image loadingIcon = EnsureHouseLoadingIcon();
        if (loadingIcon != null)
        {
            loadingIcon.gameObject.SetActive(true);
            Color color = loadingIcon.color;
            color.a = 0.2f;
            loadingIcon.color = color;

            _houseLoadingBlinkTween?.Kill();
            _houseLoadingBlinkTween = loadingIcon
                .DOFade(1f, Mathf.Max(0.1f, houseLoadingBlinkDuration))
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetUpdate(true);
        }

        this.Invoke(nameof(FinishHouseLoadingScreen), Mathf.Max(0f, duration), FinishHouseLoadingScreen);
    }

    private void FinishHouseLoadingScreen()
    {
        _houseLoadingBlinkTween?.Kill();
        _houseLoadingBlinkTween = null;

        if (_houseLoadingIcon != null)
            _houseLoadingIcon.gameObject.SetActive(false);

        EnableOverlay(false, houseLoadingFadeOutDuration);
        _player?.LockControls(false);
        _houseLoadingScreenActive = false;
    }

    private Image EnsureHouseLoadingIcon()
    {
        if (_houseLoadingIcon != null)
            return _houseLoadingIcon;

        Sprite loadingSprite = FindISpmzSprite();
        if (loadingSprite == null || overlay == null)
            return null;

        GameObject iconObject = new GameObject("HouseLoading_iSpmz", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.SetParent(overlay.transform, false);
        iconRect.anchorMin = new Vector2(1f, 0f);
        iconRect.anchorMax = new Vector2(1f, 0f);
        iconRect.pivot = new Vector2(1f, 0f);
        iconRect.anchoredPosition = new Vector2(-houseLoadingIconMargin.x, houseLoadingIconMargin.y);
        iconRect.sizeDelta = houseLoadingIconSize;

        _houseLoadingIcon = iconObject.GetComponent<Image>();
        _houseLoadingIcon.sprite = loadingSprite;
        _houseLoadingIcon.preserveAspect = true;
        _houseLoadingIcon.raycastTarget = false;
        _houseLoadingIcon.gameObject.SetActive(false);
        _houseLoadingIcon.transform.SetAsLastSibling();
        return _houseLoadingIcon;
    }

    private void CacheGrabLayout()
    {
        if (_grabLayoutCached || tGrab == null)
            return;

        _grabRect = tGrab.rectTransform;
        if (_grabRect != null)
            _grabBaseSize = _grabRect.sizeDelta;

        _grabBaseFontSize = tGrab.fontSize;
        _grabLayoutCached = true;
    }

    private void ApplyGrabTextLayout()
    {
        CacheGrabLayout();
        if (!_grabLayoutCached || tGrab == null)
            return;

        float scale = MobileInput.Enabled ? Mathf.Max(1f, mobileGrabTextScale) : 1f;

        if (_grabRect != null)
            _grabRect.sizeDelta = _grabBaseSize * scale;

        tGrab.fontSize = _grabBaseFontSize * scale;
    }

    private void ForceHouseLoadingOverlayFront()
    {
        if (overlay == null)
            return;

        overlay.gameObject.SetActive(true);
        overlay.transform.SetAsLastSibling();
        Color color = overlay.color;
        color.a = 1f;
        overlay.color = color;

        if (_houseLoadingIcon != null)
            _houseLoadingIcon.transform.SetAsLastSibling();
    }

    private Sprite FindISpmzSprite()
    {
        Image[] images = GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];
            if (image == null || image.sprite == null)
                continue;

            if (string.Equals(image.sprite.name, "iSpmz", StringComparison.Ordinal))
                return image.sprite;

            if (string.Equals(image.gameObject.name, "iSpmz", StringComparison.Ordinal))
                return image.sprite;
        }

        return null;
    }
}
