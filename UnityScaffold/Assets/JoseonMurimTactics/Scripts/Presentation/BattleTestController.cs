using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace JoseonMurimTactics
{
[DisallowMultipleComponent]
public sealed class BattleTestController : MonoBehaviour
{
    public int width = 16;
    public int height = 12;
    public float tileWidth = 1.16f;
    public float tileHeight = 0.62f;
    public bool useAuthoredSceneMap = true;
    public bool useTilemapBattlefield = true;
    public bool useLegacyDiamondTerrain;
    public bool useCanvasHud = true;
    public BattleTestUnitDefinition[] unitDefinitions = new BattleTestUnitDefinition[0];

    private const int SmokeCoverBonus = 2;
    private const int CoverInteractBonus = 2;
    private const int FireInteractDamage = 4;
    private const int FallDamage = 10;
    private const int HighGroundRangeBonusElevation = 2;
    private const string MapDisplayName = "백두산 설문 관문전";
    private const string MapConcept =
        "중앙 1칸 협로, 좌측 설죽림 우회로, 우측 절벽 고지, 얼어붙은 여울과 붕괴 가능한 다리 밧줄을 쓰는 대표 수작업 전장";
    private static readonly bool UseLegacyOnGui = false;

    private readonly List<BattleTestUnit> units = new List<BattleTestUnit>();
    private readonly List<string> battleLog = new List<string>();
    private readonly List<BattleTestInteractable> interactables = new List<BattleTestInteractable>();
    private readonly PhaseTurnController phaseTurn = new PhaseTurnController();
    private readonly System.Random random = new System.Random(20260608);
    private BattleTestTile[,] tiles;
    private Sprite diamondSprite;
    private Sprite softDiamondSprite;
    private Sprite detailSprite;
    private Sprite dotSprite;
    private Sprite mountainRidgeSprite;
    private Sprite pineSilhouetteSprite;
    private BattleTilemapBattlefield tilemapBattlefield;
    private Coroutine mapIntroCoroutine;
    private bool mapAssetSpritesLoaded;
    private readonly Dictionary<TerrainType, Sprite> terrainAssetSprites = new Dictionary<TerrainType, Sprite>();
    private readonly Dictionary<string, Sprite> interactableAssetSprites = new Dictionary<string, Sprite>();
    private GUIStyle panelStyle;
    private GUIStyle labelStyle;
    private GUIStyle titleStyle;
    private GUIStyle smallStyle;
    private GUIStyle logStyle;
    private Canvas hudCanvas;
    private Text hudPhaseText;
    private Text hudActiveText;
    private Text hudResourceText;
    private Text hudObjectiveText;
    private Text hudPhaseListText;
    private Text hudLogText;
    private Text hudInspectText;
    private Text hudForecastText;
    private Text hudRosterText;
    private Button hudMoveButton;
    private Button hudAttackButton;
    private Button hudSkillButton;
    private Button hudGuardButton;
    private Button hudInteractButton;
    private Button hudPhaseEndButton;
    private Button hudResetButton;
    private readonly List<Button> hudCommandButtons = new List<Button>();
    private BattleHUDController battleHud;
    private BattleTestUnit activeUnit;
    private BattleTestUnit hoveredUnit;
    private BattleTestTile hoveredTile;
    private int round = 1;
    private bool busy;
    private bool aiQueued;
    private bool battleOver;
    private bool showThreatOverlay;
    private bool showElevationOverlay;
    private bool showCoverOverlay;
    private bool showSightOverlay;
    private bool showObjectiveOverlay = true;
    private bool showTerrainNames;
    private bool showHudLog = true;
    private bool scoutMode;
    private string hudNotice;
    private float hudNoticeUntil;
    private BattleCommandMode commandMode = BattleCommandMode.Move;

    private void Start()
    {
        BuildBattle();
    }

    private void Update()
    {
        if (battleOver)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                BuildBattle();
            }

            return;
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            BuildBattle();
            return;
        }

        UpdateHover();

        if (busy)
        {
            return;
        }

        if (phaseTurn.IsEnemyPhase)
        {
            if (!aiQueued)
            {
                aiQueued = true;
                StartCoroutine(RunEnemyPhase());
            }

            return;
        }

        if (phaseTurn.IsPlayerPhase && Input.GetKeyDown(KeyCode.S))
        {
            ToggleScoutMode();
            return;
        }

        if (scoutMode && (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)))
        {
            ExitScoutMode();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            EndTurn();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            showThreatOverlay = !showThreatOverlay;
            AddLog(showThreatOverlay ? "[지도] 적 위협 범위 표시" : "[지도] 적 위협 범위 숨김");
            RefreshHighlights();
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            showElevationOverlay = !showElevationOverlay;
            AddLog(showElevationOverlay ? "[지도] 고저 오버레이 표시" : "[지도] 고저 오버레이 숨김");
            RefreshHighlights();
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            showCoverOverlay = !showCoverOverlay;
            AddLog(showCoverOverlay ? "[지도] 엄폐 오버레이 표시" : "[지도] 엄폐 오버레이 숨김");
            RefreshHighlights();
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            showSightOverlay = !showSightOverlay;
            AddLog(showSightOverlay ? "[지도] 시야 차단 표시" : "[지도] 시야 차단 숨김");
            RefreshHighlights();
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            showObjectiveOverlay = !showObjectiveOverlay;
            AddLog(showObjectiveOverlay ? "[지도] 목표 표시" : "[지도] 목표 숨김");
            RefreshHighlights();
        }

        showTerrainNames = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

        if (activeUnit != null && Input.GetKeyDown(KeyCode.Escape))
        {
            SetCommandMode(BattleCommandMode.Move);
        }
        else if (activeUnit != null && Input.GetKeyDown(KeyCode.Alpha1))
        {
            SetCommandMode(BattleCommandMode.Move);
        }
        else if (activeUnit != null && Input.GetKeyDown(KeyCode.Alpha2))
        {
            SetCommandMode(BattleCommandMode.Attack);
        }
        else if (activeUnit != null && Input.GetKeyDown(KeyCode.Alpha3))
        {
            SetCommandMode(BattleCommandMode.Skill);
        }
        else if (activeUnit != null && Input.GetKeyDown(KeyCode.Alpha4))
        {
            GuardActiveUnit();
        }
        else if (activeUnit != null && Input.GetKeyDown(KeyCode.Alpha5))
        {
            SetCommandMode(BattleCommandMode.Interact);
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (PointerOverHud(Input.mousePosition))
            {
                return;
            }

            HandlePointer(Input.mousePosition);
        }
    }

    private void LateUpdate()
    {
        RefreshTileNameVisibility();
        RefreshBattleHud();
    }

    private void OnGUI()
    {
        if (!UseLegacyOnGui)
        {
            return;
        }

        EnsureGuiStyles();

        DrawActivePanel();
        DrawObjectivePanel();
        DrawTurnQueuePanel();
        DrawLogPanel();
        DrawInspectPanel();
        DrawForecastPanel();
        DrawRosterPanel();

        if (!battleOver)
        {
            return;
        }

        GUI.Box(new Rect((Screen.width * 0.5f) - 180f, 24f, 360f, 76f), GUIContent.none, panelStyle);
        GUI.Label(new Rect((Screen.width * 0.5f) - 146f, 42f, 310f, 28f), "전투 종료", titleStyle);
    }

    private void DrawActivePanel()
    {
        GUI.Box(new Rect(18f, 18f, 340f, 326f), GUIContent.none, panelStyle);
        string activeName = activeUnit == null ? "선택 없음" : activeUnit.definition.displayName;
        string hp = activeUnit == null ? string.Empty : $"{activeUnit.hp}/{activeUnit.definition.maxHp}";
        string side = activeUnit == null ? string.Empty : FactionLabel(activeUnit.definition.faction);

        GUI.Label(new Rect(34f, 30f, 300f, 28f), $"제 {round}턴 · {PhaseLabel(phaseTurn.CurrentPhase)}", titleStyle);
        GUI.Label(new Rect(34f, 62f, 300f, 24f), $"현재 행동: {activeName}", labelStyle);
        GUI.Label(new Rect(34f, 88f, 300f, 22f), $"진영: {side}   체력: {hp}", smallStyle);

        if (activeUnit != null)
        {
            GUI.Label(new Rect(34f, 112f, 300f, 22f),
                      $"이동: {ActionText(activeUnit.moved)}   행동: {ActionText(activeUnit.acted)}", smallStyle);
            GUI.Label(new Rect(34f, 136f, 300f, 22f),
                      $"내공: {activeUnit.inner}/{activeUnit.definition.maxInner}   방어: {YesNo(activeUnit.guarded)}",
                      smallStyle);
            GUI.Label(new Rect(34f, 160f, 300f, 22f), $"명령: {CommandLabel(commandMode)}", labelStyle);
        }

        bool playerTurn = phaseTurn.IsPlayerPhase && !scoutMode && activeUnit != null &&
                          activeUnit.definition.faction == Faction.Ally && !busy && !battleOver;
        GUI.enabled = playerTurn;

        if (GUI.Button(new Rect(34f, 192f, 70f, 28f), "이동"))
        {
            SetCommandMode(BattleCommandMode.Move);
        }

        GUI.enabled = playerTurn && activeUnit != null && !activeUnit.acted;
        if (GUI.Button(new Rect(112f, 192f, 70f, 28f), "공격"))
        {
            SetCommandMode(BattleCommandMode.Attack);
        }

        GUI.enabled = playerTurn && activeUnit != null && CanUseSpecial(activeUnit);
        if (GUI.Button(new Rect(190f, 192f, 70f, 28f), "무공"))
        {
            SetCommandMode(BattleCommandMode.Skill);
        }

        GUI.enabled = playerTurn && activeUnit != null && !activeUnit.acted;
        if (GUI.Button(new Rect(268f, 192f, 70f, 28f), "방어"))
        {
            GuardActiveUnit();
        }

        GUI.enabled = playerTurn && activeUnit != null && !activeUnit.acted && HasUsableInteractable(activeUnit);
        if (GUI.Button(new Rect(34f, 230f, 92f, 30f), "지형"))
        {
            SetCommandMode(BattleCommandMode.Interact);
        }

        GUI.enabled = playerTurn;
        if (GUI.Button(new Rect(134f, 230f, 92f, 30f), "페이즈 종료"))
        {
            EndTurn();
        }

        if (GUI.Button(new Rect(234f, 230f, 104f, 30f), "재시작"))
        {
            BuildBattle();
        }

        GUI.enabled = true;

        if (activeUnit != null)
        {
            string skillLine = activeUnit.definition.specialName;
            string cooldown =
                activeUnit.specialCooldownLeft > 0 ? $"대기 {activeUnit.specialCooldownLeft}턴" : "준비됨";
            GUI.Label(new Rect(34f, 266f, 304f, 22f), $"무공: {skillLine} ({cooldown})", smallStyle);
            GUI.Label(new Rect(34f, 290f, 304f, 22f),
                      $"민첩: {AgilityValue(activeUnit)}   반격: {CounterSummary(activeUnit)}", smallStyle);
        }
    }

    private void DrawTurnQueuePanel()
    {
        float x = Screen.width - 386f;
        GUI.Box(new Rect(x, 18f, 368f, 194f), GUIContent.none, panelStyle);
        GUI.Label(new Rect(x + 16f, 30f, 336f, 26f), "페이즈", titleStyle);

        List<BattleTestUnit> queue = GetTurnQueuePreview(7);
        for (int i = 0; i < queue.Count; i++)
        {
            BattleTestUnit unit = queue[i];
            string marker = unit == activeUnit ? "현재" : (unit.acted ? "완료" : "대기");
            string state = unit.defeated ? "전투불능" : UnitStatusText(unit);
            string line = $"{marker}  {unit.definition.displayName}  {FactionLabel(unit.definition.faction)}  {state}";
            GUI.Label(new Rect(x + 16f, 60f + (i * 18f), 336f, 18f), line, i == 0 ? labelStyle : smallStyle);
        }
    }

    private void DrawObjectivePanel()
    {
        float widthPx = Mathf.Min(500f, Screen.width - 780f);
        if (widthPx < 300f)
        {
            return;
        }

        float x = (Screen.width * 0.5f) - (widthPx * 0.5f);
        GUI.Box(new Rect(x, 18f, widthPx, 112f), GUIContent.none, panelStyle);
        GUI.Label(new Rect(x + 16f, 30f, widthPx - 32f, 24f), "목표", titleStyle);
        GUI.Label(new Rect(x + 16f, 58f, widthPx - 32f, 20f), "중원 감찰단을 설문 관문 안쪽 제단 전에 저지", labelStyle);
        GUI.Label(new Rect(x + 16f, 80f, widthPx - 32f, 18f), "추천: 설죽림 엄폐, 절벽 고지 +2, 향로/등불/밧줄 활용",
                  smallStyle);
        GUI.Label(new Rect(x + 16f, 98f, widthPx - 32f, 18f), "위험: 낙하·빙판·화염·연막 지형", smallStyle);
    }

    private void DrawLogPanel()
    {
        float x = Screen.width - 386f;
        GUI.Box(new Rect(x, 224f, 368f, 244f), GUIContent.none, panelStyle);
        GUI.Label(new Rect(x + 16f, 236f, 336f, 26f), "전투 기록", titleStyle);

        int start = Mathf.Max(0, battleLog.Count - 9);
        for (int i = start; i < battleLog.Count; i++)
        {
            GUI.Label(new Rect(x + 16f, 268f + ((i - start) * 22f), 336f, 22f), battleLog[i], logStyle);
        }
    }

    private void DrawInspectPanel()
    {
        GUI.Box(new Rect(18f, 356f, 340f, 190f), GUIContent.none, panelStyle);
        GUI.Label(new Rect(34f, 368f, 300f, 26f), "정보", titleStyle);

        if (hoveredUnit != null)
        {
            GUI.Label(new Rect(34f, 398f, 300f, 22f),
                      $"{hoveredUnit.definition.displayName} ({FactionLabel(hoveredUnit.definition.faction)})",
                      labelStyle);
            GUI.Label(
                new Rect(34f, 422f, 300f, 22f),
                $"체력 {hoveredUnit.hp}/{hoveredUnit.definition.maxHp}   방어 {DefenseValue(hoveredUnit, TileAt(hoveredUnit.cell))}",
                smallStyle);
            GUI.Label(
                new Rect(34f, 446f, 300f, 22f),
                $"{hoveredUnit.definition.age}세 · {hoveredUnit.definition.mbti} · {hoveredUnit.definition.elementName}/{hoveredUnit.definition.weaponName}",
                smallStyle);
            GUI.Label(new Rect(34f, 470f, 300f, 22f),
                      $"상태: {UnitStatusText(hoveredUnit)}   무공: {hoveredUnit.definition.specialName}", smallStyle);
            return;
        }

        if (hoveredTile != null)
        {
            BattleTestInteractable prop = GetInteractableAt(hoveredTile.cell);
            GUI.Label(new Rect(34f, 398f, 300f, 22f),
                      $"{TerrainLabel(hoveredTile.terrain)}  ({hoveredTile.cell.x},{hoveredTile.cell.y})", labelStyle);
            GUI.Label(new Rect(34f, 422f, 300f, 22f),
                      $"이동 비용 {hoveredTile.moveCost}   엄폐 +{hoveredTile.coverBonus}", smallStyle);
            GUI.Label(new Rect(34f, 446f, 300f, 22f),
                      $"고저 {hoveredTile.elevation}   진입 {YesNo(hoveredTile.walkable)}", smallStyle);
            if (prop != null)
            {
                int distance = activeUnit == null ? -1 : GridDistance(activeUnit.cell, prop.cell);
                bool usable = activeUnit != null && !activeUnit.acted && distance <= 1;
                GUI.Label(new Rect(34f, 470f, 300f, 22f), $"지형지물: {prop.displayName}", smallStyle);
                GUI.Label(new Rect(34f, 494f, 300f, 22f),
                          $"효과: {InteractableEffectText(prop.kind)}   사용: {YesNo(usable)}", smallStyle);
            }
            else
            {
                GUI.Label(new Rect(34f, 470f, 300f, 22f), $"위험: {TileHazardText(hoveredTile)}", smallStyle);
            }
            return;
        }

        GUI.Label(new Rect(34f, 398f, 300f, 22f), "가리킨 대상 없음", smallStyle);
    }

    private void DrawForecastPanel()
    {
        float x = Screen.width - 386f;
        GUI.Box(new Rect(x, 452f, 368f, 224f), GUIContent.none, panelStyle);
        GUI.Label(new Rect(x + 16f, 464f, 336f, 26f), "전투 예측", titleStyle);

        BattleForecast forecast = BuildForecast(activeUnit, hoveredUnit);
        if (!forecast.valid)
        {
            if (!string.IsNullOrEmpty(forecast.actorName))
            {
                GUI.Label(new Rect(x + 16f, 494f, 336f, 22f),
                          $"{forecast.actorName} -> {forecast.targetName}  [{forecast.commandName}]", labelStyle);
                GUI.Label(new Rect(x + 16f, 520f, 336f, 20f), $"불가: {forecast.invalidReason}", smallStyle);
                GUI.Label(new Rect(x + 16f, 542f, 336f, 20f), $"거리 {forecast.distance} / 사거리 {forecast.range}",
                          smallStyle);
                GUI.Label(new Rect(x + 16f, 564f, 336f, 20f), forecast.costText, smallStyle);
            }
            else
            {
                GUI.Label(new Rect(x + 16f, 494f, 336f, 24f), forecast.invalidReason, smallStyle);
            }

            return;
        }

        GUI.Label(new Rect(x + 16f, 494f, 336f, 22f),
                  $"{forecast.actorName} -> {forecast.targetName}  [{forecast.commandName}]", labelStyle);
        GUI.Label(new Rect(x + 16f, 520f, 336f, 20f),
                  $"거리 {forecast.distance} / 사거리 {forecast.range}: {forecast.rangeText}", smallStyle);
        string hitText =
            forecast.neededRollText == "판정 없음"
                ? "명중: 판정 없음"
                : $"명중: d20 + {forecast.attackBonus} + 고저 {forecast.heightBonus} + 지형 {forecast.terrainBonus} vs 방어 {forecast.defense}";
        GUI.Label(new Rect(x + 16f, 542f, 336f, 20f), hitText, smallStyle);
        GUI.Label(new Rect(x + 16f, 564f, 336f, 20f), $"필요값: {forecast.neededRollText}", smallStyle);
        GUI.Label(new Rect(x + 16f, 586f, 336f, 20f), forecast.damageText, smallStyle);
        GUI.Label(new Rect(x + 16f, 608f, 336f, 20f), forecast.hpAfterText, smallStyle);
        GUI.Label(new Rect(x + 16f, 630f, 336f, 20f), $"{forecast.counterText}   {forecast.followUpText}", smallStyle);
        GUI.Label(new Rect(x + 16f, 650f, 336f, 20f), forecast.costText, smallStyle);
    }

    private void DrawRosterPanel()
    {
        float y = Screen.height - 118f;
        GUI.Box(new Rect(18f, y, Screen.width - 36f, 100f), GUIContent.none, panelStyle);
        GUI.Label(new Rect(34f, y + 12f, 280f, 26f), "부대 현황", titleStyle);

        float cardWidth = Mathf.Min(220f, (Screen.width - 80f) / Mathf.Max(1, units.Count));
        for (int i = 0; i < units.Count; i++)
        {
            BattleTestUnit unit = units[i];
            float x = 34f + (i * cardWidth);
            string prefix = unit == activeUnit ? "> " : string.Empty;
            GUI.Label(new Rect(x, y + 42f, cardWidth - 8f, 22f), $"{prefix}{unit.definition.displayName}",
                      unit == activeUnit ? labelStyle : smallStyle);
            GUI.Label(new Rect(x, y + 64f, cardWidth - 8f, 22f),
                      $"체력 {unit.hp}/{unit.definition.maxHp}  {UnitStatusText(unit)}", smallStyle);
        }
    }

    private void EnsureCanvasHud()
    {
        if (hudCanvas != null)
        {
            return;
        }

        EnsureEventSystem();
        UiTheme.EnsureStyles();
        hudCommandButtons.Clear();

        GameObject canvasObject = new GameObject("BattleCanvasHud", typeof(RectTransform), typeof(Canvas),
                                                 typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        hudCanvas = canvasObject.GetComponent<Canvas>();
        hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        hudCanvas.sortingOrder = 50;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform root = canvasObject.GetComponent<RectTransform>();
        RectTransform active = CreatePanel(root, "현재 행동 패널", new Vector2(0f, 1f), new Vector2(0f, 1f),
                                           new Vector2(0f, 1f), new Vector2(18f, -18f), new Vector2(340f, 326f));
        hudPhaseText = CreateHudText(active, "페이즈", new Rect(16f, 12f, 308f, 30f), string.Empty, 22f,
                                     TextAnchor.MiddleLeft, true);
        hudActiveText = CreateHudText(active, "행동 유닛", new Rect(16f, 48f, 308f, 54f), string.Empty, 17f,
                                      TextAnchor.UpperLeft, false);
        hudResourceText = CreateHudText(active, "자원", new Rect(16f, 106f, 308f, 54f), string.Empty, 15f,
                                        TextAnchor.UpperLeft, false);
        hudMoveButton = CreateHudButton(active, "이동", new Rect(16f, 176f, 70f, 30f), "이동",
                                        () => SetCommandMode(BattleCommandMode.Move));
        hudAttackButton = CreateHudButton(active, "공격", new Rect(94f, 176f, 70f, 30f), "공격",
                                          () => SetCommandMode(BattleCommandMode.Attack));
        hudSkillButton = CreateHudButton(active, "무공", new Rect(172f, 176f, 70f, 30f), "무공",
                                         () => SetCommandMode(BattleCommandMode.Skill));
        hudGuardButton = CreateHudButton(active, "방어", new Rect(250f, 176f, 70f, 30f), "방어", GuardActiveUnit);
        hudInteractButton = CreateHudButton(active, "지형", new Rect(16f, 218f, 94f, 32f), "지형",
                                            () => SetCommandMode(BattleCommandMode.Interact));
        hudPhaseEndButton =
            CreateHudButton(active, "페이즈 종료", new Rect(118f, 218f, 104f, 32f), "페이즈 종료", EndTurn);
        hudResetButton = CreateHudButton(active, "재시작", new Rect(230f, 218f, 90f, 32f), "재시작", BuildBattle);
        hudObjectiveText = CreateHudText(active, "전술 힌트", new Rect(16f, 264f, 308f, 50f), string.Empty, 14f,
                                         TextAnchor.UpperLeft, false);

        RectTransform objective = CreatePanel(root, "목표 패널", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                                              new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(500f, 112f));
        CreateHudText(objective, "목표 제목", new Rect(16f, 12f, 468f, 24f), "목표", 20f, TextAnchor.MiddleLeft,
                      true);
        CreateHudText(
            objective, "목표 본문", new Rect(16f, 42f, 468f, 76f),
            "중원 사절 호위대를 제압\n추천: 대나무숲 엄폐, 지붕 고저 +2, 향로/등불 활용\n위험: 화염 칸 진입 시 피해",
            15f, TextAnchor.UpperLeft, false);

        RectTransform phase = CreatePanel(root, "페이즈 패널", new Vector2(1f, 1f), new Vector2(1f, 1f),
                                          new Vector2(1f, 1f), new Vector2(-18f, -18f), new Vector2(368f, 194f));
        CreateHudText(phase, "페이즈 제목", new Rect(16f, 12f, 336f, 24f), "행동 순서", 20f, TextAnchor.MiddleLeft,
                      true);
        hudPhaseListText = CreateHudText(phase, "페이즈 목록", new Rect(16f, 42f, 336f, 144f), string.Empty, 14f,
                                         TextAnchor.UpperLeft, false);

        RectTransform log = CreatePanel(root, "전투 기록 패널", new Vector2(1f, 1f), new Vector2(1f, 1f),
                                        new Vector2(1f, 1f), new Vector2(-18f, -224f), new Vector2(368f, 244f));
        CreateHudText(log, "전투 기록 제목", new Rect(16f, 12f, 336f, 24f), "전투 기록", 20f, TextAnchor.MiddleLeft,
                      true);
        hudLogText = CreateHudText(log, "전투 기록", new Rect(16f, 42f, 336f, 200f), string.Empty, 14f,
                                   TextAnchor.UpperLeft, false);

        RectTransform inspect = CreatePanel(root, "정보 패널", new Vector2(0f, 1f), new Vector2(0f, 1f),
                                            new Vector2(0f, 1f), new Vector2(18f, -356f), new Vector2(340f, 190f));
        CreateHudText(inspect, "정보 제목", new Rect(16f, 12f, 308f, 24f), "정보", 20f, TextAnchor.MiddleLeft,
                      true);
        hudInspectText = CreateHudText(inspect, "정보 본문", new Rect(16f, 42f, 308f, 144f), string.Empty, 14f,
                                       TextAnchor.UpperLeft, false);

        RectTransform forecast = CreatePanel(root, "전투 예측 패널", new Vector2(1f, 1f), new Vector2(1f, 1f),
                                             new Vector2(1f, 1f), new Vector2(-18f, -452f), new Vector2(368f, 224f));
        CreateHudText(forecast, "전투 예측 제목", new Rect(16f, 12f, 336f, 24f), "전투 예측", 20f,
                      TextAnchor.MiddleLeft, true);
        hudForecastText = CreateHudText(forecast, "전투 예측", new Rect(16f, 40f, 336f, 184f), string.Empty, 13f,
                                        TextAnchor.UpperLeft, false);

        RectTransform roster = CreatePanel(root, "부대 현황 패널", new Vector2(0f, 0f), new Vector2(1f, 0f),
                                           new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(-36f, 100f));
        CreateHudText(roster, "부대 현황 제목", new Rect(16f, 12f, 240f, 24f), "부대 현황", 20f,
                      TextAnchor.MiddleLeft, true);
        hudRosterText = CreateHudText(roster, "부대 현황", new Rect(16f, 42f, 1220f, 54f), string.Empty, 14f,
                                      TextAnchor.UpperLeft, false);

        RefreshCanvasHud();
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null)
        {
            return;
        }

        GameObject eventSystemObject =
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        DontDestroyOnLoad(eventSystemObject);
    }

    private RectTransform CreatePanel(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
                                      Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        Image image = panel.GetComponent<Image>();
        image.color = new Color(0.09f, 0.085f, 0.075f, 0.86f);
        return rect;
    }

    private Text CreateHudText(RectTransform parent, string name, Rect frame, string text, float size,
                               TextAnchor alignment, bool bold)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        SetTopLeft(rect, frame);

        Text uiText = textObject.GetComponent<Text>();
        uiText.text = text;
        uiText.font = UiTheme.Font;
        uiText.fontSize = Mathf.RoundToInt(size);
        uiText.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        uiText.alignment = alignment;
        uiText.color = bold ? new Color(0.96f, 0.90f, 0.78f, 1f) : new Color(0.88f, 0.84f, 0.74f, 1f);
        uiText.horizontalOverflow = HorizontalWrapMode.Wrap;
        uiText.verticalOverflow = VerticalWrapMode.Truncate;
        uiText.raycastTarget = false;
        uiText.supportRichText = true;
        return uiText;
    }

    private Button CreateHudButton(RectTransform parent, string name, Rect frame, string label, Action action)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        SetTopLeft(rect, frame);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.18f, 0.16f, 0.13f, 0.96f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.18f, 0.16f, 0.13f, 0.96f);
        colors.highlightedColor = new Color(0.33f, 0.28f, 0.20f, 1f);
        colors.selectedColor = new Color(0.50f, 0.36f, 0.17f, 1f);
        colors.pressedColor = new Color(0.62f, 0.42f, 0.18f, 1f);
        colors.disabledColor = new Color(0.12f, 0.115f, 0.10f, 0.55f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        button.onClick.AddListener(() => action());

        Text text = CreateHudText(rect, "라벨", new Rect(0f, 4f, frame.width, frame.height - 8f), label, 14f,
                                      TextAnchor.MiddleCenter, true);
        text.color = new Color(0.98f, 0.93f, 0.82f, 1f);

        if (name == "이동" || name == "공격" || name == "무공" || name == "지형")
        {
            hudCommandButtons.Add(button);
        }

        return button;
    }

    private static void SetTopLeft(RectTransform rect, Rect frame)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(frame.x, -frame.y);
        rect.sizeDelta = new Vector2(frame.width, frame.height);
    }

    private void RefreshCanvasHud()
    {
        if (hudCanvas == null)
        {
            return;
        }

        string phaseText = battleOver ? "전투 종료" : $"제 {round}턴 · {PhaseLabel(phaseTurn.CurrentPhase)}";
        string activeName = activeUnit == null ? "선택 없음" : activeUnit.definition.displayName;
        string side = activeUnit == null ? "-" : FactionLabel(activeUnit.definition.faction);
        string hp = activeUnit == null ? "-" : $"{activeUnit.hp}/{activeUnit.definition.maxHp}";
        SetText(hudPhaseText, phaseText);
        SetText(hudActiveText, $"현재 행동: {activeName}\n진영: {side}   체력: {hp}");

        if (activeUnit == null)
        {
            SetText(hudResourceText, "아군을 선택하세요.\n페이즈 종료로 적 페이즈 진행");
        }
        else
        {
            SetText(
                hudResourceText,
                $"이동: {ActionText(activeUnit.moved)} ({activeUnit.actions.movementLeft})   행동: {ActionText(activeUnit.acted)}\n내공: {activeUnit.inner}/{activeUnit.definition.maxInner}   반응: {ActionText(!activeUnit.CanReact)}   명령: {CommandLabel(commandMode)}");
        }

        SetText(hudObjectiveText,
                $"{MapDisplayName}\n{MapConcept}\nTab 위협 · H 고저 · C 엄폐 · V 시야 · O 목표 · Alt 지형명");
        SetText(hudPhaseListText, BuildPhaseListText());
        SetText(hudLogText, BuildLogText());
        SetText(hudInspectText, BuildInspectText());
        SetText(hudForecastText, BuildHudForecastText());
        SetText(hudRosterText, BuildRosterText());

        bool playerTurn = phaseTurn.IsPlayerPhase && !scoutMode && activeUnit != null &&
                          activeUnit.definition.faction == Faction.Ally && !busy && !battleOver;
        SetButtonEnabled(hudMoveButton, playerTurn && activeUnit.CanMove);
        SetButtonEnabled(hudAttackButton, playerTurn && activeUnit.CanUseMainAction);
        SetButtonEnabled(hudSkillButton, playerTurn && CanUseSpecial(activeUnit));
        SetButtonEnabled(hudGuardButton, playerTurn && activeUnit.CanUseMainAction);
        SetButtonEnabled(hudInteractButton,
                         playerTurn && activeUnit.CanUseMainAction && HasUsableInteractable(activeUnit));
        SetButtonEnabled(hudPhaseEndButton, phaseTurn.IsPlayerPhase && !busy && !battleOver);
        SetButtonEnabled(hudResetButton, true);
        RefreshCommandButtonState(hudMoveButton, commandMode == BattleCommandMode.Move && playerTurn);
        RefreshCommandButtonState(hudAttackButton, commandMode == BattleCommandMode.Attack && playerTurn);
        RefreshCommandButtonState(hudSkillButton, commandMode == BattleCommandMode.Skill && playerTurn);
        RefreshCommandButtonState(hudInteractButton, commandMode == BattleCommandMode.Interact && playerTurn);
    }

    private string BuildPhaseListText()
    {
        StringBuilder builder = new StringBuilder();
        List<BattleTestUnit> queue = GetTurnQueuePreview(7);
        for (int i = 0; i < queue.Count; i++)
        {
            BattleTestUnit unit = queue[i];
            string marker = unit == activeUnit ? "현재" : (unit.acted ? "완료" : "대기");
            builder.Append(marker)
                .Append("  ")
                .Append(unit.definition.displayName)
                .Append("  ")
                .Append(FactionLabel(unit.definition.faction))
                .Append("  ")
                .AppendLine(unit.defeated ? "전투불능" : UnitStatusText(unit));
        }

        return builder.ToString();
    }

    private string BuildLogText()
    {
        StringBuilder builder = new StringBuilder();
        int start = Mathf.Max(0, battleLog.Count - 8);
        for (int i = start; i < battleLog.Count; i++)
        {
            builder.AppendLine(battleLog[i]);
        }

        return builder.ToString();
    }

    private string BuildInspectText()
    {
        if (hoveredUnit != null)
        {
            return $"{hoveredUnit.definition.displayName} ({FactionLabel(hoveredUnit.definition.faction)})\n{hoveredUnit.definition.age}세 · {hoveredUnit.definition.mbti} · {hoveredUnit.definition.sectName}\n{hoveredUnit.definition.elementName}/{hoveredUnit.definition.weaponName} · {hoveredUnit.definition.speechTone}\n체력 {hoveredUnit.hp}/{hoveredUnit.definition.maxHp}   방어 {DefenseValue(hoveredUnit, TileAt(hoveredUnit.cell))}\n상태: {UnitStatusText(hoveredUnit)}\n무공: {hoveredUnit.definition.specialName}";
        }

        if (hoveredTile != null)
        {
            BattleTestInteractable prop = GetInteractableAt(hoveredTile.cell);
            StringBuilder builder = new StringBuilder();
            builder.Append(TerrainLabel(hoveredTile.terrain))
                .Append("  (")
                .Append(hoveredTile.cell.x)
                .Append(",")
                .Append(hoveredTile.cell.y)
                .AppendLine(")");
            builder.Append("이동 비용 ")
                .Append(hoveredTile.moveCost)
                .Append("   엄폐 +")
                .AppendLine(hoveredTile.coverBonus.ToString());
            builder.Append("고저 ")
                .Append(hoveredTile.elevation)
                .Append("   진입 ")
                .AppendLine(YesNo(hoveredTile.walkable));
            builder.Append("시야 ")
                .Append(hoveredTile.blocksLineOfSight ? "차단" : "개방")
                .Append("   병목 ")
                .AppendLine(YesNo(hoveredTile.isChokePoint));
            if (hoveredTile.elevation > 0)
            {
                builder.Append("고지 효과: 위에서 공격 시 명중 +2");
                if (hoveredTile.elevation >= 2)
                {
                    builder.Append(" / 원거리 유리");
                }

                builder.AppendLine();
            }

            if (prop != null)
            {
                int distance = activeUnit == null ? -1 : GridDistance(activeUnit.cell, prop.cell);
                bool usable = activeUnit != null && !activeUnit.acted && distance <= 1;
                builder.Append("지형지물: ").AppendLine(prop.displayName);
                builder.Append("효과: ")
                    .Append(InteractableEffectText(prop.kind))
                    .Append("   사용: ")
                    .Append(YesNo(usable));
            }
            else
            {
                builder.Append("위험: ").Append(TileHazardText(hoveredTile));
            }

            if (!string.IsNullOrEmpty(hoveredTile.tacticalNote))
            {
                builder.AppendLine().Append("전술: ").Append(hoveredTile.tacticalNote);
            }

            return builder.ToString();
        }

        return "가리킨 대상 없음";
    }

    private string BuildHudForecastText()
    {
        BattleForecast forecast = BuildForecast(activeUnit, hoveredUnit);
        if (!forecast.valid)
        {
            if (string.IsNullOrEmpty(forecast.actorName))
            {
                return forecast.invalidReason;
            }

            return $"[{forecast.actorName}: {forecast.commandName}]\n대상: {forecast.targetName}\n불가: {forecast.invalidReason}\n거리 {forecast.distance} / 사거리 {forecast.range}\n{forecast.costText}";
        }

        return $"[{forecast.actorName}: {forecast.commandName}]\n대상: {forecast.targetName}\n명중 {forecast.neededRollText}\n{forecast.damageText}\n{forecast.hpAfterText}\n{forecast.counterText}\n{forecast.followUpText}\n{forecast.costText}";
    }

    private string BuildRosterText()
    {
        StringBuilder builder = new StringBuilder();
        for (int i = 0; i < units.Count; i++)
        {
            BattleTestUnit unit = units[i];
            if (i > 0)
            {
                builder.Append("   |   ");
            }

            if (unit == activeUnit)
            {
                builder.Append("> ");
            }

            builder.Append(unit.definition.displayName)
                .Append(" 체력 ")
                .Append(unit.hp)
                .Append("/")
                .Append(unit.definition.maxHp)
                .Append(" ")
                .Append(UnitStatusText(unit));
        }

        return builder.ToString();
    }

    private static void SetText(Text text, string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }

    private static void SetButtonEnabled(Button button, bool enabled)
    {
        if (button != null)
        {
            button.interactable = enabled;
        }
    }

    private static void RefreshCommandButtonState(Button button, bool selected)
    {
        if (button == null)
        {
            return;
        }

        Image image = button.GetComponent<Image>();
        if (image != null && button.interactable)
        {
            image.color = selected ? new Color(0.56f, 0.39f, 0.18f, 1f) : new Color(0.18f, 0.16f, 0.13f, 0.96f);
        }
    }

    private void BuildBattle()
    {
        StopAllCoroutines();
        ClearGeneratedObjects();
        width = Mathf.Max(width, 16);
        height = Mathf.Max(height, 12);

        units.Clear();
        battleLog.Clear();
        interactables.Clear();
        activeUnit = null;
        round = 1;
        busy = false;
        aiQueued = false;
        battleOver = false;
        commandMode = BattleCommandMode.Move;
        scoutMode = true;
        showThreatOverlay = true;
        showElevationOverlay = true;
        showCoverOverlay = true;
        showSightOverlay = true;
        showObjectiveOverlay = true;
        phaseTurn.Reset();
        hoveredTile = null;
        hoveredUnit = null;

        EnsureMapVisualSprites();
        CreateTerrain();
        SpawnUnits();
        units.Sort((left, right) => right.initiative.CompareTo(left.initiative));
        CenterCamera();
        EnsureBattleHud();
        AddLog("[Scout] Scout mode: inspect enemies, hazards, terrain, and move allies onto southern deployment cells.");

        AddLog("[체계] 전투 준비 완료.");
        BeginPlayerPhase();
        if (tilemapBattlefield != null)
        {
            mapIntroCoroutine = StartCoroutine(PlayMapIntro());
        }
    }

    private void ClearGeneratedObjects()
    {
        List<Transform> children = new List<Transform>();
        for (int i = 0; i < transform.childCount; i++)
        {
            children.Add(transform.GetChild(i));
        }

        foreach (Transform child in children)
        {
            Destroy(child.gameObject);
        }

        tilemapBattlefield = null;
        mapIntroCoroutine = null;
        ClearCanvasHudReferences();
    }

    private void ClearCanvasHudReferences()
    {
        hudCanvas = null;
        hudPhaseText = null;
        hudActiveText = null;
        hudResourceText = null;
        hudObjectiveText = null;
        hudPhaseListText = null;
        hudLogText = null;
        hudInspectText = null;
        hudForecastText = null;
        hudRosterText = null;
        hudMoveButton = null;
        hudAttackButton = null;
        hudSkillButton = null;
        hudGuardButton = null;
        hudInteractButton = null;
        hudPhaseEndButton = null;
        hudResetButton = null;
        hudCommandButtons.Clear();
        battleHud = null;
        hudNotice = string.Empty;
        hudNoticeUntil = 0f;
    }

    private void EnsureBattleHud()
    {
        if (!useCanvasHud)
        {
            return;
        }

        if (battleHud != null)
        {
            return;
        }

        GameObject hudObject = new GameObject("Battle HUD");
        hudObject.transform.SetParent(transform, false);
        battleHud = hudObject.AddComponent<BattleHUDController>();
        battleHud.Initialize(this);
    }

    private void RefreshBattleHud()
    {
        if (!useCanvasHud)
        {
            return;
        }

        EnsureBattleHud();
        if (battleHud != null)
        {
            battleHud.Refresh(CreateHudSnapshot());
        }
    }

    private BattleHudSnapshot CreateHudSnapshot()
    {
        BattleHudSnapshot snapshot = new BattleHudSnapshot
        {
            phase = phaseTurn.CurrentPhase,
            round = round,
            battleOver = battleOver,
            scoutMode = scoutMode,
            instruction = BuildHudInstructionText(),
            objectiveText = BuildHudObjectiveText(),
            unitInfoText = BuildHudUnitInfo(),
            hoverTitle = BuildHudHoverTitle(),
            hoverBody = BuildHudHoverBody(),
            showLog = showHudLog,
            showThreatRange = showThreatOverlay,
            showElevationOverlay = showElevationOverlay,
            showCoverOverlay = showCoverOverlay,
            showSightOverlay = showSightOverlay,
            showObjectiveOverlay = showObjectiveOverlay,
            commandMode = commandMode,
            activeUnit = activeUnit,
            noticeText = Time.time < hudNoticeUntil ? hudNotice : string.Empty
        };

        bool playerTurn = phaseTurn.IsPlayerPhase && !scoutMode && activeUnit != null &&
                          activeUnit.definition.faction == Faction.Ally && !busy && !battleOver;
        snapshot.canMove = playerTurn && activeUnit.CanMove;
        snapshot.canAttack = playerTurn && activeUnit.CanUseMainAction;
        snapshot.canSkill = playerTurn && CanUseSpecial(activeUnit);
        snapshot.canGuard = playerTurn && activeUnit.CanUseMainAction;
        snapshot.canTerrain = playerTurn && activeUnit.CanUseMainAction && HasUsableInteractable(activeUnit);
        snapshot.canWait = phaseTurn.IsPlayerPhase && !busy && !battleOver;

        BuildHudForecast(snapshot);

        foreach (BattleTestUnit unit in units)
        {
            if (unit.definition.faction != Faction.Ally)
            {
                continue;
            }

            snapshot.allies.Add(unit);
            snapshot.unitStatuses[unit] = UnitStatusText(unit);
            if (!busy && !battleOver && phaseTurn.CanPlayerControl(unit))
            {
                snapshot.selectableUnits.Add(unit);
            }
        }

        snapshot.logs.AddRange(battleLog);
        return snapshot;
    }

    private string BuildHudObjectiveText()
    {
        if (scoutMode)
        {
            return $"{MapDisplayName}\nSCOUT: enemy positions, danger tiles, terrain info, LoS blockers, props.\nSelect ally -> click southern deployment tile to reposition. S/Space starts battle.";
        }

        return $"{MapDisplayName}\n주 목표: 관문 정찰조장 제압\n보조: 협로 엄폐, 고저차, 지형 상호작용 활용\n단축: S 정찰 / Tab 위협 / H 고저 / C 엄폐 / V 시야 / O 목표";
    }

    private string BuildHudInstructionText()
    {
        if (battleOver)
        {
            return "R 키 또는 전투 재시작 버튼으로 다시 시작합니다.";
        }

        if (phaseTurn.IsEnemyPhase)
        {
            return "적군이 행동 중입니다. 위험 범위와 반격 결과를 확인하세요.";
        }

        if (activeUnit == null)
        {
            return "행동 가능한 아군을 지도나 하단 로스터에서 선택하세요.";
        }

        switch (commandMode)
        {
        case BattleCommandMode.Attack:
            return "붉은 대상은 공격 범위입니다. 적에게 마우스를 올리면 전투 예측이 표시됩니다.";
        case BattleCommandMode.Skill:
            return "무공 범위를 확인한 뒤 대상을 클릭하세요. 내공과 재사용 대기시간을 확인하세요.";
        case BattleCommandMode.Interact:
            return "금색 지형 오브젝트를 활용하세요. 향로, 등불, 밧줄, 바위를 전술에 이용할 수 있습니다.";
        default:
            return "파란 칸은 이동 가능 범위입니다. 이동 후 공격, 무공, 방어, 지형 활용으로 행동을 마무리하세요.";
        }
    }

    private string BuildHudUnitInfo()
    {
        if (activeUnit == null)
        {
            return "선택 유닛: 없음\n아군을 선택하세요.";
        }

        return "선택: " + activeUnit.definition.displayName +
               "\n문파: " + activeUnit.definition.sectName +
               "\nHP " + activeUnit.hp + "/" + activeUnit.definition.maxHp +
               "   내공 " + activeUnit.inner + "/" + activeUnit.definition.maxInner +
               "\n이동 " + activeUnit.actions.movementLeft + "/" + EffectiveMoveRange(activeUnit) +
               "   민첩 " + AgilityValue(activeUnit) +
               "\n명령: " + CommandLabel(commandMode) +
               "   상태: " + UnitStatusText(activeUnit);
    }

    private string BuildHudHoverTitle()
    {
        if (hoveredUnit != null)
        {
            return hoveredUnit.definition.displayName + " / " + FactionLabel(hoveredUnit.definition.faction);
        }

        if (hoveredTile != null)
        {
            return TerrainLabel(hoveredTile.terrain) + "  (" + hoveredTile.cell.x + "," + hoveredTile.cell.y + ")";
        }

        return "전술 정보";
    }

    private string BuildHudHoverBody()
    {
        if (hoveredUnit != null)
        {
            return "HP " + hoveredUnit.hp + "/" + hoveredUnit.definition.maxHp +
                   "   방어 " + DefenseValue(hoveredUnit, TileAt(hoveredUnit.cell)) +
                   "\n" + hoveredUnit.definition.elementName + "/" + hoveredUnit.definition.weaponName +
                   "   " + hoveredUnit.definition.mbti +
                   "\n무공: " + hoveredUnit.definition.specialName +
                   "\n상태: " + UnitStatusText(hoveredUnit);
        }

        if (hoveredTile != null)
        {
            BattleTestInteractable prop = GetInteractableAt(hoveredTile.cell);
            string propLine = prop == null
                                  ? "위험: " + TileHazardText(hoveredTile)
                                  : "지형 오브젝트: " + prop.displayName + " / " + InteractableEffectText(prop.kind);
            return "이동 비용 " + hoveredTile.moveCost +
                   "   엄폐 +" + hoveredTile.coverBonus +
                   "\n고저차 " + hoveredTile.elevation +
                   "   시야 " + (hoveredTile.blocksLineOfSight ? "차단" : "개방") +
                   "\n" + propLine +
                   (string.IsNullOrEmpty(hoveredTile.tacticalNote) ? string.Empty : "\n" + hoveredTile.tacticalNote);
        }

        return "유닛이나 지형에 마우스를 올리면 이동 비용, 엄폐, 고저차, 위험도를 보여줍니다.";
    }

    private void BuildHudForecast(BattleHudSnapshot snapshot)
    {
        BattleForecast forecast = BuildForecast(activeUnit, hoveredUnit);
        snapshot.hasForecast = activeUnit != null;
        snapshot.forecastTitle = "전투 예측";

        if (!forecast.valid)
        {
            snapshot.forecastLeft = string.IsNullOrEmpty(forecast.actorName)
                                        ? string.Empty
                                        : forecast.actorName + "\n" + forecast.commandName;
            snapshot.forecastCenter = string.IsNullOrEmpty(forecast.invalidReason)
                                          ? "공격 또는 무공을 선택한 뒤 대상을 가리키세요."
                                          : forecast.invalidReason;
            snapshot.forecastRight = string.IsNullOrEmpty(forecast.targetName)
                                         ? string.Empty
                                         : forecast.targetName + "\n거리 " + forecast.distance + "/" + forecast.range +
                                           "\n" + forecast.costText;
            return;
        }

        snapshot.forecastLeft = forecast.actorName + "\n" + forecast.commandName + "\n" + forecast.damageText +
                                "\n" + forecast.costText;
        snapshot.forecastCenter = "명중 " + forecast.neededRollText +
                                  "\n거리 " + forecast.distance + "/" + forecast.range + ": " + forecast.rangeText +
                                  "\n보정 공격 " + forecast.attackBonus + " / 고저 " + forecast.heightBonus +
                                  " / 지형 " + forecast.terrainBonus +
                                  "\n대상 방어 " + forecast.defense;
        snapshot.forecastRight = forecast.targetName + "\n" + forecast.hpAfterText + "\n" + forecast.counterText +
                                 "\n" + forecast.followUpText;
    }

    public void HudSetCommand(BattleCommandMode mode)
    {
        SetCommandMode(mode);
        ShowHudNotice(CommandLabel(mode));
    }

    public void HudGuard()
    {
        GuardActiveUnit();
    }

    public void HudWait()
    {
        if (scoutMode)
        {
            ExitScoutMode();
            return;
        }

        EndTurn();
    }

    private void ToggleScoutMode()
    {
        if (!phaseTurn.IsPlayerPhase || battleOver || busy)
        {
            return;
        }

        if (scoutMode)
        {
            ExitScoutMode();
            return;
        }

        scoutMode = true;
        showThreatOverlay = true;
        showElevationOverlay = true;
        showCoverOverlay = true;
        showSightOverlay = true;
        commandMode = BattleCommandMode.Move;
        AddLog("[Scout] 정찰 모드 재개.");
        RefreshHighlights();
    }

    private void ExitScoutMode()
    {
        if (!scoutMode)
        {
            return;
        }

        scoutMode = false;
        commandMode = BattleCommandMode.Move;
        AddLog("[Scout] 정찰 종료. 아군 행동을 시작합니다.");
        RefreshHighlights();
        RefreshUnits();
    }

    public void HudToggleThreat()
    {
        showThreatOverlay = !showThreatOverlay;
        ShowHudNotice(showThreatOverlay ? "위협 범위 표시" : "위협 범위 숨김");
        RefreshHighlights();
    }

    public void HudToggleCover()
    {
        showCoverOverlay = !showCoverOverlay;
        ShowHudNotice(showCoverOverlay ? "엄폐 표시" : "엄폐 숨김");
        RefreshHighlights();
    }

    public void HudToggleLog()
    {
        showHudLog = !showHudLog;
    }

    public void HudResetBattle()
    {
        BuildBattle();
    }

    public void HudSelectUnit(BattleTestUnit unit)
    {
        SelectPlayerUnit(unit);
    }

    private void ShowHudNotice(string message)
    {
        hudNotice = message;
        hudNoticeUntil = Time.time + 1.2f;
    }

    private void CreateTerrain()
    {
        if (useAuthoredSceneMap && TryCreateAuthoredSceneTerrain())
        {
            return;
        }

        if (useTilemapBattlefield && !useLegacyDiamondTerrain)
        {
            CreateTilemapTerrain();
            return;
        }

        CreateLegacyDebugTerrain();
    }

    private bool TryCreateAuthoredSceneTerrain()
    {
        BattleMapSceneController mapController = FindAnyObjectByType<BattleMapSceneController>();
        if (mapController == null || !mapController.AuthoredProductionMap)
        {
            return false;
        }

        mapController.InitializeRuntime();
        BattleMapTilemapBinder binder = mapController.Binder;
        if (binder == null || binder.TacticalOverlay == null || binder.TacticalOverlay.Cells.Count == 0)
        {
            return false;
        }

        tileWidth = mapController.TileWidth;
        tileHeight = mapController.TileHeight;
        width = Mathf.Max(16, mapController.Size.x);
        height = Mathf.Max(12, mapController.Size.y);
        tiles = new BattleTestTile[width, height];

        BattleTilemapBattlefield authoredBattlefield = binder.GetComponent<BattleTilemapBattlefield>();
        if (authoredBattlefield == null)
        {
            authoredBattlefield = binder.gameObject.AddComponent<BattleTilemapBattlefield>();
        }

        authoredBattlefield.BindAuthored(binder, tileWidth, tileHeight, diamondSprite, softDiamondSprite, detailSprite,
                                         dotSprite);
        tilemapBattlefield = authoredBattlefield;

        Transform terrainRoot = new GameObject("Battlefield_Authored_TacticalOverlay").transform;
        terrainRoot.SetParent(transform, false);

        foreach (TacticalGridCellData cellData in binder.TacticalOverlay.Cells)
        {
            if (cellData == null || !IsInside(cellData.cell))
            {
                continue;
            }

            TerrainProfile profile = TerrainProfileFromAuthoredCell(cellData);
            CreateAuthoredTacticalCellCollider(terrainRoot, cellData.cell, profile, mapController.CellToWorld(cellData.cell),
                                               cellData);
        }

        RegisterAuthoredInteractables(mapController);
        return true;
    }

    private TerrainProfile TerrainProfileFromAuthoredCell(TacticalGridCellData data)
    {
        bool objective = string.Equals(data.zoneId, "objective", StringComparison.OrdinalIgnoreCase);
        bool danger = data.hazardType != HazardType.None || data.northEdge == EdgeType.CliffDrop ||
                      data.eastEdge == EdgeType.CliffDrop || data.southEdge == EdgeType.CliffDrop ||
                      data.westEdge == EdgeType.CliffDrop;
        return new TerrainProfile(data.terrainType, Color.white, data.elevation, CoverBonusFromCoverType(data.coverType),
                                  Mathf.Max(1, data.moveCost), data.walkable && !data.blocksMovement,
                                  data.blocksLineOfSight, data.isChokePoint, objective, danger,
                                  string.IsNullOrEmpty(data.laneId) ? "authored" : data.laneId,
                                  string.IsNullOrEmpty(data.decorSetKey) ? data.displayName : data.decorSetKey);
    }

    private void CreateAuthoredTacticalCellCollider(Transform parent, Vector2Int cell, TerrainProfile profile,
                                                    Vector3 worldPosition, TacticalGridCellData cellData)
    {
        GameObject tileObject = new GameObject($"AuthoredTacticalCell_{cell.x}_{cell.y}_{profile.terrain}");
        tileObject.transform.SetParent(parent, false);
        tileObject.transform.position = worldPosition;
        tileObject.transform.localScale = new Vector3(tileWidth, tileWidth, 1f);

        PolygonCollider2D collider = tileObject.AddComponent<PolygonCollider2D>();
        collider.points = new[] { new Vector2(0f, 0.25f), new Vector2(0.5f, 0f), new Vector2(0f, -0.25f),
                                  new Vector2(-0.5f, 0f) };

        BattleTestTile tile = tileObject.AddComponent<BattleTestTile>();
        tile.cell = cell;
        tile.terrain = profile.terrain;
        tile.elevation = profile.elevation;
        tile.walkable = profile.walkable;
        tile.moveCost = profile.moveCost;
        tile.coverBonus = profile.coverBonus;
        tile.baseCoverBonus = profile.coverBonus;
        tile.baseColor = Color.white;
        tile.blocksLineOfSight = profile.blocksLineOfSight;
        tile.isChokePoint = profile.isChokePoint;
        tile.objective = profile.objective;
        tile.danger = profile.danger;
        tile.hazardType = cellData == null ? HazardType.None : cellData.hazardType;
        tile.northEdge = cellData == null ? EdgeType.None : cellData.northEdge;
        tile.eastEdge = cellData == null ? EdgeType.None : cellData.eastEdge;
        tile.southEdge = cellData == null ? EdgeType.None : cellData.southEdge;
        tile.westEdge = cellData == null ? EdgeType.None : cellData.westEdge;
        tile.laneId = profile.laneId;
        tile.tacticalNote = profile.tacticalNote;
        tile.tilemapBattlefield = tilemapBattlefield;
        tile.nameLabel = CreateTileNameLabel(tileObject.transform, profile);
        tiles[cell.x, cell.y] = tile;
    }

    private void RegisterAuthoredInteractables(BattleMapSceneController mapController)
    {
        foreach (MapPropView prop in mapController.InteractiveProps())
        {
            if (prop == null || !IsInside(prop.cell))
            {
                continue;
            }

            BattleTestInteractableKind kind = BattleTestKindFromInteractableKind(prop.kind);
            string id = string.IsNullOrEmpty(prop.propId) ? prop.name : prop.propId;
            string displayName = string.IsNullOrEmpty(prop.displayName) ? prop.name : prop.displayName;
            BattleTestInteractable interactable = new BattleTestInteractable(id, displayName, kind, prop.cell)
            {
                renderer = FindPrimaryPropRenderer(prop.transform)
            };
            interactables.Add(interactable);
        }
    }

    private void CreateTilemapTerrain()
    {
        tiles = new BattleTestTile[width, height];
        Transform terrainRoot = new GameObject("Battlefield_Tilemap").transform;
        terrainRoot.SetParent(transform, false);
        tilemapBattlefield = terrainRoot.gameObject.AddComponent<BattleTilemapBattlefield>();
        tilemapBattlefield.Initialize(width, height, tileWidth, tileHeight, diamondSprite, softDiamondSprite,
                                      detailSprite, dotSprite);

        CreateMapBackdrop(terrainRoot);
        CreateMapAtmosphere(terrainRoot);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                TerrainProfile profile = ResolveTerrain(x, y);
                Vector2Int cell = new Vector2Int(x, y);
                Vector3 worldPosition = GridToWorld(cell);
                tilemapBattlefield.SetTerrainCell(cell, profile.terrain, GetTerrainSprite(profile.terrain),
                                                  VaryTerrainColor(profile.color, cell), profile.moveCost,
                                                  profile.walkable, profile.blocksLineOfSight, profile.isChokePoint,
                                                  profile.elevation, profile.coverBonus, profile.objective,
                                                  profile.danger, profile.laneId, profile.tacticalNote,
                                                  worldPosition);
                CreateTacticalCellCollider(cell, profile, worldPosition);
            }
        }

        CreateInteractables(terrainRoot);
    }

    private void CreateTacticalCellCollider(Vector2Int cell, TerrainProfile profile, Vector3 worldPosition)
    {
        Transform parent = tilemapBattlefield == null || tilemapBattlefield.CellColliderRoot == null
                               ? transform
                               : tilemapBattlefield.CellColliderRoot;
        GameObject tileObject = new GameObject($"TacticalCell_{cell.x}_{cell.y}_{profile.terrain}");
        tileObject.transform.SetParent(parent, false);
        tileObject.transform.position = worldPosition;
        tileObject.transform.localScale = new Vector3(tileWidth, tileWidth, 1f);

        PolygonCollider2D collider = tileObject.AddComponent<PolygonCollider2D>();
        collider.points = new[] { new Vector2(0f, 0.25f), new Vector2(0.5f, 0f), new Vector2(0f, -0.25f),
                                  new Vector2(-0.5f, 0f) };

        BattleTestTile tile = tileObject.AddComponent<BattleTestTile>();
        tile.cell = cell;
        tile.terrain = profile.terrain;
        tile.elevation = profile.elevation;
        tile.walkable = profile.walkable;
        tile.moveCost = profile.moveCost;
        tile.coverBonus = profile.coverBonus;
        tile.baseCoverBonus = profile.coverBonus;
        tile.baseColor = VaryTerrainColor(profile.color, cell);
        tile.blocksLineOfSight = profile.blocksLineOfSight;
        tile.isChokePoint = profile.isChokePoint;
        tile.objective = profile.objective;
        tile.danger = profile.danger;
        tile.hazardType = HazardTypeForProfile(profile);
        tile.laneId = profile.laneId;
        tile.tacticalNote = profile.tacticalNote;
        tile.tilemapBattlefield = tilemapBattlefield;
        tile.nameLabel = CreateTileNameLabel(tileObject.transform, profile);
        tiles[cell.x, cell.y] = tile;
    }

    private void CreateLegacyDebugTerrain()
    {
        tiles = new BattleTestTile[width, height];
        Transform terrainRoot = new GameObject("Terrain").transform;
        terrainRoot.SetParent(transform, false);
        CreateMapBackdrop(terrainRoot);
        CreateMapAtmosphere(terrainRoot);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                TerrainProfile profile = ResolveTerrain(x, y);
                Vector2Int cell = new Vector2Int(x, y);
                GameObject tileObject = new GameObject($"Tile_{x}_{y}_{profile.terrain}");
                tileObject.transform.SetParent(terrainRoot, false);
                tileObject.transform.position = GridToWorld(cell);
                tileObject.transform.localScale = new Vector3(tileWidth, tileWidth, 1f);

                SpriteRenderer renderer = tileObject.AddComponent<SpriteRenderer>();
                Sprite terrainSprite = GetTerrainSprite(profile.terrain);
                renderer.sprite = terrainSprite;
                renderer.color = terrainSprite == diamondSprite ? VaryTerrainColor(profile.color, cell)
                                                                : VaryTerrainColor(Color.white, cell);
                renderer.sortingOrder = (x + y) * 8;

                PolygonCollider2D collider = tileObject.AddComponent<PolygonCollider2D>();
                collider.points = new[] { new Vector2(0f, 0.25f), new Vector2(0.5f, 0f), new Vector2(0f, -0.25f),
                                          new Vector2(-0.5f, 0f) };

                CreateTileShadow(tileObject.transform, profile, cell, renderer.sortingOrder);
                CreateHeightSkirt(tileObject.transform, profile, renderer.sortingOrder);
                CreateTerrainDetails(tileObject.transform, profile, cell, renderer.sortingOrder);

                GameObject highlight = new GameObject("Highlight");
                highlight.transform.SetParent(tileObject.transform, false);
                highlight.transform.localPosition = new Vector3(0f, 0f, -0.02f);
                SpriteRenderer highlightRenderer = highlight.AddComponent<SpriteRenderer>();
                highlightRenderer.sprite = diamondSprite;
                highlightRenderer.color = Color.clear;
                highlightRenderer.sortingOrder = renderer.sortingOrder + 7;

                BattleTestTile tile = tileObject.AddComponent<BattleTestTile>();
                tile.cell = cell;
                tile.terrain = profile.terrain;
                tile.elevation = profile.elevation;
                tile.walkable = profile.walkable;
                tile.moveCost = profile.moveCost;
                tile.coverBonus = profile.coverBonus;
                tile.baseCoverBonus = profile.coverBonus;
                tile.baseColor = renderer.color;
                tile.blocksLineOfSight = profile.blocksLineOfSight;
                tile.isChokePoint = profile.isChokePoint;
                tile.objective = profile.objective;
                tile.danger = profile.danger;
                tile.hazardType = HazardTypeForProfile(profile);
                tile.laneId = profile.laneId;
                tile.tacticalNote = profile.tacticalNote;
                tile.terrainRenderer = renderer;
                tile.highlightRenderer = highlightRenderer;
                tile.nameLabel = CreateTileNameLabel(tileObject.transform, profile);
                tiles[x, y] = tile;
            }
        }

        CreateInteractables(terrainRoot);
    }

    private void CreateInteractables(Transform terrainRoot)
    {
        Transform propParent = tilemapBattlefield == null || tilemapBattlefield.Binder == null ||
                               tilemapBattlefield.Binder.PropsRoot == null
                                   ? terrainRoot
                                   : tilemapBattlefield.Binder.PropsRoot;
        Transform propRoot = new GameObject("Interactables").transform;
        propRoot.SetParent(propParent, false);

        AddInteractable(propRoot, "signboard", "백두천광 현판", BattleTestInteractableKind.Objective,
                        new Vector2Int(7, 10), new Color(0.92f, 0.76f, 0.34f, 1f));
        AddInteractable(propRoot, "incense", "제단 향로", BattleTestInteractableKind.Smoke, new Vector2Int(7, 9),
                        new Color(0.74f, 0.68f, 0.58f, 1f));
        AddInteractable(propRoot, "lantern", "붉은 등불", BattleTestInteractableKind.Fire, new Vector2Int(6, 2),
                        new Color(1f, 0.32f, 0.18f, 1f));
        AddInteractable(propRoot, "oil_jar", "기름항아리", BattleTestInteractableKind.Fire, new Vector2Int(5, 2),
                        new Color(0.78f, 0.43f, 0.18f, 1f));
        AddInteractable(propRoot, "wine_cart", "술수레", BattleTestInteractableKind.Cover, new Vector2Int(4, 3),
                        new Color(0.64f, 0.38f, 0.18f, 1f));
        AddInteractable(propRoot, "fallen_wall", "무너진 담장", BattleTestInteractableKind.Cover, new Vector2Int(9, 7),
                        new Color(0.54f, 0.49f, 0.42f, 1f));
        AddInteractable(propRoot, "bridge_rope", "낡은 다리 밧줄", BattleTestInteractableKind.CollapseBridge,
                        new Vector2Int(12, 5), new Color(0.45f, 0.28f, 0.12f, 1f));
        AddInteractable(propRoot, "bamboo_bundle", "대나무 묶음", BattleTestInteractableKind.BambooFall,
                        new Vector2Int(2, 7), new Color(0.18f, 0.58f, 0.28f, 1f));
        AddInteractable(propRoot, "stone_lantern", "석등", BattleTestInteractableKind.Rockfall,
                        new Vector2Int(10, 9), new Color(0.62f, 0.57f, 0.49f, 1f));
        AddInteractable(propRoot, "snow_pine", "눈덮인 소나무", BattleTestInteractableKind.BambooFall,
                        new Vector2Int(2, 9), new Color(0.32f, 0.55f, 0.38f, 1f));
        AddInteractable(propRoot, "frozen_boulder", "눈덮인 바위", BattleTestInteractableKind.Rockfall,
                        new Vector2Int(11, 8), new Color(0.68f, 0.70f, 0.66f, 1f));
    }

    private void AddInteractable(Transform parent, string id, string displayName, BattleTestInteractableKind kind,
                                 Vector2Int cell, Color color)
    {
        if (!IsInside(cell))
        {
            return;
        }

        BattleTestInteractable interactable = new BattleTestInteractable(id, displayName, kind, cell);
        interactables.Add(interactable);

        GameObject propObject = new GameObject(displayName);
        propObject.transform.SetParent(parent, false);
        propObject.transform.position = GridToWorld(cell) + new Vector3(0f, 0.13f, -0.04f);

        SpriteRenderer renderer = propObject.AddComponent<SpriteRenderer>();
        Sprite propSprite = GetInteractableSprite(id, kind);
        bool hasPropAsset = propSprite != null;
        float propScale = hasPropAsset ? tileWidth * 0.52f : tileWidth * 0.34f;
        propObject.transform.localScale = new Vector3(propScale, propScale, 1f);
        renderer.sprite = hasPropAsset
                              ? propSprite
                              : kind == BattleTestInteractableKind.Cover ||
                                kind == BattleTestInteractableKind.CollapseBridge
                                  ? diamondSprite
                                  : dotSprite;
        renderer.color = hasPropAsset ? Color.white : color;
        renderer.sortingOrder = 2200 + ((cell.x + cell.y) * 2);
        interactable.renderer = renderer;
        if (tilemapBattlefield != null)
        {
            tilemapBattlefield.RegisterPropRenderer(renderer, cell, kind);
        }

        AttachMapPropComponents(propObject, id, displayName, kind, cell);
        CreateInteractableAccent(propObject.transform, kind, renderer.sortingOrder + 1);

        TextMesh label = CreateWorldLabel(propObject.transform, InteractableGlyph(kind), 46, new Vector3(0f, 0.38f, -0.05f),
                                          new Color(1f, 0.92f, 0.66f, 1f), 2300 + ((cell.x + cell.y) * 2));
        interactable.label = label;
    }

    private void AttachMapPropComponents(GameObject propObject, string id, string displayName,
                                         BattleTestInteractableKind kind, Vector2Int cell)
    {
        MapPropView view = GetOrAdd<MapPropView>(propObject);
        view.Configure(id, displayName, cell, ResolvePropKind(id, kind), true);

        InteractableProp interactableProp = GetOrAdd<InteractableProp>(propObject);
        interactableProp.Configure(ActionSlot.Main, ResolvePropCheckStat(kind), ResolvePropDc(kind),
                                   ResolvePropRadius(kind), ResolvePropEffect(kind), true);

        switch (kind)
        {
        case BattleTestInteractableKind.Smoke:
            GetOrAdd<LineOfSightBlocker>(propObject).Configure(1, 1);
            GetOrAdd<MapLightAnchor>(propObject).Configure(new Color(0.70f, 0.78f, 0.72f, 1f), 1.20f, 0.32f);
            break;
        case BattleTestInteractableKind.Fire:
            GetOrAdd<DestructibleProp>(propObject).Configure(8, TerrainType.Fire, HazardType.Fire,
                                                              InteractableEffectType.CreateFire);
            GetOrAdd<MapLightAnchor>(propObject).Configure(new Color(1f, 0.46f, 0.18f, 1f), 1.45f, 0.92f);
            break;
        case BattleTestInteractableKind.Cover:
            GetOrAdd<CoverProvider>(propObject).Configure(id == "fallen_wall" ? CoverType.Full : CoverType.Heavy,
                                                          id == "fallen_wall" ? 4 : 2);
            if (id == "fallen_wall")
            {
                GetOrAdd<LineOfSightBlocker>(propObject).Configure(1);
            }
            break;
        case BattleTestInteractableKind.Objective:
            GetOrAdd<MapLightAnchor>(propObject).Configure(new Color(1f, 0.74f, 0.34f, 1f), 1.70f, 0.62f);
            break;
        case BattleTestInteractableKind.CollapseBridge:
            GetOrAdd<DestructibleProp>(propObject).Configure(10, TerrainType.Rubble, HazardType.Collapse,
                                                              InteractableEffectType.CollapseBridge);
            break;
        case BattleTestInteractableKind.BambooFall:
            GetOrAdd<CoverProvider>(propObject).Configure(CoverType.Heavy, 2);
            GetOrAdd<LineOfSightBlocker>(propObject).Configure(2, 1);
            GetOrAdd<DestructibleProp>(propObject).Configure(9, TerrainType.Rubble, HazardType.None,
                                                              InteractableEffectType.BlockSight);
            break;
        case BattleTestInteractableKind.Rockfall:
            GetOrAdd<CoverProvider>(propObject).Configure(CoverType.Full, 4);
            GetOrAdd<LineOfSightBlocker>(propObject).Configure(2);
            GetOrAdd<DestructibleProp>(propObject).Configure(14, TerrainType.Rubble, HazardType.Fall,
                                                              InteractableEffectType.Push);
            break;
        }
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component == null ? target.AddComponent<T>() : component;
    }

    private static SpriteRenderer FindPrimaryPropRenderer(Transform prop)
    {
        if (prop == null)
        {
            return null;
        }

        SpriteRenderer direct = prop.GetComponent<SpriteRenderer>();
        if (direct != null)
        {
            return direct;
        }

        SpriteRenderer[] renderers = prop.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer != null && renderer.gameObject.name != "Grounding Shadow")
            {
                return renderer;
            }
        }

        return renderers.Length == 0 ? null : renderers[0];
    }

    private static int CoverBonusFromCoverType(CoverType coverType)
    {
        switch (coverType)
        {
        case CoverType.Full:
            return 4;
        case CoverType.Heavy:
            return 2;
        case CoverType.Light:
            return 1;
        default:
            return 0;
        }
    }

    private static HazardType HazardTypeForProfile(TerrainProfile profile)
    {
        switch (profile.terrain)
        {
        case TerrainType.Fire:
            return HazardType.Fire;
        case TerrainType.Smoke:
            return HazardType.Smoke;
        case TerrainType.Ice:
            return HazardType.Ice;
        case TerrainType.ShallowWater:
            return HazardType.Slippery;
        case TerrainType.Water:
        case TerrainType.DeepWater:
            return HazardType.DeepWater;
        case TerrainType.Cliff:
            return profile.danger ? HazardType.Fall : HazardType.None;
        default:
            return profile.danger && !profile.walkable ? HazardType.Fall : HazardType.None;
        }
    }

    private static BattleTestInteractableKind BattleTestKindFromInteractableKind(InteractableKind kind)
    {
        switch (kind)
        {
        case InteractableKind.IncenseBurner:
            return BattleTestInteractableKind.Smoke;
        case InteractableKind.Lantern:
        case InteractableKind.OilJar:
        case InteractableKind.Beacon:
            return BattleTestInteractableKind.Fire;
        case InteractableKind.WoodenBridge:
            return BattleTestInteractableKind.CollapseBridge;
        case InteractableKind.BambooBundle:
            return BattleTestInteractableKind.BambooFall;
        case InteractableKind.RockLantern:
            return BattleTestInteractableKind.Rockfall;
        case InteractableKind.SectSignboard:
        case InteractableKind.Gate:
            return BattleTestInteractableKind.Objective;
        default:
            return BattleTestInteractableKind.Cover;
        }
    }

    private static InteractableKind ResolvePropKind(string id, BattleTestInteractableKind kind)
    {
        switch (kind)
        {
        case BattleTestInteractableKind.Smoke:
            return InteractableKind.IncenseBurner;
        case BattleTestInteractableKind.Fire:
            return id == "oil_jar" ? InteractableKind.OilJar : InteractableKind.Lantern;
        case BattleTestInteractableKind.Cover:
            return id == "fallen_wall" ? InteractableKind.FallenWall : InteractableKind.WineCart;
        case BattleTestInteractableKind.Objective:
            return InteractableKind.SectSignboard;
        case BattleTestInteractableKind.CollapseBridge:
            return InteractableKind.WoodenBridge;
        case BattleTestInteractableKind.BambooFall:
            return InteractableKind.BambooBundle;
        case BattleTestInteractableKind.Rockfall:
            return InteractableKind.RockLantern;
        default:
            return InteractableKind.WineCart;
        }
    }

    private static StatType ResolvePropCheckStat(BattleTestInteractableKind kind)
    {
        switch (kind)
        {
        case BattleTestInteractableKind.Smoke:
        case BattleTestInteractableKind.Fire:
            return StatType.InnerPower;
        case BattleTestInteractableKind.BambooFall:
        case BattleTestInteractableKind.Rockfall:
        case BattleTestInteractableKind.CollapseBridge:
            return StatType.Strength;
        default:
            return StatType.Agility;
        }
    }

    private static int ResolvePropDc(BattleTestInteractableKind kind)
    {
        switch (kind)
        {
        case BattleTestInteractableKind.Objective:
            return 0;
        case BattleTestInteractableKind.CollapseBridge:
        case BattleTestInteractableKind.Rockfall:
            return 14;
        case BattleTestInteractableKind.BambooFall:
            return 12;
        default:
            return 10;
        }
    }

    private static int ResolvePropRadius(BattleTestInteractableKind kind)
    {
        switch (kind)
        {
        case BattleTestInteractableKind.Smoke:
        case BattleTestInteractableKind.Fire:
        case BattleTestInteractableKind.Rockfall:
            return 2;
        default:
            return 1;
        }
    }

    private static InteractableEffectType ResolvePropEffect(BattleTestInteractableKind kind)
    {
        switch (kind)
        {
        case BattleTestInteractableKind.Smoke:
            return InteractableEffectType.CreateSmoke;
        case BattleTestInteractableKind.Fire:
            return InteractableEffectType.CreateFire;
        case BattleTestInteractableKind.CollapseBridge:
            return InteractableEffectType.CollapseBridge;
        case BattleTestInteractableKind.BambooFall:
            return InteractableEffectType.BlockSight;
        case BattleTestInteractableKind.Rockfall:
            return InteractableEffectType.Push;
        default:
            return InteractableEffectType.CreateCover;
        }
    }

    private void CreateInteractableAccent(Transform parent, BattleTestInteractableKind kind, int sortingOrder)
    {
        switch (kind)
        {
        case BattleTestInteractableKind.Objective:
            CreateDetailSprite(parent, "Signboard Beam", detailSprite, new Vector3(0f, 0.10f, -0.05f),
                               new Vector3(0.78f, 0.10f, 1f), 0f, new Color(0.24f, 0.13f, 0.05f, 0.78f),
                               sortingOrder);
            CreateDetailSprite(parent, "Signboard Post", detailSprite, new Vector3(0f, -0.05f, -0.05f),
                               new Vector3(0.12f, 0.48f, 1f), 0f, new Color(0.18f, 0.10f, 0.04f, 0.70f),
                               sortingOrder);
            break;
        case BattleTestInteractableKind.Smoke:
            CreateDetailSprite(parent, "Smoke Wisp A", dotSprite, new Vector3(-0.10f, 0.16f, -0.05f),
                               Vector3.one * 0.34f, 0f, new Color(0.86f, 0.82f, 0.72f, 0.38f), sortingOrder);
            CreateDetailSprite(parent, "Smoke Wisp B", dotSprite, new Vector3(0.12f, 0.24f, -0.05f),
                               Vector3.one * 0.26f, 0f, new Color(0.72f, 0.70f, 0.64f, 0.34f), sortingOrder);
            break;
        case BattleTestInteractableKind.Fire:
            CreateDetailSprite(parent, "Flame Core", detailSprite, new Vector3(0f, 0.14f, -0.05f),
                               new Vector3(0.18f, 0.48f, 1f), -8f, new Color(1f, 0.82f, 0.24f, 0.78f),
                               sortingOrder);
            CreateDetailSprite(parent, "Flame Edge", detailSprite, new Vector3(0.06f, 0.10f, -0.05f),
                               new Vector3(0.14f, 0.36f, 1f), 14f, new Color(1f, 0.20f, 0.08f, 0.58f),
                               sortingOrder + 1);
            break;
        case BattleTestInteractableKind.Cover:
            CreateRockDetails(parent, Vector2Int.zero, sortingOrder);
            break;
        case BattleTestInteractableKind.CollapseBridge:
            CreateDetailSprite(parent, "Bridge Rope", detailSprite, new Vector3(0f, 0.04f, -0.05f),
                               new Vector3(0.86f, 0.035f, 1f), -12f, new Color(0.92f, 0.70f, 0.42f, 0.64f),
                               sortingOrder);
            break;
        case BattleTestInteractableKind.BambooFall:
            CreateDetailSprite(parent, "Bundled Bamboo A", detailSprite, new Vector3(-0.06f, 0.08f, -0.05f),
                               new Vector3(0.04f, 0.62f, 1f), -28f, new Color(0.60f, 0.88f, 0.35f, 0.70f),
                               sortingOrder);
            CreateDetailSprite(parent, "Bundled Bamboo B", detailSprite, new Vector3(0.08f, 0.06f, -0.05f),
                               new Vector3(0.04f, 0.58f, 1f), 24f, new Color(0.18f, 0.42f, 0.18f, 0.74f),
                               sortingOrder);
            break;
        case BattleTestInteractableKind.Rockfall:
            CreateRockDetails(parent, Vector2Int.one, sortingOrder);
            break;
        }
    }

    private void CreateMapBackdrop(Transform terrainRoot)
    {
        GameObject backdrop = new GameObject("Painted Map Backdrop");
        backdrop.transform.SetParent(terrainRoot, false);

        Vector3 min = GridToWorld(Vector2Int.zero);
        Vector3 max = GridToWorld(new Vector2Int(width - 1, height - 1));
        Vector3 left = GridToWorld(new Vector2Int(0, height - 1));
        Vector3 right = GridToWorld(new Vector2Int(width - 1, 0));
        Vector3 center = (min + max + left + right) * 0.25f;
        backdrop.transform.position = center + new Vector3(0f, -0.18f, 0.08f);
        backdrop.transform.localScale = new Vector3(width * tileWidth * 2.40f, height * tileHeight * 2.80f, 1f);
        backdrop.transform.rotation = Quaternion.Euler(0f, 0f, 45f);

        SpriteRenderer renderer = backdrop.AddComponent<SpriteRenderer>();
        renderer.sprite = detailSprite;
        renderer.color = new Color(0.18f, 0.27f, 0.23f, 0.82f);
        renderer.sortingOrder = -80;

        CreateAtmosphereSprite(terrainRoot, "Dawn Mountain Sky Wash", softDiamondSprite,
                               center + new Vector3(0f, 1.35f, 0.10f),
                               new Vector3(width * tileWidth * 2.85f, height * tileHeight * 3.15f, 1f), 45f,
                               new Color(0.36f, 0.50f, 0.50f, 0.34f), -92);
        CreateAtmosphereSprite(terrainRoot, "Far Baekdu Ridge", mountainRidgeSprite,
                               center + new Vector3(-0.80f, 2.55f, 0.04f), new Vector3(3.35f, 1.35f, 1f), 0f,
                               new Color(0.25f, 0.36f, 0.32f, 0.66f), -76);
        CreateAtmosphereSprite(terrainRoot, "Near Pine Ridge", mountainRidgeSprite,
                               center + new Vector3(0.55f, 1.50f, 0.03f), new Vector3(3.80f, 1.05f, 1f), 0f,
                               new Color(0.10f, 0.23f, 0.16f, 0.58f), -72);
        CreateAtmosphereSprite(terrainRoot, "Left Pine Silhouette", pineSilhouetteSprite,
                               left + new Vector3(-1.45f, 0.78f, 0.02f), new Vector3(1.35f, 1.55f, 1f), -4f,
                               new Color(0.07f, 0.18f, 0.12f, 0.54f), -68);
        CreateAtmosphereSprite(terrainRoot, "Right Pine Silhouette", pineSilhouetteSprite,
                               right + new Vector3(1.30f, 0.60f, 0.02f), new Vector3(-1.25f, 1.40f, 1f), 5f,
                               new Color(0.07f, 0.18f, 0.12f, 0.48f), -68);
    }

    private void CreateMapAtmosphere(Transform terrainRoot)
    {
        Transform atmosphereRoot = new GameObject("Painted Atmosphere").transform;
        atmosphereRoot.SetParent(terrainRoot, false);

        CreateZoneWash(atmosphereRoot, "Bamboo Canopy Wash", new Vector2Int(2, 7), new Vector2(3.80f, 2.65f),
                       -18f, new Color(0.06f, 0.22f, 0.10f, 0.16f), 1120, true);
        CreateZoneWash(atmosphereRoot, "Stream Cold Wash", new Vector2Int(12, 5), new Vector2(2.20f, 4.10f),
                       -29f, new Color(0.18f, 0.48f, 0.56f, 0.17f), 1121, true);
        CreateZoneWash(atmosphereRoot, "Shrine Gold Wash", new Vector2Int(7, 9), new Vector2(4.15f, 2.55f),
                       11f, new Color(0.84f, 0.56f, 0.20f, 0.13f), 1122, false);
        CreateZoneWash(atmosphereRoot, "Roof Crimson Wash", new Vector2Int(11, 9), new Vector2(2.90f, 1.75f),
                       18f, new Color(0.66f, 0.10f, 0.08f, 0.12f), 1123, false);

        CreateMistBand(atmosphereRoot, "Low Valley Mist", new Vector2Int(4, 2), new Vector3(-0.18f, 0.06f, -0.03f),
                       new Vector3(4.80f, 0.38f, 1f), -14f, new Color(0.72f, 0.78f, 0.72f, 0.13f), 1240);
        CreateMistBand(atmosphereRoot, "Stream Spray Mist", new Vector2Int(13, 6), new Vector3(0.08f, 0.03f, -0.03f),
                       new Vector3(2.65f, 0.28f, 1f), -34f, new Color(0.56f, 0.78f, 0.82f, 0.16f), 1241);
        CreateMistBand(atmosphereRoot, "Shrine Dust Beam", new Vector2Int(7, 8), new Vector3(0.18f, 0.22f, -0.03f),
                       new Vector3(3.10f, 0.22f, 1f), 12f, new Color(1f, 0.78f, 0.42f, 0.11f), 1242);

        CreateGlow(atmosphereRoot, "Lantern Glow", new Vector2Int(6, 2), new Color(1f, 0.45f, 0.16f, 0.24f),
                   0.95f, 1360);
        CreateGlow(atmosphereRoot, "Oil Jar Heat Glow", new Vector2Int(5, 2), new Color(1f, 0.32f, 0.10f, 0.17f),
                   0.72f, 1359);
        CreateGlow(atmosphereRoot, "Signboard Halo", new Vector2Int(7, 10), new Color(1f, 0.80f, 0.36f, 0.18f),
                   1.18f, 1361);
    }

    private void CreateZoneWash(Transform parent, string name, Vector2Int cell, Vector2 scale, float zRotation,
                                Color color, int sortingOrder, bool animated)
    {
        SpriteRenderer renderer = CreateAtmosphereSprite(parent, name, softDiamondSprite,
                                                         GridToWorld(cell) + new Vector3(0f, 0.04f, -0.03f),
                                                         new Vector3(scale.x, scale.y, 1f), zRotation, color,
                                                         sortingOrder);
        if (animated)
        {
            BattleMapAmbientMotion motion = renderer.gameObject.AddComponent<BattleMapAmbientMotion>();
            motion.drift = new Vector3(0.035f, 0.018f, 0f);
            motion.speed = 0.36f;
            motion.alphaPulse = 0.12f;
            motion.scalePulse = 0.025f;
        }
    }

    private void CreateMistBand(Transform parent, string name, Vector2Int cell, Vector3 offset, Vector3 scale,
                                float zRotation, Color color, int sortingOrder)
    {
        SpriteRenderer renderer = CreateAtmosphereSprite(parent, name, detailSprite, GridToWorld(cell) + offset, scale,
                                                         zRotation, color, sortingOrder);
        BattleMapAmbientMotion motion = renderer.gameObject.AddComponent<BattleMapAmbientMotion>();
        motion.drift = new Vector3(0.10f, 0.025f, 0f);
        motion.speed = 0.24f;
        motion.alphaPulse = 0.22f;
        motion.scalePulse = 0.035f;
    }

    private void CreateGlow(Transform parent, string name, Vector2Int cell, Color color, float scale, int sortingOrder)
    {
        SpriteRenderer renderer = CreateAtmosphereSprite(parent, name, dotSprite,
                                                         GridToWorld(cell) + new Vector3(0f, 0.14f, -0.04f),
                                                         Vector3.one * scale, 0f, color, sortingOrder);
        BattleMapAmbientMotion motion = renderer.gameObject.AddComponent<BattleMapAmbientMotion>();
        motion.drift = new Vector3(0f, 0.018f, 0f);
        motion.speed = 0.62f;
        motion.alphaPulse = 0.30f;
        motion.scalePulse = 0.055f;
    }

    private SpriteRenderer CreateAtmosphereSprite(Transform parent, string name, Sprite sprite, Vector3 worldPosition,
                                                  Vector3 scale, float zRotation, Color color, int sortingOrder)
    {
        GameObject atmosphere = new GameObject(name);
        atmosphere.transform.SetParent(parent, false);
        atmosphere.transform.position = worldPosition;
        atmosphere.transform.localScale = scale;
        atmosphere.transform.localRotation = Quaternion.Euler(0f, 0f, zRotation);

        SpriteRenderer renderer = atmosphere.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }

    private void CreateTileShadow(Transform parent, TerrainProfile profile, Vector2Int cell, int sortingOrder)
    {
        GameObject shadow = new GameObject("Ground Shadow");
        shadow.transform.SetParent(parent, false);
        shadow.transform.localPosition = new Vector3(0.035f, -0.055f - profile.elevation * 0.018f, 0.05f);
        shadow.transform.localScale = new Vector3(1.08f, 1.05f, 1f);

        SpriteRenderer renderer = shadow.AddComponent<SpriteRenderer>();
        renderer.sprite = softDiamondSprite;
        float alpha = profile.walkable ? 0.18f + profile.elevation * 0.055f : 0.34f;
        renderer.color = new Color(0f, 0f, 0f, Mathf.Clamp(alpha, 0.12f, 0.42f));
        renderer.sortingOrder = sortingOrder - 5;
    }

    private void CreateHeightSkirt(Transform parent, TerrainProfile profile, int sortingOrder)
    {
        int layers = Mathf.Clamp(profile.walkable ? profile.elevation : profile.elevation + 1, 0, 3);
        for (int i = 0; i < layers; i++)
        {
            GameObject skirt = new GameObject("Height Side");
            skirt.transform.SetParent(parent, false);
            skirt.transform.localPosition = new Vector3(0f, -0.050f * (i + 1), 0.035f);
            skirt.transform.localScale = new Vector3(0.96f - i * 0.035f, 0.92f, 1f);

            SpriteRenderer renderer = skirt.AddComponent<SpriteRenderer>();
            renderer.sprite = softDiamondSprite;
            renderer.color = Color.Lerp(profile.color, Color.black, profile.walkable ? 0.38f : 0.55f);
            renderer.sortingOrder = sortingOrder - 4 + i;
        }
    }

    private void CreateTerrainDetails(Transform parent, TerrainProfile profile, Vector2Int cell, int sortingOrder)
    {
        switch (profile.terrain)
        {
        case TerrainType.Road:
        case TerrainType.Stone:
        case TerrainType.ShrineFloor:
            CreateStoneDetails(parent, cell, sortingOrder + 2, profile.terrain == TerrainType.ShrineFloor);
            break;
        case TerrainType.Bamboo:
        case TerrainType.Forest:
            CreateBambooDetails(parent, cell, sortingOrder + 2);
            break;
        case TerrainType.ShallowWater:
        case TerrainType.DeepWater:
            CreateWaterDetails(parent, cell, sortingOrder + 2);
            break;
        case TerrainType.Bridge:
        case TerrainType.Wood:
            CreateBridgeDetails(parent, cell, sortingOrder + 2);
            break;
        case TerrainType.Roof:
            CreateRoofDetails(parent, cell, sortingOrder + 2);
            break;
        case TerrainType.Rubble:
        case TerrainType.Wall:
        case TerrainType.Cliff:
            CreateRockDetails(parent, cell, sortingOrder + 2);
            break;
        case TerrainType.Plain:
        case TerrainType.Hill:
            CreateGrassDetails(parent, cell, sortingOrder + 2, profile.elevation);
            break;
        }

        if (profile.isChokePoint)
        {
            CreateDetailSprite(parent, "Choke Edge", detailSprite, new Vector3(0f, -0.03f, -0.045f),
                               new Vector3(0.40f, 0.025f, 1f), -18f, new Color(0.86f, 0.68f, 0.30f, 0.36f),
                               sortingOrder + 3);
        }
    }

    private void CreateStoneDetails(Transform parent, Vector2Int cell, int sortingOrder, bool shrine)
    {
        int count = shrine ? 3 : 2;
        for (int i = 0; i < count; i++)
        {
            Vector3 position = DetailPosition(cell, i, 0.28f, 0.12f);
            float rotation = -24f + Stable01(cell, i + 17) * 48f;
            Color color = shrine ? new Color(0.82f, 0.74f, 0.56f, 0.38f)
                                 : new Color(0.25f, 0.23f, 0.19f, 0.26f);
            CreateDetailSprite(parent, "Stone Grain", detailSprite, position,
                               new Vector3(0.16f + Stable01(cell, i + 2) * 0.12f, 0.018f, 1f), rotation, color,
                               sortingOrder);
        }
    }

    private void CreateBambooDetails(Transform parent, Vector2Int cell, int sortingOrder)
    {
        for (int i = 0; i < 3; i++)
        {
            Vector3 position = DetailPosition(cell, i, 0.30f, 0.14f);
            float rotation = -10f + Stable01(cell, i + 31) * 20f;
            Color color = i % 2 == 0 ? new Color(0.43f, 0.78f, 0.35f, 0.48f)
                                     : new Color(0.10f, 0.27f, 0.14f, 0.54f);
            CreateDetailSprite(parent, "Bamboo Stroke", detailSprite, position,
                               new Vector3(0.028f, 0.34f + Stable01(cell, i + 4) * 0.18f, 1f), rotation, color,
                               sortingOrder);
        }
    }

    private void CreateWaterDetails(Transform parent, Vector2Int cell, int sortingOrder)
    {
        for (int i = 0; i < 2; i++)
        {
            Vector3 position = DetailPosition(cell, i, 0.24f, 0.10f);
            CreateDetailSprite(parent, "Water Sheen", detailSprite, position,
                               new Vector3(0.24f + Stable01(cell, i + 5) * 0.15f, 0.018f, 1f), -12f,
                               new Color(0.55f, 0.88f, 0.92f, 0.38f), sortingOrder);
        }
    }

    private void CreateBridgeDetails(Transform parent, Vector2Int cell, int sortingOrder)
    {
        for (int i = 0; i < 3; i++)
        {
            CreateDetailSprite(parent, "Bridge Plank", detailSprite, new Vector3(-0.16f + i * 0.16f, 0.01f, -0.04f),
                               new Vector3(0.12f, 0.028f, 1f), -25f, new Color(0.22f, 0.13f, 0.07f, 0.52f),
                               sortingOrder);
        }
    }

    private void CreateRoofDetails(Transform parent, Vector2Int cell, int sortingOrder)
    {
        for (int i = 0; i < 3; i++)
        {
            CreateDetailSprite(parent, "Roof Rib", detailSprite, new Vector3(-0.18f + i * 0.18f, 0.02f, -0.04f),
                               new Vector3(0.035f, 0.30f, 1f), 25f, new Color(0.28f, 0.06f, 0.04f, 0.46f),
                               sortingOrder);
        }
    }

    private void CreateRockDetails(Transform parent, Vector2Int cell, int sortingOrder)
    {
        for (int i = 0; i < 3; i++)
        {
            Vector3 position = DetailPosition(cell, i, 0.25f, 0.11f);
            CreateDetailSprite(parent, "Rock Chip", dotSprite, position,
                               Vector3.one * (0.055f + Stable01(cell, i + 8) * 0.055f), 0f,
                               new Color(0.18f, 0.16f, 0.13f, 0.42f), sortingOrder);
        }
    }

    private void CreateGrassDetails(Transform parent, Vector2Int cell, int sortingOrder, int elevation)
    {
        int count = elevation > 0 ? 3 : 2;
        for (int i = 0; i < count; i++)
        {
            Vector3 position = DetailPosition(cell, i, 0.30f, 0.12f);
            Color color = elevation > 0 ? new Color(0.62f, 0.63f, 0.36f, 0.42f)
                                        : new Color(0.25f, 0.50f, 0.24f, 0.36f);
            CreateDetailSprite(parent, "Grass Brush", detailSprite, position,
                               new Vector3(0.025f, 0.18f + Stable01(cell, i + 9) * 0.10f, 1f),
                               -22f + Stable01(cell, i + 10) * 44f, color, sortingOrder);
        }
    }

    private void CreateDetailSprite(Transform parent, string name, Sprite sprite, Vector3 localPosition,
                                    Vector3 localScale, float zRotation, Color color, int sortingOrder)
    {
        GameObject detail = new GameObject(name);
        detail.transform.SetParent(parent, false);
        detail.transform.localPosition = localPosition;
        detail.transform.localScale = localScale;
        detail.transform.localRotation = Quaternion.Euler(0f, 0f, zRotation);

        SpriteRenderer renderer = detail.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
    }

    private Vector3 DetailPosition(Vector2Int cell, int index, float xRadius, float yRadius)
    {
        float x = (Stable01(cell, index * 11 + 1) - 0.5f) * xRadius;
        float y = (Stable01(cell, index * 11 + 2) - 0.5f) * yRadius;
        return new Vector3(x, y, -0.04f);
    }

    private Color VaryTerrainColor(Color color, Vector2Int cell)
    {
        float amount = -0.028f + Stable01(cell, 99) * 0.056f;
        Color target = amount >= 0f ? Color.white : Color.black;
        return Color.Lerp(color, target, Mathf.Abs(amount));
    }

    private static float Stable01(Vector2Int cell, int salt)
    {
        unchecked
        {
            int hash = cell.x * 73856093 ^ cell.y * 19349663 ^ salt * 83492791;
            hash ^= hash << 13;
            hash ^= hash >> 17;
            hash ^= hash << 5;
            return (hash & 0x7fffffff) / (float)int.MaxValue;
        }
    }

    private TextMesh CreateTileNameLabel(Transform parent, TerrainProfile profile)
    {
        string text = string.Empty;
        if (profile.objective)
        {
            text = "目";
        }
        else if (profile.isChokePoint)
        {
            text = "狹";
        }
        else if (profile.elevation >= 2)
        {
            text = "高" + profile.elevation;
        }
        else if (profile.blocksLineOfSight)
        {
            text = "遮";
        }
        else if (profile.danger)
        {
            text = "危";
        }

        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        TextMesh label = CreateWorldLabel(parent, text, 40, new Vector3(0f, 0.03f, -0.06f),
                                          new Color(0.98f, 0.92f, 0.70f, 0.92f), 1800);
        label.gameObject.SetActive(false);
        return label;
    }

    private TextMesh CreateWorldLabel(Transform parent, string text, int fontSize, Vector3 localPosition, Color color,
                                      int sortingOrder)
    {
        GameObject labelObject = new GameObject("Map Label");
        labelObject.transform.SetParent(parent, false);
        labelObject.transform.localPosition = localPosition;

        TextMesh mesh = labelObject.AddComponent<TextMesh>();
        mesh.text = text;
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.alignment = TextAlignment.Center;
        mesh.fontSize = fontSize;
        mesh.characterSize = 0.018f;
        mesh.color = color;

        MeshRenderer renderer = labelObject.GetComponent<MeshRenderer>();
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = sortingOrder;
        return mesh;
    }

    private void SpawnUnits()
    {
        Transform unitRoot = new GameObject("Units").transform;
        unitRoot.SetParent(transform, false);

        foreach (BattleTestUnitDefinition definition in unitDefinitions)
        {
            if (!IsInside(definition.startCell))
            {
                continue;
            }

            GameObject unitObject = new GameObject(definition.displayName);
            unitObject.transform.SetParent(unitRoot, false);
            unitObject.transform.position = UnitWorldPosition(definition.startCell);

            CharacterVisualController visual = unitObject.AddComponent<CharacterVisualController>();
            visual.visual = definition.visual;
            visual.sortingLayerName = "Default";
            visual.baseSortingOrder = 3000;
            visual.ApplyVisual();

            CircleCollider2D collider = unitObject.AddComponent<CircleCollider2D>();
            collider.radius = 0.26f;
            collider.offset = new Vector2(0f, 0.28f);

            ShadowBlob shadow = unitObject.AddComponent<ShadowBlob>();
            shadow.Configure(new Vector2(0.92f, 0.26f), new Color(0.025f, 0.022f, 0.018f, 0.32f), 2940);

            BattleTestUnitView view = unitObject.AddComponent<BattleTestUnitView>();
            BattleTestUnit unit = new BattleTestUnit(definition, view);
            unit.initiative = definition.initiative + random.Next(0, 5);
            view.Bind(unit, visual);
            units.Add(unit);
        }
    }

    private void BeginPlayerPhase()
    {
        if (CheckBattleEnd())
        {
            return;
        }

        phaseTurn.BeginPlayerPhase();
        round = phaseTurn.Round;
        aiQueued = false;
        commandMode = BattleCommandMode.Move;
        PrepareFactionForPhase(Faction.Ally);
        if (CheckBattleEnd())
        {
            return;
        }

        activeUnit = FindNextReadyUnit(Faction.Ally);
        AddLog($"[페이즈] 제 {round}턴 아군 페이즈");
        RefreshHighlights();
        RefreshUnits();
    }

    private void BeginEnemyPhase()
    {
        if (CheckBattleEnd())
        {
            return;
        }

        phaseTurn.BeginEnemyPhase();
        aiQueued = false;
        activeUnit = null;
        commandMode = BattleCommandMode.Move;
        PrepareFactionForPhase(Faction.Enemy);
        if (CheckBattleEnd())
        {
            return;
        }

        ClearHighlights();
        AddLog($"[페이즈] 제 {round}턴 적 페이즈");
        RefreshUnits();
    }

    private void EndTurn()
    {
        if (battleOver || !phaseTurn.IsPlayerPhase)
        {
            return;
        }

        EndPlayerPhase();
    }

    private void EndPlayerPhase()
    {
        activeUnit = null;
        ClearHighlights();
        BeginEnemyPhase();
    }

    private void EndEnemyPhase()
    {
        phaseTurn.CompleteEnemyPhase();
        round = phaseTurn.Round;
        TickRoundEffects();
        BeginPlayerPhase();
    }

    private void PrepareFactionForPhase(Faction faction)
    {
        foreach (BattleTestUnit unit in units)
        {
            if (unit.defeated || unit.definition.faction != faction)
            {
                continue;
            }

            ApplyStartOfTurn(unit);
            if (unit.defeated)
            {
                continue;
            }

            unit.ResetActions(EffectiveMoveRange(unit));
        }
    }

    private BattleTestUnit FindNextReadyUnit(Faction faction)
    {
        foreach (BattleTestUnit unit in units)
        {
            if (!unit.defeated && unit.definition.faction == faction && !unit.acted)
            {
                return unit;
            }
        }

        return null;
    }

    private void HandlePointer(Vector3 screenPosition)
    {
        Vector3 world = Camera.main.ScreenToWorldPoint(screenPosition);
        Vector2 point = new Vector2(world.x, world.y);
        Collider2D[] hits = Physics2D.OverlapPointAll(point);

        BattleTestUnit clickedUnit = null;
        BattleTestTile clickedTile = null;

        foreach (Collider2D hit in hits)
        {
            BattleTestUnitView view = hit.GetComponentInParent<BattleTestUnitView>();
            if (view != null && view.Unit != null && !view.Unit.defeated)
            {
                clickedUnit = view.Unit;
                break;
            }
        }

        foreach (Collider2D hit in hits)
        {
            BattleTestTile tile = hit.GetComponent<BattleTestTile>();
            if (tile != null)
            {
                clickedTile = tile;
                break;
            }
        }

        if (phaseTurn.IsPlayerPhase && clickedUnit != null && clickedUnit.definition.faction == Faction.Ally)
        {
            SelectPlayerUnit(clickedUnit);
            return;
        }

        if (activeUnit == null)
        {
            AddLog("[UI] 행동할 아군을 선택하세요.");
            return;
        }

        if (scoutMode)
        {
            if (clickedTile != null && TryScoutDeploy(clickedTile))
            {
                return;
            }

            AddLog("[Scout] Select an ally, then click a southern deployment tile.");
            return;
        }

        if (commandMode == BattleCommandMode.Attack)
        {
            if (clickedUnit != null && clickedUnit.definition.faction != activeUnit.definition.faction)
            {
                TryAttack(activeUnit, clickedUnit, false);
            }
            else
            {
                AddLog("[UI] 공격할 적을 선택하세요.");
            }

            return;
        }

        if (commandMode == BattleCommandMode.Skill)
        {
            if (clickedUnit != null)
            {
                TrySpecial(activeUnit, clickedUnit);
            }
            else
            {
                AddLog("[UI] 무공 대상을 선택하세요.");
            }

            return;
        }

        if (commandMode == BattleCommandMode.Interact)
        {
            if (clickedTile != null)
            {
                TryInteract(activeUnit, clickedTile);
            }
            else
            {
                AddLog("[UI] 사용할 지형지물을 선택하세요.");
            }

            return;
        }

        if (commandMode == BattleCommandMode.Move && clickedTile != null)
        {
            TryMove(activeUnit, clickedTile);
            return;
        }

        if (clickedUnit != null && clickedUnit.definition.faction != activeUnit.definition.faction)
        {
            SetCommandMode(BattleCommandMode.Attack);
        }
    }

    private void SelectPlayerUnit(BattleTestUnit unit)
    {
        if (!phaseTurn.CanPlayerControl(unit))
        {
            AddLog("[UI] 이미 행동한 아군입니다.");
            return;
        }

        activeUnit = unit;
        commandMode = BattleCommandMode.Move;
        AddLog($"[선택] {unit.definition.displayName}");
        RefreshHighlights();
        RefreshUnits();
    }

    private bool TryScoutDeploy(BattleTestTile destination)
    {
        if (!scoutMode || activeUnit == null || destination == null)
        {
            return false;
        }

        if (!IsDeploymentCell(destination.cell))
        {
            AddLog("[Scout] 남쪽 배치 구역에만 재배치할 수 있습니다.");
            return false;
        }

        if (!destination.walkable || UnitAt(destination.cell) != null)
        {
            AddLog("[Scout] 해당 배치칸은 사용할 수 없습니다.");
            return false;
        }

        activeUnit.cell = destination.cell;
        if (activeUnit.view != null)
        {
            activeUnit.view.transform.position = UnitWorldPosition(destination.cell);
        }

        AddLog($"[Scout] {activeUnit.definition.displayName} deployment -> ({destination.cell.x},{destination.cell.y}).");
        RefreshHighlights();
        RefreshUnits();
        return true;
    }

    private bool IsDeploymentCell(Vector2Int cell)
    {
        BattleTestTile tile = TileAt(cell);
        if (tile == null)
        {
            return false;
        }

        if (string.Equals(tile.laneId, "south_deployment", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return cell.y <= 2 && cell.x >= 5 && cell.x <= 13;
    }

    private void TryMove(BattleTestUnit unit, BattleTestTile destination)
    {
        if (!unit.CanMove)
        {
            AddLog("[이동] 이미 이동했습니다.");
            return;
        }

        if (!destination.walkable || UnitAt(destination.cell) != null)
        {
            AddLog("[이동] 진입할 수 없는 칸입니다.");
            return;
        }

        Dictionary<Vector2Int, int> reachable = GetReachableCells(unit);
        if (!reachable.ContainsKey(destination.cell))
        {
            AddLog("[이동] 이동 범위를 벗어났습니다.");
            return;
        }

        unit.cell = destination.cell;
        unit.SpendMovement(reachable[destination.cell]);
        ApplyTileEntry(unit, destination);
        StartCoroutine(AnimateMove(unit, UnitWorldPosition(destination.cell)));
        AddLog($"[이동] {unit.definition.displayName} 이동.");
    }

    private bool TryAttack(BattleTestUnit attacker, BattleTestUnit target, bool endAfterAttack)
    {
        if (!attacker.CanUseMainAction)
        {
            AddLog("[행동] 이미 행동했습니다.");
            return false;
        }

        if (target.defeated)
        {
            return false;
        }

        int range = EffectiveAttackRange(attacker);
        int distance = GridDistance(attacker.cell, target.cell);
        if (distance > range)
        {
            AddLog("[공격] 대상이 사거리 밖입니다.");
            return false;
        }

        if (range > 1 && !HasLineOfSight(attacker.cell, target.cell))
        {
            AddLog("[공격] 대나무숲/연막/담장에 시야가 막혔습니다.");
            return false;
        }

        ResolveAttack(attacker, target, false);
        ResolvePostAttack(attacker, target, false);
        attacker.SpendMainAction();
        RefreshUnits();

        if (CheckBattleEnd())
        {
            return true;
        }

        if (AdvanceAfterAction(attacker))
        {
            return true;
        }

        if (endAfterAttack)
        {
            EndTurn();
        }
        else
        {
            RefreshHighlights();
        }

        return true;
    }

    private bool ResolveAttack(BattleTestUnit attacker, BattleTestUnit target, bool special)
    {
        BattleTestTile from = TileAt(attacker.cell);
        BattleTestTile to = TileAt(target.cell);
        string moveName = special ? attacker.definition.specialName : "공격";
        int d20 = random.Next(1, 21);
        int heightBonus = HeightAttackModifier(from, to);
        int attackBonus = attacker.definition.attackBonus + (special ? attacker.definition.specialAttackBonus : 0);
        int attackTotal = d20 + attackBonus + heightBonus;
        int defense = DefenseValue(target, to);
        bool critical = d20 == 20;
        bool hit = critical || (d20 != 1 && attackTotal >= defense);

        if (!hit)
        {
            AddLog($"[빗나감] {attacker.definition.displayName}의 {moveName} 실패.");
            return false;
        }

        int damage = random.Next(attacker.definition.damageMin, attacker.definition.damageMax + 1);
        if (special)
        {
            damage += attacker.definition.specialPower;
        }

        if (critical)
        {
            damage *= 2;
        }

        damage += HeightDamageBonus(heightBonus);
        if (target.guarded)
        {
            damage = Mathf.Max(1, Mathf.CeilToInt(damage * 0.55f));
        }

        target.hp = Mathf.Max(0, target.hp - damage);
        AddLog($"[명중] {attacker.definition.displayName}: {moveName}! 피해 {damage}.");

        if (target.hp == 0)
        {
            target.defeated = true;
            target.view.SetDefeated(true);
            AddLog($"[전투불능] {target.definition.displayName} 쓰러짐.");
        }

        return true;
    }

    private bool TrySpecial(BattleTestUnit actor, BattleTestUnit target)
    {
        if (!CanUseSpecial(actor))
        {
            AddLog("[무공] 사용할 수 없습니다.");
            return false;
        }

        if (!IsValidSpecialTarget(actor, target))
        {
            AddLog("[무공] 대상이 맞지 않습니다.");
            return false;
        }

        int range = EffectiveSpecialRange(actor);
        int distance = GridDistance(actor.cell, target.cell);
        if (distance > range)
        {
            AddLog("[무공] 대상이 사거리 밖입니다.");
            return false;
        }

        if (range > 1 && IsHostileAttackSpecial(actor.definition.specialEffect) &&
            !HasLineOfSight(actor.cell, target.cell))
        {
            AddLog("[무공] 시야가 막혀 펼칠 수 없습니다.");
            return false;
        }

        actor.inner -= actor.definition.specialCost;
        actor.specialCooldownLeft = actor.definition.specialCooldown;
        actor.SpendMainAction();

        bool attackLike = ApplySpecialEffect(actor, target, true);
        if (attackLike)
        {
            ResolvePostAttack(actor, target, true);
        }

        RefreshHighlights();
        RefreshUnits();
        if (CheckBattleEnd())
        {
            return true;
        }

        AdvanceAfterAction(actor);
        return true;
    }

    private bool ApplySpecialEffect(BattleTestUnit actor, BattleTestUnit target, bool allowStatus)
    {
        switch (actor.definition.specialEffect)
        {
        case BattleSpecialEffect.Heal:
            int healed =
                Mathf.Min(target.definition.maxHp - target.hp, actor.definition.specialPower + random.Next(4, 9));
            target.hp += Mathf.Max(0, healed);
            target.poisoned = false;
            target.chilled = false;
            AddLog(
                $"[무공] {actor.definition.displayName}: {actor.definition.specialName}. {target.definition.displayName} 회복 {healed}.");
            return false;
        case BattleSpecialEffect.Poison:
            bool poisonHit = ResolveAttack(actor, target, true);
            if (allowStatus && poisonHit && !target.defeated)
            {
                target.poisoned = true;
                CreatePoisonSmoke(target.cell);
                AddLog($"[상태] {target.definition.displayName} 중독.");
            }
            return true;
        case BattleSpecialEffect.Freeze:
            bool freezeHit = ResolveAttack(actor, target, true);
            if (allowStatus && freezeHit && !target.defeated)
            {
                target.chilled = true;
                FreezeWaterAround(target.cell);
                AddLog($"[상태] {target.definition.displayName} 둔화.");
            }
            return true;
        case BattleSpecialEffect.Mark:
            target.marked = true;
            AddLog($"[파훼] {actor.definition.displayName}가 {target.definition.displayName}의 검로를 읽었다.");
            return false;
        case BattleSpecialEffect.BreakGuard:
            target.guarded = false;
            target.marked = true;
            bool palmHit = ResolveAttack(actor, target, true);
            if (palmHit && !target.defeated)
            {
                TryPushTarget(actor, target, 1, "palm strike");
            }
            return true;
        default:
            ResolveAttack(actor, target, true);
            return true;
        }
    }

    private void ResolvePostAttack(BattleTestUnit attacker, BattleTestUnit target, bool special)
    {
        if (attacker == null || target == null || attacker.defeated || target.defeated)
        {
            return;
        }

        BattleTestCounterMove counter = FindCounterMove(target, attacker);
        if (counter.valid)
        {
            target.SpendReaction();
            AddLog($"[반격] {target.definition.displayName}: {counter.label}.");
            if (counter.special)
            {
                target.inner = Mathf.Max(0, target.inner - target.definition.specialCost);
                target.specialCooldownLeft = Mathf.Max(target.specialCooldownLeft, target.definition.specialCooldown);
                ApplySpecialEffect(target, attacker, true);
            }
            else
            {
                ResolveAttack(target, attacker, false);
            }
        }
        else
        {
            AddLog($"[반격] {target.definition.displayName} 반격 불가.");
        }

        if (!attacker.defeated && !target.defeated && CanFollowUp(attacker, target, special))
        {
            AddLog($"[추격] {attacker.definition.displayName}가 빈틈을 찔렀다.");
            ResolveAttack(attacker, target, special);
        }
    }

    private BattleTestCounterMove FindCounterMove(BattleTestUnit defender, BattleTestUnit attacker)
    {
        if (defender == null || attacker == null || defender.defeated || attacker.defeated)
        {
            return BattleTestCounterMove.None;
        }

        int distance = GridDistance(defender.cell, attacker.cell);
        if (defender.marked)
        {
            return BattleTestCounterMove.None;
        }

        if (!defender.CanReact)
        {
            return BattleTestCounterMove.None;
        }

        int attackRange = EffectiveAttackRange(defender);
        if (distance <= attackRange)
        {
            if (attackRange > 1 && !HasLineOfSight(defender.cell, attacker.cell))
            {
                return BattleTestCounterMove.None;
            }

            return new BattleTestCounterMove(false, "기본 공격");
        }

        int specialRange = EffectiveSpecialRange(defender);
        if (CanUseCounterSpecial(defender) && distance <= specialRange)
        {
            if (specialRange > 1 && !HasLineOfSight(defender.cell, attacker.cell))
            {
                return BattleTestCounterMove.None;
            }

            return new BattleTestCounterMove(true, defender.definition.specialName);
        }

        return BattleTestCounterMove.None;
    }

    private bool CanUseCounterSpecial(BattleTestUnit unit)
    {
        if (unit == null || unit.defeated || !unit.CanReact || unit.inner < unit.definition.specialCost ||
            unit.specialCooldownLeft > 0)
        {
            return false;
        }

        return IsHostileAttackSpecial(unit.definition.specialEffect);
    }

    private bool CanFollowUp(BattleTestUnit attacker, BattleTestUnit target, bool special)
    {
        if (attacker == null || target == null || attacker.defeated || target.defeated)
        {
            return false;
        }

        int range = special ? EffectiveSpecialRange(attacker) : EffectiveAttackRange(attacker);
        if (range >= 4 || (special && (attacker.definition.specialEffect == BattleSpecialEffect.Heal ||
                                       attacker.definition.specialEffect == BattleSpecialEffect.Mark)))
        {
            return false;
        }

        return AgilityValue(attacker) - AgilityValue(target) >= 5;
    }

    private int AgilityValue(BattleTestUnit unit)
    {
        if (unit == null)
        {
            return 0;
        }

        return unit.definition.agility >= 0 ? unit.definition.agility : unit.definition.initiative;
    }

    private int EffectiveAttackRange(BattleTestUnit unit)
    {
        return EffectiveAttackRange(unit, unit == null ? null : TileAt(unit.cell));
    }

    private int EffectiveAttackRange(BattleTestUnit unit, BattleTestTile fromTile)
    {
        if (unit == null)
        {
            return 0;
        }

        return ApplyHighGroundRangeBonus(unit.definition.attackRange, fromTile);
    }

    private int EffectiveSpecialRange(BattleTestUnit unit)
    {
        return EffectiveSpecialRange(unit, unit == null ? null : TileAt(unit.cell));
    }

    private int EffectiveSpecialRange(BattleTestUnit unit, BattleTestTile fromTile)
    {
        if (unit == null)
        {
            return 0;
        }

        return ApplyHighGroundRangeBonus(unit.definition.specialRange, fromTile);
    }

    private int ApplyHighGroundRangeBonus(int baseRange, BattleTestTile fromTile)
    {
        if (baseRange > 1 && fromTile != null && fromTile.elevation >= HighGroundRangeBonusElevation)
        {
            return baseRange + 1;
        }

        return baseRange;
    }

    private static int HeightAttackModifier(BattleTestTile fromTile, BattleTestTile toTile)
    {
        if (fromTile == null || toTile == null)
        {
            return 0;
        }

        int delta = fromTile.elevation - toTile.elevation;
        if (delta >= 2)
        {
            return 3;
        }

        if (delta > 0)
        {
            return 2;
        }

        if (delta <= -2)
        {
            return -2;
        }

        return delta < 0 ? -1 : 0;
    }

    private static int HeightDamageBonus(int heightAttackModifier)
    {
        return Mathf.Max(0, heightAttackModifier);
    }

    private bool TryInteract(BattleTestUnit actor, BattleTestTile clickedTile)
    {
        if (actor == null || !actor.CanUseMainAction)
        {
            AddLog("[행동] 이미 행동했습니다.");
            return false;
        }

        BattleTestInteractable interactable = FindUsableInteractable(actor, clickedTile.cell);
        if (interactable == null)
        {
            AddLog("[지형] 닿는 곳에 사용할 지형지물이 없습니다.");
            return false;
        }

        if (interactable.kind == BattleTestInteractableKind.Objective)
        {
            AddLog("[목표] 현판은 지켜야 합니다. 적이 닿기 전에 병목을 막으세요.");
            return false;
        }

        interactable.used = true;
        FadeInteractable(interactable);

        BattleTestTile tile = TileAt(interactable.cell);
        switch (interactable.kind)
        {
        case BattleTestInteractableKind.Smoke:
            if (tile != null)
            {
                tile.smokeTurns = 2;
                tile.coverBonus = tile.baseCoverBonus + (tile.extraCover ? CoverInteractBonus : 0) + SmokeCoverBonus;
                RefreshTerrainTint(tile);
            }
            AddLog(
                $"[지형] {actor.definition.displayName}가 {interactable.displayName}를 흔들었다. 연기 엄폐 +{SmokeCoverBonus}.");
            break;
        case BattleTestInteractableKind.Fire:
            if (tile != null)
            {
                tile.fireTurns = 2;
                RefreshTerrainTint(tile);
            }
            DamageUnitsAround(actor, interactable.cell, 1, FireInteractDamage, "화염");
            AddLog(
                $"[지형] {actor.definition.displayName}가 {interactable.displayName}를 터뜨렸다. 화염 피해 {FireInteractDamage}.");
            break;
        case BattleTestInteractableKind.Cover:
            if (tile != null)
            {
                if (!tile.extraCover)
                {
                    tile.coverBonus += CoverInteractBonus;
                    tile.extraCover = true;
                }

                RefreshTerrainTint(tile);
            }
            AddLog(
                $"[지형] {actor.definition.displayName}가 {interactable.displayName}를 밀었다. 엄폐 +{CoverInteractBonus}.");
            break;
        case BattleTestInteractableKind.CollapseBridge:
            int collapsed = CollapseBridgeAt(interactable.cell);
            DamageUnitsAround(actor, interactable.cell, 1, FallDamage, "다리 붕괴");
            AddLog($"[지형] {actor.definition.displayName}가 밧줄을 끊었다. 우측 다리 {collapsed}칸 붕괴.");
            break;
        case BattleTestInteractableKind.BambooFall:
            int blocked = DropBambooAt(interactable.cell);
            AddLog($"[지형] {actor.definition.displayName}가 대나무 묶음을 베었다. {blocked}칸 시야 차단.");
            break;
        case BattleTestInteractableKind.Rockfall:
            int rubble = DropRockAt(interactable.cell);
            DamageUnitsAround(actor, interactable.cell, 1, FallDamage, "낙석");
            AddLog($"[지형] {actor.definition.displayName}가 석등을 무너뜨렸다. 낙석 피해 {FallDamage}, 잔해 {rubble}칸.");
            break;
        }

        actor.SpendMainAction();
        RefreshHighlights();
        RefreshUnits();
        if (CheckBattleEnd())
        {
            return true;
        }

        AdvanceAfterAction(actor);
        return true;
    }

    private void GuardActiveUnit()
    {
        if (activeUnit == null || !activeUnit.CanUseMainAction || activeUnit.definition.faction != Faction.Ally)
        {
            return;
        }

        activeUnit.guarded = true;
        activeUnit.SpendMainAction();
        AddLog($"[방어] {activeUnit.definition.displayName} 방어 태세.");
        RefreshHighlights();
        RefreshUnits();
        AdvanceAfterAction(activeUnit);
    }

    private bool AdvanceAfterAction(BattleTestUnit actor)
    {
        if (actor == null || actor.definition.faction != Faction.Ally || !phaseTurn.IsPlayerPhase)
        {
            return false;
        }

        if (phaseTurn.AllFactionUnitsActed(units, Faction.Ally))
        {
            AddLog("[페이즈] 모든 아군 행동 완료");
            EndPlayerPhase();
            return true;
        }

        activeUnit = FindNextReadyUnit(Faction.Ally);
        commandMode = BattleCommandMode.Move;
        RefreshHighlights();
        RefreshUnits();
        return false;
    }

    private IEnumerator AnimateMove(BattleTestUnit unit, Vector3 target)
    {
        busy = true;
        Vector3 start = unit.view.transform.position;
        float elapsed = 0f;
        const float duration = 0.18f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            unit.view.transform.position = Vector3.Lerp(start, target, t);
            yield return null;
        }

        unit.view.transform.position = target;
        busy = false;
        RefreshHighlights();
        RefreshUnits();
    }

    private IEnumerator RunEnemyPhase()
    {
        busy = true;
        yield return new WaitForSeconds(0.3f);

        foreach (BattleTestUnit enemy in units)
        {
            if (enemy.defeated || enemy.definition.faction != Faction.Enemy || enemy.acted)
            {
                continue;
            }

            activeUnit = enemy;
            commandMode = BattleCommandMode.Move;
            RefreshHighlights();
            RefreshUnits();
            yield return new WaitForSeconds(0.25f);

            BattleTestUnit target = FindNearestEnemy(activeUnit);
            if (target == null)
            {
                activeUnit.SpendMainAction();
                continue;
            }

            int desiredRange = CanUseSpecial(activeUnit) ? EffectiveSpecialRange(activeUnit) : EffectiveAttackRange(activeUnit);
            if ((GridDistance(activeUnit.cell, target.cell) > desiredRange ||
                 (desiredRange > 1 && !HasLineOfSight(activeUnit.cell, target.cell))) &&
                !activeUnit.moved)
            {
                BattleTestTile best = FindBestMoveToward(activeUnit, target.cell);
                if (best != null)
                {
                    int moveCost = 0;
                    Dictionary<Vector2Int, int> reachable = GetReachableCells(activeUnit);
                    reachable.TryGetValue(best.cell, out moveCost);
                    activeUnit.cell = best.cell;
                    activeUnit.SpendMovement(moveCost);
                    ApplyTileEntry(activeUnit, best);
                    yield return AnimateMove(activeUnit, UnitWorldPosition(best.cell));
                    yield return new WaitForSeconds(0.15f);
                }
            }

            if (CanUseSpecial(activeUnit) && IsValidSpecialTarget(activeUnit, target) &&
                GridDistance(activeUnit.cell, target.cell) <= EffectiveSpecialRange(activeUnit) &&
                (!IsHostileAttackSpecial(activeUnit.definition.specialEffect) ||
                 EffectiveSpecialRange(activeUnit) <= 1 ||
                 HasLineOfSight(activeUnit.cell, target.cell)))
            {
                TrySpecial(activeUnit, target);
            }
            else
            {
                TryAttack(activeUnit, target, false);
            }

            if (battleOver)
            {
                busy = false;
                yield break;
            }

            yield return new WaitForSeconds(0.3f);
        }

        busy = false;
        activeUnit = null;
        aiQueued = false;
        EndEnemyPhase();
    }

    private BattleTestTile FindBestMoveToward(BattleTestUnit unit, Vector2Int targetCell)
    {
        Dictionary<Vector2Int, int> reachable = GetReachableCells(unit);
        BattleTestTile best = null;
        int bestScore = int.MinValue;

        foreach (KeyValuePair<Vector2Int, int> pair in reachable)
        {
            if (pair.Key == unit.cell || UnitAt(pair.Key) != null)
            {
                continue;
            }

            BattleTestTile tile = TileAt(pair.Key);
            if (tile == null)
            {
                continue;
            }

            int distance = GridDistance(pair.Key, targetCell);
            int score = -distance * 12 - pair.Value * 2 + tile.elevation * 5 + tile.coverBonus * 3;
            if (tile.isChokePoint)
            {
                score += 8;
            }

            if (tile.danger || tile.fireTurns > 0)
            {
                score -= 12;
            }

            int range = CanUseSpecial(unit) ? EffectiveSpecialRange(unit, tile) : EffectiveAttackRange(unit, tile);
            if (distance <= range && (range <= 1 || HasLineOfSight(pair.Key, targetCell)))
            {
                score += 18;
            }

            if (score > bestScore)
            {
                bestScore = score;
                best = tile;
            }
        }

        return best;
    }

    private BattleTestUnit FindNearestEnemy(BattleTestUnit unit)
    {
        BattleTestUnit best = null;
        int bestDistance = int.MaxValue;

        foreach (BattleTestUnit other in units)
        {
            if (other.defeated || other.definition.faction == unit.definition.faction)
            {
                continue;
            }

            int distance = GridDistance(unit.cell, other.cell);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = other;
            }
        }

        return best;
    }

    private void ApplyStartOfTurn(BattleTestUnit unit)
    {
        unit.guarded = false;
        unit.marked = false;
        unit.inner = Mathf.Min(unit.definition.maxInner, unit.inner + 1);
        unit.specialCooldownLeft = Mathf.Max(0, unit.specialCooldownLeft - 1);

        if (unit.poisoned)
        {
            unit.hp = Mathf.Max(0, unit.hp - 3);
            AddLog($"[상태] {unit.definition.displayName} 중독 피해 3.");
            if (unit.hp == 0)
            {
                unit.defeated = true;
                unit.view.SetDefeated(true);
            }
        }
    }

    private void TickRoundEffects()
    {
        if (tiles == null)
        {
            return;
        }

        bool faded = false;
        foreach (BattleTestTile tile in tiles)
        {
            if (tile == null)
            {
                continue;
            }

            int previousSmoke = tile.smokeTurns;
            int previousFire = tile.fireTurns;
            tile.smokeTurns = Mathf.Max(0, tile.smokeTurns - 1);
            tile.fireTurns = Mathf.Max(0, tile.fireTurns - 1);

            if (previousSmoke > 0 && tile.smokeTurns == 0)
            {
                tile.coverBonus = tile.baseCoverBonus + (tile.extraCover ? CoverInteractBonus : 0);
                faded = true;
            }

            if (previousFire > 0 && tile.fireTurns == 0)
            {
                faded = true;
            }

            if (previousSmoke != tile.smokeTurns || previousFire != tile.fireTurns)
            {
                RefreshTerrainTint(tile);
            }
        }

        if (faded)
        {
            AddLog("[지형] 연기와 화염이 잦아들었다.");
        }
    }

    private int EffectiveMoveRange(BattleTestUnit unit)
    {
        int range = unit.definition.moveRange;
        if (unit.chilled)
        {
            range = Mathf.Max(2, range - 1);
        }

        return range;
    }

    private bool CanUseSpecial(BattleTestUnit unit)
    {
        return unit != null && unit.CanUseMainAction && unit.inner >= unit.definition.specialCost &&
               unit.specialCooldownLeft <= 0 && unit.definition.specialEffect != BattleSpecialEffect.None;
    }

    private bool IsValidSpecialTarget(BattleTestUnit actor, BattleTestUnit target)
    {
        if (actor == null || target == null || target.defeated)
        {
            return false;
        }

        if (actor.definition.specialEffect == BattleSpecialEffect.Heal)
        {
            return target.definition.faction == actor.definition.faction && target.hp < target.definition.maxHp;
        }

        return target.definition.faction != actor.definition.faction;
    }

    private BattleForecast BuildForecast(BattleTestUnit actor, BattleTestUnit target)
    {
        if (actor == null || actor.defeated)
        {
            return BattleForecast.Invalid("행동할 아군을 선택하세요.");
        }

        bool attack = commandMode == BattleCommandMode.Attack;
        bool special = commandMode == BattleCommandMode.Skill;
        if (!attack && !special)
        {
            return BattleForecast.Invalid("공격 또는 무공을 선택한 뒤 대상을 가리키세요.");
        }

        if (target == null)
        {
            return BattleForecast.Invalid("대상을 가리키면 전투 예측이 표시됩니다.");
        }

        string commandName = special ? actor.definition.specialName : "공격";
        int range = special ? EffectiveSpecialRange(actor) : EffectiveAttackRange(actor);
        int distance = GridDistance(actor.cell, target.cell);
        string costText =
            special ? $"소모: 내공 {actor.definition.specialCost} / 재사용 {actor.definition.specialCooldown}턴"
                    : "소모: 행동 1회";

        if (target.defeated)
        {
            return BattleForecast.Invalid(actor.definition.displayName, target.definition.displayName, commandName,
                                          "이미 전투불능인 대상입니다.", distance, range, costText);
        }

        if (special)
        {
            string unavailable = SpecialUnavailableReason(actor);
            if (!string.IsNullOrEmpty(unavailable))
            {
                return BattleForecast.Invalid(actor.definition.displayName, target.definition.displayName, commandName,
                                              unavailable, distance, range, costText);
            }

            if (!IsValidSpecialTarget(actor, target))
            {
                return BattleForecast.Invalid(actor.definition.displayName, target.definition.displayName, commandName,
                                              "무공 대상 조건이 맞지 않습니다.", distance, range, costText);
            }
        }
        else if (target.definition.faction == actor.definition.faction)
        {
            return BattleForecast.Invalid(actor.definition.displayName, target.definition.displayName, commandName,
                                          "아군은 공격 대상이 아닙니다.", distance, range, costText);
        }

        if (distance > range)
        {
            return BattleForecast.Invalid(actor.definition.displayName, target.definition.displayName, commandName,
                                          "사거리 밖입니다.", distance, range, costText);
        }

        if (range > 1 && (!special || IsHostileAttackSpecial(actor.definition.specialEffect)) &&
            !HasLineOfSight(actor.cell, target.cell))
        {
            return BattleForecast.Invalid(actor.definition.displayName, target.definition.displayName, commandName,
                                          "시야가 막혔습니다.", distance, range, costText);
        }

        BattleTestTile from = TileAt(actor.cell);
        BattleTestTile to = TileAt(target.cell);
        int heightBonus = HeightAttackModifier(from, to);
        int terrainBonus = 0;
        int attackBonus = actor.definition.attackBonus + (special ? actor.definition.specialAttackBonus : 0);
        int defense = DefenseValue(target, to);
        bool attackLike = !special || IsHostileAttackSpecial(actor.definition.specialEffect);
        BattleTestCounterMove counter = attackLike ? FindCounterMove(target, actor) : BattleTestCounterMove.None;
        bool followUp = attackLike && CanFollowUp(actor, target, special);
        string neededRollText =
            attackLike ? NeededRollText(defense, attackBonus, heightBonus, terrainBonus) : "판정 없음";
        if (!attackLike)
        {
            attackBonus = 0;
            heightBonus = 0;
            terrainBonus = 0;
            defense = 0;
        }

        int damageMin = 0;
        int damageMax = 0;
        string damageText = "피해: 없음 | 파훼 +0";
        string hpAfterText = $"예상 전투 후 체력: {target.hp}-{target.hp}";
        if (attackLike)
        {
            int heightDamageBonus = HeightDamageBonus(heightBonus);
            damageMin = actor.definition.damageMin + (special ? actor.definition.specialPower : 0) + heightDamageBonus;
            damageMax = actor.definition.damageMax + (special ? actor.definition.specialPower : 0) + heightDamageBonus;
            if (target.guarded)
            {
                damageMin = Mathf.Max(1, Mathf.CeilToInt(damageMin * 0.55f));
                damageMax = Mathf.Max(1, Mathf.CeilToInt(damageMax * 0.55f));
            }

            damageMin = Mathf.Max(1, damageMin);
            damageMax = Mathf.Max(1, damageMax);
            int breakGain = special ? 18 : 12;
            damageText = $"피해 {damageMin}-{damageMax} | 치명 5% | 파훼 +{breakGain}";
            hpAfterText =
                $"예상 전투 후 체력: {Mathf.Max(0, target.hp - damageMax)}-{Mathf.Max(0, target.hp - damageMin)}";
        }

        return new BattleForecast(
            true, string.Empty, actor.definition.displayName, target.definition.displayName, commandName, distance,
            range, distance <= range ? "사거리 안" : "사거리 밖", attackBonus, heightBonus, terrainBonus, defense,
            neededRollText, damageText, hpAfterText, CounterForecastText(target, actor, counter, attackLike),
            followUp ? $"추격: 가능 (민첩 {AgilityValue(actor)} vs {AgilityValue(target)})" : "추격: 불가", costText);
    }

    private string CounterSummary(BattleTestUnit unit)
    {
        if (unit == null)
        {
            return "-";
        }

        return CanUseCounterSpecial(unit) ? unit.definition.specialName : $"공격 R{unit.definition.attackRange}";
    }

    private string CounterForecastText(BattleTestUnit defender, BattleTestUnit attacker, BattleTestCounterMove counter,
                                       bool attackLike)
    {
        if (!attackLike)
        {
            return "상대 반격: 없음";
        }

        if (defender == null || attacker == null || defender.defeated)
        {
            return "상대 반격: 없음";
        }

        if (defender.marked)
        {
            return "상대 반격: 파훼 - 반격 봉쇄";
        }

        if (!defender.CanReact)
        {
            return "상대 반격: 불가 - 반응 소모";
        }

        int distance = GridDistance(defender.cell, attacker.cell);
        if (!counter.valid)
        {
            if (distance > EffectiveAttackRange(defender) && distance > EffectiveSpecialRange(defender))
            {
                return "상대 반격: 불가 - 사거리 밖";
            }

            if (defender.inner < defender.definition.specialCost)
            {
                return "상대 반격: 불가 - 내공 부족";
            }

            if (defender.specialCooldownLeft > 0)
            {
                return $"상대 반격: 불가 - 재사용 대기 {defender.specialCooldownLeft}턴";
            }

            return "상대 반격: 불가";
        }

        BattleTestTile from = TileAt(defender.cell);
        BattleTestTile to = TileAt(attacker.cell);
        int heightBonus = HeightAttackModifier(from, to);
        int attackBonus =
            defender.definition.attackBonus + (counter.special ? defender.definition.specialAttackBonus : 0);
        int defense = DefenseValue(attacker, to);
        int damageMin =
            defender.definition.damageMin + (counter.special ? defender.definition.specialPower : 0) + heightBonus;
        int damageMax =
            defender.definition.damageMax + (counter.special ? defender.definition.specialPower : 0) + heightBonus;
        damageMin = Mathf.Max(1, damageMin);
        damageMax = Mathf.Max(1, damageMax);
        return $"상대 반격: {counter.label} / 명중 {HitChancePercent(defense, attackBonus, heightBonus, 0)}% / 피해 {damageMin}-{damageMax}";
    }

    private string SpecialUnavailableReason(BattleTestUnit unit)
    {
        if (unit == null)
        {
            return "행동할 유닛이 없습니다.";
        }

        if (!unit.CanUseMainAction)
        {
            return "이미 행동했습니다.";
        }

        if (unit.definition.specialEffect == BattleSpecialEffect.None)
        {
            return "장착된 무공이 없습니다.";
        }

        if (unit.inner < unit.definition.specialCost)
        {
            return $"내공 부족 ({unit.inner}/{unit.definition.specialCost}).";
        }

        if (unit.specialCooldownLeft > 0)
        {
            return $"재사용 대기 {unit.specialCooldownLeft}턴.";
        }

        return string.Empty;
    }

    private bool IsHostileAttackSpecial(BattleSpecialEffect effect)
    {
        return effect == BattleSpecialEffect.Strike || effect == BattleSpecialEffect.Poison ||
               effect == BattleSpecialEffect.Freeze || effect == BattleSpecialEffect.BreakGuard;
    }

    private string NeededRollText(int defense, int attackBonus, int heightBonus, int terrainBonus)
    {
        int needed = defense - attackBonus - heightBonus - terrainBonus;
        if (needed <= 2)
        {
            return $"{HitChancePercent(defense, attackBonus, heightBonus, terrainBonus)}% / d20 2+ 필요";
        }

        if (needed > 20)
        {
            return "5% / d20 20 필요";
        }

        return $"{HitChancePercent(defense, attackBonus, heightBonus, terrainBonus)}% / d20 {needed}+ 필요";
    }

    private int HitChancePercent(int defense, int attackBonus, int heightBonus, int terrainBonus)
    {
        int needed = defense - attackBonus - heightBonus - terrainBonus;
        if (needed <= 2)
        {
            return 95;
        }

        if (needed > 20)
        {
            return 5;
        }

        return Mathf.Clamp((21 - needed) * 5, 5, 95);
    }

    private int DefenseValue(BattleTestUnit unit, BattleTestTile tile)
    {
        int defense = unit.definition.defense;
        if (tile != null)
        {
            defense += tile.coverBonus;
        }

        if (unit.guarded)
        {
            defense += 2;
        }

        if (unit.marked)
        {
            defense -= 2;
        }

        return defense;
    }

    private void ApplyTileEntry(BattleTestUnit unit, BattleTestTile tile)
    {
        if (unit == null || tile == null || unit.defeated)
        {
            return;
        }

        if (tile.fireTurns > 0)
        {
            int damage = random.Next(2, 6);
            unit.hp = Mathf.Max(0, unit.hp - damage);
            AddLog($"[지형] {unit.definition.displayName} 화염 진입. 피해 {damage}.");
            if (unit.hp == 0)
            {
                unit.defeated = true;
                unit.view.SetDefeated(true);
                AddLog($"[전투불능] {unit.definition.displayName} 쓰러짐.");
            }
        }
    }

    private void DealTerrainDamage(BattleTestUnit unit, int damage, string reason)
    {
        if (unit == null || unit.defeated)
        {
            return;
        }

        unit.hp = Mathf.Max(0, unit.hp - damage);
        AddLog($"[Terrain] {unit.definition.displayName} takes {damage} from {reason}.");
        if (unit.hp == 0)
        {
            unit.defeated = true;
            unit.view.SetDefeated(true);
            AddLog($"[Terrain] {unit.definition.displayName} defeated by {reason}.");
        }
    }

    private void ApplyPushLandingHazard(BattleTestUnit unit, BattleTestTile tile, string reason)
    {
        if (unit == null || tile == null || unit.defeated)
        {
            return;
        }

        if (tile.hazardType == HazardType.DeepWater || tile.terrain == TerrainType.DeepWater)
        {
            unit.chilled = true;
            DealTerrainDamage(unit, 6, reason + " into deep water");
        }
        else if (tile.hazardType == HazardType.Ice || tile.hazardType == HazardType.Slippery)
        {
            unit.chilled = true;
            AddLog($"[Terrain] {unit.definition.displayName} loses footing on ice.");
        }
    }

    private bool TryPushTarget(BattleTestUnit actor, BattleTestUnit target, int distance, string reason)
    {
        if (actor == null || target == null || target.defeated)
        {
            return false;
        }

        Vector2Int direction = PushDirection(actor.cell, target.cell);
        BattleTestTile currentTile = TileAt(target.cell);
        for (int i = 0; i < Mathf.Max(1, distance); i++)
        {
            Vector2Int nextCell = target.cell + direction;
            BattleTestTile nextTile = TileAt(nextCell);
            if (nextTile == null)
            {
                DealTerrainDamage(target, FallDamage, reason + " over the edge");
                return true;
            }

            if (UnitAt(nextCell) != null)
            {
                DealTerrainDamage(target, 3, reason + " collision");
                return false;
            }

            if (!nextTile.walkable)
            {
                if (nextTile.hazardType == HazardType.DeepWater || nextTile.terrain == TerrainType.DeepWater)
                {
                    target.chilled = true;
                    DealTerrainDamage(target, 6, reason + " into deep water");
                    return true;
                }

                if (IsCliffDropToward(currentTile, direction) || nextTile.hazardType == HazardType.Fall)
                {
                    DealTerrainDamage(target, FallDamage, reason + " off a cliff");
                    return true;
                }

                DealTerrainDamage(target, 3, reason + " into terrain");
                return false;
            }

            target.cell = nextCell;
            if (target.view != null)
            {
                target.view.transform.position = UnitWorldPosition(nextCell);
            }

            ApplyTileEntry(target, nextTile);
            ApplyPushLandingHazard(target, nextTile, reason);
            currentTile = nextTile;
            if (target.defeated)
            {
                return true;
            }
        }

        AddLog($"[Push] {target.definition.displayName} pushed by {reason}.");
        RefreshHighlights();
        RefreshUnits();
        return true;
    }

    private static Vector2Int PushDirection(Vector2Int from, Vector2Int to)
    {
        Vector2Int delta = to - from;
        if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y) && delta.x != 0)
        {
            return new Vector2Int(delta.x > 0 ? 1 : -1, 0);
        }

        if (delta.y != 0)
        {
            return new Vector2Int(0, delta.y > 0 ? 1 : -1);
        }

        return Vector2Int.up;
    }

    private static bool IsCliffDropToward(BattleTestTile tile, Vector2Int direction)
    {
        if (tile == null)
        {
            return false;
        }

        if (direction.x > 0)
        {
            return tile.eastEdge == EdgeType.CliffDrop;
        }

        if (direction.x < 0)
        {
            return tile.westEdge == EdgeType.CliffDrop;
        }

        if (direction.y > 0)
        {
            return tile.northEdge == EdgeType.CliffDrop;
        }

        return tile.southEdge == EdgeType.CliffDrop;
    }

    private void FreezeWaterAround(Vector2Int center)
    {
        int changed = 0;
        foreach (Vector2Int cell in RadiusCells(center, 1))
        {
            BattleTestTile tile = TileAt(cell);
            if (tile == null)
            {
                continue;
            }

            if (tile.terrain != TerrainType.Water && tile.terrain != TerrainType.ShallowWater &&
                tile.terrain != TerrainType.DeepWater && tile.hazardType != HazardType.DeepWater &&
                tile.hazardType != HazardType.Slippery)
            {
                continue;
            }

            tile.terrain = TerrainType.Ice;
            tile.walkable = true;
            tile.moveCost = Mathf.Min(Mathf.Max(2, tile.moveCost), 3);
            tile.hazardType = HazardType.Ice;
            tile.danger = true;
            tile.blocksLineOfSight = false;
            tile.tacticalNote = "Frozen by ice art: walkable, slippery, and vulnerable to knockback.";
            RefreshTerrainTint(tile);
            changed++;
        }

        if (changed > 0)
        {
            AddLog($"[Terrain] Ice art froze {changed} water tiles.");
        }
    }

    private void CreatePoisonSmoke(Vector2Int center)
    {
        int changed = 0;
        foreach (Vector2Int cell in RadiusCells(center, 1))
        {
            BattleTestTile tile = TileAt(cell);
            if (tile == null || !tile.walkable)
            {
                continue;
            }

            tile.smokeTurns = Mathf.Max(tile.smokeTurns, 2);
            tile.hazardType = HazardType.Poison;
            tile.danger = true;
            tile.blocksLineOfSight = true;
            tile.tacticalNote = "Poison mist: blocks line of sight and marks the route as dangerous.";
            RefreshTerrainTint(tile);
            changed++;
        }

        if (changed > 0)
        {
            AddLog($"[Terrain] Poison mist formed on {changed} tiles.");
        }
    }

    private void DamageUnitsAround(BattleTestUnit actor, Vector2Int center, int radius, int damage, string reason)
    {
        foreach (BattleTestUnit unit in units)
        {
            if (unit.defeated || unit.definition.faction == actor.definition.faction)
            {
                continue;
            }

            if (GridDistance(unit.cell, center) > radius)
            {
                continue;
            }

            unit.hp = Mathf.Max(0, unit.hp - damage);
            AddLog($"[지형] {unit.definition.displayName} {reason} 피해 {damage}.");
            if (unit.hp == 0)
            {
                unit.defeated = true;
                unit.view.SetDefeated(true);
                AddLog($"[전투불능] {unit.definition.displayName} 쓰러짐.");
            }
        }
    }

    private void FadeInteractable(BattleTestInteractable interactable)
    {
        if (interactable.renderer != null)
        {
            interactable.renderer.color = new Color(0.35f, 0.35f, 0.35f, 0.72f);
        }

        if (interactable.label != null)
        {
            interactable.label.color = new Color(0.55f, 0.52f, 0.46f, 0.72f);
        }
    }

    private int CollapseBridgeAt(Vector2Int center)
    {
        int changed = 0;
        for (int x = 11; x <= 14; x++)
        {
            BattleTestTile tile = TileAt(new Vector2Int(x, center.y));
            if (tile == null || tile.terrain != TerrainType.Bridge)
            {
                continue;
            }

            tile.terrain = TerrainType.ShallowWater;
            tile.walkable = false;
            tile.moveCost = 99;
            tile.coverBonus = 0;
            tile.baseCoverBonus = 0;
            tile.blocksLineOfSight = false;
            tile.isChokePoint = false;
            tile.danger = true;
            tile.baseColor = new Color(0.12f, 0.22f, 0.26f, 1f);
            tile.tacticalNote = "붕괴한 다리: 우측 우회로 차단, 여울로 우회해야 한다";
            RefreshTerrainTint(tile);
            changed++;
        }

        return changed;
    }

    private int DropBambooAt(Vector2Int center)
    {
        int changed = 0;
        Vector2Int[] cells =
        {
            center,
            new Vector2Int(center.x + 1, center.y),
            new Vector2Int(center.x, center.y + 1),
            new Vector2Int(center.x + 1, center.y + 1)
        };

        foreach (Vector2Int cell in cells)
        {
            BattleTestTile tile = TileAt(cell);
            if (tile == null || !tile.walkable)
            {
                continue;
            }

            tile.terrain = TerrainType.Bamboo;
            tile.moveCost = Mathf.Max(tile.moveCost, 2);
            tile.blocksLineOfSight = true;
            tile.coverBonus = Mathf.Max(tile.coverBonus, tile.baseCoverBonus + CoverInteractBonus);
            tile.extraCover = true;
            tile.tacticalNote = "쓰러진 대나무: 시야 차단과 엄폐를 만든 임시 장벽";
            RefreshTerrainTint(tile);
            changed++;
        }

        return changed;
    }

    private int DropRockAt(Vector2Int center)
    {
        int changed = 0;
        foreach (Vector2Int cell in Neighbors(center))
        {
            BattleTestTile tile = TileAt(cell);
            if (tile == null || !tile.walkable)
            {
                continue;
            }

            tile.terrain = TerrainType.Rubble;
            tile.moveCost = Mathf.Max(tile.moveCost, 2);
            tile.coverBonus = Mathf.Max(tile.coverBonus, tile.baseCoverBonus + CoverInteractBonus);
            tile.extraCover = true;
            tile.blocksLineOfSight = tile.blocksLineOfSight || tile.coverBonus >= 3;
            tile.danger = true;
            tile.baseColor = new Color(0.45f, 0.40f, 0.35f, 1f);
            tile.tacticalNote = "낙석 잔해: 강엄폐, 통행 지연, 하단 진입 억제";
            RefreshTerrainTint(tile);
            changed++;
        }

        return changed;
    }

    private BattleTestInteractable FindUsableInteractable(BattleTestUnit actor, Vector2Int clickedCell)
    {
        foreach (BattleTestInteractable interactable in interactables)
        {
            if (interactable.used || interactable.kind == BattleTestInteractableKind.Objective ||
                interactable.cell != clickedCell)
            {
                continue;
            }

            if (GridDistance(actor.cell, interactable.cell) <= 1)
            {
                return interactable;
            }
        }

        return null;
    }

    private bool HasUsableInteractable(BattleTestUnit actor)
    {
        if (actor == null)
        {
            return false;
        }

        foreach (BattleTestInteractable interactable in interactables)
        {
            if (!interactable.used && interactable.kind != BattleTestInteractableKind.Objective &&
                GridDistance(actor.cell, interactable.cell) <= 1)
            {
                return true;
            }
        }

        return false;
    }

    private BattleTestInteractable GetInteractableAt(Vector2Int cell)
    {
        foreach (BattleTestInteractable interactable in interactables)
        {
            if (!interactable.used && interactable.cell == cell)
            {
                return interactable;
            }
        }

        return null;
    }

    private string TileHazardText(BattleTestTile tile)
    {
        if (tile == null)
        {
            return "-";
        }

        List<string> states = new List<string>();
        if (tile.hazardType != HazardType.None)
        {
            states.Add(tile.hazardType.ToString());
        }

        if (tile.smokeTurns > 0)
        {
            states.Add("연기");
        }

        if (tile.fireTurns > 0)
        {
            states.Add("화염");
        }

        if (tile.extraCover)
        {
            states.Add("중엄폐");
        }

        if (!tile.walkable)
        {
            states.Add("진입불가");
        }

        if (tile.danger)
        {
            states.Add("위험");
        }

        if (tile.isChokePoint)
        {
            states.Add("병목");
        }

        if (tile.blocksLineOfSight)
        {
            states.Add("시야차단");
        }

        return states.Count == 0 ? "없음" : string.Join(", ", states);
    }

    private string InteractableEffectText(BattleTestInteractableKind kind)
    {
        switch (kind)
        {
        case BattleTestInteractableKind.Smoke:
            return $"연기 엄폐 +{SmokeCoverBonus}";
        case BattleTestInteractableKind.Fire:
            return $"화염 피해 {FireInteractDamage}";
        case BattleTestInteractableKind.Cover:
            return $"엄폐 +{CoverInteractBonus}";
        case BattleTestInteractableKind.Objective:
            return "보호 목표";
        case BattleTestInteractableKind.CollapseBridge:
            return "다리 붕괴 / 낙하 피해";
        case BattleTestInteractableKind.BambooFall:
            return "대나무 장벽 생성";
        case BattleTestInteractableKind.Rockfall:
            return $"낙석 피해 {FallDamage} / 잔해";
        default:
            return "-";
        }
    }

    private string InteractableGlyph(BattleTestInteractableKind kind)
    {
        switch (kind)
        {
        case BattleTestInteractableKind.Smoke:
            return "煙";
        case BattleTestInteractableKind.Fire:
            return "火";
        case BattleTestInteractableKind.Cover:
            return "盾";
        case BattleTestInteractableKind.Objective:
            return "守";
        case BattleTestInteractableKind.CollapseBridge:
            return "斷";
        case BattleTestInteractableKind.BambooFall:
            return "竹";
        case BattleTestInteractableKind.Rockfall:
            return "石";
        default:
            return "物";
        }
    }

    private void RefreshTerrainTint(BattleTestTile tile)
    {
        if (tile == null)
        {
            return;
        }

        if (tile.terrainRenderer != null)
        {
            if (tile.fireTurns > 0)
            {
                tile.terrainRenderer.color = new Color(0.72f, 0.20f, 0.12f, 1f);
            }
            else if (tile.smokeTurns > 0)
            {
                tile.terrainRenderer.color = new Color(0.54f, 0.54f, 0.50f, 1f);
            }
            else if (!tile.walkable && tile.danger)
            {
                tile.terrainRenderer.color = new Color(0.12f, 0.22f, 0.26f, 1f);
            }
            else if (tile.extraCover)
            {
                tile.terrainRenderer.color = new Color(0.44f, 0.29f, 0.17f, 1f);
            }
            else
            {
                tile.terrainRenderer.color = tile.baseColor;
            }
        }
        else if (tilemapBattlefield != null)
        {
            tilemapBattlefield.SetTerrainTint(tile.cell, tile.terrain, TerrainTint(tile));
        }
    }

    private Color TerrainTint(BattleTestTile tile)
    {
        if (tile.fireTurns > 0)
        {
            return new Color(0.72f, 0.20f, 0.12f, 1f);
        }

        if (tile.smokeTurns > 0)
        {
            return new Color(0.54f, 0.54f, 0.50f, 1f);
        }

        if (!tile.walkable && tile.danger)
        {
            return new Color(0.12f, 0.22f, 0.26f, 1f);
        }

        if (tile.extraCover)
        {
            return new Color(0.44f, 0.29f, 0.17f, 1f);
        }

        return Color.white;
    }

    private string UnitStatusText(BattleTestUnit unit)
    {
        if (unit.defeated)
        {
            return "전투불능";
        }

        List<string> states = new List<string>();
        if (unit.guarded)
        {
            states.Add("방어");
        }

        if (unit.poisoned)
        {
            states.Add("독");
        }

        if (unit.chilled)
        {
            states.Add("둔화");
        }

        if (unit.marked)
        {
            states.Add("파훼");
        }

        if (unit.moved && unit.acted)
        {
            states.Add("완료");
        }
        else if (unit.moved)
        {
            states.Add("이동함");
        }
        else if (unit.acted)
        {
            states.Add("행동함");
        }

        return states.Count == 0 ? "대기" : string.Join(", ", states);
    }

    private string ActionText(bool spent)
    {
        return spent ? "완료" : "가능";
    }

    private string PhaseLabel(BattlePhase phase)
    {
        switch (phase)
        {
        case BattlePhase.PlayerPhase:
            return "아군 페이즈";
        case BattlePhase.EnemyPhase:
            return "적 페이즈";
        case BattlePhase.NeutralPhase:
            return "중립 페이즈";
        default:
            return phase.ToString();
        }
    }

    private string FactionLabel(Faction faction)
    {
        switch (faction)
        {
        case Faction.Ally:
            return "아군";
        case Faction.Enemy:
            return "적";
        case Faction.Neutral:
            return "중립";
        default:
            return faction.ToString();
        }
    }

    private string CommandLabel(BattleCommandMode mode)
    {
        switch (mode)
        {
        case BattleCommandMode.Move:
            return "이동";
        case BattleCommandMode.Attack:
            return "공격";
        case BattleCommandMode.Skill:
            return "무공";
        case BattleCommandMode.Interact:
            return "지형";
        default:
            return mode.ToString();
        }
    }

    private string TerrainLabel(TerrainType terrain)
    {
        switch (terrain)
        {
        case TerrainType.Stone:
            return "석로";
        case TerrainType.Wood:
            return "목재";
        case TerrainType.Water:
            return "물가";
        case TerrainType.Bamboo:
            return "대나무숲";
        case TerrainType.Bridge:
            return "다리";
        case TerrainType.Roof:
            return "지붕";
        case TerrainType.Cliff:
            return "절벽";
        case TerrainType.Wall:
            return "담장";
        case TerrainType.Plain:
            return "평지";
        case TerrainType.Road:
            return "돌계단";
        case TerrainType.ShrineFloor:
            return "사당 마당";
        case TerrainType.Forest:
            return "숲";
        case TerrainType.ShallowWater:
            return "얕은 여울";
        case TerrainType.DeepWater:
            return "깊은 물";
        case TerrainType.Mud:
            return "진흙";
        case TerrainType.Snow:
            return "눈길";
        case TerrainType.Ice:
            return "빙판";
        case TerrainType.Hill:
            return "능선";
        case TerrainType.Gate:
            return "문";
        case TerrainType.Interior:
            return "실내";
        case TerrainType.Fire:
            return "화염";
        case TerrainType.Smoke:
            return "연막";
        case TerrainType.Trap:
            return "함정";
        case TerrainType.Rubble:
            return "잔해";
        default:
            return terrain.ToString();
        }
    }

    private void SetCommandMode(BattleCommandMode mode)
    {
        if (activeUnit == null || activeUnit.definition.faction != Faction.Ally)
        {
            return;
        }

        if (scoutMode && mode != BattleCommandMode.Move)
        {
            AddLog("[Scout] 정찰 중에는 공격/무공 대신 지형 확인과 배치 변경만 가능합니다.");
            return;
        }

        if (mode == BattleCommandMode.Interact && (activeUnit.acted || !HasUsableInteractable(activeUnit)))
        {
            AddLog("[UI] 사용할 수 있는 지형지물이 없습니다.");
            return;
        }

        commandMode = mode;
        RefreshHighlights();
    }

    private List<BattleTestUnit> GetTurnQueuePreview(int count)
    {
        List<BattleTestUnit> queue = new List<BattleTestUnit>();
        if (units.Count == 0)
        {
            return queue;
        }

        Faction primary = phaseTurn.IsEnemyPhase ? Faction.Enemy : Faction.Ally;
        Faction secondary = phaseTurn.IsEnemyPhase ? Faction.Ally : Faction.Enemy;
        AppendFactionPreview(queue, primary, count);
        AppendFactionPreview(queue, secondary, count);
        return queue;
    }

    private void AppendFactionPreview(List<BattleTestUnit> queue, Faction faction, int count)
    {
        foreach (BattleTestUnit unit in units)
        {
            if (queue.Count >= count)
            {
                return;
            }

            if (!unit.defeated && unit.definition.faction == faction)
            {
                queue.Add(unit);
            }
        }
    }

    private void UpdateHover()
    {
        hoveredUnit = null;
        hoveredTile = null;

        if (Camera.main == null)
        {
            return;
        }

        Vector3 world = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Collider2D[] hits = Physics2D.OverlapPointAll(new Vector2(world.x, world.y));

        foreach (Collider2D hit in hits)
        {
            BattleTestUnitView view = hit.GetComponentInParent<BattleTestUnitView>();
            if (view != null && view.Unit != null && !view.Unit.defeated)
            {
                hoveredUnit = view.Unit;
                break;
            }
        }

        foreach (Collider2D hit in hits)
        {
            BattleTestTile tile = hit.GetComponent<BattleTestTile>();
            if (tile != null)
            {
                hoveredTile = tile;
                break;
            }
        }
    }

    private Dictionary<Vector2Int, int> GetReachableCells(BattleTestUnit unit)
    {
        Dictionary<Vector2Int, int> cost = new Dictionary<Vector2Int, int>();
        List<Vector2Int> frontier = new List<Vector2Int>();
        cost[unit.cell] = 0;
        frontier.Add(unit.cell);

        while (frontier.Count > 0)
        {
            Vector2Int current = PopLowestCost(frontier, cost);
            foreach (Vector2Int next in Neighbors(current))
            {
                BattleTestTile tile = TileAt(next);
                if (tile == null || !tile.walkable)
                {
                    continue;
                }

                BattleTestUnit occupant = UnitAt(next);
                if (occupant != null && occupant != unit)
                {
                    continue;
                }

                int stepCost = StepMoveCost(TileAt(current), tile);
                if (stepCost == int.MaxValue)
                {
                    continue;
                }

                int nextCost = cost[current] + stepCost;
                if (nextCost > EffectiveMoveRange(unit))
                {
                    continue;
                }

                if (cost.TryGetValue(next, out int oldCost) && oldCost <= nextCost)
                {
                    continue;
                }

                cost[next] = nextCost;
                if (!frontier.Contains(next))
                {
                    frontier.Add(next);
                }
            }
        }

        return cost;
    }

    private static Vector2Int PopLowestCost(List<Vector2Int> frontier, Dictionary<Vector2Int, int> cost)
    {
        int bestIndex = 0;
        int bestCost = int.MaxValue;
        for (int i = 0; i < frontier.Count; i++)
        {
            Vector2Int cell = frontier[i];
            int value = cost.TryGetValue(cell, out int c) ? c : int.MaxValue;
            if (value < bestCost)
            {
                bestIndex = i;
                bestCost = value;
            }
        }

        Vector2Int result = frontier[bestIndex];
        frontier.RemoveAt(bestIndex);
        return result;
    }

    private int StepMoveCost(BattleTestTile from, BattleTestTile to)
    {
        if (to == null || !to.walkable)
        {
            return int.MaxValue;
        }

        int elevationDiff = from == null ? 0 : to.elevation - from.elevation;
        if (elevationDiff >= 3)
        {
            return int.MaxValue;
        }

        int cost = Mathf.Max(1, to.moveCost);
        if (elevationDiff > 0)
        {
            cost += elevationDiff;
        }

        if (to.fireTurns > 0)
        {
            cost += 1;
        }

        return cost;
    }

    private IEnumerable<Vector2Int> Neighbors(Vector2Int cell)
    {
        yield return new Vector2Int(cell.x + 1, cell.y);
        yield return new Vector2Int(cell.x - 1, cell.y);
        yield return new Vector2Int(cell.x, cell.y + 1);
        yield return new Vector2Int(cell.x, cell.y - 1);
    }

    private IEnumerable<Vector2Int> RadiusCells(Vector2Int center, int radius)
    {
        for (int y = center.y - radius; y <= center.y + radius; y++)
        {
            for (int x = center.x - radius; x <= center.x + radius; x++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                if (GridDistance(center, cell) <= radius)
                {
                    yield return cell;
                }
            }
        }
    }

    private void RefreshHighlights()
    {
        ClearHighlights();

        if (activeUnit == null || activeUnit.defeated)
        {
            return;
        }

        BattleTestTile activeTile = TileAt(activeUnit.cell);
        if (activeTile != null)
        {
            activeTile.SetHighlight(new Color(1f, 0.72f, 0.16f, 0.44f));
        }

        DrawMapOverlays();

        if (scoutMode)
        {
            HighlightDeploymentCells();
            return;
        }

        if (commandMode == BattleCommandMode.Move && !activeUnit.moved)
        {
            foreach (Vector2Int cell in GetReachableCells(activeUnit).Keys)
            {
                if (cell == activeUnit.cell)
                {
                    continue;
                }

                BattleTestTile tile = TileAt(cell);
                if (tile != null)
                {
                    tile.SetHighlight(new Color(0.24f, 0.56f, 0.92f, 0.28f));
                }
            }
        }

        if (commandMode == BattleCommandMode.Attack && !activeUnit.acted)
        {
            int range = EffectiveAttackRange(activeUnit);
            foreach (BattleTestUnit target in units)
            {
                if (target.defeated || target.definition.faction == activeUnit.definition.faction)
                {
                    continue;
                }

                if (GridDistance(activeUnit.cell, target.cell) <= range &&
                    (range <= 1 || HasLineOfSight(activeUnit.cell, target.cell)))
                {
                    BattleTestTile tile = TileAt(target.cell);
                    if (tile != null)
                    {
                        tile.SetHighlight(new Color(0.95f, 0.18f, 0.14f, 0.36f));
                    }
                }
            }
        }

        if (commandMode == BattleCommandMode.Skill && CanUseSpecial(activeUnit))
        {
            int range = EffectiveSpecialRange(activeUnit);
            foreach (BattleTestUnit target in units)
            {
                if (!IsValidSpecialTarget(activeUnit, target))
                {
                    continue;
                }

                if (GridDistance(activeUnit.cell, target.cell) <= range &&
                    (range <= 1 || !IsHostileAttackSpecial(activeUnit.definition.specialEffect) ||
                     HasLineOfSight(activeUnit.cell, target.cell)))
                {
                    BattleTestTile tile = TileAt(target.cell);
                    if (tile != null)
                    {
                        Color color = activeUnit.definition.specialEffect == BattleSpecialEffect.Heal
                                          ? new Color(0.18f, 0.88f, 0.58f, 0.32f)
                                          : new Color(0.62f, 0.30f, 0.90f, 0.34f);
                        tile.SetHighlight(color);
                    }
                }
            }
        }

        if (commandMode == BattleCommandMode.Interact && !activeUnit.acted)
        {
            foreach (BattleTestInteractable interactable in interactables)
            {
                if (interactable.used || GridDistance(activeUnit.cell, interactable.cell) > 1)
                {
                    continue;
                }

                BattleTestTile tile = TileAt(interactable.cell);
                if (tile != null)
                {
                    tile.SetHighlight(new Color(1f, 0.62f, 0.16f, 0.36f));
                }
            }
        }
    }

    private void HighlightDeploymentCells()
    {
        if (tiles == null)
        {
            return;
        }

        foreach (BattleTestTile tile in tiles)
        {
            if (tile != null && IsDeploymentCell(tile.cell) && tile.walkable && UnitAt(tile.cell) == null)
            {
                tile.SetHighlight(new Color(0.16f, 0.66f, 0.94f, 0.30f));
            }
        }
    }

    private void DrawMapOverlays()
    {
        if (tiles == null)
        {
            return;
        }

        foreach (BattleTestTile tile in tiles)
        {
            if (tile == null)
            {
                continue;
            }

            if (showThreatOverlay && IsInEnemyThreat(tile.cell))
            {
                tile.SetHighlight(new Color(0.70f, 0.08f, 0.08f, 0.20f));
            }

            if (showElevationOverlay && tile.elevation > 0)
            {
                float alpha = Mathf.Clamp01(0.12f + tile.elevation * 0.065f);
                tile.SetHighlight(new Color(1f, 0.80f, 0.20f, alpha));
            }

            if (showCoverOverlay && tile.coverBonus > 0)
            {
                tile.SetHighlight(new Color(0.24f, 0.62f, 0.46f, 0.24f));
            }

            if (showSightOverlay && tile.blocksLineOfSight)
            {
                tile.SetHighlight(new Color(0.42f, 0.36f, 0.28f, 0.28f));
            }

            if (showObjectiveOverlay && tile.objective)
            {
                tile.SetHighlight(new Color(1f, 0.80f, 0.14f, 0.34f));
            }

            if (tile.danger && !showObjectiveOverlay)
            {
                tile.SetHighlight(new Color(0.86f, 0.16f, 0.08f, 0.20f));
            }
        }
    }

    private bool IsInEnemyThreat(Vector2Int cell)
    {
        foreach (BattleTestUnit unit in units)
        {
            if (unit.defeated || unit.definition.faction != Faction.Enemy)
            {
                continue;
            }

            int range = Mathf.Max(EffectiveAttackRange(unit),
                                  CanUseCounterSpecial(unit) ? EffectiveSpecialRange(unit) : EffectiveAttackRange(unit));
            if (GridDistance(unit.cell, cell) <= range && (range <= 1 || HasLineOfSight(unit.cell, cell)))
            {
                return true;
            }
        }

        return false;
    }

    private void ClearHighlights()
    {
        if (tiles == null)
        {
            return;
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                tiles[x, y].SetHighlight(Color.clear);
            }
        }
    }

    private void RefreshTileNameVisibility()
    {
        if (tiles != null)
        {
            foreach (BattleTestTile tile in tiles)
            {
                if (tile == null || tile.nameLabel == null)
                {
                    continue;
                }

                bool visible = showTerrainNames || (showObjectiveOverlay && tile.objective) ||
                               (showElevationOverlay && tile.elevation >= 2) ||
                               (showSightOverlay && tile.blocksLineOfSight) ||
                               (showCoverOverlay && tile.coverBonus > 0);
                tile.nameLabel.gameObject.SetActive(visible);
            }
        }

        foreach (BattleTestInteractable interactable in interactables)
        {
            if (interactable.label != null)
            {
                interactable.label.gameObject.SetActive(showTerrainNames || showObjectiveOverlay);
            }
        }
    }

    private void RefreshUnits()
    {
        foreach (BattleTestUnit unit in units)
        {
            unit.view.Refresh(unit == activeUnit);
        }
    }

    private bool CheckBattleEnd()
    {
        bool alliesAlive = false;
        bool enemiesAlive = false;
        bool objectiveBreached = false;

        foreach (BattleTestUnit unit in units)
        {
            if (unit.defeated)
            {
                continue;
            }

            if (unit.definition.faction == Faction.Ally)
            {
                alliesAlive = true;
            }
            else if (unit.definition.faction == Faction.Enemy)
            {
                enemiesAlive = true;
                BattleTestTile tile = TileAt(unit.cell);
                if (tile != null && tile.objective)
                {
                    objectiveBreached = true;
                }
            }
        }

        if (objectiveBreached)
        {
            battleOver = true;
            ClearHighlights();
            AddLog("[전투 종료] 패배. 철랑문이 백두천광 현판까지 돌파했다.");
            return true;
        }

        if (alliesAlive && enemiesAlive)
        {
            return false;
        }

        battleOver = true;
        ClearHighlights();
        AddLog(alliesAlive ? "[전투 종료] 승리." : "[전투 종료] 패배.");
        return true;
    }

    private BattleTestUnit UnitAt(Vector2Int cell)
    {
        foreach (BattleTestUnit unit in units)
        {
            if (!unit.defeated && unit.cell == cell)
            {
                return unit;
            }
        }

        return null;
    }

    private BattleTestTile TileAt(Vector2Int cell)
    {
        return IsInside(cell) ? tiles[cell.x, cell.y] : null;
    }

    private bool IsInside(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < width && cell.y >= 0 && cell.y < height;
    }

    private int GridDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private bool HasLineOfSight(Vector2Int fromCell, Vector2Int toCell)
    {
        if (!IsInside(fromCell) || !IsInside(toCell))
        {
            return false;
        }

        if (GridDistance(fromCell, toCell) <= 1)
        {
            return true;
        }

        BattleTestTile source = TileAt(fromCell);
        int sourceElevation = source == null ? 0 : source.elevation;
        foreach (Vector2Int cell in CellsOnLine(fromCell, toCell))
        {
            if (cell == fromCell || cell == toCell)
            {
                continue;
            }

            BattleTestTile tile = TileAt(cell);
            if (tile == null)
            {
                return false;
            }

            if (tile.smokeTurns > 0)
            {
                return false;
            }

            if (!tile.blocksLineOfSight)
            {
                continue;
            }

            if (sourceElevation >= tile.elevation + 2)
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private IEnumerable<Vector2Int> CellsOnLine(Vector2Int fromCell, Vector2Int toCell)
    {
        int x0 = fromCell.x;
        int y0 = fromCell.y;
        int x1 = toCell.x;
        int y1 = toCell.y;
        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            yield return new Vector2Int(x0, y0);

            if (x0 == x1 && y0 == y1)
            {
                yield break;
            }

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }

            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }

    private Vector3 GridToWorld(Vector2Int cell)
    {
        float x = (cell.x - cell.y) * tileWidth * 0.5f;
        float y = (cell.x + cell.y) * tileHeight * 0.5f;
        return new Vector3(x, y, 0f);
    }

    private Vector3 UnitWorldPosition(Vector2Int cell)
    {
        Vector3 position = GridToWorld(cell);
        position.y += 0.18f;
        return position;
    }

    private IEnumerator PlayMapIntro()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            yield break;
        }

        busy = true;
        Vector3 startPosition = camera.transform.position;
        float startSize = camera.orthographicSize;

        yield return PanCamera(camera, GridToWorld(new Vector2Int(7, 5)), Mathf.Max(3.6f, startSize * 0.72f), 0.55f);
        yield return PanCamera(camera, GridToWorld(new Vector2Int(12, 8)), Mathf.Max(3.2f, startSize * 0.64f), 0.48f);
        yield return PanCamera(camera, GridToWorld(new Vector2Int(7, 10)), Mathf.Max(3.3f, startSize * 0.66f), 0.48f);
        yield return PanCamera(camera, startPosition, startSize, 0.62f);

        busy = false;
        mapIntroCoroutine = null;
        RefreshHighlights();
    }

    private IEnumerator PanCamera(Camera camera, Vector3 targetWorld, float targetSize, float duration)
    {
        Vector3 fromPosition = camera.transform.position;
        Vector3 toPosition = new Vector3(targetWorld.x, targetWorld.y, fromPosition.z);
        float fromSize = camera.orthographicSize;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);
            camera.transform.position = Vector3.Lerp(fromPosition, toPosition, t);
            camera.orthographicSize = Mathf.Lerp(fromSize, targetSize, t);
            yield return null;
        }

        camera.transform.position = toPosition;
        camera.orthographicSize = targetSize;
        yield return new WaitForSeconds(0.12f);
    }

    private TerrainProfile ResolveTerrain(int x, int y)
    {
        return ResolveBaekduSnowGateTerrain(x, y);
    }

    private TerrainProfile ResolveBaekduSnowGateTerrain(int x, int y)
    {
        if (x <= 3 && y >= 4 && y <= 10)
        {
            bool choke = (x == 2 && (y == 6 || y == 7)) || (x == 1 && y == 5);
            int elevation = y >= 8 ? 1 : 0;
            return new TerrainProfile(TerrainType.Bamboo, new Color(0.15f, 0.38f, 0.22f, 1f), elevation, 2, 2,
                                      true, true, choke, false, false, "left_bamboo_flank",
                                      "Left bamboo forest flank: slow, cover-rich, blocks line of sight.");
        }

        if (y == 5 && x >= 0 && x <= 15)
        {
            if (x >= 6 && x <= 8)
            {
                return new TerrainProfile(TerrainType.Bridge, new Color(0.46f, 0.29f, 0.14f, 1f), 1, 0, 1, true,
                                          false, true, false, false, "central_bridge",
                                          "Central bridge bottleneck over the frozen Snow Gate stream.");
            }

            if ((x >= 1 && x <= 3) || (x >= 12 && x <= 14))
            {
                return new TerrainProfile(TerrainType.ShallowWater, new Color(0.20f, 0.48f, 0.56f, 1f), 0, 0, 3,
                                          true, false, false, false, true, "shallow_ford",
                                          "Shallow ford: slow river crossing with exposure risk.");
            }

            return new TerrainProfile(TerrainType.DeepWater, new Color(0.08f, 0.22f, 0.31f, 1f), 0, 0, 99, false,
                                      false, false, false, true, "yalu_river",
                                      "Deep frozen channel: impassable water and fall hazard.");
        }

        if ((x == 5 || x == 9) && y >= 4 && y <= 7)
        {
            return new TerrainProfile(TerrainType.Cliff, new Color(0.24f, 0.23f, 0.20f, 1f), 2, 0, 99, false,
                                      true, false, false, true, "bridge_cliff",
                                      "Bridge-side cliff drop: blocks path and sight.");
        }

        if (x == 7 && y == 9)
        {
            return new TerrainProfile(TerrainType.Smoke, new Color(0.44f, 0.52f, 0.48f, 1f), 2, 2, 2, true, true,
                                      false, false, true, "ruined_shrine_altar",
                                      "Incense smoke hazard: blocks sight and grants temporary cover around the altar.");
        }

        if (x >= 11 && x <= 15 && y >= 6 && y <= 10)
        {
            bool fallEdge = (x == 11 && y >= 6 && y <= 8) || (x == 14 && y == 7);
            bool beacon = x == 12 && y == 8;
            return new TerrainProfile(beacon ? TerrainType.Gate : TerrainType.Hill,
                                      beacon ? new Color(0.58f, 0.47f, 0.28f, 1f)
                                             : new Color(0.42f, 0.42f, 0.30f, 1f),
                                      3, 1, 2, true, false, fallEdge, beacon, fallEdge, "right_cliff_highground",
                                      beacon
                                          ? "Beacon high ground objective: long sightline and warm light pool."
                                          : "Right cliff high ground: strong ranged angle with fall edges.");
        }

        if (x >= 6 && x <= 9 && y >= 8 && y <= 10)
        {
            bool objective = (x == 7 || x == 8) && y == 10;
            return new TerrainProfile(TerrainType.ShrineFloor, new Color(0.64f, 0.58f, 0.46f, 1f), 2, 1, 1, true,
                                      false, false, objective, false, "ruined_shrine_altar",
                                      objective
                                          ? "Ruined shrine altar objective behind the gate."
                                          : "Ruined shrine flagstones: defensible raised ground.");
        }

        if (x >= 4 && x <= 10 && y == 4)
        {
            return new TerrainProfile(TerrainType.Road, new Color(0.54f, 0.48f, 0.36f, 1f), 0, 0, 1, true,
                                      false, x >= 6 && x <= 8, false, false, "south_gate_road",
                                      "Southern approach road feeding into the bridge choke.");
        }

        if (x == 6 && y == 2)
        {
            return new TerrainProfile(TerrainType.Fire, new Color(0.84f, 0.28f, 0.12f, 1f), 0, 0, 2, true, false,
                                      false, false, true, "enemy_approach",
                                      "Lantern fire hazard: a controlled flame pocket near the southern approach.");
        }

        if (x >= 4 && x <= 10 && y >= 6 && y <= 8)
        {
            bool rubble = (x == 9 && y == 7) || (x == 10 && y == 7);
            return new TerrainProfile(rubble ? TerrainType.Rubble : TerrainType.Stone,
                                      rubble ? new Color(0.44f, 0.39f, 0.33f, 1f)
                                             : new Color(0.50f, 0.46f, 0.36f, 1f),
                                      rubble ? 1 : 2, rubble ? 4 : 0, rubble ? 2 : 1, true, rubble, rubble, false,
                                      rubble, "gate_courtyard",
                                      rubble
                                          ? "Collapsed wall cover near the gate, blocks some sight."
                                          : "Gate courtyard: raised stone lanes into the shrine.");
        }

        if (x >= 4 && x <= 10 && y <= 3)
        {
            bool cartLane = (x == 4 || x == 5) && y == 3;
            return new TerrainProfile(cartLane ? TerrainType.Mud : TerrainType.Plain,
                                      cartLane ? new Color(0.34f, 0.27f, 0.18f, 1f)
                                               : new Color(0.39f, 0.43f, 0.30f, 1f),
                                      0, cartLane ? 1 : 0, cartLane ? 2 : 1, true, false, x == 7 && y == 3,
                                      false, false, "enemy_approach",
                                      "Enemy approach: open ground leading to carts, lanterns, and the bridge.");
        }

        if (x >= 13 && x <= 14 && y >= 1 && y <= 2)
        {
            return new TerrainProfile(TerrainType.Ice, new Color(0.62f, 0.78f, 0.86f, 1f), 0, 0, 2, true, false,
                                      false, false, true, "right_shoal",
                                      "Frozen shoal: slippery alternate crossing beneath the cliff.");
        }

        if (x >= 12 && y <= 4)
        {
            return new TerrainProfile(TerrainType.ShallowWater, new Color(0.18f, 0.42f, 0.50f, 1f), 0, 0, 3, true,
                                      false, false, false, true, "right_shoal",
                                      "Right shoal: slow alternate crossing beneath the cliff.");
        }

        if (x <= 1 && y <= 3)
        {
            return new TerrainProfile(TerrainType.Forest, new Color(0.18f, 0.34f, 0.20f, 1f), 0, 1, 2, true, true,
                                      false, false, false, "riverbank_forest",
                                      "Riverbank trees: light cover and sight disruption.");
        }

        if (y >= 10)
        {
            return new TerrainProfile(TerrainType.Wall, new Color(0.22f, 0.20f, 0.18f, 1f), 2, 0, 99, false, true,
                                      false, false, true, "north_gate_wall",
                                      "Northern gate wall and ruined palisade.");
        }

        return new TerrainProfile(TerrainType.Stone, new Color(0.47f, 0.43f, 0.34f, 1f), 1, 0, 1, true, false,
                                  false, false, false, "canyon_floor",
                                  "Snow Gate courtyard floor: standard tactical ground around the Baekdu pass.");
    }

    private TerrainProfile ResolveLegacyShrineTerrain(int x, int y)
    {
        if (x == 7 && y >= 2 && y <= 8)
        {
            int elevation = y >= 7 ? 2 : y >= 4 ? 1 : 0;
            return new TerrainProfile(TerrainType.Road, new Color(0.58f, 0.54f, 0.43f, 1f), elevation, 0, 1, true,
                                      false, y >= 4 && y <= 7, false, false, "center_stair",
                                      "중앙 돌계단: 가장 빠르지만 한 명씩 막히는 병목");
        }

        if ((x == 6 || x == 8) && y >= 4 && y <= 7)
        {
            return new TerrainProfile(TerrainType.Cliff, new Color(0.26f, 0.24f, 0.21f, 1f), 1, 0, 99, false,
                                      true, false, false, true, "center_cliff", "계단을 감싸는 절벽 - 통행 불가");
        }

        if (x >= 5 && x <= 9 && y >= 8 && y <= 10)
        {
            bool objective = y == 10 && x >= 6 && x <= 8;
            return new TerrainProfile(TerrainType.ShrineFloor, new Color(0.66f, 0.61f, 0.50f, 1f), 2, 1, 1, true,
                                      false, false, objective, false, "shrine_high",
                                      objective ? "현판 보호 목표. 적이 닿으면 패배 위험" : "폐사당 고지: 원거리와 방어에 유리");
        }

        if (x >= 10 && x <= 13 && y >= 8 && y <= 10)
        {
            return new TerrainProfile(TerrainType.Roof, new Color(0.60f, 0.24f, 0.18f, 1f), 3, 1, 1, true,
                                      false, x == 10 && y == 8, false, x >= 12, "roof_route",
                                      "누각 지붕: 고저 3, 원거리 사거리와 시야에 유리");
        }

        if (x <= 4 && y >= 3 && y <= 10)
        {
            bool choke = (x == 3 && (y == 6 || y == 7)) || (x == 1 && y == 5);
            return new TerrainProfile(TerrainType.Bamboo, new Color(0.18f, 0.42f, 0.25f, 1f), y >= 8 ? 1 : 0, 2, 2,
                                      true, true, choke, false, false, "bamboo_flank",
                                      "대나무숲 샛길: 이동 비용 2, 시야 차단, 은신/암기 유리");
        }

        if (x >= 11 && x <= 14 && y >= 3 && y <= 6)
        {
            if (y == 5)
            {
                return new TerrainProfile(TerrainType.Bridge, new Color(0.46f, 0.28f, 0.13f, 1f), 1, 0, 1, true,
                                          false, true, false, true, "right_bridge",
                                          "낡은 나무다리: 1칸 폭 우회로, 붕괴 가능");
            }

            return new TerrainProfile(TerrainType.ShallowWater, new Color(0.20f, 0.43f, 0.52f, 1f), 0, 0, 3, true,
                                      false, false, false, true, "stream", "얕은 여울: 이동 비용 3, 빙공 연계 가능");
        }

        if ((x == 10 && y >= 4 && y <= 7) || (x == 9 && y == 7))
        {
            return new TerrainProfile(TerrainType.Rubble, new Color(0.45f, 0.40f, 0.35f, 1f), 1, 4, 2, true, true,
                                      x == 9 && y == 7, false, false, "broken_wall",
                                      "무너진 담장: 강엄폐와 부분 시야 차단");
        }

        if (x >= 11 && x <= 13 && y >= 6 && y <= 8)
        {
            return new TerrainProfile(TerrainType.Hill, new Color(0.45f, 0.45f, 0.31f, 1f), 2, 1, 2, true, false,
                                      false, false, false, "right_ridge", "우측 능선: 다리 우회 뒤 고지 진입로");
        }

        if (x >= 5 && x <= 9 && y <= 3)
        {
            bool choke = x == 7 && y == 3;
            return new TerrainProfile(TerrainType.Road, new Color(0.53f, 0.48f, 0.37f, 1f), 0, 0, 1, true, false,
                                      choke, false, false, "approach", "철랑문 진입로: 중앙 병목으로 이어진다");
        }

        if ((x == 5 && y >= 5 && y <= 7) || (x == 9 && y >= 4 && y <= 6) || (x >= 5 && x <= 9 && y == 11))
        {
            return new TerrainProfile(TerrainType.Wall, new Color(0.24f, 0.22f, 0.19f, 1f), 2, 0, 99, false, true,
                                      false, false, true, "shrine_wall", "폐사당 담장/절벽 - 이동과 시야 차단");
        }

        if (y <= 2)
        {
            return new TerrainProfile(TerrainType.Plain, new Color(0.42f, 0.45f, 0.32f, 1f), 0, 0, 1, true, false,
                                      false, false, false, "enemy_entry", "하단 진입로: 적 증원이 들어오는 열린 지대");
        }

        return new TerrainProfile(TerrainType.Stone, new Color(0.48f, 0.45f, 0.36f, 1f), 1, 0, 1, true, false,
                                  false, false, false, "courtyard", "폐사당 고개 마당");
    }

    private void CenterCamera()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            return;
        }

        Vector3 min = GridToWorld(Vector2Int.zero);
        Vector3 max = GridToWorld(new Vector2Int(width - 1, height - 1));
        Vector3 left = GridToWorld(new Vector2Int(0, height - 1));
        Vector3 right = GridToWorld(new Vector2Int(width - 1, 0));
        Vector3 center = (min + max + left + right) * 0.25f;
        camera.transform.position = new Vector3(center.x, center.y + 0.45f, -10f);
        camera.orthographic = true;
        camera.orthographicSize = Mathf.Max(4.15f, height * 0.48f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.20f, 0.30f, 0.27f, 1f);
    }

    private bool PointerOverHud(Vector3 screenPosition)
    {
        if (battleHud != null && battleHud.PointerOverHud(screenPosition))
        {
            return true;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return true;
        }

        return false;
    }

    private void EnsureMapVisualSprites()
    {
        diamondSprite = diamondSprite == null ? CreateDiamondSprite() : diamondSprite;
        softDiamondSprite = softDiamondSprite == null ? CreateSoftDiamondSprite() : softDiamondSprite;
        detailSprite = detailSprite == null ? CreateDetailSprite() : detailSprite;
        dotSprite = dotSprite == null ? CreateDotSprite() : dotSprite;
        mountainRidgeSprite = mountainRidgeSprite == null ? CreateMountainRidgeSprite() : mountainRidgeSprite;
        pineSilhouetteSprite = pineSilhouetteSprite == null ? CreatePineSilhouetteSprite() : pineSilhouetteSprite;
        LoadMapAssetSprites();
    }

    private void LoadMapAssetSprites()
    {
        if (mapAssetSpritesLoaded)
        {
            return;
        }

        mapAssetSpritesLoaded = true;
        terrainAssetSprites[TerrainType.Plain] = LoadMapSprite("Tiles/plain_moss");
        terrainAssetSprites[TerrainType.Hill] = LoadMapSprite("Tiles/hill_moss");
        terrainAssetSprites[TerrainType.Stone] = LoadMapSprite("Tiles/stone_courtyard");
        terrainAssetSprites[TerrainType.Road] = LoadMapSprite("Tiles/road_stair");
        terrainAssetSprites[TerrainType.ShrineFloor] = LoadMapSprite("Tiles/shrine_floor");
        terrainAssetSprites[TerrainType.Bamboo] = LoadMapSprite("Tiles/bamboo_floor");
        terrainAssetSprites[TerrainType.Forest] = LoadMapSprite("Tiles/forest_floor");
        terrainAssetSprites[TerrainType.ShallowWater] = LoadMapSprite("Tiles/shallow_water");
        terrainAssetSprites[TerrainType.DeepWater] = LoadMapSprite("Tiles/deep_water");
        terrainAssetSprites[TerrainType.Water] = terrainAssetSprites[TerrainType.ShallowWater];
        terrainAssetSprites[TerrainType.Wood] = LoadMapSprite("Tiles/wood_plank");
        terrainAssetSprites[TerrainType.Bridge] = LoadMapSprite("Tiles/wood_bridge");
        terrainAssetSprites[TerrainType.Roof] = LoadMapSprite("Tiles/roof_tile");
        terrainAssetSprites[TerrainType.Cliff] = LoadMapSprite("Tiles/cliff_face");
        terrainAssetSprites[TerrainType.Wall] = LoadMapSprite("Tiles/wall_broken");
        terrainAssetSprites[TerrainType.Rubble] = LoadMapSprite("Tiles/rubble");
        terrainAssetSprites[TerrainType.Mud] = LoadMapSprite("Tiles/mud_path");
        terrainAssetSprites[TerrainType.Snow] = LoadMapSprite("Tiles/snow_edge");
        terrainAssetSprites[TerrainType.Ice] = LoadMapSprite("Tiles/ice_slick");
        terrainAssetSprites[TerrainType.Gate] = LoadMapSprite("Tiles/gate_threshold");
        terrainAssetSprites[TerrainType.Interior] = terrainAssetSprites[TerrainType.ShrineFloor];
        terrainAssetSprites[TerrainType.Fire] = LoadMapSprite("Tiles/fire_scorch");
        terrainAssetSprites[TerrainType.Smoke] = LoadMapSprite("Tiles/smoke_veil");
        terrainAssetSprites[TerrainType.Trap] = LoadMapSprite("Tiles/trap_mark");

        interactableAssetSprites["signboard"] = LoadMapSprite("Objects/sect_signboard");
        interactableAssetSprites["incense"] = LoadMapSprite("Objects/incense_burner");
        interactableAssetSprites["lantern"] = LoadMapSprite("Objects/red_lantern");
        interactableAssetSprites["oil_jar"] = LoadMapSprite("Objects/oil_jar");
        interactableAssetSprites["wine_cart"] = LoadMapSprite("Objects/wine_cart");
        interactableAssetSprites["fallen_wall"] = LoadMapSprite("Objects/fallen_wall");
        interactableAssetSprites["bridge_rope"] = LoadMapSprite("Objects/bridge_rope");
        interactableAssetSprites["bamboo_bundle"] = LoadMapSprite("Objects/bamboo_bundle");
        interactableAssetSprites["stone_lantern"] = LoadMapSprite("Objects/stone_lantern");
        interactableAssetSprites["snow_pine"] = LoadMapSprite("Objects/baekdu_snow_pine");
        interactableAssetSprites["frozen_boulder"] = LoadMapSprite("Objects/baekdu_snow_boulder");
        interactableAssetSprites["fire"] = LoadMapSprite("Objects/flame_pillar");
        interactableAssetSprites["smoke"] = LoadMapSprite("Objects/smoke_wisp");
        interactableAssetSprites["rockfall"] = LoadMapSprite("Objects/falling_boulder");
    }

    private Sprite LoadMapSprite(string relativePath)
    {
        return Resources.Load<Sprite>("MapAssets/" + relativePath);
    }

    private Sprite GetTerrainSprite(TerrainType terrain)
    {
        return terrainAssetSprites.TryGetValue(terrain, out Sprite sprite) && sprite != null ? sprite : diamondSprite;
    }

    private Sprite GetInteractableSprite(string id, BattleTestInteractableKind kind)
    {
        if (!string.IsNullOrEmpty(id) && interactableAssetSprites.TryGetValue(id, out Sprite sprite) && sprite != null)
        {
            return sprite;
        }

        switch (kind)
        {
        case BattleTestInteractableKind.Fire:
            return interactableAssetSprites.TryGetValue("fire", out sprite) ? sprite : null;
        case BattleTestInteractableKind.Smoke:
            return interactableAssetSprites.TryGetValue("smoke", out sprite) ? sprite : null;
        case BattleTestInteractableKind.Rockfall:
            return interactableAssetSprites.TryGetValue("rockfall", out sprite) ? sprite : null;
        default:
            return null;
        }
    }

    private Sprite CreateDiamondSprite()
    {
        const int textureWidth = 96;
        const int textureHeight = 48;
        Texture2D texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        texture.name = "BattleTestDiamond";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < textureHeight; y++)
        {
            for (int x = 0; x < textureWidth; x++)
            {
                float nx = Mathf.Abs(((x + 0.5f) / textureWidth * 2f) - 1f);
                float ny = Mathf.Abs(((y + 0.5f) / textureHeight * 2f) - 1f);
                float d = nx + ny;
                if (d <= 1f)
                {
                    float edge = d > 0.90f ? Mathf.Lerp(1f, 0.88f, Mathf.InverseLerp(0.90f, 1f, d)) : 1f;
                    texture.SetPixel(x, y, new Color(edge, edge, edge, 0.98f));
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, textureWidth, textureHeight), new Vector2(0.5f, 0.5f), 96f);
    }

    private Sprite CreateSoftDiamondSprite()
    {
        const int textureWidth = 96;
        const int textureHeight = 48;
        Texture2D texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        texture.name = "BattleTestSoftDiamond";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < textureHeight; y++)
        {
            for (int x = 0; x < textureWidth; x++)
            {
                float nx = Mathf.Abs(((x + 0.5f) / textureWidth * 2f) - 1f);
                float ny = Mathf.Abs(((y + 0.5f) / textureHeight * 2f) - 1f);
                float d = nx + ny;
                float alpha = d <= 1f ? Mathf.Clamp01(1f - Mathf.InverseLerp(0.76f, 1f, d) * 0.55f) : 0f;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, textureWidth, textureHeight), new Vector2(0.5f, 0.5f), 96f);
    }

    private Sprite CreateDetailSprite()
    {
        const int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "BattleTestBrush";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = Mathf.Abs(((x + 0.5f) / size * 2f) - 1f);
                float ny = Mathf.Abs(((y + 0.5f) / size * 2f) - 1f);
                float alpha = Mathf.Clamp01(1f - Mathf.Max(nx, ny));
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 96f);
    }

    private Sprite CreateDotSprite()
    {
        const int size = 32;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "BattleTestDot";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = ((x + 0.5f) / size * 2f) - 1f;
                float ny = ((y + 0.5f) / size * 2f) - 1f;
                float distance = Mathf.Sqrt(nx * nx + ny * ny);
                float alpha = Mathf.Clamp01(1f - Mathf.InverseLerp(0.50f, 1f, distance));
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 96f);
    }

    private Sprite CreateMountainRidgeSprite()
    {
        const int textureWidth = 512;
        const int textureHeight = 160;
        Texture2D texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        texture.name = "BattleTestMountainRidge";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < textureHeight; y++)
        {
            float fy = (y + 0.5f) / textureHeight;
            for (int x = 0; x < textureWidth; x++)
            {
                float fx = (x + 0.5f) / textureWidth;
                float farRidge = 0.54f + Mathf.Sin(fx * 9.1f + 0.6f) * 0.10f +
                                 Mathf.Sin(fx * 22.0f) * 0.045f;
                float nearRidge = 0.36f + Mathf.Sin(fx * 6.2f + 1.8f) * 0.13f +
                                  Mathf.Sin(fx * 18.0f + 0.4f) * 0.040f;
                Color color = Color.clear;

                if (fy < nearRidge)
                {
                    float fade = Mathf.Clamp01(fy / Mathf.Max(0.01f, nearRidge));
                    color = new Color(1f, 1f, 1f, Mathf.Lerp(0.82f, 0.30f, fade));
                }
                else if (fy < farRidge)
                {
                    float fade = Mathf.Clamp01(fy / Mathf.Max(0.01f, farRidge));
                    color = new Color(1f, 1f, 1f, Mathf.Lerp(0.42f, 0.12f, fade));
                }

                float snowLine = nearRidge - 0.028f;
                if (fy > snowLine && fy < nearRidge + 0.010f)
                {
                    color = Color.Lerp(color, new Color(1f, 1f, 1f, 0.62f), 0.45f);
                }

                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, textureWidth, textureHeight), new Vector2(0.5f, 0.5f), 96f);
    }

    private Sprite CreatePineSilhouetteSprite()
    {
        const int textureWidth = 96;
        const int textureHeight = 192;
        Texture2D texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        texture.name = "BattleTestPineSilhouette";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < textureHeight; y++)
        {
            float fy = (y + 0.5f) / textureHeight;
            for (int x = 0; x < textureWidth; x++)
            {
                float nx = (((x + 0.5f) / textureWidth) * 2f) - 1f;
                bool trunk = Mathf.Abs(nx) < 0.055f && fy < 0.80f;
                bool needles = false;

                for (int tier = 0; tier < 5; tier++)
                {
                    float tip = 0.96f - (tier * 0.135f);
                    float baseY = tip - 0.255f;
                    if (fy < baseY || fy > tip)
                    {
                        continue;
                    }

                    float t = (tip - fy) / Mathf.Max(0.01f, tip - baseY);
                    float halfWidth = (0.19f + tier * 0.085f) * Mathf.Clamp01(t);
                    needles |= Mathf.Abs(nx) < halfWidth;
                }

                float alpha = trunk || needles ? Mathf.Clamp01(0.72f - fy * 0.18f) : 0f;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, textureWidth, textureHeight), new Vector2(0.5f, 0f), 96f);
    }

    private void EnsureGuiStyles()
    {
        if (panelStyle != null)
        {
            return;
        }

        panelStyle = new GUIStyle(GUI.skin.box);
        panelStyle.normal.background = MakeTexture(new Color(0.08f, 0.07f, 0.055f, 0.84f));
        panelStyle.border = new RectOffset(6, 6, 6, 6);

        labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.normal.textColor = new Color(0.96f, 0.88f, 0.72f, 1f);
        labelStyle.fontSize = 16;
        labelStyle.fontStyle = FontStyle.Bold;

        titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.normal.textColor = new Color(1f, 0.89f, 0.58f, 1f);
        titleStyle.fontSize = 18;
        titleStyle.fontStyle = FontStyle.Bold;

        smallStyle = new GUIStyle(GUI.skin.label);
        smallStyle.normal.textColor = new Color(0.82f, 0.78f, 0.68f, 1f);
        smallStyle.fontSize = 13;
        smallStyle.wordWrap = true;

        logStyle = new GUIStyle(GUI.skin.label);
        logStyle.normal.textColor = new Color(0.86f, 0.82f, 0.74f, 1f);
        logStyle.fontSize = 13;
        logStyle.wordWrap = true;
    }

    private Texture2D MakeTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    private string YesNo(bool value)
    {
        return value ? "Yes" : "No";
    }

    private void AddLog(string message)
    {
        battleLog.Add(message);
        if (battleLog.Count > 24)
        {
            battleLog.RemoveAt(0);
        }

        Debug.Log("[BattleTest] " + message);
    }

    private readonly struct BattleForecast
    {
        public static BattleForecast Invalid(string reason)
        {
            return new BattleForecast(false, reason, string.Empty, string.Empty, string.Empty, 0, 0, string.Empty, 0, 0,
                                      0, 0, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
                                      string.Empty);
        }

        public static BattleForecast Invalid(string actorName, string targetName, string commandName, string reason,
                                             int distance, int range, string costText)
        {
            return new BattleForecast(false, reason, actorName, targetName, commandName, distance, range, "invalid", 0,
                                      0, 0, 0, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
                                      costText);
        }

        public readonly bool valid;
        public readonly string invalidReason;
        public readonly string actorName;
        public readonly string targetName;
        public readonly string commandName;
        public readonly int distance;
        public readonly int range;
        public readonly string rangeText;
        public readonly int attackBonus;
        public readonly int heightBonus;
        public readonly int terrainBonus;
        public readonly int defense;
        public readonly string neededRollText;
        public readonly string damageText;
        public readonly string hpAfterText;
        public readonly string counterText;
        public readonly string followUpText;
        public readonly string costText;

        public BattleForecast(bool valid, string invalidReason, string actorName, string targetName, string commandName,
                              int distance, int range, string rangeText, int attackBonus, int heightBonus,
                              int terrainBonus, int defense, string neededRollText, string damageText,
                              string hpAfterText, string counterText, string followUpText, string costText)
        {
            this.valid = valid;
            this.invalidReason = invalidReason;
            this.actorName = actorName;
            this.targetName = targetName;
            this.commandName = commandName;
            this.distance = distance;
            this.range = range;
            this.rangeText = rangeText;
            this.attackBonus = attackBonus;
            this.heightBonus = heightBonus;
            this.terrainBonus = terrainBonus;
            this.defense = defense;
            this.neededRollText = neededRollText;
            this.damageText = damageText;
            this.hpAfterText = hpAfterText;
            this.counterText = counterText;
            this.followUpText = followUpText;
            this.costText = costText;
        }
    }

    private readonly struct BattleTestCounterMove
    {
        public static readonly BattleTestCounterMove None = new BattleTestCounterMove(false, false, string.Empty);

        public readonly bool valid;
        public readonly bool special;
        public readonly string label;

        public BattleTestCounterMove(bool special, string label) : this(true, special, label)
        {
        }

        private BattleTestCounterMove(bool valid, bool special, string label)
        {
            this.valid = valid;
            this.special = special;
            this.label = label;
        }
    }

    private readonly struct TerrainProfile
    {
        public readonly TerrainType terrain;
        public readonly Color color;
        public readonly int elevation;
        public readonly int coverBonus;
        public readonly int moveCost;
        public readonly bool walkable;
        public readonly bool blocksLineOfSight;
        public readonly bool isChokePoint;
        public readonly bool objective;
        public readonly bool danger;
        public readonly string laneId;
        public readonly string tacticalNote;

        public TerrainProfile(TerrainType terrain, Color color, int elevation, int coverBonus, int moveCost,
                              bool walkable, bool blocksLineOfSight = false, bool isChokePoint = false,
                              bool objective = false, bool danger = false, string laneId = "",
                              string tacticalNote = "")
        {
            this.terrain = terrain;
            this.color = color;
            this.elevation = elevation;
            this.coverBonus = coverBonus;
            this.moveCost = moveCost;
            this.walkable = walkable;
            this.blocksLineOfSight = blocksLineOfSight;
            this.isChokePoint = isChokePoint;
            this.objective = objective;
            this.danger = danger;
            this.laneId = laneId;
            this.tacticalNote = tacticalNote;
        }
    }
}

[Serializable]
public sealed class BattleTestUnitDefinition
{
    public string id;
    public string displayName;
    public Faction faction;
    public CharacterVisualData visual;
    public Vector2Int startCell;
    public string sectName;
    public int age;
    public string mbti;
    public string elementName;
    public string weaponName;
    public string speechTone;
    public int maxHp = 30;
    public int maxInner = 3;
    public int initiative = 10;
    public int agility = -1;
    public int moveRange = 4;
    public int attackRange = 1;
    public int attackBonus = 5;
    public int defense = 14;
    public int damageMin = 5;
    public int damageMax = 9;
    public string specialName = "Special";
    public int specialRange = 1;
    public int specialCost = 1;
    public int specialCooldown = 2;
    public int specialPower = 4;
    public int specialAttackBonus = 1;
    public BattleSpecialEffect specialEffect = BattleSpecialEffect.Strike;
}

public enum BattleCommandMode
{
    Move,
    Attack,
    Skill,
    Interact
}

public enum BattleSpecialEffect
{
    None,
    Strike,
    Heal,
    Poison,
    Freeze,
    Mark,
    BreakGuard
}

public enum BattleTestInteractableKind
{
    Smoke,
    Fire,
    Cover,
    Objective,
    CollapseBridge,
    BambooFall,
    Rockfall
}

public sealed class BattleTestInteractable
{
    public readonly string id;
    public readonly string displayName;
    public readonly BattleTestInteractableKind kind;
    public readonly Vector2Int cell;
    public bool used;
    public SpriteRenderer renderer;
    public TextMesh label;

    public BattleTestInteractable(string id, string displayName, BattleTestInteractableKind kind, Vector2Int cell)
    {
        this.id = id;
        this.displayName = displayName;
        this.kind = kind;
        this.cell = cell;
    }
}

public sealed class BattleMapAmbientMotion : MonoBehaviour
{
    public Vector3 drift = Vector3.zero;
    public float speed = 0.35f;
    public float alphaPulse = 0.10f;
    public float scalePulse = 0.02f;

    private Vector3 origin;
    private Vector3 baseScale;
    private SpriteRenderer spriteRenderer;
    private Color baseColor;
    private float phase;

    private void Awake()
    {
        origin = transform.localPosition;
        baseScale = transform.localScale;
        spriteRenderer = GetComponent<SpriteRenderer>();
        baseColor = spriteRenderer == null ? Color.white : spriteRenderer.color;
        phase = (transform.position.x * 12.9898f) + (transform.position.y * 78.233f);
    }

    private void Update()
    {
        float wave = Mathf.Sin((Time.time * Mathf.Max(0.01f, speed)) + phase);
        transform.localPosition = origin + (drift * wave);
        transform.localScale = baseScale * Mathf.Max(0.01f, 1f + (wave * scalePulse));

        if (spriteRenderer == null)
        {
            return;
        }

        Color color = baseColor;
        color.a = Mathf.Clamp01(baseColor.a * (1f + (wave * alphaPulse)));
        spriteRenderer.color = color;
    }
}

public sealed class BattleTestTile : MonoBehaviour
{
    public Vector2Int cell;
    public TerrainType terrain;
    public int elevation;
    public bool walkable = true;
    public int moveCost = 1;
    public int coverBonus;
    public int baseCoverBonus;
    public Color baseColor = Color.white;
    public int smokeTurns;
    public int fireTurns;
    public bool extraCover;
    public bool blocksLineOfSight;
    public bool isChokePoint;
    public bool objective;
    public bool danger;
    public HazardType hazardType;
    public EdgeType northEdge;
    public EdgeType eastEdge;
    public EdgeType southEdge;
    public EdgeType westEdge;
    public string laneId;
    public string tacticalNote;
    public TextMesh nameLabel;
    public SpriteRenderer terrainRenderer;
    public SpriteRenderer highlightRenderer;
    public BattleTilemapBattlefield tilemapBattlefield;

    public void SetHighlight(Color color)
    {
        if (tilemapBattlefield != null)
        {
            tilemapBattlefield.SetHighlight(cell, color);
            return;
        }

        if (highlightRenderer != null)
        {
            highlightRenderer.color = color;
        }
    }
}

public sealed class BattleTestUnitView : MonoBehaviour
{
    private TextMesh label;
    private Transform turnMarkerRoot;
    private TextMesh turnMarkerText;
    private SpriteRenderer turnMarkerPlate;
    private SpriteRenderer turnMarkerArrow;
    private SpriteRenderer turnMarkerHalo;
    private SpriteRenderer turnGroundRing;
    private CharacterVisualController visualController;
    private Vector3 turnMarkerBasePosition;
    private readonly Vector3 groundRingBaseScale = new Vector3(1.56f, 0.46f, 1f);
    private static Sprite turnMarkerPlateSprite;
    private static Sprite turnMarkerArrowSprite;

    public BattleTestUnit Unit { get; private set; }

    public void Bind(BattleTestUnit unit, CharacterVisualController controller)
    {
        Unit = unit;
        visualController = controller;
        label = CreateLabel();
        CreateTurnMarker();
        Refresh(false);
    }

    private void LateUpdate()
    {
        if (turnMarkerRoot == null || !turnMarkerRoot.gameObject.activeSelf)
        {
            return;
        }

        float wave = Mathf.Sin(Time.time * 5.4f);
        turnMarkerRoot.localPosition = turnMarkerBasePosition + new Vector3(0f, wave * 0.035f, 0f);
        turnMarkerRoot.localScale = Vector3.one * (1f + wave * 0.035f);

        if (turnMarkerHalo != null)
        {
            Color color = turnMarkerHalo.color;
            color.a = 0.24f + Mathf.Abs(wave) * 0.18f;
            turnMarkerHalo.color = color;
        }

        if (turnGroundRing != null && turnGroundRing.gameObject.activeSelf)
        {
            turnGroundRing.transform.localScale = groundRingBaseScale * (1f + Mathf.Abs(wave) * 0.055f);
            Color color = turnGroundRing.color;
            color.a = 0.18f + Mathf.Abs(wave) * 0.16f;
            turnGroundRing.color = color;
        }
    }

    public void Refresh(bool selected)
    {
        if (visualController != null)
        {
            visualController.SetSelected(selected);
        }

        if (label != null && Unit != null)
        {
            label.text = $"{Unit.definition.displayName}\n체력 {Unit.hp}/{Unit.definition.maxHp}";
            label.color = Unit.definition.faction == Faction.Ally
                              ? new Color(0.80f, 0.90f, 1f, Unit.defeated ? 0.45f : 1f)
                              : new Color(1f, 0.72f, 0.68f, Unit.defeated ? 0.45f : 1f);
        }

        RefreshTurnMarker(selected);
    }

    public void SetDefeated(bool defeated)
    {
        if (visualController != null)
        {
            visualController.SetDefeated(defeated);
        }

        transform.localScale = defeated ? new Vector3(0.88f, 0.88f, 1f) : Vector3.one;
        Refresh(false);
    }

    private void CreateTurnMarker()
    {
        GameObject root = new GameObject("Current Turn Marker");
        root.transform.SetParent(transform, false);
        turnMarkerRoot = root.transform;

        turnMarkerHalo = CreateMarkerSprite("Turn Halo", GetTurnMarkerPlateSprite(), new Vector3(0f, -0.012f, 0.02f),
                                            new Vector3(1.92f, 0.68f, 1f),
                                            new Color(1f, 0.74f, 0.16f, 0.32f), 6098);
        turnMarkerPlate =
            CreateMarkerSprite("Turn Plate", GetTurnMarkerPlateSprite(), Vector3.zero, new Vector3(1.56f, 0.46f, 1f),
                               new Color(0.95f, 0.72f, 0.16f, 0.96f), 6100);
        turnMarkerArrow = CreateMarkerSprite("Turn Arrow", GetTurnMarkerArrowSprite(), new Vector3(0f, -0.20f, 0f),
                                             new Vector3(0.48f, 0.34f, 1f),
                                             new Color(0.95f, 0.72f, 0.16f, 0.96f), 6101);

        GameObject ringObject = new GameObject("Current Turn Ground Ring");
        ringObject.transform.SetParent(transform, false);
        ringObject.transform.localPosition = new Vector3(0f, -0.30f, 0.03f);
        ringObject.transform.localScale = groundRingBaseScale;
        turnGroundRing = ringObject.AddComponent<SpriteRenderer>();
        turnGroundRing.sprite = GetTurnMarkerPlateSprite();
        turnGroundRing.color = new Color(1f, 0.72f, 0.16f, 0.24f);
        turnGroundRing.sortingLayerName = "Default";
        turnGroundRing.sortingOrder = 4098;
        turnGroundRing.gameObject.SetActive(false);

        GameObject textObject = new GameObject("Turn Marker Text");
        textObject.transform.SetParent(turnMarkerRoot, false);
        textObject.transform.localPosition = new Vector3(0f, -0.005f, -0.02f);
        turnMarkerText = textObject.AddComponent<TextMesh>();
        turnMarkerText.anchor = TextAnchor.MiddleCenter;
        turnMarkerText.alignment = TextAlignment.Center;
        turnMarkerText.fontSize = 44;
        turnMarkerText.characterSize = 0.014f;
        turnMarkerText.color = new Color(0.11f, 0.075f, 0.03f, 1f);
        ApplyWorldTextFont(turnMarkerText);

        MeshRenderer textRenderer = textObject.GetComponent<MeshRenderer>();
        textRenderer.sortingLayerName = "Default";
        textRenderer.sortingOrder = 6102;
        turnMarkerRoot.gameObject.SetActive(false);
    }

    private SpriteRenderer CreateMarkerSprite(string name, Sprite sprite, Vector3 localPosition, Vector3 localScale,
                                              Color color, int sortingOrder)
    {
        GameObject markerObject = new GameObject(name);
        markerObject.transform.SetParent(turnMarkerRoot, false);
        markerObject.transform.localPosition = localPosition;
        markerObject.transform.localScale = localScale;

        SpriteRenderer renderer = markerObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }

    private void RefreshTurnMarker(bool selected)
    {
        if (turnMarkerRoot == null || Unit == null)
        {
            return;
        }

        bool visible = selected && !Unit.defeated;
        turnMarkerRoot.gameObject.SetActive(visible);
        if (turnGroundRing != null)
        {
            turnGroundRing.gameObject.SetActive(visible);
        }

        if (!visible)
        {
            return;
        }

        CharacterVisualData visual = Unit.definition.visual;
        float markerY = visual == null ? 1.74f : visual.spriteOffset.y + Mathf.Max(1.00f, visual.heightInTiles) + 0.58f;
        turnMarkerBasePosition = new Vector3(0f, markerY, -0.09f);
        turnMarkerRoot.localPosition = turnMarkerBasePosition;

        bool enemy = Unit.definition.faction == Faction.Enemy;
        Color plate = enemy ? new Color(0.92f, 0.20f, 0.16f, 0.98f) : new Color(0.98f, 0.68f, 0.12f, 0.98f);
        Color halo = plate;
        halo.a = enemy ? 0.30f : 0.34f;

        turnMarkerPlate.color = plate;
        turnMarkerArrow.color = plate;
        turnMarkerHalo.color = halo;
        if (turnGroundRing != null)
        {
            Color ring = plate;
            ring.a = enemy ? 0.24f : 0.28f;
            turnGroundRing.color = ring;
        }

        turnMarkerText.text = enemy ? "적 턴" : "현재 턴";
        turnMarkerText.color = enemy ? new Color(1f, 0.95f, 0.86f, 1f) : new Color(0.12f, 0.075f, 0.025f, 1f);
    }

    private TextMesh CreateLabel()
    {
        GameObject labelObject = new GameObject("Unit Label");
        labelObject.transform.SetParent(transform, false);
        labelObject.transform.localPosition = new Vector3(0f, -0.42f, -0.04f);

        TextMesh mesh = labelObject.AddComponent<TextMesh>();
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.alignment = TextAlignment.Center;
        mesh.fontSize = 42;
        mesh.characterSize = 0.016f;
        ApplyWorldTextFont(mesh);

        MeshRenderer renderer = labelObject.GetComponent<MeshRenderer>();
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = 5000;
        return mesh;
    }

    private static void ApplyWorldTextFont(TextMesh mesh)
    {
        UiTheme.EnsureStyles();
        if (UiTheme.Font == null)
        {
            return;
        }

        mesh.font = UiTheme.Font;
        MeshRenderer renderer = mesh.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = UiTheme.Font.material;
        }
    }

    private static Sprite GetTurnMarkerPlateSprite()
    {
        if (turnMarkerPlateSprite != null)
        {
            return turnMarkerPlateSprite;
        }

        const int textureWidth = 128;
        const int textureHeight = 40;
        Texture2D texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        texture.name = "BattleTestTurnMarkerPlate";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Vector2 center = new Vector2(textureWidth * 0.5f, textureHeight * 0.5f);
        for (int y = 0; y < textureHeight; y++)
        {
            for (int x = 0; x < textureWidth; x++)
            {
                float nx = Mathf.Abs((x + 0.5f - center.x) / center.x);
                float ny = Mathf.Abs((y + 0.5f - center.y) / center.y);
                float rounded = Mathf.Pow(nx, 4.0f) + Mathf.Pow(ny, 4.0f);
                float alpha = Mathf.Clamp01((1.08f - rounded) * 8f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        turnMarkerPlateSprite = Sprite.Create(texture, new Rect(0f, 0f, textureWidth, textureHeight),
                                              new Vector2(0.5f, 0.5f), 96f);
        turnMarkerPlateSprite.name = "BattleTestTurnMarkerPlate";
        return turnMarkerPlateSprite;
    }

    private static Sprite GetTurnMarkerArrowSprite()
    {
        if (turnMarkerArrowSprite != null)
        {
            return turnMarkerArrowSprite;
        }

        const int textureWidth = 48;
        const int textureHeight = 32;
        Texture2D texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        texture.name = "BattleTestTurnMarkerArrow";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < textureHeight; y++)
        {
            float fy = (y + 0.5f) / textureHeight;
            for (int x = 0; x < textureWidth; x++)
            {
                float nx = Mathf.Abs((((x + 0.5f) / textureWidth) * 2f) - 1f);
                float halfWidth = 1f - fy;
                float alpha = nx <= halfWidth ? Mathf.Clamp01((halfWidth - nx) * 12f) : 0f;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        turnMarkerArrowSprite = Sprite.Create(texture, new Rect(0f, 0f, textureWidth, textureHeight),
                                              new Vector2(0.5f, 1f), 96f);
        turnMarkerArrowSprite.name = "BattleTestTurnMarkerArrow";
        return turnMarkerArrowSprite;
    }
}

public sealed class BattleTestUnit
{
    public readonly BattleTestUnitDefinition definition;
    public readonly BattleTestUnitView view;
    public readonly ActionEconomy actions = new ActionEconomy();
    public Vector2Int cell;
    public int hp;
    public int inner;
    public int initiative;
    public int specialCooldownLeft;
    public bool moved;
    public bool acted;
    public bool defeated;
    public bool guarded;
    public bool poisoned;
    public bool chilled;
    public bool marked;

    public BattleTestUnit(BattleTestUnitDefinition definition, BattleTestUnitView view)
    {
        this.definition = definition;
        this.view = view;
        cell = definition.startCell;
        hp = definition.maxHp;
        inner = definition.maxInner;
        ResetActions(definition.moveRange);
    }

    public bool CanMove => !moved && actions.movementLeft > 0;
    public bool CanUseMainAction => !acted && actions.CanSpend(ActionSlot.Main);
    public bool CanReact => actions.CanSpend(ActionSlot.Reaction);

    public void ResetActions(int movement)
    {
        actions.ResetForTurn(Mathf.Max(0, movement));
        moved = false;
        acted = false;
    }

    public void SpendMovement(int cost)
    {
        moved = true;
        actions.movementLeft = Mathf.Max(0, actions.movementLeft - Mathf.Max(0, cost));
    }

    public void SpendMainAction()
    {
        acted = true;
        actions.Spend(ActionSlot.Main);
    }

    public void SpendReaction()
    {
        actions.Spend(ActionSlot.Reaction);
    }
}
}
