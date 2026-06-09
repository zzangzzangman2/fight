using UnityEngine;

namespace JoseonMurimTactics
{
    /// <summary>
    /// 전투 종료 결과를 스토리 시스템으로 전달한다. 전투 씬은 종료 시 SetResult를 호출하고
    /// SceneFlow가 BattleResult 씬을 연다. BattleResult 컨트롤러가 LastResult를 읽어 표시/정산한다.
    /// </summary>
    public static class BattleResultBridge
    {
        public static string CurrentBattleId { get; private set; }
        public static BattleResultData LastResult { get; private set; }

        public static void BeginBattle(string battleId)
        {
            CurrentBattleId = battleId;
            LastResult = null;
        }

        public static void SetResult(BattleResultData result)
        {
            LastResult = result;
            if (result != null)
            {
                Debug.Log($"[BattleResultBridge] Result for {result.battleId}: {result.outcome}");
            }
        }

        public static bool HasResult => LastResult != null;

        public static void Clear()
        {
            LastResult = null;
        }
    }
}
