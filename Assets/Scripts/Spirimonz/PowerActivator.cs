using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class PowerActivator : GameBehaviour
{
    [Header("Feedbacks")]
    public GameObject[] objectsToEnable;
    public Light feedbackLight;

    [Header("Power Effects")]
    public MonoBehaviour[] powerEffects;

    public void Activate()
    {
        foreach (var go in objectsToEnable)
            go.SetActive(true);

        if (feedbackLight != null)
            feedbackLight.enabled = true;

        foreach (var effect in powerEffects)
            effect.enabled = true;
    }

    public void Deactivate()
    {
        foreach (var go in objectsToEnable)
            go.SetActive(false);

        if (feedbackLight != null)
            feedbackLight.enabled = false;

        foreach (var effect in powerEffects)
            effect.enabled = false;
    }
}