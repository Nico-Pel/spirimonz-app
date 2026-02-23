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
    public Color goColorBase;
    public Color goColorQuestsCompleted;

    private GameManager _gameManager;
    private HouseEntry _entry;

    private HouseEntry _currentEntry;

    public void OpenPanel(HouseEntry entry)
    {
        _currentEntry = entry;

        bool allQuestCompleted = true;
        for (int i = 0; i < quests.Length; i++)
        {
            bool isActive = i < entry.map.quests.Length;
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
        
        gameObject.SetActive(true);
        tTitleMap.text = entry.map.houseName;
        tRoomsNb.text = entry.map.roomsNumber.ToString();
        mapImage.sprite = entry.map.sprite;
        
        bGo.image.color = allQuestCompleted ? goColorQuestsCompleted : goColorBase;
        tPrice.text = allQuestCompleted ? "Enter" : (entry.map.entryPrince + "$");

        _entry = entry;
        
        bGo.onClick.RemoveAllListeners();
        bGo.onClick.AddListener(Go);

        if (_gameManager == null)
        {
            _gameManager = GameManager.Instance;;
        }
        
        bool enoughMoney = _gameManager.CanBuy(entry.map.entryPrince);
        bGo.interactable = enoughMoney;
        tPrice.color = enoughMoney ? Color.white : Color.red;
    }

    private void Go()
    {
        if (_gameManager.Buy(_currentEntry.map.entryPrince))
        {
            UIGame.Instance.CloseAllWindows();
            _entry.Entry(Player.Instance);
        }
    }
}