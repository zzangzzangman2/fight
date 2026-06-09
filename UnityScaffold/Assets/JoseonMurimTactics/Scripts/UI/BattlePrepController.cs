using System.Collections.Generic;
using UnityEngine;

namespace JoseonMurimTactics
{
    /// <summary>
    /// [6] BattlePrep — 출격 인원 / 승리·패배 조건 / 보조 목표 / 맵 미리보기 / 무공 확인 / 출격.
    /// 출격 버튼은 BattleEntryAdapter를 통해 기존 BattleTest 씬으로 진입한다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BattlePrepController : MonoBehaviour
    {
        private GameRoot root;
        private BattleDefinition def;
        private MissionInfo mission;

        private void Awake()
        {
            root = GameRoot.EnsureExists();
            string id = string.IsNullOrEmpty(BattleEntryAdapter.PendingBattleId)
                ? HubController.FirstBattleId
                : BattleEntryAdapter.PendingBattleId;
            def = BattleCatalog.Get(id);
            mission = FindMission(id);
        }

        private static MissionInfo FindMission(string battleId)
        {
            foreach (MissionInfo m in MissionCatalog.All)
            {
                if (m.battleId == battleId) return m;
            }

            return null;
        }

        private void OnGUI()
        {
            UiTheme.Begin(true);
            float w = Screen.width;
            float h = Screen.height;
            float s = UiTheme.Scale;
            float margin = 44f * s;

            GUI.Label(new Rect(margin, 26f * s, w - margin * 2f, 46f * s), "출격 준비", UiTheme.Title);
            GUI.Label(new Rect(margin, 74f * s, w - margin * 2f, 28f * s), $"{def.title} · {def.location}", UiTheme.BodyCenter);

            float top = 116f * s;
            float bottom = h - 96f * s;
            float colGap = 24f * s;
            float colW = (w - margin * 2f - colGap) * 0.5f;

            // 왼쪽: 조건/목표
            Rect left = new Rect(margin, top, colW, bottom - top);
            UiTheme.DrawPanel(left);
            float lx = left.x + 22f * s;
            float lw = left.width - 44f * s;
            float y = left.y + 18f * s;

            if (mission != null)
            {
                GUI.Label(new Rect(lx, y, lw, 32f * s), "임무 개요", UiTheme.Heading); y += 36f * s;
                Pair(lx, ref y, lw, s, "적 세력", mission.enemyFaction);
                Pair(lx, ref y, lw, s, "추천 레벨", "Lv." + mission.recommendedLevel + " · " + mission.difficulty);
                y += 10f * s;
            }

            GUI.Label(new Rect(lx, y, lw, 32f * s), "승리 조건", UiTheme.Heading); y += 38f * s;
            GUI.Label(new Rect(lx + 10f * s, y, lw - 10f * s, 28f * s), "◎ " + def.victoryCondition, UiTheme.Body); y += 40f * s;

            GUI.Label(new Rect(lx, y, lw, 32f * s), "패배 조건", UiTheme.Heading); y += 38f * s;
            foreach (string c in def.defeatConditions)
            {
                GUI.Label(new Rect(lx + 10f * s, y, lw - 10f * s, 26f * s), "✕ " + c, UiTheme.Body);
                y += 30f * s;
            }
            y += 12f * s;

            GUI.Label(new Rect(lx, y, lw, 32f * s), "보조 목표", UiTheme.Heading); y += 38f * s;
            foreach (BattleObjective o in def.objectives)
            {
                string tag = o.optional ? "○" : "◆";
                GUI.Label(new Rect(lx + 10f * s, y, lw - 10f * s, 26f * s), $"{tag} {o.description}", UiTheme.Small);
                y += 30f * s;
            }

            // 오른쪽: 인원/보정/맵
            Rect right = new Rect(margin + colW + colGap, top, colW, bottom - top);
            UiTheme.DrawPanel(right);
            float rx = right.x + 22f * s;
            float rw = right.width - 44f * s;
            float ry = right.y + 18f * s;
            GUI.Label(new Rect(rx, ry, rw, 32f * s), "출격 인원", UiTheme.Heading); ry += 38f * s;
            foreach (string member in def.roster)
            {
                GUI.Label(new Rect(rx + 10f * s, ry, rw - 10f * s, 28f * s), "• " + member, UiTheme.Body);
                ry += 32f * s;
            }
            ry += 10f * s;

            GUI.Label(new Rect(rx, ry, rw, 32f * s), "예상 보상", UiTheme.Heading); ry += 36f * s;
            if (def.silverReward > 0)
            {
                GUI.Label(new Rect(rx + 10f * s, ry, rw - 10f * s, 26f * s), "· 은전 " + def.silverReward, UiTheme.Small);
                ry += 28f * s;
            }
            List<string> rewards = mission != null && mission.rewardPreview.Count > 0 ? new List<string>(mission.rewardPreview) : def.rewardItems;
            foreach (string item in rewards)
            {
                if (item.StartsWith("은전")) continue;
                GUI.Label(new Rect(rx + 10f * s, ry, rw - 10f * s, 26f * s), "· " + item, UiTheme.Small);
                ry += 28f * s;
            }
            ry += 10f * s;

            List<string> mods = CollectBattleModifiers();
            GUI.Label(new Rect(rx, ry, rw, 32f * s), "전투 시작 보정", UiTheme.Heading); ry += 38f * s;
            if (mods.Count == 0)
            {
                GUI.Label(new Rect(rx + 10f * s, ry, rw - 10f * s, 26f * s), "특이사항 없음", UiTheme.Small);
                ry += 30f * s;
            }
            else
            {
                foreach (string m in mods)
                {
                    GUI.Label(new Rect(rx + 10f * s, ry, rw - 10f * s, 26f * s), "› " + m, UiTheme.Small);
                    ry += 28f * s;
                }
            }
            ry += 12f * s;

            if (mission != null && !string.IsNullOrEmpty(mission.dangerNotes))
            {
                Rect warn = new Rect(rx, ry, rw, 58f * s);
                UiTheme.DrawFill(warn, new Color(0.706f, 0.220f, 0.169f, 0.14f));
                GUI.Label(new Rect(warn.x + 10f * s, warn.y + 6f * s, warn.width - 20f * s, warn.height - 12f * s),
                    "⚠ " + mission.dangerNotes, UiTheme.Small);
                ry += 66f * s;
            }

            GUI.Label(new Rect(rx, ry, rw, 32f * s), "맵 미리보기", UiTheme.Heading); ry += 38f * s;
            Rect mapRect = new Rect(rx, ry, rw, Mathf.Max(60f * s, right.yMax - 22f * s - ry));
            UiTheme.DrawFill(mapRect, UiTheme.HanjiPanelAlt);
            UiTheme.DrawHLine(new Rect(mapRect.x, mapRect.center.y, mapRect.width, 2f * s), UiTheme.Teal);
            GUI.Label(mapRect, def.mapHint, new GUIStyle(UiTheme.Small) { alignment = TextAnchor.MiddleCenter, padding = new RectOffset(12, 12, 12, 12) });

            // 하단 버튼
            float bw = 240f * s;
            float by = h - 78f * s;
            if (GUI.Button(new Rect(margin, by, bw, 56f * s), "← 거점으로", UiTheme.Button))
            {
                root.Flow.GoToHub(SceneNames.HubPyesadang);
            }

            if (GUI.Button(new Rect(w - margin - bw, by, bw, 56f * s), "출격! →", UiTheme.ButtonPrimary))
            {
                root.Session.actionsTaken++;
                root.Flow.GoToBattle(def.id);
            }
        }

        private static void Pair(float x, ref float y, float w, float s, string label, string value)
        {
            GUI.Label(new Rect(x + 10f * s, y, w * 0.34f, 28f * s), label, UiTheme.SmallMuted);
            GUI.Label(new Rect(x + 10f * s + w * 0.34f, y, w * 0.66f - 10f * s, 28f * s), value, UiTheme.Body);
            y += 30f * s;
        }

        private List<string> CollectBattleModifiers()
        {
            List<string> list = new List<string>();
            int momentum = root.Flags.GetInt("battlemod:park_momentum");
            if (momentum != 0) list.Add($"박성준 기세 +{momentum}");
            int enemyMorale = root.Flags.GetInt("battlemod:enemy_leader_morale");
            if (enemyMorale != 0) list.Add($"적 대장 사기 +{enemyMorale} (도발됨)");
            int dcDown = root.Flags.GetInt("battlemod:dialogue_dc_down");
            if (dcDown != 0) list.Add("대화 판정 유리 (예법 우위)");
            return list;
        }
    }
}
