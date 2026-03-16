using UnityEngine;
using UnityEngine.EventSystems;

public class MobileLookArea : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public float sensitivity = 1f;
    public bool invertY = true;

    private int _pointerId = -1;
    private Vector2 _lastPos;

    public void OnPointerDown(PointerEventData eventData)
    {
        _pointerId = eventData.pointerId;
        _lastPos = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (eventData.pointerId != _pointerId)
            return;

        Vector2 delta = eventData.position - _lastPos;
        _lastPos = eventData.position;

        if (invertY)
            delta.y = -delta.y;

        // Normalise pour être indépendant de la résolution.
        Vector2 normalized = new Vector2(
            delta.x / Mathf.Max(Screen.width, 1),
            delta.y / Mathf.Max(Screen.height, 1)
        );

        MobileInput.AddLookDelta(normalized * sensitivity);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.pointerId != _pointerId)
            return;

        _pointerId = -1;
        _lastPos = Vector2.zero;
    }

    private void OnDisable()
    {
        _pointerId = -1;
        _lastPos = Vector2.zero;
    }
}
