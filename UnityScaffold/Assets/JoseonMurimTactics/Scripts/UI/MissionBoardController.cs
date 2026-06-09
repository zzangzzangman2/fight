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
        private GameRoot root;
        private IReadOnlyList<MissionInfo> missions;
        private int selected;

        private void Awake()
        {
            root = GameRoot.EnsureExists();
            missions = MissionCatalog.All;
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

            float top = 108f * s;
            float bottom = h - 96f * s;
            float listW = (w - margin * 2f) * 0.42f;
            float detailX = margin + listW + 22f * s;
            float detailW = w - margin - detailX;

            DrawList(new Rect(margin, top, listW, bottom - top), s);
            DrawDetail(new Rect(detailX, top, detailW, bottom - top), s);

            MissionInfo sel = Current();
            bool playable = sel != null && sel.IsUnlocked(root.Flags) && sel.IsPlayable;

            float bw = 240f * s;
            float by = h - 78f * s;
            if (GUI.Button(new Rect(margin, by, bw, 56f * s), "← 거점으로", UiTheme.Button))
            {
                root.Flow.GoToHub(SceneNames.HubPyesadang);
            }

            GUI.enabled = playable;
            string label = sel != null && !sel.IsPlayable ? "준비 중" : "출격 준비로 →";
            if (GUI.Button(new Rect(w - margin - bw, by, bw, 56f * s), label, UiTheme.ButtonPrimary))
            {
                root.Session.actionsTaken++;
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
                bool unlocked = m.IsUnlocked(root.Flags);
                bool done = m.IsCompleted(root.Flags);
                Rect card = new Rect(x, y, cw, ch);

                if (i == selected)
                {
                    UiTheme.DrawFill(new Rect(card.x - 4f * s, card.y - 4f * s, card.width + 8f * s, card.height + 8f * s), UiTheme.Gold);
                }
                UiTheme.DrawPanel(card, i != selected);

                if (GUI.Button(card, GUIContent.none, UiTheme.PanelSoft))
                {
                    selected = i;
                }

                string badge = !unlocked ? "🔒 잠김" : done ? "✔ 완료" : (m.isStory ? "주요" : "의뢰");
                GUI.Label(new Rect(card.x + 14f * s, card.y + 10f * s, card.width - 120f * s, 28f * s), m.title, UiTheme.Body);
                GUI.Label(new Rect(card.xMax - 110f * s, card.y + 12f * s, 96f * s, 24f * s), badge,
                    new GUIStyle(UiTheme.SmallMuted) { alignment = TextAnchor.MiddleRight });
                GUI.Label(new Rect(card.x + 14f * s, card.y + 42f * s, card.width - 28f * s, 24f * s),
                    $"{m.location}", UiTheme.SmallMuted);
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

            GUI.Label(new Rect(x, y, wdt, 34f * s), m.title, UiTheme.Heading); y += 42f * s;
            Line(x, ref y, wdt, s, "위치", m.location);
            Line(x, ref y, wdt, s, "적 세력", m.enemyFaction);
            Line(x, ref y, wdt, s, "추천 레벨", "Lv." + m.recommendedLevel);
            Line(x, ref y, wdt, s, "난이도", m.difficulty);
            Line(x, ref y, wdt, s, "승리 조건", m.victoryConditionShort);
            y += 8f * s;

            GUI.Label(new Rect(x, y, wdt, 24f * s), "임무 설명", UiTheme.SmallMuted); y += 28f * s;
            GUI.Label(new Rect(x, y, wdt, 86f * s), m.summary, UiTheme.Body); y += 92f * s;

            GUI.Label(new Rect(x, y, wdt, 24f * s), "보상", UiTheme.SmallMuted); y += 28f * s;
            foreach (string r in m.rewardPreview)
            {
                GUI.Label(new Rect(x + 10f * s, y, wdt - 10f * s, 24f * s), "· " + r, UiTheme.Small); y += 26f * s;
            }
            y += 8f * s;

            if (!string.IsNullOrEmpty(m.dangerNotes))
            {
                Rect warn = new Rect(x, y, wdt, 64f * s);
                UiTheme.DrawFill(warn, new Color(0.706f, 0.220f, 0.169f, 0.14f));
                GUI.Label(new Rect(warn.x + 10f * s, warn.y + 8f * s, warn.width - 20f * s, warn.height - 16f * s),
                    "⚠ 위험 지형 — " + m.dangerNotes, UiTheme.Small);
                y += 72f * s;
            }

            if (!m.IsUnlocked(root.Flags))
            {
                GUI.Label(new Rect(x, rect.yMax - 40f * s, wdt, 28f * s), "이전 임무를 완료하면 개방됩니다.",
                    new GUIStyle(UiTheme.SmallMuted) { alignment = TextAnchor.MiddleCenter });
            }
            else if (!m.IsPlayable)
            {
                GUI.Label(new Rect(x, rect.yMax - 40f * s, wdt, 28f * s), "이 임무의 전투는 다음 버전에서 구현됩니다.",
                    new GUIStyle(UiTheme.SmallMuted) { alignment = TextAnchor.MiddleCenter });
            }
        }

        private MissionInfo Current()
        {
            if (missions == null || missions.Count == 0) return null;
            selected = Mathf.Clamp(selected, 0, missions.Count - 1);
            return missions[selected];
        }

        private static void Line(float x, ref float y, float w, float s, string label, string value)
        {
            GUI.Label(new Rect(x, y, w * 0.32f, 28f * s), label, UiTheme.SmallMuted);
            GUI.Label(new Rect(x + w * 0.32f, y, w * 0.68f, 28f * s), value, UiTheme.Body);
            y += 32f * s;
        }
    }
}
