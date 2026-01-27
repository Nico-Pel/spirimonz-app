using System;
using UnityEngine;
using TMPro;
using UnityEngine.Serialization;

public class UIGame : MonoBehaviour
{
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
        pointerON.SetActive(enable);
    }

    public void EnableGrabText(bool enable)
    {
        tGrab.gameObject.SetActive(enable);
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

    private void CloseAllWindows()
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
}