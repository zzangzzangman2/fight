using System;
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
    public const string BanditLairBattleId = "BATTLE_SOBAEK_BANDIT_LAIR";
    public const string WolfPassBattleId = "BATTLE_SOBAEK_WOLF_PASS";
    public const string TigerRavineBattleId = "BATTLE_SOBAEK_TIGER_RAVINE";
    public const string LeopardCliffBattleId = "BATTLE_SOBAEK_LEOPARD_CLIFF";
    public const string SeorakPassRescueBattleId = "BATTLE_CH1_SEORAK_PASS_RESCUE";
    private const int MaxDailyActions = 3;
    public const string ActionPointKey = "hub:daily_actions_remaining";
    private const string ActionPointInitializedFlag = "FLAG_HUB_ACTION_POINTS_READY";

    private enum HubMenu
    {
        Overview,
        Sortie,
        Training,
        Companions,
        Equipment,
        Sect,
        Tavern,
        Infirmary,
        Market,
        Library,
        Save,
        Settings
    }

    private enum HubMapAction
    {
        None,
        OpenMissionBoard,
        OpenBattlePrep,
        OpenMenu
    }

    private sealed class HubMapInfo
    {
        public string id;
        public string title;
        public string subtitle;
        public string category;
        public string status;
        public string description;
        public string rewardSummary;
        public string dangerSummary;
        public string artResource;
        public string actionLabel;
        public string battleId;
        public HubMenu targetMenu;
        public HubMapAction action;
    }

    private GameRoot root;
    private HubMenu menu = HubMenu.Overview;
    private string pinnedHubMapInfoId = "mission_gate";
    private string hoveredHubMapInfoId;
    private static readonly Dictionary<string, Texture2D> HubMapInfoArtCache =
        new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);
    private static readonly string[] FocusTrainingKeys =
    {
        MurimStatFormula.HpKey,
        MurimStatFormula.InnerKey,
        MurimStatFormula.StrengthKey,
        MurimStatFormula.AgilityKey,
        MurimStatFormula.InnerPowerKey,
        MurimStatFormula.SpiritKey,
        MurimStatFormula.InsightKey,
        MurimStatFormula.CharmKey
    };
    private DialogueController talk;
    private readonly List<string> log = new List<string>();
    private string toast;
    private float toastTimer;
    private int loreIndex;
    private string rumor;
    private RumorData rumorData;
    private string pendingOverwriteSlot;
    private GameSettings settings;
    private Texture2D hubMapTexture;
    private HubEquipmentPanel equipmentPanel;
    private int marketTab;
    private Vector2 marketScroll;
    private string giftTargetId;
    private Vector2 giftScroll;
    private string visitCompanionId;
    private Vector2 visitScroll;

    private void Awake()
    {
        root = GameRoot.EnsureExists();
        EnsureActionPoints();
        settings = GameSettings.Load();
        hubMapTexture = Resources.Load<Texture2D>("UI/hub_free_time_map_v1");
        equipmentPanel = new HubEquipmentPanel(root, ShowToast, AddLog);
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
        bool overviewMap = menu == HubMenu.Overview || menu == HubMenu.Equipment;
        float rightW = overviewMap ? 0f : 300f * s;
        float rightGap = overviewMap ? 0f : 16f * s;
        float centerX = margin + menuW + 16f * s;
        float centerW = w - margin - rightW - rightGap - centerX;

        DrawMenu(new Rect(margin, top, menuW, bottom - top), s);
        DrawContent(new Rect(centerX, top, centerW, bottom - top), s);
        if (!overviewMap)
        {
            DrawCompanionSummary(new Rect(w - margin - rightW, top, rightW, bottom - top), s);
        }

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
            $"제{DayIndex}일   기력 {ActionsRemaining}/{MaxDailyActions}   위명 {root.Reputation.Get(FactionIds.JoseonSects)}   은냥 {root.Flags.GetInt("silver")}";
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
        MenuButton(ref y, x, bw, bh, gap, "장비", HubMenu.Equipment);
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
        DrawContentBackground(rect, s);
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
        case HubMenu.Equipment:
            equipmentPanel.Draw(inner, s);
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

    private void DrawContentBackground(Rect rect, float s)
    {
        Texture2D background = DialogueBackgroundRegistry.LoadBackgroundTexture(BackgroundIdFor(menu));
        if (background == null)
        {
            return;
        }

        Rect art = new Rect(rect.x + 8f * s, rect.y + 8f * s, rect.width - 16f * s, rect.height - 16f * s);
        GUI.DrawTexture(art, background, ScaleMode.ScaleAndCrop);
        UiTheme.DrawFill(art, new Color(0.000f, 0.010f, 0.012f, 0.66f));
        UiTheme.DrawBottomShade(new Rect(art.x, art.y + art.height * 0.36f, art.width, art.height * 0.64f));
    }

    private static string BackgroundIdFor(HubMenu hubMenu)
    {
        switch (hubMenu)
        {
        case HubMenu.Sortie:
        case HubMenu.Sect:
            return "bg_pyesadang_main_hall_day";
        case HubMenu.Training:
            return "bg_pyesadang_training_ground_evening";
        case HubMenu.Companions:
            return "bg_pyesadang_courtyard_dawn";
        case HubMenu.Equipment:
            return "bg_pyesadang_main_hall_day";
        case HubMenu.Tavern:
            return "bg_pyesadang_tavern_corner";
        case HubMenu.Infirmary:
            return "bg_pyesadang_infirmary";
        case HubMenu.Market:
            return "bg_pyesadang_market_stall";
        case HubMenu.Library:
            return "bg_pyesadang_library";
        case HubMenu.Save:
        case HubMenu.Settings:
            return "bg_pyesadang_main_hall_night";
        default:
            return "bg_pyesadang_courtyard_dawn";
        }
    }

    private void DrawOverview(Rect r, float s)
    {
        GUI.Label(new Rect(r.x, r.y, r.width, 36f * s), "소백촌 자유시간 지도", UiTheme.Heading);
        GUI.Label(new Rect(r.x, r.y + 38f * s, r.width, 34f * s),
                  "장소 표지판에 마우스를 올리면 오른쪽에 상세 정보가 뜨고, 클릭하면 그 정보를 고정한다.", UiTheme.Body);

        Rect status = new Rect(r.x, r.y + 78f * s, r.width, 38f * s);
        UiTheme.DrawFill(status, new Color(0.010f, 0.014f, 0.014f, 0.62f));
        GUI.Label(new Rect(status.x + 14f * s, status.y + 7f * s, status.width * 0.30f, 26f * s),
                  $"제{DayIndex}일 · 기력 {ActionsRemaining}/{MaxDailyActions}", UiTheme.Body);
        GUI.Label(new Rect(status.x + status.width * 0.30f, status.y + 8f * s, status.width * 0.54f, 24f * s),
                  $"수련 {root.Flags.GetInt("growth:martial_xp")} · 연구 {root.Flags.GetInt("growth:research_xp")} · 문파 복구 {root.Flags.GetInt("sect:repair")} · 마을 신뢰 {root.Flags.GetInt("sect:village_trust")}",
                  UiTheme.SmallMuted);
        if (GUI.Button(new Rect(status.xMax - 142f * s, status.y + 4f * s, 126f * s, 30f * s), "하루 보내기",
                       UiTheme.Button))
        {
            BeginNextDay();
        }

        Rect frame = new Rect(r.x, r.y + 128f * s, r.width, Mathf.Max(320f * s, r.height - 132f * s));
        UiTheme.DrawPanel(frame, true);
        bool sideBySide = frame.width >= 820f * s;
        Rect mapBounds;
        Rect infoRect;
        if (sideBySide)
        {
            float infoW = Mathf.Clamp(frame.width * 0.31f, 280f * s, 390f * s);
            infoRect = new Rect(frame.xMax - 10f * s - infoW, frame.y + 10f * s, infoW, frame.height - 20f * s);
            mapBounds = new Rect(frame.x + 10f * s, frame.y + 10f * s, infoRect.x - frame.x - 24f * s,
                                 frame.height - 20f * s);
        }
        else
        {
            float infoH = Mathf.Min(184f * s, frame.height * 0.38f);
            infoRect = new Rect(frame.x + 10f * s, frame.yMax - 10f * s - infoH, frame.width - 20f * s, infoH);
            mapBounds = new Rect(frame.x + 10f * s, frame.y + 10f * s, frame.width - 20f * s,
                                 infoRect.y - frame.y - 18f * s);
        }

        Rect map = FitAspect(mapBounds, 16f / 9f);
        hoveredHubMapInfoId = null;

        if (hubMapTexture != null)
        {
            GUI.DrawTexture(map, hubMapTexture, ScaleMode.ScaleAndCrop);
        }
        else
        {
            DrawFallbackHubMap(map, s);
        }

        UiTheme.DrawFill(map, new Color(0.000f, 0.010f, 0.012f, 0.12f));
        DrawMapCornerFrame(map, s);

        if (MapHotspot(map, 0.045f, 0.200f, 0.185f, 0.092f, s, "출정문", "임무 게시판", "!",
                       "mission_gate", HubMenu.Sortie))
        {
            pinnedHubMapInfoId = "mission_gate";
        }

        if (MapHotspot(map, 0.055f, 0.060f, 0.225f, 0.092f, s, "뒷산 도적 소굴", "기력 1 · 반복 의뢰",
                       ActionsRemaining > 0 ? "!" : "0", "bandit_lair", HubMenu.Sortie))
        {
            pinnedHubMapInfoId = "bandit_lair";
        }

        if (MapHotspot(map, 0.735f, 0.060f, 0.190f, 0.092f, s, "늑대 고개", "기력 1 · 방목길 방어",
                       ActionsRemaining > 0 ? "!" : "0", "wolf_pass", HubMenu.Sortie))
        {
            pinnedHubMapInfoId = "wolf_pass";
        }

        if (MapHotspot(map, 0.730f, 0.330f, 0.195f, 0.092f, s, "호랑이 바위골", "기력 1 · 주민 구조",
                       ActionsRemaining > 0 ? "!" : "0", "tiger_ravine", HubMenu.Sortie))
        {
            pinnedHubMapInfoId = "tiger_ravine";
        }

        if (MapHotspot(map, 0.755f, 0.690f, 0.180f, 0.092f, s, "표범 절벽길", "기력 1 · 약초길 호송",
                       ActionsRemaining > 0 ? "!" : "0", "leopard_cliff", HubMenu.Sortie))
        {
            pinnedHubMapInfoId = "leopard_cliff";
        }

        if (MapHotspot(map, 0.505f, 0.382f, 0.175f, 0.092f, s, "연무장", "수련 · 기력 1",
                       ActionsRemaining > 0 ? "" : "0", "training_yard", HubMenu.Training))
        {
            pinnedHubMapInfoId = "training_yard";
        }

        if (MapHotspot(map, 0.365f, 0.165f, 0.190f, 0.092f, s, "검각 본당", "문파 재건", "!", "sect_hall",
                       HubMenu.Sect))
        {
            pinnedHubMapInfoId = "sect_hall";
        }

        if (MapHotspot(map, 0.690f, 0.165f, 0.170f, 0.092f, s, "후산 정자", "동료 대화", CompanionBadge(),
                       "companion_deck", HubMenu.Companions))
        {
            pinnedHubMapInfoId = "companion_deck";
        }

        if (MapHotspot(map, 0.090f, 0.535f, 0.160f, 0.092f, s, "객잔", "소문 · 일감", "!", "tavern",
                       HubMenu.Tavern))
        {
            pinnedHubMapInfoId = "tavern";
        }

        if (MapHotspot(map, 0.300f, 0.705f, 0.165f, 0.092f, s, "장터", "보급 · 선물 · 장비", "", "market",
                       HubMenu.Market))
        {
            pinnedHubMapInfoId = "market";
        }

        if (MapHotspot(map, 0.560f, 0.735f, 0.170f, 0.092f, s, "서고", "무공 연구", root.Flags.HasFlag(StoryFlags.FirstBattleWon) ? "!" : "",
                       "library", HubMenu.Library))
        {
            pinnedHubMapInfoId = "library";
        }

        if (MapHotspot(map, 0.755f, 0.520f, 0.165f, 0.092f, s, "의원", "치료 · 약초", InjuredCount() > 0 ? "!" : "",
                       "infirmary", HubMenu.Infirmary))
        {
            pinnedHubMapInfoId = "infirmary";
        }

        string activeInfoId = !string.IsNullOrEmpty(hoveredHubMapInfoId) ? hoveredHubMapInfoId : pinnedHubMapInfoId;
        DrawHubMapInfoPanel(infoRect, HubMapInfoFor(activeInfoId), s);
    }

    private void OpenFreeTimeBattlePrep(string battleId, string label)
    {
        if (ActionsRemaining <= 0)
        {
            ShowToast("기력이 부족합니다.");
            AddLog(label + ": 기력이 부족하다. 하루를 보내고 다시 행동할 수 있다.");
            return;
        }

        root.Flow.GoToBattlePrep(battleId);
    }

    private HubMapInfo HubMapInfoFor(string id)
    {
        switch (id)
        {
        case "bandit_lair":
            return FreeTimeBattleMapInfo(id, BanditLairBattleId, "뒷산 도적 소굴");
        case "wolf_pass":
            return FreeTimeBattleMapInfo(id, WolfPassBattleId, "늑대 고개");
        case "tiger_ravine":
            return FreeTimeBattleMapInfo(id, TigerRavineBattleId, "호랑이 바위골");
        case "leopard_cliff":
            return FreeTimeBattleMapInfo(id, LeopardCliffBattleId, "표범 절벽길");
        case "training_yard":
            return new HubMapInfo
            {
                id = id,
                artResource = "UI/HubLocationCards/hub_location_training_yard",
                title = "연무장",
                subtitle = "무공 수련 · 조작 감각 회복",
                category = "자유시간 행동",
                status = ActionsRemaining > 0 ? $"기력 1 소모 가능 · 남은 기력 {ActionsRemaining}" : "기력 부족 · 하루 보내기 필요",
                description = "천광심법 호흡, 백야검결 검로, 동료 합련을 진행한다. 전투 전 성장치를 쌓고 조작 순서를 익히는 장소다.",
                rewardSummary = "천광심법 / 백야검결 / 합련 숙련 상승",
                dangerSummary = "기력을 쓰는 행동이므로 출정 의뢰와 같은 날 우선순위를 정해야 한다.",
                action = HubMapAction.OpenMenu,
                targetMenu = HubMenu.Training,
                actionLabel = "연무장 열기"
            };
        case "sect_hall":
            return new HubMapInfo
            {
                id = id,
                artResource = "UI/HubLocationCards/hub_location_sect_hall",
                title = "검각 본당",
                subtitle = "문파 재건 · 기조 정리",
                category = "거점 정비",
                status = $"문파 복구 {root.Flags.GetInt("sect:repair")} · 위명 {root.Reputation.Get(FactionIds.JoseonSects)}",
                description = "낡은 검각을 손보고 문파 기조, 평판, 장기 목표를 확인한다. 백두천광검문을 다시 세우는 중심 공간이다.",
                rewardSummary = "문파 복구, 조선문파연합 위명, 장기 정책 확인",
                dangerSummary = "재건 선택은 향후 평판과 세력 반응에 이어질 수 있다.",
                action = HubMapAction.OpenMenu,
                targetMenu = HubMenu.Sect,
                actionLabel = "문파 메뉴 열기"
            };
        case "companion_deck":
            return new HubMapInfo
            {
                id = id,
                artResource = "UI/HubLocationCards/hub_location_companion_deck",
                title = "후산 정자",
                subtitle = "동료 대화 · 선물 · 방문",
                category = "동료",
                status = CompanionBadge() == "!" ? "오늘 방문 가능한 동료 있음" : "동료 상태 확인",
                description = "동료와 시간을 보내고 선물을 건네며 현재 고민과 전투 지원 효과를 확인한다.",
                rewardSummary = "연애도, 유대 단계, 지원 효과 단서",
                dangerSummary = "동료 방문은 하루 단위 제한이 있으므로 필요한 대화를 먼저 챙기자.",
                action = HubMapAction.OpenMenu,
                targetMenu = HubMenu.Companions,
                actionLabel = "동료 메뉴 열기"
            };
        case "tavern":
            return new HubMapInfo
            {
                id = id,
                artResource = "UI/HubLocationCards/hub_location_tavern",
                title = "객잔",
                subtitle = "소문 · 일감 · 마을 정보",
                category = "자유시간 행동",
                status = ActionsRemaining > 0 ? $"기력 행동 가능 · 남은 기력 {ActionsRemaining}" : "기력 부족 · 하루 보내기 필요",
                description = "상인과 무인에게서 임무 단서와 세력 소문을 듣고, 품팔이로 은냥과 마을 신뢰를 챙긴다.",
                rewardSummary = "소문 단서, 은냥, 마을 신뢰",
                dangerSummary = "소문과 품팔이는 기력을 쓰므로 출정 전에 남은 행동을 확인하자.",
                action = HubMapAction.OpenMenu,
                targetMenu = HubMenu.Tavern,
                actionLabel = "객잔 열기"
            };
        case "market":
            return new HubMapInfo
            {
                id = id,
                artResource = "UI/HubLocationCards/hub_location_market",
                title = "장터",
                subtitle = "보급 · 선물 · 장비",
                category = "정비",
                status = $"은냥 {root.Flags.GetInt("silver")}",
                description = "회복 소모품, 선물, 수리 재료를 구입한다. 출정 전에 장비와 물자를 보강하는 장소다.",
                rewardSummary = "소모품 구매, 선물 확보, 인벤토리 정비",
                dangerSummary = "은냥을 과하게 쓰면 문파 복구와 장비 구매가 늦어진다.",
                action = HubMapAction.OpenMenu,
                targetMenu = HubMenu.Market,
                actionLabel = "장터 열기"
            };
        case "library":
            return new HubMapInfo
            {
                id = id,
                artResource = "UI/HubLocationCards/hub_location_library",
                title = "서고",
                subtitle = "무공 연구 · 도감 · 단서",
                category = "자유시간 행동",
                status = $"연구 {root.Flags.GetInt("growth:research_xp")} · {(root.Flags.HasFlag(StoryFlags.FirstBattleWon) ? "새 단서 있음" : "기록 열람")}",
                description = "백두산 영맥, 천광검문 계보, 검은 표식의 단서를 정리한다. 연구 행동으로 무공과 세계 정보를 연다.",
                rewardSummary = "무공 연구, 도감 항목, 세력 단서",
                dangerSummary = "연구는 즉시 전투력보다 장기 성장과 해금에 무게가 있다.",
                action = HubMapAction.OpenMenu,
                targetMenu = HubMenu.Library,
                actionLabel = "서고 열기"
            };
        case "infirmary":
            return new HubMapInfo
            {
                id = id,
                artResource = "UI/HubLocationCards/hub_location_infirmary",
                title = "의원",
                subtitle = "치료 · 약초 · 부상 관리",
                category = "정비",
                status = InjuredCount() > 0 ? $"부상자 {InjuredCount()}명 확인 필요" : "부상자 없음",
                description = "부상 동료를 치료하고 초희의 약방에서 약재를 관리한다. 다음 출정을 가볍게 만드는 안전망이다.",
                rewardSummary = "부상 치료, 약재 보급, 다음 전투 위험 감소",
                dangerSummary = "부상을 방치하면 다음 출정 준비와 전투 안정성이 떨어진다.",
                action = HubMapAction.OpenMenu,
                targetMenu = HubMenu.Infirmary,
                actionLabel = "의원 열기"
            };
        case "mission_gate":
        default:
            return new HubMapInfo
            {
                id = "mission_gate",
                artResource = "UI/HubLocationCards/hub_location_mission_gate",
                title = "출정문",
                subtitle = "임무 게시판 · 자유시간 의뢰",
                category = "출정",
                status = $"기력 {ActionsRemaining}/{MaxDailyActions} · 전투 준비 확인",
                description = "메인 전투와 반복 의뢰를 고른다. 마을 주변 의뢰는 기력 1을 쓰며, 각 전장 지형과 보상을 확인한 뒤 출격 준비로 넘어간다.",
                rewardSummary = "임무 보상 미리보기, 전장 정보, 출격 준비",
                dangerSummary = "권장 레벨과 위험 지형을 확인하지 않으면 전투 손실이 커질 수 있다.",
                action = HubMapAction.OpenMissionBoard,
                targetMenu = HubMenu.Sortie,
                actionLabel = "임무 게시판 열기"
            };
        }
    }

    private HubMapInfo FreeTimeBattleMapInfo(string id, string battleId, string fallbackTitle)
    {
        MissionInfo mission = MissionForBattle(battleId);
        string title = mission != null ? mission.title : fallbackTitle;
        string subtitle = mission != null ? mission.location : "소백촌 자유시간 의뢰";
        string description = mission != null ? mission.summary : "자유시간에 처리할 수 있는 반복 의뢰다.";
        string reward = RewardPreview(mission);
        string danger = mission != null && !string.IsNullOrEmpty(mission.dangerNotes)
                            ? mission.dangerNotes
                            : "기력 1을 소모하는 반복 의뢰.";
        string level = mission != null ? $"Lv.{mission.recommendedLevel} · {mission.difficulty}" : "권장 레벨 확인";
        string enemy = mission != null ? mission.enemyFaction : "마을 위협";

        return new HubMapInfo
        {
            id = id,
            artResource = HubMapMissionArtResource(battleId),
            title = title,
            subtitle = subtitle,
            category = "반복 의뢰",
            status = $"{level} · {enemy} · {(ActionsRemaining > 0 ? "출격 가능" : "기력 부족")}",
            description = description,
            rewardSummary = reward,
            dangerSummary = danger,
            action = HubMapAction.OpenBattlePrep,
            battleId = battleId,
            targetMenu = HubMenu.Sortie,
            actionLabel = ActionsRemaining > 0 ? "출격 준비로" : "기력 부족"
        };
    }

    private static string HubMapMissionArtResource(string battleId)
    {
        switch (battleId)
        {
        case BanditLairBattleId:
            return "MapAssets/Backgrounds/sobaek_bandit_lair_srpg_ground";
        case WolfPassBattleId:
            return "MapAssets/Backgrounds/sobaek_wolf_pass_srpg_ground";
        case TigerRavineBattleId:
            return "MapAssets/Backgrounds/sobaek_tiger_ravine_srpg_ground";
        case LeopardCliffBattleId:
            return "MapAssets/Backgrounds/sobaek_leopard_cliff_srpg_ground";
        default:
            return "MapAssets/Backgrounds/baekdu_snow_gate_srpg_ground";
        }
    }

    private static MissionInfo MissionForBattle(string battleId)
    {
        foreach (MissionInfo mission in MissionCatalog.All)
        {
            if (mission.battleId == battleId)
            {
                return mission;
            }
        }

        return null;
    }

    private static string RewardPreview(MissionInfo mission)
    {
        if (mission == null || mission.rewardPreview == null || mission.rewardPreview.Count == 0)
        {
            return "보상 정보 없음";
        }

        return string.Join(" / ", mission.rewardPreview.ToArray());
    }

    private void DrawHubMapInfoPanel(Rect rect, HubMapInfo info, float s)
    {
        if (info == null)
        {
            return;
        }

        UiTheme.DrawPanel(rect, true);
        float x = rect.x + 16f * s;
        float y = rect.y + 14f * s;
        float w = rect.width - 32f * s;

        Rect artRect = new Rect(x, y, w, Mathf.Clamp(rect.height * 0.28f, 112f * s, 168f * s));
        DrawHubMapInfoArt(artRect, info, s);
        y += artRect.height + 12f * s;

        GUI.Label(new Rect(x, y, w, 30f * s), info.title, UiTheme.Heading);
        y += 34f * s;
        GUI.Label(new Rect(x, y, w, 22f * s), $"{info.category} · {info.subtitle}", UiTheme.SmallMuted);
        y += 30f * s;

        DrawHubMapInfoBlock(x, ref y, w, s, "상태", info.status, 32f);
        DrawHubMapInfoBlock(x, ref y, w, s, "설명", info.description, 58f);
        DrawHubMapInfoBlock(x, ref y, w, s, "보상 / 효과", info.rewardSummary, 40f);

        if (!string.IsNullOrEmpty(info.dangerSummary))
        {
            GUIStyle warningStyle = new GUIStyle(UiTheme.Small)
            {
                wordWrap = true,
                clipping = TextClipping.Clip
            };
            warningStyle.normal.textColor = UiTheme.Ink;
            float warningH = Mathf.Clamp(warningStyle.CalcHeight(new GUIContent(info.dangerSummary), w - 18f * s) + 16f * s,
                                         42f * s, 64f * s);
            Rect warn = new Rect(x, y, w, warningH);
            UiTheme.DrawFill(warn, new Color(0.706f, 0.220f, 0.169f, 0.16f));
            GUI.Label(new Rect(warn.x + 9f * s, warn.y + 7f * s, warn.width - 18f * s, warn.height - 14f * s),
                      info.dangerSummary, warningStyle);
        }

        if (info.action != HubMapAction.None)
        {
            bool enabled = HubMapActionEnabled(info);
            Rect button = new Rect(x, rect.yMax - 52f * s, w, 40f * s);
            GUI.enabled = enabled;
            if (GUI.Button(button, enabled ? info.actionLabel : "기력 부족", enabled ? UiTheme.ButtonPrimary : UiTheme.Button))
            {
                ExecuteHubMapInfoAction(info);
            }
            GUI.enabled = true;
        }
    }

    private static void DrawHubMapInfoArt(Rect rect, HubMapInfo info, float s)
    {
        UiTheme.DrawFill(rect, new Color(0.010f, 0.014f, 0.014f, 0.88f));
        Texture2D art = LoadHubMapInfoArt(info != null ? info.artResource : null);
        if (art != null)
        {
            GUI.DrawTexture(rect, art, ScaleMode.ScaleAndCrop);
        }
        else
        {
            GUIStyle placeholder = new GUIStyle(UiTheme.SmallMuted)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                clipping = TextClipping.Clip
            };
            GUI.Label(rect, "ART", placeholder);
        }

        float shadeH = Mathf.Min(46f * s, rect.height * 0.38f);
        UiTheme.DrawFill(new Rect(rect.x, rect.yMax - shadeH, rect.width, shadeH),
                         new Color(0.000f, 0.010f, 0.014f, 0.62f));
        GUIStyle caption = new GUIStyle(UiTheme.Small)
        {
            alignment = TextAnchor.MiddleLeft,
            fontStyle = FontStyle.Bold,
            clipping = TextClipping.Clip
        };
        caption.normal.textColor = UiTheme.GoldBright;
        GUI.Label(new Rect(rect.x + 10f * s, rect.yMax - shadeH, rect.width - 20f * s, shadeH),
                  info != null ? info.title : string.Empty, caption);
        DrawSimpleFrame(rect, Mathf.Max(1f, 1.25f * s), new Color(UiTheme.Gold.r, UiTheme.Gold.g, UiTheme.Gold.b, 0.70f));
    }

    private static Texture2D LoadHubMapInfoArt(string resource)
    {
        if (string.IsNullOrEmpty(resource))
        {
            return null;
        }

        Texture2D art;
        if (!HubMapInfoArtCache.TryGetValue(resource, out art))
        {
            art = Resources.Load<Texture2D>(resource);
            HubMapInfoArtCache[resource] = art;
        }

        return art;
    }

    private static void DrawHubMapInfoBlock(float x, ref float y, float w, float s, string label, string body,
                                            float maxHeightBase)
    {
        GUI.Label(new Rect(x, y, w, 20f * s), label, UiTheme.SmallMuted);
        y += 22f * s;

        GUIStyle bodyStyle = new GUIStyle(UiTheme.Small)
        {
            wordWrap = true,
            clipping = TextClipping.Clip
        };
        bodyStyle.normal.textColor = UiTheme.Ink;
        float bodyH = Mathf.Clamp(bodyStyle.CalcHeight(new GUIContent(body ?? string.Empty), w), 24f * s,
                                  maxHeightBase * s);
        GUI.Label(new Rect(x, y, w, bodyH), body ?? string.Empty, bodyStyle);
        y += bodyH + 10f * s;
    }

    private bool HubMapActionEnabled(HubMapInfo info)
    {
        return info.action != HubMapAction.OpenBattlePrep || ActionsRemaining > 0;
    }

    private void ExecuteHubMapInfoAction(HubMapInfo info)
    {
        switch (info.action)
        {
        case HubMapAction.OpenMissionBoard:
            root.Flow.GoToMissionBoard();
            break;
        case HubMapAction.OpenBattlePrep:
            OpenFreeTimeBattlePrep(info.battleId, info.title);
            break;
        case HubMapAction.OpenMenu:
            menu = info.targetMenu;
            break;
        }
    }

    private bool MapHotspot(Rect parent, float px, float py, float pw, float ph, float s, string title, string subtitle,
                            string badge, string infoId, HubMenu target)
    {
        Rect rect = new Rect(parent.x + parent.width * px, parent.y + parent.height * py, parent.width * pw,
                             parent.height * ph);
        rect.width = Mathf.Max(rect.width, 126f * s);
        rect.height = Mathf.Max(rect.height, 44f * s);

        bool hover = rect.Contains(Event.current.mousePosition);
        if (hover)
        {
            hoveredHubMapInfoId = infoId;
        }

        bool selected = string.Equals(pinnedHubMapInfoId, infoId, StringComparison.OrdinalIgnoreCase);
        Color fill = selected
                         ? new Color(0.090f, 0.165f, 0.140f, 0.86f)
                         : hover ? new Color(0.070f, 0.082f, 0.074f, 0.86f)
                                 : new Color(0.022f, 0.026f, 0.024f, 0.76f);
        Color edge = selected || hover ? UiTheme.GoldBright : new Color(UiTheme.Gold.r, UiTheme.Gold.g, UiTheme.Gold.b, 0.72f);

        UiTheme.DrawFill(new Rect(rect.x + 4f * s, rect.y + 5f * s, rect.width, rect.height),
                         new Color(0f, 0f, 0f, 0.32f));
        UiTheme.DrawFill(rect, fill);
        DrawSimpleFrame(rect, Mathf.Max(1f, 1.3f * s), edge);

        GUIStyle titleStyle = new GUIStyle(UiTheme.Body)
        {
            alignment = TextAnchor.UpperLeft,
            fontStyle = FontStyle.Bold,
            fontSize = Mathf.RoundToInt(15f * s),
            clipping = TextClipping.Clip
        };
        titleStyle.normal.textColor = UiTheme.GoldBright;
        GUIStyle subStyle = new GUIStyle(UiTheme.SmallMuted)
        {
            alignment = TextAnchor.UpperLeft,
            fontSize = Mathf.RoundToInt(11f * s),
            clipping = TextClipping.Clip
        };
        subStyle.normal.textColor = UiTheme.Ink;

        GUI.Label(new Rect(rect.x + 12f * s, rect.y + 6f * s, rect.width - 34f * s, 20f * s), title, titleStyle);
        GUI.Label(new Rect(rect.x + 12f * s, rect.y + 25f * s, rect.width - 24f * s, 17f * s), subtitle, subStyle);

        if (!string.IsNullOrEmpty(badge))
        {
            Rect dot = new Rect(rect.xMax - 23f * s, rect.y + 5f * s, 18f * s, 18f * s);
            UiTheme.DrawSeal(dot, badge);
        }

        return GUI.Button(rect, GUIContent.none, GUIStyle.none);
    }

    private static Rect FitAspect(Rect bounds, float aspect)
    {
        float width = bounds.width;
        float height = width / aspect;
        if (height > bounds.height)
        {
            height = bounds.height;
            width = height * aspect;
        }

        return new Rect(bounds.x + (bounds.width - width) * 0.5f, bounds.y + (bounds.height - height) * 0.5f, width,
                        height);
    }

    private static void DrawSimpleFrame(Rect rect, float thick, Color color)
    {
        UiTheme.DrawFill(new Rect(rect.x, rect.y, rect.width, thick), color);
        UiTheme.DrawFill(new Rect(rect.x, rect.yMax - thick, rect.width, thick), color);
        UiTheme.DrawFill(new Rect(rect.x, rect.y, thick, rect.height), color);
        UiTheme.DrawFill(new Rect(rect.xMax - thick, rect.y, thick, rect.height), color);
    }

    private static void DrawMapCornerFrame(Rect rect, float s)
    {
        DrawSimpleFrame(rect, Mathf.Max(1f, 1.2f * s), new Color(UiTheme.Gold.r, UiTheme.Gold.g, UiTheme.Gold.b, 0.56f));
        float len = 28f * s;
        float thick = Mathf.Max(2f, 2f * s);
        Color gold = UiTheme.GoldBright;
        UiTheme.DrawFill(new Rect(rect.x + 8f * s, rect.y + 8f * s, len, thick), gold);
        UiTheme.DrawFill(new Rect(rect.x + 8f * s, rect.y + 8f * s, thick, len), gold);
        UiTheme.DrawFill(new Rect(rect.xMax - 8f * s - len, rect.y + 8f * s, len, thick), gold);
        UiTheme.DrawFill(new Rect(rect.xMax - 8f * s - thick, rect.y + 8f * s, thick, len), gold);
        UiTheme.DrawFill(new Rect(rect.x + 8f * s, rect.yMax - 8f * s - thick, len, thick), gold);
        UiTheme.DrawFill(new Rect(rect.x + 8f * s, rect.yMax - 8f * s - len, thick, len), gold);
        UiTheme.DrawFill(new Rect(rect.xMax - 8f * s - len, rect.yMax - 8f * s - thick, len, thick), gold);
        UiTheme.DrawFill(new Rect(rect.xMax - 8f * s - thick, rect.yMax - 8f * s - len, thick, len), gold);
    }

    private static void DrawFallbackHubMap(Rect map, float s)
    {
        UiTheme.DrawFill(map, new Color(0.315f, 0.365f, 0.285f, 1f));
        UiTheme.DrawFill(new Rect(map.x, map.y, map.width, map.height * 0.30f), new Color(0.420f, 0.455f, 0.390f, 1f));
        UiTheme.DrawFill(new Rect(map.x + map.width * 0.44f, map.y + map.height * 0.16f, map.width * 0.20f,
                                  map.height * 0.16f), new Color(0.250f, 0.310f, 0.285f, 1f));
        UiTheme.DrawFill(new Rect(map.x + map.width * 0.47f, map.y + map.height * 0.40f, map.width * 0.22f,
                                  map.height * 0.18f), new Color(0.520f, 0.460f, 0.330f, 1f));
        UiTheme.DrawFill(new Rect(map.x + map.width * 0.16f, map.y + map.height * 0.68f, map.width * 0.22f,
                                  map.height * 0.14f), new Color(0.410f, 0.290f, 0.210f, 1f));
        GUI.Label(new Rect(map.x + 18f * s, map.y + 16f * s, map.width - 36f * s, 28f * s),
                  "hub_free_time_map_v1 리소스를 불러오지 못했습니다.", UiTheme.SmallMuted);
    }

    private void DrawSortie(Rect r, float s)
    {
        GUI.Label(new Rect(r.x, r.y, r.width, 36f * s), "출정", UiTheme.Heading);
        GUI.Label(new Rect(r.x, r.y + 48f * s, r.width, 90f * s),
                  "임무 게시판에서 메인 전투와 자유시간 의뢰를 고른다.\n도적 소굴 같은 마을 의뢰는 기력 1을 쓰고, " +
                      "늑대·호랑이·표범 습격 같은 토벌 의뢰도 지형 정보를 확인한 뒤 출격 준비로 넘어간다.",
                  UiTheme.Body);
        if (GUI.Button(new Rect(r.x, r.y + 150f * s, r.width * 0.7f, 60f * s), "임무 선택 →", UiTheme.ButtonPrimary))
        {
            root.Flow.GoToMissionBoard();
        }
    }

    private void DrawTraining(Rect r, float s)
    {
        GUI.Label(new Rect(r.x, r.y, r.width, 36f * s), "연무장", UiTheme.Heading);
        GUI.Label(new Rect(r.x, r.y + 42f * s, r.width, 28f * s),
                  "오늘의 기력을 어디에 태울지 고르는 수련 보드다. 카드를 누르면 즉시 기력 1을 쓰고 숙련이 오른다.",
                  UiTheme.Small);

        float topY = r.y + 82f * s;
        float gap = 12f * s;
        float summaryW = Mathf.Clamp(r.width * 0.26f, 250f * s, 340f * s);
        Rect cardArea = new Rect(r.x, topY, r.width - summaryW - gap,
                                 Mathf.Clamp(r.height - 176f * s, 198f * s, 292f * s));
        Rect summary = new Rect(cardArea.xMax + gap, topY, summaryW, cardArea.height);
        float cardW = (cardArea.width - gap * 2f) / 3f;

        DrawTrainingCard(new Rect(cardArea.x, cardArea.y, cardW, cardArea.height), s, 0,
                         "천광심법 호흡", "내공 호흡", "빛의 내공을 몸 안에 맞춰 명중과 상태 저항의 씨앗을 만든다.",
                         "UI/HubTrainingCards/training_breath", "growth:inner_art_xp", 16, UiTheme.GoldBright);
        DrawTrainingCard(new Rect(cardArea.x + (cardW + gap), cardArea.y, cardW, cardArea.height), s, 1,
                         "백야검결 검로", "검법 숙련", "첫 검로를 반복해 이동 후 공격 감각과 검기 운용을 다듬는다.",
                         "UI/HubTrainingCards/training_sword", "growth:sword_xp", 24, new Color(0.62f, 0.84f, 1f, 1f));
        DrawTrainingCard(new Rect(cardArea.x + (cardW + gap) * 2f, cardArea.y, cardW, cardArea.height), s, 2,
                         "속성 연계 합련", "동료 전술", "동료와 속성 타이밍을 맞춰 후속타와 보조 운용을 익힌다.",
                         "UI/HubTrainingCards/training_sparring", "growth:teamwork_xp", 24, UiTheme.Teal);

        DrawTrainingSummary(summary, s);

        float lowerY = topY + cardArea.height + 14f * s;
        Rect statBoard = new Rect(r.x, lowerY, r.width, Mathf.Max(250f * s, r.yMax - lowerY - 6f * s));
        DrawFocusedTrainingBoard(statBoard, s);
    }

    private void DrawTrainingCard(Rect rect, float s, int drillIndex, string title, string subtitle, string body,
                                  string artResource, string statKey, int nextGoal, Color accent)
    {
        int value = root.Flags.GetInt(statKey);
        bool canTrain = ActionsRemaining > 0;
        bool hover = rect.Contains(Event.current.mousePosition);
        UiTheme.DrawFill(new Rect(rect.x + 3f * s, rect.y + 5f * s, rect.width, rect.height),
                         new Color(0f, 0f, 0f, 0.32f));
        UiTheme.DrawFill(rect, hover ? new Color(0.060f, 0.075f, 0.070f, 0.96f)
                                     : new Color(0.030f, 0.040f, 0.038f, 0.94f));
        DrawSimpleFrame(rect, Mathf.Max(1f, 1.2f * s),
                        hover ? UiTheme.GoldBright : new Color(UiTheme.Gold.r, UiTheme.Gold.g, UiTheme.Gold.b, 0.58f));

        Rect art = new Rect(rect.x + 8f * s, rect.y + 8f * s, rect.width - 16f * s, rect.height * 0.54f);
        Texture2D texture = LoadHubMapInfoArt(artResource);
        if (texture != null)
        {
            GUI.DrawTexture(art, texture, ScaleMode.ScaleAndCrop);
        }
        UiTheme.DrawFill(new Rect(art.x, art.yMax - 54f * s, art.width, 54f * s), new Color(0f, 0.010f, 0.014f, 0.66f));
        DrawSimpleFrame(art, Mathf.Max(1f, 1f * s), new Color(accent.r, accent.g, accent.b, 0.72f));

        GUIStyle titleStyle = new GUIStyle(UiTheme.Body)
        {
            fontStyle = FontStyle.Bold,
            fontSize = Mathf.RoundToInt(17f * s),
            alignment = TextAnchor.MiddleLeft,
            clipping = TextClipping.Clip
        };
        titleStyle.normal.textColor = UiTheme.GoldBright;
        GUI.Label(new Rect(art.x + 10f * s, art.yMax - 48f * s, art.width - 20f * s, 24f * s), title, titleStyle);
        GUI.Label(new Rect(art.x + 10f * s, art.yMax - 24f * s, art.width - 20f * s, 18f * s), subtitle, UiTheme.SmallMuted);

        float y = art.yMax + 10f * s;
        GUIStyle bodyStyle = new GUIStyle(UiTheme.Small)
        {
            wordWrap = true,
            clipping = TextClipping.Clip
        };
        bodyStyle.normal.textColor = UiTheme.Ink;
        GUI.Label(new Rect(rect.x + 12f * s, y, rect.width - 24f * s, 42f * s), body, bodyStyle);
        y += 48f * s;

        DrawTrainingProgress(new Rect(rect.x + 12f * s, y, rect.width - 24f * s, 30f * s), s, value, nextGoal, accent);

        Rect cost = new Rect(rect.x + 12f * s, rect.yMax - 42f * s, 72f * s, 30f * s);
        UiTheme.DrawFill(cost, new Color(accent.r * 0.20f, accent.g * 0.20f, accent.b * 0.20f, 0.90f));
        DrawSimpleFrame(cost, Mathf.Max(1f, 1f * s), new Color(accent.r, accent.g, accent.b, 0.80f));
        GUI.Label(cost, "기력 1", UiTheme.Small);

        Rect button = new Rect(rect.xMax - 116f * s, rect.yMax - 42f * s, 104f * s, 30f * s);
        GUI.enabled = canTrain;
        if (GUI.Button(button, canTrain ? "수련" : "기력 부족", canTrain ? UiTheme.ButtonPrimary : UiTheme.Button))
        {
            if (TrySpendAction("연무장 수련"))
            {
                ApplyTraining(drillIndex);
            }
        }
        GUI.enabled = true;
    }

    private void DrawTrainingSummary(Rect rect, float s)
    {
        UiTheme.DrawPanel(rect, true);
        float x = rect.x + 14f * s;
        float y = rect.y + 12f * s;
        float w = rect.width - 28f * s;

        GUI.Label(new Rect(x, y, w, 26f * s), "오늘의 수련 상태", UiTheme.Body);
        y += 34f * s;

        GUIStyle apStyle = new GUIStyle(UiTheme.Title)
        {
            fontSize = Mathf.RoundToInt(34f * s),
            alignment = TextAnchor.MiddleLeft
        };
        apStyle.normal.textColor = ActionsRemaining > 0 ? UiTheme.GoldBright : new Color(0.72f, 0.72f, 0.68f, 1f);
        GUI.Label(new Rect(x, y, w, 42f * s), $"기력 {ActionsRemaining}/{MaxDailyActions}", apStyle);
        y += 54f * s;

        DrawTrainingMiniStat(new Rect(x, y, w, 34f * s), s, "심법", root.Flags.GetInt("growth:inner_art_xp"), 16,
                             UiTheme.GoldBright);
        y += 42f * s;
        DrawTrainingMiniStat(new Rect(x, y, w, 34f * s), s, "검결", root.Flags.GetInt("growth:sword_xp"), 24,
                             new Color(0.62f, 0.84f, 1f, 1f));
        y += 42f * s;
        DrawTrainingMiniStat(new Rect(x, y, w, 34f * s), s, "합련", root.Flags.GetInt("growth:teamwork_xp"), 24,
                             UiTheme.Teal);
        y += 48f * s;

        UiTheme.DrawFill(new Rect(x, y, w, Mathf.Max(44f * s, rect.yMax - y - 12f * s)),
                         new Color(0.010f, 0.018f, 0.018f, 0.54f));
        GUI.Label(new Rect(x + 10f * s, y + 8f * s, w - 20f * s, 46f * s),
                  "심법 16부터 초식 단서가 열리고, 검결과 합련은 전투 선택지의 안정도를 끌어올린다.",
                  UiTheme.SmallMuted);
    }

    private static void DrawTrainingMiniStat(Rect rect, float s, string label, int value, int goal, Color accent)
    {
        GUI.Label(new Rect(rect.x, rect.y, 52f * s, rect.height), label, UiTheme.Small);
        DrawTrainingProgress(new Rect(rect.x + 58f * s, rect.y + 4f * s, rect.width - 58f * s, rect.height - 8f * s),
                             s, value, goal, accent);
    }

    private static void DrawTrainingProgress(Rect rect, float s, int value, int goal, Color accent)
    {
        int clampedGoal = Mathf.Max(1, goal);
        float frac = Mathf.Clamp01(value / (float)clampedGoal);
        Rect bar = new Rect(rect.x, rect.y + rect.height - 12f * s, rect.width, 10f * s);
        UiTheme.DrawFill(bar, new Color(0.020f, 0.026f, 0.024f, 0.90f));
        UiTheme.DrawFill(new Rect(bar.x, bar.y, bar.width * frac, bar.height), new Color(accent.r, accent.g, accent.b, 0.92f));
        DrawSimpleFrame(bar, Mathf.Max(1f, 1f * s), new Color(accent.r, accent.g, accent.b, 0.52f));
        GUIStyle valueStyle = new GUIStyle(UiTheme.SmallMuted)
        {
            alignment = TextAnchor.UpperRight,
            fontStyle = FontStyle.Bold,
            clipping = TextClipping.Clip
        };
        GUI.Label(new Rect(rect.x, rect.y, rect.width, 18f * s), $"{value}/{clampedGoal}", valueStyle);
    }

    private void DrawFocusedTrainingBoard(Rect rect, float s)
    {
        UiTheme.DrawPanel(rect, true);
        float pad = 14f * s;
        Rect header = new Rect(rect.x + pad, rect.y + 10f * s, rect.width - pad * 2f, 42f * s);
        GUI.Label(new Rect(header.x, header.y, header.width * 0.5f, header.height), "능력 집중 수련", UiTheme.Body);
        GUI.Label(new Rect(header.x + header.width * 0.48f, header.y + 4f * s, header.width * 0.52f, 24f * s),
                  "대상: 박성준 · 원하는 능력을 직접 올립니다.", UiTheme.SmallMuted);

        CharacterProgressState progress = GetTrainingTargetProgress();
        int columns = rect.width < 760f * s ? 2 : 4;
        int rows = Mathf.CeilToInt(FocusTrainingKeys.Length / (float)columns);
        float gap = 10f * s;
        float startY = rect.y + 58f * s;
        float cellW = (rect.width - pad * 2f - gap * (columns - 1)) / columns;
        float cellH = Mathf.Max(82f * s, (rect.yMax - startY - pad - gap * (rows - 1)) / rows);

        for (int i = 0; i < FocusTrainingKeys.Length; i++)
        {
            int col = i % columns;
            int row = i / columns;
            Rect cell = new Rect(rect.x + pad + (cellW + gap) * col, startY + (cellH + gap) * row, cellW, cellH);
            DrawFocusedTrainingCard(cell, s, FocusTrainingKeys[i], progress, i);
        }
    }

    private void DrawFocusedTrainingCard(Rect rect, float s, string key, CharacterProgressState progress, int index)
    {
        bool hover = rect.Contains(Event.current.mousePosition);
        Color accent = FocusTrainingAccent(index);
        UiTheme.DrawFill(rect, hover ? new Color(0.055f, 0.075f, 0.068f, 0.96f)
                                     : new Color(0.018f, 0.028f, 0.026f, 0.92f));
        DrawSimpleFrame(rect, Mathf.Max(1f, 1f * s),
                        hover ? accent : new Color(UiTheme.Gold.r, UiTheme.Gold.g, UiTheme.Gold.b, 0.42f));

        string label = MurimStatFormula.TrainingLabel(key);
        int bonus = MurimStatFormula.ProgressBonus(progress, key);
        int repeat = root.Flags.GetInt("growth:focus:" + key);
        float x = rect.x + 10f * s;
        float y = rect.y + 8f * s;
        float w = rect.width - 20f * s;

        GUIStyle labelStyle = new GUIStyle(UiTheme.Body)
        {
            fontStyle = FontStyle.Bold,
            fontSize = Mathf.RoundToInt(17f * s),
            clipping = TextClipping.Clip
        };
        labelStyle.normal.textColor = accent;
        GUI.Label(new Rect(x, y, w * 0.58f, 24f * s), label, labelStyle);
        GUI.Label(new Rect(x + w * 0.58f, y + 2f * s, w * 0.42f, 22f * s),
                  key == MurimStatFormula.HpKey || key == MurimStatFormula.InnerKey ? $"+{bonus}" : $"보너스 +{bonus}",
                  UiTheme.SmallMuted);
        y += 28f * s;

        GUIStyle desc = new GUIStyle(UiTheme.SmallMuted)
        {
            wordWrap = true,
            clipping = TextClipping.Clip
        };
        GUI.Label(new Rect(x, y, w, 34f * s), MurimStatFormula.TrainingDescription(key), desc);

        DrawTrainingProgress(new Rect(x, rect.yMax - 43f * s, Mathf.Max(90f * s, w - 106f * s), 30f * s), s,
                             repeat, 5, accent);

        Rect button = new Rect(rect.xMax - 96f * s, rect.yMax - 38f * s, 84f * s, 28f * s);
        GUI.enabled = ActionsRemaining > 0;
        if (GUI.Button(button, ActionsRemaining > 0 ? "수련" : "기력 0",
                       ActionsRemaining > 0 ? UiTheme.ButtonPrimary : UiTheme.Button))
        {
            if (TrySpendAction(label + " 수련"))
            {
                ApplyFocusedTraining(key);
            }
        }
        GUI.enabled = true;
    }

    private CharacterProgressState GetTrainingTargetProgress()
    {
        if (root == null || root.Session == null)
        {
            return null;
        }

        ProgressionService progression = new ProgressionService(root.Session);
        return progression.GetSnapshot(CharacterGrowthCatalog.ProtagonistId);
    }

    private static Color FocusTrainingAccent(int index)
    {
        switch (index % 8)
        {
            case 0: return new Color(0.94f, 0.34f, 0.25f, 1f);
            case 1: return new Color(0.38f, 0.78f, 1f, 1f);
            case 2: return UiTheme.GoldBright;
            case 3: return new Color(0.58f, 0.92f, 0.68f, 1f);
            case 4: return new Color(0.68f, 0.70f, 1f, 1f);
            case 5: return new Color(0.84f, 0.74f, 0.52f, 1f);
            case 6: return UiTheme.Teal;
            default: return new Color(1f, 0.66f, 0.86f, 1f);
        }
    }

    private static void DrawTrainingFlow(Rect rect, float s)
    {
        UiTheme.DrawFill(rect, new Color(0.010f, 0.016f, 0.016f, 0.58f));
        DrawSimpleFrame(rect, Mathf.Max(1f, 1f * s), new Color(UiTheme.Gold.r, UiTheme.Gold.g, UiTheme.Gold.b, 0.35f));

        GUI.Label(new Rect(rect.x + 14f * s, rect.y + 10f * s, 180f * s, 26f * s), "전투 감각 루틴", UiTheme.Small);
        string[] steps = { "선택", "이동", "사거리", "무공", "예측", "주사위", "반격", "대기", "종료" };
        float startX = rect.x + 14f * s;
        float y = rect.y + 48f * s;
        float available = rect.width - 28f * s;
        float stepW = available / steps.Length;
        for (int i = 0; i < steps.Length; i++)
        {
            Vector2 center = new Vector2(startX + stepW * i + stepW * 0.5f, y + 16f * s);
            Color accent = i < 5 ? UiTheme.GoldBright : UiTheme.Teal;
            UiTheme.DrawFill(new Rect(center.x - 13f * s, center.y - 13f * s, 26f * s, 26f * s),
                             new Color(accent.r * 0.18f, accent.g * 0.18f, accent.b * 0.18f, 0.92f));
            DrawSimpleFrame(new Rect(center.x - 13f * s, center.y - 13f * s, 26f * s, 26f * s),
                            Mathf.Max(1f, 1f * s), new Color(accent.r, accent.g, accent.b, 0.72f));
            GUI.Label(new Rect(center.x - 22f * s, center.y + 18f * s, 44f * s, 20f * s), steps[i], UiTheme.SmallMuted);
            if (i < steps.Length - 1)
            {
                UiTheme.DrawFill(new Rect(center.x + 15f * s, center.y - 1f * s, stepW - 30f * s, 2f * s),
                                 new Color(UiTheme.Gold.r, UiTheme.Gold.g, UiTheme.Gold.b, 0.34f));
            }
        }
    }

    private void DrawCompanions(Rect r, float s)
    {
        if (!string.IsNullOrEmpty(giftTargetId))
        {
            if (!root.Session.HasCompanion(giftTargetId))
            {
                giftTargetId = null;
            }
            else
            {
                DrawGiftGiving(r, s, giftTargetId);
                return;
            }
        }

        if (!string.IsNullOrEmpty(visitCompanionId))
        {
            if (!root.Session.HasCompanion(visitCompanionId))
            {
                visitCompanionId = null;
            }
            else
            {
                DrawCompanionVisit(r, s, visitCompanionId);
                return;
            }
        }

        GUI.Label(new Rect(r.x, r.y, r.width, 36f * s), "동료", UiTheme.Heading);
        float y = r.y + 48f * s;
        float cardH = 144f * s;
        foreach (string id in root.Session.recruitedCompanionIds)
        {
            CompanionInfo info = CompanionCatalog.Info(id);
            if (info == null)
                continue;
            Rect card = new Rect(r.x, y, r.width, cardH);
            bool visitedToday = HasVisitedCompanionToday(id);
            UiTheme.DrawPanel(card, true);
            if (visitedToday)
            {
                UiTheme.DrawFill(card, new Color(0f, 0f, 0f, 0.30f));
            }

            GUI.Label(new Rect(card.x + 16f * s, card.y + 10f * s, card.width - 180f * s, 30f * s),
                      $"{info.name} · {info.title}", UiTheme.Body);
            GUI.Label(
                new Rect(card.x + 16f * s, card.y + 42f * s, card.width - 180f * s, 26f * s),
                $"{info.age}세 · {info.mbti} · {info.region} {info.sectName} · {info.element}/{info.weapon}   |   연애도 {root.Approval.GetStageLabel(id)} ({root.Approval.Get(id)})",
                UiTheme.SmallMuted);
            GUI.Label(new Rect(card.x + 16f * s, card.y + 68f * s, card.width - 180f * s, 24f * s),
                      "고민: " + CompanionConcern(id), UiTheme.SmallMuted);
            GUI.Label(new Rect(card.x + 16f * s, card.y + 92f * s, card.width - 180f * s, 24f * s),
                      $"다음 대화: {NextCompanionTalk(id)}   |   상태: {CompanionStatusText(id)}",
                      UiTheme.SmallMuted);

            // 장비 요약(후속 지시 §9) — 장비가 적용된 상태인지 동료 목록에서 바로 보이게 한다.
            string equipSummary = root.Equipment != null ? root.Equipment.Summary(id) : string.Empty;
            GUIStyle equipStyle = new GUIStyle(UiTheme.SmallMuted);
            if (!string.IsNullOrEmpty(equipSummary))
            {
                equipStyle.normal.textColor = UiTheme.GoldBright;
            }

            GUI.Label(new Rect(card.x + 16f * s, card.y + 116f * s, card.width - 180f * s, 24f * s),
                      "장비: " + (string.IsNullOrEmpty(equipSummary) ? "없음 — ‘장비’ 메뉴에서 정비" : equipSummary),
                      equipStyle);
            GUI.enabled = !visitedToday;
            if (GUI.Button(new Rect(card.xMax - 144f * s, card.y + 14f * s, 128f * s, 44f * s),
                           visitedToday ? "방문 완료" : "방문",
                           visitedToday ? UiTheme.Button : UiTheme.ButtonPrimary))
            {
                visitCompanionId = id;
                visitScroll = Vector2.zero;
            }
            GUI.enabled = true;

            bool giftedToday = root.Gifts != null && root.Gifts.HasGiftedToday(id);
            bool canOpenGift = !visitedToday && !giftedToday;
            GUI.enabled = canOpenGift;
            if (GUI.Button(new Rect(card.xMax - 144f * s, card.y + cardH - 58f * s, 128f * s, 44f * s),
                           visitedToday ? "방문 완료" : giftedToday ? "선물 완료" : "선물",
                           canOpenGift ? UiTheme.ButtonPrimary : UiTheme.Button))
            {
                giftTargetId = id;
                giftScroll = Vector2.zero;
            }
            GUI.enabled = true;

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

    private void DrawCompanionVisit(Rect r, float s, string companionId)
    {
        CompanionInfo info = CompanionCatalog.Info(companionId);
        if (info == null)
        {
            visitCompanionId = null;
            return;
        }

        GUI.Label(new Rect(r.x, r.y, r.width - 150f * s, 36f * s), $"{info.name} 방문", UiTheme.Heading);
        if (GUI.Button(new Rect(r.xMax - 140f * s, r.y, 128f * s, 38f * s), "← 동료", UiTheme.Button))
        {
            visitCompanionId = null;
            return;
        }

        Rect body = new Rect(r.x, r.y + 46f * s, r.width, r.height - 46f * s);
        float contentH = Mathf.Max(body.height, 720f * s);
        visitScroll = GUI.BeginScrollView(body, visitScroll, new Rect(0f, 0f, body.width - 18f * s, contentH));

        float x = 0f;
        float y = 0f;
        float w = body.width - 18f * s;
        bool visitedToday = HasVisitedCompanionToday(companionId);
        Rect profile = new Rect(x, y, w, 160f * s);
        UiTheme.DrawPanel(profile, true);
        if (visitedToday)
        {
            UiTheme.DrawFill(profile, new Color(0f, 0f, 0f, 0.20f));
        }

        GUI.Label(new Rect(profile.x + 16f * s, profile.y + 12f * s, profile.width - 32f * s, 28f * s),
                  $"{CompanionVisitPlace(companionId)} · {info.title}", UiTheme.Body);
        GUI.Label(new Rect(profile.x + 16f * s, profile.y + 44f * s, profile.width - 32f * s, 24f * s),
                  $"{info.region} {info.sectName} · {info.element}/{info.weapon} · {info.mbti}", UiTheme.SmallMuted);
        GUI.Label(new Rect(profile.x + 16f * s, profile.y + 70f * s, profile.width - 32f * s, 44f * s),
                  VisitMoodLine(companionId), UiTheme.SmallMuted);

        int approval = root.Approval.Get(companionId);
        Rect gaugeBg = new Rect(profile.x + 16f * s, profile.y + 122f * s, profile.width - 32f * s, 12f * s);
        UiTheme.DrawFill(gaugeBg, UiTheme.HanjiPanelAlt);
        UiTheme.DrawFill(new Rect(gaugeBg.x, gaugeBg.y, gaugeBg.width * Mathf.Clamp01(approval / 100f), gaugeBg.height),
                         UiTheme.Teal);
        GUI.Label(new Rect(profile.x + 16f * s, profile.y + 136f * s, profile.width * 0.46f, 22f * s),
                  $"연애도 {approval}/100 · {root.Approval.GetStageLabel(companionId)}", UiTheme.SmallMuted);
        GUI.Label(new Rect(profile.x + profile.width * 0.52f, profile.y + 136f * s, profile.width * 0.44f, 22f * s),
                  VisitTodayText(companionId), new GUIStyle(UiTheme.SmallMuted) { alignment = TextAnchor.MiddleRight });

        y += profile.height + 12f * s;

        Rect state = new Rect(x, y, w, 126f * s);
        UiTheme.DrawPanel(state);
        float sx = state.x + 14f * s;
        float sy = state.y + 10f * s;
        Line(sx, ref sy, state.width - 28f * s, s, "상태", CompanionStatusText(companionId));
        Line(sx, ref sy, state.width - 28f * s, s, "관심사", CompanionConcern(companionId));
        Line(sx, ref sy, state.width - 28f * s, s, "지원 효과", SupportPreview(companionId, approval));
        y += state.height + 14f * s;

        GUI.Label(new Rect(x, y, w, 30f * s), "방문 행동", UiTheme.Heading);
        if (visitedToday)
        {
            GUI.Label(new Rect(x + 96f * s, y + 2f * s, w - 96f * s, 26f * s),
                      "오늘은 이미 이 동료와 시간을 보냈다. 내일 다시 방문 가능.", UiTheme.SmallMuted);
        }
        y += 38f * s;

        float gap = 10f * s;
        float colW = (w - gap) * 0.5f;
        float actionH = 72f * s;
        if (VisitActionButton(new Rect(x, y, colW, actionH), s, "대화하기", NextCompanionTalk(companionId),
                              true, !visitedToday))
        {
            StartCompanionTalk(companionId);
        }

        if (VisitActionButton(new Rect(x + colW + gap, y, colW, actionH), s, "자유 대화",
                              "근황과 속마음 · 연애 선택", false, !visitedToday))
        {
            StartGeneratedCompanionTalk(companionId);
        }
        y += actionH + gap;

        bool giftedToday = root.Gifts != null && root.Gifts.HasGiftedToday(companionId);
        if (VisitActionButton(new Rect(x, y, colW, actionH), s,
                              giftedToday ? "선물 완료" : visitedToday ? "방문 완료" : "선물주기",
                              FavoriteGiftHint(companionId), !visitedToday && !giftedToday,
                              !visitedToday && !giftedToday))
        {
            giftTargetId = companionId;
            giftScroll = Vector2.zero;
        }

        if (VisitActionButton(new Rect(x + colW + gap, y, colW, actionH), s, "합련",
                              "합련 +6 · 호감 +2 · 피로 +10", false, !visitedToday))
        {
            ApplyCompanionVisitTraining(companionId);
        }
        y += actionH + gap;

        if (VisitActionButton(new Rect(x, y, colW, actionH), s, "산책/순찰",
                              "마을 신뢰 +1 · 위명 +1 · 호감 +2", false, !visitedToday))
        {
            ApplyCompanionVisitPatrol(companionId);
        }

        if (VisitActionButton(new Rect(x + colW + gap, y, colW, actionH), s, "휴식/간호",
                              IsWounded(companionId) ? "부상 회복 · 호감 +2" : "피로 -20 · 호감 +1",
                              false, !visitedToday))
        {
            ApplyCompanionVisitRest(companionId);
        }
        y += actionH + gap;

        if (VisitActionButton(new Rect(x, y, colW, actionH), s, "전술 회의",
                              "전술 이해 +6 · 호감 +1", false, !visitedToday))
        {
            ApplyCompanionVisitStrategy(companionId);
        }

        Rect memo = new Rect(x + colW + gap, y, colW, actionH);
        UiTheme.DrawPanel(memo);
        GUI.Label(new Rect(memo.x + 12f * s, memo.y + 8f * s, memo.width - 24f * s, 24f * s), "개인 과제",
                  UiTheme.Body);
        GUI.Label(new Rect(memo.x + 12f * s, memo.y + 34f * s, memo.width - 24f * s, 32f * s),
                  CompanionVisitHook(companionId), UiTheme.SmallMuted);
        y += actionH + 14f * s;

        GUI.Label(new Rect(x, y, w, 78f * s), info.profile, UiTheme.SmallMuted);
        GUI.EndScrollView();
    }

    private bool VisitActionButton(Rect rect, float s, string title, string hint, bool primary = false,
                                   bool enabled = true)
    {
        bool oldEnabled = GUI.enabled;
        GUI.enabled = oldEnabled && enabled;
        bool clicked = GUI.Button(rect, title, primary ? UiTheme.ButtonPrimary : UiTheme.Button);
        GUI.enabled = oldEnabled;
        GUI.Label(new Rect(rect.x + 12f * s, rect.y + 40f * s, rect.width - 24f * s, 24f * s), hint,
                  enabled ? UiTheme.SmallMuted : UiTheme.Small);
        return enabled && clicked;
    }

    private void StartCompanionTalk(string companionId)
    {
        if (HasVisitedCompanionToday(companionId))
        {
            ShowToast("오늘은 이미 방문했습니다.");
            return;
        }

        if (!TrySpendAction("동료 방문 대화"))
        {
            return;
        }

        talk = new DialogueController(BuildCompanionTalk(companionId), root);
        root.Session.actionsTaken++;
        MarkCompanionVisited(companionId);
        SaveHubProgress();
    }

    private void StartGeneratedCompanionTalk(string companionId)
    {
        if (HasVisitedCompanionToday(companionId))
        {
            ShowToast("오늘은 이미 방문했습니다.");
            return;
        }

        if (!TrySpendAction("동료 자유 대화"))
        {
            return;
        }

        talk = new DialogueController(BuildGeneratedVisitTalk(companionId), root);
        root.Session.actionsTaken++;
        root.Flags.AddInt("companion:free_talk_count:" + companionId, 1);
        MarkCompanionVisited(companionId);
        SaveHubProgress();
    }

    private DialogueScript BuildGeneratedVisitTalk(string companionId)
    {
        DialogueScript script = new DialogueScript();
        CompanionInfo info = CompanionCatalog.Info(companionId);
        string name = info != null ? info.name : CompanionCatalog.Name(companionId);
        string line = root.Narration != null
                          ? root.Narration.GenerateCompanionReaction(companionId, "hub_companion_visit")
                          : $"{name}이 조용히 고개를 끄덕인다.";

        DialogueNode opener = new DialogueNode("visit_ai_001", name, line, "visit_ai_002", companionId);
        opener.speakerTitle = info != null ? info.sectName : null;
        opener.backgroundId = CompanionVisitBackground(companionId);
        script.Add(opener);

        DialogueNode choice = new DialogueNode("visit_ai_002", "박성준", "(어떻게 답할까?)");
        choice.backgroundId = CompanionVisitBackground(companionId);
        choice.choices.Add(new DialogueChoice("오늘은 그 마음을 기억하겠습니다.", HeroDisposition.Chivalrous)
                               .Approval(companionId, +2));
        choice.choices.Add(new DialogueChoice("전장에서도 그렇게 맞춰보죠.", HeroDisposition.Royal)
                               .Approval(companionId, +1)
                               .Battle("visit_bond:" + companionId, +1));
        choice.choices.Add(new DialogueChoice("좋습니다. 다음엔 더 어려운 이야기도 듣죠.", HeroDisposition.Romantic)
                               .Approval(companionId, +2));
        script.Add(choice);
        return script;
    }

    private void ApplyCompanionVisitTraining(string companionId)
    {
        if (HasVisitedCompanionToday(companionId))
        {
            ShowToast("오늘은 이미 방문했습니다.");
            return;
        }

        if (!TrySpendAction("동료 합련"))
        {
            return;
        }

        root.Session.actionsTaken++;
        root.Flags.AddInt("growth:teamwork_xp", 6);
        root.Flags.AddInt("growth:martial_xp", 4);
        root.Approval.Add(companionId, +2);
        if (root.CompanionStates != null)
        {
            root.CompanionStates.AddFatigue(companionId, +10);
        }

        MarkCompanionVisited(companionId);
        ShowToast($"{CompanionCatalog.Name(companionId)} 호감 +2");
        AddLog($"{CompanionCatalog.Name(companionId)}와 합련했다. 합련 +6, 무공 경험 +4, 호감 +2.");
        SaveHubProgress();
    }

    private void ApplyCompanionVisitPatrol(string companionId)
    {
        if (HasVisitedCompanionToday(companionId))
        {
            ShowToast("오늘은 이미 방문했습니다.");
            return;
        }

        if (!TrySpendAction("동료 산책/순찰"))
        {
            return;
        }

        root.Session.actionsTaken++;
        root.Flags.AddInt("sect:village_trust", 1);
        root.Reputation.Add(FactionIds.JoseonSects, +1);
        root.Approval.Add(companionId, +2);
        if (root.CompanionStates != null)
        {
            root.CompanionStates.AddFatigue(companionId, +5);
        }

        MarkCompanionVisited(companionId);
        ShowToast("마을 신뢰 +1");
        AddLog($"{CompanionCatalog.Name(companionId)}와 소백촌 길목을 돌았다. 마을 신뢰 +1, 위명 +1, 호감 +2.");
        SaveHubProgress();
    }

    private void ApplyCompanionVisitRest(string companionId)
    {
        if (HasVisitedCompanionToday(companionId))
        {
            ShowToast("오늘은 이미 방문했습니다.");
            return;
        }

        if (!TrySpendAction("동료 휴식/간호"))
        {
            return;
        }

        root.Session.actionsTaken++;
        bool wounded = IsWounded(companionId);
        if (root.CompanionStates != null)
        {
            if (wounded)
            {
                root.CompanionStates.Heal(companionId);
            }
            else
            {
                root.CompanionStates.AddFatigue(companionId, -20);
            }
        }

        root.Approval.Add(companionId, wounded ? +2 : +1);
        MarkCompanionVisited(companionId);
        ShowToast(wounded ? "부상 회복" : "피로 회복");
        AddLog(wounded
                   ? $"{CompanionCatalog.Name(companionId)}의 상처를 살폈다. 부상 회복, 호감 +2."
                   : $"{CompanionCatalog.Name(companionId)}와 조용히 쉬었다. 피로 -20, 호감 +1.");
        SaveHubProgress();
    }

    private void ApplyCompanionVisitStrategy(string companionId)
    {
        if (HasVisitedCompanionToday(companionId))
        {
            ShowToast("오늘은 이미 방문했습니다.");
            return;
        }

        if (!TrySpendAction("동료 전술 회의"))
        {
            return;
        }

        root.Session.actionsTaken++;
        root.Flags.AddInt("growth:tactics_xp", 6);
        root.Flags.AddInt("support:plan:" + companionId, 1);
        root.Approval.Add(companionId, +1);
        MarkCompanionVisited(companionId);
        ShowToast("전술 이해 +6");
        AddLog($"{CompanionCatalog.Name(companionId)}와 다음 전장의 역할을 맞췄다. 전술 이해 +6, 호감 +1.");
        SaveHubProgress();
    }

    private void MarkCompanionVisited(string companionId)
    {
        if (string.IsNullOrEmpty(companionId))
        {
            return;
        }

        root.Flags.SetInt("companion:visit:last_day:" + companionId, DayIndex);
        root.Flags.AddInt("companion:visit:count:" + companionId, 1);
    }

    private bool HasVisitedCompanionToday(string companionId)
    {
        if (string.IsNullOrEmpty(companionId))
        {
            return false;
        }

        bool visited = root.Flags.GetInt("companion:visit:last_day:" + companionId) >= DayIndex;
        bool gifted = root.Gifts != null && root.Gifts.HasGiftedToday(companionId);
        return visited || gifted;
    }

    private string VisitTodayText(string companionId)
    {
        bool visitedToday = HasVisitedCompanionToday(companionId);
        bool giftedToday = root.Gifts != null && root.Gifts.HasGiftedToday(companionId);
        string visit = visitedToday ? "오늘 방문함" : "오늘 방문 전";
        string gift = giftedToday ? "선물 완료" : visitedToday ? "선물 불가" : "선물 가능";
        return visit + " · " + gift + " · 기력 " + ActionsRemaining + "/" + MaxDailyActions;
    }

    private static string CompanionVisitPlace(string companionId)
    {
        switch (CharacterIdAliasResolver.Normalize(companionId))
        {
        case "baek_ryeon":
            return "후산 약재 정자";
        case "do_arin":
            return "연무장 화로 옆";
        case "jin_seoyul":
            return "검각 처마 밑";
        case "shin_seoa":
            return "소백촌 꽃길";
        case "han_biyeon":
            return "서고 뒤 그림자길";
        default:
            return "후산 정자";
        }
    }

    private static string CompanionVisitBackground(string companionId)
    {
        switch (CharacterIdAliasResolver.Normalize(companionId))
        {
        case "baek_ryeon":
            return "bg_seorak_spear_council_hall";
        case "do_arin":
            return "bg_hwawang_blade_training_ground";
        case "jin_seoyul":
            return "bg_cheonroe_staff_dojo_gyeongseong";
        case "shin_seoa":
            return "bg_hwajeop_flower_fan_courtyard";
        case "han_biyeon":
            return "bg_heukryeon_shadow_cliff_temple";
        default:
            return "bg_pyesadang_courtyard_dawn";
        }
    }

    private static string VisitMoodLine(string companionId)
    {
        switch (CharacterIdAliasResolver.Normalize(companionId))
        {
        case "baek_ryeon":
            return "약재 향이 남은 창대가 벽에 기대어 있다. 백련은 다친 사람의 이름을 먼저 확인한다.";
        case "do_arin":
            return "도아린은 불씨를 툭툭 건드리며 다음 정면돌파 이야기를 기다린다.";
        case "jin_seoyul":
            return "진서율은 처마의 물방울 궤적을 보며 번개가 흐를 길을 계산하고 있다.";
        case "shin_seoa":
            return "신서아는 부채 끝으로 바람길을 그리며 모두가 설 자리를 살핀다.";
        case "han_biyeon":
            return "한비연은 밝은 곳과 어두운 곳의 경계를 재며 짧게 웃는다.";
        default:
            return "동료가 오늘의 문파 분위기를 살피고 있다.";
        }
    }

    private static string CompanionVisitHook(string companionId)
    {
        switch (CharacterIdAliasResolver.Normalize(companionId))
        {
        case "baek_ryeon":
            return "부상자 보호 루트";
        case "do_arin":
            return "돌파 전열 루트";
        case "jin_seoyul":
            return "감찰단 전기 단서";
        case "shin_seoa":
            return "지원 진형 루트";
        case "han_biyeon":
            return "독살 누명 단서";
        default:
            return "신뢰 루트";
        }
    }

    private static string FavoriteGiftHint(string companionId)
    {
        foreach (GiftInfo gift in GiftCatalog.All)
        {
            if (gift.IsFavoriteOf(companionId))
            {
                return "최애: " + gift.displayName + " +" + gift.favoriteDelta;
            }
        }

        return "범용 선물로 호감 상승";
    }

    private static string SupportPreview(string companionId, int approval)
    {
        string stage = approval >= 80 ? "동지" : approval >= 60 ? "신뢰" : approval >= 40 ? "동행" : "경계";
        switch (CharacterIdAliasResolver.Normalize(companionId))
        {
        case "baek_ryeon":
            return stage + " · 보호 대상 인접 시 창수 견제";
        case "do_arin":
            return stage + " · 돌파 시작 턴 기세 보조";
        case "jin_seoyul":
            return stage + " · 원거리 빈틈 표시";
        case "shin_seoa":
            return stage + " · 지원 위치 보정";
        case "han_biyeon":
            return stage + " · 암습 경로 탐지";
        default:
            return stage + " · 동행 보정";
        }
    }

    private void SaveHubProgress()
    {
        if (root != null && root.Save != null && root.Session != null)
        {
            root.Save.Save(root.Session);
        }
    }

    /// <summary>동료 선물 화면 — 보유 선물 목록에서 골라 건넨다(하루 1회/동료, 설계 §B).</summary>
    private void DrawGiftGiving(Rect r, float s, string companionId)
    {
        CompanionInfo info = CompanionCatalog.Info(companionId);
        string name = info != null ? info.name : companionId;
        GUI.Label(new Rect(r.x, r.y, r.width - 150f * s, 36f * s), $"선물 — {name}", UiTheme.Heading);
        if (GUI.Button(new Rect(r.xMax - 140f * s, r.y, 128f * s, 38f * s), "← 동료", UiTheme.Button))
        {
            giftTargetId = null;
            return;
        }

        // 연애도 현황
        int approval = root.Approval.Get(companionId);
        Rect status = new Rect(r.x, r.y + 46f * s, r.width, 54f * s);
        UiTheme.DrawFill(status, new Color(0.030f, 0.040f, 0.038f, 0.85f));
        Color rose = new Color(0.94f, 0.45f, 0.62f, 1f);
        GUIStyle gaugeTitle = new GUIStyle(UiTheme.Small) { fontStyle = FontStyle.Bold };
        gaugeTitle.normal.textColor = rose;
        GUI.Label(new Rect(status.x + 14f * s, status.y + 6f * s, status.width * 0.4f, 24f * s),
                  $"연애도 {approval}/100 · {root.Approval.GetStageLabel(companionId)}", gaugeTitle);
        bool giftedToday = root.Gifts != null && root.Gifts.HasGiftedToday(companionId);
        bool visitedToday = HasVisitedCompanionToday(companionId);
        GUIStyle stateStyle = new GUIStyle(UiTheme.SmallMuted) { alignment = TextAnchor.MiddleRight };
        string stateText = giftedToday ? "오늘 선물 완료" : visitedToday ? "오늘 방문 행동 완료" : "오늘 선물 가능";
        GUI.Label(new Rect(status.x + status.width * 0.5f, status.y + 6f * s, status.width * 0.5f - 14f * s, 24f * s),
                  stateText, stateStyle);
        Rect barBg = new Rect(status.x + 14f * s, status.y + 34f * s, status.width - 28f * s, 10f * s);
        UiTheme.DrawFill(barBg, UiTheme.HanjiPanelAlt);
        UiTheme.DrawFill(new Rect(barBg.x, barBg.y, barBg.width * Mathf.Clamp01(approval / 100f), barBg.height), rose);

        // 보유 선물 목록
        List<InventoryStack> gifts = new List<InventoryStack>();
        foreach (InventoryStack stack in root.Inventory.AllStacks())
        {
            if (stack.type == InventoryItemType.Gift)
            {
                gifts.Add(stack);
            }
        }

        Rect body = new Rect(r.x, status.yMax + 12f * s, r.width, r.yMax - status.yMax - 12f * s);
        if (gifts.Count == 0)
        {
            GUI.Label(new Rect(body.x, body.y, body.width, 60f * s),
                      "보유한 선물이 없다. 장터의 ‘선물’ 탭에서 구입할 수 있다.", UiTheme.Body);
            return;
        }

        float rowH = 62f * s;
        giftScroll = GUI.BeginScrollView(body, giftScroll,
                                         new Rect(0f, 0f, body.width - 18f * s,
                                                  Mathf.Max(body.height, gifts.Count * (rowH + 8f * s))));
        float y = 0f;
        float w = body.width - 18f * s;
        foreach (InventoryStack stack in gifts)
        {
            GiftInfo gift = GiftCatalog.Get(stack.itemId);
            if (gift == null)
            {
                continue;
            }

            Rect row = new Rect(0f, y, w, rowH);
            bool hover = row.Contains(Event.current.mousePosition);
            UiTheme.DrawFill(row, hover ? new Color(0.060f, 0.075f, 0.070f, 0.88f)
                                        : new Color(0.030f, 0.040f, 0.038f, 0.75f));
            if (visitedToday)
            {
                UiTheme.DrawFill(row, new Color(0f, 0f, 0f, 0.28f));
            }

            bool favorite = gift.IsFavoriteOf(companionId);
            UiTheme.DrawFill(new Rect(row.x, row.y, 4f * s, row.height),
                             favorite ? rose : HubInventoryGrid.AccentFor(InventoryItemType.Gift));

            GUI.Label(new Rect(row.x + 14f * s, row.y + 6f * s, row.width * 0.6f, 26f * s),
                      favorite ? $"{gift.displayName}  <color=#F0709C>최애 선물 ◆</color>" : gift.displayName,
                      UiTheme.Body);
            GUI.Label(new Rect(row.x + 14f * s, row.y + 32f * s, row.width - 220f * s, 24f * s),
                      $"{gift.description}  (연애도 +{gift.DeltaFor(companionId)})", UiTheme.SmallMuted);
            GUIStyle countStyle = new GUIStyle(UiTheme.Small) { alignment = TextAnchor.MiddleRight };
            GUI.Label(new Rect(row.xMax - 196f * s, row.y + 6f * s, 40f * s, 24f * s), "x" + stack.count, countStyle);

            string reason = string.Empty;
            bool canGive = !visitedToday && root.Gifts != null && root.Gifts.CanGift(companionId, gift.id, out reason);
            if (visitedToday)
            {
                reason = "오늘은 이미 이 동료와 시간을 보냈다.";
            }

            GUI.enabled = canGive;
            if (GUI.Button(new Rect(row.xMax - 148f * s, row.y + 10f * s, 136f * s, 42f * s), "건네기",
                           UiTheme.ButtonPrimary))
            {
                GiftResult result = root.Gifts.Give(companionId, gift.id);
                if (result.success)
                {
                    ShowToast(result.wasFavorite ? $"최애 선물! 연애도 +{result.delta}" : $"연애도 +{result.delta}");
                    AddLog(result.message.Replace("\n", " "));
                    MarkCompanionVisited(companionId);
                    SaveHubProgress();
                }
                else
                {
                    ShowToast(result.message);
                }
            }

            GUI.enabled = true;
            y += rowH + 8f * s;
        }

        GUI.EndScrollView();
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
        Line(r.x, ref y, r.width, s, "은냥", root.Flags.GetInt("silver").ToString());
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
                AddLog("소백촌 길목을 살피고 잡일을 도왔다. 마을 신뢰 +2, 은냥 +10.");
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
                ShowToast("은냥 +35");
                AddLog("객잔 주인의 일감을 받아 장작과 심부름을 처리했다. 은냥 +35, 마을 신뢰 +1.");
            }
        }
        GUI.Label(new Rect(r.x, r.y + 268f * s, r.width, 60f * s),
                  "· 서브 의뢰와 동료 영입 소문은 이후 버전에서 열립니다.", UiTheme.SmallMuted);
    }

    private void DrawInfirmary(Rect r, float s)
    {
        GUI.Label(new Rect(r.x, r.y, r.width, 36f * s), "의원", UiTheme.Heading);
        System.Collections.Generic.List<string> injured = root.CompanionStates.InjuredCompanionIds();
        float y = r.y + 50f * s;
        if (injured.Count > 0)
        {
            GUI.Label(new Rect(r.x, y, r.width, 28f * s), "치료가 필요한 동료:", UiTheme.Body);
            y += 34f * s;
            foreach (string id in injured)
            {
                GUI.Label(new Rect(r.x + 10f * s, y, r.width - 10f * s, 26f * s),
                          "· " + CompanionCatalog.Name(id) + " (" + CompanionStatusText(id) + ")", UiTheme.Small);
                y += 28f * s;
            }
            if (GUI.Button(new Rect(r.x, y + 8f * s, r.width * 0.6f, 48f * s), "치료하기", UiTheme.ButtonPrimary))
            {
                if (TrySpendAction("의원 치료"))
                {
                    root.CompanionStates.HealAll();
                    if (root.Session.lastBattleResult != null)
                    {
                        root.Session.lastBattleResult.woundedCompanions.Clear();
                    }

                    root.Flags.AddInt("supply:medicine", -Mathf.Min(1, Mathf.Max(0, root.Flags.GetInt("supply:medicine"))));
                    root.Save.Save(root.Session);
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

    private static readonly string[] MarketTabs = { "보급품", "선물", "장비", "재료" };

    private void DrawMarket(Rect r, float s)
    {
        GUI.Label(new Rect(r.x, r.y, r.width * 0.5f, 36f * s), "장터", UiTheme.Heading);
        GUIStyle silverStyle = new GUIStyle(UiTheme.Heading)
        {
            alignment = TextAnchor.MiddleRight,
            fontSize = Mathf.RoundToInt(22f * s)
        };
        silverStyle.normal.textColor = UiTheme.GoldBright;
        GUI.Label(new Rect(r.x + r.width * 0.4f, r.y, r.width * 0.6f, 36f * s),
                  $"보유 은냥  {root.Flags.GetInt("silver")}", silverStyle);

        float tabY = r.y + 42f * s;
        float gap = 8f * s;
        float tw = Mathf.Min(110f * s, (r.width - gap * 3f) / 4f);
        for (int i = 0; i < MarketTabs.Length; i++)
        {
            if (GUI.Button(new Rect(r.x + i * (tw + gap), tabY, tw, 36f * s), MarketTabs[i],
                           marketTab == i ? UiTheme.ButtonPrimary : UiTheme.Button))
            {
                marketTab = i;
            }
        }

        Rect body = new Rect(r.x, tabY + 44f * s, r.width, r.yMax - tabY - 44f * s);
        float w = body.width - 18f * s;
        marketScroll =
            GUI.BeginScrollView(body, marketScroll, new Rect(0f, 0f, w, MarketContentHeight(s, body.height)));
        float y = 0f;
        switch (marketTab)
        {
        case 1:
            GUI.Label(new Rect(0f, y, w, 24f * s),
                      "동료들의 마음을 여는 공략 선물. 하루 1회씩 건넬 수 있다.", UiTheme.SmallMuted);
            y += 30f * s;
            foreach (GiftInfo gift in GiftCatalog.All)
            {
                string favoriteTag = string.IsNullOrEmpty(gift.favoriteCompanionId)
                                         ? $"범용 · 연애도 +{gift.baseDelta}"
                                         : $"{CompanionCatalog.Name(gift.favoriteCompanionId)} 최애 +{gift.favoriteDelta}";
                BuyRow(w, ref y, s, gift.id, gift.displayName, gift.price, $"{gift.description}  ({favoriteTag})");
            }

            break;
        case 2:
            GUI.Label(new Rect(0f, y, w, 24f * s), "구입한 장비는 허브 ‘장비’ 메뉴에서 장착·강화한다.",
                      UiTheme.SmallMuted);
            y += 30f * s;
            foreach (EquipmentInfo equip in EquipmentCatalog.All)
            {
                string ownerTag = equip.IsExclusive
                                      ? $"{CharacterGrowthCatalog.DisplayName(equip.requiredCharacterId)} 전용"
                                      : "공용";
                string desc =
                    $"{EquipmentCatalog.SlotLabel(equip.slot)} · {ownerTag} · {EquipmentCatalog.DescribeBonus(equip, 0)}";
                BuyRow(w, ref y, s, equip.id, equip.displayName, equip.price, desc);
            }

            break;
        case 3:
            GUI.Label(new Rect(0f, y, w, 24f * s), "공방 강화 재료. +2 강화부터 재료가 든다.", UiTheme.SmallMuted);
            y += 30f * s;
            foreach (MaterialCatalog.MaterialInfo material in MaterialCatalog.All)
            {
                BuyRow(w, ref y, s, material.id, material.displayName, material.price, material.description);
            }

            break;
        default:
            IReadOnlyList<ShopItemInfo> stock =
                root.ShopRepository != null ? root.ShopRepository.StockFor("hub_market", root.Session) : null;
            if (stock != null)
            {
                foreach (ShopItemInfo item in stock)
                {
                    // 재료류(목재 등)는 재료 탭에서 판매하므로 보급품 탭에서는 숨긴다.
                    if (IsShopItemUnlocked(item) && InventoryService.TypeOf(item.id) != InventoryItemType.Material)
                    {
                        BuyRow(w, ref y, s, item.id, item.displayName, item.price, ShopItemDescription(item.id));
                    }
                }
            }

            break;
        }

        GUI.EndScrollView();
    }

    private float MarketContentHeight(float s, float minHeight)
    {
        int rows;
        switch (marketTab)
        {
        case 1:
            rows = GiftCatalog.All.Count;
            break;
        case 2:
            rows = EquipmentCatalog.All.Count;
            break;
        case 3:
            rows = MaterialCatalog.All.Count;
            break;
        default:
            rows = 6;
            break;
        }

        return Mathf.Max(minHeight, rows * 60f * s + 64f * s);
    }

    private void BuyRow(float w, ref float y, float s, string itemId, string item, int price, string desc)
    {
        Rect row = new Rect(0f, y, w, 54f * s);
        bool hover = row.Contains(Event.current.mousePosition);
        UiTheme.DrawFill(row, hover ? new Color(0.060f, 0.075f, 0.070f, 0.85f)
                                    : new Color(0.030f, 0.040f, 0.038f, 0.72f));
        InventoryItemType itemType = InventoryService.TypeOf(itemId);
        UiTheme.DrawFill(new Rect(row.x, row.y, 4f * s, row.height), HubInventoryGrid.AccentFor(itemType));
        HubInventoryGrid.DrawItemIcon(new Rect(row.x + 12f * s, row.y + 7f * s, 40f * s, 40f * s), itemId,
                                      itemType, s);
        int owned = root.Inventory.GetCount(itemId);
        string nameLabel = owned > 0 ? $"{item}  <color=#9FB6A0>보유 {owned}</color>" : item;
        GUI.Label(new Rect(row.x + 62f * s, row.y + 4f * s, row.width * 0.58f, 26f * s), nameLabel, UiTheme.Body);
        GUI.Label(new Rect(row.x + 62f * s, row.y + 28f * s, row.width - 228f * s, 22f * s), desc,
                  UiTheme.SmallMuted);
        bool canBuy = root.Flags.GetInt("silver") >= price;
        GUI.enabled = canBuy;
        if (GUI.Button(new Rect(row.xMax - 150f * s, row.y + 6f * s, 140f * s, 42f * s), $"구매 ({price}냥)",
                       UiTheme.Button))
        {
            if (root.Inventory.Purchase(root.Flags, itemId, 1, price))
            {
                ShowToast($"{item} 구매");
                AddLog($"장터에서 {item}을(를) 샀다. (-{price}은냥, 보유 {root.Inventory.GetCount(itemId)})");
            }
        }
        GUI.enabled = true;
        y += 60f * s;
    }

    private bool IsShopItemUnlocked(ShopItemInfo item)
    {
        return item != null && (string.IsNullOrEmpty(item.unlockFlag) || root.Flags.HasFlag(item.unlockFlag));
    }

    private static string ShopItemDescription(string itemId)
    {
        switch (InventoryService.NormalizeItemId(itemId))
        {
        case "medicine_bundle":
            return "전투 후 회복에 쓰인다.";
        case "wood_bundle":
            return "문파 시설 복구 재료.";
        case "inner_power_pill":
            return "내공 회복 소모품.";
        case "throwing_dagger_bundle":
            return "암기 보급.";
        default:
            return "보급품.";
        }
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
        string loreText = LoreText(loreIndex);
        string unlockedLore = UnlockedLoreText();
        if (!string.IsNullOrEmpty(unlockedLore))
        {
            loreText += "\n\n해금 기록\n" + unlockedLore;
        }

        GUI.Label(new Rect(body.x + 14f * s, body.y + 12f * s, body.width - 28f * s, body.height - 24f * s),
                  loreText, UiTheme.Body);

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

    private string UnlockedLoreText()
    {
        if (root == null || root.LoreRepository == null)
        {
            return string.Empty;
        }

        List<string> lines = new List<string>();
        foreach (LoreEntry entry in root.LoreRepository.All)
        {
            if (entry == null)
            {
                continue;
            }

            bool unlocked = root.Session.unlockedCodexEntryIds.Contains(entry.id) ||
                            (!string.IsNullOrEmpty(entry.unlockFlag) && root.Flags.HasFlag(entry.unlockFlag));
            if (unlocked)
            {
                lines.Add("· " + entry.title + " — " + entry.body);
            }
        }

        return string.Join("\n", lines);
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

    private void ApplyFocusedTraining(string key)
    {
        string characterId = CharacterGrowthCatalog.ProtagonistId;
        string label = MurimStatFormula.TrainingLabel(key);
        int amount = 1;
        root.Session.actionsTaken++;
        root.Flags.AddInt("growth:martial_xp", 4);
        root.Flags.AddInt("growth:focus:" + key, 1);

        if (key == MurimStatFormula.HpKey)
        {
            amount = 2;
            ProgressionKeys.AddInt(root.Session, ProgressionKeys.HpBonus(characterId), amount);
        }
        else if (key == MurimStatFormula.InnerKey)
        {
            ProgressionKeys.AddInt(root.Session, ProgressionKeys.InnerBonus(characterId), amount);
        }
        else
        {
            ProgressionKeys.AddInt(root.Session, ProgressionKeys.StatBonus(characterId, key), amount);
        }

        ShowToast(label + " +" + amount);
        AddLog("박성준이 " + label + " 집중 수련을 마쳤다. " + label + " +" + amount + ".");
    }

    private string CompanionBadge()
    {
        return root.Session.recruitedCompanionIds.Count > 0 ? "!" : string.Empty;
    }

    private int InjuredCount()
    {
        return root != null && root.CompanionStates != null ? root.CompanionStates.InjuredCount() : 0;
    }

    private bool IsWounded(string companionId)
    {
        return root != null && root.CompanionStates != null &&
               root.CompanionStates.InjuryOf(companionId) != CompanionInjuryLevel.Healthy;
    }

    private string CompanionStatusText(string companionId)
    {
        if (root == null || root.CompanionStates == null)
        {
            return "출전 가능";
        }

        CompanionInjuryLevel injury = root.CompanionStates.InjuryOf(companionId);
        string label = CompanionStateService.InjuryLabel(injury);
        int fatigue = root.CompanionStates.FatigueOf(companionId);
        return fatigue > 0 ? $"{label} · 피로 {fatigue}" : label;
    }

    private static string CompanionConcern(string companionId)
    {
        switch (CharacterIdAliasResolver.Normalize(companionId))
        {
        case "baek_ryeon":
            return "부상자와 약재 부족";
        case "do_arin":
            return "정면승부로 문파 명예 회복";
        case "jin_seoyul":
            return "천뢰봉문 감전 사건의 진상";
        case "shin_seoa":
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
        id = CharacterIdAliasResolver.Normalize(id);
        DialogueScript authored = TryBuildAuthoredCompanionTalk(id);
        if (authored != null)
        {
            return authored;
        }

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

    private static DialogueScript TryBuildAuthoredCompanionTalk(string companionId)
    {
        string sceneId = AuthoredCompanionSceneId(companionId);
        if (string.IsNullOrEmpty(sceneId))
        {
            return null;
        }

        AuthoringContentManifest manifest = AuthoringContentManifest.LoadFromResources();
        DialogueScript script = AuthoringDialogueAdapter.ToDialogueScript(manifest, sceneId);
        return script.Nodes.Count > 0 ? script : null;
    }

    private static string AuthoredCompanionSceneId(string companionId)
    {
        companionId = CharacterIdAliasResolver.Normalize(companionId);
        if (companionId == CompanionCatalog.BaekRyeon)
            return "companion_baek_ryeon_talk";
        if (companionId == CompanionCatalog.DoArin)
            return "companion_do_arin_talk";
        if (companionId == CompanionCatalog.JinSeoyul)
            return "companion_jin_seoyul_talk";
        if (companionId == CompanionCatalog.SeoA)
            return "companion_shin_seoa_talk";
        if (companionId == CompanionCatalog.HanBiyeon)
            return "companion_han_biyeon_talk";

        return string.Empty;
    }
}
}
