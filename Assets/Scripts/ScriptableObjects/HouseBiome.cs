using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "HouseBiome", menuName = "HouseBiome")]
public class HouseBiome : ScriptableObject
{
    [System.Serializable]
    public class GhostSpirimonzLink
    {
        public GhostParameters.GhostType ghostType;
        public Spirimonz spirimonzPrefab;
    }
    
    [Header("Ghost → Spirimonz mapping")]
    public List<GhostSpirimonzLink> spirimonzByGhostType = new();

    private Dictionary<GhostParameters.GhostType, Spirimonz> _cache;

    private void BuildCache()
    {
        _cache = new Dictionary<GhostParameters.GhostType, Spirimonz>();

        foreach (var link in spirimonzByGhostType)
        {
            if (!_cache.ContainsKey(link.ghostType) && link.spirimonzPrefab != null)
                _cache.Add(link.ghostType, link.spirimonzPrefab);
        }
    }

    public Spirimonz GetSpirimonzPrefab(GhostParameters.GhostType ghostType)
    {
        if (_cache == null)
            BuildCache();

        return _cache.TryGetValue(ghostType, out var prefab)
            ? prefab
            : null;
    }
}