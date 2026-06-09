using UnityEngine;

namespace JoseonMurimTactics
{
    /// <summary>
    /// 스토리 전투에 GameRoot가 런타임 주입하는 결과 복귀 오버레이. 기존 BattleTest의 OnGUI를
    /// 건드리지 않도록 상단 중앙에만 그린다. v0.8에서는 실제 승패 판정 대신 임시 버튼으로
    /// 결과를 발생시킨다(설계 §14: 임시 버튼/테스트 훅 허용). 이후 전투 종료 이벤트에 연결한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BattleReturnOverlay : MonoBehaviour
    {
        private bool savedDiscipleObjective = true;
        private bool keptAltarObjective = true;
        private bool leaving;

        private void OnGUI()
        {
            if (leaving)
            {
                return;
            }

            UiTheme.EnsureStyles();
            float s = UiTheme.Scale;
            float w = Screen.width;

            float pw = Mathf.Min(560f * s, w - 760f * s); // 좌/우 BattleTest 패널 사이 중앙 폭
            if (pw < 320f * s) pw = 320f * s;
            float x = (w - pw) * 0.5f;
            Rect bar = new Rect(x, 14f * s, pw, 132f * s);
            UiTheme.DrawPanel(bar);

            BattleDefinition def = BattleCatalog.Get(BattleResultBridge.CurrentBattleId);
            GUI.Label(new Rect(bar.x + 16f * s, bar.y + 8f * s, bar.width - 32f * s, 28f * s),
                "전투: " + def.title, UiTheme.Heading);
            GUI.Label(new Rect(bar.x + 16f * s, bar.y + 38f * s, bar.width - 32f * s, 24f * s),
                "v0.8 임시 종료 훅 — 결과 화면 확인용", UiTheme.SmallMuted);

            float ty = bar.y + 66f * s;
            savedDiscipleObjective = GUI.Toggle(new Rect(bar.x + 16f * s, ty, bar.width * 0.5f - 20f * s, 24f * s),
                savedDiscipleObjective, " 제자 구출", UiTheme.Small);
            keptAltarObjective = GUI.Toggle(new Rect(bar.x + bar.width * 0.5f, ty, bar.width * 0.5f - 20f * s, 24f * s),
                keptAltarObjective, " 제단 보존", UiTheme.Small);

            float by = bar.y + 94f * s;
            float bw = (bar.width - 48f * s) * 0.5f;
            if (GUI.Button(new Rect(bar.x + 16f * s, by, bw, 32f * s), "승리로 결과 보기", UiTheme.ButtonPrimary))
            {
                Finish(true);
            }

            if (GUI.Button(new Rect(bar.x + 32f * s + bw, by, bw, 32f * s), "패배로 결과 보기", UiTheme.Button))
            {
                Finish(false);
            }
        }

        private void Finish(bool won)
        {
            leaving = true;
            BattleDefinition def = BattleCatalog.Get(BattleResultBridge.CurrentBattleId);

            BattleResultData result = new BattleResultData
            {
                battleId = def.id,
                outcome = won ? BattleOutcome.Victory : BattleOutcome.Defeat,
                defeatedBoss = def.bossName,
                turnCount = won ? 6 : 10
            };

            if (won)
            {
                result.completedObjectives.Add("OBJ_DEFEAT_WIJIGANG");
                if (savedDiscipleObjective) result.completedObjectives.Add("OBJ_SAVE_DISCIPLE");
                else result.failedObjectives.Add("OBJ_SAVE_DISCIPLE");
                if (keptAltarObjective) result.completedObjectives.Add("OBJ_KEEP_ALTAR");
                else result.failedObjectives.Add("OBJ_KEEP_ALTAR");

                result.silver = def.silverReward;
                result.rewardItems.AddRange(def.rewardItems);
                foreach (IdDelta f in def.factionOnWin) result.factionChanges.Add(new BattleResultData.StatDelta(f.id, f.delta));
                foreach (IdDelta a in def.approvalOnWin) result.approvalChanges.Add(new BattleResultData.StatDelta(a.id, a.delta));
            }
            else
            {
                result.failedObjectives.Add("OBJ_DEFEAT_WIJIGANG");
                result.woundedCompanions.Add(CompanionCatalog.BaekRyeon);
            }

            GameRoot root = GameRoot.EnsureExists();
            root.Flow.GoToBattleResult(result);
        }
    }
}
