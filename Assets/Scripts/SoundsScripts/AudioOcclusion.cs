using System;
using UnityEngine;

public class AudioOcclusion : MonoBehaviour
{
    public LayerMask occlusionMask;
    public float occludedVolume = 0.5f;
    public float occludedCutoff = 800f;
    public float openCutoff = 22000f;
    public float smoothSpeed = 8f;
    [Tooltip("If the vertical distance between listener and source is below this value, default occluders (Ground/Ceiling) won't mute the sound.")]
    private float verticalBypassHeight = 2f;

    private AudioSource _source;
    private AudioLowPassFilter _lowPass;
    private Transform _listener;
    private float _baseVolume;
    private int _defaultOccluderMask;
    private RaycastHit[] _hitBuffer = new RaycastHit[16];

    private void Start()
    {
        _source = GetComponent<AudioSource>();
        _lowPass = gameObject.AddComponent<AudioLowPassFilter>();
        
        if(Camera.main != null)
            _listener = Camera.main.transform;
        
        _baseVolume = _source.volume;
        RefreshDefaultOccluderMask();
    }

    private void OnValidate()
    {
        RefreshDefaultOccluderMask();
    }

    void Update()
    {
        if (_listener == null)
            return;
        
        Vector3 dir = _listener.position - transform.position;
        float dist = dir.magnitude;
        float verticalDelta = Mathf.Abs(_listener.position.y - transform.position.y);
        bool bypassDefaultOccluders = verticalBypassHeight > 0f && verticalDelta <= verticalBypassHeight;

        int mask = occlusionMask | _defaultOccluderMask;
        bool blocked = false;
        if (mask != 0)
        {
            int hitCount = Physics.RaycastNonAlloc(
                transform.position,
                dir.normalized,
                _hitBuffer,
                dist,
                mask,
                QueryTriggerInteraction.Ignore
            );

            if (hitCount == _hitBuffer.Length)
            {
                _hitBuffer = new RaycastHit[_hitBuffer.Length * 2];
                hitCount = Physics.RaycastNonAlloc(
                    transform.position,
                    dir.normalized,
                    _hitBuffer,
                    dist,
                    mask,
                    QueryTriggerInteraction.Ignore
                );
            }

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = _hitBuffer[i];
                int layer = hit.collider.gameObject.layer;

                if ((_defaultOccluderMask & (1 << layer)) != 0)
                {
                    if (!bypassDefaultOccluders)
                    {
                        blocked = true;
                        break;
                    }

                    continue;
                }

                if (hit.transform.TryGetComponent<AudioOccluder>(out AudioOccluder occluder))
                {
                    if (occluder.blockSound)
                    {
                        blocked = true;
                        break;
                    }
                    continue;
                }

                blocked = true;
                break;
            }
        }

        float volume = blocked ? occludedVolume : 1f;
        float targetCutoff = blocked ? occludedCutoff : openCutoff;

        _source.volume = Mathf.Lerp(_source.volume, _baseVolume * volume, Time.deltaTime * smoothSpeed);
        _lowPass.cutoffFrequency = Mathf.Lerp(_lowPass.cutoffFrequency, targetCutoff, Time.deltaTime * smoothSpeed);
    }

    private void RefreshDefaultOccluderMask()
    {
        _defaultOccluderMask = 0;
        AddLayerToDefaultMask("Ground");
        AddLayerToDefaultMask("Ceiling");
    }

    private void AddLayerToDefaultMask(string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        if (layer >= 0)
            _defaultOccluderMask |= 1 << layer;
    }
}
