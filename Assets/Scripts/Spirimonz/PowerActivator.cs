using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PowerFeedbackObject
{
    public GameObject targetObject;
    public Vector3 minScale = Vector3.one * 0.1f;
    public Vector3 maxScale = Vector3.one;
}

[System.Serializable]
public class PowerFeedbackLight
{
    public Light light;
    public float minIntensity = 0f;
    public float maxIntensity = 1f;
}

public class PowerActivator : MonoBehaviour
{
    [Header("Feedbacks")]
    public GameObject[] objectsToEnable; // simple on/off
    public PowerFeedbackObject[] objectsToScale;
    public PowerFeedbackLight[] lightsToScale;

    public SpmzUsePower spirimonz;

    private void Start()
    {
        // Initialise les scales selon l'énergie actuelle si _spirimonz est déjà assigné
        if (spirimonz != null)
        {
            float t = Mathf.Clamp01(spirimonz.CurrentEnergyFraction());
            foreach (var obj in objectsToScale)
            {
                if (obj.targetObject != null)
                    obj.targetObject.transform.localScale =
                        Vector3.Lerp(obj.minScale, obj.maxScale, t);
            }
        }
        
        // Initialise l'état : objets off, lights off
        Deactivate();
    }

    public void Activate()
    {
        // Activer les objets simples
        foreach (GameObject obj in objectsToEnable)
            if (obj != null) obj.SetActive(true);

        // Activer les lights
        foreach (var l in lightsToScale)
            if (l.light != null) l.light.enabled = true;
    }

    public void Deactivate()
    {
        // Objets simples
        foreach (GameObject obj in objectsToEnable)
            if (obj != null) obj.SetActive(false);

        // Lights éteintes
        foreach (var l in lightsToScale)
            if (l.light != null) l.light.enabled = false;
    }

    private void Update()
    {
        if (spirimonz == null) return;
        
        float t = 0f; // facteur pour scale (0..1)
        if (spirimonz != null)
            t = Mathf.Clamp01(spirimonz.CurrentEnergyFraction());
        else
        {
            // Optionnel : si on veut simuler un fill par défaut au Start, on peut t = 0
            t = 0f;
        }

        // Update scale même si le pouvoir n'est pas actif
        foreach (var obj in objectsToScale)
        {
            if (obj.targetObject == null) continue;
            if (!obj.targetObject.activeSelf) continue; // skip si l'objet est désactivé

            float f = (spirimonz != null) ? Mathf.Clamp01(spirimonz.CurrentEnergyFraction()) : 0f;
            obj.targetObject.transform.localScale = Vector3.Lerp(obj.minScale, obj.maxScale, f);
        }
    }
}