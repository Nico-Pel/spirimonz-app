using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CaptureScene : GameBehaviour
{
    public AudioClip victorySound;
    public float victoryVolume = 1f;
    
    public AudioClip loseSound;
    public float loseVolume = 1f;
    
    public AudioClip heartBeatingClip;
    public float heartBeatVolume = 1f;
    
    public GameObject ghostModel;
    public Animator ghostAnimator;
    
    public Animator sceneAnimator;
    public float delayBeforeStartingWinAnimation = 0.5f;

    private Spirimonz _capturedSpirimonz;

    private void OnEnable()
    {
        UIGame.Instance.EnableOverlay(false, 0.5f);
        SoundManager.Instance.StopAmbient(1.5f);
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
        ghostModel.SetActive(false);
        _capturedSpirimonz = Instantiate(House.Instance.GetSpirimonzPrefab(), transform.position, Quaternion.identity);
        _capturedSpirimonz.hidingGameObject.SetActive(false);
        _capturedSpirimonz.Lock();
        this.Invoke(delayBeforeStartingWinAnimation, PlayWinAnimation);
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
        this.Invoke(0.3f, () => UIGame.Instance.EnableOverlay(true, 0.1f));
    }

    public void PlayerFakeHeartBeating()
    {
        SoundManager.Instance.PlaySound(
            heartBeatingClip, transform.position, heartBeatVolume, sourceParent: transform, duration: -1f, loop: false);
    }

    private void PlayVictorySound()
    {
        SoundManager.Instance.PlaySound(
            victorySound, transform.position, victoryVolume, sourceParent: transform, duration: -1f, loop: false);
    }
    
    private void PlayLoseSound()
    {
        SoundManager.Instance.PlaySound(
            loseSound, transform.position, loseVolume, sourceParent: transform, duration: -1f, loop: false);
    }
}