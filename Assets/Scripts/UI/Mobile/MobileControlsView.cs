using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class MobileControlsView : MonoBehaviour
{
    public const string ResourcePath = "UI/MobileControlsCanvas";

    [Header("Roots")]
    public RectTransform safeAreaRoot;
    public RectTransform joystickRoot;
    public RectTransform keyButtonsRoot;

    [Header("Visibility")]
    public MobileControlsRoot joystickVisibilityRoot;
    public MobileControlsRoot keyButtonsVisibilityRoot;
    public SafeAreaFitter keyButtonsSafeArea;

    [Header("Controls")]
    public MobileJoystick moveJoystick;
    public MobileLookJoystick lookJoystick;
    public MobileJoystickInputRouter inputRouter;
    public MobileActionButtons actionButtons;
    public MobileKeyButtonsVisibility keyButtonsVisibility;
    public MobileInventoryFooter inventoryFooter;

    [Header("Joystick Visuals")]
    public Image moveJoystickBaseImage;
    public Image moveJoystickHandleImage;
    public Image lookJoystickBaseImage;
    public Image lookJoystickHandleImage;

    [Header("Action Button Visuals")]
    public Image grabButtonImage;
    public Image dropButtonImage;
    public Image throwButtonImage;
    public Image secondaryButtonImage;
    public Image torchButtonImage;

    public void InitializeAfterInstantiation()
    {
        DontDestroyOnLoad(gameObject);
        NormalizeRootTransform();
        EnsureReferences();
        CacheRuntimeState();
    }

    private void Awake()
    {
        NormalizeRootTransform();
        EnsureReferences();
        CacheRuntimeState();
    }

    private void Update()
    {
        NormalizeRootTransform();
        NormalizeChildRoots();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureReferences();
    }
#endif

    public void EnsureReferences()
    {
        if (safeAreaRoot == null)
            safeAreaRoot = transform.Find("SafeAreaRoot") as RectTransform;

        if (joystickRoot == null)
            joystickRoot = transform.Find("MobileJoysticksRoot") as RectTransform;

        if (keyButtonsRoot == null)
            keyButtonsRoot = transform.Find("MobileKeyButtonsRoot") as RectTransform;
        if (keyButtonsRoot == null)
            keyButtonsRoot = transform.Find("SafeAreaRoot/MobileKeyButtonsRoot") as RectTransform;

        if (joystickVisibilityRoot == null && joystickRoot != null)
            joystickVisibilityRoot = joystickRoot.GetComponent<MobileControlsRoot>();

        if (keyButtonsVisibilityRoot == null && keyButtonsRoot != null)
            keyButtonsVisibilityRoot = keyButtonsRoot.GetComponent<MobileControlsRoot>();

        if (keyButtonsSafeArea == null && keyButtonsRoot != null)
            keyButtonsSafeArea = keyButtonsRoot.GetComponent<SafeAreaFitter>();

        if (moveJoystick == null)
            moveJoystick = GetComponentInChildren<MobileJoystick>(true);

        if (lookJoystick == null)
            lookJoystick = GetComponentInChildren<MobileLookJoystick>(true);

        if (inputRouter == null)
            inputRouter = GetComponent<MobileJoystickInputRouter>();

        if (actionButtons == null)
            actionButtons = GetComponentInChildren<MobileActionButtons>(true);

        if (keyButtonsVisibility == null)
            keyButtonsVisibility = GetComponentInChildren<MobileKeyButtonsVisibility>(true);

        if (inventoryFooter == null)
        {
            inventoryFooter = GetComponentInChildren<MobileInventoryFooter>(true);
            if (inventoryFooter == null)
            {
                Transform footerRoot = transform.Find("FooterButtons");
                if (footerRoot != null)
                    inventoryFooter = footerRoot.gameObject.AddComponent<MobileInventoryFooter>();
            }
        }

        if (moveJoystickBaseImage == null && moveJoystick != null)
            moveJoystickBaseImage = moveJoystick.GetComponent<Image>();

        if (moveJoystickHandleImage == null && moveJoystick != null && moveJoystick.handle != null)
            moveJoystickHandleImage = moveJoystick.handle.GetComponent<Image>();

        if (lookJoystickBaseImage == null && lookJoystick != null)
            lookJoystickBaseImage = lookJoystick.GetComponent<Image>();

        if (lookJoystickHandleImage == null && lookJoystick != null && lookJoystick.handle != null)
            lookJoystickHandleImage = lookJoystick.handle.GetComponent<Image>();

        if (actionButtons != null)
        {
            if (secondaryButtonImage == null && actionButtons.secondaryButton != null)
                secondaryButtonImage = actionButtons.secondaryButton.GetComponent<Image>();

            if (grabButtonImage == null && actionButtons.primaryButton != null)
                grabButtonImage = actionButtons.primaryButton.GetComponent<Image>();

            if (torchButtonImage == null && actionButtons.torchButton != null)
                torchButtonImage = actionButtons.torchButton.GetComponent<Image>();
        }

        if (inputRouter != null)
        {
            if (inputRouter.joystickRoot == null)
                inputRouter.joystickRoot = joystickRoot;
            if (inputRouter.moveJoystick == null)
                inputRouter.moveJoystick = moveJoystick;
            if (inputRouter.lookJoystick == null)
                inputRouter.lookJoystick = lookJoystick;
        }
    }

    private void CacheRuntimeState()
    {
        if (moveJoystick != null)
            moveJoystick.CacheStartPositions();

        if (lookJoystick != null)
            lookJoystick.CacheStartPositions();
    }

    private void NormalizeRootTransform()
    {
        RectTransform rect = transform as RectTransform;
        if (rect == null)
            return;

        rect.localScale = Vector3.one;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = Vector2.zero;
    }

    private void NormalizeChildRoots()
    {
        NormalizeChildRect(joystickRoot);
        NormalizeChildRect(keyButtonsRoot);
        NormalizeChildRect(safeAreaRoot);

        if (moveJoystick != null)
        {
            moveJoystick.gameObject.SetActive(true);
            RectTransform moveRect = moveJoystick.transform as RectTransform;
            if (moveRect != null)
                moveRect.localScale = Vector3.one;
        }

        if (lookJoystick != null)
        {
            lookJoystick.gameObject.SetActive(true);
            RectTransform lookRect = lookJoystick.transform as RectTransform;
            if (lookRect != null)
                lookRect.localScale = Vector3.one;
        }
    }

    private static void NormalizeChildRect(RectTransform rect)
    {
        if (rect == null)
            return;

        rect.gameObject.SetActive(true);
        rect.localScale = Vector3.one;

        if (rect.parent == null)
            return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
