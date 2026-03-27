using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class WorldRandomHouseLoader : MonoBehaviour
{
    private const string PENDING_ACTION_KEY = "WorldRandomHouseLoader.PendingAction";
    [Header("Scenes")]
    public string[] houseSceneNames = { "House01", "House02", "House03", "House04", "House05" };

    [Header("Random Team")]
    public int randomTeamSize = 6;

    private enum PendingAction
    {
        None,
        LoadRandomHouse,
        LoadRandomHouseWithRandomTeam
    }

    private static PendingAction _pendingAction = PendingAction.None;
    private Coroutine _pendingRoutine;

    private void OnEnable()
    {
        if (!Application.isPlaying)
            return;

#if UNITY_EDITOR
        if (_pendingAction == PendingAction.None)
        {
            int stored = UnityEditor.SessionState.GetInt(PENDING_ACTION_KEY, (int)PendingAction.None);
            if (stored != (int)PendingAction.None)
            {
                _pendingAction = (PendingAction)stored;
                UnityEditor.SessionState.EraseInt(PENDING_ACTION_KEY);
            }
        }
#endif

        if (_pendingAction == PendingAction.None)
            return;

        if (_pendingRoutine != null)
            StopCoroutine(_pendingRoutine);

        _pendingRoutine = StartCoroutine(ExecutePendingAction());
    }

    private IEnumerator ExecutePendingAction()
    {
        // Wait for managers to be initialized (after entering play mode)
        while (GameManager.Instance == null || InventoryManager.Instance == null)
            yield return null;

        // Wait for team settings list to be initialized by LoadTeamFromSave
        while (InventoryManager.Instance.spirimonzTeamSettings == null ||
               InventoryManager.Instance.spirimonzTeamSettings.Count == 0)
            yield return null;

        // Allow one frame for any late init
        yield return null;

        PendingAction action = _pendingAction;
        _pendingAction = PendingAction.None;

        if (action == PendingAction.LoadRandomHouseWithRandomTeam)
            LoadRandomHouseWithRandomTeam();
        else if (action == PendingAction.LoadRandomHouse)
            LoadRandomHouse();

        _pendingRoutine = null;
    }

    public void EditorRequestRandomHouse()
    {
        if (Application.isPlaying)
        {
            LoadRandomHouse();
            return;
        }

#if UNITY_EDITOR
        _pendingAction = PendingAction.LoadRandomHouse;
        UnityEditor.SessionState.SetInt(PENDING_ACTION_KEY, (int)_pendingAction);
        UnityEditor.EditorApplication.EnterPlaymode();
#endif
    }

    public void EditorRequestRandomHouseWithRandomTeam()
    {
        if (Application.isPlaying)
        {
            LoadRandomHouseWithRandomTeam();
            return;
        }

#if UNITY_EDITOR
        _pendingAction = PendingAction.LoadRandomHouseWithRandomTeam;
        UnityEditor.SessionState.SetInt(PENDING_ACTION_KEY, (int)_pendingAction);
        UnityEditor.EditorApplication.EnterPlaymode();
#endif
    }

    public void LoadRandomHouse()
    {
        string sceneName = GetRandomHouseSceneName();
        if (string.IsNullOrEmpty(sceneName))
            return;

        GameManager.Instance.LoadScene(sceneName);
    }

    public void LoadRandomHouseWithRandomTeam()
    {
        ApplyRandomTeam();
        LoadRandomHouse();
    }

    private string GetRandomHouseSceneName()
    {
        if (houseSceneNames == null || houseSceneNames.Length == 0)
            return null;

        int index = Random.Range(0, houseSceneNames.Length);
        return houseSceneNames[index];
    }

    private void ApplyRandomTeam()
    {
        GameManager gm = GameManager.Instance;
        InventoryManager inventory = InventoryManager.Instance;
        if (gm == null || inventory == null)
            return;

        SpirimonzSettings[] all = gm.allSpirimonzSettings;
        if (all == null || all.Length == 0)
            return;

        List<SpirimonzSettings> available = new List<SpirimonzSettings>();
        foreach (SpirimonzSettings s in all)
        {
            if (s != null)
                available.Add(s);
        }

        if (available.Count == 0)
            return;

        int teamSize = Mathf.Max(1, randomTeamSize);
        int pickCount = Mathf.Min(teamSize, available.Count);

        inventory.spirimonzTeamSettings.Clear();
        for (int i = 0; i < teamSize; i++)
            inventory.spirimonzTeamSettings.Add(null);

        for (int i = 0; i < pickCount; i++)
        {
            int index = Random.Range(0, available.Count);
            SpirimonzSettings picked = available[index];
            available.RemoveAt(index);
            inventory.spirimonzTeamSettings[i] = picked;
        }

        inventory.onTeamChange?.Invoke();
    }
}
