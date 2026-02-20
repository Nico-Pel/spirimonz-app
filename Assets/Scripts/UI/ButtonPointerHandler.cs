using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class ButtonPointerHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    
    public UnityEvent onPointerDown;
    public UnityEvent onPointerUp;
    
    public void OnPointerDown(PointerEventData eventData)
    {
        onPointerDown?.Invoke();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        onPointerUp?.Invoke();
    }
}