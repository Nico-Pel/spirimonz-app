using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class ClickableKeyShape : ClickableObject
{
    [Header("Skinned Mesh")]
    public SkinnedMeshRenderer skinnedMesh;
    [ReadOnly] public int blendShapeIndex = 0;
    public Collider colliderToDisableOnActivate;

    [Header("BlendShape Values")]
    public float startValue = 0f;
    public float targetValue = 100f;

    [Header("Lerp Speeds")]
    public float lerpSpeed = 5f;
    public float lerpBackSpeed = 8f;

    [Header("Initial State")]
    public bool setIsActivatedFromCurrentValue = true; // <-- option pour auto-détecter
    
    [Header("Sounds")] 
    public AudioClip openSound;
    public AudioClip closeSound;
    public float volume = 0.5f;
    public float pitchMin = 0.9f;
    public float pitchMax = 1.1f;

    private bool _isActivated;
    private Coroutine _currentCoroutine;

    protected override void Awake()
    {
        base.Awake();
        if (skinnedMesh == null || skinnedMesh.sharedMesh == null) return;

        int shapeIndex = -1;

        // Chercher le blendshape par nom (si tu connais le nom du keyShape)
        string keyShapeName = "Key 1"; // <-- remplace par ton keyShape
        for (int i = 0; i < skinnedMesh.sharedMesh.blendShapeCount; i++)
        {
            if (skinnedMesh.sharedMesh.GetBlendShapeName(i) == keyShapeName)
            {
                shapeIndex = i;
                break;
            }
        }

        if (shapeIndex == -1) return; // pas trouvé

        // Lire la vraie valeur actuelle
        float currentValue = skinnedMesh.GetBlendShapeWeight(shapeIndex);

        // Déterminer l'état initial
        _isActivated = Mathf.Abs(currentValue - targetValue) < Mathf.Abs(currentValue - startValue);

        // S'assurer que le blendshape correspond à l'état
        skinnedMesh.SetBlendShapeWeight(shapeIndex, _isActivated ? targetValue : startValue);

        // sauvegarder l'index pour le reste du script
        blendShapeIndex = shapeIndex;
    }

    public override void OnClick()
    {
        base.OnClick();

        if (skinnedMesh == null) return;

        if (_currentCoroutine != null)
            StopCoroutine(_currentCoroutine);

        float from = _isActivated ? targetValue : startValue;
        float to = _isActivated ? startValue : targetValue;
        float speed = _isActivated ? lerpBackSpeed : lerpSpeed;

        _currentCoroutine = StartCoroutine(LerpBlendShape(from, to, speed));

        _isActivated = !_isActivated;

        if(colliderToDisableOnActivate != null)
            colliderToDisableOnActivate.enabled = !_isActivated;
        
        PlaySound();
    }

    private IEnumerator LerpBlendShape(float from, float to, float speed)
    {
        float current = from;

        while (!Mathf.Approximately(current, to))
        {
            current = Mathf.Lerp(current, to, Time.deltaTime * speed);
            skinnedMesh.SetBlendShapeWeight(blendShapeIndex, current);
            yield return null;
        }

        skinnedMesh.SetBlendShapeWeight(blendShapeIndex, to);
    }
    
    private void PlaySound()
    {
        if (openSound == null) return;
        
        AudioClip clip = _isActivated && closeSound != null ? closeSound : openSound;
        SoundManager.Instance?.PlaySound(clip, activitySource.transform.position, volume, Random.Range(pitchMin, pitchMax));
    }
}