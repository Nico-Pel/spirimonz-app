using UnityEngine;

public class AudioOcclusion : MonoBehaviour
{
    public LayerMask occlusionMask;
    public float occludedVolume = 0.5f;
    public float occludedCutoff = 800f;
    public float openCutoff = 22000f;
    public float smoothSpeed = 8f;

    private AudioSource _source;
    private AudioLowPassFilter _lowPass;
    private Transform _listener;

    void Awake()
    {
        _source = GetComponent<AudioSource>();
        _lowPass = gameObject.AddComponent<AudioLowPassFilter>();
        _listener = Camera.main.transform;
    }

    void Update()
    {
        Vector3 dir = _listener.position - transform.position;
        float dist = dir.magnitude;

        bool blocked = Physics.Raycast(
            transform.position,
            dir.normalized,
            dist,
            occlusionMask
        );

        float targetVolume = blocked ? occludedVolume : 1f;
        float targetCutoff = blocked ? occludedCutoff : openCutoff;

        _source.volume = Mathf.Lerp(_source.volume, targetVolume, Time.deltaTime * smoothSpeed);
        _lowPass.cutoffFrequency = Mathf.Lerp(_lowPass.cutoffFrequency, targetCutoff, Time.deltaTime * smoothSpeed);
    }
}
