using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpmzZoneUV : Spirimonz
{
    public float range = 5f; // Distance de détection
    public float chargeSpeed = 0.1f; // Valeur ajoutée par seconde
    
    public Transform UVsourceTransform;

    public List<PrintSource> printSources = new List<PrintSource>();
    public override void InitSpirimonz()
    {
        base.InitSpirimonz();
        printSources.AddRange(FindObjectsOfType<PrintSource>());
    }
    public override void UpdateSpirimonzBehaviour()
    {
        base.UpdateSpirimonzBehaviour();
        
        foreach (PrintSource ps in printSources)
        {
            float dist = Vector3.Distance(UVsourceTransform.position, ps.transform.position);
            if (dist <= range)
            {
                ps.ChargingColor(chargeSpeed * Time.deltaTime);
            }
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}
