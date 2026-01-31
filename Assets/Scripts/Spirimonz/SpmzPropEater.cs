using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Random = UnityEngine.Random;

public class SpmzPropEater : Spirimonz
{
    [Header("Prop Eater settings")] 
    public float askingRange = 6f;
    public float stopAskingRange = 8f;
    public float timeBeforeIgnoringDroppedObject = 1f;

    [Header("Prop Eater Components")] 
    public Transform eatPos;
    public SphereCollider sphereCollider;

    [Header("Fruit settings")]
    public Fruit fruitPrefab;
    public Transform spawnFruitPos;
    public float giveFruitForceForward = 5f;
    public float giveFruitForceUp = 1f;
    public float percentageChancesToGiveFruit = 10f;
    public float percentageChancesUpOnFail = 15f;
    public float delayBeforeGivingFruit = 0.25f;

    [Header("Audio settings")] 
    public AudioClip eatSound;
    public float eatSoundVolume = 0.5f;
    public float pitchSoundMin = 0.3f;
    public float pitchSoundMax = 0.6f;
    
    private float _basePercentageChancesToGiveFruit;
    
    private bool _isAskingFruit;
    private bool _playerIsHoldingObject;

    private List<CatchableObject> _eatableObjects = new List<CatchableObject>();
    private InteractionController _interactionController;

    private bool _askingForFruit;
    protected override void Start()
    {
        base.Start();
        _interactionController = Player.Instance.interactionController;
        
        _interactionController.OnGrabItem.AddListener(PlayerGrabbedObject);
        _interactionController.OnDropItem.AddListener(PlayerDroppedObject);

        _basePercentageChancesToGiveFruit = percentageChancesToGiveFruit;
    }

    private void PlayerGrabbedObject(CatchableObject grabbedObject)
    {
        _playerIsHoldingObject = grabbedObject is not Fruit;
        CancelInvoke(nameof(ResetPlayerHoldingObject));
    }
    
    private void PlayerDroppedObject(CatchableObject grabbedObject)
    {
        if (isOnTheMap)
        {
            _eatableObjects.Add(grabbedObject);

            this.Invoke(timeBeforeIgnoringDroppedObject, () =>
            {
                if (grabbedObject != null && _eatableObjects.Contains(grabbedObject))
                {
                    _eatableObjects.Remove(grabbedObject);
                }
            });
        }
        
        Invoke(nameof(ResetPlayerHoldingObject), timeBeforeIgnoringDroppedObject);
    }

    public override bool UpdateSpirimonzBehaviour()
    {
        if (!base.UpdateSpirimonzBehaviour()) return false;

        float dist = Vector3.Distance(_interactionController.transform.position, transform.position);

        bool shouldAskFruit = _isAskingFruit ? dist < stopAskingRange : dist < askingRange;
        if (_playerIsHoldingObject == false) shouldAskFruit = false;

        if (shouldAskFruit && _isAskingFruit == false)
        {
            animator.SetTrigger("AskFruit");
        }
        
        _isAskingFruit = shouldAskFruit;
        animator.SetBool("AskingFruit", _isAskingFruit);
        sphereCollider.enabled = _isAskingFruit;
        
        return true;
    }

    private void ResetPlayerHoldingObject()
    {
        _playerIsHoldingObject = false;
    }

    protected override void UpdateMovementBehaviour()
    {
        if (_isAskingFruit)
        {
            agent.speed = 0;
            LookAtPlayer();
            return;
        }
        
        base.UpdateMovementBehaviour();
    }

    protected override void OnColliderTriggeredEnter(Collider other)
    {
        if (isOnTheMap == false || hidingGameObject.activeSelf) return;

        if(other.TryGetComponent(out CatchableObject catchableObject))
            TryToEatItem(catchableObject);
    }
    
    protected override void OnColliderTriggeredExit(Collider other)
    {
        if (isOnTheMap == false || hidingGameObject.activeSelf) return;

        if(other.TryGetComponent(out CatchableObject catchableObject))
            TryToEatItem(catchableObject);
    }

    private void TryToEatItem(CatchableObject catchableObject)
    {
        if (catchableObject is Fruit) return;
        
        if (_eatableObjects.Contains(catchableObject))
        {
            EatObject(catchableObject);
        }
    }

    private void EatObject(CatchableObject catchableObject)
    {
        PlayEatSound();
        
        _eatableObjects.Remove(catchableObject);
        catchableObject.transform.DOMove(eatPos.position, 0.5f);
        catchableObject.transform.DOScale(Vector3.one * 0.01f, 0.5f).OnComplete(() =>
        {
            TryToGiveFruit();
            Destroy(catchableObject);
        });
    }

    private void TryToGiveFruit()
    {
        float roll = Random.Range(0f, 100f);
        if (roll <= percentageChancesToGiveFruit)
        {
            GiveFruit();
        }
        else
        {
            FailToGiveFruit();
        }
    }

    private void FailToGiveFruit()
    {
        percentageChancesToGiveFruit += percentageChancesUpOnFail;
    }

    private void GiveFruit()
    {
        percentageChancesToGiveFruit = _basePercentageChancesToGiveFruit;
        animator.SetTrigger("DropFruit");
        this.Invoke(delayBeforeGivingFruit, () =>
        {
            Fruit newFruit = Instantiate(fruitPrefab, spawnFruitPos.position, Quaternion.identity);
            newFruit.transform.DOScale(Vector3.one, 0.5f).From(0);
            newFruit.rb.isKinematic = false;
            newFruit.rb.AddForce(transform.forward * giveFruitForceForward + Vector3.up * giveFruitForceUp);
        });
    }

    public override bool GoBackToHands(Transform handPos)
    {
        if (!base.GoBackToHands(handPos)) return false;

        _eatableObjects.Clear();
        
        return true;
    }

    private void PlayEatSound()
    {
        if(eatSound != null)
            SoundManager.Instance?.PlaySound(eatSound, transform.position, volume: eatSoundVolume, pitch: Random.Range(pitchSoundMin, pitchSoundMax));
    }
}
