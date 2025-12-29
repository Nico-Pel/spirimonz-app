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

    public bool CanSeePlayer(Player player)
    {
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

        return playerWasSeen;
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

        // Point de départ et point d'arrivée
        Vector3 origin = ghostHead.position;
        Vector3 target = player.head.position + Vector3.up * 1.0f; // vise la tête
        Vector3 direction = target - origin;
        float distance = direction.magnitude;
        direction.Normalize();

        // Raycast vers le joueur
        if (Physics.Raycast(origin, direction, out RaycastHit hit, distance, obstacleMask | playerMask))
        {
            // Vérifie si on a touché le joueur
            if (((1 << hit.collider.gameObject.layer) & playerMask) != 0)
            {
                return true;
            }
        }

        return false;
    }
}