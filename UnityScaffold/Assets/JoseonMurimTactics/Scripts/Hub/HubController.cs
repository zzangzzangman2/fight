using System.Collections.Generic;
using UnityEngine;

namespace JoseonMurimTactics
{
    /// <summary>
    /// 해동문 본산 허브(설계 v0.9 §2-4, §5). 메뉴형 허브가 이제 게임의 중심.
    /// 출정 / 연무장 / 동료 / 문파 / 객잔 / 의원 / 장터 / 서고 / 저장 / 설정.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HubController : MonoBehaviour
    {
        public const string FirstBattleId = "BATTLE_PYESADANG_DEFENSE";

        private enum HubMenu { Overview, Sortie, Training, Companions, Sect, Tavern, Infirmary, Market, Library, Save, Settings }

        private GameRoot root;
        private HubMenu menu = HubMenu.Overview;
        private DialogueController talk;
        private readonly List<string> log = new List<string>();
        private string toast;
        private float toastTimer;
        private int loreIndex;
        private string rumor;

        private void Awake()
        {
            root = GameRoot.EnsureExists();
            AddLog($"{root.Session.sectName}의 임시 거점, 폐사당에 도착했다.");
            rumor = root.Narration != null ? root.Narration.GenerateNpcLine("hub", root.Session) : string.Empty;
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

            if (talk != null)
            {
                GUI.Label(new Rect(0f, 28f * s, w, 40f * s), "동료와 대화", UiTheme.Title);
                talk.Draw(w, h);
                if (talk.IsFinished) talk = null;
                return;
            }

            float margin = 32f * s;
            DrawTopBar(w, s, margin);

            float top = 96f * s;
            float bottom = h - 52f * s;
            float menuW = 210f * s;
            float rightW = 300f * s;
            float centerX = margin + menuW + 16f * s;
            float centerW = w - margin - rightW - 16f * s - centerX;

            DrawMenu(new Rect(margin, top, menuW, bottom - top), s);
            DrawContent(new Rect(centerX, top, centerW, bottom - top), s);
            DrawCompanionSummary(new Rect(w - margin - rightW, top, rightW, bottom - top), s);

            string hint = log.Count > 0 ? log[log.Count - 1] : "메뉴를 선택하세요.";
            GUI.Label(new Rect(margin, bottom + 12f * s, w - margin * 2f, 28f * s), "• " + hint, UiTheme.SmallMuted);

            if (!string.IsNullOrEmpty(toast)) DrawToast(w, h, s);
        }

        private void DrawTopBar(float w, float s, float margin)
        {
            Rect bar = new Rect(margin, 20f * s, w - margin * 2f, 58f * s);
            UiTheme.DrawPanel(bar, true);
            GameSession ses = root.Session;
            GUI.Label(new Rect(bar.x + 18f * s, bar.y + 12f * s, bar.width * 0.34f, 34f * s), "해동문 · 폐사당 거점", UiTheme.Heading);
            string mid = $"{ses.sectName}  ·  제0장  ·  기조 {StoryEnumLabels.Label(ses.heroDisposition)}";
            GUI.Label(new Rect(bar.x + bar.width * 0.34f, bar.y + 15f * s, bar.width * 0.4f, 30f * s), mid, UiTheme.Body);
            string right = $"위명 {root.Reputation.Get(FactionIds.JoseonSects)}   은전 {root.Flags.GetInt("silver")}";
            GUI.Label(new Rect(bar.x + bar.width * 0.6f - 18f * s, bar.y + 15f * s, bar.width * 0.4f, 30f * s), right,
                new GUIStyle(UiTheme.Body) { alignment = TextAnchor.MiddleRight });
        }

        private void DrawMenu(Rect rect, float s)
        {
            UiTheme.DrawPanel(rect);
            float x = rect.x + 14f * s;
            float y = rect.y + 14f * s;
            float bw = rect.width - 28f * s;
            float bh = 46f * s;
            float gap = 7f * s;

            MenuButton(ref y, x, bw, bh, gap, "출정", HubMenu.Sortie);
            MenuButton(ref y, x, bw, bh, gap, "연무장", HubMenu.Training);
            MenuButton(ref y, x, bw, bh, gap, "동료", HubMenu.Companions);
            MenuButton(ref y, x, bw, bh, gap, "문파", HubMenu.Sect);
            MenuButton(ref y, x, bw, bh, gap, "객잔", HubMenu.Tavern);
            MenuButton(ref y, x, bw, bh, gap, "의원", HubMenu.Infirmary);
            MenuButton(ref y, x, bw, bh, gap, "장터", HubMenu.Market);
            MenuButton(ref y, x, bw, bh, gap, "서고", HubMenu.Library);
            y += gap;
            MenuButton(ref y, x, bw, bh, gap, "저장", HubMenu.Save);
            MenuButton(ref y, x, bw, bh, gap, "설정", HubMenu.Settings);
        }

        private void MenuButton(ref float y, float x, float bw, float bh, float gap, string label, HubMenu target)
        {
            if (GUI.Button(new Rect(x, y, bw, bh), label, menu == target ? UiTheme.ButtonPrimary : UiTheme.Button))
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
                case HubMenu.Sortie: DrawSortie(inner, s); break;
                case HubMenu.Training: DrawTraining(inner, s); break;
                case HubMenu.Companions: DrawCompanions(inner, s); break;
                case HubMenu.Sect: DrawSect(inner, s); break;
                case HubMenu.Tavern: DrawTavern(inner, s); break;
                case HubMenu.Infirmary: DrawInfirmary(inner, s); break;
                case HubMenu.Market: DrawMarket(inner, s); break;
                case HubMenu.Library: DrawLibrary(inner, s); break;
                case HubMenu.Save: DrawSave(inner, s); break;
                case HubMenu.Settings: DrawSettings(inner, s); break;
                default: DrawOverview(inner, s); break;
            }
        }

        private void DrawOverview(Rect r, float s)
        {
            GUI.Label(new Rect(r.x, r.y, r.width, 36f * s), "거점 개요", UiTheme.Heading);
            GUI.Label(new Rect(r.x, r.y + 46f * s, r.width, r.height - 46f * s),
                "비에 젖은 폐사당에 조선 문파의 첫 깃발이 섰다.\n\n" +
                "전투만 하는 곳이 아니다. 동료와 이야기하고, 문파의 사정을 살피고, 객잔에서 소문을 듣고, 의원에서 부상을 다스린 뒤 출정하라.\n\n" +
                "준비가 되면 '출정'에서 임무를 골라 압록강 폐사당 방어전에 나설 수 있다.", UiTheme.Body);
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

        private void DrawTraining(Rect r, float s)
        {
            GUI.Label(new Rect(r.x, r.y, r.width, 36f * s), "연무장", UiTheme.Heading);
            GUI.Label(new Rect(r.x, r.y + 44f * s, r.width, 50f * s),
                "기초 조작과 무공을 점검하는 곳. (실전 성장·대련은 이후 버전)", UiTheme.Small);
            float y = r.y + 100f * s;
            string[] drills = { "박성준 — 백두광검 검로 점검", "백련 — 설악창 한기 운용", "도아린 — 화왕도 돌파 연습" };
            foreach (string d in drills)
            {
                if (GUI.Button(new Rect(r.x, r.y + (y - r.y), r.width * 0.78f, 46f * s), d, UiTheme.Button))
                {
                    AddLog($"{d} … 땀이 식기 전에 한 합 더.");
                    root.Session.actionsTaken++;
                    ShowToast("연무를 마쳤다.");
                }
                y += 54f * s;
            }
            GUI.Label(new Rect(r.x, y + 6f * s, r.width, 120f * s),
                "전투 조작 순서: ①유닛 선택 ②파란 칸 이동 ③적 사거리 확인 ④공격/무공 ⑤예측 확인 ⑥주사위 ⑦반격 확인 ⑧대기 ⑨페이즈 종료.",
                UiTheme.SmallMuted);
        }

        private void DrawCompanions(Rect r, float s)
        {
            GUI.Label(new Rect(r.x, r.y, r.width, 36f * s), "동료", UiTheme.Heading);
            float y = r.y + 48f * s;
            float cardH = 106f * s;
            foreach (string id in root.Session.recruitedCompanionIds)
            {
                CompanionInfo info = CompanionCatalog.Info(id);
                if (info == null) continue;
                Rect card = new Rect(r.x, y, r.width, cardH);
                UiTheme.DrawPanel(card, true);
                GUI.Label(new Rect(card.x + 16f * s, card.y + 10f * s, card.width - 180f * s, 30f * s), $"{info.name} · {info.title}", UiTheme.Body);
                GUI.Label(new Rect(card.x + 16f * s, card.y + 42f * s, card.width - 180f * s, 24f * s),
                    $"{info.age}세 · {info.mbti} · {info.region} {info.sectName}", UiTheme.SmallMuted);
                GUI.Label(new Rect(card.x + 16f * s, card.y + 68f * s, card.width - 180f * s, 24f * s),
                    $"{info.element} / {info.weapon}   |   {root.Approval.GetStageLabel(id)} ({root.Approval.Get(id)})", UiTheme.SmallMuted);
                if (GUI.Button(new Rect(card.xMax - 144f * s, card.y + card.height * 0.5f - 22f * s, 128f * s, 44f * s), "대화", UiTheme.Button))
                {
                    talk = new DialogueController(BuildCompanionTalk(id), root);
                    root.Session.actionsTaken++;
                }
                y += cardH + 12f * s;
            }
            GUI.Label(new Rect(r.x, y + 4f * s, r.width, 26f * s), "── 이후 합류 예정 ──", UiTheme.SmallMuted);
            y += 34f * s;
            string[] locked = { CompanionCatalog.SeoA, CompanionCatalog.MaeHwaryeong, CompanionCatalog.HanBiyeon };
            foreach (string id in locked)
            {
                CompanionInfo info = CompanionCatalog.Info(id);
                if (info == null || root.Session.HasCompanion(id)) continue;
                GUI.Label(new Rect(r.x, y, r.width, 26f * s), $"🔒 {info.name} — {info.region} {info.sectName} / {info.element} / {info.weapon}", UiTheme.SmallMuted);
                y += 28f * s;
            }
        }

        private void DrawSect(Rect r, float s)
        {
            GUI.Label(new Rect(r.x, r.y, r.width, 36f * s), "문파 — 해동문", UiTheme.Heading);
            float y = r.y + 46f * s;
            Line(r.x, ref y, r.width, s, "위명", root.Reputation.Get(FactionIds.JoseonSects).ToString());
            Line(r.x, ref y, r.width, s, "중원무림맹 적대", (-root.Reputation.Get(FactionIds.ZhongyuanAlliance)).ToString());
            Line(r.x, ref y, r.width, s, "조정 관심", root.Reputation.Get(FactionIds.RoyalCourt).ToString());
            Line(r.x, ref y, r.width, s, "은전", root.Flags.GetInt("silver").ToString());
            y += 8f * s;

            GUI.Label(new Rect(r.x, y, r.width, 30f * s), "문파 기조 (정책)", UiTheme.Heading); y += 38f * s;
            HeroDisposition[] all = { HeroDisposition.Royal, HeroDisposition.Chivalrous, HeroDisposition.Conqueror, HeroDisposition.Romantic };
            float gap = 8f * s;
            float bw = (r.width - gap * 3f) / 4f;
            for (int i = 0; i < all.Length; i++)
            {
                bool sel = root.Session.heroDisposition == all[i];
                if (GUI.Button(new Rect(r.x + (bw + gap) * i, y, bw, 44f * s), StoryEnumLabels.Label(all[i]),
                        sel ? UiTheme.ButtonPrimary : UiTheme.Button))
                {
                    root.Session.heroDisposition = all[i];
                    ShowToast($"문파 기조를 {StoryEnumLabels.Label(all[i])}(으)로 정했다.");
                    AddLog($"해동문의 기조가 {StoryEnumLabels.Label(all[i])}(으)로 바뀌었다.");
                }
            }
            y += 52f * s;
            GUI.Label(new Rect(r.x, y, r.width, 60f * s), "정책 효과 — " + PolicyEffect(root.Session.heroDisposition), UiTheme.Small);
        }

        private static string PolicyEffect(HeroDisposition d)
        {
            switch (d)
            {
                case HeroDisposition.Royal: return "왕도: 명성 획득 증가, 적 항복 유도에 유리. 사파/마교와 마찰 가능.";
                case HeroDisposition.Chivalrous: return "협도: 민심 보너스, 약자 보호 보조 목표에 유리. 협박 선택지 약화.";
                case HeroDisposition.Conqueror: return "패도: 적 기세 감소에 강함. 선한 동료 승인도 리스크.";
                case HeroDisposition.Romantic: return "풍류: 대화·도발 보너스. 실패 시 승인도/적대 리스크.";
                default: return string.Empty;
            }
        }

        private void DrawTavern(Rect r, float s)
        {
            GUI.Label(new Rect(r.x, r.y, r.width, 36f * s), "객잔", UiTheme.Heading);
            GUI.Label(new Rect(r.x, r.y + 46f * s, r.width, 28f * s), "무림의 소문이 술잔을 타고 흐른다.", UiTheme.SmallMuted);
            Rect quote = new Rect(r.x, r.y + 80f * s, r.width, 90f * s);
            UiTheme.DrawFill(quote, UiTheme.HanjiPanelAlt);
            GUI.Label(new Rect(quote.x + 12f * s, quote.y + 10f * s, quote.width - 24f * s, quote.height - 20f * s),
                string.IsNullOrEmpty(rumor) ? "..." : rumor, new GUIStyle(UiTheme.Body) { fontStyle = FontStyle.Italic });
            if (GUI.Button(new Rect(r.x, r.y + 186f * s, r.width * 0.5f, 46f * s), "소문 더 듣기", UiTheme.Button))
            {
                root.Session.actionsTaken++;
                rumor = root.Narration.GenerateNpcLine("tavern" + root.Session.actionsTaken, root.Session);
            }
            GUI.Label(new Rect(r.x, r.y + 246f * s, r.width, 60f * s),
                "· 서브 의뢰와 동료 영입 소문은 이후 버전에서 열립니다.", UiTheme.SmallMuted);
        }

        private void DrawInfirmary(Rect r, float s)
        {
            GUI.Label(new Rect(r.x, r.y, r.width, 36f * s), "의원", UiTheme.Heading);
            BattleResultData last = root.Session.lastBattleResult;
            float y = r.y + 50f * s;
            if (last != null && last.woundedCompanions.Count > 0)
            {
                GUI.Label(new Rect(r.x, y, r.width, 28f * s), "치료가 필요한 동료:", UiTheme.Body); y += 34f * s;
                foreach (string id in last.woundedCompanions)
                {
                    GUI.Label(new Rect(r.x + 10f * s, y, r.width - 10f * s, 26f * s), "· " + CompanionCatalog.Name(id) + " (부상)", UiTheme.Small);
                    y += 28f * s;
                }
                if (GUI.Button(new Rect(r.x, y + 8f * s, r.width * 0.6f, 48f * s), "치료하기", UiTheme.ButtonPrimary))
                {
                    last.woundedCompanions.Clear();
                    ShowToast("동료의 상처를 다스렸다.");
                    AddLog("의원에서 동료들의 부상을 치료했다.");
                }
            }
            else
            {
                GUI.Label(new Rect(r.x, y, r.width, 60f * s), "지금은 치료가 필요한 동료가 없다.\n전투에서 다친 동료가 생기면 이곳에서 회복시킨다.", UiTheme.Body);
            }
        }

        private void DrawMarket(Rect r, float s)
        {
            GUI.Label(new Rect(r.x, r.y, r.width, 36f * s), "장터", UiTheme.Heading);
            GUI.Label(new Rect(r.x, r.y + 44f * s, r.width, 26f * s), $"보유 은전: {root.Flags.GetInt("silver")}", UiTheme.Body);
            float y = r.y + 84f * s;
            BuyRow(r, ref y, s, "약재 꾸러미", 40, "전투 후 회복에 쓰인다.");
            BuyRow(r, ref y, s, "내공단", 60, "내공 회복 소모품.");
            BuyRow(r, ref y, s, "투척 비수 묶음", 30, "암기 보급.");
            GUI.Label(new Rect(r.x, y + 6f * s, r.width, 40f * s), "· 장비/무공 상점은 이후 버전에서 확장됩니다.", UiTheme.SmallMuted);
        }

        private void BuyRow(Rect r, ref float y, float s, string item, int price, string desc)
        {
            Rect row = new Rect(r.x, y, r.width, 54f * s);
            GUI.Label(new Rect(row.x, row.y + 4f * s, row.width * 0.5f, 26f * s), item, UiTheme.Body);
            GUI.Label(new Rect(row.x, row.y + 28f * s, row.width * 0.6f, 22f * s), desc, UiTheme.SmallMuted);
            bool canBuy = root.Flags.GetInt("silver") >= price;
            GUI.enabled = canBuy;
            if (GUI.Button(new Rect(row.xMax - 150f * s, row.y + 6f * s, 140f * s, 42f * s), $"구매 ({price})", UiTheme.Button))
            {
                root.Flags.AddInt("silver", -price);
                ShowToast($"{item} 구매");
                AddLog($"장터에서 {item}을(를) 샀다. (-{price}은전)");
            }
            GUI.enabled = true;
            y += 60f * s;
        }

        private void DrawLibrary(Rect r, float s)
        {
            GUI.Label(new Rect(r.x, r.y, r.width, 36f * s), "서고 — 도감", UiTheme.Heading);
            float gap = 8f * s;
            float bw = (r.width - gap * 2f) / 3f;
            string[] tabs = { "세계관", "세력", "무공" };
            for (int i = 0; i < tabs.Length; i++)
            {
                if (GUI.Button(new Rect(r.x + (bw + gap) * i, r.y + 44f * s, bw, 40f * s), tabs[i],
                        loreIndex == i ? UiTheme.ButtonPrimary : UiTheme.Button))
                {
                    loreIndex = i;
                }
            }
            Rect body = new Rect(r.x, r.y + 96f * s, r.width, r.height - 96f * s);
            UiTheme.DrawFill(body, UiTheme.HanjiPanelAlt);
            GUI.Label(new Rect(body.x + 14f * s, body.y + 12f * s, body.width - 28f * s, body.height - 24f * s), LoreText(loreIndex), UiTheme.Body);
        }

        private static string LoreText(int i)
        {
            switch (i)
            {
                case 0: return "중원무림맹의 강경 정파가 조선 문파들을 ‘하위 분파’로 흡수하려 한다. 무공·예법·기록·언어를 중원식으로 바꾸라 강요하는 가운데, 흩어진 조선 문파들이 해동문 박성준을 중심으로 연합하기 시작한다.";
                case 1: return "· 조선문파연합: 해동문이 묶어가는 신흥 연합.\n· 중원무림맹(강경파): 흡수·동화를 밀어붙이는 권력층.\n· 무림맹 감찰단: 현판령을 집행하는 첨병.\n· 조정 / 마교 / 흑립방: 각자의 셈을 가진 변수들.";
                case 2: return "· 백두광검(박성준): 빛과 검으로 파훼를 만든다.\n· 설악창(백련): 서리와 창으로 적을 묶는다.\n· 화왕도(도아린): 불과 도로 정면을 돌파한다.\n· 천뢰봉(서아): 전기와 봉으로 빠르게 흔든다.\n· 풍매선(매화령): 바람과 꽃, 부채로 지원한다.\n· 흑연암기(한비연): 어둠과 독, 단검·암기로 빈틈을 찌른다.";
                default: return string.Empty;
            }
        }

        private void DrawSave(Rect r, float s)
        {
            GUI.Label(new Rect(r.x, r.y, r.width, 36f * s), "저장", UiTheme.Heading);
            GUI.Label(new Rect(r.x, r.y + 48f * s, r.width, 56f * s), "현재 진행 상황을 자동 저장 슬롯에 기록한다.", UiTheme.Body);
            if (GUI.Button(new Rect(r.x, r.y + 116f * s, r.width * 0.6f, 56f * s), "지금 저장", UiTheme.ButtonPrimary))
            {
                bool ok = root.Save.Save(root.Session);
                ShowToast(ok ? "저장되었습니다." : "저장에 실패했습니다.");
                AddLog(ok ? "진행 상황을 기록했다." : "저장에 실패했다.");
            }
        }

        private void DrawSettings(Rect r, float s)
        {
            GUI.Label(new Rect(r.x, r.y, r.width, 36f * s), "설정", UiTheme.Heading);
            GUI.Label(new Rect(r.x, r.y + 48f * s, r.width, 90f * s),
                "해상도/소리/텍스트 속도 옵션은 이후 버전에서 추가됩니다.\n지금은 타이틀 화면의 설정과 동일하게 자리만 잡아둔 상태입니다.", UiTheme.Body);
            if (GUI.Button(new Rect(r.x, r.y + 150f * s, r.width * 0.6f, 52f * s), "타이틀로", UiTheme.Button))
            {
                root.Flow.GoToTitle();
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
                GUI.Label(new Rect(rect.x + 18f * s, y + 28f * s, rect.width - 36f * s, 24f * s), info.role, UiTheme.SmallMuted);
                Rect barBg = new Rect(rect.x + 18f * s, y + 54f * s, rect.width - 36f * s, 10f * s);
                UiTheme.DrawFill(barBg, UiTheme.HanjiPanelAlt);
                float frac = Mathf.Clamp01(root.Approval.Get(id) / 100f);
                UiTheme.DrawFill(new Rect(barBg.x, barBg.y, barBg.width * frac, barBg.height), UiTheme.Teal);
                GUI.Label(new Rect(rect.x + 18f * s, y + 64f * s, rect.width - 36f * s, 22f * s), root.Approval.GetStageLabel(id), UiTheme.SmallMuted);
                y += 104f * s;
            }
        }

        private void DrawToast(float w, float h, float s)
        {
            float tw = 360f * s, th = 56f * s;
            Rect t = new Rect(w * 0.5f - tw * 0.5f, h * 0.16f, tw, th);
            UiTheme.DrawPanel(t);
            GUI.Label(t, toast, UiTheme.BodyCenter);
        }

        private static void Line(float x, ref float y, float w, float s, string label, string value)
        {
            GUI.Label(new Rect(x, y, w * 0.4f, 30f * s), label, UiTheme.SmallMuted);
            GUI.Label(new Rect(x + w * 0.4f, y, w * 0.6f, 30f * s), value, UiTheme.Body);
            y += 34f * s;
        }

        private void ShowToast(string text) { toast = text; toastTimer = 2.0f; }

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

            if (id == CompanionCatalog.BaekRyeon)
            {
                d.Add(new DialogueNode("t0", name, "“창끝은 차갑게 두겠습니다. 다만, 사람을 살릴 길까지 얼리지는 말아 주세요.”", "t1"));
                DialogueNode c = new DialogueNode("t1", "박성준", "(어떻게 답할까?)");
                c.choices.Add(new DialogueChoice("다친 제자들부터 살피자.", HeroDisposition.Chivalrous, "t2a").Approval(id, +3));
                c.choices.Add(new DialogueChoice("냉정하게 — 지금은 전열이 먼저다.", HeroDisposition.Conqueror, "t2b").Approval(id, -2));
                d.Add(c);
                d.Add(new DialogueNode("t2a", name, "백련이 조용히 고개를 숙인다. “…네. 그 말이면 충분합니다.”", null));
                d.Add(new DialogueNode("t2b", name, "백련의 눈빛이 잠시 얼어붙는다. “그 냉정함이 사람을 버리지 않길 바랍니다.”", null));
            }
            else if (id == CompanionCatalog.DoArin)
            {
                d.Add(new DialogueNode("t0", name, "“문주, 복잡하게 재지 말자. 저놈들이 밀고 오면, 내가 먼저 불길 열게.”", "t1"));
                DialogueNode c = new DialogueNode("t1", "박성준", "(어떻게 답할까?)");
                c.choices.Add(new DialogueChoice("좋다. 단, 혼자 앞서지 마라.", HeroDisposition.Royal, "t2a").Approval(id, +2));
                c.choices.Add(new DialogueChoice("앞장서라. 길은 힘으로 연다.", HeroDisposition.Conqueror, "t2b").Approval(id, +4));
                d.Add(c);
                d.Add(new DialogueNode("t2a", name, "도아린이 도집을 툭 친다. “알았어. 한 발만 먼저 간다, 한 발만.”", null));
                d.Add(new DialogueNode("t2b", name, "도아린이 씩 웃는다. “그 말 기다렸어.”", null));
            }
            else if (id == CompanionCatalog.SeoA)
            {
                d.Add(new DialogueNode("t0", name, "“문주님! 저 방금 번개가 어디로 튀는지 봤어요. 아, 아니, 진짜로요!”", null));
            }
            else if (id == CompanionCatalog.MaeHwaryeong)
            {
                d.Add(new DialogueNode("t0", name, "“바람은 억지로 잡으면 달아나요. 사람 마음도 비슷하답니다, 문주님.”", null));
            }
            else if (id == CompanionCatalog.HanBiyeon)
            {
                d.Add(new DialogueNode("t0", name, "“정면으로 부딪히는 건 취향이 아니야. 대신, 등 뒤의 길은 내가 볼게.”", null));
            }
            else
            {
                d.Add(new DialogueNode("t0", name, "“…아직 그대를 다 믿지는 않소.”", null));
            }

            return d;
        }
    }
}
