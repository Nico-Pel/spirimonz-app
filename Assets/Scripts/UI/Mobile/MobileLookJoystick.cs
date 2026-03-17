using UnityEngine;
using UnityEngine.EventSystems;

public class MobileLookJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public RectTransform handle;
    public float handleRange = 80f;
    public float deadZone = 0.1f;
    public bool invertY = false;
    public bool floating = true;
    public bool returnToOriginOnRelease = true;

    private RectTransform _rectTransform;
    private Vector2 _startHandlePos;
    private Vector2 _startBasePos;

    private void Awake()
    {
        _rectTransform = transform as RectTransform;
        CacheStartPositions();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        RectTransform parentRect = _rectTransform != null ? _rectTransform.parent as RectTransform : null;
        ProcessPointerDown(eventData.position, eventData.pressEventCamera, parentRect, floating);
    }

    public void OnDrag(PointerEventData eventData)
    {
        ProcessDrag(eventData.position, eventData.pressEventCamera);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        ProcessPointerUp(returnToOriginOnRelease);
    }

    private void OnDisable()
    {
        ProcessPointerUp(returnToOriginOnRelease);
    }

    public void ProcessPointerDown(Vector2 screenPos, Camera cam, RectTransform parentRect, bool allowFloating)
    {
        if (_rectTransform == null)
            return;

        if (allowFloating && floating && parentRect != null)
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPos, cam, out Vector2 localPoint))
            {
                Vector2 anchorPos = GetAnchorLocalPosition(parentRect, _rectTransform.anchorMin);
                _rectTransform.anchoredPosition = localPoint - anchorPos;
            }
        }

        ProcessDrag(screenPos, cam);
    }

    public void ProcessDrag(Vector2 screenPos, Camera cam)
    {
        if (handle == null || _rectTransform == null)
            return;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rectTransform, screenPos, cam, out Vector2 localPoint))
        {
            return;
        }

        Vector2 input = localPoint / handleRange;
        input = Vector2.ClampMagnitude(input, 1f);

        if (input.magnitude < deadZone)
            input = Vector2.zero;

        if (invertY)
            input.y = -input.y;

        handle.anchoredPosition = _startHandlePos + (input * handleRange);
        MobileInput.SetLookAxis(input);
    }

    public void ProcessPointerUp(bool resetBase)
    {
        if (handle != null)
            handle.anchoredPosition = _startHandlePos;
        MobileInput.SetLookAxis(Vector2.zero);

        if (resetBase && returnToOriginOnRelease)
            ResetBasePosition();
    }

    public void ResetBasePosition()
    {
        if (_rectTransform != null)
            _rectTransform.anchoredPosition = _startBasePos;
    }

    public Vector2 GetBasePosition()
    {
        return _rectTransform != null ? _rectTransform.anchoredPosition : Vector2.zero;
    }

    public bool ContainsScreenPoint(Vector2 screenPos, Camera cam)
    {
        if (_rectTransform == null)
            return false;
        return RectTransformUtility.RectangleContainsScreenPoint(_rectTransform, screenPos, cam);
    }

    public void CacheStartPositions()
    {
        if (handle != null)
            _startHandlePos = handle.anchoredPosition;
        if (_rectTransform != null)
            _startBasePos = _rectTransform.anchoredPosition;
    }

    private Vector2 GetAnchorLocalPosition(RectTransform parentRect, Vector2 anchor)
    {
        Rect rect = parentRect.rect;
        return new Vector2(rect.xMin + (rect.width * anchor.x), rect.yMin + (rect.height * anchor.y));
    }
}
