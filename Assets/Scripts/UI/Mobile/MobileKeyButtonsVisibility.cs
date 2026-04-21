using UnityEngine;

public class MobileKeyButtonsVisibility : MonoBehaviour
{
    public GameObject settingsButton;
    public GameObject journalButton;
    public GameObject prevButton;
    public GameObject nextButton;
    public GameObject yButton;

    private void Awake()
    {
        EnsureReferences();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureReferences();
    }
#endif

    private void Update()
    {
        bool canOpenJournal = TutorialInputGate.IsAllowed(TutorialInputGate.AllowJournal);

        SetActive(settingsButton, true);
        SetActive(journalButton, canOpenJournal);
        SetActive(yButton, true);
    }

    private void EnsureReferences()
    {
        if (settingsButton == null)
            settingsButton = FindOptional("Key_ESC");

        if (journalButton == null)
            journalButton = FindOptional("Key_J");
    }

    private GameObject FindOptional(string childName)
    {
        Transform child = transform.Find(childName);
        return child != null ? child.gameObject : null;
    }

    private void SetActive(GameObject go, bool active)
    {
        if (go != null && go.activeSelf != active)
            go.SetActive(active);
    }
}
