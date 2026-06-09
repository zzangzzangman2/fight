using System;
using System.Collections.Generic;

namespace JoseonMurimTactics
{
public enum BattleOutcome
{
    Undecided,
    Victory,
    Defeat
}

/// <summary>
/// 전투 종료 시 스토리 시스템으로 넘어오는 결과. BattleResultBridge가 채우고
/// BattleResult 씬과 IAINarrationService가 읽는다.
/// </summary>
[Serializable]
public sealed class BattleResultData
{
    public string battleId;
    public BattleOutcome outcome = BattleOutcome.Undecided;
    public string defeatedBoss;
    public int turnCount;

    // 보조 목표 달성 여부 (보조목표 id -> 달성).
    public List<string> completedObjectives = new List<string>();
    public List<string> failedObjectives = new List<string>();

    // 부상/전투불능 동료 id.
    public List<string> woundedCompanions = new List<string>();

    // 보상.
    public int silver;
    public List<string> rewardItems = new List<string>();

    // 평판/승인도/위명 변화 (id -> delta). 표시용.
    public List<StatDelta> factionChanges = new List<StatDelta>();
    public List<StatDelta> approvalChanges = new List<StatDelta>();

    public List<string> specialFlags = new List<string>();

    public bool Won => outcome == BattleOutcome.Victory;

    [Serializable]
    public struct StatDelta
    {
        public string id;
        public int delta;

        public StatDelta(string id, int delta)
        {
            this.id = id;
            this.delta = delta;
        }
    }
}
}
