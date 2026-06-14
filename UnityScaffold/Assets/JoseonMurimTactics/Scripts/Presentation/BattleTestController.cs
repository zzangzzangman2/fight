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
public sealed partial class BattleTestController : MonoBehaviour
{
    public int width = 16;
    public int height = 12;
    public float tileWidth = 1.16f;
    public float tileHeight = 0.62f;
    public bool useAuthoredSceneMap = true;
    public bool useTilemapBattlefield = true;
    public bool useLegacyDiamondTerrain;
    public bool useCanvasHud = true;
    public BattleTestMapVariant mapVariant = BattleTestMapVariant.BaekduSnowGate;
    [Header("Visual Upgrade V1")]
    public BattleVisualProfile battleVisualProfile;
    public BattleVfxLibrary battleVfxLibrary;
    public BattleUiSkinData battleUiSkin;
    public BattleTestUnitDefinition[] unitDefinitions = new BattleTestUnitDefinition[0];
    private BattleTestUnitDefinition[] baselineUnitDefinitions;

    private const int SmokeCoverBonus = 2;
    private const int CoverInteractBonus = 2;
    private const int FireInteractDamage = 4;
    private const int FallDamage = 10;
    private const int HighGroundRangeBonusElevation = 2;
    private const int DefaultTurnLimit = 12;
    private const int DefaultPoisonTurns = 2;
    private const int DefaultChilledTurns = 2;
    private const string RequiredHeroUnitId = "park_sungjun";
    private const float EnemyPhaseStartDelay = 0.15f;
    private const float EnemyFocusSeconds = 0.12f;
    private const float EnemyFocusSettleDelay = 0.12f;
    private const float EnemyPostMoveDelay = 0.08f;
    private const float EnemyBetweenUnitDelay = 0.15f;
    private const string SnowGatePaintedBattleMapResource = "MapAssets/Backgrounds/baekdu_snow_gate_srpg_ground";
    private const string SnowfieldPaintedBattleMapResource = "MapAssets/Backgrounds/baekdu_mountain_snowfield_srpg_ground";
    private const string BanditLairPaintedBattleMapResource = "MapAssets/Backgrounds/sobaek_bandit_lair_srpg_ground";
    private const string WolfPassPaintedBattleMapResource = "MapAssets/Backgrounds/sobaek_wolf_pass_srpg_ground";
    private const string TigerRavinePaintedBattleMapResource = "MapAssets/Backgrounds/sobaek_tiger_ravine_srpg_ground";
    private const string LeopardCliffPaintedBattleMapResource = "MapAssets/Backgrounds/sobaek_leopard_cliff_srpg_ground";
    private const string SnowGateMapDisplayName = "백두산 설문 관문전";
    private const string SnowfieldMapDisplayName = "백두산 천지 설산로";
    private const string BanditLairMapDisplayName = "소백촌 도적 소굴";
    private const string WolfPassMapDisplayName = "소백촌 늑대 고개";
    private const string TigerRavineMapDisplayName = "백호 바위골";
    private const string LeopardCliffMapDisplayName = "표범 절벽길";
    private const string SeorakPassRescueMapDisplayName = "설운령 약초 수레 호위전";
    private const string SnowGateMapConcept =
        "중앙 1칸 협로, 좌측 설죽림 우회로, 우측 절벽 고지, 얼어붙은 여울과 붕괴 가능한 다리 밧줄을 쓰는 대표 수작업 전장";
    private const string SnowfieldMapConcept =
        "설송림, 현무암 절벽, 얼음 물길, 온천 증기를 따라 움직이는 백두산 설산 SRPG 전장";
    private const string BanditLairMapConcept =
        "벌목길, 폐광 동굴, 통나무 장애물, 진흙 웅덩이, 덫, 망루 고지로 구성된 자유시간 반복 의뢰 전장";
    private const string WolfPassMapConcept =
        "개울 병목, 쓰러진 통나무, 자작나무 숲, H2 능선과 늑대 굴 바위로 구성된 야수 방어 전장";
    private const string TigerRavineMapConcept =
        "억새 엄폐, 막힌 바위벽, 낙석 협곡, H3 바위 선반으로 주민을 구조하는 산군 토벌 전장";
    private const string LeopardCliffMapConcept =
        "낭떠러지, 대나무 덤불, 밧줄다리, H3 약초 선반으로 매복을 읽는 표범 호송 전장";
    private const string SeorakPassRescueMapConcept =
        "설운령 산길, 약초 수레, 피난민, 밧줄다리 병목과 대나무 덤불을 활용하는 백련 첫 합류 호위전";
    private static readonly bool UseLegacyOnGui = false;
    private static readonly Vector2Int[] FrontDescentAllyStartCells =
    {
        new Vector2Int(6, 8),
        new Vector2Int(7, 8),
        new Vector2Int(6, 7),
        new Vector2Int(7, 7),
        new Vector2Int(8, 7),
        new Vector2Int(9, 7)
    };
    private static readonly Vector2Int[] BanditFrontDescentAllyStartCells =
    {
        new Vector2Int(8, 7),
        new Vector2Int(10, 7),
        new Vector2Int(11, 7),
        new Vector2Int(12, 7),
        new Vector2Int(8, 8),
        new Vector2Int(8, 9)
    };
    private static readonly Vector2Int[] WolfFrontDescentAllyStartCells =
    {
        new Vector2Int(5, 8),
        new Vector2Int(6, 8),
        new Vector2Int(7, 8),
        new Vector2Int(8, 8),
        new Vector2Int(7, 7),
        new Vector2Int(8, 7)
    };
    private static readonly Vector2Int[] TigerFrontDescentAllyStartCells =
    {
        new Vector2Int(8, 8),
        new Vector2Int(9, 8),
        new Vector2Int(10, 8),
        new Vector2Int(8, 7),
        new Vector2Int(9, 7),
        new Vector2Int(10, 7)
    };
    private static readonly Vector2Int[] LeopardFrontDescentAllyStartCells =
    {
        new Vector2Int(8, 8),
        new Vector2Int(9, 8),
        new Vector2Int(10, 8),
        new Vector2Int(8, 7),
        new Vector2Int(8, 9),
        new Vector2Int(9, 9)
    };
    private static readonly Vector2Int[] FrontDescentEnemyStartCells =
    {
        new Vector2Int(5, 1),
        new Vector2Int(6, 1),
        new Vector2Int(7, 1),
        new Vector2Int(8, 1),
        new Vector2Int(9, 1),
        new Vector2Int(10, 1)
    };
    private static readonly Vector2Int[] SnowGateAscentAllyStartCells =
    {
        new Vector2Int(4, 0),
        new Vector2Int(5, 0),
        new Vector2Int(6, 0),
        new Vector2Int(7, 0),
        new Vector2Int(4, 1),
        new Vector2Int(5, 1)
    };
    private static readonly Vector2Int[] SnowGateAscentEnemyStartCells =
    {
        new Vector2Int(7, 2),
        new Vector2Int(8, 2),
        new Vector2Int(9, 2),
        new Vector2Int(7, 3),
        new Vector2Int(8, 3),
        new Vector2Int(9, 3)
    };
    private const float TacticalCameraMinSize = 3.05f;
    private const float TacticalCameraMaxSize = 3.45f;
    private const float CameraFocusYOffset = 0.22f;
    private string PaintedBattleMapResource
    {
        get
        {
            switch (mapVariant)
            {
            case BattleTestMapVariant.BaekduMountainSnowfield:
                return SnowfieldPaintedBattleMapResource;
            case BattleTestMapVariant.BanditLair:
                return BanditLairPaintedBattleMapResource;
            case BattleTestMapVariant.WolfPass:
                return WolfPassPaintedBattleMapResource;
            case BattleTestMapVariant.TigerRavine:
                return TigerRavinePaintedBattleMapResource;
            case BattleTestMapVariant.LeopardCliff:
                return LeopardCliffPaintedBattleMapResource;
            case BattleTestMapVariant.SeorakPassRescue:
                return string.Empty;
            default:
                return SnowGatePaintedBattleMapResource;
            }
        }
    }

    private string MapDisplayName
    {
        get
        {
            switch (mapVariant)
            {
            case BattleTestMapVariant.BaekduMountainSnowfield:
                return SnowfieldMapDisplayName;
            case BattleTestMapVariant.BanditLair:
                return BanditLairMapDisplayName;
            case BattleTestMapVariant.WolfPass:
                return WolfPassMapDisplayName;
            case BattleTestMapVariant.TigerRavine:
                return TigerRavineMapDisplayName;
            case BattleTestMapVariant.LeopardCliff:
                return LeopardCliffMapDisplayName;
            case BattleTestMapVariant.SeorakPassRescue:
                return SeorakPassRescueMapDisplayName;
            default:
                return SnowGateMapDisplayName;
            }
        }
    }

    private string MapConcept
    {
        get
        {
            switch (mapVariant)
            {
            case BattleTestMapVariant.BaekduMountainSnowfield:
                return SnowfieldMapConcept;
            case BattleTestMapVariant.BanditLair:
                return BanditLairMapConcept;
            case BattleTestMapVariant.WolfPass:
                return WolfPassMapConcept;
            case BattleTestMapVariant.TigerRavine:
                return TigerRavineMapConcept;
            case BattleTestMapVariant.LeopardCliff:
                return LeopardCliffMapConcept;
            case BattleTestMapVariant.SeorakPassRescue:
                return SeorakPassRescueMapConcept;
            default:
                return SnowGateMapConcept;
            }
        }
    }

    private readonly List<BattleTestUnit> units = new List<BattleTestUnit>();
    private readonly List<string> battleLog = new List<string>();
    private readonly List<BattleTestInteractable> interactables = new List<BattleTestInteractable>();
    private readonly PhaseTurnController phaseTurn = new PhaseTurnController();
    private readonly HashSet<Vector2Int> enemyThreatCells = new HashSet<Vector2Int>();
    private bool authoredMapBound;
    private System.Random random = new System.Random(20260608);
    private BattleTestTile[,] tiles;
    private Sprite diamondSprite;
    private Sprite softDiamondSprite;
    private Sprite detailSprite;
    private Sprite dotSprite;
    private Sprite mountainRidgeSprite;
    private Sprite pineSilhouetteSprite;
    private Sprite paintedMapBackdropSprite;
    private static Sprite battleIntegrationOverlaySprite;
    private BattleTilemapBattlefield tilemapBattlefield;
    private Coroutine mapIntroCoroutine;
    private Coroutine cameraPanCoroutine;
    private bool mapAssetSpritesLoaded;
    private BattleMapData activeBattleMapData;
    private string activeBattleMapDataSource = string.Empty;
    private int activeBattleMapDataCellCount;
    private bool suppressCameraFocus;
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
    private BattleCameraFx battleCameraFx;
    private DamagePopupPresenter damagePopupPresenter;
    private BattleImpactPresenter battleImpactPresenter;
    private BattleTestUnit activeUnit;
    private BattleTestUnit hoveredUnit;
    private BattleTestTile hoveredTile;
    private BattleTestUnit inspectedUnit;
    private BattleTestTile inspectedTile;
    private Vector3 inspectedScreenPosition;
    private int round = 1;
    private bool busy;
    private bool aiQueued;
    private bool battleOver;
    private bool showThreatOverlay;
    private bool showElevationOverlay;
    private bool showCoverOverlay;
    private bool showSightOverlay;
    private bool showObjectiveOverlay;
    private bool showTerrainNames;
    private bool showHudLog;
    private bool scoutMode;
    private string hudNotice;
    private float hudNoticeUntil;
    private BattleCommandMode commandMode = BattleCommandMode.Move;
    private MovementUndoState pendingMovementUndo;
    private bool runtimeMapEditorForcedMap;
    private BattleTestMapVariant runtimeMapEditorVariant = BattleTestMapVariant.BaekduSnowGate;
    private readonly Dictionary<Vector2Int, BattleMapRuntimeCellEdit> runtimeMapEditOverrides =
        new Dictionary<Vector2Int, BattleMapRuntimeCellEdit>();

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

        BattleMapRuntimeEditorOverlay mapEditor = GetComponent<BattleMapRuntimeEditorOverlay>();
        if (mapEditor != null && mapEditor.IsEditing)
        {
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


        if (!scoutMode && phaseTurn.IsPlayerPhase && activeUnit != null && HandleCombatMotionDebugKeys())
        {
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

        if (activeUnit != null && (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1)))
        {
            if (TryUndoPendingMove(activeUnit))
            {
                return;
            }

            if (!Input.GetKeyDown(KeyCode.Escape))
            {
                return;
            }

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

    private bool HandleCombatMotionDebugKeys()
    {
        if (activeUnit == null || activeUnit.defeated || activeUnit.view == null)
        {
            return false;
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            BattleTestTile tile = FindDebugMoveTile(activeUnit);
            if (tile != null)
            {
                TryMove(activeUnit, tile);
            }
            else
            {
                AddLog("[MotionTest] No reachable move tile.");
            }

            return true;
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            BattleTestUnit target = FindNearestEnemy(activeUnit);
            if (target != null)
            {
                SetCommandMode(BattleCommandMode.Attack);
                if (!TryAttack(activeUnit, target, false))
                {
                    StartCoroutine(RunMotionDebugAttackSequence(activeUnit, target, false));
                }
            }
            else
            {
                AddLog("[MotionTest] No attack target.");
            }

            return true;
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            BattleTestUnit target = FindNearestEnemy(activeUnit);
            if (target != null)
            {
                SetCommandMode(BattleCommandMode.Skill);
                if (!TrySpecial(activeUnit, target))
                {
                    StartCoroutine(RunMotionDebugAttackSequence(activeUnit, target, true));
                }
            }
            else
            {
                AddLog("[MotionTest] No skill target.");
            }

            return true;
        }

        if (Input.GetKeyDown(KeyCode.H))
        {
            activeUnit.view.PlayHit();
            AddLog("[MotionTest] Hit reaction.");
            return true;
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            activeUnit.guarded = true;
            activeUnit.view.PlayGuard();
            AddLog("[MotionTest] Guard pose.");
            return true;
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            activeUnit.hp = 0;
            activeUnit.defeated = true;
            activeUnit.view.SetDefeated(true);
            RefreshUnits();
            AddLog("[MotionTest] Defeat pose.");
            return true;
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            activeUnit.view.PlayVictory();
            AddLog("[MotionTest] Victory pose.");
            return true;
        }

        return false;
    }
    private void LateUpdate()
    {
        RefreshTileNameVisibility();
        DestroyLegacyCanvasHud();
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

        GUI.enabled = playerTurn && activeUnit != null && activeUnit.CanUseMainAction;
        if (GUI.Button(new Rect(112f, 192f, 70f, 28f), "공격"))
        {
            SetCommandMode(BattleCommandMode.Attack);
        }

        GUI.enabled = playerTurn && activeUnit != null && CanUseSpecial(activeUnit);
        if (GUI.Button(new Rect(190f, 192f, 70f, 28f), "무공"))
        {
            SetCommandMode(BattleCommandMode.Skill);
        }

        GUI.enabled = playerTurn && activeUnit != null && CanGuard(activeUnit);
        if (GUI.Button(new Rect(268f, 192f, 70f, 28f), "방어"))
        {
            GuardActiveUnit();
        }

        GUI.enabled = playerTurn && activeUnit != null && CanUseTerrainCommand(activeUnit);
        if (GUI.Button(new Rect(34f, 230f, 92f, 30f), "지형"))
        {
            SetCommandMode(BattleCommandMode.Interact);
        }

        GUI.enabled = playerTurn;
        if (GUI.Button(new Rect(134f, 230f, 92f, 30f), "대기"))
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
            CreateHudButton(active, "대기", new Rect(118f, 218f, 104f, 32f), "대기", EndTurn);
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
        image.color = new Color(0.045f, 0.052f, 0.062f, 0.86f);
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
        uiText.color = bold ? new Color(0.90f, 0.96f, 1f, 1f) : new Color(0.78f, 0.84f, 0.90f, 1f);
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
        image.color = new Color(0.075f, 0.095f, 0.115f, 0.96f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.075f, 0.095f, 0.115f, 0.96f);
        colors.highlightedColor = new Color(0.13f, 0.18f, 0.22f, 1f);
        colors.selectedColor = new Color(0.16f, 0.42f, 0.58f, 1f);
        colors.pressedColor = new Color(0.20f, 0.55f, 0.70f, 1f);
        colors.disabledColor = new Color(0.05f, 0.058f, 0.066f, 0.55f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        button.onClick.AddListener(() => action());

        Text text = CreateHudText(rect, "라벨", new Rect(0f, 4f, frame.width, frame.height - 8f), label, 14f,
                                      TextAnchor.MiddleCenter, true);
        text.color = new Color(0.90f, 0.96f, 1f, 1f);

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
            SetText(hudResourceText, "아군을 선택하세요.\n대기로 현재 캐릭터 행동 종료");
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
        SetButtonEnabled(hudGuardButton, playerTurn && CanGuard(activeUnit));
        SetButtonEnabled(hudInteractButton,
                         playerTurn && CanUseTerrainCommand(activeUnit));
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
        if (inspectedUnit != null)
        {
            return $"{inspectedUnit.definition.displayName} ({FactionLabel(inspectedUnit.definition.faction)})\n{inspectedUnit.definition.age}세 · {inspectedUnit.definition.mbti} · {inspectedUnit.definition.sectName}\n{inspectedUnit.definition.elementName}/{inspectedUnit.definition.weaponName} · {inspectedUnit.definition.speechTone}\n체력 {inspectedUnit.hp}/{inspectedUnit.definition.maxHp}   방어 {DefenseValue(inspectedUnit, TileAt(inspectedUnit.cell))}\n상태: {UnitStatusText(inspectedUnit)}\n무공: {inspectedUnit.definition.specialName}";
        }

        if (inspectedTile != null)
        {
            BattleTestInteractable prop = GetInteractableAt(inspectedTile.cell);
            StringBuilder builder = new StringBuilder();
            builder.Append(TerrainLabel(inspectedTile.terrain))
                .Append("  (")
                .Append(inspectedTile.cell.x)
                .Append(",")
                .Append(inspectedTile.cell.y)
                .AppendLine(")");
            builder.Append("이동 비용 ")
                .Append(inspectedTile.moveCost)
                .Append("   엄폐 +")
                .AppendLine(inspectedTile.coverBonus.ToString());
            builder.Append("고저 ")
                .Append(inspectedTile.elevation)
                .Append("   진입 ")
                .AppendLine(YesNo(inspectedTile.walkable));
            builder.Append("시야 ")
                .Append(inspectedTile.blocksLineOfSight ? "차단" : "개방")
                .Append("   병목 ")
                .AppendLine(YesNo(inspectedTile.isChokePoint));
            if (inspectedTile.elevation > 0)
            {
                builder.Append("고지 효과: 위에서 공격 시 명중 +2");
                if (inspectedTile.elevation >= 2)
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
                builder.Append("위험: ").Append(TileHazardText(inspectedTile));
            }

            if (!string.IsNullOrEmpty(inspectedTile.tacticalNote))
            {
                builder.AppendLine().Append("전술: ").Append(inspectedTile.tacticalNote);
            }

            return builder.ToString();
        }

        return string.Empty;
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
            image.color = selected ? new Color(0.16f, 0.42f, 0.58f, 1f) : new Color(0.075f, 0.095f, 0.115f, 0.96f);
        }
    }

    private void BuildBattle()
    {
        StopAllCoroutines();
        ClearGeneratedObjects();
        ApplyBattleEntryConfiguration();
        ApplyEquipmentBonuses();
        width = Mathf.Max(width, 16);
        height = Mathf.Max(height, 12);

        // 전투마다 새 시드로 주사위를 재초기화 — 고정 시드라 게임 첫 전투의 명중/피해/선공이 매번 동일하던 문제 방지.
        random = new System.Random();
        units.Clear();
        battleLog.Clear();
        interactables.Clear();
        activeUnit = null;
        round = 1;
        busy = false;
        aiQueued = false;
        battleOver = false;
        commandMode = BattleCommandMode.Move;
        pendingMovementUndo = default;
        scoutMode = false;
        showThreatOverlay = false;
        showElevationOverlay = false;
        showCoverOverlay = false;
        showSightOverlay = false;
        showObjectiveOverlay = false;
        phaseTurn.Reset();
        hoveredTile = null;
        hoveredUnit = null;
        inspectedTile = null;
        inspectedUnit = null;
        inspectedScreenPosition = Vector3.zero;
        suppressCameraFocus = false;
        cameraPanCoroutine = null;
        mapAssetSpritesLoaded = false;

        PrepareBattleMapData();
        EnsureMapVisualSprites();
        CreateTerrain();
        EnsureMapDebugOverlay();
        SpawnUnits();
        units.Sort((left, right) => right.initiative.CompareTo(left.initiative));
        CenterCamera();
        EnsureBattleHud();
        EnsureBattlePresentationFx();
        AddLog("[\uBC30\uCE58] \uC804\uD22C\uB294 \uCE90\uB9AD\uD130 \uBC30\uCE58\uBD80\uD130 \uC2DC\uC791\uD569\uB2C8\uB2E4. \uD30C\uB780 \uC2DC\uC791 \uCE78\uC5D0\uB9CC \uBC30\uCE58\uD560 \uC218 \uC788\uC2B5\uB2C8\uB2E4.");
        AddLog("[체계] 전투 준비 완료.");
        suppressCameraFocus = true;
        BeginPlayerPhase();
        suppressCameraFocus = false;
        EnterDeploymentMode(true);
        if (tilemapBattlefield != null && Application.isPlaying)
        {
            mapIntroCoroutine = StartCoroutine(PlayMapIntro());
        }
        else
        {
            FocusCameraOnDeploymentOverview(0f);
        }
    }

    private void PrepareBattleMapData()
    {
        activeBattleMapData = null;
        activeBattleMapDataSource = BattleMapRuntimeCatalog.SourceName(mapVariant);
        activeBattleMapDataCellCount = BattleMapRuntimeCatalog.CellCount(mapVariant);

        if (BattleMapRuntimeCatalog.TryGetDataAsset(mapVariant, out BattleMapData mapData) && mapData != null)
        {
            activeBattleMapData = mapData;
            width = Mathf.Max(width, mapData.size.x);
            height = Mathf.Max(height, mapData.size.y);
            tileWidth = mapData.tileWidth > 0f ? mapData.tileWidth : tileWidth;
            tileHeight = mapData.tileHeight > 0f ? mapData.tileHeight : tileHeight;
        }

        string mapId = activeBattleMapData == null || string.IsNullOrEmpty(activeBattleMapData.mapId)
                           ? mapVariant.ToString()
                           : activeBattleMapData.mapId;
        LoadRuntimeMapEditOverrides();
        AddLog($"[MapData] {mapId} source={activeBattleMapDataSource} cells={activeBattleMapDataCellCount}");
    }

    private void LoadRuntimeMapEditOverrides()
    {
        runtimeMapEditOverrides.Clear();
        if (!BattleMapRuntimeEditStore.TryLoadBestOverride(mapVariant, out List<BattleMapRuntimeCellEdit> edits,
                                                           out string path, out _))
        {
            return;
        }

        for (int i = 0; i < edits.Count; i++)
        {
            runtimeMapEditOverrides[edits[i].cell] = edits[i];
        }

        if (runtimeMapEditOverrides.Count > 0 &&
            !activeBattleMapDataSource.Contains("RuntimeEditCsv"))
        {
            activeBattleMapDataSource += $"+RuntimeEditCsv({System.IO.Path.GetFileName(path)})";
        }
    }

    private void EnsureMapDebugOverlay()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        BattleMapDebugOverlay overlay = GetComponent<BattleMapDebugOverlay>();
        if (overlay == null)
        {
            overlay = gameObject.AddComponent<BattleMapDebugOverlay>();
        }

        overlay.Bind(this);

        BattleMapRuntimeEditorOverlay editor = GetComponent<BattleMapRuntimeEditorOverlay>();
        if (editor == null)
        {
            editor = gameObject.AddComponent<BattleMapRuntimeEditorOverlay>();
        }

        editor.Bind(this);
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
            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }

        tilemapBattlefield = null;
        mapIntroCoroutine = null;
        cameraPanCoroutine = null;
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

    private void ApplyBattleEntryConfiguration()
    {
        if (baselineUnitDefinitions == null)
        {
            baselineUnitDefinitions = CloneUnitDefinitions(unitDefinitions);
        }

        if (runtimeMapEditorForcedMap)
        {
            ApplyRuntimeEditorMapConfiguration(runtimeMapEditorVariant);
            return;
        }

        string battleId = !string.IsNullOrEmpty(BattleEntryAdapter.PendingBattleId)
                              ? BattleEntryAdapter.PendingBattleId
                              : BattleResultBridge.CurrentBattleId;
        if (battleId == HubController.BanditLairBattleId)
        {
            mapVariant = BattleTestMapVariant.BanditLair;
            useAuthoredSceneMap = false;
            width = 16;
            height = 12;
            unitDefinitions = BuildBanditLairUnitDefinitions(baselineUnitDefinitions);
            return;
        }

        if (battleId == HubController.WolfPassBattleId)
        {
            mapVariant = BattleTestMapVariant.WolfPass;
            useAuthoredSceneMap = false;
            width = 15;
            height = 12;
            unitDefinitions = BuildWolfPassUnitDefinitions(baselineUnitDefinitions);
            return;
        }

        if (battleId == HubController.TigerRavineBattleId)
        {
            mapVariant = BattleTestMapVariant.TigerRavine;
            useAuthoredSceneMap = false;
            width = 16;
            height = 12;
            unitDefinitions = BuildTigerRavineUnitDefinitions(baselineUnitDefinitions);
            return;
        }

        if (battleId == HubController.LeopardCliffBattleId)
        {
            mapVariant = BattleTestMapVariant.LeopardCliff;
            useAuthoredSceneMap = false;
            width = 16;
            height = 12;
            unitDefinitions = BuildLeopardCliffUnitDefinitions(baselineUnitDefinitions);
            return;
        }

        if (battleId == HubController.SeorakPassRescueBattleId)
        {
            mapVariant = BattleTestMapVariant.SeorakPassRescue;
            useAuthoredSceneMap = false;
            width = 16;
            height = 12;
            unitDefinitions = BuildSeorakPassRescueUnitDefinitions(baselineUnitDefinitions);
            return;
        }

        mapVariant = BattleTestMapVariant.BaekduSnowGate;
        useAuthoredSceneMap = false;
        width = 16;
        height = 12;
        unitDefinitions = BuildBaekduSnowGateUnitDefinitions(baselineUnitDefinitions);
    }

    public void LoadMapForRuntimeEditing(BattleTestMapVariant variant)
    {
        runtimeMapEditorForcedMap = true;
        runtimeMapEditorVariant = variant;
        BuildBattle();
    }

    private void ApplyRuntimeEditorMapConfiguration(BattleTestMapVariant variant)
    {
        mapVariant = variant;
        useAuthoredSceneMap = false;
        width = variant == BattleTestMapVariant.WolfPass ? 15 : 16;
        height = 12;

        switch (variant)
        {
        case BattleTestMapVariant.BanditLair:
            unitDefinitions = BuildBanditLairUnitDefinitions(baselineUnitDefinitions);
            break;
        case BattleTestMapVariant.WolfPass:
            unitDefinitions = BuildWolfPassUnitDefinitions(baselineUnitDefinitions);
            break;
        case BattleTestMapVariant.TigerRavine:
            unitDefinitions = BuildTigerRavineUnitDefinitions(baselineUnitDefinitions);
            break;
        case BattleTestMapVariant.LeopardCliff:
            unitDefinitions = BuildLeopardCliffUnitDefinitions(baselineUnitDefinitions);
            break;
        case BattleTestMapVariant.SeorakPassRescue:
            unitDefinitions = BuildSeorakPassRescueUnitDefinitions(baselineUnitDefinitions);
            break;
        case BattleTestMapVariant.BaekduSnowGate:
            unitDefinitions = BuildBaekduSnowGateUnitDefinitions(baselineUnitDefinitions);
            break;
        default:
            unitDefinitions = CloneUnitDefinitions(baselineUnitDefinitions);
            break;
        }
    }

    /// <summary>
    /// 허브 정비창에서 장착한 장비/강화 보정을 아군 유닛 수치에 반영한다(설계 §F).
    /// unitDefinitions는 매 전투 진입 시 baseline에서 새로 복제되므로 중복 적용되지 않는다.
    /// 에디터에서 전투 씬을 단독 실행하면 GameRoot가 없어 보정 없이 진행된다.
    /// </summary>
    private void ApplyEquipmentBonuses()
    {
        GameRoot root = GameRoot.Instance;
        if (root == null || root.Session == null || unitDefinitions == null)
        {
            return;
        }

        EquipmentService equipment = new EquipmentService(root.Session);
        ProgressionService progression = new ProgressionService(root.Session);
        foreach (BattleTestUnitDefinition definition in unitDefinitions)
        {
            if (definition == null || definition.faction != Faction.Ally)
            {
                continue;
            }

            CharacterProgressState growth = progression.GetSnapshot(definition.id);
            ApplyGrowthBonuses(definition, growth);

            EquipmentBonus bonus = equipment.BuildBonus(definition.id);
            if (bonus.IsEmpty)
            {
                continue;
            }

            definition.maxHp += bonus.hp;
            definition.maxInner += bonus.inner;
            definition.attackBonus += bonus.acc;
            definition.defense += bonus.guard;
            definition.damageMin += bonus.atk;
            definition.damageMax += bonus.atk;
            definition.moveRange += bonus.move;
        }
    }

    private static void ApplyGrowthBonuses(BattleTestUnitDefinition definition, CharacterProgressState growth)
    {
        if (definition == null || growth == null)
        {
            return;
        }

        SixStats bonus = growth.statBonuses;
        definition.maxHp += growth.hpBonus + Mathf.Max(0, bonus.strength) * 2 + Mathf.Max(0, bonus.spirit);
        definition.maxInner += growth.innerBonus + Mathf.Max(0, bonus.innerPower) / 3;
        definition.attackBonus += Mathf.Max(0, bonus.insight + bonus.agility / 2) / 3;
        definition.defense += Mathf.Max(0, bonus.spirit + bonus.strength / 2) / 3;
        int power = Mathf.Max(0, bonus.strength + bonus.innerPower);
        definition.damageMin += power / 4;
        definition.damageMax += power / 4;
        definition.specialPower += Mathf.Max(0, bonus.innerPower + bonus.insight / 2) / 4;
        definition.specialAttackBonus += Mathf.Max(0, bonus.insight) / 4;
        definition.initiative += Mathf.Max(0, bonus.agility * 2 + bonus.insight) / 3;
        if (definition.agility < 0)
        {
            definition.agility = definition.initiative;
        }

        definition.agility += bonus.agility;
        if (bonus.agility >= 4)
        {
            definition.moveRange += bonus.agility / 4;
        }
    }

    private static BattleTestUnitDefinition[] CloneUnitDefinitions(BattleTestUnitDefinition[] source)
    {
        if (source == null)
        {
            return new BattleTestUnitDefinition[0];
        }

        BattleTestUnitDefinition[] clones = new BattleTestUnitDefinition[source.Length];
        for (int i = 0; i < source.Length; i++)
        {
            clones[i] = CloneUnitDefinition(source[i]);
        }

        return clones;
    }

    private static BattleTestUnitDefinition CloneUnitDefinition(BattleTestUnitDefinition source)
    {
        if (source == null)
        {
            return null;
        }

        return new BattleTestUnitDefinition
        {
            id = source.id,
            displayName = source.displayName,
            faction = source.faction,
            visual = source.visual,
            startCell = source.startCell,
            sectName = source.sectName,
            age = source.age,
            mbti = source.mbti,
            elementName = source.elementName,
            weaponName = source.weaponName,
            speechTone = source.speechTone,
            maxHp = source.maxHp,
            maxInner = source.maxInner,
            initiative = source.initiative,
            agility = source.agility,
            moveRange = source.moveRange,
            attackRange = source.attackRange,
            attackBonus = source.attackBonus,
            defense = source.defense,
            damageMin = source.damageMin,
            damageMax = source.damageMax,
            specialName = source.specialName,
            specialRange = source.specialRange,
            specialCost = source.specialCost,
            specialCooldown = source.specialCooldown,
            specialPower = source.specialPower,
            specialAttackBonus = source.specialAttackBonus,
            specialEffect = source.specialEffect
        };
    }

    private static BattleTestUnitDefinition[] BuildBaekduSnowGateUnitDefinitions(BattleTestUnitDefinition[] baseDefinitions)
    {
        List<BattleTestUnitDefinition> result = new List<BattleTestUnitDefinition>();
        Vector2Int[] allyCells = RuntimeBaekduDeploymentCellsOrDefault(SnowGateAscentAllyStartCells);
        Vector2Int[] enemyCells = RuntimeBaekduEnemySpawnCellsOrDefault(SnowGateAscentEnemyStartCells);

        int allyIndex = 0;
        int enemyIndex = 0;
        foreach (BattleTestUnitDefinition definition in baseDefinitions)
        {
            if (definition == null)
            {
                continue;
            }

            BattleTestUnitDefinition unit = CloneUnitDefinition(definition);
            if (unit.faction == Faction.Ally)
            {
                unit.startCell = allyCells[Mathf.Min(allyIndex, allyCells.Length - 1)];
                allyIndex++;
            }
            else if (unit.faction == Faction.Enemy)
            {
                unit.startCell = enemyCells[Mathf.Min(enemyIndex, enemyCells.Length - 1)];
                enemyIndex++;
            }

            result.Add(unit);
        }

        ApplyEnemyStartCells(result, enemyCells);
        return result.ToArray();
    }

    private static Vector2Int[] RuntimeBaekduDeploymentCellsOrDefault(Vector2Int[] fallback)
    {
        return RuntimeBaekduCellsOrDefault(cell => cell.deployZone > 0 && cell.walkable && cell.occupyAllowed,
                                          fallback);
    }

    private static Vector2Int[] RuntimeBaekduEnemySpawnCellsOrDefault(Vector2Int[] fallback)
    {
        return RuntimeBaekduCellsOrDefault(cell => cell.walkable && cell.occupyAllowed &&
                                                   cell.HasTag(BattleMapRuntimeEditStore.EnemySpawnTag),
                                          fallback);
    }

    private static Vector2Int[] RuntimeBaekduCellsOrDefault(Predicate<BattleMapRuntimeCell> predicate,
                                                           Vector2Int[] fallback)
    {
        List<Vector2Int> cells = new List<Vector2Int>();
        foreach (BattleMapRuntimeCell cell in BattleMapRuntimeCatalog.Cells(BattleTestMapVariant.BaekduSnowGate))
        {
            if (cell != null && predicate(cell))
            {
                cells.Add(cell.cell);
            }
        }

        if (cells.Count == 0)
        {
            return fallback;
        }

        cells.Sort((left, right) =>
        {
            int yCompare = left.y.CompareTo(right.y);
            return yCompare != 0 ? yCompare : left.x.CompareTo(right.x);
        });
        return cells.ToArray();
    }

    private static BattleTestUnitDefinition[] BuildBanditLairUnitDefinitions(BattleTestUnitDefinition[] baseDefinitions)
    {
        List<BattleTestUnitDefinition> result = new List<BattleTestUnitDefinition>();
        Vector2Int[] allyCells = BanditFrontDescentAllyStartCells;

        int allyIndex = 0;
        foreach (BattleTestUnitDefinition definition in baseDefinitions)
        {
            if (definition == null || definition.faction != Faction.Ally)
            {
                continue;
            }

            BattleTestUnitDefinition ally = CloneUnitDefinition(definition);
            ally.startCell = allyCells[Mathf.Min(allyIndex, allyCells.Length - 1)];
            result.Add(ally);
            allyIndex++;
        }

        BattleTestUnitDefinition guard = FindDefinition(baseDefinitions, "bandit_guard_1") ??
                                        FindDefinition(baseDefinitions, "iron_wolf_guard_1") ??
                                        FindFirstDefinition(baseDefinitions, Faction.Enemy);
        BattleTestUnitDefinition scout = FindDefinition(baseDefinitions, "bandit_scout_1") ?? guard;
        BattleTestUnitDefinition captain = FindDefinition(baseDefinitions, "bandit_captain") ??
                                           FindDefinition(baseDefinitions, "iron_wolf_captain") ?? guard;

        result.Add(BanditUnit(guard, "bandit_cutthroat_1", "흑립방 칼잡이", new Vector2Int(7, 8),
                              "흑립방 산도적", "흙먼지", "도", 26, 3, 13, 14, 4, 1, 5, 13, 4, 8, "난도질", 1,
                              1, 2, 3, 1, BattleSpecialEffect.Strike));
        result.Add(BanditUnit(guard, "bandit_spear_1", "벌목길 창수", new Vector2Int(10, 7),
                              "흑립방 산도적", "거친 바람", "장창", 28, 3, 12, 13, 4, 2, 5, 13, 4, 8, "밀쳐 찌르기",
                              2, 1, 2, 3, 1, BattleSpecialEffect.BreakGuard));
        result.Add(BanditUnit(scout, "bandit_slinger_1", "망루 투석꾼", new Vector2Int(13, 8),
                              "흑립방 산도적", "돌가루", "투석끈", 22, 2, 14, 15, 4, 3, 4, 12, 3, 6, "흙먼지 던지기",
                              3, 1, 2, 0, 0, BattleSpecialEffect.Mark));
        result.Add(BanditUnit(guard, "bandit_trapper_1", "덫지기", new Vector2Int(4, 7),
                              "흑립방 산도적", "독", "단검", 24, 3, 15, 16, 5, 1, 5, 12, 3, 7, "독묻은 쇠못",
                              2, 1, 2, 3, 1, BattleSpecialEffect.Poison));
        result.Add(BanditUnit(captain, "bandit_boss_gwakchil", "흑립방 두목 곽칠", new Vector2Int(8, 10),
                              "흑립방", "압박", "대도", 40, 4, 11, 12, 4, 1, 7, 15, 6, 11, "목책 내려찍기", 1,
                              1, 2, 5, 2, BattleSpecialEffect.BreakGuard));

        ApplyEnemyStartCells(result, FrontDescentEnemyStartCells);
        return result.ToArray();
    }

    private static BattleTestUnitDefinition[] BuildWolfPassUnitDefinitions(BattleTestUnitDefinition[] baseDefinitions)
    {
        List<BattleTestUnitDefinition> result = new List<BattleTestUnitDefinition>();
        AddFreeTimeAllies(result, baseDefinitions, WolfFrontDescentAllyStartCells);

        BattleTestUnitDefinition guard = FindDefinition(baseDefinitions, "iron_wolf_guard_1") ??
                                        FindFirstDefinition(baseDefinitions, Faction.Enemy);
        BattleTestUnitDefinition spear = FindDefinition(baseDefinitions, "iron_wolf_spear_1") ?? guard;
        BattleTestUnitDefinition captain = FindDefinition(baseDefinitions, "iron_wolf_captain") ?? guard;

        result.Add(BeastUnit(guard, "wolf_runner_1", "굶주린 늑대", new Vector2Int(5, 5),
                             "백두산 야수", "야성", "이빨", 20, 1, 16, 18, 5, 1, 5, 12, 4, 7, "덮쳐 물기",
                             1, 1, 2, 2, 1, BattleSpecialEffect.Strike));
        result.Add(BeastUnit(spear, "wolf_runner_2", "능선 늑대", new Vector2Int(10, 6),
                             "백두산 야수", "야성", "발톱", 22, 1, 15, 17, 5, 1, 5, 12, 4, 8, "측면 물기",
                             1, 1, 2, 3, 1, BattleSpecialEffect.Mark));
        result.Add(BeastUnit(guard, "wolf_den_guard", "굴 지키는 늑대", new Vector2Int(12, 9),
                             "백두산 야수", "야성", "송곳니", 24, 1, 13, 16, 4, 1, 6, 13, 5, 8, "지키는 포효",
                             1, 1, 2, 3, 1, BattleSpecialEffect.BreakGuard));
        result.Add(BeastUnit(captain, "wolf_alpha", "굶주린 늑대 우두머리", new Vector2Int(12, 10),
                             "백두산 야수", "야성", "우두머리 이빨", 34, 2, 14, 17, 5, 1, 7, 14, 6, 11,
                             "무리 돌진", 1, 1, 2, 4, 2, BattleSpecialEffect.BreakGuard));

        ApplyEnemyStartCells(result, FrontDescentEnemyStartCells);
        return result.ToArray();
    }

    private static BattleTestUnitDefinition[] BuildTigerRavineUnitDefinitions(BattleTestUnitDefinition[] baseDefinitions)
    {
        List<BattleTestUnitDefinition> result = new List<BattleTestUnitDefinition>();
        AddFreeTimeAllies(result, baseDefinitions, TigerFrontDescentAllyStartCells);

        BattleTestUnitDefinition guard = FindDefinition(baseDefinitions, "iron_wolf_guard_1") ??
                                        FindFirstDefinition(baseDefinitions, Faction.Enemy);
        BattleTestUnitDefinition spear = FindDefinition(baseDefinitions, "iron_wolf_spear_1") ?? guard;
        BattleTestUnitDefinition captain = FindDefinition(baseDefinitions, "iron_wolf_captain") ?? guard;

        result.Add(BeastUnit(guard, "tiger_shadow_1", "바위골 호랑이", new Vector2Int(4, 6),
                             "백두산 야수", "산기운", "발톱", 30, 2, 13, 15, 4, 1, 6, 14, 6, 10, "앞발 후려치기",
                             1, 1, 2, 3, 1, BattleSpecialEffect.Strike));
        result.Add(BeastUnit(spear, "tiger_shadow_2", "억새밭 산짐승", new Vector2Int(8, 5),
                             "백두산 야수", "산기운", "이빨", 26, 1, 15, 16, 5, 1, 5, 13, 5, 9, "억새 돌진",
                             1, 1, 2, 3, 1, BattleSpecialEffect.Mark));
        result.Add(BeastUnit(guard, "tiger_cave_guard", "바위굴 수호수", new Vector2Int(12, 8),
                             "백두산 야수", "산기운", "발톱", 32, 2, 12, 14, 4, 1, 6, 15, 6, 10, "낙석 몰이",
                             1, 1, 2, 4, 1, BattleSpecialEffect.BreakGuard));
        result.Add(BeastUnit(captain, "tiger_boss_sangun", "산군 호랑이", new Vector2Int(13, 9),
                             "백두산 야수", "산군", "대호의 발톱", 54, 3, 14, 16, 5, 1, 8, 16, 8, 14,
                             "산군 포효", 1, 1, 2, 5, 2, BattleSpecialEffect.BreakGuard));

        ApplyEnemyStartCells(result, FrontDescentEnemyStartCells);
        return result.ToArray();
    }

    private static BattleTestUnitDefinition[] BuildLeopardCliffUnitDefinitions(BattleTestUnitDefinition[] baseDefinitions)
    {
        List<BattleTestUnitDefinition> result = new List<BattleTestUnitDefinition>();
        AddFreeTimeAllies(result, baseDefinitions, LeopardFrontDescentAllyStartCells);

        BattleTestUnitDefinition guard = FindDefinition(baseDefinitions, "iron_wolf_guard_1") ??
                                        FindFirstDefinition(baseDefinitions, Faction.Enemy);
        BattleTestUnitDefinition spear = FindDefinition(baseDefinitions, "iron_wolf_spear_1") ?? guard;
        BattleTestUnitDefinition captain = FindDefinition(baseDefinitions, "iron_wolf_captain") ?? guard;

        result.Add(BeastUnit(guard, "leopard_ambusher_1", "절벽 표범", new Vector2Int(3, 7),
                             "백두산 야수", "그림자", "발톱", 24, 2, 17, 18, 6, 1, 5, 13, 4, 8, "절벽 급습",
                             1, 1, 2, 3, 1, BattleSpecialEffect.Mark));
        result.Add(BeastUnit(spear, "leopard_ambusher_2", "대나무 표범", new Vector2Int(10, 6),
                             "백두산 야수", "그림자", "이빨", 26, 2, 16, 18, 6, 1, 5, 13, 5, 9, "그림자 물기",
                             1, 1, 2, 3, 1, BattleSpecialEffect.Poison));
        result.Add(BeastUnit(guard, "leopard_ridge_guard", "약초길 표범", new Vector2Int(12, 7),
                             "백두산 야수", "그림자", "발톱", 28, 2, 15, 17, 5, 1, 6, 14, 5, 9, "바위 타기",
                             1, 1, 2, 3, 1, BattleSpecialEffect.Strike));
        result.Add(BeastUnit(captain, "leopard_boss_shadow", "그림자 표범", new Vector2Int(13, 8),
                             "백두산 야수", "그림자", "검은 발톱", 40, 3, 17, 19, 6, 1, 7, 15, 7, 12,
                             "무음 도약", 1, 1, 2, 5, 2, BattleSpecialEffect.Mark));

        ApplyEnemyStartCells(result, FrontDescentEnemyStartCells);
        return result.ToArray();
    }

    private static BattleTestUnitDefinition[] BuildSeorakPassRescueUnitDefinitions(BattleTestUnitDefinition[] baseDefinitions)
    {
        List<BattleTestUnitDefinition> result = new List<BattleTestUnitDefinition>();
        AddNamedAlly(result, baseDefinitions, "park_sungjun", LeopardFrontDescentAllyStartCells[0]);
        AddNamedAlly(result, baseDefinitions, "baek_ryeon", LeopardFrontDescentAllyStartCells[1]);

        BattleTestUnitDefinition guard = FindDefinition(baseDefinitions, "bandit_guard_1") ??
                                        FindDefinition(baseDefinitions, "iron_wolf_guard_1") ??
                                        FindFirstDefinition(baseDefinitions, Faction.Enemy);
        BattleTestUnitDefinition scout = FindDefinition(baseDefinitions, "bandit_scout_1") ?? guard;
        BattleTestUnitDefinition captain = FindDefinition(baseDefinitions, "bandit_captain") ??
                                           FindDefinition(baseDefinitions, "iron_wolf_captain") ?? guard;

        result.Add(BanditUnit(guard, "seorak_bandit_blade_1", "철비채 도객", new Vector2Int(4, 7),
                              "철비채", "눈먼 탐욕", "도", 22, 2, 13, 14, 4, 1, 5, 12, 4, 7, "길목 베기",
                              1, 1, 2, 3, 1, BattleSpecialEffect.Strike));
        result.Add(BanditUnit(scout, "seorak_bandit_archer_1", "철비채 궁수", new Vector2Int(10, 6),
                              "철비채", "매복", "각궁", 18, 2, 15, 16, 4, 3, 4, 11, 3, 6, "수레 노리기",
                              3, 1, 2, 0, 0, BattleSpecialEffect.Mark));
        result.Add(BanditUnit(guard, "seorak_bandit_axe_1", "철비채 도끼병", new Vector2Int(12, 7),
                              "철비채", "완력", "도끼", 26, 2, 11, 12, 3, 1, 6, 14, 5, 9, "수레 내려찍기",
                              1, 1, 2, 4, 1, BattleSpecialEffect.BreakGuard));
        result.Add(BanditUnit(captain, "seorak_bandit_boss_yudalgeun", "철비채 두목 유달근",
                              new Vector2Int(13, 8), "철비채", "협박", "대도", 34, 3, 12, 13, 4, 1, 7,
                              15, 6, 10, "목숨값 흥정", 1, 1, 2, 4, 2, BattleSpecialEffect.BreakGuard));

        ApplyEnemyStartCells(result, FrontDescentEnemyStartCells);
        return result.ToArray();
    }

    private static void ApplyEnemyStartCells(List<BattleTestUnitDefinition> result, Vector2Int[] enemyCells)
    {
        if (result == null || enemyCells == null || enemyCells.Length == 0)
        {
            return;
        }

        int enemyIndex = 0;
        foreach (BattleTestUnitDefinition unit in result)
        {
            if (unit == null || unit.faction != Faction.Enemy)
            {
                continue;
            }

            unit.startCell = enemyCells[Mathf.Min(enemyIndex, enemyCells.Length - 1)];
            enemyIndex++;
        }
    }

    private static void AddFreeTimeAllies(List<BattleTestUnitDefinition> result, BattleTestUnitDefinition[] baseDefinitions,
                                          Vector2Int[] allyCells)
    {
        if (baseDefinitions == null)
        {
            return;
        }

        int allyIndex = 0;
        foreach (BattleTestUnitDefinition definition in baseDefinitions)
        {
            if (definition == null || definition.faction != Faction.Ally)
            {
                continue;
            }

            BattleTestUnitDefinition ally = CloneUnitDefinition(definition);
            ally.startCell = allyCells[Mathf.Min(allyIndex, allyCells.Length - 1)];
            result.Add(ally);
            allyIndex++;
        }
    }

    private static void AddNamedAlly(List<BattleTestUnitDefinition> result, BattleTestUnitDefinition[] baseDefinitions,
                                     string id, Vector2Int cell)
    {
        BattleTestUnitDefinition definition = FindDefinition(baseDefinitions, id);
        if (definition == null || definition.faction != Faction.Ally)
        {
            return;
        }

        BattleTestUnitDefinition ally = CloneUnitDefinition(definition);
        ally.startCell = cell;
        result.Add(ally);
    }

    private static BattleTestUnitDefinition BanditUnit(BattleTestUnitDefinition template, string id, string displayName,
                                                       Vector2Int cell, string sectName, string elementName,
                                                       string weaponName, int maxHp, int maxInner, int initiative,
                                                       int agility, int moveRange, int attackRange, int attackBonus,
                                                       int defense, int damageMin, int damageMax, string specialName,
                                                       int specialRange, int specialCost, int specialCooldown,
                                                       int specialPower, int specialAttackBonus,
                                                       BattleSpecialEffect specialEffect)
    {
        BattleTestUnitDefinition unit = CloneUnitDefinition(template) ?? new BattleTestUnitDefinition();
        unit.id = id;
        unit.displayName = displayName;
        unit.faction = Faction.Enemy;
        unit.startCell = cell;
        unit.sectName = sectName;
        unit.elementName = elementName;
        unit.weaponName = weaponName;
        unit.speechTone = "험한 산도적 말투";
        unit.maxHp = maxHp;
        unit.maxInner = maxInner;
        unit.initiative = initiative;
        unit.agility = agility;
        unit.moveRange = moveRange;
        unit.attackRange = attackRange;
        unit.attackBonus = attackBonus;
        unit.defense = defense;
        unit.damageMin = damageMin;
        unit.damageMax = damageMax;
        unit.specialName = specialName;
        unit.specialRange = specialRange;
        unit.specialCost = specialCost;
        unit.specialCooldown = specialCooldown;
        unit.specialPower = specialPower;
        unit.specialAttackBonus = specialAttackBonus;
        unit.specialEffect = specialEffect;
        return unit;
    }

    private static BattleTestUnitDefinition BeastUnit(BattleTestUnitDefinition template, string id, string displayName,
                                                      Vector2Int cell, string sectName, string elementName,
                                                      string weaponName, int maxHp, int maxInner, int initiative,
                                                      int agility, int moveRange, int attackRange, int attackBonus,
                                                      int defense, int damageMin, int damageMax, string specialName,
                                                      int specialRange, int specialCost, int specialCooldown,
                                                      int specialPower, int specialAttackBonus,
                                                      BattleSpecialEffect specialEffect)
    {
        BattleTestUnitDefinition unit = CloneUnitDefinition(template) ?? new BattleTestUnitDefinition();
        unit.id = id;
        unit.displayName = displayName;
        unit.faction = Faction.Enemy;
        unit.startCell = cell;
        unit.sectName = sectName;
        unit.elementName = elementName;
        unit.weaponName = weaponName;
        unit.speechTone = "낮은 울음과 포효";
        unit.maxHp = maxHp;
        unit.maxInner = maxInner;
        unit.initiative = initiative;
        unit.agility = agility;
        unit.moveRange = moveRange;
        unit.attackRange = attackRange;
        unit.attackBonus = attackBonus;
        unit.defense = defense;
        unit.damageMin = damageMin;
        unit.damageMax = damageMax;
        unit.specialName = specialName;
        unit.specialRange = specialRange;
        unit.specialCost = specialCost;
        unit.specialCooldown = specialCooldown;
        unit.specialPower = specialPower;
        unit.specialAttackBonus = specialAttackBonus;
        unit.specialEffect = specialEffect;
        return unit;
    }

    private static BattleTestUnitDefinition FindDefinition(BattleTestUnitDefinition[] definitions, string id)
    {
        if (definitions == null)
        {
            return null;
        }

        foreach (BattleTestUnitDefinition definition in definitions)
        {
            if (definition != null && definition.id == id)
            {
                return definition;
            }
        }

        return null;
    }

    private static BattleTestUnitDefinition FindFirstDefinition(BattleTestUnitDefinition[] definitions, Faction faction)
    {
        if (definitions == null)
        {
            return null;
        }

        foreach (BattleTestUnitDefinition definition in definitions)
        {
            if (definition != null && definition.faction == faction)
            {
                return definition;
            }
        }

        return null;
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

        DestroyLegacyCanvasHud();
        GameObject hudObject = new GameObject("Battle HUD");
        hudObject.transform.SetParent(transform, false);
        battleHud = hudObject.AddComponent<BattleHUDController>();
        battleHud.Initialize(this);
    }

    private void EnsureBattlePresentationFx()
    {
        battleCameraFx = battleCameraFx != null ? battleCameraFx : GetComponent<BattleCameraFx>();
        if (battleCameraFx == null)
        {
            battleCameraFx = gameObject.AddComponent<BattleCameraFx>();
        }

        damagePopupPresenter = damagePopupPresenter != null ? damagePopupPresenter : GetComponent<DamagePopupPresenter>();
        if (damagePopupPresenter == null)
        {
            damagePopupPresenter = gameObject.AddComponent<DamagePopupPresenter>();
        }

        battleImpactPresenter = battleImpactPresenter != null ? battleImpactPresenter : GetComponent<BattleImpactPresenter>();
        if (battleImpactPresenter == null)
        {
            battleImpactPresenter = gameObject.AddComponent<BattleImpactPresenter>();
        }
    }

    private void DestroyLegacyCanvasHud()
    {
        if (hudCanvas != null)
        {
            Destroy(hudCanvas.gameObject);
        }

        Transform legacy = transform.Find("BattleCanvasHud");
        if (legacy != null)
        {
            Destroy(legacy.gameObject);
        }

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
            turnLimit = DefaultTurnLimit,
            battleOver = battleOver,
            scoutMode = scoutMode,
            instruction = BuildHudInstructionText(),
            objectiveText = BuildHudObjectiveText(),
            unitInfoText = BuildHudUnitInfo(),
            hoverTitle = BuildHudHoverTitle(),
            hoverBody = BuildHudHoverBody(),
            hoverScreenPosition = inspectedScreenPosition,
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
        snapshot.canGuard = playerTurn && CanGuard(activeUnit);
        snapshot.canTerrain = playerTurn && CanUseTerrainCommand(activeUnit);
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

    public BattleTestUnit PreviewActiveUnit => activeUnit;
    public bool PreviewBattleOver => battleOver;
    public bool PreviewBusy => busy;
    public bool PreviewIsPlayerPhase => phaseTurn.IsPlayerPhase;
    public bool PreviewDeploymentMode => scoutMode;
    public BattleCommandMode PreviewCommandMode => commandMode;
    public IEnumerable<BattleTestUnit> PreviewUnits => units;
    public IEnumerable<BattleTestTile> PreviewTiles
    {
        get
        {
            if (tiles == null)
            {
                yield break;
            }

            foreach (BattleTestTile tile in tiles)
            {
                if (tile != null)
                {
                    yield return tile;
                }
            }
        }
    }

    public Dictionary<Vector2Int, int> GetPreviewReachableCells(BattleTestUnit unit)
    {
        return unit == null ? new Dictionary<Vector2Int, int>() : GetLegalMovePreviewCells(unit);
    }

    public int GetPreviewMoveRange(BattleTestUnit unit)
    {
        return unit == null ? 0 : EffectiveMoveRange(unit);
    }

    public BattleTestUnit GetPreviewUnitAt(Vector2Int cell)
    {
        return UnitAt(cell);
    }

    public BattleTestTile GetPreviewTileAt(Vector2Int cell)
    {
        return TileAt(cell);
    }

    public List<Vector2Int> GetPreviewMovePath(BattleTestUnit unit, Vector2Int destination)
    {
        return unit == null ? new List<Vector2Int>() : FindMovePath(unit, destination);
    }

    public Vector3 GetPreviewUnitWorldPosition(Vector2Int cell)
    {
        return UnitWorldPosition(cell);
    }

    public Vector3 GetPreviewTileWorldPosition(Vector2Int cell)
    {
        return GridToWorld(cell);
    }

    public Vector2Int GetPreviewGridCell(Vector2 worldPoint)
    {
        return WorldToGrid(worldPoint);
    }

    public void ClearPreviewHighlights()
    {
        ClearHighlights();
    }

    private string BuildHudObjectiveText()
    {
        if (scoutMode)
        {
            return MapDisplayName +
                   "\n\uCE90\uB9AD\uD130 \uBC30\uCE58: \uC544\uAD70\uC744 \uC120\uD0DD\uD558\uACE0 \uD30C\uB780 \uC2DC\uC791 \uCE78\uC744 \uD074\uB9AD\uD558\uC138\uC694." +
                   "\n\uD30C\uB780\uC0C9\uC740 \uBC30\uCE58 \uAC00\uB2A5, \uBE68\uAC04\uC0C9\uC740 \uBC30\uCE58 \uAD6C\uC5ED \uBC16 \uACBD\uACC4\uC785\uB2C8\uB2E4. Space/\uC2DC\uC791\uC73C\uB85C \uC804\uD22C \uC2DC\uC791.";
        }

        if (mapVariant == BattleTestMapVariant.BanditLair)
        {
            return $"{MapDisplayName}\n주 목표: 도적 두목 제압 / 보급 상자 회수\n보조: 덫 회피, 망루 고지 제압, 통나무 엄폐 활용\n단축: S 정찰 / Tab 위협 / H 고저 / C 엄폐 / V 시야 / O 목표";
        }
        if (mapVariant == BattleTestMapVariant.WolfPass)
        {
            return $"{MapDisplayName}\n주 목표: 늑대 우두머리 제압 / 방목길 확보\n보조: 개울 병목, 늑대 굴 봉쇄, 통나무 우회\n단축: S 정찰 / Tab 위협 / H 고저 / C 엄폐 / V 시야 / O 목표";
        }
        if (mapVariant == BattleTestMapVariant.TigerRavine)
        {
            return $"{MapDisplayName}\n주 목표: 산군 호랑이 제압 / 주민 구조\n보조: 억새 엄폐, 낙석 회피, H3 바위 선반 확보\n단축: S 정찰 / Tab 위협 / H 고저 / C 엄폐 / V 시야 / O 목표";
        }
        if (mapVariant == BattleTestMapVariant.LeopardCliff)
        {
            return $"{MapDisplayName}\n주 목표: 그림자 표범 격퇴 / 약초길 개방\n보조: 밧줄다리 병목, 절벽 매복 회피, 약초 선반 확보\n단축: S 정찰 / Tab 위협 / H 고저 / C 엄폐 / V 시야 / O 목표";
        }
        if (mapVariant == BattleTestMapVariant.SeorakPassRescue)
        {
            return $"{MapDisplayName}\n주 목표: 유달근 격파 / 약초 수레와 피난민 보호\n보조: 백련과 협공, 밧줄다리 병목, 대나무 덤불 엄폐\n단축: S 정찰 / Tab 위협 / H 고저 / C 엄폐 / V 시야 / O 목표";
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

        if (!activeUnit.CanMove && activeUnit.CanUseMainAction && commandMode == BattleCommandMode.Attack)
        {
            return "이동 완료. 이제 공격하거나 대기만 할 수 있습니다.";
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
            return "파란 칸은 이동 가능 범위입니다. 이동을 확정하면 공격 또는 대기만 남습니다.";
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
        if (inspectedUnit != null)
        {
            return inspectedUnit.definition.displayName + " / " + FactionLabel(inspectedUnit.definition.faction);
        }

        if (inspectedTile != null)
        {
            return TerrainLabel(inspectedTile.terrain) + "  (" + inspectedTile.cell.x + "," + inspectedTile.cell.y + ")";
        }

        return string.Empty;
    }

    private string BuildHudHoverBody()
    {
        if (inspectedUnit != null)
        {
            return "HP " + inspectedUnit.hp + "/" + inspectedUnit.definition.maxHp +
                   "   방어 " + DefenseValue(inspectedUnit, TileAt(inspectedUnit.cell)) +
                   "\n" + inspectedUnit.definition.elementName + "/" + inspectedUnit.definition.weaponName +
                   "   " + inspectedUnit.definition.mbti +
                   "\n무공: " + inspectedUnit.definition.specialName +
                   "\n상태: " + UnitStatusText(inspectedUnit);
        }

        if (inspectedTile != null)
        {
            BattleTestInteractable prop = GetInteractableAt(inspectedTile.cell);
            string propLine = prop == null
                                  ? "위험: " + TileHazardText(inspectedTile)
                                  : "지형 오브젝트: " + prop.displayName + " / " + InteractableEffectText(prop.kind);
            return "이동 비용 " + inspectedTile.moveCost +
                   "   엄폐 +" + inspectedTile.coverBonus +
                   "\n고저차 " + inspectedTile.elevation +
                   "   시야 " + (inspectedTile.blocksLineOfSight ? "차단" : "개방") +
                   "\n" + propLine +
                   (string.IsNullOrEmpty(inspectedTile.tacticalNote) ? string.Empty : "\n" + inspectedTile.tacticalNote);
        }

        return string.Empty;
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

        EnterDeploymentMode(false);
    }

    private void ExitScoutMode()
    {
        if (!scoutMode)
        {
            return;
        }

        scoutMode = false;
        commandMode = BattleCommandMode.Move;
        AddLog("[\uBC30\uCE58] \uBC30\uCE58 \uC885\uB8CC. \uC544\uAD70 \uD589\uB3D9\uC744 \uC2DC\uC791\uD569\uB2C8\uB2E4.");
        RefreshHighlights();
        RefreshUnits();
    }

    private void EnterDeploymentMode(bool initialEntry)
    {
        if (!phaseTurn.IsPlayerPhase || battleOver || busy)
        {
            return;
        }

        scoutMode = true;
        showThreatOverlay = false;
        showElevationOverlay = false;
        showCoverOverlay = false;
        showSightOverlay = false;
        showObjectiveOverlay = false;
        commandMode = BattleCommandMode.Move;
        FaceUnitsForDeployment();
        AddLog(initialEntry
                   ? "[\uBC30\uCE58] \uC804\uD22C \uC2DC\uC791 \uC804, \uC544\uAD70\uC744 \uD30C\uB780 \uC2DC\uC791 \uCE78\uC5D0 \uBC30\uCE58\uD558\uC138\uC694."
                   : "[\uBC30\uCE58] \uBC30\uCE58 \uBAA8\uB4DC \uC7AC\uAC1C.");
        ShowHudNotice("\uD558\uB2E8 \uCE90\uB9AD\uD130\uB97C \uD30C\uB780 \uC2DC\uC791 \uCE78\uC73C\uB85C \uB4DC\uB798\uADF8");
        RefreshHighlights();
        RefreshUnits();
        FocusCameraOnDeploymentOverview(initialEntry ? 0f : 0.18f);
    }

    private void FaceUnitsForDeployment()
    {
        foreach (BattleTestUnit unit in units)
        {
            FaceUnitTowardOpposingSide(unit, true);
        }
    }

    private void FaceUnitTowardOpposingSide(BattleTestUnit unit, bool playIdle)
    {
        if (unit == null || unit.defeated || unit.view == null)
        {
            return;
        }

        Vector2 direction = DirectionToOpposingSide(unit);
        unit.view.FaceDirection(direction);
        if (playIdle)
        {
            unit.view.PlayIdle();
        }
    }

    private Vector2 DirectionToOpposingSide(BattleTestUnit unit)
    {
        if (unit == null || unit.definition == null)
        {
            return Vector2.down;
        }

        Faction opposingFaction = OpposingFaction(unit.definition.faction);
        if (TryGetFactionCenterWorld(opposingFaction, out Vector3 targetWorld))
        {
            Vector3 unitWorld = unit.view != null ? unit.view.transform.position : UnitWorldPosition(unit.cell);
            Vector2 direction = new Vector2(targetWorld.x - unitWorld.x, targetWorld.y - unitWorld.y);
            if (direction.sqrMagnitude > 0.0001f)
            {
                return direction;
            }
        }

        return DefaultFacingForFaction(unit.definition.faction);
    }

    private bool TryGetFactionCenterWorld(Faction faction, out Vector3 center)
    {
        center = Vector3.zero;
        int count = 0;
        foreach (BattleTestUnit unit in units)
        {
            if (unit == null || unit.defeated || unit.definition == null || unit.definition.faction != faction)
            {
                continue;
            }

            center += unit.view != null ? unit.view.transform.position : UnitWorldPosition(unit.cell);
            count++;
        }

        if (count == 0)
        {
            return false;
        }

        center /= count;
        return true;
    }

    private static Faction OpposingFaction(Faction faction)
    {
        return faction == Faction.Enemy ? Faction.Ally : Faction.Enemy;
    }

    private static Vector2 DefaultFacingForFaction(Faction faction)
    {
        return faction == Faction.Enemy ? Vector2.up : Vector2.down;
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

    public void HudToggleObjective()
    {
        showObjectiveOverlay = !showObjectiveOverlay;
        ShowHudNotice(showObjectiveOverlay ? "목표 표시" : "목표 접힘");
        RefreshHighlights();
    }

    public void HudResetBattle()
    {
        BuildBattle();
    }

    public void HudSelectUnit(BattleTestUnit unit)
    {
        SelectPlayerUnit(unit);
    }

    public void HudBeginDeploymentDrag(BattleTestUnit unit)
    {
        if (!scoutMode || unit == null || unit.defeated)
        {
            return;
        }

        activeUnit = unit;
        if (unit.view != null)
        {
            FaceUnitTowardOpposingSide(unit, true);
        }

        ShowHudNotice("\uD30C\uB780 \uC2DC\uC791 \uCE78\uC5D0 \uB193\uC73C\uBA74 \uBC30\uCE58\uB429\uB2C8\uB2E4");
        RefreshHighlights();
        RefreshUnits();
    }

    public void HudDropDeploymentUnit(BattleTestUnit unit, Vector2 screenPosition)
    {
        if (!scoutMode || unit == null || unit.defeated)
        {
            return;
        }

        activeUnit = unit;
        BattleTestTile tile = ResolvePointerTile(screenPosition);
        if (tile != null && TryScoutDeploy(tile))
        {
            ShowHudNotice(unit.definition.displayName + " \uBC30\uCE58 \uC644\uB8CC");
            return;
        }

        ShowHudNotice("\uD30C\uB780 \uC2DC\uC791 \uCE78\uC5D0\uB9CC \uBC30\uCE58\uD560 \uC218 \uC788\uC2B5\uB2C8\uB2E4");
        RefreshHighlights();
        RefreshUnits();
    }

    private void ShowHudNotice(string message)
    {
        hudNotice = message;
        hudNoticeUntil = Time.time + 1.2f;
    }

    private void ClearHudNotice()
    {
        hudNotice = string.Empty;
        hudNoticeUntil = 0f;
    }

    private void CreateTerrain()
    {
        authoredMapBound = false;
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
        authoredMapBound = true;
        return true;
    }

    private TerrainProfile TerrainProfileFromAuthoredCell(TacticalGridCellData data)
    {
        bool objective = string.Equals(data.zoneId, "objective", StringComparison.OrdinalIgnoreCase);
        bool danger = data.hazardType != HazardType.None || data.northEdge == EdgeType.CliffDrop ||
                      data.eastEdge == EdgeType.CliffDrop || data.southEdge == EdgeType.CliffDrop ||
                      data.westEdge == EdgeType.CliffDrop;
        int coverBonus = data.coverBonus > 0 ? data.coverBonus : CoverBonusFromCoverType(data.coverType);
        return new TerrainProfile(data.terrainType, Color.white, data.elevation, coverBonus,
                                  Mathf.Max(1, data.moveCost), data.walkable && !data.blocksMovement,
                                  data.blocksLineOfSight, data.isChokePoint, objective, danger,
                                  string.IsNullOrEmpty(data.laneId) ? "authored" : data.laneId,
                                  string.IsNullOrEmpty(data.decorSetKey) ? data.displayName : data.decorSetKey);
    }

    private TerrainProfile TerrainProfileFromRuntimeCell(BattleMapRuntimeCell data)
    {
        if (data == null)
        {
            return new TerrainProfile(TerrainType.Wall, new Color(0.18f, 0.18f, 0.18f, 1f), 0, 0, 99,
                                      false, true, false, false, true, "missing",
                                      "Missing runtime map data: blocked by default.");
        }

        return new TerrainProfile(data.terrainType, RuntimeTerrainColor(data), data.elevation,
                                  data.coverBonus, Mathf.Max(1, data.moveCost),
                                  data.walkable && data.occupyAllowed, data.blocksLineOfSight,
                                  data.isChokePoint, data.objective, data.danger,
                                  data.laneId, data.tacticalNote);
    }

    private static Color RuntimeTerrainColor(BattleMapRuntimeCell data)
    {
        switch (data.terrainType)
        {
        case TerrainType.Road:
            return new Color(0.58f, 0.53f, 0.44f, 1f);
        case TerrainType.Stone:
        case TerrainType.Gate:
            return new Color(0.56f, 0.52f, 0.47f, 1f);
        case TerrainType.Snow:
            return new Color(0.78f, 0.82f, 0.83f, 1f);
        case TerrainType.Forest:
            return new Color(0.08f, 0.17f, 0.14f, 1f);
        case TerrainType.DeepWater:
            return new Color(0.08f, 0.22f, 0.31f, 1f);
        case TerrainType.Rubble:
            return new Color(0.34f, 0.34f, 0.32f, 1f);
        case TerrainType.Fire:
            return new Color(0.84f, 0.28f, 0.12f, 1f);
        case TerrainType.Cliff:
            return new Color(0.18f, 0.20f, 0.23f, 1f);
        case TerrainType.Wall:
            return new Color(0.24f, 0.23f, 0.22f, 1f);
        default:
            return new Color(0.62f, 0.62f, 0.58f, 1f);
        }
    }

    private void CreateAuthoredTacticalCellCollider(Transform parent, Vector2Int cell, TerrainProfile profile,
                                                    Vector3 worldPosition, TacticalGridCellData cellData)
    {
        GameObject tileObject = new GameObject($"AuthoredTacticalCell_{cell.x}_{cell.y}_{profile.terrain}");
        tileObject.transform.SetParent(parent, false);
        tileObject.transform.position = worldPosition;
        tileObject.transform.localScale = new Vector3(tileWidth, tileWidth, 1f);

        PolygonCollider2D collider = tileObject.AddComponent<PolygonCollider2D>();
        collider.points = BuildIsoCellColliderPoints();

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
        tile.blocksProjectiles = cellData != null && cellData.blocksProjectiles;
        tile.isChokePoint = profile.isChokePoint;
        tile.objective = profile.objective;
        tile.danger = profile.danger;
        tile.occupyAllowed = cellData == null || cellData.occupyAllowed;
        tile.deployZone = cellData == null ? 0 : cellData.deployZone;
        tile.hazardType = cellData == null ? HazardType.None : cellData.hazardType;
        tile.northEdge = cellData == null ? EdgeType.None : cellData.northEdge;
        tile.eastEdge = cellData == null ? EdgeType.None : cellData.eastEdge;
        tile.southEdge = cellData == null ? EdgeType.None : cellData.southEdge;
        tile.westEdge = cellData == null ? EdgeType.None : cellData.westEdge;
        tile.laneId = profile.laneId;
        tile.tacticalNote = profile.tacticalNote;
        if (cellData != null && cellData.tags != null)
        {
            tile.tags.AddRange(cellData.tags);
        }
        tile.tilemapBattlefield = tilemapBattlefield;
        tile.nameLabel = CreateTileNameLabel(tileObject.transform, profile);
        ApplyRuntimeCellMetadata(tile);
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
        tilemapBattlefield.UsePaintedGroundBackdrop = paintedMapBackdropSprite != null;

        CreateMapBackdrop(terrainRoot);
        CreateMapAtmosphere(terrainRoot);
        CreateBattleIntegrationOverlay(terrainRoot);

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
        collider.points = BuildIsoCellColliderPoints();

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
        ApplyRuntimeCellMetadata(tile);
        tiles[cell.x, cell.y] = tile;
    }

    private Vector2[] BuildIsoCellColliderPoints()
    {
        float halfHeight = tileHeight / Mathf.Max(0.01f, tileWidth * 2f);
        return new[] { new Vector2(0f, halfHeight), new Vector2(0.5f, 0f), new Vector2(0f, -halfHeight),
                       new Vector2(-0.5f, 0f) };
    }

    private void CreateLegacyDebugTerrain()
    {
        tiles = new BattleTestTile[width, height];
        Transform terrainRoot = new GameObject("Terrain").transform;
        terrainRoot.SetParent(transform, false);
        CreateMapBackdrop(terrainRoot);
        CreateMapAtmosphere(terrainRoot);
        CreateBattleIntegrationOverlay(terrainRoot);

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
                collider.points = BuildIsoCellColliderPoints();

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
                ApplyRuntimeCellMetadata(tile);
                tiles[x, y] = tile;
            }
        }

        CreateInteractables(terrainRoot);
    }

    private void ApplyRuntimeCellMetadata(BattleTestTile tile)
    {
        if (tile == null)
        {
            return;
        }

        if (BattleMapRuntimeCatalog.TryGetCell(mapVariant, tile.cell, out BattleMapRuntimeCell data))
        {
            tile.terrain = data.terrainType;
            tile.elevation = data.elevation;
            tile.walkable = data.walkable && data.occupyAllowed;
            tile.moveCost = Mathf.Max(1, data.moveCost);
            tile.coverBonus = data.coverBonus;
            tile.baseCoverBonus = data.coverBonus;
            tile.blocksLineOfSight = data.blocksLineOfSight;
            tile.blocksProjectiles = data.blocksProjectiles;
            tile.isChokePoint = data.isChokePoint;
            tile.objective = data.objective;
            tile.danger = data.danger;
            tile.occupyAllowed = data.occupyAllowed;
            tile.deployZone = data.deployZone;
            tile.hazardType = data.hazardType;
            tile.laneId = data.laneId;
            tile.tacticalNote = data.tacticalNote;
            tile.tags.Clear();
            tile.tags.AddRange(data.tags);
        }

        if (runtimeMapEditOverrides.TryGetValue(tile.cell, out BattleMapRuntimeCellEdit edit))
        {
            edit.ApplyTo(tile);
        }
    }

    private void CreateInteractables(Transform terrainRoot)
    {
        Transform propParent = tilemapBattlefield == null || tilemapBattlefield.Binder == null ||
                               tilemapBattlefield.Binder.PropsRoot == null
                                   ? terrainRoot
                                   : tilemapBattlefield.Binder.PropsRoot;
        Transform propRoot = new GameObject("Interactables").transform;
        propRoot.SetParent(propParent, false);

        if (mapVariant == BattleTestMapVariant.BaekduMountainSnowfield)
        {
            CreateBaekduMountainSnowfieldInteractables(propRoot);
            return;
        }

        if (mapVariant == BattleTestMapVariant.BanditLair)
        {
            CreateBanditLairInteractables(propRoot);
            return;
        }

        if (mapVariant == BattleTestMapVariant.WolfPass)
        {
            CreateWolfPassInteractables(propRoot);
            return;
        }

        if (mapVariant == BattleTestMapVariant.TigerRavine)
        {
            CreateTigerRavineInteractables(propRoot);
            return;
        }

        if (mapVariant == BattleTestMapVariant.LeopardCliff)
        {
            CreateLeopardCliffInteractables(propRoot);
            return;
        }

        if (mapVariant == BattleTestMapVariant.SeorakPassRescue)
        {
            CreateSeorakPassRescueInteractables(propRoot);
            return;
        }

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

    private void CreateBaekduMountainSnowfieldInteractables(Transform propRoot)
    {
        AddInteractable(propRoot, "baekdu_broken_snow_gate", "백두 설산 표석", BattleTestInteractableKind.Objective,
                        new Vector2Int(13, 9), new Color(0.86f, 0.78f, 0.55f, 1f));
        AddInteractable(propRoot, "baekdu_hot_spring_steam", "온천 증기", BattleTestInteractableKind.Smoke,
                        new Vector2Int(12, 8), new Color(0.72f, 0.86f, 0.82f, 1f));
        AddInteractable(propRoot, "baekdu_ice_crystal", "푸른 빙정", BattleTestInteractableKind.Rockfall,
                        new Vector2Int(7, 3), new Color(0.44f, 0.74f, 0.95f, 1f));
        AddInteractable(propRoot, "baekdu_snow_pine", "눈덮인 설송", BattleTestInteractableKind.BambooFall,
                        new Vector2Int(2, 9), new Color(0.32f, 0.55f, 0.38f, 1f));
        AddInteractable(propRoot, "baekdu_snow_boulder", "현무암 눈바위", BattleTestInteractableKind.Rockfall,
                        new Vector2Int(11, 8), new Color(0.66f, 0.68f, 0.66f, 1f));
        AddInteractable(propRoot, "baekdu_snowdrift_cover", "쌓인 눈더미", BattleTestInteractableKind.Cover,
                        new Vector2Int(4, 7), new Color(0.82f, 0.88f, 0.92f, 1f));
        AddInteractable(propRoot, "baekdu_frozen_stone_lantern", "얼어붙은 석등", BattleTestInteractableKind.Rockfall,
                        new Vector2Int(9, 9), new Color(0.62f, 0.60f, 0.55f, 1f));
        AddInteractable(propRoot, "baekdu_frozen_rope_posts", "얼어붙은 밧줄 말뚝",
                        BattleTestInteractableKind.CollapseBridge, new Vector2Int(8, 3),
                        new Color(0.45f, 0.35f, 0.22f, 1f));
    }

    private void CreateBanditLairInteractables(Transform propRoot)
    {
        AddInteractable(propRoot, "stolen_cache", "빼앗긴 보급 상자", BattleTestInteractableKind.Objective,
                        new Vector2Int(8, 10), new Color(0.84f, 0.68f, 0.38f, 1f));
        AddInteractable(propRoot, "wine_cart", "넘어진 짐수레", BattleTestInteractableKind.Cover,
                        new Vector2Int(5, 5), new Color(0.54f, 0.32f, 0.18f, 1f));
        AddInteractable(propRoot, "oil_jar", "도적 화약 항아리", BattleTestInteractableKind.Fire,
                        new Vector2Int(9, 7), new Color(0.74f, 0.38f, 0.18f, 1f));
        AddInteractable(propRoot, "lantern", "망루 횃불", BattleTestInteractableKind.Fire,
                        new Vector2Int(13, 8), new Color(1f, 0.42f, 0.16f, 1f));
        AddInteractable(propRoot, "fallen_wall", "쌓아둔 통나무", BattleTestInteractableKind.Cover,
                        new Vector2Int(6, 8), new Color(0.44f, 0.28f, 0.16f, 1f));
        AddInteractable(propRoot, "bridge_rope", "낡은 밧줄 다리", BattleTestInteractableKind.CollapseBridge,
                        new Vector2Int(7, 4), new Color(0.42f, 0.25f, 0.12f, 1f));
        AddInteractable(propRoot, "bamboo_bundle", "쓰러뜨릴 벌목 더미", BattleTestInteractableKind.BambooFall,
                        new Vector2Int(4, 7), new Color(0.28f, 0.46f, 0.20f, 1f));
        AddInteractable(propRoot, "stone_lantern", "굴 입구 낙석", BattleTestInteractableKind.Rockfall,
                        new Vector2Int(10, 10), new Color(0.45f, 0.43f, 0.38f, 1f));
    }

    private void CreateWolfPassInteractables(Transform propRoot)
    {
        AddInteractable(propRoot, "wolf_den_marker", "늑대 굴 봉쇄 지점", BattleTestInteractableKind.Objective,
                        new Vector2Int(12, 10), new Color(0.80f, 0.68f, 0.42f, 1f));
        AddInteractable(propRoot, "bridge_rope", "개울 징검다리", BattleTestInteractableKind.CollapseBridge,
                        new Vector2Int(7, 4), new Color(0.40f, 0.27f, 0.16f, 1f));
        AddInteractable(propRoot, "fallen_wall", "쓰러진 통나무 엄폐", BattleTestInteractableKind.Cover,
                        new Vector2Int(5, 6), new Color(0.46f, 0.30f, 0.16f, 1f));
        AddInteractable(propRoot, "bamboo_bundle", "휘어진 자작나무", BattleTestInteractableKind.BambooFall,
                        new Vector2Int(3, 8), new Color(0.26f, 0.48f, 0.24f, 1f));
        AddInteractable(propRoot, "stone_lantern", "능선 굴러내릴 바위", BattleTestInteractableKind.Rockfall,
                        new Vector2Int(10, 8), new Color(0.48f, 0.46f, 0.40f, 1f));
        AddInteractable(propRoot, "snow_pine", "빽빽한 자작나무", BattleTestInteractableKind.BambooFall,
                        new Vector2Int(2, 9), new Color(0.30f, 0.50f, 0.30f, 1f));
    }

    private void CreateTigerRavineInteractables(Transform propRoot)
    {
        AddInteractable(propRoot, "trapped_villagers", "갇힌 주민", BattleTestInteractableKind.Objective,
                        new Vector2Int(14, 9), new Color(0.86f, 0.70f, 0.40f, 1f));
        AddInteractable(propRoot, "fallen_wall", "큰 바위 엄폐", BattleTestInteractableKind.Cover,
                        new Vector2Int(4, 6), new Color(0.48f, 0.43f, 0.34f, 1f));
        AddInteractable(propRoot, "stone_lantern", "흔들리는 낙석", BattleTestInteractableKind.Rockfall,
                        new Vector2Int(8, 5), new Color(0.52f, 0.48f, 0.40f, 1f));
        AddInteractable(propRoot, "bamboo_bundle", "억새 더미", BattleTestInteractableKind.BambooFall,
                        new Vector2Int(3, 7), new Color(0.52f, 0.58f, 0.28f, 1f));
        AddInteractable(propRoot, "smoke", "흙먼지 구름", BattleTestInteractableKind.Smoke,
                        new Vector2Int(9, 6), new Color(0.58f, 0.54f, 0.46f, 1f));
        AddInteractable(propRoot, "frozen_boulder", "바위 선반 낙석", BattleTestInteractableKind.Rockfall,
                        new Vector2Int(12, 8), new Color(0.55f, 0.52f, 0.45f, 1f));
    }

    private void CreateLeopardCliffInteractables(Transform propRoot)
    {
        AddInteractable(propRoot, "herb_cache", "희귀 약초 군락", BattleTestInteractableKind.Objective,
                        new Vector2Int(14, 8), new Color(0.82f, 0.74f, 0.40f, 1f));
        AddInteractable(propRoot, "bridge_rope", "절벽 밧줄다리", BattleTestInteractableKind.CollapseBridge,
                        new Vector2Int(8, 5), new Color(0.42f, 0.28f, 0.16f, 1f));
        AddInteractable(propRoot, "bamboo_bundle", "대나무 덤불", BattleTestInteractableKind.BambooFall,
                        new Vector2Int(4, 7), new Color(0.20f, 0.50f, 0.28f, 1f));
        AddInteractable(propRoot, "fallen_wall", "절벽길 바위 엄폐", BattleTestInteractableKind.Cover,
                        new Vector2Int(10, 6), new Color(0.46f, 0.42f, 0.34f, 1f));
        AddInteractable(propRoot, "stone_lantern", "떨어질 선반 바위", BattleTestInteractableKind.Rockfall,
                        new Vector2Int(12, 7), new Color(0.50f, 0.46f, 0.39f, 1f));
        AddInteractable(propRoot, "smoke", "절벽 안개", BattleTestInteractableKind.Smoke,
                        new Vector2Int(5, 8), new Color(0.54f, 0.60f, 0.56f, 1f));
    }

    private void CreateSeorakPassRescueInteractables(Transform propRoot)
    {
        AddInteractable(propRoot, "wine_cart", "약초 수레와 피난민", BattleTestInteractableKind.Objective,
                        new Vector2Int(14, 8), new Color(0.86f, 0.74f, 0.42f, 1f));
        AddInteractable(propRoot, "bridge_rope", "설운령 밧줄다리", BattleTestInteractableKind.CollapseBridge,
                        new Vector2Int(8, 5), new Color(0.42f, 0.28f, 0.16f, 1f));
        AddInteractable(propRoot, "bamboo_bundle", "눈 젖은 대나무 덤불", BattleTestInteractableKind.BambooFall,
                        new Vector2Int(4, 7), new Color(0.18f, 0.48f, 0.32f, 1f));
        AddInteractable(propRoot, "fallen_wall", "수레 길 바위 엄폐", BattleTestInteractableKind.Cover,
                        new Vector2Int(10, 6), new Color(0.50f, 0.46f, 0.38f, 1f));
        AddInteractable(propRoot, "stone_lantern", "흔들리는 선반 바위", BattleTestInteractableKind.Rockfall,
                        new Vector2Int(12, 7), new Color(0.55f, 0.52f, 0.45f, 1f));
        AddInteractable(propRoot, "smoke", "서리 안개", BattleTestInteractableKind.Smoke,
                        new Vector2Int(5, 8), new Color(0.56f, 0.64f, 0.66f, 1f));
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
        propObject.transform.position = GridToWorld(cell) + new Vector3(0f, -0.02f, -0.04f);

        if (!ShouldShowRuntimeInteractableSprites())
        {
            AttachMapPropComponents(propObject, id, displayName, kind, cell);
            return;
        }

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
    }

    private bool ShouldShowRuntimeInteractableSprites()
    {
        return paintedMapBackdropSprite == null;
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

        SpriteRenderer renderer = backdrop.AddComponent<SpriteRenderer>();
        if (paintedMapBackdropSprite != null)
        {
            backdrop.transform.position = center + new Vector3(0f, 0.25f, 0.12f);
            renderer.sprite = paintedMapBackdropSprite;
            renderer.color = new Color(1f, 0.98f, 0.92f, 1f);
            renderer.sortingOrder = -96;

            float playableWidth = Mathf.Abs(right.x - left.x) + tileWidth * 4.30f;
            float playableHeight = Mathf.Abs(max.y - min.y) + tileHeight * 4.10f;
            float spriteWidth = Mathf.Max(0.01f, paintedMapBackdropSprite.bounds.size.x);
            float spriteHeight = Mathf.Max(0.01f, paintedMapBackdropSprite.bounds.size.y);
            float uniformScale = Mathf.Max(playableWidth / spriteWidth, playableHeight / spriteHeight);
            backdrop.transform.localScale = Vector3.one * uniformScale;
            return;
        }

        backdrop.transform.position = center + new Vector3(0f, -0.18f, 0.08f);
        backdrop.transform.localScale = new Vector3(width * tileWidth * 2.40f, height * tileHeight * 2.80f, 1f);
        backdrop.transform.rotation = Quaternion.Euler(0f, 0f, 45f);
        renderer.sprite = detailSprite;
        renderer.color = new Color(0.18f, 0.27f, 0.23f, 0.82f);
        renderer.sortingOrder = -80;

        CreateAtmosphereSprite(terrainRoot, "Dawn Mountain Sky Wash", softDiamondSprite,
                               center + new Vector3(0f, 1.35f, 0.10f),
                               new Vector3(width * tileWidth * 2.85f, height * tileHeight * 3.15f, 1f), 45f,
                               new Color(0.36f, 0.50f, 0.50f, 0.05f), -92);
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

    private void CreateBattleIntegrationOverlay(Transform terrainRoot)
    {
        if (terrainRoot == null)
        {
            return;
        }

        Sprite overlaySprite = GetBattleIntegrationOverlaySprite();
        if (overlaySprite == null)
        {
            return;
        }

        Vector3 min = GridToWorld(Vector2Int.zero);
        Vector3 max = GridToWorld(new Vector2Int(width - 1, height - 1));
        Vector3 left = GridToWorld(new Vector2Int(0, height - 1));
        Vector3 right = GridToWorld(new Vector2Int(width - 1, 0));
        Vector3 center = (min + max + left + right) * 0.25f;

        float playableWidth = Mathf.Abs(right.x - left.x) + tileWidth * 6.0f;
        float playableHeight = Mathf.Abs(max.y - min.y) + tileHeight * 8.0f;
        Bounds spriteBounds = overlaySprite.bounds;

        GameObject overlay = new GameObject("Battle Scene Integration Overlay");
        overlay.transform.SetParent(terrainRoot, false);
        overlay.transform.position = center + new Vector3(0f, 0.22f, -0.16f);
        overlay.transform.localScale =
            new Vector3(playableWidth / Mathf.Max(0.001f, spriteBounds.size.x),
                        playableHeight / Mathf.Max(0.001f, spriteBounds.size.y),
                        1f);

        SpriteRenderer renderer = overlay.AddComponent<SpriteRenderer>();
        renderer.sprite = overlaySprite;
        renderer.color = BattleIntegrationOverlayTint();
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = 5200;
    }

    private void CreateMapAtmosphere(Transform terrainRoot)
    {
        if (paintedMapBackdropSprite != null)
        {
            return;
        }

        Transform atmosphereRoot = new GameObject("Painted Atmosphere").transform;
        atmosphereRoot.SetParent(terrainRoot, false);

        if (mapVariant == BattleTestMapVariant.BaekduMountainSnowfield)
        {
            CreateBaekduSnowfieldAtmosphere(atmosphereRoot);
            return;
        }

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

    private void CreateBaekduSnowfieldAtmosphere(Transform atmosphereRoot)
    {
        CreateZoneWash(atmosphereRoot, "Dense Snow Pine Wash", new Vector2Int(1, 8), new Vector2(4.20f, 4.80f),
                       -14f, new Color(0.04f, 0.18f, 0.16f, 0.035f), 1120, true);
        CreateZoneWash(atmosphereRoot, "Frozen Stream Wash", new Vector2Int(7, 3), new Vector2(5.30f, 2.20f),
                       8f, new Color(0.22f, 0.58f, 0.76f, 0.035f), 1121, true);
        CreateZoneWash(atmosphereRoot, "Basalt Cliff Wash", new Vector2Int(7, 10), new Vector2(6.10f, 2.40f),
                       0f, new Color(0.08f, 0.10f, 0.13f, 0.03f), 1122, true);
        CreateZoneWash(atmosphereRoot, "Hot Spring Steam Wash", new Vector2Int(12, 8), new Vector2(3.40f, 2.35f),
                       -8f, new Color(0.78f, 0.92f, 0.86f, 0.055f), 1123, false);
        CreateMistBand(atmosphereRoot, "Snow Drift Mist", new Vector2Int(4, 7), new Vector3(-0.18f, 0.06f, -0.03f),
                       new Vector3(2.20f, 0.20f, 1f), -16f, new Color(0.82f, 0.88f, 0.92f, 0.035f), 1240);
        CreateMistBand(atmosphereRoot, "Hot Spring Vapor", new Vector2Int(12, 8), new Vector3(0.10f, 0.13f, -0.03f),
                       new Vector3(1.75f, 0.22f, 1f), 18f, new Color(0.82f, 0.95f, 0.90f, 0.055f), 1241);
        CreateGlow(atmosphereRoot, "Hot Spring Glow", new Vector2Int(12, 8), new Color(0.82f, 0.95f, 0.86f, 0.12f),
                   1.12f, 1360);
        CreateGlow(atmosphereRoot, "Summit Marker Halo", new Vector2Int(13, 9), new Color(1f, 0.86f, 0.48f, 0.10f),
                   1.02f, 1361);
    }

    private Color BattleAmbientTint()
    {
        switch (mapVariant)
        {
        case BattleTestMapVariant.BaekduSnowGate:
        case BattleTestMapVariant.BaekduMountainSnowfield:
        case BattleTestMapVariant.SeorakPassRescue:
            return new Color(0.78f, 0.82f, 0.90f, 1f);
        case BattleTestMapVariant.BanditLair:
            return new Color(0.78f, 0.74f, 0.68f, 1f);
        case BattleTestMapVariant.WolfPass:
            return new Color(0.82f, 0.83f, 0.76f, 1f);
        default:
            return new Color(0.84f, 0.80f, 0.74f, 1f);
        }
    }

    private Color BattleGroundBlendTint()
    {
        switch (mapVariant)
        {
        case BattleTestMapVariant.BaekduSnowGate:
        case BattleTestMapVariant.BaekduMountainSnowfield:
        case BattleTestMapVariant.SeorakPassRescue:
            return new Color(0.25f, 0.31f, 0.42f, 0.22f);
        case BattleTestMapVariant.BanditLair:
            return new Color(0.26f, 0.20f, 0.15f, 0.23f);
        case BattleTestMapVariant.WolfPass:
            return new Color(0.20f, 0.24f, 0.17f, 0.21f);
        default:
            return new Color(0.27f, 0.23f, 0.18f, 0.21f);
        }
    }

    private Color BattleFootOcclusionTint()
    {
        switch (mapVariant)
        {
        case BattleTestMapVariant.BaekduSnowGate:
        case BattleTestMapVariant.BaekduMountainSnowfield:
        case BattleTestMapVariant.SeorakPassRescue:
            return new Color(0.74f, 0.82f, 0.92f, 0.38f);
        case BattleTestMapVariant.BanditLair:
            return new Color(0.24f, 0.18f, 0.13f, 0.34f);
        case BattleTestMapVariant.WolfPass:
            return new Color(0.16f, 0.24f, 0.14f, 0.32f);
        default:
            return new Color(0.23f, 0.20f, 0.15f, 0.32f);
        }
    }

    private Color BattleIntegrationOverlayTint()
    {
        switch (mapVariant)
        {
        case BattleTestMapVariant.BaekduSnowGate:
        case BattleTestMapVariant.BaekduMountainSnowfield:
        case BattleTestMapVariant.SeorakPassRescue:
            return new Color(0.88f, 0.92f, 1f, 1f);
        case BattleTestMapVariant.BanditLair:
            return new Color(0.98f, 0.89f, 0.78f, 1f);
        default:
            return new Color(0.96f, 0.94f, 0.86f, 1f);
        }
    }

    private void ApplyBattleSceneIntegration(CharacterVisualController visual)
    {
        if (visual == null)
        {
            return;
        }

        visual.SetSceneIntegration(BattleAmbientTint(), BattleGroundBlendTint(), 0.13f, BattleFootOcclusionTint());
    }

    private static Sprite GetBattleIntegrationOverlaySprite()
    {
        if (battleIntegrationOverlaySprite != null)
        {
            return battleIntegrationOverlaySprite;
        }

        Texture2D texture = new Texture2D(512, 512, TextureFormat.RGBA32, false);
        texture.name = "GeneratedBattleSceneIntegrationOverlay";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                float nx = ((x + 0.5f) / texture.width * 2f) - 1f;
                float ny = ((y + 0.5f) / texture.height * 2f) - 1f;
                float radius = Mathf.Sqrt((nx * nx * 0.86f) + (ny * ny * 1.18f));
                float vignette = Mathf.SmoothStep(0.48f, 1.08f, radius) * 0.32f;
                float grain = Mathf.PerlinNoise((x * 0.115f) + 13.7f, (y * 0.115f) + 31.4f);
                float fiber = Mathf.PerlinNoise((x * 0.031f) + 5.2f, (y * 0.19f) + 9.6f);
                float paperAlpha = Mathf.Clamp01(0.024f + (grain * 0.018f) + (fiber * 0.010f));
                float alpha = Mathf.Clamp01(paperAlpha + vignette);
                float vignetteWeight = Mathf.Clamp01(vignette / Mathf.Max(0.001f, alpha));
                Color paper = new Color(0.78f, 0.70f, 0.56f, alpha);
                Color ink = new Color(0.025f, 0.022f, 0.020f, alpha);
                texture.SetPixel(x, y, Color.Lerp(paper, ink, vignetteWeight));
            }
        }

        texture.Apply();
        battleIntegrationOverlaySprite =
            Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 128f);
        battleIntegrationOverlaySprite.name = "GeneratedBattleSceneIntegrationOverlay";
        return battleIntegrationOverlaySprite;
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
            text = "목표";
        }
        else if (profile.isChokePoint)
        {
            text = "병목";
        }
        else if (profile.elevation >= 2)
        {
            text = "고지" + profile.elevation;
        }
        else if (profile.blocksLineOfSight)
        {
            text = "차단";
        }
        else if (profile.danger)
        {
            text = "위험";
        }

        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        TextMesh label = CreateWorldLabel(parent, text, text.Length > 2 ? 34 : 38, new Vector3(0f, 0.03f, -0.06f),
                                          new Color(0.98f, 0.92f, 0.70f, 0.88f), 1800);
        label.characterSize = text.Length > 2 ? 0.014f : 0.0155f;
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
        HashSet<Vector2Int> occupiedSpawnCells = new HashSet<Vector2Int>();

        foreach (BattleTestUnitDefinition definition in unitDefinitions)
        {
            if (definition == null)
            {
                continue;
            }

            if (!TryResolveInitialSpawnCell(definition, occupiedSpawnCells, out Vector2Int spawnCell))
            {
                AddLog($"[배치] {definition.displayName} 배치 가능한 시작 칸을 찾지 못했습니다.");
                continue;
            }

            if (spawnCell != definition.startCell)
            {
                AddLog($"[배치] {definition.displayName} 시작 위치 보정: ({definition.startCell.x},{definition.startCell.y}) -> ({spawnCell.x},{spawnCell.y})");
            }

            GameObject unitObject = new GameObject(definition.displayName);
            unitObject.transform.SetParent(unitRoot, false);
            unitObject.transform.position = UnitWorldPosition(spawnCell);

            CharacterVisualController visual = unitObject.AddComponent<CharacterVisualController>();
            visual.visual = definition.visual;
            visual.sortingLayerName = "Default";
            // Authored painted maps are a flattened illustration. Terrain legality blocks wall/prop cells,
            // so legal units must render above the foreground paint instead of hiding behind it.
            visual.baseSortingOrder = authoredMapBound ? 2600 : 3000;
            ApplyBattleSceneIntegration(visual);

            CircleCollider2D collider = unitObject.AddComponent<CircleCollider2D>();
            collider.radius = 0.22f;
            collider.offset = new Vector2(0f, 0.24f);

            BattleTestUnitView view = unitObject.AddComponent<BattleTestUnitView>();
            BattleTestUnit unit = new BattleTestUnit(definition, view);
            unit.cell = spawnCell;
            unit.initiative = definition.initiative + random.Next(0, 5);
            view.Bind(unit, visual);
            view.FaceDirection(DefaultFacingForFaction(definition.faction));
            view.PlayIdle();
            units.Add(unit);
            occupiedSpawnCells.Add(spawnCell);
        }

        FaceUnitsForDeployment();
    }

    private bool TryResolveInitialSpawnCell(BattleTestUnitDefinition definition, HashSet<Vector2Int> occupied,
                                            out Vector2Int spawnCell)
    {
        Vector2Int preferred = definition.startCell;
        if (definition.faction == Faction.Ally)
        {
            if (TryFindNearestSpawnCell(preferred, occupied, true, out spawnCell))
            {
                return true;
            }
        }
        else if (IsValidInitialSpawnCell(preferred, occupied, false))
        {
            spawnCell = preferred;
            return true;
        }

        return TryFindNearestSpawnCell(preferred, occupied, false, out spawnCell);
    }

    private bool TryFindNearestSpawnCell(Vector2Int preferred, HashSet<Vector2Int> occupied, bool deploymentOnly,
                                         out Vector2Int spawnCell)
    {
        spawnCell = preferred;
        int bestScore = int.MaxValue;
        bool found = false;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2Int candidate = new Vector2Int(x, y);
                if (!IsValidInitialSpawnCell(candidate, occupied, deploymentOnly))
                {
                    continue;
                }

                int score = GridDistance(preferred, candidate) * 100 + y * 4 + x;
                if (score >= bestScore)
                {
                    continue;
                }

                bestScore = score;
                spawnCell = candidate;
                found = true;
            }
        }

        return found;
    }

    private bool IsValidInitialSpawnCell(Vector2Int cell, HashSet<Vector2Int> occupied, bool deploymentOnly)
    {
        BattleTestTile tile = TileAt(cell);
        if (!CanStandOnTile(tile) || IsCellBlockedByInteractable(cell) || IsCellUnsafeForInitialSpawn(cell) ||
            occupied.Contains(cell))
        {
            return false;
        }

        return !deploymentOnly || IsDeploymentCell(cell);
    }

    private bool IsCellUnsafeForInitialSpawn(Vector2Int cell)
    {
        foreach (BattleTestInteractable interactable in interactables)
        {
            if (interactable == null || interactable.used)
            {
                continue;
            }

            if (interactable.cell == cell)
            {
                return true;
            }

            if (IsLargeInitialSpawnAvoidanceProp(interactable) && ChebyshevDistance(interactable.cell, cell) <= 1)
            {
                return true;
            }
        }

        return false;
    }

    private bool CanStandOnCell(Vector2Int cell)
    {
        return CanStandOnTile(TileAt(cell));
    }

    private bool CanStandOnTile(BattleTestTile tile)
    {
        return BattlePathService.CanStandOnTile(tile);
    }

    private static bool IsLargeInitialSpawnAvoidanceProp(BattleTestInteractable interactable)
    {
        if (interactable == null)
        {
            return false;
        }

        switch (interactable.kind)
        {
        case BattleTestInteractableKind.Cover:
        case BattleTestInteractableKind.CollapseBridge:
        case BattleTestInteractableKind.BambooFall:
        case BattleTestInteractableKind.Rockfall:
            return true;
        default:
            return false;
        }
    }

    private void BeginPlayerPhase()
    {
        if (CheckBattleEnd())
        {
            return;
        }

        pendingMovementUndo = default;
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
        commandMode = DefaultCommandForUnit(activeUnit);
        AddLog($"[페이즈] 제 {round}턴 아군 페이즈");
        RefreshHighlights();
        RefreshUnits();
        if (activeUnit != null)
        {
            activeUnit.view.PlayTurnStart();
        }
        if (!suppressCameraFocus)
        {
            FocusCameraOnUnit(activeUnit, 0.36f);
        }
    }

    private void BeginEnemyPhase()
    {
        if (CheckBattleEnd())
        {
            return;
        }

        pendingMovementUndo = default;
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

        if (activeUnit != null && phaseTurn.CanPlayerControl(activeUnit))
        {
            WaitActiveUnit();
            return;
        }

        EndPlayerPhase();
    }

    private void WaitActiveUnit()
    {
        if (activeUnit == null || !phaseTurn.CanPlayerControl(activeUnit))
        {
            return;
        }

        BattleTestUnit waitingUnit = activeUnit;
        CommitPendingMove(waitingUnit);
        waitingUnit.view.PlayWait();
        waitingUnit.SpendMainAction();
        AddLog($"[대기] {waitingUnit.definition.displayName} 행동 종료.");
        AdvanceAfterAction(waitingUnit);
    }

    private void EndPlayerPhase()
    {
        pendingMovementUndo = default;
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

        clickedTile = ResolveTileFromPointer(point, hits);

        // 유닛 위를 클릭하면 배치/이동 스냅이 클릭을 가로채지 않고 '선택'으로 처리한다.
        // 배치 단계: 맵 캐릭터 클릭 = 배치 변경 대상 선택, 빈 파란칸 클릭 = 그 유닛 재배치.
        // 액션 단계: 유닛 클릭 = 선택/공격, 빈 칸 클릭 = 이동.
        BattleTestUnit unitAtClick = clickedUnit != null
                                         ? clickedUnit
                                         : (clickedTile != null ? UnitAt(clickedTile.cell) : null);
        bool clickedOnUnit = unitAtClick != null && !unitAtClick.defeated;

        if (scoutMode && activeUnit != null && !clickedOnUnit)
        {
            BattleTestTile deploymentDestination = ResolveDeploymentPointerDestination(point, clickedTile);
            if (deploymentDestination != null && TryScoutDeploy(deploymentDestination))
            {
                SetInspectedTarget(null, deploymentDestination, screenPosition);
                return;
            }
        }

        if (IsMovementPreviewActive() && !clickedOnUnit)
        {
            BattleTestTile moveDestination = ResolveMovePointerDestination(activeUnit, point, clickedTile);
            if (moveDestination != null)
            {
                SetInspectedTarget(null, moveDestination, screenPosition);
                TryMove(activeUnit, moveDestination);
                return;
            }
        }

        clickedUnit = ResolveClickedUnit(clickedUnit, clickedTile);
        SetInspectedTarget(clickedUnit, clickedTile, screenPosition);

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

            AddLog("[\uBC30\uCE58] \uC544\uAD70\uC744 \uC120\uD0DD\uD558\uACE0 \uD30C\uB780 \uC2DC\uC791 \uCE78\uC744 \uD074\uB9AD\uD558\uC138\uC694.");
            return;
        }

        if (commandMode == BattleCommandMode.Attack)
        {
            BattleTestUnit attackTarget = ResolveAttackClickTarget(activeUnit, clickedUnit, clickedTile);
            if (attackTarget != null)
            {
                TryAttack(activeUnit, attackTarget, false);
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

        if (clickedUnit != null && clickedUnit.definition.faction != activeUnit.definition.faction)
        {
            SetCommandMode(BattleCommandMode.Attack);
            if (CanBasicAttackTarget(activeUnit, clickedUnit))
            {
                TryAttack(activeUnit, clickedUnit, false);
            }

            return;
        }

        if (commandMode == BattleCommandMode.Move && clickedTile != null)
        {
            TryMove(activeUnit, clickedTile);
            return;
        }
    }

    private BattleTestTile ResolvePointerTile(Vector3 screenPosition)
    {
        if (Camera.main == null)
        {
            return null;
        }

        Vector3 world = Camera.main.ScreenToWorldPoint(screenPosition);
        Vector2 point = new Vector2(world.x, world.y);
        Collider2D[] hits = Physics2D.OverlapPointAll(point);

        BattleTestTile clickedTile = ResolveTileFromPointer(point, hits);
        if (scoutMode && activeUnit != null)
        {
            return ResolveDeploymentPointerDestination(point, clickedTile) ?? clickedTile;
        }

        return clickedTile;
    }

    private BattleTestTile ResolveTileFromPointer(Vector2 point, Collider2D[] hits)
    {
        if (hits != null)
        {
            foreach (Collider2D hit in hits)
            {
                BattleTestTile tile = hit.GetComponent<BattleTestTile>();
                if (tile != null)
                {
                    return tile;
                }
            }
        }

        return TileAt(WorldToGrid(point));
    }

    private BattleTestTile ResolveMovePointerDestination(BattleTestUnit unit, Vector2 point,
                                                        BattleTestTile clickedTile)
    {
        if (unit == null)
        {
            return null;
        }

        Dictionary<Vector2Int, int> reachable = GetReachableCells(unit);
        if (CanUseMoveDestination(unit, clickedTile, reachable, false))
        {
            return clickedTile;
        }

        return FindPointerTile(point, tile => CanUseMoveDestination(unit, tile, reachable, false));
    }

    private BattleTestTile ResolveDeploymentPointerDestination(Vector2 point, BattleTestTile clickedTile)
    {
        if (activeUnit != null && clickedTile != null && clickedTile.cell == activeUnit.cell)
        {
            BattleTestTile nearbyDestination =
                FindPointerTile(point, tile => tile.cell != activeUnit.cell && CanUseDeploymentDestination(tile));
            if (nearbyDestination != null)
            {
                return nearbyDestination;
            }
        }

        if (CanUseDeploymentDestination(clickedTile))
        {
            return clickedTile;
        }

        return FindPointerTile(point, CanUseDeploymentDestination);
    }

    private BattleTestTile FindPointerTile(Vector2 point, Func<BattleTestTile, bool> predicate)
    {
        if (tiles == null || predicate == null)
        {
            return null;
        }

        BattleTestTile best = null;
        float bestScore = 1.10f;
        float bestDistance = float.MaxValue;
        float halfWidth = Mathf.Max(0.001f, tileWidth * 0.5f);
        float halfHeight = Mathf.Max(0.001f, tileHeight * 0.5f);

        foreach (BattleTestTile tile in tiles)
        {
            if (tile == null || !predicate(tile))
            {
                continue;
            }

            Vector3 world = GridToWorld(tile.cell);
            float score = Mathf.Abs(point.x - world.x) / halfWidth +
                          Mathf.Abs(point.y - world.y) / halfHeight;
            if (score > 1.10f)
            {
                continue;
            }

            float distance = ((Vector2)world - point).sqrMagnitude;
            if (score < bestScore || (Mathf.Approximately(score, bestScore) && distance < bestDistance))
            {
                best = tile;
                bestScore = score;
                bestDistance = distance;
            }
        }

        return best;
    }

    private void SelectPlayerUnit(BattleTestUnit unit)
    {
        if (!phaseTurn.CanPlayerControl(unit))
        {
            AddLog("[UI] 이미 행동한 아군입니다.");
            return;
        }

        if (pendingMovementUndo.active && pendingMovementUndo.unit != unit)
        {
            CommitPendingMove(pendingMovementUndo.unit);
        }

        activeUnit = unit;
        commandMode = DefaultCommandForUnit(unit);
        AddLog($"[선택] {unit.definition.displayName}");
        RefreshHighlights();
        RefreshUnits();
        unit.view.PlayClickReaction();
        if (scoutMode)
        {
            FocusCameraOnDeploymentOverview(0.16f);
        }
        else
        {
            FocusCameraOnUnit(unit, 0.24f);
        }
    }

    private void SetInspectedTarget(BattleTestUnit unit, BattleTestTile tile, Vector3 screenPosition)
    {
        inspectedUnit = unit != null && !unit.defeated ? unit : null;
        inspectedTile = inspectedUnit == null ? tile : null;
        inspectedScreenPosition = inspectedUnit != null || inspectedTile != null ? screenPosition : Vector3.zero;
    }

    private bool TryScoutDeploy(BattleTestTile destination)
    {
        if (!scoutMode || activeUnit == null || destination == null)
        {
            return false;
        }

        if (!IsDeploymentCell(destination.cell))
        {
            AddLog("[\uBC30\uCE58] \uD30C\uB780 \uC2DC\uC791 \uAD6C\uC5ED\uC5D0\uB9CC \uBC30\uCE58\uD560 \uC218 \uC788\uC2B5\uB2C8\uB2E4.");
            return false;
        }

        BattleTestUnit occupant = UnitAt(destination.cell);
        if (!CanStandOnTile(destination) || IsCellBlockedByInteractable(destination.cell) ||
            (occupant != null && occupant != activeUnit))
        {
            AddLog("[\uBC30\uCE58] \uD574\uB2F9 \uC2DC\uC791 \uCE78\uC740 \uC0AC\uC6A9\uD560 \uC218 \uC5C6\uC2B5\uB2C8\uB2E4.");
            return false;
        }

        activeUnit.cell = destination.cell;
        if (activeUnit.view != null)
        {
            activeUnit.view.transform.position = UnitWorldPosition(destination.cell);
            FaceUnitTowardOpposingSide(activeUnit, true);
        }

        AddLog($"[\uBC30\uCE58] {activeUnit.definition.displayName} \uBC30\uCE58 -> ({destination.cell.x},{destination.cell.y})");
        RefreshHighlights();
        RefreshUnits();
        FocusCameraOnDeploymentOverview(0.12f);
        return true;
    }

    private bool IsDeploymentCell(Vector2Int cell)
    {
        BattleTestTile tile = TileAt(cell);
        if (tile == null)
        {
            return false;
        }

        if (tile.deployZone > 0)
        {
            return true;
        }

        if (mapVariant == BattleTestMapVariant.BaekduSnowGate)
        {
            return IsBaekduSnowGateDeploymentStartCell(cell.x, cell.y);
        }

        return IsFrontDescentDeploymentCell(cell);
    }

    private static bool IsFrontDescentDeploymentCell(Vector2Int cell)
    {
        return cell.y >= 7 && cell.y <= 9 && cell.x >= 5 && cell.x <= 12;
    }

    private bool CanUseDeploymentDestination(BattleTestTile destination)
    {
        if (destination == null || !IsDeploymentCell(destination.cell))
        {
            return false;
        }

        BattleTestUnit occupant = UnitAt(destination.cell);
        return CanStandOnTile(destination) && !IsCellBlockedByInteractable(destination.cell) &&
               (occupant == null || occupant == activeUnit);
    }

    private void TryMove(BattleTestUnit unit, BattleTestTile destination)
    {
        if (!unit.CanMove)
        {
            AddLog("[이동] 이미 이동했습니다.");
            return;
        }

        if (destination == null || destination.cell == unit.cell ||
            !CanEnterCellForMovement(unit, destination.cell))
        {
            AddLog("[이동] 진입할 수 없는 칸입니다.");
            return;
        }

        Dictionary<Vector2Int, int> reachable = GetReachableCells(unit);
        if (!CanUseMoveDestination(unit, destination, reachable, false))
        {
            AddLog("[이동] 이동 범위를 벗어났습니다.");
            return;
        }

        List<Vector2Int> path = FindMovePath(unit, destination.cell);
        if (path.Count < 2)
        {
            AddLog("[Move] No valid path to destination.");
            return;
        }

        if (unit.definition.faction == Faction.Ally && phaseTurn.IsPlayerPhase)
        {
            pendingMovementUndo = MovementUndoState.Capture(unit);
        }
        else
        {
            pendingMovementUndo = default;
        }

        unit.cell = destination.cell;
        unit.SpendMovement(reachable[destination.cell]);
        ApplyTileEntry(unit, destination);
        commandMode = DefaultCommandForUnit(unit);

        if (unit.defeated)
        {
            // 이동 중 지형 피해로 쓰러지면 걷기 연출/후속 행동 없이 위치만 정리하고 턴을 진행한다.
            pendingMovementUndo = default;
            if (unit.view != null)
            {
                unit.view.transform.position = UnitWorldPosition(destination.cell);
            }

            AddLog($"[이동] {unit.definition.displayName} 이동 중 지형 피해로 쓰러졌습니다.");
            RefreshHighlights();
            RefreshUnits();
            if (!CheckBattleEnd())
            {
                AdvanceAfterAction(unit);
            }

            return;
        }

        if (Application.isPlaying)
        {
            StartCoroutine(AnimateMove(unit, path));
        }
        else
        {
            FaceUnitAlongPath(unit, path);
            unit.view.transform.position = UnitWorldPosition(destination.cell);
            FocusCameraOnUnit(unit, 0f);
            RefreshHighlights();
            RefreshUnits();
        }
        AddLog($"[이동] {unit.definition.displayName} 이동.");
    }

    private bool TryUndoPendingMove(BattleTestUnit unit)
    {
        if (!CanUndoPendingMove(unit))
        {
            return false;
        }

        RestoreMovementUndo(pendingMovementUndo);
        pendingMovementUndo = default;
        activeUnit = unit;
        commandMode = BattleCommandMode.Move;
        AddLog("[Move] Movement canceled.");
        RefreshHighlights();
        RefreshUnits();
        FocusCameraOnUnit(unit, 0.12f);
        return true;
    }

    private bool CanUndoPendingMove(BattleTestUnit unit)
    {
        return pendingMovementUndo.active && pendingMovementUndo.unit == unit && unit != null &&
               !busy && !battleOver && phaseTurn.IsPlayerPhase &&
               unit.definition.faction == Faction.Ally && !unit.acted;
    }

    private void CommitPendingMove(BattleTestUnit unit)
    {
        if (pendingMovementUndo.active && (unit == null || pendingMovementUndo.unit == unit))
        {
            pendingMovementUndo = default;
        }
    }

    private void RestoreMovementUndo(MovementUndoState state)
    {
        BattleTestUnit unit = state.unit;
        if (unit == null)
        {
            return;
        }

        unit.cell = state.cell;
        unit.hp = state.hp;
        unit.inner = state.inner;
        unit.specialCooldownLeft = state.specialCooldownLeft;
        unit.moved = state.moved;
        unit.acted = state.acted;
        unit.defeated = state.defeated;
        unit.guarded = state.guarded;
        unit.poisoned = state.poisoned;
        unit.poisonTurnsLeft = state.poisonTurnsLeft;
        unit.chilled = state.chilled;
        unit.chilledTurnsLeft = state.chilledTurnsLeft;
        unit.marked = state.marked;
        unit.actions.mainAction = state.mainAction;
        unit.actions.bonusAction = state.bonusAction;
        unit.actions.reaction = state.reaction;
        unit.actions.movementLeft = state.movementLeft;

        if (unit.view != null)
        {
            unit.view.SetDefeated(unit.defeated);
            unit.view.transform.position = UnitWorldPosition(unit.cell);
            if (state.hasFacingDirection)
            {
                unit.view.FaceDirection(state.facingDirection);
            }
            if (!unit.defeated)
            {
                unit.view.PlayIdle();
            }
        }
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

        BattleTargetingForecast forecast =
            BattleTargetingService.CanAttack(attacker, target, EffectiveAttackRange(attacker), TileAt, IsInside);
        if (!forecast.canTarget)
        {
            AddLog("[공격] " + TargetingReasonText(forecast.reason));
            return false;
        }

        CommitPendingMove(attacker);

        if (Application.isPlaying)
        {
            StartCoroutine(RunAttackCommand(attacker, target, false, endAfterAttack));
            return true;
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

    private bool CanBasicAttackTarget(BattleTestUnit attacker, BattleTestUnit target)
    {
        if (attacker == null || target == null || attacker.defeated || target.defeated ||
            attacker.definition.faction == target.definition.faction || !attacker.CanUseMainAction)
        {
            return false;
        }

        return BattleTargetingService.CanAttack(attacker, target, EffectiveAttackRange(attacker), TileAt, IsInside)
                                     .canTarget;
    }

    private bool ResolveAttack(BattleTestUnit attacker, BattleTestUnit target, bool special)
    {
        attacker.view.FaceToward(target.view.transform.position);
        target.view.FaceToward(attacker.view.transform.position);
        if (special)
        {
            attacker.view.PlaySkill();
        }
        else
        {
            attacker.view.PlayAttack();
        }

        BattleTestAttackResult result = RollAttackResult(attacker, target, special);
        ApplyAttackResultAtHitFrame(result);
        return result.hit;
    }

    private struct BattleTestAttackResult
    {
        public BattleTestUnit attacker;
        public BattleTestUnit target;
        public bool special;
        public string moveName;
        public bool hit;
        public bool critical;
        public int damage;

        public BattleTestAttackResult(BattleTestUnit attacker, BattleTestUnit target, bool special, string moveName,
                                      bool hit, bool critical, int damage)
        {
            this.attacker = attacker;
            this.target = target;
            this.special = special;
            this.moveName = moveName;
            this.hit = hit;
            this.critical = critical;
            this.damage = damage;
        }
    }

    private IEnumerator RunAttackCommand(BattleTestUnit attacker, BattleTestUnit target, bool special, bool endAfterAttack)
    {
        StopCameraPanRoutine();
        ClearHudNotice();
        busy = true;
        if (special)
        {
            SpendSpecialResource(attacker);
            PrepareSpecialBeforeAttack(attacker, target);
        }

        BattleTestAttackResult result = default;
        yield return ExecuteAttackSequence(attacker, target, special, resolved => result = resolved);
        if (special)
        {
            ApplySpecialStatusAfterHit(result);
        }

        yield return RunPostAttackSequence(attacker, target, special);
        attacker.SpendMainAction();
        RefreshHighlights();
        RefreshUnits();

        if (CheckBattleEnd())
        {
            busy = false;
            yield break;
        }

        if (!AdvanceAfterAction(attacker))
        {
            if (endAfterAttack)
            {
                EndTurn();
            }
            else
            {
                RefreshHighlights();
            }
        }

        busy = false;
    }

    private IEnumerator RunMotionDebugAttackSequence(BattleTestUnit attacker, BattleTestUnit target, bool special)
    {
        if (attacker == null || target == null || attacker.defeated || target.defeated ||
            attacker.view == null || target.view == null)
        {
            yield break;
        }

        StopCameraPanRoutine();
        ClearHudNotice();
        busy = true;
        AddLog(special ? "[MotionTest] Forced skill presentation." : "[MotionTest] Forced attack presentation.");

        BattleTestAttackResult result = default;
        yield return ExecuteAttackSequence(attacker, target, special, resolved => result = resolved, true);
        if (special)
        {
            ApplySpecialStatusAfterHit(result);
        }

        RefreshHighlights();
        RefreshUnits();
        CheckBattleEnd();
        busy = false;
    }

    private IEnumerator RunEnemyActionCommand(BattleTestUnit actor, BattleTestUnit target, bool special)
    {
        if (actor == null || target == null || actor.defeated || target.defeated)
        {
            yield break;
        }

        bool specialAttackLike = special && IsHostileAttackSpecial(actor.definition.specialEffect);
        if (special)
        {
            SpendSpecialResource(actor);
        }

        if (specialAttackLike)
        {
            PrepareSpecialBeforeAttack(actor, target);
            BattleTestAttackResult result = default;
            yield return ExecuteAttackSequence(actor, target, true, resolved => result = resolved);
            ApplySpecialStatusAfterHit(result);
            yield return RunPostAttackSequence(actor, target, true);
        }
        else if (special)
        {
            actor.view.FaceToward(target.view.transform.position);
            actor.view.PlaySkill();
            bool attackLike = ApplySpecialEffect(actor, target, true);
            yield return WaitActionSeconds(actor.view.CreateTimeline(true).Duration);
            if (attackLike)
            {
                yield return RunPostAttackSequence(actor, target, true);
            }
        }
        else
        {
            BattleTestAttackResult result = default;
            yield return ExecuteAttackSequence(actor, target, false, resolved => result = resolved);
            yield return RunPostAttackSequence(actor, target, false);
        }

        actor.SpendMainAction();
        RefreshHighlights();
        RefreshUnits();
        CheckBattleEnd();
    }

    private IEnumerator ExecuteAttackSequence(BattleTestUnit attacker, BattleTestUnit target, bool special,
                                              Action<BattleTestAttackResult> onResolved, bool forceHit = false)
    {
        ClearHudNotice();
        CombatActionTimeline timeline = attacker.view.CreateTimeline(special);
        attacker.view.FaceToward(target.view.transform.position);
        target.view.FaceToward(attacker.view.transform.position);
        EnsureBattlePresentationFx();
        battleImpactPresenter.PlayAttackStartAsync(attacker, target, special);
        if (special)
        {
            attacker.view.PlaySkill();
        }
        else
        {
            attacker.view.PlayAttack();
        }

        BattleTestAttackResult result = RollAttackResult(attacker, target, special);
        if (forceHit && !result.hit)
        {
            int debugDamage = Mathf.Max(1, attacker.definition.damageMax + (special ? attacker.definition.specialPower : 0));
            if (target.hp > 1)
            {
                debugDamage = Mathf.Min(debugDamage, target.hp - 1);
            }

            result = new BattleTestAttackResult(attacker, target, special, result.moveName, true, false, debugDamage);
        }
        yield return WaitActionSeconds(timeline.VfxTime);
        yield return WaitActionSeconds(Mathf.Max(0f, timeline.HitTime - timeline.VfxTime));
        ApplyAttackResultAtHitFrame(result);
        PlayAttackImpactPresentation(result);
        onResolved?.Invoke(result);

        if (result.hit)
        {
            // 타격 프레임 히트스톱 — 명중 순간을 잠깐 멈춰 타격감을 만든다. 치명타는 더 길게.
            yield return HitStop(result.critical ? 0.10f : 0.05f);
        }

        if (timeline.CameraShakeDuration > 0f && timeline.CameraShakeStrength > 0f)
        {
            float critBoost = result.critical ? 1.6f : 1f;
            yield return ShakeCamera(timeline.CameraShakeStrength * critBoost,
                                     timeline.CameraShakeDuration * (result.critical ? 1.3f : 1f));
        }

        yield return WaitActionSeconds(Mathf.Max(0f, timeline.Duration - timeline.HitTime) + timeline.RecoveryTime);
    }

    /// <summary>짧은 시간 정지(히트스톱). 실제 시간으로 기다린 뒤 timeScale을 복원한다.</summary>
    private IEnumerator HitStop(float duration)
    {
        float previous = Time.timeScale;
        Time.timeScale = 0.05f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        Time.timeScale = previous;
    }

    private BattleTestAttackResult RollAttackResult(BattleTestUnit attacker, BattleTestUnit target, bool special)
    {
        BattleTestTile from = TileAt(attacker.cell);
        BattleTestTile to = TileAt(target.cell);
        string moveName = special ? attacker.definition.specialName : "Attack";
        int d20 = random.Next(1, 21);
        int heightBonus = HeightAttackModifier(from, to);
        int attackBonus = attacker.definition.attackBonus + (special ? attacker.definition.specialAttackBonus : 0);
        int attackTotal = d20 + attackBonus + heightBonus;
        int defense = DefenseValue(target, to);
        bool critical = d20 == 20;
        bool hit = critical || (d20 != 1 && attackTotal >= defense);
        int damage = 0;

        if (hit)
        {
            damage = random.Next(attacker.definition.damageMin, attacker.definition.damageMax + 1);
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
        }

        return new BattleTestAttackResult(attacker, target, special, moveName, hit, critical, damage);
    }

    private void ApplyAttackResultAtHitFrame(BattleTestAttackResult result)
    {
        BattleTestUnit attacker = result.attacker;
        BattleTestUnit target = result.target;
        if (attacker == null || target == null || target.defeated)
        {
            return;
        }

        if (!result.hit)
        {
            target.view.PlayGuard();
            AddLog($"[Miss] {attacker.definition.displayName}: {result.moveName} missed at hit frame.");
            return;
        }

        target.view.PlayHit();
        target.hp = Mathf.Max(0, target.hp - result.damage);
        AddLog($"[HitFrame] {attacker.definition.displayName}: {result.moveName}, damage {result.damage}.");

        if (target.hp == 0)
        {
            target.defeated = true;
            target.view.SetDefeated(true);
            AddLog($"[Defeat] {target.definition.displayName} is down.");
        }
    }

    private void PlayAttackImpactPresentation(BattleTestAttackResult result)
    {
        if (!Application.isPlaying)
        {
            return;
        }

        EnsureBattlePresentationFx();
        if (battleImpactPresenter == null)
        {
            return;
        }

        if (result.hit)
        {
            damagePopupPresenter?.ShowDamage(result.target.view.transform.position, result.damage, result.critical);
            battleImpactPresenter.PlayHitAsync(result.target, result.damage, result.critical, false);
        }
        else
        {
            damagePopupPresenter?.ShowMiss(result.target.view.transform.position);
            battleImpactPresenter.PlayMissAsync(result.target, false);
        }
    }

    private void PrepareSpecialBeforeAttack(BattleTestUnit actor, BattleTestUnit target)
    {
        if (actor == null || target == null)
        {
            return;
        }

        if (actor.definition.specialEffect == BattleSpecialEffect.BreakGuard)
        {
            target.guarded = false;
            target.marked = true;
        }
    }

    private void ApplySpecialStatusAfterHit(BattleTestAttackResult result)
    {
        BattleTestUnit actor = result.attacker;
        BattleTestUnit target = result.target;
        if (actor == null || target == null || !result.hit || target.defeated)
        {
            return;
        }

        switch (actor.definition.specialEffect)
        {
        case BattleSpecialEffect.Poison:
            ApplyPoison(target, DefaultPoisonTurns);
            CreatePoisonSmoke(target.cell);
            AddLog($"[Status] {target.definition.displayName} poisoned.");
            break;
        case BattleSpecialEffect.Freeze:
            ApplyChill(target, DefaultChilledTurns);
            FreezeWaterAround(target.cell);
            AddLog($"[Status] {target.definition.displayName} chilled.");
            break;
        case BattleSpecialEffect.BreakGuard:
            TryPushTarget(actor, target, 1, "palm strike");
            break;
        }
    }

    private void SpendSpecialResource(BattleTestUnit actor)
    {
        if (actor == null)
        {
            return;
        }

        actor.inner = Mathf.Max(0, actor.inner - actor.definition.specialCost);
        actor.specialCooldownLeft = actor.definition.specialCooldown;
    }

    private void ApplyPoison(BattleTestUnit target, int turns)
    {
        if (target == null || target.defeated)
        {
            return;
        }

        target.poisoned = true;
        target.poisonTurnsLeft = Mathf.Max(target.poisonTurnsLeft, Mathf.Max(1, turns));
    }

    private void ApplyChill(BattleTestUnit target, int turns)
    {
        if (target == null || target.defeated)
        {
            return;
        }

        target.chilled = true;
        target.chilledTurnsLeft = Mathf.Max(target.chilledTurnsLeft, Mathf.Max(1, turns));
    }

    private void ClearNegativeStatuses(BattleTestUnit target)
    {
        if (target == null)
        {
            return;
        }

        target.poisoned = false;
        target.poisonTurnsLeft = 0;
        target.chilled = false;
        target.chilledTurnsLeft = 0;
    }

    private IEnumerator WaitActionSeconds(float seconds)
    {
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator ShakeCamera(float strength, float duration)
    {
        Camera camera = Camera.main;
        if (camera == null || duration <= 0f || strength <= 0f)
        {
            yield break;
        }

        Vector3 basePosition = camera.transform.position;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float fade = 1f - Mathf.Clamp01(elapsed / duration);
            Vector2 jitter = UnityEngine.Random.insideUnitCircle * strength * fade;
            camera.transform.position = basePosition + new Vector3(jitter.x, jitter.y, 0f);
            yield return null;
        }

        camera.transform.position = basePosition;
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

        bool hostileAttackLike = IsHostileAttackSpecial(actor.definition.specialEffect);
        BattleTargetingForecast forecast =
            BattleTargetingService.CanUseSkill(actor, target, EffectiveSpecialRange(actor), hostileAttackLike,
                                               TileAt, IsInside);
        if (!forecast.canTarget)
        {
            AddLog("[무공] " + TargetingReasonText(forecast.reason));
            return false;
        }

        CommitPendingMove(actor);

        if (Application.isPlaying && hostileAttackLike)
        {
            StartCoroutine(RunAttackCommand(actor, target, true, false));
            return true;
        }

        SpendSpecialResource(actor);
        actor.SpendMainAction();
        actor.view.FaceToward(target.view.transform.position);
        actor.view.PlaySkill();

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
            ClearNegativeStatuses(target);
            AddLog(
                $"[무공] {actor.definition.displayName}: {actor.definition.specialName}. {target.definition.displayName} 회복 {healed}.");
            return false;
        case BattleSpecialEffect.Poison:
            bool poisonHit = ResolveAttack(actor, target, true);
            if (allowStatus && poisonHit && !target.defeated)
            {
                ApplyPoison(target, DefaultPoisonTurns);
                CreatePoisonSmoke(target.cell);
                AddLog($"[상태] {target.definition.displayName} 중독.");
            }
            return true;
        case BattleSpecialEffect.Freeze:
            bool freezeHit = ResolveAttack(actor, target, true);
            if (allowStatus && freezeHit && !target.defeated)
            {
                ApplyChill(target, DefaultChilledTurns);
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

    /// <summary>반격/추격 연출판 — 즉발 대신 본 공격과 같은 선딜→타격 프레임→후딜 타임라인으로 재생한다.
    /// 플레이 중 코루틴 경로 전용. 동기 ResolvePostAttack은 에디터/스모크 체크 경로에서만 쓴다.</summary>
    private IEnumerator RunPostAttackSequence(BattleTestUnit attacker, BattleTestUnit target, bool special)
    {
        if (attacker == null || target == null || attacker.defeated || target.defeated)
        {
            yield break;
        }

        BattleTestCounterMove counter = FindCounterMove(target, attacker);
        if (counter.valid)
        {
            target.SpendReaction();
            AddLog($"[반격] {target.definition.displayName}: {counter.label}.");
            EnsureBattlePresentationFx();
            battleImpactPresenter.PlayCounterAsync(target);
            yield return WaitActionSeconds(0.22f);
            if (counter.special)
            {
                target.inner = Mathf.Max(0, target.inner - target.definition.specialCost);
                target.specialCooldownLeft = Mathf.Max(target.specialCooldownLeft, target.definition.specialCooldown);
                PrepareSpecialBeforeAttack(target, attacker);
                BattleTestAttackResult counterResult = default;
                yield return ExecuteAttackSequence(target, attacker, true, resolved => counterResult = resolved);
                ApplySpecialStatusAfterHit(counterResult);
            }
            else
            {
                yield return ExecuteAttackSequence(target, attacker, false, _ => { });
            }
        }
        else
        {
            AddLog($"[반격] {target.definition.displayName} 반격 불가.");
        }

        if (!attacker.defeated && !target.defeated && CanFollowUp(attacker, target, special))
        {
            AddLog($"[추격] {attacker.definition.displayName}가 빈틈을 찔렀다.");
            yield return WaitActionSeconds(0.18f);
            if (special)
            {
                PrepareSpecialBeforeAttack(attacker, target);
            }

            BattleTestAttackResult followUpResult = default;
            yield return ExecuteAttackSequence(attacker, target, special, resolved => followUpResult = resolved);
            if (special)
            {
                ApplySpecialStatusAfterHit(followUpResult);
            }
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
        if (BattleTargetingService.CanAttack(defender, attacker, attackRange, TileAt, IsInside).canTarget)
        {
            return new BattleTestCounterMove(false, "기본 공격");
        }

        int specialRange = EffectiveSpecialRange(defender);
        if (CanUseCounterSpecial(defender) &&
            BattleTargetingService
                .CanUseSkill(defender, attacker, specialRange,
                             IsHostileAttackSpecial(defender.definition.specialEffect), TileAt, IsInside)
                .canTarget)
        {
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

        if (actor.moved)
        {
            AddLog("[행동] 이동 후에는 공격 또는 대기만 가능합니다.");
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
            AddLog(ObjectiveInteractMessage());
            return false;
        }

        CommitPendingMove(actor);

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
        if (activeUnit == null || activeUnit.definition.faction != Faction.Ally)
        {
            return;
        }

        if (!CanGuard(activeUnit))
        {
            AddLog("[행동] 이동 후에는 공격 또는 대기만 가능합니다.");
            return;
        }

        CommitPendingMove(activeUnit);
        activeUnit.guarded = true;
        activeUnit.view.PlayGuard();
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

        CommitPendingMove(actor);

        if (phaseTurn.AllFactionUnitsActed(units, Faction.Ally))
        {
            AddLog("[페이즈] 모든 아군 행동 완료");
            EndPlayerPhase();
            return true;
        }

        activeUnit = FindNextReadyUnit(Faction.Ally);
        commandMode = DefaultCommandForUnit(activeUnit);
        if (activeUnit != null)
        {
            AddLog($"[선택] 다음 행동: {activeUnit.definition.displayName}");
        }
        RefreshHighlights();
        RefreshUnits();
        if (activeUnit != null)
        {
            activeUnit.view.PlayTurnStart();
        }
        FocusCameraOnUnit(activeUnit, 0.28f);
        return false;
    }

    private string ObjectiveInteractMessage()
    {
        switch (mapVariant)
        {
        case BattleTestMapVariant.BanditLair:
            return "[목표] 빼앗긴 보급입니다. 도적 두목을 제압한 뒤 회수하세요.";
        case BattleTestMapVariant.WolfPass:
            return "[목표] 늑대 굴입니다. 우두머리를 제압하고 피난로를 확보한 뒤 봉쇄하세요.";
        case BattleTestMapVariant.TigerRavine:
            return "[목표] 갇힌 주민입니다. 산군을 떼어내고 바위 선반 길을 열어 구조하세요.";
        case BattleTestMapVariant.LeopardCliff:
            return "[목표] 약초꾼 호송 지점입니다. 표범 매복을 정리한 뒤 지나갈 수 있습니다.";
        case BattleTestMapVariant.SeorakPassRescue:
            return "[목표] 약초 수레와 피난민입니다. 유달근을 묶어두고 수레 주변 전열을 유지하세요.";
        default:
            return "[목표] 현판은 지켜야 합니다. 적이 닿기 전에 병목을 막으세요.";
        }
    }

    private IEnumerator AnimateMove(BattleTestUnit unit, List<Vector2Int> path)
    {
        StopCameraPanRoutine();
        busy = true;
        Camera camera = Camera.main;
        float cameraTargetSize = camera == null ? 0f : CalculateTacticalCameraSize(camera);

        if (path == null || path.Count < 2)
        {
            busy = false;
            yield break;
        }

        unit.view.PlayMove();
        float duration = unit.view.WalkSecondsPerTile();
        int stepIndex = 0;
        for (int i = 1; i < path.Count; i++)
        {
            Vector3 start = unit.view.transform.position;
            Vector3 target = UnitWorldPosition(path[i]);
            bool firstStep = i == 1;
            bool lastStep = i == path.Count - 1;
            unit.view.FaceDirection(new Vector2(target.x - start.x, target.y - start.y));
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // 출발 칸만 가속, 도착 칸만 감속, 중간 칸은 등속(경계 속도 일치) — 칸마다 멈칫거리지 않는다.
                // 보폭 바운스/스쿼시는 CharacterVisualController가 몸 스프라이트에만 적용한다(그림자·발자국은 지면 고정).
                float eased = firstStep && lastStep ? Mathf.SmoothStep(0f, 1f, t)
                              : firstStep          ? t * t * (2f - t)
                              : lastStep           ? 1f - ((1f - t) * (1f - t) * (1f + t))
                                                   : t;
                unit.view.SetMoveStridePhase((stepIndex * 0.5f) + t);
                unit.view.transform.position = Vector3.Lerp(start, target, eased);
                if (camera != null)
                {
                    Vector3 cameraTarget = CameraPositionForFocus(camera, unit.view.transform.position, cameraTargetSize);
                    float follow = 1f - Mathf.Exp(-7f * Time.deltaTime);
                    camera.transform.position = Vector3.Lerp(camera.transform.position, cameraTarget, follow);
                    camera.orthographicSize = Mathf.Lerp(camera.orthographicSize, cameraTargetSize, follow);
                }

                yield return null;
            }

            unit.view.transform.position = target;
            stepIndex++;
        }

        unit.view.PlayIdle();
        FaceUnitAlongPath(unit, path);
        if (unit.definition.faction == Faction.Ally && phaseTurn.IsPlayerPhase)
        {
            ShowHudNotice("이동 완료: 공격 또는 대기");
        }

        float settleTime = unit.view.MoveSettleTime();
        if (settleTime > 0f)
        {
            yield return new WaitForSeconds(settleTime);
        }

        if (camera != null)
        {
            SetCameraFocusImmediate(camera, unit.view.transform.position, cameraTargetSize);
        }

        busy = false;
        RefreshHighlights();
        RefreshUnits();
    }

    private void FaceUnitAlongPath(BattleTestUnit unit, IList<Vector2Int> path)
    {
        if (unit == null || unit.view == null || path == null || path.Count < 2)
        {
            return;
        }

        Vector3 from = UnitWorldPosition(path[path.Count - 2]);
        Vector3 to = UnitWorldPosition(path[path.Count - 1]);
        unit.view.FaceDirection(new Vector2(to.x - from.x, to.y - from.y));
    }

    private IEnumerator RunEnemyPhase()
    {
        busy = true;
        yield return new WaitForSeconds(EnemyPhaseStartDelay);

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
            yield return PanCameraToUnit(activeUnit, EnemyFocusSeconds);
            yield return new WaitForSeconds(EnemyFocusSettleDelay);

            // 힐러 적: 사거리 안에 부상한 아군이 있으면 공격 대신 치유(무공은 제자리 시전이라 이동 전에만).
            BattleTestUnit healTarget = FindEnemyHealTarget(activeUnit);
            if (healTarget != null)
            {
                yield return RunEnemyActionCommand(activeUnit, healTarget, true);
                if (battleOver)
                {
                    busy = false;
                    yield break;
                }

                yield return new WaitForSeconds(EnemyBetweenUnitDelay);
                continue;
            }

            BattleTestUnit target = ChooseEnemyTarget(activeUnit);
            if (target == null)
            {
                activeUnit.view.PlayWait();
                activeUnit.SpendMainAction();
                continue;
            }

            bool specialReadyWithoutMoving = CanUseSpecial(activeUnit) && IsValidSpecialTarget(activeUnit, target) &&
                                             BattleTargetingService
                                                 .CanUseSkill(activeUnit, target, EffectiveSpecialRange(activeUnit),
                                                              IsHostileAttackSpecial(activeUnit.definition.specialEffect),
                                                              TileAt, IsInside)
                                                 .canTarget;
            int desiredRange = specialReadyWithoutMoving ? EffectiveSpecialRange(activeUnit) : EffectiveAttackRange(activeUnit);
            bool canActFromCurrentCell = specialReadyWithoutMoving ||
                                         BattleTargetingService
                                             .CanAttack(activeUnit, target, desiredRange, TileAt, IsInside)
                                             .canTarget;
            if (!canActFromCurrentCell && !activeUnit.moved)
            {
                BattleTestTile best = FindBestMoveToward(activeUnit, target.cell);
                if (best != null)
                {
                    int moveCost = 0;
                    Dictionary<Vector2Int, int> reachable = GetReachableCells(activeUnit);
                    reachable.TryGetValue(best.cell, out moveCost);
                    List<Vector2Int> path = FindMovePath(activeUnit, best.cell);
                    activeUnit.cell = best.cell;
                    activeUnit.SpendMovement(moveCost);
                    ApplyTileEntry(activeUnit, best);
                    if (activeUnit.defeated)
                    {
                        // 이동 중 지형 피해(화염 등)로 전투불능 — 시체가 걷거나 공격하지 않도록 위치만 정리하고 다음 적으로.
                        if (activeUnit.view != null)
                        {
                            activeUnit.view.transform.position = UnitWorldPosition(best.cell);
                        }

                        RefreshUnits();
                        if (CheckBattleEnd())
                        {
                            busy = false;
                            yield break;
                        }

                        yield return new WaitForSeconds(EnemyBetweenUnitDelay);
                        continue;
                    }

                    yield return AnimateMove(activeUnit, path);
                    yield return new WaitForSeconds(EnemyPostMoveDelay);
                }
            }

            bool useSpecial = CanUseSpecial(activeUnit) && IsValidSpecialTarget(activeUnit, target) &&
                              BattleTargetingService
                                  .CanUseSkill(activeUnit, target, EffectiveSpecialRange(activeUnit),
                                               IsHostileAttackSpecial(activeUnit.definition.specialEffect),
                                               TileAt, IsInside)
                                  .canTarget;

            // 사거리/시야 밖이면 공격하지 않는다 — 플레이어 TryAttack과 동일한 규칙. 접근만 한 뒤 대기.
            int actionRange = useSpecial ? EffectiveSpecialRange(activeUnit) : EffectiveAttackRange(activeUnit);
            bool canReachTarget = useSpecial
                                      ? BattleTargetingService
                                          .CanUseSkill(activeUnit, target, actionRange,
                                                       IsHostileAttackSpecial(activeUnit.definition.specialEffect),
                                                       TileAt, IsInside)
                                          .canTarget
                                      : BattleTargetingService.CanAttack(activeUnit, target, actionRange, TileAt, IsInside)
                                                              .canTarget;
            if (canReachTarget)
            {
                yield return RunEnemyActionCommand(activeUnit, target, useSpecial);
            }
            else
            {
                activeUnit.view.PlayWait();
                activeUnit.SpendMainAction();
                AddLog($"[적] {activeUnit.definition.displayName} 사거리 밖 — 대기.");
            }

            if (battleOver)
            {
                busy = false;
                yield break;
            }

            yield return new WaitForSeconds(EnemyBetweenUnitDelay);
        }

        busy = false;
        activeUnit = null;
        aiQueued = false;
        EndEnemyPhase();
    }

    private BattleTestTile FindDebugMoveTile(BattleTestUnit unit)
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
            if (!CanStandOnTile(tile))
            {
                continue;
            }

            int crowdPenalty = 0;
            foreach (BattleTestUnit other in units)
            {
                if (other == null || other == unit || other.defeated)
                {
                    continue;
                }

                int distanceToOther = GridDistance(pair.Key, other.cell);
                if (distanceToOther <= 1)
                {
                    crowdPenalty += 100;
                }
                else if (distanceToOther == 2)
                {
                    crowdPenalty += 24;
                }
            }

            int score = GridDistance(unit.cell, pair.Key) * 10 - pair.Value + tile.elevation - crowdPenalty;
            if (score > bestScore)
            {
                bestScore = score;
                best = tile;
            }
        }

        return best;
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

            int range = EffectiveAttackRange(unit, tile);
            if (BattleTargetingService.CanAttackFrom(pair.Key, targetCell, range, TileAt, IsInside).canTarget)
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

    // 적 AI 타겟 선택/치유 헬퍼(ChooseEnemyTarget, FindEnemyHealTarget, CanReachToAttackThisTurn,
    // CanHitFrom, EstimateAttackDamage)는 BattleTestController.EnemyAi.cs(partial)로 분리됨.

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
            if (unit.poisonTurnsLeft <= 0)
            {
                unit.poisonTurnsLeft = 1;
            }

            unit.hp = Mathf.Max(0, unit.hp - 3);
            AddLog($"[상태] {unit.definition.displayName} 중독 피해 3.");
            if (unit.hp == 0)
            {
                unit.defeated = true;
                unit.view.SetDefeated(true);
                ClearNegativeStatuses(unit);
                return;
            }

            unit.poisonTurnsLeft--;
            if (unit.poisonTurnsLeft <= 0)
            {
                unit.poisoned = false;
                unit.poisonTurnsLeft = 0;
                AddLog($"[Status] {unit.definition.displayName} poison faded.");
            }
        }

        if (unit.chilled)
        {
            if (unit.chilledTurnsLeft <= 0)
            {
                unit.chilledTurnsLeft = 1;
            }

            unit.chilledTurnsLeft--;
            if (unit.chilledTurnsLeft <= 0)
            {
                unit.chilled = false;
                unit.chilledTurnsLeft = 0;
                AddLog($"[Status] {unit.definition.displayName} chill faded.");
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
        return unit != null && !unit.moved && unit.CanUseMainAction && unit.inner >= unit.definition.specialCost &&
               unit.specialCooldownLeft <= 0 && unit.definition.specialEffect != BattleSpecialEffect.None;
    }

    private bool CanGuard(BattleTestUnit unit)
    {
        return unit != null && !unit.moved && unit.CanUseMainAction;
    }

    private bool CanUseTerrainCommand(BattleTestUnit unit)
    {
        return unit != null && !unit.moved && unit.CanUseMainAction && HasUsableInteractable(unit);
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

        BattleTargetingForecast targeting = special
                                                ? BattleTargetingService
                                                    .CanUseSkill(actor, target, range,
                                                                 IsHostileAttackSpecial(actor.definition.specialEffect),
                                                                 TileAt, IsInside)
                                                : BattleTargetingService.CanAttack(actor, target, range, TileAt, IsInside);
        if (!targeting.canTarget)
        {
            return BattleForecast.Invalid(actor.definition.displayName, target.definition.displayName, commandName,
                                          TargetingReasonText(targeting.reason), distance, range, costText);
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

    private static string TargetingReasonText(string reason)
    {
        switch (reason)
        {
        case "out of range":
            return "사거리 밖입니다.";
        case "line of sight blocked":
            return "시야가 막혔습니다.";
        case "height or edge blocked":
            return "고저차나 장애물 때문에 닿지 않습니다.";
        case "same faction":
            return "대상 진영이 맞지 않습니다.";
        case "invalid":
            return "대상을 지정할 수 없습니다.";
        default:
            return string.IsNullOrEmpty(reason) ? "대상을 지정할 수 없습니다." : reason;
        }
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

        if (unit.moved)
        {
            return "이동 후에는 공격 또는 대기만 가능합니다.";
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
            ApplyChill(unit, 1);
            DealTerrainDamage(unit, 6, reason + " into deep water");
        }
        else if (tile.hazardType == HazardType.Ice || tile.hazardType == HazardType.Slippery)
        {
            ApplyChill(unit, 1);
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
        bool movedAny = false;

        // 밀려난 거리만큼 순간이동하지 않고 짧게 미끄러지는 연출로 따라간다(로직 좌표는 즉시 갱신).
        void SyncPushedView()
        {
            if (!movedAny || target.view == null)
            {
                return;
            }

            Vector3 destination = UnitWorldPosition(target.cell);
            if (Application.isPlaying)
            {
                StartCoroutine(AnimatePushSlide(target.view.transform, destination));
            }
            else
            {
                target.view.transform.position = destination;
            }
        }

        for (int i = 0; i < Mathf.Max(1, distance); i++)
        {
            Vector2Int nextCell = target.cell + direction;
            BattleTestTile nextTile = TileAt(nextCell);
            if (nextTile == null)
            {
                SyncPushedView();
                DealTerrainDamage(target, FallDamage, reason + " over the edge");
                return true;
            }

            if (UnitAt(nextCell) != null || IsCellBlockedByInteractable(nextCell))
            {
                SyncPushedView();
                DealTerrainDamage(target, 3, reason + " collision");
                return false;
            }

            if (!CanStandOnTile(nextTile))
            {
                if (nextTile.hazardType == HazardType.DeepWater || nextTile.terrain == TerrainType.DeepWater)
                {
                    ApplyChill(target, 1);
                    SyncPushedView();
                    DealTerrainDamage(target, 6, reason + " into deep water");
                    return true;
                }

                if (IsCliffDropToward(currentTile, direction) || nextTile.hazardType == HazardType.Fall)
                {
                    SyncPushedView();
                    DealTerrainDamage(target, FallDamage, reason + " off a cliff");
                    return true;
                }

                SyncPushedView();
                DealTerrainDamage(target, 3, reason + " into terrain");
                return false;
            }

            target.cell = nextCell;
            movedAny = true;
            ApplyTileEntry(target, nextTile);
            ApplyPushLandingHazard(target, nextTile, reason);
            currentTile = nextTile;
            if (target.defeated)
            {
                SyncPushedView();
                return true;
            }
        }

        SyncPushedView();
        AddLog($"[Push] {target.definition.displayName} pushed by {reason}.");
        RefreshHighlights();
        RefreshUnits();
        return true;
    }

    private IEnumerator AnimatePushSlide(Transform view, Vector3 destination)
    {
        Vector3 start = view.position;
        const float duration = 0.16f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - ((1f - t) * (1f - t));
            Vector3 position = Vector3.Lerp(start, destination, eased);
            position.y += Mathf.Sin(t * Mathf.PI) * 0.06f; // 짧은 포물선 — 튕겨난 느낌
            view.position = position;
            yield return null;
        }

        view.position = destination;
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
            if (!CanStandOnTile(tile))
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
            if (!CanStandOnTile(tile))
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
            if (!CanStandOnTile(tile))
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

    private bool IsCellBlockedByInteractable(Vector2Int cell)
    {
        foreach (BattleTestInteractable interactable in interactables)
        {
            if (interactable == null || interactable.used || interactable.cell != cell)
            {
                continue;
            }

            if (IsBlockingInteractable(interactable))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsBlockingInteractable(BattleTestInteractable interactable)
    {
        if (interactable == null)
        {
            return false;
        }

        switch (interactable.kind)
        {
        case BattleTestInteractableKind.Objective:
        case BattleTestInteractableKind.Smoke:
        case BattleTestInteractableKind.CollapseBridge:
            return false;
        default:
            return true;
        }
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
            return new Color(1f, 0.30f, 0.16f, 0.18f);
        }

        if (tile.smokeTurns > 0)
        {
            return new Color(0.64f, 0.70f, 0.74f, 0.15f);
        }

        if (!tile.walkable && tile.danger)
        {
            return new Color(0.92f, 0.18f, 0.12f, 0.12f);
        }

        if (tile.extraCover)
        {
            return new Color(0.30f, 0.78f, 0.45f, 0.12f);
        }

        return Color.clear;
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
            AddLog("[\uBC30\uCE58] \uBC30\uCE58 \uC911\uC5D0\uB294 \uACF5\uACA9/\uBB34\uACF5 \uB300\uC2E0 \uC544\uAD70 \uBC30\uCE58\uB9CC \uBC14\uAFC0 \uC218 \uC788\uC2B5\uB2C8\uB2E4.");
            return;
        }

        if (mode == BattleCommandMode.Move && !activeUnit.CanMove)
        {
            commandMode = DefaultCommandForUnit(activeUnit);
            AddLog("[UI] 이 캐릭터는 이미 이동했습니다.");
            RefreshHighlights();
            return;
        }

        if (mode == BattleCommandMode.Attack && !activeUnit.CanUseMainAction)
        {
            AddLog("[UI] 이미 행동한 캐릭터입니다.");
            return;
        }

        if (activeUnit.moved && (mode == BattleCommandMode.Skill || mode == BattleCommandMode.Interact))
        {
            AddLog("[UI] 이동 후에는 공격 또는 대기만 가능합니다.");
            return;
        }

        if (mode == BattleCommandMode.Skill && !CanUseSpecial(activeUnit))
        {
            AddLog("[UI] 지금 사용할 수 있는 무공이 없습니다.");
            return;
        }

        if (mode == BattleCommandMode.Interact && !CanUseTerrainCommand(activeUnit))
        {
            AddLog("[UI] 사용할 수 있는 지형지물이 없습니다.");
            return;
        }

        commandMode = mode;
        RefreshHighlights();
    }

    private BattleCommandMode DefaultCommandForUnit(BattleTestUnit unit)
    {
        if (unit == null || unit.definition.faction != Faction.Ally)
        {
            return BattleCommandMode.Move;
        }

        if (unit.CanMove)
        {
            return BattleCommandMode.Move;
        }

        if (unit.CanUseMainAction)
        {
            return BattleCommandMode.Attack;
        }

        return BattleCommandMode.Attack;
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

        if (Camera.main == null || PointerOverHud(Input.mousePosition))
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

    private Dictionary<Vector2Int, int> GetLegalMovePreviewCells(BattleTestUnit unit)
    {
        Dictionary<Vector2Int, int> reachable = GetReachableCells(unit);
        Dictionary<Vector2Int, int> legal = new Dictionary<Vector2Int, int>(reachable.Count);
        foreach (KeyValuePair<Vector2Int, int> pair in reachable)
        {
            BattleTestTile tile = TileAt(pair.Key);
            if (pair.Key == unit.cell || CanUseMoveDestination(unit, tile, reachable, false))
            {
                legal[pair.Key] = pair.Value;
            }
        }

        return legal;
    }

    private bool CanUseMoveDestination(BattleTestUnit unit, BattleTestTile destination,
                                       Dictionary<Vector2Int, int> reachable, bool requirePath)
    {
        if (unit == null || destination == null || destination.cell == unit.cell)
        {
            return false;
        }

        if (!CanEnterCellForMovement(unit, destination.cell))
        {
            return false;
        }

        if (reachable != null && !reachable.ContainsKey(destination.cell))
        {
            return false;
        }

        return !requirePath || FindMovePath(unit, destination.cell).Count >= 2;
    }

    private bool CanEnterCellForMovement(BattleTestUnit unit, Vector2Int cell)
    {
        BattleTestTile tile = TileAt(cell);
        BattleTestUnit occupant = UnitAt(cell);
        return BattlePathService.CanEnterCell(unit, tile, occupant, IsCellBlockedByInteractable(cell));
    }

    private Dictionary<Vector2Int, int> GetReachableCells(BattleTestUnit unit)
    {
        return BattlePathService.GetReachableCells(unit, EffectiveMoveRange(unit), TileAt, Neighbors,
                                                   CanEnterCellForMovement);
    }

    private List<Vector2Int> FindMovePath(BattleTestUnit unit, Vector2Int destination)
    {
        return BattlePathService.FindMovePath(unit, destination, EffectiveMoveRange(unit), TileAt, Neighbors,
                                              CanEnterCellForMovement);
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
        return BattlePathService.StepMoveCost(from, to);
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

        if (scoutMode)
        {
            HighlightDeploymentCells();
            if (activeUnit != null && !activeUnit.defeated)
            {
                BattleTestTile selectedTile = TileAt(activeUnit.cell);
                if (selectedTile != null)
                {
                    selectedTile.SetHighlight(new Color(0.12f, 0.86f, 1f, 0.78f));
                }
            }

            return;
        }

        if (activeUnit == null || activeUnit.defeated)
        {
            return;
        }

        BattleTestTile activeTile = TileAt(activeUnit.cell);
        bool movementPreview = IsMovementPreviewActive();

        DrawMapOverlays(movementPreview);

        if (movementPreview)
        {
            HighlightMoveReachability(activeUnit);
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

                if (BattleTargetingService.CanAttack(activeUnit, target, range, TileAt, IsInside).canTarget)
                {
                    BattleTestTile tile = TileAt(target.cell);
                    if (tile != null)
                    {
                        tile.SetHighlight(new Color(1f, 0.27f, 0.20f, 0.52f));
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

                if (BattleTargetingService
                    .CanUseSkill(activeUnit, target, range,
                                 IsHostileAttackSpecial(activeUnit.definition.specialEffect), TileAt, IsInside)
                    .canTarget)
                {
                    BattleTestTile tile = TileAt(target.cell);
                    if (tile != null)
                    {
                        Color color = activeUnit.definition.specialEffect == BattleSpecialEffect.Heal
                                          ? new Color(0.28f, 0.92f, 0.55f, 0.36f)
                                          : new Color(0.74f, 0.40f, 1f, 0.38f);
                        tile.SetHighlight(color);
                    }
                }
            }
        }

        if (commandMode == BattleCommandMode.Interact && CanUseTerrainCommand(activeUnit))
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
                    tile.SetHighlight(new Color(1f, 0.70f, 0.18f, 0.38f));
                }
            }
        }

        if (activeTile != null && movementPreview)
        {
            activeTile.SetHighlight(new Color(0.26f, 0.78f, 1f, 0.30f));
        }
    }

    private bool IsMovementPreviewActive()
    {
        return phaseTurn.IsPlayerPhase && !scoutMode && activeUnit != null && !activeUnit.defeated &&
               activeUnit.definition.faction == Faction.Ally && commandMode == BattleCommandMode.Move &&
               activeUnit.CanMove && !battleOver;
    }

    private void HighlightMoveReachability(BattleTestUnit unit)
    {
        Dictionary<Vector2Int, int> reachable = GetLegalMovePreviewCells(unit);
        HashSet<Vector2Int> boundary = new HashSet<Vector2Int>();
        int moveRange = Mathf.Max(1, EffectiveMoveRange(unit));

        foreach (KeyValuePair<Vector2Int, int> pair in reachable)
        {
            Vector2Int cell = pair.Key;
            if (cell != unit.cell)
            {
                BattleTestTile tile = TileAt(cell);
                if (CanUseMoveDestination(unit, tile, reachable, false))
                {
                    float costRatio = Mathf.Clamp01(pair.Value / (float)moveRange);
                    float alpha = Mathf.Lerp(0.62f, 0.48f, costRatio);
                    tile.SetHighlight(new Color(0.06f, 0.48f, 1f, alpha));
                }
            }

            foreach (Vector2Int neighbor in Neighbors(cell))
            {
                if (!IsInside(neighbor) || reachable.ContainsKey(neighbor))
                {
                    continue;
                }

                BattleTestTile edgeTile = TileAt(neighbor);
                if (edgeTile == null)
                {
                    continue;
                }

                if (ShouldShowMoveBoundary(unit, neighbor))
                {
                    boundary.Add(neighbor);
                }
            }
        }

        foreach (Vector2Int cell in boundary)
        {
            BattleTestTile tile = TileAt(cell);
            if (tile != null)
            {
                tile.SetHighlight(new Color(1f, 0.16f, 0.13f, 0.74f));
            }
        }
    }

    private bool ShouldShowMoveBoundary(BattleTestUnit unit, Vector2Int boundaryCell)
    {
        if (unit == null || boundaryCell == unit.cell)
        {
            return false;
        }

        Vector3 fromUnit = GridToWorld(boundaryCell) - GridToWorld(unit.cell);
        if (fromUnit.sqrMagnitude <= 0.0001f)
        {
            return false;
        }

        // Screen-up and side edges only. Bottom/back edges stay unmarked so the
        // preview reads as blue movement area with a red forward boundary.
        return fromUnit.y >= -(tileHeight * 0.35f);
    }

    private void HighlightDeploymentCells()
    {
        if (tiles == null)
        {
            return;
        }

        HashSet<Vector2Int> deploymentCells = new HashSet<Vector2Int>();
        HashSet<Vector2Int> boundaryCells = new HashSet<Vector2Int>();

        foreach (BattleTestTile tile in tiles)
        {
            if (tile == null || !IsDeploymentCell(tile.cell) || !CanStandOnTile(tile) ||
                IsCellBlockedByInteractable(tile.cell))
            {
                continue;
            }

            deploymentCells.Add(tile.cell);
            foreach (Vector2Int neighbor in Neighbors(tile.cell))
            {
                if (!IsInside(neighbor) || deploymentCells.Contains(neighbor))
                {
                    continue;
                }

                BattleTestTile neighborTile = TileAt(neighbor);
                if (neighborTile != null && !IsDeploymentCell(neighbor))
                {
                    boundaryCells.Add(neighbor);
                }
            }
        }

        foreach (Vector2Int cell in boundaryCells)
        {
            BattleTestTile tile = TileAt(cell);
            if (tile != null)
            {
                tile.SetHighlight(new Color(1f, 0.12f, 0.10f, 0.54f));
            }
        }

        foreach (Vector2Int cell in deploymentCells)
        {
            BattleTestTile tile = TileAt(cell);
            if (tile == null)
            {
                continue;
            }

            BattleTestUnit occupant = UnitAt(cell);
            Color highlight = new Color(0.06f, 0.48f, 1f, 0.58f);
            if (occupant == activeUnit)
            {
                highlight = new Color(0.12f, 0.86f, 1f, 0.78f);
            }
            else if (occupant != null)
            {
                highlight = new Color(0.08f, 0.54f, 1f, 0.42f);
            }

            tile.SetHighlight(highlight);
        }
    }

    private void DrawMapOverlays(bool movementPreview)
    {
        if (tiles == null)
        {
            return;
        }

        if (!movementPreview && showThreatOverlay)
        {
            RebuildEnemyThreatCells();
        }

        foreach (BattleTestTile tile in tiles)
        {
            if (tile == null)
            {
                continue;
            }

            if (!movementPreview && showThreatOverlay && IsInEnemyThreat(tile.cell))
            {
                // 위험 구역: 내부는 반투명, 가장자리는 진한 림으로 칠해 설원 위에서도 경계가 보이게 한다(테두리 효과).
                tile.SetHighlight(IsThreatEdge(tile.cell)
                                      ? new Color(1f, 0.30f, 0.22f, 0.62f)
                                      : new Color(0.95f, 0.20f, 0.16f, 0.34f));
            }

            if (!movementPreview && showElevationOverlay && tile.elevation > 0)
            {
                float alpha = Mathf.Clamp01(0.16f + tile.elevation * 0.07f);
                tile.SetHighlight(new Color(1f, 0.80f, 0.20f, alpha));
            }

            if (!movementPreview && showCoverOverlay && tile.coverBonus > 0)
            {
                tile.SetHighlight(new Color(0.28f, 0.78f, 0.45f, 0.34f));
            }

            if (!movementPreview && showSightOverlay && tile.blocksLineOfSight)
            {
                tile.SetHighlight(new Color(0.55f, 0.45f, 0.32f, 0.36f));
            }

            if (!movementPreview && showObjectiveOverlay && tile.objective)
            {
                tile.SetHighlight(new Color(1f, 0.80f, 0.16f, 0.28f));
            }

            if (!movementPreview && showThreatOverlay && tile.danger)
            {
                tile.SetHighlight(new Color(0.96f, 0.30f, 0.12f, 0.42f));
            }
        }
    }

    private bool IsInEnemyThreat(Vector2Int cell)
    {
        return enemyThreatCells.Contains(cell);
    }

    // 위협 구역의 가장자리 칸(이웃 중 위협이 아닌 칸 또는 맵 밖이 있는 칸).
    private bool IsThreatEdge(Vector2Int cell)
    {
        foreach (Vector2Int neighbor in Neighbors(cell))
        {
            if (!IsInside(neighbor) || !enemyThreatCells.Contains(neighbor))
            {
                return true;
            }
        }

        return false;
    }

    // 적이 "한 번 이동 후 기본 공격"으로 닿는 칸까지 모두 위협으로 계산한다.
    // 현재 위치에서는 무공/반격기 사거리도 포함(이동하면 무공 불가이므로 이동칸은 공격 사거리만).
    private void RebuildEnemyThreatCells()
    {
        enemyThreatCells.Clear();
        foreach (BattleTestUnit unit in units)
        {
            if (unit == null || unit.defeated || unit.definition.faction != Faction.Enemy)
            {
                continue;
            }

            AddThreatFromStandCell(unit, unit.cell, true);
            foreach (Vector2Int standCell in GetReachableCells(unit).Keys)
            {
                if (standCell == unit.cell || UnitAt(standCell) != null)
                {
                    continue;
                }

                AddThreatFromStandCell(unit, standCell, false);
            }
        }
    }

    private void AddThreatFromStandCell(BattleTestUnit unit, Vector2Int standCell, bool fromCurrentCell)
    {
        BattleTestTile standTile = TileAt(standCell);
        if (standTile == null)
        {
            return;
        }

        int range = EffectiveAttackRange(unit, standTile);
        if (fromCurrentCell && CanUseCounterSpecial(unit))
        {
            range = Mathf.Max(range, EffectiveSpecialRange(unit, standTile));
        }

        foreach (Vector2Int targetCell in RadiusCells(standCell, range))
        {
            if (!IsInside(targetCell) || enemyThreatCells.Contains(targetCell))
            {
                continue;
            }

            if (!BattleTargetingService.CanAttackFrom(standCell, targetCell, range, TileAt, IsInside).canTarget)
            {
                continue;
            }

            enemyThreatCells.Add(targetCell);
        }
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
        bool movementPreview = IsMovementPreviewActive();
        if (tiles != null)
        {
            foreach (BattleTestTile tile in tiles)
            {
                if (tile == null || tile.nameLabel == null)
                {
                    continue;
                }

                bool visible = showTerrainNames ||
                               (!movementPreview &&
                                ((showObjectiveOverlay && tile.objective) ||
                                 (showElevationOverlay && tile.elevation >= 2) ||
                                 (showSightOverlay && tile.blocksLineOfSight) ||
                                 (showCoverOverlay && tile.coverBonus > 0)));
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
        if (battleOver)
        {
            return true;
        }

        if (round > DefaultTurnLimit)
        {
            battleOver = true;
            ClearHighlights();
            PlayBattleOutcomeVisuals(false);
            AddLog("[Battle End] Defeat. Turn limit exceeded.");
            return true;
        }

        bool alliesAlive = false;
        bool enemiesAlive = false;
        bool objectiveBreached = false;
        bool requiredHeroDefeated = false;
        string requiredAllyId = RequiredAllyUnitId(mapVariant);
        string victoryBossId = VictoryBossUnitId(mapVariant);
        bool requiredAllyDefeated = false;
        bool bossPresent = false;
        bool bossAlive = false;

        foreach (BattleTestUnit unit in units)
        {
            if (unit.definition == null)
            {
                continue;
            }

            if (unit.definition.id == RequiredHeroUnitId && unit.defeated)
            {
                requiredHeroDefeated = true;
            }

            if (requiredAllyId != null && unit.definition.id == requiredAllyId && unit.defeated)
            {
                requiredAllyDefeated = true;
            }

            if (victoryBossId != null && unit.definition.id == victoryBossId)
            {
                bossPresent = true;
                if (!unit.defeated)
                {
                    bossAlive = true;
                }
            }

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
                if (HasEnemyBreachObjective(mapVariant) && tile != null && tile.objective)
                {
                    objectiveBreached = true;
                }
            }
        }

        if (requiredHeroDefeated)
        {
            battleOver = true;
            ClearHighlights();
            PlayBattleOutcomeVisuals(false);
            AddLog("[Battle End] Defeat. Park Sungjun has fallen.");
            return true;
        }

        if (requiredAllyDefeated)
        {
            battleOver = true;
            ClearHighlights();
            PlayBattleOutcomeVisuals(false);
            AddLog("[전투 종료] 패배. 백련을 지키지 못했다.");
            return true;
        }

        if (objectiveBreached)
        {
            battleOver = true;
            ClearHighlights();
            PlayBattleOutcomeVisuals(false);
            AddLog("[전투 종료] 패배. 철랑문이 백두천광 현판까지 돌파했다.");
            return true;
        }

        // 보스 처치형 전투(설악 구조전): 두목만 제압하면 잔당이 남아도 승리한다.
        if (victoryBossId != null && bossPresent && !bossAlive && alliesAlive)
        {
            battleOver = true;
            ClearHighlights();
            PlayBattleOutcomeVisuals(true);
            AddLog("[전투 종료] 승리. 철비채 두목 유달근을 제압하고 약초 수레를 지켜냈다.");
            return true;
        }

        if (alliesAlive && enemiesAlive)
        {
            return false;
        }

        battleOver = true;
        ClearHighlights();
        PlayBattleOutcomeVisuals(alliesAlive);
        AddLog(alliesAlive ? "[전투 종료] 승리." : "[전투 종료] 패배.");
        return true;
    }

    private static bool HasEnemyBreachObjective(BattleTestMapVariant variant)
    {
        return variant == BattleTestMapVariant.BaekduSnowGate ||
               variant == BattleTestMapVariant.BaekduMountainSnowfield;
    }

    // 해당 전투에서 반드시 생존해야 하는 아군(쓰러지면 패배). 없으면 null.
    private static string RequiredAllyUnitId(BattleTestMapVariant variant)
    {
        // 설악 구조전: 백련을 지켜야 한다(보조 목표).
        return variant == BattleTestMapVariant.SeorakPassRescue ? "baek_ryeon" : null;
    }

    // 처치하면 즉시 승리하는 보스 유닛 id(잔당 무관). 없으면 null.
    private static string VictoryBossUnitId(BattleTestMapVariant variant)
    {
        // 설악 구조전: 철비채 두목 유달근 격파가 승리 조건.
        return variant == BattleTestMapVariant.SeorakPassRescue ? "seorak_bandit_boss_yudalgeun" : null;
    }

    private void PlayBattleOutcomeVisuals(bool alliesWon)
    {
        foreach (BattleTestUnit unit in units)
        {
            if (unit.defeated)
            {
                continue;
            }

            bool winningSide = (unit.definition.faction == Faction.Ally) == alliesWon;
            if (winningSide)
            {
                unit.view.PlayVictory();
            }
            else
            {
                unit.view.PlayWait();
            }
        }
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

    private BattleTestUnit ResolveClickedUnit(BattleTestUnit clickedUnit, BattleTestTile clickedTile)
    {
        if (clickedUnit != null || clickedTile == null)
        {
            return clickedUnit;
        }

        return UnitAt(clickedTile.cell);
    }

    private BattleTestUnit ResolveAttackClickTarget(BattleTestUnit attacker, BattleTestUnit clickedUnit,
                                                    BattleTestTile clickedTile)
    {
        if (attacker == null)
        {
            return null;
        }

        if (clickedUnit != null)
        {
            return clickedUnit.definition.faction == attacker.definition.faction ? null : clickedUnit;
        }

        if (clickedTile == null)
        {
            return null;
        }

        BattleTestUnit uniqueTarget = null;
        foreach (BattleTestUnit unit in units)
        {
            if (!CanBasicAttackTarget(attacker, unit) || GridDistance(clickedTile.cell, unit.cell) > 1)
            {
                continue;
            }

            if (uniqueTarget != null)
            {
                return null;
            }

            uniqueTarget = unit;
        }

        return uniqueTarget;
    }

    private BattleTestTile TileAt(Vector2Int cell)
    {
        if (tiles == null || cell.x < 0 || cell.y < 0 ||
            cell.x >= tiles.GetLength(0) || cell.y >= tiles.GetLength(1))
        {
            return null;
        }

        return tiles[cell.x, cell.y];
    }

    private bool IsInside(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < width && cell.y >= 0 && cell.y < height;
    }

    private int GridDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private int ChebyshevDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
    }

    private bool HasLineOfSight(Vector2Int fromCell, Vector2Int toCell)
    {
        return BattleTargetingService.HasLineOfSight(fromCell, toCell, TileAt, IsInside);
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

    private Vector2Int WorldToGrid(Vector2 world)
    {
        float gridXMinusY = world.x / Mathf.Max(0.001f, tileWidth * 0.5f);
        float gridXPlusY = world.y / Mathf.Max(0.001f, tileHeight * 0.5f);
        int x = Mathf.RoundToInt((gridXMinusY + gridXPlusY) * 0.5f);
        int y = Mathf.RoundToInt((gridXPlusY - gridXMinusY) * 0.5f);
        return new Vector2Int(x, y);
    }

    private Vector3 UnitWorldPosition(Vector2Int cell)
    {
        return GridToWorld(cell);
    }

    private IEnumerator PlayMapIntro()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            yield break;
        }

        busy = true;
        StopCameraPanRoutine();
        CenterCamera();
        yield return new WaitForSeconds(0.38f);

        yield return PanCamera(camera, GetDeploymentOverviewWorld(), CalculateDeploymentCameraSize(camera), 0.72f);

        busy = false;
        mapIntroCoroutine = null;
        RefreshHighlights();
    }

    private IEnumerator PanCamera(Camera camera, Vector3 targetWorld, float targetSize, float duration)
    {
        Vector3 fromPosition = camera.transform.position;
        Vector3 toPosition = CameraPositionForFocus(camera, targetWorld, targetSize);
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

    private IEnumerator PanCameraToUnit(BattleTestUnit unit, float duration)
    {
        Camera camera = Camera.main;
        if (camera == null || unit == null)
        {
            yield break;
        }

        StopCameraPanRoutine();
        yield return PanCamera(camera, FocusWorldForUnit(unit), CalculateTacticalCameraSize(camera), duration);
    }

    private IEnumerator CameraPanRoutine(Camera camera, Vector3 targetWorld, float targetSize, float duration)
    {
        yield return PanCamera(camera, targetWorld, targetSize, duration);
        cameraPanCoroutine = null;
    }

    private void FocusCameraOnUnit(BattleTestUnit unit, float duration)
    {
        if (suppressCameraFocus || unit == null)
        {
            return;
        }

        if (scoutMode)
        {
            FocusCameraOnDeploymentOverview(duration);
            return;
        }

        Camera camera = Camera.main;
        if (camera == null)
        {
            return;
        }

        float targetSize = CalculateTacticalCameraSize(camera);
        Vector3 targetWorld = FocusWorldForUnit(unit);
        StopCameraPanRoutine();

        if (!Application.isPlaying || duration <= 0f)
        {
            SetCameraFocusImmediate(camera, targetWorld, targetSize);
            return;
        }

        cameraPanCoroutine = StartCoroutine(CameraPanRoutine(camera, targetWorld, targetSize, duration));
    }

    private void FocusCameraOnDeploymentOverview(float duration)
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            return;
        }

        Vector3 targetWorld = GetDeploymentOverviewWorld();
        float targetSize = CalculateDeploymentCameraSize(camera);
        StopCameraPanRoutine();

        if (!Application.isPlaying || duration <= 0f)
        {
            SetCameraFocusImmediate(camera, targetWorld, targetSize);
            return;
        }

        cameraPanCoroutine = StartCoroutine(CameraPanRoutine(camera, targetWorld, targetSize, duration));
    }

    private void StopCameraPanRoutine()
    {
        if (cameraPanCoroutine == null)
        {
            return;
        }

        StopCoroutine(cameraPanCoroutine);
        cameraPanCoroutine = null;
    }

    private void SetCameraFocusImmediate(Camera camera, Vector3 targetWorld, float targetSize)
    {
        camera.transform.position = CameraPositionForFocus(camera, targetWorld, targetSize);
        camera.orthographicSize = targetSize;
    }

    private Vector3 CameraPositionForFocus(Camera camera, Vector3 targetWorld, float targetSize)
    {
        Bounds bounds = CalculateMapWorldBounds(0.75f);
        float aspect = Mathf.Max(0.1f, camera.aspect);
        float halfHeight = Mathf.Max(0.01f, targetSize);
        float halfWidth = halfHeight * aspect;
        float targetX = targetWorld.x;
        float targetY = targetWorld.y + CameraFocusYOffset;

        if (bounds.size.x <= halfWidth * 2f)
        {
            targetX = bounds.center.x;
        }
        else
        {
            targetX = Mathf.Clamp(targetX, bounds.min.x + halfWidth, bounds.max.x - halfWidth);
        }

        if (bounds.size.y <= halfHeight * 2f)
        {
            targetY = bounds.center.y;
        }
        else
        {
            targetY = Mathf.Clamp(targetY, bounds.min.y + halfHeight, bounds.max.y - halfHeight);
        }

        return new Vector3(targetX, targetY, camera.transform.position.z);
    }

    private Vector3 FocusWorldForUnit(BattleTestUnit unit)
    {
        if (unit.view != null)
        {
            return unit.view.transform.position;
        }

        return UnitWorldPosition(unit.cell);
    }

    private Vector3 GetFactionFocusWorld(Faction faction)
    {
        Vector3 total = Vector3.zero;
        int count = 0;
        foreach (BattleTestUnit unit in units)
        {
            if (unit.defeated || unit.definition.faction != faction)
            {
                continue;
            }

            total += UnitWorldPosition(unit.cell);
            count++;
        }

        return count == 0 ? MapCenterWorld() : total / count;
    }

    private Vector3 GetDeploymentOverviewWorld()
    {
        Bounds bounds = CalculateDeploymentUnitBounds();
        if (bounds.size == Vector3.zero)
        {
            return MapCenterWorld();
        }

        Vector3 center = bounds.center;
        center.y += 0.34f;
        return center;
    }

    private float CalculateTacticalCameraSize(Camera camera)
    {
        return Mathf.Clamp(CalculateFullMapCameraSize(camera) * 0.62f, TacticalCameraMinSize, TacticalCameraMaxSize);
    }

    private float CalculateDeploymentCameraSize(Camera camera)
    {
        Bounds bounds = CalculateDeploymentUnitBounds();
        if (bounds.size == Vector3.zero)
        {
            return CalculateFullMapCameraSize(camera);
        }

        float aspect = Mathf.Max(0.1f, camera.aspect);
        float sizeForHeight = bounds.extents.y + 1.55f;
        float sizeForWidth = (bounds.extents.x + 1.60f) / aspect;
        float desiredSize = Mathf.Max(sizeForHeight, sizeForWidth);
        return Mathf.Clamp(desiredSize, TacticalCameraMaxSize + 0.72f, CalculateFullMapCameraSize(camera));
    }

    private float CalculateFullMapCameraSize(Camera camera)
    {
        Bounds bounds = CalculateMapWorldBounds(0.8f);
        float aspect = Mathf.Max(0.1f, camera.aspect);
        float sizeForHeight = bounds.extents.y;
        float sizeForWidth = bounds.extents.x / aspect;
        return Mathf.Max(4.8f, Mathf.Max(sizeForHeight, sizeForWidth) + 0.2f);
    }

    private Vector3 MapCenterWorld()
    {
        return CalculateMapWorldBounds(0f).center;
    }

    private Bounds CalculateMapWorldBounds(float padding)
    {
        Vector3 first = GridToWorld(Vector2Int.zero);
        Bounds bounds = new Bounds(first, Vector3.zero);
        bounds.Encapsulate(GridToWorld(new Vector2Int(width - 1, 0)));
        bounds.Encapsulate(GridToWorld(new Vector2Int(0, height - 1)));
        bounds.Encapsulate(GridToWorld(new Vector2Int(width - 1, height - 1)));
        bounds.Expand(Mathf.Max(0f, padding) * 2f);
        return bounds;
    }

    private Bounds CalculateDeploymentUnitBounds()
    {
        bool hasBounds = false;
        Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
        foreach (BattleTestUnit unit in units)
        {
            if (unit == null || unit.defeated)
            {
                continue;
            }

            Vector3 position = UnitWorldPosition(unit.cell);
            if (!hasBounds)
            {
                bounds = new Bounds(position, Vector3.zero);
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(position);
            }

            bounds.Encapsulate(position + new Vector3(0f, 1.10f, 0f));
        }

        if (!hasBounds)
        {
            return new Bounds(Vector3.zero, Vector3.zero);
        }

        bounds.Expand(new Vector3(1.15f, 0.70f, 0f));
        return bounds;
    }

    private TerrainProfile ResolveTerrain(int x, int y)
    {
        switch (mapVariant)
        {
        case BattleTestMapVariant.BaekduMountainSnowfield:
            return ResolveBaekduMountainSnowfieldTerrain(x, y);
        case BattleTestMapVariant.BanditLair:
            return ResolveBanditLairTerrain(x, y);
        case BattleTestMapVariant.WolfPass:
            return ResolveWolfPassTerrain(x, y);
        case BattleTestMapVariant.TigerRavine:
            return ResolveTigerRavineTerrain(x, y);
        case BattleTestMapVariant.LeopardCliff:
            return ResolveLeopardCliffTerrain(x, y);
        case BattleTestMapVariant.SeorakPassRescue:
            return ResolveLeopardCliffTerrain(x, y);
        default:
            return ResolveBaekduSnowGateTerrain(x, y);
        }
    }

    private TerrainProfile ResolveBanditLairTerrain(int x, int y)
    {
        if (IsBanditLairOuterWall(x, y))
        {
            return new TerrainProfile(TerrainType.Forest, new Color(0.10f, 0.18f, 0.12f, 1f), y >= 8 ? 1 : 0, 0,
                                      99, false, true, false, false, false, "bandit_outer_forest_wall",
                                      "Dense mountain brush and felled trees: impassable edge of the lair.");
        }

        if (IsBanditLairPalisadeBlocker(x, y))
        {
            return new TerrainProfile(TerrainType.Wall, new Color(0.25f, 0.18f, 0.12f, 1f), y >= 8 ? 2 : 1, 0,
                                      99, false, true, false, false, false, "bandit_palisade_wall",
                                      "Sharpened log palisade: blocks movement and sight.");
        }

        if (IsBanditLairCaveWall(x, y))
        {
            return new TerrainProfile(TerrainType.Cliff, new Color(0.16f, 0.15f, 0.13f, 1f), 3, 0, 99, false, true,
                                      false, false, true, "bandit_cave_wall",
                                      "Old mine wall: blocked cave rock and fall edge.");
        }

        if (IsBanditLairLogBlocker(x, y))
        {
            return new TerrainProfile(TerrainType.Rubble, new Color(0.36f, 0.25f, 0.15f, 1f), y >= 7 ? 1 : 0, 3,
                                      99, false, true, false, false, false, "bandit_log_blocker",
                                      "Stacked lumber and thorn barricade: move around it.");
        }

        if (y == 4 && x >= 3 && x <= 12)
        {
            if (x == 7 || x == 8)
            {
                return new TerrainProfile(TerrainType.Bridge, new Color(0.42f, 0.28f, 0.16f, 1f), 0, 0, 1, true,
                                          false, true, false, false, "bandit_rope_bridge",
                                          "Narrow rope bridge over the drainage ditch.");
            }

            if (x == 3 || x == 12)
            {
                return new TerrainProfile(TerrainType.Mud, new Color(0.26f, 0.22f, 0.15f, 1f), 0, 1, 3, true,
                                          false, false, false, true, "bandit_muddy_bank",
                                          "Muddy ditch bank: passable but slow and exposed.");
            }

            return new TerrainProfile(TerrainType.DeepWater, new Color(0.06f, 0.13f, 0.12f, 1f), 0, 0, 99, false,
                                      false, false, false, true, "bandit_drainage_ditch",
                                      "Deep drainage ditch: cannot be crossed away from the bridge.");
        }

        if ((x == 5 && y == 6) || (x == 10 && y == 5) || (x == 9 && y == 8))
        {
            return new TerrainProfile(TerrainType.Trap, new Color(0.34f, 0.22f, 0.15f, 1f), 0, 0, 3, true, false,
                                      false, false, true, "bandit_hidden_trap",
                                      "Hidden snare pit: passable, costly, and dangerous.");
        }

        if (x >= 12 && x <= 14 && y >= 7 && y <= 9)
        {
            bool tower = x >= 13 && y >= 8;
            return new TerrainProfile(tower ? TerrainType.Roof : TerrainType.Hill,
                                      tower ? new Color(0.45f, 0.30f, 0.18f, 1f)
                                            : new Color(0.42f, 0.36f, 0.24f, 1f),
                                      tower ? 2 : 1, tower ? 2 : 1, tower ? 1 : 2, true, false,
                                      x == 12 && y == 7, false, tower, "bandit_watchtower_ridge",
                                      tower
                                          ? "Bandit watchtower: high ground with cover and long sight lines."
                                          : "Slope up toward the watchtower.");
        }

        if (x >= 6 && x <= 10 && y >= 9 && y <= 10)
        {
            bool cache = x == 8 && y == 10;
            return new TerrainProfile(cache ? TerrainType.Gate : TerrainType.Interior,
                                      cache ? new Color(0.58f, 0.42f, 0.22f, 1f)
                                            : new Color(0.31f, 0.25f, 0.18f, 1f),
                                      2, cache ? 2 : 1, 1, true, false, cache, cache, false,
                                      "bandit_cave_mouth",
                                      cache
                                          ? "Stolen supply cache at the mine mouth: mission objective."
                                          : "Mine mouth planks: defensible high ground.");
        }

        if (x >= 4 && x <= 11 && y >= 6 && y <= 8)
        {
            bool centralRoad = x >= 6 && x <= 9;
            bool cover = (x == 5 && y == 7) || (x == 10 && y == 7);
            return new TerrainProfile(centralRoad ? TerrainType.Road : TerrainType.Mud,
                                      centralRoad ? new Color(0.48f, 0.38f, 0.25f, 1f)
                                                  : new Color(0.30f, 0.26f, 0.18f, 1f),
                                      y >= 8 ? 1 : 0, cover ? 2 : 0, centralRoad ? 1 : 2, true, cover,
                                      centralRoad && y == 8, false, false, "bandit_camp_lane",
                                      "Main camp lane: faster route through the bandit tents and lumber piles.");
        }

        if (x <= 4 && y >= 5 && y <= 9)
        {
            return new TerrainProfile(TerrainType.Forest, new Color(0.15f, 0.31f, 0.18f, 1f), y >= 8 ? 1 : 0, 2,
                                      2, true, true, x == 4 && y == 7, false, false, "bandit_left_woods",
                                      "Left woods: slow cover route with blocked sight lines.");
        }

        if (x >= 4 && x <= 10 && y <= 3)
        {
            bool road = x >= 6 && x <= 9;
            return new TerrainProfile(road ? TerrainType.Road : TerrainType.Plain,
                                      road ? new Color(0.50f, 0.42f, 0.28f, 1f)
                                           : new Color(0.32f, 0.42f, 0.24f, 1f),
                                      0, 0, road ? 1 : 2, true, false, road && y == 2, false, false,
                                      "bandit_southern_logging_road",
                                      "Southern logging road: ally entry into the bandit lair.");
        }

        if (x >= 11 && y <= 6)
        {
            return new TerrainProfile(TerrainType.Forest, new Color(0.18f, 0.33f, 0.20f, 1f), 0, 1, 2, true, true,
                                      false, false, false, "bandit_right_scrub",
                                      "Right scrub path below the watchtower.");
        }

        return new TerrainProfile(TerrainType.Plain, new Color(0.34f, 0.43f, 0.25f, 1f), 0, 0, 2, true, false,
                                  false, false, false, "bandit_open_clearing",
                                  "Open lair clearing: uneven grass and dirt.");
    }

    private TerrainProfile ResolveWolfPassTerrain(int x, int y)
    {
        if (IsWolfPassOuterBlocker(x, y))
        {
            return new TerrainProfile(TerrainType.Forest, new Color(0.09f, 0.20f, 0.13f, 1f), y >= 8 ? 2 : 0, 0,
                                      99, false, true, false, false, false, "wolf_outer_forest_wall",
                                      "Dense birch and pine wall: impassable from map creation.");
        }

        if (IsWolfPassTreeBlocker(x, y))
        {
            return new TerrainProfile(TerrainType.Forest, new Color(0.10f, 0.24f, 0.15f, 1f), 1, 0, 99, false,
                                      true, false, false, false, "wolf_birch_blocker",
                                      "Thick birch trunk cluster: blocks movement and sight.");
        }

        if (IsWolfPassLogBlocker(x, y))
        {
            return new TerrainProfile(TerrainType.Rubble, new Color(0.36f, 0.25f, 0.15f, 1f), y >= 7 ? 1 : 0, 3,
                                      99, false, true, false, false, false, "wolf_fallen_log_blocker",
                                      "Fallen logs and thorn brush: impassable obstacle.");
        }

        if (IsWolfPassDenRockBlocker(x, y))
        {
            return new TerrainProfile(TerrainType.Cliff, new Color(0.24f, 0.25f, 0.20f, 1f), 2, 0, 99, false,
                                      true, false, false, true, "wolf_den_rock_wall",
                                      "Rock wall around the wolf den: cannot be crossed.");
        }

        if (y == 4 && x >= 3 && x <= 12)
        {
            if (x == 7 || x == 8)
            {
                return new TerrainProfile(TerrainType.Bridge, new Color(0.45f, 0.31f, 0.18f, 1f), 0, 0, 1, true,
                                          false, true, false, false, "wolf_creek_bridge",
                                          "Stepping-stone bridge across the cold creek.");
            }

            if (x == 5 || x == 10)
            {
                return new TerrainProfile(TerrainType.ShallowWater, new Color(0.20f, 0.46f, 0.50f, 1f), 0, 0, 3,
                                          true, false, false, false, true, "wolf_shallow_creek",
                                          "Shallow creek ford: slow and exposed.");
            }

            return new TerrainProfile(TerrainType.DeepWater, new Color(0.07f, 0.22f, 0.26f, 1f), 0, 0, 99, false,
                                      false, false, false, true, "wolf_deep_creek",
                                      "Deep creek cut: impassable away from bridge and fords.");
        }

        if (x >= 10 && x <= 13 && y >= 6 && y <= 10)
        {
            bool den = x == 12 && y == 10;
            bool ridgeTop = x >= 11 && y >= 7;
            int elevation = ridgeTop ? 2 : 1;
            return new TerrainProfile(den ? TerrainType.Gate : TerrainType.Hill,
                                      den ? new Color(0.55f, 0.43f, 0.25f, 1f)
                                          : new Color(0.43f, 0.46f, 0.28f, 1f),
                                      elevation, ridgeTop ? 1 : 0, ridgeTop ? 1 : 2, true, false,
                                      x == 10 && y == 6, den, ridgeTop && x == 13, "wolf_eastern_ridge",
                                      den
                                          ? "Wolf den objective on the eastern ridge."
                                          : "Eastern ridge: high ground with one-level climb routes.");
        }

        if (x <= 4 && y >= 5 && y <= 9)
        {
            return new TerrainProfile(TerrainType.Forest, new Color(0.14f, 0.32f, 0.18f, 1f), y >= 8 ? 1 : 0, 2,
                                      2, true, true, x == 4 && y == 7, false, false, "wolf_left_birch_woods",
                                      "Left birch woods: slow covered flank with blocked sight lines.");
        }

        if (x >= 5 && x <= 10 && y <= 3)
        {
            bool road = x >= 6 && x <= 9;
            return new TerrainProfile(road ? TerrainType.Road : TerrainType.Plain,
                                      road ? new Color(0.52f, 0.45f, 0.30f, 1f)
                                           : new Color(0.36f, 0.46f, 0.25f, 1f),
                                      0, 0, road ? 1 : 2, true, false, road && y == 2, false, false,
                                      "wolf_southern_pasture_road",
                                      "Southern pasture road: ally entry and herder escape route.");
        }

        if (x >= 5 && x <= 9 && y >= 5 && y <= 8)
        {
            bool path = x == 7 || x == 8;
            bool danger = x == 6 && y == 6;
            return new TerrainProfile(path ? TerrainType.Road : TerrainType.Plain,
                                      path ? new Color(0.50f, 0.42f, 0.28f, 1f)
                                           : new Color(0.34f, 0.44f, 0.25f, 1f),
                                      y >= 7 ? 1 : 0, danger ? 1 : 0, path ? 1 : 2, true, false,
                                      path && y == 6, false, danger, "wolf_central_pass",
                                      "Central pass: uneven ground between creek and den ridge.");
        }

        return new TerrainProfile(TerrainType.Plain, new Color(0.35f, 0.46f, 0.25f, 1f), 0, 0, 2, true, false,
                                  false, false, false, "wolf_open_pasture",
                                  "Open pasture grass: standard movement around the wolf pass.");
    }

    private TerrainProfile ResolveTigerRavineTerrain(int x, int y)
    {
        if (IsTigerRavineOuterBlocker(x, y))
        {
            return new TerrainProfile(TerrainType.Cliff, new Color(0.20f, 0.19f, 0.16f, 1f), y >= 8 ? 3 : 1, 0,
                                      99, false, true, false, false, true, "tiger_outer_cliff_wall",
                                      "Ravine edge and dense brush: impassable boundary.");
        }

        if (IsTigerRavineCliffBlocker(x, y))
        {
            return new TerrainProfile(TerrainType.Cliff, new Color(0.26f, 0.24f, 0.20f, 1f), 2, 0, 99, false,
                                      true, false, false, true, "tiger_central_cliff_wall",
                                      "Central rock wall: blocks direct movement through the ravine.");
        }

        if (IsTigerRavineBoulderBlocker(x, y))
        {
            return new TerrainProfile(TerrainType.Rubble, new Color(0.38f, 0.34f, 0.28f, 1f), 1, 4, 99, false,
                                      true, false, false, false, "tiger_boulder_blocker",
                                      "Collapsed boulder: full obstacle, not decorative.");
        }

        if (x >= 11 && x <= 14 && y >= 7 && y <= 10)
        {
            bool objective = x == 14 && y == 9;
            int elevation = x >= 13 && y >= 8 ? 3 : x >= 12 ? 2 : 1;
            return new TerrainProfile(objective ? TerrainType.Gate : TerrainType.Hill,
                                      objective ? new Color(0.60f, 0.46f, 0.27f, 1f)
                                                : new Color(0.44f, 0.40f, 0.30f, 1f),
                                      elevation, elevation >= 2 ? 1 : 0, elevation >= 2 ? 1 : 2, true, false,
                                      x == 11 && y == 7, objective, elevation >= 3, "tiger_eastern_rock_shelf",
                                      objective
                                          ? "Trapped villagers on the eastern H3 rock shelf."
                                          : "Eastern rock shelf: strong high ground reached by staged climbs.");
        }

        if (x >= 2 && x <= 5 && y >= 5 && y <= 8)
        {
            return new TerrainProfile(TerrainType.Forest, new Color(0.42f, 0.44f, 0.22f, 1f), 0, 2, 2, true,
                                      true, x == 5 && y == 6, false, false, "tiger_reed_cover",
                                      "Tall reed cover: slow but safe approach for rescuers.");
        }

        if (x >= 7 && x <= 10 && y >= 5 && y <= 8)
        {
            bool dust = (x == 8 && y == 5) || (x == 9 && y == 6);
            return new TerrainProfile(dust ? TerrainType.Rubble : TerrainType.Road,
                                      dust ? new Color(0.43f, 0.36f, 0.27f, 1f)
                                           : new Color(0.52f, 0.43f, 0.30f, 1f),
                                      y >= 7 ? 1 : 0, dust ? 1 : 0, dust ? 2 : 1, true, false,
                                      x == 8 && y == 6, false, dust, "tiger_ravine_floor",
                                      "Ravine floor: main lane with loose rock hazards.");
        }

        if (x >= 4 && x <= 10 && y <= 3)
        {
            bool road = x >= 6 && x <= 9;
            return new TerrainProfile(road ? TerrainType.Road : TerrainType.Plain,
                                      road ? new Color(0.53f, 0.45f, 0.30f, 1f)
                                           : new Color(0.38f, 0.45f, 0.27f, 1f),
                                      0, 0, road ? 1 : 2, true, false, road && y == 2, false, false,
                                      "tiger_southern_rescue_road",
                                      "Southern rescue road: ally entry into the ravine.");
        }

        if (x >= 10 && x <= 12 && y >= 4 && y <= 6)
        {
            return new TerrainProfile(TerrainType.Mud, new Color(0.35f, 0.30f, 0.22f, 1f), y >= 6 ? 1 : 0, 1, 2,
                                      true, false, false, false, y == 6, "tiger_muddy_slope",
                                      "Muddy slope toward the rock shelf: passable but exposed.");
        }

        return new TerrainProfile(TerrainType.Plain, new Color(0.38f, 0.43f, 0.26f, 1f), 0, 0, 2, true, false,
                                  false, false, false, "tiger_open_ravine",
                                  "Open ravine grass and gravel.");
    }

    private TerrainProfile ResolveLeopardCliffTerrain(int x, int y)
    {
        if (IsLeopardCliffOuterBlocker(x, y))
        {
            return new TerrainProfile(TerrainType.Cliff, new Color(0.18f, 0.19f, 0.17f, 1f), y >= 7 ? 3 : 1, 0,
                                      99, false, true, false, false, true, "leopard_outer_cliff_wall",
                                      "Outer cliff and brush: impassable map edge.");
        }

        if (IsLeopardCliffBambooBlocker(x, y))
        {
            return new TerrainProfile(TerrainType.Bamboo, new Color(0.09f, 0.25f, 0.16f, 1f), y >= 8 ? 2 : 1, 0,
                                      99, false, true, false, false, false, "leopard_bamboo_blocker",
                                      "Dense bamboo wall: blocks movement and line of sight.");
        }

        if (IsLeopardCliffRockBlocker(x, y))
        {
            return new TerrainProfile(TerrainType.Cliff, new Color(0.25f, 0.24f, 0.21f, 1f), 2, 0, 99, false,
                                      true, false, false, true, "leopard_rock_drop_blocker",
                                      "Sheer rock drop: cannot be crossed.");
        }

        if (y == 5 && x >= 6 && x <= 9)
        {
            if (x == 8 || x == 9)
            {
                return new TerrainProfile(TerrainType.Bridge, new Color(0.42f, 0.28f, 0.16f, 1f), 1, 0, 1, true,
                                          false, true, false, false, "leopard_rope_bridge",
                                          "Rope bridge over the cliff cut.");
            }

            return new TerrainProfile(TerrainType.DeepWater, new Color(0.08f, 0.14f, 0.17f, 1f), 0, 0, 99, false,
                                      false, false, false, true, "leopard_cliff_gap",
                                      "Open cliff gap: impassable except by rope bridge.");
        }

        if (x >= 11 && x <= 14 && y >= 6 && y <= 9)
        {
            bool objective = x == 14 && y == 8;
            int elevation = x >= 13 && y >= 8 ? 3 : x >= 12 ? 2 : 1;
            return new TerrainProfile(objective ? TerrainType.Gate : TerrainType.Hill,
                                      objective ? new Color(0.58f, 0.48f, 0.27f, 1f)
                                                : new Color(0.39f, 0.42f, 0.27f, 1f),
                                      elevation, elevation >= 2 ? 1 : 0, elevation >= 2 ? 1 : 2, true, false,
                                      x == 11 && y == 6, objective, elevation >= 3, "leopard_herb_shelf",
                                      objective
                                          ? "Rare herb shelf objective on H3 cliff high ground."
                                          : "Northeast herb shelf: staged climb and leopard ambush ground.");
        }

        if (x <= 5 && y >= 6 && y <= 9)
        {
            bool mist = x == 5 && y == 8;
            return new TerrainProfile(mist ? TerrainType.Smoke : TerrainType.Bamboo,
                                      mist ? new Color(0.43f, 0.50f, 0.44f, 1f)
                                           : new Color(0.16f, 0.36f, 0.21f, 1f),
                                      y >= 8 ? 2 : 1, 2, 2, true, true, x == 4 && y == 7, false, mist,
                                      "leopard_left_bamboo_path",
                                      "Left bamboo path: covered, slow, and vision-blocking.");
        }

        if (x >= 4 && x <= 10 && y >= 2 && y <= 4)
        {
            bool road = y == 3 || x >= 7;
            return new TerrainProfile(road ? TerrainType.Road : TerrainType.Plain,
                                      road ? new Color(0.50f, 0.42f, 0.29f, 1f)
                                           : new Color(0.36f, 0.46f, 0.26f, 1f),
                                      0, 0, road ? 1 : 2, true, false, road && x == 7, false, false,
                                      "leopard_southern_cliff_road",
                                      "Southern cliff road: ally escort entry.");
        }

        if (x >= 9 && x <= 11 && y >= 5 && y <= 7)
        {
            bool ambush = x == 11 && y == 7;
            return new TerrainProfile(TerrainType.Rubble, new Color(0.39f, 0.36f, 0.29f, 1f), y >= 7 ? 1 : 0,
                                      ambush ? 1 : 0, 2, true, false, false, false, ambush,
                                      "leopard_rocky_connector",
                                      "Rocky connector from rope bridge to herb shelf.");
        }

        return new TerrainProfile(TerrainType.Plain, new Color(0.35f, 0.43f, 0.25f, 1f), 0, 0, 2, true, false,
                                  false, false, false, "leopard_open_cliff_grass",
                                  "Open cliff grass: standard movement around the escort route.");
    }

    private TerrainProfile ResolveBaekduMountainSnowfieldTerrain(int x, int y)
    {
        if (IsSnowfieldDensePineBlocker(x, y))
        {
            return new TerrainProfile(TerrainType.Forest, new Color(0.08f, 0.19f, 0.16f, 1f), y >= 8 ? 1 : 0, 0,
                                      99, false, true, false, false, false, "snow_pine_wall",
                                      "Dense Baekdu snow pine stand: blocks movement and line of sight.");
        }

        if (IsSnowfieldBasaltCliffBlocker(x, y))
        {
            return new TerrainProfile(TerrainType.Cliff, new Color(0.16f, 0.18f, 0.21f, 1f), y >= 10 ? 3 : 2, 0,
                                      99, false, true, false, false, true, "basalt_cliff_wall",
                                      "Black basalt cliff: impassable ridge and sight blocker.");
        }

        if (IsSnowfieldNaturalPropBlocker(x, y))
        {
            return new TerrainProfile(TerrainType.Rubble, new Color(0.46f, 0.49f, 0.50f, 1f), y >= 8 ? 2 : 1, 4,
                                      99, false, true, false, false, false, "snowfield_natural_blocker",
                                      "Large snow pine, basalt boulder, or packed drift: move around it.");
        }

        if ((y == 3 || y == 4) && x >= 4 && x <= 11)
        {
            if ((x == 7 || x == 8) && y == 3)
            {
                return new TerrainProfile(TerrainType.Ice, new Color(0.52f, 0.78f, 0.90f, 1f), 0, 0, 1, true,
                                          false, true, false, true, "frozen_crossing",
                                          "Narrow frozen crossing: the safe route through the ice channel.");
            }

            if (x == 4 || x == 11)
            {
                return new TerrainProfile(TerrainType.ShallowWater, new Color(0.30f, 0.56f, 0.66f, 1f), 0, 0, 3,
                                          true, false, false, false, true, "thin_ice_edge",
                                          "Thin ice edge: slow but passable.");
            }

            return new TerrainProfile(TerrainType.DeepWater, new Color(0.08f, 0.28f, 0.38f, 1f), 0, 0, 99, false,
                                      false, false, false, true, "deep_frozen_channel",
                                      "Deep frozen water: impassable ice channel.");
        }

        if (x >= 12 && x <= 14 && y == 7)
        {
            return new TerrainProfile(TerrainType.Hill, new Color(0.62f, 0.67f, 0.66f, 1f), 2, 1, 2, true,
                                      false, x == 13, false, false, "hot_spring_snow_ramp",
                                      "Snow ramp toward the hot spring ridge: high ground is reachable but costly.");
        }

        if (x >= 12 && x <= 14 && y >= 8 && y <= 10)
        {
            bool hotSpring = x == 12 && y == 8;
            bool objective = x == 13 && y == 9;
            TerrainType terrain = hotSpring ? TerrainType.Smoke : objective ? TerrainType.Gate : TerrainType.Hill;
            return new TerrainProfile(terrain,
                                      hotSpring ? new Color(0.58f, 0.80f, 0.74f, 1f)
                                                : new Color(0.60f, 0.64f, 0.62f, 1f),
                                      3, hotSpring ? 2 : 1, hotSpring ? 2 : 1, true, hotSpring,
                                      x == 12 || objective, objective, hotSpring, "hot_spring_highground",
                                      objective
                                          ? "Baekdu summit marker objective: visible high ground beyond the snow pass."
                                          : "Hot spring high ground: warm steam and strong line-of-sight control.");
        }

        if (x <= 3 && y >= 4 && y <= 9)
        {
            bool choke = (x == 2 && y == 7) || (x == 3 && y == 8);
            return new TerrainProfile(TerrainType.Forest, new Color(0.18f, 0.34f, 0.30f, 1f), y >= 8 ? 1 : 0, 2, 2,
                                      true, true, choke, false, false, "snow_pine_flank",
                                      "Snow pine flank: slow cover route with blocked sight lines.");
        }

        if (x >= 4 && x <= 11 && y >= 7 && y <= 9)
        {
            bool ridge = y == 9 || (x >= 9 && y >= 8);
            bool driftCover = (x == 5 && y == 8) || (x == 9 && y == 9);
            return new TerrainProfile(ridge ? TerrainType.Hill : TerrainType.Snow,
                                      ridge ? new Color(0.66f, 0.70f, 0.68f, 1f)
                                            : new Color(0.80f, 0.84f, 0.84f, 1f),
                                      ridge ? 2 : 1, driftCover ? 2 : 0, driftCover ? 2 : 1, true, false,
                                      x == 7 && y == 8, false, false, "central_snowfield",
                                      "Central Baekdu snowfield: open movement lane between pine and basalt.");
        }

        if (x >= 4 && x <= 11 && y >= 5 && y <= 6)
        {
            bool road = x >= 5 && x <= 10;
            return new TerrainProfile(road ? TerrainType.Road : TerrainType.Snow,
                                      road ? new Color(0.66f, 0.62f, 0.52f, 1f)
                                           : new Color(0.78f, 0.82f, 0.82f, 1f),
                                      1, 0, road ? 1 : 2, true, false, x == 7 || x == 8, false, false,
                                      "snow_pass_approach",
                                      "Wind-carved snow pass approach: main route toward the hot spring ridge.");
        }

        if (x >= 4 && x <= 10 && y <= 2)
        {
            bool road = x >= 5 && x <= 8;
            return new TerrainProfile(road ? TerrainType.Road : TerrainType.Plain,
                                      road ? new Color(0.62f, 0.58f, 0.50f, 1f)
                                           : new Color(0.76f, 0.81f, 0.82f, 1f),
                                      0, 0, road ? 1 : 2, true, false, x == 7 && y == 2, false, false,
                                      "southern_snow_entry",
                                      "Southern snow entry: enemy approach through packed snow.");
        }

        if (x >= 11 && y <= 6)
        {
            return new TerrainProfile(TerrainType.Ice, new Color(0.54f, 0.72f, 0.80f, 1f), 0, 0, 2, true, false,
                                      false, false, true, "right_ice_shoal",
                                      "Right ice shoal: slippery alternate route below the basalt ridge.");
        }

        if (y >= 10)
        {
            return new TerrainProfile(TerrainType.Cliff, new Color(0.20f, 0.22f, 0.24f, 1f), 3, 0, 99, false, true,
                                      false, false, true, "northern_basalt_wall",
                                      "Northern basalt wall: impassable map edge.");
        }

        return new TerrainProfile(TerrainType.Snow, new Color(0.78f, 0.82f, 0.83f, 1f), 0, 0, 2, true, false,
                                  false, false, false, "open_snowfield",
                                  "Open Baekdu snowfield: soft snow with slower movement.");
    }

    private TerrainProfile ResolveBaekduSnowGateTerrain(int x, int y)
    {
        if (BattleMapRuntimeCatalog.TryGetCell(mapVariant, new Vector2Int(x, y), out BattleMapRuntimeCell data))
        {
            return TerrainProfileFromRuntimeCell(data);
        }

        return new TerrainProfile(TerrainType.Wall, new Color(0.18f, 0.18f, 0.18f, 1f), 2, 0, 99, false, true,
                                  false, false, true, "missing_runtime_cell",
                                  "Missing runtime cell: blocked by default.");
    }

    private static bool IsBaekduSnowGatePaintedNoStandCell(Vector2Int cell)
    {
        return !IsBaekduSnowGatePaintedStandCell(cell.x, cell.y);
    }

    private static bool IsBaekduSnowGatePaintedStandCell(int x, int y)
    {
        if (BattleMapRuntimeCatalog.TryGetCell(BattleTestMapVariant.BaekduSnowGate, new Vector2Int(x, y),
                                               out BattleMapRuntimeCell data))
        {
            return data.walkable && data.occupyAllowed && data.moveCost < 90;
        }

        return false;
    }

    private static bool IsBaekduSnowGateDeploymentStartCell(int x, int y)
    {
        if (BattleMapRuntimeCatalog.TryGetCell(BattleTestMapVariant.BaekduSnowGate, new Vector2Int(x, y),
                                               out BattleMapRuntimeCell data))
        {
            return data.deployZone > 0 && data.walkable && data.occupyAllowed;
        }

        return false;
    }

    private static TerrainProfile BaekduSnowGatePaintedBlockerProfile(int x, int y)
    {
        if ((y >= 5 && x >= 7 && x <= 12) || (y >= 4 && x >= 9 && x <= 12))
        {
            return new TerrainProfile(TerrainType.Wall, new Color(0.24f, 0.23f, 0.22f, 1f), 2, 0, 99, false,
                                      true, false, false, true, "painted_gate_wall",
                                      "Painted gate wall, palisade, and upper stair facade: impassable backdrop obstacle.");
        }

        if (x >= 10 && y <= 3)
        {
            return new TerrainProfile(TerrainType.Forest, new Color(0.08f, 0.17f, 0.14f, 1f), 1, 0, 99, false,
                                      true, false, false, false, "painted_right_gate_edge",
                                      "Painted right-side fence and pine edge: outside the usable stone approach.");
        }

        if (x <= 3 && y >= 4)
        {
            return new TerrainProfile(TerrainType.DeepWater, new Color(0.08f, 0.22f, 0.31f, 1f), 0, 0, 99, false,
                                      false, false, false, true, "painted_left_ice_water",
                                      "Painted ice water and cliff edge: never valid for standing or deployment.");
        }

        if (x >= 13 && y <= 6)
        {
            return new TerrainProfile(TerrainType.Forest, new Color(0.08f, 0.17f, 0.14f, 1f), 1, 0, 99, false,
                                      true, false, false, false, "painted_right_forest",
                                      "Painted right-side pine and boulder mass: blocks movement and line of sight.");
        }

        if (y >= 8 && x <= 9)
        {
            return new TerrainProfile(TerrainType.Wall, new Color(0.24f, 0.23f, 0.22f, 1f), 2, 0, 99, false,
                                      true, false, false, true, "painted_gate_wall",
                                      "Painted gate wall, rocks, and palisade: impassable backdrop obstacle.");
        }

        if (y >= 10 || x <= 3 || x >= 15)
        {
            return new TerrainProfile(TerrainType.Cliff, new Color(0.18f, 0.20f, 0.23f, 1f), 2, 0, 99, false,
                                      true, false, false, true, "painted_cliff_edge",
                                      "Painted cliff or outer-map edge: not a legal standing tile.");
        }

        return new TerrainProfile(TerrainType.Rubble, new Color(0.34f, 0.34f, 0.32f, 1f), 1, 2, 99, false,
                                  true, false, false, false, "painted_static_obstacle",
                                  "Painted boulder, fence, or tree mass: blocked so units cannot stand on the art.");
    }

    private static bool IsSnowfieldDensePineBlocker(int x, int y)
    {
        return (x == 0 && y >= 0 && y <= 11) ||
               (x == 1 && (y <= 2 || y >= 8)) ||
               (x == 2 && y >= 10) ||
               (x == 3 && y == 11);
    }

    private static bool IsSnowfieldBasaltCliffBlocker(int x, int y)
    {
        return y == 11 ||
               (y == 10 && x >= 4 && x <= 11) ||
               (x == 4 && y >= 9 && y <= 10) ||
               (x == 11 && y >= 9 && y <= 10);
    }

    private static bool IsSnowfieldNaturalPropBlocker(int x, int y)
    {
        return (x == 2 && y == 9) ||
               (x == 11 && y == 8) ||
               (x == 13 && y == 6);
    }

    private static bool IsBanditLairOuterWall(int x, int y)
    {
        return x == 0 ||
               y == 11 ||
               (x == 1 && (y <= 2 || y >= 8)) ||
               (x == 15 && y >= 2) ||
               (x == 14 && y >= 10);
    }

    private static bool IsBanditLairPalisadeBlocker(int x, int y)
    {
        return (x == 5 && y >= 8 && y <= 10) ||
               (x == 11 && y >= 8 && y <= 10) ||
               (y == 9 && (x == 4 || x == 12));
    }

    private static bool IsBanditLairCaveWall(int x, int y)
    {
        return (y == 10 && (x <= 4 || x >= 12)) ||
               (y == 9 && (x == 13 || x == 14));
    }

    private static bool IsBanditLairLogBlocker(int x, int y)
    {
        return (x == 3 && y == 6) ||
               (x == 6 && y == 6) ||
               (x == 10 && y == 8) ||
               (x == 2 && y == 4);
    }

    private static bool IsWolfPassOuterBlocker(int x, int y)
    {
        return x == 0 ||
               y == 11 ||
               (x == 1 && (y <= 2 || y >= 9)) ||
               (x == 14 && y >= 3);
    }

    private static bool IsWolfPassTreeBlocker(int x, int y)
    {
        return (x == 2 && y == 9) ||
               (x == 3 && y == 10) ||
               (x == 4 && y == 9);
    }

    private static bool IsWolfPassLogBlocker(int x, int y)
    {
        return (x == 4 && y == 6) ||
               (x == 6 && y == 6) ||
               (x == 11 && y == 8);
    }

    private static bool IsWolfPassDenRockBlocker(int x, int y)
    {
        return (x == 11 && y == 10) ||
               (x == 13 && (y == 9 || y == 10));
    }

    private static bool IsTigerRavineOuterBlocker(int x, int y)
    {
        return x == 0 ||
               y == 11 ||
               (x == 1 && (y <= 1 || y >= 9)) ||
               (x == 15 && y >= 2);
    }

    private static bool IsTigerRavineCliffBlocker(int x, int y)
    {
        return x == 6 && y >= 3 && y <= 9 && y != 5 && y != 6;
    }

    private static bool IsTigerRavineBoulderBlocker(int x, int y)
    {
        return (x == 9 && y == 4) ||
               (x == 10 && y == 4) ||
               (x == 10 && y == 5) ||
               (x == 7 && y == 8);
    }

    private static bool IsLeopardCliffOuterBlocker(int x, int y)
    {
        return x == 0 ||
               y == 11 ||
               (x == 1 && y >= 10) ||
               (x == 15 && y >= 4);
    }

    private static bool IsLeopardCliffBambooBlocker(int x, int y)
    {
        return (x == 2 && y == 8) ||
               (x == 3 && y == 9) ||
               (x == 6 && y == 8) ||
               (x == 7 && y == 8);
    }

    private static bool IsLeopardCliffRockBlocker(int x, int y)
    {
        return (x == 12 && y == 5) ||
               (x == 13 && y == 5) ||
               (x == 14 && y == 5) ||
               (x == 14 && y == 10);
    }

    private static bool IsDenseTreeBlocker(int x, int y)
    {
        return (x == 0 && y >= 0 && y <= 10) ||
               (x == 1 && (y == 0 || y >= 9)) ||
               (x == 3 && y == 10);
    }

    private static bool IsSolidNaturalPropBlocker(int x, int y)
    {
        return (x == 2 && y == 9) ||
               (x == 11 && y == 8);
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

        camera.orthographic = true;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.20f, 0.30f, 0.27f, 1f);
        camera.transform.position = new Vector3(camera.transform.position.x, camera.transform.position.y, -10f);
        SetCameraFocusImmediate(camera, MapCenterWorld(), CalculateFullMapCameraSize(camera));
    }

    private bool PointerOverHud(Vector3 screenPosition)
    {
        if (battleHud != null)
        {
            return battleHud.PointerOverHud(screenPosition);
        }

        if (hudCanvas != null && hudCanvas.isActiveAndEnabled && EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
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
        terrainAssetSprites.Clear();
        interactableAssetSprites.Clear();
        string backdropResource = PaintedBattleMapResource;
        paintedMapBackdropSprite = string.IsNullOrEmpty(backdropResource)
                                       ? null
                                       : Resources.Load<Sprite>(backdropResource);
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

        if (mapVariant == BattleTestMapVariant.BaekduMountainSnowfield)
        {
            ApplyBaekduSnowfieldSprites();
        }

        if (paintedMapBackdropSprite == null)
        {
            ApplyBattleVisualProfileSprites();
        }
    }

    private void ApplyBaekduSnowfieldSprites()
    {
        terrainAssetSprites[TerrainType.Plain] = LoadMapSprite("Tiles/baekdu_snow_plain");
        terrainAssetSprites[TerrainType.Snow] = LoadMapSprite("Tiles/baekdu_deep_snow");
        terrainAssetSprites[TerrainType.Hill] = LoadMapSprite("Tiles/baekdu_wind_snow_ridge");
        terrainAssetSprites[TerrainType.Ice] = LoadMapSprite("Tiles/baekdu_ice_slick");
        terrainAssetSprites[TerrainType.ShallowWater] = LoadMapSprite("Tiles/baekdu_frozen_stream");
        terrainAssetSprites[TerrainType.DeepWater] = LoadMapSprite("Tiles/baekdu_dark_frozen_water");
        terrainAssetSprites[TerrainType.Road] = LoadMapSprite("Tiles/baekdu_frozen_stair_road");
        terrainAssetSprites[TerrainType.Stone] = LoadMapSprite("Tiles/baekdu_volcanic_snow_rock");
        terrainAssetSprites[TerrainType.Cliff] = LoadMapSprite("Tiles/baekdu_snow_basalt_cliff");
        terrainAssetSprites[TerrainType.Wall] = terrainAssetSprites[TerrainType.Cliff];
        terrainAssetSprites[TerrainType.ShrineFloor] = LoadMapSprite("Tiles/baekdu_snow_shrine_floor");
        terrainAssetSprites[TerrainType.Gate] = LoadMapSprite("Tiles/baekdu_snow_mountain_pass");
        terrainAssetSprites[TerrainType.Forest] = LoadMapSprite("Tiles/baekdu_snow_pine_floor");
        terrainAssetSprites[TerrainType.Bamboo] = LoadMapSprite("Tiles/baekdu_snow_bamboo_floor");
        terrainAssetSprites[TerrainType.Mud] = LoadMapSprite("Tiles/baekdu_snow_mountain_pass");
        terrainAssetSprites[TerrainType.Rubble] = LoadMapSprite("Tiles/baekdu_snow_stone_courtyard");
        terrainAssetSprites[TerrainType.Smoke] = LoadMapSprite("Tiles/baekdu_hot_spring_ground");
        terrainAssetSprites[TerrainType.Trap] = LoadMapSprite("Tiles/baekdu_cracked_ice_hazard");

        interactableAssetSprites["baekdu_snow_pine"] = LoadMapSprite("Objects/baekdu_snow_pine");
        interactableAssetSprites["baekdu_snow_boulder"] = LoadMapSprite("Objects/baekdu_snow_boulder");
        interactableAssetSprites["baekdu_snowdrift_cover"] = LoadMapSprite("Objects/baekdu_snowdrift_cover");
        interactableAssetSprites["baekdu_ice_crystal"] = LoadMapSprite("Objects/baekdu_ice_crystal");
        interactableAssetSprites["baekdu_hot_spring_steam"] = LoadMapSprite("Objects/baekdu_hot_spring_steam");
        interactableAssetSprites["baekdu_frozen_stone_lantern"] = LoadMapSprite("Objects/baekdu_frozen_stone_lantern");
        interactableAssetSprites["baekdu_frozen_rope_posts"] = LoadMapSprite("Objects/baekdu_frozen_rope_posts");
        interactableAssetSprites["baekdu_broken_snow_gate"] = LoadMapSprite("Objects/baekdu_broken_snow_gate");
    }

    private bool UseBattleVisualProfileSprites()
    {
        return battleVisualProfile != null && mapVariant == BattleTestMapVariant.BaekduMountainSnowfield;
    }

    private void ApplyBattleVisualProfileSprites()
    {
        if (!UseBattleVisualProfileSprites())
        {
            return;
        }

        AssignTerrainProfileSprite(TerrainType.Plain, battleVisualProfile.groundTiles, "snow_ground_01");
        AssignTerrainProfileSprite(TerrainType.Snow, battleVisualProfile.groundTiles, "snow_ground_02");
        AssignTerrainProfileSprite(TerrainType.Hill, battleVisualProfile.groundTiles, "snow_ground_03");
        AssignTerrainProfileSprite(TerrainType.Forest, battleVisualProfile.groundTiles, "snow_ground_03");
        AssignTerrainProfileSprite(TerrainType.Bamboo, battleVisualProfile.groundTiles, "snow_ground_02");
        AssignTerrainProfileSprite(TerrainType.Road, battleVisualProfile.roadTiles, "packed_snow_road_01");
        AssignTerrainProfileSprite(TerrainType.Mud, battleVisualProfile.roadTiles, "packed_snow_road_01");
        AssignTerrainProfileSprite(TerrainType.Gate, battleVisualProfile.roadTiles, "stone_stair_snow_01");
        AssignTerrainProfileSprite(TerrainType.Stone, battleVisualProfile.roadTiles, "stone_stair_snow_01");
        AssignTerrainProfileSprite(TerrainType.ShrineFloor, battleVisualProfile.groundTiles,
                                   "shrine_floor_ruined_01");
        AssignTerrainProfileSprite(TerrainType.ShallowWater, battleVisualProfile.waterTiles, "frozen_stream_01");
        AssignTerrainProfileSprite(TerrainType.DeepWater, battleVisualProfile.waterTiles, "frozen_stream_01");
        AssignTerrainProfileSprite(TerrainType.Water, battleVisualProfile.waterTiles, "frozen_stream_01");
        AssignTerrainProfileSprite(TerrainType.Ice, battleVisualProfile.waterTiles, "ice_crack_01");
        AssignTerrainProfileSprite(TerrainType.Trap, battleVisualProfile.waterTiles, "ice_crack_01");
        AssignTerrainProfileSprite(TerrainType.Cliff, battleVisualProfile.cliffTiles, "cliff_top_snow_01");
        AssignTerrainProfileSprite(TerrainType.Wall, battleVisualProfile.cliffTiles, "cliff_side_ice_01");
        AssignTerrainProfileSprite(TerrainType.Rubble, battleVisualProfile.decorTiles, "burned_ground_01");
        AssignTerrainProfileSprite(TerrainType.Fire, battleVisualProfile.decorTiles, "burned_ground_01");
        AssignTerrainProfileSprite(TerrainType.Smoke, battleVisualProfile.decorTiles, "smoke_ground_01");

        AssignInteractableProfileSprite("baekdu_broken_snow_gate", "broken_sect_gate");
        AssignInteractableProfileSprite("baekdu_hot_spring_steam", "incense_burner_frozen");
        AssignInteractableProfileSprite("baekdu_ice_crystal", "snow_rock_cover_02");
        AssignInteractableProfileSprite("baekdu_snow_pine", "frozen_pine_large");
        AssignInteractableProfileSprite("baekdu_snow_boulder", "snow_rock_cover_01");
        AssignInteractableProfileSprite("baekdu_snowdrift_cover", "snow_rock_cover_02");
        AssignInteractableProfileSprite("baekdu_frozen_stone_lantern", "stone_lantern_snow");
        AssignInteractableProfileSprite("baekdu_frozen_rope_posts", "ice_bridge_rope");
        AssignInteractableProfileSprite("signboard", "broken_signboard_haedongmun");
        AssignInteractableProfileSprite("lantern", "torch_stand_lit");
        AssignInteractableProfileSprite("fire", "torch_stand_lit");
        AssignInteractableProfileSprite("stone_lantern", "stone_lantern_snow");
        AssignInteractableProfileSprite("snow_pine", "frozen_pine_large");
        AssignInteractableProfileSprite("frozen_boulder", "snow_rock_cover_01");
    }

    private void AssignTerrainProfileSprite(TerrainType terrain, Sprite[] candidates, params string[] nameHints)
    {
        Sprite sprite = FindVisualProfileSprite(candidates, nameHints);
        if (sprite != null)
        {
            terrainAssetSprites[terrain] = sprite;
        }
    }

    private void AssignInteractableProfileSprite(string id, params string[] nameHints)
    {
        Sprite sprite = FindVisualProfileSprite(battleVisualProfile.propSprites, nameHints);
        if (sprite != null)
        {
            interactableAssetSprites[id] = sprite;
        }
    }

    private static Sprite FindVisualProfileSprite(Sprite[] candidates, params string[] nameHints)
    {
        if (candidates == null || nameHints == null)
        {
            return null;
        }

        foreach (string hint in nameHints)
        {
            if (string.IsNullOrEmpty(hint))
            {
                continue;
            }

            for (int i = 0; i < candidates.Length; i++)
            {
                Sprite sprite = candidates[i];
                if (sprite != null && sprite.name.IndexOf(hint, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return sprite;
                }
            }
        }

        return null;
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

    private struct MovementUndoState
    {
        public bool active;
        public BattleTestUnit unit;
        public Vector2Int cell;
        public int hp;
        public int inner;
        public int specialCooldownLeft;
        public bool moved;
        public bool acted;
        public bool defeated;
        public bool guarded;
        public bool poisoned;
        public int poisonTurnsLeft;
        public bool chilled;
        public int chilledTurnsLeft;
        public bool marked;
        public bool mainAction;
        public bool bonusAction;
        public bool reaction;
        public int movementLeft;
        public bool hasFacingDirection;
        public Vector2 facingDirection;

        public static MovementUndoState Capture(BattleTestUnit unit)
        {
            if (unit == null)
            {
                return default;
            }

            return new MovementUndoState
            {
                active = true,
                unit = unit,
                cell = unit.cell,
                hp = unit.hp,
                inner = unit.inner,
                specialCooldownLeft = unit.specialCooldownLeft,
                moved = unit.moved,
                acted = unit.acted,
                defeated = unit.defeated,
                guarded = unit.guarded,
                poisoned = unit.poisoned,
                poisonTurnsLeft = unit.poisonTurnsLeft,
                chilled = unit.chilled,
                chilledTurnsLeft = unit.chilledTurnsLeft,
                marked = unit.marked,
                mainAction = unit.actions.mainAction,
                bonusAction = unit.actions.bonusAction,
                reaction = unit.actions.reaction,
                movementLeft = unit.actions.movementLeft,
                hasFacingDirection = unit.view != null,
                facingDirection = unit.view == null ? Vector2.down : unit.view.CurrentFacingDirection()
            };
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

public enum BattleTestMapVariant
{
    BaekduSnowGate,
    BaekduMountainSnowfield,
    BanditLair,
    WolfPass,
    TigerRavine,
    LeopardCliff,
    SeorakPassRescue
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
    public bool blocksProjectiles;
    public bool isChokePoint;
    public bool objective;
    public bool danger;
    public bool occupyAllowed = true;
    public int deployZone;
    public HazardType hazardType;
    public EdgeType northEdge;
    public EdgeType eastEdge;
    public EdgeType southEdge;
    public EdgeType westEdge;
    public string laneId;
    public string tacticalNote;
    public List<string> tags = new List<string>();
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

    public bool HasTag(string tag)
    {
        if (string.IsNullOrEmpty(tag) || tags == null)
        {
            return false;
        }

        for (int i = 0; i < tags.Count; i++)
        {
            if (string.Equals(tags[i], tag, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}

public sealed class BattleTestUnitView : MonoBehaviour
{
    private static readonly Color WorldHpBackColor = new Color(0.020f, 0.022f, 0.026f, 0.74f);
    private static readonly Color WorldHpAllyColor = new Color(0.760f, 0.260f, 0.225f, 0.90f);
    private static readonly Color WorldHpEnemyColor = new Color(0.885f, 0.350f, 0.300f, 0.92f);
    private static readonly Color WorldHpDangerColor = new Color(0.960f, 0.675f, 0.255f, 0.95f);
    private static readonly Color SelectedRingAllyColor = new Color(0.280f, 0.760f, 1f, 0.88f);
    private static readonly Color SelectedRingEnemyColor = new Color(1f, 0.330f, 0.270f, 0.88f);
    private static readonly Color SelectedHpHealthyColor = new Color(0.270f, 0.940f, 0.520f, 0.98f);
    private static readonly Color SelectedHpWoundedColor = new Color(0.980f, 0.300f, 0.210f, 0.98f);
    private const float WorldHpBackScaleX = 0.86f;
    private const float WorldHpFillScaleX = 0.82f;
    private const float SelectedHpBackScaleX = 1.10f;
    private const float SelectedHpFillScaleX = 1.04f;
    private const float WorldHpSpriteHalfWidth = 0.31f;
    private const float StatusAnchorFallbackY = 0.62f;
    private const float StatusAnchorHeadGap = 0.055f;
    private const float StatusLabelGap = 0.082f;

    private TextMesh label;
    private MeshRenderer labelRenderer;
    private Transform turnMarkerRoot;
    private SpriteRenderer turnGroundRing;
    private SpriteRenderer hpBarBack;
    private SpriteRenderer hpBarFill;
    private SpriteRenderer selectedHpBarBack;
    private SpriteRenderer selectedHpBarFill;
    private CharacterVisualController visualController;
    private bool isSelected;
    private Vector3 turnMarkerBasePosition;
    private static Sprite turnGroundRingSprite;
    private static Sprite hpBarSprite;

    public BattleTestUnit Unit { get; private set; }

    public void Bind(BattleTestUnit unit, CharacterVisualController controller)
    {
        Unit = unit;
        visualController = controller;
        if (visualController != null)
        {
            visualController.visual = Unit == null || Unit.definition == null ? null : Unit.definition.visual;
            visualController.ApplyVisual();
            visualController.SetSelected(false);
        }

        CreateLabel();
        CreateTurnMarker();
        Refresh(false);
    }

    private void LateUpdate()
    {
        UpdateStatusAttachmentPosition();
        UpdateAttachmentSorting();
        if (turnMarkerRoot == null || !turnMarkerRoot.gameObject.activeSelf)
        {
            return;
        }

        // Selection ring pulse: keep the marker attached to the character's feet.
        float wave = Mathf.Sin(Time.time * 4.6f);
        float pulse = Mathf.Abs(wave);
        turnMarkerRoot.localPosition = turnMarkerBasePosition;

        if (turnGroundRing != null && turnGroundRing.gameObject.activeSelf)
        {
            Color color = turnGroundRing.color;
            color.a = 0.54f + pulse * 0.22f;
            turnGroundRing.color = color;
            float scale = 1f + pulse * 0.035f;
            turnGroundRing.transform.localScale = new Vector3(scale, scale, 1f);
        }
    }

    public void Refresh(bool selected)
    {
        isSelected = selected;
        if (visualController != null)
        {
            visualController.SetActed(Unit != null && Unit.acted && !Unit.defeated);
            visualController.SetSelected(selected);
        }

        if (label != null && Unit != null)
        {
            label.text = Unit.definition.displayName;
            label.color = Unit.definition.faction == Faction.Ally
                              ? new Color(0.84f, 0.93f, 1f, Unit.defeated ? 0.40f : 0.96f)
                              : new Color(1f, 0.74f, 0.70f, Unit.defeated ? 0.40f : 0.96f);
        }

        RefreshHpBar();
        RefreshTurnMarker(selected);
    }

    private void RefreshHpBar()
    {
        if (hpBarFill == null || Unit == null)
        {
            return;
        }

        bool visible = !Unit.defeated;
        bool selectedVisible = visible && isSelected;
        hpBarBack.gameObject.SetActive(visible && !selectedVisible);
        hpBarFill.gameObject.SetActive(visible && !selectedVisible);
        if (selectedHpBarBack != null)
        {
            selectedHpBarBack.gameObject.SetActive(selectedVisible);
        }

        if (selectedHpBarFill != null)
        {
            selectedHpBarFill.gameObject.SetActive(selectedVisible);
        }

        if (!visible)
        {
            return;
        }

        float ratio = Mathf.Clamp01(Unit.hp / (float)Mathf.Max(1, Unit.definition.maxHp));
        Color hpColor = Unit.definition.faction == Faction.Enemy ? WorldHpEnemyColor : WorldHpAllyColor;
        if (ratio <= 0.25f)
        {
            hpColor = Color.Lerp(hpColor, WorldHpDangerColor, 0.42f);
        }

        hpColor.a = Mathf.Lerp(0.58f, hpColor.a, 1f - ratio) * (Unit == null || Unit.defeated ? 0.45f : 1f);
        hpBarBack.color = WorldHpBackColor;
        hpBarFill.color = hpColor;
        ApplyHpFill(hpBarFill, ratio, WorldHpFillScaleX);

        if (selectedHpBarBack != null && selectedHpBarFill != null)
        {
            selectedHpBarBack.color = new Color(0.010f, 0.014f, 0.014f, 0.82f);
            selectedHpBarFill.color = SelectedHpColor(ratio);
            ApplyHpFill(selectedHpBarFill, ratio, SelectedHpFillScaleX);
        }

        if (visualController != null)
        {
            visualController.PlayLowHp(ratio <= 0.25f);
        }
    }

    private static void ApplyHpFill(SpriteRenderer fillRenderer, float ratio, float fullScaleX)
    {
        if (fillRenderer == null)
        {
            return;
        }

        Transform fill = fillRenderer.transform;
        Vector3 scale = fill.localScale;
        scale.x = Mathf.Max(0.001f, fullScaleX * ratio);
        fill.localScale = scale;
        fill.localPosition = new Vector3(-WorldHpSpriteHalfWidth * fullScaleX * (1f - ratio),
                                         fill.localPosition.y, fill.localPosition.z);
    }

    private static Color SelectedHpColor(float ratio)
    {
        Color color = Color.Lerp(SelectedHpWoundedColor, SelectedHpHealthyColor, Mathf.Clamp01(ratio));
        if (ratio <= 0.25f)
        {
            color = Color.Lerp(color, WorldHpDangerColor, 0.24f);
        }

        color.a = 0.98f;
        return color;
    }

    public void FaceToward(Vector3 worldPosition)
    {
        if (visualController != null)
        {
            visualController.FaceToward(worldPosition);
        }
    }

    public void FaceDirection(Vector2 direction)
    {
        if (visualController != null)
        {
            visualController.FaceDirection(direction);
        }
    }

    public Vector2 CurrentFacingDirection()
    {
        return visualController == null ? Vector2.down : visualController.CurrentFacingDirection();
    }

    public CombatActionTimeline CreateTimeline(bool special)
    {
        return visualController != null ? visualController.CreateTimeline(special) : new CombatActionTimeline(null, special);
    }

    public float WalkSecondsPerTile()
    {
        return visualController != null ? visualController.WalkSecondsPerTile() : 0.44f;
    }

    public float MoveSettleTime()
    {
        return visualController != null ? visualController.MoveSettleTime() : 0.10f;
    }
    public void PlayIdle()
    {
        if (visualController != null)
        {
            visualController.PlayIdle();
        }
    }

    public void PlayMove()
    {
        if (visualController != null)
        {
            visualController.PlayMove();
        }
    }

    public void PlayTurnStart()
    {
        if (visualController != null)
        {
            visualController.PlayTurnStart();
        }
    }

    public void PlayClickReaction()
    {
        if (visualController != null)
        {
            visualController.PlayClickReaction();
        }
    }

    public void PlayLowHp(bool value)
    {
        if (visualController != null)
        {
            visualController.PlayLowHp(value);
        }
    }

    public void SetMoveStridePhase(float phase)
    {
        if (visualController != null)
        {
            visualController.SetMoveStridePhase(phase);
        }
    }

    public void PlayAttack()
    {
        if (visualController != null)
        {
            visualController.PlayAttack();
        }
    }

    public void PlaySkill()
    {
        if (visualController != null)
        {
            visualController.PlaySkill();
        }
    }

    public void PlayHit()
    {
        if (visualController != null)
        {
            visualController.PlayHit();
        }
    }

    public void PlayGuard()
    {
        if (visualController != null)
        {
            visualController.PlayGuard();
        }
    }

    public void PlayWait()
    {
        if (visualController != null)
        {
            visualController.PlayWait();
        }
    }

    public void PlayVictory()
    {
        if (visualController != null)
        {
            visualController.PlayVictory();
        }
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

        GameObject ringObject = new GameObject("Current Turn Ground Ring");
        ringObject.transform.SetParent(transform, false);
        ringObject.transform.localPosition = new Vector3(0f, 0.055f, 0.02f);
        turnGroundRing = ringObject.AddComponent<SpriteRenderer>();
        turnGroundRing.sprite = GetTurnGroundRingSprite();
        turnGroundRing.color = SelectedRingAllyColor;
        turnGroundRing.sortingLayerName = "Default";
        turnGroundRing.gameObject.SetActive(false);

        selectedHpBarBack = CreateBarSprite("Selected HP Bar Back", new Vector3(0f, StatusAnchorFallbackY, -0.055f),
                                            new Color(0.010f, 0.014f, 0.014f, 0.82f));
        selectedHpBarBack.transform.localScale = new Vector3(SelectedHpBackScaleX, 0.92f, 1f);
        selectedHpBarFill = CreateBarSprite("Selected HP Bar Fill", new Vector3(0f, StatusAnchorFallbackY, -0.065f),
                                            SelectedHpHealthyColor);
        selectedHpBarFill.transform.localScale = new Vector3(SelectedHpFillScaleX, 0.66f, 1f);
        selectedHpBarBack.gameObject.SetActive(false);
        selectedHpBarFill.gameObject.SetActive(false);

        turnMarkerRoot.gameObject.SetActive(false);
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

        turnMarkerBasePosition = Vector3.zero;
        turnMarkerRoot.localPosition = turnMarkerBasePosition;

        bool enemy = Unit.definition.faction == Faction.Enemy;
        Color accent = enemy ? SelectedRingEnemyColor : SelectedRingAllyColor;
        if (turnGroundRing != null)
        {
            Color ring = accent;
            ring.a = 0.70f;
            turnGroundRing.color = ring;
            turnGroundRing.transform.localScale = Vector3.one;
        }
    }

    private void UpdateAttachmentSorting()
    {
        if (visualController == null)
        {
            return;
        }

        int body = visualController.CurrentBodySortingOrder;
        if (turnGroundRing != null)
        {
            turnGroundRing.sortingOrder = body - 3;
        }

        if (hpBarBack != null)
        {
            hpBarBack.sortingOrder = body + 7;
        }

        if (hpBarFill != null)
        {
            hpBarFill.sortingOrder = body + 8;
        }

        if (selectedHpBarBack != null)
        {
            selectedHpBarBack.sortingOrder = body + 7;
        }

        if (selectedHpBarFill != null)
        {
            selectedHpBarFill.sortingOrder = body + 8;
        }

        if (labelRenderer != null)
        {
            labelRenderer.sortingOrder = body + 9;
        }
    }

    private void UpdateStatusAttachmentPosition()
    {
        float hpY = StatusAnchorY();
        SetLocalY(hpBarBack, hpY);
        SetLocalY(hpBarFill, hpY);
        SetLocalY(selectedHpBarBack, hpY);
        SetLocalY(selectedHpBarFill, hpY);
        if (label != null)
        {
            Transform labelTransform = label.transform;
            Vector3 local = labelTransform.localPosition;
            labelTransform.localPosition = new Vector3(local.x, hpY - StatusLabelGap, local.z);
        }
    }

    private float StatusAnchorY()
    {
        if (visualController == null || visualController.bodyRenderer == null || visualController.bodyRenderer.sprite == null)
        {
            return StatusAnchorFallbackY;
        }

        Bounds bounds = visualController.bodyRenderer.bounds;
        float localTop = transform.InverseTransformPoint(new Vector3(bounds.center.x, bounds.max.y, transform.position.z)).y;
        return Mathf.Clamp(localTop + StatusAnchorHeadGap, 0.42f, 1.34f);
    }

    private static void SetLocalY(SpriteRenderer renderer, float y)
    {
        if (renderer == null)
        {
            return;
        }

        Transform t = renderer.transform;
        Vector3 local = t.localPosition;
        t.localPosition = new Vector3(local.x, y, local.z);
    }

    private void CreateLabel()
    {
        GameObject labelObject = new GameObject("Unit Label");
        labelObject.transform.SetParent(transform, false);
        labelObject.transform.localPosition = new Vector3(0f, StatusAnchorFallbackY - StatusLabelGap, -0.04f);

        label = labelObject.AddComponent<TextMesh>();
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.fontSize = 34;
        label.characterSize = 0.0135f;
        ApplyWorldTextFont(label);

        labelRenderer = labelObject.GetComponent<MeshRenderer>();
        labelRenderer.sortingLayerName = "Default";

        hpBarBack = CreateBarSprite("HP Bar Back", new Vector3(0f, StatusAnchorFallbackY, -0.03f), WorldHpBackColor);
        hpBarBack.transform.localScale = new Vector3(WorldHpBackScaleX, 0.82f, 1f);
        hpBarFill = CreateBarSprite("HP Bar Fill", new Vector3(0f, StatusAnchorFallbackY, -0.04f),
                                    Unit != null && Unit.definition.faction == Faction.Enemy
                                        ? WorldHpEnemyColor
                                        : WorldHpAllyColor);
        hpBarFill.transform.localScale = new Vector3(WorldHpFillScaleX, 0.56f, 1f);
    }

    private SpriteRenderer CreateBarSprite(string name, Vector3 localPosition, Color color)
    {
        GameObject barObject = new GameObject(name);
        barObject.transform.SetParent(transform, false);
        barObject.transform.localPosition = localPosition;

        SpriteRenderer renderer = barObject.AddComponent<SpriteRenderer>();
        renderer.sprite = GetHpBarSprite();
        renderer.color = color;
        renderer.sortingLayerName = "Default";
        return renderer;
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

    private static Sprite GetTurnGroundRingSprite()
    {
        if (turnGroundRingSprite != null)
        {
            return turnGroundRingSprite;
        }

        // Soft ellipse ring around the character's feet, closer to SRPG unit selection cursors.
        const int textureWidth = 256;
        const int textureHeight = 144;
        Texture2D texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        texture.name = "BattleTestTurnGroundRing";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < textureHeight; y++)
        {
            for (int x = 0; x < textureWidth; x++)
            {
                float nx = (((x + 0.5f) / textureWidth) * 2f) - 1f;
                float ny = (((y + 0.5f) / textureHeight) * 2f) - 1f;
                float d = Mathf.Sqrt((nx * nx) + (ny * ny));
                float outer = Mathf.Clamp01(1f - Mathf.Abs(d - 0.84f) * 18f);
                float inner = Mathf.Clamp01(1f - Mathf.Abs(d - 0.62f) * 26f) * 0.34f;
                float glow = Mathf.Clamp01(1f - d) * 0.12f;
                float alpha = Mathf.Clamp01(outer + inner + glow);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        turnGroundRingSprite = Sprite.Create(texture, new Rect(0f, 0f, textureWidth, textureHeight),
                                             new Vector2(0.5f, 0.5f), 205f);
        turnGroundRingSprite.name = "BattleTestTurnGroundRing";
        return turnGroundRingSprite;
    }

    private static Sprite GetHpBarSprite()
    {
        if (hpBarSprite != null)
        {
            return hpBarSprite;
        }

        const int textureWidth = 62;
        const int textureHeight = 7;
        Texture2D texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        texture.name = "BattleTestHpBar";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Point;

        for (int y = 0; y < textureHeight; y++)
        {
            for (int x = 0; x < textureWidth; x++)
            {
                bool corner = (x == 0 || x == textureWidth - 1) && (y == 0 || y == textureHeight - 1);
                texture.SetPixel(x, y, corner ? Color.clear : Color.white);
            }
        }

        texture.Apply();
        hpBarSprite = Sprite.Create(texture, new Rect(0f, 0f, textureWidth, textureHeight),
                                    new Vector2(0.5f, 0.5f), 100f);
        hpBarSprite.name = "BattleTestHpBar";
        return hpBarSprite;
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
    public int poisonTurnsLeft;
    public bool chilled;
    public int chilledTurnsLeft;
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
        actions.movementLeft = 0;
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
