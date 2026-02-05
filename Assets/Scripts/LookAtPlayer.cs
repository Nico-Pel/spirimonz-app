using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class LookAtPlayer : GameBehaviour
{
    public LookAtConstraint lookAtConstraint;
    
    void Start()
    {
        if(lookAtConstraint != null)
            Initialize();
    }

    private void Initialize()
    {
        ConstraintSource cs = lookAtConstraint.GetSource(0);
        cs.sourceTransform = Camera.main.transform;
        lookAtConstraint.SetSource(0, cs);
    }
}