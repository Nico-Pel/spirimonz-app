using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UITitleSaveSlot : GameBehaviour
{
    [Header("Slot")]
    public int slotIndex = 1;

    [Header("Panels")]
    public GameObject savePanel;
    public GameObject newGamePanel;

    [Header("Info")]
    public TextMeshProUGUI tMoney;
    public TextMeshProUGUI tUnlocked;
    public Transform spmzPos;
    public float previewScale = 6f;
    public LayerMask previewLayer;
    public Color unlockedTotalColor = new Color32(0x5c, 0x69, 0x9b, 0xff);

    [Header("Button")]
    public Button bSelect;

    [Header("Sounds")]
    public SoundParameters selectSound;
    public SoundParameters selectNewGameSound;

    [ReadOnly] public bool hasSave;

    private GameObject _previewBody;
    private UITitleScreen _titleScreen;

    public void Initialize(UITitleScreen screen)
    {
        _titleScreen = screen;
#if UNITY_EDITOR
        UISoundDefaults.AssignIfNull(ref selectSound);
        UISoundDefaults.AssignIfNull(ref selectNewGameSound);
#endif
        UISoundDefaults.MarkAsUi(selectSound);
        UISoundDefaults.MarkAsUi(selectNewGameSound);
        if (bSelect != null)
        {
            bSelect.onClick.RemoveAllListeners();
            bSelect.onClick.AddListener(OnSelectPressed);
        }
    }

    public void Refresh(GameManager gameManager)
    {
        ClearPreview();

        GameData data = SaveManager.Load(slotIndex, createIfMissing: false);
        hasSave = data != null;

        if (savePanel != null) savePanel.SetActive(hasSave);
        if (newGamePanel != null) newGamePanel.SetActive(!hasSave);

        if (!hasSave || data == null)
            return;

        if (tMoney != null)
            tMoney.text = GetInt(data, SaveKeys.GOLD, 0) + "$";

        SpirimonzSettings[] all = gameManager != null ? gameManager.allSpirimonzSettings : SaveManager.allSpirimonzSettings;
        int unlocked = CountUnlocked(data, all);
        int total = all != null ? all.Length : data.spirimonzCollection.Length;
        if (tUnlocked != null)
            tUnlocked.text = FormatUnlockedText(unlocked, total);

        SpirimonzSettings firstTeamSpmz = FindFirstTeamSpirimonz(data, all);
        if (firstTeamSpmz != null && firstTeamSpmz.spirimonzBodyPrefab != null && spmzPos != null)
        {
            _previewBody = Instantiate(firstTeamSpmz.spirimonzBodyPrefab, spmzPos);
            _previewBody.transform.localPosition = firstTeamSpmz.bodyPresentationOffset;
            _previewBody.transform.localRotation = Quaternion.identity;
            _previewBody.transform.localScale = Vector3.one * previewScale;
            if (previewLayer.value != 0)
                ApplyLayerRecursively(_previewBody, LayerMaskToLayer(previewLayer));
        }
    }

    private void OnSelectPressed()
    {
        if (hasSave)
        {
            if (selectSound != null)
                selectSound.PlaySound();
        }
        else
        {
            if (selectNewGameSound != null)
                selectNewGameSound.PlaySound();
            else if (selectSound != null)
                selectSound.PlaySound();
        }
        if (_titleScreen != null)
            _titleScreen.OnSlotSelected(this);
    }

    private int GetInt(GameData data, string id, int defaultValue)
    {
        if (data == null || data.ints == null)
            return defaultValue;

        for (int i = 0; i < data.ints.Count; i++)
        {
            if (data.ints[i].id == id)
                return data.ints[i].value;
        }

        return defaultValue;
    }

    private int CountUnlocked(GameData data, SpirimonzSettings[] all)
    {
        if (data == null || data.spirimonzCollection == null)
            return 0;

        int count = 0;
        if (all != null && all.Length > 0)
        {
            for (int i = 0; i < all.Length; i++)
            {
                SpirimonzSettings settings = all[i];
                if (settings == null)
                    continue;

                if (settings.unlockedByDefault)
                {
                    count++;
                    continue;
                }

                for (int j = 0; j < data.spirimonzCollection.Length; j++)
                {
                    SpirimonzData spData = data.spirimonzCollection[j];
                    if (spData != null && spData.id == settings.spirimonzID && spData.unlocked)
                    {
                        count++;
                        break;
                    }
                }
            }

            return count;
        }

        foreach (SpirimonzData spData in data.spirimonzCollection)
        {
            if (spData != null && spData.unlocked)
                count++;
        }

        return count;
    }

    private SpirimonzSettings FindFirstTeamSpirimonz(GameData data, SpirimonzSettings[] all)
    {
        if (data == null || data.spirimonzCollection == null || all == null)
            return null;

        SpirimonzData best = null;
        for (int i = 0; i < data.spirimonzCollection.Length; i++)
        {
            SpirimonzData sp = data.spirimonzCollection[i];
            if (sp == null || !sp.inTeam)
                continue;

            if (best == null || sp.teamPosition < best.teamPosition)
                best = sp;
        }

        if (best == null)
            return null;

        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] != null && all[i].spirimonzID == best.id)
                return all[i];
        }

        return null;
    }

    private void ClearPreview()
    {
        if (_previewBody != null)
            Destroy(_previewBody);
        _previewBody = null;
    }

    private void ApplyLayerRecursively(GameObject target, int layer)
    {
        if (target == null || layer < 0)
            return;

        target.layer = layer;
        foreach (Transform child in target.transform)
            ApplyLayerRecursively(child.gameObject, layer);
    }

    private string FormatUnlockedText(int unlocked, int total)
    {
        string colorHex = ColorUtility.ToHtmlStringRGB(unlockedTotalColor);
        return $"{unlocked}<color=#{colorHex}>/{total}</color>";
    }
}
