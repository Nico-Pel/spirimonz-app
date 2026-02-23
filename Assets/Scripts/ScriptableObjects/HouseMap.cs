using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "HouseMap", menuName = "HouseMap")]
public class HouseMap : ScriptableObject
{
    [Header("House Settings")] 
    public string houseID;
    public string houseName;
    public Sprite sprite;
    public HouseBiome linkedHouseBiome;
    public Quest[] quests;
    public int entryPrince = 50;
    
    //Use bake rooms button in house scene
    [ReadOnly] public int roomsNumber;
    
    [Header("House's Ghost Settings")]
    public GhostParameters[] possibleGhostParameters;
    public Ghost[] possibleGhosts;

    private void OnValidate()
    {
        houseID = this.name;
    }
}