using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Ghosts/Ghost Type Data")]
public class GhostTypeData : ScriptableObject
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
    
    public GhostType ghostType;
    public Sprite ghostSprite;
}