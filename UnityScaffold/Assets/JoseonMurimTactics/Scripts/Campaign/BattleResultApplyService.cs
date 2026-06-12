namespace JoseonMurimTactics
{
public sealed class BattleResultApplyOutcome
{
    public bool applied;
    public bool duplicate;
    public bool replayRewardsReduced;
    public bool autosaveSucceeded;
    public string resultId;
}

public sealed class BattleResultApplyService
{
    private readonly GameRoot root;

    public BattleResultApplyService(GameRoot root)
    {
        this.root = root;
    }

    public BattleResultApplyOutcome Apply(BattleResultData result, BattleDefinition definition)
    {
        BattleResultApplyOutcome outcome = new BattleResultApplyOutcome();
        if (root == null || result == null)
        {
            return outcome;
        }

        string id = result.EnsureResultId();
        outcome.resultId = id;

        if (root.Session.appliedBattleResultIds.Contains(id))
        {
            outcome.duplicate = true;
            root.Session.lastBattleResult = result;
            return outcome;
        }

        root.Session.appliedBattleResultIds.Add(id);

        bool alreadyCompletedMission = result.Won && definition != null && !definition.repeatable &&
                                       !string.IsNullOrEmpty(definition.questId) &&
                                       root.Session.completedMissionIds.Contains(definition.questId);
        outcome.replayRewardsReduced = alreadyCompletedMission;

        if (!alreadyCompletedMission)
        {
            foreach (BattleResultData.StatDelta faction in result.factionChanges)
            {
                root.Reputation.Add(faction.id, faction.delta);
            }

            foreach (BattleResultData.StatDelta approval in result.approvalChanges)
            {
                root.Approval.Add(approval.id, approval.delta);
            }

            if (result.silver != 0)
            {
                root.Flags.AddInt("silver", result.silver);
            }

            foreach (string item in result.rewardItems)
            {
                root.Inventory.AddItemFromDisplayName(item);
            }
        }
        else if (result.silver > 0)
        {
            root.Flags.AddInt("silver", System.Math.Max(1, result.silver / 4));
        }

        if (result.Won && definition != null && IsVillageFreeTimeBattle(definition.id))
        {
            root.Flags.AddInt("sect:village_trust", 1);
        }

        foreach (string companionId in result.woundedCompanions)
        {
            root.CompanionStates.MarkWounded(companionId);
        }

        root.Quests.ResolveBattle(result, definition);

        // Long campaign progression add-on: Lv50, 경지, 무공 숙련, 동료 유대, 세력 정복도.
        // 은전/아이템/평판은 위에서 이미 지급했으므로 성장 계열 보상만 처리한다.
        ProgressionBattleRewardBridge.Apply(root, result, definition, alreadyCompletedMission);

        if (definition != null && !string.IsNullOrEmpty(definition.questId))
        {
            root.Session.missionAttempts[definition.questId] =
                root.Session.missionAttempts.TryGetValue(definition.questId, out int attempts) ? attempts + 1 : 1;
            if (result.Won && !definition.repeatable)
            {
                root.Session.completedMissionIds.Add(definition.questId);
            }
        }

        root.Session.lastBattleResult = result;
        if (result.Won)
        {
            root.Flags.SetFlag(StoryFlags.FirstBattleWon);
            root.Flags.SetFlag(StoryFlags.EnvoyDefeated);
            root.Flags.SetFlag(StoryFlags.HubUnlocked);
            root.Session.unlockedCodexEntryIds.Add("lore_black_mark");

            if (definition != null && definition.id == HubController.FirstBattleId)
            {
                root.Flags.SetFlag(StoryFlags.Chapter1FirstDefenseCleared);
                root.Flags.SetFlag(StoryFlags.Chapter1SeorakRequestUnlocked);
            }

            if (definition != null && definition.id == HubController.SeorakPassRescueBattleId)
            {
                root.Flags.SetFlag(StoryFlags.Chapter1MetBaekRyeon);
                root.Flags.SetFlag(StoryFlags.Chapter1RescuedSeorakCart);
                root.Flags.SetFlag(StoryFlags.Chapter1InvitedToSeorakSpearHall);
                root.Flags.SetFlag(StoryFlags.Chapter1SeorakPactStarted);
                root.Flags.SetFlag(StoryFlags.CompanionBaekRyeonTempJoined);
                root.Flags.SetFlag(StoryFlags.AllianceSeorakScoutSupport);
                root.Flags.SetFlag(StoryFlags.BaekRyeonRecruited);
                root.Session.RecruitCompanion(CompanionCatalog.BaekRyeon);
            }
        }

        outcome.autosaveSucceeded = root.Save.Save(root.Session);
        if (root.Notifications != null)
        {
            root.Notifications.PushAutosave(outcome.autosaveSucceeded);
        }

        outcome.applied = true;
        return outcome;
    }

    private static bool IsVillageFreeTimeBattle(string battleId)
    {
        return battleId == HubController.BanditLairBattleId ||
               battleId == HubController.WolfPassBattleId ||
               battleId == HubController.TigerRavineBattleId ||
               battleId == HubController.LeopardCliffBattleId;
    }
}
}
