using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class HouseEntry : GameBehaviour
{
    [FormerlySerializedAs("houseSceneName")] public string sceneName;
    public int houseID = -1;
    public bool isExit;
    public FakeInteractable doorInteractable;
    public Animator animator;
    public float fadeDuration = 3f;
    
    [Header("Locked Cursor")]
    public Sprite lockCursor;
    public float lockedCursorSize = 2.5f;
    
    [Header("Audio")]
    public AudioClip openSound;
    public AudioClip closeSound;
    public float volume = 1f;
    public float soundDelay = 0.33f;

    public MeshRenderer debugRender;

    private bool hasEntered = false;
    private GameManager _gameManager;
    
    private float _securityTime = 1f;
    private bool _canBeTriggered;

    private void Awake()
    {
        if(debugRender != null)
            debugRender.enabled = false;

        if (doorInteractable != null)
            doorInteractable.InteractionLocked = true;
    }

    private void Start()
    {
        _gameManager = GameManager.Instance;
        
        if (isExit)
        {
            Ghost currentGhost = House.Instance.currentGhost;
            currentGhost.onGhostStartToHunt.AddListener(LockDoor);
            currentGhost.onGhostStopToHunt.AddListener(UnlockDoor);
        }
        
        this.Invoke(_securityTime, () => _canBeTriggered = true);
    }

    private void LockDoor()
    {
        animator.SetTrigger("Close");
        PlayerSound(closeSound);
        
        if (doorInteractable != null)
        {
            doorInteractable.SetCursor(lockCursor, lockedCursorSize);
            doorInteractable.InteractionLocked = false;
        }
    }
    
    private void UnlockDoor()
    {
        animator.SetTrigger("Open");
        PlayerSound(openSound);

        if (doorInteractable != null)
        {
            doorInteractable.SetCursor(null);
            doorInteractable.InteractionLocked = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasEntered || _canBeTriggered == false)
            return;

        Player player = other.GetComponentInParent<Player>();
        if (player)
        {
            hasEntered = true;
            Entry(player);
        }
    }

    public void Entry(Player player, bool useDeadAnimation = false)
    {
        player.LockControls(true);
        
        UIGame.Instance?.EnableOverlay(true, fadeDuration);
        
        if(_gameManager != null && !isExit)
            _gameManager.SetCurrentHouseID(houseID);
        
        string animationToUse = isExit ? "Close" : "Open";
        if(animator != null)
            animator.SetTrigger(animationToUse);
        
        AudioClip soundToUse = isExit ? closeSound : openSound;
        this.Invoke(soundDelay, () => PlayerSound(soundToUse));

        this.Invoke(fadeDuration, () =>
        {
            LoadHouseScene(useDeadAnimation);
        });
    }

    private void LoadHouseScene(bool useDeadAnimation)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("HouseEntry: House scene is not assigned or not in Build Settings.");
            return;
        }

        if (_gameManager != null)
        {
            _gameManager.UseDeadAnimation();
            _gameManager.LoadScene(sceneName, isExit);
        }
        else
        {
            //Prevent to be hard locked on Editor test
            SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        }
    }

    private void PlayerSound(AudioClip clip)
    {
        if (clip == null) return;
        
        SoundManager.Instance?.PlaySound(clip, transform.position, volume);
    }
}