using UnityEngine;

[RequireComponent(typeof(Spirimonz))]
public class SpmzGhostPathPitchSound : GameBehaviour
{
    [Header("References")]
    public Spirimonz linkedSpirimonz;
    public SoundParameters loopSound;

    [Header("Pitch By Path Distance")]
    [Min(0.1f)] public float updateInterval = 0.2f;
    [Min(0.1f)] public float nearDistance = 2f;
    [Min(0.1f)] public float farDistance = 20f;
    public float farPitch = 0.5f;
    public float nearPitch = 1.5f;
    [Min(0.01f)] public float navMeshSampleRadius = 0.5f;
    public bool stopSoundIfPathNotFound = false;

    [Header("Debug")]
    [ReadOnly] public float currentTargetPitch;
    [ReadOnly] public float currentPitchMin;
    [ReadOnly] public float currentPitchMax;

    private Ghost _ghost;
    private float _nextUpdateTime;

    private void Awake()
    {
        if (linkedSpirimonz == null)
            linkedSpirimonz = GetComponent<Spirimonz>();
    }

    private void OnEnable()
    {
        _nextUpdateTime = 0f;
        UpdatePitchRange();
    }

    private void Update()
    {
        if (Time.time < _nextUpdateTime)
            return;

        _nextUpdateTime = Time.time + Mathf.Max(0.01f, updateInterval);
        UpdatePitchRange();
    }

    private float EvaluatePitch()
    {
        Ghost ghost = GetGhost();
        if (ghost == null)
            return farPitch;

        float pathDistance = PathDistance(transform.position, ghost.transform.position, navMeshSampleRadius);
        if (pathDistance < 0f)
            return stopSoundIfPathNotFound ? -1f : farPitch;

        float maxDistance = Mathf.Max(nearDistance, farDistance);
        float minDistance = Mathf.Min(nearDistance, farDistance);
        float t = Mathf.InverseLerp(maxDistance, minDistance, pathDistance);
        return Mathf.Lerp(farPitch, nearPitch, t);
    }

    private Ghost GetGhost()
    {
        if (_ghost != null)
            return _ghost;

        House house = House.Instance;
        if (house == null)
            return null;

        _ghost = house.currentGhost;
        return _ghost;
    }

    private float UpdatePitchRange()
    {
        if (loopSound == null)
            return -1f;

        currentTargetPitch = EvaluatePitch();
        if (currentTargetPitch < 0f)
        {
            currentPitchMin = 0f;
            currentPitchMax = 0f;
            return currentTargetPitch;
        }

        currentPitchMin = Mathf.Max(0.01f, currentTargetPitch - 0.1f);
        currentPitchMax = Mathf.Max(currentPitchMin, currentTargetPitch + 0.1f);
        loopSound.SetPitchRange(currentPitchMin, currentPitchMax);
        return currentTargetPitch;
    }
}
