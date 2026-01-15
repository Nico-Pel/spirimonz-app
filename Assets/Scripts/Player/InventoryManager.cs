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
        HoldTwoHands
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

    private void Awake()
    {
        InitializeTeam();
    }

    private void InitializeTeam()
    {
        foreach (Spirimonz spmz in spirimonzTeamPrefabs)
        {
            if (spmz == null) continue;
            
            Spirimonz newSpirimonz = Instantiate(spmz, spirimonzHandPos);
            newSpirimonz.EnableSpirimonz(false);
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
            selectedSpirimonz.currentRoom = Player.Instance.currentRoom;
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
            if (Player.Instance.fpsController.mLight.gameObject.activeInHierarchy == true)
            {
                Player.Instance.fpsController.ForceLightState(false);
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
                        Player.Instance.fpsController.ForceLightState(true);
                        _forcedLightStateDuringCam = false;
                    }
                });
            }
            handAnimator.SetInteger("HandPos", (int)HandPoses.LightAim);
        }
    }

    private void UseWatchObject()
    {
        if (Player.Instance.interactionController.objectInHands) return;
        
        handAnimator.SetInteger("HandPos", (int)HandPoses.LightAim);
        UnequipSpirimonz();
    }

    private void EquipSpirimonz(int teamIndex)
    {
        //You can't select a Spirimonz if an object in hands
        if (Player.Instance.interactionController.objectInHands != null) return;
        
        if (selectedSpirimonz != null && selectedSpirimonz.isOnTheMap == false)
        {
            selectedSpirimonz.gameObject.SetActive(false);
        }

        if (spirimonzTeam[teamIndex] == null) return;
        
        selectedSpirimonz = spirimonzTeam[teamIndex];
        
        if (selectedSpirimonz.isOnTheMap)
        {
            handAnimator.SetInteger("HandPos", (int)HandPoses.Null);
            return;
        }
        
        selectedSpirimonz.gameObject.SetActive(true);
        handAnimator.SetInteger("HandPos", (int)selectedSpirimonz.handPosType);
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

        Vector3 dropPos = Player.Instance.interactionController.GetLastGroundPos();
        if (dropPos == Vector3.zero) return; //No Ground detected

        DropSpirimonz(dropPos);
    }

    private void DropSpirimonz(Vector3 dropPos)
    {
        if (selectedSpirimonz == null) return; //ERROR, no spirimonz selected
        
        handAnimator.SetInteger("HandPos", (int)HandPoses.Null);
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

        spirimonzToDrop.transform.DOJump(dropPos, 1, 1, 0.75f).OnComplete(() =>
        {
            spirimonzToDrop.EnableSpirimonz(true);
            spirimonzToDrop.DroppedOnMap();
        });
        selectedSpirimonz = null;
    }

    public void SpirimonzGoBackToHands(Spirimonz spirimonz)
    {
        spirimonz.GoBackToHands(spirimonzHandPos);
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

        if (Player.Instance.interactionController.objectInHands != null)
            return true;

        return false;
    }

    public void SetHandsStateNull()
    {
        handAnimator.SetInteger("HandPos", (int)HandPoses.Null);
    }
}
