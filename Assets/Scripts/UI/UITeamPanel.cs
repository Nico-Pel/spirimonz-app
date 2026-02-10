using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class UITeamPanel : GameBehaviour
{
    [Header("3D Components")] 
    public Transform spmzBodyPos;
    [ReadOnly] public GameObject currentSpirimonzBody;
    
    [Header("Texts")]
    public TextMeshProUGUI tSpirimonzName;
    public TextMeshProUGUI[] tSpirimonzAbilities;
    public TextMeshProUGUI[] tSwitchNbs;

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

    [Header("Buttons")] 
    public Button[] switchButtons;
    
    public Color selectColor;
    public Color selectIconColor;
    private Color _baseColor;
    private Color _baseIconColor;
    
    private InventoryManager _inventoryManager;
    private bool _initialized;

    private void Awake()
    {
        _baseColor = switchButtons[0].image.color;
        _baseIconColor = tSwitchNbs[0].color;
        _abilityPanelBaseColor = abilityPanels[0].color;
        _abilityPanelOffColor = new Color(_abilityPanelBaseColor.r, _abilityPanelBaseColor.g, _abilityPanelBaseColor.b, 0.2f);
    }

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (Player.Instance is GamePlayer gamePlayer)
        {
            _inventoryManager = gamePlayer.inventoryManager;
        }
        
        for (int i = 0; i < switchButtons.Length; i++)
        {
            bool isNull = _inventoryManager.spirimonzTeam[i] == null;
            switchButtons[i].interactable = !isNull;
            if (isNull == false)
            {
                int index = i;
                switchButtons[i].onClick.AddListener(() => SelectSpirimonz(index));
            }
        }

        _initialized = true;
        SelectTargetedSpirimonz();
    }

    private void OnEnable()
    {
        SelectTargetedSpirimonz();
    }

    private void SelectTargetedSpirimonz()
    {
        if (_inventoryManager != null && _initialized)
        {
            int indexToFocus = _inventoryManager.currentSelectedIndex;
            if (indexToFocus < 0)
            {
                indexToFocus = 0;
            }
            SelectSpirimonz(indexToFocus);
        }
    }

    private void SelectSpirimonz(int teamID)
    {
        SpirimonzSettings spmz = _inventoryManager.spirimonzTeamSettings[teamID];
        tSpirimonzName.text = spmz.spirimonzName;

        primaryType.sprite = spmz.PrimaryTypeSprite;
        secondaryType.sprite = spmz.SecondaryTypeSprite;

        int abilityCount = spmz.abilitiesDescriptions.Length;
        for (int i = 0; i < abilityPanels.Length; i++)
        {
            if (i >= abilityCount)
            {
                abilityPanels[i].color = _abilityPanelOffColor;
                tSpirimonzAbilities[i].text = "";
            }
            else
            {
                abilityPanels[i].color = _abilityPanelBaseColor;
                tSpirimonzAbilities[i].text = spmz.abilitiesDescriptions[i];
            }
        }

        if (currentSpirimonzBody != null)
        {
            Destroy(currentSpirimonzBody);
        }
        
        currentSpirimonzBody = Instantiate(spmz.spirimonzBodyPrefab, spmzBodyPos.position, spmzBodyPos.rotation, spmzBodyPos);
        spmzBodyPos.localPosition = Vector3.zero + spmz.bodyPresentationOffset;
        currentSpirimonzBody.transform.localScale = Vector3.one * 7f;
        ChangeLayer(currentSpirimonzBody, 5);

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

        for (int i = 0; i < switchButtons.Length; i++)
        {
            bool isSelected = i == teamID;
            switchButtons[i].image.color = isSelected ? selectColor : _baseColor;
            tSwitchNbs[i].color = isSelected ? selectIconColor : _baseIconColor;
        }
    }
}