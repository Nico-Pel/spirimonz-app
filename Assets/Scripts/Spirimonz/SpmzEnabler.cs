using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpmzEnabler : Spirimonz
{
    [Header("Objects to enable in cases")]
    
    public GameObject[] turnOnInHands;
    public GameObject[] turnOffInHands;
    
    public GameObject[] turnOnOnMap;
    public GameObject[] turnOffOnMap;

    private FlammableElement[] _childFlammables;

    private void Awake()
    {
        CacheChildFlammables();

        if (GetComponentInParent<Player>())
        {
            EnableHandsElements();
        }
    }

    public override bool GoBackToHands(Transform handPos)
    {
        if (!base.GoBackToHands(handPos))
        {
            return false;
        }
        
        EnableHandsElements();
        return true;
    }

    private void EnableHandsElements()
    {
        foreach (GameObject g in turnOnInHands)
        {
            if (g != null)
                g.SetActive(true);
        }
        foreach (GameObject g in turnOffInHands)
        {
            if (g != null)
                g.SetActive(false);
        }

        RefreshChildFlammables(immediate: false);
    }

    public override void DroppedOnMap()
    {
        base.DroppedOnMap();
        EnableMapElements();
    }

    private void EnableMapElements()
    {
        foreach (GameObject g in turnOnOnMap)
        {
            if (g != null)
                g.SetActive(true);
        }
        foreach (GameObject g in turnOffOnMap)
        {
            if (g != null)
                g.SetActive(false);
        }

        RefreshChildFlammables(immediate: false);
    }

    private void CacheChildFlammables()
    {
        _childFlammables = GetComponentsInChildren<FlammableElement>(true);
    }

    private void RefreshChildFlammables(bool immediate)
    {
        if (_childFlammables == null || _childFlammables.Length == 0)
            CacheChildFlammables();

        if (_childFlammables == null)
            return;

        foreach (FlammableElement flammable in _childFlammables)
        {
            if (flammable == null)
                continue;

            flammable.RefreshFireVisuals(immediate);
        }
    }
}
