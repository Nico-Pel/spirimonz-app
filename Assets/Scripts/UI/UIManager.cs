using System;
using UnityEngine;
using TMPro;
using UnityEngine.Serialization;
using UnityEngine.UI;
using DG.Tweening;

public class UIManager : GameBehaviour
{
    public Image overlay;

    private bool _currentCursorState;
    private int _showCursorsActivatedCount;
    public bool IsCursorActive => _showCursorsActivatedCount > 0;

    protected Player _player;

    protected virtual void Start()
    {
        ShowCursor(false);

        _player = Player.Instance;
    }

    public void AddShowCursor()
    {
        _showCursorsActivatedCount++;
        
        if (_currentCursorState == false)
            ShowCursor(true);
    }

    public void RemoveShowCursor()
    {
        _showCursorsActivatedCount--;
        
        if(_showCursorsActivatedCount <= 0)
            ShowCursor(false);
    }
    
    public void EnableOverlay(bool enable, float fadeDuration)
    {
        if (overlay == null)
            return;

        overlay.DOKill();
        Color colorToUse = enable ? Color.black : new Color(0, 0, 0, 0);
        if (fadeDuration <= 0)
        {
            overlay.color = colorToUse;
        }
        else
        {
            overlay.DOColor(colorToUse, fadeDuration);
        }
    }

    public void BlinkOverlay(float halfBlinkDuration = 0.2f)
    {
        EnableOverlay(true, halfBlinkDuration);
        this.Invoke(halfBlinkDuration, () =>
        {
            EnableOverlay(false, halfBlinkDuration);
        });
    }
    
    private void ShowCursor(bool enable)
    {
        if (MobileInput.Enabled)
        {
            bool showCursor = !Application.isMobilePlatform;
            _currentCursorState = showCursor;
            Cursor.visible = showCursor;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            _currentCursorState = enable;
            Cursor.visible = enable;
            Cursor.lockState = enable ? CursorLockMode.None : CursorLockMode.Locked;
        }

        if(_player != null && enable == true)
            _player.LockControls(true);
    }
    
    public virtual void ExitLastMenu()
    {
    }

    public virtual void CloseAllWindows()
    {
    }
}
