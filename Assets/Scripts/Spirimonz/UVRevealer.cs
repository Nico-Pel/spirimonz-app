using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UVRevealer : GameBehaviour
{
    public float range = 3f;
    public float chargeSpeed = 0.25f;
    public Transform source;

    private List<PrintSource> _printSources;
    private House _house;
    private void Start()
    {
        _printSources = new List<PrintSource>(
            FindObjectsOfType<PrintSource>()
        );

        _house = House.Instance;

        foreach (PrintSource ps in _house.printSourcesAddedToGame)
        {
            if(_printSources.Contains(ps) == false)
                _printSources.Add(ps);
        }
        
        _house.onNewPrintSourceAddedToGame.AddListener(AddNewPrintSourceToList);
    }

    private void AddNewPrintSourceToList(PrintSource newSource)
    {
        if(_printSources.Contains(newSource) == false)
            _printSources.Add(newSource);
    }

    private void Update()
    {
        foreach (PrintSource ps in _printSources)
        {
            if (ps == null) continue;
            
            float dist = Vector3.Distance(source.position, ps.transform.position);
            if (dist <= range)
            {
                ps.ChargingColor(chargeSpeed * Time.deltaTime);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (source == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(source.position, range);
    }
}
