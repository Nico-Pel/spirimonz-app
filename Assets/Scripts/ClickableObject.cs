using UnityEngine;

public class ClickableObject : MonoBehaviour
{
    [Header("Interaction Options")]
    public bool canClick = true;
    public bool canHold = false;
    public bool canRelease = false;

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