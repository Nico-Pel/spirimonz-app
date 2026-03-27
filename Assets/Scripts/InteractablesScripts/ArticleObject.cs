using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArticleObject : CatchableObject
{
    public Article article;

    [Header("Sound")] 
    public float soundDelay = 0.2f;
    public SoundParameters soundParameters;

    public override void OnGrab()
    {
        base.OnGrab();
        transform.localScale = Vector3.one * 0.01f;
        
        this.Invoke(4, () => Destroy(this.gameObject));
        
        Player player = Player.Instance;
        player.ReceiveArticle(article, true);

        if (soundParameters != null)
        {
            this.Invoke(soundDelay, () =>
            {
                soundParameters.PlaySound(player.transform.position);
            });
        }

        //If the ghost is Totemic: There is a 10% chance that a hunt will begin once there is a talisman in the house it haunts.
        Ghost ghost = House.Instance.currentGhost;
        if (ghost.ghostParameters.ghostTypeData.ghostType == GhostTypeData.GhostType.Totemic)
        {
            ghost.TryToTriggerAHunt(10f, 0.1f, 3f);
        }
    }
}