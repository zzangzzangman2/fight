using UnityEngine;

namespace JoseonMurimTactics
{
/// <summary>
/// Story/Campaign에서 전투 씬으로 넘어갈 때 필요한 정보를 전달한다.
/// v0.8에서는 기존 BattleTest 씬을 재사용하며, 어떤 전투인지(battleId)만 정적으로 넘긴다.
/// 나중에 정식 BattleScene으로 교체할 수 있도록 진입점을 한 곳에 모아둔다.
/// </summary>
public static class BattleEntryAdapter
{
    /// <summary>다음에 진입할 전투 id. BattlePrep/출격에서 설정하고 전투 씬이 읽는다.</summary>
    public static string PendingBattleId { get; private set; }

    public static void SetPendingBattle(string battleId)
    {
        PendingBattleId = battleId;
        Debug.Log("[BattleEntryAdapter] Pending battle = " + (battleId ?? "(none)"));
    }

    public static void Clear()
    {
        PendingBattleId = null;
    }
}
}
