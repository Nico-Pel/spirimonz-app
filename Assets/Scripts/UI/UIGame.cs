using System;
using UnityEngine;
using TMPro;
using UnityEngine.Serialization;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.Events;

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

        if (settingsMenu == null)
            settingsMenu = UISettingsMenu.EnsureExists(this);
        UISoundDefaults.MarkHierarchyAsUiSounds(gameObject);
        
        EnableOverlay(true, 0);
        EnableOverlay(false, openSceneFadeDuration);
        
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
        if (TutorialManager.Instance != null &&
            (TutorialManager.Instance.IsControlsTutorial || TutorialManager.Instance.IsTraining))
            return false;

        bool isWorld = _gameManager != null && _gameManager.IsWorld();
        if (isWorld)
            return true;

        bool tabletOpen = tablet != null && tablet.gameObject.activeSelf;
        if (tabletOpen)
            return true;

        return forceShowMoney && MobileInput.Enabled;
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
                settingsMenu.Toggle();
                return;
            }

            ExitLastMenu();
        }
    }

    public void InitControlTexts(InputManager controller)
    {
        tGrab.text = LocalizationManager.Format("ui.grab_item", controller.GetGrabDisplay());
    }

    public void EnablePointer(bool enable)
    {
        if (Player.Instance != null && Player.Instance.IsDead()) enable = false;
        
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

    public void OpenEndGame(UIEndGame.EndTypes endType, House house)
    {
        EnableOverlay(false, 0.5f);
        tablet.gameObject.SetActive(true);
        tablet.OpenEndGame(endType, house);
        OnEndGameOpened?.Invoke(endType);
    }
}
