using System;
using UnityEngine;

public class ActivableObject : MonoBehaviour
{
    public bool isActivated;
    public bool isLocked;

    private void Start()
    {
        if (isActivated)
        {
            Activate();
        }
    }

    public void Operate()
    {
        if (!isActivated)
        {
            Activate();
        }
        else
        {
            Deactivate();
        }
    }

    public virtual void Activate()
    {
        if (isLocked) return;

        isActivated = true;
    }

    public virtual void Deactivate()
    {
        isActivated = false;
    }
}