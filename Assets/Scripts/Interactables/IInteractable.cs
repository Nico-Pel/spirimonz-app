using UnityEngine;

public interface IInteractable
{
    void OnInteractStart();
    void OnInteractHold();
    void OnInteractEnd();
}