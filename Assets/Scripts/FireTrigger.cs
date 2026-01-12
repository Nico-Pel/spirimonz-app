using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireTrigger : MonoBehaviour
{
    [Tooltip("Having a linked fireable object is not an obligation")]
    public FireableElement linkedFireableObject;

    public bool canGiveFire = true;
    
    private void OnTriggerEnter(Collider other)
    {
        if (canGiveFire == false) return;
        
        if (linkedFireableObject != null)
        {
            if (linkedFireableObject.IsOnFire() == false) return;
        }
        
        if (other.TryGetComponent(out FireableElement otherFire))
        {
            if (otherFire.IsOnFire() == false)
            {
                otherFire.EnableFire(true);
            }
        }
    }
}