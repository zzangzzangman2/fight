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

        private const int SmokeCoverBonus = 2;
        private const int CoverInteractBonus = 2;
        private const int FireInteractDamage = 4;

        private readonly List<BattleTestUnit> units = new List<BattleTestUnit>();
        private readonly List<string> battleLog = new List<string>();
        private readonly List<BattleTestInteractable> interactables = new List<BattleTestInteractable>();
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
        private int activeIndex;
        private int round = 1;
        private bool busy;
        private bool aiQueued;
        private bool battleOver;
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

            if (busy || activeUnit == null)
            {
                return;
            }

            if (activeUnit.definition.faction == Faction.Enemy)
            {
                if (!aiQueued)
                {
                    aiQueued = true;
                    StartCoroutine(RunEnemyTurn());
                }

                return;
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                EndTurn();
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                SetCommandMode(BattleCommandMode.Move);
            }
            else if (Input.GetKeyDown(KeyCode.Alpha1))
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
            GUI.Box(new Rect(18f, 18f, 340f, 326f), GUIContent.none, panelStyle);
            string activeName = activeUnit == null ? "None" : activeUnit.definition.displayName;
            string hp = activeUnit == null ? string.Empty : $"{activeUnit.hp}/{activeUnit.definition.maxHp}";
            string side = activeUnit == null ? string.Empty : activeUnit.definition.faction.ToString();

            GUI.Label(new Rect(34f, 30f, 300f, 28f), $"Round {round}", titleStyle);
            GUI.Label(new Rect(34f, 62f, 300f, 24f), $"Now: {activeName}", labelStyle);
            GUI.Label(new Rect(34f, 88f, 300f, 22f), $"Side: {side}   HP: {hp}", smallStyle);

            if (activeUnit != null)
            {
                GUI.Label(new Rect(34f, 112f, 300f, 22f), $"Move: {ActionText(activeUnit.moved)}   Action: {ActionText(activeUnit.acted)}", smallStyle);
                GUI.Label(new Rect(34f, 136f, 300f, 22f), $"Inner: {activeUnit.inner}/{activeUnit.definition.maxInner}   Guard: {YesNo(activeUnit.guarded)}", smallStyle);
                GUI.Label(new Rect(34f, 160f, 300f, 22f), $"Mode: {commandMode}", labelStyle);
            }

            bool playerTurn = activeUnit != null && activeUnit.definition.faction == Faction.Ally && !busy && !battleOver;
            GUI.enabled = playerTurn;

            if (GUI.Button(new Rect(34f, 192f, 70f, 28f), "Move"))
            {
                SetCommandMode(BattleCommandMode.Move);
            }

            GUI.enabled = playerTurn && activeUnit != null && !activeUnit.acted;
            if (GUI.Button(new Rect(112f, 192f, 70f, 28f), "Attack"))
            {
                SetCommandMode(BattleCommandMode.Attack);
            }

            GUI.enabled = playerTurn && activeUnit != null && CanUseSpecial(activeUnit);
            if (GUI.Button(new Rect(190f, 192f, 70f, 28f), "Skill"))
            {
                SetCommandMode(BattleCommandMode.Skill);
            }

            GUI.enabled = playerTurn && activeUnit != null && !activeUnit.acted;
            if (GUI.Button(new Rect(268f, 192f, 70f, 28f), "Guard"))
            {
                GuardActiveUnit();
            }

            GUI.enabled = playerTurn && activeUnit != null && !activeUnit.acted && HasUsableInteractable(activeUnit);
            if (GUI.Button(new Rect(34f, 230f, 92f, 30f), "Interact"))
            {
                SetCommandMode(BattleCommandMode.Interact);
            }

            GUI.enabled = playerTurn;
            if (GUI.Button(new Rect(134f, 230f, 92f, 30f), "행동 끝"))
            {
                EndTurn();
            }

            if (GUI.Button(new Rect(234f, 230f, 104f, 30f), "Reset"))
            {
                BuildBattle();
            }

            GUI.enabled = true;

            if (activeUnit != null)
            {
                string skillLine = activeUnit.definition.specialName;
                string cooldown = activeUnit.specialCooldownLeft > 0 ? $"CD {activeUnit.specialCooldownLeft}" : "Ready";
                GUI.Label(new Rect(34f, 266f, 304f, 22f), $"Skill: {skillLine} ({cooldown})", smallStyle);
                GUI.Label(new Rect(34f, 290f, 304f, 22f), $"Agi: {AgilityValue(activeUnit)}   Counter: {CounterSummary(activeUnit)}", smallStyle);
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
            GUI.Box(new Rect(18f, 356f, 340f, 190f), GUIContent.none, panelStyle);
            GUI.Label(new Rect(34f, 368f, 300f, 26f), "Inspect", titleStyle);

            if (hoveredUnit != null)
            {
                GUI.Label(new Rect(34f, 398f, 300f, 22f), $"{hoveredUnit.definition.displayName} ({hoveredUnit.definition.faction})", labelStyle);
                GUI.Label(new Rect(34f, 422f, 300f, 22f), $"HP {hoveredUnit.hp}/{hoveredUnit.definition.maxHp}   DEF {DefenseValue(hoveredUnit, TileAt(hoveredUnit.cell))}", smallStyle);
                GUI.Label(new Rect(34f, 446f, 300f, 22f), $"Status: {UnitStatusText(hoveredUnit)}", smallStyle);
                GUI.Label(new Rect(34f, 470f, 300f, 22f), $"Skill: {hoveredUnit.definition.specialName}", smallStyle);
                return;
            }

            if (hoveredTile != null)
            {
                BattleTestInteractable prop = GetInteractableAt(hoveredTile.cell);
                GUI.Label(new Rect(34f, 398f, 300f, 22f), $"{hoveredTile.terrain}  ({hoveredTile.cell.x},{hoveredTile.cell.y})", labelStyle);
                GUI.Label(new Rect(34f, 422f, 300f, 22f), $"Move Cost {hoveredTile.moveCost}   Cover +{hoveredTile.coverBonus}", smallStyle);
                GUI.Label(new Rect(34f, 446f, 300f, 22f), $"Elevation {hoveredTile.elevation}   Walkable {YesNo(hoveredTile.walkable)}", smallStyle);
                if (prop != null)
                {
                    int distance = activeUnit == null ? -1 : GridDistance(activeUnit.cell, prop.cell);
                    bool usable = activeUnit != null && !activeUnit.acted && distance <= 1;
                    GUI.Label(new Rect(34f, 470f, 300f, 22f), $"Object: {prop.displayName}", smallStyle);
                    GUI.Label(new Rect(34f, 494f, 300f, 22f), $"Effect: {InteractableEffectText(prop.kind)}   Usable: {YesNo(usable)}", smallStyle);
                }
                else
                {
                    GUI.Label(new Rect(34f, 470f, 300f, 22f), $"Hazard: {TileHazardText(hoveredTile)}", smallStyle);
                }
                return;
            }

            GUI.Label(new Rect(34f, 398f, 300f, 22f), "No target", smallStyle);
        }

        private void DrawForecastPanel()
        {
            float x = Screen.width - 386f;
            GUI.Box(new Rect(x, 452f, 368f, 224f), GUIContent.none, panelStyle);
            GUI.Label(new Rect(x + 16f, 464f, 336f, 26f), "Battle Forecast", titleStyle);

            BattleForecast forecast = BuildForecast(activeUnit, hoveredUnit);
            if (!forecast.valid)
            {
                if (!string.IsNullOrEmpty(forecast.actorName))
                {
                    GUI.Label(new Rect(x + 16f, 494f, 336f, 22f), $"{forecast.actorName} -> {forecast.targetName}  [{forecast.commandName}]", labelStyle);
                    GUI.Label(new Rect(x + 16f, 520f, 336f, 20f), $"Invalid: {forecast.invalidReason}", smallStyle);
                    GUI.Label(new Rect(x + 16f, 542f, 336f, 20f), $"Distance {forecast.distance} / Range {forecast.range}", smallStyle);
                    GUI.Label(new Rect(x + 16f, 564f, 336f, 20f), forecast.costText, smallStyle);
                }
                else
                {
                    GUI.Label(new Rect(x + 16f, 494f, 336f, 24f), forecast.invalidReason, smallStyle);
                }

                return;
            }

            GUI.Label(new Rect(x + 16f, 494f, 336f, 22f), $"{forecast.actorName} -> {forecast.targetName}  [{forecast.commandName}]", labelStyle);
            GUI.Label(new Rect(x + 16f, 520f, 336f, 20f), $"Distance {forecast.distance} / Range {forecast.range}: {forecast.rangeText}", smallStyle);
            string hitText = forecast.neededRollText == "No attack roll"
                ? "Hit: No attack roll"
                : $"Hit: d20 + {forecast.attackBonus} + height {forecast.heightBonus} + terrain {forecast.terrainBonus} vs DEF {forecast.defense}";
            GUI.Label(new Rect(x + 16f, 542f, 336f, 20f), hitText, smallStyle);
            GUI.Label(new Rect(x + 16f, 564f, 336f, 20f), $"Need: {forecast.neededRollText}", smallStyle);
            GUI.Label(new Rect(x + 16f, 586f, 336f, 20f), forecast.damageText, smallStyle);
            GUI.Label(new Rect(x + 16f, 608f, 336f, 20f), forecast.hpAfterText, smallStyle);
            GUI.Label(new Rect(x + 16f, 630f, 336f, 20f), $"{forecast.counterText}   {forecast.followUpText}", smallStyle);
            GUI.Label(new Rect(x + 16f, 650f, 336f, 20f), forecast.costText, smallStyle);
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
            interactables.Clear();
            activeUnit = null;
            activeIndex = 0;
            round = 1;
            busy = false;
            aiQueued = false;
            battleOver = false;
            commandMode = BattleCommandMode.Move;
            hoveredTile = null;
            hoveredUnit = null;

            diamondSprite = diamondSprite == null ? CreateDiamondSprite() : diamondSprite;
            CreateTerrain();
            SpawnUnits();
            units.Sort((left, right) => right.initiative.CompareTo(left.initiative));
            CenterCamera();

            AddLog("[System] Battle test ready.");
            BeginTurn();
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
                    tile.elevation = profile.elevation;
                    tile.walkable = profile.walkable;
                    tile.moveCost = profile.moveCost;
                    tile.coverBonus = profile.coverBonus;
                    tile.baseCoverBonus = profile.coverBonus;
                    tile.baseColor = profile.color;
                    tile.terrainRenderer = renderer;
                    tile.highlightRenderer = highlightRenderer;
                    tiles[x, y] = tile;
                }
            }

            CreateInteractables(terrainRoot);
        }

        private void CreateInteractables(Transform terrainRoot)
        {
            Transform propRoot = new GameObject("Interactables").transform;
            propRoot.SetParent(terrainRoot, false);

            AddInteractable(propRoot, "incense", "제단 향로", BattleTestInteractableKind.Smoke, new Vector2Int(5, 2), new Color(0.74f, 0.68f, 0.58f, 1f));
            AddInteractable(propRoot, "lantern", "붉은 등불", BattleTestInteractableKind.Fire, new Vector2Int(7, 2), new Color(1f, 0.32f, 0.18f, 1f));
            AddInteractable(propRoot, "wine_cart", "술수레", BattleTestInteractableKind.Cover, new Vector2Int(3, 5), new Color(0.64f, 0.38f, 0.18f, 1f));
        }

        private void AddInteractable(Transform parent, string id, string displayName, BattleTestInteractableKind kind, Vector2Int cell, Color color)
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
            propObject.transform.localScale = new Vector3(tileWidth * 0.34f, tileWidth * 0.34f, 1f);

            SpriteRenderer renderer = propObject.AddComponent<SpriteRenderer>();
            renderer.sprite = diamondSprite;
            renderer.color = color;
            renderer.sortingOrder = 2200 + ((cell.x + cell.y) * 2);
            interactable.renderer = renderer;
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
                unit.initiative = definition.initiative + random.Next(0, 5);
                view.Bind(unit, visual);
                units.Add(unit);
            }
        }

        private void BeginTurn()
        {
            if (CheckBattleEnd())
            {
                return;
            }

            for (int guard = 0; guard < units.Count; guard++)
            {
                if (activeIndex >= units.Count)
                {
                    activeIndex = 0;
                    round++;
                    TickRoundEffects();
                }

                BattleTestUnit candidate = units[activeIndex];
                if (!candidate.defeated)
                {
                    activeUnit = candidate;
                    ApplyStartOfTurn(activeUnit);
                    if (activeUnit.defeated)
                    {
                        activeIndex++;
                        continue;
                    }

                    activeUnit.moved = false;
                    activeUnit.acted = false;
                    aiQueued = false;
                    commandMode = BattleCommandMode.Move;
                    AddLog($"[Turn] {activeUnit.definition.displayName}");
                    RefreshHighlights();
                    RefreshUnits();
                    return;
                }

                activeIndex++;
            }
        }

        private void EndTurn()
        {
            if (battleOver)
            {
                return;
            }

            ClearHighlights();
            activeIndex++;
            BeginTurn();
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

            if (commandMode == BattleCommandMode.Attack)
            {
                if (clickedUnit != null && clickedUnit.definition.faction != activeUnit.definition.faction)
                {
                    TryAttack(activeUnit, clickedUnit, false);
                }
                else
                {
                    AddLog("[UI] No enemy target.");
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
                    AddLog("[UI] No skill target.");
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
                    AddLog("[UI] No object target.");
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

        private void TryMove(BattleTestUnit unit, BattleTestTile destination)
        {
            if (unit.moved)
            {
                AddLog("[Move] Move already spent.");
                return;
            }

            if (!destination.walkable || UnitAt(destination.cell) != null)
            {
                AddLog("[Move] Blocked tile.");
                return;
            }

            Dictionary<Vector2Int, int> reachable = GetReachableCells(unit);
            if (!reachable.ContainsKey(destination.cell))
            {
                AddLog("[Move] Out of movement range.");
                return;
            }

            unit.cell = destination.cell;
            unit.moved = true;
            ApplyTileEntry(unit, destination);
            StartCoroutine(AnimateMove(unit, UnitWorldPosition(destination.cell)));
            AddLog($"[Move] {unit.definition.displayName} moved.");
        }

        private bool TryAttack(BattleTestUnit attacker, BattleTestUnit target, bool endAfterAttack)
        {
            if (attacker.acted)
            {
                AddLog("[Action] Action already spent.");
                return false;
            }

            if (target.defeated)
            {
                return false;
            }

            int distance = GridDistance(attacker.cell, target.cell);
            if (distance > attacker.definition.attackRange)
            {
                AddLog("[Attack] Target out of range.");
                return false;
            }

            ResolveAttack(attacker, target, false);
            ResolvePostAttack(attacker, target, false);
            attacker.acted = true;
            RefreshUnits();

            if (CheckBattleEnd())
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
            int d20 = random.Next(1, 21);
            int heightBonus = from != null && to != null && from.elevation > to.elevation ? 2 : 0;
            int attackBonus = attacker.definition.attackBonus + (special ? attacker.definition.specialAttackBonus : 0);
            int attackTotal = d20 + attackBonus + heightBonus;
            int defense = DefenseValue(target, to);
            bool critical = d20 == 20;
            bool hit = critical || (d20 != 1 && attackTotal >= defense);

            if (!hit)
            {
                AddLog($"[Miss] {attacker.definition.displayName} missed {target.definition.displayName}. d20 {d20}+{attackBonus}+{heightBonus} vs DEF {defense}");
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
            AddLog($"[Hit] {attacker.definition.displayName} rolls {d20}+{attackBonus}+{heightBonus} vs DEF {defense}.");
            AddLog($"[Damage] {target.definition.displayName} takes {damage}.");

            if (target.hp == 0)
            {
                target.defeated = true;
                target.view.SetDefeated(true);
                AddLog($"[Damage] {target.definition.displayName} defeated.");
            }

            return true;
        }

        private bool TrySpecial(BattleTestUnit actor, BattleTestUnit target)
        {
            if (!CanUseSpecial(actor))
            {
                AddLog("[Skill] Skill unavailable.");
                return false;
            }

            if (!IsValidSpecialTarget(actor, target))
            {
                AddLog("[Skill] Invalid skill target.");
                return false;
            }

            int distance = GridDistance(actor.cell, target.cell);
            if (distance > actor.definition.specialRange)
            {
                AddLog("[Skill] Skill target out of range.");
                return false;
            }

            actor.inner -= actor.definition.specialCost;
            actor.specialCooldownLeft = actor.definition.specialCooldown;
            actor.acted = true;

            bool attackLike = ApplySpecialEffect(actor, target, true);
            if (attackLike)
            {
                ResolvePostAttack(actor, target, true);
            }

            RefreshHighlights();
            RefreshUnits();
            CheckBattleEnd();
            return true;
        }

        private bool ApplySpecialEffect(BattleTestUnit actor, BattleTestUnit target, bool allowStatus)
        {
            switch (actor.definition.specialEffect)
            {
                case BattleSpecialEffect.Heal:
                    int healed = Mathf.Min(target.definition.maxHp - target.hp, actor.definition.specialPower + random.Next(4, 9));
                    target.hp += Mathf.Max(0, healed);
                    target.poisoned = false;
                    target.chilled = false;
                    AddLog($"[Skill] {actor.definition.displayName} used {actor.definition.specialName}. {target.definition.displayName} healed {healed}.");
                    return false;
                case BattleSpecialEffect.Poison:
                    bool poisonHit = ResolveAttack(actor, target, true);
                    if (allowStatus && poisonHit && !target.defeated)
                    {
                        target.poisoned = true;
                        AddLog($"[Status] {target.definition.displayName} poisoned.");
                    }
                    return true;
                case BattleSpecialEffect.Freeze:
                    bool freezeHit = ResolveAttack(actor, target, true);
                    if (allowStatus && freezeHit && !target.defeated)
                    {
                        target.chilled = true;
                        AddLog($"[Status] {target.definition.displayName} slowed.");
                    }
                    return true;
                case BattleSpecialEffect.Mark:
                    target.marked = true;
                    AddLog($"[Status] {actor.definition.displayName} marked {target.definition.displayName}.");
                    return false;
                case BattleSpecialEffect.BreakGuard:
                    target.guarded = false;
                    target.marked = true;
                    ResolveAttack(actor, target, true);
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
                AddLog($"[Counter] {target.definition.displayName} uses {counter.label}.");
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
                AddLog($"[Counter] {target.definition.displayName} cannot counter.");
            }

            if (!attacker.defeated && !target.defeated && CanFollowUp(attacker, target, special))
            {
                AddLog($"[Follow-up] {attacker.definition.displayName} attacks again.");
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
            if (distance <= defender.definition.attackRange)
            {
                return new BattleTestCounterMove(false, "Attack");
            }

            if (CanUseCounterSpecial(defender) && distance <= defender.definition.specialRange)
            {
                return new BattleTestCounterMove(true, defender.definition.specialName);
            }

            return BattleTestCounterMove.None;
        }

        private bool CanUseCounterSpecial(BattleTestUnit unit)
        {
            if (unit == null || unit.defeated || unit.inner < unit.definition.specialCost || unit.specialCooldownLeft > 0)
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

            int range = special ? attacker.definition.specialRange : attacker.definition.attackRange;
            if (range >= 4 || (special && (attacker.definition.specialEffect == BattleSpecialEffect.Heal || attacker.definition.specialEffect == BattleSpecialEffect.Mark)))
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

        private bool TryInteract(BattleTestUnit actor, BattleTestTile clickedTile)
        {
            if (actor == null || actor.acted)
            {
                AddLog("[Action] Action already spent.");
                return false;
            }

            BattleTestInteractable interactable = FindUsableInteractable(actor, clickedTile.cell);
            if (interactable == null)
            {
                AddLog("[Terrain] No usable terrain object in reach.");
                return false;
            }

            interactable.used = true;
            if (interactable.renderer != null)
            {
                interactable.renderer.color = new Color(0.35f, 0.35f, 0.35f, 0.72f);
            }

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
                    AddLog($"[Terrain] {actor.definition.displayName} used {interactable.displayName}: cover +{SmokeCoverBonus}.");
                    break;
                case BattleTestInteractableKind.Fire:
                    if (tile != null)
                    {
                        tile.fireTurns = 2;
                        RefreshTerrainTint(tile);
                    }
                    DamageUnitsAround(actor, interactable.cell, 1, FireInteractDamage, "flame");
                    AddLog($"[Terrain] {actor.definition.displayName} used {interactable.displayName}: flame damage {FireInteractDamage}.");
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
                    AddLog($"[Terrain] {actor.definition.displayName} shoved {interactable.displayName}: cover +{CoverInteractBonus}.");
                    break;
            }

            actor.acted = true;
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
            AddLog($"[Guard] {activeUnit.definition.displayName} guards.");
            RefreshHighlights();
            RefreshUnits();
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

        private IEnumerator RunEnemyTurn()
        {
            busy = true;
            yield return new WaitForSeconds(0.3f);

            BattleTestUnit target = FindNearestEnemy(activeUnit);
            if (target == null)
            {
                busy = false;
                EndTurn();
                yield break;
            }

            int desiredRange = CanUseSpecial(activeUnit) ? activeUnit.definition.specialRange : activeUnit.definition.attackRange;
            if (GridDistance(activeUnit.cell, target.cell) > desiredRange && !activeUnit.moved)
            {
                BattleTestTile best = FindBestMoveToward(activeUnit, target.cell);
                if (best != null)
                {
                    activeUnit.cell = best.cell;
                    activeUnit.moved = true;
                    ApplyTileEntry(activeUnit, best);
                    yield return AnimateMove(activeUnit, UnitWorldPosition(best.cell));
                    yield return new WaitForSeconds(0.15f);
                }
            }

            if (CanUseSpecial(activeUnit) && IsValidSpecialTarget(activeUnit, target) && GridDistance(activeUnit.cell, target.cell) <= activeUnit.definition.specialRange)
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
            busy = false;
            EndTurn();
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
            unit.inner = Mathf.Min(unit.definition.maxInner, unit.inner + 1);
            unit.specialCooldownLeft = Mathf.Max(0, unit.specialCooldownLeft - 1);

            if (unit.poisoned)
            {
                unit.hp = Mathf.Max(0, unit.hp - 3);
                AddLog($"[Status] Poison deals 3 to {unit.definition.displayName}.");
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
                AddLog("[Terrain] Terrain effects faded.");
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

        private BattleForecast BuildForecast(BattleTestUnit actor, BattleTestUnit target)
        {
            if (actor == null || actor.defeated)
            {
                return BattleForecast.Invalid("No active attacker.");
            }

            bool attack = commandMode == BattleCommandMode.Attack;
            bool special = commandMode == BattleCommandMode.Skill;
            if (!attack && !special)
            {
                return BattleForecast.Invalid("Select Attack or Skill, then hover a target.");
            }

            if (target == null)
            {
                return BattleForecast.Invalid("Hover a target in Attack or Skill mode.");
            }

            string commandName = special ? actor.definition.specialName : "Attack";
            int range = special ? actor.definition.specialRange : actor.definition.attackRange;
            int distance = GridDistance(actor.cell, target.cell);
            string costText = special
                ? $"Cost: {actor.definition.specialCost} inner, CD {actor.definition.specialCooldown}"
                : "Cost: action";

            if (target.defeated)
            {
                return BattleForecast.Invalid(actor.definition.displayName, target.definition.displayName, commandName, "Target already defeated.", distance, range, costText);
            }

            if (special)
            {
                string unavailable = SpecialUnavailableReason(actor);
                if (!string.IsNullOrEmpty(unavailable))
                {
                    return BattleForecast.Invalid(actor.definition.displayName, target.definition.displayName, commandName, unavailable, distance, range, costText);
                }

                if (!IsValidSpecialTarget(actor, target))
                {
                    return BattleForecast.Invalid(actor.definition.displayName, target.definition.displayName, commandName, "No valid skill target.", distance, range, costText);
                }
            }
            else if (target.definition.faction == actor.definition.faction)
            {
                return BattleForecast.Invalid(actor.definition.displayName, target.definition.displayName, commandName, "No valid hostile target.", distance, range, costText);
            }

            if (distance > range)
            {
                return BattleForecast.Invalid(actor.definition.displayName, target.definition.displayName, commandName, "Out of range.", distance, range, costText);
            }

            BattleTestTile from = TileAt(actor.cell);
            BattleTestTile to = TileAt(target.cell);
            int heightBonus = from != null && to != null && from.elevation > to.elevation ? 2 : 0;
            int terrainBonus = 0;
            int attackBonus = actor.definition.attackBonus + (special ? actor.definition.specialAttackBonus : 0);
            int defense = DefenseValue(target, to);
            bool attackLike = !special || IsHostileAttackSpecial(actor.definition.specialEffect);
            BattleTestCounterMove counter = attackLike ? FindCounterMove(target, actor) : BattleTestCounterMove.None;
            bool followUp = attackLike && CanFollowUp(actor, target, special);
            string neededRollText = attackLike ? NeededRollText(defense, attackBonus, heightBonus, terrainBonus) : "No attack roll";
            if (!attackLike)
            {
                attackBonus = 0;
                heightBonus = 0;
                terrainBonus = 0;
                defense = 0;
            }

            int damageMin = 0;
            int damageMax = 0;
            string damageText = "Damage: none";
            string hpAfterText = $"Target HP after: {target.hp}-{target.hp}";
            if (attackLike)
            {
                damageMin = actor.definition.damageMin + (special ? actor.definition.specialPower : 0) + heightBonus;
                damageMax = actor.definition.damageMax + (special ? actor.definition.specialPower : 0) + heightBonus;
                if (target.guarded)
                {
                    damageMin = Mathf.Max(1, Mathf.CeilToInt(damageMin * 0.55f));
                    damageMax = Mathf.Max(1, Mathf.CeilToInt(damageMax * 0.55f));
                }

                damageMin = Mathf.Max(1, damageMin);
                damageMax = Mathf.Max(1, damageMax);
                damageText = $"Damage: {damageMin}-{damageMax} (crit x2 on nat 20)";
                hpAfterText = $"Target HP after: {Mathf.Max(0, target.hp - damageMax)}-{Mathf.Max(0, target.hp - damageMin)}";
            }

            return new BattleForecast(
                true,
                string.Empty,
                actor.definition.displayName,
                target.definition.displayName,
                commandName,
                distance,
                range,
                distance <= range ? "in range" : "out of range",
                attackBonus,
                heightBonus,
                terrainBonus,
                defense,
                neededRollText,
                damageText,
                hpAfterText,
                counter.valid ? $"Counter: {counter.label}" : "Counter: none",
                followUp ? $"Follow-up: yes (AGI {AgilityValue(actor)} vs {AgilityValue(target)})" : "Follow-up: no",
                costText);
        }

        private string CounterSummary(BattleTestUnit unit)
        {
            if (unit == null)
            {
                return "-";
            }

            return CanUseCounterSpecial(unit) ? unit.definition.specialName : $"Attack R{unit.definition.attackRange}";
        }

        private string SpecialUnavailableReason(BattleTestUnit unit)
        {
            if (unit == null)
            {
                return "No active unit.";
            }

            if (unit.acted)
            {
                return "Already acted.";
            }

            if (unit.definition.specialEffect == BattleSpecialEffect.None)
            {
                return "No skill equipped.";
            }

            if (unit.inner < unit.definition.specialCost)
            {
                return $"Not enough inner ({unit.inner}/{unit.definition.specialCost}).";
            }

            if (unit.specialCooldownLeft > 0)
            {
                return $"Cooldown {unit.specialCooldownLeft} turn(s).";
            }

            return string.Empty;
        }

        private bool IsHostileAttackSpecial(BattleSpecialEffect effect)
        {
            return effect == BattleSpecialEffect.Strike
                || effect == BattleSpecialEffect.Poison
                || effect == BattleSpecialEffect.Freeze
                || effect == BattleSpecialEffect.BreakGuard;
        }

        private string NeededRollText(int defense, int attackBonus, int heightBonus, int terrainBonus)
        {
            int needed = defense - attackBonus - heightBonus - terrainBonus;
            if (needed <= 2)
            {
                return "2+ on d20 (natural 1 misses)";
            }

            if (needed > 20)
            {
                return "20 only (natural 20 crits)";
            }

            return $"{needed}+ on d20";
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
                AddLog($"[Terrain] {unit.definition.displayName} entered flame: {damage} damage.");
                if (unit.hp == 0)
                {
                    unit.defeated = true;
                    unit.view.SetDefeated(true);
                    AddLog($"[Damage] {unit.definition.displayName} defeated.");
                }
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
                AddLog($"[Terrain] {unit.definition.displayName} takes {damage} {reason} damage.");
                if (unit.hp == 0)
                {
                    unit.defeated = true;
                    unit.view.SetDefeated(true);
                    AddLog($"[Damage] {unit.definition.displayName} defeated.");
                }
            }
        }

        private BattleTestInteractable FindUsableInteractable(BattleTestUnit actor, Vector2Int clickedCell)
        {
            foreach (BattleTestInteractable interactable in interactables)
            {
                if (interactable.used || interactable.cell != clickedCell)
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
                if (!interactable.used && GridDistance(actor.cell, interactable.cell) <= 1)
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
            if (tile.smokeTurns > 0)
            {
                states.Add("Smoke");
            }

            if (tile.fireTurns > 0)
            {
                states.Add("Fire");
            }

            if (tile.extraCover)
            {
                states.Add("Heavy Cover");
            }

            return states.Count == 0 ? "None" : string.Join(", ", states);
        }

        private string InteractableEffectText(BattleTestInteractableKind kind)
        {
            switch (kind)
            {
                case BattleTestInteractableKind.Smoke:
                    return $"Smoke cover +{SmokeCoverBonus}";
                case BattleTestInteractableKind.Fire:
                    return $"Fire damage {FireInteractDamage}";
                case BattleTestInteractableKind.Cover:
                    return $"Cover +{CoverInteractBonus}";
                default:
                    return "-";
            }
        }

        private void RefreshTerrainTint(BattleTestTile tile)
        {
            if (tile != null && tile.terrainRenderer != null)
            {
                if (tile.fireTurns > 0)
                {
                    tile.terrainRenderer.color = new Color(0.72f, 0.20f, 0.12f, 1f);
                }
                else if (tile.smokeTurns > 0)
                {
                    tile.terrainRenderer.color = new Color(0.54f, 0.54f, 0.50f, 1f);
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

            if (mode == BattleCommandMode.Interact && (activeUnit.acted || !HasUsableInteractable(activeUnit)))
            {
                AddLog("[UI] No usable terrain object in reach.");
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

            int index = activeIndex;
            int guard = 0;
            while (queue.Count < count && guard < units.Count * 2)
            {
                if (index >= units.Count)
                {
                    index = 0;
                }

                BattleTestUnit unit = units[index];
                if (!unit.defeated)
                {
                    queue.Add(unit);
                }

                index++;
                guard++;
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
                        tile.SetHighlight(new Color(1f, 0.62f, 0.16f, 0.58f));
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
            bool alliesAlive = false;
            bool enemiesAlive = false;

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
                }
            }

            if (alliesAlive && enemiesAlive)
            {
                return false;
            }

            battleOver = true;
            ClearHighlights();
            AddLog(alliesAlive ? "[System] Victory." : "[System] Defeat.");
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

            if (y == 3 && x >= 2 && x <= 7)
            {
                bool bridge = x == 4 || x == 5;
                return bridge
                    ? new TerrainProfile(TerrainType.Bridge, new Color(0.49f, 0.31f, 0.16f, 1f), 0, 0, 1, true)
                    : new TerrainProfile(TerrainType.Water, new Color(0.16f, 0.37f, 0.50f, 1f), 0, 0, 3, true);
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
            Rect leftPanel = new Rect(18f, 18f, 340f, 528f);
            Rect rightPanel = new Rect(Screen.width - 386f, 18f, 368f, 700f);
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

        private readonly struct BattleForecast
        {
            public static BattleForecast Invalid(string reason)
            {
                return new BattleForecast(false, reason, string.Empty, string.Empty, string.Empty, 0, 0, string.Empty, 0, 0, 0, 0, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty);
            }

            public static BattleForecast Invalid(string actorName, string targetName, string commandName, string reason, int distance, int range, string costText)
            {
                return new BattleForecast(false, reason, actorName, targetName, commandName, distance, range, "invalid", 0, 0, 0, 0, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, costText);
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

            public BattleForecast(bool valid, string invalidReason, string actorName, string targetName, string commandName, int distance, int range, string rangeText, int attackBonus, int heightBonus, int terrainBonus, int defense, string neededRollText, string damageText, string hpAfterText, string counterText, string followUpText, string costText)
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

            public BattleTestCounterMove(bool special, string label)
                : this(true, special, label)
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

            public TerrainProfile(TerrainType terrain, Color color, int elevation, int coverBonus, int moveCost, bool walkable)
            {
                this.terrain = terrain;
                this.color = color;
                this.elevation = elevation;
                this.coverBonus = coverBonus;
                this.moveCost = moveCost;
                this.walkable = walkable;
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
        Cover
    }

    public sealed class BattleTestInteractable
    {
        public readonly string id;
        public readonly string displayName;
        public readonly BattleTestInteractableKind kind;
        public readonly Vector2Int cell;
        public bool used;
        public SpriteRenderer renderer;

        public BattleTestInteractable(string id, string displayName, BattleTestInteractableKind kind, Vector2Int cell)
        {
            this.id = id;
            this.displayName = displayName;
            this.kind = kind;
            this.cell = cell;
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
        public SpriteRenderer terrainRenderer;
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
        }
    }
}
