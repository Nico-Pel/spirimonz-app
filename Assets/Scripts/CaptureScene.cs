using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CaptureScene : GameBehaviour
{
    public AudioClip victorySound;
    public float victoryVolume = 1f;

    public GameObject smokeDarkWinEffect;
    
    public AudioClip loseSound;
    public float loseVolume = 1f;
    
    public AudioClip heartBeatingClip;
    public float heartBeatVolume = 1f;
    
    public GameObject ghostModel;
    public Animator ghostAnimator;
    
    public Animator sceneAnimator;
    public float delayBeforeStartingWinAnimation = 0.5f;

    private GameObject _capturedSpirimonz;

    private void OnEnable()
    {
        UIGame.Instance.EnableOverlay(false, 0.5f);
        SoundManager.Instance.StopAmbient(1.5f);
        
        Camera.main.gameObject.SetActive(false);
    }

    //Animation Event
    public void TriggerSuccessOrNot()
    {
        if (GhostInvestigator.Instance.IsSuccess())
        {
            Win();
        }
        else
        {
            Lose();
        }
    }

    private void Win()
    {
        SpirimonzSettings selectedSpirimonz = House.Instance.GetSpirimonzSettings();
        
        smokeDarkWinEffect.SetActive(true);
        
        ghostModel.SetActive(false);
        _capturedSpirimonz = Instantiate(selectedSpirimonz.spirimonzBodyPrefab, transform.position + selectedSpirimonz.bodyPresentationOffset, Quaternion.identity);
        this.Invoke(delayBeforeStartingWinAnimation, PlayWinAnimation);
        
        UnlockSpirimonz(selectedSpirimonz.spirimonzID);
        
        this.Invoke(8, () => Exit(false));
    }

    private void UnlockSpirimonz(string spirimonzID)
    {
        GameManager gameManager = GameManager.Instance;
        if (gameManager.IsSpirimonzCaptured(spirimonzID) == false)
        {
            gameManager.UnlockSpirimonz(spirimonzID);
        }
    }

    private void PlayWinAnimation()
    {
        sceneAnimator.SetTrigger("Win");
        PlayVictorySound();
    }

    private void Lose()
    {
        ghostAnimator.SetTrigger("Attack");
        PlayLoseSound();
        this.Invoke(0.275f, () => UIGame.Instance.EnableOverlay(true, 0.1f));

        this.Invoke(3, () => Exit(true));
    }

    public void PlayerFakeHeartBeating()
    {
        SoundManager.Instance.PlaySound(
            heartBeatingClip, transform.position, heartBeatVolume, sourceParent: transform, duration: -1f, loop: false, ignoreAudioOcclusion: true);
    }

    private void PlayVictorySound()
    {
        SoundManager.Instance.PlaySound(
            victorySound, transform.position, victoryVolume, sourceParent: transform, duration: -1f, loop: false, pitch:1f, ignoreAudioOcclusion: true);
    }
    
    private void PlayLoseSound()
    {
        SoundManager.Instance.PlaySound(
            loseSound, transform.position, loseVolume, sourceParent: transform, duration: -1f, loop: false, ignoreAudioOcclusion: true);
    }

    private void Exit(bool isDead)
    {
        House.Instance.houseEntry.Entry(Player.Instance, isDead);
    }
}