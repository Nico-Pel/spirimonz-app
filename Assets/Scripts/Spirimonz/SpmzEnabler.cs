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

    private void Awake()
    {
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
            g.SetActive(true);
        }
        foreach (GameObject g in turnOffInHands)
        {
            g.SetActive(false);
        }
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
            g.SetActive(true);
        }
        foreach (GameObject g in turnOffOnMap)
        {
            g.SetActive(false);
        }
    }
}
