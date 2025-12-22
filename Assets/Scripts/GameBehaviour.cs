using System;
using System.Collections;
using UnityEngine;

public class GameBehaviour : MonoBehaviour
{
    /// <summary>
    /// Exécute une action après un délai en secondes
    /// </summary>
    public void Invoke(float delay, Action action)
    {
        StartCoroutine(InvokeCoroutine(delay, action));
    }

    private IEnumerator InvokeCoroutine(float delay, Action action)
    {
        yield return new WaitForSeconds(delay);
        action?.Invoke();
    }
}