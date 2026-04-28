public static class LoadScenePolicyPreview
{
#if UNITY_EDITOR
    private const string PendingPoliciesKey = "LoadScenePolicyPreview.PendingPolicies";

    public static void RequestPoliciesOnNextTitleScreen()
    {
        UnityEditor.SessionState.SetBool(PendingPoliciesKey, true);
    }

    public static bool ConsumePendingPoliciesRequest()
    {
        bool pending = UnityEditor.SessionState.GetBool(PendingPoliciesKey, false);
        if (pending)
            UnityEditor.SessionState.SetBool(PendingPoliciesKey, false);

        return pending;
    }
#else
    public static void RequestPoliciesOnNextTitleScreen()
    {
    }

    public static bool ConsumePendingPoliciesRequest()
    {
        return false;
    }
#endif
}
