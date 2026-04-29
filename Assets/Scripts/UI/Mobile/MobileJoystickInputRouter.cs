using UnityEngine;
using UnityEngine.EventSystems;

[DefaultExecutionOrder(-100)]
public class MobileJoystickInputRouter : MonoBehaviour
{
    public RectTransform joystickRoot;
    public MobileJoystick moveJoystick;
    public MobileLookJoystick lookJoystick;

    [Header("Floating")]
    public bool freezeFloatingWhenDoorGrabbed = true;

    [Header("Tap For Primary")]
    public bool enablePrimaryTouch = true;
    public float joystickActivationThreshold = 12f;
    public float doorHoldActivationTime = 0.2f;
    [Range(0.05f, 1f)] public float doorHoldCenterWidth = 0.3f;
    [Range(0.05f, 1f)] public float doorHoldCenterHeight = 0.35f;

    [Header("Mouse Simulation")]
    public bool enableMouseSimulation = true;

    private int _leftId = int.MinValue;
    private int _rightId = int.MinValue;
    private int _primaryId = int.MinValue;
    private int _clearPrimaryScreenFrame = -1;

    private struct PendingTouch
    {
        public int id;
        public Vector2 startPos;
        public Vector2 lastPos;
        public float startTime;
        public bool doorCandidate;
        public bool active;
    }

    private PendingTouch _pendingLeft;
    private PendingTouch _pendingRight;

    private InteractionController _interaction;

    private void Update()
    {
        if (!MobileInput.Enabled)
        {
            ResetAll();
            ApplyJoystickVisibility(false, false);
            return;
        }

        bool allowMove = true;
        bool allowLook = true;
        bool allowPrimary = true;
        if (!ResolveInputPermissions(ref allowMove, ref allowLook, ref allowPrimary))
        {
            ResetAll();
            ApplyJoystickVisibility(false, false);
            return;
        }

        ApplyJoystickVisibility(allowMove, allowLook);

        if (!allowPrimary && _primaryId != int.MinValue)
        {
            MobileInput.SetPrimaryHeld(false);
            MobileInput.ClearPrimaryScreenPos();
            _primaryId = int.MinValue;
        }

        if (_clearPrimaryScreenFrame >= 0 && Time.frameCount >= _clearPrimaryScreenFrame && _primaryId == int.MinValue)
        {
            MobileInput.ClearPrimaryScreenPos();
            _clearPrimaryScreenFrame = -1;
        }

        bool doorGrabbed = freezeFloatingWhenDoorGrabbed && IsDoorGrabbed();
        HandleTouches(doorGrabbed, allowMove, allowLook, allowPrimary);

        if (enableMouseSimulation && Input.touchCount == 0)
            HandleMouse(doorGrabbed, allowMove, allowLook, allowPrimary);
    }

    private void HandleTouches(bool doorGrabbed, bool allowMove, bool allowLook, bool allowPrimary)
    {
        int touchCount = Input.touchCount;
        if (touchCount == 0)
            return;

        float halfWidth = Screen.width * 0.5f;
        float thresholdSqr = joystickActivationThreshold * joystickActivationThreshold;

        for (int i = 0; i < touchCount; i++)
        {
            Touch touch = Input.touches[i];
            int id = touch.fingerId;

            if (touch.phase == TouchPhase.Began)
            {
                if (IsOverUI(id))
                    continue;

                bool isLeftHalf = touch.position.x < halfWidth;

                if ((isLeftHalf && allowMove) || (!isLeftHalf && allowLook))
                    StartPending(isLeftHalf, id, touch.position);
            }

            if (id == _leftId)
            {
                if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                    moveJoystick.ProcessDrag(touch.position, null);
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    moveJoystick.ProcessPointerUp(true);
                    _leftId = int.MinValue;
                }
            }
            else if (id == _rightId)
            {
                if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                    lookJoystick.ProcessDrag(touch.position, null);
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    lookJoystick.ProcessPointerUp(true);
                    _rightId = int.MinValue;
                }
            }
            else if (id == _primaryId)
            {
                if (!allowPrimary)
                {
                    MobileInput.SetPrimaryHeld(false);
                    MobileInput.ClearPrimaryScreenPos();
                    _primaryId = int.MinValue;
                    continue;
                }

                if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                    MobileInput.SetPrimaryScreenPos(touch.position);
                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    MobileInput.SetPrimaryHeld(false);
                    MobileInput.ClearPrimaryScreenPos();
                    _primaryId = int.MinValue;
                }
            }
            else if (IsPendingLeft(id))
            {
                if (!allowMove)
                {
                    ClearPending(ref _pendingLeft);
                    continue;
                }

                if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                {
                    UpdatePending(ref _pendingLeft, touch.position);
                    if (TryPromotePendingDoorHold(ref _pendingLeft, id, allowPrimary))
                        continue;

                    if (!IsPendingDoorCandidate(_pendingLeft) &&
                        _leftId == int.MinValue &&
                        (_pendingLeft.lastPos - _pendingLeft.startPos).sqrMagnitude >= thresholdSqr)
                    {
                        _leftId = id;
                        moveJoystick.ProcessPointerDown(_pendingLeft.startPos, null, joystickRoot, !doorGrabbed);
                        moveJoystick.ProcessDrag(touch.position, null);
                        ClearPending(ref _pendingLeft);
                    }
                }
                if (touch.phase == TouchPhase.Ended)
                {
                    TriggerPrimaryTap(_pendingLeft.lastPos);
                    ClearPending(ref _pendingLeft);
                }
                else if (touch.phase == TouchPhase.Canceled)
                {
                    ClearPending(ref _pendingLeft);
                }
            }
            else if (IsPendingRight(id))
            {
                if (!allowLook)
                {
                    ClearPending(ref _pendingRight);
                    continue;
                }

                if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                {
                    UpdatePending(ref _pendingRight, touch.position);
                    if (TryPromotePendingDoorHold(ref _pendingRight, id, allowPrimary))
                        continue;

                    if (!IsPendingDoorCandidate(_pendingRight) &&
                        _rightId == int.MinValue &&
                        (_pendingRight.lastPos - _pendingRight.startPos).sqrMagnitude >= thresholdSqr)
                    {
                        _rightId = id;
                        lookJoystick.ProcessPointerDown(_pendingRight.startPos, null, joystickRoot, !doorGrabbed);
                        lookJoystick.ProcessDrag(touch.position, null);
                        ClearPending(ref _pendingRight);
                    }
                }
                if (touch.phase == TouchPhase.Ended)
                {
                    TriggerPrimaryTap(_pendingRight.lastPos);
                    ClearPending(ref _pendingRight);
                }
                else if (touch.phase == TouchPhase.Canceled)
                {
                    ClearPending(ref _pendingRight);
                }
            }
        }
    }

    private void HandleMouse(bool doorGrabbed, bool allowMove, bool allowLook, bool allowPrimary)
    {
        float halfWidth = Screen.width * 0.5f;
        float thresholdSqr = joystickActivationThreshold * joystickActivationThreshold;
        Vector2 mousePos = Input.mousePosition;

        if (Input.GetMouseButtonDown(0))
        {
            if (IsOverUI(-1))
                return;

            bool isLeftHalf = mousePos.x < halfWidth;

            if ((isLeftHalf && allowMove) || (!isLeftHalf && allowLook))
                StartPending(isLeftHalf, -1, mousePos);
        }

        if (Input.GetMouseButton(0))
        {
            if (_leftId == -1)
            {
                moveJoystick.ProcessDrag(mousePos, null);
            }
            else if (_rightId == -1)
            {
                lookJoystick.ProcessDrag(mousePos, null);
            }
            else if (_primaryId == -1)
            {
                if (!allowPrimary)
                {
                    MobileInput.SetPrimaryHeld(false);
                    MobileInput.ClearPrimaryScreenPos();
                    _primaryId = int.MinValue;
                }
                MobileInput.SetPrimaryScreenPos(mousePos);
            }
            else if (IsPendingLeft(-1))
            {
                if (!allowMove)
                {
                    ClearPending(ref _pendingLeft);
                    return;
                }

                UpdatePending(ref _pendingLeft, mousePos);
                if (TryPromotePendingDoorHold(ref _pendingLeft, -1, allowPrimary))
                    return;

                if (!IsPendingDoorCandidate(_pendingLeft) &&
                    _leftId == int.MinValue &&
                    (_pendingLeft.lastPos - _pendingLeft.startPos).sqrMagnitude >= thresholdSqr)
                {
                    _leftId = -1;
                    moveJoystick.ProcessPointerDown(_pendingLeft.startPos, null, joystickRoot, !doorGrabbed);
                    moveJoystick.ProcessDrag(mousePos, null);
                    ClearPending(ref _pendingLeft);
                }
            }
            else if (IsPendingRight(-1))
            {
                if (!allowLook)
                {
                    ClearPending(ref _pendingRight);
                    return;
                }

                UpdatePending(ref _pendingRight, mousePos);
                if (TryPromotePendingDoorHold(ref _pendingRight, -1, allowPrimary))
                    return;

                if (!IsPendingDoorCandidate(_pendingRight) &&
                    _rightId == int.MinValue &&
                    (_pendingRight.lastPos - _pendingRight.startPos).sqrMagnitude >= thresholdSqr)
                {
                    _rightId = -1;
                    lookJoystick.ProcessPointerDown(_pendingRight.startPos, null, joystickRoot, !doorGrabbed);
                    lookJoystick.ProcessDrag(mousePos, null);
                    ClearPending(ref _pendingRight);
                }
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (_leftId == -1)
            {
                moveJoystick.ProcessPointerUp(true);
                _leftId = int.MinValue;
            }
            else if (_rightId == -1)
            {
                lookJoystick.ProcessPointerUp(true);
                _rightId = int.MinValue;
            }

            if (_primaryId == -1)
            {
                MobileInput.SetPrimaryHeld(false);
                MobileInput.ClearPrimaryScreenPos();
                _primaryId = int.MinValue;
            }
            else if (IsPendingLeft(-1))
            {
                TriggerPrimaryTap(_pendingLeft.lastPos);
                ClearPending(ref _pendingLeft);
            }
            else if (IsPendingRight(-1))
            {
                TriggerPrimaryTap(_pendingRight.lastPos);
                ClearPending(ref _pendingRight);
            }
        }
    }

    private bool IsOverUI(int pointerId)
    {
        if (EventSystem.current == null)
            return false;

        if (pointerId < 0)
            return EventSystem.current.IsPointerOverGameObject();

        return EventSystem.current.IsPointerOverGameObject(pointerId);
    }

    private bool IsDoorGrabbed()
    {
        if (_interaction == null)
        {
            GamePlayer gp = Player.Instance as GamePlayer;
            if (gp != null)
                _interaction = gp.interactionController;
        }

        return _interaction != null && _interaction.IsDoorGrabbed();
    }

    private bool IsDoorUnderScreenPoint(Vector2 screenPos)
    {
        if (_interaction == null)
        {
            GamePlayer gp = Player.Instance as GamePlayer;
            if (gp != null)
                _interaction = gp.interactionController;
        }

        if (_interaction == null)
            return false;

        return _interaction.TryGetDoorUnderScreenPoint(screenPos, out _);
    }

    private bool IsTouchOnJoystick(Vector2 screenPos)
    {
        if (moveJoystick != null && moveJoystick.gameObject.activeInHierarchy && moveJoystick.ContainsScreenPoint(screenPos, null))
            return true;
        if (lookJoystick != null && lookJoystick.gameObject.activeInHierarchy && lookJoystick.ContainsScreenPoint(screenPos, null))
            return true;
        return false;
    }

    private void ResetAll()
    {
        if (_leftId != int.MinValue)
        {
            moveJoystick.ProcessPointerUp(true);
            _leftId = int.MinValue;
        }

        if (_rightId != int.MinValue)
        {
            lookJoystick.ProcessPointerUp(true);
            _rightId = int.MinValue;
        }

        if (_primaryId != int.MinValue)
        {
            MobileInput.SetPrimaryHeld(false);
            MobileInput.ClearPrimaryScreenPos();
            _primaryId = int.MinValue;
        }

        ClearPending(ref _pendingLeft);
        ClearPending(ref _pendingRight);
        _clearPrimaryScreenFrame = -1;
        MobileInput.ClearPrimaryScreenPos();
    }

    private bool ResolveInputPermissions(ref bool allowMove, ref bool allowLook, ref bool allowPrimary)
    {
        if (Player.Instance != null && Player.Instance.IsLocked())
        {
            if (Player.Instance.IsDead())
            {
                allowMove = false;
                allowPrimary = false;
                allowLook = true;
            }
            else
            {
                return false;
            }
        }

        if (TutorialInputGate.Enabled)
        {
            if (!TutorialInputGate.AllowMovement)
                allowMove = false;
            if (!TutorialInputGate.AllowLook)
                allowLook = false;
            if (!TutorialInputGate.AllowInteract && !TutorialInputGate.AllowInteractSpmz)
                allowPrimary = false;
        }

        if (joystickRoot != null)
        {
            CanvasGroup group = joystickRoot.GetComponent<CanvasGroup>();
            if (group != null)
            {
                bool visible = group.interactable && group.alpha > 0.001f;
                if (!visible)
                    return false;
            }
        }

        return true;
    }

    private void ApplyJoystickVisibility(bool moveVisible, bool lookVisible)
    {
        if (!moveVisible)
        {
            ClearPending(ref _pendingLeft);
            if (_leftId != int.MinValue)
            {
                moveJoystick.ProcessPointerUp(true);
                _leftId = int.MinValue;
            }
        }

        if (!lookVisible)
        {
            ClearPending(ref _pendingRight);
            if (_rightId != int.MinValue)
            {
                lookJoystick.ProcessPointerUp(true);
                _rightId = int.MinValue;
            }
        }

        SetJoystickActive(moveJoystick, moveVisible);
        SetJoystickActive(lookJoystick, lookVisible);
    }

    private void SetJoystickActive(MobileJoystick joystick, bool active)
    {
        if (joystick == null)
            return;
        if (joystick.gameObject.activeSelf == active)
            return;

        if (!active)
            joystick.ProcessPointerUp(true);

        joystick.gameObject.SetActive(active);
    }

    private void SetJoystickActive(MobileLookJoystick joystick, bool active)
    {
        if (joystick == null)
            return;
        if (joystick.gameObject.activeSelf == active)
            return;

        if (!active)
            joystick.ProcessPointerUp(true);

        joystick.gameObject.SetActive(active);
    }

    private void StartPending(bool isLeftHalf, int id, Vector2 position)
    {
        bool doorCandidate = enablePrimaryTouch &&
                             IsCenteredDoorTargeted() &&
                             IsInDoorHoldCenterZone(position);

        if (isLeftHalf)
        {
            if (!_pendingLeft.active)
            {
                _pendingLeft.active = true;
                _pendingLeft.id = id;
                _pendingLeft.startPos = position;
                _pendingLeft.lastPos = position;
                _pendingLeft.startTime = Time.time;
                _pendingLeft.doorCandidate = doorCandidate;
            }
        }
        else
        {
            if (!_pendingRight.active)
            {
                _pendingRight.active = true;
                _pendingRight.id = id;
                _pendingRight.startPos = position;
                _pendingRight.lastPos = position;
                _pendingRight.startTime = Time.time;
                _pendingRight.doorCandidate = doorCandidate;
            }
        }
    }

    private static void UpdatePending(ref PendingTouch pending, Vector2 position)
    {
        if (!pending.active)
            return;
        pending.lastPos = position;
    }

    private static void ClearPending(ref PendingTouch pending)
    {
        pending.active = false;
        pending.id = int.MinValue;
        pending.startPos = Vector2.zero;
        pending.lastPos = Vector2.zero;
        pending.startTime = 0f;
        pending.doorCandidate = false;
    }

    private bool IsPendingLeft(int id)
    {
        return _pendingLeft.active && _pendingLeft.id == id;
    }

    private bool IsPendingRight(int id)
    {
        return _pendingRight.active && _pendingRight.id == id;
    }

    private void TriggerPrimaryTap(Vector2 screenPos)
    {
        if (!enablePrimaryTouch || _primaryId != int.MinValue)
            return;

        MobileInput.SetPrimaryScreenPos(screenPos);
        MobileInput.PressPrimary();
        _clearPrimaryScreenFrame = Time.frameCount + 1;
    }

    private bool TryPromotePendingDoorHold(ref PendingTouch pending, int id, bool allowPrimary)
    {
        if (!pending.active || pending.id != id || !allowPrimary || !enablePrimaryTouch)
            return false;

        if (!pending.doorCandidate)
            return false;

        if (_primaryId != int.MinValue)
            return false;

        if (Time.time - pending.startTime < doorHoldActivationTime)
            return false;

        if (!IsCenteredDoorTargeted())
            return false;

        _primaryId = id;
        MobileInput.SetPrimaryHeld(true);
        MobileInput.SetPrimaryScreenPos(pending.lastPos);
        ClearPending(ref pending);
        return true;
    }

    private static bool IsPendingDoorCandidate(PendingTouch pending)
    {
        return pending.active && pending.doorCandidate;
    }

    private bool IsCenteredDoorTargeted()
    {
        if (_interaction == null)
        {
            GamePlayer gp = Player.Instance as GamePlayer;
            if (gp != null)
                _interaction = gp.interactionController;
        }

        return _interaction != null && _interaction.IsDoorTargeted();
    }

    private bool IsInDoorHoldCenterZone(Vector2 screenPos)
    {
        if (Screen.width <= 0 || Screen.height <= 0)
            return false;

        float halfZoneWidth = Screen.width * doorHoldCenterWidth * 0.5f;
        float halfZoneHeight = Screen.height * doorHoldCenterHeight * 0.5f;
        Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Vector2 delta = screenPos - center;

        return Mathf.Abs(delta.x) <= halfZoneWidth &&
               Mathf.Abs(delta.y) <= halfZoneHeight;
    }
}
