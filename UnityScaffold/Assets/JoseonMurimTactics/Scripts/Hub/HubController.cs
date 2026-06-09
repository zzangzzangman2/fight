using System.Collections.Generic;
using UnityEngine;

namespace JoseonMurimTactics
{
    /// <summary>
    /// [5] Hub_Pyesadang — 폐사당 임시 거점. 메뉴형 허브(설계 §11):
    /// 동료 대화 / 문파 관리 / 수련 / 정찰 / 출격 / 저장.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HubController : MonoBehaviour
    {
        public const string FirstBattleId = "BATTLE_PYESADANG_DEFENSE";

        private enum HubMenu { Overview, Companions, Sect, Training, Scout, Sortie, Save }

        private GameRoot root;
        private HubMenu menu = HubMenu.Overview;
        private DialogueController talk;
        private readonly List<string> log = new List<string>();
        private string toast;
        private float toastTimer;
        private Vector2 scroll;

        private void Awake()
        {
            root = GameRoot.EnsureExists();
            AddLog($"{root.Session.sectName}의 임시 거점, 폐사당에 도착했다.");
        }

        private void Update()
        {
            if (toastTimer > 0f)
            {
                toastTimer -= Time.unscaledDeltaTime;
                if (toastTimer <= 0f) toast = null;
            }
        }

        private void OnGUI()
        {
            UiTheme.Begin(true);
            float w = Screen.width;
            float h = Screen.height;
            float s = UiTheme.Scale;

            // 동료 대화 진행 중이면 대화창 우선
            if (talk != null)
            {
                GUI.Label(new Rect(0f, 28f * s, w, 40f * s), "동료와 대화", UiTheme.Title);
                talk.Draw(w, h);
                if (talk.IsFinished)
                {
                    talk = null;
                }

                return;
            }

            float margin = 36f * s;
            DrawTopBar(w, s, margin);

            float top = 96f * s;
            float bottom = h - 56f * s;
            float menuW = 230f * s;
            float rightW = 320f * s;
            float centerX = margin + menuW + 18f * s;
            float centerW = w - margin - rightW - 18f * s - centerX;

            DrawMenu(new Rect(margin, top, menuW, bottom - top), s);
            DrawContent(new Rect(centerX, top, centerW, bottom - top), s);
            DrawCompanionSummary(new Rect(w - margin - rightW, top, rightW, bottom - top), s);

            // 하단 로그/힌트
            string hint = log.Count > 0 ? log[log.Count - 1] : "메뉴를 선택하세요.";
            GUI.Label(new Rect(margin, bottom + 14f * s, w - margin * 2f, 30f * s), "• " + hint, UiTheme.SmallMuted);

            if (!string.IsNullOrEmpty(toast))
            {
                DrawToast(w, h, s);
            }
        }

        private void DrawTopBar(float w, float s, float margin)
        {
            Rect bar = new Rect(margin, 22f * s, w - margin * 2f, 60f * s);
            UiTheme.DrawPanel(bar, true);
            GameSession ses = root.Session;
            GUI.Label(new Rect(bar.x + 18f * s, bar.y + 12f * s, bar.width * 0.4f, 36f * s), "폐사당 임시 거점", UiTheme.Heading);
            string mid = $"{ses.sectName}  ·  제0장  ·  성향 {StoryEnumLabels.Label(ses.heroDisposition)}";
            GUI.Label(new Rect(bar.x + bar.width * 0.36f, bar.y + 16f * s, bar.width * 0.4f, 30f * s), mid, UiTheme.Body);
            int renown = root.Reputation.Get(FactionIds.JoseonSects);
            string right = $"위명 {renown}   행동 {ses.actionsTaken}";
            GUIStyle r = new GUIStyle(UiTheme.Body) { alignment = TextAnchor.MiddleRight };
            GUI.Label(new Rect(bar.x + bar.width * 0.6f - 18f * s, bar.y + 16f * s, bar.width * 0.4f, 30f * s), right, r);
        }

        private void DrawMenu(Rect rect, float s)
        {
            UiTheme.DrawPanel(rect);
            float x = rect.x + 16f * s;
            float y = rect.y + 16f * s;
            float bw = rect.width - 32f * s;
            float bh = 54f * s;
            float gap = 10f * s;

            MenuButton(ref y, x, bw, bh, gap, "동료 대화", HubMenu.Companions);
            MenuButton(ref y, x, bw, bh, gap, "문파 관리", HubMenu.Sect);
            MenuButton(ref y, x, bw, bh, gap, "수련", HubMenu.Training);
            MenuButton(ref y, x, bw, bh, gap, "정찰", HubMenu.Scout);
            y += gap;
            MenuButton(ref y, x, bw, bh, gap, "출정", HubMenu.Sortie);
            MenuButton(ref y, x, bw, bh, gap, "저장", HubMenu.Save);
        }

        private void MenuButton(ref float y, float x, float bw, float bh, float gap, string label, HubMenu target)
        {
            bool sel = menu == target;
            if (GUI.Button(new Rect(x, y, bw, bh), label, sel ? UiTheme.ButtonPrimary : UiTheme.Button))
            {
                menu = target;
            }

            y += bh + gap;
        }

        private void DrawContent(Rect rect, float s)
        {
            UiTheme.DrawPanel(rect);
            Rect inner = new Rect(rect.x + 22f * s, rect.y + 18f * s, rect.width - 44f * s, rect.height - 36f * s);

            switch (menu)
            {
                case HubMenu.Companions: DrawCompanions(inner, s); break;
                case HubMenu.Sect: DrawSect(inner, s); break;
                case HubMenu.Training: DrawTraining(inner, s); break;
                case HubMenu.Scout: DrawScout(inner, s); break;
                case HubMenu.Sortie: DrawSortie(inner, s); break;
                case HubMenu.Save: DrawSave(inner, s); break;
                default: DrawOverview(inner, s); break;
            }
        }

        private void DrawOverview(Rect r, float s)
        {
            GUI.Label(new Rect(r.x, r.y, r.width, 36f * s), "거점 개요", UiTheme.Heading);
            GUI.Label(new Rect(r.x, r.y + 46f * s, r.width, r.height - 46f * s),
                "비에 젖은 폐사당에 조선 문파의 첫 깃발이 섰다.\n\n" +
                "전투만 하는 곳이 아니다. 동료와 이야기하고, 문파의 사정을 살피고, 다음 싸움을 정찰한 뒤 출격하라.\n\n" +
                "왼쪽 메뉴에서 할 일을 고르고, 준비가 되면 '출격'으로 압록강 폐사당 방어전에 나설 수 있다.",
                UiTheme.Body);
        }

        private void DrawCompanions(Rect r, float s)
        {
            GUI.Label(new Rect(r.x, r.y, r.width, 36f * s), "동료 대화", UiTheme.Heading);
            float y = r.y + 48f * s;
            float cardH = 92f * s;

            foreach (string id in root.Session.recruitedCompanionIds)
            {
                CompanionInfo info = CompanionCatalog.Info(id);
                if (info == null) continue;
                Rect card = new Rect(r.x, y, r.width, cardH);
                UiTheme.DrawPanel(card, true);
                GUI.Label(new Rect(card.x + 16f * s, card.y + 10f * s, card.width - 200f * s, 30f * s), $"{info.name} · {info.title}", UiTheme.Body);
                GUI.Label(new Rect(card.x + 16f * s, card.y + 42f * s, card.width - 200f * s, 26f * s),
                    $"{info.role}   |   {root.Approval.GetStageLabel(id)} ({root.Approval.Get(id)})", UiTheme.SmallMuted);
                if (GUI.Button(new Rect(card.xMax - 150f * s, card.y + card.height * 0.5f - 24f * s, 134f * s, 48f * s), "대화", UiTheme.Button))
                {
                    talk = new DialogueController(BuildCompanionTalk(id), root);
                    root.Session.actionsTaken++;
                }

                y += cardH + 12f * s;
            }

            // 잠긴 슬롯
            GUI.Label(new Rect(r.x, y + 6f * s, r.width, 26f * s), "── 아직 합류하지 않은 고수들 ──", UiTheme.SmallMuted);
            y += 38f * s;
            string[] locked = { CompanionCatalog.HanBiyeon, CompanionCatalog.DoArin, CompanionCatalog.MaeHwaryeong, CompanionCatalog.KangChohui };
            foreach (string id in locked)
            {
                CompanionInfo info = CompanionCatalog.Info(id);
                if (info == null || root.Session.HasCompanion(id)) continue;
                GUI.Label(new Rect(r.x, y, r.width, 26f * s), $"🔒 {info.name} — {info.title} (이후 장에서 합류)", UiTheme.SmallMuted);
                y += 30f * s;
            }
        }

        private void DrawSect(Rect r, float s)
        {
            GUI.Label(new Rect(r.x, r.y, r.width, 36f * s), "문파 관리", UiTheme.Heading);
            float y = r.y + 50f * s;
            Line(r.x, ref y, r.width, s, "문파명", root.Session.sectName);
            Line(r.x, ref y, r.width, s, "성향", StoryEnumLabels.Label(root.Session.heroDisposition));
            Line(r.x, ref y, r.width, s, "초기 무공", StoryEnumLabels.Label(root.Session.startingArt));
            y += 12f * s;
            GUI.Label(new Rect(r.x, y, r.width, 30f * s), "세력 평판", UiTheme.Heading);
            y += 40f * s;
            string[] factions = { FactionIds.JoseonSects, FactionIds.ZhongyuanAlliance, FactionIds.RoyalCourt, FactionIds.DemonicCult };
            foreach (string f in factions)
            {
                Line(r.x, ref y, r.width, s, FactionIds.Label(f), root.Reputation.Get(f).ToString());
            }
        }

        private void DrawTraining(Rect r, float s)
        {
            GUI.Label(new Rect(r.x, r.y, r.width, 36f * s), "수련", UiTheme.Heading);
            GUI.Label(new Rect(r.x, r.y + 44f * s, r.width, 60f * s),
                "v0.8에서는 수련 연출만 제공한다. 실제 능력치 성장은 이후 버전에서 붙는다.", UiTheme.Small);
            float y = r.y + 110f * s;
            string[] drills = { "박성준 — 기본 무공 점검", "윤서화 — 반격 검로 단련", "백련 — 한기 운용 수련" };
            foreach (string drill in drills)
            {
                if (GUI.Button(new Rect(r.x, y, r.width * 0.8f, 50f * s), drill, UiTheme.Button))
                {
                    AddLog($"{drill} … 땀이 식기 전에 한 합 더.");
                    root.Session.actionsTaken++;
                    ShowToast("수련을 마쳤다.");
                }

                y += 60f * s;
            }
        }

        private void DrawScout(Rect r, float s)
        {
            GUI.Label(new Rect(r.x, r.y, r.width, 36f * s), "정찰 — 압록강 폐사당 방어전", UiTheme.Heading);
            GUI.Label(new Rect(r.x, r.y + 48f * s, r.width, r.height - 48f * s),
                "<b>적 구성</b>\n  · 중원 감찰사 위지강 (대장, 제압 목표)\n  · 감찰단 호위무사 다수, 원거리 암기수 포함\n\n" +
                "<b>지형 힌트</b>\n  · 무너진 다리와 물가 — 도하 지점이 좁다.\n  · 제단 주변은 엄폐가 좋다. 제단을 부수지 말 것.\n\n" +
                "<b>보조 목표</b>\n  · 다친 조선 제자 구출\n  · 제단 보존\n  · 위지강을 죽이지 않고 제압",
                UiTheme.Body);
        }

        private void DrawSortie(Rect r, float s)
        {
            GUI.Label(new Rect(r.x, r.y, r.width, 36f * s), "출정", UiTheme.Heading);
            GUI.Label(new Rect(r.x, r.y + 48f * s, r.width, 90f * s),
                "임무 게시판에서 출정할 임무를 고른다.\n임무를 선택하면 적 정보·보상·승패 조건을 확인하고 출격 준비로 넘어간다.", UiTheme.Body);
            if (GUI.Button(new Rect(r.x, r.y + 150f * s, r.width * 0.7f, 60f * s), "임무 선택 →", UiTheme.ButtonPrimary))
            {
                root.Flow.GoToMissionBoard();
            }
        }

        private void DrawSave(Rect r, float s)
        {
            GUI.Label(new Rect(r.x, r.y, r.width, 36f * s), "저장", UiTheme.Heading);
            GUI.Label(new Rect(r.x, r.y + 48f * s, r.width, 60f * s),
                "현재 진행 상황을 자동 저장 슬롯에 기록한다.", UiTheme.Body);
            if (GUI.Button(new Rect(r.x, r.y + 120f * s, r.width * 0.6f, 56f * s), "지금 저장", UiTheme.ButtonPrimary))
            {
                bool ok = root.Save.Save(root.Session);
                ShowToast(ok ? "저장되었습니다." : "저장에 실패했습니다.");
                AddLog(ok ? "진행 상황을 기록했다." : "저장에 실패했다.");
            }
        }

        private void DrawCompanionSummary(Rect rect, float s)
        {
            UiTheme.DrawPanel(rect, true);
            GUI.Label(new Rect(rect.x + 18f * s, rect.y + 14f * s, rect.width - 36f * s, 32f * s), "동료", UiTheme.Heading);
            float y = rect.y + 56f * s;
            foreach (string id in root.Session.recruitedCompanionIds)
            {
                CompanionInfo info = CompanionCatalog.Info(id);
                if (info == null) continue;
                GUI.Label(new Rect(rect.x + 18f * s, y, rect.width - 36f * s, 28f * s), info.name, UiTheme.Body);
                GUI.Label(new Rect(rect.x + 18f * s, y + 28f * s, rect.width - 36f * s, 24f * s),
                    $"{info.role}", UiTheme.SmallMuted);
                // 승인도 바
                Rect barBg = new Rect(rect.x + 18f * s, y + 54f * s, rect.width - 36f * s, 10f * s);
                UiTheme.DrawFill(barBg, UiTheme.HanjiPanelAlt);
                float frac = Mathf.Clamp01(root.Approval.Get(id) / 100f);
                UiTheme.DrawFill(new Rect(barBg.x, barBg.y, barBg.width * frac, barBg.height), UiTheme.Teal);
                GUI.Label(new Rect(rect.x + 18f * s, y + 64f * s, rect.width - 36f * s, 22f * s),
                    root.Approval.GetStageLabel(id), UiTheme.SmallMuted);
                y += 104f * s;
            }
        }

        private void DrawToast(float w, float h, float s)
        {
            float tw = 360f * s;
            float th = 56f * s;
            Rect t = new Rect(w * 0.5f - tw * 0.5f, h * 0.16f, tw, th);
            UiTheme.DrawPanel(t);
            GUI.Label(t, toast, UiTheme.BodyCenter);
        }

        private static void Line(float x, ref float y, float w, float s, string label, string value)
        {
            GUI.Label(new Rect(x, y, w * 0.4f, 30f * s), label, UiTheme.SmallMuted);
            GUI.Label(new Rect(x + w * 0.4f, y, w * 0.6f, 30f * s), value, UiTheme.Body);
            y += 36f * s;
        }

        private void ShowToast(string text)
        {
            toast = text;
            toastTimer = 2.0f;
        }

        private void AddLog(string text)
        {
            log.Add(text);
            if (log.Count > 20) log.RemoveAt(0);
        }

        private static DialogueScript BuildCompanionTalk(string id)
        {
            DialogueScript d = new DialogueScript();
            CompanionInfo info = CompanionCatalog.Info(id);
            string name = info != null ? info.name : id;

            if (id == CompanionCatalog.YunSeohwa)
            {
                d.Add(new DialogueNode("t0", name,
                    "“문주. 깃발은 세웠으나, 검을 들 자들의 마음까지 세운 것은 아니오.”", "t1"));
                DialogueNode c = new DialogueNode("t1", "박성준", "(어떻게 답할까?)");
                c.choices.Add(new DialogueChoice("정중히 — 그대의 검에 기대겠소.", HeroDisposition.Chivalrous, "t2a").Approval(id, +3));
                c.choices.Add(new DialogueChoice("농담으로 — 마음은 차차 열리는 법이지.", HeroDisposition.Romantic, "t2b").Approval(id, -3));
                d.Add(c);
                d.Add(new DialogueNode("t2a", name, "윤서화가 짧게 고개를 끄덕인다. “…기대에 어긋나지 않겠소.”", null));
                d.Add(new DialogueNode("t2b", name, "윤서화의 눈이 차가워진다. “지금은 그럴 때가 아니오.”", null));
            }
            else if (id == CompanionCatalog.BaekRyeon)
            {
                d.Add(new DialogueNode("t0", name,
                    "“다친 제자들은 고비를 넘겼어요. 다만, 약재가 곧 동날 거예요.”", "t1"));
                DialogueNode c = new DialogueNode("t1", "박성준", "(어떻게 답할까?)");
                c.choices.Add(new DialogueChoice("제자들 안부부터 챙긴다.", HeroDisposition.Chivalrous, "t2a").Approval(id, +3));
                c.choices.Add(new DialogueChoice("약재 걱정은 나중, 지금은 출격이 먼저요.", HeroDisposition.Conqueror, "t2b").Approval(id, -2));
                d.Add(c);
                d.Add(new DialogueNode("t2a", name, "백련이 옅게 웃는다. “…고마워요, 문주.”", null));
                d.Add(new DialogueNode("t2b", name, "백련이 입술을 다문다. “사람이 먼저예요, 문주.”", null));
            }
            else
            {
                d.Add(new DialogueNode("t0", name, "“…아직 그대를 다 믿지는 않소.”", null));
            }

            return d;
        }
    }
}
