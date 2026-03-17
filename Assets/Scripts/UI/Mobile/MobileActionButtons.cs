using UnityEngine;

public class MobileActionButtons : MonoBehaviour
{
    public GameObject grabButton;
    public GameObject dropButton;
    public GameObject throwButton;
    public GameObject secondaryButton;
    public GameObject torchButton;

    private GamePlayer _gamePlayer;
    private InteractionController _interaction;

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
            // TPS / autres scènes : garder le bouton grab visible
            SetActive(grabButton, true);
            SetActive(dropButton, false);
            SetActive(throwButton, false);
            SetActive(secondaryButton, false);
            SetActive(torchButton, false);
            return;
        }

        bool hasObject = _interaction != null && _interaction.objectInHands != null;
        bool hasCandleInHands = hasObject && _interaction.objectInHands is CatchableFireObject;
        bool hasSpirimonzInHands = _gamePlayer.inventoryManager != null
            && _gamePlayer.inventoryManager.selectedSpirimonz != null
            && !_gamePlayer.inventoryManager.selectedSpirimonz.isOnTheMap;

        SetActive(grabButton, !hasObject);
        SetActive(dropButton, hasObject);
        SetActive(throwButton, hasObject);
        SetActive(secondaryButton, (!hasObject && !hasSpirimonzInHands) || hasCandleInHands);
        SetActive(torchButton, true);
    }

    private void SetAll(bool enabled)
    {
        SetActive(grabButton, enabled);
        SetActive(dropButton, enabled && false);
        SetActive(throwButton, enabled && false);
        SetActive(secondaryButton, enabled && false);
        SetActive(torchButton, enabled && false);
    }

    private void SetActive(GameObject go, bool active)
    {
        if (go != null && go.activeSelf != active)
            go.SetActive(active);
    }
}
