using System.Collections.Generic;
using UnityEngine;

namespace JoseonMurimTactics
{
/// <summary>
/// 출정 임무 선택 보드(설계 v0.9 §2-4, §5-2). 좌측 임무 목록, 우측 상세, 하단 출정.
/// 임무 선택 시 BattleEntryAdapter에 전투를 예약하고 출격 준비(BattlePrep)로 넘어간다.
/// </summary>
[DisallowMultipleComponent]
public sealed class MissionBoardController : MonoBehaviour
{
    private enum MissionFilter
    {
        All,
        Available,
        Completed,
        Locked
    }

    private GameRoot root;
    private IReadOnlyList<MissionInfo> missions;
    private int selected;
    private MissionFilter filter = MissionFilter.All;

    private void Awake()
    {
        root = GameRoot.EnsureExists();
        missions = root.MissionRepository != null ? root.MissionRepository.All : MissionCatalog.All;
    }

    private void OnGUI()
    {
        UiTheme.Begin(true);
        float w = Screen.width;
        float h = Screen.height;
        float s = UiTheme.Scale;
        float margin = 44f * s;

        UiTheme.LabelShadow(new Rect(margin, 24f * s, w - margin * 2f, 50f * s), "출정 — 임무 선택", UiTheme.Title);
        UiTheme.DrawDivider(w * 0.5f, 86f * s, w - margin * 2f);

        DrawFilters(new Rect(margin, 100f * s, w - margin * 2f, 38f * s), s);

        float top = 150f * s;
        float bottom = h - 96f * s;
        float listW = (w - margin * 2f) * 0.42f;
        float detailX = margin + listW + 22f * s;
        float detailW = w - margin - detailX;

        DrawList(new Rect(margin, top, listW, bottom - top), s);
        DrawDetail(new Rect(detailX, top, detailW, bottom - top), s);

        MissionInfo sel = Current();
        bool playable = sel != null && sel.IsUnlocked(root.Flags) && sel.IsPlayable && HasFreeTimeFor(sel);

        float bw = 240f * s;
        float by = h - 78f * s;
        if (GUI.Button(new Rect(margin, by, bw, 56f * s), "← 거점으로", UiTheme.Button))
        {
            root.Flow.GoToHub(SceneNames.HubPyesadang);
        }

        GUI.enabled = playable;
        string label = sel != null && !sel.IsPlayable ? "준비 중" :
                       sel != null && sel.consumesFreeTime && !HasFreeTimeFor(sel) ? "기력 부족" : "출격 준비로 →";
        if (GUI.Button(new Rect(w - margin - bw, by, bw, 56f * s), label, UiTheme.ButtonPrimary))
        {
            root.Flow.GoToBattlePrep(sel.battleId);
        }
        GUI.enabled = true;
    }

    private void DrawList(Rect rect, float s)
    {
        UiTheme.DrawPanel(rect);
        float x = rect.x + 16f * s;
        float y = rect.y + 16f * s;
        float cw = rect.width - 32f * s;
        float ch = 92f * s;

        for (int i = 0; i < missions.Count; i++)
        {
            MissionInfo m = missions[i];
            if (!ShouldShow(m))
            {
                continue;
            }

            bool unlocked = m.IsUnlocked(root.Flags);
            Rect card = new Rect(x, y, cw, ch);

            if (i == selected)
            {
                UiTheme.DrawFill(new Rect(card.x - 4f * s, card.y - 4f * s, card.width + 8f * s, card.height + 8f * s),
                                 UiTheme.Gold);
            }
            UiTheme.DrawPanel(card, i != selected);

            if (GUI.Button(card, GUIContent.none, UiTheme.PanelSoft))
            {
                selected = i;
            }

            string badge = MissionStatus(m);
            GUI.Label(new Rect(card.x + 14f * s, card.y + 10f * s, card.width - 120f * s, 28f * s), m.title,
                      UiTheme.Body);
            GUI.Label(new Rect(card.xMax - 110f * s, card.y + 12f * s, 96f * s, 24f * s), badge,
                      new GUIStyle(UiTheme.SmallMuted) { alignment = TextAnchor.MiddleRight });
            GUI.Label(new Rect(card.x + 14f * s, card.y + 42f * s, card.width - 28f * s, 24f * s), $"{m.location}",
                      UiTheme.SmallMuted);
            GUI.Label(new Rect(card.x + 14f * s, card.y + 64f * s, card.width - 28f * s, 24f * s),
                      $"추천 Lv.{m.recommendedLevel} · 난이도 {m.difficulty}", UiTheme.SmallMuted);

            y += ch + 12f * s;
        }
    }

    private void DrawDetail(Rect rect, float s)
    {
        UiTheme.DrawPanel(rect);
        MissionInfo m = Current();
        if (m == null)
        {
            return;
        }

        float x = rect.x + 22f * s;
        float wdt = rect.width - 44f * s;
        float y = rect.y + 18f * s;

        GUI.Label(new Rect(x, y, wdt, 34f * s), m.title, UiTheme.Heading);
        y += 42f * s;
        Line(x, ref y, wdt, s, "위치", m.location);
        Line(x, ref y, wdt, s, "적 세력", m.enemyFaction);
        Line(x, ref y, wdt, s, "추천 레벨", "Lv." + m.recommendedLevel);
        Line(x, ref y, wdt, s, "난이도", m.difficulty);
        Line(x, ref y, wdt, s, "상태", MissionStatus(m));
        if (m.consumesFreeTime)
        {
            Line(x, ref y, wdt, s, "자유시간", $"기력 1 소모 · 남은 기력 {FreeActionsRemaining()}");
        }
        Line(x, ref y, wdt, s, "시도", MissionAttempts(m.id).ToString());
        Line(x, ref y, wdt, s, "승리 조건", m.victoryConditionShort);
        y += 8f * s;

        GUI.Label(new Rect(x, y, wdt, 24f * s), "임무 설명", UiTheme.SmallMuted);
        y += 28f * s;
        GUI.Label(new Rect(x, y, wdt, 86f * s), m.summary, UiTheme.Body);
        y += 92f * s;

        GUI.Label(new Rect(x, y, wdt, 24f * s), "보상", UiTheme.SmallMuted);
        y += 28f * s;
        foreach (string r in m.rewardPreview)
        {
            GUI.Label(new Rect(x + 10f * s, y, wdt - 10f * s, 24f * s), "· " + r, UiTheme.Small);
            y += 26f * s;
        }
        y += 8f * s;

        GUI.Label(new Rect(x, y, wdt, 24f * s), "보조 목표 / 실패 결과 / 세력 변화", UiTheme.SmallMuted);
        y += 28f * s;
        GUI.Label(new Rect(x + 10f * s, y, wdt - 10f * s, 24f * s),
                  "· 보조 목표: " + ObjectiveSummary(m),
                  UiTheme.Small);
        y += 26f * s;
        GUI.Label(new Rect(x + 10f * s, y, wdt - 10f * s, 24f * s),
                  "· 실패: " + FailureSummary(m),
                  UiTheme.Small);
        y += 26f * s;
        GUI.Label(new Rect(x + 10f * s, y, wdt - 10f * s, 24f * s),
                  "· 예상 변화: " + ExpectedChangeSummary(m),
                  UiTheme.Small);
        y += 34f * s;

        if (!string.IsNullOrEmpty(m.dangerNotes))
        {
            Rect warn = new Rect(x, y, wdt, 64f * s);
            UiTheme.DrawFill(warn, new Color(0.706f, 0.220f, 0.169f, 0.14f));
            GUI.Label(new Rect(warn.x + 10f * s, warn.y + 8f * s, warn.width - 20f * s, warn.height - 16f * s),
                      "전술 주의 — " + m.dangerNotes, UiTheme.Small);
            y += 72f * s;
        }

        if (!m.IsUnlocked(root.Flags))
        {
            GUI.Label(new Rect(x, rect.yMax - 40f * s, wdt, 28f * s), LockedReason(m),
                      new GUIStyle(UiTheme.SmallMuted) { alignment = TextAnchor.MiddleCenter });
        }
        else if (!m.IsPlayable)
        {
            GUI.Label(new Rect(x, rect.yMax - 40f * s, wdt, 28f * s), "이 임무의 전투는 다음 버전에서 구현됩니다.",
                      new GUIStyle(UiTheme.SmallMuted) { alignment = TextAnchor.MiddleCenter });
        }
        else if (IsMissionCompleted(m))
        {
            GUI.Label(new Rect(x, rect.yMax - 40f * s, wdt, 28f * s), "재전투 시 첫 클리어 보상과 평판 변화는 반복 지급되지 않습니다.",
                      new GUIStyle(UiTheme.SmallMuted) { alignment = TextAnchor.MiddleCenter });
        }
        else if (m.repeatable)
        {
            GUI.Label(new Rect(x, rect.yMax - 40f * s, wdt, 28f * s), "반복 의뢰입니다. 출격 확정 시 자유시간/기력 1을 소모합니다.",
                      new GUIStyle(UiTheme.SmallMuted) { alignment = TextAnchor.MiddleCenter });
        }
    }

    private MissionInfo Current()
    {
        if (missions == null || missions.Count == 0)
            return null;
        selected = Mathf.Clamp(selected, 0, missions.Count - 1);
        if (ShouldShow(missions[selected]))
        {
            return missions[selected];
        }

        for (int i = 0; i < missions.Count; i++)
        {
            if (ShouldShow(missions[i]))
            {
                selected = i;
                return missions[i];
            }
        }

        return null;
    }

    private void DrawFilters(Rect rect, float s)
    {
        GUI.Label(new Rect(rect.x, rect.y, 90f * s, rect.height), "필터", UiTheme.SmallMuted);
        string[] labels = { "전체", "출격 가능", "완료", "잠김" };
        filter = (MissionFilter)GUI.SelectionGrid(new Rect(rect.x + 92f * s, rect.y, 420f * s, rect.height),
                                                  (int)filter, labels, labels.Length, UiTheme.Button);
    }

    private bool ShouldShow(MissionInfo mission)
    {
        if (mission == null)
        {
            return false;
        }

        bool unlocked = mission.IsUnlocked(root.Flags);
        bool completed = IsMissionCompleted(mission);
        switch (filter)
        {
        case MissionFilter.Available:
            return unlocked && !completed;
        case MissionFilter.Completed:
            return completed;
        case MissionFilter.Locked:
            return !unlocked;
        default:
            return true;
        }
    }

    private string MissionStatus(MissionInfo mission)
    {
        if (mission == null)
        {
            return "-";
        }

        if (!mission.IsUnlocked(root.Flags))
        {
            return "잠김";
        }

        if (IsMissionCompleted(mission))
        {
            return "완료";
        }

        if (mission.repeatable)
        {
            return mission.IsPlayable ? "반복 의뢰" : "준비 중";
        }

        return mission.IsPlayable ? (mission.isStory ? "주요" : "의뢰") : "준비 중";
    }

    private bool IsMissionCompleted(MissionInfo mission)
    {
        if (mission == null || mission.repeatable)
        {
            return false;
        }

        return mission.IsCompleted(root.Flags) || root.Session.completedMissionIds.Contains(mission.id);
    }

    private bool HasFreeTimeFor(MissionInfo mission)
    {
        return mission == null || !mission.consumesFreeTime || FreeActionsRemaining() > 0;
    }

    private int FreeActionsRemaining()
    {
        return root == null || root.Flags == null ? 0 : Mathf.Max(0, root.Flags.GetInt(HubController.ActionPointKey));
    }

    private int MissionAttempts(string missionId)
    {
        return root != null && root.Session.missionAttempts.TryGetValue(missionId, out int attempts) ? attempts : 0;
    }

    private string LockedReason(MissionInfo mission)
    {
        if (mission == null)
        {
            return "선택한 임무가 없습니다.";
        }

        if (string.IsNullOrEmpty(mission.requiredFlag))
        {
            return "아직 열리지 않은 임무입니다.";
        }

        return "필요 조건 미달: " + mission.requiredFlag;
    }

    private static string ObjectiveSummary(MissionInfo mission)
    {
        if (mission == null || !mission.repeatable)
        {
            return "제단 보존 / 부상자 최소화 / 8턴 이내";
        }

        if (mission.battleId == HubController.BanditLairBattleId)
        {
            return "덫 피해 최소화 / 보급 회수 / 망루 고지 제압";
        }
        if (mission.battleId == HubController.WolfPassBattleId)
        {
            return "피난로 확보 / 늑대 굴 봉쇄 / 개울 병목 활용";
        }
        if (mission.battleId == HubController.TigerRavineBattleId)
        {
            return "주민 구조 / 바위 선반 확보 / 억새 엄폐 활용";
        }
        if (mission.battleId == HubController.LeopardCliffBattleId)
        {
            return "약초꾼 호송 / 절벽 매복 회피 / 밧줄다리 확보";
        }

        return "지형 목표 확보 / 부상자 최소화 / 빠른 정리";
    }

    private static string FailureSummary(MissionInfo mission)
    {
        if (mission == null || !mission.repeatable)
        {
            return "중원무림맹 위압 상승, 허브 기능 일부 지연";
        }

        if (mission.battleId == HubController.BanditLairBattleId)
        {
            return "자유시간 소모, 도적 소문 위험도 유지";
        }

        return "자유시간 소모, 마을 외곽 피해와 야수 출몰 위험도 유지";
    }

    private static string ExpectedChangeSummary(MissionInfo mission)
    {
        if (mission == null || !mission.repeatable)
        {
            return "조선문파연합 +5 / 감찰단 적대 +5";
        }

        if (mission.battleId == HubController.BanditLairBattleId)
        {
            return "조선문파연합 +1 / 흑립방 -2 / 마을 신뢰 상승";
        }

        return "조선문파연합 +1 / 마을 신뢰 상승";
    }

    private static void Line(float x, ref float y, float w, float s, string label, string value)
    {
        GUI.Label(new Rect(x, y, w * 0.32f, 28f * s), label, UiTheme.SmallMuted);
        GUI.Label(new Rect(x + w * 0.32f, y, w * 0.68f, 28f * s), value, UiTheme.Body);
        y += 32f * s;
    }
}
}
