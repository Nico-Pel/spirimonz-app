using System;
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
    private float _baseVolume;

    private void Start()
    {
        _source = GetComponent<AudioSource>();
        _lowPass = gameObject.AddComponent<AudioLowPassFilter>();
        
        if(Camera.main != null)
            _listener = Camera.main.transform;
        
        _baseVolume = _source.volume;
    }

    void Update()
    {
        if (_listener == null)
            return;
        
        Vector3 dir = _listener.position - transform.position;
        float dist = dir.magnitude;

        RaycastHit hit;
        bool blocked = Physics.Raycast(
            transform.position,
            dir.normalized,
            out hit,      // <-- ici
            dist,
            occlusionMask,
            QueryTriggerInteraction.Ignore 
        );

        if (blocked)
        {
            if (hit.transform.TryGetComponent<AudioOccluder>(out AudioOccluder occluder))
            {
                blocked = occluder.blockSound;
            }
        }

        float volume = blocked ? occludedVolume : 1f;
        float targetCutoff = blocked ? occludedCutoff : openCutoff;

        _source.volume = Mathf.Lerp(_source.volume, _baseVolume * volume, Time.deltaTime * smoothSpeed);
        _lowPass.cutoffFrequency = Mathf.Lerp(_lowPass.cutoffFrequency, targetCutoff, Time.deltaTime * smoothSpeed);
    }
}