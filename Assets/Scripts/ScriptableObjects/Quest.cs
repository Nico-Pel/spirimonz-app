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
}
