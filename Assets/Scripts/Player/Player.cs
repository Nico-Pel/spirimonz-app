using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Player : GameBehaviour
{
    public static Player Instance { get; private set; }

    public CharacterController characterController;
    public InputManager inputManager;
    
    [Space]
    
    [ReadOnly] private bool lockControls;
    protected bool _isDead;

    private void Awake()
    {
        Instance = this;
        
        if(GameManager.Instance != null)
            GameManager.Instance.player = this;
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
        transform.position = newPos;
        characterController.enabled = true;
    }
    
    public void SetRotation(Quaternion newRot)
    {
        transform.rotation = newRot;
    }
}
