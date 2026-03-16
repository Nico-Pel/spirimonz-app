using UnityEngine;
using Cinemachine;

public class MobileCinemachineInputGate : MonoBehaviour
{
    private static MobileCinemachineInputGate _instance;

    public static void EnsureExists()
    {
        if (_instance != null)
            return;

        GameObject go = new GameObject("MobileCinemachineInputGate");
        DontDestroyOnLoad(go);
        _instance = go.AddComponent<MobileCinemachineInputGate>();
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        CinemachineCore.GetInputAxis = GetAxis;
    }

    private float GetAxis(string axisName)
    {
        if (MobileInput.Enabled)
            return 0f;
        return Input.GetAxis(axisName);
    }

    private void OnDestroy()
    {
        if (CinemachineCore.GetInputAxis == GetAxis)
            CinemachineCore.GetInputAxis = Input.GetAxis;

        if (_instance == this)
            _instance = null;
    }
}
