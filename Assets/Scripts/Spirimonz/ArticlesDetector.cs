using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArticlesDetector : GameBehaviour
{
    public Spirimonz linkedSpirimonz;
    [Min(0.1f)] public float detectionRadius = 3f;
    [Min(0.05f)] public float detectionInterval = 0.5f;
    [Min(0.1f)] public float revealDuration = 10f;
    public GameObject revealPrefab;
    public Vector3 revealLocalOffset = new Vector3(0f, 0.15f, 0f);

    private readonly Dictionary<ArticleObject, GameObject> _activeReveals = new Dictionary<ArticleObject, GameObject>();
    private float _nextDetectionTime;
    private bool _detectionEnabled;

    public void SetDetectionEnabled(bool enable)
    {
        _detectionEnabled = enable;
        if (enable)
            _nextDetectionTime = Time.time;
        else
            ClearAllReveals();
    }

    private void Update()
    {
        if (!_detectionEnabled || revealPrefab == null)
            return;

        if (Time.time < _nextDetectionTime)
            return;

        _nextDetectionTime = Time.time + Mathf.Max(0.05f, detectionInterval);

        ArticleObject[] articles = FindObjectsByType<ArticleObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < articles.Length; i++)
        {
            ArticleObject article = articles[i];
            if (article == null)
                continue;

            if (article.isGrabbed)
            {
                RemoveReveal(article);
                continue;
            }

            if (Vector3.Distance(transform.position, article.transform.position) > detectionRadius)
                continue;

            TryRevealArticle(article);
        }
    }

    private void TryRevealArticle(ArticleObject article)
    {
        if (_activeReveals.TryGetValue(article, out GameObject existingReveal))
        {
            if (existingReveal != null)
                return;

            _activeReveals.Remove(article);
        }

        GameObject instance = Instantiate(revealPrefab, article.transform);
        instance.transform.localPosition = revealLocalOffset;
        instance.transform.localRotation = Quaternion.identity;
        _activeReveals[article] = instance;
        StartCoroutine(RemoveRevealAfterDelay(article, instance, revealDuration));
    }

    private IEnumerator RemoveRevealAfterDelay(ArticleObject article, GameObject revealInstance, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (_activeReveals.TryGetValue(article, out GameObject currentReveal) && currentReveal == revealInstance)
            _activeReveals.Remove(article);

        if (revealInstance != null)
            Destroy(revealInstance);
    }

    private void RemoveReveal(ArticleObject article)
    {
        if (article == null)
            return;

        if (!_activeReveals.TryGetValue(article, out GameObject revealInstance))
            return;

        _activeReveals.Remove(article);

        if (revealInstance != null)
            Destroy(revealInstance);
    }

    private void ClearAllReveals()
    {
        if (_activeReveals.Count == 0)
            return;

        foreach (KeyValuePair<ArticleObject, GameObject> pair in _activeReveals)
        {
            if (pair.Value != null)
                Destroy(pair.Value);
        }

        _activeReveals.Clear();
    }

    private void OnDisable()
    {
        _detectionEnabled = false;
        ClearAllReveals();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.9f, 0.3f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
