using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

public class ClickableObject : GameBehaviour, IInteractable
{
    public ActivitySource activitySource;
    [Header("Prints")]
    public PrintSource[] printSources;

    [Header("Interaction Options")]
    public bool canClick = true;
    public bool canHold = false;
    public bool canRelease = false;
    public bool canBeClickedOnTriggerByGhostDuringHunt;
    public bool ignoreActivitySource;

    public House house { get; set; }

    private float _securityClickTime = 0.15f;
    private bool _clickSecurityLocked = false;
    private int _lastHoldFrame = -1;

    public UnityEvent onClick;
    
    protected virtual void Awake()
    {
        if (activitySource == null && !ignoreActivitySource)
        {
            Debug.LogError(
                $"{nameof(ActivitySource)} introuvable sur {name}",
                this
            );
        }

        if (canClick == false && canHold == false)
            LockInteraction(true);
    }

    public virtual void Initialize(House h)
    {
        house = h;
    }

    // =========================
    // IInteractable
    // =========================

    public Sprite SpecialCursor { get; set; }
    public float CursorSize { get; set; }

    public void OnInteractStart()
    {
        if (_clickSecurityLocked) return;

        _clickSecurityLocked = true;
        this.Invoke(_securityClickTime, () => _clickSecurityLocked = false);
            
        if (canClick)
            OnClick();

        if (!canClick && canHold)
        {
            OnHold();
            _lastHoldFrame = Time.frameCount;
        }
    }

    public void OnInteractHold()
    {
        if (canHold)
        {
            if (_lastHoldFrame == Time.frameCount)
                return;
            OnHold();
        }
    }

    public void OnInteractEnd()
    {
        if (canRelease)
            OnRelease();
    }

    public bool InteractionLocked { get; set; }

    // =========================
    // Existing logic (inchangé)
    // =========================

    public virtual void OnClick()
    {
        onClick?.Invoke();
    }

    public virtual void OnHold()
    {
    }

    public virtual void OnRelease()
    {
    }

    public virtual PrintSource GetRandomPrintSource()
    {
        if (printSources == null || printSources.Length == 0) return null;
        
        List<PrintSource> possiblePrintSources = new List<PrintSource>();
        foreach (PrintSource printSource in printSources)
        {
            if (printSource != null && printSource.IsActivated() == false)
                possiblePrintSources.Add(printSource);
        }
        
        if (possiblePrintSources.Count == 0) return null;
        return possiblePrintSources[Random.Range(0, possiblePrintSources.Count)];
    }

    public void LockInteraction(bool enable)
    {
        InteractionLocked = enable;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Ghost ghost) && ghost.currentState == Ghost.GhostState.huntingState)
        {
            GhostClickedDuringAHunt();
        }
    }

    protected virtual void GhostClickedDuringAHunt()
    {
        
    }
}
