using System;
using System.Collections;
using System.Collections.Generic;
using Random = UnityEngine.Random;

public class PrintTrigger : GameBehaviour
{
    public PrintSource[] printSources;
    private bool _canReceivePrint;

    private void Start()
    {
        foreach (PrintSource source in printSources)
        {
            source.OnActivate.AddListener(() => _canReceivePrint = false);
            source.OnDeactivate.AddListener(() => _canReceivePrint = true);
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

    public bool CanReceivePrint() => _canReceivePrint;
}
