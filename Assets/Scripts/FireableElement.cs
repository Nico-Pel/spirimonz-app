using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireableElement : GameBehaviour
{
    public bool startOnFire;
    public bool turnOffOnThrow;
    
    public GameObject[] fireObjects;
    public ParticleSystem[] fireOffParticles;

    public bool canBeTurnedOn = true;

    private bool _isOnFire;

    private void Start()
    {
        EnableFire(startOnFire, false);
    }

    private void TurnOffFire()
    {
        EnableFire(false);
    }

    public void EnableFire(bool enable, bool useParticlesOff = true)
    {
        if (enable == true && canBeTurnedOn == false) return;
        
        _isOnFire = enable;
        
        foreach (GameObject fire in fireObjects)
        {
            fire.SetActive(enable);
        }

        if (enable == false && useParticlesOff && fireOffParticles.Length > 0)
        {
            foreach (ParticleSystem particles in fireOffParticles)
            {
                if (particles != null)
                {
                    particles.Play();
                }
            }
        }
    }
    
    public bool IsOnFire()
    {
        return _isOnFire;
    }
}