using UnityEngine;
using UnityEngine.UI;

public class MobileKeyButtonsVisibility : MonoBehaviour
{
    public GameObject settingsButton;
    public GameObject journalButton;
    public GameObject shopButton;
    public GameObject prevButton;
    public GameObject nextButton;
    public GameObject yButton;
    public int shopPrivateWindowId;

    private Button _shopButtonComponent;
    private bool _shopButtonBound;

    private void Awake()
    {
        EnsureReferences();
        EnsureShopBinding();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureReferences();
    }
#endif

    private void Update()
    {
        EnsureShopBinding();
        bool canOpenJournal = TutorialInputGate.IsAllowed(TutorialInputGate.AllowJournal);
        bool settingsOpen = UIGame.Instance != null &&
                            UIGame.Instance.settingsMenu != null &&
                            UIGame.Instance.settingsMenu.IsOpen;
        bool tabletOpen = UIGame.Instance != null &&
                          UIGame.Instance.tablet != null &&
                          UIGame.Instance.tablet.gameObject.activeSelf;
        bool captureUiHidden = UIGame.Instance != null && UIGame.Instance.IsCaptureUiHidden;
        bool showDebugMoney = GameManager.Instance != null && GameManager.Instance.IsDebugMoneyButtonVisibleOnMobile();
        bool showOtherTopButtons = !settingsOpen && !tabletOpen && !captureUiHidden;
        bool useMobileUi = MobileInput.Enabled ||
                           Application.isMobilePlatform ||
                           (GameManager.Instance != null && GameManager.Instance.mobileControlsEnabled);
        bool isTitleScreen = GameManager.Instance != null && GameManager.Instance.IsTitleScreenActive();
        bool isWorld = GameManager.Instance != null && GameManager.Instance.IsWorld();

        if (isTitleScreen)
        {
            SetActive(settingsButton, useMobileUi && !captureUiHidden);
            SetActive(journalButton, false);
            SetActive(shopButton, false);
            SetActive(prevButton, false);
            SetActive(nextButton, false);
            SetActive(yButton, false);
            return;
        }

        SetActive(settingsButton, true);
        SetActive(journalButton, canOpenJournal && showOtherTopButtons);
        SetActive(shopButton, useMobileUi && showOtherTopButtons && isWorld);
        SetActive(prevButton, showOtherTopButtons);
        SetActive(nextButton, showOtherTopButtons);
        SetActive(yButton, showDebugMoney && showOtherTopButtons);
    }

    private void EnsureReferences()
    {
        if (settingsButton == null)
            settingsButton = FindOptional("Key_ESC");

        if (journalButton == null)
            journalButton = FindOptional("Key_J");

        if (shopButton == null)
            shopButton = FindOptional("Key_Shop");
    }

    private GameObject FindOptional(string childName)
    {
        Transform child = transform.Find(childName);
        return child != null ? child.gameObject : null;
    }

    private void EnsureShopBinding()
    {
        if (_shopButtonBound && _shopButtonComponent != null)
            return;

        if (shopButton == null)
            return;

        _shopButtonComponent = shopButton.GetComponent<Button>();
        if (_shopButtonComponent == null)
            return;

        _shopButtonComponent.onClick.RemoveListener(OpenShopWindow);
        _shopButtonComponent.onClick.AddListener(OpenShopWindow);
        _shopButtonBound = true;
    }

    private void OpenShopWindow()
    {
        if (UIGame.Instance == null)
            return;

        UIGame.Instance.OpenPrivateTabletWindow(shopPrivateWindowId);
    }

    private void SetActive(GameObject go, bool active)
    {
        if (go != null && go.activeSelf != active)
            go.SetActive(active);
    }
}
