namespace JoseonMurimTactics
{
public sealed class BattleResultApplyOutcome
{
    public bool applied;
    public bool duplicate;
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

        root.Quests.ResolveBattle(result, definition);
        root.Session.lastBattleResult = result;
        if (result.Won)
        {
            root.Flags.SetFlag(StoryFlags.FirstBattleWon);
            root.Flags.SetFlag(StoryFlags.EnvoyDefeated);
            root.Flags.SetFlag(StoryFlags.HubUnlocked);
        }

        root.Save.Save(root.Session);
        outcome.applied = true;
        return outcome;
    }
}
}
