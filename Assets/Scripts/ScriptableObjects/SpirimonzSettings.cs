using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using System.Linq;

[CreateAssetMenu(fileName = "SpirimonzSettings", menuName = "SpirimonzSettings")]
public class SpirimonzSettings : ScriptableObject
{
    [System.Serializable]
    public class AbilitySettings
    {
        public GhostInvestigator.EvidenceType[] evidenceTypes;
        [TextArea(3, 10)]
        public string description;
    }

    [Space]
    public bool unlockedByDefault;
    [Space]
    
    [ReadOnly]public string spirimonzID = "spiri000";
    public string spirimonzName;
    
    public Spirimonz spirimonzPrefab;
    public GameObject spirimonzBodyPrefab;
    public Vector3 bodyPresentationOffset;

    [Space] 
    
    public Sprite img;
    
    [Space]
    public AbilitySettings[] abilities;
    
    [Space]
    
    public GhostTypeData primarySPMZType;
    public GhostTypeData secondarySPMZType;
    
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

    public bool IsUsefulForEvidence(GhostInvestigator.EvidenceType evidenceType)
    {
        return abilities.Any(a =>
            a.evidenceTypes != null &&
            a.evidenceTypes.Contains(evidenceType));
    }
}