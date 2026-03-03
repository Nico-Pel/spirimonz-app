using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GamePlayer : Player
{
    [Header("Player Components")]
    public InteractionController interactionController;
    public FPSControllerNoPhysics fpsController;
    public Transform spirimonzHandPos;
    public Animator handAnimator;
    
    [Header("Player Settings")]
    [ReadOnly] public Room currentRoom;
    [ReadOnly] public House house;

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
    
    private Ghost _ghost;

    protected override void Start()
    {
        base.Start();
        
        _ghost = House.Instance.currentGhost;
        _ghost.onGhostStartToHunt.AddListener(() => _detectHeartBeat = true);
        _ghost.onGhostStopToHunt.AddListener(() => _detectHeartBeat = false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Room room))
        {
            SetCurrentRoom(room);
        }
    }

    private void SetCurrentRoom(Room room)
    {
        currentRoom = room;

        Spirimonz spirimonzInHands = inventoryManager.selectedSpirimonz;
        if(spirimonzInHands != null)
            spirimonzInHands.SetCurrentRoom(room);
    }

    public void UseSlashAnimation()
    {
        handAnimator.SetTrigger("Slash");
    }
    
    public void AlertTheHuntingGhost()
    {
        if (house.currentGhost == null || house.currentGhost.currentState != Ghost.GhostState.huntingState)
        {
            return;
        }

        if (_ghost.currentRoom == currentRoom)
        {
            _ghost.PlayerFound();
        }
        else
        {
            float distance = Vector3.Distance(this.transform.position, house.currentGhost.transform.position);
            if (distance < house.currentGhost.detectPlayerActivityRange)
            {
                house.currentGhost.ForceNewWaypoint(currentRoom);
            }
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
        
        # if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.L))
        {
            LockControls(!IsLocked());
        }
        # endif
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
        inventoryManager.articlesFoundInGame.Clear();
        
        this.Invoke(2, () =>
        {
            LockControls(true);
        });
        
        House.Instance.currentGhost.LockGhost();
        
        //Sounds
        SoundManager soundManager = SoundManager.Instance;
        soundManager.PlaySound(House.Instance.currentGhost.killSound, transform.position, 1f, sourceParent: transform, duration: -1f, loop: false);
        
        this.Invoke(deathSoundDelay, () => PlayDeathSound(soundManager));
        this.Invoke(groundFallDelay, () => PlayerFallGroundSound(soundManager));
        this.Invoke(groundFallDelay, () => StopAmbientSound(soundManager));
        
        //Animation
        handAnimator.SetBool("IsDead", true);
        handAnimator.SetTrigger("Death");
        
        //UI
        UIGame uiGame = UIGame.Instance;
        uiGame.CloseAllWindows();
        uiGame.EnablePointer(false);
        uiGame.EnableOverlay(true, 5);
    }

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
    
    public Vector3 GetForward()
    {
        return camera.transform.forward;
    }
    
    public override void ReceiveArticle(Article article, bool useSound = false)
    {
        base.ReceiveArticle(article, useSound);
        if (useSound)
        {
            interactionController.objectInHands = null;
            handAnimator.SetInteger("HandPos", 1);
        }
    }
}