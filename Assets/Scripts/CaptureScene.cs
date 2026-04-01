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

        EvaluateCaptureQuests();
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
        float fadeDelay = 0.275f;
        if (_house != null && _house.currentGhost != null)
            fadeDelay = Mathf.Max(0f, _house.currentGhost.captureLoseFadeDelay);
        this.Invoke(fadeDelay, () => UIGame.Instance.EnableOverlay(true, 0.1f));

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

    private void EvaluateCaptureQuests()
    {
        if (_house == null || _house.map == null || _house.map.quests == null)
            return;

        List<SpirimonzSettings> team = GetTeamSettings();
        int teamCount = team.Count;
        Ghost ghost = _house.currentGhost;
        GhostParameters ghostParams = ghost != null ? ghost.ghostParameters : null;
        GhostTypeData.GhostType ghostType = ghostParams != null && ghostParams.ghostTypeData != null
            ? ghostParams.ghostTypeData.ghostType
            : GhostTypeData.GhostType.DEBUG;
        float elapsedMinutes = _house.GetElapsedMinutes();
        float seenDuration = ghost != null ? ghost.playerSeenDuration : 0f;

        foreach (Quest quest in _house.map.quests)
        {
            if (quest == null)
                continue;

            switch (quest.type)
            {
                case Quest.QuestType.Capture:
                    GameManager.Instance.UpdateQuestProgress(quest, _house.map.houseID, 1);
                    break;
                case Quest.QuestType.CaptureWithMinUsefulForEvidence:
                {
                    int needed = Mathf.Max(1, quest.minUsefulCount);
                    if (teamCount > 0 && CountUseful(team, quest.evidenceType) >= needed)
                        GameManager.Instance.UpdateQuestProgress(quest, _house.map.houseID, 1);
                    break;
                }
                case Quest.QuestType.CaptureWithOnlyUsefulForEvidence:
                    if (teamCount > 0 && AllUseful(team, quest.evidenceType))
                        GameManager.Instance.UpdateQuestProgress(quest, _house.map.houseID, 1);
                    break;
                case Quest.QuestType.CaptureWithoutUsefulForEvidence:
                    if (teamCount > 0 && NoneUseful(team, quest.evidenceType))
                        GameManager.Instance.UpdateQuestProgress(quest, _house.map.houseID, 1);
                    break;
                case Quest.QuestType.CaptureWithoutUsefulForEvidenceDouble:
                {
                    GhostInvestigator.EvidenceType a = quest.evidenceType;
                    GhostInvestigator.EvidenceType b = quest.useSecondEvidenceType ? quest.evidenceTypeB : quest.evidenceType;
                    if (teamCount > 0 && NoneUseful(team, a) && NoneUseful(team, b))
                        GameManager.Instance.UpdateQuestProgress(quest, _house.map.houseID, 1);
                    break;
                }
                case Quest.QuestType.CaptureWithMaxTeamCount:
                    if (teamCount > 0 && teamCount <= Mathf.Max(1, quest.maxTeamCount))
                        GameManager.Instance.UpdateQuestProgress(quest, _house.map.houseID, 1);
                    break;
                case Quest.QuestType.CaptureWithSingleSpirimonz:
                    if (teamCount == 1)
                        GameManager.Instance.UpdateQuestProgress(quest, _house.map.houseID, 1);
                    break;
                case Quest.QuestType.CaptureAfterSeenByGhost:
                    if (teamCount > 0 && seenDuration >= Mathf.Max(0f, quest.minSeenSeconds))
                        GameManager.Instance.UpdateQuestProgress(quest, _house.map.houseID, 1);
                    break;
                case Quest.QuestType.CaptureUnderTime:
                    if (teamCount > 0 && elapsedMinutes <= Mathf.Max(0.01f, quest.maxMinutes))
                        GameManager.Instance.UpdateQuestProgress(quest, _house.map.houseID, 1);
                    break;
                case Quest.QuestType.CaptureGhostType:
                {
                    bool match = ghostType == quest.ghostType;
                    if (!match && quest.useSecondGhostType)
                        match = ghostType == quest.ghostTypeB;
                    if (match)
                        GameManager.Instance.UpdateQuestProgress(quest, _house.map.houseID, 1);
                    break;
                }
                case Quest.QuestType.CaptureWithNoDroppableSpirimonz:
                    if (teamCount > 0 && AllDroppable(team, false))
                        GameManager.Instance.UpdateQuestProgress(quest, _house.map.houseID, 1);
                    break;
                case Quest.QuestType.CaptureWithOnlyDroppableSpirimonz:
                    if (teamCount > 0 && AllDroppable(team, true))
                        GameManager.Instance.UpdateQuestProgress(quest, _house.map.houseID, 1);
                    break;
            }
        }
    }

    private List<SpirimonzSettings> GetTeamSettings()
    {
        List<SpirimonzSettings> results = new List<SpirimonzSettings>();
        InventoryManager inventory = InventoryManager.Instance;
        if (inventory == null || inventory.spirimonzTeamSettings == null)
            return results;

        foreach (SpirimonzSettings s in inventory.spirimonzTeamSettings)
        {
            if (s != null)
                results.Add(s);
        }

        return results;
    }

    private int CountUseful(List<SpirimonzSettings> team, GhostInvestigator.EvidenceType evidence)
    {
        int count = 0;
        for (int i = 0; i < team.Count; i++)
        {
            if (team[i] != null && team[i].IsUsefulForEvidence(evidence))
                count++;
        }
        return count;
    }

    private bool AllUseful(List<SpirimonzSettings> team, GhostInvestigator.EvidenceType evidence)
    {
        if (team.Count == 0)
            return false;

        for (int i = 0; i < team.Count; i++)
        {
            if (team[i] == null || !team[i].IsUsefulForEvidence(evidence))
                return false;
        }

        return true;
    }

    private bool NoneUseful(List<SpirimonzSettings> team, GhostInvestigator.EvidenceType evidence)
    {
        for (int i = 0; i < team.Count; i++)
        {
            if (team[i] != null && team[i].IsUsefulForEvidence(evidence))
                return false;
        }

        return true;
    }

    private bool AllDroppable(List<SpirimonzSettings> team, bool value)
    {
        if (team.Count == 0)
            return false;

        for (int i = 0; i < team.Count; i++)
        {
            if (team[i] == null || team[i].canBeDroppedOnMap != value)
                return false;
        }

        return true;
    }
}
