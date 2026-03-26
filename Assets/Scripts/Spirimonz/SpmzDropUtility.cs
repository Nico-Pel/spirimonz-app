using UnityEngine;

public static class SpmzDropUtility
{
    public static T SpawnDrop<T>(
        T prefab,
        Vector3 position,
        Quaternion rotation,
        Transform parent,
        Vector3 force,
        Vector3 torque) where T : Component
    {
        if (prefab == null)
            return null;

        T spawned = Object.Instantiate(prefab, position, rotation, parent);
        if (spawned == null)
            return null;

        ApplyDropForces(spawned.gameObject, force, torque);
        return spawned;
    }

    public static GameObject SpawnDrop(
        GameObject prefab,
        Vector3 position,
        Quaternion rotation,
        Transform parent,
        Vector3 force,
        Vector3 torque)
    {
        if (prefab == null)
            return null;

        GameObject spawned = Object.Instantiate(prefab, position, rotation, parent);
        if (spawned == null)
            return null;

        ApplyDropForces(spawned, force, torque);
        
        ActivitySource newActivitySource = spawned.GetComponentInChildren<ActivitySource>();
        if(newActivitySource != null)
            House.Instance.DeclareNewActivitySource(newActivitySource);
        
        PrintSource newPrintSource = spawned.GetComponentInChildren<PrintSource>();
        if(newPrintSource != null)
            House.Instance.DeclareNewPrintSource(newPrintSource);
        
        return spawned;
    }

    public static void ApplyDropForces(GameObject spawnedObject, Vector3 force, Vector3 torque)
    {
        if (spawnedObject == null)
            return;

        if (spawnedObject.TryGetComponent(out CatchableObject catchableObject))
        {
            catchableObject.ApplyForce(force, torque);
            catchableObject.EnableCollisionSoundImmediate();
            return;
        }

        Rigidbody rb = spawnedObject.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = spawnedObject.GetComponentInChildren<Rigidbody>();
        }

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.WakeUp();
            rb.AddForce(force, ForceMode.Impulse);
            rb.AddTorque(torque, ForceMode.Impulse);
        }
    }
}
