using System;
using UnityEngine;
using TMPro;
using UnityEngine.Serialization;
using UnityEngine.UI;
using DG.Tweening;

public class UIGame : UIManager
{
    public GameObject pointerBase;
    public Image pointerON;
    public TextMeshProUGUI tGrab;
    public UITablet tablet;
    public static UIGame Instance { get; private set; }

    private Sprite _baseBigPointer;
    private Player _player;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        CloseAllWindows();

        _baseBigPointer = pointerON.sprite;
    }
    
    private void Start()
    {
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
        InitControlTexts(_player.inputManager);
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
        tablet.gameObject.SetActive(true);
        tablet.OpenTab(0, true);
    }
    
    private void OpenJournal()
    {
        tablet.gameObject.SetActive(true);
        tablet.OpenTab(1, true);
    }

    private void CloseTablet()
    {
        tablet.gameObject.SetActive(false);
    }

    public override void ExitLastMenu()
    {
        base.ExitLastMenu();
        tablet.gameObject.SetActive(false);
    }

    public override void CloseAllWindows()
    {
        base.CloseAllWindows();
        tablet.CloseAllTabs();
    }
}