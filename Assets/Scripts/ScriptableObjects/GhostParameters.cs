using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

[CreateAssetMenu(menuName = "Ghosts/GhostParameters")]
public class GhostParameters : ScriptableObject
{
    [System.Serializable]
    public class GhostClue
    {
        [TextArea(3, 10)]
        public string description;
    }
    
    [FormerlySerializedAs("ghostType")] public GhostTypeData ghostTypeData;
    
    [Space]
    public GhostClue[] ghostClues;
    [Space]
    
    [Header("Ghost Stats : Hunting")]
    public float minimumAngerToHunt = 50;
    public float averageHuntTime = 10f;
    public float minimumPeaceTime = 60f;

    [Header("Ghost Stats : Anger")]
    public float startingAnger = 5f;
    public float angerToAddByTriggeringPlayer = 5f;
    public float passiveAngerIncreaseAmount = 2f;
    public float passiveAngerIncreaseMinDelay = 60f;
    public float passiveAngerIncreaseMaxDelay = 120f;
    
    [Space]

    [Header("Ghost Stats : Speed")] 
    public float hidingSpeedBase = 0.75f;
    public float normalSpeedBase = 1f;
    public float targetingSpeedBase = 2f;
    
    [Space]
    
    [Header("Ghost Stats : Throwing Forces")] 
    public float throwForceMin = 0.5f;
    public float throwForceMax = 4;
    
    [Space]
    
    [Header("Ghost Stats : Doors interactions")] 
    public float slamChances = 50;
    public float openingDoorSpeedMultiplier = 1f;
    
    [Space]
    
    [Header("Evidences")]
    public bool SpiritPrints;
    public bool EatFruits;
    public bool BlowUpFlammables;
    public bool FreezingTemperature;
    public bool HighSpiritActivities;
    public bool SpiritOrbs;
    public bool Radioactivity;

    public string GetLocalizedClue(int index)
    {
        if (ghostClues == null || index < 0 || index >= ghostClues.Length)
            return string.Empty;

        string fallback = ghostClues[index].description;
        string key = $"ghost_clue.{name}.clue_{index}";
        return LocalizationManager.Get(key, fallback);
    }

    [Header("Spirit Prints")] 
    public float chancesToPutPrintOnDoors = 33;
    public float chancesToPutPrintOnPrintTriggers = 33;
    public float chancesToPutPrintOnSwitch = 33;
    public float chancesToPutPrintOnGround = 33;

    [Header("Eating Fruit")] 
    [Tooltip("Use this data only if you want to use Eating Fruits Activities")]
    public float chancesToEatFruitInsteadOfThrowingIt = 70f;

    [Header("Spirit Activities")]
    public float activityOneChances = 35f;
    public float activityTwoChances = 35f;
    public float activityThreeChances = 20f;
    public float activityFourChances = 10f;
    [Tooltip("Use this data only if you want to use High Spirit Activities")]
    public float chancesToChangeThreeOrFourActivityIntoFive = 30f;

    public float activityTimeMin = 10;
    public float activityTimeMax = 20;

    [Header("Cold temperature")] 
    public float refreshmentAfterActivityMin = -1;
    public float refreshmentAfterActivityMax = -5;

    [Header("Spirit Orbs")] 
    public float nextOrbsDelayMin = 3f;
    public float nextOrbsDelayMax = 10f;

    [Header("Radiations")] 
    public float chancesToDetectRadiationOnTrigger = 10f;
    public float radiationDurationOnTrigger = 5f;
    public float radiationDurationAfterAttack = 15f;

    [Header("Other settings")] 
    [Tooltip("Ignore electronic and lights (Switch)")]
    public float chancesToInteractWithAClickableInstedOfNothing = 50f;

    
    public int GetRandomActivityValue()
    {
        float roll = Random.Range(0f, 100f);
        float cumulative = 0f;

        cumulative += activityOneChances;
        if (roll < cumulative && ghostTypeData.ghostType != GhostTypeData.GhostType.Psychic) return 1;

        cumulative += activityTwoChances;
        if (roll < cumulative) return 2;

        cumulative += activityThreeChances;
        if (roll < cumulative) return HighSpiritActivities && Random.Range(0f, 100f) >= chancesToChangeThreeOrFourActivityIntoFive ? 5 : 3;

        cumulative += activityFourChances;
        if (roll < cumulative) return HighSpiritActivities && Random.Range(0f, 100f) >= chancesToChangeThreeOrFourActivityIntoFive ? 5 : 4;

        // Sécurité si le total < 100
        return 4;
    }

    public float GetRandomActivityTime()
    {
        return Random.Range(activityTimeMin, activityTimeMax);
    }

    public float GetRandomRefreshment()
    {
        float refreshment = Random.Range(refreshmentAfterActivityMin, refreshmentAfterActivityMax);
        return -Math.Abs(refreshment);
    }

    public bool ShouldEatFruit()
    {
        if (!EatFruits) return false;
        
        float roll = Random.Range(0f, 100f);
        return roll <= chancesToEatFruitInsteadOfThrowingIt;
    }

    public bool ShouldDetectRadiationOnTrigger()
    {
        if (Radioactivity == false) return false;
        
        float roll = Random.Range(0f, 100f);
        return roll <= chancesToDetectRadiationOnTrigger;
    }
    
    public bool HasEvidence(GhostInvestigator.EvidenceType type)
    {
        return type switch
        {
            GhostInvestigator.EvidenceType.SpiritPrints => SpiritPrints,
            GhostInvestigator.EvidenceType.EatFruits => EatFruits,
            GhostInvestigator.EvidenceType.BlowUpFlammables => BlowUpFlammables,
            GhostInvestigator.EvidenceType.FreezingTemperature => FreezingTemperature,
            GhostInvestigator.EvidenceType.HighSpiritActivities => HighSpiritActivities,
            GhostInvestigator.EvidenceType.SpiritOrbs => SpiritOrbs,
            GhostInvestigator.EvidenceType.Radioactivity => Radioactivity,
            _ => false
        };
    }

    //Ignore electronic and lights (Switch)
    public bool ShouldInteractWithClickableInsteadOfNothing()
    {
        float roll = Random.Range(0f, 100f);
        return roll <= chancesToInteractWithAClickableInstedOfNothing;
    }

    private void OnValidate()
    {
        if (startingAnger < 0f)
            startingAnger = 0f;

        if (angerToAddByTriggeringPlayer <= 0f)
            angerToAddByTriggeringPlayer = 5f;

        if (passiveAngerIncreaseAmount < 0f)
            passiveAngerIncreaseAmount = 0f;

        if (passiveAngerIncreaseMinDelay < 0f)
            passiveAngerIncreaseMinDelay = 0f;

        if (passiveAngerIncreaseMaxDelay < passiveAngerIncreaseMinDelay)
            passiveAngerIncreaseMaxDelay = passiveAngerIncreaseMinDelay;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(GhostParameters))]
public class GhostParametersEditor : Editor
{
    public override void OnInspectorGUI()
    {
        GhostParameters gp = (GhostParameters)target;

        DrawDefaultInspector();

        float total =
            gp.activityOneChances +
            gp.activityTwoChances +
            gp.activityThreeChances +
            gp.activityFourChances;

        EditorGUILayout.Space();

        if (total > 100f)
        {
            EditorGUILayout.HelpBox(
                $"Spirit Activities total = {total}% (dépasse 100%)",
                MessageType.Error
            );
        }
        else
        {
            EditorGUILayout.HelpBox(
                $"Spirit Activities total = {total}%",
                MessageType.Info
            );
        }
    }
}
#endif
