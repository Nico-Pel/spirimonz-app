using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : GameBehaviour
{
    public static Player Instance { get; private set; }

    [ReadOnly] private bool lockControls;
    
    [Header("Player Components")]
    public InteractionController interactionController;
    public FPSControllerNoPhysics fpsController;
    public InventoryManager inventoryManager;
    
    [Header("Player Settings")]
    public Room currentRoom;
    public House house;

    public Transform head;
    public Transform body;

    [Header("Sounds")] 
    public AudioClip deathSound;
    public float deathVolume = 1f;
    public float deathSoundDelay = 1f;
    public AudioClip groundFallSound;
    public float groundFallVolume = 3f;
    public float groundFallDelay = 1.5f;
        
    //Heart beating
    [Header("Sounds : HeartBeating")]
    public AudioClip heartBeating;
    private bool _detectHeartBeat;
    private float _heartBeatMaxDelay = 1.5f;
    private float _heartBeatMinDelay = 0.4f;
    private float _heartBeatMaxVolume = 1f;
    private float _heartBeatMinVolume = 0.1f;
    private float _minDistanceFromGhostToEarHeartBeating = 15f;
    private float _delayBeforeNextBeat;
    
    private bool _isDead;
    private Ghost _ghost;
    
    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        _ghost = House.Instance.currentGhost;
        _ghost.onGhostStartToHunt.AddListener(() => _detectHeartBeat = true);
        _ghost.onGhostStopToHunt.AddListener(() => _detectHeartBeat = false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Room room))
        {
            currentRoom = room;
        }
    }

    public void UseSlashAnimation()
    {
        inventoryManager.handAnimator.SetTrigger("Slash");
    }

    public void AlertTheHuntingGhost()
    {
        if (house.currentGhost == null || house.currentGhost.currentState != Ghost.GhostState.huntingState)
        {
            return;
        }
        
        float distance = Vector3.Distance(this.transform.position, house.currentGhost.transform.position);
        if (distance < house.currentGhost.detectPlayerActivityRange)
        {
            house.currentGhost.ForceNewWaypoint(currentRoom);
        }
    }

    private void Update()
    {
        if (_isDead) return;
        
        HandleHeartBeat();

        if (_delayBeforeNextBeat > 0)
        {
            _delayBeforeNextBeat -= Time.deltaTime;
        }
    }

    private void HandleHeartBeat()
    {
        if (!_detectHeartBeat) return; // Ghost is not hunting

        float distFromGhost = Vector3.Distance(transform.position, _ghost.transform.position);

        if (distFromGhost <= _minDistanceFromGhostToEarHeartBeating && _delayBeforeNextBeat <= 0f)
        {
            // Calcul du volume et du délai entre battements
            float volume = Mathf.Lerp(_heartBeatMaxVolume, _heartBeatMinVolume, distFromGhost / _minDistanceFromGhostToEarHeartBeating);
            float nextBeatDelay = Mathf.Lerp(_heartBeatMinDelay, _heartBeatMaxDelay, distFromGhost / _minDistanceFromGhostToEarHeartBeating);

            // Jouer le battement
            PlayHeartBeat(volume);

            // Préparer le prochain battement
            _delayBeforeNextBeat = nextBeatDelay;
        }
    }

    private void PlayHeartBeat(float volume)
    {
        SoundManager.Instance.PlaySound(
            heartBeating, transform.position, volume, sourceParent: transform, duration: -1f, loop: false);
    }

    public void Die()
    {
        _isDead = true;
        LockControls(true);
        House.Instance.currentGhost.LockGhost();
        
        //Sounds
        SoundManager soundManager = SoundManager.Instance;
        soundManager.PlaySound(House.Instance.currentGhost.killSound, transform.position, 1f, sourceParent: transform, duration: -1f, loop: false);
        
        this.Invoke(deathSoundDelay, () => PlayDeathSound(soundManager));
        this.Invoke(groundFallDelay, () => PlayerFallGroundSound(soundManager));
        this.Invoke(groundFallDelay, () => StopAmbientSound(soundManager));
        
        //Animation
        inventoryManager.handAnimator.SetBool("IsDead", true);
        inventoryManager.handAnimator.SetTrigger("Death");
        
        //UI
        UIGame uiGame = UIGame.Instance;
        uiGame.CloseAllWindows();
        uiGame.EnablePointer(false);
        uiGame.EnableOverlay(true, 7);
    }
    
    public bool IsDead() => _isDead;

    public void LockControls(bool enable)
    {
        if (_isDead == true) enable = true;
        
        lockControls = enable;
    }
    
    public bool AreControlsLocked() => lockControls;

    private void PlayDeathSound(SoundManager soundManager)
    {
        soundManager.PlaySound(deathSound, transform.position, deathVolume, sourceParent: transform, duration: -1f, loop: false);
    }

    private void PlayerFallGroundSound(SoundManager soundManager)
    {
        soundManager.PlaySound(groundFallSound, transform.position, groundFallVolume, sourceParent: transform, duration: -1f, loop: false);
    }

    private void StopAmbientSound(SoundManager soundManager)
    {
        soundManager.StopAmbient(2f);
    }
}