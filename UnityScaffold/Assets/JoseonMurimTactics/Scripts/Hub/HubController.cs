using System.Collections.Generic;
using UnityEngine;

namespace JoseonMurimTactics
{
/// <summary>
/// 백두천광검문 거점 허브(설계 v0.9 §2-4, §5). 메뉴형 허브가 이제 게임의 중심.
/// 출정 / 연무장 / 동료 / 문파 / 객잔 / 의원 / 장터 / 서고 / 저장 / 설정.
/// </summary>
[DisallowMultipleComponent]
public sealed class HubController : MonoBehaviour
{
    public const string FirstBattleId = "BATTLE_PYESADANG_DEFENSE";
    private const int MaxDailyActions = 3;
    private const string ActionPointKey = "hub:daily_actions_remaining";
    private const string ActionPointInitializedFlag = "FLAG_HUB_ACTION_POINTS_READY";

    private enum HubMenu
    {
        Overview,
        Sortie,
        Training,
        Companions,
        Sect,
        Tavern,
        Infirmary,
        Market,
        Library,
        Save,
        Settings
    }

    private GameRoot root;
    private HubMenu menu = HubMenu.Overview;
    private DialogueController talk;
    private readonly List<string> log = new List<string>();
    private string toast;
    private float toastTimer;
    private int loreIndex;
    private string rumor;
    private RumorData rumorData;
    private string pendingOverwriteSlot;
    private GameSettings settings;

    private void Awake()
    {
        root = GameRoot.EnsureExists();
        EnsureActionPoints();
        settings = GameSettings.Load();
        AddLog($"{root.Session.sectName}의 낡은 검각에 새벽빛이 들었다.");
        RefreshRumor("hub");
    }

    private void Update()
    {
        if (toastTimer > 0f)
        {
            toastTimer -= Time.unscaledDeltaTime;
            if (toastTimer <= 0f)
                toast = null;
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
            if (talk.IsFinished)
                talk = null;
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

        if (!string.IsNullOrEmpty(toast))
            DrawToast(w, h, s);
    }

    private void DrawTopBar(float w, float s, float margin)
    {
        Rect bar = new Rect(margin, 20f * s, w - margin * 2f, 58f * s);
        UiTheme.DrawPanel(bar, true);
        GameSession ses = root.Session;
        GUI.Label(new Rect(bar.x + 18f * s, bar.y + 12f * s, bar.width * 0.34f, 34f * s), "백두천광검문 · 소백촌 거점",
                  UiTheme.Heading);
        string mid = $"{ses.sectName}  ·  제1장  ·  기조 {StoryEnumLabels.Label(ses.heroDisposition)}";
        GUI.Label(new Rect(bar.x + bar.width * 0.34f, bar.y + 15f * s, bar.width * 0.4f, 30f * s), mid, UiTheme.Body);
        string right =
            $"제{DayIndex}일   기력 {ActionsRemaining}/{MaxDailyActions}   위명 {root.Reputation.Get(FactionIds.JoseonSects)}   은전 {root.Flags.GetInt("silver")}";
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
        if (GUI.Button(new Rect(x, y, bw, bh), "지도", UiTheme.Button))
        {
            root.Flow.GoToWorldMap();
        }

        y += bh + gap;
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
        case HubMenu.Sortie:
            DrawSortie(inner, s);
            break;
        case HubMenu.Training:
            DrawTraining(inner, s);
            break;
        case HubMenu.Companions:
            DrawCompanions(inner, s);
            break;
        case HubMenu.Sect:
            DrawSect(inner, s);
            break;
        case HubMenu.Tavern:
            DrawTavern(inner, s);
            break;
        case HubMenu.Infirmary:
            DrawInfirmary(inner, s);
            break;
        case HubMenu.Market:
            DrawMarket(inner, s);
            break;
        case HubMenu.Library:
            DrawLibrary(inner, s);
            break;
        case HubMenu.Save:
            DrawSave(inner, s);
            break;
        case HubMenu.Settings:
            DrawSettings(inner, s);
            break;
        default:
            DrawOverview(inner, s);
            break;
        }
    }

    private void DrawOverview(Rect r, float s)
    {
        GUI.Label(new Rect(r.x, r.y, r.width, 36f * s), "백두산 검각", UiTheme.Heading);
        GUI.Label(new Rect(r.x, r.y + 38f * s, r.width, 44f * s),
                  "하루가 지나면 기력이 회복된다. 수련, 일감, 복구를 골라 오늘의 자유시간을 쓴다.",
                  UiTheme.Body);

        Rect yard = new Rect(r.x, r.y + 92f * s, r.width, Mathf.Min(360f * s, r.height - 140f * s));
        UiTheme.DrawFill(yard, UiTheme.HanjiPanelAlt);
        GUI.Label(new Rect(yard.x + 18f * s, yard.y + 14f * s, yard.width - 36f * s, 30f * s),
                  "검각 전경: 본당 / 연무장 / 소백촌 장터 / 의원 / 약방 / 서고 / 출정 깃발", UiTheme.SmallMuted);

        Hotspot(yard, 0.44f, 0.13f, 0.22f, 0.13f, s, "출정 깃발", HubMenu.Sortie, "!");
        Hotspot(yard, 0.08f, 0.38f, 0.22f, 0.14f, s, "연무장", HubMenu.Training, ActionsRemaining > 0 ? "" : "!");
        Hotspot(yard, 0.38f, 0.42f, 0.24f, 0.15f, s, "검각 마루", HubMenu.Companions, CompanionBadge());
        Hotspot(yard, 0.40f, 0.68f, 0.22f, 0.14f, s, "문파 재건", HubMenu.Sect, "!");
        Hotspot(yard, 0.68f, 0.30f, 0.22f, 0.14f, s, "객잔", HubMenu.Tavern, "!");
        Hotspot(yard, 0.68f, 0.56f, 0.22f, 0.14f, s, "의원", HubMenu.Infirmary, InjuredCount() > 0 ? "!" : "");
        Hotspot(yard, 0.09f, 0.64f, 0.20f, 0.13f, s, "장터", HubMenu.Market, "");
        Hotspot(yard, 0.12f, 0.16f, 0.20f, 0.13f, s, "서고", HubMenu.Library,
                root.Flags.HasFlag(StoryFlags.FirstBattleWon) ? "!" : "");

        float y = yard.yMax + 14f * s;
        GUI.Label(new Rect(r.x, y, r.width * 0.55f, 28f * s), $"오늘 기력: {ActionsRemaining}/{MaxDailyActions}",
                  UiTheme.Body);
        GUI.Label(new Rect(r.x, y + 30f * s, r.width * 0.72f, 26f * s),
                  $"수련도 {root.Flags.GetInt("growth:martial_xp")}   연구 {root.Flags.GetInt("growth:research_xp")}   문파 복구 {root.Flags.GetInt("sect:repair")}   마을 신뢰 {root.Flags.GetInt("sect:village_trust")}",
                  UiTheme.SmallMuted);
        if (GUI.Button(new Rect(r.x + r.width - 160f * s, y - 6f * s, 160f * s, 42f * s), "하루 보내기", UiTheme.Button))
        {
            BeginNextDay();
        }
    }

    private void Hotspot(Rect parent, float px, float py, float pw, float ph, float s, string label, HubMenu target,
                         string badge)
    {
        Rect rect = new Rect(parent.x + parent.width * px, parent.y + parent.height * py, parent.width * pw,
                             parent.height * ph);
        bool clicked = GUI.Button(rect, label, menu == target ? UiTheme.ButtonPrimary : UiTheme.Button);
        if (!string.IsNullOrEmpty(badge))
        {
            Rect dot = new Rect(rect.xMax - 24f * s, rect.y + 6f * s, 18f * s, 18f * s);
            UiTheme.DrawSeal(dot, badge);
        }

        if (clicked)
        {
            menu = target;
        }
    }

    private void DrawSortie(Rect r, float s)
    {
        GUI.Label(new Rect(r.x, r.y, r.width, 36f * s), "출정", UiTheme.Heading);
        GUI.Label(new Rect(r.x, r.y + 48f * s, r.width, 90f * s),
                  "임무 게시판에서 출정할 임무를 고른다.\n임무를 선택하면 적 정보·보상·승패 조건을 확인하고 출격 " +
                      "준비로 넘어간다.",
                  UiTheme.Body);
        if (GUI.Button(new Rect(r.x, r.y + 150f * s, r.width * 0.7f, 60f * s), "임무 선택 →", UiTheme.ButtonPrimary))
        {
            root.Flow.GoToMissionBoard();
        }
    }

    private void DrawTraining(Rect r, float s)
    {
        GUI.Label(new Rect(r.x, r.y, r.width, 36f * s), "연무장", UiTheme.Heading);
        GUI.Label(new Rect(r.x, r.y + 44f * s, r.width, 50f * s),
                  "기력을 써서 무공 숙련을 올린다. 일정 수치가 쌓이면 초식과 심법 단서가 열린다.", UiTheme.Small);
        float y = r.y + 100f * s;
        string[] drills = { "박성준 — 천광심법 호흡", "박성준 — 백야검결 검로", "동료 합련 — 속성 연계 대련" };
        for (int i = 0; i < drills.Length; i++)
        {
            string d = drills[i];
            if (GUI.Button(new Rect(r.x, r.y + (y - r.y), r.width * 0.78f, 46f * s), d, UiTheme.Button))
            {
                if (TrySpendAction("연무장 수련"))
                {
                    ApplyTraining(i);
                }
            }
            y += 54f * s;
        }
        GUI.Label(new Rect(r.x, y, r.width, 30f * s),
                  $"천광심법 {root.Flags.GetInt("growth:inner_art_xp")}   백야검결 {root.Flags.GetInt("growth:sword_xp")}   합련 {root.Flags.GetInt("growth:teamwork_xp")}",
                  UiTheme.SmallMuted);
        y += 34f * s;
        GUI.Label(new Rect(r.x, y + 6f * s, r.width, 120f * s),
                  "전투 조작 순서: ①유닛 선택 ②파란 칸 이동 ③적 사거리 확인 ④공격/무공 ⑤예측 확인 ⑥주사위 ⑦반격 확인 " +
                      "⑧대기 ⑨페이즈 종료.",
                  UiTheme.SmallMuted);
    }

    private void DrawCompanions(Rect r, float s)
    {
        GUI.Label(new Rect(r.x, r.y, r.width, 36f * s), "동료", UiTheme.Heading);
        float y = r.y + 48f * s;
        float cardH = 126f * s;
        foreach (string id in root.Session.recruitedCompanionIds)
        {
            CompanionInfo info = CompanionCatalog.Info(id);
            if (info == null)
                continue;
            Rect card = new Rect(r.x, y, r.width, cardH);
            UiTheme.DrawPanel(card, true);
            GUI.Label(new Rect(card.x + 16f * s, card.y + 10f * s, card.width - 180f * s, 30f * s),
                      $"{info.name} · {info.title}", UiTheme.Body);
            GUI.Label(
                new Rect(card.x + 16f * s, card.y + 42f * s, card.width - 180f * s, 26f * s),
                $"{info.age}세 · {info.mbti} · {info.region} {info.sectName} · {info.element}/{info.weapon}   |   {root.Approval.GetStageLabel(id)} ({root.Approval.Get(id)})",
                UiTheme.SmallMuted);
            GUI.Label(new Rect(card.x + 16f * s, card.y + 68f * s, card.width - 180f * s, 24f * s),
                      "고민: " + CompanionConcern(id), UiTheme.SmallMuted);
            GUI.Label(new Rect(card.x + 16f * s, card.y + 92f * s, card.width - 180f * s, 24f * s),
                      $"다음 대화: {NextCompanionTalk(id)}   |   상태: {(IsWounded(id) ? "부상" : "출전 가능")}",
                      UiTheme.SmallMuted);
            if (GUI.Button(new Rect(card.xMax - 144f * s, card.y + card.height * 0.5f - 22f * s, 128f * s, 44f * s),
                           "대화", UiTheme.Button))
            {
                if (TrySpendAction("동료 대화"))
                {
                    talk = new DialogueController(BuildCompanionTalk(id), root);
                    root.Session.actionsTaken++;
                }
            }
            y += cardH + 12f * s;
        }
        GUI.Label(new Rect(r.x, y + 4f * s, r.width, 26f * s), "── 이후 합류 예정 ──", UiTheme.SmallMuted);
        y += 34f * s;
        string[] locked = { CompanionCatalog.JinSeoyul, CompanionCatalog.SeoA, CompanionCatalog.HanBiyeon };
        foreach (string id in locked)
        {
            CompanionInfo info = CompanionCatalog.Info(id);
            if (info == null || root.Session.HasCompanion(id))
                continue;
            GUI.Label(new Rect(r.x, y, r.width, 26f * s),
                      $"🔒 {info.name} — {info.region} {info.sectName} / {info.element} / {info.weapon}",
                      UiTheme.SmallMuted);
            y += 28f * s;
        }
    }

    private void DrawSect(Rect r, float s)
    {
        GUI.Label(new Rect(r.x, r.y, r.width, 36f * s), "문파 — " + root.Session.sectName, UiTheme.Heading);
        float y = r.y + 46f * s;
        Line(r.x, ref y, r.width, s, "위명", root.Reputation.Get(FactionIds.JoseonSects).ToString());
        Line(r.x, ref y, r.width, s, "조선문파연합 결속", root.Reputation.Get(FactionIds.JoseonSects).ToString());
        Line(r.x, ref y, r.width, s, "중원무림맹 적대",
             (-root.Reputation.Get(FactionIds.ZhongyuanAlliance)).ToString());
        Line(r.x, ref y, r.width, s, "조정 관심", root.Reputation.Get(FactionIds.RoyalCourt).ToString());
        Line(r.x, ref y, r.width, s, "은전", root.Flags.GetInt("silver").ToString());
        Line(r.x, ref y, r.width, s, "제자 / 부상자",
             $"{Mathf.Max(6, root.Session.recruitedCompanionIds.Count + 4)}명 / {InjuredCount()}명");
        Line(r.x, ref y, r.width, s, "보급",
             $"약재 {root.Flags.GetInt("supply:medicine")} · 무기 {root.Flags.GetInt("supply:weapons")}");
        y += 8f * s;

        GUI.Label(new Rect(r.x, y, r.width, 30f * s), "자유행동", UiTheme.Heading);
        y += 38f * s;
        if (GUI.Button(new Rect(r.x, y, r.width * 0.46f, 44f * s), "검각 보수", UiTheme.Button))
        {
            if (TrySpendAction("검각 보수"))
            {
                root.Flags.AddInt("sect:repair", 2);
                root.Reputation.Add(FactionIds.JoseonSects, +1);
                root.Session.actionsTaken++;
                ShowToast("문파 복구 +2");
                AddLog("무너진 처마와 연무장 바닥을 손봤다. 문파 복구 +2, 조선문파연합 위명 +1.");
            }
        }

        if (GUI.Button(new Rect(r.x + r.width * 0.50f, y, r.width * 0.46f, 44f * s), "소백촌 순찰", UiTheme.Button))
        {
            if (TrySpendAction("소백촌 순찰"))
            {
                root.Flags.AddInt("sect:village_trust", 2);
                root.Flags.AddInt("silver", 10);
                root.Session.actionsTaken++;
                ShowToast("마을 신뢰 +2");
                AddLog("소백촌 길목을 살피고 잡일을 도왔다. 마을 신뢰 +2, 은전 +10.");
            }
        }
        y += 58f * s;

        GUI.Label(new Rect(r.x, y, r.width, 30f * s), "문파 기조 (정책)", UiTheme.Heading);
        y += 38f * s;
        HeroDisposition[] all = { HeroDisposition.Royal, HeroDisposition.Chivalrous, HeroDisposition.Conqueror,
                                  HeroDisposition.Romantic };
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
                AddLog($"{root.Session.sectName}의 기조가 {StoryEnumLabels.Label(all[i])}(으)로 바뀌었다.");
            }
        }
        y += 52f * s;
        GUI.Label(new Rect(r.x, y, r.width, 60f * s), "정책 효과 — " + PolicyEffect(root.Session.heroDisposition),
                  UiTheme.Small);
        y += 72f * s;

        GUI.Label(new Rect(r.x, y, r.width, 28f * s), "세력 평판", UiTheme.Heading);
        y += 34f * s;
        FactionMeter(r.x, ref y, r.width, s, FactionIds.JoseonSects);
        FactionMeter(r.x, ref y, r.width, s, FactionIds.ZhongyuanAlliance);
        FactionMeter(r.x, ref y, r.width, s, FactionIds.MurimInspectors);
        FactionMeter(r.x, ref y, r.width, s, FactionIds.RoyalCourt);
        FactionMeter(r.x, ref y, r.width, s, FactionIds.BlackHatGuild);
        GUI.Label(new Rect(r.x, y + 4f * s, r.width, 36f * s),
                  "최근 변화: 백두산 주변에서 검은 표식이 발견되고, 중원 하위 문파가 영맥을 엿본다는 소문이 돈다.",
                  UiTheme.SmallMuted);
    }

    private static string PolicyEffect(HeroDisposition d)
    {
        switch (d)
        {
        case HeroDisposition.Royal:
            return "왕도: 명성 획득 증가, 적 항복 유도에 유리. 사파/마교와 마찰 가능.";
        case HeroDisposition.Chivalrous:
            return "협도: 민심 보너스, 약자 보호 보조 목표에 유리. 협박 선택지 약화.";
        case HeroDisposition.Conqueror:
            return "패도: 적 기세 감소에 강함. 선한 동료 승인도 리스크.";
        case HeroDisposition.Romantic:
            return "풍류: 대화·도발 보너스. 실패 시 승인도/적대 리스크.";
        default:
            return string.Empty;
        }
    }

    private void DrawTavern(Rect r, float s)
    {
        GUI.Label(new Rect(r.x, r.y, r.width, 36f * s), "객잔", UiTheme.Heading);
        GUI.Label(new Rect(r.x, r.y + 46f * s, r.width, 28f * s), "무림의 소문이 술잔을 타고 흐른다.",
                  UiTheme.SmallMuted);
        Rect quote = new Rect(r.x, r.y + 80f * s, r.width, 90f * s);
        UiTheme.DrawFill(quote, UiTheme.HanjiPanelAlt);
        GUI.Label(new Rect(quote.x + 12f * s, quote.y + 10f * s, quote.width - 24f * s, quote.height - 20f * s),
                  string.IsNullOrEmpty(rumor) ? "..." : rumor,
                  new GUIStyle(UiTheme.Body) { fontStyle = FontStyle.Italic });
        if (rumorData != null)
        {
            GUI.Label(
                new Rect(r.x, r.y + 174f * s, r.width, 24f * s),
                $"관련 세력: {FactionIds.Label(rumorData.relatedFaction)} · 위험도 {rumorData.dangerLevel} · 단서 {SafeHint(rumorData.missionHintId)}",
                UiTheme.SmallMuted);
        }
        if (GUI.Button(new Rect(r.x, r.y + 208f * s, r.width * 0.5f, 46f * s), "소문 더 듣기", UiTheme.Button))
        {
            if (TrySpendAction("객잔 소문"))
            {
                root.Session.actionsTaken++;
                root.Flags.AddInt("rumor:clues", 1);
                RefreshRumor("tavern" + root.Session.actionsTaken);
                AddLog("객잔에서 중원 사신로와 동료 영입 단서를 들었다. 소문 단서 +1.");
            }
        }
        if (GUI.Button(new Rect(r.x + r.width * 0.54f, r.y + 208f * s, r.width * 0.42f, 46f * s), "품팔이 찾기",
                       UiTheme.Button))
        {
            if (TrySpendAction("객잔 품팔이"))
            {
                root.Session.actionsTaken++;
                root.Flags.AddInt("silver", 35);
                root.Flags.AddInt("sect:village_trust", 1);
                ShowToast("은전 +35");
                AddLog("객잔 주인의 일감을 받아 장작과 심부름을 처리했다. 은전 +35, 마을 신뢰 +1.");
            }
        }
        GUI.Label(new Rect(r.x, r.y + 268f * s, r.width, 60f * s),
                  "· 서브 의뢰와 동료 영입 소문은 이후 버전에서 열립니다.", UiTheme.SmallMuted);
    }

    private void DrawInfirmary(Rect r, float s)
    {
        GUI.Label(new Rect(r.x, r.y, r.width, 36f * s), "의원", UiTheme.Heading);
        BattleResultData last = root.Session.lastBattleResult;
        float y = r.y + 50f * s;
        if (last != null && last.woundedCompanions.Count > 0)
        {
            GUI.Label(new Rect(r.x, y, r.width, 28f * s), "치료가 필요한 동료:", UiTheme.Body);
            y += 34f * s;
            foreach (string id in last.woundedCompanions)
            {
                GUI.Label(new Rect(r.x + 10f * s, y, r.width - 10f * s, 26f * s),
                          "· " + CompanionCatalog.Name(id) + " (부상)", UiTheme.Small);
                y += 28f * s;
            }
            if (GUI.Button(new Rect(r.x, y + 8f * s, r.width * 0.6f, 48f * s), "치료하기", UiTheme.ButtonPrimary))
            {
                if (TrySpendAction("의원 치료"))
                {
                    last.woundedCompanions.Clear();
                    root.Flags.AddInt("supply:medicine", -Mathf.Min(1, Mathf.Max(0, root.Flags.GetInt("supply:medicine"))));
                    ShowToast("동료의 상처를 다스렸다.");
                    AddLog("의원에서 동료들의 부상을 치료했다. 다음 출정 준비가 가벼워졌다.");
                }
            }
        }
        else
        {
            GUI.Label(new Rect(r.x, y, r.width, 60f * s),
                      "지금은 치료가 필요한 동료가 없다.\n전투에서 다친 동료가 생기면 이곳에서 회복시킨다.",
                      UiTheme.Body);
            if (GUI.Button(new Rect(r.x, y + 78f * s, r.width * 0.54f, 46f * s), "약초 달이기", UiTheme.Button))
            {
                if (TrySpendAction("약초 달이기"))
                {
                    root.Flags.AddInt("supply:medicine", 1);
                    root.Session.actionsTaken++;
                    ShowToast("약재 +1");
                    AddLog("초희를 도와 약초를 달였다. 약재 보급 +1.");
                }
            }
        }
    }

    private void DrawMarket(Rect r, float s)
    {
        GUI.Label(new Rect(r.x, r.y, r.width, 36f * s), "장터", UiTheme.Heading);
        GUI.Label(new Rect(r.x, r.y + 44f * s, r.width, 26f * s), $"보유 은전: {root.Flags.GetInt("silver")}",
                  UiTheme.Body);
        float y = r.y + 84f * s;
        BuyRow(r, ref y, s, "medicine_bundle", "약재 꾸러미", 40, "전투 후 회복에 쓰인다.");
        BuyRow(r, ref y, s, "inner_power_pill", "내공단", 60, "내공 회복 소모품.");
        BuyRow(r, ref y, s, "throwing_dagger_bundle", "투척 비수 묶음", 30, "암기 보급.");
        y += 8f * s;
        GUI.Label(new Rect(r.x, y, r.width, 26f * s), "보유품", UiTheme.Heading);
        y += 34f * s;
        foreach (InventoryStack stack in root.Inventory.AllStacks())
        {
            GUI.Label(new Rect(r.x + 8f * s, y, r.width - 16f * s, 24f * s),
                      $"· {InventoryService.Label(stack.itemId)} x{stack.count}", UiTheme.Small);
            y += 26f * s;
        }
        GUI.Label(new Rect(r.x, y + 6f * s, r.width, 40f * s), "· 장비/무공 상점은 이후 버전에서 확장됩니다.",
                  UiTheme.SmallMuted);
    }

    private void BuyRow(Rect r, ref float y, float s, string itemId, string item, int price, string desc)
    {
        Rect row = new Rect(r.x, y, r.width, 54f * s);
        GUI.Label(new Rect(row.x, row.y + 4f * s, row.width * 0.5f, 26f * s), item, UiTheme.Body);
        GUI.Label(new Rect(row.x, row.y + 28f * s, row.width * 0.6f, 22f * s), desc, UiTheme.SmallMuted);
        bool canBuy = root.Flags.GetInt("silver") >= price;
        GUI.enabled = canBuy;
        if (GUI.Button(new Rect(row.xMax - 150f * s, row.y + 6f * s, 140f * s, 42f * s), $"구매 ({price})",
                       UiTheme.Button))
        {
            if (root.Inventory.Purchase(root.Flags, itemId, 1, price))
            {
                ShowToast($"{item} 구매");
                AddLog($"장터에서 {item}을(를) 샀다. (-{price}은전, 보유 {root.Inventory.GetCount(itemId)})");
            }
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
        GUI.Label(new Rect(body.x + 14f * s, body.y + 12f * s, body.width - 28f * s, body.height - 24f * s),
                  LoreText(loreIndex), UiTheme.Body);

        if (GUI.Button(new Rect(body.xMax - 150f * s, body.yMax - 48f * s, 132f * s, 36f * s), "연구 기록",
                       UiTheme.Button))
        {
            if (TrySpendAction("서고 연구"))
            {
                root.Flags.AddInt("growth:research_xp", 5);
                if (root.Flags.GetInt("growth:research_xp") >= 10)
                {
                    root.Flags.SetFlag("skill_hint:cheongwang_breath");
                }

                root.Session.actionsTaken++;
                ShowToast("무공 연구 +5");
                AddLog("서고에서 백두산 영맥과 천광검문의 전승 기록을 대조했다. 무공 연구 +5.");
            }
        }
    }

    private static string LoreText(int i)
    {
        switch (i)
        {
        case 0:
            return "백두천광검문은 백두산 천지의 새벽빛을 검에 담는 정파였다. 지금은 문도 대부분이 흩어지고 낡은 " +
                   "검각과 병든 문주, 외동아들 박성준만 남았다. 1장은 수련, 생계, 문파 복구, 마을 신뢰를 쌓으며 " +
                   "꺼져가는 천광을 다시 살리는 이야기다.";
        case 1:
            return "· 백두천광검문: 북방을 지키던 조선 정파. 백두산 영맥과 천광검문 비급을 품고 있다.\n· 철랑문: " +
                   "백두산 주변을 괴롭히는 중원 하위 문파. 2장의 첫 적대 세력.\n· 모용세가: 더 큰 그림을 가진 " +
                   "오대세가 중 하나. 3장에서 본격적으로 그림자를 드리운다.\n· 소백촌: 문파 아래 마을. 신뢰와 생계, " +
                   "재건의 중심.";
        case 2:
            return "· 천광심법: 빛 속성 내공. 정화, 명중, 상태 저항과 연결된다.\n· 백야검결: 박성준의 주력 검법. " +
                   "밤에도 꺼지지 않는 검기.\n· 새벽일섬: 초반 필살기. 어둠·독·공포 계열을 끊는 빛의 베기.\n· " +
                   "광명호신기: 아군 보호와 방어를 돕는 천광검문의 호신기.";
        default:
            return string.Empty;
        }
    }

    private void DrawSave(Rect r, float s)
    {
        GUI.Label(new Rect(r.x, r.y, r.width, 36f * s), "저장", UiTheme.Heading);
        GUI.Label(new Rect(r.x, r.y + 44f * s, r.width, 30f * s), "자동 저장 1개 + 수동 슬롯 3개", UiTheme.SmallMuted);
        float y = r.y + 86f * s;

        DrawSaveSlot(r.x, ref y, r.width, s, SaveManager.AutoSlot);
        foreach (string slot in SaveManager.ManualSlots)
        {
            DrawSaveSlot(r.x, ref y, r.width, s, slot);
        }

        if (!string.IsNullOrEmpty(pendingOverwriteSlot))
        {
            Rect confirm = new Rect(r.x, r.yMax - 70f * s, r.width, 56f * s);
            UiTheme.DrawFill(confirm, new Color(0.706f, 0.220f, 0.169f, 0.14f));
            GUI.Label(new Rect(confirm.x + 12f * s, confirm.y + 12f * s, confirm.width - 250f * s, 28f * s),
                      $"슬롯 {SlotLabel(pendingOverwriteSlot)}에 덮어쓸까요?", UiTheme.Body);
            if (GUI.Button(new Rect(confirm.xMax - 220f * s, confirm.y + 8f * s, 96f * s, 40f * s), "덮어쓰기",
                           UiTheme.ButtonPrimary))
            {
                SaveToSlot(pendingOverwriteSlot);
                pendingOverwriteSlot = null;
            }

            if (GUI.Button(new Rect(confirm.xMax - 112f * s, confirm.y + 8f * s, 96f * s, 40f * s), "취소",
                           UiTheme.Button))
            {
                pendingOverwriteSlot = null;
            }
        }
    }

    private void DrawSettings(Rect r, float s)
    {
        GUI.Label(new Rect(r.x, r.y, r.width, 36f * s), "설정", UiTheme.Heading);
        float y = r.y + 52f * s;
        Slider(r.x, ref y, r.width, s, "BGM 볼륨", ref settings.bgmVolume);
        Slider(r.x, ref y, r.width, s, "효과음 볼륨", ref settings.sfxVolume);
        Slider(r.x, ref y, r.width, s, "텍스트 속도", ref settings.textSpeed);
        Slider(r.x, ref y, r.width, s, "자동 텍스트 속도", ref settings.autoTextSpeed);
        Slider(r.x, ref y, r.width, s, "주사위 애니메이션", ref settings.diceAnimationSpeed);
        Slider(r.x, ref y, r.width, s, "UI 크기", ref settings.uiScale, 0.8f, 1.4f);

        settings.fullscreen =
            GUI.Toggle(new Rect(r.x, y, r.width, 28f * s), settings.fullscreen, "전체화면/창모드", UiTheme.Body);
        y += 34f * s;
        settings.screenShake =
            GUI.Toggle(new Rect(r.x, y, r.width, 28f * s), settings.screenShake, "화면 흔들림", UiTheme.Body);
        y += 34f * s;
        settings.autoDialogue =
            GUI.Toggle(new Rect(r.x, y, r.width, 28f * s), settings.autoDialogue, "자동 대화", UiTheme.Body);
        y += 34f * s;
        settings.choiceEffectPreview = GUI.Toggle(new Rect(r.x, y, r.width, 28f * s), settings.choiceEffectPreview,
                                                  "선택지 효과 미리보기", UiTheme.Body);
        y += 34f * s;
        settings.damageNumbers =
            GUI.Toggle(new Rect(r.x, y, r.width, 28f * s), settings.damageNumbers, "피해 숫자 표시", UiTheme.Body);
        y += 34f * s;
        settings.detailedCombatMath = GUI.Toggle(new Rect(r.x, y, r.width, 28f * s), settings.detailedCombatMath,
                                                 "전투 상세 계산 표시", UiTheme.Body);
        y += 42f * s;

        GUI.Label(new Rect(r.x, y, r.width, 24f * s), "해상도", UiTheme.SmallMuted);
        y += 26f * s;
        settings.resolutionIndex =
            GUI.SelectionGrid(new Rect(r.x, y, r.width * 0.72f, 38f * s), Mathf.Clamp(settings.resolutionIndex, 0, 2),
                              new[] { "1280x720", "1600x900", "1920x1080" }, 3, UiTheme.Button);
        y += 54f * s;

        if (GUI.Button(new Rect(r.x, y, r.width * 0.34f, 52f * s), "설정 저장", UiTheme.ButtonPrimary))
        {
            settings.Save();
            ShowToast("설정 저장");
        }

        if (GUI.Button(new Rect(r.x + r.width * 0.38f, y, r.width * 0.34f, 52f * s), "타이틀로", UiTheme.Button))
        {
            settings.Save();
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
            if (info == null)
                continue;
            GUI.Label(new Rect(rect.x + 18f * s, y, rect.width - 36f * s, 28f * s), info.name, UiTheme.Body);
            GUI.Label(new Rect(rect.x + 18f * s, y + 28f * s, rect.width - 36f * s, 24f * s), info.role,
                      UiTheme.SmallMuted);
            Rect barBg = new Rect(rect.x + 18f * s, y + 54f * s, rect.width - 36f * s, 10f * s);
            UiTheme.DrawFill(barBg, UiTheme.HanjiPanelAlt);
            float frac = Mathf.Clamp01(root.Approval.Get(id) / 100f);
            UiTheme.DrawFill(new Rect(barBg.x, barBg.y, barBg.width * frac, barBg.height), UiTheme.Teal);
            GUI.Label(new Rect(rect.x + 18f * s, y + 64f * s, rect.width - 36f * s, 22f * s),
                      root.Approval.GetStageLabel(id), UiTheme.SmallMuted);
            y += 104f * s;
        }
    }

    private void DrawSaveSlot(float x, ref float y, float w, float s, string slot)
    {
        SaveSlotSummary summary = root.Save.Peek(slot);
        Rect row = new Rect(x, y, w, 64f * s);
        UiTheme.DrawPanel(row, true);

        GUI.Label(new Rect(row.x + 14f * s, row.y + 8f * s, row.width * 0.24f, 24f * s), SlotLabel(slot), UiTheme.Body);
        string detail =
            summary.exists
                ? $"{summary.sectName} · {summary.chapterTitle} · {summary.playTimeText} · {summary.savedAtText}"
                : "비어 있음";
        GUI.Label(new Rect(row.x + 14f * s, row.y + 34f * s, row.width - 250f * s, 22f * s), detail,
                  UiTheme.SmallMuted);

        if (GUI.Button(new Rect(row.xMax - 224f * s, row.y + 12f * s, 96f * s, 40f * s), "저장", UiTheme.ButtonPrimary))
        {
            if (summary.exists)
            {
                pendingOverwriteSlot = slot;
            }
            else
            {
                SaveToSlot(slot);
            }
        }

        GUI.enabled = summary.exists;
        if (GUI.Button(new Rect(row.xMax - 116f * s, row.y + 12f * s, 96f * s, 40f * s), "불러오기", UiTheme.Button))
        {
            GameSession loaded = root.Save.Load(slot);
            if (loaded != null)
            {
                root.LoadExistingSession(loaded);
                ShowToast("불러왔습니다.");
                AddLog($"{SlotLabel(slot)}에서 진행을 불러왔다.");
            }
        }
        GUI.enabled = true;

        y += 74f * s;
    }

    private void SaveToSlot(string slot)
    {
        bool ok = root.Save.Save(root.Session, slot);
        ShowToast(ok ? "저장되었습니다." : "저장에 실패했습니다.");
        AddLog(ok ? $"{SlotLabel(slot)}에 진행 상황을 기록했다." : "저장에 실패했다.");
    }

    private void RefreshRumor(string sourceId)
    {
        if (root.Narration is MockAINarrationService mock)
        {
            rumorData = mock.GenerateRumorData(sourceId, root.Session);
            rumor = rumorData != null ? rumorData.rumorText : string.Empty;
        }
        else
        {
            rumorData = null;
            rumor = root.Narration != null ? root.Narration.GenerateNpcLine(sourceId, root.Session) : string.Empty;
        }
    }

    private static string SafeHint(string hint)
    {
        return string.IsNullOrEmpty(hint) ? "없음" : hint;
    }

    private static string SlotLabel(string slot)
    {
        return slot == SaveManager.AutoSlot ? "자동 저장" : "슬롯 " + slot;
    }

    private int ActionsRemaining => Mathf.Clamp(root.Flags.GetInt(ActionPointKey), 0, MaxDailyActions);

    private int DayIndex => Mathf.Max(1, root.Flags.GetInt("calendar:day") + 1);

    private void BeginNextDay()
    {
        root.Flags.AddInt("calendar:day", 1);
        root.Flags.SetInt(ActionPointKey, MaxDailyActions);
        root.Flags.SetFlag(ActionPointInitializedFlag);
        ShowToast($"제{DayIndex}일 · 기력 회복");
        AddLog("밤을 넘기고 새벽 수련종이 울렸다. 하루가 지나 기력이 모두 회복되었다.");
    }

    private void EnsureActionPoints()
    {
        if (!root.Flags.HasFlag(ActionPointInitializedFlag))
        {
            root.Flags.SetInt(ActionPointKey, MaxDailyActions);
            root.Flags.SetFlag(ActionPointInitializedFlag);
        }
    }

    private bool TrySpendAction(string label)
    {
        int remaining = ActionsRemaining;
        if (remaining <= 0)
        {
            ShowToast("기력이 부족합니다.");
            AddLog($"{label}: 기력이 부족하다. 하루를 보내고 다시 행동할 수 있다.");
            return false;
        }

        root.Flags.SetInt(ActionPointKey, remaining - 1);
        return true;
    }

    private void ApplyTraining(int drillIndex)
    {
        root.Session.actionsTaken++;
        root.Flags.AddInt("growth:martial_xp", 8);

        switch (drillIndex)
        {
        case 0:
            root.Flags.AddInt("growth:inner_art_xp", 8);
            root.Reputation.Add(FactionIds.JoseonSects, +1);
            if (root.Flags.GetInt("growth:inner_art_xp") >= 16)
            {
                root.Flags.SetFlag("skill_hint:cheongwang_breath");
            }

            ShowToast("천광심법 +8");
            AddLog("천지의 빛을 호흡에 맞췄다. 천광심법 +8, 조선문파연합 위명 +1.");
            break;
        case 1:
            root.Flags.AddInt("growth:sword_xp", 8);
            if (root.Flags.GetInt("growth:sword_xp") >= 16)
            {
                root.Flags.SetFlag("skill_hint:baegya_first_form");
            }

            ShowToast("백야검결 +8");
            AddLog("백야검결의 첫 검로를 몸에 새겼다. 검법 숙련 +8.");
            break;
        default:
            root.Flags.AddInt("growth:teamwork_xp", 8);
            foreach (string id in root.Session.recruitedCompanionIds)
            {
                root.Approval.Add(id, +1);
            }

            ShowToast("합련 +8");
            AddLog("동료들과 속성 연계를 맞췄다. 합련 +8, 합류 동료 호감 +1.");
            break;
        }
    }

    private string CompanionBadge()
    {
        return root.Session.recruitedCompanionIds.Count > 0 ? "!" : string.Empty;
    }

    private int InjuredCount()
    {
        BattleResultData last = root.Session.lastBattleResult;
        return last != null && last.woundedCompanions != null ? last.woundedCompanions.Count : 0;
    }

    private bool IsWounded(string companionId)
    {
        BattleResultData last = root.Session.lastBattleResult;
        return last != null && last.woundedCompanions != null && last.woundedCompanions.Contains(companionId);
    }

    private static string CompanionConcern(string companionId)
    {
        switch (companionId)
        {
        case "baek_ryeon":
            return "부상자와 약재 부족";
        case "do_arin":
            return "정면승부로 문파 명예 회복";
        case "jin_seoyul":
            return "천뢰봉문 감전 사건의 진상";
        case "seo_a":
            return "작은 자신도 문파를 지킬 수 있다는 증명";
        case "han_biyeon":
            return "흑련암문 독살 누명 단서";
        default:
            return "문주의 결정을 지켜보는 중";
        }
    }

    private string NextCompanionTalk(string companionId)
    {
        int approval = root.Approval.Get(companionId);
        if (approval >= 80)
            return "맹약 이벤트 준비";
        if (approval >= 60)
            return "신뢰 대화 가능";
        if (approval >= 40)
            return "동행 대화 가능";
        return "경계 완화 필요";
    }

    private void FactionMeter(float x, ref float y, float w, float s, string factionId)
    {
        int value = root.Reputation.Get(factionId);
        GUI.Label(new Rect(x, y, w * 0.34f, 24f * s), FactionIds.Label(factionId), UiTheme.SmallMuted);
        Rect bg = new Rect(x + w * 0.34f, y + 7f * s, w * 0.50f, 10f * s);
        UiTheme.DrawFill(bg, UiTheme.HanjiPanelAlt);
        float normalized = Mathf.InverseLerp(FactionReputationService.Min, FactionReputationService.Max, value);
        UiTheme.DrawFill(new Rect(bg.x, bg.y, bg.width * normalized, bg.height),
                         value >= 0 ? UiTheme.Teal : UiTheme.SealRed);
        GUI.Label(new Rect(x + w * 0.86f, y, w * 0.14f, 24f * s), value.ToString(), UiTheme.SmallMuted);
        y += 28f * s;
    }

    private static void Slider(float x, ref float y, float w, float s, string label, ref float value, float min = 0f,
                               float max = 1f)
    {
        GUI.Label(new Rect(x, y, w * 0.34f, 26f * s), label, UiTheme.SmallMuted);
        value = GUI.HorizontalSlider(new Rect(x + w * 0.34f, y + 8f * s, w * 0.48f, 20f * s), value, min, max);
        GUI.Label(new Rect(x + w * 0.84f, y, w * 0.16f, 26f * s), value.ToString("0.00"), UiTheme.SmallMuted);
        y += 34f * s;
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

    private void ShowToast(string text)
    {
        toast = text;
        toastTimer = 2.0f;
    }

    private void AddLog(string text)
    {
        log.Add(text);
        if (log.Count > 20)
            log.RemoveAt(0);
    }

    private static DialogueScript BuildCompanionTalk(string id)
    {
        DialogueScript d = new DialogueScript();
        CompanionInfo info = CompanionCatalog.Info(id);
        string name = info != null ? info.name : id;

        if (id == CompanionCatalog.BaekRyeon)
        {
            d.Add(new DialogueNode("t0", name,
                                   "“창끝은 차갑게 두겠습니다. 다만, 사람을 살릴 길까지 얼리지는 말아 주세요.”", "t1"));
            DialogueNode c = new DialogueNode("t1", "박성준", "(어떻게 답할까?)");
            c.choices.Add(
                new DialogueChoice("다친 제자들부터 살피자.", HeroDisposition.Chivalrous, "t2a").Approval(id, +3));
            c.choices.Add(new DialogueChoice("냉정하게 — 지금은 전열이 먼저다.", HeroDisposition.Conqueror, "t2b")
                              .Approval(id, -2));
            d.Add(c);
            d.Add(new DialogueNode("t2a", name, "백련이 조용히 고개를 숙인다. “…네. 그 말이면 충분합니다.”", null));
            d.Add(new DialogueNode("t2b", name,
                                   "백련의 눈빛이 잠시 얼어붙는다. “그 냉정함이 사람을 버리지 않길 바랍니다.”", null));
        }
        else if (id == CompanionCatalog.DoArin)
        {
            d.Add(new DialogueNode("t0", name, "“문주, 복잡하게 재지 말자. 저놈들이 밀고 오면, 내가 먼저 불길 열게.”",
                                   "t1"));
            DialogueNode c = new DialogueNode("t1", "박성준", "(어떻게 답할까?)");
            c.choices.Add(
                new DialogueChoice("좋다. 단, 혼자 앞서지 마라.", HeroDisposition.Royal, "t2a").Approval(id, +2));
            c.choices.Add(
                new DialogueChoice("앞장서라. 길은 힘으로 연다.", HeroDisposition.Conqueror, "t2b").Approval(id, +4));
            d.Add(c);
            d.Add(
                new DialogueNode("t2a", name, "도아린이 도집을 툭 친다. “알았어. 한 발만 먼저 간다, 한 발만.”", null));
            d.Add(new DialogueNode("t2b", name, "도아린이 씩 웃는다. “그 말 기다렸어.”", null));
        }
        else if (id == CompanionCatalog.JinSeoyul)
        {
            d.Add(new DialogueNode(
                "t0", name, "“문주님, 방금 지붕 물받이 봤어요? 저기 전기 흘리면 감찰단 발이 딱 멈출걸요?”", null));
        }
        else if (id == CompanionCatalog.SeoA)
        {
            d.Add(new DialogueNode(
                "t0", name, "“나도 할 수 있어요! 작다고 얕보면 안 된다구요. 꽃바람은 낮게 불 때 더 잘 스며들어요.”",
                null));
        }
        else if (id == CompanionCatalog.HanBiyeon)
        {
            d.Add(new DialogueNode("t0", name,
                                   "“정면으로 부딪히는 건 취향이 아니야. 대신, 구월산 그림자길은 내가 볼게.”", null));
        }
        else
        {
            d.Add(new DialogueNode("t0", name, "“…아직 그대를 다 믿지는 않소.”", null));
        }

        return d;
    }
}
}
