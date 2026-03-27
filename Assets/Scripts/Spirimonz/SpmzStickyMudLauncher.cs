using UnityEngine;

public class SpmzStickyMudLauncher : SpmzUsePower
{
    [Header("Sticky Mud")]
    public StickyMudProjectile[] stickyMudPrefabs;
    public Transform spawnPoint;
    public float launchSpeed = 12f;
    public float upwardSpeed = 0f;
    public float launchCooldown = 0.2f;
    public float launchDelay = 0f;
    public SoundParameters shootSoundParameters;

    [Header("Aim")]
    public bool useCameraAim = true;
    public float maxAimDistance = 25f;
    public LayerMask aimLayers = ~0;

    [Header("Collision")]
    public bool ignoreOwnerColliders = true;

    private Camera _cam;
    private float _nextLaunchTime;
    private bool _pendingShot;
    private const string LAUNCH_INVOKE = "SpmzStickyMudLauncher.Launch";

    public override void InitSpirimonz()
    {
        canBeDroppedOnMap = false;
        powerActiveInHands = true;
        useMode = PowerUseMode.SingleShot;
        base.InitSpirimonz();
    }

    private void Reset()
    {
        useMode = PowerUseMode.SingleShot;
        energyCostPerUse = 33f;
        usingEnergyForSec = 0f;
        rechargeForSec = 0.15f;
        singleShotActiveDuration = 0.15f;
    }

    protected override void OnPowerActivated()
    {
        base.OnPowerActivated();

        if (_pendingShot)
        {
            RestoreEnergy(energyCostPerUse);
            return;
        }

        if (Time.time < _nextLaunchTime)
        {
            RestoreEnergy(energyCostPerUse);
            return;
        }

        _nextLaunchTime = Time.time + Mathf.Max(0f, launchCooldown);

        float delay = Mathf.Max(0f, launchDelay);
        if (delay > 0f)
        {
            _pendingShot = true;
            CancelInvoke(LAUNCH_INVOKE);
            this.Invoke(LAUNCH_INVOKE, delay, LaunchStickyMud);
        }
        else
        {
            LaunchStickyMud();
        }
    }

    private void LaunchStickyMud()
    {
        _pendingShot = false;

        StickyMudProjectile prefabToUse = PickStickyMudPrefab();
        if (prefabToUse == null)
        {
            RestoreEnergy(energyCostPerUse);
            return;
        }

        Transform origin = spawnPoint != null ? spawnPoint : transform;
        Vector3 spawnPos = origin.position;
        Vector3 direction = GetAimDirection(origin);

        Transform parent = House.Instance != null ? House.Instance.transform : null;
        StickyMudProjectile spawned = Instantiate(prefabToUse, spawnPos, Quaternion.LookRotation(direction), parent);
        if (spawned == null)
        {
            RestoreEnergy(energyCostPerUse);
            return;
        }

        spawned.Initialize(this, energyCostPerUse);
        Rigidbody rb = spawned.rb != null ? spawned.rb : spawned.GetComponent<Rigidbody>();
        if (spawned.rb == null)
            spawned.rb = rb;
        if (rb == null)
        {
            RestoreEnergy(energyCostPerUse);
            Destroy(spawned.gameObject);
            return;
        }

        if (ignoreOwnerColliders)
            IgnoreOwnerCollisions(spawned);

        rb.isKinematic = false;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Vector3 velocity = direction * launchSpeed + Vector3.up * upwardSpeed;
        rb.AddForce(velocity, ForceMode.VelocityChange);
        if (shootSoundParameters != null)
            shootSoundParameters.PlaySound(spawnPos);
    }

    private StickyMudProjectile PickStickyMudPrefab()
    {
        if (stickyMudPrefabs == null || stickyMudPrefabs.Length == 0)
            return null;

        int index = Random.Range(0, stickyMudPrefabs.Length);
        return stickyMudPrefabs[index];
    }

    protected override void OnDisable()
    {
        if (_pendingShot)
        {
            CancelInvoke(LAUNCH_INVOKE);
            _pendingShot = false;
            RestoreEnergy(energyCostPerUse);
        }

        base.OnDisable();
    }

    private Vector3 GetAimDirection(Transform origin)
    {
        Vector3 direction = origin.forward;

        if (!useCameraAim)
            return direction.normalized;

        if (_cam == null)
        {
            Player player = Player.Instance;
            if (player != null && player.camera != null)
                _cam = player.camera;
            else
                _cam = Camera.main;
        }

        if (_cam == null)
            return direction.normalized;

        Vector3 camPos = _cam.transform.position;
        Vector3 camForward = _cam.transform.forward;

        if (Physics.Raycast(camPos, camForward, out RaycastHit hit, maxAimDistance, aimLayers, QueryTriggerInteraction.Ignore))
            direction = (hit.point - origin.position).normalized;
        else
            direction = camForward;

        return direction.normalized;
    }

    private void IgnoreOwnerCollisions(StickyMudProjectile spawned)
    {
        Collider[] spawnedCols = spawned.GetComponentsInChildren<Collider>();
        if (spawnedCols == null || spawnedCols.Length == 0)
            return;

        Player player = Player.Instance;
        if (player != null)
        {
            Collider[] playerCols = player.GetComponentsInChildren<Collider>();
            foreach (Collider pCol in playerCols)
            {
                if (pCol == null) continue;
                foreach (Collider sCol in spawnedCols)
                {
                    if (sCol == null) continue;
                    Physics.IgnoreCollision(sCol, pCol, true);
                }
            }
        }

        Collider[] ownerCols = GetComponentsInChildren<Collider>();
        foreach (Collider oCol in ownerCols)
        {
            if (oCol == null) continue;
            foreach (Collider sCol in spawnedCols)
            {
                if (sCol == null) continue;
                Physics.IgnoreCollision(sCol, oCol, true);
            }
        }
    }
}
