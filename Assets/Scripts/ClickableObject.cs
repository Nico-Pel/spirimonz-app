using UnityEngine;

[RequireComponent(typeof(ActivitySource))]
public class ClickableObject : MonoBehaviour
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
            activitySource = GetComponent<ActivitySource>();
        }
    }
    
    public virtual void Initialize(House h)
    {
        house = h;
    }

    /// <summary>Quand l'objet est cliqué</summary>
    public virtual void OnClick()
    {
        Debug.Log($"{name} clicked!");
    }

    /// <summary>Quand l'objet est maintenu</summary>
    public virtual void OnHold()
    {
        Debug.Log($"{name} held!");
    }

    /// <summary>Quand le clic est relâché</summary>
    public virtual void OnRelease()
    {
        Debug.Log($"{name} released!");
    }
}