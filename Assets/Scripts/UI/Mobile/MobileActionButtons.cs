using UnityEngine;
using UnityEngine.UI;

public class MobileActionButtons : MonoBehaviour
{
    public GameObject primaryButton;
    public GameObject secondaryButton;
    public GameObject torchButton;
    public GameObject crouchButton;
    public Image primaryButtonImage;
    public Image secondaryButtonImage;
    public Image torchButtonImage;
    public Image crouchButtonImage;
    public Image torchIconImage;
    public Sprite torchIconEnabledSprite;
    public Sprite torchIconDisabledSprite;

    private GamePlayer _gamePlayer;
    private InteractionController _interaction;
    private MobileButton _primaryMobileButton;
    private MobileButton _secondaryMobileButton;
    private MobileButton _crouchMobileButton;

    private void Awake()
    {
        EnsureReferences();
        CacheSprites();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureReferences();
    }
#endif

    private void Update()
    {
        if (!MobileInput.Enabled)
        {
            SetAll(false);
            return;
        }

        if (_gamePlayer == null)
        {
            _gamePlayer = Player.Instance as GamePlayer;
            if (_gamePlayer != null)
                _interaction = _gamePlayer.interactionController;
        }

        if (_gamePlayer == null)
        {
            SetActive(primaryButton, true);
            SetActive(secondaryButton, false);
            SetActive(torchButton, false);
            SetActive(crouchButton, false);
            SetButtonAction(ref _primaryMobileButton, primaryButton, MobileButton.Action.Grab);
            return;
        }

        bool hasObject = _interaction != null && _interaction.objectInHands != null;
        bool hasCandleInHands = hasObject && _interaction.objectInHands is CatchableFireObject;
        bool hasBookInHands = hasObject && _interaction.objectInHands is CatchableBook;
        bool hasSpirimonzInHands = _gamePlayer.inventoryManager != null
            && _gamePlayer.inventoryManager.selectedSpirimonz != null
            && !_gamePlayer.inventoryManager.selectedSpirimonz.isOnTheMap;
        bool canTalkToNpc = HasNpcInteractionTarget();
        bool hasSecondaryButtonSpirimonz = hasSpirimonzInHands && _gamePlayer.inventoryManager.selectedSpirimonz.useSecondaryButton;
        bool canUseSecondary = (!hasObject && !hasSpirimonzInHands) || hasCandleInHands || hasBookInHands || hasSecondaryButtonSpirimonz;
        bool canThrow = hasObject && !hasCandleInHands;
        bool canUsePrimary = hasObject
            ? TutorialInputGate.IsAllowed(TutorialInputGate.AllowDrop) || canTalkToNpc
            : TutorialInputGate.IsAllowed(TutorialInputGate.AllowGrab)
              || TutorialInputGate.IsAllowed(TutorialInputGate.AllowInteract)
              || TutorialInputGate.IsAllowed(TutorialInputGate.AllowPickupSpmz);
        bool canUseSecondaryButton = (canThrow && TutorialInputGate.IsAllowed(TutorialInputGate.AllowThrow))
            || (canUseSecondary && TutorialInputGate.IsAllowed(TutorialInputGate.AllowSecondary));
        bool canUseTorch = TutorialInputGate.IsAllowed(TutorialInputGate.AllowLight);
        bool canUseCrouch = TutorialInputGate.IsAllowed(TutorialInputGate.AllowMovement);

        SetActive(primaryButton, canUsePrimary);
        SetActive(secondaryButton, canUseSecondaryButton);
        SetActive(torchButton, canUseTorch);
        SetActive(crouchButton, canUseCrouch);

        SetButtonAction(ref _primaryMobileButton, primaryButton, hasObject ? MobileButton.Action.Drop : MobileButton.Action.Grab);
        SetButtonAction(ref _secondaryMobileButton, secondaryButton, canThrow ? MobileButton.Action.Throw : MobileButton.Action.Secondary);
        SetButtonAction(ref _crouchMobileButton, crouchButton, MobileButton.Action.Crouch);
        UpdateTorchVisual();
    }

    private bool HasNpcInteractionTarget()
    {
        if (!TutorialInputGate.IsAllowed(TutorialInputGate.AllowInteract))
            return false;

        Player player = Player.Instance;
        if (player == null || player.IsLocked())
            return false;

        NPC npc = player.currentNPC;
        if (npc == null && _interaction != null)
            npc = _interaction.CurrentNpcTarget;

        return npc != null && npc.CanInteract(player);
    }

    private void SetAll(bool enabled)
    {
        SetActive(primaryButton, enabled);
        SetActive(secondaryButton, enabled && false);
        SetActive(torchButton, enabled && false);
        SetActive(crouchButton, enabled && false);
    }

    private void EnsureReferences()
    {
        if (primaryButton == null)
            primaryButton = FindOptional("Action_A");

        if (secondaryButton == null)
            secondaryButton = FindOptional("Action_B");

        if (torchButton == null)
            torchButton = FindOptional("Action_Torch");

        if (crouchButton == null)
            crouchButton = FindOptional("Action_Crouch");

        if (primaryButtonImage == null && primaryButton != null)
            primaryButtonImage = primaryButton.GetComponent<Image>();

        if (secondaryButtonImage == null && secondaryButton != null)
            secondaryButtonImage = secondaryButton.GetComponent<Image>();

        if (torchButtonImage == null && torchButton != null)
            torchButtonImage = torchButton.GetComponent<Image>();

        if (crouchButtonImage == null && crouchButton != null)
            crouchButtonImage = crouchButton.GetComponent<Image>();

        if (torchIconImage == null && torchButton != null)
        {
            Transform icon = torchButton.transform.Find("iLamp");
            if (icon != null)
                torchIconImage = icon.GetComponent<Image>();
        }

        _primaryMobileButton = GetOrAddMobileButton(primaryButton);
        _secondaryMobileButton = GetOrAddMobileButton(secondaryButton);
        GetOrAddMobileButton(torchButton);
        _crouchMobileButton = GetOrAddMobileButton(crouchButton);
    }

    private void CacheSprites()
    {
        if (torchIconDisabledSprite == null && torchIconImage != null)
            torchIconDisabledSprite = torchIconImage.sprite;

        if (torchIconEnabledSprite == null && torchButton != null)
        {
            torchIconEnabledSprite = FindOptionalSprite(
                "iLampOn",
                "iLampEnabled",
                "iLampAlt",
                "iLampSecondary");
        }
    }

    private void UpdateTorchVisual()
    {
        if (torchIconImage == null || _gamePlayer == null || _gamePlayer.fpsController == null || _gamePlayer.fpsController.mLight == null)
            return;

        bool lightEnabled = _gamePlayer.fpsController.mLight.gameObject.activeSelf;
        Sprite targetSprite = lightEnabled ? torchIconEnabledSprite : torchIconDisabledSprite;
        if (targetSprite != null)
            torchIconImage.sprite = targetSprite;
    }

    private void SetButtonAction(ref MobileButton cachedButton, GameObject buttonObject, MobileButton.Action action)
    {
        cachedButton = GetOrAddMobileButton(buttonObject);
        if (cachedButton != null)
            cachedButton.action = action;
    }

    private MobileButton GetOrAddMobileButton(GameObject buttonObject)
    {
        if (buttonObject == null)
            return null;

        Button button = buttonObject.GetComponent<Button>();
        if (button == null)
            button = buttonObject.AddComponent<Button>();

        Image image = buttonObject.GetComponent<Image>();
        if (button.targetGraphic == null && image != null)
            button.targetGraphic = image;

        MobileButton mobileButton = buttonObject.GetComponent<MobileButton>();
        if (mobileButton == null)
            mobileButton = buttonObject.AddComponent<MobileButton>();

        return mobileButton;
    }

    private GameObject FindOptional(string childName)
    {
        Transform child = transform.Find(childName);
        return child != null ? child.gameObject : null;
    }

    private Sprite FindOptionalSprite(params string[] childNames)
    {
        if (torchButton == null)
            return null;

        for (int i = 0; i < childNames.Length; i++)
        {
            Transform child = torchButton.transform.Find(childNames[i]);
            if (child == null)
                continue;

            Image image = child.GetComponent<Image>();
            if (image != null && image.sprite != null)
                return image.sprite;
        }

        return null;
    }

    private void SetActive(GameObject go, bool active)
    {
        if (go != null && go.activeSelf != active)
            go.SetActive(active);
    }
}
