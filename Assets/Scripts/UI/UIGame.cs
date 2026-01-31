using System;
using UnityEngine;
using TMPro;
using UnityEngine.Serialization;
using UnityEngine.UI;
using DG.Tweening;

public class UIGame : MonoBehaviour
{
    public Image overlay;

    public GameObject pointerBase;
    public GameObject pointerON;
    public TextMeshProUGUI tGrab;
    public UIJournal Journal;
    public static UIGame Instance { get; private set; }

    private bool _currentCursorState;
    private int _showCursorsActivatedCount;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        CloseAllWindows();
    }
    
    private void Start()
    {
        Ghost currentGhost = House.Instance?.currentGhost;
        if (currentGhost != null)
        {
            currentGhost.onGhostStartToHunt.AddListener(CloseAllWindows);
        }
    }

    public void InitControlTexts(FPSControllerNoPhysics controller)
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
        pointerON.SetActive(enable);
    }

    public void EnableGrabText(bool enable)
    {
        tGrab.gameObject.SetActive(enable);
    }

    public void SwitchJournalState()
    {
        EnableJournal(!GetJournalState());
    }

    public void EnableJournal(bool enable)
    {
        Journal.gameObject.SetActive(enable);
    }

    public bool GetJournalState()
    {
        return Journal.gameObject.activeSelf;
    }

    public void ExitLastMenu()
    {
        if (GetJournalState() == true)
        {
            EnableJournal(false);
        }
    }

    public void CloseAllWindows()
    {
        EnableJournal(false);
    }

    private void ShowCursor(bool enable)
    {
        _currentCursorState = enable;

        Cursor.visible = enable;
        Cursor.lockState = enable ? CursorLockMode.None : CursorLockMode.Locked;

        Player.Instance.LockControls(enable);
    }

    public void AddShowCursor()
    {
        _showCursorsActivatedCount++;
        
        if (_currentCursorState == false)
            ShowCursor(true);
    }

    public void RemoveShowCursor()
    {
        _showCursorsActivatedCount--;
        
        if(_showCursorsActivatedCount <= 0)
            ShowCursor(false);
    }
    
    public void EnableOverlay(bool enable, float fadeDuration)
    {
        Color colorToUse = enable ? Color.black : new Color(0, 0, 0, 0);
        if (fadeDuration <= 0)
        {
            overlay.color = colorToUse;
        }
        else
        {
            overlay.DOColor(colorToUse, fadeDuration);
        }
    }
}