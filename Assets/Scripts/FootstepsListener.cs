using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class FootstepsListener : GameBehaviour
{
    [System.Serializable]
    public class FootstepSounds
    {
        public string groundTag;          // Tag du sol
        public AudioClip[] stepClips;     // Liste de sons possibles pour ce sol
        public float volumeMultiplier = 1f;
        public float pitchMin = 0.95f;
        public float pitchMax = 1.05f;
    }

    [Header("Footsteps")] 
    public float range = 15f;
    public float footstepVolume = 0.025f;
    public FootstepSounds[] footstepSounds; // Paramétrable dans l'Inspector
    public bool ignoreAudioOcclusion = true;
    
    public float groundCheckDistance;
    public LayerMask groundLayers;
    
    public Collider[] ignoredColliders;
    
    public void PlayFootstep(float volumeMultiplier = 1f)
    {
        Ray ray = new Ray(transform.position + Vector3.up * 0.25f, Vector3.down);
        Debug.DrawRay(ray.origin, ray.direction * groundCheckDistance, Color.red);

        RaycastHit[] hits = Physics.RaycastAll(
            ray,
            groundCheckDistance,
            groundLayers,
            QueryTriggerInteraction.Ignore
        );

        if (hits.Length == 0)
            return;
        
        // Toujours trier par distance !
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            if (IsIgnored(hit.collider))
                continue;

            string groundTag = hit.collider.tag;

            foreach (var footstep in footstepSounds)
            {
                if (footstep.groundTag == groundTag && footstep.stepClips.Length > 0)
                {
                    AudioClip clip = footstep.stepClips[
                        Random.Range(0, footstep.stepClips.Length)
                    ];
                    SoundManager.Instance.PlaySound(
                        clip,
                        transform.position,
                        footstepVolume * footstep.volumeMultiplier * volumeMultiplier,
                        Random.Range(footstep.pitchMin, footstep.pitchMax),
                        -1f,
                        range,
                        false,
                        transform,
                        ignoreAudioOcclusion : ignoreAudioOcclusion
                    );
                    return; // IMPORTANT : premier sol valide
                }
            }
        }
    }
    
    bool IsIgnored(Collider col)
    {
        foreach (var ignored in ignoredColliders)
        {
            if (col == ignored)
                return true;
        }
        return false;
    }
}
