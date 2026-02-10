using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using GameObject = UnityEngine.GameObject;

public class UITablet : MonoBehaviour
{
    [Header("Tabs")]
    public Button[] tabButtons;
    public Image[] tabIcons;
    public Color selectColor;
    public Color selectIconColor;
    public GameObject[] tabWindows;

    private Color _baseColor;
    private Color _baseIconColor;
    
    [Header("Panel animation")]
    public RectTransform tabletPanel;
    public float openDuration = 0.4f;
    public Ease openEase = Ease.OutCubic;
    
    private float _targetWidth;

    private void Awake()
    {
        _baseColor = tabButtons[0].image.color;
        _baseIconColor = tabIcons[0].color;
        
        _targetWidth = tabletPanel.sizeDelta.x;
    }

    private void Start()
    {
        for (int i = 0; i < tabButtons.Length; i++)
        {
            int index = i;
            tabButtons[i].onClick.AddListener(() => OpenTab(index, false));
        }
    }

    public void OpenTab(int tabID, bool canTurnOffTablet)
    {
        //The tab is already open, close it
        if (canTurnOffTablet && tabWindows[tabID].activeInHierarchy)
        {
            tabWindows[tabID].SetActive(false);
            gameObject.SetActive(false);
            return;
        }
        
        for (int i = 0; i < tabWindows.Length; i++)
        {
            bool selected = tabID == i;
            tabWindows[i].SetActive(selected);
            tabButtons[i].image.color = selected ? selectColor : _baseColor;
            tabIcons[i].color = selected ? selectIconColor : _baseIconColor;
        }
    }
    
    private void OnEnable()
    {
        TabletOpenAnimation();
    }

    private void TabletOpenAnimation()
    {
        tabletPanel.DOKill();

        tabletPanel.sizeDelta = new Vector2(50f, tabletPanel.sizeDelta.y);
        tabletPanel.gameObject.SetActive(true);

        tabletPanel
            .DOSizeDelta(new Vector2(_targetWidth, tabletPanel.sizeDelta.y), openDuration)
            .SetEase(openEase);
    }

    public void CloseAllTabs()
    {
        foreach (GameObject g in tabWindows)
        {
            g.SetActive(false);
        }
        gameObject.SetActive(false);
    }
}