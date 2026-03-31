using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIQuest : GameBehaviour
{
    public TextMeshProUGUI tTitle;
    public TextMeshProUGUI tProgression;
    public TextMeshProUGUI tDescription;

    public Image iBackground;
    public GameObject validationMarker;

    public Color textColorBase;
    public Color textColorValidate;

    public Color backgroundBaseColor;
    private GameManager _gameManager;

    private void Start()
    {
        _gameManager = GameManager.Instance;
    }

    public void SetQuest(Quest quest, HouseMap map)
    {
        if(_gameManager == null)
            _gameManager = GameManager.Instance;

        int questProgress = _gameManager.GetQuestProgress(quest, map.houseID);
        bool questComplete = false;
        
        if (questProgress >= quest.goal)
        {
            questProgress = quest.goal;
            questComplete = true;
        }
        
        tTitle.text = quest.GetLocalizedName();
        tTitle.color = questComplete ? textColorValidate : textColorBase;
        
        tProgression.text = questProgress + "/" + quest.goal;
        tProgression.color = questComplete ? textColorValidate : textColorBase;
        
        tDescription.text = quest.GetLocalizedDescription();

        Color bc = backgroundBaseColor;
        iBackground.color = questComplete ? new Color(bc.r, bc.g, bc.b, bc.a / 2) : backgroundBaseColor;
        
        validationMarker.gameObject.SetActive(questComplete);
    }
}
