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
        string id = string.IsNullOrEmpty(BattleEntryAdapter.PendingBattleId) ? HubController.FirstBattleId
                                                                             : BattleEntryAdapter.PendingBattleId;
        def = BattleCatalog.Get(id);
        mission = FindMission(id);
    }

    private static MissionInfo FindMission(string battleId)
    {
        foreach (MissionInfo m in MissionCatalog.All)
        {
            if (m.battleId == battleId)
                return m;
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
        GUI.Label(new Rect(margin, 74f * s, w - margin * 2f, 28f * s), $"{def.title} · {def.location}",
                  UiTheme.BodyCenter);

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
            GUI.Label(new Rect(lx, y, lw, 32f * s), "임무 개요", UiTheme.Heading);
            y += 36f * s;
            Pair(lx, ref y, lw, s, "적 세력", mission.enemyFaction);
            Pair(lx, ref y, lw, s, "추천 레벨", "Lv." + mission.recommendedLevel + " · " + mission.difficulty);
            y += 10f * s;
        }

        GUI.Label(new Rect(lx, y, lw, 32f * s), "승리 조건", UiTheme.Heading);
        y += 38f * s;
        GUI.Label(new Rect(lx + 10f * s, y, lw - 10f * s, 28f * s), "◎ " + def.victoryCondition, UiTheme.Body);
        y += 40f * s;

        GUI.Label(new Rect(lx, y, lw, 32f * s), "패배 조건", UiTheme.Heading);
        y += 38f * s;
        foreach (string c in def.defeatConditions)
        {
            GUI.Label(new Rect(lx + 10f * s, y, lw - 10f * s, 26f * s), "✕ " + c, UiTheme.Body);
            y += 30f * s;
        }
        y += 12f * s;

        GUI.Label(new Rect(lx, y, lw, 32f * s), "보조 목표", UiTheme.Heading);
        y += 38f * s;
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
        GUI.Label(new Rect(rx, ry, rw, 32f * s), "출격 인원", UiTheme.Heading);
        ry += 38f * s;
        foreach (string member in def.roster)
        {
            GUI.Label(new Rect(rx + 10f * s, ry, rw - 10f * s, 28f * s), "• " + member, UiTheme.Body);
            ry += 32f * s;
        }
        ry += 10f * s;

        GUI.Label(new Rect(rx, ry, rw, 32f * s), "예상 보상", UiTheme.Heading);
        ry += 36f * s;
        if (def.silverReward > 0)
        {
            GUI.Label(new Rect(rx + 10f * s, ry, rw - 10f * s, 26f * s), "· 은전 " + def.silverReward, UiTheme.Small);
            ry += 28f * s;
        }
        List<string> rewards = mission != null && mission.rewardPreview.Count > 0
                                   ? new List<string>(mission.rewardPreview)
                                   : def.rewardItems;
        foreach (string item in rewards)
        {
            if (item.StartsWith("은전"))
                continue;
            GUI.Label(new Rect(rx + 10f * s, ry, rw - 10f * s, 26f * s), "· " + item, UiTheme.Small);
            ry += 28f * s;
        }
        ry += 10f * s;

        List<string> mods = CollectBattleModifiers();
        GUI.Label(new Rect(rx, ry, rw, 32f * s), "전투 시작 보정", UiTheme.Heading);
        ry += 38f * s;
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

        GUI.Label(new Rect(rx, ry, rw, 32f * s), "추천 준비", UiTheme.Heading);
        ry += 36f * s;
        GUI.Label(new Rect(rx + 10f * s, ry, rw - 10f * s, 24f * s), "· 제단 주변 엄폐 활용", UiTheme.Small);
        ry += 26f * s;
        GUI.Label(new Rect(rx + 10f * s, ry, rw - 10f * s, 24f * s), "· 부상자 발생 시 전투 후 의원 확인",
                  UiTheme.Small);
        ry += 26f * s;
        GUI.Label(new Rect(rx + 10f * s, ry, rw - 10f * s, 24f * s), "· 동료 승인도 변화는 결과 화면에서 정산",
                  UiTheme.Small);
        ry += 34f * s;

        GUI.Label(new Rect(rx, ry, rw, 32f * s), "지도 전술 분석", UiTheme.Heading);
        ry += 38f * s;
        Rect mapRect = new Rect(rx, ry, rw, Mathf.Max(152f * s, right.yMax - 22f * s - ry));
        UiTheme.DrawFill(mapRect, UiTheme.HanjiPanelAlt);
        DrawTacticalMapPreview(new Rect(mapRect.x + 12f * s, mapRect.y + 12f * s, 146f * s,
                                        mapRect.height - 24f * s), s);
        Rect analysisText = new Rect(mapRect.x + 172f * s, mapRect.y + 12f * s, mapRect.width - 184f * s,
                                     mapRect.height - 24f * s);
        GUI.Label(analysisText, BuildMapAnalysisText(), new GUIStyle(UiTheme.Small) {
                      alignment = TextAnchor.UpperLeft,
                      padding = new RectOffset(6, 6, 4, 4),
                      wordWrap = true
                  });

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
        if (momentum != 0)
            list.Add($"박성준 기세 +{momentum}");
        int enemyMorale = root.Flags.GetInt("battlemod:enemy_leader_morale");
        if (enemyMorale != 0)
            list.Add($"적 대장 사기 +{enemyMorale} (도발됨)");
        int dcDown = root.Flags.GetInt("battlemod:dialogue_dc_down");
        if (dcDown != 0)
            list.Add("대화 판정 유리 (예법 우위)");
        return list;
    }

    private string BuildMapAnalysisText()
    {
        if (def == null || def.id != HubController.FirstBattleId)
        {
            return def == null ? string.Empty : def.mapHint;
        }

        return "폐사당 고개 방어전\n" +
               "• 중앙 돌계단: 1칸 병목, 전열 1명으로 적 진입 차단\n" +
               "• 좌측 대나무숲: 이동 비용 2, 원거리 시야 차단, 독침·암기 유리\n" +
               "• 우측 낡은 다리: 빠른 우회로지만 밧줄 절단으로 붕괴 가능\n" +
               "• 상단 사당/누각: 고저 2~3, 원거리 사거리 +1과 명중 보너스\n" +
               "• 향로·등불·석등: 연막, 화염, 낙석으로 전투 흐름 변환";
    }

    private static void DrawTacticalMapPreview(Rect rect, float s)
    {
        UiTheme.DrawFill(rect, new Color(0.12f, 0.12f, 0.10f, 0.42f));

        Color road = new Color(0.62f, 0.56f, 0.42f, 0.92f);
        Color bamboo = new Color(0.16f, 0.42f, 0.25f, 0.90f);
        Color water = new Color(0.18f, 0.42f, 0.52f, 0.88f);
        Color shrine = new Color(0.74f, 0.64f, 0.44f, 0.92f);
        Color roof = new Color(0.58f, 0.20f, 0.16f, 0.92f);
        Color mark = new Color(0.96f, 0.78f, 0.24f, 1f);

        UiTheme.DrawFill(new Rect(rect.x + 12f * s, rect.y + 28f * s, 44f * s, rect.height - 42f * s), bamboo);
        UiTheme.DrawFill(new Rect(rect.center.x - 9f * s, rect.y + 44f * s, 18f * s, rect.height - 58f * s), road);
        UiTheme.DrawFill(new Rect(rect.center.x - 35f * s, rect.y + 16f * s, 70f * s, 36f * s), shrine);
        UiTheme.DrawFill(new Rect(rect.center.x + 28f * s, rect.y + 12f * s, 42f * s, 34f * s), roof);
        UiTheme.DrawFill(new Rect(rect.xMax - 46f * s, rect.y + 58f * s, 26f * s, rect.height - 80f * s), water);
        UiTheme.DrawFill(new Rect(rect.xMax - 58f * s, rect.center.y - 8f * s, 42f * s, 16f * s), road);
        UiTheme.DrawFill(new Rect(rect.center.x - 6f * s, rect.y + 72f * s, 12f * s, 22f * s), mark);

        GUI.Label(new Rect(rect.x, rect.y + 2f * s, rect.width, 18f * s), "H2 사당 / H3 누각",
                  new GUIStyle(UiTheme.SmallMuted) { alignment = TextAnchor.MiddleCenter });
        GUI.Label(new Rect(rect.x, rect.yMax - 20f * s, rect.width, 18f * s), "적 진입",
                  new GUIStyle(UiTheme.SmallMuted) { alignment = TextAnchor.MiddleCenter });
    }
}
}
