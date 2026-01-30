using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UVRevealer : GameBehaviour
{
    public float range = 5f;
    public float chargeSpeed = 0.1f;
    public Transform source;

    private List<PrintSource> _printSources;

    private void Awake()
    {
        _printSources = new List<PrintSource>(
            FindObjectsOfType<PrintSource>()
        );
    }

    private void Update()
    {
        foreach (var ps in _printSources)
        {
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
