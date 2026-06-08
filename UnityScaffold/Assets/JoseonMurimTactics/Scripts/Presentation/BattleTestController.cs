using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JoseonMurimTactics
{
    [DisallowMultipleComponent]
    public sealed class BattleTestController : MonoBehaviour
    {
        public int width = 12;
        public int height = 8;
        public float tileWidth = 1.16f;
        public float tileHeight = 0.62f;
        public BattleTestUnitDefinition[] unitDefinitions = new BattleTestUnitDefinition[0];
        public bool useCanvasHud = true;
        public bool showDebugOnGui;

        private readonly List<BattleTestUnit> units = new List<BattleTestUnit>();
        private readonly List<string> battleLog = new List<string>();
        private readonly System.Random random = new System.Random(20260608);
        private BattleTestTile[,] tiles;
        private Sprite diamondSprite;
        private GUIStyle panelStyle;
        private GUIStyle labelStyle;
        private GUIStyle titleStyle;
        private GUIStyle smallStyle;
        private GUIStyle logStyle;
        private GUIStyle phaseStyle;
        private GUIStyle commandStyle;
        private GUIStyle commandActiveStyle;
        private GUIStyle cardStyle;
        private GUIStyle cardActiveStyle;
        private GUIStyle warningStyle;
        private GUIStyle tinyStyle;
        private BattleTestUnit activeUnit;
        private BattleTestUnit hoveredUnit;
        private BattleTestTile hoveredTile;
        private int round = 1;
        private bool busy;
        private bool aiQueued;
        private bool battleOver;
        private bool showThreatRange;
        private BattleCommandMode commandMode = BattleCommandMode.Move;
        private PhaseTurnController phaseTurnController;
        private UnitSelectionController unitSelectionController;
        private BreakResolver breakResolver;
        private CounterattackService counterattackService;
        private BattleForecastService battleForecastService;
        private ThreatRangeService threatRangeService;
        private ObjectiveManager objectiveManager;
        private EnemyTacticsAI enemyTacticsAI;
        private FollowUpResolver followUpResolver;
        private BattleForecastPanel battleForecastPanel;
        private BattleForecast currentForecast;
        private readonly Stack<BattleSnapshot> rewindStack = new Stack<BattleSnapshot>();
        private readonly List<BattleTerrainObject> terrainObjects = new List<BattleTerrainObject>();
        private BattleHUDController hud;
        private int rewindUsesRemaining = 3;
        private bool showLog;
        private string hudNotice;
        private float hudNoticeUntil;

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

            if (phaseTurnController != null && phaseTurnController.Phase == BattlePhase.EnemyPhase)
            {
                if (!busy && !aiQueued)
                {
                    aiQueued = true;
                    StartCoroutine(RunEnemyPhase());
                }

                return;
            }

            if (busy || activeUnit == null)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                EndTurn();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                SetCommandMode(BattleCommandMode.Move);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                SetCommandMode(BattleCommandMode.Attack);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                SetCommandMode(BattleCommandMode.Skill);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                GuardActiveUnit();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                SetCommandMode(BattleCommandMode.Terrain);
            }
            else if (Input.GetKeyDown(KeyCode.T))
            {
                showThreatRange = !showThreatRange;
                RefreshHighlights();
            }
            else if (Input.GetKeyDown(KeyCode.Z))
            {
                RewindLastAction();
            }
            else if (Input.GetKeyDown(KeyCode.L))
            {
                showLog = !showLog;
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
            if (!useCanvasHud)
            {
                return;
            }

            EnsureBattleHud();
            if (hud != null)
            {
                hud.Refresh(CreateHudSnapshot());
            }
        }

        private void OnGUI()
        {
            if (useCanvasHud && !showDebugOnGui)
            {
                return;
            }

            EnsureGuiStyles();

            DrawPhaseBanner();
            DrawActivePanel();
            DrawTurnQueuePanel();
            DrawCommandMenu();
            DrawInspectPanel();
            DrawForecastPanel();
            DrawLogPanel();
            DrawRosterPanel();
            DrawLegendPanel();

            if (!battleOver)
            {
                return;
            }

            GUI.Box(new Rect((Screen.width * 0.5f) - 180f, 24f, 360f, 76f), GUIContent.none, panelStyle);
            GUI.Label(new Rect((Screen.width * 0.5f) - 146f, 42f, 310f, 28f), "전투 종료", titleStyle);
        }

        private void DrawActivePanel()
        {
            GUI.Box(new Rect(18f, 18f, 380f, 190f), GUIContent.none, panelStyle);
            string activeName = activeUnit == null ? "None" : activeUnit.definition.displayName;
            string hp = activeUnit == null ? string.Empty : $"{activeUnit.hp}/{activeUnit.definition.maxHp}";

            GUI.Label(new Rect(34f, 30f, 344f, 26f), "Scenario Objective", titleStyle);
            GUI.Label(new Rect(34f, 58f, 344f, 20f), "Amnok Shrine Reclamation v0.6", labelStyle);
            GUI.Label(new Rect(34f, 84f, 344f, 20f), "Main: Defeat the Central Inspector", smallStyle);
            GUI.Label(new Rect(34f, 106f, 344f, 20f), $"Bonus: win by round 8, preserve altar, use terrain x2", smallStyle);
            GUI.Label(new Rect(34f, 128f, 344f, 20f), "Defeat: Park or Baek down, round 12 over", warningStyle);

            if (activeUnit != null)
            {
                GUI.Label(new Rect(34f, 154f, 344f, 20f), $"Selected: {activeName}   HP {hp}   Inner {activeUnit.inner}/{activeUnit.definition.maxInner}", labelStyle);
                GUI.Label(new Rect(34f, 174f, 344f, 18f), $"{activeUnit.definition.style}  |  {UnitStatusText(activeUnit)}", tinyStyle);
            }
        }

        private void DrawPhaseBanner()
        {
            BattlePhase phase = phaseTurnController == null ? BattlePhase.PlayerPhase : phaseTurnController.Phase;
            string phaseText = phase == BattlePhase.EnemyPhase ? "ENEMY PHASE" : "PLAYER PHASE";
            if (phase == BattlePhase.Victory || phase == BattlePhase.Defeat)
            {
                phaseText = phase.ToString().ToUpperInvariant();
            }

            Rect banner = new Rect((Screen.width * 0.5f) - 300f, 18f, 600f, 68f);
            GUI.Box(banner, GUIContent.none, phaseStyle);
            GUI.Label(new Rect(banner.x + 20f, banner.y + 10f, banner.width - 40f, 28f), $"{phaseText}  -  Round {phaseTurnController?.Round ?? round}", titleStyle);
            GUI.Label(new Rect(banner.x + 20f, banner.y + 40f, banner.width - 40f, 20f), InstructionText(), smallStyle);
        }

        private void DrawCommandMenu()
        {
            Rect panel = new Rect(Screen.width - 232f, 206f, 214f, 370f);
            GUI.Box(panel, GUIContent.none, panelStyle);
            GUI.Label(new Rect(panel.x + 16f, panel.y + 14f, panel.width - 32f, 24f), "Commands", titleStyle);

            bool playerTurn = CanPlayerControlActive();
            float y = panel.y + 48f;
            if (DrawCommandButton(new Rect(panel.x + 16f, y, 182f, 36f), "1  Move", playerTurn && !activeUnit.moved, commandMode == BattleCommandMode.Move))
            {
                SetCommandMode(BattleCommandMode.Move);
            }

            y += 42f;
            if (DrawCommandButton(new Rect(panel.x + 16f, y, 182f, 36f), "2  Attack", playerTurn && !activeUnit.acted, commandMode == BattleCommandMode.Attack))
            {
                SetCommandMode(BattleCommandMode.Attack);
            }

            y += 42f;
            if (DrawCommandButton(new Rect(panel.x + 16f, y, 182f, 36f), "3  Skill", playerTurn && CanUseSpecial(activeUnit), commandMode == BattleCommandMode.Skill))
            {
                SetCommandMode(BattleCommandMode.Skill);
            }

            y += 42f;
            if (DrawCommandButton(new Rect(panel.x + 16f, y, 182f, 36f), "4  Guard", playerTurn && !activeUnit.acted, false))
            {
                GuardActiveUnit();
            }

            y += 42f;
            if (DrawCommandButton(new Rect(panel.x + 16f, y, 86f, 36f), "5 Terrain", playerTurn && !activeUnit.acted, commandMode == BattleCommandMode.Terrain))
            {
                SetCommandMode(BattleCommandMode.Terrain);
            }

            if (DrawCommandButton(new Rect(panel.x + 112f, y, 86f, 36f), "Wait", playerTurn, false))
            {
                EndTurn();
            }

            y += 48f;
            if (DrawCommandButton(new Rect(panel.x + 16f, y, 62f, 32f), showThreatRange ? "Danger" : "Threat", true, showThreatRange))
            {
                showThreatRange = !showThreatRange;
                RefreshHighlights();
            }

            if (DrawCommandButton(new Rect(panel.x + 82f, y, 52f, 32f), $"Z {rewindUsesRemaining}", rewindUsesRemaining > 0 && rewindStack.Count > 0 && !busy, false))
            {
                RewindLastAction();
            }

            if (DrawCommandButton(new Rect(panel.x + 138f, y, 60f, 32f), showLog ? "Log On" : "Log", true, showLog))
            {
                showLog = !showLog;
            }

            if (DrawCommandButton(new Rect(panel.x + 16f, y + 38f, 182f, 28f), "Reset Battle", true, false))
            {
                BuildBattle();
            }

            if (activeUnit != null)
            {
                string cooldown = activeUnit.specialCooldownLeft > 0 ? $"CD {activeUnit.specialCooldownLeft}" : "Ready";
                GUI.Label(new Rect(panel.x + 16f, panel.y + 318f, 182f, 18f), $"Skill: {activeUnit.definition.specialName}", tinyStyle);
                GUI.Label(new Rect(panel.x + 16f, panel.y + 336f, 182f, 18f), $"Range {activeUnit.definition.specialRange}  {cooldown}", tinyStyle);
            }
        }

        private void DrawTurnQueuePanel()
        {
            float x = Screen.width - 292f;
            GUI.Box(new Rect(x, 18f, 274f, 176f), GUIContent.none, panelStyle);
            string header = phaseTurnController != null && phaseTurnController.Phase == BattlePhase.EnemyPhase ? "Enemy Acting" : "Ready Allies";
            GUI.Label(new Rect(x + 16f, 30f, 238f, 26f), header, titleStyle);

            List<BattleTestUnit> queue = GetTurnQueuePreview(6);
            for (int i = 0; i < queue.Count; i++)
            {
                BattleTestUnit unit = queue[i];
                string marker = unit == activeUnit ? "NOW" : "READY";
                string state = unit.defeated ? "Down" : UnitStatusText(unit);
                string line = $"{marker}  {unit.definition.displayName}  {state}";
                GUI.Label(new Rect(x + 16f, 60f + (i * 20f), 238f, 18f), line, unit == activeUnit ? labelStyle : tinyStyle);
            }
        }

        private void DrawLogPanel()
        {
            if (!showLog)
            {
                Rect collapsed = new Rect(Screen.width - 170f, Screen.height - 196f, 152f, 34f);
                GUI.Box(collapsed, GUIContent.none, panelStyle);
                GUI.Label(new Rect(collapsed.x + 12f, collapsed.y + 8f, collapsed.width - 24f, 18f), "Log collapsed (L)", tinyStyle);
                return;
            }

            float x = Screen.width - 348f;
            float y = Screen.height - 380f;
            GUI.Box(new Rect(x, y, 330f, 222f), GUIContent.none, panelStyle);
            GUI.Label(new Rect(x + 16f, y + 12f, 296f, 24f), "Combat Log", titleStyle);

            int start = Mathf.Max(0, battleLog.Count - 8);
            for (int i = start; i < battleLog.Count; i++)
            {
                GUI.Label(new Rect(x + 16f, y + 42f + ((i - start) * 22f), 296f, 22f), battleLog[i], logStyle);
            }
        }

        private void DrawInspectPanel()
        {
            GUI.Box(new Rect(18f, 218f, 380f, 140f), GUIContent.none, panelStyle);
            GUI.Label(new Rect(34f, 230f, 344f, 24f), "Hover Info", titleStyle);

            if (hoveredUnit != null)
            {
                GUI.Label(new Rect(34f, 258f, 344f, 20f), $"{hoveredUnit.definition.displayName} ({hoveredUnit.definition.faction})", labelStyle);
                GUI.Label(new Rect(34f, 282f, 344f, 20f), $"HP {hoveredUnit.hp}/{hoveredUnit.definition.maxHp}   DEF {DefenseValue(hoveredUnit, TileAt(hoveredUnit.cell))}   Style {hoveredUnit.definition.style}", smallStyle);
                GUI.Label(new Rect(34f, 306f, 344f, 20f), $"Status: {UnitStatusText(hoveredUnit)}", smallStyle);
                GUI.Label(new Rect(34f, 330f, 344f, 20f), $"Skill: {hoveredUnit.definition.specialName}", tinyStyle);
                return;
            }

            if (hoveredTile != null)
            {
                GUI.Label(new Rect(34f, 258f, 344f, 20f), $"{hoveredTile.terrain}  ({hoveredTile.cell.x},{hoveredTile.cell.y})", labelStyle);
                GUI.Label(new Rect(34f, 282f, 344f, 20f), $"Move Cost {hoveredTile.moveCost}   Cover +{hoveredTile.coverBonus}", smallStyle);
                GUI.Label(new Rect(34f, 306f, 344f, 20f), $"Elevation {hoveredTile.elevation}   Hazard {hoveredTile.hazard}", smallStyle);
                return;
            }

            GUI.Label(new Rect(34f, 258f, 344f, 22f), "Hover a unit or tile to see tactical details.", smallStyle);
        }

        private void DrawForecastPanel()
        {
            if (battleForecastPanel == null || activeUnit == null || !currentForecast.valid)
            {
                return;
            }

            Rect rect = new Rect((Screen.width * 0.5f) - 390f, Screen.height - 322f, 780f, 160f);
            battleForecastPanel.Draw(rect, currentForecast, panelStyle, titleStyle, smallStyle);
        }

        private void DrawRosterPanel()
        {
            float y = Screen.height - 142f;
            GUI.Box(new Rect(18f, y, Screen.width - 36f, 124f), GUIContent.none, panelStyle);
            GUI.Label(new Rect(34f, y + 12f, 420f, 24f), "Ally Units - click a ready card to choose action order", titleStyle);

            List<BattleTestUnit> allies = FactionUnits(Faction.Ally);
            float cardWidth = Mathf.Min(244f, (Screen.width - 84f) / Mathf.Max(1, allies.Count));
            for (int i = 0; i < allies.Count; i++)
            {
                BattleTestUnit unit = allies[i];
                float x = 34f + (i * cardWidth);
                DrawRosterCard(new Rect(x, y + 40f, cardWidth - 10f, 74f), unit);
            }
        }

        private void DrawLegendPanel()
        {
            Rect rect = new Rect((Screen.width * 0.5f) - 300f, 92f, 600f, 34f);
            GUI.Box(rect, GUIContent.none, panelStyle);
            DrawLegendItem(rect.x + 18f, rect.y + 10f, new Color(0.25f, 0.58f, 1f, 0.9f), "Move");
            DrawLegendItem(rect.x + 120f, rect.y + 10f, new Color(1f, 0.18f, 0.16f, 0.9f), "Attack");
            DrawLegendItem(rect.x + 224f, rect.y + 10f, new Color(0.72f, 0.28f, 1f, 0.9f), "Skill");
            DrawLegendItem(rect.x + 326f, rect.y + 10f, new Color(1f, 0.76f, 0.18f, 0.9f), "Active");
            DrawLegendItem(rect.x + 430f, rect.y + 10f, new Color(1f, 0.76f, 0.18f, 0.9f), "Terrain");
            DrawLegendItem(rect.x + 518f, rect.y + 10f, new Color(0.72f, 0.16f, 0.85f, 0.55f), "Threat");
        }

        private void DrawLegendItem(float x, float y, Color color, string text)
        {
            FillRect(new Rect(x, y + 2f, 14f, 14f), color);
            GUI.Label(new Rect(x + 20f, y - 1f, 96f, 18f), text, tinyStyle);
        }

        private bool DrawCommandButton(Rect rect, string label, bool enabled, bool active)
        {
            bool oldEnabled = GUI.enabled;
            GUI.enabled = enabled;
            bool clicked = GUI.Button(rect, label, active ? commandActiveStyle : commandStyle);
            GUI.enabled = oldEnabled;
            return enabled && clicked;
        }

        private void DrawRosterCard(Rect rect, BattleTestUnit unit)
        {
            bool selected = unit == activeUnit;
            GUI.Box(rect, GUIContent.none, selected ? cardActiveStyle : cardStyle);
            GUI.Label(new Rect(rect.x + 10f, rect.y + 8f, rect.width - 20f, 18f), unit.definition.displayName, selected ? labelStyle : smallStyle);

            float hpRatio = unit.definition.maxHp <= 0 ? 0f : Mathf.Clamp01((float)unit.hp / unit.definition.maxHp);
            FillRect(new Rect(rect.x + 10f, rect.y + 32f, rect.width - 20f, 8f), new Color(0.18f, 0.12f, 0.10f, 1f));
            FillRect(new Rect(rect.x + 10f, rect.y + 32f, (rect.width - 20f) * hpRatio, 8f), unit.defeated ? new Color(0.4f, 0.4f, 0.4f, 1f) : new Color(0.78f, 0.18f, 0.13f, 1f));

            string status = unit.defeated ? "Down" : unit.turnEnded ? "Done" : UnitStatusText(unit);
            BattleTestTile tile = TileAt(unit.cell);
            string terrain = tile != null ? tile.terrain.ToString() : "None";
            GUI.Label(new Rect(rect.x + 10f, rect.y + 44f, rect.width - 86f, 18f), $"HP {unit.hp}/{unit.definition.maxHp}  Inner {unit.inner}/{unit.definition.maxInner}", tinyStyle);
            GUI.Label(new Rect(rect.x + 10f, rect.y + 60f, rect.width - 86f, 18f), $"{status}  Agi {unit.definition.agility}  {terrain}  {(unit.supported ? "Support" : string.Empty)}", tinyStyle);

            bool canSelect = phaseTurnController != null && phaseTurnController.Phase == BattlePhase.PlayerPhase && !unit.defeated && !unit.turnEnded && !busy && !battleOver;
            bool oldEnabled = GUI.enabled;
            GUI.enabled = canSelect;
            if (GUI.Button(new Rect(rect.x + rect.width - 72f, rect.y + 46f, 62f, 22f), selected ? "Active" : "Select"))
            {
                SelectAlly(unit);
            }

            GUI.enabled = oldEnabled;
        }

        private bool CanPlayerControlActive()
        {
            return activeUnit != null
                && activeUnit.definition.faction == Faction.Ally
                && !busy
                && !battleOver
                && phaseTurnController != null
                && phaseTurnController.Phase == BattlePhase.PlayerPhase;
        }

        private string InstructionText()
        {
            if (battleOver)
            {
                return "R 키로 전투를 다시 시작합니다.";
            }

            if (phaseTurnController != null && phaseTurnController.Phase == BattlePhase.EnemyPhase)
            {
                return "적군이 행동 중입니다. 위험 범위와 반격 결과를 확인하세요.";
            }

            if (activeUnit == null)
            {
                return "행동 가능한 아군을 지도나 하단 로스터에서 선택하세요.";
            }

            if (commandMode == BattleCommandMode.Move && !activeUnit.moved)
            {
                return "파란 칸은 이동 가능 범위입니다. 이동 후 공격, 무공, 방어, 대기를 선택하세요.";
            }

            if (commandMode == BattleCommandMode.Attack)
            {
                return "붉은 칸은 공격 범위입니다. 적에게 마우스를 올리면 전투 예측이 표시됩니다.";
            }

            if (commandMode == BattleCommandMode.Skill)
            {
                return "보라색 칸은 무공 범위입니다. 예측을 확인한 뒤 대상을 클릭하세요.";
            }

            if (commandMode == BattleCommandMode.Terrain)
            {
                return "금색 칸은 지형 활용 대상입니다. 향로, 등불, 종, 다리 밧줄을 전술에 이용하세요.";
            }

            return "공격, 무공, 지형 활용, 방어, 대기 중 하나로 행동을 마무리하세요.";
        }

        private List<BattleTestUnit> FactionUnits(Faction faction)
        {
            List<BattleTestUnit> result = new List<BattleTestUnit>();
            for (int i = 0; i < units.Count; i++)
            {
                if (units[i].definition.faction == faction)
                {
                    result.Add(units[i]);
                }
            }

            return result;
        }

        private void BuildBattle()
        {
            StopAllCoroutines();
            ClearGeneratedObjects();

            units.Clear();
            battleLog.Clear();
            activeUnit = null;
            round = 1;
            busy = false;
            aiQueued = false;
            battleOver = false;
            commandMode = BattleCommandMode.Move;
            showThreatRange = false;
            showLog = false;
            rewindUsesRemaining = 3;
            rewindStack.Clear();
            terrainObjects.Clear();
            hoveredTile = null;
            hoveredUnit = null;
            currentForecast = default;
            phaseTurnController = new PhaseTurnController();
            unitSelectionController = new UnitSelectionController();
            breakResolver = new BreakResolver();
            counterattackService = new CounterattackService();
            followUpResolver = new FollowUpResolver();
            battleForecastService = new BattleForecastService(breakResolver, counterattackService, followUpResolver);
            threatRangeService = new ThreatRangeService();
            objectiveManager = new ObjectiveManager();
            enemyTacticsAI = new EnemyTacticsAI(breakResolver, battleForecastService);
            battleForecastPanel = new BattleForecastPanel();

            diamondSprite = diamondSprite == null ? CreateDiamondSprite() : diamondSprite;
            CreateTerrain();
            CreateTerrainObjects();
            SpawnUnits();
            CenterCamera();
            EnsureBattleHud();

            AddLog("압록강 폐사당 탈환전 v0.7");
            AddLog("아군 페이즈: 원하는 순서로 유닛을 행동시키세요.");
            phaseTurnController.StartBattle(units);
            BeginPlayerPhase();
        }

        private void EnsureBattleHud()
        {
            if (!useCanvasHud)
            {
                return;
            }

            if (hud != null)
            {
                return;
            }

            GameObject hudObject = new GameObject("Battle HUD");
            hudObject.transform.SetParent(transform, false);
            hud = hudObject.AddComponent<BattleHUDController>();
            hud.Initialize(this);
        }

        private BattleHudSnapshot CreateHudSnapshot()
        {
            BattleHudSnapshot snapshot = new BattleHudSnapshot
            {
                phase = phaseTurnController != null ? phaseTurnController.Phase : BattlePhase.PlayerPhase,
                round = phaseTurnController != null ? phaseTurnController.Round : round,
                instruction = InstructionText(),
                activeUnit = activeUnit,
                hoveredUnit = hoveredUnit,
                hoveredTile = hoveredTile,
                hoverDefense = hoveredUnit != null ? DefenseValue(hoveredUnit, TileAt(hoveredUnit.cell)) : 0,
                hoverStatus = hoveredUnit != null ? UnitStatusText(hoveredUnit) : string.Empty,
                forecast = currentForecast,
                hasForecast = activeUnit != null && (currentForecast.valid || !string.IsNullOrEmpty(currentForecast.actorName) || !string.IsNullOrEmpty(currentForecast.reason)),
                showLog = showLog,
                showThreatRange = showThreatRange,
                commandMode = commandMode,
                rewindUsesRemaining = rewindUsesRemaining,
                noticeText = Time.time < hudNoticeUntil ? hudNotice : string.Empty
            };

            bool playerTurn = CanPlayerControlActive();
            snapshot.canMove = playerTurn && activeUnit != null && !activeUnit.moved;
            snapshot.canAttack = playerTurn && activeUnit != null && !activeUnit.acted;
            snapshot.canSkill = playerTurn && CanUseSpecial(activeUnit);
            snapshot.canGuard = playerTurn && activeUnit != null && !activeUnit.acted;
            snapshot.canTerrain = playerTurn && activeUnit != null && !activeUnit.acted;
            snapshot.canWait = playerTurn;
            snapshot.canRewind = rewindUsesRemaining > 0 && rewindStack.Count > 0 && !busy;

            List<BattleTestUnit> allies = FactionUnits(Faction.Ally);
            for (int i = 0; i < allies.Count; i++)
            {
                BattleTestUnit unit = allies[i];
                snapshot.allies.Add(unit);
                snapshot.unitStatuses[unit] = UnitStatusText(unit);
                if (phaseTurnController != null
                    && phaseTurnController.Phase == BattlePhase.PlayerPhase
                    && !unit.defeated
                    && !unit.turnEnded
                    && !busy
                    && !battleOver)
                {
                    snapshot.selectableUnits.Add(unit);
                }
            }

            snapshot.logs.AddRange(battleLog);
            return snapshot;
        }

        public void HudSetCommand(BattleCommandMode mode)
        {
            SetCommandMode(mode);
        }

        public void HudGuard()
        {
            GuardActiveUnit();
        }

        public void HudWait()
        {
            EndTurn();
        }

        public void HudToggleThreat()
        {
            showThreatRange = !showThreatRange;
            RefreshHighlights();
        }

        public void HudToggleLog()
        {
            showLog = !showLog;
        }

        public void HudRewind()
        {
            RewindLastAction();
        }

        public void HudResetBattle()
        {
            BuildBattle();
        }

        public void HudSelectUnit(BattleTestUnit unit)
        {
            SelectAlly(unit);
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
        }

        private void CreateTerrain()
        {
            tiles = new BattleTestTile[width, height];
            Transform terrainRoot = new GameObject("Terrain").transform;
            terrainRoot.SetParent(transform, false);

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
                    renderer.sprite = diamondSprite;
                    renderer.color = profile.color;
                    renderer.sortingOrder = (x + y) * 2;

                    PolygonCollider2D collider = tileObject.AddComponent<PolygonCollider2D>();
                    collider.points = new[]
                    {
                        new Vector2(0f, 0.25f),
                        new Vector2(0.5f, 0f),
                        new Vector2(0f, -0.25f),
                        new Vector2(-0.5f, 0f)
                    };

                    GameObject highlight = new GameObject("Highlight");
                    highlight.transform.SetParent(tileObject.transform, false);
                    highlight.transform.localPosition = new Vector3(0f, 0f, -0.02f);
                    SpriteRenderer highlightRenderer = highlight.AddComponent<SpriteRenderer>();
                    highlightRenderer.sprite = diamondSprite;
                    highlightRenderer.color = Color.clear;
                    highlightRenderer.sortingOrder = renderer.sortingOrder + 1;

                    BattleTestTile tile = tileObject.AddComponent<BattleTestTile>();
                    tile.cell = cell;
                    tile.terrain = profile.terrain;
                    tile.hazard = profile.hazard;
                    tile.elevation = profile.elevation;
                    tile.walkable = profile.walkable;
                    tile.moveCost = profile.moveCost;
                    tile.coverBonus = profile.coverBonus;
                    tile.baseRenderer = renderer;
                    tile.highlightRenderer = highlightRenderer;
                    tiles[x, y] = tile;
                }
            }
        }

        private void CreateTerrainObjects()
        {
            Transform objectRoot = new GameObject("Terrain Objects").transform;
            objectRoot.SetParent(transform, false);

            AddTerrainObject(objectRoot, "제단 향로", TerrainObjectKind.IncenseBrazier, new Vector2Int(6, 4), 12);
            AddTerrainObject(objectRoot, "기름 등불", TerrainObjectKind.OilLantern, new Vector2Int(9, 3), 10);
            AddTerrainObject(objectRoot, "사당 종", TerrainObjectKind.ShrineBell, new Vector2Int(7, 6), 14);
            AddTerrainObject(objectRoot, "다리 밧줄", TerrainObjectKind.BridgeRope, new Vector2Int(5, 5), 8);
        }

        private void AddTerrainObject(Transform root, string displayName, TerrainObjectKind kind, Vector2Int cell, int hp)
        {
            if (!IsInside(cell))
            {
                return;
            }

            GameObject objectGameObject = new GameObject(displayName);
            objectGameObject.transform.SetParent(root, false);
            objectGameObject.transform.position = UnitWorldPosition(cell) + new Vector3(0f, 0.18f, -0.03f);

            SpriteRenderer renderer = objectGameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = diamondSprite;
            renderer.color = TerrainObjectColor(kind);
            renderer.sortingOrder = 4200;
            objectGameObject.transform.localScale = new Vector3(tileWidth * 0.34f, tileWidth * 0.34f, 1f);

            TextMesh label = CreateTerrainObjectLabel(objectGameObject.transform, displayName);
            BattleTerrainObject terrainObject = objectGameObject.AddComponent<BattleTerrainObject>();
            terrainObject.displayName = displayName;
            terrainObject.kind = kind;
            terrainObject.cell = cell;
            terrainObject.hp = hp;
            terrainObject.maxHp = hp;
            terrainObject.interactRange = 1;
            terrainObject.renderer = renderer;
            terrainObject.label = label;
            terrainObjects.Add(terrainObject);
        }

        private TextMesh CreateTerrainObjectLabel(Transform parent, string displayName)
        {
            GameObject labelObject = new GameObject("Terrain Object Label");
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = new Vector3(0f, -0.42f, -0.04f);

            TextMesh mesh = labelObject.AddComponent<TextMesh>();
            mesh.text = displayName;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.fontSize = 34;
            mesh.characterSize = 0.014f;
            mesh.color = new Color(0.96f, 0.88f, 0.58f, 1f);

            MeshRenderer meshRenderer = labelObject.GetComponent<MeshRenderer>();
            meshRenderer.sortingLayerName = "Default";
            meshRenderer.sortingOrder = 5001;
            return mesh;
        }

        private Color TerrainObjectColor(TerrainObjectKind kind)
        {
            switch (kind)
            {
                case TerrainObjectKind.IncenseBrazier:
                    return new Color(0.80f, 0.74f, 0.52f, 0.95f);
                case TerrainObjectKind.OilLantern:
                    return new Color(1f, 0.52f, 0.16f, 0.95f);
                case TerrainObjectKind.BridgeRope:
                    return new Color(0.54f, 0.34f, 0.18f, 0.95f);
                case TerrainObjectKind.ShrineBell:
                    return new Color(0.96f, 0.76f, 0.22f, 0.95f);
                default:
                    return Color.white;
            }
        }

        private Color HazardColor(TerrainType terrain, HazardType hazard)
        {
            switch (hazard)
            {
                case HazardType.Smoke:
                    return new Color(0.58f, 0.62f, 0.58f, 1f);
                case HazardType.Fire:
                    return new Color(0.86f, 0.30f, 0.12f, 1f);
                case HazardType.Ice:
                    return new Color(0.64f, 0.82f, 0.92f, 1f);
                case HazardType.Slippery:
                    return new Color(0.34f, 0.50f, 0.56f, 1f);
                default:
                    return TerrainColor(terrain);
            }
        }

        private Color TerrainColor(TerrainType terrain)
        {
            switch (terrain)
            {
                case TerrainType.Bamboo:
                    return new Color(0.40f, 0.58f, 0.34f, 1f);
                case TerrainType.Water:
                    return new Color(0.40f, 0.62f, 0.72f, 1f);
                case TerrainType.Bridge:
                    return new Color(0.62f, 0.44f, 0.24f, 1f);
                case TerrainType.Roof:
                    return new Color(0.68f, 0.34f, 0.23f, 1f);
                case TerrainType.Cliff:
                    return new Color(0.46f, 0.40f, 0.32f, 1f);
                case TerrainType.Wall:
                    return new Color(0.36f, 0.34f, 0.32f, 1f);
                default:
                    return new Color(0.66f, 0.60f, 0.46f, 1f);
            }
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

                BattleTestUnitView view = unitObject.AddComponent<BattleTestUnitView>();
                BattleTestUnit unit = new BattleTestUnit(definition, view);
                unit.initiative = definition.initiative;
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

            round = phaseTurnController.Round;
            aiQueued = false;
            for (int i = 0; i < units.Count; i++)
            {
                BattleTestUnit unit = units[i];
                if (!unit.defeated && unit.definition.faction == Faction.Ally)
                {
                    ApplyStartOfTurn(unit);
                }
            }

            activeUnit = unitSelectionController.SelectFirstReadyAlly(units);
            commandMode = BattleCommandMode.Move;
            currentForecast = default;
            AddLog($"아군 페이즈 {phaseTurnController.Round}: 행동 순서를 선택하세요.");
            RefreshHighlights();
            RefreshUnits();
        }

        private void EndTurn()
        {
            if (battleOver || activeUnit == null || phaseTurnController.Phase != BattlePhase.PlayerPhase)
            {
                return;
            }

            CompleteActiveUnit("waits.");
        }

        private void CompleteActiveUnit(string reason)
        {
            if (activeUnit == null)
            {
                return;
            }

            AddLog($"{activeUnit.definition.displayName} {reason}");
            phaseTurnController.MarkUnitFinished(activeUnit);
            currentForecast = default;

            if (phaseTurnController.AllFactionUnitsFinished(units, Faction.Ally))
            {
                activeUnit = null;
                phaseTurnController.BeginEnemyPhase(units);
                AddLog("적군 페이즈.");
                RefreshHighlights();
                RefreshUnits();
                return;
            }

            activeUnit = unitSelectionController.SelectFirstReadyAlly(units);
            commandMode = BattleCommandMode.Move;
            RefreshHighlights();
            RefreshUnits();
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

            if (clickedUnit != null && clickedUnit.definition.faction == Faction.Ally)
            {
                SelectAlly(clickedUnit);
                return;
            }

            if (clickedUnit != null && clickedUnit.definition.faction != activeUnit.definition.faction && commandMode == BattleCommandMode.Move)
            {
                SetCommandMode(BattleCommandMode.Attack);
                UpdateForecast(clickedUnit, false);
                AddLog("Target preview. Click the enemy again to attack.");
                return;
            }

            if (commandMode == BattleCommandMode.Attack)
            {
                if (clickedUnit != null && clickedUnit.definition.faction != activeUnit.definition.faction)
                {
                    TryAttack(activeUnit, clickedUnit, true);
                }
                else
                {
                    AddLog("No enemy target.");
                }

                return;
            }

            if (commandMode == BattleCommandMode.Skill)
            {
                if (clickedUnit != null)
                {
                    if (TrySpecial(activeUnit, clickedUnit))
                    {
                        CompleteActiveUnit("used skill.");
                    }
                }
                else
                {
                    AddLog("No skill target.");
                }

                return;
            }

            if (commandMode == BattleCommandMode.Terrain)
            {
                BattleTerrainObject terrainObject = clickedTile != null ? TerrainObjectAt(clickedTile.cell) : null;
                if (terrainObject != null && TryUseTerrainObject(activeUnit, terrainObject))
                {
                    CompleteActiveUnit("used terrain.");
                }
                else
                {
                    AddLog("No usable terrain object in range.");
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
                UpdateForecast(clickedUnit, false);
            }
        }

        private void SelectAlly(BattleTestUnit unit)
        {
            if (unit == null || unit.definition.faction != Faction.Ally)
            {
                return;
            }

            if (unitSelectionController.TrySelect(unit))
            {
                activeUnit = unit;
                commandMode = activeUnit.moved && !activeUnit.acted ? BattleCommandMode.Attack : BattleCommandMode.Move;
                currentForecast = default;
                AddLog($"Selected {activeUnit.definition.displayName}.");
                RefreshHighlights();
                RefreshUnits();
            }
            else
            {
                AddLog("That ally already acted.");
            }
        }

        private void TryMove(BattleTestUnit unit, BattleTestTile destination)
        {
            if (unit.moved)
            {
                AddLog("Move already spent.");
                return;
            }

            if (!destination.walkable || UnitAt(destination.cell) != null)
            {
                AddLog("Blocked tile.");
                return;
            }

            Dictionary<Vector2Int, int> reachable = GetReachableCells(unit);
            if (!reachable.ContainsKey(destination.cell))
            {
                AddLog("Out of movement range.");
                return;
            }

            SaveRewindSnapshot("before move");
            unit.cell = destination.cell;
            unit.moved = true;
            ApplyTileEntryEffect(unit, destination);
            StartCoroutine(AnimateMove(unit, UnitWorldPosition(destination.cell)));
            currentForecast = default;
            if (!unit.acted)
            {
                commandMode = BattleCommandMode.Attack;
            }

            AddLog($"{unit.definition.displayName} moved. Choose a target, skill, guard, or wait.");
            RefreshHighlights();
        }

        private bool TryAttack(BattleTestUnit attacker, BattleTestUnit target, bool endAfterAttack)
        {
            if (attacker.acted)
            {
                AddLog("Action already spent.");
                return false;
            }

            if (target.defeated)
            {
                return false;
            }

            int distance = GridDistance(attacker.cell, target.cell);
            if (distance > attacker.definition.attackRange || distance < attacker.definition.attackMinRange)
            {
                AddLog("Target out of range.");
                return false;
            }

            SaveRewindSnapshot("before attack");
            bool hit = ResolveAttack(attacker, target, false);
            attacker.acted = true;
            if (hit)
            {
                ResolveCounterIfPossible(target, attacker);
                ResolveFollowUpIfPossible(attacker, target, false);
            }

            RefreshUnits();

            if (CheckBattleEnd())
            {
                return true;
            }

            if (endAfterAttack)
            {
                CompleteActiveUnit("attacked.");
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
            int d20 = random.Next(1, 21);
            int heightBonus = from != null && to != null && from.elevation > to.elevation ? 2 : 0;
            if (to != null && (to.hazard == HazardType.Ice || to.hazard == HazardType.Slippery) && attacker.definition.style == SkillStyle.Ice)
            {
                heightBonus += 2;
            }

            int styleBonus = breakResolver.AttackModifier(attacker.definition.style, target.definition.style);
            int supportBonus = attacker.supported ? 1 : 0;
            int attackBonus = attacker.definition.attackBonus + styleBonus + supportBonus + (special ? attacker.definition.specialAttackBonus : 0);
            int attackTotal = d20 + attackBonus + heightBonus;
            int defense = DefenseValue(target, to);
            bool critical = d20 == 20;
            bool hit = critical || (d20 != 1 && attackTotal >= defense);
            ShowHudNotice(FormatDiceNotice(d20, attackBonus + heightBonus, attackTotal, defense, hit, critical));

            if (!hit)
            {
                AddLog($"{attacker.definition.displayName} missed {target.definition.displayName}. d20 {d20}+{attackBonus}+{heightBonus} vs {defense}");
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

            damage += heightBonus;
            if (target.guarded)
            {
                damage = Mathf.Max(1, Mathf.CeilToInt(damage * 0.55f));
            }

            target.hp = Mathf.Max(0, target.hp - damage);
            AddLog($"{attacker.definition.displayName} hit {target.definition.displayName} for {damage}. d20 {d20}+{attackBonus}+{heightBonus} vs {defense}");

            int breakGain = breakResolver.BreakGain(attacker.definition.style, target.definition.style, true);
            breakResolver.ApplyBreak(target, breakGain);
            if (target.broken)
            {
                AddLog($"{target.definition.displayName} Broken: counter sealed.");
            }
            else if (breakGain > 0)
            {
                AddLog($"{target.definition.displayName} break +{breakGain} ({target.breakGauge}/100).");
            }

            if (target.hp == 0)
            {
                target.defeated = true;
                target.view.SetDefeated(true);
                AddLog($"{target.definition.displayName} defeated.");
            }

            return true;
        }

        private void ResolveCounterIfPossible(BattleTestUnit defender, BattleTestUnit attacker)
        {
            int distance = GridDistance(defender.cell, attacker.cell);
            if (!counterattackService.CanCounter(defender, attacker, distance))
            {
                return;
            }

            defender.counterSpent = true;
            defender.inner = Mathf.Max(0, defender.inner - defender.definition.counterInnerCost);
            AddLog($"{defender.definition.displayName} counters.");
            ResolveAttack(defender, attacker, false);
        }

        private void ResolveFollowUpIfPossible(BattleTestUnit attacker, BattleTestUnit target, bool special)
        {
            if (followUpResolver == null || !followUpResolver.CanFollowUp(attacker, target, special))
            {
                return;
            }

            if (attacker.defeated || target.defeated)
            {
                return;
            }

            attacker.inner = Mathf.Max(0, attacker.inner - 1);
            AddLog($"{attacker.definition.displayName} follows up.");
            ResolveAttack(attacker, target, false);
        }

        private bool TrySpecial(BattleTestUnit actor, BattleTestUnit target)
        {
            if (!CanUseSpecial(actor))
            {
                AddLog("Skill unavailable.");
                return false;
            }

            if (!IsValidSpecialTarget(actor, target))
            {
                AddLog("Invalid skill target.");
                return false;
            }

            int distance = GridDistance(actor.cell, target.cell);
            if (distance > actor.definition.specialRange || distance < actor.definition.specialMinRange)
            {
                AddLog("Skill target out of range.");
                return false;
            }

            SaveRewindSnapshot("before skill");
            actor.inner -= actor.definition.specialCost;
            actor.specialCooldownLeft = actor.definition.specialCooldown;
            actor.acted = true;

            switch (actor.definition.specialEffect)
            {
                case BattleSpecialEffect.Heal:
                    int healed = Mathf.Min(target.definition.maxHp - target.hp, actor.definition.specialPower + random.Next(4, 9));
                    target.hp += Mathf.Max(0, healed);
                    target.poisoned = false;
                    target.chilled = false;
                    AddLog($"{actor.definition.displayName} used {actor.definition.specialName}. {target.definition.displayName} healed {healed}.");
                    break;
                case BattleSpecialEffect.Poison:
                    if (ResolveAttack(actor, target, true))
                    {
                        ResolveCounterIfPossible(target, actor);
                    }

                    if (!target.defeated)
                    {
                        target.poisoned = true;
                        AddLog($"{target.definition.displayName} poisoned.");
                    }
                    break;
                case BattleSpecialEffect.Freeze:
                    if (ResolveAttack(actor, target, true))
                    {
                        ResolveCounterIfPossible(target, actor);
                    }

                    if (!target.defeated)
                    {
                        target.chilled = true;
                        AddLog($"{target.definition.displayName} slowed.");
                    }
                    break;
                case BattleSpecialEffect.Mark:
                    target.marked = true;
                    AddLog($"{actor.definition.displayName} marked {target.definition.displayName}.");
                    break;
                case BattleSpecialEffect.BreakGuard:
                    target.guarded = false;
                    target.marked = true;
                    if (ResolveAttack(actor, target, true))
                    {
                        ResolveCounterIfPossible(target, actor);
                    }
                    break;
                default:
                    if (ResolveAttack(actor, target, true))
                    {
                        ResolveCounterIfPossible(target, actor);
                    }
                    break;
            }

            RefreshHighlights();
            RefreshUnits();
            CheckBattleEnd();
            return true;
        }

        private void GuardActiveUnit()
        {
            if (activeUnit == null || activeUnit.acted || activeUnit.definition.faction != Faction.Ally)
            {
                return;
            }

            activeUnit.guarded = true;
            activeUnit.acted = true;
            CompleteActiveUnit("guards.");
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

            for (int i = 0; i < units.Count; i++)
            {
                BattleTestUnit enemy = units[i];
                if (enemy.defeated || enemy.definition.faction != Faction.Enemy)
                {
                    continue;
                }

                activeUnit = enemy;
                ApplyStartOfTurn(enemy);
                if (enemy.defeated)
                {
                    continue;
                }

                AddLog($"{enemy.definition.displayName} acts.");
                RefreshHighlights();
                RefreshUnits();
                yield return new WaitForSeconds(0.2f);

                BattleTestUnit target = enemyTacticsAI.ChooseTarget(enemy, units, TileAt);
                if (target == null)
                {
                    phaseTurnController.MarkUnitFinished(enemy);
                    continue;
                }

                int desiredRange = CanUseSpecial(enemy) ? enemy.definition.specialRange : enemy.definition.attackRange;
                if (GridDistance(enemy.cell, target.cell) > desiredRange && !enemy.moved)
                {
                    BattleTestTile best = FindBestMoveToward(enemy, target.cell);
                    if (best != null)
                    {
                        enemy.cell = best.cell;
                        enemy.moved = true;
                        yield return AnimateMove(enemy, UnitWorldPosition(best.cell));
                        yield return new WaitForSeconds(0.15f);
                    }
                }

                if (CanUseSpecial(enemy) && IsValidSpecialTarget(enemy, target) && GridDistance(enemy.cell, target.cell) <= enemy.definition.specialRange)
                {
                    TrySpecial(enemy, target);
                }
                else
                {
                    TryAttack(enemy, target, false);
                }

                phaseTurnController.MarkUnitFinished(enemy);
                if (CheckBattleEnd())
                {
                    busy = false;
                    yield break;
                }

                yield return new WaitForSeconds(0.25f);
            }

            phaseTurnController.FinishEnemyPhase(units);
            busy = false;
            activeUnit = null;
            BeginPlayerPhase();
        }

        private BattleTestTile FindBestMoveToward(BattleTestUnit unit, Vector2Int targetCell)
        {
            Dictionary<Vector2Int, int> reachable = GetReachableCells(unit);
            BattleTestTile best = null;
            int bestDistance = GridDistance(unit.cell, targetCell);
            int bestCost = int.MaxValue;

            foreach (KeyValuePair<Vector2Int, int> pair in reachable)
            {
                if (pair.Key == unit.cell || UnitAt(pair.Key) != null)
                {
                    continue;
                }

                int distance = GridDistance(pair.Key, targetCell);
                if (distance < bestDistance || (distance == bestDistance && pair.Value < bestCost))
                {
                    bestDistance = distance;
                    bestCost = pair.Value;
                    best = TileAt(pair.Key);
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
            unit.counterSpent = false;
            unit.inner = Mathf.Min(unit.definition.maxInner, unit.inner + 1);
            unit.specialCooldownLeft = Mathf.Max(0, unit.specialCooldownLeft - 1);
            if (unit.broken)
            {
                unit.broken = false;
                unit.breakGauge = Mathf.Max(0, unit.breakGauge - 55);
                AddLog($"{unit.definition.displayName} recovers from Broken.");
            }

            if (unit.poisoned)
            {
                unit.hp = Mathf.Max(0, unit.hp - 3);
                AddLog($"{unit.definition.displayName} takes 3 poison damage.");
                if (unit.hp == 0)
                {
                    unit.defeated = true;
                    unit.view.SetDefeated(true);
                }
            }

            BattleTestTile tile = TileAt(unit.cell);
            if (tile != null && tile.hazard == HazardType.Fire)
            {
                unit.hp = Mathf.Max(0, unit.hp - 4);
                AddLog($"{unit.definition.displayName} takes 4 fire damage.");
                if (unit.hp == 0)
                {
                    unit.defeated = true;
                    unit.view.SetDefeated(true);
                }
            }
        }

        private void ApplyTileEntryEffect(BattleTestUnit unit, BattleTestTile tile)
        {
            if (tile == null)
            {
                return;
            }

            if (tile.hazard == HazardType.Ice || tile.hazard == HazardType.Slippery)
            {
                unit.chilled = true;
                AddLog($"{unit.definition.displayName} loses footing.");
            }
            else if (tile.hazard == HazardType.Fire)
            {
                unit.hp = Mathf.Max(0, unit.hp - 3);
                AddLog($"{unit.definition.displayName} enters fire for 3 damage.");
            }
            else if (tile.hazard == HazardType.Smoke)
            {
                unit.marked = false;
                AddLog($"{unit.definition.displayName} enters smoke.");
            }
        }

        private BattleTerrainObject TerrainObjectAt(Vector2Int cell)
        {
            for (int i = 0; i < terrainObjects.Count; i++)
            {
                BattleTerrainObject terrainObject = terrainObjects[i];
                if (terrainObject != null && !terrainObject.destroyed && terrainObject.cell == cell)
                {
                    return terrainObject;
                }
            }

            return null;
        }

        private bool TryUseTerrainObject(BattleTestUnit unit, BattleTerrainObject terrainObject)
        {
            if (unit == null || terrainObject == null || terrainObject.used || terrainObject.destroyed)
            {
                return false;
            }

            if (GridDistance(unit.cell, terrainObject.cell) > terrainObject.interactRange)
            {
                return false;
            }

            SaveRewindSnapshot("before terrain");
            terrainObject.used = true;
            unit.acted = true;

            switch (terrainObject.kind)
            {
                case TerrainObjectKind.IncenseBrazier:
                    ApplyAreaHazard(terrainObject.cell, 1, HazardType.Smoke);
                    AddLog($"{unit.definition.displayName} spreads incense smoke.");
                    break;
                case TerrainObjectKind.OilLantern:
                    ApplyAreaHazard(terrainObject.cell, 1, HazardType.Fire);
                    DamageUnitsInRadius(terrainObject.cell, 1, 5, Faction.Enemy);
                    terrainObject.destroyed = true;
                    AddLog($"{unit.definition.displayName} breaks an oil lantern.");
                    break;
                case TerrainObjectKind.ShrineBell:
                    BreakEnemiesInRadius(terrainObject.cell, 2, 18);
                    AddLog($"{unit.definition.displayName} rings the shrine bell.");
                    break;
                case TerrainObjectKind.BridgeRope:
                    ApplyAreaHazard(terrainObject.cell, 1, HazardType.Slippery);
                    terrainObject.destroyed = true;
                    AddLog($"{unit.definition.displayName} cuts the bridge rope.");
                    break;
            }

            RefreshTerrainObjects();
            RefreshHighlights();
            RefreshUnits();
            CheckBattleEnd();
            return true;
        }

        private void ApplyAreaHazard(Vector2Int center, int radius, HazardType hazard)
        {
            for (int y = center.y - radius; y <= center.y + radius; y++)
            {
                for (int x = center.x - radius; x <= center.x + radius; x++)
                {
                    Vector2Int cell = new Vector2Int(x, y);
                    if (GridDistance(center, cell) > radius)
                    {
                        continue;
                    }

                    BattleTestTile tile = TileAt(cell);
                    if (tile == null || !tile.walkable)
                    {
                        continue;
                    }

                    tile.hazard = hazard;
                    tile.SetBaseColor(HazardColor(tile.terrain, hazard));
                }
            }
        }

        private void DamageUnitsInRadius(Vector2Int center, int radius, int damage, Faction targetFaction)
        {
            for (int i = 0; i < units.Count; i++)
            {
                BattleTestUnit unit = units[i];
                if (unit.defeated || unit.definition.faction != targetFaction || GridDistance(center, unit.cell) > radius)
                {
                    continue;
                }

                unit.hp = Mathf.Max(0, unit.hp - damage);
                AddLog($"{unit.definition.displayName} takes {damage} terrain damage.");
                if (unit.hp == 0)
                {
                    unit.defeated = true;
                    unit.view.SetDefeated(true);
                }
            }
        }

        private void BreakEnemiesInRadius(Vector2Int center, int radius, int amount)
        {
            for (int i = 0; i < units.Count; i++)
            {
                BattleTestUnit unit = units[i];
                if (unit.defeated || unit.definition.faction != Faction.Enemy || GridDistance(center, unit.cell) > radius)
                {
                    continue;
                }

                breakResolver.ApplyBreak(unit, amount);
                unit.marked = true;
                AddLog($"{unit.definition.displayName} break +{amount}.");
            }
        }

        private void RefreshTerrainObjects()
        {
            for (int i = 0; i < terrainObjects.Count; i++)
            {
                BattleTerrainObject terrainObject = terrainObjects[i];
                if (terrainObject != null)
                {
                    terrainObject.Refresh();
                }
            }
        }

        private void SaveRewindSnapshot(string reason)
        {
            if (phaseTurnController == null
                || phaseTurnController.Phase != BattlePhase.PlayerPhase
                || activeUnit == null
                || activeUnit.definition.faction != Faction.Ally
                || battleOver)
            {
                return;
            }

            rewindStack.Push(BattleSnapshot.Capture(units, tiles, terrainObjects, activeUnit, commandMode, reason));
            while (rewindStack.Count > 6)
            {
                BattleSnapshot[] snapshots = rewindStack.ToArray();
                rewindStack.Clear();
                for (int i = snapshots.Length - 2; i >= 0; i--)
                {
                    rewindStack.Push(snapshots[i]);
                }
            }
        }

        private void RewindLastAction()
        {
            if (rewindUsesRemaining <= 0 || rewindStack.Count == 0 || busy)
            {
                AddLog("No rewind available.");
                return;
            }

            BattleSnapshot snapshot = rewindStack.Pop();
            snapshot.Restore(units, tiles, terrainObjects, HazardColor);
            activeUnit = FindUnitById(snapshot.activeUnitId);
            commandMode = snapshot.commandMode;
            currentForecast = default;
            rewindUsesRemaining--;
            AddLog($"Rewind used: {snapshot.reason}. Remaining {rewindUsesRemaining}.");
            RefreshTerrainObjects();
            RefreshHighlights();
            RefreshUnits();
        }

        private BattleTestUnit FindUnitById(string id)
        {
            for (int i = 0; i < units.Count; i++)
            {
                if (units[i].definition.id == id)
                {
                    return units[i];
                }
            }

            return null;
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
            return unit != null
                && !unit.acted
                && unit.inner >= unit.definition.specialCost
                && unit.specialCooldownLeft <= 0
                && unit.definition.specialEffect != BattleSpecialEffect.None;
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

        private int DefenseValue(BattleTestUnit unit, BattleTestTile tile)
        {
            int defense = unit.definition.defense;
            if (tile != null)
            {
                defense += tile.coverBonus;
                if (tile.hazard == HazardType.Smoke)
                {
                    defense += 2;
                }
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

        private string UnitStatusText(BattleTestUnit unit)
        {
            if (unit.defeated)
            {
                return "Down";
            }

            List<string> states = new List<string>();
            if (unit.guarded)
            {
                states.Add("Guard");
            }

            if (unit.broken)
            {
                states.Add("Broken");
            }

            if (unit.prone)
            {
                states.Add("Prone");
            }

            if (unit.disarmed)
            {
                states.Add("Disarmed");
            }

            if (unit.poisoned)
            {
                states.Add("Poison");
            }

            if (unit.chilled)
            {
                states.Add("Slow");
            }

            if (unit.marked)
            {
                states.Add("Marked");
            }

            if (unit.moved && unit.acted)
            {
                states.Add("Done");
            }
            else if (unit.moved)
            {
                states.Add("Moved");
            }
            else if (unit.acted)
            {
                states.Add("Action Done");
            }

            return states.Count == 0 ? "Ready" : string.Join(", ", states);
        }

        private string ActionText(bool spent)
        {
            return spent ? "Done" : "Ready";
        }

        private void SetCommandMode(BattleCommandMode mode)
        {
            if (activeUnit == null || activeUnit.definition.faction != Faction.Ally)
            {
                return;
            }

            if (mode == BattleCommandMode.Move && activeUnit.moved)
            {
                AddLog("Move already spent. Choose attack, skill, guard, or wait.");
                return;
            }

            if (mode == BattleCommandMode.Attack && activeUnit.acted)
            {
                AddLog("Action already spent. Wait to finish this unit.");
                return;
            }

            if (mode == BattleCommandMode.Skill && !CanUseSpecial(activeUnit))
            {
                AddLog("Skill is unavailable.");
                return;
            }

            if (mode == BattleCommandMode.Terrain && activeUnit.acted)
            {
                AddLog("Action already spent. Wait to finish this unit.");
                return;
            }

            commandMode = mode;
            if (hoveredUnit != null && hoveredUnit.definition.faction != activeUnit.definition.faction)
            {
                UpdateForecast(hoveredUnit, mode == BattleCommandMode.Skill);
            }
            else
            {
                currentForecast = default;
            }

            RefreshHighlights();
        }

        private List<BattleTestUnit> GetTurnQueuePreview(int count)
        {
            List<BattleTestUnit> queue = new List<BattleTestUnit>();
            Faction faction = phaseTurnController != null && phaseTurnController.Phase == BattlePhase.EnemyPhase ? Faction.Enemy : Faction.Ally;
            for (int i = 0; i < units.Count && queue.Count < count; i++)
            {
                BattleTestUnit unit = units[i];
                if (!unit.defeated && unit.definition.faction == faction && !unit.turnEnded)
                {
                    queue.Add(unit);
                }
            }

            return queue;
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

            if (hoveredUnit != null && activeUnit != null && hoveredUnit.definition.faction != activeUnit.definition.faction)
            {
                UpdateForecast(hoveredUnit, commandMode == BattleCommandMode.Skill);
                return;
            }

            currentForecast = default;
        }

        private void UpdateForecast(BattleTestUnit target, bool special)
        {
            if (battleForecastService == null || activeUnit == null || target == null)
            {
                currentForecast = default;
                return;
            }

            currentForecast = battleForecastService.Create(activeUnit, target, TileAt(activeUnit.cell), TileAt(target.cell), special);
        }

        private Dictionary<Vector2Int, int> GetReachableCells(BattleTestUnit unit)
        {
            Dictionary<Vector2Int, int> cost = new Dictionary<Vector2Int, int>();
            List<Vector2Int> frontier = new List<Vector2Int>();
            cost[unit.cell] = 0;
            frontier.Add(unit.cell);

            while (frontier.Count > 0)
            {
                int bestIndex = 0;
                int bestCost = cost[frontier[0]];
                for (int i = 1; i < frontier.Count; i++)
                {
                    int candidateCost = cost[frontier[i]];
                    if (candidateCost < bestCost)
                    {
                        bestCost = candidateCost;
                        bestIndex = i;
                    }
                }

                Vector2Int current = frontier[bestIndex];
                frontier.RemoveAt(bestIndex);

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

                    int nextCost = cost[current] + MoveCost(unit, TileAt(current), tile);
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

        private int MoveCost(BattleTestUnit unit, BattleTestTile from, BattleTestTile to)
        {
            int cost = Mathf.Max(1, to.moveCost);
            if (from != null && Mathf.Abs(to.elevation - from.elevation) > 0)
            {
                cost += Mathf.Abs(to.elevation - from.elevation);
            }

            if (to.terrain == TerrainType.Water && unit.definition.style != SkillStyle.Ice)
            {
                cost += 1;
            }

            if (to.terrain == TerrainType.Bamboo && unit.definition.style != SkillStyle.HiddenWeapon)
            {
                cost += 1;
            }

            if (to.hazard == HazardType.Slippery || to.hazard == HazardType.Fire)
            {
                cost += 1;
            }

            if (unit.chilled)
            {
                cost += 1;
            }

            if (IsEnemyThreatenedCell(unit.definition.faction, to.cell))
            {
                cost += 1;
            }

            return cost;
        }

        private bool IsEnemyThreatenedCell(Faction movingFaction, Vector2Int cell)
        {
            Faction enemyFaction = movingFaction == Faction.Ally ? Faction.Enemy : Faction.Ally;
            for (int i = 0; i < units.Count; i++)
            {
                BattleTestUnit unit = units[i];
                if (unit.defeated || unit.definition.faction != enemyFaction)
                {
                    continue;
                }

                int range = Mathf.Max(unit.definition.attackRange, unit.definition.specialRange);
                if (GridDistance(unit.cell, cell) <= range)
                {
                    return true;
                }
            }

            return false;
        }

        private IEnumerable<Vector2Int> Neighbors(Vector2Int cell)
        {
            yield return new Vector2Int(cell.x + 1, cell.y);
            yield return new Vector2Int(cell.x - 1, cell.y);
            yield return new Vector2Int(cell.x, cell.y + 1);
            yield return new Vector2Int(cell.x, cell.y - 1);
        }

        private void RefreshHighlights()
        {
            ClearHighlights();

            if (showThreatRange && threatRangeService != null)
            {
                HashSet<Vector2Int> threatened = threatRangeService.BuildThreatCells(units, Faction.Enemy, width, height);
                foreach (Vector2Int cell in threatened)
                {
                    BattleTestTile tile = TileAt(cell);
                    if (tile != null)
                    {
                        tile.SetHighlight(new Color(0.72f, 0.16f, 0.85f, 0.30f));
                    }
                }
            }

            if (activeUnit == null || activeUnit.defeated)
            {
                return;
            }

            BattleTestTile activeTile = TileAt(activeUnit.cell);
            if (activeTile != null)
            {
                activeTile.SetHighlight(new Color(1f, 0.76f, 0.18f, 0.62f));
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
                        tile.SetHighlight(new Color(0.25f, 0.58f, 1f, 0.38f));
                    }
                }
            }

            if (commandMode == BattleCommandMode.Attack && !activeUnit.acted)
            {
                foreach (BattleTestUnit target in units)
                {
                    if (target.defeated || target.definition.faction == activeUnit.definition.faction)
                    {
                        continue;
                    }

                    int distance = GridDistance(activeUnit.cell, target.cell);
                    if (distance <= activeUnit.definition.attackRange && distance >= activeUnit.definition.attackMinRange)
                    {
                        BattleTestTile tile = TileAt(target.cell);
                        if (tile != null)
                        {
                            tile.SetHighlight(new Color(1f, 0.18f, 0.16f, 0.52f));
                        }
                    }
                }
            }

            if (commandMode == BattleCommandMode.Skill && CanUseSpecial(activeUnit))
            {
                foreach (BattleTestUnit target in units)
                {
                    if (!IsValidSpecialTarget(activeUnit, target))
                    {
                        continue;
                    }

                    int distance = GridDistance(activeUnit.cell, target.cell);
                    if (distance <= activeUnit.definition.specialRange && distance >= activeUnit.definition.specialMinRange)
                    {
                        BattleTestTile tile = TileAt(target.cell);
                        if (tile != null)
                        {
                            Color color = activeUnit.definition.specialEffect == BattleSpecialEffect.Heal
                                ? new Color(0.18f, 1f, 0.62f, 0.48f)
                                : new Color(0.72f, 0.28f, 1f, 0.50f);
                            tile.SetHighlight(color);
                        }
                    }
                }
            }

            if (commandMode == BattleCommandMode.Terrain && !activeUnit.acted)
            {
                for (int i = 0; i < terrainObjects.Count; i++)
                {
                    BattleTerrainObject terrainObject = terrainObjects[i];
                    if (terrainObject == null || terrainObject.used || terrainObject.destroyed)
                    {
                        continue;
                    }

                    if (GridDistance(activeUnit.cell, terrainObject.cell) <= terrainObject.interactRange)
                    {
                        BattleTestTile tile = TileAt(terrainObject.cell);
                        if (tile != null)
                        {
                            tile.SetHighlight(new Color(1f, 0.76f, 0.18f, 0.62f));
                        }
                    }
                }
            }

            if (hoveredUnit != null && hoveredUnit.definition.faction != activeUnit.definition.faction)
            {
                BattleTestTile targetTile = TileAt(hoveredUnit.cell);
                if (targetTile != null)
                {
                    targetTile.SetHighlight(new Color(1f, 0.92f, 0.70f, 0.78f));
                }
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

        private void RefreshUnits()
        {
            UpdateSupportLinks();
            foreach (BattleTestUnit unit in units)
            {
                unit.view.Refresh(unit == activeUnit);
            }
        }

        private void UpdateSupportLinks()
        {
            for (int i = 0; i < units.Count; i++)
            {
                BattleTestUnit unit = units[i];
                unit.supported = false;
                if (unit.defeated)
                {
                    continue;
                }

                for (int j = 0; j < units.Count; j++)
                {
                    BattleTestUnit other = units[j];
                    if (unit == other || other.defeated || other.definition.faction != unit.definition.faction)
                    {
                        continue;
                    }

                    if (GridDistance(unit.cell, other.cell) <= 2)
                    {
                        unit.supported = true;
                        break;
                    }
                }
            }
        }

        private bool CheckBattleEnd()
        {
            if (objectiveManager == null)
            {
                return false;
            }

            BattleOutcome outcome = objectiveManager.Evaluate(units, phaseTurnController != null ? phaseTurnController.Round : round);
            if (outcome == BattleOutcome.Ongoing)
            {
                return false;
            }

            battleOver = true;
            phaseTurnController.SetOutcome(outcome);
            ClearHighlights();
            AddLog(outcome == BattleOutcome.Victory ? "승리: 중원 감찰사를 제압했습니다." : "패배: 전투 목표를 달성하지 못했습니다.");
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

        private TerrainProfile ResolveTerrain(int x, int y)
        {
            if ((x >= 8 && x <= 10 && y == 7) || (x == 10 && y == 8))
            {
                return new TerrainProfile(TerrainType.Wall, new Color(0.36f, 0.34f, 0.32f, 1f), 1, 0, 99, false);
            }

            if ((x == 6 && y == 4) || (x == 7 && y == 4))
            {
                return new TerrainProfile(TerrainType.Stone, new Color(0.58f, 0.62f, 0.58f, 1f), 0, 2, 1, true, HazardType.Smoke);
            }

            if (x == 9 && y == 3)
            {
                return new TerrainProfile(TerrainType.Stone, new Color(0.86f, 0.30f, 0.12f, 1f), 0, 0, 2, true, HazardType.Fire);
            }

            if (x == 11 && y == 5)
            {
                return new TerrainProfile(TerrainType.Water, new Color(0.64f, 0.82f, 0.92f, 1f), 0, 0, 2, true, HazardType.Ice);
            }

            if (y == 5 && x >= 2 && x <= 11)
            {
                bool bridge = x == 5 || x == 6;
                return bridge
                    ? new TerrainProfile(TerrainType.Bridge, new Color(0.62f, 0.44f, 0.24f, 1f), 0, 0, 1, true)
                    : new TerrainProfile(TerrainType.Water, new Color(0.40f, 0.62f, 0.72f, 1f), 0, 0, 3, true, HazardType.Slippery);
            }

            if (x <= 3 && y >= 7)
            {
                return new TerrainProfile(TerrainType.Bamboo, new Color(0.40f, 0.58f, 0.34f, 1f), 0, 1, 2, true);
            }

            if (x >= 12 && y <= 4)
            {
                return new TerrainProfile(TerrainType.Roof, new Color(0.68f, 0.34f, 0.23f, 1f), 1, 0, 1, true);
            }

            if (x >= 14 && y >= 8)
            {
                return new TerrainProfile(TerrainType.Cliff, new Color(0.46f, 0.40f, 0.32f, 1f), 1, 2, 2, true);
            }

            if (x >= 6 && x <= 9 && y >= 6 && y <= 8)
            {
                return new TerrainProfile(TerrainType.Stone, new Color(0.70f, 0.64f, 0.48f, 1f), 0, 1, 1, true);
            }

            return new TerrainProfile(TerrainType.Stone, new Color(0.66f, 0.60f, 0.46f, 1f), 0, 0, 1, true);
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
            camera.backgroundColor = new Color(0.74f, 0.80f, 0.76f, 1f);
        }

        private bool PointerOverHud(Vector3 screenPosition)
        {
            if (useCanvasHud)
            {
                return hud != null && hud.PointerOverHud(screenPosition);
            }

            float guiY = Screen.height - screenPosition.y;
            Vector2 point = new Vector2(screenPosition.x, guiY);
            Rect objectivePanel = new Rect(18f, 18f, 380f, 190f);
            Rect inspectPanel = new Rect(18f, 218f, 380f, 140f);
            Rect phaseBanner = new Rect((Screen.width * 0.5f) - 300f, 18f, 600f, 108f);
            Rect readyPanel = new Rect(Screen.width - 292f, 18f, 274f, 176f);
            Rect commandPanel = new Rect(Screen.width - 232f, 206f, 214f, 370f);
            Rect logPanel = new Rect(Screen.width - 348f, Screen.height - 380f, 330f, 222f);
            Rect forecastPanel = new Rect((Screen.width * 0.5f) - 390f, Screen.height - 322f, 780f, 160f);
            Rect bottomPanel = new Rect(18f, Screen.height - 142f, Screen.width - 36f, 124f);
            return objectivePanel.Contains(point)
                || inspectPanel.Contains(point)
                || phaseBanner.Contains(point)
                || readyPanel.Contains(point)
                || commandPanel.Contains(point)
                || logPanel.Contains(point)
                || forecastPanel.Contains(point)
                || bottomPanel.Contains(point);
        }

        private Sprite CreateDiamondSprite()
        {
            const int textureWidth = 96;
            const int textureHeight = 48;
            Texture2D texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
            texture.name = "BattleTestDiamond";
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Point;

            for (int y = 0; y < textureHeight; y++)
            {
                for (int x = 0; x < textureWidth; x++)
                {
                    float nx = Mathf.Abs(((x + 0.5f) / textureWidth * 2f) - 1f);
                    float ny = Mathf.Abs(((y + 0.5f) / textureHeight * 2f) - 1f);
                    float d = nx + ny;
                    if (d <= 1f)
                    {
                        float edge = d > 0.88f ? 0.74f : 1f;
                        texture.SetPixel(x, y, new Color(edge, edge, edge, 1f));
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

        private void EnsureGuiStyles()
        {
            if (panelStyle != null)
            {
                return;
            }

            panelStyle = new GUIStyle(GUI.skin.box);
            panelStyle.normal.background = MakeTexture(new Color(0.91f, 0.86f, 0.74f, 0.90f));
            panelStyle.border = new RectOffset(6, 6, 6, 6);

            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.normal.textColor = new Color(0.08f, 0.22f, 0.25f, 1f);
            labelStyle.fontSize = 16;
            labelStyle.fontStyle = FontStyle.Bold;

            titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.normal.textColor = new Color(0.06f, 0.25f, 0.30f, 1f);
            titleStyle.fontSize = 18;
            titleStyle.fontStyle = FontStyle.Bold;

            smallStyle = new GUIStyle(GUI.skin.label);
            smallStyle.normal.textColor = new Color(0.20f, 0.18f, 0.14f, 1f);
            smallStyle.fontSize = 13;
            smallStyle.wordWrap = true;

            logStyle = new GUIStyle(GUI.skin.label);
            logStyle.normal.textColor = new Color(0.18f, 0.16f, 0.12f, 1f);
            logStyle.fontSize = 13;
            logStyle.wordWrap = true;

            phaseStyle = new GUIStyle(GUI.skin.box);
            phaseStyle.normal.background = MakeTexture(new Color(0.78f, 0.88f, 0.84f, 0.92f));
            phaseStyle.border = new RectOffset(8, 8, 8, 8);

            commandStyle = new GUIStyle(GUI.skin.button);
            commandStyle.normal.textColor = new Color(0.10f, 0.24f, 0.28f, 1f);
            commandStyle.fontSize = 16;
            commandStyle.fontStyle = FontStyle.Bold;
            commandStyle.alignment = TextAnchor.MiddleLeft;
            commandStyle.padding = new RectOffset(16, 8, 0, 0);

            commandActiveStyle = new GUIStyle(commandStyle);
            commandActiveStyle.normal.textColor = new Color(0.07f, 0.16f, 0.18f, 1f);
            commandActiveStyle.normal.background = MakeTexture(new Color(0.86f, 0.72f, 0.34f, 0.95f));

            cardStyle = new GUIStyle(GUI.skin.box);
            cardStyle.normal.background = MakeTexture(new Color(0.88f, 0.84f, 0.70f, 0.92f));
            cardStyle.border = new RectOffset(6, 6, 6, 6);

            cardActiveStyle = new GUIStyle(cardStyle);
            cardActiveStyle.normal.background = MakeTexture(new Color(0.76f, 0.86f, 0.80f, 0.96f));

            warningStyle = new GUIStyle(GUI.skin.label);
            warningStyle.normal.textColor = new Color(0.58f, 0.14f, 0.08f, 1f);
            warningStyle.fontSize = 13;
            warningStyle.fontStyle = FontStyle.Bold;

            tinyStyle = new GUIStyle(GUI.skin.label);
            tinyStyle.normal.textColor = new Color(0.22f, 0.22f, 0.18f, 1f);
            tinyStyle.fontSize = 12;
            tinyStyle.wordWrap = true;
        }

        private Texture2D MakeTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private void FillRect(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private string YesNo(bool value)
        {
            return value ? "Yes" : "No";
        }

        private void AddLog(string message)
        {
            message = KoreanizeLog(message);
            battleLog.Add(message);
            if (battleLog.Count > 24)
            {
                battleLog.RemoveAt(0);
            }

            Debug.Log("[BattleTest] " + message);
        }

        private void ShowHudNotice(string message)
        {
            hudNotice = message;
            hudNoticeUntil = Time.time + 1.2f;
        }

        private string FormatDiceNotice(int d20, int modifier, int total, int defense, bool hit, bool critical)
        {
            if (d20 == 20)
            {
                return $"회심!  d20 {d20} + {modifier} = {total}\n목표 {defense} 이상 / 명중";
            }

            if (d20 == 1)
            {
                return $"허초!  d20 {d20}\n자연 1 / 빗나감";
            }

            return $"d20 {d20} + {modifier} = {total}\n목표 {defense} 이상 / {(hit ? "명중" : "빗나감")}";
        }

        private string KoreanizeLog(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return message;
            }

            return message
                .Replace("Player Phase", "아군 페이즈")
                .Replace("Enemy Phase", "적군 페이즈")
                .Replace("choose unit order", "행동 순서를 선택하세요")
                .Replace("choose allies in any order", "원하는 순서로 아군을 행동시키세요")
                .Replace("Selected", "선택")
                .Replace("acts.", "행동합니다.")
                .Replace("moved", "이동")
                .Replace("attacked", "공격")
                .Replace("used skill", "무공 사용")
                .Replace("used terrain", "지형 활용")
                .Replace("guards", "방어 태세")
                .Replace("waits", "대기")
                .Replace("Target preview. Click the enemy again to attack.", "대상 예측 표시. 같은 적을 다시 클릭하면 공격합니다.")
                .Replace("No enemy target.", "공격할 적이 없습니다.")
                .Replace("No skill target.", "무공 대상이 없습니다.")
                .Replace("No usable terrain object in range.", "사거리 안에 활용할 지형 오브젝트가 없습니다.")
                .Replace("Move already spent.", "이미 이동했습니다.")
                .Replace("Action already spent.", "이미 행동했습니다.")
                .Replace("Blocked tile.", "막힌 칸입니다.")
                .Replace("Out of movement range.", "이동 범위 밖입니다.")
                .Replace("Target out of range.", "공격 사거리 밖입니다.")
                .Replace("Skill target out of range.", "무공 사거리 밖입니다.")
                .Replace("Skill unavailable.", "무공을 사용할 수 없습니다.")
                .Replace("Invalid skill target.", "무공 대상이 올바르지 않습니다.")
                .Replace("missed", "빗나감")
                .Replace("hit", "명중")
                .Replace("for", "피해")
                .Replace("defeated", "전투불능")
                .Replace("Broken: counter sealed.", "파훼: 반격 봉쇄")
                .Replace("break", "파훼")
                .Replace("counters", "반격")
                .Replace("follows up", "추격")
                .Replace("poisoned", "중독")
                .Replace("slowed", "둔화")
                .Replace("marked", "표식 부여")
                .Replace("Rewind used", "천기역전 사용")
                .Replace("Remaining", "남은 횟수")
                .Replace("Reset", "초기화")
                .Replace("No rewind point.", "되돌릴 지점이 없습니다.")
                .Replace("Rewind unavailable.", "천기역전을 사용할 수 없습니다.");
        }

        private readonly struct TerrainProfile
        {
            public readonly TerrainType terrain;
            public readonly Color color;
            public readonly int elevation;
            public readonly int coverBonus;
            public readonly int moveCost;
            public readonly bool walkable;
            public readonly HazardType hazard;

            public TerrainProfile(TerrainType terrain, Color color, int elevation, int coverBonus, int moveCost, bool walkable, HazardType hazard = HazardType.None)
            {
                this.terrain = terrain;
                this.color = color;
                this.elevation = elevation;
                this.coverBonus = coverBonus;
                this.moveCost = moveCost;
                this.walkable = walkable;
                this.hazard = hazard;
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
        public SkillStyle style = SkillStyle.Sword;
        public int maxHp = 30;
        public int maxInner = 3;
        public int initiative = 10;
        public int agility = 10;
        public int moveRange = 4;
        public int attackMinRange = 1;
        public int attackRange = 1;
        public string basicAttackName = "기본 공격";
        public int attackBonus = 5;
        public int defense = 14;
        public int damageMin = 5;
        public int damageMax = 9;
        public bool canCounter = true;
        public bool followUpAllowed = true;
        public int counterRange = 1;
        public int counterInnerCost;
        public string specialName = "무공";
        public int specialMinRange = 1;
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
        Terrain
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

    public enum TerrainObjectKind
    {
        IncenseBrazier,
        OilLantern,
        ShrineBell,
        BridgeRope
    }

    public sealed class BattleTerrainObject : MonoBehaviour
    {
        public string displayName;
        public TerrainObjectKind kind;
        public Vector2Int cell;
        public int hp;
        public int maxHp;
        public int interactRange = 1;
        public bool used;
        public bool destroyed;
        public SpriteRenderer renderer;
        public TextMesh label;

        public void Refresh()
        {
            if (renderer != null)
            {
                float alpha = destroyed ? 0.15f : used ? 0.36f : 0.95f;
                Color color = renderer.color;
                color.a = alpha;
                renderer.color = color;
            }

            if (label != null)
            {
                label.text = destroyed ? displayName + " (파괴)" : used ? displayName + " (사용)" : displayName;
                label.color = destroyed
                    ? new Color(0.65f, 0.60f, 0.52f, 0.55f)
                    : new Color(0.96f, 0.88f, 0.58f, used ? 0.62f : 1f);
            }
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
        public HazardType hazard;
        public SpriteRenderer baseRenderer;
        public SpriteRenderer highlightRenderer;

        public void SetHighlight(Color color)
        {
            if (highlightRenderer != null)
            {
                highlightRenderer.color = color;
            }
        }

        public void SetBaseColor(Color color)
        {
            if (baseRenderer != null)
            {
                baseRenderer.color = color;
            }
        }
    }

    public sealed class BattleTestUnitView : MonoBehaviour
    {
        private TextMesh label;
        private CharacterVisualController visualController;

        public BattleTestUnit Unit { get; private set; }

        public void Bind(BattleTestUnit unit, CharacterVisualController controller)
        {
            Unit = unit;
            visualController = controller;
            label = CreateLabel();
            Refresh(false);
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

            MeshRenderer renderer = labelObject.GetComponent<MeshRenderer>();
            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = 5000;
            return mesh;
        }
    }

    public sealed class BattleTestUnit
    {
        public readonly BattleTestUnitDefinition definition;
        public readonly BattleTestUnitView view;
        public Vector2Int cell;
        public int hp;
        public int inner;
        public int initiative;
        public int specialCooldownLeft;
        public int breakGauge;
        public bool moved;
        public bool acted;
        public bool turnEnded;
        public bool defeated;
        public bool guarded;
        public bool counterSpent;
        public bool broken;
        public bool disarmed;
        public bool prone;
        public bool poisoned;
        public bool chilled;
        public bool marked;
        public bool supported;

        public BattleTestUnit(BattleTestUnitDefinition definition, BattleTestUnitView view)
        {
            this.definition = definition;
            this.view = view;
            cell = definition.startCell;
            hp = definition.maxHp;
            inner = definition.maxInner;
        }
    }

    public sealed class BattleSnapshot
    {
        public string activeUnitId;
        public string reason;
        public BattleCommandMode commandMode;
        public UnitSnapshot[] units;
        public TileSnapshot[] tiles;
        public TerrainObjectSnapshot[] terrainObjects;

        public static BattleSnapshot Capture(
            IReadOnlyList<BattleTestUnit> units,
            BattleTestTile[,] tiles,
            IReadOnlyList<BattleTerrainObject> terrainObjects,
            BattleTestUnit activeUnit,
            BattleCommandMode commandMode,
            string reason)
        {
            BattleSnapshot snapshot = new BattleSnapshot
            {
                activeUnitId = activeUnit != null ? activeUnit.definition.id : string.Empty,
                commandMode = commandMode,
                reason = reason,
                units = new UnitSnapshot[units.Count],
                tiles = CaptureTiles(tiles),
                terrainObjects = new TerrainObjectSnapshot[terrainObjects.Count]
            };

            for (int i = 0; i < units.Count; i++)
            {
                snapshot.units[i] = UnitSnapshot.Capture(units[i]);
            }

            for (int i = 0; i < terrainObjects.Count; i++)
            {
                snapshot.terrainObjects[i] = TerrainObjectSnapshot.Capture(terrainObjects[i]);
            }

            return snapshot;
        }

        public void Restore(
            IReadOnlyList<BattleTestUnit> targetUnits,
            BattleTestTile[,] targetTiles,
            IReadOnlyList<BattleTerrainObject> targetObjects,
            System.Func<TerrainType, HazardType, Color> hazardColor)
        {
            for (int i = 0; i < units.Length && i < targetUnits.Count; i++)
            {
                units[i].Restore(targetUnits[i]);
            }

            RestoreTiles(targetTiles, hazardColor);

            for (int i = 0; i < terrainObjects.Length && i < targetObjects.Count; i++)
            {
                terrainObjects[i].Restore(targetObjects[i]);
            }
        }

        private static TileSnapshot[] CaptureTiles(BattleTestTile[,] tiles)
        {
            int width = tiles.GetLength(0);
            int height = tiles.GetLength(1);
            TileSnapshot[] result = new TileSnapshot[width * height];
            int index = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    result[index++] = TileSnapshot.Capture(tiles[x, y]);
                }
            }

            return result;
        }

        private void RestoreTiles(BattleTestTile[,] targetTiles, System.Func<TerrainType, HazardType, Color> hazardColor)
        {
            int width = targetTiles.GetLength(0);
            int height = targetTiles.GetLength(1);
            int index = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width && index < tiles.Length; x++)
                {
                    tiles[index++].Restore(targetTiles[x, y], hazardColor);
                }
            }
        }
    }

    public struct UnitSnapshot
    {
        public Vector2Int cell;
        public Vector3 worldPosition;
        public int hp;
        public int inner;
        public int specialCooldownLeft;
        public int breakGauge;
        public bool moved;
        public bool acted;
        public bool turnEnded;
        public bool defeated;
        public bool guarded;
        public bool counterSpent;
        public bool broken;
        public bool disarmed;
        public bool prone;
        public bool poisoned;
        public bool chilled;
        public bool marked;
        public bool supported;

        public static UnitSnapshot Capture(BattleTestUnit unit)
        {
            return new UnitSnapshot
            {
                cell = unit.cell,
                worldPosition = unit.view.transform.position,
                hp = unit.hp,
                inner = unit.inner,
                specialCooldownLeft = unit.specialCooldownLeft,
                breakGauge = unit.breakGauge,
                moved = unit.moved,
                acted = unit.acted,
                turnEnded = unit.turnEnded,
                defeated = unit.defeated,
                guarded = unit.guarded,
                counterSpent = unit.counterSpent,
                broken = unit.broken,
                disarmed = unit.disarmed,
                prone = unit.prone,
                poisoned = unit.poisoned,
                chilled = unit.chilled,
                marked = unit.marked,
                supported = unit.supported
            };
        }

        public void Restore(BattleTestUnit unit)
        {
            unit.cell = cell;
            unit.hp = hp;
            unit.inner = inner;
            unit.specialCooldownLeft = specialCooldownLeft;
            unit.breakGauge = breakGauge;
            unit.moved = moved;
            unit.acted = acted;
            unit.turnEnded = turnEnded;
            unit.defeated = defeated;
            unit.guarded = guarded;
            unit.counterSpent = counterSpent;
            unit.broken = broken;
            unit.disarmed = disarmed;
            unit.prone = prone;
            unit.poisoned = poisoned;
            unit.chilled = chilled;
            unit.marked = marked;
            unit.supported = supported;
            unit.view.transform.position = worldPosition;
            unit.view.SetDefeated(defeated);
        }
    }

    public struct TileSnapshot
    {
        public HazardType hazard;

        public static TileSnapshot Capture(BattleTestTile tile)
        {
            return new TileSnapshot
            {
                hazard = tile.hazard
            };
        }

        public void Restore(BattleTestTile tile, System.Func<TerrainType, HazardType, Color> hazardColor)
        {
            tile.hazard = hazard;
            tile.SetBaseColor(hazardColor(tile.terrain, hazard));
        }
    }

    public struct TerrainObjectSnapshot
    {
        public int hp;
        public bool used;
        public bool destroyed;

        public static TerrainObjectSnapshot Capture(BattleTerrainObject terrainObject)
        {
            return new TerrainObjectSnapshot
            {
                hp = terrainObject.hp,
                used = terrainObject.used,
                destroyed = terrainObject.destroyed
            };
        }

        public void Restore(BattleTerrainObject terrainObject)
        {
            terrainObject.hp = hp;
            terrainObject.used = used;
            terrainObject.destroyed = destroyed;
            terrainObject.Refresh();
        }
    }
}
