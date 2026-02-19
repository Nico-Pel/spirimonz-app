using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "HouseBiome", menuName = "HouseBiome")]
public class HouseBiome : ScriptableObject
{
    public SpirimonzSettings[] spirimonzInThisBiome;

    public SpirimonzSettings GetCapturedSpirimonz(GhostTypeData.GhostType ghostType)
    {
        //To prevent a crash, if there is no spirimonz in this biome, use 1st spirimonz in datas
        if (spirimonzInThisBiome.Length == 0)
        {
            return GameManager.Instance.allSpirimonzSettings[0];
        }
        
        List<SpirimonzSettings> possibleSpirimonz = new List<SpirimonzSettings>();

        foreach (SpirimonzSettings spmz in spirimonzInThisBiome)
        {
            if (spmz.primarySPMZType.ghostType == ghostType || spmz.secondarySPMZType.ghostType == ghostType)
            {
                possibleSpirimonz.Add(spmz);
            }
        }

        //If there is no spirimonz for this ghost type, use a full random spirimonz instead
        if (possibleSpirimonz.Count == 0)
        {
            foreach (SpirimonzSettings spmz in spirimonzInThisBiome)
            {
                possibleSpirimonz.Add(spmz);
            }
        }
        
        return possibleSpirimonz[Random.Range(0, possibleSpirimonz.Count)];
    }
}