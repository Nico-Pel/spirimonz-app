using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class HouseEntry : GameBehaviour
{
    [FormerlySerializedAs("houseSceneName")] public string sceneName;
    public int houseID = -1;
    public Animator animator;
    public float fadeDuration = 3f;
    
    [Header("Audio")]
    public AudioClip entrySound;
    public float volume = 1f;
    public float soundDelay = 0.33f;

    public MeshRenderer debugRender;

    private bool hasEntered = false;

    private void Awake()
    {
        if(debugRender != null)
            debugRender.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasEntered)
            return;

        Player player = other.GetComponentInParent<Player>();
        if (player)
        {
            hasEntered = true;
            Entry(player);
        }
    }

    private void Entry(Player player)
    {
        player.LockControls(true);
        
        UIWorld.Instance?.EnableOverlay(true, fadeDuration);
        UIGame.Instance?.EnableOverlay(true, fadeDuration);
        
        GameManager.Instance?.SetCurrentHouseID(houseID);
        
        if(animator != null)
            animator.SetTrigger("Open");
        
        this.Invoke(soundDelay, PlayerEntrySound);

        this.Invoke(fadeDuration, () =>
        {
            LoadHouseScene();
        });
    }

    private void LoadHouseScene()
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("HouseEntry: House scene is not assigned or not in Build Settings.");
            return;
        }

        GameManager.Instance.LoadScene(sceneName);
    }

    private void PlayerEntrySound()
    {
        if (entrySound == null) return;
        
        SoundManager.Instance?.PlaySound(entrySound, transform.position, volume);
    }
}