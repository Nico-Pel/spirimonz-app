using UnityEngine;
using UnityEngine.EventSystems;

public class MobileJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public RectTransform handle;
    public float handleRange = 80f;
    public float deadZone = 0.1f;

    private RectTransform _rectTransform;
    private Vector2 _startHandlePos;

    private void Awake()
    {
        _rectTransform = transform as RectTransform;
        if (handle != null)
            _startHandlePos = handle.anchoredPosition;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (handle == null || _rectTransform == null)
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
        {
            return;
        }

        Vector2 input = localPoint / handleRange;
        input = Vector2.ClampMagnitude(input, 1f);

        if (input.magnitude < deadZone)
            input = Vector2.zero;

        handle.anchoredPosition = _startHandlePos + (input * handleRange);
        MobileInput.SetMove(input);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (handle != null)
            handle.anchoredPosition = _startHandlePos;
        MobileInput.SetMove(Vector2.zero);
    }
}
