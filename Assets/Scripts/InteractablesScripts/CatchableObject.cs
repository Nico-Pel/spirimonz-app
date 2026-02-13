using System;
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;
using Random = UnityEngine.Random;

[RequireComponent(typeof(ActivitySource))]
public class CatchableObject : GameBehaviour, IInteractable
{
    public bool canBeGrabByPlayer = true;
    public bool canBeThrownByGhost = true;
    public bool canBeThrownByPlayer = true;
    public bool setRotZeroOnDrop = true;

    public Rigidbody rb;
    public ActivitySource activitySource;

    [ReadOnly] public bool isGrabbed;

    [Header("Sounds")] 
    public AudioClip[] collisionSounds;
    public float collisionSoundVolume = 0.5f;
    public float collisionSoundAveragePitch = 1f;
    public float collisionSoundVariationPitch = 0.1f;
    public float collisionSoundRange = 15f;
    public float minForceToPlayCollision = 1f;

    private bool _canCallCollisionSound = false;
    private float _collisionSoundsMinDelay = 0.5f;
    private float _collisionStartDelay = 1f;

    private Transform _currentHolder;

    private void Awake()
    {
        if (activitySource == null)
            activitySource = GetComponent<ActivitySource>();

        if (rb == null)
            rb = GetComponent<Rigidbody>();

        if (!canBeGrabByPlayer)
            InteractionLocked = true;
        
        this.Invoke(_collisionStartDelay, () => _canCallCollisionSound = true);
    }

    // =========================
    // IInteractable
    // =========================

    public Sprite SpecialCursor { get; set; }
    public float CursorSize { get; set; }

    public void OnInteractStart()
    {
        // NE FAIT RIEN pour l'instant
        // Le Player continue d'utiliser GrabObject()
    }

    public void OnInteractHold() { }

    public void OnInteractEnd() { }
    public bool InteractionLocked { get; set; }

    // =========================
    // Logique EXISTANTE (copiée du Player)
    // =========================

    public void Grab(Transform handPosition)
    {
        if (isGrabbed) return;
        
        isGrabbed = true;
        _currentHolder = handPosition;

        transform.SetParent(handPosition);
        rb.isKinematic = true;
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        
        OnGrab();
    }

    public void Drop(Vector3 dropPosition, Vector3 throwForce)
    {
        if (!isGrabbed) return;

        transform.parent = House.Instance.transform;
        transform.position = dropPosition;

        rb.isKinematic = false;
        rb.AddForce(throwForce, ForceMode.Impulse);

        isGrabbed = false;
        _currentHolder = null;

        if (throwForce != Vector3.zero)
        {
            OnThrow();
        }
        else
        {
            OnDrop();
        }
    }

    public virtual void OnDrop()
    {
        if (setRotZeroOnDrop)
        {
            transform.DORotate(Vector3.zero, 0.5f);
        }
    }

    public virtual void OnGrab()
    {
        
    }
    public virtual void OnThrow()
    {
        
    }
    
    public void ApplyForce(Vector3 force, Vector3 torque = default)
    {
        rb.isKinematic = false;
        rb.AddForce(force, ForceMode.Impulse);
        rb.AddTorque(torque, ForceMode.Impulse);
    }

    public virtual void SpecialActionInHandsOnClick()
    {
        
    }

    private void OnCollisionEnter(Collision collision)
    {
        float impactForce = collision.relativeVelocity.magnitude;

        if (impactForce > minForceToPlayCollision)
            OnCollision(collision.transform, impactForce);
    }

    protected virtual void OnCollision(Transform other, float impactForce)
    {
        PlayCollisionSound(impactForce);
    }

    private void PlayCollisionSound(float impactForce)
    {
        if (collisionSounds.Length == 0|| !_canCallCollisionSound) return;

        AudioClip clipToUse = null;
        clipToUse = collisionSounds[Random.Range(0, collisionSounds.Length)];
        if (clipToUse == null)
        {
            //If selected clip is null, select first viable sound
            foreach (AudioClip clip in collisionSounds)
            {
                if (clip != null)
                {
                    clipToUse = clip;
                    break;
                }
            }
        }

        //No viable audio clip
        if (clipToUse == null)
            return;
        
        _canCallCollisionSound = false;
        this.Invoke(_collisionSoundsMinDelay, () => _canCallCollisionSound = true);

        float volumeMultiplier = Mathf.Clamp01(impactForce / 10f); // normalise impactForce
        float pitch = collisionSoundAveragePitch + Random.Range(-collisionSoundVariationPitch, collisionSoundVariationPitch);

        SoundManager.Instance.PlaySound(
            clipToUse,
            position: transform.position,
            volume: collisionSoundVolume * volumeMultiplier,
            pitch: pitch,
            range: collisionSoundRange
        );
    }
}