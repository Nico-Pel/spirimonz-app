using UnityEngine;
using UnityEngine.EventSystems;

public class MobileButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public enum Action
    {
        Primary,
        Secondary,
        Grab,
        Drop,
        Throw,
        Sprint,
        Crouch,
        Jump,
        ToggleLight,
        OpenJournal,
        OpenTeamMenu,
        ExitMenus,
        Next,
        Previous,
        KeyY,
        Inventory1,
        Inventory2,
        Inventory3,
        Inventory4,
        Inventory5,
        Inventory6
    }

    public Action action = Action.Primary;

    public void OnPointerDown(PointerEventData eventData)
    {
        switch (action)
        {
            case Action.Primary:
                MobileInput.SetPrimaryHeld(true);
                break;
            case Action.Secondary:
                MobileInput.SetSecondaryHeld(true);
                break;
            case Action.Sprint:
                MobileInput.SetSprintHeld(true);
                break;
            case Action.Grab:
                MobileInput.PressGrab();
                break;
            case Action.Drop:
                MobileInput.PressDrop();
                break;
            case Action.Throw:
                MobileInput.PressThrow();
                break;
            case Action.Crouch:
                MobileInput.PressCrouch();
                break;
            case Action.Jump:
                MobileInput.PressJump();
                break;
            case Action.ToggleLight:
                MobileInput.PressToggleLight();
                break;
            case Action.OpenJournal:
                MobileInput.PressOpenJournal();
                break;
            case Action.OpenTeamMenu:
                MobileInput.PressOpenTeamMenu();
                break;
            case Action.ExitMenus:
                MobileInput.PressExitMenus();
                break;
            case Action.Next:
                MobileInput.PressNext();
                break;
            case Action.Previous:
                MobileInput.PressPrevious();
                break;
            case Action.KeyY:
                MobileInput.PressY();
                break;
            case Action.Inventory1:
                MobileInput.PressInventorySlot(0);
                break;
            case Action.Inventory2:
                MobileInput.PressInventorySlot(1);
                break;
            case Action.Inventory3:
                MobileInput.PressInventorySlot(2);
                break;
            case Action.Inventory4:
                MobileInput.PressInventorySlot(3);
                break;
            case Action.Inventory5:
                MobileInput.PressInventorySlot(4);
                break;
            case Action.Inventory6:
                MobileInput.PressInventorySlot(5);
                break;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        ReleaseHoldActions();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ReleaseHoldActions();
    }

    private void OnDisable()
    {
        ReleaseHoldActions();
    }

    private void ReleaseHoldActions()
    {
        if (action == Action.Primary)
            MobileInput.SetPrimaryHeld(false);
        else if (action == Action.Secondary)
            MobileInput.SetSecondaryHeld(false);
        else if (action == Action.Sprint)
            MobileInput.SetSprintHeld(false);
    }
}
