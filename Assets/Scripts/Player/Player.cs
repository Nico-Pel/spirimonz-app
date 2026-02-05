using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Player : GameBehaviour
{
    public static Player Instance { get; private set; }

    public InputManager inputManager;
    
    [ReadOnly] private bool lockControls;
    protected bool _isDead;

    private void Awake()
    {
        Instance = this;
    }
    
    public void LockControls(bool enable)
    {
        lockControls = enable;
    }
    
    public bool IsLocked() => lockControls;
    public bool IsDead() => _isDead;
}
