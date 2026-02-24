using System;
using UnityEngine;
using TMPro;
using UnityEngine.Serialization;
using UnityEngine.UI;
using DG.Tweening;

public class UIGame : UIManager
{
    public static UIGame Instance { get; private set; }

    public float openSceneFadeDuration = 3f;

    [Space] [Header("Window elements")]
    public TextMeshProUGUI tGold;
    
    [Space]
    
    [Header("Cursor elements")]
    public GameObject pointerBase;
    public Image pointerON;
    public TextMeshProUGUI tGrab;
    
    [Space]
    
    [Header("Window elements")]
    public UITablet tablet;

    private Sprite _baseBigPointer;
    private GameManager _gameManager;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject);
            return;
        }

        Instance = this;
        CloseAllWindows();

        if(pointerON != null)
            _baseBigPointer = pointerON.sprite;
    }
    
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
        UpdateGold();
        
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
            UpdateGold();
    }

    public void UpdateGold()
    {
        tGold.text = "$" + _gameManager.GetInt(SaveKeys.GOLD);
    }

    private void Update()
    {
        HandleUI();
    }
    
    private void HandleUI()
    {
        if (Input.GetKeyDown(_player.inputManager.openJournal) && _player.IsDead() == false)
        {
            OpenJournal();
        }
        
        if (Input.GetKeyDown(_player.inputManager.openTeamMenu) && _player.IsDead() == false)
        {
            OpenTeamPanel();
        }

        if (Input.GetKeyDown(_player.inputManager.exitMenus))
        {
            ExitLastMenu();
        }
    }

    public void InitControlTexts(InputManager controller)
    {
        tGrab.text = "Grab Item [" + controller.grabObject + "]";
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
        CloseTablet();
        tablet.OpenTab(0, true);
    }
    
    private void OpenJournal()
    {
        CloseTablet();
        tablet.OpenTab(1, true);
    }

    public void OpenPrivateTabletWindow(int windowID)
    {
        CloseTablet();
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
    }

    public override void CloseAllWindows()
    {
        base.CloseAllWindows();
        tablet.TurnOffTablet();
    }

    public void OpenEndGame(UIEndGame.EndTypes endType, House house)
    {
        tablet.gameObject.SetActive(true);
        tablet.OpenEndGame(endType, house);
    }
}