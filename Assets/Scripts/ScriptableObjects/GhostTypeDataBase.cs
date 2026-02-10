using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Ghosts/Ghost Type Database")]
public class GhostTypeDatabase : ScriptableObject
{
    public enum GhostType
    {
        Blazing, //Flamboyant
        Totemic, //Totémique
        Aquatic, //Aqueux
        Glacial, //Glacial
        Misty, //Brumeux
        Demonic, //Démoniaque
        Runic, //Runique
        Grumpy, //Grognon
        Trickster, //Farceur
        Weird, //Bizarre
        Draconic, //Draconique
        Earthbound, //Téllurique
        Psychic, //Psychique
        Striker, //Frappeur
        Voltaic, //Voltaïque
        Luminous, //Lumineux
        DEBUG
    }
    public List<GhostTypeData> ghostTypes;

    private Dictionary<GhostTypeData.GhostType, GhostTypeData> _cache;

    public GhostTypeData Get(GhostTypeData.GhostType type)
    {
        _cache ??= ghostTypes.ToDictionary(g => g.ghostType);
        return _cache[type];
    }
}