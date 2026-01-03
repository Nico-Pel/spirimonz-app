using UnityEngine;

public class ClickableObject : MonoBehaviour, IInteractable
{
    public ActivitySource activitySource;

    [Header("Interaction Options")]
    public bool canClick = true;
    public bool canHold = false;
    public bool canRelease = false;

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
}