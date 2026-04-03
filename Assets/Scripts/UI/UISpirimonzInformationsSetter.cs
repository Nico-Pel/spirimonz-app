using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using UnityEngine.UI;

public class UISpirimonzInformationsSetter : GameBehaviour
{
    public enum EmptyInfoMode
    {
        Empty,
        Remove
    }

    [Header("3D Components")]
    public Transform spmzBodyPos;
    [ReadOnly] public GameObject currentSpirimonzBody;
    public UISpirimonzInformationsSetter[] linkedSpirimonzSetters;
    
    [Header("Texts")]
    public TextMeshProUGUI tSpirimonzName;
    public TextMeshProUGUI[] tSpirimonzAbilities;
    
    [Header("Images")] 
    public Image[] abilityPanels;
    public Image primaryType;
    public Image secondaryType;
    public Image[] booleanFeedbacks;

    public Color abilityPanelBaseColor;
    public Color abilityPanelOffColor;

    [Header("Sprites")] 
    public Sprite nopSprite;
    public Color nopColor;
    public Sprite yesSprite;
    public Color yesColor;

    [Header("Empty/Remove Display")]
    public string emptyName = "Empty";
    [Range(0f, 1f)] public float emptyTextAlpha = 0.35f;
    [Range(0f, 1f)] public float emptyIconAlpha = 0.25f;
    public string removeName = "Remove";
    public Color removeNameColor = new Color(1f, 0.3f, 0.3f, 1f);
    
    private SpirimonzSettings _lastSpirimonzSettings;
    private bool _lastUpdateIncludedBody = true;
    private Color _nameBaseColor;
    private Color[] _abilityBaseColors;
    private Color _primaryTypeBaseColor = Color.white;
    private Color _secondaryTypeBaseColor = Color.white;

    public UnityEvent onInfoChanges;

    private void Awake()
    {
        if (tSpirimonzName != null)
            _nameBaseColor = tSpirimonzName.color;

        if (tSpirimonzAbilities != null && tSpirimonzAbilities.Length > 0)
        {
            _abilityBaseColors = new Color[tSpirimonzAbilities.Length];
            for (int i = 0; i < tSpirimonzAbilities.Length; i++)
            {
                _abilityBaseColors[i] = tSpirimonzAbilities[i] != null
                    ? tSpirimonzAbilities[i].color
                    : Color.white;
            }
        }

        if (primaryType != null)
            _primaryTypeBaseColor = primaryType.color;
        if (secondaryType != null)
            _secondaryTypeBaseColor = secondaryType.color;
    }

    public void SetSpirimonz(SpirimonzSettings spmz)
    {
        ApplySpirimonzInfo(spmz, updateBody: true);
    }

    public void SetSpirimonzInfoOnly(SpirimonzSettings spmz)
    {
        ApplySpirimonzInfo(spmz, updateBody: false);
    }

    private void ApplySpirimonzInfo(SpirimonzSettings spmz, bool updateBody)
    {
        if (spmz == null)
            return;

        if(tSpirimonzName != null)
        {
            tSpirimonzName.text = spmz.spirimonzName;
            tSpirimonzName.color = _nameBaseColor;
        }

        if(primaryType != null)
        {
            if (!primaryType.gameObject.activeSelf)
                primaryType.gameObject.SetActive(true);
            primaryType.sprite = spmz.PrimaryTypeSprite;
            primaryType.color = _primaryTypeBaseColor;
        }
        
        if(secondaryType != null)
        {
            if (!secondaryType.gameObject.activeSelf)
                secondaryType.gameObject.SetActive(true);
            secondaryType.sprite = spmz.SecondaryTypeSprite;
            secondaryType.color = _secondaryTypeBaseColor;
        }

        if (abilityPanels.Length > 0)
        {
            SetSpirimonzAbilities(spmz);
        }

        if (updateBody && spmzBodyPos != null)
        {
            SetSpirimonzBody(spmz);
        }

        if (booleanFeedbacks.Length > 0)
        {
            SetSpirimonzBooleanFeedbacks(spmz);
        }
        
        _lastSpirimonzSettings = spmz;
        _lastUpdateIncludedBody = updateBody;
        onInfoChanges?.Invoke();
    }

    public void SetEmptyInfo(EmptyInfoMode mode, Color? removeColorOverride = null, bool clearBody = true)
    {
        if (clearBody)
            ClearSpirimonzBody();

        _lastSpirimonzSettings = null;

        if (tSpirimonzName != null)
        {
            if (mode == EmptyInfoMode.Remove)
            {
                tSpirimonzName.text = removeName;
                tSpirimonzName.color = removeColorOverride.HasValue ? removeColorOverride.Value : removeNameColor;
            }
            else
            {
                tSpirimonzName.text = emptyName;
                tSpirimonzName.color = WithAlpha(_nameBaseColor, emptyTextAlpha);
            }
        }

        if (primaryType != null)
        {
            primaryType.sprite = null;
            primaryType.color = WithAlpha(_primaryTypeBaseColor, emptyIconAlpha);
            primaryType.gameObject.SetActive(false);
        }

        if (secondaryType != null)
        {
            secondaryType.sprite = null;
            secondaryType.color = WithAlpha(_secondaryTypeBaseColor, emptyIconAlpha);
            secondaryType.gameObject.SetActive(false);
        }

        if (abilityPanels != null && abilityPanels.Length > 0)
        {
            for (int i = 0; i < abilityPanels.Length; i++)
            {
                if (abilityPanels[i] != null)
                    abilityPanels[i].color = WithAlpha(abilityPanelOffColor, emptyIconAlpha);
            }
        }

        if (tSpirimonzAbilities != null && tSpirimonzAbilities.Length > 0)
        {
            for (int i = 0; i < tSpirimonzAbilities.Length; i++)
            {
                if (tSpirimonzAbilities[i] == null)
                    continue;

                tSpirimonzAbilities[i].text = "";
                Color baseColor = (_abilityBaseColors != null && i < _abilityBaseColors.Length)
                    ? _abilityBaseColors[i]
                    : tSpirimonzAbilities[i].color;
                tSpirimonzAbilities[i].color = WithAlpha(baseColor, emptyTextAlpha);
            }
        }

        if (booleanFeedbacks != null && booleanFeedbacks.Length > 0)
        {
            for (int i = 0; i < booleanFeedbacks.Length; i++)
            {
                if (booleanFeedbacks[i] == null)
                    continue;

                booleanFeedbacks[i].sprite = nopSprite;
                booleanFeedbacks[i].color = WithAlpha(nopColor, emptyIconAlpha);
            }
        }

        onInfoChanges?.Invoke();
    }

    private void SetSpirimonzAbilities(SpirimonzSettings spmz)
    {
        int abilityCount = spmz.abilities.Length;
        for (int i = 0; i < abilityPanels.Length; i++)
        {
            bool abilityExist = i < abilityCount;

            if (abilityPanels[i] != null)
                abilityPanels[i].color = abilityExist ? abilityPanelBaseColor : abilityPanelOffColor;
                
            if(tSpirimonzAbilities[i] != null)
            {
                tSpirimonzAbilities[i].text = abilityExist ? spmz.abilities[i].description : "";
                if (_abilityBaseColors != null && i < _abilityBaseColors.Length)
                    tSpirimonzAbilities[i].color = _abilityBaseColors[i];
            }
        }
    }

    private void SetSpirimonzBody(SpirimonzSettings spmz)
    {
        ClearSpirimonzBody();

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
            ApplySpirimonzInfo(_lastSpirimonzSettings, _lastUpdateIncludedBody);
    }

    private void OnDisable()
    {
        if(currentSpirimonzBody != null)
            Destroy(currentSpirimonzBody);
    }

    public SpirimonzSettings GetLastSpirimonzSettings()
    {
        return _lastSpirimonzSettings;
    }

    public void SetTypeIconsVisible(bool visible)
    {
        if (primaryType != null)
            primaryType.gameObject.SetActive(visible);
        if (secondaryType != null)
            secondaryType.gameObject.SetActive(visible);
    }

    private void ClearSpirimonzBody()
    {
        if (currentSpirimonzBody != null)
        {
            Destroy(currentSpirimonzBody);
            currentSpirimonzBody = null;
        }

        foreach (UISpirimonzInformationsSetter linkedSetter in linkedSpirimonzSetters)
        {
            if (linkedSetter.currentSpirimonzBody != null)
            {
                Destroy(linkedSetter.currentSpirimonzBody);
                linkedSetter.currentSpirimonzBody = null;
            }
        }
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = Mathf.Clamp01(alpha);
        return color;
    }
}
