using UnityEngine;

public interface IInteractable
{
    Sprite SpecialCursor { get; set; }
    float CursorSize { get; set; }
    
    void OnInteractStart();
    void OnInteractHold();
    void OnInteractEnd();

    bool InteractionLocked { get; set; }
}