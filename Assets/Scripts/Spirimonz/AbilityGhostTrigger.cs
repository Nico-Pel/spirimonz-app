using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class GhostUnityEvent : UnityEvent<Ghost> { }

public class AbilityGhostTrigger : GameBehaviour
{
    public Spirimonz linkedSpirimonz;
    
    [Space]
    
    public float triggerCooldown = 3f;
    public bool triggerOnlyIfGhostTouchesGround = false;
    public UnityEvent onGhostTriggered;
    public GhostUnityEvent onGhostTriggeredWithGhost;

    private bool _canTriggerGhost = true;
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Ghost ghost))
        {
            TriggerGhost(ghost);
        }
    }

    protected virtual void TriggerGhost(Ghost ghost = null)
    {
        if (_canTriggerGhost == false) return;
        if (ghost != null && ghost.levitates && triggerOnlyIfGhostTouchesGround) return;

        if (linkedSpirimonz != null && linkedSpirimonz.isOnTheMap == false && linkedSpirimonz.powerActiveInHands == false) return;
        
        if (triggerCooldown > 0)
        {
            _canTriggerGhost = false;
            this.Invoke(triggerCooldown, () => _canTriggerGhost = true);
        }
        
        onGhostTriggered?.Invoke();
        onGhostTriggeredWithGhost?.Invoke(ghost);
    }
}
