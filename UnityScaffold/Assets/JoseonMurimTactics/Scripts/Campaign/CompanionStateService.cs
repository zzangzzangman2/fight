using System.Collections.Generic;

namespace JoseonMurimTactics
{
public enum CompanionInjuryLevel
{
    Healthy = 0,
    Light = 1,
    Heavy = 2,
    Unavailable = 3
}

public sealed class CompanionStateService
{
    private readonly GameSession session;

    public CompanionStateService(GameSession session)
    {
        this.session = session;
    }

    public CompanionInjuryLevel InjuryOf(string companionId)
    {
        if (session == null || string.IsNullOrEmpty(companionId))
        {
            return CompanionInjuryLevel.Healthy;
        }

        return session.companionInjury.TryGetValue(companionId, out int level)
                   ? (CompanionInjuryLevel)Clamp(level, 0, (int)CompanionInjuryLevel.Unavailable)
                   : CompanionInjuryLevel.Healthy;
    }

    public int FatigueOf(string companionId)
    {
        if (session == null || string.IsNullOrEmpty(companionId))
        {
            return 0;
        }

        return session.companionFatigue.TryGetValue(companionId, out int value) ? Clamp(value, 0, 100) : 0;
    }

    public bool IsBattleReady(string companionId)
    {
        return InjuryOf(companionId) != CompanionInjuryLevel.Unavailable && FatigueOf(companionId) < 100;
    }

    public void MarkWounded(string companionId)
    {
        if (session == null || string.IsNullOrEmpty(companionId))
        {
            return;
        }

        CompanionInjuryLevel current = InjuryOf(companionId);
        CompanionInjuryLevel next = current == CompanionInjuryLevel.Healthy ? CompanionInjuryLevel.Light
                                                                            : CompanionInjuryLevel.Heavy;
        session.companionInjury[companionId] = (int)next;
    }

    public void SetInjury(string companionId, CompanionInjuryLevel injury)
    {
        if (session == null || string.IsNullOrEmpty(companionId))
        {
            return;
        }

        if (injury <= CompanionInjuryLevel.Healthy)
        {
            session.companionInjury.Remove(companionId);
            return;
        }

        session.companionInjury[companionId] = (int)injury;
    }

    public void AddFatigue(string companionId, int delta)
    {
        if (session == null || string.IsNullOrEmpty(companionId) || delta == 0)
        {
            return;
        }

        int next = Clamp(FatigueOf(companionId) + delta, 0, 100);
        if (next <= 0)
        {
            session.companionFatigue.Remove(companionId);
            return;
        }

        session.companionFatigue[companionId] = next;
    }

    public void Heal(string companionId)
    {
        SetInjury(companionId, CompanionInjuryLevel.Healthy);
        AddFatigue(companionId, -30);
    }

    public void HealAll()
    {
        if (session == null)
        {
            return;
        }

        List<string> ids = InjuredCompanionIds();
        foreach (string id in ids)
        {
            Heal(id);
        }
    }

    public int InjuredCount()
    {
        return InjuredCompanionIds().Count;
    }

    public List<string> InjuredCompanionIds()
    {
        List<string> ids = new List<string>();
        if (session == null)
        {
            return ids;
        }

        foreach (KeyValuePair<string, int> pair in session.companionInjury)
        {
            if (!string.IsNullOrEmpty(pair.Key) && pair.Value > 0)
            {
                ids.Add(pair.Key);
            }
        }

        ids.Sort();
        return ids;
    }

    public static string InjuryLabel(CompanionInjuryLevel injury)
    {
        switch (injury)
        {
        case CompanionInjuryLevel.Light:
            return "경상";
        case CompanionInjuryLevel.Heavy:
            return "중상";
        case CompanionInjuryLevel.Unavailable:
            return "출전 불가";
        default:
            return "출전 가능";
        }
    }

    private static int Clamp(int value, int min, int max)
    {
        if (value < min)
            return min;
        return value > max ? max : value;
    }
}
}
