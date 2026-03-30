using System.Collections.Generic;
using UnityEngine;

public class TutorialDropZone : MonoBehaviour
{
    public string zoneId;
    public Collider[] colliders;
    public bool autoCollectChildren = true;

    public void CollectColliders(List<Collider> results)
    {
        if (results == null)
            return;

        if ((colliders == null || colliders.Length == 0) && autoCollectChildren)
            colliders = GetComponentsInChildren<Collider>(true);

        if (colliders == null)
            return;

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider col = colliders[i];
            if (col != null)
                results.Add(col);
        }
    }
}
