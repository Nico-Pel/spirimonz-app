using UnityEngine;
using TMPro;

public class UIGame : MonoBehaviour
{
    public GameObject cursorON;
    public TextMeshProUGUI tGrab;
    public UIJournal Journal;
    public static UIGame Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void InitControlTexts(FPSControllerNoPhysics controller)
    {
        tGrab.text = "Grab Item [" + controller.grabObject + "]";
    }

    public void EnableCursor(bool enable)
    {
        cursorON.SetActive(enable);
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
}