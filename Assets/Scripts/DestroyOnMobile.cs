using UnityEngine;

public class DestroyOnMobile : MonoBehaviour
{
    [Tooltip("If enabled, destroy immediately in Awake. Otherwise destroy in Start.")]
    public bool destroyInAwake = true;

    private void Awake()
    {
        if (destroyInAwake)
            TryDestroy();
    }

    private void Start()
    {
        if (!destroyInAwake)
            TryDestroy();
    }

    private void TryDestroy()
    {
        if (!ShouldDestroyForMobile())
            return;

        Destroy(gameObject);
    }

    private bool ShouldDestroyForMobile()
    {
        if (Application.isMobilePlatform)
            return true;

        return GameManager.Instance != null && GameManager.Instance.mobileControlsEnabled;
    }
}
