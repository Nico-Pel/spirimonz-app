using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "SpirimonzSettings", menuName = "SpirimonzSettings")]
public class SpirimonzSettings : ScriptableObject
{
    [ReadOnly]public string spirimonzID = "spiri000";
    public string spirimonzName;
    
    public Spirimonz spirimonzPrefab;
    public GameObject spirimonzBodyPrefab;
    public Vector3 bodyPresentationOffset;

    [Space]
    
    [TextArea(3, 10)]
    public string[] abilitiesDescriptions;
    
    [Space]
    
    public GhostTypeData primarySPMZType;
    [FormerlySerializedAs("secundarySPMZType")] public GhostTypeData secondarySPMZType;
    
    public Sprite PrimaryTypeSprite =>
        primarySPMZType.ghostSprite;

    public Sprite SecondaryTypeSprite =>
        secondarySPMZType.ghostSprite;

    [Space]
    
    public bool canUsePowerInHands;
    public bool canBeDroppedOnMap;
    public bool canBeTakenBackInHands;
    public bool canFollowPlayer;

    private void OnValidate()
    {
        spirimonzID = this.name;
    }
}
