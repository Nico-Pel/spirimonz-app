using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class FireTrigger : MonoBehaviour
{
    [Tooltip("Having a linked flammable object is not an obligation")]
    public FlammableElement linkedFlammableObject;

    public bool canGiveFire = true;
    
    private void OnTriggerEnter(Collider other)
    {
        if (canGiveFire == false) return;
        
        if (linkedFlammableObject != null)
        {
            if (linkedFlammableObject.IsOnFire() == false) return;
        }
        
        if (other.TryGetComponent(out FlammableElement otherFire))
        {
            if (otherFire.IsOnFire() == false)
            {
                otherFire.EnableFire(true);
            }
        }
    }
}