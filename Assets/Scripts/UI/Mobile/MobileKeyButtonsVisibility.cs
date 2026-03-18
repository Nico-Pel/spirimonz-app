using UnityEngine;

public class MobileKeyButtonsVisibility : MonoBehaviour
{
    public GameObject prevButton;
    public GameObject nextButton;
    public GameObject yButton;

    private bool _initialized;
    private bool _lastIsWorld;
    private RectTransform _prevRect;
    private RectTransform _nextRect;
    private RectTransform _yRect;
    private Vector2 _prevBasePos;
    private Vector2 _nextBasePos;
    private Vector2 _yBasePos;

    private void Update()
    {
        if (GameManager.Instance == null)
            return;

        bool isWorld = GameManager.Instance.IsWorld();
        if (_initialized && isWorld == _lastIsWorld)
            return;

        _initialized = true;
        _lastIsWorld = isWorld;

        CacheRects();

        if (isWorld)
        {
            // World (TPS) -> hide Prev/Nxt, show Y
            SetActive(prevButton, false);
            SetActive(nextButton, false);
            SetActive(yButton, true);
            RestorePositions();
        }
        else
        {
            // House (FPS) -> show Prev/Nxt, hide Y and shift up
            SetActive(prevButton, true);
            SetActive(nextButton, true);
            SetActive(yButton, false);
            ShiftPrevNextUp();
        }
    }

    private void SetActive(GameObject go, bool active)
    {
        if (go != null && go.activeSelf != active)
            go.SetActive(active);
    }

    private void CacheRects()
    {
        if (_prevRect == null && prevButton != null)
        {
            _prevRect = prevButton.GetComponent<RectTransform>();
            if (_prevRect != null)
                _prevBasePos = _prevRect.anchoredPosition;
        }

        if (_nextRect == null && nextButton != null)
        {
            _nextRect = nextButton.GetComponent<RectTransform>();
            if (_nextRect != null)
                _nextBasePos = _nextRect.anchoredPosition;
        }

        if (_yRect == null && yButton != null)
        {
            _yRect = yButton.GetComponent<RectTransform>();
            if (_yRect != null)
                _yBasePos = _yRect.anchoredPosition;
        }
    }

    private void RestorePositions()
    {
        if (_prevRect != null)
            _prevRect.anchoredPosition = _prevBasePos;
        if (_nextRect != null)
            _nextRect.anchoredPosition = _nextBasePos;
    }

    private void ShiftPrevNextUp()
    {
        if (_prevRect == null || _nextRect == null || _yRect == null)
            return;

        _prevRect.anchoredPosition = _yBasePos;
        _nextRect.anchoredPosition = _prevBasePos;
    }
}
