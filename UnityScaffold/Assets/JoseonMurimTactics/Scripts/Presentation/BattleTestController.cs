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
        private BattleForecastPanel battleForecastPanel;
        private BattleForecast currentForecast;

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
            else if (Input.GetKeyDown(KeyCode.T))
            {
                showThreatRange = !showThreatRange;
                RefreshHighlights();
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

        private void OnGUI()
        {
            EnsureGuiStyles();

            DrawActivePanel();
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
            GUI.Label(new Rect((Screen.width * 0.5f) - 146f, 42f, 310f, 28f), "Battle Finished", titleStyle);
        }

        private void DrawActivePanel()
        {
            GUI.Box(new Rect(18f, 18f, 340f, 336f), GUIContent.none, panelStyle);
            string activeName = activeUnit == null ? "None" : activeUnit.definition.displayName;
            string hp = activeUnit == null ? string.Empty : $"{activeUnit.hp}/{activeUnit.definition.maxHp}";
            string side = activeUnit == null ? string.Empty : activeUnit.definition.faction.ToString();
            string phase = phaseTurnController == null ? "PlayerPhase" : phaseTurnController.Phase.ToString();

            GUI.Label(new Rect(34f, 30f, 300f, 28f), $"Round {phaseTurnController?.Round ?? round}  {phase}", titleStyle);
            GUI.Label(new Rect(34f, 58f, 300f, 22f), objectiveManager != null ? objectiveManager.ScenarioTitle : "Battle Test", smallStyle);
            GUI.Label(new Rect(34f, 82f, 300f, 24f), $"Now: {activeName}", labelStyle);
            GUI.Label(new Rect(34f, 108f, 300f, 22f), $"Side: {side}   HP: {hp}", smallStyle);

            if (activeUnit != null)
            {
                GUI.Label(new Rect(34f, 132f, 300f, 22f), $"Move: {ActionText(activeUnit.moved)}   Action: {ActionText(activeUnit.acted)}   Done: {YesNo(activeUnit.turnEnded)}", smallStyle);
                GUI.Label(new Rect(34f, 156f, 300f, 22f), $"Inner: {activeUnit.inner}/{activeUnit.definition.maxInner}   Break: {activeUnit.breakGauge}/100", smallStyle);
                GUI.Label(new Rect(34f, 180f, 300f, 22f), $"Mode: {commandMode}   Threat: {YesNo(showThreatRange)}", labelStyle);
            }

            bool playerTurn = activeUnit != null && activeUnit.definition.faction == Faction.Ally && !busy && !battleOver && phaseTurnController.Phase == BattlePhase.PlayerPhase;
            GUI.enabled = playerTurn;

            if (GUI.Button(new Rect(34f, 212f, 70f, 28f), "Move"))
            {
                SetCommandMode(BattleCommandMode.Move);
            }

            GUI.enabled = playerTurn && activeUnit != null && !activeUnit.acted;
            if (GUI.Button(new Rect(112f, 212f, 70f, 28f), "Attack"))
            {
                SetCommandMode(BattleCommandMode.Attack);
            }

            GUI.enabled = playerTurn && activeUnit != null && CanUseSpecial(activeUnit);
            if (GUI.Button(new Rect(190f, 212f, 70f, 28f), "Skill"))
            {
                SetCommandMode(BattleCommandMode.Skill);
            }

            GUI.enabled = playerTurn && activeUnit != null && !activeUnit.acted;
            if (GUI.Button(new Rect(268f, 212f, 70f, 28f), "Guard"))
            {
                GuardActiveUnit();
            }

            GUI.enabled = playerTurn;
            if (GUI.Button(new Rect(34f, 250f, 96f, 30f), "Wait"))
            {
                EndTurn();
            }

            GUI.enabled = true;
            if (GUI.Button(new Rect(138f, 250f, 96f, 30f), "Threat"))
            {
                showThreatRange = !showThreatRange;
                RefreshHighlights();
            }

            if (GUI.Button(new Rect(242f, 250f, 96f, 30f), "Reset"))
            {
                BuildBattle();
            }

            GUI.enabled = true;

            if (activeUnit != null)
            {
                string skillLine = activeUnit.definition.specialName;
                string cooldown = activeUnit.specialCooldownLeft > 0 ? $"CD {activeUnit.specialCooldownLeft}" : "Ready";
                GUI.Label(new Rect(34f, 290f, 304f, 22f), $"Skill: {skillLine} ({cooldown})", smallStyle);
                GUI.Label(new Rect(34f, 312f, 304f, 22f), $"Style: {activeUnit.definition.style}   Counter: R{activeUnit.definition.counterRange}", smallStyle);
            }
        }

        private void DrawTurnQueuePanel()
        {
            float x = Screen.width - 386f;
            GUI.Box(new Rect(x, 18f, 368f, 194f), GUIContent.none, panelStyle);
            GUI.Label(new Rect(x + 16f, 30f, 336f, 26f), "Turn Order", titleStyle);

            List<BattleTestUnit> queue = GetTurnQueuePreview(7);
            for (int i = 0; i < queue.Count; i++)
            {
                BattleTestUnit unit = queue[i];
                string marker = i == 0 ? "NOW" : $"#{i + 1}";
                string state = unit.defeated ? "Down" : UnitStatusText(unit);
                string line = $"{marker}  {unit.definition.displayName}  {unit.definition.faction}  {state}";
                GUI.Label(new Rect(x + 16f, 60f + (i * 18f), 336f, 18f), line, i == 0 ? labelStyle : smallStyle);
            }
        }

        private void DrawLogPanel()
        {
            float x = Screen.width - 386f;
            GUI.Box(new Rect(x, 224f, 368f, 244f), GUIContent.none, panelStyle);
            GUI.Label(new Rect(x + 16f, 236f, 336f, 26f), "Combat Log", titleStyle);

            int start = Mathf.Max(0, battleLog.Count - 9);
            for (int i = start; i < battleLog.Count; i++)
            {
                GUI.Label(new Rect(x + 16f, 268f + ((i - start) * 22f), 336f, 22f), battleLog[i], logStyle);
            }
        }

        private void DrawInspectPanel()
        {
            GUI.Box(new Rect(18f, 332f, 340f, 142f), GUIContent.none, panelStyle);
            GUI.Label(new Rect(34f, 344f, 300f, 26f), "Inspect", titleStyle);

            if (hoveredUnit != null)
            {
                GUI.Label(new Rect(34f, 374f, 300f, 22f), $"{hoveredUnit.definition.displayName} ({hoveredUnit.definition.faction})", labelStyle);
                GUI.Label(new Rect(34f, 398f, 300f, 22f), $"HP {hoveredUnit.hp}/{hoveredUnit.definition.maxHp}   DEF {DefenseValue(hoveredUnit, TileAt(hoveredUnit.cell))}", smallStyle);
                GUI.Label(new Rect(34f, 422f, 300f, 22f), $"Status: {UnitStatusText(hoveredUnit)}", smallStyle);
                GUI.Label(new Rect(34f, 446f, 300f, 22f), $"Skill: {hoveredUnit.definition.specialName}", smallStyle);
                return;
            }

            if (hoveredTile != null)
            {
                GUI.Label(new Rect(34f, 374f, 300f, 22f), $"{hoveredTile.terrain}  ({hoveredTile.cell.x},{hoveredTile.cell.y})", labelStyle);
                GUI.Label(new Rect(34f, 398f, 300f, 22f), $"Move Cost {hoveredTile.moveCost}   Cover +{hoveredTile.coverBonus}", smallStyle);
                GUI.Label(new Rect(34f, 422f, 300f, 22f), $"Elevation {hoveredTile.elevation}   Hazard {hoveredTile.hazard}", smallStyle);
                return;
            }

            GUI.Label(new Rect(34f, 374f, 300f, 22f), "No target", smallStyle);
        }

        private void DrawForecastPanel()
        {
            if (battleForecastPanel == null || activeUnit == null)
            {
                return;
            }

            Rect rect = new Rect(18f, 486f, 340f, 190f);
            battleForecastPanel.Draw(rect, currentForecast, panelStyle, titleStyle, smallStyle);
        }

        private void DrawRosterPanel()
        {
            float y = Screen.height - 118f;
            GUI.Box(new Rect(18f, y, Screen.width - 36f, 100f), GUIContent.none, panelStyle);
            GUI.Label(new Rect(34f, y + 12f, 280f, 26f), "Unit Status", titleStyle);

            float cardWidth = Mathf.Min(220f, (Screen.width - 80f) / Mathf.Max(1, units.Count));
            for (int i = 0; i < units.Count; i++)
            {
                BattleTestUnit unit = units[i];
                float x = 34f + (i * cardWidth);
                string prefix = unit == activeUnit ? "> " : string.Empty;
                GUI.Label(new Rect(x, y + 42f, cardWidth - 8f, 22f), $"{prefix}{unit.definition.displayName}", unit == activeUnit ? labelStyle : smallStyle);
                GUI.Label(new Rect(x, y + 64f, cardWidth - 8f, 22f), $"HP {unit.hp}/{unit.definition.maxHp}  {UnitStatusText(unit)}", smallStyle);
            }
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
            hoveredTile = null;
            hoveredUnit = null;
            currentForecast = default;
            phaseTurnController = new PhaseTurnController();
            unitSelectionController = new UnitSelectionController();
            breakResolver = new BreakResolver();
            counterattackService = new CounterattackService();
            battleForecastService = new BattleForecastService(breakResolver, counterattackService);
            threatRangeService = new ThreatRangeService();
            objectiveManager = new ObjectiveManager();
            enemyTacticsAI = new EnemyTacticsAI(breakResolver, battleForecastService);
            battleForecastPanel = new BattleForecastPanel();

            diamondSprite = diamondSprite == null ? CreateDiamondSprite() : diamondSprite;
            CreateTerrain();
            SpawnUnits();
            CenterCamera();

            AddLog(objectiveManager.ScenarioTitle);
            AddLog("Player Phase: choose allies in any order.");
            phaseTurnController.StartBattle(units);
            BeginPlayerPhase();
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
                    tile.highlightRenderer = highlightRenderer;
                    tiles[x, y] = tile;
                }
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
            AddLog($"Player Phase {phaseTurnController.Round}: choose unit order.");
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
                AddLog("Enemy Phase.");
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
                if (unitSelectionController.TrySelect(clickedUnit))
                {
                    activeUnit = clickedUnit;
                    commandMode = BattleCommandMode.Move;
                    currentForecast = default;
                    AddLog($"Selected {activeUnit.definition.displayName}.");
                    RefreshHighlights();
                    RefreshUnits();
                }
                else
                {
                    AddLog("That ally already acted.");
                }

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

            unit.cell = destination.cell;
            unit.moved = true;
            ApplyTileEntryEffect(unit, destination);
            StartCoroutine(AnimateMove(unit, UnitWorldPosition(destination.cell)));
            AddLog($"{unit.definition.displayName} moved.");
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
            if (distance > attacker.definition.attackRange)
            {
                AddLog("Target out of range.");
                return false;
            }

            bool hit = ResolveAttack(attacker, target, false);
            attacker.acted = true;
            if (hit)
            {
                ResolveCounterIfPossible(target, attacker);
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
            int attackBonus = attacker.definition.attackBonus + styleBonus + (special ? attacker.definition.specialAttackBonus : 0);
            int attackTotal = d20 + attackBonus + heightBonus;
            int defense = DefenseValue(target, to);
            bool critical = d20 == 20;
            bool hit = critical || (d20 != 1 && attackTotal >= defense);

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
            if (distance > actor.definition.specialRange)
            {
                AddLog("Skill target out of range.");
                return false;
            }

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
            }
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
            Queue<Vector2Int> frontier = new Queue<Vector2Int>();
            cost[unit.cell] = 0;
            frontier.Enqueue(unit.cell);

            while (frontier.Count > 0)
            {
                Vector2Int current = frontier.Dequeue();
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

                    int nextCost = cost[current] + Mathf.Max(1, tile.moveCost);
                    if (nextCost > EffectiveMoveRange(unit))
                    {
                        continue;
                    }

                    if (cost.TryGetValue(next, out int oldCost) && oldCost <= nextCost)
                    {
                        continue;
                    }

                    cost[next] = nextCost;
                    frontier.Enqueue(next);
                }
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
                        tile.SetHighlight(new Color(1f, 0.12f, 0.08f, 0.26f));
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

                    if (GridDistance(activeUnit.cell, target.cell) <= activeUnit.definition.attackRange)
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

                    if (GridDistance(activeUnit.cell, target.cell) <= activeUnit.definition.specialRange)
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
            foreach (BattleTestUnit unit in units)
            {
                unit.view.Refresh(unit == activeUnit);
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
            AddLog(outcome == BattleOutcome.Victory ? "Victory: inspector subdued." : "Defeat: objective failed.");
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
            if ((x == 6 && y == 5) || (x == 7 && y == 5) || (x == 8 && y == 5))
            {
                return new TerrainProfile(TerrainType.Wall, new Color(0.22f, 0.21f, 0.22f, 1f), 1, 0, 99, false);
            }

            if ((x == 5 && y == 2) || (x == 6 && y == 2))
            {
                return new TerrainProfile(TerrainType.Stone, new Color(0.42f, 0.42f, 0.43f, 1f), 0, 2, 1, true, HazardType.Smoke);
            }

            if (x == 7 && y == 2)
            {
                return new TerrainProfile(TerrainType.Stone, new Color(0.70f, 0.20f, 0.12f, 1f), 0, 0, 2, true, HazardType.Fire);
            }

            if (x == 6 && y == 4)
            {
                return new TerrainProfile(TerrainType.Water, new Color(0.62f, 0.78f, 0.86f, 1f), 0, 0, 2, true, HazardType.Ice);
            }

            if (y == 3 && x >= 2 && x <= 7)
            {
                bool bridge = x == 4 || x == 5;
                return bridge
                    ? new TerrainProfile(TerrainType.Bridge, new Color(0.49f, 0.31f, 0.16f, 1f), 0, 0, 1, true)
                    : new TerrainProfile(TerrainType.Water, new Color(0.16f, 0.37f, 0.50f, 1f), 0, 0, 3, true, HazardType.Slippery);
            }

            if (x <= 2 && y >= 4)
            {
                return new TerrainProfile(TerrainType.Bamboo, new Color(0.22f, 0.42f, 0.24f, 1f), 0, 1, 2, true);
            }

            if (x >= 8 && y <= 2)
            {
                return new TerrainProfile(TerrainType.Roof, new Color(0.54f, 0.23f, 0.16f, 1f), 1, 0, 1, true);
            }

            if (x >= 9 && y >= 5)
            {
                return new TerrainProfile(TerrainType.Cliff, new Color(0.33f, 0.30f, 0.26f, 1f), 1, 2, 2, true);
            }

            return new TerrainProfile(TerrainType.Stone, new Color(0.47f, 0.43f, 0.34f, 1f), 0, 0, 1, true);
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
            camera.backgroundColor = new Color(0.08f, 0.075f, 0.065f, 1f);
        }

        private bool PointerOverHud(Vector3 screenPosition)
        {
            float guiY = Screen.height - screenPosition.y;
            Vector2 point = new Vector2(screenPosition.x, guiY);
            Rect leftPanel = new Rect(18f, 18f, 340f, 658f);
            Rect rightPanel = new Rect(Screen.width - 386f, 18f, 368f, 450f);
            Rect bottomPanel = new Rect(18f, Screen.height - 118f, Screen.width - 36f, 100f);
            return leftPanel.Contains(point) || rightPanel.Contains(point) || bottomPanel.Contains(point);
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
        public int moveRange = 4;
        public int attackRange = 1;
        public string basicAttackName = "Basic Strike";
        public int attackBonus = 5;
        public int defense = 14;
        public int damageMin = 5;
        public int damageMax = 9;
        public bool canCounter = true;
        public int counterRange = 1;
        public int counterInnerCost;
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
        Skill
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

    public sealed class BattleTestTile : MonoBehaviour
    {
        public Vector2Int cell;
        public TerrainType terrain;
        public int elevation;
        public bool walkable = true;
        public int moveCost = 1;
        public int coverBonus;
        public HazardType hazard;
        public SpriteRenderer highlightRenderer;

        public void SetHighlight(Color color)
        {
            if (highlightRenderer != null)
            {
                highlightRenderer.color = color;
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
                label.text = $"{Unit.definition.displayName}\nHP {Unit.hp}/{Unit.definition.maxHp}";
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

        public BattleTestUnit(BattleTestUnitDefinition definition, BattleTestUnitView view)
        {
            this.definition = definition;
            this.view = view;
            cell = definition.startCell;
            hp = definition.maxHp;
            inner = definition.maxInner;
        }
    }
}
