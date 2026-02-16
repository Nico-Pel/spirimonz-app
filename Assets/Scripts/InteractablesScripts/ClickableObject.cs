using System;
using UnityEngine;
using UnityEngine.Events;

public class ClickableObject : GameBehaviour, IInteractable
{
    public ActivitySource activitySource;

    [Header("Interaction Options")]
    public bool canClick = true;
    public bool canHold = false;
    public bool canRelease = false;
    public bool canBeClickedOnTriggerByGhostDuringHunt;

    public House house { get; set; }

    private void Awake()
    {
        if (activitySource == null)
        {
            Debug.LogError(
                $"{nameof(ActivitySource)} introuvable sur {name}",
                this
            );
        }
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
        if (canClick)
            OnClick();
    }

    public void OnInteractHold()
    {
        if (canHold)
            OnHold();
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
        Debug.Log($"{name} clicked!");
    }

    public virtual void OnHold()
    {
        Debug.Log($"{name} held!");
    }

    public virtual void OnRelease()
    {
        Debug.Log($"{name} released!");
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