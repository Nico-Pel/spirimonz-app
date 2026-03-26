using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class PrintTrigger : GameBehaviour
{
    public bool isAnInstantiatedObject;
    public PrintSource[] printSources;
    private bool _canReceivePrint = false;

    private float _delayBeforeActivation = 3f;

    private void Start()
    {
        //Security, prevent ghost to trigger a print on start
        this.Invoke(_delayBeforeActivation, () => _canReceivePrint = true);
        
        foreach (PrintSource source in printSources)
        {
            source.OnActivate.AddListener(() => _canReceivePrint = false);
            source.OnDeactivate.AddListener(() => _canReceivePrint = true);

            if (isAnInstantiatedObject)
            {
                Debug.Log("Pouet try to declare new print source");
                House.Instance.DeclareNewPrintSource(source);
            }
        }
    }

    public PrintSource GetRandomPrintSource()
    {
        if (printSources == null || printSources.Length == 0 || !_canReceivePrint)
            return null;

        List<PrintSource> available = new List<PrintSource>();
        foreach (var ps in printSources)
            if (!ps.IsActivated())
                available.Add(ps);

        return available.Count > 0 ? available[Random.Range(0, available.Count)] : null;
    }
}