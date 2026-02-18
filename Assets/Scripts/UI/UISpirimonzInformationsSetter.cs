using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UISpirimonzInformationsSetter : GameBehaviour
{
    [Header("3D Components")]
    public Transform spmzBodyPos;
    [ReadOnly] public GameObject currentSpirimonzBody;
    
    [Header("Texts")]
    public TextMeshProUGUI tSpirimonzName;
    public TextMeshProUGUI[] tSpirimonzAbilities;
    
    [Header("Images")] 
    public Image[] abilityPanels;
    public Image primaryType;
    public Image secondaryType;
    public Image[] booleanFeedbacks;

    private Color _abilityPanelBaseColor;
    private Color _abilityPanelOffColor;

    [Header("Sprites")] 
    public Sprite nopSprite;
    public Color nopColor;
    public Sprite yesSprite;
    public Color yesColor;
    
    private SpirimonzSettings _lastSpirimonzSettings;
    
    private void Awake()
    {
        _abilityPanelBaseColor = abilityPanels[0].color;
        _abilityPanelOffColor = new Color(_abilityPanelBaseColor.r, _abilityPanelBaseColor.g, _abilityPanelBaseColor.b, 0.2f);
    }
    
    public void SetSpirimonz(SpirimonzSettings spmz)
    {
        if(tSpirimonzName != null)
            tSpirimonzName.text = spmz.spirimonzName;

        if(primaryType != null)
            primaryType.sprite = spmz.PrimaryTypeSprite;
        
        if(secondaryType != null)
            secondaryType.sprite = spmz.SecondaryTypeSprite;

        if (abilityPanels.Length > 0)
        {
            SetSpirimonzAbilities(spmz);
        }

        if (spmzBodyPos != null)
        {
            SetSpirimonzBody(spmz);
        }

        if (booleanFeedbacks.Length > 0)
        {
            SetSpirimonzBooleanFeedbacks(spmz);
        }
        
        _lastSpirimonzSettings = spmz;
    }

    private void SetSpirimonzAbilities(SpirimonzSettings spmz)
    {
        int abilityCount = spmz.abilitiesDescriptions.Length;
        for (int i = 0; i < abilityPanels.Length; i++)
        {
            bool abilityExist = i < abilityCount;

            if (abilityPanels[i] != null)
                abilityPanels[i].color = abilityExist ? _abilityPanelBaseColor : _abilityPanelOffColor;
                
            if(tSpirimonzAbilities[i] != null)
                tSpirimonzAbilities[i].text = abilityExist ? spmz.abilitiesDescriptions[i] : "";
        }
    }

    private void SetSpirimonzBody(SpirimonzSettings spmz)
    {
        if (currentSpirimonzBody != null)
        {
            Destroy(currentSpirimonzBody);
        }
        
        currentSpirimonzBody = Instantiate(spmz.spirimonzBodyPrefab, spmzBodyPos.position, spmzBodyPos.rotation, spmzBodyPos);
        spmzBodyPos.localPosition = Vector3.zero + spmz.bodyPresentationOffset;
        currentSpirimonzBody.transform.localScale = Vector3.one * 7f;
        ChangeLayer(currentSpirimonzBody, 5);
    }

    private void SetSpirimonzBooleanFeedbacks(SpirimonzSettings spmz)
    {
        bool powerInHands = spmz.canUsePowerInHands;
        booleanFeedbacks[0].sprite = powerInHands ? yesSprite : nopSprite;
        booleanFeedbacks[0].color = powerInHands ? yesColor : nopColor;
        
        bool canDropOnMap = spmz.canBeDroppedOnMap;
        booleanFeedbacks[1].sprite = canDropOnMap ? yesSprite : nopSprite;
        booleanFeedbacks[1].color = canDropOnMap ? yesColor : nopColor;
        
        bool canGoBackToHands = spmz.canBeTakenBackInHands;
        booleanFeedbacks[2].sprite = canGoBackToHands ? yesSprite : nopSprite;
        booleanFeedbacks[2].color = canGoBackToHands ? yesColor : nopColor;
        
        bool followPlayer = spmz.canFollowPlayer;
        booleanFeedbacks[3].sprite = followPlayer ? yesSprite : nopSprite;
        booleanFeedbacks[3].color = followPlayer ? yesColor : nopColor;
    }

    private void OnEnable()
    {
        if(_lastSpirimonzSettings != null)
            SetSpirimonzBody(_lastSpirimonzSettings);
    }

    private void OnDisable()
    {
        if(currentSpirimonzBody != null)
            Destroy(currentSpirimonzBody);
    }
}