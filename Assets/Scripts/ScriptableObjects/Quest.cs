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
    }

    public QuestType type;
    public string questName;
    public string questDescription;
    public int goal;
    
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
}
