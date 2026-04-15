public static class LocalizationKeys
{
    public static string DialogueNpcName(Dialogue dialogue)
    {
        if (dialogue == null) return string.Empty;
        return $"dialogue.{dialogue.name}.npc_name";
    }

    public static string DialogueLine(Dialogue dialogue, int index)
    {
        if (dialogue == null) return string.Empty;
        return $"dialogue.{dialogue.name}.line_{index}";
    }

    public static string QuestName(Quest quest)
    {
        if (quest == null) return string.Empty;
        return $"quest.{quest.name}.name";
    }

    public static string QuestDescription(Quest quest)
    {
        if (quest == null) return string.Empty;
        return $"quest.{quest.name}.description";
    }

    public static string TutorialObjectiveTitle(TutorialStepSO step)
    {
        if (step == null) return string.Empty;
        string id = string.IsNullOrWhiteSpace(step.id) ? step.name : step.id;
        return $"tutorial.{id}.objective_title";
    }

    public static string SpirimonzName(SpirimonzSettings settings)
    {
        if (settings == null) return string.Empty;
        return $"spirimonz.{settings.spirimonzID}.name";
    }

    public static string SpirimonzAbility(SpirimonzSettings settings, int index)
    {
        if (settings == null) return string.Empty;
        return $"spirimonz.{settings.spirimonzID}.ability_{index}";
    }

    public static string ArticleName(Article article)
    {
        if (article == null) return string.Empty;
        return $"article.{article.name}.name";
    }

    public static string EvidenceTitle(EvidenceParameter evidence)
    {
        if (evidence == null) return string.Empty;
        return $"evidence.{evidence.evidenceType.ToString().ToLowerInvariant()}.title";
    }

    public static string EvidenceInfo(EvidenceParameter evidence)
    {
        if (evidence == null) return string.Empty;
        return $"evidence.{evidence.evidenceType.ToString().ToLowerInvariant()}.info";
    }

    public static string HouseName(HouseMap map)
    {
        if (map == null) return string.Empty;
        return $"house.{map.houseID}.name";
    }

    public static string SecretWorldName(SecretWorld world)
    {
        if (world == null) return string.Empty;
        return $"secret_world.{world.name}.name";
    }
}
