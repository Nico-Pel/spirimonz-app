using UnityEngine;

public class SignNPC : NPC
{
    [Header("Sign Defaults")]
    public bool autoConfigureInEditor = true;

    private void Reset()
    {
        ApplySignDefaults();
    }

    private void OnValidate()
    {
        if (Application.isPlaying || !autoConfigureInEditor)
            return;

        ApplySignDefaults();
    }

    private void ApplySignDefaults()
    {
        movingType = MovingType.none;
        useAnimationTalk = false;
        turnBodyToTalk = false;
        mustBeInFront = false;
        maxNeckAngle = 180f;
        lockRotationToY = false;
        useNeckLookAt = false;
        interactionAngleIgnoreDistance = 2f;

        if (neck == null)
            neck = transform;
    }
}
