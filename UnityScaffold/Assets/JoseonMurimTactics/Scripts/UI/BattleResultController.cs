using System.Collections.Generic;
using UnityEngine;

namespace JoseonMurimTactics
{
/// <summary>
/// [7] BattleResult — 승/패, 전투 등급(갑/을/병), 보상, 승인도·평판 변화, 무림 소문 표시.
/// 결과를 세션에 1회 정산(평판/승인도/은전/퀘스트)하고 자동 저장한 뒤 허브로 돌아간다.
/// </summary>
[DisallowMultipleComponent]
public sealed class BattleResultController : MonoBehaviour
{
    private GameRoot root;
    private BattleResultData result;
    private BattleDefinition def;
    private string grade;
    private string rumor;
    private BattleResultApplyOutcome applyOutcome;

    private void Awake()
    {
        root = GameRoot.EnsureExists();
        result = BattleResultBridge.LastResult;
        if (result == null)
        {
            // 씬을 직접 열었을 때의 안전 기본값.
            result = new BattleResultData { battleId = HubController.FirstBattleId, outcome = BattleOutcome.Victory,
                                            defeatedBoss = "철랑문 정찰조장", turnCount = 6 };
            result.completedObjectives.Add("OBJ_DEFEAT_SCOUTS");
        }

        def = root.BattleRepository != null ? root.BattleRepository.Get(result.battleId) : BattleCatalog.Get(result.battleId);
        grade = ComputeGrade();
        rumor = root.Narration != null ? root.Narration.GenerateRumor(result) : string.Empty;
        applyOutcome = root.BattleResults.Apply(result, def);
    }

    private string ComputeGrade()
    {
        if (!result.Won)
        {
            return "—";
        }

        int optionalDone = 0;
        foreach (string objId in result.completedObjectives)
        {
            if (IsOptionalObjective(objId))
            {
                optionalDone++;
            }
        }

        if (optionalDone >= 2 && result.turnCount <= 8)
            return "갑";
        if (optionalDone >= 1)
            return "을";
        return "병";
    }

    private bool IsOptionalObjective(string objId)
    {
        if (def == null || def.objectives == null)
        {
            return objId != "OBJ_DEFEAT_SCOUTS" && objId != "OBJ_CLEAR_BANDIT_LAIR" &&
                   objId != "OBJ_RECOVER_SUPPLIES";
        }

        foreach (BattleObjective objective in def.objectives)
        {
            if (objective != null && objective.id == objId)
            {
                return objective.optional;
            }
        }

        return false;
    }

    private void OnGUI()
    {
        UiTheme.Begin(true);
        float w = Screen.width;
        float h = Screen.height;
        float s = UiTheme.Scale;
        float margin = 48f * s;

        bool won = result.Won;
        string banner = won ? "승 리" : "패 배";
        GUIStyle bannerStyle = new GUIStyle(UiTheme.Logo) { fontSize = Mathf.RoundToInt(56 * s) };
        bannerStyle.normal.textColor = won ? UiTheme.Navy : UiTheme.SealRed;
        UiTheme.LabelShadow(new Rect(0f, 28f * s, w, 80f * s), banner, bannerStyle);
        GUI.Label(new Rect(0f, 104f * s, w, 30f * s), def.title, UiTheme.BodyCenter);
        UiTheme.DrawDivider(w * 0.5f, 140f * s, 420f * s);

        // 등급 인장
        float seal = 92f * s;
        UiTheme.DrawSeal(new Rect(w - margin - seal, 34f * s, seal, seal), grade);

        float top = 150f * s;
        float bottom = h - 96f * s;
        float colGap = 24f * s;
        float colW = (w - margin * 2f - colGap) * 0.5f;

        // 왼쪽: 보상/정산
        Rect left = new Rect(margin, top, colW, bottom - top);
        UiTheme.DrawPanel(left);
        float lx = left.x + 22f * s;
        float lw = left.width - 44f * s;
        float y = left.y + 18f * s;

        if (won)
        {
            GUI.Label(new Rect(lx, y, lw, 32f * s), "보상", UiTheme.Heading);
            y += 38f * s;
            if (applyOutcome != null && applyOutcome.duplicate)
            {
                GUI.Label(new Rect(lx + 10f * s, y, lw, 28f * s), "이미 정산된 전투 결과입니다.", UiTheme.SmallMuted);
                y += 32f * s;
            }
            else if (applyOutcome != null && applyOutcome.replayRewardsReduced)
            {
                GUI.Label(new Rect(lx + 10f * s, y, lw, 28f * s), "재전투 보상: 첫 클리어 보상과 평판 변화는 제외",
                          UiTheme.SmallMuted);
                y += 32f * s;
            }

            bool duplicate = applyOutcome != null && applyOutcome.duplicate;
            if (!duplicate)
            {
                int silver = applyOutcome != null && applyOutcome.replayRewardsReduced
                                 ? Mathf.Max(1, result.silver / 4)
                                 : result.silver;
                GUI.Label(new Rect(lx + 10f * s, y, lw, 28f * s), $"은냥 {silver}", UiTheme.Body);
                y += 32f * s;
                if (applyOutcome == null || !applyOutcome.replayRewardsReduced)
                {
                    List<string> rewardLines = InventoryService.FormatRewardLines(result.rewardItems);
                    for (int i = 0; i < rewardLines.Count; i++)
                    {
                        GUI.Label(new Rect(lx + 10f * s, y, lw - 10f * s, 26f * s), "· " + rewardLines[i],
                                  UiTheme.Small);
                        y += 28f * s;
                    }
                }

                List<string> lootLines = CollectLootLines();
                if (lootLines.Count > 0)
                {
                    y += 4f * s;
                    GUI.Label(new Rect(lx + 10f * s, y, lw - 10f * s, 24f * s), "추가 전리품", UiTheme.SmallMuted);
                    y += 24f * s;
                    for (int i = 0; i < lootLines.Count; i++)
                    {
                        GUI.Label(new Rect(lx + 10f * s, y, lw - 10f * s, 26f * s), "· " + lootLines[i],
                                  UiTheme.Small);
                        y += 28f * s;
                    }
                }
                else if (applyOutcome != null && applyOutcome.replayRewardsReduced)
                {
                    GUI.Label(new Rect(lx + 10f * s, y, lw - 10f * s, 26f * s), "· 추가 전리품 없음",
                              UiTheme.SmallMuted);
                    y += 28f * s;
                }
            }
            y += 12f * s;
        }

        GUI.Label(new Rect(lx, y, lw, 32f * s), "전황", UiTheme.Heading);
        y += 38f * s;
        GUI.Label(new Rect(lx + 10f * s, y, lw - 10f * s, 26f * s), $"소요 턴 {result.turnCount}", UiTheme.Small);
        y += 30f * s;
        foreach (string objId in result.completedObjectives)
        {
            GUI.Label(new Rect(lx + 10f * s, y, lw - 10f * s, 26f * s), "✔ " + ObjectiveLabel(objId), UiTheme.Small);
            y += 28f * s;
        }
        foreach (string objId in result.failedObjectives)
        {
            GUIStyle fail = new GUIStyle(UiTheme.Small);
            fail.normal.textColor = UiTheme.SealRed;
            GUI.Label(new Rect(lx + 10f * s, y, lw - 10f * s, 26f * s), "✘ " + ObjectiveLabel(objId), fail);
            y += 28f * s;
        }
        foreach (string id in result.woundedCompanions)
        {
            GUI.Label(new Rect(lx + 10f * s, y, lw - 10f * s, 26f * s), $"※ {CompanionCatalog.Name(id)} 부상",
                      UiTheme.Small);
            y += 28f * s;
        }

        // 장기 성장 요약 — ProgressionBattleRewardBridge가 specialFlags에 남긴 progression: 항목.
        List<string> growthLines = new List<string>();
        foreach (string flag in result.specialFlags)
        {
            if (!string.IsNullOrEmpty(flag) && flag.StartsWith("progression:"))
            {
                growthLines.Add(flag.Substring("progression:".Length));
            }
        }

        if (growthLines.Count > 0)
        {
            y += 10f * s;
            GUI.Label(new Rect(lx, y, lw, 32f * s), "성장", UiTheme.Heading);
            y += 38f * s;
            int shown = Mathf.Min(growthLines.Count, 6);
            for (int i = 0; i < shown; i++)
            {
                GUI.Label(new Rect(lx + 10f * s, y, lw - 10f * s, 26f * s), "· " + growthLines[i], UiTheme.Small);
                y += 28f * s;
            }

            if (growthLines.Count > shown)
            {
                GUI.Label(new Rect(lx + 10f * s, y, lw - 10f * s, 26f * s), $"외 {growthLines.Count - shown}건 (저장됨)",
                          UiTheme.SmallMuted);
                y += 28f * s;
            }
        }

        // 오른쪽: 변화/소문
        Rect right = new Rect(margin + colW + colGap, top, colW, bottom - top);
        UiTheme.DrawPanel(right);
        float rx = right.x + 22f * s;
        float rw = right.width - 44f * s;
        float ry = right.y + 18f * s;

        GUI.Label(new Rect(rx, ry, rw, 32f * s), "세력 평판 변화", UiTheme.Heading);
        ry += 38f * s;
        if (result.factionChanges.Count == 0)
        {
            GUI.Label(new Rect(rx + 10f * s, ry, rw, 26f * s), "변화 없음", UiTheme.Small);
            ry += 30f * s;
        }
        foreach (BattleResultData.StatDelta f in result.factionChanges)
        {
            GUI.Label(new Rect(rx + 10f * s, ry, rw - 10f * s, 26f * s), $"{FactionIds.Label(f.id)} {Signed(f.delta)}",
                      UiTheme.Small);
            ry += 28f * s;
        }
        ry += 10f * s;

        GUI.Label(new Rect(rx, ry, rw, 32f * s), "동료 승인도 변화", UiTheme.Heading);
        ry += 38f * s;
        if (result.approvalChanges.Count == 0)
        {
            GUI.Label(new Rect(rx + 10f * s, ry, rw, 26f * s), "변화 없음", UiTheme.Small);
            ry += 30f * s;
        }
        foreach (BattleResultData.StatDelta a in result.approvalChanges)
        {
            GUI.Label(new Rect(rx + 10f * s, ry, rw - 10f * s, 26f * s),
                      $"{CompanionCatalog.Name(a.id)} {Signed(a.delta)}", UiTheme.Small);
            ry += 28f * s;
        }
        ry += 12f * s;

        GUI.Label(new Rect(rx, ry, rw, 32f * s), "무림 소문", UiTheme.Heading);
        ry += 38f * s;
        Rect rumorBox = new Rect(rx, ry, rw, 96f * s);
        UiTheme.DrawFill(rumorBox, UiTheme.HanjiPanelAlt);
        GUI.Label(
            new Rect(rumorBox.x + 12f * s, rumorBox.y + 10f * s, rumorBox.width - 24f * s, rumorBox.height - 20f * s),
            "“" + rumor + "”", new GUIStyle(UiTheme.Body) { fontStyle = FontStyle.Italic });
        ry += 110f * s;

        GUI.Label(new Rect(rx, ry, rw, 32f * s), "해금된 기능", UiTheme.Heading);
        ry += 34f * s;
        GUI.Label(new Rect(rx + 10f * s, ry, rw - 10f * s, 24f * s), "· 객잔 소문 기능 확장", UiTheme.Small);
        ry += 26f * s;
        GUI.Label(new Rect(rx + 10f * s, ry, rw - 10f * s, 24f * s), "· 서고: 검은 표식 항목 개방", UiTheme.Small);
        ry += 26f * s;
        GUI.Label(new Rect(rx + 10f * s, ry, rw - 10f * s, 24f * s), "· 임무 게시판 다음 장 예고", UiTheme.Small);

        // 하단
        float bw = 280f * s;
        float by = h - 78f * s;
        string leftLabel = won ? "거점으로 돌아가기" : "재도전 준비";
        if (GUI.Button(new Rect(w * 0.5f - bw - 10f * s, by, bw, 56f * s), leftLabel, UiTheme.ButtonPrimary))
        {
            if (won)
            {
                root.Flow.GoToHub(SceneNames.HubPyesadang);
            }
            else
            {
                root.Flow.GoToBattlePrep(def.id);
            }
        }

        bool nextChapterReady = false;
        GUI.enabled = won ? nextChapterReady : true;
        string rightLabel = won ? "다음 장 (준비 중)" : "거점에서 재정비";
        if (GUI.Button(new Rect(w * 0.5f + 10f * s, by, bw, 56f * s), rightLabel, UiTheme.Button))
        {
            root.Flow.GoToHub(SceneNames.HubPyesadang);
        }
        GUI.enabled = true;
    }

    private static string ObjectiveLabel(string objId)
    {
        switch (objId)
        {
        case "OBJ_DEFEAT_SCOUTS":
            return "철랑문 정찰조 격퇴";
        case "OBJ_CLEAR_BANDIT_LAIR":
            return "흑립방 두목 제압";
        case "OBJ_RECOVER_SUPPLIES":
            return "빼앗긴 보급 회수";
        case "OBJ_AVOID_TRAPS":
            return "덫 피해 최소화";
        case "OBJ_REPEL_WOLF_PACK":
            return "굶주린 늑대 무리 격퇴";
        case "OBJ_PROTECT_HERDERS":
            return "방목민 피난로 확보";
        case "OBJ_SECURE_WOLF_DEN":
            return "북쪽 늑대 굴 봉쇄";
        case "OBJ_SUBDUE_TIGER":
            return "산군 호랑이 제압";
        case "OBJ_RESCUE_VILLAGERS":
            return "갇힌 주민 구조";
        case "OBJ_CONTROL_RAVINE_HIGHGROUND":
            return "동쪽 바위 선반 확보";
        case "OBJ_DRIVE_OFF_LEOPARD":
            return "그림자 표범 격퇴";
        case "OBJ_ESCORT_HERBALISTS":
            return "약초꾼 호송로 개방";
        case "OBJ_AVOID_CLIFF_AMBUSH":
            return "절벽 매복 피해 최소화";
        case "OBJ_SAVE_PORTERS":
            return "마을 짐꾼 생존";
        case "OBJ_INSPECT_MARK":
            return "검은 표식 조사";
        default:
            return objId;
        }
    }

    private static string Signed(int v)
    {
        return v > 0 ? "+" + v : v.ToString();
    }

    private List<string> CollectLootLines()
    {
        List<string> itemIds = new List<string>();
        List<int> deltas = new List<int>();
        if (result == null || result.specialFlags == null)
        {
            return new List<string>();
        }

        for (int i = 0; i < result.specialFlags.Count; i++)
        {
            string flag = result.specialFlags[i];
            if (string.IsNullOrEmpty(flag) || !flag.StartsWith("loot:"))
            {
                continue;
            }

            string payload = flag.Substring("loot:".Length);
            int split = payload.LastIndexOf(':');
            if (split <= 0 || split >= payload.Length - 1)
            {
                continue;
            }

            string itemId = payload.Substring(0, split);
            if (!int.TryParse(payload.Substring(split + 1), out int delta) || delta <= 0)
            {
                continue;
            }

            itemIds.Add(itemId);
            deltas.Add(delta);
        }

        return InventoryService.FormatRewardDeltaLines(itemIds, deltas);
    }
}
}
