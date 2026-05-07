using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public class MobileInventoryFooter : MonoBehaviour
{
    [Header("Navigation")]
    public GameObject prevButton;
    public GameObject nextButton;

    [Header("Inventory Buttons")]
    public GameObject lampButton;
    public GameObject[] teamButtons = new GameObject[5];

    [Header("Visual References")]
    public Image lampButtonImage;
    public GameObject lampSelector;
    public Image[] teamButtonImages = new Image[5];
    public GameObject[] teamSelectors = new GameObject[5];

    [Header("Visual State")]
    public Color emptySlotColor = new Color(1f, 1f, 1f, 0.15f);
    public Color filledSlotColor = Color.white;

    private readonly Sprite[] _teamEmptySprites = new Sprite[5];
    private readonly Color[] _teamBaseColors = new Color[5];

    private InventoryManager _inventoryManager;
    private bool _cachedTeamVisuals;
    private CanvasGroup _group;

    private void Awake()
    {
        _group = GetComponent<CanvasGroup>();
        EnsureReferences();
        EnsureRuntimeButtons();
        CacheVisualDefaults();
        RefreshVisuals();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureReferences();
    }
#endif

    private void Update()
    {
        RefreshVisuals();
    }

    public void EnsureReferences()
    {
        if (prevButton == null)
            prevButton = FindOptional("Key_Prev");

        if (nextButton == null)
            nextButton = FindOptional("Key_Nxt");

        if (lampButton == null)
            lampButton = FindOptional("TeamButton-Lamp-NightVision");

        for (int i = 0; i < teamButtons.Length; i++)
        {
            if (teamButtons[i] == null)
                teamButtons[i] = FindOptional($"TeamButton{i + 1:00}");
        }

        if (lampButtonImage == null && lampButton != null)
            lampButtonImage = lampButton.GetComponent<Image>();

        if (lampSelector == null && lampButton != null)
            lampSelector = FindSelector(lampButton);

        for (int i = 0; i < teamButtons.Length; i++)
        {
            if (teamButtonImages[i] == null && teamButtons[i] != null)
                teamButtonImages[i] = teamButtons[i].GetComponent<Image>();

            if (teamSelectors[i] == null && teamButtons[i] != null)
                teamSelectors[i] = FindSelector(teamButtons[i]);
        }
    }

    private void EnsureRuntimeButtons()
    {
        ConfigureInventoryButton(lampButton, 0);

        for (int i = 0; i < teamButtons.Length; i++)
            ConfigureInventoryButton(teamButtons[i], i + 1);
    }

    private void ConfigureInventoryButton(GameObject buttonObject, int inventoryIndex)
    {
        if (buttonObject == null)
            return;

        Button button = buttonObject.GetComponent<Button>();
        if (button == null)
            button = buttonObject.AddComponent<Button>();

        Image image = buttonObject.GetComponent<Image>();
        if (button.targetGraphic == null && image != null)
            button.targetGraphic = image;

        MobileButton mobileButton = buttonObject.GetComponent<MobileButton>();
        if (mobileButton == null)
            mobileButton = buttonObject.AddComponent<MobileButton>();

        mobileButton.action = GetInventoryAction(inventoryIndex);
    }

    private void CacheVisualDefaults()
    {
        if (_cachedTeamVisuals)
            return;

        for (int i = 0; i < teamButtonImages.Length; i++)
        {
            Image image = teamButtonImages[i];
            if (image == null)
                continue;

            _teamEmptySprites[i] = image.sprite;
            _teamBaseColors[i] = image.color;
        }

        _cachedTeamVisuals = true;
    }

    private void RefreshVisuals()
    {
        if (_inventoryManager == null)
            _inventoryManager = InventoryManager.Instance;

        bool isWorld = GameManager.Instance != null && GameManager.Instance.IsWorld();
        bool isTitleScreen = GameManager.Instance != null && GameManager.Instance.IsTitleScreenActive();
        bool settingsOpen = UIGame.Instance != null &&
                            UIGame.Instance.settingsMenu != null &&
                            UIGame.Instance.settingsMenu.IsOpen;
        bool tabletOpen = UIGame.Instance != null &&
                          UIGame.Instance.tablet != null &&
                          UIGame.Instance.tablet.gameObject.activeSelf;
        bool houseLoadingActive = UIGame.Instance != null && UIGame.Instance.IsBlockingHouseLoadingScreenActive;
        bool captureUiHidden = UIGame.Instance != null && UIGame.Instance.IsCaptureUiHidden;
        bool dialogueActive = UIGame.Instance != null &&
                              UIGame.Instance.uiDialogue != null &&
                              UIGame.Instance.uiDialogue.IsDialogueActive;
        bool[] slotAllowed = new bool[6];
        int unlockedSlots = 0;

        for (int i = 0; i < slotAllowed.Length; i++)
        {
            slotAllowed[i] = TutorialInputGate.IsInventorySlotAllowed(i);
            if (slotAllowed[i])
                unlockedSlots++;
        }

        bool showFooter = MobileInput.Enabled &&
                          !isWorld &&
                          !isTitleScreen &&
                          !settingsOpen &&
                          !tabletOpen &&
                          !dialogueActive &&
                          !captureUiHidden &&
                          !houseLoadingActive &&
                          unlockedSlots > 0;
        ApplyVisibility(showFooter);

        SetActive(lampButton, showFooter && slotAllowed[0]);
        SetActive(prevButton, showFooter && unlockedSlots > 1);
        SetActive(nextButton, showFooter && unlockedSlots > 1);

        int selectedIndex = _inventoryManager != null ? _inventoryManager.currentSelectedIndex : -1;
        SetActive(lampSelector, showFooter && slotAllowed[0] && selectedIndex == 0);

        for (int i = 0; i < teamButtons.Length; i++)
        {
            bool isUnlocked = slotAllowed[i + 1];
            SetActive(teamButtons[i], showFooter && isUnlocked);

            SpirimonzSettings settings = GetTeamSettings(i);
            bool hasSpirimonz = settings != null;

            Image image = teamButtonImages[i];
            if (image != null)
            {
                image.sprite = hasSpirimonz && settings.img != null ? settings.img : _teamEmptySprites[i];
                image.color = hasSpirimonz ? filledSlotColor : GetEmptyColor(i);
                image.preserveAspect = true;
            }

            SetActive(teamSelectors[i], showFooter && isUnlocked && selectedIndex == i + 1);
        }
    }

    private SpirimonzSettings GetTeamSettings(int teamIndex)
    {
        if (_inventoryManager == null || _inventoryManager.spirimonzTeamSettings == null)
            return null;

        if (teamIndex < 0 || teamIndex >= _inventoryManager.spirimonzTeamSettings.Count)
            return null;

        return _inventoryManager.spirimonzTeamSettings[teamIndex];
    }

    private Color GetEmptyColor(int index)
    {
        if (index < 0 || index >= _teamBaseColors.Length)
            return emptySlotColor;

        Color cached = _teamBaseColors[index];
        return cached.a > 0f ? cached : emptySlotColor;
    }

    private GameObject FindOptional(string childName)
    {
        Transform child = FindDeepChild(transform, childName);
        return child != null ? child.gameObject : null;
    }

    private static Transform FindDeepChild(Transform parent, string childName)
    {
        if (parent == null)
            return null;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName)
                return child;

            Transform nested = FindDeepChild(child, childName);
            if (nested != null)
                return nested;
        }

        return null;
    }

    private static GameObject FindSelector(GameObject buttonObject)
    {
        if (buttonObject == null)
            return null;

        Transform selector = buttonObject.transform.Find("selector");
        return selector != null ? selector.gameObject : null;
    }

    private static void SetActive(GameObject go, bool active)
    {
        if (go != null && go.activeSelf != active)
            go.SetActive(active);
    }

    private void ApplyVisibility(bool visible)
    {
        if (_group == null)
            _group = GetComponent<CanvasGroup>();

        if (_group == null)
            return;

        _group.alpha = visible ? 1f : 0f;
        _group.interactable = visible;
        _group.blocksRaycasts = visible;
    }

    private static MobileButton.Action GetInventoryAction(int inventoryIndex)
    {
        int actionValue = (int)MobileButton.Action.Inventory1 + Mathf.Clamp(inventoryIndex, 0, 5);
        return (MobileButton.Action)actionValue;
    }
}
