using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class Room : MonoBehaviour
{
    public enum RoomType
    {
        none,
        bathroom,
        toilet,
        kitchen,
        mainRoom,
        dinningRoom,
        dinningMainRoom,
        bedRoom,
        babyBedRoom,
    }
    
    public RoomType roomType;
    
    [Header("Temperature Settings")]
    [SerializeField] private float currentTemperature = 20f;
    private float _temperatureTarget = 20f;
    public float _smoothSpeed = 1f; // vitesse de transition (°C/sec)
    public float minNormalTemperature = 1f; // température minimale possible
    public float minFreezingTemperature = -2f; // température minimale possible (Freezing)
    public float maxTemperature = 30f; // température maximale possible
    
    public float naturalReequilibriumSpeed = 0.01f;

    private float _startTemperature;
    
    [Header("Radiations in Room")]
    public bool radiationInTheRoom;
    public float radiationDuration;
    public UnityEvent<float> OnRadiationStart;
    public UnityEvent OnRadiationEnd;

    [Header("Objects in Room")]
    public List<ClickableObject> clickableObjects = new List<ClickableObject>();
    public List<ActivableObject> activableObjects = new List<ActivableObject>();

    [Header("Neighbor Rooms")]
    public Room[] neighborRooms;

    public House house { get; set; }

    public void Initialize(House h)
    {
        house = h;
        foreach (ClickableObject c in clickableObjects)
        {
            if (c == null)
                continue;

            c.Initialize(house);
        }

        foreach (ActivableObject a in activableObjects)
        {
            if (a == null)
                continue;

            a.Initialize(house);
        }

        // Initialisation aléatoire de la température
        float temperatureRandomVariation = Random.Range(-house.temperatureMaxRoomVariation, house.temperatureMaxRoomVariation);

        bool isFavoriteRoom = house.currentGhost != null && house.currentGhost.favoriteRoom == this;
        bool isBlazingGhost = house.currentGhost != null &&
                              house.currentGhost.ghostParameters != null &&
                              house.currentGhost.ghostParameters.ghostTypeData != null &&
                              house.currentGhost.ghostParameters.ghostTypeData.ghostType == GhostTypeData.GhostType.Blazing;

        // Default bias: the favorite room is slightly colder (unless Blazing ghosts).
        if (isFavoriteRoom && !isBlazingGhost)
        {
            temperatureRandomVariation = -Mathf.Abs(temperatureRandomVariation);
            float penalty = Mathf.Max(0f, house.favoriteRoomTemperaturePenalty);
            temperatureRandomVariation -= penalty;
        }

        currentTemperature = house.averageStartTemperature + temperatureRandomVariation;
        float minTemperature = GetMinTemperature();
        currentTemperature = Mathf.Clamp(currentTemperature, minTemperature, maxTemperature);
        _temperatureTarget = currentTemperature; // synchronisation initiale
        _startTemperature = currentTemperature;
    }

    public Switch SelectRandomSwitchObject(ActivableObject.ActivationSpecialType forbiddenType = ActivableObject.ActivationSpecialType.none)
    {
        List<Switch> selectableObjects = new List<Switch>();
        foreach (ClickableObject c in clickableObjects)
        {
            Switch s = c as Switch;
            if (s != null)
            {
                if(s.activableObject != null && s.activableObject.activationType != ActivableObject.ActivationSpecialType.none && 
                   s.activableObject.activationType != forbiddenType)
                    selectableObjects.Add(s);
            }
        }
        if (selectableObjects.Count == 0) return null;
        
        Switch randomSwitch = selectableObjects[Random.Range(0, selectableObjects.Count)];
        return randomSwitch;
    }

    public Switch SelectSpecialSwitchObject(ActivableObject.ActivationSpecialType targetedType)
    {
        List<Switch> selectableObjects = new List<Switch>();
        foreach (ClickableObject co in clickableObjects)
        {
            if(co.TryGetComponent(out Switch s) && s.activableObject.activationType == targetedType)
                selectableObjects.Add(s);
        }

        if (selectableObjects.Count == 0)
            return null;

        return selectableObjects[Random.Range(0, selectableObjects.Count)];
    }

    public ClickableObject SelectRandomClickableObject(bool ignoreSwitch = false)
    {
        List<ClickableObject> selectableObjects = new List<ClickableObject>();
        foreach (ClickableObject co in clickableObjects)
        {
            if (ignoreSwitch && co.TryGetComponent(out Switch s)) continue;
            
            selectableObjects.Add(co);
        }

        if (selectableObjects.Count == 0)
            return null;

        return selectableObjects[Random.Range(0, selectableObjects.Count)];
    }

    private void Update()
    {
        // Natural re-equilibrium (VERY slow)
        _temperatureTarget = Mathf.MoveTowards(
            _temperatureTarget,
            _startTemperature,
            naturalReequilibriumSpeed * Time.deltaTime
        );

        // Smooth transition to target
        currentTemperature = Mathf.MoveTowards(
            currentTemperature,
            _temperatureTarget,
            _smoothSpeed * Time.deltaTime
        );

        // Optional subtle noise
        currentTemperature += Random.Range(-0.02f, 0.02f);

        float minTemperature = GetMinTemperature();
        currentTemperature = Mathf.Clamp(currentTemperature, minTemperature, maxTemperature);

        //Radiations
        if (radiationInTheRoom)
        {
            radiationDuration -= Time.deltaTime;
            if (radiationDuration <= 0)
            {
                EndRadiation();
            }
        }
    }

    public void StartRadiation(float duration)
    {
        Debug.Log("Start radiation");
        radiationDuration = duration;
        radiationInTheRoom = true;
        OnRadiationStart?.Invoke(radiationDuration);
    }

    public void StopRadiation()
    {
        if (!radiationInTheRoom)
            return;

        radiationDuration = 0f;
        EndRadiation();
    }

    private void EndRadiation()
    {
        radiationInTheRoom = false;
        radiationDuration = 0;
        OnRadiationEnd?.Invoke();
    }

    /// <summary>
    /// Ajoute un delta de température (positif ou négatif)
    /// Cumulable pour plusieurs sources en même temps
    /// </summary>
    public void AddTemperatureDelta(float delta)
    {
        if (delta < 0f && TutorialManager.IsTutorialActive && TutorialManager.Instance != null)
            delta *= Mathf.Max(0.1f, TutorialManager.Instance.tutorialCoolingMultiplier);

        _temperatureTarget += delta;
        float minTemperature = GetMinTemperature();
        _temperatureTarget = Mathf.Clamp(_temperatureTarget, minTemperature, maxTemperature);
    }

    public void AddTemperatureDeltaClamped(float delta, float minAllowedTemperature)
    {
        if (delta < 0f && TutorialManager.IsTutorialActive && TutorialManager.Instance != null)
            delta *= Mathf.Max(0.1f, TutorialManager.Instance.tutorialCoolingMultiplier);

        _temperatureTarget += delta;
        float minTemperature = GetMinTemperature();
        minTemperature = Mathf.Max(minTemperature, minAllowedTemperature);
        _temperatureTarget = Mathf.Clamp(_temperatureTarget, minTemperature, maxTemperature);
    }

    public void AddHeatingClamped(float delta, float maxAllowedTemperature)
    {
        if (delta <= 0f)
            return;

        _temperatureTarget += delta;
        float minTemperature = GetMinTemperature();
        float maxTemp = Mathf.Min(maxAllowedTemperature, maxTemperature);
        _temperatureTarget = Mathf.Clamp(_temperatureTarget, minTemperature, maxTemp);
    }

    // Pour compatibilité et lisibilité
    public void AddCooling(float value) => AddTemperatureDelta(-value);
    public void AddHeating(float value) => AddTemperatureDelta(value);

    public float GetTemperatureCelsius() => currentTemperature;
    public float GetStartTemperature() => _startTemperature;
    
    public float GetTemperatureFahrenheit()
    { 
        return (currentTemperature * 9f / 5f) + 32f;
    }

    private float GetMinTemperature()
    {
        bool freezingTemperature = house != null &&
                                   house.currentGhost != null &&
                                   house.currentGhost.ghostParameters != null &&
                                   house.currentGhost.ghostParameters.FreezingTemperature;

        return freezingTemperature ? minFreezingTemperature : minNormalTemperature;
    }
}
