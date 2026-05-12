using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class UIEntryPanel : GameBehaviour
{
    [Header("Components")] 
    public UIQuest[] quests;
    
    [Space]
    public TextMeshProUGUI tTitleMap;
    public Image mapImage;
    public TextMeshProUGUI tRoomsNb;
    public TextMeshProUGUI tPrice;

    public Button bGo;
    public Button bGoTuto;
    public Button bGoTraining;
    public Button bClose;
    public Color goColorBase;
    public Color goColorQuestsCompleted;

    [Header("Sounds")]
    public SoundParameters goSound;
    public SoundParameters goPaidSound;
    public SoundParameters goTutoSound;
    public SoundParameters goTrainingSound;
    public SoundParameters closeSound;

    [Header("Panels")]
    public GameObject normalPanel;
    public GameObject tutoPanel;

    [Header("Tips")]
    public GameObject iTips;
    public Image tipsBackground;
    public Color tipsBackgroundDefault = new Color(1f, 1f, 1f, 0.15f);
    public Color tipsBackgroundSecretWorld = new Color(1f, 0.85f, 0.3f, 0.2f);
    public TextMeshProUGUI tTips;
    [TextArea] public string tipsHasQuestsEnglish = "Complete all quests to gain free access to this location.";
    [TextArea] public string tipsHasQuestsFrench = "Complete toutes les quêtes pour accéder gratuitement à cet endroit.";
    [TextArea] public string tipsAllCompletedEnglish = "All quests completed, access is now free.";
    [TextArea] public string tipsAllCompletedFrench = "Toutes les quêtes sont complétées, l'accès est maintenant gratuit.";
    [TextArea] public string tipsSecretWorldEnglish = "The entry cost for this house increases with the number of runs in this world until your next visit.";
    [TextArea] public string tipsSecretWorldFrench = "Le coût d'entrée augmente avec le nombre de runs dans ce monde jusqu'à ta prochaine visite.";
    [TextArea] public string enterTextEnglish = "Enter";
    [TextArea] public string enterTextFrench = "Entrer";
    [TextArea] public string freeTextEnglish = "Free";
    [TextArea] public string freeTextFrench = "Gratuit";

    private const string TipsHasQuestsKey = "ui.entry.tips.has_quests";
    private const string TipsAllCompletedKey = "ui.entry.tips.all_completed";
    private const string TipsSecretWorldKey = "ui.entry.tips.secret_world";
    private const string EnterKey = "ui.entry.enter";
    private const string FreeKey = "ui.common.free";

    private GameManager _gameManager;
    private HouseEntry _entry;

    private HouseEntry _currentEntry;
    private bool _allQuestCompleted;

    public void OpenPanel(HouseEntry entry)
    {
        _currentEntry = entry;

        if (_gameManager == null)
        {
            _gameManager = GameManager.Instance;
        }

        bool allQuestCompleted = true;
        int questCount = entry != null && entry.map != null && entry.map.quests != null ? entry.map.quests.Length : 0;
        for (int i = 0; i < quests.Length; i++)
        {
            bool isActive = i < questCount;
            quests[i].gameObject.SetActive(isActive);
            if (isActive)
            {
                quests[i].SetQuest(entry.map.quests[i], entry.map);
                if (entry.map.quests[i].IsCompleted(entry.map.houseID) == false)
                {
                    allQuestCompleted = false;
                }
            }
        }

        bool freeByPrice = entry != null && entry.map != null && entry.map.entryPrince <= 0;
        bool freeAccess = freeByPrice || (allQuestCompleted && questCount > 0);
        _allQuestCompleted = freeAccess;

        bool isSecretWorldHouse = entry != null && entry.map != null && entry.map.linkedSecretWorld != null;
        bool isInSecretWorld = _gameManager != null &&
                               _gameManager.IsTemporaryWorldScene(SceneManager.GetActiveScene().name);
        bool useSecretWorldPricing = isSecretWorldHouse && isInSecretWorld;
        if (useSecretWorldPricing && _gameManager != null)
        {
            _gameManager.SetSecretWorldPriceConfig(
                entry.map.linkedSecretWorld.houseEntryPriceIncrease,
                entry.map.linkedSecretWorld.maxHouseEntryPriceIncreases);
        }

        if (normalPanel != null)
            normalPanel.SetActive(entry == null || !entry.hasTutorialModes);
        if (tutoPanel != null)
            tutoPanel.SetActive(entry != null && entry.hasTutorialModes);
        
        gameObject.SetActive(true);
        tTitleMap.text = entry.map.GetLocalizedName();
        tRoomsNb.text = entry.map.roomsNumber.ToString();
        mapImage.sprite = entry.map.sprite;
        
        bGo.image.color = freeAccess && !freeByPrice ? goColorQuestsCompleted : goColorBase;

        int priceToUse = entry != null && entry.map != null ? entry.map.entryPrince : 0;
        if (useSecretWorldPricing)
            priceToUse = _gameManager != null ? _gameManager.GetSecretWorldHouseEntryPrice(entry.map.linkedSecretWorld) : 0;
        else if (freeAccess)
            priceToUse = 0;

        if (tPrice != null)
        {
            if (priceToUse <= 0)
                tPrice.text = LocalizationManager.Get(FreeKey, LocalizeFallback(freeTextEnglish, freeTextFrench));
            else if (!useSecretWorldPricing && freeAccess && !freeByPrice)
                tPrice.text = LocalizationManager.Get(EnterKey, LocalizeFallback(enterTextEnglish, enterTextFrench));
            else
                tPrice.text = priceToUse + "#";
        }

        _entry = entry;
        
        bGo.onClick.RemoveAllListeners();
        bGo.onClick.AddListener(GoNormal);

        if (bClose != null)
        {
            bClose.onClick.RemoveAllListeners();
            bClose.onClick.AddListener(ClosePanel);
        }

        if (bGoTuto != null)
        {
            bGoTuto.onClick.RemoveAllListeners();
            bGoTuto.onClick.AddListener(GoTutorial);
        }

        if (bGoTraining != null)
        {
            bGoTraining.onClick.RemoveAllListeners();
            bGoTraining.onClick.AddListener(GoTraining);
        }

        int price = priceToUse;
        bool enoughMoney = price <= 0 || _gameManager.CanBuy(price);
        bGo.interactable = enoughMoney;
        tPrice.color = enoughMoney ? Color.white : Color.red;
        if (bGoTuto != null)
            bGoTuto.interactable = enoughMoney;
        if (bGoTraining != null)
            bGoTraining.interactable = enoughMoney;

        if (iTips != null)
        {
            bool showTips = useSecretWorldPricing || (questCount > 0 && !freeByPrice);
            iTips.SetActive(showTips);
            if (tipsBackground != null)
                tipsBackground.color = useSecretWorldPricing ? tipsBackgroundSecretWorld : tipsBackgroundDefault;

            if (showTips && tTips != null)
            {
                if (useSecretWorldPricing)
                {
                    tTips.text = LocalizationManager.Get(TipsSecretWorldKey, LocalizeFallback(tipsSecretWorldEnglish, tipsSecretWorldFrench));
                }
                else
                {
                    string tips = freeAccess
                        ? LocalizationManager.Get(TipsAllCompletedKey, LocalizeFallback(tipsAllCompletedEnglish, tipsAllCompletedFrench))
                        : LocalizationManager.Get(TipsHasQuestsKey, LocalizeFallback(tipsHasQuestsEnglish, tipsHasQuestsFrench));
                    tTips.text = tips;
                }
            }
        }
    }

    private void GoNormal()
    {
        TryEnterWithMode(GameManager.HouseSceneMode.NormalMap, goSound, goPaidSound);
    }

    private void GoTutorial()
    {
        TryEnterWithMode(GameManager.HouseSceneMode.Tutorial, goTutoSound, goPaidSound);
    }

    private void GoTraining()
    {
        TryEnterWithMode(GameManager.HouseSceneMode.Training, goTrainingSound, goPaidSound);
    }

    private void TryEnterWithMode(GameManager.HouseSceneMode mode, SoundParameters freeSound, SoundParameters paidSound)
    {
        if (_gameManager == null || _currentEntry == null)
            return;

        bool isSecretWorldHouse = _currentEntry.map != null && _currentEntry.map.linkedSecretWorld != null;
        bool isInSecretWorld = _gameManager.IsTemporaryWorldScene(SceneManager.GetActiveScene().name);
        bool useSecretWorldPricing = isSecretWorldHouse && isInSecretWorld;

        int price = _allQuestCompleted ? 0 : _currentEntry.map.entryPrince;
        if (useSecretWorldPricing)
            price = _gameManager.GetSecretWorldHouseEntryPrice(_currentEntry.map.linkedSecretWorld);

        if (price > 0 && !_gameManager.Buy(price))
            return;

        if (price > 0)
        {
            if (paidSound != null)
                paidSound.PlaySound();
        }
        else if (freeSound != null)
        {
            freeSound.PlaySound();
        }

        _gameManager.SetNextHouseSceneMode(mode);
        {
            UIGame.Instance.CloseAllWindows();
            _entry.Entry(Player.Instance);
        }
    }

    private void ClosePanel()
    {
        if (closeSound != null)
        {
            UITablet tablet = UIGame.Instance != null ? UIGame.Instance.tablet : null;
            if (tablet == null || tablet.closeTabletSound == null)
                closeSound.PlaySound();
        }

        UIGame.Instance.CloseAllWindows();
    }

    private string LocalizeFallback(string english, string french)
    {
        if (LanguageManager.CurrentLanguage == Language.French && !string.IsNullOrWhiteSpace(french))
            return french;

        return english;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        UISoundDefaults.AssignIfNull(ref goSound);
        UISoundDefaults.AssignIfNull(ref goPaidSound);
        UISoundDefaults.AssignIfNull(ref goTutoSound);
        UISoundDefaults.AssignIfNull(ref goTrainingSound);
        UISoundDefaults.AssignIfNull(ref closeSound);
    }
#endif
}
