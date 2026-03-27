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
    public Sprite icon;

    [Header("Evidence in Books")] 
    public Material linkedMaterial;
    public float offsetYYes = 0f;
    public float offsetYNo = 0.25f;
}
