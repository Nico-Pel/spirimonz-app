using UnityEngine;
using DG.Tweening;

public class SpawnObjectOnEvent : GameBehaviour
{
    [Header("Spawn")]
    public GameObject prefabToSpawn;
    public Transform spawnPoint;
    [Range(0f, 1f)] public float spawnChance = 1f;
    [Min(0f)] public float spawnDelay = 0f;
    public Vector3 spawnScaleMultiplier = Vector3.one;
    public bool parentToSpawnPoint = true;
    public bool destroyPreviousSpawn = false;
    [Min(0.01f)] public float spawnScaleFrom = 0.1f;
    [Min(0f)] public float spawnScaleDuration = 0.2f;
    public Ease spawnScaleEase = Ease.OutBack;
    [Min(0f)] public float forwardForce = 0f;
    public ForceMode forwardForceMode = ForceMode.Impulse;

    [Header("Sound")]
    public SoundParameters spawnSoundParameters;
    public SpmzDetector detectorToStopSoundsFrom;
    public bool stopDetectorSoundsBeforeSpawn = false;

    private GameObject _lastSpawnedObject;

    public void SpawnObject()
    {
        if (prefabToSpawn == null)
            return;

        if (Random.value > spawnChance)
            return;

        if (spawnSoundParameters != null)
            spawnSoundParameters.PlaySound();
        
        if (spawnDelay > 0f)
        {
            this.Invoke(spawnDelay, SpawnNow);
            return;
        }

        SpawnNow();
    }

    private void SpawnNow()
    {
        if (prefabToSpawn == null)
            return;

        if (stopDetectorSoundsBeforeSpawn && detectorToStopSoundsFrom != null)
            StopDetectorSounds();

        Transform targetPoint = spawnPoint != null ? spawnPoint : transform;

        if (destroyPreviousSpawn && _lastSpawnedObject != null)
            Destroy(_lastSpawnedObject);

        Transform parent = parentToSpawnPoint ? targetPoint : null;
        _lastSpawnedObject = Instantiate(prefabToSpawn, targetPoint.position, targetPoint.rotation, parent);

        if (_lastSpawnedObject != null)
        {
            Vector3 targetScale = Vector3.Scale(_lastSpawnedObject.transform.localScale, spawnScaleMultiplier);
            _lastSpawnedObject.transform.localScale = Vector3.one * spawnScaleFrom;
            _lastSpawnedObject.transform.DOScale(targetScale, spawnScaleDuration).SetEase(spawnScaleEase);

            Rigidbody spawnedRb = _lastSpawnedObject.GetComponent<Rigidbody>();
            if (spawnedRb != null && forwardForce > 0f)
                spawnedRb.AddForce(targetPoint.forward * forwardForce, forwardForceMode);
        }
    }

    public void SpawnObject(ActivitySource _)
    {
        SpawnObject();
    }

    private void StopDetectorSounds()
    {
        AudioEmitter3D[] emitters = detectorToStopSoundsFrom.GetComponentsInChildren<AudioEmitter3D>(true);
        for (int i = 0; i < emitters.Length; i++)
        {
            AudioEmitter3D emitter = emitters[i];
            if (emitter == null)
                continue;

            emitter.Stop();
            Destroy(emitter.gameObject);
        }
    }
}
