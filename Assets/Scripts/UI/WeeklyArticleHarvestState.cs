using System;
using UnityEngine;
using YsoCorp;

public static class WeeklyArticleHarvestState
{
    private const string WeekStartKey = "weekly_article_harvest_week_start";
    private const string FoundPrefix = "weekly_article_harvest_found_";
    private const string ClaimedPrefix = "weekly_article_harvest_claimed_";

    public static event Action OnStateChanged;

    public static void RegisterFound(Article article)
    {
        if (!IsTrackable(article))
            return;

        string key = GetFoundKey(article);
        if (ADataManager.GetBool(key))
            return;

        ADataManager.SetBool(key, true);
        ADataManager.ForceSave();
        OnStateChanged?.Invoke();
    }

    public static bool IsFoundThisWeek(Article article)
    {
        if (!IsTrackable(article))
            return false;

        return ADataManager.GetBool(GetFoundKey(article));
    }

    public static bool IsClaimedThisWeek(Article article)
    {
        if (!IsTrackable(article))
            return false;

        return ADataManager.GetBool(GetClaimedKey(article));
    }

    public static bool CanClaim(Article article)
    {
        return IsFoundThisWeek(article) && !IsClaimedThisWeek(article);
    }

    public static int Claim(Article article, bool doubled)
    {
        if (!CanClaim(article))
            return 0;

        int amount = article.GetWeeklyHarvestReward();
        if (doubled)
            amount *= 2;

        ADataManager.SetBool(GetClaimedKey(article), true);
        ADataManager.ForceSave();
        OnStateChanged?.Invoke();
        return Mathf.Max(0, amount);
    }

    public static DateTime GetNextResetUtc()
    {
        return GetCurrentWeekStartUtc().AddDays(7);
    }

    public static void EnsureCurrentWeek()
    {
        string currentWeekStart = GetCurrentWeekStartKey();
        string savedWeekStart = ADataManager.GetString(WeekStartKey, string.Empty);
        if (savedWeekStart == currentWeekStart)
            return;

        ADataManager.SetString(WeekStartKey, currentWeekStart);
        ADataManager.ForceSave();
        OnStateChanged?.Invoke();
    }

    private static bool IsTrackable(Article article)
    {
        EnsureCurrentWeek();
        return article != null && article.GetWeeklyHarvestReward() > 0;
    }

    private static string GetFoundKey(Article article)
    {
        return $"{FoundPrefix}{GetCurrentWeekStartKey()}_{article.name}";
    }

    private static string GetClaimedKey(Article article)
    {
        return $"{ClaimedPrefix}{GetCurrentWeekStartKey()}_{article.name}";
    }

    private static string GetCurrentWeekStartKey()
    {
        return GetCurrentWeekStartUtc().ToString("yyyyMMdd");
    }

    private static DateTime GetCurrentWeekStartUtc()
    {
        DateTime now = DateTime.UtcNow;
        int diff = ((int)now.DayOfWeek + 6) % 7;
        return now.Date.AddDays(-diff);
    }
}
