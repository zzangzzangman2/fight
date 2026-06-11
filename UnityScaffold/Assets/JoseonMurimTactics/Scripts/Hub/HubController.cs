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
    private Texture2D hubMapTexture;
    private HubEquipmentPanel equipmentPanel;
    private int marketTab;
    private Vector2 marketScroll;
    private string giftTargetId;
    private Vector2 giftScroll;

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
                  "장소 표지판을 눌러 오늘의 자유시간 행동, 마을 의뢰, 정비 메뉴로 이동한다.", UiTheme.Body);

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
        Rect map = FitAspect(new Rect(frame.x + 10f * s, frame.y + 10f * s, frame.width - 20f * s,
                                      frame.height - 20f * s), 16f / 9f);

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
                       HubMenu.Sortie))
        {
            root.Flow.GoToMissionBoard();
        }

        if (MapHotspot(map, 0.055f, 0.060f, 0.225f, 0.092f, s, "뒷산 도적 소굴", "기력 1 · 반복 의뢰",
                       ActionsRemaining > 0 ? "!" : "0", HubMenu.Sortie))
        {
            OpenFreeTimeBattlePrep(BanditLairBattleId, "도적 소굴 의뢰");
        }

        if (MapHotspot(map, 0.735f, 0.060f, 0.190f, 0.092f, s, "늑대 고개", "기력 1 · 방목길 방어",
                       ActionsRemaining > 0 ? "!" : "0", HubMenu.Sortie))
        {
            OpenFreeTimeBattlePrep(WolfPassBattleId, "늑대 고개 방어");
        }

        if (MapHotspot(map, 0.730f, 0.330f, 0.195f, 0.092f, s, "호랑이 바위골", "기력 1 · 주민 구조",
                       ActionsRemaining > 0 ? "!" : "0", HubMenu.Sortie))
        {
            OpenFreeTimeBattlePrep(TigerRavineBattleId, "호랑이 바위골 구조");
        }

        if (MapHotspot(map, 0.755f, 0.690f, 0.180f, 0.092f, s, "표범 절벽길", "기력 1 · 약초길 호송",
                       ActionsRemaining > 0 ? "!" : "0", HubMenu.Sortie))
        {
            OpenFreeTimeBattlePrep(LeopardCliffBattleId, "표범 절벽길 호송");
        }

        if (MapHotspot(map, 0.505f, 0.382f, 0.175f, 0.092f, s, "연무장", "수련 · 기력 1",
                       ActionsRemaining > 0 ? "" : "0", HubMenu.Training))
        {
            menu = HubMenu.Training;
        }

        if (MapHotspot(map, 0.365f, 0.165f, 0.190f, 0.092f, s, "검각 본당", "문파 재건", "!", HubMenu.Sect))
        {
            menu = HubMenu.Sect;
        }

        if (MapHotspot(map, 0.690f, 0.165f, 0.170f, 0.092f, s, "후산 정자", "동료 대화", CompanionBadge(),
                       HubMenu.Companions))
        {
            menu = HubMenu.Companions;
        }

        if (MapHotspot(map, 0.090f, 0.535f, 0.160f, 0.092f, s, "객잔", "소문 · 일감", "!", HubMenu.Tavern))
        {
            menu = HubMenu.Tavern;
        }

        if (MapHotspot(map, 0.300f, 0.705f, 0.165f, 0.092f, s, "장터", "보급 · 선물 · 장비", "", HubMenu.Market))
        {
            menu = HubMenu.Market;
        }

        if (MapHotspot(map, 0.560f, 0.735f, 0.170f, 0.092f, s, "서고", "무공 연구", root.Flags.HasFlag(StoryFlags.FirstBattleWon) ? "!" : "",
                       HubMenu.Library))
        {
            menu = HubMenu.Library;
        }

        if (MapHotspot(map, 0.755f, 0.520f, 0.165f, 0.092f, s, "의원", "치료 · 약초", InjuredCount() > 0 ? "!" : "",
                       HubMenu.Infirmary))
        {
            menu = HubMenu.Infirmary;
        }
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

    private bool MapHotspot(Rect parent, float px, float py, float pw, float ph, float s, string title, string subtitle,
                            string badge, HubMenu target)
    {
        Rect rect = new Rect(parent.x + parent.width * px, parent.y + parent.height * py, parent.width * pw,
                             parent.height * ph);
        rect.width = Mathf.Max(rect.width, 126f * s);
        rect.height = Mathf.Max(rect.height, 44f * s);

        bool hover = rect.Contains(Event.current.mousePosition);
        bool selected = menu == target;
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

        GUI.Label(new Rect(r.x, r.y, r.width, 36f * s), "동료", UiTheme.Heading);
        float y = r.y + 48f * s;
        float cardH = 144f * s;
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
                $"{info.age}세 · {info.mbti} · {info.region} {info.sectName} · {info.element}/{info.weapon}   |   유대 {root.Approval.GetStageLabel(id)} ({root.Approval.Get(id)})",
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
            if (GUI.Button(new Rect(card.xMax - 144f * s, card.y + 14f * s, 128f * s, 44f * s), "대화",
                           UiTheme.Button))
            {
                if (TrySpendAction("동료 대화"))
                {
                    talk = new DialogueController(BuildCompanionTalk(id), root);
                    root.Session.actionsTaken++;
                }
            }

            bool giftedToday = root.Gifts != null && root.Gifts.HasGiftedToday(id);
            if (GUI.Button(new Rect(card.xMax - 144f * s, card.y + cardH - 58f * s, 128f * s, 44f * s),
                           giftedToday ? "선물 완료" : "선물", giftedToday ? UiTheme.Button : UiTheme.ButtonPrimary))
            {
                giftTargetId = id;
                giftScroll = Vector2.zero;
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

        // 유대 현황
        int approval = root.Approval.Get(companionId);
        Rect status = new Rect(r.x, r.y + 46f * s, r.width, 54f * s);
        UiTheme.DrawFill(status, new Color(0.030f, 0.040f, 0.038f, 0.85f));
        GUIStyle gaugeTitle = new GUIStyle(UiTheme.Small) { fontStyle = FontStyle.Bold };
        gaugeTitle.normal.textColor = UiTheme.Teal;
        GUI.Label(new Rect(status.x + 14f * s, status.y + 6f * s, status.width * 0.4f, 24f * s),
                  $"유대 {approval}/100 · {root.Approval.GetStageLabel(companionId)}", gaugeTitle);
        bool giftedToday = root.Gifts != null && root.Gifts.HasGiftedToday(companionId);
        GUIStyle stateStyle = new GUIStyle(UiTheme.SmallMuted) { alignment = TextAnchor.MiddleRight };
        GUI.Label(new Rect(status.x + status.width * 0.5f, status.y + 6f * s, status.width * 0.5f - 14f * s, 24f * s),
                  giftedToday ? "오늘은 이미 선물을 건넸다" : "오늘 선물 가능", stateStyle);
        Rect barBg = new Rect(status.x + 14f * s, status.y + 34f * s, status.width - 28f * s, 10f * s);
        UiTheme.DrawFill(barBg, UiTheme.HanjiPanelAlt);
        UiTheme.DrawFill(new Rect(barBg.x, barBg.y, barBg.width * Mathf.Clamp01(approval / 100f), barBg.height),
                         UiTheme.Teal);

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
            bool favorite = gift.IsFavoriteOf(companionId);
            UiTheme.DrawFill(new Rect(row.x, row.y, 4f * s, row.height),
                             favorite ? UiTheme.GoldBright : HubInventoryGrid.AccentFor(InventoryItemType.Gift));

            GUI.Label(new Rect(row.x + 14f * s, row.y + 6f * s, row.width * 0.6f, 26f * s),
                      favorite ? $"{gift.displayName}  <color=#F5C75C>최애 선물 ◆</color>" : gift.displayName,
                      UiTheme.Body);
            GUI.Label(new Rect(row.x + 14f * s, row.y + 32f * s, row.width - 220f * s, 24f * s),
                      $"{gift.description}  (유대 +{gift.DeltaFor(companionId)})", UiTheme.SmallMuted);
            GUIStyle countStyle = new GUIStyle(UiTheme.Small) { alignment = TextAnchor.MiddleRight };
            GUI.Label(new Rect(row.xMax - 196f * s, row.y + 6f * s, 40f * s, 24f * s), "x" + stack.count, countStyle);

            string reason;
            bool canGive = root.Gifts != null && root.Gifts.CanGift(companionId, gift.id, out reason);
            GUI.enabled = canGive;
            if (GUI.Button(new Rect(row.xMax - 148f * s, row.y + 10f * s, 136f * s, 42f * s), "건네기",
                           UiTheme.ButtonPrimary))
            {
                GiftResult result = root.Gifts.Give(companionId, gift.id);
                if (result.success)
                {
                    ShowToast(result.wasFavorite ? $"최애 선물! 유대 +{result.delta}" : $"유대 +{result.delta}");
                    AddLog(result.message.Replace("\n", " "));
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
                      "선물을 주면 동료의 유대가 오른다. 동료당 하루 1회.", UiTheme.SmallMuted);
            y += 30f * s;
            foreach (GiftInfo gift in GiftCatalog.All)
            {
                string favoriteTag = string.IsNullOrEmpty(gift.favoriteCompanionId)
                                         ? $"범용 · 유대 +{gift.baseDelta}"
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
        UiTheme.DrawFill(new Rect(row.x, row.y, 4f * s, row.height),
                         HubInventoryGrid.AccentFor(InventoryService.TypeOf(itemId)));
        int owned = root.Inventory.GetCount(itemId);
        string nameLabel = owned > 0 ? $"{item}  <color=#9FB6A0>보유 {owned}</color>" : item;
        GUI.Label(new Rect(row.x + 14f * s, row.y + 4f * s, row.width * 0.62f, 26f * s), nameLabel, UiTheme.Body);
        GUI.Label(new Rect(row.x + 14f * s, row.y + 28f * s, row.width - 180f * s, 22f * s), desc,
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
        if (companionId == CompanionCatalog.BaekRyeon)
            return "companion_baek_ryeon_talk";
        if (companionId == CompanionCatalog.DoArin)
            return "companion_do_arin_talk";
        if (companionId == CompanionCatalog.JinSeoyul)
            return "companion_jin_seoyul_talk";
        if (companionId == CompanionCatalog.SeoA)
            return "companion_seo_a_talk";
        if (companionId == CompanionCatalog.HanBiyeon)
            return "companion_han_biyeon_talk";

        return string.Empty;
    }
}
}
