using System;
using UnityEngine;

namespace JoseonMurimTactics
{
[Serializable]
public sealed class CombatNarrationResult
{
    public string narration;
    public string enemyLine;
    public string companionLine;
    public string intentTag;
    public TimelineCue timelineCue;
    public string effectCue;
}

public interface ICombatNarrationClient
{
    CombatNarrationResult DescribeSkill(CombatantRuntime actor, SkillData skill, CombatantRuntime target);
}

public sealed class GeminiNarrationClientMock : MonoBehaviour, ICombatNarrationClient
{
    public CombatNarrationResult DescribeSkill(CombatantRuntime actor, SkillData skill, CombatantRuntime target)
    {
        CombatNarrationResult result = new CombatNarrationResult();
        string targetName = target == null ? "전장" : target.DisplayName;
        result.narration = actor.DisplayName + "의 " + skill.displayName + "이 " + targetName + "의 흐름을 흔든다.";
        result.enemyLine = "그 초식, 기록해 둘 만하군.";
        result.companionLine = "지금 흐름을 잡았습니다.";
        result.intentTag = "flavor_only";
        result.timelineCue = skill.timelineCue;
        result.effectCue = "MockImpact";
        return result;
    }
}
}
