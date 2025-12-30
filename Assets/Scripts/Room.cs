using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Room : MonoBehaviour
{
    [Header("Temperature Settings")]
    public float currentTemperature = 20f;
    private float _temperatureTarget = 20f;
    public float _smoothSpeed = 1f; // vitesse de transition (°C/sec)
    public float minTemperature = -2f; // température minimale possible
    public float maxTemperature = 30f; // température maximale possible

    [Header("Objects in Room")]
    public ClickableObject[] clickableObjects;
    public ActivableObject[] activableObjects;

    [Header("Neighbor Rooms")]
    public Room[] neighborRooms;

    public House house { get; set; }

    public void Initialize(House h)
    {
        house = h;
        foreach (ClickableObject c in clickableObjects)
            c.Initialize(house);

        foreach (ActivableObject a in activableObjects)
            a.Initialize(house);

        // Initialisation aléatoire de la température
        float temperatureRandomVariation = Random.Range(-house.temperatureMaxRoomVariation, house.temperatureMaxRoomVariation);

        // La pièce favorite du fantôme ne peut pas avoir de variation négative sauf pour les fantômes Blazing
        if (house.currentGhost.favoriteRoom == this && 
            house.currentGhost.ghostParameters.ghostType != GhostParameters.GhostType.Blazing && 
            temperatureRandomVariation < 0)
        {
            temperatureRandomVariation = -temperatureRandomVariation;
        }

        currentTemperature = house.averageStartTemperature + temperatureRandomVariation;
        _temperatureTarget = currentTemperature; // synchronisation initiale
    }

    public Switch SelectRandomSwitchObject(ActivableObject.ActivationSpecialType forbiddenType = ActivableObject.ActivationSpecialType.none)
    {
        List<Switch> selectableObjects = new List<Switch>();
        foreach (Switch s in clickableObjects)
        {
            if(s.activableObject.activationType != ActivableObject.ActivationSpecialType.none && 
               s.activableObject.activationType != forbiddenType)
                selectableObjects.Add(s);
        }
        if (selectableObjects.Count == 0) return null;
        return selectableObjects[Random.Range(0, selectableObjects.Count)];
    }

    public Switch SelectSpecialSwitchObject(ActivableObject.ActivationSpecialType forbiddenType)
    {
        List<Switch> selectableObjects = new List<Switch>();
        foreach (Switch s in clickableObjects)
        {
            if(s.activableObject.activationType == forbiddenType)
                selectableObjects.Add(s);
        }

        if (selectableObjects.Count == 0)
            return null;

        return selectableObjects[Random.Range(0, selectableObjects.Count)];
    }

    private void Update()
    {
        // Interpolation douce vers la température cible
        currentTemperature = Mathf.MoveTowards(currentTemperature, _temperatureTarget, _smoothSpeed * Time.deltaTime);

        // Petit bruit naturel pour plus de réalisme
        currentTemperature += Random.Range(-0.02f, 0.02f);

        // Clamp pour rester dans des limites réalistes
        currentTemperature = Mathf.Clamp(currentTemperature, minTemperature, maxTemperature);
    }

    /// <summary>
    /// Ajoute un delta de température (positif ou négatif)
    /// Cumulable pour plusieurs sources en même temps
    /// </summary>
    public void AddTemperatureDelta(float delta)
    {
        _temperatureTarget += delta;
        _temperatureTarget = Mathf.Clamp(_temperatureTarget, minTemperature, maxTemperature);
    }

    // Pour compatibilité et lisibilité
    public void AddCooling(float value) => AddTemperatureDelta(-value);
    public void AddHeating(float value) => AddTemperatureDelta(value);
}