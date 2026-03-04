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

    public bool ignoreFruits = true;
    public bool ignoreCandles = true;

    [Header("Prop Eater Components")] 
    public Transform eatPos;
    public SphereCollider sphereCollider;

    [Header("Audio settings")] 
    public SoundParameters eatSoundParameters;
    
    private bool _isAskingItem;
    private bool _playerIsHoldingObject;

    private List<CatchableObject> _eatableObjects = new List<CatchableObject>();
    private InteractionController _interactionController;

    private bool _askingForItem;
    private GamePlayer _player;
    
    protected override void Start()
    {
        base.Start();
        _player = (GamePlayer)Player.Instance;
        _interactionController = _player.interactionController;
        
        _interactionController.OnGrabItem.AddListener(PlayerGrabbedObject);
        _interactionController.OnDropItem.AddListener(PlayerDroppedObject);
    }

    protected virtual void PlayerGrabbedObject(CatchableObject grabbedObject)
    {
        if ((ignoreFruits && grabbedObject is Fruit) || (ignoreCandles && grabbedObject is CatchableFireObject))
        {
            _playerIsHoldingObject = false;
        }
        else
        {
            _playerIsHoldingObject = true;
        }
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

        bool shouldAskFruit = _isAskingItem ? dist < stopAskingRange : dist < askingRange;
        if (_playerIsHoldingObject == false) shouldAskFruit = false;

        if (shouldAskFruit && _isAskingItem == false)
        {
            animator.SetTrigger("AskItem");
        }
        
        _isAskingItem = shouldAskFruit;
        animator.SetBool("AskingItem", _isAskingItem);
        sphereCollider.enabled = _isAskingItem;
        
        return true;
    }

    private void ResetPlayerHoldingObject()
    {
        _playerIsHoldingObject = false;
    }

    protected override void UpdateMovementBehaviour()
    {
        if (_isAskingItem)
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
        if (ignoreFruits && catchableObject is Fruit) return;
        
        if (ignoreCandles && catchableObject is CatchableFireObject) return;
        
        if (_eatableObjects.Contains(catchableObject))
        {
            EatObject(catchableObject);
        }
    }

    private void EatObject(CatchableObject catchableObject)
    {
        PlayEatSound();

        animator.SetTrigger("Eat");
        
        catchableObject.canBeGrabByPlayer = false;
        catchableObject.canBeThrownByGhost = false;
        
        _eatableObjects.Remove(catchableObject);
        catchableObject.transform.DOMove(eatPos.position, 0.5f);
        catchableObject.transform.DOScale(Vector3.one * 0.01f, 0.5f).OnComplete(() =>
        {
            SwallowObject(catchableObject);
        });
    }

    protected virtual void SwallowObject(CatchableObject catchableObject)
    {
        
    }

    
    public override bool GoBackToHands(Transform handPos)
    {
        if (!base.GoBackToHands(handPos)) return false;

        _eatableObjects.Clear();
        
        return true;
    }

    private void PlayEatSound()
    {
        if(eatSoundParameters != null)
            eatSoundParameters.PlaySound(transform.position);
    }
}
