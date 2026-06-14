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
    private const string BanditLairMapPreviewResource = "MapAssets/Backgrounds/sobaek_bandit_lair_srpg_ground";
    private const string WolfPassMapPreviewResource = "MapAssets/Backgrounds/sobaek_wolf_pass_srpg_ground";
    private const string TigerRavineMapPreviewResource = "MapAssets/Backgrounds/sobaek_tiger_ravine_srpg_ground";
    private const string LeopardCliffMapPreviewResource = "MapAssets/Backgrounds/sobaek_leopard_cliff_srpg_ground";
    private static readonly Dictionary<string, Texture2D> MapPreviewTextureCache =
        new Dictionary<string, Texture2D>();

    private GameRoot root;
    private BattleDefinition def;
    private MissionInfo mission;
    private DialogueController introDialogue;
    private bool introDialogueCommitted;

    private void Awake()
    {
        root = GameRoot.EnsureExists();
        string id = string.IsNullOrEmpty(BattleEntryAdapter.PendingBattleId) ? HubController.FirstBattleId
                                                                             : BattleEntryAdapter.PendingBattleId;
        def = BattleCatalog.Get(id);
        mission = FindMission(id);
        if (def != null && def.id == HubController.SeorakPassRescueBattleId &&
            !root.Flags.HasFlag(StoryFlags.Chapter1SeorakRequestStarted))
        {
            DialogueScript script = TryBuildAuthoredDialogue("chapter1_baek_ryeon_join_before_battle");
            introDialogue = script == null ? null : new DialogueController(script, root);
        }
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

    private static string FreeTimeMapPreviewResourceForBattle(string battleId)
    {
        if (battleId == HubController.BanditLairBattleId)
        {
            return BanditLairMapPreviewResource;
        }

        if (battleId == HubController.WolfPassBattleId)
        {
            return WolfPassMapPreviewResource;
        }

        if (battleId == HubController.TigerRavineBattleId)
        {
            return TigerRavineMapPreviewResource;
        }

        if (battleId == HubController.LeopardCliffBattleId)
        {
            return LeopardCliffMapPreviewResource;
        }

        return string.Empty;
    }

    private void OnGUI()
    {
        UiTheme.Begin(true);
        float w = Screen.width;
        float h = Screen.height;
        float s = UiTheme.Scale;
        float margin = 44f * s;

        if (introDialogue != null)
        {
            if (!introDialogue.IsFinished)
            {
                UiTheme.DrawTitleBackdrop();
                GUI.Label(new Rect(0f, 24f * s, w, 44f * s), "제1장 2막 · 서리창의 약속", UiTheme.Title);
                GUI.Label(new Rect(0f, 70f * s, w, 28f * s), "설운령 산길 — 약초 수레 호위", UiTheme.BodyCenter);
                UiTheme.DrawDivider(w * 0.5f, 108f * s, 480f * s);
                introDialogue.Draw(w, h);
                return;
            }

            CommitIntroDialogue();
        }

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
            if (mission.consumesFreeTime)
            {
                Pair(lx, ref y, lw, s, "자유시간", $"기력 1 소모 · 남은 기력 {FreeActionsRemaining()}");
            }
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
        GUI.Label(new Rect(rx, ry, rw, 32f * s), "출격 인원 · 장비", UiTheme.Heading);
        ry += 38f * s;
        // 2열 그리드: 이름 + 장비 요약(이번 전투 전에 정비했다는 체감, 설계 §F).
        float cellW = (rw - 10f * s) * 0.5f;
        for (int i = 0; i < def.roster.Count; i++)
        {
            string member = def.roster[i];
            float cx = rx + (i % 2) * (cellW + 10f * s);
            float cy = ry + (i / 2) * 56f * s;
            GUI.Label(new Rect(cx + 6f * s, cy, cellW - 6f * s, 26f * s), "• " + member, UiTheme.Body);

            string charId = CharacterGrowthCatalog.NormalizeCharacterId(member);
            string summary = root.Equipment != null ? root.Equipment.Summary(charId) : string.Empty;
            EquipmentBonus bonus = root.Equipment != null ? root.Equipment.BuildBonus(charId) : default;
            string line = string.IsNullOrEmpty(summary) ? "장비 없음"
                                                        : bonus.IsEmpty ? summary : $"{summary} ({bonus})";
            GUIStyle equipStyle = new GUIStyle(UiTheme.SmallMuted)
            {
                fontSize = Mathf.RoundToInt(12f * s),
                clipping = TextClipping.Clip,
                wordWrap = false
            };
            if (!string.IsNullOrEmpty(summary))
            {
                equipStyle.normal.textColor = UiTheme.GoldBright;
            }

            GUI.Label(new Rect(cx + 20f * s, cy + 26f * s, cellW - 20f * s, 22f * s), line, equipStyle);
        }

        ry += ((def.roster.Count + 1) / 2) * 56f * s + 6f * s;

        GUI.Label(new Rect(rx, ry, rw, 32f * s), "예상 보상", UiTheme.Heading);
        ry += 36f * s;
        if (def.silverReward > 0)
        {
            GUI.Label(new Rect(rx + 10f * s, ry, rw - 10f * s, 26f * s), "· 은냥 " + def.silverReward, UiTheme.Small);
            ry += 28f * s;
        }
        List<string> rewards = mission != null && mission.rewardPreview.Count > 0
                                   ? new List<string>(mission.rewardPreview)
                                   : def.rewardItems;
        List<string> rewardLines = InventoryService.FormatRewardLines(rewards);
        foreach (string item in rewardLines)
        {
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
        Rect previewRect = new Rect(mapRect.x + 12f * s, mapRect.y + 12f * s, 146f * s,
                                    mapRect.height - 24f * s);
        if (def != null && def.id == HubController.SeorakPassRescueBattleId)
        {
            DrawSeorakPassMapPreview(previewRect, s);
        }
        else if (def != null && def.id == HubController.BanditLairBattleId)
        {
            if (!DrawPaintedMapPreview(previewRect, BanditLairMapPreviewResource, s))
            {
                DrawBanditLairMapPreview(previewRect, s);
            }
        }
        else if (def != null && IsWildlifeBattle(def.id))
        {
            if (!DrawPaintedMapPreview(previewRect, FreeTimeMapPreviewResourceForBattle(def.id), s))
            {
                DrawWildlifeMapPreview(previewRect, s, def.id);
            }
        }
        else
        {
            DrawTacticalMapPreview(previewRect, s);
        }
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
            if (!TrySpendMissionFreeTime())
            {
                return;
            }

            root.Session.actionsTaken++;
            root.Flow.GoToBattle(def.id);
        }
    }

    private bool TrySpendMissionFreeTime()
    {
        if (mission == null || !mission.consumesFreeTime)
        {
            return true;
        }

        int remaining = FreeActionsRemaining();
        if (remaining <= 0)
        {
            Debug.Log("[BattlePrep] Not enough free-time action points for " + mission.id);
            return false;
        }

        root.Flags.SetInt(HubController.ActionPointKey, remaining - 1);
        return true;
    }

    private int FreeActionsRemaining()
    {
        return root == null || root.Flags == null ? 0 : Mathf.Max(0, root.Flags.GetInt(HubController.ActionPointKey));
    }

    private void CommitIntroDialogue()
    {
        if (introDialogueCommitted)
        {
            return;
        }

        introDialogueCommitted = true;
        root.Flags.SetFlag(StoryFlags.Chapter1SeorakRequestStarted);
        root.Flags.SetFlag(StoryFlags.Chapter1MetBaekRyeon);
        root.Flags.SetFlag(StoryFlags.CompanionBaekRyeonTempJoined);
        root.Save.Save(root.Session);
    }

    private static DialogueScript TryBuildAuthoredDialogue(string sceneId)
    {
        AuthoringContentManifest manifest = AuthoringContentManifest.LoadFromResources();
        DialogueScript script = AuthoringDialogueAdapter.ToDialogueScript(manifest, sceneId);
        return script.Nodes.Count > 0 ? script : null;
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
            if (def != null && def.id == HubController.SeorakPassRescueBattleId)
            {
                return "설운령 약초 수레 호위전\n" +
                       "• 남쪽 산길: 박성준 진입로, 수레까지 최단 이동\n" +
                       "• 중앙 밧줄다리: 1칸 병목, 산적을 끊어내기 좋음\n" +
                       "• 좌측 대나무 덤불: 시야 차단, 백련 창수 견제에 유리\n" +
                       "• 북동쪽 약초 선반: 약초 수레와 피난민 보호 지점\n" +
                       "• 목표: 유달근 격파 전에 보호 대상이 무너지지 않게 전열 유지";
            }

            return def == null ? string.Empty : def.mapHint;
        }

        return "폐사당 고개 방어전\n" +
               "• 중앙 돌계단: 1칸 병목, 전열 1명으로 적 진입 차단\n" +
               "• 좌측 대나무숲: 이동 비용 2, 원거리 시야 차단, 독침·암기 유리\n" +
               "• 우측 낡은 다리: 빠른 우회로지만 밧줄 절단으로 붕괴 가능\n" +
               "• 상단 사당/누각: 고저 2~3, 원거리 사거리 +1과 명중 보너스\n" +
               "• 향로·등불·석등: 연막, 화염, 낙석으로 전투 흐름 변환";
    }

    private static bool IsWildlifeBattle(string battleId)
    {
        return battleId == HubController.WolfPassBattleId ||
               battleId == HubController.TigerRavineBattleId ||
               battleId == HubController.LeopardCliffBattleId;
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

    private static bool DrawPaintedMapPreview(Rect rect, string resourcePath, float s)
    {
        Texture2D texture = LoadMapPreviewTexture(resourcePath);
        if (texture == null)
        {
            return false;
        }

        GUI.DrawTexture(rect, texture, ScaleMode.ScaleAndCrop);
        Color edge = new Color(0.87f, 0.67f, 0.32f, 0.82f);
        float thick = Mathf.Max(1f, s);
        UiTheme.DrawFill(new Rect(rect.x, rect.y, rect.width, thick), edge);
        UiTheme.DrawFill(new Rect(rect.x, rect.yMax - thick, rect.width, thick), edge);
        UiTheme.DrawFill(new Rect(rect.x, rect.y, thick, rect.height), edge);
        UiTheme.DrawFill(new Rect(rect.xMax - thick, rect.y, thick, rect.height), edge);
        return true;
    }

    private static Texture2D LoadMapPreviewTexture(string resourcePath)
    {
        if (string.IsNullOrEmpty(resourcePath))
        {
            return null;
        }

        Texture2D cached;
        if (MapPreviewTextureCache.TryGetValue(resourcePath, out cached))
        {
            return cached;
        }

        Sprite sprite = Resources.Load<Sprite>(resourcePath);
        Texture2D texture = sprite != null ? sprite.texture : Resources.Load<Texture2D>(resourcePath);
        MapPreviewTextureCache[resourcePath] = texture;
        return texture;
    }

    private static void DrawBanditLairMapPreview(Rect rect, float s)
    {
        UiTheme.DrawFill(rect, new Color(0.10f, 0.13f, 0.09f, 0.46f));

        Color woods = new Color(0.14f, 0.34f, 0.17f, 0.92f);
        Color road = new Color(0.48f, 0.38f, 0.24f, 0.94f);
        Color mud = new Color(0.25f, 0.20f, 0.14f, 0.92f);
        Color tower = new Color(0.45f, 0.28f, 0.16f, 0.94f);
        Color cave = new Color(0.18f, 0.16f, 0.13f, 0.96f);
        Color mark = new Color(0.96f, 0.78f, 0.24f, 1f);

        UiTheme.DrawFill(new Rect(rect.x + 8f * s, rect.y + 18f * s, 32f * s, rect.height - 32f * s), woods);
        UiTheme.DrawFill(new Rect(rect.center.x - 12f * s, rect.y + 22f * s, 24f * s, rect.height - 38f * s), road);
        UiTheme.DrawFill(new Rect(rect.x + 38f * s, rect.center.y - 8f * s, rect.width - 76f * s, 16f * s), mud);
        UiTheme.DrawFill(new Rect(rect.xMax - 44f * s, rect.y + 30f * s, 30f * s, 52f * s), tower);
        UiTheme.DrawFill(new Rect(rect.center.x - 34f * s, rect.y + 12f * s, 68f * s, 34f * s), cave);
        UiTheme.DrawFill(new Rect(rect.center.x - 7f * s, rect.y + 20f * s, 14f * s, 14f * s), mark);
        UiTheme.DrawFill(new Rect(rect.center.x - 26f * s, rect.center.y + 22f * s, 14f * s, 14f * s),
                         new Color(0.72f, 0.18f, 0.10f, 0.88f));
        UiTheme.DrawFill(new Rect(rect.center.x + 24f * s, rect.center.y + 2f * s, 14f * s, 14f * s),
                         new Color(0.72f, 0.18f, 0.10f, 0.88f));

        GUI.Label(new Rect(rect.x, rect.y + 2f * s, rect.width, 18f * s), "폐광 / 보급 상자",
                  new GUIStyle(UiTheme.SmallMuted) { alignment = TextAnchor.MiddleCenter });
        GUI.Label(new Rect(rect.x, rect.yMax - 20f * s, rect.width, 18f * s), "아군 진입",
                  new GUIStyle(UiTheme.SmallMuted) { alignment = TextAnchor.MiddleCenter });
    }

    private static void DrawSeorakPassMapPreview(Rect rect, float s)
    {
        UiTheme.DrawFill(rect, new Color(0.10f, 0.14f, 0.15f, 0.46f));

        Color cliff = new Color(0.34f, 0.36f, 0.34f, 0.94f);
        Color road = new Color(0.52f, 0.46f, 0.34f, 0.94f);
        Color bamboo = new Color(0.13f, 0.34f, 0.25f, 0.92f);
        Color bridge = new Color(0.43f, 0.27f, 0.15f, 0.96f);
        Color cart = new Color(0.92f, 0.76f, 0.38f, 1f);
        Color danger = new Color(0.72f, 0.18f, 0.10f, 0.88f);

        UiTheme.DrawFill(new Rect(rect.x + 8f * s, rect.y + 18f * s, 34f * s, rect.height - 36f * s), bamboo);
        UiTheme.DrawFill(new Rect(rect.center.x - 10f * s, rect.y + 24f * s, 20f * s, rect.height - 42f * s), road);
        UiTheme.DrawFill(new Rect(rect.center.x - 18f * s, rect.center.y - 8f * s, 52f * s, 16f * s), bridge);
        UiTheme.DrawFill(new Rect(rect.xMax - 46f * s, rect.y + 34f * s, 32f * s, 62f * s), cliff);
        UiTheme.DrawFill(new Rect(rect.xMax - 36f * s, rect.y + 44f * s, 16f * s, 16f * s), cart);
        UiTheme.DrawFill(new Rect(rect.center.x + 26f * s, rect.center.y + 18f * s, 14f * s, 14f * s), danger);
        UiTheme.DrawFill(new Rect(rect.center.x - 34f * s, rect.center.y + 4f * s, 14f * s, 14f * s), danger);

        GUI.Label(new Rect(rect.x, rect.y + 2f * s, rect.width, 18f * s), "약초 수레 / H3 선반",
                  new GUIStyle(UiTheme.SmallMuted) { alignment = TextAnchor.MiddleCenter });
        GUI.Label(new Rect(rect.x, rect.yMax - 20f * s, rect.width, 18f * s), "박성준·백련 진입",
                  new GUIStyle(UiTheme.SmallMuted) { alignment = TextAnchor.MiddleCenter });
    }

    private static void DrawWildlifeMapPreview(Rect rect, float s, string battleId)
    {
        UiTheme.DrawFill(rect, new Color(0.10f, 0.12f, 0.10f, 0.46f));

        Color woods = new Color(0.13f, 0.32f, 0.18f, 0.92f);
        Color road = new Color(0.50f, 0.43f, 0.29f, 0.94f);
        Color water = new Color(0.18f, 0.43f, 0.51f, 0.88f);
        Color rock = new Color(0.42f, 0.38f, 0.31f, 0.94f);
        Color high = new Color(0.55f, 0.49f, 0.33f, 0.96f);
        Color danger = new Color(0.72f, 0.18f, 0.10f, 0.88f);
        Color mark = new Color(0.96f, 0.78f, 0.24f, 1f);

        if (battleId == HubController.WolfPassBattleId)
        {
            UiTheme.DrawFill(new Rect(rect.x + 8f * s, rect.y + 18f * s, 34f * s, rect.height - 34f * s), woods);
            UiTheme.DrawFill(new Rect(rect.x + 38f * s, rect.center.y - 8f * s, rect.width - 72f * s, 16f * s), water);
            UiTheme.DrawFill(new Rect(rect.center.x - 10f * s, rect.y + 20f * s, 20f * s, rect.height - 42f * s), road);
            UiTheme.DrawFill(new Rect(rect.xMax - 42f * s, rect.y + 20f * s, 28f * s, 58f * s), high);
            UiTheme.DrawFill(new Rect(rect.xMax - 34f * s, rect.y + 28f * s, 14f * s, 14f * s), mark);
            UiTheme.DrawFill(new Rect(rect.center.x - 30f * s, rect.center.y + 20f * s, 14f * s, 14f * s), danger);
            GUI.Label(new Rect(rect.x, rect.y + 2f * s, rect.width, 18f * s), "늑대 굴 / H2 능선",
                      new GUIStyle(UiTheme.SmallMuted) { alignment = TextAnchor.MiddleCenter });
        }
        else if (battleId == HubController.TigerRavineBattleId)
        {
            UiTheme.DrawFill(new Rect(rect.x + 10f * s, rect.y + 38f * s, 42f * s, 58f * s), woods);
            UiTheme.DrawFill(new Rect(rect.center.x - 8f * s, rect.y + 22f * s, 16f * s, rect.height - 44f * s), rock);
            UiTheme.DrawFill(new Rect(rect.x + 46f * s, rect.center.y - 8f * s, rect.width - 92f * s, 16f * s), road);
            UiTheme.DrawFill(new Rect(rect.xMax - 48f * s, rect.y + 18f * s, 34f * s, 72f * s), high);
            UiTheme.DrawFill(new Rect(rect.xMax - 35f * s, rect.y + 24f * s, 16f * s, 16f * s), mark);
            UiTheme.DrawFill(new Rect(rect.center.x + 12f * s, rect.center.y + 18f * s, 16f * s, 16f * s), danger);
            GUI.Label(new Rect(rect.x, rect.y + 2f * s, rect.width, 18f * s), "바위 선반 / H3",
                      new GUIStyle(UiTheme.SmallMuted) { alignment = TextAnchor.MiddleCenter });
        }
        else
        {
            UiTheme.DrawFill(new Rect(rect.x + 10f * s, rect.y + 18f * s, 36f * s, 58f * s), woods);
            UiTheme.DrawFill(new Rect(rect.x + 48f * s, rect.y + 26f * s, rect.width - 96f * s, 14f * s), road);
            UiTheme.DrawFill(new Rect(rect.center.x - 12f * s, rect.center.y - 8f * s, 24f * s, 16f * s), road);
            UiTheme.DrawFill(new Rect(rect.x + 42f * s, rect.y + 76f * s, rect.width - 58f * s, 22f * s), rock);
            UiTheme.DrawFill(new Rect(rect.xMax - 42f * s, rect.y + 36f * s, 28f * s, 54f * s), high);
            UiTheme.DrawFill(new Rect(rect.xMax - 34f * s, rect.y + 44f * s, 14f * s, 14f * s), mark);
            UiTheme.DrawFill(new Rect(rect.center.x - 36f * s, rect.center.y + 18f * s, 14f * s, 14f * s), danger);
            GUI.Label(new Rect(rect.x, rect.y + 2f * s, rect.width, 18f * s), "약초 선반 / 절벽길",
                      new GUIStyle(UiTheme.SmallMuted) { alignment = TextAnchor.MiddleCenter });
        }

        GUI.Label(new Rect(rect.x, rect.yMax - 20f * s, rect.width, 18f * s), "아군 진입",
                  new GUIStyle(UiTheme.SmallMuted) { alignment = TextAnchor.MiddleCenter });
    }
}
}
