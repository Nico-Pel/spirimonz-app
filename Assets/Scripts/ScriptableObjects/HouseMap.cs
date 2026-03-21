using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

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
    public int victoryReward = 100;
    
    //Use bake rooms button in house scene
    [ReadOnly] public int roomsNumber;
    
    [Header("House's Ghost Settings")]
    public GhostParameters[] possibleGhostParameters;
    [FormerlySerializedAs("possibleGhosts")] public Ghost[] ghosts;

    private void OnValidate()
    {
        houseID = this.name;
    }

    public Ghost GetRandomGhost(House h)
    {
        if (h.useDebugs && h.forcedGhostModel != null)
            return h.forcedGhostModel;
        
        List<Ghost> possibleGhosts = new List<Ghost>();

        foreach (Ghost g in ghosts)
        {
            if (g.ghostShape == Ghost.GhostShape.small &&
                h.selectedGhostParameter.ghostTypeData.ghostType == GhostTypeData.GhostType.Draconic)
            {
                continue;
            }
            
            possibleGhosts.Add(g);
        }
        
        return possibleGhosts[Random.Range(0, possibleGhosts.Count)];
    }
}