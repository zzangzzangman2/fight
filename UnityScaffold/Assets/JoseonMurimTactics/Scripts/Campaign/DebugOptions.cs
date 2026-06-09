namespace JoseonMurimTactics
{
public sealed class DebugOptions
{
    public bool showBattleTestButton;
    public bool allowDebugSessionFactory;

    public static DebugOptions CreateDefault()
    {
        DebugOptions options = new DebugOptions();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        options.showBattleTestButton = true;
        options.allowDebugSessionFactory = true;
#endif
        return options;
    }
}

public static class DebugSessionFactory
{
    public static GameSession CreateBattleTestSession()
    {
        GameSession session = new GameSession();
        session.currentChapterId = "CHAPTER_01";
        session.RecruitCompanion(CompanionCatalog.BaekRyeon);
        session.RecruitCompanion(CompanionCatalog.DoArin);
        session.RecruitCompanion(CompanionCatalog.JinSeoyul);
        session.RecruitCompanion(CompanionCatalog.SeoA);
        session.RecruitCompanion(CompanionCatalog.HanBiyeon);
        session.intVars["silver"] = 120;
        session.storyFlags.Add(StoryFlags.Chapter1Started);
        session.storyFlags.Add(StoryFlags.HubUnlocked);
        return session;
    }
}
}
