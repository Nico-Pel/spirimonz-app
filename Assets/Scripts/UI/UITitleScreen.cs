using UnityEngine;

public class UITitleScreen : GameBehaviour
{
    public UITitleSaveSlot[] slots;
    public string houseTutoSceneName = "HouseTuto";

    private GameManager _gameManager;

    private void Awake()
    {
        _gameManager = GameManager.Instance;

        if (slots != null)
        {
            foreach (UITitleSaveSlot slot in slots)
            {
                if (slot != null)
                    slot.Initialize(this);
            }
        }
    }

    private void Start()
    {
        RefreshSlots();
    }

    public void RefreshSlots()
    {
        if (slots == null)
            return;

        if (_gameManager == null)
            _gameManager = GameManager.Instance;

        foreach (UITitleSaveSlot slot in slots)
        {
            if (slot != null)
                slot.Refresh(_gameManager);
        }
    }

    public void OnSlotSelected(UITitleSaveSlot slot)
    {
        if (slot == null)
            return;

        if (_gameManager == null)
            _gameManager = GameManager.Instance;

        if (_gameManager == null)
            return;

        if (slot.hasSave)
        {
            _gameManager.UseSaveSlot(slot.slotIndex, createIfMissing: true, temporary: false);
            _gameManager.LoadWorldFromCurrentSave();
        }
        else
        {
            _gameManager.UseSaveSlot(slot.slotIndex, createIfMissing: true, temporary: false);
            _gameManager.SetNextHouseSceneMode(GameManager.HouseSceneMode.Tutorial);
            _gameManager.LoadScene(houseTutoSceneName);
        }
    }
}
