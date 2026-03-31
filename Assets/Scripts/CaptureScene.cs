using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class CaptureScene : GameBehaviour
{
    public Article victoryArticle;
    public Article alreadyCapturedArticle;

    [Space]
    
    public AudioClip victorySound;
    public float victoryVolume = 1f;

    public GameObject smokeDarkWinEffect;
    
    public AudioClip loseSound;
    public float loseVolume = 1f;
    
    public AudioClip heartBeatingClip;
    public float heartBeatVolume = 1f;
    
    private Animator _ghostAnimator;
    
    public Animator sceneAnimator;
    public float delayBeforeStartingWinAnimation = 0.5f;

    private GameObject _capturedSpirimonz;
    private House _house;
    
    private GameObject _ghost;
    private void OnEnable()
    {
        UIGame.Instance.EnableOverlay(false, 0.5f);
        SoundManager.Instance.StopAmbient(1.5f);
        
        Camera.main.gameObject.SetActive(false);

        if (_house == null)
        {
            _house = House.Instance;
        }
        
        if (_ghost == null)
        {
            _ghost = Instantiate(_house.currentGhost.ghostModel, transform.position, quaternion.identity, transform);
            _ghost.SetActive(true);
            _ghost.GetComponentInChildren<Renderer>().enabled = true;
            _ghostAnimator = _ghost.GetComponentInChildren<Animator>();
        }
    }

    //Animation Event
    public void TriggerSuccessOrNot()
    {
        foreach (Quest quest in _house.map.quests)
        {
            if(quest.type == Quest.QuestType.TryToCapture)
                GameManager.Instance.UpdateQuestProgress(quest, _house.map.houseID, 1);
        }
        
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
        SpirimonzSettings selectedSpirimonz = _house.GetSpirimonzSettings();
        TutorialManager tutorial = TutorialManager.Instance;
        if (tutorial != null && tutorial.IsTraining && tutorial.forcedCapturedSpirimonz != null)
            selectedSpirimonz = tutorial.forcedCapturedSpirimonz;
        
        _house.currentPlayer.ReceiveArticle(victoryArticle);
        
        smokeDarkWinEffect.SetActive(true);
        
        _ghost.SetActive(false);
        _capturedSpirimonz = Instantiate(selectedSpirimonz.spirimonzBodyPrefab, transform.position + selectedSpirimonz.bodyPresentationOffset, Quaternion.identity);
        this.Invoke(delayBeforeStartingWinAnimation, PlayWinAnimation);
        
        UnlockSpirimonz(selectedSpirimonz.spirimonzID);
        
        this.Invoke(8, () => Exit(false));
        
        foreach (Quest quest in _house.map.quests)
        {
            if(quest.type == Quest.QuestType.Capture)
                GameManager.Instance.UpdateQuestProgress(quest, _house.map.houseID, 1);
        }
    }

    private void UnlockSpirimonz(string spirimonzID)
    {
        GameManager gameManager = GameManager.Instance;
        if (gameManager.IsSpirimonzCaptured(spirimonzID) == false)
        {
            gameManager.UnlockSpirimonz(spirimonzID);
        }
        else
        {
            _house.currentPlayer.ReceiveArticle(alreadyCapturedArticle);
        }
    }

    private void PlayWinAnimation()
    {
        sceneAnimator.SetTrigger("Win");
        PlayVictorySound();
    }

    private void Lose()
    {
        _ghostAnimator.SetTrigger("Attack");
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
        if(isDead)
            _house.currentPlayer.inventoryManager.articlesFoundInGame.Clear();
        
        UIEndGame.EndTypes endType = isDead ? UIEndGame.EndTypes.Lose : UIEndGame.EndTypes.Win;
        UIGame.Instance.OpenEndGame(endType, _house);
        
        if(isDead)
            _ghost.SetActive(false);
    }
}
