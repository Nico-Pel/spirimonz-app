using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Room : MonoBehaviour
{
    public ClickableObject[] clickableObjects;
    public ActivableObject[] activableObjects;
    
    public Room[] neighborRooms;
    
    public House house { get; set; }

    public void Initialize(House h)
    {
        house = h;
        foreach (ClickableObject c in clickableObjects)
        {
            c.Initialize(house);
        }
        foreach (ActivableObject a in activableObjects)
        {
            a.Initialize(house);
        }
    }

    public Switch SelectRandomSwitchObject(ActivableObject.ActivationSpecialType forbiddenType = ActivableObject.ActivationSpecialType.none)
    {
        List<Switch> selectableObjects = new List<Switch>();
        foreach (Switch s in clickableObjects)
        {
            if(s.activableObject.activationType != ActivableObject.ActivationSpecialType.none || s.activableObject.activationType != forbiddenType)
                selectableObjects.Add(s);
        }
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
        return selectableObjects[Random.Range(0, selectableObjects.Count)];
    }
}
