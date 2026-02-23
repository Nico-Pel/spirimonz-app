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

    private HouseEntry _entry;

    public void OpenPanel(HouseEntry entry)
    {
        gameObject.SetActive(true);
        tTitleMap.text = entry.map.houseName;
        tRoomsNb.text = entry.map.roomsNumber.ToString();
        mapImage.sprite = entry.map.sprite;
        tPrice.text = entry.map.entryPrince + "$";

        for (int i = 0; i < quests.Length; i++)
        {
            bool isActive = i < entry.map.quests.Length;
            quests[i].gameObject.SetActive(isActive);
            if (isActive)
            {
                quests[i].SetQuest(entry.map.quests[i], entry.map);
            }
        }

        _entry = entry;
        
        bGo.onClick.RemoveAllListeners();
        bGo.onClick.AddListener(Go);
    }

    private void Go()
    {
        UIGame.Instance.CloseAllWindows();
        _entry.Entry(Player.Instance);
    }
}