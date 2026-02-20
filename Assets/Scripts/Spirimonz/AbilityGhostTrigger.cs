using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
public class AbilityGhostTrigger : GameBehaviour
{
    public Spirimonz linkedSpirimonz;
    
    [Space]
    
    public float triggerCooldown = 3f;
    public UnityEvent onGhostTriggered;

    private bool _canTriggerGhost = true;
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Ghost ghost))
        {
            TriggerGhost();
        }
    }

    protected virtual void TriggerGhost()
    {
        if (_canTriggerGhost == false) return;

        if (linkedSpirimonz != null && linkedSpirimonz.isOnTheMap == false && linkedSpirimonz.powerActiveInHands == false) return;
        
        if (triggerCooldown > 0)
        {
            _canTriggerGhost = false;
            this.Invoke(triggerCooldown, () => _canTriggerGhost = true);
        }
        
        onGhostTriggered?.Invoke();
    }
}
