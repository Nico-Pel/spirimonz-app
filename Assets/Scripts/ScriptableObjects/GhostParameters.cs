using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEditor;

[CreateAssetMenu(fileName = "GhostParameters", menuName = "GhostParameters")]
public class GhostParameters : ScriptableObject
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
    
    [Space]
    
    [Header("Evidences")] 
    public bool SpiritPrints;
    public bool EatFruits;
    public bool AnswerVocals;
    public bool FreezingTemperature;
    public bool HighSpiritActivities;
    public bool SpiritOrbs;
    public bool Radioactivity;

    [Header("Spirit Prints")] 
    public float chancesToPutPrintOnDoors = 33;
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
    
    public int GetRandomActivityValue()
    {
        float roll = Random.Range(0f, 100f);
        float cumulative = 0f;

        cumulative += activityOneChances;
        if (roll < cumulative) return 1;

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
        return Random.Range(refreshmentAfterActivityMin, refreshmentAfterActivityMax);
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
