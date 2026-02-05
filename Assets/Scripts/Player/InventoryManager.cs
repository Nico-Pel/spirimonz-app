using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class InventoryManager : GameBehaviour
{
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
    
    [Header("Keyboard (AZERTY)")] 
    public KeyCode[] inventoryKeys = new KeyCode[6];

    [Header("Team")]
    public Spirimonz[] spirimonzTeamPrefabs = new Spirimonz[5];
    public List<Spirimonz> spirimonzTeam = new List<Spirimonz>();
    [ReadOnly] public Spirimonz selectedSpirimonz = null;
    [ReadOnly] public int currentSelectedIndex;

    [Header("Components")] 
    public Transform spirimonzHandPos;
    public Animator handAnimator;

    [Header("Layer Masks")] 
    public LayerMask fpsMask;
    public LayerMask spirimonzMask;

    private bool _forcedLightStateDuringCam;
    private GamePlayer _player;

    private void Awake()
    {
        InitializeTeam();
    }

    private void Start()
    {
        _player = (GamePlayer)Player.Instance;
        UseWatchObject();
    }

    private void InitializeTeam()
    {
        foreach (Spirimonz spmz in spirimonzTeamPrefabs)
        {
            if (spmz == null) continue;
            
            Spirimonz newSpirimonz = Instantiate(spmz, spirimonzHandPos);
            spirimonzTeam.Add(newSpirimonz);
            newSpirimonz.transform.localPosition = Vector3.zero;
            newSpirimonz.transform.localEulerAngles = Vector3.zero;
            newSpirimonz.ChangeLayer(fpsMask, 0);
        }
    }
    
    void Update()
    {
        if (selectedSpirimonz != null && selectedSpirimonz.isOnTheMap == false)
        {
            selectedSpirimonz.currentRoom = _player.currentRoom;
        }
        
        for (int i = 0; i < inventoryKeys.Length; i++)
        {
            if (Input.GetKeyDown(inventoryKeys[i]))
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
        if (Input.GetMouseButtonDown(1) && handAnimator.GetInteger("HandPos") == (int)HandPoses.LightAim)
        {
            handAnimator.SetInteger("HandPos", (int)HandPoses.CameraAim);
            if (_player.fpsController.mLight.gameObject.activeInHierarchy == true)
            {
                _player.fpsController.ForceLightState(false);
                _forcedLightStateDuringCam = true;
            }
        }
        if (Input.GetMouseButtonUp(1) && handAnimator.GetInteger("HandPos") == (int)HandPoses.CameraAim)
        {
            if(_forcedLightStateDuringCam)
            {this.Invoke(0.25f, () =>
                {
                    if (handAnimator.GetInteger("HandPos") == (int)HandPoses.LightAim)
                    {
                        _player.fpsController.ForceLightState(true);
                        _forcedLightStateDuringCam = false;
                    }
                });
            }
            handAnimator.SetInteger("HandPos", (int)HandPoses.LightAim);
        }
    }

    private void UseWatchObject()
    {
        if (_player.interactionController.objectInHands) return;
        
        handAnimator.SetInteger("HandPos", (int)HandPoses.LightAim);
        UnequipSpirimonz();
    }

    private void EquipSpirimonz(int teamIndex)
    {
        //You can't select a Spirimonz if an object in hands
        if (_player.interactionController.objectInHands != null) return;

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
            handAnimator.SetInteger("HandPos", (int)HandPoses.LightAim);
            return;
        }
        
        SetHandsStateNull();
        CancelInvoke(nameof(SetSpirimonzHandPos));
        Invoke(nameof(SetSpirimonzHandPos), 0.25f);
    }

    private void SetSpirimonzHandPos()
    {
        Spirimonz spirimonzToUse = spirimonzTeam[currentSelectedIndex - 1];
        handAnimator.SetInteger("HandPos", (int)spirimonzToUse.handPosType);
        
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

        Vector3 dropPos = _player.interactionController.GetLastGroundPos();
        if (dropPos == Vector3.zero)
        {
            if (_player.interactionController.DetectCollisionForward())
            {
                dropPos = this.transform.position;
            }
            else
            {
                Vector3 playerForward = _player.GetForward() * 1f;
                dropPos = this.transform.position + new Vector3(playerForward.x, 0, playerForward.z);
            }
        }

        DropSpirimonz(dropPos);
    }

    private void DropSpirimonz(Vector3 dropPos)
    {
        if (selectedSpirimonz == null || _player.interactionController.HasTarget()) return; //ERROR, no spirimonz selected or Interaction controller target something (door?)

        if (House.Instance.currentGhost.IsHunting()) return; //Can't drop Spirimonz during a hunt
        
        handAnimator.SetInteger("HandPos", (int)HandPoses.LightAim);
        Spirimonz spirimonzToDrop = selectedSpirimonz;
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
        float camX = _player.fpsController.playerCamera.transform.localEulerAngles.x;

        // Mapper de 0-360 à 0-180 pour regarder vers le bas
        if (camX > 180f) camX -= 360f; // [-180,180]

        // Clamp entre 0 et 80 pour éviter les valeurs négatives ou trop grandes
        camX = Mathf.Clamp(camX, 0f, 80f);

        // Normalisation : 0 -> maxJump, 80 -> minJump (0)
        float t = camX / 80f; // 0..1
        float maxJump = 2f;
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
        spirimonz.GoBackToHands(spirimonzHandPos);

        if (spirimonz.canBeTakenBackIntoHands == false) return;
        
        spirimonz.ChangeLayer(fpsMask, 0);
        
        //Equip spirimonz if player do not use items or other spirimonz
        if(handAnimator.GetInteger("HandPos") == (int)HandPoses.Null || handAnimator.GetInteger("HandPos") == (int)HandPoses.LightAim)
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
        if (handAnimator.GetInteger("HandPos") != (int)HandPoses.Null &&
            handAnimator.GetInteger("HandPos") != (int)HandPoses.LightAim)
            return true;

        if (_player.interactionController.objectInHands != null)
            return true;

        return false;
    }

    public void SetHandsStateNull()
    {
        handAnimator.SetInteger("HandPos", (int)HandPoses.Null);
    }
}
