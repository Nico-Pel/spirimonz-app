using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;
using DG.Tweening;

public class SpmzItemHider : Spirimonz
{
    [Header("Item Hiding Settings")]
    public GameObject itemPrefab;
    public float hideItemDelay = 3f;
    public float[] hotAndColdStatesRanges;

    [Header("Sounds")]
    public SoundParameters invokeItemSound;
    public float invokeItemSoundDelay = 0f;
    public SoundParameters[] rangeFeedbackSoundsByState;
    public SoundParameters tooFarFeedbackSound;
    
    private bool _canInteractBase;
    private bool _canBeTakenBackIntoHandsBase;
    private float _lookAtSpeedBase;

    private bool _itemFound;
    private GameObject _spawnedItem;
    private GamePlayer _gamePlayer;
    
    private void Awake()
    {
        _canInteractBase = canInteract;
        _canBeTakenBackIntoHandsBase = canBeTakenBackIntoHands;
        _lookAtSpeedBase = lookAtSpeed;
    }

    protected override void Start()
    {
        base.Start();
        _gamePlayer = _house.currentPlayer as GamePlayer;

        if (_gamePlayer != null)
        {
            _gamePlayer.interactionController.OnGrabItem.AddListener(CheckItemInPlayerHands);
        }
    }

    private void CheckItemInPlayerHands(CatchableObject catchableObject)
    {
        if (_itemFound == false && _spawnedItem != null && catchableObject.gameObject == _spawnedItem)
        {
            _itemFound = true;
        }
    }
    
    public override void DroppingOnMap()
    {
        base.DroppingOnMap();
        StartInvokeItem();
    }

    protected override void OnHuntEnd()
    {
        base.OnHuntEnd();

        if (_spawnedItem == false)
        {
            StartInvokeItem();
        }else if (canInteract == false)
        {
            canInteract = true;
        }
    }

    private void StartInvokeItem()
    {
        canInteract = false;
        canBeTakenBackIntoHands = false;
        lookAtSpeed = 0;
        
        animator.SetTrigger("InvokeItem");
        if (invokeItemSound != null)
        {
            if (invokeItemSoundDelay > 0f)
                this.Invoke(invokeItemSoundDelay, () => invokeItemSound.PlaySound(transform.position));
            else
                invokeItemSound.PlaySound(transform.position);
        }
        this.Invoke(hideItemDelay, SpawnItem);

        _currentBehaviour = SpirimonzBehaviourState.Wait;
    }

    private void SpawnItem()
    {
        Vector3 itemPos = ChoseItemPos();
        
        _spawnedItem = Instantiate(itemPrefab, itemPos + Vector3.up * 0.1f, Quaternion.identity);
        Vector3 baseScale = _spawnedItem.transform.localScale;
        _spawnedItem.transform.DOScale(baseScale, 1).From(0.1f).SetEase(Ease.OutBack);

        canInteract = _canInteractBase;
        canBeTakenBackIntoHands = _canBeTakenBackIntoHandsBase;
        lookAtSpeed = _lookAtSpeedBase;

        _currentBehaviour = baseBehaviour;
    }

    private Vector3 ChoseItemPos()
    {
        List<Vector3> possiblePosition = new List<Vector3>();

        List<ArticleObject> allArticleObjects = new List<ArticleObject>(
            FindObjectsByType<ArticleObject>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            )
        );
        
        var inactiveArticles = allArticleObjects
            .Where(a => !a.gameObject.activeInHierarchy)
            .ToList();

        foreach (ArticleObject articleObject in inactiveArticles)
        {
            possiblePosition.Add(articleObject.transform.position);
        }

        return possiblePosition[Random.Range(0, possiblePosition.Count)];
    }

    public override void InteractionStarted()
    {
        if (_itemFound || _spawnedItem == null)
        {
            SwitchBehaviour();
        }
        else
        {
            float distFromItem = -1;
            int tries = 0;
            while (distFromItem == -1 || tries > 5)
            {
                distFromItem = this.PathDistance(_gamePlayer.transform.position, _spawnedItem.transform.position, tries + 0.1f);
                if (distFromItem == -1)
                {
                    tries += 1;
                }
            }

            if (distFromItem == -1)
            {
                _itemFound = true;
                return;
            }

            int rangeState = hotAndColdStatesRanges.Length; // Coldest by default

            for (int i = 0; i < hotAndColdStatesRanges.Length; i++)
            {
                if (distFromItem <= hotAndColdStatesRanges[i])
                {
                    rangeState = i;
                    break;
                }
            }

            animator.SetInteger("RangeFeedbackState", rangeState);
            animator.SetTrigger("RangeFeedback");
            PlayRangeFeedbackSound(rangeState);
        }
    }

    private void PlayRangeFeedbackSound(int rangeState)
    {
        if (rangeState >= hotAndColdStatesRanges.Length)
        {
            tooFarFeedbackSound?.PlaySound(transform.position);
            return;
        }

        if (rangeFeedbackSoundsByState == null || rangeState < 0 || rangeState >= rangeFeedbackSoundsByState.Length)
            return;

        rangeFeedbackSoundsByState[rangeState]?.PlaySound(transform.position);
    }
}
