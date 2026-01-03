using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(AudioSource))]
public class Door : GameBehaviour
{
    [Header("Door Components")]
    public HingeJoint hingeJoint;
    public Rigidbody rb;
    public AudioSource audioSource;
    public ActivitySource activitySource;
    public PrintSource[] printSources;

    [Header("Door Settings")] 
    public float closeAngle = 0;
    public float openFullAngle = -90;

    public float autoCloseSpeed = 10f;
    public float checkDelay = 0.2f;
    public float slamAngleDetected = 20;
    public float slamDetectionDuration = 0.2f;
    public float closeAnglePermissiveness = 5f;
    
    public bool slamDetected = false;

    private float _lastAngle;
    private float _almostCloseAngle;
    private bool _askedForGhostSlam = false;
    private bool _ghostJustInteracted;

    [Header("Door Sounds")]
    public AudioClip openSound;
    public AudioClip closeSound;
    public AudioClip slamSound;

    private float previousAngle = 0f;
    [ReadOnly] public bool isOpen = false;

    private bool _isGrabbed = false; // vrai quand le joueur manipule la porte

    private void Start()
    {
        closeAngle = Mathf.Abs(hingeJoint.angle);
        _almostCloseAngle = closeAngle += closeAnglePermissiveness;
    }

    public void Grab()
    {
        _lastAngle = Mathf.Abs(hingeJoint.angle);
        _isGrabbed = true;
        this.Invoke(checkDelay, CheckAngle);
        _askedForGhostSlam = false;
    }

    private void StopDoor()
    {
        JointMotor motor = hingeJoint.motor;
        motor.force = 0;
        motor.targetVelocity = 0;
        hingeJoint.motor = motor;
        
        rb.freezeRotation = true;
    }

    public void Release()
    {
        _isGrabbed = false;
        if (isOpen == true && Mathf.Abs(hingeJoint.angle) < _almostCloseAngle)
        {
            CloseDoor(autoCloseSpeed);
        }
        else
        {
            StopDoor();
        }
    }

    public void CloseDoor(float closeSpeed, bool forcedSlam = false)
    {
        isOpen = false;
        HingeClose(closeSpeed);

        if (slamDetected || forcedSlam)
        {
            PlaySound(slamSound);
        }
        else
        {
            PlaySound(closeSound);
        }
    }

    public void GhostDoorInteraction(float openPercentage, float moveSpeed, bool slam = false)
    {
        rb.freezeRotation = false;
        
        _ghostJustInteracted = true;
        _askedForGhostSlam = slam;
        this.Invoke(0.75f, () => _ghostJustInteracted = false);

        if (openPercentage > 0)
        {
            isOpen = true;
            PlaySound(openSound);
        }
        
        float targetAngle = GetTargetedAngle(openPercentage);
        ForcedHinge(targetAngle, moveSpeed);
    }

    public float GetTargetedAngle(float openPercentage)
    {
        // Clamp pour être sûr que openPercentage est entre 0 et 1
        openPercentage = Mathf.Clamp01(openPercentage);

        float targetAngle = closeAngle + (openFullAngle - closeAngle) * openPercentage;
        return targetAngle;
    }
    
    private void HingeClose(float closeSpeed)
    {
        if (hingeJoint == null) return;

        // On active le moteur
        hingeJoint.useMotor = true;

        // Calcul du delta d'angle entre la position actuelle et la position cible (0°)
        float currentAngle = hingeJoint.angle; // angle actuel du HingeJoint
        float targetAngle = closeAngle;

        // La vitesse du moteur doit être négative si l'angle est positif pour fermer
        float motorVelocity = (currentAngle > targetAngle) ? -Mathf.Abs(closeSpeed) : Mathf.Abs(closeSpeed);

        JointMotor motor = hingeJoint.motor;
        motor.force = 100;
        motor.targetVelocity = motorVelocity;
        hingeJoint.motor = motor;
    }
    
    private void ForcedHinge(float targetAngle, float moveSpeed)
    {
        if (hingeJoint == null) return;

        float currentAngle = hingeJoint.angle;
        float delta = targetAngle - currentAngle;

        // Si on est déjà proche du target → stop
        if (Mathf.Abs(delta) < 0.5f) // tolérance 0.5°
        {
            hingeJoint.useMotor = false;
            return;
        }

        // Vitesse proportionnelle au delta (pour éviter de rater le target)
        float motorVelocity = Mathf.Sign(delta) * Mathf.Min(moveSpeed, Mathf.Abs(delta) * 10f);

        JointMotor motor = hingeJoint.motor;
        motor.force = 100;
        motor.targetVelocity = motorVelocity;
        hingeJoint.motor = motor;
        hingeJoint.useMotor = true;
    }

    private void Update()
    {
        if (_isGrabbed)
        {
            Debug.Log("ANGLE " + hingeJoint.angle);
        }
        else if(isOpen && Mathf.Abs(hingeJoint.angle) < _almostCloseAngle && !_ghostJustInteracted)
        {
            CloseDoor(10, _askedForGhostSlam);
        }
        else if (isOpen && !_ghostJustInteracted && hingeJoint.velocity < 2)
        {
            StopDoor();
        }
    }

    private void CheckAngle()
    {
        if (Mathf.Abs(_lastAngle) - Mathf.Abs(hingeJoint.angle) > slamAngleDetected)
        {
            slamDetected = true;
            this.Invoke(slamDetectionDuration, () => slamDetected = false);
        }
            
        _lastAngle = hingeJoint.angle;

        if (isOpen == false && Mathf.Abs(hingeJoint.angle) > _almostCloseAngle)
        {
            isOpen = true;
            PlaySound(openSound);
        }

        if (!_isGrabbed)
            return;

        this.Invoke(checkDelay, CheckAngle);
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip == null || audioSource == null) return;
        audioSource.PlayOneShot(clip);
        Debug.Log("SON " + clip.name);
    }

    public bool IsGrabbed()
    {
        return _isGrabbed;
    }

    public PrintSource GetRandomPrintSource()
    {
        if (printSources.Length == 0) return null;
        
        List<PrintSource> possiblePrintSources = new List<PrintSource>();
        foreach (PrintSource printSource in printSources)
        {
            if(printSource.IsActivated() == false)
                possiblePrintSources.Add(printSource);
        }
        return possiblePrintSources[Random.Range(0, possiblePrintSources.Count)];
    }
}