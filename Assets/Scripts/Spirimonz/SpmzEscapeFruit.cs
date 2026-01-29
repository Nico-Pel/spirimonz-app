using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpmzEscapeFruit : Spirimonz
{
    public Fruit fruitPrefab;
    public float spawnFruitForwardOffset = 0.5f;
    public float spawnFruitUpOffset = 0.5f;
    
    public override void EscapePointReached()
    {
        base.EscapePointReached();
        
        //Wait
        SwitchBehaviour();

        baseBehaviour = SpirimonzBehaviourState.FollowPlayer;

        Vector3 spawnPos = transform.position + transform.forward * spawnFruitForwardOffset + Vector3.up * spawnFruitUpOffset;
        Fruit newFruit = Instantiate(fruitPrefab, spawnPos, Quaternion.identity, House.Instance.transform);

        if (animator != null)
        {
            animator.SetTrigger("DropFruit");
        }
        
        this.Invoke(1f, () =>
        {
            //Now can look at player while waiting, and not during the fruit drop
            //lookAtPlayerWhileWaiting = true;
            
            //Follow player
            SwitchBehaviour();
        });
    }

    public override void InteractionStarted()
    {
        //Ignore player while Escape point is not reached
        if (baseBehaviour == SpirimonzBehaviourState.Escape) return;
        
        base.InteractionStarted();
    }
}
