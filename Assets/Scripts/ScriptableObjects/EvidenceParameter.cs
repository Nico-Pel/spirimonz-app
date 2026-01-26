using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EvidenceParameters", menuName = "EvidenceParameters")]
public class EvidenceParameter : ScriptableObject
{
    [Header("Linked Evidence")]
    public GhostInvestigator.EvidenceType evidenceType;
    
    [Header("Texts Settings")]
    public string title;
    public string info;
}
