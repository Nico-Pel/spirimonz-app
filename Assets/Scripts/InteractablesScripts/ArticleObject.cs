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
    }
}