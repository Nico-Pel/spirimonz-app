using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;
using UnityEngine.Serialization;

public class InventoryManager : GameBehaviour
{
    public static InventoryManager Instance { get; private set; }

    public enum HandPoses
    {
        Null,
        LightAim,
        CameraAim,
        PalmOfTheHand,
        HoldOneHand,
        HoldTwoHands,
        HoldTwoHandsSmall
    }

    [Header("Team")]
    public List<SpirimonzSettings> spirimonzTeamSettings = new List<SpirimonzSettings>(5);
    public List<Spirimonz> spirimonzTeam = new List<Spirimonz>();
    [ReadOnly] public Spirimonz selectedSpirimonz = null;
    [ReadOnly] public int currentSelectedIndex;

    [Header("Layer Masks")] 
    public LayerMask fpsMask;
    public LayerMask spirimonzMask;

    private bool _forcedLightStateDuringCam;
    private Player _player;
    private GamePlayer _gamePlayer;
    private GameManager _gameManager;

    public UnityEvent onTeamChange;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
        Instance = this;
    }

    private void Start()
    {
        InitPlayer();
        _gameManager = GameManager.Instance;
    }

    private void InitPlayer()
    {
        _player = Player.Instance;
        _gamePlayer = _player as GamePlayer;
    }

    public void OnLoadHouseScene()
    {
        InitPlayer();
        InitializeTeam(); 
        UseWatchObject();
    }

    /// <summary>Remplit spirimonzTeamSettings à partir du gameData du GameManager</summary>
    public void LoadTeamFromSave()
    {
        spirimonzTeamSettings.Clear();
        for (int i = 0; i < 5; i++) spirimonzTeamSettings.Add(null);

        var data = _gameManager.GetGameData();

        foreach (var spData in data.spirimonzCollection)
        {
            if (!spData.inTeam) continue;

            var settings = Array.Find(_gameManager.allSpirimonzSettings, s => s.spirimonzID == spData.id);
            if (settings != null && spData.teamPosition >= 0 && spData.teamPosition < spirimonzTeamSettings.Count)
            {
                spirimonzTeamSettings[spData.teamPosition] = settings;
            }
        }
    }

    public bool AddSpirimonzToTeam(SpirimonzSettings spirimonz, int position = -1)
    {
        if (_gameManager == null) return false;

        // Cherche si le Spirimonz est déjà dans la team
        int existingIndex = spirimonzTeamSettings.FindIndex(s => s != null && s.spirimonzID == spirimonz.spirimonzID);

        if (position < 0)
        {
            // Ajout au prochain slot libre
            position = spirimonzTeamSettings.FindIndex(s => s == null);
            if (position == -1)
            {
                Debug.LogWarning($"Pas de place pour {spirimonz.spirimonzName}");
                return false;
            }

            if (existingIndex != -1)
            {
                if (position < existingIndex)
                {
                    // Le nouveau slot est "plus haut" (index plus petit) → on retire l'ancien Spirimonz
                    RemoveSpirimonzFromTeam(existingIndex);
                }
                else
                {
                    // Le nouveau slot est plus bas ou égal → ne rien faire
                    Debug.Log($"Spirimonz {spirimonz.spirimonzName} est déjà dans la team, aucun ajout nécessaire.");
                    return false;
                }
            }
        }
        else
        {
            // Ajout à une position spécifique
            if (existingIndex != -1 && existingIndex != position)
            {
                RemoveSpirimonzFromTeam(existingIndex);
            }
        }

        // Retire un Spirimonz déjà présent à cette position
        if (spirimonzTeamSettings[position] != null)
        {
            if (existingIndex != -1)
            {
                AddSpirimonzToTeam(spirimonzTeamSettings[position], existingIndex);
            }
            RemoveSpirimonzFromTeam(position);
        }

        // Ajoute le Spirimonz
        spirimonzTeamSettings[position] = spirimonz;

        // Met à jour le GameManager / save
        _gameManager.SetSpirimonzInTeam(spirimonz.spirimonzID, position, true);

        onTeamChange?.Invoke();
        return true;
    }


    /// <summary>Retire un Spirimonz de la team</summary>
    public void RemoveSpirimonzFromTeam(int position)
    {
        if (position < 0 || position >= spirimonzTeamSettings.Count) return;

        var settings = spirimonzTeamSettings[position];
        if (settings == null) return;

        spirimonzTeamSettings[position] = null;

        // Mettre à jour le GameManager (et la save)
        _gameManager.SetSpirimonzInTeam(settings.spirimonzID, position, false);
        onTeamChange?.Invoke();
    }

    /// <summary>Instancie les Spirimonz dans les mains du joueur (uniquement quand on est dans une maison)</summary>
    public void InitializeTeam()
    {
        spirimonzTeam.Clear();

        if (_gamePlayer == null) return;

        foreach (var spmzS in spirimonzTeamSettings)
        {
            if (spmzS == null) continue;

            Spirimonz newSpirimonz = Instantiate(spmzS.spirimonzPrefab, _gamePlayer.spirimonzHandPos);
            spirimonzTeam.Add(newSpirimonz);
            newSpirimonz.transform.localPosition = Vector3.zero;
            newSpirimonz.transform.localEulerAngles = Vector3.zero;
            newSpirimonz.ChangeLayer(fpsMask, 0);
        }
    }
    
    void Update()
    {
        if (_gamePlayer != null)
        {
            UpdateGamePlayer();
        }
    }

    private void UpdateGamePlayer()
    {
        if (selectedSpirimonz != null && selectedSpirimonz.isOnTheMap == false)
        {
            selectedSpirimonz.currentRoom = _gamePlayer.currentRoom;
        }
        
        for (int i = 0; i < _player.inputManager.inventoryKeys.Length; i++)
        {
            if (Input.GetKeyDown(_player.inputManager.inventoryKeys[i]))
            {
                currentSelectedIndex = i;
                if (i == 0)
                {
                    UseWatchObject();
                }
                else
                {
                    EquipSpirimonz(i - 1);
                }
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            TryToDropSpirimonz();
        }
        
        if (Input.GetMouseButtonDown(1) && _gamePlayer.handAnimator.GetInteger("HandPos") == (int)HandPoses.LightAim)
        {
            TurnOnNightVision();
        }
        
        if (Input.GetMouseButtonUp(1) && _gamePlayer.handAnimator.GetInteger("HandPos") == (int)HandPoses.CameraAim)
        {
            TurnOffNightVision();
        }
    }

    private void TurnOnNightVision()
    {
        _gamePlayer.handAnimator.SetInteger("HandPos", (int)HandPoses.CameraAim);
        if (_gamePlayer.fpsController.mLight.gameObject.activeInHierarchy == true)
        {
            _gamePlayer.fpsController.ForceLightState(false);
            _forcedLightStateDuringCam = true;
        }
    }
    
    private void TurnOffNightVision()
    {
        if(_forcedLightStateDuringCam)
        {this.Invoke(0.25f, () =>
            {
                if (_gamePlayer.handAnimator.GetInteger("HandPos") == (int)HandPoses.LightAim)
                {
                    _gamePlayer.fpsController.ForceLightState(true);
                    _forcedLightStateDuringCam = false;
                }
            });
        }
        _gamePlayer.handAnimator.SetInteger("HandPos", (int)HandPoses.LightAim);
    }

    private void UseWatchObject()
    {
        if (_gamePlayer.interactionController.objectInHands) return;
        
        _gamePlayer.handAnimator.SetInteger("HandPos", (int)HandPoses.LightAim);
        UnequipSpirimonz();
    }

    private void EquipSpirimonz(int teamIndex)
    {
        //You can't select a Spirimonz if an object in hands
        if (_gamePlayer.interactionController.objectInHands != null) return;

        if (spirimonzTeam[teamIndex] == null) return;
        
        if (selectedSpirimonz != null && selectedSpirimonz.isOnTheMap == false)
        {
            //Spirimonz is already selected
            if(spirimonzTeam[teamIndex] == selectedSpirimonz) return;
            
            selectedSpirimonz.gameObject.SetActive(false);
        }
        
        selectedSpirimonz = spirimonzTeam[teamIndex];
        
        if (selectedSpirimonz.isOnTheMap)
        {
            _gamePlayer.handAnimator.SetInteger("HandPos", (int)HandPoses.LightAim);
            return;
        }
        
        SetHandsStateNull();
        CancelInvoke(nameof(SetSpirimonzHandPos));
        Invoke(nameof(SetSpirimonzHandPos), 0.25f);
    }

    private void SetSpirimonzHandPos()
    {
        Spirimonz spirimonzToUse = spirimonzTeam[currentSelectedIndex - 1];
        _gamePlayer.handAnimator.SetInteger("HandPos", (int)spirimonzToUse.handPosType);
        
        spirimonzToUse.gameObject.SetActive(true);
        spirimonzToUse.transform.localScale = Vector3.zero;
        spirimonzToUse.transform.DOScale(1, 0.5f).SetEase(Ease.OutBack);
    }

    private void UnequipSpirimonz()
    {
        currentSelectedIndex = -1;
        if (selectedSpirimonz != null && selectedSpirimonz.isOnTheMap == false)
        {
            selectedSpirimonz.gameObject.SetActive(false);
        }
        selectedSpirimonz = null;
    }

    private void TryToDropSpirimonz()
    {
        if (selectedSpirimonz == null) return; //No spirimonz in hands
        if (selectedSpirimonz.canBeDroppedOnMap == false) return;

        Vector3 dropPos = _gamePlayer.interactionController.GetLastGroundPos();
        if (dropPos == Vector3.zero)
        {
            if (_gamePlayer.interactionController.DetectCollisionForward())
            {
                dropPos = this.transform.position;
            }
            else
            {
                Vector3 playerPos = _player.transform.position; // ou _gamePlayer.transform.position
                Vector3 playerForward = _gamePlayer.GetForward().normalized; 
                dropPos = playerPos + new Vector3(playerForward.x, 0, playerForward.z) * 1f; // 1m devant le joueur

            }
        }

        DropSpirimonz(dropPos);
    }

    private void DropSpirimonz(Vector3 dropPos)
    {
        if (selectedSpirimonz == null || _gamePlayer.interactionController.HasTarget()) return; //ERROR, no spirimonz selected or Interaction controller target something (door?)

        if (House.Instance.currentGhost.IsHunting(false)) return; //Can't drop Spirimonz during a hunt
        
        Spirimonz spirimonzToDrop = selectedSpirimonz;
        if (spirimonzToDrop.IsInHidingMode()) return;
        
        _gamePlayer.handAnimator.SetInteger("HandPos", (int)HandPoses.LightAim);
        spirimonzToDrop.transform.parent = House.Instance.transform;
        spirimonzToDrop.ChangeLayer(spirimonzMask, 0);

        if (spirimonzToDrop.lookForwardOnDropOnMap)
        {
            spirimonzToDrop.transform.DORotate(transform.localEulerAngles, 0.5f, RotateMode.Fast);
        }
        else
        {
            Vector3 oppositeRotation = transform.localEulerAngles;
            oppositeRotation.y += 180f;
            spirimonzToDrop.transform.DORotate(oppositeRotation, 0.5f, RotateMode.Fast);
        }

        spirimonzToDrop.DroppingOnMap();
        float camX = _gamePlayer.camera.transform.localEulerAngles.x;

        // Mapper de 0-360 à 0-180 pour regarder vers le bas
        if (camX > 180f) camX -= 360f; // [-180,180]

        // Clamp entre 0 et 80 pour éviter les valeurs négatives ou trop grandes
        camX = Mathf.Clamp(camX, 0f, 80f);

        // Normalisation : 0 -> maxJump, 80 -> minJump (0)
        float t = camX / 80f; // 0..1
        float maxJump = 1.5f;
        float minJump = 0f;
        float jumpPower = Mathf.Lerp(maxJump, minJump, t) * spirimonzToDrop.jumpForceMultiplier;
        
        // JumpDuration (vitesse) : 0 = lent, 1 = rapide
        float minDuration = 1f;  // regarde horizontale = lent
        float maxDuration = 0.5f; // regarde vers le bas = rapide
        float jumpDuration = Mathf.Lerp(minDuration, maxDuration, t); // descend avec t

        spirimonzToDrop.transform.DOJump(dropPos, jumpPower, 1, jumpDuration)
            .OnComplete(() =>
            {
                spirimonzToDrop.EnableSpirimonz(true);
                spirimonzToDrop.DroppedOnMap();
            });
        selectedSpirimonz = null;
    }

    public void SpirimonzGoBackToHands(Spirimonz spirimonz)
    {
        spirimonz.GoBackToHands(_gamePlayer.spirimonzHandPos);

        if (spirimonz.canBeTakenBackIntoHands == false) return;
        
        spirimonz.ChangeLayer(fpsMask, 0);
        
        //Equip spirimonz if player do not use items or other spirimonz
        if(_gamePlayer.handAnimator.GetInteger("HandPos") == (int)HandPoses.Null || _gamePlayer.handAnimator.GetInteger("HandPos") == (int)HandPoses.LightAim)
        {
            currentSelectedIndex = GetSpirimonzIndex(spirimonz) + 1;
            EquipSpirimonz(currentSelectedIndex - 1);
        }
    }

    public int GetSpirimonzIndex(Spirimonz spirimonz)
    {
        for (int i = 0; i < spirimonzTeam.Count; i++)
        {
            if (spirimonzTeam[i] == spirimonz)
                return i;
        }

        Debug.Log("ERROR, NO SPIRIMONZ ID FOUND for: ", spirimonz);
        return -1;
    }

    public void ReplaceSpirimonzByAnItem()
    {
        if(selectedSpirimonz != null)
            UnequipSpirimonz();
    }

    public bool OccupedHands()
    {
        if (_gamePlayer.handAnimator.GetInteger("HandPos") != (int)HandPoses.Null &&
            _gamePlayer.handAnimator.GetInteger("HandPos") != (int)HandPoses.LightAim)
            return true;

        if (_gamePlayer.interactionController.objectInHands != null)
            return true;

        return false;
    }

    public void SetHandsStateNull()
    {
        _gamePlayer.handAnimator.SetInteger("HandPos", (int)HandPoses.Null);
    }

    public bool IsSpirimonzInTeam(SpirimonzSettings spmz)
    {
        foreach (SpirimonzSettings ss in spirimonzTeamSettings)
        {
            if (spmz == ss)
                return true;
        }

        return false;
    }
}
