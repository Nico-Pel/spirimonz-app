using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Player : GameBehaviour
{
    public static Player Instance { get; private set; }

    public CharacterController characterController;
    [ReadOnly] public InputManager inputManager;
    [ReadOnly] public InventoryManager inventoryManager;
    
    [Space]
    
    [ReadOnly] private bool lockControls;
    protected bool _isDead;

    private void Awake()
    {
        Instance = this;
        
        if(GameManager.Instance != null)
            GameManager.Instance.player = this;
    }

    protected virtual void Start()
    {
        inventoryManager = InventoryManager.Instance;
        inputManager = InputManager.Instance;
    }

    public void LockControls(bool enable)
    {
        lockControls = enable;
    }
    
    public bool IsLocked() => lockControls;
    public bool IsDead() => _isDead;

    public void SetPosition(Vector3 newPos)
    {
        if (characterController == null) return;
        
        characterController.enabled = false;
        characterController.transform.position = newPos;
        characterController.enabled = true;
    }
    
    public void SetRotation(Quaternion newRot)
    {
        //characterController.enabled = false;
        characterController.transform.rotation = newRot;
        //characterController.enabled = true;
    }

    public Vector3 GetPosition()
    {
        return characterController.transform.position;
    }

    public Quaternion GetRotation()
    {
        return characterController.transform.rotation;
    }
}
