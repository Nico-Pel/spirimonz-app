using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Quest", menuName = "Quest")]
public class Quest : ScriptableObject
{
    public enum QuestType
    {
        TryToCapture,
        Capture,
        LightACandle,
        CaptureWithMinUsefulForEvidence,
        CaptureWithOnlyUsefulForEvidence,
        CaptureWithoutUsefulForEvidence,
        CaptureWithoutUsefulForEvidenceDouble,
        CaptureWithMaxTeamCount,
        CaptureWithSingleSpirimonz,
        CaptureAfterSeenByGhost,
        CaptureUnderTime,
        CaptureGhostType,
        CaptureWithNoDroppableSpirimonz,
        CaptureWithOnlyDroppableSpirimonz
    }

    public QuestType type;
    public string questName;
    [TextArea] public string questNameFrench;
    public string questDescription;
    [TextArea] public string questDescriptionFrench;
    public int goal;
    public int rewardPrice = 50;

    [Header("Capture Conditions")]
    public GhostInvestigator.EvidenceType evidenceType;
    public GhostInvestigator.EvidenceType evidenceTypeB;
    public bool useSecondEvidenceType;
    public int minUsefulCount = 1;
    public int maxTeamCount = 3;
    public float minSeenSeconds = 5f;
    public float maxMinutes = 5f;
    public GhostTypeData.GhostType ghostType;
    public GhostTypeData.GhostType ghostTypeB;
    public bool useSecondGhostType;
    
    public bool IsCompleted(string houseID)
    {
        int questProgress = GameManager.Instance.GetQuestProgress(this, houseID);
        bool questComplete = false;
        
        if (questProgress >= goal)
        {
            questProgress = goal;
            questComplete = true;
        }

        return questComplete;
    }

    public string GetLocalizedName()
    {
        if (LanguageManager.CurrentLanguage == Language.French && !string.IsNullOrWhiteSpace(questNameFrench))
            return questNameFrench;

        return questName;
    }

    public string GetLocalizedDescription()
    {
        if (LanguageManager.CurrentLanguage == Language.French && !string.IsNullOrWhiteSpace(questDescriptionFrench))
            return questDescriptionFrench;

        return questDescription;
    }
}
