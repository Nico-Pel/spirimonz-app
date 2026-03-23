using System;
using System.Collections;
using UnityEngine;

public class GhostVision : GameBehaviour
{
    public Transform ghostHead;  // Position de la tête du fantôme
    public LayerMask obstacleMask; // Masque pour les obstacles
    public LayerMask playerMask;   // Masque pour le joueur

    public float losePlayerDelay = 2f;
    
    public bool playerWasSeen = false;

    private Coroutine losePlayerCoroutine;
    private GamePlayer _player;
    private int _lastQueryFrame = -1;
    private Player _lastQueriedPlayer;
    private bool _lastCanSeeResult;

    private void Start()
    {
        _player = (GamePlayer)Player.Instance;
    }

    public bool CanSeePlayer(Player player)
    {
        if (_lastQueryFrame == Time.frameCount && _lastQueriedPlayer == player)
            return _lastCanSeeResult;

        _lastQueryFrame = Time.frameCount;
        _lastQueriedPlayer = player;

        if (HasLineOfSight(player))
        {
            playerWasSeen = true;

            // Stop la coroutine si elle existe
            if (losePlayerCoroutine != null)
            {
                StopCoroutine(losePlayerCoroutine);
                losePlayerCoroutine = null;
            }
        }
        else
        {
            // Lance la coroutine seulement si elle n'existe pas déjà
            if (losePlayerCoroutine == null)
                losePlayerCoroutine = StartCoroutine(LosePlayerAfterDelay());
        }

        _lastCanSeeResult = playerWasSeen;
        return _lastCanSeeResult;
    }

    private IEnumerator LosePlayerAfterDelay()
    {
        yield return new WaitForSeconds(losePlayerDelay);
        playerWasSeen = false;
        losePlayerCoroutine = null;
    }

    private bool HasLineOfSight(Player player)
    {
        if (ghostHead == null || player == null)
            return false;

        Vector3 origin = ghostHead.position;
        Vector3 target = _player.head.position;
        Vector3 direction = target - origin;
        float distance = direction.magnitude;
        direction.Normalize();

        // Raycast
        if (Physics.Raycast(origin, direction, out RaycastHit hit, distance, obstacleMask | playerMask))
        {
            // Si on touche le joueur
            if (((1 << hit.collider.gameObject.layer) & playerMask) != 0)
            {
                Debug.DrawRay(origin, direction * distance, Color.green);
                return true;
            }
            else
            {
                // On touche un obstacle
                Debug.DrawRay(origin, direction * hit.distance, Color.red);
                return false;
            }
            
            Debug.Log($"Hit: {hit.collider.name} | distance: {hit.distance}");
        }

        // Rien touché
        Debug.DrawRay(origin, direction * distance, Color.red);
        return false;
    }
}
