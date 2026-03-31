using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    public Color goColorBase;
    public Color goColorQuestsCompleted;

    [Header("Panels")]
    public GameObject normalPanel;
    public GameObject tutoPanel;

    [Header("Tips")]
    public GameObject iTips;
    public TextMeshProUGUI tTips;
    [TextArea] public string tipsHasQuestsEnglish = "Complete all quests to gain free access to this location.";
    [TextArea] public string tipsHasQuestsFrench = "Complete toutes les quêtes pour accéder gratuitement à cet endroit.";
    [TextArea] public string tipsAllCompletedEnglish = "All quests completed, access is now free.";
    [TextArea] public string tipsAllCompletedFrench = "Toutes les quêtes sont complétées, l'accès est maintenant gratuit.";
    [TextArea] public string enterTextEnglish = "Enter";
    [TextArea] public string enterTextFrench = "Entrer";

    private GameManager _gameManager;
    private HouseEntry _entry;

    private HouseEntry _currentEntry;
    private bool _allQuestCompleted;

    public void OpenPanel(HouseEntry entry)
    {
        _currentEntry = entry;

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

        bool freeAccess = allQuestCompleted && questCount > 0;
        _allQuestCompleted = freeAccess;

        if (normalPanel != null)
            normalPanel.SetActive(entry == null || !entry.hasTutorialModes);
        if (tutoPanel != null)
            tutoPanel.SetActive(entry != null && entry.hasTutorialModes);
        
        gameObject.SetActive(true);
        tTitleMap.text = entry.map.houseName;
        tRoomsNb.text = entry.map.roomsNumber.ToString();
        mapImage.sprite = entry.map.sprite;
        
        bGo.image.color = freeAccess ? goColorQuestsCompleted : goColorBase;
        tPrice.text = freeAccess ? Localize(enterTextEnglish, enterTextFrench) : (entry.map.entryPrince + "$");

        _entry = entry;
        
        bGo.onClick.RemoveAllListeners();
        bGo.onClick.AddListener(GoNormal);

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

        if (_gameManager == null)
        {
            _gameManager = GameManager.Instance;;
        }

        int price = freeAccess ? 0 : entry.map.entryPrince;
        bool enoughMoney = price <= 0 || _gameManager.CanBuy(price);
        bGo.interactable = enoughMoney;
        tPrice.color = enoughMoney ? Color.white : Color.red;
        if (bGoTuto != null)
            bGoTuto.interactable = enoughMoney;
        if (bGoTraining != null)
            bGoTraining.interactable = enoughMoney;

        if (iTips != null)
        {
            bool showTips = questCount > 0;
            iTips.SetActive(showTips);
            if (showTips && tTips != null)
            {
                string tips = freeAccess
                    ? Localize(tipsAllCompletedEnglish, tipsAllCompletedFrench)
                    : Localize(tipsHasQuestsEnglish, tipsHasQuestsFrench);
                tTips.text = tips;
            }
        }
    }

    private void GoNormal()
    {
        TryEnterWithMode(GameManager.HouseSceneMode.NormalMap);
    }

    private void GoTutorial()
    {
        TryEnterWithMode(GameManager.HouseSceneMode.Tutorial);
    }

    private void GoTraining()
    {
        TryEnterWithMode(GameManager.HouseSceneMode.Training);
    }

    private void TryEnterWithMode(GameManager.HouseSceneMode mode)
    {
        if (_gameManager == null || _currentEntry == null)
            return;

        int price = _allQuestCompleted ? 0 : _currentEntry.map.entryPrince;
        if (price > 0 && !_gameManager.Buy(price))
            return;

        _gameManager.SetNextHouseSceneMode(mode);
        {
            UIGame.Instance.CloseAllWindows();
            _entry.Entry(Player.Instance);
        }
    }

    private string Localize(string english, string french)
    {
        if (LanguageManager.CurrentLanguage == Language.French && !string.IsNullOrWhiteSpace(french))
            return french;

        return english;
    }
}
